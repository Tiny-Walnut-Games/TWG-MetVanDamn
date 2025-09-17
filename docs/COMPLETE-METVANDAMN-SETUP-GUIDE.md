# 🗺️ The Complete MetVanDAMN Setup & Experience Guide
## *From Fresh Clone to "Holy Cosmic Cheeks, This Actually Works!" in 15 Minutes*

> **"In the realm of procedural MetroidVania generation, there are no accidents - only adventures waiting to unfold."**
> — The Sacred Scrolls of MetVanDAMN, Chapter 3, Verse 42

---

## 🎯 **Mission: Complete Success**

**Goal**: Take you from repository clone to successfully experiencing procedural MetVanDAMN world generation with visual feedback and complete understanding.

**Success Criteria**:
- ✅ Unity opens project without errors
- ✅ You can press Play and see immediate world generation
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
```bash
# Install Python dependencies for validation tools
pip install -r scripts/requirements.txt

# Note: Network timeouts are acceptable - core dependencies are typically pre-installed
```

---

## 🏗️ **Phase 2: Unity Project Setup (5 minutes)**

### **Step 1: Open in Unity**
1. **Launch Unity Hub**
2. **Click "Add"** and navigate to your cloned `TWG-MetVanDamn` folder
3. **Select Unity 6000.2.0f1** (or latest Unity 6.x version)
4. **Click "Open"**

Unity will automatically:
- Import all packages and dependencies
- Compile scripts (may take 2-3 minutes on first import)
- Initialize DOTS systems

### **Step 2: Verify DOTS Packages**
Open **Window > Package Manager** and confirm these packages are installed:
- ✅ **Entities** (1.2.0+)
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
2. **Save As**: `Assets/Scenes/MyMetVanDAMN.unity`
3. **Add GameObject** and name it "MetVanDAMN_Setup"
4. **Add Component**: `SmokeTestSceneSetup` (from TinyWalnutGames.MetVD.Samples)

#### **Configure World Parameters**
```yaml
World Seed: 42                    # Reproducible worlds
World Size: (50, 50)             # Reasonable for testing
Target Sector Count: 5           # Number of districts
Biome Transition Radius: 10.0    # Polarity field influence
Enable Debug Visualization: ✅   # See world bounds
Log Generation Steps: ✅         # Track progress
```

#### **Test Generation**
1. **Press Play** ▶️
2. **Inspector shows**: Runtime regeneration controls
3. **Scene View shows**: Green wireframe world bounds

---

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
```bash
# Validate documentation structure (~60ms)
python3 src/SymbolicLinter/validate_docs.py --tldl-path docs/

# Validate debug overlay system (~56ms)
python3 src/DebugOverlayValidation/debug_overlay_validator.py --path src/

# Run symbolic linting (~68ms - may show expected parse errors)
python3 src/SymbolicLinter/symbolic_linter.py --path src/
```

### **Expected Results**
- **✅ TLDL validation**: PASS (potential warnings about entry ID format)
- **⚠️ Debug overlay validation**: 85.7% health score (C# parsing issues are expected)
- **⚠️ Symbolic linting**: Parse errors for Python files (expected behavior)

### **Manual Verification Checklist**
- [ ] Unity console shows successful world generation
- [ ] Scene view displays green debug bounds
- [ ] Entity Debugger shows hub + district entities
- [ ] Inspector regeneration buttons work during Play mode
- [ ] Different seeds produce different debug visualizations
- [ ] No compilation errors in Console

---

## ⚔️ **Troubleshooting Common Issues**

### **"No Entities Created"**
**Symptoms**: Console shows generation logs but Entity Debugger is empty
**Solutions**:
1. Verify `SmokeTestSceneSetup` component is **active and enabled**
2. Check **Window > Entities > Entity Debugger** is set to correct world
3. Ensure **World.DefaultGameObjectInjectionWorld** exists

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
**Symptoms**: Red error messages in Console
**Solutions**:
1. **Restart Unity** (clears cached compilation issues)
2. **Reimport All**: Assets > Reimport All
3. **Verify DOTS packages** are properly installed

### **"Performance Issues"**
**Symptoms**: Gizmos redraw constantly, inspector lags
**Solutions**:
1. Our **performance fix** reduces redraw frequency to 0.5s intervals
2. **Disable Debug Visualization** when not needed
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

---

## 📚 **Next Steps & Advanced Usage**

### **Expand Your MetVanDAMN Knowledge**
1. **Study the Code**: Explore `Packages/com.tinywalnutgames.metvd.*` directories
2. **Read TLDL Entries**: Check `docs/` for development chronicles
3. **Examine Test Cases**: Look at `Assets/MetVanDAMN/Authoring/Tests/`
4. **Join the Community**: Contribute to Living Dev Agent workflow

### **Create Your First Custom World**
1. **Modify Parameters**: Try different seeds, sizes, and sector counts
2. **Add Biome Fields**: Create custom polarity field configurations
3. **Extend Room Types**: Add new room generation patterns
4. **Implement Features**: Build on the MetVanDAMN foundation

### **Living Dev Agent Workflow**
```bash
# Create development entries for your discoveries
scripts/init_agent_context.sh --create-tldl "MyMetVanDAMNExperiments"

# Document your journey for future adventurers
# Follow the TLDL template in docs/tldl_template.yaml
```

---

## 🎯 **Phase 5: Procedural Leveling Perk System (5 minutes)**

### **The Complete Player Progression Experience**

MetVanDAMN includes a **fully functional procedural leveling perk system** that provides seed-based upgrade choices, biome-aware curation, and complete save/load persistence.

### **Quick Start: Complete Demo Scene**

#### **Generate Demo Scene with Upgrade System**
1. **Menu**: `Tools > MetVanDAMN > Create Base DEMO Scene > Complete 2D Platformer Demo`
2. **Wait** for scene creation (includes all systems)
3. **Scene opens**: `Assets/Scenes/MetVanDAMN_Complete2DPlatformer.unity`

#### **Experience the Upgrade System**
1. **Press Play** ▶️ to enter the demo
2. **Test Controls**:
   - **F1**: Gain 50 XP (debug)
   - **F2**: Force level up (debug)  
   - **F3**: Force show upgrade choices (debug)
   - **F4**: Reset progression (debug)

3. **Level Up Flow**:
   - Gain XP → Level Up → **Modal UI appears** with 3-4 upgrade choices
   - **Click upgrade** → Effects apply → Game resumes with new abilities
   - **Repeat** to experience procedural choice generation

### **Manual Player Setup**

#### **Add to Existing Player GameObject**
```csharp
// Option 1: Automatic setup (recommended)
var setup = player.AddComponent<CompletePlayerSetup>();
setup.SetupPlayer(); // Call in Start() or manually

// Option 2: Individual components
var progression = player.AddComponent<PlayerLevelProgression>();
var choiceSystem = player.AddComponent<LevelUpChoiceSystem>();
var effectApplicator = player.AddComponent<UpgradeEffectApplicator>();
```

#### **Configure Upgrade Database**
1. **Create GameObject**: "UpgradeDatabaseManager" 
2. **Add Component**: `UpgradeDatabaseManager`
3. **Auto-Find Collections**: Enabled (finds upgrade assets automatically)

### **Understanding the System**

#### **Sample Upgrades Included**
- **Movement**: Double Jump, Wall Jump, Speed Boost
- **Offense**: Damage Boost, Charge Attack
- **Defense**: Health Boost, Health Regeneration  
- **Utility**: Auto Loot, Inventory Expansion
- **Special**: *More coming in your custom collections*

#### **Choice Generation Features**
- **Seed-Based**: Same world seed = same upgrade choices at same level
- **Biome-Aware**: Current biome influences upgrade weights
- **Category-Balanced**: Always includes 2+ different categories
- **Player-Filtered**: Only shows viable upgrades for current build
- **Duplicate Prevention**: Configurable uniqueness rules

#### **UI Experience**
- **Rich Display**: Name, description, category icon, stat preview
- **Hover Effects**: Visual feedback and audio cues
- **Game Pause**: Time stops during choice selection
- **Immediate Application**: Effects visible instantly after selection

### **Validation Steps**

#### **Test Upgrade System**
1. **Add Test Runner**: Add `UpgradeSystemTestRunner` to any GameObject
2. **Run Tests**: Press **F5** or Context Menu > "Run All Tests"
3. **Check Results**: Console shows 5/5 tests passed ✅

#### **Manual Verification**
- [ ] Can gain XP (F1 debug key)
- [ ] Level up triggers choice UI (F2 debug key)
- [ ] Can select upgrades from modal
- [ ] Effects apply correctly (check stats/abilities)
- [ ] Progression saves between sessions
- [ ] Different seeds produce different choices

### **Customization Quick Start**

#### **Create Custom Upgrades**
1. **Assets > Create > MetVanDAMN > Upgrade Definition**
2. **Configure**: Name, description, category, effects, requirements
3. **Add to Collection**: Drag to appropriate UpgradeCollection asset

#### **Create New Collections**
1. **Assets > Create > MetVanDAMN > Upgrade Collection** 
2. **Set Category**: Movement/Offense/Defense/Utility/Special
3. **Add Upgrades**: Reference your UpgradeDefinition assets
4. **Configure Biome Weights**: Influence based on world context

#### **Integration Points**
```csharp
// Trigger XP gain from your systems
var progression = player.GetComponent<PlayerLevelProgression>();
progression.GainXP(50); // From combat, exploration, quests, etc.

// Check player abilities for gating content
if ((progression.CurrentAbilities & Ability.DoubleJump) != 0)
{
    // Player has double jump - unlock high platforms
}

// Query current stats for dynamic content
var effectApplicator = player.GetComponent<UpgradeEffectApplicator>();
float currentSpeed = effectApplicator.GetCurrentStat("runspeed");
```

### **Performance & Quality**

- **Target**: 60fps sustained operation ✅
- **Choice Generation**: < 1ms per selection ✅
- **UI Creation**: < 10ms for modal display ✅
- **Save/Load**: < 5ms for progression data ✅
- **Memory Usage**: Minimal ScriptableObject references ✅

### **Complete Documentation**

**📖 Detailed Guide**: `Assets/MetVanDAMN/Docs/PROCEDURAL_LEVELING_PERK_SYSTEM_GUIDE.md`

Covers:
- Complete API reference
- Advanced configuration
- Extension examples
- Performance optimization
- Troubleshooting guide

---

## 🏆 **Achievement Unlocked: MetVanDAMN Master**

**🎉 Congratulations, fellow Buttguard!** You've successfully:

- ✅ **Set up MetVanDAMN** from fresh repository clone
- ✅ **Experienced procedural generation** with immediate visual feedback
- ✅ **Understood the architecture** and customization options
- ✅ **Mastered troubleshooting** common setup issues
- ✅ **Validated your installation** with comprehensive testing

**You are now ready to:**
- 🌍 **Generate infinite procedural worlds** with confidence
- 🛠️ **Customize and extend** the MetVanDAMN system
- 🧙‍♂️ **Contribute to the community** through TLDL documentation
- 🍑 **Maintain butt comfort** while coding awesome features

---

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

**Version**: 1.0 - Complete Setup Guide
**Created**: January 2025
**Author**: Living Dev Agent Community
**Sacred Commitment**: Save The Butts! Manifesto Compliance
**Purpose**: Eliminate setup frustration and enable immediate MetVanDAMN success

*This guide is living documentation - it evolves with community feedback and discoveries.*
