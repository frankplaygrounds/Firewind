using System;
using System.Data;
using System.Text;
using Firewind.HabboHotel.GameClients;
using Firewind.HabboHotel.Items;
using Firewind.Messages;
using Database_Manager.Database.Session_Details.Interfaces;
using Database_Manager.Database;

namespace Firewind.HabboHotel.Catalogs
{
    class Marketplace
    {
        internal static Boolean CanSellItem(UserItem Item)
        {
            if (!Item.GetBaseItem().AllowTrade || !Item.GetBaseItem().AllowMarketplaceSell)
            {
                return false;
            }

            return true;
        }

        internal static void SellItem(GameClient Session, uint ItemId, int SellingPrice)
        {
            UserItem Item = Session.GetHabbo().GetInventoryComponent().GetItem(ItemId);
            
            if (Item == null || SellingPrice < 1 || SellingPrice > 10000 || !CanSellItem(Item))
            {
                Session.GetMessageHandler().GetResponse().Init(610);
                Session.GetMessageHandler().GetResponse().AppendBoolean(false);
                Session.GetMessageHandler().SendResponse();
                return;
            }



            int Comission = CalculateComissionPrice(SellingPrice);
            int TotalPrice = SellingPrice + Comission;
            int ItemType = 1;

            if (Item.GetBaseItem().Type == 'i')
                ItemType++;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("INSERT INTO catalog_marketplace_offers (item_id,user_id,asking_price,total_price,public_name,sprite_id,item_type,timestamp,extra_data) VALUES (@item_id,@user_id,@asking_price,@total_price,@public_name,@sprite_id,@item_type,@timestamp,@extra_data)");
                dbClient.addParameter("item_id", Item.BaseItem);
                dbClient.addParameter("user_id", Session.GetHabbo().Id);
                dbClient.addParameter("asking_price", SellingPrice);
                dbClient.addParameter("total_price", TotalPrice);
                dbClient.addParameter("public_name", Item.GetBaseItem().Name);
                dbClient.addParameter("sprite_id", Item.GetBaseItem().SpriteId);
                dbClient.addParameter("item_type", ItemType);
                dbClient.addParameter("timestamp", FirewindEnvironment.GetUnixTimestamp());
                dbClient.addParameter("extra_data", Item.Data);
                dbClient.runQuery();
            }

            Session.GetHabbo().GetInventoryComponent().RemoveItem(ItemId, false);
            Session.GetHabbo().GetInventoryComponent().RunDBUpdate();

            Session.GetMessageHandler().GetResponse().Init(610);
            Session.GetMessageHandler().GetResponse().AppendBoolean(true);
            Session.GetMessageHandler().SendResponse();
        }

        internal static int CalculateComissionPrice(float SellingPrice)
        {
            return (int)Math.Ceiling((float)(SellingPrice / 100));
        }

        internal static Double FormatTimestamp()
        {
            return FirewindEnvironment.GetUnixTimestamp() - 172800;
        }

        internal static string FormatTimestampString()
        {
            return TextHandling.GetFirstSiffer(FormatTimestamp()).ToString();
        }

        internal static ServerMessage SerializeOffers(int MinCost, int MaxCost, String SearchQuery, int FilterMode)
        {
            // IgI`UJUIIY~JX]gXoAJISA

            DataTable Data = new DataTable();
            StringBuilder WhereClause = new StringBuilder();
            string OrderMode = "";

            WhereClause.Append("WHERE state = '1' AND timestamp >= " + FormatTimestampString());

            if (MinCost >= 0)
            {
                WhereClause.Append(" AND total_price > " + MinCost);
            }

            if (MaxCost >= 0)
            {
                WhereClause.Append(" AND total_price < " + MaxCost);
            }

            switch (FilterMode)
            {
                case 1:
                default:

                    OrderMode = "ORDER BY asking_price DESC";
                    break;

                case 2:

                    OrderMode = "ORDER BY asking_price ASC";
                    break;
            }

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                if (SearchQuery.Length >= 1)
                    WhereClause.Append(" AND public_name LIKE @search_query");


                dbClient.setQuery("SELECT offer_id, item_type, sprite_id, total_price FROM catalog_marketplace_offers " + WhereClause.ToString() + " " + OrderMode + " LIMIT 100");
                if (SearchQuery.Length >= 1)
                    dbClient.addParameter("search_query", SearchQuery + "%");

                Data = dbClient.getTable();
            }

            ServerMessage Message = new ServerMessage(615);

            if (Data != null)
            {
                Message.AppendInt32(Data.Rows.Count);

                foreach (DataRow Row in Data.Rows)
                {
                    Message.AppendUInt(Convert.ToUInt32(Row["offer_id"]));
                    Message.AppendInt32(1);
                    Message.AppendInt32(int.Parse(Row["item_type"].ToString()));
                    Message.AppendInt32((int)Row["sprite_id"]); // Sprite ID
                    Message.AppendString(""); // Extra Chr (R52)
                    Message.AppendInt32((int)Row["total_price"]); // Price
                    Message.AppendInt32((int)Row["sprite_id"]); // ??
                    Message.AppendInt32((int)Row["total_price"]); // Avg
                    Message.AppendInt32(0); // Offers
                }
            }
            else
            {
                Message.AppendInt32(0);
            }

            return Message;
        }

        internal static ServerMessage SerializeOwnOffers(uint HabboId)
        {
            int Profits = 0;
            DataTable Data;
            String RawProfit;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("SELECT timestamp, state, offer_id, item_type, sprite_id, total_price FROM catalog_marketplace_offers WHERE user_id = " + HabboId);
                Data = dbClient.getTable();

                dbClient.setQuery("SELECT SUM(asking_price) FROM catalog_marketplace_offers WHERE state = '2' AND user_id = " + HabboId);
                DataRow profitRow = dbClient.getRow();
                RawProfit = profitRow == null || profitRow[0] == DBNull.Value ? string.Empty : profitRow[0].ToString();
            }

            if (RawProfit.Length > 0)
                Profits = int.Parse(RawProfit);

            ServerMessage Message = new ServerMessage(616);
            Message.AppendInt32(Profits);

            if (Data != null)
            {
                Message.AppendInt32(Data.Rows.Count);

                foreach (DataRow Row in Data.Rows)
                {
                    int MinutesLeft = (int)Math.Floor((((Double)Row["timestamp"] + 172800) - FirewindEnvironment.GetUnixTimestamp()) / 60);
                    int state = int.Parse(Row["state"].ToString());

                    if (MinutesLeft <= 0)
                    {
                        state = 3;
                        MinutesLeft = 0;
                    }

                    Message.AppendUInt(Convert.ToUInt32(Row["offer_id"]));
                    Message.AppendInt32(state); // 1 = active, 2 = sold, 3 = expired
                    Message.AppendInt32(int.Parse(Row["item_type"].ToString())); // always 1 (??)
                    Message.AppendInt32((int)Row["sprite_id"]);
                    Message.AppendString(""); // Extra Chr (R52)
                    Message.AppendInt32((int)Row["total_price"]); // ??
                    Message.AppendInt32(MinutesLeft);
                    Message.AppendInt32((int)Row["sprite_id"]);
                }
            }
            else
                Message.AppendInt32(0);

            return Message;
        }
    }
}
