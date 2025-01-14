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
            GetResponse().Init(Outgoing.ShopData1);
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
            GetResponse().Init(611);
            GetResponse().AppendBoolean(true);
            GetResponse().AppendInt32(99999);
            SendResponse();
        }

        internal void MarketplacePostItem()
        {
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
                dbClient.setQuery("SELECT item_id, user_id, extra_data, offer_id state FROM catalog_marketplace_offers WHERE offer_id = " + ItemId + " LIMIT 1");
                Row = dbClient.getRow();
            }

            if (Row == null || Convert.ToUInt32(Row["user_id"]) != Session.GetHabbo().Id || (UInt32)Row["state"] != 1)
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
                dbClient.runFastQuery("DELETE FROM catalog_marketplace_offers WHERE offer_id = " + ItemId + "");
            }

            GetResponse().Init(614);
            GetResponse().AppendUInt(Convert.ToUInt32(Row["offer_id"]));
            GetResponse().AppendBoolean(true);
            SendResponse();
        }

        internal void MarketplaceClaimCredits()
        {
            DataTable Results = null;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("SELECT asking_price FROM catalog_marketplace_offers WHERE user_id = " + Session.GetHabbo().Id + " AND state = 2");
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
                dbClient.runFastQuery("DELETE FROM catalog_marketplace_offers WHERE user_id = " + Session.GetHabbo().Id + " AND state = 2");
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
                dbClient.setQuery("SELECT state, timestamp, total_price, extra_data, item_id FROM catalog_marketplace_offers WHERE offer_id = " + ItemId + " ");
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
            if ((int)Row["total_price"] >= 1)
            {
                Session.GetHabbo().Credits -= prize;
                Session.GetHabbo().UpdateCreditsBalance();
            }

            FirewindEnvironment.GetGame().GetCatalog().DeliverItems(Session, Item, 1, (String)Row["extra_data"]);
            Session.GetHabbo().GetInventoryComponent().RunDBUpdate();

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.runFastQuery("UPDATE catalog_marketplace_offers SET state = 2 WHERE offer_id = " + ItemId + "");
            }


            Session.GetMessageHandler().GetResponse().Init(67);
            Session.GetMessageHandler().GetResponse().AppendUInt(Item.ItemId);
            Session.GetMessageHandler().GetResponse().AppendString(Item.Name);
            Session.GetMessageHandler().GetResponse().AppendInt32(prize);
            Session.GetMessageHandler().GetResponse().AppendInt32(0);
            Session.GetMessageHandler().GetResponse().AppendInt32(0);
            Session.GetMessageHandler().GetResponse().AppendInt32(1);
            Session.GetMessageHandler().GetResponse().AppendString(Item.Type.ToString());
            Session.GetMessageHandler().GetResponse().AppendInt32(Item.SpriteId);
            Session.GetMessageHandler().GetResponse().AppendString("");
            Session.GetMessageHandler().GetResponse().AppendInt32(1);
            Session.GetMessageHandler().GetResponse().AppendInt32(0);
            Session.GetMessageHandler().SendResponse();

            Session.SendMessage(Marketplace.SerializeOffers(-1, -1, "", 1));
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

            int count = 0, petid = 0;
            GetResponse().Init(Outgoing.PetRace);

            switch (PetType)
            {
                case "a0 pet0":
                    GetResponse().AppendString("a0 pet0");
                    count = 25;
                    petid = 0;
                    break;

                case "a0 pet1":
                    GetResponse().AppendString("a0 pet1");
                    count = 25;
                    petid = 1;
                    break;

                case "a0 pet2":
                    GetResponse().AppendString("a0 pet2");
                    count = 12;
                    petid = 2;
                    break;

                case "a0 pet3":
                    GetResponse().AppendString("a0 pet3");
                    count = 7;
                    petid = 3;
                    break;

                case "a0 pet4":
                    GetResponse().AppendString("a0 pet4");
                    count = 4;
                    petid = 4;
                    break;

                case "a0 pet5":
                    GetResponse().AppendString("a0 pet5");
                    count = 7;
                    petid = 5;
                    break;

                case "a0 pet6":
                    GetResponse().AppendString("a0 pet6");
                    count = 13;
                    petid = 6;
                    break;

                case "a0 pet7":
                    GetResponse().AppendString("a0 pet7");
                    count = 8;
                    petid = 7;
                    break;

                case "a0 pet8":
                    GetResponse().AppendString("a0 pet8");
                    count = 13;
                    petid = 8;
                    break;

                case "a0 pet9":
                    GetResponse().AppendString("a0 pet9");
                    count = 14;
                    petid = 9;
                    break;

                case "a0 pet10":
                    GetResponse().AppendString("a0 pet10");
                    count = 1;
                    petid = 10;
                    break;

                case "a0 pet11":
                    GetResponse().AppendString("a0 pet11");
                    count = 14;
                    petid = 11;
                    break;

                case "a0 pet12":
                    GetResponse().AppendString("a0 pet12");
                    count = 8;
                    petid = 12;
                    break;

                case "a0 pet13": // Caballo - Horse
                    GetResponse().AppendString("a0 pet13");
                    count = 17;
                    petid = 13;
                    break;


                case "a0 pet14":
                    GetResponse().AppendString("a0 pet14");
                    count = 9;
                    petid = 14;
                    break;

                case "a0 pet15":
                    GetResponse().AppendString("a0 pet15");
                    count = 16;
                    petid = 15;
                    break;

                case "a0 pet16": // MosterPlant
                    GetResponse().AppendString("a0 pet16");
                    count = 18;
                    petid = 16;
                    break;

                case "a0 pet17": // bunnyeaster
                    GetResponse().AppendString("a0 pet17");
                    count = 19;
                    petid = 17;
                    break;

                case "a0 pet18": // bunnydepressed
                    GetResponse().AppendString("a0 pet18");
                    count = 20;
                    petid = 18;
                    break;

                case "a0 pet19": // bunnylove
                    GetResponse().AppendString("a0 pet19");
                    count = 21;
                    petid = 19;
                    break;

                case "a0 pet20": // MosterPlant
                    GetResponse().AppendString("a0 pet20");
                    count = 22;
                    petid = 20;
                    break;

                case "a0 pet21": // pigeonevil
                    GetResponse().AppendString("a0 pet21");
                    count = 23;
                    petid = 21;
                    break;

                case "a0 pet22": //pigeongood
                    GetResponse().AppendString("a0 pet22");
                    count = 24;
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
