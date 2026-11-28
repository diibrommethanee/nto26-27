using _Project.Develop.Gameplay.Input;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using UnityCamera = UnityEngine.Camera;

namespace _Project.Develop.Gameplay.Environment
{
    [RequireComponent(typeof(Collider2D), typeof(Animator))]
    public class AnimatorClickTrigger : MonoBehaviour
    {
        private static readonly int Trigger = Animator.StringToHash("trigger");
        
        private UnityCamera _camera;
        private Collider2D _col;

        private InputController _input;
        private Animator _anim;

        [Inject]
        private void Construct(InputController input)
        {
            _input = input;
        }

        private void Start()
        {
            _camera = UnityCamera.main;
            _col = GetComponent<Collider2D>();
            
            _anim = GetComponent<Animator>();
            
            _input.Actions.Player.MoveTo.performed += OnClick;
        }

        private void OnClick(InputAction.CallbackContext _)
        {
            TriggerAtAsync().Forget();
        }

        private async UniTask TriggerAtAsync()
        {
            await UniTask.Yield(PlayerLoopTiming.LastUpdate, destroyCancellationToken);
            if (!isActiveAndEnabled)
                return;

            if (_input.IsPointerOverUI)
                return;

            var pos = _input.PointerPosition;
            
            var ray = _camera.ScreenPointToRay(pos);
            var plane = new Plane(Vector3.forward, _col.transform.position);
            if (plane.Raycast(ray, out var distance) && _col.OverlapPoint(ray.GetPoint(distance)))
                _anim.SetTrigger(Trigger);
        }
        
        private void OnDestroy()
        {
            _input.Actions.Player.MoveTo.performed -= OnClick;
        }
    }
}
