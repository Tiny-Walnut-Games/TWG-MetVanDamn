using Unity.Entities;

namespace TinyWalnutGames.MetVD.Graph
{
#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
    /// <summary>
    /// Test wrapper for ProceduralRoomGeneratorSystem. Ensures the production system is present
    /// and allows legacy tests referencing *SystemTest types to drive updates explicitly.
    /// Non-destructive bridging; does not alter production system code.
    /// </summary>
    [DisableAutoCreation]
    public sealed partial class ProceduralRoomGeneratorSystemTest : SystemBase
    {
        private ProceduralRoomGeneratorSystem _managedSystem; // underlying managed system (editor/test build)
        private InitializationSystemGroup _initGroup;
        private bool _addedToGroup;

        protected override void OnCreate()
        {
            // Acquire (or create) Initialization group
            _initGroup = World.GetOrCreateSystemManaged<InitializationSystemGroup>();
            // Ensure production system exists (managed variant in editor/test builds)
            _managedSystem = World.GetOrCreateSystemManaged<ProceduralRoomGeneratorSystem>();
            // Add once to group if not already scheduled
            if (!_initGroup.Systems.Contains(_managedSystem))
            {
                _initGroup.AddSystemToUpdateList(_managedSystem);
                _addedToGroup = true;
                _initGroup.SortSystems();
            }
        }

        protected override void OnUpdate()
        {
            // Forward a single update tick to underlying system explicitly.
            // This allows tests that update only the *Test system to drive content generation.
            if (_managedSystem == null)
                return; // Safety guard (should not happen)
            _managedSystem.Update();
        }

        protected override void OnDestroy()
        {
            // Do not remove from group to avoid side-effects if other tests still expect it there.
            _managedSystem = null;
            _initGroup = null;
        }
    }
#endif
}
