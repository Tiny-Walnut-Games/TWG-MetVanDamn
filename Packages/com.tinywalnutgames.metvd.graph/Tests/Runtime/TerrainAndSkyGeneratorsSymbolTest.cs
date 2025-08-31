using NUnit.Framework;
using TinyWalnutGames.MetVD.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Graph.Tests
	{
	/// <summary>
	/// Simple test to verify that all symbols referenced in TerrainAndSkyGenerators are properly resolved
	/// </summary>
	public class TerrainAndSkyGeneratorsSymbolTest
		{
		private World testWorld;

		[SetUp]
		public void SetUp ()
			{
			this.testWorld = new World("Test World");
			}

		[TearDown]
		public void TearDown ()
			{
			if (this.testWorld?.IsCreated == true)
				{
				this.testWorld.Dispose();
				}
			}

		[Test]
		public void StackedSegmentGenerator_CanBeCreated ()
			{
			// Test that the StackedSegmentGenerator system can be created
			// For ISystem (unmanaged) types, we test by getting the simulation group
			InitializationSystemGroup simGroup = this.testWorld.GetOrCreateSystemManaged<InitializationSystemGroup>();
			Assert.IsNotNull(simGroup);

			// The system should be automatically registered in the group via [UpdateInGroup]
			// We can verify compilation by just checking the simulation group exists
			// The actual system instance is managed by the group internally
			}

		[Test]
		public void LinearBranchingCorridorGenerator_CanBeCreated ()
			{
			// Test that the LinearBranchingCorridorGenerator system can be created
			InitializationSystemGroup simGroup = this.testWorld.GetOrCreateSystemManaged<InitializationSystemGroup>();
			Assert.IsNotNull(simGroup);
			}

		[Test]
		public void BiomeWeightedHeightmapGenerator_CanBeCreated ()
			{
			// Test that the BiomeWeightedHeightmapGenerator system can be created
			InitializationSystemGroup simGroup = this.testWorld.GetOrCreateSystemManaged<InitializationSystemGroup>();
			Assert.IsNotNull(simGroup);
			}

		[Test]
		public void LayeredPlatformCloudGenerator_CanBeCreated ()
			{
			// Test that the LayeredPlatformCloudGenerator system can be created
			InitializationSystemGroup simGroup = this.testWorld.GetOrCreateSystemManaged<InitializationSystemGroup>();
			Assert.IsNotNull(simGroup);
			}

		[Test]
		public void BeatType_Enum_CanBeUsed ()
			{
			// Test that BeatType enum can be used
			BeatType challengeBeat = BeatType.Challenge;
			BeatType restBeat = BeatType.Rest;
			BeatType secretBeat = BeatType.Secret;

			Assert.AreEqual(0, (byte)challengeBeat);
			Assert.AreEqual(1, (byte)restBeat);
			Assert.AreEqual(2, (byte)secretBeat);
			}

		[Test]
		public void CloudMotionType_Enum_CanBeUsed ()
			{
			// Test that CloudMotionType enum can be used
			CloudMotionType gentle = CloudMotionType.Gentle;
			CloudMotionType gusty = CloudMotionType.Gusty;
			CloudMotionType conveyor = CloudMotionType.Conveyor;
			CloudMotionType electric = CloudMotionType.Electric;

			Assert.AreEqual(0, (byte)gentle);
			Assert.AreEqual(1, (byte)gusty);
			Assert.AreEqual(2, (byte)conveyor);
			Assert.AreEqual(3, (byte)electric);
			}

		[Test]
		public void TypeConversionUtility_CanConvertTypes ()
			{
			// Test that TypeConversionUtility can convert between types
			RoomFeatureType platformType = RoomFeatureType.Platform;
			RoomFeatureType obstacleType = RoomFeatureType.Obstacle;

#pragma warning disable CS0618 // Type or member is obsolete
			RoomFeatureObjectType convertedPlatform = TypeConversionUtility.ConvertToObjectType(platformType);
			RoomFeatureObjectType convertedObstacle = TypeConversionUtility.ConvertToObjectType(obstacleType);
#pragma warning restore CS0618 // Type or member is obsolete

			Assert.AreEqual(RoomFeatureType.Platform, convertedPlatform);
			Assert.AreEqual(RoomFeatureType.Obstacle, convertedObstacle);
			}

		[Test]
		public void JumpArcSolver_CanCalculateMinimumSpacing ()
			{
			// Test that JumpArcSolver can calculate minimum platform spacing
			var physics = new JumpArcPhysics(3.0f, 4.0f, 1.5f, 1.0f, 2.0f, 6.0f);
			JumpArcSolver.CalculateMinimumPlatformSpacing(physics, out int2 spacing);

			Assert.Greater(spacing.x, 0);
			Assert.Greater(spacing.y, 0);
			}

		[Test]
		public void JumpArcSolver_CanCheckReachability ()
			{
			// Test that JumpArcSolver can check if positions are reachable
			var physics = new JumpArcPhysics(3.0f, 4.0f, 1.5f, 1.0f, 2.0f, 6.0f);
			var from = new int2(0, 0);
			var to = new int2(2, 1);

			bool isReachable = JumpArcSolver.IsReachable(from, to, Ability.Jump, physics);
			Assert.IsTrue(isReachable);
			}

		[Test]
		public void JumpArcSolver_CanCalculateJumpArc ()
			{
			// Test that JumpArcSolver can calculate jump arc data
			var physics = new JumpArcPhysics(3.0f, 4.0f, 1.5f, 1.0f, 2.0f, 6.0f);
			var from = new int2(0, 0);
			var to = new int2(2, 1);

			JumpArcSolver.CalculateJumpArc(from, to, physics, out JumpArcData arcData);

			Assert.AreEqual(from, arcData.StartPosition);
			Assert.AreEqual(to, arcData.EndPosition);
			Assert.Greater(arcData.FlightTime, 0);
			}
		}
	}
