using _Project.Develop.Gameplay.Input;

namespace _Project.Develop.Gameplay.Evidence
{
    public interface IWorldEvidence
    {
        bool IsCollected { get; }
        float HoldDuration { get; }
        bool CanCollect(SkillType? activeSkill);
        void SetHoldProgress(float progress);
        void Collect(EvidenceService evidenceService);
    }
}
