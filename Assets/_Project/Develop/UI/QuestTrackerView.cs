using System;
using MessagePipe;
using TMPro;
using UnityEngine;
using VContainer;
using _Project.Develop.Data.Quests;
using _Project.Develop.Gameplay.Events;

namespace _Project.Develop.UI
{
    public sealed class QuestTrackerView : MonoBehaviour
    {
        private const string CompletedText = "Завершено";
        
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI currentStepText;

        private ISubscriber<QuestStepCompletedEvent> _subscriber;
        private ISubscriber<StartQuestEvent> _startedSubscriber;
        private ISubscriber<QuestCompletedEvent> _completedSubscriber;
        private IQuestRegistry _registry;
        private IDisposable _subscription;
        private IDisposable _startedSubscription;
        private IDisposable _completedSubscription;
        
        private string _currentQuestId;
        private string _currentStepId;
        private bool _isCompleted;

        [Inject]
        public void Construct(ISubscriber<QuestStepCompletedEvent> subscriber, ISubscriber<StartQuestEvent> startedSubscriber, ISubscriber<QuestCompletedEvent> completedSubscriber, IQuestRegistry registry)
        {
            _subscriber = subscriber;
            _startedSubscriber = startedSubscriber;
            _completedSubscriber = completedSubscriber;
            _registry = registry;
        }

        private void Start()
        {
            panelRoot.SetActive(false);
            _subscription = _subscriber.Subscribe(OnStepUpdated);
            _startedSubscription = _startedSubscriber.Subscribe(OnQuestStarted);
            _completedSubscription = _completedSubscriber.Subscribe(OnQuestCompleted);
        }

        private void OnQuestCompleted(QuestCompletedEvent evt)
        {
            if (_currentQuestId != evt.QuestId) return;
            
            if (_isCompleted) return;

            _isCompleted = true;
            _currentStepId = null;
            currentStepText.text = CompletedText;
        }
        
        private void OnQuestStarted(StartQuestEvent evt)
        {
            var quest = evt.QuestData;
            if (quest == null)
            {
                Debug.LogWarning("[QuestTracker] StartQuestEvent пришел с пустым квестом");
                return;
            }

            _currentQuestId = quest.questId;
            _currentStepId = null;
            _isCompleted = false;

            panelRoot.SetActive(true);
            titleText.text = quest.questName;
            descriptionText.text = quest.description;

            if (quest.steps != null && quest.steps.Count > 0 && quest.steps[0] != null)
            {
                var firstStep = quest.steps[0];
                _currentStepId = firstStep.id;
                currentStepText.text = firstStep.title;
            }
            else
            {
                currentStepText.text = string.Empty;
                Debug.LogWarning($"[QuestTracker] У квеста '{quest.questId}' нет шагов");
            }
        }
        
        private void OnStepUpdated(QuestStepCompletedEvent evt)
        {
            if (_isCompleted && evt.QuestId == _currentQuestId)
                return;

            if (!_registry.TryGetQuest(evt.QuestId, out var quest))
            {
                Debug.LogWarning($"[QuestTracker] Квест '{evt.QuestId}' не найден в реестре");
                return;
            }

            if (!_registry.TryGetStep(evt.QuestId, evt.StepId, out var step))
            {
                Debug.LogWarning($"[QuestTracker] шага '{evt.StepId}' не найдено в квесте '{evt.QuestId}'");
                return;
            }

            _currentQuestId = evt.QuestId;
            _currentStepId = evt.StepId;
            _isCompleted = false;

            panelRoot.SetActive(true);
            titleText.text = quest.questName;
            descriptionText.text = quest.description;
            currentStepText.text = step.title;
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
            _startedSubscription?.Dispose();
            _completedSubscription?.Dispose();
            _subscription = null;
            _startedSubscription = null;
            _completedSubscription = null;
        }
    }
}