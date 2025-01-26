using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Firewind.Core;
using Firewind.HabboHotel.Catalogs;
using Firewind.HabboHotel.GameClients;
using Firewind.HabboHotel.Items;
using Firewind.HabboHotel.Pets;
using Firewind.Messages;
using System.Linq;
using Firewind.Collections;
using Database_Manager.Database.Session_Details.Interfaces;
using Firewind.HabboHotel.Users.UserDataManagement;
using Firewind.Util;
using HabboEvents;
using Firewind.HabboHotel.Rooms;


namespace Firewind.HabboHotel.Users.Inventory
{
    class InventoryComponent
    {
        private Hashtable floorItems;
        private Hashtable wallItems;
        private Hashtable discs;

        private SafeDictionary<UInt32, Pet> InventoryPets;
        private Hashtable mAddedItems;
        private ArrayList mRemovedItems;
        private GameClient mClient;

        internal uint UserId;
        internal int ItemCount
        {
            get
            {
                return floorItems.Count + wallItems.Count;
            }
        }

        internal InventoryComponent(uint UserId, GameClient Client, UserData UserData)
        {
            this.mClient = Client;
            this.UserId = UserId;
            this.floorItems = new Hashtable();
            this.wallItems = new Hashtable();
            this.discs = new Hashtable();

            foreach (UserItem item in UserData.inventory)
            {
                if (item.GetBaseItem().InteractionType == InteractionType.musicdisc)
                    discs.Add(item.Id, item);
                if (item.isWallItem)
                    wallItems.Add(item.Id, item);
                else
                    floorItems.Add(item.Id, item);
            }

            this.InventoryPets = new SafeDictionary<UInt32, Pet>(UserData.pets);
            this.mAddedItems = new Hashtable();
            this.mRemovedItems = new ArrayList();
            this.isUpdated = false;
        }

        internal void ClearItems()
        {
            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.runFastQuery("DELETE FROM items_users WHERE user_id = " + UserId); //Do join
                //dbClient.runFastQuery("DELETE FROM user_items_songs WHERE user_id = " + UserId);
            }

            mAddedItems.Clear();
            mRemovedItems.Clear();
            floorItems.Clear();
            wallItems.Clear();
            discs.Clear();
            InventoryPets.Clear();
            isUpdated = true;

            mClient.GetMessageHandler().GetResponse().Init(Outgoing.UpdateInventary);
            GetClient().GetMessageHandler().SendResponse();
        }

        internal void SetActiveState(GameClient client)
        {
            this.mClient = client;
            userAttatched = true;
        }

        internal void SetIdleState()
        {
            userAttatched = false;
            this.mClient = null;
        }

        #region PETS
        //internal void ClearPets()
        //{
        //    using (DatabaseClient dbClient = FirewindEnvironment.GetDatabase().GetClient())
        //        dbClient.runFastQuery("DELETE FROM user_pets WHERE user_id = " + UserId + " AND room_id = 0;");

        //    UpdatePets(true);
        //}

        //internal void UpdatePets(bool FromDatabase)
        //{
        //    if (FromDatabase)
        //    {
        //        LoadInventory();
        //    }

        //    GetClient().SendMessage(SerializePetInventory());
        //}

        internal Pet GetPet(uint Id)
        {
            if (InventoryPets.ContainsKey(Id))
                return InventoryPets[Id] as Pet;
            return null;

            //{
            //    List<Pet>.Enumerator Pets = this.InventoryPets.GetEnumerator();

            //    while (Pets.MoveNext())
            //    {
            //        Pet Pet = Pets.Current;

            //        if (Pet.PetId == Id)
            //        {
            //            return Pet;
            //        }
            //    }
            //}

            //return null;
        }

        internal bool RemovePet(uint PetId)
        {
            isUpdated = false;
            ServerMessage RemoveMessage = new ServerMessage(Outgoing.PetRemovedFromInventory);
            RemoveMessage.AppendUInt(PetId);
            GetClient().SendMessage(RemoveMessage);

            InventoryPets.Remove(PetId);
            return true;
        }

        internal void MovePetToRoom(UInt32 PetId)
        {
            isUpdated = false;
            RemovePet(PetId);
        }

        internal void AddPet(Pet Pet)
        {
            isUpdated = false;
            if (Pet == null || InventoryPets.ContainsKey(Pet.PetId))
                return;

            Pet.PlacedInRoom = false;
            Pet.RoomId = 0;

            InventoryPets.Add(Pet.PetId, Pet);

            //using (DatabaseClient dbClient = FirewindEnvironment.GetDatabase().GetClient())
            //{
            //    dbClient.addParameter("botid", Pet.PetId);
            //    dbClient.runFastQuery("UPDATE user_pets SET room_id = 0, x = 0, y = 0, z = 0 WHERE id = @botid LIMIT 1");
            //}

            ServerMessage AddMessage = new ServerMessage(Outgoing.PetAddedToInventory);
            Pet.SerializeInventory(AddMessage);
            AddMessage.AppendBoolean(true); // ???
            GetClient().SendMessage(AddMessage);
            //UpdatePets(false);
        }
        #endregion

        internal void LoadInventory()
        {
            floorItems.Clear();
            wallItems.Clear();

            DataTable Data;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("CALL getuseritems(@userid)");
                dbClient.addParameter("userid", (int)UserId);

                Data = dbClient.getTable();

                //dbClient.setQuery("SELECT item_id, song_id FROM user_items_songs WHERE user_id = " + UserId);
                //dSongs = dbClient.getTable();
            }

            uint id;
            uint baseitem;
            int dataType;
            string extradata;
            int extra;
            foreach (DataRow Row in Data.Rows)
            {
                id = Convert.ToUInt32(Row[0]);
                baseitem = Convert.ToUInt32(Row[1]);

                    IRoomItemData data;
                    if (DBNull.Value.Equals(Row[2]))
                    {
                        data = new StringData("");
                        extra = 0;
                    }
                    else
                    {
                        dataType = Convert.ToInt32(Row[2]);
                        extradata = (string)Row[3];
                        extra = Convert.ToInt32(Row[4]);
                        switch (dataType)
                        {
                            case 0:
                                data = new StringData(extradata);
                                break;
                            case 1:
                                data = new MapStuffData();
                                break;
                            case 2:
                                data = new StringArrayStuffData();
                                break;
                            case 3:
                                data = new StringIntData();
                                break;
                            default:
                                data = new StringData(extradata);
                                break;
                        }
                        try
                        {
                            data.Parse(extradata);
                        }
                        catch
                        {
                            Logging.LogException(string.Format("Error in furni data! Item ID: \"{0}\" and data: \"{1}\"", id, extradata.Replace(Convert.ToChar(1).ToString(), "[1]")));
                            data = new StringData(extradata);
                        }
                    }

                UserItem item = new UserItem(id, baseitem, data, extra);

                if (item.GetBaseItem().InteractionType == InteractionType.musicdisc)
                    discs.Add(id, item);
                if (item.isWallItem)
                    wallItems.Add(id, item);
                else
                    floorItems.Add(id, item);
            }

            discs.Clear();

            //uint songItemID;
            //uint songID;
            //foreach (DataRow dRow in dSongs.Rows)
            //{
            //    songItemID = (uint)dRow[0];
            //    songID = (uint)dRow[1];

            //    SongItem song = new SongItem(songItemID, songID);
            //    songs.Add(songItemID, song);
            //}


            this.InventoryPets.Clear();
            DataTable Data2;
            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                //dbClient.addParameter("userid", UserId);
                dbClient.setQuery("SELECT id, user_id, room_id, name, type, race, color, expirience, energy, nutrition, respect, createstamp, x, y, z, have_saddle FROM user_pets WHERE user_id = " + UserId + " AND room_id = 0");
                Data2 = dbClient.getTable();
            }

            if (Data2 != null)
            {
                foreach (DataRow Row in Data2.Rows)
                {
                    Pet newPet = Catalog.GeneratePetFromRow(Row);
                    InventoryPets.Add(newPet.PetId, newPet);
                }
            }
        }

        internal void UpdateItems(bool FromDatabase)
        {
            if (FromDatabase)
            {
                RunDBUpdate();
                LoadInventory();
            }

            if (mClient == null)
                return;

            if (mClient.GetMessageHandler() == null)
                return;
            if (mClient.GetMessageHandler().GetResponse() == null)
                return;
            mClient.GetMessageHandler().GetResponse().Init(Outgoing.UpdateInventary);
            mClient.GetMessageHandler().SendResponse();
        }

        internal UserItem GetItem(uint Id)
        {
            isUpdated = false;
            if (floorItems.ContainsKey(Id))
                return (UserItem)floorItems[Id];
            else if (wallItems.ContainsKey(Id))
                return (UserItem)wallItems[Id];

            return null;
        }

        internal UserItem AddNewItem(UInt32 Id, UInt32 BaseItem, IRoomItemData data, int extra, bool insert, bool fromRoom, UInt32 songID = 0)
        {
            isUpdated = false;
            if (insert)
            {
                if (fromRoom)
                {
                    using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                    {
                        dbClient.runFastQuery("REPLACE INTO items_users VALUES (" + Id + "," + UserId + ")");

                        //dbClient.setQuery("REPLACE INTO user_items (id, user_id,base_item,extra_data) VALUES ('" + Id + "','" + UserId + "','" + BaseItem + "',@extra_data)");
                        //dbClient.addParameter("extra_data", ExtraData);
                        //dbClient.runQuery();
                    }

                    Item baseItem = FirewindEnvironment.GetGame().GetItemManager().GetItem(BaseItem);

                    if (baseItem != null && baseItem.InteractionType == InteractionType.musicdisc)
                    {
                        using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                        {
                            dbClient.runFastQuery("DELETE FROM items_rooms_songs WHERE item_id = " + Id);
                            //dbClient.runFastQuery("REPLACE INTO user_items_songs (item_id,user_id,song_id) VALUES (" + Id + "," + UserId + "," + songID + ")");
                        }
                    }
                }
                else
                {
                    using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                    {
                        dbClient.setQuery("INSERT INTO items (base_id) VALUES (" + BaseItem + ")");
                        Id = (uint)dbClient.insertQuery();

                        //if (!string.IsNullOrEmpty(ExtraData))
                        {
                            dbClient.setQuery("INSERT INTO items_extradata VALUES (" + Id + ",@datatype,@data,@extra)");
                            dbClient.addParameter("datatype", data.GetTypeID());
                            dbClient.addParameter("data", data);
                            dbClient.addParameter("extra", extra);
                            dbClient.runQuery();
                        }

                        dbClient.runFastQuery("INSERT INTO items_users VALUES (" + Id + "," + UserId + ")");
                        //dbClient.setQuery("INSERT INTO user_items (user_id,base_item,extra_data) VALUES ('" + UserId + "','" + BaseItem + "',@extra_data)");
                        //dbClient.addParameter("extra_data", ExtraData);
                        //Id = (uint)dbClient.insertQuery();
                    }
                }
            }
            UserItem ItemToAdd = new UserItem(Id, BaseItem, data, extra);

            if (UserHoldsItem(Id))
            {
                RemoveItem(Id, false);
            }

            if (ItemToAdd.GetBaseItem().InteractionType == InteractionType.musicdisc)
                discs.Add(ItemToAdd.Id, ItemToAdd);
            if (ItemToAdd.isWallItem)
                wallItems.Add(ItemToAdd.Id, ItemToAdd);
            else
                floorItems.Add(ItemToAdd.Id, ItemToAdd);

            if (mRemovedItems.Contains(Id))
                mRemovedItems.Remove(Id);

            if (!mAddedItems.ContainsKey(Id))
                mAddedItems.Add(Id, ItemToAdd);

            return ItemToAdd;
            //Logging.WriteLine("Item added: " + BaseItem);
        }

        private bool UserHoldsItem(uint itemID)
        {
            if (discs.ContainsKey(itemID))
                return true;
            if (floorItems.ContainsKey(itemID))
                return true;
            if (wallItems.ContainsKey(itemID))
                return true;
            return false;
        }

        internal void RemoveItem(UInt32 Id, bool PlacedInroom)
        {
            isUpdated = false;
            GetClient().GetMessageHandler().GetResponse().Init(Outgoing.RemoveObjectFromInventory);
            GetClient().GetMessageHandler().GetResponse().AppendUInt(Id);
            GetClient().GetMessageHandler().SendResponse();

            if (mAddedItems.ContainsKey(Id))
                mAddedItems.Remove(Id);
            if (mRemovedItems.Contains(Id))
                return;

            discs.Remove(Id);
            floorItems.Remove(Id);
            wallItems.Remove(Id);
            mRemovedItems.Add(Id);


            //{
            //    InventoryItems.Remove(GetItem(Id));

            //    using (DatabaseClient dbClient = FirewindEnvironment.GetDatabase().GetClient())
            //    {
            //        dbClient.runFastQuery("DELETE FROM user_items WHERE id = '" + Id + "' LIMIT 1");
            //    }
            //}
        }

        //internal ServerMessage SerializeItemInventory()
        //{
        //    //int wallItemCount = InventoryItems.Count(p => p.isWallItem);
        //    //int floorItemCount = InventoryItems.Count(p => !p.isWallItem);

        //    //var wallItems = (from p in InventoryItems where p.isWallItem select p);
        //    //var floorItems = (from p in InventoryItems where !p.isWallItem select p);


        //    //foreach (UserItem item in wallItems)
        //    //{
        //    //    //Serialize code here lol
        //    //}

        //    //foreach (UserItem item in floorItems)
        //    //{
        //    //    //Serialize code here lol
        //    //}


        //    //ServerMessage Message = new ServerMessage(140);
        //    //Message.AppendInt32(this.ItemCount);

        //    //{
        //    //    List<UserItem>.Enumerator eItems = this.InventoryItems.GetEnumerator();

        //    //    while (eItems.MoveNext())
        //    //    {
        //    //        eItems.Current.Serialize(Message);
        //    //    }
        //    //}

        //    //Message.AppendInt32(this.ItemCount);
        //    //return Message;
        //}

        internal ServerMessage SerializeFloorItemInventory()
        {
            ServerMessage Message = new ServerMessage(Outgoing.Inventory);
            Message.AppendString("S");
            Message.AppendInt32(1);
            Message.AppendInt32(1);
            Message.AppendInt32(floorItems.Count + discs.Count); // + discs.count

            foreach (UserItem item in floorItems.Values)
            {
                item.SerializeFloor(Message, true);
            }

            foreach (UserItem item in discs.Values)
            {
                item.SerializeFloor(Message, true);
            }

            return Message;
        }

        internal ServerMessage SerializeWallItemInventory()
        {
            ServerMessage Message = new ServerMessage(Outgoing.Inventory);
            Message.AppendString("I");
            Message.AppendInt32(1);
            Message.AppendInt32(1);
            Message.AppendInt32(wallItems.Count);

            foreach (UserItem item in wallItems.Values)
            {
                item.SerializeWall(Message, true);
            }

            return Message;
        }

        internal ServerMessage SerializePetInventory()
        {
            ServerMessage Message = new ServerMessage(Outgoing.PetInventory);
            Message.AppendInt32(1);
            Message.AppendInt32(1);
            Message.AppendInt32(InventoryPets.Count);

            foreach (Pet Pet in InventoryPets.Values)
            {
                Pet.SerializeInventory(Message);
            }
            //Logging.WriteLine(InventoryPets.Count);
            return Message;
        }

        private GameClient GetClient()
        {
            return FirewindEnvironment.GetGame().GetClientManager().GetClientByUserID(UserId);
        }

        internal void AddItemArray(List<RoomItem> RoomItemList)
        {
            foreach (RoomItem Item in RoomItemList)
            {
                AddItem(Item);
            }
        }

        internal void AddItem(RoomItem item)
        {
            AddNewItem(item.Id, item.BaseItem, item.data, item.Extra, true, true, 0);
        }


        private bool isUpdated;
        internal void RunCycleUpdate()
        {
            isUpdated = true;
            RunDBUpdate();
        }

        internal void RunDBUpdate()
        {
            //DateTime Start = DateTime.Now;
            try
            {
                if (mRemovedItems.Count > 0 || mAddedItems.Count > 0 || InventoryPets.Count > 0)
                {

                    QueryChunk queries = new QueryChunk();

                    if (mAddedItems.Count > 0) //This should be checked more carefully
                    {
                        foreach (UserItem Item in mAddedItems.Values)
                        {
                            queries.AddQuery("UPDATE items_users SET user_id = " + UserId + " WHERE item_id = " + Item.Id);
                            //parameters.Add("extra_data" + Item.Id, ((StringData)Item.data).Data);
                            //QueryBuilder.Append("UPDATE user_items SET user_id = " + UserId + " , base_item =" + Item.BaseItem + ", extra_data = @extra_data" + Item.Id + " WHERE id = " + Item.Id + "; ");
                        }

                        mAddedItems.Clear();
                    }
                    if (mRemovedItems.Count > 0)
                    {
                        foreach (UInt32 ItemID in mRemovedItems.ToArray())
                        {
                            queries.AddQuery("DELETE FROM items_users WHERE item_id=" + ItemID + " AND user_id=" + UserId);
                        }

                        mRemovedItems.Clear();
                    }

                    foreach (Pet pet in InventoryPets.Values)
                    {
                        if (pet.DBState == DatabaseUpdateState.NeedsUpdate)
                        {

                            queries.AddParameter(pet.PetId + "name", pet.Name);
                            queries.AddParameter(pet.PetId + "race", pet.Race);
                            queries.AddParameter(pet.PetId + "color", pet.Color);
                            queries.AddQuery("UPDATE user_pets SET room_id = " + pet.RoomId + ", name = @" + pet.PetId + "name, race = @" + pet.PetId + "race, color = @" + pet.PetId + "color, type = " + pet.Type + ", expirience = " + pet.Expirience + ", " +
                                "energy = " + pet.Energy + ", nutrition = " + pet.Nutrition + ", respect = " + pet.Respect + ", createstamp = '" + pet.CreationStamp + "', x = " + pet.X + ", Y = " + pet.Y + ", Z = " + pet.Z + " WHERE id = " + pet.PetId);
                        }
                        pet.DBState = DatabaseUpdateState.Updated;
                    }

                    using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                    {
                        queries.Execute(dbClient);
                    }
                }
            }
            catch (Exception e)
            {
                Logging.LogCacheError("FATAL ERROR DURING USER INVENTORY DB UPDATE: " + e.ToString());
            }
        }

        private bool userAttatched = false;

        internal bool NeedsUpdate
        {
            get
            {
                return (!userAttatched && !isUpdated);
            }
        }

        internal ServerMessage SerializeMusicDiscs()
        {
            ServerMessage Message = new ServerMessage(333);
            Message.AppendInt32(discs.Count);

            foreach (UserItem SongDisk in discs.Values)
            {
                uint SongId = 0;
                uint.TryParse(SongDisk.Data.ToString(), out SongId);

                Message.AppendUInt(SongDisk.Id);
                Message.AppendUInt(SongId);
            }

            return Message;
        }

        internal List<Pet> GetPets()
        {
            List<Pet> toReturn = new List<Pet>();
            foreach (Pet pet in InventoryPets.Values)
                toReturn.Add(pet);

            return toReturn;
        }

        public bool isInactive
        {
            get
            {
                return !userAttatched;
            }
        }

        internal void SendFloorInventoryUpdate()
        {
            this.mClient.SendMessage(SerializeFloorItemInventory());
        }

        internal Hashtable songDisks
        {
            get
            {
                return this.discs;
            }
        }
    }
}
