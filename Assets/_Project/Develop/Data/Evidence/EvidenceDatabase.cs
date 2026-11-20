using System.Collections.Generic;
using UnityEngine;

namespace _Project.Develop.Data.Evidence
{
    [CreateAssetMenu(menuName = "Configs/Evidence/EvidenceDatabase")]
    public class EvidenceDatabase : ScriptableObject
    {
        [SerializeField] private List<ClueRecipe> recipes;

        public bool TryGetResult(Clue first, Clue second, out Clue result)
        {
            foreach (var recipe in recipes)
            {
                if ((recipe.first == first && recipe.second == second) ||
                    (recipe.first == second && recipe.second == first))
                {
                    result = recipe.result;
                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}