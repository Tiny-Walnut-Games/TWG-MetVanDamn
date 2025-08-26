using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// Configuration for world bootstrap generation
    /// </summary>
    public struct WorldBootstrapConfiguration : IComponentData
    {
        public uint Seed;
        public int2 WorldSize;
        public RandomizationMode RandomizationMode;
        public BiomeGenerationSettings BiomeSettings;
        public DistrictGenerationSettings DistrictSettings;
        public SectorGenerationSettings SectorSettings;
        public RoomGenerationSettings RoomSettings;
        public bool EnableDebugVisualization;
        public bool LogGenerationSteps;

        // Convenience properties for backward compatibility
        public int2 BiomeCountRange => BiomeSettings.BiomeCountRange;
        public int2 DistrictCountRange => DistrictSettings.DistrictCountRange;
        public float DistrictMinDistance => DistrictSettings.DistrictMinDistance;

        public WorldBootstrapConfiguration(uint seed, int2 worldSize, RandomizationMode randomizationMode,
            BiomeGenerationSettings biomeSettings, DistrictGenerationSettings districtSettings,
            SectorGenerationSettings sectorSettings, RoomGenerationSettings roomSettings,
            bool enableDebugVisualization, bool logGenerationSteps)
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
        
        // Alternate constructor matching test expectations
        public WorldBootstrapConfiguration(WorldSettings worldSettings, BiomeSettings biomeSettings,
            DistrictSettings districtSettings, DebugSettings debugSettings)
        {
            Seed = worldSettings.Seed;
            WorldSize = worldSettings.WorldSize;
            RandomizationMode = worldSettings.RandomizationMode;
            BiomeSettings = new BiomeGenerationSettings
            {
                BiomeCountRange = biomeSettings.BiomeCountRange,
                BiomeWeight = biomeSettings.BiomeWeight
            };
            DistrictSettings = new DistrictGenerationSettings
            {
                DistrictCountRange = districtSettings.DistrictCountRange,
                DistrictMinDistance = districtSettings.DistrictMinDistance,
                DistrictWeight = districtSettings.DistrictWeight
            };
            SectorSettings = new SectorGenerationSettings
            {
                SectorsPerDistrictRange = districtSettings.SectorsPerDistrictRange,
                SectorGridSize = districtSettings.SectorGridSize
            };
            RoomSettings = new RoomGenerationSettings
            {
                RoomsPerSectorRange = districtSettings.RoomsPerSectorRange,
                TargetLoopDensity = districtSettings.TargetLoopDensity
            };
            EnableDebugVisualization = debugSettings.EnableDebugVisualization;
            LogGenerationSteps = debugSettings.LogGenerationSteps;
        }
    }

    /// <summary>
    /// Biome generation settings
    /// </summary>
    public struct BiomeGenerationSettings
    {
        public int2 BiomeCountRange;
        public float BiomeWeight;
    }

    /// <summary>
    /// District generation settings
    /// </summary>
    public struct DistrictGenerationSettings
    {
        public int2 DistrictCountRange;
        public float DistrictMinDistance;
        public float DistrictWeight;
    }

    /// <summary>
    /// Sector generation settings
    /// </summary>
    public struct SectorGenerationSettings
    {
        public int2 SectorsPerDistrictRange;
        public int2 SectorGridSize;
    }

    /// <summary>
    /// Room generation settings
    /// </summary>
    public struct RoomGenerationSettings
    {
        public int2 RoomsPerSectorRange;
        public float TargetLoopDensity;
    }

    // Test compatibility structs
    public struct WorldSettings
    {
        public uint Seed;
        public int2 WorldSize;
        public RandomizationMode RandomizationMode;
        
        public WorldSettings(uint seed, int2 worldSize, RandomizationMode randomizationMode)
        {
            Seed = seed;
            WorldSize = worldSize;
            RandomizationMode = randomizationMode;
        }
    }

    public struct BiomeSettings
    {
        public int2 BiomeCountRange;
        public float BiomeWeight;
        
        public BiomeSettings(int2 biomeCountRange, float biomeWeight)
        {
            BiomeCountRange = biomeCountRange;
            BiomeWeight = biomeWeight;
        }
    }

    public struct DistrictSettings
    {
        public int2 DistrictCountRange;
        public float DistrictMinDistance;
        public float DistrictWeight;
        public int2 SectorsPerDistrictRange;
        public int2 SectorGridSize;
        public int2 RoomsPerSectorRange;
        public float TargetLoopDensity;
        
        public DistrictSettings(int2 districtCountRange, float districtMinDistance, float districtWeight,
            int2 sectorsPerDistrictRange, int2 sectorGridSize, int2 roomsPerSectorRange, float targetLoopDensity)
        {
            DistrictCountRange = districtCountRange;
            DistrictMinDistance = districtMinDistance;
            DistrictWeight = districtWeight;
            SectorsPerDistrictRange = sectorsPerDistrictRange;
            SectorGridSize = sectorGridSize;
            RoomsPerSectorRange = roomsPerSectorRange;
            TargetLoopDensity = targetLoopDensity;
        }
    }

    public struct SectorSettings
    {
        public int2 SectorsPerDistrictRange;
        public int2 SectorGridSize;
        public int2 RoomsPerSectorRange;
        public float TargetLoopDensity;
        
        public SectorSettings(int2 sectorsPerDistrictRange, int2 sectorGridSize, int2 roomsPerSectorRange, float targetLoopDensity)
        {
            SectorsPerDistrictRange = sectorsPerDistrictRange;
            SectorGridSize = sectorGridSize;
            RoomsPerSectorRange = roomsPerSectorRange;
            TargetLoopDensity = targetLoopDensity;
        }
    }

    public struct RoomSettings
    {
        public int2 RoomsPerSectorRange;
        public float TargetLoopDensity;
        
        public RoomSettings(int2 roomsPerSectorRange, float targetLoopDensity)
        {
            RoomsPerSectorRange = roomsPerSectorRange;
            TargetLoopDensity = targetLoopDensity;
        }
    }

    public struct DebugSettings
    {
        public bool EnableDebugVisualization;
        public bool LogGenerationSteps;
        
        public DebugSettings(bool enableDebugVisualization, bool logGenerationSteps)
        {
            EnableDebugVisualization = enableDebugVisualization;
            LogGenerationSteps = logGenerationSteps;
        }
    }

    /// <summary>
    /// Tag component to mark world bootstrap in progress
    /// </summary>
    public struct WorldBootstrapInProgressTag : IComponentData { }

    /// <summary>
    /// Tag component to mark world bootstrap complete
    /// </summary>
    public struct WorldBootstrapCompleteTag : IComponentData { }
}