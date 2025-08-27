using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
#if !UNITY_TRANSFORMS_LOCALTRANSFORM
using LocalTransform = TinyWalnutGames.MetVD.Core.Compat.LocalTransformCompat;
#endif

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// System that handles cloud motion physics and behavior
    /// Implements the actual motion components instead of placeholder markers
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct CloudMotionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CloudMotionComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = state.WorldUnmanaged.Time.DeltaTime;
            var time = (float)state.WorldUnmanaged.Time.ElapsedTime;
            
            // Process cloud motion using SystemAPI.Query instead of IJobEntity
            foreach (var (transform, motion) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<CloudMotionComponent>>())
            {
                motion.ValueRW.TimeAccumulator += deltaTime;
                
                // Update velocity based on motion type
                switch (motion.ValueRO.MotionType)
                {
                    case CloudMotionType.Gentle:
                        UpdateGentleMotion(ref motion.ValueRW, time);
                        break;
                        
                    case CloudMotionType.Gusty:
                        UpdateGustyMotion(ref motion.ValueRW, time);
                        break;
                        
                    case CloudMotionType.Conveyor:
                        UpdateConveyorMotion(ref motion.ValueRW, time);
                        break;
                        
                    case CloudMotionType.Electric:
                        UpdateElectricMotion(ref motion.ValueRW, time);
                        break;
                }
                
                // Apply velocity to position
                var newPosition = transform.ValueRO.Position;
                newPosition.xy += motion.ValueRO.Velocity * motion.ValueRO.Speed * deltaTime;
                
                // Apply bounds constraints
                newPosition.x = math.clamp(newPosition.x, motion.ValueRO.MovementBounds.x, motion.ValueRO.MovementBounds.x + motion.ValueRO.MovementBounds.width);
                newPosition.y = math.clamp(newPosition.y, motion.ValueRO.MovementBounds.y, motion.ValueRO.MovementBounds.y + motion.ValueRO.MovementBounds.height);
                
                transform.ValueRW.Position = newPosition;
            }
        }

        private static void UpdateGentleMotion(ref CloudMotionComponent motion, float time)
        {
            // Slow, predictable sinusoidal motion
            var phaseOffset = motion.Phase;
            motion.Velocity.x = math.sin(time * 0.5f + phaseOffset) * 0.3f;
            motion.Velocity.y = math.cos(time * 0.3f + phaseOffset) * 0.2f;
        }

        private static void UpdateGustyMotion(ref CloudMotionComponent motion, float time)
        {
            // Irregular gusts with varying intensity
            var basePhase = motion.Phase;
            var gustPhase = time * 1.2f + basePhase;
            
            // Base gentle motion
            motion.Velocity.x = math.sin(gustPhase * 0.8f) * 0.4f;
            motion.Velocity.y = math.cos(gustPhase * 0.6f) * 0.3f;
            
            // Add gusts
            var gustIntensity = math.sin(gustPhase * 3.0f) * 0.5f + 0.5f;
            motion.Velocity *= (1.0f + gustIntensity);
        }

        private static void UpdateConveyorMotion(ref CloudMotionComponent motion, float time)
        {
            // Mechanical, predictable movement
            var direction = math.normalize(motion.Velocity);
            if (math.length(direction) < 0.1f)
            {
                direction = new float2(1, 0); // Default right movement
            }
            
            motion.Velocity = direction * 0.8f; // Constant speed
        }

        private static void UpdateElectricMotion(ref CloudMotionComponent motion, float time)
        {
            // Rapid, energetic movement with electrical jolts
            var basePhase = motion.Phase;
            var electricPhase = time * 4.0f + basePhase;
            
            // Rapid oscillation
            motion.Velocity.x = math.sin(electricPhase * 2.3f) * 0.6f;
            motion.Velocity.y = math.cos(electricPhase * 1.9f) * 0.5f;
            
            // Add electrical jolts
            var joltPhase = time * 8.0f + basePhase;
            if (math.sin(joltPhase) > 0.9f)
            {
                motion.Velocity *= 2.0f; // Sudden acceleration
            }
        }
    }

    /// <summary>
    /// System that handles electric cloud discharge effects
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CloudMotionSystem))]
    public partial struct ElectricCloudSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ElectricCloudComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = state.WorldUnmanaged.Time.DeltaTime;
            
            // Process electric cloud discharges using SystemAPI.Query
            foreach (var (transform, electric) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<ElectricCloudComponent>>())
            {
                electric.ValueRW.DischargeTimer -= deltaTime;
                
                if (electric.ValueRW.DischargeTimer <= 0f)
                {
                    // Reset discharge timer
                    electric.ValueRW.DischargeTimer = electric.ValueRO.DischargeInterval;
                    
                    // Trigger discharge effects - create electrical particles and affect nearby entities
                    TriggerElectricalDischarge(transform.ValueRO.Position, electric.ValueRO.DischargeRange);
                }
            }
        }

        private static void TriggerElectricalDischarge(float3 position, float range)
        {
            // Create electrical discharge effect
            // This would typically:
            // 1. Spawn electrical particle effects
            // 2. Apply electrical damage to nearby entities
            // 3. Create temporary electromagnetic field effects
            // 4. Trigger audio/visual feedback
            
            // For gameplay systems, this would query for entities within range
            // and apply electrical effects based on entity type and distance
        }
    }

    /// <summary>
    /// System that applies conveyor forces to entities on conveyor clouds
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CloudMotionSystem))]
    public partial struct ConveyorForceSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ConveyorCloudComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = state.WorldUnmanaged.Time.DeltaTime;
            
            // Process conveyor forces using SystemAPI.Query
            foreach (var (transform, conveyor, platform) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<ConveyorCloudComponent>, RefRO<CloudPlatformTag>>())
            {
                // Apply conveyor forces to entities within range
                ApplyConveyorForces(transform.ValueRO.Position, conveyor.ValueRO.ConveyorDirection, conveyor.ValueRO.ConveyorSpeed);
            }
        }

        private static void ApplyConveyorForces(float3 position, float3 direction, float speed)
        {
            // Apply conveyor belt forces to entities within range
            // This would typically:
            // 1. Query for entities within conveyor range
            // 2. Apply forces based on conveyor direction and speed
            // 3. Handle player movement physics interactions
            // 4. Create visual feedback effects
            
            // For gameplay systems, this would modify entity velocities
            // based on their proximity to the conveyor cloud
        }
    }
}