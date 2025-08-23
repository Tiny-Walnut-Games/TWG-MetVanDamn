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
            // Test that BiomeArtProfile can be created as a ScriptableObject
            var profile = ScriptableObject.CreateInstance<BiomeArtProfile>();
            
            Assert.IsNotNull(profile);
            Assert.AreEqual("", profile.biomeName); // Default empty string
            Assert.AreEqual(Color.white, profile.debugColor); // Default white
            Assert.AreEqual(0.1f, profile.propSpawnChance, 0.001f); // Default spawn chance
        }

        [Test]
        public void BiomeArtProfileReference_CanBeAddedToEntity()
        {
            // Test that BiomeArtProfileReference component can be added to entities
            var entity = entityManager.CreateEntity();
            var profile = ScriptableObject.CreateInstance<BiomeArtProfile>();
            
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
            // Test that BiomeTransition component can be created and used
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
            // Test that ProjectionType enum has expected values
            Assert.AreEqual(0, (int)ProjectionType.Platformer);
            Assert.AreEqual(1, (int)ProjectionType.TopDown);
            Assert.AreEqual(2, (int)ProjectionType.Isometric);
            Assert.AreEqual(3, (int)ProjectionType.Hexagonal);
        }

        [Test]
        public void BiomeArtProfile_DefaultLayersConfiguration()
        {
            // Test that default layer configuration is sensible
            var profile = ScriptableObject.CreateInstance<BiomeArtProfile>();
            
            // Should start with empty allowed prop layers
            Assert.IsNotNull(profile.allowedPropLayers);
            Assert.AreEqual(0, profile.allowedPropLayers.Count);
            
            // Should have reasonable default spawn chance
            Assert.That(profile.propSpawnChance, Is.InRange(0f, 1f));
        }

        [Test]
        public void BiomeArtProfile_AllowedPropLayersCanBeModified()
        {
            // Test that allowed prop layers can be configured
            var profile = ScriptableObject.CreateInstance<BiomeArtProfile>();
            
            profile.allowedPropLayers.Add("FloorProps");
            profile.allowedPropLayers.Add("WalkableProps");
            profile.allowedPropLayers.Add("OverheadProps");
            
            Assert.AreEqual(3, profile.allowedPropLayers.Count);
            Assert.Contains("FloorProps", profile.allowedPropLayers);
            Assert.Contains("WalkableProps", profile.allowedPropLayers);
            Assert.Contains("OverheadProps", profile.allowedPropLayers);
        }
    }
}