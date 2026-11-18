using System;
using System.Collections.Generic;
using _Project.Develop.Data.Evidence;

namespace _Project.Develop.Gameplay.Evidence
{
    public class EvidenceService
    {
        private readonly EvidenceDatabase _database;
        private readonly List<EvidenceData> _collected = new();

        public IReadOnlyList<EvidenceData> Collected { get; }

        public EvidenceService(EvidenceDatabase database)
        {
            _database = database;
            Collected = _collected.AsReadOnly();
        }

        public bool Collect(EvidenceData evidence)
        {
            if (evidence == null)
                throw new ArgumentNullException(nameof(evidence));

            if (_collected.Contains(evidence))
                return false;

            _collected.Add(evidence);
            return true;
        }

        public bool TryCombine(EvidenceData first, EvidenceData second, out EvidenceData result)
        {
            return _database.TryGetResult(first, second, out result) && result != null;
        }

        public bool CheckHypothesis(HypothesisData hypothesis, IReadOnlyList<EvidenceData> answers)
        {
            var index = 0;
            foreach (var node in hypothesis.nodes)
            {
                if (node.type != HypothesisNodeType.Fill)
                    continue;

                if (index >= answers.Count || !IsCorrectAnswer(node, answers[index], index))
                    return false;

                index++;
            }

            return index > 0 && index == answers.Count;
        }

        private bool IsCorrectAnswer(HypothesisNode node, EvidenceData answer, int index)
        {
            if (answer == null || answer.fillIndex != index || string.IsNullOrWhiteSpace(answer.fillWord) ||
                !_collected.Contains(answer))
                return false;

            return node.correctEvidence != null
                ? answer == node.correctEvidence
                : string.Equals(answer.fillWord, node.correctAnswerFill, StringComparison.OrdinalIgnoreCase);
        }
    }
}
