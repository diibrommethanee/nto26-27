namespace _Project.Develop.Gameplay.Timeline
{
    public readonly struct DayPhaseChanged
    {
        public DayPhase Phase { get; }

        public DayPhaseChanged(DayPhase phase)
        {
            Phase = phase;
        }
    }
}
