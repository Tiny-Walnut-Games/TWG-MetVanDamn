using Unity.Entities;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// SystemBase test wrapper for RoomNavigationGeneratorSystem (ISystem production variant)
    /// Provides .Update() for existing unit tests expecting managed systems.
    /// Forwards to underlying ISystem via World.Unmanaged.GetUnsafeSystemRef<T>().
    /// </summary>
    [DisableAutoCreation]
    public sealed partial class RoomNavigationGeneratorSystemTest : SystemBase
    {
        protected override void OnCreate()
        {
            // The unmanaged system will be created automatically
        }
        
        protected override void OnUpdate()
        {
            // Get SystemState and call OnUpdate directly on the unmanaged system
            ref var systemState = ref this.CheckedStateRef;
            ref var unmanagedSystem = ref World.Unmanaged.GetUnsafeSystemRef<RoomNavigationGeneratorSystem>(
                World.Unmanaged.GetExistingUnmanagedSystem<RoomNavigationGeneratorSystem>());
            unmanagedSystem.OnUpdate(ref systemState);
        }
    }
}
