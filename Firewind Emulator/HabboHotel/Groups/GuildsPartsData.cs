using Database_Manager.Database.Session_Details.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Firewind.HabboHotel.Groups
{
    internal class GuildsPartsData
    {
        internal static List<GuildsPartsData> BaseBadges;
        internal static List<GuildsPartsData> ColorBadges1;
        internal static List<GuildsPartsData> ColorBadges2;
        internal static List<GuildsPartsData> ColorBadges3;
        internal string ExtraData1;
        internal string ExtraData2;
        internal int Id;
        internal static List<GuildsPartsData> SymbolBadges;

        internal static void InitGroups()
        {
            BaseBadges = new List<GuildsPartsData>();
            SymbolBadges = new List<GuildsPartsData>();
            ColorBadges1 = new List<GuildsPartsData>();
            ColorBadges2 = new List<GuildsPartsData>();
            ColorBadges3 = new List<GuildsPartsData>();
            using (IQueryAdapter adapter = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                adapter.setQuery("SELECT * FROM groups_elements");
                DataTable table = adapter.getTable();
                foreach (DataRow row in table.Rows)
                {
                    GuildsPartsData data;
                    if (row["Type"].ToString() == "Base")
                    {
                        data = new GuildsPartsData
                        {
                            Id = (int)row["Id"],
                            ExtraData1 = (string)row["ExtraData1"],
                            ExtraData2 = (string)row["ExtraData2"]
                        };
                        BaseBadges.Add(data);
                    }
                    else if (row["ExtraData1"].ToString().StartsWith("symbol_"))
                    {
                        data = new GuildsPartsData
                        {
                            Id = (int)row["Id"],
                            ExtraData1 = (string)row["ExtraData1"],
                            ExtraData2 = (string)row["ExtraData2"]
                        };
                        SymbolBadges.Add(data);
                    }
                    else if (row["Type"].ToString() == "Color1")
                    {
                        data = new GuildsPartsData
                        {
                            Id = (int)row["Id"],
                            ExtraData1 = (string)row["ExtraData1"]
                        };
                        ColorBadges1.Add(data);
                    }
                    else if (row["Type"].ToString() == "Color2")
                    {
                        data = new GuildsPartsData
                        {
                            Id = (int)row["Id"],
                            ExtraData1 = (string)row["ExtraData1"]
                        };
                        ColorBadges2.Add(data);
                    }
                    else if (row["Type"].ToString() == "Color3")
                    {
                        data = new GuildsPartsData
                        {
                            Id = (int)row["Id"],
                            ExtraData1 = (string)row["ExtraData1"]
                        };
                        ColorBadges3.Add(data);
                    }
                }

                if (table.Rows.Count == 0)
                    AddFallbackParts();
            }
        }

        private static void AddFallbackParts()
        {
            BaseBadges.Add(new GuildsPartsData { Id = 1, ExtraData1 = "base_basic_1.gif", ExtraData2 = "" });
            SymbolBadges.Add(new GuildsPartsData { Id = 2, ExtraData1 = "symbol_background_1.gif", ExtraData2 = "" });
            ColorBadges1.Add(new GuildsPartsData { Id = 3, ExtraData1 = "ffffff" });
            ColorBadges2.Add(new GuildsPartsData { Id = 4, ExtraData1 = "ffffff" });
            ColorBadges3.Add(new GuildsPartsData { Id = 5, ExtraData1 = "ffffff" });
        }
    }
}
