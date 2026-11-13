using _Project.Develop.Data.Camera;
using _Project.Develop.Gameplay.Input;
using NaughtyAttributes;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using VContainer;
using UnityCamera = UnityEngine.Camera;

namespace _Project.Develop.Gameplay.Camera
{
    [RequireComponent(typeof(CinemachineCamera))]
    [RequireComponent(typeof(CinemachineFollow))]
    public class CameraController : MonoBehaviour
    {
        [SerializeField, Expandable] private CameraSettings settings;
        
        private CinemachineCamera _cinemachineCamera;
        private CinemachineFollow _follow;
        
        private InputController _input;
        private UnityCamera _camera;

        private Vector3 _initialFollowOffset;
        private float _targetPanOffset;
        private float _currentPanOffset;
        private float _panVelocity;

        private float _targetZoom;
        private float _currentZoom;
        private float _zoomVelocity;

        [Inject]
        private void Construct(InputController input)
        {
            _input = input;
        }

        private void Start()
        {
            _cinemachineCamera = GetComponent<CinemachineCamera>();
            _follow = GetComponent<CinemachineFollow>();

            _camera = UnityCamera.main;

            _initialFollowOffset = _follow.FollowOffset;

            _currentZoom = _cinemachineCamera.Lens.OrthographicSize;
            _targetZoom = _currentZoom;
            
            _input.Actions.Player.CameraPan.performed += OnCameraPan;
            _input.Actions.Camera.Zoom.performed += OnZoom;
        }
        
        private void OnCameraPan(InputAction.CallbackContext ctx)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
            
            _targetPanOffset = Mathf.Clamp(_targetPanOffset + ctx.ReadValue<Vector2>().y * settings.panSensitivity, -settings.maxPanOffset, settings.maxPanOffset);
        }

        private void OnZoom(InputAction.CallbackContext ctx)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var nextZoom = Mathf.Clamp(_targetZoom - ctx.ReadValue<Vector2>().y * settings.zoomSensitivity, settings.minZoom, settings.maxZoom);

            var mouseX = _camera.ScreenToViewportPoint(Mouse.current.position.ReadValue()).x;
            var horizontalOffset = (mouseX - 0.5f) * 2f;

            _targetPanOffset = Mathf.Clamp(_targetPanOffset + horizontalOffset * _camera.aspect * (_targetZoom - nextZoom), -settings.maxPanOffset, settings.maxPanOffset);
            _targetZoom = nextZoom;
        }
        
        private void Update()
        {
            _currentPanOffset = Mathf.SmoothDamp(_currentPanOffset, _targetPanOffset, ref _panVelocity, settings.panSmoothTime);
            _follow.FollowOffset = _initialFollowOffset + Vector3.right * _currentPanOffset;

            _currentZoom = Mathf.SmoothDamp(_currentZoom, _targetZoom, ref _zoomVelocity, settings.zoomSmoothTime);
            _cinemachineCamera.Lens.OrthographicSize = _currentZoom;
        }
        
        private void OnDestroy()
        {
            _input.Actions.Player.CameraPan.performed -= OnCameraPan;
            _input.Actions.Camera.Zoom.performed -= OnZoom;
        }
    }
}