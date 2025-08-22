using NUnit.Framework;
using Unity.Entities;
using TinyWalnutGames.MetVD.Core;
using CoreBiome = TinyWalnutGames.MetVD.Core.Biome;
using AggregatorNS = TinyWalnutGames.MetVD.Utility; // ensure namespace resolution

namespace TinyWalnutGames.MetVD.Tests.Utility
{
    public class AggregatorSystemTests
    {
        private World _world;
        private SimulationSystemGroup _simGroup;

        [SetUp]
        public void SetUp()
        {
            _world = new World("AggregatorTestWorld");
            _simGroup = _world.GetOrCreateSystemManaged<SimulationSystemGroup>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_world.IsCreated) _world.Dispose();
        }

        [Test]
        public void EmptyWorld_YieldsZero()
        {
            var agg = _world.GetOrCreateSystem<AggregatorNS.AggregatorSystem<CoreBiome>>();
            _simGroup.AddSystemToUpdateList(agg);
            _simGroup.SortSystems();
            _simGroup.Update();
            Assert.AreEqual(0, AggregatorNS.AggregatorSystem<CoreBiome>.GetLastCount());
        }

        [Test]
        public void PopulatedWorld_CountMatchesEntities()
        {
            var em = _world.EntityManager;
            for (int i = 0; i < 5; i++)
            {
                var e = em.CreateEntity();
                em.AddComponentData(e, new CoreBiome { Type = BiomeType.SolarPlains, PrimaryPolarity = Polarity.Sun, PolarityStrength = 0.5f, SecondaryPolarity = Polarity.None, DifficultyModifier = 1f });
            }
            var agg = _world.GetOrCreateSystem<AggregatorNS.AggregatorSystem<CoreBiome>>();
            _simGroup.AddSystemToUpdateList(agg);
            _simGroup.SortSystems();
            _simGroup.Update();
            Assert.AreEqual(5, AggregatorNS.AggregatorSystem<CoreBiome>.GetLastCount());
        }

        [Test]
        public void MultipleFrames_ClearAndRecapture()
        {
            var em = _world.EntityManager;
            var agg = _world.GetOrCreateSystem<AggregatorNS.AggregatorSystem<CoreBiome>>();
            _simGroup.AddSystemToUpdateList(agg);
            _simGroup.SortSystems();

            _simGroup.Update();
            Assert.AreEqual(0, AggregatorNS.AggregatorSystem<CoreBiome>.GetLastCount());

            for (int i = 0; i < 3; i++)
            {
                var e = em.CreateEntity();
                em.AddComponentData(e, new CoreBiome { Type = BiomeType.VolcanicCore, PrimaryPolarity = Polarity.Heat, PolarityStrength = 0.8f, SecondaryPolarity = Polarity.None, DifficultyModifier = 1.2f });
            }
            _simGroup.Update();
            Assert.AreEqual(3, AggregatorNS.AggregatorSystem<CoreBiome>.GetLastCount());

            using (var query = em.CreateEntityQuery(typeof(CoreBiome)))
            {
                var arr = query.ToEntityArray(Unity.Collections.Allocator.Temp);
                em.DestroyEntity(arr[0]);
            }
            _simGroup.Update();
            Assert.AreEqual(2, AggregatorNS.AggregatorSystem<CoreBiome>.GetLastCount());
        }
    }
}
