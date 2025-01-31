using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HabboEvents
{
    public static class Outgoing
    {
        public static int SendBannerMessageComposer = 1064;
        public static int SecretKeyComposer = 408;
        public static int Ping = 2023;
        public static int AuthenticationOK = 2382;
        public static int HomeRoom = 3756;
        public static int FavouriteRooms = 3464;
        public static int FavsUpdate = 997;
        public static int Fuserights = 1112;
        public static int CurrentMinimails = 1286;
        public static int AvailabilityStatus = 812;
        public static int InfoFeedEnable = 1343;
        public static int ActivityPoints = 845;
        public static int CreditBalance = 912;
        public static int UniqueID = 1374;
        public static int HabboInfomation = 3668;
        public static int ProfileInformation = 1720;
        public static int Allowances = 519;
        public static int AchievementPoints = 3450;
        public static int OpenShop = 846;
        public static int OpenShopPage = 3032;
        public static int ShopData1 = 1972;
        public static int ShopData2 = 1301;
        public static int Offer = 826;
        public static int PetRace = 2613;
        public static int CheckPetName = 2043;
        public static int UpdateShop = 1663;
        public static int AchievementList = 1872;
        public static int AchievementProgress = 2331;
        public static int UnlockAchievement = 2210;
        public static int OpenModTools = 2309;
        public static int SendNotif = 1543;
        public static int ModResponse = 3800;
        public static int BroadcastMessage = 3802;
        public static int GiftError = 2748;
        public static int UpdateInventary = 3164;
        public static int NavigatorPacket = 2375;
        public static int PublicCategories = 1458;
        public static int EffectsInventary = 2968;
        public static int AddEffectToInventary = 1330;
        public static int EnableEffect = 430;
        public static int StopEffect = 3307;
        public static int OpenHelpTool = 2962;
        public static int CanCreateRoom = 3587;
        public static int OnCreateRoomInfo = 2471;
        public static int RoomSettingsData = 214;
        public static int UpdateRoomOne = 554;
        public static int RoomAds = 2822;
        public static int PrepareRoomForUsers = 2466;
        public static int RoomErrorToEnter = 3673;
        public static int OutOfRoom = 450;
        public static int DoorBellNoPerson = 2458;
        public static int Doorbell = 1689;
        public static int InvalidDoorBell = 2458;
        public static int InitFriends = 2479;
        public static int InitRequests = 3143;
        public static int SendFriendRequest = 852;
        public static int FriendUpdate = 2800;
        public static int InstantChat = 1535;
        public static int InstantInvite = 2675;
        public static int InstantChatError = 1133;
        public static int RoomReady = 2371; // InitialRoomInformation
        public static int FollowBuddyError = 805;
        public static int SearchFriend = 2653;
        public static int TradeStart = 343;
        public static int TradeUpdate = 3145;
        public static int TradeAcceptUpdate = 278;
        public static int TradeComplete = 2831;
        public static int TradeCloseClean = 2499;
        public static int TradeClose = 2936;
        public static int RoomDecoration = 2841;
        public static int RoomRightsLevel = 3124;
        public static int HasOwnerRights = 3002;
        public static int RateRoom = 3710;
        public static int ScoreMeter = 3710;
        public static int CanCreateEvent = 2232;
        public static int RoomEvent = 1743;
        public static int HeightMap = 2644;
        public static int FloorHeightMap = 3482;
        public static int OpenGift = 1252;
        public static int Objects = 362;
        public static int SerializeWallItems = 801;
        public static int FloodFilter = 3153;
        public static int SetRoomUser = 780;
        public static int ConfigureWallandFloor = 2006;
        public static int SerializeClub = 3208;
        public static int ClubComposer = 3432;
        public static int PopularTags = 3132;
        public static int FlatCats = 1835;
        public static int Talk = 2383;
        public static int Shout = 2462;
        public static int Whisp = 3757;
        public static int UpdateIgnoreStatus = 2283;
        public static int GiveRespect = 899;
        public static int PrepareCampaing = 2322;
        public static int SendCampaingData = 1125;
        public static int UpdateUserInformation = 911;
        public static int Dance = 3951;
        public static int Action = 3622;
        public static int IdleStatus = 1268;
        public static int Inventory = 3306;
        public static int PetInventory = 416;
        public static int PlaceBot = 780;
        public static int PetInformation = 2967;
        public static int RespectPet = 1750;
        public static int AddExperience = 3123;
        public static int BadgesInventory = 1528;
        public static int UpdateBadges = 65;
        public static int GetUserBadges = 65;
        public static int GetUserTags = 2272;
        public static int GivePowers = 212;
        public static int RemovePowers = 701;
        public static int FlatControllerAdded = 658;
        public static int TypingStatus = 467;
        public static int RemoveObjectFromInventory = 1259;
        public static int AddWallItemToRoom = 1739;
        public static int UpdateWallItemOnRoom = 2540;
        public static int PickUpWallItem = 342;
        public static int UserLeftRoom = 1325;
        public static int RoomTool = 1569;
        public static int UserTool = 628;
        public static int RoomChatlog = 2582;
        public static int UserChatlog = 2409;
        public static int RoomVisits = 436; // LIE, NOT ROOMVISITS, A WEIRD THING, EXACTLY LIKE 'FOLLOW TO ROOM OR STH?'
        public static int IssueChatlog = 1994;
        public static int SerializeIssue = 3285;
        public static int UpdateIssue = 645;
        public static int ApplyEffects = 2807;
        public static int ApplyCarryItem = 1750;
        public static int ObjectOnRoller = 2787;
        public static int DimmerData = 3256;
        public static int OpenPostIt = 1222;
        public static int UpdateFreezeLives = 1166;
        public static int WardrobeData = 2355;
        public static int HelpRequest = 230; // don't know at all
        public static int SerializeCompetitionWinners = 2344;

        public static int RecyclePrizes = 1244;
        public static int PetRespectNotification = 3692;
        public static int PetAddedToInventory = 3947;
        public static int MOTDNotification = 3677;

        // Wired
        public static int WiredFurniTrigger = 1153; // causante
        public static int WiredEffect = 21;
        public static int WiredCondition = 2626;
        public static int SaveWired = 3725;

        // Floor item IDs
        public static int ObjectAdd = 2302;
        public static int ObjectRemove = 2238;
        public static int ObjectUpdate = 2206;
        public static int ObjectDataUpdate = 41;

        // For multiple items
        public static int ObjectsDataUpdate = 1208;
        //2295 _-1qv _-09Y DiceValue
        //2357 _-4Ng _-1-M OneWayDoorStatus

        // Interaction
        public static int OneWayDoorStatus = 2357;

        public static int UserUpdate = 172;

        // Room enter
        public static int RoomEntryInfo = 2877;
        public static int NoSuchFlat = 1378;
        public static int FurnitureAliases = 1765;
        public static int GetGuestRoomResult = 542;

        public static int RoomForward = 2022;

        // Door bell
        public static int FlatAccessible = 368;

        // Disconnection
        public static int DisconnectReason = 4000;

        // Groups/guilds
        public static int PurchaseGuildInfo = 3341;
        public static int GuildEditorData = 1725;
        public static int HabboGroupJoinFailed = 2407;
        public static int GroupInfo = 2602;
        public static int OwnGuilds = 1463;
        public static int GroupCreated = 1327;

        // User
        public static int SoundSettings = 89;
        public static int WelcomeBack = 3015;

        // Pets
        public static int PetCommands = 2603;

        // Messenger
        public static int MessengerError = 3736;

        // Engine
        public static int GenericError = 3192;

        // Vouchers
        public static int VoucherRedeemError = 3694;
        public static int VoucherRedeemOk = 3049;

        // Inventory
        public static int PetRemovedFromInventory = 3225;

        // Catalog
        public static int NotEnoughBalance = 3269;
        public static int PurchaseError = 277;
        public static int PurchaseOK = 2069;
        public static int UnseenItems = 135;

        // Trax/Jukebox
        public static int JukeboxSongDisks = 1211;
        public static int TraxSongInfo = 1882;
    }
}
