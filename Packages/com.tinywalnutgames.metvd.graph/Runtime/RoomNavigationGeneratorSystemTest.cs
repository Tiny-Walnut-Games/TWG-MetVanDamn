using Unity.Entities;

namespace TinyWalnutGames.MetVD.Graph
{
    [DisableAutoCreation]
    public sealed partial class RoomNavigationGeneratorSystemTest : SystemBase
    {
        protected override void OnCreate()
        {
            // No-op: test wrapper kept for backwards compatibility
        }
        protected override void OnUpdate()
        {
            // Intentionally left blank; legacy tests should be updated to drive SimulationSystemGroup
        }
    }
}
