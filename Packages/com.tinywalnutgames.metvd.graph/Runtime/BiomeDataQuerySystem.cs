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
        private ComponentLookup<NodeId> _nodeIdLookup;
        private BufferLookup<ConnectionBufferElement> _connectionLookup;
        private EntityQuery _requestQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _biomeLookup = state.GetComponentLookup<Core.Biome>(true);
            _roomBiomeDataLookup = state.GetComponentLookup<RoomBiomeData>();
            _nodeIdLookup = state.GetComponentLookup<NodeId>(true);
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
            _nodeIdLookup.Update(ref state);
            _connectionLookup.Update(ref state);

            // Process biome data requests using manual EntityQuery iteration
            var entities = _requestQuery.ToEntityArray(Allocator.Temp);
            var requests = _requestQuery.ToComponentDataArray<BiomeDataRequest>(Allocator.Temp);
            var nodeIds = _requestQuery.ToComponentDataArray<NodeId>(Allocator.Temp);
            
            for (int i = 0; i < entities.Length; i++)
            {
                var resolvedData = ResolveFromHierarchy(ref state, entities[i], requests[i], nodeIds[i]);
                
                // Add the resolved biome data component
                _roomBiomeDataLookup[entities[i]] = resolvedData;
            }
            
            entities.Dispose();
            requests.Dispose();
            nodeIds.Dispose();
        }

        private RoomBiomeData ResolveFromHierarchy(ref SystemState state, Entity roomEntity, BiomeDataRequest request, NodeId nodeId)
        {
            // Try to find parent district with biome data
            var parentDistrict = FindParentDistrict(ref state, roomEntity, request.MaxSearchDepth);
            
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
            var siblingBiome = FindSiblingBiome(ref state, roomEntity, nodeId);
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

        private Entity FindParentDistrict(ref SystemState state, Entity roomEntity, int maxDepth)
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
                if (_nodeIdLookup.HasComponent(currentEntity))
                {
                    var nodeId = _nodeIdLookup[currentEntity];
                    
                    // If this entity has a parent reference, continue traversal
                    if (nodeId.ParentId != 0 && nodeId.Level > 0)
                    {
                        // Find entity with parent NodeId
                        var parentEntity = FindEntityByNodeId(ref state, nodeId.ParentId);
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

        private readonly Entity FindEntityByNodeId(ref SystemState state, uint parentId)
        {
            // Implement efficient entity lookup by NodeId using EntityQuery
            var nodeQuery = SystemAPI.QueryBuilder()
                .WithAll<NodeId>()
                .Build();
                
            if (nodeQuery.IsEmpty)
                return Entity.Null;
                
            var entities = nodeQuery.ToEntityArray(Allocator.Temp);
            var nodeIds = nodeQuery.ToComponentDataArray<NodeId>(Allocator.Temp);
            
            for (int i = 0; i < entities.Length; i++)
            {
                if (nodeIds[i].Value == parentId)
                {
                    entities.Dispose();
                    nodeIds.Dispose();
                    return entities[i];
                }
            }
            
            entities.Dispose();
            nodeIds.Dispose();
            return Entity.Null;
        }

        private RoomBiomeData FindSiblingBiome(ref SystemState state, Entity roomEntity, NodeId nodeId)
        {
            // Look at connected/nearby rooms to infer biome type
            // Complete implementation using connection system for biome propagation

            if (_connectionLookup.HasBuffer(roomEntity))
            {
                var connections = _connectionLookup[roomEntity];
                foreach (var connection in connections)
                {
                    // Resolve connection to target entity and check for biome data
                    var targetEntity = FindEntityByNodeId(ref state, connection.ToNodeId.Value);
                    if (targetEntity != Entity.Null && _biomeLookup.HasComponent(targetEntity))
                    {
                        var targetBiome = _biomeLookup[targetEntity];
                        
                        // Propagate biome with some variation based on connection type
                        var influenceStrength = CalculateConnectionInfluence(connection);
                        if (influenceStrength > 0.7f)
                        {
                            // Strong connection - inherit biome with minimal variation
                            return new RoomBiomeData(
                                targetBiome.BiomeType,
                                targetBiome.Polarity,
                                targetBiome.ParentDistrict,
                                influenceStrength
                            );
                        }
                        else if (influenceStrength > 0.3f)
                        {
                            // Moderate connection - blend biomes
                            var blendedBiome = BlendBiomes(targetBiome.BiomeType, InferBiomeFromNodeId(nodeId));
                            return new RoomBiomeData(
                                blendedBiome,
                                targetBiome.Polarity,
                                targetBiome.ParentDistrict,
                                influenceStrength
                            );
                        }
                    }
                }
            }
            
            return RoomBiomeData.CreateUnresolved(Entity.Null);
        }

        
        private float CalculateConnectionInfluence(ConnectionBufferElement connection)
        {
            // Calculate how much influence this connection has on biome propagation
            // Based on connection distance, type, and strength
            var distance = math.distance(connection.FromNodeId.Value, connection.ToNodeId.Value);
            var maxDistance = 1000f; // Normalize distance factor
            var distanceFactor = 1.0f - math.min(distance / maxDistance, 1.0f);
            
            // Connection weight affects influence strength
            var weightFactor = connection.Weight / 100f; // Assuming weights are 0-100
            
            return distanceFactor * weightFactor;
        }
        
        private BiomeType BlendBiomes(BiomeType biome1, BiomeType biome2)
        {
            // Intelligent biome blending based on compatibility
            if (biome1 == biome2) return biome1;
            
            // Define biome compatibility and blending rules
            return (biome1, biome2) switch
            {
                (BiomeType.SkyGardens, BiomeType.HubArea) => BiomeType.SkyGardens,
                (BiomeType.HubArea, BiomeType.SkyGardens) => BiomeType.SkyGardens,
                (BiomeType.ShadowRealms, BiomeType.HubArea) => BiomeType.ShadowRealms,
                (BiomeType.HubArea, BiomeType.ShadowRealms) => BiomeType.ShadowRealms,
                _ => biome1 // Default to first biome when no specific rule
            };
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
            
            // Convert to manual EntityQuery iteration for source generator compatibility
            var entities = _roomsNeedingBiomeData.ToEntityArray(Allocator.Temp);
            var roomData = _roomsNeedingBiomeData.ToComponentDataArray<RoomHierarchyData>(Allocator.Temp);
            var nodeIds = _roomsNeedingBiomeData.ToComponentDataArray<NodeId>(Allocator.Temp);
            
            for (int i = 0; i < entities.Length; i++)
            {
                ecb.AddComponent(entities[i], new BiomeDataRequest(
                    priority: GetRequestPriority(roomData[i].Type),
                    allowDefaults: true,
                    maxSearchDepth: 5
                ));
            }
            
            entities.Dispose();
            roomData.Dispose();
            nodeIds.Dispose();
            
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
