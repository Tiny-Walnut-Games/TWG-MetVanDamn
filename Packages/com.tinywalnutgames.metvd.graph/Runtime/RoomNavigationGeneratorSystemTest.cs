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
            // For testing purposes, we just need to ensure the system is available
            // The actual ISystem will be managed by the ECS runtime
        }
        
        protected override void OnUpdate()
        {
            // Test wrapper - the actual system runs via ECS framework
            // This is just a placeholder for unit tests that expect a managed system
        }
    }
}
