using Unity.Entities;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// Test alias for CinemachineZoneGeneratorSystem.
    /// Since CinemachineZoneGeneratorSystem is already a SystemBase, tests can use it directly.
    /// This provides name compatibility for any tests expecting CinemachineZoneGeneratorSystemTest.
    /// </summary>
    public partial class CinemachineZoneGeneratorSystemTest : CinemachineZoneGeneratorSystem
    {
        // Inherits all functionality from CinemachineZoneGeneratorSystem
    }
}
