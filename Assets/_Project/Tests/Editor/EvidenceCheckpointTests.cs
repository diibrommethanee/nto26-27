using System.IO;
using System.Reflection;
using _Project.Develop.Data;
using _Project.Develop.Data.Evidence;
using _Project.Develop.Gameplay.Evidence;
using _Project.Develop.Gameplay.Npc;
using _Project.Develop.Gameplay.Quests;
using NUnit.Framework;
using UnityEditor;

namespace _Project.Tests
{
    public class EvidenceCheckpointTests
    {
        [Test]
        public void OnlyCorrectClueSolvesTheFillSlot()
        {
            var hypothesis = UnityEngine.ScriptableObject.CreateInstance<Hypothesis>();
            var morning = UnityEngine.ScriptableObject.CreateInstance<Clue>();
            var evening = UnityEngine.ScriptableObject.CreateInstance<Clue>();
            try
            {
                var evidence = new EvidenceService(null, new QuestRewards(new NpcStates()));
                morning.hypothesis = evening.hypothesis = hypothesis;
                morning.fillWord = "morning";
                evening.fillWord = "evening";
                evidence.Collect(morning);
                evidence.Collect(evening);
                hypothesis.nodes.Add(new HypothesisNode { type = HypothesisNodeType.Fill, correctClue = evening });
                Assert.That(evidence.CheckHypothesis(hypothesis, new[] { morning }), Is.False);
                Assert.That(evidence.HasSolvedHypothesis(hypothesis), Is.False);
                Assert.That(evidence.CheckHypothesis(hypothesis, new[] { evening }), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(morning);
                UnityEngine.Object.DestroyImmediate(evening);
                UnityEngine.Object.DestroyImmediate(hypothesis);
            }
        }

        [Test]
        public void UnknownKeyAfterAValidKeyPreservesPreviouslyCollectedClues()
        {
            var database = AssetDatabase.LoadAssetAtPath<EvidenceDatabase>(
                "Assets/_Project/Develop/Data/Evidence/EvidenceDatabase.asset");
            var clue = AssetDatabase.LoadAssetAtPath<Clue>(
                "Assets/_Project/Develop/Data/Evidence/Clues/OverheardKey.asset");
            var otherClue = AssetDatabase.LoadAssetAtPath<Clue>(
                "Assets/_Project/Develop/Data/Evidence/Clues/OldEmptyCup.asset");
            var evidence = new EvidenceService(database, new QuestRewards(new NpcStates()));
            var saved = typeof(EvidenceService).GetProperty("Clues", BindingFlags.Instance | BindingFlags.NonPublic);
            var key = CheckpointCatalog.Instance.Key(clue);
            saved.SetValue(evidence, new[] { key });
            var error = Assert.Throws<TargetInvocationException>(() =>
                saved.SetValue(evidence, new[] { CheckpointCatalog.Instance.Key(otherClue), "missing-asset" }));
            Assert.That(error.InnerException, Is.TypeOf<InvalidDataException>());
            Assert.That(evidence.Collected, Is.EqualTo(new[] { clue }));
        }
    }
}
