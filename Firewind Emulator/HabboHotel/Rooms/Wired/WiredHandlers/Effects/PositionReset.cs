using System.Collections.Generic;
using System.Drawing;
using Firewind.HabboHotel.Items;
using Firewind.HabboHotel.Rooms.Games;
using Firewind.HabboHotel.Rooms.Wired.WiredHandlers.Interfaces;
using Database_Manager.Database.Session_Details.Interfaces;
using System.Data;
using System;

namespace Firewind.HabboHotel.Rooms.Wired.WiredHandlers.Effects
{
    class PositionReset : IWiredTrigger, IWiredCycleable, IWiredEffect, IWiredMatchFurni
    {
        private RoomItemHandling roomItemHandler;
        private WiredHandler handler;
        private uint itemID;

        private List<RoomItem> items;
        private Dictionary<uint, WiredMatchFurniSnapshot> snapshots;
        private bool matchState;
        private bool matchDirection;
        private bool matchPosition;
        private int delay;
        private int cycles;

        private bool disposed;

        public PositionReset(List<RoomItem> items, int delay, RoomItemHandling roomItemHandler, WiredHandler handler, uint itemID)
            : this(items, delay, roomItemHandler, handler, itemID, false, false, false)
        {
        }

        public PositionReset(List<RoomItem> items, int delay, RoomItemHandling roomItemHandler, WiredHandler handler, uint itemID, bool matchState, bool matchDirection, bool matchPosition)
        {
            this.items = items;
            this.snapshots = new Dictionary<uint, WiredMatchFurniSnapshot>();
            this.matchState = matchState;
            this.matchDirection = matchDirection;
            this.matchPosition = matchPosition;
            this.delay = delay;
            this.roomItemHandler = roomItemHandler;
            this.cycles = 0;
            this.itemID = itemID;
            this.handler = handler;
            this.disposed = false;
        }

        public bool OnCycle()
        {
            cycles++;
            if (cycles > delay)
            {
                HandleItems();
                return false;
            }
            return true;
        }

        public bool Handle(RoomUser user, Team team, RoomItem item)
        {
            cycles = 0;
            if (delay == 0)
            {
                return HandleItems();
            }
            else
            {
                handler.RequestCycle(this);
            }
            return false;
        }

        private bool HandleItems()
        {
            return HandleItems(true);
        }

        private bool HandleItems(bool showWiredEvent)
        {
            if (showWiredEvent && handler != null)
                handler.OnEvent(itemID);

            bool itemChanged = false;
            foreach (RoomItem item in items)
            {
                WiredMatchFurniSnapshot snapshot = GetSnapshot(item);
                if (snapshot == null)
                    continue;

                if (matchState && ApplyState(item, snapshot.State))
                    itemChanged = true;

                if (matchPosition || matchDirection)
                {
                    int newX = matchPosition ? snapshot.X : item.GetX;
                    int newY = matchPosition ? snapshot.Y : item.GetY;
                    int newRot = matchDirection ? snapshot.Rotation : item.Rot;

                    if ((item.GetX != newX || item.GetY != newY || item.Rot != newRot) &&
                        roomItemHandler.SetFloorItem(null, item, newX, newY, newRot, false, false, true, true))
                        itemChanged = true;
                }
            }

            return itemChanged;
        }

        public bool ApplySnapshot(Room room)
        {
            return HandleItems(false);
        }

        private WiredMatchFurniSnapshot GetSnapshot(RoomItem item)
        {
            WiredMatchFurniSnapshot snapshot;
            if (snapshots != null && snapshots.TryGetValue(item.Id, out snapshot))
                return snapshot;

            Point originalPosition = item.GetPlacementPosition();
            return new WiredMatchFurniSnapshot(item.Id, item.originalExtraData != null ? item.originalExtraData.ToString() : string.Empty, item.Rot, originalPosition.X, originalPosition.Y);
        }

        private bool ApplyState(RoomItem item, string state)
        {
            string currentState = item.data != null ? item.data.ToString() : string.Empty;
            if (currentState == state)
                return false;

            item.data = CreateData(item.data != null ? item.data.GetTypeID() : 0, state);
            item.UpdateState(false, true);
            return true;
        }

        private IRoomItemData CreateData(int type, string state)
        {
            try
            {
                IRoomItemData data;
                switch (type)
                {
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
                        return new StringData(state);
                }

                data.Parse(state);
                return data;
            }
            catch
            {
                return new StringData(state);
            }
        }

        public void Dispose()
        {
            disposed = true;
            roomItemHandler = null;
            handler = null;
            if (items != null)
                items.Clear();
            items = null;
            if (snapshots != null)
                snapshots.Clear();
            snapshots = null;
        }
        
        public bool IsSpecial(out SpecialEffects function)
        {
            function = SpecialEffects.None;
            return false;
        }

        public void SaveToDatabase(IQueryAdapter dbClient)
        {
            WiredUtillity.SaveTriggerItem(dbClient, (int)itemID, "integer", WiredMatchFurniSnapshot.EncodeFlags(matchState, matchDirection, matchPosition), delay.ToString(), false);

            snapshots.Clear();
            foreach (RoomItem item in items)
                snapshots[item.Id] = WiredMatchFurniSnapshot.FromItem(item);

            lock (items)
            {
                dbClient.runFastQuery("DELETE FROM trigger_in_place WHERE original_trigger = '" + this.itemID + "'"); 
                foreach (RoomItem i in items)
                {
                    WiredUtillity.SaveTrigger(dbClient, (int)itemID, (int)i.Id);
                }
            }

            WiredMatchFurniSnapshot.Save(dbClient, (int)itemID, snapshots.Values);
        }

        public void LoadFromDatabase(IQueryAdapter dbClient, Room insideRoom)
        {
            dbClient.setQuery("SELECT trigger_data, trigger_data_2 FROM trigger_item WHERE trigger_id = @id ");
            dbClient.addParameter("id", (int)this.itemID);
            DataRow dRow = dbClient.getRow();
            if (dRow != null)
            {
                this.delay = Convert.ToInt32(dRow[0].ToString());
                WiredMatchFurniSnapshot.DecodeFlags(dRow[1].ToString(), true, true, true, out matchState, out matchDirection, out matchPosition);
            }
            else
            {
                delay = 20;
                matchState = true;
                matchDirection = true;
                matchPosition = true;
            }

            dbClient.setQuery("SELECT triggers_item FROM trigger_in_place WHERE original_trigger = " + this.itemID);
            DataTable dTable = dbClient.getTable();
            RoomItem targetItem;
            foreach (DataRow dRows in dTable.Rows)
            {
                targetItem = insideRoom.GetRoomItemHandler().GetItem(Convert.ToUInt32(dRows[0]));
                if (targetItem == null || this.items.Contains(targetItem))
                    continue;
                this.items.Add(targetItem);
            }

            snapshots = WiredMatchFurniSnapshot.Load(dbClient, (int)itemID);
            if (snapshots.Count == 0)
            {
                foreach (RoomItem item in items)
                {
                    Point originalPosition = item.GetPlacementPosition();
                    snapshots[item.Id] = new WiredMatchFurniSnapshot(item.Id, item.originalExtraData != null ? item.originalExtraData.ToString() : string.Empty, item.Rot, originalPosition.X, originalPosition.Y);
                }
            }
        }

        public void DeleteFromDatabase(IQueryAdapter dbClient)
        {
            dbClient.runFastQuery("DELETE FROM trigger_item WHERE trigger_id = '" + this.itemID + "'");
            dbClient.runFastQuery("DELETE FROM trigger_in_place WHERE original_trigger = '" + this.itemID + "'");
            WiredMatchFurniSnapshot.Delete(dbClient, (int)itemID);
        }

        public bool Disposed()
        {
            return disposed;
        }

    }
}
