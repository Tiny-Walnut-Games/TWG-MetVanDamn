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
            // Get the managed system and update it via simulation group
            var simGroup = World.GetExistingSystemManaged<SimulationSystemGroup>();
            if (simGroup != null)
            {
                simGroup.Update();
            }
        }
    }
}
