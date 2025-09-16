using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Core
    {
    public struct WorldSeedData : IComponentData
        {
        public uint Value;
        }

    public struct WorldBoundsData : IComponentData
        {
        public float3 Center;
        public float3 Extents;
        }

    public struct WorldGenerationConfigData : IComponentData
        {
        public int TargetSectorCount;
        public float BiomeTransitionRadius;
        public byte EnableDebugVisualization; // 0/1 for bool
        public byte LogGenerationSteps;       // 0/1 for bool
        }
    }
