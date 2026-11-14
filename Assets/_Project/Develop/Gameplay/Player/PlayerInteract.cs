using _Project.Develop.Gameplay.Input;
using NaughtyAttributes;
using UnityEngine;
using PrimeTween;
using VContainer;
using UnityCamera = UnityEngine.Camera;

namespace _Project.Develop.Gameplay.Player
{
    public class PlayerInteract : MonoBehaviour
    {
        [BoxGroup("Interaction Icon")]
        
        [SerializeField] private Transform interactIcon;
        [SerializeField] private Vector3 iconOffset = new Vector3(40f, -40f, 0f);
        [SerializeField] private float iconWorldZ = -1f;
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
        }
        
        private void Update()
        {
            var mousePosition = _input.Actions.Player.MousePosition.ReadValue<Vector2>();
            
            UpdateTarget(mousePosition);

            if (_currentTarget != null)
                PositionIcon(mousePosition);

            if (_input.Actions.Player.Interaction.WasPressedThisFrame())
                _currentTarget?.Interact();
            
            Debug.Log(_currentTarget == null);
        }
        
        private void UpdateTarget(Vector2 mousePosition)
        {
            var ray = _camera.ScreenPointToRay(mousePosition);
            var hits = Physics2D.GetRayIntersectionAll(ray, Mathf.Infinity);

            IInteractable target = null;
            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                var candidate = hit.collider.GetComponent<IInteractable>();
                if (candidate != null)
                {
                    target = candidate;
                    break;
                }
            }

            if (!ReferenceEquals(target, _currentTarget))
                SetTarget(target);
        }
        
        private void PositionIcon(Vector2 screenPosition)
        {
            var world = _camera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, -_camera.transform.position.z));
            world.z = iconWorldZ;
            interactIcon.position = world + iconOffset;
        }

        private void SetTarget(IInteractable target)
        {
            _currentTarget = target;
            Tween.StopAll(onTarget: interactIcon);
            
            if (target == null)
            {
                if (interactIcon.localScale == Vector3.zero)
                    return;
                
                Tween.Scale(interactIcon, Vector3.zero, hideDuration, ease: Ease.InBack);
            }
            else
            {
                if (interactIcon.localScale == Vector3.one)
                    return;
                
                Tween.Scale(interactIcon, Vector3.one, showDuration, ease: Ease.OutBack);
            }
        }
    }
}