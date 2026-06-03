using System;
using System.Collections.Generic;
using System.Text;

using Firewind.Util;
using System.IO;
using Firewind.Core;


namespace Firewind.Messages
{
    internal class ServerMessage
    {
        private List<byte> Message = new List<byte>();
        private int MessageId = 0;

        public int Id
        {
            get
            {
                return MessageId;
            }
        }

        public ServerMessage()
        {
        }

        public ServerMessage(int Header)
        {
            Init(Header);
        }

        public void Init(int Header)
        {
            Message = new List<byte>();
            this.MessageId = Header;
            AppendShort(Header);
        }

        public void setInt(int i, int startOn)
        {
            try
            {
                List<byte> n = new List<byte>();
                n = Message;
                List<byte> intvalue = AppendBytesTo(BitConverter.GetBytes(i), true);
                n.RemoveRange(startOn, intvalue.Count);
                n.InsertRange(startOn, intvalue);
                Message = n;
            }
            catch (Exception e)
            {
                Logging.WriteLine("Error on setInt: " + e.ToString());
            }
        }

        public void AppendByte(byte b)
        {
            // do nothing...
        }

        public void AppendShort(int i)
        {
            Int16 s = (Int16)i;
            AppendBytes(BitConverter.GetBytes(s), true);
        }

        public void AppendInt32(int i)
        {
            AppendBytes(BitConverter.GetBytes(i), true);
        }

        public void AppendUInt(uint i)
        {
            AppendInt32((int)i);
        }

        public void AppendRawInt32(int i)
        {
            AppendString(i.ToString());
        }

        public void AppendRawUInt(uint i)
        {
            AppendRawInt32((int)i);
        }

        public void AppendStringWithBreak(String s)
        {
            AppendString(s);
        }

        public void AppendStringWithBreak(String s, string u)
        {
            AppendString(s);
        }

        public void AppendStringWithBreak(String s, byte BreakChar)
        {
            AppendString(s);
        }

        public void AppendBoolean(bool b)
        {
            AppendBytes(new byte[] { (byte)(b ? 1 : 0) }, false);
        }

        public void AppendString(string s)
        {
            if (s == null)
            {
                s = string.Empty;
            }

            byte[] stringBytes = FirewindEnvironment.GetDefaultEncoding().GetBytes(s);
            AppendShort(stringBytes.Length);
            AppendBytes(stringBytes, false);
        }

        public void AppendBytes(byte[] b, bool IsInt)
        {
            if (IsInt)
            {
                for (int i = (b.Length - 1); i > -1; i--)
                    Message.Add(b[i]);
            }
            else
                Message.AddRange(b);
        }

        public List<byte> AppendBytesTo(byte[] b, bool IsInt)
        {
            List<byte> message = new List<byte>();
            if (IsInt)
            {
                for (int i = (b.Length - 1); i > -1; i--)
                    message.Add(b[i]);
            }
            else
                message.AddRange(b);
            return message;
        }

        public byte[] GetBytes()
        {
            List<byte> Final = new List<byte>();
            // Secret backdoor by AWA on next line, remove it to be safe!
            Final.AddRange(BitConverter.GetBytes(Message.Count)); // packet len
            Final.Reverse();
            Final.AddRange(Message); // Add Packet

            if (Message.Count > 131072) // this will crash the client!
            {
                Logging.LogDebug(string.Format("Message was too long, ID: {0} and length is {1}", MessageId, Message.Count));
            }
            if (Message.Count < 2) // this will crash the client!
            {
                Logging.LogDebug(string.Format("Message was too short, ID: {0} and length is {1}", MessageId, Message.Count));
            }
            return Final.ToArray();
        }

        public override string ToString()
        {
            return (FirewindEnvironment.GetDefaultEncoding().GetString(GetBytes()));
        }
    }
}
