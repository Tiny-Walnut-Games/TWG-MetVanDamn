using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.QuadRig
{
    /// <summary>
    /// Core component marking an entity as a quad-rig humanoid character.
    /// Contains essential configuration for the billboard and skinning systems.
    /// </summary>
    public struct QuadRigHumanoid : IComponentData
    {
        /// <summary>
        /// Unique identifier for this character rig
        /// </summary>
        public uint RigId;

        /// <summary>
        /// Current texture atlas being used for biome skin
        /// </summary>
        public uint CurrentAtlasId;

        /// <summary>
        /// Scale factor for the character quads
        /// </summary>
        public float Scale;

        /// <summary>
        /// Whether billboard system should apply to this character
        /// </summary>
        public bool EnableBillboard;

        /// <summary>
        /// Whether alpha masking is enabled for silhouette integrity
        /// </summary>
        public bool EnableAlphaMask;

        public QuadRigHumanoid(uint rigId, uint atlasId = 0, float scale = 1.0f, bool enableBillboard = true, bool enableAlphaMask = true)
        {
            RigId = rigId;
            CurrentAtlasId = atlasId;
            Scale = math.max(0.1f, scale);
            EnableBillboard = enableBillboard;
            EnableAlphaMask = enableAlphaMask;
        }
    }

    /// <summary>
    /// Billboard component for Y-axis rotation towards camera.
    /// Ensures characters always face the camera without distorting animation.
    /// </summary>
    public struct BillboardData : IComponentData
    {
        /// <summary>
        /// Axis constraint for billboard rotation (Y-axis only for humanoids)
        /// </summary>
        public float3 ConstraintAxis;

        /// <summary>
        /// Whether billboard is currently active
        /// </summary>
        public bool IsActive;

        /// <summary>
        /// Smooth rotation speed for billboard transitions
        /// </summary>
        public float RotationSpeed;

        public BillboardData(bool isActive = true, float rotationSpeed = 5.0f)
        {
            ConstraintAxis = math.up(); // Y-axis constraint
            IsActive = isActive;
            RotationSpeed = math.max(0.1f, rotationSpeed);
        }
    }

    /// <summary>
    /// Quad mesh part definition for character body parts.
    /// Each part (head, torso, arms, legs) has its own UV mapping and bone binding.
    /// </summary>
    public struct QuadMeshPart : IComponentData
    {
        /// <summary>
        /// Type of body part this quad represents
        /// </summary>
        public QuadPartType PartType;

        /// <summary>
        /// UV coordinates for texture atlas mapping
        /// </summary>
        public float4 UVRect; // x,y = offset, z,w = size

        /// <summary>
        /// Bone index this part is bound to
        /// </summary>
        public int BoneIndex;

        /// <summary>
        /// Local offset from bone position
        /// </summary>
        public float3 LocalOffset;

        /// <summary>
        /// Size of the quad mesh
        /// </summary>
        public float2 QuadSize;

        public QuadMeshPart(QuadPartType partType, float4 uvRect, int boneIndex, float3 localOffset, float2 quadSize)
        {
            PartType = partType;
            UVRect = uvRect;
            BoneIndex = boneIndex;
            LocalOffset = localOffset;
            QuadSize = quadSize;
        }
    }

    /// <summary>
    /// Types of character body parts for quad mesh system
    /// </summary>
    public enum QuadPartType : byte
    {
        Head = 0,
        Torso = 1,
        LeftArm = 2,
        RightArm = 3,
        LeftLeg = 4,
        RightLeg = 5,
        LeftHand = 6,
        RightHand = 7,
        LeftFoot = 8,
        RightFoot = 9
    }

    /// <summary>
    /// Texture atlas configuration for biome skin swapping.
    /// Allows instant skin changes without altering rig or animation data.
    /// </summary>
    public struct TextureAtlasData : IComponentData
    {
        /// <summary>
        /// Unique identifier for this atlas
        /// </summary>
        public uint AtlasId;

        /// <summary>
        /// Biome type this atlas represents
        /// </summary>
        public uint BiomeType;

        /// <summary>
        /// Atlas texture dimensions
        /// </summary>
        public int2 AtlasDimensions;

        /// <summary>
        /// Number of character parts in this atlas
        /// </summary>
        public int PartCount;

        public TextureAtlasData(uint atlasId, uint biomeType, int2 dimensions, int partCount)
        {
            AtlasId = atlasId;
            BiomeType = biomeType;
            AtlasDimensions = dimensions;
            PartCount = partCount;
        }
    }

    /// <summary>
    /// Buffer element for storing bone hierarchy data.
    /// Compatible with Unity's Animator bone structure.
    /// </summary>
    public struct BoneHierarchyElement : IBufferElementData
    {
        /// <summary>
        /// Bone index in the hierarchy
        /// </summary>
        public int BoneIndex;

        /// <summary>
        /// Parent bone index (-1 for root)
        /// </summary>
        public int ParentIndex;

        /// <summary>
        /// Local position relative to parent
        /// </summary>
        public float3 LocalPosition;

        /// <summary>
        /// Local rotation relative to parent
        /// </summary>
        public quaternion LocalRotation;

        /// <summary>
        /// Bone name hash for Unity Animator compatibility
        /// </summary>
        public uint BoneNameHash;

        public BoneHierarchyElement(int boneIndex, int parentIndex, float3 localPosition, quaternion localRotation, uint boneNameHash)
        {
            BoneIndex = boneIndex;
            ParentIndex = parentIndex;
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            BoneNameHash = boneNameHash;
        }
    }

    /// <summary>
    /// Buffer element for UV atlas part definitions.
    /// Maps each character part to its UV coordinates in the texture atlas.
    /// </summary>
    public struct AtlasPartElement : IBufferElementData
    {
        /// <summary>
        /// Part type this UV mapping represents
        /// </summary>
        public QuadPartType PartType;

        /// <summary>
        /// UV rectangle in normalized coordinates (0-1)
        /// </summary>
        public float4 UVRect;

        public AtlasPartElement(QuadPartType partType, float4 uvRect)
        {
            PartType = partType;
            UVRect = uvRect;
        }
    }
}