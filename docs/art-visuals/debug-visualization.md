# 🔍 Debug Visualization
## *See Your World Come Alive*

> **"Debugging is like being a detective in a game where you're also the murderer. Visualization tools help you see the clues you left behind."**

[![Debug Tools](https://img.shields.io/badge/Debug-Tools-red.svg)](debug-visualization.md)
[![Visualization](https://img.shields.io/badge/Visualization-Systems-orange.svg)](debug-visualization.md)

---

## 🎯 **What is Debug Visualization?**

**Debug Visualization** in MetVanDAMN lets you see what's happening inside your world generation. Instead of guessing why districts aren't connecting or biomes aren't blending, you get visual feedback showing:

- 🗺️ **World Structure** - See districts, rooms, and connections
- 🌡️ **Biome Fields** - Visualize polarity fields and transitions
- 🔗 **Navigation Graph** - View AI pathfinding networks
- 📊 **Performance Data** - Monitor system performance
- 🎯 **Validation Results** - See what's working and what's broken

**Turn invisible systems into visible feedback!**

---

## 🏗️ **Core Visualization Systems**

### **World Bounds Visualizer**

```csharp
// Draw world boundaries and districts
public class WorldBoundsVisualizer : MonoBehaviour
{
    [SerializeField] private Color boundsColor = Color.green;
    [SerializeField] private Color districtColor = Color.blue;
    [SerializeField] private float lineWidth = 2f;

    private WorldConfiguration worldConfig;
    private EntityManager entityManager;

    void Start()
    {
        worldConfig = FindObjectOfType<WorldConfiguration>();
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    void OnDrawGizmos()
    {
        if (worldConfig == null) return;

        // Draw world bounds
        DrawWorldBounds();

        // Draw districts
        DrawDistricts();

        // Draw connections
        DrawConnections();
    }

    private void DrawWorldBounds()
    {
        Gizmos.color = boundsColor;
        Gizmos.DrawWireCube(worldConfig.WorldCenter, worldConfig.WorldSize);
    }

    private void DrawDistricts()
    {
        var districtQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<NodeId>(),
            ComponentType.ReadOnly<WfcState>()
        );

        Gizmos.color = districtColor;

        districtQuery.ForEach((Entity entity, ref NodeId nodeId, ref WfcState state) =>
        {
            // Calculate district bounds from node position
            Vector3 districtCenter = CalculateDistrictCenter(nodeId);
            Vector3 districtSize = CalculateDistrictSize(state);

            Gizmos.DrawWireCube(districtCenter, districtSize);

            // Label district
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(districtCenter, $"District {nodeId.Value}");
            #endif
        });
    }

    private void DrawConnections()
    {
        // Draw lines between connected districts
        var connectionQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<Connection>()
        );

        Gizmos.color = Color.yellow;

        connectionQuery.ForEach((Entity entity, ref Connection connection) =>
        {
            Vector3 startPos = GetDistrictPosition(connection.StartDistrict);
            Vector3 endPos = GetDistrictPosition(connection.EndDistrict);

            Gizmos.DrawLine(startPos, endPos);
        });
    }
}
```

### **Biome Field Renderer**

```csharp
// Visualize biome polarity fields
public class BiomeFieldVisualizer : MonoBehaviour
{
    [SerializeField] private float fieldResolution = 1f;
    [SerializeField] private Gradient heatGradient;
    [SerializeField] private Gradient coldGradient;
    [SerializeField] private float maxFieldStrength = 10f;

    private PolarityFieldSystem polaritySystem;

    void Start()
    {
        polaritySystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<PolarityFieldSystem>();
    }

    void OnDrawGizmos()
    {
        if (polaritySystem == null) return;

        DrawHeatField();
        DrawColdField();
        DrawBiomeTransitions();
    }

    private void DrawHeatField()
    {
        Bounds worldBounds = GetWorldBounds();

        for (float x = worldBounds.min.x; x < worldBounds.max.x; x += fieldResolution)
        {
            for (float y = worldBounds.min.y; y < worldBounds.max.y; y += fieldResolution)
            {
                Vector3 position = new Vector3(x, y, 0);
                float heatValue = polaritySystem.SampleHeatField(position);

                // Color based on heat intensity
                Color color = heatGradient.Evaluate(heatValue / maxFieldStrength);
                color.a = 0.3f; // Semi-transparent

                Gizmos.color = color;
                Gizmos.DrawCube(position, Vector3.one * fieldResolution * 0.8f);
            }
        }
    }

    private void DrawColdField()
    {
        Bounds worldBounds = GetWorldBounds();

        for (float x = worldBounds.min.x; x < worldBounds.max.x; x += fieldResolution)
        {
            for (float y = worldBounds.min.y; y < worldBounds.max.y; y += fieldResolution)
            {
                Vector3 position = new Vector3(x, y, 0);
                float coldValue = polaritySystem.SampleColdField(position);

                Color color = coldGradient.Evaluate(coldValue / maxFieldStrength);
                color.a = 0.3f;

                Gizmos.color = color;
                Gizmos.DrawCube(position, Vector3.one * fieldResolution * 0.8f);
            }
        }
    }

    private void DrawBiomeTransitions()
    {
        // Draw lines where biomes transition
        Bounds worldBounds = GetWorldBounds();

        for (float x = worldBounds.min.x; x < worldBounds.max.x; x += fieldResolution)
        {
            for (float y = worldBounds.min.y; y < worldBounds.max.y; y += fieldResolution)
            {
                Vector3 position = new Vector3(x, y, 0);
                BiomeType biome = CalculateBiomeAtPosition(position);

                // Check neighbors for transitions
                CheckBiomeTransition(position, biome, Vector3.right * fieldResolution);
                CheckBiomeTransition(position, biome, Vector3.up * fieldResolution);
            }
        }
    }

    private void CheckBiomeTransition(Vector3 position, BiomeType currentBiome, Vector3 offset)
    {
        BiomeType neighborBiome = CalculateBiomeAtPosition(position + offset);

        if (currentBiome != neighborBiome)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(position, position + offset);
        }
    }
}
```

---

## 🚀 **Quick Debug Setup (10 Minutes)**

### **Step 1: Basic World Visualizer**

```csharp
// Drop this on any GameObject to see world structure
public class QuickWorldVisualizer : MonoBehaviour
{
    [SerializeField] private bool showDistricts = true;
    [SerializeField] private bool showConnections = true;
    [SerializeField] private bool showBounds = true;

    void OnDrawGizmos()
    {
        if (showBounds) DrawWorldBounds();
        if (showDistricts) DrawDistricts();
        if (showConnections) DrawConnections();
    }

    private void DrawWorldBounds()
    {
        // Find world configuration
        var worldConfig = FindObjectOfType<WorldConfiguration>();
        if (worldConfig == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(worldConfig.WorldCenter, worldConfig.WorldSize);

        #if UNITY_EDITOR
        UnityEditor.Handles.Label(worldConfig.WorldCenter, "WORLD BOUNDS");
        #endif
    }

    private void DrawDistricts()
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var districtQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<NodeId>());

        Gizmos.color = Color.cyan;

        districtQuery.ForEach((Entity entity, ref NodeId nodeId) =>
        {
            // Estimate district position (you'll need to implement proper positioning)
            Vector3 position = EstimateDistrictPosition(nodeId);
            Gizmos.DrawWireSphere(position, 5f);

            #if UNITY_EDITOR
            UnityEditor.Handles.Label(position, $"District {nodeId.Value}");
            #endif
        });
    }

    private void DrawConnections()
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var connectionQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<Connection>());

        Gizmos.color = Color.yellow;

        connectionQuery.ForEach((Entity entity, ref Connection connection) =>
        {
            Vector3 start = EstimateDistrictPosition(connection.StartDistrict);
            Vector3 end = EstimateDistrictPosition(connection.EndDistrict);

            Gizmos.DrawLine(start, end);
        });
    }

    // Placeholder - implement based on your district positioning system
    private Vector3 EstimateDistrictPosition(NodeId nodeId)
    {
        // This is a simplified example
        // In real implementation, you'd get position from district data
        return new Vector3(nodeId.Value * 20f, 0, 0);
    }
}
```

### **Step 2: Performance Monitor**

```csharp
// Monitor system performance in real-time
public class PerformanceMonitor : MonoBehaviour
{
    [SerializeField] private float updateInterval = 1f;
    [SerializeField] private bool showFPS = true;
    [SerializeField] private bool showMemory = true;
    [SerializeField] private bool showEntityCount = true;

    private float lastUpdateTime;
    private float fps;
    private long memoryUsage;
    private int entityCount;

    void Update()
    {
        if (Time.time - lastUpdateTime > updateInterval)
        {
            UpdateMetrics();
            lastUpdateTime = Time.time;
        }
    }

    void OnGUI()
    {
        if (!Application.isEditor) return;

        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 14;

        float yPos = 10f;

        if (showFPS)
        {
            GUI.Label(new Rect(10, yPos, 200, 20), $"FPS: {fps:F1}", style);
            yPos += 20;
        }

        if (showMemory)
        {
            GUI.Label(new Rect(10, yPos, 200, 20), $"Memory: {memoryUsage / 1024 / 1024} MB", style);
            yPos += 20;
        }

        if (showEntityCount)
        {
            GUI.Label(new Rect(10, yPos, 200, 20), $"Entities: {entityCount}", style);
            yPos += 20;
        }
    }

    private void UpdateMetrics()
    {
        // FPS calculation
        fps = 1f / Time.deltaTime;

        // Memory usage
        memoryUsage = System.GC.GetTotalMemory(false);

        // Entity count
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        entityCount = entityManager.GetAllEntities().Length;
    }
}
```

### **Step 3: Validation Visualizer**

```csharp
// Show validation results visually
public class ValidationVisualizer : MonoBehaviour
{
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color errorColor = Color.red;

    private ValidationSystem validationSystem;

    void Start()
    {
        validationSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<ValidationSystem>();
    }

    void OnDrawGizmos()
    {
        if (validationSystem == null) return;

        var results = validationSystem.GetValidationResults();

        foreach (var result in results)
        {
            DrawValidationResult(result);
        }
    }

    private void DrawValidationResult(ValidationResult result)
    {
        Color color = GetColorForSeverity(result.Severity);
        Gizmos.color = color;

        // Draw at the location of the issue
        Vector3 position = GetIssuePosition(result);

        switch (result.Type)
        {
            case ValidationType.MissingConnection:
                Gizmos.DrawWireSphere(position, 2f);
                break;

            case ValidationType.BiomeConflict:
                Gizmos.DrawCube(position, Vector3.one * 3f);
                break;

            case ValidationType.PerformanceIssue:
                Gizmos.DrawWireCube(position, Vector3.one * 4f);
                break;
        }

        #if UNITY_EDITOR
        UnityEditor.Handles.Label(position, result.Message);
        #endif
    }

    private Color GetColorForSeverity(ValidationSeverity severity)
    {
        switch (severity)
        {
            case ValidationSeverity.Info: return Color.blue;
            case ValidationSeverity.Warning: return warningColor;
            case ValidationSeverity.Error: return errorColor;
            default: return Color.gray;
        }
    }

    private Vector3 GetIssuePosition(ValidationResult result)
    {
        // Convert result location to world position
        // Implementation depends on your validation system
        return result.Position;
    }
}
```

---

## 🎨 **Advanced Visualization Features**

### **Navigation Graph Viewer**

```csharp
// Visualize AI navigation paths
public class NavigationVisualizer : MonoBehaviour
{
    [SerializeField] private Color nodeColor = Color.blue;
    [SerializeField] private Color edgeColor = Color.gray;
    [SerializeField] private Color pathColor = Color.green;
    [SerializeField] private float nodeSize = 0.5f;

    private NavigationSystem navigationSystem;
    private List<Vector3> currentPath = new List<Vector3>();

    void Start()
    {
        navigationSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<NavigationSystem>();
    }

    void OnDrawGizmos()
    {
        if (navigationSystem == null) return;

        DrawNavigationGraph();
        DrawCurrentPath();
    }

    private void DrawNavigationGraph()
    {
        var graph = navigationSystem.GetNavigationGraph();

        // Draw nodes
        Gizmos.color = nodeColor;
        foreach (var node in graph.Nodes)
        {
            Gizmos.DrawSphere(node.Position, nodeSize);
        }

        // Draw edges
        Gizmos.color = edgeColor;
        foreach (var edge in graph.Edges)
        {
            Gizmos.DrawLine(edge.Start.Position, edge.End.Position);
        }
    }

    private void DrawCurrentPath()
    {
        if (currentPath.Count < 2) return;

        Gizmos.color = pathColor;
        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            Gizmos.DrawSphere(currentPath[i], nodeSize * 1.5f);
        }

        // Draw final node
        Gizmos.DrawSphere(currentPath[currentPath.Count - 1], nodeSize * 1.5f);
    }

    // Call this when pathfinding occurs
    public void ShowPath(List<Vector3> path)
    {
        currentPath = new List<Vector3>(path);
    }
}
```

### **Biome Influence Map**

```csharp
// Show how biomes influence each other
public class BiomeInfluenceVisualizer : MonoBehaviour
{
    [SerializeField] private float resolution = 2f;
    [SerializeField] private float maxInfluence = 20f;
    [SerializeField] private Gradient influenceGradient;

    private PolarityFieldSystem polaritySystem;

    void Start()
    {
        polaritySystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<PolarityFieldSystem>();
    }

    void OnDrawGizmos()
    {
        if (polaritySystem == null) return;

        DrawInfluenceField();
        DrawInfluenceVectors();
    }

    private void DrawInfluenceField()
    {
        Bounds worldBounds = GetWorldBounds();

        for (float x = worldBounds.min.x; x < worldBounds.max.x; x += resolution)
        {
            for (float y = worldBounds.min.y; y < worldBounds.max.y; y += resolution)
            {
                Vector3 position = new Vector3(x, y, 0);

                // Calculate total influence at this point
                float totalInfluence = CalculateTotalInfluence(position);

                // Color based on influence strength
                float normalizedInfluence = Mathf.Clamp01(totalInfluence / maxInfluence);
                Color color = influenceGradient.Evaluate(normalizedInfluence);
                color.a = 0.4f;

                Gizmos.color = color;
                Gizmos.DrawCube(position, Vector3.one * resolution * 0.9f);
            }
        }
    }

    private void DrawInfluenceVectors()
    {
        Bounds worldBounds = GetWorldBounds();

        Gizmos.color = Color.white;

        for (float x = worldBounds.min.x; x < worldBounds.max.x; x += resolution * 2)
        {
            for (float y = worldBounds.min.y; y < worldBounds.max.y; y += resolution * 2)
            {
                Vector3 position = new Vector3(x, y, 0);

                // Calculate influence direction
                Vector3 influenceVector = CalculateInfluenceVector(position);

                if (influenceVector.magnitude > 0.1f)
                {
                    // Draw arrow showing influence direction
                    DrawArrow(position, influenceVector.normalized * 2f);
                }
            }
        }
    }

    private void DrawArrow(Vector3 start, Vector3 direction)
    {
        Vector3 end = start + direction;

        // Main line
        Gizmos.DrawLine(start, end);

        // Arrow head
        Vector3 right = Vector3.Cross(direction, Vector3.forward).normalized * 0.3f;
        Gizmos.DrawLine(end, end - direction * 0.3f + right);
        Gizmos.DrawLine(end, end - direction * 0.3f - right);
    }

    private float CalculateTotalInfluence(Vector3 position)
    {
        // Sum all polarity field influences
        float heat = polaritySystem.SampleHeatField(position);
        float cold = polaritySystem.SampleColdField(position);
        float sun = polaritySystem.SampleSunField(position);
        float moon = polaritySystem.SampleMoonField(position);

        return heat + cold + sun + moon;
    }

    private Vector3 CalculateInfluenceVector(Vector3 position)
    {
        // Calculate gradient of influence field
        float epsilon = 0.1f;

        float center = CalculateTotalInfluence(position);
        float right = CalculateTotalInfluence(position + Vector3.right * epsilon);
        float up = CalculateTotalInfluence(position + Vector3.up * epsilon);

        return new Vector3(right - center, up - center, 0).normalized;
    }
}
```

### **Real-time System Profiler**

```csharp
// Profile ECS system performance
public class SystemProfiler : MonoBehaviour
{
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private int maxSystemsToShow = 10;

    private Dictionary<string, SystemPerformanceData> systemData = new Dictionary<string, SystemPerformanceData>();
    private float lastUpdateTime;

    void Update()
    {
        if (Time.time - lastUpdateTime > updateInterval)
        {
            UpdateSystemData();
            lastUpdateTime = Time.time;
        }
    }

    void OnGUI()
    {
        if (!Application.isEditor) return;

        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 12;

        float yPos = 10f;

        GUI.Label(new Rect(10, yPos, 300, 20), "System Performance:", style);
        yPos += 25;

        // Sort by execution time (descending)
        var sortedSystems = systemData.OrderByDescending(kvp => kvp.Value.AverageTime);

        int count = 0;
        foreach (var kvp in sortedSystems)
        {
            if (count >= maxSystemsToShow) break;

            string systemName = kvp.Key;
            var data = kvp.Value;

            string label = $"{systemName}: {data.AverageTime:F3}ms ({data.CallCount} calls)";
            GUI.Label(new Rect(10, yPos, 400, 20), label, style);
            yPos += 20;

            count++;
        }
    }

    private void UpdateSystemData()
    {
        var world = World.DefaultGameObjectInjectionWorld;

        // Get all systems
        var systems = world.Systems;

        foreach (var system in systems)
        {
            string systemName = system.GetType().Name;

            if (!systemData.ContainsKey(systemName))
            {
                systemData[systemName] = new SystemPerformanceData();
            }

            var data = systemData[systemName];

            // Update performance data (you'd need to implement timing in your systems)
            // This is a simplified example
            data.Update(0.016f); // Placeholder timing
        }
    }
}

public class SystemPerformanceData
{
    public float TotalTime;
    public int CallCount;
    public float AverageTime => CallCount > 0 ? TotalTime / CallCount : 0;

    public void Update(float executionTime)
    {
        TotalTime += executionTime;
        CallCount++;
    }
}
```

---

## 🎮 **Editor Integration**

### **Custom Debug Window**

```csharp
using UnityEditor;
using UnityEngine;

public class MetVanDAMNDebugWindow : EditorWindow
{
    private bool showWorldStructure = true;
    private bool showBiomeFields = true;
    private bool showNavigation = false;
    private bool showPerformance = true;

    [MenuItem("MetVanDAMN/Debug Visualizer")]
    static void ShowWindow()
    {
        GetWindow<MetVanDAMNDebugWindow>("MetVanDAMN Debug");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Debug Visualization Controls", EditorStyles.boldLabel);

        showWorldStructure = EditorGUILayout.Toggle("World Structure", showWorldStructure);
        showBiomeFields = EditorGUILayout.Toggle("Biome Fields", showBiomeFields);
        showNavigation = EditorGUILayout.Toggle("Navigation Graph", showNavigation);
        showPerformance = EditorGUILayout.Toggle("Performance Monitor", showPerformance);

        EditorGUILayout.Space();

        if (GUILayout.Button("Refresh All Systems"))
        {
            RefreshDebugSystems();
        }

        if (GUILayout.Button("Clear Debug Data"))
        {
            ClearDebugData();
        }

        EditorGUILayout.Space();

        // Show current status
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

        var world = World.DefaultGameObjectInjectionWorld;
        if (world != null)
        {
            EditorGUILayout.LabelField($"Entities: {world.EntityManager.GetAllEntities().Length}");
            EditorGUILayout.LabelField($"Systems: {world.Systems.Count}");
        }
        else
        {
            EditorGUILayout.LabelField("No ECS World found", EditorStyles.helpBox);
        }
    }

    private void RefreshDebugSystems()
    {
        // Find and refresh all debug visualizers
        var visualizers = FindObjectsOfType<MonoBehaviour>()
            .Where(obj => obj is IDebugVisualizer);

        foreach (var visualizer in visualizers)
        {
            (visualizer as IDebugVisualizer).Refresh();
        }
    }

    private void ClearDebugData()
    {
        // Clear debug data from all systems
        var systems = FindObjectsOfType<MonoBehaviour>()
            .Where(obj => obj is IDebugSystem);

        foreach (var system in systems)
        {
            (system as IDebugSystem).ClearDebugData();
        }
    }
}

public interface IDebugVisualizer
{
    void Refresh();
}

public interface IDebugSystem
{
    void ClearDebugData();
}
```

### **Scene View Integration**

```csharp
// Add debug controls to scene view
[InitializeOnLoad]
public class SceneViewDebugControls
{
    static SceneViewDebugControls()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    static void OnSceneGUI(SceneView sceneView)
    {
        // Add debug controls to scene view
        Handles.BeginGUI();

        // Position controls in top-right corner
        Rect rect = new Rect(sceneView.position.width - 200, 10, 190, 100);

        GUILayout.BeginArea(rect);

        EditorGUILayout.LabelField("MetVanDAMN Debug", EditorStyles.boldLabel);

        if (GUILayout.Button("Toggle World Bounds"))
        {
            ToggleDebugVisualizer<WorldBoundsVisualizer>();
        }

        if (GUILayout.Button("Toggle Biome Fields"))
        {
            ToggleDebugVisualizer<BiomeFieldVisualizer>();
        }

        if (GUILayout.Button("Toggle Navigation"))
        {
            ToggleDebugVisualizer<NavigationVisualizer>();
        }

        if (GUILayout.Button("Clear All Gizmos"))
        {
            ClearAllDebugVisualizers();
        }

        GUILayout.EndArea();

        Handles.EndGUI();
    }

    static void ToggleDebugVisualizer<T>() where T : MonoBehaviour
    {
        var visualizer = Object.FindObjectOfType<T>();
        if (visualizer != null)
        {
            visualizer.enabled = !visualizer.enabled;
        }
        else
        {
            // Create visualizer if it doesn't exist
            var go = new GameObject(typeof(T).Name);
            go.AddComponent<T>();
        }
    }

    static void ClearAllDebugVisualizers()
    {
        var visualizers = Object.FindObjectsOfType<MonoBehaviour>()
            .Where(obj => obj is IDebugVisualizer);

        foreach (var visualizer in visualizers)
        {
            Object.DestroyImmediate(visualizer.gameObject);
        }
    }
}
```

---

## 🚀 **Next Steps**

**Ready to debug like a pro?**
- **[Validation Tools](../testing-debugging/validation.md)** - Automated problem detection
- **[Performance Optimization](../advanced/performance.md)** - Keep your game running fast
- **[Troubleshooting Guide](../testing-debugging/troubleshooting.md)** - Fix common issues

**Need more debug help?**
- Check the [debug scenes](../../Assets/Scenes/) for examples
- Look at [validation tools](../testing-debugging/validation.md) for automated checks
- Join [Unity ECS forums](https://forum.unity.com/forums/entities.158/) for community support

---

*"Debugging without visualization is like trying to solve a puzzle in the dark. Turn on the lights and see what you're working with!"*

**🍑 Happy Debugging! 🍑**
