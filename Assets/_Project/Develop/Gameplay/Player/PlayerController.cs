using _Project.Develop.Data.Player;
using _Project.Develop.Gameplay.Dialogue;
using _Project.Develop.Gameplay.Input;
using _Project.Develop.Gameplay.NPC;
using _Project.Develop.Gameplay.Player.Abilities;
using _Project.Develop.Gameplay.Tea;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using VContainer;
using UnityCamera = UnityEngine.Camera;

namespace _Project.Develop.Gameplay.Player
{
    [RequireComponent(typeof(NavMeshAgent), typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour, _Project.Develop.Data.ISavable, IPlayerController
    {
        [SerializeField] private ParticleSystem moveTargetParticles;
        
        private NavMeshAgent _agent;
        private NavMeshPath _path;
        private Collider2D _bodyCollider;
        private NpcInteract _pendingNpc;
        private bool _takingDialogueStep;
        
        private PlayerStats _stats;
        private InputController _input;
        private DialogueRunner _dialogue;
        private PlayerAbility _ability;
        private UnityCamera _camera;
        private Vector2 _moveStartPosition;
        private bool _moveStartedDuringChoice;
        private NextDayBuffService _buffService;
        
        [_Project.Develop.Data.Save]
        private float[] SavedPosition
        {
            get => new[] { transform.position.x, transform.position.y, transform.position.z };
            set
            {
                if (!_agent.Warp(new Vector3(value[0], value[1], value[2])))
                    throw new System.InvalidOperationException("Cannot restore player position.");
            }
        }

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

        private void Start()
        {
            _ability = GetComponent<PlayerAbility>();
            _agent.speed = _buffService.MovementSpeed(_stats.speed);
            _agent.acceleration = _stats.acceleration;
            _agent.stoppingDistance = _stats.stoppingDistance;
            _bodyCollider = GetComponent<Collider2D>();
            _camera = UnityCamera.main;

            _input.Actions.Player.MoveTo.started += OnMoveStarted;
            _input.Actions.Player.MoveTo.performed += OnMoveTo;
        }

        private void OnMoveStarted(InputAction.CallbackContext _)
        {
            _moveStartPosition = _input.PointerPosition;
            _moveStartedDuringChoice = _dialogue.IsChoosing;
        }

        private void OnMoveTo(InputAction.CallbackContext _)
        {
            if (_moveStartedDuringChoice)
                return;

            var mousePosition = _input.PointerPosition;
            if (Vector2.Distance(_moveStartPosition, mousePosition) >= _stats.startMovePixels)
                return;

            MoveToAsync(mousePosition).Forget();
        }
        
        private async UniTask MoveToAsync(Vector2 mousePosition)
        {
            await UniTask.Yield(PlayerLoopTiming.LastUpdate, destroyCancellationToken);

            if (_input.CutsceneActive || _input.IsPointerOverUI)
                return;
            
            if (!_camera.pixelRect.Contains(mousePosition))
                return;

            var ray = _camera.ScreenPointToRay(mousePosition);
            if (HitsInteractionTarget(ray)) return;

            var plane = new Plane(Vector3.forward, transform.position);

            if (!plane.Raycast(ray, out var distance))
                return;

            _pendingNpc = null;
            var target = ray.GetPoint(distance);
            TrySetMoveTarget(target);
        }

        private bool HitsInteractionTarget(Ray ray)
        {
            var hits = Physics2D.GetRayIntersectionAll(ray, Mathf.Infinity);
            foreach (var hit in hits)
            {
                if (hit.collider == null)
                    continue;

                if (hit.collider.GetComponentInParent<IInteractable>() != null)
                    return true;

                if (_ability.CanCollectClueAt(hit.collider))
                    return true;
            }

            return false;
        }

        private bool TrySetMoveTarget(Vector3 destination, bool showMarker = true)
        {
            if (!NavMesh.SamplePosition(destination, out var hit, 0.6f, _agent.areaMask) ||
                !_agent.CalculatePath(hit.position, _path) ||
                _path.status != NavMeshPathStatus.PathComplete || !_agent.SetPath(_path))
                return false;

            if (!showMarker)
                return true;

            moveTargetParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            moveTargetParticles.transform.position = hit.position;
            moveTargetParticles.Play();
            return true;
        }


        internal void ApproachNpc(NpcInteract npc)
        {
            if (_dialogue.IsRunning)
            {
                if (_dialogue.Interlocutor == npc.Data)
                    _dialogue.RequestContinue();
                return;
            }

            var zone = npc.DialogueZone;
            _pendingNpc = npc;
            _takingDialogueStep = false;
            if (zone.Contains(_bodyCollider))
            {
                _takingDialogueStep = TryStepTowardsNpc(npc);
                if (!_takingDialogueStep) TryStartNpcDialogue(npc);
                return;
            }

            var target = zone.GetApproachPoint(transform.position, _stats.stoppingDistance);
            if (!TrySetMoveTarget(target))
                _pendingNpc = null;
        }

        private bool TryStepTowardsNpc(NpcInteract npc)
        {
            var towardsNpc = npc.transform.position.x - transform.position.x;
            if (Mathf.Abs(towardsNpc) <= 0.01f || towardsNpc * transform.localScale.x <= 0f)
                return false;

            var step = Mathf.Max(0.35f, _agent.stoppingDistance * 2f);
            var destination = transform.position + Vector3.right * Mathf.Sign(towardsNpc) * step;
            if (TrySetMoveTarget(destination, showMarker: false)) return true;

            FaceDirection(towardsNpc);
            return false;
        }

        internal void TryStartNpcDialogue(NpcInteract npc)
        {
            if (_pendingNpc != npc || _takingDialogueStep)
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
                FaceDirection(Velocity.x);

            FinishApproachWhenStopped();
        }

        private void FinishApproachWhenStopped()
        {
            if (!_takingDialogueStep || !_pendingNpc || _agent.pathPending) return;
            if (_agent.hasPath && _agent.remainingDistance > _agent.stoppingDistance) return;

            var npc = _pendingNpc;
            _takingDialogueStep = false;
            if (!npc.DialogueZone.Contains(_bodyCollider))
            {
                _pendingNpc = null;
                return;
            }

            var towardsNpc = npc.transform.position.x - transform.position.x;
            if (Mathf.Abs(towardsNpc) > 0.01f)
                FaceDirection(towardsNpc);
            TryStartNpcDialogue(npc);
        }

        private void FaceDirection(float horizontalDirection)
        {
            var scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * -Mathf.Sign(horizontalDirection);
            transform.localScale = scale;
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
