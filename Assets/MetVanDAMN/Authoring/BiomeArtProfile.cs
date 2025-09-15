using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

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
	/// Debug visualization settings for checkered material override system
	/// Enables coordinate-aware procedural material generation for biome debugging
	/// </summary>
	[System.Serializable]
	public class CheckeredMaterialSettings
		{
		[Header("Checkered Material Override")]
		[Tooltip("Enable procedural checkered material generation for debug visualization")]
		public bool enableCheckerOverride = false;

		[Header("Coordinate Intelligence")]
		[Range(0f, 2f), Tooltip("How strongly world coordinates influence pattern complexity")]
		public float coordinateInfluenceStrength = 0.7f;

		[Range(0.5f, 2f), Tooltip("Distance from origin scaling factor for pattern detail")]
		public float distanceScalingFactor = 1.0f;

		[Tooltip("Enable coordinate-based pattern warping for organic appearance")]
		public bool enableCoordinateWarping = true;

		[Header("Animation & Dynamics")]
		[Range(0f, 1f), Tooltip("Animation speed for polarity-based material effects")]
		public float polarityAnimationSpeed = 0.2f;

		[Range(0.5f, 3f), Tooltip("Multiplier for complexity-based visual enhancements")]
		public float complexityTierMultiplier = 1.2f;

		[Header("Pattern Customization")]
		[Range(2, 32), Tooltip("Base checker size in pixels (coordinate complexity modifies this)")]
		public int baseCheckerSize = 8;

		[Tooltip("Use mathematical patterns (Fibonacci, prime numbers) to influence generation")]
		public bool useMathematicalPatterns = true;

		[Range(0f, 1f), Tooltip("Strength of mathematical pattern influence on checker generation")]
		public float mathematicalPatternStrength = 0.5f;

		[Header("Biome-Specific Overrides")]
		[Tooltip("Use biome-specific color relationships for secondary checker colors")]
		public bool useBiomeColorHarmony = true;

		[Range(0f, 1f), Tooltip("Intensity of biome-specific visual effects")]
		public float biomeVisualizationIntensity = 0.8f;

		/// <summary>
		/// Converts these settings to the runtime complexity settings structure
		/// Enables seamless integration with the BiomeCheckerMaterialOverride system
		/// </summary>
		public BiomeCheckerMaterialOverride.CheckerComplexitySettings ToComplexitySettings()
			{
			return new BiomeCheckerMaterialOverride.CheckerComplexitySettings
				{
				coordinateInfluenceStrength = coordinateInfluenceStrength,
				distanceScalingFactor = distanceScalingFactor,
				polarityAnimationSpeed = polarityAnimationSpeed,
				enableCoordinateWarping = enableCoordinateWarping,
				complexityTierMultiplier = complexityTierMultiplier
				};
			}

		/// <summary>
		/// Creates default settings optimized for biome debugging and visualization
		/// Provides balanced coordinate-awareness without overwhelming visual complexity
		/// </summary>
		public static CheckeredMaterialSettings CreateDebugOptimized()
			{
			return new CheckeredMaterialSettings
				{
				enableCheckerOverride = true,
				coordinateInfluenceStrength = 0.6f,
				distanceScalingFactor = 1.0f,
				enableCoordinateWarping = true,
				polarityAnimationSpeed = 0.15f,
				complexityTierMultiplier = 1.0f,
				baseCheckerSize = 8,
				useMathematicalPatterns = true,
				mathematicalPatternStrength = 0.3f,
				useBiomeColorHarmony = true,
				biomeVisualizationIntensity = 0.9f
				};
			}

		/// <summary>
		/// Creates settings optimized for performance with minimal coordinate influence
		/// Suitable for runtime use where visual fidelity is less critical than performance
		/// </summary>
		public static CheckeredMaterialSettings CreatePerformanceOptimized()
			{
			return new CheckeredMaterialSettings
				{
				enableCheckerOverride = true,
				coordinateInfluenceStrength = 0.3f,
				distanceScalingFactor = 0.5f,
				enableCoordinateWarping = false,
				polarityAnimationSpeed = 0f,
				complexityTierMultiplier = 0.8f,
				baseCheckerSize = 16,
				useMathematicalPatterns = false,
				mathematicalPatternStrength = 0f,
				useBiomeColorHarmony = true,
				biomeVisualizationIntensity = 0.6f
				};
			}
		}

	/// <summary>
	/// Comprehensive prop placement configuration for advanced biome art
	/// </summary>
	[System.Serializable]
	public class PropPlacementSettings
		{
		[Header("Basic Settings")]
		[Tooltip("Prefabs to spawn as props in this biome")]
		public GameObject [ ] propPrefabs = new GameObject [ 0 ];

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
		public TileBase [ ] transitionTiles;

		[Header("Explicit Transition Mapping")]
		[Tooltip("Optional explicit tile to use as the source (From) transition tile. If null, floorTile is used as fallback.")]
		public TileBase transitionFromTile;

		[Tooltip("Optional explicit tile for the first blend band (BlendA).")]
		public TileBase transitionBlendA;

		[Tooltip("Optional explicit tile for the second blend band (BlendB).")]
		public TileBase transitionBlendB;

		[Tooltip("Optional explicit tile to use as the destination (To) transition tile. If null, last entry of transitionTiles or floorTile is used.")]
		public TileBase transitionToTile;

		[Header("Transition Settings")]
		[Range(0f, 0.5f), Tooltip("Half-width of the central blend band as a normalized value (0..0.5).\n" +
		    "Lower values make a sharp cut between biomes; higher values broaden the blended area.")]
		public float transitionDeadzone = 0.1f;

		[Header("Props")]
		[Tooltip("Advanced prop placement configuration for this biome.")]
		public PropPlacementSettings propSettings;

		[Header("Debug Visualization")]
		[Tooltip("Checkered material override settings for coordinate-aware debugging")]
		public CheckeredMaterialSettings checkerSettings = new();

		[Header("Advanced")]
		[Tooltip("Optional sorting layer override for biome visuals.")]
		public string sortingLayerOverride;

		[Tooltip("Optional material override for biome visuals.")]
		public Material materialOverride;

		// Convenience properties for compatibility with existing code
		/// <summary>
		/// Convenient access to prop prefabs for backward compatibility
		/// </summary>
		public GameObject [ ] PropPrefabs => propSettings?.propPrefabs ?? new GameObject [ 0 ];

		/// <summary>
		/// Convenient access to all tiles for backward compatibility
		/// </summary>
		public TileBase [ ] Tiles
			{
			get
				{
				var tileList = new List<TileBase>();
				if (floorTile != null)
					{
					tileList.Add(floorTile);
					}

				if (wallTile != null)
					{
					tileList.Add(wallTile);
					}

				if (backgroundTile != null)
					{
					tileList.Add(backgroundTile);
					}

				if (transitionTiles != null)
					{
					tileList.AddRange(transitionTiles);
					}

				return tileList.ToArray();
				}
			}

		/// <summary>
		/// Applies checkered material override to the specified tilemap if enabled
		/// Integrates seamlessly with the BiomeCheckerMaterialOverride system
		/// </summary>
		public void ApplyCheckerOverrideIfEnabled(Tilemap tilemap, Core.BiomeType biome,
			Core.NodeId nodeId)
			{
			if (checkerSettings?.enableCheckerOverride == true && tilemap != null)
				{
				BiomeCheckerMaterialOverride.CheckerComplexitySettings complexitySettings = checkerSettings.ToComplexitySettings();
				BiomeCheckerMaterialOverride.ApplyCheckerOverrideToTilemap(tilemap, biome, nodeId, complexitySettings);
				}
			}
		}
	}
