#if UNITY_EDITOR
using Unity.Entities;
using TinyWalnutGames.MetVD.Authoring;
using TinyWalnutGames.MetVD.Biome;

namespace TinyWalnutGames.MetVD.Authoring.Editor
{
    public class BiomeFieldBaker : Baker<BiomeFieldAuthoring>
    {
        public override void Bake(BiomeFieldAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new BiomeFieldData
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
