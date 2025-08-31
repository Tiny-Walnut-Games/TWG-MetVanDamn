using Unity.Entities;

namespace TinyWalnutGames.MetVD.Graph
	{
	/// <summary>
	/// SystemBase test wrapper for ProceduralRoomGeneratorSystem (ISystem production variant)
	/// Provides .Update() for existing unit tests expecting managed systems.
	/// Uses SimulationSystemGroup to drive the unmanaged system properly.
	/// </summary>
	[DisableAutoCreation]
	public partial class ProceduralRoomGeneratorSystemTest : SystemBase
		{
		protected override void OnCreate ()
			{
			// Nothing needed for setup
			}

		protected override void OnUpdate ()
			{
			// Drive the unmanaged system through SimulationSystemGroup
			SimulationSystemGroup simGroup = this.World.GetOrCreateSystemManaged<SimulationSystemGroup>();
			// ProceduralRoomGeneratorSystem is automatically part of SimulationSystemGroup via [UpdateInGroup]
			// Removed invalid call: simGroup.RequireForUpdate(World.Unmanaged);
			// If you need to ensure the group updates, you can call simGroup.Update() directly if appropriate:
			simGroup.Update();
			}
		}
	}
