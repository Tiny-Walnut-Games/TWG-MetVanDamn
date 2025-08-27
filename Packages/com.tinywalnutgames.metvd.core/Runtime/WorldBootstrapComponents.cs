using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Shared;

namespace TinyWalnutGames.MetVD.Core
{
    /// <summary>
    /// Component data for world bootstrap configuration
    /// Contains all settings needed for procedural world generation
    /// </summary>
    // Grouped settings structs
    public struct BiomeGenerationSettings
    {
        public int2 BiomeCountRange;  // x = min, y = max
        public float BiomeWeight;

        public BiomeGenerationSettings(int2 biomeCountRange, float biomeWeight)
        {
            BiomeCountRange = biomeCountRange;
            BiomeWeight = biomeWeight;
        }
    }

    public struct DistrictGenerationSettings
    {
        public int2 DistrictCountRange;  // x = min, y = max
        public float DistrictMinDistance;
        public float DistrictWeight;

        public DistrictGenerationSettings(int2 districtCountRange, float districtMinDistance, float districtWeight)
        {
            DistrictCountRange = districtCountRange;
            DistrictMinDistance = districtMinDistance;
            DistrictWeight = districtWeight;
        }
    }

    public struct SectorGenerationSettings
    {
        public int2 SectorsPerDistrictRange;  // x = min, y = max
        public int2 SectorGridSize;

        public SectorGenerationSettings(int2 sectorsPerDistrictRange, int2 sectorGridSize)
        {
            SectorsPerDistrictRange = sectorsPerDistrictRange;
            SectorGridSize = sectorGridSize;
        }
    }

    public struct RoomGenerationSettings
    {
        public int2 RoomsPerSectorRange;  // x = min, y = max
        public float TargetLoopDensity;
        public int CorridorWidth; // Add this field

        public RoomGenerationSettings(int2 roomsPerSectorRange, float targetLoopDensity)
        {
            RoomsPerSectorRange = roomsPerSectorRange;
            TargetLoopDensity = targetLoopDensity;
            CorridorWidth = 0; // Default value
        }

        // three argument constructor for convenience
        public RoomGenerationSettings(int2 roomsPerSectorRange, float targetLoopDensity, int corridorWidth)
        {
            RoomsPerSectorRange = roomsPerSectorRange;
            TargetLoopDensity = targetLoopDensity;
            CorridorWidth = corridorWidth;
        }
    }

    public struct WorldBootstrapConfiguration : IComponentData
    {
        // World-level settings
        public int Seed;
        public int2 WorldSize;
        public RandomizationMode RandomizationMode;

        // Grouped settings
        public BiomeGenerationSettings BiomeSettings;
        public DistrictGenerationSettings DistrictSettings;
        public SectorGenerationSettings SectorSettings;
        public RoomGenerationSettings RoomSettings;

        // Debug settings
        public bool EnableDebugVisualization;
        public bool LogGenerationSteps;

        public WorldBootstrapConfiguration(
            int seed,
            int2 worldSize,
            RandomizationMode randomizationMode,
            BiomeGenerationSettings biomeSettings,
            DistrictGenerationSettings districtSettings,
            SectorGenerationSettings sectorSettings,
            RoomGenerationSettings roomSettings,
            bool enableDebugVisualization,
            bool logGenerationSteps)
        {
            Seed = seed;
            WorldSize = worldSize;
            RandomizationMode = randomizationMode;
            BiomeSettings = biomeSettings;
            DistrictSettings = districtSettings;
            SectorSettings = sectorSettings;
            RoomSettings = roomSettings;
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
