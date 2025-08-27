using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// System that handles cloud motion physics and platform mechanics
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(TransformSystemGroup))]
    public partial struct CloudMotionSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _transformLookup = state.GetComponentLookup<LocalTransform>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _transformLookup.Update(ref state);

            var deltaTime = state.WorldUnmanaged.Time.DeltaTime;
            var time = (float)state.WorldUnmanaged.Time.ElapsedTime;

            // Update basic cloud motion
            var cloudJob = new CloudMotionJob
            {
                DeltaTime = deltaTime,
                Time = time
            };
            state.Dependency = cloudJob.ScheduleParallel(state.Dependency);

            // Update conveyor forces on entities
            var conveyorJob = new ConveyorForceJob
            {
                DeltaTime = deltaTime,
                TransformLookup = _transformLookup
            };
            state.Dependency = conveyorJob.ScheduleParallel(state.Dependency);
        }
    }

    /// <summary>
    /// Job that updates cloud motion physics based on motion type
    /// </summary>
    [BurstCompile]
    public partial struct CloudMotionJob : IJobEntity
    {
        public float DeltaTime;
        public float Time;

        public void Execute(ref LocalTransform transform, ref CloudMotionComponent motion)
        {
            if (!motion.IsActive) return;

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

            // Apply velocity to transform
            var newPosition = transform.Position + motion.Velocity * DeltaTime * motion.Speed;
            
            // Check bounds and wrap/bounce as needed
            if (motion.MovementBounds.width > 0 && motion.MovementBounds.height > 0)
            {
                if (newPosition.x < motion.MovementBounds.xMin || newPosition.x > motion.MovementBounds.xMax)
                {
                    motion.Velocity.x = -motion.Velocity.x;
                    newPosition.x = math.clamp(newPosition.x, motion.MovementBounds.xMin, motion.MovementBounds.xMax);
                }
                if (newPosition.y < motion.MovementBounds.yMin || newPosition.y > motion.MovementBounds.yMax)
                {
                    motion.Velocity.y = -motion.Velocity.y;
                    newPosition.y = math.clamp(newPosition.y, motion.MovementBounds.yMin, motion.MovementBounds.yMax);
                }
            }

            transform.Position = newPosition;
        }

        private void UpdateGentleMotion(ref CloudMotionComponent motion, float time)
        {
            // Slow, predictable sine wave motion
            var phase = time * 0.5f + motion.TimeAccumulator;
            motion.Velocity.x = math.sin(phase) * 0.5f;
            motion.Velocity.y = math.cos(phase * 0.7f) * 0.3f;
        }

        private void UpdateGustyMotion(ref CloudMotionComponent motion, float time)
        {
            // Irregular wind patterns using multiple sine waves
            var phase1 = time * 1.2f + motion.TimeAccumulator;
            var phase2 = time * 0.8f + motion.TimeAccumulator * 1.5f;
            motion.Velocity.x = (math.sin(phase1) + math.sin(phase2 * 1.3f)) * 0.7f;
            motion.Velocity.y = math.sin(phase1 * 0.6f) * 0.4f;
        }

        private void UpdateConveyorMotion(ref CloudMotionComponent motion, float time)
        {
            // Steady, mechanical movement with periodic direction changes
            var cycle = math.floor(time / 5.0f); // Change direction every 5 seconds
            var direction = (cycle % 2 == 0) ? 1.0f : -1.0f;
            motion.Velocity.x = direction * 1.0f;
            motion.Velocity.y = 0f;
        }

        private void UpdateElectricMotion(ref CloudMotionComponent motion, float time)
        {
            // Rapid, energetic movement with sudden direction changes
            var phase = time * 3.0f + motion.TimeAccumulator;
            var random1 = math.sin(phase * 17.0f); // High frequency for jittery movement
            var random2 = math.cos(phase * 23.0f);
            motion.Velocity.x = random1 * 1.5f;
            motion.Velocity.y = random2 * 1.2f;
        }
    }

    /// <summary>
    /// Job that applies conveyor forces to entities on conveyor clouds
    /// </summary>
    [BurstCompile]
    public partial struct ConveyorForceJob : IJobEntity
    {
        public float DeltaTime;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

        public void Execute(ref LocalTransform transform, in ConveyorCloudComponent conveyor, in CloudMotionComponent motion)
        {
            if (!motion.IsActive) return;

            // Apply conveyor force to the platform itself and any entities on it
            var conveyorForce = conveyor.ConveyorDirection * conveyor.ConveyorSpeed * DeltaTime;
            if (conveyor.ReverseDirection)
            {
                conveyorForce = -conveyorForce;
            }

            transform.Position += conveyorForce;
        }
    }
}