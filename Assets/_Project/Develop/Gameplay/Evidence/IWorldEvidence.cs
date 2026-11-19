namespace _Project.Develop.Gameplay.Evidence
{
    public interface IWorldEvidence
    {
        bool IsCollected { get; }
        float HoldDuration { get; }
        void SetHoldProgress(float progress);
        void Collect(EvidenceService evidenceService);
    }
}