using System;

namespace _Project.Develop.Gameplay.Input
{
    public class InputController : IDisposable
    {
        public PlayerInputActions Actions { get; } = new();

        public void Enable()
        {
            Actions.Enable();
        }

        public void Dispose()
        {
            Actions.Dispose();
        }
    }
}