# 🤖 Enemy Behavior Tutorial
## *Create Smart, Challenging Enemies*

> **"Enemies aren't just obstacles - they're the teachers that show players how to master your game's mechanics."**

[![Enemy AI](https://img.shields.io/badge/Enemy-AI-red.svg)](enemy-behavior.md)
[![Behavior Systems](https://img.shields.io/badge/Behavior-Systems-blue.svg)](enemy-behavior.md)

---

## 🎯 **What Makes Good Enemy AI?**

**Good enemy AI** creates challenge without frustration. Enemies should:

- 🎯 **React to Player Actions** - Change behavior based on what the player does
- 🧠 **Have Clear Patterns** - Players can learn and predict enemy behavior
- 📈 **Scale with Difficulty** - Harder enemies for experienced players
- 🎭 **Show Intent** - Players can see what enemies are about to do
- 🔄 **Recover from Mistakes** - Don't punish players forever for one error

**Let's create enemies that teach players your game!**

---

## 🏗️ **Core Enemy Components**

### **Basic Enemy Structure**

```csharp
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

// Core enemy data
public struct Enemy : IComponentData
{
    public EnemyType Type;
    public float Health;
    public float MaxHealth;
    public float Speed;
    public float DetectionRange;
    public float AttackRange;
    public float AttackCooldown;
    public float LastAttackTime;
    public Entity TargetEntity;
    public EnemyState CurrentState;
}

public enum EnemyType
{
    Patrol,
    Chase,
    Ambush,
    Turret,
    Swarm
}

public enum EnemyState
{
    Idle,
    Patrolling,
    Alert,
    Chasing,
    Attacking,
    Fleeing,
    Dead
}

// Enemy behavior configuration
public struct EnemyBehaviorConfig : IComponentData
{
    public float PatrolSpeed;
    public float ChaseSpeed;
    public float AlertDuration;
    public float AttackDamage;
    public float FleeHealthThreshold; // Flee when health drops below this %
    public bool CanFly;
    public bool CanClimb;
}
```

### **State Machine System**

```csharp
// Manages enemy state transitions
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class EnemyStateMachineSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        Entities.ForEach((Entity entity, ref Enemy enemy, ref EnemyBehaviorConfig config,
                         in LocalTransform transform) =>
        {
            // Update state based on current situation
            EnemyState newState = DetermineNextState(entity, enemy, config, transform);

            if (newState != enemy.CurrentState)
            {
                OnStateExit(enemy.CurrentState, entity);
                enemy.CurrentState = newState;
                OnStateEnter(newState, entity);
            }

            // Execute current state behavior
            ExecuteStateBehavior(entity, enemy, config, transform, deltaTime);

        }).Schedule();
    }

    private EnemyState DetermineNextState(Entity entity, Enemy enemy,
                                        EnemyBehaviorConfig config, LocalTransform transform)
    {
        // Check for player detection
        if (enemy.TargetEntity != Entity.Null)
        {
            float distanceToTarget = math.distance(transform.Position,
                GetComponent<LocalTransform>(enemy.TargetEntity).Position);

            // In attack range?
            if (distanceToTarget <= enemy.AttackRange)
            {
                return EnemyState.Attacking;
            }

            // In detection range?
            if (distanceToTarget <= enemy.DetectionRange)
            {
                return EnemyState.Chasing;
            }
        }

        // Low health - flee!
        if (enemy.Health / enemy.MaxHealth <= config.FleeHealthThreshold)
        {
            return EnemyState.Fleeing;
        }

        // Heard something suspicious?
        if (IsAlertTriggered(entity))
        {
            return EnemyState.Alert;
        }

        // Default to patrolling
        return EnemyState.Patrolling;
    }

    private void ExecuteStateBehavior(Entity entity, Enemy enemy, EnemyBehaviorConfig config,
                                    LocalTransform transform, float deltaTime)
    {
        switch (enemy.CurrentState)
        {
            case EnemyState.Patrolling:
                ExecutePatrolBehavior(entity, config, transform, deltaTime);
                break;

            case EnemyState.Chasing:
                ExecuteChaseBehavior(entity, enemy.TargetEntity, config, transform, deltaTime);
                break;

            case EnemyState.Attacking:
                ExecuteAttackBehavior(entity, enemy, config, transform, deltaTime);
                break;

            case EnemyState.Fleeing:
                ExecuteFleeBehavior(entity, enemy.TargetEntity, config, transform, deltaTime);
                break;
        }
    }

    // State transition callbacks
    private void OnStateEnter(EnemyState state, Entity entity)
    {
        // Play animation, sound effects, etc.
        switch (state)
        {
            case EnemyState.Alert:
                // Play alert sound, change color, etc.
                break;

            case EnemyState.Chasing:
                // Play chase music, increase speed, etc.
                break;
        }
    }

    private void OnStateExit(EnemyState state, Entity entity)
    {
        // Clean up state-specific effects
    }
}
```

---

## 🚀 **Build Your First Enemy (20 Minutes)**

### **Step 1: Basic Patrol Enemy**

```csharp
// Patrol between waypoints
public struct PatrolData : IComponentData
{
    public BlobArray<float3> Waypoints;
    public int CurrentWaypointIndex;
    public float WaitTimeAtWaypoint;
    public float CurrentWaitTime;
}

public partial class PatrolSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        Entities.ForEach((Entity entity, ref LocalTransform transform, ref PatrolData patrol,
                         in Enemy enemy, in EnemyBehaviorConfig config) =>
        {
            if (enemy.CurrentState != EnemyState.Patrolling) return;

            // Move towards current waypoint
            float3 targetPos = patrol.Waypoints[patrol.CurrentWaypointIndex];
            float3 direction = math.normalize(targetPos - transform.Position);
            float3 movement = direction * config.PatrolSpeed * deltaTime;

            transform.Position += movement;

            // Check if reached waypoint
            if (math.distance(transform.Position, targetPos) < 0.5f)
            {
                // Start waiting
                patrol.CurrentWaitTime += deltaTime;

                if (patrol.CurrentWaitTime >= patrol.WaitTimeAtWaypoint)
                {
                    // Move to next waypoint
                    patrol.CurrentWaypointIndex = (patrol.CurrentWaypointIndex + 1) % patrol.Waypoints.Length;
                    patrol.CurrentWaitTime = 0f;
                }
            }

        }).Schedule();
    }
}

// Setup patrol enemy
public class PatrolEnemyAuthoring : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waitTime = 2f;

    class Baker : Baker<PatrolEnemyAuthoring>
    {
        public override void Bake(PatrolEnemyAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Add enemy components
            AddComponent(entity, new Enemy
            {
                Type = EnemyType.Patrol,
                Health = 100f,
                MaxHealth = 100f,
                Speed = 2f,
                DetectionRange = 10f,
                AttackRange = 1f,
                CurrentState = EnemyState.Patrolling
            });

            AddComponent(entity, new EnemyBehaviorConfig
            {
                PatrolSpeed = 2f,
                ChaseSpeed = 4f,
                AlertDuration = 3f,
                AttackDamage = 10f,
                FleeHealthThreshold = 0.2f
            });

            // Create waypoint blob
            BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var patrolBlob = ref blobBuilder.ConstructRoot<PatrolData>();
            BlobBuilderArray<float3> waypointArray = blobBuilder.Allocate(ref patrolBlob.Waypoints, authoring.waypoints.Length);

            for (int i = 0; i < authoring.waypoints.Length; i++)
            {
                waypointArray[i] = authoring.waypoints[i].position;
            }

            AddBlobAsset(ref patrolBlob, out BlobAssetReference<PatrolData> patrolRef);

            AddComponent(entity, new PatrolData
            {
                Waypoints = patrolRef.Value.Waypoints,
                CurrentWaypointIndex = 0,
                WaitTimeAtWaypoint = authoring.waitTime,
                CurrentWaitTime = 0f
            });
        }
    }
}
```

### **Step 2: Add Chase Behavior**

```csharp
// Chase the player when detected
public partial class ChaseSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        Entities.ForEach((Entity entity, ref LocalTransform transform,
                         in Enemy enemy, in EnemyBehaviorConfig config) =>
        {
            if (enemy.CurrentState != EnemyState.Chasing) return;
            if (enemy.TargetEntity == Entity.Null) return;

            // Move towards target
            LocalTransform targetTransform = GetComponent<LocalTransform>(enemy.TargetEntity);
            float3 direction = math.normalize(targetTransform.Position - transform.Position);
            float3 movement = direction * config.ChaseSpeed * deltaTime;

            transform.Position += movement;

            // Face target
            transform.Rotation = quaternion.LookRotation(direction, math.up());

        }).Schedule();
    }
}

// Detection system
public partial class EnemyDetectionSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Find player entity (simplified - you'd use a proper player tag)
        Entity playerEntity = Entity.Null;
        Entities.ForEach((Entity entity, in Player player) =>
        {
            playerEntity = entity;
        }).Run();

        if (playerEntity == Entity.Null) return;

        Entities.ForEach((Entity entity, ref Enemy enemy, in LocalTransform transform) =>
        {
            if (enemy.TargetEntity != Entity.Null) return; // Already has target

            float distanceToPlayer = math.distance(transform.Position,
                GetComponent<LocalTransform>(playerEntity).Position);

            if (distanceToPlayer <= enemy.DetectionRange)
            {
                enemy.TargetEntity = playerEntity;
                // State machine will handle transition to chasing
            }

        }).Schedule();
    }
}
```

### **Step 3: Add Attack Behavior**

```csharp
// Attack when in range
public struct AttackData : IComponentData
{
    public float AttackDuration;
    public float CurrentAttackTime;
    public bool IsAttacking;
}

public partial class AttackSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        float currentTime = (float)SystemAPI.Time.ElapsedTime;

        Entities.ForEach((Entity entity, ref Enemy enemy, ref AttackData attack,
                         in EnemyBehaviorConfig config, in LocalTransform transform) =>
        {
            if (enemy.CurrentState != EnemyState.Attacking) return;

            // Check attack cooldown
            if (currentTime - enemy.LastAttackTime < enemy.AttackCooldown) return;

            // Start attack
            if (!attack.IsAttacking)
            {
                attack.IsAttacking = true;
                attack.CurrentAttackTime = 0f;
                // Play attack animation/sound here
            }

            // Update attack
            attack.CurrentAttackTime += deltaTime;

            if (attack.CurrentAttackTime >= attack.AttackDuration)
            {
                // Attack finished - deal damage
                DealDamageToTarget(enemy.TargetEntity, config.AttackDamage);

                // Reset attack
                attack.IsAttacking = false;
                enemy.LastAttackTime = currentTime;

                // Return to chasing
                enemy.CurrentState = EnemyState.Chasing;
            }

        }).Schedule();
    }

    private void DealDamageToTarget(Entity target, float damage)
    {
        // Add damage to target's health component
        if (EntityManager.HasComponent<Health>(target))
        {
            Health targetHealth = EntityManager.GetComponentData<Health>(target);
            targetHealth.Current -= damage;
            EntityManager.SetComponentData(target, targetHealth);
        }
    }
}
```

### **Step 4: Add Flee Behavior**

```csharp
// Run away when hurt
public partial class FleeSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        Entities.ForEach((Entity entity, ref LocalTransform transform,
                         in Enemy enemy, in EnemyBehaviorConfig config) =>
        {
            if (enemy.CurrentState != EnemyState.Fleeing) return;
            if (enemy.TargetEntity == Entity.Null) return;

            // Move away from target
            LocalTransform targetTransform = GetComponent<LocalTransform>(enemy.TargetEntity);
            float3 direction = math.normalize(transform.Position - targetTransform.Position);
            float3 movement = direction * config.ChaseSpeed * deltaTime; // Use chase speed for fleeing

            transform.Position += movement;

            // Face away from target
            transform.Rotation = quaternion.LookRotation(direction, math.up());

        }).Schedule();
    }
}
```

---

## 🎨 **Advanced Enemy Behaviors**

### **Ambush Enemy**

```csharp
// Hide and surprise attack
public struct AmbushData : IComponentData
{
    public float3 HidePosition;
    public float AmbushRange;
    public bool IsHidden;
    public float UnhideDelay;
    public float CurrentUnhideTime;
}

public partial class AmbushSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        Entities.ForEach((Entity entity, ref AmbushData ambush, ref Enemy enemy,
                         ref LocalTransform transform) =>
        {
            if (enemy.CurrentState != EnemyState.Idle) return;

            // Check if player is in ambush range
            if (enemy.TargetEntity != Entity.Null)
            {
                float distanceToTarget = math.distance(transform.Position,
                    GetComponent<LocalTransform>(enemy.TargetEntity).Position);

                if (distanceToTarget <= ambush.AmbushRange)
                {
                    // Spring the ambush!
                    ambush.IsHidden = false;
                    enemy.CurrentState = EnemyState.Chasing;

                    // Play ambush sound/animation
                    TriggerAmbushEffect(entity);
                }
            }

            // Handle unhide delay after ambush
            if (!ambush.IsHidden && ambush.UnhideDelay > 0)
            {
                ambush.CurrentUnhideTime += deltaTime;

                if (ambush.CurrentUnhideTime >= ambush.UnhideDelay)
                {
                    ambush.IsHidden = true;
                    ambush.CurrentUnhideTime = 0f;
                    enemy.CurrentState = EnemyState.Idle;
                }
            }

        }).Schedule();
    }

    private void TriggerAmbushEffect(Entity entity)
    {
        // Play sound, spawn particles, etc.
        // You could also trigger camera shake or screen effects
    }
}
```

### **Swarm Enemy**

```csharp
// Coordinate with other enemies
public struct SwarmData : IComponentData
{
    public Entity SwarmLeader;
    public float SwarmRadius;
    public float SeparationDistance;
    public float CohesionStrength;
    public float AlignmentStrength;
}

public partial class SwarmSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // Calculate swarm behavior for each enemy
        Entities.ForEach((Entity entity, ref SwarmData swarm, ref LocalTransform transform,
                         in Enemy enemy, in EnemyBehaviorConfig config) =>
        {
            if (enemy.CurrentState != EnemyState.Chasing) return;

            // Boid-like swarm behavior
            float3 separation = CalculateSeparation(entity, swarm);
            float3 cohesion = CalculateCohesion(entity, swarm);
            float3 alignment = CalculateAlignment(entity, swarm);

            // Combine forces
            float3 swarmForce = separation + cohesion * swarm.CohesionStrength +
                              alignment * swarm.AlignmentStrength;

            // Apply movement
            float3 movement = swarmForce * config.ChaseSpeed * deltaTime;
            transform.Position += movement;

        }).Schedule();
    }

    private float3 CalculateSeparation(Entity entity, SwarmData swarm)
    {
        float3 separationForce = float3.zero;
        int neighborCount = 0;

        // Find nearby swarm members
        Entities.ForEach((Entity otherEntity, in SwarmData otherSwarm, in LocalTransform otherTransform) =>
        {
            if (otherEntity == entity) return;
            if (otherSwarm.SwarmLeader != swarm.SwarmLeader) return;

            float3 toOther = otherTransform.Position - GetComponent<LocalTransform>(entity).Position;
            float distance = math.length(toOther);

            if (distance < swarm.SeparationDistance && distance > 0)
            {
                // Push away from nearby enemies
                separationForce -= math.normalize(toOther) / distance;
                neighborCount++;
            }

        }).Run();

        return neighborCount > 0 ? separationForce / neighborCount : float3.zero;
    }

    private float3 CalculateCohesion(Entity entity, SwarmData swarm)
    {
        float3 centerOfMass = float3.zero;
        int neighborCount = 0;

        // Find swarm center
        Entities.ForEach((Entity otherEntity, in SwarmData otherSwarm, in LocalTransform otherTransform) =>
        {
            if (otherEntity == entity) return;
            if (otherSwarm.SwarmLeader != swarm.SwarmLeader) return;

            centerOfMass += otherTransform.Position;
            neighborCount++;

        }).Run();

        if (neighborCount == 0) return float3.zero;

        centerOfMass /= neighborCount;
        return math.normalize(centerOfMass - GetComponent<LocalTransform>(entity).Position);
    }

    private float3 CalculateAlignment(Entity entity, SwarmData swarm)
    {
        float3 averageVelocity = float3.zero;
        int neighborCount = 0;

        // Average velocity of nearby swarm members
        Entities.ForEach((Entity otherEntity, in SwarmData otherSwarm, in LocalTransform otherTransform) =>
        {
            if (otherEntity == entity) return;
            if (otherSwarm.SwarmLeader != swarm.SwarmLeader) return;

            // You'd need velocity component for proper alignment
            // For now, use position difference as approximation
            averageVelocity += otherTransform.Position - GetComponent<LocalTransform>(entity).Position;
            neighborCount++;

        }).Run();

        return neighborCount > 0 ? math.normalize(averageVelocity / neighborCount) : float3.zero;
    }
}
```

### **Turret Enemy**

```csharp
// Stationary but powerful
public struct TurretData : IComponentData
{
    public float RotationSpeed;
    public float FireRate;
    public float LastFireTime;
    public float ProjectileSpeed;
    public Entity ProjectilePrefab;
}

public partial class TurretSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        float currentTime = (float)SystemAPI.Time.ElapsedTime;

        Entities.ForEach((Entity entity, ref TurretData turret, ref Enemy enemy,
                         ref LocalTransform transform) =>
        {
            if (enemy.TargetEntity == Entity.Null) return;

            LocalTransform targetTransform = GetComponent<LocalTransform>(enemy.TargetEntity);
            float3 toTarget = targetTransform.Position - transform.Position;
            float distanceToTarget = math.length(toTarget);

            // Rotate towards target
            quaternion targetRotation = quaternion.LookRotation(math.normalize(toTarget), math.up());
            transform.Rotation = math.slerp(transform.Rotation, targetRotation,
                                          turret.RotationSpeed * deltaTime);

            // Fire if in range and cooldown ready
            if (distanceToTarget <= enemy.AttackRange &&
                currentTime - turret.LastFireTime >= 1f / turret.FireRate)
            {
                FireProjectile(entity, turret, transform.Position, math.normalize(toTarget));
                turret.LastFireTime = currentTime;
            }

        }).Schedule();
    }

    private void FireProjectile(Entity turretEntity, TurretData turret, float3 position, float3 direction)
    {
        // Instantiate projectile entity
        Entity projectileEntity = EntityManager.Instantiate(turret.ProjectilePrefab);

        // Set projectile position and velocity
        EntityManager.SetComponentData(projectileEntity, LocalTransform.FromPosition(position));

        // Add velocity component (assuming you have one)
        EntityManager.AddComponentData(projectileEntity, new Velocity
        {
            Value = direction * turret.ProjectileSpeed
        });
    }
}
```

---

## 🎮 **Enemy Difficulty Scaling**

### **Adaptive Difficulty**

```csharp
// Scale enemy behavior based on player performance
public struct DifficultyScaling : IComponentData
{
    public float PlayerSkillLevel; // 0-1 based on performance
    public float HealthMultiplier;
    public float SpeedMultiplier;
    public float DamageMultiplier;
    public float DetectionRangeMultiplier;
}

public partial class DifficultyScalingSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Calculate player skill level based on various metrics
        float playerSkill = CalculatePlayerSkillLevel();

        Entities.ForEach((Entity entity, ref Enemy enemy, ref EnemyBehaviorConfig config,
                         ref DifficultyScaling scaling) =>
        {
            // Update scaling based on current skill assessment
            scaling.PlayerSkillLevel = playerSkill;

            // Scale enemy stats
            scaling.HealthMultiplier = 1f + (playerSkill * 0.5f); // +50% health at max skill
            scaling.SpeedMultiplier = 1f + (playerSkill * 0.3f);  // +30% speed at max skill
            scaling.DamageMultiplier = 1f + (playerSkill * 0.4f); // +40% damage at max skill
            scaling.DetectionRangeMultiplier = 1f + (playerSkill * 0.2f); // +20% detection

            // Apply scaling to enemy
            enemy.MaxHealth = 100f * scaling.HealthMultiplier;
            enemy.Health = math.min(enemy.Health, enemy.MaxHealth); // Don't kill with scaling
            enemy.Speed = 2f * scaling.SpeedMultiplier;
            enemy.DetectionRange = 10f * scaling.DetectionRangeMultiplier;

            config.AttackDamage = 10f * scaling.DamageMultiplier;

        }).Schedule();
    }

    private float CalculatePlayerSkillLevel()
    {
        // Assess player skill based on:
        // - Time survived
        // - Enemies defeated
        // - Damage taken
        // - Items collected
        // - etc.

        // Simplified example
        float skillLevel = 0f;

        // Check player stats (you'd have a player stats system)
        Entities.ForEach((in PlayerStats stats) =>
        {
            skillLevel = math.clamp(stats.KillCount / 50f, 0f, 1f); // Max out at 50 kills
        }).Run();

        return skillLevel;
    }
}
```

### **Behavior Unlocks**

```csharp
// Unlock new enemy behaviors as player progresses
public struct BehaviorUnlocks : IComponentData
{
    public bool CanUseSpecialAttacks;
    public bool CanCallReinforcements;
    public bool CanUseEnvironmentalHazards;
    public bool CanPredictPlayerMovement;
}

public partial class BehaviorUnlockSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Check player progress
        bool hasAdvanced = CheckPlayerProgress();

        Entities.ForEach((Entity entity, ref BehaviorUnlocks unlocks, ref Enemy enemy) =>
        {
            unlocks.CanUseSpecialAttacks = hasAdvanced;
            unlocks.CanCallReinforcements = hasAdvanced;
            unlocks.CanUseEnvironmentalHazards = hasAdvanced;
            unlocks.CanPredictPlayerMovement = hasAdvanced;

            // Modify behavior based on unlocks
            if (unlocks.CanPredictPlayerMovement && enemy.CurrentState == EnemyState.Chasing)
            {
                // Predict where player will be and move there instead
                PredictAndIntercept(entity, enemy.TargetEntity);
            }

        }).Schedule();
    }

    private bool CheckPlayerProgress()
    {
        // Check if player has reached certain milestones
        // - Defeated boss
        // - Collected certain items
        // - Reached certain level
        // etc.

        return false; // Implement based on your game's progression system
    }

    private void PredictAndIntercept(Entity enemyEntity, Entity targetEntity)
    {
        // Simple prediction: assume player continues in current direction
        // In a real game, you'd use more sophisticated prediction

        if (EntityManager.HasComponent<Velocity>(targetEntity))
        {
            Velocity playerVelocity = EntityManager.GetComponentData<Velocity>(targetEntity);
            LocalTransform enemyTransform = EntityManager.GetComponentData<LocalTransform>(enemyEntity);
            LocalTransform targetTransform = EntityManager.GetComponentData<LocalTransform>(targetEntity);

            // Predict future position
            float predictionTime = 1f; // Predict 1 second ahead
            float3 predictedPosition = targetTransform.Position + playerVelocity.Value * predictionTime;

            // Move towards predicted position
            float3 direction = math.normalize(predictedPosition - enemyTransform.Position);
            // Apply movement (would be handled by movement system)
        }
    }
}
```

---

## 🎯 **Testing Enemy Behavior**

### **Enemy Behavior Tests**

```csharp
// Unit tests for enemy behavior
[TestFixture]
public class EnemyBehaviorTests
{
    [Test]
    public void PatrolEnemy_ReachesWaypoint_ChangesTarget()
    {
        // Setup patrol enemy
        var enemyEntity = CreatePatrolEnemy();

        // Move to first waypoint
        // Assert enemy moves towards waypoint

        // Reach waypoint
        // Assert enemy waits, then changes to next waypoint
    }

    [Test]
    public void ChaseEnemy_DetectsPlayer_EntersChaseState()
    {
        // Setup enemy and player
        var enemyEntity = CreateChaseEnemy();
        var playerEntity = CreatePlayer();

        // Move player into detection range
        // Assert enemy detects player and enters chase state
    }

    [Test]
    public void SwarmEnemies_MaintainFormation_AvoidCollision()
    {
        // Setup swarm of enemies
        var swarmEntities = CreateSwarmEnemies(5);

        // Assert enemies maintain separation distance
        // Assert enemies move as group towards target
    }

    [Test]
    public void DifficultyScaling_IncreasesWithPlayerSkill()
    {
        // Setup enemy with scaling
        var enemyEntity = CreateScalingEnemy();

        // Simulate low player skill
        // Assert enemy has base stats

        // Simulate high player skill
        // Assert enemy stats are increased
    }
}
```

### **Playtesting Checklist**

- [ ] **Patrol Patterns**: Do enemies follow logical patrol routes?
- [ ] **Detection**: Do enemies notice player at appropriate distances?
- [ ] **Chase Behavior**: Do enemies pursue player effectively?
- [ ] **Attack Timing**: Do enemies attack with good timing?
- [ ] **Flee Behavior**: Do enemies retreat when appropriate?
- [ ] **Difficulty Balance**: Are enemies challenging but fair?
- [ ] **Visual Feedback**: Can players tell what enemies will do next?
- [ ] **Recovery**: Can players recover from enemy encounters?

---

## 🚀 **Next Steps**

**Ready to create more enemies?**
- **[Navigation Systems](../ai-gameplay/navigation.md)** - Advanced pathfinding for enemies
- **[Player Systems](../ai-gameplay/player.md)** - Create player characters to test against
- **[Biome Integration](../building-worlds/biomes.md)** - Make enemies fit their environment

**Need enemy inspiration?**
- Study [classic games](../../tutorials/) for enemy behavior patterns
- Check [enemy prefabs](../../Assets/Prefabs/Enemies/) for examples
- Read [AI game design books](https://www.gamedeveloper.com/design/game-ai-pro-tips) for advanced techniques

---

*"Great enemies don't just fight players - they teach them how to play your game better."*

**🍑 Happy Enemy Creating! 🍑**
