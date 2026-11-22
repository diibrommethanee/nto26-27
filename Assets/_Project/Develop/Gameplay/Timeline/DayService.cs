using System;
using MessagePipe;
using VContainer.Unity;

namespace _Project.Develop.Gameplay.Timeline
{
    public enum DayPhase
    {
        Morning,
        Daytime,
        Evening,
        Ending
    }

    public class DayService : IInitializable, IDisposable
    {
        private readonly ISubscriber<ForceDayPhase> _forcePhase;
        private readonly IBufferedPublisher<DayPhaseChanged> _phaseChanged;
        private IDisposable _forcePhaseSubscription;

        public DayService(ISubscriber<ForceDayPhase> forcePhase, IBufferedPublisher<DayPhaseChanged> phaseChanged)
        {
            _forcePhase = forcePhase;
            _phaseChanged = phaseChanged;
        }

        public void Initialize()
        {
            _forcePhaseSubscription = _forcePhase.Subscribe(OnForcePhase);
            _phaseChanged.Publish(new DayPhaseChanged(Phase));
        }

        private void SetPhase(DayPhase phase)
        {
            Phase = phase;
            _phaseChanged.Publish(new DayPhaseChanged(phase));
        }

        public void Dispose() => _forcePhaseSubscription?.Dispose();

        private void OnForcePhase(ForceDayPhase message)
        {
            if ((message.Phase is DayPhase.Daytime or DayPhase.Evening) && message.Phase > Phase)
                SetPhase(message.Phase);
        }

        public int DayNumber { get; private set; } = 1;
        public DayPhase Phase { get; private set; } = DayPhase.Morning;

        public bool TryAdvancePhase()
        {
            var next = Phase switch
            {
                DayPhase.Morning => DayPhase.Daytime,
                DayPhase.Daytime => DayPhase.Evening,
                _ => Phase
            };

            if (next == Phase)
                return false;

            SetPhase(next);
            return true;
        }

        public bool TryEndDay()
        {
            if (Phase != DayPhase.Evening)
                return false;

            SetPhase(DayPhase.Ending);
            return true;
        }

        public bool TryStartNextDay()
        {
            if (Phase != DayPhase.Ending)
                return false;

            DayNumber++;
            SetPhase(DayPhase.Morning);
            return true;
        }
    }
}
