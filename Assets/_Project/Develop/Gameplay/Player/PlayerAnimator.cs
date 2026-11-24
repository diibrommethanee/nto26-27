using UnityEngine;

namespace _Project.Develop.Gameplay.Player
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        private static readonly int Walk = Animator.StringToHash("Walk");
        
        [SerializeField] private ParticleSystem footstepSparks;

        private Animator _anim;
        private IPlayerController _player;

        private void Awake()
        {
            _anim = GetComponent<Animator>();
            _player = GetComponentInParent<IPlayerController>();
        }

        private void Update()
        {
            var walking = _player.Velocity.sqrMagnitude > 0.0001f;
            _anim.SetBool(Walk, walking);

            if (walking && !footstepSparks.isPlaying)
                footstepSparks.Play();
            else if (!walking && footstepSparks.isPlaying)
                footstepSparks.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
