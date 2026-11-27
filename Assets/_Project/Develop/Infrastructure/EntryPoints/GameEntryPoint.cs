using System;
using _Project.Develop.UI;
using _Project.Develop.Gameplay.Timeline;
using UnityEngine;
using VContainer.Unity;

namespace _Project.Develop.Infrastructure.EntryPoints
{
    public class GameEntryPoint : IStartable, IDisposable
    {
        private readonly CheckpointService _checkpoints;
        private readonly DayService _day;

        private readonly CheckpointIndicator _indicator;

        public GameEntryPoint(CheckpointService checkpoints, DayService day, CheckpointIndicator indicator)
        {
            _checkpoints = checkpoints;
            _day = day;
            _indicator = indicator;
        }

        public void Start()
        {
            _checkpoints.SavingChanged += _indicator.SetSaving;
            _checkpoints.Restore();
            _day.NextDayStarted += Save;
        }

        private async void Save()
        {
            try { await _checkpoints.SaveAsync(); }
            catch (Exception error) { Debug.LogException(error); }
        }

        public void Dispose()
        {
            _day.NextDayStarted -= Save;
            _checkpoints.SavingChanged -= _indicator.SetSaving;
        }
    }
}
