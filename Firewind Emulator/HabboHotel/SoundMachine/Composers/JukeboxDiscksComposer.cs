using System.Collections.Generic;
using Firewind.HabboHotel.GameClients;
using Firewind.HabboHotel.Items;
using Firewind.Messages;
using System.Collections;
using HabboEvents;
using System;

namespace Firewind.HabboHotel.SoundMachine.Composers
{
    class JukeboxComposer
    {
        internal static ServerMessage Compose(GameClient Session)
        {
            return Session.GetHabbo().GetInventoryComponent().SerializeMusicDiscs();
        }

        internal static ServerMessage Compose(int PlaylistCapacity, List<SongInstance> Playlist)
        {
            ServerMessage Message = new ServerMessage(Outgoing.JukeboxSongDisks);
            Message.AppendInt32(PlaylistCapacity);
            Message.AppendInt32(Playlist.Count);

            foreach (SongInstance Song in Playlist)
            {
                Message.AppendUInt(Song.DiskItem.itemID);
                Message.AppendUInt(Song.SongData.Id);
                Message.AppendString(Song.SongData.Name);
                Message.AppendString(Song.SongData.Data);
            }

            return Message;
        }

        internal static ServerMessage Compose(uint SongId, int PlaylistItemNumber, int SyncTimestampMs)
        {
            ServerMessage Message = new ServerMessage(327);

            if (SongId == 0)
            {
                Message.AppendInt32(-1);
                Message.AppendInt32(-1);
                Message.AppendInt32(-1);
                Message.AppendInt32(-1);
                Message.AppendInt32(0);
            }
            else
            {
                Message.AppendUInt(SongId);
                Message.AppendInt32(PlaylistItemNumber);
                Message.AppendUInt(SongId);
                Message.AppendInt32(0);
                Message.AppendInt32(SyncTimestampMs);
            }

            return Message;
        }

        public static ServerMessage Compose(List<SongData> Songs)
        {
            ServerMessage Message = new ServerMessage(Outgoing.TraxSongInfo);
            Message.AppendInt32(Songs.Count);

            foreach (SongData Song in Songs)
            {
                Message.AppendUInt(Song.Id);
                Message.AppendString(Song.Name);
                Message.AppendString(Song.Data);
                Message.AppendInt32(Song.LengthMiliseconds);
                Message.AppendString(Song.Artist);
            }

            return Message;
        }

        public static ServerMessage ComposePlayingComposer(uint SongId, int PlaylistItemNumber, int SyncTimestampMs)
        {
            ServerMessage Message = new ServerMessage(Outgoing.NowPlaying);

            if (SongId == 0)
            {
                Message.AppendInt32(-1);
                Message.AppendInt32(-1);
                Message.AppendInt32(-1);
                Message.AppendInt32(-1);
                Message.AppendInt32(0);
            }
            else
            {
                Message.AppendUInt(SongId);
                Message.AppendInt32(PlaylistItemNumber);
                Message.AppendUInt(SongId);
                Message.AppendInt32(0);
                Message.AppendInt32(SyncTimestampMs);
            }

            return Message;
        }

        internal static ServerMessage SerializeSongInventory(Hashtable songs)
        {
            ServerMessage message = new ServerMessage(Outgoing.UserSongDisksInventory);
            message.AppendInt32(songs.Count);

                foreach (UserItem item in songs.Values)
                {
                    int i = (int)TextHandling.Parse(item.Data.ToString());
                    message.AppendInt32((int)item.Id);
                    message.AppendInt32(i);
                }

            return message;
        }
    }
}
