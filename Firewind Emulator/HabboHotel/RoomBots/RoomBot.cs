using System;
using System.Collections.Generic;
using Firewind.Messages;


namespace Firewind.HabboHotel.RoomBots
{
    class RoomBot
    {
        internal uint BotId;
        internal UInt32 RoomId;

        internal AIType AiType;
        internal string WalkingMode;

        internal string Name;
        internal string Motto;
        internal string Look;
        internal uint OwnerId;
        internal string OwnerName;
        internal string Gender;
        internal int ExpireTimestamp;
        internal int DanceId;
        internal bool ChatAuto;
        internal bool ChatRandom;
        internal int ChatDelay;
        internal int ChatTimeOut;
        internal int LastChatIndex;
        internal List<string> ChatLines;

        internal int X;
        internal int Y;
        internal int Z;
        internal int Rot;

        internal int minX;
        internal int maxX;
        internal int minY;
        internal int maxY;

        internal List<RandomSpeech> RandomSpeech;
        internal List<BotResponse> Responses;

        internal bool IsPet
        {
            get
            {
                return (this.AiType == AIType.Pet);
            }
        }

        internal bool IsRentable
        {
            get
            {
                return (this.AiType == AIType.Rentable);
            }
        }

        internal bool IsExpired
        {
            get
            {
                return IsRentable && ExpireTimestamp > 0 && ExpireTimestamp <= FirewindEnvironment.GetUnixTimestamp();
            }
        }

        internal int RentableSecondsLeft
        {
            get
            {
                if (!IsRentable || ExpireTimestamp <= 0)
                    return 0;

                return Math.Max(0, ExpireTimestamp - FirewindEnvironment.GetUnixTimestamp());
            }
        }

        internal RoomBot(uint BotId, UInt32 RoomId, AIType AiType, string WalkingMode, string Name, string Motto, string Look,
            int X, int Y, int Z, int Rot, int minX, int minY, int maxX, int maxY, ref List<RandomSpeech> Speeches, ref List<BotResponse> Responses, uint OwnerId = 0, string Gender = "M", string OwnerName = "", int ExpireTimestamp = 0, int DanceId = 0, bool ChatAuto = false, bool ChatRandom = false, int ChatDelay = 7, List<string> ChatLines = null)
        {
            this.BotId = BotId;
            this.RoomId = RoomId;
            this.AiType = AiType;
            this.WalkingMode = WalkingMode;
            this.Name = Name;
            this.Motto = Motto;
            this.Look = Look;
            this.OwnerId = OwnerId;
            this.OwnerName = OwnerName;
            this.Gender = string.IsNullOrEmpty(Gender) ? "M" : Gender;
            this.ExpireTimestamp = ExpireTimestamp;
            this.DanceId = Math.Max(0, Math.Min(4, DanceId));
            this.ChatAuto = ChatAuto;
            this.ChatRandom = ChatRandom;
            this.ChatDelay = Math.Max(1, ChatDelay);
            this.ChatTimeOut = 0;
            this.LastChatIndex = -1;
            this.ChatLines = ChatLines ?? new List<string>();
            this.X = X;
            this.Y = Y;
            this.Z = Z;
            this.Rot = Rot;
            this.minX = minX;
            this.minY = minY;
            this.maxX = maxX;
            this.maxY = maxY;

            LoadRandomSpeech(Speeches);
            LoadResponses(Responses);
        }

        internal void SerializeInventory(ServerMessage Message)
        {
            Message.AppendUInt(BotId);
            Message.AppendString(Name);
            Message.AppendString(Gender.ToUpper());
            Message.AppendString(Look);
        }

        internal void LoadRandomSpeech(List<RandomSpeech> Speeches)
        {
            this.RandomSpeech = new List<RandomSpeech>();

            foreach (RandomSpeech Speech in Speeches)
            {
                if (Speech.BotID == BotId)
                    RandomSpeech.Add(Speech);
            }
        }

        internal void LoadResponses(List<BotResponse> Respons)
        {
            this.Responses = new List<BotResponse>();

            foreach (BotResponse Resp in Respons)
            {
                if (Resp.BotId == BotId)
                    Responses.Add(Resp);
            }
        }

        internal BotResponse GetResponse(string Message)
        {
            foreach (BotResponse Response in Responses)
            {
                if (Response.KeywordMatched(Message))
                {
                    return Response;
                }
            }

            return null;
        }

        internal RandomSpeech GetRandomSpeech()
        {
            return RandomSpeech[FirewindEnvironment.GetRandomNumber(0, (RandomSpeech.Count - 1))];
        }

        internal string GetNextChatLine()
        {
            if (ChatLines == null || ChatLines.Count == 0)
                return string.Empty;

            if (ChatRandom)
                return ChatLines[FirewindEnvironment.GetRandomNumber(0, ChatLines.Count - 1)];

            LastChatIndex++;
            if (LastChatIndex >= ChatLines.Count)
                LastChatIndex = 0;

            return ChatLines[LastChatIndex];
        }

        internal BotAI GenerateBotAI(int VirtualId)
        {
            switch (AiType)
            {
                default:
                case AIType.Generic:
                case AIType.Rentable:
                    return new GenericBot(VirtualId);
                case AIType.Guide:
                    return new GuideBot();
                case AIType.Pet:
                    return new PetBot(VirtualId);
            }
        }
    }

    internal enum AIType
    {
        Pet,
        Guide,
        Generic,
        Rentable
    }
}
