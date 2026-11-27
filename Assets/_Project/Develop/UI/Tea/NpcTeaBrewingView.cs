using _Project.Develop.Data.Tea;
using System;
using System.Collections.Generic;
using _Project.Develop.Data.Npc;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Develop.UI.Tea
{
    public class NpcTeaBrewingView : TeaBrewingView
    {
        [Header("NPC tea actions")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button offerTeaButton;

        private NpcData _targetNpc;
        private string _initialRestartButtonLabel;
        private string _initialOfferTeaButtonLabel;
        private bool _offerTeaButtonAssigned;

        public event Action<NpcData, IReadOnlyList<TeaIngredientType>> NpcCompleted;

        public bool BeginForNpc(NpcData npc)
        {
            if (npc == null)
            {
                Debug.LogWarning("Cannot brew tea for a null NPC.");
                return false;
            }

            BeginBrewing();
            _targetNpc = npc;
            return true;
        }

        public override void HideImmediate()
        {
            _targetNpc = null;
            base.HideImmediate();
        }

        public override void TryCompleteBrewing()
        {
            if (CanCompleteBrewing)
                ShowCompletionActions();
        }

        protected override bool CanSelectIngredient() => SelectedIngredients.Count < 1;

        protected override void OnIngredientSelected()
        {
            if (!CanCompleteBrewing)
                return;

            ShowCompletionActions();
        }

        protected override void OnPourClicked()
        {
            if (State == TeaBrewingState.ReadyToPour)
            {
                base.OnPourClicked();
                return;
            }

            if (CanCompleteBrewing)
                ShowCompletionActions();
        }

        protected override void BindAdditionalButtons()
        {
            _offerTeaButtonAssigned = offerTeaButton != null;
            _initialRestartButtonLabel = GetButtonLabel(restartButton) ?? "???????";
            _initialOfferTeaButtonLabel = GetButtonLabel(offerTeaButton) ?? "?????????? ???";

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartClicked);
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (offerTeaButton != null)
            {
                offerTeaButton.onClick.RemoveListener(OnOfferTeaClicked);
                offerTeaButton.onClick.AddListener(OnOfferTeaClicked);
            }

            SetActionButtonsVisible(false);
        }

        protected override void OnBrewingStarted()
        {
            if (!string.IsNullOrEmpty(_initialRestartButtonLabel))
                SetButtonLabel(restartButton, _initialRestartButtonLabel);
            if (!string.IsNullOrEmpty(_initialOfferTeaButtonLabel))
                SetButtonLabel(offerTeaButton, _initialOfferTeaButtonLabel);

            SetActionButtonsVisible(false);
        }

        protected override void OnBrewingHidden()
        {
            SetActionButtonsVisible(false);
        }

        protected override void OnBrewingFinished()
        {
            SetActionButtonsVisible(false);
        }

        protected override void OnBrewingCompleted(IReadOnlyList<TeaIngredientType> ingredients)
        {
            NpcCompleted?.Invoke(_targetNpc, ingredients);
        }

        private void OnRestartClicked()
        {
            if (State != TeaBrewingState.WaitingIngredient && State != TeaBrewingState.Completed)
                return;

            BeginBrewing();
        }

        private void OnOfferTeaClicked()
        {
            if ((State == TeaBrewingState.WaitingIngredient || State == TeaBrewingState.Completed) &&
                SelectedIngredients.Count > 0)
            {
                CompleteBrewing();
            }
        }

        private void ShowCompletionActions()
        {
            if (State != TeaBrewingState.WaitingIngredient || !CanCompleteBrewing)
                return;

            EnterCompletionState();
            if (!_offerTeaButtonAssigned || offerTeaButton == null)
            {
                CompleteBrewing();
                return;
            }

            SetActionButtonsVisible(true);
            if (restartButton != null)
                restartButton.interactable = true;
            offerTeaButton.interactable = true;
        }

        private void SetActionButtonsVisible(bool visible)
        {
            if (restartButton != null)
                restartButton.gameObject.SetActive(visible);
            if (offerTeaButton != null)
                offerTeaButton.gameObject.SetActive(visible);
        }
    }
}
