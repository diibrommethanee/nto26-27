using _Project.Develop.Data;
using _Project.Develop.Data.Tea;
using System.Collections.Generic;

namespace _Project.Develop.Gameplay.Tea
{
    /// <summary>
    /// Applies the selected ingredient when a new day starts.
    /// </summary>
    public class NextDayBuffService : ISavable
    {
        public const float MovementSpeedMultiplier = 1.15f;
        public const float AbilityRadiusMultiplier = 1.2f;

        private readonly List<TeaIngredientType> _pendingIngredients = new();

        // Player buffs (applied on next day start)
        [Save] public bool MintActiveThisDay { get; private set; }
        [Save] public bool LemonBalmActiveThisDay { get; private set; }
        [Save] public bool ThymeActiveThisDay { get; private set; }

        [Save] private TeaIngredientType[] SavedIngredients
        {
            get => _pendingIngredients.ToArray();
            set => SetPendingIngredients(value);
        }

        public bool HasPendingIngredients => _pendingIngredients.Count > 0;

        public IReadOnlyList<TeaIngredientType> PendingIngredients => _pendingIngredients;

        [Save] public int? DialoguePointCostOverride { get; private set; }

        public void SetPendingIngredients(IReadOnlyList<TeaIngredientType> ingredients)
        {
            _pendingIngredients.Clear();
            if (ingredients == null)
                return;

            for (int i = 0; i < ingredients.Count; i++)
            {
                var ingredient = ingredients[i];
                if (ingredient == TeaIngredientType.None)
                    continue;

                _pendingIngredients.Add(ingredient);
                break;
            }
        }

        public void ApplyPendingForNewDay()
        {
            MintActiveThisDay = false;
            LemonBalmActiveThisDay = false;
            ThymeActiveThisDay = false;

            for (int i = 0; i < _pendingIngredients.Count; i++)
            {
                switch (_pendingIngredients[i])
                {
                    case TeaIngredientType.Mint:
                        MintActiveThisDay = true;
                        break;
                    case TeaIngredientType.LemonBalm:
                        LemonBalmActiveThisDay = true;
                        break;
                    case TeaIngredientType.Thyme:
                        ThymeActiveThisDay = true;
                        break;
                }
            }

            _pendingIngredients.Clear();
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
