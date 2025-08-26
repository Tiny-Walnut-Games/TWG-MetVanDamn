# Meta-Progression Hooks Integration - Implementation Plan

**Feature Gap**: No placement of persistent unlocks or ability-gated routes in worldgen  
**Priority**: High  
**Complexity**: Medium  
**Dependencies**: Existing GateCondition.cs, SectorRefineSystem.cs

## Overview

Integrate meta-progression elements directly into the world generation pipeline, creating persistent unlocks, ability-gated routes, and upgrades that influence subsequent runs while preserving the existing progression gate architecture.

## Current State Analysis

### Existing Implementation
- **GateCondition.cs**: Handles polarity-aware locks and basic progression requirements
- **SectorRefineSystem.cs**: Manages sector layout and connection refinement
- **Connection System**: Manages traversal between areas with ability requirements

### Identified Gaps
- No persistent progression state management across runs
- Missing integration of meta-upgrades into procedural layouts
- No placement system for upgrade/unlock nodes
- Limited connection between progression unlocks and world structure

## Architecture Design

### Core Components

```csharp
// Meta-progression node for persistent unlocks
public struct MetaProgressionNode : IComponentData
{
    public MetaUnlockType UnlockType;
    public GateCondition RequiredCondition;    // Reuse existing gate logic
    public bool IsPersistent;                  // Survives death/reset
    public BiomeType PreferredBiome;
    public float PlacementWeight;              // Influence on generation
    public Entity VisualPrefab;               // Upgrade shrine, tool, etc.
}

public enum MetaUnlockType : byte
{
    // Movement Abilities
    DoubleJump, AirDash, WallClimb, GrappleHook,
    
    // Tools & Keys  
    MasterKey, BiomeKey, SpecialTool, 
    
    // Permanent Upgrades
    HealthIncrease, EnergyExpansion, MovementSpeed,
    
    // Route Modifiers
    SecretSense, HazardImmunity, PassiveRegeneration
}

// Integration with existing gate system
public struct MetaGateCondition : IComponentData  
{
    public GateCondition BaseCondition;       // Existing polarity/ability logic
    public MetaUnlockType RequiredUnlock;     // Additional meta requirement
    public bool IsOptionalRoute;              // Creates shortcut vs. critical path
}
```

### Enhanced Systems

#### 1. MetaProgressionPlacementSystem
```csharp
[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(SectorRefineSystem))]
public partial struct MetaProgressionPlacementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Place meta-progression nodes in appropriate locations
        // Integrate with existing sector refinement
        // Balance unlock placement across biomes and difficulty
    }
}
```

#### 2. PersistentProgressionState
```csharp
// Global state manager for meta-progression
public struct PersistentProgressionState : IComponentData, IEnableableComponent
{
    public FixedList64Bytes<MetaUnlockType> UnlockedAbilities;
    public FixedList32Bytes<BiomeType> UnlockedBiomes;
    public uint TotalRunsCompleted;
    public float ProgressionModifier;         // Affects future generation
}
```

## Implementation Steps

### Phase 1: Core Integration (Week 1-2)
1. **Extend GateCondition System**:
   - Add MetaGateCondition component that wraps existing GateCondition
   - Modify gate evaluation to check meta-progression state
   - Preserve existing polarity and ability requirements

2. **Create Meta-Progression Data Model**:
   - Define MetaProgressionNode and supporting enums
   - Implement persistent state management across runs
   - Add serialization for save/load functionality

3. **Basic Placement Logic**:
   - Create MetaProgressionPlacementSystem skeleton
   - Integrate with SectorRefineSystem for placement timing
   - Add basic validation and testing framework

### Phase 2: Worldgen Integration (Week 3-4)
1. **Route Generation Enhancement**:
   - Modify connection generation to create ability-gated routes
   - Add optional route tagging for meta-progression shortcuts
   - Ensure critical path remains accessible without meta-unlocks

2. **Biome-Specific Placement**:
   - Integrate with BiomeType system for themed unlocks
   - Add placement weight calculations based on biome affinity
   - Create unlock distribution algorithms for balanced progression

3. **Visual Integration**:
   - Connect with BiomeArtIntegrationSystem for visual representation
   - Add upgrade shrine/altar prefab support
   - Implement discovery feedback and visual polish

### Phase 3: Advanced Features (Week 4-5)
1. **Dynamic Difficulty Scaling**:
   - Adjust world generation based on unlocked abilities
   - Create harder optional routes for upgraded players
   - Balance challenge progression with meta-progression

2. **Cross-Run Persistence**:
   - Implement save/load for persistent progression state
   - Add meta-progression analytics and balance tracking
   - Create unlock condition validation and progression gates

3. **Integration with Existing Systems**:
   - Connect with secret room system for hidden upgrades
   - Integrate with boss system for major unlock rewards
   - Add support for traversal upgrade system integration

## Technical Implementation Details

### Data Flow Integration
```csharp
// Enhanced sector refinement with meta-progression
[UpdateInGroup(typeof(SectorRefineSystem))]
public partial struct MetaProgressionSectorRefiner : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var progressionState = GetSingleton<PersistentProgressionState>();
        
        Entities.ForEach((ref SectorData sector, in BiomeType biome) => 
        {
            // Calculate meta-progression influence on sector
            float progressionWeight = CalculateProgressionWeight(progressionState, biome);
            
            // Place appropriate meta-progression nodes
            PlaceMetaProgressionNodes(ref sector, progressionWeight, biome);
            
            // Modify connections based on unlocked abilities
            AdjustConnectionsForMetaProgression(ref sector, progressionState);
            
        }).Run();
    }
}
```

### Persistence Layer
```csharp
// Save/Load integration for meta-progression
public static class MetaProgressionPersistence
{
    public static void SaveProgressionState(PersistentProgressionState state)
    {
        // Serialize to persistent storage
        // Include validation and backup logic
    }
    
    public static PersistentProgressionState LoadProgressionState()
    {
        // Deserialize from storage with fallback defaults
        // Validate unlock consistency
    }
}
```

## Integration Points

### Existing Systems Enhancement
- **GateCondition.cs**: Add meta-progression evaluation wrapper
- **SectorRefineSystem.cs**: Include meta-progression placement pass
- **ConnectionBuilderSystem.cs**: Add ability-gated route generation
- **BiomeArtIntegrationSystem.cs**: Visual representation of upgrades

### Future System Hooks
- **Secret Room System**: Hidden meta-progression unlocks
- **Boss Integration**: Major upgrade rewards from boss encounters
- **Environmental Storytelling**: Lore integration with upgrade discovery
- **Traversal Upgrades**: Direct integration with movement abilities

## Testing Strategy

### Progression Testing
- Validate unlock sequences work correctly across multiple runs
- Test persistence and save/load functionality
- Verify balance between gated and open routes

### Integration Testing  
- Ensure existing gate condition functionality is preserved
- Test biome-specific placement accuracy
- Validate performance impact on world generation

### Gameplay Testing
- Player progression feel and pacing validation
- Meta-progression motivation and reward satisfaction
- Long-term progression curve and replay value assessment

## Success Criteria

### Functional Requirements
- [ ] Meta-progression unlocks persist correctly across runs
- [ ] Ability-gated routes provide meaningful player choice
- [ ] Integration preserves existing progression gate functionality
- [ ] Unlock placement feels authored and balanced across biomes
- [ ] Save/load system maintains progression state reliably

### Technical Requirements
- [ ] Burst-compatible implementation with optimal performance
- [ ] Modular integration that doesn't break existing systems
- [ ] Comprehensive test coverage for all unlock types
- [ ] Memory-efficient persistence with minimal storage overhead
- [ ] Future-proof design for additional unlock types

### Genre Compliance  
- [ ] Meets roguelike meta-progression expectations
- [ ] Provides meaningful long-term progression goals
- [ ] Creates satisfying "unlock moment" experiences
- [ ] Balances progression rewards with challenge scaling
- [ ] Supports multiple progression paths and play styles

## Risk Mitigation

### Technical Risks
- **Save System Complexity**: Start with simple serialization, iterate
- **Performance Impact**: Profile generation time with meta-progression enabled
- **Integration Conflicts**: Comprehensive testing with existing systems

### Design Risks
- **Progression Pacing**: Playtesting and data-driven balance adjustments
- **Choice Overwhelming**: Clear visual feedback and progressive unlock introduction
- **Grind Prevention**: Ensure meaningful progression without excessive repetition

## Timeline

| Week | Milestone | Deliverables |
|------|-----------|--------------|
| 1-2  | Core Integration | MetaGateCondition wrapper, basic placement system, data model |
| 3-4  | Worldgen Integration | Route generation, biome placement, visual integration |
| 5    | Advanced Features | Dynamic scaling, persistence, system integration |

**Total Estimated Effort**: 5 weeks for complete implementation  
**Minimum Viable Product**: 3 weeks for basic meta-progression placement

---

*This implementation plan builds directly on the existing progression gate architecture while adding the persistent meta-progression layer essential for roguelike genre compliance.*