using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using TinyWalnutGames.MetVD.Biome;

namespace TinyWalnutGames.MetVD.Samples
{
    /// <summary>
    /// Smoke test scene setup for MetVanDAMN engine
    /// Provides immediate "hit Play -> see map" experience for validation
    /// </summary>
    public class SmokeTestSceneSetup : MonoBehaviour
    {
        [Header("World Generation Parameters")]
        [SerializeField] private uint worldSeed = 42;
        [SerializeField] private int2 worldSize = new(50, 50);
        [SerializeField] private int targetSectorCount = 5;
        [SerializeField] private float biomeTransitionRadius = 10.0f;
        
        [Header("Debug Visualization")]
        [SerializeField] private bool enableDebugVisualization = true;
        [SerializeField] private bool logGenerationSteps = true;
        
        private EntityManager entityManager;
        private World defaultWorld;
        
        void Start()
        {
            SetupSmokeTestWorld();
        }
        
        void Update()
        {
            // Periodically re-draw bounds if debug visualization enabled (consumes field each frame)
            if (enableDebugVisualization && Time.frameCount % 120 == 0)
            {
                DebugDrawBounds();
            }
        }
        
#if UNITY_EDITOR
        void OnValidate()
        {
            // Respond immediately in editor when toggled so value is meaningfully used
            if (Application.isPlaying == false && enableDebugVisualization)
            {
                DebugDrawBounds();
            }
        }
#endif
        
        void SetupSmokeTestWorld()
        {
            defaultWorld = World.DefaultGameObjectInjectionWorld;
            entityManager = defaultWorld.EntityManager;
            
            if (logGenerationSteps)
            {
                Debug.Log("🚀 MetVanDAMN Smoke Test: Starting world generation...");
            }
            
            CreateWorldConfiguration();
            CreateDistrictEntities();
            CreateBiomeFieldEntities();
            
            if (enableDebugVisualization)
            {
                DebugDrawBounds();
            }
            
            if (logGenerationSteps)
            {
                Debug.Log($"✅ MetVanDAMN Smoke Test: World setup complete with seed {worldSeed}");
                Debug.Log($"   World size: {worldSize.x}x{worldSize.y}");
                Debug.Log($"   Target sectors: {targetSectorCount}");
                Debug.Log("   Systems will begin generation on next frame.");
            }
        }
        
        void CreateWorldConfiguration()
        {
            var configEntity = entityManager.CreateEntity();
            entityManager.SetName(configEntity, "WorldConfiguration");
            
            entityManager.AddComponentData(configEntity, new WorldSeed { Value = worldSeed });
            entityManager.AddComponentData(configEntity, new WorldBounds 
            { 
                Min = new int2(-worldSize.x / 2, -worldSize.y / 2),
                Max = new int2(worldSize.x / 2, worldSize.y / 2)
            });
            
            // Integrate targetSectorCount with generation pipeline
            entityManager.AddComponentData(configEntity, new WorldGenerationConfig
            {
                TargetSectorCount = targetSectorCount,
                MaxDistrictCount = targetSectorCount * 4, // Allow room for subdivision
                BiomeTransitionRadius = biomeTransitionRadius
            });
        }
        
        void CreateDistrictEntities()
        {
            // Use targetSectorCount to determine how many districts to create
            int actualDistrictCount = math.min(targetSectorCount, 24); // Reasonable upper limit
            int gridSize = (int)math.ceil(math.sqrt(actualDistrictCount));
            
            var hubEntity = entityManager.CreateEntity();
            entityManager.SetName(hubEntity, "HubDistrict");
            
            entityManager.AddComponentData(hubEntity, new NodeId 
            { 
                Coordinates = int2.zero,
                Level = 0,
                Value = 0,
                ParentId = 0
            });
            
            entityManager.AddComponentData(hubEntity, new WfcState());
            entityManager.AddBuffer<WfcCandidateBufferElement>(hubEntity);
            entityManager.AddBuffer<ConnectionBufferElement>(hubEntity);
            
            int districtId = 1;
            int districtsCreated = 0;
            int halfGrid = gridSize / 2;
            
            for (int x = -halfGrid; x <= halfGrid && districtsCreated < actualDistrictCount; x++)
            {
                for (int y = -halfGrid; y <= halfGrid && districtsCreated < actualDistrictCount; y++)
                {
                    if (x == 0 && y == 0) continue; // Skip hub position
                    
                    var districtEntity = entityManager.CreateEntity();
                    entityManager.SetName(districtEntity, $"District_{x}_{y}");
                    
                    entityManager.AddComponentData(districtEntity, new NodeId 
                    { 
                        Coordinates = new int2(x * 10, y * 10),
                        Level = (byte)(math.abs(x) + math.abs(y)),
                        Value = (uint)districtId++,
                        ParentId = 0
                    });
                    
                    entityManager.AddComponentData(districtEntity, new WfcState());
                    entityManager.AddBuffer<WfcCandidateBufferElement>(districtEntity);
                    entityManager.AddBuffer<ConnectionBufferElement>(districtEntity);
                    entityManager.AddComponentData(districtEntity, new SectorRefinementData(0.3f));
                    entityManager.AddBuffer<GateConditionBufferElement>(districtEntity);
                    
                    districtsCreated++;
                }
            }
            
            if (logGenerationSteps)
            {
                Debug.Log($"Created {districtsCreated} districts based on targetSectorCount ({targetSectorCount})");
            }
        }
        
        void CreateBiomeFieldEntities()
        {
            CreatePolarityField(Polarity.Sun, new float2(15, 15), "SunField");
            CreatePolarityField(Polarity.Moon, new float2(-15, -15), "MoonField");
            CreatePolarityField(Polarity.Heat, new float2(15, -15), "HeatField");
            CreatePolarityField(Polarity.Cold, new float2(-15, 15), "ColdField");
        }
        
        void CreatePolarityField(Polarity polarity, float2 center, string name)
        {
            var fieldEntity = entityManager.CreateEntity();
            entityManager.SetName(fieldEntity, name);
            
            entityManager.AddComponentData(fieldEntity, new PolarityFieldData
            {
                Polarity = polarity,
                Center = center,
                Radius = biomeTransitionRadius,
                Strength = 0.8f
            });
        }

        void DebugDrawBounds()
        {
            var color = new Color(0.2f, 0.9f, 0.4f, 0.6f);
            var half = new float3(worldSize.x * 0.5f, 0, worldSize.y * 0.5f);
            Debug.DrawLine(new Vector3(-half.x, 0, -half.z), new Vector3(half.x, 0, -half.z), color, 1f);
            Debug.DrawLine(new Vector3(half.x, 0, -half.z), new Vector3(half.x, 0, half.z), color, 1f);
            Debug.DrawLine(new Vector3(half.x, 0, half.z), new Vector3(-half.x, 0, half.z), color, 1f);
            Debug.DrawLine(new Vector3(-half.x, 0, half.z), new Vector3(-half.x, 0, -half.z), color, 1f);
        }

        void OnDestroy()
        {
            if (logGenerationSteps)
            {
                Debug.Log("🔚 MetVanDAMN Smoke Test: Scene cleanup complete");
            }
        }
    }
    
    public struct WorldSeed : IComponentData
    {
        public uint Value;
    }
    
    public struct WorldBounds : IComponentData
    {
        public int2 Min;
        public int2 Max;
    }
    
    public struct PolarityFieldData : IComponentData
    {
        public Polarity Polarity;
        public float2 Center;
        public float Radius;
        public float Strength;
    }
    
    /// <summary>
    /// World generation configuration that integrates targetSectorCount with the generation pipeline
    /// </summary>
    public struct WorldGenerationConfig : IComponentData
    {
        public int TargetSectorCount;
        public int MaxDistrictCount;
        public float BiomeTransitionRadius;
    }
}
