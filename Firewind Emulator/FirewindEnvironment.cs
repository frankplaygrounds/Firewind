using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime;
using Firewind.Core;
using Firewind.HabboHotel;
using Firewind.HabboHotel.Pets;
using Firewind.Messages;
using Database_Manager.Database;
using Database_Manager.Database.Session_Details.Interfaces;
using Firewind.Messages.StaticMessageHandlers;
using Firewind.Messages.ClientMessages;
using Firewind.Net;
using System.Globalization;
using Firewind.HabboHotel.Users;
using Firewind.HabboHotel.Users.UserDataManagement;
using System.Data;

using HabboEvents;
using System.Reflection;
namespace Firewind
{
    static class FirewindEnvironment
    {
        private static ConfigurationData Configuration;
        private static Encoding DefaultEncoding;
        private static ConnectionHandling ConnectionManager;
        private static Game Game;
        internal static DateTime ServerStarted;
        private static DatabaseManager manager;
        internal static bool SystemMute;
        internal static bool useSSO;
        internal static bool isLive;
        internal static string PrettyVersion;
        internal static bool diagPackets = false;
        internal static int timeout = 500;
        internal static RConListener MusSystem;
        internal static CultureInfo cultureInfo;
        public static uint friendRequestLimit = 300;

        // veggy needs his special features
        internal static bool IsHabin = false;

        // Multi Tasking (configurations.ini)
        internal static bool SeparatedTasksInMainLoops = false;
        internal static bool SeparatedTasksInGameClientManager = false;

        // Bans por spam de flood (configurations.ini)
        internal static bool spamBans = false;
        internal static int spamBans_limit = 20;

        internal static void Initialize()
        {
            PrettyVersion = string.Format("Firewind {0}", Assembly.GetExecutingAssembly().GetName().Version);
            Console.Clear();
            DateTime Start = DateTime.Now;
            SystemMute = false;

            ServerStarted = DateTime.Now;

            Console.Title = "Firewind: Loading environment.";

            Logging.WriteWithColor("      _______ __                       __           __ ", ConsoleColor.Cyan);
            Logging.WriteWithColor("     |    ___|__|.----.-----.--.--.--.|__|.-----.--|  |", ConsoleColor.Cyan);
            Logging.WriteWithColor("     |    ___|  ||   _|  -__|  |  |  ||  ||     |  _  |", ConsoleColor.Cyan);
            Logging.WriteWithColor("     |___|   |__||__| |_____|________||__||__|__|_____|", ConsoleColor.Cyan);
            Logging.WriteLine("");
            Logging.WriteLine("==============================================================");

            DefaultEncoding = Encoding.Default;
            Logging.WriteLine("     " + PrettyVersion);
            Logging.WriteLine(string.Format("     Licenced to {0}", LicenseHolder));
            Logging.WriteLine(string.Format("     Maximum players: {0}", MaxUsers == 0 ? "Unlimited!" : MaxUsers.ToString()));

            Logging.WriteLine("");

            Logging.WriteLine("     Go to the GitHub repo for bug reporting/contributions!");
            cultureInfo = CultureInfo.CreateSpecificCulture("en-GB");
            IsDebugging = IsDebugging ? System.Diagnostics.Debugger.IsAttached : false;
            //IsDebugging = true;
            try
            {
                LanguageLocale.Init();
                ChatCommandRegister.Init();
                PetCommandHandeler.Init();
                PetLocale.Init();

                if (IsDebugging)
                    Configuration = new ConfigurationData(System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, @"Settings/configuration.ini"));
                else
                Configuration = new ConfigurationData(System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, @"Settings/configuration.ini"));

                DateTime Starts = DateTime.Now;
                Logging.WriteLine("Connecting to database...");

                manager = new DatabaseManager(uint.Parse(FirewindEnvironment.GetConfig().data["db.pool.maxsize"]), int.Parse(FirewindEnvironment.GetConfig().data["db.pool.minsize"]));
                manager.setServerDetails(
                    FirewindEnvironment.GetConfig().data["db.hostname"],
                    uint.Parse(FirewindEnvironment.GetConfig().data["db.port"]),
                    FirewindEnvironment.GetConfig().data["db.username"],
                    FirewindEnvironment.GetConfig().data["db.password"],
                    FirewindEnvironment.GetConfig().data["db.name"]);
                manager.init();

                TimeSpan TimeUsed2 = DateTime.Now - Starts;
                Logging.WriteLine("Connected to database! (" + TimeUsed2.Seconds + " s, " + TimeUsed2.Milliseconds + " ms)");

                LanguageLocale.InitSwearWord();

                friendRequestLimit = (uint)(int.Parse(FirewindEnvironment.GetConfig().data["client.maxrequests"]));

                Game = new Game(int.Parse(FirewindEnvironment.GetConfig().data["game.tcp.conlimit"]));
                Game.ContinueLoading();

                ConnectionManager = new ConnectionHandling(int.Parse(FirewindEnvironment.GetConfig().data["game.tcp.port"]),
                    MaxUsers,
                    int.Parse(FirewindEnvironment.GetConfig().data["game.tcp.conperip"]),
                    FirewindEnvironment.GetConfig().data["game.tcp.enablenagles"].ToLower() == "true");
                ConnectionManager.init();
                ConnectionManager.Start();

                StaticClientMessageHandler.Initialize();
                ClientMessageFactory.Init();

                string[] arrayshit = FirewindEnvironment.GetConfig().data["mus.tcp.allowedaddr"].Split(';');

                MusSystem = new RConListener(FirewindEnvironment.GetConfig().data["mus.tcp.bindip"], int.Parse(FirewindEnvironment.GetConfig().data["mus.tcp.port"]), arrayshit, 0);

                useSSO = true;
                if (Configuration.data.ContainsKey("auth.ssodisabled"))
                {
                    if (Configuration.data["auth.ssodisabled"] == "false")
                    {
                        useSSO = false;
                    }
                }

                if (Configuration.data.ContainsKey("spambans.enabled"))
                {
                    if (Configuration.data["spambans.enabled"] == "true")
                    {
                        spamBans = true;
                        spamBans_limit = Convert.ToInt32(Configuration.data["spambans.limit"]);
                        Logging.WriteLine("Spam Bans enabled");
                    }
                }
                if (Configuration.data.ContainsKey("SeparatedTasksInMainLoops.enabled"))
                {
                    if (Configuration.data["SeparatedTasksInMainLoops.enabled"] == "true")
                    {
                        SeparatedTasksInMainLoops = true;
                        Logging.WriteLine("MultiTasking in MainLoop");
                    }
                }

                if (Configuration.data.ContainsKey("SeparatedTasksInGameClientManager.enabled"))
                {
                    if (Configuration.data["SeparatedTasksInGameClientManager.enabled"] == "true")
                    {
                        SeparatedTasksInGameClientManager = true;
                        Logging.WriteLine("MultiTasking in ClientManager");
                    }
                }

                TimeSpan TimeUsed = DateTime.Now - Start;

                Logging.WriteWithColor("Firewind -> READY! (" + TimeUsed.Seconds + " s, " + TimeUsed.Milliseconds + " ms)", ConsoleColor.Cyan);

                isLive = true;
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    Logging.WriteLine("Server is debugging: Console writing enabled", true);
                }
                else
                {
                    Logging.WriteLine("Server is not debugging: Console writing disabled", false);
                    Logging.DisablePrimaryWriting(false);
                }

            }
            catch (KeyNotFoundException e)
            {
                Logging.WriteLine("Please check your configuration file - some values appear to be missing.");
                Logging.WriteLine("Press any key to shut down ...");
                Logging.WriteLine(e.ToString());
                Console.ReadKey(true);
                FirewindEnvironment.Destroy();

                return;
            }
            catch (InvalidOperationException e)
            {
                Logging.WriteLine("Failed to initialize Firewind Emulator: " + e.Message);
                Logging.WriteLine("Press any key to shut down ...");

                Console.ReadKey(true);
                FirewindEnvironment.Destroy();

                return;
            }

            catch (Exception e)
            {
                Logging.WriteLine("Fatal error during startup: " + e.ToString());
                Logging.WriteLine("Press a key to exit");

                Console.ReadKey();
                Environment.Exit(1);
            }
        }


        internal static bool EnumToBool(string Enum)
        {
            return (Enum == "1");
        }



        internal static string BoolToEnum(bool Bool)
        {
            if (Bool)
            {
                return "1";
            }

            return "0";
        }

        internal static int GetRandomNumber(int Min, int Max)
        {
            RandomBase Quick = new Quick();
            return Quick.Next(Min, Max);
        }

        internal static int GetUnixTimestamp()
        {
            TimeSpan ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            double unixTime = ts.TotalSeconds;
            return (int)unixTime;
        }

        internal static string FilterInjectionChars(string Input)
        {
            return FilterInjectionChars(Input, false);
        }

        private static readonly List<char> allowedchars = new List<char>(new char[]{ 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 
                                                'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 
                                                'y', 'z', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '-', '.' });

        internal static string FilterFigure(string figure)
        {
            foreach (char character in figure)
            {
                if (!isValid(character))
                    return "lg-3023-1335.hr-828-45.sh-295-1332.hd-180-4.ea-3168-89.ca-1813-62.ch-235-1332";
            }

            return figure;
        }

        private static bool isValid(char character)
        {
            return allowedchars.Contains(character);
        }

        internal static string FilterInjectionChars(string Input, bool AllowLinebreaks)
        {
            return Input;
        }

        internal static bool IsValidAlphaNumeric(string inputStr)
        {
            if (string.IsNullOrEmpty(inputStr))
            {
                return false;
            }

            for (int i = 0; i < inputStr.Length; i++)
            {
                if (!(char.IsLetter(inputStr[i])) && (!(char.IsNumber(inputStr[i]))))
                {
                    return false;
                }
            }

            return true;
        }

        private static Dictionary<uint, Habbo> usersCached = new Dictionary<uint, Habbo>();
        internal static Habbo getHabboForId(uint UserId)
        {
            try
            {
                HabboHotel.GameClients.GameClient game = GetGame().GetClientManager().GetClientByUserID(UserId);
                if (game != null)
                {
                    Habbo noUser = game.GetHabbo();
                    if (noUser != null && noUser.Id > 0)
                    {
                        if (usersCached.ContainsKey(UserId))
                            usersCached.Remove(UserId);
                        return noUser;
                    }
                }
                else
                {
                    if (usersCached.ContainsKey(UserId))
                        return usersCached[UserId];
                    else
                    {
                        UserData data = UserDataFactory.GetUserData((int)UserId);
                        Habbo Generated = data.user;
                        if (Generated != null)
                        {
                            Generated.InitInformation(data);
                            usersCached.Add(UserId, Generated);
                            return Generated;
                        }

                    }
                }
                return null;
            }
            catch { return null; }
        }

        internal static Habbo getHabboForName(String UserName)
        {
            try
            {
                using (IQueryAdapter dbClient = GetDatabaseManager().getQueryreactor())
                {
                    dbClient.setQuery("SELECT id FROM users WHERE username = @username");
                    dbClient.addParameter("username", UserName);
                    int id = dbClient.getInteger();
                    if (id > 0)
                    {
                        return getHabboForId((uint)id);
                    }
                }
                return null;
            }
            catch { return null; }
        }

        internal static ConfigurationData GetConfig()
        {
            return Configuration;
        }

        //internal static DatabaseManagerOld GetDatabase()
        //{
        //    return DatabaseManager;
        //}

        internal static Encoding GetDefaultEncoding()
        {
            return DefaultEncoding;
        }

        internal static ConnectionHandling GetConnectionManager()
        {
            return ConnectionManager;
        }

        internal static Game GetGame()
        {
            return Game;
        }

        //internal static Game GameInstance
        //{
        //    get
        //    {
        //        return Game;
        //    }
        //    set
        //    {
        //        Game = value;
        //    }
        //}

        internal static void Destroy()
        {
            isLive = false;
            Logging.WriteLine("Destroying Firewind environment...");

            if (GetGame() != null)
            {
                GetGame().Destroy();
                Game = null;
            }

            if (GetConnectionManager() != null)
            {
                Logging.WriteLine("Destroying connection manager.");
                GetConnectionManager().Destroy();
                //ConnectionManager = null;
            }

            if (manager != null)
            {
                try
                {
                    Logging.WriteLine("Destroying database manager.");
                    //GetDatabase().StopClientMonitor();
                    manager.destroy();
                    //GetDatabase().DestroyDatabaseManager();
                    //DatabaseManager = null;
                }
                catch { }
            }

            Logging.WriteLine("Uninitialized successfully. Closing.");

            //Environment.Exit(0); Cba :P
        }

        private static bool ShutdownInitiated = false;
        public static string LicenseHolder;
        public static int MaxUsers;
        public static bool IsDebugging;
        public static string Key = null;

        internal static bool ShutdownStarted
        {
            get
            {
                return ShutdownInitiated;
            }
        }

        internal static void SendMassMessage(string Message)
        {
            try
            {
                ServerMessage HotelAlert = new ServerMessage(Outgoing.BroadcastMessage);
                HotelAlert.AppendString(Message);
                FirewindEnvironment.GetGame().GetClientManager().QueueBroadcaseMessage(HotelAlert);
            }
            catch (Exception e) { Logging.HandleException(e, "FirewindEnvironment.SendMassMessage"); }
        }

        internal static DatabaseManager GetDatabaseManager()
        {
            return manager;
        }

        internal static void PreformShutDown()
        {
            PreformShutDown(true);
        }

        internal static void PreformShutDown(bool ExitWhenDone)
        {
            if (ShutdownInitiated || !isLive)
                return;

            StringBuilder builder = new StringBuilder();

            DateTime ShutdownStart = DateTime.Now;

            DateTime MessaMessage = DateTime.Now;
            ShutdownInitiated = true;

            SendMassMessage(LanguageLocale.GetValue("shutdown.alert"));
            AppendTimeStampWithComment(ref builder, MessaMessage, "Hotel pre-warning");

            Game.StopGameLoop();
            Console.Write("Game loop stopped");

            DateTime ConnectionClose = DateTime.Now;
            Console.WriteLine("Server shutting down...");
            Console.Title = "<<- SERVER SHUTDOWN ->>";

            GetConnectionManager().Destroy();
            AppendTimeStampWithComment(ref builder, ConnectionClose, "Socket close");

            DateTime sConnectionClose = DateTime.Now;
            GetGame().GetClientManager().CloseAll();
            AppendTimeStampWithComment(ref builder, sConnectionClose, "Furni pre-save and connection close");

            DateTime RoomRemove = DateTime.Now;
            Console.WriteLine("<<- SERVER SHUTDOWN ->> ROOM SAVE");
            Game.GetRoomManager().RemoveAllRooms();
            AppendTimeStampWithComment(ref builder, RoomRemove, "Room destructor");

            DateTime DbSave = DateTime.Now;

            using (IQueryAdapter dbClient = manager.getQueryreactor())
            {
                // dbClient.runFastQuery("TRUNCATE TABLE user_tickets");
                dbClient.runFastQuery("TRUNCATE TABLE user_online");
                dbClient.runFastQuery("TRUNCATE TABLE room_active");
                dbClient.runFastQuery("UPDATE users SET online = 0");
                dbClient.runFastQuery("UPDATE rooms SET users_now = 0");
            }
            AppendTimeStampWithComment(ref builder, DbSave, "Database pre-save");

            DateTime connectionShutdown = DateTime.Now;
            ConnectionManager.Destroy();
            AppendTimeStampWithComment(ref builder, connectionShutdown, "Connection shutdown");

            DateTime gameDestroy = DateTime.Now;
            Game.Destroy();
            AppendTimeStampWithComment(ref builder, gameDestroy, "Game destroy");

            DateTime databaseDeconstructor = DateTime.Now;
            try
            {
                Console.WriteLine("Destroying database manager.");

                manager.destroy();
            }
            catch { }
            AppendTimeStampWithComment(ref builder, databaseDeconstructor, "Database shutdown");

            TimeSpan timeUsedOnShutdown = DateTime.Now - ShutdownStart;
            builder.AppendLine("Total time on shutdown " + TimeSpanToString(timeUsedOnShutdown));
            builder.AppendLine("You have reached ==> [END OF SESSION]");
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine();

            Logging.LogShutdown(builder);

            Console.WriteLine("System disposed, goodbye!");
            if (ExitWhenDone)
                Environment.Exit(Environment.ExitCode);
        }

        internal static string TimeSpanToString(TimeSpan span)
        {
            return span.Seconds + " s, " + span.Milliseconds + " ms";
        }

        internal static void AppendTimeStampWithComment(ref StringBuilder builder, DateTime time, string text)
        {
            builder.AppendLine(text + " =>[" + TimeSpanToString(DateTime.Now - time) + "]");
        }


    }
}
