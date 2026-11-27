using _Project.Develop.Gameplay.Input;
using MessagePipe;

namespace _Project.Develop.Gameplay.Player.Abilities
{
    public readonly struct SmellVisibilityChanged
    {
        public bool IsActive { get; }

        public SmellVisibilityChanged(bool isActive)
        {
            IsActive = isActive;
        }
    }

    public class PlayerSmellAbility : IPlayerAbility
    {
        public SkillType Skill => SkillType.Smell;

        private readonly IBufferedPublisher<SmellVisibilityChanged> _visibility;

        public PlayerSmellAbility(IBufferedPublisher<SmellVisibilityChanged> visibility)
        {
            _visibility = visibility;
        }

        public void SetActive(bool active) => _visibility.Publish(new SmellVisibilityChanged(active));
    }
}
