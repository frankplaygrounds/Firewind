using System;
using System.Collections.Generic;
using System.Linq;
using Database_Manager.Database.Session_Details.Interfaces;
using Firewind.HabboHotel.Groups;
using Firewind.HabboHotel.Groups.Types;
using Firewind.HabboHotel.Rooms;
using Firewind.HabboHotel.Users;
using HabboEvents;

namespace Firewind.Messages
{
    internal partial class GameClientMessageHandler
    {
        public const int GROUP_CREDIT_COST = 10;

        public void CreateGuild()
        {
            string name = FirewindEnvironment.FilterInjectionChars(Request.ReadString());
            string desc = FirewindEnvironment.FilterInjectionChars(Request.ReadString());
            int roomID = Request.ReadInt32();
            int color1 = Request.ReadInt32();
            int color2 = Request.ReadInt32();
            GroupBadgeInfo badgeInfo = ReadGroupBadgeInfo();

            if (!Session.GetHabbo().UsersRooms.Exists(t => t.Id == roomID))
                return;

            Group group = FirewindEnvironment.GetGame().GetGroupManager().CreateGroup(Session, name, desc, roomID, color1, color2, badgeInfo.BadgeData, badgeInfo.Base, badgeInfo.BaseColor, badgeInfo.States);
            if (group == null)
                return;

            if (!Session.GetHabbo().Groups.Contains(group.ID))
                Session.GetHabbo().Groups.Add(group.ID);
            Session.GetHabbo().FavouriteGroup = group.ID;

            Response.Init(Outgoing.GroupCreated);
            Response.AppendInt32(group.RoomID);
            Response.AppendInt32(group.ID);
            SendResponse();

            SendGroupList(Outgoing.HabboGroupsWhereMember);
            SendFavouriteGroupUpdate(group.ID);
            RefreshRoomUserGroup();
        }

        public void UpdateGuildBadge()
        {
            int guildID = Request.ReadInt32();
            Group group = FirewindEnvironment.GetGame().GetGroupManager().GetGroup(guildID);
            if (!CanManageGroup(group))
                return;

            GroupBadgeInfo badgeInfo = ReadGroupBadgeInfo();
            FirewindEnvironment.GetGame().GetGroupManager().UpdateBadge(group, badgeInfo.BadgeData, badgeInfo.Base, badgeInfo.BaseColor, badgeInfo.States);
            SendGroupDetails(group);
        }

        public void StartGuildPurchase()
        {
            List<RoomData> availableRooms = Session.GetHabbo().UsersRooms.FindAll(s => s.GroupID == 0);

            Response.Init(Outgoing.PurchaseGuildInfo);
            Response.AppendInt32(GROUP_CREDIT_COST);

            Response.AppendInt32(availableRooms.Count);
            foreach (RoomData data in availableRooms)
            {
                Response.AppendInt32((int)data.Id);
                Response.AppendString(data.Name);
                Response.AppendBoolean(false);
            }

            Response.AppendInt32(5);

            Response.AppendInt32(10);
            Response.AppendInt32(3);
            Response.AppendInt32(4);

            Response.AppendInt32(19);
            Response.AppendInt32(11);
            Response.AppendInt32(5);

            Response.AppendInt32(19);
            Response.AppendInt32(1);
            Response.AppendInt32(3);

            Response.AppendInt32(29);
            Response.AppendInt32(11);
            Response.AppendInt32(4);

            Response.AppendInt32(0);
            Response.AppendInt32(0);
            Response.AppendInt32(0);

            SendResponse();
            SendGroupBadgeParts();
        }

        public void GetGroupBadgeParts()
        {
            SendGroupBadgeParts();
        }

        public void GetGuildInfo()
        {
            int groupID = Request.ReadInt32();
            bool unknownFlag = Request.ReadBoolean();

            Group group = FirewindEnvironment.GetGame().GetGroupManager().GetGroup(groupID);
            if (group == null)
                return;

            SendGroupDetails(group);
        }

        public void GetHabboGroupsWhereMember()
        {
            SendGroupList(Outgoing.HabboGroupsWhereMember);
        }

        public void GetGuildFurniInfo()
        {
            SendGroupList(Outgoing.GuildFurniInfo);
        }

        public void GetGuildManageInfo()
        {
            int groupID = Request.ReadInt32();
            Group group = FirewindEnvironment.GetGame().GetGroupManager().GetGroup(groupID);
            if (group == null)
                return;

            Response.Init(Outgoing.GuildEditInfo);
            List<RoomData> availableRooms = Session.GetHabbo().UsersRooms.FindAll(s => s.GroupID == 0 || s.Id == group.RoomID);
            Response.AppendInt32(availableRooms.Count);
            foreach (RoomData data in availableRooms)
            {
                Response.AppendInt32((int)data.Id);
                Response.AppendString(data.Name);
                Response.AppendBoolean(data.Id == group.RoomID);
            }

            Response.AppendBoolean(true);
            Response.AppendInt32(group.ID);
            Response.AppendString(group.Name);
            Response.AppendString(group.Description);
            Response.AppendInt32(group.RoomID);
            Response.AppendInt32(group.ColorID1);
            Response.AppendInt32(group.ColorID2);
            Response.AppendInt32(group.Type);
            Response.AppendInt32(group.RightsType);
            Response.AppendBoolean(false);
            Response.AppendString(string.Empty);
            Response.AppendInt32(5);
            Response.AppendInt32(group.GuildBase);
            Response.AppendInt32(group.GuildBaseColor);
            Response.AppendInt32(4);
            AppendGuildStates(group);
            Response.AppendString(group.BadgeCode);
            Response.AppendInt32(group.Members.Count);
            Response.AppendInt32(group.PendingMembers.Count);
            SendResponse();
        }

        public void JoinGroup()
        {
            int groupID = Request.ReadInt32();
            Group group = FirewindEnvironment.GetGame().GetGroupManager().GetGroup(groupID);
            if (group == null)
                return;

            if (FirewindEnvironment.GetGame().GetGroupManager().AddMember(group, Session.GetHabbo()))
            {
                SendGroupDetails(group);
                if (group.Members.Contains(Session.GetHabbo().Id))
                {
                    if (Session.GetHabbo().FavouriteGroup == group.ID)
                        SendFavouriteGroupUpdate(group.ID);
                    SendGroupList(Outgoing.HabboGroupsWhereMember);
                    RefreshRoomUserGroup();
                }
            }
        }

        public void SelectFavouriteHabboGroup()
        {
            int groupID = Request.ReadInt32();
            FirewindEnvironment.GetGame().GetGroupManager().SetFavouriteGroup(Session.GetHabbo(), groupID);
            SendFavouriteGroupUpdate(groupID);
            SendGroupList(Outgoing.HabboGroupsWhereMember);
            RefreshRoomUserGroup();
        }

        public void DeselectFavouriteHabboGroup()
        {
            FirewindEnvironment.GetGame().GetGroupManager().ClearFavouriteGroup(Session.GetHabbo());
            SendFavouriteGroupUpdate(0);
            SendGroupList(Outgoing.HabboGroupsWhereMember);
            RefreshRoomUserGroup();
        }

        public void GetGroupMemberList()
        {
            int groupID = Request.ReadInt32();
            int page = Request.RemainingLength >= 4 ? Request.ReadInt32() : 0;
            string search = Request.RemainingLength > 2 ? Request.ReadString() : string.Empty;
            int requestType = Request.RemainingLength >= 4 ? Request.ReadInt32() : 0;

            Group group = FirewindEnvironment.GetGame().GetGroupManager().GetGroup(groupID);
            if (group == null)
                return;

            SendGroupMemberList(group, page, search, requestType);
        }

        public void PromoteGroupMember()
        {
            int groupID = Request.ReadInt32();
            uint userID = Request.ReadUInt32();
            Group group = FirewindEnvironment.GetGame().GetGroupManager().GetGroup(groupID);
            if (!CanManageGroup(group))
                return;

            FirewindEnvironment.GetGame().GetGroupManager().SetMemberRank(group, userID, 1);
            SendGroupMemberList(group, 0, string.Empty, 0);
        }

        public void DemoteGroupMember()
        {
            int groupID = Request.ReadInt32();
            uint userID = Request.ReadUInt32();
            Group group = FirewindEnvironment.GetGame().GetGroupManager().GetGroup(groupID);
            if (!CanManageGroup(group))
                return;

            FirewindEnvironment.GetGame().GetGroupManager().SetMemberRank(group, userID, 3);
            SendGroupMemberList(group, 0, string.Empty, 0);
        }

        public void KickGroupMember()
        {
            int groupID = Request.ReadInt32();
            uint userID = Request.ReadUInt32();
            Group group = FirewindEnvironment.GetGame().GetGroupManager().GetGroup(groupID);
            if (!CanManageGroup(group))
                return;

            FirewindEnvironment.GetGame().GetGroupManager().RemoveMember(group, userID);
            SendGroupMemberList(group, 0, string.Empty, 0);
        }

        public void AcceptMembershipRequest()
        {
            int groupID = Request.ReadInt32();
            uint userID = Request.ReadUInt32();
            Group group = FirewindEnvironment.GetGame().GetGroupManager().GetGroup(groupID);
            if (!CanManageGroup(group))
                return;

            FirewindEnvironment.GetGame().GetGroupManager().AcceptMember(group, userID);
            SendGroupMemberList(group, 0, string.Empty, 2);
        }

        public void DeclineMembershipRequest()
        {
            int groupID = Request.ReadInt32();
            uint userID = Request.ReadUInt32();
            Group group = FirewindEnvironment.GetGame().GetGroupManager().GetGroup(groupID);
            if (!CanManageGroup(group))
                return;

            FirewindEnvironment.GetGame().GetGroupManager().RemoveMember(group, userID);
            SendGroupMemberList(group, 0, string.Empty, 2);
        }

        public void UpdateGuildIdentity()
        {
            int groupID = Request.ReadInt32();
            string name = FirewindEnvironment.FilterInjectionChars(Request.ReadString());
            string description = FirewindEnvironment.FilterInjectionChars(Request.ReadString());
            Group group = FirewindEnvironment.GetGame().GetGroupManager().GetGroup(groupID);
            if (!CanManageGroup(group))
                return;

            FirewindEnvironment.GetGame().GetGroupManager().UpdateIdentity(group, name, description);
            SendGroupDetails(group);
        }

        public void UpdateGuildColors()
        {
            int groupID = Request.ReadInt32();
            int color1 = Request.ReadInt32();
            int color2 = Request.ReadInt32();
            Group group = FirewindEnvironment.GetGame().GetGroupManager().GetGroup(groupID);
            if (!CanManageGroup(group))
                return;

            FirewindEnvironment.GetGame().GetGroupManager().UpdateColors(group, color1, color2);
            SendGroupList(Outgoing.HabboGroupsWhereMember);
        }

        public void UpdateGuildSettings()
        {
            int groupID = Request.ReadInt32();
            int type = Request.ReadInt32();
            int rightsType = Request.ReadInt32();
            Group group = FirewindEnvironment.GetGame().GetGroupManager().GetGroup(groupID);
            if (!CanManageGroup(group))
                return;

            FirewindEnvironment.GetGame().GetGroupManager().UpdateSettings(group, type, rightsType);
            SendGroupDetails(group);
        }

        public void FindHotGroups()
        {
            Response.Init(Outgoing.NavigatorPacket);
            Response.AppendInt32(8);
            Response.AppendString(string.Empty);
            Response.AppendInt32(0);
            Response.AppendBoolean(false);
            SendResponse();
        }

        private void SendGroupMemberList(Group group, int page, string search, int requestType)
        {
            List<uint> members = requestType == 2 ? group.PendingMembers.Select(userID => (uint)userID).ToList() : group.Members.ToList();
            if (!string.IsNullOrEmpty(search))
            {
                members = members.FindAll(userID =>
                {
                    Habbo habbo = FirewindEnvironment.getHabboForId(userID);
                    return habbo != null && habbo.Username.ToLower().Contains(search.ToLower());
                });
            }

            if (requestType == 1)
            {
                members = members.FindAll(userID => group.MemberRanks.ContainsKey(userID) && group.MemberRanks[userID] <= 1);
            }

            List<Tuple<uint, Habbo>> memberData = new List<Tuple<uint, Habbo>>();
            foreach (uint userID in members)
            {
                Habbo habbo = FirewindEnvironment.getHabboForId(userID);
                if (habbo != null)
                    memberData.Add(new Tuple<uint, Habbo>(userID, habbo));
            }

            int pageSize = 14;
            List<Tuple<uint, Habbo>> pageMembers = memberData.Skip(page * pageSize).Take(pageSize).ToList();

            Response.Init(Outgoing.GroupMemberList);
            Response.AppendInt32(group.ID);
            Response.AppendString(group.Name);
            Response.AppendInt32(group.RoomID);
            Response.AppendString(group.BadgeCode);
            Response.AppendInt32(memberData.Count);
            Response.AppendInt32(pageMembers.Count);

            foreach (Tuple<uint, Habbo> member in pageMembers)
            {
                uint userID = member.Item1;
                Habbo habbo = member.Item2;
                int rank = group.OwnerID == userID ? 0 : (group.MemberRanks.ContainsKey(userID) ? group.MemberRanks[userID] : 3);
                Response.AppendInt32(rank);
                Response.AppendUInt(userID);
                Response.AppendString(habbo.Username);
                Response.AppendString(habbo.Look);
                Response.AppendString(group.DateCreated);
            }

            Response.AppendBoolean(group.OwnerID == Session.GetHabbo().Id || (group.MemberRanks.ContainsKey(Session.GetHabbo().Id) && group.MemberRanks[Session.GetHabbo().Id] <= 1));
            Response.AppendInt32(pageSize);
            Response.AppendInt32(page);
            Response.AppendInt32(requestType);
            Response.AppendString(search);
            SendResponse();
        }

        private void SendGroupDetails(Group group)
        {
            bool isMember = group.Members.Contains(Session.GetHabbo().Id);
            bool isPending = group.PendingMembers.Contains((int)Session.GetHabbo().Id);
            bool isAdmin = group.OwnerID == Session.GetHabbo().Id ||
                (group.MemberRanks.ContainsKey(Session.GetHabbo().Id) && group.MemberRanks[Session.GetHabbo().Id] <= 1);

            Response.Init(Outgoing.HabboGroupDetails);
            Response.AppendInt32(group.ID);
            Response.AppendBoolean(isMember);
            Response.AppendInt32(group.Type);
            Response.AppendString(group.Name);
            Response.AppendString(group.Description);
            Response.AppendString(group.BadgeCode);
            Response.AppendInt32(group.RoomID);
            Response.AppendString(group.RoomName);
            Response.AppendInt32(isPending ? 2 : (isMember ? 1 : 0));
            Response.AppendInt32(group.Members.Count);
            Response.AppendBoolean(group.ID == Session.GetHabbo().FavouriteGroup);
            Response.AppendString(group.DateCreated);
            Response.AppendBoolean(group.OwnerID == Session.GetHabbo().Id);
            Response.AppendBoolean(isAdmin);
            Response.AppendString(group.OwnerName);
            Response.AppendBoolean(true);
            Response.AppendBoolean(true);
            Response.AppendInt32(group.PendingMembers.Count);
            SendResponse();
        }

        private bool CanManageGroup(Group group)
        {
            return group != null && (group.OwnerID == Session.GetHabbo().Id ||
                (group.MemberRanks.ContainsKey(Session.GetHabbo().Id) && group.MemberRanks[Session.GetHabbo().Id] <= 1));
        }

        private void SendGroupList(int header)
        {
            List<Group> groups = FirewindEnvironment.GetGame().GetGroupManager().GetGroups(Session.GetHabbo().Groups);
            Response.Init(header);
            Response.AppendInt32(groups.Count);

            foreach (Group group in groups)
            {
                Response.AppendInt32(group.ID);
                Response.AppendString(group.Name);
                Response.AppendString(group.BadgeCode);
                Response.AppendString(group.Color1);
                Response.AppendString(group.Color2);
                Response.AppendBoolean(group.ID == Session.GetHabbo().FavouriteGroup);
            }

            SendResponse();
        }

        private void SendFavouriteGroupUpdate(int groupID)
        {
            Group group = FirewindEnvironment.GetGame().GetGroupManager().GetGroup(groupID);
            SendGroupBadgeUpdate(group);

            int virtualID = 0;
            Room room = Session.GetHabbo().CurrentRoom;
            if (room != null)
            {
                RoomUser user = room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
                if (user != null)
                    virtualID = user.VirtualId;
            }

            Response.Init(Outgoing.FavoritemembershipUpdate);
            Response.AppendInt32(virtualID);
            Response.AppendInt32(group != null ? group.ID : 0);
            Response.AppendInt32(3);
            Response.AppendString(group != null ? group.Name : string.Empty);
            SendResponse();
        }

        private void SendGroupBadgeUpdate(Group group)
        {
            if (group == null)
                return;

            ServerMessage badgeUpdate = new ServerMessage(Outgoing.HabboGroupBadges);
            badgeUpdate.AppendInt32(1);
            badgeUpdate.AppendInt32(group.ID);
            badgeUpdate.AppendString(group.BadgeCode);

            Room room = Session.GetHabbo().CurrentRoom;
            if (room != null)
                room.SendMessage(badgeUpdate);
            else
                Session.SendMessage(badgeUpdate);
        }

        private void RefreshRoomUserGroup()
        {
            Room room = Session.GetHabbo().CurrentRoom;
            if (room == null)
                return;

            RoomUser user = room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (user == null)
                return;

            ServerMessage update = new ServerMessage(Outgoing.SetRoomUser);
            update.AppendInt32(1);
            user.Serialize(update);
            room.SendMessage(update);
        }

        private void AppendGuildStates(Group group)
        {
            for (int i = 0; i < 12; i++)
                Response.AppendInt32(i < group.GuildStates.Count ? group.GuildStates[i] : 0);
        }

        private GroupBadgeInfo ReadGroupBadgeInfo()
        {
            GroupBadgeInfo badge = new GroupBadgeInfo();
            if (Request.RemainingLength < 4)
                return badge;

            int marker = Request.ReadInt32();

            if (marker <= 5 && Request.RemainingLength >= 12)
            {
                badge.Base = Request.ReadInt32();
                badge.BaseColor = Request.ReadInt32();
                int symbolCount = Request.ReadInt32();
                badge.BadgeData.Add(new Tuple<int, int, int>(badge.Base, badge.BaseColor, 0));

                int symbolsToRead = Math.Min(symbolCount, Request.RemainingLength / 12);
                for (int i = 0; i < symbolsToRead; i++)
                {
                    int part = Request.ReadInt32();
                    int color = Request.ReadInt32();
                    int position = Request.ReadInt32();
                    badge.States.Add(part);
                    badge.States.Add(color);
                    badge.States.Add(position);
                    badge.BadgeData.Add(new Tuple<int, int, int>(part, color, position));
                }

                return badge;
            }

            int valuesToRead = Math.Min(marker, Request.RemainingLength / 4);
            List<int> values = new List<int>();
            for (int i = 0; i < valuesToRead; i++)
                values.Add(Request.ReadInt32());

            for (int i = 0; i + 2 < values.Count; i += 3)
            {
                if (i == 0)
                {
                    badge.Base = values[i];
                    badge.BaseColor = values[i + 1];
                }
                else
                {
                    badge.States.Add(values[i]);
                    badge.States.Add(values[i + 1]);
                    badge.States.Add(values[i + 2]);
                }

                badge.BadgeData.Add(new Tuple<int, int, int>(values[i], values[i + 1], values[i + 2]));
            }

            return badge;
        }

        private class GroupBadgeInfo
        {
            internal int Base;
            internal int BaseColor;
            internal List<int> States;
            internal List<Tuple<int, int, int>> BadgeData;

            internal GroupBadgeInfo()
            {
                States = new List<int>();
                BadgeData = new List<Tuple<int, int, int>>();
            }
        }

        private void SendGroupBadgeParts()
        {
            Response.Init(Outgoing.GuildEditorData);

            Response.AppendInt32(GuildsPartsData.BaseBadges.Count);
            foreach (GuildsPartsData data in GuildsPartsData.BaseBadges)
            {
                Response.AppendInt32(data.Id);
                Response.AppendString(data.ExtraData1);
                Response.AppendString(data.ExtraData2);
            }

            Response.AppendInt32(GuildsPartsData.SymbolBadges.Count);
            foreach (GuildsPartsData data in GuildsPartsData.SymbolBadges)
            {
                Response.AppendInt32(data.Id);
                Response.AppendString(data.ExtraData1);
                Response.AppendString(data.ExtraData2);
            }

            Response.AppendInt32(GuildsPartsData.ColorBadges1.Count);
            foreach (GuildsPartsData data in GuildsPartsData.ColorBadges1)
            {
                Response.AppendInt32(data.Id);
                Response.AppendString(data.ExtraData1);
            }

            Response.AppendInt32(GuildsPartsData.ColorBadges2.Count);
            foreach (GuildsPartsData data in GuildsPartsData.ColorBadges2)
            {
                Response.AppendInt32(data.Id);
                Response.AppendString(data.ExtraData1);
            }

            Response.AppendInt32(GuildsPartsData.ColorBadges3.Count);
            foreach (GuildsPartsData data in GuildsPartsData.ColorBadges3)
            {
                Response.AppendInt32(data.Id);
                Response.AppendString(data.ExtraData1);
            }

            SendResponse();
        }
    }
}
