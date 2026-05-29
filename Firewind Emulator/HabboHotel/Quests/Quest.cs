namespace Firewind.HabboHotel.Quests
{
    public class Quest
    {
        internal readonly uint Id;
        internal readonly string Category;
        internal readonly int Number;
        internal readonly QuestType GoalType;
        internal readonly uint GoalData;
        internal readonly string Name;
        internal readonly int Reward;
        internal readonly string DataBit;
        internal readonly int RewardType;
        internal readonly int TimeUnlock;

        public string ActionName
        {
            get
            {
                return QuestTypeUtillity.GetString(GoalType);
            }
        }
        
        public Quest(uint Id, string Category, int Number, QuestType GoalType, uint GoalData, string Name, int Reward,
            string DataBit, int RewardType = 3, int TimeUnlock = 0)
        {
            this.Id = Id;
            this.Category = Category;
            this.Number = Number;
            this.GoalType = GoalType;
            this.GoalData = GoalData;
            this.Name = Name;
            this.Reward = Reward;
            this.DataBit = DataBit;
            this.RewardType = RewardType;
            this.TimeUnlock = TimeUnlock;
        }

        public bool IsCompleted(int UserProgress)
        {
            switch (GoalType)
            {
                default:
                    return (UserProgress >= GoalData);
                case QuestType.EXPLORE_FIND_ITEM:
                case QuestType.STAND_ON:
                case QuestType.GIVE_ITEM:
                    return (UserProgress >= 1);
            }
        }
    }
}
