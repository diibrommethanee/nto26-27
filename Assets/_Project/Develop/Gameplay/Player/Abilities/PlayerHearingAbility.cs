using MessagePipe;

namespace _Project.Develop.Gameplay.Player.Abilities
{
    public readonly struct HearingChanged
    {
        public bool IsActive { get; }

        public HearingChanged(bool isActive)
        {
            IsActive = isActive;
        }
    }

    public class PlayerHearingAbility
    {
        private readonly IBufferedPublisher<HearingChanged> _visibility;

        public PlayerHearingAbility(IBufferedPublisher<HearingChanged> visibility)
        {
            _visibility = visibility;
        }

        public void SetActive(bool active) => _visibility.Publish(new HearingChanged(active));
    }
}
