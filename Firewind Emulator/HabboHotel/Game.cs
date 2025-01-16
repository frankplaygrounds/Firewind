using System;
using System.Threading.Tasks;
using Firewind.Core;
using Firewind.HabboHotel.Achievements;
using Firewind.HabboHotel.Advertisements;
using Firewind.HabboHotel.Catalogs;
using Firewind.HabboHotel.GameClients;
using Firewind.HabboHotel.Items;
using Firewind.HabboHotel.Misc;
using Firewind.HabboHotel.Navigators;
using Firewind.HabboHotel.Roles;
using Firewind.HabboHotel.RoomBots;
using Firewind.HabboHotel.Rooms;
using Firewind.HabboHotel.Support;
using Firewind.HabboHotel.Pets;
using Firewind.HabboHotel.Users.Inventory;
using Database_Manager.Database.Session_Details.Interfaces;
using System.Threading;
using Firewind.HabboHotel.Quests;
using Firewind.HabboHotel.SoundMachine;
using Firewind.HabboHotel.Users.Competitions;
using Firewind.HabboHotel.Groups;

namespace Firewind.HabboHotel
{
    class Game
    {
        #region Fields
        private GameClientManager ClientManager;
        private ModerationBanManager BanManager;
        private RoleManager RoleManager;
        private Catalog Catalog;
        private Navigator Navigator;
        private ItemManager ItemManager;
        private RoomManager RoomManager;
        private AdvertisementManager AdvertisementManager;
        private PixelManager PixelManager;
        private AchievementManager AchievementManager;
        private ModerationTool ModerationTool;
        private BotManager BotManager;
        //private Task StatisticsThread;
        //private Task ConsoleTitleTask;
        private InventoryGlobal globalInventory;
        private QuestManager questManager;
        //private SoundMachineManager soundMachineManager;
        private GroupManager groupManager;

        private Thread gameLoop;
        private Thread gameLoopSUBRooms;
        private Thread deadLocksThread;
        private bool gameLoopActive;
        private bool gameLoopEnded;
        private const int gameLoopSleepTime = 25;

        // Tasks Main GameLoop
        private Task LowPriorityWorker_task, RoomManagerCycle_task, ClientManagerCycle_task;
        internal bool LowPriorityWorker_ended, RoomManagerCycle_ended, ClientManagerCycle_ended;
        #endregion

        #region Return values

        internal GameClientManager GetClientManager()
        {
            return ClientManager;
        }

        internal ModerationBanManager GetBanManager()
        {
            return BanManager;
        }

        internal RoleManager GetRoleManager()
        {
            return RoleManager;
        }

        internal Catalog GetCatalog()
        {
            return Catalog;
        }

        internal Navigator GetNavigator()
        {
            return Navigator;
        }

        internal ItemManager GetItemManager()
        {
            return ItemManager;
        }

        internal RoomManager GetRoomManager()
        {
            return RoomManager;
        }

        internal AdvertisementManager GetAdvertisementManager()
        {
            return AdvertisementManager;
        }

        internal PixelManager GetPixelManager()
        {
            return PixelManager;
        }

        internal AchievementManager GetAchievementManager()
        {
            return AchievementManager;
        }

        internal ModerationTool GetModerationTool()
        {
            return ModerationTool;
        }

        internal BotManager GetBotManager()
        {
            return BotManager;
        }

        internal InventoryGlobal GetInventory()
        {
            return globalInventory;
        }

        internal QuestManager GetQuestManager()
        {
            return questManager;
        }

        internal GroupManager GetGroupManager()
        {
            return groupManager;
        }
        #endregion

        #region Boot
        internal Game(int conns)
        {
            ClientManager = new GameClientManager();

            //if (FirewindEnvironment.GetConfig().data["client.ping.enabled"] == "1")
            //{
            //    ClientManager.StartConnectionChecker();
            //}

            
            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                
                //FirewindEnvironment.GameInstance = this;
                DateTime start = DateTime.Now;

                BanManager = new ModerationBanManager();
                RoleManager = new RoleManager();
                Catalog = new Catalog();
                Navigator = new Navigator();
                ItemManager = new ItemManager();
                RoomManager = new RoomManager();
                AdvertisementManager = new AdvertisementManager();
                PixelManager = new PixelManager();
                
                ModerationTool = new ModerationTool();
                BotManager = new BotManager();
                questManager = new QuestManager();
                //soundMachineManager = new SoundMachineManager();

                TimeSpan spent = DateTime.Now - start;

                Logging.WriteLine("Class initialization -> READY! (" + spent.Seconds + " s, " + spent.Milliseconds + " ms)");
            }
        }
        internal void ContinueLoading()
        {
            DateTime Start;
            TimeSpan TimeUsed;
            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                Start = DateTime.Now;
                BanManager.LoadBans(dbClient);
                TimeUsed = DateTime.Now - Start;
                Logging.WriteLine("Ban manager -> READY! (" + TimeUsed.Seconds + " s, " + TimeUsed.Milliseconds + " ms)");

                Start = DateTime.Now;
                //RoleManager.LoadRoles(dbClient);
                RoleManager.LoadRights(dbClient);
                TimeUsed = DateTime.Now - Start;
                Logging.WriteLine("Role manager -> READY! (" + TimeUsed.Seconds + " s, " + TimeUsed.Milliseconds + " ms)");

                Start = DateTime.Now;
                Catalog.Initialize(dbClient);
                TimeUsed = DateTime.Now - Start;
                Logging.WriteLine("Catacache -> READY! (" + TimeUsed.Seconds + " s, " + TimeUsed.Milliseconds + " ms)");

                Start = DateTime.Now;
                Navigator.Initialize(dbClient);
                TimeUsed = DateTime.Now - Start;
                Logging.WriteLine("Navigator -> READY! (" + TimeUsed.Seconds + " s, " + TimeUsed.Milliseconds + " ms)");

                Start = DateTime.Now;
                ItemManager.LoadItems(dbClient);
                globalInventory = new InventoryGlobal();
                TimeUsed = DateTime.Now - Start;
                Logging.WriteLine("Item manager -> READY! (" + TimeUsed.Seconds + " s, " + TimeUsed.Milliseconds + " ms)");

                Start = DateTime.Now;
                RoomManager.LoadModels(dbClient);
                RoomManager.InitVotedRooms(dbClient);
                TimeUsed = DateTime.Now - Start;
                Logging.WriteLine("Room manager -> READY! (" + TimeUsed.Seconds + " s, " + TimeUsed.Milliseconds + " ms)");

                Start = DateTime.Now;
                Ranking.initRankings(dbClient);
                TimeUsed = DateTime.Now - Start;
                Logging.WriteLine("User rankings -> READY! (" + TimeUsed.Seconds + " s, " + TimeUsed.Milliseconds + " ms)");

                Start = DateTime.Now;
                AdvertisementManager.LoadRoomAdvertisements(dbClient);
                TimeUsed = DateTime.Now - Start;
                Logging.WriteLine("Adviserment manager -> READY! (" + TimeUsed.Seconds + " s, " + TimeUsed.Milliseconds + " ms)");

                Start = DateTime.Now;
                AchievementManager = new AchievementManager(dbClient);
                questManager.Initialize(dbClient);
                TimeUsed = DateTime.Now - Start;
                Logging.WriteLine("Achievement manager -> READY! (" + TimeUsed.Seconds + " s, " + TimeUsed.Milliseconds + " ms)");

                Start = DateTime.Now;
                ModerationTool.LoadMessagePresets(dbClient);
                ModerationTool.LoadPendingTickets(dbClient);
                TimeUsed = DateTime.Now - Start;
                Logging.WriteLine("Moderation tool -> READY! (" + TimeUsed.Seconds + " s, " + TimeUsed.Milliseconds + " ms)");

                Start = DateTime.Now;
                BotManager.LoadBots(dbClient);
                TimeUsed = DateTime.Now - Start;
                Logging.WriteLine("Bot manager manager -> READY! (" + TimeUsed.Seconds + " s, " + TimeUsed.Milliseconds + " ms)");

                Start = DateTime.Now;
                PetRace.Init(dbClient);
                TimeUsed = DateTime.Now - Start;
                Logging.WriteLine("Pet system -> READY! (" + TimeUsed.Seconds + " s, " + TimeUsed.Milliseconds + " ms)");

                Start = DateTime.Now;
                Catalog.InitCache();
                TimeUsed = DateTime.Now - Start;
                Logging.WriteLine("Catalogue manager -> READY! (" + TimeUsed.Seconds + " s, " + TimeUsed.Milliseconds + " ms)");

                Start = DateTime.Now;
                SongManager.Initialize();
                TimeUsed = DateTime.Now - Start;
                Logging.WriteLine("Sound manager -> READY! (" + TimeUsed.Seconds + " s, " + TimeUsed.Milliseconds + " ms)");


                //GuildsPartsData.InitGroups();
                //groupManager = new GroupManager(dbClient);


                Start = DateTime.Now;
                DatabaseCleanup(dbClient);
                LowPriorityWorker.Init(dbClient);
                TimeUsed = DateTime.Now - Start;
                Logging.WriteLine("Database -> Cleanup performed! (" + TimeUsed.Seconds + " s, " + TimeUsed.Milliseconds + " ms)");
            }

            StartGameLoop();

            Logging.WriteLine("Game manager -> READY!");
        }

        #endregion

        #region Game loop

        internal void StartGameLoop()
        {
            gameLoopActive = true;
            gameLoop = new Thread(new ThreadStart(MainGameLoop));
            gameLoop.Start();

            //gameLoopSUBRooms = new Thread(new ThreadStart(RoomManager.OnCycle_RoomDedicatedThread));
            //gameLoopSUBRooms.Start();

        }

        internal void StopGameLoop()
        {
            gameLoopActive = false;

            while (LowPriorityWorker_ended == false || RoomManagerCycle_ended == false || ClientManagerCycle_ended == false)
            {
                Thread.Sleep(gameLoopSleepTime);
            }
        }

        internal byte GameLoopStatus = 0;
        internal static bool gameLoopEnabled = true;

        private void MainGameLoop()
        {
            while (gameLoopActive)
            {
                DateTime startTaskTime;
                TimeSpan spentTime;
                startTaskTime = DateTime.Now;
                if (gameLoopEnabled)
                {
                    try
                    {
                        LowPriorityWorker_ended = false;
                        RoomManagerCycle_ended = false;
                        ClientManagerCycle_ended = false;
                        if (FirewindEnvironment.SeparatedTasksInMainLoops != true)
                        {
                            GameLoopStatus = 1;
                            LowPriorityWorker.Process(); //1 query
                            GameLoopStatus = 5;
                            RoomManager.OnCycle(); // Queries for furni save
                            GameLoopStatus = 6;
                            ClientManager.OnCycle();
                            GameLoopStatus = 7;
                            //groupManager.OnCycle();
                        }
                        else
                        {
                            if (LowPriorityWorker_task == null || LowPriorityWorker_task.IsCompleted == true)
                            {
                                LowPriorityWorker_task = new Task(LowPriorityWorker.Process);
                                LowPriorityWorker_task.Start();
                            }

                            if (RoomManagerCycle_task == null || RoomManagerCycle_task.IsCompleted == true)
                            {
                                RoomManagerCycle_task = new Task(RoomManager.OnCycle);
                                RoomManagerCycle_task.Start();
                            }

                            if (ClientManagerCycle_task == null || ClientManagerCycle_task.IsCompleted == true)
                            {
                                ClientManagerCycle_task = new Task(ClientManager.OnCycle);
                                ClientManagerCycle_task.Start();
                            }

                        }


                    }
                    catch (Exception e)
                    {
                        Logging.LogCriticalException("INVALID MARIO BUG IN GAME LOOP: " + e.ToString());
                    }
                    GameLoopStatus = 8;
                }
                spentTime = DateTime.Now - startTaskTime;
                if (spentTime.TotalSeconds > 3)
                {
                    Logging.LogDebug("Game.MainGameLoop spent: " + spentTime.TotalSeconds + " seconds in working.");
                }

                Thread.Sleep(gameLoopSleepTime);
            }

        }
        #endregion
        #region Pasaje de parámetros a thread dedicado
        internal bool gameLoopEnabled_EXT
        {
            get
            {
                return gameLoopEnabled;
            }
        }

        internal bool gameLoopActive_EXT
        {
            get
            {
                return gameLoopActive;
            }
        }

        internal int gameLoopSleepTime_EXT
        {
            get
            {
                return gameLoopSleepTime;
            }
        }
        #endregion
        #region Shutdown
        internal static void DatabaseCleanup(IQueryAdapter dbClient)
        {
            //dbClient.runFastQuery("TRUNCATE TABLE user_tickets");
            dbClient.runFastQuery("TRUNCATE TABLE user_online");
            dbClient.runFastQuery("TRUNCATE TABLE room_active");
            dbClient.runFastQuery("UPDATE server_status SET status = 1, users_online = 0, rooms_loaded = 0, server_ver = '" + FirewindEnvironment.PrettyVersion + "', stamp = '" + FirewindEnvironment.GetUnixTimestamp() + "' ");
        }

        internal void Destroy()
        {

            //Stop game thread

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                DatabaseCleanup(dbClient);
            }
            if (GetClientManager() != null)
            {
                //GetClientManager().Clear();
                //GetClientManager().StopConnectionChecker();
            }

            Logging.WriteLine("Destroyed Habbo Hotel.");
        }
        #endregion


        #region reload
        internal void reloaditems()
        {
            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                ItemManager.LoadItems(dbClient);
                globalInventory = new InventoryGlobal();
            }
        }

        #endregion

    }
}
