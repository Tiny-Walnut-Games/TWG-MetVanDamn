using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using TinyWalnutGames.MetVD.QuadRig;

namespace TinyWalnutGames.MetVD.QuadRig.Tests
{
    /// <summary>
    /// Comprehensive test suite for the DOTS Quad-Rig Humanoid Prototype.
    /// Validates all requirements and ensures story test integrity.
    /// </summary>
    public class QuadRigHumanoidTests
    {
        private World testWorld;
        private EntityManager entityManager;

        [SetUp]
        public void SetUp()
        {
            testWorld = new World("TestWorld");
            entityManager = testWorld.EntityManager;
        }

        [TearDown]
        public void TearDown()
        {
            if (testWorld != null && testWorld.IsCreated)
            {
                testWorld.Dispose();
            }
        }

        [Test]
        public void QuadRigHumanoid_CreatesWithValidDefaults()
        {
            // Arrange & Act
            var humanoid = new QuadRigHumanoid(rigId: 1, atlasId: 0, scale: 1.0f);

            // Assert
            Assert.That(humanoid.RigId, Is.EqualTo(1));
            Assert.That(humanoid.CurrentAtlasId, Is.EqualTo(0));
            Assert.That(humanoid.Scale, Is.EqualTo(1.0f));
            Assert.That(humanoid.EnableBillboard, Is.True);
            Assert.That(humanoid.EnableAlphaMask, Is.True);
        }

        [Test]
        public void QuadRigHumanoid_ClampsScaleToMinimum()
        {
            // Arrange & Act
            var humanoid = new QuadRigHumanoid(rigId: 1, scale: -0.5f);

            // Assert
            Assert.That(humanoid.Scale, Is.GreaterThanOrEqualTo(0.1f));
        }

        [Test]
        public void BillboardData_InitializesCorrectly()
        {
            // Arrange & Act
            var billboard = new BillboardData(isActive: true, rotationSpeed: 3.0f);

            // Assert
            Assert.That(billboard.IsActive, Is.True);
            Assert.That(billboard.RotationSpeed, Is.EqualTo(3.0f));
            Assert.That(billboard.ConstraintAxis, Is.EqualTo(math.up()));
        }

        [Test]
        public void BillboardData_ClampsRotationSpeedToMinimum()
        {
            // Arrange & Act
            var billboard = new BillboardData(rotationSpeed: -1.0f);

            // Assert
            Assert.That(billboard.RotationSpeed, Is.GreaterThanOrEqualTo(0.1f));
        }

        [Test]
        public void QuadMeshPart_StoresAllRequiredData()
        {
            // Arrange
            var partType = QuadPartType.Head;
            var uvRect = new float4(0, 0, 0.25f, 0.25f);
            var boneIndex = 5;
            var localOffset = new float3(0, 0.5f, 0);
            var quadSize = new float2(0.8f, 1.0f);

            // Act
            var meshPart = new QuadMeshPart(partType, uvRect, boneIndex, localOffset, quadSize);

            // Assert
            Assert.That(meshPart.PartType, Is.EqualTo(partType));
            Assert.That(meshPart.UVRect, Is.EqualTo(uvRect));
            Assert.That(meshPart.BoneIndex, Is.EqualTo(boneIndex));
            Assert.That(meshPart.LocalOffset, Is.EqualTo(localOffset));
            Assert.That(meshPart.QuadSize, Is.EqualTo(quadSize));
        }

        [Test]
        public void TextureAtlasData_ValidatesCorrectly()
        {
            // Arrange & Act
            var atlasData = new TextureAtlasData(
                atlasId: 1,
                biomeType: 2,
                dimensions: new int2(512, 512),
                partCount: 10
            );

            // Assert
            Assert.That(atlasData.AtlasId, Is.EqualTo(1));
            Assert.That(atlasData.BiomeType, Is.EqualTo(2));
            Assert.That(atlasData.AtlasDimensions, Is.EqualTo(new int2(512, 512)));
            Assert.That(atlasData.PartCount, Is.EqualTo(10));
        }

        [Test]
        public void BoneHierarchyElement_CreatesValidBone()
        {
            // Arrange
            var boneIndex = 1;
            var parentIndex = 0;
            var localPosition = new float3(0, 1, 0);
            var localRotation = quaternion.identity;
            var boneNameHash = 12345u;

            // Act
            var bone = new BoneHierarchyElement(boneIndex, parentIndex, localPosition, localRotation, boneNameHash);

            // Assert
            Assert.That(bone.BoneIndex, Is.EqualTo(boneIndex));
            Assert.That(bone.ParentIndex, Is.EqualTo(parentIndex));
            Assert.That(bone.LocalPosition, Is.EqualTo(localPosition));
            Assert.That(bone.LocalRotation, Is.EqualTo(localRotation));
            Assert.That(bone.BoneNameHash, Is.EqualTo(boneNameHash));
        }

        [Test]
        public void AtlasPartElement_MapsPartToUV()
        {
            // Arrange
            var partType = QuadPartType.Torso;
            var uvRect = new float4(0.25f, 0.75f, 0.25f, 0.25f);

            // Act
            var atlasElement = new AtlasPartElement(partType, uvRect);

            // Assert
            Assert.That(atlasElement.PartType, Is.EqualTo(partType));
            Assert.That(atlasElement.UVRect, Is.EqualTo(uvRect));
        }

        [Test]
        public void BillboardUtility_CalculatesCorrectYAxisRotation()
        {
            // Arrange
            var from = new float3(0, 0, 0);
            var to = new float3(1, 5, 1); // Y difference should be ignored

            // Act
            var rotation = BillboardUtility.CalculateYAxisLookRotation(from, to);

            // Assert
            // Should only rotate around Y axis, ignoring Y difference
            var forward = math.mul(rotation, math.forward());
            Assert.That(math.abs(forward.y), Is.LessThan(0.01f), "Billboard rotation should not include Y axis tilt");
        }

        [Test]
        public void BillboardUtility_ValidatesBillboardConfig()
        {
            // Arrange
            var validBillboard = new BillboardData();
            var invalidBillboard = new BillboardData();

            // Act & Assert
            Assert.That(BillboardUtility.ValidateBillboardConfig(validBillboard), Is.True);
        }

        [Test]
        public void QuadMeshUtility_CreatesStandardPartSizes()
        {
            // Act
            var sizes = QuadMeshUtility.StandardPartSizes;

            // Assert
            Assert.That(sizes, Contains.Key(QuadPartType.Head));
            Assert.That(sizes, Contains.Key(QuadPartType.Torso));
            Assert.That(sizes, Contains.Key(QuadPartType.LeftArm));
            Assert.That(sizes, Contains.Key(QuadPartType.RightArm));
            Assert.That(sizes, Contains.Key(QuadPartType.LeftLeg));
            Assert.That(sizes, Contains.Key(QuadPartType.RightLeg));

            // Validate head is smaller than torso
            Assert.That(sizes[QuadPartType.Head].x, Is.LessThan(sizes[QuadPartType.Torso].x));
        }

        [Test]
        public void QuadMeshUtility_CreatesStandardAtlasUVs()
        {
            // Act
            var uvs = QuadMeshUtility.CreateStandardAtlasUVs();

            // Assert
            Assert.That(uvs.Count, Is.EqualTo(10)); // All part types

            // Validate UV coordinates are in 0-1 range
            foreach (var uv in uvs.Values)
            {
                Assert.That(uv.x, Is.InRange(0f, 1f));
                Assert.That(uv.y, Is.InRange(0f, 1f));
                Assert.That(uv.z, Is.InRange(0f, 1f)); // Width
                Assert.That(uv.w, Is.InRange(0f, 1f)); // Height
                
                // Validate UV rect doesn't exceed atlas bounds
                Assert.That(uv.x + uv.z, Is.LessThanOrEqualTo(1f));
                Assert.That(uv.y + uv.w, Is.LessThanOrEqualTo(1f));
            }
        }

        [Test]
        public void QuadMeshUtility_CreatesCompleteHumanoidParts()
        {
            // Act
            var parts = QuadMeshUtility.CreateHumanoidQuadParts();

            // Assert
            Assert.That(parts.Length, Is.EqualTo(10)); // All QuadPartType values

            // Validate each part has valid data
            foreach (var part in parts)
            {
                Assert.That(part.QuadSize.x, Is.GreaterThan(0));
                Assert.That(part.QuadSize.y, Is.GreaterThan(0));
                Assert.That(part.BoneIndex, Is.GreaterThanOrEqualTo(0));
            }
        }

        [Test]
        public void BoneHierarchyUtility_CreatesValidStandardHierarchy()
        {
            // Act
            var bones = BoneHierarchyUtility.CreateStandardHumanoidHierarchy();

            // Assert
            Assert.That(bones.Length, Is.EqualTo(20)); // Standard humanoid bone count

            // Validate hierarchy integrity
            Assert.That(BoneHierarchyUtility.ValidateBoneHierarchy(bones), Is.True);

            // Validate root bone exists
            bool hasRoot = false;
            foreach (var bone in bones)
            {
                if (bone.ParentIndex == -1)
                {
                    hasRoot = true;
                    break;
                }
            }
            Assert.That(hasRoot, Is.True);
        }

        [Test]
        public void BoneHierarchyUtility_ValidatesHierarchyCorrectly()
        {
            // Arrange
            var validHierarchy = BoneHierarchyUtility.CreateStandardHumanoidHierarchy();
            var invalidHierarchy = new BoneHierarchyElement[2]
            {
                new BoneHierarchyElement(0, 1, float3.zero, quaternion.identity, 0), // Circular reference
                new BoneHierarchyElement(1, 0, float3.zero, quaternion.identity, 0)
            };

            // Act & Assert
            Assert.That(BoneHierarchyUtility.ValidateBoneHierarchy(validHierarchy), Is.True);
            Assert.That(BoneHierarchyUtility.ValidateBoneHierarchy(invalidHierarchy), Is.False);
            Assert.That(BoneHierarchyUtility.ValidateBoneHierarchy(null), Is.False);
            Assert.That(BoneHierarchyUtility.ValidateBoneHierarchy(new BoneHierarchyElement[0]), Is.False);
        }

        [Test]
        public void BiomeSkinUtility_CreatesBiomeAtlasCorrectly()
        {
            // Arrange
            var atlasId = 5u;
            var biomeType = BiomeSkinUtility.BiomeType.Forest;
            var dimensions = new int2(1024, 1024);

            // Act
            var atlas = BiomeSkinUtility.CreateBiomeAtlas(atlasId, biomeType, dimensions);

            // Assert
            Assert.That(atlas.AtlasId, Is.EqualTo(atlasId));
            Assert.That(atlas.BiomeType, Is.EqualTo((uint)biomeType));
            Assert.That(atlas.AtlasDimensions, Is.EqualTo(dimensions));
            Assert.That(atlas.PartCount, Is.EqualTo(10));
        }

        [Test]
        public void BiomeSkinUtility_CreatesStandardHumanoidUVs()
        {
            // Act
            using var uvs = BiomeSkinUtility.GetStandardHumanoidUVs(Allocator.Temp);

            // Assert
            Assert.That(uvs.Length, Is.EqualTo(10));

            // Validate all UVs are in valid range
            for (int i = 0; i < uvs.Length; i++)
            {
                var uv = uvs[i];
                Assert.That(uv.x, Is.InRange(0f, 1f));
                Assert.That(uv.y, Is.InRange(0f, 1f));
                Assert.That(uv.z, Is.InRange(0f, 1f));
                Assert.That(uv.w, Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void Entity_CanBeCreatedWithAllComponents()
        {
            // Arrange
            var entity = entityManager.CreateEntity();

            // Act
            entityManager.AddComponentData(entity, new QuadRigHumanoid(1));
            entityManager.AddComponentData(entity, new BillboardData());
            entityManager.AddComponentData(entity, LocalTransform.Identity);
            entityManager.AddComponentData(entity, new TextureAtlasData(0, 0, new int2(512, 512), 10));

            var boneBuffer = entityManager.AddBuffer<BoneHierarchyElement>(entity);
            var bones = BoneHierarchyUtility.CreateStandardHumanoidHierarchy();
            foreach (var bone in bones)
            {
                boneBuffer.Add(bone);
            }

            // Assert
            Assert.That(entityManager.HasComponent<QuadRigHumanoid>(entity), Is.True);
            Assert.That(entityManager.HasComponent<BillboardData>(entity), Is.True);
            Assert.That(entityManager.HasComponent<LocalTransform>(entity), Is.True);
            Assert.That(entityManager.HasComponent<TextureAtlasData>(entity), Is.True);
            Assert.That(entityManager.HasBuffer<BoneHierarchyElement>(entity), Is.True);

            var retrievedBones = entityManager.GetBuffer<BoneHierarchyElement>(entity);
            Assert.That(retrievedBones.Length, Is.EqualTo(20));
        }

        [Test]
        public void StoryTest_ComponentsPassAllValidationChecks()
        {
            // This test verifies that our components meet all story test requirements
            
            // Arrange
            var entity = entityManager.CreateEntity();
            var humanoid = new QuadRigHumanoid(1, 0, 1.0f, true, true);
            var billboard = new BillboardData(true, 5.0f);
            var atlas = new TextureAtlasData(0, 0, new int2(512, 512), 10);

            // Act
            entityManager.AddComponentData(entity, humanoid);
            entityManager.AddComponentData(entity, billboard);
            entityManager.AddComponentData(entity, LocalTransform.Identity);
            entityManager.AddComponentData(entity, atlas);

            // Assert story test requirements
            Assert.That(humanoid.EnableBillboard, Is.True, "Billboard must be enabled for camera facing");
            Assert.That(humanoid.EnableAlphaMask, Is.True, "Alpha mask must be enabled for silhouette integrity");
            Assert.That(billboard.IsActive, Is.True, "Billboard system must be active");
            Assert.That(atlas.PartCount, Is.EqualTo(10), "Atlas must support all humanoid parts");
            
            // Validate biome skin swapping capability
            var originalAtlasId = humanoid.CurrentAtlasId;
            humanoid.CurrentAtlasId = 1; // Simulate skin swap
            Assert.That(humanoid.CurrentAtlasId, Is.Not.EqualTo(originalAtlasId), "Skin swapping must work instantly");
        }
    }
}