using System;
using System.Collections.Generic;
using Firewind.HabboHotel.Items;

namespace Firewind.HabboHotel.Items
{
    class Item
    {
        private uint Id;

        internal int SpriteId;

        internal string Name;
        internal char Type;

        internal int Width;
        internal int Length;
        internal double Height;

        internal bool Stackable;
        internal bool Walkable;
        internal bool IsSeat;

        internal bool AllowRecycle;
        internal bool AllowTrade;
        internal bool AllowMarketplaceSell;
        internal bool AllowInventoryStack;
        internal bool AllowGroupItem;

        internal InteractionType InteractionType;

        internal List<int> VendingIds;

        internal int Modes;

        

        internal uint ItemId
        {
            get
            {
                return Id;
            }
        }

        internal Item(UInt32 Id, int Sprite, string Name, string Type, int Width, int Length, double Height, bool Stackable, bool Walkable, bool IsSeat, bool AllowRecycle, bool AllowTrade, bool AllowMarketplaceSell, bool AllowInventoryStack, InteractionType InteractionType, int Modes, string VendingIds, bool AllowGroupItem = false)
        {
            this.Id = Id;
            this.SpriteId = Sprite;
            this.Name = Name;
            this.Type = char.Parse(Type);
            this.Width = Width;
            this.Length = Length;
            this.Height = Height;
            this.Stackable = Stackable;
            this.Walkable = Walkable;
            this.IsSeat = IsSeat;
            this.AllowRecycle = AllowRecycle;
            this.AllowTrade = AllowTrade;
            this.AllowMarketplaceSell = AllowMarketplaceSell;
            this.AllowInventoryStack = AllowInventoryStack;
            this.AllowGroupItem = AllowGroupItem;
            this.InteractionType = InteractionType;
            this.Modes = Modes;
            this.VendingIds = new List<int>();
            if (!string.IsNullOrWhiteSpace(VendingIds))
            {
                foreach (string VendingId in VendingIds.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int parsedVendingId;
                    if (int.TryParse(VendingId.Trim(), out parsedVendingId) && parsedVendingId > 0)
                    {
                        this.VendingIds.Add(parsedVendingId);
                    }
                }
            }
        }
    }
}
