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

        public void Execute(ref LocalTransform transform, ref CloudMotionComponent motion)
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
            var newPosition = transform.Position;
            newPosition.xy += motion.Velocity * motion.Speed * DeltaTime;
            
            // Apply bounds constraints
            newPosition.x = math.clamp(newPosition.x, motion.Bounds.x, motion.Bounds.z);
            newPosition.y = math.clamp(newPosition.y, motion.Bounds.y, motion.Bounds.w);
            
            transform.Position = newPosition;
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

        public void Execute(ref ElectricCloudComponent electric)
        {
            electric.NextDischarge -= DeltaTime;
            
            if (electric.NextDischarge <= 0f)
            {
                // Reset discharge timer
                electric.NextDischarge = electric.DischargeInterval;
                
                // Discharge would trigger effects here (handled by other systems)
                // For now, just reset the timer to maintain the rhythm
            }
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

        public void Execute(in LocalTransform transform, in ConveyorCloudComponent conveyor, in CloudPlatformTag platform)
        {
            // In a full implementation, this would:
            // 1. Query for nearby player entities
            // 2. Check if player is standing on this cloud platform
            // 3. Apply conveyor force to player's velocity/position
            // 4. Handle airborne effects if enabled
            
            // For now, just maintain the component data structure
            // The actual force application would be handled by a player physics system
        }
    }
}