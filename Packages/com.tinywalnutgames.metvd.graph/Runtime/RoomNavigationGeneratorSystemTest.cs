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
            // Ensure the unmanaged system is created
            World.Unmanaged.GetOrCreateSystem<RoomNavigationGeneratorSystem>();
        }
        
        protected override void OnUpdate()
        {
            // Forward to the unmanaged system
            ref var system = ref World.Unmanaged.GetUnsafeSystemRef<RoomNavigationGeneratorSystem>(
                World.Unmanaged.GetExistingSystem<RoomNavigationGeneratorSystem>());
            system.OnUpdate(ref CheckedStateRef);
        }
    }
}
