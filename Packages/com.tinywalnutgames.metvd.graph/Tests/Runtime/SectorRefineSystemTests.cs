using NUnit.Framework;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using Unity.Entities;

namespace TinyWalnutGames.MetVD.Tests
	{
	/// <summary>
	/// Managed driver to execute SectorRefinementJob logic in tests (avoids unmanaged ISystem registration issues).
	/// </summary>
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	internal partial class SectorRefineTestDriverSystem : SystemBase
		{
		protected override void OnUpdate ()
			{
			ComponentLookup<WfcState> wfcStateLookup = this.GetComponentLookup<WfcState>(true);
			ComponentLookup<NodeId> nodeIdLookup = this.GetComponentLookup<NodeId>(true);
			BufferLookup<ConnectionBufferElement> connectionBufferLookup = this.GetBufferLookup<ConnectionBufferElement>();
			BufferLookup<GateConditionBufferElement> gateBufferLookup = this.GetBufferLookup<GateConditionBufferElement>();

			uint baseSeed = (uint)(this.World.Unmanaged.Time.ElapsedTime * 997.0);
			var random = new Unity.Mathematics.Random(baseSeed == 0 ? 1u : baseSeed);
			float delta = this.World.Unmanaged.Time.DeltaTime;

			var job = new SectorRefinementJob
				{
				WfcStateLookup = wfcStateLookup,
				NodeIdLookup = nodeIdLookup,
				ConnectionBufferLookup = connectionBufferLookup,
				GateBufferLookup = gateBufferLookup,
				Random = random,
				DeltaTime = delta
				};
			this.Dependency = job.ScheduleParallel(this.Dependency);
			}
		}

	/// <summary>
	/// Tests covering phase transitions and core behaviors of SectorRefineSystem via driver system.
	/// Focuses on deterministic, state-driven branches (not probabilistic loop creation).
	/// </summary>
	public class SectorRefineSystemTests
		{
		private World _world;
		private SimulationSystemGroup _simGroup;

		[SetUp]
		public void SetUp ()
			{
			this._world = new World("SectorRefineTestWorld");
			this._simGroup = this._world.GetOrCreateSystemManaged<SimulationSystemGroup>();
			SectorRefineTestDriverSystem driver = this._world.GetOrCreateSystemManaged<SectorRefineTestDriverSystem>();
			this._simGroup.AddSystemToUpdateList(driver);
			this._simGroup.SortSystems();
			}

		[TearDown]
		public void TearDown ()
			{
			if (this._world != null && this._world.IsCreated)
				{
				this._world.Dispose();
				}
			}

		[Test]
		public void PlanningPhase_Completes_WhenWfcCompleted ()
			{
			EntityManager em = this._world.EntityManager;
			Entity e = em.CreateEntity();
			em.AddComponentData(e, new WfcState(WfcGenerationState.Completed));
			em.AddComponentData(e, new SectorRefinementData(0.3f) { Phase = SectorRefinementPhase.Planning });

			this._simGroup.Update();

			SectorRefinementData data = em.GetComponentData<SectorRefinementData>(e);
			Assert.AreEqual(SectorRefinementPhase.LoopCreation, data.Phase, "Phase should advance to LoopCreation after planning with completed WFC.");
			Assert.GreaterOrEqual(data.CriticalPathLength, 6);
			Assert.LessOrEqual(data.CriticalPathLength, 14); // random.NextInt(6,15) upper exclusive
			Assert.AreEqual(0, data.LoopCount);
			}

		[Test]
		public void LoopCreation_PhaseAdvances_WhenTargetLoopsAlreadyReached ()
			{
			EntityManager em = this._world.EntityManager;
			Entity e = em.CreateEntity();
			em.AddComponentData(e, new SectorRefinementData(0.3f)
				{
				Phase = SectorRefinementPhase.LoopCreation,
				CriticalPathLength = 10,
				LoopCount = (int)(10 * 0.3f) // target loops already satisfied
				});
			em.AddBuffer<ConnectionBufferElement>(e); // required for loop phase logic path

			this._simGroup.Update();

			SectorRefinementData data = em.GetComponentData<SectorRefinementData>(e);
			Assert.AreEqual(SectorRefinementPhase.LockPlacement, data.Phase, "Should move to LockPlacement when loop target achieved.");
			}

		[Test]
		public void LockPlacement_AddsHardLocks_AndAdvancesToPathValidation ()
			{
			EntityManager em = this._world.EntityManager;
			Entity e = em.CreateEntity();
			em.AddComponentData(e, new SectorRefinementData(0.3f)
				{
				Phase = SectorRefinementPhase.LockPlacement,
				CriticalPathLength = 16,
				LoopCount = 5 // arbitrary non-zero to avoid PathValidation failure later
				});
			em.AddBuffer<GateConditionBufferElement>(e);

			this._simGroup.Update();

			SectorRefinementData data = em.GetComponentData<SectorRefinementData>(e);
			Assert.AreEqual(SectorRefinementPhase.PathValidation, data.Phase, "Should proceed to PathValidation after lock placement.");
			Assert.GreaterOrEqual(data.HardLockCount, 1, "At least one hard lock should be added.");
			}

		[Test]
		public void PathValidation_Completes_WhenLoopsAndLocksPresent ()
			{
			EntityManager em = this._world.EntityManager;
			Entity e = em.CreateEntity();
			em.AddComponentData(e, new SectorRefinementData(0.3f)
				{
				Phase = SectorRefinementPhase.PathValidation,
				CriticalPathLength = 12,
				LoopCount = 2,
				HardLockCount = 1
				});

			this._simGroup.Update();

			SectorRefinementData data = em.GetComponentData<SectorRefinementData>(e);
			Assert.AreEqual(SectorRefinementPhase.Completed, data.Phase, "Valid path setup should mark refinement Completed.");
			}

		[Test]
		public void PathValidation_Fails_WhenNoLoopsOrLocks ()
			{
			EntityManager em = this._world.EntityManager;
			Entity e = em.CreateEntity();
			em.AddComponentData(e, new SectorRefinementData(0.3f)
				{
				Phase = SectorRefinementPhase.PathValidation,
				CriticalPathLength = 11,
				LoopCount = 0,
				HardLockCount = 0
				});

			this._simGroup.Update();

			SectorRefinementData data = em.GetComponentData<SectorRefinementData>(e);
			Assert.AreEqual(SectorRefinementPhase.Failed, data.Phase, "Missing loops & locks over thresholds should Fail refinement.");
			}
		}
	}
