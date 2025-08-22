# MetVanDAMN Samples Package

This package provides sample scenes and demo content for the MetVanDAMN procedural Metroidvania engine.

## Smoke Test Scene

The primary sample is a **smoke-test scene** that demonstrates the engine working out-of-the-box:

### Quick Start

1. Open Unity with the MetVanDAMN project
2. In the Project window, navigate to `Packages/com.metvd.samples/Runtime/`
3. Add the `SmokeTestSceneSetup` component to any GameObject in your scene
4. Press Play
5. Watch console logs for world generation progress
6. See instant indie-quality map generation!

### What It Demonstrates

- **Deterministic Generation**: Same seed produces identical worlds
- **WFC District Creation**: Wave Function Collapse generates coherent layouts
- **Biome Field Assignment**: Environmental polarities assign automatically
- **Sector Refinement**: Loops and hard locks placed per specification
- **Complete Validation**: All systems working together without errors

### Configuration

The `SmokeTestSceneSetup` component exposes several parameters:

```csharp
[Header("World Generation Parameters")]
public uint worldSeed = 42;                    // Deterministic seed
public int2 worldSize = new int2(50, 50);      // World bounds
public int targetSectorCount = 5;              // Number of sectors
public float biomeTransitionRadius = 10.0f;    // Polarity field radius

[Header("Debug Visualization")]
public bool enableDebugVisualization = true;   // Visual debug aids
public bool logGenerationSteps = true;         // Console logging
```

### Generated Entities

The smoke test creates:

- **World Configuration**: Central configuration with seed and bounds
- **Hub District**: Central area at coordinates (0,0)
- **Surrounding Districts**: 5x5 grid of districts with different levels
- **Biome Fields**: Environmental zones with polarity assignments
- **Polarity Fields**: Sun, Moon, Heat, and Cold zones for environmental variety

### Console Output

When working correctly, you should see:

```
🚀 MetVanDAMN Smoke Test: Starting world generation...
✅ MetVanDAMN Smoke Test: World setup complete with seed 42
   World size: 50x50
   Target sectors: 5
   Systems will begin generation on next frame.
```

## Extension Points

### Custom Scenes

Create your own scenes by:

1. Adding the `SmokeTestSceneSetup` component
2. Modifying parameters for your desired world
3. Adding visual components (tilemaps, sprites) for rendering
4. Implementing custom biome-specific artwork

### Integration with Your Assets

The smoke test provides entity structure - add your own:

- **Tilemap Renderers** for visual representation
- **Sprite Renderers** for game objects
- **Physics Components** for gameplay interaction
- **Animation Systems** for dynamic elements

## Technical Details

### Entity Creation

The smoke test follows ECS best practices:

- Creates entities with appropriate components
- Uses component data for configuration
- Leverages buffer elements for dynamic collections
- Implements deterministic seeding for reproducibility

### System Integration

Works with all MetVanDAMN systems:

- **DistrictWfcSystem**: Generates district layouts
- **SectorRefineSystem**: Adds loops and hard locks
- **BiomeFieldSystem**: Assigns environmental properties
- **Validation Systems**: Ensures integrity throughout

### Performance

The smoke test is designed for immediate feedback:

- Minimal entity count for fast startup
- Efficient component usage
- Burst-compiled system compatibility
- Memory-efficient buffer allocation

## Troubleshooting

### No Console Output

1. Check that `logGenerationSteps` is enabled
2. Ensure the component is attached to an active GameObject
3. Verify all MetVanDAMN packages are properly installed

### World Not Generating

1. Run `./scripts/validate-metvan.sh` to check system health
2. Verify Unity Entities package is installed
3. Check for compilation errors in the Console window

### Performance Issues

1. Reduce `worldSize` for faster generation
2. Lower `targetSectorCount` for simpler worlds
3. Disable `enableDebugVisualization` for production builds

## Next Steps

- Add visual assets following the [Tilemap Integration Guide](../../docs/gitbook/tilemap-integration/README.md)
- Extend with custom biomes using the [Engine Systems Documentation](../../docs/gitbook/engine-systems/README.md)
- Implement gameplay mechanics on top of the generated world structure

## Support

For issues or questions:

1. Check the [Validation Guide](../../docs/gitbook/validation/README.md) for common problems
2. Review the complete [Documentation](../../docs/gitbook/README.md)
3. File issues on the GitHub repository