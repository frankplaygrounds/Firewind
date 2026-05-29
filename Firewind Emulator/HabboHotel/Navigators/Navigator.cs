using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Firewind.HabboHotel.GameClients;
using Firewind.HabboHotel.Rooms;
using Firewind.HabboHotel.Users.Messenger;
using Firewind.Messages;
using Database_Manager.Database.Session_Details.Interfaces;
using HabboEvents;

namespace Firewind.HabboHotel.Navigators
{
    class Navigator
    {
        //private Dictionary<int, string> PublicCategories;
        private Hashtable PrivateCategories;

        private Dictionary<Int32, PublicItem> PublicItems;

        internal Navigator()
        {
            //PublicCategories = new Dictionary<int, string>();
            PrivateCategories = new Hashtable();
            PublicItems = new Dictionary<int, PublicItem>();
            //PublicRecommended = new Dictionary<int, PublicItem>();
        }

        internal void Initialize(IQueryAdapter dbClient)
        {
            PrivateCategories.Clear();
            PublicItems.Clear();
            //dbClient.setQuery("SELECT id,caption FROM navigator_pubcats WHERE enabled = 2"); //wtf?
            //DataTable dPubCats = dbClient.getTable();

            dbClient.setQuery("SELECT id,caption,min_rank FROM navigator_flatcats WHERE enabled = 2");
            DataTable dPrivCats = dbClient.getTable();

            dbClient.setQuery("SELECT * FROM navigator_publics ORDER BY ordernum ASC");
            DataTable dPubItems = dbClient.getTable();

            //if (dPubCats != null)
            //{
            //    foreach (DataRow Row in dPubCats.Rows)
            //    {
            //        PublicCategories.Add((int)Row["id"], (string)Row["caption"]);
            //    }
            //}

            if (dPrivCats != null)
            {
                foreach (DataRow Row in dPrivCats.Rows)
                {
                    PrivateCategories.Add((int)Row["id"], new FlatCat((int)Row["id"], (string)Row["caption"], (int)Row["min_rank"]));
                }
            }

            if (dPubItems != null)
            {
                foreach (DataRow Row in dPubItems.Rows)
                {
                    PublicItems.Add((int)Row["id"], new PublicItem((int)Row["id"], int.Parse(Row["bannertype"].ToString()), (string)Row["caption"], (string)Row["description"],
                        (string)Row["image"], ((Row["image_type"].ToString().ToLower() == "internal") ? PublicImageType.INTERNAL : PublicImageType.EXTERNAL),
                        Convert.ToUInt32(Row["room_id"]), (int)Row["category_id"], (int)Row["category_parent_id"], FirewindEnvironment.EnumToBool(Row["recommended"].ToString()), (int)Row["typeofdata"], (string)Row["tag"]));
                }
            }

            //if (dPubRecommended != null)
            //{
            //    foreach (DataRow Row in dPubRecommended.Rows)
            //    {
            //        PublicRecommended.Add((int)Row["id"], new PublicItem((int)Row["id"], int.Parse(Row["bannertype"].ToString()), (string)Row["caption"],
            //            (string)Row["image"], ((Row["image_type"].ToString().ToLower() == "internal") ? PublicImageType.INTERNAL : PublicImageType.EXTERNAL),
            //            (uint)Row["room_id"], (int)Row["category_parent_id"], false));
            //    }
            //}
        }

        /** COMENTADO YA QUE SALAS PUBLICAS NUEVA CRYPTO NO NECESARIO
         * internal Int32 GetCountForParent(Int32 ParentId)
        {
            int i = 0;

            foreach (PublicItem Item in PublicItems.Values)
            {
                if (Item.ParentId == ParentId || ParentId == -1)
                {
                    i++;
                }
            }

            return i;
        }*/

        internal FlatCat GetFlatCat(Int32 Id)
        {
            if (PrivateCategories.ContainsKey(Id))
                return (FlatCat)PrivateCategories[Id];

            return null;
        }

        internal ServerMessage SerializeFlatCategories(GameClient Session)
        {
            ServerMessage Cats = new ServerMessage(Outgoing.FlatCats);
            Cats.AppendInt32(PrivateCategories.Count);

            foreach (FlatCat FlatCat in PrivateCategories.Values)
            {
                Cats.AppendInt32(FlatCat.Id);
                Cats.AppendString(FlatCat.Caption);
                Cats.AppendBoolean(FlatCat.MinRank <= 2);
            }

            return Cats;
        }

        internal ServerMessage SerializePublicRooms()
        {
            ServerMessage Frontpage = new ServerMessage(Outgoing.PublicCategories);
            Frontpage.AppendInt32(this.PublicItems.Count);
            foreach (PublicItem Pub in PublicItems.Values)
            {
                if (Pub.ParentId > 0)
                    continue;

                Pub.Serialize(Frontpage);

                if (Pub.itemType != PublicItemType.CATEGORY)
                    continue;

                foreach (PublicItem pub in PublicItems.Values)
                {
                    if (Pub.Id != pub.ParentId)
                        continue;

                    pub.Serialize(Frontpage);
                }
            }

            if (this.PublicItems.Count > 0)
            {
                Frontpage.AppendInt32(1); // FEATURED ROOM
                Random random = new Random();
                int randomINT = random.Next(0, this.PublicItems.Count - 1);
                this.PublicItems.ElementAt(randomINT).Value.Serialize(Frontpage);
            }
            else
            {
                Frontpage.AppendInt32(0);
            }
            Frontpage.AppendInt32(0);
            return Frontpage;
             

        }

        private void SerializeItemsFromCata(int Id, ServerMessage Message)
        {
            foreach (PublicItem Item in this.PublicItems.Values)
            {
                if (Item.ParentId == Id)
                {
                    Item.Serialize(Message);
                }
            }
        }

        internal ServerMessage SerializeFavoriteRooms(GameClient Session)
        {
            ServerMessage Rooms = new ServerMessage(Outgoing.NavigatorPacket);
            Rooms.AppendInt32(6);
            Rooms.AppendString("");
            Rooms.AppendInt32(Session.GetHabbo().FavoriteRooms.Count);

            RoomData room;
            foreach (uint Id in Session.GetHabbo().FavoriteRooms.ToArray())
            {
                room = FirewindEnvironment.GetGame().GetRoomManager().GenerateRoomData(Id);
                if (room != null)
                    room.Serialize(Rooms, false);
            }
            Rooms.AppendBoolean(false);
            return Rooms;
        }
       
        internal ServerMessage SerializeRecentRooms(GameClient Session)
        {
            //using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            //{
            //    dbClient.setQuery("SELECT rooms.id, rooms.caption, rooms.description, rooms.roomtype, rooms.owner, rooms.state, rooms.category, rooms.users_now, rooms.users_max, rooms.model_name, rooms.public_ccts, rooms.score, rooms.allow_pets, rooms.allow_pets_eat, rooms.allow_walkthrough, rooms.allow_hidewall, rooms.password, rooms.wallpaper, rooms.floor, rooms.landscape, rooms.icon_items, rooms.icon_bg, rooms.icon_fg, rooms.tags, rooms.allow_rightsoverride FROM rooms JOIN user_roomvisits ON rooms.id = user_roomvisits.room_id WHERE user_roomvisits.user_id = " + Session.GetHabbo().Id + " ORDER BY entry_timestamp DESC LIMIT 50;");
            //    DataTable Data = dbClient.getTable();

            //    List<RoomData> ValidRecentRooms = new List<RoomData>();
            //    List<uint> RoomsListed = new List<uint>();

            //    if (Data != null)
            //    {
            //        foreach (DataRow Row in Data.Rows)
            //        {

            //            RoomData tData = FirewindEnvironment.GetGame().GetRoomManager().FetchRoomData((uint)Row["id"], Row);
            //            tData.Fill(Row);
            //            ValidRecentRooms.Add(tData);
            //            RoomsListed.Add(tData.Id);
            //        }
            //    }

                ServerMessage Rooms = new ServerMessage(Outgoing.NavigatorPacket);
                Rooms.AppendInt32(7);
                Rooms.AppendString("");
                //Rooms.AppendInt32(ValidRecentRooms.Count);
                Rooms.AppendInt32(0);
                Rooms.AppendBoolean(false);

            //    foreach (RoomData _Data in ValidRecentRooms)
            //    {
            //        _Data.Serialize(Rooms, false);
            //    }

                return Rooms;
            //}
        }

        internal ServerMessage SerializeEventListing(int Category)
        {
            ServerMessage Message = new ServerMessage(451);
            Message.AppendInt32(Category);
            Message.AppendInt32(12);
            Message.AppendString("");

            KeyValuePair<RoomData, int>[] eventRooms = FirewindEnvironment.GetGame().GetRoomManager().GetEventRoomsForCategory(Category);
            Message.AppendInt32(eventRooms.Length);

            foreach (KeyValuePair<RoomData, int> pair in eventRooms)
            {
                pair.Key.Serialize(Message, true);
            }

            return Message;
        }

        internal ServerMessage SerializePopularRoomTags()
        {
            Dictionary<string, int> Tags = new Dictionary<string, int>();

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("SELECT rooms.tags, room_active.active_users FROM rooms LEFT JOIN room_active ON (room_active.roomid = rooms.id) WHERE active_users > 0 ORDER BY active_users DESC LIMIT 50");
                DataTable Data = dbClient.getTable();


                if (Data != null)
                {
                    int activeUsers;
                    foreach (DataRow Row in Data.Rows)
                    {
                        if (!string.IsNullOrEmpty(Row["active_users"].ToString()))
                            activeUsers = (int)Row["active_users"];
                        else
                            activeUsers = 0;

                        List<string> RoomTags = new List<string>();

                        foreach (string Tag in Row["tags"].ToString().Split(','))
                        {
                            RoomTags.Add(Tag);
                        }

                        foreach (string Tag in RoomTags)
                        {
                            if (Tags.ContainsKey(Tag))
                            {
                                Tags[Tag] += activeUsers;
                            }
                            else
                            {
                                Tags.Add(Tag, activeUsers);
                            }
                        }
                    }
                }

                List<KeyValuePair<string, int>> SortedTags = new List<KeyValuePair<string, int>>(Tags);

                SortedTags.Sort(

                    delegate(KeyValuePair<string, int> firstPair,

                    KeyValuePair<string, int> nextPair)
                    {
                        return firstPair.Value.CompareTo(nextPair.Value);
                    }

                );

                ServerMessage Message = new ServerMessage(Outgoing.PopularTags);
                Message.AppendInt32(SortedTags.Count);

                foreach (KeyValuePair<string, int> TagData in SortedTags)
                {
                    Message.AppendString(TagData.Key);
                    Message.AppendInt32(TagData.Value);
                }

                return Message;
            }
        }

        internal ServerMessage SerializeSearchResults(string SearchQuery)
        {
            bool OwnerOnly = SearchQuery.StartsWith("owner:");
            DataTable Data = new DataTable();

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                if (SearchQuery.Length > 0)
                {
                    if (OwnerOnly) // search for owner
                    {
                        dbClient.setQuery("SELECT rooms.*, room_active.active_users FROM rooms LEFT JOIN room_active ON (room_active.roomid = rooms.id) WHERE owner = @query " +
                                    "UNION ALL " +
                                    "SELECT rooms.*, room_active.active_users FROM rooms LEFT JOIN room_active ON (room_active.roomid = rooms.id) WHERE caption = @query " +
                                    "ORDER BY active_users DESC LIMIT 50");
                    }

                    dbClient.setQuery("SELECT rooms.*, room_active.active_users FROM rooms LEFT JOIN room_active ON (room_active.roomid = rooms.id) WHERE owner = @query " +
                                "UNION ALL " +
                                "SELECT rooms.*, room_active.active_users FROM rooms LEFT JOIN room_active ON (room_active.roomid = rooms.id) WHERE caption = @query " +
                                "ORDER BY active_users DESC LIMIT 50");
                    if (OwnerOnly)
                        dbClient.addParameter("query", SearchQuery.Substring(SearchQuery.IndexOf(':') + 1));
                    else
                        dbClient.addParameter("query", SearchQuery);
                    Data = dbClient.getTable();
                }
            }

            List<RoomData> Results = new List<RoomData>();

            if (Data != null)
            {
                foreach (DataRow Row in Data.Rows)
                {
                    RoomData RData = FirewindEnvironment.GetGame().GetRoomManager().FetchRoomData(Convert.ToUInt32(Row["id"]), Row);
                    Results.Add(RData);
                }
            }

            ServerMessage Message = new ServerMessage(Outgoing.NavigatorPacket);
            Message.AppendInt32(8);
            Message.AppendString(SearchQuery);
            Message.AppendInt32(Results.Count);

            foreach (RoomData Room in Results)
            {
                Room.Serialize(Message, false);
            }
            Message.AppendBoolean(false);
            return Message;
        }

        private ServerMessage SerializeActiveRooms(int category)
        {
            //Logging.WriteLine("get pages for category: " + category);
            ServerMessage reply = new ServerMessage(Outgoing.NavigatorPacket);
            reply.AppendInt32(1);
            reply.AppendString(category.ToString());

            KeyValuePair<RoomData, int>[] rooms = FirewindEnvironment.GetGame().GetRoomManager().GetActiveRooms();
            SerializeNavigatorPopularRooms(ref reply, rooms, category);

            Array.Clear(rooms, 0, rooms.Length);
            rooms = null;

            return reply;
        }

        internal ServerMessage SerializeNavigator(GameClient session, int mode)
        {
            if (mode >= 0)
                return SerializeActiveRooms(mode);

            ServerMessage reply = new ServerMessage(Outgoing.NavigatorPacket);

            switch (mode)
            {
                case -5:
                case -4:
                    {
                        reply.AppendInt32(mode * (-1));
                        
                        List<RoomData> activeFriends = session.GetHabbo().GetMessenger().GetActiveFriendsRooms().OrderByDescending(p => p.UsersNow).ToList();
                        SerializeNavigatorRooms(ref reply, activeFriends);

                        return reply;
                    }

                case -3:
                    {
                        reply.AppendInt32(5);
                        SerializeNavigatorRooms(ref reply, session.GetHabbo().UsersRooms);

                        return reply;
                    }

                case -2:
                    {
                        reply.AppendInt32(2);

                        KeyValuePair<RoomData, int>[] rooms = FirewindEnvironment.GetGame().GetRoomManager().GetVotedRooms();
                        SerializeNavigatorRooms(ref reply, rooms);

                        Array.Clear(rooms, 0, rooms.Length);
                        rooms = null;

                        return reply;
                    }

                case -1:
                    {
                        //Logging.WriteLine("Hallo! we need u for army!");
                        reply.AppendInt32(1);
                        reply.AppendString("-1");

                        KeyValuePair<RoomData, int>[] rooms = null;
                        try
                        {
                            rooms = FirewindEnvironment.GetGame().GetRoomManager().GetActiveRooms();

                            SerializeNavigatorPopularRooms(ref reply, rooms);

                            Array.Clear(rooms, 0, rooms.Length);
                            rooms = null;
                        }
                        catch
                        {
                            reply.AppendInt32(0);
                            reply.AppendBoolean(false);
                        }

                        return reply;
                    }
            }

            return reply;
        }

        private void SerializeNavigatorPopularRooms(ref ServerMessage reply, KeyValuePair<RoomData, int>[] rooms, int Category)
        {
            reply.AppendInt32(rooms.Length);
            int i = 0;
            foreach (KeyValuePair<RoomData, int> pair in rooms)
            {
                RoomData data = pair.Key;
                if (data.Category.Equals(Category))
                {
                    data.Serialize(reply, false);
                    i++;
                }
                else
                    continue;
            }
            //Logging.WriteLine("Rooms in this category: " + i);
            reply.setInt(i, 8 + Category.ToString().Length);
            reply.AppendBoolean(false);
        }

        private void SerializeNavigatorPopularRooms(ref ServerMessage reply, KeyValuePair<RoomData, int>[] rooms)
        {
            reply.AppendInt32(rooms.Length);

            foreach (KeyValuePair<RoomData, int> pair in rooms)
            {
                pair.Key.Serialize(reply, false);
            }
            reply.AppendBoolean(false);

            //if (PublicRecommended.Count > 0)
            //{
            //    reply.AppendString("");

            //    foreach (PublicItem Pub in PublicRecommended.Values)
            //    {
            //        Pub.Serialize(reply);
            //    }
            //}
        }

        
        private void SerializeNavigatorRooms(ref ServerMessage reply, List<RoomData> rooms)
        {
            reply.AppendString("");
            reply.AppendInt32(rooms.Count);

            foreach (RoomData data in rooms)
            {

                data.Serialize(reply, false);
            }
            reply.AppendBoolean(false);

            //if (PublicRecommended.Count > 0)
            //{
            //    reply.AppendString("");

            //    foreach (PublicItem Pub in PublicRecommended.Values)
            //    {
            //        Pub.Serialize(reply);
            //    }
            //}
        }

        private void SerializeNavigatorRooms(ref ServerMessage reply, KeyValuePair<RoomData, int>[] rooms)
        {
            reply.AppendString(string.Empty);            
            reply.AppendInt32(rooms.Length);

            foreach (KeyValuePair<RoomData,int> pair in rooms)
            {
                pair.Key.Serialize(reply, false);
            }
            reply.AppendBoolean(false);

            //if (PublicRecommended.Count > 0)
            //{
            //    reply.AppendString("");

            //    foreach (PublicItem Pub in PublicRecommended.Values)
            //    {
            //        Pub.Serialize(reply);
            //    }
            //}
        }
    }
}
