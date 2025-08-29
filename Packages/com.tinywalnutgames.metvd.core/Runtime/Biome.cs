using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Core
{
    /// <summary>
    /// Polarity flags for dual-polarity gates and biome coherence
    /// Uses bitmask system for flexible single and dual pole combinations
    /// </summary>
    [System.Flags]
    public enum Polarity : byte
    {
        None = 0,
        Sun = 1 << 0,    // Light polarity
        Moon = 1 << 1,   // Dark polarity  
        Heat = 1 << 2,   // Fire/energy polarity
        Cold = 1 << 3,   // Ice/crystal polarity
        Earth = 1 << 4,  // Ground/nature polarity
        Wind = 1 << 5,   // Air/storm polarity
        Life = 1 << 6,   // Bio/organic polarity
        Tech = 1 << 7,   // Machine/digital polarity
        
        // Common dual-polarity combinations
        SunMoon = Sun | Moon,
        HeatCold = Heat | Cold,
        EarthWind = Earth | Wind,
        LifeTech = Life | Tech,
        
        // Special markers  
        Any = Sun | Moon | Heat | Cold | Earth | Wind | Life | Tech   // Matches any polarity (OR of all poles)
    }

    /// <summary>
    /// Biome type enumeration for world generation
    /// </summary>
    public enum BiomeType : byte
    {
        Unknown = 0,
        
        // Light-aligned biomes
        SolarPlains = 1,
        CrystalCaverns = 2,
        SkyGardens = 3,
        
        // Dark-aligned biomes
        ShadowRealms = 4,
        DeepUnderwater = 5,
        VoidChambers = 6,
        
        // Hazard/Energy biomes
        VolcanicCore = 7, // Primary volcanic core biome (keep original numeric value for existing references)
        PowerPlant = 8,
        PlasmaFields = 9,
        
        // Ice/Crystal biomes
        FrozenWastes = 10,
        IceCatacombs = 11,
        CryogenicLabs = 12,
        IcyCanyon = 13,
        Tundra = 14,
        
        // Earth/Nature biomes
        Forest = 15,
        Mountains = 16,
        Desert = 17,
        
        // Water biomes
        Ocean = 18,
        
        // Space biomes
        Cosmic = 19,
        
        // Crystal biomes
        Crystal = 20,
        
        // Ruins/Ancient biomes
        Ruins = 21,
        AncientRuins = 22,
        
        // Volcanic/Fire biomes  
        Volcanic = 23,
        Hell = 24, // Changed from Magma to Hell for clarity

        // Neutral/Mixed biomes
        HubArea = 25,
        TransitionZone = 26
    }

    /// <summary>
    /// District type enumeration for specialized district generation
    /// </summary>
    public enum DistrictType : byte
    {
        Standard = 0,      // Standard district with balanced generation
        Hub = 1,           // Central hub district with high connectivity
        Maze = 2,          // Complex maze-like district with high density
        Linear = 3,        // Linear district with corridor-like layout
        Circular = 4,      // Circular district with radial layout
        Specialized = 5    // Special-purpose district with unique rules
    }

    /// <summary>
    /// Biome complexity tier for coordinate-aware material generation and spatial optimization
    /// Integrates with BiomeCheckerMaterialOverride system for sophisticated visual feedback
    /// </summary>
    public enum BiomeComplexityTier : byte
    {
        Simple = 0,        // Basic biomes with minimal environmental features
        Moderate = 1,      // Standard biomes with typical feature density
        Complex = 2,       // Rich biomes with dense feature sets and interactions
        Extreme = 3,       // Highly complex biomes with maximum feature density
        Transcendent = 4   // Beyond normal complexity - special biomes with unique properties
    }

    /// <summary>
    /// Biome rarity classification for procedural generation balancing
    /// Influences spawn probability and distance scaling from world origin
    /// </summary>
    public enum BiomeRarity : byte
    {
        Common = 0,        // Frequently encountered near world origin
        Uncommon = 1,      // Moderately rare, appears at medium distances
        Rare = 2,          // Seldom seen, typically found at greater distances
        Epic = 3,          // Very rare, distant locations only
        Legendary = 4      // Extremely rare, special conditions required
    }

    /// <summary>
    /// Assigns biome type and polarity field for world coherence
    /// Essential for WFC biome generation with gradient rules
    /// Enhanced with coordinate-aware spatial intelligence for advanced material generation
    /// </summary>
    public struct Biome : IComponentData
    {
        /// <summary>
        /// The type of biome this node represents
        /// </summary>
        public BiomeType Type;
        
        /// <summary>
        /// Primary polarity field for this biome
        /// </summary>
        public Polarity PrimaryPolarity;
        
        /// <summary>
        /// Secondary polarity field (for mixed biomes)
        /// </summary>
        public Polarity SecondaryPolarity;
        
        /// <summary>
        /// Strength of polarity field (0.0 to 1.0)
        /// Used for gradient calculations and adjacency rules
        /// </summary>
        public float PolarityStrength;
        
        /// <summary>
        /// Biome difficulty modifier for progression pacing
        /// </summary>
        public float DifficultyModifier;

        /// <summary>
        /// Complexity tier for coordinate-aware material generation
        /// Influences checker pattern detail, animation speed, and spatial coherence
        /// </summary>
        public BiomeComplexityTier ComplexityTier;

        /// <summary>
        /// Rarity classification affecting spawn probability and distance scaling
        /// </summary>
        public BiomeRarity Rarity;

        /// <summary>
        /// Coordinate influence factor for spatial material behavior
        /// Higher values create more dramatic coordinate-based visual effects
        /// </summary>
        public float CoordinateInfluenceStrength;

        public Biome(BiomeType type, Polarity primaryPolarity, float polarityStrength = 1.0f, 
                    Polarity secondaryPolarity = Polarity.None, float difficultyModifier = 1.0f,
                    BiomeComplexityTier complexityTier = BiomeComplexityTier.Moderate,
                    BiomeRarity rarity = BiomeRarity.Common, float coordinateInfluence = 0.7f)
        {
            Type = type;
            PrimaryPolarity = primaryPolarity;
            SecondaryPolarity = secondaryPolarity;
            PolarityStrength = math.clamp(polarityStrength, 0.0f, 1.0f);
            DifficultyModifier = math.max(0.1f, difficultyModifier);
            ComplexityTier = complexityTier;
            Rarity = rarity;
            CoordinateInfluenceStrength = math.clamp(coordinateInfluence, 0.0f, 2.0f);
        }

        /// <summary>
        /// Check if this biome is compatible with a given polarity
        /// </summary>
        public readonly bool IsCompatibleWith(Polarity requiredPolarity)
        {
            if (requiredPolarity != Polarity.Any && requiredPolarity != Polarity.None)
            {
                return (PrimaryPolarity & requiredPolarity) != 0 ||
                       (SecondaryPolarity & requiredPolarity) != 0;
            }

            return true;
        }

        /// <summary>
        /// Calculate polarity resonance strength between two biomes
        /// Uses mathematical harmony principles for smooth transitions
        /// </summary>
        public readonly float CalculatePolarityResonance(Biome otherBiome)
        {
            // Base resonance from shared polarities
            float primaryResonance = CalculatePolarityOverlap(PrimaryPolarity, otherBiome.PrimaryPolarity);
            float secondaryResonance = CalculatePolarityOverlap(SecondaryPolarity, otherBiome.SecondaryPolarity);
            float crossResonance = CalculatePolarityOverlap(PrimaryPolarity, otherBiome.SecondaryPolarity) +
                                 CalculatePolarityOverlap(SecondaryPolarity, otherBiome.PrimaryPolarity);

            // Weight the resonances - primary connections are strongest
            float totalResonance = (primaryResonance * 0.5f) + (secondaryResonance * 0.3f) + (crossResonance * 0.2f);

            // Apply polarity strength modulation
            float strengthModulation = (PolarityStrength + otherBiome.PolarityStrength) * 0.5f;
            
            return math.clamp(totalResonance * strengthModulation, 0.0f, 1.0f);
        }

        /// <summary>
        /// Get the spatial complexity factor for coordinate-aware systems
        /// Integrates complexity tier, rarity, and coordinate influence for material generation
        /// </summary>
        public readonly float GetSpatialComplexityFactor(int2 worldCoordinates)
        {
            // Base complexity from tier
            float tierComplexity = ((int)ComplexityTier + 1) * 0.2f; // 0.2 to 1.0

            // Rarity influences complexity at distance
            float distanceFromOrigin = math.length(worldCoordinates);
            float rarityInfluence = ((int)Rarity + 1) * 0.15f; // 0.15 to 0.75
            float distanceScaledRarity = rarityInfluence * math.min(distanceFromOrigin / 20.0f, 1.0f);

            // Coordinate pattern influence using mathematical beauty
            float coordinatePattern = CalculateCoordinatePatternFactor(worldCoordinates);
            float patternInfluence = coordinatePattern * CoordinateInfluenceStrength;

            // Combine factors with weighted importance
            float totalComplexity = (tierComplexity * 0.4f) + (distanceScaledRarity * 0.3f) + (patternInfluence * 0.3f);

            return math.clamp(totalComplexity, 0.1f, 2.0f);
        }

        /// <summary>
        /// Calculate whether this biome should use enhanced coordinate warping for materials
        /// Based on complexity tier and biome type characteristics
        /// </summary>
        public readonly bool ShouldUseCoordinateWarping()
        {
            // Transcendent biomes always use warping
            if (ComplexityTier == BiomeComplexityTier.Transcendent)
            {
                return true;
            }

            // Complex biomes usually use warping
            if (ComplexityTier == BiomeComplexityTier.Complex)
            {
                return true;
            }

            // Certain biome types are naturally warp-friendly
            return Type switch
            {
                BiomeType.VoidChambers => true,    // Void warps reality
                BiomeType.PlasmaFields => true,    // Plasma is inherently chaotic
                BiomeType.Cosmic => true,          // Space bends coordinates
                BiomeType.CrystalCaverns => true,  // Crystals refract space
                BiomeType.ShadowRealms => true,    // Shadows distort perception
                _ => ComplexityTier >= BiomeComplexityTier.Moderate
            };
        }

        /// <summary>
        /// Get the base material animation speed for this biome type
        /// Integrates with BiomeCheckerMaterialOverride polarity animation system
        /// </summary>
        public readonly float GetBaseMaterialAnimationSpeed()
        {
            // Base speed from polarity - dual polarities create more dynamic animations
            float polaritySpeed = (PrimaryPolarity != Polarity.None ? 0.5f : 0.0f) +
                                (SecondaryPolarity != Polarity.None ? 0.3f : 0.0f);

            // Biome-specific speed modifiers
            float biomeSpeedModifier = Type switch
            {
                BiomeType.PlasmaFields => 2.0f,     // Fastest animation
                BiomeType.VoidChambers => 1.8f,     // Very fast, chaotic
                BiomeType.PowerPlant => 1.5f,       // Fast, energetic
                BiomeType.Ocean => 1.2f,            // Flowing motion
                BiomeType.SkyGardens => 1.1f,       // Gentle movement
                BiomeType.FrozenWastes => 0.3f,     // Slow, frozen
                BiomeType.IceCatacombs => 0.4f,     // Slightly more movement
                BiomeType.CryogenicLabs => 0.2f,    // Almost static
                BiomeType.AncientRuins => 0.6f,     // Ancient, slow
                _ => 1.0f                           // Standard speed
            };

            // Apply complexity tier scaling
            float complexityScaling = 1.0f + ((int)ComplexityTier * 0.2f);

            return polaritySpeed * biomeSpeedModifier * complexityScaling * PolarityStrength;
        }

        /// <summary>
        /// Calculate mathematical pattern influence based on coordinate relationships
        /// Used for sophisticated spatial material behavior
        /// </summary>
        private readonly float CalculateCoordinatePatternFactor(int2 coordinates)
        {
            // Prime number influence - creates irregular but pleasing patterns
            float primeInfluence = (IsPrime(math.abs(coordinates.x)) ? 0.3f : 0.0f) +
                                 (IsPrime(math.abs(coordinates.y)) ? 0.3f : 0.0f);

            // Golden ratio spiral influence - natural mathematical beauty
            float spiralInfluence = CalculateGoldenSpiralInfluence(coordinates);

            // Symmetry influence - balanced, harmonious patterns
            float symmetryInfluence = CalculateSymmetryInfluence(coordinates);

            // Fibonacci influence - organic growth patterns
            float fibonacciInfluence = CalculateFibonacciInfluence(coordinates);

            // Combine influences with natural weighting
            return (primeInfluence * 0.3f) + (spiralInfluence * 0.25f) + 
                   (symmetryInfluence * 0.25f) + (fibonacciInfluence * 0.2f);
        }

        /// <summary>
        /// Calculate polarity overlap for resonance calculations
        /// </summary>
        private readonly float CalculatePolarityOverlap(Polarity polarity1, Polarity polarity2)
        {
            if (polarity1 == Polarity.None || polarity2 == Polarity.None)
            {
                return 0.0f;
            }

            int overlap = math.countbits((uint)(polarity1 & polarity2));
            int total = math.countbits((uint)(polarity1 | polarity2));
            
            return total > 0 ? (float)overlap / total : 0.0f;
        }

        /// <summary>
        /// Mathematical beauty: Golden spiral influence calculation
        /// </summary>
        private readonly float CalculateGoldenSpiralInfluence(int2 coordinates)
        {
            float distance = math.length(coordinates);
            if (distance < 0.1f)
            {
                return 1.0f; // Origin point
            }

            float angle = math.atan2(coordinates.y, coordinates.x);
            float goldenAngle = (math.sqrt(5.0f) - 1.0f) / 2.0f * math.PI;
            float expectedRadius = math.exp(angle / math.tan(goldenAngle));
            
            float radiusDifference = math.abs(distance - expectedRadius);
            return math.clamp(1.0f - (radiusDifference / (distance + 1.0f)), 0.0f, 1.0f);
        }

        /// <summary>
        /// Calculate symmetry influence for balanced patterns
        /// </summary>
        private readonly float CalculateSymmetryInfluence(int2 coordinates)
        {
            float symmetryScore = 0.0f;
            
            // Horizontal symmetry
            if (coordinates.x == -coordinates.x)
            {
                symmetryScore += 0.25f;
            }

            // Vertical symmetry
            if (coordinates.y == -coordinates.y)
            {
                symmetryScore += 0.25f;
            }

            // Diagonal symmetries
            if (coordinates.x == coordinates.y)
            {
                symmetryScore += 0.25f;
            }

            if (coordinates.x == -coordinates.y)
            {
                symmetryScore += 0.25f;
            }

            return symmetryScore;
        }

        /// <summary>
        /// Calculate Fibonacci sequence influence for organic patterns
        /// </summary>
        private readonly float CalculateFibonacciInfluence(int2 coordinates)
        {
            int distanceSum = math.abs(coordinates.x) + math.abs(coordinates.y);
            
            // Check proximity to Fibonacci numbers
            int[] fibonacci = { 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377, 610 };
            
            int closestDistance = int.MaxValue;
            foreach (int fib in fibonacci)
            {
                int distance = math.abs(distanceSum - fib);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                }
            }
            
            return closestDistance == 0 ? 1.0f : 1.0f / (1.0f + closestDistance * 0.1f);
        }

        /// <summary>
        /// Check if a number is prime (mathematical beauty helper)
        /// </summary>
        private readonly bool IsPrime(int number)
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

        public override readonly string ToString()
        {
            return $"Biome({Type}, {PrimaryPolarity}, Strength:{PolarityStrength:F2}, Tier:{ComplexityTier}, Rarity:{Rarity})";
        }
    }

    /// <summary>
    /// Extension methods for biome-related calculations and utilities
    /// Provides additional coordinate-aware functionality for systems integration
    /// </summary>
    public static class BiomeExtensions
    {
        /// <summary>
        /// Calculate the effective spawn probability for a biome at given coordinates
        /// Integrates rarity with distance scaling for natural biome distribution
        /// </summary>
        public static float CalculateSpawnProbability(this Biome biome, int2 worldCoordinates, float baseSpawnRate = 1.0f)
        {
            float distanceFromOrigin = math.length(worldCoordinates);
            float normalizedDistance = distanceFromOrigin / 25.0f; // Normalize to reasonable world scale
            
            // Rarity affects probability - common biomes appear near origin, rare ones far away
            float rarityModifier = (int)biome.Rarity switch
            {
                0 => math.max(0.1f, 1.0f - normalizedDistance * 0.5f),     // Common: high near origin
                1 => 0.8f + math.sin(normalizedDistance * math.PI) * 0.3f, // Uncommon: peak at medium distance
                2 => math.clamp(normalizedDistance - 0.3f, 0.0f, 1.0f),    // Rare: increases with distance
                3 => math.max(0.0f, normalizedDistance - 0.6f) * 1.5f,     // Epic: far distances only
                4 => normalizedDistance > 0.8f ? 0.1f : 0.0f,              // Legendary: extreme distances
                _ => 1.0f
            };
            
            return baseSpawnRate * rarityModifier * biome.PolarityStrength;
        }

        /// <summary>
        /// Get the material complexity settings for BiomeCheckerMaterialOverride integration
        /// Returns settings optimized for this biome's characteristics
        /// </summary>
        public static (float coordinateInfluence, float distanceScaling, float animationSpeed, bool enableWarping, float complexityMultiplier) 
            GetMaterialComplexitySettings(this Biome biome, int2 worldCoordinates)
        {
            float coordinateInfluence = biome.CoordinateInfluenceStrength;
            float distanceScaling = 1.0f + ((int)biome.Rarity * 0.2f); // Rarer biomes scale more with distance
            float animationSpeed = biome.GetBaseMaterialAnimationSpeed();
            bool enableWarping = biome.ShouldUseCoordinateWarping();
            float complexityMultiplier = biome.GetSpatialComplexityFactor(worldCoordinates);
            
            return (coordinateInfluence, distanceScaling, animationSpeed, enableWarping, complexityMultiplier);
        }

        /// <summary>
        /// Calculate the transition strength between two biomes for smooth boundaries
        /// Used by biome field systems for gradient calculations
        /// </summary>
        public static float CalculateTransitionStrength(this Biome fromBiome, Biome toBiome, float distance, float maxTransitionDistance = 5.0f)
        {
            // Base transition from polarity resonance
            float polarityTransition = fromBiome.CalculatePolarityResonance(toBiome);
            
            // Distance falloff - closer biomes transition more smoothly
            float distanceFactor = 1.0f - math.clamp(distance / maxTransitionDistance, 0.0f, 1.0f);
            
            // Complexity difference affects transition smoothness
            int complexityDifference = math.abs((int)fromBiome.ComplexityTier - (int)toBiome.ComplexityTier);
            float complexityPenalty = 1.0f - (complexityDifference * 0.15f); // Penalize large complexity jumps
            
            return polarityTransition * distanceFactor * math.max(0.1f, complexityPenalty);
        }
    }
}
