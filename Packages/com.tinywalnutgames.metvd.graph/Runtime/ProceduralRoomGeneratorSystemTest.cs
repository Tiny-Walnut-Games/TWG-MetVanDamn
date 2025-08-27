using Unity.Entities;
using Unity.Burst;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// SystemBase test wrapper for ProceduralRoomGeneratorSystem (ISystem production variant)
    /// Provides .Update() for existing unit tests expecting managed systems.
    /// Forwards to underlying ISystem via World.Unmanaged.GetUnsafeSystemRef<T>().
    /// </summary>
    [DisableAutoCreation]
    public partial class ProceduralRoomGeneratorSystemTest : SystemBase
    {
        protected override void OnCreate()
        {
            // Ensure the unmanaged system is created
            World.Unmanaged.GetOrCreateUnmanagedSystem<ProceduralRoomGeneratorSystem>();
        }
        
        protected override void OnUpdate()
        {
            // Forward to the unmanaged system using correct API
            var systemHandle = World.Unmanaged.GetExistingUnmanagedSystem<ProceduralRoomGeneratorSystem>();
            ref var system = ref World.Unmanaged.GetUnsafeSystemRef<ProceduralRoomGeneratorSystem>(systemHandle);
            system.OnUpdate(ref CheckedStateRef);
        }
    }
}
