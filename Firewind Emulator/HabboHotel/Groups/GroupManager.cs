using Firewind.HabboHotel.Groups.Types;
using Database_Manager.Database.Session_Details.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Firewind.HabboHotel.GameClients;
using Firewind.HabboHotel.Users;

namespace Firewind.HabboHotel.Groups
{
    class GroupManager
    {
        internal Dictionary<int, Group> Groups;
        private readonly string _groupsTable;
        private readonly Dictionary<int, int> _groupReferences;
        private readonly Queue _groupDeleteQueue;
        private DateTime lastCycle;

        internal GroupManager(IQueryAdapter dbClient)
        {
            this.lastCycle = DateTime.Now;
            this.Groups = new Dictionary<int, Group>();
            this._groupReferences = new Dictionary<int, int>();
            this._groupDeleteQueue = new Queue();
            this._groupsTable = ResolveGroupsTable(dbClient);

            LoadGroups(dbClient);

            FirewindEnvironment.GetGame().GetClientManager().OnLoggedInClient += GroupManager_OnLoggedInClient;
        }

        private static string ResolveGroupsTable(IQueryAdapter dbClient)
        {
            dbClient.setQuery("SHOW TABLES LIKE 'groups'");
            if (dbClient.findsResult())
                return "groups";

            dbClient.setQuery("SHOW TABLES LIKE 'groups_details'");
            if (dbClient.findsResult())
                return "groups_details";

            return "groups";
        }

        private string RoomIdColumn
        {
            get { return _groupsTable == "groups_details" ? "roomid" : "room_id"; }
        }

        private void GroupManager_OnLoggedInClient(GameClient client)
        {
            Habbo user = client.GetHabbo();
            if (user == null || user.Groups == null || user.Groups.Count == 0)
                return;

            client.GetConnection().connectionChanged += GroupManager_connectionChanged;

            foreach (int groupID in user.Groups)
            {
                if (GetGroup(groupID) == null)
                    continue;

                if (!_groupReferences.ContainsKey(groupID))
                    _groupReferences.Add(groupID, 1);
                else
                    _groupReferences[groupID]++;
            }
        }

        void GroupManager_connectionChanged(ConnectionManager.ConnectionInformation information, ConnectionManager.ConnectionState state)
        {
            if (state != ConnectionManager.ConnectionState.closed)
                return;

            GameClient client = FirewindEnvironment.GetGame().GetClientManager().GetClient((uint)information.getConnectionID());
            if (client == null)
                return;

            Habbo user = client.GetHabbo();
            if (user == null || user.Groups == null || user.Groups.Count == 0)
                return;

            lock (_groupDeleteQueue.SyncRoot)
            {
                foreach (int groupID in user.Groups)
                {
                    if (!_groupReferences.ContainsKey(groupID))
                        continue;

                    if (--_groupReferences[groupID] <= 0)
                    {
                        _groupReferences.Remove(groupID);
                        if (Groups.ContainsKey(groupID))
                            _groupDeleteQueue.Enqueue(groupID);
                    }
                }
            }
        }

        internal void OnCycle()
        {
            if ((DateTime.Now - lastCycle).TotalSeconds < 10)
                return;

            lastCycle = DateTime.Now;
            if (_groupDeleteQueue.Count <= 0)
                return;

            lock (_groupDeleteQueue.SyncRoot)
            {
                while (_groupDeleteQueue.Count > 0)
                {
                    int groupID = (int)_groupDeleteQueue.Dequeue();
                    if (Groups.ContainsKey(groupID))
                        Groups.Remove(groupID);
                }
            }
        }

        internal void LoadGroups(IQueryAdapter dbClient)
        {
            dbClient.setQuery("SELECT * FROM " + _groupsTable);
            DataTable groupTable = dbClient.getTable();

            foreach (DataRow groupRow in groupTable.Rows)
            {
                int groupId = Convert.ToInt32(groupRow["id"]);
                Group group = new Group(groupRow, LoadMemberships(dbClient, groupId));
                Groups[group.ID] = group;
            }
        }

        private DataTable LoadMemberships(IQueryAdapter dbClient, int groupId)
        {
            dbClient.setQuery("SELECT * FROM groups_memberships WHERE groupid = @id");
            dbClient.addParameter("id", groupId);
            return dbClient.getTable();
        }

        public Group GetGroup(int id)
        {
            if (id <= 0)
                return null;

            if (Groups.ContainsKey(id))
                return Groups[id];

            return LoadGroup(id) ? Groups[id] : null;
        }

        internal bool LoadGroup(int id)
        {
            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("SELECT * FROM " + _groupsTable + " WHERE id = @id LIMIT 1");
                dbClient.addParameter("id", id);
                DataRow row = dbClient.getRow();

                if (row == null || row.ItemArray.Length == 0)
                    return false;

                Group group = new Group(row, LoadMemberships(dbClient, id));
                Groups[id] = group;
                return true;
            }
        }

        public Group GetGroupForRoom(int roomId)
        {
            foreach (Group group in Groups.Values)
            {
                if (group.RoomID == roomId)
                    return group;
            }

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("SELECT id FROM " + _groupsTable + " WHERE " + RoomIdColumn + " = @roomid LIMIT 1");
                dbClient.addParameter("roomid", roomId);
                int groupId = dbClient.getInteger();
                return GetGroup(groupId);
            }
        }

        public Dictionary<int, Group> GetGroups()
        {
            return this.Groups;
        }

        public Group CreateGroup(GameClient creator, string name, string description, int roomID, int color1, int color2, List<Tuple<int, int, int>> badgeData, int guildBase, int guildBaseColor, List<int> guildStates)
        {
            if (GetGroupForRoom(roomID) != null)
                return null;

            int groupID;
            string badgeCode = Group.GenerateBadgeImage(badgeData);
            string createTime = DateTime.Now.ToString("d-M-yyyy");
            string stateData = SerializeGuildStates(guildStates);

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                if (_groupsTable == "groups_details")
                {
                    dbClient.setQuery("INSERT INTO groups_details(name,description,ownerid,roomid,created,badge,type,recommended,views,pane,topics) VALUES(@name,@desc,@ownerid,@roomid,@date,@badge,0,0,0,0,0)");
                }
                else
                {
                    dbClient.setQuery("INSERT INTO groups(name,badge,owner_id,owner_name,description,room_id,colour_one,colour_two,guild_base,guild_base_colour,guild_states,date_created,type,rights_type) VALUES(@name,@badge,@ownerid,@ownername,@desc,@roomid,@color1,@color2,@base,@basecolor,@states,@date,0,0)");
                    dbClient.addParameter("ownername", creator.GetHabbo().Username);
                    dbClient.addParameter("color1", color1);
                    dbClient.addParameter("color2", color2);
                    dbClient.addParameter("base", guildBase);
                    dbClient.addParameter("basecolor", guildBaseColor);
                    dbClient.addParameter("states", stateData);
                }

                dbClient.addParameter("name", name);
                dbClient.addParameter("desc", description);
                dbClient.addParameter("ownerid", creator.GetHabbo().Id);
                dbClient.addParameter("roomid", roomID);
                dbClient.addParameter("badge", badgeCode);
                dbClient.addParameter("date", createTime);
                groupID = (int)dbClient.insertQuery();

                dbClient.setQuery("UPDATE groups_memberships SET is_current = '0' WHERE userid = @userid");
                dbClient.addParameter("userid", creator.GetHabbo().Id);
                dbClient.runQuery();

                dbClient.setQuery("INSERT INTO groups_memberships(userid,groupid,member_rank,is_current,is_pending) VALUES(@userid,@groupid,'1','1','0')");
                dbClient.addParameter("userid", creator.GetHabbo().Id);
                dbClient.addParameter("groupid", groupID);
                dbClient.runQuery();
            }

            Group group = new Group()
            {
                ID = groupID,
                Name = name,
                Description = description,
                RoomID = roomID,
                ColorID1 = color1,
                ColorID2 = color2,
                GuildBase = guildBase,
                GuildBaseColor = guildBaseColor,
                GuildStates = guildStates,
                BadgeCode = badgeCode,
                DateCreated = createTime,
                OwnerID = (int)creator.GetHabbo().Id,
                Type = 0,
                RightsType = 0
            };
            group.Members.Add(creator.GetHabbo().Id);
            group.MemberRanks[creator.GetHabbo().Id] = 1;

            Groups[groupID] = group;
            return group;
        }

        public bool AddMember(Group group, Habbo user)
        {
            if (group == null || user == null)
                return false;

            if (group.Members.Contains(user.Id))
                return true;

            if (group.PendingMembers.Contains((int)user.Id))
                return true;

            bool makeFavourite = user.FavouriteGroup == 0;
            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                if (group.Type == 1)
                {
                    dbClient.setQuery("INSERT INTO groups_memberships(userid,groupid,member_rank,is_current,is_pending) VALUES(@userid,@groupid,'3','0','1')");
                    dbClient.addParameter("userid", user.Id);
                    dbClient.addParameter("groupid", group.ID);
                    dbClient.runQuery();

                    group.PendingMembers.Add((int)user.Id);
                    return true;
                }

                if (makeFavourite)
                {
                    dbClient.setQuery("UPDATE groups_memberships SET is_current = '0' WHERE userid = @userid");
                    dbClient.addParameter("userid", user.Id);
                    dbClient.runQuery();
                }

                dbClient.setQuery("INSERT INTO groups_memberships(userid,groupid,member_rank,is_current,is_pending) VALUES(@userid,@groupid,'3',@current,'0')");
                dbClient.addParameter("userid", user.Id);
                dbClient.addParameter("groupid", group.ID);
                dbClient.addParameter("current", makeFavourite ? "1" : "0");
                dbClient.runQuery();
            }

            group.Members.Add(user.Id);
            group.MemberRanks[user.Id] = 3;
            if (!user.Groups.Contains(group.ID))
                user.Groups.Add(group.ID);
            if (makeFavourite)
                user.FavouriteGroup = group.ID;

            return true;
        }

        public void UpdateIdentity(Group group, string name, string description)
        {
            if (group == null)
                return;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("UPDATE " + _groupsTable + " SET name = @name, description = @desc WHERE id = @id");
                dbClient.addParameter("name", name);
                dbClient.addParameter("desc", description);
                dbClient.addParameter("id", group.ID);
                dbClient.runQuery();
            }

            group.Name = name;
            group.Description = description;
        }

        public void UpdateColors(Group group, int color1, int color2)
        {
            if (group == null)
                return;

            group.SetColors(color1, color2);
            if (_groupsTable == "groups_details")
                return;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("UPDATE groups SET colour_one = @color1, colour_two = @color2, html_colour_one = @html1, html_colour_two = @html2 WHERE id = @id");
                dbClient.addParameter("color1", color1);
                dbClient.addParameter("color2", color2);
                dbClient.addParameter("html1", group.Color1);
                dbClient.addParameter("html2", group.Color2);
                dbClient.addParameter("id", group.ID);
                dbClient.runQuery();
            }
        }

        public void UpdateSettings(Group group, int type, int rightsType)
        {
            if (group == null)
                return;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                if (_groupsTable == "groups_details")
                    dbClient.setQuery("UPDATE groups_details SET type = @type WHERE id = @id");
                else
                    dbClient.setQuery("UPDATE groups SET type = @type, rights_type = @rights WHERE id = @id");

                dbClient.addParameter("type", type);
                dbClient.addParameter("rights", rightsType);
                dbClient.addParameter("id", group.ID);
                dbClient.runQuery();
            }

            group.Type = type;
            group.RightsType = rightsType;
        }

        public void UpdateBadge(Group group, List<Tuple<int, int, int>> badgeData, int guildBase, int guildBaseColor, List<int> guildStates)
        {
            if (group == null || badgeData == null || badgeData.Count == 0)
                return;

            string badgeCode = Group.GenerateBadgeImage(badgeData);
            string stateData = SerializeGuildStates(guildStates);

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                if (_groupsTable == "groups_details")
                {
                    dbClient.setQuery("UPDATE groups_details SET badge = @badge WHERE id = @id");
                }
                else
                {
                    dbClient.setQuery("UPDATE groups SET badge = @badge, guild_base = @base, guild_base_colour = @basecolor, guild_states = @states WHERE id = @id");
                    dbClient.addParameter("base", guildBase);
                    dbClient.addParameter("basecolor", guildBaseColor);
                    dbClient.addParameter("states", stateData);
                }

                dbClient.addParameter("badge", badgeCode);
                dbClient.addParameter("id", group.ID);
                dbClient.runQuery();
            }

            group.BadgeCode = badgeCode;
            group.GuildBase = guildBase;
            group.GuildBaseColor = guildBaseColor;
            group.GuildStates = guildStates;
        }

        public bool SetMemberRank(Group group, uint userId, int rank)
        {
            if (group == null || group.OwnerID == userId || !group.Members.Contains(userId))
                return false;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("UPDATE groups_memberships SET member_rank = @rank WHERE userid = @userid AND groupid = @groupid AND is_pending = '0'");
                dbClient.addParameter("rank", rank);
                dbClient.addParameter("userid", userId);
                dbClient.addParameter("groupid", group.ID);
                dbClient.runQuery();
            }

            group.MemberRanks[userId] = rank;
            return true;
        }

        public bool AcceptMember(Group group, uint userId)
        {
            if (group == null || group.Members.Contains(userId))
                return false;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("UPDATE groups_memberships SET is_pending = '0', member_rank = '3' WHERE userid = @userid AND groupid = @groupid");
                dbClient.addParameter("userid", userId);
                dbClient.addParameter("groupid", group.ID);
                dbClient.runQuery();
            }

            group.PendingMembers.Remove((int)userId);
            group.Members.Add(userId);
            group.MemberRanks[userId] = 3;

            Habbo user = FirewindEnvironment.getHabboForId(userId);
            if (user != null && !user.Groups.Contains(group.ID))
                user.Groups.Add(group.ID);

            return true;
        }

        public bool RemoveMember(Group group, uint userId)
        {
            if (group == null || group.OwnerID == userId)
                return false;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("DELETE FROM groups_memberships WHERE userid = @userid AND groupid = @groupid");
                dbClient.addParameter("userid", userId);
                dbClient.addParameter("groupid", group.ID);
                dbClient.runQuery();
            }

            group.Members.Remove(userId);
            group.PendingMembers.Remove((int)userId);
            group.MemberRanks.Remove(userId);

            Habbo user = FirewindEnvironment.getHabboForId(userId);
            if (user != null)
            {
                user.Groups.Remove(group.ID);
                if (user.FavouriteGroup == group.ID)
                    user.FavouriteGroup = 0;
            }

            return true;
        }

        public void SetFavouriteGroup(Habbo user, int groupId)
        {
            if (user == null || !user.Groups.Contains(groupId))
                return;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("UPDATE groups_memberships SET is_current = '0' WHERE userid = @userid");
                dbClient.addParameter("userid", user.Id);
                dbClient.runQuery();

                dbClient.setQuery("UPDATE groups_memberships SET is_current = '1' WHERE userid = @userid AND groupid = @groupid");
                dbClient.addParameter("userid", user.Id);
                dbClient.addParameter("groupid", groupId);
                dbClient.runQuery();
            }

            user.FavouriteGroup = groupId;
        }

        public void ClearFavouriteGroup(Habbo user)
        {
            if (user == null)
                return;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("UPDATE groups_memberships SET is_current = '0' WHERE userid = @userid");
                dbClient.addParameter("userid", user.Id);
                dbClient.runQuery();
            }

            user.FavouriteGroup = 0;
        }

        public List<Group> GetGroups(List<int> idList)
        {
            List<Group> groups = new List<Group>();
            if (idList == null)
                return groups;

            foreach (int id in idList)
            {
                Group g = GetGroup(id);
                if (g != null)
                    groups.Add(g);
            }

            return groups;
        }

        private static string SerializeGuildStates(List<int> guildStates)
        {
            if (guildStates == null || guildStates.Count == 0)
                return string.Empty;

            return string.Join(";", guildStates.ToArray());
        }
    }
}
