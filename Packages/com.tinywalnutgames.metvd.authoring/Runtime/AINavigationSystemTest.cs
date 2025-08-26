#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
using Unity.Entities;

namespace TinyWalnutGames.MetVD.Authoring
{
    [DisableAutoCreation]
    public sealed partial class AINavigationSystemTest : SystemBase
    {
        protected override void OnCreate() { }
        protected override void OnUpdate() { /* no-op wrapper */ }
    }
}
#endif
