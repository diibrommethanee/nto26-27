using System.Collections.Generic;
using UnityEngine;

namespace _Project.Develop.Data.Evidence
{
    [CreateAssetMenu(menuName = "Configs/Evidence/EvidenceDatabase")]
    public class EvidenceDatabase : ScriptableObject
    {
        [SerializeField] private List<ClueRecipe> recipes = new();

        public bool TryGetResult(Clue first, Clue second, out Clue result)
        {
            result = null;
            if (first == null || second == null) return false;

            foreach (var recipe in recipes)
            {
                if (recipe == null || recipe.result == null) continue;
                if ((recipe.first == first && recipe.second == second) ||
                    (recipe.first == second && recipe.second == first))
                {
                    result = recipe.result;
                    return true;
                }
            }

            return false;
        }
    }
}