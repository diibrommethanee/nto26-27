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
        public const int StartingMinutes = 480;
        public const int MinutesPerPhase = 120;
        public const int DialogueCostMinutes = 30;
        public const int AbilityCostMinutes = 15;

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
        public int RemainingMinutes { get; private set; } = StartingMinutes;

        public bool TrySpendMinutes(int minutes)
        {
            if (minutes <= 0 || minutes > RemainingMinutes || Phase == DayPhase.Ending)
                return false;

            RemainingMinutes -= minutes;
            var spentMinutes = StartingMinutes - RemainingMinutes;
            if (spentMinutes >= MinutesPerPhase * 2 && Phase < DayPhase.Evening)
                SetPhase(DayPhase.Evening);
            else if (spentMinutes >= MinutesPerPhase && Phase < DayPhase.Daytime)
                SetPhase(DayPhase.Daytime);

            return true;
        }

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
            RemainingMinutes = StartingMinutes;
            SetPhase(DayPhase.Morning);
            return true;
        }
    }
}
