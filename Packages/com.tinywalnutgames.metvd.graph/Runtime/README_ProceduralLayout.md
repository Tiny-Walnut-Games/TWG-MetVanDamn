# Procedural World Layout System

The Procedural World Layout system is a comprehensive enhancement to MetVanDAMN's existing WFC-based world generation. It provides deterministic district placement, adaptive rule randomization, and hierarchical sector/room subdivision.

## Overview

By default the system uses the legacy Grid-First flow. An optional Shape-First flow can be enabled to generate an organic silhouette first, then place districts to fit the shape.

The overall generation involves the following stages:

1. District pre-layout (or fit-to-shape in Shape-First)
1. Connection building
1. Rule randomization
1. Sector/room hierarchy
1. WFC integration
1. Debug visualization

## System Architecture

### Core Components

- `WorldConfiguration` - Extended with `RandomizationMode` and `GenerationFlow` enums
- `DistrictLayoutDoneTag` - Gates downstream systems
- `WorldRuleSet` - Stores randomized biome/upgrade rules
- `SectorHierarchyData` - Local grid subdivision data
- `RoomHierarchyData` - BSP room generation data

### ECS Systems (Execution Order)

1. `BuildConnectionBuffersSystem` (existing, authoring layer)
1. `WorldAspectRandomizerSystem` (shape-first only)
1. `WorldShapeWfcSystem` (shape-first only)
1. `DistrictFitToShapeSystem` (shape-first only)
1. `DistrictLayoutSystem` (legacy only; gated by `WorldConfiguration.Flow`)
1. `ConnectionBuilderSystem` (graph connectivity)
1. `RuleRandomizationSystem` (adaptive rules)
1. `SectorRoomHierarchySystem` (subdivision)
1. `DistrictWfcSystem` (gated by layout completion)

## Randomization Modes

### None

- Biome Polarities: Curated (Sun|Moon|Heat|Cold)
- Upgrades: Standard set (Jump, DoubleJump, Dash, WallJump)
- Use Case: Consistent, balanced gameplay

### Partial

- Biome Polarities: Randomized (2-6 from available set)
- Upgrades: Curated standard set
- Use Case: Visual variety with predictable progression

## Optional Shape-First Flow

1. World Shape (organic, optional) - Coarse WFC/cellular pass creates an organic silhouette
1. District Fit-to-Shape - Places districts inside the silhouette (Poisson within mask)
1. Connection Building - Creates graph connections between districts
1. Rule Randomization - Adapts biome polarities and upgrade rules
1. Sector/Room Hierarchy - Subdivides districts into gameplay areas
1. WFC Integration - Runs existing WFC with placed coordinates
1. Debug Visualization - Comprehensive gizmo overlays for validation

## District Placement Strategies

### Poisson-Disc Sampling (≤16 districts)

When using the ShapeFirstOrganic flow, sampling is constrained to the generated shape mask, producing non-rectangular, organic world layouts with varied aspect ratios.

- Guaranteed no overlaps or clustering
- Scales well to large district counts

## Sector Subdivision

- 6x6 local grid within each district
- Deterministic seeding per district
- Minimum room size: 2x2 local units
- Maximum 6 rooms per sector

## Debug Visualization

- Connections: Directional arrows (single/double for one-way/bidirectional)
- Biome Radius: Wire discs showing influence areas

### Interactive Controls

- Toggle visibility for each element type
- Real-time coordinate and connection display

### ProceduralLayoutPreview Window

- Location: `MetVanDAMN/World Layout Preview`
- Purpose: Inspect procedural layout stages
- Use Case: Design-time validation without entering Play mode

### ProceduralLayoutDemo Component

- Auto-demo mode: Cycles through all stages automatically
- Stage monitoring: Real-time progress tracking
- Debug logging: Detailed output for each stage
- GUI overlay: In-game status display

## Integration Examples

### Basic Setup

```csharp
// 1. Add WorldConfigurationAuthoring to scene
worldConfig.randomizationMode = RandomizationMode.Partial;

worldConfig.seed = 12345;
worldConfig.worldSize = new int2(32, 32);

// 2. Create DistrictAuthoring objects at (0,0) coordinates
// 3. Systems will automatically place and connect districts
```

### Custom Placement

```csharp
// Districts with Level=0 and Coordinates=(0,0) will be processed
var district = entityManager.CreateEntity();
entityManager.AddComponentData(district, new NodeId(1, 0, 0, int2.zero));
entityManager.AddComponentData(district, new WfcState());
```

### Rule Access

```csharp
// Access generated rules after randomization
var ruleSet = SystemAPI.GetSingleton<WorldRuleSet>();
bool hasJumpUpgrade = (ruleSet.AvailableUpgradesMask & (1u << 0)) != 0;
```

## Reachability Guarantees

The system includes several safeguards to ensure world completability:

1. Essential Upgrades: Jump upgrade always available in Full mode
1. Minimum Connections: K-nearest ensures graph connectivity
1. Loop Creation: Random long edges prevent linear progression
1. Distance Validation: Poisson-disc prevents impossible spacing
1. Polarity Minimums: At least 2 polarities always assigned

## Performance Considerations

- Burst Compiled: All systems use Burst for optimal performance
- Deterministic: Same seed produces identical results
- Scalable: Jittered grid handles 100+ districts efficiently
- Memory Efficient: Temporary arrays disposed after use
- One-Shot: Layout systems run once then disable

## Debugging Tips

1. Enable Debug Logging: Set flags in ProceduralLayoutDemo
1. Use Preview Tool: Test algorithms without entering Play mode
1. Check Gizmo Toggles: Verify visualization settings
1. Monitor Stage Progress: Watch for system execution order issues
1. Validate Seeds: Ensure deterministic behavior across runs

## Future Extensions

- Custom Placement Strategies: Plugin architecture for new algorithms
- Biome-Aware Connections: Polarity-based connection filtering
- Dynamic Rule Adaptation: Runtime rule modification
- Procedural Gate Placement: Integration with progression system
- Multi-Level Hierarchies: Support for sub-room divisions
