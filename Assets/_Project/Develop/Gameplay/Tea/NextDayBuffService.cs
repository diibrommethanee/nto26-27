using _Project.Develop.Data;
using _Project.Develop.Data.Tea;
using System;
using System.Collections.Generic;
using System.Linq;

namespace _Project.Develop.Gameplay.Tea
{
    /// <summary>
    /// Applies the selected ingredient when a new day starts.
    /// </summary>
    public class NextDayBuffService : ISavable
    {
        public const float MovementSpeedMultiplier = 1.15f;
        public const float AbilityRadiusMultiplier = 1.2f;

        private TeaIngredientType _pendingIngredient;

        [Save] public bool MintActiveThisDay { get; private set; }
        [Save] public bool LemonBalmActiveThisDay { get; private set; }
        [Save] public bool ThymeActiveThisDay { get; private set; }

        [Save] private TeaIngredientType[] SavedIngredients
        {
            get => HasPendingIngredients ? new[] { _pendingIngredient } : Array.Empty<TeaIngredientType>();
            set => SetPendingIngredients(value);
        }

        public float AbilityRadius(float baseRadius) =>
            ThymeActiveThisDay ? baseRadius * AbilityRadiusMultiplier : baseRadius;

        public float MovementSpeed(float baseSpeed) =>
            LemonBalmActiveThisDay ? baseSpeed * MovementSpeedMultiplier : baseSpeed;

        public bool HasPendingIngredients => _pendingIngredient != TeaIngredientType.None;

        public IReadOnlyList<TeaIngredientType> PendingIngredients => SavedIngredients;

        [Save] public int? DialoguePointCostOverride { get; private set; }

        public void SetPendingIngredients(IReadOnlyList<TeaIngredientType> ingredients)
        {
            _pendingIngredient = ingredients?.FirstOrDefault(ingredient => ingredient != TeaIngredientType.None)
                ?? TeaIngredientType.None;
        }

        public void ApplyPendingForNewDay()
        {
            MintActiveThisDay = _pendingIngredient == TeaIngredientType.Mint;
            LemonBalmActiveThisDay = _pendingIngredient == TeaIngredientType.LemonBalm;
            ThymeActiveThisDay = _pendingIngredient == TeaIngredientType.Thyme;
            _pendingIngredient = TeaIngredientType.None;
        }

        public void SetDialoguePointCostOverride(int? costMinutes)
        {
            DialoguePointCostOverride = costMinutes;
        }

        public void ClearCurrentDayBuffs()
        {
            MintActiveThisDay = false;
            LemonBalmActiveThisDay = false;
            ThymeActiveThisDay = false;
            DialoguePointCostOverride = null;
        }
    }
}
