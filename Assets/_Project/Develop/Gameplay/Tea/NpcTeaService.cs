using _Project.Develop.Gameplay.Npc;
using System;
using System.Collections.Generic;
using _Project.Develop.Data.Npc;
using _Project.Develop.Data.Tea;

namespace _Project.Develop.Gameplay.Tea
{
    public class NpcTeaService
    {
        private const int ClosedMoodMaximum = -25;
        private const int ExcitedMoodMinimum = 25;

        private readonly TeaRecipeBook _recipes;

        private readonly NpcStates _states;

        public NpcTeaService(TeaRecipeBook recipes, NpcStates states)
        {
            _recipes = recipes;
            _states = states;
        }

        public bool CanServe(NpcData npc) => npc != null && !_states[npc].TeaServed;

        public bool TryServe(NpcData npc, IReadOnlyList<TeaIngredientType> ingredients)
        {
            if (!CanServe(npc)) return false;

            var state = _states[npc];
            var delta = MoodDeltaFor(state.Mood, ingredients);
            state.Mood += delta;
            state.TeaServed = true;
            if (delta != 0)
                UnityEngine.Debug.Log($"[Tea] Applied mood {delta:+#;-#;0} to {npc.displayName} (new mood: {state.Mood})");
            return true;
        }

        public int MoodDeltaFor(NpcData npc, IReadOnlyList<TeaIngredientType> ingredients)
        {
            return npc == null || ingredients == null ? 0 : MoodDeltaFor(_states[npc].Mood, ingredients);
        }

        private int MoodDeltaFor(int mood, IReadOnlyList<TeaIngredientType> ingredients)
        {
            if (ingredients == null)
                return 0;

            var currentState = GetMoodState(mood);
            if (!_recipes.TryFind(currentState, ingredients, out var targetState))
                return 0;

            var targetMood = targetState switch
            {
                TeaMood.Closed => Math.Min(mood, ClosedMoodMaximum),
                TeaMood.Sleepy => Math.Clamp(mood, ClosedMoodMaximum + 1, ExcitedMoodMinimum - 1),
                TeaMood.Excited => Math.Max(mood, ExcitedMoodMinimum),
                _ => mood
            };

            return targetMood - mood;
        }

        private static TeaMood GetMoodState(int mood)
        {
            if (mood <= ClosedMoodMaximum)
                return TeaMood.Closed;
            if (mood >= ExcitedMoodMinimum)
                return TeaMood.Excited;
            return TeaMood.Sleepy;
        }
    }
}
