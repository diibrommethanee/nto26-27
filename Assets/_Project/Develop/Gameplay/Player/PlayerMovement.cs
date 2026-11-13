using _Project.Develop.Data.Player;
using _Project.Develop.Gameplay.Input;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using VContainer;
using UnityCamera = UnityEngine.Camera;

namespace _Project.Develop.Gameplay.Player
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Expandable] private PlayerStats stats;
        
        private NavMeshAgent _agent;
        private NavMeshPath _path;
        
        private InputController _input;
        private UnityCamera _camera;
        
        [Inject]
        private void Construct(InputController input)
        {
            _input = input;
        }
        
        private void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
            _path = new NavMeshPath();
            
            _agent.speed = stats.speed;
            _agent.acceleration = stats.acceleration;
            _agent.stoppingDistance = stats.stoppingDistance;
            
            _agent.updateRotation = false;
            _agent.updateUpAxis = false;
            
            _camera = UnityCamera.main;

            _input.Actions.Player.MoveTo.performed += OnMoveTo;
        }

        private void OnMoveTo(InputAction.CallbackContext ctx)
        {
            if (CalculatePath())
                _agent.SetPath(_path);
        }
        
        private bool CalculatePath()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return false;
            
            var mousePos = _input.Actions.Player.MousePosition.ReadValue<Vector2>();
            if (!_camera.pixelRect.Contains(mousePos))
                return false;

            var destination = _camera.ScreenToWorldPoint(mousePos);
            destination.z = transform.position.z;

            return _agent.CalculatePath(destination, _path) &&
                   _path.status == NavMeshPathStatus.PathComplete;
        }

        private void OnDestroy()
        {
            _input.Actions.Player.MoveTo.performed -= OnMoveTo;
        }
    }
}