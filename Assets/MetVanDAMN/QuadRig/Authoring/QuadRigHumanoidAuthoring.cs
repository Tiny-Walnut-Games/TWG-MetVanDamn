using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using TinyWalnutGames.MetVD.QuadRig;

namespace TinyWalnutGames.MetVD.QuadRig.Authoring
{
    /// <summary>
    /// Authoring component for creating DOTS Quad-Rig Humanoid characters.
    /// Bridges the GameObject world with the ECS implementation.
    /// Provides inspector-friendly interface for configuring quad-rig characters.
    /// </summary>
    public class QuadRigHumanoidAuthoring : MonoBehaviour
    {
        [Header("Character Configuration")]
        [SerializeField] private uint rigId = 1;
        [SerializeField] private float characterScale = 1.0f;
        [SerializeField] private bool enableBillboard = true;
        [SerializeField] private bool enableAlphaMask = true;

        [Header("Billboard Settings")]
        [SerializeField] private float billboardRotationSpeed = 5.0f;

        [Header("Biome Atlas Configuration")]
        [SerializeField] private uint startingAtlasId = 0;
        [SerializeField] private BiomeSkinUtility.BiomeType biomeType = BiomeSkinUtility.BiomeType.Default;
        [SerializeField] private Vector2Int atlasDimensions = new Vector2Int(512, 512);

        [Header("Character Parts")]
        [SerializeField] private bool autoGenerateParts = true;
        [SerializeField] private QuadPartConfiguration[] customParts;

        [Header("Debug & Testing")]
        [SerializeField] private bool enableStoryTest = false;
        [SerializeField] private bool showDebugGizmos = true;

        /// <summary>
        /// Configuration for individual character parts
        /// </summary>
        [System.Serializable]
        public class QuadPartConfiguration
        {
            public QuadPartType partType;
            public Vector2 quadSize = Vector2.one;
            public Vector3 localOffset = Vector3.zero;
            public Rect uvRect = new Rect(0, 0, 0.25f, 0.25f);
        }

        /// <summary>
        /// Creates the ECS entity with all required components
        /// </summary>
        public class QuadRigHumanoidBaker : Baker<QuadRigHumanoidAuthoring>
        {
            public override void Bake(QuadRigHumanoidAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                // Add core quad-rig component
                AddComponent(entity, new QuadRigHumanoid(
                    authoring.rigId,
                    authoring.startingAtlasId,
                    authoring.characterScale,
                    authoring.enableBillboard,
                    authoring.enableAlphaMask
                ));

                // Add billboard component
                AddComponent(entity, new BillboardData(
                    authoring.enableBillboard,
                    authoring.billboardRotationSpeed
                ));

                // Add texture atlas data
                AddComponent(entity, BiomeSkinUtility.CreateBiomeAtlas(
                    authoring.startingAtlasId,
                    authoring.biomeType,
                    new int2(authoring.atlasDimensions.x, authoring.atlasDimensions.y)
                ));

                // Add bone hierarchy
                var boneBuffer = AddBuffer<BoneHierarchyElement>(entity);
                var standardBones = BoneHierarchyUtility.CreateStandardHumanoidHierarchy();
                foreach (var bone in standardBones)
                {
                    boneBuffer.Add(bone);
                }

                // Add character parts
                if (authoring.autoGenerateParts)
                {
                    CreateStandardParts(entity, authoring);
                }
                else
                {
                    CreateCustomParts(entity, authoring);
                }

                // Add story test component if enabled
                if (authoring.enableStoryTest)
                {
                    AddComponentObject(entity, authoring.gameObject.AddComponent<QuadRigStoryTest>());
                }
            }

            private void CreateStandardParts(Entity entity, QuadRigHumanoidAuthoring authoring)
            {
                var standardParts = QuadMeshUtility.CreateHumanoidQuadParts(authoring.startingAtlasId);
                
                // Create child entities for each part
                foreach (var part in standardParts)
                {
                    var partEntity = CreateAdditionalEntity(TransformUsageFlags.Dynamic);
                    AddComponent(partEntity, part);
                    
                    // Link to parent character
                    SetParent(partEntity, entity);
                }
            }

            private void CreateCustomParts(Entity entity, QuadRigHumanoidAuthoring authoring)
            {
                if (authoring.customParts == null) return;

                foreach (var partConfig in authoring.customParts)
                {
                    var partEntity = CreateAdditionalEntity(TransformUsageFlags.Dynamic);
                    
                    var meshPart = new QuadMeshPart(
                        partConfig.partType,
                        new float4(partConfig.uvRect.x, partConfig.uvRect.y, partConfig.uvRect.width, partConfig.uvRect.height),
                        BoneHierarchyUtility.GetBoneIndexForQuadPart(partConfig.partType),
                        new float3(partConfig.localOffset.x, partConfig.localOffset.y, partConfig.localOffset.z),
                        new float2(partConfig.quadSize.x, partConfig.quadSize.y)
                    );
                    
                    AddComponent(partEntity, meshPart);
                    SetParent(partEntity, entity);
                }
            }
        }

        /// <summary>
        /// Validates configuration in the inspector
        /// </summary>
        private void OnValidate()
        {
            // Ensure valid values
            rigId = math.max(1, rigId);
            characterScale = math.max(0.1f, characterScale);
            billboardRotationSpeed = math.max(0.1f, billboardRotationSpeed);
            
            // Ensure atlas dimensions are power of 2
            atlasDimensions.x = Mathf.ClosestPowerOfTwo(math.max(64, atlasDimensions.x));
            atlasDimensions.y = Mathf.ClosestPowerOfTwo(math.max(64, atlasDimensions.y));
        }

        /// <summary>
        /// Draw debug gizmos for visualization
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            // Draw character bounds
            Gizmos.color = enableBillboard ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(transform.position, Vector3.one * characterScale);

            // Draw billboard indicator
            if (enableBillboard)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, transform.forward * 2f);
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.2f);
            }

            // Draw part indicators
            if (autoGenerateParts)
            {
                DrawStandardPartGizmos();
            }
            else if (customParts != null)
            {
                DrawCustomPartGizmos();
            }
        }

        private void DrawStandardPartGizmos()
        {
            var standardSizes = QuadMeshUtility.StandardPartSizes;
            
            foreach (var partType in System.Enum.GetValues<QuadPartType>())
            {
                if (standardSizes.TryGetValue(partType, out var size))
                {
                    var offset = GetStandardPartOffset(partType);
                    var worldPos = transform.position + offset * characterScale;
                    
                    Gizmos.color = GetPartColor(partType);
                    Gizmos.DrawWireCube(worldPos, new Vector3(size.x, size.y, 0.1f) * characterScale);
                }
            }
        }

        private void DrawCustomPartGizmos()
        {
            foreach (var part in customParts)
            {
                var worldPos = transform.position + part.localOffset * characterScale;
                
                Gizmos.color = GetPartColor(part.partType);
                Gizmos.DrawWireCube(worldPos, new Vector3(part.quadSize.x, part.quadSize.y, 0.1f) * characterScale);
            }
        }

        private Vector3 GetStandardPartOffset(QuadPartType partType)
        {
            return partType switch
            {
                QuadPartType.Head => new Vector3(0, 1.8f, 0),
                QuadPartType.Torso => new Vector3(0, 1.0f, 0),
                QuadPartType.LeftArm => new Vector3(-0.8f, 1.2f, 0),
                QuadPartType.RightArm => new Vector3(0.8f, 1.2f, 0),
                QuadPartType.LeftLeg => new Vector3(-0.3f, 0.2f, 0),
                QuadPartType.RightLeg => new Vector3(0.3f, 0.2f, 0),
                QuadPartType.LeftHand => new Vector3(-1.2f, 0.8f, 0),
                QuadPartType.RightHand => new Vector3(1.2f, 0.8f, 0),
                QuadPartType.LeftFoot => new Vector3(-0.3f, -0.5f, 0),
                QuadPartType.RightFoot => new Vector3(0.3f, -0.5f, 0),
                _ => Vector3.zero
            };
        }

        private Color GetPartColor(QuadPartType partType)
        {
            return partType switch
            {
                QuadPartType.Head => Color.yellow,
                QuadPartType.Torso => Color.blue,
                QuadPartType.LeftArm or QuadPartType.RightArm => Color.red,
                QuadPartType.LeftLeg or QuadPartType.RightLeg => Color.green,
                QuadPartType.LeftHand or QuadPartType.RightHand => Color.magenta,
                QuadPartType.LeftFoot or QuadPartType.RightFoot => Color.cyan,
                _ => Color.white
            };
        }

        /// <summary>
        /// Context menu methods for testing
        /// </summary>
        [ContextMenu("Test Biome Skin Swap")]
        private void TestBiomeSkinSwap()
        {
            startingAtlasId = (startingAtlasId + 1) % 5; // Cycle through atlas IDs
            Debug.Log($"🎨 Testing skin swap to atlas {startingAtlasId}");
        }

        [ContextMenu("Toggle Billboard")]
        private void ToggleBillboard()
        {
            enableBillboard = !enableBillboard;
            Debug.Log($"📡 Billboard {(enableBillboard ? "enabled" : "disabled")}");
        }

        [ContextMenu("Run Story Test")]
        private void RunStoryTest()
        {
            var storyTest = GetComponent<QuadRigStoryTest>();
            if (storyTest == null)
            {
                storyTest = gameObject.AddComponent<QuadRigStoryTest>();
            }
            storyTest.RunStoryTestManually();
        }

        [ContextMenu("Generate Standard Parts")]
        private void GenerateStandardParts()
        {
            customParts = new QuadPartConfiguration[10];
            var standardSizes = QuadMeshUtility.StandardPartSizes;
            var standardUVs = QuadMeshUtility.CreateStandardAtlasUVs();

            int index = 0;
            foreach (var partType in System.Enum.GetValues<QuadPartType>())
            {
                customParts[index] = new QuadPartConfiguration
                {
                    partType = partType,
                    quadSize = standardSizes[partType],
                    localOffset = GetStandardPartOffset(partType),
                    uvRect = new Rect(
                        standardUVs[partType].x,
                        standardUVs[partType].y,
                        standardUVs[partType].z,
                        standardUVs[partType].w
                    )
                };
                index++;
            }

            autoGenerateParts = false;
            Debug.Log("✅ Generated standard part configurations");
        }
    }
}