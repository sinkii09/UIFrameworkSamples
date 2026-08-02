using ColorStackSort.Logic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ColorStackSort.Tests
{
    /// <summary>
    /// Guards the asset-to-generator boundary. The generator itself is covered by the Logic tests;
    /// what is untested elsewhere is whether the inspector's four ints reach the four
    /// <see cref="LevelParams"/> slots they are supposed to.
    /// </summary>
    public class ColorStackSortSettingsTests
    {
        private ColorStackSortSettings _settings;

        [SetUp]
        public void SetUp() => _settings = ScriptableObject.CreateInstance<ColorStackSortSettings>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_settings);

        /// <summary>
        /// Writes a private serialized field. Note this bypasses [Range], which is exactly the
        /// point — Range constrains the inspector, not a hand-edited .asset file.
        /// </summary>
        private void SetField(string fieldName, int value)
        {
            var serialized = new SerializedObject(_settings);
            serialized.FindProperty(fieldName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private void SetBool(string fieldName, bool value)
        {
            var serialized = new SerializedObject(_settings);
            serialized.FindProperty(fieldName).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void ToLevelParams_DefaultAsset_IsValid()
        {
            Assert.DoesNotThrow(() => _settings.ToLevelParams());
        }

        [Test]
        public void ToLevelParams_DefaultAsset_GeneratesASolvableLevel()
        {
            // A combination can pass Validate() and still be one the generator cannot build.
            // The defaults ship with the feature, so they get an end-to-end check.
            var level = LevelGenerator.Generate(_settings.ToLevelParams(), _settings.BaseSeed);

            Assert.IsNotNull(level.Board);
            Assert.IsNotEmpty(level.Solution, "Default settings generated a level with no solution.");
        }

        [Test]
        public void ToLevelParams_MapsEachFieldToItsOwnSlot()
        {
            // Four mutually distinct values, so a transposed constructor argument cannot pass.
            SetField("_colorCount", 5);
            SetField("_capacity", 3);
            SetField("_emptyContainers", 1);
            SetField("_scrambleSteps", 17);

            var parameters = _settings.ToLevelParams();

            Assert.AreEqual(5, parameters.ColorCount);
            Assert.AreEqual(3, parameters.Capacity);
            Assert.AreEqual(1, parameters.EmptyContainers);
            Assert.AreEqual(17, parameters.ScrambleSteps);
        }

        [Test]
        public void ToLevelParams_NoEmptyContainers_Throws()
        {
            // Every container would start full, so no move could ever be legal. Better to throw
            // at load than to hand the player an unplayable board.
            SetField("_emptyContainers", 0);

            Assert.Throws<System.ArgumentException>(() => _settings.ToLevelParams());
        }

        [Test]
        public void ToLevelParams_SingleColor_Throws()
        {
            SetField("_colorCount", 1);

            Assert.Throws<System.ArgumentException>(() => _settings.ToLevelParams());
        }

        [Test]
        public void BaseSeed_RoundTrips()
        {
            SetField("_seed", 12345);

            Assert.AreEqual(12345, _settings.BaseSeed);
        }

        [Test]
        public void ParamsForLevel_ByDefault_FollowsTheCurveNotTheFixedRecipe()
        {
            // The fixed knobs are a debug override, off by default. Were the default inverted,
            // every level would silently render the same board.
            SetField("_colorCount", 12);

            Assert.IsFalse(_settings.UsesFixedRecipe);
            Assert.AreEqual(DifficultyCurve.ForLevel(1).ColorCount, _settings.ParamsForLevel(1).ColorCount);
        }

        [Test]
        public void ParamsForLevel_WithFixedRecipe_IgnoresTheLevel()
        {
            SetBool("_useFixedRecipe", true);
            SetField("_colorCount", 5);

            Assert.AreEqual(5, _settings.ParamsForLevel(1).ColorCount);
            Assert.AreEqual(5, _settings.ParamsForLevel(99).ColorCount);
            Assert.AreEqual(_settings.SeedForLevel(1), _settings.SeedForLevel(99),
                "the whole point of the override is that the board never changes");
        }

        [Test]
        public void SeedForLevel_ByDefault_DiffersPerLevel()
        {
            Assert.AreNotEqual(_settings.SeedForLevel(1), _settings.SeedForLevel(2));
        }
    }
}
