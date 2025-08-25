# Unity Editor Tools

This directory contains general-purpose Unity Editor tools that can be used across the entire project.

## Tools

### BatchSpriteSlicer

**Location**: `Assets/Tools/Editor/BatchSpriteSlicer.cs`  
**Menu**: `Tools > Batch Sprite Slicer`  
**Dependencies**: Unity 2D Sprite package (`com.unity.2d.sprite`)

A comprehensive tool for batch processing sprite textures in Unity. Features include:

- **Grid-based slicing**: Slice textures into uniform grids using either fixed cell sizes or column/row counts
- **Copy/Paste layouts**: Copy slice layouts from one texture and apply to others with automatic scaling
- **Pivot adjustment**: Batch adjust pivot points for all slices in selected textures  
- **Smart empty detection**: Option to ignore fully transparent cells during slicing
- **Physics shape support**: Preserves and scales custom physics outlines when copying layouts

#### Usage

1. Open the tool via `Tools > Batch Sprite Slicer`
2. Select one or more textures in the Project window
3. Configure slicing settings (grid size, pivot alignment, etc.)
4. Use the appropriate button based on your needs:
   - **Slice Selected Sprites**: Create new grid-based slices
   - **Copy Rect Layout**: Copy existing slice layout from first selected texture
   - **Paste Rect Layout**: Apply copied layout to all selected textures
   - **Adjust Pivot**: Update pivot alignment for existing slices

#### Requirements

- Unity 6.2.1f1 or later
- 2D Sprite package (com.unity.2d.sprite)
- Textures must be imported as Sprites for full functionality

#### Assembly

This tool is part of the `TinyWalnutGames.Tools.Editor` assembly and is only available in the Unity Editor.

---

### BiomeRegionExtractor

**Location**: `Assets/Tools/Editor/BiomeRegionExtractor.cs`  
**Menu**: `Tools > Biome Region Extractor`  
**Dependencies**: Unity 2D Sprite package (`com.unity.2d.sprite`)

An advanced tool for automatically slicing and organizing biome-specific sprites from large spritesheets. Designed to eliminate the manual, error-prone process of hand-cutting and sorting assets when multiple biome tilesets are combined in a single atlas.

#### Core Features

- **Biome Mask Support**: Accept color-coded mask images, JSON rect lists, or tilemap exports mapping each sprite cell to a biome ID
- **Batch Slicing**: Uses existing BatchSpriteSlicer logic for pivot, outline, and import settings, applied only to cells belonging to the current biome group
- **Export & Organization**: Creates sub-folders per biome, names sprites with biome prefix for easy search, optionally packs each biome into its own atlas
- **Validation & Pre-Flight Checks**: Warns if selected sheets have mismatched cell sizes or layouts, flags "mixed" cells that straddle biome boundaries for manual review
- **Metadata Integration**: Stores biome ID in sprite asset metadata for ECS bootstrap to link directly to biome systems

#### Workflow

1. Select spritesheet(s) in Project view
2. Assign biome mask or mapping file (color-coded Texture2D or JSON TextAsset)
3. Configure slicing settings (cell size, pivot, outline)
4. Detect biomes from mask or run auto-detection for contiguous regions
5. Review and edit detected biome mappings
6. Run pre-flight validation to check for issues
7. Click **Process** → tool slices, groups, and exports sprites into biome folders

#### UI Components

- **Spritesheet Selection**: Multi-select textures containing mixed biome tiles
- **Biome Mapping**: Assign mask files and configure detected biome properties
- **Slicing Settings**: Cell size, pivot alignment, physics shape generation
- **Validation Panel**: Pre-flight checks with clickable warnings
- **Preview Area**: Resizable pane with biome color overlay and zoom controls
- **Export Settings**: Output folder, naming patterns, atlas generation options

#### Biome Mask Formats

**Color-Coded Masks**: Texture2D where each pixel color represents a biome type
- Red (1,0,0): Volcanic biomes
- Green (0,1,0): Forest biomes  
- Blue (0,0,1): Ocean biomes
- Yellow (1,1,0): Desert biomes
- Custom colors detected automatically

**JSON Mapping** (planned): TextAsset with biome definitions and rect coordinates

#### Integration with MetVanDAMN

- **ECS Metadata**: Embeds biome IDs in sprite userData for runtime ECS integration
- **BiomeArtProfile Integration**: Generated sprites work seamlessly with existing biome art system
- **Naming Conventions**: Follows established patterns for easy integration with biome systems

#### Requirements

- Unity 6.2.1f1 or later
- 2D Sprite package (com.unity.2d.sprite)
- Textures must be imported as Sprites for full functionality
- Biome mask textures should match spritesheet dimensions

#### Assembly

This tool is part of the `TinyWalnutGames.Tools.Editor` assembly and is only available in the Unity Editor.