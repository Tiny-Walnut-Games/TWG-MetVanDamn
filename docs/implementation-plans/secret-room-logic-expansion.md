# Secret Room Logic Expansion - Implementation Plan

**Feature Gap**: No destructible walls, hidden switches, or alternate entrances  
**Priority**: High  
**Complexity**: Low-Medium  
**Dependencies**: Existing SecretAreaConfig, ProceduralRoomGeneration.cs

## Overview

Expand the existing secret area system beyond basic percentage allocation to include destructible walls, hidden switches, puzzle sequences, and multiple discovery methods that create meaningful exploration rewards.

## Current State Analysis

### Existing Implementation
- **SecretAreaConfig**: Basic secret area percentage and size configuration
- **ProceduralRoomGeneration.cs**: Integration with room generation pipeline
- **Capabilities**: Reserved space allocation, basic secret area placement

### Identified Gaps
- Only supports space reservation, no actual secret access mechanisms
- Missing destructible wall system for bombing/breaking through barriers
- No hidden switch or puzzle sequence implementation
- Limited to single entry method per secret area

## Architecture Design

### Enhanced Components

```csharp
// Expanded secret room data structure
public struct SecretRoomData : IComponentData
{
    public SecretAccessType PrimaryAccess;
    public SecretAccessType SecondaryAccess;    // Optional alternate access
    public TileType DestructibleWallType;
    public Entity SwitchEntity;
    public float DiscoveryDifficulty;           // Affects rewards
    public BiomeType BiomeAffinity;
    public FixedList32Bytes<int2> EntryPoints;
}

public enum SecretAccessType : byte
{
    None = 0,
    DestructibleWall,    // Bomb/attack to break through
    HiddenSwitch,        // Switch hidden in environment
    PuzzleSequence,      // Multi-step activation
    RequiredAbility,     // Needs specific movement ability
    TimedAccess,         // Limited time window
    EnvironmentalTrigger // Interaction with biome elements
}

// Switch and trigger data
public struct SecretTrigger : IComponentData
{
    public Entity LinkedSecretRoom;
    public TriggerActivation ActivationType;
    public float ActivationRange;
    public bool RequiresSpecificAbility;
    public Ability RequiredAbility;
}
```

### Enhanced Systems

#### 1. SecretRoomGeneratorSystem
```csharp
[BurstCompile]
[UpdateInGroup(typeof(ProceduralRoomGeneratorSystem))]
public partial struct SecretRoomGeneratorSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Generate secret rooms with specific access methods
        // Create destructible walls and hidden switches
        // Integrate with biome theming for appropriate mechanisms
    }
}
```

## Implementation Steps

### Phase 1: Access Method Framework (Week 1-2)
1. **Expand SecretAreaConfig**: Add access type selection and configuration
2. **Create Destructible Wall System**: Tile-based destruction with ability requirements
3. **Basic Switch Implementation**: Hidden switches with proximity activation

### Phase 2: Advanced Access Methods (Week 3-4)
1. **Puzzle Sequences**: Multi-step activation requiring specific actions
2. **Biome Integration**: Biome-specific access methods (ice melting, plant growth, etc.)
3. **Visual Polish**: Clear but subtle visual cues for discovery

## Success Criteria
- [ ] Multiple access methods create varied discovery experiences
- [ ] Destructible walls provide satisfying bombing/breaking mechanics
- [ ] Hidden switches are discoverable but not obvious
- [ ] Integration preserves existing secret area functionality
- [ ] Biome-specific theming creates coherent world experience

**Timeline**: 4 weeks total | 2 weeks MVP