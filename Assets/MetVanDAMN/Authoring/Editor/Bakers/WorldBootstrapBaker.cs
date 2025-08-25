#if UNITY_EDITOR
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Authoring;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Shared;

namespace TinyWalnutGames.MetVD.Authoring.Editor
{
    public class WorldBootstrapBaker : Baker<WorldBootstrapAuthoring>
    {
        public override void Bake(WorldBootstrapAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            
            // Convert authoring settings to component data
            var bootstrapConfig = new WorldBootstrapConfiguration(
                authoring.seed,
                authoring.worldSize,
                authoring.randomizationMode,
                new int2(authoring.biomeCount.x, authoring.biomeCount.y),
                authoring.biomeWeight,
                new int2(authoring.districtCount.x, authoring.districtCount.y),
                authoring.districtMinDistance,
                authoring.districtWeight,
                new int2(authoring.sectorsPerDistrict.x, authoring.sectorsPerDistrict.y),
                authoring.sectorGridSize,
                new int2(authoring.roomsPerSector.x, authoring.roomsPerSector.y),
                authoring.targetLoopDensity,
                authoring.enableDebugVisualization,
                authoring.logGenerationSteps
            );
            
            AddComponent(entity, bootstrapConfig);
            
            // Also create the standard WorldConfiguration for compatibility with existing systems
            AddComponent(entity, new WorldConfiguration
            {
                Seed = authoring.seed,
                WorldSize = authoring.worldSize,
                TargetSectors = authoring.sectorsPerDistrict.y * authoring.districtCount.y, // Max possible sectors
                RandomizationMode = authoring.randomizationMode
            });
        }
    }
}
#endif