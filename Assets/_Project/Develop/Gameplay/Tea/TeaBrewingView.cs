using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Develop.Gameplay.Tea
{
    public enum TeaBrewingState
    {
        Idle,
        ReadyToPour,
        Pouring,
        WaitingIngredient,
        Completed
    }

    public enum TeaIngredientType
    {
        None,
        Mint,
        LemonBalm,
        Thyme
    }

    [Serializable]
    public class TeaIngredientOption
    {
        public TeaIngredientType type = TeaIngredientType.None;
        public Button button;
        public RectTransform visualRoot;
        public Image iconImage;
    }

    public class TeaBrewingView : MonoBehaviour
    {
        [Header("Flow")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button pourButton;

        [Header("Visuals")]
        [SerializeField] private RectTransform teapot;
        [SerializeField] private RectTransform cup;
        [SerializeField] private Image teaLiquidImage;
        [SerializeField] private CanvasGroup ingredientsGroup;
        [SerializeField] private List<TeaIngredientOption> ingredients = new();

        [Header("Timings")]
        [SerializeField] private float pourDuration = 1.35f;
        [SerializeField] private float ingredientMoveDuration = 0.45f;
        [SerializeField] private float ingredientDropDuration = 0.35f;

        [Header("Positions")]
        [SerializeField] private Vector2 teapotPourOffset = new(-180f, 140f);
        [SerializeField] private Vector3 teapotPourRotation = new(0f, 0f, -28f);
        [SerializeField] private Vector2 cupPourOffset = new(85f, 0f);

        [Header("Tea Colors")]
        [SerializeField] private Color baseTeaColor = new(0.78f, 0.58f, 0.35f, 0.95f);
        [SerializeField] private Color mintTeaColor = new(0.72f, 0.86f, 0.58f, 0.95f);
        [SerializeField] private Color lemonBalmTeaColor = new(0.93f, 0.86f, 0.55f, 0.95f);
        [SerializeField] private Color thymeTeaColor = new(0.8f, 0.74f, 0.56f, 0.95f);

        private readonly List<Tween> _idleTweens = new();
        private readonly List<TeaIngredientType> _selectedIngredients = new();
        private readonly HashSet<TeaIngredientType> _selectedSet = new();
        private Vector2 _teapotStartPos;
        private Vector3 _teapotStartRot;
        private Vector3 _teapotStartScale;
        private Vector2 _cupStartPos;
        private Sequence _activeSequence;
        private Tween _autoCompleteTween;
        private bool _initialized;

        public event Action<IReadOnlyList<TeaIngredientType>> Completed;

        public TeaBrewingState State { get; protected set; } = TeaBrewingState.Idle;
        public IReadOnlyList<TeaIngredientType> SelectedIngredients => _selectedIngredients;

        protected virtual void Awake()
        {
            EnsureInitialized();
            if (State == TeaBrewingState.Idle)
                HideImmediate();
        }

        protected virtual void OnDisable()
        {
            KillTweens();
        }

        public void Begin()
        {
            BeginBrewing();
        }

        protected void BeginBrewing()
        {
            EnsureInitialized();
            KillTweens();
            ResetToReadyState();

            State = TeaBrewingState.ReadyToPour;
            gameObject.SetActive(true);

            SetCanvasGroup(canvasGroup, 1f, true);
            SetCanvasGroup(ingredientsGroup, 0f, false);
            OnBrewingStarted();

            if (pourButton != null)
            {
                pourButton.gameObject.SetActive(true);
                pourButton.interactable = true;
            }

            if (teapot != null)
            {
                teapot.anchoredPosition = _teapotStartPos + new Vector2(0f, 40f);
                teapot.localScale = Vector3.zero;
                Tween.UIAnchoredPosition(teapot, _teapotStartPos, 0.45f, Ease.OutCubic);
                Tween.Scale(teapot, _teapotStartScale, 0.45f, Ease.OutBack);
            }

            if (cup != null)
            {
                cup.anchoredPosition = _cupStartPos + new Vector2(0f, -30f);
                Tween.UIAnchoredPosition(cup, _cupStartPos, 0.4f, Ease.OutCubic, startDelay: 0.05f);
            }

            StartIdleAnimations();
        }

        public virtual void HideImmediate()
        {
            EnsureInitialized();
            KillTweens();
            State = TeaBrewingState.Idle;
            SetCanvasGroup(canvasGroup, 0f, false);
            SetCanvasGroup(ingredientsGroup, 0f, false);
            OnBrewingHidden();
            gameObject.SetActive(false);
        }

        private void EnsureInitialized()
        {
            if (_initialized)
                return;

            CacheStartPose();
            BindButtons();
            _initialized = true;
        }

        private void CacheStartPose()
        {
            if (teapot != null)
            {
                _teapotStartPos = teapot.anchoredPosition;
                _teapotStartRot = teapot.localEulerAngles;
                _teapotStartScale = teapot.localScale;
            }

            if (cup != null)
                _cupStartPos = cup.anchoredPosition;
        }

        private void ResetToReadyState()
        {
            _selectedIngredients.Clear();
            _selectedSet.Clear();

            if (pourButton != null)
                pourButton.gameObject.SetActive(true);

            if (teapot != null)
            {
                teapot.anchoredPosition = _teapotStartPos;
                teapot.localEulerAngles = _teapotStartRot;
                teapot.localScale = _teapotStartScale;
            }

            if (cup != null)
                cup.anchoredPosition = _cupStartPos;

            if (teaLiquidImage != null)
            {
                var c = baseTeaColor;
                c.a = 0f;
                teaLiquidImage.color = c;
            }

            foreach (var option in ingredients)
            {
                if (option == null)
                    continue;

                if (option.button != null)
                    option.button.interactable = true;

                if (option.visualRoot == null)
                    continue;

                option.visualRoot.gameObject.SetActive(true);
                option.visualRoot.localScale = Vector3.one;
                if (option.iconImage != null)
                {
                    var color = option.iconImage.color;
                    color.a = 1f;
                    option.iconImage.color = color;
                }

            }
        }

        private void BindButtons()
        {
            if (pourButton != null)
            {
                pourButton.onClick.RemoveListener(OnPourClicked);
                pourButton.onClick.AddListener(OnPourClicked);
            }

            for (int i = 0; i < ingredients.Count; i++)
            {
                int index = i;
                var option = ingredients[i];
                if (option?.button == null)
                    continue;

                option.button.onClick.RemoveAllListeners();
                option.button.onClick.AddListener(() => OnIngredientClicked(index));
            }

            BindAdditionalButtons();
        }

        private void StartIdleAnimations()
        {
            StopIdleAnimations();

            if (teapot != null)
                _idleTweens.Add(Tween.LocalPositionY(teapot, _teapotStartPos.y + 8f, 1.8f, Ease.InOutSine, -1, CycleMode.Yoyo));
        }

        private void StopIdleAnimations()
        {
            foreach (var tween in _idleTweens)
                tween.Stop();

            _idleTweens.Clear();
        }

        protected virtual void OnPourClicked()
        {
            if (State == TeaBrewingState.ReadyToPour)
                StartPour();
            else if (State == TeaBrewingState.WaitingIngredient && _selectedIngredients.Count > 0)
                CompleteBrewing();
        }

        public bool CanCompleteBrewing => State == TeaBrewingState.WaitingIngredient && _selectedIngredients.Count > 0;

        public virtual void TryCompleteBrewing()
        {
            if (CanCompleteBrewing)
                CompleteBrewing();
        }

        protected void ScheduleAutoComplete(float delay)
        {
            _autoCompleteTween.Stop();
            if (State != TeaBrewingState.WaitingIngredient)
                return;

            _autoCompleteTween = Tween.Delay(delay, () =>
            {
                if (State != TeaBrewingState.WaitingIngredient)
                    return;

                CompleteBrewing();
            });
        }

        protected void EnterCompletionState()
        {
            State = TeaBrewingState.Completed;
            _autoCompleteTween.Stop();

            if (pourButton != null)
            {
                if (teapot != null && pourButton.gameObject == teapot.gameObject)
                    pourButton.interactable = false;
                else
                    pourButton.gameObject.SetActive(false);
            }

            if (ingredientsGroup != null)
            {
                ingredientsGroup.interactable = false;
                ingredientsGroup.blocksRaycasts = false;
            }
        }

        private void StartPour()
        {
            State = TeaBrewingState.Pouring;
            StopIdleAnimations();

            if (pourButton != null)
                pourButton.interactable = false;

            Vector2 pourPos = _teapotStartPos + teapotPourOffset;
            Vector2 cupPourPos = _cupStartPos + cupPourOffset;

            _activeSequence.Stop();
            _activeSequence = Sequence.Create();
            _activeSequence.Group(Tween.UIAnchoredPosition(teapot, pourPos, pourDuration * 0.45f, Ease.InOutCubic));
            _activeSequence.Group(Tween.LocalRotation(teapot, teapotPourRotation, pourDuration * 0.45f, Ease.InOutCubic));
            _activeSequence.Group(Tween.UIAnchoredPosition(cup, cupPourPos, pourDuration * 0.35f, Ease.OutCubic));

            if (teaLiquidImage != null)
            {
                var c = baseTeaColor;
                c.a = 0f;
                teaLiquidImage.color = c;
                _activeSequence.Group(Tween.Alpha(teaLiquidImage, baseTeaColor.a, pourDuration * 0.45f, Ease.OutQuad, startDelay: pourDuration * 0.2f));
            }

            _activeSequence.ChainDelay(pourDuration * 0.3f);
            _activeSequence.Chain(Tween.UIAnchoredPosition(teapot, _teapotStartPos, 0.45f, Ease.OutCubic));
            _activeSequence.Group(Tween.LocalRotation(teapot, _teapotStartRot, 0.45f, Ease.OutCubic));
            _activeSequence.Group(Tween.UIAnchoredPosition(cup, _cupStartPos, 0.35f, Ease.OutCubic));
            _activeSequence.ChainCallback(this, target => target.OnPourFinished());
        }

        private void OnPourFinished()
        {
            State = TeaBrewingState.WaitingIngredient;
            if (pourButton != null)
            {
                pourButton.interactable = true;
                SetButtonLabel(pourButton, "Готово");
            }

            SetCanvasGroup(ingredientsGroup, 0f, true);
            Tween.Alpha(ingredientsGroup, 1f, 0.3f, Ease.OutCubic);

            for (int i = 0; i < ingredients.Count; i++)
            {
                var option = ingredients[i];
                if (option?.visualRoot == null)
                    continue;

                option.visualRoot.localScale = Vector3.zero;
                Tween.Scale(option.visualRoot, Vector3.one, 0.28f, Ease.OutBack, startDelay: i * 0.06f);
            }
        }

        private void OnIngredientClicked(int index)
        {
            if (State != TeaBrewingState.WaitingIngredient || index < 0 || index >= ingredients.Count)
                return;

            var option = ingredients[index];
            if (option == null || option.type == TeaIngredientType.None)
                return;

            if (_selectedSet.Contains(option.type))
                return;

            if (!CanSelectIngredient())
                return;

            _selectedSet.Add(option.type);
            _selectedIngredients.Add(option.type);
            SetIngredientSelectedVisual(option, true);
            PlayIngredientPickAnimation(option);

            ApplyIngredientAccent();

            OnIngredientSelected();
        }

        protected virtual bool CanSelectIngredient() => _selectedIngredients.Count < 1;

        protected virtual void OnIngredientSelected()
        {
            foreach (var ingredient in ingredients)
                if (ingredient?.button != null)
                    ingredient.button.interactable = false;

            ScheduleAutoComplete(0.35f);
        }

        private void SetIngredientSelectedVisual(TeaIngredientOption option, bool selected)
        {
            if (option?.visualRoot == null)
                return;

            Tween.Scale(option.visualRoot, selected ? Vector3.one * 1.08f : Vector3.one, 0.15f, Ease.OutQuad);
            if (option.iconImage != null)
            {
                var color = option.iconImage.color;
                color.a = selected ? 1f : 0.75f;
                option.iconImage.color = color;
            }
        }

        private void PlayIngredientPickAnimation(TeaIngredientOption option)
        {
            if (option?.visualRoot == null)
                return;

            var visual = option.visualRoot;
            Tween.PunchScale(visual, Vector3.one * 0.08f, 0.25f, 8);
            if (cup != null)
                Tween.PunchScale(cup, Vector3.one * 0.04f, 0.25f, 6);
        }

        private void ApplyIngredientAccent()
        {
            if (teaLiquidImage == null)
                return;

            var target = GetTeaColor(_selectedIngredients);
            Tween.Color(teaLiquidImage, target, 0.25f, Ease.OutQuad);
        }

        private Color GetTeaColor(IReadOnlyList<TeaIngredientType> selected)
        {
            if (selected == null || selected.Count == 0)
                return baseTeaColor;

            float r = 0f, g = 0f, b = 0f;
            for (int i = 0; i < selected.Count; i++)
            {
                var c = GetSingleTeaColor(selected[i]);
                r += c.r;
                g += c.g;
                b += c.b;
            }

            float inv = 1f / selected.Count;
            return new Color(r * inv, g * inv, b * inv, baseTeaColor.a);
        }

        private Color GetSingleTeaColor(TeaIngredientType type)
        {
            return type switch
            {
                TeaIngredientType.Mint => mintTeaColor,
                TeaIngredientType.LemonBalm => lemonBalmTeaColor,
                TeaIngredientType.Thyme => thymeTeaColor,
                _ => baseTeaColor
            };
        }

        protected void CompleteBrewing()
        {
            if (State != TeaBrewingState.WaitingIngredient && State != TeaBrewingState.Completed)
                return;
            if (_selectedIngredients.Count == 0)
                return;

            if (State != TeaBrewingState.Completed)
                State = TeaBrewingState.Completed;

            _autoCompleteTween.Stop();
            if (pourButton != null)
            {
                if (teapot != null && pourButton.gameObject == teapot.gameObject)
                    pourButton.interactable = false;
                else
                    pourButton.gameObject.SetActive(false);
            }

            if (ingredientsGroup != null)
            {
                ingredientsGroup.interactable = false;
                ingredientsGroup.blocksRaycasts = false;
            }

            var result = new List<TeaIngredientType>(_selectedIngredients);
            OnBrewingFinished();
            OnBrewingCompleted(result);
        }

        protected virtual void OnBrewingCompleted(IReadOnlyList<TeaIngredientType> ingredients)
        {
            Completed?.Invoke(ingredients);
        }

        protected virtual void BindAdditionalButtons() { }
        protected virtual void OnBrewingStarted() { }
        protected virtual void OnBrewingHidden() { }
        protected virtual void OnBrewingFinished() { }

        private static void SetCanvasGroup(CanvasGroup group, float alpha, bool interactive)
        {
            if (group == null)
                return;

            group.alpha = alpha;
            group.interactable = interactive;
            group.blocksRaycasts = interactive;
        }

        protected static void SetButtonLabel(Button button, string text)
        {
            if (button == null)
                return;

            var label = button.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (label != null)
            {
                label.text = text;
                return;
            }

            var legacyLabel = button.GetComponentInChildren<Text>(true);
            if (legacyLabel != null)
                legacyLabel.text = text;
        }

        protected static string GetButtonLabel(Button button)
        {
            if (button == null)
                return null;

            var label = button.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (label != null)
                return label.text;

            var legacyLabel = button.GetComponentInChildren<Text>(true);
            return legacyLabel != null ? legacyLabel.text : null;
        }

        private void KillTweens()
        {
            _activeSequence.Stop();
            _autoCompleteTween.Stop();
            StopIdleAnimations();
        }
    }
}
