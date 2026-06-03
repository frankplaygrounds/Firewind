using System.Collections;
using System.Collections.Generic;
using Firewind.Messages;
using System.Linq;

namespace Firewind.HabboHotel.Catalogs
{
    class CatalogPage
    {
        private int Id;
        internal int ParentId;

        internal string Caption;

        internal bool Visible;
        internal bool Enabled;

        internal uint MinRank;
        internal bool ClubOnly;

        internal int IconColor;
        internal int IconImage;

        internal string Layout;

        internal string LayoutHeadline;
        internal string LayoutTeaser;
        internal string LayoutSpecial;

        internal string Text1;
        internal string Text2;
        internal string TextDetails;
        internal string TextTeaser;

        //internal List<CatalogItem> Items;
        internal Dictionary<uint, CatalogItem> Items;

        private ServerMessage mMessage;

        internal int PageId
        {
            get
            {
                return Id;
            }
        }

        internal CatalogPage(int Id, int ParentId, string Caption, bool Visible, bool Enabled,
            uint MinRank, bool ClubOnly, int IconColor, int IconImage, string Layout, string LayoutHeadline,
            string LayoutTeaser, string LayoutSpecial, string Text1, string Text2, string TextDetails,
            string TextTeaser, ref Hashtable CataItems)
        {
            Items = new Dictionary<uint, CatalogItem>();

            this.Id = Id;
            this.ParentId = ParentId;
            this.Caption = Caption;
            this.Visible = Visible;
            this.Enabled = Enabled;
            this.MinRank = MinRank;
            this.ClubOnly = ClubOnly;
            this.IconColor = IconColor;
            this.IconImage = IconImage;
            this.Layout = Layout;
            this.LayoutHeadline = LayoutHeadline;
            this.LayoutTeaser = LayoutTeaser;
            this.LayoutSpecial = LayoutSpecial;
            this.Text1 = Text1;
            this.Text2 = Text2;
            this.TextDetails = TextDetails;
            this.TextTeaser = TextTeaser;
            foreach (CatalogItem Item in CataItems.Values)
            {
                if (Item.PageID == Id)
                    Items.Add(Item.Id, Item);
            }
        }

        internal void InitMsg()
        {
            mMessage = Catalog.SerializePage(this);
        }

        internal CatalogItem GetItem(uint pId)
        {
            if (Items.ContainsKey(pId))
                return (CatalogItem)Items[pId];

            return null;
        }

        internal ServerMessage GetMessage
        {
            get
            {
                return mMessage;
            }
        }

        internal void Serialize(int Rank, ServerMessage Message)
        {
            Message.AppendBoolean(Visible);
            Message.AppendInt32(IconColor);
            Message.AppendInt32(IconImage);
            Message.AppendInt32(Id);
            Message.AppendString(Layout);
            Message.AppendString(Caption);
            Message.AppendInt32(ParentId == -1 ? FirewindEnvironment.GetGame().GetCatalog().GetTreeSize(Rank, Id) : 0);
        }
    }
}
