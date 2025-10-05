# ⚡ Performance Optimization
## *Making Your MetVanDAMN Worlds Run Fast*

> **"Performance isn't a feature - it's a requirement. Players notice when your game runs smoothly, and they REALLY notice when it doesn't."**

[![Performance](https://img.shields.io/badge/Performance-Optimization-red.svg)](performance.md)
[![Burst](https://img.shields.io/badge/Burst-Compiled-orange.svg)](https://docs.unity3d.com/Packages/com.unity.burst@1.8/)

---

## 🎯 **Why Performance Matters**

**Performance** is about making your game run smoothly on target hardware. MetVanDAMN worlds can be huge, so optimization is crucial:

- 🎮 **60 FPS Target** - Games should run at smooth framerates
- 📱 **Platform Variety** - Works on different hardware specs
- 🔋 **Battery Life** - Efficient code uses less power
- 👥 **Player Experience** - Lag breaks immersion

**Good performance means more players can enjoy your game!**

---

## 🏗️ **Performance Profiling**

### **Unity Profiler**

1. Open **Window > Analysis > Profiler**
2. Play your scene
3. Look for bottlenecks:
   - **CPU Usage** - Which systems take the most time?
   - **Memory** - Are you allocating too much?
   - **Rendering** - Too many draw calls?

### **MetVanDAMN-Specific Profiling**

```csharp
// Add to any system to measure performance
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class PerformanceMonitorSystem : SystemBase
{
    private System.Diagnostics.Stopwatch stopwatch;
    private int frameCount = 0;

    protected override void OnCreate()
    {
        stopwatch = new System.Diagnostics.Stopwatch();
    }

    protected override void OnUpdate()
    {
        frameCount++;

        if (frameCount % 60 == 0) // Every second at 60fps
        {
            Debug.Log($"Frame time: {SystemAPI.Time.DeltaTime * 1000:F2}ms");
        }

        // Profile specific operations
        stopwatch.Restart();

        // ... your code here ...

        stopwatch.Stop();
        if (stopwatch.ElapsedMilliseconds > 16) // Over 1 frame at 60fps
        {
            Debug.LogWarning($"Slow operation detected: {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}
```

---

## 🚀 **ECS Performance Best Practices**

### **Use Burst Compilation**

```csharp
// ❌ Without Burst - slow
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class SlowSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (transform, velocity) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Velocity>>())
        {
            transform.ValueRW.Position += velocity.ValueRO.Value * SystemAPI.Time.DeltaTime;
        }
    }
}

// ✅ With Burst - fast!
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct FastMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (transform, velocity) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Velocity>>())
        {
            transform.ValueRW.Position += velocity.ValueRO.Value * deltaTime;
        }
    }
}
```

**Burst Benefits:**
- 🚀 **10-100x faster** than regular C#
- 🔒 **No GC allocations** in compiled code
- ⚡ **SIMD instructions** for parallel processing

### **Use Jobs for Parallel Processing**

```csharp
[BurstCompile]
public partial struct ParallelMovementJob : IJobEntity
{
    public float DeltaTime;

    [BurstCompile]
    public void Execute(RefRW<LocalTransform> transform, in Velocity velocity)
    {
        transform.ValueRW.Position += velocity.ValueRO.Value * DeltaTime;
    }
}

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ParallelMovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Schedule job to run on multiple cores
        new ParallelMovementJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime
        }.ScheduleParallel(); // ✨ Magic happens here
    }
}
```

### **Optimize Queries**

```csharp
// ❌ Inefficient - checks all entities
foreach (var health in SystemAPI.Query<RefRW<Health>>())
{
    // Process every entity with Health
}

// ✅ Efficient - specific queries
foreach (var health in SystemAPI.Query<RefRW<Health>>()
    .WithAll<EnemyTag>()           // Only enemies
    .WithNone<DeadTag>())          // Not dead
{
    // Process only living enemies
}

// ✅ Even better - use aspects
public readonly partial struct EnemyAspect : IAspect
{
    public readonly RefRW<Health> Health;
    public readonly RefRO<EnemyTag> Tag;
    // ... other components
}

foreach (var enemy in SystemAPI.Query<EnemyAspect>()
    .WithNone<DeadTag>())
{
    // Clean, type-safe access
}
```

---

## 🎮 **World Generation Optimization**

### **Chunk-Based Generation**

```csharp
// Generate world in chunks instead of all at once
[GenerateAuthoringComponent]
public struct WorldChunk : IComponentData
{
    public int2 ChunkPosition;
    public ChunkState State; // Empty, Generating, Complete
}

[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct ChunkGenerationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Only process a few chunks per frame
        int chunksProcessedThisFrame = 0;
        const int maxChunksPerFrame = 5;

        foreach (var (chunk, entity) in SystemAPI.Query<RefRW<WorldChunk>>()
            .WithEntityAccess())
        {
            if (chunksProcessedThisFrame >= maxChunksPerFrame) break;

            if (chunk.ValueRO.State == ChunkState.Generating)
            {
                // Generate this chunk
                GenerateChunk(chunk.ValueRW.ChunkPosition);
                chunk.ValueRW.State = ChunkState.Complete;
                chunksProcessedThisFrame++;
            }
        }
    }

    private void GenerateChunk(int2 chunkPos)
    {
        // Generate rooms, connections, etc. for this chunk
    }
}
```

### **Level of Detail (LOD)**

```csharp
public struct LODLevel : IComponentData
{
    public int CurrentLevel; // 0 = high detail, 3 = low detail
    public float DistanceToCamera;
}

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct LODSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float3 cameraPos = Camera.main.transform.position;

        foreach (var (transform, lod) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<LODLevel>>())
        {
            float distance = math.distance(transform.ValueRO.Position, cameraPos);

            // Update LOD based on distance
            if (distance < 10f) lod.ValueRW.CurrentLevel = 0;      // High detail
            else if (distance < 50f) lod.ValueRW.CurrentLevel = 1; // Medium detail
            else if (distance < 100f) lod.ValueRW.CurrentLevel = 2; // Low detail
            else lod.ValueRW.CurrentLevel = 3;                     // Very low detail
        }
    }
}
```

### **Object Pooling**

```csharp
// Pool entities instead of creating/destroying
public struct EntityPool : IComponentData
{
    public NativeList<Entity> AvailableEntities;
    public Entity PrefabEntity;
}

public partial class EntityPoolSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Manage pool of reusable entities
    }

    public Entity GetEntity(EntityManager em, EntityPool pool)
    {
        if (pool.AvailableEntities.Length > 0)
        {
            // Reuse existing entity
            Entity entity = pool.AvailableEntities[pool.AvailableEntities.Length - 1];
            pool.AvailableEntities.RemoveAt(pool.AvailableEntities.Length - 1);
            return entity;
        }
        else
        {
            // Create new entity
            return em.Instantiate(pool.PrefabEntity);
        }
    }

    public void ReturnEntity(EntityPool pool, Entity entity)
    {
        // Reset entity state and return to pool
        pool.AvailableEntities.Add(entity);
    }
}
```

---

## 🎨 **Rendering Optimization**

### **Frustum Culling**

```csharp
// Only render visible entities
[GenerateAuthoringComponent]
public struct Cullable : IComponentData
{
    public Bounds Bounds;
}

[BurstCompile]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct FrustumCullingSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var camera = Camera.main;
        var planes = GeometryUtility.CalculateFrustumPlanes(camera);

        foreach (var (cullable, entity) in SystemAPI.Query<RefRO<Cullable>>()
            .WithEntityAccess())
        {
            bool visible = GeometryUtility.TestPlanesAABB(planes, cullable.ValueRO.Bounds);

            // Enable/disable rendering based on visibility
            if (visible)
            {
                state.EntityManager.RemoveComponent<Disabled>(entity);
            }
            else
            {
                state.EntityManager.AddComponent<Disabled>(entity);
            }
        }
    }
}
```

### **Batch Rendering**

```csharp
// Group similar objects for efficient rendering
[GenerateAuthoringComponent]
public struct RenderBatch : IComponentData
{
    public int BatchId;
    public Material SharedMaterial;
}

[BurstCompile]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct BatchRenderingSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Group entities by material for batched rendering
        var batches = new NativeHashMap<int, NativeList<Entity>>(64, Allocator.Temp);

        foreach (var (batch, entity) in SystemAPI.Query<RefRO<RenderBatch>>()
            .WithEntityAccess())
        {
            if (!batches.ContainsKey(batch.ValueRO.BatchId))
            {
                batches[batch.ValueRO.BatchId] = new NativeList<Entity>(Allocator.Temp);
            }
            batches[batch.ValueRO.BatchId].Add(entity);
        }

        // Render each batch
        foreach (var batch in batches)
        {
            // Combine meshes/materials for this batch
            RenderBatch(batch.Value);
        }
    }
}
```

---

## 🔧 **Memory Optimization**

### **Component Data Layout**

```csharp
// ❌ Bad - large components mixed with small ones
public struct BadLayout : IComponentData
{
    public float Health;        // 4 bytes
    public Matrix4x4 Transform; // 64 bytes - misaligned!
    public bool IsAlive;        // 1 byte
}

// ✅ Good - similar sizes grouped
public struct HealthData : IComponentData
{
    public float Health;        // 4 bytes
    public float MaxHealth;     // 4 bytes
    public bool IsAlive;        // 1 byte (packed)
}

public struct TransformData : IComponentData
{
    public Matrix4x4 Transform; // 64 bytes - separate component
}
```

### **Avoid Allocations in Hot Code**

```csharp
// ❌ Bad - allocates every frame
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class BadSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var list = new List<float>(); // Allocates!
        foreach (var health in SystemAPI.Query<RefRO<Health>>())
        {
            list.Add(health.ValueRO.Value);
        }
    }
}

// ✅ Good - no allocations
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct GoodSystem : SystemBase
{
    private NativeList<float> healthValues; // Pre-allocated

    protected override void OnCreate()
    {
        healthValues = new NativeList<float>(1000, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        healthValues.Dispose();
    }

    protected override void OnUpdate()
    {
        healthValues.Clear();
        foreach (var health in SystemAPI.Query<RefRO<Health>>())
        {
            healthValues.Add(health.ValueRO.Value); // No allocation
        }
    }
}
```

---

## 🎯 **Profiling Tools**

### **Custom Performance Monitors**

```csharp
public static class PerformanceProfiler
{
    private static System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
    private static Dictionary<string, long> timings = new Dictionary<string, long>();

    public static void StartTiming(string operation)
    {
        stopwatch.Restart();
    }

    public static void EndTiming(string operation)
    {
        stopwatch.Stop();
        timings[operation] = stopwatch.ElapsedTicks;

        if (stopwatch.ElapsedMilliseconds > 16) // Over 1 frame
        {
            Debug.LogWarning($"{operation} took {stopwatch.ElapsedMilliseconds}ms");
        }
    }

    public static void LogReport()
    {
        Debug.Log("=== PERFORMANCE REPORT ===");
        foreach (var kvp in timings)
        {
            double ms = (kvp.Value / (double)System.Diagnostics.Stopwatch.Frequency) * 1000;
            Debug.Log($"{kvp.Key}: {ms:F2}ms");
        }
    }
}

// Usage in systems
protected override void OnUpdate()
{
    PerformanceProfiler.StartTiming("Enemy AI Update");

    // ... AI logic ...

    PerformanceProfiler.EndTiming("Enemy AI Update");
}
```

### **Memory Usage Tracking**

```csharp
public class MemoryProfiler : MonoBehaviour
{
    void Update()
    {
        if (Time.frameCount % 60 == 0) // Every second
        {
            long memoryUsed = System.GC.GetTotalMemory(false);
            Debug.Log($"Memory Usage: {memoryUsed / 1024 / 1024} MB");

            // Force GC to see managed memory
            System.GC.Collect();
            long afterGC = System.GC.GetTotalMemory(true);
            Debug.Log($"After GC: {afterGC / 1024 / 1024} MB");
        }
    }
}
```

---

## 🚀 **Platform-Specific Optimization**

### **Mobile Optimization**

```csharp
// Reduce quality on mobile
public class MobileOptimizationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        #if UNITY_IOS || UNITY_ANDROID
        // Lower quality settings for mobile
        QualitySettings.SetQualityLevel(1); // Low quality

        // Reduce particle effects
        foreach (var particles in SystemAPI.Query<RefRW<ParticleSystem>>())
        {
            particles.ValueRW.maxParticles = 50; // Reduce particle count
        }
        #endif
    }
}
```

### **WebGL Optimization**

```csharp
// WebGL has limitations - optimize accordingly
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct WebGLOptimizationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        #if UNITY_WEBGL
        // Reduce entity count for WebGL
        int maxEntities = 500; // WebGL limit
        int currentEntities = 0;

        foreach (var entity in SystemAPI.Query<Entity>()
            .WithEntityAccess())
        {
            currentEntities++;
            if (currentEntities > maxEntities)
            {
                // Disable excess entities
                state.EntityManager.AddComponent<Disabled>(entity);
            }
        }
        #endif
    }
}
```

---

## 🎯 **Performance Checklist**

### **Before Release**
- [ ] **Profile on target hardware** - Test on actual devices
- [ ] **Check memory usage** - Monitor for leaks
- [ ] **Test with maximum entities** - Stress test limits
- [ ] **Verify 60 FPS** - Smooth performance across all scenes
- [ ] **Battery testing** - Check power consumption

### **Common Issues**
- [ ] **GC spikes** - Use Profiler to find allocations
- [ ] **Frame drops** - Identify CPU/GPU bottlenecks
- [ ] **Memory leaks** - Check for undisposed native containers
- [ ] **Shader compilation** - Pre-warm shaders on load

### **Optimization Priority**
1. **Critical Path** - Systems that run every frame
2. **Memory Usage** - Large allocations and leaks
3. **Load Times** - Initial scene loading
4. **Visual Quality** - Effects that impact perception

---

## 🚀 **Next Steps**

**Ready to optimize further?**
- **[ECS Architecture](ecs-architecture.md)** - Deep dive into ECS patterns
- **[Extending MetVanDAMN](extending.md)** - Add custom optimizations
- **[Debug Tools](../testing-debugging/debug-tools.md)** - Advanced profiling

**Need help with performance?**
- Use the [Unity Profiler](https://docs.unity.com/Packages/com.unity.entities@1.2/manual/profiler.html)
- Check [MetVanDAMN performance docs](../../docs/performance/) for framework-specific tips
- Join [performance discussions](https://github.com/jmeyer1980/TWG-MetVanDamn/discussions/categories/performance)

---

*"Performance optimization is like cleaning your room - it feels like work, but the result is so much better!"*

**🍑 Happy Optimizing! 🍑**
