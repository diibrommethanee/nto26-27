using _Project.Develop.Data.Questing;
using _Project.Develop.Data.Questing.Quests;
using _Project.Develop.Data.Surfaces;
using _Project.Develop.Gameplay.Quests;
using _Project.Develop.Gameplay.Input;
using _Project.Develop.UI.Surface;
using PrimeTween;
using System;
using System.Linq;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;

namespace _Project.Develop.UI
{
    public class QuestTrackerView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const string CompletedText = "Завершено";
        
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI currentStepText;

        private GameObject _descriptionSection;
        private LayoutElement _descriptionLayout;
        private CanvasGroup _descriptionFade;
        private VerticalLayoutGroup _descriptionContent;
        private Tween _descriptionTween;
        private bool _descriptionOpen;
        private bool _isHovered;
        private CanvasGroup _group;
        private SurfaceGraphic _cover;
        private (SurfaceGraphic graphic, SurfaceStyle style, Color tint)[] _defaultSurfaces;
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
            _cover.ShowTextureAndPattern = false;
            _defaultSurfaces = GetComponentsInChildren<SurfaceGraphic>(true)
                .Select(graphic => (graphic, graphic.style, graphic.color))
                .ToArray();
            _cover.raycastTarget = true;
            _descriptionSection = descriptionText.transform.parent.gameObject;
            _descriptionSection.transform.SetAsLastSibling();
            _descriptionContent = _descriptionSection.GetComponent<VerticalLayoutGroup>();
            _descriptionLayout = _descriptionSection.AddComponent<LayoutElement>();
            _descriptionLayout.layoutPriority = 2;
            _descriptionFade = _descriptionSection.AddComponent<CanvasGroup>();
            _descriptionFade.alpha = 0f;
            _descriptionSection.AddComponent<RectMask2D>();
            _descriptionSection.SetActive(false);
            
            _group.alpha = 0;
            _group.blocksRaycasts = false;
            
            _subscription = _subscriber.Subscribe(OnStepUpdated);
            _startedSubscription = _startedSubscriber.Subscribe(OnQuestStarted);
            _completedSubscription = _completedSubscriber.Subscribe(OnQuestCompleted);
            _zoomSubscription = _zoomMode.Subscribe(OnZoomModeChanged);
        }

        private void ApplySurfaceStyle(SurfaceStyle style)
        {
            foreach (var surface in _defaultSurfaces)
            {
                var source = SurfaceGraphic.FillSource.Fill;
                if (style != null)
                {
                    var section = surface.graphic.transform;
                    if (section == titleText.transform.parent) source = SurfaceGraphic.FillSource.Edge;
                    else if (section == currentStepText.transform.parent) source = SurfaceGraphic.FillSource.Accent;
                    else if (section == descriptionText.transform.parent) source = SurfaceGraphic.FillSource.PatternTint;
                }
                surface.graphic.FillFrom = source;
                surface.graphic.style = style != null ? style : surface.style;
                surface.graphic.color = style != null ? Color.white : surface.tint;
            }
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
            ApplySurfaceStyle(quest.surfaceStyle);
            _currentStepId = null;
            _isCompleted = false;

            if (!_input.IsZoomMode && !_zoomFade.isAlive) ShowTracker();
            
            titleText.text = quest.questName;
            descriptionText.text = quest.description;
            UpdateDescriptionVisibility();

            var firstStep = quest.steps.FirstOrDefault(step => step.eventType != QuestEventType.Empty);
            _currentStepId = firstStep?.id;
            _isCompleted = firstStep == null;
            currentStepText.text = firstStep?.title ?? CompletedText;
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
            ApplySurfaceStyle(quest.surfaceStyle);
            var nextStep = quest.steps
                .SkipWhile(candidate => candidate != step)
                .Skip(1)
                .FirstOrDefault(candidate => candidate.eventType != QuestEventType.Empty);
            _currentStepId = nextStep?.id;
            _isCompleted = false;

            if (!_input.IsZoomMode && !_zoomFade.isAlive) ShowTracker();
            
            titleText.text = quest.questName;
            descriptionText.text = quest.description;
            UpdateDescriptionVisibility();
            currentStepText.text = nextStep?.title ?? CompletedText;
        }

        private void OnZoomModeChanged(ZoomModeChanged message)
        {
            _isHovered = false;
            UpdateDescriptionVisibility();
            _zoomFade.Stop();
            if (!_currentQuest) return;

            _group.interactable = _group.blocksRaycasts = false;
            var visible = !message.IsActive;
            _zoomFade = Tween.Alpha(_group, visible ? 1f : 0f, visible ? 0.28f : 0.18f,
                Ease.OutCubic, useUnscaledTime: true);
            if (visible)
                _zoomFade.OnComplete(this, view => view.ShowTracker());
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            UpdateDescriptionVisibility();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            UpdateDescriptionVisibility();
        }

        private void UpdateDescriptionVisibility()
        {
            bool open = _isHovered && !string.IsNullOrWhiteSpace(descriptionText.text);
            if (_descriptionOpen == open) return;
            _descriptionOpen = open;
            _descriptionTween.Stop();

            var rect = (RectTransform)_descriptionSection.transform;
            float startHeight = _descriptionSection.activeSelf ? rect.rect.height : 0f;
            float startAlpha = _descriptionFade.alpha;
            _descriptionSection.SetActive(true);
            _descriptionLayout.minHeight = 0f;
            _descriptionLayout.preferredHeight = startHeight;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            float endHeight = open ? _descriptionContent.preferredHeight : 0f;

            _descriptionTween = Tween.Custom(this, 0f, 1f, open ? .28f : .18f,
                (view, t) =>
                {
                    view._descriptionLayout.preferredHeight = Mathf.Lerp(startHeight, endHeight, t);
                    view._descriptionFade.alpha = Mathf.Lerp(startAlpha, open ? 1f : 0f, t * t);
                }, open ? Ease.OutCubic : Ease.InOutSine, useUnscaledTime: true);
            _descriptionTween.OnComplete(this, view =>
            {
                view._descriptionSection.SetActive(view._descriptionOpen);
                view._descriptionLayout.minHeight = -1f;
                view._descriptionLayout.preferredHeight = -1f;
            });
        }

        private void ShowTracker()
        {
            _group.alpha = 1f;
            _group.interactable = _group.blocksRaycasts = true;
        }

        private void OnDestroy()
        {
            _descriptionTween.Stop();
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
