using _Project.Develop.Gameplay.Input;
using NaughtyAttributes;
using UnityEngine;
using PrimeTween;
using UnityEngine.InputSystem;
using VContainer;
using UnityCamera = UnityEngine.Camera;

namespace _Project.Develop.Gameplay.Player
{
    public class PlayerInteract : MonoBehaviour
    {
        [BoxGroup("Interaction Icon")]
        [SerializeField] private RectTransform interactIcon;
        [SerializeField] private Vector2 iconOffset = new Vector2(40f, -40f);
        [SerializeField] private float showDuration = 0.25f;
        [SerializeField] private float hideDuration = 0.15f;
        
        private InputController _input;
        private UnityCamera _camera;
        private IInteractable _currentTarget;
        
        [Inject]
        private void Construct(InputController input)
        {
            _input = input;
        }
        
        private void Start()
        {
            _camera = UnityCamera.main;
            interactIcon.localScale = Vector3.zero;
            
            _input.Actions.Player.Interaction.performed += OnInteraction;
            _input.Actions.Player.MousePosition.performed += OnMouseMoved;
        }
        
        private void OnDestroy()
        {
            if (_input == null) return;
            _input.Actions.Player.Interaction.performed -= OnInteraction;
            _input.Actions.Player.MousePosition.performed -= OnMouseMoved;
        }
        
        private void OnInteraction(InputAction.CallbackContext ctx)
        {
            if (_currentTarget != null)
                _currentTarget.Interact();
        }
        
        private void OnMouseMoved(InputAction.CallbackContext ctx)
        {
            var mousePosition = ctx.ReadValue<Vector2>();
            var ray = _camera.ScreenPointToRay(mousePosition);
            var hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity);

            var target = hit.collider != null
                ? hit.collider.GetComponentInParent<IInteractable>()
                : null;

            if (!ReferenceEquals(target, _currentTarget))
                SetTarget(target);

            if (_currentTarget != null)
                interactIcon.position = mousePosition + iconOffset;
        }

        private void SetTarget(IInteractable target)
        {
            _currentTarget = target;

            if (target == null)
                Tween.Scale(interactIcon, Vector3.zero, hideDuration, ease: Ease.InBack);
            else
                Tween.Scale(interactIcon, Vector3.one, showDuration, ease: Ease.OutBack);
        }
    }
}