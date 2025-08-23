using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;
using TinyWalnutGames.MetVD.Biome;
using TinyWalnutGames.MetVD.Core;
using System.Collections.Generic;

// Alias core biome component to avoid namespace ambiguity
using CoreBiome = TinyWalnutGames.MetVD.Core.Biome;

namespace TinyWalnutGames.MetVD.Authoring
{
    /// <summary>
    /// Component data for tracking biome transition zones
    /// </summary>
    public struct BiomeTransition : IComponentData
    {
        public BiomeType FromBiome;
        public BiomeType ToBiome;
        public float TransitionStrength; // 0 = fully FromBiome, 1 = fully ToBiome
        public float DistanceToBoundary;
        public bool TransitionTilesApplied;
    }

    /// <summary>
    /// System for handling biome transitions and applying appropriate blend tiles
    /// Refactored to SystemBase with structural changes for clarity over performance.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(BiomeArtIntegrationSystem))]
    public partial class BiomeTransitionSystem : SystemBase
    {
        private EntityQuery _biomeNodeQuery;

        protected override void OnCreate()
        {
            // Query for nodes that can participate in transitions (have biome + node id + connections)
            _biomeNodeQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<CoreBiome>(),
                    ComponentType.ReadOnly<NodeId>(),
                    ComponentType.ReadOnly<ConnectionBufferElement>()
                }
            });
        }

        protected override void OnUpdate()
        {
            // Build a lookup from NodeId.Value -> Entity for neighbor resolution (one per frame)
            var nodeEntities = _biomeNodeQuery.ToEntityArray(Allocator.Temp);
            var nodeIds = _biomeNodeQuery.ToComponentDataArray<NodeId>(Allocator.Temp);
            var nodeMap = new NativeHashMap<uint, Entity>(nodeEntities.Length, Allocator.Temp);
            for (int i = 0; i < nodeEntities.Length; i++)
            {
                nodeMap.TryAdd(nodeIds[i].Value, nodeEntities[i]);
            }

            // Process transitions
            Entities
                .WithName("BiomeTransitionDetection")
                .WithReadOnly(nodeMap)
                .WithStructuralChanges()
                .ForEach((Entity entity,
                          ref CoreBiome biome,
                          in NodeId nodeId,
                          in DynamicBuffer<ConnectionBufferElement> connections) =>
                {
                    bool hasTransition = false;
                    BiomeType neighborBiome = BiomeType.Unknown;
                    float minDistance = float.MaxValue;

                    for (int i = 0; i < connections.Length; i++)
                    {
                        var connection = connections[i].Value;
                        uint neighborNodeId = connection.GetDestination(nodeId.Value);
                        if (neighborNodeId == 0) continue;
                        if (nodeMap.TryGetValue(neighborNodeId, out var neighborEntity))
                        {
                            if (EntityManager.HasComponent<CoreBiome>(neighborEntity))
                            {
                                var neighborBiomeData = EntityManager.GetComponentData<CoreBiome>(neighborEntity);
                                if (neighborBiomeData.Type != biome.Type && neighborBiomeData.Type != BiomeType.Unknown)
                                {
                                    hasTransition = true;
                                    neighborBiome = neighborBiomeData.Type;
                                    minDistance = math.min(minDistance, connection.TraversalCost);
                                }
                            }
                        }
                    }

                    if (hasTransition)
                    {
                        float strength = CalculateTransitionStrength(minDistance);
                        var transition = new BiomeTransition
                        {
                            FromBiome = biome.Type,
                            ToBiome = neighborBiome,
                            TransitionStrength = strength,
                            DistanceToBoundary = minDistance,
                            TransitionTilesApplied = false
                        };

                        if (EntityManager.HasComponent<BiomeTransition>(entity))
                            EntityManager.SetComponentData(entity, transition);
                        else
                            EntityManager.AddComponentData(entity, transition);
                    }
                    else
                    {
                        if (EntityManager.HasComponent<BiomeTransition>(entity))
                        {
                            // Remove transition component if no longer valid
                            EntityManager.RemoveComponent<BiomeTransition>(entity);
                        }
                    }
                }).Run();

            nodeEntities.Dispose();
            nodeIds.Dispose();
            nodeMap.Dispose();
        }

        private static float CalculateTransitionStrength(float distance)
        {
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
            Entities
                .WithStructuralChanges()
                .ForEach((Entity entity, ref BiomeTransition transition,
                         in CoreBiome biome, in NodeId nodeId,
                         in BiomeArtProfileReference artProfileRef) =>
                {
                    if (transition.TransitionTilesApplied)
                        return;

                    var isValid = artProfileRef.ProfileRef.IsValid();
                    if (!isValid) return;

                    var artProfile = artProfileRef.ProfileRef.Value;
                    if (artProfile == null || artProfile.transitionTiles == null || artProfile.transitionTiles.Length == 0)
                        return;

                    ApplyTransitionTiles(artProfile, transition, nodeId);
                    transition.TransitionTilesApplied = true;
                }).Run();
        }

        private void ApplyTransitionTiles(BiomeArtProfile artProfile, BiomeTransition transition, NodeId nodeId)
        {
            var grid = GameObject.FindFirstObjectByType<Grid>();
            if (grid == null) return;

            Transform blendingLayer = grid.transform.Find("Blending");
            if (blendingLayer == null)
            {
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

            if (!blendingLayer.TryGetComponent<Tilemap>(out var tilemap)) return;

            int tileIndex = Mathf.FloorToInt(transition.TransitionStrength * artProfile.transitionTiles.Length);
            tileIndex = Mathf.Clamp(tileIndex, 0, artProfile.transitionTiles.Length - 1);
            var transitionTile = artProfile.transitionTiles[tileIndex];
            if (transitionTile == null) return;

            Vector3Int position = new(nodeId.Coordinates.x, nodeId.Coordinates.y, 0);
            tilemap.SetTile(position, transitionTile);
            Debug.Log($"Applied transition tile from {transition.FromBiome} to {transition.ToBiome} at {position} strength {transition.TransitionStrength:F2}");
        }
    }
}
