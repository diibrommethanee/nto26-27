using _Project.Develop.Gameplay.Input;

namespace _Project.Develop.Gameplay.Player.Abilities
{
    public readonly struct SelectedSkillChanged
    {
        public SkillType Skill { get; }

        public SelectedSkillChanged(SkillType skill)
        {
            Skill = skill;
        }
    }
}
