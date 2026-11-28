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

        public IReadOnlyList<QuestProgress> ActiveQuests { get; }
        public IReadOnlyList<QuestProgress> CompletedQuests { get; }

        private IEnumerable<QuestProgress> AllQuests => _activeQuests.Concat(_completedQuests);
        private QuestProgress DefaultTrackedQuest => _activeQuests.LastOrDefault() ?? _completedQuests.LastOrDefault();

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
            var tracked = _restoredTrackedKey == null ? null : AllQuests
                .FirstOrDefault(quest => CheckpointCatalog.Instance.Key(quest.Data) == _restoredTrackedKey);
            _restoredTrackedKey = null;
            Track(tracked ?? DefaultTrackedQuest);
        }

        [Save] private Dictionary<string, int[]> SavedQuests
        {
            get => AllQuests.ToDictionary(
                quest => CheckpointCatalog.Instance.Key(quest.Data),
                quest => quest.Capture());
            set
            {
                var active = new List<QuestProgress>();
                var completed = new List<QuestProgress>();
                foreach (var pair in value)
                {
                    var quest = CheckpointCatalog.Instance.Resolve<Quest>(pair.Key);
                    var progress = QuestProgress.FromCheckpoint(quest, pair.Value);
                    if (progress.IsCompleted)
                        completed.Add(progress);
                    else
                        active.Add(progress);
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
            ActiveQuests = _activeQuests.AsReadOnly();
            CompletedQuests = _completedQuests.AsReadOnly();
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

            if (AllQuests.Any(progress => progress.Data == data))
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
            if (TrackedQuest?.Data == quest) Track(DefaultTrackedQuest);
            Debug.Log("Quest removed");
        }

        private void OnNpcInteracted(NpcInteractedEvent evt)
        {
            UpdateProgress(QuestEventType.NpcInteracted, evt.Npc);
        }

        private void OnNpcTalked(NpcTalkedEvent evt)
        {
            UpdateProgress(QuestEventType.NpcTalked, evt.Npc);
        }

        private void OnClueCollected(Clue clue, Vector3? sourceWorldPosition, Vector2? boardPosition)
        {
            UpdateProgress(QuestEventType.ClueCollected, clue);
        }

        private void OnHypothesisUnlocked(Hypothesis hypothesis)
        {
            UpdateProgress(QuestEventType.HypothesisCollected, hypothesis);
        }

        private void OnHypothesisPassed(Hypothesis hypothesis)
        {
            UpdateProgress(QuestEventType.HypothesisPassed, hypothesis);
        }

        private void UpdateProgress(QuestEventType eventType, QuestTarget target)
        {
            if (target == null)
                return;

            for (var i = _activeQuests.Count - 1; i >= 0; i--)
            {
                var quest = _activeQuests[i];
                var step = quest.CurrentStep;

                if (step == null || !step.TryComplete(eventType, target))
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
