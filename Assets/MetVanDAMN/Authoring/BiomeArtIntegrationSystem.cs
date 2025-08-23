using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.GridLayerEditor;
using BiomeFieldSystem = TinyWalnutGames.MetVD.Biome.BiomeFieldSystem;

namespace TinyWalnutGames.MetVD.Authoring
{
    /// <summary>
    /// System responsible for applying biome art profiles to generated worlds
    /// Integrates with Grid Layer Editor for projection-aware tilemap creation
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(BiomeFieldSystem))]
    public partial struct BiomeArtIntegrationSystem : ISystem
    {
        private ComponentLookup<Biome> biomeLookup;
        private ComponentLookup<BiomeArtProfileReference> artProfileLookup;
        private ComponentLookup<NodeId> nodeIdLookup;

        public void OnCreate(ref SystemState state)
        {
            biomeLookup = state.GetComponentLookup<Biome>(true);
            artProfileLookup = state.GetComponentLookup<BiomeArtProfileReference>();
            nodeIdLookup = state.GetComponentLookup<NodeId>(true);

            // Require biome components to run
            state.RequireForUpdate<Biome>();
            state.RequireForUpdate<BiomeArtProfileReference>();
        }

        public void OnUpdate(ref SystemState state)
        {
            biomeLookup.Update(ref state);
            artProfileLookup.Update(ref state);
            nodeIdLookup.Update(ref state);

            // Process biome art integration on main thread since it interacts with GameObjects
            var job = new BiomeArtIntegrationJob
            {
                BiomeLookup = biomeLookup,
                ArtProfileLookup = artProfileLookup,
                NodeIdLookup = nodeIdLookup
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
    }

    /// <summary>
    /// Job for processing biome art integration
    /// Handles tilemap creation and prop placement for biomes
    /// </summary>
    [BurstCompile]
    public partial struct BiomeArtIntegrationJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<Biome> BiomeLookup;
        public ComponentLookup<BiomeArtProfileReference> ArtProfileLookup;
        [ReadOnly] public ComponentLookup<NodeId> NodeIdLookup;

        public readonly void Execute(Entity entity, ref BiomeArtProfileReference artProfile, in Biome biome, in NodeId nodeId)
        {
            // Skip if already applied or profile is null
            if (artProfile.IsApplied || !artProfile.ProfileRef.IsValid)
                return;

            // Mark as applied to prevent duplicate processing
            artProfile.IsApplied = true;

            // Schedule tilemap creation based on projection type
            // Note: Actual tilemap creation happens on main thread via hybrid components
            ScheduleTilemapCreation(artProfile.ProjectionType, biome, nodeId);
        }

        private readonly void ScheduleTilemapCreation(ProjectionType projectionType, Biome biome, NodeId nodeId)
        {
            // This method sets up data for main thread tilemap creation
            // The actual GameObject/Tilemap creation must happen on main thread
            // For now, we'll use a marker to indicate this biome needs tilemap creation
            
            // Future: Add to a command buffer or event system for main thread processing
            // For the initial implementation, we'll handle this in the main thread portion
        }
    }

    /// <summary>
    /// Hybrid component for main thread tilemap and prop processing
    /// Handles Unity GameObject and Tilemap creation which cannot be done in jobs
    /// </summary>
    public partial class BiomeArtMainThreadSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // Process biome art profiles that need tilemap creation
            Entities
                .WithStructuralChanges() // Allow structural changes for GameObject creation
                .ForEach((Entity entity, ref BiomeArtProfileReference artProfileRef, in Biome biome, in NodeId nodeId) =>
                {
                    if (artProfileRef.IsApplied || !artProfileRef.ProfileRef.IsValid)
                        return;

                    var artProfile = artProfileRef.ProfileRef.Value;
                    if (artProfile == null)
                        return;

                    // Create tilemap based on projection type
                    CreateBiomeSpecificTilemap(artProfileRef.ProjectionType, artProfile, biome, nodeId);
                    
                    // Place props
                    PlaceBiomeProps(artProfile, biome, nodeId);

                    // Mark as applied
                    artProfileRef.IsApplied = true;

                }).Run();
        }

        private void CreateBiomeSpecificTilemap(ProjectionType projectionType, BiomeArtProfile artProfile, Biome biome, NodeId nodeId)
        {
            // Get appropriate layer configuration based on projection type
            string[] layerNames = GetLayerNamesForProjection(projectionType);
            
            // Create grid with appropriate projection settings
            CreateProjectionAwareGrid(projectionType, layerNames, artProfile.biomeName);
            
            // Apply biome-specific tiles to the created layers
            ApplyBiomeTilesToLayers(artProfile, layerNames);
        }

        private string[] GetLayerNamesForProjection(ProjectionType projectionType)
        {
            return projectionType switch
            {
                ProjectionType.Platformer => System.Enum.GetNames(typeof(TwoDimensionalGridSetup.SideScrollingLayers)),
                ProjectionType.TopDown => System.Enum.GetNames(typeof(TwoDimensionalGridSetup.TopDownLayers)),
                ProjectionType.Isometric => System.Enum.GetNames(typeof(IsometricHexagonalLayers)),
                ProjectionType.Hexagonal => System.Enum.GetNames(typeof(IsometricHexagonalLayers)),
                _ => System.Enum.GetNames(typeof(TwoDimensionalGridSetup.TopDownLayers))
            };
        }

        private Grid CreateProjectionAwareGrid(ProjectionType projectionType, string[] layerNames, string biomeName)
        {
            // Call appropriate TwoDimensionalGridSetup method based on projection
            Grid createdGrid = null;
            switch (projectionType)
            {
                case ProjectionType.Platformer:
                    createdGrid = TwoDimensionalGridSetup.CreateSideScrollingGrid();
                    break;
                case ProjectionType.TopDown:
                    createdGrid = TwoDimensionalGridSetup.CreateDefaultTopDownGrid();
                    break;
                case ProjectionType.Isometric:
                    createdGrid = TwoDimensionalGridSetup.CreateIsometricTopDownGrid();
                    break;
                case ProjectionType.Hexagonal:
                    createdGrid = TwoDimensionalGridSetup.CreateHexTopDownGrid();
                    break;
                default:
                    createdGrid = TwoDimensionalGridSetup.CreateDefaultTopDownGrid();
                    break;
            }

            // Rename the created grid to include biome information
            if (createdGrid != null)
            {
                createdGrid.name = $"{biomeName} Grid ({projectionType})";
            }
            return createdGrid;
        }

        private void ApplyBiomeTilesToLayers(BiomeArtProfile artProfile, string[] layerNames, Grid grid)
        {
            if (grid == null) return;

            foreach (string layerName in layerNames)
            {
                var layerObject = grid.transform.Find(layerName);
                if (layerObject == null) continue;

                var tilemap = layerObject.GetComponent<Tilemap>();
                var renderer = layerObject.GetComponent<TilemapRenderer>();
                
                if (tilemap == null || renderer == null) continue;

                // Apply biome-specific tiles based on layer type
                ApplyTileToLayer(tilemap, renderer, layerName, artProfile);
            }
        }

        private void ApplyTileToLayer(Tilemap tilemap, TilemapRenderer renderer, string layerName, BiomeArtProfile artProfile)
        {
            TileBase tileToApply = null;
            
            // Determine which tile to use based on layer name
            if (layerName.Contains("Floor") || layerName.Contains("Ground"))
            {
                tileToApply = artProfile.floorTile;
            }
            else if (layerName.Contains("Wall") || layerName.Contains("Hazards"))
            {
                tileToApply = artProfile.wallTile;
            }
            else if (layerName.Contains("Background") || layerName.Contains("Parallax"))
            {
                tileToApply = artProfile.backgroundTile;
            }

            if (tileToApply != null)
            {
                // For demonstration, place a single tile at origin
                // In production, this would be driven by the actual world generation data
                Vector3Int position = Vector3Int.zero;
                tilemap.SetTile(position, tileToApply);
            }

            // Apply material and sorting layer overrides if specified
            if (!string.IsNullOrEmpty(artProfile.sortingLayerOverride))
            {
                renderer.sortingLayerName = artProfile.sortingLayerOverride;
            }

            if (artProfile.materialOverride != null)
            {
                renderer.material = artProfile.materialOverride;
            }
        }

        private void PlaceBiomeProps(BiomeArtProfile artProfile, Biome biome, NodeId nodeId)
        {
            if (artProfile.propPrefabs == null || artProfile.propPrefabs.Length == 0)
                return;

            var grid = GameObject.FindObjectOfType<Grid>();
            if (grid == null) return;

            // Place props in allowed layers
            foreach (string layerName in artProfile.allowedPropLayers)
            {
                if (UnityEngine.Random.value <= artProfile.propSpawnChance)
                {
                    PlacePropInLayer(grid, layerName, artProfile, nodeId);
                }
            }
        }

        private void PlacePropInLayer(Grid grid, string layerName, BiomeArtProfile artProfile, NodeId nodeId)
        {
            var layerObject = grid.transform.Find(layerName);
            if (layerObject == null) return;

            // Select random prop prefab
            if (artProfile.propPrefabs.Length == 0) return;
            int propIndex = UnityEngine.Random.Range(0, artProfile.propPrefabs.Length);
            var propPrefab = artProfile.propPrefabs[propIndex];
            
            if (propPrefab == null) return;

            // Calculate world position based on node coordinates
            Vector3 worldPosition = new Vector3(nodeId.Coordinates.x, nodeId.Coordinates.y, 0);
            
            // Instantiate prop
            var propInstance = GameObject.Instantiate(propPrefab, worldPosition, Quaternion.identity, layerObject);
            propInstance.name = $"{artProfile.biomeName} Prop ({propIndex})";
        }
    }
}