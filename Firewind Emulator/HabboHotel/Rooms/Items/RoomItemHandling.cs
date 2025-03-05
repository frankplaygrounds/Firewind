using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using Firewind.Collections;
using Firewind.Core;
using Firewind.HabboHotel.GameClients;
using Firewind.HabboHotel.Items;
using Firewind.HabboHotel.Pathfinding;
using Firewind.Messages;
using Database_Manager.Database.Session_Details.Interfaces;
using Firewind.HabboHotel.Rooms.Games;
using Firewind.HabboHotel.Rooms.Wired;
using Firewind.Util;
using HabboEvents;


namespace Firewind.HabboHotel.Rooms
{
    class RoomItemHandling
    {
        private Room room;

        internal QueuedDictionary<uint, RoomItem> mFloorItems;
        internal QueuedDictionary<uint, RoomItem> mWallItems;

        private Hashtable mRemovedItems;
        private Hashtable mMovedItems;
        private Hashtable mAddedItems;

        internal QueuedDictionary<uint, RoomItem> mRollers;
        private List<uint> rollerItemsMoved;
        private List<uint> rollerUsersMoved;
        private List<ServerMessage> rollerMessages;

        private bool mGotRollers;
        private int mRollerSpeed;
        private int mRoolerCycle;

        private Queue roomItemUpdateQueue;

        internal bool GotRollers
        {
            get
            {
                return mGotRollers;
            }
            set
            {
                mGotRollers = value;
            }
        }

        public RoomItemHandling(Room room)
        {
            this.room = room;

            this.mRemovedItems = new Hashtable();
            this.mMovedItems = new Hashtable();
            this.mAddedItems = new Hashtable();
            this.mRollers = new QueuedDictionary<uint,RoomItem>();

            this.mWallItems = new QueuedDictionary<uint, RoomItem>();
            this.mFloorItems = new QueuedDictionary<uint, RoomItem>();
            this.roomItemUpdateQueue = new Queue();
            this.mGotRollers = false;
            this.mRoolerCycle = 0;
            this.mRollerSpeed = 4;

            rollerItemsMoved = new List<uint>();
            rollerUsersMoved = new List<uint>();
            rollerMessages = new List<ServerMessage>();
        }

        internal void QueueRoomItemUpdate(RoomItem item)
        {
            lock (roomItemUpdateQueue.SyncRoot)
            {
                roomItemUpdateQueue.Enqueue(item);
            }
        }

        internal List<RoomItem> RemoveAllFurniture(GameClient Session)
        {
            List<RoomItem> ReturnList = new List<RoomItem>();
            foreach (RoomItem Item in mFloorItems.Values.ToArray())
            {
                Item.Interactor.OnRemove(Session, Item);
                ServerMessage Message = new ServerMessage(Outgoing.ObjectRemove);
                Message.AppendString(Item.Id + String.Empty);
                Message.AppendInt32(0);
                Message.AppendInt32(room.OwnerId);
                room.SendMessage(Message);

                //mFloorItems.Remove(Item.Id);

                ReturnList.Add(Item);
            }

            foreach (RoomItem Item in mWallItems.Values.ToArray())
            {
                Item.Interactor.OnRemove(Session, Item);
                ServerMessage Message = new ServerMessage(Outgoing.PickUpWallItem);
                Message.AppendString(Item.Id + String.Empty);
                Message.AppendInt32(room.OwnerId);
                room.SendMessage(Message);
                //mWallItems.Remove(Item.Id);

                ReturnList.Add(Item);
            }

            mWallItems.Clear();
            mFloorItems.Clear();

            mRemovedItems.Clear();

            mMovedItems.Clear();
            mAddedItems.Clear();
            mRollers.QueueDelegate(new onCycleDoneDelegate(ClearRollers));

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.runFastQuery("DELETE FROM items_rooms WHERE room_id = " + room.RoomId);
            }

            room.GetGameMap().GenerateMaps();
            room.GetRoomUserManager().UpdateUserStatusses();

            if (room.GotWired())
            {
                room.GetWiredHandler().OnPickall();
            }

            return ReturnList;
        }

        private void ClearRollers()
        {
            mRollers.Clear();
        }

        internal void SetSpeed(int p)
        {
            this.mRollerSpeed = p;
        }

        internal void LoadFurniture()
        {
            //this.Items.Clear();
            this.mFloorItems.Clear();
            this.mWallItems.Clear();
            DataTable Data;
            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("CALL getroomitems(@roomid)");
                dbClient.addParameter("roomid", room.RoomId);
                Data = dbClient.getTable();


                uint itemID;
                decimal x;
                decimal y;
                sbyte n;
                uint baseID;
                int dataType;
                string extradata;
                WallCoordinate wallCoord;
                int extra;
                foreach (DataRow dRow in Data.Rows)
                {
                    itemID = Convert.ToUInt32(dRow[0]);
                    x = Convert.ToDecimal(dRow[1]);
                    y = Convert.ToDecimal(dRow[2]);
                    n = Convert.ToSByte(dRow[3]);
                    baseID = Convert.ToUInt32(dRow[4]);
                    IRoomItemData data;
                    if (DBNull.Value.Equals(dRow[5]))
                    {
                        data = new StringData("");
                        extra = 0;
                    }
                    else
                    {
                        dataType = Convert.ToInt32(dRow[5]);
                        extradata = (string)dRow[6];
                        extra = Convert.ToInt32(dRow[7]);
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
                            Logging.LogException(string.Format("Error in furni data! Item ID: \"{0}\" and data: \"{1}\"", itemID, extradata.Replace(Convert.ToChar(1).ToString(), "[1]")));
                            dbClient.runFastQuery(string.Format("DELETE FROM items_extradata WHERE item_id = {0}", itemID));
                        }
                    }

                    if (FirewindEnvironment.GetGame().GetItemManager().GetItem(baseID).Type == 'i') // Is wallitem
                    {
                        wallCoord = new WallCoordinate((double)x, (double)y, n);
                        RoomItem item = new RoomItem(itemID, room.RoomId, baseID, data, extra, wallCoord, room);

                        if (!mWallItems.ContainsKey(itemID))
                            mWallItems.Inner.Add(itemID, item);
                    }
                    else //Is flooritem
                    {
                        int coordX, coordY;
                        TextHandling.Split((double)x, out coordX, out coordY);

                        RoomItem item = new RoomItem(itemID, room.RoomId, baseID, data, extra, coordX, coordY, (double)y, n, room);
                        if (!mFloorItems.ContainsKey(itemID))
                            mFloorItems.Inner.Add(itemID, item);
                    }
                }

                foreach (RoomItem Item in mFloorItems.Values)
                {
                    if (Item.IsRoller)
                        mGotRollers = true;
                    else if (Item.GetBaseItem().InteractionType == Firewind.HabboHotel.Items.InteractionType.dimmer)
                    {
                        if (room.MoodlightData == null)
                            room.MoodlightData = new MoodlightData(Item.Id);
                    }
                    else if (WiredUtillity.TypeIsWired(Item.GetBaseItem().InteractionType))
                    {
                        WiredLoader.LoadWiredItem(Item, room, dbClient);
                        room.GetWiredHandler().AddWire(Item, Item.Coordinate, Item.Rot, Item.GetBaseItem().InteractionType);
                    }

                }
            }
        }


        internal RoomItem GetItem(uint pId)
        {
            if (mFloorItems.ContainsKey(pId))
                return mFloorItems.GetValue(pId);
            else if (mWallItems.ContainsKey(pId))
                return mWallItems.GetValue(pId);
            else
                return null;
        }

        internal void RemoveFurniture(GameClient Session, uint pId)
        {
            RoomItem Item = GetItem(pId);
            /*Logging.WriteLine("");
            Logging.WriteLine("");
            Logging.WriteLine("");
            Logging.WriteLine("");
            Logging.WriteLine("");
            Logging.WriteLine("");
            Logging.WriteLine("Trying to remove furni " + pId);*/
            if (Item == null)
                return;
            //Logging.WriteLine("IS ANYONE THERE????");
            if (Item.GetBaseItem().InteractionType == InteractionType.fbgate)
            {
                room.GetSoccer().UnRegisterGate(Item);
            }

            if (Item.GetBaseItem().InteractionType != InteractionType.gift)
                Item.Interactor.OnRemove(Session, Item);
            RemoveRoomItem(Item);

            if (Item.wiredHandler != null)
            {
                using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                {
                    Item.wiredHandler.DeleteFromDatabase(dbClient);
                    Item.wiredHandler.Dispose();
                    room.GetWiredHandler().RemoveFurniture(Item);
                }
                Item.wiredHandler = null;
            }

            if (Item.wiredCondition != null)
            {
                using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                {
                    Item.wiredCondition.DeleteFromDatabase(dbClient);
                    Item.wiredCondition.Dispose();
                    room.GetWiredHandler().conditionHandler.ClearTile(Item.Coordinate);
                }
                Item.wiredCondition = null;
            }

        }

        private void RemoveRoomItem(RoomItem Item)
        {
            //Logging.WriteLine("HALLO?? TRYING TO REMOVE???");
            if (Item.IsWallItem)
            {
                ServerMessage Message = new ServerMessage(Outgoing.PickUpWallItem);
                Message.AppendString(Item.Id + String.Empty);
                Message.AppendInt32(room.OwnerId);
                room.SendMessage(Message);
            }
            else if (Item.IsFloorItem)
            {
                //Logging.WriteLine("YEEEEEAH?????");
                ServerMessage Message = new ServerMessage(Outgoing.ObjectRemove);
                Message.AppendString(Item.Id + String.Empty);
                Message.AppendInt32(0);
                Message.AppendInt32(room.OwnerId);
                room.SendMessage(Message);
            }

            

            if (Item.IsWallItem)
                mWallItems.Remove(Item.Id);
            else
            {
                //Logging.WriteLine("REMOVE FLOOR Item wtf?" + mFloorItems.ContainsKey(Item.Id));
                this.mFloorItems.Remove(Item.Id);
                this.mFloorItems.OnCycle();
                room.GetGameMap().RemoveFromMap(Item);
            }

            //Logging.WriteLine("removed?? = " + mFloorItems.ContainsKey(Item.Id));
            RemoveItem(Item);

            room.GetRoomUserManager().UpdateUserStatusses();

            if (WiredHandler.TypeIsWire(Item.GetBaseItem().InteractionType))
            {
                room.GetWiredHandler().RemoveWiredItem(Item.Coordinate);
            }
        }

        private List<ServerMessage> CycleRollers() // Credit goes to Thor for the roller packets
        {
            mRollers.OnCycle();

            if (mGotRollers)
            {
                if (mRoolerCycle >= mRollerSpeed || mRollerSpeed == 0)
                {
                    rollerItemsMoved.Clear();
                    rollerUsersMoved.Clear();
                    rollerMessages.Clear();

                    List<RoomItem> ItemsOnRoller;
                    List<RoomItem> ItemsOnNext;

                    foreach (RoomItem Item in mRollers.Values)
                    {
                        Point NextCoord = Item.SquareInFront;

                        ItemsOnRoller = room.GetGameMap().GetRoomItemForSquare(Item.GetX, Item.GetY, Item.GetZ);
                        RoomUser UserOnRoller = room.GetRoomUserManager().GetUserForSquare(Item.GetX, Item.GetY);

                        if (ItemsOnRoller.Count > 0 || UserOnRoller != null)
                        {
                            ItemsOnNext = room.GetGameMap().GetCoordinatedItems(NextCoord);

                            double NextZ = 0;
                            int ItemCount = 0;
                            bool NextRoller = false;

                            double NextRollerZ = 0.0;
                            bool NextRollerClear = true;

                            foreach (RoomItem tItem in ItemsOnNext)
                            {
                                if (tItem.IsRoller)
                                {
                                    NextRoller = true;
                                    if (tItem.TotalHeight > NextRollerZ)
                                        NextRollerZ = tItem.TotalHeight;
                                }
                            }

                            if (NextRoller)
                            {
                                foreach (RoomItem tItem in ItemsOnNext)
                                {
                                    if (tItem.TotalHeight > NextRollerZ)
                                        NextRollerClear = false;
                                }
                            }
                            else
                                NextRollerZ = NextRollerZ + room.GetGameMap().GetHeightForSquareFromData(NextCoord);
                            NextZ = NextRollerZ;
                            bool rItemsOnNext = (ItemCount > 0);
                            if (room.GetRoomUserManager().GetUserForSquare(NextCoord.X, NextCoord.Y) != null)
                                rItemsOnNext = true;

                            foreach (RoomItem tItem in ItemsOnRoller)
                            {
                                double AddZ = tItem.GetZ - Item.TotalHeight;
                                if (!rollerItemsMoved.Contains(tItem.Id) && room.GetGameMap().CanRollItemHere(NextCoord.X, NextCoord.Y)
                                    && NextRollerClear && Item.GetZ < tItem.GetZ && room.GetRoomUserManager().GetUserForSquare(NextCoord.X, NextCoord.Y) == null)
                                {
                                    rollerMessages.Add(UpdateItemOnRoller(tItem, NextCoord, Item.Id, NextRollerZ + AddZ));
                                    rollerItemsMoved.Add(tItem.Id);
                                }
                            }

                            if (UserOnRoller != null && !UserOnRoller.IsWalking && NextRollerClear && !rItemsOnNext && room.GetGameMap().CanRollItemHere(NextCoord.X, NextCoord.Y) && room.GetGameMap().GetFloorStatus(NextCoord) != 0)
                            {
                                if (!rollerUsersMoved.Contains(UserOnRoller.HabboId))
                                {
                                    rollerMessages.Add(UpdateUserOnRoller(UserOnRoller, NextCoord, Item.Id, NextZ));
                                    rollerUsersMoved.Add(UserOnRoller.HabboId);
                                }
                            }
                        }
                    }

                    mRoolerCycle = 0;
                    return rollerMessages;
                }
                else
                    mRoolerCycle++;
            }

            return new List<ServerMessage>();
        }

        private ServerMessage UpdateItemOnRoller(RoomItem pItem, Point NextCoord, uint pRolledID, Double NextZ)
        {
            ServerMessage mMessage = new ServerMessage();
            mMessage.Init(Outgoing.ObjectOnRoller); // Cf
            mMessage.AppendInt32(pItem.GetX);
            mMessage.AppendInt32(pItem.GetY);

            mMessage.AppendInt32(NextCoord.X);
            mMessage.AppendInt32(NextCoord.Y);

            mMessage.AppendInt32(1);

            mMessage.AppendUInt(pItem.Id);

            mMessage.AppendString(TextHandling.GetString(pItem.GetZ));
            mMessage.AppendString(TextHandling.GetString(NextZ));

            mMessage.AppendUInt(pRolledID);

            //room.SendMessage(mMessage);

            //SetFloorItem(null, pItem, NextCoord.X, NextCoord.Y, pItem.Rot, false, true);
            SetFloorItem(pItem, NextCoord.X, NextCoord.Y, NextZ);

            return mMessage;
        }

        internal ServerMessage UpdateUserOnRoller(RoomUser pUser, Point pNextCoord, uint pRollerID, Double NextZ)
        {
            ServerMessage mMessage = new ServerMessage(0);
            mMessage.Init(Outgoing.ObjectOnRoller); // Cf
            mMessage.AppendInt32(pUser.X);
            mMessage.AppendInt32(pUser.Y);

            mMessage.AppendInt32(pNextCoord.X);
            mMessage.AppendInt32(pNextCoord.Y);

            mMessage.AppendInt32(0);
            mMessage.AppendUInt(pRollerID);
            mMessage.AppendInt32(2);
            mMessage.AppendInt32(pUser.VirtualId);
            mMessage.AppendString(TextHandling.GetString(pUser.Z));
            mMessage.AppendString(TextHandling.GetString(NextZ));

            room.GetGameMap().UpdateUserMovement(new Point(pUser.X, pUser.Y), new Point(pNextCoord.X, pNextCoord.Y), pUser);
            room.GetGameMap().GameMap[pUser.X, pUser.Y] = 1;
            pUser.X = pNextCoord.X;
            pUser.Y = pNextCoord.Y;
            pUser.Z = NextZ;
            room.GetGameMap().GameMap[pUser.X, pUser.Y] = 0;

            return mMessage;
        }

        internal void SaveFurniture(IQueryAdapter dbClient)
        {
            try
            {
                if (mAddedItems.Count > 0 || mRemovedItems.Count > 0 || mMovedItems.Count > 0 || room.GetRoomUserManager().PetCount > 0)
                {
                    QueryChunk standardQueries = new QueryChunk();
                    QueryChunk itemInserts = new QueryChunk("REPLACE INTO items_rooms (item_id,room_id,x,y,n) VALUES ");
                    QueryChunk extradataInserts = new QueryChunk("REPLACE INTO items_extradata (item_id,data_type,data) VALUES ");

                    foreach (RoomItem Item in mRemovedItems.Values)
                    {
                        standardQueries.AddQuery("DELETE FROM items_rooms WHERE item_id = " + Item.Id + " AND room_id = " + room.RoomId); //Do join + function
                    }

                    if (mAddedItems.Count > 0)
                    {
                        foreach (RoomItem Item in mAddedItems.Values)
                        {
                            //if (!string.IsNullOrEmpty((string)Item.data.GetData()))
                            {
                                extradataInserts.AddQuery("(" + Item.Id + ",@data_type_id" + Item.Id + ",@data_id" + Item.Id + ")");
                                extradataInserts.AddParameter("@data_type_id" + Item.Id, Item.data.GetTypeID());
                                extradataInserts.AddParameter("@data_id" + Item.Id, Item.data.ToString());
                            }

                            if (Item.IsFloorItem)
                            {
                                double combinedCoords = TextHandling.Combine(Item.GetX, Item.GetY);
                                itemInserts.AddQuery("(" + Item.Id + "," + Item.RoomId + "," + TextHandling.GetString(combinedCoords) + "," + TextHandling.GetString(Item.GetZ) + "," + Item.Rot + ")");
                            }
                            else
                            {
                                itemInserts.AddQuery("(" + Item.Id + "," + Item.RoomId + "," + TextHandling.GetString(Item.wallCoord.GetXValue()) + "," + TextHandling.GetString(Item.wallCoord.GetYValue()) + "," + Item.wallCoord.n() + ")");
                            }
                        }
                    }


                    foreach (RoomItem Item in mMovedItems.Values)
                    {
                        //if (!string.IsNullOrEmpty((string)Item.data.GetData()))
                        {
                            extradataInserts.AddQuery("(" + Item.Id + ",@data_type_id" + Item.Id + ",@data_id" + Item.Id + ")");
                            extradataInserts.AddParameter("@data_type_id" + Item.Id, Item.data.GetTypeID());
                            extradataInserts.AddParameter("@data_id" + Item.Id, Item.data.ToString());

                            //standardQueries.AddQuery("UPDATE items_extradata SET data = @data" + Item.Id + " WHERE item_id = " + Item.Id);
                            //standardQueries.AddParameter("data" + Item.Id, ((StringData)Item.data).Data);
                        }

                        if (Item.IsWallItem)
                        {
                            standardQueries.AddQuery("UPDATE items_rooms SET x=" + TextHandling.GetString(Item.wallCoord.GetXValue()) + ", y=" + TextHandling.GetString(Item.wallCoord.GetYValue()) + ", n=" + Item.wallCoord.n() + " WHERE item_id = " + Item.Id);
                        }
                        else
                        {
                            double combinedCoords = TextHandling.Combine(Item.GetX, Item.GetY);

                            standardQueries.AddQuery("UPDATE items_rooms SET x=" + TextHandling.GetString(combinedCoords) + ", y=" + TextHandling.GetString(Item.GetZ) + ", n=" + Item.Rot + " WHERE item_id = " + Item.Id);
                        }
                    }

                    room.GetRoomUserManager().AppendPetsUpdateString(dbClient);

                    mAddedItems.Clear();
                    mRemovedItems.Clear();
                    mMovedItems.Clear();

                    standardQueries.Execute(dbClient);
                    itemInserts.Execute(dbClient);
                    extradataInserts.Execute(dbClient);

                    standardQueries.Dispose();
                    itemInserts.Dispose();
                    extradataInserts.Dispose();

                    standardQueries = null;
                    itemInserts = null;
                    extradataInserts = null;
                }
            }
            catch (Exception e)
            {
                Logging.LogCriticalException("Error during saving furniture for room " + room.RoomId + ". Stack: " + e.ToString());
            }
        }

        internal bool SetFloorItem(GameClient Session, RoomItem Item, int newX, int newY, int newRot, bool newItem, bool OnRoller, bool sendMessage, bool SpecialMove = false)
        {
            return SetFloorItem(Session, Item, newX, newY, newRot, newItem, OnRoller, sendMessage, true, SpecialMove);
        }

        internal bool SetFloorItem(GameClient Session, RoomItem Item, int newX, int newY, int newRot, bool newItem, bool OnRoller, bool sendMessage, bool updateRoomUserStatuses, bool SpecialMove = false)
        {
            bool NeedsReAdd = false;
            if (!newItem)
                NeedsReAdd = room.GetGameMap().RemoveFromMap(Item);
            Dictionary<int, ThreeDCoord> AffectedTiles = Gamemap.GetAffectedTiles(Item.GetBaseItem().Length, Item.GetBaseItem().Width, newX, newY, newRot);

            if (!room.GetGameMap().ValidTile(newX, newY) || room.GetGameMap().SquareHasUsers(newX, newY) && !Item.GetBaseItem().IsSeat)
            {
                if (NeedsReAdd)
                {
                    AddItem(Item);
                    room.GetGameMap().AddToMap(Item);
                }
                return false;
            }

            foreach (ThreeDCoord Tile in AffectedTiles.Values)
            {
                if (!room.GetGameMap().ValidTile(Tile.X, Tile.Y) || (room.GetGameMap().SquareHasUsers(Tile.X, Tile.Y) && !Item.GetBaseItem().IsSeat))
                {
                    if (NeedsReAdd)
                    {
                        AddItem(Item);
                        room.GetGameMap().AddToMap(Item);
                    }
                    return false;
                }
            }

            // Start calculating new Z coordinate
            Double newZ = room.GetGameMap().Model.SqFloorHeight[newX, newY];
            try
            {
                if (Math.Abs(Session.GetHabbo().StackHeight) < 40 && Math.Abs(Session.GetHabbo().StackHeight) > -40)
                {
                    newZ = Session.GetHabbo().StackHeight;
                } else
                {
                    Session.SendMOTD("Whoops, something went wrong!\n\nStack height must be between -40 and 40. \nThe item was placed at a stack height of 0.\n\nReset your stack height by typing :bh");
                    newZ = 0;
                }
            } catch { 
                //was either ran by wired or i fucked up
            }

            if (!OnRoller)
            {
                // Is the item trying to stack on itself!?
                //if (Item.Rot == newRot && Item.GetX == newX && Item.GetY == newY && Item.GetZ != newZ)
                //{
                //    if (NeedsReAdd)
                //        AddItem(Item);
                //    return false;
                //}

                // Make sure this tile is open and there are no users here
                if (room.GetGameMap().Model.SqState[newX, newY] != SquareState.OPEN && !Item.GetBaseItem().IsSeat)
                {
                    if (NeedsReAdd)
                    {
                        AddItem(Item);
                        room.GetGameMap().AddToMap(Item);
                    }
                    return false;
                }

                foreach (ThreeDCoord Tile in AffectedTiles.Values)
                {
                    if (room.GetGameMap().Model.SqState[Tile.X, Tile.Y] != SquareState.OPEN && !Item.GetBaseItem().IsSeat)
                    {
                        if (NeedsReAdd)
                        {
                            AddItem(Item);
                            room.GetGameMap().AddToMap(Item);
                        }
                        return false;
                    }
                }

                // And that we have no users
                if (!Item.GetBaseItem().IsSeat && !Item.IsRoller)
                {
                    foreach (ThreeDCoord Tile in AffectedTiles.Values)
                    {
                        if (room.GetGameMap().GetRoomUsers(new Point(Tile.X, Tile.Y)).Count > 0)
                        {
                            if (NeedsReAdd)
                            {
                                AddItem(Item);
                                room.GetGameMap().AddToMap(Item);
                            }
                            return false;
                        }
                    }
                }
            }

            // Find affected objects
            List<RoomItem> ItemsOnTile = GetFurniObjects(newX, newY);
            List<RoomItem> ItemsAffected = new List<RoomItem>();
            List<RoomItem> ItemsComplete = new List<RoomItem>();

            foreach (ThreeDCoord Tile in AffectedTiles.Values)
            {
                List<RoomItem> Temp = GetFurniObjects(Tile.X, Tile.Y);

                if (Temp != null)
                {
                    ItemsAffected.AddRange(Temp);
                }
            }


            ItemsComplete.AddRange(ItemsOnTile);
            ItemsComplete.AddRange(ItemsAffected);

            if (!OnRoller)
            {
                // Check for items in the stack that do not allow stacking on top of them
                foreach (RoomItem I in ItemsComplete)
                {
                    if (Session.GetHabbo().StackHeightStatus)
                    {
                        // we dont care
                        continue;
                    }

                    if (I == null)
                        continue;

                    if (I.Id == Item.Id)
                    {
                        continue;
                    }

                    if (I.GetBaseItem() == null)
                        continue;

                    if (!I.GetBaseItem().Stackable)
                    {
                        if (NeedsReAdd)
                        {
                            AddItem(Item);
                            room.GetGameMap().AddToMap(Item);
                        }
                        return false;
                    }
                }
            }

            //if (!Item.IsRoller)
            {
                // If this is a rotating action, maintain item at current height
                if (Item.Rot != newRot && Item.GetX == newX && Item.GetY == newY)
                {
                    newZ = Item.GetZ;
                }

                // Are there any higher objects in the stack!?
                foreach (RoomItem I in ItemsComplete)
                {

                    if (I.Id == Item.Id)
                    {
                        continue; // cannot stack on self
                    }

                    if (Session.GetHabbo().StackHeightStatus) 
                    {
                        newZ = Session.GetHabbo().StackHeight;
                        continue;
                    }

                    if (I.TotalHeight > newZ)
                    {
                        newZ = I.TotalHeight;
                    }
                }
            }

            // Verify the rotation is correct
            //if (newRot != 0 && newRot != 2 && newRot != 4 && newRot != 6 && newRot != 8)
            //{
            //    newRot = 0;
            //}

            //Item.GetX = newX;
            //Item.GetY = newY;
            //Item.GetZ = newZ;


            Item.Rot = newRot;
            int oldX = Item.GetX;
            int oldY = Item.GetY;
            Item.SetState(newX, newY, newZ, AffectedTiles);

            if (!OnRoller && Session != null)
                Item.Interactor.OnPlace(Session, Item);


            if (newItem)
            {
                if (mFloorItems.ContainsKey(Item.Id))
                {
                    if (Session != null)
                        Session.SendNotif(LanguageLocale.GetValue("room.itemplaced"));

                    //Remove from map!!!
                    return true;
                }
                /*else if (mFloorItems.ContainsKey(Item.Id) && Item.MagicRemove)
                {
                    RemoveFurniture(Session, Item.Id);
                    if (mFloorItems.ContainsKey(Item.Id))
                    {
                        Logging.WriteLine("lul?");
                        mFloorItems.Remove(Item.Id);
                    }
                }*/

                //using (DatabaseClient dbClient = FirewindEnvironment.GetDatabase().GetClient())
                //{
                //    dbClient.addParameter("extra_data", ((StringData)Item.data).Data);
                //    dbClient.runFastQuery("INSERT INTO room_items (id,room_id,base_item,extra_data,x,y,z,rot,wall_pos) VALUES ('" + Item.Id + "','" + RoomId + "','" + Item.BaseItem + "',@extra_data,'" + Item.GetX + "','" + Item.GetY + "','" + Item.GetZ + "','" + Item.Rot + "','')");
                //}
                //if (mRemovedItems.ContainsKey(Item.Id))
                //    mRemovedItems.Remove(Item.Id);
                //if (mAddedItems.ContainsKey(Item.Id))
                //    return false;

                //mAddedItems.Add(Item.Id, Item);

                if (Item.IsFloorItem && !mFloorItems.ContainsKey(Item.Id))
                    mFloorItems.Add(Item.Id, Item);
                else if (Item.IsWallItem && !mWallItems.ContainsKey(Item.Id))
                    mWallItems.Add(Item.Id, Item);

                AddItem(Item);

                if (sendMessage)
                {
                    ServerMessage Message = new ServerMessage(Outgoing.ObjectAdd);
                    Item.Serialize(Message, room.OwnerId);
                    Message.AppendString(room.Owner);
                    room.SendMessage(Message);
                }
            }
            else
            {
                //using (DatabaseClient dbClient = FirewindEnvironment.GetDatabase().GetClient())
                //{
                //    dbClient.runFastQuery("UPDATE room_items SET x = '" + Item.GetX + "', y = '" + Item.GetY + "', z = '" + Item.GetZ + "', rot = '" + Item.Rot + "', wall_pos = '' WHERE id = '" + Item.Id + "' LIMIT 1");
                //}
                UpdateItem(Item);

                if (!OnRoller && sendMessage)
                {
                    if (SpecialMove)
                    {
                        ServerMessage Message = new ServerMessage(Outgoing.ObjectOnRoller);
                        Message.AppendInt32(oldX);
                        Message.AppendInt32(oldY);
                        Message.AppendInt32(Item.GetX);
                        Message.AppendInt32(Item.GetY);
                        Message.AppendInt32(1);
                        Message.AppendUInt(Item.Id);
                        Message.AppendString(String.Format("{0:0.00}", TextHandling.GetString(Item.GetZ))); // altura a la que se encuentra
                        Message.AppendString(String.Format("{0:0.00}", TextHandling.GetString(Item.GetZ))); // altura del furni
                        Message.AppendInt32(-1);
                        room.SendMessage(Message);
                    }
                    else 
                    { 
                        ServerMessage Message = new ServerMessage(Outgoing.ObjectUpdate);
                    Item.Serialize(Message, room.OwnerId);
                    //Message.AppendString(room.Owner);
                    room.SendMessage(Message);
                }
                }
            }

            if (!newItem)
            {
                room.GetWiredHandler().RemoveWiredItem(new System.Drawing.Point(oldX, oldY));

                if (WiredHandler.TypeIsWire(Item.GetBaseItem().InteractionType))
                {
                    room.GetWiredHandler().AddWire(Item, new System.Drawing.Point(newX, newY), newRot, Item.GetBaseItem().InteractionType);
                }
            }
            else
            {
                if (WiredHandler.TypeIsWire(Item.GetBaseItem().InteractionType))
                {
                    room.GetWiredHandler().AddWire(Item, Item.Coordinate, newRot, Item.GetBaseItem().InteractionType);
                }
            }

            //GenerateMaps(false);
            room.GetGameMap().AddToMap(Item);

            if (Item.GetBaseItem().IsSeat)
                updateRoomUserStatuses = true;

            if (updateRoomUserStatuses)
                room.GetRoomUserManager().UpdateUserStatusses();

            return true;
        }

        internal List<RoomItem> GetFurniObjects(int X, int Y) 
        {

            return room.GetGameMap().GetCoordinatedItems(new Point(X, Y));
            //List<RoomItem> Results = new List<RoomItem>();

            //foreach (RoomItem Item in mFloorItems.Values)
            //{
            //    if (Item.GetX == X && Item.GetY == Y)
            //    {
            //        Results.Add(Item);
            //    }

            //    Dictionary<int, ThreeDCoord> PointList = Item.GetAffectedTiles; //GetAffectedTiles(Item.GetBaseItem().Length, Item.GetBaseItem().Width, Item.GetX, Item.GetY, Item.Rot);

            //    foreach (ThreeDCoord Tile in PointList.Values)
            //    {
            //        if (Tile.X == X && Tile.Y == Y)
            //        {
            //            Results.Add(Item);
            //        }
            //    }
            //}
            //return Results;
        }

        internal bool SetFloorItem(RoomItem Item, int newX, int newY, Double newZ)
        {
            room.GetGameMap().RemoveFromMap(Item);
            Item.SetState(newX, newY, newZ, Gamemap.GetAffectedTiles(Item.GetBaseItem().Length, Item.GetBaseItem().Width, newX, newY, Item.Rot));

            UpdateItem(Item);
            room.GetGameMap().AddItemToMap(Item);

            return true;
        }

        internal bool SetWallItem(GameClient Session, RoomItem Item)
        {
            if (!Item.IsWallItem || mWallItems.ContainsKey(Item.Id))
                return false;
            if (mFloorItems.ContainsKey(Item.Id))
            {
                Session.SendNotif(LanguageLocale.GetValue("room.itemplaced"));
                return true;
            }
            Item.Interactor.OnPlace(Session, Item);

            if (Item.GetBaseItem().InteractionType == InteractionType.dimmer)
            {
                if (room.MoodlightData == null)
                {
                    room.MoodlightData = new MoodlightData(Item.Id);
                    ((StringData)Item.data).Data = room.MoodlightData.GenerateExtraData();
                }
            }

            mWallItems.Add(Item.Id, Item);
            AddItem(Item);

            ServerMessage Message = new ServerMessage(Outgoing.AddWallItemToRoom);
            Item.Serialize(Message, room.OwnerId);
            Message.AppendString(room.Owner); // TODO
            room.SendMessage(Message);

            return true;
        }
        internal void UpdateItem(RoomItem item)
        {
            if (mAddedItems.ContainsKey(item.Id))
                return;
            if (mRemovedItems.ContainsKey(item.Id))
                mRemovedItems.Remove(item.Id);
            if (!mMovedItems.ContainsKey(item.Id))
                mMovedItems.Add(item.Id, item);
        }

        internal void AddItem(RoomItem item)
        {
            if (mRemovedItems.ContainsKey(item.Id))
                mRemovedItems.Remove(item.Id);
            if (!mMovedItems.ContainsKey(item.Id) && !mAddedItems.ContainsKey(item.Id))
                mAddedItems.Add(item.Id, item);
        }

        internal void RemoveItem(RoomItem item)
        {
            if (mAddedItems.ContainsKey(item.Id))
                mAddedItems.Remove(item.Id);

            if (mMovedItems.ContainsKey(item.Id))
                mMovedItems.Remove(item.Id);
            if (!mRemovedItems.ContainsKey(item.Id))
                mRemovedItems.Add(item.Id, item);
            mRollers.Remove(item.Id);
        }

        internal void OnCycle()
        {
            if (mGotRollers)
            {
                try
                {
                    room.SendMessage(CycleRollers());
                }
                catch (Exception e)
                {
                    Logging.LogThreadException(e.ToString(), "rollers for room with ID " + room.RoomId);
                    mGotRollers = false;
                }
            }

            if (roomItemUpdateQueue.Count > 0)
            {
                List<RoomItem> addItems = new List<RoomItem>();
                lock (roomItemUpdateQueue.SyncRoot)
                {
                    while (roomItemUpdateQueue.Count > 0)
                    {
                        RoomItem item = (RoomItem)roomItemUpdateQueue.Dequeue();
                        item.ProcessUpdates();

                        if (item.IsTrans || item.UpdateCounter > 0)
                            addItems.Add(item);
                    }

                    foreach (RoomItem item in addItems)
                    {
                        roomItemUpdateQueue.Enqueue(item);
                    }
                }
            }

            mFloorItems.OnCycle();
            mWallItems.OnCycle();
        }

        internal void Destroy()
        {
            mFloorItems.Clear();
            mWallItems.Clear();
            mRemovedItems.Clear();
            mMovedItems.Clear();
            mAddedItems.Clear();
            roomItemUpdateQueue.Clear();

            room = null;
            mFloorItems = null;
            mWallItems = null;
            mRemovedItems = null;
            mMovedItems = null;
            mAddedItems = null;
            mWallItems = null;
            roomItemUpdateQueue = null;
        }
    }
}
