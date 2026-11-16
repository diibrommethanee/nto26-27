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
    }
}