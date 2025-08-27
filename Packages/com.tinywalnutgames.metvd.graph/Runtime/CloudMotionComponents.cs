using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// Component for cloud motion physics - attached to cloud entities
    /// </summary>
    public struct CloudMotionComponent : IComponentData
    {
        public CloudMotionType MotionType;
        public float3 Velocity;
        public float Speed;
        public float TimeAccumulator;
        public float Phase;
        public RectInt MovementBounds;
        public bool IsActive;
        
        public CloudMotionComponent(CloudMotionType motionType, float3 velocity, float speed, RectInt bounds)
        {
            MotionType = motionType;
            Velocity = velocity;
            Speed = speed;
            TimeAccumulator = 0f;
            Phase = 0f;
            MovementBounds = bounds;
            IsActive = true;
        }
    }

    /// <summary>
    /// Component for electric cloud discharge effects
    /// </summary>
    public struct ElectricCloudComponent : IComponentData
    {
        public float DischargeTimer;
        public float DischargeInterval;
        public float DischargeRange;
        public bool IsCharging;
        
        public ElectricCloudComponent(float interval = 3.0f, float range = 5.0f)
        {
            DischargeTimer = 0f;
            DischargeInterval = interval;
            DischargeRange = range;
            IsCharging = false;
        }
    }

    /// <summary>
    /// Component for conveyor cloud platform mechanics
    /// </summary>
    public struct ConveyorCloudComponent : IComponentData
    {
        public float3 ConveyorDirection;
        public float ConveyorSpeed;
        public bool ReverseDirection;
        
        public ConveyorCloudComponent(float3 direction, float speed = 2.0f)
        {
            ConveyorDirection = direction;
            ConveyorSpeed = speed;
            ReverseDirection = false;
        }
    }

    /// <summary>
    /// Tag component to mark clouds for motion system processing
    /// </summary>
    public struct CloudMotionTag : IComponentData
    {
    }

    /// <summary>
    /// Tag component to mark cloud platforms that can interact with player/entities
    /// </summary>
    public struct CloudPlatformTag : IComponentData
    {
    }
}