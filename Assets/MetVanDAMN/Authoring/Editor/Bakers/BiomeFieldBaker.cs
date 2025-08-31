#if UNITY_EDITOR
using TinyWalnutGames.MetVD.Biome;
using Unity.Entities;

namespace TinyWalnutGames.MetVD.Authoring.Editor
	{
	public class BiomeFieldBaker : Baker<BiomeFieldAuthoring>
		{
		public override void Bake (BiomeFieldAuthoring authoring)
			{
			Entity entity = this.GetEntity(TransformUsageFlags.Dynamic);
			this.AddComponent(entity, new BiomeFieldData
				{
				PrimaryBiome = authoring.primaryBiome,
				SecondaryBiome = authoring.secondaryBiome,
				Strength = authoring.strength,
				Gradient = authoring.gradient
				});
			}
		}
	}
#endif
