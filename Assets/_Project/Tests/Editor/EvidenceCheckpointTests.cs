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
