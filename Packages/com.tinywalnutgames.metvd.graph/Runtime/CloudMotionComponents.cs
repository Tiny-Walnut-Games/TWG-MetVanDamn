using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
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
        public float DischargeDamage;
        public bool IsCharging;
        
        public ElectricCloudComponent(float interval = 3.0f, float range = 5.0f, float damage = 10.0f)
        {
            DischargeTimer = 0f;
            DischargeInterval = interval;
            DischargeRange = range;
            DischargeDamage = damage;
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
        public RectInt PlatformBounds;
        public bool ReverseDirection;
        
        public ConveyorCloudComponent(float3 direction, float speed = 2.0f, RectInt bounds = default)
        {
            ConveyorDirection = direction;
            ConveyorSpeed = speed;
            PlatformBounds = bounds.width == 0 ? new RectInt(-5, -5, 10, 10) : bounds; // Default 10x10 platform
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