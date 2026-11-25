using _Project.Develop.Data.Evidence;
using UnityEngine;

namespace _Project.Develop.Gameplay.Abilities.Hearing
{
    public class HearingSource : MonoBehaviour
    {
        [Min(0.1f)] public float radius = 7f;
        [Range(0f, 1f)] public float frequency = 0.68f;
        [Range(0.005f, 0.2f)] public float tolerance = 0.035f;
        [Min(0.1f)] public float holdSeconds = 1.1f;
        [TextArea] public string transcript;
        public Clue clue;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
