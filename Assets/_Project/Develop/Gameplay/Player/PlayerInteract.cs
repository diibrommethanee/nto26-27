using _Project.Develop.Gameplay.Input;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using VContainer;

namespace _Project.Develop.Gameplay.Player
{
    public class PlayerInteract : MonoBehaviour
    {
        private InputController _input;
        
        private Camera _camera;
        
        [Inject]
        private void Construct(InputController input)
        {
            _input = input;
        }
        
        private void Start()
        {
            _camera = Camera.main;
            
            _input.Actions.Player.Interaction.performed += _ =>
            {
                if (TryInteract(out var target))
                {
                    target.Interact();
                }
            };
        }

        private bool TryInteract(out IInteractable target)
        {
            target = null;

            var mousePosition = _input.Actions.Player.MousePosition.ReadValue<Vector2>();
            var ray = _camera.ScreenPointToRay(mousePosition);

            var hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity);

            if (hit.collider == null)
                return false;

            target = hit.collider.GetComponentInParent<IInteractable>();

            return target != null;
        }
    }
}