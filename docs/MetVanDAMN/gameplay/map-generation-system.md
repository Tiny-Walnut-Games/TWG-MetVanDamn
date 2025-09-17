# 🗺️ MetVanDAMN Map Generation System

## Overview

The MetVanDAMN Map Generation System provides comprehensive world visualization and mapping functionality for procedurally generated MetroidVania worlds. It creates both in-game minimaps and detailed world overview maps that update dynamically as players explore.

## Features

### 🧭 **In-Game Minimap**
- **Real-time player tracking** with position updates
- **Exploration-based reveal** - only shows discovered areas
- **District and room visualization** with color-coded types
- **Compact HUD integration** in top-right corner
- **Toggle with M key** for easy access

### 🖼️ **Detailed World Map**
- **Complete world overview** showing all districts, rooms, and biomes
- **High-resolution rendering** (configurable up to 4K)
- **Layered visualization**: biomes → districts → rooms → connections
- **Interactive display** with zoom and pan capabilities
- **Export functionality** for sharing and documentation

### 🎨 **Visual Elements**
- **Districts**: Color-coded by type (hub=yellow, normal=cyan, explored=green, locked=red)
- **Rooms**: Shape-coded by function (chamber=square, corridor=capsule, etc.)
- **Biome Fields**: Semi-transparent overlays showing polarity influences
- **Connections**: White lines showing district adjacency
- **Player Position**: Real-time indicator on both maps

## Integration

### Automatic Setup
The map generation system is automatically included when using the Complete Demo Scene Generator:

```
Tools > MetVanDAMN > Create Base DEMO Scene > Any Demo Type
```

### Manual Setup
Add to any existing scene:

1. **Add Component**: Attach `MetVanDAMNMapGenerator` to any GameObject
2. **Configure Settings**: Adjust resolution, colors, and features in Inspector
3. **Generate Maps**: Hit Play and maps generate automatically

## Controls

### In-Game Controls
- **M Key**: Toggle detailed world map display
- **Automatic**: Minimap updates as player moves and explores

### Editor Controls
```
Tools > MetVanDAMN > Map Generation >
  🗺️ Generate World Map     - Create/update all maps
  🧭 Toggle Minimap         - Show/hide minimap
  🖼️ Toggle Detailed Map    - Show/hide detailed map
  📁 Export Map as Image    - Save map to persistent data
  🔄 Regenerate All Maps    - Refresh all visualizations
```

## Configuration

### Inspector Settings

**Map Generation Settings**
- `autoGenerateOnWorldSetup`: Generate maps when world is created
- `showMinimapInGame`: Enable in-game minimap display
- `generateDetailedWorldMap`: Create high-res overview map
- `exportMapAsImage`: Save maps as PNG files

**Visual Settings**
- `mapResolution`: Detail map size (256-2048 recommended)
- `minimapSize`: Minimap pixel size (128-512 recommended)
- `backgroundColor`: Base color for unexplored areas

**Color Customization**
- **Districts**: Hub, normal, explored, locked colors
- **Rooms**: Chamber, corridor, hub, specialty colors  
- **Biomes**: Sun, moon, heat, cold field colors
- **UI**: Current location, unexplored area colors

## Usage Examples

### Basic Map Generation
```csharp
// Get the map generator
var mapGen = FindObjectOfType<MetVanDAMNMapGenerator>();

// Generate all maps
mapGen.GenerateWorldMap();

// Toggle displays
mapGen.ToggleDetailedMap();
mapGen.ToggleMinimap();
```

### Custom Configuration
```csharp
// Configure for high-quality export
mapGen.mapResolution = 2048;
mapGen.exportMapAsImage = true;
mapGen.GenerateWorldMap();

// Configure for performance
mapGen.mapResolution = 256;
mapGen.minimapSize = 128;
mapGen.RegenerateMap();
```

### Access Map Data
```csharp
// Get generated textures
Texture2D worldMap = mapGen.GetWorldMapTexture();
Texture2D minimap = mapGen.GetMinimapTexture();

// Get world information
var worldData = mapGen.GetWorldMapData();
Debug.Log($"World seed: {worldData.seed}");
```

## Troubleshooting

### Common Issues

**Maps Not Generating**
- Ensure world generation completed before calling GenerateWorldMap()
- Check that ECS entities exist (districts, rooms, biome fields)
- Verify Entity Manager is available and world is created

**Visual Quality Issues**
- Increase mapResolution for sharper details
- Adjust colors for better contrast
- Check backgroundColor vs content colors

**Performance Problems**
- Reduce mapResolution (512 or lower)
- Disable exportMapAsImage
- Decrease update frequency for minimap

**UI Not Appearing**
- Ensure Canvas exists in scene
- Check that UI elements aren't behind other objects
- Verify anchor settings for different screen sizes

### Debug Information
The system provides comprehensive logging:
- `🗺️ Starting MetVanDAMN world map generation...`
- `📊 Extracting world data from ECS entities...`
- `🎨 Generating detailed world map texture...`
- `✅ MetVanDAMN world map generation complete!`

---

The MetVanDAMN Map Generation System provides a complete solution for world visualization, ensuring players never lose their way in the vast procedural landscapes while maintaining the sense of discovery and exploration that makes MetroidVania games compelling.