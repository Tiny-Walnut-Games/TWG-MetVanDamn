#if UNITY_EDITOR
using TinyWalnutGames.MetVD.Shared;
using Unity.Entities;

namespace TinyWalnutGames.MetVD.Authoring.Editor
	{
	public class WorldConfigurationBaker : Baker<WorldConfigurationAuthoring>
		{
		public override void Bake (WorldConfigurationAuthoring authoring)
			{
			Entity entity = this.GetEntity(TransformUsageFlags.None);
			this.AddComponent(entity, new WorldConfiguration
				{
				Seed = authoring.seed,
				WorldSize = authoring.worldSize,
				TargetSectors = authoring.targetSectors,
				RandomizationMode = authoring.randomizationMode
				});
			}
		}
	}
#endif
