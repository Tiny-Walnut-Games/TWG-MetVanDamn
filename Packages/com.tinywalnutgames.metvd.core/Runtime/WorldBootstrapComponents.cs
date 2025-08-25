using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Shared;

namespace TinyWalnutGames.MetVD.Core
{
    /// <summary>
    /// Component data for world bootstrap configuration
    /// Contains all settings needed for procedural world generation
    /// </summary>
    public struct WorldBootstrapConfiguration : IComponentData
    {
        // World-level settings
        public int Seed;
        public int2 WorldSize;
        public RandomizationMode RandomizationMode;
        
        // Biome generation settings
        public int2 BiomeCountRange;  // x = min, y = max
        public float BiomeWeight;
        
        // District generation settings
        public int2 DistrictCountRange;  // x = min, y = max
        public float DistrictMinDistance;
        public float DistrictWeight;
        
        // Sector generation settings
        public int2 SectorsPerDistrictRange;  // x = min, y = max
        public int2 SectorGridSize;
        
        // Room generation settings
        public int2 RoomsPerSectorRange;  // x = min, y = max
        public float TargetLoopDensity;
        
        // Debug settings
        public bool EnableDebugVisualization;
        public bool LogGenerationSteps;

        public WorldBootstrapConfiguration(
            int seed, int2 worldSize, RandomizationMode randomizationMode,
            int2 biomeCountRange, float biomeWeight,
            int2 districtCountRange, float districtMinDistance, float districtWeight,
            int2 sectorsPerDistrictRange, int2 sectorGridSize,
            int2 roomsPerSectorRange, float targetLoopDensity,
            bool enableDebugVisualization, bool logGenerationSteps)
        {
            Seed = seed;
            WorldSize = worldSize;
            RandomizationMode = randomizationMode;
            BiomeCountRange = biomeCountRange;
            BiomeWeight = biomeWeight;
            DistrictCountRange = districtCountRange;
            DistrictMinDistance = districtMinDistance;
            DistrictWeight = districtWeight;
            SectorsPerDistrictRange = sectorsPerDistrictRange;
            SectorGridSize = sectorGridSize;
            RoomsPerSectorRange = roomsPerSectorRange;
            TargetLoopDensity = targetLoopDensity;
            EnableDebugVisualization = enableDebugVisualization;
            LogGenerationSteps = logGenerationSteps;
        }
    }

    /// <summary>
    /// Tag component indicating that world bootstrap generation is in progress
    /// </summary>
    public struct WorldBootstrapInProgressTag : IComponentData { }

    /// <summary>
    /// Tag component indicating that world bootstrap generation has completed
    /// </summary>
    public struct WorldBootstrapCompleteTag : IComponentData
    {
        public int BiomesGenerated;
        public int DistrictsGenerated;
        public int SectorsGenerated;
        public int RoomsGenerated;

        public WorldBootstrapCompleteTag(int biomes, int districts, int sectors, int rooms)
        {
            BiomesGenerated = biomes;
            DistrictsGenerated = districts;
            SectorsGenerated = sectors;
            RoomsGenerated = rooms;
        }
    }
}