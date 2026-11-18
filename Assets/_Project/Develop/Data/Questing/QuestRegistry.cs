using System.Collections.Generic;
using _Project.Develop.Data.Questing.Quests;
using UnityEngine;

namespace _Project.Develop.Data.Questing
{
    public class QuestRegistry : IQuestRegistry
    {
        private readonly HashSet<Quest> _quests;
        private readonly Dictionary<Quest, Dictionary<string, int>> _stepIndices;

        public QuestRegistry(QuestDatabase database)
        {
            _quests = new HashSet<Quest>();
            _stepIndices = new Dictionary<Quest, Dictionary<string, int>>();

            if (database == null || database.quests == null)
            {
                Debug.LogError("[QuestRegistry] Quest database is missing.");
                return;
            }

            foreach (var quest in database.quests)
            {
                if (quest == null)
                {
                    Debug.LogError("[QuestRegistry] Null quest in the database.");
                    continue;
                }

                if (!_quests.Add(quest))
                {
                    Debug.LogError($"[QuestRegistry] Duplicate quest reference: '{quest.name}'.");
                    continue;
                }

                _stepIndices[quest] = BuildStepIndices(quest);
            }
        }

        private static Dictionary<string, int> BuildStepIndices(Quest quest)
        {
            var map = new Dictionary<string, int>(quest.steps?.Count ?? 0);

            if (quest.steps == null)
                return map;

            for (int i = 0; i < quest.steps.Count; i++)
            {
                var step = quest.steps[i];

                if (step == null)
                {
                    Debug.LogError($"[QuestRegistry] Null step at index {i} in quest '{quest.name}'.");
                    continue;
                }

                if (string.IsNullOrEmpty(step.id))
                {
                    Debug.LogError($"[QuestRegistry] Empty step ID at index {i} in quest '{quest.name}'.");
                    continue;
                }

                if (map.ContainsKey(step.id))
                {
                    Debug.LogError($"[QuestRegistry] Duplicate step ID '{step.id}' in quest '{quest.name}'.");
                    continue;
                }

                map[step.id] = i;
            }

            return map;
        }

        public bool Contains(Quest quest)
            => quest != null && _quests.Contains(quest);

        public bool TryGetStep(Quest quest, string stepId, out QuestStep step)
        {
            step = null;
            if (string.IsNullOrEmpty(stepId)) return false;
            if (!Contains(quest)) return false;
            if (!_stepIndices[quest].TryGetValue(stepId, out var index)) return false;
            step = quest.steps[index];
            return true;
        }

        public bool TryGetStepIndex(Quest quest, string stepId, out int index)
        {
            index = -1;
            if (string.IsNullOrEmpty(stepId)) return false;
            if (quest == null || !_stepIndices.TryGetValue(quest, out var map)) return false;
            return map.TryGetValue(stepId, out index);
        }

        public bool TryGetNextStep(Quest quest, string stepId, out QuestStep nextStep)
        {
            nextStep = null;
            if (!TryGetStepIndex(quest, stepId, out var index)) return false;

            var next = index + 1;
            if (next >= quest.steps.Count) return false;

            nextStep = quest.steps[next];
            return nextStep != null;
        }

        public IReadOnlyCollection<Quest> AllQuests => _quests;
    }
}
