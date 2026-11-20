namespace _Project.Develop.Gameplay.Input
{
    public readonly struct ZoomModeChanged
    {
        public bool IsActive { get; }

        public ZoomModeChanged(bool isActive)
        {
            IsActive = isActive;
        }
    }
}
