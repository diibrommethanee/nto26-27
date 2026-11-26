using System;
using System.Collections.Generic;
using _Project.Develop.Data.Npc;

namespace _Project.Develop.Gameplay.Tea
{
    /// <summary>
    /// Applies a single ingredient as a next-day player buff and maps NPC recipes to mood transitions.
    /// </summary>
    public class NextDayBuffService
    {
        public const float MovementSpeedMultiplier = 1.15f;
        public const float AbilityRadiusMultiplier = 1.2f;

        private const int ClosedMoodMaximum = -25;
        private const int SleepyMoodMinimum = -24;
        private const int SleepyMoodMaximum = 24;
        private const int ExcitedMoodMinimum = 25;

        private const int MintMask = 1;
        private const int LemonBalmMask = 2;
        private const int ThymeMask = 4;

        private readonly List<TeaIngredientType> _pendingIngredients = new();

        // Player buffs (applied on next day start)
        public bool MintActiveThisDay { get; private set; }
        public bool LemonBalmActiveThisDay { get; private set; }
        public bool ThymeActiveThisDay { get; private set; }

        public int MoodDeltaFor(NpcData npc, IReadOnlyList<TeaIngredientType> ingredients)
        {
            if (npc == null || ingredients == null)
                return 0;

            var mood = npc.state != null ? npc.state.mood : npc.initialMood;
            var currentState = GetMoodState(mood);
            var recipe = GetRecipeMask(ingredients);
            if (!TryGetTargetState(currentState, recipe, out var targetState))
                return 0;

            var targetMood = targetState switch
            {
                TeaMoodState.Closed => Math.Min(mood, ClosedMoodMaximum),
                TeaMoodState.Sleepy => Math.Max(SleepyMoodMinimum, Math.Min(SleepyMoodMaximum, mood)),
                TeaMoodState.Excited => Math.Max(mood, ExcitedMoodMinimum),
                _ => mood
            };

            return targetMood - mood;
        }

        public bool HasPendingIngredients => _pendingIngredients.Count > 0;

        public IReadOnlyList<TeaIngredientType> PendingIngredients => _pendingIngredients;

        public int? DialoguePointCostOverride { get; private set; }

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

        private static TeaMoodState GetMoodState(int mood)
        {
            if (mood <= ClosedMoodMaximum)
                return TeaMoodState.Closed;
            if (mood >= ExcitedMoodMinimum)
                return TeaMoodState.Excited;
            return TeaMoodState.Sleepy;
        }

        private static int GetRecipeMask(IReadOnlyList<TeaIngredientType> ingredients)
        {
            var mask = 0;
            for (int i = 0; i < ingredients.Count; i++)
            {
                mask |= ingredients[i] switch
                {
                    TeaIngredientType.Mint => MintMask,
                    TeaIngredientType.LemonBalm => LemonBalmMask,
                    TeaIngredientType.Thyme => ThymeMask,
                    _ => 0
                };
            }

            return mask;
        }

        private static bool TryGetTargetState(TeaMoodState current, int recipe, out TeaMoodState target)
        {
            // Closed: mint -> excited, mint + lemon balm -> sleepy.
            // Excited: lemon balm -> sleepy, lemon balm + thyme -> closed.
            // Sleepy: thyme -> closed, mint + thyme -> excited.
            target = current switch
            {
                TeaMoodState.Closed when recipe == MintMask => TeaMoodState.Excited,
                TeaMoodState.Closed when recipe == (MintMask | LemonBalmMask) => TeaMoodState.Sleepy,
                TeaMoodState.Excited when recipe == LemonBalmMask => TeaMoodState.Sleepy,
                TeaMoodState.Excited when recipe == (LemonBalmMask | ThymeMask) => TeaMoodState.Closed,
                TeaMoodState.Sleepy when recipe == ThymeMask => TeaMoodState.Closed,
                TeaMoodState.Sleepy when recipe == (MintMask | ThymeMask) => TeaMoodState.Excited,
                _ => current
            };

            return target != current;
        }

        private enum TeaMoodState
        {
            Closed,
            Excited,
            Sleepy
        }
    }
}
