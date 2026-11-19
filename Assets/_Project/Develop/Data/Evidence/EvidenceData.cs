using UnityEngine;

namespace _Project.Develop.Data.Evidence
{
    [CreateAssetMenu(menuName = "Configs/Evidence/Evidence Data")]
    public class EvidenceData : ScriptableObject
    {
        public string id;
        public string title;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Hypothesis Text")]
        public string fillWord;
        [Min(0), Tooltip("Индекс пропуска гипотезы, начиная с 0.")]
        public int fillIndex;
    }
}
