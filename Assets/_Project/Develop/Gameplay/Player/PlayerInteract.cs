using _Project.Develop.Gameplay.Input;
using _Project.Develop.Gameplay.Evidence;
using UnityEngine;
using PrimeTween;
using VContainer;
using UnityCamera = UnityEngine.Camera;

namespace _Project.Develop.Gameplay.Player
{
    public class PlayerInteract : MonoBehaviour
    {
        [SerializeField] private Transform interactIcon;
        [SerializeField] private Vector3 iconOffset = new(0.5f, -0.5f);
        [SerializeField] private float showDuration = 0.25f;
        [SerializeField] private float hideDuration = 0.15f;
        
        private InputController _input;
        private EvidenceService _evidenceService;
        private UnityCamera _camera;
        
        private IInteractable _currentTarget;
        private IWorldClue _currentHoldTarget;
        private IWorldClue _holdingTarget;
        private float _holdProgress;
        
        [Inject]
        private void Construct(InputController input, EvidenceService evidenceService)
        {
            _input = input;
            _evidenceService = evidenceService;
        }
        
        private void Start()
        {
            _camera = UnityCamera.main;
            
            interactIcon.localScale = Vector3.zero;
        }
        
        private void Update()
        {
            var mousePos = _input.Actions.Player.MousePosition.ReadValue<Vector2>();
            UpdateTarget(mousePos);

            UpdateHoldInteraction();

            if (_currentTarget != null)
                PositionIcon(mousePos);

            if (_input.Actions.Player.Interaction.WasPressedThisFrame())
                _currentTarget?.Interact();
        }

        private void UpdateHoldInteraction()
        {
            var leftMouse = _input.Actions.Player.MoveTo;
            var holdTarget = _currentHoldTarget;

            if (leftMouse.WasPressedThisFrame())
            {
                CancelHold();
                if (holdTarget != null && !holdTarget.IsCollected)
                {
                    _holdingTarget = holdTarget;
                    _holdProgress = 0f;
                    _holdingTarget.SetHoldProgress(0f);
                }
            }

            if (_holdingTarget == null)
                return;

            if (!leftMouse.IsPressed() || !ReferenceEquals(holdTarget, _holdingTarget))
            {
                CancelHold();
                return;
            }

            var duration = Mathf.Max(0.01f, _holdingTarget.HoldDuration);
            _holdProgress = Mathf.Min(1f, _holdProgress + Time.deltaTime / duration);
            _holdingTarget.SetHoldProgress(_holdProgress);

            if (_holdProgress < 1f)
                return;

            var completedTarget = _holdingTarget;
            _holdingTarget = null;
            completedTarget.Collect(_evidenceService);
        }

        private void CancelHold()
        {
            if (_holdingTarget != null && !_holdingTarget.IsCollected)
                _holdingTarget.SetHoldProgress(0f);

            _holdingTarget = null;
            _holdProgress = 0f;
        }
        
        private void UpdateTarget(Vector2 mousePosition)
        {
            var ray = _camera.ScreenPointToRay(mousePosition);
            var hits = Physics2D.GetRayIntersectionAll(ray, Mathf.Infinity);

            IInteractable target = null;
            IWorldClue holdTarget = null;
            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;

                var interactiveCandidate = hit.collider.GetComponentInParent<IInteractable>();
                var holdCandidate = hit.collider.GetComponentInParent<IWorldClue>();

                if (holdCandidate != null &&
                    (holdCandidate.IsCollected || !holdCandidate.CanCollect(_input.ActiveSkill)))
                    holdCandidate = null;

                if (interactiveCandidate == null && holdCandidate == null) continue;

                target = interactiveCandidate;
                holdTarget = holdCandidate;
                break;
            }

            if (!ReferenceEquals(target, _currentTarget) || !ReferenceEquals(holdTarget, _currentHoldTarget))
                SetTarget(target, holdTarget);
        }
        
        private void PositionIcon(Vector2 screenPos)
        {
            var worldPos = _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -_camera.transform.position.z));
            worldPos.z = 0f;
            
            interactIcon.position = worldPos + iconOffset;
        }

        private void SetTarget(IInteractable target, IWorldClue holdTarget)
        {
            if (!ReferenceEquals(holdTarget, _holdingTarget))
                CancelHold();

            _currentTarget = target;
            _currentHoldTarget = holdTarget;
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
