using System;
using System.Collections.Generic;
using Firewind.Core;
using Firewind.HabboHotel.Misc;
using Firewind.HabboHotel.Pathfinding;
using Firewind.HabboHotel.Users;
using Firewind.HabboHotel.Users.UserDataManagement;
using Firewind.HabboHotel.Users.Competitions;
using Firewind.Messages;
using Firewind.Net;
using Firewind.Util;
using ConnectionManager;
using System.Drawing;

using HabboEvents;
using System.Threading;
using System.Threading.Tasks;
using Firewind.HabboHotel.Rooms;
using Database_Manager.Database.Session_Details.Interfaces;
namespace Firewind.HabboHotel.GameClients
{
    public class GameClient
    {
        private uint Id;

        private ConnectionInformation Connection;
        private GameClientMessageHandler MessageHandler;

        private Habbo Habbo;

        internal DateTime TimePingedReceived;

        internal bool SetDoorPos;
        internal Point newDoorPos = default(Point);
        internal  GamePacketParser packetParser;


        internal uint ConnectionID
        {
            get
            {
                return Id;
            }
        }

        internal int CurrentRoomUserID;
        internal string MachineId;

        internal GameClient(uint ClientId, ConnectionInformation pConnection)
        {
            Id = ClientId;
            Connection = pConnection;
            SetDoorPos = false;
            CurrentRoomUserID = -1;
            packetParser = new GamePacketParser(this);
        }


        void SwitchParserRequest()
        {
            if (MessageHandler == null)
            {
                InitHandler();
            }
            packetParser.SetConnection(Connection);
            packetParser.onNewPacket += new GamePacketParser.HandlePacket(parser_onNewPacket);
            byte[] data = (Connection.parser as InitialPacketParser).currentData;
            Connection.parser.Dispose();
            Connection.parser = packetParser;
            Connection.parser.handlePacketData(data);
        }

        void parser_onNewPacket(ClientMessage Message)
        {
            try
            {
                MessageHandler.HandleRequest(Message);
            }
            catch (Exception e) { Logging.LogPacketException(Message.ToString(), e.ToString()); }
        }

        void PolicyRequest()
        {
            Connection.SendData(FirewindEnvironment.GetDefaultEncoding().GetBytes(CrossdomainPolicy.GetXmlPolicy()));
        }

        internal ConnectionInformation GetConnection()
        {
            return Connection;
        }

        internal GameClientMessageHandler GetMessageHandler()
        {
            return MessageHandler;
        }

        internal bool gotTheThing = false;

        internal Habbo GetHabbo()
        {
            return Habbo;
        }

        internal void StartConnection()
        {
            if (Connection == null)
            {
                return;
            }

            TimePingedReceived = DateTime.Now;

            (Connection.parser as InitialPacketParser).PolicyRequest += new InitialPacketParser.NoParamDelegate(PolicyRequest);
            (Connection.parser as InitialPacketParser).SwitchParserRequest += new InitialPacketParser.NoParamDelegate(SwitchParserRequest);

            Connection.startPacketProcessing();
        }

        internal void InitHandler()
        {
            MessageHandler = new GameClientMessageHandler(this);
        }

        internal bool tryLogin(string AuthTicket)
        {
            int loginProgress = 0;
            try
            {
                string ip = GetConnection().getIp();
                byte errorCode = 0;
                UserData userData = UserDataFactory.GetUserData(AuthTicket, ip, out errorCode);
                if (errorCode == 1)
                {
                    SendNotifWithScroll(LanguageLocale.GetValue("login.invalidsso"));
                    return false;
                }
                else if (errorCode == 2)
                {
                    SendNotifWithScroll(LanguageLocale.GetValue("login.loggedin"));
                    return false;
                }
                loginProgress++;


                FirewindEnvironment.GetGame().GetClientManager().RegisterClient(this, userData.userID, userData.user.Username);
                this.Habbo = userData.user;
                userData.user.LoadData(userData);
                loginProgress++;

                if (userData.user.Username == null)
                {
                    SendBanMessage("You have no username.");
                    return false;
                }
                string banReason = FirewindEnvironment.GetGame().GetBanManager().GetBanReason(userData.user.Username, ip);
                loginProgress++;
                if (!string.IsNullOrEmpty(banReason))
                {
                    SendBanMessage(banReason);
                    return false;
                }

                userData.user.Init(this, userData);

                QueuedServerMessage response = new QueuedServerMessage(Connection);

                ServerMessage UniqueId = new ServerMessage(Outgoing.UniqueID);
                UniqueId.AppendString(this.MachineId);
                response.appendResponse(UniqueId);

                ServerMessage authok = new ServerMessage(Outgoing.AuthenticationOK);
                response.appendResponse(authok);

                ServerMessage HomeRoom = new ServerMessage(Outgoing.HomeRoom);
                HomeRoom.AppendUInt(this.GetHabbo().HomeRoom); // first home
                HomeRoom.AppendUInt(this.GetHabbo().HomeRoom); // first home
                SendMessage(HomeRoom);

                loginProgress++;

                ServerMessage FavouriteRooms = new ServerMessage(Outgoing.FavouriteRooms);
                FavouriteRooms.AppendInt32(30); // max rooms
                FavouriteRooms.AppendInt32(userData.user.FavoriteRooms.Count);
                foreach (uint Id in userData.user.FavoriteRooms.ToArray())
                {
                    FavouriteRooms.AppendUInt(Id);
                }
                response.appendResponse(FavouriteRooms);

                loginProgress++;

                ServerMessage fuserights = new ServerMessage(Outgoing.Fuserights);
                if (GetHabbo().GetSubscriptionManager().HasSubscription("habbo_vip")) // VIP 
                    fuserights.AppendInt32(2);
                else if (GetHabbo().GetSubscriptionManager().HasSubscription("habbo_club")) // HC
                    fuserights.AppendInt32(1);
                else
                    fuserights.AppendInt32(0);
                fuserights.AppendUInt(this.GetHabbo().Rank);
                response.appendResponse(fuserights);

                loginProgress++;

                ServerMessage bools1 = new ServerMessage(Outgoing.AvailabilityStatus);
                bools1.AppendBoolean(true);
                bools1.AppendBoolean(false);
                response.appendResponse(bools1);

                ServerMessage bools2 = new ServerMessage(Outgoing.InfoFeedEnable);
                bools2.AppendBoolean(false);
                response.appendResponse(bools2);

                loginProgress++;

                ServerMessage setRanking = new ServerMessage(Outgoing.SerializeCompetitionWinners);
                setRanking.AppendString("hlatCompetitions"); // competition type
                setRanking.AppendInt32(Ranking.getCompetitionForInfo("hlatCompetitions").Count);

                loginProgress++;
                int i = 0;
                foreach (Ranking r in Ranking.getCompetitionForInfo("hlatCompetitions"))
                {
                    i++;
                    setRanking.AppendUInt(r.UserId);
                    Habbo data = FirewindEnvironment.getHabboForId(r.UserId);
                    setRanking.AppendString((data != null) ? data.Username : "Desconocido");
                    setRanking.AppendString((data != null) ? data.Look : "sh-907-96.hd-3096-3.he-3082-91.lg-3018-81.ch-660-95.hr-9534-34");
                    setRanking.AppendInt32(i); // position
                    setRanking.AppendInt32(r.Score);
                }
                response.appendResponse(setRanking);

                loginProgress++;

                if (userData.user.HasFuse("fuse_mod"))
                {
                    this.SendMessage(FirewindEnvironment.GetGame().GetModerationTool().SerializeTool());
                    FirewindEnvironment.GetGame().GetModerationTool().SerializeOpenTickets(ref response, userData.userID);
                }

                if (LanguageLocale.welcomeAlertEnabled)
                {
                    this.SendMOTD(LanguageLocale.welcomeAlert);
                }

                loginProgress++;
                using (IQueryAdapter db = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                {
                    db.setQuery("UPDATE users SET online = '1' WHERE id = @id");
                    db.addParameter("id", this.GetHabbo().Id);
                    db.runQuery();
                }

                loginProgress++;

                // Send the big chunk
                response.sendResponse();

                // fire the event!
                FirewindEnvironment.GetGame().GetClientManager().ClientLoggedIn(this);
                return true;

            }
            catch (UserDataNotFoundException e)
            {
                SendNotifWithScroll(LanguageLocale.GetValue("login.invalidsso") + "extra data: " + e.ToString());
            }
            catch (Exception e)
            {
                Logging.LogCriticalException(String.Format("Invalid Dario bug duing user login (progress = {0}): ", loginProgress) + e.ToString());
                SendNotifWithScroll("Login error: " + e.ToString());
            }
            return false;
        }
        internal void SendNotifWithScroll(string message)
        {
            ServerMessage notif = new ServerMessage(Outgoing.SendNotif);
            notif.AppendString(message);
            notif.AppendString(""); // link
            SendMessage(notif);
        }

        internal void SendBanMessage(string Message)
        {
            ServerMessage BanMessage = new ServerMessage(Outgoing.DisconnectReason);
            //BanMessage.AppendStringWithBreak(LanguageLocale.GetValue("moderation.banmessage"), 13);
            //BanMessage.AppendString(Message);
            BanMessage.AppendInt32(10);
            GetConnection().SendData(BanMessage.GetBytes());
        }

        internal void SendNotif(string Message)
        {
            SendNotif(Message, false);
        }

        internal void SendBroadcastMessage(string Message)
        {
            ServerMessage notif = new ServerMessage(Outgoing.BroadcastMessage);
            notif.AppendString(Message);
            notif.AppendString(""); // link
            SendMessage(notif);
        }
        internal void SendMOTD(string[] messages)
        {
            ServerMessage notif = new ServerMessage(Outgoing.MOTDNotification);

            notif.AppendInt32(messages.Length);
            foreach(string message in messages)
               notif.AppendString(message);

            SendMessage(notif);
        }
        internal void SendMOTD(string message)
        {
            SendMOTD(new[] { message });
        }

        internal void SendNotif(string Message, Boolean FromHotelManager)
        {
            ServerMessage notif = new ServerMessage(Outgoing.SendNotif);
            notif.AppendString(Message);
            notif.AppendString(""); // link
            SendMessage(notif);
        }

        internal void Stop()
        {
            if (GetMessageHandler() != null)
                MessageHandler.Destroy();

            if (GetHabbo() != null)
                Habbo.OnDisconnect();
            CurrentRoomUserID = -1;

            this.MessageHandler = null;
            this.Habbo = null;
            this.Connection = null;
        }

        private bool Disconnected = false;

        internal void Disconnect()
        {
            if (GetHabbo() != null && GetHabbo().GetInventoryComponent() != null)
            {
                using (IQueryAdapter db = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                {
                    db.setQuery("UPDATE users SET online = '0' WHERE id = @id");
                    db.addParameter("id", this.GetHabbo().Id);
                    db.runQuery();
                }

                GetHabbo().GetInventoryComponent().RunDBUpdate();
            }
            if (!Disconnected)
            {
                if (Connection != null)
                    Connection.Dispose();
                Disconnected = true;
            }
        }

        internal void SendMessage(ServerMessage Message)
        {
            if (Message == null)
                return;
            if (GetConnection() == null)
                return;
            GetConnection().SendData(Message.GetBytes());
        }

        internal void UnsafeSendMessage(ServerMessage Message)
        {
            if (Message == null)
                return;
            if (GetConnection() == null)
                return;
            GetConnection().SendUnsafeData(Message.GetBytes());
        }
    }
}
