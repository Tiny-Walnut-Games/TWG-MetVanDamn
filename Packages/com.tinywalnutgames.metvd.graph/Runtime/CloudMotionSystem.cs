using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

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
            
            var motionJob = new CloudMotionJob
            {
                DeltaTime = deltaTime,
                Time = (float)state.WorldUnmanaged.Time.ElapsedTime
            };
            
            state.Dependency = motionJob.ScheduleParallel(state.Dependency);
        }
    }

    /// <summary>
    /// Job that processes cloud motion for all cloud entities
    /// </summary>
    [BurstCompile]
    public partial struct CloudMotionJob : IJobEntity
    {
        public float DeltaTime;
        public float Time;

        public void Execute(RefRW<LocalTransform> transform, ref CloudMotionComponent motion)
        {
            motion.TimeAccumulator += DeltaTime;
            
            // Update velocity based on motion type
            switch (motion.MotionType)
            {
                case CloudMotionType.Gentle:
                    UpdateGentleMotion(ref motion, Time);
                    break;
                    
                case CloudMotionType.Gusty:
                    UpdateGustyMotion(ref motion, Time);
                    break;
                    
                case CloudMotionType.Conveyor:
                    UpdateConveyorMotion(ref motion, Time);
                    break;
                    
                case CloudMotionType.Electric:
                    UpdateElectricMotion(ref motion, Time);
                    break;
            }
            
            // Apply velocity to position
            var newPosition = transform.ValueRO.Position;
            newPosition.xy += motion.Velocity * motion.Speed * DeltaTime;
            
            // Apply bounds constraints
            newPosition.x = math.clamp(newPosition.x, motion.MovementBounds.x, motion.MovementBounds.x + motion.MovementBounds.width);
            newPosition.y = math.clamp(newPosition.y, motion.MovementBounds.y, motion.MovementBounds.y + motion.MovementBounds.height);
            
            transform.ValueRW.Position = newPosition;
        }

        private void UpdateGentleMotion(ref CloudMotionComponent motion, float time)
        {
            // Slow, predictable sinusoidal motion
            var phaseOffset = motion.Phase;
            motion.Velocity.x = math.sin(time * 0.5f + phaseOffset) * 0.3f;
            motion.Velocity.y = math.cos(time * 0.3f + phaseOffset) * 0.2f;
        }

        private void UpdateGustyMotion(ref CloudMotionComponent motion, float time)
        {
            // Irregular wind patterns with random gusts
            var basePhase = motion.Phase;
            var gustPhase = time * 2.0f + basePhase;
            
            // Base wind
            motion.Velocity.x = math.sin(gustPhase * 0.7f) * 0.4f;
            motion.Velocity.y = math.cos(gustPhase * 0.5f) * 0.3f;
            
            // Add random gusts
            var gustStrength = math.sin(gustPhase * 3.0f) * math.sin(gustPhase * 1.7f);
            if (gustStrength > 0.6f)
            {
                motion.Velocity *= 1.0f + gustStrength;
            }
        }

        private void UpdateConveyorMotion(ref CloudMotionComponent motion, float time)
        {
            // Mechanical, predictable movement
            var direction = math.normalize(motion.Velocity);
            if (math.length(direction) < 0.1f)
            {
                direction = new float2(1, 0); // Default right movement
            }
            
            motion.Velocity = direction * 0.8f; // Constant speed
        }

        private void UpdateElectricMotion(ref CloudMotionComponent motion, float time)
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
            
            var dischargeJob = new ElectricDischargeJob
            {
                DeltaTime = deltaTime
            };
            
            state.Dependency = dischargeJob.ScheduleParallel(state.Dependency);
        }
    }

    /// <summary>
    /// Job that processes electric cloud discharge timing
    /// </summary>
    [BurstCompile]
    public partial struct ElectricDischargeJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(in RefRO<LocalTransform> transform, ref ElectricCloudComponent electric)
        {
            electric.DischargeTimer -= DeltaTime;
            
            if (electric.DischargeTimer <= 0f)
            {
                // Reset discharge timer
                electric.DischargeTimer = electric.DischargeInterval;
                
                // Trigger discharge effects - create electrical particles and affect nearby entities
                TriggerElectricalDischarge(transform.ValueRO.Position, electric.DischargeRange);
            }
        }

        private void TriggerElectricalDischarge(float3 position, float range)
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
        private ComponentLookup<LocalTransform> _transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ConveyorCloudComponent>();
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _transformLookup.Update(ref state);
            
            var conveyorJob = new ConveyorForceJob
            {
                TransformLookup = _transformLookup,
                DeltaTime = state.WorldUnmanaged.Time.DeltaTime
            };
            
            state.Dependency = conveyorJob.ScheduleParallel(state.Dependency);
        }
    }

    /// <summary>
    /// Job that applies conveyor forces (would interact with player physics in full implementation)
    /// </summary>
    [BurstCompile]
    public partial struct ConveyorForceJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        public float DeltaTime;

        public void Execute(in RefRO<LocalTransform> transform, in ConveyorCloudComponent conveyor, in CloudPlatformTag platform)
        {
            // Apply conveyor forces to entities within range
            ApplyConveyorForces(transform.ValueRO.Position, conveyor.ConveyorDirection, conveyor.ConveyorSpeed);
        }

        private void ApplyConveyorForces(float3 position, float3 direction, float speed)
        {
            // Apply conveyor belt forces to nearby entities
            // This would typically:
            // 1. Query for player/physics entities within platform range
            // 2. Check if entities are standing on or near the platform
            // 3. Apply horizontal velocity in the conveyor direction
            // 4. Handle platform-specific physics interactions
            
            // For gameplay systems, this would modify velocity components
            // of entities within the conveyor's influence area
        }
    }
}