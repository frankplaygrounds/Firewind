using Firewind.HabboHotel.Rooms;
using Firewind.HabboHotel.Users;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Firewind.HabboHotel.Groups.Types
{
    class Group
    {
        private Habbo _owner;
        private RoomData _room;
        private string _color1;
        private string _color2;

        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string BadgeCode { get; set; }
        public string DateCreated { get; set; }

        public int OwnerID { get; set; }
        public string OwnerName
        {
            get
            {
                if (_owner == null)
                    _owner = FirewindEnvironment.getHabboForId((uint)OwnerID);
                return _owner != null ? _owner.Username : string.Empty;
            }
        }

        public int RoomID { get; set; }
        public string RoomName
        {
            get
            {
                if (_room == null)
                    _room = FirewindEnvironment.GetGame().GetRoomManager().GenerateRoomData((uint)RoomID);
                return _room != null ? _room.Name : string.Empty;
            }
        }

        public int ColorID1 { get; set; }
        public int ColorID2 { get; set; }
        public int GuildBase { get; set; }
        public int GuildBaseColor { get; set; }
        public List<int> GuildStates { get; set; }

        public string Color1
        {
            get
            {
                if (_color1 == null)
                    _color1 = GetColor(ColorID1);
                return _color1;
            }
            set
            {
                ColorID1 = GetColorId(value);
                _color1 = value;
            }
        }

        public string Color2
        {
            get
            {
                if (_color2 == null)
                    _color2 = GetColor(ColorID2);
                return _color2;
            }
            set
            {
                ColorID2 = GetColorId(value);
                _color2 = value;
            }
        }

        public List<int> PendingMembers { get; set; }
        public List<uint> Members { get; set; }
        public Dictionary<uint, int> MemberRanks { get; set; }

        public int Type { get; set; }
        public int RightsType { get; set; }

        public Group(DataRow data, DataTable members)
            : this()
        {
            ID = GetInt(data, "id");
            Name = GetString(data, "name");
            Description = GetString(data, "description");
            BadgeCode = GetString(data, "badge");
            DateCreated = GetString(data, "date_created", "created");
            OwnerID = GetInt(data, "owner_id", "users_id", "ownerid");
            RoomID = GetInt(data, "room_id", "rooms_id", "roomid");
            ColorID1 = GetInt(data, "colour_one", "color1");
            ColorID2 = GetInt(data, "colour_two", "color2");
            GuildBase = GetInt(data, "guild_base");
            GuildBaseColor = GetInt(data, "guild_base_colour", "guild_base_color");
            GuildStates = ParseGuildStates(GetString(data, "guild_states"));
            Type = GetInt(data, "type");
            RightsType = GetInt(data, "rights_type");

            foreach (DataRow member in members.Rows)
            {
                uint userId = Convert.ToUInt32(GetInt(member, "userid", "user_id"));
                int rank = GetInt(member, "member_rank", "rank");
                bool isPending = GetString(member, "is_pending") == "1";

                if (isPending)
                    PendingMembers.Add((int)userId);
                else if (!Members.Contains(userId))
                    Members.Add(userId);

                MemberRanks[userId] = rank;
            }

            if (OwnerID > 0 && !Members.Contains((uint)OwnerID))
            {
                Members.Add((uint)OwnerID);
                MemberRanks[(uint)OwnerID] = 1;
            }
        }

        public Group()
        {
            Name = string.Empty;
            Description = string.Empty;
            BadgeCode = string.Empty;
            DateCreated = string.Empty;
            PendingMembers = new List<int>();
            Members = new List<uint>();
            MemberRanks = new Dictionary<uint, int>();
            GuildStates = new List<int>();
        }

        public void SetColors(int color1, int color2)
        {
            ColorID1 = color1;
            ColorID2 = color2;
            _color1 = null;
            _color2 = null;
        }

        public static string GenerateBadgeImage(List<Tuple<int, int, int>> parts)
        {
            if (parts == null || parts.Count == 0)
                return string.Empty;

            StringBuilder image = new StringBuilder();
            image.Append("b");
            image.Append(parts[0].Item1.ToString("D2"));
            image.Append(parts[0].Item2.ToString("D2"));

            for (int i = 1; i < parts.Count; i++)
            {
                Tuple<int, int, int> part = parts[i];
                if (part.Item1 == 0)
                    continue;

                image.Append("s");
                image.Append((part.Item1 - 20).ToString("D2"));
                image.Append(part.Item2.ToString("D2"));
                image.Append(part.Item3);
            }

            return image.ToString();
        }

        private static string GetColor(int id)
        {
            GuildsPartsData data = FindColor(id);
            return data != null ? data.ExtraData1 : "FFFFFF";
        }

        private static int GetColorId(string color)
        {
            if (string.IsNullOrEmpty(color))
                return 0;

            GuildsPartsData data = FindColor(color);
            return data != null ? data.Id : 0;
        }

        private static GuildsPartsData FindColor(int id)
        {
            GuildsPartsData data = GuildsPartsData.ColorBadges1.Find(t => t.Id == id);
            if (data != null)
                return data;

            data = GuildsPartsData.ColorBadges2.Find(t => t.Id == id);
            if (data != null)
                return data;

            return GuildsPartsData.ColorBadges3.Find(t => t.Id == id);
        }

        private static GuildsPartsData FindColor(string color)
        {
            GuildsPartsData data = GuildsPartsData.ColorBadges1.Find(t => t.ExtraData1 == color);
            if (data != null)
                return data;

            data = GuildsPartsData.ColorBadges2.Find(t => t.ExtraData1 == color);
            if (data != null)
                return data;

            return GuildsPartsData.ColorBadges3.Find(t => t.ExtraData1 == color);
        }

        private static List<int> ParseGuildStates(string data)
        {
            List<int> states = new List<int>();
            if (string.IsNullOrEmpty(data))
                return states;

            foreach (string part in data.Split(';'))
            {
                int value;
                if (int.TryParse(part, out value))
                    states.Add(value);
            }

            return states;
        }

        private static int GetInt(DataRow row, params string[] names)
        {
            object value = GetValue(row, names);
            return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
        }

        private static string GetString(DataRow row, params string[] names)
        {
            object value = GetValue(row, names);
            return value == null || value == DBNull.Value ? string.Empty : value.ToString();
        }

        private static object GetValue(DataRow row, params string[] names)
        {
            foreach (string name in names)
            {
                if (row.Table.Columns.Contains(name))
                    return row[name];
            }

            return null;
        }
    }
}
