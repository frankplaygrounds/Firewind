using System;
using System.Collections.Generic;
using System.Drawing;
using Firewind.Core;
using Firewind.HabboHotel.Users;
using Firewind.HabboHotel.Items.Interactors;
using Firewind.HabboHotel.Pathfinding;
using Firewind.HabboHotel.Rooms;
using Firewind.HabboHotel.Rooms.Games;
using Firewind.HabboHotel.Rooms.Wired;
using Firewind.Messages;
using Firewind.HabboHotel.Rooms.Wired.WiredHandlers.Interfaces;
using HabboEvents;
using Database_Manager.Database.Session_Details.Interfaces;


namespace Firewind.HabboHotel.Items
{
    public delegate void OnItemTrigger(object sender, ItemTriggeredArgs e);
   
    public class RoomItem : IEquatable<RoomItem>
    {
        internal UInt32 Id;
        internal UInt32 RoomId;
        internal UInt32 BaseItem;
        internal string Figure;
        internal string Gender;
        internal uint interactingBallUser;
        internal Team team;
        internal byte interactionCountHelper;
        public byte interactionCount;
        internal int value;
        internal FreezePowerUp freezePowerUp;
        internal IWiredCondition wiredCondition;
        internal IRoomItemData originalExtraData;

        internal IWiredTrigger wiredHandler;
        internal event OnItemTrigger itemTriggerEventHandler;
        internal event UserWalksFurniDelegate OnUserWalksOffFurni;
        internal event UserWalksFurniDelegate OnUserWalksOnFurni;

        private Dictionary<int, ThreeDCoord> mAffectedPoints;
        private Point placedPosition;

        internal IRoomItemData data;
        internal int Extra;

        internal Point GetPlacementPosition()
        {
            return placedPosition;
        }

        internal Dictionary<int, ThreeDCoord> GetAffectedTiles
        {
            get
            {
                return mAffectedPoints;
            }
        }

        private int mX; //byte
        internal int GetX
        {
            get
            {
                return mX;
            }
        }

        private int mY;//byte

        internal int GetY
        {
            get
            {
                return mY;
            }
        }

        private Double mZ; //Float??

        internal Double GetZ
        {
            get
            {
                return mZ;
            }
        }

        internal void SetState(int pX, int pY, Double pZ, Dictionary<int, ThreeDCoord> Tiles)
        {
            this.mX = pX;
            this.mY = pY;
            if (!double.IsInfinity(pZ))
            {
                this.mZ = pZ;
            }
            this.mAffectedPoints = Tiles;
        }

        internal int Rot;//byte

        internal WallCoordinate wallCoord;

        private bool updateNeeded;
        internal bool UpdateNeeded
        {
            get
            {
                return updateNeeded;
            }
            set
            {
                if (value == true)
                    GetRoom().GetRoomItemHandler().QueueRoomItemUpdate(this);
                updateNeeded = value;
            }
        }
        internal int UpdateCounter; //byte

        internal UInt32 InteractingUser;
        internal UInt32 InteractingUser2;

        private Item mBaseItem;
        private Room mRoom;
        private bool mIsWallItem;
        private bool mIsFloorItem;

        private bool mIsRoller;
        internal bool IsTrans;
        internal bool pendingReset = false;
        internal bool MagicRemove = false;

        internal bool IsRoller
        {
            get
            {
                return mIsRoller;
            }
        }

        internal Point Coordinate
        {
            get
            {
                return new Point(mX, mY);
            }
        }

        internal List<Point> GetCoords
        {
            get
            {
                List<Point> toReturn = new List<Point>();
                toReturn.Add(Coordinate);

                foreach (ThreeDCoord tile in mAffectedPoints.Values)
                {
                    toReturn.Add(new Point(tile.X, tile.Y));
                }

                return toReturn;
            }
        }

        internal double TotalHeight
        {
            get
            {
                return mZ + GetBaseItem().Height;
            }
        }

        internal bool IsWallItem
        {
            get
            {
                return mIsWallItem;
            }
        }

        internal bool IsFloorItem
        {
            get
            {
                return mIsFloorItem;
            }
        }

        internal Point SquareInFront
        {
            get
            {
                Point Sq = new Point(mX, mY);

                if (Rot == 0)
                {
                    Sq.Y--;
                }
                else if (Rot == 2)
                {
                    Sq.X++;
                }
                else if (Rot == 4)
                {
                    Sq.Y++;
                }
                else if (Rot == 6)
                {
                    Sq.X--;
                }

                return Sq;
            }
        }

        internal Point SquareBehind
        {
            get
            {
                Point Sq = new Point(mX, mY);

                if (Rot == 0)
                {
                    Sq.Y++;
                }
                else if (Rot == 2)
                {
                    Sq.X--;
                }
                else if (Rot == 4)
                {
                    Sq.Y--;
                }
                else if (Rot == 6)
                {
                    Sq.X++;
                }

                return Sq;
            }
        }

        internal FurniInteractor Interactor
        {
            get
            {
                switch (GetBaseItem().InteractionType)
                {
                    case InteractionType.teleport:
                        return new InteractorTeleport();
                    case InteractionType.bottle:
                        return new InteractorSpinningBottle();
                    case InteractionType.dice:
                        return new InteractorDice();
                    case InteractionType.habbowheel:
                        return new InteractorHabboWheel();
                    case InteractionType.loveshuffler:
                        return new InteractorLoveShuffler();
                    case InteractionType.onewaygate:
                        return new InteractorOneWayGate();
                    case InteractionType.alert:
                        return new InteractorAlert();
                    case InteractionType.vendingmachine:
                        return new InteractorVendor();
                    case InteractionType.gate:
                        return new InteractorGate(GetBaseItem().Modes);
                    case InteractionType.guilddoor:
                        return new InteractorGuildGate();
                    case InteractionType.scoreboard:
                        return new InteractorScoreboard();
                    case InteractionType.football:
                        return new InteractorFootball();
                    case InteractionType.footballcounterblue:
                    case InteractionType.footballcountergreen:
                    case InteractionType.footballcounterred:
                    case InteractionType.footballcounteryellow:
                        return new InteractorScoreCounter();
                    case InteractionType.banzaicounter:
                        return new InteractorBanzaiTimer();
                    case InteractionType.banzaipuck:
                        return new InteractorBanzaiPuck();
                    case InteractionType.banzaifloor:
                        return new InteractorNone();
                    case InteractionType.banzaiscoreblue:
                    case InteractionType.banzaiscoregreen:
                    case InteractionType.banzaiscorered:
                    case InteractionType.banzaiscoreyellow:
                        return new InteractorBanzaiScoreCounter();
                    case InteractionType.freezetimer:
                        return new InteractorFreezeTimer();
                    case InteractionType.freezetile:
                    case InteractionType.freezetileblock:
                        return new InteractorFreezeTile();
                    case InteractionType.triggertimer:
                    case InteractionType.triggerroomenter:
                    case InteractionType.triggergameend:
                    case InteractionType.triggergamestart:
                    case InteractionType.triggerrepeater:
                    case InteractionType.triggeronusersay:
                    case InteractionType.triggerscoreachieved:
                    case InteractionType.triggerstatechanged:
                    case InteractionType.triggerwalkonfurni:
                    case InteractionType.triggerwalkofffurni:
                    case InteractionType.actiongivescore:
                    case InteractionType.actionposreset:
                    case InteractionType.actionmoverotate:
                    case InteractionType.actionresettimer:
                    case InteractionType.actionshowmessage:
                    case InteractionType.actionteleportto:
                    case InteractionType.actiontogglestate:
                    case InteractionType.conditionfurnishaveusers:
                    case InteractionType.conditionstatepos:
                    case InteractionType.conditiontimelessthan:
                    case InteractionType.conditiontimemorethan:
                    case InteractionType.conditiontriggeronfurni:
                    case InteractionType.specialrandom:
                    case InteractionType.specialunseen:
                        return new WiredInteractor();
                    case InteractionType.wire:
                    case InteractionType.wireCenter:
                    case InteractionType.wireCorner:
                    case InteractionType.wireSplitter:
                    case InteractionType.wireStandard:
                        return new InteractorIgnore();
                    case InteractionType.jukebox:
                        return new InteractorJukebox();
                    case InteractionType.mannequin:
                        return new InteractorMannequin();
                    case InteractionType.puzzlebox:
                        return new InteractorPuzzleBox();

                    case InteractionType.none:
                    default:
                        return new InteractorGenericSwitch(GetBaseItem().Modes);
                }
            }
        }

        internal void OnTrigger(RoomUser user)
        {
            if (itemTriggerEventHandler != null)
                itemTriggerEventHandler(null, new ItemTriggeredArgs(user, this));
        }

        internal RoomItem(UInt32 Id, UInt32 RoomId, UInt32 BaseItem, IRoomItemData data, int extra, int X, int Y, Double Z, int Rot, Room pRoom)
        {
            this.Id = Id;
            this.RoomId = RoomId;
            this.BaseItem = BaseItem;
            //this.ExtraData = data;
            this.originalExtraData = data;
            this.data = data;
            this.Extra = extra;
            this.mX = X;
            this.mY = Y;
            if(!double.IsInfinity(Z))
                this.mZ = Z;
            this.Rot = Rot;
            this.UpdateNeeded = false;
            this.UpdateCounter = 0;
            this.InteractingUser = 0;
            this.InteractingUser2 = 0;
            this.IsTrans = false;
            this.interactingBallUser = 0;
            this.interactionCount = 0;
            this.value = 0;
            this.placedPosition = new Point(X, Y);

            mBaseItem = FirewindEnvironment.GetGame().GetItemManager().GetItem(BaseItem);
            mRoom = pRoom; //Todo: rub my penis

            if (GetBaseItem() == null)
                Logging.LogException("Unknown baseID: " + BaseItem);


            switch (GetBaseItem().InteractionType)
            {
                case InteractionType.teleport:
                    IsTrans = true;
                    ReqUpdate(0, true);
                    break;

                case InteractionType.roller:
                    mIsRoller = true;
                    pRoom.GetRoomItemHandler().GotRollers = true;
                    break;

                case InteractionType.banzaiscoreblue:
                case InteractionType.footballcounterblue:
                case InteractionType.banzaigateblue:
                case InteractionType.freezebluegate:
                case InteractionType.freezebluecounter:
                    team = Team.blue;
                    break;

                case InteractionType.banzaiscoregreen:
                case InteractionType.footballcountergreen:
                case InteractionType.banzaigategreen:
                case InteractionType.freezegreencounter:
                case InteractionType.freezegreengate:
                    team = Team.green;
                    break;

                case InteractionType.banzaiscorered:
                case InteractionType.footballcounterred:
                case InteractionType.banzaigatered:
                case InteractionType.freezeredcounter:
                case InteractionType.freezeredgate:
                    team = Team.red;
                    break;

                case InteractionType.banzaiscoreyellow:
                case InteractionType.footballcounteryellow:
                case InteractionType.banzaigateyellow:
                case InteractionType.freezeyellowcounter:
                case InteractionType.freezeyellowgate:
                    team = Team.yellow;
                    break;

                case InteractionType.banzaitele:
                    {
                        this.data = new StringData("");
                        break;
                    }
            }
            
            mIsWallItem = (GetBaseItem().Type.ToString().ToLower() == "i");
            mIsFloorItem = (GetBaseItem().Type.ToString().ToLower() == "s");
            mAffectedPoints = Gamemap.GetAffectedTiles(GetBaseItem().Length, GetBaseItem().Width, mX, mY, Rot);
        }

        internal RoomItem(UInt32 Id, UInt32 RoomId, UInt32 BaseItem, IRoomItemData data, int extra, WallCoordinate wallCoord, Room pRoom)
        {
            this.Id = Id;
            this.RoomId = RoomId;
            this.BaseItem = BaseItem;
            this.data = data;
            this.Extra = extra;
            this.originalExtraData = data;
            this.mX = 0;
            this.mY = 0;
            this.mZ = 0.0;
            this.UpdateNeeded = false;
            this.UpdateCounter = 0;
            this.InteractingUser = 0;
            this.InteractingUser2 = 0;
            this.IsTrans = false;
            this.interactingBallUser = 0;
            this.interactionCount = 0;
            this.value = 0;
            this.placedPosition = new Point(0, 0);
            this.wallCoord = wallCoord;

            mBaseItem = FirewindEnvironment.GetGame().GetItemManager().GetItem(BaseItem);
            mRoom = pRoom;

            if (GetBaseItem() == null)
                Logging.LogException("Unknown baseID: " + BaseItem);

            mIsWallItem = true;
            mIsFloorItem = false;
            mAffectedPoints = new Dictionary<int, ThreeDCoord>();
        }

        internal void Destroy()
        {
            mRoom = null;
            mAffectedPoints.Clear();

            if (wiredHandler != null)
                wiredHandler.Dispose();
            wiredHandler = null;
            itemTriggerEventHandler = null;
            OnUserWalksOffFurni = null;
            OnUserWalksOnFurni = null;
        }

        public bool Equals(RoomItem comparedItem)
        {
            return (comparedItem.Id == this.Id);
        }

        internal void ProcessUpdates()
        {
            this.UpdateCounter--;

            if (this.UpdateCounter <= 0 || IsTrans)
            {
                this.UpdateNeeded = false;
                this.UpdateCounter = 0;

                RoomUser User = null;
                RoomUser User2 = null;

                switch (GetBaseItem().InteractionType)
                {
                    case InteractionType.gift:
                        // do nothing
                        break;
                    case InteractionType.onewaygate:

                        User = null;

                        if (InteractingUser > 0)
                        {
                            User = GetRoom().GetRoomUserManager().GetRoomUserByHabbo(InteractingUser);
                            //GetRoom().FreeSqareForUsers(mX, mY);
                        }

                        if (User != null && User.Coordinate == SquareBehind)
                        {
                            User.UnlockWalking();

                            //data =  new StringData("0");
                            InteractingUser = 0;

                            ServerMessage update = new ServerMessage(Outgoing.OneWayDoorStatus);
                            update.AppendUInt(Id); // id
                            update.AppendInt32(0); // status
                            mRoom.SendMessage(update);
                        }
                        else if (data.ToString() == "1")
                        {
                            //data =  new StringData("0");

                            ServerMessage update = new ServerMessage(Outgoing.OneWayDoorStatus);
                            update.AppendUInt(Id); // id
                            update.AppendInt32(0); // status
                            mRoom.SendMessage(update);
                        }

                        if (User == null)
                        {
                            InteractingUser = 0;
                        }

                        break;

                    case InteractionType.teleport:
                        User = null;
                        User2 = null;

                        bool keepDoorOpen = false;
                        bool showTeleEffect = false;

                        // Do we have a primary user that wants to go somewhere?
                        if (InteractingUser > 0)
                        {
                            User = GetRoom().GetRoomUserManager().GetRoomUserByHabbo(InteractingUser);

                            // Is this user okay?
                            if (User != null)
                            {
                                // Is he in the tele?
                                if (User.Coordinate == Coordinate)
                                {
                                    //Remove the user from the square
                                    User.AllowOverride = false;

                                    if (TeleHandler.IsTeleLinked(Id, mRoom))
                                    {
                                        showTeleEffect = true;

                                        if (true)
                                        {
                                            // Woop! No more delay.
                                            uint TeleId = TeleHandler.GetLinkedTele(Id, mRoom);
                                            uint RoomId = TeleHandler.GetTeleRoomId(TeleId, mRoom);

                                            // Do we need to tele to the same room or gtf to another?
                                            if (RoomId == this.RoomId)
                                            {
                                                RoomItem Item = GetRoom().GetRoomItemHandler().GetItem(TeleId);

                                                if (Item == null)
                                                {
                                                    User.UnlockWalking();
                                                }
                                                else
                                                {


                                                    // Set pos
                                                    User.SetPos(Item.GetX, Item.GetY, Item.GetZ);
                                                    User.SetRot(Item.Rot, false);

                                                    // Force tele effect update (dirty)
                                                    Item.data = new StringData("2");
                                                    Item.UpdateState(false, true);

                                                    // Set secondary interacting user
                                                    Item.InteractingUser2 = InteractingUser;
                                                }
                                            }
                                            else
                                            {
                                                // Let's run the teleport delegate to take futher care of this.. WHY DARIO?!
                                                if (!User.IsBot && User != null && User.GetClient() != null && User.GetClient().GetHabbo() != null && User.GetClient().GetMessageHandler() != null)
                                                {
                                                    User.GetClient().GetHabbo().IsTeleporting = true;
                                                    User.GetClient().GetHabbo().TeleportingRoomID = RoomId;
                                                    User.GetClient().GetHabbo().TeleporterId = TeleId;
                                                    User.GetClient().GetMessageHandler().ForwardToRoom((int)RoomId);
                                                }
                                                else
                                                {
                                                }
                                                    //FirewindEnvironment.GetGame().GetRoomManager().AddTeleAction(new TeleUserData(User.GetClient().GetMessageHandler(), User.GetClient().GetHabbo(), RoomId, TeleId));
                                            }

                                            // We're done with this tele. We have another one to bother.
                                            InteractingUser = 0;
                                        }
                                        else
                                        {
                                            // We're linked, but there's a delay, so decrease the delay and wait it out.
                                            //User.TeleDelay--;
                                        }
                                    }
                                    else
                                    {
                                        // This tele is not linked, so let's gtfo.
                                        User.UnlockWalking();
                                        InteractingUser = 0;
                                    }
                                }
                                // Is he in front of the tele?
                                else if (User.Coordinate == SquareInFront)
                                {
                                    User.AllowOverride = true;
                                    // Open the door
                                    keepDoorOpen = true;

                                    // Lock his walking. We're taking control over him. Allow overriding so he can get in the tele.
                                    if (User.IsWalking && (User.GoalX != mX || User.GoalY != mY))
                                    {
                                        User.ClearMovement(true);
                                    }

                                    User.CanWalk = false;
                                    User.AllowOverride = true;

                                    // Move into the tele
                                    User.MoveTo(Coordinate.X, Coordinate.Y, true);
                                }
                                // Not even near, do nothing and move on for the next user.
                                else
                                {
                                    InteractingUser = 0;
                                }
                            }
                            else
                            {
                                // Invalid user, do nothing and move on for the next user. 
                                InteractingUser = 0;
                            }
                        }

                        // Do we have a secondary user that wants to get out of the tele?
                        if (InteractingUser2 > 0)
                        {
                            User2 = GetRoom().GetRoomUserManager().GetRoomUserByHabbo(InteractingUser2);

                            // Is this user okay?
                            if (User2 != null)
                            {
                                // If so, open the door, unlock the user's walking, and try to push him out in the right direction. We're done with him!
                                keepDoorOpen = true;
                                User2.UnlockWalking();
                                User2.MoveTo(SquareInFront);
                            }

                            // This is a one time thing, whether the user's valid or not.
                            InteractingUser2 = 0;
                        }

                        // Set the new item state, by priority
                        if (keepDoorOpen)
                        {
                            if (data.ToString() != "1")
                            {
                                data =  new StringData("1");
                                UpdateState(false, true);
                            }
                        }
                        else if (showTeleEffect)
                        {
                            if (data.ToString() != "2")
                            {
                                data =  new StringData("2");
                                UpdateState(false, true);
                            }
                        }
                        else
                        {
                            if (data.ToString() != "0")
                            {
                                data =  new StringData("0");
                                UpdateState(false, true);
                            }
                        }

                        // We're constantly going!
                        ReqUpdate(1, false);

                        break;

                    case InteractionType.bottle:

                        data =  new StringData(FirewindEnvironment.GetRandomNumber(0, 7).ToString());
                        UpdateState();
                        break;

                    case InteractionType.dice:

                        data =  new StringData(FirewindEnvironment.GetRandomNumber(1, 6).ToString());
                        UpdateState();
                        break;

                    case InteractionType.habbowheel:

                        data =  new StringData(FirewindEnvironment.GetRandomNumber(1, 10).ToString());
                        UpdateState();
                        break;

                    case InteractionType.loveshuffler:

                        if (Convert.ToString(data.GetData()) == "0")
                        {
                            data =  new StringData(FirewindEnvironment.GetRandomNumber(1, 4).ToString());
                            ReqUpdate(20, false);
                        }
                        else if (Convert.ToString(data.GetData()) != "-1")
                        {
                            data =  new StringData("-1");
                        }

                        UpdateState(false, true);
                        break;

                    case InteractionType.alert:

                        if (Convert.ToString(data.GetData()) == "1")
                        {
                            data =  new StringData("0");
                            UpdateState(false, true);
                        }

                        break;

                    case InteractionType.vendingmachine:

                        if (Convert.ToString(data.GetData()) == "1")
                        {
                            User = GetRoom().GetRoomUserManager().GetRoomUserByHabbo(InteractingUser);

                            if (User != null)
                            {
                                User.UnlockWalking();

                                int randomDrink = GetBaseItem().VendingIds[FirewindEnvironment.GetRandomNumber(0, (GetBaseItem().VendingIds.Count - 1))];
                                User.CarryItem(randomDrink);
                            }

                            this.InteractingUser = 0;
                            data =  new StringData("0");

                            UpdateState(false, true);
                        }

                        break;


                    case InteractionType.scoreboard:
                        {
                            if (string.IsNullOrEmpty((string)data.GetData()))
                                break;

                            
                            int seconds = 0;

                            try
                            {
                                seconds = int.Parse((string)data.GetData());
                            }
                            catch { }

                            if (seconds > 0)
                            {
                                if (interactionCountHelper == 1)
                                {
                                    seconds--;
                                    interactionCountHelper = 0;

                                    data = new StringData(seconds.ToString());
                                    UpdateState();
                                }
                                else
                                    interactionCountHelper ++;

                                UpdateCounter = 1;
                            }
                            else
                                UpdateCounter = 0;

                            break;
                        }

                    case InteractionType.banzaicounter:
                        {
                            if (string.IsNullOrEmpty((string)data.GetData()))
                                break;

                            int seconds = 0;

                            try
                            {
                                seconds = int.Parse((string)data.GetData());
                            }
                            catch { }

                            if (seconds > 0)
                            {
                                if (interactionCountHelper == 1)
                                {
                                    seconds--;
                                    interactionCountHelper = 0;

                                    if (GetRoom().GetBanzai().isBanzaiActive)
                                    {
                                        ((StringData)data).Data = seconds.ToString();
                                        UpdateState();
                                    }
                                    else
                                        break;
                                }
                                else
                                    interactionCountHelper++;

                                UpdateCounter = 1;
                            }
                            else
                            {
                                UpdateCounter = 0;
                                GetRoom().GetBanzai().BanzaiEnd();
                            }

                            break;
                        }

                    case InteractionType.banzaitele:
                        {

                            ((StringData)data).Data = string.Empty;
                            UpdateState();
                            break;
                        }
                    /*
3 = Red 1
4 = Red 2
5 = Red 3

6 = Green 1
7 = Green 2
8 = Green 3

9 = Blue 1
10= Blue 2
11= Blue 3

12= Yellow 1
13= Yellow 2
14= Yellow 3
*/
                    case InteractionType.banzaifloor:
                        {
                            if (value == 3)
                            {
                                if (interactionCountHelper == 1)
                                {
                                    interactionCountHelper = 0;

                                    switch (team)
                                    {
                                        case Team.blue:
                                            {
                                                ((StringData)data).Data = "11";
                                                break;
                                            }

                                        case Team.green:
                                            {
                                                ((StringData)data).Data = "8";
                                                break;
                                            }

                                        case Team.red:
                                            {
                                                ((StringData)data).Data = "5";
                                                break;
                                            }

                                        case Team.yellow:
                                            {
                                                ((StringData)data).Data = "14";
                                                break;
                                            }
                                    }
                                }
                                else
                                {
                                    ((StringData)data).Data = "";
                                    interactionCountHelper++;
                                }

                                UpdateState();

                                interactionCount++;

                                if (interactionCount < 16)
                                {
                                    UpdateCounter = 1;
                                }
                                else
                                    UpdateCounter = 0;
                            }
                            break;
                        }

                    case InteractionType.banzaipuck:
                        {

                            if (interactionCount > 4)
                            {
                                interactionCount++;
                                UpdateCounter = 1;
                            }
                            else
                            {
                                interactionCount = 0;
                                UpdateCounter = 0;
                            }

                            break;
                        }

                    case InteractionType.freezetile:
                        {
                            if (InteractingUser > 0)
                            {
                                ((StringData)data).Data = "11000";
                                UpdateState(false, true);

                                GetRoom().GetFreeze().onFreezeTiles(this, freezePowerUp, InteractingUser);
                                InteractingUser = 0;
                                interactionCountHelper = 0;
                            }
                            break;
                        }

                    case InteractionType.freezetimer:
                        {
                            if (string.IsNullOrEmpty((string)data.GetData()))
                                break;

                            int seconds = 0;

                            try
                            {
                                seconds = int.Parse(data.GetData().ToString());
                            }
                            catch { }

                            if (seconds > 0)
                            {
                                if (interactionCountHelper == 1)
                                {
                                    seconds--;
                                    interactionCountHelper = 0;
                                    if (GetRoom().GetFreeze().GameIsStarted)
                                    {
                                        ((StringData)data).Data = seconds.ToString();
                                        UpdateState();
                                    }
                                    else
                                        break;
                                }
                                else
                                    interactionCountHelper++;

                                UpdateCounter = 1;
                            }
                            else
                            {
                                UpdateNeeded = false;
                                GetRoom().GetFreeze().StopGame();
                            }

                            break;
                        }
                }
            }
        }

        internal void ReqUpdate(int Cycles, bool setUpdate)
        {
            this.UpdateCounter = Cycles;
            if (setUpdate)
                this.UpdateNeeded = true;
        }

        internal void UpdateState()
        {
            UpdateState(true, true);
        }

        internal void UpdateState(bool inDb, bool inRoom)
        {
            if (GetRoom() == null)
                return;

            if (inDb)
            {
                GetRoom().GetRoomItemHandler().UpdateItem(this);
            }

            if (inRoom)
            {
                ServerMessage Message = new ServerMessage(0);

                if (IsFloorItem)
                {
                    Message.Init(Outgoing.ObjectDataUpdate);
                    Message.AppendString(Id.ToString());
                    Message.AppendInt32(0);
                    Message.AppendString(data is StringArrayStuffData ? data.ToString() : data.GetData().ToString());
                }
                else
                {
                    Message.Init(Outgoing.UpdateWallItemOnRoom);
                    Serialize(Message, GetRoom().OwnerId);
                }

                GetRoom().SendMessage(Message);
            }
        }

        internal void Serialize(ServerMessage Message, int UserId)
        {
            // int
            // int
            // int
            // int
            // int
            // string
            // int (extra)
            // int data type (0,1,2,3,4) (
            // 0 = (StringData?)          - string
            // 1 = (MapStuffData)         - int i, foreach i { string, string }
            // 2 = (StringArrayStuffData) - int i, foreach i { string }
            // 3 = (?)                    - string, int
            // 4 = not implemented?

            // ---data
            //    int


            // int
            // int
            // int
            // if type < 0, string

            if (IsFloorItem)
            {
                Message.AppendUInt(Id);
                Message.AppendInt32(GetBaseItem().SpriteId); // type
                Message.AppendInt32(mX); // x
                Message.AppendInt32(mY); // y
                Message.AppendInt32(Rot); // dir
                Message.AppendString(String.Format("{0:0.00}", TextHandling.GetString(mZ))); // z
                Message.AppendInt32(Extra); // extra
                Message.AppendInt32(data.GetTypeID()); // data type

                data.AppendToMessage(Message);

                //if (this.GetBaseItem().InteractionType == InteractionType.gift)
                //{
                //    int result = 0;
                //    if (ExtraData.Contains(Convert.ToChar(5).ToString()))
                //    {
                //        int color = int.Parse(ExtraData.Split((char)5)[1]);
                //        int lazo = int.Parse(ExtraData.Split((char)5)[2]);
                //        result = color * 1000 + lazo;
                //    }
                //    Message.AppendInt32(result);
                //    if (this.ExtraData.Contains(Convert.ToChar(5).ToString()))
                //    {
                //        uint PurchaserId = (uint)int.Parse(ExtraData.Split(';')[0]);
                //        Habbo Purchaser = FirewindEnvironment.getHabboForId(PurchaserId);
                //        if (Purchaser != null)
                //        {
                //            // "MESSAGE", "PRODUCT_CODE", "EXTRA_PARAM", "PURCHASER_NAME", "PURCHASER_FIGURE";

                //            Message.AppendInt32(1);
                //            Message.AppendInt32(6);
                //            Message.AppendString("EXTRA_PARAM");
                //            Message.AppendString("");
                //            Message.AppendString("MESSAGE");
                //            Message.AppendString(ExtraData.Split(';')[1].Split((char)5)[0]);
                //            Message.AppendString("PURCHASER_NAME");
                //            Message.AppendString(Purchaser.Username);
                //            Message.AppendString("PURCHASER_FIGURE");
                //            Message.AppendString(Purchaser.Look);
                //            Message.AppendString("PRODUCT_CODE");
                //            Message.AppendString("");
                //            Message.AppendString("state");
                //            Message.AppendString(MagicRemove ? "1" : "0");
                //        }
                //        else
                //        {
                //            Message.AppendInt32(0);
                //        }
                //    }
                //    // this.ExtraData.Contains(Convert.ToChar(5).ToString()) ? ExtraData.Split((char)5)[1] : "0"
                //    else
                //        Message.AppendInt32(0);
                //}
                //else
                //{
                //    Message.AppendInt32(0);
                //    Message.AppendInt32(0);
                //    if (GetBaseItem().InteractionType != InteractionType.fbgate)
                //        Message.AppendString(ExtraData);
                //    else
                //        Message.AppendString(string.Empty);
                //}
                Message.AppendInt32(-1);
                Message.AppendInt32(1); // Type New R63 ('use bottom')
                Message.AppendInt32(UserId);
            }
            else if (IsWallItem)
            {
                Message.AppendString(Id + String.Empty);
                Message.AppendInt32(GetBaseItem().SpriteId);
                Message.AppendString(wallCoord.ToString());
                switch (GetBaseItem().InteractionType)
                {
                    case InteractionType.postit:
                        Message.AppendString(data.GetData().ToString().Split(' ')[0]);
                        break;

                    default:
                        Message.AppendString(data.ToString());
                        break;
                }
                Message.AppendInt32(1); // Type New R63 ('use bottom')
                Message.AppendInt32(UserId);

                if (data.GetType() != typeof(StringData))
                {
                    Logging.LogException(string.Format("Strange wallitem, {2}, {3}, {0}, \"{1}\"", Id, data.ToString(), GetBaseItem().Name, GetBaseItem().Type));

                    using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                        dbClient.runFastQuery(string.Format("DELETE FROM items_extradata WHERE item_id = {0}", Id));
                }
            }
        }

        internal void refreshItem()
        {
            mBaseItem = null;
        }

        internal Item GetBaseItem()
        {
            if (mBaseItem == null)
                mBaseItem = FirewindEnvironment.GetGame().GetItemManager().GetItem(BaseItem);

            return mBaseItem;
        }

        internal Room GetRoom()
        {
            if (mRoom == null)
                mRoom = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(RoomId); //Todo: rub my penis

            return mRoom;
        }

        internal void UserWalksOnFurni(RoomUser user)
        {
            if (OnUserWalksOnFurni != null)
                OnUserWalksOnFurni(this, new UserWalksOnArgs(user));
        }

        internal void UserWalksOffFurni(RoomUser user)
        {
            if (OnUserWalksOffFurni != null)
                OnUserWalksOffFurni(this, new UserWalksOnArgs(user));
        }
    }
}
