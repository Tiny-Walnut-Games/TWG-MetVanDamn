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