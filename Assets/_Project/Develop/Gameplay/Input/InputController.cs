using System;
using MessagePipe;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace _Project.Develop.Gameplay.Input
{
    public enum SkillType
    {
        Smell,
        Hearing
    }

    public class InputController : IStartable, ITickable, IDisposable
    {
        public PlayerInputActions Actions { get; } = new();
        private readonly IPublisher<ZoomModeChanged> _zoomModePublisher;
        public bool IsZoomMode => Actions.Camera.Zoom.enabled;

        public InputController(IPublisher<ZoomModeChanged> zoomModePublisher)
        {
            _zoomModePublisher = zoomModePublisher;
        }

        private float _modePressedAt = float.PositiveInfinity;
        private Vector2 _modeStartPosition;
        private bool _modeMoved;


        public void Start()
        {
            Actions.Enable();
            Actions.Camera.Zoom.Disable();

            Actions.Camera.Mode.started += OnZoomPressed;
            Actions.Camera.Mode.canceled += OnZoomReleased;
            Actions.Player.MousePosition.performed += OnMousePositionChanged;

        }

        private void OnZoomPressed(InputAction.CallbackContext _)
        {
            _modePressedAt = Time.unscaledTime;
            _modeStartPosition = Actions.Player.MousePosition.ReadValue<Vector2>();
            _modeMoved = false;
            Actions.Player.CameraPan.Disable();
        }

        private void OnMousePositionChanged(InputAction.CallbackContext context)
        {
            if (!Actions.Camera.Mode.IsPressed())
                return;

            if (Vector2.Distance(_modeStartPosition, context.ReadValue<Vector2>()) < 12f)
                return;

            _modeMoved = true;
            if (IsZoomMode)
                ExitZoomMode();
        }

        public void Tick()
        {
            if (!Actions.Camera.Mode.IsPressed())
            {
                if (!float.IsPositiveInfinity(_modePressedAt))
                    OnZoomReleased(default);
                return;
            }

            if (_modeMoved)
                return;

            if (Time.unscaledTime - _modePressedAt < 0.2f || Actions.Camera.Zoom.enabled)
                return;

            Actions.Player.Disable();
            Actions.Player.MousePosition.Enable();
            Actions.Camera.Zoom.Enable();
            _zoomModePublisher.Publish(new ZoomModeChanged(true));
        }

        private void OnZoomReleased(InputAction.CallbackContext _)
        {
            _modePressedAt = float.PositiveInfinity;
            _modeMoved = false;
            var wasZooming = IsZoomMode;
            Actions.Camera.Zoom.Disable();
            Actions.Player.Enable();
            if (wasZooming)
                _zoomModePublisher.Publish(new ZoomModeChanged(false));
        }

        private void ExitZoomMode()
        {
            Actions.Camera.Zoom.Disable();
            Actions.Player.Enable();
            _modePressedAt = float.PositiveInfinity;
            _zoomModePublisher.Publish(new ZoomModeChanged(false));
        }

        public void Dispose()
        {
            Actions.Camera.Mode.started -= OnZoomPressed;
            Actions.Camera.Mode.canceled -= OnZoomReleased;
            Actions.Player.MousePosition.performed -= OnMousePositionChanged;

            Actions.Disable();
            Actions.Dispose();
        }
    }
}
