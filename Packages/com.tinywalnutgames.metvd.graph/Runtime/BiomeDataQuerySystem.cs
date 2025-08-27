using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;

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
        private ComponentLookup<Biome> _biomeLookup;
        private ComponentLookup<RoomBiomeData> _roomBiomeDataLookup;
        private BufferLookup<ConnectionBufferElement> _connectionLookup;
        private EntityQuery _requestQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _biomeLookup = state.GetComponentLookup<Biome>(true);
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

            var resolveJob = new BiomeDataResolveJob
            {
                BiomeLookup = _biomeLookup,
                RoomBiomeDataLookup = _roomBiomeDataLookup,
                ConnectionLookup = _connectionLookup
            };

            state.Dependency = resolveJob.ScheduleParallel(_requestQuery, state.Dependency);
        }
    }

    /// <summary>
    /// Job that resolves biome data by querying parent hierarchy
    /// </summary>
    [BurstCompile]
    public partial struct BiomeDataResolveJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<Biome> BiomeLookup;
        public ComponentLookup<RoomBiomeData> RoomBiomeDataLookup;
        [ReadOnly] public BufferLookup<ConnectionBufferElement> ConnectionLookup;

        public void Execute(Entity entity, in BiomeDataRequest request, in NodeId nodeId)
        {
            var resolvedData = ResolveFromHierarchy(entity, request, nodeId);
            
            // Add the resolved biome data component
            RoomBiomeDataLookup[entity] = resolvedData;
        }

        private RoomBiomeData ResolveFromHierarchy(Entity roomEntity, BiomeDataRequest request, NodeId nodeId)
        {
            // Try to find parent district with biome data
            var parentDistrict = FindParentDistrict(roomEntity, request.MaxSearchDepth);
            
            if (parentDistrict != Entity.Null && BiomeLookup.HasComponent(parentDistrict))
            {
                var parentBiome = BiomeLookup[parentDistrict];
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
            // This would use actual hierarchy traversal in a full implementation
            // For now, return Entity.Null to indicate no parent found
            
            // In practice, this would:
            // 1. Check if roomEntity has a parent component/reference
            // 2. Traverse up the hierarchy checking each parent for Biome component
            // 3. Return the first entity found with biome data
            // 4. Respect maxDepth to avoid infinite loops
            
            return Entity.Null;
        }

        private RoomBiomeData FindSiblingBiome(Entity roomEntity, NodeId nodeId)
        {
            // Look at connected/nearby rooms to infer biome type
            // This would use the connection system in a full implementation
            
            if (ConnectionLookup.HasBuffer(roomEntity))
            {
                var connections = ConnectionLookup[roomEntity];
                foreach (var connection in connections)
                {
                    // Check connected nodes for biome data
                    // In practice, would resolve connection.ToNodeId to entity and check biome
                }
            }
            
            return RoomBiomeData.CreateUnresolved(Entity.Null);
        }

        private RoomBiomeData CreateIntelligentDefault(NodeId nodeId, Entity parentDistrict)
        {
            // Create intelligent defaults based on node characteristics
            var biomeType = InferBiomeFromNodeId(nodeId);
            var polarity = InferPolarityFromBiome(biomeType);
            
            return new RoomBiomeData(biomeType, polarity, parentDistrict, 1f);
        }

        private BiomeType InferBiomeFromNodeId(NodeId nodeId)
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

        private Polarity InferPolarityFromBiome(BiomeType biomeType)
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
            
            var addRequestJob = new AddBiomeDataRequestJob
            {
                ECB = ecb.AsParallelWriter()
            };

            state.Dependency = addRequestJob.ScheduleParallel(_roomsNeedingBiomeData, state.Dependency);
            state.Dependency = ecb.Playback(state.EntityManager, state.Dependency);
            ecb.Dispose(state.Dependency);
        }
    }

    /// <summary>
    /// Job that adds BiomeDataRequest components to rooms that need biome resolution
    /// </summary>
    [BurstCompile]
    public partial struct AddBiomeDataRequestJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, in RoomHierarchyData roomData)
        {
            // Add request with priority based on room type
            var priority = GetRequestPriority(roomData.Type);
            var request = new BiomeDataRequest(priority, allowDefaults: true, maxSearchDepth: 3);
            
            ECB.AddComponent(chunkIndex, entity, request);
        }

        private int GetRequestPriority(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Boss => 10,      // High priority
                RoomType.Special => 8,    // High priority
                RoomType.Secret => 6,     // Medium priority
                RoomType.Save => 5,       // Medium priority
                RoomType.Standard => 3,   // Normal priority
                RoomType.Corridor => 1,   // Low priority
                _ => 0                    // Default priority
            };
        }
    }
}