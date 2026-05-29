using System;
using System.Collections.Generic;
using System.Data;
using Database_Manager.Database.Session_Details.Interfaces;
using Firewind.HabboHotel.Items;

namespace Firewind.HabboHotel.Rooms.Wired
{
    internal class WiredMatchFurniSnapshot
    {
        internal uint ItemId;
        internal string State;
        internal int Rotation;
        internal int X;
        internal int Y;

        internal WiredMatchFurniSnapshot(uint itemId, string state, int rotation, int x, int y)
        {
            ItemId = itemId;
            State = state ?? string.Empty;
            Rotation = rotation;
            X = x;
            Y = y;
        }

        internal static WiredMatchFurniSnapshot FromItem(RoomItem item)
        {
            return new WiredMatchFurniSnapshot(item.Id, item.data != null ? item.data.ToString() : string.Empty, item.Rot, item.GetX, item.GetY);
        }

        internal static string EncodeFlags(bool matchState, bool matchDirection, bool matchPosition)
        {
            return (matchState ? "1" : "0") + ";" + (matchDirection ? "1" : "0") + ";" + (matchPosition ? "1" : "0");
        }

        internal static void DecodeFlags(string flags, bool defaultState, bool defaultDirection, bool defaultPosition, out bool matchState, out bool matchDirection, out bool matchPosition)
        {
            matchState = defaultState;
            matchDirection = defaultDirection;
            matchPosition = defaultPosition;

            if (string.IsNullOrEmpty(flags))
                return;

            string[] parts = flags.Split(';');
            if (parts.Length > 0)
                matchState = parts[0] == "1";
            if (parts.Length > 1)
                matchDirection = parts[1] == "1";
            if (parts.Length > 2)
                matchPosition = parts[2] == "1";
        }

        internal static void EnsureTable(IQueryAdapter dbClient)
        {
            dbClient.runFastQuery("CREATE TABLE IF NOT EXISTS trigger_item_snapshot (trigger_id INT NOT NULL, item_id INT NOT NULL, item_state TEXT NOT NULL, rotation INT NOT NULL, x INT NOT NULL, y INT NOT NULL, PRIMARY KEY (trigger_id, item_id))");
        }

        internal static Dictionary<uint, WiredMatchFurniSnapshot> Load(IQueryAdapter dbClient, int triggerId)
        {
            Dictionary<uint, WiredMatchFurniSnapshot> snapshots = new Dictionary<uint, WiredMatchFurniSnapshot>();
            EnsureTable(dbClient);

            dbClient.setQuery("SELECT item_id, item_state, rotation, x, y FROM trigger_item_snapshot WHERE trigger_id = @trigger_id");
            dbClient.addParameter("trigger_id", triggerId);
            DataTable table = dbClient.getTable();
            if (table == null)
                return snapshots;

            foreach (DataRow row in table.Rows)
            {
                uint itemId = Convert.ToUInt32(row["item_id"]);
                snapshots[itemId] = new WiredMatchFurniSnapshot(
                    itemId,
                    row["item_state"].ToString(),
                    Convert.ToInt32(row["rotation"]),
                    Convert.ToInt32(row["x"]),
                    Convert.ToInt32(row["y"]));
            }

            return snapshots;
        }

        internal static void Save(IQueryAdapter dbClient, int triggerId, IEnumerable<WiredMatchFurniSnapshot> snapshots)
        {
            EnsureTable(dbClient);
            dbClient.runFastQuery("DELETE FROM trigger_item_snapshot WHERE trigger_id = " + triggerId);

            foreach (WiredMatchFurniSnapshot snapshot in snapshots)
            {
                dbClient.setQuery("REPLACE INTO trigger_item_snapshot (trigger_id, item_id, item_state, rotation, x, y) VALUES (@trigger_id, @item_id, @item_state, @rotation, @x, @y)");
                dbClient.addParameter("trigger_id", triggerId);
                dbClient.addParameter("item_id", (int)snapshot.ItemId);
                dbClient.addParameter("item_state", snapshot.State);
                dbClient.addParameter("rotation", snapshot.Rotation);
                dbClient.addParameter("x", snapshot.X);
                dbClient.addParameter("y", snapshot.Y);
                dbClient.runQuery();
            }
        }

        internal static void Delete(IQueryAdapter dbClient, int triggerId)
        {
            EnsureTable(dbClient);
            dbClient.runFastQuery("DELETE FROM trigger_item_snapshot WHERE trigger_id = " + triggerId);
        }
    }
}
