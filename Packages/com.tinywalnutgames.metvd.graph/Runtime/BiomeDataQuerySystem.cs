using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;
using BiomeFieldData = TinyWalnutGames.MetVD.Biome.BiomeFieldData;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// Component that caches biome data for a room based on parent district/sector
    /// </summary>
    public struct RoomBiomeData : IComponentData
    {
        /// <summary>
        /// The resolved biome type for this room
        /// </summary>
        public BiomeType BiomeType;
        
        /// <summary>
        /// Primary polarity inherited from parent district
        /// </summary>
        public Polarity PrimaryPolarity;
        
        /// <summary>
        /// Secondary polarity for transition zones
        /// </summary>
        public Polarity SecondaryPolarity;
        
        /// <summary>
        /// Biome difficulty modifier
        /// </summary>
        public float DifficultyModifier;
        
        /// <summary>
        /// Whether this data has been resolved from parent hierarchy
        /// </summary>
        public bool IsResolved;
        
        /// <summary>
        /// Entity reference to parent district (for hierarchy queries)
        /// </summary>
        public Entity ParentDistrict;

        public RoomBiomeData(BiomeType biomeType, Polarity primaryPolarity, Entity parentDistrict, 
                           float difficultyModifier = 1f, Polarity secondaryPolarity = Polarity.None)
        {
            BiomeType = biomeType;
            PrimaryPolarity = primaryPolarity;
            SecondaryPolarity = secondaryPolarity;
            DifficultyModifier = difficultyModifier;
            IsResolved = true;
            ParentDistrict = parentDistrict;
        }

        public static RoomBiomeData CreateUnresolved(Entity parentDistrict)
        {
            return new RoomBiomeData
            {
                BiomeType = BiomeType.Unknown,
                PrimaryPolarity = Polarity.None,
                SecondaryPolarity = Polarity.None,
                DifficultyModifier = 1f,
                IsResolved = false,
                ParentDistrict = parentDistrict
            };
        }
    }

    /// <summary>
    /// Component for requesting biome data resolution
    /// </summary>
    public struct BiomeDataRequest : IComponentData
    {
        /// <summary>
        /// Priority of this request (higher = process first)
        /// </summary>
        public int Priority;
        
        /// <summary>
        /// Whether to fallback to defaults if parent data unavailable
        /// </summary>
        public bool AllowDefaults;
        
        /// <summary>
        /// Maximum distance to search for parent biome data
        /// </summary>
        public int MaxSearchDepth;

        public BiomeDataRequest(int priority = 0, bool allowDefaults = true, int maxSearchDepth = 3)
        {
            Priority = priority;
            AllowDefaults = allowDefaults;
            MaxSearchDepth = math.max(1, maxSearchDepth);
        }
    }

    /// <summary>
    /// System that resolves biome data for rooms by querying parent district/sector hierarchy
    /// Replaces hardcoded default values in RoomManagementSystem
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(RoomManagementSystem))]
    public partial struct BiomeDataQuerySystem : ISystem
    {
        private ComponentLookup<Core.Biome> _biomeLookup;
        private ComponentLookup<RoomBiomeData> _roomBiomeDataLookup;
        private BufferLookup<ConnectionBufferElement> _connectionLookup;
        private EntityQuery _requestQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _biomeLookup = state.GetComponentLookup<Core.Biome>(true);
            _roomBiomeDataLookup = state.GetComponentLookup<RoomBiomeData>();
            _connectionLookup = state.GetBufferLookup<ConnectionBufferElement>(true);
            
            _requestQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<BiomeDataRequest>()
                .WithNone<RoomBiomeData>()
                .Build(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_requestQuery.IsEmpty) return;

            _biomeLookup.Update(ref state);
            _roomBiomeDataLookup.Update(ref state);
            _connectionLookup.Update(ref state);

            // Process biome data requests using manual EntityQuery iteration
            var entities = _requestQuery.ToEntityArray(Allocator.Temp);
            var requests = _requestQuery.ToComponentDataArray<BiomeDataRequest>(Allocator.Temp);
            var nodeIds = _requestQuery.ToComponentDataArray<NodeId>(Allocator.Temp);
            
            for (int i = 0; i < entities.Length; i++)
            {
                var resolvedData = ResolveFromHierarchy(entities[i], requests[i], nodeIds[i]);
                
                // Add the resolved biome data component
                _roomBiomeDataLookup[entities[i]] = resolvedData;
            }
            
            entities.Dispose();
            requests.Dispose();
            nodeIds.Dispose();
        }

        private RoomBiomeData ResolveFromHierarchy(Entity roomEntity, BiomeDataRequest request, NodeId nodeId)
        {
            // Try to find parent district with biome data
            var parentDistrict = FindParentDistrict(roomEntity, request.MaxSearchDepth);
            
            if (parentDistrict != Entity.Null && _biomeLookup.HasComponent(parentDistrict))
            {
                var parentBiome = _biomeLookup[parentDistrict];
                return new RoomBiomeData(
                    parentBiome.Type,
                    parentBiome.PrimaryPolarity,
                    parentDistrict,
                    parentBiome.DifficultyModifier,
                    parentBiome.SecondaryPolarity
                );
            }
            
            // Try to infer from sibling rooms
            var siblingBiome = FindSiblingBiome(roomEntity, nodeId);
            if (siblingBiome.BiomeType != BiomeType.Unknown)
            {
                return siblingBiome;
            }
            
            // Fallback to intelligent defaults based on node position or characteristics
            if (request.AllowDefaults)
            {
                return CreateIntelligentDefault(nodeId, parentDistrict);
            }
            
            // Return unresolved marker
            return RoomBiomeData.CreateUnresolved(parentDistrict);
        }

        private Entity FindParentDistrict(Entity roomEntity, int maxDepth)
        {
            // Walk up the hierarchy looking for a district entity with biome data
            Entity currentEntity = roomEntity;
            
            for (int depth = 0; depth < maxDepth; depth++)
            {
                // Check if current entity has biome data
                if (_biomeLookup.HasComponent(currentEntity))
                {
                    return currentEntity;
                }
                
                // Try to find parent entity through NodeId hierarchy
                if (SystemAPI.HasComponent<NodeId>(currentEntity))
                {
                    var nodeId = SystemAPI.GetComponent<NodeId>(currentEntity);
                    
                    // If this entity has a parent reference, continue traversal
                    if (nodeId.ParentId != 0 && nodeId.Level > 0)
                    {
                        // Find entity with parent NodeId
                        var parentEntity = FindEntityByNodeId(nodeId.ParentId);
                        if (parentEntity != Entity.Null)
                        {
                            currentEntity = parentEntity;
                            continue;
                        }
                    }
                }
                
                // No more parents found
                break;
            }
            
            return Entity.Null;
        }

        private Entity FindEntityByNodeId(uint parentId)
        {
            // This would typically use a lookup table or entity query
            // For now, return Entity.Null - in practice this would be optimized
            // with a cached lookup from NodeId.Value to Entity
            return Entity.Null;
        }

        private RoomBiomeData FindSiblingBiome(Entity roomEntity, NodeId nodeId)
        {
            // Look at connected/nearby rooms to infer biome type
            // This would use the connection system in a full implementation
            
            if (_connectionLookup.HasBuffer(roomEntity))
            {
                var connections = _connectionLookup[roomEntity];
                foreach (var connection in connections)
                {
                    // Check connected nodes for biome data
                    // In practice, would resolve connection.ToNodeId to entity and check biome
                }
            }
            
            return RoomBiomeData.CreateUnresolved(Entity.Null);
        }

        private static RoomBiomeData CreateIntelligentDefault(NodeId nodeId, Entity parentDistrict)
        {
            // Create intelligent defaults based on node characteristics
            var biomeType = InferBiomeFromNodeId(nodeId);
            var polarity = InferPolarityFromBiome(biomeType);
            
            return new RoomBiomeData(biomeType, polarity, parentDistrict, 1f);
        }

        private static BiomeType InferBiomeFromNodeId(NodeId nodeId)
        {
            // Use node ID to deterministically assign biome types
            var hash = nodeId.Value;
            var biomeIndex = hash % 8; // Distribute across common biome types
            
            return biomeIndex switch
            {
                0 => BiomeType.HubArea,
                1 => BiomeType.SkyGardens,
                2 => BiomeType.ShadowRealms,
                3 => BiomeType.CrystalCaverns,
                4 => BiomeType.PowerPlant,
                5 => BiomeType.FrozenWastes,
                6 => BiomeType.Forest,
                7 => BiomeType.VolcanicCore,
                _ => BiomeType.HubArea // Fallback
            };
        }

        private static Polarity InferPolarityFromBiome(BiomeType biomeType)
        {
            return biomeType switch
            {
                BiomeType.SkyGardens => Polarity.Sun | Polarity.Wind,
                BiomeType.ShadowRealms => Polarity.Moon,
                BiomeType.CrystalCaverns => Polarity.Cold | Polarity.Earth,
                BiomeType.PowerPlant => Polarity.Tech | Polarity.Heat,
                BiomeType.FrozenWastes => Polarity.Cold | Polarity.Wind,
                BiomeType.Forest => Polarity.Life | Polarity.Earth,
                BiomeType.VolcanicCore => Polarity.Heat | Polarity.Earth,
                BiomeType.HubArea => Polarity.None,
                _ => Polarity.None
            };
        }
    }

    /// <summary>
    /// Helper system that automatically adds BiomeDataRequest to rooms that need biome resolution
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(BiomeDataQuerySystem))]
    public partial struct BiomeDataRequestSystem : ISystem
    {
        private EntityQuery _roomsNeedingBiomeData;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _roomsNeedingBiomeData = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<RoomHierarchyData, NodeId>()
                .WithNone<RoomBiomeData, BiomeDataRequest>()
                .Build(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_roomsNeedingBiomeData.IsEmpty) return;

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            // Use SystemAPI.Query instead of IJobEntity
            foreach (var (entity, roomData, nodeId) in SystemAPI.Query<RefRO<RoomHierarchyData>, RefRO<NodeId>>().WithEntityAccess().WithAll<RoomHierarchyData>().WithNone<RoomBiomeData, BiomeDataRequest>())
            {
                ecb.AddComponent(entity, new BiomeDataRequest(
                    priority: GetRequestPriority(roomData.ValueRO.Type),
                    allowDefaults: true,
                    maxSearchDepth: 5
                ));
            }
            
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private static RoomType GetRoomTypeFromHierarchy(RoomType hierarchyType)
        {
            return hierarchyType switch
            {
                RoomType.Hub => RoomType.Hub,
                RoomType.Normal => RoomType.Normal,
                RoomType.Boss => RoomType.Boss,
                _ => RoomType.Normal
            };
        }

        private static int GetRequestPriority(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Boss => 10,      // High priority
                RoomType.Treasure => 8,   // High priority 
                RoomType.Shop => 6,       // Medium priority 
                RoomType.Save => 5,       // Medium priority
                RoomType.Normal => 3,     // Normal priority 
                RoomType.Hub => 4,        // Normal priority 
                RoomType.Entrance => 7,   // High priority
                RoomType.Exit => 7,       // High priority
                _ => 0                    // Default priority
            };
        }
    }
}