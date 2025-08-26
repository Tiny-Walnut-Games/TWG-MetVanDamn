using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace TinyWalnutGames.MetVD.Authoring
{
    /// <summary>
    /// Prop placement strategy for biome-specific behavior
    /// </summary>
    public enum PropPlacementStrategy
    {
        Random,          // Random scatter (good for generic decoration)
        Clustered,       // Form natural clusters (good for vegetation, rocks)
        Sparse,          // Sparse but meaningful placement (good for special items)
        Linear,          // Follow lines or edges (good for fence posts, paths)
        Radial,          // Radiate from center points (good for settlements, clearings)
        Terrain          // Follow terrain features (good for water plants, cliff vegetation)
    }

    /// <summary>
    /// Prop clustering behavior settings
    /// </summary>
    [System.Serializable]
    public class ClusteringSettings
    {
        [Range(2, 20), Tooltip("Number of props per cluster")]
        public int clusterSize = 5;

        [Range(0.5f, 10f), Tooltip("Radius of each cluster in world units")]
        public float clusterRadius = 3f;

        [Range(0f, 1f), Tooltip("How tightly packed props are within clusters")]
        public float clusterDensity = 0.7f;

        [Range(5f, 50f), Tooltip("Minimum distance between cluster centers")]
        public float clusterSeparation = 15f;
    }

    /// <summary>
    /// Advanced avoidance rules for prop placement
    /// </summary>
    [System.Serializable]
    public class AvoidanceSettings
    {
        [Tooltip("Layers to avoid when placing props")]
        public List<string> avoidLayers = new() { "Hazards", "Walls" };

        [Range(0.5f, 5f), Tooltip("Minimum distance from avoided features")]
        public float avoidanceRadius = 1.5f;

        [Tooltip("Avoid biome transition areas")]
        public bool avoidTransitions = true;

        [Range(0.5f, 10f), Tooltip("Distance from biome edges to avoid")]
        public float transitionAvoidanceRadius = 2f;

        [Tooltip("Avoid spawning props too close to each other")]
        public bool avoidOvercrowding = true;

        [Range(0.5f, 5f), Tooltip("Minimum distance between props")]
        public float minimumPropDistance = 1f;
    }

    /// <summary>
    /// Size and rotation variation settings for natural placement
    /// </summary>
    [System.Serializable]
    public class VariationSettings
    {
        [Header("Size Variation")]
        [Range(0.5f, 2f), Tooltip("Minimum scale multiplier")]
        public float minScale = 0.8f;

        [Range(0.5f, 2f), Tooltip("Maximum scale multiplier")]
        public float maxScale = 1.2f;

        [Header("Rotation Variation")]
        [Tooltip("Allow random Y-axis rotation")]
        public bool randomRotation = true;

        [Range(0f, 360f), Tooltip("Maximum rotation angle in degrees")]
        public float maxRotationAngle = 360f;

        [Header("Position Variation")]
        [Range(0f, 1f), Tooltip("Random offset within tile bounds")]
        public float positionJitter = 0.3f;
    }

    /// <summary>
    /// Comprehensive prop placement configuration for advanced biome art
    /// </summary>
    [System.Serializable]
    public class PropPlacementSettings
    {
        [Header("Basic Settings")]
        [Tooltip("Prefabs to spawn as props in this biome")]
        public GameObject[] propPrefabs = new GameObject[0];

        [Tooltip("Names of tilemap layers where props can be placed")]
        public List<string> allowedPropLayers = new();

        [Header("Placement Strategy")]
        [Tooltip("How props should be distributed in this biome")]
        public PropPlacementStrategy strategy = PropPlacementStrategy.Random;

        [Header("Density Control")]
        [Range(0f, 1f), Tooltip("Base spawn probability per eligible tile")]
        public float baseDensity = 0.1f;

        [Tooltip("Density curve based on distance from biome center (0=edge, 1=center)")]
        public AnimationCurve densityCurve = AnimationCurve.Linear(0f, 0.5f, 1f, 1f);

        [Range(0f, 2f), Tooltip("Multiplier for overall prop density")]
        public float densityMultiplier = 1f;

        [Header("Advanced Placement")]
        public ClusteringSettings clustering = new();
        public AvoidanceSettings avoidance = new();
        public VariationSettings variation = new();

        [Header("Performance")]
        [Range(10, 1000), Tooltip("Maximum props to place per biome instance")]
        public int maxPropsPerBiome = 100;

        [Tooltip("Use spatial optimization for large biomes")]
        public bool useSpatialOptimization = true;
    }

    [CreateAssetMenu(
        fileName = "BiomeArtProfile",
        menuName = "MetVanDAMN/Biome Art Profile",
        order = 0)]
    public class BiomeArtProfile : ScriptableObject
    {
        [Header("Biome Identity")]
        public string biomeName;
        public Color debugColor = Color.white;

        [Header("Tilemap Art")]
        [Tooltip("Floor tile (preferably RuleTile from Unity 2D Tilemap Extras)")]
        public TileBase floorTile;

        [Tooltip("Wall tile (preferably RuleTile from Unity 2D Tilemap Extras)")]
        public TileBase wallTile;

        [Tooltip("Background tile (preferably RuleTile from Unity 2D Tilemap Extras)")]
        public TileBase backgroundTile;

        [Tooltip("Optional tiles for biome-to-biome transitions.")]
        public TileBase[] transitionTiles;

        [Header("Props")]
        [Tooltip("Advanced prop placement configuration for this biome.")]
        public PropPlacementSettings propSettings;

        [Header("Advanced")]
        [Tooltip("Optional sorting layer override for biome visuals.")]
        public string sortingLayerOverride;

        [Tooltip("Optional material override for biome visuals.")]
        public Material materialOverride;
        
        // Convenience properties for compatibility with existing code
        /// <summary>
        /// Convenient access to prop prefabs for backward compatibility
        /// </summary>
        public GameObject[] propPrefabs => propSettings?.propPrefabs ?? new GameObject[0];
        
        /// <summary>
        /// Convenient access to all tiles for backward compatibility
        /// </summary>
        public TileBase[] tiles
        {
            get
            {
                var tileList = new List<TileBase>();
                if (floorTile != null) tileList.Add(floorTile);
                if (wallTile != null) tileList.Add(wallTile);
                if (backgroundTile != null) tileList.Add(backgroundTile);
                if (transitionTiles != null) tileList.AddRange(transitionTiles);
                return tileList.ToArray();
            }
        }
    }
}
