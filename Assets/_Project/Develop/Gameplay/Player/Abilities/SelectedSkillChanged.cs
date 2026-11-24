using _Project.Develop.Gameplay.Input;

namespace _Project.Develop.Gameplay.Player.Abilities
{
    public readonly struct SelectedSkillChanged
    {
        public SkillType Skill { get; }

        public bool IsActive { get; }

        public SelectedSkillChanged(SkillType skill, bool isActive = false)
        {
            Skill = skill;
            IsActive = isActive;
        }
    }
}
