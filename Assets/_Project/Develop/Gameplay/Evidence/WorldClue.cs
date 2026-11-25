using _Project.Develop.Data.Evidence;
using _Project.Develop.Gameplay.Input;
using UnityEngine;

namespace _Project.Develop.Gameplay.Evidence
{
    public enum ClueSkillRequirement
    {
        None,
        Smell,
        Hearing
    }
    
    public class WorldClue : MonoBehaviour, IWorldClue
    {
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");

        [SerializeField] private Clue clue;
        [SerializeField] private ClueSkillRequirement requiredSkill;
        [SerializeField, Min(0.1f)] private float holdDuration = 1.5f;
        
        private SpriteRenderer _sprite;
        private Collider2D _сol;

        private MaterialPropertyBlock _propertyBlock;

        public bool IsCollected { get; private set; }
        public bool IsPickup => clue.type == ClueType.Collectible;
        public float HoldDuration => holdDuration;
        public ClueSkillRequirement RequiredSkill => requiredSkill;

        public bool CanCollect(SkillType? activeSkill)
        {
            return requiredSkill switch
            {
                ClueSkillRequirement.None => true,
                ClueSkillRequirement.Smell => activeSkill == SkillType.Smell,
                ClueSkillRequirement.Hearing => activeSkill == SkillType.Hearing,
                _ => false
            };
        }

        private void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();
            _сol = GetComponent<Collider2D>();

            _propertyBlock = new MaterialPropertyBlock();
            SetHoldProgress(0f);
        }

        public void SetHoldProgress(float progress)
        {
            _sprite.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(ProgressId, Mathf.Clamp01(progress));
            _sprite.SetPropertyBlock(_propertyBlock);
        }

        public void Collect(EvidenceService evidenceService)
        {
            if (IsCollected)
                return;

            SetHoldProgress(1f);
            IsCollected = true;
            evidenceService.Collect(clue, transform.position, sourceSprite: IsPickup || requiredSkill == ClueSkillRequirement.Hearing ? _sprite.sprite : null);
            _сol.enabled = false;
            if (IsPickup)
                gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (!IsCollected)
                SetHoldProgress(0f);
        }
    }
}
