using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// Component for cloud motion physics and behavior
    /// Replaces the placeholder marker system in TerrainAndSkyGenerators
    /// </summary>
    public struct CloudMotionComponent : IComponentData
    {
        /// <summary>
        /// Type of motion this cloud exhibits
        /// </summary>
        public CloudMotionType MotionType;
        
        /// <summary>
        /// Current velocity vector
        /// </summary>
        public float2 Velocity;
        
        /// <summary>
        /// Base speed multiplier for this cloud
        /// </summary>
        public float Speed;
        
        /// <summary>
        /// Motion pattern phase (for sinusoidal or cyclic motion)
        /// </summary>
        public float Phase;
        
        /// <summary>
        /// Motion bounds (for constrained movement)
        /// </summary>
        public float4 Bounds; // x,y = min bounds, z,w = max bounds
        
        /// <summary>
        /// Time accumulator for pattern calculations
        /// </summary>
        public float TimeAccumulator;

        public CloudMotionComponent(CloudMotionType motionType, float2 initialVelocity, float speed, 
                                  float4 bounds, float phase = 0f)
        {
            MotionType = motionType;
            Velocity = initialVelocity;
            Speed = math.max(0.1f, speed);
            Phase = phase;
            Bounds = bounds;
            TimeAccumulator = 0f;
        }
    }

    /// <summary>
    /// Tag component to mark entities as cloud platforms
    /// Used for gameplay interaction systems
    /// </summary>
    public struct CloudPlatformTag : IComponentData
    {
        /// <summary>
        /// Whether players can stand on this cloud
        /// </summary>
        public bool CanStandOn;
        
        /// <summary>
        /// Damage per second if touching (for electric clouds)
        /// </summary>
        public float ContactDamage;
        
        /// <summary>
        /// Force applied to player when in contact (for conveyor clouds)
        /// </summary>
        public float2 ConveyorForce;

        public CloudPlatformTag(bool canStandOn = true, float contactDamage = 0f, float2 conveyorForce = default)
        {
            CanStandOn = canStandOn;
            ContactDamage = contactDamage;
            ConveyorForce = conveyorForce;
        }
    }

    /// <summary>
    /// Component for electric cloud special effects and behavior
    /// </summary>
    public struct ElectricCloudComponent : IComponentData
    {
        /// <summary>
        /// Electrical discharge interval (seconds)
        /// </summary>
        public float DischargeInterval;
        
        /// <summary>
        /// Time until next discharge
        /// </summary>
        public float NextDischarge;
        
        /// <summary>
        /// Range of electrical effect
        /// </summary>
        public float EffectRange;
        
        /// <summary>
        /// Damage dealt by electrical discharge
        /// </summary>
        public float DischargeDamage;

        public ElectricCloudComponent(float dischargeInterval = 2f, float effectRange = 3f, float dischargeDamage = 1f)
        {
            DischargeInterval = math.max(0.1f, dischargeInterval);
            NextDischarge = dischargeInterval;
            EffectRange = math.max(0.5f, effectRange);
            DischargeDamage = math.max(0f, dischargeDamage);
        }
    }

    /// <summary>
    /// Component for conveyor cloud mechanical behavior
    /// </summary>
    public struct ConveyorCloudComponent : IComponentData
    {
        /// <summary>
        /// Direction and strength of conveyor force
        /// </summary>
        public float2 ConveyorDirection;
        
        /// <summary>
        /// Force magnitude applied to entities on the conveyor
        /// </summary>
        public float ForceStrength;
        
        /// <summary>
        /// Whether the conveyor affects airborne entities
        /// </summary>
        public bool AffectsAirborne;

        public ConveyorCloudComponent(float2 direction, float forceStrength = 5f, bool affectsAirborne = false)
        {
            ConveyorDirection = math.normalize(direction);
            ForceStrength = math.max(0f, forceStrength);
            AffectsAirborne = affectsAirborne;
        }
    }
}