using System.Reflection;
using _Project.Develop.Data.Npc;
using _Project.Develop.Data.Tea;
using _Project.Develop.Gameplay.Npc;
using _Project.Develop.Gameplay.Tea;
using NUnit.Framework;
using UnityEditor;

namespace _Project.Tests
{
    public class NpcCheckpointTests
    {
        [Test]
        public void RestoringAMiddayCheckpointKeepsMoodDialogueAndTeaServed()
        {
            var npc = AssetDatabase.LoadAssetAtPath<NpcData>("Assets/_Project/Develop/Data/Npc/Mr. Fih.asset");
            var recipes = AssetDatabase.LoadAssetAtPath<TeaRecipeBook>(
                "Assets/_Project/Develop/Data/Tea/NpcTeaRecipes.asset");
            var original = new NpcStates();
            Assert.That(new NpcTeaService(recipes, original).TryServe(npc, new[] { TeaIngredientType.Thyme }), Is.True);

            var saved = typeof(NpcStates).GetProperty("Characters", BindingFlags.Instance | BindingFlags.NonPublic);
            var restored = new NpcStates();
            saved.SetValue(restored, saved.GetValue(original));

            Assert.That(restored[npc].Mood, Is.EqualTo(-25));
            Assert.That(restored[npc].Dialogue, Is.SameAs(npc.dialogue));
            Assert.That(restored[npc].TeaServed, Is.True);
        }
    }
}
