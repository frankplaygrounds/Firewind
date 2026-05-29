using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Firewind.Core;
using Firewind.HabboHotel.Advertisements;
using Firewind.HabboHotel.Catalogs;
using Firewind.HabboHotel.GameClients;
using Firewind.HabboHotel.Items;
using Firewind.HabboHotel.Navigators;
using Firewind.HabboHotel.Pathfinding;
using Firewind.HabboHotel.Pets;
using Firewind.HabboHotel.RoomBots;
using Firewind.HabboHotel.Rooms;
using Firewind.HabboHotel.Users;
using Firewind.HabboHotel.Users.Badges;
using Firewind.Collections;
using Database_Manager.Database.Session_Details.Interfaces;
using Firewind.HabboHotel.Groups;
using Firewind.HabboHotel.Groups.Types;
using System.Collections;
using Firewind.HabboHotel.Rooms.Wired;
using Firewind.HabboHotel.Rooms.Wired.WiredHandlers.Interfaces;
using System.Drawing;
using HabboEvents;
using System.Reflection;
using Firewind.HabboHotel.SoundMachine;

namespace Firewind.Messages
{
    partial class GameClientMessageHandler
    {
        internal Room CurrentLoadingRoom;
        private int FloodCount;
        private DateTime FloodTime;

        // Room enter packet order:
        // -> OpenFlatConnection
        // <- RoomReady
        // -> GetHabboGroupBadges
        // -> GetFurnitureAliases
        // -> GetRoomEntryData
        // <- RoomEntryInfo
        // -> GetGuestRoom
        // <- GetGuestRoomResult
        // -> GetRoomAd
        internal void OpenFlatConnection()
        {
            if (FirewindEnvironment.ShutdownStarted)
            {
                Session.SendNotif(LanguageLocale.GetValue("shutdown.alert"));
                return;
            }

            int flatID = Request.ReadInt32();
            string password = Request.ReadString();
            int notUsed = Request.ReadInt32();

            if (Session.GetHabbo().IsTeleporting && Session.GetHabbo().TeleportingRoomID != flatID)
                return;

            Room room = FirewindEnvironment.GetGame().GetRoomManager().LoadRoom((uint)flatID);
            if (room == null)
            {
                // Don't actually think this message is used anymore
                Response.Init(Outgoing.NoSuchFlat);
                Response.AppendInt32(flatID); // flatId
                SendResponse();
                return;
            }

            ClearRoomLoading();
            if (Session.GetHabbo().InRoom)
            {
                Room oldRoom = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

                if (oldRoom != null)
                {
                    oldRoom.GetRoomUserManager().RemoveUserFromRoom(Session, false, false);
                    Session.CurrentRoomUserID = -1;
                }
            }
            //QueuedServerMessage response = new QueuedServerMessage(Session.GetConnection());

            if (room.UsersNow >= room.UsersMax && !Session.GetHabbo().HasFuse("fuse_enter_full_rooms"))
            {
                if (!FirewindEnvironment.GetGame().GetRoleManager().RankHasRight(Session.GetHabbo().Rank, "fuse_enter_full_rooms"))
                {
                    // This room is full!!!!
                    Response.Init(Outgoing.RoomErrorToEnter);
                    Response.AppendInt32(1);
                    SendResponse();
                    //response.appendResponse(GetResponse());

                    Response.Init(Outgoing.OutOfRoom);
                    SendResponse();
                    //response.appendResponse(GetResponse());

                    //response.sendResponse();
                    return;
                }
            }

            if (!Session.GetHabbo().HasFuse("fuse_enter_any_room") && room.UserIsBanned(Session.GetHabbo().Id))
            {
                if (room.HasBanExpired(Session.GetHabbo().Id))
                {
                    room.RemoveBan(Session.GetHabbo().Id);
                }
                else
                {
                    // You are banned of this room!
                    Response.Init(Outgoing.RoomErrorToEnter);
                    Response.AppendInt32(4);
                    SendResponse();
                    //response.appendResponse(GetResponse());

                    Response.Init(Outgoing.OutOfRoom);
                    SendResponse();
                    //response.appendResponse(GetResponse());

                    //response.sendResponse();
                    return;
                }
            }

            Session.GetHabbo().LoadingRoom = (uint)flatID;
            CurrentLoadingRoom = room;

            Response.Init(Outgoing.PrepareRoomForUsers);
            SendResponse();
            //response.appendResponse(GetResponse());

            if (!Session.GetHabbo().HasFuse("fuse_enter_any_room") && !room.CheckRights(Session, true) && !Session.GetHabbo().IsTeleporting)
            {
                switch (room.State)
                {
                    case 1:
                        if (room.UserCount == 0)
                        {
                            // Aww nobody in da room!
                            Response.Init(Outgoing.DoorBellNoPerson);
                            SendResponse();
                            //response.appendResponse(GetResponse());
                        }
                        else
                        {
                            // Waiting for answer!
                            Response.Init(Outgoing.Doorbell);
                            Response.AppendStringWithBreak("");
                            SendResponse();
                            //response.appendResponse(GetResponse());

                            ServerMessage RingMessage = new ServerMessage(Outgoing.Doorbell);
                            RingMessage.AppendStringWithBreak(Session.GetHabbo().Username);
                            room.SendMessageToUsersWithRights(RingMessage);
                        }
                        //response.sendResponse();
                        return;

                    case 2:
                        if (password.ToLower() != room.Password.ToLower())
                        {
                            // your password fail :( !

                            Response.Init(Outgoing.GenericError);
                            Response.AppendInt32(-100002);
                            SendResponse();
                            //response.appendResponse(GetResponse());

                            Response.Init(Outgoing.OutOfRoom);
                            SendResponse();
                            //response.appendResponse(GetResponse());

                            //response.sendResponse();
                            return;
                        }
                        break;
                }
            }
            Session.GetHabbo().LoadingChecksPassed = true;

            if (room.RoomData.Badge != null && room.RoomData.Badge != "")
            {
                Session.GetHabbo().GetBadgeComponent().GiveBadge(room.RoomData.Badge, true);
                Session.SendNotif(LanguageLocale.GetValue("user.badgereceived"));
            }

            //response.addBytes(LoadRoomForUser().getPacket);
            //LoadRoomForUser();
            //response.sendResponse();

            // Every check done, tell client to procceed
            Response.Init(Outgoing.RoomReady);
            Response.AppendStringWithBreak(room.ModelName); // if starts with "model_", roomCategory = 1
            Response.AppendUInt(room.RoomId); // flatId
            SendResponse();
        }

        internal void GetHabboGroupBadges()
        {
            Dictionary<int, string> groupBadges = new Dictionary<int, string>();

            if (CurrentLoadingRoom != null && CurrentLoadingRoom.RoomData.Group != null)
                groupBadges[CurrentLoadingRoom.RoomData.Group.ID] = CurrentLoadingRoom.RoomData.Group.BadgeCode;

            if (CurrentLoadingRoom != null)
            {
                foreach (RoomUser user in CurrentLoadingRoom.GetRoomUserManager().UserList.Values)
                {
                    if (user == null || user.IsBot || user.IsPet || user.GetClient() == null || user.GetClient().GetHabbo() == null)
                        continue;

                    int favouriteGroup = user.GetClient().GetHabbo().FavouriteGroup;
                    if (favouriteGroup <= 0 || FirewindEnvironment.GetGame().GetGroupManager() == null)
                        continue;

                    Group group = FirewindEnvironment.GetGame().GetGroupManager().GetGroup(favouriteGroup);
                    if (group != null)
                        groupBadges[group.ID] = group.BadgeCode;
                }
            }

            if (Session.GetHabbo().FavouriteGroup > 0 && FirewindEnvironment.GetGame().GetGroupManager() != null)
            {
                Group group = FirewindEnvironment.GetGame().GetGroupManager().GetGroup(Session.GetHabbo().FavouriteGroup);
                if (group != null)
                    groupBadges[group.ID] = group.BadgeCode;
            }

            Response.Init(Outgoing.HabboGroupBadges);
            Response.AppendInt32(groupBadges.Count);
            foreach (KeyValuePair<int, string> groupBadge in groupBadges)
            {
                Response.AppendInt32(groupBadge.Key);
                Response.AppendString(groupBadge.Value);
            }
            SendResponse();
        }

        // GetRoomData1
        internal void GetFurnitureAliases()
        {
            if (Session.GetHabbo().LoadingRoom <= 0 || CurrentLoadingRoom == null || !Session.GetHabbo().LoadingChecksPassed)
                return;

            Response.Init(Outgoing.FurnitureAliases);
            Response.AppendInt32(0); // count, idk what furnialiases is
            SendResponse();
        }

        // GetRoomData2
        internal void GetRoomEntryData()
        {
            if ((Session.GetHabbo().LoadingRoom <= 0 || CurrentLoadingRoom == null || !Session.GetHabbo().LoadingChecksPassed))
                return;

            Response.Init(Outgoing.RoomEntryInfo);
            Response.AppendBoolean(true); // privateRoom
            Response.AppendUInt(CurrentLoadingRoom.RoomId); // guestRoomId
            Response.AppendBoolean(CurrentLoadingRoom.CheckRights(Session, true)); // owner
            SendResponse();
        }

        internal void GetGuestRoom()
        {
            int roomID = Request.ReadInt32();
            bool unk1 = Request.ReadBoolean(); // these 2 bools could have with forwarding to do
            bool unk2 = Request.ReadBoolean();

            //Logging.LogDebug(String.Format("p1: {0}, p2: {1}", unk1, unk2));
            if (!Session.GetHabbo().HasFuse("fuse_enter_any_room") && (Session.GetHabbo().LoadingRoom != roomID || CurrentLoadingRoom == null || !Session.GetHabbo().LoadingChecksPassed))
                return;

            Habbo targetHabbo = Session.GetHabbo();

            RoomData room = FirewindEnvironment.GetGame().GetRoomManager().GenerateRoomData((uint)roomID);

            if (CurrentLoadingRoom == null) // Happens with MOD-tool
            {
                CurrentLoadingRoom = FirewindEnvironment.GetGame().GetRoomManager().LoadRoom((uint)roomID);

                Response.Init(Outgoing.PrepareRoomForUsers);
                SendResponse();

                Response.Init(Outgoing.RoomReady);
                Response.AppendStringWithBreak(room.ModelName); // if starts with "model_", roomCategory = 1
                Response.AppendInt32(roomID); // flatId
                SendResponse();
                //return;
            }

            if (Session.GetHabbo().InRoom)
            {
                Room oldRoom = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

                if (oldRoom != null)
                {
                    oldRoom.GetRoomUserManager().RemoveUserFromRoom(Session, false, false);
                    Session.CurrentRoomUserID = -1;
                }
            }

            if (room == null || CurrentLoadingRoom == null)
                return;

            Response.Init(Outgoing.GetGuestRoomResult);
            Response.AppendBoolean(true); // enterRoom
            room.Serialize(Response, false);
            Response.AppendBoolean(false); // roomForward
            Response.AppendString("");

            SendResponse();

            if (room.Wallpaper != "0.0")
            {
                Response.Init(Outgoing.RoomDecoration);
                Response.AppendStringWithBreak("wallpaper");
                Response.AppendStringWithBreak(room.Wallpaper);
                SendResponse();
            }

            if (room.Floor != "0.0")
            {
                Response.Init(Outgoing.RoomDecoration);
                Response.AppendStringWithBreak("floor");
                Response.AppendStringWithBreak(room.Floor);
                SendResponse();
            }

            Response.Init(Outgoing.RoomDecoration);
            Response.AppendStringWithBreak("landscape");
            Response.AppendStringWithBreak(room.Landscape);
            SendResponse();

            if (CurrentLoadingRoom.CheckRights(Session, true))
            {
                Response.Init(Outgoing.RoomRightsLevel);
                Response.AppendInt32(4);
                SendResponse();

                Response.Init(Outgoing.HasOwnerRights);
                SendResponse();
            }
            else if (CurrentLoadingRoom.CheckRights(Session))
            {
                Response.Init(Outgoing.RoomRightsLevel);
                Response.AppendInt32(1);
                SendResponse();
            }
            else
            {
                Response.Init(Outgoing.RoomRightsLevel);
                Response.AppendInt32(0);
                SendResponse();
            }

            Response.Init(Outgoing.ScoreMeter);
            Response.AppendInt32(room.Score);
            Response.AppendBoolean(!(Session.GetHabbo().RatedRooms.Contains(room.Id) || CurrentLoadingRoom.CheckRights(Session, true)));
            SendResponse();

            if (CurrentLoadingRoom.HasOngoingEvent)
            {
                //Session.SendMessage(Room.Event.Serialize(Session));
            }
            else
            {
                Response.Init(Outgoing.RoomEvent);
                Response.AppendStringWithBreak("-1");
                SendResponse();
            }

            Session.SendMessage(CurrentLoadingRoom.GetGameMap().Model.GetHeightmap());
            Session.SendMessage(CurrentLoadingRoom.GetGameMap().Model.SerializeRelativeHeightmap());

            QueuedServerMessage response = new QueuedServerMessage(Session.GetConnection());
            RoomItem[] floorItems = CurrentLoadingRoom.GetRoomItemHandler().mFloorItems.Values.ToArray();
            RoomItem[] wallItems = CurrentLoadingRoom.GetRoomItemHandler().mWallItems.Values.ToArray();

            Response.Init(Outgoing.Objects);
            Response.AppendInt32(1); // count of owners

            // serialize all owners
            Response.AppendInt32(CurrentLoadingRoom.OwnerId);
            Response.AppendString(CurrentLoadingRoom.Owner);

            // serialize items
            Response.AppendInt32(floorItems.Length);

            foreach (RoomItem Item in floorItems)
                Item.Serialize(Response, CurrentLoadingRoom.OwnerId);

            response.appendResponse(GetResponse());

            Response.Init(Outgoing.SerializeWallItems);
            Response.AppendInt32(1); // count of owners

            // serialize all owners
            Response.AppendInt32(CurrentLoadingRoom.OwnerId);
            Response.AppendString(CurrentLoadingRoom.Owner);

            // serialize items
            Response.AppendInt32(wallItems.Length);

            foreach (RoomItem Item in wallItems)
                Item.Serialize(Response, CurrentLoadingRoom.OwnerId);

            response.appendResponse(GetResponse());

            Array.Clear(floorItems, 0, floorItems.Length);
            Array.Clear(wallItems, 0, wallItems.Length);
            floorItems = null;
            wallItems = null;

            CurrentLoadingRoom.GetRoomUserManager().AddUserToRoom(Session, false);

            response.sendResponse();
            ClearRoomLoading();
        }

        internal void GetRoomAd()
        {
            if (Session.GetHabbo().LoadingRoom <= 0 || !Session.GetHabbo().LoadingChecksPassed || CurrentLoadingRoom == null)
            {
                return;
            }

            if (CurrentLoadingRoom.UsersNow + 1 > CurrentLoadingRoom.UsersMax && !Session.GetHabbo().HasFuse("fuse_enter_full_rooms"))
            {
                Session.SendNotif(LanguageLocale.GetValue("room.fullerror"));
                return;
            }
            
            ClearRoomLoading();

            QueuedServerMessage response = new QueuedServerMessage(Session.GetConnection());
            /*Response.Init(30);

            if (!string.IsNullOrEmpty(CurrentLoadingRoom.GetGameMap().StaticModel.StaticFurniMap))
            {
                Response.AppendStringWithBreak(CurrentLoadingRoom.GetGameMap().StaticModel.StaticFurniMap);
            }
            else
            {
                Response.AppendInt32(0);
            }*/

            //response.appendResponse(GetResponse());
            //SendResponse();

            //if (CurrentLoadingRoom.Type == "private" || CurrentLoadingRoom.Type == "public")
            //{
            //    RoomItem[] floorItems = CurrentLoadingRoom.GetRoomItemHandler().mFloorItems.Values.ToArray();
            //    RoomItem[] wallItems = CurrentLoadingRoom.GetRoomItemHandler().mWallItems.Values.ToArray();

            //    Response.Init(Outgoing.Objects);
            //    Response.AppendInt32(1); // count of owners

            //    // serialize all owners
            //    Response.AppendInt32(CurrentLoadingRoom.OwnerId);
            //    Response.AppendString(CurrentLoadingRoom.Owner);

            //    // serialize items
            //    /*if (CurrentLoadingRoom.Type == "public")
            //        Response.AppendInt32(floorItems.Length + 1);
            //    else*/
            //        Response.AppendInt32(floorItems.Length);

            //    foreach (RoomItem Item in floorItems)
            //        Item.Serialize(Response, CurrentLoadingRoom.OwnerId);
            //    /*if (CurrentLoadingRoom.Type == "public")
            //    {
            //        Response.AppendInt32(floorItems.Length+5);
            //        Response.AppendInt32(4276); // room_ads
            //        Response.AppendInt32(11);
            //        Response.AppendInt32(25);
            //        Response.AppendInt32(4);
            //        Response.AppendStringWithBreak("0.0");
            //        Response.AppendInt32(0);
            //        Response.AppendInt32(1);
            //        Response.AppendInt32(6);
            //        Response.AppendString("offsetZ");
            //        Response.AppendString("0");
            //        Response.AppendString("imageUrl");
            //        Response.AppendString("http://91.121.241.155/oldswfs/c_images/album1134/torchmpu.gif");
            //        Response.AppendString("offsetY");
            //        Response.AppendString("0");
            //        Response.AppendString("offsetX");
            //        Response.AppendString("0");
            //        Response.AppendString("state");
            //        Response.AppendString("0");
            //        Response.AppendString("clickurl");
            //        Response.AppendString("");
            //        Response.AppendInt32(-1);
            //        Response.AppendInt32(1); // Type New R63 ('use bottom')
            //        Response.AppendInt32(CurrentLoadingRoom.OwnerId);
            //    }*/
            //    response.appendResponse(GetResponse());

            //    Response.Init(Outgoing.SerializeWallItems);
            //    Response.AppendInt32(1); // count of owners

            //    // serialize all owners
            //    Response.AppendInt32(CurrentLoadingRoom.OwnerId);
            //    Response.AppendString(CurrentLoadingRoom.Owner);

            //    // serialize items
            //    Response.AppendInt32(wallItems.Length);

            //    foreach (RoomItem Item in wallItems)
            //        Item.Serialize(Response, CurrentLoadingRoom.OwnerId);

            //    response.appendResponse(GetResponse());

            //    Array.Clear(floorItems, 0, floorItems.Length);
            //    Array.Clear(wallItems, 0, wallItems.Length);
            //    floorItems = null;
            //    wallItems = null;
                 
            //}
            //CurrentLoadingRoom.GetRoomUserManager().AddUserToRoom(Session, false);


            //response.sendResponse();
        }

        internal void GoToFlat()
        {
            if (Session.GetHabbo().LoadingRoom <= 0 || CurrentLoadingRoom == null || !Session.GetHabbo().LoadingChecksPassed)
                return;

            Room room = CurrentLoadingRoom;
            if (room.RoomData.Badge != null && room.RoomData.Badge != "")
            {
                Session.GetHabbo().GetBadgeComponent().GiveBadge(room.RoomData.Badge, true);
                Session.SendNotif(LanguageLocale.GetValue("user.badgereceived"));
            }

            // Every check done, tell client to procceed
            Response.Init(Outgoing.RoomReady);
            Response.AppendStringWithBreak(room.ModelName); // if starts with "model_", roomCategory = 1
            Response.AppendUInt(room.RoomId); // flatId
            SendResponse();
        }


        internal void OnRoomUserAdd()
        {
            QueuedServerMessage response = new QueuedServerMessage(Session.GetConnection());

            List<RoomUser> UsersToDisplay = new List<RoomUser>();

            if (CurrentLoadingRoom == null)
                return;

            foreach (RoomUser User in CurrentLoadingRoom.GetRoomUserManager().UserList.Values)
            {
                if (User.IsSpectator)
                    continue;

                UsersToDisplay.Add(User);
            }

            Response.Init(Outgoing.SetRoomUser);
            Response.AppendInt32(UsersToDisplay.Count);

            foreach (RoomUser User in UsersToDisplay)
            {
                User.Serialize(Response);
            }
            response.appendResponse(GetResponse());

            Response.Init(Outgoing.ConfigureWallandFloor);
            GetResponse().AppendBoolean(CurrentLoadingRoom.Hidewall);
            GetResponse().AppendInt32(CurrentLoadingRoom.WallThickness);
            GetResponse().AppendInt32(CurrentLoadingRoom.FloorThickness);
            response.appendResponse(GetResponse());

            if (CurrentLoadingRoom.UsersWithRights.Count > 0/* && CurrentLoadingRoom.CheckRights(Session, true)*/)
            {
                GetResponse().Init(Outgoing.FlatControllerAdded);
                GetResponse().AppendUInt(CurrentLoadingRoom.RoomData.Id);
                GetResponse().AppendInt32(CurrentLoadingRoom.UsersWithRights.Count);
                foreach (uint i in CurrentLoadingRoom.UsersWithRights)
                {
                    Habbo xUser = FirewindEnvironment.getHabboForId(i);
                    GetResponse().AppendUInt(xUser.Id);
                    GetResponse().AppendString(xUser.Username);
                }
                response.appendResponse(GetResponse());

                foreach (uint i in CurrentLoadingRoom.UsersWithRights)
                {
                    Habbo xUser = FirewindEnvironment.getHabboForId(i);
                    GetResponse().Init(Outgoing.GivePowers);
                    GetResponse().AppendUInt(CurrentLoadingRoom.RoomId);
                    GetResponse().AppendUInt(xUser.Id);
                    GetResponse().AppendString(xUser.Username);
                    response.appendResponse(GetResponse());
                }
            }
            ServerMessage Updates = CurrentLoadingRoom.GetRoomUserManager().SerializeStatusUpdates(true);

            if (Updates != null)
            {
                //Session.SendMessage(Updates);
                response.appendResponse(Updates);
            }
            //return;
            foreach (RoomUser User in CurrentLoadingRoom.GetRoomUserManager().UserList.Values)
            {
                if (User.IsSpectator)
                    continue;

                if (User.IsDancing)
                {
                    Response.Init(Outgoing.Dance);
                    Response.AppendInt32(User.VirtualId);
                    Response.AppendInt32(User.DanceId);
                    response.appendResponse(GetResponse());
                }

                if (User.IsAsleep)
                {
                    Response.Init(Outgoing.IdleStatus);
                    Response.AppendInt32(User.VirtualId);
                    Response.AppendBoolean(true);
                    response.appendResponse(GetResponse());
                }

                if (User.CarryItemID > 0 && User.CarryTimer > 0)
                {
                    Response.Init(Outgoing.ApplyCarryItem);
                    Response.AppendInt32(User.VirtualId);
                    Response.AppendInt32(User.CarryTimer);
                    response.appendResponse(GetResponse());
                }

                if (!User.IsBot)
                {
                    try
                    {
                        if (User.GetClient() != null && User.GetClient().GetHabbo() != null && User.GetClient().GetHabbo().GetAvatarEffectsInventoryComponent() != null && User.CurrentEffect >= 1)
                        {
                            Response.Init(Outgoing.ApplyEffects);
                            Response.AppendInt32(User.VirtualId);
                            Response.AppendInt32(User.CurrentEffect);
                            Response.AppendInt32(0);
                            response.appendResponse(GetResponse());
                        }
                    }
                    catch (Exception e) { Logging.HandleException(e, "Rooms.SendRoomData3"); }
                }
            }

            response.sendResponse();
            CurrentLoadingRoom = null;
        }

        internal void ForwardToRoom(int flatID)
        {
            if (FirewindEnvironment.ShutdownStarted)
            {
                Session.SendNotif(LanguageLocale.GetValue("shutdown.alert"));
                return;
            }

            if (Session.GetHabbo().IsTeleporting && Session.GetHabbo().TeleportingRoomID != flatID)
                return;

            Room room = FirewindEnvironment.GetGame().GetRoomManager().LoadRoom((uint)flatID);
            if (room == null)
            {
                // Don't actually think this message is used anymore
                Response.Init(Outgoing.NoSuchFlat);
                Response.AppendInt32(flatID); // flatId
                SendResponse();
                return;
            }

            ClearRoomLoading();
            if (Session.GetHabbo().InRoom)
            {
                Room oldRoom = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

                if (oldRoom != null)
                {
                    oldRoom.GetRoomUserManager().RemoveUserFromRoom(Session, false, false);
                    Session.CurrentRoomUserID = -1;
                }
            }
            //QueuedServerMessage response = new QueuedServerMessage(Session.GetConnection());

            if (room.UsersNow >= room.UsersMax && !Session.GetHabbo().HasFuse("fuse_enter_full_rooms"))
            {
                if (!FirewindEnvironment.GetGame().GetRoleManager().RankHasRight(Session.GetHabbo().Rank, "fuse_enter_full_rooms"))
                {
                    // This room is full!!!!
                    Response.Init(Outgoing.RoomErrorToEnter);
                    Response.AppendInt32(1);
                    SendResponse();
                    //response.appendResponse(GetResponse());

                    Response.Init(Outgoing.OutOfRoom);
                    SendResponse();
                    //response.appendResponse(GetResponse());

                    //response.sendResponse();
                    return;
                }
            }

            if (!Session.GetHabbo().HasFuse("fuse_enter_any_room") && room.UserIsBanned(Session.GetHabbo().Id))
            {
                if (room.HasBanExpired(Session.GetHabbo().Id))
                {
                    room.RemoveBan(Session.GetHabbo().Id);
                }
                else
                {
                    // You are banned of this room!
                    Response.Init(Outgoing.RoomErrorToEnter);
                    Response.AppendInt32(4);
                    SendResponse();
                    //response.appendResponse(GetResponse());

                    Response.Init(Outgoing.OutOfRoom);
                    SendResponse();
                    //response.appendResponse(GetResponse());

                    //response.sendResponse();
                    return;
                }
            }

            Session.GetHabbo().LoadingRoom = (uint)flatID;
            CurrentLoadingRoom = room;

            Response.Init(Outgoing.PrepareRoomForUsers);
            SendResponse();
            //response.appendResponse(GetResponse());

            if (!Session.GetHabbo().HasFuse("fuse_enter_any_room") && !room.CheckRights(Session, true) && !Session.GetHabbo().IsTeleporting)
            {
                switch (room.State)
                {
                    case 1:
                        if (room.UserCount == 0)
                        {
                            // Aww nobody in da room!
                            Response.Init(Outgoing.DoorBellNoPerson);
                            SendResponse();
                            //response.appendResponse(GetResponse());
                        }
                        else
                        {
                            // Waiting for answer!
                            Response.Init(Outgoing.Doorbell);
                            Response.AppendStringWithBreak("");
                            SendResponse();
                            //response.appendResponse(GetResponse());

                            ServerMessage RingMessage = new ServerMessage(Outgoing.Doorbell);
                            RingMessage.AppendStringWithBreak(Session.GetHabbo().Username);
                            room.SendMessageToUsersWithRights(RingMessage);
                        }
                        //response.sendResponse();
                        return;

                    case 2:
                        //if (password.ToLower() != room.Password.ToLower())
                        {
                            // your password fail :( !

                            Response.Init(Outgoing.GenericError);
                            Response.AppendInt32(-100002); // can be 4009 if you want something like 'need.to.be.vip'
                            SendResponse();
                            //response.appendResponse(GetResponse());

                            Response.Init(Outgoing.OutOfRoom);
                            SendResponse();
                            //response.appendResponse(GetResponse());

                            //response.sendResponse();
                            return;
                        }
                        //break;
                }
            }
            Session.GetHabbo().LoadingChecksPassed = true;

            if (room.RoomData.Badge != null && room.RoomData.Badge != "")
            {
                Session.GetHabbo().GetBadgeComponent().GiveBadge(room.RoomData.Badge, true);
                Session.SendNotif(LanguageLocale.GetValue("user.badgereceived"));
            }

            //response.sendResponse();

            // Every check done, tell client to procceed
            Response.Init(Outgoing.RoomReady);
            Response.AppendStringWithBreak(room.ModelName); // if starts with "model_", roomCategory = 1
            Response.AppendUInt(room.RoomId); // flatId
            SendResponse();
        }

        internal QueuedServerMessage LoadRoomForUser()
        {
            //Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().LoadingRoom);
            
            Room Room = CurrentLoadingRoom;

            QueuedServerMessage response = new QueuedServerMessage(Session.GetConnection());

            if (Room == null || !Session.GetHabbo().LoadingChecksPassed)
                return response;


            // todo: Room.SerializeGroupBadges()
            /*Response.Init(309);
            Response.AppendInt32(1); //yes or no i guess
            Response.AppendInt32(122768); // group iD
            Response.AppendStringWithBreak("b1201Xs03097s55091s17094a7a396e8d44670744d87bf913858b1fc");*/

            Response.Init(Outgoing.RoomReady);
            Response.AppendStringWithBreak(Room.ModelName); // if starts with "model_", roomCategory = 1
            Response.AppendUInt(Room.RoomId); // flatId
            response.appendResponse(GetResponse());

            if (Session.GetHabbo().SpectatorMode)
            {
                //Response.Init(254);
                //response.appendResponse(GetResponse());
            }

                if (Room.Wallpaper != "0.0")
                {
                    Response.Init(Outgoing.RoomDecoration);
                    Response.AppendStringWithBreak("wallpaper");
                    Response.AppendStringWithBreak(Room.Wallpaper);
                    response.appendResponse(GetResponse());
                }

                if (Room.Floor != "0.0")
                {
                    Response.Init(Outgoing.RoomDecoration);
                    Response.AppendStringWithBreak("floor");
                    Response.AppendStringWithBreak(Room.Floor);
                    response.appendResponse(GetResponse());
                }

                Response.Init(Outgoing.RoomDecoration);
                Response.AppendStringWithBreak("landscape");
                Response.AppendStringWithBreak(Room.Landscape);
                response.appendResponse(GetResponse());

                if (Room.CheckRights(Session, true))
                {
                    Response.Init(Outgoing.RoomRightsLevel);
                    Response.AppendInt32(4);
                    response.appendResponse(GetResponse());

                    Response.Init(Outgoing.HasOwnerRights);
                    response.appendResponse(GetResponse());
                }
                else if (Room.CheckRights(Session))
                {
                    Response.Init(Outgoing.RoomRightsLevel);
                    Response.AppendInt32(1);
                    response.appendResponse(GetResponse());
                }
                else
                {
                    Response.Init(Outgoing.RoomRightsLevel);
                    Response.AppendInt32(0);
                    response.appendResponse(GetResponse());
                }

                Response.Init(Outgoing.ScoreMeter);
                Response.AppendInt32(Room.Score);
                Response.AppendBoolean(!(Session.GetHabbo().RatedRooms.Contains(Room.RoomId) || Room.CheckRights(Session, true)));
                response.appendResponse(GetResponse());

                if (Room.HasOngoingEvent)
                {
                    //Session.SendMessage(Room.Event.Serialize(Session));
                }
                else
                {
                    Response.Init(Outgoing.RoomEvent);
                    Response.AppendStringWithBreak("-1");
                    response.appendResponse(GetResponse());
                }

            //response.sendResponse();
            return response;
        }

        internal void ClearRoomLoading()
        {
            Session.GetHabbo().LoadingRoom = 0;
            Session.GetHabbo().LoadingChecksPassed = false;
        }

        internal void GetInventory()
        {
            QueuedServerMessage response = new QueuedServerMessage(Session.GetConnection());
            response.appendResponse(Session.GetHabbo().GetInventoryComponent().SerializeFloorItemInventory());
            response.appendResponse(Session.GetHabbo().GetInventoryComponent().SerializeWallItemInventory());
            response.sendResponse();
        }

        internal void Talk()
        {
            if (FirewindEnvironment.SystemMute)
                return;
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null)
            {
                return;
            }

            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (User == null)
            {
                return;
            }

            User.Chat(Session, FirewindEnvironment.FilterInjectionChars(Request.ReadString()), false);
        }

        internal void Shout()
        {
            if (FirewindEnvironment.SystemMute)
                return;
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null)
            {
                return;
            }

            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (User == null)
            {
                return;
            }

            User.Chat(Session, FirewindEnvironment.FilterInjectionChars(Request.ReadString()), true);
        }

        internal void Whisper()
        {
            if (FirewindEnvironment.SystemMute)
                return;

            if (Session == null || Session.GetHabbo() == null)
                return;

            Room Room = Session.GetHabbo().CurrentRoom;

            if (Room == null)
            {
                return;
            }

            if (Session.GetHabbo().Muted)
            {
                Session.SendNotif(LanguageLocale.GetValue("user.ismuted"));
                return;
            }

            if (Room.RoomMuted)
                return;

            TimeSpan SinceLastMessage = DateTime.Now - FloodTime;
            if (SinceLastMessage.Seconds > 4)
                FloodCount = 0;

            if (SinceLastMessage.Seconds < 4 && FloodCount > 5 && Session.GetHabbo().Rank < 5)
            {
                ServerMessage Packet = new ServerMessage(Outgoing.FloodFilter);
                Packet.AppendInt32(30); //Blocked for 30sec
                Session.SendMessage(Packet);
                return;
            }
            FloodTime = DateTime.Now;
            FloodCount++;

            string Params = Request.ReadString();
            string ToUser = Params.Split(' ')[0];
            string Message = Params.Substring(ToUser.Length + 1);

            Message = LanguageLocale.FilterSwearwords(Message);
            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            ServerMessage TellMsg = new ServerMessage();
            TellMsg.Init(Outgoing.Whisp);
            TellMsg.AppendInt32(User.VirtualId);
            TellMsg.AppendStringWithBreak(Message);
            TellMsg.AppendInt32(0);
            TellMsg.AppendInt32(0);
            TellMsg.AppendInt32(-1);

            Session.SendMessage(TellMsg);
            User.Unidle();

            RoomUser User2 = Room.GetRoomUserManager().GetRoomUserByHabbo(ToUser);
            if (ToUser == User.GetUsername() || User2 == null)
                return;

            if (!User2.GetClient().GetHabbo().MutedUsers.Contains(Session.GetHabbo().Id))
                User2.GetClient().SendMessage(TellMsg);

            List<RoomUser> ToNotify = Room.GetRoomUserManager().GetRoomUserByRank(6);
            if (ToNotify.Count > 0)
            {
                TellMsg = new ServerMessage();
                TellMsg.Init(Outgoing.Whisp);
                TellMsg.AppendInt32(User.VirtualId);
                TellMsg.AppendStringWithBreak(LanguageLocale.GetValue("moderation.whisper") + ToUser + ": " + Message);
                TellMsg.AppendInt32(0);
                TellMsg.AppendInt32(0);
                TellMsg.AppendInt32(-1);


                foreach (RoomUser user in ToNotify)
                    if (user.HabboId != User2.HabboId && user.HabboId != User.HabboId)
                        user.GetClient().SendMessage(TellMsg);

            }
        }

        internal void Move()
        {
            Room Room = Session.GetHabbo().CurrentRoom;

            if (Room == null)
            {
                return;
            }

            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (User == null || !User.CanWalk)
            {
                return;
            }

            int MoveX = Request.ReadInt32();
            int MoveY = Request.ReadInt32();

            if (MoveX == User.X && MoveY == User.Y)
            {
                return;
            }

            User.MoveTo(MoveX, MoveY);
        }

        internal void CanCreateRoom()
        {
            Response.Init(Outgoing.CanCreateRoom);
            Response.AppendInt32(0); // true = show error with number below
            Response.AppendInt32(99999); // max rooms
            SendResponse();

            // todo: room limit
        }

        internal void CreateRoom()
        {
            string RoomName = FirewindEnvironment.FilterInjectionChars(Request.ReadString());
            string ModelName = Request.ReadString();
            //string RoomState = Request.PopFixedString(); // unused, room open by default on creation. may be added in later build of Habbo?

            RoomData NewRoom = FirewindEnvironment.GetGame().GetRoomManager().CreateRoom(Session, RoomName, ModelName);

            if (NewRoom != null)
            {
                Response.Init(Outgoing.OnCreateRoomInfo);
                Response.AppendUInt(NewRoom.Id);
                Response.AppendStringWithBreak(NewRoom.Name);
                SendResponse();
            }
        }

        internal void GetRoomSettings()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CheckRights(Session, true))
            {
                return;
            }

            Session.SendMessage(FirewindEnvironment.GetGame().GetNavigator().SerializeFlatCategories(Session));

            GetResponse().Init(Outgoing.RoomSettingsData);
            GetResponse().AppendUInt(Room.RoomId); // roomId
            GetResponse().AppendStringWithBreak(Room.Name); // name
            GetResponse().AppendStringWithBreak(Room.Description); // description
            GetResponse().AppendInt32(Room.State); // doorMode
            GetResponse().AppendInt32(Room.Category); // categoryId
            GetResponse().AppendInt32(Room.UsersMax); // maximumVisitors
            GetResponse().AppendInt32(((Room.RoomData.Model.MapSizeX * Room.RoomData.Model.MapSizeY) > 100) ? 50 : 25); // maximumVisitorsLimit

            GetResponse().AppendInt32(Room.TagCount); // tags count
            foreach (string Tag in Room.Tags.ToArray())
            {
                GetResponse().AppendStringWithBreak(Tag);
            }

            //GetResponse().AppendInt32(Room.UsersWithRights.Count); // controllers count

            //foreach (uint userID in Room.UsersWithRights) // FlatControllerData
            //{
            //    Habbo xUser = FirewindEnvironment.getHabboForId(userID);
            //    GetResponse().AppendUInt(xUser.Id); // userId
            //    GetResponse().AppendStringWithBreak(xUser.Username); // userName
            //}

            GetResponse().AppendInt32(Room.CanTradeInRoom ? 1 : 0); // tradingAllowed

            GetResponse().AppendInt32(Room.AllowPets ? 1 : 0); // allowPets
            GetResponse().AppendInt32(Room.AllowPetsEating ? 1 : 0); // allowFoodConsume
            GetResponse().AppendInt32(Room.AllowWalkthrough ? 1 : 0); // allowWalkThrough
            GetResponse().AppendInt32(Room.Hidewall ? 1 : 0); // hideWalls

            GetResponse().AppendInt32(Room.WallThickness); // wallThickness
            GetResponse().AppendInt32(Room.FloorThickness); // floorThickness

            SendResponse();

            if (Room.UsersWithRights.Count > 0)
            {
                GetResponse().Init(Outgoing.FlatControllerAdded);

                GetResponse().AppendUInt(Room.RoomData.Id); // flatId
                GetResponse().AppendInt32(Room.UsersWithRights.Count); // controllers count

                foreach (uint userID in Room.UsersWithRights) // FlatControllerData
                {
                    Habbo user = FirewindEnvironment.getHabboForId(userID);
                    GetResponse().AppendUInt(user.Id); // userId
                    GetResponse().AppendStringWithBreak(user.Username); // userName
                }
                SendResponse();
            }
        }

        internal void SaveRoomSettings()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CheckRights(Session, true))
            {
                return;
            }

            int Id = Request.ReadInt32(); // roomId
            string Name = FirewindEnvironment.FilterInjectionChars(Request.ReadString()); // name
            string Description = FirewindEnvironment.FilterInjectionChars(Request.ReadString()); // description
            int State = Request.ReadInt32(); // doorMode
            string Password = FirewindEnvironment.FilterInjectionChars(Request.ReadString()); // password
            int MaxUsers = Request.ReadInt32(); // maximumVisitors
            int CategoryId = Request.ReadInt32(); // categoryId

            int TagCount = Request.ReadInt32(); // tags count
            List<string> Tags = new List<string>();
            StringBuilder formattedTags = new StringBuilder();

            for (int i = 0; i < TagCount; i++)
            {
                if (i > 0)
                {
                    formattedTags.Append(",");
                }

                string tag = FirewindEnvironment.FilterInjectionChars(Request.ReadString().ToLower());

                Tags.Add(tag);
                formattedTags.Append(tag);
            }

            // not used
            bool AllowTrade = (Request.ReadInt32() == 1);

            bool AllowPets = Request.ReadBoolean(); // allowPets
            bool AllowPetsEat = Request.ReadBoolean(); // allowFoodConsume
            bool AllowWalkthrough = Request.ReadBoolean(); // allowWalkThrough
            bool Hidewall = Request.ReadBoolean(); // hideWalls
            int WallThickness = Request.ReadInt32(); // wallThickness
            int FloorThickness = Request.ReadInt32(); // floorThickness

         

            if (WallThickness < -2 || WallThickness > 1)
            {
                WallThickness = 0;
            }

            if (FloorThickness < -2 || FloorThickness > 1)
            {
                FloorThickness = 0;
            }

            if (Name.Length < 1)
            {
                return;
            }

            if (State < 0 || State > 2)
            {
                return;
            }

            FlatCat FlatCat = FirewindEnvironment.GetGame().GetNavigator().GetFlatCat(CategoryId);

            if (FlatCat == null)
            {
                return;
            }

            if (FlatCat.MinRank > Session.GetHabbo().Rank)
            {
                Session.SendNotif(LanguageLocale.GetValue("user.roomdata.rightserror"));
                CategoryId = 0;
            }

            if (TagCount > 2)
            {
                return;
            }

            Room.AllowPets = AllowPets;
            Room.AllowPetsEating = AllowPetsEat;
            Room.AllowWalkthrough = AllowWalkthrough;
            Room.Hidewall = Hidewall;

            Room.RoomData.AllowPets = AllowPets;
            Room.RoomData.AllowPetsEating = AllowPetsEat;
            Room.RoomData.AllowWalkthrough = AllowWalkthrough;
            Room.RoomData.Hidewall = Hidewall;

            Room.Name = Name;
            Room.State = State;
            Room.Description = Description;
            Room.Category = CategoryId;
            Room.Password = Password;

            Room.RoomData.Name = Name;
            Room.RoomData.State = State;
            Room.RoomData.Description = Description;
            Room.RoomData.Category = CategoryId;
            Room.RoomData.Password = Password;

            Room.ClearTags();
            Room.AddTagRange(Tags);
            Room.UsersMax = MaxUsers;

            Room.RoomData.Tags.Clear();
            Room.RoomData.Tags.AddRange(Tags);
            Room.RoomData.UsersMax = MaxUsers;

            Room.WallThickness = WallThickness;
            Room.FloorThickness = FloorThickness;
            Room.RoomData.WallThickness = WallThickness;
            Room.RoomData.FloorThickness = FloorThickness;

            string formattedState = "open";

            if (Room.State == 1)
                formattedState = "locked";
            else if (Room.State > 1)
                formattedState = "password";

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("UPDATE rooms SET caption = @caption, description = @description, password = @password, category = " + CategoryId + ", state = '" + formattedState + "', tags = @tags, users_max = " + MaxUsers + ", allow_pets = " + TextHandling.BooleanToInt(AllowPets) + ", allow_pets_eat = " + TextHandling.BooleanToInt(AllowPetsEat) + ", allow_walkthrough = " + TextHandling.BooleanToInt(AllowWalkthrough) + ", allow_hidewall = " + TextHandling.BooleanToInt(Room.Hidewall) + ", floorthickness = " + Room.FloorThickness + ", wallthickness = " + Room.WallThickness + " WHERE id = " + Room.RoomId);
                dbClient.addParameter("caption", Room.Name);
                dbClient.addParameter("description", Room.Description);
                dbClient.addParameter("password", Room.Password);
                dbClient.addParameter("tags", formattedTags.ToString());
                dbClient.runQuery();
            }

            //FirewindEnvironment.GetGame().GetRoomManager().QueueActiveRoomRemove(oldRoomData);
            //FirewindEnvironment.GetGame().GetRoomManager().QueueActiveRoomAdd(Room.RoomData);

            GetResponse().Init(Outgoing.UpdateRoomOne);
            GetResponse().AppendUInt(Room.RoomId);
            SendResponse();

            GetResponse().Init(Outgoing.ConfigureWallandFloor);
            GetResponse().AppendBoolean(Room.Hidewall);
            GetResponse().AppendInt32(Room.WallThickness);
            GetResponse().AppendInt32(Room.FloorThickness);
            Session.GetHabbo().CurrentRoom.SendMessage(GetResponse());

            RoomData Data = Room.RoomData;

            Response.Init(Outgoing.GetGuestRoomResult);
            Response.AppendBoolean(true); // enterRoom
            Data.Serialize(Response, false);
            Response.AppendBoolean(false); // roomForward
            Response.AppendString("");

            Session.GetHabbo().CurrentRoom.SendMessage(GetResponse());
        }

        internal void GiveRights()
        {
            uint UserId = Request.ReadUInt32();

            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (Room == null)
                return;

            RoomUser RoomUser = Room.GetRoomUserManager().GetRoomUserByHabbo(UserId);

            if (Room == null || !Room.CheckRights(Session, true) || RoomUser == null || RoomUser.IsBot)
            {
                return;
            }

            if (Room.UsersWithRights.Contains(UserId))
            {
                Session.SendNotif(LanguageLocale.GetValue("user.giverights.error"));
                return;
            }

            Room.UsersWithRights.Add(UserId);

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.runFastQuery("INSERT INTO room_rights (room_id,user_id) VALUES (" + Room.RoomId + "," + UserId + ")");
            }

            Response.Init(Outgoing.GivePowers);
            Response.AppendUInt(Room.RoomId);
            Response.AppendUInt(UserId);
            Response.AppendStringWithBreak(RoomUser.GetClient().GetHabbo().Username);
            SendResponse();

            //RoomUser.AddStatus("flatcrtl 1", "");
            //RoomUser.UpdateNeeded = true;

            if (RoomUser != null && !RoomUser.IsBot)
            {
                Response.Init(Outgoing.RoomRightsLevel);
                Response.AppendInt32(1);
                RoomUser.GetClient().SendMessage(GetResponse());
            }
        }

        internal void RemoveRights()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CheckRights(Session, true))
            {
                return;
            }

            StringBuilder DeleteParams = new StringBuilder();

            int Amount = Request.ReadInt32();

            for (int i = 0; i < Amount; i++)
            {
                if (i > 0)
                {
                    DeleteParams.Append(" OR ");
                }

                uint UserId = Request.ReadUInt32();
                Room.UsersWithRights.Remove(UserId);
                DeleteParams.Append("room_id = '" + Room.RoomId + "' AND user_id = '" + UserId + "'");

                RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(UserId);

                if (User != null && !User.IsBot)
                {
                    Response.Init(Outgoing.RoomRightsLevel);
                    Response.AppendInt32(0);
                    User.GetClient().SendMessage(GetResponse());
                }

                // GhntX]hqu@U
                Response.Init(Outgoing.RemovePowers);
                Response.AppendUInt(Room.RoomId);
                Response.AppendUInt(UserId);
                SendResponse();

                //User.AddStatus("flatcrtl", "");
               // User.UpdateNeeded = true;
            }

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.runFastQuery("DELETE FROM room_rights WHERE " + DeleteParams.ToString());
            }
        }

        internal void TakeAllRights()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CheckRights(Session, true))
            {
                return;
            }

            foreach (uint UserId in Room.UsersWithRights)
            {
                RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(UserId);

                if (User != null && !User.IsBot)
                {
                    Response.Init(Outgoing.RoomRightsLevel);
                    Response.AppendInt32(0);
                    User.GetClient().SendMessage(GetResponse());
                }

                // GhntX]hqu@U
                Response.Init(Outgoing.RemovePowers);
                Response.AppendUInt(Room.RoomId);
                Response.AppendUInt(UserId);
                SendResponse();

                //User.AddStatus("flatcrtl 0", "");
            }

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.runFastQuery("DELETE FROM room_rights WHERE room_id = " + Room.RoomId);
            }

            Room.UsersWithRights.Clear();
        }

        internal void KickUser()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null)
            {
                return;
            }

            if (!Room.CheckRights(Session))
            {
                return; // insufficient permissions
            }

            uint UserId = Request.ReadUInt32();
            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(UserId);

            if (User == null || User.IsBot)
            {
                return;
            }

            if (Room.CheckRights(User.GetClient(), true) || User.GetClient().GetHabbo().HasFuse("fuse_mod") || User.GetClient().GetHabbo().HasFuse("fuse_no_kick"))
            {
                return; // can't kick room owner or mods!
            }

            Room.GetRoomUserManager().RemoveUserFromRoom(User.GetClient(), true, true);
            User.GetClient().CurrentRoomUserID = -1;
        }

        internal void BanUser()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CheckRights(Session, true))
            {
                return; // insufficient permissions
            }

            uint UserId = Request.ReadUInt32();
            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(UserId);

            if (User == null || User.IsBot)
            {
                return;
            }

            if (User.GetClient().GetHabbo().HasFuse("fuse_mod") || User.GetClient().GetHabbo().HasFuse("fuse_no_kick"))
            {
                return;
            }

            Room.AddBan(UserId);
            Room.GetRoomUserManager().RemoveUserFromRoom(User.GetClient(), true, true);

            Session.CurrentRoomUserID = -1;
        }

        internal void SetHomeRoom()
        {
            uint RoomId = Request.ReadUInt32();
            RoomData Data = FirewindEnvironment.GetGame().GetRoomManager().GenerateRoomData(RoomId);

            if (RoomId != 0)
            {
                if (Data == null || Data.Owner.ToLower() != Session.GetHabbo().Username.ToLower())
                {
                    return;
                }
            }

            Session.GetHabbo().HomeRoom = RoomId;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.runFastQuery("UPDATE users SET home_room = " + RoomId + " WHERE id = " + Session.GetHabbo().Id);
            }

            Response.Init(Outgoing.HomeRoom);
            Response.AppendUInt(RoomId);
            Response.AppendUInt(RoomId);
            SendResponse();
        }

        internal void DeleteRoom()
        {
            uint RoomId = Request.ReadUInt32();
            if (Session == null || Session.GetHabbo() == null || Session.GetHabbo().UsersRooms == null)
                return;
            
            //TargetRoom = Session.GetHabbo().CurrentRoom; ;
            Room TargetRoom = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(RoomId);
            if (TargetRoom == null)
                return;
            if (TargetRoom.Owner == Session.GetHabbo().Username || Session.GetHabbo().Rank > 6)
            {

                if (this.Session.GetHabbo().GetInventoryComponent() != null)
                {
                    this.Session.GetHabbo().GetInventoryComponent().AddItemArray(TargetRoom.GetRoomItemHandler().RemoveAllFurniture(Session));
                }

                RoomData data = TargetRoom.RoomData;
                FirewindEnvironment.GetGame().GetRoomManager().UnloadRoom(TargetRoom);
                FirewindEnvironment.GetGame().GetRoomManager().QueueVoteRemove(data);

                using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                {
                    dbClient.runFastQuery("DELETE FROM rooms WHERE id = " + RoomId);
                    dbClient.runFastQuery("DELETE FROM user_favorites WHERE room_id = " + RoomId);
                    dbClient.runFastQuery("DELETE items, items_extradata, items_rooms "+
                                            "FROM items_rooms "+
                                            "INNER JOIN items ON (items.item_id = items_rooms.item_id) "+
                                            "LEFT JOIN items_extradata ON (items_extradata.item_id = items.item_id) "+
                                            "WHERE items_rooms.room_id = " + RoomId);
                    dbClient.runFastQuery("DELETE FROM room_rights WHERE room_id = " + RoomId);
                    dbClient.runFastQuery("UPDATE users SET home_room = '0' WHERE home_room = " + RoomId);
                }

                if (Session.GetHabbo().Username != data.Owner)
                {
                    FirewindEnvironment.GetGame().GetModerationTool().LogStaffEntry(Session.GetHabbo().Username, data.Name, "Room deletion", string.Format("Deleted room ID {0}", data.Id));
                }

                RoomData removedRoom = (from p in Session.GetHabbo().UsersRooms
                                        where p.Id == RoomId
                                        select p).SingleOrDefault();
                if (removedRoom != null)
                    Session.GetHabbo().UsersRooms.Remove(removedRoom);

                
            }
        }

        internal void LookAt()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null)
            {
                return;
            }

            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (User == null)
            {
                return;
            }

            User.Unidle();

            int X = Request.ReadInt32();
            int Y = Request.ReadInt32();

            if (X == User.X && Y == User.Y)
            {
                return;
            }

            int Rot = Rotation.Calculate(User.X, User.Y, X, Y);

            User.SetRot(Rot, false);
            User.UpdateNeeded = true;
        }

        internal void StartTyping()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null)
            {
                return;
            }

            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (User == null)
            {
                return;
            }

            ServerMessage Message = new ServerMessage(Outgoing.TypingStatus);
            Message.AppendInt32(User.VirtualId);
            Message.AppendInt32(1);
            Room.SendMessage(Message);
        }

        internal void StopTyping()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null)
            {
                return;
            }

            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (User == null)
            {
                return;
            }

            ServerMessage Message = new ServerMessage(Outgoing.TypingStatus);
            Message.AppendInt32(User.VirtualId);
            Message.AppendInt32(0);
            Room.SendMessage(Message);
        }

        internal void IgnoreUser()
        {

            Room Room = Session.GetHabbo().CurrentRoom;

            if (Room == null)
                return;

            String username = Request.ReadString();
            Habbo user = FirewindEnvironment.GetGame().GetClientManager().GetClientByUsername(username).GetHabbo();

            if (user == null)
                return;

            if (Session.GetHabbo().MutedUsers.Contains(user.Id))
                return;

            Session.GetHabbo().MutedUsers.Add(user.Id);

            Response.Init(Outgoing.UpdateIgnoreStatus);
            Response.AppendInt32(1);
            Response.AppendString(username);
            SendResponse();
        }

        internal void UnignoreUser()
        {

            Room Room = Session.GetHabbo().CurrentRoom;

            if (Room == null)
                return;

            String username = Request.ReadString();
            Habbo user = FirewindEnvironment.GetGame().GetClientManager().GetClientByUsername(username).GetHabbo();

            if (user == null)
                return;

            if (!Session.GetHabbo().MutedUsers.Contains(user.Id))
                return;

            Session.GetHabbo().MutedUsers.Remove(user.Id);

            Response.Init(Outgoing.UpdateIgnoreStatus);
            Response.AppendInt32(3);
            Response.AppendString(username);
            SendResponse();
        }

        internal void CanCreateRoomEvent()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CheckRights(Session, true))
            {
                return;
            }

            Boolean Allow = true;
            int ErrorCode = 0;

            if (Room.State != 0)
            {
                Allow = false;
                ErrorCode = 3;
            }

            Response.Init(Outgoing.CanCreateEvent);
            Response.AppendBoolean(Allow);
            Response.AppendInt32(ErrorCode);
            SendResponse();
        }

        internal void StartEvent()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CheckRights(Session, true) || Room.Event != null || Room.State != 0)
            {
                return;
            }

            int category = Request.ReadInt32();
            string name = FirewindEnvironment.FilterInjectionChars(Request.ReadString());
            string descr = FirewindEnvironment.FilterInjectionChars(Request.ReadString());
            int tagCount = Request.ReadInt32();

            Room.Event = new RoomEvent(Room.RoomId, name, descr, category, null);
            Room.Event.Tags = new ArrayList();

            for (int i = 0; i < tagCount; i++)
            {
                Room.Event.Tags.Add(FirewindEnvironment.FilterInjectionChars(Request.ReadString()));
            }

            Room.SendMessage(Room.Event.Serialize(Session));

            FirewindEnvironment.GetGame().GetRoomManager().GetEventManager().QueueAddEvent(Room.RoomData, Room.Event.Category);
        }

        internal void StopEvent()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CheckRights(Session, true) || Room.Event == null)
            {
                return;
            }

            //Room.Event = null;

            ServerMessage Message = new ServerMessage(Outgoing.RoomEvent);
            Message.AppendStringWithBreak("-1");
            Room.SendMessage(Message);

            FirewindEnvironment.GetGame().GetRoomManager().GetEventManager().QueueRemoveEvent(Room.RoomData, Room.Event.Category);
        }

        internal void EditEvent()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CheckRights(Session, true) || Room.Event == null)
            {
                return;
            }

            int category = Request.ReadInt32();
            string name = FirewindEnvironment.FilterInjectionChars(Request.ReadString());
            string descr = FirewindEnvironment.FilterInjectionChars(Request.ReadString());
            int tagCount = Request.ReadInt32();

            Room.Event.Category = category;
            Room.Event.Name = name;
            Room.Event.Description = descr;
            Room.Event.Tags = new ArrayList();

            FirewindEnvironment.GetGame().GetRoomManager().GetEventManager().QueueUpdateEvent(Room.RoomData, Room.Event.Category);

            for (int i = 0; i < tagCount; i++)
            {
                Room.Event.Tags.Add(FirewindEnvironment.FilterInjectionChars(Request.ReadString()));
            }

            Room.SendMessage(Room.Event.Serialize(Session));
        }

        internal void Wave()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null)
            {
                return;
            }

            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (User == null)
            {
                return;
            }

            User.Unidle();
            int Action = Request.ReadInt32();
            User.DanceId = 0;

            ServerMessage Message = new ServerMessage(Outgoing.Action);
            Message.AppendInt32(User.VirtualId);
            Message.AppendInt32(Action);
            Room.SendMessage(Message);
            //Logging.WriteLine(Action);
            if (Action == 5) // idle
            {
                User.IsAsleep = true;

                ServerMessage FallAsleep = new ServerMessage(Outgoing.IdleStatus);
                FallAsleep.AppendInt32(User.VirtualId);
                FallAsleep.AppendBoolean(User.IsAsleep);
                Room.SendMessage(FallAsleep);
            }

            FirewindEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Session, HabboHotel.Quests.QuestType.SOCIAL_WAVE);
        }

        internal void Sign()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null)
            {
                return;
            }

            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (User == null)
            {
                return;
            }

            User.Unidle();
            int SignId = Request.ReadInt32();
            User.AddStatus("sign", Convert.ToString(SignId));
            User.UpdateNeeded = true;
        }

        internal void GetUserTags()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null)
            {
                return;
            }

            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Request.ReadUInt32());

            if (User == null || User.IsBot)
            {
                return;
            }

            Response.Init(Outgoing.GetUserTags);
            Response.AppendUInt(User.GetClient().GetHabbo().Id);
            Response.AppendInt32(User.GetClient().GetHabbo().Tags.Count);

            foreach (string Tag in User.GetClient().GetHabbo().Tags)
            {
                Response.AppendStringWithBreak(Tag);
            }

            SendResponse();
        }

        internal void GetUserBadges()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null)
            {
                return;
            }

            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Request.ReadUInt32());

            if (User == null || User.IsBot)
                return;
            if (User.GetClient() == null)
                return;

            // CdjUzYZJIACH_RespectEarned1JACH_EmailVerification1E^jUzYZH

            Response.Init(Outgoing.GetUserBadges);
            Response.AppendUInt(User.GetClient().GetHabbo().Id);
            Response.AppendInt32(User.GetClient().GetHabbo().GetBadgeComponent().EquippedCount);

            foreach (Badge Badge in User.GetClient().GetHabbo().GetBadgeComponent().BadgeList.Values)
            {
                if (Badge.Slot <= 0)
                {
                    continue;
                }

                Response.AppendInt32(Badge.Slot);
                Response.AppendStringWithBreak(Badge.Code);
            }

            SendResponse();
        }

        internal void RateRoom()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || Session.GetHabbo().RatedRooms.Contains(Room.RoomId) || Room.CheckRights(Session, true))
            {
                return;
            }

            int Rating = Request.ReadInt32();

            switch (Rating)
            {
                case -1:

                    Room.Score--;
                    Room.RoomData.Score--;
                    break;

                case 1:

                    Room.Score++;
                    Room.RoomData.Score++;
                    break;

                default:

                    return;
            }

            FirewindEnvironment.GetGame().GetRoomManager().QueueVoteAdd(Room.RoomData);

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.runFastQuery("UPDATE rooms SET score = " + Room.Score + " WHERE id = " + Room.RoomId);
            }

            Session.GetHabbo().RatedRooms.Add(Room.RoomId);

            Response.Init(Outgoing.RateRoom);
            Response.AppendInt32(Room.Score);
            SendResponseWithOwnerParam();
        }

        internal void Dance()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null)
            {
                return;
            }

            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (User == null)
            {
                return;
            }

            User.Unidle();

            int DanceId = Request.ReadInt32();

            if (DanceId < 0 || DanceId > 4 || (!Session.GetHabbo().GetSubscriptionManager().HasSubscription("habbo_vip") && DanceId > 1))
            {
                DanceId = 0;
            }

            if (DanceId > 0 && User.CarryItemID > 0)
            {
                User.CarryItem(0);
            }

            User.DanceId = DanceId;

            ServerMessage DanceMessage = new ServerMessage(Outgoing.Dance);
            DanceMessage.AppendInt32(User.VirtualId);
            DanceMessage.AppendInt32(DanceId);
            Room.SendMessage(DanceMessage);

            FirewindEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Session, HabboHotel.Quests.QuestType.SOCIAL_DANCE);
        }

        internal void AnswerDoorbell()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CheckRights(Session))
            {
                return;
            }

            string Name = Request.ReadString();
            bool Result = Request.ReadBoolean();

            GameClient Client = FirewindEnvironment.GetGame().GetClientManager().GetClientByUsername(Name);

            if (Client == null)
            {
                return;
            }

            if (Result)
            {
                Client.GetHabbo().LoadingChecksPassed = true;

                Client.GetMessageHandler().Response.Init(Outgoing.FlatAccessible);
                Client.GetMessageHandler().Response.AppendString("");
                Client.GetMessageHandler().SendResponse();
            }
            else
            {
                Client.GetMessageHandler().Response.Init(Outgoing.InvalidDoorBell);
                Client.GetMessageHandler().Response.AppendString("");
                Client.GetMessageHandler().SendResponse();
            }
        }

        internal void ApplyRoomEffect()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CheckRights(Session, true))
            {
                return;
            }

            UserItem Item = Session.GetHabbo().GetInventoryComponent().GetItem(Request.ReadUInt32());

            if (Item == null)
            {
                return;
            }

            string type = "floor";

            if (Item.GetBaseItem().Name.ToLower().Contains("wallpaper"))
            {
                type = "wallpaper";
            }
            else if (Item.GetBaseItem().Name.ToLower().Contains("landscape"))
            {
                type = "landscape";
            }

            switch (type)
            {
                case "floor":

                    Room.Floor = Item.Data.ToString();
                    Room.RoomData.Floor = Item.Data.ToString();

                    FirewindEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Session, HabboHotel.Quests.QuestType.FURNI_DECORATION_FLOOR);
                    break;

                case "wallpaper":

                    Room.Wallpaper = Item.Data.ToString();
                    Room.RoomData.Wallpaper = Item.Data.ToString();

                    FirewindEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Session, HabboHotel.Quests.QuestType.FURNI_DECORATION_WALL);
                    break;

                case "landscape":

                    Room.Landscape = Item.Data.ToString();
                    Room.RoomData.Landscape = Item.Data.ToString();
                    break;
            }

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("UPDATE rooms SET " + type + " = @extradata WHERE id = " + Room.RoomId);
                dbClient.addParameter("extradata", Item.Data);
                dbClient.runQuery();
            }

            Session.GetHabbo().GetInventoryComponent().RemoveItem(Item.Id, false);

            ServerMessage Message = new ServerMessage(Outgoing.RoomDecoration);
            Message.AppendStringWithBreak(type);
            Message.AppendStringWithBreak(Item.Data.ToString());
            Room.SendMessage(Message);
        }

        internal void PlacePostIt()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (Room == null || !Room.CheckRights(Session))
            {
                return;
            }
            
            uint itemId = Request.ReadUInt32();
            string locationData = Request.ReadString();

            UserItem item = Session.GetHabbo().GetInventoryComponent().GetItem(itemId);

            if (item == null || Room == null)
                return;

            try
            {
                WallCoordinate coordinate = new WallCoordinate(":" + locationData.Split(':')[1]);

                RoomItem RoomItem = new RoomItem(item.Id, Room.RoomId, item.BaseItem, item.Data, item.Extra, coordinate, Room);

                if (Room.GetRoomItemHandler().SetWallItem(Session, RoomItem))
                {
                    Session.GetHabbo().GetInventoryComponent().RemoveItem(itemId, true);
                }
            }
            catch
            {
                Response.Init(516);
                Response.AppendInt32(11);
                SendResponse();
                return;
            }
        }

        internal void PlaceItem()
        {
            // AZ@J16 10 10 0

            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CheckRights(Session))
            {
                return;
            }



            string PlacementData = Request.ReadString();
            string[] DataBits = PlacementData.Split(' ');
            uint ItemId = uint.Parse(DataBits[0].Replace("-",""));

            UserItem Item = Session.GetHabbo().GetInventoryComponent().GetItem(ItemId);

            if (Item == null)
            {
                return;
            }
            //bool UpdateNeeded = false;

            switch (Item.GetBaseItem().InteractionType)
            {
                case Firewind.HabboHotel.Items.InteractionType.dimmer:
                    {
                        MoodlightData moodData = Room.MoodlightData;
                        if (moodData != null && Room.GetRoomItemHandler().GetItem(moodData.ItemId) != null)
                            Session.SendNotif(LanguageLocale.GetValue("user.maxmoodlightsreached"));
                        break;
                    }
            }

            // Wall Item
            if (DataBits[1].StartsWith(":"))
            {
                try
                {
                    WallCoordinate coordinate = new WallCoordinate(":" + PlacementData.Split(':')[1]);
                    RoomItem RoomItem = new RoomItem(Item.Id, Room.RoomId, Item.BaseItem, Item.Data, Item.Extra, coordinate, Room);

                    if (Room.GetRoomItemHandler().SetWallItem(Session, RoomItem))
                    {
                        Session.GetHabbo().GetInventoryComponent().RemoveItem(ItemId, true);
                    }
                }
                catch
                {
                    // Invalid wallitem
                    /*
                    Response.Init(516);
                    Response.AppendInt32(11);
                    SendResponse();*/
                    return;
                }
            }
            // Floor Item
            else
            {
                int X = int.Parse(DataBits[1]);
                int Y = int.Parse(DataBits[2]);
                int Rot = int.Parse(DataBits[3]);

                if (Session.GetHabbo().forceRot > -1)
                    Rot = Session.GetHabbo().forceRot;

                RoomItem RoomItem = new RoomItem(Item.Id, Room.RoomId, Item.BaseItem, Item.Data, Item.Extra, X, Y, 0, Rot, Room);

                if (Room.GetRoomItemHandler().SetFloorItem(Session, RoomItem, X, Y, Rot, true, false, true))
                {
                    Session.GetHabbo().GetInventoryComponent().RemoveItem(ItemId, true);
                }
                else // Placing failed, tell client to put item back
                {
                    Response.Init(Outgoing.ObjectUpdate);
                    RoomItem.Serialize(Response, Room.OwnerId);
                    SendResponse();
                    return;
                }

                if (WiredUtillity.TypeIsWired(Item.GetBaseItem().InteractionType))
                {
                    WiredSaver.HandleDefaultSave(Item.Id, Room);
                }

                FirewindEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Session, HabboHotel.Quests.QuestType.FURNI_PLACE);
            }
            //if (UpdateNeeded)
            //    Room.SaveFurniture();
            
        }

        internal void TakeItem()
        {
            int junk = Request.ReadInt32();
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CheckRights(Session, true))
            {
                return;
            }

            RoomItem Item = Room.GetRoomItemHandler().GetItem(Request.ReadUInt32());

            if (Item == null)
            {
                return;
            }



            if (Item.GetBaseItem().InteractionType == Firewind.HabboHotel.Items.InteractionType.postit)
                return;

            Room.GetRoomItemHandler().RemoveFurniture(Session, Item.Id);
            Session.GetHabbo().GetInventoryComponent().AddNewItem(Item.Id, Item.BaseItem, Item.data, Item.Extra, true, true, 0);
            Session.GetHabbo().GetInventoryComponent().UpdateItems(false);

            FirewindEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Session, HabboHotel.Quests.QuestType.FURNI_PICK);
        }

        internal void MoveItem()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CheckRights(Session))
            {
                return;
            }

            RoomItem Item = Room.GetRoomItemHandler().GetItem(Request.ReadUInt32());

            if (Item == null)
            {
                return;
            }

            if (Item.wiredHandler != null)
            {
                using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                {
                    //Item.wiredHandler.DeleteFromDatabase(dbClient);
                    Item.wiredHandler.Dispose();
                    Room.GetWiredHandler().RemoveFurniture(Item);
                }
                Item.wiredHandler = null;
            }

            if (Item.wiredCondition != null)
            {
                using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                {
                    //Item.wiredCondition.DeleteFromDatabase(dbClient);
                    Item.wiredCondition.Dispose();
                    Room.GetWiredHandler().conditionHandler.ClearTile(Item.Coordinate);
                }
                Item.wiredCondition = null;
            }

            int x = Request.ReadInt32();
            int y = Request.ReadInt32();
            int Rotation = Request.ReadInt32();
            int Junk = Request.ReadInt32();

            bool UpdateNeeded = false;

            if (Item.GetBaseItem().InteractionType == Firewind.HabboHotel.Items.InteractionType.teleport)
                UpdateNeeded = true;

            if (x != Item.GetX || y != Item.GetY)
            {
                FirewindEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Session, HabboHotel.Quests.QuestType.FURNI_MOVE);
            }

            if (Rotation != Item.Rot)
            {
                FirewindEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Session, HabboHotel.Quests.QuestType.FURNI_ROTATE);
            }

            if (!Room.GetRoomItemHandler().SetFloorItem(Session, Item, x, y, Rotation, false, false, true)) // Placing failed, tell client to put item back
            {
                Response.Init(Outgoing.ObjectUpdate);
                Item.Serialize(Response, Room.OwnerId);
                SendResponse();
                return;
            }

            if (Item.GetZ >= 0.1)
            {
                FirewindEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Session, HabboHotel.Quests.QuestType.FURNI_STACK);
            }

            if (UpdateNeeded)
            {
                using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                {
                    Room.GetRoomItemHandler().SaveFurniture(dbClient);
                }
            }
        }

        internal void MoveWallItem()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CheckRights(Session))
            {
                return;
            }

            uint itemID = Request.ReadUInt32();
            string wallPositionData = Request.ReadString();

            RoomItem Item = Room.GetRoomItemHandler().GetItem(itemID);

            if (Item == null)
                return;

            try
            {
                WallCoordinate coordinate = new WallCoordinate(":" + wallPositionData.Split(':')[1]);
                Item.wallCoord = coordinate;
            }
            catch
            {
                // invalid pos
                /*
                Response.Init(516);
                Response.AppendInt32(11);
                SendResponse();*/

                return;
            }

            Room.GetRoomItemHandler().UpdateItem(Item);

            /*ServerMessage LeaveMessage = new ServerMessage();
            LeaveMessage.AppendRawUInt(Item.Id);
            LeaveMessage.AppendStringWithBreak(string.Empty);
            LeaveMessage.AppendBoolean(false);
            Room.SendMessage(LeaveMessage);*/

            ServerMessage Message = new ServerMessage(Outgoing.UpdateWallItemOnRoom);
            Item.Serialize(Message, Room.OwnerId);
            Room.SendMessage(Message);
        }

        internal void TriggerItem()
        {

            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null)
            {
                return;
            }

            int itemID = Request.ReadInt32();


            if (itemID < 0)
            {
                int data = Request.ReadInt32();
                Logging.LogDebug(string.Format("Item triggered with negative ({0}) item ID, data was {1}!", itemID, data));

                itemID = Math.Abs(itemID);
            }

            RoomItem Item = Room.GetRoomItemHandler().GetItem((uint)itemID);
            if (Item == null)
            {
                return;
            }

            Boolean hasRights = false;

            if (Room.CheckRights(Session))
            {
                hasRights = true;
            }

            int request = Request.ReadInt32();
            if(Item.Interactor.OnTrigger(Session, Item, request, hasRights))
                Item.OnTrigger(Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id));
            Room.GetRoomItemHandler().UpdateItem(Item);

            if (Room.GotWired() && !WiredUtillity.TypeIsWired(Item.GetBaseItem().InteractionType))
            {
                bool shouldBeHandled = false;
                int x = Item.GetX;
                int y = Item.GetY;
                Point up = new Point(x, y + 1);
                Point down = new Point(x + 1, y);
                Point left = new Point(x, y - 1);
                Point right = new Point(x - 1, y);

                foreach (RoomItem item in Room.GetGameMap().GetCoordinatedItems(up))
                {
                    if (WiredHandler.TypeIsWire(item.GetBaseItem().InteractionType))
                        shouldBeHandled = true;
                }

                foreach (RoomItem item in Room.GetGameMap().GetCoordinatedItems(down))
                {
                    if (WiredHandler.TypeIsWire(item.GetBaseItem().InteractionType))
                        shouldBeHandled = true;
                }

                foreach (RoomItem item in Room.GetGameMap().GetCoordinatedItems(left))
                {
                    if (WiredHandler.TypeIsWire(item.GetBaseItem().InteractionType))
                        shouldBeHandled = true;
                }

                foreach (RoomItem item in Room.GetGameMap().GetCoordinatedItems(right))
                {
                    if (WiredHandler.TypeIsWire(item.GetBaseItem().InteractionType))
                        shouldBeHandled = true;
                }

                if (shouldBeHandled)
                    Room.GetWiredHandler().TriggerOnWire(Item.Coordinate);
                else
                    Room.GetWiredHandler().RemoveWiredItem(Item.Coordinate);
            }
        }

        internal void TriggerItemDiceSpecial()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null)
            {
                return;
            }

            RoomItem Item = Room.GetRoomItemHandler().GetItem(Request.ReadUInt32());

            if (Item == null)
            {
                return;
            }

            Boolean hasRights = false;

            if (Room.CheckRights(Session))
            {
                hasRights = true;
            }

            Item.Interactor.OnTrigger(Session, Item, -1, hasRights);
            Item.OnTrigger(Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id));
        }

        internal void OpenPostit()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            //TODO fix post it
            if (Room == null)
            {
                return;
            }

            RoomItem Item = Room.GetRoomItemHandler().GetItem(Request.ReadUInt32());

            if (Item == null || Item.GetBaseItem().InteractionType != Firewind.HabboHotel.Items.InteractionType.postit)
            {
                return;
            }

            // @p181855059CFF9C stickynotemsg
            Response.Init(Outgoing.OpenPostIt);
            Response.AppendStringWithBreak(Item.Id.ToString());
            Response.AppendStringWithBreak(((StringData)Item.data).Data);
            SendResponse();
        }

        internal void SavePostit()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null)
            {
                return;
            }

            RoomItem Item = Room.GetRoomItemHandler().GetItem(Request.ReadUInt32());

            if (Item == null || Item.GetBaseItem().InteractionType != Firewind.HabboHotel.Items.InteractionType.postit)
            {
                return;
            }
            String Color = Request.ReadString();
            String Text = FirewindEnvironment.FilterInjectionChars(Request.ReadString(), true);

            if (!Room.CheckRights(Session))
            {
                if (!Text.StartsWith(((StringData)Item.data).Data))
                {
                    return; // we can only ADD stuff! older stuff changed, this is not allowed
                }
            }

            switch (Color)
            {
                case "FFFF33":
                case "FF9CFF":
                case "9CCEFF":
                case "9CFF9C":

                    break;

                default:

                    return; // invalid color
            }

            ((StringData)Item.data).Data = Color + " " + Text;
            Item.UpdateState(true, true);
        }

        internal void DeletePostit()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CheckRights(Session, true))
            {
                return;
            }

            RoomItem Item = Room.GetRoomItemHandler().GetItem(Request.ReadUInt32());

            if (Item == null || Item.GetBaseItem().InteractionType != Firewind.HabboHotel.Items.InteractionType.postit)
            {
                return;
            }

            Room.GetRoomItemHandler().RemoveFurniture(Session, Item.Id);
        }


        internal void AnswerInfobusPoll()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            int AnswerId = Request.ReadInt32();
            int QuestionId = Room.CurrentPollId;
            DataTable Data = null;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("SELECT * FROM infobus_answers WHERE question_id = '" + QuestionId + "'");
                Data = dbClient.getTable();

                dbClient.setQuery("INSERT INTO `infobus_results` (`question_id`, `answer_id`) VALUES ('" + QuestionId + "', '" + Data.Rows[AnswerId - 1]["id"] + "')");
                dbClient.insertQuery();

            }


        }

        internal void OpenPresent()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CheckRights(Session, true))
            {
                return;
            }

            uint ItemId = Request.ReadUInt32();
            RoomItem Present = Room.GetRoomItemHandler().GetItem(ItemId);
            //Logging.WriteLine("Item of present >> " + ItemId);
            if (Present == null)
            {
                return;
            }
            Present.MagicRemove = true;
            ServerMessage magicResponse = new ServerMessage(Outgoing.ObjectUpdate);
            Present.Serialize(magicResponse, Room.OwnerId);
            Room.SendMessage(magicResponse);

            DataRow Data = null;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("SELECT base_id,amount,extra_data FROM user_presents WHERE item_id = " + Present.Id + "");
                Data = dbClient.getRow();
            }

            if (Data == null)
            {
                Room.GetRoomItemHandler().RemoveFurniture(Session, Present.Id);
                return;
            }

            Item BaseItem = FirewindEnvironment.GetGame().GetItemManager().GetItem(Convert.ToUInt32(Data["base_id"]));

            if (BaseItem == null)
            {
                Room.GetRoomItemHandler().RemoveFurniture(Session, Present.Id);
                return;
            }
            if (BaseItem.Type.ToString().ToLower().Equals("s") && BaseItem.InteractionType != InteractionType.teleport)
            {
                Room.GetRoomItemHandler().RemoveFurniture(Session, Present.Id);

                using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                {
                    dbClient.runFastQuery("UPDATE items SET base_id = '" + Data["base_id"] + "' WHERE item_id = " + Present.Id);
                    dbClient.runFastQuery("UPDATE items_extradata SET data = '' WHERE item_id = " + Present.Id);
                    dbClient.runFastQuery("DELETE FROM user_presents WHERE item_id = " + Present.Id);
                }

                string type = Present.GetBaseItem().Type.ToString().ToLower();
                string ExtraData = Present.data.GetData().ToString();
                Present.BaseItem = Convert.ToUInt32(Data["base_id"]);
                Present.refreshItem();
                Present.data = new StringData("");
                //Logging.WriteLine("Hallo, new BaseItem: " + Present.GetBaseItem().Name);
                if (!Room.GetRoomItemHandler().SetFloorItem(Session, Present, Present.GetX, Present.GetY, Present.Rot, true, false, true))
                {
                    Session.SendNotif("Ha ocurrido un error al crear tu regalo!"); // spanish, what a suprise
                    return;
                }
                /*Response.Init(219);
                Response.AppendUInt(Present.Id);
                SendResponse();*/

                Response.Init(Outgoing.OpenGift);
                Response.AppendStringWithBreak(BaseItem.Type.ToString());
                Response.AppendInt32(BaseItem.SpriteId);
                Response.AppendStringWithBreak(BaseItem.Name);
                Response.AppendUInt(Present.Id);
                Response.AppendString(type);
                Response.AppendBoolean(true);
                Response.AppendString(ExtraData);
                SendResponse();
            }
            else
            {
                Room.GetRoomItemHandler().RemoveFurniture(Session, Present.Id);
                using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                {
                    dbClient.runFastQuery("DELETE FROM user_presents WHERE item_id = " + Present.Id);
                }
                Session.GetMessageHandler().GetResponse().Init(Outgoing.UnseenItems);
                Session.GetMessageHandler().GetResponse().AppendInt32(1); // items
                int Type = 2;
                if (BaseItem.Type.ToString().ToLower().Equals("s"))
                {
                    if (BaseItem.InteractionType == InteractionType.pet)
                        Type = 3;
                    else
                        Type = 1;
                }
                Session.GetMessageHandler().GetResponse().AppendInt32(Type);
                List<UserItem> items = FirewindEnvironment.GetGame().GetCatalog().DeliverItems(Session, BaseItem, (int)Data["amount"], (String)Data["extra_data"]);
                Session.GetMessageHandler().GetResponse().AppendInt32(items.Count);
                foreach (UserItem u in items)
                    Session.GetMessageHandler().GetResponse().AppendUInt(u.Id);
                Session.GetMessageHandler().SendResponse();
                Session.GetHabbo().GetInventoryComponent().UpdateItems(false);
            }
            

            //FirewindEnvironment.GetGame().GetCatalog().DeliverItems(Session, BaseItem, (int)Data["amount"], (String)Data["extra_data"]);
        }

        internal void GetMoodlight()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CheckRights(Session, true))
            {
                //Logging.WriteLine("error loading! " + (Room.MoodlightData == null));
                return;
            }

            if (Room.MoodlightData == null)
            {
                foreach(RoomItem item in Room.GetRoomItemHandler().mWallItems.Values)
                {
                    if (item.GetBaseItem().InteractionType == InteractionType.dimmer)
                        Room.MoodlightData = new MoodlightData(item.Id);
                }
            }

            if (Room.MoodlightData == null)
                return;

            Response.Init(Outgoing.DimmerData);
            Response.AppendInt32(Room.MoodlightData.Presets.Count);
            Response.AppendInt32(Room.MoodlightData.CurrentPreset);

                int i = 0;

                foreach (MoodlightPreset Preset in Room.MoodlightData.Presets)
                {
                    i++;

                    Response.AppendInt32(i);
                    Response.AppendInt32(int.Parse(FirewindEnvironment.BoolToEnum(Preset.BackgroundOnly)) + 1);
                    Response.AppendStringWithBreak(Preset.ColorCode);
                    Response.AppendInt32(Preset.ColorIntensity);
                }
            

            SendResponse();
        }

        internal void UpdateMoodlight()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CheckRights(Session, true) || Room.MoodlightData == null)
            {
                return;
            }

            RoomItem Item = Room.GetRoomItemHandler().GetItem(Room.MoodlightData.ItemId);

            if (Item == null || Item.GetBaseItem().InteractionType != InteractionType.dimmer)
                return;

            // EVIH@G#EA4532RbI

            int Preset = Request.ReadInt32();
            int BackgroundMode = Request.ReadInt32();
            string ColorCode = Request.ReadString();
            int Intensity = Request.ReadInt32();

            bool BackgroundOnly = false;

            if (BackgroundMode >= 2)
            {
                BackgroundOnly = true;
            }

            Room.MoodlightData.Enabled = true;
            Room.MoodlightData.CurrentPreset = Preset;
            Room.MoodlightData.UpdatePreset(Preset, ColorCode, Intensity, BackgroundOnly);

            ((StringData)Item.data).Data = Room.MoodlightData.GenerateExtraData();
            Item.UpdateState();
        }

        internal void SwitchMoodlightStatus()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CheckRights(Session, true) || Room.MoodlightData == null)
            {
                return;
            }

            RoomItem Item = Room.GetRoomItemHandler().GetItem(Room.MoodlightData.ItemId);

            if (Item == null || Item.GetBaseItem().InteractionType != InteractionType.dimmer)
                return;

            if (Room.MoodlightData.Enabled)
            {
                Room.MoodlightData.Disable();
            }
            else
            {
                Room.MoodlightData.Enable();
            }

            ((StringData)Item.data).Data = Room.MoodlightData.GenerateExtraData();
            Item.UpdateState();
        }

        internal void InitTrade()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CanTradeInRoom)
            {
                return;
            }

            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            RoomUser User2 = Room.GetRoomUserManager().GetRoomUserByVirtualId(Request.ReadInt32());

            if (User2 == null || User2.GetClient() == null || User2.GetClient().GetHabbo() == null)
                return;

            bool IsDisabled = false;
            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("SELECT block_trade FROM users WHERE id = " + User2.GetClient().GetHabbo().Id);
                IsDisabled = FirewindEnvironment.EnumToBool(dbClient.getString());
            }

            if (IsDisabled)
            {
                Session.SendNotif(LanguageLocale.GetValue("user.tradedisabled"));
                return;
            }
            else
                Room.TryStartTrade(User, User2);
            
        }

        internal void OfferTradeItem()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CanTradeInRoom)
            {
                return;
            }

            Trade Trade = Room.GetUserTrade(Session.GetHabbo().Id);
            UserItem Item = Session.GetHabbo().GetInventoryComponent().GetItem(Request.ReadUInt32());

            if (Trade == null || Item == null)
            {
                return;
            }

            Trade.OfferItem(Session.GetHabbo().Id, Item);
        }

        internal void TakeBackTradeItem()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CanTradeInRoom)
            {
                return;
            }

            Trade Trade = Room.GetUserTrade(Session.GetHabbo().Id);
            UserItem Item = Session.GetHabbo().GetInventoryComponent().GetItem(Request.ReadUInt32());

            if (Trade == null || Item == null)
            {
                return;
            }

            Trade.TakeBackItem(Session.GetHabbo().Id, Item);
        }

        internal void StopTrade()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CanTradeInRoom)
            {
                return;
            }

            Room.TryStopTrade(Session.GetHabbo().Id);
        }

        internal void AcceptTrade()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CanTradeInRoom)
            {
                return;
            }

            Trade Trade = Room.GetUserTrade(Session.GetHabbo().Id);

            if (Trade == null)
            {
                return;
            }

            Trade.Accept(Session.GetHabbo().Id);
        }

        internal void UnacceptTrade()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CanTradeInRoom)
            {
                return;
            }

            Trade Trade = Room.GetUserTrade(Session.GetHabbo().Id);

            if (Trade == null)
            {
                return;
            }

            Trade.Unaccept(Session.GetHabbo().Id);
        }

        internal void CompleteTrade()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CanTradeInRoom)
            {
                return;
            }

            Trade Trade = Room.GetUserTrade(Session.GetHabbo().Id);

            if (Trade == null)
            {
                return;
            }

            Trade.CompleteTrade(Session.GetHabbo().Id);
        }

        internal void GiveRespect()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || Session.GetHabbo().DailyRespectPoints <= 0)
            {
                return;
            }

            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Request.ReadUInt32());
            
            if (User == null || User.GetClient().GetHabbo().Id == Session.GetHabbo().Id || User.IsBot)
            {
                return;
            }

            FirewindEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Session, HabboHotel.Quests.QuestType.SOCIAL_RESPECT);

            FirewindEnvironment.GetGame().GetAchievementManager().ProgressUserAchievement(Session, "ACH_RespectEarned", 1);
            FirewindEnvironment.GetGame().GetAchievementManager().ProgressUserAchievement(User.GetClient(), "ACH_RespectGiven", 1);

            Session.GetHabbo().DailyRespectPoints--;
            User.GetClient().GetHabbo().Respect++;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.runFastQuery("UPDATE users SET respect = respect + 1 WHERE id = " + User.GetClient().GetHabbo().Id);
                dbClient.runFastQuery("UPDATE users SET daily_respect_points = daily_respect_points - 1 WHERE id = " + Session.GetHabbo().Id);
            }

            // FxkqUzYP_
            ServerMessage Message = new ServerMessage(Outgoing.GiveRespect);
            Message.AppendUInt(User.GetClient().GetHabbo().Id);
            Message.AppendInt32(User.GetClient().GetHabbo().Respect);
            Room.SendMessage(Message);
        }

        internal void ApplyEffect()
        {
            Session.GetHabbo().GetAvatarEffectsInventoryComponent().ApplyEffect(Request.ReadInt32());
        }

        internal void EnableEffect()
        {
            Session.GetHabbo().GetAvatarEffectsInventoryComponent().EnableEffect(Request.ReadInt32());
        }

        internal void RecycleItems()
        {
            if (!Session.GetHabbo().InRoom)
            {
                return;
            }

            int itemCount = Request.ReadInt32();

            if (itemCount != 5)
            {
                return;
            }

            for (int i = 0; i < itemCount; i++)
            {
                UserItem Item = Session.GetHabbo().GetInventoryComponent().GetItem(Request.ReadUInt32());

                if (Item != null && Item.GetBaseItem().AllowRecycle)
                {
                    Session.GetHabbo().GetInventoryComponent().RemoveItem(Item.Id, false);
                }
                else
                {
                    return;
                }
            }

            uint newItemId;// = FirewindEnvironment.GetGame().GetCatalog().GenerateItemId();
            EcotronReward Reward = FirewindEnvironment.GetGame().GetCatalog().GetRandomEcotronReward();

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("INSERT INTO user_items (user_id,base_item,extra_data) VALUES ( @userid ,1478, @timestamp)");
                dbClient.addParameter("userid", (int)Session.GetHabbo().Id);
                dbClient.addParameter("timestamp", DateTime.Now.ToLongDateString());

                newItemId = (uint)dbClient.insertQuery();

                dbClient.runFastQuery("INSERT INTO user_presents (item_id,base_id,amount,extra_data) VALUES (" + newItemId + "," + Reward.BaseId + ",1,'')");
            }

            Session.GetHabbo().GetInventoryComponent().UpdateItems(true);

            Response.Init(508);
            Response.AppendBoolean(true);
            Response.AppendUInt(newItemId);
            SendResponse();
        }

        internal void RedeemExchangeFurni()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CheckRights(Session, true))
            {
                return;
            }

            RoomItem Exchange = Room.GetRoomItemHandler().GetItem(Request.ReadUInt32());

            if (Exchange == null)
            {
                return;
            }

            if (!Exchange.GetBaseItem().Name.StartsWith("CF_") && !Exchange.GetBaseItem().Name.StartsWith("CFC_"))
            {
                return;
            }
            
            string[] Split = Exchange.GetBaseItem().Name.Split('_');
            int Value = int.Parse(Split[1]);

            if (Value > 0)
            {
                Session.GetHabbo().Credits += Value;
                Session.GetHabbo().UpdateCreditsBalance();
            }

            Room.GetRoomItemHandler().RemoveFurniture(Session, Exchange.Id);

            Response.Init(Outgoing.UpdateInventary);
            SendResponse();
        }

        internal void GiveHanditem()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            //if (Room == null || Room.IsPublic || (!Room.AllowPets && !Room.CheckRights(Session, true)))
            if (Room == null)
                return;

            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (User == null)
                return;

            RoomUser toGive = Room.GetRoomUserManager().GetRoomUserByHabbo(Request.ReadUInt32());
            if (toGive == null)
                return;

            if (User.CarryItemID > 0 && User.CarryTimer > 0)
            {
                toGive.CarryItem(User.CarryItemID);
                User.CarryItem(0);
            }
        }

        internal void RemoveHanditem()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            //if (Room == null || Room.IsPublic || (!Room.AllowPets && !Room.CheckRights(Session, true)))
            if (Room == null)
                return;

            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (User == null)
                return;

            if (User.CarryItemID > 0 && User.CarryTimer > 0)
                User.CarryItem(0);
        }

        internal void SetLookTransfer()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CheckRights(Session))
                return;

            uint ItemID = Request.ReadUInt32();

            string Gender = Request.ReadString().ToUpper();
            string Look = FirewindEnvironment.FilterInjectionChars(Request.ReadString());

            RoomItem RoomItemToSet = Room.GetRoomItemHandler().mFloorItems.GetValue(ItemID);

            if (Gender.Length > 1)
                return;

            if (Gender != "M" && Gender != "F")
                return;

            RoomItemToSet.Figure = FirewindEnvironment.FilterFigure(Look);
            RoomItemToSet.Gender = Gender;

            ((StringData)RoomItemToSet.data).Data = Gender + ":" + Look;
        }

        //internal void PetRaces()
        //{
        //    int PetType = Request.PopWiredInt32();
        //    Response.Init(827);

        //    if (PetType == 0) // dogs 
        //        Response.AppendString("HQFSDQDRDSCPDQCRCSBPCQEPERESEPFKJRBIHSARAQAPAQBPB");
        //    else if (PetType == 1) // cats 
        //        Response.AppendString("IQFSDQDRDSCPDQCRCSBPCQEPERESEPFKJRBIHSARAQAPAQBPB");
        //    else if (PetType == 2) // croc 
        //        Response.AppendString("JPCHIJKPAQARASAPBQBRBSB");
        //    else if (PetType == 3) // terreir 
        //        Response.AppendString("KSAHIJKPAQARA");
        //    else if (PetType == 4) // bear 
        //        Response.AppendString("PAPAHIJK");
        //    else if (PetType == 5) // pigs 
        //        Response.AppendString("QASAHIJKQASAPB");
        //    else if (PetType == 6) // lion 
        //        Response.AppendString("RASAHIJKPAQASB");
        //    else if (PetType == 7) // rhino 
        //        Response.AppendString("SASAHIJPAQARASA");
        //    else if (PetType == 8) // spider 
        //        Response.AppendString("PBQCHIRBSBRCJKPAQARASAPBQB");

        //    SendResponse();
        //}  

        internal void SaveWired()
        {
            uint itemID = Request.ReadUInt32();
            Room room = Session.GetHabbo().CurrentRoom;
            if (room == null || !room.CheckRights(Session))
                return;

            WiredSaver.HandleSave(Session, itemID, room, Request);
        }

        internal void SaveWiredConditions()
        {
            uint itemID = Request.ReadUInt32();
            Room room = Session.GetHabbo().CurrentRoom;
            if (room == null || !room.CheckRights(Session))
                return;

            WiredSaver.HandleConditionSave(Session, itemID, room, Request);
        }

        internal void ApplySnapshot()
        {
            uint itemID = Request.ReadUInt32();
            Room room = Session.GetHabbo().CurrentRoom;
            if (room == null || !room.CheckRights(Session))
                return;

            RoomItem item = room.GetRoomItemHandler().GetItem(itemID);
            if (item == null)
                return;

            IWiredMatchFurni matchFurni = item.wiredHandler as IWiredMatchFurni;
            if (matchFurni == null)
                matchFurni = item.wiredCondition as IWiredMatchFurni;

            if (matchFurni != null)
                matchFurni.ApplySnapshot(room);
        }

        #region Unused
        internal void GetAdvertisement()
        {
            //RoomAdvertisement Ad = FirewindEnvironment.GetGame().GetAdvertisementManager().GetRandomRoomAdvertisement();

            //Response.Init(258);

            //if (Ad == null)
            //{
            //    Response.AppendStringWithBreak("");
            //    Response.AppendStringWithBreak("");
            //}
            //else
            //{
            //    Response.AppendStringWithBreak(Ad.AdImage);
            //    Response.AppendStringWithBreak(Ad.AdLink);

            //    Ad.OnView();
            //}

            //SendResponse();
        }

        // OpenPub
        internal void OpenConnection()
        {
            //int Junk = Request.PopWiredInt32();
            //uint Id = Request.PopWiredUInt();
            //int Junk2 = Request.PopWiredInt32();

            //RoomData Data = FirewindEnvironment.GetGame().GetRoomManager().GenerateRoomData(Id);

            //if (Data == null)
            //{
            //    return;
            //}

            //PrepareRoomForUser(Data.Id, "");
        }
        #endregion

        internal void FindFriends()
        {
            var activeRooms = FirewindEnvironment.GetGame().GetRoomManager().GetActiveRooms();

            if (activeRooms == null || activeRooms.Count() == 0)
            {
                Session.SendNotif("There are no active rooms to join.");
                return;
            }

            Random rnd = new Random();
            var randomRoom = activeRooms[rnd.Next(activeRooms.Count())];
            Room randomRoomPair = FirewindEnvironment.GetGame().GetRoomManager()
                                     .LoadRoom(randomRoom.Key.Id);

            Session.GetMessageHandler().ForwardToRoom((int)randomRoomPair.RoomId);
        }

        internal void MannequeNameChange()
        {
            if (Session.GetHabbo().CurrentRoom == null || !Session.GetHabbo().CurrentRoom.CheckRights(Session, true))
                return;

            int itemID = Request.ReadInt32();
            string newName = Request.ReadString(Encoding.UTF8);

            RoomItem item = Session.GetHabbo().CurrentRoom.GetRoomItemHandler().GetItem((uint)itemID);
            if (item == null || item.data.GetTypeID() != 1)
                return;

            MapStuffData data = ((MapStuffData)item.data);
            if (!data.Data.ContainsKey("OUTFIT_NAME")) // this isn't any mannequin!
                return;

            data.Data["OUTFIT_NAME"] = newName;

            // Send update to room
            ServerMessage Message = new ServerMessage(Outgoing.ObjectUpdate);
            item.Serialize(Message, Session.GetHabbo().CurrentRoom.OwnerId);
            Session.GetHabbo().CurrentRoom.SendMessage(Message);

            // Add to MySQL save queue
            Session.GetHabbo().CurrentRoom.GetRoomItemHandler().UpdateItem(item);
        }

        internal void MannequeFigureChange()
        {
            if (Session.GetHabbo().CurrentRoom == null || !Session.GetHabbo().CurrentRoom.CheckRights(Session, true))
                return;

            int itemID = Request.ReadInt32();

            RoomItem item = Session.GetHabbo().CurrentRoom.GetRoomItemHandler().GetItem((uint)itemID);
            if (item == null || item.data.GetTypeID() != 1)
                return;

            MapStuffData data = ((MapStuffData)item.data);
            if (!data.Data.ContainsKey("OUTFIT_NAME")) // this isn't any mannequin!
                return;

            // We gotta remove all headgear!
            string filteredLook = "";
            string[] sp = Session.GetHabbo().Look.Split('.');
            foreach (string s in sp)
            {
                if(!(s.StartsWith("hd") || s.StartsWith("ha") || s.StartsWith("he") || s.StartsWith("fa") || s.StartsWith("ea") || s.StartsWith("hr")))
                {
                    filteredLook += "." + s;
                }
            }
            filteredLook = filteredLook.Substring(1);

            data.Data["FIGURE"] = filteredLook;
            data.Data["GENDER"] = Session.GetHabbo().Gender;

            // Send update to room
            ServerMessage Message = new ServerMessage(Outgoing.ObjectUpdate);
            item.Serialize(Message, Session.GetHabbo().CurrentRoom.OwnerId);
            Session.GetHabbo().CurrentRoom.SendMessage(Message);

            // Add to MySQL save queue
            Session.GetHabbo().CurrentRoom.GetRoomItemHandler().UpdateItem(item);
        }

        internal void SetAdParameters()
        {
            if (Session.GetHabbo().CurrentRoom == null || !Session.GetHabbo().CurrentRoom.CheckRights(Session, true))
                return;

            int itemID = Request.ReadInt32();

            RoomItem item = Session.GetHabbo().CurrentRoom.GetRoomItemHandler().GetItem((uint)itemID);
            if (item == null)
                return;

            List<string> allowedParameters = new List<string>(new string[] { "imageUrl", "clickUrl", "offsetX", "offsetY", "offsetZ" });
            MapStuffData data = new MapStuffData();
            int mapLength = Request.ReadInt32();
            for (int i = 0; i < mapLength/2; i++)
            {
                string key = Request.ReadString();
                string value = Request.ReadString();

                if (!allowedParameters.Contains(key))
                {
                    Session.SendMOTD("Invalid parameter(s)!");
                    return;
                }
                data.Data.Add(key, value);
            }
            item.data = data;

            // Send update to room
            ServerMessage Message = new ServerMessage(Outgoing.ObjectUpdate);
            item.Serialize(Message, Session.GetHabbo().CurrentRoom.OwnerId);
            Session.GetHabbo().CurrentRoom.SendMessage(Message);

            // Add to MySQL save queue
            Session.GetHabbo().CurrentRoom.GetRoomItemHandler().UpdateItem(item);
        }

        //internal void SaveWiredWithFurniture()
        //{
        //    uint itemID = Request.PopWiredUInt();
        //    WiredSaver.HandleSave(itemID, Session.GetHabbo().CurrentRoom, Request);
        //}

        //internal void RegisterRooms()
        //{
        //    RequestHandlers.Add(182, new RequestHandler(GetAdvertisement));
        //    RequestHandlers.Add(388, new RequestHandler(GetPub));
        //    RequestHandlers.Add(2, new RequestHandler(OpenPub));
        //    RequestHandlers.Add(230, new RequestHandler(GetGroupBadges));
        //    RequestHandlers.Add(215, new RequestHandler(GetRoomData1));
        //    RequestHandlers.Add(390, new RequestHandler(GetRoomData2));
        //    RequestHandlers.Add(126, new RequestHandler(GetRoomData3));
        //    RequestHandlers.Add(52, new RequestHandler(Talk));
        //    RequestHandlers.Add(55, new RequestHandler(Shout));
        //    RequestHandlers.Add(56, new RequestHandler(Whisper));
        //    RequestHandlers.Add(75, new RequestHandler(Move));
        //    RequestHandlers.Add(387, new RequestHandler(CanCreateRoom));
        //    RequestHandlers.Add(29, new RequestHandler(CreateRoom));
        //    RequestHandlers.Add(400, new RequestHandler(GetRoomEditData));
        //    RequestHandlers.Add(386, new RequestHandler(SaveRoomIcon));
        //    RequestHandlers.Add(401, new RequestHandler(SaveRoomData));
        //    RequestHandlers.Add(96, new RequestHandler(GiveRights));
        //    RequestHandlers.Add(97, new RequestHandler(TakeRights));
        //    RequestHandlers.Add(155, new RequestHandler(TakeAllRights));
        //    RequestHandlers.Add(95, new RequestHandler(KickUser));
        //    RequestHandlers.Add(320, new RequestHandler(BanUser));
        //    RequestHandlers.Add(71, new RequestHandler(InitTrade));
        //    RequestHandlers.Add(384, new RequestHandler(SetHomeRoom));
        //    RequestHandlers.Add(23, new RequestHandler(DeleteRoom));
        //    RequestHandlers.Add(79, new RequestHandler(LookAt));
        //    RequestHandlers.Add(317, new RequestHandler(StartTyping));
        //    RequestHandlers.Add(318, new RequestHandler(StopTyping));
        //    RequestHandlers.Add(319, new RequestHandler(IgnoreUser));
        //    RequestHandlers.Add(322, new RequestHandler(UnignoreUser));
        //    RequestHandlers.Add(345, new RequestHandler(CanCreateRoomEvent));
        //    RequestHandlers.Add(346, new RequestHandler(StartEvent));
        //    RequestHandlers.Add(347, new RequestHandler(StopEvent));
        //    RequestHandlers.Add(348, new RequestHandler(EditEvent));
        //    RequestHandlers.Add(94, new RequestHandler(Wave));
        //    RequestHandlers.Add(263, new RequestHandler(GetUserTags));
        //    RequestHandlers.Add(159, new RequestHandler(GetUserBadges));
        //    RequestHandlers.Add(261, new RequestHandler(RateRoom));
        //    RequestHandlers.Add(93, new RequestHandler(Dance));
        //    RequestHandlers.Add(98, new RequestHandler(AnswerDoorbell));
        //    RequestHandlers.Add(59, new RequestHandler(ReqLoadRoomForUser));
        //    RequestHandlers.Add(66, new RequestHandler(ApplyRoomEffect));
        //    RequestHandlers.Add(90, new RequestHandler(PlaceItem));
        //    RequestHandlers.Add(67, new RequestHandler(TakeItem));
        //    RequestHandlers.Add(73, new RequestHandler(MoveItem));
        //    RequestHandlers.Add(91, new RequestHandler(MoveWallItem));
        //    RequestHandlers.Add(392, new RequestHandler(TriggerItem)); // Generic trigger item
        //    RequestHandlers.Add(393, new RequestHandler(TriggerItem)); // Generic trigger item
        //    RequestHandlers.Add(83, new RequestHandler(OpenPostit));
        //    RequestHandlers.Add(84, new RequestHandler(SavePostit));
        //    RequestHandlers.Add(85, new RequestHandler(DeletePostit));
        //    RequestHandlers.Add(78, new RequestHandler(OpenPresent));
        //    RequestHandlers.Add(341, new RequestHandler(GetMoodlight));
        //    RequestHandlers.Add(342, new RequestHandler(UpdateMoodlight));
        //    RequestHandlers.Add(343, new RequestHandler(SwitchMoodlightStatus));
        //    RequestHandlers.Add(72, new RequestHandler(OfferTradeItem));
        //    RequestHandlers.Add(405, new RequestHandler(TakeBackTradeItem));
        //    RequestHandlers.Add(70, new RequestHandler(StopTrade));
        //    RequestHandlers.Add(403, new RequestHandler(StopTrade));
        //    RequestHandlers.Add(69, new RequestHandler(AcceptTrade));
        //    RequestHandlers.Add(68, new RequestHandler(UnacceptTrade));
        //    RequestHandlers.Add(402, new RequestHandler(CompleteTrade));
        //    RequestHandlers.Add(371, new RequestHandler(GiveRespect));
        //    RequestHandlers.Add(372, new RequestHandler(ApplyEffect));
        //    RequestHandlers.Add(373, new RequestHandler(EnableEffect));
        //    //RequestHandlers.Add(3004, new RequestHandler(GetTrainerPanel)); DARIO! :@@@@
        //    RequestHandlers.Add(232, new RequestHandler(TriggerItem)); // One way gates
        //    RequestHandlers.Add(314, new RequestHandler(TriggerItem)); // Love Shuffler
        //    RequestHandlers.Add(247, new RequestHandler(TriggerItem)); // Habbo Wheel
        //    RequestHandlers.Add(76, new RequestHandler(TriggerItem)); // Dice
        //    RequestHandlers.Add(77, new RequestHandler(TriggerItemDiceSpecial)); // Dice (special)
        //    RequestHandlers.Add(414, new RequestHandler(RecycleItems));
        //    RequestHandlers.Add(183, new RequestHandler(RedeemExchangeFurni));
        //    RequestHandlers.Add(113, new RequestHandler(EnterInfobus));
        //    RequestHandlers.Add(441, new RequestHandler(KickBot));
        //    RequestHandlers.Add(3002, new RequestHandler(PlacePet));
        //    RequestHandlers.Add(3001, new RequestHandler(GetPetInfo));
        //    RequestHandlers.Add(3003, new RequestHandler(PickUpPet));
        //    RequestHandlers.Add(3004, new RequestHandler(CommandsPet));
        //    RequestHandlers.Add(3005, new RequestHandler(RespectPet));
        //    RequestHandlers.Add(3254, new RequestHandler(PlacePostIt));
        //    //RequestHandlers.Add(3007, new RequestHandler(PetRaces));
        //    RequestHandlers.Add(480, new RequestHandler(SetLookTransfer));
            
        //    RequestHandlers.Add(3051, new RequestHandler(SaveWired));
        //    RequestHandlers.Add(3050, new RequestHandler(SaveWiredWithFurniture));
        //}

        //internal void UnregisterRoom()
        //{
        //    RequestHandlers.Remove(182);
        //    RequestHandlers.Remove(388);
        //    RequestHandlers.Remove(2);
        //    RequestHandlers.Remove(230);
        //    RequestHandlers.Remove(215);
        //    RequestHandlers.Remove(390);
        //    RequestHandlers.Remove(126);
        //    RequestHandlers.Remove(52);
        //    RequestHandlers.Remove(55);
        //    RequestHandlers.Remove(56);
        //    RequestHandlers.Remove(75);
        //    RequestHandlers.Remove(387);
        //    RequestHandlers.Remove(29);
        //    RequestHandlers.Remove(400);
        //    RequestHandlers.Remove(386);
        //    RequestHandlers.Remove(401);
        //    RequestHandlers.Remove(96);
        //    RequestHandlers.Remove(97);
        //    RequestHandlers.Remove(155);
        //    RequestHandlers.Remove(95);
        //    RequestHandlers.Remove(320);
        //    RequestHandlers.Remove(71);
        //    RequestHandlers.Remove(384);
        //    RequestHandlers.Remove(23);
        //    RequestHandlers.Remove(79);
        //    RequestHandlers.Remove(317);
        //    RequestHandlers.Remove(318);
        //    RequestHandlers.Remove(319);
        //    RequestHandlers.Remove(322);
        //    RequestHandlers.Remove(345);
        //    RequestHandlers.Remove(346);
        //    RequestHandlers.Remove(347);
        //    RequestHandlers.Remove(348);
        //    RequestHandlers.Remove(94);
        //    RequestHandlers.Remove(263);
        //    RequestHandlers.Remove(159);
        //    RequestHandlers.Remove(261);
        //    RequestHandlers.Remove(93);
        //    RequestHandlers.Remove(98);
        //    RequestHandlers.Remove(59);
        //    RequestHandlers.Remove(66);
        //    RequestHandlers.Remove(90);
        //    RequestHandlers.Remove(67);
        //    RequestHandlers.Remove(73);
        //    RequestHandlers.Remove(392);
        //    RequestHandlers.Remove(393);
        //    RequestHandlers.Remove(83);
        //    RequestHandlers.Remove(84);
        //    RequestHandlers.Remove(85);
        //    RequestHandlers.Remove(78);
        //    RequestHandlers.Remove(341);
        //    RequestHandlers.Remove(342);
        //    RequestHandlers.Remove(343);
        //    RequestHandlers.Remove(72);
        //    RequestHandlers.Remove(405);
        //    RequestHandlers.Remove(70);
        //    RequestHandlers.Remove(403);
        //    RequestHandlers.Remove(69);
        //    RequestHandlers.Remove(68);
        //    RequestHandlers.Remove(402);
        //    RequestHandlers.Remove(371);
        //    RequestHandlers.Remove(372);
        //    RequestHandlers.Remove(373);
        //    RequestHandlers.Remove(232);
        //    RequestHandlers.Remove(314);
        //    RequestHandlers.Remove(247);
        //    RequestHandlers.Remove(76);
        //    RequestHandlers.Remove(77);
        //    RequestHandlers.Remove(414);
        //    RequestHandlers.Remove(183);
        //    RequestHandlers.Remove(113);
        //    RequestHandlers.Remove(441);
        //    RequestHandlers.Remove(3002);
        //    RequestHandlers.Remove(3001);
        //    RequestHandlers.Remove(3003);
        //    RequestHandlers.Remove(3005);
        //    RequestHandlers.Remove(480);
        //    RequestHandlers.Remove(3051);
        //}
    }
}
