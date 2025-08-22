#if UNITY_EDITOR
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Authoring;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Authoring.Editor
{
    public class WorldConfigurationBaker : Baker<WorldConfigurationAuthoring>
    {
        public override void Bake(WorldConfigurationAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new WorldConfiguration
            {
                Seed = authoring.seed,
                WorldSize = authoring.worldSize,
                TargetSectors = authoring.targetSectors
            });
        }
    }
}
#endif
