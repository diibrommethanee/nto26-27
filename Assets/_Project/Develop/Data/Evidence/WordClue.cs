using UnityEngine;

namespace _Project.Develop.Data.Evidence
{
    [CreateAssetMenu(menuName = "Configs/Evidence/Word Clue")]
    public class WordClue : Clue
    {
        [TextArea(3, 12)] public string text;
    }
}
