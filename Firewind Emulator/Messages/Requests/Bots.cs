using System;
using System.Data;
using Firewind.HabboHotel.Misc;
using Firewind.HabboHotel.Rooms;
using Firewind.HabboHotel.Users;
using Firewind.HabboHotel.Users.Badges;
using Database_Manager.Database.Session_Details.Interfaces;
using HabboEvents;
using Database_Manager;
using Firewind.Messages;
using Firewind.HabboHotel.GameClients;
using System.Drawing;
using Firewind.HabboHotel.Pets;
using Firewind.Core;
using Firewind.HabboHotel.RoomBots;
using System.Collections.Generic;
using Firewind.HabboHotel.Items;
using Firewind.HabboHotel.Catalogs;

namespace Firewind.Messages
{
    partial class GameClientMessageHandler
    {
        internal void KickBot()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null)
            {
                return;
            }

            int botReference = Request.ReadInt32();
            RoomUser Bot = Room.GetRoomUserManager().GetRoomUserByVirtualId(botReference);
            if (Bot == null)
            {
                long botId = botReference;
                if (botId < 0)
                    botId = -botId;

                if (botId <= UInt32.MaxValue)
                    Bot = Room.GetRoomUserManager().GetBotByBotId((uint)botId);
            }

            if (Bot == null || !Bot.IsBot)
            {
                return;
            }

            if (!Room.CheckRights(Session, true) && (Bot.BotData == null || Bot.BotData.OwnerId != Session.GetHabbo().Id))
            {
                return;
            }

            if (Bot.BotData != null && Bot.BotData.OwnerId > 0)
            {
                if (Bot.BotData.IsExpired)
                {
                    Catalog.DeleteUserBot(Bot.BotData.BotId);
                    if (Bot.BotData.OwnerId == Session.GetHabbo().Id)
                        Session.GetHabbo().GetInventoryComponent().RemoveBot(Bot.BotData.BotId);
                }
                else
                {
                    using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                    {
                        dbClient.setQuery("UPDATE user_bots SET room_id = 0, x = 0, y = 0, z = 0 WHERE id = @id LIMIT 1");
                        dbClient.addParameter("id", Bot.BotData.BotId);
                        dbClient.runQuery();
                    }

                    if (Bot.BotData.OwnerId == Session.GetHabbo().Id)
                        Session.GetHabbo().GetInventoryComponent().AddBot(Bot.BotData);
                }

                if (Bot.BotData.OwnerId == Session.GetHabbo().Id)
                    Session.SendMessage(Session.GetHabbo().GetInventoryComponent().SerializeBotInventory());
            }

            Room.GetRoomUserManager().RemoveBot(Bot.VirtualId, true);
        }

        internal void PlacePet()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || (!Room.AllowPets && !Room.CheckRights(Session, true)) || !Room.CheckRights(Session, true))
            {
                return;
            }
            if (Room.GetRoomUserManager().GetPetCount() >= 10) // TODO: Not hardcoded message and amount + placepetfailed message
            {
                Session.SendNotif("You can't put down any more pets!");
                return;
            }

            uint PetId = Request.ReadUInt32();

            Pet Pet = Session.GetHabbo().GetInventoryComponent().GetPet(PetId);

            if (Pet == null || Pet.PlacedInRoom)
            {
                return;
            }

            int X = Request.ReadInt32();
            int Y = Request.ReadInt32();

            if (!Room.GetGameMap().CanWalk(X, Y, false))
            {
                return;
            }

            //if (Room.GetRoomUserManager().PetCount >= RoomManager.MAX_PETS_PER_ROOM)
            //{
            //    Session.SendNotif(LanguageLocale.GetValue("user.maxpetreached"));
            //    return;
            //}

            RoomUser oldPet = Room.GetRoomUserManager().GetPet(PetId);
            if (oldPet != null)
                Room.GetRoomUserManager().RemoveBot(oldPet.VirtualId, false);

            Pet.PlacedInRoom = true;
            Pet.RoomId = Room.RoomId;

            List<RandomSpeech> RndSpeechList = new List<RandomSpeech>();
            List<BotResponse> BotResponse = new List<BotResponse>();
            RoomUser PetUser = Room.GetRoomUserManager().DeployBot(new RoomBot(Pet.PetId, Pet.RoomId, AIType.Pet, "freeroam", Pet.Name, "", Pet.Look, X, Y, 0, 0, 0, 0, 0, 0, ref RndSpeechList, ref BotResponse), Pet);

            Session.GetHabbo().GetInventoryComponent().MovePetToRoom(Pet.PetId);

            if (Pet.DBState != DatabaseUpdateState.NeedsInsert)
                Pet.DBState = DatabaseUpdateState.NeedsUpdate;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                Room.GetRoomUserManager().SavePets(dbClient);

            Session.SendMessage(Session.GetHabbo().GetInventoryComponent().SerializePetInventory());
        }

        internal void GetPetInfo()
        {
            if (Session.GetHabbo() == null || Session.GetHabbo().CurrentRoom == null)
                return;

            RoomUser pet = Session.GetHabbo().CurrentRoom.GetRoomUserManager().GetPet(Request.ReadUInt32());
            if (pet == null || pet.PetData == null)
            {
                Session.SendNotif(LanguageLocale.GetValue("user.petinfoerror"));
                return;
            }

            Session.SendMessage(pet.PetData.SerializeInfo());
        }

        internal void PickUpPet()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Session == null || Session.GetHabbo() == null || Session.GetHabbo().GetInventoryComponent() == null)
                return;

            if (Room == null || (!Room.AllowPets && !Room.CheckRights(Session, true)))
            {
                return;
            }

            uint PetId = Request.ReadUInt32();
            RoomUser PetUser = Room.GetRoomUserManager().GetPet(PetId);
            if (PetUser == null)
                return;

            if (PetUser.isMounted == true)
            {
                RoomUser usuarioVinculado = Room.GetRoomUserManager().GetRoomUserByVirtualId(Convert.ToInt32(PetUser.mountID));
                if (usuarioVinculado != null)
                {
                    usuarioVinculado.isMounted = false;
                    usuarioVinculado.ApplyEffect(-1);
                    usuarioVinculado.MoveTo(new Point(usuarioVinculado.X + 1, usuarioVinculado.Y + 1));
                }
            }

            if (PetUser.PetData.DBState != DatabaseUpdateState.NeedsInsert)
                PetUser.PetData.DBState = DatabaseUpdateState.NeedsUpdate;
            PetUser.PetData.RoomId = 0;

            Session.GetHabbo().GetInventoryComponent().AddPet(PetUser.PetData);

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
                Room.GetRoomUserManager().SavePets(dbClient);

            Room.GetRoomUserManager().RemoveBot(PetUser.VirtualId, false);
            Session.SendMessage(Session.GetHabbo().GetInventoryComponent().SerializePetInventory());
        }

        internal void RespectPet()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || (!Room.AllowPets))
            {
                return;
            }

            uint PetId = Request.ReadUInt32();
            RoomUser PetUser = Room.GetRoomUserManager().GetPet(PetId);

            if (PetUser == null || PetUser.PetData == null)
            {
                return;
            }

            PetUser.PetData.OnRespect();
            Session.GetHabbo().DailyPetRespectPoints--;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                //dbClient.addParameter("userid", Session.GetHabbo().Id);
                dbClient.runFastQuery("UPDATE users SET daily_pet_respect_points = daily_pet_respect_points - 1 WHERE id = " + Session.GetHabbo().Id);
            }
        }

        internal void AddSaddle()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || (!Room.AllowPets && !Room.CheckRights(Session, true)))
            {
                return;
            }

            uint ItemId = Request.ReadUInt32();
            RoomItem Item = Room.GetRoomItemHandler().GetItem(ItemId);
            if (Item == null)
                return; ;

            uint PetId = Request.ReadUInt32();
            RoomUser PetUser = Room.GetRoomUserManager().GetPet(PetId);

            if (PetUser == null || PetUser.PetData == null || PetUser.PetData.OwnerId != Session.GetHabbo().Id)
            {
                return;
            }

            Room.GetRoomItemHandler().RemoveFurniture(Session, Item.Id);
            PetUser.PetData.HaveSaddle = true;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                //dbClient.addParameter("userid", Session.GetHabbo().Id);
                dbClient.runFastQuery("UPDATE user_pets SET have_saddle = 1 WHERE id = " + PetUser.PetData.PetId);
            }
        }

        internal void RemoveSaddle()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || (!Room.AllowPets && !Room.CheckRights(Session, true)))
            {
                return;
            }

            uint PetId = Request.ReadUInt32();
            RoomUser PetUser = Room.GetRoomUserManager().GetPet(PetId);

            if (PetUser == null || PetUser.PetData == null || PetUser.PetData.OwnerId != Session.GetHabbo().Id)
            {
                return;
            }

            FirewindEnvironment.GetGame().GetCatalog().DeliverItems(Session, FirewindEnvironment.GetGame().GetItemManager().GetItem((uint)2804), 1, "");
            PetUser.PetData.HaveSaddle = false;

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                //dbClient.addParameter("userid", Session.GetHabbo().Id);
                dbClient.runFastQuery("UPDATE user_pets SET have_saddle = 0 WHERE id = " + PetUser.PetData.PetId);
            }
        }

        internal void MountPet()
        {
            // RWUAM_MOUNT_PET
            // RWUAM_DISMOUNT_PET

            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            //if (Room == null || Room.IsPublic || (!Room.AllowPets && !Room.CheckRights(Session, true)))
            if (Room == null)
            {
                return;
            }


            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (User == null)
                return;

            uint PetId = Request.ReadUInt32();
            // true = RWUAM_MOUNT_PET, false = RWUAM_DISMOUNT_PET
            bool mountOn = Request.ReadBoolean();
            RoomUser Pet = Room.GetRoomUserManager().GetPet(PetId);

            //if (Pet == null || Pet.PetData == null || Pet.PetData.OwnerId != Session.GetHabbo().Id)
            if (Pet == null || Pet.PetData == null || !Pet.PetData.HaveSaddle)
            {
                return;
            }

            // GET TO DA CHO-- ..HORSE!
            if (mountOn)
            {
                if (User.isMounted == true || Pet.isMounted)
                {
                    string[] Speech2 = PetLocale.GetValue("pet.alreadymounted");
                    Random RandomSpeech2 = new Random();
                    Pet.Chat(null, Speech2[RandomSpeech2.Next(0, Speech2.Length - 1)], false);
                }
                else
                {
                    Pet.Statusses.Remove("sit");
                    Pet.Statusses.Remove("lay");
                    Pet.Statusses.Remove("snf");
                    Pet.Statusses.Remove("eat");
                    Pet.Statusses.Remove("ded");
                    Pet.Statusses.Remove("jmp");
                    int NewX2 = User.X;
                    int NewY2 = User.Y;
                    Pet.PetData.AddExpirience(10); // Give XP
                    Room.SendMessage(Room.GetRoomItemHandler().UpdateUserOnRoller(Pet, new Point(NewX2, NewY2), 0, Room.GetGameMap().SqAbsoluteHeight(NewX2, NewY2)));
                    Room.GetRoomUserManager().UpdateUserStatus(Pet, false);
                    Room.SendMessage(Room.GetRoomItemHandler().UpdateUserOnRoller(User, new Point(NewX2, NewY2), 0, Room.GetGameMap().SqAbsoluteHeight(NewX2, NewY2) + 1));
                    Room.GetRoomUserManager().UpdateUserStatus(User, false);
                    Pet.ClearMovement(true);
                    User.isMounted = true;
                    Pet.isMounted = true;
                    Pet.mountID = (uint)User.VirtualId;
                    User.mountID = Convert.ToUInt32(Pet.VirtualId);
                    User.ApplyEffect(77);
                    User.MoveTo(NewX2 + 1, NewY2 + 1);
                }
            }
            else
            {
                Pet.Statusses.Remove("sit");
                Pet.Statusses.Remove("lay");
                Pet.Statusses.Remove("snf");
                Pet.Statusses.Remove("eat");
                Pet.Statusses.Remove("ded");
                Pet.Statusses.Remove("jmp");
                User.isMounted = false;
                User.mountID = 0;
                Pet.isMounted = false;
                Pet.mountID = 0;
                User.MoveTo(User.X + 1, User.Y + 1);
                User.ApplyEffect(-1);
            }
        }

        internal void GetPetCommands()
        {
            uint PetID = Request.ReadUInt32();
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            RoomUser PetUser = Room.GetRoomUserManager().GetPet(PetID);

            if (PetUser == null || PetUser.PetData == null)
                return;

            GetResponse().Init(Outgoing.PetCommands);
            GetResponse().AppendUInt(PetID); // petId

            int level = PetUser.PetData.Level;

            GetResponse().AppendInt32(18); // allCommands count
            for (int i = 0; i < 18; i++)
                GetResponse().AppendInt32(i);

            GetResponse().AppendInt32(Math.Min(level, 18)); // enabledCommands count
            for (int i = 0; i < Math.Min(level, 18); i++)
                GetResponse().AppendInt32(i);

            SendResponse();
        }

        internal void AnyoneRide()
        {
            int ID = Request.ReadInt32(); // Get Next ID
            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("SELECT * FROM user_pets WHERE id='" + ID + "'");
                DataRow Row = dbClient.getRow();
                if ((string)Row[3] != "")
                {
                    int Next = 2;

                    if ((string)Row[16] == "0")
                    {
                        Next = 1;
                        Session.SendBroadcastMessage("Users can't Ride Now (Except by You)");
                    }
                    if ((string)Row[16] == "1")
                    {
                        Next = 0;
                        Session.SendBroadcastMessage("Users can Ride Now");
                    }
                    dbClient.runFastQuery("UPDATE user_pets SET everyone_can_ride='" + Next + "' WHERE id='" + ID + "'");
                }

            }
        }

        internal void GetBotInventory()
        {
            if (Session == null || Session.GetHabbo() == null || Session.GetHabbo().GetInventoryComponent() == null)
                return;

            Session.SendMessage(Session.GetHabbo().GetInventoryComponent().SerializeBotInventory());
        }

        internal void PlaceBot()
        {
            Room Room = FirewindEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);

            if (Room == null || !Room.CheckRights(Session, true))
                return;

            uint botID = Request.ReadUInt32();
            int x = Request.ReadInt32();
            int y = Request.ReadInt32();

            RoomBot Bot = Session.GetHabbo().GetInventoryComponent().GetBot(botID);
            if (Bot == null)
                return;

            if (Bot.IsExpired)
            {
                Catalog.DeleteUserBot(botID);
                Session.GetHabbo().GetInventoryComponent().RemoveBot(botID);
                Session.SendMessage(Session.GetHabbo().GetInventoryComponent().SerializeBotInventory());
                return;
            }

            if (!Room.GetGameMap().SquareIsOpen(x, y, false) || !Room.GetGameMap().CanWalk(x, y, false))
                return;

            int z = (int)Room.GetGameMap().SqAbsoluteHeight(x, y);
            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("UPDATE user_bots SET room_id = @room_id, x = @x, y = @y, z = @z WHERE id = @id AND user_id = @user_id LIMIT 1");
                dbClient.addParameter("room_id", Room.RoomId);
                dbClient.addParameter("x", x);
                dbClient.addParameter("y", y);
                dbClient.addParameter("z", z);
                dbClient.addParameter("id", botID);
                dbClient.addParameter("user_id", Session.GetHabbo().Id);
                dbClient.runQuery();
            }

            Bot.RoomId = Room.RoomId;
            Bot.X = x;
            Bot.Y = y;
            Bot.Z = z;
            Room.GetRoomUserManager().DeployBot(Bot, null);

            Session.GetHabbo().GetInventoryComponent().MoveBotToRoom(botID);
            Session.SendMessage(Session.GetHabbo().GetInventoryComponent().SerializeBotInventory());
        }
    }
}
