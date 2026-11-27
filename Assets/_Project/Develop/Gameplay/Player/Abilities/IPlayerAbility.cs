using _Project.Develop.Gameplay.Input;

namespace _Project.Develop.Gameplay.Player.Abilities
{
    public interface IPlayerAbility
    {
        SkillType Skill { get; }
        void SetActive(bool active);
    }
}
