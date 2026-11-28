using _Project.Develop.Data.Timeline;
using System;
using _Project.Develop.Gameplay.Tea;
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

    public class DayService : _Project.Develop.Data.ISavable, _Project.Develop.Data.IAfterRestore, IInitializable, IDisposable
    {
        private readonly DaySettings _settings;

        private readonly ISubscriber<ForceDayPhase> _forcePhase;
        private readonly IBufferedPublisher<DayPhaseChanged> _phaseChanged;
        private readonly NextDayBuffService _buffService;
        private IDisposable _forcePhaseSubscription;

        public DayService(ISubscriber<ForceDayPhase> forcePhase, IBufferedPublisher<DayPhaseChanged> phaseChanged,
            NextDayBuffService buffService, DaySettings settings)
        {
            _forcePhase = forcePhase;
            _phaseChanged = phaseChanged;
            _buffService = buffService;
            _settings = settings;
            RemainingMinutes = settings.startingMinutes;
        }

        public void Initialize()
        {
            _buffService.ApplyPendingForNewDay();
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
            if (message.Phase is DayPhase.Daytime or DayPhase.Evening)
                AdvanceTo(message.Phase);
        }

        [_Project.Develop.Data.Save] public int DayNumber { get; private set; } = 1;
        [_Project.Develop.Data.Save] public DayPhase Phase { get; private set; } = DayPhase.Morning;
        [_Project.Develop.Data.Save] public int RemainingMinutes { get; private set; }

        public void AfterRestore() => _phaseChanged.Publish(new DayPhaseChanged(Phase));

        public bool TrySpendDialogueTime() =>
            TrySpendMinutes(_buffService.DialoguePointCostOverride ?? _settings.dialogueCostMinutes);

        public bool TrySpendAbilityTime() => TrySpendMinutes(_settings.abilityCostMinutes);

        private bool TrySpendMinutes(int minutes)
        {
            if (minutes <= 0 || minutes > RemainingMinutes || Phase == DayPhase.Ending)
                return false;

            RemainingMinutes -= minutes;
            UpdatePhaseFromTimeSpent();
            return true;
        }

        private void UpdatePhaseFromTimeSpent()
        {
            var spentMinutes = _settings.startingMinutes - RemainingMinutes;
            if (spentMinutes >= 2 * _settings.minutesPerPhase)
                AdvanceTo(DayPhase.Evening);
            else if (spentMinutes >= _settings.minutesPerPhase)
                AdvanceTo(DayPhase.Daytime);
        }

        private void AdvanceTo(DayPhase phase)
        {
            if (phase > Phase) SetPhase(phase);
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

        internal bool TryStartNextDay()
        {
            if (Phase != DayPhase.Ending)
                return false;

            DayNumber++;
            RemainingMinutes = _settings.startingMinutes;
            _buffService.ApplyPendingForNewDay();
            SetPhase(DayPhase.Morning);
            return true;
        }
    }
}
