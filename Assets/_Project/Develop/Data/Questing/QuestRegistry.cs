using System.Collections.Generic;
using _Project.Develop.Data.Questing.Quests;
using UnityEngine;

namespace _Project.Develop.Data.Questing
{
    public class QuestRegistry : IQuestRegistry
    {
        private readonly Dictionary<string, Quest> _quests;
        private readonly Dictionary<string, Dictionary<string, int>> _stepIndices;

        public QuestRegistry(QuestDatabase database)
        {
            _quests = new Dictionary<string, Quest>();
            _stepIndices = new Dictionary<string, Dictionary<string, int>>();

            if (database == null)
            {
                Debug.LogError("[QuestRegistry] пустая база данных квестов");
                return;
            }

            foreach (var quest in database.quests)
            {
                if (quest == null)
                {
                    Debug.LogError("[QuestRegistry] пустой QuestData в базе данных");
                    continue;
                }

                if (string.IsNullOrEmpty(quest.questId))
                {
                    Debug.LogError($"[QuestRegistry] у квеста '{quest.name}' пустой айди");
                    continue;
                }

                if (_quests.ContainsKey(quest.questId))
                {
                    Debug.LogError($"[QuestRegistry] дубликат айди '{quest.questId}' (вот тут '{quest.name}')");
                    continue;
                }

                _quests[quest.questId] = quest;
                _stepIndices[quest.questId] = BuildStepIndices(quest);
            }
        }

        private static Dictionary<string, int> BuildStepIndices(Quest quest)
        {
            var map = new Dictionary<string, int>(quest.steps.Count);

            for (int i = 0; i < quest.steps.Count; i++)
            {
                var step = quest.steps[i];

                if (step == null)
                {
                    Debug.LogError($"[QuestRegistry] пустой шаг квеста на {i} в квесте '{quest.questId}'");
                    continue;
                }

                if (string.IsNullOrEmpty(step.id))
                {
                    Debug.LogError($"[QuestRegistry] пустой step id {i} в квесте '{quest.questId}'");
                    continue;
                }

                if (map.ContainsKey(step.id))
                {
                    Debug.LogError($"[QuestRegistry] дубликат step id '{step.id}' в квесте '{quest.questId}'");
                    continue;
                }

                map[step.id] = i;
            }

            return map;
        }

        public bool TryGetQuest(string questId, out Quest quest)
            => _quests.TryGetValue(questId, out quest);

        public bool TryGetStep(string questId, string stepId, out QuestStep step)
        {
            step = null;
            if (string.IsNullOrEmpty(stepId)) return false;
            if (!_quests.TryGetValue(questId, out var quest)) return false;
            if (!_stepIndices[questId].TryGetValue(stepId, out var index)) return false;
            step = quest.steps[index];
            return true;
        }

        public bool TryGetStepIndex(string questId, string stepId, out int index)
        {
            index = -1;
            if (string.IsNullOrEmpty(stepId)) return false;
            if (!_stepIndices.TryGetValue(questId, out var map)) return false;
            return map.TryGetValue(stepId, out index);
        }

        public bool TryGetNextStep(string questId, string stepId, out QuestStep nextStep)
        {
            nextStep = null;
            if (!TryGetStepIndex(questId, stepId, out var index)) return false;
            if (!_quests.TryGetValue(questId, out var quest)) return false;

            var next = index + 1;
            if (next >= quest.steps.Count) return false;

            nextStep = quest.steps[next];
            return nextStep != null;
        }

        public IReadOnlyCollection<Quest> AllQuests => _quests.Values;
    }
}