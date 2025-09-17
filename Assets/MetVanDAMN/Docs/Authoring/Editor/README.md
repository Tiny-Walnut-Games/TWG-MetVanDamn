# MetVanDAMN Authoring Editor Tools

This directory contains Unity Editor-specific tools for the MetVanDAMN authoring workflow.

## Bakers

Located in `Bakers/` subdirectory:

- **`DistrictBaker`** - Converts DistrictAuthoring to NodeId, WfcState, SectorRefinementData
- **`ConnectionBaker`** - Converts ConnectionAuthoring to ConnectionEdge components  
- **`BiomeFieldBaker`** - Converts BiomeFieldAuthoring to BiomeFieldData
- **`WorldConfigurationBaker`** - Converts WorldConfigurationAuthoring to world settings
- **`GateConditionBaker`** - Converts GateConditionAuthoring to GateConditionBufferElement
- **`WfcTilePrototypeBaker`** - Converts WfcTilePrototypeAuthoring to WfcTilePrototype + sockets
- **`WorldBootstrapBaker`** - Converts WorldBootstrapAuthoring to WorldBootstrapConfiguration

## Editor Tools

- **`MetVanDAMNSamplePrefabCreator`** - Creates sample prefabs for quick scene setup
- **`MetVanDAMNAuthoringSampleCreator`** - Generates complete authoring sample scene
- **`MetVanDAMNSceneBootstrap`** - Creates baseline playable scenes with subscenes
- **`WorldBootstrapDemo`** - Provides WorldBootstrap authoring preview and controls
- **`MetVDWorldDebugWindow`** - In-editor visualization and debugging tools

## Gizmos & Visualization

Located in `Gizmos/` subdirectory:

- **`MetVDGizmoDrawer`** - Scene view gizmo rendering for authoring components
- **`MetVDGizmoSettings`** - Configurable settings for gizmo appearance
- **`ProceduralLayoutGizmoDrawer`** - Advanced layout visualization tools

## Inspectors

- **`WorldBootstrapAuthoringInspector`** - Custom inspector for WorldBootstrapAuthoring
- **`BiomeArtProfileInspector`** - Custom inspector for BiomeArtProfile with preview
- **`BiomeArtProfileEditor`** - Advanced editor for BiomeArtProfile configurations

## Sync Systems

Located in `Sync/` subdirectory:

- **`DistrictPlacementSyncSystem`** - Syncs ECS changes back to authoring for gizmos

## Utility Tools

- **`BatchSpriteSlicer`** - Batch processing tool for sprite assets
- **`ProceduralLayoutPreview`** - Preview system for procedural layouts
- **`BiomeArtProfileSamples`** - Sample generation for BiomeArtProfile

## Usage

These tools are automatically available in the Unity Editor when the authoring assembly is included. Access them through:

- **Menu**: `Tools/MetVanDAMN/...`
- **Windows**: `Window/MetVanDAMN/...` 
- **Component Inspectors**: Automatic custom inspectors
- **Scene View**: Automatic gizmo rendering

## Assembly Dependencies

- `TinyWalnutGames.MetVD.Authoring.Editor.asmdef`
- References Unity Editor assemblies
- References runtime authoring assembly
- References core MetVanDAMN assemblies