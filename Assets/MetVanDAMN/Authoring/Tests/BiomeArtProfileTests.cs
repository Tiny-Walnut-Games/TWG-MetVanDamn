using NUnit.Framework;
using TinyWalnutGames.MetVD.Core;
using Unity.Entities;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring.Tests
	{
	/// <summary>
	/// Tests for BiomeArtProfile integration with ECS systems
	/// </summary>
#nullable enable
	public class BiomeArtProfileTests
		{
		private World testWorld = null!; // assigned in SetUp
		private EntityManager entityManager; // struct assigned in SetUp

		[SetUp]
		public void SetUp()
			{
			this.testWorld = new World("Test World");
			this.entityManager = this.testWorld.EntityManager;
			}

		[TearDown]
		public void TearDown()
			{
			this.testWorld?.Dispose();
			}

		[Test]
		public void BiomeArtProfile_CanBeCreated()
			{
			BiomeArtProfile profile = ScriptableObject.CreateInstance<BiomeArtProfile>();
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
			Entity entity = this.entityManager.CreateEntity();
			BiomeArtProfile profile = ScriptableObject.CreateInstance<BiomeArtProfile>();
			profile.propSettings = new PropPlacementSettings();

			var artProfileRef = new BiomeArtProfileReference
				{
				ProfileRef = new UnityObjectRef<BiomeArtProfile> { Value = profile },
				IsApplied = false,
				ProjectionType = ProjectionType.TopDown
				};

			this.entityManager.AddComponentData(entity, artProfileRef);

			Assert.IsTrue(this.entityManager.HasComponent<BiomeArtProfileReference>(entity));

			BiomeArtProfileReference retrievedRef = this.entityManager.GetComponentData<BiomeArtProfileReference>(entity);
			Assert.AreEqual(ProjectionType.TopDown, retrievedRef.ProjectionType);
			Assert.IsFalse(retrievedRef.IsApplied);
			}

		[Test]
		public void BiomeTransition_ComponentDataWorks()
			{
			Entity entity = this.entityManager.CreateEntity();

			var transition = new BiomeTransition
				{
				FromBiome = BiomeType.SolarPlains,
				ToBiome = BiomeType.CrystalCaverns,
				TransitionStrength = 0.5f,
				DistanceToBoundary = 2.0f,
				TransitionTilesApplied = false
				};

			this.entityManager.AddComponentData(entity, transition);

			Assert.IsTrue(this.entityManager.HasComponent<BiomeTransition>(entity));

			BiomeTransition retrievedTransition = this.entityManager.GetComponentData<BiomeTransition>(entity);
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
			BiomeArtProfile profile = ScriptableObject.CreateInstance<BiomeArtProfile>();
			profile.propSettings = new PropPlacementSettings();

			Assert.IsNotNull(profile.propSettings.allowedPropLayers);
			Assert.AreEqual(0, profile.propSettings.allowedPropLayers.Count);
			Assert.That(profile.propSettings.baseDensity, Is.InRange(0f, 1f));
			}

		[Test]
		public void BiomeArtProfile_AllowedPropLayersCanBeModified()
			{
			BiomeArtProfile profile = ScriptableObject.CreateInstance<BiomeArtProfile>();
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
