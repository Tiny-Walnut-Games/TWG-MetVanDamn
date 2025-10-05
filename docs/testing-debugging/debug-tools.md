# 🔍 Debug Tools & Visualization
## *See Your World Generation in Action*

> **"Debugging without visualization is like trying to solve a puzzle in the dark. Turn on the lights!"**

[![Debug Tools](https://img.shields.io/badge/Debug-Tools-orange.svg)](debug-tools.md)
[![Visualization](https://img.shields.io/badge/Visualization-Real--time-blue.svg)](debug-tools.md)

---

## 🎯 **Why Debug Visualization?**

**Debug tools** let you see exactly how MetVanDAMN generates worlds. Instead of guessing what's happening, you can:

- 👁️ **Watch Generation** - See rooms and connections appear in real-time
- 🎨 **Color-Code Elements** - Different colors for different types
- 📊 **Show Data** - Display entity counts, performance stats
- 🎮 **Interactive Inspection** - Click entities to see their data
- 📱 **Mobile Friendly** - Works on all platforms

**Good debugging turns "why isn't this working?" into "oh, I see the problem!"**

---

## 🏗️ **Built-in Debug Systems**

### **World Bounds Visualization**

```csharp
// Draws the world boundaries as colored wireframes
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class WorldBoundsDebugSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Draw every 120 frames (2 seconds at 60fps)
        if (SystemAPI.Time.FrameCount % 120 != 0) return;

        foreach (var bounds in SystemAPI.Query<RefRO<WorldBounds>>())
        {
            // Draw world boundary
            DebugDrawBounds(bounds.ValueRO.Bounds, Color.green, 10f);
        }
    }

    private void DebugDrawBounds(Bounds bounds, Color color, float duration)
    {
        Vector3 center = bounds.center;
        Vector3 size = bounds.size;

        // Draw wireframe cube
        Debug.DrawLine(center + new Vector3(-size.x/2, -size.y/2, -size.z/2),
                     center + new Vector3(size.x/2, -size.y/2, -size.z/2), color, duration);
        // ... more lines for complete cube
    }
}
```

### **District Visualization**

```csharp
// Shows districts as colored spheres
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class DistrictDebugSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (district, transform) in SystemAPI.Query<RefRO<DistrictData>, RefRO<LocalTransform>>())
        {
            Color districtColor = GetDistrictColor(district.ValueRO.Type);
            DebugDrawSphere(transform.ValueRO.Position, 2f, districtColor);
        }
    }

    private Color GetDistrictColor(DistrictType type)
    {
        switch (type)
        {
            case DistrictType.Hub: return Color.yellow;
            case DistrictType.Normal: return Color.blue;
            case DistrictType.Challenge: return Color.red;
            case DistrictType.Secret: return Color.magenta;
            default: return Color.gray;
        }
    }

    private void DebugDrawSphere(Vector3 center, float radius, Color color)
    {
        // Draw sphere using Debug.DrawLine
        const int segments = 16;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (i / (float)segments) * 360f * Mathf.Deg2Rad;
            float angle2 = ((i + 1) / (float)segments) * 360f * Mathf.Deg2Rad;

            Vector3 point1 = center + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * radius;
            Vector3 point2 = center + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * radius;

            Debug.DrawLine(point1, point2, color, 0.1f);
        }
    }
}
```

### **Connection Visualization**

```csharp
// Draws lines between connected rooms
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class ConnectionDebugSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Build connection dictionary
        var connections = new Dictionary<Entity, List<Entity>>();

        foreach (var (connection, entity) in SystemAPI.Query<RefRO<RoomConnection>>()
            .WithEntityAccess())
        {
            if (!connections.ContainsKey(connection.ValueRO.FromRoom))
                connections[connection.ValueRO.FromRoom] = new List<Entity>();

            connections[connection.ValueRO.FromRoom].Add(connection.ValueRO.ToRoom);
        }

        // Draw connection lines
        foreach (var kvp in connections)
        {
            if (EntityManager.HasComponent<LocalTransform>(kvp.Key))
            {
                var fromPos = EntityManager.GetComponentData<LocalTransform>(kvp.Key).Position;

                foreach (var toRoom in kvp.Value)
                {
                    if (EntityManager.HasComponent<LocalTransform>(toRoom))
                    {
                        var toPos = EntityManager.GetComponentData<LocalTransform>(toRoom).Position;
                        Debug.DrawLine(fromPos, toPos, Color.cyan, 0.1f);
                    }
                }
            }
        }
    }
}
```

---

## 🚀 **Quick Debug Setup (10 Minutes)**

### **Step 1: Enable Debug Rendering**

```csharp
// Component to control debug visualization
[GenerateAuthoringComponent]
public struct DebugVisualization : IComponentData
{
    public bool ShowWorldBounds;
    public bool ShowDistricts;
    public bool ShowConnections;
    public bool ShowBiomes;
    public bool ShowPerformanceStats;
}

// System to manage debug rendering
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class DebugSetupSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Add debug component to world if it doesn't exist
        var query = SystemAPI.Query<RefRO<DebugVisualization>>();
        if (query.IsEmpty)
        {
            var entity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(entity, new DebugVisualization
            {
                ShowWorldBounds = true,
                ShowDistricts = true,
                ShowConnections = true,
                ShowBiomes = false,
                ShowPerformanceStats = true
            });
        }
    }
}
```

### **Step 2: Create Debug Toggle**

```csharp
// In-game debug toggle
public class DebugToggle : MonoBehaviour
{
    [SerializeField] private KeyCode toggleKey = KeyCode.F12;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleDebugVisualization();
        }
    }

    private void ToggleDebugVisualization()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;

        foreach (var debug in em.GetAllEntities()
            .Where(e => em.HasComponent<DebugVisualization>(e)))
        {
            var data = em.GetComponentData<DebugVisualization>(debug);
            data.ShowWorldBounds = !data.ShowWorldBounds;
            data.ShowDistricts = !data.ShowDistricts;
            data.ShowConnections = !data.ShowConnections;
            em.SetComponentData(debug, data);

            Debug.Log($"Debug visualization: {(data.ShowWorldBounds ? "ON" : "OFF")}");
            break;
        }
    }
}
```

### **Step 3: Add Performance Overlay**

```csharp
// On-screen performance stats
public class PerformanceOverlay : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI statsText;

    private float updateTimer = 0f;
    private int frameCount = 0;
    private float fps = 0f;

    void Update()
    {
        frameCount++;
        updateTimer += Time.deltaTime;

        if (updateTimer >= 0.5f) // Update 2x per second
        {
            fps = frameCount / updateTimer;
            frameCount = 0;
            updateTimer = 0f;

            UpdateStatsDisplay();
        }
    }

    private void UpdateStatsDisplay()
    {
        if (statsText != null)
        {
            var em = World.DefaultGameObjectInjectionWorld?.EntityManager;
            int entityCount = em?.GetAllEntities().Length ?? 0;

            statsText.text = $"FPS: {fps:F1}\n" +
                           $"Entities: {entityCount}\n" +
                           $"Time: {Time.time:F1}s";
        }
    }
}
```

---

## 🎮 **Advanced Debug Features**

### **Entity Inspector**

```csharp
// Click to inspect entities
public class EntityInspector : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI inspectText;
    [SerializeField] private Camera cam;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            InspectEntityUnderMouse();
        }
    }

    private void InspectEntityUnderMouse()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Try to find ECS entity from GameObject
            var entityObject = hit.collider.GetComponent<EntityReference>();
            if (entityObject != null)
            {
                DisplayEntityInfo(entityObject.Entity);
            }
        }
    }

    private void DisplayEntityInfo(Entity entity)
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;

        string info = $"Entity: {entity.Index}.{entity.Version}\n";

        // Check for common components
        if (em.HasComponent<LocalTransform>(entity))
        {
            var transform = em.GetComponentData<LocalTransform>(entity);
            info += $"Position: {transform.Position}\n";
        }

        if (em.HasComponent<Health>(entity))
        {
            var health = em.GetComponentData<Health>(entity);
            info += $"Health: {health.Value}/{health.MaxValue}\n";
        }

        if (em.HasComponent<DistrictData>(entity))
        {
            var district = em.GetComponentData<DistrictData>(entity);
            info += $"District Type: {district.Type}\n";
        }

        if (inspectText != null)
        {
            inspectText.text = info;
        }

        Debug.Log(info);
    }
}

// Component to link GameObjects to ECS entities
public class EntityReference : MonoBehaviour
{
    public Entity Entity;
}
```

### **Generation Step Logger**

```csharp
// Logs each step of world generation
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class GenerationLoggerSystem : SystemBase
{
    private NativeList<FixedString128Bytes> logMessages;

    protected override void OnCreate()
    {
        logMessages = new NativeList<FixedString128Bytes>(Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        logMessages.Dispose();
    }

    protected override void OnUpdate()
    {
        // Log generation events
        var generationEvents = SystemAPI.Query<RefRO<GenerationEvent>>();

        foreach (var evt in generationEvents)
        {
            FixedString128Bytes message = $"{SystemAPI.Time.ElapsedTime:F2}s: {evt.ValueRO.Message}";
            logMessages.Add(message);

            // Keep only last 50 messages
            if (logMessages.Length > 50)
            {
                logMessages.RemoveAt(0);
            }

            Debug.Log($"[Generation] {evt.ValueRO.Message}");
        }
    }

    // Method to get recent logs
    public void GetRecentLogs(List<string> output)
    {
        output.Clear();
        for (int i = 0; i < logMessages.Length; i++)
        {
            output.Add(logMessages[i].ToString());
        }
    }
}

// Event component for logging
[GenerateAuthoringComponent]
public struct GenerationEvent : IComponentData
{
    public FixedString128Bytes Message;
}
```

### **Biome Field Visualizer**

```csharp
// Shows biome polarity fields
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class BiomeFieldDebugSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (field, transform) in SystemAPI.Query<RefRO<BiomeField>, RefRO<LocalTransform>>())
        {
            // Draw field influence radius
            DebugDrawCircle(transform.ValueRO.Position, field.ValueRO.Radius,
                          GetBiomeColor(field.ValueRO.Type), 16);

            // Draw field strength indicator
            float strength = field.ValueRO.Strength;
            Vector3 strengthIndicator = transform.ValueRO.Position + Vector3.up * strength * 2f;
            Debug.DrawLine(transform.ValueRO.Position, strengthIndicator, Color.white, 0.1f);
        }
    }

    private Color GetBiomeColor(BiomeType type)
    {
        switch (type)
        {
            case BiomeType.Sun: return Color.yellow;
            case BiomeType.Moon: return Color.blue;
            case BiomeType.Heat: return Color.red;
            case BiomeType.Cold: return Color.cyan;
            default: return Color.gray;
        }
    }

    private void DebugDrawCircle(Vector3 center, float radius, Color color, int segments)
    {
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (i / (float)segments) * 360f * Mathf.Deg2Rad;
            float angle2 = ((i + 1) / (float)segments) * 360f * Mathf.Deg2Rad;

            Vector3 point1 = center + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * radius;
            Vector3 point2 = center + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * radius;

            Debug.DrawLine(point1, point2, color, 0.1f);
        }
    }
}
```

---

## 🔧 **Editor Debug Tools**

### **Entity Debugger Window**

```csharp
using UnityEditor;
using UnityEngine;

public class EntityDebuggerWindow : EditorWindow
{
    private Vector2 scrollPos;

    [MenuItem("MetVanDAMN/Entity Debugger")]
    static void ShowWindow()
    {
        GetWindow<EntityDebuggerWindow>("Entity Debugger");
    }

    void OnGUI()
    {
        var em = World.DefaultGameObjectInjectionWorld?.EntityManager;
        if (em == null)
        {
            EditorGUILayout.LabelField("No ECS world active");
            return;
        }

        EditorGUILayout.LabelField("Active Entities", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        int entityCount = 0;
        foreach (var entity in em.GetAllEntities())
        {
            entityCount++;
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"Entity {entity.Index}.{entity.Version}");

            if (GUILayout.Button("Inspect", GUILayout.Width(60)))
            {
                InspectEntity(entity);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.LabelField($"Total Entities: {entityCount}");
    }

    private void InspectEntity(Entity entity)
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;

        Debug.Log($"=== Inspecting Entity {entity.Index}.{entity.Version} ===");

        // Log all components
        var types = em.GetComponentTypes(entity);
        foreach (var type in types)
        {
            Debug.Log($"Component: {type.GetManagedType()}");
        }
    }
}
```

### **World Generation Recorder**

```csharp
// Records generation for playback/debugging
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class GenerationRecorderSystem : SystemBase
{
    private NativeList<GenerationFrame> recordedFrames;

    protected override void OnCreate()
    {
        recordedFrames = new NativeList<GenerationFrame>(Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        recordedFrames.Dispose();
    }

    protected override void OnUpdate()
    {
        // Record current state every few frames
        if (SystemAPI.Time.FrameCount % 10 == 0)
        {
            RecordCurrentFrame();
        }
    }

    private void RecordCurrentFrame()
    {
        var frame = new GenerationFrame
        {
            FrameNumber = SystemAPI.Time.FrameCount,
            Timestamp = SystemAPI.Time.ElapsedTime,
            EntityCount = 0
        };

        // Count entities (simplified)
        foreach (var _ in SystemAPI.Query<Entity>())
        {
            frame.EntityCount++;
        }

        recordedFrames.Add(frame);
    }

    // Playback methods would go here
}

public struct GenerationFrame
{
    public int FrameNumber;
    public double Timestamp;
    public int EntityCount;
}
```

---

## 🎯 **Debug Best Practices**

### **Performance Considerations**
- **Conditional Rendering** - Only draw debug visuals when needed
- **Frame Rate Limiting** - Don't update every frame for expensive operations
- **LOD for Debug** - Reduce detail for distant objects
- **Cleanup** - Remove debug systems in release builds

### **Visual Design**
- **Color Coding** - Use consistent colors for different types
- **Size Scaling** - Make important elements more visible
- **Transparency** - Use alpha for non-intrusive overlays
- **Animation** - Subtle animations to draw attention

### **Information Hierarchy**
- **Essential First** - Show critical info prominently
- **Progressive Disclosure** - More detail on demand
- **Context Aware** - Different info for different situations
- **User Control** - Let users choose what to see

### **Cross-Platform**
- **Shader Compatibility** - Debug visuals work on all platforms
- **Input Methods** - Support keyboard, mouse, touch
- **Performance Scaling** - Adjust quality for device capabilities
- **Build Variants** - Different debug levels for different builds

---

## 🚀 **Next Steps**

**Ready to debug your worlds?**
- **[Validation Tools](validation.md)** - Automated problem detection
- **[Troubleshooting](troubleshooting.md)** - Fix common issues
- **[Performance Optimization](../advanced/performance.md)** - Speed up debug systems

**Need debug tool examples?**
- Check the [debug systems](../../Assets/Scripts/Debug/) in the project
- Look at [debug scenes](../../Assets/Scenes/Debug/) for examples
- Join [GitHub Discussions](https://github.com/jmeyer1980/TWG-MetVanDAMN/discussions)

---

*"Debugging is like being a detective in a game where you're also the murderer. Make it easier on yourself!"*

**🍑 Happy Debugging! 🍑**
