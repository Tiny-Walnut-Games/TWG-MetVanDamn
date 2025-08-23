# Procedural World Layout System

The Procedural World Layout system is a comprehensive enhancement to MetVanDAMN's existing WFC-based world generation. It provides deterministic district placement, adaptive rule randomization, and hierarchical sector/room subdivision.

## Overview

The system operates in six distinct stages:

1. **District Pre-Layout** - Places districts procedurally before WFC
2. **Connection Building** - Creates graph connections between districts  
3. **Rule Randomization** - Adapts biome polarities and upgrade rules
4. **Sector/Room Hierarchy** - Subdivides districts into gameplay areas
5. **WFC Integration** - Runs existing WFC with placed coordinates
6. **Debug Visualization** - Comprehensive gizmo overlays for validation

## System Architecture

### Core Components

- `WorldConfiguration` - Extended with `RandomizationMode` enum
- `DistrictLayoutDoneTag` - Gates downstream systems
- `WorldRuleSet` - Stores randomized biome/upgrade rules
- `SectorHierarchyData` - Local grid subdivision data
- `RoomHierarchyData` - BSP room generation data

### ECS Systems (Execution Order)

1. `BuildConnectionBuffersSystem` (existing, authoring layer)
2. `DistrictLayoutSystem` (new, procedural placement)
3. `ConnectionBuilderSystem` (new, graph connectivity)
4. `RuleRandomizationSystem` (new, adaptive rules)
5. `SectorRoomHierarchySystem` (new, subdivision)
6. `DistrictWfcSystem` (updated, gated by layout completion)

## Randomization Modes

### None
- **Biome Polarities**: Curated (Sun|Moon|Heat|Cold)
- **Upgrades**: Standard set (Jump, DoubleJump, Dash, WallJump)
- **Use Case**: Consistent, balanced gameplay

### Partial  
- **Biome Polarities**: Randomized (2-6 from available set)
- **Upgrades**: Curated standard set
- **Use Case**: Visual variety with predictable progression

### Full
- **Biome Polarities**: Randomized (2-6 from available set)
- **Upgrades**: Randomized with reachability guards
- **Use Case**: Maximum variety with ensured completability

## District Placement Strategies

### Poisson-Disc Sampling (≤16 districts)
- Organic spacing using rejection sampling
- Minimum distance = 20% of world size
- Maximum 30 placement attempts per district
- Fallback to random placement if needed

### Jittered Grid (>16 districts)
- Grid-based placement with 30% jitter
- Shuffled cell assignment for variation
- Guaranteed no overlaps or clustering
- Scales well to large district counts

## Connection Graph Generation

### K-Nearest Neighbors
- Each district connects to up to 3 nearest neighbors
- Bidirectional connections with distance-based costs
- Duplicate filtering to prevent redundancy

### Random Long Edges
- 1 long edge per 3 districts (minimum 1)
- Creates loops for backtracking and replayability
- Higher traversal cost (15% vs 10% for local connections)

## Sector/Room Hierarchy

### Sector Subdivision
- 6x6 local grid within each district
- 2-4 sectors per district (randomized)
- Jittered placement within grid cells
- Deterministic seeding per district

### Room Generation (BSP)
- Binary Space Partitioning within sectors
- Minimum room size: 2x2 local units
- Maximum 6 rooms per sector
- Room types: Normal, Entrance, Exit, Boss, Treasure, Shop, Save, Hub

## Debug Visualization

### Gizmo Features
- **Unplaced Districts**: Gray wireframe placeholders at (0,0)
- **Placed Districts**: Biome-colored filled areas with labels
- **Connections**: Directional arrows (single/double for one-way/bidirectional)
- **Biome Radius**: Wire discs showing influence areas
- **Randomization Mode**: HUD display of current settings

### Interactive Controls
- Toggle visibility for each element type
- Preview layout button (runs algorithms in edit mode)
- Frame all districts camera tool
- Real-time coordinate and connection display

## Editor Tools

### ProceduralLayoutPreview Window
- **Location**: `MetVanDAMN/World Layout Preview`
- **Features**: Algorithm preview, settings application, district analysis
- **Use Case**: Design-time validation without entering Play mode

### ProceduralLayoutDemo Component
- **Auto-demo mode**: Cycles through all stages automatically
- **Stage monitoring**: Real-time progress tracking
- **Debug logging**: Detailed output for each stage
- **GUI overlay**: In-game status display

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

1. **Essential Upgrades**: Jump upgrade always available in Full mode
2. **Minimum Connections**: K-nearest ensures graph connectivity  
3. **Loop Creation**: Random long edges prevent linear progression
4. **Distance Validation**: Poisson-disc prevents impossible spacing
5. **Polarity Minimums**: At least 2 polarities always assigned

## Performance Considerations

- **Burst Compiled**: All systems use Burst for optimal performance
- **Deterministic**: Same seed produces identical results
- **Scalable**: Jittered grid handles 100+ districts efficiently
- **Memory Efficient**: Temporary arrays disposed after use
- **One-Shot**: Layout systems run once then disable

## Debugging Tips

1. **Enable Debug Logging**: Set flags in ProceduralLayoutDemo
2. **Use Preview Tool**: Test algorithms without entering Play mode
3. **Check Gizmo Toggles**: Verify visualization settings
4. **Monitor Stage Progress**: Watch for system execution order issues
5. **Validate Seeds**: Ensure deterministic behavior across runs

## Future Extensions

- **Custom Placement Strategies**: Plugin architecture for new algorithms
- **Biome-Aware Connections**: Polarity-based connection filtering
- **Dynamic Rule Adaptation**: Runtime rule modification
- **Procedural Gate Placement**: Integration with progression system
- **Multi-Level Hierarchies**: Support for sub-room divisions