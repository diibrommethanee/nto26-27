using PrimeTween;
using UnityEngine;

namespace _Project.Develop.Gameplay.Environment
{
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public class SpriteFadeTrigger : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float fadedAlpha;
        
        [SerializeField, Min(0.01f), Space(5)] private float fadeDuration = 0.2f;
        [SerializeField] private Ease ease = Ease.Linear;

        private SpriteRenderer _sprite;
        private float _originalAlpha;

        private void Start()
        {
            _sprite = GetComponent<SpriteRenderer>();
            _originalAlpha = _sprite.color.a;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
                FadeTo(fadedAlpha);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
                FadeTo(_originalAlpha);
        }

        private void FadeTo(float alpha)
        {
            Tween.StopAll(onTarget: _sprite);
            Tween.Alpha(_sprite, alpha, fadeDuration, ease);
        }
    }
}
