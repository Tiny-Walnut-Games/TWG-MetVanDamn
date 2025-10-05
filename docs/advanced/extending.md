# 🛠️ Extending MetVanDAMN
## *Adding Your Own Features to the Framework*

> **"MetVanDAMN is designed to be extended. Don't like how something works? Change it! Want a new feature? Add it!"**

[![Extending](https://img.shields.io/badge/Extending-Framework-purple.svg)](extending.md)
[![Modular](https://img.shields.io/badge/Modular-Architecture-green.svg)](extending.md)

---

## 🎯 **Why Extend MetVanDAMN?**

**Extending** MetVanDAMN allows you to:

- 🎨 **Customize Generation** - Change how worlds are created
- ➕ **Add New Features** - Implement unique mechanics
- 🔧 **Modify Behavior** - Adjust existing systems
- 🚀 **Optimize Performance** - Tailor for your specific needs
- 🎮 **Create Variants** - Build different game types

**The framework is designed to be modular and extensible!**

---

## 🏗️ **Extension Architecture**

### **Core Extension Points**

1. **Systems** - Add new ECS systems
2. **Components** - Create new data structures
3. **Algorithms** - Modify generation logic
4. **Assets** - Add custom art and audio
5. **Configuration** - Extend world settings

### **Extension Patterns**

```csharp
// Pattern 1: Add to existing systems
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(MetVanDAMN.Core.BiomeSystem))] // Run after core system
public partial class CustomBiomeModifierSystem : SystemBase
{
    // Your custom logic here
}

// Pattern 2: Create new component systems
[GenerateAuthoringComponent]
public struct CustomFeature : IComponentData
{
    public float CustomValue;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class CustomFeatureSystem : SystemBase
{
    // Process your custom components
}
```

---

## 🚀 **Quick Extension Examples**

### **Example 1: Custom Biome Logic**

```csharp
// Add a new biome type
public enum CustomBiomeType
{
    Fire = 100,    // Start at 100 to avoid conflicts
    Ice = 101,
    Magic = 102
}

// Extend biome data
[GenerateAuthoringComponent]
public struct CustomBiomeData : IComponentData
{
    public CustomBiomeType Type;
    public float Intensity;
}

// Custom biome system
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class CustomBiomeSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (biome, transform) in SystemAPI.Query<RefRO<CustomBiomeData>, RefRW<LocalTransform>>())
        {
            switch (biome.ValueRO.Type)
            {
                case CustomBiomeType.Fire:
                    // Apply fire effects (particles, damage over time, etc.)
                    ApplyFireEffects(transform.ValueRW);
                    break;

                case CustomBiomeType.Ice:
                    // Apply ice effects (slow movement, slippery surfaces)
                    ApplyIceEffects(transform.ValueRW);
                    break;

                case CustomBiomeType.Magic:
                    // Apply magic effects (teleportation, illusions)
                    ApplyMagicEffects(transform.ValueRW);
                    break;
            }
        }
    }

    private void ApplyFireEffects(LocalTransform transform)
    {
        // Add fire particle effects, damage nearby entities, etc.
    }

    private void ApplyIceEffects(LocalTransform transform)
    {
        // Make surfaces slippery, slow nearby movement, etc.
    }

    private void ApplyMagicEffects(LocalTransform transform)
    {
        // Create magical illusions, teleportation points, etc.
    }
}
```

### **Example 2: New Enemy Type**

```csharp
// Custom enemy component
[GenerateAuthoringComponent]
public struct FlyingEnemy : IComponentData
{
    public float FlightHeight;
    public float HoverAmplitude;
    public float Speed;
}

// Flying enemy system
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class FlyingEnemySystem : SystemBase
{
    protected override void OnUpdate()
    {
        float time = (float)SystemAPI.Time.ElapsedTime;

        foreach (var (flying, transform) in SystemAPI.Query<RefRO<FlyingEnemy>, RefRW<LocalTransform>>())
        {
            // Make enemy hover up and down
            float hoverOffset = math.sin(time * 2f) * flying.ValueRO.HoverAmplitude;
            float targetHeight = flying.ValueRO.FlightHeight + hoverOffset;

            // Smoothly move to target height
            float3 currentPos = transform.ValueRO.Position;
            float3 targetPos = new float3(currentPos.x, targetHeight, currentPos.z);

            transform.ValueRW.Position = math.lerp(currentPos, targetPos,
                flying.ValueRO.Speed * SystemAPI.Time.DeltaTime);
        }
    }
}
```

### **Example 3: Custom Room Generator**

```csharp
// Custom room type
public enum CustomRoomType
{
    Treasure = 200,
    Puzzle = 201,
    Boss = 202
}

// Custom room generator
public class CustomRoomGenerator : MetVanDAMN.Core.IRoomGenerator
{
    public void GenerateRoom(EntityManager em, Entity roomEntity, RoomData roomData)
    {
        switch ((CustomRoomType)roomData.Type)
        {
            case CustomRoomType.Treasure:
                GenerateTreasureRoom(em, roomEntity, roomData);
                break;

            case CustomRoomType.Puzzle:
                GeneratePuzzleRoom(em, roomEntity, roomData);
                break;

            case CustomRoomType.Boss:
                GenerateBossRoom(em, roomEntity, roomData);
                break;
        }
    }

    private void GenerateTreasureRoom(EntityManager em, Entity roomEntity, RoomData roomData)
    {
        // Create treasure chests, gold piles, etc.
        for (int i = 0; i < 5; i++)
        {
            Entity chest = em.CreateEntity();
            em.AddComponentData(chest, new TreasureChest { Value = 100 });
            em.AddComponentData(chest, LocalTransform.FromPosition(roomData.Bounds.center + new float3(i, 0, 0)));
        }
    }

    private void GeneratePuzzleRoom(EntityManager em, Entity roomEntity, RoomData roomData)
    {
        // Create puzzle elements, switches, doors, etc.
        // Implementation depends on your puzzle mechanics
    }

    private void GenerateBossRoom(EntityManager em, Entity roomEntity, RoomData roomData)
    {
        // Create boss enemy, arena, checkpoints, etc.
        Entity boss = em.CreateEntity();
        em.AddComponentData(boss, new BossEnemy { Health = 1000, Phase = 1 });
        em.AddComponentData(boss, LocalTransform.FromPosition(roomData.Bounds.center));
    }
}
```

---

## 🎮 **Advanced Extensions**

### **Custom World Generation Algorithm**

```csharp
// Custom world generator
public class CustomWorldGenerator : MetVanDAMN.Core.IWorldGenerator
{
    public void GenerateWorld(EntityManager em, WorldConfiguration config)
    {
        // Custom generation logic
        // Instead of standard district-based generation,
        // create a custom layout

        // Example: Generate a spiral world
        GenerateSpiralLayout(em, config);
    }

    private void GenerateSpiralLayout(EntityManager em, WorldConfiguration config)
    {
        int districtsCreated = 0;
        float angle = 0f;
        float radius = 5f;

        while (districtsCreated < config.TargetDistrictCount)
        {
            // Calculate position in spiral
            float x = math.cos(angle) * radius;
            float z = math.sin(angle) * radius;

            // Create district at this position
            Entity district = em.CreateEntity();
            em.AddComponentData(district, new DistrictData
            {
                Position = new int2((int)x, (int)z),
                Type = DistrictType.Normal
            });

            // Move to next spiral position
            angle += 0.5f; // Spiral tightness
            radius += 2f;  // Spiral expansion
            districtsCreated++;
        }
    }
}
```

### **Modular Feature System**

```csharp
// Feature toggle system
[GenerateAuthoringComponent]
public struct FeatureToggle : IComponentData
{
    public FeatureType EnabledFeatures; // Bitmask of features
}

[Flags]
public enum FeatureType
{
    None = 0,
    FlyingEnemies = 1 << 0,
    CustomBiomes = 1 << 1,
    PuzzleRooms = 1 << 2,
    BossBattles = 1 << 3,
    All = ~0
}

// Conditional system execution
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class ConditionalSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Only run if feature is enabled
        foreach (var toggle in SystemAPI.Query<RefRO<FeatureToggle>>())
        {
            if ((toggle.ValueRO.EnabledFeatures & FeatureType.FlyingEnemies) != 0)
            {
                // Run flying enemy logic
                UpdateFlyingEnemies();
            }

            if ((toggle.ValueRO.EnabledFeatures & FeatureType.CustomBiomes) != 0)
            {
                // Run custom biome logic
                UpdateCustomBiomes();
            }
        }
    }
}
```

### **Plugin Architecture**

```csharp
// Plugin interface
public interface IMetVanDAMNPlugin
{
    string Name { get; }
    string Version { get; }
    void Initialize(World world);
    void Update();
    void Cleanup();
}

// Plugin manager
public class PluginManager : SystemBase
{
    private List<IMetVanDAMNPlugin> plugins = new List<IMetVanDAMNPlugin>();

    public void RegisterPlugin(IMetVanDAMNPlugin plugin)
    {
        plugins.Add(plugin);
        plugin.Initialize(World);
    }

    protected override void OnUpdate()
    {
        foreach (var plugin in plugins)
        {
            plugin.Update();
        }
    }

    protected override void OnDestroy()
    {
        foreach (var plugin in plugins)
        {
            plugin.Cleanup();
        }
    }
}

// Example plugin
public class AnalyticsPlugin : IMetVanDAMNPlugin
{
    public string Name => "Analytics Plugin";
    public string Version => "1.0.0";

    public void Initialize(World world)
    {
        Debug.Log("Analytics plugin initialized");
        // Setup analytics tracking
    }

    public void Update()
    {
        // Track player progress, world generation stats, etc.
    }

    public void Cleanup()
    {
        // Save analytics data
        Debug.Log("Analytics plugin cleaned up");
    }
}
```

---

## 🔧 **Extension Best Practices**

### **Compatibility**

```csharp
// Check for required components
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class CompatibleSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Only process entities that have required components
        foreach (var entity in SystemAPI.Query<Entity>()
            .WithAll<RequiredComponent>()           // Must have this
            .WithNone<IncompatibleComponent>()      // Must not have this
            .WithEntityAccess())
        {
            // Safe to process
            ProcessEntity(entity);
        }
    }
}
```

### **Version Compatibility**

```csharp
// Version checking
public static class MetVanDAMNVersion
{
    public const string RequiredVersion = "1.2.0";

    public static bool IsCompatible(string currentVersion)
    {
        // Simple version comparison
        return string.Compare(currentVersion, RequiredVersion) >= 0;
    }
}

// In your extension
public class MyExtension : MonoBehaviour
{
    void Start()
    {
        string currentVersion = MetVanDAMN.Core.Version.GetVersion();
        if (!MetVanDAMNVersion.IsCompatible(currentVersion))
        {
            Debug.LogError($"Extension requires MetVanDAMN {MetVanDAMNVersion.RequiredVersion} or higher. Current: {currentVersion}");
            enabled = false;
            return;
        }

        // Safe to initialize
        InitializeExtension();
    }
}
```

### **Error Handling**

```csharp
// Robust extension with error handling
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class RobustSystem : SystemBase
{
    protected override void OnUpdate()
    {
        try
        {
            // Your extension logic
            foreach (var data in SystemAPI.Query<RefRW<CustomData>>())
            {
                ProcessData(data);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Extension error in {GetType().Name}: {e.Message}");
            // Continue execution or disable system
        }
    }

    private void ProcessData(RefRW<CustomData> data)
    {
        // Validate data before processing
        if (data.ValueRO.Value < 0)
        {
            Debug.LogWarning("Invalid data value, skipping");
            return;
        }

        // Process data
        data.ValueRW.Value *= 2;
    }
}
```

---

## 📦 **Packaging Extensions**

### **Extension Package Structure**

```
MyCustomExtension/
├── Runtime/
│   ├── Systems/
│   │   ├── CustomSystem.cs
│   │   └── AnotherSystem.cs
│   ├── Components/
│   │   ├── CustomComponent.cs
│   │   └── FeatureData.cs
│   └── Utilities/
│       └── ExtensionUtils.cs
├── Editor/
│   ├── CustomEditor.cs
│   └── InspectorExtensions.cs
├── Tests/
│   ├── CustomSystemTests.cs
│   └── IntegrationTests.cs
├── Documentation/
│   ├── README.md
│   └── Examples/
├── package.json
└── CHANGELOG.md
```

### **Package Configuration**

```json
// package.json
{
  "name": "com.yourcompany.metvandamn.extension",
  "version": "1.0.0",
  "displayName": "My Custom MetVanDAMN Extension",
  "description": "Adds custom features to MetVanDAMN",
  "dependencies": {
    "com.tinywalnutgames.metvd.core": "1.2.0"
  },
  "keywords": ["metvandamn", "extension", "procedural"],
  "author": {
    "name": "Your Name",
    "email": "your.email@example.com"
  }
}
```

---

## 🎯 **Testing Extensions**

### **Unit Tests**

```csharp
using NUnit.Framework;
using Unity.Entities;
using UnityEngine.TestTools;

public class CustomSystemTests
{
    private World world;
    private EntityManager em;

    [SetUp]
    public void Setup()
    {
        world = new World("Test World");
        em = world.EntityManager;
    }

    [TearDown]
    public void TearDown()
    {
        world.Dispose();
    }

    [Test]
    public void CustomSystem_ProcessesEntitiesCorrectly()
    {
        // Create test entity
        Entity entity = em.CreateEntity();
        em.AddComponentData(entity, new CustomData { Value = 5 });

        // Create and update system
        var system = world.GetOrCreateSystem<CustomSystem>();
        system.Update();

        // Verify results
        var data = em.GetComponentData<CustomData>(entity);
        Assert.AreEqual(10, data.Value); // Should be doubled
    }
}
```

### **Integration Tests**

```csharp
[TestFixture]
public class ExtensionIntegrationTests
{
    [UnityTest]
    public IEnumerator Extension_WorksWithMetVanDAMN()
    {
        // Load test scene with MetVanDAMN
        yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("TestScene");

        // Wait for systems to initialize
        yield return new WaitForSeconds(1f);

        // Verify extension components exist
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        int customEntities = 0;

        foreach (var entity in em.GetAllEntities())
        {
            if (em.HasComponent<CustomData>(entity))
            {
                customEntities++;
            }
        }

        Assert.Greater(customEntities, 0, "Extension should create custom entities");
    }
}
```

---

## 🚀 **Publishing Extensions**

### **GitHub Repository**

1. **Create Repository** - Use clear naming like `MetVanDAMN-CustomExtension`
2. **Documentation** - Include setup instructions and examples
3. **License** - Choose appropriate open source license
4. **Releases** - Tag versions for easy installation

### **Unity Package Registry**

```json
// Submit to Unity's package registry
{
  "name": "com.yourcompany.metvandamn.extension",
  "versions": {
    "1.0.0": {
      "dependencies": {},
      "dist": {
        "shasum": "...",
        "tarball": "https://..."
      }
    }
  }
}
```

### **Community Sharing**

- **GitHub Discussions** - Share your extension
- **Unity Forums** - Post in MetVanDAMN threads
- **Itch.io** - Share demo projects
- **Discord** - Join gamedev communities

---

## 🎯 **Extension Ideas**

### **Gameplay Extensions**
- **Time Travel** - Rewind world generation
- **Multiplayer** - Synchronized world generation
- **Destructible Worlds** - Dynamic terrain destruction
- **Weather Systems** - Dynamic environmental effects

### **Art Extensions**
- **Style Transfer** - Apply art styles procedurally
- **Animation Systems** - Custom character animations
- **Particle Effects** - Advanced visual effects
- **Audio Integration** - Dynamic sound generation

### **Technical Extensions**
- **Save/Load Systems** - Persistent world state
- **Networking** - Multiplayer world sync
- **Mod Support** - Runtime mod loading
- **Performance Tools** - Advanced profiling

---

## 🚀 **Next Steps**

**Ready to create your extension?**
- **[ECS Architecture](../advanced/ecs-architecture.md)** - Understand the foundation
- **[Performance Optimization](performance.md)** - Make your extension fast
- **[Testing & Debugging](../../testing-debugging/)** - Ensure quality

**Need inspiration?**
- Browse [existing extensions](../../Assets/Extensions/) in the project
- Check [GitHub Discussions](https://github.com/jmeyer1980/TWG-MetVanDAMN/discussions) for ideas
- Study the [core systems](../../Assets/Scripts/ECS/) for patterns

---

*"The best games are built by communities. Your extension could be the next big feature!"*

**🍑 Happy Extending! 🍑**
