using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// Cinemachine zone generation system implementing Master Spec requirements:
    /// - Spawn Virtual Camera GameObject for each room entity
    /// - Configure follow/look targets, lens settings, damping  
    /// - Generate masking/confiner volume from room bounds
    /// - Store camera zone data in ECS components linked to room
    /// - Biome-specific camera presets (wide FOV in sky, tight framing in caves)
    /// 
    /// Runs AFTER navigation generation as final step in procedural pipeline
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(RoomNavigationGeneratorSystem))]
    public partial class CinemachineZoneGeneratorSystem : SystemBase
    {
        private EntityQuery _roomsWithNavigationQuery;
        
        protected override void OnCreate()
        {
            // Rooms that have navigation generated but no cinemachine zones
            _roomsWithNavigationQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NodeId, RoomHierarchyData, RoomTemplate, ProceduralRoomGenerated>()
                .WithNone<CinemachineZoneData>()
                .Build(this);
                
            RequireForUpdate(_roomsWithNavigationQuery);
        }

        protected override void OnUpdate()
        {
            using var roomEntities = _roomsWithNavigationQuery.ToEntityArray(Allocator.Temp);
            using var nodeIds = _roomsWithNavigationQuery.ToComponentDataArray<NodeId>(Allocator.Temp);
            using var roomData = _roomsWithNavigationQuery.ToComponentDataArray<RoomHierarchyData>(Allocator.Temp);
            using var templates = _roomsWithNavigationQuery.ToComponentDataArray<RoomTemplate>(Allocator.Temp);
            
            for (int i = 0; i < roomEntities.Length; i++)
            {
                var roomEntity = roomEntities[i];
                var nodeId = nodeIds[i];
                var hierarchy = roomData[i];
                var template = templates[i];
                
                // Check if cinemachine already generated
                var genStatus = EntityManager.GetComponentData<ProceduralRoomGenerated>(roomEntity);
                if (genStatus.CinemachineGenerated) continue;
                
                // Generate cinemachine zone for this room
                GenerateCinemachineZone(EntityManager, roomEntity, hierarchy, template, nodeId, ref genStatus);
                
                // Update generation status
                genStatus.CinemachineGenerated = true;
                EntityManager.SetComponentData(roomEntity, genStatus);
            }
        }

        private static void GenerateCinemachineZone(EntityManager entityManager, Entity roomEntity,
                                                   RoomHierarchyData hierarchy, RoomTemplate template,
                                                   NodeId nodeId, ref ProceduralRoomGenerated genStatus)
        {
            var bounds = hierarchy.Bounds;
            var biomeAffinity = template.CapabilityTags.BiomeType;
            
            // Create camera preset based on biome and room type
            var cameraPreset = CreateBiomeSpecificCameraPreset(biomeAffinity, hierarchy.Type, template.GeneratorType);
            
            // Calculate camera positioning and bounds
            var cameraPosition = CalculateCameraPosition(bounds, cameraPreset);
            var confinerBounds = CalculateConfinerBounds(bounds, cameraPreset);
            
            // Create cinemachine zone data component
            var zoneData = new CinemachineZoneData
            {
                RoomNodeId = nodeId.Value,
                CameraPosition = cameraPosition,
                ConfinerBounds = confinerBounds,
                CameraPreset = cameraPreset,
                IsActive = false, // Will be activated by proximity/trigger systems
                BlendTime = CalculateBlendTime(hierarchy.Type, template.GeneratorType),
                Priority = CalculateCameraPriority(hierarchy.Type)
            };
            
            // Add component to room entity
            entityManager.AddComponentData(roomEntity, zoneData);
            
            // Create GameObject representation for Cinemachine integration
            // Note: In a real implementation, this would create actual Cinemachine Virtual Camera GameObjects
            // For this minimal implementation, we store the data for later GameObject creation
            CreateCameraZoneGameObjectData(entityManager, roomEntity, zoneData, nodeId);
        }

        private static CinemachineCameraPreset CreateBiomeSpecificCameraPreset(BiomeAffinity biome, RoomType roomType, RoomGeneratorType generatorType)
        {
            var preset = new CinemachineCameraPreset();
            
            // Base settings
            preset.FieldOfView = 60.0f;
            preset.FollowDamping = new float3(1.0f, 1.0f, 1.0f);
            preset.LookDamping = new float3(1.0f, 1.0f, 1.0f);
            preset.LensShift = float2.zero;
            preset.Offset = new float3(0, 2, -10);
            
            // Biome-specific adjustments
            switch (biome)
            {
                case BiomeAffinity.Sky:
                    // Wide FOV for sky biome to show expansive aerial views
                    preset.FieldOfView = 75.0f;
                    preset.Offset = new float3(0, 1, -15); // Further back
                    preset.FollowDamping = new float3(0.5f, 0.3f, 0.5f); // Smoother for flying
                    break;
                    
                case BiomeAffinity.Underground:
                    // Tight framing for caves/underground
                    preset.FieldOfView = 45.0f;
                    preset.Offset = new float3(0, 1, -8); // Closer
                    preset.FollowDamping = new float3(1.5f, 1.5f, 1.5f); // More responsive
                    break;
                    
                case BiomeAffinity.Mountain:
                    // Medium-wide view for vertical spaces
                    preset.FieldOfView = 65.0f;
                    preset.Offset = new float3(0, 3, -12);
                    break;
                    
                case BiomeAffinity.Ocean:
                    // Fluid movement for water environments
                    preset.FieldOfView = 55.0f;
                    preset.FollowDamping = new float3(0.8f, 0.4f, 0.8f);
                    break;
                    
                case BiomeAffinity.TechZone:
                    // Precise, responsive framing for tech areas
                    preset.FieldOfView = 58.0f;
                    preset.FollowDamping = new float3(1.2f, 1.2f, 1.2f);
                    preset.LensShift = new float2(0, 0.1f); // Slight upward bias
                    break;
            }
            
            // Room type adjustments
            switch (roomType)
            {
                case RoomType.Boss:
                    // Dynamic camera for boss fights
                    preset.FieldOfView += 10.0f; // Wider to show boss
                    preset.FollowDamping *= 0.7f; // More responsive
                    break;
                    
                case RoomType.Treasure:
                    // Focused view for treasure rooms
                    preset.FieldOfView -= 5.0f;
                    preset.FollowDamping *= 1.3f; // More stable
                    break;
                    
                case RoomType.Hub:
                    // Stable, wide view for navigation hubs
                    preset.FieldOfView += 5.0f;
                    preset.FollowDamping *= 1.5f; // Very stable
                    break;
            }
            
            // Generator type adjustments
            switch (generatorType)
            {
                case RoomGeneratorType.VerticalSegment:
                    // Vertical rooms need different aspect considerations
                    preset.Offset.y += 2.0f; // Higher camera
                    preset.FieldOfView += 5.0f;
                    break;
                    
                case RoomGeneratorType.HorizontalCorridor:
                    // Horizontal rooms can use wider framing
                    preset.FieldOfView += 8.0f;
                    preset.Offset.z -= 2.0f; // Further back
                    break;
                    
                case RoomGeneratorType.SkyBiomePlatform:
                    // Sky platforms need special consideration for moving elements
                    preset.FollowDamping *= 0.6f; // More responsive to platform movement
                    preset.LookDamping *= 0.8f;
                    break;
            }
            
            return preset;
        }

        private static float3 CalculateCameraPosition(RectInt bounds, CinemachineCameraPreset preset)
        {
            // Position camera at room center with preset offset
            var roomCenter = new float3(
                bounds.x + bounds.width * 0.5f,
                bounds.y + bounds.height * 0.5f,
                0
            );
            
            return roomCenter + preset.Offset;
        }

        private static BoundingBox CalculateConfinerBounds(RectInt bounds, CinemachineCameraPreset preset)
        {
            // Create confiner bounds with some padding beyond room bounds
            float padding = 2.0f;
            
            var min = new float3(
                bounds.x - padding,
                bounds.y - padding,
                preset.Offset.z - 5.0f
            );
            
            var max = new float3(
                bounds.x + bounds.width + padding,
                bounds.y + bounds.height + padding,
                preset.Offset.z + 5.0f
            );
            
            return new BoundingBox { Min = min, Max = max };
        }

        private static float CalculateBlendTime(RoomType roomType, RoomGeneratorType generatorType)
        {
            // Different room types have different optimal blend times
            return roomType switch
            {
                RoomType.Boss => 0.3f,      // Quick transitions for boss encounters
                RoomType.Hub => 1.0f,       // Smooth transitions for hubs
                RoomType.Treasure => 0.5f,  // Medium transitions for treasures
                _ => generatorType switch
                {
                    RoomGeneratorType.HorizontalCorridor => 0.7f, // Flow-friendly
                    RoomGeneratorType.VerticalSegment => 0.4f,    // Responsive for vertical movement
                    _ => 0.6f // Default blend time
                }
            };
        }

        private static int CalculateCameraPriority(RoomType roomType)
        {
            // Camera priorities - higher values take precedence
            return roomType switch
            {
                RoomType.Boss => 15,      // Highest priority
                RoomType.Treasure => 12,
                RoomType.Hub => 10,
                RoomType.Save => 8,
                RoomType.Shop => 8,
                _ => 5                    // Normal rooms
            };
        }

        private static void CreateCameraZoneGameObjectData(EntityManager entityManager, Entity roomEntity, 
                                                          CinemachineZoneData zoneData, NodeId nodeId)
        {
            // In a full implementation, this would create actual Cinemachine Virtual Camera GameObjects
            // For this minimal implementation, we create a component that can be used to create GameObjects later
            var gameObjectData = new CinemachineGameObjectReference
            {
                RoomEntity = roomEntity,
                CameraName = $"VCam_Room_{nodeId.Value}",
                ShouldCreateGameObject = true,
                GameObjectInstanceId = 0 // Will be set when GameObject is created
            };
            
            entityManager.AddComponentData(roomEntity, gameObjectData);
        }
    }

    /// <summary>
    /// Component storing Cinemachine camera zone data for a room
    /// Contains all camera settings and activation logic
    /// </summary>
    public struct CinemachineZoneData : IComponentData
    {
        public uint RoomNodeId;                    // Associated room ID
        public float3 CameraPosition;              // Camera position in world space
        public BoundingBox ConfinerBounds;         // Camera movement bounds
        public CinemachineCameraPreset CameraPreset; // Camera settings
        public bool IsActive;                      // Whether this camera is currently active
        public float BlendTime;                    // Time to blend to this camera
        public int Priority;                       // Camera priority (higher = more important)
    }

    /// <summary>
    /// Camera preset settings for different biomes and room types
    /// </summary>
    public struct CinemachineCameraPreset : IComponentData
    {
        public float FieldOfView;                  // Camera field of view
        public float3 FollowDamping;               // Damping for follow movement (XYZ)
        public float3 LookDamping;                 // Damping for look direction (XYZ)
        public float2 LensShift;                   // Lens shift for framing adjustments
        public float3 Offset;                      // Camera offset from target
    }

    /// <summary>
    /// Component for linking ECS room entities to Cinemachine GameObjects
    /// Enables hybrid ECS/GameObject workflow for camera management
    /// </summary>
    public struct CinemachineGameObjectReference : IComponentData
    {
        public Entity RoomEntity;                  // Associated room entity
        public FixedString64Bytes CameraName;      // GameObject name for the camera
        public bool ShouldCreateGameObject;        // Whether GameObject needs to be created
        public int GameObjectInstanceId;           // Unity GameObject instance ID when created
    }

    /// <summary>
    /// Simple bounding box structure for camera confiner bounds
    /// </summary>
    public struct BoundingBox
    {
        public float3 Min;
        public float3 Max;
        
        public float3 Center => (Min + Max) * 0.5f;
        public float3 Size => Max - Min;
    }
}