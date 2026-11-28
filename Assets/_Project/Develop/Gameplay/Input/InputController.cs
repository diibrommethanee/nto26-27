using System;
using MessagePipe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
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
        public Vector2 PointerPosition => Actions.Player.MousePosition.ReadValue<Vector2>();
        public bool IsPointerOverUI => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        public bool IsZoomMode => Actions.Camera.Zoom.enabled;

        public InputController(IPublisher<ZoomModeChanged> zoomModePublisher)
        {
            _zoomModePublisher = zoomModePublisher;
        }

        private const float ZoomHoldSeconds = 0.2f;
        private float? _modePressedAt;
        private bool _notebookOpen;
        private bool _hearingActive;


        public void Start()
        {
            Actions.Enable();
            Actions.Camera.Zoom.Disable();

            Actions.Camera.Mode.started += OnZoomPressed;
            Actions.Camera.Mode.canceled += OnZoomReleased;

        }

        public bool CutsceneActive { get; private set; }

        public void SetCutsceneActive(bool active)
        {
            CutsceneActive = active;
            RefreshCameraActions();
            RefreshPlayerActions();
        }

        public void SetHearingActive(bool active)
        {
            _hearingActive = active;
            RefreshPlayerActions();
        }

        private void RefreshCameraActions()
        {
            if (CutsceneActive)
            {
                if (IsZoomMode) ExitZoomMode();
                _modePressedAt = null;
                Actions.Camera.Disable();
            }
            else
            {
                Actions.Camera.Mode.Enable();
            }
        }

        private void OnZoomPressed(InputAction.CallbackContext _)
        {
            _modePressedAt = Time.unscaledTime;
            Actions.Player.CameraPan.Disable();
        }

        public void SetNotebookOpen(bool isOpen)
        {
            if (_notebookOpen == isOpen)
                return;

            _notebookOpen = isOpen;
            RefreshPlayerActions();
        }

        public void Tick()
        {
            if (CutsceneActive) return;
            if (!Actions.Camera.Mode.IsPressed())
            {
                if (_modePressedAt.HasValue)
                    ReleaseZoom();
                return;
            }

            if (_modePressedAt is not float pressedAt || Time.unscaledTime - pressedAt < ZoomHoldSeconds || IsZoomMode)
                return;

            Actions.Player.Disable();
            Actions.Camera.Zoom.Enable();
            _zoomModePublisher.Publish(new ZoomModeChanged(true));
        }

        private void OnZoomReleased(InputAction.CallbackContext _) => ReleaseZoom();

        private void ReleaseZoom()
        {
            _modePressedAt = null;
            if (CutsceneActive) return;
            var wasZooming = IsZoomMode;
            Actions.Camera.Zoom.Disable();
            RefreshPlayerActions();
            if (wasZooming)
                _zoomModePublisher.Publish(new ZoomModeChanged(false));
        }

        private void ExitZoomMode()
        {
            Actions.Camera.Zoom.Disable();
            _modePressedAt = null;
            _zoomModePublisher.Publish(new ZoomModeChanged(false));
        }

        private void RefreshPlayerActions()
        {
            if (CutsceneActive || _notebookOpen || IsZoomMode)
            {
                Actions.Player.Disable();
                return;
            }

            Actions.Player.Enable();
            if (_hearingActive || _modePressedAt.HasValue)
                Actions.Player.CameraPan.Disable();
        }

        public void Dispose()
        {
            Actions.Camera.Mode.started -= OnZoomPressed;
            Actions.Camera.Mode.canceled -= OnZoomReleased;

            Actions.Disable();
            Actions.Dispose();
        }
    }
}
