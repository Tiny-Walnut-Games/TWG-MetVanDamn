using NUnit.Framework;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using TinyWalnutGames.MetVD.Shared;
using Unity.Entities;
using Unity.Mathematics;

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

			// Add WorldSeed component that DistrictWfcSystem requires for deterministic behavior
			Entity worldSeedEntity = _world.EntityManager.CreateEntity();
			_world.EntityManager.AddComponentData(worldSeedEntity, new WorldSeed { Value = 42u });
			}
		[TearDown]
		public void TearDown()
			{
			if (_world.IsCreated)
				{
				_world.Dispose();
				}
			}

		private Entity CreateBaseEntity(WfcGenerationState initial, bool withBuffer = true, int iteration = 0)
			{
			EntityManager em = _world.EntityManager;
			Entity e = em.CreateEntity();
			em.AddComponentData(e, new WfcState(initial) { Iteration = iteration });
			em.AddComponentData(e, new NodeId(value: 0, level: 0, parentId: 0, coordinates: int2.zero));
			if (withBuffer)
				{
				em.AddBuffer<WfcCandidateBufferElement>(e);
				}

			return e;
			}
		[Test]
		public void Initialization_AddsCandidates_SetsInProgress()
			{
			// Enable debug output to see what's happening
			DistrictWfcSystem.DebugWfc = true;

			Entity e = CreateBaseEntity(WfcGenerationState.Initialized, withBuffer: true);

			// Debug: Check initial state
			EntityManager em = _world.EntityManager;
			WfcState initialState = em.GetComponentData<WfcState>(e);
			DynamicBuffer<WfcCandidateBufferElement> initialBuffer = em.GetBuffer<WfcCandidateBufferElement>(e);
			NodeId nodeId = em.GetComponentData<NodeId>(e);

			UnityEngine.Debug.Log($"[TEST] BEFORE Update: Entity={e.Index}, State={initialState.State}, BufferLength={initialBuffer.Length}, NodeId={nodeId._value}");

			// Check if system can find our entity
			using var testQuery = em.CreateEntityQuery(typeof(WfcState), typeof(NodeId));
			UnityEngine.Debug.Log($"[TEST] Entities matching WfcState+NodeId: {testQuery.CalculateEntityCount()}");

			using var seedQuery = em.CreateEntityQuery(typeof(WorldSeed));
			UnityEngine.Debug.Log($"[TEST] WorldSeed entities: {seedQuery.CalculateEntityCount()}");

			_simGroup.Update();

			// Debug: Check final state
			WfcState state = em.GetComponentData<WfcState>(e);
			DynamicBuffer<WfcCandidateBufferElement> candidates = em.GetBuffer<WfcCandidateBufferElement>(e);

			UnityEngine.Debug.Log($"[TEST] AFTER Update: State={state.State}, BufferLength={candidates.Length}, Entropy={state.Entropy}");

			// Log buffer contents if any
			if (candidates.Length > 0)
				{
				for (int i = 0; i < candidates.Length; i++)
					{
					UnityEngine.Debug.Log($"[TEST] Candidate {i}: TileId={candidates[i].TileId}, Weight={candidates[i].Weight}");
					}
				}

			Assert.AreEqual(WfcGenerationState.InProgress, state.State, $"Expected InProgress, got {state.State}");
			Assert.AreEqual(4, candidates.Length, $"Expected 4 candidates, got {candidates.Length}");
			Assert.AreEqual(4, state.Entropy, $"Expected entropy 4, got {state.Entropy}");
			}

		[Test]
		public void Progression_IncrementsIteration_EntropyReflectsCandidateCount()
			{
			Entity e = CreateBaseEntity(WfcGenerationState.Initialized, withBuffer: true);
			_simGroup.Update(); // initialization
			_simGroup.Update(); // progression
			WfcState state = _world.EntityManager.GetComponentData<WfcState>(e);
			Assert.GreaterOrEqual(state.Iteration, 1);
			Assert.Greater(state.Entropy, 0);
			}

		[Test]
		public void SingleCandidate_CollapsesToCompleted()
			{
			EntityManager em = _world.EntityManager;
			Entity e = CreateBaseEntity(WfcGenerationState.InProgress, withBuffer: true, iteration: 5);
			DynamicBuffer<WfcCandidateBufferElement> buffer = em.GetBuffer<WfcCandidateBufferElement>(e);
			buffer.Add(new WfcCandidateBufferElement(3, 1.0f));
			_simGroup.Update();
			WfcState state = em.GetComponentData<WfcState>(e);
			Assert.AreEqual(WfcGenerationState.Completed, state.State);
			Assert.AreEqual(3u, state.AssignedTileId);
			Assert.IsTrue(state.IsCollapsed);
			}

		[Test]
		public void EmptyCandidateBuffer_SetsContradiction()
			{
			EntityManager em = _world.EntityManager;
			Entity e = CreateBaseEntity(WfcGenerationState.InProgress, withBuffer: true);
			DynamicBuffer<WfcCandidateBufferElement> buffer = em.GetBuffer<WfcCandidateBufferElement>(e);
			buffer.Clear();
			_simGroup.Update();
			WfcState state = em.GetComponentData<WfcState>(e);
			Assert.AreEqual(WfcGenerationState.Contradiction, state.State);
			}

		[Test]
		public void MissingCandidateBuffer_MarksFailed()
			{
			Entity e = CreateBaseEntity(WfcGenerationState.InProgress, withBuffer: false);
			_simGroup.Update();
			WfcState state = _world.EntityManager.GetComponentData<WfcState>(e);
			Assert.AreEqual(WfcGenerationState.Failed, state.State);
			}

		[Test]
		public void OverIterationThreshold_TriggersRandomCollapse()
			{
			EntityManager em = _world.EntityManager;
			Entity e = CreateBaseEntity(WfcGenerationState.InProgress, withBuffer: true, iteration: 101);
			DynamicBuffer<WfcCandidateBufferElement> buffer = em.GetBuffer<WfcCandidateBufferElement>(e);
			buffer.Add(new WfcCandidateBufferElement(1, 0.4f));
			buffer.Add(new WfcCandidateBufferElement(2, 0.3f));
			buffer.Add(new WfcCandidateBufferElement(3, 0.3f));
			_simGroup.Update();
			WfcState state = em.GetComponentData<WfcState>(e);
			Assert.AreEqual(WfcGenerationState.Completed, state.State);
			Assert.IsTrue(state.IsCollapsed);
			Assert.AreNotEqual(0u, state.AssignedTileId);
			}
		}
	}
