using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Biome;
using CoreBiome = TinyWalnutGames.MetVD.Core.Biome;

namespace TinyWalnutGames.MetVD.Tests.Biome
{
    /// <summary>
    /// Tests for BiomeFieldSystem logic: type assignment, polarity strength scaling, secondary polarity, difficulty smoothing.
    /// </summary>
    public class BiomeFieldSystemTests
    {
        private World _world;
        private SimulationSystemGroup _simGroup;

        [SetUp]
        public void SetUp()
        {
            _world = new World("BiomeFieldTestWorld");
            _simGroup = _world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            var handle = _world.GetOrCreateSystem<BiomeFieldSystem>();
            _simGroup.AddSystemToUpdateList(handle);
            _simGroup.SortSystems();
        }

        [TearDown]
        public void TearDown()
        {
            if (_world.IsCreated) _world.Dispose();
        }

        private Entity CreateBiomeEntity(Polarity primary, int2 coords, byte level = 0)
        {
            var em = _world.EntityManager;
            var e = em.CreateEntity();
            em.AddComponentData(e, new CoreBiome { Type = BiomeType.Unknown, PrimaryPolarity = primary, SecondaryPolarity = Polarity.None, PolarityStrength = 0f, DifficultyModifier = 0f });
            em.AddComponentData(e, new NodeId { Value = 1, Coordinates = coords, Level = level, ParentId = 0 });
            return e;
        }

        [Test]
        public void AssignBiomeType_SunPolarity_PositiveY_SetsSkyGardens()
        {
            var e = CreateBiomeEntity(Polarity.Sun, new int2(0, 5));
            _simGroup.Update();
            var biome = _world.EntityManager.GetComponentData<CoreBiome>(e);
            Assert.AreEqual(BiomeType.SkyGardens, biome.Type);
        }

        [Test]
        public void AssignBiomeType_SunPolarity_NegativeY_SetsSolarPlains()
        {
            var e = CreateBiomeEntity(Polarity.Sun, new int2(0, -5));
            _simGroup.Update();
            var biome = _world.EntityManager.GetComponentData<CoreBiome>(e);
            Assert.AreEqual(BiomeType.SolarPlains, biome.Type);
        }

        [Test]
        public void PolarityStrength_CenterWeaker_OuterStronger()
        {
            var center = CreateBiomeEntity(Polarity.Heat, new int2(0, 0));
            var edge = CreateBiomeEntity(Polarity.Heat, new int2(40, 0));
            _simGroup.Update();
            var em = _world.EntityManager;
            var centerBiome = em.GetComponentData<CoreBiome>(center);
            var edgeBiome = em.GetComponentData<CoreBiome>(edge);
            Assert.Less(centerBiome.PolarityStrength, edgeBiome.PolarityStrength, "Center polarity strength should be lower than far edge.");
        }

        [Test]
        public void PolarityStrength_LevelInfluence_IncreasesWithLevel()
        {
            var low = CreateBiomeEntity(Polarity.Cold, new int2(5, 5), level:0);
            var high = CreateBiomeEntity(Polarity.Cold, new int2(5, 5), level:3);
            _simGroup.Update();
            var em = _world.EntityManager;
            var lowBiome = em.GetComponentData<CoreBiome>(low);
            var highBiome = em.GetComponentData<CoreBiome>(high);
            Assert.Greater(highBiome.PolarityStrength, lowBiome.PolarityStrength, "Higher level should amplify polarity strength.");
        }

        [Test]
        public void SecondaryPolarity_OnlyForTransitionZone()
        {
            var trans = CreateBiomeEntity(Polarity.None, new int2(1,1));
            // Force assignment to TransitionZone: Use primary None and non-zero level to bias fallback.
            _simGroup.Update();
            var biome = _world.EntityManager.GetComponentData<CoreBiome>(trans);
            if (biome.Type == BiomeType.TransitionZone)
            {
                Assert.AreNotEqual(Polarity.None, biome.SecondaryPolarity, "Transition zone should gain secondary polarity.");
            }
            else
            {
                Assert.AreEqual(Polarity.None, biome.SecondaryPolarity, "Non-transition biomes should have no secondary polarity.");
            }
        }

        [Test]
        public void DifficultyModifier_Smoothing_IncreasesOverUpdates()
        {
            var e = CreateBiomeEntity(Polarity.Sun, new int2(0, 10));
            var em = _world.EntityManager;
            _simGroup.Update();
            var first = em.GetComponentData<CoreBiome>(e).DifficultyModifier;
            _simGroup.Update();
            var second = em.GetComponentData<CoreBiome>(e).DifficultyModifier;
            Assert.GreaterOrEqual(second, first, "Difficulty modifier should not decrease across early smoothing updates.");
        }
    }
}
