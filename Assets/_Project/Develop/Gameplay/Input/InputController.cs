using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace _Project.Develop.Gameplay.Input
{
    public class InputController : IStartable, ITickable, IDisposable
    {
        public PlayerInputActions Actions { get; } = new();
        private float _modePressedAt = float.PositiveInfinity;
        
        public void Start()
        {
            Actions.Enable();
            Actions.Camera.Zoom.Disable();
            
            Actions.Camera.Mode.started += OnZoomPressed;
            Actions.Camera.Mode.canceled += OnZoomReleased;
        }
        
        private void OnZoomPressed(InputAction.CallbackContext _)
        {
            _modePressedAt = Time.unscaledTime;
            Actions.Player.CameraPan.Disable();
        }

        public void Tick()
        {
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
        }

        private void OnZoomReleased(InputAction.CallbackContext _)
        {
            _modePressedAt = float.PositiveInfinity;
            Actions.Camera.Zoom.Disable();
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