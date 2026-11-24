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
        private bool _notebookOpen;


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
            if (active)
            {
                if (IsZoomMode) ExitZoomMode();
                _modePressedAt = float.PositiveInfinity;
                Actions.Camera.Disable();
                Actions.Player.Disable();
            }
            else
            {
                Actions.Player.Enable();
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
                if (!float.IsPositiveInfinity(_modePressedAt))
                    OnZoomReleased(default);
                return;
            }

            if (Time.unscaledTime - _modePressedAt < 0.2f || Actions.Camera.Zoom.enabled)
                return;

            Actions.Player.Disable();
            Actions.Camera.Zoom.Enable();
            _zoomModePublisher.Publish(new ZoomModeChanged(true));
        }

        private void OnZoomReleased(InputAction.CallbackContext _)
        {
            _modePressedAt = float.PositiveInfinity;
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
            RefreshPlayerActions();
            _modePressedAt = float.PositiveInfinity;
            _zoomModePublisher.Publish(new ZoomModeChanged(false));
        }

        private void RefreshPlayerActions()
        {
            if (_notebookOpen || IsZoomMode)
                Actions.Player.Disable();
            else
                Actions.Player.Enable();
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
