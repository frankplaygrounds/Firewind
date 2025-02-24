using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Firewind.Messages;
using Firewind.HabboHotel.GameClients;
using Firewind.HabboHotel.Quests;

namespace Firewind.HabboHotel.Quests.Composer
{
    class QuestCompletedComposer
    {
        internal static ServerMessage Compose(GameClient Session, Quest Quest)
        {
            ServerMessage Message = new ServerMessage(518);
            QuestListComposer.SerializeQuest(Message, Session, Quest, Quest.Category);
            return Message;
        }
    }
}
