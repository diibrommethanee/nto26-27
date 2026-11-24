using _Project.Develop.Data.Questing;
using _Project.Develop.Data.Questing.Quests;
using _Project.Develop.Data.Surfaces;
using _Project.Develop.Gameplay.Quests;
using _Project.Develop.Gameplay.Input;
using _Project.Develop.UI.Surface;
using PrimeTween;
using System;
using MessagePipe;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Project.Develop.UI
{
    public class QuestTrackerView : MonoBehaviour
    {
        private const string CompletedText = "Завершено";
        
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI currentStepText;

        private CanvasGroup _group;
        private SurfaceGraphic _cover;
        private SurfaceStyle _defaultCoverStyle;
        private InputController _input;
        private ISubscriber<ZoomModeChanged> _zoomMode;
        private IDisposable _zoomSubscription;
        private Tween _zoomFade;

        private ISubscriber<QuestStepCompletedEvent> _subscriber;
        private ISubscriber<StartQuestEvent> _startedSubscriber;
        private ISubscriber<QuestCompletedEvent> _completedSubscriber;
        private IQuestRegistry _registry;
        private IDisposable _subscription;
        private IDisposable _startedSubscription;
        private IDisposable _completedSubscription;
        
        private Quest _currentQuest;
        private string _currentStepId;
        private bool _isCompleted;

        [Inject]
        public void Construct(ISubscriber<QuestStepCompletedEvent> subscriber,
            ISubscriber<StartQuestEvent> startedSubscriber,
            ISubscriber<QuestCompletedEvent> completedSubscriber,
            IQuestRegistry registry, InputController input, ISubscriber<ZoomModeChanged> zoomMode)
        {
            _subscriber = subscriber;
            _startedSubscriber = startedSubscriber;
            _completedSubscriber = completedSubscriber;
            _registry = registry;
            _input = input;
            _zoomMode = zoomMode;
        }

        private void Start()
        {
            _group = GetComponent<CanvasGroup>();
            _cover = GetComponent<SurfaceGraphic>();
            _defaultCoverStyle = _cover.style;
            
            _group.alpha = 0;
            _group.blocksRaycasts = false;
            
            _subscription = _subscriber.Subscribe(OnStepUpdated);
            _startedSubscription = _startedSubscriber.Subscribe(OnQuestStarted);
            _completedSubscription = _completedSubscriber.Subscribe(OnQuestCompleted);
            _zoomSubscription = _zoomMode.Subscribe(OnZoomModeChanged);
        }

        private void OnQuestCompleted(QuestCompletedEvent evt)
        {
            if (_currentQuest != evt.Quest) return;
            
            if (_isCompleted) return;

            _isCompleted = true;
            _currentStepId = null;
            currentStepText.text = CompletedText;
        }
        
        private void OnQuestStarted(StartQuestEvent evt)
        {
            var quest = evt.Quest;
            if (quest == null)
            {
                Debug.LogWarning("[QuestTracker] StartQuestEvent contains a null quest.");
                return;
            }

            _currentQuest = quest;
            _cover.style = quest.surfaceStyle != null ? quest.surfaceStyle : _defaultCoverStyle;
            _currentStepId = null;
            _isCompleted = false;

            if (!_input.IsZoomMode && !_zoomFade.isAlive) ShowTracker();
            
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
                Debug.LogWarning($"[QuestTracker] Quest '{quest.name}' has no steps.");
            }
        }
        
        private void OnStepUpdated(QuestStepCompletedEvent evt)
        {
            if (_isCompleted && evt.Quest == _currentQuest)
                return;

            var quest = evt.Quest;
            if (quest == null || !_registry.Contains(quest))
            {
                Debug.LogWarning("[QuestTracker] Quest was not found in the registry.");
                return;
            }

            if (!_registry.TryGetStep(quest, evt.StepId, out var step))
            {
                Debug.LogWarning($"[QuestTracker] Step '{evt.StepId}' was not found in quest '{quest.name}'.");
                return;
            }

            _currentQuest = quest;
            _cover.style = quest.surfaceStyle != null ? quest.surfaceStyle : _defaultCoverStyle;
            _currentStepId = evt.StepId;
            _isCompleted = false;

            if (!_input.IsZoomMode && !_zoomFade.isAlive) ShowTracker();
            
            titleText.text = quest.questName;
            descriptionText.text = quest.description;
            currentStepText.text = step.title;
        }

        private void OnZoomModeChanged(ZoomModeChanged message)
        {
            _zoomFade.Stop();
            if (!_currentQuest) return;

            _group.interactable = _group.blocksRaycasts = false;
            var visible = !message.IsActive;
            _zoomFade = Tween.Alpha(_group, visible ? 1f : 0f, visible ? 0.28f : 0.18f,
                Ease.OutCubic, useUnscaledTime: true);
            if (visible)
                _zoomFade.OnComplete(this, view => view.ShowTracker());
        }

        private void ShowTracker()
        {
            _group.alpha = 1f;
            _group.interactable = _group.blocksRaycasts = true;
        }

        private void OnDestroy()
        {
            _zoomSubscription?.Dispose();
            _zoomFade.Stop();
            _subscription?.Dispose();
            _startedSubscription?.Dispose();
            _completedSubscription?.Dispose();
            _subscription = null;
            _startedSubscription = null;
            _completedSubscription = null;
        }
    }
}
