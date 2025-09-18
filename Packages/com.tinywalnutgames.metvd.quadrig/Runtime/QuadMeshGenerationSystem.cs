using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace TinyWalnutGames.MetVD.QuadRig
    {
    // 🧙‍♂️ SACRED RENDERING BRIDGE: Mesh generation system bridging ECS data to Unity rendering pipeline
    // 🔧 ENHANCEMENT READY: Managed component placeholders for future Entities Graphics integration
    // 🍑 CHEEK PRESERVATION: Protects mesh integrity during biome skin swapping operations

    // Placeholder tag & managed component for rendering data (since RenderMesh isn't available)
    public struct QuadRigRenderedTag : IComponentData { }
    public class QuadRigMeshMaterial : IComponentData
        {
        public Mesh Mesh;
        public Material Material;

        public QuadRigMeshMaterial()
            {
            Mesh = new Mesh();
            Material = new Material(Shader.Find("Sprites/Default"));
            }
        }
    /// <summary>
    /// System responsible for generating and managing quad meshes for humanoid character parts.
    /// Creates GPU-friendly meshes with proper UV mapping for texture atlas support.
    /// Supports dynamic biome skin swapping through material changes.
    /// </summary>
    [RequireMatchingQueriesForUpdate]
    public partial class QuadMeshGenerationSystem : SystemBase
        {
        private EntityQuery quadRigQuery;
        private static readonly int AtlasTexturePropertyId = Shader.PropertyToID("_MainTex");
        private static readonly int AlphaMaskPropertyId = Shader.PropertyToID("_AlphaMask");

        protected override void OnCreate()
            {
            quadRigQuery = GetEntityQuery(
                ComponentType.ReadOnly<QuadRigHumanoid>(),
                ComponentType.ReadOnly<QuadMeshPart>(),
                ComponentType.ReadOnly<LocalTransform>()
            );

            RequireForUpdate(quadRigQuery);
            }

        protected override void OnUpdate()
            {
            // Process entities that need mesh generation
            Entities
                .WithName("GenerateQuadMeshes")
                .ForEach((Entity entity, in QuadRigHumanoid humanoid, in QuadMeshPart meshPart, in LocalTransform transform) =>
                {
                    GenerateQuadMeshForPart(entity, humanoid, meshPart, transform);
                })
                .WithoutBurst()
                .Run();
            }

        /// <summary>
        /// Generates a quad mesh for a specific character part with proper UV mapping
        /// </summary>
        private void GenerateQuadMeshForPart(Entity entity, QuadRigHumanoid humanoid, QuadMeshPart meshPart, LocalTransform transform)
            {
            // Create quad mesh data
            var mesh = CreateQuadMesh(meshPart.QuadSize, meshPart.UVRect);

            // Create material for this part
            var material = CreateQuadMaterial(humanoid.CurrentAtlasId, humanoid.EnableAlphaMask);

            // Set up rendering components
            SetupMeshRendering(entity, mesh, material);
            }

        /// <summary>
        /// Creates a quad mesh with specified size and UV coordinates
        /// </summary>
        private Mesh CreateQuadMesh(float2 size, float4 uvRect)
            {
            var mesh = new Mesh();
            mesh.name = "QuadRigMesh";

            // Vertices for a quad (centered)
            var vertices = new Vector3[4]
            {
                new (-size.x * 0.5f, -size.y * 0.5f, 0), // Bottom-left
                new (size.x * 0.5f, -size.y * 0.5f, 0),  // Bottom-right
                new (size.x * 0.5f, size.y * 0.5f, 0),   // Top-right
                new (-size.x * 0.5f, size.y * 0.5f, 0)   // Top-left
            };

            // UV coordinates mapped to atlas rectangle
            var uvs = new Vector2[4]
            {
                new (uvRect.x, uvRect.y),                           // Bottom-left
                new (uvRect.x + uvRect.z, uvRect.y),                // Bottom-right
                new (uvRect.x + uvRect.z, uvRect.y + uvRect.w),     // Top-right
                new (uvRect.x, uvRect.y + uvRect.w)                 // Top-left
            };

            // Triangles (two triangles make a quad)
            var triangles = new int[6]
            {
                0, 1, 2,  // First triangle
                0, 2, 3   // Second triangle
            };

            // Normals (all facing forward)
            var normals = new Vector3[4]
            {
                Vector3.back, Vector3.back, Vector3.back, Vector3.back
            };

            // Set mesh data
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.normals = normals;

            // Calculate bounds
            mesh.RecalculateBounds();

            return mesh;
            }

        /// <summary>
        /// Creates a material for quad rendering with atlas texture support
        /// </summary>
        private Material CreateQuadMaterial(uint atlasId, bool enableAlphaMask)
            {
            // In a real implementation, this would load the appropriate shader and texture
            // For now, create a basic material that demonstrates the concept
            var material = new Material(Shader.Find("Sprites/Default"));

            // Set material properties for alpha masking
            if (enableAlphaMask)
                {
                material.SetFloat("_Mode", 1); // Cutout mode
                material.SetFloat("_Cutoff", 0.5f);
                material.EnableKeyword("_ALPHATEST_ON");
                }

            // TODO: Load texture atlas based on atlasId
            // material.SetTexture(AtlasTexturePropertyId, LoadAtlasTexture(atlasId));

            return material;
            }

        /// <summary>
        /// Sets up ECS rendering components for the quad mesh
        /// </summary>
        private void SetupMeshRendering(Entity entity, Mesh mesh, Material material)
            {
            // TEMPORARY PLACEHOLDER:
            // The legacy Hybrid Renderer `RenderMesh` component is not available in this project setup.
            // A proper Entities Graphics path would use MaterialMeshInfo / MeshRenderer components.
            // For now we simply attach a tag component so future systems can pick it up.
            if (!EntityManager.HasComponent<QuadRigRenderedTag>(entity))
                {
                EntityManager.AddComponent<QuadRigRenderedTag>(entity);
                }

            // Store mesh/material references via a managed component until a DOTS graphics path is implemented.
            if (!EntityManager.HasComponent<QuadRigMeshMaterial>(entity))
                {
                EntityManager.AddComponentObject(entity, new QuadRigMeshMaterial { Mesh = mesh, Material = material });
                }
            else
                {
                var mm = EntityManager.GetComponentObject<QuadRigMeshMaterial>(entity);
                mm.Mesh = mesh;
                mm.Material = material;
                }
            }
        }

    /// <summary>
    /// Static utility class for quad mesh generation and management.
    /// Provides helper methods for creating standard humanoid quad configurations.
    /// </summary>
    public static class QuadMeshUtility
        {
        /// <summary>
        /// Standard quad sizes for different humanoid parts
        /// </summary>
        public static readonly Dictionary<QuadPartType, float2> StandardPartSizes = new()
        {
            { QuadPartType.Head, new float2(0.8f, 1.0f) },
            { QuadPartType.Torso, new float2(1.2f, 1.6f) },
            { QuadPartType.LeftArm, new float2(0.4f, 1.2f) },
            { QuadPartType.RightArm, new float2(0.4f, 1.2f) },
            { QuadPartType.LeftLeg, new float2(0.5f, 1.4f) },
            { QuadPartType.RightLeg, new float2(0.5f, 1.4f) },
            { QuadPartType.LeftHand, new float2(0.3f, 0.4f) },
            { QuadPartType.RightHand, new float2(0.3f, 0.4f) },
            { QuadPartType.LeftFoot, new float2(0.4f, 0.2f) },
            { QuadPartType.RightFoot, new float2(0.4f, 0.2f) }
        };

        /// <summary>
        /// Creates standard UV rectangles for a 4x4 atlas layout
        /// </summary>
        public static Dictionary<QuadPartType, float4> CreateStandardAtlasUVs()
            {
            const float cellSize = 0.25f; // 4x4 grid

            return new Dictionary<QuadPartType, float4>
            {
                { QuadPartType.Head, new float4(0, 0.75f, cellSize, cellSize) },
                { QuadPartType.Torso, new float4(0.25f, 0.75f, cellSize, cellSize) },
                { QuadPartType.LeftArm, new float4(0.5f, 0.75f, cellSize, cellSize) },
                { QuadPartType.RightArm, new float4(0.75f, 0.75f, cellSize, cellSize) },
                { QuadPartType.LeftLeg, new float4(0, 0.5f, cellSize, cellSize) },
                { QuadPartType.RightLeg, new float4(0.25f, 0.5f, cellSize, cellSize) },
                { QuadPartType.LeftHand, new float4(0.5f, 0.5f, cellSize, cellSize) },
                { QuadPartType.RightHand, new float4(0.75f, 0.5f, cellSize, cellSize) },
                { QuadPartType.LeftFoot, new float4(0, 0.25f, cellSize, cellSize) },
                { QuadPartType.RightFoot, new float4(0.25f, 0.25f, cellSize, cellSize) }
            };
            }

        /// <summary>
        /// Creates a complete set of quad mesh parts for a humanoid character
        /// </summary>
        public static QuadMeshPart[] CreateHumanoidQuadParts(uint atlasId = 0)
            {
            var uvs = CreateStandardAtlasUVs();
            var parts = new List<QuadMeshPart>();

            foreach (var value in System.Enum.GetValues(typeof(QuadPartType)))
                {
                var partType = (QuadPartType)value;
                var size = StandardPartSizes[partType];
                var uv = uvs[partType];
                var boneIndex = GetBoneIndexForPart(partType);
                var localOffset = GetLocalOffsetForPart(partType);

                parts.Add(new QuadMeshPart(partType, uv, boneIndex, localOffset, size));
                }

            return parts.ToArray();
            }

        /// <summary>
        /// Maps part types to bone indices in a standard humanoid hierarchy
        /// </summary>
        private static int GetBoneIndexForPart(QuadPartType partType)
            {
            return partType switch
                {
                    QuadPartType.Head => 0,      // Head bone
                    QuadPartType.Torso => 1,     // Spine bone
                    QuadPartType.LeftArm => 2,   // Left upper arm
                    QuadPartType.RightArm => 3,  // Right upper arm
                    QuadPartType.LeftLeg => 4,   // Left upper leg
                    QuadPartType.RightLeg => 5,  // Right upper leg
                    QuadPartType.LeftHand => 6,  // Left hand
                    QuadPartType.RightHand => 7, // Right hand
                    QuadPartType.LeftFoot => 8,  // Left foot
                    QuadPartType.RightFoot => 9, // Right foot
                    _ => 1 // Default to spine
                    };
            }

        /// <summary>
        /// Gets standard local offset for each part type relative to its bone
        /// </summary>
        private static float3 GetLocalOffsetForPart(QuadPartType partType)
            {
            return partType switch
                {
                    QuadPartType.Head => new float3(0, 0.5f, 0),
                    QuadPartType.Torso => new float3(0, 0, 0),
                    QuadPartType.LeftArm => new float3(0, -0.3f, 0),
                    QuadPartType.RightArm => new float3(0, -0.3f, 0),
                    QuadPartType.LeftLeg => new float3(0, -0.4f, 0),
                    QuadPartType.RightLeg => new float3(0, -0.4f, 0),
                    QuadPartType.LeftHand => new float3(0, -0.6f, 0),
                    QuadPartType.RightHand => new float3(0, -0.6f, 0),
                    QuadPartType.LeftFoot => new float3(0, -0.7f, 0),
                    QuadPartType.RightFoot => new float3(0, -0.7f, 0),
                    _ => float3.zero
                    };
            }
        }
    }
