using Firewind.Messages;

namespace Firewind.HabboHotel.Quests.Composer
{
    class QuestAbortedComposer
    {
        internal static ServerMessage Compose()
        {
            return new ServerMessage(1154);
        }
    }
}
