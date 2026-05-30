using Firewind.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Firewind.HabboHotel.Rooms
{
    // Used for guild furniture
    internal class StringArrayStuffData : IRoomItemData
    {
        // 2 = (StringArrayStuffData) - int i, foreach i { string }
        internal List<string> Data;

        internal StringArrayStuffData()
        {
            Data = new List<string>();
        }

        public int GetTypeID()
        {
            return 2;
        }

        public void Parse(string rawData)
        {
            Data = rawData.Split(Convert.ToChar(1)).ToList<string>();
        }

        public void AppendToMessage(ServerMessage message)
        {
            message.AppendInt32(Data.Count);
            foreach (string value in Data)
                message.AppendString(value);
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (string value in Data)
            {
                builder.Append(Convert.ToChar(1));
                builder.Append(value);
            }

            return builder.Length > 0 ? builder.ToString().Substring(1) : "";
        }

        public object GetData()
        {
            return Data;
        }
    }
}
