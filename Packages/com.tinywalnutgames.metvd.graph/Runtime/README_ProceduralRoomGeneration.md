# Procedural Room Generation — Best Fit Matrix & Pipeline

## Overview

This system implements a comprehensive procedural room generation pipeline that chooses the optimal generation strategy based on gameplay goals. The system includes seven specialized generators and a six-step pipeline flow that ensures coherent, skill-aware room creation.

## 🎯 Best Fit Matrix

| Gameplay Goal | Generator Type | Why It Fits | Implementation |
|---------------|----------------|-------------|----------------|
| **Movement Skill Puzzles** (dash, wall‑cling, grapple) | `PatternDrivenModular` | Deliberate placement of skill gates with Movement Capability Tags | Uses `RoomPatternElement` buffer with skill requirements |
| **Platforming Puzzle Testing Grounds** (jump/double‑jump) | `ParametricChallenge` | Physics-aware platform placement with Jump Arc Solver validation | Uses `JumpPhysicsData` and `JumpArcValidation` components |
| **Standard Platforming + Optional Secrets** | `WeightedTilePrefab` | Easy-flow layouts with secret area hooks | Uses `SecretAreaConfig` for hidden alcoves and alternate routes |
| **Vertical Layout Rooms** | `StackedSegment` | Builds rooms in vertical slices with coherent climb/jump routes | Ensures vertical connectivity between segments |
| **Horizontal Layout Rooms** | `LinearBranchingCorridor` | Rhythm-based pacing with challenge/rest/secret beats | Creates branching paths for speedrun-friendly design |
| **Top‑World Terrain Generation** | `BiomeWeightedHeightmap` | Noise + biome masks for terrain sculpting | Integrates with ECS terrain chunks |
| **Sky Biome Generation** | `LayeredPlatformCloud` | Moving cloud platforms with biome-specific motion patterns | Uses `CloudMotionType` for different movement behaviors |

## 🔑 Implementation Keys

### Skill-Aware Generation
Every room element can be tagged with required and optional skills:

```csharp
var skillTag = new SkillTag(
    requiredSkill: Ability.Dash,
    optionalSkill: Ability.WallJump,
    skillDifficulty: 0.8f
);
```

### Jump Arc Solver
Physics-aware reachability validation ensures all platforms are accessible:

```csharp
var jumpPhysics = new JumpPhysicsData(
    maxHeight: 4.0f,
    maxDistance: 6.0f,
    gravity: 9.81f,
    speed: 5.0f,
    hasDoubleJump: true
);

bool canReach = JumpArcSolver.IsReachable(startPos, targetPos, jumpPhysics);
```

### Secret Area Hooks
Reserve percentage of room tiles for hidden content:

```csharp
var secretConfig = new SecretAreaConfig(
    secretPercentage: 0.15f,
    minSize: new int2(2, 2),
    maxSize: new int2(4, 4),
    secretSkill: Ability.Bomb,
    useDestructibleWalls: true,
    useAlternateRoutes: true
);
```

### Biome Affinity
Elements are filtered by biome compatibility:

```csharp
var biomeAffinity = new BiomeAffinity(
    primaryBiome: BiomeType.SolarPlains,
    polarityAffinity: Polarity.Sun,
    selectionWeight: 0.8f
);

bool compatible = biomeAffinity.IsCompatibleWith(BiomeType.SolarPlains, Polarity.Sun);
```

## 🛠 Pipeline Flow

The system follows a six-step pipeline for each room:

### 1. Biome Selection
- Choose biome & sub‑biome based on world generation rules
- Apply biome‑specific prop/hazard sets
- Factor in polarity constraints

### 2. Layout Type Decision
- Decide vertical vs. horizontal orientation
- Consider biome constraints (sky biomes favor verticality)
- Use room aspect ratio as input

### 3. Room Generator Choice
- Select generator type based on room type and layout
- Filter available modules by biome and required skills
- Apply Best Fit Matrix logic

### 4. Content Pass
- Place hazards, props, and secrets using selected generator
- Run Jump Arc Solver to validate reachability
- Add skill-specific challenges based on available abilities

### 5. Biome‑Specific Overrides
- Apply visual and mechanical overrides
- Add moving clouds in sky biomes
- Include tech zone hazards and automated systems

### 6. Nav Generation (Post‑Content)
- Mark empty tiles above traversable tiles as navigable
- Calculate jump vectors between platforms
- Add movement‑type‑aware navigation edges

## 🚀 Usage Examples

### Basic Room Generation
```csharp
// Room generation is automatic when RoomHierarchyData is created
var roomData = new RoomHierarchyData(
    bounds: new RectInt(0, 0, 16, 12),
    type: RoomType.Normal,
    isLeafRoom: true
);

// System automatically creates RoomGenerationRequest and processes pipeline
```

### Custom Generator Selection
```csharp
var request = new RoomGenerationRequest(
    generatorType: RoomGeneratorType.PatternDrivenModular,
    targetBiome: BiomeType.VolcanicCore,
    targetPolarity: Polarity.Heat,
    availableSkills: Ability.Jump | Ability.Dash | Ability.HeatResistance,
    seed: 12345
);
```

### Physics-Based Validation
```csharp
// For parametric challenge rooms
var jumpPhysics = new JumpPhysicsData(4.0f, 6.0f, 9.81f, 5.0f, true, false, true);
var platforms = new NativeArray<float2>(platformCount, Allocator.Temp);
// ... populate platforms
bool roomIsTraversable = JumpArcSolver.ValidateRoomReachability(
    platforms, obstacles, jumpPhysics
);
```

## 🧩 Generator Details

### PatternDrivenModular
- Creates skill-specific patterns (dash gaps, wall-climb shafts, grapple points)
- Uses `RoomPatternElement` buffer to store pattern data
- Filters patterns based on available player abilities

### ParametricChallenge
- Calculates optimal platform spacing using jump physics
- Validates reachability between all critical points
- Stores jump connections for navigation

### WeightedTilePrefab
- Generates main flow layout (60% of room area)
- Adds secret areas based on `SecretAreaConfig`
- Creates destructible walls and alternate routes

### StackedSegment
- Divides room into vertical segments
- Ensures climb/jump connectivity between levels
- Adds intermediate platforms for unreachable gaps

### LinearBranchingCorridor
- Creates rhythm-based horizontal progression
- Alternates challenge, rest, and secret beats
- Generates upper/lower branching paths for variety

### BiomeWeightedHeightmap
- Uses biome-specific noise functions for terrain
- Varies height based on biome characteristics
- Adds biome-appropriate features (crystals, lava vents, etc.)

### LayeredPlatformCloud
- Creates horizontal layers of cloud platforms
- Generates floating islands with biome-specific features
- Applies motion patterns based on `CloudMotionType`

## 🔧 System Integration

### ECS Components
- `RoomGenerationRequest` - Pipeline coordination
- `SkillTag` - Skill requirements for room elements
- `BiomeAffinity` - Biome compatibility for content
- `JumpPhysicsData` - Physics parameters for validation
- `SecretAreaConfig` - Secret area generation settings

### Buffer Elements
- `RoomPatternElement` - Pattern-driven generation data
- `RoomModuleElement` - Modular prefab references
- `JumpConnectionElement` - Validated jump connections

### Systems Execution Order
1. `RoomManagementSystem` - Initializes room generation requests
2. `RoomGenerationPipelineSystem` - Orchestrates 6-step pipeline
3. `PatternDrivenModularGenerator` - Skill-based pattern generation
4. `ParametricChallengeGenerator` - Physics-based platform layout
5. `WeightedTilePrefabGenerator` - Standard content generation
6. `StackedSegmentGenerator` - Vertical room structure
7. `LinearBranchingCorridorGenerator` - Horizontal flow layout
8. `BiomeWeightedHeightmapGenerator` - Terrain generation
9. `LayeredPlatformCloudGenerator` - Sky biome generation

## 📊 Performance Considerations

- All systems are Burst-compiled for optimal performance
- Deterministic generation using seeded random numbers
- Memory-efficient buffer systems minimize allocations
- Physics validation runs only when necessary
- Parallel processing where possible

## 🧪 Testing

Comprehensive test suite covers:
- Component initialization and validation
- Jump arc physics calculations
- Biome compatibility checks
- Pipeline step progression
- Generator type selection logic

Run tests with: `[TestFixture] ProceduralRoomGenerationTests`

## 🔮 Future Extensions

- **Custom Placement Strategies**: Plugin architecture for new algorithms
- **Dynamic Difficulty Adaptation**: Runtime adjustment based on player performance  
- **Procedural Audio Integration**: Biome-specific ambient soundscapes
- **Multi-Level Hierarchies**: Support for sub-room divisions
- **AI Behavior Integration**: Generator-aware enemy placement
- **Real-time Preview**: Editor tools for visualizing generation results

## 📚 Related Documentation

- [BiomeArtProfile_UserGuide.md](../../../Assets/MetVanDAMN/Authoring/BiomeArtProfile_UserGuide.md) - Visual biome integration
- [README_ProceduralLayout.md](README_ProceduralLayout.md) - Overall layout system
- [DistrictSectorRoomFeatures.md](../../../docs/DistrictSectorRoomFeatures.md) - Hierarchical structure

This system provides the foundation for rich, skill-aware procedural room generation that adapts to player progression and creates compelling Metroidvania gameplay experiences.