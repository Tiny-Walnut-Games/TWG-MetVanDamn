using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Core
    {
    public readonly partial struct WorldConfigAspect : IAspect
        {
        public readonly RefRO<WorldSeedData> Seed;
        public readonly RefRO<WorldBoundsData> Bounds;
        public readonly RefRW<WorldGenerationConfigData> Generation;

        public uint WorldSeed => Seed.ValueRO.Value;
        public float3 Center => Bounds.ValueRO.Center;
        public float3 Extents => Bounds.ValueRO.Extents;
        public int TargetSectors { get => Generation.ValueRO.TargetSectorCount; set => Generation.ValueRW.TargetSectorCount = value; }
        public float BiomeTransitionRadius { get => Generation.ValueRO.BiomeTransitionRadius; set => Generation.ValueRW.BiomeTransitionRadius = value; }
        public bool DebugViz { get => Generation.ValueRO.EnableDebugVisualization != 0; set => Generation.ValueRW.EnableDebugVisualization = (byte)(value ? 1 : 0); }
        public bool LogSteps { get => Generation.ValueRO.LogGenerationSteps != 0; set => Generation.ValueRW.LogGenerationSteps = (byte)(value ? 1 : 0); }
        }
    }
