using _Project.Develop.Gameplay.Input;
using _Project.Develop.Gameplay.Evidence;
using _Project.Develop.Gameplay.NPC;
using _Project.Develop.Gameplay.Player.Abilities;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;
using UnityCamera = UnityEngine.Camera;

namespace _Project.Develop.Gameplay.Player
{
    public class PlayerInteract : MonoBehaviour
    {
        private InputController _input;
        private PlayerAbility _ability;
        private PlayerController _player;
        private EvidenceService _evidenceService;
        private UnityCamera _camera;
        
        private IInteractable _currentTarget;
        private IInteractable _pressedInteractionTarget;
        private IWorldClue _currentHoldTarget;
        private IWorldClue _holdingTarget;
        private float _holdProgress;
        private PlayerLasso _lasso;
        
        [Inject]
        private void Construct(InputController input, EvidenceService evidenceService)
        {
            _input = input;
            _evidenceService = evidenceService;
        }
        
        private void Start()
        {
            _ability = GetComponent<PlayerAbility>();
            _player = GetComponent<PlayerController>();
            _camera = UnityCamera.main;

            _lasso = new PlayerLasso(_input, _evidenceService, _camera, _ability, gameObject);
        }
        
        private void Update()
        {
            if (_input.Actions.Player.enabled)
                _lasso.Tick();
            else
                _lasso.Cancel();

            var mousePos = _input.Actions.Player.MousePosition.ReadValue<Vector2>();
            UpdateTarget(mousePos);

            UpdateHoldInteraction();
        }

        private void LateUpdate()
        {
            var interaction = _input.Actions.Player.Interaction;
            if (!interaction.enabled || _input.CutsceneActive || _input.IsZoomMode)
            {
                _pressedInteractionTarget = null;
                return;
            }

            bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (interaction.WasPressedThisFrame())
                _pressedInteractionTarget = pointerOverUi ? null : _currentTarget;

            if (!interaction.WasReleasedThisFrame())
                return;

            var target = _pressedInteractionTarget;
            _pressedInteractionTarget = null;
            if (pointerOverUi || !ReferenceEquals(target, _currentTarget))
                return;

            if (target is NpcInteract npc)
                _player.ApproachNpc(npc);
            else
                target?.Interact();
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
                var holdCandidate = hit.collider.GetComponentInParent<WorldClue>();

                if (holdCandidate != null &&
                    (holdCandidate.IsCollected ||
                     holdCandidate.RequiredSkill == ClueSkillRequirement.None ||
                     !holdCandidate.CanCollect(_ability.ActiveSkill)))
                    holdCandidate = null;

                if (interactiveCandidate == null && holdCandidate == null) continue;

                target = interactiveCandidate;
                holdTarget = holdCandidate;
                break;
            }

            if (!ReferenceEquals(target, _currentTarget) || !ReferenceEquals(holdTarget, _currentHoldTarget))
                SetTarget(target, holdTarget);
        }

        private void SetTarget(IInteractable target, IWorldClue holdTarget)
        {
            if (!ReferenceEquals(holdTarget, _holdingTarget))
                CancelHold();

            _currentTarget = target;
            _currentHoldTarget = holdTarget;
        }

        private void OnDestroy()
        {
            _lasso?.Dispose();
        }
    }

}
