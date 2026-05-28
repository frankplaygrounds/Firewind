using System;
using Firewind.HabboHotel.GameClients;
using Firewind.HabboHotel.Rooms;
using Database_Manager.Database.Session_Details.Interfaces;

namespace Firewind.HabboHotel.ChatMessageStorage
{
    class ChatMessageFactory
    {
        internal static ChatMessage CreateMessage(string message, GameClient user, Room room)
        {
            uint userID = user.GetHabbo().Id;
            string username = user.GetHabbo().Username;
            uint roomID = room.RoomId;
            string roomName = room.Name;
            DateTime timeSpoken = DateTime.Now;

            ChatMessage chatMessage = new ChatMessage(userID, username, roomID, roomName, message, timeSpoken);

            using (IQueryAdapter dbClient = FirewindEnvironment.GetDatabaseManager().getQueryreactor())
            {
                dbClient.setQuery("INSERT into `chatlogs`(`user_id`, `room_id`, `hour`, `minute`, `full_date`, `timestamp`, `message`, `user_name`) VALUES(" + userID + ", " + roomID + ", " + timeSpoken.Hour + ", " + timeSpoken.Minute + ", '" + timeSpoken.ToString() + "', " + FirewindEnvironment.GetUnixTimestamp() + ", @msg, @username);");
                dbClient.addParameter("msg", message);
                dbClient.addParameter("username", username);
                dbClient.runQuery();
            }

            return chatMessage;
        }
    }
}
