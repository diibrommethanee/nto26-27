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
                var match = recipe?.ResultFor(first, second);
                if (match == null) continue;

                result = match;
                return true;
            }

            return false;
        }
    }
}