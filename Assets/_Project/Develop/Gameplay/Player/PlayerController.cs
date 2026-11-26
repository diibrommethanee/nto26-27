using _Project.Develop.Data.Player;
using _Project.Develop.Gameplay.Dialogue;
using _Project.Develop.Gameplay.Evidence;
using _Project.Develop.Gameplay.Input;
using _Project.Develop.Gameplay.NPC;
using _Project.Develop.Gameplay.Player.Abilities;
using _Project.Develop.Gameplay.Tea;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using VContainer;
using UnityCamera = UnityEngine.Camera;

namespace _Project.Develop.Gameplay.Player
{
    [RequireComponent(typeof(NavMeshAgent), typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour, IPlayerController
    {
        [SerializeField] private ParticleSystem moveTargetParticles;
        
        private NavMeshAgent _agent;
        private NavMeshPath _path;
        private Collider2D _bodyCollider;
        private NpcInteract _pendingNpc;
        
        private PlayerStats _stats;
        private InputController _input;
        private DialogueRunner _dialogue;
        private PlayerAbility _ability;
        private UnityCamera _camera;
        private Vector2 _moveStartPosition;
        private bool _moveStartedDuringChoice;
        private NextDayBuffService _buffService;
        public Vector2 Velocity => _agent.velocity;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            
            _agent.updateRotation = false;
            _agent.updateUpAxis = false;
            
            _path = new NavMeshPath();
        }

        [Inject]
        private void Construct(InputController input, DialogueRunner dialogue, PlayerStats stats, NextDayBuffService buffService)
        {
            _input = input;
            _dialogue = dialogue;
            _stats = stats;
            _buffService = buffService;
        }

        private void ApplySpeedBuff()
        {
            if (_agent == null || _stats == null) return;
            _agent.speed = _stats.speed;
            if (_buffService != null && _buffService.LemonBalmActiveThisDay)
                _agent.speed *= NextDayBuffService.MovementSpeedMultiplier;
        }

        private void Start()
        {
            _ability = GetComponent<PlayerAbility>();
            ApplySpeedBuff();
            _agent.acceleration = _stats.acceleration;
            _agent.stoppingDistance = _stats.stoppingDistance;
            _bodyCollider = GetComponent<Collider2D>();
            _camera = UnityCamera.main;

            _input.Actions.Player.MoveTo.started += OnMoveStarted;
            _input.Actions.Player.MoveTo.performed += OnMoveTo;
        }

        private void OnMoveStarted(InputAction.CallbackContext _)
        {
            _moveStartPosition = _input.Actions.Player.MousePosition.ReadValue<Vector2>();
            _moveStartedDuringChoice = _dialogue.IsChoosing;
        }

        private void OnMoveTo(InputAction.CallbackContext _)
        {
            if (_moveStartedDuringChoice)
                return;

            var mousePosition = _input.Actions.Player.MousePosition.ReadValue<Vector2>();
            if (Vector2.Distance(_moveStartPosition, mousePosition) >= _stats.startMovePixels)
                return;

            MoveToAsync(mousePosition).Forget();
        }
        
        private async UniTask MoveToAsync(Vector2 mousePosition)
        {
            await UniTask.Yield(PlayerLoopTiming.LastUpdate, destroyCancellationToken);

            if (_input.CutsceneActive || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
                return;
            
            if (!_camera.pixelRect.Contains(mousePosition))
                return;

            var ray = _camera.ScreenPointToRay(mousePosition);
            var hits = Physics2D.GetRayIntersectionAll(ray, Mathf.Infinity);
            foreach (var hit in hits)
            {
                if (hit.collider == null)
                    continue;

                var clue = hit.collider.GetComponentInParent<IWorldClue>();
                if (clue != null && !clue.IsCollected && clue.CanCollect(_ability.ActiveSkill))
                    return;
            }

            var plane = new Plane(Vector3.forward, transform.position);
            
            if (!plane.Raycast(ray, out var distance))
                return;

            _pendingNpc = null;
            var target = ray.GetPoint(distance);
            TrySetMoveTarget(target);
        }

        private bool TrySetMoveTarget(Vector3 destination)
        {
            if (!NavMesh.SamplePosition(destination, out var hit, 0.6f, _agent.areaMask) ||
                !_agent.CalculatePath(hit.position, _path) ||
                _path.status != NavMeshPathStatus.PathComplete || !_agent.SetPath(_path))
                return false;

            moveTargetParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            moveTargetParticles.transform.position = hit.position;
            moveTargetParticles.Play();
            return true;
        }


        internal void ApproachNpc(NpcInteract npc)
        {
            if (_dialogue.IsRunning)
                return;

            var zone = npc.GetComponentInChildren<NpcDialogueExit>();
            _pendingNpc = npc;
            if (zone.Contains(_bodyCollider))
            {
                TryStartNpcDialogue(npc);
                return;
            }

            var bounds = zone.Bounds;
            var inset = Mathf.Max(.5f, _stats.stoppingDistance * 2f);
            var target = bounds.ClosestPoint(transform.position);
            target = Vector3.MoveTowards(target, bounds.center, inset);
            if (!TrySetMoveTarget(target))
                _pendingNpc = null;
        }

        internal void TryStartNpcDialogue(NpcInteract npc)
        {
            if (_pendingNpc != npc)
                return;

            _pendingNpc = null;
            if (_input.CutsceneActive)
                return;

            _agent.ResetPath();
            npc.Interact();
        }

        private void Update()
        {
            if (_input.CutsceneActive)
            {
                _pendingNpc = null;
                if (_agent.hasPath)
                    _agent.ResetPath();
                return;
            }

            if (!Mathf.Approximately(Velocity.x, 0f))
            {
                var scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * -Mathf.Sign(Velocity.x);
                transform.localScale = scale;
            }
        }

        private void OnDisable()
        {
            _pendingNpc = null;
            if (_agent.isOnNavMesh)
                _agent.ResetPath();
        }

        private void OnDestroy()
        {
            _input.Actions.Player.MoveTo.started -= OnMoveStarted;
            _input.Actions.Player.MoveTo.performed -= OnMoveTo;
        }
    }
    
    public interface IPlayerController
    {
        Vector2 Velocity { get; }
    }
}
