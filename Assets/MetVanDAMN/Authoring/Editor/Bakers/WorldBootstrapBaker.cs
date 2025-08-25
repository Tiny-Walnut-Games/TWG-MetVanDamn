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
            // Grouped settings structs for new constructor signature
            var biomeSettings = new BiomeGenerationSettings
            {
                BiomeCount = new int2(authoring.biomeCount.x, authoring.biomeCount.y),
                BiomeWeight = authoring.biomeWeight
            };
            var districtSettings = new DistrictGenerationSettings
            {
                DistrictCount = new int2(authoring.districtCount.x, authoring.districtCount.y),
                DistrictMinDistance = authoring.districtMinDistance,
                DistrictWeight = authoring.districtWeight
            };
            var sectorSettings = new SectorGenerationSettings
            {
                SectorsPerDistrict = new int2(authoring.sectorsPerDistrict.x, authoring.sectorsPerDistrict.y),
                SectorGridSize = authoring.sectorGridSize
            };
            var roomSettings = new RoomGenerationSettings
            {
                RoomsPerSector = new int2(authoring.roomsPerSector.x, authoring.roomsPerSector.y),
                TargetLoopDensity = authoring.targetLoopDensity
            };
            var bootstrapConfig = new WorldBootstrapConfiguration(
                authoring.seed,
                authoring.worldSize,
                authoring.randomizationMode,
                biomeSettings,
                districtSettings,
                sectorSettings,
                roomSettings,
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