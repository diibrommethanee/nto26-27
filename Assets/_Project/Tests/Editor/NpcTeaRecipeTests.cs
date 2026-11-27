using _Project.Develop.Gameplay.Npc;
using _Project.Develop.Data.Npc;
using _Project.Develop.Data.Tea;
using _Project.Develop.Gameplay.Tea;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace _Project.Tests
{
    public class NpcTeaRecipeTests
    {
        private TeaRecipeBook _book;
        private NpcData _npc;
        private NpcTeaService _tea;
        private NpcStates _states;

        [SetUp]
        public void SetUp()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TeaRecipeBook>(
                "Assets/_Project/Develop/Data/Tea/NpcTeaRecipes.asset");
            _book = Object.Instantiate(asset);
            _states = new NpcStates();
            _tea = new NpcTeaService(_book, _states);
            _npc = ScriptableObject.CreateInstance<NpcData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_npc);
            Object.DestroyImmediate(_book);
        }

        [TestCase(-25, 25, TeaIngredientType.Mint)]
        [TestCase(-25, -24, TeaIngredientType.LemonBalm, TeaIngredientType.Mint)]
        [TestCase(25, 24, TeaIngredientType.LemonBalm)]
        [TestCase(25, -25, TeaIngredientType.LemonBalm, TeaIngredientType.Thyme)]
        [TestCase(24, -25, TeaIngredientType.Thyme)]
        [TestCase(-24, 25, TeaIngredientType.Thyme, TeaIngredientType.Mint)]
        [TestCase(0, 0)]
        [TestCase(0, 0, TeaIngredientType.None)]
        public void OriginalRecipesPreserveMoodTransitions(int initial, int expected, params TeaIngredientType[] ingredients)
        {
            _npc.initialMood = initial;
            Assert.That(initial + _tea.MoodDeltaFor(_npc, ingredients), Is.EqualTo(expected));
        }

        [Test]
        public void ServingTeaChangesTheNpcOnlyOnce()
        {
            _npc.initialMood = -25;
            var mint = new[] { TeaIngredientType.Mint };
            Assert.That(_tea.TryServe(_npc, mint), Is.True);
            Assert.That(_states[_npc].Mood, Is.EqualTo(25));
            Assert.That(_tea.TryServe(_npc, mint), Is.False);
            Assert.That(_states[_npc].Mood, Is.EqualTo(25));
        }

        [Test]
        public void DesignerCanAddASeventhRecipeThroughSerializedData()
        {
            var mint = new[] { TeaIngredientType.Mint };
            Assert.That(_tea.MoodDeltaFor(_npc, mint), Is.Zero);

            var settings = new SerializedObject(_book);
            var recipes = settings.FindProperty("recipes");
            var index = recipes.arraySize++;
            var recipe = recipes.GetArrayElementAtIndex(index);
            recipe.FindPropertyRelative("currentMood").enumValueIndex = (int)TeaMood.Sleepy;
            recipe.FindPropertyRelative("targetMood").enumValueIndex = (int)TeaMood.Excited;
            var ingredients = recipe.FindPropertyRelative("ingredients");
            ingredients.arraySize = 1;
            ingredients.GetArrayElementAtIndex(0).enumValueIndex = (int)TeaIngredientType.Mint;
            settings.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(_tea.MoodDeltaFor(_npc, mint), Is.EqualTo(25));
        }
    }
}
