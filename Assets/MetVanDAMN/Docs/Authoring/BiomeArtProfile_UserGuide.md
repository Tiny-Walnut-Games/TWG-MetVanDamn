# Biome Art Profile System - User Guide

## Overview

The Biome Art Profile system enables biome-specific tilemap generation and prop placement in MetVanDAMN's ECS worldgen. It integrates with the Grid Layer Editor to support platformer, top-down, isometric, and hexagonal projections.

## Core Components

### BiomeArtProfile ScriptableObject

The `BiomeArtProfile` is the main asset that defines the visual characteristics of a biome:

```csharp
[CreateAssetMenu(menuName = "MetVanDAMN/Biome Art Profile")]
public class BiomeArtProfile : ScriptableObject
{
    [Header("Biome Identity")]
    public string biomeName;                    // Descriptive name for the biome
    public Color debugColor;                    // Debug visualization color
    
    [Header("Tilemap Art")]
    public TileBase floorTile;                  // Floor/ground tiles
    public TileBase wallTile;                   // Wall/barrier tiles  
    public TileBase backgroundTile;             // Background decoration tiles
    public TileBase[] transitionTiles;          // Transition between biomes
    
    [Header("Props")]
    public GameObject[] propPrefabs;            // Prop prefabs to spawn
    public float propSpawnChance;               // Chance to spawn props (0-1)
    public List<string> allowedPropLayers;      // Tilemap layers for prop placement
    
    [Header("Advanced")]
    public string sortingLayerOverride;         // Override sorting layer
    public Material materialOverride;           // Override material
}
```

## Quick Start

### 1. Create a BiomeArtProfile

Right-click in the Project window and select:
`Create > MetVanDAMN > Biome Art Profile`

Or use the sample profiles:
`Assets > Create > MetVanDAMN > Sample Biome Profiles > Solar Plains Profile`

### 2. Configure the Profile

- **Biome Name**: Descriptive name (e.g., "Solar Plains", "Crystal Caverns")
- **Debug Color**: Unique color for debug visualization
- **Tiles**: Assign TileBase assets (preferably RuleTiles from Unity 2D Tilemap Extras)
- **Props**: Assign GameObject prefabs for environmental props
- **Prop Spawn Chance**: Probability of prop placement (0.1 = 10% chance)
- **Allowed Prop Layers**: Specify tilemap layers where props can be placed

### 3. Set Up Biome Entities

Add the `BiomeArtProfileAuthoring` component to GameObjects representing biomes:

```csharp
public class BiomeArtProfileAuthoring : MonoBehaviour
{
    public BiomeArtProfile artProfile;          // Reference to your profile
    public ProjectionType projectionType;      // Platformer/TopDown/Isometric/Hexagonal
    public bool autoConfigureBiomeType;        // Auto-detect biome type from name
}
```

### 4. Projection Types

The system supports four projection types:

- **Platformer**: Side-scrolling with parallax layers
- **TopDown**: Classic top-down view with depth layers
- **Isometric**: Isometric projection with Y-sorted layers
- **Hexagonal**: Hexagonal grid with specialized layers

## Layer Configuration

Each projection type uses specific layer naming conventions:

### Platformer Layers
- Parallax5, Parallax4, Parallax3, Parallax2, Parallax1
- Background2, Background1, BackgroundProps
- WalkableGround, WalkableProps
- Hazards, Foreground, ForegroundProps
- RoomMasking, Blending

### Top-Down Layers
- DeepOcean, Ocean, ShallowWater
- Floor, FloorProps
- WalkableGround, WalkableProps
- OverheadProps, RoomMasking, Blending

### Tile Assignment Rules

The system automatically assigns tiles based on layer names:

- **Floor/Ground layers**: Use `floorTile`
- **Wall/Hazard layers**: Use `wallTile`  
- **Background/Parallax layers**: Use `backgroundTile`
- **Blending layers**: Use `transitionTiles` for biome boundaries

## Prop Placement

Props are automatically placed according to these rules:

1. **Layer Restriction**: Only placed in layers listed in `allowedPropLayers`
2. **Spawn Chance**: Probability determined by `propSpawnChance`
3. **Random Selection**: Randomly selects from `propPrefabs` array
4. **Position**: Based on node coordinates from ECS worldgen

### Recommended Prop Layers

```csharp
profile.allowedPropLayers.Add("FloorProps");      // Ground-level decoration
profile.allowedPropLayers.Add("WalkableProps");   // Interactive props
profile.allowedPropLayers.Add("OverheadProps");   // Hanging/overhead decoration
```

## Biome Transitions

The system automatically detects biome boundaries and applies transition tiles:

1. **Detection**: Monitors neighboring nodes for biome type changes
2. **Strength Calculation**: Based on distance to biome boundary
3. **Tile Selection**: Chooses transition tile based on transition strength
4. **Placement**: Applied to "Blending" layer or first available tilemap layer

## Integration with ECS Systems

The biome art system integrates with existing ECS components:

### Core ECS Components
- `Biome`: Core biome data (type, polarity, strength)
- `NodeId`: Spatial positioning in the world graph
- `BiomeArtProfileReference`: Links biome entities to art profiles

### System Execution Order
1. `BiomeFieldSystem`: Assigns biome types and polarity
2. `BiomeArtIntegrationSystem`: Applies art profiles to biomes
3. `BiomeTransitionSystem`: Handles biome boundary transitions

## Advanced Usage

### Custom Grid Creation

For programmatic grid creation, call the Grid Layer Editor methods:

```csharp
// Based on projection type
switch (projectionType)
{
    case ProjectionType.Platformer:
        TwoDimensionalGridSetup.CreateSideScrollingGrid();
        break;
    case ProjectionType.TopDown:
        TwoDimensionalGridSetup.CreateDefaultTopDownGrid();
        break;
    case ProjectionType.Isometric:
        TwoDimensionalGridSetup.CreateIsometricTopDownGrid();
        break;
    case ProjectionType.Hexagonal:
        TwoDimensionalGridSetup.CreateHexTopDownGrid();
        break;
}
```

### Material and Sorting Overrides

Use advanced settings for specialized visual requirements:

```csharp
profile.sortingLayerOverride = "BiomeLayer";        // Custom sorting layer
profile.materialOverride = customBiomeMaterial;     // Custom material shader
```

## Best Practices

### Performance
- Limit prop spawn chance to reasonable values (0.05-0.2)
- Use object pooling for frequently spawned props
- Optimize RuleTile configurations for performance

### Organization
- Create separate profiles for each major biome type
- Use consistent naming conventions for easy identification
- Group related profiles in dedicated project folders

### Transitions
- Always provide transition tiles for smooth biome boundaries
- Test transition quality with different biome combinations
- Consider creating specialized transition-only profiles

## Troubleshooting

### Common Issues

**Props not spawning:**
- Check `allowedPropLayers` matches actual tilemap layer names
- Verify `propSpawnChance` is greater than 0
- Ensure `propPrefabs` array contains valid prefabs

**Tiles not appearing:**
- Verify TileBase assets are properly assigned
- Check tilemap layer names match expected conventions
- Ensure Grid and Tilemap components exist in scene

**Transitions not working:**
- Verify `transitionTiles` array is populated
- Check biome boundary detection logic
- Ensure "Blending" layer exists or fallback layer is available

### Debug Visualization

Use the `debugColor` field to visualize biome assignments:
- Each biome will use its specified debug color
- Helps identify biome boundary issues
- Useful for validating biome type assignments

## Example Workflow

1. **Create Profile**: `Create > MetVanDAMN > Biome Art Profile`
2. **Configure Art**: Assign tiles and props to the profile
3. **Set Up Layers**: Configure `allowedPropLayers` for prop placement
4. **Add to Scene**: Place `BiomeArtProfileAuthoring` on biome GameObjects
5. **Select Projection**: Choose appropriate projection type
6. **Test**: Play scene to see biome art generation in action
7. **Iterate**: Adjust spawn chances, tiles, and props as needed

This system provides a flexible foundation for creating visually distinct biomes that automatically integrate with MetVanDAMN's procedural world generation.