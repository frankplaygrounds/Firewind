using System;
using System.Collections.Generic;
using System.Data;
using Firewind.Core;
using Firewind.HabboHotel.Items;
using Database_Manager.Database.Session_Details.Interfaces;

namespace Firewind.HabboHotel.Items
{
    class ItemManager
    {
        private Dictionary<UInt32, Item> Items;

        internal ItemManager()
        {
            Items = new Dictionary<uint, Item>();
        }

        internal void LoadItems(IQueryAdapter dbClient)
        {
            Items = new Dictionary<uint, Item>();

            dbClient.setQuery("SELECT * FROM items_base");
            DataTable ItemData = dbClient.getTable();

            if (ItemData != null)
            {
                uint id;
                int spriteID;
                string itemName;
                string type;
                int width;
                int length;
                double height;
                bool allowStack;
                bool allowWalk;
                bool allowSit;
                bool allowRecycle;
                bool allowTrade;
                bool allowMarketplace;
                bool allowInventoryStack;
                InteractionType interactionType;
                int cycleCount;
                string vendingIDS;

                foreach (DataRow dRow in ItemData.Rows)
                {
                    try
                    {
                        id = Convert.ToUInt32(dRow["id"]);
                        spriteID = (int)dRow["sprite_id"];
                        itemName = (string)dRow["item_name"];
                        type = (string)dRow["type"];
                        width = (int)dRow["width"];
                        length = (int)dRow["length"];
                        height = Convert.ToDouble(dRow["stack_height"]);
                        allowStack = Convert.ToInt32(dRow["can_stack"]) == 1;
                        allowWalk = Convert.ToInt32(dRow["is_walkable"]) == 1;
                        allowSit = Convert.ToInt32(dRow["can_sit"]) == 1;
                        allowRecycle = Convert.ToInt32(dRow["allow_recycle"]) == 1;
                        allowTrade = Convert.ToInt32(dRow["allow_trade"]) == 1;
                        allowMarketplace = Convert.ToInt32(dRow["allow_marketplace_sell"]) == 1;
                        allowInventoryStack = Convert.ToInt32(dRow["allow_inventory_stack"]) == 1;
                        string interactionTypeName = (string)dRow["interaction_type"];
                        interactionType = InterractionTypes.GetTypeFromString(interactionTypeName);
                        if (interactionType == InteractionType.none && interactionTypeName == "wired")
                            interactionType = InterractionTypes.GetTypeFromString(itemName);
                        cycleCount = (int)dRow["interaction_modes_count"];
                        vendingIDS = (string)dRow["vending_ids"];

                        Item item = new Item(id, spriteID, itemName, type, width, length, height, allowStack, allowWalk, allowSit, allowRecycle, allowTrade, allowMarketplace, allowInventoryStack, interactionType, cycleCount, vendingIDS);
                        Items.Add(id, item);
                    }
                    catch (Exception e)
                    {
                        Logging.WriteLine(e.ToString());
                        Console.ReadKey();
                        Logging.WriteLine("Could not load item #" + Convert.ToUInt32(dRow[0]) + ", please verify the data is okay.");
                    }
                }
            }
        }

        internal Boolean ContainsItem(uint Id)
        {
            return Items.ContainsKey(Id);
        }

        internal Item GetItem(uint Id)
        {
            if (ContainsItem(Id))
                return Items[Id];

            return null;
        }
        internal Item GetItemBySpriteID(int spriteID)
        {
            foreach (Item item in Items.Values)
                if (item.SpriteId == spriteID)
                    return item;
            return null;
        }
    }
}
