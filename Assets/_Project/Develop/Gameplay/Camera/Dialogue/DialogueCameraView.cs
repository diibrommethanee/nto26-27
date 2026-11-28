using UnityEngine;

namespace _Project.Develop.Gameplay.Camera
{
    [System.Serializable]
    public class DialogueCameraView
    {
        [SerializeField, Range(0.2f, 1f)] private float zoomRatio = 0.87f;
        [SerializeField, Min(0.01f)] private float blendSeconds = 0.56f;
        [SerializeField, Min(0f)] private float bubbleMargin = 1.5f;

        [SerializeField] private EnvironmentCamera environmentCamera;
        [SerializeField] private EnvironmentCamera foregroundCamera;
        private Transform player;
        private Transform npc;
        private Vector2 center;
        private float size;
        private float weight;

        public void Begin(Transform playerAnchor, Transform npcAnchor)
        {
            player = playerAnchor;
            npc = npcAnchor;
        }

        public void End() => npc = null;

        public void Blend(Vector3 followPosition, float aspect, ref Vector3 offset, ref float zoom)
        {
            weight = Mathf.MoveTowards(weight, npc ? 1f : 0f, Time.unscaledDeltaTime / blendSeconds);
            // Take the frame early and ease into the final composition.
            float remaining = 1f - weight;
            float blend = 1f - remaining * remaining * remaining * (1f + 3f * weight);
            environmentCamera.SetStrength(blend, player, npc);
            foregroundCamera.SetStrength(blend, player, npc);
            if (npc)
                FrameSpeakers(aspect, zoom);

            var target = new Vector3(center.x - followPosition.x, center.y - followPosition.y, offset.z);
            offset = Vector3.Lerp(offset, target, blend);
            zoom = Mathf.Lerp(zoom, size, blend);
        }

        private void FrameSpeakers(float aspect, float gameplayZoom)
        {
            Vector2 playerPosition = player.position;
            Vector2 npcPosition = npc.position;
            center = Vector2.Lerp(playerPosition, npcPosition, 0.4f);

            var distance = npcPosition - playerPosition;
            var verticalRoom = Mathf.Abs(distance.y) * 0.6f + bubbleMargin;
            var horizontalRoom = (Mathf.Abs(distance.x) * 0.6f + bubbleMargin) / aspect;
            var requiredSize = Mathf.Max(verticalRoom, horizontalRoom);
            size = Mathf.Clamp(requiredSize, gameplayZoom * zoomRatio, gameplayZoom);
        }
    }
}
