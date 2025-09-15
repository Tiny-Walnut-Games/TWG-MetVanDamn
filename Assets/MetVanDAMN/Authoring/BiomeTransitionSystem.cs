using TinyWalnutGames.MetVD.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
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
            // Build a lookup from NodeId._value -> Entity for neighbor resolution (one per frame)
            NativeArray<Entity> nodeEntities = _biomeNodeQuery.ToEntityArray(Allocator.Temp);
            NativeArray<NodeId> nodeIds = _biomeNodeQuery.ToComponentDataArray<NodeId>(Allocator.Temp);
            var nodeMap = new NativeHashMap<uint, Entity>(nodeEntities.Length, Allocator.Temp);
            for (int i = 0; i < nodeEntities.Length; i++)
            {
                nodeMap.TryAdd(nodeIds[i]._value, nodeEntities[i]);
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
                        Connection connection = connections[i].Value;
                        uint neighborNodeId = connection.GetDestination(nodeId._value);
                        if (neighborNodeId == 0)
                        {
                            continue;
                        }

                        if (nodeMap.TryGetValue(neighborNodeId, out Entity neighborEntity))
                        {
                            if (EntityManager.HasComponent<CoreBiome>(neighborEntity))
                            {
                                CoreBiome neighborBiomeData = EntityManager.GetComponentData<CoreBiome>(neighborEntity);
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
                        {
                            EntityManager.SetComponentData(entity, transition);
                        }
                        else
                        {
                            EntityManager.AddComponentData(entity, transition);
                        }
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
            // Collect candidates on main thread then apply painter outside the Entities.ForEach to avoid method capture issues
            var candidates = new System.Collections.Generic.List<(Entity entity, BiomeArtProfile profile, BiomeTransition transition, NodeId nodeId)>();

            Entities
                .WithName("BiomeTransitionApplyCollect")
                .WithStructuralChanges()
                .WithoutBurst()
                .ForEach((Entity entity, ref BiomeTransition transition,
                          in CoreBiome biome, in NodeId nodeId,
                          in BiomeArtProfileReference artProfileRef) =>
                {
                    if (transition.TransitionTilesApplied)
                        return;

                    if (!artProfileRef.ProfileRef.IsValid())
                        return;

                    BiomeArtProfile artProfile = artProfileRef.ProfileRef.Value;
                    if (artProfile == null)
                        return;

                    bool hasArray = artProfile.transitionTiles != null && artProfile.transitionTiles.Length > 0;
                    bool hasExplicit = artProfile.transitionFromTile != null || artProfile.transitionBlendA != null || artProfile.transitionBlendB != null || artProfile.transitionToTile != null;
                    if (!hasArray && !hasExplicit)
                        return;

                    candidates.Add((entity, artProfile, transition, nodeId));
                }).Run();

            // Apply transitions using instance helper
            for (int i = 0; i < candidates.Count; i++)
            {
                // deconstruct to satisfy analyzers
                (Entity entity, BiomeArtProfile profile, BiomeTransition transition, NodeId nodeId) = candidates[i];
                ApplyTransitionTiles(profile, transition, nodeId);
                // mark applied
                var updated = transition;
                updated.TransitionTilesApplied = true;
                if (EntityManager.HasComponent<BiomeTransition>(entity))
                {
                    EntityManager.SetComponentData(entity, updated);
                }
            }
        }

        // Utility: try to find a BiomeArtProfile asset for a given BiomeType in loaded resources
        private BiomeArtProfile GetArtProfileForBiome(BiomeType biomeType)
        {
#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("t:BiomeArtProfile");
            foreach (var g in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<BiomeArtProfile>(path);
                if (profile != null && string.Equals(profile.biomeName, biomeType.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return profile;
                }
            }
#endif
            return null;
        }

        private static Color GenerateHashBasedBiomeColor(BiomeType biome)
        {
            // Simple deterministic color based on biome enum value
            int v = (int)biome;
            uint hash = (uint)(v * 2654435761u);
            float hue = (hash % 360u) / 360f;
            var c = Color.HSVToRGB(hue, 0.55f, 0.9f);
            return c;
        }

        private void ApplyTransitionTiles(BiomeArtProfile artProfile, BiomeTransition transition, NodeId nodeId)
        {
            // Painter: blend between FromBiome -> BlendA -> BlendB -> ToBiome
            // Find or create a blending tilemap layer inside the Grid
            Grid grid = UnityEngine.Object.FindFirstObjectByType<Grid>();
            if (grid == null) return;

            Tilemap blendingTilemap = null;
            Transform blendingTransform = grid.transform.Find("Blending");
            if (blendingTransform != null)
            {
                blendingTilemap = blendingTransform.GetComponent<Tilemap>();
            }
            else
            {
                // Attempt to locate by convention
                for (int i = 0; i < grid.transform.childCount; i++)
                {
                    var child = grid.transform.GetChild(i);
                    if (child.name == "Blending")
                    {
                        blendingTilemap = child.GetComponent<Tilemap>();
                        break;
                    }
                    if (blendingTilemap == null && child.GetComponent<Tilemap>() != null)
                  {
                      // Use first available as fallback
                      blendingTilemap = child.GetComponent<Tilemap>();
                  }
                }
            }

            if (blendingTilemap == null)
            {
                // Create a blending layer under the grid for painter to use
                var blendingGO = new GameObject("Blending", typeof(Tilemap), typeof(TilemapRenderer));
                blendingGO.transform.SetParent(grid.transform, false);
                blendingTilemap = blendingGO.GetComponent<Tilemap>();
            }

            // Choose tiles for blend bands: explicit hooks preferred, then array, then fallbacks
            TileBase[] tiles = artProfile.transitionTiles ?? new TileBase[0];
            TileBase tileFrom = artProfile.transitionFromTile ?? artProfile.floorTile; // explicit From tile preferred
            TileBase tileTo = artProfile.transitionToTile ?? (tiles.Length > 0 ? tiles[^1] : artProfile.floorTile);
            TileBase blendA = artProfile.transitionBlendA ?? (tiles.Length >= 1 ? tiles[0] : null);
            TileBase blendB = artProfile.transitionBlendB ?? (tiles.Length >= 2 ? tiles[1] : blendA);

            // Compute the tile positions to paint within the transition radius
            int radius = Mathf.Max(1, Mathf.CeilToInt(transition.DistanceToBoundary));
            int centerX = nodeId.Coordinates.x;
            int centerY = nodeId.Coordinates.y;

            // Painting loop: banded radial blending
            for (int ox = -radius; ox <= radius; ox++)
            {
                for (int oy = -radius; oy <= radius; oy++)
                {
                    int tx = centerX + ox;
                    int ty = centerY + oy;
                    float distance = math.sqrt(ox * ox + oy * oy);
                    if (distance > radius) continue;

                    float t = math.clamp(distance / (float)radius, 0f, 1f);
                    // Deadzone controls central blended band's half-width (normalized 0..0.5)
                    float deadzone = math.clamp(artProfile.transitionDeadzone, 0f, 0.5f);
                    float lower = 0.5f - deadzone;
                    float upper = 0.5f + deadzone;
                    TileBase chosen = null;
                    if (t <= lower)
                    {
                        // Fully in "From" band
                        chosen = tileFrom ?? blendA;
                    }
                    else if (t >= upper)
                    {
                        // Fully in "To" band
                        chosen = tileTo ?? blendB;
                    }
                    else
                    {
                        // Inside deadzone: choose blendA or blendB based on local position and transition strength
                        float local = (t - lower) / math.max(1e-6f, (upper - lower)); // 0..1
                        // Prefer blendB when transition strength is high, otherwise blendA
                        float bias = transition.TransitionStrength;
                        // combine local and bias to decide mid tile; threshold at 0.5
                        float score = math.lerp(local, bias, 0.5f);
                        chosen = (score < 0.5f) ? (blendA ?? blendB) : (blendB ?? blendA);
                    }

                    // Final tile selection with fallbacks
                    TileBase finalTile = chosen ?? tileFrom ?? tileTo;
                    if (finalTile == null) continue;

                    // Color fallback: if explicit transition tiles are missing, tint the chosen tile
                    // Determine colors for blending: prefer art profiles' debugColor, fallback to hash-based color
                    Color colorA = artProfile != null ? artProfile.debugColor : new Color(0.5f, 0.5f, 0.5f, 1f);
                    var otherProfile = GetArtProfileForBiome(transition.ToBiome);
                    Color colorB = otherProfile != null ? otherProfile.debugColor : GenerateHashBasedBiomeColor(transition.ToBiome);
                    // Blend factor uses normalized distance and transition strength bias
                    float blendFactor = math.clamp((t * 0.5f) + (transition.TransitionStrength * 0.5f), 0f, 1f);

                    var cell = new Vector3Int(tx, ty, 0);
                    blendingTilemap.SetTile(cell, finalTile);
                    blendingTilemap.SetColor(cell, Color.Lerp(colorA, colorB, blendFactor));
                }
            }

            Debug.Log($"Applied blended transition tiles for {transition.FromBiome}->{transition.ToBiome} at node {nodeId.Value} (r={radius})");
        }
    }
}
