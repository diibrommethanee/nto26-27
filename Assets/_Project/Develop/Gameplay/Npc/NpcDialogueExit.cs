using _Project.Develop.Gameplay.Player;
using UnityEngine;

namespace _Project.Develop.Gameplay.NPC
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class NpcDialogueExit : MonoBehaviour
    {
        private NpcInteract _npc;
        private BoxCollider2D _zone;

        private void Awake()
        {
            _npc = GetComponentInParent<NpcInteract>();
            _zone = GetComponent<BoxCollider2D>();
        }

        internal Vector3 GetApproachPoint(Vector3 playerPosition, float stoppingDistance)
        {
            var bounds = _zone.bounds;
            var inset = Mathf.Max(.5f, stoppingDistance * 2f);
            return Vector3.MoveTowards(bounds.ClosestPoint(playerPosition), bounds.center, inset);
        }

        internal bool Contains(Collider2D playerCollider)
        {
            return _zone.Distance(playerCollider).isOverlapped;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (TryGetPlayer(other, out var player))
                player.TryStartNpcDialogue(_npc);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (TryGetPlayer(other, out _))
                _npc.EndDialogueOnExit();
        }

        private static bool TryGetPlayer(Collider2D other, out PlayerController player)
        {
            player = null;
            var body = other.attachedRigidbody;
            return !other.isTrigger && body != null && body.TryGetComponent(out player);
        }
    }
}
