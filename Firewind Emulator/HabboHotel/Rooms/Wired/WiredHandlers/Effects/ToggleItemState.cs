using System.Collections;
using System.Collections.Generic;
using Firewind.HabboHotel.Items;
using Firewind.HabboHotel.Rooms.Games;
using Firewind.HabboHotel.Rooms.Wired.WiredHandlers.Interfaces;
using Database_Manager.Database.Session_Details.Interfaces;
using System.Data;
using System;

namespace Firewind.HabboHotel.Rooms.Wired.WiredHandlers.Effects
{
    class ToggleItemState: IWiredTrigger, IWiredCycleable, IWiredEffect
    {
        private RoomItem item;
        private Gamemap gamemap;
        private WiredHandler handler;

        private List<RoomItem> items;
        private int delay;
        private int cycles;
        private Queue delayedTriggeringUsers;

        private bool disposed;

        public ToggleItemState(Gamemap gamemap, WiredHandler handler, List<RoomItem> items, int delay, RoomItem Item)
        {
            this.item = Item;
            this.gamemap = gamemap;
            this.handler = handler;
            this.items = items;
            this.delay = delay;
            this.cycles = 0;
            this.delayedTriggeringUsers = new Queue();
            this.disposed = false;
        }

        public bool OnCycle()
        {
            if (disposed)
                return false;

            cycles++;
            if (cycles > delay)
            {
                if (delayedTriggeringUsers.Count > 0)
                {
                    lock (delayedTriggeringUsers.SyncRoot)
                    {
                        while (delayedTriggeringUsers.Count > 0)
                        {
                            RoomUser user = (RoomUser)delayedTriggeringUsers.Dequeue();
                            ToggleItems(user);
                        }
                    }
                }
                return false;
            }

            return true;
        }

        public bool Handle(RoomUser user, Team team, RoomItem i)
        {
            if (disposed)
                return false;
            cycles = 0;
            if (delay == 0 && user != null)
            {
                return ToggleItems(user);
            }
            else
            {
                lock (delayedTriggeringUsers.SyncRoot)
                {
                    delayedTriggeringUsers.Enqueue(user);
                }
                handler.RequestCycle(this);
            }
            return false;
        }

        private bool ToggleItems(RoomUser user)
        {
            if (disposed)
                return false;
            handler.OnEvent(item.Id);
            bool itemTriggered = false;
            //Logging.WriteLine("serialize action babe!");
            foreach (RoomItem i in items)
            {
                if (i == null)
                    continue;
                //Logging.WriteLine("do it!");
                if (user != null && user.GetClient() != null)
                    i.Interactor.OnTrigger(user.GetClient(), i, 0, true);
                else
                    i.Interactor.OnTrigger(null, i, 0, true);
                itemTriggered = true;
            }
            return itemTriggered;
        }

        public void Dispose()
        {
            disposed = true;
            gamemap = null;
            handler = null;
            if (items != null)
                items.Clear();
            delayedTriggeringUsers.Clear();
        }

        public bool IsSpecial(out SpecialEffects function)
        {
            function = SpecialEffects.None;
            return false;
        }

        public void SaveToDatabase(IQueryAdapter dbClient)
        {
            WiredUtillity.SaveTriggerItem(dbClient, (int)item.Id, "integer", string.Empty, delay.ToString(), false);
            lock (items)
            {
                dbClient.runFastQuery("DELETE FROM trigger_in_place WHERE original_trigger = '" + this.item.Id + "'"); 
                foreach (RoomItem i in items)
                {
                    WiredUtillity.SaveTrigger(dbClient, (int)item.Id, (int)i.Id);
                }
                //Logging.WriteLine("save trigger 'updatestate' items: " + items.Count);
            }
        }

        public void LoadFromDatabase(IQueryAdapter dbClient, Room insideRoom)
        {
            dbClient.setQuery("SELECT trigger_data FROM trigger_item WHERE trigger_id = @id ");
            dbClient.addParameter("id", (int)this.item.Id);
            DataRow dRow = dbClient.getRow();
            if (dRow != null)
                this.delay = Convert.ToInt32(dRow[0].ToString());
            else
                this.delay = 20;

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
        }

        public void DeleteFromDatabase(IQueryAdapter dbClient)
        {
            dbClient.runFastQuery("DELETE FROM trigger_item WHERE trigger_id = '" + this.item.Id + "'");
            dbClient.runFastQuery("DELETE FROM trigger_in_place WHERE original_trigger = '" + this.item.Id + "'");
        }

        public bool Disposed()
        {
            return disposed;
        }
    }
}
