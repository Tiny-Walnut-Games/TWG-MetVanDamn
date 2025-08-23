using NUnit.Framework;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Authoring;

namespace TinyWalnutGames.MetVD.Authoring.Tests
{
    /// <summary>
    /// Tests for BiomeArtProfile integration with ECS systems
    /// </summary>
    public class BiomeArtProfileTests
    {
        private World testWorld;
        private EntityManager entityManager;

        [SetUp]
        public void SetUp()
        {
            testWorld = new World("Test World");
            entityManager = testWorld.EntityManager;
        }

        [TearDown]
        public void TearDown()
        {
            testWorld?.Dispose();
        }

        [Test]
        public void BiomeArtProfile_CanBeCreated()
        {
            var profile = ScriptableObject.CreateInstance<BiomeArtProfile>();
            // Initialize nested settings (was previously flat fields like propSpawnChance)
            profile.propSettings = new PropPlacementSettings();

            Assert.IsNotNull(profile);
            Assert.AreEqual(string.Empty, profile.biomeName ?? string.Empty);
            Assert.AreEqual(Color.white, profile.debugColor);
            Assert.IsNotNull(profile.propSettings);
            Assert.AreEqual(0.1f, profile.propSettings.baseDensity, 0.001f); // default base density
        }

        [Test]
        public void BiomeArtProfileReference_CanBeAddedToEntity()
        {
            var entity = entityManager.CreateEntity();
            var profile = ScriptableObject.CreateInstance<BiomeArtProfile>();
            profile.propSettings = new PropPlacementSettings();

            var artProfileRef = new BiomeArtProfileReference
            {
                ProfileRef = new UnityObjectRef<BiomeArtProfile> { Value = profile },
                IsApplied = false,
                ProjectionType = ProjectionType.TopDown
            };

            entityManager.AddComponentData(entity, artProfileRef);

            Assert.IsTrue(entityManager.HasComponent<BiomeArtProfileReference>(entity));

            var retrievedRef = entityManager.GetComponentData<BiomeArtProfileReference>(entity);
            Assert.AreEqual(ProjectionType.TopDown, retrievedRef.ProjectionType);
            Assert.IsFalse(retrievedRef.IsApplied);
        }

        [Test]
        public void BiomeTransition_ComponentDataWorks()
        {
            var entity = entityManager.CreateEntity();

            var transition = new BiomeTransition
            {
                FromBiome = BiomeType.SolarPlains,
                ToBiome = BiomeType.CrystalCaverns,
                TransitionStrength = 0.5f,
                DistanceToBoundary = 2.0f,
                TransitionTilesApplied = false
            };

            entityManager.AddComponentData(entity, transition);

            Assert.IsTrue(entityManager.HasComponent<BiomeTransition>(entity));

            var retrievedTransition = entityManager.GetComponentData<BiomeTransition>(entity);
            Assert.AreEqual(BiomeType.SolarPlains, retrievedTransition.FromBiome);
            Assert.AreEqual(BiomeType.CrystalCaverns, retrievedTransition.ToBiome);
            Assert.AreEqual(0.5f, retrievedTransition.TransitionStrength, 0.001f);
        }

        [Test]
        public void ProjectionType_EnumValuesAreCorrect()
        {
            Assert.AreEqual(0, (int)ProjectionType.Platformer);
            Assert.AreEqual(1, (int)ProjectionType.TopDown);
            Assert.AreEqual(2, (int)ProjectionType.Isometric);
            Assert.AreEqual(3, (int)ProjectionType.Hexagonal);
        }

        [Test]
        public void BiomeArtProfile_DefaultLayersConfiguration()
        {
            var profile = ScriptableObject.CreateInstance<BiomeArtProfile>();
            profile.propSettings = new PropPlacementSettings();

            Assert.IsNotNull(profile.propSettings.allowedPropLayers);
            Assert.AreEqual(0, profile.propSettings.allowedPropLayers.Count);
            Assert.That(profile.propSettings.baseDensity, Is.InRange(0f, 1f));
        }

        [Test]
        public void BiomeArtProfile_AllowedPropLayersCanBeModified()
        {
            var profile = ScriptableObject.CreateInstance<BiomeArtProfile>();
            profile.propSettings = new PropPlacementSettings();

            profile.propSettings.allowedPropLayers.Add("FloorProps");
            profile.propSettings.allowedPropLayers.Add("WalkableProps");
            profile.propSettings.allowedPropLayers.Add("OverheadProps");

            Assert.AreEqual(3, profile.propSettings.allowedPropLayers.Count);
            Assert.Contains("FloorProps", profile.propSettings.allowedPropLayers);
            Assert.Contains("WalkableProps", profile.propSettings.allowedPropLayers);
            Assert.Contains("OverheadProps", profile.propSettings.allowedPropLayers);
        }
    }
}
