using NUnit.Framework;
using Unity.Entities;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;

namespace TinyWalnutGames.MetVD.Tests
{
    /// <summary>
    /// Managed driver to execute SectorRefinementJob logic in tests (avoids unmanaged ISystem registration issues).
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    internal partial class SectorRefineTestDriverSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var wfcStateLookup = GetComponentLookup<WfcState>(true);
            var nodeIdLookup = GetComponentLookup<NodeId>(true);
            var connectionBufferLookup = GetBufferLookup<ConnectionBufferElement>();
            var gateBufferLookup = GetBufferLookup<GateConditionBufferElement>();

            uint baseSeed = (uint)(World.Unmanaged.Time.ElapsedTime * 997.0);
            var random = new Unity.Mathematics.Random(baseSeed == 0 ? 1u : baseSeed);
            float delta = World.Unmanaged.Time.DeltaTime;

            var job = new SectorRefinementJob
            {
                WfcStateLookup = wfcStateLookup,
                NodeIdLookup = nodeIdLookup,
                ConnectionBufferLookup = connectionBufferLookup,
                GateBufferLookup = gateBufferLookup,
                Random = random,
                DeltaTime = delta
            };
            Dependency = job.ScheduleParallel(Dependency);
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
        public void SetUp()
        {
            _world = new World("SectorRefineTestWorld");
            _simGroup = _world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            var driver = _world.GetOrCreateSystemManaged<SectorRefineTestDriverSystem>();
            _simGroup.AddSystemToUpdateList(driver);
            _simGroup.SortSystems();
        }

        [TearDown]
        public void TearDown()
        {
            if (_world != null && _world.IsCreated)
                _world.Dispose();
        }

        [Test]
        public void PlanningPhase_Completes_WhenWfcCompleted()
        {
            var em = _world.EntityManager;
            var e = em.CreateEntity();
            em.AddComponentData(e, new WfcState(WfcGenerationState.Completed));
            em.AddComponentData(e, new SectorRefinementData(0.3f) { Phase = SectorRefinementPhase.Planning });

            _simGroup.Update();

            var data = em.GetComponentData<SectorRefinementData>(e);
            Assert.AreEqual(SectorRefinementPhase.LoopCreation, data.Phase, "Phase should advance to LoopCreation after planning with completed WFC.");
            Assert.GreaterOrEqual(data.CriticalPathLength, 6);
            Assert.LessOrEqual(data.CriticalPathLength, 14); // random.NextInt(6,15) upper exclusive
            Assert.AreEqual(0, data.LoopCount);
        }

        [Test]
        public void LoopCreation_PhaseAdvances_WhenTargetLoopsAlreadyReached()
        {
            var em = _world.EntityManager;
            var e = em.CreateEntity();
            em.AddComponentData(e, new SectorRefinementData(0.3f)
            {
                Phase = SectorRefinementPhase.LoopCreation,
                CriticalPathLength = 10,
                LoopCount = (int)(10 * 0.3f) // target loops already satisfied
            });
            em.AddBuffer<ConnectionBufferElement>(e); // required for loop phase logic path

            _simGroup.Update();

            var data = em.GetComponentData<SectorRefinementData>(e);
            Assert.AreEqual(SectorRefinementPhase.LockPlacement, data.Phase, "Should move to LockPlacement when loop target achieved.");
        }

        [Test]
        public void LockPlacement_AddsHardLocks_AndAdvancesToPathValidation()
        {
            var em = _world.EntityManager;
            var e = em.CreateEntity();
            em.AddComponentData(e, new SectorRefinementData(0.3f)
            {
                Phase = SectorRefinementPhase.LockPlacement,
                CriticalPathLength = 16,
                LoopCount = 5 // arbitrary non-zero to avoid PathValidation failure later
            });
            em.AddBuffer<GateConditionBufferElement>(e);

            _simGroup.Update();

            var data = em.GetComponentData<SectorRefinementData>(e);
            Assert.AreEqual(SectorRefinementPhase.PathValidation, data.Phase, "Should proceed to PathValidation after lock placement.");
            Assert.GreaterOrEqual(data.HardLockCount, 1, "At least one hard lock should be added.");
        }

        [Test]
        public void PathValidation_Completes_WhenLoopsAndLocksPresent()
        {
            var em = _world.EntityManager;
            var e = em.CreateEntity();
            em.AddComponentData(e, new SectorRefinementData(0.3f)
            {
                Phase = SectorRefinementPhase.PathValidation,
                CriticalPathLength = 12,
                LoopCount = 2,
                HardLockCount = 1
            });

            _simGroup.Update();

            var data = em.GetComponentData<SectorRefinementData>(e);
            Assert.AreEqual(SectorRefinementPhase.Completed, data.Phase, "Valid path setup should mark refinement Completed.");
        }

        [Test]
        public void PathValidation_Fails_WhenNoLoopsOrLocks()
        {
            var em = _world.EntityManager;
            var e = em.CreateEntity();
            em.AddComponentData(e, new SectorRefinementData(0.3f)
            {
                Phase = SectorRefinementPhase.PathValidation,
                CriticalPathLength = 11,
                LoopCount = 0,
                HardLockCount = 0
            });

            _simGroup.Update();

            var data = em.GetComponentData<SectorRefinementData>(e);
            Assert.AreEqual(SectorRefinementPhase.Failed, data.Phase, "Missing loops & locks over thresholds should Fail refinement.");
        }
    }
}
