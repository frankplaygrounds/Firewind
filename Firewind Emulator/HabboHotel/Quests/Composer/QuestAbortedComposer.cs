using Firewind.Messages;

namespace Firewind.HabboHotel.Quests.Composer
{
    class QuestAbortedComposer
    {
        internal static ServerMessage Compose()
        {
            ServerMessage Message = new ServerMessage(1154);
            Message.AppendBoolean(false);
            return Message;
        }
    }
}
