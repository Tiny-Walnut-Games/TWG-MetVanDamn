using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;

namespace TinyWalnutGames.MetVD.Tests
{
    public class DistrictWfcSystemTests
    {
        private World _world;
        private SimulationSystemGroup _simGroup;

        [SetUp]
        public void SetUp()
        {
            _world = new World("DistrictWfcTestWorld");
            _simGroup = _world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            SystemHandle systemHandle = _world.GetOrCreateSystem(typeof(DistrictWfcSystem));
            _simGroup.AddSystemToUpdateList(systemHandle);
            _simGroup.SortSystems();
        }

        [TearDown]
        public void TearDown()
        {
            if (_world.IsCreated) _world.Dispose();
        }

        private Entity CreateBaseEntity(WfcGenerationState initial, bool withBuffer = true, int iteration = 0)
        {
            var em = _world.EntityManager;
            var e = em.CreateEntity();
            em.AddComponentData(e, new WfcState(initial) { Iteration = iteration });
            em.AddComponentData(e, new NodeId { Value = 0, Coordinates = int2.zero, Level = 0, ParentId = 0 });
            if (withBuffer) em.AddBuffer<WfcCandidateBufferElement>(e);
            return e;
        }

        [Test]
        public void Initialization_AddsCandidates_SetsInProgress()
        {
            var e = CreateBaseEntity(WfcGenerationState.Initialized, withBuffer: true);
            _simGroup.Update();
            var em = _world.EntityManager;
            var state = em.GetComponentData<WfcState>(e);
            var candidates = em.GetBuffer<WfcCandidateBufferElement>(e);
            Assert.AreEqual(WfcGenerationState.InProgress, state.State);
            Assert.AreEqual(4, candidates.Length);
            Assert.AreEqual(4, state.Entropy);
        }

        [Test]
        public void Progression_IncrementsIteration_EntropyReflectsCandidateCount()
        {
            var e = CreateBaseEntity(WfcGenerationState.Initialized, withBuffer: true);
            _simGroup.Update(); // initialization
            _simGroup.Update(); // progression
            var state = _world.EntityManager.GetComponentData<WfcState>(e);
            Assert.GreaterOrEqual(state.Iteration, 1);
            Assert.Greater(state.Entropy, 0);
        }

        [Test]
        public void SingleCandidate_CollapsesToCompleted()
        {
            var em = _world.EntityManager;
            var e = CreateBaseEntity(WfcGenerationState.InProgress, withBuffer: true, iteration: 5);
            var buffer = em.GetBuffer<WfcCandidateBufferElement>(e);
            buffer.Add(new WfcCandidateBufferElement(3, 1.0f));
            _simGroup.Update();
            var state = em.GetComponentData<WfcState>(e);
            Assert.AreEqual(WfcGenerationState.Completed, state.State);
            Assert.AreEqual(3u, state.AssignedTileId);
            Assert.IsTrue(state.IsCollapsed);
        }

        [Test]
        public void EmptyCandidateBuffer_SetsContradiction()
        {
            var em = _world.EntityManager;
            var e = CreateBaseEntity(WfcGenerationState.InProgress, withBuffer: true);
            var buffer = em.GetBuffer<WfcCandidateBufferElement>(e);
            buffer.Clear();
            _simGroup.Update();
            var state = em.GetComponentData<WfcState>(e);
            Assert.AreEqual(WfcGenerationState.Contradiction, state.State);
        }

        [Test]
        public void MissingCandidateBuffer_MarksFailed()
        {
            var e = CreateBaseEntity(WfcGenerationState.InProgress, withBuffer: false);
            _simGroup.Update();
            var state = _world.EntityManager.GetComponentData<WfcState>(e);
            Assert.AreEqual(WfcGenerationState.Failed, state.State);
        }

        [Test]
        public void OverIterationThreshold_TriggersRandomCollapse()
        {
            var em = _world.EntityManager;
            var e = CreateBaseEntity(WfcGenerationState.InProgress, withBuffer: true, iteration: 101);
            var buffer = em.GetBuffer<WfcCandidateBufferElement>(e);
            buffer.Add(new WfcCandidateBufferElement(1, 0.4f));
            buffer.Add(new WfcCandidateBufferElement(2, 0.3f));
            buffer.Add(new WfcCandidateBufferElement(3, 0.3f));
            _simGroup.Update();
            var state = em.GetComponentData<WfcState>(e);
            Assert.AreEqual(WfcGenerationState.Completed, state.State);
            Assert.IsTrue(state.IsCollapsed);
            Assert.AreNotEqual(0u, state.AssignedTileId);
        }
    }
}
