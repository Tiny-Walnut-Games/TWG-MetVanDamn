using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Authoring
{
    /// <summary>
    /// Component data for tracking biome transition zones
    /// </summary>
    public struct BiomeTransition : IComponentData
    {
        /// <summary>
        /// Primary biome type in this transition
        /// </summary>
        public BiomeType FromBiome;
        
        /// <summary>
        /// Secondary biome type in this transition
        /// </summary>
        public BiomeType ToBiome;
        
        /// <summary>
        /// Transition strength (0.0 = fully FromBiome, 1.0 = fully ToBiome)
        /// </summary>
        public float TransitionStrength;
        
        /// <summary>
        /// Distance to the transition boundary
        /// </summary>
        public float DistanceToBoundary;
        
        /// <summary>
        /// Whether transition tiles have been applied
        /// </summary>
        public bool TransitionTilesApplied;
    }

    /// <summary>
    /// System for handling biome transitions and applying appropriate blend tiles
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(BiomeArtIntegrationSystem))]
    public partial struct BiomeTransitionSystem : ISystem
    {
        private ComponentLookup<Biome> biomeLookup;
        private ComponentLookup<BiomeTransition> transitionLookup;
        private ComponentLookup<NodeId> nodeIdLookup;
        private BufferLookup<ConnectionBufferElement> connectionBufferLookup;

        public void OnCreate(ref SystemState state)
        {
            biomeLookup = state.GetComponentLookup<Biome>(true);
            transitionLookup = state.GetComponentLookup<BiomeTransition>();
            nodeIdLookup = state.GetComponentLookup<NodeId>(true);
            connectionBufferLookup = state.GetBufferLookup<ConnectionBufferElement>(true);

            // Require biome components to run
            state.RequireForUpdate<Biome>();
        }

        public void OnUpdate(ref SystemState state)
        {
            biomeLookup.Update(ref state);
            transitionLookup.Update(ref state);
            nodeIdLookup.Update(ref state);
            connectionBufferLookup.Update(ref state);

            // Detect and create biome transitions
            var detectionJob = new BiomeTransitionDetectionJob
            {
                BiomeLookup = biomeLookup,
                TransitionLookup = transitionLookup,
                NodeIdLookup = nodeIdLookup,
                ConnectionBufferLookup = connectionBufferLookup
            };

            state.Dependency = detectionJob.ScheduleParallel(state.Dependency);
        }
    }

    /// <summary>
    /// Job for detecting biome transitions and calculating transition strengths
    /// </summary>
    [BurstCompile]
    public partial struct BiomeTransitionDetectionJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<Biome> BiomeLookup;
        public ComponentLookup<BiomeTransition> TransitionLookup;
        [ReadOnly] public ComponentLookup<NodeId> NodeIdLookup;
        [ReadOnly] public BufferLookup<ConnectionBufferElement> ConnectionBufferLookup;

        public readonly void Execute(Entity entity, ref Biome biome, in NodeId nodeId)
        {
            // Check if this entity has neighbors with different biomes
            if (!ConnectionBufferLookup.TryGetBuffer(entity, out var connections))
                return;

            bool hasTransition = false;
            BiomeType neighborBiome = BiomeType.Unknown;
            float minDistance = float.MaxValue;

            // Check connected nodes for biome differences
            for (int i = 0; i < connections.Length; i++)
            {
                var connection = connections[i].Value;
                uint neighborNodeId = connection.GetDestination(nodeId.Value);
                
                if (neighborNodeId == 0) continue; // Invalid connection
                
                // Find neighbor entity by NodeId (simplified - in production would use more efficient lookup)
                if (TryFindEntityByNodeId(neighborNodeId, out var neighborEntity))
                {
                    if (BiomeLookup.TryGetComponent(neighborEntity, out var neighborBiomeData))
                    {
                        if (neighborBiomeData.Type != biome.Type && neighborBiomeData.Type != BiomeType.Unknown)
                        {
                            hasTransition = true;
                            neighborBiome = neighborBiomeData.Type;
                            minDistance = math.min(minDistance, connection.TraversalCost);
                        }
                    }
                }
            }

            // Create or update transition component if needed
            if (hasTransition)
            {
                var transition = new BiomeTransition
                {
                    FromBiome = biome.Type,
                    ToBiome = neighborBiome,
                    TransitionStrength = CalculateTransitionStrength(minDistance),
                    DistanceToBoundary = minDistance,
                    TransitionTilesApplied = false
                };

                TransitionLookup.SetComponent(entity, transition);
            }
            else if (TransitionLookup.HasComponent(entity))
            {
                // Remove transition if no longer at a boundary
                TransitionLookup.SetComponent(entity, default(BiomeTransition));
            }
        }

        private readonly bool TryFindEntityByNodeId(uint nodeId, out Entity entity)
        {
            // Linear search through all entities with NodeId component
            foreach (var pair in NodeIdLookup)
            {
                if (pair.Value.Value == nodeId)
                {
                    entity = pair.Key;
                    return true;
                }
            }
            entity = Entity.Null;
            return false;
        }

        private readonly float CalculateTransitionStrength(float distance)
        {
            // Simple distance-based transition strength
            // Closer to boundary = higher transition strength
            const float maxTransitionDistance = 3.0f;
            return math.saturate(1.0f - (distance / maxTransitionDistance));
        }
    }

    /// <summary>
    /// Main thread system for applying transition tiles to tilemaps
    /// </summary>
    public partial class BiomeTransitionMainThreadSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // Process biome transitions that need tile application
            Entities
                .WithStructuralChanges()
                .ForEach((Entity entity, ref BiomeTransition transition, 
                         in Biome biome, in NodeId nodeId, 
                         in BiomeArtProfileReference artProfileRef) =>
                {
                    if (transition.TransitionTilesApplied || !artProfileRef.ProfileRef.IsValid)
                        return;

                    var artProfile = artProfileRef.ProfileRef.Value;
                    if (artProfile == null || artProfile.transitionTiles == null || artProfile.transitionTiles.Length == 0)
                        return;

                    // Apply transition tiles
                    ApplyTransitionTiles(artProfile, transition, nodeId);
                    
                    // Mark as applied
                    transition.TransitionTilesApplied = true;

                }).Run();
        }

        private void ApplyTransitionTiles(BiomeArtProfile artProfile, BiomeTransition transition, NodeId nodeId)
        {
            var grid = GameObject.FindObjectOfType<Grid>();
            if (grid == null) return;

            // Find appropriate tilemap layer for transition tiles
            // Prefer "Blending" layer if available, otherwise use first available layer
            Transform blendingLayer = grid.transform.Find("Blending");
            if (blendingLayer == null)
            {
                // Fallback to first child with Tilemap
                for (int i = 0; i < grid.transform.childCount; i++)
                {
                    var child = grid.transform.GetChild(i);
                    if (child.GetComponent<Tilemap>() != null)
                    {
                        blendingLayer = child;
                        break;
                    }
                }
            }

            if (blendingLayer == null) return;

            var tilemap = blendingLayer.GetComponent<Tilemap>();
            if (tilemap == null) return;

            // Select transition tile based on transition strength
            int tileIndex = Mathf.FloorToInt(transition.TransitionStrength * artProfile.transitionTiles.Length);
            tileIndex = Mathf.Clamp(tileIndex, 0, artProfile.transitionTiles.Length - 1);
            
            var transitionTile = artProfile.transitionTiles[tileIndex];
            if (transitionTile == null) return;

            // Calculate world position for transition tile
            Vector3Int position = new Vector3Int(nodeId.Coordinates.x, nodeId.Coordinates.y, 0);
            
            // Place transition tile
            tilemap.SetTile(position, transitionTile);
            
            Debug.Log($"Applied transition tile from {transition.FromBiome} to {transition.ToBiome} at {position} with strength {transition.TransitionStrength:F2}");
        }
    }
}