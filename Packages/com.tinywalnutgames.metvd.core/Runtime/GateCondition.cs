using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Core
{
    /// <summary>
    /// Player abilities that can unlock gated content
    /// Expandable system for Metroidvania progression
    /// </summary>
    [System.Flags]
    public enum Ability : uint
    {
        None = 0,
        
        // Movement abilities
        Jump = 1 << 0,
        DoubleJump = 1 << 1,
        WallJump = 1 << 2,
        Dash = 1 << 3,
        GlideSpeed = 1 << 4,
        
        // Arc-based movement abilities
        ArcJump = 1 << 5,      // Precise parabolic jump control
        ChargedJump = 1 << 6,  // Variable jump height/distance
        TeleportArc = 1 << 7,  // Short-range teleportation with arc visualization
        
        // Environmental abilities  
        Swim = 1 << 8,
        Climb = 1 << 9,
        HeatResistance = 1 << 10,
        ColdResistance = 1 << 11,
        PressureResistance = 1 << 12,
        
        // Tool abilities
        Bomb = 1 << 13,
        Grapple = 1 << 14,     // Grappling hook with arc trajectory
        Drill = 1 << 15,
        Scan = 1 << 16,
        Hack = 1 << 17,
        
        // Polarity abilities (match Polarity enum)
        SunAccess = 1 << 18,
        MoonAccess = 1 << 19,
        HeatAccess = 1 << 20,
        ColdAccess = 1 << 21,
        EarthAccess = 1 << 22,
        WindAccess = 1 << 23,
        LifeAccess = 1 << 24,
        TechAccess = 1 << 25,
        
        // Cross-scale abilities (spacecraft/vehicle)
        WarpDrive = 1 << 23,
        HeatShielding = 1 << 24,
        ColdShielding = 1 << 25,
        OrbitalManeuver = 1 << 26,
        TerrainTraversal = 1 << 27,
        Flight = 1 << 28,
        
        // Meta progression
        MapUnlock = 1 << 29,
        SaveUnlock = 1 << 30,
        FastTravel = 0x80000000,
        
        // Special combined abilities
        AllMovement = Jump | DoubleJump | WallJump | Dash | GlideSpeed | ArcJump | ChargedJump | TeleportArc,
        AllArcMovement = ArcJump | ChargedJump | TeleportArc,
        AllEnvironmental = Swim | Climb | HeatResistance | ColdResistance | PressureResistance,
        AllTools = Bomb | Grapple | Drill | Scan | Hack,
        AllPolarity = SunAccess | MoonAccess | HeatAccess | ColdAccess | EarthAccess | WindAccess | LifeAccess | TechAccess,
        AllSpacecraft = WarpDrive | HeatShielding | ColdShielding | OrbitalManeuver,
        AllVehicle = TerrainTraversal | Flight | HeatShielding | ColdShielding,
        Everything = 0xFFFFFFFF
    }

    /// <summary>
    /// Gate difficulty levels for skill-based progression
    /// </summary>
    public enum GateSoftness : byte
    {
        Hard = 0,        // Impossible without required abilities/polarity
        VeryDifficult = 1, // Requires exceptional skill
        Difficult = 2,   // Requires above-average skill
        Moderate = 3,    // Achievable with practice
        Easy = 4,        // Minor skill gate
        Trivial = 5      // Barely a gate at all
    }

    /// <summary>
    /// Polarity mask + ability + softness tuple for gate logic
    /// Core component for Metroidvania progression gating system
    /// </summary>
    public struct GateCondition : IComponentData
    {
        /// <summary>
        /// Required polarity to pass this gate
        /// </summary>
        public Polarity RequiredPolarity;
        
        /// <summary>
        /// Required abilities to pass this gate
        /// </summary>
        public Ability RequiredAbilities;
        
        /// <summary>
        /// Softness of this gate (skill-based bypass possibility)
        /// </summary>
        public GateSoftness Softness;
        
        /// <summary>
        /// Minimum player skill level to attempt bypass (0.0 to 1.0)
        /// </summary>
        public float MinimumSkillLevel;
        
        /// <summary>
        /// Whether this gate is currently active
        /// </summary>
        public bool IsActive;
        
        /// <summary>
        /// Whether this gate has been unlocked permanently
        /// </summary>
        public bool IsUnlocked;
        
        /// <summary>
        /// Gate description for UI/debugging
        /// </summary>
        public FixedString64Bytes Description;

        public GateCondition(Polarity requiredPolarity = Polarity.None, 
                            Ability requiredAbilities = Ability.None,
                            GateSoftness softness = GateSoftness.Hard,
                            float minimumSkillLevel = 0.0f,
                            FixedString64Bytes description = default)
        {
            RequiredPolarity = requiredPolarity;
            RequiredAbilities = requiredAbilities;
            Softness = softness;
            MinimumSkillLevel = math.clamp(minimumSkillLevel, 0.0f, 1.0f);
            IsActive = true;
            IsUnlocked = false;
            Description = description;
        }

        /// <summary>
        /// Check if the gate can be passed with given polarity and abilities
        /// </summary>
        public readonly bool CanPass(Polarity availablePolarity, Ability availableAbilities, float playerSkillLevel = 0.0f)
        {
            if (!IsActive || IsUnlocked) 
                return true;

            // Polarity rule: Hard gates require ALL required bits; softer gates require ANY bit.
            bool polarityMatch = RequiredPolarity == Polarity.None || RequiredPolarity == Polarity.Any ||
                                  (Softness == GateSoftness.Hard
                                    ? (availablePolarity & RequiredPolarity) == RequiredPolarity
                                    : (availablePolarity & RequiredPolarity) != 0);

            bool abilityMatch = RequiredAbilities == Ability.None ||
                                 (availableAbilities & RequiredAbilities) == RequiredAbilities;

            // If hard requirements are met, gate can be passed
            if (polarityMatch && abilityMatch)
                return true;

            // Check for skill-based bypass
            if (Softness != GateSoftness.Hard && playerSkillLevel >= MinimumSkillLevel)
            {
                // Skill bypass is possible but becomes harder with stricter requirements
                float bypassDifficulty = (float)Softness / 5.0f; // Convert to 0.0-1.0 range
                return playerSkillLevel >= (1.0f - bypassDifficulty);
            }

            return false;
        }

        /// <summary>
        /// Get the missing requirements for passing this gate
        /// </summary>
        public readonly (Polarity missingPolarity, Ability missingAbilities) GetMissingRequirements(
            Polarity availablePolarity, Ability availableAbilities)
        {
            Polarity missingPolarity = Polarity.None;
            Ability missingAbilities = Ability.None;

            if (RequiredPolarity != Polarity.None && RequiredPolarity != Polarity.Any)
            {
                // Reflect the same ALL vs ANY rule for Hard vs soft when reporting missing bits.
                if (Softness == GateSoftness.Hard)
                    missingPolarity = (RequiredPolarity & ~availablePolarity);
                else if ((availablePolarity & RequiredPolarity) == 0)
                    missingPolarity = RequiredPolarity; // none of the acceptable bits present
            }

            if (RequiredAbilities != Ability.None)
                missingAbilities = RequiredAbilities & ~availableAbilities;

            return (missingPolarity, missingAbilities);
        }

        public override readonly string ToString()
        {
            return $"Gate({RequiredPolarity}, {RequiredAbilities}, {Softness}, Active:{IsActive})";
        }
    }
    
    /// <summary>
    /// Buffer element for storing multiple gate conditions on a single entity
    /// Enables complex multi-requirement gates
    /// </summary>
    public struct GateConditionBufferElement : IBufferElementData
    {
        public GateCondition Value;
        
        public static implicit operator GateCondition(GateConditionBufferElement e) => e.Value;
        public static implicit operator GateConditionBufferElement(GateCondition e) => new() { Value = e };
    }
}
