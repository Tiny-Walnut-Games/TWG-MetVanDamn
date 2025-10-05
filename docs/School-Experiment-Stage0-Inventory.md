# School Experiment - Stage 0: Inventory Tool

## Overview

The **InventoryCollector** is a Unity Editor tool that implements Stage 0 of the 'school' experiment workflow. It scans the Unity project repository to enumerate all 'faculty surfaces' (Unity scripts, prefabs, relevant data assets, entrypoints) and outputs a deterministic `inventory.json` file.

## Features

- **Unity 6 Compatible**: Operates within Unity 6 (2023+) EditorContext using Unity APIs
- **Asset Discovery**: Uses `AssetDatabase`, `ScriptableObject`, and other Unity APIs for comprehensive asset scanning
- **Faculty Surface Classification**: Automatically categorizes assets into meaningful types
- **Hash Stamping**: Generates deterministic hashes for inventory validation
- **Interactive UI**: User-friendly Unity Editor window interface
- **Comprehensive Output**: Detailed JSON inventory with metadata and analysis

## Usage

### Opening the Tool

1. In Unity Editor, navigate to: **Tools → School → Generate Inventory**
2. The "School Inventory" window will open

### Generating Inventory

1. Click the **"🔍 Scan Faculty Surfaces"** button
2. The tool will scan all assets in the project
3. Progress will be displayed during scanning
4. Results will be shown in the window upon completion
5. Inventory file will be saved to `assets/experiments/school/inventory.json`

### Output Location

The inventory is saved to:
```
assets/experiments/school/inventory.json
```

## Faculty Surface Types

The tool automatically detects and classifies these asset types:

- **Scripts**: C# MonoScript files (.cs)
- **Prefabs**: GameObject prefabs (.prefab)
- **ScriptableObjects**: Data asset files
- **Materials**: Material assets (.mat)
- **Textures**: Texture2D assets
- **Shaders**: Shader files (.shader)
- **Scenes**: Unity scene files (.unity)
- **Audio**: AudioClip assets
- **Meshes**: 3D model assets

## JSON Output Structure

```json
{
  "Timestamp": "2025-09-06T01:29:05.966548Z",
  "Hash": "d0865a96189ad5c3",
  "UnityVersion": "6000.2.0f1",
  "ProjectPath": "/path/to/project/Assets",
  "FacultySurfaces": [
    {
      "Path": "Assets/Path/To/Asset.cs",
      "Type": "Script",
      "Guid": "unity-asset-guid",
      "AssetType": "MonoScript",
      "FileSize": 1024,
      "LastModified": "2024-09-06T01:23:45.123Z",
      "Tags": ["Component", "EditorTool"]
    }
  ]
}
```

### Field Descriptions

- **Timestamp**: ISO 8601 timestamp of when inventory was generated
- **Hash**: SHA-256 hash of all faculty surfaces for integrity verification
- **UnityVersion**: Version of Unity used to generate the inventory
- **ProjectPath**: Absolute path to the Unity project Assets folder
- **FacultySurfaces**: Array of all discovered faculty surfaces

#### Faculty Surface Fields

- **Path**: Relative path from Assets folder
- **Type**: Classified surface type (Script, Prefab, etc.)
- **Guid**: Unity's internal asset GUID
- **AssetType**: Unity's internal asset type name
- **FileSize**: File size in bytes
- **LastModified**: ISO 8601 timestamp of last modification
- **Tags**: Array of additional classification tags

## Integration with GitHub

The tool is designed to work within Unity Editor and does not require direct GitHub API integration. However, the deterministic hash output enables:

- **Change Detection**: Compare hashes between inventory generations
- **Version Control**: Track faculty surface changes over time
- **CI/CD Integration**: Automated inventory validation in workflows

## Performance

- **Scan Speed**: Optimized for large Unity projects
- **Progress Feedback**: Real-time progress updates during scanning
- **Memory Efficient**: Streams asset processing to minimize memory usage
- **Non-blocking**: Async operations prevent Unity Editor freezing

## Platform Compatibility

- **Unity 6+**: Requires Unity 6 (2023+) or later
- **Editor Only**: Tool runs exclusively in Unity Editor context
- **Cross-Platform**: Works on Windows, macOS, and Linux Unity installations

## File Structure

```
Assets/TWG/TLDA/Tools/School/Editor/
├── InventoryCollector.cs          # Main Unity Editor tool
└── InventoryCollector.cs.meta     # Unity metadata file

assets/experiments/school/
└── inventory.json                 # Generated inventory output
```

## Implementation Details

The tool follows Unity Editor best practices:

- **EditorWindow**: Extends Unity's EditorWindow for UI
- **MenuItem**: Accessible via Unity's top menu bar
- **AssetDatabase**: Uses Unity's official API for asset discovery
- **JsonUtility**: Uses Unity's built-in JSON serialization
- **Async/Await**: Non-blocking scanning operations
- **Progress Bars**: Native Unity progress feedback

## Troubleshooting

### Common Issues

1. **"Output file not found"**: Run inventory scan first
2. **Scan appears frozen**: Check Unity Console for progress updates
3. **Large project scanning**: Allow extra time for projects with many assets
4. **Permission errors**: Ensure write access to output directory

### Debug Information

The tool logs detailed information to Unity Console:
- Scan progress updates
- Asset discovery statistics
- Error messages with specific file paths
- Completion confirmation with summary

## Development

The tool is designed for extension and customization:

- **Faculty Surface Types**: Add new asset classifications in `ClassifyAsset()`
- **Output Format**: Modify JSON structure in data classes
- **UI Elements**: Enhance EditorWindow interface
- **Scanning Logic**: Customize asset discovery algorithms

## License

This tool is part of the TWG-TLDA project and follows the project's licensing terms.