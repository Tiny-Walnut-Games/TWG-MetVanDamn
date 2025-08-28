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

        // Weather/Environmental biomes
        Storm = 25,          // Storm and wind-based biome
        MetalRich = 26,      // Metal-rich mining biome
        Insulating = 27,     // Energy-insulating/dampening biome

        // Neutral/Mixed biomes
        HubArea = 28,
        TransitionZone = 29
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
    /// Assigns biome type and polarity field for world coherence
    /// Essential for WFC biome generation with gradient rules
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

        public Biome(BiomeType type, Polarity primaryPolarity, float polarityStrength = 1.0f, 
                    Polarity secondaryPolarity = Polarity.None, float difficultyModifier = 1.0f)
        {
            Type = type;
            PrimaryPolarity = primaryPolarity;
            SecondaryPolarity = secondaryPolarity;
            PolarityStrength = math.clamp(polarityStrength, 0.0f, 1.0f);
            DifficultyModifier = math.max(0.1f, difficultyModifier);
        }

        /// <summary>
        /// Check if this biome is compatible with a given polarity
        /// </summary>
        public readonly bool IsCompatibleWith(Polarity requiredPolarity)
        {
            if (requiredPolarity == Polarity.Any || requiredPolarity == Polarity.None)
                return true;
                
            return (PrimaryPolarity & requiredPolarity) != 0 || 
                   (SecondaryPolarity & requiredPolarity) != 0;
        }

        public override readonly string ToString()
        {
            return $"Biome({Type}, {PrimaryPolarity}, Strength:{PolarityStrength:F2})";
        }
    }
}
