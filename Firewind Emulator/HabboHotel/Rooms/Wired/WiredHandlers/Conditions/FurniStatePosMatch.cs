using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Firewind.HabboHotel.Rooms.Wired.WiredHandlers.Interfaces;
using Firewind.HabboHotel.Items;
using Database_Manager.Database.Session_Details.Interfaces;
using System.Data;
using System.Drawing;

namespace Firewind.HabboHotel.Rooms.Wired.WiredHandlers.Conditions
{
    class FurniStatePosMatch : IWiredCondition, IWiredMatchFurni
    {
        private RoomItem item;
        private List<RoomItem> items;
        private Dictionary<uint, WiredMatchFurniSnapshot> snapshots;
        private bool matchState;
        private bool matchDirection;
        private bool matchPosition;
        private bool isDisposed;

        public FurniStatePosMatch(RoomItem item, List<RoomItem> items)
            : this(item, items, true, true, true)
        {
        }

        public FurniStatePosMatch(RoomItem item, List<RoomItem> items, bool matchState, bool matchDirection, bool matchPosition)
        {
            this.item = item;
            this.items = items;
            this.snapshots = new Dictionary<uint, WiredMatchFurniSnapshot>();
            this.matchState = matchState;
            this.matchDirection = matchDirection;
            this.matchPosition = matchPosition;
            this.isDisposed = false;
        }

        public bool AllowsExecution(RoomUser user)
        {
            foreach (RoomItem item in items)
            {
                WiredMatchFurniSnapshot snapshot = GetSnapshot(item);
                if (snapshot == null)
                    continue;

                if (matchState && (item.data == null || item.data.ToString() != snapshot.State))
                    return false;

                if (matchPosition && (item.GetX != snapshot.X || item.GetY != snapshot.Y))
                    return false;

                if (matchDirection && item.Rot != snapshot.Rotation)
                    return false;
            }

            return true;
        }

        public bool ApplySnapshot(Room room)
        {
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
                        room.GetRoomItemHandler().SetFloorItem(null, item, newX, newY, newRot, false, false, true, true))
                        itemChanged = true;
                }
            }

            return itemChanged;
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

        public void SaveToDatabase(IQueryAdapter dbClient)
        {
            WiredUtillity.SaveTriggerItem(dbClient, (int)item.Id, "integer", WiredMatchFurniSnapshot.EncodeFlags(matchState, matchDirection, matchPosition), string.Empty, false);

            snapshots.Clear();
            foreach (RoomItem selectedItem in items)
                snapshots[selectedItem.Id] = WiredMatchFurniSnapshot.FromItem(selectedItem);

            lock (items)
            {
                dbClient.runFastQuery("DELETE FROM trigger_in_place WHERE original_trigger = '" + this.item.Id + "'");
                foreach (RoomItem i in items)
                {
                    WiredUtillity.SaveTrigger(dbClient, (int)item.Id, (int)i.Id);
                }
            }

            WiredMatchFurniSnapshot.Save(dbClient, (int)item.Id, snapshots.Values);
        }

        public void LoadFromDatabase(IQueryAdapter dbClient, Room insideRoom)
        {
            dbClient.setQuery("SELECT trigger_data_2 FROM trigger_item WHERE trigger_id = @id ");
            dbClient.addParameter("id", (int)this.item.Id);
            DataRow dataRow = dbClient.getRow();
            if (dataRow != null)
                WiredMatchFurniSnapshot.DecodeFlags(dataRow[0].ToString(), true, true, true, out matchState, out matchDirection, out matchPosition);

            dbClient.setQuery("SELECT triggers_item FROM trigger_in_place WHERE original_trigger = " + this.item.Id);
            DataTable dTable = dbClient.getTable();
            RoomItem targetItem;
            foreach (DataRow dRows in dTable.Rows)
            {
                targetItem = insideRoom.GetRoomItemHandler().GetItem(Convert.ToUInt32(dRows[0]));
                if (targetItem == null || this.items.Contains(targetItem))
                    continue;
                this.items.Add(targetItem);
            }

            snapshots = WiredMatchFurniSnapshot.Load(dbClient, (int)item.Id);
            if (snapshots.Count == 0)
            {
                foreach (RoomItem selectedItem in items)
                {
                    Point originalPosition = selectedItem.GetPlacementPosition();
                    snapshots[selectedItem.Id] = new WiredMatchFurniSnapshot(selectedItem.Id, selectedItem.originalExtraData != null ? selectedItem.originalExtraData.ToString() : string.Empty, selectedItem.Rot, originalPosition.X, originalPosition.Y);
                }
            }
        }

        public void DeleteFromDatabase(IQueryAdapter dbClient)
        {
            dbClient.runFastQuery("DELETE FROM trigger_item WHERE trigger_id = '" + this.item.Id + "'");
            dbClient.runFastQuery("DELETE FROM trigger_in_place WHERE original_trigger = '" + this.item.Id + "'");
            WiredMatchFurniSnapshot.Delete(dbClient, (int)item.Id);
        }

        public void Dispose()
        {
            isDisposed = true;
            item = null;
            if (items != null)
                items.Clear();
            items = null;
            if (snapshots != null)
                snapshots.Clear();
            snapshots = null;
        }

        public bool Disposed()
        {
            return isDisposed;
        }
    }
}
