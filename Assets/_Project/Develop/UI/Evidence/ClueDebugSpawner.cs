using _Project.Develop.Data.Evidence;
using UnityEngine;
using NaughtyAttributes;

namespace _Project.Develop.UI.Evidence
{
    public class ClueDebugSpawner : MonoBehaviour
    {
        [SerializeField] private EvidenceView boardController;
        [SerializeField] private Clue clue;

        [SerializeField] private Vector2 spawnPosition;

        [Button("Add Clue")]
        private void AddClue()
        {
            boardController.SpawnClue(clue, spawnPosition);
        }
    }
}