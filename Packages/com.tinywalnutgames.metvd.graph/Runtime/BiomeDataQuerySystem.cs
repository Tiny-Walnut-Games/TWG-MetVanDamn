using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Biome;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// Component that holds cached biome information for a room
    /// </summary>
    public struct RoomBiomeData : IComponentData
    {
        public BiomeType BiomeType;
        public Polarity Polarity;
        public float DifficultyModifier;
        public Entity BiomeSource; // Entity that provided this biome data
        public bool IsResolved;
        
        public RoomBiomeData(BiomeType biomeType, Polarity polarity, float difficulty = 0.5f, Entity source = default)
        {
            BiomeType = biomeType;
            Polarity = polarity;
            DifficultyModifier = difficulty;
            BiomeSource = source;
            IsResolved = true;
        }
    }

    /// <summary>
    /// Component that requests biome data resolution for a room
    /// </summary>
    public struct BiomeDataRequest : IComponentData
    {
        public Entity RoomEntity;
        public NodeId ParentDistrict;
        public NodeId ParentSector;
        public bool RequireImmediate;
        
        public BiomeDataRequest(Entity room, NodeId district, NodeId sector, bool immediate = false)
        {
            RoomEntity = room;
            ParentDistrict = district;
            ParentSector = sector;
            RequireImmediate = immediate;
        }
    }

    /// <summary>
    /// System that resolves biome data from parent district/sector hierarchy
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(RoomManagementSystem))]
    public partial struct BiomeDataQuerySystem : ISystem
    {
        private ComponentLookup<BiomeFieldData> _biomeFieldLookup;
        private ComponentLookup<RoomBiomeData> _roomBiomeDataLookup;
        private BufferLookup<ConnectionBufferElement> _connectionLookup;
        private EntityQuery _requestQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _biomeFieldLookup = state.GetComponentLookup<BiomeFieldData>(true);
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
            _biomeFieldLookup.Update(ref state);
            _roomBiomeDataLookup.Update(ref state);
            _connectionLookup.Update(ref state);

            new BiomeDataQueryJob
            {
                BiomeFieldLookup = _biomeFieldLookup,
                RoomBiomeDataLookup = _roomBiomeDataLookup,
                ConnectionLookup = _connectionLookup,
                EntityCommandBuffer = state.GetEntityCommandBuffer()
            }.Schedule(_requestQuery, state.Dependency).Complete();
        }
    }

    /// <summary>
    /// Job that processes biome data requests by querying parent hierarchy
    /// </summary>
    [BurstCompile]
    public partial struct BiomeDataQueryJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<BiomeFieldData> BiomeFieldLookup;
        [ReadOnly] public ComponentLookup<RoomBiomeData> RoomBiomeDataLookup;
        [ReadOnly] public BufferLookup<ConnectionBufferElement> ConnectionLookup;
        public EntityCommandBuffer EntityCommandBuffer;

        public void Execute(Entity entity, in BiomeDataRequest request)
        {
            var biomeData = ResolveHierarchicalBiomeData(request);
            
            // Add resolved biome data to the room
            EntityCommandBuffer.AddComponent(request.RoomEntity, biomeData);
            
            // Remove the request component as it's now resolved
            EntityCommandBuffer.RemoveComponent<BiomeDataRequest>(entity);
        }

        private RoomBiomeData ResolveHierarchicalBiomeData(in BiomeDataRequest request)
        {
            // Try to get biome data from direct parent district
            if (TryGetBiomeFromDistrict(request.ParentDistrict, out var districtBiome))
            {
                return districtBiome;
            }

            // Try to get biome data from parent sector
            if (TryGetBiomeFromSector(request.ParentSector, out var sectorBiome))
            {
                return sectorBiome;
            }

            // Fall back to room characteristics-based biome assignment
            return CreateDefaultBiomeData(request.RoomEntity);
        }

        private bool TryGetBiomeFromDistrict(NodeId districtId, out RoomBiomeData biomeData)
        {
            biomeData = default;
            
            // Find district entity by NodeId (would typically use a lookup system)
            var districtEntity = FindEntityByNodeId(districtId);
            if (districtEntity == Entity.Null)
                return false;

            if (!BiomeFieldLookup.TryGetComponent(districtEntity, out var biomeField))
                return false;

            biomeData = new RoomBiomeData(
                biomeField.PrimaryBiome,
                Polarity.None, // BiomeFieldData doesn't have polarity, use default
                CalculateDifficultyFromDistrict(biomeField),
                districtEntity
            );
            return true;
        }

        private bool TryGetBiomeFromSector(NodeId sectorId, out RoomBiomeData biomeData)
        {
            biomeData = default;
            
            var sectorEntity = FindEntityByNodeId(sectorId);
            if (sectorEntity == Entity.Null)
                return false;

            if (!BiomeFieldLookup.TryGetComponent(sectorEntity, out var biomeField))
                return false;

            biomeData = new RoomBiomeData(
                biomeField.PrimaryBiome,
                Polarity.None, // BiomeFieldData doesn't have polarity, use default
                CalculateDifficultyFromSector(biomeField),
                sectorEntity
            );
            return true;
        }

        private RoomBiomeData CreateDefaultBiomeData(Entity roomEntity)
        {
            // Create intelligent defaults based on available room characteristics
            var biomeType = DetermineDefaultBiomeType(roomEntity);
            var polarity = DetermineDefaultPolarity(roomEntity);
            var difficulty = 0.5f; // Default medium difficulty

            return new RoomBiomeData(biomeType, polarity, difficulty, Entity.Null);
        }

        private BiomeType DetermineDefaultBiomeType(Entity roomEntity)
        {
            // In a full implementation, this would analyze room connections,
            // position in world hierarchy, etc.
            
            // For now, use room characteristics to make educated guesses
            if (RoomBiomeDataLookup.HasComponent(roomEntity))
            {
                // Use existing biome data if available
                var existing = RoomBiomeDataLookup[roomEntity];
                if (existing.IsResolved)
                    return existing.BiomeType;
            }

            // Default fallback based on room type
            return BiomeType.HubArea; // Safe default for most rooms
        }

        private Polarity DetermineDefaultPolarity(Entity roomEntity)
        {
            // Analyze room characteristics to determine appropriate polarity
            // This could be based on room type, connections, position, etc.
            
            return Polarity.None; // Neutral polarity as safe default
        }

        private float CalculateDifficultyFromDistrict(in BiomeFieldData biomeField)
        {
            // Calculate difficulty based on district biome properties
            return biomeField.PrimaryBiome switch
            {
                BiomeType.ShadowRealms => 0.8f,      // High difficulty
                BiomeType.PlasmaFields => 0.9f,      // Very high difficulty
                BiomeType.SkyGardens => 0.3f,        // Low difficulty
                BiomeType.HubArea => 0.2f,           // Very low difficulty
                _ => 0.5f                            // Medium difficulty default
            };
        }

        private float CalculateDifficultyFromSector(in BiomeFieldData biomeField)
        {
            // Sector-based difficulty is typically lower than district-based
            var baseDifficulty = CalculateDifficultyFromDistrict(biomeField);
            return math.max(0.1f, baseDifficulty - 0.2f); // Reduce by 0.2, minimum 0.1
        }

        private Entity FindEntityByNodeId(NodeId nodeId)
        {
            // In a full implementation, this would use a proper NodeId lookup system
            // For now, return Entity.Null to indicate not found
            // This would typically query a NodeIdLookup component lookup or similar
            return Entity.Null;
        }
    }
}