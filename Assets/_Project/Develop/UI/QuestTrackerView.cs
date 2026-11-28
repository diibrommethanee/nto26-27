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

        private QuestService _quests;
        private Quest _currentQuest;

        [Inject]
        public void Construct(QuestService quests, InputController input, ISubscriber<ZoomModeChanged> zoomMode)
        {
            _quests = quests;
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
            
            _quests.TrackedQuestChanged += ShowQuest;
            ShowQuest(_quests.TrackedQuest);
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

        private void ShowQuest(QuestProgress progress)
        {
            _currentQuest = progress?.Data;
            if (_currentQuest == null)
            {
                _zoomFade.Stop();
                _group.alpha = 0f;
                _group.SetInteractive(false);
                return;
            }

            ApplySurfaceStyle(_currentQuest.surfaceStyle);
            titleText.text = _currentQuest.questName;
            descriptionText.text = _currentQuest.description;
            currentStepText.text = progress.CurrentStep?.Data.title ?? CompletedText;
            UpdateDescriptionVisibility();
            if (!_input.IsZoomMode && !_zoomFade.isAlive) ShowTracker();
        }

        private void OnZoomModeChanged(ZoomModeChanged message)
        {
            _isHovered = false;
            UpdateDescriptionVisibility();
            _zoomFade.Stop();
            if (!_currentQuest) return;

            _group.SetInteractive(false);
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
            _group.SetInteractive(true);
        }

        private void OnDestroy()
        {
            _descriptionTween.Stop();
            _zoomSubscription?.Dispose();
            _zoomFade.Stop();
            if (_quests != null) _quests.TrackedQuestChanged -= ShowQuest;
        }
    }
}
