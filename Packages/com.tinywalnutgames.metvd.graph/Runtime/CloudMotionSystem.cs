using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
#if UNITY_TRANSFORMS_LOCALTRANSFORM
using Unity.Transforms;
#else
using TinyWalnutGames.MetVD.Core.Compat;
using LocalTransform = TinyWalnutGames.MetVD.Core.Compat.LocalTransformCompat;
#endif

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// System that handles cloud motion physics and behavior
    /// Provides motion updates; compatible with absence of Unity.Transforms via LocalTransformCompat.
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
            var motionJob = new CloudMotionJob { DeltaTime = deltaTime, Time = (float)state.WorldUnmanaged.Time.ElapsedTime };
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
            switch (motion.MotionType)
            {
                case CloudMotionType.Gentle: UpdateGentleMotion(ref motion, Time); break;
                case CloudMotionType.Gusty: UpdateGustyMotion(ref motion, Time); break;
                case CloudMotionType.Conveyor: UpdateConveyorMotion(ref motion, Time); break;
                case CloudMotionType.Electric: UpdateElectricMotion(ref motion, Time); break;
            }
#if UNITY_TRANSFORMS_LOCALTRANSFORM
            var newPosition = transform.Position;
            newPosition.xy += motion.Velocity * motion.Speed * DeltaTime;
            newPosition.x = math.clamp(newPosition.x, motion.Bounds.x, motion.Bounds.z);
            newPosition.y = math.clamp(newPosition.y, motion.Bounds.y, motion.Bounds.w);
            transform.Position = newPosition;
#else
            var newPos = transform.Position;
            newPos.xy += motion.Velocity * motion.Speed * DeltaTime;
            newPos.x = math.clamp(newPos.x, motion.Bounds.x, motion.Bounds.z);
            newPos.y = math.clamp(newPos.y, motion.Bounds.y, motion.Bounds.w);
            transform.Position = newPos;
#endif
        }

        private void UpdateGentleMotion(ref CloudMotionComponent motion, float time)
        {
            var phaseOffset = motion.Phase;
            motion.Velocity.x = math.sin(time * 0.5f + phaseOffset) * 0.3f;
            motion.Velocity.y = math.cos(time * 0.3f + phaseOffset) * 0.2f;
        }
        private void UpdateGustyMotion(ref CloudMotionComponent motion, float time)
        {
            var basePhase = motion.Phase;
            var gustPhase = time * 2.0f + basePhase;
            motion.Velocity.x = math.sin(gustPhase * 0.7f) * 0.4f;
            motion.Velocity.y = math.cos(gustPhase * 0.5f) * 0.3f;
            var gustStrength = math.sin(gustPhase * 3.0f) * math.sin(gustPhase * 1.7f);
            if (gustStrength > 0.6f) motion.Velocity *= 1.0f + gustStrength;
        }
        private void UpdateConveyorMotion(ref CloudMotionComponent motion, float time)
        {
            var direction = math.normalize(motion.Velocity);
            if (math.length(direction) < 0.1f) direction = new float2(1, 0);
            motion.Velocity = direction * 0.8f;
        }
        private void UpdateElectricMotion(ref CloudMotionComponent motion, float time)
        {
            var basePhase = motion.Phase;
            var electricPhase = time * 4.0f + basePhase;
            motion.Velocity.x = math.sin(electricPhase * 2.3f) * 0.6f;
            motion.Velocity.y = math.cos(electricPhase * 1.9f) * 0.5f;
            var joltPhase = time * 8.0f + basePhase;
            if (math.sin(joltPhase) > 0.9f) motion.Velocity *= 2.0f;
        }
    }

#if ENABLE_CLOUD_ADVANCED_SYSTEMS
    /// <summary>
    /// System that handles electric cloud discharge effects
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CloudMotionSystem))]
    public partial struct ElectricCloudSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state) { state.RequireForUpdate<ElectricCloudComponent>(); }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dischargeJob = new ElectricDischargeJob { DeltaTime = state.WorldUnmanaged.Time.DeltaTime };
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
                electric.NextDischarge = electric.DischargeInterval;
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
        [BurstCompile] public void OnCreate(ref SystemState state) { state.RequireForUpdate<ConveyorCloudComponent>(); }
        [BurstCompile] public void OnUpdate(ref SystemState state)
        {
            var conveyorJob = new ConveyorForceJob { DeltaTime = state.WorldUnmanaged.Time.DeltaTime };
            state.Dependency = conveyorJob.ScheduleParallel(state.Dependency);
        }
    }

    /// <summary>
    /// Job that applies conveyor forces (would interact with player physics in full implementation)
    /// </summary>
    [BurstCompile]
    public partial struct ConveyorForceJob : IJobEntity
    {
        public float DeltaTime;
        public void Execute(in LocalTransform transform, in ConveyorCloudComponent conveyor, in CloudPlatformTag platform)
        {
            // Placeholder - kept for future force application logic.
        }
    }
#endif
}
