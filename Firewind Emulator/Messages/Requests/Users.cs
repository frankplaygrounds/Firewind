using System;
using System.Collections.Generic;
using System.Data;
using Firewind.HabboHotel.Misc;
using Firewind.HabboHotel.Groups.Types;
using Firewind.HabboHotel.Rooms;
using Firewind.HabboHotel.Users;
using Firewind.HabboHotel.Users.Badges;
using Database_Manager.Database.Session_Details.Interfaces;
using HabboEvents;
using Firewind.Core;

namespace Firewind.Messages
{
    partial class GameClientMessageHandler
    {
        internal void GetUserInfo()
        {
            GetResponse().Init(Outgoing.HabboInfomation);
            GetResponse().AppendUInt(Session.GetHabbo().Id);
            GetResponse().AppendString(Session.GetHabbo().Username);
            GetResponse().AppendString(Session.GetHabbo().Look);
            GetResponse().AppendString(Session.GetHabbo().Gender.ToUpper());
            GetResponse().AppendString(Session.GetHabbo().Motto);
            GetResponse().AppendString(Session.GetHabbo().RealName);
            GetResponse().AppendBoolean(false);
            GetResponse().AppendInt32(Session.GetHabbo().Respect);
            GetResponse().AppendInt32(Session.GetHabbo().DailyRespectPoints); // respect to give away
            GetResponse().AppendInt32(Session.GetHabbo().DailyPetRespectPoints);
            GetResponse().AppendBoolean(true);
            GetResponse().AppendString(Session.GetHabbo().LastOnline); // lul
            GetResponse().AppendBoolean(false);
            GetResponse().AppendBoolean(false);
            SendResponse();

            Boolean SafeChat = true;
            Boolean IsGuide = false;
            Boolean VoteInCompetitions = false;

            GetResponse().Init(Outgoing.Allowances);
            GetResponse().AppendInt32(3); // count
            GetResponse().AppendString("SAFE_CHAT");
            GetResponse().AppendBoolean(SafeChat);
            GetResponse().AppendString((!SafeChat) ? "requirement.unfulfilled.safety_quiz_1" : "");
            GetResponse().AppendString("USE_GUIDE_TOOL");
            GetResponse().AppendBoolean(IsGuide);
            GetResponse().AppendString((!IsGuide) ? "requirement.unfulfilled.helper_level_4" : "");
            GetResponse().AppendString("VOTE_IN_COMPETITIONS");
            GetResponse().AppendBoolean(VoteInCompetitions);
            GetResponse().AppendString((!VoteInCompetitions) ? "requirement.unfulfilled.helper_level_2" : "");
            SendResponse();

            GetResponse().Init(Outgoing.AchievementPoints);
            GetResponse().AppendInt32(Session.GetHabbo().AchievementPoints);
            SendResponse();

            // Welcome back message, TODO: make it optional?
            Response.Init(Outgoing.WelcomeBack);
            Response.AppendInt32(0);
            Response.AppendInt32(0);
            Response.AppendInt32(0); // prizes count
            SendResponse();

            InitMessenger();
        }

        internal void GetCreditsInfo()
        {
            if (FirewindEnvironment.GetGame().GetClientManager().pixelsOnLogin > 0)
                PixelManager.GivePixels(Session, FirewindEnvironment.GetGame().GetClientManager().pixelsOnLogin);
            else
                Session.GetHabbo().UpdateActivityPointsBalance(false);

            if (FirewindEnvironment.GetGame().GetClientManager().creditsOnLogin > 0)
                Session.GetHabbo().Credits += FirewindEnvironment.GetGame().GetClientManager().creditsOnLogin;

            Session.GetHabbo().UpdateCreditsBalance();
        }

        internal void ScrGetUserInfo()
        {
            GetResponse().Init(Outgoing.SerializeClub);
            GetResponse().AppendString("club_habbo");

            if (Session.GetHabbo().GetSubscriptionManager().HasSubscription("habbo_vip"))
            {
                Double Expire = Session.GetHabbo().GetSubscriptionManager().GetSubscription("habbo_vip").ExpireTime;
                Double TimeLeft = Expire - FirewindEnvironment.GetUnixTimestamp();
                int TotalDaysLeft = (int)Math.Ceiling(TimeLeft / 86400);
                /*Double Initialized = Session.GetHabbo().GetSubscriptionManager().GetSubscription("habbo_vip").ini;
                Double TimeLeft = Expire - FirewindEnvironment.GetUnixTimestamp();
                int TotalDaysLeft = (int)Math.Ceiling(TimeLeft / 86400);*/
                int MonthsLeft = TotalDaysLeft / 31;

                if (MonthsLeft >= 1) MonthsLeft--;

                GetResponse().AppendInt32(TotalDaysLeft - (MonthsLeft * 31)); // days left
                GetResponse().AppendInt32(2); // days multiplier
                GetResponse().AppendInt32(MonthsLeft); // months left
                GetResponse().AppendInt32(1); // ???
                GetResponse().AppendBoolean(true); // HC PRIVILEGE
                GetResponse().AppendBoolean(true); // VIP PRIVILEGE
                GetResponse().AppendInt32(0); // days i have on hc
                GetResponse().AppendInt32(0); // days i've purchased
                GetResponse().AppendInt32(495); // value 4 groups
            }
            else
            {
                GetResponse().AppendInt32(0);
                GetResponse().AppendInt32(0); // ??
                GetResponse().AppendInt32(0);
                GetResponse().AppendInt32(0); // type
                GetResponse().AppendBoolean(false);
                GetResponse().AppendBoolean(true);
                GetResponse().AppendInt32(0);
                GetResponse().AppendInt32(0); // days i have on hc
                GetResponse().AppendInt32(0); // days i have on vip
            }

            SendResponse();
        }

        internal void GetBadges()
        {
            Session.SendMessage(Session.GetHabbo().GetBadgeComponent().Serialize());
        }

        internal void UpdateBadges()
        {
            Session.GetHabbo().GetBadgeComponent().ResetSlots();

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.runFastQuery("UPDATE user_badges SET badge_slot = 0 WHERE user_id = " + Session.GetHabbo().Id);
            }

            for(int i = 0; i < 5; i++)
            {
                int Slot = Request.ReadInt32();
                string Badge = Request.ReadString();

                if(Badge.Length == 0)
                    continue;

                if (!Session.GetHabbo().GetBadgeComponent().HasBadge(Badge) || Slot < 1 || Slot > 5)
                    return;
                
                Session.GetHabbo().GetBadgeComponent().GetBadge(Badge).Slot = Slot;

                using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                {
                    dbClient.setQuery("UPDATE user_badges SET badge_slot = " + Slot + " WHERE badge_id = @badge AND user_id = " + Session.GetHabbo().Id + "");
                    dbClient.addParameter("badge", Badge);
                    dbClient.runQuery();
                }
            }

            FirewindEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Session, HabboHotel.Quests.QuestType.PROFILE_BADGE);

            ServerMessage Message = new ServerMessage(Outgoing.UpdateBadges);
            Message.AppendUInt(Session.GetHabbo().Id);
            Message.AppendInt32(Session.GetHabbo().GetBadgeComponent().EquippedCount);

            foreach (Badge Badge in Session.GetHabbo().GetBadgeComponent().BadgeList.Values)
            {
                if (Badge.Slot <= 0)
                {
                    continue;
                }

                Message.AppendInt32(Badge.Slot);
                Message.AppendString(Badge.Code);
            }

            if (Session.GetHabbo().InRoom && FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId) != null)
            {
                FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId).SendMessage(Message);
            }
            else
            {
                Session.SendMessage(Message);
            }
        }

        internal void GetAchievements()
        {
            FirewindEnvironment.GetGame().GetAchievementManager().GetList(Session, Request);
        }

        internal void PrepareCampaing()
        {
            String campaingbadge = Request.ReadString();

            Response.Init(Outgoing.PrepareCampaing);
            Response.AppendString(campaingbadge); // tha badge
            Response.AppendBoolean(false); // received
            SendResponse();
        }

        internal void SendCampaingData()
        {
            try
            {
                // 2012-07-09 00:00,africaDesertFurniPromo;2012-07-16 00:00,africaSavannahFurniPromo;2012-07-23 00:00,africaJungleFurniPromo[0]africaDesertFurniPromo
                //String promo = Request.PopFixedString();
                String promo = "";
                //Logging.WriteLine(promo);
                string finalpromo = "africaSavannahFurniPromo";
                /*
                string[] possiblepromos = promo.Split(';');
                DateTime current = DateTime.Now;
                foreach (string s in possiblepromos)
                {
                    if (s == "")
                        break;
                    string[] s1 = s.Split(',');
                    String hour = s1[0];
                    if (hour == "")
                        break;
                    string[] hours = hour.Split(' ')[0].Split('-');
                    string promo2 = s1[1];
                    if (promo2 == "")
                        break;
                    int Year = int.Parse(hours[0]);
                    int Month = int.Parse(hours[1]);
                    int Day = int.Parse(hours[2]);
                    if (Year >= current.Year)
                    {
                        if (Month >= current.Month)
                        {
                            if (Day >= current.Day)
                                finalpromo = promo2;
                        }
                    }
                }*/

                Response.Init(Outgoing.SendCampaingData);
                Response.AppendString(promo);
                Response.AppendString(finalpromo);
                SendResponse();

                
            }
            catch (Exception e)
            {
                //Logging.WriteLine("Weird campaing not serialized!");
            }
        }

        internal void LoadProfile()
        {
            uint userID = Request.ReadUInt32();
            bool unused = Request.ReadBoolean(); // Always true

            Habbo Data;
            Data = userID == Session.GetHabbo().Id ? Session.GetHabbo() : FirewindEnvironment.getHabboForId(userID);
            if (Data == null)
                return;

            // Get the info we need
            bool isOnline = FirewindEnvironment.GetGame().GetClientManager().GetClientByUserID(userID) != null;

            Response.Init(Outgoing.ProfileInformation);

            Response.AppendInt32((int)Data.Id);
            Response.AppendString(Data.Username);
            Response.AppendString(Data.Look);
            Response.AppendString(Data.Motto);
            Response.AppendString(Data.AccountCreated); // created
            Response.AppendInt32(Data.AchievementPoints); // Achievement Points
            Response.AppendInt32(Data.GetMessenger().myFriends); //friends

            Response.AppendBoolean(userID != Session.GetHabbo().Id && Data.GetMessenger().FriendshipExists(Session.GetHabbo().Id)); // is friend
            Response.AppendBoolean(Data.GetMessenger().requests.ContainsKey(Session.GetHabbo().Id)); // firend request sent
            Response.AppendBoolean(isOnline); // is online

            List<Group> groups = FirewindEnvironment.GetGame().GetGroupManager() != null ?
                FirewindEnvironment.GetGame().GetGroupManager().GetGroups(Data.Groups) : new List<Group>();
            Response.AppendInt32(groups.Count); // group count
            foreach (Group group in groups)
            {
                Response.AppendInt32(group.ID);
                Response.AppendString(group.Name);
                Response.AppendString(group.BadgeCode);
                Response.AppendString(group.Color1);
                Response.AppendString(group.Color2);
                Response.AppendBoolean(group.ID == Data.FavouriteGroup);
            }

            Response.AppendInt32(0); // last online in seconds
            Response.AppendBoolean(true); // show it

            SendResponse();
        }


        internal void ChangeLook()
        {
            if (Session.GetHabbo().MutantPenalty)
            {
                Session.SendNotif("Because of a penalty or restriction on your account, you are not allowed to change your look.");
                return;
            }

            string Gender = Request.ReadString().ToUpper();
            string Look = FirewindEnvironment.FilterInjectionChars(Request.ReadString());

            //if (!AntiMutant.ValidateLook(Look, Gender))
            //{
            //    return;
            //}

            FirewindEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Session, HabboHotel.Quests.QuestType.PROFILE_CHANGE_LOOK);

            Session.GetHabbo().Look = FirewindEnvironment.FilterFigure(Look);
            Session.GetHabbo().Gender = Gender.ToLower();

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("UPDATE users SET look = @look, gender = @gender WHERE id = " + Session.GetHabbo().Id);
                dbClient.addParameter("look", Look);
                dbClient.addParameter("gender", Gender);
                dbClient.runQuery();
            }

            FirewindEnvironment.GetGame().GetAchievementManager().ProgressUserAchievement(Session, "ACH_AvatarLooks", 1);

            Session.GetMessageHandler().GetResponse().Init(Outgoing.UpdateUserInformation);
            Session.GetMessageHandler().GetResponse().AppendInt32(-1);
            Session.GetMessageHandler().GetResponse().AppendString(Session.GetHabbo().Look);
            Session.GetMessageHandler().GetResponse().AppendString(Session.GetHabbo().Gender.ToLower());
            Session.GetMessageHandler().GetResponse().AppendString(Session.GetHabbo().Motto);
            Session.GetMessageHandler().GetResponse().AppendInt32(Session.GetHabbo().AchievementPoints);
            Session.GetMessageHandler().SendResponse();

            if (Session.GetHabbo().InRoom)
            {
                Room Room = Session.GetHabbo().CurrentRoom;

                if (Room == null)
                {
                    return;
                }

                RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

                if (User == null)
                {
                    return;
                }

                ServerMessage RoomUpdate = new ServerMessage(Outgoing.UpdateUserInformation);
                RoomUpdate.AppendInt32(User.VirtualId);
                RoomUpdate.AppendString(Session.GetHabbo().Look);
                RoomUpdate.AppendString(Session.GetHabbo().Gender.ToLower());
                RoomUpdate.AppendString(Session.GetHabbo().Motto);
                RoomUpdate.AppendInt32(Session.GetHabbo().AchievementPoints);
                Room.SendMessage(RoomUpdate);
            }
        }

        internal void ChangeMotto()
        {
            string Motto = FirewindEnvironment.FilterInjectionChars(Request.ReadString());

            if (Motto.Length == 0 || Motto == Session.GetHabbo().Motto) // Prevents spam?
            {
                return;
            }

            //if (Motto.Length < 0)
            //{
            //    return; // trying to fk the client :D
            //} Congratulations. The string length can not hold calue < 0. Stupid -_-"

            Session.GetHabbo().Motto = Motto;


            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("UPDATE users SET motto = @motto WHERE id = '" + Session.GetHabbo().Id + "'");
                dbClient.addParameter("motto", Motto);
                dbClient.runQuery();
            }

            FirewindEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Session, HabboHotel.Quests.QuestType.PROFILE_CHANGE_MOTTO);

            if (Session.GetHabbo().InRoom)
            {
                Room Room = Session.GetHabbo().CurrentRoom;

                if (Room == null)
                {
                    return;
                }

                RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

                if (User == null)
                {
                    return;
                }

                ServerMessage RoomUpdate = new ServerMessage(Outgoing.UpdateUserInformation);
                RoomUpdate.AppendInt32(User.VirtualId);
                RoomUpdate.AppendString(Session.GetHabbo().Look);
                RoomUpdate.AppendString(Session.GetHabbo().Gender.ToLower());
                RoomUpdate.AppendString(Session.GetHabbo().Motto);
                RoomUpdate.AppendInt32(Session.GetHabbo().AchievementPoints);
                Room.SendMessage(RoomUpdate);
            }

            FirewindEnvironment.GetGame().GetAchievementManager().ProgressUserAchievement(Session, "ACH_Motto", 1);
        }

        internal void GetWardrobe()
        {
            GetResponse().Init(Outgoing.WardrobeData);
            GetResponse().AppendInt32(Session.GetHabbo().GetSubscriptionManager().HasSubscription("habbo_vip") ? 1 : 0);

            if (Session.GetHabbo().GetSubscriptionManager().HasSubscription("habbo_vip"))
            {
                using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                {
                    //dbClient.addParameter("userid", Session.GetHabbo().Id);
                    dbClient.setQuery("SELECT slot_id, look, gender FROM user_wardrobe WHERE user_id = " + Session.GetHabbo().Id);
                    DataTable WardrobeData = dbClient.getTable();

                    if (WardrobeData == null)
                    {
                        GetResponse().AppendInt32(0);
                    }
                    else
                    {
                        GetResponse().AppendInt32(WardrobeData.Rows.Count);

                        foreach (DataRow Row in WardrobeData.Rows)
                        {
                            GetResponse().AppendUInt(Convert.ToUInt32(Row["slot_id"]));
                            GetResponse().AppendString((string)Row["look"]);
                            GetResponse().AppendString((string)Row["gender"].ToString().ToUpper());
                        }
                    }
                }

                SendResponse();
            }
        }

        internal void SaveWardrobe()
        {
            uint SlotId = Request.ReadUInt32();

            string Look = Request.ReadString();
            string Gender = Request.ReadString();

            //if (!AntiMutant.ValidateLook(Look, Gender))
            //{
            //    return;
            //}

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("SELECT null FROM user_wardrobe WHERE user_id = " + Session.GetHabbo().Id + " AND slot_id = " + SlotId + "");
                dbClient.addParameter("look", Look);
                dbClient.addParameter("gender", Gender.ToUpper());

                if (dbClient.getRow() != null)
                {
                    dbClient.setQuery("UPDATE user_wardrobe SET look = @look, gender = @gender WHERE user_id = " + Session.GetHabbo().Id + " AND slot_id = " + SlotId + ";");
                    dbClient.addParameter("look", Look);
                    dbClient.addParameter("gender", Gender.ToUpper());
                    dbClient.runQuery();
                }
                else
                {
                    dbClient.setQuery("INSERT INTO user_wardrobe (user_id,slot_id,look,gender) VALUES (" + Session.GetHabbo().Id + "," + SlotId + ",@look,@gender)");
                    dbClient.addParameter("look", Look);
                    dbClient.addParameter("gender", Gender.ToUpper());
                    dbClient.runQuery();
                }
            }
        }

        internal void GetPetsInventory()
        {
            if (Session.GetHabbo().GetInventoryComponent() == null)
            {
                return;
            }

            Session.SendMessage(Session.GetHabbo().GetInventoryComponent().SerializePetInventory());
        }

        internal void GetSoundSettings()
        {
            Response.Init(Outgoing.SoundSettings);
            Response.AppendInt32(100); // TODO: dynamic soud settings
            SendResponse();
        }

        //internal void RegisterUsers()
        //{
        //    RequestHandlers.Add(7, new RequestHandler(GetUserInfo));
        //    RequestHandlers.Add(8, new RequestHandler(GetBalance));
        //    RequestHandlers.Add(26, new RequestHandler(GetSubscriptionData));

        //    RequestHandlers.Add(157, new RequestHandler(GetBadges));
        //    RequestHandlers.Add(158, new RequestHandler(UpdateBadges));
        //    RequestHandlers.Add(370, new RequestHandler(GetAchievements));

        //    RequestHandlers.Add(44, new RequestHandler(ChangeLook));
        //    RequestHandlers.Add(484, new RequestHandler(ChangeMotto));
        //    RequestHandlers.Add(375, new RequestHandler(GetWardrobe));
        //    RequestHandlers.Add(376, new RequestHandler(SaveWardrobe));

        //    RequestHandlers.Add(404, new RequestHandler(GetInventory));
        //    RequestHandlers.Add(3000, new RequestHandler(GetPetsInventory));

        //}

        //internal void UnregisterUser()
        //{
        //    RequestHandlers.Remove(7);
        //    RequestHandlers.Remove(8);
        //    RequestHandlers.Remove(26);
        //    RequestHandlers.Remove(157);
        //    RequestHandlers.Remove(158);
        //    RequestHandlers.Remove(370);
        //    RequestHandlers.Remove(44);
        //    RequestHandlers.Remove(375);
        //    RequestHandlers.Remove(376);
        //    RequestHandlers.Remove(404);
        //    RequestHandlers.Remove(3000);
        //}
    }
}
