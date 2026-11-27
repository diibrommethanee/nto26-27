using System;
using _Project.Develop.UI;
using VContainer.Unity;

namespace _Project.Develop.Infrastructure.EntryPoints
{
    public class GameEntryPoint : IStartable, IDisposable
    {
        private readonly CheckpointService _checkpoints;
        private readonly CheckpointIndicator _indicator;

        public GameEntryPoint(CheckpointService checkpoints, CheckpointIndicator indicator)
        {
            _checkpoints = checkpoints;
            _indicator = indicator;
        }

        public void Start()
        {
            _checkpoints.SavingChanged += _indicator.SetSaving;
            _checkpoints.Restore();
        }

        public void Dispose()
        {
            _checkpoints.SavingChanged -= _indicator.SetSaving;
        }
    }
}
