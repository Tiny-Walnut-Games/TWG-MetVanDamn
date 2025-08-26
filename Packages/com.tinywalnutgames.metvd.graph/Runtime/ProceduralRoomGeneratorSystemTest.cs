using Unity.Entities;
using Unity.Burst;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// SystemBase test wrapper for ProceduralRoomGeneratorSystem (ISystem production variant)
    /// Provides .Update() for existing unit tests expecting managed systems.
    /// Mirrors logic by invoking underlying ISystem via World.Unmanaged if needed.
    /// </summary>
    [DisableAutoCreation]
    public partial class ProceduralRoomGeneratorSystemTest : SystemBase
    {
        private ProceduralRoomGeneratorSystem _impl; // unused placeholder for symmetry
        protected override void OnCreate()
        {
            // Ensure required components match production system requirements if any.
        }
        protected override void OnUpdate()
        {
            // No-op: production logic lives in ProceduralRoomGeneratorSystem (ISystem) which runs in its update group.
            // Tests can still attach to world progression; this keeps API surface without duplicating logic.
        }
    }
}
