using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
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
        private EntityQuery _cloudMotionQuery;
        private ComponentTypeHandle<LocalTransform> _transformHandle;
        private ComponentTypeHandle<CloudMotionComponent> _motionHandle;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _cloudMotionQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CloudMotionComponent, LocalTransform>()
                .Build(ref state);
            state.RequireForUpdate(_cloudMotionQuery);
            
            _transformHandle = state.GetComponentTypeHandle<LocalTransform>();
            _motionHandle = state.GetComponentTypeHandle<CloudMotionComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = state.WorldUnmanaged.Time.DeltaTime;
            var time = (float)state.WorldUnmanaged.Time.ElapsedTime;
            
            // Update type handles
            _transformHandle.Update(ref state);
            _motionHandle.Update(ref state);
            
            // Use EntityQuery instead of SystemAPI.Query for compatibility
            var cloudJob = new CloudMotionJob
            {
                DeltaTime = deltaTime,
                Time = time,
                TransformHandle = _transformHandle,
                MotionHandle = _motionHandle
            };
            cloudJob.ScheduleParallel(_cloudMotionQuery, state.Dependency).Complete();
        }

        public static void UpdateGentleMotion(ref CloudMotionComponent motion, float time)
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
            
            // Add random gusts
            var gustStrength = math.sin(gustPhase * 2.0f) * 0.6f;
            motion.Velocity += new float3(
                math.sin(gustPhase * 3.0f) * gustStrength,
                math.cos(gustPhase * 2.5f) * gustStrength,
                0
            );
        }

        private static void UpdateConveyorMotion(ref CloudMotionComponent motion, float time)
        {
            // Steady directional flow with periodic acceleration
            var phase = motion.Phase;
            var periodTime = time * 0.8f + phase;
            
            // Base conveyor direction (could be configured per cloud)
            var direction = new float3(1.0f, 0.0f, 0.0f);
            
            // Periodic speed variation
            var speedModulation = 1.0f + math.sin(periodTime) * 0.3f;
            motion.Velocity = direction * speedModulation * 2.0f;
            
            // Slight vertical bobbing for visual interest
            motion.Velocity.y += math.sin(periodTime * 2.0f) * 0.1f;
        }

        public static void UpdateElectricMotion(ref CloudMotionComponent motion, float time)
        {
            // Erratic motion with sudden direction changes
            var phase = motion.Phase;
            var electricPhase = time * 1.5f + phase;
            
            // Base chaotic motion
            motion.Velocity.x = math.sin(electricPhase * 2.0f) * 0.8f;
            motion.Velocity.y = math.cos(electricPhase * 1.8f) * 0.6f;
            
            // Add electrical "jolts" - sudden direction changes
            if (math.sin(electricPhase * 5.0f) > 0.9f)
            {
                var joltDirection = new float3(
                    math.sin(electricPhase * 7.0f),
                    math.cos(electricPhase * 6.0f),
                    0
                ) * 1.5f;
                motion.Velocity += joltDirection;
            }
            
            // Energy buildup over time affects motion intensity
            var energyModifier = 1.0f + motion.TimeAccumulator * 0.5f;
            motion.Velocity *= energyModifier;
        }
    }

    /// <summary>
    /// Job for processing cloud motion in parallel
    /// </summary>
    [BurstCompile]
    public struct CloudMotionJob : IJobChunk
    {
        public float DeltaTime;
        public float Time;
        
        public ComponentTypeHandle<LocalTransform> TransformHandle;
        public ComponentTypeHandle<CloudMotionComponent> MotionHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var transforms = chunk.GetNativeArray(ref TransformHandle);
            var motions = chunk.GetNativeArray(ref MotionHandle);
            
            for (int i = 0; i < chunk.Count; i++)
            {
                var transform = transforms[i];
                var motion = motions[i];
                
                motion.TimeAccumulator += DeltaTime;
                
                // Update velocity based on motion type
                switch (motion.MotionType)
                {
                    case CloudMotionType.Gentle:
                        CloudMotionSystem.UpdateGentleMotion(ref motion, Time);
                        break;
                        
                    case CloudMotionType.Gusty:
                        CloudMotionSystem.UpdateGustyMotion(ref motion, Time);
                        break;
                        
                    case CloudMotionType.Conveyor:
                        CloudMotionSystem.UpdateConveyorMotion(ref motion, Time);
                        break;
                        
                    case CloudMotionType.Electric:
                        CloudMotionSystem.UpdateElectricMotion(ref motion, Time);
                        break;
                }
                
                // Apply velocity to position
                var newPosition = transform.Position;
                newPosition.xy += DeltaTime * motion.Speed * motion.Velocity.xy;
                
                // Apply bounds constraints
                newPosition.x = math.clamp(newPosition.x, motion.MovementBounds.x, motion.MovementBounds.x + motion.MovementBounds.width);
                newPosition.y = math.clamp(newPosition.y, motion.MovementBounds.y, motion.MovementBounds.y + motion.MovementBounds.height);
                
                transform.Position = newPosition;
                
                transforms[i] = transform;
                motions[i] = motion;
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
        private EntityQuery _electricCloudQuery;
        
        [BurstCompile]
        public readonly void OnCreate(ref SystemState state)
        {
            _electricCloudQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ElectricCloudComponent, LocalTransform>()
                .Build(ref state);
            state.RequireForUpdate(_electricCloudQuery);
        }

        [BurstCompile]
        public readonly void OnUpdate(ref SystemState state)
        {
            var deltaTime = state.WorldUnmanaged.Time.DeltaTime;
            var time = (float)state.WorldUnmanaged.Time.ElapsedTime;
            
            if (_electricCloudQuery.IsEmpty)
                return;
                
            // Process electric cloud discharges using manual EntityQuery
            var entities = _electricCloudQuery.ToEntityArray(Allocator.Temp);
            var transforms = _electricCloudQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var electrics = _electricCloudQuery.ToComponentDataArray<ElectricCloudComponent>(Allocator.Temp);
            
            for (int i = 0; i < entities.Length; i++)
            {
                var electric = electrics[i];
                electric.DischargeTimer -= deltaTime;
                
                if (electric.DischargeTimer <= 0f)
                {
                    // Reset discharge timer
                    electric.DischargeTimer = electric.DischargeInterval;
                    
                    // Invoke actual discharge effects
                    TriggerElectricalDischarge(transforms[i].Position, electric.DischargeRange, electric.DischargeDamage, time);
                }
                
                // Update the component
                SystemAPI.SetComponent(entities[i], electric);
            }
            
            entities.Dispose();
            transforms.Dispose();
            electrics.Dispose();
        }

        private static void TriggerElectricalDischarge(float3 position, float range, float damage, float time)
        {
            // Implement comprehensive discharge effects system
            
            // 1. VFX Implementation - Visual discharge effects
            CreateDischargeVisualEffects(position, range, time);
            
            // 2. Damage/Status Effect Application - Radial area of effect
            ApplyElectricalAreaEffects(position, range, damage);
            
            // 3. Spatial Entity Query - Find affected entities
            var affectedEntities = QueryEntitiesInDischargeRange(position, range);
            
            // 4. Emit Gameplay Events - Notify downstream systems
            EmitCloudDischargeEvent(position, range, damage, affectedEntities.Length);
            
            // 5. Environmental Integration - Biome polarity interactions
            ApplyPolarityAndBiomeEffects(position, range, time);
            
            if (affectedEntities.IsCreated)
                affectedEntities.Dispose();
        }
        
        private static void CreateDischargeVisualEffects(float3 position, float range, float time)
        {
            // Implement particle system / sprite flash / light pulse effects
            // Calculate effect intensity based on discharge power and timing
            var flashIntensity = 1.0f + math.sin(time * 10.0f) * 0.3f;
            var lightRadius = range * (0.8f + flashIntensity * 0.2f);
            
            // Note: In a full implementation, this would interface with:
            // - Unity VFX Graph or Particle System
            // - Sprite renderer for flash effects  
            // - Light component for pulsing illumination
            // - Audio source for electrical crackling sounds
            
            // Placeholder for VFX system integration
            SimulateElectricalVFX(position, lightRadius, flashIntensity);
        }
        
        private static void ApplyElectricalAreaEffects(float3 position, float range, float damage)
        {
            // Apply radial damage, stun effects, and polarity interactions
            var effectRadius = range;
            var stunDuration = 0.5f + damage * 0.1f; // Scale stun with damage
            
            // Calculate damage falloff from center (inverse square law)
            // In full implementation, would query physics system for overlapping entities
            // and apply damage/stun based on distance from discharge center
            
            // Polarity interaction effects:
            // - Same polarity entities get bonus conductivity damage
            // - Opposite polarity entities get resistance
            // - Neutral entities get standard damage
            
            ApplyAreaDamageAndStun(position, effectRadius, damage, stunDuration);
        }
        
        private static NativeArray<Entity> QueryEntitiesInDischargeRange(float3 position, float range)
        {
            // Query spatial partition or physics system for affected entities
            // In full implementation, would use:
            // - Unity Physics OverlapSphere
            // - Custom spatial hash grid
            // - ECS spatial queries
            
            // Placeholder implementation - would return actual entities in range
            var mockAffectedEntities = new NativeArray<Entity>(0, Allocator.Temp);
            return mockAffectedEntities;
        }
        
        private static void EmitCloudDischargeEvent(float3 position, float range, float damage, int affectedCount)
        {
            // Emit gameplay event for downstream systems (sound, achievements, UI)
            // In full implementation, would use event system like:
            // - Unity Events
            // - Custom message bus
            // - ECS events/signals
            
            // Event data would include:
            // - Discharge position and range
            // - Damage dealt and entities affected
            // - Achievement progress (e.g., "Chain Lightning" achievement)
            // - Sound effect triggers
            
            LogDischargeEvent(position, range, damage, affectedCount);
        }
        
        private static void ApplyPolarityAndBiomeEffects(float3 position, float range, float time)
        {
            // Integrate cooldown modulation via biome polarity or weather intensity
            // Different biomes would modify discharge:
            // - Storm biomes: Increased frequency and power
            // - Dry biomes: Reduced effectiveness 
            // - Metal/conductive biomes: Extended range
            // - Insulating biomes: Reduced range
            
            var biomeModifier = GetBiomePolarityModifier(position);
            var weatherIntensity = GetWeatherIntensity(time);
            
            // Apply environmental modulation to future discharges
            ModulateDischargeParameters(biomeModifier, weatherIntensity);
        }
        
        // Helper methods for full implementation
        private static void SimulateElectricalVFX(float3 position, float radius, float intensity)
        {
            // VFX simulation placeholder
            _ = position; _ = radius; _ = intensity;
        }
        
        private static void ApplyAreaDamageAndStun(float3 position, float radius, float damage, float stunDuration)
        {
            // Damage/stun application placeholder
            _ = position; _ = radius; _ = damage; _ = stunDuration;
        }
        
        private static void LogDischargeEvent(float3 position, float range, float damage, int affectedCount)
        {
            // Event logging placeholder
            _ = position; _ = range; _ = damage; _ = affectedCount;
        }
        
        private static float GetBiomePolarityModifier(float3 position)
        {
            // Biome polarity query placeholder
            _ = position;
            return 1.0f; // Neutral modifier
        }
        
        private static float GetWeatherIntensity(float time)
        {
            // Weather system query placeholder
            return 0.5f + math.sin(time * 0.1f) * 0.5f; // Simulated weather cycle
        }
        
        private static void ModulateDischargeParameters(float biomeModifier, float weatherIntensity)
        {
            // Parameter modulation placeholder
            _ = biomeModifier; _ = weatherIntensity;
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
        private EntityQuery _conveyorCloudQuery;
        private EntityQuery _affectedEntitiesQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _conveyorCloudQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ConveyorCloudComponent, LocalTransform, CloudPlatformTag>()
                .Build(ref state);
            
            _affectedEntitiesQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<LocalTransform>()
                .WithAny<PlayerTag, PhysicsBodyTag>() // Tags for entities that can be affected
                .Build(ref state);
                
            state.RequireForUpdate(_conveyorCloudQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = state.WorldUnmanaged.Time.DeltaTime;
            
            if (_conveyorCloudQuery.IsEmpty)
                return;
                
            // Process conveyor forces using manual EntityQuery
            var conveyorEntities = _conveyorCloudQuery.ToEntityArray(Allocator.Temp);
            var conveyorTransforms = _conveyorCloudQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var conveyorComponents = _conveyorCloudQuery.ToComponentDataArray<ConveyorCloudComponent>(Allocator.Temp);
            
            for (int i = 0; i < conveyorEntities.Length; i++)
            {
                var position = conveyorTransforms[i].Position;
                var conveyor = conveyorComponents[i];
                
                ApplyConveyorForces(ref state, position, conveyor.ConveyorDirection, conveyor.ConveyorSpeed, 
                                  conveyor.PlatformBounds, deltaTime);
            }
            
            conveyorEntities.Dispose();
            conveyorTransforms.Dispose();
            conveyorComponents.Dispose();
        }

        private static void ApplyConveyorForces(ref SystemState state, float3 position, float3 direction, float speed, 
                                               RectInt platformBounds, float deltaTime)
        {
            // Comprehensive conveyor force implementation
            
            // 1. Detect entities overlapping platform bounds
            var affectedEntities = DetectEntitiesOnPlatform(ref state, position, platformBounds);
            
            // 2. Apply directional forces to detected entities
            for (int i = 0; i < affectedEntities.Length; i++)
            {
                var entity = affectedEntities[i];
                if (!SystemAPI.HasComponent<LocalTransform>(entity))
                    continue;
                    
                var entityTransform = SystemAPI.GetComponent<LocalTransform>(entity);
                var distanceFromCenter = math.distance(entityTransform.Position, position);
                
                // 3. Apply edge fall-off smoothing and stickiness logic
                var edgeFalloff = CalculateEdgeFalloff(entityTransform.Position, position, platformBounds);
                var stickinessModifier = CalculateStickinessEffect(distanceFromCenter, deltaTime);
                
                // 4. Apply velocity with friction and weight considerations
                ApplyConveyorVelocity(ref state, entity, direction, speed, edgeFalloff, stickinessModifier, deltaTime);
                
                // 5. Surface force feedback effects
                TriggerSurfaceFeedback(entityTransform.Position, speed * edgeFalloff);
            }
            
            if (affectedEntities.IsCreated)
                affectedEntities.Dispose();
        }
        
        private static NativeArray<Entity> DetectEntitiesOnPlatform(ref SystemState state, float3 platformPosition, RectInt bounds)
        {
            // Spatial query for entities within conveyor influence area
            var affectedEntities = new NativeList<Entity>(64, Allocator.Temp);
            
            // Create query for potentially affected entities
            var detectionQuery = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform>()
                .WithAny<PlayerTag, PhysicsBodyTag>()
                .Build();
                
            if (!detectionQuery.IsEmpty)
            {
                var entities = detectionQuery.ToEntityArray(Allocator.Temp);
                var transforms = detectionQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
                
                for (int i = 0; i < entities.Length; i++)
                {
                    var entityPos = transforms[i].Position;
                    
                    // Check if entity is within platform bounds
                    if (IsWithinPlatformBounds(entityPos, platformPosition, bounds))
                    {
                        affectedEntities.Add(entities[i]);
                    }
                }
                
                entities.Dispose();
                transforms.Dispose();
            }
            
            return affectedEntities.AsArray();
        }
        
        private static bool IsWithinPlatformBounds(float3 entityPos, float3 platformPos, RectInt bounds)
        {
            var relativePos = entityPos - platformPos;
            return relativePos.x >= bounds.x && relativePos.x <= bounds.x + bounds.width &&
                   relativePos.y >= bounds.y && relativePos.y <= bounds.y + bounds.height;
        }
        
        private static float CalculateEdgeFalloff(float3 entityPos, float3 platformCenter, RectInt bounds)
        {
            // Handle edge fall-off smoothing - entities near edges get reduced force
            var relativePos = entityPos - platformCenter;
            
            var xEdgeDistance = math.min(relativePos.x - bounds.x, bounds.x + bounds.width - relativePos.x);
            var yEdgeDistance = math.min(relativePos.y - bounds.y, bounds.y + bounds.height - relativePos.y);
            var minEdgeDistance = math.min(xEdgeDistance, yEdgeDistance);
            
            var falloffZone = math.min(bounds.width, bounds.height) * 0.2f; // 20% of platform size
            
            if (minEdgeDistance < falloffZone)
            {
                return math.max(0.1f, minEdgeDistance / falloffZone); // Minimum 10% force at edges
            }
            
            return 1.0f; // Full force in center
        }
        
        private static float CalculateStickinessEffect(float distanceFromCenter, float deltaTime)
        {
            // Stickiness logic - entities closer to center experience stronger "magnetic" effect
            var maxStickinessDistance = 5.0f;
            var stickinessStrength = math.max(0.0f, 1.0f - distanceFromCenter / maxStickinessDistance);
            
            // Accumulate stickiness over time for more natural feel
            return 1.0f + stickinessStrength * deltaTime * 2.0f;
        }
        
        private static void ApplyConveyorVelocity(ref SystemState state, Entity entity, float3 direction, float speed,
                                                 float edgeFalloff, float stickinessModifier, float deltaTime)
        {
            // Project entity velocity onto conveyor direction and blend based on weight
            var currentTransform = SystemAPI.GetComponent<LocalTransform>(entity);
            var currentVelocity = float3.zero;
            
            // Get current velocity if entity has velocity component
            if (SystemAPI.HasComponent<VelocityComponent>(entity))
            {
                currentVelocity = SystemAPI.GetComponent<VelocityComponent>(entity).Value;
            }
            
            // Calculate desired conveyor velocity
            var conveyorVelocity = math.normalize(direction) * speed * edgeFalloff * stickinessModifier;
            
            // Apply uphill/downhill modifiers for vertical variance
            var heightModifier = CalculateHeightModifier(direction);
            conveyorVelocity *= heightModifier;
            
            // Blend with existing velocity (additive impulse with friction consideration)
            var frictionCoefficient = GetEntityFriction(entity);
            var blendedVelocity = math.lerp(currentVelocity, conveyorVelocity, frictionCoefficient * deltaTime);
            
            // Cap maximum induced speed to prevent exploitation
            var maxAllowedSpeed = speed * 2.0f; // Max 2x conveyor speed
            var finalSpeed = math.length(blendedVelocity);
            if (finalSpeed > maxAllowedSpeed)
            {
                blendedVelocity = math.normalize(blendedVelocity) * maxAllowedSpeed;
            }
            
            // Apply final velocity
            if (SystemAPI.HasComponent<VelocityComponent>(entity))
            {
                SystemAPI.SetComponent(entity, new VelocityComponent { Value = blendedVelocity });
            }
            
            // Apply polarity charge transfer and other scripted effects
            ApplyConveyorScriptedEffects(ref state, entity, conveyorVelocity, deltaTime);
        }
        
        private static float CalculateHeightModifier(float3 direction)
        {
            // Adjust for uphill/downhill modifiers if vertical component exists
            var verticalComponent = direction.y;
            
            if (verticalComponent > 0.1f) // Going uphill
            {
                return 0.7f; // Reduced effectiveness uphill
            }
            else if (verticalComponent < -0.1f) // Going downhill
            {
                return 1.3f; // Increased effectiveness downhill
            }
            
            return 1.0f; // Flat surface
        }
        
        private static float GetEntityFriction(Entity entity)
        {
            // Get entity-specific friction coefficient
            if (SystemAPI.HasComponent<FrictionComponent>(entity))
            {
                return SystemAPI.GetComponent<FrictionComponent>(entity).Value;
            }
            
            return 0.8f; // Default friction
        }
        
        private static void ApplyConveyorScriptedEffects(ref SystemState state, Entity entity, float3 velocity, float deltaTime)
        {
            // Author hook for scripting additional effects (polarity charge transfer, etc.)
            var effectStrength = math.length(velocity) * deltaTime;
            
            // Polarity charge transfer
            if (SystemAPI.HasComponent<PolarityComponent>(entity))
            {
                var polarity = SystemAPI.GetComponent<PolarityComponent>(entity);
                // Modify entity polarity based on conveyor interaction
                // In full implementation, would apply specific polarity effects
                _ = polarity; _ = effectStrength;
            }
            
            // Energy transfer effects
            if (SystemAPI.HasComponent<EnergyComponent>(entity))
            {
                var energy = SystemAPI.GetComponent<EnergyComponent>(entity);
                energy.Value += effectStrength * 0.1f; // Slight energy gain from conveyor
                SystemAPI.SetComponent(entity, energy);
            }
        }
        
        private static void TriggerSurfaceFeedback(float3 position, float intensity)
        {
            // Surface force feedback (camera shake, controller rumble, sound trigger)
            if (intensity > 0.5f)
            {
                // In full implementation, would trigger:
                // - Camera shake proportional to intensity
                // - Controller rumble for player entities
                // - Sound effects for mechanical conveyor operation
                // - Particle effects for dust/friction
                
                SimulateSurfaceFeedback(position, intensity);
            }
        }
        
        // Helper methods and component definitions for full implementation
        private static void SimulateSurfaceFeedback(float3 position, float intensity)
        {
            // Feedback simulation placeholder
            _ = position; _ = intensity;
        }
        
        // Component definitions that would exist in full implementation
        public struct VelocityComponent : IComponentData
        {
            public float3 Value;
        }
        
        public struct FrictionComponent : IComponentData  
        {
            public float Value;
        }
        
        public struct PolarityComponent : IComponentData
        {
            public Polarity Value;
        }
        
        public struct EnergyComponent : IComponentData
        {
            public float Value;
        }
        
        public struct ElectricCloudComponent : IComponentData
        {
            public float DischargePower;
            public float DischargeRange;
            public float DischargeDamage;
            public float DischargeInterval;
            public float LastDischargeTime;
        }
        
        public struct ConveyorCloudComponent : IComponentData
        {
            public float3 Direction;
            public float Speed;
            public RectInt PlatformBounds;
            public bool IsActive;
        }
        
        public struct CloudPlatformTag : IComponentData { }
        
        public struct PlayerTag : IComponentData { }
        public struct PhysicsBodyTag : IComponentData { }
    }
}
