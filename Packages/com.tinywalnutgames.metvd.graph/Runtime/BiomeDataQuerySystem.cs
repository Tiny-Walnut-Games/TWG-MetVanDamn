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
        public BiomeType BiomeType;
        public Polarity PrimaryPolarity;
        public Polarity SecondaryPolarity;
        public float DifficultyModifier;
        public bool IsResolved;
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
        public int Priority;
        public bool AllowDefaults;
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
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(RoomManagementSystem))]
    public partial struct BiomeDataQuerySystem : ISystem
    {
        // NOTE: Fully qualify Core.Biome to avoid ambiguity with any Biome namespace
        private ComponentLookup<TinyWalnutGames.MetVD.Core.Biome> _biomeLookup;
        private ComponentLookup<RoomBiomeData> _roomBiomeDataLookup;
        private BufferLookup<ConnectionBufferElement> _connectionLookup;
        private EntityQuery _requestQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _biomeLookup = state.GetComponentLookup<TinyWalnutGames.MetVD.Core.Biome>(true);
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
        [ReadOnly] public ComponentLookup<TinyWalnutGames.MetVD.Core.Biome> BiomeLookup;
        public ComponentLookup<RoomBiomeData> RoomBiomeDataLookup;
        [ReadOnly] public BufferLookup<ConnectionBufferElement> ConnectionLookup;

        public void Execute(Entity entity, in BiomeDataRequest request, in NodeId nodeId)
        {
            var resolvedData = ResolveFromHierarchy(entity, request, nodeId);
            RoomBiomeDataLookup[entity] = resolvedData;
        }

        private RoomBiomeData ResolveFromHierarchy(Entity roomEntity, BiomeDataRequest request, NodeId nodeId)
        {
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

            var siblingBiome = FindSiblingBiome(roomEntity, nodeId);
            if (siblingBiome.BiomeType != BiomeType.Unknown)
                return siblingBiome;

            if (request.AllowDefaults)
                return CreateIntelligentDefault(nodeId, parentDistrict);

            return RoomBiomeData.CreateUnresolved(parentDistrict);
        }

        private Entity FindParentDistrict(Entity roomEntity, int maxDepth)
        {
            return Entity.Null; // Placeholder traversal
        }

        private RoomBiomeData FindSiblingBiome(Entity roomEntity, NodeId nodeId)
        {
            if (ConnectionLookup.HasBuffer(roomEntity))
            {
                var connections = ConnectionLookup[roomEntity];
                foreach (var connection in connections)
                {
                    // Future: inspect connected entities for existing biome data
                }
            }
            return RoomBiomeData.CreateUnresolved(Entity.Null);
        }

        private RoomBiomeData CreateIntelligentDefault(NodeId nodeId, Entity parentDistrict)
        {
            var biomeType = InferBiomeFromNodeId(nodeId);
            var polarity = InferPolarityFromBiome(biomeType);
            return new RoomBiomeData(biomeType, polarity, parentDistrict, 1f);
        }

        private BiomeType InferBiomeFromNodeId(NodeId nodeId)
        {
            var hash = nodeId.Value;
            var biomeIndex = hash % 8;
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
                _ => BiomeType.HubArea
            };
        }

        private Polarity InferPolarityFromBiome(BiomeType biomeType) => biomeType switch
        {
            BiomeType.SkyGardens => Polarity.Wind,
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

            // NOTE: Original playback pattern (ecb.Playback(state.EntityManager, state.Dependency)) removed because
            // the overload with JobHandle does not exist in the current Entities version. We complete the scheduled
            // job then playback/dispose explicitly. (Original code kept in comment for reference.)
            // ORIGINAL (legacy):
            // state.Dependency = addRequestJob.ScheduleParallel(_roomsNeedingBiomeData, state.Dependency);
            // state.Dependency = ecb.Playback(state.EntityManager, state.Dependency);
            // ecb.Dispose(state.Dependency);

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var addRequestJob = new AddBiomeDataRequestJob
            {
                ECB = ecb.AsParallelWriter()
            };
            state.Dependency = addRequestJob.ScheduleParallel(_roomsNeedingBiomeData, state.Dependency);
            state.Dependency.Complete(); // Ensure job finished before playback (non-destructive deterministic pattern)
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
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
            var priority = GetRequestPriority(roomData.Type);
            var request = new BiomeDataRequest(priority, allowDefaults: true, maxSearchDepth: 3);
            ECB.AddComponent(chunkIndex, entity, request);
        }

        private int GetRequestPriority(RoomType roomType)
        {
            // Updated to reflect current RoomType enum (removed: Special, Secret, Standard, Corridor)
            return roomType switch
            {
                RoomType.Boss => 10,
                RoomType.Treasure => 8,
                RoomType.Entrance => 7,
                RoomType.Exit => 7,
                RoomType.Save => 5,
                RoomType.Shop => 5,
                RoomType.Normal => 3,
                RoomType.Hub => 2,
                _ => 1
            };
        }
    }
}
