using _Project.Develop.Gameplay.Input;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using VContainer;

namespace _Project.Develop.Gameplay.Player
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerMovement : MonoBehaviour
    {
        private InputController _input;
        
        private NavMeshAgent _agent;
        private Camera _camera;
        
        [Inject]
        private void Construct(InputController input)
        {
            _input = input;
        }
        
        private void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
        
            _agent.updateRotation = false;
            _agent.updateUpAxis = false;
            
            _camera = Camera.main;
            
            _input.Actions.Player.MoveTo.performed += _ =>
            {
                if (TryGetDestination(out var destination))
                    _agent.SetDestination(destination);
            };
        }
        
        private bool TryGetDestination(out Vector2 destination)
        {
            destination = default;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return false;

            var pos = _input.Actions.Player.MousePosition.ReadValue<Vector2>();
            if (!_camera.pixelRect.Contains(pos))
                return false;

            destination = _camera.ScreenToWorldPoint(pos);
            return true;
        }

    }
}