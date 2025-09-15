using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVanDAMN.Authoring
    {
    [DisallowMultipleComponent]
    public sealed class WorldAuthoring : MonoBehaviour
        {
        [Header("World Configuration")]
        public uint worldSeed = 42;
        [Tooltip("World size extents (full size), centered at this GameObject's position.")]
        public Vector3 worldSize = new(50f, 50f, 0f);

        [Header("Generation Settings")]
        public int targetSectorCount = 5;
        public float biomeTransitionRadius = 10f;
        public bool enableDebugVisualization = true;
        public bool logGenerationSteps = true;

        class Baker : Baker<WorldAuthoring>
            {
            public override void Bake(WorldAuthoring authoring)
                {
                var e = GetEntity(TransformUsageFlags.None);
                AddComponent(e, new WorldSeedData { Value = authoring.worldSeed });
                var center = (float3)authoring.transform.position;
                var extents = (float3)(authoring.worldSize * 0.5f);
                AddComponent(e, new WorldBoundsData { Center = center, Extents = extents });
                AddComponent(e, new WorldGenerationConfigData
                    {
                    TargetSectorCount = authoring.targetSectorCount,
                    BiomeTransitionRadius = authoring.biomeTransitionRadius,
                    EnableDebugVisualization = (byte)(authoring.enableDebugVisualization ? 1 : 0),
                    LogGenerationSteps = (byte)(authoring.logGenerationSteps ? 1 : 0)
                    });
                }
            }
        }
    }
