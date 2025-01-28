using System;
using System.Data;
using Firewind.HabboHotel.GameClients;

namespace Firewind.HabboHotel.Users.Authenticator
{
    static class HabboFactory
    {
        internal static Habbo GenerateHabbo(DataRow dRow)
        {
            uint id = Convert.ToUInt32(dRow["id"]);
            string username = (string)dRow["username"];
            string realname = (string)dRow["real_name"];
            uint rank = Convert.ToUInt32(dRow["rank"]);
            string motto = (string)dRow["motto"];
            string look = (string)dRow["look"];
            string gender = (string)dRow["gender"];
            string lastonline = Convert.ToString(dRow["last_online"]);
            int credits = (int)dRow["credits"];
            int activityPoints = (int)dRow["activity_points"];
            double activityPointsLastUpdate = Convert.ToDouble(dRow["activity_points_lastupdate"]);
            bool isMuted = FirewindEnvironment.EnumToBool(dRow["is_muted"].ToString());
            uint homeRoom = Convert.ToUInt32(dRow["home_room"]);
            int respect = (Int32)dRow["respect"];
            int dailyRespect = (int)dRow["daily_respect_points"];
            int dailyPetRespect = (int)dRow["daily_pet_respect_points"];
            bool mtantPenalty = (dRow["mutant_penalty"].ToString() != "0");
            bool blockFriends = FirewindEnvironment.EnumToBool(dRow["block_newfriends"].ToString());
            uint questID = Convert.ToUInt32(dRow["currentquestid"]);
            int questProgress = Convert.ToInt32(dRow["currentquestprogress"]);
            int achiecvementPoints = Convert.ToInt32(dRow["achievement_points"]);
            int vippoints = Convert.ToInt32(dRow["vip_points"]);
            int favgroup = dRow["favourite_group"] == DBNull.Value ? 0 : Convert.ToInt32(dRow["favourite_group"]);
            string AccountCreated = (string)dRow["account_created"];
            return new Habbo(id, username, realname, rank, motto, look, gender, credits, vippoints, activityPoints, activityPointsLastUpdate, isMuted, homeRoom, respect, dailyRespect, dailyPetRespect, mtantPenalty, blockFriends, questID, questProgress, achiecvementPoints, lastonline, favgroup, AccountCreated);
        }
    }
}
