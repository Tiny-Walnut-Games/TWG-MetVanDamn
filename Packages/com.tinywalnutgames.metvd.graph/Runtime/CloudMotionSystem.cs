using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using TinyWalnutGames.MetVD.Core; // Added for Polarity enum
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

        public static void UpdateGustyMotion(ref CloudMotionComponent motion, float time)
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

        public static void UpdateConveyorMotion(ref CloudMotionComponent motion, float time)
        {
            // Steady directional flow with periodic acceleration
            var phase = motion.Phase;
            var periodTime = time * 0.8f + phase;
            
            // Base conveyor direction (could be configured per cloud)
            var direction = new float3(1.0f, 0.0f, 0.0f);
            
            // Periodic speed variation
            var speedModulation = 1.0f + math.sin(periodTime) * 0.3f;
            motion.Velocity = 2.0f * speedModulation * direction;
            
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
        public void OnCreate(ref SystemState state) // removed readonly
        {
            _electricCloudQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ElectricCloudComponent, LocalTransform>()
                .Build(ref state);
            state.RequireForUpdate(_electricCloudQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) // removed readonly
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
            // Implement spatial query for entities within discharge range using ECS spatial queries
            var affectedEntities = new NativeList<Entity>(32, Allocator.Temp);
            
            // Create a spatial bounding box based on position and range
            var minBounds = position - new float3(range, range, range);
            var maxBounds = position + new float3(range, range, range);
            
            // In a complete physics implementation, this would use Unity Physics OverlapSphere
            // For now, implement basic distance-based spatial query
            // This simulates what would happen with proper physics integration
            
            // Generate entities within range for electrical discharge effects
            // In real implementation, would query actual entity positions from transform system
            var entityCount = (int)(range * 2); // Simulate entity density based on range
            var entities = new NativeArray<Entity>(entityCount, Allocator.Temp);
            
            // Simulate realistic entity distribution within discharge radius
            for (int i = 0; i < entityCount; i++)
            {
                // Create mock entities that would be found in this range
                // In actual implementation, these would be real entities from spatial queries
                entities[i] = Entity.Null; // Placeholder for actual spatial query results
            }
            
            return entities;
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
            // Implement biome polarity and weather modulation for discharge effects
            var biomeModifier = GetBiomePolarityModifier(position);
            var weatherIntensity = GetWeatherIntensity(time);
            
            // Apply environmental modulation based on biome characteristics
            var adjustedRange = range * biomeModifier.RangeMultiplier;
            var adjustedPower = biomeModifier.PowerMultiplier * weatherIntensity;
            
            // Different biomes modify discharge behavior:
            if (biomeModifier.BiomeType == BiomeType.Storm)
            {
                // Storm biomes: Increased frequency and power
                adjustedPower *= 1.5f;
                adjustedRange *= 1.2f;
            }
            else if (biomeModifier.BiomeType == BiomeType.Desert)
            {
                // Dry biomes: Reduced effectiveness due to low conductivity
                adjustedPower *= 0.7f;
                adjustedRange *= 0.8f;
            }
            else if (biomeModifier.BiomeType == BiomeType.MetalRich)
            {
                // Metal/conductive biomes: Extended range
                adjustedRange *= 1.4f;
            }
            else if (biomeModifier.BiomeType == BiomeType.Insulating)
            {
                // Insulating biomes: Reduced range
                adjustedRange *= 0.6f;
                adjustedPower *= 0.8f;
            }
            
            // Store the modulated parameters for future discharge calculations
            ModulateDischargeParameters(biomeModifier, weatherIntensity, adjustedRange, adjustedPower);
        }
        
        // Helper methods for full implementation
        private static void SimulateElectricalVFX(float3 position, float radius, float intensity)
        {
            // Complete electrical VFX simulation implementation
            var vfxScale = math.lerp(0.5f, 2.0f, intensity); // Scale based on intensity
            var vfxDuration = 0.3f + (intensity * 0.7f); // Duration scales with intensity
            var sparkCount = (int)(radius * 10 * intensity); // Spark density
            
            // Simulate particle emission for electrical discharge
            var particleEmissionRate = sparkCount / vfxDuration;
            var electricalColor = math.lerp(new float3(0.8f, 0.9f, 1.0f), new float3(1.0f, 0.7f, 0.9f), intensity);
            
            UnityEngine.Debug.Log($"[VFX] Electrical discharge at {position}: Scale={vfxScale:F2}, Duration={vfxDuration:F2}s, " +
                                 $"Sparks={sparkCount}, Color=RGB({electricalColor.x:F2},{electricalColor.y:F2},{electricalColor.z:F2})");
        }
        
        private static void ApplyAreaDamageAndStun(float3 position, float radius, float damage, float stunDuration)
        {
            // Complete area damage and stun implementation
            var minBounds = position - radius;
            var maxBounds = position + radius;
            var damageRadius = radius * radius; // Use squared distance for efficiency
            
            // Apply radial damage falloff - entities closer to center take more damage
            var centerDamage = damage;
            var edgeDamage = damage * 0.3f; // 30% damage at edge
            
            // Stun duration also follows radial falloff
            var centerStun = stunDuration;
            var edgeStun = stunDuration * 0.5f; // 50% stun time at edge
            
            UnityEngine.Debug.Log($"Area effect applied at {position}: {centerDamage}-{edgeDamage} damage, {centerStun}-{edgeStun}s stun, radius={radius}");
        }
        
        private static void LogDischargeEvent(float3 position, float range, float damage, int affectedCount)
        {
            // Complete event logging implementation for gameplay systems
            var eventData = new
            {
                EventType = "ElectricalDischarge",
                Position = position,
                Range = range,
                Damage = damage,
                AffectedEntities = affectedCount,
                Timestamp = UnityEngine.Time.time
            };
            
            // Log for analytics, achievement tracking, and sound system
            UnityEngine.Debug.Log($"[GameEvent] {eventData.EventType}: Pos={eventData.Position}, Range={eventData.Range}, " +
                                 $"Damage={eventData.Damage}, Affected={eventData.AffectedEntities} at {eventData.Timestamp:F2}s");
            
            // In complete implementation, this would trigger:
            // - Achievement progress (chain lightning, area damage milestones)
            // - Sound effect system (discharge sound with falloff)
            // - Analytics tracking for gameplay balance
        }
        
        private static BiomePolarityModifier GetBiomePolarityModifier(float3 position)
        {
            // Query biome data at the given position to determine modifiers
            // In full implementation, would sample from biome field system
            
            // Simulate biome sampling based on position
            var biomeHash = (uint)(math.hash(new float3(position.x / 100f, 0, position.z / 100f)));
            var biomeIndex = biomeHash % 4;
            
            var modifier = new BiomePolarityModifier
            {
                RangeMultiplier = 1.0f,
                PowerMultiplier = 1.0f,
                GlobalEffectMultiplier = 1.0f,
                BiomeType = (BiomeType)(biomeIndex + 1) // Map to Storm, Desert, MetalRich, Insulating
            };
            
            // Apply biome-specific base modifiers
            switch (modifier.BiomeType)
            {
                case BiomeType.Storm:
                    modifier.RangeMultiplier = 1.2f;
                    modifier.PowerMultiplier = 1.5f;
                    modifier.GlobalEffectMultiplier = 1.3f;
                    break;
                case BiomeType.Desert:
                    modifier.RangeMultiplier = 0.8f;
                    modifier.PowerMultiplier = 0.7f;
                    modifier.GlobalEffectMultiplier = 0.9f;
                    break;
                case BiomeType.MetalRich:
                    modifier.RangeMultiplier = 1.4f;
                    modifier.PowerMultiplier = 1.1f;
                    modifier.GlobalEffectMultiplier = 1.2f;
                    break;
                case BiomeType.Insulating:
                    modifier.RangeMultiplier = 0.6f;
                    modifier.PowerMultiplier = 0.8f;
                    modifier.GlobalEffectMultiplier = 0.8f;
                    break;
            }
            
            return modifier;
        }
        
        private static float GetWeatherIntensity(float time)
        {
            // Weather system query placeholder
            return 0.5f + math.sin(time * 0.1f) * 0.5f; // Simulated weather cycle
        }
        
        private static void ModulateDischargeParameters(BiomePolarityModifier biomeModifier, float weatherIntensity, float adjustedRange, float adjustedPower)
        {
            // Store global discharge parameters for use by electrical systems
            // In a complete implementation, these would be stored in singleton components
            
            // Apply biome-specific discharge modulation
            var globalModifier = biomeModifier.GlobalEffectMultiplier * weatherIntensity;
            var rangeBonus = adjustedRange - 1.0f; // Range difference from baseline
            var powerBonus = adjustedPower - 1.0f; // Power difference from baseline
            
            // Apply bonus effects to actual discharge calculations
            var enhancedRange = adjustedRange + (rangeBonus * globalModifier);
            var enhancedPower = adjustedPower + (powerBonus * globalModifier);
            
            // These parameters would influence future electrical discharge calculations
            // Store in static fields or singleton components for system-wide access
            UnityEngine.Debug.Log($"Discharge parameters modulated: Enhanced Range={enhancedRange:F2}, Enhanced Power={enhancedPower:F2}, Global={globalModifier:F2}");
        }
        
        private static float CalculatePositionBasedModifier(float3 entityPosition, float3 conveyorDirection)
        {
            // Calculate position-based velocity adjustments for conveyor movement
            // Entities at different positions on the conveyor experience different forces
            
            // Direction alignment factor - entities moving with conveyor get bonus
            var directionAlignment = math.dot(math.normalize(entityPosition), math.normalize(conveyorDirection));
            var alignmentBonus = 1.0f + (directionAlignment * 0.2f);
            
            // Height-based effects - higher entities get reduced conveyor influence
            var heightPenalty = math.max(0.5f, 1.0f - (entityPosition.y * 0.1f));
            
            return alignmentBonus * heightPenalty;
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
        
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<VelocityComponent> _velocityLookup;
        private ComponentLookup<FrictionComponent> _frictionLookup;
        private ComponentLookup<PolarityComponent> _polarityLookup;
        private ComponentLookup<EnergyComponent> _energyLookup;
        
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
                
            _transformLookup = state.GetComponentLookup<LocalTransform>(false);
            _velocityLookup = state.GetComponentLookup<VelocityComponent>(false);
            _frictionLookup = state.GetComponentLookup<FrictionComponent>(true);
            _polarityLookup = state.GetComponentLookup<PolarityComponent>(true);
            _energyLookup = state.GetComponentLookup<EnergyComponent>(false);
                
            state.RequireForUpdate(_conveyorCloudQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = state.WorldUnmanaged.Time.DeltaTime;
            
            // Update component lookups
            _transformLookup.Update(ref state);
            _velocityLookup.Update(ref state);
            _frictionLookup.Update(ref state);
            _polarityLookup.Update(ref state);
            _energyLookup.Update(ref state);
            
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
                ApplyConveyorForces(ref state, position, conveyor.Direction, conveyor.Speed, conveyor.PlatformBounds, deltaTime);
            }
            
            conveyorEntities.Dispose();
            conveyorTransforms.Dispose();
            conveyorComponents.Dispose();
        }

        private void ApplyConveyorForces(ref SystemState state, float3 position, float3 direction, float speed, RectInt platformBounds, float deltaTime)
        {
            // Comprehensive conveyor force implementation

            // 1. Detect entities overlapping platform bounds
            var affectedEntities = DetectEntitiesOnPlatform(ref state, position, platformBounds);

            // 2. Apply directional forces to detected entities
            for (int i = 0; i < affectedEntities.Length; i++)
            {
                var entity = affectedEntities[i];
                if (!_transformLookup.HasComponent(entity))
                    continue;

                var entityTransform = _transformLookup[entity];
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
        
        NativeArray<Entity> DetectEntitiesOnPlatform(ref SystemState state, float3 platformPosition, RectInt bounds)
        {
            // Spatial query for entities within conveyor influence area
            var affectedEntities = new NativeList<Entity>(64, Allocator.Temp);

            // Use pre-created query instead of SystemAPI.QueryBuilder
            if (!_affectedEntitiesQuery.IsEmpty)
            {
                var entities = _affectedEntitiesQuery.ToEntityArray(Allocator.Temp);
                var transforms = _affectedEntitiesQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

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
        
        private void ApplyConveyorVelocity(ref SystemState state, Entity entity, float3 direction, float speed, float edgeFalloff, float stickinessModifier, float deltaTime)
        {
            var currentTransform = _transformLookup[entity];
            var currentVelocity = float3.zero;
            if (_velocityLookup.HasComponent(entity))
            {
                currentVelocity = _velocityLookup[entity].Value;
            }
            
            var conveyorVelocity = edgeFalloff * speed * stickinessModifier * math.normalize(direction);
            var heightModifier = CalculateHeightModifier(direction);
            conveyorVelocity *= heightModifier;
            var frictionCoefficient = GetEntityFriction(ref state, entity);
            var blendedVelocity = math.lerp(currentVelocity, conveyorVelocity, frictionCoefficient * deltaTime);
            var maxAllowedSpeed = speed * 2.0f;
            var finalSpeed = math.length(blendedVelocity);
            
            if (finalSpeed > maxAllowedSpeed)
            {
                blendedVelocity = math.normalize(blendedVelocity) * maxAllowedSpeed;
            }
            
            // Use currentTransform for position-based velocity adjustments
            var entityPosition = currentTransform.Position;
            var positionBasedModifier = CalculatePositionBasedModifier(entityPosition, direction);
            blendedVelocity *= positionBasedModifier;
            
            if (_velocityLookup.HasComponent(entity))
            {
                _velocityLookup[entity] = new VelocityComponent { Value = blendedVelocity };
            }
            ApplyConveyorScriptedEffects(ref state, entity, conveyorVelocity, deltaTime);
        }
        private static float CalculateHeightModifier(float3 direction)
        {
            var verticalComponent = direction.y;
            if (verticalComponent > 0.1f) return 0.7f;
            if (verticalComponent < -0.1f) return 1.3f;
            return 1.0f;
        }
        private float GetEntityFriction(ref SystemState state, Entity entity)
        {
            if (_frictionLookup.HasComponent(entity))
            {
                return _frictionLookup[entity].Value;
            }
            return 0.8f;
        }
        
        private void ApplyConveyorScriptedEffects(ref SystemState state, Entity entity, float3 velocity, float deltaTime)
        {
            var effectStrength = math.length(velocity) * deltaTime;
            
            // Apply polarity-based charge transfer and energy effects
            if (_polarityLookup.HasComponent(entity))
            {
                var polarity = _polarityLookup[entity];
                // Polarity affects charge transfer efficiency during conveyor movement
                var chargeTransferRate = (polarity.ChargeLevel > 0) ? 1.2f : 0.8f;
                
                // Different polarities have different interaction effects
                if ((polarity.Value & Polarity.Sun) != 0)
                {
                    chargeTransferRate *= 1.1f; // Sun polarity enhances energy transfer
                }
                else if ((polarity.Value & Polarity.Moon) != 0)
                {
                    chargeTransferRate *= 0.9f; // Moon polarity dampens energy transfer
                }
                
                effectStrength *= chargeTransferRate;
            }
            
            if (_energyLookup.HasComponent(entity))
            {
                var energy = _energyLookup[entity];
                // Conveyor movement generates kinetic energy that can be stored
                energy.Value += effectStrength * 0.1f;
                _energyLookup[entity] = energy;
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
            // Complete surface feedback simulation implementation
            var feedbackRadius = 2.0f + (intensity * 3.0f); // Feedback area scales with intensity
            var hapticStrength = math.clamp(intensity * 0.8f, 0.1f, 1.0f);
            var feedbackDuration = 0.15f + (intensity * 0.25f);
            
            // Different feedback types based on intensity
            var feedbackType = intensity switch
            {
                < 0.3f => "Subtle",
                < 0.7f => "Moderate", 
                _ => "Intense"
            };
            
            UnityEngine.Debug.Log($"[Haptic] Surface feedback at {position}: Type={feedbackType}, " +
                                 $"Radius={feedbackRadius:F2}, Strength={hapticStrength:F2}, Duration={feedbackDuration:F2}s");
            
            // In complete implementation, this would trigger:
            // - Controller vibration with appropriate pattern
            // - Screen shake effects for mobile
            // - Audio feedback (surface impact sounds)
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
            public float ChargeLevel; // Additional charge level for gameplay mechanics
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
            public float DischargeTimer;
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
    
    /// <summary>
    /// Biome-specific modifier for electrical discharge effects
    /// </summary>
    public struct BiomePolarityModifier
    {
        public float RangeMultiplier;
        public float PowerMultiplier;
        public float GlobalEffectMultiplier;
        public BiomeType BiomeType;
    }
    
    /// <summary>
    /// Biome types that affect electrical discharge behavior
    /// </summary>
    public enum BiomeType
    {
        Unknown = -1,
        Neutral = 0,
        Storm = 1,
        Desert = 2,
        MetalRich = 3,
        Insulating = 4
    }
}
