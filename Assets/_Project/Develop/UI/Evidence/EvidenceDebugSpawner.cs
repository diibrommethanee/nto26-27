using _Project.Develop.Data.Evidence;
using UnityEngine;
using NaughtyAttributes;

namespace _Project.Develop.UI.Evidence
{
    public class EvidenceDebugSpawner : MonoBehaviour
    {
        [SerializeField] private EvidenceBoardController boardController;
        [SerializeField] private EvidenceData evidenceData;

        [SerializeField] private Vector2 spawnPosition;

        [Button("Add Evidence")]
        private void AddEvidence()
        {
            boardController.SpawnEvidence(
                evidenceData,
                spawnPosition
            );
        }
    }
}