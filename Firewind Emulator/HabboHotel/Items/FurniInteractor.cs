using System;
using System.Collections.Generic;
using System.Data;
using Database_Manager.Database.Session_Details.Interfaces;
using Firewind.HabboHotel.GameClients;
using Firewind.HabboHotel.Pathfinding;
using Firewind.HabboHotel.Rooms;
using Firewind.HabboHotel.Rooms.Games;
using System.Drawing;
using Firewind.Messages;
using HabboEvents;


namespace Firewind.HabboHotel.Items.Interactors
{
    abstract class FurniInteractor
    {
        internal abstract void OnPlace(GameClient Session, RoomItem Item);
        internal abstract void OnRemove(GameClient Session, RoomItem Item);
        internal abstract bool OnTrigger(GameClient Session, RoomItem Item, int Request, Boolean UserHasRights);
    }

    //class InteractorStatic : FurniInteractor
    //{
    //    internal override void OnPlace(GameClient Session, RoomItem Item) { }
    //    internal override void OnRemove(GameClient Session, RoomItem Item) { }
    //    internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights) { }
    //}

    class InteractorTeleport : FurniInteractor
    {
        internal override void OnPlace(GameClient Session, RoomItem Item)
        {
            Item.data = new StringData("0");

            if (Item.InteractingUser != 0)
            {
                RoomUser User = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Item.InteractingUser);

                if (User != null)
                {
                    User.ClearMovement(true);
                    User.AllowOverride = false;
                    User.CanWalk = true;
                }

                Item.InteractingUser = 0;
            }

            if (Item.InteractingUser2 != 0)
            {
                RoomUser User = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Item.InteractingUser2);

                if (User != null)
                {
                    User.ClearMovement(true);
                    User.AllowOverride = false;
                    User.CanWalk = true;
                }

                Item.InteractingUser2 = 0;
            }

            //Item.GetRoom().RegenerateUserMatrix();
        }

        internal override void OnRemove(GameClient Session, RoomItem Item)
        {
            Item.data = new StringData("0");

            if (Item.InteractingUser != 0)
            {
                RoomUser User = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Item.InteractingUser);

                if (User != null)
                {
                    User.UnlockWalking();
                }

                Item.InteractingUser = 0;
            }

            if (Item.InteractingUser2 != 0)
            {
                RoomUser User = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Item.InteractingUser2);

                if (User != null)
                {
                    User.UnlockWalking();
                }

                Item.InteractingUser2 = 0;
            }

            //Item.GetRoom().RegenerateUserMatrix();
        }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            // Is this user valid?
            if (Item == null || Item.GetRoom() == null || Session == null || Session.GetHabbo() == null)
                return false;
            RoomUser User = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (User == null)
            {
                return false;
            }

            // Alright. But is this user in the right position?
            if (User.Coordinate == Item.Coordinate || User.Coordinate == Item.SquareInFront)
            {
                // Fine. But is this tele even free?
                if (Item.InteractingUser != 0)
                {
                    return false;
                }

                //User.TeleDelay = -1;
                Item.InteractingUser = User.GetClient().GetHabbo().Id;
            }
            else if (User.CanWalk)
            {
                User.MoveTo(Item.SquareInFront);
            }
            return true;
        }
    }

    class InteractorSpinningBottle : FurniInteractor
    {
        internal override void OnPlace(GameClient Session, RoomItem Item)
        {
            Item.data = new StringData("0");
            Item.UpdateState(true, false);
        }

        internal override void OnRemove(GameClient Session, RoomItem Item)
        {
            Item.data = new StringData("0");
        }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            if (Item.data.GetData() != "-1")
            {
                Item.data = new StringData("-1");
                Item.UpdateState(false, true);
                Item.ReqUpdate(3, true);
            }
            return true;
        }
    }

    class InteractorDice : FurniInteractor
    {
        internal override void OnPlace(GameClient Session, RoomItem Item) { }
        internal override void OnRemove(GameClient Session, RoomItem Item) { }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            RoomUser User = null;
            if (Session != null)
                User = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (User == null)
                return false;

            if (Gamemap.TilesTouching(Item.GetX, Item.GetY, User.X, User.Y))
            {
                if (Item.data.GetData() != "-1")
                {
                    if (Request == -1)
                    {
                        Item.data = new StringData("0");
                        Item.UpdateState();
                    }
                    else
                    {
                        Item.data = new StringData("-1");
                        Item.UpdateState(false, true);
                        Item.ReqUpdate(4, true);
                    }
                }
            }
            else
            {
                User.MoveTo(Item.SquareInFront);
            }
            return true;
        }
    }

    class InteractorHabboWheel : FurniInteractor
    {
        internal override void OnPlace(GameClient Session, RoomItem Item)
        {
            ((StringData)Item.data).Data = "-1";
            Item.ReqUpdate(10, true);
        }

        internal override void OnRemove(GameClient Session, RoomItem Item)
        {
            ((StringData)Item.data).Data = "-1";
        }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            if (!UserHasRights)
            {
                return false;
            }

            if (Item.data.GetData() != "-1")
            {
                ((StringData)Item.data).Data = "-1";
                Item.UpdateState();
                Item.ReqUpdate(10, true);
            }
            return true;
        }
    }

    class InteractorLoveShuffler : FurniInteractor
    {
        internal override void OnPlace(GameClient Session, RoomItem Item)
        {
            ((StringData)Item.data).Data = "-1";
        }

        internal override void OnRemove(GameClient Session, RoomItem Item)
        {
            ((StringData)Item.data).Data = "-1";
        }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            if (!UserHasRights)
            {
                return false;
            }

            if (Item.data.GetData() != "0")
            {
                Item.data = new StringData("0");
                Item.UpdateState(false, true);
                Item.ReqUpdate(10, true);
            }
            return true;
        }
    }

    class InteractorOneWayGate : FurniInteractor
    {
        internal override void OnPlace(GameClient Session, RoomItem Item)
        {
            Item.data = new StringData("0");

            if (Item.InteractingUser != 0)
            {
                RoomUser User = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Item.InteractingUser);

                if (User != null)
                {
                    User.ClearMovement(true);
                    User.UnlockWalking();
                }

                Item.InteractingUser = 0;
            }
        }

        internal override void OnRemove(GameClient Session, RoomItem Item)
        {
            Item.data = new StringData("0");

            if (Item.InteractingUser != 0)
            {
                RoomUser User = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Item.InteractingUser);

                if (User != null)
                {
                    User.ClearMovement(true);
                    User.UnlockWalking();
                }

                Item.InteractingUser = 0;
            }
        }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            if (Session == null)
                return false;
            RoomUser User = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            if (User == null)
            {
                return false;
            }

            if (User.Coordinate != Item.SquareInFront && User.CanWalk)
            {
                User.MoveTo(Item.SquareInFront);
                return false;
            }

            // This check works for some reason
            if (!Item.GetRoom().GetGameMap().itemCanBePlacedHere(Item.SquareBehind.X, Item.SquareBehind.Y))
            {
                return false;
            }

            if (Item.InteractingUser == 0)
            {
                Item.InteractingUser = User.HabboId;

                User.CanWalk = false;

                if (User.IsWalking && (User.GoalX != Item.SquareInFront.X || User.GoalY != Item.SquareInFront.Y))
                {
                    User.ClearMovement(true);
                }

                ServerMessage update = new ServerMessage(Outgoing.OneWayDoorStatus);
                update.AppendUInt(Item.Id); // id
                update.AppendInt32(1); // status
                Item.GetRoom().SendMessage(update);

                User.AllowOverride = true;
                User.MoveTo(Item.SquareBehind);

                Item.ReqUpdate(4, true);
                return true;
            }
            return false;
        }
    }

    class InteractorAlert : FurniInteractor
    {
        internal override void OnPlace(GameClient Session, RoomItem Item)
        {
            Item.data = new StringData("0");
        }

        internal override void OnRemove(GameClient Session, RoomItem Item)
        {
            Item.data = new StringData("0");
        }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            if (!UserHasRights)
            {
                return false;
            }

            if (Item.data.GetData() == "0")
            {
                ((StringData)Item.data).Data = "1";
                Item.UpdateState(false, true);
                Item.ReqUpdate(4, true);
                return true;
            }
            return false;
        }
    }

    class InteractorVendor : FurniInteractor
    {
        internal override void OnPlace(GameClient Session, RoomItem Item)
        {
            Item.data = new StringData("0");

            if (Item.InteractingUser > 0)
            {
                RoomUser User = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Item.InteractingUser);

                if (User != null)
                {
                    User.CanWalk = true;
                }
            }
        }

        internal override void OnRemove(GameClient Session, RoomItem Item)
        {
            Item.data = new StringData("0");

            if (Item.InteractingUser > 0)
            {
                RoomUser User = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Item.InteractingUser);

                if (User != null)
                {
                    User.CanWalk = true;
                }
            }
        }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            if (((StringData)Item.data).Data != "1" && Item.GetBaseItem().VendingIds.Count >= 1 && Item.InteractingUser == 0 && Session != null)
            {
                RoomUser User = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

                if (User == null)
                {
                    return false;
                }

                if (!Gamemap.TilesTouching(User.X, User.Y, Item.GetX, Item.GetY))
                {
                    User.MoveTo(Item.SquareInFront);
                    return false;
                }

                Item.InteractingUser = Session.GetHabbo().Id;

                User.CanWalk = false;
                User.ClearMovement(true);
                User.SetRot(Rotation.Calculate(User.X, User.Y, Item.GetX, Item.GetY), false);

                Item.ReqUpdate(2, true);

                ((StringData)Item.data).Data = "1";
                Item.UpdateState(false, true);
                return true;
            }
            return false;
        }
    }

    class InteractorGenericSwitch : FurniInteractor
    {
        int Modes;

        internal InteractorGenericSwitch(int Modes)
        {
            this.Modes = (Modes - 1);

            if (this.Modes < 0)
            {
                this.Modes = 0;
            }
        }

        internal override void OnPlace(GameClient Session, RoomItem Item) { }
        internal override void OnRemove(GameClient Session, RoomItem Item) { }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            if (Session != null)
                FirewindEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Session, HabboHotel.Quests.QuestType.FURNI_SWITCH);
            if (!UserHasRights)
            {
                return false;
            }

            if (this.Modes == 0)
            {
                return false;
            }

            int currentMode = 0;
            int newMode = 0;

            try
            {
                currentMode = int.Parse(Item.data.GetData().ToString());
            }
            catch // (Exception e)
            {
                //Logging.HandleException(e, "InteractorGenericSwitch.OnTrigger");
            }

            if (currentMode <= 0)
            {
                newMode = 1;
            }
            else if (currentMode >= Modes)
            {
                newMode = 0;
            }
            else
            {
                newMode = currentMode + 1;
            }

            Item.data = new StringData(newMode.ToString());
            Item.UpdateState();
            return true;
        }
    }

    class InteractorNone : FurniInteractor
    {
        internal override void OnPlace(GameClient Session, RoomItem Item) { }
        internal override void OnRemove(GameClient Session, RoomItem Item) { }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            return true;
        }
    }


    class InteractorGate : FurniInteractor
    {
        int Modes;

        internal InteractorGate(int Modes)
        {
            this.Modes = (Modes - 1);

            if (this.Modes < 0)
            {
                this.Modes = 0;
            }
        }

        internal override void OnPlace(GameClient Session, RoomItem Item) { }
        internal override void OnRemove(GameClient Session, RoomItem Item) { }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            if (!UserHasRights)
            {
                return false;
            }

            if (this.Modes == 0)
            {
                Item.UpdateState(false, true);
            }

            int currentMode = 0;
            int newMode = 0;

            try
            {
                currentMode = int.Parse(Item.data.GetData().ToString());
            }
            catch //(Exception e)
            {
                //Logging.HandleException(e, "InteractorGate.OnTrigger");
            }

            if (currentMode <= 0)
            {
                newMode = 1;
            }
            else if (currentMode >= Modes)
            {
                newMode = 0;
            }
            else
            {
                newMode = currentMode + 1;
            }

            if (newMode == 0)
            {
                if (!Item.GetRoom().GetGameMap().itemCanBePlacedHere(Item.GetX, Item.GetY))
                {
                    return false;
                }

                //Dictionary<int, ThreeDCoord> Points = Gamemap.GetAffectedTiles(Item.GetBaseItem().Length, Item.GetBaseItem().Width,
                //    Item.GetX, Item.GetY, Item.Rot);

                //if (Points == null)
                //{
                //    Points = new Dictionary<int, ThreeDCoord>();
                //}

                //foreach (Rooms.AffectedTile Tile in Points.Values)
                //{
                //    if (!Item.GetRoom().SquareIsOpen(Tile.X, Tile.Y, false))
                //        return;
                //}
            }

            Item.data = new StringData(newMode.ToString());
            Item.UpdateState();
            Item.GetRoom().GetGameMap().updateMapForItem(Item);
            //Item.GetRoom().GenerateMaps();
            return true;
        }
    }

    class InteractorScoreboard : FurniInteractor
    {
        internal override void OnPlace(GameClient Session, RoomItem Item) { }
        internal override void OnRemove(GameClient Session, RoomItem Item) { }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            if (!UserHasRights)
            {
                return false;
            }

            int oldValue = 0;

            if (!string.IsNullOrEmpty((string)Item.data.GetData()))
            {
                try
                {
                    oldValue = int.Parse((string)Item.data.GetData());
                }
                catch { }
            }


            if (Request == 1)
            {
                if (Item.pendingReset && oldValue > 0)
                {
                    oldValue = 0;
                    Item.pendingReset = false;
                }
                else
                {
                    oldValue = oldValue + 60;
                    Item.UpdateNeeded = false;
                }
            }
            else if (Request == 2)
            {
                Item.UpdateNeeded = !Item.UpdateNeeded;
                Item.pendingReset = true;
            }


            Item.data = new StringData(oldValue.ToString());
            Item.UpdateState();
            return true;
        }
    }

    class InteractorFootball : FurniInteractor
    {
        internal override void OnPlace(GameClient Session, RoomItem Item) { }
        internal override void OnRemove(GameClient Session, RoomItem Item) { }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            if (Session == null)
                return false;
            RoomUser interactingUser = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            Point userCoord = interactingUser.Coordinate;
            Point ballCoord = Item.Coordinate;

            int differenceX = userCoord.X - ballCoord.X;
            int differenceY = userCoord.Y - ballCoord.Y;

            if (differenceX <= 1 && differenceX >= -1 && differenceY <= 1 && differenceY >= -1)
            {
                differenceX = differenceX * 2;
                differenceY = differenceY * 2;

                int newX = Item.GetX + differenceX;
                int newY = Item.GetY + differenceY;

                Item.GetRoom().GetSoccer().MoveBall(Item, Session, newX, newY);
                interactingUser.MoveTo(ballCoord);
            }
            else //if (differenceX == 2 || differenceY == 2 || differenceY == - 2 || differenceX == -2)
            {
                Item.interactingBallUser = Session.GetHabbo().Id;

                differenceX = differenceX * (-1);
                differenceY = differenceY * (-1);

                if (differenceX > 1)
                    differenceX = 1;
                else if (differenceX < -1)
                    differenceX = -1;


                if (differenceY > 1)
                    differenceY = 1;
                else if (differenceY < -1)
                    differenceY = -1;


                int newX = Item.GetX + differenceX;
                int newY = Item.GetY + differenceY;

                interactingUser.MoveTo(new Point(newX, newY));
            }
            return true;
        }
    }

    class InteractorScoreCounter : FurniInteractor
    {
        internal override void OnPlace(GameClient Session, RoomItem Item)
        {
            if (Item.team == Team.none)
                return;

            ((StringData)Item.data).Data = Item.GetRoom().GetGameManager().Points[(int)Item.team].ToString();
            Item.UpdateState(false, true);
        }
        internal override void OnRemove(GameClient Session, RoomItem Item) { }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            if (!UserHasRights)
            {
                return false;
            }

            int oldValue = 0;

            if (!string.IsNullOrEmpty((string)Item.data.GetData()))
            {
                try
                {
                    oldValue = int.Parse((string)Item.data.GetData());
                }
                catch { }
            }


            if (Request == 1)
            {
                oldValue++;
            }
            else if (Request == 2)
            {
                oldValue--;
            }
            else if (Request == 3)
            {
                oldValue = 0;
            }

            Item.data = new StringData(oldValue.ToString());
            Item.UpdateState(false, true);
            return true;
        }

        private static void UpdateTeamPoints(int points, Team team, RoomItem Item)
        {
            if (team == Team.none)
                return;

            Item.GetRoom().GetGameManager().Points[(int)Item.team] = points;
            Item.UpdateState(false, true);
        }
    }
    class InteractorBanzaiScoreCounter : FurniInteractor
    {
        internal override void OnPlace(GameClient Session, RoomItem Item)
        {
            if (Item.team == Team.none)
                return;

            ((StringData)Item.data).Data = Item.GetRoom().GetGameManager().Points[(int)Item.team].ToString();
            Item.UpdateState(false, true);
        }
        internal override void OnRemove(GameClient Session, RoomItem Item) { }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            if (!UserHasRights)
            {
                return false;
            }


            Item.GetRoom().GetGameManager().Points[(int)Item.team] = 0;

            Item.data = new StringData("0");
            Item.UpdateState();
            return true;
        }

        private static void UpdateTeamPoints(int points, Team team, RoomItem Item)
        {
            if (team == Team.none)
                return;

            Item.GetRoom().GetGameManager().Points[(int)Item.team] = points;
            Item.UpdateState();
        }
    }



    class InteractorBanzaiTimer : FurniInteractor
    {
        internal override void OnPlace(GameClient Session, RoomItem Item) { }
        internal override void OnRemove(GameClient Session, RoomItem Item) { }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            if (!UserHasRights)
            {
                return false;
            }

            int oldValue = 0;

            if (!string.IsNullOrEmpty((string)Item.data.GetData()))
            {
                try
                {
                    oldValue = int.Parse((string)Item.data.GetData());
                }
                catch { }
            }

            if (Request == 2)
            {
                if (Item.GetRoom().GetBanzai().isBanzaiActive && Item.pendingReset && oldValue > 0)
                {
                    oldValue = 0;
                    Item.pendingReset = false;
                }
                else
                {
                    if (oldValue == 0 || oldValue == 30 || oldValue == 60 || oldValue == 120 || oldValue == 180 || oldValue == 300 || oldValue == 600)
                    {
                        if (oldValue == 0)
                            oldValue = 30;
                        else if (oldValue == 30)
                            oldValue = 60;
                        else if (oldValue == 60)
                            oldValue = 120;
                        else if (oldValue == 120)
                            oldValue = 180;
                        else if (oldValue == 180)
                            oldValue = 300;
                        else if (oldValue == 300)
                            oldValue = 600;
                        else if (oldValue == 600)
                            oldValue = 0;
                    }
                    else
                        oldValue = 0;
                    Item.UpdateNeeded = false;
                }
            }
            else if (Request == 1)
            {
                if (!Item.GetRoom().GetBanzai().isBanzaiActive)
                {
                    Item.UpdateNeeded = !Item.UpdateNeeded;

                    if (Item.UpdateNeeded)
                    {
                        //Logging.WriteLine("Game started");
                        Item.GetRoom().GetBanzai().BanzaiStart();
                    }

                    Item.pendingReset = true;
                }
            }


            Item.data = new StringData(oldValue.ToString());
            Item.UpdateState();
            return true;
        }
    }

    class InteractorBanzaiPuck : FurniInteractor
    {
        internal override void OnPlace(GameClient Session, RoomItem Item) { }
        internal override void OnRemove(GameClient Session, RoomItem Item) { }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            if (Session == null)
                return false;
            RoomUser interactingUser = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            Point userCoord = interactingUser.Coordinate;
            Point ballCoord = Item.Coordinate;

            int differenceX = userCoord.X - ballCoord.X;
            int differenceY = userCoord.Y - ballCoord.Y;

            if (differenceX <= 1 && differenceX >= -1 && differenceY <= 1 && differenceY >= -1)
            {
                differenceX = differenceX * 2;
                differenceY = differenceY * 2;

                int newX = Item.GetX + differenceX;
                int newY = Item.GetY + differenceY;

                Item.GetRoom().GetSoccer().MoveBall(Item, Session, newX, newY);
                interactingUser.MoveTo(ballCoord);
            }
            else //if (differenceX == 2 || differenceY == 2 || differenceY == - 2 || differenceX == -2)
            {
                Item.interactingBallUser = Session.GetHabbo().Id;

                differenceX = differenceX * (-1);
                differenceY = differenceY * (-1);

                if (differenceX > 1)
                    differenceX = 1;
                else if (differenceX < -1)
                    differenceX = -1;


                if (differenceY > 1)
                    differenceY = 1;
                else if (differenceY < -1)
                    differenceY = -1;


                int newX = Item.GetX + differenceX;
                int newY = Item.GetY + differenceY;

                interactingUser.MoveTo(new Point(newX, newY));
            }
            return true;
        }
    }

    class InteractorFreezeTimer : FurniInteractor
    {
        internal override void OnPlace(GameClient Session, RoomItem Item) { }
        internal override void OnRemove(GameClient Session, RoomItem Item) { }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            if (!UserHasRights)
            {
                return false;
            }

            int oldValue = 0;

            if (!string.IsNullOrEmpty((string)Item.data.GetData()))
            {
                try
                {
                    oldValue = int.Parse((string)Item.data.GetData());
                }
                catch { }
            }

            if (Request == 2)
            {
                if (Item.pendingReset && oldValue > 0)
                {
                    oldValue = 0;
                    Item.pendingReset = false;
                }
                else
                {
                    if (oldValue == 0 || oldValue == 30 || oldValue == 60 || oldValue == 120 || oldValue == 180 || oldValue == 300 || oldValue == 600)
                    {
                        if (oldValue == 0)
                            oldValue = 30;
                        else if (oldValue == 30)
                            oldValue = 60;
                        else if (oldValue == 60)
                            oldValue = 120;
                        else if (oldValue == 120)
                            oldValue = 180;
                        else if (oldValue == 180)
                            oldValue = 300;
                        else if (oldValue == 300)
                            oldValue = 600;
                        else if (oldValue == 600)
                            oldValue = 0;
                    }
                    else
                        oldValue = 0;
                    Item.UpdateNeeded = false;
                }
            }
            else if (Request == 1)
            {
                if (!Item.GetRoom().GetFreeze().GameIsStarted)
                {
                    Item.UpdateNeeded = !Item.UpdateNeeded;

                    if (Item.UpdateNeeded)
                    {
                        //Logging.WriteLine("Game started");
                        Item.GetRoom().GetFreeze().StartGame();
                    }

                    Item.pendingReset = true;
                }
            }


            Item.data = new StringData(oldValue.ToString());
            Item.UpdateState();
            return true;
        }
    }

    class InteractorFreezeTile : FurniInteractor
    {
        internal override void OnPlace(GameClient Session, RoomItem Item) { }
        internal override void OnRemove(GameClient Session, RoomItem Item) { }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            if (Session == null || Session.GetHabbo() == null || Item.InteractingUser > 0)
                return false;

            string username = Session.GetHabbo().Username;
            RoomUser user = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(username);

            user.GoalX = Item.GetX;
            user.GoalY = Item.GetY;

            if (user.team != Team.none)
                user.throwBallAtGoal = true;

            //Logging.WriteLine(Request.ToString());

            //int oldValue = 0;

            //if (!string.IsNullOrEmpty((string)Item.data.GetData()))
            //{
            //    try
            //    {
            //        oldValue = int.Parse((string)Item.data.GetData());
            //    }
            //    catch { }
            //}
            //if (oldValue == 0)
            //    oldValue = 1000;
            //    //oldValue = 11000;
            //else
            //    oldValue = 0;

            //Item.data = new StringData(oldValue.ToString());
            //Item.UpdateState();
            return true;
        }
    }

    class InteractorIncrementer : FurniInteractor
    {
        internal override void OnPlace(GameClient Session, RoomItem Item) { }
        internal override void OnRemove(GameClient Session, RoomItem Item) { }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {

            int oldValue = 0;

            if (!string.IsNullOrEmpty((string)Item.data.GetData()))
            {
                try
                {
                    oldValue = int.Parse((string)Item.data.GetData());
                }
                catch { }
            }
            oldValue += 1;
            //Logging.WriteLine(oldValue.ToString());

            Item.data = new StringData(oldValue.ToString());
            Item.UpdateState();
            return true;
        }
    }

    class InteractorIgnore : FurniInteractor
    {
        internal override void OnPlace(GameClient Session, RoomItem Item) { }
        internal override void OnRemove(GameClient Session, RoomItem Item) { }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            return true;
        }
    }

    class WiredInteractor : FurniInteractor
    {
        internal override void OnPlace(GameClient Session, RoomItem Item) { }
        internal override void OnRemove(GameClient Session, RoomItem Item) { }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            if (Session == null || Item == null)
                return false;

            if (!UserHasRights)
                return false;

            String ExtraInfo = "";
            List<RoomItem> items = new List<RoomItem>();
            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                try
                {
                    dbClient.setQuery("SELECT trigger_data FROM trigger_item WHERE trigger_id = @id ");
                    dbClient.addParameter("id", (int)Item.Id);
                    ExtraInfo = dbClient.getString();
                }
                catch { }
                try
                {
                    dbClient.setQuery("SELECT triggers_item FROM trigger_in_place WHERE original_trigger = @id");
                    dbClient.addParameter("id", (int)Item.Id);
                    DataTable dTable = dbClient.getTable();
                    RoomItem targetItem;
                    foreach (DataRow dRows in dTable.Rows)
                    {
                        targetItem = Item.GetRoom().GetRoomItemHandler().GetItem(Convert.ToUInt32(dRows[0]));
                        if (targetItem == null || items.Contains(targetItem))
                            continue;
                        items.Add(targetItem);
                    }
                }
                catch { }
            }
            switch (Item.GetBaseItem().InteractionType)
            {
                #region Triggers

                case InteractionType.triggerwalkonfurni:
                    {
                        ServerMessage message = new ServerMessage(Outgoing.WiredEffect);
                        message.AppendBoolean(false); // stuffTypeSelectionEnabled
                        message.AppendInt32(5); // furniLimit

                        message.AppendInt32(items.Count); //stuffIds
                        foreach (RoomItem item in items)
                            message.AppendUInt(item.Id);

                        message.AppendInt32(Item.GetBaseItem().SpriteId); // stuffTypeId
                        message.AppendUInt(Item.Id); // id
                        message.AppendString(ExtraInfo); // stringParam

                        message.AppendInt32(0); // intParams

                        // 1=Perform the Effect on one random Furni whose type matches one of the picked Furnis
                        // 2=Perform the Effect on a Furni defined by the Trigger or Condition
                        // 0=Perform the Effect on picked Furnis

                        message.AppendInt32(8); // type
                        message.AppendInt32(0); // delayInPulses
                        message.AppendInt32(0); // conflictingTriggers
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        Session.SendMessage(message);
                        break;
                    }

                case InteractionType.triggergamestart:
                    {
                        ServerMessage message = new ServerMessage(Outgoing.WiredFurniTrigger);
                        message.AppendBoolean(false);
                        message.AppendInt32(0);
                        message.AppendInt32(items.Count);
                        foreach (RoomItem item in items)
                            message.AppendUInt(item.Id);
                        message.AppendInt32(Item.GetBaseItem().SpriteId);
                        message.AppendUInt(Item.Id);
                        message.AppendString(ExtraInfo);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(8);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        Session.SendMessage(message);
                        break;
                    }

                case InteractionType.triggerroomenter:
                    {
                        ServerMessage message = new ServerMessage(Outgoing.WiredFurniTrigger);
                        message.AppendBoolean(false);
                        message.AppendInt32(0);
                        message.AppendInt32(items.Count);
                        foreach (RoomItem item in items)
                            message.AppendUInt(item.Id);
                        message.AppendInt32(Item.GetBaseItem().SpriteId);
                        message.AppendUInt(Item.Id);
                        message.AppendString(ExtraInfo);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(7);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);

                        Session.SendMessage(message);
                        break;
                    }

                case InteractionType.triggergameend:
                    {
                        ServerMessage message = new ServerMessage(Outgoing.WiredFurniTrigger);
                        message.AppendBoolean(false);
                        message.AppendInt32(0);
                        message.AppendInt32(items.Count);
                        foreach (RoomItem item in items)
                            message.AppendUInt(item.Id);
                        message.AppendInt32(Item.GetBaseItem().SpriteId);
                        message.AppendUInt(Item.Id);
                        message.AppendString(ExtraInfo);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(8);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);

                        Session.SendMessage(message);
                        break;
                    }

                case InteractionType.triggertimer:
                    {
                        ServerMessage message = new ServerMessage(Outgoing.WiredFurniTrigger);
                        message.AppendBoolean(false);
                        message.AppendInt32(5);
                        message.AppendInt32(items.Count);
                        foreach (RoomItem item in items)
                            message.AppendUInt(item.Id);
                        message.AppendInt32(Item.GetBaseItem().SpriteId);
                        message.AppendUInt(Item.Id);

                        message.AppendString(ExtraInfo);
                        message.AppendInt32(1);
                        message.AppendInt32(1);
                        message.AppendInt32(1);
                        message.AppendInt32(3);
                        message.AppendInt32(0);
                        message.AppendInt32(0);

                        Session.SendMessage(message);
                        break;
                    }

                case InteractionType.triggerwalkofffurni:
                    {
                        ServerMessage message = new ServerMessage(Outgoing.WiredEffect);
                        message.AppendBoolean(false);
                        message.AppendInt32(5);
                        message.AppendInt32(items.Count);
                        foreach (RoomItem item in items)
                            message.AppendUInt(item.Id);
                        message.AppendInt32(Item.GetBaseItem().SpriteId);
                        message.AppendUInt(Item.Id);

                        message.AppendString(ExtraInfo);
                        message.AppendInt32(0);
                        message.AppendInt32(2);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);

                        Session.SendMessage(message);
                        break;
                    }

                case InteractionType.triggeronusersay:
                    {
                        ServerMessage message = new ServerMessage(Outgoing.WiredFurniTrigger);
                        message.AppendBoolean(false);
                        message.AppendInt32(0);
                        message.AppendInt32(items.Count);
                        foreach (RoomItem item in items)
                            message.AppendUInt(item.Id);
                        message.AppendInt32(Item.GetBaseItem().SpriteId);
                        message.AppendUInt(Item.Id);
                        message.AppendString(ExtraInfo);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);

                        Session.SendMessage(message);
                        break;
                    }

                case InteractionType.triggerscoreachieved:
                    {
                        ServerMessage message = new ServerMessage(Outgoing.WiredFurniTrigger);
                        message.AppendBoolean(false);
                        message.AppendInt32(5);
                        message.AppendInt32(items.Count);
                        foreach (RoomItem item in items)
                            message.AppendUInt(item.Id);
                        message.AppendInt32(Item.GetBaseItem().SpriteId);
                        message.AppendUInt(Item.Id);
                        message.AppendString(ExtraInfo);
                        message.AppendInt32(1);
                        message.AppendInt32(100);
                        message.AppendInt32(0);
                        message.AppendInt32(10);
                        message.AppendInt32(0);
                        message.AppendInt32(0);

                        Session.SendMessage(message);
                        break;
                    }

                case InteractionType.triggerrepeater:
                    {
                        ServerMessage message = new ServerMessage(Outgoing.WiredFurniTrigger);
                        message.AppendBoolean(false);
                        message.AppendInt32(5);
                        message.AppendInt32(items.Count);
                        foreach (RoomItem item in items)
                            message.AppendUInt(item.Id);
                        message.AppendInt32(Item.GetBaseItem().SpriteId);
                        message.AppendUInt(Item.Id);
                        message.AppendString(ExtraInfo);
                        message.AppendInt32(1);
                        message.AppendInt32(10);
                        message.AppendInt32(0);
                        message.AppendInt32(6);
                        message.AppendInt32(0);
                        message.AppendInt32(0);

                        Session.SendMessage(message);
                        break;
                    }

                case InteractionType.triggerstatechanged:
                    {
                        ServerMessage message = new ServerMessage(Outgoing.WiredEffect);
                        message.AppendBoolean(false);
                        message.AppendInt32(5);
                        message.AppendInt32(items.Count);
                        foreach (RoomItem item in items)
                            message.AppendUInt(item.Id);
                        message.AppendInt32(Item.GetBaseItem().SpriteId);
                        message.AppendUInt(Item.Id);
                        message.AppendString(ExtraInfo);
                        message.AppendInt32(0);
                        message.AppendInt32(8);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);

                        Session.SendMessage(message);
                        break;
                    }
                #endregion

                #region Effects
                case InteractionType.actionposreset:
                    {
                        ServerMessage message = new ServerMessage(Outgoing.WiredEffect);
                        message.AppendBoolean(false);
                        message.AppendInt32(5);
                        message.AppendInt32(items.Count);
                        foreach (RoomItem item in items)
                            message.AppendUInt(item.Id);
                        message.AppendInt32(Item.GetBaseItem().SpriteId);
                        message.AppendUInt(Item.Id);
                        message.AppendString(ExtraInfo);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        Session.SendMessage(message);
                        break;
                    }

                case InteractionType.actiongivescore:
                    {
                        ServerMessage message = new ServerMessage(Outgoing.WiredEffect);
                        message.AppendBoolean(false);
                        message.AppendInt32(5);
                        message.AppendInt32(items.Count);
                        foreach (RoomItem item in items)
                            message.AppendUInt(item.Id);
                        message.AppendInt32(Item.GetBaseItem().SpriteId);
                        message.AppendUInt(Item.Id);
                        message.AppendString(ExtraInfo);
                        message.AppendInt32(2);
                        message.AppendInt32(5);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(6);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        Session.SendMessage(message);
                        break;
                    }

                case InteractionType.actionresettimer:
                    {
                        ServerMessage message = new ServerMessage(Outgoing.WiredEffect);
                        message.AppendBoolean(false);
                        message.AppendInt32(5);
                        message.AppendInt32(items.Count);
                        foreach (RoomItem item in items)
                            message.AppendUInt(item.Id);
                        message.AppendInt32(Item.GetBaseItem().SpriteId);
                        message.AppendUInt(Item.Id);
                        message.AppendString(ExtraInfo);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        Session.SendMessage(message);
                        break;
                    }

                case InteractionType.actiontogglestate:
                    {
                        ServerMessage message = new ServerMessage(Outgoing.WiredEffect);
                        message.AppendBoolean(false);
                        message.AppendInt32(5);
                        message.AppendInt32(items.Count);
                        foreach (RoomItem item in items)
                            message.AppendUInt(item.Id);
                        message.AppendInt32(Item.GetBaseItem().SpriteId);
                        message.AppendUInt(Item.Id);
                        message.AppendString(ExtraInfo);
                        message.AppendInt32(0);
                        message.AppendInt32(8);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);

                        Session.SendMessage(message);
                        break;
                    }

                case InteractionType.actionshowmessage:
                    {
                        ServerMessage message = new ServerMessage(Outgoing.WiredEffect);
                        message.AppendBoolean(false);
                        message.AppendInt32(0);
                        message.AppendInt32(items.Count);
                        foreach (RoomItem item in items)
                            message.AppendUInt(item.Id);
                        message.AppendInt32(Item.GetBaseItem().SpriteId);
                        message.AppendUInt(Item.Id);
                        message.AppendString(ExtraInfo);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(7);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);

                        Session.SendMessage(message);
                        break;
                    }

                case InteractionType.actionteleportto:
                    {
                        ServerMessage message = new ServerMessage(Outgoing.WiredEffect);
                        message.AppendBoolean(false);
                        message.AppendInt32(5);
                        message.AppendInt32(items.Count);
                        foreach (RoomItem item in items)
                            message.AppendUInt(item.Id);
                        message.AppendInt32(Item.GetBaseItem().SpriteId);
                        message.AppendUInt(Item.Id);
                        message.AppendString(ExtraInfo);
                        message.AppendInt32(0);
                        message.AppendInt32(8);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendByte(2);

                        Session.SendMessage(message);
                        break;
                    }

                case InteractionType.actionmoverotate:
                    {
                        ServerMessage message = new ServerMessage(Outgoing.WiredEffect);
                        message.AppendBoolean(false);
                        message.AppendInt32(5);
                        message.AppendInt32(items.Count);
                        foreach (RoomItem item in items)
                            message.AppendUInt(item.Id);
                        message.AppendInt32(Item.GetBaseItem().SpriteId);
                        message.AppendUInt(Item.Id);
                        message.AppendString(ExtraInfo);
                        message.AppendInt32(2);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(4);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);

                        Session.SendMessage(message);
                        break;
                    }

                #endregion

                #region Add-ons
                case InteractionType.specialrandom:
                    {
                        ServerMessage message = new ServerMessage(Outgoing.WiredEffect);
                        message.AppendBoolean(false);
                        message.AppendInt32(5);
                        message.AppendInt32(items.Count);
                        foreach (RoomItem item in items)
                            message.AppendUInt(item.Id);
                        message.AppendInt32(Item.GetBaseItem().SpriteId);
                        message.AppendUInt(Item.Id);
                        message.AppendString(ExtraInfo);
                        message.AppendInt32(0);
                        message.AppendInt32(8);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);

                        Session.SendMessage(message);
                        break;
                    }

                case InteractionType.specialunseen:
                    {
                        ServerMessage message = new ServerMessage(Outgoing.WiredEffect);
                        message.AppendBoolean(false);
                        message.AppendInt32(5);
                        message.AppendInt32(items.Count);
                        foreach (RoomItem item in items)
                            message.AppendUInt(item.Id);
                        message.AppendInt32(Item.GetBaseItem().SpriteId);
                        message.AppendUInt(Item.Id);
                        message.AppendString(ExtraInfo);
                        message.AppendInt32(0);
                        message.AppendInt32(8);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);

                        Session.SendMessage(message);
                        break;
                    }
                #endregion

                #region Conditions
                case InteractionType.conditiontimelessthan:
                case InteractionType.conditiontimemorethan:
                    {
                        ServerMessage message = new ServerMessage(Outgoing.WiredCondition);
                        message.AppendBoolean(false);
                        message.AppendInt32(5);
                        message.AppendInt32(items.Count);
                        foreach (RoomItem item in items)
                            message.AppendUInt(item.Id);
                        message.AppendInt32(Item.GetBaseItem().SpriteId);
                        message.AppendUInt(Item.Id);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        //message.AppendBoolean(false);
                        //message.AppendBoolean(false);
                        //message.AppendInt32(7);
                        //message.AppendBoolean(false);
                        //message.AppendBoolean(false);

                        Session.SendMessage(message);
                        break;
                    }

                case InteractionType.conditionfurnishaveusers:
                    {
                        ServerMessage message = new ServerMessage(Outgoing.WiredCondition);
                        message.AppendBoolean(false);
                        message.AppendInt32(5);
                        message.AppendInt32(items.Count);
                        foreach (RoomItem item in items)
                            message.AppendUInt(item.Id);
                        message.AppendInt32(Item.GetBaseItem().SpriteId);
                        message.AppendUInt(Item.Id);

                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(1);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);

                        Session.SendMessage(message);
                        break;
                    }

                case InteractionType.conditionstatepos:
                    {
                        ServerMessage message = new ServerMessage(Outgoing.WiredCondition);
                        message.AppendBoolean(false);
                        message.AppendInt32(5);
                        message.AppendInt32(items.Count);
                        foreach (RoomItem item in items)
                            message.AppendUInt(item.Id);
                        message.AppendInt32(Item.GetBaseItem().SpriteId);
                        message.AppendUInt(Item.Id);

                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(1);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);

                        Session.SendMessage(message);
                        break;
                    }

                case InteractionType.conditiontriggeronfurni:
                    {
                        ServerMessage message = new ServerMessage(Outgoing.WiredCondition);
                        message.AppendBoolean(false);
                        message.AppendInt32(5);
                        message.AppendInt32(items.Count);
                        foreach (RoomItem item in items)
                            message.AppendUInt(item.Id);
                        message.AppendInt32(Item.GetBaseItem().SpriteId);
                        message.AppendUInt(Item.Id);

                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);
                        message.AppendInt32(0);

                        Session.SendMessage(message);
                        break;
                    }

                //Unknown:
                //2 radio + 5 selct
                #endregion

            }
            return true;
        }
    }

    class InteractorJukebox : FurniInteractor
    {
        internal override void OnPlace(GameClient Session, RoomItem Item) { }
        internal override void OnRemove(GameClient Session, RoomItem Item) { }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            /*if (((StringData)Item.data).Data == "1")
            {
                Item.GetRoom().GetRoomMusicController().Stop();
                Item.data = new StringData("0");
            }
            else
            {
                Item.GetRoom().GetRoomMusicController().Start();
                ((StringData)Item.data).Data = "1";
            }

            Item.UpdateState();*/
            return true;
        }
    }

    class InteractorPuzzleBox : FurniInteractor
    {
        internal override void OnPlace(GameClient Session, RoomItem Item) { }
        internal override void OnRemove(GameClient Session, RoomItem Item) { }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            RoomUser User = Item.GetRoom().GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);

            Point ItemCoordx1 = new Point(Item.Coordinate.X + 1, Item.Coordinate.Y);
            Point ItemCoordx2 = new Point(Item.Coordinate.X - 1, Item.Coordinate.Y);
            Point ItemCoordy1 = new Point(Item.Coordinate.X, Item.Coordinate.Y + 1);
            Point ItemCoordy2 = new Point(Item.Coordinate.X, Item.Coordinate.Y - 1);

            if (User == null)
            {
                return false;
            }

            if (User.Coordinate != ItemCoordx1 && User.Coordinate != ItemCoordx2 && User.Coordinate != ItemCoordy1 && User.Coordinate != ItemCoordy2)
            {
                if (User.CanWalk)
                {
                    User.MoveTo(Item.SquareInFront);
                    return false;
                }
            }
            else
            {
                int NewX = Item.Coordinate.X;
                int NewY = Item.Coordinate.Y;

                if (User.Coordinate == ItemCoordx1)
                {
                    NewX = Item.Coordinate.X - 1;
                    NewY = Item.Coordinate.Y;
                }
                else if (User.Coordinate == ItemCoordx2)
                {
                    NewX = Item.Coordinate.X + 1;
                    NewY = Item.Coordinate.Y;
                }
                else if (User.Coordinate == ItemCoordy1)
                {
                    NewX = Item.Coordinate.X;
                    NewY = Item.Coordinate.Y - 1;
                }
                else if (User.Coordinate == ItemCoordy2)
                {
                    NewX = Item.Coordinate.X;
                    NewY = Item.Coordinate.Y + 1;
                }

                if (Item.GetRoom().GetGameMap().itemCanBePlacedHere(NewX, NewY))
                {
                    Double NewZ = Item.GetRoom().GetGameMap().SqAbsoluteHeight(NewX, NewY);

                    ServerMessage Message = new ServerMessage();
                    Message.Init(Outgoing.ObjectOnRoller); // Cf
                    Message.AppendInt32(Item.Coordinate.X);
                    Message.AppendInt32(Item.Coordinate.Y);
                    Message.AppendInt32(NewX);
                    Message.AppendInt32(NewY);
                    Message.AppendInt32(1);
                    Message.AppendUInt(Item.Id);
                    Message.AppendString(Item.GetZ.ToString().Replace(',', '.'));
                    Message.AppendString(NewZ.ToString().Replace(',', '.'));
                    Message.AppendUInt(0);
                    Item.GetRoom().SendMessage(Message);

                    Item.GetRoom().GetRoomItemHandler().SetFloorItem(User.GetClient(), Item, NewX, NewY, Item.Rot, false, false, false);
                }
            }
            return true;
        }
    }

    class InteractorMannequin : FurniInteractor
    {
        internal override void OnPlace(GameClient Session, RoomItem Item)
        {

        }

        internal override void OnRemove(GameClient Session, RoomItem Item)
        {

        }

        internal override bool OnTrigger(GameClient Session, RoomItem Item, int Request, bool UserHasRights)
        {
            Room Room = Session.GetHabbo().CurrentRoom;
            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            MapStuffData data = (MapStuffData)Item.data;

            string figure = data.Data["FIGURE"];
            string gender = data.Data["GENDER"];

            // We gotta keep our skin and headgear!
            string filteredLook = "";
            string[] sp = Session.GetHabbo().Look.Split('.');
            foreach (string s in sp)
            {
                if ((s.StartsWith("hd") || s.StartsWith("ha") || s.StartsWith("he") || s.StartsWith("fa") || s.StartsWith("ea") || s.StartsWith("hr")))
                {
                    filteredLook += s + ".";
                }
            }
            filteredLook += figure;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {

                dbClient.setQuery("UPDATE users SET gender = @gender, look = @look WHERE id = @id");
                dbClient.addParameter("id", Session.GetHabbo().Id);
                dbClient.addParameter("gender", gender);
                dbClient.addParameter("look", filteredLook);
                dbClient.runQuery();
            }

            Session.GetHabbo().Look = filteredLook;
            Session.GetHabbo().Gender = gender;

            Session.GetMessageHandler().GetResponse().Init(Outgoing.UpdateUserInformation);
            Session.GetMessageHandler().GetResponse().AppendInt32(-1);
            Session.GetMessageHandler().GetResponse().AppendStringWithBreak(Session.GetHabbo().Look);
            Session.GetMessageHandler().GetResponse().AppendStringWithBreak(Session.GetHabbo().Gender.ToLower());
            Session.GetMessageHandler().GetResponse().AppendStringWithBreak(Session.GetHabbo().Motto);
            Session.GetMessageHandler().GetResponse().AppendInt32(Session.GetHabbo().AchievementPoints);
            Session.GetMessageHandler().SendResponse();

            ServerMessage RoomUpdate = new ServerMessage(Outgoing.UpdateUserInformation);
            RoomUpdate.AppendInt32(User.VirtualId);
            RoomUpdate.AppendStringWithBreak(Session.GetHabbo().Look);
            RoomUpdate.AppendStringWithBreak(Session.GetHabbo().Gender.ToLower());
            RoomUpdate.AppendStringWithBreak(Session.GetHabbo().Motto);
            RoomUpdate.AppendInt32(Session.GetHabbo().AchievementPoints);
            Room.SendMessage(RoomUpdate);

            return true;
        }

    }
}