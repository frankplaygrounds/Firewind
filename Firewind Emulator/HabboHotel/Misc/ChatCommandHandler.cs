using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Firewind.Core;
using Firewind.HabboHotel.GameClients;
using Firewind.HabboHotel.Items;
using Firewind.HabboHotel.Rooms;
using Firewind.HabboHotel.Rooms.RoomIvokedItems;
using Firewind.HabboHotel.Users;
using Firewind.HabboHotel.Users.Competitions;
using Firewind.Messages;
using Database_Manager.Database.Session_Details.Interfaces;
using System.Drawing;
using Firewind.Util;
using HabboEvents;
using Firewind.Collections;
using System.Threading;

namespace Firewind.HabboHotel.Misc
{
    class ChatCommandHandler
    {
        private GameClient Session;
        private string[] Params;

        public ChatCommandHandler(string[] input, GameClient session)
        {
            Params = input;
            Session = session;
        }

        internal bool WasExecuted()
        {
            ChatCommand command = ChatCommandRegister.GetCommand(Params[0].Substring(1).ToLower());

            if (command.UserGotAuthorization(Session))
            {
                ChatCommandRegister.InvokeCommand(this, command.commandID);
                Dispose();
                return true;
            }
            else
            {
                Dispose();
                return false;
            }
        }

        internal void Dispose()
        {
            Session = null;
            Array.Clear(this.Params, 0, Params.Length);
        }

        #region Commands
        internal void addtag()
        {
            string tag;
            if (!TryGetTagParameter(out tag))
                return;

            if (Session.GetHabbo().Tags.Exists(existingTag => existingTag.Equals(tag, StringComparison.OrdinalIgnoreCase)))
            {
                Session.SendNotif("You already have that tag.");
                return;
            }

            if (Session.GetHabbo().Tags.Count >= 5)
            {
                Session.SendNotif("You can only have 5 tags.");
                return;
            }

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("INSERT INTO user_tags(user_id,tag) VALUES(@userid,@tag)");
                dbClient.addParameter("userid", Session.GetHabbo().Id);
                dbClient.addParameter("tag", tag);
                dbClient.runQuery();
            }

            Session.GetHabbo().Tags.Add(tag);
            SendTagUpdate();
            Session.SendNotif("Tag added.");
        }

        internal void deltag()
        {
            string tag;
            if (!TryGetTagParameter(out tag))
                return;

            int removed = Session.GetHabbo().Tags.RemoveAll(existingTag => existingTag.Equals(tag, StringComparison.OrdinalIgnoreCase));
            if (removed == 0)
            {
                Session.SendNotif("You do not have that tag.");
                return;
            }

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("DELETE FROM user_tags WHERE user_id = @userid AND tag = @tag");
                dbClient.addParameter("userid", Session.GetHabbo().Id);
                dbClient.addParameter("tag", tag);
                dbClient.runQuery();
            }

            SendTagUpdate();
            Session.SendNotif("Tag removed.");
        }

        private bool TryGetTagParameter(out string tag)
        {
            tag = string.Empty;
            if (Params.Length != 2)
            {
                Session.SendNotif("Usage: :addtag tag or :deltag tag");
                return false;
            }

            tag = Params[1].Trim().ToLower();
            if (tag.Length == 0 || tag.Length > 50 || !FirewindEnvironment.IsValidAlphaNumeric(tag))
            {
                Session.SendNotif("Tags can only contain letters and numbers.");
                return false;
            }

            return true;
        }

        private void SendTagUpdate()
        {
            ServerMessage message = new ServerMessage(Outgoing.GetUserTags);
            message.AppendUInt(Session.GetHabbo().Id);
            message.AppendInt32(Session.GetHabbo().Tags.Count);
            foreach (string tag in Session.GetHabbo().Tags)
                message.AppendStringWithBreak(tag);

            Room room = Session.GetHabbo().CurrentRoom;
            if (room != null)
                room.SendMessage(message);
            else
                Session.SendMessage(message);
        }

        internal void moonwalk()
        {
            Room room = Session.GetHabbo().CurrentRoom;
            if (room == null)
                return;

            RoomUser roomuser = room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            roomuser.moonwalkEnabled = !roomuser.moonwalkEnabled;

            if (roomuser.moonwalkEnabled)
                Session.SendNotif("Moonwalk enabled");
            else
                Session.SendNotif("Moonwalk disabled");
        }

        internal void startquestion()
        {
            try
            {
                Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
                DataTable Data = null;
                int QuestionId = int.Parse(Params[1]);
                Room.CurrentPollId = QuestionId;
                string Question;


                using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                {
                    dbClient.setQuery("SELECT question FROM infobus_questions WHERE id = '" + QuestionId + "' LIMIT 1");
                    Question = dbClient.getString();
                }


                using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                {

                    dbClient.setQuery("SELECT * FROM infobus_answers WHERE question_id = '" + QuestionId + "'");
                    Data = dbClient.getTable();

                }

                ServerMessage InfobusQuestion = new ServerMessage(2600);
                InfobusQuestion.AppendString(Question);
                InfobusQuestion.AppendInt32(Data.Rows.Count);
                if (Data != null)
                {
                    foreach (DataRow Row in Data.Rows)
                    {
                        InfobusQuestion.AppendInt32((int)Row["id"]);
                        InfobusQuestion.AppendString((string)Row["answer_text"]);
                    }
                }
                Room.SendMessage(InfobusQuestion);



                Thread Infobus = new Thread(delegate () { Room.ShowResults(Room, QuestionId, Session); });
                Infobus.Start();
            }
            catch
            {
                Session.SendNotif("Wrong syntax.");
                //Room.HasThread.Add((uint)QuestionId, Infobus);
            }

        }

        internal void givescore()
        {
            if (Params.Length < 3 || Params.Length > 3)
                return;

            int UserId = 0;
            if (int.TryParse(Params[1], out UserId))
            {
                Habbo data = FirewindEnvironment.getHabboForId((uint)UserId);
                if (data != null)
                {
                    int Score = int.Parse(Params[2]);
                    Ranking.AddScoreToUserId(Score, (uint)data.Id, RankingType.COMPETITIONS);
                }
                else
                {
                    Session.SendNotif("El usuario al que quieres darle puntos no existe!");
                }
            }
            else
            {
                Habbo data = FirewindEnvironment.getHabboForName(Params[1]);
                if (data != null)
                {
                    int Score = int.Parse(Params[2]);
                    Ranking.AddScoreToUserId(Score, (uint)data.Id, RankingType.COMPETITIONS);
                }
                else
                {
                    Session.SendNotif("El usuario al que quieres darle puntos no existe!");
                }
            }
        }

        internal void push()
        {
            Room room = Session.GetHabbo().CurrentRoom;
            if (room == null)
                return;

            RoomUser roomuser = room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (roomuser == null)
                return;
            
            if (Params.Length == 1)
            {
                
                Point squareInfront = CoordinationUtil.GetPointInFront(roomuser.Coordinate, roomuser.RotBody);
                List<RoomUser> users = room.GetGameMap().GetRoomUsers(squareInfront);

                Point squareInFrontOfUserInFront = CoordinationUtil.GetPointInFront(squareInfront, roomuser.RotBody); //Yo dawg, we heard yo like coordinates, so we put a coordinate inside yo coordinate
                if (room.GetGameMap().CanWalk(squareInFrontOfUserInFront.X, squareInFrontOfUserInFront.Y, false) == false)
                {
                    return;
                }

                foreach (RoomUser user in users)
                {
                    user.MoveTo(squareInFrontOfUserInFront);
                }
            }
            else
            {
                RoomUser roomuserTarget = room.GetRoomUserManager().GetRoomUserByHabbo(Params[1]);
                if (roomuserTarget == null)
                    return;

                Point furtherstSquare = CoordinationUtil.GetPointBehind(roomuserTarget.Coordinate, roomuserTarget.RotBody);

                Point a = new Point(furtherstSquare.X, furtherstSquare.Y++);
                if (CoordinationUtil.GetDistance(furtherstSquare, a) > CoordinationUtil.GetDistance(furtherstSquare, roomuserTarget.Coordinate))
                    furtherstSquare = a;

                Point b = new Point(furtherstSquare.X, furtherstSquare.Y--);
                if (CoordinationUtil.GetDistance(furtherstSquare, b) > CoordinationUtil.GetDistance(furtherstSquare, roomuserTarget.Coordinate))
                    furtherstSquare = b;

                Point c = new Point(furtherstSquare.X++, furtherstSquare.Y);
                if (CoordinationUtil.GetDistance(furtherstSquare, c) > CoordinationUtil.GetDistance(furtherstSquare, roomuserTarget.Coordinate))
                    furtherstSquare = c;

                Point d = new Point(furtherstSquare.X--, furtherstSquare.Y++);
                if (CoordinationUtil.GetDistance(furtherstSquare, d) > CoordinationUtil.GetDistance(furtherstSquare, roomuserTarget.Coordinate))
                    furtherstSquare = d;

                Point e = new Point(furtherstSquare.X++, furtherstSquare.Y--);
                if (CoordinationUtil.GetDistance(furtherstSquare, e) > CoordinationUtil.GetDistance(furtherstSquare, roomuserTarget.Coordinate))
                    furtherstSquare = e;

                Point f = new Point(furtherstSquare.X--, furtherstSquare.Y);
                if (CoordinationUtil.GetDistance(furtherstSquare, f) > CoordinationUtil.GetDistance(furtherstSquare, roomuserTarget.Coordinate))
                    furtherstSquare = f;

                roomuserTarget.MoveTo(furtherstSquare);
            }
        }

        internal void pull()
        {
            Room room = Session.GetHabbo().CurrentRoom;
            if (room == null)
                return;


            RoomUser roomuser = room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (roomuser == null)
                return;

            if (Params.Length == 1)
            {
                Point squareInFront = CoordinationUtil.GetPointInFront(roomuser.Coordinate, roomuser.RotBody);
                Point squareInFrontInFront = CoordinationUtil.GetPointBehind(squareInFront, CoordinationUtil.RotationIverse(roomuser.RotBody));
                List<RoomUser> users = room.GetGameMap().GetRoomUsers(squareInFrontInFront);

                foreach (RoomUser user in users)
                {
                    user.MoveTo(squareInFront);
                }
            }
            else
            {
                RoomUser roomuserTarget = room.GetRoomUserManager().GetRoomUserByHabbo(Params[1]);
                if (roomuserTarget == null)
                    return;

                Point closestSquare = CoordinationUtil.GetPointBehind(roomuserTarget.Coordinate, roomuserTarget.RotBody);

                Point a = new Point(closestSquare.X, closestSquare.Y++);
                if (CoordinationUtil.GetDistance(closestSquare, a) < CoordinationUtil.GetDistance(closestSquare, roomuserTarget.Coordinate))
                    closestSquare = a;

                Point b = new Point(closestSquare.X, closestSquare.Y--);
                if (CoordinationUtil.GetDistance(closestSquare, b) < CoordinationUtil.GetDistance(closestSquare, roomuserTarget.Coordinate))
                    closestSquare = b;

                Point c = new Point(closestSquare.X++, closestSquare.Y);
                if (CoordinationUtil.GetDistance(closestSquare, c) < CoordinationUtil.GetDistance(closestSquare, roomuserTarget.Coordinate))
                    closestSquare = c;

                Point d = new Point(closestSquare.X--, closestSquare.Y++);
                if (CoordinationUtil.GetDistance(closestSquare, d) < CoordinationUtil.GetDistance(closestSquare, roomuserTarget.Coordinate))
                    closestSquare = d;

                Point e = new Point(closestSquare.X++, closestSquare.Y--);
                if (CoordinationUtil.GetDistance(closestSquare, e) < CoordinationUtil.GetDistance(closestSquare, roomuserTarget.Coordinate))
                    closestSquare = e;

                Point f = new Point(closestSquare.X--, closestSquare.Y);
                if (CoordinationUtil.GetDistance(closestSquare, f) < CoordinationUtil.GetDistance(closestSquare, roomuserTarget.Coordinate))
                    closestSquare = f;

                roomuserTarget.MoveTo(closestSquare);
            }
        }

        internal void copylook()
        {
            string copyTarget = Params[1];
            bool findResult = false;


            string gender = null;
            string figure = null;
            DataRow dRow;
            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("SELECT gender,look FROM users WHERE username = @username");
                dbClient.addParameter("username", copyTarget);
                dRow = dbClient.getRow();

                if (dRow != null)
                {
                    findResult = true;
                    gender = (string)dRow[0];
                    figure = (string)dRow[1];

                    dbClient.setQuery("UPDATE users SET gender = @gender, look = @look WHERE username = @username");
                    dbClient.addParameter("gender", gender);
                    dbClient.addParameter("look", figure);
                    dbClient.addParameter("username", Session.GetHabbo().Username);
                    dbClient.runQuery();
                }
            }

            if (findResult)
            {
                Session.GetHabbo().Gender = gender;
                Session.GetHabbo().Look = figure;
                Session.GetMessageHandler().GetResponse().Init(Outgoing.UpdateUserInformation);
                Session.GetMessageHandler().GetResponse().AppendInt32(-1);
                Session.GetMessageHandler().GetResponse().AppendStringWithBreak(Session.GetHabbo().Look);
                Session.GetMessageHandler().GetResponse().AppendStringWithBreak(Session.GetHabbo().Gender.ToLower());
                Session.GetMessageHandler().GetResponse().AppendStringWithBreak(Session.GetHabbo().Motto);
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
                    RoomUpdate.AppendStringWithBreak(Session.GetHabbo().Look);
                    RoomUpdate.AppendStringWithBreak(Session.GetHabbo().Gender.ToLower());
                    RoomUpdate.AppendStringWithBreak(Session.GetHabbo().Motto);
                    RoomUpdate.AppendInt32(Session.GetHabbo().AchievementPoints);
                    Room.SendMessage(RoomUpdate);
                }
            }
        }


        internal void pickall()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            TargetRoom = Session.GetHabbo().CurrentRoom;

            if (TargetRoom != null && TargetRoom.CheckRights(Session, true))
            {
                List<RoomItem> RemovedItems = TargetRoom.GetRoomItemHandler().RemoveAllFurniture(Session);

                Session.GetHabbo().GetInventoryComponent().AddItemArray(RemovedItems);
                Session.GetHabbo().GetInventoryComponent().UpdateItems(false); //ARGH!
            }
        }

        internal void setspeed()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            TargetRoom = Session.GetHabbo().CurrentRoom;
            if (TargetRoom != null && TargetRoom.CheckRights(Session, true))
            {
                try
                {
                    Session.GetHabbo().CurrentRoom.GetRoomItemHandler().SetSpeed(int.Parse(Params[1]));
                }
                catch { Session.SendNotif(LanguageLocale.GetValue("input.intonly")); }
            }
        }

        internal void unload()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            FirewindEnvironment.GetGame().GetRoomManager().UnloadRoom(TargetRoom);
            //TargetRoom.RequestReload();
        }

        internal void disablediagonal()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;

            TargetRoom = Session.GetHabbo().CurrentRoom;

            if (TargetRoom.GetGameMap().DiagonalEnabled)
                TargetRoom.GetGameMap().DiagonalEnabled = false;
            else
                TargetRoom.GetGameMap().DiagonalEnabled = true;
        }

        internal void setmax()
        {

            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            TargetRoom = Session.GetHabbo().CurrentRoom;
            UInt32 roomid = TargetRoom.RoomId;

            try
            {
                int MaxUsers = int.Parse(Params[1]);

                if ((MaxUsers > 100 && Session.GetHabbo().Rank == 1) || MaxUsers > 200)
                    Session.SendNotif(LanguageLocale.GetValue("setmax.maxusersreached"));
                else
                {
                    TargetRoom.SetMaxUsers(MaxUsers);
                }
            }
            catch
            { }
        }

        internal void overridee()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            RoomUser TargetRoomUser = null;

            TargetRoom = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (TargetRoom == null)
                return;

            TargetRoomUser = TargetRoom.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (TargetRoomUser == null)
                return;

            if (TargetRoomUser.AllowOverride)
            {
                TargetRoomUser.AllowOverride = false;
                Session.SendNotif(LanguageLocale.GetValue("override.disabled"));
            }
            else
            {
                TargetRoomUser.AllowOverride = true;
                Session.SendNotif(LanguageLocale.GetValue("override.enabled"));
            }

            TargetRoom.GetGameMap().GenerateMaps();
        }

        internal void warp()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            RoomUser TargetRoomUser = null;

            TargetRoom = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            TargetRoomUser = TargetRoom.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (TargetRoomUser == null)
                return;
            if (!TargetRoom.CheckRights(Session,false))
            {
                return;
            }

            TargetRoomUser.TeleportEnabled = !TargetRoomUser.TeleportEnabled;

            TargetRoom.GetGameMap().GenerateMaps();
        }
        
        internal void teleport()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            RoomUser TargetRoomUser = null;

            TargetRoom = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            TargetRoomUser = TargetRoom.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (TargetRoomUser == null)
                return;

            TargetRoomUser.TeleportEnabled = !TargetRoomUser.TeleportEnabled;

            TargetRoom.GetGameMap().GenerateMaps();
        }

        internal static void catarefresh()
        {
            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                FirewindEnvironment.GetGame().GetCatalog().Initialize(dbClient);
                FirewindEnvironment.GetGame().GetItemManager().LoadItems(dbClient);
            }
            FirewindEnvironment.GetGame().GetCatalog().InitCache();
            FirewindEnvironment.GetGame().GetClientManager().QueueBroadcaseMessage(new ServerMessage(Outgoing.UpdateShop));
        }

        internal void unbanUser()
        {
            if (Params.Length > 1)
            {
                FirewindEnvironment.GetGame().GetBanManager().UnbanUser(Params[1]);
            }
        }

        internal void roomalert()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            TargetRoom = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (TargetRoom == null)
                return;

            if (TargetRoom.OwnerId == Session.GetHabbo().Id && Session.GetHabbo().Rank >= 2)
            {
				if(Session.GetHabbo().Rank < 5)
				{
					string Msg = MergeParams(Params, 1);

					ServerMessage nMessage = new ServerMessage();
					nMessage.Init(Outgoing.SendNotif);
					nMessage.AppendString("Fra roomeier: (" + TargetRoom.Owner + ")\n" + Msg);
					nMessage.AppendString("");
					TargetRoom.QueueRoomMessage(nMessage);
				}
				else
				{
					string Msg = MergeParams(Params, 1);

					ServerMessage nMessage = new ServerMessage();
					nMessage.Init(Outgoing.SendNotif);
					nMessage.AppendString(Msg);
					nMessage.AppendString("");
					FirewindEnvironment.GetGame().GetModerationTool().LogStaffEntry(Session.GetHabbo().Username, string.Empty, "Alert", "Room alert with message [" + Msg + "]");
					TargetRoom.QueueRoomMessage(nMessage);
				}
            }
            else
            {
                if (Session.GetHabbo().Rank > 5)
                {
                    string Msg = MergeParams(Params, 1);

                    ServerMessage nMessage = new ServerMessage();
                    nMessage.Init(Outgoing.SendNotif);
                    nMessage.AppendString(Msg);
                    nMessage.AppendString("");
                    FirewindEnvironment.GetGame().GetModerationTool().LogStaffEntry(Session.GetHabbo().Username, string.Empty, "Alert", "Room alert with message [" + Msg + "]");
                    TargetRoom.QueueRoomMessage(nMessage);
                }
            }
        }

        internal void coords()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            RoomUser TargetRoomUser = null;
            TargetRoom = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (TargetRoom == null)
            {
                return;
            }

            TargetRoomUser = TargetRoom.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (TargetRoomUser == null)
            {
                return;
            }

            Session.SendNotif("X: " + TargetRoomUser.X + " - Y: " + TargetRoomUser.Y + " - Z: " + TargetRoomUser.Z + " - Rot: " + TargetRoomUser.RotBody + ", sqState: " + TargetRoom.GetGameMap().GameMap[TargetRoomUser.X, TargetRoomUser.Y].ToString());
        }

        internal void coins()
        {
            GameClient TargetClient = null;
            Room TargetRoom = Session.GetHabbo().CurrentRoom;

            TargetClient = FirewindEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient != null)
            {
                int creditsToAdd;
                if (int.TryParse(Params[2], out creditsToAdd))
                {
                    TargetClient.GetHabbo().Credits = TargetClient.GetHabbo().Credits + creditsToAdd;
                    TargetClient.GetHabbo().UpdateCreditsBalance();
                    TargetClient.SendNotif(Session.GetHabbo().Username + LanguageLocale.GetValue("coins.awardmessage1") + creditsToAdd.ToString() + LanguageLocale.GetValue("coins.awardmessage2"));
                    Session.SendNotif(LanguageLocale.GetValue("coins.updateok"));
                    return;
                }
                else
                {
                    Session.SendNotif(LanguageLocale.GetValue("input.intonly"));
                    return;
                }
            }
            else
            {
                int creditsToAdd;
                if (int.TryParse(Params[2], out creditsToAdd))
                {
                    using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                    {
                        int UserAmount = 0;
                        int UserId = 0;
                        string Username = Params[1];

                        dbClient.setQuery("SELECT id, credits FROM users WHERE username = @username");
                        dbClient.addParameter("username", Username);

                        DataTable table = dbClient.getTable();
                        foreach (DataRow d in table.Rows)
                        {
                            UserAmount = (int)d["credits"];
                            UserId = (int)(uint)d["id"];
                        }

                        dbClient.runFastQuery("UPDATE users SET credits = " + (UserAmount + creditsToAdd) + " WHERE id = " + UserId);
                        Session.SendNotif(Username + " now has " + (UserAmount + creditsToAdd) + " credits.");
                    }
                    return;
                }
                else
                {
                    Session.SendNotif(LanguageLocale.GetValue("input.intonly"));
                    return;
                }
            }
        }

        internal void pixels()
        {
            GameClient TargetClient = null;
            Room TargetRoom = Session.GetHabbo().CurrentRoom;

            TargetClient = FirewindEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient != null)
            {
                int creditsToAdd;
                if (int.TryParse(Params[2], out creditsToAdd))
                {
                    TargetClient.GetHabbo().ActivityPoints = TargetClient.GetHabbo().ActivityPoints + creditsToAdd;
                    TargetClient.GetHabbo().UpdateActivityPointsBalance(true);
                    TargetClient.SendNotif(Session.GetHabbo().Username + LanguageLocale.GetValue("pixels.awardmessage1") + creditsToAdd.ToString() + LanguageLocale.GetValue("pixels.awardmessage2"));
                    Session.SendNotif(LanguageLocale.GetValue("pixels.updateok"));
                    return;
                }
                else
                {
                    Session.SendNotif(LanguageLocale.GetValue("input.intonly"));
                    return;
                }
            }
            else
            {
                int creditsToAdd;
                if (int.TryParse(Params[2], out creditsToAdd))
                {
                    using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                    {
                        int UserAmount = 0;
                        int UserId = 0;
                        string Username = Params[1];

                        dbClient.setQuery("SELECT id, activity_points FROM users WHERE username = @username");
                        dbClient.addParameter("username", Username);

                        DataTable table = dbClient.getTable();
                        foreach (DataRow d in table.Rows)
                        {
                            UserAmount = (int)d["activity_points"];
                            UserId = (int)(uint)d["id"];
                        }

                        dbClient.runFastQuery("UPDATE users SET activity_points = " + (UserAmount + creditsToAdd) + " WHERE id = " + UserId);
                        Session.SendNotif(Username + " now has " + (UserAmount + creditsToAdd) + " pixels.");
                    }
                    return;
                }
            }
        }

        internal void handitem()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            RoomUser TargetRoomUser = null;
            TargetRoom = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (TargetRoom == null)
            {
                return;
            }

            TargetRoomUser = TargetRoom.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (TargetRoomUser == null)
            {
                return;
            }

            try
            {
                TargetRoomUser.CarryItem(int.Parse(Params[1]));
            }
            catch { }

            return;
        }

        internal void sit()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            RoomUser TargetRoomUser = null;
            TargetRoom = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (TargetRoom == null)
            {
                return;
            }

            TargetRoomUser = TargetRoom.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (TargetRoomUser == null)
            {
                return;
            }

            try
            {
                if (TargetRoomUser.acostadoBol == true)
                {
                    TargetRoomUser.acostadoBol = false;
                    TargetRoomUser.RemoveStatus("lay");
                }
                if (!TargetRoomUser.Statusses.ContainsKey("sit"))
                {
                    if ((TargetRoomUser.RotBody % 2) == 0)
                    {
                        TargetRoomUser.AddStatus("sit", Convert.ToString(TargetRoom.GetGameMap().Model.SqFloorHeight[TargetRoomUser.X, TargetRoomUser.Y] + 0.55).Replace(",", "."));
                        TargetRoomUser.sentadoBol = true;
                        TargetRoomUser.UpdateNeeded = true;
                    }
                    else
                    {
                        Session.SendNotif(LanguageLocale.GetValue("diag.noaction"));
                    }
                }
            }
            catch { }

            return;
        }


        internal void lay()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            RoomUser TargetRoomUser = null;
            TargetRoom = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (TargetRoom == null)
            {
                return;
            }

            TargetRoomUser = TargetRoom.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (TargetRoomUser == null)
            {
                return;
            }

            try
            {
                if (TargetRoomUser.sentadoBol == true)
                {
                    TargetRoomUser.sentadoBol = false;
                    TargetRoomUser.RemoveStatus("sit");
                }

                if (!TargetRoomUser.Statusses.ContainsKey("lay"))
                {
                    if ((TargetRoomUser.RotBody % 2) == 0)
                    {
                        TargetRoomUser.AddStatus("lay", Convert.ToString(TargetRoom.GetGameMap().Model.SqFloorHeight[TargetRoomUser.X, TargetRoomUser.Y] + 0.55).Replace(",", "."));
                        TargetRoomUser.acostadoBol = true;
                        TargetRoomUser.UpdateNeeded = true;
                    }
                    else
                    {
                        Session.SendNotif(LanguageLocale.GetValue("diag.noaction"));
                    }
                }
            }
            catch { }

            return;
        }


        internal void hotelalert()
        {
            string Notice = GetInput(Params).Substring(4);
            ServerMessage HotelAlert = new ServerMessage(Outgoing.BroadcastMessage);
            HotelAlert.AppendStringWithBreak(LanguageLocale.GetValue("hotelallert.notice") + "\r\n" + 
            Notice + "\r\n" + "- " + Session.GetHabbo().Username);
            FirewindEnvironment.GetGame().GetClientManager().QueueBroadcaseMessage(HotelAlert);
            FirewindEnvironment.GetGame().GetModerationTool().LogStaffEntry(Session.GetHabbo().Username, string.Empty, "HotelAlert", "Hotel alert [" + Notice + "]");

            //FirewindEnvironment.messagingBot.SendMassMessage(new PublicMessage(string.Format("[{0}] => [{1}]", Session.GetHabbo().Username, Notice)), true);
        }

        internal void freeze()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            RoomUser Target = Session.GetHabbo().CurrentRoom.GetRoomUserManager().GetRoomUserByHabbo(Params[1]);
            if (Target != null)
                Target.Freezed = (Target.Freezed != true);
        }

        internal void buyx()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            try
            {
                int userInput = int.Parse(Params[1]);
                if (Session.GetHabbo().Rank > 1)
                {
                    if (userInput <= 0)
                    {
                        Session.SendNotif(LanguageLocale.GetValue("buyx.maxminreached"));
                    }
                    else
                    {
                        Session.GetHabbo().buyItemLoop = userInput;
                    }
                }
                else
                {
                    if (userInput <= 0 || userInput >= 50)
                    {
                        Session.SendNotif(LanguageLocale.GetValue("buyx.maxminreached"));
                    }
                    else
                    {
                        Session.GetHabbo().buyItemLoop = userInput;
                    }

                }
            }
            catch
            {
                Session.SendNotif(LanguageLocale.GetValue("input.intonly"));
            }
        }

        internal void enable()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;

            int EffectID = 0;
            if (int.TryParse(Params[1], out EffectID))
            {
                Session.GetHabbo().GetAvatarEffectsInventoryComponent().ApplyCustomEffect(EffectID);
            }
            else
            {
                Session.SendNotif(LanguageLocale.GetValue("input.intonly"));
            }
        }

        internal void roommute()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            if (Session.GetHabbo().CurrentRoom.RoomMuted)
                Session.GetHabbo().CurrentRoom.RoomMuted = false;
            else
                Session.GetHabbo().CurrentRoom.RoomMuted = true;

            FirewindEnvironment.GetGame().GetModerationTool().LogStaffEntry(Session.GetHabbo().Username, string.Empty, "Room Mute", "Room muted");

        }

        public void masscredits()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;

            try
            {
                int CreditAmount = int.Parse(Params[1]);
                FirewindEnvironment.GetGame().GetClientManager().QueueCreditsUpdate(CreditAmount);
                FirewindEnvironment.GetGame().GetModerationTool().LogStaffEntry(Session.GetHabbo().Username, string.Empty, "Mass Credits", "Send [" + CreditAmount + "] credits to everyone online");

            }
            catch
            {
                Session.SendNotif(LanguageLocale.GetValue("input.intonly"));
            }
        }

        internal void globalcredits()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;

            try
            {
                int CreditAmount = int.Parse(Params[1]);
                FirewindEnvironment.GetGame().GetClientManager().QueueCreditsUpdate(CreditAmount);

                using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                    dbClient.runFastQuery("UPDATE users SET credits = credits + " + CreditAmount);

                FirewindEnvironment.GetGame().GetModerationTool().LogStaffEntry(Session.GetHabbo().Username, string.Empty, "Mass Credits", "Send [" + CreditAmount + "] credits to everyone in the database");

            }
            catch
            {
                Session.SendNotif(LanguageLocale.GetValue("input.intonly"));
            }
        }

        internal void openroom()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;

            try
            {
                int roomID = int.Parse(Params[1]);

                Session.GetMessageHandler().ForwardToRoom(roomID);
            }
            catch
            {
                Session.SendNotif(LanguageLocale.GetValue("input.intonly"));
            }
        }

        internal void stalk()
        {
            GameClient TargetClient = FirewindEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            if (TargetClient == null || TargetClient.GetHabbo() == null || TargetClient.GetHabbo().CurrentRoom == null)
                return;

            Session.GetMessageHandler().ForwardToRoom((int)TargetClient.GetHabbo().CurrentRoom.RoomId);
        }

        internal void roombadge()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            if (Session.GetHabbo().CurrentRoom == null)
                return;
            
            TargetRoom.QueueRoomBadge(Params[1]);

            FirewindEnvironment.GetGame().GetModerationTool().LogStaffEntry(Session.GetHabbo().Username, string.Empty, "Badge", "Roombadge in room [" + TargetRoom.RoomId + "] with badge [" + Params[1] + "]");

        }

        internal void massbadge()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            FirewindEnvironment.GetGame().GetClientManager().QueueBadgeUpdate(Params[1]);
            FirewindEnvironment.GetGame().GetModerationTool().LogStaffEntry(Session.GetHabbo().Username, string.Empty, "Badge", "Mass badge with badge [" + Params[1] + "]");
        }

        internal void language()
        {
            string targetUser = Params[1];
            DataRow Result;
            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("SELECT country FROM users JOIN ip_cache ON (users.ip_last = ip_cache.ip) AND username = @username");
                dbClient.addParameter("username", targetUser);
                Result = dbClient.getRow();
            }

            Session.SendNotif(targetUser + LanguageLocale.GetValue("language.notif") + (string)Result["country"]);
        }

        internal void userinfo()
        {
            string username = Params[1];
            bool UserOnline = true;
            if (string.IsNullOrEmpty(username))
            {
                Session.SendNotif(LanguageLocale.GetValue("input.userparammissing"));
                return;
            }

            GameClient tTargetClient = FirewindEnvironment.GetGame().GetClientManager().GetClientByUsername(username);

            if (tTargetClient == null || tTargetClient.GetHabbo() == null)
            {
                Session.SendNotif(LanguageLocale.GetValue("input.useroffline"));
                return;
            }
            Habbo User = tTargetClient.GetHabbo();

            //Habbo User = FirewindEnvironment.GetGame().GetClientManager().GetClientByUserID(username).GetHabbo();
            StringBuilder RoomInformation = new StringBuilder();

            if (User.CurrentRoom != null)
            {
                RoomInformation.Append(" - " + LanguageLocale.GetValue("roominfo.title") + " [" + User.CurrentRoom.RoomId + "] - \r");
                RoomInformation.Append(LanguageLocale.GetValue("userinfo.owner") + User.CurrentRoom.Owner + "\r");
                RoomInformation.Append(LanguageLocale.GetValue("userinfo.roomname") + User.CurrentRoom.Name + "\r");
                RoomInformation.Append(LanguageLocale.GetValue("userinfo.usercount") + User.CurrentRoom.UserCount + "/" + User.CurrentRoom.UsersMax);
            }

            Session.SendNotif(LanguageLocale.GetValue("userinfo.userinfotitle") + username + ":\r" +
                LanguageLocale.GetValue("userinfo.rank") + User.Rank + " \r" +
                LanguageLocale.GetValue("userinfo.isonline") + UserOnline.ToString() + " \r" +
                LanguageLocale.GetValue("userinfo.userid") + User.Id + " \r" +
                LanguageLocale.GetValue("userinfo.visitingroom") + User.CurrentRoomId + " \r" +
                LanguageLocale.GetValue("userinfo.motto") + User.Motto + " \r" +
                LanguageLocale.GetValue("userinfo.credits") + User.Credits + " \r" +
                LanguageLocale.GetValue("userinfo.ismuted") + User.Muted.ToString() + "\r" +
                "\r\r" +
                RoomInformation.ToString());
        }

        internal void linkAlert()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            // Hotel Alert pluss link :hal <link> <message>
            string Link = Params[1];

            string Message = MergeParams(Params, 2);

            ServerMessage nMessage = new ServerMessage(Outgoing.SendNotif);
            nMessage.AppendStringWithBreak(LanguageLocale.GetValue("hotelallert.notice") + "\r\n" + Message + "\r\n-" + Session.GetHabbo().Username);
            nMessage.AppendStringWithBreak(Link);
            FirewindEnvironment.GetGame().GetClientManager().QueueBroadcaseMessage(nMessage);


            //FirewindEnvironment.messagingBot.SendMassMessage(new PublicMessage(string.Format("[{0}] => [{1}] + [{2}]", Session.GetHabbo().Username, Link, Message)), true);
        }

        internal void shutdown()
        {
            Logging.LogCriticalException("User " + Session.GetHabbo().Username + " shut down the server " + DateTime.Now.ToString());
            Task ShutdownTask = new Task(FirewindEnvironment.PreformShutDown);
            ShutdownTask.Start();
        }

        internal void dumpmaps()
        {
            StringBuilder Dump = new StringBuilder();
            Dump.Append(Session.GetHabbo().CurrentRoom.GetGameMap().GenerateMapDump());

            FileStream errWriter = new System.IO.FileStream(@"Logs\mapdumps.txt", System.IO.FileMode.Append, System.IO.FileAccess.Write);
            byte[] Msg = ASCIIEncoding.ASCII.GetBytes(Dump.ToString() + "\r\n\r\n");
            errWriter.Write(Msg, 0, Msg.Length);
            errWriter.Dispose();
        }

        internal void giveBadge()
        {
            GameClient TargetClient = null;
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            //.GetBadgeComponent().GiveBadge("HC1", true);

            TargetClient = FirewindEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient != null)
            {
                TargetClient.GetHabbo().GetBadgeComponent().GiveBadge(FirewindEnvironment.FilterInjectionChars(Params[2]), true);

                FirewindEnvironment.GetGame().GetModerationTool().LogStaffEntry(Session.GetHabbo().Username, TargetClient.GetHabbo().Username, "Badge", "Badge given to user [" + Params[2] + "]");
                return;
            }
            else
            {
                Session.SendNotif(LanguageLocale.GetValue("input.usernotfound"));
                return;
            }
        }

        internal void invisible()
        {
            if (Session.GetHabbo().SpectatorMode)
            {
                Session.GetHabbo().SpectatorMode = false;
                Session.SendNotif(LanguageLocale.GetValue("invisible.enabled"));
            }
            else
            {
                Session.GetHabbo().SpectatorMode = true;
                Session.SendNotif(LanguageLocale.GetValue("invisible.disabled"));
            }
        }

        internal void giveCrystals()
        {
            GameClient TargetClient = null;
            Room TargetRoom = Session.GetHabbo().CurrentRoom;

            TargetClient = FirewindEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            if (TargetClient == null)
            {
                Session.SendNotif(LanguageLocale.GetValue("input.usernotfound"));
                return;
            }
            try
            {
                TargetClient.GetHabbo().GiveUserCrystals(int.Parse(Params[2]));
                Session.SendNotif("Send " + Params[2] + " Credits to " + Params[1]);

                FirewindEnvironment.GetGame().GetModerationTool().LogStaffEntry(Session.GetHabbo().Username, TargetClient.GetHabbo().Username, "BelCredits/crystals", "Belcredits/crystals amount [" + Params[2] + "]");
            }
            catch (FormatException) { return; }

        }

        internal void ban()
        {
            GameClient TargetClient = null;
            Room TargetRoom = Session.GetHabbo().CurrentRoom;

            TargetClient = FirewindEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            
            if (TargetClient == null)
            {
                Session.SendNotif(LanguageLocale.GetValue("input.usernotfound"));
                return;
            }

            if (TargetClient.GetHabbo().Rank >= Session.GetHabbo().Rank)
            {
                Session.SendNotif(LanguageLocale.GetValue("ban.notallowed"));
                return;
            }

            int BanTime = 0;

            try
            {
                BanTime = int.Parse(Params[2]);
            }
            catch (FormatException) { return; }

            if (BanTime <= 600)
            {
                Session.SendNotif(LanguageLocale.GetValue("ban.toolesstime"));
            }
            else
            {
                FirewindEnvironment.GetGame().GetBanManager().BanUser(TargetClient, Session.GetHabbo().Username, BanTime, MergeParams(Params, 3), false);
                FirewindEnvironment.GetGame().GetModerationTool().LogStaffEntry(Session.GetHabbo().Username, TargetClient.GetHabbo().Username, "Ban", "Ban for " + BanTime + " seconds with message " + MergeParams(Params, 3));
            }
        }

        internal void disconnect()
        {
            GameClient TargetClient = null;
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            TargetClient = FirewindEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            if (TargetClient == null)
            {
                Session.SendNotif("User not found.");
                return;
            }

            if (TargetClient.GetHabbo().Rank >= Session.GetHabbo().Rank)
            {
                Session.SendNotif(LanguageLocale.GetValue("disconnect.notallwed"));
                return;
            }
            FirewindEnvironment.GetGame().GetModerationTool().LogStaffEntry(Session.GetHabbo().Username, TargetClient.GetHabbo().Username, "Disconnect", "User disconnected by user");

            TargetClient.GetConnection().Dispose();
        }

        internal void superban()
        {
            GameClient TargetClient = null;
            Room TargetRoom = Session.GetHabbo().CurrentRoom;

            TargetClient = FirewindEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            if (TargetClient == null)
            {
                Session.SendNotif(LanguageLocale.GetValue("input.usernotfound"));
                return;
            }

            if (TargetClient.GetHabbo().Rank >= Session.GetHabbo().Rank)
            {
                Session.SendNotif(LanguageLocale.GetValue("ban.notallowed"));
                return;
            }
            int BanTime;
            try
            {
                BanTime = int.Parse(Params[2]);
            }
            catch (FormatException) { return; }

            FirewindEnvironment.GetGame().GetModerationTool().LogStaffEntry(Session.GetHabbo().Username, TargetClient.GetHabbo().Username, "Ban", "Long ban for " + BanTime + " seconds");

            if (BanTime <= 600)
            {
                Session.SendNotif(LanguageLocale.GetValue("ban.toolesstime"));
            }
            else
            {
                FirewindEnvironment.GetGame().GetBanManager().BanUser(TargetClient, Session.GetHabbo().Username, BanTime, MergeParams(Params, 3), true);
            }
        }

        internal void langban()
        {
            GameClient TargetClient = null;
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            TargetClient = FirewindEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            if (TargetClient == null)
            {
                Session.SendNotif(LanguageLocale.GetValue("input.usernotfound"));
                return;
            }

            if (TargetClient.GetHabbo().Rank >= Session.GetHabbo().Rank)
            {
                Session.SendNotif(LanguageLocale.GetValue("ban.notallowed"));
                return;
            }

            FirewindEnvironment.GetGame().GetModerationTool().LogStaffEntry(Session.GetHabbo().Username, TargetClient.GetHabbo().Username, "Ban", "Long ban for 31536000 seconds");
            FirewindEnvironment.GetGame().GetBanManager().BanUser(TargetClient, Session.GetHabbo().Username, 31536000, "This is an english hotel. Therefore, brazilian, portugeese and spanish users has to find their own retro, and not mess with an english one.", true);
        }

        internal void roomkick()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;

            TargetRoom = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (TargetRoom == null)
            {
                return;
            }

            string ModMsg = MergeParams(Params, 1);

            RoomKick kick = new RoomKick(ModMsg, (int)Session.GetHabbo().Rank);

            FirewindEnvironment.GetGame().GetModerationTool().LogStaffEntry(Session.GetHabbo().Username, string.Empty, "Room kick", "Kicked the whole room");
            TargetRoom.QueueRoomKick(kick);
        }

        internal void mute()
        {
            string TargetUser = null;
            GameClient TargetClient = null;
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            TargetUser = Params[1];
            TargetClient = FirewindEnvironment.GetGame().GetClientManager().GetClientByUsername(TargetUser);

            if (TargetClient == null || TargetClient.GetHabbo() == null)
            {
                Session.SendNotif(LanguageLocale.GetValue("input.usernotfound"));
                return;
            }

            if (TargetClient.GetHabbo().Rank >= Session.GetHabbo().Rank)
            {
                Session.SendNotif(LanguageLocale.GetValue("mute.notallowed"));
                return;
            }

            FirewindEnvironment.GetGame().GetModerationTool().LogStaffEntry(Session.GetHabbo().Username, TargetClient.GetHabbo().Username, "Mute", "Muted user");
            TargetClient.GetHabbo().Mute();
        }

        internal void unmute()
        {
            string TargetUser = null;
            GameClient TargetClient = null;
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            TargetUser = Params[1];
            TargetClient = FirewindEnvironment.GetGame().GetClientManager().GetClientByUsername(TargetUser);

            if (TargetClient == null || TargetClient.GetHabbo() == null)
            {
                Session.SendNotif(LanguageLocale.GetValue("input.usernotfound"));
                return;
            }

            //if (TargetClient.GetHabbo().Rank >= Session.GetHabbo().Rank) FUCK YOU!
            //{
            //    Session.SendNotif("You are not allowed to (un)mute that user.");
            //    return true;
            //}
            FirewindEnvironment.GetGame().GetModerationTool().LogStaffEntry(Session.GetHabbo().Username, TargetClient.GetHabbo().Username, "Mute", "Un Muted user");

            TargetClient.GetHabbo().Unmute();
        }

        internal void alert()
        {
            string TargetUser = null;
            GameClient TargetClient = null;
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            TargetUser = Params[1];
            TargetClient = FirewindEnvironment.GetGame().GetClientManager().GetClientByUsername(TargetUser);

            if (TargetClient == null)
            {
                Session.SendNotif(LanguageLocale.GetValue("input.usernotfound"));
                return;
            }

            FirewindEnvironment.GetGame().GetModerationTool().LogStaffEntry(Session.GetHabbo().Username, TargetClient.GetHabbo().Username, "Alert", "Alerted user with message [" + MergeParams(Params, 2) + "]");
            TargetClient.SendNotif(MergeParams(Params, 2), Session.GetHabbo().HasFuse("fuse_admin"));
        }

        internal void deleteMission()
        {
            string TargetUser = Params[1];
            GameClient TargetClient = null;
            Room TargetRoom = Session.GetHabbo().CurrentRoom;

            TargetClient = FirewindEnvironment.GetGame().GetClientManager().GetClientByUsername(TargetUser);

            if (TargetClient == null)
            {
                Session.SendNotif(LanguageLocale.GetValue("input.usernotfound"));
                return;
            }
            if (Session.GetHabbo().Rank <= TargetClient.GetHabbo().Rank)
            {
                Session.SendNotif(LanguageLocale.GetValue("user.notpermitted"));
                return;
            }
            TargetClient.GetHabbo().Motto = LanguageLocale.GetValue("user.unacceptable_motto");
            //TODO update motto

            FirewindEnvironment.GetGame().GetModerationTool().LogStaffEntry(Session.GetHabbo().Username, TargetClient.GetHabbo().Username, "mission removal", "removed mission");

            Room Room = TargetClient.GetHabbo().CurrentRoom;

            if (Room == null)
            {
                return;
            }
            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (User == null)
            {
                return;
            }

            ServerMessage RoomUpdate = new ServerMessage(266);
            RoomUpdate.AppendInt32(User.VirtualId);
            RoomUpdate.AppendStringWithBreak(Session.GetHabbo().Look);
            RoomUpdate.AppendStringWithBreak(Session.GetHabbo().Gender.ToLower());
            RoomUpdate.AppendStringWithBreak(Session.GetHabbo().Motto);
            Room.SendMessage(RoomUpdate);
        }

        internal void kick()
        {
            string TargetUser = null;
            GameClient TargetClient = null;
            Room TargetRoom = Session.GetHabbo().CurrentRoom;

            TargetUser = Params[1];
            TargetClient = FirewindEnvironment.GetGame().GetClientManager().GetClientByUsername(TargetUser);

            if (TargetClient == null)
            {
                Session.SendNotif(LanguageLocale.GetValue("input.usernotfound"));
                return;
            }

            if (Session.GetHabbo().Rank <= TargetClient.GetHabbo().Rank)
            {
                Session.SendNotif(LanguageLocale.GetValue("kick.notallwed"));
                return;
            }

            if (TargetClient.GetHabbo().CurrentRoomId < 1)
            {
                Session.SendNotif(LanguageLocale.GetValue("kick.error"));
                return;
            }

            TargetRoom = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(TargetClient.GetHabbo().CurrentRoomId);

            if (TargetRoom == null)
            {
                return;
            }

            TargetRoom.GetRoomUserManager().RemoveUserFromRoom(TargetClient, true, false);
            TargetClient.CurrentRoomUserID = -1;

            if (Params.Length > 2)
            {
                TargetClient.SendNotif(LanguageLocale.GetValue("kick.withmessage") + MergeParams(Params, 2));
            }
            else
            {
                TargetClient.SendNotif(LanguageLocale.GetValue("kick.nomessage"));
            }
        }

        internal void commands()
        {
            Session.SendMOTD(ChatCommandRegister.GenerateCommandList(Session));
        }

        internal void bh()
        {
            Room Room = Session.GetHabbo().CurrentRoom;
            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);


            if (Params.Length == 1) {
                Session.GetHabbo().StackHeight = 0;
                Session.GetHabbo().StackHeightStatus = false;
                Session.SendNotif("Build height disabled.");
                return;
            }

            if (!double.TryParse(Params[1], out Session.GetHabbo().StackHeight))
            {
                Session.SendNotif("Please enter a valid integer or double value.");
                Session.GetHabbo().StackHeightStatus = false;
                return;
            }

            Session.GetHabbo().StackHeightStatus = true;

            ServerMessage servermsg = new ServerMessage();
            servermsg.Init(Outgoing.Whisp);
            servermsg.AppendInt32(User.VirtualId);
            servermsg.AppendString("Build height updated.");
            servermsg.AppendInt32(0);
            servermsg.AppendInt32(0);
            servermsg.AppendInt32(-1);
            Session.SendMessage(servermsg);
        }

        internal void info()
        {
            DateTime Now = DateTime.Now;
            TimeSpan Uptime = Now - FirewindEnvironment.ServerStarted;

            StringBuilder Alert = new StringBuilder();

            int UsersOnline = FirewindEnvironment.GetGame().GetClientManager().ClientCount;
            int RoomsLoaded = FirewindEnvironment.GetGame().GetRoomManager().LoadedRoomsCount;
            
            Alert.Append("Furni runs on an edit of Butterfly Emulator \n");
            Alert.Append("-----------------------------------------------\n");
            Alert.Append("Server Status:\n");
            Alert.Append("Uptime: " + Uptime.Days + " day(s) " + Uptime.Hours + " hours and " + Uptime.Minutes + " minutes.\n");
            Alert.Append("Users currently online: " + UsersOnline + "\n");
            Alert.Append("Rooms currently loaded: " + RoomsLoaded + "\n\n");

            Session.SendMOTD(Alert.ToString());
        }

        internal void enablestatus()
        {
            Room currentRoom = Session.GetHabbo().CurrentRoom;
            RoomUser user = currentRoom.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Username);

            user.AddStatus(Params[0], string.Empty);
        }

        internal void disablefriends()
        {
            //case "disablefriends":
            //case "disablefriendrequests":
            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.runFastQuery("UPDATE users SET block_newfriends = '1' WHERE id = " + Session.GetHabbo().Id);
            }

            Session.GetHabbo().HasFriendRequestsDisabled = true;
            Session.SendNotif(LanguageLocale.GetValue("friends.disabled"));
        }

        internal void enablefriends()
        {
            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.runFastQuery("UPDATE users SET block_newfriends = '0' WHERE id = " + Session.GetHabbo().Id);
            }
            Session.GetHabbo().HasFriendRequestsDisabled = false;
            Session.SendNotif(LanguageLocale.GetValue("friends.enabled"));
        }

        internal void disabletrade()
        {
            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.runFastQuery("UPDATE users SET block_trade = '1' WHERE id = " + Session.GetHabbo().Id);
            }
            Session.SendNotif(LanguageLocale.GetValue("trade.disable"));
        }

        internal void enabletrade()
        {
            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.runFastQuery("UPDATE users SET block_trade = '0' WHERE id = " + Session.GetHabbo().Id);
            }
            Session.SendNotif(LanguageLocale.GetValue("trade.enable"));
        }


        internal void mordi() // yo mama
        {
            Session.GetHabbo().GetAvatarEffectsInventoryComponent().ApplyCustomEffect(60);
        }

        internal void wheresmypet()
        {
        }

        internal void powerlevels()
        {
            Session.SendNotif("Powerlevel: " + FirewindEnvironment.GetRandomNumber(9001, 10000) + " (Over the FUCKING 9000)");
        }

        internal void forcerot()
        {
            try
            {
                int userInput = int.Parse(Params[1]);
                if (userInput <= -1 || userInput >= 7)
                {
                    Session.SendNotif(LanguageLocale.GetValue("forcerot.inputerror"));
                }
                else
                {
                    Session.GetHabbo().forceRot = userInput;
                }
            }
            catch
            {
                Session.SendNotif(LanguageLocale.GetValue("input.intonly"));
            }
        }

        internal void seteffect()
        {
            Session.GetHabbo().GetAvatarEffectsInventoryComponent().ApplyEffect(int.Parse(Params[1]));
        }

        internal void empty()
        {
            if (Params.Length > 1 && Session.GetHabbo().HasFuse("fuse_sysadmin"))
            {
                GameClient Client = FirewindEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

                if (Client != null) //User online
                {
                    Client.GetHabbo().GetInventoryComponent().ClearItems();
                    Session.SendNotif(LanguageLocale.GetValue("empty.dbcleared"));
                }
                else //Offline
                {
                    using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                    {
                        dbClient.setQuery("SELECT id FROM users WHERE username = @usrname");
                        dbClient.addParameter("usrname", Params[1]);
                        int UserID = int.Parse(dbClient.getString());

                        dbClient.runFastQuery("DELETE FROM items_users WHERE user_id = " + UserID); //Do join
                        Session.SendNotif(LanguageLocale.GetValue("empty.cachecleared"));
                    }
                }
            }
            else
            {
                Session.GetHabbo().GetInventoryComponent().ClearItems();
                Session.SendNotif(LanguageLocale.GetValue("empty.cleared"));
            }
        }

        internal void whosonline()
        {
            int UsersOnline = FirewindEnvironment.GetGame().GetClientManager().ClientCount;
            if (UsersOnline == 1)
                Session.SendNotif("There is " + UsersOnline.ToString() + " user online.");
            if (UsersOnline > 1)
                Session.SendNotif("There are " + UsersOnline.ToString() + " users online.");
        }

        internal void registerIRC()
        {
            
        }

        internal void come()
        {
            if (Params.Length < 1)
            {
                Session.SendNotif("No use specified");
                return;
            }
            string username = Params[1];
            GameClient client = FirewindEnvironment.GetGame().GetClientManager().GetClientByUsername(username);
            if (client == null)
            {
                Session.SendNotif("User is  offline");
                return;
            }

            Room room = Session.GetHabbo().CurrentRoom;

            client.GetMessageHandler().ForwardToRoom((int)room.RoomId);
        }

        internal void Fly()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;
            RoomUser TargetRoomUser = null;

            TargetRoom = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (TargetRoom == null)
                return;

            TargetRoomUser = TargetRoom.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (TargetRoomUser == null)
                return;

            TargetRoomUser.isFlying = true;
            TargetRoomUser.AllowOverride = true;
        }

        internal void globalpixels()
        {
            int amount = int.Parse(Params[1]);
            FirewindEnvironment.GetGame().GetClientManager().QueuePixelsUpdate(amount);

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                dbClient.runFastQuery("UPDATE users SET pixels = pixels + " + amount);
        }

        // Leon's commands

        internal void vipcommands()
        {
            uint Rank = Session.GetHabbo().Rank;

            StringBuilder Notif = new StringBuilder();
            Notif.Append("    -- VIP KOMMANDOER --    \n");

            if (Rank >= 2)
            {
                Notif.Append("- :copy brukernavn for å kopiere stil\n");
                Notif.Append("- :moonwalk for å gå baklengs\n");
                Notif.Append("- :follow brukernavn for å linke en bruker\n");
                Notif.Append("- :lay gjør så du kan ligge på gulvet\n");
                Notif.Append("- :enable og id på effekt\n");
            }
            if (Rank >= 3)
            {
                //Notif.Append("- :makeme red,blue,green,yellow og orange\n");
                Notif.Append("- :massclothes - Alle skifter til samme klær som deg.\n");
            }
            if (Rank >= 4)
            {
                Notif.Append("- :roomalert melding - Fungerer kun i egne rom.\n"); // done
                Notif.Append("- :massdance ID (1,2,3 og 4) - Får alle til å danse.\n"); // done
                Notif.Append("- :massaction wave - Får alle til å vinke.\n"); // done
                Notif.Append("- :massaction kiss - Får alle til å kysse.\n"); // done
                Notif.Append("- :massaction sleep - Får alle til å sove.\n"); // done
                Notif.Append("- :massaction laugh - Får alle til å le.\n"); // done
                Notif.Append("- :masslay - Får alle til å ligge ned.\n"); // done
            }

            Session.SendBroadcastMessage(Notif.ToString());
        }

        internal void massclothes()
        {
            Room currentRoom = Session.GetHabbo().CurrentRoom;
            if (currentRoom != null)
            {
                if (currentRoom.Owner == Session.GetHabbo().Username && Session.GetHabbo().Rank >= 3)
                {
                    List<RoomUser> roomUsers = currentRoom.GetRoomUserManager().GetRoomUsers();
                    foreach (RoomUser user in roomUsers)
                    {
                        ServerMessage RoomUpdate = new ServerMessage(Outgoing.UpdateUserInformation);
                        RoomUpdate.AppendInt32(user.VirtualId);
                        RoomUpdate.AppendStringWithBreak(Session.GetHabbo().Look);
                        RoomUpdate.AppendStringWithBreak(Session.GetHabbo().Gender.ToLower());
                        RoomUpdate.AppendStringWithBreak(user.GetClient().GetHabbo().Motto);
                        RoomUpdate.AppendInt32(user.GetClient().GetHabbo().AchievementPoints);
                        currentRoom.SendMessage(RoomUpdate);
                    }
                }
            }
        }

        internal void masslay()
        {
            Room currentRoom = Session.GetHabbo().CurrentRoom;
            if (currentRoom != null)
            {
                if (currentRoom.Owner == Session.GetHabbo().Username && Session.GetHabbo().Rank >= 4)
                {
                    List<RoomUser> roomUsers = currentRoom.GetRoomUserManager().GetRoomUsers();
                    foreach (RoomUser user in roomUsers)
                    {
                        if (user.sentadoBol == true)
                        {
                            user.sentadoBol = false;
                            user.RemoveStatus("sit");
                        }

                        if (!user.Statusses.ContainsKey("lay"))
                        {
                            if ((user.RotBody % 2) == 0)
                            {
                                user.AddStatus("lay", Convert.ToString(currentRoom.GetGameMap().Model.SqFloorHeight[user.X, user.Y] + 0.55).Replace(",", "."));
                                user.acostadoBol = true;
                                user.UpdateNeeded = true;
                            }
                        }
                    }
                }
            }
        }

        internal void massaction()
        {
            Room currentRoom = Session.GetHabbo().CurrentRoom;
            if (currentRoom != null)
            {
                if (currentRoom.Owner == Session.GetHabbo().Username && Session.GetHabbo().Rank >= 4)
                {
                    string action = Params[1];
                    int ActionId = 0;

                    switch(action)
                    {
                        case "wave":
                            ActionId = 1;
                            break;

                        case "kiss":
                            ActionId = 2;
                            break;

                        case "laugh":
                            ActionId = 3;
                            break;

                        case "sleep":
                            ActionId = 5;
                            break;

                        default:
                            Session.SendNotif(":massaction wave\n:massaction kiss\n:massaction laugh\n:massaction sleep");
                            return;
                    }


                    List<RoomUser> roomUsers = currentRoom.GetRoomUserManager().GetRoomUsers();
                    foreach (RoomUser user in roomUsers)
                    {
                        user.DanceId = 0;
                        ServerMessage message = new ServerMessage(Outgoing.Action);
                        message.AppendInt32(user.VirtualId);
                        message.AppendInt32(ActionId);
                        currentRoom.SendMessage(message);
                        if (ActionId == 5)
                        {
                            user.IsAsleep = true;
                            ServerMessage message2 = new ServerMessage(Outgoing.IdleStatus);
                            message2.AppendInt32(user.VirtualId);
                            message2.AppendBoolean(user.IsAsleep);
                            currentRoom.SendMessage(message2);
                        }
                    }
                }
            }
        }

        internal void massdance()
        {
            Room currentRoom = this.Session.GetHabbo().CurrentRoom;
            if (currentRoom != null)
            {
                if (currentRoom.Owner == Session.GetHabbo().Username && Session.GetHabbo().Rank >= 4)
                {
                    int result = 0;
                    if (int.TryParse(this.Params[1], out result))
                    {
                        List<RoomUser> roomUsers = currentRoom.GetRoomUserManager().GetRoomUsers();
                        foreach (RoomUser user in roomUsers)
                        {
                            user.DanceId = result;
                            ServerMessage message = new ServerMessage(Outgoing.Dance);
                            message.AppendInt32(user.VirtualId);
                            message.AppendInt32(result);
                            currentRoom.SendMessage(message);
                        }
                    }
                }
            }
        }

        internal void massroombadge()
        {
            string Badge = Params[1];
            
            if(Badge != "" && Badge != null)
            {
                using(IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                {
                    dbClient.setQuery("UPDATE rooms SET badge = @badge WHERE id = " + Session.GetHabbo().CurrentRoomId);
                    dbClient.addParameter("badge", Badge);
                    dbClient.runQuery();
                }
                
                Session.SendNotif("Room badge set to: " + Badge);
            } 
            else
            {
                Session.SendNotif("Please specify a badge.");
            }
        }

        internal void vippoints()
        {
            string username = Params[1];
            int amount = int.Parse(Params[2]);

            GameClient client = FirewindEnvironment.GetGame().GetClientManager().GetClientByUsername(username);
       
            if (client == null)
            {
                using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                {
                    int UserAmount = 0;
                    int UserId = 0;

                    dbClient.setQuery("SELECT id, vip_points FROM users WHERE username = @username");
                    dbClient.addParameter("username", username);

                    DataTable table = dbClient.getTable();
                    foreach (DataRow d in table.Rows)
                    {
                        UserAmount = (int)d["vip_points"];
                        UserId = (int)(uint)d["id"];
                    }

                    dbClient.runFastQuery("UPDATE users SET vip_points = " + (UserAmount + amount) + " WHERE id = " + UserId);
                    Session.SendNotif(username + " now has " + (UserAmount + amount) + " VIP Points.");
                }

                return;
            }
            else
            {
                
                client.GetHabbo().VipPoints += amount;

                using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                {
                    dbClient.runFastQuery("UPDATE users SET vip_points = " + client.GetHabbo().VipPoints + " WHERE id = " + client.GetHabbo().Id);
                }

                client.GetHabbo().UpdateActivityPointsBalance(false);

                Session.SendNotif(client.GetHabbo().Username + " now has " + client.GetHabbo().VipPoints + " VIP Points.");
            }
        }

        internal void close()
        {
            Room TargetRoom = Session.GetHabbo().CurrentRoom;

            TargetRoom = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            TargetRoom.Name = LanguageLocale.GetValue("moderation.room.roomclosed");
            TargetRoom.Description = LanguageLocale.GetValue("moderation.room.roomclosed");

            TargetRoom.State = 1;

            ServerMessage nMessage = new ServerMessage();
            nMessage.Init(Outgoing.SendNotif);
            nMessage.AppendString(LanguageLocale.GetValue("moderation.room.roomclosed"));
            nMessage.AppendString("");
            TargetRoom.QueueRoomMessage(nMessage);

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.runFastQuery("UPDATE rooms SET state = 'locked', caption = '" + LanguageLocale.GetValue("moderation.room.roomclosed") + "', description = '" + LanguageLocale.GetValue("moderation.room.roomclosed") + "' WHERE id = " + TargetRoom.RoomId);
            }

            FirewindEnvironment.GetGame().GetRoomManager().UnloadRoom(TargetRoom);
        }

        internal void refresh()
        {
            switch (Params[1])
            {
                default:
                    Session.SendNotif("Invalid request.");
                    break;

                case "navigator":
                    FirewindEnvironment.GetGame().GetNavigator().Initialize(FirewindEnvironment.GetDatabaseManager().getQueryreactor());
                    Session.SendNotif("Reloaded navigator.");
                    break;

                case "bans":
                    FirewindEnvironment.GetGame().GetBanManager().LoadBans(FirewindEnvironment.GetDatabaseManager().getQueryreactor());
                    Session.SendNotif("Reloaded bans");
                    break;

                case "filter":
                    LanguageLocale.InitSwearWord();
                    Session.SendNotif("Reloaded word filter.");
                    break;
                case "catalog":
                    using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                    {
                        FirewindEnvironment.GetGame().GetCatalog().Initialize(dbClient);
                        FirewindEnvironment.GetGame().GetItemManager().LoadItems(dbClient);
                    }
                    FirewindEnvironment.GetGame().GetCatalog().InitCache();
                    FirewindEnvironment.GetGame().GetClientManager().QueueBroadcaseMessage(new ServerMessage(Outgoing.UpdateShop));
                    break;
            }
        }
        #endregion

        internal static string MergeParams(string[] Params, int Start)
        {
            StringBuilder MergedParams = new StringBuilder();

            for (int i = 0; i < Params.Length; i++)
            {
                if (i < Start)
                {
                    continue;
                }

                if (i > Start)
                {
                    MergedParams.Append(" ");
                }

                MergedParams.Append(Params[i]);
            }

            return MergedParams.ToString();
        }

        private static string GetInput(string[] Params)
        {
            StringBuilder builder = new StringBuilder();

            foreach (string param in Params)
                builder.Append(param + " ");

            return builder.ToString();
        }

    }
}
