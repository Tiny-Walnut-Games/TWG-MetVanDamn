# 🧬 ECS Architecture Deep Dive
## *Understanding Unity's DOTS Entity Component System*

> **"ECS isn't just a performance optimization - it's a fundamental shift in how you think about game architecture."**

[![ECS](https://img.shields.io/badge/ECS-Architecture-blue.svg)](ecs-architecture.md)
[![DOTS 1.2.0](https://img.shields.io/badge/DOTS-1.2.0-blue.svg)](https://docs.unity3d.com/Packages/com.unity.entities@1.2/)

---

## 🎯 **What is ECS?**

**ECS (Entity Component System)** is Unity's high-performance architecture that replaces traditional GameObject/MonoBehaviour patterns. Instead of objects with behaviors, ECS uses:

- **Entities** - Simple IDs representing game objects
- **Components** - Pure data structures
- **Systems** - Logic that operates on components

**Why ECS?** Traditional Unity struggles with thousands of objects. ECS can handle millions!

---

## 🏗️ **ECS vs Traditional Unity**

### **Traditional Unity (GameObject/MonoBehaviour)**

```csharp
// Traditional approach - everything in one script
public class Enemy : MonoBehaviour
{
    public float health = 100f;
    public float speed = 5f;
    public Vector3 targetPosition;

    void Update()
    {
        // Movement logic
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        // Health logic
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
}
```

**Problems:**
- ❌ **Tight Coupling** - Movement and health logic mixed together
- ❌ **Cache Misses** - Data scattered in memory
- ❌ **Overhead** - Each GameObject has Unity overhead
- ❌ **Scalability** - Performance drops with many objects

### **ECS Approach**

```csharp
// ECS Components - Pure data
public struct Health : IComponentData
{
    public float Value;
    public float MaxValue;
}

public struct Movement : IComponentData
{
    public float Speed;
    public float3 TargetPosition;
}

public struct EnemyTag : IComponentData { }

// ECS Systems - Pure logic
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class MovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // Process all entities with Movement component
        foreach (var (transform, movement) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Movement>>())
        {
            transform.ValueRW.Position = math.lerp(
                transform.ValueRW.Position,
                movement.ValueRO.TargetPosition,
                movement.ValueRO.Speed * deltaTime
            );
        }
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class HealthSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Process all entities with Health component
        foreach (var health in SystemAPI.Query<RefRW<Health>>())
        {
            if (health.ValueRW.Value <= 0)
            {
                // Mark for destruction (handled by another system)
                health.ValueRW.Value = -1;
            }
        }
    }
}
```

**Benefits:**
- ✅ **Separation of Concerns** - Data and logic are separate
- ✅ **Memory Efficiency** - Data is contiguous in memory
- ✅ **Parallel Processing** - Systems can run in parallel
- ✅ **Scalability** - Performance stays consistent with more entities

---

## 🚀 **Getting Started with ECS (10 Minutes)**

### **Step 1: Install DOTS Packages**

1. Open **Package Manager** (Window > Package Manager)
2. Select **Unity Registry**
3. Install:
   - **Entities** (1.2.0+)
   - **Entities Graphics** (1.2.0+)
   - **Unity Physics** (1.2.0+)

### **Step 2: Create Your First Entity**

```csharp
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

public class ECSTest : MonoBehaviour
{
    void Start()
    {
        // Get the entity manager
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // Create a simple entity
        Entity entity = entityManager.CreateEntity();

        // Add components
        entityManager.AddComponentData(entity, new LocalTransform
        {
            Position = new float3(0, 0, 0),
            Rotation = quaternion.identity,
            Scale = 1f
        });

        entityManager.AddComponentData(entity, new Health
        {
            Value = 100f,
            MaxValue = 100f
        });

        Debug.Log("Created ECS entity!");
    }
}
```

### **Step 3: Create a System**

```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct HealthRegenSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // Regenerate health over time
        foreach (var health in SystemAPI.Query<RefRW<Health>>())
        {
            if (health.ValueRW.Value < health.ValueRW.MaxValue)
            {
                health.ValueRW.Value += 10f * deltaTime; // 10 health per second
                health.ValueRW.Value = math.min(health.ValueRW.Value, health.ValueRW.MaxValue);
            }
        }
    }
}
```

### **Step 4: Register the System**

```csharp
// In a bootstrap script or scene setup
public class BootstrapECS : MonoBehaviour
{
    void Start()
    {
        // Systems are automatically discovered and added
        // But you can manually control them if needed
        DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(
            World.DefaultGameObjectInjectionWorld,
            typeof(HealthRegenSystem)
        );
    }
}
```

---

## 🎮 **Core ECS Concepts**

### **Entities**

**Entities** are just IDs - they don't contain data or logic. Think of them as rows in a database.

```csharp
// Creating entities
EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;

// Single entity
Entity entity = em.CreateEntity();

// Multiple entities
NativeArray<Entity> entities = em.CreateEntity(
    archetype,  // EntityArchetype
    1000        // count
);

// Entity with specific components
EntityArchetype archetype = em.CreateArchetype(
    typeof(LocalTransform),
    typeof(Health),
    typeof(Movement)
);
```

### **Components**

**Components** are pure data structures. They should contain NO logic - just data.

```csharp
// Good component - pure data
[GenerateAuthoringComponent]
public struct PlayerInput : IComponentData
{
    public float2 MoveInput;
    public bool JumpPressed;
    public bool AttackPressed;
}

// Bad component - has logic (don't do this!)
public struct BadComponent : IComponentData
{
    public float Health;
    public void TakeDamage(float damage) // ❌ Logic in component!
    {
        Health -= damage;
    }
}
```

### **Systems**

**Systems** contain all the logic. They process entities that have specific components.

```csharp
// System that processes entities with specific components
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class MovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // Query for entities with both components
        foreach (var (transform, velocity) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRO<Velocity>>())
        {
            // Update position based on velocity
            transform.ValueRW.Position += velocity.ValueRO.Value * deltaTime;
        }
    }
}
```

---

## 🔧 **Advanced ECS Patterns**

### **Entity Command Buffer (ECB)**

Use ECB for deferred operations (creating/destroying entities during system updates).

```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class DamageSystem : SystemBase
{
    private EntityCommandBufferSystem ecbSystem;

    protected override void OnCreate()
    {
        ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = ecbSystem.CreateCommandBuffer();

        // Process damage
        foreach (var (health, entity) in SystemAPI.Query<RefRW<Health>>().WithEntityAccess())
        {
            if (health.ValueRW.Value <= 0)
            {
                // Destroy entity next frame (safe during iteration)
                ecb.DestroyEntity(entity);
            }
        }
    }
}
```

### **Jobs and Burst Compilation**

Use Jobs for parallel processing and Burst for maximum performance.

```csharp
[BurstCompile]
public partial struct ParallelMovementJob : IJobEntity
{
    public float DeltaTime;

    [BurstCompile]
    public void Execute(RefRW<LocalTransform> transform, in Velocity velocity)
    {
        transform.ValueRW.Position += velocity.Value * DeltaTime;
    }
}

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct MovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Schedule parallel job
        new ParallelMovementJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime
        }.ScheduleParallel();
    }
}
```

### **Aspects (Unity 6.0+)**

Aspects group related components for cleaner code.

```csharp
public readonly partial struct PlayerAspect : IAspect
{
    // Required components
    public readonly RefRW<LocalTransform> Transform;
    public readonly RefRW<Health> Health;
    public readonly RefRO<PlayerInput> Input;

    // Optional components
    public readonly RefRW<Velocity> Velocity;

    // Computed properties
    public float3 Position => Transform.ValueRO.Position;
    public bool IsAlive => Health.ValueRO.Value > 0;
}

// Use in systems
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class PlayerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var player in SystemAPI.Query<PlayerAspect>())
        {
            if (player.IsAlive)
            {
                // Process player logic
                player.Transform.ValueRW.Position += new float3(player.Input.ValueRO.MoveInput.x, 0, player.Input.ValueRO.MoveInput.y);
            }
        }
    }
}
```

---

## 🎯 **ECS in MetVanDAMN**

### **How MetVanDAMN Uses ECS**

MetVanDAMN uses ECS extensively for performance:

```csharp
// World generation entities
public struct WorldConfiguration : IComponentData
{
    public int Seed;
    public int2 Size;
    public float BiomeTransitionRadius;
}

public struct DistrictData : IComponentData
{
    public Entity DistrictEntity;
    public int2 Position;
    public DistrictType Type;
}

// Systems for world generation
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class WorldGenerationSystem : SystemBase
{
    // Generates districts, rooms, connections
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class BiomeApplicationSystem : SystemBase
{
    // Applies biome effects to generated world
}
```

### **Performance Benefits**

- **1000+ Districts**: ECS handles large worlds smoothly
- **Real-time Generation**: Systems can run in parallel
- **Memory Efficient**: Components are tightly packed
- **Scalable**: Performance doesn't degrade with world size

---

## 🔧 **Debugging ECS**

### **Entity Debugger**

1. Open **Window > Entities > Entity Debugger**
2. See all entities and their components
3. Filter by component types
4. Inspect component data in real-time

### **System Scheduling Debugger**

```csharp
// Add to any system to see when it runs
protected override void OnUpdate()
{
    Debug.Log($"System {GetType().Name} running at frame {Time.frameCount}");
}
```

### **Component Data Inspection**

```csharp
// Debug system to inspect entities
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class DebugSystem : SystemBase
{
    protected override void OnUpdate()
    {
        int entityCount = 0;
        foreach (var health in SystemAPI.Query<RefRO<Health>>())
        {
            entityCount++;
        }
        Debug.Log($"Total entities with Health: {entityCount}");
    }
}
```

---

## 🎯 **Migration Guide: MonoBehaviour to ECS**

### **Step 1: Identify Components**

```csharp
// Before (MonoBehaviour)
public class Enemy : MonoBehaviour
{
    public float health = 100f;
    public float speed = 5f;
    public Vector3 target;
}

// After (Components)
public struct Health : IComponentData { public float Value; }
public struct Movement : IComponentData { public float Speed; public float3 Target; }
public struct EnemyTag : IComponentData { }
```

### **Step 2: Create Systems**

```csharp
// Before (Update method)
void Update()
{
    transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
}

// After (System)
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class MovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float dt = SystemAPI.Time.DeltaTime;
        foreach (var (transform, movement) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Movement>>())
        {
            transform.ValueRW.Position = math.lerp(transform.ValueRW.Position, movement.ValueRO.Target, movement.ValueRO.Speed * dt);
        }
    }
}
```

### **Step 3: Handle Creation/Destruction**

```csharp
// Before
Instantiate(enemyPrefab, position, rotation);

// After
Entity entity = em.Instantiate(enemyPrefabEntity);
em.SetComponentData(entity, LocalTransform.FromPositionRotation(position, rotation));
```

---

## 🚀 **Performance Tips**

### **Memory Layout**
- Group frequently accessed components together
- Use smaller data types when possible
- Avoid large structs in components

### **System Optimization**
- Use `[BurstCompile]` on all systems
- Schedule jobs in parallel when possible
- Use `IJobEntity` for simple operations

### **Query Optimization**
- Be specific with queries to reduce iterations
- Use `WithAll`, `WithAny`, `WithNone` to filter
- Cache query results when possible

### **Entity Management**
- Use EntityCommandBuffer for deferred operations
- Pool entities when creating/destroying frequently
- Use archetypes for similar entities

---

## 🎯 **Common Pitfalls**

| Problem | Symptom | Solution |
|---------|---------|----------|
| **Component not updating** | Changes don't apply | Use `RefRW` for writable components |
| **System not running** | Logic doesn't execute | Check system update group and order |
| **Memory corruption** | Crashes or weird behavior | Don't store references to component data |
| **Slow performance** | Low FPS with many entities | Use Burst compilation and parallel jobs |
| **Query not finding entities** | System processes nothing | Verify entities have required components |

---

## 🚀 **Next Steps**

**Ready to dive deeper?**
- **[Performance Optimization](performance.md)** - Speed up your ECS code
- **[Extending MetVanDAMN](extending.md)** - Add custom features to the framework
- **[Jobs and Burst](../advanced/jobs-burst.md)** - Advanced parallel processing

**Need help?**
- Check the [ECS documentation](https://docs.unity3d.com/Packages/com.unity.entities@1.2/)
- Look at [MetVanDAMN's ECS systems](../../Assets/Scripts/ECS/) for examples
- Join [Unity ECS discussions](https://forum.unity.com/forums/ecs.222/) for community help

---

*"ECS is like learning to ride a bicycle - awkward at first, but once you get it, you wonder how you ever lived without it."*

**🍑 Happy Entity Component System Programming! 🍑**
