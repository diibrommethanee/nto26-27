using System;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace _Project.Develop.Gameplay.Input
{
    public class InputController : IStartable, IDisposable
    {
        public PlayerInputActions Actions { get; } = new();
        
        public void Start()
        {
            Actions.Enable();
            Actions.Camera.Zoom.Disable();
            
            Actions.Camera.Mode.performed += OnZoomPressed;
            Actions.Camera.Mode.canceled += OnZoomReleased;
        }
        
        private void OnZoomPressed(InputAction.CallbackContext _)
        {
            Actions.Player.Disable();
            Actions.Camera.Zoom.Enable();
        }

        private void OnZoomReleased(InputAction.CallbackContext _)
        {
            Actions.Camera.Zoom.Disable();
            Actions.Player.Enable();
        }

        public void Dispose()
        {
            Actions.Camera.Mode.performed -= OnZoomPressed;
            Actions.Camera.Mode.canceled -= OnZoomReleased;

            Actions.Disable();
            Actions.Dispose();
        }
    }
}