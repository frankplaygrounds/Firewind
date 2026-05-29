using System;
using System.Threading;
using Firewind.Core;
using Firewind.Net;
using System.Text;
using Database_Manager.Database.Session_Details.Interfaces;

namespace Firewind.HabboHotel.Misc
{
    internal class LowPriorityWorker
    {
        private static int _runFrequency;
        private static int UserPeak;


        internal static void Init(IQueryAdapter dbClient)
        {
            dbClient.setQuery("SELECT userpeak FROM server_status");
            UserPeak = dbClient.getInteger();

            _runFrequency = int.Parse(FirewindEnvironment.GetConfig().GetEntry("backgroundworker.interval", "10000")); // leon is crazy, 300!?! (THIS IS MADNESS!!)
        }

        private static DateTime processLastExecution;
        internal static void Process()
        {
            try
            {
                DateTime startTaskTime;
                TimeSpan spentTime;
                startTaskTime = DateTime.Now;

                TimeSpan sinceLastTime = DateTime.Now - processLastExecution;

                if (sinceLastTime.TotalMilliseconds >= _runFrequency)
                {
                    processLastExecution = DateTime.Now;
                    try
                    {

                        int Status = 1;
                        int UsersOnline = FirewindEnvironment.GetGame().GetClientManager().ClientCount;
                        int RoomsLoaded = FirewindEnvironment.GetGame().GetRoomManager().LoadedRoomsCount;

                        TimeSpan Uptime = DateTime.Now - FirewindEnvironment.ServerStarted;
                        string addOn = string.Empty;
                        if (System.Diagnostics.Debugger.IsAttached)
                            addOn = "[DEBUG] ";
                        Console.Title = addOn + "Firewind | Uptime: " + Uptime.Minutes + " minutes, " + Uptime.Hours + " hours and " + Uptime.Days + " day(s) | " +
                            "Online users: " + UsersOnline + " | Loaded rooms: " + RoomsLoaded;

                        #region Statistics
                        if (UsersOnline > UserPeak)
                            UserPeak = UsersOnline;

                        using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                        {
                            dbClient.runFastQuery("UPDATE server_status SET stamp = '" + FirewindEnvironment.GetUnixTimestamp() + "', status = " + Status + ", users_online = " + UsersOnline + ", rooms_loaded = " + RoomsLoaded + ", server_ver = '" + FirewindEnvironment.PrettyVersion + "', userpeak = " + UserPeak + "");
                        }
                        #endregion
                    }
                    catch (Exception e) { Logging.LogThreadException(e.ToString(), "Server status update task"); }
                }
                spentTime = DateTime.Now - startTaskTime;

                if (spentTime.TotalSeconds > 3)
                {
                    Logging.WriteLine("LowPriorityWorker.Process spent: " + spentTime.TotalSeconds + " seconds in working.");
                }
                FirewindEnvironment.GetGame().LowPriorityWorker_ended = true;
            }
            catch (Exception e) { Logging.LogThreadException(e.ToString(), "LowPriorityWorker.Process Exception --> Not inclusive"); }
        }
    }
}
