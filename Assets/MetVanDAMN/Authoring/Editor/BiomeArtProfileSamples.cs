using UnityEngine;
using TinyWalnutGames.MetVD.Authoring;

namespace TinyWalnutGames.MetVD.Authoring.Editor
{
    /// <summary>
    /// Utility class for creating sample BiomeArtProfile assets with advanced prop placement
    /// Demonstrates B+/A-level prop placement configurations for different biome types
    /// </summary>
    public static class BiomeArtProfileSamples
    {
        // Helper curve builders (Unity does not expose AnimationCurve.EaseIn / EaseOut factory methods natively)
        // These approximate ease shapes using two keyframes with adjusted tangents.
        private static AnimationCurve EaseOutCurve(float x0, float y0, float x1, float y1)
        {
            var k0 = new Keyframe(x0, y0, 0f, 0f);          // Flat tangent at start (quick rise after)
            var k1 = new Keyframe(x1, y1, 0f, 0f);          // Flat tangent at end to slow into value
            // Adjust tangents to create an ease-out effect (fast start -> slow end)
            k0.outTangent = (y1 - y0) * 2f;                // Push quickly upward
            k1.inTangent = 0f;                             // Flatten into end
            return new AnimationCurve(k0, k1);
        }

        private static AnimationCurve EaseInCurve(float x0, float y0, float x1, float y1)
        {
            var k0 = new Keyframe(x0, y0, 0f, 0f);          // Flat at start (slow start)
            var k1 = new Keyframe(x1, y1, 0f, 0f);          // Flat at end optional
            // Adjust tangents to create an ease-in effect (slow start -> fast end)
            k0.outTangent = 0f;                            // Stay flat initially
            k1.inTangent = (y1 - y0) * 2f;                 // Accelerate into end
            return new AnimationCurve(k0, k1);
        }

        /// <summary>
        /// Creates a sample forest biome with clustered vegetation placement
        /// Demonstrates B+ level clustering and natural distribution
        /// </summary>
        public static BiomeArtProfile CreateForestBiomeSample()
        {
            BiomeArtProfile profile = ScriptableObject.CreateInstance<BiomeArtProfile>();
            profile.biomeName = "Dense Forest";
            profile.debugColor = new Color(0.2f, 0.6f, 0.2f, 1f);

            // Configure advanced prop placement
            profile.propSettings = new PropPlacementSettings
            {
                // Strategy: Clustered for natural forest groupings
                strategy = PropPlacementStrategy.Clustered,
                
                // Density: High base density with center-focused distribution
                baseDensity = 0.25f,
                densityMultiplier = 1.4f,
                densityCurve = AnimationCurve.EaseInOut(0f, 0.3f, 1f, 1f),
                
                // Clustering: Natural tree groupings
                clustering = new ClusteringSettings
                {
                    clusterSize = 8,
                    clusterRadius = 5f,
                    clusterDensity = 0.75f,
                    clusterSeparation = 15f
                },
                
                // Avoidance: Stay away from hazards and maintain spacing
                avoidance = new AvoidanceSettings
                {
                    avoidLayers = { "Hazards", "Water", "Cliffs" },
                    avoidanceRadius = 2f,
                    avoidTransitions = true,
                    transitionAvoidanceRadius = 3f,
                    avoidOvercrowding = true,
                    minimumPropDistance = 1.5f
                },
                
                // Variation: Natural size and rotation variety
                variation = new VariationSettings
                {
                    minScale = 0.8f,
                    maxScale = 1.3f,
                    randomRotation = true,
                    maxRotationAngle = 360f,
                    positionJitter = 0.4f
                },
                
                // Layers and performance
                allowedPropLayers = { "Ground", "Vegetation", "Decoration" },
                maxPropsPerBiome = 150,
                useSpatialOptimization = true
            };

            return profile;
        }

        /// <summary>
        /// Creates a sample desert biome with sparse oasis-style placement
        /// Demonstrates A-level radial distribution and terrain awareness
        /// </summary>
        public static BiomeArtProfile CreateDesertBiomeSample()
        {
            BiomeArtProfile profile = ScriptableObject.CreateInstance<BiomeArtProfile>();
            profile.biomeName = "Arid Desert";
            profile.debugColor = new Color(0.8f, 0.7f, 0.3f, 1f);

            profile.propSettings = new PropPlacementSettings
            {
                // Strategy: Radial for oasis-like distribution
                strategy = PropPlacementStrategy.Radial,
                
                // Density: Low with center concentration
                baseDensity = 0.08f,
                densityMultiplier = 1.2f,
                densityCurve = EaseOutCurve(0f, 0.1f, 1f, 0.9f), // replaced unsupported AnimationCurve.EaseOut
                
                // Clustering: Small, tight oasis clusters
                clustering = new ClusteringSettings
                {
                    clusterSize = 3,
                    clusterRadius = 2.5f,
                    clusterDensity = 0.9f,
                    clusterSeparation = 25f
                },
                
                // Avoidance: Large separation for sparse feel
                avoidance = new AvoidanceSettings
                {
                    avoidLayers = { "Hazards", "Dunes" },
                    avoidanceRadius = 3f,
                    avoidTransitions = true,
                    transitionAvoidanceRadius = 4f,
                    avoidOvercrowding = true,
                    minimumPropDistance = 3f
                },
                
                // Variation: Weather-beaten look
                variation = new VariationSettings
                {
                    minScale = 0.6f,
                    maxScale = 1.1f,
                    randomRotation = true,
                    maxRotationAngle = 180f,
                    positionJitter = 0.6f
                },
                
                allowedPropLayers = { "Ground", "Sparse" },
                maxPropsPerBiome = 50,
                useSpatialOptimization = true
            };

            return profile;
        }

        /// <summary>
        /// Creates a sample mountain biome with terrain-aware placement
        /// Demonstrates A-level terrain analysis and elevation-based distribution
        /// </summary>
        public static BiomeArtProfile CreateMountainBiomeSample()
        {
            BiomeArtProfile profile = ScriptableObject.CreateInstance<BiomeArtProfile>();
            profile.biomeName = "Rocky Mountains";
            profile.debugColor = new Color(0.5f, 0.4f, 0.3f, 1f);

            profile.propSettings = new PropPlacementSettings
            {
                // Strategy: Terrain-aware for realistic mountain placement
                strategy = PropPlacementStrategy.Terrain,
                
                // Density: Moderate with elevation-based variation
                baseDensity = 0.15f,
                densityMultiplier = 1.1f,
                densityCurve = AnimationCurve.Linear(0f, 0.8f, 1f, 0.4f), // More sparse at center (peaks)
                
                // Clustering: Small rocky outcrops
                clustering = new ClusteringSettings
                {
                    clusterSize = 4,
                    clusterRadius = 3f,
                    clusterDensity = 0.6f,
                    clusterSeparation = 12f
                },
                
                // Avoidance: Respect cliffs and steep terrain
                avoidance = new AvoidanceSettings
                {
                    avoidLayers = { "Cliffs", "Water", "Ice" },
                    avoidanceRadius = 4f,
                    avoidTransitions = true,
                    transitionAvoidanceRadius = 2f,
                    avoidOvercrowding = true,
                    minimumPropDistance = 2f
                },
                
                // Variation: Dramatic scale differences
                variation = new VariationSettings
                {
                    minScale = 0.7f,
                    maxScale = 1.6f,
                    randomRotation = true,
                    maxRotationAngle = 360f,
                    positionJitter = 0.8f
                },
                
                allowedPropLayers = { "Ground", "Rocks", "Vegetation" },
                maxPropsPerBiome = 80,
                useSpatialOptimization = true
            };

            return profile;
        }

        /// <summary>
        /// Creates a sample coastal biome with linear shoreline placement
        /// Demonstrates B+ level linear distribution and water awareness
        /// </summary>
        public static BiomeArtProfile CreateCoastalBiomeSample()
        {
            BiomeArtProfile profile = ScriptableObject.CreateInstance<BiomeArtProfile>();
            profile.biomeName = "Coastal Shores";
            profile.debugColor = new Color(0.4f, 0.6f, 0.8f, 1f);

            profile.propSettings = new PropPlacementSettings
            {
                // Strategy: Linear for shoreline features
                strategy = PropPlacementStrategy.Linear,
                
                // Density: Edge-focused distribution
                baseDensity = 0.2f,
                densityMultiplier = 1.3f,
                densityCurve = EaseInCurve(0f, 1f, 1f, 0.2f), // replaced unsupported AnimationCurve.EaseIn
                
                // Clustering: Small tidal pools and driftwood
                clustering = new ClusteringSettings
                {
                    clusterSize = 5,
                    clusterRadius = 2f,
                    clusterDensity = 0.8f,
                    clusterSeparation = 8f
                },
                
                // Avoidance: Water-aware placement
                avoidance = new AvoidanceSettings
                {
                    avoidLayers = { "DeepWater", "Hazards" },
                    avoidanceRadius = 1.5f,
                    avoidTransitions = false, // We want edge placement
                    avoidOvercrowding = true,
                    minimumPropDistance = 1.2f
                },
                
                // Variation: Smooth beach stones and varied driftwood
                variation = new VariationSettings
                {
                    minScale = 0.9f,
                    maxScale = 1.2f,
                    randomRotation = true,
                    maxRotationAngle = 180f,
                    positionJitter = 0.3f
                },
                
                allowedPropLayers = { "Shoreline", "Beach", "Vegetation" },
                maxPropsPerBiome = 120,
                useSpatialOptimization = true
            };

            return profile;
        }

        /// <summary>
        /// Creates a sample urban biome with sparse strategic placement
        /// Demonstrates A-level sparse distribution for important landmarks
        /// </summary>
        public static BiomeArtProfile CreateUrbanBiomeSample()
        {
            BiomeArtProfile profile = ScriptableObject.CreateInstance<BiomeArtProfile>();
            profile.biomeName = "Urban District";
            profile.debugColor = new Color(0.6f, 0.6f, 0.6f, 1f);

            profile.propSettings = new PropPlacementSettings
            {
                // Strategy: Sparse for important landmarks and features
                strategy = PropPlacementStrategy.Sparse,
                
                // Density: Very low but meaningful
                baseDensity = 0.05f,
                densityMultiplier = 1f,
                densityCurve = AnimationCurve.Constant(0f, 1f, 1f), // Uniform distribution
                
                // Clustering: Building complexes
                clustering = new ClusteringSettings
                {
                    clusterSize = 2,
                    clusterRadius = 8f,
                    clusterDensity = 0.5f,
                    clusterSeparation = 30f
                },
                
                // Avoidance: Strict spacing for urban planning
                avoidance = new AvoidanceSettings
                {
                    avoidLayers = { "Roads", "Hazards" },
                    avoidanceRadius = 5f,
                    avoidTransitions = true,
                    transitionAvoidanceRadius = 3f,
                    avoidOvercrowding = true,
                    minimumPropDistance = 8f // Large spacing for buildings
                },
                
                // Variation: Architectural variety
                variation = new VariationSettings
                {
                    minScale = 0.9f,
                    maxScale = 1.4f,
                    randomRotation = true,
                    maxRotationAngle = 90f, // Aligned to grid
                    positionJitter = 0.2f
                },
                
                allowedPropLayers = { "Buildings", "Infrastructure", "Decoration" },
                maxPropsPerBiome = 30,
                useSpatialOptimization = false // Small scale doesn't need optimization
            };

            return profile;
        }
    }
}
