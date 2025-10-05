# Story Test - Virginized Version

This is a clean, generic version of the Story Test framework that can be used with any Unity 6+ C#9+ Git/HuggingFace repository.

## 🎭 What Was Created

### Core Framework Files
- `Runtime/StoryTest.cs` - Base class for creating comprehensive validation suites
- `Runtime/StoryIgnoreAttribute.cs` - Attribute for excluding code from validation
- `Editor/StoryIntegrityValidator.cs` - Automatic code quality validation
- `ExampleGameStoryTest.cs` - Complete example implementation

### Unity Integration Files
- `Runtime/StoryTest.Runtime.asmdef` - Runtime assembly definition
- `Editor/StoryTest.Editor.asmdef` - Editor assembly definition
- `package.json` - Unity package manifest for easy installation

### Documentation & Setup
- `README.md` - Comprehensive setup and usage guide
- `.gitignore` - Git ignore rules for Unity projects

## 🚀 Key Differences from Original

### Removed MetVanDAMN/QuadRig Dependencies
- No references to specific game frameworks
- Generic namespace (`StoryTest` instead of `TinyWalnutGames.StoryTest`)
- Configurable namespace prefix for validation

### Simplified Architecture
- Single base `StoryTest` class instead of multiple specialized tests
- Configurable validation rules via `StoryIntegrityConfig`
- Easy extension points for custom validation

### Unity 6+ C#9+ Optimized
- Uses modern C# features (nullable reference types, pattern matching)
- Unity 6.0+ assembly definitions
- Compatible with any render pipeline

### Framework Agnostic
- No ECS, DOTS, or Unity-specific framework assumptions
- Works with MonoBehaviour, ECS, or any architecture
- Configurable for any project structure

## 🛠️ How to Use in Any Project

1. **Copy the `StoryTest-Generic` folder** to your Unity project's `Assets` folder
2. **Configure the namespace** in `StoryIntegrityValidator.cs`:
   ```csharp
   public static string RootNamespacePrefix { get; set; } = "YourCompany.YourProject";
   ```
3. **Run validation** via `Tools → Story Test → Run Integrity Validation`
4. **Create custom Story Tests** by inheriting from the `StoryTest` base class

## 🎯 Validation Rules

The framework automatically validates:
- **Phantom Props**: Serialized fields written but never read
- **Cold Public Methods**: Public methods never called
- **Hollow Enums**: Enums declared but never used
- **Premature Celebrations**: Methods that always return true/false

Custom Story Tests validate:
- System integration and data flow
- Component dependencies
- User experience workflows
- Performance requirements
- Platform-specific behavior

## 📦 Distribution Ready

This virginized version is ready to be:
- Added to any Unity project
- Published as a Unity package
- Shared across teams
- Extended for specific project needs
- Integrated into CI/CD pipelines

The framework maintains the core Story Test philosophy - ensuring all components work together harmoniously like actors in a stage play - while being completely generic and reusable.
