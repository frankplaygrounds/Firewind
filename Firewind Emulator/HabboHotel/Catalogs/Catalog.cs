using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Firewind.Core;
using Firewind.HabboHotel.GameClients;
using Firewind.HabboHotel.Items;
using Firewind.HabboHotel.Pets;
using Firewind.HabboHotel.Users.Inventory;
using Firewind.Messages;
using Database_Manager.Database.Session_Details.Interfaces;
using HabboEvents;
using Firewind.HabboHotel.Rooms;
using Firewind.HabboHotel.RoomBots;
using Firewind.HabboHotel.Groups.Types;


namespace Firewind.HabboHotel.Catalogs
{
    class Catalog
    {
        internal Dictionary<int, CatalogPage> Pages;
        internal List<EcotronReward> EcotronRewards;

        private Marketplace Marketplace;

        private ServerMessage[] mCataIndexCache;
        //private Task mFurniIDCYcler;

        internal Catalog()
        {
            Marketplace = new Marketplace();
        }

        internal void Initialize(IQueryAdapter dbClient)
        {
            Pages = new Dictionary<int, CatalogPage>();
            EcotronRewards = new List<EcotronReward>();

            dbClient.setQuery("SELECT * FROM catalog_pages ORDER BY order_num");
            DataTable Data = dbClient.getTable();

            dbClient.setQuery("SELECT * FROM ecotron_rewards ORDER BY item_id");
            DataTable EcoData = dbClient.getTable();

            Hashtable CataItems = new Hashtable();
            dbClient.setQuery("SELECT * FROM catalog_items");
            DataTable CatalogueItems = dbClient.getTable();

            if (CatalogueItems != null)
            {
                foreach (DataRow Row in CatalogueItems.Rows)
                {
                    if (string.IsNullOrEmpty(Row["item_ids"].ToString()) || (int)Row["amount"] <= 0)
                    {
                        continue;
                    }
                    CataItems.Add(Convert.ToUInt32(Row["id"]), new CatalogItem(Row));
                    //Items.Add(new CatalogItem((uint)Row["id"], (string)Row["catalog_name"], (string)Row["item_ids"], (int)Row["cost_credits"], (int)Row["cost_pixels"], (int)Row["amount"]));
                }
            }

            if (Data != null)
            {
                foreach (DataRow Row in Data.Rows)
                {
                    Boolean Visible = false;
                    Boolean Enabled = false;

                    if (Row["visible"].ToString() == "1")
                    {
                        Visible = true;
                    }

                    if (Row["enabled"].ToString() == "1")
                    {
                        Enabled = true;
                    }

                    Pages.Add((int)Row["id"], new CatalogPage((int)Row["id"], (int)Row["parent_id"],
                        (string)Row["caption"], Visible, Enabled, Convert.ToUInt32(Row["min_rank"]),
                        FirewindEnvironment.EnumToBool(Row["club_only"].ToString()), (int)Row["icon_color"],
                        (int)Row["icon_image"], (string)Row["page_layout"], (string)Row["page_headline"],
                        (string)Row["page_teaser"], (string)Row["page_special"], (string)Row["page_text1"],
                        (string)Row["page_text2"], (string)Row["page_text_details"], (string)Row["page_text_teaser"], ref CataItems));
                }
            }

            if (EcoData != null)
            {
                foreach (DataRow Row in EcoData.Rows)
                {
                    EcotronRewards.Add(new EcotronReward(Convert.ToUInt32(Row["display_id"]), Convert.ToUInt32(Row["item_id"]), Convert.ToUInt32(Row["reward_level"])));
                }
            }

            RestackByFrontpage();
        }

        internal void RestackByFrontpage()
        {
            CatalogPage fronpage = Pages[1];
            Dictionary<int, CatalogPage> restOfCata = new Dictionary<int, CatalogPage>(Pages);

            restOfCata.Remove(1);
            Pages.Clear();

            Pages.Add(fronpage.PageId, fronpage);

            foreach (KeyValuePair<int, CatalogPage> pair in restOfCata)
                Pages.Add(pair.Key, pair.Value);
        }

        internal void InitCache()
        {
            mCataIndexCache = new ServerMessage[10]; //Max 7 ranks

            for (int i = 1; i < 10; i++)
            {
                mCataIndexCache[i] = SerializeIndexForCache(i);
            }

            foreach (CatalogPage Page in Pages.Values)
            {
                Page.InitMsg();
            }
        }

        internal CatalogItem FindItem(uint ItemId)
        {
            foreach (CatalogPage Page in Pages.Values)
            {
                if (Page.Items.ContainsKey(ItemId))
                    return (CatalogItem)Page.Items[ItemId];
            }

            return null;
        }

        //internal Boolean IsItemInCatalog(uint BaseId)
        //{
        //    DataRow Row = null;

        //    using (DatabaseClient dbClient = FirewindEnvironment.GetDatabase().GetClient())
        //    {
        //        Row = dbClient.getRow("SELECT id FROM catalog_items WHERE item_ids = '" + BaseId + "' LIMIT 1");
        //    }

        //    if (Row != null)
        //    {
        //        return true;
        //    }

        //    return false;
        //}

        private static bool CanSerializeInIndex(CatalogPage page, int rank)
        {
            return page.Visible && page.MinRank <= rank;
        }

        internal int GetTreeSize(int rank, int TreeId)
        {
            int i = 0;

            foreach (CatalogPage Page in Pages.Values)
            {
                if (!CanSerializeInIndex(Page, rank))
                {
                    continue;
                }

                if (Page.ParentId == TreeId)
                {
                    i++;
                }
            }


            return i;
        }

        private void SerializeIndexTree(CatalogPage Page, int rank, ServerMessage Index, HashSet<int> serializedPages)
        {
            if (!serializedPages.Add(Page.PageId))
            {
                return;
            }

            Page.Serialize(rank, Index);

            foreach (CatalogPage childPage in Pages.Values)
            {
                if (childPage.ParentId != Page.PageId || !CanSerializeInIndex(childPage, rank))
                {
                    continue;
                }

                if (serializedPages.Add(childPage.PageId))
                    childPage.Serialize(rank, Index);
            }
        }

        internal CatalogPage GetPage(int Page)
        {
            if (!Pages.ContainsKey(Page))
            {
                return null;
            }

            return Pages[Page];
        }

        internal void HandlePurchase(GameClient Session, int PageId, uint ItemId, string extraParameter, int buyAmount, Boolean IsGift, string GiftUser, string GiftMessage, int GiftSpriteId, int GiftLazo, int GiftColor, bool giftShowIdentity)
        {
            int finalAmount = buyAmount;
            if (buyAmount > 5) // Possible discount!
            {
                // Nearest number that increases the amount of free items
                int nearestDiscount = ((int)Math.Floor(buyAmount / 6.0) * 6);

                // How many free ones we get
                int freeItemsCount = (nearestDiscount - 3) / 3;

                // Add 1 free if more than 42
                if (buyAmount >= 42)
                    freeItemsCount++;

                // Doesn't follow rules as it isn't dividable by 6, but still increases free items
                if (buyAmount >= 99)
                {
                    freeItemsCount = 33;
                }

                // This is how many we pay for in the end
                finalAmount = buyAmount - freeItemsCount;
            }

            //Logging.WriteLine("Amount: " + priceAmount + "; withOffer= " + finalAmount);
            CatalogPage Page;
            if (!Pages.TryGetValue(PageId, out Page))
                return;
            if (Page == null || !Page.Enabled || !Page.Visible || Session == null || Session.GetHabbo() == null)
            {
                Session.SendMessage(new ServerMessage(Outgoing.PurchaseError)); 
                return;
            }
            if (Page.ClubOnly && !Session.GetHabbo().GetSubscriptionManager().HasSubscription("habbo_club") && !Session.GetHabbo().GetSubscriptionManager().HasSubscription("habbo_vip"))
            {
                Session.SendMessage(new ServerMessage(Outgoing.PurchaseError));
                return;
            }
            if (Page.MinRank > Session.GetHabbo().Rank)
            {
                Session.SendMessage(new ServerMessage(Outgoing.PurchaseError));
                return;
            }
            CatalogItem Item = Page.GetItem(ItemId);

            if (Item == null) // TODO: Check item minimum club rank
            {
                Session.SendMessage(new ServerMessage(Outgoing.PurchaseError));
                return;
            }
            if (!Item.HaveOffer && buyAmount > 1) // Check if somebody is bulk-buying when not allowed
            {
                Session.SendMessage(new ServerMessage(Outgoing.PurchaseError));
                return;
            }
            if (Item.IsLimited && Item.LimitedStack <= Item.LimitedSelled)
            {
                Session.SendMessage(new ServerMessage(Outgoing.PurchaseError));
                return;
            }
            if (Item.ContainsBotProduct() && !RentableBotsEnabled())
            {
                Session.SendMessage(new ServerMessage(Outgoing.PurchaseError));
                return;
            }

            uint GiftUserId = 0;
            //int giftWrappingCost = 0;
            if (IsGift)
            {
                if(!Item.AllowGift)
                {
                    Session.SendMessage(new ServerMessage(Outgoing.PurchaseError));
                    return;
                }
                if(Item.Items.Count > 1 || Item.Amount > 1) // Gifts can only have 1 item?
                {
                    Session.SendMessage(new ServerMessage(Outgoing.PurchaseError));
                    return;
                }

                DataRow dRow;
                using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                {
                    dbClient.setQuery("SELECT id FROM users WHERE username = @gift_user");
                    dbClient.addParameter("gift_user", GiftUser);


                    dRow = dbClient.getRow();
                }

                if (dRow == null)
                {
                    Session.GetMessageHandler().GetResponse().Init(Outgoing.GiftError);
                    Session.GetMessageHandler().GetResponse().AppendString(GiftUser);
                    Session.GetMessageHandler().SendResponse();

                    return;
                }

                GiftUserId = Convert.ToUInt32(dRow[0]);

                if (GiftUserId == 0)
                {
                    Session.GetMessageHandler().GetResponse().Init(Outgoing.GiftError);
                    Session.GetMessageHandler().GetResponse().AppendString(GiftUser);
                    Session.GetMessageHandler().SendResponse();

                    return;
                }
            }

            Boolean CreditsError = false;
            Boolean PixelError = false;

            if (Session.GetHabbo().Credits < (Item.CreditsCost * finalAmount))
            {
                CreditsError = true;
            }

            if (Session.GetHabbo().ActivityPoints < (Item.PixelsCost * finalAmount))
            {
                PixelError = true;
            }

            if (CreditsError || PixelError)
            {
                ServerMessage message = new ServerMessage(Outgoing.NotEnoughBalance);
                message.AppendBoolean(CreditsError);
                message.AppendBoolean(PixelError);
                Session.SendMessage(message);
                return;
            }


            if (Item.CrystalCost > 0)
            {
                int cost = Item.CrystalCost * finalAmount;
                if (Session.GetHabbo().VipPoints < cost)
                {
                    Session.SendNotif("You can't afford that item!");
                    Session.SendMessage(new ServerMessage(Outgoing.PurchaseError));
                    return;
                }

                Session.GetHabbo().VipPoints -= cost;
                Session.GetHabbo().UpdateActivityPointsBalance(true);

                using (IQueryAdapter adapter = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                {
                    adapter.runFastQuery("UPDATE users SET vip_points = " + Session.GetHabbo().VipPoints + " WHERE id = " + Session.GetHabbo().Id);
                }                               
  
            }

            if (Item.CreditsCost > 0 && !IsGift)
            {
                Session.GetHabbo().Credits -= (Item.CreditsCost * finalAmount);
                Session.GetHabbo().UpdateCreditsBalance();
            }

            if (Item.PixelsCost > 0 && !IsGift)
            {
                Session.GetHabbo().ActivityPoints -= (Item.PixelsCost * finalAmount);
                Session.GetHabbo().UpdateActivityPointsBalance(true);
            }

            // Item is purchased, now do post-proccessing
            if (Item.IsLimited)
            {
                Item.LimitedSelled++;
                using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                {
                    dbClient.runFastQuery("UPDATE catalog_items SET limited_sells = " + Item.LimitedSelled + " WHERE id = " + Item.Id);
                }
                Page.InitMsg(); // update page!

                // send update
                Session.SendMessage(Page.GetMessage);
            }

            foreach (uint i in Item.Items)
            {
                if (Item.IsBotProduct(i))
                {
                    RoomBot bot = CreateCatalogBot(Session, Item, extraParameter);
                    if (bot == null || bot.BotId == 0)
                    {
                        Session.SendMessage(new ServerMessage(Outgoing.PurchaseError));
                        return;
                    }

                    Session.GetMessageHandler().GetResponse().Init(Outgoing.PurchaseOK);
                    Item.Serialize(Session.GetMessageHandler().GetResponse());
                    Session.GetMessageHandler().SendResponse();

                    Session.GetHabbo().GetInventoryComponent().AddBot(bot);
                    Session.SendMessage(Session.GetHabbo().GetInventoryComponent().SerializeBotInventory());
                    continue;
                }

                Item baseItem = Item.GetBaseItem(i);
                if (baseItem == null)
                {
                    Session.SendMessage(new ServerMessage(Outgoing.PurchaseError));
                    return;
                }

                //Logging.WriteLine(Item.GetBaseItem().ItemId);
                //Logging.WriteLine(Item.GetBaseItem().InteractionType.ToLower());
                // Extra Data is _NOT_ filtered at this point and MUST BE VERIFIED BELOW:
                if (baseItem.Type == 'h') // Subscription
                {
                    int Months = 0;
                    int Days = 0;
                    if (Item.Name.Contains("HABBO_CLUB_VIP_"))
                    {
                        if (Item.Name.Contains("_DAY"))
                        {
                            Days = int.Parse(Item.Name.Split('_')[3]);
                        }
                        else if (Item.Name.Contains("_MONTH"))
                        {
                            Months = int.Parse(Item.Name.Split('_')[3]);
                            Days = 31 * Months;
                        }
                    }
                    else if (Item.Name.Equals("deal_vip_1_year_and_badge"))
                    {
                        Months = 12;
                        Days = 31 * Months;
                    }
                    else if (Item.Name.Equals("HABBO_CLUB_VIP_5_YEAR"))
                    {
                        Months = 5 * 12;
                        Days = 31 * Months;
                    }
                    else if (Item.Name.StartsWith("DEAL_HC_"))
                    {
                        Months = int.Parse(Item.Name.Split('_')[2]);
                        Days = 31 * Months;

                        Session.GetHabbo().GetSubscriptionManager().AddOrExtendSubscription("habbo_club", Days * 24 * 3600);
                        Session.GetHabbo().SerializeClub();
                        return;
                    }

                    Session.GetHabbo().GetSubscriptionManager().AddOrExtendSubscription("habbo_vip", Days * 24 * 3600);
                    Session.GetHabbo().SerializeClub();
                    return;
                }



                if (IsGift && baseItem.Type == 'e')
                {
                    Session.SendNotif(LanguageLocale.GetValue("catalog.gift.send.error"));
                    return;
                }
                string purchaseExtraParameter = string.IsNullOrEmpty(extraParameter) ? Item.GetPurchaseExtraData(baseItem) : extraParameter;
                IRoomItemData itemData = new StringData(purchaseExtraParameter);
                switch (baseItem.InteractionType)
                {
                    case InteractionType.none:
                        //itemData = new StringData(extraParameter);
                        break;

                    case InteractionType.musicdisc:
                        itemData = new StringData(Item.songID.ToString());
                        break;

                    #region Pet handling
                    case InteractionType.pet:
                        try
                        {
                            string[] Bits = extraParameter.Split('\n');
                            string PetName = Bits[0];
                            string Race = Bits[1];
                            string Color = Bits[2];

                            int.Parse(Race); // to trigger any possible errors

                            if (!CheckPetName(PetName))
                            {
                                Session.SendMessage(new ServerMessage(Outgoing.PurchaseError));
                                return;
                            }

                            //if (Race.Length != 1)
                            //    return;

                            if (Color.Length != 6)
                            {
                                Session.SendMessage(new ServerMessage(Outgoing.PurchaseError));
                                return;
                            }
                        }
                        catch (Exception e)
                        {
                            Logging.WriteLine(e.ToString());
                            Logging.HandleException(e, "Catalog.HandlePurchase");
                            Session.SendMessage(new ServerMessage(Outgoing.PurchaseError));
                            return;
                        }

                        break;

                    #endregion

                    case InteractionType.roomeffect:

                        Double Number = 0;

                        try
                        {
                            if (string.IsNullOrEmpty(purchaseExtraParameter))
                                Number = 0;
                            else
                                Number = Double.Parse(purchaseExtraParameter, FirewindEnvironment.cultureInfo);
                        }
                        catch (Exception e) { Logging.HandleException(e, "Catalog.HandlePurchase: " + purchaseExtraParameter); }

                        itemData = new StringData(Number.ToString().Replace(',', '.'));
                        break; // maintain extra data // todo: validate

                    case InteractionType.postit:
                        itemData = new StringData("FFFF33");
                        break;

                    case InteractionType.dimmer:
                        itemData = new StringData("1,1,1,#000000,255");
                        break;

                    case InteractionType.trophy:
                        itemData = new StringData(String.Format("{0}\t{1}\t{2}", Session.GetHabbo().Username, DateTime.Now.ToString("d-M-yyy"), extraParameter));
                        break;

                    case InteractionType.guildgeneric:
                    case InteractionType.guilddoor:
                        itemData = BuildGroupItemData(Session, extraParameter, baseItem.InteractionType == InteractionType.guilddoor);
                        if (itemData == null)
                        {
                            Session.SendMessage(new ServerMessage(Outgoing.PurchaseError));
                            return;
                        }
                        break;

                    //case InteractionType.mannequin:
                    //    MapStuffData data = new MapStuffData();
                    //    data.Data.Add("OUTFIT_NAME", "");
                    //    data.Data.Add("FIGURE", "");
                    //    data.Data.Add("GENDER", "");
                    //    itemData = data;
                    //    break;
                    default:
                        //itemData = new StringData(extraParameter);
                        break;
                }

                //Session.GetMessageHandler().GetResponse().Init(Outgoing.UpdateInventary);
                //Session.GetMessageHandler().SendResponse();

                Session.GetMessageHandler().GetResponse().Init(Outgoing.PurchaseOK); // PurchaseOKMessageEvent
                Item.Serialize(Session.GetMessageHandler().GetResponse());
                Session.GetMessageHandler().SendResponse();

                if (IsGift)
                {
                    uint itemID;
                    //uint GenId = GenerateItemId();
                    Item Present = FirewindEnvironment.GetGame().GetItemManager().GetItemBySpriteID(GiftSpriteId);
                    if (Present == null)
                    {
                        Logging.LogDebug(string.Format("Somebody tried to purchase a present with invalid sprite ID: {0}", GiftSpriteId));
                        Session.SendMessage(new ServerMessage(Outgoing.PurchaseError));
                        return;
                    }

                    MapStuffData giftData = new MapStuffData();

                    if (giftShowIdentity)
                    {
                        giftData.Data.Add("PURCHASER_NAME", Session.GetHabbo().Username);
                        giftData.Data.Add("PURCHASER_FIGURE", Session.GetHabbo().Look);
                    }
                    giftData.Data.Add("MESSAGE", GiftMessage);
                    giftData.Data.Add("PRODUCT_CODE", "10");
                    giftData.Data.Add("EXTRA_PARAM", "test");
                    giftData.Data.Add("state", "1");

                    using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                    {
                        dbClient.setQuery("INSERT INTO items (base_id) VALUES (" + Present.ItemId + ")");
                        itemID = (uint)dbClient.insertQuery();

                        dbClient.runFastQuery("INSERT INTO items_users VALUES (" + itemID + "," + GiftUserId + ")");

                        if (!string.IsNullOrEmpty(GiftMessage))
                        {
                            dbClient.setQuery("INSERT INTO items_extradata VALUES (" + itemID + ",@datatype,@data,@extra)");
                            dbClient.addParameter("datatype", giftData.GetTypeID());
                            dbClient.addParameter("data", giftData.ToString());
                            dbClient.addParameter("extra", GiftColor * 1000 + GiftLazo);
                            dbClient.runQuery();
                        }

                        dbClient.setQuery("INSERT INTO user_presents (item_id,base_id,amount,extra_data) VALUES (" + itemID + "," + Item.GetBaseItem(i).ItemId + "," + Item.Amount + ",@extra_data)");
                        dbClient.addParameter("extra_data", itemData.ToString());
                        dbClient.runQuery();
                    }

                    GameClient Receiver = FirewindEnvironment.GetGame().GetClientManager().GetClientByUserID(GiftUserId);

                    if (Receiver != null)
                    {
                        Receiver.SendNotif(LanguageLocale.GetValue("catalog.gift.received") + Session.GetHabbo().Username);
                        UserItem u = Receiver.GetHabbo().GetInventoryComponent().AddNewItem(itemID, Present.ItemId, giftData, GiftColor * 1000 + GiftLazo, false, false, 0);
                        Receiver.GetHabbo().GetInventoryComponent().SendFloorInventoryUpdate();
                        Receiver.GetMessageHandler().GetResponse().Init(Outgoing.UnseenItems);
                        Receiver.GetMessageHandler().GetResponse().AppendInt32(1); // items
                        Receiver.GetMessageHandler().GetResponse().AppendInt32(1); // type (gift) == s
                        Receiver.GetMessageHandler().GetResponse().AppendInt32(1);
                        Receiver.GetMessageHandler().GetResponse().AppendUInt(u.Id);
                        Receiver.GetMessageHandler().SendResponse();
                        InventoryComponent targetInventory = Receiver.GetHabbo().GetInventoryComponent();
                        if (targetInventory != null)
                            targetInventory.RunDBUpdate();
                    }

                    Session.SendNotif(LanguageLocale.GetValue("catalog.gift.sent"));
                }
                else
                {
                    List<UserItem> items = DeliverItems(Session, Item.GetBaseItem(i), (buyAmount * Item.Amount), itemData.ToString(), Item.songID);
                    int Type = 2;
                    if (Item.GetBaseItem(i).Type.ToString().ToLower().Equals("s"))
                    {
                        if (Item.GetBaseItem(i).InteractionType == InteractionType.pet)
                            Type = 3;
                        else
                            Type = 1;
                    }

                    Session.GetMessageHandler().GetResponse().Init(Outgoing.UnseenItems);
                    Session.GetMessageHandler().GetResponse().AppendInt32(1); // items
                    Session.GetMessageHandler().GetResponse().AppendInt32(Type);

                    Session.GetMessageHandler().GetResponse().AppendInt32(items.Count);
                    foreach (UserItem u in items)
                        Session.GetMessageHandler().GetResponse().AppendUInt(u.Id);

                    Session.GetMessageHandler().SendResponse();

                    //Logging.WriteLine("Purchased " + items.Count);
                    Session.GetHabbo().GetInventoryComponent().UpdateItems(false);

                    if (Item.GetBaseItem(i).InteractionType == InteractionType.pet)
                    {
                        Session.SendMessage(Session.GetHabbo().GetInventoryComponent().SerializePetInventory());
                    }

                }
            }
        }

        internal void PurchaseGift(GameClient session, int pageID, int offerID, string extraParameter, string recipient, string message, int wrapSpriteID, int strappingID, int color, bool showIdentity)
        {
            // check if it's a special gift, or just the classic free one
            bool isBasicGift = (wrapSpriteID == 0 && strappingID == 0 && color == 0);


        }

        internal static bool CheckPetName(string PetName)
        {
            if (PetName.Length < 1 || PetName.Length > 16)
            {
                return false;
            }

            if (!FirewindEnvironment.IsValidAlphaNumeric(PetName))
            {
                return false;
            }

            return true;
        }

        internal static RoomBot GenerateBotFromRow(DataRow Row)
        {
            if (Row == null)
                return null;

            List<RandomSpeech> randomSpeech = new List<RandomSpeech>();
            List<BotResponse> botResponses = new List<BotResponse>();

            uint botId = Convert.ToUInt32(Row["id"]);
            uint ownerId = Row.Table.Columns.Contains("user_id") ? Convert.ToUInt32(Row["user_id"]) : 0;
            uint roomId = Row.Table.Columns.Contains("room_id") ? Convert.ToUInt32(Row["room_id"]) : 0;
            string name = GetBotString(Row, "name", "Jon");
            string motto = GetBotString(Row, "motto", string.Empty);
            string figure = GetBotString(Row, "look", string.Empty);
            if (string.IsNullOrEmpty(figure))
                figure = GetBotString(Row, "figure", CatalogItem.DefaultBotFigure);
            string gender = GetBotString(Row, "gender", "M").ToUpper();
            int x = GetBotInt(Row, "x");
            int y = GetBotInt(Row, "y");
            int z = GetBotInt(Row, "z");
            int rotation = GetBotInt(Row, "rotation");
            string walkingMode = GetBotString(Row, "walk_mode", "freeroam");
            string botType = GetBotString(Row, "bot_type", ownerId > 0 ? "rentable" : "generic");
            string ownerName = GetBotString(Row, "owner_name", string.Empty);
            int expireTimestamp = GetBotInt(Row, "expire_timestamp");
            int danceId = GetBotInt(Row, "dance_id");
            bool chatAuto = GetBotBool(Row, "chat_auto");
            bool chatRandom = GetBotBool(Row, "chat_random");
            int chatDelay = GetBotInt(Row, "chat_delay");
            List<string> chatLines = GetBotChatLines(Row);
            AIType aiType = botType.Equals("rentable", StringComparison.OrdinalIgnoreCase) ? AIType.Rentable : AIType.Generic;

            return new RoomBot(botId, roomId, aiType, walkingMode, name, motto, figure,
                x, y, z, rotation, 0, 0, 0, 0, ref randomSpeech, ref botResponses, ownerId, gender, ownerName, expireTimestamp, danceId, chatAuto, chatRandom, chatDelay, chatLines);
        }

        internal static RoomBot CreateBot(uint userId, string name, string look, string motto, string gender)
        {
            return CreateBot(userId, name, look, motto, gender, true, GetRentableBotDurationSeconds(), string.Empty);
        }

        internal static RoomBot CreateBot(uint userId, string name, string look, string motto, string gender, bool isRentable, int durationSeconds, string ownerName)
        {
            List<RandomSpeech> randomSpeech = new List<RandomSpeech>();
            List<BotResponse> botResponses = new List<BotResponse>();
            int expireTimestamp = (isRentable && durationSeconds > 0) ? FirewindEnvironment.GetUnixTimestamp() + durationSeconds : 0;
            AIType aiType = isRentable ? AIType.Rentable : AIType.Generic;
            RoomBot bot = new RoomBot(0, 0, aiType, "freeroam", name, motto, look, 0, 0, 0, 0, 0, 0, 0, 0,
                ref randomSpeech, ref botResponses, userId, gender, ownerName, expireTimestamp);

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("INSERT INTO user_bots (user_id,name,gender,figure,motto,room_id,walk_mode,bot_type,expire_timestamp,dance_id,chat_auto,chat_random,chat_delay,chat_lines) VALUES (@user_id,@name,@gender,@figure,@motto,0,'freeroam',@bot_type,@expire_timestamp,0,'0','0',7,'')");
                dbClient.addParameter("user_id", userId);
                dbClient.addParameter("name", name);
                dbClient.addParameter("gender", gender);
                dbClient.addParameter("figure", look);
                dbClient.addParameter("motto", motto);
                dbClient.addParameter("bot_type", isRentable ? "rentable" : "generic");
                dbClient.addParameter("expire_timestamp", expireTimestamp);
                bot.BotId = (uint)dbClient.insertQuery();
            }

            return bot;
        }

        private static RoomBot CreateCatalogBot(GameClient session, CatalogItem item, string extraParameter)
        {
            Dictionary<string, string> botData = ParseBotPurchaseData(extraParameter);
            string name = CleanBotString(GetBotPurchaseValue(botData, "name", GetConfigEntry("catalog.rentablebots.default.name", "Jon")), 32, "Jon");
            string motto = CleanBotString(GetBotPurchaseValue(botData, "motto", GetConfigEntry("catalog.rentablebots.default.motto", string.Empty)), 120, string.Empty);
            string look = CleanBotString(GetBotPurchaseValue(botData, "figure", GetConfigEntry("catalog.rentablebots.default.figure", CatalogItem.DefaultBotFigure)), 255, CatalogItem.DefaultBotFigure);
            string gender = CleanBotGender(GetBotPurchaseValue(botData, "gender", GetConfigEntry("catalog.rentablebots.default.gender", "M")));

            if (string.IsNullOrEmpty(look))
                look = CatalogItem.DefaultBotFigure;

            look = FirewindEnvironment.FilterFigure(look);

            return CreateBot(session.GetHabbo().Id, name, look, motto, gender, true, GetRentableBotDurationSeconds(), session.GetHabbo().Username);
        }

        internal static void SaveUserBotSettings(RoomBot bot)
        {
            if (bot == null || bot.BotId == 0)
                return;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                string chatLines = bot.ChatLines == null ? string.Empty : string.Join("\r", bot.ChatLines.ToArray());
                dbClient.setQuery("UPDATE user_bots SET name = @name, motto = @motto, gender = @gender, figure = @figure, walk_mode = @walk_mode, dance_id = @dance_id, chat_auto = @chat_auto, chat_random = @chat_random, chat_delay = @chat_delay, chat_lines = @chat_lines WHERE id = @id LIMIT 1");
                dbClient.addParameter("name", bot.Name);
                dbClient.addParameter("motto", bot.Motto);
                dbClient.addParameter("gender", bot.Gender);
                dbClient.addParameter("figure", bot.Look);
                dbClient.addParameter("walk_mode", bot.WalkingMode);
                dbClient.addParameter("dance_id", bot.DanceId);
                dbClient.addParameter("chat_auto", bot.ChatAuto ? "1" : "0");
                dbClient.addParameter("chat_random", bot.ChatRandom ? "1" : "0");
                dbClient.addParameter("chat_delay", bot.ChatDelay);
                dbClient.addParameter("chat_lines", chatLines);
                dbClient.addParameter("id", bot.BotId);
                dbClient.runQuery();
            }
        }

        internal static void DeleteExpiredRentableBots(IQueryAdapter dbClient)
        {
            dbClient.runFastQuery("DELETE FROM user_bots WHERE bot_type = 'rentable' AND expire_timestamp > 0 AND expire_timestamp <= " + FirewindEnvironment.GetUnixTimestamp());
        }

        internal static void DeleteUserBot(uint botId)
        {
            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.runFastQuery("DELETE FROM user_bots WHERE id = " + botId + " LIMIT 1");
            }
        }

        internal static int GetRentableBotDurationSeconds()
        {
            int duration = GetConfigInt("catalog.rentablebots.default.duration", 604800);
            return Math.Max(0, duration);
        }

        private static bool RentableBotsEnabled()
        {
            string enabled = GetConfigEntry("catalog.rentablebots.enabled", "true").ToLower();
            return enabled == "true" || enabled == "1" || enabled == "yes";
        }

        private static string GetConfigEntry(string key, string defaultValue)
        {
            return FirewindEnvironment.GetConfig().GetEntry(key, defaultValue);
        }

        private static int GetConfigInt(string key, int defaultValue)
        {
            int value;
            if (!int.TryParse(GetConfigEntry(key, defaultValue.ToString()), out value))
                return defaultValue;

            return value;
        }

        private static Dictionary<string, string> ParseBotPurchaseData(string extraParameter)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(extraParameter))
                return result;

            if (extraParameter.IndexOf(':') == -1 && extraParameter.IndexOf('=') == -1 && extraParameter.IndexOf(';') == -1)
            {
                result["name"] = extraParameter;
                return result;
            }

            foreach (string rawPart in extraParameter.Split(';'))
            {
                string part = rawPart.Trim();
                if (part.Length == 0)
                    continue;

                int separator = part.IndexOf(':');
                if (separator == -1)
                    separator = part.IndexOf('=');

                if (separator <= 0 || separator >= part.Length - 1)
                    continue;

                result[part.Substring(0, separator).Trim()] = part.Substring(separator + 1).Trim();
            }

            return result;
        }

        private static string GetBotPurchaseValue(Dictionary<string, string> botData, string key, string defaultValue)
        {
            if (botData.ContainsKey(key))
                return botData[key];

            if (key == "name" && botData.ContainsKey("bot_name"))
                return botData["bot_name"];

            if (key == "figure" && botData.ContainsKey("look"))
                return botData["look"];

            if (key == "gender" && botData.ContainsKey("sex"))
                return botData["sex"];

            return defaultValue;
        }

        private static string CleanBotString(string value, int maxLength, string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            value = value.Trim();
            if (value.Length > maxLength)
                value = value.Substring(0, maxLength);

            return value;
        }

        private static string CleanBotGender(string gender)
        {
            if (!string.IsNullOrEmpty(gender) && gender.StartsWith("F", StringComparison.OrdinalIgnoreCase))
                return "F";

            return "M";
        }

        private static string GetBotString(DataRow row, string column, string defaultValue)
        {
            if (!row.Table.Columns.Contains(column) || row[column] == DBNull.Value)
                return defaultValue;

            return row[column].ToString();
        }

        private static int GetBotInt(DataRow row, string column)
        {
            if (!row.Table.Columns.Contains(column) || row[column] == DBNull.Value)
                return 0;

            return Convert.ToInt32(row[column]);
        }

        private static bool GetBotBool(DataRow row, string column)
        {
            string value = GetBotString(row, column, "0").ToLower();
            return value == "1" || value == "true" || value == "yes";
        }

        private static List<string> GetBotChatLines(DataRow row)
        {
            List<string> result = new List<string>();
            string rawLines = GetBotString(row, "chat_lines", string.Empty);
            if (string.IsNullOrEmpty(rawLines))
                return result;

            foreach (string line in rawLines.Split('\r'))
            {
                string cleanedLine = line.Trim();
                if (cleanedLine.Length > 0)
                    result.Add(cleanedLine);
            }

            return result;
        }

        private static IRoomItemData BuildGroupItemData(GameClient Session, string groupData, bool isDoor)
        {
            int groupId;
            if (!int.TryParse(groupData, out groupId) || groupId <= 0)
                groupId = Session.GetHabbo().FavouriteGroup;

            Group group = FirewindEnvironment.GetGame().GetGroupManager().GetGroup(groupId);
            if (group == null || !group.Members.Contains(Session.GetHabbo().Id))
                return null;

            StringArrayStuffData data = new StringArrayStuffData();
            data.Data = new List<string>();
            data.Data.Add(isDoor ? "0" : string.Empty);
            data.Data.Add(group.ID.ToString());
            data.Data.Add(group.BadgeCode);
            data.Data.Add(group.Color1);
            data.Data.Add(group.Color2);
            return data;
        }

        internal List<UserItem> DeliverItems(GameClient Session, Item Item, int Amount, String ExtraData, uint songID = 0)
        {
            List<UserItem> result = new List<UserItem>();
            switch (Item.Type.ToString())
            {
                case "i":
                case "s":
                    for (int i = 0; i < Amount; i++)
                    {
                        //uint GeneratedId = GenerateItemId();
                        switch (Item.InteractionType)
                        {
                            case InteractionType.pet:

                                //int petType = int.Parse(Item.InteractionType.ToString().Replace("pet", ""));
                                int petType = int.Parse(Item.Name.Substring(Item.Name.IndexOf(' ') + 4));
                                string[] PetData = ExtraData.Split('\n');

                                Pet GeneratedPet = CreatePet(Session.GetHabbo().Id, PetData[0], petType, PetData[1], PetData[2]);

                                Session.GetHabbo().GetInventoryComponent().AddPet(GeneratedPet);
                                result.Add(Session.GetHabbo().GetInventoryComponent().AddNewItem(0, 320, new StringData("0"), 0, true, false, 0));

                                break;

                            case InteractionType.teleport:

                                UserItem one = Session.GetHabbo().GetInventoryComponent().AddNewItem(0, Item.ItemId, new StringData("0"), 0, true, false, 0);
                                uint idOne = one.Id;
                                UserItem two = Session.GetHabbo().GetInventoryComponent().AddNewItem(0, Item.ItemId, new StringData("0"), 0, true, false, 0);
                                uint idTwo = two.Id;
                                result.Add(one);
                                result.Add(two);

                                using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                                {
                                    dbClient.runFastQuery("INSERT INTO items_tele_links (tele_one_id,tele_two_id) VALUES (" + idOne + "," + idTwo + ")");
                                    dbClient.runFastQuery("INSERT INTO items_tele_links (tele_one_id,tele_two_id) VALUES (" + idTwo + "," + idOne + ")");
                                }

                                break;

                            case InteractionType.dimmer:

                                UserItem it = Session.GetHabbo().GetInventoryComponent().AddNewItem(0, Item.ItemId, new StringData(ExtraData), 0, true, false, 0);
                                uint id = it.Id;
                                result.Add(it);
                                using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                                {
                                    dbClient.runFastQuery("INSERT INTO items_moodlight (item_id,enabled,current_preset,preset_one,preset_two,preset_three) VALUES (" + id + ",0,1,'#000000,255,0','#000000,255,0','#000000,255,0')");
                                }


                                break;

                            case InteractionType.musicdisc:
                                {
                                    result.Add(Session.GetHabbo().GetInventoryComponent().AddNewItem(0, Item.ItemId, new StringData(songID.ToString()), 0, true, false, songID));
                                    break;
                                }
                            case InteractionType.mannequin:
                                MapStuffData data = new MapStuffData();
                                data.Data.Add("OUTFIT_NAME", "");
                                data.Data.Add("FIGURE", "hr-515-33.hd-600-1.ch-635-70.lg-716-66-62.sh-735-68");
                                data.Data.Add("GENDER", "M");

                                result.Add(Session.GetHabbo().GetInventoryComponent().AddNewItem(0, Item.ItemId, data, 0, true, false, songID));
                                break;
                                
                            case InteractionType.guildgeneric:
                            case InteractionType.guilddoor:
                                StringArrayStuffData stringData = new StringArrayStuffData();
                                if (ExtraData.Contains(Convert.ToChar(1).ToString()))
                                {
                                    stringData.Parse(ExtraData);
                                }
                                else
                                {
                                    stringData = BuildGroupItemData(Session, ExtraData, Item.InteractionType == InteractionType.guilddoor) as StringArrayStuffData;
                                }

                                if (stringData == null)
                                    break;

                                result.Add(Session.GetHabbo().GetInventoryComponent().AddNewItem(0, Item.ItemId, stringData, 0, true, false, songID));
                                break;

                            default:

                                result.Add(Session.GetHabbo().GetInventoryComponent().AddNewItem(0, Item.ItemId, new StringData(ExtraData), 0, true, false, songID));
                                break;
                        }
                    }
                    return result;

                case "e":

                    for (int i = 0; i < Amount; i++)
                    {
                        Session.GetHabbo().GetAvatarEffectsInventoryComponent().AddEffect(Item.SpriteId, 3600);
                    }

                    return result;

                case "r": // Rentable bot

                    return result;

                default:

                    Session.SendNotif(LanguageLocale.GetValue("catalog.buyerror"));
                    return result;
            }
        }

        internal static Pet CreatePet(uint UserId, string Name, int Type, string Race, string Color)
        {
            Pet pet = new Pet(404, UserId, 0, Name, (uint)Type, Race, Color, 0, 100, 100, 0, FirewindEnvironment.GetUnixTimestamp(), 0, 0, 0.0, false);
            pet.DBState = DatabaseUpdateState.NeedsUpdate;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("INSERT INTO user_pets (user_id,name,type,race,color,expirience,energy,createstamp) VALUES (" + pet.OwnerId + ",@" + pet.PetId + "name," + pet.Type + ",@" + pet.PetId + "race,@" + pet.PetId + "color,0,100,'" + pet.CreationStamp + "')");
                dbClient.addParameter(pet.PetId + "name", pet.Name);
                dbClient.addParameter(pet.PetId + "race", pet.Race);
                dbClient.addParameter(pet.PetId + "color", pet.Color);
                pet.PetId = (uint)dbClient.insertQuery();
            }
            return pet;
        }

        internal static Pet GeneratePetFromRow(DataRow Row)
        {
            if (Row == null)
            {
                return null;
            }

            return new Pet(Convert.ToUInt32(Row["id"]), Convert.ToUInt32(Row["user_id"]), Convert.ToUInt32(Row["room_id"]), (string)Row["name"], Convert.ToUInt32(Row["type"]), (string)Row["race"], (string)Row["color"], (int)Row["expirience"], (int)Row["energy"], (int)Row["nutrition"], (int)Row["respect"], (double)Row["createstamp"], (int)Row["x"], (int)Row["y"], (double)Row["z"], (Convert.ToInt32(Row["have_saddle"])==1));
        }

        //internal Pet GeneratePetFromRow(DataRow Row, uint PetID)
        //{
        //    if (Row == null)
        //        return null;

        //    return new Pet(PetID, (uint)Row["user_id"], (uint)Row["room_id"], (string)Row["name"], (uint)Row["type"], (string)Row["race"], (string)Row["color"], (int)Row["expirience"], (int)Row["energy"], (int)Row["nutrition"], (int)Row["respect"], (double)Row["createstamp"], (int)Row["x"], (int)Row["y"], (double)Row["z"]);
        //}

        //internal uint GenerateItemId()
        //{
        //    //uint i = 0;

        //    //using (DatabaseClient dbClient = FirewindEnvironment.GetDatabase().GetClient())
        //    //{
        //    //    i = mCacheID++;
        //    //    dbClient.runFastQuery("UPDATE item_id_generator SET id_generator = '" + mCacheID + "' LIMIT 1");
        //    //}

        //    return mCacheID++;
        //}

        internal EcotronReward GetRandomEcotronReward()
        {
            uint Level = 1;

            if (FirewindEnvironment.GetRandomNumber(1, 2000) == 2000)
            {
                Level = 5;
            }
            else if (FirewindEnvironment.GetRandomNumber(1, 200) == 200)
            {
                Level = 4;
            }
            else if (FirewindEnvironment.GetRandomNumber(1, 40) == 40)
            {
                Level = 3;
            }
            else if (FirewindEnvironment.GetRandomNumber(1, 4) == 4)
            {
                Level = 2;
            }

            List<EcotronReward> PossibleRewards = GetEcotronRewardsForLevel(Level);

            if (PossibleRewards != null && PossibleRewards.Count >= 1)
            {
                return PossibleRewards[FirewindEnvironment.GetRandomNumber(0, (PossibleRewards.Count - 1))];
            }
            else
            {
                return new EcotronReward(0, 1479, 0); // eco lamp two :D
            }
        }

        internal List<EcotronReward> GetEcotronRewardsForLevel(uint Level)
        {
            List<EcotronReward> Rewards = new List<EcotronReward>();

            foreach (EcotronReward R in EcotronRewards)
            {
                if (R.RewardLevel == Level)
                {
                    Rewards.Add(R);
                }
            }


            return Rewards;
        }

        internal ServerMessage SerializeIndexForCache(int rank)
        {
            //ServerMessage Index = new ServerMessage(126);
            //Index.AppendBoolean(false);
            //Index.AppendInt32(0);
            //Index.AppendInt32(0);
            //Index.AppendInt32(-1);
            //Index.AppendString("");
            //Index.AppendBoolean(false);
            ServerMessage Index = new ServerMessage(Outgoing.OpenShop); //Fix for r61
            Index.AppendBoolean(true);
            Index.AppendInt32(0);
            Index.AppendInt32(0);
            Index.AppendInt32(-1);
            Index.AppendString("root");
            Index.AppendString("");
            Index.AppendInt32(GetTreeSize(rank, -1));

            HashSet<int> serializedPages = new HashSet<int>();
            foreach (CatalogPage Page in Pages.Values)
            {
                if (Page.ParentId != -1 || !CanSerializeInIndex(Page, rank))
                    continue;

                SerializeIndexTree(Page, rank, Index, serializedPages);
            }
            Index.AppendBoolean(false); // is updated
            return Index;
        }

        internal ServerMessage GetIndexMessageForRank(uint Rank)
        {
            if (Rank < 1)
                Rank = 1;
            if (Rank > 10)
                Rank = 10;

            return mCataIndexCache[Rank];
        }

        internal static ServerMessage SerializePage(CatalogPage Page)
        {
            ServerMessage PageData = new ServerMessage(Outgoing.OpenShopPage);
            PageData.AppendInt32(Page.PageId);

            switch (Page.Layout)
            {
                case "frontpage":

                    PageData.AppendString("frontpage3");
                    PageData.AppendInt32(2);
                    //for (int i = 0; i < 3; i++)
                    //{
                    //    PageData.AppendString("catalog_club_headline1");
                    //}
                    //PageData.AppendInt32(7);
                    //for (int i = 0; i < 7; i++)
                    //{
                    //    PageData.AppendString("#FEFEFE");
                    //}
                    PageData.AppendString("Bundles_ts");
                    PageData.AppendString("");
                    PageData.AppendInt32(11);
                    PageData.AppendString("");
                    PageData.AppendString("");
                    PageData.AppendString("");
                    PageData.AppendString("How to get Habbo Credits");
                    PageData.AppendString("You can get Habbo Credits via Prepaid Cards, Home Phone, Credit Card, Mobile, completing offers and more! " + Convert.ToChar(13) + Convert.ToChar(10) + Convert.ToChar(13) + Convert.ToChar(10) + "To redeem your Habbo Credits, enter your voucher code below.");
                    PageData.AppendString(Page.TextDetails);
                    PageData.AppendString("");
                    PageData.AppendString("#FEFEFE");
                    PageData.AppendString("#FEFEFE");
                    PageData.AppendString(LanguageLocale.GetValue("catalog.waystogetcredits"));
                    PageData.AppendString("credits");
                    break;

                case "recycler_info":

                    PageData.AppendString(Page.Layout);
                    PageData.AppendInt32(2);
                    PageData.AppendString(Page.LayoutHeadline);
                    PageData.AppendString(Page.LayoutTeaser);
                    PageData.AppendInt32(3);
                    PageData.AppendString(Page.Text1);
                    PageData.AppendString(Page.Text2);
                    PageData.AppendString(Page.TextDetails);

                    break;

                case "recycler_prizes":

                    // Ac@aArecycler_prizesIcatalog_recycler_headline3IDe Ecotron geeft altijd een van deze beloningen:H
                    PageData.AppendString("recycler_prizes");
                    PageData.AppendInt32(1);
                    PageData.AppendString("catalog_recycler_headline3");
                    PageData.AppendInt32(1);
                    PageData.AppendString(Page.Text1);

                    break;

                case "spaces_new":

                    PageData.AppendString(Page.Layout);
                    PageData.AppendInt32(1);
                    PageData.AppendString(Page.LayoutHeadline);
                    PageData.AppendInt32(1);
                    PageData.AppendString(Page.Text1);

                    break;

                case "recycler":

                    PageData.AppendString(Page.Layout);
                    PageData.AppendInt32(2);
                    PageData.AppendString(Page.LayoutHeadline);
                    PageData.AppendString(Page.LayoutTeaser);
                    PageData.AppendInt32(1);
                    PageData.AppendStringWithBreak(Page.Text1, 10);
                    PageData.AppendString(Page.Text2);
                    PageData.AppendString(Page.TextDetails);

                    break;

                case "trophies":

                    PageData.AppendString("trophies");
                    PageData.AppendInt32(1);
                    PageData.AppendString(Page.LayoutHeadline);
                    PageData.AppendInt32(2);
                    PageData.AppendString(Page.Text1);
                    PageData.AppendString(Page.TextDetails);

                    break;

                case "pets":

                    PageData.AppendString("pets");
                    PageData.AppendInt32(2);
                    PageData.AppendString(Page.LayoutHeadline);
                    PageData.AppendString(Page.LayoutTeaser);
                    PageData.AppendInt32(4);
                    PageData.AppendString(Page.Text1);
                    PageData.AppendString(LanguageLocale.GetValue("catalog.pickname"));
                    PageData.AppendString(LanguageLocale.GetValue("catalog.pickcolor"));
                    PageData.AppendString(LanguageLocale.GetValue("catalog.pickrace"));

                    break;

                case "soundmachine":

                    PageData.AppendString(Page.Layout);
                    PageData.AppendInt32(2);
                    PageData.AppendString(Page.LayoutHeadline);
                    PageData.AppendString(Page.LayoutTeaser);
                    PageData.AppendInt32(2);
                    PageData.AppendString(Page.Text1);
                    PageData.AppendString(Page.TextDetails);
                    break;

                case "club_buy":

                    PageData.AppendString("vip_buy"); // layout
                    PageData.AppendInt32(2);
                    PageData.AppendString("ctlg_buy_vip_header");
                    PageData.AppendString("ctlg_gift_vip_teaser");
                    PageData.AppendInt32(0);
                    break;

                case "guild_frontpage":
                    PageData.AppendString(Page.Layout);
                    PageData.AppendInt32(2);
                    PageData.AppendString("catalog_groups_en");
                    PageData.AppendString("");
                    PageData.AppendInt32(3);
                    PageData.AppendString(Page.LayoutTeaser);
                    PageData.AppendString(Page.LayoutSpecial);
                    PageData.AppendString(Page.Text1);
                    break;

                default:

                    PageData.AppendString(Page.Layout);
                    PageData.AppendInt32(3);
                    PageData.AppendString(Page.LayoutHeadline);
                    PageData.AppendString(Page.LayoutTeaser);
                    PageData.AppendString(Page.LayoutSpecial);
                    PageData.AppendInt32(3);
                    PageData.AppendString(Page.Text1);
                    PageData.AppendString(Page.TextDetails);
                    PageData.AppendString(Page.TextTeaser);

                    break;
            }

            if (!Page.Layout.Equals("frontpage") && !Page.Layout.Equals("club_buy"))
            {
                PageData.AppendInt32(Page.Items.Count);

                //if (Page.Layout == "trophies") // We have to order descending here!
                foreach (CatalogItem Item in Page.Items.Values)
                {
                    Item.Serialize(PageData);
                }
            }
            else
                PageData.AppendInt32(0);
            PageData.AppendInt32(-1);
            PageData.AppendBoolean(false);

            return PageData;
        }

        //internal ServerMessage SerializeTestIndex()
        //{
        //    ServerMessage Message = new ServerMessage(126);

        //    Message.AppendInt32(0);
        //    Message.AppendInt32(0);
        //    Message.AppendInt32(0);
        //    Message.AppendInt32(-1);
        //    Message.AppendString("");
        //    Message.AppendInt32(0);
        //    Message.AppendInt32(100);

        //    for (int i = 1; i <= 150; i++)
        //    {
        //        Message.AppendInt32(1);
        //        Message.AppendInt32(i);
        //        Message.AppendInt32(i);
        //        Message.AppendInt32(i);
        //        Message.AppendString("#" + i);
        //        Message.AppendInt32(0);
        //        Message.AppendInt32(0);
        //    }

        //    return Message;

        //   
        //}

        //internal VoucherHandler GetVoucherHandler()
        //{
        //    return VoucherHandler;
        //}

        internal Marketplace GetMarketplace()
        {
            return Marketplace;
        }
    }
}
