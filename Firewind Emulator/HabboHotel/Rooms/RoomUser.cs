using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Firewind.HabboHotel.ChatMessageStorage;
using Firewind.HabboHotel.GameClients;
using Firewind.HabboHotel.Groups.Types;
using Firewind.HabboHotel.Misc;
using Firewind.HabboHotel.Pathfinding;
using Firewind.HabboHotel.Pets;
using Firewind.HabboHotel.RoomBots;
using Firewind.HabboHotel.Rooms.Games;
using Firewind.Messages;
using System.Drawing;
using Firewind.Core;
using HabboEvents;
using System.Data;
using Database_Manager.Database.Session_Details.Interfaces;

namespace Firewind.HabboHotel.Rooms
{
    public class RoomUser : IEquatable<RoomUser>
    {
        internal UInt32 HabboId;
        internal Int32 VirtualId;
        internal UInt32 RoomId;

        internal int IdleTime;//byte
        //internal int Steps;

        internal int X;//byte
        internal int Y;//byte
        internal double Z;
        internal byte SqState;

        internal int CarryItemID;//byte
        internal int CarryTimer;//byte

        internal int RotHead;//byte
        internal int RotBody;//byte

        internal bool CanWalk;
        internal bool AllowOverride;
        internal bool TeleportEnabled;

        internal int GoalX;//byte
        internal int GoalY;//byte

        internal Boolean SetStep;
        internal int SetX;//byte
        internal int SetY;//byte
        internal double SetZ;
        internal uint userID;

        internal RoomBot BotData;
        internal BotAI BotAI;

        internal ItemEffectType CurrentItemEffect;
        internal bool Freezed;
        internal int FreezeCounter;
        internal Team team;
        internal FreezePowerUp banzaiPowerUp;
        internal int FreezeLives;

        internal bool shieldActive;
        internal int shieldCounter;

        internal bool throwBallAtGoal;
        internal bool moonwalkEnabled = false;
        internal bool isFlying = false;
        internal int flyk = 0;
        internal bool isMounted = false;
        internal bool sentadoBol = false;
        internal bool acostadoBol = false;
        internal uint mountID = 0;

        internal Point Coordinate
        {
            get
            {
                return new Point(X, Y);
            }
        }

        public bool Equals(RoomUser comparedUser)
        {
            return (comparedUser.HabboId == this.HabboId);
        }

        internal bool IsPet
        {
            get
            {
                return (IsBot && BotData.IsPet);
            }
        }

        internal string GetUsername()
        {
            if (IsBot)
                return string.Empty;

            if (this.GetClient() != null)
            {
                return GetClient().GetHabbo().Username;
            }

            return string.Empty;
        }

        internal int CurrentEffect
        {
            get
            {
                return GetClient().GetHabbo().GetAvatarEffectsInventoryComponent().CurrentEffect;
            }
        }

        internal Pet PetData;

        internal Boolean IsWalking;
        internal Boolean UpdateNeeded;
        internal Boolean IsAsleep;

        internal Dictionary<string, string> Statusses;

        internal int DanceId;

        //internal List<Coord> Path;
        //internal int PathStep;

        //internal bool PathRecalcNeeded;
        //internal int PathRecalcX;
        //internal int PathRecalcY;
        //private int mMessageAmount;

        //internal int TeleDelay;//byte
        private int FloodCount;//byte

        internal Boolean IsDancing
        {
            get
            {
                if (DanceId >= 1)
                {
                    return true;
                }

                return false;
            }
        }

        internal Boolean NeedsAutokick
        {
            get
            {
                if (IsBot)
                    return false;
                if (GetClient() == null || GetClient().GetHabbo() == null)
                    return true;
                if (GetClient().GetHabbo().Rank > 1)
                    return false;
                if (IdleTime >= 1800)
                    return true;

                return false;
            }
        }

        internal bool IsTrading
        {
            get
            {
                if (IsBot)
                {
                    return false;
                }

                if (Statusses.ContainsKey("trd"))
                {
                    return true;
                }

                return false;
            }
        }

        internal bool IsOwner()
        {
            if (IsBot)
                return false;
            return (GetUsername() == GetRoom().Owner);
        }

        internal bool IsBot
        {
            get
            {
                if (this.BotData != null)
                {
                    return true;
                }

                return false;
            }
        }

        internal bool IsSpectator;

        internal int InternalRoomID;

        private Queue events;

        internal RoomUser(uint HabboId, uint RoomId, int VirtualId, Room room, bool isSpectator)
        {
            this.Freezed = false;
            this.HabboId = HabboId;
            this.RoomId = RoomId;
            this.VirtualId = VirtualId;
            this.IdleTime = 0;
            this.X = 0;
            this.Y = 0;
            this.Z = 0;
            this.RotHead = 0;
            this.RotBody = 0;
            this.UpdateNeeded = true;
            this.Statusses = new Dictionary<string, string>();
            //this.Path = new List<Coord>();
            //this.PathStep = 0;
            //this.TeleDelay = -1;
            this.mRoom = room;

            this.AllowOverride = false;
            this.CanWalk = true;

            this.IsSpectator = isSpectator;
            this.SqState = 3;
            //this.Steps = 0;

            this.InternalRoomID = 0;
            this.CurrentItemEffect = ItemEffectType.None;
            this.events = new Queue();
            this.FreezeLives = 0;
            //this.mMessageAmount = 0;
        }

        internal RoomUser(uint HabboId, uint RoomId, int VirtualId, GameClient pClient, Room room)
        {
            this.mClient = pClient;
            this.Freezed = false;
            this.HabboId = HabboId;
            this.RoomId = RoomId;
            this.VirtualId = VirtualId;
            this.IdleTime = 0;
            this.X = 0;
            this.Y = 0;
            this.Z = 0;
            this.RotHead = 0;
            this.RotBody = 0;
            this.UpdateNeeded = true;
            this.Statusses = new Dictionary<string, string>();
            //this.Path = new List<Coord>();
            //this.PathStep = 0;
            //this.TeleDelay = -1;

            this.AllowOverride = false;
            this.CanWalk = true;

            this.IsSpectator = false;
            this.SqState = 3;
            //this.Steps = 0;

            this.InternalRoomID = 0;
            this.CurrentItemEffect = ItemEffectType.None;
            this.mRoom = room;
            this.events = new Queue();
        }

        internal void Unidle()
        {
            this.IdleTime = 0;

            if (this.IsAsleep)
            {
                this.IsAsleep = false;

                ServerMessage Message = new ServerMessage(Outgoing.IdleStatus);
                Message.AppendInt32(VirtualId);
                Message.AppendBoolean(false);

                GetRoom().SendMessage(Message);
            }
        }

        internal void OnFly()
        {
            if (flyk == 0)
            {
                flyk++;
                return;
            }

            double lastK = 0.5 * Math.Sin(0.7 * flyk);
            flyk++;
            double nextK = 0.5 * Math.Sin(0.7 * flyk);
            double differance = nextK - lastK;

            GetRoom().SendMessage(GetRoom().GetRoomItemHandler().UpdateUserOnRoller(this, this.Coordinate, 0, this.Z + differance));
        }

        internal void Dispose()
        {
            Statusses.Clear();
            mRoom = null;
            mClient = null;
        }

        internal void Chat(GameClient Session, string Message, bool Shout)
        {

            if (Session != null)
            {
                if (Session.GetHabbo().Rank < 5)
                {
                    if (GetRoom().RoomMuted)
                        return;
                }
            }

            Unidle();

            if (!IsPet && !IsBot)
            {
                Users.Habbo clientUser = GetClient().GetHabbo();
                if (clientUser.Muted)
                {
                    GetClient().SendNotif("You are muted.");
                    return;
                }

                if (Message.StartsWith(":") && Session != null)
                {
                    string[] parsedCommand = Message.Split(' ');
                    if (ChatCommandRegister.IsChatCommand(parsedCommand[0].ToLower().Substring(1)))
                    {
                        try
                        {
                            ChatCommandHandler handler = new ChatCommandHandler(Message.Split(' '), Session);

                            if (handler.WasExecuted())
                            {
                                //Logging.LogMessage(string.Format("User {0} issued command {1}", GetUsername(), Message));
                                if (Session.GetHabbo().Rank > 5)
                                {
                                    FirewindEnvironment.GetGame().GetModerationTool().LogStaffEntry(Session.GetHabbo().Username, string.Empty, "Chat command", string.Format("Issued chat command {0}", Message));
                                }
                                return;
                            }
                        }
                        catch (Exception x) { Logging.LogException("In-game command error: " + x.ToString()); }
                    }
                    if (FirewindEnvironment.IsHabin)
                    {
                        bool result;
                        using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                        {
                            dbClient.setQuery("SELECT id FROM users WHERE (hpo = '1' OR hpo = '1' OR hds = '1' OR hmg = '1' OR hmb = '1' OR ele = '1' OR lar = '1') AND username = @name");
                            dbClient.addParameter("name", Session.GetHabbo().Username);
                            result = dbClient.findsResult();
                            string command = parsedCommand[0].Substring(1);
                            // Fuck this command system, we make our own!
                            switch (command)
                            {
                                case "relationship":
                                    if (!result)
                                    {
                                        Session.SendMOTD("Du må være medlem av Mafia eller Police for å ha kommandoene til forhold.");
                                        break;
                                    }
                                    dbClient.setQuery("SELECT sender_id FROM users_relationships WHERE (recipent_id = @myid OR sender_id = @myid) AND accepted = '0'");
                                    dbClient.addParameter("myid", Session.GetHabbo().Id);
                                    if (dbClient.findsResult())
                                    {
                                        Session.SendMOTD("Du har allerede spurt om et forhold med denne personen, vennligst vent på et svar.");
                                        return;
                                    }
                                    dbClient.setQuery("SELECT id FROM users WHERE username = @name");
                                    dbClient.addParameter("name", parsedCommand[1]);
                                    int id = dbClient.getInteger();

                                    dbClient.setQuery("INSERT IGNORE INTO users_relationships(sender_id,recipent_id) VALUES(@myid,@hisid)");
                                    dbClient.addParameter("myid", Session.GetHabbo().Id);
                                    dbClient.addParameter("hisid", id);
                                    dbClient.runQuery();

                                    Session.SendMOTD("Du har sendt en forespørsel til " + parsedCommand[1]);
                                    return;

                                case "mystatus":
                                    if (!result)
                                    {
                                        Session.SendMOTD("Du må være medlem av Mafia eller Police for å ha kommandoene til forhold.");
                                        break;
                                    }
                                    StringBuilder statusMessage = new StringBuilder();
                                    statusMessage.AppendLine("Du har følgende forespørsler:");
                                    dbClient.setQuery("SELECT sender_id FROM users_relationships WHERE accepted = '0' AND recipent_id = @myid LIMIT 6");
                                    dbClient.addParameter("myid", Session.GetHabbo().Id);
                                    DataTable table = dbClient.getTable();
                                    foreach (DataRow row in table.Rows)
                                    {
                                        statusMessage.AppendLine(FirewindEnvironment.getHabboForId(Convert.ToUInt32(row[0])).Username);
                                    }
                                    statusMessage.AppendLine("Du kan maks ha 6 forespørsler på en gang.");
                                    statusMessage.AppendLine("Skriv :accept navn for å akseptere en forespørsel.");
                                    Session.SendMOTD(statusMessage.ToString());
                                    return;

                                case "accept":
                                    if (!result)
                                    {
                                        Session.SendMOTD("Du må være medlem av Mafia eller Police for å ha kommandoene til forhold.");
                                        break;
                                    }
                                    dbClient.setQuery("SELECT sender_id FROM users_relationships WHERE (recipent_id = @myid OR sender_id = @myid) AND accepted = '1'");
                                    dbClient.addParameter("myid", Session.GetHabbo().Id);
                                    if (dbClient.findsResult())
                                    {
                                        Session.SendMOTD("Du er allerede i et forhold!");
                                        return;
                                    }
                                    dbClient.setQuery("UPDATE users_relationships SET accepted = '1' WHERE sender_id = @sid AND recipent_id = @myid LIMIT 1");
                                    dbClient.addParameter("myid", Session.GetHabbo().Id);
                                    dbClient.addParameter("sid", FirewindEnvironment.getHabboForName(parsedCommand[1]).Id);
                                    dbClient.runQuery();
                                    Session.SendMOTD("Du er nå i et forhold med " + parsedCommand[1]);
                                    return;

                                case "decline":
                                    if (!result)
                                    {
                                        Session.SendMOTD("Du må være medlem av Mafia eller Police for å ha kommandoene til forhold.");
                                        break;
                                    }
                                    dbClient.setQuery("DELETE FROM users_relationships WHERE sender_id = @sid AND recipent_id = @myid AND accepted = '0' LIMIT 1");
                                    dbClient.addParameter("myid", Session.GetHabbo().Id);
                                    dbClient.addParameter("sid", FirewindEnvironment.getHabboForName(parsedCommand[1]).Id);
                                    dbClient.runQuery();
                                    Session.SendMOTD("Du har avslått " + parsedCommand[1]);
                                    return;

                                case "declineall":
                                    if (!result)
                                    {
                                        Session.SendMOTD("Du må være medlem av Mafia eller Police for å ha kommandoene til forhold.");
                                        break;
                                    }
                                    dbClient.setQuery("DELETE FROM users_relationships WHERE recipent_id = @myid AND accepted = '0' LIMIT 1");
                                    dbClient.addParameter("myid", Session.GetHabbo().Id);
                                    dbClient.runQuery();
                                    Session.SendMOTD("Du har avslått alle.");
                                    return;

                                case "status":
                                    uint userID = FirewindEnvironment.getHabboForName(parsedCommand[1]).Id;
                                    dbClient.setQuery("SELECT sender_id,recipent_id FROM users_relationships WHERE (recipent_id = @userid OR sender_id = @userid) AND accepted = '1' LIMIT 1");
                                    dbClient.addParameter("userid", userID);
                                    DataRow resultRow = dbClient.getRow();

                                    if (resultRow == null)
                                        Session.SendMOTD(parsedCommand[1] + " er singel.");
                                    else
                                    {
                                        bool isSender = Convert.ToUInt32(resultRow[0]) == userID;
                                        Session.SendMOTD(parsedCommand[1] + " er i et forhold med " + (isSender ? FirewindEnvironment.getHabboForId(Convert.ToUInt32(resultRow[1])).Username : FirewindEnvironment.getHabboForId(Convert.ToUInt32(resultRow[0])).Username));
                                    }
                                    return;

                                case "removerelationship":
                                    if (!result)
                                    {
                                        Session.SendMOTD("Du må være medlem av Mafia eller Police for å ha kommandoene til forhold.");
                                        break;
                                    }
                                    dbClient.setQuery("DELETE FROM users_relationships WHERE accepted = '1' AND (recipent_id = @myid OR sender_id = @myid) LIMIT 1");
                                    dbClient.addParameter("myid", Session.GetHabbo().Id);
                                    dbClient.runQuery();
                                    Session.SendMOTD("Du er ikke lenger i noen forhold.");
                                    return;
                            }
                        }
                    }
                }


                uint rank = 1;
                Message = LanguageLocale.FilterSwearwords(Message);
                if (Session != null && Session.GetHabbo() != null)
                    rank = Session.GetHabbo().Rank;
                TimeSpan SinceLastMessage = DateTime.Now - clientUser.spamFloodTime;
                if (SinceLastMessage.TotalSeconds > clientUser.spamProtectionTime && clientUser.spamProtectionBol == true)
                {
                    FloodCount = 0;
                    clientUser.spamProtectionBol = false;
                    clientUser.spamProtectionAbuse = 0;
                }
                else
                {
                    if (SinceLastMessage.TotalSeconds > 4)
                        FloodCount = 0;
                }

                if (SinceLastMessage.TotalSeconds < clientUser.spamProtectionTime && clientUser.spamProtectionBol == true)
                {
                    ServerMessage Packet = new ServerMessage(Outgoing.FloodFilter);
                    int timeToWait = clientUser.spamProtectionTime - SinceLastMessage.Seconds;
                    Packet.AppendInt32(timeToWait); //Blocked for X sec
                    GetClient().SendMessage(Packet);

                    if (FirewindEnvironment.spamBans == true)
                    {
                        clientUser.spamProtectionAbuse++;
                        GameClient toBan;
                        toBan = FirewindEnvironment.GetGame().GetClientManager().GetClientByUsername(Session.GetHabbo().Username);
                        if (clientUser.spamProtectionAbuse >= FirewindEnvironment.spamBans_limit)
                        {
                            FirewindEnvironment.GetGame().GetBanManager().BanUser(toBan, "SPAM*ABUSE", 800, LanguageLocale.GetValue("flood.banmessage"), false);
                        }
                        else
                        {
                            toBan.SendNotif(LanguageLocale.GetValue("flood.pleasewait").Replace("%secs%", Convert.ToString(timeToWait)));
                        }
                    }
                    return;
                }

                if (SinceLastMessage.TotalSeconds < 4 && FloodCount > 5 && rank < 5)
                {
                    ServerMessage Packet = new ServerMessage(Outgoing.FloodFilter);
                    clientUser.spamProtectionCount += 1;
                    if (clientUser.spamProtectionCount % 2 == 0)
                    {
                        clientUser.spamProtectionTime = (10 * clientUser.spamProtectionCount);
                    }
                    else
                    {
                        clientUser.spamProtectionTime = 10 * (clientUser.spamProtectionCount - 1);
                    }
                    clientUser.spamProtectionBol = true;
                    Packet.AppendInt32(clientUser.spamProtectionTime - SinceLastMessage.Seconds); //Blocked for X sec
                    GetClient().SendMessage(Packet);
                    return;
                }

                clientUser.spamFloodTime = DateTime.Now;
                FloodCount++;

                FirewindEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Session, HabboHotel.Quests.QuestType.SOCIAL_CHAT);

                GetClient().GetHabbo().GetChatMessageManager().AddMessage(ChatMessageFactory.CreateMessage(Message, this.GetClient(), this.GetRoom()));
            }

            InvokedChatMessage message = new InvokedChatMessage(this, Message, Shout);
            GetRoom().QueueChatMessage(message);
        }

        internal void OnChat(InvokedChatMessage message)
        {
            string Message = message.message;

            if (GetRoom() != null && !GetRoom().AllowsShous(this, Message))
                return;

            int ChatHeader = Outgoing.Talk;

            if (message.shout)
            {
                ChatHeader = Outgoing.Shout;
            }

            string Site = "";

            ServerMessage ChatMessage = new ServerMessage(ChatHeader);
            ChatMessage.AppendInt32(VirtualId);

            //if (Message.Contains("http://") || Message.Contains("www."))
            //{
            //    string[] Split = Message.Split(' ');

            //    foreach (string Msg in Split)
            //    {
            //        if (Msg.StartsWith("http://") || Msg.StartsWith("www."))
            //        {
            //            Site = Msg;
            //        }
            //    }

            //    Message = Message.Replace(Site, "{0}");
            //}

            ChatMessage.AppendString(Message);

            if (!string.IsNullOrEmpty(Site))
            {
                ChatMessage.AppendBoolean(false);
                ChatMessage.AppendBoolean(true);
                ChatMessage.AppendString(Site.Replace("http://", string.Empty));
                ChatMessage.AppendString(Site);
            }

            ChatMessage.AppendInt32(GetSpeechEmotion(Message));
            ChatMessage.AppendInt32(0);
            ChatMessage.AppendInt32(-1);

            GetRoom().GetRoomUserManager().TurnHeads(X, Y, HabboId);

            foreach (RoomUser user in GetRoom().GetRoomUserManager().GetRoomUsers())
            {
                if (user.GetClient().GetHabbo().MutedUsers.Contains(HabboId))
                    continue;
                user.GetClient().SendMessage(ChatMessage);
            }

            if (!IsBot)
            {
                GetRoom().OnUserSay(this, Message, message.shout);
                LogMessage(Message);
            }

            message.Dispose();
        }

        private static void LogMessage(string message)
        {
        //    ChatMessage chatMessage = ChatMessageFactory.CreateMessage(message, GetClient(), GetRoom());

        //    foreach (RoomUser user in GetRoom().UserList.Values)
        //    {
        //        if (!user.IsBot && user.GetClient() != null && user.GetClient().GetHabbo() != null)
        //            user.GetClient().GetHabbo().GetChatMessageManager().AddMessage(chatMessage);
        //    }

        //    GetRoom().GetChatMessageManager().AddMessage(chatMessage);
        }

        internal static int GetSpeechEmotion(string Message)
        {
            Message = Message.ToLower();

            if (Message.Contains(":)") || Message.Contains(":d") || Message.Contains("=]") || 
                Message.Contains("=d") || Message.Contains(":>"))
            {
                return 1;
            }

            if (Message.Contains(">:(") || Message.Contains(":@"))
            {
                return 2;
            }

            if (Message.Contains(":o"))
            {
                return 3;
            }

            if (Message.Contains(":(") || Message.Contains("=[") || Message.Contains(":'(") || Message.Contains("='["))
            {
                return 4;
            }

            return 0;
        }

        internal void ClearMovement(bool Update)
        {
            IsWalking = false;
            Statusses.Remove("mv");
            GoalX = 0;
            GoalY = 0;
            SetStep = false;
            SetX = 0;
            SetY = 0;
            SetZ = 0;

            if (Update)
            {
                UpdateNeeded = true;
            }
        }

        internal void MoveTo(Point c)
        {
            MoveTo(c.X, c.Y);
        }

        internal void MoveTo(int pX, int pY, bool pOverride)
        {
            if (GetRoom().GetGameMap().SquareHasUsers(pX, pY) && !pOverride)
                return;

            Unidle();

            if (TeleportEnabled)
            {
                GetRoom().SendMessage(GetRoom().GetRoomItemHandler().UpdateUserOnRoller(this, new Point(pX, pY), 0, GetRoom().GetGameMap().SqAbsoluteHeight(GoalX, GoalY)));
                GetRoom().GetRoomUserManager().UpdateUserStatus(this, false);
                return;
            }

            IsWalking = true;
            GoalX = pX;
            GoalY = pY;
            throwBallAtGoal = false;
        }

        internal void MoveTo(int pX, int pY)
        {
            MoveTo(pX, pY, false);
        }

        internal void UnlockWalking()
        {
            this.AllowOverride = false;
            this.CanWalk = true;
        }

        internal void SetPos(int pX, int pY, double pZ)
        {
            this.X = pX;
            this.Y = pY;
            this.Z = pZ;
            if (isFlying)
                Z += 4 + 0.5 * Math.Sin(0.7 * flyk);
        }

        internal void CarryItem(int Item)
        {
            this.CarryItemID = Item;

            if (Item > 0)
            {
                this.CarryTimer = 240;
            }
            else
            {
                this.CarryTimer = 0;
            }

            ServerMessage Message = new ServerMessage(Outgoing.ApplyCarryItem);
            Message.AppendInt32(VirtualId);
            Message.AppendInt32(Item);
            GetRoom().SendMessage(Message);
        }


        internal void SetRot(int Rotation)
        {
            SetRot(Rotation, false); //**************
        }

        internal void SetRot(int Rotation, bool HeadOnly)
        {
            if (Statusses.ContainsKey("lay") || IsWalking)
            {
                return;
            }

            int diff = this.RotBody - Rotation;

            this.RotHead = this.RotBody;

            if (Statusses.ContainsKey("sit") || HeadOnly)
            {
                if (RotBody == 2 || RotBody == 4)
                {
                    if (diff > 0)
                    {
                        RotHead = RotBody - 1;
                    }
                    else if (diff < 0)
                    {
                        RotHead = RotBody + 1;
                    }
                }
                else if (RotBody == 0 || RotBody == 6)
                {
                    if (diff > 0)
                    {
                        RotHead = RotBody - 1;
                    }
                    else if (diff < 0)
                    {
                        RotHead = RotBody + 1;
                    }
                }
            }
            else if (diff <= -2 || diff >= 2)
            {
                this.RotHead = Rotation;
                this.RotBody = Rotation;
            }
            else
            {
                this.RotHead = Rotation;
            }

            this.UpdateNeeded = true;
        }

        internal void AddStatus(string Key, string Value)
        {
            Statusses[Key] = Value;
        }

        internal void RemoveStatus(string Key)
        {
            if (Statusses.ContainsKey(Key))
            {
                Statusses.Remove(Key);
            }
        }

        internal void ApplyEffect(int effectID)
        {
            if (IsBot || GetClient() == null || GetClient().GetHabbo() == null || GetClient().GetHabbo().GetAvatarEffectsInventoryComponent() == null)
                return;

            GetClient().GetHabbo().GetAvatarEffectsInventoryComponent().ApplyCustomEffect(effectID);
        }

        //internal void ResetStatus()
        //{
        //    Statusses = new Dictionary<string, string>();
        //}

        internal void Serialize(ServerMessage Message)
        {
            // @\Ihqu@UMeth0d13haiihr-893-45.hd-180-8.ch-875-62.lg-280-62.sh-290-62.ca-1813-.he-1601-[IMRAPD4.0JImMcIrDK
            // MSadiePull up a pew and have a brew!hr-500-45.hd-600-1.ch-823-75.lg-716-76.sh-730-62.he-1602-75IRBPA2.0PAK

            if (Message == null)
                return;

            if (IsSpectator)
                return;

            if (IsBot)
            {
                // monster plant:         2   1 3 8    0 -1 9
                // no saddle:             2   2 -1 1   3 -1 1
                // with saddle (cool):    3   2 -1 1   3 -1 1   4 10 0
                // with saddle (normal):  3   2 -1 1   3 -1 1   4  9 0
                // // typeId + paletteId + color + unkcount + 3 int
                // hair -1 hairid
                // tail -1 tailid
                //string[,] data = new string[,] { [2, };

                Message.AppendInt32(BotAI.BaseId);
                Message.AppendString(BotData.Name);
                Message.AppendString(BotData.Motto);
                Message.AppendString(BotData.Look.ToLower() + ((PetData.HaveSaddle) ? "3 2 -1 1 3 -1 1 4 9 0" : "2 2 -1 1 3 -1 1"));
                Message.AppendInt32(VirtualId);
                Message.AppendInt32(X);
                Message.AppendInt32(Y);
                Message.AppendString(TextHandling.GetString(Z));
                Message.AppendInt32(0);
                Message.AppendInt32((BotData.AiType == AIType.Pet) ? 2 : 3);
                if (BotData.AiType == AIType.Pet)
                {
                    Message.AppendUInt(PetData.Type);
                    Message.AppendUInt(PetData.OwnerId); // userid
                    Message.AppendString(PetData.OwnerName); // username
                    Message.AppendInt32(1);


                    Message.AppendBoolean(PetData.HaveSaddle);
                    
                    
                    Message.AppendBoolean(isMounted);
                    Message.AppendInt32(0);
                    Message.AppendInt32(0);
                    Message.AppendString("");
                }
            }
            else if (!IsBot && GetClient() != null && GetClient().GetHabbo() != null)
            {
                Users.Habbo User = GetClient().GetHabbo();
                Message.AppendUInt(User.Id);
                Message.AppendString(User.Username);
                Message.AppendString(User.Motto);
                Message.AppendString(User.Look);
                Message.AppendInt32(VirtualId);
                Message.AppendInt32(X);
                Message.AppendInt32(Y);
                Message.AppendString(TextHandling.GetString(Z));
                Message.AppendInt32(0);
                Message.AppendInt32(1);
                Message.AppendString(User.Gender.ToLower());

                Group favouriteGroup = null;
                if (User.FavouriteGroup > 0 && FirewindEnvironment.GetGame().GetGroupManager() != null)
                    favouriteGroup = FirewindEnvironment.GetGame().GetGroupManager().GetGroup(User.FavouriteGroup);

                Message.AppendInt32(favouriteGroup != null ? favouriteGroup.ID : 0); // group ID
                Message.AppendInt32(0); // Looks like unused
                Message.AppendString(favouriteGroup != null ? favouriteGroup.Name : ""); // group name

                Message.AppendString(""); // botFigure
                Message.AppendInt32(User.AchievementPoints);
            }
        }

        internal void SerializeStatus(ServerMessage Message)
        {
            if (IsSpectator)
            {
                return;
            }

            Message.AppendInt32(VirtualId);
            Message.AppendInt32(X);
            Message.AppendInt32(Y);
            Message.AppendString(TextHandling.GetString(Z));
            Message.AppendInt32(RotHead);
            Message.AppendInt32(RotBody);
            StringBuilder StatusComposer = new StringBuilder();
            StatusComposer.Append("/");

            foreach (KeyValuePair<string, string> Status in Statusses)
            {
                StatusComposer.Append(Status.Key);

                if (Status.Value != string.Empty)
                {
                    StatusComposer.Append(" ");
                    StatusComposer.Append(Status.Value);
                }

                StatusComposer.Append("/");
            }

            StatusComposer.Append("/");
            Message.AppendString(StatusComposer.ToString());

            RemoveStatus("sign"); // fix for infinitive signs
        }

        internal void SerializeStatus(ServerMessage Message, String Status)
        {
            if (IsSpectator)
            {
                return;
            }

            Message.AppendInt32(VirtualId);
            Message.AppendInt32(X);
            Message.AppendInt32(Y);
            Message.AppendString(TextHandling.GetString(Z));
            Message.AppendInt32(RotHead);
            Message.AppendInt32(RotBody);
            StringBuilder StatusComposer = new StringBuilder();
            Message.AppendString(Status);
        }

        private GameClient mClient;
        internal GameClient GetClient()
        {
            if (IsBot)
            {
                return null;
            }
            if (mClient == null)
                mClient = FirewindEnvironment.GetGame().GetClientManager().GetClientByUserID(HabboId);
            return mClient;
        }

        private Room mRoom;
        private Room GetRoom()
        {
            if (mRoom == null)
                mRoom = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(RoomId);
            return mRoom;
        }
    }

    internal enum ItemEffectType
    {
        None,
        Swim,
        SwimLow,
        SwimHalloween,
        Iceskates,
        Normalskates,
        PublicPool
        //Skateboard?
    }

    internal static class ByteToItemEffectEnum
    {
        internal static ItemEffectType Parse(byte pByte)
        {
            switch (pByte)
            {
                case 0:
                    return ItemEffectType.None;
                case 1:
                    return ItemEffectType.Swim;
                case 2:
                    return ItemEffectType.Normalskates;
                case 3:
                    return ItemEffectType.Iceskates;
                case 4:
                    return ItemEffectType.SwimLow;
                case 5:
                    return ItemEffectType.SwimHalloween;
                case 6:
                    return ItemEffectType.PublicPool;
                default:
                    return ItemEffectType.None;
            }
        }
    }
    //0 = none
    //1 = pool
    //2 = normal skates
    //3 = ice skates
}
