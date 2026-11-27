using _Project.Develop.Gameplay.Input;
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

    public class PlayerHearingAbility : IPlayerAbility
    {
        public SkillType Skill => SkillType.Hearing;

        private readonly IBufferedPublisher<HearingChanged> _visibility;

        private readonly InputController _input;

        public PlayerHearingAbility(IBufferedPublisher<HearingChanged> visibility, InputController input)
        {
            _visibility = visibility;
            _input = input;
        }

        public void SetActive(bool active)
        {
            _input.SetHearingActive(active);
            _visibility.Publish(new HearingChanged(active));
        }
    }
}
