using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Firewind.Messages;
using Firewind.HabboHotel.Quests;
using Firewind.HabboHotel.GameClients;

namespace Firewind.HabboHotel.Quests.Composer
{
    class QuestStartedComposer
    {
        internal static ServerMessage Compose(GameClient Session, Quest Quest)
        {
            ServerMessage Message = new ServerMessage(3396);
            QuestListComposer.SerializeQuest(Message, Session, Quest, Quest.Category);
            return Message;
        }
    }
}
