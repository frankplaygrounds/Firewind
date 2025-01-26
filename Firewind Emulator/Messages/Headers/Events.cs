using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HabboEvents
{
    public static class Incoming
    {
        public static int CheckReleaseMessageEvent = 4000;
        public static int InitCrypto = 712;
        public static int SecretKey = 205;
		public static int ClientVars = 1903;
		public static int UniqueMachineID = 1527;
		public static int SSOTicket = 1913;
		public static int MachineInformation = 181;
		public static int OnEventHappend = 2087;
		public static int UserInformation = 1551;
        public static int ScrGetUserInfo = 2783;
		public static int LoadCategorys = 1946;
        public static int Pong = 2168;
		public static int PingInternval = 530;
		public static int OpenHelpTool = 2251;
		public static int GetWardrobe = 902;
		public static int SaveWardrobe = 222;
		public static int OpenAchievements = 137;
		public static int OpenGift = 3897;
		public static int StartEffect = 2996;
		public static int EnableEffect = 2989;
		public static int RedeemVoucher = 1771;
		public static int LoadFeaturedRooms = 3598;
        public static int PopularRoomsSearch = 2704;
		public static int LoadMyRooms = 1459;
		public static int LoadPopularTags = 3569;
		public static int RoomsWhereMyFriends = 3740;
		public static int RoomsOfMyFriends = 1110;
		public static int MyFavs = 1035;
		public static int RecentRooms = 2435;
		public static int HighRatedRooms = 720;
		public static int SearchRoomByName = 542;
		public static int CanCreateRoom = 2933;
		public static int CreateRoom = 1665;
        public static int GetRoomSettings = 65;
        public static int SaveRoomSettings = 384;
        public static int OpenFlatConnection = 1730;
		public static int LookTo = 3671;
		public static int StartTrade = 1644;
		public static int SendOffer = 882;
		public static int CancelOffer = 907;
		public static int AcceptTrade = 3435;
		public static int UnacceptTrade = 1960;
		public static int ConfirmTrade = 3147;
		public static int CancelTrade = 937;
		public static int Move = 970;
		public static int Talk = 1972;
		public static int Shout = 3277;
		public static int SendRespects = 1194;
		public static int PrepareCampaing = 2288;
		public static int SendCampaingData = 2058;
		public static int ChangeLook = 3899;
		public static int ChangeMotto = 787;
		public static int ApplyDance = 2991;
		public static int ApplySign = 295;
		public static int ApplyAction = 2078;
		public static int GetFriends = 1615;
		public static int FriendRequest = 1361;
		public static int AcceptRequest = 3730;
        public static int DeclineFriend = 1906;
		public static int SendInstantMessenger = 1573;
		public static int FollowFriend = 505;
		public static int UpdateFriendsState = 2557;
		public static int InviteFriendsToMyRoom = 1503;
		public static int SearchFriend = 859;
		public static int DeleteFriend = 3045;
		public static int LoadProfile = 1412;
		public static int OpenInventory = 2710;
		public static int ApplySpace = 155;
		public static int AddFloorItem = 1861;
		public static int MoveOrRotate = 1688;
		public static int MoveWall = 1033;
		public static int HandleItem = 657;
		public static int HandleWallItem = 3288;
		public static int StartMoodlight = 3455;
		public static int TurnOnMoodlight = 51;
		public static int ApplyMoodlightChanges = 1456;
		public static int AddPostIt = 3548;
		public static int OpenPostIt = 294;
		public static int SavePostIt = 1181;
		public static int DeletePostIt = 3321;
		public static int CanCreateRoomEvent = 1832;
		public static int CreateRoomEvent = 1416;
		public static int StopEvent = 3386;
		public static int EditEvent = 1047;
		public static int SetHome = 2102;
		public static int RemoveRoom = 2355;
		public static int RedeemExchangeFurni = 894;
		public static int StartTyping = 839;
		public static int StopTyping = 1096;
		public static int GiveRights = 3768;
		public static int RemoveAllRights = 1479;
        public static int RemoveRights = 2990;
		public static int PickupItem = 1128;
		public static int SaveWiredTrigger = 981;
		public static int SaveWiredEffect = 682;
		public static int ToolForThisRoom = 514;
		public static int ToolForUser = 2452;
		public static int GetRoomVisits = 2973;
		public static int UserChatlog = 3799;
		public static int SendMessageByTemplate = 414;
		public static int ModActionKickUser = 1376;
		public static int ModActionMuteUser = 1993;
		public static int ModActionBanUser = 1217;
		public static int SendUserMessage = 2732;
		public static int PerformRoomAction = 2090;
		public static int CreateTicket = 3468;
		public static int PickIssue = 1641;
		public static int IssueChatlog = 2156;
        public static int CloseIssues = 3087;
		public static int ReleaseIssue = 2294;
		public static int OpenRoomChatlog = 96;
		public static int GiveRoomScore = 1173;
		public static int SendRoomAlert = 126;
		public static int KickUserOfRoom = 18;
		public static int BanUserOfRoom = 2790;
		public static int IgnoreUser = 2481;
		public static int UnignoreUser = 2560;
		public static int AnswerDoorBell = 2637;
		public static int AddFavourite = 2575;
		public static int RemoveFavourite = 2788;
		public static int Whisp = 3059;
		public static int BadgesInventary = 148;
		public static int BotsInventary = 1642;
		public static int ApplyBadge = 1321;
		public static int GetUserBadges = 1699;
		public static int GetUserTags = 2253;
		public static int GoToHotelView = 2162;
        public static int RemoveHanditem = 3843;
        public static int GiveObject = 2566;
		public static int FindFriends = 2441;
        public static int GetRecyclerPrizes = 3526;

        public static int ThrowDice = 2372;
        public static int DiceOff = 301;

        public static int MannequeNameChange = 2036;
        public static int MannequeFigureChange = 2868;

        public static int SetAdParameters = 358;

        // Room loading
        public static int GetGuestRoom = 798; //+int(roomid)
        public static int GetFurnitureAliases = 2242;
        public static int GetHabboGroupBadges = 2709;
        public static int GetRoomEntryData = 1081;
		public static int GetRoomAd = 3873;

        // Door bell
        public static int GoToFlat = 3734;

        // Furniture
        public static int EnterOneWayDoor = 2505;

        public static int UseWallItem = 3793;

        // Groups
        public static int StartGuildPurchase = 1137;
        public static int GetGuildInfo = 1660;
        public static int CreateGuild = 2282;

        // Engine
        public static int EventLog = 2087;
        public static int PerformanceLog = 7;

        // User
        public static int GetCreditsInfo = 3249;
        public static int GetBadgePointLimits = 2344;
        public static int GetSoundSettings = 3995;

        // Pets
        public static int GetPetInventory = 3127;
        public static int PlacePet = 461;
        public static int PickupPet = 2378;
        public static int RespetPet = 112;
        public static int AddSaddleToPet = 243;
        public static int RemoveSaddle = 809;
        public static int MountOnPet = 296;
        public static int PetInfo = 303;
        public static int GetPetCommands = 2687;

        // Catalog
        public static int GetCatalogIndex = 2053;
        public static int GetMarketplaceConfiguration = 1937;
        public static int CatalogData2 = 3355;
        public static int GetSomethingUnknown = 3509;
        public static int OpenCatalogPage = 991;
        public static int CatalogGetRace = 2738;
        public static int CheckPetName = 926;
        public static int PurchaseCatalogItem = 1581;
        public static int PurchaseFromCatalogAsGift = 1574;

        // Bots
        public static int GetBotInventory = 1642;
        public static int PlaceBot = 829;

		// Wired
		public static int UpdateCondition = 2013;

		// Trax/jukebox
		public static int GetSoundMachinePlayList = 2520;
		public static int GetSongInfo = 772;


		public static int AnswerQuestion = 1251;
	}
}
