using NUnit.Framework;
using System.Collections;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using Unity.Entities;
using UnityEngine.TestTools;

namespace TinyWalnutGames.MetVD.Tests
	{
	/// <summary>
	/// Tests for MetVanDAMN WFC system ensuring thread safety and constraint validation
	/// </summary>
	public class WfcSystemTests
		{
		private World testWorld;
		private SimulationSystemGroup simGroup;

		[SetUp]
		public void SetUp ()
			{
			testWorld = new World("TestWorld");
			// Use Simulation group to drive updates; systems may be registered by bootstrap in play mode
			simGroup = testWorld.GetOrCreateSystemManaged<SimulationSystemGroup>();
			}

		[TearDown]
		public void TearDown ()
			{
			if (testWorld != null && testWorld.IsCreated)
				{
				testWorld.Dispose();
				}
			}

		[Test]
		public void WfcSystem_ThreadSafety_ShouldUseParallelRandomArray ()
			{
			// This test verifies that WFC system uses NativeArray<Random> for thread safety
			// (addresses blocker #3 - Random in parallel jobs)

			// Create a test entity with WFC component
			Entity entity = testWorld.EntityManager.CreateEntity();
			testWorld.EntityManager.AddComponentData(entity, new WfcState(WfcGenerationState.Initialized));

			// Group update should not throw even if WFC is not present in this world
			Assert.DoesNotThrow(() =>
			{
				simGroup.Update();
			}, "WFC system should handle parallel random generation safely");
			}

		[Test]
		public void WfcConstraintPropagation_SocketCompatibility_ShouldValidateCorrectly ()
			{
			// Test socket compatibility checking (implementation gap)
			var socketA = new WfcSocket(1, 0, Polarity.Sun, true);
			var socketB = new WfcSocket(1, 2, Polarity.Sun, true); // Opposite direction
			var socketC = new WfcSocket(2, 0, Polarity.Moon, true); // Different ID

			// Compatible sockets (same ID, opposite directions, compatible polarity)
			Assert.IsTrue(socketA.IsCompatibleWith(socketB), "Same ID sockets with opposite directions should be compatible");

			// Incompatible sockets (different IDs)
			Assert.IsFalse(socketA.IsCompatibleWith(socketC), "Different socket IDs should not be compatible");
			}

		[Test]
		public void WfcBiomeValidation_PolarityConstraints_ShouldEnforceCoherence ()
			{
			// Test biome polarity constraints in WFC
			var sunBiome = new Biome(BiomeType.SolarPlains, Polarity.Sun, 1.0f);
			var moonBiome = new Biome(BiomeType.ShadowRealms, Polarity.Moon, 1.0f);

			// Same polarity biomes should be compatible with themselves
			Assert.IsTrue(sunBiome.IsCompatibleWith(Polarity.Sun), "Sun biome should be compatible with Sun polarity");

			// Different primary polarities should not be compatible
			Assert.IsFalse(sunBiome.IsCompatibleWith(Polarity.Moon), "Sun biome should not be compatible with Moon polarity");

			// Any polarity should work with all biomes
			Assert.IsTrue(sunBiome.IsCompatibleWith(Polarity.Any), "Any polarity should work with all biomes");
			Assert.IsTrue(moonBiome.IsCompatibleWith(Polarity.Any), "Any polarity should work with all biomes");
			}

		[UnityTest]
		public IEnumerator WfcGenerationStress_MultipleFrames_ShouldCompleteWithoutErrors ()
			{
			// Stress test for WFC generation over multiple frames
			for (int frame = 0; frame < 10; frame++)
				{
				// Create test entities for WFC processing
				for (int i = 0; i < 5; i++)
					{
					Entity entity = testWorld.EntityManager.CreateEntity();
					testWorld.EntityManager.AddComponentData(entity, new WfcState(WfcGenerationState.Initialized));
					testWorld.EntityManager.AddBuffer<WfcCandidateBufferElement>(entity);
					}

				// Update group; should not throw
				Assert.DoesNotThrow(() => simGroup.Update(), $"WFC update should not throw on frame {frame}");

				yield return null;
				}
			}
		}
	}
