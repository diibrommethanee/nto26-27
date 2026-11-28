using _Project.Develop.Data.Player;
using _Project.Develop.Gameplay.Input;
using _Project.Develop.Gameplay.Evidence;
using _Project.Develop.Gameplay.NPC;
using _Project.Develop.Gameplay.Player.Abilities;
using _Project.Develop.Gameplay.Timeline;
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
        private NpcInteract _pressedNpc;
        private WorldClue _currentPickup;
        private WorldClue _pressedPickup;
        private PlayerLasso _lasso;
        private PlayerStats _stats;
        private DayService _dayService;
        
        [Inject]
        private void Construct(InputController input, EvidenceService evidenceService, PlayerStats stats,
            DayService dayService)
        {
            _input = input;
            _evidenceService = evidenceService;
            _stats = stats;
            _dayService = dayService;
        }
        
        private void Start()
        {
            _ability = GetComponent<PlayerAbility>();
            _player = GetComponent<PlayerController>();
            _camera = UnityCamera.main;

            _lasso = new PlayerLasso(_input, _evidenceService, _camera, _ability, gameObject, _stats);
        }
        
        private void Update()
        {
            if (_input.Actions.Player.enabled && _dayService.RemainingMinutes > 0)
                _lasso.Tick();
            else
                _lasso.Cancel();

            var mousePos = _input.Actions.Player.MousePosition.ReadValue<Vector2>();
            UpdateTarget(mousePos);

        }

        private void LateUpdate()
        {
            var interaction = _input.Actions.Player.Interaction;
            if (!interaction.enabled || _input.CutsceneActive || _input.IsZoomMode)
            {
                _pressedInteractionTarget = null;
                _pressedNpc = null;
                _pressedPickup = null;
                return;
            }

            bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            var moveTo = _input.Actions.Player.MoveTo;
            if (moveTo.WasPressedThisFrame())
                _pressedNpc = pointerOverUi ? null : _currentTarget as NpcInteract;

            if (moveTo.WasReleasedThisFrame())
            {
                var pressedNpc = _pressedNpc;
                _pressedNpc = null;
                if (!pointerOverUi && pressedNpc != null && ReferenceEquals(pressedNpc, _currentTarget))
                    _player.ApproachNpc(pressedNpc);
            }

            if (interaction.WasPressedThisFrame())
            {
                _pressedInteractionTarget = pointerOverUi ? null : _currentTarget;
                _pressedPickup = pointerOverUi ? null : _currentPickup;
            }

            if (!interaction.WasReleasedThisFrame())
                return;

            var target = _pressedInteractionTarget;
            _pressedInteractionTarget = null;
            var pickup = _pressedPickup;
            _pressedPickup = null;
            if (!pointerOverUi && pickup != null && pickup == _currentPickup)
            {
                pickup.Collect(_evidenceService);
                return;
            }
            if (pointerOverUi || !ReferenceEquals(target, _currentTarget))
                return;

            if (target is NpcInteract npc)
                _player.ApproachNpc(npc);
            else
                target?.Interact();
        }

        private void UpdateTarget(Vector2 mousePosition)
        {
            _currentTarget = null;
            _currentPickup = null;
            var ray = _camera.ScreenPointToRay(mousePosition);
            foreach (var hit in Physics2D.GetRayIntersectionAll(ray, Mathf.Infinity))
            {
                if (hit.collider == null)
                    continue;

                var pickup = hit.collider.GetComponentInParent<WorldClue>();
                if (_dayService.RemainingMinutes > 0 && pickup != null && pickup.IsPickup && pickup.CanCollect(_ability.ActiveSkill))
                {
                    _currentPickup = pickup;
                    return;
                }

                var target = hit.collider.GetComponentInParent<IInteractable>();
                if (target != null)
                {
                    _currentTarget = target;
                    return;
                }
            }
        }

        private void OnDestroy()
        {
            _lasso?.Dispose();
        }
    }

}
