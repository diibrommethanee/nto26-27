using _Project.Develop.Gameplay.Quests;
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
        private readonly QuestRewards _rewards;
        private readonly EvidenceDatabase _database;

        private readonly List<Clue> _collected = new();
        private readonly Dictionary<Clue, Sprite> _collectedSprites = new();
        private readonly List<Hypothesis> _unlockedHypotheses = new();
        private Dictionary<Hypothesis, IReadOnlyList<Clue>> _solvedHypotheses = new();

        public IReadOnlyList<Clue> Collected { get; }
        public IReadOnlyList<Hypothesis> UnlockedHypotheses { get; }
        public event Action Restored;
        public void AfterRestore() => Restored?.Invoke();
        public event Action<Hypothesis> HypothesisUnlocked;
        public event Action<Hypothesis> HypothesisPassed;
        public event Action<Clue, Vector3?, Vector2?> ClueCollected;

        [Save] private string[] Clues
        {
            get => CheckpointCatalog.Instance.Keys(_collected);
            set => RestoreList(_collected, value);
        }

        [Save] private string[] Hypotheses
        {
            get => CheckpointCatalog.Instance.Keys(_unlockedHypotheses);
            set => RestoreList(_unlockedHypotheses, value);
        }

        [Save] private Dictionary<string, string[]> Answers
        {
            get => _solvedHypotheses.ToDictionary(pair => CheckpointCatalog.Instance.Key(pair.Key),
                pair => CheckpointCatalog.Instance.Keys(pair.Value));
            set
            {
                _solvedHypotheses = value.ToDictionary(
                    pair => CheckpointCatalog.Instance.Resolve<Hypothesis>(pair.Key),
                    pair => (IReadOnlyList<Clue>)Array.AsReadOnly(
                        CheckpointCatalog.Instance.ResolveAll<Clue>(pair.Value)));
            }
        }

        private static void RestoreList<T>(List<T> destination, string[] keys) where T : ScriptableObject
        {
            var restored = CheckpointCatalog.Instance.ResolveAll<T>(keys);
            destination.Clear();
            destination.AddRange(restored);
        }

        public EvidenceService(EvidenceDatabase database, QuestRewards rewards)
        {
            _rewards = rewards;
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
            _rewards.Apply(clue.reward);

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
            return _database.TryGetResult(first, second, out result);
        }

        public bool HasClue(Clue clue) => _collected.Contains(clue);

        public Sprite GetClueSprite(Clue clue) => _collectedSprites.TryGetValue(clue, out var sprite) ? sprite : clue.icon;

        public bool HasSolvedHypothesis(Hypothesis hypothesis) => _solvedHypotheses.ContainsKey(hypothesis);

        public bool TryGetHypothesisAnswers(Hypothesis hypothesis, out IReadOnlyList<Clue> answers) =>
            _solvedHypotheses.TryGetValue(hypothesis, out answers);

        public bool CheckHypothesis(Hypothesis hypothesis, IReadOnlyList<Clue> answers)
        {
            if (!hypothesis.MatchesAnswers(answers) || !answers.All(HasClue))
                return false;

            if (!_solvedHypotheses.ContainsKey(hypothesis))
            {
                _solvedHypotheses.Add(hypothesis, Array.AsReadOnly(answers.ToArray()));
                HypothesisPassed?.Invoke(hypothesis);
            }
            return true;
        }
    }
}
