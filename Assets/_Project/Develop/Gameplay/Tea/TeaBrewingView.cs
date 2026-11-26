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
        AddingIngredient,
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

        [Header("Teapot / Cup")]
        [SerializeField] private RectTransform teapot;
        [SerializeField] private RectTransform cup;

        [Header("Ingredients")]
        [SerializeField] private CanvasGroup ingredientsGroup;
        [SerializeField] private List<TeaIngredientOption> ingredients = new();

        [Header("Animation")]
        [SerializeField] private float pourDuration = 1.35f;
        [SerializeField] private float teapotMoveDuration = 0.45f;
        [SerializeField] private Vector2 teapotPourOffset = new(-180f, 140f);
        [SerializeField] private float teapotPourAngle = 28f;
        [SerializeField] private float ingredientDropDuration = 0.55f;

        public event Action<TeaIngredientOption> Completed;
        public bool IsCompleted => State == TeaBrewingState.Completed;
        public TeaBrewingState State { get; private set; } = TeaBrewingState.Idle;
        public TeaIngredientOption SelectedIngredient { get; private set; }

        private Vector2 _teapotStartPos;
        private Vector3 _teapotStartRot;
        private Vector3 _teapotStartScale;
        private Vector2 _cupStartPos;
        private Sequence _activeSequence;
        private readonly List<Tween> _idleTweens = new();
        private bool _initialized;

        private void Awake()
        {
            EnsureInitialized();
            HideImmediate();
        }

        private void OnDestroy()
        {
            UnbindButtons();
            KillTweens();
        }

        public void Begin()
        {
            EnsureInitialized();
            KillTweens();
            ResetVisuals();
            gameObject.SetActive(true);

            State = TeaBrewingState.ReadyToPour;
            SetCanvasGroup(canvasGroup, 1f, true);
            SetCanvasGroup(ingredientsGroup, 0f, false);

            if (pourButton != null)
                pourButton.interactable = true;

            foreach (var ingredient in ingredients)
            {
                if (ingredient?.button != null)
                    ingredient.button.interactable = true;
            }

            PlayIntroAnimation();
            StartIdleAnimation();
        }

        public void HideImmediate()
        {
            EnsureInitialized();
            KillTweens();
            ResetVisuals();
            State = TeaBrewingState.Idle;
            SetCanvasGroup(canvasGroup, 0f, false);
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

        private void BindButtons()
        {
            if (pourButton == null && teapot != null)
                pourButton = teapot.GetComponent<Button>();

            if (pourButton != null)
                pourButton.onClick.AddListener(OnPourClicked);

            for (int i = 0; i < ingredients.Count; i++)
            {
                int index = i;
                var option = ingredients[index];
                if (option?.button != null)
                    option.button.onClick.AddListener(() => OnIngredientClicked(index));
            }
        }

        private void UnbindButtons()
        {
            if (pourButton != null)
                pourButton.onClick.RemoveListener(OnPourClicked);

            foreach (var option in ingredients)
            {
                if (option?.button != null)
                    option.button.onClick.RemoveAllListeners();
            }
        }

        private void ResetVisuals()
        {
            SelectedIngredient = null;

            if (teapot != null)
            {
                teapot.anchoredPosition = _teapotStartPos;
                teapot.localEulerAngles = _teapotStartRot;
                teapot.localScale = _teapotStartScale;
            }

            if (cup != null)
                cup.anchoredPosition = _cupStartPos;

            SetCanvasGroup(ingredientsGroup, 0f, false);

            foreach (var option in ingredients)
            {
                if (option?.visualRoot != null)
                    option.visualRoot.localScale = Vector3.one;
            }
        }

        private void PlayIntroAnimation()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                Tween.Alpha(canvasGroup, 1f, 0.35f, Ease.OutCubic);
            }

            if (teapot != null)
            {
                teapot.localScale = _teapotStartScale * 0.92f;
                Tween.Scale(teapot, _teapotStartScale, 0.45f, Ease.OutBack);
            }

            if (cup != null)
            {
                cup.anchoredPosition = _cupStartPos + new Vector2(0f, -30f);
                Tween.UIAnchoredPosition(cup, _cupStartPos, 0.45f, Ease.OutCubic);
            }
        }

        private void StartIdleAnimation()
        {
            if (teapot == null)
                return;

            _idleTweens.Add(Tween.LocalPositionY(teapot, _teapotStartPos.y + 8f, 1.8f, Ease.InOutSine, -1, CycleMode.Yoyo));
        }

        private void StopIdleAnimations()
        {
            foreach (var tween in _idleTweens)
                tween.Stop();

            _idleTweens.Clear();
        }

        private void OnPourClicked()
        {
            if (State != TeaBrewingState.ReadyToPour)
                return;

            State = TeaBrewingState.Pouring;
            StopIdleAnimations();

            if (pourButton != null)
                pourButton.interactable = false;

            Vector2 pourPos = _teapotStartPos + teapotPourOffset;
            Vector3 pourRot = _teapotStartRot + new Vector3(0f, 0f, teapotPourAngle);

            _activeSequence = Sequence.Create();
            if (teapot != null)
            {
                _activeSequence.Group(Tween.UIAnchoredPosition(teapot, pourPos, teapotMoveDuration, Ease.InOutCubic));
                _activeSequence.Group(Tween.LocalRotation(teapot, pourRot, teapotMoveDuration, Ease.InOutCubic));
            }

            _activeSequence.Chain(Tween.Delay(pourDuration));
            if (cup != null)
                _activeSequence.Group(Tween.PunchScale(cup, Vector3.one * 0.04f, pourDuration, 2));

            if (teapot != null)
            {
                _activeSequence.Chain(Tween.UIAnchoredPosition(teapot, _teapotStartPos, teapotMoveDuration, Ease.InOutCubic));
                _activeSequence.Group(Tween.LocalRotation(teapot, _teapotStartRot, teapotMoveDuration, Ease.InOutCubic));
            }

            _activeSequence.OnComplete(this, target => target.OnPourCompleted());
        }

        private void OnPourCompleted()
        {
            State = TeaBrewingState.WaitingIngredient;
            ShowIngredients();
        }

        private void ShowIngredients()
        {
            if (ingredientsGroup == null)
                return;

            SetCanvasGroup(ingredientsGroup, 0f, true);
            Tween.Alpha(ingredientsGroup, 1f, 0.3f, Ease.OutCubic);

            for (int i = 0; i < ingredients.Count; i++)
            {
                var option = ingredients[i];
                if (option?.visualRoot == null)
                    continue;

                option.visualRoot.localScale = Vector3.one * 0.8f;
                Tween.Scale(option.visualRoot, Vector3.one, 0.32f, Ease.OutBack, startDelay: i * 0.06f);
            }
        }

        private void OnIngredientClicked(int index)
        {
            if (State != TeaBrewingState.WaitingIngredient || index < 0 || index >= ingredients.Count)
                return;

            var option = ingredients[index];
            if (option == null)
                return;

            State = TeaBrewingState.AddingIngredient;
            SelectedIngredient = option;

            foreach (var ingredient in ingredients)
            {
                if (ingredient?.button != null)
                    ingredient.button.interactable = false;
            }

            PlayIngredientAnimation(option);
        }

        private void PlayIngredientAnimation(TeaIngredientOption option)
        {
            var sequence = Sequence.Create();
            var visual = option.visualRoot;

            if (visual != null)
            {
                Vector2 startPos = visual.anchoredPosition;
                Vector2 targetPos = cup != null
                    ? cup.anchoredPosition + new Vector2(0f, 110f)
                    : startPos + new Vector2(0f, -120f);

                sequence.Group(Tween.UIAnchoredPosition(visual, targetPos, ingredientDropDuration, Ease.InCubic));
                sequence.Group(Tween.Scale(visual, Vector3.one * 0.55f, ingredientDropDuration, Ease.InQuad));
                sequence.ChainCallback(this, target => target.ApplyIngredientAccent());
                sequence.Chain(Tween.Scale(visual, Vector3.zero, 0.18f, Ease.InBack));
                sequence.ChainCallback(() =>
                {
                    visual.anchoredPosition = startPos;
                    visual.localScale = Vector3.one;
                });
            }
            else
            {
                sequence.ChainCallback(this, target => target.ApplyIngredientAccent());
            }

            sequence.ChainCallback(this, target => target.OnIngredientApplied(option));
        }

        private void ApplyIngredientAccent()
        {
            if (cup != null)
                Tween.PunchScale(cup, Vector3.one * 0.06f, 0.35f, 8);
        }

        private void OnIngredientApplied(TeaIngredientOption option)
        {
            State = TeaBrewingState.Completed;
            SelectedIngredient = option;

            if (ingredientsGroup != null)
            {
                ingredientsGroup.interactable = false;
                ingredientsGroup.blocksRaycasts = false;
                Tween.Alpha(ingredientsGroup, 0.35f, 0.25f, Ease.OutQuad);
            }

            Completed?.Invoke(option);
        }

        private static void SetCanvasGroup(CanvasGroup group, float alpha, bool interactive)
        {
            if (group == null)
                return;

            group.alpha = alpha;
            group.interactable = interactive;
            group.blocksRaycasts = interactive;
        }

        private void KillTweens()
        {
            _activeSequence.Stop();
            StopIdleAnimations();
        }
    }
}
