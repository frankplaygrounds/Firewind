using System;
using System.Collections.Generic;
using System.Data;
using Firewind.HabboHotel.Catalogs;
using Firewind.HabboHotel.Items;
using Firewind.Core;
using Firewind.HabboHotel.Pets;
using Database_Manager.Database.Session_Details.Interfaces;
using HabboEvents;

namespace Firewind.Messages
{
    partial class GameClientMessageHandler
    {
        internal void GetCatalogIndex()
        {
            if (Session.GetHabbo() == null)
                return;
            Session.SendMessage(FirewindEnvironment.GetGame().GetCatalog().GetIndexMessageForRank(Session.GetHabbo().Rank));
        }

        internal void GetCatalogPage()
        {
            CatalogPage Page = FirewindEnvironment.GetGame().GetCatalog().GetPage(Request.ReadInt32());

            if (Page == null || !Page.Enabled || !Page.Visible || Page.MinRank > Session.GetHabbo().Rank)
            {
                return;
            }

            if (Page.ClubOnly && !Session.GetHabbo().GetSubscriptionManager().HasSubscription("habbo_vip"))
            {
                Session.SendNotif(LanguageLocale.GetValue("catalog.missingclubmembership"));
                return;
            }



            if (Page.Layout == "recycler")
            {
                Session.SendNotif("Ecotron was not coded yet!");
                return;
            }


            Session.SendMessage(Page.GetMessage);

            if (Page.Layout.Equals("club_buy"))
            {
                ServerMessage clubBuy = new ServerMessage(Outgoing.ClubComposer);
                clubBuy.AppendInt32(Page.Items.Values.Count);
                foreach (CatalogItem Item in Page.Items.Values)
                {
                    Item.SerializeClub(clubBuy, Session);
                }
                clubBuy.AppendInt32(1); // sorry don't know :(!
                Session.SendMessage(clubBuy);
            }
            /*
            if (Page.Layout == "recycler")
            {
                GetResponse().Init(507);
                GetResponse().AppendBoolean(true);
                GetResponse().AppendBoolean(false);
                SendResponse();
            }*/
        }

        internal void RedeemVoucher()
        {
            VoucherHandler.TryRedeemVoucher(Session, Request.ReadString());
        }

        internal void HandlePurchase()
        {
            int PageId = Request.ReadInt32();
            uint ItemId = Request.ReadUInt32();
            string extraParameter = Request.ReadString();
            int Amount = Request.ReadInt32();
            /*for (int i = 0; i < Session.GetHabbo().buyItemLoop; i++)
            {*/
            FirewindEnvironment.GetGame().GetCatalog().HandlePurchase(Session, PageId, ItemId, extraParameter, Amount, false, "", "", 0, 0, 0, false);
            //}
        }

        internal void PurchaseFromCatalogAsGift() // (k:int, k:int, k:String, k:String, k:String, k:int, k:int, k:int, k:Boolean)
        {
            int PageId = Request.ReadInt32(); // pageId
            uint ItemId = Request.ReadUInt32(); // offerId
            string ExtraData = Request.ReadString(); // extraParameter
            string GiftUser = FirewindEnvironment.FilterInjectionChars(Request.ReadString());
            string GiftMessage = FirewindEnvironment.FilterInjectionChars(Request.ReadString());
            int SpriteId = Request.ReadInt32();
            int Lazo = Request.ReadInt32();
            int Color = Request.ReadInt32();
            bool showIdentity = Request.ReadBoolean();

            //bool dnow = Request.PopWiredBoolean();
            //Logging.WriteLine("PageId: " + PageId + "; ItemId: " + ItemId + "; ExtraData: " + ExtraData + "; User: " + GiftUser + "; Message: " + GiftMessage + "; SpriteId: " + SpriteId + "; Color: " + Color + "; Lazo: " + Lazo);
            FirewindEnvironment.GetGame().GetCatalog().HandlePurchase(Session, PageId, ItemId, ExtraData, 1, true, GiftUser, GiftMessage, SpriteId, Lazo, Color, showIdentity);
        }

        internal void GetRecyclerRewards()
        {
            // GzQAQAXtGIsZJKPAPrIsXLKKPJKsY}JsXBKsX~JJPASCsX|JiXBPsZAKs[|JiYBPsZ}JsXAKsYAKsX}JsY|JsY~Js[{JiZAPs[JsZBKIIRAsX@KsYBKsZJs[@Ks[~JsZ|J

            GetResponse().Init(Outgoing.RecyclePrizes);
            GetResponse().AppendInt32(0);
            GetResponse().AppendInt32(0);
            GetResponse().AppendInt32(0);

            //for (uint i = 5; i >= 1; i--)
            //{
            //    GetResponse().AppendUInt(i);

            //    if (i <= 1)
            //    {
            //        GetResponse().AppendInt32(0);
            //    }
            //    else if (i == 2)
            //    {
            //        GetResponse().AppendInt32(4);
            //    }
            //    else if (i == 3)
            //    {
            //        GetResponse().AppendInt32(40);
            //    }
            //    else if (i == 4)
            //    {
            //        GetResponse().AppendInt32(200);
            //    }
            //    else if (i >= 5)
            //    {
            //        GetResponse().AppendInt32(2000);
            //    }

            //    List<EcotronReward> Rewards = FirewindEnvironment.GetGame().GetCatalog().GetEcotronRewardsForLevel(i);

            //    GetResponse().AppendInt32(Rewards.Count);

            //    foreach (EcotronReward Reward in Rewards)
            //    {
            //        GetResponse().AppendString(Reward.GetBaseItem().Type.ToString().ToLower());
            //        GetResponse().AppendUInt(Reward.DisplayId);
            //    }
            //}

            SendResponse();
        }

        internal void CanGift()
        {
            uint Id = Request.ReadUInt32();

            CatalogItem Item = FirewindEnvironment.GetGame().GetCatalog().FindItem(Id);

            if (Item == null)
            {
                return;
            }

            /*GetResponse().Init(622);
            GetResponse().AppendUInt(Item.Id);
            GetResponse().AppendBoolean(Item.GetBaseItem().AllowGift);
            SendResponse();*/
        }

        internal void GetMarketplaceConfiguration()
        {
            GetResponse().Init(Outgoing.MarketplaceConfiguration);
            //  1 1 1 5 1 10000 48 7
            GetResponse().AppendBoolean(true);
            GetResponse().AppendInt32(1);
            GetResponse().AppendInt32(0);
            GetResponse().AppendInt32(0);
            GetResponse().AppendInt32(1);
            GetResponse().AppendInt32(10000);
            GetResponse().AppendInt32(48);
            GetResponse().AppendInt32(7);
            SendResponse();

            //1244
        }

        internal void GetCataData2()
        {
            GetResponse().Init(Outgoing.ShopData2);
            GetResponse().AppendBoolean(true);
            GetResponse().AppendInt32(1);
            GetResponse().AppendInt32(10);
            for (int i = 3372; i < 3382; )
            {
                GetResponse().AppendInt32(i);
                i++;
            }
            GetResponse().AppendInt32(7);
            GetResponse().AppendInt32(0);
            GetResponse().AppendInt32(1);
            GetResponse().AppendInt32(2);
            GetResponse().AppendInt32(3);
            GetResponse().AppendInt32(4);
            GetResponse().AppendInt32(5);
            GetResponse().AppendInt32(6);
            GetResponse().AppendInt32(11);
            GetResponse().AppendInt32(0);
            GetResponse().AppendInt32(1);
            GetResponse().AppendInt32(2);
            GetResponse().AppendInt32(3);
            GetResponse().AppendInt32(4);
            GetResponse().AppendInt32(5);
            GetResponse().AppendInt32(6);
            GetResponse().AppendInt32(7);
            GetResponse().AppendInt32(8);
            GetResponse().AppendInt32(9);
            GetResponse().AppendInt32(10);
            GetResponse().AppendInt32(7);
            for (int i = 187; i < 194; )
            {
                GetResponse().AppendInt32(i);
                i++;
            }
            SendResponse();

            GetResponse().Init(Outgoing.Offer);
            GetResponse().AppendInt32(100);
            GetResponse().AppendInt32(6);
            GetResponse().AppendInt32(1);
            GetResponse().AppendInt32(1);
            GetResponse().AppendInt32(2);
            GetResponse().AppendInt32(40);
            GetResponse().AppendInt32(99);
            SendResponse();
        }

        internal void MarketplaceCanSell()
        {
            GetResponse().Init(Outgoing.MarketplaceCanMakeOfferResult);
            GetResponse().AppendInt32(1);
            GetResponse().AppendInt32(0);
            SendResponse();
        }

        internal void MarketplaceItemStats()
        {
            int itemType = 0;
            int spriteId = 0;

            if (Request.RemainingLength >= 4)
                itemType = Request.ReadInt32(); // floor item = 1, wall item = 2

            if (Request.RemainingLength >= 4)
                spriteId = Request.ReadInt32(); // furni type/sprite id

            GetResponse().Init(Outgoing.MarketplaceItemStats);
            GetResponse().AppendInt32(GetMarketplaceAveragePrice(spriteId, 7));
            GetResponse().AppendInt32(GetMarketplaceItemsOnSale(spriteId));
            GetResponse().AppendInt32(30);
            GetResponse().AppendInt32(0);
            GetResponse().AppendInt32(itemType);
            GetResponse().AppendInt32(spriteId);
            SendResponse();
        }

        internal void MarketplacePostItem()
        {
            if (Request.RemainingLength < 12)
            {
                MarketplaceCanSell();
                return;
            }

            if (Session.GetHabbo().GetInventoryComponent() == null)
            {
                return;
            }

            int sellingPrice = Request.ReadInt32();
            int junk = Request.ReadInt32();
            uint itemId = Request.ReadUInt32();

            UserItem Item = Session.GetHabbo().GetInventoryComponent().GetItem(itemId);

            if (Item == null || !Item.GetBaseItem().AllowTrade)
            {
                return;
            }

            Marketplace.SellItem(Session, Item.Id, sellingPrice);
        }

        internal void MarketplaceGetOwnOffers()
        {
            Session.SendMessage(Marketplace.SerializeOwnOffers(Session.GetHabbo().Id));
        }

        internal void MarketplaceTakeBack()
        {
            uint ItemId = Request.ReadUInt32();
            DataRow Row = null;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("SELECT item_id, user_id, extra_data, offer_id, state FROM catalog_marketplace_offers WHERE offer_id = @offer_id LIMIT 1");
                dbClient.addParameter("offer_id", ItemId);
                Row = dbClient.getRow();
            }

            if (Row == null || Convert.ToUInt32(Row["user_id"]) != Session.GetHabbo().Id || Row["state"].ToString() != "1")
            {
                return;
            }

            Item Item = FirewindEnvironment.GetGame().GetItemManager().GetItem(Convert.ToUInt32(Row["item_id"]));

            if (Item == null)
            {
                return;
            }

            FirewindEnvironment.GetGame().GetCatalog().DeliverItems(Session, Item, 1, (String)Row["extra_data"]);

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("DELETE FROM catalog_marketplace_offers WHERE offer_id = @offer_id LIMIT 1");
                dbClient.addParameter("offer_id", ItemId);
                dbClient.runQuery();
            }

            GetResponse().Init(Outgoing.MarketplaceCancelSaleResult);
            GetResponse().AppendUInt(Convert.ToUInt32(Row["offer_id"]));
            GetResponse().AppendBoolean(true);
            SendResponse();
        }

        internal void MarketplaceClaimCredits()
        {
            DataTable Results = null;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("SELECT asking_price FROM catalog_marketplace_offers WHERE user_id = @user_id AND state = '2'");
                dbClient.addParameter("user_id", Session.GetHabbo().Id);
                Results = dbClient.getTable();
            }

            if (Results == null)
            {
                return;
            }

            int Profit = 0;

            foreach (DataRow Row in Results.Rows)
            {
                Profit += (int)Row["asking_price"];
            }

            if (Profit >= 1)
            {
                Session.GetHabbo().Credits += Profit;
                Session.GetHabbo().UpdateCreditsBalance();
            }

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("DELETE FROM catalog_marketplace_offers WHERE user_id = @user_id AND state = '2'");
                dbClient.addParameter("user_id", Session.GetHabbo().Id);
                dbClient.runQuery();
            }
        }

        internal void MarketplaceGetOffers()
        {
            int MinPrice = Request.ReadInt32();
            int MaxPrice = Request.ReadInt32();
            string SearchQuery = Request.ReadString();
            int FilterMode = Request.ReadInt32();

            Session.SendMessage(Marketplace.SerializeOffers(MinPrice, MaxPrice, SearchQuery, FilterMode));
        }

        internal void MarketplacePurchase()
        {
            uint ItemId = Request.ReadUInt32();
            DataRow Row = null;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("SELECT state, timestamp, total_price, extra_data, item_id, user_id FROM catalog_marketplace_offers WHERE offer_id = @offer_id LIMIT 1");
                dbClient.addParameter("offer_id", ItemId);
                Row = dbClient.getRow();
            }

            if (Row == null || (string)Row["state"] != "1" || (double)Row["timestamp"] <= Marketplace.FormatTimestamp())
            {
                Session.SendNotif(LanguageLocale.GetValue("catalog.offerexpired"));
                return;
            }

            Item Item = FirewindEnvironment.GetGame().GetItemManager().GetItem(Convert.ToUInt32(Row["item_id"]));

            if (Item == null)
            {
                return;
            }

            int prize = (int)Row["total_price"];
            if (Convert.ToUInt32(Row["user_id"]) == Session.GetHabbo().Id || Session.GetHabbo().Credits < prize)
            {
                SendMarketplaceBuyResult(4, 0, ItemId, prize);
                return;
            }

            if ((int)Row["total_price"] >= 1)
            {
                Session.GetHabbo().Credits -= prize;
                Session.GetHabbo().UpdateCreditsBalance();
            }

            FirewindEnvironment.GetGame().GetCatalog().DeliverItems(Session, Item, 1, (String)Row["extra_data"]);
            Session.GetHabbo().GetInventoryComponent().RunDBUpdate();

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("UPDATE catalog_marketplace_offers SET state = '2' WHERE offer_id = @offer_id LIMIT 1");
                dbClient.addParameter("offer_id", ItemId);
                dbClient.runQuery();
            }


            SendMarketplaceBuyResult(1, 0, ItemId, prize);

            Session.SendMessage(Marketplace.SerializeOffers(-1, -1, "", 1));
        }

        private int GetMarketplaceItemsOnSale(int spriteId)
        {
            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("SELECT COUNT(*) FROM catalog_marketplace_offers WHERE state = '1' AND timestamp >= @timestamp AND sprite_id = @sprite_id");
                dbClient.addParameter("timestamp", Marketplace.FormatTimestamp());
                dbClient.addParameter("sprite_id", spriteId);
                return dbClient.getInteger();
            }
        }

        private int GetMarketplaceAveragePrice(int spriteId, int days)
        {
            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("SELECT AVG(asking_price) AS average_price FROM catalog_marketplace_offers WHERE state = '2' AND timestamp >= @timestamp AND sprite_id = @sprite_id");
                dbClient.addParameter("timestamp", FirewindEnvironment.GetUnixTimestamp() - (days * 86400));
                dbClient.addParameter("sprite_id", spriteId);
                DataRow row = dbClient.getRow();
                if (row == null || row["average_price"] == DBNull.Value)
                    return 0;

                return Convert.ToInt32(row["average_price"]);
            }
        }

        private void SendMarketplaceBuyResult(int result, int newOfferId, uint requestedOfferId, int price)
        {
            GetResponse().Init(Outgoing.MarketplaceBuyResult);
            GetResponse().AppendInt32(result);
            GetResponse().AppendInt32(newOfferId);
            GetResponse().AppendUInt(requestedOfferId);
            GetResponse().AppendInt32(price);
            SendResponse();
        }

        internal void CheckPetName()
        {
            String PetName = Request.ReadString();
            Session.GetMessageHandler().GetResponse().Init(Outgoing.CheckPetName);
            Session.GetMessageHandler().GetResponse().AppendInt32(Catalog.CheckPetName(PetName) ? 0 : 2);
            Session.GetMessageHandler().GetResponse().AppendString(PetName);
            Session.GetMessageHandler().SendResponse();
        }

        internal void PetRaces()
        {
            string PetType = Request.ReadString();

            int petid = 0;
            GetResponse().Init(Outgoing.PetRace);

            switch (PetType)
            {
                case "a0 pet0":
                    GetResponse().AppendString("a0 pet0");
                    petid = 0;
                    break;

                case "a0 pet1":
                    GetResponse().AppendString("a0 pet1");
                    petid = 1;
                    break;

                case "a0 pet2":
                    GetResponse().AppendString("a0 pet2");
                    petid = 2;
                    break;

                case "a0 pet3":
                    GetResponse().AppendString("a0 pet3");
                    petid = 3;
                    break;

                case "a0 pet4":
                    GetResponse().AppendString("a0 pet4");
                    petid = 4;
                    break;

                case "a0 pet5":
                    GetResponse().AppendString("a0 pet5");
                    petid = 5;
                    break;

                case "a0 pet6":
                    GetResponse().AppendString("a0 pet6");
                    petid = 6;
                    break;

                case "a0 pet7":
                    GetResponse().AppendString("a0 pet7");
                    petid = 7;
                    break;

                case "a0 pet8":
                    GetResponse().AppendString("a0 pet8");
                    petid = 8;
                    break;

                case "a0 pet9":
                    GetResponse().AppendString("a0 pet9");
                    petid = 9;
                    break;

                case "a0 pet10":
                    GetResponse().AppendString("a0 pet10");
                    petid = 10;
                    break;

                case "a0 pet11":
                    GetResponse().AppendString("a0 pet11");
                    petid = 11;
                    break;

                case "a0 pet12":
                    GetResponse().AppendString("a0 pet12");
                    petid = 12;
                    break;

                case "a0 pet13": // Caballo - Horse
                    GetResponse().AppendString("a0 pet13");
                    petid = 13;
                    break;


                case "a0 pet14":
                    GetResponse().AppendString("a0 pet14");
                    petid = 14;
                    break;

                case "a0 pet15":
                    GetResponse().AppendString("a0 pet15");
                    petid = 15;
                    break;

                case "a0 pet16": // MosterPlant
                    GetResponse().AppendString("a0 pet16");
                    petid = 16;
                    break;

                case "a0 pet17": // bunnyeaster
                    GetResponse().AppendString("a0 pet17");
                    petid = 17;
                    break;

                case "a0 pet18": // bunnydepressed
                    GetResponse().AppendString("a0 pet18");
                    petid = 18;
                    break;

                case "a0 pet19": // bunnylove
                    GetResponse().AppendString("a0 pet19");
                    petid = 19;
                    break;

                case "a0 pet20": // MosterPlant
                    GetResponse().AppendString("a0 pet20");
                    petid = 20;
                    break;

                case "a0 pet21": // pigeonevil
                    GetResponse().AppendString("a0 pet21");
                    petid = 21;
                    break;

                case "a0 pet22": //pigeongood
                    GetResponse().AppendString("a0 pet22");
                    petid = 22;
                    break;
            }

            if (PetRace.RaceGotRaces(petid))
            {
                List<PetRace> Races = PetRace.GetRacesForRaceId(petid);
                GetResponse().AppendInt32(Races.Count);
                foreach (PetRace r in Races)
                {
                    GetResponse().AppendInt32(petid); // pet id
                    GetResponse().AppendInt32(r.Color1); // color1
                    GetResponse().AppendInt32(r.Color2); // color2
                    GetResponse().AppendBoolean(r.Has1Color); // has1color
                    GetResponse().AppendBoolean(r.Has2Color); // has2color
                }
            }
            else
            {
                Session.SendNotif("¡Ha ocurrido un error cuando ibas a ver esta mascota, repórtalo a un administrador!");
                GetResponse().AppendInt32(0);
            }
            SendResponse();
        }

        //internal void RegisterCatalog()
        //{
        //    RequestHandlers.Add(101, new RequestHandler(GetCatalogIndex));
        //    RequestHandlers.Add(102,new RequestHandler(GetCatalogPage));
        //    RequestHandlers.Add(129,new RequestHandler(RedeemVoucher));
        //    RequestHandlers.Add(100,new RequestHandler(HandlePurchase));
        //    RequestHandlers.Add(472,new RequestHandler(PurchaseGift));
        //    RequestHandlers.Add(412,new RequestHandler(GetRecyclerRewards));
        //    RequestHandlers.Add(3030,new RequestHandler(CanGift));
        //    RequestHandlers.Add(3011,new RequestHandler(GetCataData1));
        //    RequestHandlers.Add(473,new RequestHandler(GetCataData2));
        //    RequestHandlers.Add(3012,new RequestHandler(MarketplaceCanSell));
        //    RequestHandlers.Add(3010,new RequestHandler(MarketplacePostItem));
        //    RequestHandlers.Add(3019,new RequestHandler(MarketplaceGetOwnOffers));
        //    RequestHandlers.Add(3015,new RequestHandler(MarketplaceTakeBack));
        //    RequestHandlers.Add(3016,new RequestHandler(MarketplaceClaimCredits));
        //    RequestHandlers.Add(3018,new RequestHandler(MarketplaceGetOffers));
        //    RequestHandlers.Add(3014,new RequestHandler(MarketplacePurchase));
        //    RequestHandlers.Add(42, new RequestHandler(CheckPetName));
        //    RequestHandlers.Add(3007, new RequestHandler(PetRaces));
        //}

        //internal void UnregisterCatalog()
        //{
        //    RequestHandlers.Remove(101);
        //    RequestHandlers.Remove(102);
        //    RequestHandlers.Remove(129);
        //    RequestHandlers.Remove(100);
        //    RequestHandlers.Remove(472);
        //    RequestHandlers.Remove(412);
        //    RequestHandlers.Remove(3030);
        //    RequestHandlers.Remove(3011);
        //    RequestHandlers.Remove(473);
        //    RequestHandlers.Remove(3012);
        //    RequestHandlers.Remove(3010);
        //    RequestHandlers.Remove(3019);
        //    RequestHandlers.Remove(3015);
        //    RequestHandlers.Remove(3016);
        //    RequestHandlers.Remove(3018);
        //    RequestHandlers.Remove(3014);
        //    RequestHandlers.Remove(3007);
        //    RequestHandlers.Remove(42);
        //}
    }
}
