using _Project.Develop.Gameplay.Npc;
using _Project.Develop.Data.Questing.Rewards;

namespace _Project.Develop.Gameplay.Quests
{
    public class QuestRewards
    {
        private readonly NpcStates _states;

        public QuestRewards(NpcStates states) => _states = states;

        internal void Apply(QuestReward reward)
        {
            if (reward.dialogueNpc != null && reward.dialogue != null)
                _states[reward.dialogueNpc].Dialogue = reward.dialogue;

            if (reward.moodNpc != null && reward.moodIncrease > 0)
                _states[reward.moodNpc].Mood += reward.moodIncrease;
        }
    }
}
