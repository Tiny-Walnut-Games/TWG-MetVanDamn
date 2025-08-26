using Unity.Entities;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// SystemBase test wrapper for RoomNavigationGeneratorSystem (ISystem production variant)
    /// Provides .Update() for existing unit tests expecting managed systems.
    /// Forwards to underlying ISystem via World.Unmanaged API.
    /// </summary>
    [DisableAutoCreation]
    public sealed partial class RoomNavigationGeneratorSystemTest : SystemBase
    {
        private SystemHandle _unmanagedHandle;
        
        protected override void OnCreate()
        {
            _unmanagedHandle = World.Unmanaged.GetOrCreateSystem<RoomNavigationGeneratorSystem>();
        }
        
        protected override void OnUpdate()
        {
            if (!_unmanagedHandle.IsValid)
                _unmanagedHandle = World.Unmanaged.GetExistingSystem<RoomNavigationGeneratorSystem>();
            if (_unmanagedHandle.IsValid)
            {
                ref var system = ref World.Unmanaged.GetUnsafeSystemRef<RoomNavigationGeneratorSystem>(_unmanagedHandle);
                system.OnUpdate(ref CheckedStateRef);
            }
        }
    }
}
