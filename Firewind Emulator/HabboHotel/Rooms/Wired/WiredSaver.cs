using System.Collections.Generic;
using Firewind.HabboHotel.Items;
using Firewind.HabboHotel.Rooms.Wired.WiredHandlers;
using Firewind.HabboHotel.Rooms.Wired.WiredHandlers.Effects;
using Firewind.HabboHotel.Rooms.Wired.WiredHandlers.Interfaces;
using Firewind.HabboHotel.Rooms.Wired.WiredHandlers.Triggers;
using Firewind.Messages;
using Database_Manager.Database.Session_Details.Interfaces;
using Firewind.HabboHotel.Rooms.Wired.WiredHandlers.Conditions;
using Firewind.HabboHotel.GameClients;
using System;
using HabboEvents;

namespace Firewind.HabboHotel.Rooms.Wired
{
    class WiredSaver
    {
        internal static void HandleDefaultSave(uint itemID, Room room)
        {
            RoomItem item = room.GetRoomItemHandler().GetItem(itemID);
            if (item == null)
                return;

            InteractionType type = item.GetBaseItem().InteractionType;
            switch (type)
            {
                case InteractionType.actiongivescore:
                    {
                        int points = 0;
                        int games = 0;

                        IWiredTrigger action = new GiveScore(games, points, room.GetGameManager(), itemID);
                        HandleTriggerSave(action, room.GetWiredHandler(), room, itemID);
                        break;
                    }

                case InteractionType.actionmoverotate:
                    {
                        MovementState movement = MovementState.none;
                        RotationState rotation = RotationState.none;

                        List<RoomItem> items = new List<RoomItem>();
                        int delay = 0;

                        IWiredTrigger action = new MoveRotate(movement, rotation, items, delay, room, room.GetWiredHandler(), itemID);
                        HandleTriggerSave(action, room.GetWiredHandler(), room, itemID);
                        break;
                    }

                case InteractionType.actionposreset:
                    {
                        List<RoomItem> items = new List<RoomItem>();
                        int delay = 0;

                        IWiredTrigger action = new PositionReset(items, delay, room.GetRoomItemHandler(), room.GetWiredHandler(), itemID);
                        HandleTriggerSave(action, room.GetWiredHandler(), room, itemID);
                        break;
                    }

                case InteractionType.actionresettimer:
                    {
                        List<RoomItem> items = new List<RoomItem>();
                        int delay = 0;

                        IWiredTrigger action = new TimerReset(room, room.GetWiredHandler(), items, delay, itemID);
                        HandleTriggerSave(action, room.GetWiredHandler(), room, itemID);
                        break;
                    }

                case InteractionType.actionshowmessage:
                    {
                        string message = string.Empty;

                        IWiredTrigger action = new ShowMessage(message, room.GetWiredHandler(), itemID);
                        HandleTriggerSave(action, room.GetWiredHandler(), room, itemID);
                        break;
                    }

                case InteractionType.actionteleportto:
                    {
                        List<RoomItem> items = new List<RoomItem>();
                        int delay = 0;

                        IWiredTrigger action = new TeleportToItem(room.GetGameMap(), room.GetWiredHandler(), items, delay, itemID);
                        HandleTriggerSave(action, room.GetWiredHandler(), room, itemID);
                        break;
                    }

                case InteractionType.actiontogglestate:
                    {
                        List<RoomItem> items = new List<RoomItem>();
                        int delay = 0;

                        IWiredTrigger action = new ToggleItemState(room.GetGameMap(), room.GetWiredHandler(), items, delay, item);
                        HandleTriggerSave(action, room.GetWiredHandler(), room, itemID);
                        break;
                    }

                case InteractionType.conditionfurnishaveusers:
                    {

                        break;
                    }

                case InteractionType.conditionstatepos:
                    {

                        break;
                    }

                case InteractionType.conditiontimelessthan:
                    {

                        break;
                    }

                case InteractionType.conditiontimemorethan:
                    {

                        break;
                    }

                case InteractionType.conditiontriggeronfurni:
                    {

                        break;
                    }

                case InteractionType.specialrandom:
                    {

                        break;
                    }

                case InteractionType.specialunseen:
                    {

                        break;
                    }

                case InteractionType.triggergameend:
                    {
                        IWiredTrigger handler = new GameEnds(item, room.GetWiredHandler(), room.GetGameManager());
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);
                        break;
                    }

                case InteractionType.triggergamestart:
                    {
                        IWiredTrigger handler = new GameStarts(item, room.GetWiredHandler(), room.GetGameManager());
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);
                        break;
                    }

                case InteractionType.triggeronusersay:
                    {
                        bool isOnlyOwner = false;
                        string message = string.Empty;

                        IWiredTrigger handler = new UserSays(item, room.GetWiredHandler(), !isOnlyOwner, message, room);
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);
                        break;
                    }

                case InteractionType.triggerrepeater:
                    {
                        int cycleTimes = 0;

                        IWiredTrigger handler = new Repeater(room.GetWiredHandler(), item, cycleTimes);
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);
                        break;
                    }

                case InteractionType.triggerroomenter:
                    {
                        string users = string.Empty;

                        IWiredTrigger handler = new EntersRoom(item, room.GetWiredHandler(), room.GetRoomUserManager(), !string.IsNullOrEmpty(users), users);
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);
                        break;
                    }

                case InteractionType.triggerscoreachieved:
                    {
                        int score = 0;

                        IWiredTrigger handler = new ScoreAchieved(item, room.GetWiredHandler(), score, room.GetGameManager());
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);
                        break;
                    }

                case InteractionType.triggertimer:
                    {
                        int cycles = 0;

                        IWiredTrigger handler = new Timer(item, room.GetWiredHandler(), cycles, room.GetGameManager());
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);
                        break;
                    }

                case InteractionType.triggerstatechanged:
                    {
                        List<RoomItem> items = new List<RoomItem>();
                        int delay = 0;

                        IWiredTrigger handler = new SateChanged(room.GetWiredHandler(), item, items, delay);
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);

                        break;
                    }

                case InteractionType.triggerwalkofffurni:
                    {
                        List<RoomItem> items = new List<RoomItem>();

                        int delay = 0;

                        IWiredTrigger handler = new WalksOffFurni(item, room.GetWiredHandler(), items, delay);
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);

                        break;
                    }

                case InteractionType.triggerwalkonfurni:
                    {
                        List<RoomItem> items = new List<RoomItem>();

                        int delay = 0;

                        IWiredTrigger handler = new WalksOnFurni(item, room.GetWiredHandler(), items, delay);
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);

                        break;
                    }
            }
        }

        internal static void HandleSave(GameClient Session, uint itemID, Room room, ClientMessage clientMessage)
        {
            RoomItem item = room.GetRoomItemHandler().GetItem(itemID);
            if (item == null)
                return;

            if (item.wiredHandler != null)
            {
                item.wiredHandler.Dispose();
                item.wiredHandler = null;
            }
            //Logging.WriteLine("handle wired!");
            InteractionType type = item.GetBaseItem().InteractionType;
            switch (type)
            {
                case InteractionType.triggeronusersay:
                    {
                        int junk = clientMessage.ReadInt32();
                        bool isOnlyOwner = (clientMessage.ReadInt32() == 1);
                        string message = clientMessage.ReadString();
                        //Logging.WriteLine("Handle 'onusersay' itemid(" + item.Id + ") junk(" + junk + ") wired: isOnlyOwner(" + isOnlyOwner + ") message = " + message);
                        
                        IWiredTrigger handler = new UserSays(item, room.GetWiredHandler(), isOnlyOwner, message, room);
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);
                        break;
                    }
                case InteractionType.triggerwalkonfurni:
                    {
                        int junk = clientMessage.ReadInt32();
                        string message = clientMessage.ReadString();
                        int furniCount;
                        List<RoomItem> items = GetItems(clientMessage, room, out furniCount);
                        int delay = clientMessage.ReadInt32();

                        IWiredTrigger handler = new WalksOnFurni(item, room.GetWiredHandler(), items, delay);
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);
                        break;
                    }
                case InteractionType.triggerwalkofffurni:
                    {
                        int junk = clientMessage.ReadInt32();
                        string message = clientMessage.ReadString();
                        int furniCount;
                        List<RoomItem> items = GetItems(clientMessage, room, out furniCount);
                        int delay = clientMessage.ReadInt32();

                        IWiredTrigger handler = new WalksOffFurni(item, room.GetWiredHandler(), items, delay);
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);
                        break;
                    }
                case InteractionType.actionshowmessage:
                    {
                        int junk = clientMessage.ReadInt32();
                        string message = clientMessage.ReadString();

                        IWiredTrigger action = new ShowMessage(message, room.GetWiredHandler(), itemID);
                        HandleTriggerSave(action, room.GetWiredHandler(), room, itemID);
                        break;
                    }
                case InteractionType.actionteleportto:
                    {
                        int junk = clientMessage.ReadInt32();
                        string junk2 = clientMessage.ReadString();
                        int furniCount;
                        List<RoomItem> items = GetItems(clientMessage, room, out furniCount);
                        int delay = clientMessage.ReadInt32();

                        IWiredTrigger action = new TeleportToItem(room.GetGameMap(), room.GetWiredHandler(), items, delay, itemID);
                        HandleTriggerSave(action, room.GetWiredHandler(), room, itemID);
                        break;
                    }
                case InteractionType.actiontogglestate:
                    {
                        int junk = clientMessage.ReadInt32();
                        string message = clientMessage.ReadString();
                        int furniCount;
                        List<RoomItem> items = GetItems(clientMessage, room, out furniCount);
                        int delay = clientMessage.ReadInt32();
                        //Logging.WriteLine("Save action toogle wired with " + items.Count + " item(s) and " + delay + " second(s) of delay!");

                        IWiredTrigger action = new ToggleItemState(room.GetGameMap(), room.GetWiredHandler(), items, delay, item);
                        HandleTriggerSave(action, room.GetWiredHandler(), room, itemID);
                        break;
                    }
                case InteractionType.actionmoverotate:
                    {
                        int junk = clientMessage.ReadInt32();
                        MovementState movement = (MovementState)clientMessage.ReadInt32();
                        RotationState rotation = (RotationState)clientMessage.ReadInt32();

                        string junk2 = clientMessage.ReadString();
                        int furniCount;
                        List<RoomItem> items = GetItems(clientMessage, room, out furniCount);
                        int delay = clientMessage.ReadInt32();

                        IWiredTrigger handler = new MoveRotate(movement, rotation, items, delay, room, room.GetWiredHandler(), itemID);
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);
                        break;
                    }
                case InteractionType.actionposreset:
                    {

                        int junk = clientMessage.ReadInt32();
                        bool matchState = clientMessage.ReadInt32() == 1;
                        bool matchDirection = clientMessage.ReadInt32() == 1;
                        bool matchPosition = clientMessage.ReadInt32() == 1;
                        string junk2 = clientMessage.ReadString();

                        int furniCount;
                        List<RoomItem> items = GetItems(clientMessage, room, out furniCount);
                        int delay = clientMessage.ReadInt32();

                        IWiredTrigger action = new PositionReset(items, delay, room.GetRoomItemHandler(), room.GetWiredHandler(), itemID, matchState, matchDirection, matchPosition);
                        HandleTriggerSave(action, room.GetWiredHandler(), room, itemID);

                        break;
                    }

                case InteractionType.actionresettimer:
                    {

                        int junk = clientMessage.ReadInt32();
                        string junk2 = clientMessage.ReadString();
                        int furniCount;
                        List<RoomItem> items = GetItems(clientMessage, room, out furniCount);
                        int delay = clientMessage.ReadInt32();

                        IWiredTrigger action = new TimerReset(room, room.GetWiredHandler(), items, delay, itemID);
                        HandleTriggerSave(action, room.GetWiredHandler(), room, itemID);

                        break;
                    }
                case InteractionType.actiongivescore:
                    {
                        int junk = clientMessage.ReadInt32();
                        int points = clientMessage.ReadInt32();
                        int games = clientMessage.ReadInt32();

                        IWiredTrigger action = new GiveScore(games, points, room.GetGameManager(), itemID);
                        HandleTriggerSave(action, room.GetWiredHandler(), room, itemID);

                        break;
                    }
                case InteractionType.triggergameend:
                    {
                        IWiredTrigger handler = new GameEnds(item, room.GetWiredHandler(), room.GetGameManager());
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);
                        break;
                    }

                case InteractionType.triggergamestart:
                    {
                        IWiredTrigger handler = new GameStarts(item, room.GetWiredHandler(), room.GetGameManager());
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);
                        break;
                    }
                case InteractionType.triggerrepeater:
                    {
                        int junk = clientMessage.ReadInt32();
                        int cycleTimes = clientMessage.ReadInt32();

                        IWiredTrigger handler = new Repeater(room.GetWiredHandler(), item, cycleTimes);
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);

                        break;
                    }

                case InteractionType.triggerroomenter:
                    {
                        int junk = clientMessage.ReadInt32();
                        string users = clientMessage.ReadString();

                        IWiredTrigger handler = new EntersRoom(item, room.GetWiredHandler(), room.GetRoomUserManager(), !string.IsNullOrEmpty(users), users);
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);
                        break;
                    }

                case InteractionType.triggerscoreachieved:
                    {
                        int junk = clientMessage.ReadInt32();
                        int score = clientMessage.ReadInt32();

                        IWiredTrigger handler = new ScoreAchieved(item, room.GetWiredHandler(), score, room.GetGameManager());
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);

                        break;
                    }

                case InteractionType.triggertimer:
                    {
                        int junk = clientMessage.ReadInt32();
                        int cycles = clientMessage.ReadInt32();

                        IWiredTrigger handler = new Timer(item, room.GetWiredHandler(), cycles, room.GetGameManager());
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);

                        break;
                    }

                case InteractionType.triggerstatechanged:
                    {
                        int junk = clientMessage.ReadInt32();
                        string junk2 = clientMessage.ReadString();

                        int furniAmount;
                        List<RoomItem> items = GetItems(clientMessage, room, out furniAmount);
                        int delay = clientMessage.ReadInt32();

                        IWiredTrigger handler = new SateChanged(room.GetWiredHandler(), item, items, delay);
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);

                        break;
                    }
            }
            Session.SendMessage(new ServerMessage(Outgoing.SaveWired));
            /*switch (type)
            {
                case InteractionType.actiongivescore:
                    {
                        clientMessage.AdvancePointer(1);
                        int points = clientMessage.PopWiredInt32();
                        int games = clientMessage.PopWiredInt32();

                        IWiredTrigger action = new GiveScore(games, points, room.GetGameManager(), itemID);
                        HandleTriggerSave(action, room.GetWiredHandler(), room, itemID);

                        break;
                    }

                case InteractionType.actionmoverotate:
                    {
                        clientMessage.AdvancePointer(1);
                        MovementState movement = (MovementState)clientMessage.PopWiredInt32();
                        RotationState rotation = (RotationState)clientMessage.PopWiredInt32();

                        clientMessage.AdvancePointer(2);
                        int furniCount;
                        List<RoomItem> items = GetItems(clientMessage, room, out furniCount);
                        int delay = clientMessage.PopWiredInt32();

                        IWiredTrigger action = new MoveRotate(movement, rotation, items, delay, room, room.GetWiredHandler(), itemID);
                        HandleTriggerSave(action, room.GetWiredHandler(), room, itemID);

                        break;
                    }

                case InteractionType.actionposreset:
                    {

                        clientMessage.AdvancePointer(3);
                        int furniCount;
                        List<RoomItem> items = GetItems(clientMessage, room, out furniCount);
                        int delay = clientMessage.PopWiredInt32();

                        IWiredTrigger action = new PositionReset(items, delay, room.GetRoomItemHandler(), room.GetWiredHandler(), itemID);
                        HandleTriggerSave(action, room.GetWiredHandler(), room, itemID);

                        break;
                    }

                case InteractionType.actionresettimer:
                    {

                        clientMessage.AdvancePointer(3);
                        int furniCount;
                        List<RoomItem> items = GetItems(clientMessage, room, out furniCount);
                        int delay = clientMessage.PopWiredInt32();

                        IWiredTrigger action = new TimerReset(room, room.GetWiredHandler(), items, delay, itemID);
                        HandleTriggerSave(action, room.GetWiredHandler(), room, itemID);

                        break;
                    }

                case InteractionType.actionshowmessage:
                    {
                        clientMessage.AdvancePointer(1);
                        string message = clientMessage.PopFixedString();

                        IWiredTrigger action = new ShowMessage(message, room.GetWiredHandler(), itemID);
                        HandleTriggerSave(action, room.GetWiredHandler(), room, itemID);
                        break;
                    }

                case InteractionType.actionteleportto:
                    {
                        clientMessage.AdvancePointer(3);
                        int furniCount;
                        List<RoomItem> items = GetItems(clientMessage, room, out furniCount);
                        int delay = clientMessage.PopWiredInt32();

                        IWiredTrigger action = new TeleportToItem(room.GetGameMap(), room.GetWiredHandler(), items, delay, itemID);
                        HandleTriggerSave(action, room.GetWiredHandler(), room, itemID);

                        break;
                    }

                case InteractionType.actiontogglestate:
                    {
                        clientMessage.AdvancePointer(3);
                        int furniCount;
                        List<RoomItem> items = GetItems(clientMessage, room, out furniCount);
                        int delay = clientMessage.PopWiredInt32();

                        IWiredTrigger action = new ToggleItemState(room.GetGameMap(), room.GetWiredHandler(), items, delay, itemID);
                        HandleTriggerSave(action, room.GetWiredHandler(), room, itemID);

                        break;
                    }


                case InteractionType.conditionfurnishaveusers:
                    {
                        clientMessage.AdvancePointer(1);
                        bool a = clientMessage.PopWiredBoolean();
                        bool b = clientMessage.PopWiredBoolean();
                        bool c = clientMessage.PopWiredBoolean();
                        clientMessage.AdvancePointer(2);

                        int furniCount;
                        List<RoomItem> items = GetItems(clientMessage, room, out furniCount);


                        if (a)
                        {
                            int a1 = 2;
                        }

                        break;
                    }

                case InteractionType.conditionstatepos:
                    {

                        break;
                    }

                case InteractionType.conditiontimelessthan:
                    {

                        break;
                    }

                case InteractionType.conditiontimemorethan:
                    {

                        break;
                    }

                case InteractionType.conditiontriggeronfurni:
                    {

                        break;
                    }

                case InteractionType.specialrandom:
                    {

                        break;
                    }

                case InteractionType.specialunseen:
                    {

                        break;
                    }

                case InteractionType.triggergameend:
                    {
                        IWiredTrigger handler = new GameEnds(item, room.GetWiredHandler(), room.GetGameManager());
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);
                        break;
                    }

                case InteractionType.triggergamestart:
                    {
                        IWiredTrigger handler = new GameStarts(item, room.GetWiredHandler(), room.GetGameManager());
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);
                        break;
                    }

                case InteractionType.triggeronusersay:
                    {
                        clientMessage.AdvancePointer(1);
                        bool isOnlyOwner = clientMessage.PopWiredBoolean();
                        clientMessage.AdvancePointer(0);
                        string message = clientMessage.PopFixedString();
                        string stuff = clientMessage.ToString();

                        IWiredTrigger handler = new UserSays(item, room.GetWiredHandler(), isOnlyOwner, message, room);
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);

                        break;
                    }

                case InteractionType.triggerrepeater:
                    {
                        clientMessage.AdvancePointer(1);
                        int cycleTimes = clientMessage.PopWiredInt32();
                       
                        IWiredTrigger handler = new Repeater(room.GetWiredHandler(), item, cycleTimes);
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);
                       
                        break;
                    }

                case InteractionType.triggerroomenter:
                    {
                        clientMessage.AdvancePointer(1);
                        string users = clientMessage.PopFixedString();

                        IWiredTrigger handler = new EntersRoom(item, room.GetWiredHandler(), room.GetRoomUserManager(), !string.IsNullOrEmpty(users), users);
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);
                        break;
                    }

                case InteractionType.triggerscoreachieved:
                    {
                        clientMessage.AdvancePointer(1);
                        int score = clientMessage.PopWiredInt32();

                        IWiredTrigger handler = new ScoreAchieved(item, room.GetWiredHandler(), score, room.GetGameManager());
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);

                        break;
                    }

                case InteractionType.triggertimer:
                    {
                        clientMessage.AdvancePointer(1);
                        int cycles = clientMessage.PopWiredInt32();

                        IWiredTrigger handler = new Timer(item, room.GetWiredHandler(), cycles, room.GetGameManager());
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);

                        break;
                    }

                case InteractionType.triggerstatechanged:
                    {
                        clientMessage.AdvancePointer(3);

                        int furniAmount;
                        List<RoomItem> items = GetItems(clientMessage, room, out furniAmount);
                        int delay = clientMessage.PopWiredInt32();

                        IWiredTrigger handler = new SateChanged(room.GetWiredHandler(), item, items, delay);
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);

                        break;
                    }

                case InteractionType.triggerwalkofffurni:
                    {
                        clientMessage.AdvancePointer(3);

                        int furniAmount;
                        List<RoomItem> items = GetItems(clientMessage, room, out furniAmount);

                        int delay = clientMessage.PopWiredInt32();

                        IWiredTrigger handler = new WalksOffFurni(item, room.GetWiredHandler(), items, delay);
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);
                        break;
                    }

                case InteractionType.triggerwalkonfurni:
                    {
                        clientMessage.AdvancePointer(3);
                        int furniAmount;
                        List<RoomItem> items = GetItems(clientMessage, room, out furniAmount);

                        int delay = clientMessage.PopWiredInt32();

                        IWiredTrigger handler = new WalksOnFurni(item, room.GetWiredHandler(), items, delay);
                        HandleTriggerSave(handler, room.GetWiredHandler(), room, itemID);

                        break;
                    }
            }*/
        }

        internal static void HandleConditionSave(GameClient Session, uint itemID, Room room, ClientMessage clientMessage)
        {
            RoomItem item = room.GetRoomItemHandler().GetItem(itemID);
            if (item == null)
                return;

            if (item.wiredCondition != null)
            {
                room.GetWiredHandler().conditionHandler.RemoveRefferance(item, item.Coordinate);
                item.wiredCondition.Dispose();
                item.wiredCondition = null;
            }

            InteractionType type = item.GetBaseItem().InteractionType;

            if (type != InteractionType.conditionfurnishaveusers && type != InteractionType.conditionstatepos &&
                type != InteractionType.conditiontimelessthan && type != InteractionType.conditiontimemorethan &&
                type != InteractionType.conditiontriggeronfurni)
                return;

            // Parse data
            int[] intParams = new int[clientMessage.ReadInt32()];
            for (int i = 0; i < intParams.Length; i++)
                intParams[i] = clientMessage.ReadInt32();
            string stringParam = clientMessage.ReadString();

            int furniCount;
            List<RoomItem> items = GetItems(clientMessage, room, out furniCount);

            int stuffSelectionType = clientMessage.ReadInt32();

            IWiredCondition handler = null;
            int timerValue = intParams.Length > 0 ? intParams[0] : 0;

            switch (type)
            {
                case InteractionType.conditionfurnishaveusers:
                    {
                        handler = new FurniHasUser(item, items);
                        break;
                    }
                case InteractionType.conditionstatepos:
                    {
                        bool matchState = intParams.Length == 0 || intParams[0] == 1;
                        bool matchDirection = intParams.Length < 2 || intParams[1] == 1;
                        bool matchPosition = intParams.Length < 3 || intParams[2] == 1;

                        handler = new FurniStatePosMatch(item, items, matchState, matchDirection, matchPosition);
                        break;
                    }

                case InteractionType.conditiontimelessthan:
                    {
                        handler = new LessThanTimer(timerValue, room, item);
                        break;
                    }

                case InteractionType.conditiontimemorethan:
                    {
                        handler = new MoreThanTimer(timerValue, room, item);
                        break;
                    }

                case InteractionType.conditiontriggeronfurni:
                    {
                        handler = new TriggerUserIsOnFurni(item, items);
                        break;
                    }

                default:
                    return;
            }

            item.wiredCondition = handler;
            room.GetWiredHandler().conditionHandler.AddOrIgnoreRefferance(item);

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                handler.SaveToDatabase(dbClient);
            }
            Session.SendMessage(new ServerMessage(Outgoing.SaveWired));
        }

        private static List<RoomItem> GetItems(ClientMessage message, Room room, out int itemCount)
        {
            List<RoomItem> items = new List<RoomItem>();
            itemCount = message.ReadInt32();

            uint itemID;
            RoomItem item;
            for (int i = 0; i < itemCount; i++)
            {
                itemID = message.ReadUInt32();
                item = room.GetRoomItemHandler().GetItem(itemID);

                if (item != null && !WiredUtillity.TypeIsWired(item.GetBaseItem().InteractionType))
                    items.Add(item);
            }

            return items;
        }

        private static void HandleTriggerSave(IWiredTrigger handler, WiredHandler manager, Room room, uint itemID)
        {
            RoomItem item = room.GetRoomItemHandler().GetItem(itemID);
            if (item == null)
                return;

            item.wiredHandler = handler;
            manager.RemoveFurniture(item); //Removes it from le manager just in case there is annything registered allready
            manager.AddFurniture(item);

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                handler.SaveToDatabase(dbClient);
            }
        }
    }
}
