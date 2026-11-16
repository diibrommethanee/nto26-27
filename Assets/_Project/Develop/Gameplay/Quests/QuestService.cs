using _Project.Develop.Data.Questing.Quests;
using _Project.Develop.Data.Npc;
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

        private readonly IPublisher<QuestStepCompletedEvent> _stepCompletedPublisher;
        private readonly IPublisher<QuestCompletedEvent> _questCompletedPublisher;

        public IReadOnlyList<QuestProgress> ActiveQuests => _activeQuests;
        public IReadOnlyList<QuestProgress> CompletedQuests => _completedQuests;

        public QuestService(ISubscriber<NpcInteractedEvent> npcSubscriber,
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
            if (StartQuest(evt.Quest)) Debug.Log("Quest started");
            else  Debug.Log("Quest not started");
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
                _completedQuests.Add(progress);
                _questCompletedPublisher.Publish(new QuestCompletedEvent(data));
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

        private void UpdateProgress(QuestEventType eventType, NpcData targetNpc, int amount)
        {
            if (amount <= 0 || targetNpc == null)
                return;

            for (var i = _activeQuests.Count - 1; i >= 0; i--)
            {
                var quest = _activeQuests[i];
                var step = quest.CurrentStep;

                if (step == null)
                    continue;

                if (step.Data.eventType != eventType)
                    continue;

                if (step.Data.target != targetNpc)
                    continue;

                step.AddProgress(amount);
                Debug.Log("Updated quest progress");

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

            if (quest.Data.rewards != null)
            {
                foreach (var reward in quest.Data.rewards)
                {
                    reward?.Grant();
                }
            }
            
            _questCompletedPublisher.Publish(
                new QuestCompletedEvent(quest.Data));
            
            Debug.Log("Completed quest");
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();

            _subscriptions.Clear();
        }

        public void Initialize()
        {
            Debug.Log("Initializing quest service");
        }
    }
}
