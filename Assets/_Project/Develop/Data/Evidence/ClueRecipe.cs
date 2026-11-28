using System;
using UnityEngine;

namespace _Project.Develop.Data.Evidence
{
    [Serializable]
    public class ClueRecipe
    {
        [SerializeField] private Clue first;
        [SerializeField] private Clue second;
        [SerializeField] private Clue result;

        internal Clue ResultFor(Clue left, Clue right)
        {
            bool matches = (first == left && second == right) || (first == right && second == left);
            return matches ? result : null;
        }
    }
}