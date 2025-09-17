# MetVanDAMN Onboarding Guide

Welcome to MetVanDAMN! This guide will get you up and running with the authoring-driven procedural world generation system.

## Quick Start (Recommended Path)

### Option 1: Use the Authoring Sample Scene

**This is the preferred approach for new users** - it demonstrates the complete authoring workflow without custom bootstrappers.

1. **Create the sample scene**:
   ```
   Tools/MetVanDAMN/Create Authoring Sample Scene
   ```
   This creates `Assets/Scenes/MetVanDAMN_AuthoringSample.unity`

2. **Open the scene** and examine the structure:
   - **WorldConfiguration** - Global settings (seed, bounds)
   - **Districts** - 3x3 grid of authoring districts  
   - **Connections** - Authoring connections between adjacent districts
   - **BiomeFields** - Sample biome influence fields
   - **WfcTilePrototypeLibrary** - Tile definitions for WFC generation

3. **Press Play** - The scene will automatically:
   - Bake authoring components to ECS
   - Initialize WFC and biome systems
   - Begin procedural generation
   - No custom bootstrap code required!

4. **Customize the scene**:
   - Modify district properties in the Inspector
   - Add/remove connections by changing from/to references
   - Adjust biome field strength and gradients
   - Create new WFC tile prototypes

### Option 2: Create Your Own Scene

1. **Create prefabs** (optional):
   ```
   Tools/MetVanDAMN/Create Sample Prefabs
   ```
   This creates prefabs in `Assets/MetVanDAMN/Prefabs/Samples/`

2. **Set up a new scene**:
   - Add a `WorldConfigurationAuthoring` component to an empty GameObject
   - Add `DistrictAuthoring` components to GameObjects positioned in your scene
   - Connect districts using `ConnectionAuthoring` components
   - Add `BiomeFieldAuthoring` for environmental variation
   - Optionally add `GateConditionAuthoring` for locked areas

3. **Configure components**:
   - Use Inspector tooltips for guidance
   - Assign unique NodeIDs to districts
   - Reference district GameObjects in connection components
   - Set biome types and polarities appropriately

4. **Press Play** and watch the generation unfold!

## Legacy: SmokeTest Bootstrap (For CI/Testing)

The `SmokeTestSceneSetup` component provides immediate "press Play" functionality but is now primarily used for:
- Continuous integration testing
- Performance benchmarking  
- Quick smoke tests during development

**For learning and scene authoring, prefer the Authoring Sample approach above.**

## Understanding the Authoring Workflow

### Core Concept
MetVanDAMN uses **authoring components** that automatically convert to **ECS components** at build/play time. This means:
- Design in the Inspector with familiar Unity workflows
- Get high-performance ECS execution at runtime
- No manual ECS entity creation needed

### Key Authoring Components

1. **DistrictAuthoring** - Defines macro-level world regions
   - NodeID for identification
   - Grid coordinates for spatial layout
   - WFC generation settings
   - Target loop density for refinement

2. **ConnectionAuthoring** - Links districts together
   - From/To district references (drag GameObjects)
   - Bidirectional vs one-way connections
   - Polarity requirements (Sun/Moon/Heat/Cold)
   - Traversal cost for pathfinding

3. **BiomeFieldAuthoring** - Creates environmental zones
   - Primary and secondary biome types
   - Strength and gradient settings
   - Spatial positioning via Transform

4. **WfcTilePrototypeAuthoring** - Defines tileset rules
   - Tile ID and generation weight
   - Biome and polarity constraints
   - Socket configurations for connections
   - Min/max connection requirements

### Editor Visualization

Enable gizmos for real-time scene visualization:
- **Districts**: Colored quads with ID labels
- **Connections**: Arrow lines showing direction and type
- **Biome Fields**: Translucent spheres showing influence radius
- **Sockets**: Direction indicators on tile prototypes

Access via: `Window/MetVanDAMN/World Debugger`

## Advanced Features

### Procedural Bootstrapping
For fully procedural worlds, use `WorldBootstrapAuthoring`:
- Single component generates entire world hierarchy
- Configurable district/sector/room counts
- Deterministic with seed support
- Editor preview without entering Play mode

### BiomeArt Integration
Integrate with the biome art system:
- `BiomeArtProfileAuthoring` for tilemap placement
- Grid Layer Editor support for multi-projection layouts
- Runtime prop placement with clustering strategies

### Testing and Validation
The authoring layer includes comprehensive tests:
- **Bake tests**: Verify authoring→ECS conversion
- **Determinism tests**: Ensure reproducible generation
- **Integration tests**: Validate complete pipeline
- **Smoke tests**: Quick functionality verification

Run tests via Unity Test Runner or CI pipeline.

## Best Practices

### Scene Organization
- Group related components under parent GameObjects
- Use descriptive names: `District_Hub`, `Connection_Hub_To_North`
- Position districts to match desired spatial layout
- Use prefabs for repeated structures

### Component Configuration
- Set unique NodeIDs for all districts (avoid zero)
- Reference actual district GameObjects in connections
- Use polarity requirements to create environmental challenges
- Balance WFC tile weights for interesting generation

### Performance Considerations
- Limit district count for real-time generation (< 50 for smooth experience)
- Use appropriate biome field radii (too large = performance impact)
- Configure WFC iteration limits to prevent infinite loops
- Monitor frame rate during generation and adjust accordingly

### Debugging
- Enable debug visualization for visual feedback
- Check Unity Console for generation step logging
- Use World Debugger window for runtime inspection
- Test with different seeds to verify variety

## Common Issues

### Generation Stalls
- Check for contradictory WFC constraints
- Verify socket compatibility between tiles
- Ensure adequate candidate tile variety
- Review polarity requirements for feasibility

### Missing Connections
- Verify from/to references are set correctly
- Check that referenced districts have valid NodeIDs
- Ensure ConnectionBaker is processing correctly
- Review bidirectional vs one-way settings

### Visual Issues
- Enable gizmo rendering in Scene view
- Check material assignments on visual representations
- Verify Transform positions match intended layout
- Update MetVDGizmoSettings for custom styling

## Next Steps

1. **Explore the sample scene** - Understand component relationships
2. **Create a simple test scene** - Practice with basic authoring
3. **Experiment with different settings** - Learn how parameters affect generation
4. **Review the advanced documentation** - Dive deeper into specific systems
5. **Join the community** - Share your creations and get help

For detailed technical documentation, see the individual README files in each module directory.

Happy world building! 🌍✨