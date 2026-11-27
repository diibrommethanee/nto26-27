namespace _Project.Develop.Gameplay.Input
{
    public readonly struct SavingVisibilityChanged
    {
        public bool IsActive { get; }

        public SavingVisibilityChanged(bool isActive)
        {
            IsActive = isActive;
        }
    }
}
