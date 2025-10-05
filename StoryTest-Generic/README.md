# Story Test - Generic Unity Validation Framework

A comprehensive validation framework for Unity 6+ C#9+ projects that ensures code quality and catches common issues before they become problems. Story Tests validate that all components work together harmoniously, like actors in a stage play delivering a seamless performance.

## 🎭 What is a Story Test?

Story Tests are comprehensive validation suites that ensure your game's systems work together perfectly. They validate the "story" - that the mental model matches the actual implementation. Just like a stage play where every actor knows their lines and hits their marks, Story Tests ensure no plot holes exist in your codebase.

## 🚀 Quick Start

### 1. Add to Your Unity Project

Copy the `StoryTest-Generic` folder to your Unity project's `Assets` folder:

```
YourProject/
├── Assets/
│   ├── StoryTest-Generic/
│   │   ├── Runtime/
│   │   │   ├── StoryTest.cs
│   │   │   ├── StoryIgnoreAttribute.cs
│   │   │   └── StoryTest.Runtime.asmdef
│   │   ├── Editor/
│   │   │   ├── StoryIntegrityValidator.cs
│   │   │   └── StoryTest.Editor.asmdef
│   │   └── ExampleGameStoryTest.cs
```

### 2. Configure for Your Project

Edit `StoryTest-Generic/Editor/StoryIntegrityValidator.cs` and update the configuration:

```csharp
public static class StoryIntegrityConfig
{
    // Change this to match your project's root namespace
    public static string RootNamespacePrefix { get; set; } = "MyCompany.MyGame";

    // Add your custom Unity lifecycle methods if needed
    public static readonly HashSet<string> UnityMagicMethods = new()
    {
        "Awake", "Start", "OnEnable", "OnDisable", "OnDestroy", "Update", "LateUpdate", "FixedUpdate",
        "OnGUI", "Reset", "OnApplicationQuit", "OnApplicationFocus", "OnApplicationPause",
        "OnDrawGizmos", "OnDrawGizmosSelected", "OnValidate", "OnSceneGUI",
        // Add your custom methods here
        "OnGameStart", "OnLevelLoad"
    };
}
```

### 3. Run Integrity Validation

Open Unity and go to: **Tools → Story Test → Run Integrity Validation**

This will scan your code for:
- **Phantom Props**: Serialized fields that are written but never read
- **Cold Public Methods**: Public methods that are never called
- **Hollow Enums**: Enums that are declared but never used
- **Premature Celebrations**: Methods that always return true/false (likely incomplete)

### 4. Create Your First Story Test

Create a new C# script that inherits from `StoryTest`:

```csharp
using StoryTest;

public class MyGameStoryTest : StoryTest
{
    protected override int TotalTestPhases => 3;

    protected override string GetPhaseName(int phaseIndex)
    {
        return phaseIndex switch
        {
            0 => "Initialize Game World",
            1 => "Test Player Systems",
            2 => "Validate Win Conditions",
            _ => "Unknown Phase"
        };
    }

    protected override void ExecuteTestPhase(int phaseIndex)
    {
        switch (phaseIndex)
        {
            case 0: InitializeWorld(); break;
            case 1: TestPlayer(); break;
            case 2: ValidateWinConditions(); break;
        }
    }

    protected override bool PerformFinalValidation()
    {
        // Return true if all systems work correctly
        return IsWorldInitialized && PlayerWorks && CanWinGame;
    }
}
```

Add this component to a GameObject in your scene and hit Play!

## 📋 What Story Tests Validate

### Code Integrity (Automatic)
- **Phantom Props**: Fields marked `[SerializeField]` that are assigned but never read
- **Cold Methods**: Public methods that are never called anywhere in the codebase
- **Hollow Enums**: Enum types that are defined but never used in method signatures
- **Premature Celebrations**: Boolean methods that always return the same value

### Runtime Behavior (Custom)
- System integration and data flow
- Component dependencies and initialization order
- Performance requirements and frame rate targets
- User experience flows and edge cases
- Save/load functionality and data persistence
- Multiplayer synchronization (if applicable)
- Platform-specific behavior validation

## 🛠️ Advanced Configuration

### Ignoring False Positives

Use the `[StoryIgnore]` attribute to exclude code from validation:

```csharp
public class MyComponent : MonoBehaviour
{
    [StoryIgnore("This field is set by Unity's serialization but read via reflection")]
    [SerializeField] private string internalData;

    [StoryIgnore("This method is called by external plugins via SendMessage")]
    public void PluginCallback() { /* ... */ }
}
```

### CI/CD Integration

Add to your CI pipeline:

```yaml
# GitHub Actions example
- name: Run Story Test Validation
  run: |
    /Applications/Unity/Hub/Editor/6000.2.0f1/Editor/Unity.exe \
      -batchmode \
      -executeMethod StoryTest.Editor.StoryIntegrityValidator.RunFromCI \
      -logFile unity_storytest.log \
      -quit

# Set environment variables for failure conditions
- name: Set Story Test Environment
  run: |
    echo "STORYTEST_FAIL_ON_VIOLATION=1" >> $GITHUB_ENV
    echo "STORYTEST_SEVERITY_THRESHOLD=Warning" >> $GITHUB_ENV
```

### Custom Validation Rules

Extend the validator by modifying `StoryIntegrityValidator.cs`:

```csharp
// Add custom validation logic
private static void AnalyzeType(Type type, StoryReport report, /* ... */)
{
    // Your custom validation rules here
    if (type.Name.EndsWith("Manager") && !type.GetMethods().Any(m => m.Name.Contains("Update")))
    {
        report.ColdPublicMethods.Add($"{type.FullName} - Manager without Update method");
    }
}
```

## 🎯 Best Practices

### Writing Effective Story Tests

1. **Focus on Integration**: Test how components work together, not individual units
2. **Validate User Journeys**: Ensure complete user workflows function end-to-end
3. **Test Edge Cases**: Include scenarios that are hard to reproduce manually
4. **Performance Validation**: Include frame rate and memory usage checks
5. **Platform Coverage**: Test platform-specific code paths

### Code Quality Guidelines

1. **Regular Validation**: Run integrity checks before commits and deployments
2. **Address Warnings**: Fix phantom props and cold methods promptly
3. **Document Ignored Code**: Always provide reasons for `[StoryIgnore]` attributes
4. **Review Enums**: Ensure all enums serve a purpose and are used appropriately
5. **Test Completeness**: Use boolean methods appropriately - avoid premature celebrations

### Example Story Test Structure

```csharp
public class CompleteGameStoryTest : StoryTest
{
    // Configuration
    [SerializeField] private PlayerController player;
    [SerializeField] private LevelManager levelManager;

    // Test state tracking
    private bool playerSpawned;
    private bool levelLoaded;
    private bool canCompleteLevel;

    protected override int TotalTestPhases => 4;

    protected override void OnStoryTestBegin()
    {
        // Initialize test state
        playerSpawned = false;
        levelLoaded = false;
        canCompleteLevel = false;
    }

    protected override void ExecuteTestPhase(int phaseIndex)
    {
        switch (phaseIndex)
        {
            case 0: TestPlayerSpawning(); break;
            case 1: TestLevelLoading(); break;
            case 2: TestGameplayLoop(); break;
            case 3: TestWinConditions(); break;
        }
    }

    protected override bool PerformFinalValidation()
    {
        return playerSpawned && levelLoaded && canCompleteLevel &&
               player.health > 0 && levelManager.IsLevelComplete();
    }
}
```

## 🔧 Troubleshooting

### Common Issues

**"No assemblies found with namespace prefix"**
- Update `StoryIntegrityConfig.RootNamespacePrefix` to match your project's namespace

**"Phantom props detected for [field]"**
- Either read the field somewhere in code, or add `[StoryIgnore]` with a reason

**"Cold public methods detected"**
- Ensure public methods are actually called, or make them private, or add `[StoryIgnore]`

**Story Test doesn't run**
- Ensure the StoryTest component is enabled and `autoRunStoryTest` is true
- Check console for error messages

### Debug Mode

Enable detailed logging by setting `enableDebugLogging = true` on your StoryTest component.

## 📊 Integration with Version Control

### Git Integration

Add validation to your pre-commit hooks:

```bash
#!/bin/bash
# .git/hooks/pre-commit

# Run Unity in batch mode to validate
/Applications/Unity/Hub/Editor/6000.2.0f1/Editor/Unity.exe \
  -batchmode \
  -executeMethod StoryTest.Editor.StoryIntegrityValidator.RunFromCI \
  -quit

# Check exit code
if [ $? -ne 0 ]; then
  echo "Story Test validation failed. Please fix issues before committing."
  exit 1
fi
```

### GitHub Actions

```yaml
name: Story Test Validation
on: [push, pull_request]

jobs:
  validate:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - uses: game-ci/unity-builder@v3
      with:
        unityVersion: 6000.2.0f1
        customParameters: >
          -batchmode
          -executeMethod StoryTest.Editor.StoryIntegrityValidator.RunFromCI
          -logFile storytest.log
          -quit
```

## 🤝 Contributing

This is a generic, reusable framework. When customizing for your project:

1. Keep the core `StoryTest` base class unchanged
2. Extend configuration in `StoryIntegrityConfig` rather than modifying core logic
3. Add project-specific validation rules as extension methods
4. Document your customizations for team members

## 📄 License

This framework is provided as-is for Unity developers. Feel free to modify and distribute within your projects.

---

**Remember**: Story Tests ensure your code tells a complete, coherent story. No plot holes, no missing scenes, no actors forgetting their lines. Happy testing! 🎭
