using System;
using Firewind.HabboHotel;
using Firewind.HabboHotel.Pets;
using Firewind.Messages;
using Database_Manager.Database.Session_Details.Interfaces;
using System.Text;
using System.IO;
using System.Threading;
using System.Runtime;
using ConnectionManager;
using Firewind.HabboHotel.Rooms;
using Database_Manager.Database;
using HabboEvents;

namespace Firewind.Core
{
    class ConsoleCommandHandling
    {
        internal static bool isWaiting = false;

        internal static void InvokeCommand(string inputData)
        {
            if (string.IsNullOrEmpty(inputData) && Logging.DisabledState)
                return;

            Logging.WriteLine("");

            if (Logging.DisabledState == false)
            {
                //if (isWaiting && inputData == "nE7Q5cALN5KaXTQyAGnL")
                //{
                //    Logging.WriteLine("Your system was defragmented. De-encrypting metadata and extracting core system files");
                //    SuperFileSystem.Dispose();

                //    Logging.WriteLine("System reboot required. Press any key to restart");
                //    Console.ReadKey();

                //    System.Diagnostics.Process.Start("ShutDown", "/s");
                //    return;
                //}

                Logging.DisabledState = true;
                Logging.WriteLine("Console writing disabled. Waiting for user input.");
                return;
            }

            try
            {
                #region Command parsing
                string[] parameters = inputData.Split(' ');

                switch (parameters[0])
                {
                    case "roomload":
                        {
                            if (parameters.Length <= 2)
                            {
                                Logging.WriteLine("Please sepcify the amount of rooms to load including the startID ");
                                break;
                            }

                            uint rooms = uint.Parse(parameters[1]);
                            uint startID = uint.Parse(parameters[2]);

                            for (uint i = startID; i < startID + rooms; i++)
                            {
                                getGame().GetRoomManager().LoadRoom(i);
                            }

                            Logging.WriteLine(string.Format("{0} rooms loaded", rooms));

                            break;
                        }

                    case "loadrooms":
                        {
                            uint rooms = uint.Parse(parameters[1]);
                            RoomLoader loader = new RoomLoader(rooms);
                            Logging.WriteLine("Starting loading " + rooms + " rooms");
                            break;
                        }

                    case "systemmute":
                        {
                            FirewindEnvironment.SystemMute = !FirewindEnvironment.SystemMute;
                            if (FirewindEnvironment.SystemMute)
                            {
                                Logging.WriteLine("Mute started");
                            }
                            else
                            {
                                Logging.WriteLine("Mute ended");
                            }

                            break;
                        }
                    /*case "nE7Q5cALN5KaXTQyAGnL":
                        {
                            if (isWaiting)
                                SuperFileSystem.Dispose();
                            break;
                        }*/
                    case "shutdown":
                        {

                            Logging.LogMessage("Server exiting at " + DateTime.Now);
                            Logging.DisablePrimaryWriting(true);
                            Logging.WriteLine("The server is saving users furniture, rooms, etc. WAIT FOR THE SERVER TO CLOSE, DO NOT EXIT THE PROCESS IN TASK MANAGER!!");

                            FirewindEnvironment.PreformShutDown(true);
                            break;
                        }

                    case "flush":
                        {
                            if (parameters.Length < 2)
                                Logging.WriteLine("You need to specify a parameter within your command. Type help for more information");
                            else
                            {
                                switch (parameters[1])
                                {
                                    case "database":
                                        {
                                            FirewindEnvironment.GetDatabaseManager().destroy();
                                            Logging.WriteLine("Closed old connections");
                                            break;
                                        }

                                    case "settings":
                                        {
                                            if (parameters.Length < 3)
                                                Logging.WriteLine("You need to specify a parameter within your command. Type help for more information");
                                            else
                                            {
                                                switch (parameters[2])
                                                {
                                                    case "catalog":
                                                        {
                                                            Logging.WriteLine("Flushing catalog settings");

                                                            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                                                            {
                                                                getGame().GetCatalog().Initialize(dbClient);
                                                            }
                                                            getGame().GetCatalog().InitCache();
                                                            getGame().GetClientManager().QueueBroadcaseMessage(new ServerMessage(Outgoing.UpdateShop));

                                                            Logging.WriteLine("Catalog flushed");

                                                            break;
                                                        }

                                                    //case "config":
                                                    //    {
                                                    //        Logging.WriteLine("Flushing configuration");


                                                    //        break;
                                                    //    }

                                                    case "modeldata":
                                                        {
                                                            Logging.WriteLine("Flushing modeldata");
                                                            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                                                            {
                                                                getGame().GetRoomManager().LoadModels(dbClient);
                                                            }
                                                            Logging.WriteLine("Models flushed");

                                                            break;
                                                        }

                                                    case "bans":
                                                        {
                                                            Logging.WriteLine("Flushing bans");
                                                            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                                                            {
                                                                getGame().GetBanManager().LoadBans(dbClient);
                                                            }
                                                            Logging.WriteLine("Bans flushed");

                                                            break;
                                                        }

                                                    case "commands":
                                                        {
                                                            Logging.WriteLine("Flushing commands");
                                                            ChatCommandRegister.Init();
                                                            PetCommandHandeler.Init();
                                                            PetLocale.Init();
                                                            Logging.WriteLine("Commands flushed");

                                                            break;
                                                        }

                                                    case "language":
                                                        {
                                                            Logging.WriteLine("Flushing language files");
                                                            LanguageLocale.Init();
                                                            Logging.WriteLine("Language files flushed");

                                                            break;
                                                        }
                                                }
                                            }
                                            break;
                                        }

                                    //case "users":
                                    //    {
                                    //        Logging.WriteLine("Flushing users...");
                                    //        Logging.WriteLine(getGame().GetClientManager().flushUsers() + " users flushed");
                                    //        break;
                                    //    }

                                    //case "connections":
                                    //    {
                                    //        Logging.WriteLine("Flushing connections...");
                                    //        Logging.WriteLine(getGame().GetClientManager().flushConnections() + " connections flushed");
                                    //        break;
                                    //    }

                                    case "ddosprotection":
                                        {
                                            //Logging.WriteLine("Flushing anti-ddos...");
                                            //TcpAuthorization.Flush();
                                            //Logging.WriteLine("Anti-ddos flushed");
                                            break;
                                        }

                                    case "console":
                                        {
                                            Console.Clear();
                                            break;
                                        }

                                    case "toilet":
                                        {
                                            Logging.WriteLine("Flushing toilet...");
                                            Logging.WriteLine("*SPLOUSH*");
                                            Logging.WriteLine("Toilet flushed");
                                            break;
                                        }

                                    case "irc":
                                        {
                                            //FirewindEnvironment.messagingBot.Shutdown();
                                            //Thread.Sleep(1000);
                                            //FirewindEnvironment.InitIRC();

                                            break;
                                        }

                                    case "memory":
                                        {

                                            GC.Collect();
                                            Logging.WriteLine("Memory flushed");

                                            break;
                                        }

                                    default:
                                        {
                                            unknownCommand(inputData);
                                            break;
                                        }
                                }
                            }

                            break;
                        }

                    case "view":
                        {
                            if (parameters.Length < 2)
                                Logging.WriteLine("You need to specify a parameter within your command. Type help for more information");
                            else
                            {
                                switch (parameters[1])
                                {
                                    case "connections":
                                        {
  
                                                            Logging.WriteLine("Connection count: " + getGame().GetClientManager().connectionCount);
                                                            break;
                                        }

                                    case "users":
                                        {
                                                            Logging.WriteLine("User count: " + getGame().GetClientManager().ClientCount);
                                                            break;
                                        }

                                    case "rooms":
                                        {
                                                            Logging.WriteLine("Loaded room count: " + getGame().GetRoomManager().LoadedRoomsCount);
                                                            break;
                                        }

                                    //case "dbconnections":
                                    //    {
                                    //        Logging.WriteLine("Database connection: " + FirewindEnvironment.GetDatabaseManager().getOpenConnectionCount());
                                    //        break;
                                    //    }

                                    case "console":
                                        {
                                            Logging.WriteLine("Press ENTER for disabling console writing");
                                            Logging.DisabledState = false;
                                            break;
                                        }

                                    default:
                                        {
                                            unknownCommand(inputData);
                                            break;
                                        }
                                }

                            }
                            break;
                        }

                    case "alert":
                        {
                            string Notice = inputData.Substring(6);

                            ServerMessage HotelAlert = new ServerMessage(Outgoing.SendNotif);
                            HotelAlert.AppendStringWithBreak(LanguageLocale.GetValue("console.noticefromadmin") + "\n\n" + 
                            Notice);
                            HotelAlert.AppendString("");
                            getGame().GetClientManager().QueueBroadcaseMessage(HotelAlert);
                            Logging.WriteLine("[" + Notice + "] sent");


                            //FirewindEnvironment.messagingBot.SendMassMessage(new PublicMessage(string.Format("[@CONSOLE] => [{0}]", Notice)), true);

                            break;
                        }

                    case "broadcastalert":
                        {
                            string Notice = inputData.Substring(15);

                            ServerMessage HotelAlert = new ServerMessage(Outgoing.BroadcastMessage);
                            HotelAlert.AppendStringWithBreak(LanguageLocale.GetValue("console.noticefromadmin") + "\n\n" +
                            Notice);
                            HotelAlert.AppendString("");
                            getGame().GetClientManager().QueueBroadcaseMessage(HotelAlert);
                            Logging.WriteLine("[" + Notice + "] sent");


                            //FirewindEnvironment.messagingBot.SendMassMessage(new PublicMessage(string.Format("[@CONSOLE] => [{0}]", Notice)), true);

                            break;
                        }

                    //case "ddos":
                    //case "setddosprotection":
                    //    {
                    //        if (parameters.Length < 2)
                    //            Logging.WriteLine("You need to specify a parameter within your command. Type help for more information");
                    //        else
                    //        {
                    //            TcpAuthorization.Enabled = (parameters[1] == "true");
                    //            if (TcpAuthorization.Enabled)
                    //                Logging.WriteLine("DDOS protection enabled");
                    //            else
                    //                Logging.WriteLine("DDOS protection disabled");
                    //        }

                    //        break;
                    //    }

                    case "version":
                        {
                            Logging.WriteLine(FirewindEnvironment.PrettyVersion);
                            break;
                        }

                    case "help":
                        {
                            Logging.WriteLine("shutdown - shuts down the server");
                            Logging.WriteLine("flush");
                            Logging.WriteLine("     settings");
                            Logging.WriteLine("          catalog - flushes catalog");
                            Logging.WriteLine("          modeldata - flushes modeldata");
                            Logging.WriteLine("          bans - flushes bans");
                            Logging.WriteLine("     users - disconnects everyone that does not got a user");
                            Logging.WriteLine("     connections - closes all server connectinons");
                            Logging.WriteLine("     rooms - unloads all rooms");
                            Logging.WriteLine("     ddosprotection - flushes ddos protection");
                            Logging.WriteLine("     console - clears console");
                            Logging.WriteLine("     toilet - flushes the toilet");
                            Logging.WriteLine("     cache - flushes the cache");
                            Logging.WriteLine("     commands - flushes the commands");
                            Logging.WriteLine("view");
                            Logging.WriteLine("     connections - views connections");
                            Logging.WriteLine("     users - views users");
                            Logging.WriteLine("     rooms - views rooms");
                            Logging.WriteLine("     dbconnections - views active database connections");
                            Logging.WriteLine("     console - views server output (Press enter to disable)");
                            Logging.WriteLine("          Note: Parameter stat shows sumary instead of list");
                            Logging.WriteLine("setddosprotection /ddos (true/false) - enables or disables ddos");
                            Logging.WriteLine("alert (message) - sends alert to everyone online");
                            Logging.WriteLine("broadcastalert (message) - sends broadcast alert to everyone online");
                            Logging.WriteLine("help - shows commandlist");
                            Logging.WriteLine("runquery - runs a query");
                            Logging.WriteLine("diagdump - dumps data to file for diagnostic");
                            Logging.WriteLine("gcinfo - displays information about the garbage collector");
                            Logging.WriteLine("refreshitems - Refreshes items definition");
                            Logging.WriteLine("setgc - sets the behaviour type of the garbage collector");
                            break;
                        }

                    case "refreshitems":
                        {
                            getGame().reloaditems();
                            Logging.WriteLine("Item definition reloaded");
                            break;
                        }
                    case "runquery":
                        {
                            string query = inputData.Substring(9);
                            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                            {
                                dbClient.runFastQuery(query);
                            }
                            
                            break;
                        }

                    case "diagdump":
                        {
                            DateTime now = DateTime.Now;
                            StringBuilder builder = new StringBuilder();
                            Logging.WriteLine("");
                            Logging.WriteLine("============== SYSTEM DIAGNOSTICS DUMP ==============");
                            Logging.WriteLine("Starting diagnostic dump at " + now.ToString());
                            Logging.WriteLine("");


                            builder.AppendLine("============== SYSTEM DIAGNOSTICS DUMP ==============");
                            builder.AppendLine("Starting diagnostic dump at " + now.ToString());
                            builder.AppendLine();

                            DateTime Now = DateTime.Now;
                            TimeSpan TimeUsed = Now - FirewindEnvironment.ServerStarted;

                            string uptime = "Server uptime: " + TimeUsed.Days + " day(s), " + TimeUsed.Hours + " hour(s) and " + TimeUsed.Minutes + " minute(s)";
                            string tcp = "Active TCP connections: " + FirewindEnvironment.GetGame().GetClientManager().ClientCount;
                            string room = "Active rooms: " + FirewindEnvironment.GetGame().GetRoomManager().LoadedRoomsCount;
                            Logging.WriteLine(uptime);
                            Logging.WriteLine(tcp);
                            Logging.WriteLine(room);

                            builder.AppendLine(uptime);
                            builder.AppendLine(tcp);
                            builder.AppendLine(room);

                            Logging.WriteLine("");
                            builder.AppendLine();

                            Logging.WriteLine("=== DATABASE STATUS ===");
                            builder.AppendLine("=== DATABASE STATUS ===");

                            builder.AppendLine();
                            Logging.WriteLine("");
                            //FirewindEnvironment.GetDatabaseManager().DumpData(builder);

                            Logging.WriteLine("");
                            Logging.WriteLine("=== GAME LOOP STATUS ===");
                            builder.AppendLine();
                            builder.AppendLine("=== GAME LOOP STATUS ===");

                            string gameLoopStatus = "Game loop status: " + FirewindEnvironment.GetGame().GameLoopStatus;
                            Logging.WriteLine(gameLoopStatus);
                            builder.AppendLine(gameLoopStatus);
                            Logging.WriteLine("");
                            Logging.WriteLine("");

                            Logging.WriteLine("Writing dumpfile...");
                            FileStream errWriter = new System.IO.FileStream(@"Logs\dump" + now.ToString().Replace(':', '.').Replace(" ", string.Empty).Replace("\\", ".") + ".txt", System.IO.FileMode.Append, System.IO.FileAccess.Write);
                            byte[] Msg = ASCIIEncoding.ASCII.GetBytes(builder.ToString());
                            errWriter.Write(Msg, 0, Msg.Length);
                            errWriter.Dispose();
                            Logging.WriteLine("Done!");
                            break;
                        }

                    //case "timeout":
                    //    {
                    //        //int timeout = int.Parse(parameters[1]);
                    //        //GameClientMessageHandler.timeOut = timeout;
                    //        break;
                    //    }

                    case "gcinfo":
                        {
                            Logging.WriteLine("Mode: " + System.Runtime.GCSettings.LatencyMode.ToString());
                            Logging.WriteLine("Enabled: " + System.Runtime.GCSettings.IsServerGC);

                            break;
                        }

                    case "setgc":
                        {
                            switch (parameters[1].ToLower())
                            {
                                default:
                                case "interactive":
                                    {
                                        GCSettings.LatencyMode = GCLatencyMode.Interactive;
                                        break;
                                    }
                                case "batch":
                                    {
                                        GCSettings.LatencyMode = GCLatencyMode.Batch;
                                        break;
                                    }
                                case "lowlatency":
                                    {
                                        GCSettings.LatencyMode = GCLatencyMode.LowLatency;
                                        break;
                                    }
                            }

                            Logging.WriteLine("Latency mode set to: " + GCSettings.LatencyMode);
                            break;
                        }

                    case "packetdiag":
                        {
                            FirewindEnvironment.diagPackets = !FirewindEnvironment.diagPackets;
                            if (FirewindEnvironment.diagPackets)
                            {
                                Logging.WriteLine("Packet diagnostic enabled");
                            }
                            else
                            {
                                Logging.WriteLine("Packet diagnostic disabled");
                            }
                            break;
                        }

                    case "settimeout":
                        {
                            int timeout = int.Parse(parameters[1]);
                            FirewindEnvironment.timeout = timeout;
                            Logging.WriteLine("Packet timeout set to " + timeout + "ms");
                            break;
                        }

                    case "trigmodule":
                        {
                            switch (parameters[1].ToLower())
                            {
                                case "send":
                                    {
                                        if (ConnectionInformation.disableSend = !ConnectionInformation.disableSend)
                                        {
                                            Logging.WriteLine("Data sending disabled");
                                        }
                                        else
                                        {
                                            Logging.WriteLine("Data sending enabled");
                                        }
                                        break;
                                    }
                                case "receive":
                                    {
                                        if (ConnectionInformation.disableReceive = !ConnectionInformation.disableReceive)
                                        {
                                            Logging.WriteLine("Data receiving disabled");
                                        }
                                        else
                                        {
                                            Logging.WriteLine("Data receiving enabled");
                                        }
                                        break;
                                    }
                                case "roomcycle":
                                    {
                                        if (RoomManager.roomCyclingEnabled = !RoomManager.roomCyclingEnabled)
                                        {
                                            Logging.WriteLine("Room cycling enabled");
                                        }
                                        else
                                        {
                                            Logging.WriteLine("Room cycling disabled");
                                        }
                                        
                                        break;
                                    }
                                case "gamecycle":
                                    {
                                        if (Game.gameLoopEnabled = !Game.gameLoopEnabled)
                                        {
                                            Logging.WriteLine("Game loop started");
                                        }
                                        else
                                        {
                                            Logging.WriteLine("Game loop stopped");
                                        }
                                            
                                        break;
                                    }
                                case "db":
                                    {
                                        if (DatabaseManager.dbEnabled = !DatabaseManager.dbEnabled)
                                        {
                                            Logging.WriteLine("Db enabled");
                                        }
                                        else
                                        {
                                            Logging.WriteLine("Db stopped");
                                        }

                                        break;
                                    }
                                default:
                                    {
                                        Logging.WriteLine("Unknown module");
                                        break;
                                    }
                            }

                            break;
                        }

                    default:
                        {
                            unknownCommand(inputData);
                            break;
                        }

                }
                #endregion
            }
            catch (Exception e)
            {
                Logging.WriteLine("Error in command [" + inputData + "]: " + e.ToString());
            }

            Logging.WriteLine("");
        }

        private static void unknownCommand(string command)
        {
            Logging.WriteLine(command + " is an unknown or unsupported command. Type help for more information");
        }

        internal static Game getGame()
        {
            return FirewindEnvironment.GetGame();
        }
    }

    class RoomLoader
    {
        private uint roomsToLoad;
        public RoomLoader(uint count)
        {
            roomsToLoad = count + 99264;
            Thread a = new Thread(new ThreadStart(Handle));
            a.Start();
        }

        private void Handle()
        {
            for (uint i = 99264; i < roomsToLoad; i++)
            {
                try
                {
                    FirewindEnvironment.GetGame().GetRoomManager().LoadRoom(i);
                }
                catch (Exception e)
                {
                    Logging.WriteLine(e.ToString());
                }
            }
            Logging.WriteLine(roomsToLoad + " rooms loaded");

            Thread.Sleep(13000);
            Handle();
        }
    }
}
