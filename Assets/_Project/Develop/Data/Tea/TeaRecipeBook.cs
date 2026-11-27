using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Develop.Data.Tea
{
    // Numeric values are serialized in existing tea brewing scenes.
    public enum TeaIngredientType { None, Mint, LemonBalm, Thyme }
    public enum TeaMood { Closed, Sleepy, Excited }

    [CreateAssetMenu(menuName = "Configs/Tea/NPC Recipe Book")]
    public class TeaRecipeBook : ScriptableObject
    {
        [Serializable]
        private struct Recipe
        {
            public TeaMood currentMood;
            public TeaIngredientType[] ingredients;
            public TeaMood targetMood;
        }

        [SerializeField] private Recipe[] recipes = Array.Empty<Recipe>();

        public bool TryFind(TeaMood currentMood, IReadOnlyList<TeaIngredientType> ingredients, out TeaMood target)
        {
            var selected = new HashSet<TeaIngredientType>(ingredients);
            selected.Remove(TeaIngredientType.None);
            foreach (var recipe in recipes)
            {
                if (recipe.currentMood == currentMood && selected.Count > 0 &&
                    recipe.ingredients != null && selected.SetEquals(recipe.ingredients))
                {
                    target = recipe.targetMood;
                    return target != currentMood;
                }
            }

            target = currentMood;
            return false;
        }
    }
}
