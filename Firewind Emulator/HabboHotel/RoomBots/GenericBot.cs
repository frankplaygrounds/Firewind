using System;
using Firewind.Core;
using Firewind.HabboHotel.Pathfinding;
using Firewind.HabboHotel.Rooms;
using Firewind.HabboHotel.Catalogs;
using Firewind.Messages;
using System.Drawing;
using HabboEvents;

namespace Firewind.HabboHotel.RoomBots
{
    class GenericBot : BotAI
    {
        private int SpeechTimer;
        private int ActionTimer;

        internal GenericBot(int VirtualId)
        {
            this.SpeechTimer = new Random((VirtualId ^ 2) + DateTime.Now.Millisecond).Next(10, 250);
            this.ActionTimer = new Random((VirtualId ^ 2) + DateTime.Now.Millisecond).Next(10, 30);
        }

        internal override void OnSelfEnterRoom()
        {
            
        }

        internal override void OnSelfLeaveRoom(bool Kicked)
        {
            
        }

        internal override void OnUserEnterRoom(Rooms.RoomUser User)
        {
            
        }

        internal override void OnUserLeaveRoom(GameClients.GameClient Client)
        {
            
        }

        internal override void OnUserSay(Rooms.RoomUser User, string Message)
        {
            if (Gamemap.TileDistance(GetRoomUser().X, GetRoomUser().Y, User.X, User.Y) > 8)
            {
                return;
            }

            BotResponse Response = GetBotData().GetResponse(Message);

            if (Response == null)
            {
                return;
            }

            switch (Response.ResponseType.ToLower())
            {
                case "say":

                    GetRoomUser().Chat(null, Response.ResponseText, false);
                    break;

                case "shout":

                    GetRoomUser().Chat(null, Response.ResponseText, true);
                    break;

                case "whisper":

                    ServerMessage TellMsg = new ServerMessage(Outgoing.Whisp);
                    TellMsg.AppendInt32(GetRoomUser().VirtualId);
                    TellMsg.AppendString(Response.ResponseText);
                    TellMsg.AppendInt32(0);
                    TellMsg.AppendInt32(0);
                    TellMsg.AppendInt32(-1);

                    User.GetClient().SendMessage(TellMsg);
                    break;
            }

            if (Response.ServeId >= 1)
            {
                User.CarryItem(Response.ServeId);
            }
        }

        internal override void OnUserShout(Rooms.RoomUser User, string Message)
        {
            if (FirewindEnvironment.GetRandomNumber(0, 10) >= 5)
            {
                GetRoomUser().Chat(null, LanguageLocale.GetValue("onusershout"), true); // shout nag
            }
        }

        internal override void OnTimerTick()
        {
            RoomBot botData = GetBotData();
            if (botData == null)
                return;
            if (botData.IsExpired)
            {
                Catalog.DeleteUserBot(botData.BotId);
                GetRoom().GetRoomUserManager().RemoveBot(GetRoomUser().VirtualId, false);
                return;
            }

            if (SpeechTimer <= 0)
            {
                if (botData.RandomSpeech.Count > 0)
                {
                    RandomSpeech Speech = botData.GetRandomSpeech();
                    GetRoomUser().Chat(null, Speech.Message, Speech.Shout);
                }

                SpeechTimer = FirewindEnvironment.GetRandomNumber(10, 300);
            }
            else
            {
                SpeechTimer--;
            }

            if (ActionTimer <= 0)
            {
                switch (botData.WalkingMode.ToLower())
                {
                    default:
                    case "stand":

                        // (8) Why is my life so boring?

                        break;

                    case "freeroam":
                        Point nextCoord = GetRoom().GetGameMap().getRandomWalkableSquare();
                        GetRoomUser().MoveTo(nextCoord.X, nextCoord.Y);
                        
                        break;

                    case "specified_range":
                        Point nextCoord2 = GetRoom().GetGameMap().getRandomWalkableSquare();
                        GetRoomUser().MoveTo(nextCoord2.X, nextCoord2.Y);
                        
                        break;
                }

                ActionTimer = FirewindEnvironment.GetRandomNumber(1, 30);
            }
            else
            {
                ActionTimer--;
            }
        }
    }
}
