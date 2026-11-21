using UnityEngine;

namespace _Project.Develop.Data.Evidence
{
    [CreateAssetMenu(menuName = "Configs/Evidence/Word Clue")]
    public class WordClue : Clue
    {
        public Sprite noteSprite;
        [TextArea(3, 12)] public string text;
    }
}
