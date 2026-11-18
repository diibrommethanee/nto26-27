using _Project.Develop.Data.Evidence;
using _Project.Develop.Gameplay.Player;
using UnityEngine;

namespace _Project.Develop.Gameplay.Evidence
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class WorldEvidence : MonoBehaviour, IWorldEvidence
    {
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");

        [SerializeField] private EvidenceData evidence;
        [SerializeField, Min(0.1f)] private float holdDuration = 1.5f;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Collider2D targetCollider;

        private MaterialPropertyBlock _propertyBlock;

        public bool IsCollected { get; private set; }
        public float HoldDuration => holdDuration;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (targetCollider == null)
                targetCollider = GetComponent<Collider2D>();

            _propertyBlock = new MaterialPropertyBlock();
            SetHoldProgress(0f);
        }

        public void SetHoldProgress(float progress)
        {
            if (spriteRenderer == null)
                return;

            spriteRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(ProgressId, Mathf.Clamp01(progress));
            spriteRenderer.SetPropertyBlock(_propertyBlock);
        }

        public void Collect(EvidenceService evidenceService)
        {
            if (IsCollected)
                return;

            if (evidence == null)
            {
                Debug.LogError($"World evidence '{name}' has no Evidence Data assigned.", this);
                return;
            }

            if (evidenceService == null)
            {
                Debug.LogError($"PlayerInteract could not provide EvidenceService to '{name}'. Check its GameScope injection.", this);
                return;
            }

            SetHoldProgress(1f);
            IsCollected = true;
            evidenceService.Collect(evidence, transform.position);
            if (targetCollider != null)
                targetCollider.enabled = false;
        }

        private void OnDisable()
        {
            if (!IsCollected)
                SetHoldProgress(0f);
        }
    }
}
