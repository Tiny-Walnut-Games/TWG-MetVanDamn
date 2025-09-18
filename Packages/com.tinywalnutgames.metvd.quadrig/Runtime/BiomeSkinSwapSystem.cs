using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace TinyWalnutGames.MetVD.QuadRig
{
    /// <summary>
    /// System responsible for handling biome skin swapping for quad-rig humanoid characters.
    /// Enables instant material/texture changes without altering rig or animation data.
    /// Maintains alpha-mask silhouette integrity during transitions.
    /// </summary>
    [RequireMatchingQueriesForUpdate]
    public partial class BiomeSkinSwapSystem : SystemBase
    {
        private EntityQuery skinSwapQuery;
        private NativeHashMap<uint, Material> atlasIdToMaterial;
        private bool initialized;

        protected override void OnCreate()
        {
            skinSwapQuery = GetEntityQuery(
                ComponentType.ReadWrite<QuadRigHumanoid>(),
                ComponentType.ReadWrite<RenderMesh>(),
                ComponentType.ReadOnly<TextureAtlasData>()
            );

            RequireForUpdate(skinSwapQuery);
            atlasIdToMaterial = new NativeHashMap<uint, Material>(16, Allocator.Persistent);
            initialized = false;
        }

        protected override void OnDestroy()
        {
            if (atlasIdToMaterial.IsCreated)
            {
                atlasIdToMaterial.Dispose();
            }
        }

        protected override void OnUpdate()
        {
            if (!initialized)
            {
                InitializeMaterialCache();
                initialized = true;
            }

            // Process skin swap requests
            Entities
                .WithName("ProcessBiomeSkinSwaps")
                .ForEach((Entity entity, ref QuadRigHumanoid humanoid, ref RenderMesh renderMesh, in TextureAtlasData atlasData) =>
                {
                    // Check if atlas ID has changed (skin swap requested)
                    if (humanoid.CurrentAtlasId != atlasData.AtlasId)
                    {
                        SwapSkin(ref humanoid, ref renderMesh, atlasData);
                    }
                })
                .WithoutBurst()
                .Run();
        }

        /// <summary>
        /// Initializes the material cache with available biome atlases
        /// </summary>
        private void InitializeMaterialCache()
        {
            // Create materials for different biome types
            CreateBiomeMaterial(0, "Default", Color.white);
            CreateBiomeMaterial(1, "Forest", new Color(0.2f, 0.8f, 0.2f, 1.0f));
            CreateBiomeMaterial(2, "Desert", new Color(0.9f, 0.7f, 0.3f, 1.0f));
            CreateBiomeMaterial(3, "Ice", new Color(0.7f, 0.9f, 1.0f, 1.0f));
            CreateBiomeMaterial(4, "Volcanic", new Color(0.8f, 0.2f, 0.1f, 1.0f));
        }

        /// <summary>
        /// Creates a material for a specific biome type
        /// </summary>
        private void CreateBiomeMaterial(uint atlasId, string biomeName, Color tint)
        {
            var material = new Material(GetBiomeShader());
            material.name = $"BiomeAtlas_{biomeName}";
            
            // Set up alpha masking for silhouette integrity
            SetupAlphaMasking(material);
            
            // Apply biome tint
            material.SetColor("_Color", tint);
            
            // TODO: Load actual texture atlas for this biome
            // material.SetTexture("_MainTex", LoadBiomeAtlas(atlasId));
            
            atlasIdToMaterial.TryAdd(atlasId, material);
        }

        /// <summary>
        /// Gets the appropriate shader for biome materials
        /// </summary>
        private Shader GetBiomeShader()
        {
            // Try to find a more suitable shader, fall back to default
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }
            return shader ?? Shader.Find("Standard");
        }

        /// <summary>
        /// Sets up alpha masking properties for material
        /// </summary>
        private void SetupAlphaMasking(Material material)
        {
            // Enable alpha testing for clean silhouettes
            material.SetFloat("_Mode", 1); // Cutout mode
            material.SetFloat("_Cutoff", 0.5f);
            material.EnableKeyword("_ALPHATEST_ON");
            
            // Disable back face culling for billboard behavior
            material.SetFloat("_Cull", 0);
        }

        /// <summary>
        /// Performs the actual skin swap operation
        /// </summary>
        private void SwapSkin(ref QuadRigHumanoid humanoid, ref RenderMesh renderMesh, in TextureAtlasData atlasData)
        {
            // Get material for the new atlas
            if (atlasIdToMaterial.TryGetValue(atlasData.AtlasId, out var newMaterial))
            {
                // Update render mesh with new material
                renderMesh.material = newMaterial;
                
                // Update humanoid component to reflect the change
                humanoid.CurrentAtlasId = atlasData.AtlasId;
                
                Debug.Log($"🎨 Skin swapped to atlas {atlasData.AtlasId} for biome type {atlasData.BiomeType}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Material not found for atlas ID {atlasData.AtlasId}");
            }
        }

        /// <summary>
        /// Public method to trigger skin swap for specific entity
        /// </summary>
        public void RequestSkinSwap(Entity entity, uint newAtlasId)
        {
            if (EntityManager.HasComponent<TextureAtlasData>(entity))
            {
                var atlasData = EntityManager.GetComponentData<TextureAtlasData>(entity);
                atlasData.AtlasId = newAtlasId;
                EntityManager.SetComponentData(entity, atlasData);
            }
        }
    }

    /// <summary>
    /// Burst-compiled job for batch processing skin swaps.
    /// Optimized for performance when many characters need simultaneous skin changes.
    /// </summary>
    [BurstCompile]
    public partial struct BatchSkinSwapJob : IJobEntity
    {
        [ReadOnly] public uint TargetAtlasId;
        [ReadOnly] public uint TargetBiomeType;

        [BurstCompile]
        public void Execute(ref QuadRigHumanoid humanoid, ref TextureAtlasData atlasData)
        {
            // Only swap if this is for the target biome type
            if (atlasData.BiomeType == TargetBiomeType)
            {
                atlasData.AtlasId = TargetAtlasId;
            }
        }
    }

    /// <summary>
    /// Utility class for managing biome skin configurations and transitions.
    /// Provides high-level interface for skin swapping operations.
    /// </summary>
    public static class BiomeSkinUtility
    {
        /// <summary>
        /// Standard biome type mappings
        /// </summary>
        public enum BiomeType : uint
        {
            Default = 0,
            Forest = 1,
            Desert = 2,
            Ice = 3,
            Volcanic = 4,
            Ocean = 5,
            Sky = 6,
            Underground = 7
        }

        /// <summary>
        /// Creates a texture atlas configuration for a specific biome
        /// </summary>
        public static TextureAtlasData CreateBiomeAtlas(uint atlasId, BiomeType biomeType, int2 dimensions = default)
        {
            if (dimensions.Equals(default))
            {
                dimensions = new int2(512, 512); // Default atlas size
            }

            return new TextureAtlasData(atlasId, (uint)biomeType, dimensions, 10); // 10 parts for humanoid
        }

        /// <summary>
        /// Validates that a skin swap maintains alpha-mask integrity
        /// </summary>
        public static bool ValidateSkinSwapIntegrity(Material oldMaterial, Material newMaterial)
        {
            // Check that both materials have consistent alpha cutoff values
            var oldCutoff = oldMaterial.GetFloat("_Cutoff");
            var newCutoff = newMaterial.GetFloat("_Cutoff");
            
            return math.abs(oldCutoff - newCutoff) < 0.01f;
        }

        /// <summary>
        /// Gets the default UV mapping for a standard humanoid atlas
        /// </summary>
        public static NativeArray<float4> GetStandardHumanoidUVs(Allocator allocator)
        {
            var uvs = new NativeArray<float4>(10, allocator);
            
            // 4x4 atlas layout with 0.25f cell size
            uvs[0] = new float4(0, 0.75f, 0.25f, 0.25f);     // Head
            uvs[1] = new float4(0.25f, 0.75f, 0.25f, 0.25f); // Torso
            uvs[2] = new float4(0.5f, 0.75f, 0.25f, 0.25f);  // Left Arm
            uvs[3] = new float4(0.75f, 0.75f, 0.25f, 0.25f); // Right Arm
            uvs[4] = new float4(0, 0.5f, 0.25f, 0.25f);      // Left Leg
            uvs[5] = new float4(0.25f, 0.5f, 0.25f, 0.25f);  // Right Leg
            uvs[6] = new float4(0.5f, 0.5f, 0.25f, 0.25f);   // Left Hand
            uvs[7] = new float4(0.75f, 0.5f, 0.25f, 0.25f);  // Right Hand
            uvs[8] = new float4(0, 0.25f, 0.25f, 0.25f);     // Left Foot
            uvs[9] = new float4(0.25f, 0.25f, 0.25f, 0.25f); // Right Foot
            
            return uvs;
        }

        /// <summary>
        /// Creates a batch skin swap job for multiple entities
        /// </summary>
        public static BatchSkinSwapJob CreateBatchSwapJob(uint targetAtlasId, BiomeType targetBiomeType)
        {
            return new BatchSkinSwapJob
            {
                TargetAtlasId = targetAtlasId,
                TargetBiomeType = (uint)targetBiomeType
            };
        }
    }
}