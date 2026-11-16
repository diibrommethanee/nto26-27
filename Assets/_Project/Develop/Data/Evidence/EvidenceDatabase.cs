using System.Collections.Generic;
using UnityEngine;

namespace _Project.Develop.Data.Evidence
{
    [CreateAssetMenu(menuName = "Configs/Evidence/Evidence Database")]
    public class EvidenceDatabase : ScriptableObject
    {
        [SerializeField] private List<EvidenceRecipe> recipes;

        public bool TryGetResult(EvidenceData first, EvidenceData second, out EvidenceData result)
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