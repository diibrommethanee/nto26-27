using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Develop.Data.Quests;
using _Project.Develop.Gameplay.Events;
using MessagePipe;
using VContainer.Unity;
using NaughtyAttributes;
using UnityEngine;

namespace _Project.Develop.Gameplay.Quests
{
    public class QuestService : IInitializable, IDisposable
    {
        private readonly List<QuestProgress> _activeQuests = new();
        private readonly List<QuestProgress> _completedQuests = new();

        private readonly List<IDisposable> _subscriptions = new();

        private readonly IPublisher<QuestStepCompletedEvent> _stepCompletedPublisher;
        private readonly IPublisher<QuestCompletedEvent> _questCompletedPublisher;

        public IReadOnlyList<QuestProgress> ActiveQuests => _activeQuests;
        public IReadOnlyList<QuestProgress> CompletedQuests => _completedQuests;

        public QuestService(
            ISubscriber<NpcInteractedEvent> npcSubscriber,
            ISubscriber<StartQuestEvent> startQuestSubscriber,
            IPublisher<QuestStepCompletedEvent> stepCompletedPublisher,
            IPublisher<QuestCompletedEvent> questCompletedPublisher)
        {
            _stepCompletedPublisher = stepCompletedPublisher;
            _questCompletedPublisher = questCompletedPublisher;

            _subscriptions.Add(npcSubscriber.Subscribe(OnNpcInteracted));
            _subscriptions.Add(startQuestSubscriber.Subscribe(OnStartQuest));
        }

        public void OnStartQuest(StartQuestEvent evt)
        {
            if (StartQuest(evt.QuestData)) Debug.Log("Quest started");
            else  Debug.Log("Quest not started");
        }
        
        public bool StartQuest(QuestData data)
        {
            if (data == null)
                return false;

            if (_activeQuests.Any(x => x.Data.questId == data.questId))
                return false;

            var progress = new QuestProgress(data);

            if (progress.IsCompleted)
            {
                _completedQuests.Add(progress);
                _questCompletedPublisher.Publish(new QuestCompletedEvent(data.questId));
                return true;
            }

            _activeQuests.Add(progress);
            return true;
        }

        public void RemoveQuest(string questId)
        {
            _activeQuests.RemoveAll(x => x.Data.questId == questId);
            Debug.Log("quest removed");
        }

        private void OnNpcInteracted(NpcInteractedEvent evt)
        {
            UpdateProgress(QuestEventType.NpcInteracted, evt.NpcId, 1);
        }

        private void UpdateProgress(QuestEventType eventType, string targetId, int amount)
        {
            if (amount <= 0)
                return;

            for (int i = _activeQuests.Count - 1; i >= 0; i--)
            {
                var quest = _activeQuests[i];
                var step = quest.CurrentStep;

                if (step == null)
                    continue;

                if (step.Data.eventType != eventType)
                    continue;

                if (step.Data.targetId != targetId)
                    continue;

                step.AddProgress(amount);

                if (!step.IsCompleted)
                    continue;

                _stepCompletedPublisher.Publish(
                    new QuestStepCompletedEvent(quest.Data.questId, step.Data.id));

                quest.AdvanceStep();

                if (quest.IsCompleted)
                    CompleteQuest(quest);
            }
            Debug.Log("updated quest progress");
        }

        private void CompleteQuest(QuestProgress quest)
        {
            _activeQuests.Remove(quest);
            _completedQuests.Add(quest);

            _questCompletedPublisher.Publish(
                new QuestCompletedEvent(quest.Data.questId));
            
            Debug.Log("completed quest");
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();

            _subscriptions.Clear();
        }

        public void Initialize()
        {
            Debug.Log("initializing quest service");
        }
    }
}