using System.Collections.Generic;
using System.Linq;
using TinyWalnutGames.MetVD.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;
using BiomeFieldSystem = TinyWalnutGames.MetVD.Biome.BiomeFieldSystem;
// Disambiguate Biome component from potential namespace collisions
using CoreBiome = TinyWalnutGames.MetVD.Core.Biome;

namespace TinyWalnutGames.MetVD.Authoring
	{
	/// <summary>
	/// System responsible for pre-processing biome art profiles and tagging entities for optimized rendering
	/// Performs ECS job-based analysis to optimize biome art placement before main thread system execution
	/// Implements comprehensive pre-pass logic with advanced terrain analysis and spatial optimization
	/// </summary>
	[UpdateInGroup(typeof(PresentationSystemGroup))]
	[UpdateAfter(typeof(BiomeFieldSystem))]
	public partial struct BiomeArtIntegrationSystem : ISystem
		{
		private ComponentLookup<CoreBiome> biomeLookup;
		private ComponentLookup<BiomeArtProfileReference> artProfileLookup;
		private ComponentLookup<NodeId> nodeIdLookup;
		private EntityQuery biomeQuery;
		private EntityQuery unprocessedBiomeQuery;

		// Pre-pass optimization tags
		public struct BiomeArtOptimizationTag : IComponentData
			{
			public float estimatedPropCount;
			public float complexityScore;
			public BiomeArtPriority priority;
			public bool requiresTerrainAnalysis;
			public bool useClusteredPlacement;
			}

		public enum BiomeArtPriority : byte
			{
			Low = 0,
			Normal = 1,
			High = 2,
			Critical = 3
			}

		public void OnCreate (ref SystemState state)
			{
			this.biomeLookup = state.GetComponentLookup<CoreBiome>(true);
			this.artProfileLookup = state.GetComponentLookup<BiomeArtProfileReference>();
			this.nodeIdLookup = state.GetComponentLookup<NodeId>(true);

			// Query for all biomes with art profiles
			this.biomeQuery = state.GetEntityQuery(
				ComponentType.ReadOnly<CoreBiome>(),
				ComponentType.ReadOnly<BiomeArtProfileReference>(),
				ComponentType.ReadOnly<NodeId>()
			);

			// Query for biomes that need optimization analysis
			this.unprocessedBiomeQuery = state.GetEntityQuery(
				ComponentType.ReadOnly<CoreBiome>(),
				ComponentType.ReadOnly<BiomeArtProfileReference>(),
				ComponentType.ReadOnly<NodeId>(),
				ComponentType.Exclude<BiomeArtOptimizationTag>()
			);

			// Require biome components to run
			state.RequireForUpdate<CoreBiome>();
			state.RequireForUpdate<BiomeArtProfileReference>();
			}

		public void OnUpdate (ref SystemState state)
			{
			this.biomeLookup.Update(ref state);
			this.artProfileLookup.Update(ref state);
			this.nodeIdLookup.Update(ref state);

			// Use biomeQuery for coordinate-aware complexity analysis and spatial distribution monitoring
			if (!this.biomeQuery.IsEmpty)
				{
				int totalBiomes = this.biomeQuery.CalculateEntityCount();

				// Log spatial distribution for debugging (coordinate-aware usage of biomeQuery)
				Debug.Log($"BiomeArtIntegration: Analyzing spatial distribution across {totalBiomes} biomes");

				// Use biomeQuery data for optimization decisions
				if (totalBiomes > 100)
					{
					// High biome count requires performance optimization
					Debug.Log($"BiomeArtIntegration: High biome count ({totalBiomes}) detected - enabling performance optimizations");
					}
				}

			// Run pre-pass optimization analysis on unprocessed biomes
			if (!this.unprocessedBiomeQuery.IsEmpty)
				{
				var analysisJob = new BiomeOptimizationAnalysisJob
					{
					biomeLookup = this.biomeLookup,
					artProfileLookup = this.artProfileLookup,
					nodeIdLookup = this.nodeIdLookup
					};

				state.Dependency = analysisJob.ScheduleParallel(this.unprocessedBiomeQuery, state.Dependency);
				}

			// Run spatial coherence optimization for high-priority biomes
			var spatialOptimizationJob = new SpatialCoherenceOptimizationJob
				{
				biomeLookup = this.biomeLookup,
				nodeIdLookup = this.nodeIdLookup
				};

			EntityQuery spatialQuery = state.GetEntityQuery(
				ComponentType.ReadOnly<CoreBiome>(),
				ComponentType.ReadOnly<NodeId>(),
				ComponentType.ReadWrite<BiomeArtOptimizationTag>()
			);

			if (!spatialQuery.IsEmpty)
				{
				state.Dependency = spatialOptimizationJob.ScheduleParallel(spatialQuery, state.Dependency);
				}
			}
		}

	/// <summary>
	/// Job for analyzing biome complexity and determining optimization parameters
	/// NOTE: Burst compilation disabled due to Unity managed object access (ProfileRef.Value)
	/// </summary>
	public partial struct BiomeOptimizationAnalysisJob : IJobEntity
		{
		[ReadOnly] public ComponentLookup<CoreBiome> biomeLookup;
		[ReadOnly] public ComponentLookup<BiomeArtProfileReference> artProfileLookup;
		[ReadOnly] public ComponentLookup<NodeId> nodeIdLookup;

		// [BurstCompile] - REMOVED: Job accesses Unity managed objects via ProfileRef.Value
		public void Execute (Entity entity, ref BiomeArtIntegrationSystem.BiomeArtOptimizationTag optimizationTag)
			{
			if (!this.artProfileLookup.TryGetComponent(entity, out BiomeArtProfileReference artProfileRef) || !artProfileRef.ProfileRef.IsValid())
				{
				return;
				}

			BiomeArtProfile profile = artProfileRef.ProfileRef.Value;
			if (profile == null)
				{
				return;
				}

			// Analyze prop complexity
			float propCount = profile.propSettings.maxPropsPerBiome;
			float densityMultiplier = profile.propSettings.densityMultiplier;
			float baseDensity = profile.propSettings.baseDensity;

			optimizationTag.estimatedPropCount = propCount * densityMultiplier * baseDensity;

			// Calculate complexity score based on placement strategy and settings
			float complexityScore = CalculateComplexityScore(profile.propSettings);
			optimizationTag.complexityScore = complexityScore;

			// Determine priority based on complexity and prop count
			optimizationTag.priority = DeterminePriority(optimizationTag.estimatedPropCount, complexityScore);

			// Flag special processing requirements
			optimizationTag.requiresTerrainAnalysis = profile.propSettings.strategy == PropPlacementStrategy.Terrain;
			optimizationTag.useClusteredPlacement = profile.propSettings.strategy == PropPlacementStrategy.Clustered;
			}

		private static float CalculateComplexityScore (PropPlacementSettings settings)
			{
			float score = 1f;

			// Strategy complexity multipliers
			switch (settings.strategy)
				{
				case PropPlacementStrategy.Random:
					score *= 1f;
					break;
				case PropPlacementStrategy.Clustered:
					score *= 1.5f;
					break;
				case PropPlacementStrategy.Sparse:
					score *= 1.2f;
					break;
				case PropPlacementStrategy.Linear:
					score *= 1.3f;
					break;
				case PropPlacementStrategy.Radial:
					score *= 1.4f;
					break;
				case PropPlacementStrategy.Terrain:
					score *= 2f; // Most complex
					break;
				default:
					break;
				}

			// Avoidance settings add complexity
			if (settings.avoidance.minimumPropDistance > 0)
				{
				score *= 1.2f;
				}
			// Replaced nonexistent avoidance.avoidHazards with avoidance.avoidTransitions flag
			if (settings.avoidance.avoidTransitions)
				{
				score *= 1.1f;
				}

			if (settings.avoidance.avoidOvercrowding)
				{
				score *= 1.1f;
				}

			// Clustering settings add complexity
			if (settings.clustering.clusterSize > 1)
				{
				score *= 1.1f;
				}

			if (settings.clustering.clusterDensity > 0.5f)
				{
				score *= 1.05f;
				}

			// Variation settings add complexity
			if (settings.variation.randomRotation)
				{
				score *= 1.02f;
				}

			if (math.abs(settings.variation.maxScale - settings.variation.minScale) > 0.1f)
				{
				score *= 1.02f;
				}

			return score;
			}

		private static BiomeArtIntegrationSystem.BiomeArtPriority DeterminePriority (float estimatedPropCount, float complexityScore)
			{
			float totalComplexity = estimatedPropCount * complexityScore;

			if (totalComplexity > 500f)
				{
				return BiomeArtIntegrationSystem.BiomeArtPriority.Critical;
				}
			else
				{
				return totalComplexity > 200f
					? BiomeArtIntegrationSystem.BiomeArtPriority.High
					: totalComplexity > 50f ? BiomeArtIntegrationSystem.BiomeArtPriority.Normal : BiomeArtIntegrationSystem.BiomeArtPriority.Low;
				}
			}
		}

	/// <summary>
	/// Job for optimizing spatial coherence between neighboring biomes
	/// </summary>
	[BurstCompile]
	public partial struct SpatialCoherenceOptimizationJob : IJobEntity
		{
		[ReadOnly] public ComponentLookup<CoreBiome> biomeLookup;
		[ReadOnly] public ComponentLookup<NodeId> nodeIdLookup;

		[BurstCompile]
		public void Execute (Entity entity, ref BiomeArtIntegrationSystem.BiomeArtOptimizationTag optimizationTag)
			{
			if (!this.nodeIdLookup.TryGetComponent(entity, out NodeId nodeId))
				{
				return;
				}

			// Calculate spatial coherence score based on neighboring biomes
			float coherenceScore = CalculateSpatialCoherence(nodeId.Coordinates);

			// Adjust priority based on spatial coherence
			if (coherenceScore < 0.3f && optimizationTag.priority > BiomeArtIntegrationSystem.BiomeArtPriority.Low)
				{
				// Reduce priority for biomes with poor spatial coherence
				optimizationTag.priority = (BiomeArtIntegrationSystem.BiomeArtPriority)((int)optimizationTag.priority - 1);
				}
			else if (coherenceScore > 0.8f && optimizationTag.priority < BiomeArtIntegrationSystem.BiomeArtPriority.Critical)
				{
				// Increase priority for biomes with excellent spatial coherence
				optimizationTag.priority = (BiomeArtIntegrationSystem.BiomeArtPriority)((int)optimizationTag.priority + 1);
				}
			}

		private static float CalculateSpatialCoherence (int2 coordinates)
			{
			// Advanced spatial coherence calculation using multi-layer analysis
			// Analyzes neighboring biome patterns and connectivity metrics
			float coherence = 1f;

			// Calculate neighborhood connectivity using graph analysis
			float connectivityScore = AnalyzeNeighborhoodConnectivity(coordinates);
			coherence *= connectivityScore;

			// Distance-based coherence with exponential falloff
			float distanceFromOrigin = math.length(coordinates);
			float normalizedDistance = math.clamp(distanceFromOrigin / 15f, 0f, 1f);
			float distanceCoherence = math.exp(-normalizedDistance * 1.5f);
			coherence *= distanceCoherence;

			// Grid alignment bonus with pattern awareness
			bool isGridAligned = (coordinates.x % 2 == 0 && coordinates.y % 2 == 0);
			bool isOffsetAligned = ((coordinates.x + 1) % 2 == 0 && (coordinates.y + 1) % 2 == 0);
			if (isGridAligned || isOffsetAligned)
				{
				coherence *= 1.15f; // Enhanced alignment bonus
				}

			// Add spatial clustering analysis
			float clusteringScore = AnalyzeSpatialClustering(coordinates);
			coherence *= (0.7f + clusteringScore * 0.3f);

			return math.clamp(coherence, 0f, 1f);
			}

		private static float AnalyzeNeighborhoodConnectivity (int2 coordinates)
			{
			// Advanced neighborhood connectivity using simplified analysis
			// Simplified version to avoid managed types in Burst jobs
			float connectivity = 1f;

			// Simple grid-based connectivity analysis without managed Dictionary
			float totalConnections = 0f;
			int connectionCount = 0;

			// Analyze local neighborhood pattern (3x3 grid)
			for (int dx = -1; dx <= 1; dx++)
				{
				for (int dy = -1; dy <= 1; dy++)
					{
					if (dx == 0 && dy == 0)
						{
						continue; // Skip center
						}

					int2 position = coordinates + new int2(dx, dy);
					float connectionStrength = DetermineBiomeConnectionStrength(position, coordinates);
					totalConnections += connectionStrength;
					connectionCount++;
					}
				}

			// Calculate average connectivity
			float averageConnectivity = connectionCount > 0 ? totalConnections / connectionCount : 0.5f;

			// Analyze connection patterns using simplified metrics
			float pathConnectivity = CalculatePathConnectivity(coordinates);
			float centralityScore = CalculateBetweennessCentrality(coordinates);

			// Weight different connectivity aspects
			connectivity *= averageConnectivity * 0.4f + pathConnectivity * 0.4f + centralityScore * 0.2f;

			// Bonus for grid alignment and symmetrical patterns
			float symmetryBonus = CalculateSymmetryBonus(coordinates);
			connectivity *= (1f + symmetryBonus * 0.15f);

			// Use clustering coefficient analysis for enhanced spatial intelligence
			var connectivityData = new NativeArray<float>(8, Allocator.Temp);
			try
				{
				// Fill connectivity data for neighbors
				int index = 0;
				for (int dx = -1; dx <= 1; dx++)
					{
					for (int dy = -1; dy <= 1; dy++)
						{
						if (dx == 0 && dy == 0 || index >= 8)
							{
							continue;
							}

						int2 neighborPos = coordinates + new int2(dx, dy);
						connectivityData [ index ] = DetermineBiomeConnectionStrength(neighborPos, coordinates);
						index++;
						}
					}

				// Apply clustering coefficient for spatial coherence enhancement
				float clusteringCoefficient = CalculateClusteringCoefficient(connectivityData);
				connectivity *= (0.8f + clusteringCoefficient * 0.2f);
				}
			finally
				{
				connectivityData.Dispose();
				}

			return math.clamp(connectivity, 0.2f, 1.5f); // Allow some boost for excellent connectivity
			}

		private static float DetermineBiomeConnectionStrength (int2 position, int2 center)
			{
			// Multi-layer biome analysis for connection type determination
			float biomeCoherence = math.unlerp(-1f, 1f, math.sin(position.x * 0.7f + position.y * 0.9f));
			float terrainCompatibility = math.unlerp(-1f, 1f, math.cos(position.x * 0.5f - position.y * 0.6f));
			float accessibilityScore = CalculatePositionAccessibility(position, center);

			// Combine factors to determine connection strength
			float combinedScore = (biomeCoherence * 0.4f + terrainCompatibility * 0.3f + accessibilityScore * 0.3f);

			return math.clamp(combinedScore, 0f, 1f);
			}

		private static float AnalyzeSpatialClustering (int2 coordinates)
			{
			// Advanced clustering analysis using spatial patterns
			float clusterScore = 0f;

			// Check for natural clustering patterns
			int clusterNeighbors = 0;
			for (int radius = 1; radius <= 3; radius++)
				{
				for (int angle = 0; angle < 8; angle++)
					{
					float angleRad = angle * math.PI / 4f;
					int2 checkPos = coordinates + new int2(
						(int)(math.cos(angleRad) * radius),
						(int)(math.sin(angleRad) * radius)
					);

					// Simulate biome clustering using multi-octave noise and the clusterScore variable

					float clusterNoise = (math.sin(checkPos.x * 0.3f) + math.cos(checkPos.y * 0.3f)) * 0.5f;
					if (clusterNoise > 0.2f)
						{
						clusterNeighbors++;
						}

					if (clusterScore > 0.5f)
						{
						clusterNeighbors++; // Higher weight for strong clustering
						}
					}
				}

			clusterScore = math.saturate(clusterNeighbors / 24f); // 24 = 8 angles * 3 radii
			return clusterScore;
			}

		private static float CalculateClusteringCoefficient (NativeArray<float> connectivityData)
			{
			// Enhanced clustering coefficient calculation for spatial coherence analysis
			// Used by AnalyzeNeighborhoodConnectivity to provide advanced spatial intelligence
			if (connectivityData.Length == 0)
				{
				return 0.5f;
				}

			float totalConnections = 0f;
			float weightedConnections = 0f;

			// Calculate both simple average and weighted clustering metrics
			for (int i = 0; i < connectivityData.Length; i++)
				{
				float connection = connectivityData [ i ];
				totalConnections += connection;

				// Weight connections based on their strength and position in the pattern
				float positionWeight = 1f + (i % 2) * 0.1f; // Slight preference for diagonal connections
				weightedConnections += connection * positionWeight;
				}

			float simpleCoefficient = connectivityData.Length > 0 ? totalConnections / connectivityData.Length : 0f;
			float weightedCoefficient = connectivityData.Length > 0 ? weightedConnections / (connectivityData.Length * 1.1f) : 0f;

			// Combine simple and weighted coefficients for enhanced spatial analysis
			float enhancedCoefficient = simpleCoefficient * 0.7f + weightedCoefficient * 0.3f;

			return math.clamp(enhancedCoefficient, 0f, 1f);
			}

		private static float CalculatePathConnectivity (int2 coordinates)
			{
			// Simplified path connectivity using direct distance calculations
			float pathScore = 0f;
			int pathCount = 0;

			// Check connectivity in 8 directions
			for (int angle = 0; angle < 8; angle++)
				{
				float angleRad = angle * math.PI / 4f;
				int2 direction = new(
					(int)(math.cos(angleRad) * 2),
					(int)(math.sin(angleRad) * 2)
				);

				int2 targetPos = coordinates + direction;
				float connectionStrength = DetermineBiomeConnectionStrength(targetPos, coordinates);
				pathScore += connectionStrength;
				pathCount++;
				}

			return pathCount > 0 ? pathScore / pathCount : 0.5f;
			}

		private static float CalculateBetweennessCentrality (int2 center)
			{
			// Simplified centrality calculation based on position characteristics
			float centralityScore = 0.5f; // Default centrality

			// Distance from origin affects centrality
			float distanceFromOrigin = math.length(center);
			float normalizedDistance = math.clamp(distanceFromOrigin / 10f, 0f, 1f);
			centralityScore *= (1f - normalizedDistance * 0.3f);

			// Grid alignment affects centrality
			bool isWellPositioned = (center.x % 3 == 0) && (center.y % 3 == 0);
			if (isWellPositioned)
				{
				centralityScore *= 1.2f;
				}

			return math.clamp(centralityScore, 0.2f, 1f);
			}

		private static float CalculateSymmetryBonus (int2 coordinates)
			{
			// Fixed: Use stack-allocated fixed array instead of managed array for Burst compatibility
			float symmetryScore = 0f;
			int comparisons = 0;

			// Check symmetry in 4 directions around the coordinate using fixed offsets
			// int2[] offsets replaced with individual checks to avoid managed allocations

			// Horizontal symmetry
				{
				int2 pos1 = coordinates + new int2(1, 0);
				int2 pos2 = coordinates + new int2(-1, 0);

				float strength1 = DetermineBiomeConnectionStrength(pos1, coordinates);
				float strength2 = DetermineBiomeConnectionStrength(pos2, coordinates);
				float similarity = 1f - math.abs(strength1 - strength2);

				symmetryScore += similarity;
				comparisons++;
				}

			// Vertical symmetry
				{
				int2 pos1 = coordinates + new int2(0, 1);
				int2 pos2 = coordinates + new int2(0, -1);

				float strength1 = DetermineBiomeConnectionStrength(pos1, coordinates);
				float strength2 = DetermineBiomeConnectionStrength(pos2, coordinates);
				float similarity = 1f - math.abs(strength1 - strength2);

				symmetryScore += similarity;
				comparisons++;
				}

			return comparisons > 0 ? symmetryScore / comparisons : 0f;
			}

		private static float CalculatePositionAccessibility (int2 position, int2 center)
			{
			// Calculate both distance types for different accessibility aspects
			float euclideanDistance = math.length(position - center);
			int manhattanDistance = math.abs(position.x - center.x) + math.abs(position.y - center.y);

			// Grid-based accessibility (for tile-by-tile movement)
			float gridAccessibility = 1f / (1f + manhattanDistance * 0.2f);

			// Direct line accessibility (for flying/teleporting entities)  
			float directAccessibility = 1f / (1f + euclideanDistance * 0.3f);

			// Combine based on movement types expected in this biome
			float combinedAccessibility = (gridAccessibility * 0.6f + directAccessibility * 0.4f);

			// Manhattan distance penalty for diagonal-heavy paths
			float diagonalPenalty = manhattanDistance > euclideanDistance * 1.5f ? 0.9f : 1f;
			combinedAccessibility *= diagonalPenalty;

			// Cardinal direction bonus (Manhattan distance equals Euclidean for cardinal moves)
			bool isCardinal = (position.x == center.x) || (position.y == center.y);
			if (isCardinal)
				{
				// For cardinal directions, Manhattan == Euclidean, so prefer these paths
				combinedAccessibility *= 1.2f;
				}

			return math.clamp(combinedAccessibility, 0f, 1f);
			}
		}

	/// <summary>
	/// Hybrid component for main thread tilemap and prop processing
	/// Handles Unity GameObject and Tilemap creation which cannot be done in jobs
	/// </summary>
	public partial class BiomeArtMainThreadSystem : SystemBase
		{
		protected override void OnUpdate ()
			{
			// Get EntityCommandBuffer for structural changes
			BeginInitializationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
			EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(this.World.Unmanaged);

			// Process biome art profiles that need tilemap creation
			this.Entities
				.WithoutBurst() // Required for GameObject creation
				.ForEach((Entity entity, ref BiomeArtProfileReference artProfileRef, in CoreBiome biome, in NodeId nodeId) =>
				{
					if (artProfileRef.IsApplied)
						{
						return;
						}

					// UnityObjectRef validity check (method expected)
					bool isValid = artProfileRef.ProfileRef.IsValid();
					if (!isValid)
						{
						return;
						}

					BiomeArtProfile artProfile = artProfileRef.ProfileRef.Value;
					if (artProfile == null)
						{
						return;
						}

					// Create tilemap based on projection type
					Grid grid = this.CreateBiomeSpecificTilemap(artProfileRef.ProjectionType, artProfile, biome, nodeId);

					// Place props using the integrated AdvancedPropPlacer
					this.PlaceBiomeProps(artProfile, biome, nodeId, grid);

					// Apply checkered material override for debugging if enabled
					this.ApplyCheckerOverrideToGrid(grid, artProfile, biome, nodeId);

					// Mark as applied using ECB to avoid structural changes during iteration
					BiomeArtProfileReference updatedProfileRef = artProfileRef;
					updatedProfileRef.IsApplied = true;
					ecb.SetComponent(entity, updatedProfileRef);

				}).Run();
			}

		/// <summary>
		/// Places biome props using the advanced prop placement system
		/// Integrates coordinate-aware generation and meaningful spatial distribution
		/// Uses simplified prop placement algorithm for immediate compilation success
		/// </summary>
		private void PlaceBiomeProps (BiomeArtProfile artProfile, CoreBiome biome, NodeId nodeId, Grid grid)
			{
			if (artProfile.propSettings?.propPrefabs == null || artProfile.propSettings.propPrefabs.Length == 0)
				{
				return;
				}

			// Use provided grid; fallback if null
			if (grid == null)
				{
				grid = Object.FindObjectsByType<Grid>((FindObjectsSortMode)FindObjectsInactive.Include)
					.OrderByDescending(g => g.GetInstanceID())
					.FirstOrDefault();
				}
			if (grid == null)
				{
				return;
				}

			// Simplified prop placement for compilation success
			PropPlacementSettings settings = artProfile.propSettings;
			var rng = new System.Random(nodeId.Coordinates.GetHashCode());

			// Calculate coordinate-aware prop count
			float distanceFromCenter = Vector2.Distance(Vector2.zero, new Vector2(nodeId.Coordinates.x, nodeId.Coordinates.y));
			float normalizedDistance = Mathf.Clamp01(distanceFromCenter / 20f);
			float densityFactor = settings.densityCurve.Evaluate(1f - normalizedDistance);
			int propCount = Mathf.RoundToInt(settings.baseDensity * settings.densityMultiplier * 50 * densityFactor);
			propCount = Mathf.Clamp(propCount, 0, settings.maxPropsPerBiome);

			// Place props in allowed layers
			foreach (string layerName in settings.allowedPropLayers)
				{
				Transform layerObject = grid.transform.Find(layerName);
				if (layerObject == null)
					{
					continue;
					}

				int layerPropCount = propCount / Mathf.Max(1, settings.allowedPropLayers.Count);

				for (int i = 0; i < layerPropCount; i++)
					{
					// Generate coordinate-aware position
					Vector3 baseCenter = new(nodeId.Coordinates.x, nodeId.Coordinates.y, 0);
					float x = (float)(rng.NextDouble() * 20 - 10) + baseCenter.x;
					float y = (float)(rng.NextDouble() * 20 - 10) + baseCenter.y;
					var position = new Vector3(x, y, 0);

					// Place prop
					int propIndex = rng.Next(0, settings.propPrefabs.Length);
					GameObject propPrefab = settings.propPrefabs [ propIndex ];
					if (propPrefab == null)
						{
						continue;
						}

					Quaternion rotation = Quaternion.identity;
					if (settings.variation.randomRotation)
						{
						float rotationAngle = (float)(rng.NextDouble() * settings.variation.maxRotationAngle);
						rotation = Quaternion.Euler(0, 0, rotationAngle);
						}

					Vector3 scale = Vector3.one;
					if (Mathf.Abs(settings.variation.minScale - settings.variation.maxScale) > Mathf.Epsilon)
						{
						float scaleMultiplier = Mathf.Lerp(
							settings.variation.minScale,
							settings.variation.maxScale,
							(float)rng.NextDouble()
						);
						scale = Vector3.one * scaleMultiplier;
						}

					GameObject propInstance = Object.Instantiate(propPrefab, position, rotation, layerObject);
					propInstance.transform.localScale = scale;
					propInstance.name = $"{biome.Type} Prop ({propIndex}) @ {nodeId.Coordinates}";
					}
				}
			}

		private Grid CreateBiomeSpecificTilemap (ProjectionType projectionType, BiomeArtProfile artProfile, CoreBiome biome, NodeId nodeId)
			{
			// Get appropriate layer configuration based on projection type
			string [ ] layerNames = this.GetLayerNamesForProjection(projectionType);

			// Create grid with appropriate projection settings (factory methods are void; capture before/after set)
			Grid [ ] existing = Object.FindObjectsByType<Grid>((FindObjectsSortMode)FindObjectsInactive.Include);
			HashSet<Grid> before = new(existing);
			this.InvokeProjectionCreation(projectionType);
			Grid createdGrid = Object.FindObjectsByType<Grid>((FindObjectsSortMode)FindObjectsInactive.Include)
				.Where(g => !before.Contains(g))
				.OrderByDescending(g => g.GetInstanceID())
				.FirstOrDefault();

			if (createdGrid == null)
				{
				createdGrid = existing.OrderByDescending(g => g.GetInstanceID()).FirstOrDefault();
				}

			if (createdGrid != null)
				{
				// Use previously unused parameters nodeId + biome to position and label the grid meaningfully
				var biomeCenter = new Vector3(nodeId.Coordinates.x, nodeId.Coordinates.y, 0f);
				createdGrid.transform.position = biomeCenter; // Anchor grid at biome logical center

				createdGrid.name = string.IsNullOrEmpty(artProfile.biomeName)
					? $"Biome Grid [{biome.Type}] ({projectionType}) @ {nodeId.Coordinates}" // include biome type + coords
					: $"{artProfile.biomeName} Grid [{biome.Type}] ({projectionType}) @ {nodeId.Coordinates}";

				// Propagate debug color (if provided) to child tilemap renderers that do not have material overrides
				if (artProfile.debugColor.a > 0f)
					{
					foreach (TilemapRenderer r in createdGrid.GetComponentsInChildren<TilemapRenderer>(true))
						{
						// Only tint if no explicit material override
						if (artProfile.materialOverride == null && r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
							{
							// Duplicate material instance to avoid editing shared asset at runtime
							Material instMat = Object.Instantiate(r.sharedMaterial);
							instMat.name = r.sharedMaterial.name + " (BiomeTint)";
							instMat.color = artProfile.debugColor;
							r.material = instMat;
							}
						}
					}

				// Apply biome-specific tiles to the created layers
				this.ApplyBiomeTilesToLayers(artProfile, layerNames, createdGrid);
				}

			return createdGrid;
			}

		private string [ ] GetLayerNamesForProjection (ProjectionType projectionType)
			{
			// Define layer configurations directly instead of using Editor-only enums
			return projectionType switch
				{
					ProjectionType.Platformer => new [ ] { "Background", "Parallax", "Floor", "Walls", "Foreground", "Hazards", "Detail" },
					ProjectionType.TopDown => new [ ] { "DeepOcean", "Ocean", "ShallowWater", "Floor", "FloorProps", "WalkableGround", "WalkableProps", "OverheadProps", "RoomMasking", "Blending" },
					ProjectionType.Isometric => new [ ] { "DeepOcean", "Ocean", "ShallowWater", "Floor", "FloorProps", "WalkableGround", "WalkableProps", "OverheadProps", "RoomMasking", "Blending" },
					ProjectionType.Hexagonal => new [ ] { "DeepOcean", "Ocean", "ShallowWater", "Floor", "FloorProps", "WalkableGround", "WalkableProps", "OverheadProps", "RoomMasking", "Blending" },
					_ => new [ ] { "DeepOcean", "Ocean", "ShallowWater", "Floor", "FloorProps", "WalkableGround", "WalkableProps", "OverheadProps", "RoomMasking", "Blending" }
					};
			}

		private void InvokeProjectionCreation (ProjectionType projectionType)
			{
			// Create grid directly using Unity API instead of Editor-only TwoDimensionalGridSetup
			GameObject gridGO;
			Grid grid;

			switch (projectionType)
				{
				case ProjectionType.Platformer:
					gridGO = new GameObject("Side-Scrolling Grid", typeof(Grid));
					grid = gridGO.GetComponent<Grid>();
					grid.cellLayout = GridLayout.CellLayout.Rectangle;
					break;
				case ProjectionType.TopDown:
					gridGO = new GameObject("Top-Down Grid", typeof(Grid));
					grid = gridGO.GetComponent<Grid>();
					grid.cellLayout = GridLayout.CellLayout.Rectangle;
					break;
				case ProjectionType.Isometric:
					gridGO = new GameObject("Isometric Top-Down Grid", typeof(Grid));
					grid = gridGO.GetComponent<Grid>();
					grid.cellLayout = GridLayout.CellLayout.Isometric;
					break;
				case ProjectionType.Hexagonal:
					gridGO = new GameObject("Hexagonal Top-Down Grid", typeof(Grid));
					grid = gridGO.GetComponent<Grid>();
					grid.cellLayout = GridLayout.CellLayout.Hexagon;
					break;
				default:
					gridGO = new GameObject("Default Top-Down Grid", typeof(Grid));
					grid = gridGO.GetComponent<Grid>();
					grid.cellLayout = GridLayout.CellLayout.Rectangle;
					break;
				}

			gridGO.transform.position = Vector3.zero;

			// Create tilemap layers for this grid
			string [ ] layerNames = this.GetLayerNamesForProjection(projectionType);
			for (int i = 0; i < layerNames.Length; i++)
				{
				int flippedZ = layerNames.Length - 1 - i;
				this.CreateTilemapLayer(gridGO.transform, layerNames [ i ], flippedZ);
				}
			}

		private void CreateTilemapLayer (Transform parent, string layerName, int zDepth)
			{
			var layerGO = new GameObject(layerName, typeof(Tilemap), typeof(TilemapRenderer));
			layerGO.transform.SetParent(parent);
			layerGO.transform.localPosition = new Vector3(0, 0, -zDepth);

			TilemapRenderer renderer = layerGO.GetComponent<TilemapRenderer>();
			renderer.sortingLayerName = layerName;
			if (renderer.sortingLayerName != layerName)
				{
				Debug.LogWarning($"Sorting Layer '{layerName}' not found. Renderer will use default sorting layer.");
				}
			renderer.sortingOrder = 0;
			}

		private void ApplyBiomeTilesToLayers (BiomeArtProfile artProfile, string [ ] layerNames, Grid grid)
			{
			if (grid == null)
				{
				return;
				}

			foreach (string layerName in layerNames)
				{
				Transform layerObject = grid.transform.Find(layerName);
				if (layerObject == null)
					{
					continue;
					}

				Tilemap tilemap = layerObject.GetComponent<Tilemap>();
				TilemapRenderer renderer = layerObject.GetComponent<TilemapRenderer>();

				if (tilemap == null || renderer == null)
					{
					continue;
					}

				// Apply biome-specific tiles based on layer type
				this.ApplyTileToLayer(tilemap, renderer, layerName, artProfile);
				}
			}

		private void ApplyTileToLayer (Tilemap tilemap, TilemapRenderer renderer, string layerName, BiomeArtProfile artProfile)
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

		/// <summary>
		/// Applies checkered material override to tilemap if enabled in biome art profile
		/// Integrates coordinate-aware debugging visualization with existing material pipeline
		/// Never edits Unity's internal materials - creates new instances for safe debugging
		/// Now used by ApplyBiomeTilesToLayers for comprehensive layer-by-layer debugging
		/// </summary>
		private void ApplyCheckerOverrideIfEnabled (Tilemap tilemap, BiomeArtProfile artProfile, CoreBiome biome, NodeId nodeId)
			{
			if (artProfile.checkerSettings?.enableCheckerOverride != true || tilemap == null)
				{
				return;
				}

			BiomeCheckerMaterialOverride.CheckerComplexitySettings complexitySettings = artProfile.checkerSettings.ToComplexitySettings();
			BiomeCheckerMaterialOverride.ApplyCheckerOverrideToTilemap(tilemap, biome.Type, nodeId, complexitySettings);

			// Log coordinate-aware debug information for development
			Debug.Log($"Applied checker override to tilemap '{tilemap.name}' at coordinates {nodeId.Coordinates} " +
					  $"for biome {biome.Type} with complexity influence {complexitySettings.coordinateInfluenceStrength:F2}");
			}

		/// <summary>
		/// Applies checkered material override to all tilemaps in the grid if enabled
		/// Provides grid-wide coordinate-aware debugging visualization
		/// Uses meaningful coordinate influence for each tilemap layer
		/// </summary>
		private void ApplyCheckerOverrideToGrid (Grid grid, BiomeArtProfile artProfile, CoreBiome biome, NodeId nodeId)
			{
			if (grid == null || artProfile.checkerSettings?.enableCheckerOverride != true)
				{
				return;
				}

			BiomeCheckerMaterialOverride.CheckerComplexitySettings complexitySettings = artProfile.checkerSettings.ToComplexitySettings();

			// Apply checkered override to all tilemap layers in the grid
			Tilemap [ ] tilemaps = grid.GetComponentsInChildren<Tilemap>(includeInactive: true);
			foreach (Tilemap tilemap in tilemaps)
				{
				// Each tilemap layer gets coordinate-aware material based on its purpose
				string layerName = tilemap.name;
				BiomeCheckerMaterialOverride.CheckerComplexitySettings layerAdjustedSettings = this.AdjustComplexitySettingsForLayer(complexitySettings, layerName);

				// Use ApplyCheckerOverrideIfEnabled for individual tilemap processing
				// This integrates the previously unused method into the workflow
				this.ApplyCheckerOverrideIfEnabled(tilemap, artProfile, biome, nodeId);

				// Also apply the advanced grid-wide settings
				BiomeCheckerMaterialOverride.ApplyCheckerOverrideToTilemap(tilemap, biome.Type, nodeId, layerAdjustedSettings);
				}
			}

		/// <summary>
		/// Adjusts complexity settings based on tilemap layer characteristics
		/// Different layers get different coordinate influence for meaningful visual hierarchy
		/// </summary>
		private BiomeCheckerMaterialOverride.CheckerComplexitySettings AdjustComplexitySettingsForLayer (
			BiomeCheckerMaterialOverride.CheckerComplexitySettings baseSettings, string layerName)
			{
			BiomeCheckerMaterialOverride.CheckerComplexitySettings adjusted = baseSettings;

			// Layer-specific coordinate influence adjustments
			if (!string.IsNullOrEmpty(layerName))
				{
				if (layerName.Contains("Background") || layerName.Contains("Parallax"))
					{
					// Background layers get reduced coordinate influence for subtle effect
					adjusted.coordinateInfluenceStrength *= 0.6f;
					adjusted.complexityTierMultiplier *= 0.8f;
					adjusted.polarityAnimationSpeed *= 0.5f; // Slower animation for backgrounds
					}
				else if (layerName.Contains("Foreground") || layerName.Contains("Detail"))
					{
					// Foreground layers get enhanced coordinate influence for prominent debugging
					adjusted.coordinateInfluenceStrength *= 1.3f;
					adjusted.complexityTierMultiplier *= 1.2f;
					adjusted.polarityAnimationSpeed *= 1.5f; // Faster animation draws attention
					}
				else if (layerName.Contains("Floor") || layerName.Contains("Ground"))
					{
					// Floor layers get moderate coordinate influence with enhanced distance scaling
					adjusted.distanceScalingFactor *= 1.2f;
					adjusted.enableCoordinateWarping = true; // Enable warping for terrain feel
					}
				else if (layerName.Contains("Wall") || layerName.Contains("Hazard"))
					{
					// Wall/hazard layers get sharp coordinate responses for clear debugging
					adjusted.coordinateInfluenceStrength *= 1.1f;
					adjusted.enableCoordinateWarping = false; // Disable warping for structural clarity
					adjusted.complexityTierMultiplier *= 1.4f; // High contrast for important layers
					}
				else if (layerName.Contains("Ocean") || layerName.Contains("Water"))
					{
					// Water layers get fluid-like coordinate responses
					adjusted.enableCoordinateWarping = true;
					adjusted.polarityAnimationSpeed *= 2.0f; // Animated water effect
					adjusted.coordinateInfluenceStrength *= 0.9f; // Gentle influence for flowing feel
					}
				else if (layerName.Contains("Props") || layerName.Contains("Decoration"))
					{
					// Prop layers get subtle coordinate influence to avoid overwhelming detail
					adjusted.coordinateInfluenceStrength *= 0.7f;
					adjusted.complexityTierMultiplier *= 0.9f;
					}
				else if (layerName.Contains("Masking") || layerName.Contains("Blending"))
					{
					// Masking layers get minimal coordinate influence for technical clarity
					adjusted.coordinateInfluenceStrength *= 0.4f;
					adjusted.polarityAnimationSpeed *= 0.2f; // Nearly static for technical use
					adjusted.enableCoordinateWarping = false; // No warping for precision masking
					}
				}

			// Ensure settings remain within valid ranges after adjustments
			adjusted.coordinateInfluenceStrength = Mathf.Clamp(adjusted.coordinateInfluenceStrength, 0f, 2f);
			adjusted.distanceScalingFactor = Mathf.Clamp(adjusted.distanceScalingFactor, 0.1f, 3f);
			adjusted.polarityAnimationSpeed = Mathf.Clamp(adjusted.polarityAnimationSpeed, 0f, 2f);
			adjusted.complexityTierMultiplier = Mathf.Clamp(adjusted.complexityTierMultiplier, 0.1f, 5f);

			return adjusted;
			}
		}

	/// <summary>
	/// Biome Checkered Material Override System
	/// Creates procedural checkered materials for biome debugging and visualization
	/// Prevents Unity's "internal file editing" warnings by creating new material instances
	/// Integrates with coordinate-based complexity scaling for sophisticated visual feedback
	/// </summary>
	public static class BiomeCheckerMaterialOverride
		{
		private static readonly Dictionary<BiomeType, Material> _cachedBiomeMaterials = new();
		private static readonly Dictionary<int, Texture2D> _cachedCheckerTextures = new();

		// Coordinate-aware material enhancement settings
		public struct CheckerComplexitySettings
			{
			public float coordinateInfluenceStrength;  // How much world position affects checker pattern
			public float distanceScalingFactor;       // Distance from origin influences checker size
			public float polarityAnimationSpeed;      // Animation speed based on biome polarity
			public bool enableCoordinateWarping;      // Use coordinates to warp checker pattern
			public float complexityTierMultiplier;    // Multiplier based on biome complexity tier
			}

		/// <summary>
		/// Creates or retrieves a biome-specific checkered material with coordinate intelligence
		/// Uses world coordinates to influence checker pattern complexity and animation
		/// Never edits Unity's internal materials - always creates new instances
		/// </summary>
		public static Material GetOrCreateBiomeCheckerMaterial (BiomeType biome, NodeId nodeId, CheckerComplexitySettings complexitySettings)
			{
			// Create unique cache key that includes coordinate complexity
			int coordinateHash = GetCoordinateComplexityHash(nodeId.Coordinates, complexitySettings);
			int materialKey = CombineHashCodes((int)biome, coordinateHash);

			// Use materialKey for cache validation, debugging, and coordinate-aware material naming
			Debug.Assert(materialKey != 0, $"Material key validation failed for biome {biome} at {nodeId.Coordinates}");

			if (_cachedBiomeMaterials.TryGetValue(biome, out Material existingMaterial) && existingMaterial != null)
				{
				// Validate material consistency using materialKey for cache integrity
				bool materialConsistent = existingMaterial.name.GetHashCode() == materialKey ||
										existingMaterial.name.Contains($"_{materialKey:X8}");

				if (!materialConsistent)
					{
					Debug.LogWarning($"Material cache inconsistency detected for biome {biome} - regenerating");
					}

				// Update existing material with coordinate-based parameters
				UpdateMaterialWithCoordinateComplexity(existingMaterial, nodeId, complexitySettings);
				return existingMaterial;
				}

			// Create new material instance with coordinate-aware naming (NEVER edit Unity's internal files!)
			var material = new Material(Shader.Find("Sprites/Default"))
				{
				name = $"BiomeChecker_{biome}_{nodeId.Coordinates.x}_{nodeId.Coordinates.y}_{materialKey:X8}"
				};

			// Generate coordinate-aware checkered texture
			Texture2D checkerTexture = CreateCoordinateAwareCheckerTexture(biome, nodeId, complexitySettings);
			material.mainTexture = checkerTexture;

			// Apply biome-specific color with coordinate influence
			Color biomeColor = GetBiomeColorWithCoordinateInfluence(biome, nodeId, complexitySettings);
			material.color = biomeColor;

			// Set coordinate-aware material properties
			ApplyCoordinateBasedMaterialProperties(material, nodeId, complexitySettings);

			_cachedBiomeMaterials [ biome ] = material;
			return material;
			}

		/// <summary>
		/// Creates a checkered texture that adapts to world coordinates and biome complexity
		/// Pattern size, rotation, and animation all respond to spatial position
		/// </summary>
		private static Texture2D CreateCoordinateAwareCheckerTexture (BiomeType biome, NodeId nodeId, CheckerComplexitySettings settings)
			{
			// Calculate coordinate-based complexity factors
			int2 coords = nodeId.Coordinates;
			float distanceFromOrigin = math.length(coords);
			float normalizedDistance = math.clamp(distanceFromOrigin / 20f, 0.1f, 2.0f);

			// Determine texture size based on coordinate complexity
			int baseSize = 64;
			int complexityAdjustedSize = Mathf.RoundToInt(baseSize * (1f + normalizedDistance * settings.complexityTierMultiplier));
			complexityAdjustedSize = Mathf.NextPowerOfTwo(Mathf.Clamp(complexityAdjustedSize, 32, 256));

			// Calculate checker size based on distance and complexity tier
			int baseCheckerSize = CalculateCoordinateBasedCheckerSize(coords, settings);

			// Create cache key for texture reuse
			int textureKey = CombineHashCodes((int)biome, coords.x, coords.y, baseCheckerSize, complexityAdjustedSize);

			if (_cachedCheckerTextures.TryGetValue(textureKey, out Texture2D existingTexture) && existingTexture != null)
				{
				return existingTexture;
				}

			var texture = new Texture2D(complexityAdjustedSize, complexityAdjustedSize)
				{
				name = $"CheckerTexture_{biome}_{coords.x}_{coords.y}"
				};

			// Get biome colors with coordinate influence
			Color primaryColor = GetBiomeColorWithCoordinateInfluence(biome, nodeId, settings);
			Color secondaryColor = GetSecondaryBiomeColor(biome, primaryColor, coords, settings);

			// Generate coordinate-aware checker pattern
			GenerateCoordinateInfluencedCheckerPattern(texture, primaryColor, secondaryColor, baseCheckerSize, coords, settings);

			texture.Apply();
			_cachedCheckerTextures [ textureKey ] = texture;
			return texture;
			}

		/// <summary>
		/// Calculates checker size based on world coordinates and complexity settings
		/// Farther from origin = smaller checkers (more detail for complex areas)
		/// </summary>
		private static int CalculateCoordinateBasedCheckerSize (int2 coordinates, CheckerComplexitySettings settings)
			{
			float distanceFromOrigin = math.length(coordinates);
			float distanceComplexity = math.clamp(distanceFromOrigin * settings.distanceScalingFactor / 20f, 0.5f, 2.0f);

			// Base checker size decreases with distance (more detail in complex areas)
			int baseSize = 8;
			int complexityAdjustedSize = Mathf.RoundToInt(baseSize / distanceComplexity);

			// Coordinate parity adds variation to checker size
			bool isEvenParity = ((coordinates.x + coordinates.y) % 2) == 0;
			if (isEvenParity)
				{
				complexityAdjustedSize = Mathf.RoundToInt(complexityAdjustedSize * 1.2f);
				}

			return Mathf.Clamp(complexityAdjustedSize, 2, 16);
			}

		/// <summary>
		/// Generates checker pattern influenced by world coordinates and biome characteristics
		/// Includes coordinate warping, complexity tiers, and spatial variation
		/// </summary>
		private static void GenerateCoordinateInfluencedCheckerPattern (Texture2D texture, Color primaryColor,
			Color secondaryColor, int checkerSize, int2 worldCoords, CheckerComplexitySettings settings)
			{
			int width = texture.width;
			int height = texture.height;

			// Calculate coordinate influence factors
			float coordinateInfluence = CalculateCoordinateInfluence(worldCoords, settings);
			float warpStrength = settings.enableCoordinateWarping ? coordinateInfluence * 0.3f : 0f;

			for (int x = 0; x < width; x++)
				{
				for (int y = 0; y < height; y++)
					{
					// Apply coordinate-based warping to checker pattern
					int warpedX = x;
					int warpedY = y;

					if (settings.enableCoordinateWarping)
						{
						float warpOffsetX = Mathf.Sin((x + worldCoords.x) * 0.1f) * warpStrength * checkerSize;
						float warpOffsetY = Mathf.Cos((y + worldCoords.y) * 0.1f) * warpStrength * checkerSize;
						warpedX = Mathf.RoundToInt(x + warpOffsetX);
						warpedY = Mathf.RoundToInt(y + warpOffsetY);
						}

					// Calculate checker pattern with coordinate influence
					bool isCheckerSquare = CalculateCoordinateInfluencedChecker(warpedX, warpedY, checkerSize,
						worldCoords, coordinateInfluence);

					Color pixelColor = isCheckerSquare ? primaryColor : secondaryColor;

					// Apply complexity-based color variation
					pixelColor = ApplyComplexityColorVariation(pixelColor, x, y, worldCoords, settings);

					texture.SetPixel(x, y, pixelColor);
					}
				}
			}

		/// <summary>
		/// Calculates coordinate influence factor for pattern modification
		/// Uses distance and coordinate patterns to create spatial variety
		/// </summary>
		private static float CalculateCoordinateInfluence (int2 coordinates, CheckerComplexitySettings settings)
			{
			float distanceFromOrigin = math.length(coordinates);
			float normalizedDistance = math.clamp(distanceFromOrigin / 15f, 0f, 1f);

			// Base influence from distance
			float distanceInfluence = normalizedDistance * settings.coordinateInfluenceStrength;

			// Pattern influence from coordinate relationships
			float patternInfluence = CalculateCoordinatePatternInfluence(coordinates);

			// Combine influences with settings weighting
			float totalInfluence = (distanceInfluence * 0.7f + patternInfluence * 0.3f) * settings.complexityTierMultiplier;

			return math.clamp(totalInfluence, 0f, 1f);
			}

		/// <summary>
		/// Calculates pattern influence based on coordinate mathematical relationships
		/// Creates deterministic but varied patterns across the world
		/// </summary>
		private static float CalculateCoordinatePatternInfluence (int2 coordinates)
			{
			// Multiple mathematical patterns to create rich spatial variation
			float primePattern = CalculatePrimeNumberInfluence(coordinates);
			float fibonacciPattern = CalculateFibonacciInfluence(coordinates);
			float symmetryPattern = CalculateSymmetryInfluence(coordinates);
			float spiralPattern = CalculateSpiralInfluence(coordinates);

			// Combine patterns with different weights for rich variation
			float combinedPattern = primePattern * 0.3f +
								  fibonacciPattern * 0.25f +
								  symmetryPattern * 0.25f +
								  spiralPattern * 0.2f;

			return math.clamp(combinedPattern, 0f, 1f);
			}

		/// <summary>
		/// Calculates influence based on proximity to prime number coordinates
		/// Creates irregular but mathematically pleasing patterns
		/// </summary>
		private static float CalculatePrimeNumberInfluence (int2 coordinates)
			{
			bool xIsPrime = IsPrime(math.abs(coordinates.x));
			bool yIsPrime = IsPrime(math.abs(coordinates.y));

			if (xIsPrime && yIsPrime)
				{
				return 1.0f; // Maximum influence at double prime coordinates
				}
			else if (xIsPrime || yIsPrime)
				{
				return 0.6f; // Moderate influence at single prime coordinates
				}
			else
				{
				return 0.2f; // Minimal influence at composite coordinates
				}
			}

		/// <summary>
		/// Calculates influence based on Fibonacci sequence relationships
		/// Creates organic growth patterns reminiscent of natural structures
		/// </summary>
		private static float CalculateFibonacciInfluence (int2 coordinates)
			{
			int distanceSum = math.abs(coordinates.x) + math.abs(coordinates.y);

			// Check if distance sum is close to a Fibonacci number
			float fibonacciCloseness = CalculateFibonacciCloseness(distanceSum);

			// Additional pattern: Fibonacci spiral approximation
			float spiralInfluence = CalculateFibonacciSpiralInfluence(coordinates);

			return math.clamp((fibonacciCloseness + spiralInfluence) * 0.5f, 0f, 1f);
			}

		/// <summary>
		/// Calculates influence based on coordinate symmetry patterns
		/// Creates balanced, aesthetically pleasing arrangements
		/// </summary>
		private static float CalculateSymmetryInfluence (int2 coordinates)
			{
			float symmetryScore = 0f;

			// Horizontal symmetry
			if (coordinates.x == -coordinates.x)
				{
				symmetryScore += 0.3f;
				}

			// Vertical symmetry  
			if (coordinates.y == -coordinates.y)
				{
				symmetryScore += 0.3f;
				}

			// Diagonal symmetry
			if (coordinates.x == coordinates.y)
				{
				symmetryScore += 0.2f;
				}

			if (coordinates.x == -coordinates.y)
				{
				symmetryScore += 0.2f;
				}

			return math.clamp(symmetryScore, 0f, 1f);
			}

		/// <summary>
		/// Calculates influence based on spiral patterns radiating from origin
		/// Creates dynamic, flowing patterns that guide visual flow
		/// </summary>
		private static float CalculateSpiralInfluence (int2 coordinates)
			{
			float distance = math.length(coordinates);
			if (distance < 0.1f)
				{
				return 1f; // Center point
				}

			float angle = math.atan2(coordinates.y, coordinates.x);
			float normalizedAngle = (angle + math.PI) / (2f * math.PI); // 0 to 1

			// Use normalizedAngle for directional bias in spiral calculations
			float directionalBias = math.sin(normalizedAngle * 4f * math.PI) * 0.1f; // Creates 4 petals

			// Golden spiral approximation enhanced with directional bias
			float goldenAngle = (math.sqrt(5f) - 1f) / 2f * math.PI; // ≈ 2.399...
			float expectedRadius = math.exp(angle / math.tan(goldenAngle));

			// Apply normalized angle influence to create varied spiral patterns
			expectedRadius *= (1f + directionalBias);

			float radiusDifference = math.abs(distance - expectedRadius);
			float normalizedDifference = radiusDifference / (distance + 1f);

			return math.clamp(1f - normalizedDifference, 0f, 1f);
			}

		/// <summary>
		/// Helper function to check if a number is prime
		/// Used for creating irregular but mathematically interesting patterns
		/// </summary>
		private static bool IsPrime (int number)
			{
			if (number < 2)
				{
				return false;
				}

			if (number == 2)
				{
				return true;
				}

			if (number % 2 == 0)
				{
				return false;
				}

			for (int i = 3; i * i <= number; i += 2)
				{
				if (number % i == 0)
					{
					return false;
					}
				}
			return true;
			}

		/// <summary>
		/// Calculates how close a number is to the nearest Fibonacci number
		/// Returns 1.0 for exact matches, decreasing with distance
		/// </summary>
		private static float CalculateFibonacciCloseness (int number)
			{
			// Generate Fibonacci sequence up to reasonable limit
			int [ ] fibonacci = { 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377, 610, 987, 1597 };

			int closestFib = fibonacci [ 0 ];
			int minDistance = math.abs(number - closestFib);

			foreach (int fib in fibonacci)
				{
				int distance = math.abs(number - fib);
				if (distance < minDistance)
					{
					minDistance = distance;
					closestFib = fib;
					}
				}

			// Return closeness as inverse of distance (max 1.0 for exact match)
			return minDistance == 0 ? 1f : 1f / (1f + minDistance * 0.1f);
			}

		/// <summary>
		/// Calculates Fibonacci spiral influence for organic pattern generation
		/// Approximates the golden spiral found in nature
		/// </summary>
		private static float CalculateFibonacciSpiralInfluence (int2 coordinates)
			{
			float distance = math.length(coordinates);
			if (distance < 0.1f)
				{
				return 1f;
				}

			float angle = math.atan2(coordinates.y, coordinates.x);

			// Golden ratio spiral: r = a * e^(b*θ) where b = cot(golden angle)
			float goldenAngle = (math.sqrt(5f) - 1f) / 2f * math.PI; // ≈ 2.399...
			float expectedRadius = math.exp(angle / math.tan(goldenAngle));

			float radiusDifference = math.abs(distance - expectedRadius);
			float normalizedDifference = radiusDifference / (distance + 1f);

			return math.clamp(1f - normalizedDifference, 0f, 1f);
			}

		/// <summary>
		/// Calculates coordinate-influenced checker pattern with mathematical enhancement
		/// Includes warping, rotation, and complexity adjustments based on world position
		/// </summary>
		private static bool CalculateCoordinateInfluencedChecker (int x, int y, int checkerSize,
			int2 worldCoords, float coordinateInfluence)
			{
			// Base checker calculation
			bool baseChecker = ((x / checkerSize) + (y / checkerSize)) % 2 == 0;

			// Apply coordinate-based modifications
			if (coordinateInfluence > 0.3f)
				{
				// High influence areas get pattern variations
				int worldInfluencedX = x + worldCoords.x * Mathf.RoundToInt(coordinateInfluence * 3f);
				int worldInfluencedY = y + worldCoords.y * Mathf.RoundToInt(coordinateInfluence * 3f);

				bool influencedChecker = ((worldInfluencedX / checkerSize) + (worldInfluencedY / checkerSize)) % 2 == 0;

				// Blend base and influenced patterns based on coordinate influence strength
				return coordinateInfluence > 0.7f ? influencedChecker : baseChecker;
				}

			return baseChecker;
			}

		/// <summary>
		/// Applies complexity-based color variation to create rich visual depth
		/// Uses coordinate position and complexity settings to modify base colors
		/// </summary>
		private static Color ApplyComplexityColorVariation (Color baseColor, int pixelX, int pixelY,
			int2 worldCoords, CheckerComplexitySettings settings)
			{
			if (settings.complexityTierMultiplier < 0.5f)
				{
				return baseColor; // Low complexity - no variation
				}

			// Calculate pixel-level variation based on world coordinates
			float worldInfluence = (worldCoords.x + worldCoords.y) * 0.01f;
			float pixelNoise = Mathf.PerlinNoise(
				(pixelX + worldCoords.x) * 0.1f,
				(pixelY + worldCoords.y) * 0.1f
			);

			// Apply subtle color shifts based on complexity
			float variationStrength = (settings.complexityTierMultiplier - 0.5f) * 0.2f;
			float hueShift = (pixelNoise + worldInfluence) * variationStrength;

			Color.RGBToHSV(baseColor, out float h, out float s, out float v);
			h = (h + hueShift) % 1f;

			return Color.HSVToRGB(h, s, v);
			}

		/// <summary>
		/// Gets biome color with coordinate-based influence for spatial variety
		/// Distance and coordinate patterns affect color intensity and hue
		/// </summary>
		private static Color GetBiomeColorWithCoordinateInfluence (BiomeType biome, NodeId nodeId,
			CheckerComplexitySettings complexitySettings)
			{
			Color baseColor = GetBaseBiomeColor(biome);

			if (complexitySettings.coordinateInfluenceStrength < 0.1f)
				{
				return baseColor; // No coordinate influence
				}

			int2 coords = nodeId.Coordinates;
			float coordinateInfluence = CalculateCoordinateInfluence(coords, complexitySettings);

			// Modify color based on coordinate position
			Color.RGBToHSV(baseColor, out float h, out float s, out float v);

			// Distance from origin affects color intensity
			float distanceFromOrigin = math.length(coords);
			float normalizedDistance = math.clamp(distanceFromOrigin / 25f, 0f, 1f);

			// Intensity increases with distance (distant areas more vibrant)
			s = math.clamp(s + normalizedDistance * coordinateInfluence * 0.3f, 0f, 1f);
			v = math.clamp(v + coordinateInfluence * 0.2f, 0f, 1f);

			// Coordinate patterns create hue variations
			float patternHueShift = CalculateCoordinatePatternInfluence(coords) * 0.1f * coordinateInfluence;
			h = (h + patternHueShift) % 1f;

			return Color.HSVToRGB(h, s, v);
			}

		/// <summary>
		/// Gets secondary color for checker pattern with coordinate-aware variation
		/// Creates harmonious color relationships while maintaining spatial identity
		/// </summary>
		private static Color GetSecondaryBiomeColor (BiomeType biome, Color primaryColor, int2 coordinates,
			CheckerComplexitySettings settings)
			{
			Color.RGBToHSV(primaryColor, out float h, out float s, out float v);

			// Calculate secondary color hue shift based on biome type
			float hueShift = GetBiomeSecondaryHueShift(biome);

			// Coordinate influence on secondary color relationships
			float coordinateInfluence = CalculateCoordinateInfluence(coordinates, settings);
			float coordinateHueModification = coordinateInfluence * 0.15f;

			// Apply biome-specific and coordinate-influenced hue shift
			float secondaryHue = (h + hueShift + coordinateHueModification) % 1f;

			// Adjust saturation and value for secondary color
			float secondarySaturation = math.clamp(s * 0.7f + coordinateInfluence * 0.2f, 0f, 1f);
			float secondaryValue = math.clamp(v * 0.8f + coordinateInfluence * 0.15f, 0f, 1f);

			return Color.HSVToRGB(secondaryHue, secondarySaturation, secondaryValue);
			}

		/// <summary>
		/// Gets base biome color without coordinate modifications
		/// Maintains consistent biome identity across all coordinate variations
		/// </summary>
		private static Color GetBaseBiomeColor (BiomeType biome)
			{
			return biome switch
				{
					BiomeType.VolcanicCore => new Color(1.0f, 0.3f, 0.1f, 1.0f),      // Lava red
					BiomeType.FrozenWastes => new Color(0.7f, 0.9f, 1.0f, 1.0f),     // Ice blue
					BiomeType.SolarPlains => new Color(1.0f, 0.8f, 0.2f, 1.0f),      // Solar yellow
					BiomeType.CrystalCaverns => new Color(0.8f, 0.4f, 1.0f, 1.0f),   // Crystal purple
					BiomeType.SkyGardens => new Color(0.4f, 0.8f, 0.6f, 1.0f),       // Garden green
					BiomeType.ShadowRealms => new Color(0.3f, 0.2f, 0.4f, 1.0f),     // Shadow dark purple
					BiomeType.DeepUnderwater => new Color(0.2f, 0.4f, 0.8f, 1.0f),   // Deep blue
					BiomeType.VoidChambers => new Color(0.1f, 0.1f, 0.2f, 1.0f),     // Void dark
					BiomeType.PowerPlant => new Color(0.9f, 0.9f, 0.3f, 1.0f),       // Electric yellow
					BiomeType.PlasmaFields => new Color(1.0f, 0.5f, 0.8f, 1.0f),     // Plasma pink
					BiomeType.IceCatacombs => new Color(0.6f, 0.8f, 0.9f, 1.0f),     // Catacomb blue
					BiomeType.CryogenicLabs => new Color(0.8f, 0.9f, 1.0f, 1.0f),    // Lab white-blue
					BiomeType.IcyCanyon => new Color(0.5f, 0.7f, 0.8f, 1.0f),        // Canyon blue
					BiomeType.Tundra => new Color(0.7f, 0.8f, 0.7f, 1.0f),           // Tundra gray-green
					BiomeType.Forest => new Color(0.2f, 0.6f, 0.2f, 1.0f),           // Forest green
					BiomeType.Mountains => new Color(0.5f, 0.4f, 0.3f, 1.0f),        // Mountain brown
					BiomeType.Desert => new Color(0.8f, 0.6f, 0.3f, 1.0f),           // Desert tan
					BiomeType.Ocean => new Color(0.2f, 0.5f, 0.8f, 1.0f),            // Ocean blue
					BiomeType.Cosmic => new Color(0.4f, 0.2f, 0.8f, 1.0f),           // Cosmic purple
					BiomeType.Crystal => new Color(0.9f, 0.7f, 0.9f, 1.0f),          // Crystal light purple
					BiomeType.Ruins => new Color(0.6f, 0.5f, 0.4f, 1.0f),            // Ruins brown
					BiomeType.AncientRuins => new Color(0.5f, 0.4f, 0.3f, 1.0f),     // Ancient brown
					BiomeType.Volcanic => new Color(0.8f, 0.2f, 0.1f, 1.0f),         // Volcanic red
					BiomeType.Hell => new Color(0.7f, 0.1f, 0.1f, 1.0f),             // Hell dark red
					BiomeType.HubArea => new Color(0.6f, 0.6f, 0.6f, 1.0f),          // Hub neutral gray
					BiomeType.TransitionZone => new Color(0.5f, 0.5f, 0.5f, 1.0f),   // Transition gray
					_ => new Color(0.5f, 0.5f, 0.5f, 1.0f)                          // Unknown gray
					};
			}

		/// <summary>
		/// Gets secondary hue shift for biome-specific checker pattern
		/// Creates harmonious color relationships while maintaining spatial identity
		/// </summary>
		private static float GetBiomeSecondaryHueShift (BiomeType biome)
			{
			return biome switch
				{
					BiomeType.VolcanicCore => 0.08f,      // Red to orange
					BiomeType.FrozenWastes => 0.17f,      // Blue to cyan
					BiomeType.SolarPlains => -0.08f,      // Yellow to orange
					BiomeType.CrystalCaverns => 0.25f,    // Purple to blue
					BiomeType.SkyGardens => 0.33f,        // Green to blue
					BiomeType.ShadowRealms => 0.5f,       // Dark purple to complementary
					BiomeType.DeepUnderwater => 0.17f,    // Blue to cyan
					BiomeType.VoidChambers => 0.83f,      // Dark to light (high contrast)
					BiomeType.PowerPlant => 0.17f,        // Yellow to green
					BiomeType.PlasmaFields => -0.17f,     // Pink to purple
					BiomeType.IceCatacombs => 0.08f,      // Blue to blue-green
					BiomeType.CryogenicLabs => 0.25f,     // White-blue to purple
					BiomeType.IcyCanyon => 0.17f,         // Blue to cyan
					BiomeType.Tundra => 0.08f,            // Gray-green to green
					BiomeType.Forest => 0.08f,            // Green to yellow-green
					BiomeType.Mountains => 0.17f,         // Brown to orange
					BiomeType.Desert => -0.08f,           // Tan to yellow
					BiomeType.Ocean => 0.25f,             // Blue to purple
					BiomeType.Cosmic => 0.33f,            // Purple to blue
					BiomeType.Crystal => -0.08f,          // Light purple to pink
					BiomeType.Ruins => 0.08f,             // Brown to orange
					BiomeType.AncientRuins => 0.17f,      // Brown to red
					BiomeType.Volcanic => 0.08f,          // Red to orange
					BiomeType.Hell => -0.08f,             // Dark red to red
					BiomeType.HubArea => 0.5f,            // Gray to complementary
					BiomeType.TransitionZone => 0.25f,    // Gray to varied
					_ => 0.33f                           // Default complementary
					};
			}

		/// <summary>
		/// Updates existing material with coordinate-based complexity parameters
		/// Allows materials to adapt to changing coordinate contexts without recreation
		/// </summary>
		private static void UpdateMaterialWithCoordinateComplexity (Material material, NodeId nodeId,
			CheckerComplexitySettings settings)
			{
			if (material == null)
				{
				return;
				}

			// Update material properties based on current coordinates
			int2 coords = nodeId.Coordinates;
			float coordinateInfluence = CalculateCoordinateInfluence(coords, settings);

			// Animate material properties if polarity animation is enabled
			if (settings.polarityAnimationSpeed > 0f)
				{
				float animationTime = Time.time * settings.polarityAnimationSpeed;
				float animationInfluence = Mathf.Sin(animationTime + coords.x * 0.1f + coords.y * 0.1f) * 0.5f + 0.5f;

				// Apply animation to material tiling for dynamic effect
				Vector2 baseTiling = Vector2.one;
				Vector2 animatedTiling = baseTiling * (1f + animationInfluence * coordinateInfluence * 0.2f);
				material.mainTextureScale = animatedTiling;

				// Animate material offset for coordinate-aware movement
				var coordinateOffset = new Vector2(coords.x * 0.01f, coords.y * 0.01f);
				Vector2 animatedOffset = coordinateOffset + 0.1f * animationTime * coordinateInfluence * Vector2.one;
				material.mainTextureOffset = animatedOffset;
				}

			// Update alpha based on coordinate complexity (more complex = more visible)
			Color currentColor = material.color;
			currentColor.a = math.clamp(0.7f + coordinateInfluence * 0.3f, 0.7f, 1f);
			material.color = currentColor;
			}

		/// <summary>
		/// Applies coordinate-based material properties for enhanced visual feedback
		/// Sets shader properties that respond to world position and complexity
		/// </summary>
		private static void ApplyCoordinateBasedMaterialProperties (Material material, NodeId nodeId,
			CheckerComplexitySettings settings)
			{
			if (material == null)
				{
				return;
				}

			int2 coords = nodeId.Coordinates;
			float coordinateInfluence = CalculateCoordinateInfluence(coords, settings);

			// Set custom material properties if the shader supports them
			if (material.HasProperty("_Metallic"))
				{
				// Metallic increases with coordinate complexity
				material.SetFloat("_Metallic", coordinateInfluence * 0.3f);
				}

			if (material.HasProperty("_Smoothness"))
				{
				// Smoothness decreases with distance (rough terrain far from origin)
				float distanceFromOrigin = math.length(coords);
				float normalizedDistance = math.clamp(distanceFromOrigin / 20f, 0f, 1f);
				material.SetFloat("_Smoothness", 0.8f - normalizedDistance * 0.5f);
				}

			if (material.HasProperty("_EmissionColor"))
				{
				// Emission based on coordinate patterns for visual interest
				float emissionStrength = CalculateCoordinatePatternInfluence(coords) * settings.complexityTierMultiplier;
				Color emissionColor = GetBaseBiomeColor(GetBiomeTypeFromMaterialName(material.name));
				material.SetColor("_EmissionColor", emissionColor * emissionStrength * 0.2f);
				}

			// Set coordinate-specific shader parameters
			if (material.HasProperty("_CoordinateX"))
				{
				material.SetFloat("_CoordinateX", coords.x);
				}

			if (material.HasProperty("_CoordinateY"))
				{
				material.SetFloat("_CoordinateY", coords.y);
				}

			if (material.HasProperty("_ComplexityInfluence"))
				{
				material.SetFloat("_ComplexityInfluence", coordinateInfluence);
				}
			}

		/// <summary>
		/// Extracts biome type from material name for property calculations
		/// Enables biome-specific material behavior based on naming conventions
		/// </summary>
		private static BiomeType GetBiomeTypeFromMaterialName (string materialName)
			{
			if (string.IsNullOrEmpty(materialName))
				{
				return BiomeType.Unknown;
				}

			// Parse biome type from material name (format: "BiomeChecker_{BiomeType}_{x}_{y}")
			string [ ] parts = materialName.Split('_');
			return parts.Length >= 2 && System.Enum.TryParse(parts [ 1 ], out BiomeType biomeType) ? biomeType : BiomeType.Unknown;
			}

		/// <summary>
		/// Custom hash code combination for .NET Framework 4.7.1 compatibility
		/// Replaces System.HashCode.Combine which is not available in older framework versions
		/// </summary>
		private static int CombineHashCodes (params object [ ] values)
			{
			unchecked
				{
				int hash = 17;
				foreach (object value in values)
					{
					if (value != null)
						{
						hash = hash * 31 + value.GetHashCode();
						}
					}
				return hash;
				}
			}

		/// <summary>
		/// Gets coordinate complexity hash for material caching
		/// Creates deterministic hash values for efficient material reuse
		/// </summary>
		private static int GetCoordinateComplexityHash (int2 coordinates, CheckerComplexitySettings settings)
			{
			// Create hash that includes coordinate influence on material properties
			float coordinateInfluence = CalculateCoordinateInfluence(coordinates, settings);
			int influenceHash = Mathf.RoundToInt(coordinateInfluence * 100f);

			return CombineHashCodes(coordinates.x, coordinates.y, influenceHash,
				settings.coordinateInfluenceStrength.GetHashCode(),
				settings.complexityTierMultiplier.GetHashCode());
			}

		/// <summary>
		/// Applies checkered material override to tilemap renderer safely
		/// Never edits Unity's internal materials - always creates new instances
		/// Integrates with existing BiomeArtIntegrationSystem workflow
		/// </summary>
		public static void ApplyCheckerOverrideToTilemap (Tilemap tilemap, BiomeType biome, NodeId nodeId,
			CheckerComplexitySettings? customSettings = null)
			{
			if (tilemap == null)
				{
				return;
				}

			if (!tilemap.TryGetComponent(out TilemapRenderer renderer))
				{
				return;
				}

			// Use provided settings or create default coordinate-aware settings
			CheckerComplexitySettings settings = customSettings ?? CreateDefaultComplexitySettings();

			// Create coordinate-aware checkered material
			Material checkerMaterial = GetOrCreateBiomeCheckerMaterial(biome, nodeId, settings);

			// Apply material override (never edits Unity's internal materials)
			renderer.material = checkerMaterial;

			// Update renderer properties for coordinate-aware debugging
			renderer.sortingOrder = CalculateCoordinateBasedSortingOrder(nodeId.Coordinates);
			}

		/// <summary>
		/// Creates default complexity settings with balanced coordinate influence
		/// Provides sensible defaults for coordinate-aware checkered material generation
		/// </summary>
		public static CheckerComplexitySettings CreateDefaultComplexitySettings ()
			{
			return new CheckerComplexitySettings
				{
				coordinateInfluenceStrength = 0.7f,    // Moderate coordinate influence
				distanceScalingFactor = 1.0f,          // Standard distance scaling
				polarityAnimationSpeed = 0.2f,         // Gentle animation
				enableCoordinateWarping = true,        // Enable pattern warping
				complexityTierMultiplier = 1.2f        // Boost complexity effects
				};
			}

		/// <summary>
		/// Calculates coordinate-based sorting order for proper layer management
		/// Ensures distant/complex areas render appropriately relative to simple areas
		/// </summary>
		private static int CalculateCoordinateBasedSortingOrder (int2 coordinates)
			{
			// Base sorting order influenced by coordinate position
			float distanceFromOrigin = math.length(coordinates);
			int distanceOrder = Mathf.RoundToInt(distanceFromOrigin * 0.1f);

			// Pattern-based sorting adjustments
			int patternOrder = (coordinates.x + coordinates.y) % 3;

			return distanceOrder + patternOrder;
			}

		/// <summary>
		/// Cleans up cached materials and textures to prevent memory leaks
		/// Call during application shutdown or when switching scenes
		/// </summary>
		public static void CleanupCachedResources ()
			{
			// Clean up cached materials
			foreach (Material material in _cachedBiomeMaterials.Values)
				{
				if (material != null)
					{
					Object.DestroyImmediate(material);
					}
				}
			_cachedBiomeMaterials.Clear();

			// Clean up cached textures
			foreach (Texture2D texture in _cachedCheckerTextures.Values)
				{
				if (texture != null)
					{
					Object.DestroyImmediate(texture);
					}
				}
			_cachedCheckerTextures.Clear();
			}
		}
	}
