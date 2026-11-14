using _Project.Develop.Data.Player;
using _Project.Develop.Gameplay.Input;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using VContainer;
using UnityCamera = UnityEngine.Camera;

namespace _Project.Develop.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Expandable] private PlayerStats stats;
        
        private Rigidbody2D _rb;
        private float _targetX;
        private bool _hasTarget;
        
        private InputController _input;
        private UnityCamera _camera;
        
        [Inject]
        private void Construct(InputController input)
        {
            _input = input;
        }

        private void Start()
        {
            _rb = GetComponent<Rigidbody2D>();
            _camera = UnityCamera.main;

            _input.Actions.Player.MoveTo.performed += OnMoveTo;
        }

        private void OnMoveTo(InputAction.CallbackContext ctx)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
            
            var mousePos = _input.Actions.Player.MousePosition.ReadValue<Vector2>();
            if (!_camera.pixelRect.Contains(mousePos))
                return;

            var ray = _camera.ScreenPointToRay(mousePos);
            var plane = new Plane(Vector3.forward, transform.position);

            if (!plane.Raycast(ray, out var distance))
                return;

            _targetX = ray.GetPoint(distance).x;
            _hasTarget = true;
        }

        private void FixedUpdate()
        {
            var velocity = _rb.linearVelocity;

            if (!_hasTarget || Mathf.Abs(_targetX - _rb.position.x) <= stats.stoppingDistance)
            {
                _hasTarget = false;
                velocity.x = 0f;
            }
            else
            {
                var distance = _targetX - _rb.position.x;
                var brakingSpeed = Mathf.Sqrt(2f * stats.acceleration * Mathf.Abs(distance));
                var targetSpeed = Mathf.Sign(distance) * Mathf.Min(stats.speed, brakingSpeed);

                velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, stats.acceleration * Time.fixedDeltaTime);

                var maxSpeed = Mathf.Abs(distance) / Time.fixedDeltaTime;
                velocity.x = Mathf.Clamp(velocity.x, -maxSpeed, maxSpeed);
            }

            _rb.linearVelocity = velocity;
        }

        private void OnDisable()
        {
            _hasTarget = false;
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        }

        private void OnDestroy()
        {
            _input.Actions.Player.MoveTo.performed -= OnMoveTo;
        }
    }
}
