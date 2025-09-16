# Enemy Naming & Affix Display System API Documentation

## Overview

The Enemy Naming & Affix Display System provides a flexible, rarity-aware approach to enemy naming and visual affix indication. It supports both text names and icon-based affix indicators, scaling naming complexity with enemy rarity, and generates memorable boss names using affix-derived syllables.

## Core Components

### Data Structures

#### `RarityType` Enum
```csharp
public enum RarityType : byte
{
    Common = 0,      // Shows base type only, icons for affixes
    Uncommon = 1,    // Shows base type only, icons for affixes
    Rare = 2,        // Shows full name + icons
    Unique = 3,      // Shows full name + icons
    MiniBoss = 4,    // Shows procedural name + icons (1-2 syllables)
    Boss = 5,        // Shows procedural name + icons (2-3 syllables)
    FinalBoss = 6    // Shows procedural name + icons (3+ syllables)
}
```

#### `EnemyAffix` Component
```csharp
public struct EnemyAffix : IComponentData
{
    public FixedString64Bytes Id;                           // Unique identifier
    public FixedString64Bytes DisplayName;                  // Text for full names
    public FixedString64Bytes IconRef;                      // UI icon asset reference
    public FixedList512Bytes<FixedString32Bytes> BossSyllables; // Syllables for boss names
    public TraitCategory Category;                          // Combat, Movement, etc.
    public byte Weight;                                     // Spawn probability weight
    public byte ToxicityScore;                             // Balancing metric
    public FixedString128Bytes Description;                // Tooltip text
}
```

#### `EnemyProfile` Component
```csharp
public struct EnemyProfile : IComponentData
{
    public RarityType Rarity;               // Determines naming behavior
    public FixedString64Bytes BaseType;    // Base enemy type (e.g., "Crawler")
    public uint GenerationSeed;            // For consistent random generation
}
```

#### `EnemyNaming` Component
```csharp
public struct EnemyNaming : IComponentData
{
    public FixedString128Bytes DisplayName;    // Final generated name
    public bool ShowFullName;                  // Whether to show full name
    public bool ShowIcons;                     // Whether to show affix icons
    public AffixDisplayMode DisplayMode;       // Display configuration
}
```

## System Architecture

### EnemyNamingSystem
The core system that processes entities marked with `NeedsNameGeneration` and generates appropriate names based on rarity and affixes.

**Update Flow:**
1. Query entities with `EnemyProfile` + `NeedsNameGeneration`
2. Determine display behavior based on rarity
3. Generate names (base type, affixed, or procedural)
4. Create `EnemyNaming` component with results
5. Remove `NeedsNameGeneration` tag

### Naming Rules

#### Commons & Uncommons
- **Display**: Base type only (e.g., "Crawler")
- **Affixes**: Icons only, no text names
- **Example**: "Crawler" with shuffling movement icon

#### Rares & Uniques
- **Display**: Full name with prefix/suffix from affixes
- **Format**: `"[Prefix] [BaseType] of [Suffix]"`
- **Example**: "Venomous Crawler of Fury"

#### Bosses (MiniBoss, Boss, FinalBoss)
- **Display**: Procedural names from affix syllables
- **Generation**: Concatenate syllables with connective vowels for readability
- **Length**: Increases with boss tier (MiniBoss: 1-2, Boss: 2-3, FinalBoss: 3+)
- **Example**: "Bermagizedd" (Berserker + Mage + Summoner)

## Usage Examples

### Basic Enemy Creation
```csharp
// Create enemy entity
var enemy = entityManager.CreateEntity();

// Set profile
entityManager.AddComponentData(enemy, new EnemyProfile(
    RarityType.Rare, 
    "Sentinel", 
    12345 // seed
));

// Assign random affixes
EnemyAffixDatabase.AssignRandomAffixes(entityManager, enemy, RarityType.Rare, 12345);

// Mark for name generation
entityManager.AddComponentData(enemy, new NeedsNameGeneration());

// System will automatically generate name on next update
```

### Custom Affix Assignment
```csharp
// Create affix buffer manually
var affixBuffer = entityManager.AddBuffer<EnemyAffixBufferElement>(enemy);

// Add specific affixes by finding them in the database
var query = entityManager.CreateEntityQuery(typeof(EnemyAffix), typeof(AffixDatabaseTag));
var affixEntities = query.ToEntityArray(Allocator.Temp);

foreach (var affixEntity in affixEntities)
{
    var affix = entityManager.GetComponentData<EnemyAffix>(affixEntity);
    if (affix.Id.ToString() == "berserker")
    {
        affixBuffer.Add(affix);
        break;
    }
}
```

### Configuration
```csharp
// Set global display mode
var config = new EnemyNamingConfig(
    AffixDisplayMode.NamesAndIcons,  // Show both names and icons
    maxDisplayedAffixes: 4,          // Limit to 4 icons
    useProceduralBossNames: true,    // Enable syllable-based boss names
    namingSeed: 12345                // Consistent generation seed
);

var configEntity = entityManager.CreateEntity();
entityManager.AddComponentData(configEntity, config);
```

## Affix Database

### Predefined Affixes
The system includes 24 predefined affixes across 5 categories:

#### Combat Modifiers (5)
- **berserker**: "Ber", "Zerk" syllables
- **armored**: "Arm", "Mor" syllables  
- **regenerator**: "Re", "Gen" syllables
- **poisonous**: "Ven", "Ox" syllables
- **explosive**: "Ex", "Plos" syllables

#### Movement Modifiers (5)
- **teleporting**: "Tel", "Port" syllables
- **sprinting**: "Spr", "Int" syllables
- **shuffling**: "Shuf", "Ling" syllables
- **flying**: "Aero", "Wing" syllables
- **burrowing**: "Bur", "Row" syllables

#### Behavior Modifiers (4)
- **pack_hunter**: "Pack", "Hun" syllables
- **ambusher**: "Amb", "Ush" syllables
- **cowardly**: "Cow", "Ard" syllables
- **patrol**: "Pat", "Rol" syllables

#### Boss-Only Modifiers (5)
- **summoner**: "Mon", "Zedd" syllables
- **arena_shaper**: "Are", "Sha" syllables
- **trap_layer**: "Trap", "Lay" syllables
- **meteor_slam**: "Met", "Slam" syllables
- **gravity_shift**: "Grav", "Ity" syllables

#### Unique/Named Modifiers (5)
- **eternal_flame**: "Eter", "Flam" syllables
- **void_touched**: "Void", "Tuch" syllables
- **frostbound**: "Fros", "Boun" syllables
- **stormcaller**: "Stor", "Call" syllables
- **soulrender**: "Soul", "Ren" syllables

### Database Operations
```csharp
// Initialize database (call once at startup)
EnemyAffixDatabase.InitializeDatabase(entityManager);

// Get affixes by category
var combatAffixes = EnemyAffixDatabase.GetAffixesByCategory(
    entityManager, 
    TraitCategory.Combat, 
    Allocator.Temp
);

// Assign random affixes based on rarity
EnemyAffixDatabase.AssignRandomAffixes(entityManager, enemy, RarityType.Boss, seed);
```

## UI Integration

### Icon Display Component
```csharp
public class AffixIconDisplayUI : MonoBehaviour
{
    public void Initialize(Entity entity, EntityManager em);
    public void UpdateDisplay();
}
```

### Usage in Unity
1. Add `AffixIconDisplayUI` component to UI GameObject
2. Assign icon prefab and container transform
3. Call `Initialize()` with target enemy entity
4. System automatically updates icon display based on enemy's affix data

### Configuration Options
- **maxIcons**: Maximum number of icons to display
- **iconSpacing**: Distance between icons in UI units
- **useWorldSpace**: Display above entity vs in UI panel
- **iconPrefab**: Template GameObject for icon creation

## Testing

### Unit Tests
```csharp
[Test]
public void Common_Enemy_Shows_BaseType_Only();
[Test] 
public void Boss_Generates_Procedural_Name_From_Syllables();
[Test]
public void Icon_Display_Respects_Global_Configuration();
```

### Integration Tests
```csharp
[Test]
public void Complete_Enemy_Creation_Workflow();
[Test]
public void Multiple_Enemy_Types_Generate_Correctly();
[Test]
public void Boss_Progression_Shows_Name_Complexity_Increase();
```

## Performance Considerations

- **ECS-based**: Leverages Unity's Entity Component System for optimal performance
- **Burst-compiled**: Core systems use Burst compilation for maximum speed
- **Memory-efficient**: Uses FixedString types to avoid allocations
- **Deterministic**: Consistent results with same seeds for networking/replay

## Extension Points

### Custom Affixes
Create new affixes by adding entities with `EnemyAffix` component and `AffixDatabaseTag`:

```csharp
var customAffix = new EnemyAffix(
    "my_custom_affix",
    "My Custom Affix", 
    "icon_custom.png",
    TraitCategory.Combat,
    weight: 3,
    toxicityScore: 2,
    "Description of custom behavior"
);

customAffix.AddBossSyllable("Cus");
customAffix.AddBossSyllable("Tom");

var affixEntity = entityManager.CreateEntity();
entityManager.AddComponentData(affixEntity, customAffix);
entityManager.AddComponentData(affixEntity, new AffixDatabaseTag());
```

### Custom Naming Rules
Override `EnemyNamingSystem` to implement custom naming logic:

```csharp
public partial struct CustomNamingSystem : ISystem
{
    // Implement custom naming behavior
    // Can query EnemyProfile + NeedsNameGeneration
    // Generate custom EnemyNaming component
}
```