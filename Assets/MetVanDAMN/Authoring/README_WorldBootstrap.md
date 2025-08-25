# WorldBootstrap Procedural Generation System

The WorldBootstrap system provides **One‑District Authoring → Full Random World Generation**, eliminating the need for manual DistrictAuthoring placement while enabling complete top‑down procedural generation.

## Overview

Instead of manually placing individual `DistrictAuthoring` objects in the scene with specific coordinates and settings, you now place a single `WorldBootstrapAuthoring` component that generates the entire world hierarchy procedurally:

```
Biomes → Districts → Sectors → Rooms
```

## Quick Start

### 1. Add WorldBootstrapAuthoring to Scene

1. Create an empty GameObject in your scene
2. Add the `WorldBootstrapAuthoring` component
3. Configure the generation ranges in the inspector
4. Press Play - the world generates automatically!

### 2. Basic Configuration

```csharp
// Example configuration values
Seed: 42 (or 0 for random)
World Size: 64x64
District Count: 4-12 districts
Sectors Per District: 2-8 sectors  
Rooms Per Sector: 3-12 rooms
```

### 3. Preview Without Play Mode

Use the **"🔄 Preview Generation"** button in the inspector to see generation results without entering play mode. This calculates expected counts based on your configuration ranges.

## Configuration Options

### World Configuration
- **Seed**: Deterministic seed (0 = random). Preview button generates and displays the seed used.
- **World Size**: World bounds in Unity units (X,Z plane)
- **Randomization Mode**: None/Partial/Full - controls rule randomization

### Generation Ranges
All ranges use min/max values for procedural variation:

- **Biome Count**: 3-6 biomes (different biome types spread across world)
- **District Count**: 4-12 districts (main gameplay areas)
- **Sectors Per District**: 2-8 sectors (subdivisions within districts)
- **Rooms Per Sector**: 3-12 rooms (individual gameplay spaces)

### Advanced Options
- **District Min Distance**: Minimum spacing between districts (Poisson disc sampling)
- **Sector Grid Size**: Local grid size for sector subdivision (6x6 default)
- **Target Loop Density**: Connection density for room layouts (0.3 = 30% loops)

## System Architecture

### ECS System Flow
1. **WorldBootstrapSystem** - Generates entities from bootstrap configuration
2. **DistrictLayoutSystem** - Places districts procedurally (existing system)
3. **SectorRoomHierarchySystem** - Subdivides districts into sectors/rooms (existing system)
4. **RuleRandomizationSystem** - Applies adaptive rules (existing system)
5. **WFC Systems** - Generate final world content (existing systems)

### Component Data
- `WorldBootstrapConfiguration` - All generation settings
- `WorldBootstrapInProgressTag` - Generation state tracking
- `WorldBootstrapCompleteTag` - Completion with statistics
- `WorldConfiguration` - Compatibility with existing systems

## Integration with Existing Systems

The bootstrap system **extends** rather than replaces the existing architecture:

- ✅ **Backward Compatible**: Existing manual authoring still works
- ✅ **Reuses Existing Systems**: Leverages DistrictLayoutSystem, SectorRoomHierarchySystem
- ✅ **Same Data Flow**: Generates the same entity structures as manual authoring
- ✅ **Debug Compatibility**: Works with existing gizmos and debug visualization

## Editor Features

### Custom Inspector
- Range validation (ensures min ≤ max)
- Organized sections with foldouts
- Preview calculations without play mode
- Random seed generation button
- Help text and tooltips

### Scene Visualization
- World bounds gizmo (when debug visualization enabled)
- Corner labels (NE, NW, SE, SW)
- Semi-transparent bounds overlay

### Scene Bootstrap Integration
The existing `MetVanDAMNSceneBootstrap` system automatically detects and uses `WorldBootstrapAuthoring` when creating baseline scenes, falling back to `SmokeTestSceneSetup` if unavailable.

## Example Usage

### Simple Bootstrap Setup
```csharp
// Minimum configuration for quick testing
GameObject bootstrap = new GameObject("WorldBootstrap");
var bootstrapAuth = bootstrap.AddComponent<WorldBootstrapAuthoring>();
bootstrapAuth.seed = 42;
bootstrapAuth.worldSize = new int2(64, 64);
bootstrapAuth.districtCount = new Vector2Int(3, 8);
bootstrapAuth.sectorsPerDistrict = new Vector2Int(2, 5);
```

### Advanced Configuration
```csharp
// Full configuration with all options
var bootstrap = FindObjectOfType<WorldBootstrapAuthoring>();
bootstrap.seed = 0; // Random seed
bootstrap.worldSize = new int2(128, 128);
bootstrap.randomizationMode = RandomizationMode.Full;

// Biome settings
bootstrap.biomeCount = new Vector2Int(4, 8);
bootstrap.biomeWeight = 1.2f;

// District settings  
bootstrap.districtCount = new Vector2Int(6, 15);
bootstrap.districtMinDistance = 20f;
bootstrap.districtWeight = 0.9f;

// Sector/Room settings
bootstrap.sectorsPerDistrict = new Vector2Int(3, 10);
bootstrap.sectorGridSize = new int2(8, 8);
bootstrap.roomsPerSector = new Vector2Int(4, 15);
bootstrap.targetLoopDensity = 0.5f;

// Debug options
bootstrap.enableDebugVisualization = true;
bootstrap.logGenerationSteps = true;
```

## Benefits

### For Developers
- **Faster iteration**: No manual district placement
- **Consistent results**: Deterministic seed-based generation
- **Scalable worlds**: Easy to generate large worlds
- **Error reduction**: No manual coordinate management

### For Designers  
- **Rapid prototyping**: Preview without play mode
- **Flexible configuration**: Range-based settings for variation
- **Visual feedback**: Scene gizmos and generation logs
- **Easy experimentation**: One-click random seed generation

### For Players
- **Replayability**: Different worlds with same seed
- **Balanced progression**: Configurable difficulty and density
- **Coherent worlds**: Proper biome distribution and connections

## Migration from Manual Authoring

To migrate existing scenes:

1. **Remove manual DistrictAuthoring objects** from the scene
2. **Add WorldBootstrapAuthoring** to an empty GameObject
3. **Configure ranges** to match your desired district count
4. **Test with preview** to verify expected generation
5. **Run in play mode** to validate full generation pipeline

The system generates the same entity structures, so existing downstream systems continue to work unchanged.

## Troubleshooting

### Common Issues

**No districts generated**: Check that `districtCount` range is valid (min ≤ max, both > 0)

**Preview shows unexpected counts**: Preview uses statistical averages - actual generation includes randomization

**Systems not running**: Ensure `WorldBootstrapAuthoring` is on an active GameObject in the scene

**Compilation errors**: Verify all packages are properly imported and assembly references are set

### Debug Options

- Enable `logGenerationSteps` to see detailed generation progress
- Enable `enableDebugVisualization` for scene gizmos
- Use the preview function to validate configuration before runtime
- Check the Unity Console for generation statistics and warnings

## Performance Considerations

- **Generation Time**: Runs once at scene start - no runtime performance impact
- **Memory Usage**: Similar to manual authoring - same entity structures
- **Large Worlds**: Generation time scales with district count (O(n²) for placement)
- **Preview Calculations**: Instant - no entity creation, just math

## Future Extensions

The bootstrap system is designed for extensibility:

- **Biome Templates**: Predefined biome configurations
- **Difficulty Curves**: Adaptive room complexity based on distance from start
- **Connectivity Patterns**: Different graph topologies (linear, hub-spoke, mesh)
- **Theme Support**: Visual theming integration with BiomeArtProfile system
- **Export/Import**: Save/load bootstrap configurations as assets