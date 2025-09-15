# 🗺️ TWG-MetVanDamn

## *Procedural MetroidVania World Generation with ECS/DOTS Architecture*
>
> "May your worlds be procedurally perfect, your builds eternally green, and your development chair supremely comfortable."

### 🍑 ✨ Happy Coding! ✨ 🍑

[![Unity 6000.2+](https://img.shields.io/badge/Unity-6000.2+-black.svg?style=flat&logo=unity)](https://unity3d.com/get-unity/download)
[![DOTS 1.2.0](https://img.shields.io/badge/DOTS-1.2.0-blue.svg)](https://docs.unity3d.com/Packages/com.unity.entities@1.2/)
[![Living Dev Agent](https://img.shields.io/badge/Living%20Dev%20Agent-Enabled-green.svg)](docs/TLDL-Archive/)
[![Save The Butts!](https://img.shields.io/badge/Butt%20Safety-Certified-orange.svg)](MANIFESTO.md)

---

## 🎯 **Project Overview**

**TWG-MetVanDamn** is an advanced Unity project that combines **procedural MetroidVania world generation** with **Entity Component System (ECS/DOTS)** architecture to create dynamic, interconnected game worlds. The project features a sophisticated **Living Dev Agent workflow** that integrates comprehensive development tooling, documentation practices, and the sacred "Save The Butts!" philosophy.

### **🌟 What Makes This Special**

- **🗺️ Procedural MetroidVania Generation**: Wave Function Collapse (WFC) algorithms create interconnected districts with proper gating and biome distribution
- **⚡ High-Performance ECS/DOTS**: Unity's Data-Oriented Technology Stack for scalable, performance-optimized game systems
- **🧬 Living Dev Agent Workflow**: Comprehensive development environment with automated documentation, validation, and collaboration tools
- **🍑 Developer Comfort Focus**: Sustainable development practices prioritizing developer well-being and code quality

---

## 🏗️ **Architecture Overview**

### **Core Game Systems**

#### **🗺️ World Generation Engine**

- **District System**: Modular world areas with unique characteristics and connections
- **Wave Function Collapse (WFC)**: Constraint-solving algorithm for coherent world layout
- **Biome Field System**: Dynamic environmental influences across the world
- **Gate Condition System**: MetroidVania-style progression gating with configurable unlock conditions
- **AI Navigation System**: Runtime pathfinding with polarized gate handling for intelligent agent movement

#### **🤖 AI Navigation & Pathfinding**

- **Navigation Graph**: Runtime graph built from districts, connections, and gates
- **Agent Capabilities**: Polarity and ability-based agent configuration system
- **Polarized Gate Handling**: Dual-mode support for hard blocking vs. soft cost-based gating
- **Reachability Validation**: Automated detection of unreachable areas and connectivity issues
- **Editor Visualization**: Interactive navigation graph debugging with cost overlays

#### **🏛️ ECS/DOTS Foundation**

- **Pure ECS Architecture**: All game logic implemented as performant, data-oriented systems
- **Burst-Compiled Systems**: Maximum performance through Unity's Burst compiler
- **Job System Integration**: Efficient multi-threaded processing for complex world generation
- **Authoring Layer**: Scene-based setup tools for designers without requiring code

### **Package Structure**

```
com.tinywalnutgames.metvd.core     # Core components, IDs, math utilities
com.tinywalnutgames.metvd.biome    # Biome field system and polarity rules
com.tinywalnutgames.metvd.graph    # District WFC and sector refinement
com.tinywalnutgames.metvd.authoring # Scene authoring tools and bakers
com.tinywalnutgames.metvd.utility  # Generic utilities and aggregation systems
com.tinywalnutgames.metvd.samples  # Example scenes and configurations
```

---

## 🚀 **Quick Start**

### **Prerequisites**

- **Unity 6000.2+** (Unity 6 with DOTS 1.2.0)
- **Git** with LFS support for large assets
- **Visual Studio** or **JetBrains Rider** (recommended)
- **Python 3.11+** for Living Dev Agent tools

### **Setup Instructions**

#### **1. Clone and Initialize**

```bash
# Clone the repository
git clone https://github.com/jmeyer1980/TWG-MetVanDamn.git
cd TWG-MetVanDamn

# Initialize Living Dev Agent environment
mkdir -p .github/workflows
chmod +x scripts/init_agent_context.sh scripts/clone-and-clean.sh
scripts/init_agent_context.sh
```

#### **2. Unity Setup**

```bash
# Open project in Unity 6000.2+
# Unity will automatically resolve package dependencies
# Import any required 2D packages if working with sprites

# Verify DOTS packages are properly installed:
# - com.unity.entities 1.2.0+
# - com.unity.collections 1.2.4+
# - com.unity.mathematics 1.2.6+
```

#### **3. Development Environment**

```bash
# Install development dependencies
pip install -r scripts/requirements.txt

# Run validation to ensure setup is correct
python3 src/SymbolicLinter/validate_docs.py --tldl-path docs/
python3 src/DebugOverlayValidation/debug_overlay_validator.py --path src/
python3 src/SymbolicLinter/symbolic_linter.py --path src/
```

#### **4. First Time Setup**

1. Open Unity and load the project
2. Navigate to `Assets/Scenes/` for example scenes
3. Check `Assets/MetVanDAMN/Authoring/README.md` for authoring workflow
4. Review `MANIFESTO.md` for project philosophy and development practices
5. Create your first TLDL entry: `scripts/init_agent_context.sh --create-tldl "MyFirstAdventure"`

---

## 🎮 **Game Features**

### **🗺️ Procedural World Generation**

- **Dynamic District Creation**: Each playthrough generates unique interconnected areas
- **Intelligent Gate Placement**: Progression barriers placed according to MetroidVania design principles
- **Biome Consistency**: Environmental themes flow naturally across connected regions
- **Configurable Complexity**: Adjust world size, density, and challenge progression

### **🧬 ECS/DOTS Performance**

- **Scalable Architecture**: Handle thousands of entities without performance degradation
- **Burst-Compiled Systems**: Critical paths optimized for maximum throughput
- **Data-Oriented Design**: Memory layout optimized for cache efficiency
- **Job System Parallelization**: Multi-threaded processing for complex algorithms

### **🛠️ Designer-Friendly Tools**

- **Scene-Based Authoring**: Set up worlds visually without code
- **Real-Time Preview**: See generation results instantly during development
- **Validation Systems**: Catch configuration errors before runtime
- **Debug Overlays**: Visualize generation algorithms in action

---

## 📁 **Project Structure**

```
TWG-MetVanDamn/
├── Assets/
│   ├── MetVanDAMN/                    # Core game systems
│   │   ├── Authoring/                 # Scene authoring tools
│   │   └── Utility/                   # Generic ECS utilities
│   ├── Plugins/TLDA/                  # Living Dev Agent Unity integration
│   │   ├── Editor/                    # TLDL editor tools
│   │   ├── Runtime/                   # Runtime TLDA components
│   │   └── docs/                      # TLDA documentation
│   ├── Scenes/                        # Example and test scenes
│   └── Settings/                      # Unity project settings
├── Packages/                          # Custom packages
│   ├── com.tinywalnutgames.metvd.core/      # Core systems
│   ├── com.tinywalnutgames.metvd.biome/     # Biome management
│   ├── com.tinywalnutgames.metvd.graph/     # Graph generation
│   ├── com.tinywalnutgames.metvd.authoring/ # Authoring tools
│   ├── com.tinywalnutgames.metvd.utility/   # Utility systems
│   └── com.tinywalnutgames.metvd.samples/   # Example content
├── scripts/                           # Development automation
│   ├── init_agent_context.sh         # Environment initialization
│   ├── clone-and-clean.sh           # Template setup
│   └── cid-faculty/                  # CI/CD automation
├── src/                              # Validation and linting tools
├── docs/                             # Evergreen documentation
├── TLDL/                             # Living Dev Log entries
├── MANIFESTO.md                      # Project philosophy
├── CONTRIBUTING.md                   # Contribution guidelines
└── CHANGELOG.md                      # Release history
```

---

## 🧪 **Development Workflow**

### **🍑 Save The Butts! Philosophy**

This project follows the **Save The Butts!** manifesto, prioritizing:

- **Developer Comfort**: Ergonomic practices and sustainable development
- **Documentation Excellence**: Comprehensive TLDL (Living Dev Log) entries
- **Collaborative Spirit**: Mentoring, knowledge sharing, and inclusive practices
- **Quality Focus**: Automated validation, testing, and continuous improvement

### **📝 Living Dev Log (TLDL) Process**

```bash
# Create new development entry
scripts/init_agent_context.sh --create-tldl "FeatureName"

# Document discoveries, decisions, and learnings
# Follow template in docs/tldl_template.yaml

# Validate documentation integrity
python3 src/SymbolicLinter/validate_docs.py --tldl-path docs/
```

### **🔧 Development Commands**

```bash
# Environment initialization
scripts/init_agent_context.sh                    # Setup and validate environment
scripts/init_agent_context.sh --create-tldl "Title"  # Create new TLDL entry

# Quality assurance
python3 src/SymbolicLinter/validate_docs.py --tldl-path docs/    # ~60ms
python3 src/DebugOverlayValidation/debug_overlay_validator.py    # ~56ms
python3 src/SymbolicLinter/symbolic_linter.py --path src/        # ~68ms

# Project maintenance
scripts/directory_lint.py                        # Validate project structure
python3 scripts/cid-faculty/overlord-sentinel.js # Security validation
```

### Biome Art Profile Library (Per-Type Buckets)

- Use `Assets/MetVanDAMN/Authoring/BiomeArtProfileLibrary` to define available `BiomeArtProfile` assets.
- Populate a global `profiles` list and optional `perTypeBuckets` for specific `BiomeType` values.
- Runtime selection order in `BiomeArtAutoAssignmentSystem`:
    1) Prefer matching `perTypeBuckets[type].profiles` when present and non-empty
    2) Fallback to the global `profiles` list
- Selection is deterministic per biome entity: seeded by world seed, biome type, and `NodeId`.
- Editor tools (`Biome Color Legend`, `Sector Room Hierarchy`) use the same preference order for names/colors.

---

## 🎯 **Technical Deep Dive**

### **Wave Function Collapse Implementation**

The world generation system uses a constraint-solving approach where:

1. **District Templates**: Pre-defined room types with connection rules
2. **Constraint Propagation**: Ensures valid connections between adjacent areas
3. **Backtracking Algorithm**: Resolves conflicts through systematic exploration
4. **Biome Coherence**: Environmental rules guide theme distribution

### **ECS System Architecture**

```csharp
// Example: Biome field processing
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct BiomeFieldSystem : ISystem
{
    // High-performance biome influence calculation
    // Processes thousands of entities per frame
}
```

### **Performance Characteristics**

- **World Generation**: ~200ms for medium complexity worlds
- **Biome Processing**: 60fps with 10,000+ entities
- **Memory Usage**: <100MB for typical game worlds
- **Build Time**: ~30s for incremental builds

---

## 🤝 **Contributing**

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for detailed information about:

- **Development Workflow**: TLDL documentation, code review process
- **Code Standards**: ECS best practices, naming conventions
- **Issue Reporting**: Bug reports, feature requests
- **Community Guidelines**: Inclusive, respectful collaboration

### **Quick Contribution Checklist**

- [ ] Read the [MANIFESTO.md](MANIFESTO.md) and embrace the **Save The Butts!** philosophy
- [ ] Create TLDL entries documenting your work
- [ ] Follow ECS/DOTS best practices
- [ ] Add tests for new functionality
- [ ] Update documentation as needed
- [ ] Run validation tools before submitting PRs

---

## 📚 **Documentation & Resources**

### **Essential Documentation**

- **[MANIFESTO.md](MANIFESTO.md)**: Project philosophy and "Save The Butts!" doctrine
- **[CONTRIBUTING.md](CONTRIBUTING.md)**: Comprehensive contribution guidelines
- **[CHANGELOG.md](CHANGELOG.md)**: Release history and feature evolution
- **[Assets/Plugins/TLDA/docs/Copilot-Setup.md](Assets/Plugins/TLDA/docs/Copilot-Setup.md)**: AI collaboration setup

### **Living Dev Log Archives**

- **[TLDL/entries/](TLDL/entries/)**: Development chronicles and technical discoveries
- **[docs/](docs/)**: Evergreen documentation and templates

### **External Resources**

- **[Unity DOTS Documentation](https://docs.unity3d.com/Packages/com.unity.entities@1.2/)**: Official ECS/DOTS guides
- **[Wave Function Collapse](https://github.com/mxgmn/WaveFunctionCollapse)**: Algorithm reference implementation
- **[MetroidVania Design Patterns](https://www.gamedeveloper.com/design/the-anatomy-of-a-metroidvania-map)**: Genre design principles

---

## 🏆 **Achievements & Milestones**

- **🛡️ Buttsafe Certified™**: Comprehensive developer comfort and code quality standards
- **⚡ DOTS Optimized**: High-performance ECS architecture with Burst compilation
- **🧬 Living Documentation**: Self-documenting codebase with comprehensive TLDL integration
- **🤖 AI Collaboration Ready**: GitHub Copilot integration with ping-and-fix workflows

---

## 📄 **License & Attribution**

This project is licensed under the **GNU General Public License v3.0**. See [LICENSE](LICENSE) for details.

**Created by**: Tiny Walnut Games Development Team
**Template System**: Living Dev Agent workflow
**Philosophy**: Save The Butts! Manifesto
**Powered by**: Unity DOTS, ECS architecture, and developer comfort principles

---

## 🌟 **Acknowledgments**

Special thanks to:

- **The Living Dev Agent Community**: For innovative development workflows
- **Unity DOTS Team**: For powerful ECS framework and performance tools
- **MetroidVania Designers**: For establishing genre conventions we lovingly subvert
- **Every Developer**: Who has suffered through uncomfortable chairs and poor documentation

---

*"May your worlds be procedurally perfect, your builds eternally green, and your development chair supremely comfortable."*

**🍑 ✨ Happy Coding! ✨ 🍑**
