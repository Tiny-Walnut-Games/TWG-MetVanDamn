# 🎯 MetVanDAMN Procedural Leveling Perk System - Complete Usage Guide

## 🌟 Overview

The **Procedural Leveling Perk System** is a comprehensive upgrade framework for MetVanDAMN that provides:

- **Seed-Based Choice Generation** - World seed influences upgrade availability
- **Biome-Aware Curation** - Current biome affects upgrade weights
- **Player State Filtering** - Only viable upgrades based on current abilities
- **Category-Balanced Rolling** - Ensures diverse upgrade choices
- **Persistent Save/Load** - Chosen upgrades survive game sessions
- **Complete UI Integration** - Polished level-up choice interface
- **Effect Application** - Seamless stat and ability modifications

## 🚀 Quick Start

### Step 1: Demo Scene Generation
```
Tools → MetVanDAMN → Create Base DEMO Scene → Complete 2D Platformer Demo
```

This automatically creates a complete demo scene with the upgrade system fully integrated.

### Step 2: Play and Test
- **Hit Play** - The upgrade system is automatically configured
- **F1** - Gain 50 XP (debug)
- **F2** - Force level up (debug)
- **F3** - Force show upgrade choices (debug)
- **F4** - Reset progression (debug)

### Step 3: Level Up Experience
1. Gain enough XP to reach the next level
2. **Modal UI appears** with 3-4 upgrade choices
3. **Click an upgrade** to select it
4. **Effects apply immediately** - stats change, abilities gained
5. **Game resumes** with your new capabilities

## 🏗️ System Architecture

### Core Components

#### **UpgradeDefinition** (ScriptableObject)
Individual upgrade configurations with:
- **Basic Info**: Name, description, icon, category
- **Requirements**: Level, abilities, dependencies, conflicts
- **Effects**: Granted abilities, stat modifiers, custom effects
- **Weighting**: Base weight, biome multipliers, uniqueness

#### **UpgradeCollection** (ScriptableObject)  
Category-grouped upgrade pools with:
- **Category Assignment**: Movement, Offense, Defense, Utility, Special
- **Biome Weights**: Per-biome multipliers for contextual choices
- **Upgrade References**: Array of UpgradeDefinition assets

#### **LevelUpChoiceSystem** (MonoBehaviour)
Core choice generation logic:
- **Weighted Selection**: Based on seed, biome, player state
- **Category Distribution**: Ensures minimum distinct categories
- **Duplicate Prevention**: Configurable choice uniqueness
- **Event Integration**: Connects to progression and UI systems

#### **PlayerLevelProgression** (MonoBehaviour)
XP tracking and upgrade management:
- **Level Progression**: Configurable XP curves and max level
- **Ability Tracking**: Current abilities and granted upgrades
- **Save/Load Integration**: Persistent progression data
- **Event Broadcasting**: Level-up notifications and stats changes

#### **UpgradeEffectApplicator** (MonoBehaviour)
Stat modification and effect application:
- **Stat System**: Base stats with additive/multiplicative modifiers
- **Component Integration**: Updates player movement, combat, inventory
- **Custom Effects**: Special abilities like double jump, auto-loot
- **Stat Queries**: Runtime stat inspection and comparison

### UI Components

#### **LevelUpChoiceUI** (MonoBehaviour)
Main choice interface:
- **Dynamic Creation**: Builds UI at runtime
- **Game Pause**: Pauses time during choice selection
- **Audio Integration**: Hover and selection sounds
- **Responsive Layout**: Adapts to different screen sizes

#### **LevelUpChoiceButton** (MonoBehaviour)
Individual upgrade buttons:
- **Rich Display**: Name, description, preview, category icon
- **Hover Effects**: Visual feedback and animations
- **Category Visualization**: Color-coded by upgrade type
- **Preview Generation**: Shows stat changes and ability grants

## 📋 Configuration Guide

### Creating New Upgrades

1. **Create UpgradeDefinition Asset**:
   ```
   Assets → Create → MetVanDAMN → Upgrade Definition
   ```

2. **Configure Basic Info**:
   - **Upgrade Name**: Display name for UI
   - **Description**: Detailed explanation
   - **Icon**: Visual representation (optional)
   - **Category**: Movement/Offense/Defense/Utility/Special

3. **Set Requirements**:
   - **Minimum Level**: When this becomes available
   - **Required Abilities**: Must have these to see this upgrade
   - **Conflicting Abilities**: Cannot have these to see this upgrade
   - **Required/Conflicting Upgrades**: Dependency chains

4. **Define Effects**:
   - **Grants Abilities**: Enum flags for new abilities
   - **Modifier Type**: Additive/Multiplicative/NewAbility/Enhanced
   - **Target Stat**: Which stat to modify (walkspeed, attackdamage, etc.)
   - **Value**: Modification amount
   - **Custom Effect IDs**: Special effects (doublejump, autoloot, etc.)

5. **Configure Weighting**:
   - **Base Weight**: Relative likelihood to appear
   - **Biome Weight Multiplier**: Biome-specific adjustments
   - **Is Unique**: Can only be taken once
   - **Allow Duplicates**: Can appear multiple times

### Creating Upgrade Collections

1. **Create UpgradeCollection Asset**:
   ```
   Assets → Create → MetVanDAMN → Upgrade Collection
   ```

2. **Assign Category**: Must match contained upgrades

3. **Add Upgrades**: Drag UpgradeDefinition assets to array

4. **Configure Biome Weights**:
   - **Biome Type**: Which biome this affects
   - **Weight Multiplier**: How much to boost/reduce in this biome

### Player Setup

1. **Automatic Setup** (Recommended):
   ```csharp
   // Add to player GameObject
   var setup = player.AddComponent<CompletePlayerSetup>();
   // Configure via inspector or call setup.SetupPlayer()
   ```

2. **Manual Setup**:
   ```csharp
   // Add individual components
   var progression = player.AddComponent<PlayerLevelProgression>();
   var choiceSystem = player.AddComponent<LevelUpChoiceSystem>();
   var effectApplicator = player.AddComponent<UpgradeEffectApplicator>();
   
   // Add player components
   var movement = player.AddComponent<DemoPlayerMovement>();
   var combat = player.AddComponent<DemoPlayerCombat>();
   var inventory = player.AddComponent<DemoPlayerInventory>();
   ```

## 🔧 Integration with Existing Systems

### World Seed Integration

The system automatically retrieves the world seed from:
1. **ECS World Configuration** (preferred)
2. **Fallback**: Time-based + instance ID combination

```csharp
// The choice system automatically uses world seed for:
// - Category weight randomization
// - Individual upgrade weight calculation
// - Choice order determinism
```

### Biome Context Detection

Current implementation uses position heuristics:
```csharp
// Override GetCurrentBiomeContext() for custom biome detection
private Polarity GetCurrentBiomeContext()
{
    // Your biome detection logic here
    return currentBiome;
}
```

### Save/Load Integration

Automatic save/load via PlayerPrefs:
```csharp
// Progression automatically saves on:
// - Level up
// - Upgrade application
// - XP gain (if auto-save enabled)

// Manual save/load
progression.SaveProgression();
progression.LoadProgression();
```

## 🎮 Player Experience Flow

### 1. XP Gain Triggers
- **Combat victories** (integrate with your combat system)
- **Exploration milestones** (integrate with your progression triggers)
- **Quest completion** (integrate with your quest system)

```csharp
// Trigger XP gain from your systems
var progression = player.GetComponent<PlayerLevelProgression>();
progression.GainXP(amount);
```

### 2. Level Up Detection
- **Automatic**: System checks XP thresholds on gain
- **Events**: `OnLevelUp` event fires when threshold reached
- **UI Trigger**: Choice system automatically generates options

### 3. Choice Generation
- **Seed-based randomization** ensures deterministic choices
- **Category distribution** guarantees diverse options
- **Player state filtering** shows only viable upgrades
- **Biome influence** provides contextual relevance

### 4. UI Presentation
- **Game pauses** for decision making
- **Rich information display** with previews
- **Category visualization** for easy understanding
- **Hover effects** for exploration

### 5. Effect Application
- **Immediate stat changes** visible in debug logs
- **Ability grants** unlock new mechanics
- **Component updates** modify behavior
- **Persistent storage** survives sessions

## 🧪 Testing and Validation

### Automated Testing
```csharp
// Add to any GameObject in scene
var testRunner = gameObject.AddComponent<UpgradeSystemTestRunner>();
// Press F5 or use context menu "Run All Tests"
```

### Manual Testing Checklist

1. **Basic Functionality**:
   - [ ] Can gain XP
   - [ ] Level up triggers choice UI
   - [ ] Can select upgrades
   - [ ] Effects apply correctly
   - [ ] Progression saves/loads

2. **Choice Generation**:
   - [ ] Multiple categories represented
   - [ ] No invalid choices appear
   - [ ] Biome context affects weights
   - [ ] Same seed produces same choices

3. **UI Integration**:
   - [ ] Modal appears on level up
   - [ ] All information displays correctly
   - [ ] Hover effects work
   - [ ] Selection applies upgrade
   - [ ] Game resumes after choice

4. **Effect Application**:
   - [ ] Stat modifiers apply
   - [ ] Abilities unlock correctly
   - [ ] Component behavior changes
   - [ ] Custom effects work

### Performance Validation

Target: **60fps sustained operation**

Monitor:
- **Choice generation time** (should be < 1ms)
- **UI creation overhead** (should be < 10ms)
- **Effect application time** (should be < 1ms)
- **Save/load performance** (should be < 5ms)

## 🔍 Debugging and Troubleshooting

### Common Issues

#### "No upgrade choices available"
- **Check collections**: Ensure UpgradeCollections are assigned
- **Verify requirements**: Make sure player meets upgrade requirements
- **Check database**: Confirm UpgradeDatabaseManager finds collections

#### "UI doesn't appear on level up"
- **Verify events**: Check OnLevelUp event connections
- **Check UI references**: Ensure LevelUpChoiceUI is in scene
- **Confirm level thresholds**: Verify XP requirements are met

#### "Effects don't apply"
- **Component references**: Check UpgradeEffectApplicator has player components
- **Stat names**: Verify TargetStat names match ApplyStatModifier switch cases
- **Custom effects**: Confirm CustomEffectIds match ApplyCustomEffect cases

### Debug Tools

#### Built-in Debug Controls (F1-F4)
- **F1**: Gain 50 XP
- **F2**: Force level up
- **F3**: Force show choices
- **F4**: Reset progression

#### Console Logging
```csharp
// Enable detailed logging in components
enableDebugLogging = true;
```

#### Inspector Tools
- **Context menus**: "Generate Choices", "Reset Progression", etc.
- **Real-time stats**: View current progression data
- **Component validation**: Verify references and configuration

## 📚 API Reference

### Key Public Methods

#### PlayerLevelProgression
```csharp
void GainXP(int amount)                              // Add XP, check for level up
void ApplyUpgrade(UpgradeDefinition upgrade)         // Apply chosen upgrade
bool HasUpgrade(string upgradeId)                    // Check if upgrade owned
int CalculateXPRequiredForLevel(int level)           // Get XP threshold
```

#### LevelUpChoiceSystem
```csharp
void GenerateUpgradeChoices()                        // Force choice generation
void ChooseUpgrade(UpgradeDefinition upgrade)        // Apply selected upgrade
```

#### UpgradeEffectApplicator
```csharp
void ApplyUpgrade(UpgradeDefinition upgrade)         // Apply upgrade effects
void ResetToDefaults()                               // Clear all modifiers
float GetCurrentStat(string statName)               // Get modified stat value
string GetStatComparison(string statName)           // Get base vs current
```

#### UpgradeDatabaseManager
```csharp
UpgradeDefinition GetUpgradeById(string id)          // Find specific upgrade
List<UpgradeDefinition> GetUpgradesByCategory(...)   // Find by category
void ValidateDatabase()                              // Check for issues
```

### Events

#### PlayerLevelProgression Events
```csharp
System.Action<int> OnLevelUp                        // (newLevel)
System.Action<int, int> OnXPGained                  // (gainedXP, totalXP)
System.Action<UpgradeDefinition> OnUpgradeApplied   // (upgrade)
System.Action<int, int, int> OnStatsChanged         // (level, xp, xpRequired)
```

#### LevelUpChoiceSystem Events
```csharp
System.Action<UpgradeDefinition[]> OnChoicesGenerated  // (choices)
System.Action<UpgradeDefinition> OnUpgradeChosen       // (upgrade)
System.Action<string> OnGenerationError                // (errorMessage)
```

## 🎯 Best Practices

### Upgrade Design
- **Clear names**: Use descriptive, player-friendly names
- **Meaningful descriptions**: Explain what the upgrade does
- **Balanced values**: Test modification amounts for gameplay impact
- **Logical dependencies**: Create sensible requirement chains
- **Category diversity**: Spread upgrades across all categories

### Collection Curation
- **Balanced collections**: Include upgrades of varying power levels
- **Appropriate weighting**: Use base weights to control rarity
- **Biome relevance**: Weight upgrades higher in relevant biomes
- **Avoid dead ends**: Ensure upgrades have meaningful follow-ups

### Player Integration
- **Component order**: Add CompletePlayerSetup last
- **Reference assignment**: Let auto-setup handle component references
- **Custom stats**: Add new stats to UpgradeEffectApplicator
- **Custom effects**: Implement in ApplyCustomEffect method

### Performance Optimization
- **Pool UI elements**: Reuse choice buttons when possible
- **Cache stat values**: Avoid recalculating stats every frame
- **Batch effect application**: Apply multiple effects at once
- **Optimize choice generation**: Limit collection sizes for faster selection

## 📖 Extension Examples

### Adding Custom Stats
```csharp
// In UpgradeEffectApplicator.PlayerStats
public float customStat = 1f;

// In ApplyStatModifier switch
case "customstat":
    currentStats.customStat = ApplyModifierValue(currentStats.customStat, modifier);
    break;

// In GetCurrentStat switch  
case "customstat": return currentStats.customStat;
```

### Creating Custom Effects
```csharp
// In UpgradeEffectApplicator.ApplyCustomEffect
case "myeffect":
    // Your custom effect implementation
    MyCustomEffectHandler.Apply(upgrade.Value);
    break;
```

### Custom Biome Detection
```csharp
// In LevelUpChoiceSystem.GetCurrentBiomeContext
private Polarity GetCurrentBiomeContext()
{
    var biomeDetector = GetComponent<MyBiomeDetector>();
    return biomeDetector?.CurrentBiome ?? Polarity.Any;
}
```

---

## 🎉 Congratulations!

You now have a **complete, production-ready procedural leveling perk system** integrated into MetVanDAMN! 

The system provides:
- ✅ **Deterministic choice generation** based on world seed
- ✅ **Biome-aware upgrade curation** for contextual relevance  
- ✅ **Complete UI integration** with polish and accessibility
- ✅ **Persistent progression** with save/load functionality
- ✅ **Extensible architecture** for custom upgrades and effects
- ✅ **Performance optimization** targeting 60fps operation
- ✅ **Comprehensive testing tools** for validation and debugging

**Ready for production use** - no placeholders, no TODOs, no external dependencies required!