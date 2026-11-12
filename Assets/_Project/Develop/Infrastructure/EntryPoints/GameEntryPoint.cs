using _Project.Develop.Gameplay.Input;
using VContainer.Unity;

namespace _Project.Develop.Infrastructure.EntryPoints
{
    public class GameEntryPoint : IStartable
    {
        private readonly InputController _input;

        public GameEntryPoint(InputController input)
        {
            _input = input;
        }

        public void Start()
        {
            _input.Enable();
        }
    }
}