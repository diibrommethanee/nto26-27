using _Project.Develop.Data.Camera;
using _Project.Develop.Gameplay.Input;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using UnityCamera = UnityEngine.Camera;

namespace _Project.Develop.Gameplay.Camera
{
    [RequireComponent(typeof(CinemachineCamera))]
    [RequireComponent(typeof(CinemachineFollow))]
    [RequireComponent(typeof(CinemachineConfiner2D))]
    public class CameraController : MonoBehaviour
    {
        [SerializeField, Expandable] private CameraSettings settings;
        
        private CinemachineCamera _cinemachineCamera;
        private CinemachineFollow _follow;
        private CinemachineConfiner2D _confiner;
        
        [SerializeField] private DialogueCameraView dialogue = new();
        public DialogueCameraView Dialogue => dialogue;

        private InputController _input;
        private UnityCamera _camera;

        private Vector3 _initialFollowOffset;
        private float _targetPanOffset;
        private float _currentPanOffset;
        private float _panVelocity;

        private float _targetZoom;
        private float _currentZoom;
        private float _zoomVelocity;
        
        public bool IsZoomMode => _input != null && _input.IsZoomMode;
        public float ScrollPulse { get; private set; }
        public float ScrollDirection { get; private set; }

        [Inject]
        private void Construct(InputController input)
        {
            _input = input;
        }

        private void Start()
        {
            _cinemachineCamera = GetComponent<CinemachineCamera>();
            _follow = GetComponent<CinemachineFollow>();
            _confiner = GetComponent<CinemachineConfiner2D>();

            _camera = UnityCamera.main;

            _initialFollowOffset = _follow.FollowOffset;

            _currentZoom = _cinemachineCamera.Lens.OrthographicSize;
            _targetZoom = _currentZoom;
            
            _input.Actions.Player.CameraPan.performed += OnCameraPan;
            _input.Actions.Camera.Zoom.performed += OnZoom;
        }
        
        private void OnCameraPan(InputAction.CallbackContext ctx)
        {
            CameraPanAsync(ctx.ReadValue<Vector2>().y).Forget();
        }

        private async UniTask CameraPanAsync(float scroll)
        {
            await UniTask.Yield(PlayerLoopTiming.LastUpdate, destroyCancellationToken);
            
            if (!_input.Actions.Player.CameraPan.enabled || _input.Actions.Camera.Mode.IsPressed()) return;
            if (_input.IsPointerOverUI)
                return;
            
            Pulse(scroll);
            _targetPanOffset += scroll * settings.panSensitivity;
        }

        private void OnZoom(InputAction.CallbackContext ctx)
        {
            ZoomAsync(ctx.ReadValue<Vector2>().y).Forget();
        }

        private async UniTask ZoomAsync(float scroll)
        {
            await UniTask.Yield(PlayerLoopTiming.LastUpdate, destroyCancellationToken);
            
            if (_input.IsPointerOverUI)
                return;

            if (!_input.Actions.Camera.Zoom.enabled) return;
            var nextZoom = Mathf.Clamp(_targetZoom - scroll * settings.zoomSensitivity, settings.minZoom, settings.maxZoom);

            var mouseX = _camera.ScreenToViewportPoint(Mouse.current.position.ReadValue()).x;
            var horizontalOffset = (mouseX - 0.5f) * 2f;

            _targetPanOffset += horizontalOffset * _camera.aspect * (_targetZoom - nextZoom);
            if (!Mathf.Approximately(_targetZoom, nextZoom)) Pulse(scroll);
            _targetZoom = nextZoom;
        }
        
        private void Pulse(float scroll)
        {
            if (Mathf.Approximately(scroll, 0f)) return;
            ScrollPulse = 1f;
            ScrollDirection = Mathf.Sign(scroll);
        }

        private void Update()
        {
            DiscardPanBeyondConfiner();
            _currentPanOffset = Mathf.SmoothDamp(_currentPanOffset, _targetPanOffset, ref _panVelocity, settings.panSmoothTime);
            _currentZoom = Mathf.SmoothDamp(_currentZoom, _targetZoom, ref _zoomVelocity, settings.zoomSmoothTime);
            ScrollPulse = Mathf.MoveTowards(ScrollPulse, 0f, Time.unscaledDeltaTime * 3.5f);

            var offset = _initialFollowOffset + Vector3.right * _currentPanOffset;
            var zoom = _currentZoom;
            dialogue.Blend(_cinemachineCamera.Follow.position, _camera.aspect, ref offset, ref zoom);
            _follow.FollowOffset = offset;
            ApplyZoom(zoom);
        }

        private void ApplyZoom(float zoom)
        {
            if (_cinemachineCamera.Lens.OrthographicSize == zoom)
                return;

            _cinemachineCamera.Lens.OrthographicSize = zoom;
            _confiner.InvalidateLensCache();
        }

        private void DiscardPanBeyondConfiner()
        {
            var state = _cinemachineCamera.State;
            var correction = state.PositionCorrection.x;
            if (!_confiner.CameraWasDisplaced(_cinemachineCamera) || Mathf.Abs(correction) < 0.01f)
                return;

            var direction = -Mathf.Sign(correction);
            var edgeOffset = state.RawPosition.x + correction - _cinemachineCamera.Follow.position.x - _initialFollowOffset.x;
            var edgeDistance = Mathf.Max(0f, edgeOffset * direction);
            _targetPanOffset = Mathf.Min(_targetPanOffset * direction, edgeDistance) * direction;
            _currentPanOffset = Mathf.Min(_currentPanOffset * direction, edgeDistance) * direction;
            _panVelocity = Mathf.Min(_panVelocity * direction, 0f) * direction;
        }
        
        private void OnDestroy()
        {
            _input.Actions.Player.CameraPan.performed -= OnCameraPan;
            _input.Actions.Camera.Zoom.performed -= OnZoom;
        }
    }
}
