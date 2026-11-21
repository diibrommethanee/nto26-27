namespace _Project.Develop.Gameplay.Timeline
{
    public readonly struct ForceDayPhase
    {
        public DayPhase Phase { get; }

        public ForceDayPhase(DayPhase phase)
        {
            Phase = phase;
        }
    }
}
