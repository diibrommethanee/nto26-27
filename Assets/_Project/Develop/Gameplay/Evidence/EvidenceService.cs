using System;
using System.Linq;
using _Project.Develop.Data;
using System.Collections.Generic;
using _Project.Develop.Data.Evidence;
using UnityEngine;

namespace _Project.Develop.Gameplay.Evidence
{
    public class EvidenceService : ISavable, IAfterRestore
    {
        private readonly EvidenceDatabase _database;
        private readonly List<Clue> _collected = new();
        private readonly Dictionary<Clue, Sprite> _collectedSprites = new();
        private readonly List<Hypothesis> _unlockedHypotheses = new();
        private readonly Dictionary<Hypothesis, IReadOnlyList<Clue>> _solvedHypotheses = new();

        public IReadOnlyList<Clue> Collected { get; }
        public IReadOnlyList<Hypothesis> UnlockedHypotheses { get; }
        public event Action Restored;
        public void AfterRestore() => Restored?.Invoke();
        public event Action<Hypothesis> HypothesisUnlocked;
        public event Action<Hypothesis> HypothesisPassed;
        public event Action<Clue, Vector3?, Vector2?> ClueCollected;

        [Save] private string[] Clues
        {
            get => _collected.Select(CheckpointCatalog.Instance.Key).ToArray();
            set { _collected.Clear(); _collected.AddRange(value.Select(CheckpointCatalog.Instance.Resolve<Clue>)); }
        }

        [Save] private string[] Hypotheses
        {
            get => _unlockedHypotheses.Select(CheckpointCatalog.Instance.Key).ToArray();
            set { _unlockedHypotheses.Clear(); _unlockedHypotheses.AddRange(value.Select(CheckpointCatalog.Instance.Resolve<Hypothesis>)); }
        }

        [Save] private Dictionary<string, string[]> Answers
        {
            get => _solvedHypotheses.ToDictionary(pair => CheckpointCatalog.Instance.Key(pair.Key),
                pair => pair.Value.Select(CheckpointCatalog.Instance.Key).ToArray());
            set
            {
                _solvedHypotheses.Clear();
                foreach (var pair in value)
                    _solvedHypotheses.Add(CheckpointCatalog.Instance.Resolve<Hypothesis>(pair.Key),
                        pair.Value.Select(CheckpointCatalog.Instance.Resolve<Clue>).ToArray());
            }
        }

        public EvidenceService(EvidenceDatabase database)
        {
            _database = database;
            Collected = _collected.AsReadOnly();
            UnlockedHypotheses = _unlockedHypotheses.AsReadOnly();
        }

        public bool Collect(Clue clue, Vector3? sourceWorldPosition = null, Vector2? boardPosition = null,
            Sprite sourceSprite = null)
        {
            if (clue == null)
                throw new ArgumentNullException(nameof(clue));

            if (_collected.Contains(clue))
                return false;

            _collected.Add(clue);
            if (sourceSprite != null)
                _collectedSprites.Add(clue, sourceSprite);
            var reward = clue.reward;
            if (reward.dialogueNpc != null && reward.dialogue != null)
                reward.dialogueNpc.state.dialogue = reward.dialogue;

            if (reward.moodNpc != null && reward.moodIncrease > 0)
                reward.moodNpc.state.mood += reward.moodIncrease;

            ClueCollected?.Invoke(clue, sourceWorldPosition, boardPosition);
            return true;
        }

        public void UnlockHypothesis(Hypothesis hypothesis)
        {
            if (_unlockedHypotheses.Contains(hypothesis))
                return;

            _unlockedHypotheses.Add(hypothesis);
            HypothesisUnlocked?.Invoke(hypothesis);
        }

        public bool TryCombine(Clue first, Clue second, out Clue result)
        {
            return _database.TryGetResult(first, second, out result) && result != null;
        }

        public bool HasClue(Clue clue) => _collected.Contains(clue);

        public Sprite GetClueSprite(Clue clue) => _collectedSprites.TryGetValue(clue, out var sprite) ? sprite : clue.icon;

        public bool HasSolvedHypothesis(Hypothesis hypothesis) => _solvedHypotheses.ContainsKey(hypothesis);

        public bool TryGetHypothesisAnswers(Hypothesis hypothesis, out IReadOnlyList<Clue> answers) =>
            _solvedHypotheses.TryGetValue(hypothesis, out answers);

        public bool CheckHypothesis(Hypothesis hypothesis, IReadOnlyList<Clue> answers)
        {
            var index = 0;
            foreach (var node in hypothesis.nodes)
            {
                if (node.type != HypothesisNodeType.Fill)
                    continue;

                if (index >= answers.Count || !IsCorrectAnswer(node, answers[index]))
                    return false;

                index++;
            }

            if (index == 0 || index != answers.Count)
                return false;

            if (!_solvedHypotheses.ContainsKey(hypothesis))
            {
                var snapshot = new Clue[answers.Count];
                for (var i = 0; i < answers.Count; i++)
                    snapshot[i] = answers[i];
                _solvedHypotheses.Add(hypothesis, Array.AsReadOnly(snapshot));
                HypothesisPassed?.Invoke(hypothesis);
            }
            return true;
        }

        private bool IsCorrectAnswer(HypothesisNode node, Clue answer)
        {
            if (answer == null || string.IsNullOrWhiteSpace(answer.fillWord) ||
                !_collected.Contains(answer))
                return false;

            return node.correctClue != null
                ? answer == node.correctClue
                : string.Equals(answer.fillWord, node.correctAnswerFill, StringComparison.OrdinalIgnoreCase);
        }
    }
}
