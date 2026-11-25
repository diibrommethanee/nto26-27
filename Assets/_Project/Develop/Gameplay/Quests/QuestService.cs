using _Project.Develop.Data.Questing.Quests;
using _Project.Develop.Data.Evidence;
using _Project.Develop.Gameplay.Evidence;
using _Project.Develop.Gameplay.Npc;
using System;
using System.Collections.Generic;
using System.Linq;
using MessagePipe;
using VContainer.Unity;
using UnityEngine;

namespace _Project.Develop.Gameplay.Quests
{
    public class QuestService : IInitializable, IDisposable
    {
        private readonly List<QuestProgress> _activeQuests = new();
        private readonly List<QuestProgress> _completedQuests = new();

        private readonly List<IDisposable> _subscriptions = new();

        private readonly EvidenceService _evidence;
        private readonly IPublisher<QuestStepCompletedEvent> _stepCompletedPublisher;
        private readonly IPublisher<QuestCompletedEvent> _questCompletedPublisher;

        public IReadOnlyList<QuestProgress> ActiveQuests => _activeQuests;
        public IReadOnlyList<QuestProgress> CompletedQuests => _completedQuests;

        public QuestService(ISubscriber<NpcInteractedEvent> npcSubscriber,
            ISubscriber<NpcTalkedEvent> talkedSubscriber,
            ISubscriber<StartQuestEvent> startQuestSubscriber,
            IPublisher<QuestStepCompletedEvent> stepCompletedPublisher,
            IPublisher<QuestCompletedEvent> questCompletedPublisher,
            EvidenceService evidence)
        {
            _evidence = evidence;
            _stepCompletedPublisher = stepCompletedPublisher;
            _questCompletedPublisher = questCompletedPublisher;

            _subscriptions.Add(npcSubscriber.Subscribe(OnNpcInteracted));
            _subscriptions.Add(talkedSubscriber.Subscribe(OnNpcTalked));
            _subscriptions.Add(startQuestSubscriber.Subscribe(OnStartQuest));
        }

        public void OnStartQuest(StartQuestEvent evt)
        {
            if (!StartQuest(evt.Quest)) Debug.Log("Quest not started");
        }
        
        public bool StartQuest(Quest data)
        {
            if (data == null)
                return false;

            if (_activeQuests.Any(x => x.Data == data) ||
                _completedQuests.Any(x => x.Data == data))
                return false;

            var progress = new QuestProgress(data);

            if (progress.IsCompleted)
            {
                CompleteQuest(progress);
                return true;
            }

            _activeQuests.Add(progress);
            return true;
        }

        public void RemoveQuest(Quest quest)
        {
            _activeQuests.RemoveAll(x => x.Data == quest);
            Debug.Log("Quest removed");
        }

        private void OnNpcInteracted(NpcInteractedEvent evt)
        {
            UpdateProgress(QuestEventType.NpcInteracted, evt.Npc, 1);
        }

        private void OnNpcTalked(NpcTalkedEvent evt)
        {
            UpdateProgress(QuestEventType.NpcTalked, evt.Npc, 1);
        }

        private void OnClueCollected(Clue clue, Vector3? sourceWorldPosition, Vector2? boardPosition)
        {
            UpdateProgress(QuestEventType.ClueCollected, clue, 1);
        }

        private void OnHypothesisUnlocked(Hypothesis hypothesis)
        {
            UpdateProgress(QuestEventType.HypothesisCollected, hypothesis, 1);
        }

        private void OnHypothesisPassed(Hypothesis hypothesis)
        {
            UpdateProgress(QuestEventType.HypothesisPassed, hypothesis, 1);
        }

        private void UpdateProgress(QuestEventType eventType, QuestTarget target, int amount)
        {
            if (amount <= 0 || target == null)
                return;

            for (var i = _activeQuests.Count - 1; i >= 0; i--)
            {
                var quest = _activeQuests[i];
                var step = quest.CurrentStep;

                if (step == null)
                    continue;

                if (step.Data.eventType != eventType)
                    continue;

                if (step.Data.target != target)
                    continue;

                step.AddProgress(amount);

                if (!step.IsCompleted)
                    continue;

                _stepCompletedPublisher.Publish(
                    new QuestStepCompletedEvent(quest.Data, step.Data.id));

                quest.AdvanceStep();

                if (quest.IsCompleted)
                    CompleteQuest(quest);
            }
        }

        private void CompleteQuest(QuestProgress quest)
        {
            _activeQuests.Remove(quest);
            _completedQuests.Add(quest);

            var reward = quest.Data.reward;
            if (reward.dialogueNpc != null && reward.dialogue != null)
                reward.dialogueNpc.state.dialogue = reward.dialogue;

            if (reward.moodNpc != null && reward.moodIncrease > 0)
                reward.moodNpc.state.mood += reward.moodIncrease;
            
            _questCompletedPublisher.Publish(
                new QuestCompletedEvent(quest.Data));
        }

        public void Dispose()
        {
            _evidence.ClueCollected -= OnClueCollected;
            _evidence.HypothesisUnlocked -= OnHypothesisUnlocked;
            _evidence.HypothesisPassed -= OnHypothesisPassed;

            foreach (var subscription in _subscriptions)
                subscription.Dispose();

            _subscriptions.Clear();
        }

        public void Initialize()
        {
            _evidence.ClueCollected += OnClueCollected;
            _evidence.HypothesisUnlocked += OnHypothesisUnlocked;
            _evidence.HypothesisPassed += OnHypothesisPassed;
        }
    }
}
