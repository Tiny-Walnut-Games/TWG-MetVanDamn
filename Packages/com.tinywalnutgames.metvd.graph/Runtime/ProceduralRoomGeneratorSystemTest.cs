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
            // The managed system will be created automatically
        }
        
        protected override void OnUpdate()
        {
            // Get the managed system and update it
            var system = World.GetExistingSystemManaged<ProceduralRoomGeneratorSystem>();
            if (system != null)
            {
                system.Update();
            }
        }
    }
}
