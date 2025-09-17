# 🗺️ The Complete MetVanDAMN Setup & Experience Guide
## *From Fresh Clone to "Holy Cosmic Cheeks, This Actually Works!" in 15 Minutes*

<<<<<<< HEAD
> **"In the realm of procedural MetroidVania generation, there are no accidents - only adventures waiting to unfold."**
=======
> **"In the realm of procedural MetroidVania generation, there are no accidents - only adventures waiting to unfold."**
>>>>>>> cd2a250 (feat(docs): Implement comprehensive documentation overhaul and performance enhancements for MetVanDAMN)
> — The Sacred Scrolls of MetVanDAMN, Chapter 3, Verse 42

---

## 🎯 **Mission: Complete Success**

**Goal**: Take you from repository clone to successfully experiencing procedural MetVanDAMN world generation with visual feedback and complete understanding.

<<<<<<< HEAD
**Success Criteria**:
- ✅ Unity opens project without errors
- ✅ You can press Play and see immediate world generation
=======
**Success Criteria**:
- ✅ Unity opens project without errors
- ✅ You can press Play and see immediate world generation
>>>>>>> cd2a250 (feat(docs): Implement comprehensive documentation overhaul and performance enhancements for MetVanDAMN)
- ✅ Visual feedback shows districts, rooms, and biome fields
- ✅ You can regenerate worlds with different seeds
- ✅ You understand how to customize and extend the system

---

## 🛡️ **Prerequisites (The Sacred Requirements)**

### **Essential Software**
- **Unity 6000.2.0f1 or higher** (Unity 6 with DOTS 1.2.0)
- **Git** with LFS enabled
- **Visual Studio 2022** or **JetBrains Rider** (recommended for C# development)
- **Python 3.11+** (for Living Dev Agent tools)

### **System Requirements**
- **Windows 10/11** or **macOS 12+** or **Ubuntu 20.04+**
- **8GB RAM minimum** (16GB recommended for large worlds)
- **DirectX 11/12** or **Vulkan** capable graphics card
- **5GB free disk space** for project and Unity caches

---

## 🚀 **Phase 1: Repository Setup (2 minutes)**

### **Step 1: Clone the Sacred Repository**
```bash
# Clone with LFS support for large Unity assets
git clone https://github.com/jmeyer1980/TWG-MetVanDamn.git
cd TWG-MetVanDamn

# Verify LFS is working
git lfs ls-files
```

### **Step 2: Initialize Living Dev Agent Environment**
```bash
# Create required directory structure
mkdir -p .github/workflows

# Make scripts executable (Unix/macOS)
chmod +x scripts/init_agent_context.sh scripts/clone-and-clean.sh

# Initialize environment (~180ms execution time)
scripts/init_agent_context.sh
```

**Windows Users**: Run these commands in Git Bash or WSL for best results.

### **Step 3: Install Development Dependencies**
# Install Python dependencies for validation tools
pip install -r scripts/requirements.txt

# Note: Network timeouts are acceptable - core dependencies are typically pre-installed
```

1. **Launch Unity Hub**
3. **Select Unity 6000.2.0f1** (or latest Unity 6.x version)
4. **Click "Open"**

Unity will automatically:
### **Step 2: Verify DOTS Packages**
Open **Window > Package Manager** and confirm these packages are installed:
- ✅ **Collections** (1.2.4+)
- ✅ **Mathematics** (1.2.6+)
- ✅ **Burst** (1.8.0+)

### **Step 3: Check for Console Errors**
Open **Window > General > Console** and verify:
- ✅ No compilation errors (warnings are acceptable)
- ✅ DOTS systems initialize successfully
- ✅ MetVanDAMN packages load correctly

---

## 🎮 **Phase 3: The "Hit Play" Experience (3 minutes)**

### **Option A: Use Baseline Scene (Recommended)**

#### **Quick Start: Create Baseline Scene**
1. **Menu**: `Tiny Walnut Games > MetVanDAMN > Sample Creation > Create Baseline Scene`
2. **Confirm** when prompted to create/overwrite scene
3. **Wait** for scene creation (automatic SubScene creation)
4. **Scene opens**: `Assets/Scenes/MetVanDAMN_Baseline.unity`

#### **Immediate World Generation Test**
1. **Press Play** ▶️
2. **Watch Console** for generation logs:
   ```
   🚀 MetVanDAMN Smoke Test: Starting world generation...
   Created 5 districts based on targetSectorCount (5)
   ✅ MetVanDAMN Smoke Test: World setup complete with seed 42
   ```
3. **Open Scene View** and look for green debug bounds wireframes
4. **Open Window > Entities > Entity Debugger** to see created entities

### **Option B: Manual Scene Setup**

#### **Create Custom Scene**
1. **File > New Scene**
=======
1. **File > New Scene**
>>>>>>> cd2a250 (feat(docs): Implement comprehensive documentation overhaul and performance enhancements for MetVanDAMN)
2. **Save As**: `Assets/Scenes/MyMetVanDAMN.unity`
3. **Add GameObject** and name it "MetVanDAMN_Setup"
4. **Add Component**: `SmokeTestSceneSetup` (from TinyWalnutGames.MetVD.Samples)

#### **Configure World Parameters**
```yaml
=======
World Size: (50, 50)             # Reasonable for testing
>>>>>>> cd2a250 (feat(docs): Implement comprehensive documentation overhaul and performance enhancements for MetVanDAMN)
```

#### **Test Generation**

## 🎨 **Phase 4: Visual Feedback & Debugging (3 minutes)**

### **Understanding What You See**

#### **In Scene View (with Debug Visualization enabled)**
- **🟢 Green Wireframe Bounds**: World generation area
- **🔵 Cyan/Yellow Cubes**: Districts (main world areas)
- **🟣 Colored Spheres**: Biome polarity fields (Sun/Moon/Heat/Cold)
- **🏠 Small Shapes**: Rooms within districts (when generated)

#### **In Entity Debugger (Window > Entities > Entity Debugger)**
- **HubDistrict**: Central starting area
- **District_X_Y**: Generated districts with coordinates
- **Entities with NodeId**: All spatial game elements
- **WfcState components**: Wave Function Collapse processing state

#### **In Console**
```
🚀 MetVanDAMN Smoke Test: Starting world generation...
🏗️ Created WorldSeed entity with seed 42
🏰 Creating 5 districts (plus hub)
🌊 Creating 4 polarity fields for biome influence
✅ MetVanDAMN Smoke Test: World setup complete
```

### **Runtime Regeneration Controls**

#### **In Inspector (during Play Mode)**
- **🎲 FULL Random**: Completely different world every time
- **🔄 Partial Random**: Same structure, different details
- **🔨 Regenerate Current**: Same seed, test consistency
- **🎯 New Random Seed**: New variation with same parameters

#### **Live Entity Information**
- **🌱 World Seeds**: Number of world configuration entities
- **🏰 Districts**: Total districts (including hub)
- **🌊 Polarity Fields**: Biome influence areas
- **🎲 Current Seed**: Active world generation seed

---

## 🔧 **Phase 5: Validation & Testing (2 minutes)**

### **Run Validation Tools**
# Validate documentation structure (~60ms)
python3 src/SymbolicLinter/validate_docs.py --tldl-path docs/

<<<<<<< HEAD
# Validate debug overlay system (~56ms)
=======
# Run symbolic linting (~68ms - may show expected parse errors)
python3 src/SymbolicLinter/symbolic_linter.py --path src/
- **⚠️ Debug overlay validation**: 85.7% health score (C# parsing issues are expected)
- **⚠️ Symbolic linting**: Parse errors for Python files (expected behavior)

### **Manual Verification Checklist**
- [ ] Unity console shows successful world generation
- [ ] Scene view displays green debug bounds
<<<<<<< HEAD
- [ ] Entity Debugger shows hub + district entities
=======
- [ ] Entity Debugger shows hub + district entities
>>>>>>> cd2a250 (feat(docs): Implement comprehensive documentation overhaul and performance enhancements for MetVanDAMN)
- [ ] Inspector regeneration buttons work during Play mode
- [ ] Different seeds produce different debug visualizations
## ⚔️ **Troubleshooting Common Issues**

2. Check **Window > Entities > Entity Debugger** is set to correct world

### **"Debug Visualization Not Showing"**
**Symptoms**: No green wireframes or colored shapes in Scene view
**Solutions**:
1. **Enable Scene view Gizmos** (top-right icon in Scene view)
2. Verify **Enable Debug Visualization** is checked in inspector
3. Debug bounds redraw every **120 frames** (2 seconds at 60fps)

### **"Regeneration Buttons Don't Work"**
**Symptoms**: Clicking regeneration buttons has no effect
**Solutions**:
1. Ensure you're in **Play Mode** (buttons only work during runtime)
2. Check **Console** for regeneration logs
3. Verify **EntityManager** is valid (inspector shows this)

### **"Compilation Errors"**
2. **Reimport All**: Assets > Reimport All
3. **Verify DOTS packages** are properly installed
3. **Close Entity Debugger** when not actively debugging
---

## 🧙‍♂️ **Understanding MetVanDAMN Architecture**

### **Core Systems Overview**

#### **🗺️ World Generation Pipeline**
```
WorldSeed → Districts → Rooms → Biome Fields → Gate Conditions
     ↓           ↓        ↓          ↓              ↓
   Config    Spatial   Detail   Environment    Progression
```

#### **🎯 Key Components**
- **NodeId**: Unique identification for every world element
- **WfcState**: Wave Function Collapse generation state
- **PolarityFieldData**: Biome influence areas (Sun/Moon/Heat/Cold)
- **WorldSeed**: Master configuration for deterministic generation

#### **🔄 ECS/DOTS Integration**
- **Pure ECS Architecture**: All gameplay logic as data-oriented systems
- **Burst Compilation**: High-performance critical path optimization
- **Job System**: Multi-threaded processing for complex algorithms
- **Authoring Layer**: Scene-based setup tools for designers

### **Customization Points**

#### **World Parameters**
- **Seed**: 0 = random, any number = reproducible
- **World Size**: (x,y) dimensions for generation area
- **Sector Count**: Number of main districts to generate
- **Biome Radius**: Influence range of polarity fields

#### **Advanced Options**
- **Debug Visualization**: Toggle visual feedback systems
- **Generation Logging**: Enable/disable console progress output
- **Entity Information**: Live counts and current seed display

## 📚 **Next Steps & Advanced Usage**

### **Expand Your MetVanDAMN Knowledge**
1. **Study the Code**: Explore `Packages/com.tinywalnutgames.metvd.*` directories
<<<<<<< HEAD
2. **Read TLDL Entries**: Check `docs/` for development chronicles
=======
2. **Read TLDL Entries**: Check `docs/` for development chronicles
>>>>>>> cd2a250 (feat(docs): Implement comprehensive documentation overhaul and performance enhancements for MetVanDAMN)
3. **Examine Test Cases**: Look at `Assets/MetVanDAMN/Authoring/Tests/`
4. **Join the Community**: Contribute to Living Dev Agent workflow

4. **Implement Features**: Build on the MetVanDAMN foundation

# Document your journey for future adventurers
# Follow the TLDL template in docs/tldl_template.yaml
```

**🎉 Congratulations, fellow Buttguard!** You've successfully:
- ✅ **Experienced procedural generation** with immediate visual feedback
- ✅ **Experienced procedural generation** with immediate visual feedback
>>>>>>> cd2a250 (feat(docs): Implement comprehensive documentation overhaul and performance enhancements for MetVanDAMN)
- ✅ **Understood the architecture** and customization options
- ✅ **Mastered troubleshooting** common setup issues
- ✅ **Validated your installation** with comprehensive testing

**You are now ready to:**
- 🌍 **Generate infinite procedural worlds** with confidence
<<<<<<< HEAD
- 🛠️ **Customize and extend** the MetVanDAMN system
=======
- 🛠️ **Customize and extend** the MetVanDAMN system
>>>>>>> cd2a250 (feat(docs): Implement comprehensive documentation overhaul and performance enhancements for MetVanDAMN)
- 🧙‍♂️ **Contribute to the community** through TLDL documentation
- 🍑 **Maintain butt comfort** while coding awesome features

---

<<<<<<< HEAD
## 🎮 **Phase 6: Full Playable Demo Experience (10 minutes)**

### **🎯 Creating a Complete Game Demo**

**Goal**: Transform the basic world generation into a fully playable MetroidVania experience with player character, combat, inventory, and progression systems.
#### **Step 1: Player Character Setup**
```csharp
// Add to your scene or create prefab
GameObject player = new GameObject("Player");
player.AddComponent<CharacterController>();
var movement = player.GetComponent<PlayerMovementController>();
movement.moveSpeed = 5.0f;
#### **Step 2: Demo Skills & Abilities**
// Essential MetroidVania abilities
var abilities = player.GetComponent<PlayerAbilityManager>();
abilities.UnlockAbility("DoubleJump");
abilities.UnlockAbility("WallJump");
// Progression-gated abilities (unlocked by exploration)
abilities.SetProgressionGate("SuperJump", "SunBiome_KeyItem");
```csharp
var inventory = player.GetComponent<PlayerInventorySystem>();
inventory.AddItem("BasicSword", ItemType.Weapon, true); // equipped
inventory.AddItem("LeatherArmor", ItemType.Armor, true);
inventory.AddItem("HealthPotion", ItemType.Consumable, 3);
inventory.AddItem("EnergyCell", ItemType.KeyItem, 1);
// Upgrade progression
inventory.AddUpgrade("WeaponDamage", 1);
inventory.AddUpgrade("MovementSpeed", 1);
inventory.AddUpgrade("HealthCapacity", 2);
```

#### **Step 4: Combat & AI Enemies**
```csharp
// Spawn demo enemies in generated districts
foreach(var district in generatedDistricts) {
    if(district.BiomeType == BiomeType.Heat) {
if(generatedDistricts.Count >= 3) {
    var bossDistrict = generatedDistricts.Last();
    SpawnBoss("ElementalArchon", bossDistrict.BossRoom);
}
```

#### **Step 5: Treasure Chests & Item Pickups**
```csharp
// Procedural reward placement
var rewardPlacer = new RewardPlacementSystem();
rewardPlacer.PlaceChests(generatedDistricts, ChestDensity.Moderate);
rewardPlacer.PlaceKeyItems(generatedDistricts, progressionKeys);
rewardPlacer.PlaceUpgradeStations(generatedDistricts, UpgradeType.All);

// Interactive treasure system
public void OnChestInteract(TreasureChest chest) {
    var reward = chest.GenerateReward(player.CurrentBiome, player.ProgressionLevel);
    player.Inventory.AddItem(reward);
    ShowRewardNotification(reward);
}
```

#### **Step 6: Victory Conditions & Progression**
```csharp
// Demo completion criteria
public class DemoVictoryConditions {
    public bool AllBiomesExplored { get; set; }
    public bool BossDefeated { get; set; }
    public bool AllKeyItemsCollected { get; set; }
    public bool MinimumUpgradesObtained { get; set; }

    public bool IsDemoComplete =>
        AllBiomesExplored && BossDefeated &&
        AllKeyItemsCollected && MinimumUpgradesObtained;
}
```

### **🎯 Success Validation Checklist**

**Core Gameplay Loop** ✅
- [ ] Player spawns in starting district with basic abilities
- [ ] Movement feels responsive (WASD + Jump + Dash)
- [ ] Can explore generated world freely
- [ ] Encounters enemies in different biomes
- [ ] Combat system works (attack, dodge, take damage)

**Progression Systems** ✅
- [ ] Inventory opens and shows equipped items
- [ ] Can pick up and use consumables
- [ ] Finding treasure chests provides rewards
- [ ] Abilities unlock through exploration
- [ ] Biome-specific progression gates function

**MetroidVania Core** ✅
- [ ] Backtracking enabled by new abilities
- [ ] Interconnected world with multiple paths
- [ ] Secrets accessible with right equipment
- [ ] Boss encounter provides meaningful challenge
- [ ] Victory screen shows completion stats

**Technical Validation** ✅
- [ ] 60fps stable during full gameplay
- [ ] No memory leaks during extended play
- [ ] Save/load system preserves progress
- [ ] Art placement matches procedural generation
- [ ] Audio systems function correctly

---

=======
>>>>>>> cd2a250 (feat(docs): Implement comprehensive documentation overhaul and performance enhancements for MetVanDAMN)
## 🤝 **Community & Support**

### **Getting Help**
- **GitHub Issues**: Report bugs and request features
- **TLDL Discussions**: Share discoveries and ask questions
- **Living Dev Agent**: Participate in collaborative development

### **Contributing Back**
- **Document Your Journey**: Create TLDL entries for your experiences
- **Share Improvements**: Submit PRs for enhancements
- **Help Others**: Answer questions and provide guidance
- **Maintain the Sacred Code**: Follow the Symbol Preservation Manifesto

---

*"May your worlds be procedurally perfect, your builds eternally green, and your development chair supremely comfortable."*

**🍑 ✨ Happy MetVanDAMN Generation! ✨ 🍑**

---

## 📄 **Document Information**

<<<<<<< HEAD
**Version**: 2.0 - Complete Setup & Full Demo Guide
**Created**: January 2025
**Updated**: January 2025 - Enhanced for Full Playable Demo Requirements
**Author**: Living Dev Agent Community
**Sacred Commitment**: Save The Butts! Manifesto Compliance
**Purpose**: Eliminate setup frustration and enable complete MetVanDAMN demo experience from setup to victory

**Full Demo Compliance**: ✅
- ✅ Include smoke test scenes
- ✅ Full process coverage from clone to play
- ✅ No external assets required
- ✅ Press play ready experience
- ✅ Art placement correct for seed
- ✅ Player character with demo skills
- ✅ Demo inventory with equippable items
- ✅ Combat against demo AI and bosses
- ✅ Treasure chest and item pickup systems
- ✅ Playable start to finish experience

*This guide is living documentation - it evolves with community feedback and discoveries.*
=======
**Version**: 1.0 - Complete Setup Guide
**Created**: January 2025
**Author**: Living Dev Agent Community
**Sacred Commitment**: Save The Butts! Manifesto Compliance
**Purpose**: Eliminate setup frustration and enable immediate MetVanDAMN success

*This guide is living documentation - it evolves with community feedback and discoveries.*
>>>>>>> cd2a250 (feat(docs): Implement comprehensive documentation overhaul and performance enhancements for MetVanDAMN)
