using System;
using System.Collections;
using System.IO;
using System.Text;
using ConsoleWriter;

namespace Firewind.Core
{
    public static class Logging
    {
        internal static bool DisabledState
        {
            get
            {
                return Writer.DisabledState;
            }
            set
            {
                Writer.DisabledState = value;
            }
        }

        internal static void WriteWithColor(string Line, ConsoleColor color)
        {
            if (DisabledState)
                return; 

            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Firewind");
            Console.ResetColor();

            Console.Write("] » ");

            Console.ForegroundColor = color;

            Console.Write(Line);
            Console.Write("\n");

            Console.ResetColor();
        }

        internal static void WriteLine(string Line)
        {
            Console.WriteLine(Line);
        }

        internal static void WriteLine(string Line, bool Debug)
        {
            Logging.WriteText(Line, Debug);
        }


        internal static void WriteText(string Line, Boolean debug)
        {
            if (DisabledState)
                return; 

            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Firewind");
            Console.ResetColor();

            Console.Write("] » ");

            if (debug)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }

            Console.Write(Line);
            Console.Write("\n");

            Console.ResetColor();
        }

        internal static void LogException(string logText)
        {
            Writer.LogException(logText + Environment.NewLine);
        }

        internal static void LogCriticalException(string logText)
        {
            Writer.LogCriticalException(logText);
        }

        internal static void LogCacheError(string logText)
        {
            Writer.LogCacheError(logText);
        }

        internal static void LogMessage(string logText)
        {
            Writer.LogMessage(logText);
        }

        internal static void LogDebug(string logText)
        {
            Writer.LogDebug(logText);
        }


        internal static void LogThreadException(string Exception, string Threadname)
        {
            Writer.LogThreadException(Exception, Threadname);
        }

        public static void LogQueryError(Exception Exception, string query)
        {
            Writer.LogQueryError(Exception, query);
        }

        internal static void LogPacketException(string Packet, string Exception)
        {
            //SessionManagement.BroadcastExceptionNotification(Firewind.Core.ExceptionType.UserException, tokenID++);
            Writer.LogPacketException(Packet, Exception);
            //SessionManagement.IncreaseError();
        }

        internal static void HandleException(Exception pException, string pLocation)
        {
            Writer.HandleException(pException, pLocation);
        }

        internal static void DisablePrimaryWriting(bool ClearConsole)
        {
            Writer.DisablePrimaryWriting(ClearConsole);
        }

        internal static void LogShutdown(StringBuilder builder)
        {
            Writer.LogShutdown(builder);
        }
    }
}
