using NUnit.Framework;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Tests
	{
	public class DistrictWfcSystemTests
		{
		private World _world;
		private SimulationSystemGroup _simGroup;

		[SetUp]
		public void SetUp ()
			{
			this._world = new World("DistrictWfcTestWorld");
			this._simGroup = this._world.GetOrCreateSystemManaged<SimulationSystemGroup>();
			SystemHandle systemHandle = this._world.GetOrCreateSystem(typeof(DistrictWfcSystem));
			this._simGroup.AddSystemToUpdateList(systemHandle);
			this._simGroup.SortSystems();
			}

		[TearDown]
		public void TearDown ()
			{
			if (this._world.IsCreated)
				{
				this._world.Dispose();
				}
			}

		private Entity CreateBaseEntity (WfcGenerationState initial, bool withBuffer = true, int iteration = 0)
			{
			EntityManager em = this._world.EntityManager;
			Entity e = em.CreateEntity();
			em.AddComponentData(e, new WfcState(initial) { Iteration = iteration });
			em.AddComponentData(e, new NodeId { _value = 0, Coordinates = int2.zero, Level = 0, ParentId = 0 });
			if (withBuffer)
				{
				em.AddBuffer<WfcCandidateBufferElement>(e);
				}

			return e;
			}

		[Test]
		public void Initialization_AddsCandidates_SetsInProgress ()
			{
			Entity e = this.CreateBaseEntity(WfcGenerationState.Initialized, withBuffer: true);
			this._simGroup.Update();
			EntityManager em = this._world.EntityManager;
			WfcState state = em.GetComponentData<WfcState>(e);
			DynamicBuffer<WfcCandidateBufferElement> candidates = em.GetBuffer<WfcCandidateBufferElement>(e);
			Assert.AreEqual(WfcGenerationState.InProgress, state.State);
			Assert.AreEqual(4, candidates.Length);
			Assert.AreEqual(4, state.Entropy);
			}

		[Test]
		public void Progression_IncrementsIteration_EntropyReflectsCandidateCount ()
			{
			Entity e = this.CreateBaseEntity(WfcGenerationState.Initialized, withBuffer: true);
			this._simGroup.Update(); // initialization
			this._simGroup.Update(); // progression
			WfcState state = this._world.EntityManager.GetComponentData<WfcState>(e);
			Assert.GreaterOrEqual(state.Iteration, 1);
			Assert.Greater(state.Entropy, 0);
			}

		[Test]
		public void SingleCandidate_CollapsesToCompleted ()
			{
			EntityManager em = this._world.EntityManager;
			Entity e = this.CreateBaseEntity(WfcGenerationState.InProgress, withBuffer: true, iteration: 5);
			DynamicBuffer<WfcCandidateBufferElement> buffer = em.GetBuffer<WfcCandidateBufferElement>(e);
			buffer.Add(new WfcCandidateBufferElement(3, 1.0f));
			this._simGroup.Update();
			WfcState state = em.GetComponentData<WfcState>(e);
			Assert.AreEqual(WfcGenerationState.Completed, state.State);
			Assert.AreEqual(3u, state.AssignedTileId);
			Assert.IsTrue(state.IsCollapsed);
			}

		[Test]
		public void EmptyCandidateBuffer_SetsContradiction ()
			{
			EntityManager em = this._world.EntityManager;
			Entity e = this.CreateBaseEntity(WfcGenerationState.InProgress, withBuffer: true);
			DynamicBuffer<WfcCandidateBufferElement> buffer = em.GetBuffer<WfcCandidateBufferElement>(e);
			buffer.Clear();
			this._simGroup.Update();
			WfcState state = em.GetComponentData<WfcState>(e);
			Assert.AreEqual(WfcGenerationState.Contradiction, state.State);
			}

		[Test]
		public void MissingCandidateBuffer_MarksFailed ()
			{
			Entity e = this.CreateBaseEntity(WfcGenerationState.InProgress, withBuffer: false);
			this._simGroup.Update();
			WfcState state = this._world.EntityManager.GetComponentData<WfcState>(e);
			Assert.AreEqual(WfcGenerationState.Failed, state.State);
			}

		[Test]
		public void OverIterationThreshold_TriggersRandomCollapse ()
			{
			EntityManager em = this._world.EntityManager;
			Entity e = this.CreateBaseEntity(WfcGenerationState.InProgress, withBuffer: true, iteration: 101);
			DynamicBuffer<WfcCandidateBufferElement> buffer = em.GetBuffer<WfcCandidateBufferElement>(e);
			buffer.Add(new WfcCandidateBufferElement(1, 0.4f));
			buffer.Add(new WfcCandidateBufferElement(2, 0.3f));
			buffer.Add(new WfcCandidateBufferElement(3, 0.3f));
			this._simGroup.Update();
			WfcState state = em.GetComponentData<WfcState>(e);
			Assert.AreEqual(WfcGenerationState.Completed, state.State);
			Assert.IsTrue(state.IsCollapsed);
			Assert.AreNotEqual(0u, state.AssignedTileId);
			}
		}
	}
