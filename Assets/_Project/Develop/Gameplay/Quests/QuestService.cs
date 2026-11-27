using _Project.Develop.Data.Questing.Quests;
using _Project.Develop.Data.Evidence;
using _Project.Develop.Gameplay.Evidence;
using _Project.Develop.Gameplay.Npc;
using System;
using _Project.Develop.Data;
using System.Collections.Generic;
using System.Linq;
using MessagePipe;
using VContainer.Unity;
using UnityEngine;

namespace _Project.Develop.Gameplay.Quests
{
    public class QuestService : ISavable, IAfterRestore, IInitializable, IDisposable
    {
        private readonly List<QuestProgress> _activeQuests = new();
        private readonly List<QuestProgress> _completedQuests = new();

        private readonly List<IDisposable> _subscriptions = new();

        private readonly QuestRewards _rewards;
        private readonly EvidenceService _evidence;
        private readonly IPublisher<QuestStepCompletedEvent> _stepCompletedPublisher;
        private readonly IPublisher<QuestCompletedEvent> _questCompletedPublisher;

        public IReadOnlyList<QuestProgress> ActiveQuests => _activeQuests;
        public IReadOnlyList<QuestProgress> CompletedQuests => _completedQuests;

        public QuestProgress TrackedQuest { get; private set; }
        public event Action<QuestProgress> TrackedQuestChanged;

        private void Track(QuestProgress progress)
        {
            TrackedQuest = progress;
            TrackedQuestChanged?.Invoke(progress);
        }

        private string _restoredTrackedKey;

        [Save] private string TrackedQuestKey
        {
            get => TrackedQuest == null ? null : CheckpointCatalog.Instance.Key(TrackedQuest.Data);
            set => _restoredTrackedKey = value;
        }

        public void AfterRestore()
        {
            var tracked = _restoredTrackedKey == null ? null : _activeQuests.Concat(_completedQuests)
                .FirstOrDefault(quest => CheckpointCatalog.Instance.Key(quest.Data) == _restoredTrackedKey);
            _restoredTrackedKey = null;
            Track(tracked ?? _activeQuests.LastOrDefault() ?? _completedQuests.LastOrDefault());
        }

        [Save] private Dictionary<string, int[]> SavedQuests
        {
            get => _activeQuests.Concat(_completedQuests).ToDictionary(
                quest => CheckpointCatalog.Instance.Key(quest.Data),
                quest => quest.Steps.Select(step => step.CurrentAmount).Prepend(quest.CurrentStepIndex).ToArray());
            set
            {
                var active = new List<QuestProgress>();
                var completed = new List<QuestProgress>();
                foreach (var pair in value)
                {
                    var progress = new QuestProgress(CheckpointCatalog.Instance.Resolve<Quest>(pair.Key));
                    progress.Restore(pair.Value);
                    (progress.IsCompleted ? completed : active).Add(progress);
                }

                _activeQuests.Clear();
                _activeQuests.AddRange(active);
                _completedQuests.Clear();
                _completedQuests.AddRange(completed);
            }
        }

        public QuestService(ISubscriber<NpcInteractedEvent> npcSubscriber,
            ISubscriber<NpcTalkedEvent> talkedSubscriber,
            ISubscriber<StartQuestEvent> startQuestSubscriber,
            IPublisher<QuestStepCompletedEvent> stepCompletedPublisher,
            IPublisher<QuestCompletedEvent> questCompletedPublisher,
            EvidenceService evidence, QuestRewards rewards)
        {
            _rewards = rewards;
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
            Track(progress);
            return true;
        }

        public void RemoveQuest(Quest quest)
        {
            _activeQuests.RemoveAll(x => x.Data == quest);
            if (TrackedQuest?.Data == quest) Track(_activeQuests.LastOrDefault() ?? _completedQuests.LastOrDefault());
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
                else
                    Track(quest);
            }
        }

        private void CompleteQuest(QuestProgress quest)
        {
            _activeQuests.Remove(quest);
            _completedQuests.Add(quest);

            _rewards.Apply(quest.Data.reward);
            
            Track(quest);
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
