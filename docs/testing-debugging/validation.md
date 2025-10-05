# ✅ Validation Tools
## *Checking Your Worlds for Problems Automatically*

> **"Validation is like having a friendly ghost that whispers 'this might not work' before you ship it to players."**

[![Validation](https://img.shields.io/badge/Validation-Tools-blue.svg)](validation.md)
[![Automated](https://img.shields.io/badge/Automated-Testing-green.svg)](validation.md)

---

## 🎯 **What is Validation?**

**Validation** automatically checks your MetVanDAMN worlds for common problems. Instead of manually testing everything, validation tools:

- 🔍 **Detect Issues** - Find problems before players do
- ⚡ **Run Fast** - Check thousands of entities quickly
- 📊 **Report Clearly** - Show exactly what's wrong and how to fix it
- 🚀 **Integrate Easily** - Work in editor and during builds

**Good validation catches bugs before they reach players!**

---

## 🏗️ **Built-in Validation Tools**

### **World Structure Validation**

```csharp
// Validates basic world structure
public static class WorldValidation
{
    public static ValidationResult ValidateWorld(EntityManager em)
    {
        var result = new ValidationResult();

        // Check for required world entities
        bool hasWorldConfig = false;
        bool hasDistricts = false;

        foreach (var entity in em.GetAllEntities())
        {
            if (em.HasComponent<WorldConfiguration>(entity))
            {
                hasWorldConfig = true;
            }

            if (em.HasComponent<DistrictData>(entity))
            {
                hasDistricts = true;
            }
        }

        if (!hasWorldConfig)
        {
            result.AddError("No WorldConfiguration entity found");
        }

        if (!hasDistricts)
        {
            result.AddWarning("No districts found - world may be empty");
        }

        return result;
    }
}
```

### **Connectivity Validation**

```csharp
// Checks if all rooms are reachable
public static class ConnectivityValidation
{
    public static ValidationResult ValidateConnectivity(EntityManager em)
    {
        var result = new ValidationResult();

        // Build connectivity graph
        var graph = BuildRoomGraph(em);

        // Find unreachable rooms
        var unreachable = FindUnreachableRooms(graph);

        foreach (var room in unreachable)
        {
            result.AddError($"Room {room} is not reachable from hub");
        }

        // Check for dead ends (rooms with no exits)
        var deadEnds = FindDeadEndRooms(graph);
        if (deadEnds.Count > graph.Nodes.Count * 0.3f) // More than 30% dead ends
        {
            result.AddWarning("Too many dead-end rooms - may feel maze-like");
        }

        return result;
    }

    private static RoomGraph BuildRoomGraph(EntityManager em)
    {
        var graph = new RoomGraph();

        foreach (var (room, entity) in em.GetAllEntities()
            .Where(e => em.HasComponent<RoomData>(e)))
        {
            var roomData = em.GetComponentData<RoomData>(entity);
            graph.AddRoom(roomData.Id, roomData.Connections);
        }

        return graph;
    }
}
```

### **Biome Balance Validation**

```csharp
// Checks biome distribution and balance
public static class BiomeValidation
{
    public static ValidationResult ValidateBiomeBalance(EntityManager em)
    {
        var result = new ValidationResult();

        var biomeCounts = new Dictionary<BiomeType, int>();

        // Count biomes in world
        foreach (var entity in em.GetAllEntities())
        {
            if (em.HasComponent<BiomeData>(entity))
            {
                var biome = em.GetComponentData<BiomeData>(entity);
                if (!biomeCounts.ContainsKey(biome.Type))
                    biomeCounts[biome.Type] = 0;
                biomeCounts[biome.Type]++;
            }
        }

        // Check for biome balance
        int totalRooms = biomeCounts.Values.Sum();
        foreach (var kvp in biomeCounts)
        {
            float percentage = (float)kvp.Value / totalRooms;

            if (percentage > 0.8f)
            {
                result.AddWarning($"{kvp.Key} biome dominates {percentage:P0} of world - may feel repetitive");
            }
            else if (percentage < 0.05f)
            {
                result.AddInfo($"{kvp.Key} biome is rare ({percentage:P0}) - consider increasing");
            }
        }

        // Check for required biomes
        if (!biomeCounts.ContainsKey(BiomeType.Hub))
        {
            result.AddError("No hub biome found - players need a starting point");
        }

        return result;
    }
}
```

---

## 🚀 **Quick Validation Setup (5 Minutes)**

### **Step 1: Create Validation Runner**

```csharp
using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;

public class ValidationRunner : MonoBehaviour
{
    [ContextMenu("Run All Validations")]
    public void RunValidations()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;

        Debug.Log("🔍 Running MetVanDAMN Validations...");

        var results = new List<ValidationResult>();

        // Run all validation checks
        results.Add(WorldValidation.ValidateWorld(em));
        results.Add(ConnectivityValidation.ValidateConnectivity(em));
        results.Add(BiomeValidation.ValidateBiomeBalance(em));
        results.Add(PerformanceValidation.ValidatePerformance(em));

        // Report results
        ReportResults(results);
    }

    private void ReportResults(List<ValidationResult> results)
    {
        int totalErrors = 0;
        int totalWarnings = 0;
        int totalInfo = 0;

        foreach (var result in results)
        {
            totalErrors += result.Errors.Count;
            totalWarnings += result.Warnings.Count;
            totalInfo += result.Info.Count;

            // Log details
            foreach (var error in result.Errors)
                Debug.LogError($"❌ {error}");

            foreach (var warning in result.Warnings)
                Debug.LogWarning($"⚠️ {warning}");

            foreach (var info in result.Info)
                Debug.Log($"ℹ️ {info}");
        }

        Debug.Log($"📊 Validation Complete: {totalErrors} errors, {totalWarnings} warnings, {totalInfo} info");
    }
}
```

### **Step 2: Add to Scene**

1. Create empty GameObject called "Validators"
2. Add `ValidationRunner` component
3. Generate a world
4. Right-click component → **Run All Validations**

### **Step 3: Automated Validation**

```csharp
// Run validation every time world generates
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class AutoValidationSystem : SystemBase
{
    private bool worldGenerated = false;

    protected override void OnUpdate()
    {
        // Check if world was just generated
        if (!worldGenerated)
        {
            foreach (var config in SystemAPI.Query<RefRO<WorldConfiguration>>())
            {
                worldGenerated = true;
                RunValidation();
                break;
            }
        }
    }

    private void RunValidation()
    {
        var em = EntityManager;

        // Quick validation checks
        var result = WorldValidation.ValidateWorld(em);

        if (result.Errors.Count > 0)
        {
            Debug.LogError("🚨 World validation failed!");
            foreach (var error in result.Errors)
                Debug.LogError($"  {error}");
        }
        else
        {
            Debug.Log("✅ World validation passed");
        }
    }
}
```

---

## 🎮 **Advanced Validation Types**

### **Performance Validation**

```csharp
public static class PerformanceValidation
{
    public static ValidationResult ValidatePerformance(EntityManager em)
    {
        var result = new ValidationResult();

        // Check entity count
        int entityCount = em.GetAllEntities().Length;
        if (entityCount > 10000)
        {
            result.AddWarning($"{entityCount} entities may impact performance");
        }

        // Check for missing LOD components
        int lodCount = 0;
        foreach (var entity in em.GetAllEntities())
        {
            if (em.HasComponent<LODLevel>(entity))
                lodCount++;
        }

        float lodPercentage = (float)lodCount / entityCount;
        if (lodPercentage < 0.5f)
        {
            result.AddWarning($"Only {lodPercentage:P0} of entities have LOD - consider adding level-of-detail");
        }

        // Check for burst-compiled systems
        // (This would require reflection to check system attributes)

        return result;
    }
}
```

### **Content Validation**

```csharp
public static class ContentValidation
{
    public static ValidationResult ValidateContent(EntityManager em)
    {
        var result = new ValidationResult();

        // Check for required content
        bool hasPlayerSpawn = false;
        bool hasEnemies = false;
        bool hasCollectibles = false;

        foreach (var entity in em.GetAllEntities())
        {
            if (em.HasComponent<PlayerSpawn>(entity)) hasPlayerSpawn = true;
            if (em.HasComponent<EnemyTag>(entity)) hasEnemies = true;
            if (em.HasComponent<Collectible>(entity)) hasCollectibles = true;
        }

        if (!hasPlayerSpawn)
            result.AddError("No player spawn point found");

        if (!hasEnemies)
            result.AddWarning("No enemies found - world may feel empty");

        if (!hasCollectibles)
            result.AddInfo("No collectibles found - consider adding rewards");

        // Check room variety
        var roomTypes = new HashSet<RoomType>();
        foreach (var entity in em.GetAllEntities())
        {
            if (em.HasComponent<RoomData>(entity))
            {
                var roomData = em.GetComponentData<RoomData>(entity);
                roomTypes.Add(roomData.Type);
            }
        }

        if (roomTypes.Count < 3)
        {
            result.AddInfo($"Only {roomTypes.Count} room types - consider more variety");
        }

        return result;
    }
}
```

### **Accessibility Validation**

```csharp
public static class AccessibilityValidation
{
    public static ValidationResult ValidateAccessibility(EntityManager em)
    {
        var result = new ValidationResult();

        // Check color contrast (if using UI)
        // Check font sizes
        // Check control schemes

        // Check for alternative paths
        var graph = ConnectivityValidation.BuildRoomGraph(em);
        var alternativePaths = FindAlternativePaths(graph);

        if (alternativePaths.Count < graph.Nodes.Count * 0.5f)
        {
            result.AddWarning("Limited alternative paths - may frustrate players");
        }

        // Check difficulty progression
        var difficultyCurve = AnalyzeDifficultyProgression(em);
        if (!IsDifficultyProgressionValid(difficultyCurve))
        {
            result.AddWarning("Difficulty may not progress smoothly");
        }

        return result;
    }
}
```

---

## 🔧 **Custom Validation Rules**

### **Creating Custom Validators**

```csharp
// Interface for custom validators
public interface IWorldValidator
{
    string Name { get; }
    ValidationResult Validate(EntityManager em);
}

// Custom validator example
public class CustomGameplayValidator : IWorldValidator
{
    public string Name => "Custom Gameplay Rules";

    public ValidationResult Validate(EntityManager em)
    {
        var result = new ValidationResult();

        // Check your game's specific rules
        int powerUpCount = 0;
        int trapCount = 0;

        foreach (var entity in em.GetAllEntities())
        {
            if (em.HasComponent<PowerUp>(entity)) powerUpCount++;
            if (em.HasComponent<Trap>(entity)) trapCount++;
        }

        // Custom rule: balance power-ups and traps
        float ratio = powerUpCount > 0 ? (float)trapCount / powerUpCount : 0;
        if (ratio < 0.5f)
        {
            result.AddWarning("Too few traps compared to power-ups - may be too easy");
        }
        else if (ratio > 2f)
        {
            result.AddWarning("Too many traps compared to power-ups - may be too hard");
        }

        return result;
    }
}
```

### **Validation Result System**

```csharp
public class ValidationResult
{
    public List<string> Errors { get; } = new List<string>();
    public List<string> Warnings { get; } = new List<string>();
    public List<string> Info { get; } = new List<string>();

    public bool HasErrors => Errors.Count > 0;
    public bool HasWarnings => Warnings.Count > 0;

    public void AddError(string message) => Errors.Add(message);
    public void AddWarning(string message) => Warnings.Add(message);
    public void AddInfo(string message) => Info.Add(message);

    public void Merge(ValidationResult other)
    {
        Errors.AddRange(other.Errors);
        Warnings.AddRange(other.Warnings);
        Info.AddRange(other.Info);
    }
}
```

---

## 🎯 **Validation in Editor**

### **Editor Window**

```csharp
using UnityEditor;
using UnityEngine;

public class ValidationWindow : EditorWindow
{
    [MenuItem("MetVanDAMN/Validation")]
    static void ShowWindow()
    {
        GetWindow<ValidationWindow>("World Validation");
    }

    void OnGUI()
    {
        GUILayout.Label("MetVanDAMN World Validation", EditorStyles.boldLabel);

        if (GUILayout.Button("Run Validation"))
        {
            RunValidation();
        }

        if (GUILayout.Button("Auto-Validate on Play"))
        {
            ToggleAutoValidation();
        }
    }

    private void RunValidation()
    {
        // Run validation and display results in window
        var em = World.DefaultGameObjectInjectionWorld?.EntityManager;
        if (em == null)
        {
            Debug.LogError("No ECS world found - start play mode first");
            return;
        }

        var result = WorldValidation.ValidateWorld(em);
        DisplayResults(result);
    }

    private void DisplayResults(ValidationResult result)
    {
        // Display in editor window
        EditorGUILayout.LabelField($"Errors: {result.Errors.Count}");
        foreach (var error in result.Errors)
        {
            EditorGUILayout.LabelField($"❌ {error}", EditorStyles.miniLabel);
        }

        EditorGUILayout.LabelField($"Warnings: {result.Warnings.Count}");
        foreach (var warning in result.Warnings)
        {
            EditorGUILayout.LabelField($"⚠️ {warning}", EditorStyles.miniLabel);
        }
    }
}
```

### **Scene Validation**

```csharp
[CustomEditor(typeof(ValidationRunner))]
public class ValidationRunnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var runner = (ValidationRunner)target;

        if (GUILayout.Button("Validate Current Scene"))
        {
            runner.RunValidations();
        }

        if (GUILayout.Button("Validate Prefabs"))
        {
            ValidateAllPrefabs();
        }
    }

    private void ValidateAllPrefabs()
    {
        // Find and validate all MetVanDAMN prefabs
        var prefabs = AssetDatabase.FindAssets("t:Prefab")
            .Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(obj => obj.GetComponentInChildren<MetVanDAMNComponent>() != null);

        foreach (var prefab in prefabs)
        {
            // Validate prefab
            Debug.Log($"Validating prefab: {prefab.name}");
        }
    }
}
```

---

## 🚀 **CI/CD Integration**

### **Build Validation**

```csharp
// Validate during build process
public class BuildValidator
{
    [MenuItem("MetVanDAMN/Validate Build")]
    static void ValidateBuild()
    {
        // Run comprehensive validation
        var results = RunAllValidations();

        if (results.HasErrors)
        {
            // Fail build
            throw new System.Exception($"Build validation failed: {results.Errors.Count} errors");
        }

        Debug.Log("✅ Build validation passed");
    }

    private static ValidationResult RunAllValidations()
    {
        var combined = new ValidationResult();

        // Run all validators
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;

        combined.Merge(WorldValidation.ValidateWorld(em));
        combined.Merge(ConnectivityValidation.ValidateConnectivity(em));
        combined.Merge(BiomeValidation.ValidateBiomeBalance(em));
        combined.Merge(PerformanceValidation.ValidatePerformance(em));
        combined.Merge(ContentValidation.ValidateContent(em));

        return combined;
    }
}
```

### **Test Integration**

```csharp
[TestFixture]
public class ValidationTests
{
    [Test]
    public void World_GeneratesWithoutErrors()
    {
        // Generate test world
        var world = TestWorldGenerator.GenerateTestWorld();

        // Validate
        var result = WorldValidation.ValidateWorld(world.EntityManager);

        Assert.IsFalse(result.HasErrors, "World should generate without errors");
        Assert.LessOrEqual(result.Warnings.Count, 2, "Should have minimal warnings");
    }

    [Test]
    public void World_IsFullyConnected()
    {
        var world = TestWorldGenerator.GenerateTestWorld();
        var result = ConnectivityValidation.ValidateConnectivity(world.EntityManager);

        Assert.IsFalse(result.HasErrors, "World should be fully connected");
    }
}
```

---

## 🎯 **Best Practices**

### **Validation Timing**
- **Editor Time** - Quick checks while designing
- **Play Mode** - Full validation when testing
- **Build Time** - Comprehensive checks before release
- **Runtime** - Lightweight checks during gameplay

### **Validation Scope**
- **Unit Validation** - Individual components/systems
- **Integration Validation** - How systems work together
- **World Validation** - Complete generated worlds
- **Performance Validation** - Runtime performance checks

### **Error Handling**
- **Clear Messages** - Explain what's wrong and how to fix
- **Severity Levels** - Errors (blockers), Warnings (concerns), Info (suggestions)
- **Context** - Show which entities/rooms have issues
- **Recovery** - Suggest automatic fixes when possible

---

## 🚀 **Next Steps**

**Ready to validate your worlds?**
- **[Debug Tools](debug-tools.md)** - Visual debugging and inspection
- **[Troubleshooting](troubleshooting.md)** - Fix common problems
- **[Performance Optimization](../advanced/performance.md)** - Speed up validation

**Need help with validation?**
- Check the [built-in validators](../../Assets/Scripts/Validation/) in the project
- Look at [validation examples](../../Assets/Scenes/Validation/) scenes
- Join [GitHub Discussions](https://github.com/jmeyer1980/TWG-MetVanDAMN/discussions)

---

*"Validation doesn't make perfect worlds - it makes better worlds by catching problems early."*

**🍑 Happy Validating! 🍑**
