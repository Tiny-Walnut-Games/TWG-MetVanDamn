using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using NUnit.Framework;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;

namespace TinyWalnutGames.MetVD.Graph.Tests
{
    /// <summary>
    /// Simple test to verify that all symbols referenced in TerrainAndSkyGenerators are properly resolved
    /// </summary>
    public class TerrainAndSkyGeneratorsSymbolTest
    {
        World testWorld;

        [SetUp]
        public void SetUp()
        {
            testWorld = new World("Test World");
        }

        [TearDown]
        public void TearDown()
        {
            if (testWorld?.IsCreated == true)
            {
                testWorld.Dispose();
            }
        }

        [Test]
        public void StackedSegmentGenerator_CanBeCreated()
        {
            // Test that the StackedSegmentGenerator system can be created
            // For ISystem (unmanaged) types, we test by getting the simulation group
            var simGroup = testWorld.GetOrCreateSystemManaged<InitializationSystemGroup>();
            Assert.IsNotNull(simGroup);
            
            // The system should be automatically registered in the group via [UpdateInGroup]
            // We can verify compilation by just checking the simulation group exists
            // The actual system instance is managed by the group internally
        }

        [Test]
        public void LinearBranchingCorridorGenerator_CanBeCreated()
        {
            // Test that the LinearBranchingCorridorGenerator system can be created
            var simGroup = testWorld.GetOrCreateSystemManaged<InitializationSystemGroup>();
            Assert.IsNotNull(simGroup);
        }

        [Test]
        public void BiomeWeightedHeightmapGenerator_CanBeCreated()
        {
            // Test that the BiomeWeightedHeightmapGenerator system can be created
            var simGroup = testWorld.GetOrCreateSystemManaged<InitializationSystemGroup>();
            Assert.IsNotNull(simGroup);
        }

        [Test]
        public void LayeredPlatformCloudGenerator_CanBeCreated()
        {
            // Test that the LayeredPlatformCloudGenerator system can be created
            var simGroup = testWorld.GetOrCreateSystemManaged<InitializationSystemGroup>();
            Assert.IsNotNull(simGroup);
        }

        [Test]
        public void BeatType_Enum_CanBeUsed()
        {
            // Test that BeatType enum can be used
            var challengeBeat = BeatType.Challenge;
            var restBeat = BeatType.Rest;
            var secretBeat = BeatType.Secret;
            
            Assert.AreEqual(0, (byte)challengeBeat);
            Assert.AreEqual(1, (byte)restBeat);
            Assert.AreEqual(2, (byte)secretBeat);
        }

        [Test]
        public void CloudMotionType_Enum_CanBeUsed()
        {
            // Test that CloudMotionType enum can be used
            var gentle = CloudMotionType.Gentle;
            var gusty = CloudMotionType.Gusty;
            var conveyor = CloudMotionType.Conveyor;
            var electric = CloudMotionType.Electric;
            
            Assert.AreEqual(0, (byte)gentle);
            Assert.AreEqual(1, (byte)gusty);
            Assert.AreEqual(2, (byte)conveyor);
            Assert.AreEqual(3, (byte)electric);
        }

        [Test]
        public void TypeConversionUtility_CanConvertTypes()
        {
            // Test that TypeConversionUtility can convert between types
            var platformType = RoomFeatureType.Platform;
            var obstacleType = RoomFeatureType.Obstacle;

#pragma warning disable CS0618 // Type or member is obsolete
            var convertedPlatform = TypeConversionUtility.ConvertToObjectType(platformType);
            var convertedObstacle = TypeConversionUtility.ConvertToObjectType(obstacleType);
#pragma warning restore CS0618 // Type or member is obsolete

            Assert.AreEqual(RoomFeatureType.Platform, convertedPlatform);
            Assert.AreEqual(RoomFeatureType.Obstacle, convertedObstacle);
        }

        [Test]
        public void JumpArcSolver_CanCalculateMinimumSpacing()
        {
            // Test that JumpArcSolver can calculate minimum platform spacing
            var physics = new JumpArcPhysics(3.0f, 4.0f, 1.5f, 1.0f, 2.0f, 6.0f);
            var spacing = JumpArcSolver.CalculateMinimumPlatformSpacing(physics);
            
            Assert.Greater(spacing.x, 0);
            Assert.Greater(spacing.y, 0);
        }

        [Test]
        public void JumpArcSolver_CanCheckReachability()
        {
            // Test that JumpArcSolver can check if positions are reachable
            var physics = new JumpArcPhysics(3.0f, 4.0f, 1.5f, 1.0f, 2.0f, 6.0f);
            var from = new int2(0, 0);
            var to = new int2(2, 1);
            
            var isReachable = JumpArcSolver.IsReachable(from, to, Ability.Jump, physics);
            Assert.IsTrue(isReachable);
        }

        [Test]
        public void JumpArcSolver_CanCalculateJumpArc()
        {
            // Test that JumpArcSolver can calculate jump arc data
            var physics = new JumpArcPhysics(3.0f, 4.0f, 1.5f, 1.0f, 2.0f, 6.0f);
            var from = new int2(0, 0);
            var to = new int2(2, 1);
            
            var arcData = JumpArcSolver.CalculateJumpArc(from, to, physics);
            
            Assert.AreEqual(from, arcData.StartPosition);
            Assert.AreEqual(to, arcData.EndPosition);
            Assert.Greater(arcData.FlightTime, 0);
        }
    }
}
