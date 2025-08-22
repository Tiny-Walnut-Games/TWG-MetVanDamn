# 🍑 Living Dev Agent Template - Save The Butts! Edition

[![Buttsafe Certified](https://img.shields.io/badge/Buttsafe-Certified-gold?style=for-the-badge&logo=shield&logoColor=white)](MANIFESTO.md)
[![Living Dev Log](https://img.shields.io/badge/TLDL-Enabled-blue?style=for-the-badge&logo=book&logoColor=white)](docs/tldl_template.yaml)
[![Chronicle Keeper](https://img.shields.io/badge/Chronicle-Keeper-purple?style=for-the-badge&logo=scroll&logoColor=white)](.github/workflows/chronicle-keeper.yml)
[![Overlord Sentinel](https://img.shields.io/badge/Overlord-Sentinel-red?style=for-the-badge&logo=security&logoColor=white)](.github/workflows/overlord-sentinel-security.yml)
[![Cheekdom Approved](https://img.shields.io/badge/Cheekdom-Approved-green?style=for-the-badge&logo=star&logoColor=white)](docs/game-design-document.md)
[![Ergonomic Protocol](https://img.shields.io/badge/Ergonomic-Protocol-purple?style=for-the-badge&logo=heart&logoColor=white)](scripts/initMyButt.sh)

> *"In a world where butts are constantly under siege by runtime errors, merge conflicts, and poorly documented APIs, only the Buttwarden stands between civilization and total cheek-based catastrophe."*
> 
> — The Sacred Scrolls of the Cheekdom

Use this template to create repositories with our proven "Living Dev Agent" workflow that integrates TLDL (Living Dev Log), DevTimeTravel context capture, and comprehensive development tooling — now enhanced with the sacred **Save The Butts!** philosophy for sustainable, comfortable, and joyful development practices.

---

## 🗺️ **NAVIGATION MAP**

### 📚 **Documentation Hub** → [docs/](docs/)
Your central knowledge repository with guides, architecture, and playbooks:

- **[Setup Guide](docs/Copilot-Setup.md)** - Get started with GitHub Copilot integration
- **[Monthly TLDL Archives](docs/TLDL-Monthly/)** - Consolidated development chronicles  
- **[Playbooks](docs/playbooks/)** - Step-by-step ritual guides
- **[Daily Ledger](docs/daily-ledger/)** - Daily development activity records

### 🔧 **Scripts & Automation** → [scripts/](scripts/)
Powerful tools and automation for development workflow:

- **[LDA CLI Tool](scripts/lda)** - Complete command-line interface
- **[Chronicle Keeper](scripts/chronicle-keeper/)** - TLDL automation system
- **[Monthly Archive Generator](scripts/tldl-monthly-generator.sh)** - Consolidation ritual
- **[Initialize Agent Context](scripts/init_agent_context.sh)** - Project setup

### 🧠 **TLDL System** → [TLDL/](TLDL/)
The Living Dev Log chronicle system:

- **[TLDL Index](TLDL/index.md)** - Complete chronicle registry
- **[Entry Creation Template](docs/tldl_template.yaml)** - New chronicle template
- **[Latest Monthly Archive](docs/TLDL-Monthly/2025-08.md)** - Most recent consolidation

### 🎯 **Quick Actions**
- Create TLDL Entry: `scripts/init_agent_context.sh --create-tldl "YourTitle"`
- Generate Monthly Archive: `scripts/tldl-monthly-generator.sh --auto`
- Validate Documentation: `python3 src/SymbolicLinter/validate_docs.py --tldl-path TLDL/entries/`

---

## 🍑 **SAVE THE BUTTS! INITIATIVE**

### **Sacred Installation Ritual**
Begin your journey to Buttsafe Certification with the sacred initialization ritual:

```bash
# Clone the repository of enlightenment
git clone https://github.com/your-username/living-dev-agent-template.git my-buttastic-project
cd my-buttastic-project

# Perform the sacred butt initialization ritual
chmod +x scripts/initMyButt.sh
scripts/initMyButt.sh --comfort-mode --character-class buttwarden

# Begin your first TLDL entry documenting your character creation
scripts/init_agent_context.sh --create-tldl "MyJourneyToButtsafety"
```

### **The Four Sacred Character Classes**

Choose your path in the realm of Cheekdom:

#### 🛡️ **Buttwarden** - *Guardian of Ergonomic Enlightenment*
- Masters of workspace optimization and comfort protocols
- Specializes in protecting developers from physical and mental strain
- Abilities: Ergonomic Assessment, Comfort Aura, Emergency Repositioning

#### 🧙‍♂️ **Lintmage** - *Wielder of Code Purification Magic*  
- Wielders of code-cleaning spells and style standardization
- Transforms chaotic codebases into elegant, maintainable systems
- Abilities: Mass Refactor, Lint Storm, Code Transmutation

#### 📚 **Changelog Oracle** - *Prophet of Documentation and Version History*
- Keepers of project wisdom and development decision history
- Sees through time to understand the why behind every code change
- Abilities: Time Sight, Documentation Field, Prophetic Vision

#### 💃 **Branchdancer** - *Master of Git Flow Choreography*
- Graceful experts in version control and merge conflict resolution
- Facilitates seamless collaboration through elegant workflow design
- Abilities: Merge Dance, Branch Harmony, Flow State

### **🏆 Buttsafe Certification Standards**

Achieve legendary status through sustainable development practices:

- [ ] **Sacred Documentation**: Comprehensive README and TLDL entries
- [ ] **Character Configuration**: Choose and embrace your development class
- [ ] **Ergonomic Excellence**: Proper workspace setup and break protocols  
- [ ] **Workflow Mastery**: Clean commits, meaningful messages, collaborative reviews
- [ ] **Community Contribution**: Knowledge sharing and mentoring practices
- [ ] **Comfort Optimization**: Sustainable work habits and work-life balance

---

## 🎯 Goals

1. Package our Living Dev Log, DevTimeTravel, linters, CI pipelines, and docs into a GitHub Template.  
2. Automate extraction of key folders, scripts, configs, and documentation.  
3. Provide an easy `clone-and-clean.sh` script that spins up a fresh repo.  
4. Offer one-click cloning with GitHub's **Use this template** button.

---

## 🧩 Repository Structure

```text
living-dev-agent-template/
├── .github/
│   ├── workflows/
│   │   └── ci.yml
│   └── ISSUE_TEMPLATE/
│       ├── feature_request.md
│       └── bug_report.md
├── docs/
│   ├── Copilot-Setup.md
│   ├── devtimetravel_snapshot.yaml
│   └── tldl_template.yaml
├── scripts/
│   ├── init_agent_context.sh
│   └── clone-and-clean.sh
├── src/
│   ├── DebugOverlayValidation/
│   └── SymbolicLinter/
├── .editorconfig
├── TWG-Copilot-Agent.yaml
├── mcp-config.json
├── CONTRIBUTING.md
└── README.md
```

---

## 🔧 Quick Start

### Option 1: Use GitHub Template (Recommended)

1. **Click "Use this template"** button above
2. **Name your new repository** and configure settings
3. **Clone your new repository**:
   ```bash
   git clone https://github.com/your-username/your-new-repo.git
   cd your-new-repo
   ```
4. **Initialize the Living Dev Agent context**:
   ```bash
   chmod +x scripts/init_agent_context.sh
   scripts/init_agent_context.sh
   ```

5. **Begin the Sacred Butt Initialization Ritual** (recommended):
   ```bash
   chmod +x scripts/initMyButt.sh
   scripts/initMyButt.sh --comfort-mode
   ```

### Option 2: Manual Clone & Clean

```bash
# Clone and set up manually
git clone https://github.com/your-username/living-dev-agent-template.git my-new-project
cd my-new-project

# Run the clone-and-clean script
chmod +x scripts/clone-and-clean.sh
scripts/clone-and-clean.sh ../my-cleaned-project
```

---

## 🛠️ What's Included

### 📋 **Living Dev Log (TLDL) Workflow**
- **Structured documentation** for all development work
- **Template-driven entries** with consistent format
- **Validation tools** to ensure completeness
- **Integration with CI/CD** for automated checks

### 🕰️ **DevTimeTravel Context Capture**
- **Development state snapshots** with configurable triggers
- **Context preservation** for decision tracking
- **Integration with Git workflows** and CI processes
- **Customizable retention and storage** policies

### 🔍 **Comprehensive Linting & Validation**
- **ECS System Linter**: Validates system architecture and component usage
- **Symbolic Linter**: Checks symbol resolution and dependencies
- **Documentation Validator**: Ensures TLDL entry completeness
- **Debug Overlay Validator**: Validates debug system integration

### 🤖 **AI-Powered Development Integration**
- **GitHub Copilot configuration** optimized for this workflow
- **Model Context Protocol (MCP) support** for enhanced AI interactions
- **Automated TLDL generation** triggers and templates
- **Context-aware code suggestions** and documentation

### 🚀 **CI/CD Integration**
- **Automated validation** on every push and PR
- **Linting and quality checks** with detailed reporting
- **DevTimeTravel snapshots** on significant events
- **Integration with GitHub workflows** and status checks

### 🛡️ **Overlord Sentinel Security**
- **Free-tier security scanning** with Bandit, Semgrep OSS, and ESLint security
- **SARIF integration** for GitHub Security tab visibility
- **Dependency vulnerability monitoring** via pip-audit and npm audit
- **Chronicle Keeper integration** for security verdict preservation
- **Cost-conscious design** (≤2000 Actions minutes, no premium subscriptions required)

---

## 📚 Getting Started Guide

### 1. **Initial Setup**

After creating your repository from this template:

```bash
# Install Python dependencies
pip install -r scripts/requirements.txt

# Initialize agent context
scripts/init_agent_context.sh --verbose

# Create your first TLDL entry
scripts/init_agent_context.sh --create-tldl "ProjectInitialization"
```

### 2. **Configure for Your Project**

Edit these files to customize for your specific project:

- **`docs/devtimetravel_snapshot.yaml`**: Adjust snapshot settings
- **`TWG-Copilot-Agent.yaml`**: Configure AI assistant behavior  
- **`mcp-config.json`**: Set up Model Context Protocol if using
- **`.github/workflows/ci.yml`**: Customize CI pipeline as needed

### 3. **Validate Setup**

```bash
# Run all validation checks
python src/SymbolicLinter/validate_docs.py --tldl-path TLDL/entries/
python src/SymbolicLinter/symbolic_linter.py --path src/
python src/DebugOverlayValidation/debug_overlay_validator.py --path src/DebugOverlayValidation/
```

### 4. **Start Developing**

- **Create TLDL entries** for significant work
- **Use the linting tools** to maintain code quality
- **Leverage DevTimeTravel** for context capture
- **Follow the established patterns** in documentation and code

---

## 🧭 Core Concepts

### **TLDL (The Living Dev Log)**
A structured approach to documenting development work in real-time:

- **Discoveries**: What you learned during development
- **Actions Taken**: What you did and why
- **Next Steps**: What needs to happen next
- **Context Links**: Connections to issues, PRs, and other work

### **DevTimeTravel**
Development context capture system that:

- **Preserves decision context** for future reference
- **Captures snapshots** at key development milestones
- **Tracks code evolution** and architectural decisions
- **Enables time-travel debugging** of development decisions

### **Living Dev Agent**
AI-powered development workflow that:

- **Integrates with your tools** (IDE, GitHub, CI/CD)
- **Provides context-aware assistance** based on your project patterns
- **Automates documentation** and validation processes
- **Learns from your development patterns** over time

### **📜 Chronicle Keeper**
Automatic lore preservation system that awakens when wisdom is detected:

#### **Sacred Triggers**
The Chronicle Keeper responds to specific ritual patterns:

- **🧠 in issue titles** - Marks content as lore-worthy for automatic TLDL generation
- **`TLDL: [content]` in comments** - Direct scroll preservation requests
- **`📜 [wisdom]` in comments** - Sacred scroll markers for important insights
- **Merged PRs** - Automatic documentation of successful changes
- **Failed workflows** - Lesson capture from development challenges

#### **Ritual Usage Examples**

**Issue Title Trigger:**
```
🧠 Complex Memory Management Issue in Debug Overlay System
```

**Comment Triggers:**
```
TLDL: Found that the validation system silently fails when YAML front-matter 
is missing. Always validate template structure before pushing changes.

📜 Key insight: The symbolic linter expects imports at the top of Python files,
but our dynamic loading breaks this assumption. Added __all__ declarations.
```

#### **🧠📜 Auto-Quills Comment Templates**
Generate structured Chronicle Keeper comments with the new template system:

```bash
# List available comment templates
lda template --list

# Generate a bug discovery template
lda template --scenario bug_discovery

# Search for specific template types
lda template --search "debugging"

# Generate template with custom values
lda template --scenario ci_failure_analysis \
  --values pipeline_name="Production Deploy" \
  --values failure_category="Docker build error"
```

**Available Template Scenarios:**
- **🐛 `bug_discovery`** - Document interesting bugs with root cause analysis
- **🔍 `debugging_ritual`** - Preserve complex debugging sessions and methodologies
- **🚨 `ci_failure_analysis`** - Analyze CI/CD failures and build system issues
- **💭 `lore_reflection`** - Capture development insights and team wisdom

#### **Chronicle Keeper Workflow**
When triggered, the Chronicle Keeper automatically:

1. **Detects** the sacred trigger patterns
2. **Generates** appropriately formatted TLDL entries
3. **Commits** the scrolls to the `TLDL/entries/` directory
4. **Updates** the master chronicle index at `TLDL/index.md`
5. **Preserves** the development wisdom for future generations

**📍 Where to Find Chronicle Keeper Logs:**
- **Primary Location**: `TLDL/entries/` - All new Chronicle Keeper generated entries
- **Chronicle Index**: `docs/index.md` - Master index of all preserved scrolls  
- **Current Entries**: `TLDL/entries/` - All TLDL entries, both manual and Chronicle Keeper generated

The Chronicle Keeper ensures that hard-won knowledge doesn't get lost in the flow of development, creating a living archive of project wisdom.

---

## 🔧 Available Scripts

### **`scripts/clone-and-clean.sh`**
Sets up a fresh repository from this template:
```bash
scripts/clone-and-clean.sh /path/to/new/project
```

### **`scripts/init_agent_context.sh`**
Initializes and validates the Living Dev Agent environment:
```bash
# Standard initialization
scripts/init_agent_context.sh

# Create a TLDL entry
scripts/init_agent_context.sh --create-tldl "FeatureName"

# Dry run (show what would be done)
scripts/init_agent_context.sh --dry-run
```

### **`scripts/lda`** - Living Dev Agent CLI
Main command-line interface for Living Dev Agent operations:

```bash
# Initialize LDA in current project
lda init

# Create development snapshots
lda snapshot --id "feature-xyz" --description "Added new API endpoint"

# Manage agent profiles
lda profile list                    # List available profiles
lda profile switch validator        # Switch to specific profile
lda profile create custom           # Create new profile

# Generate Chronicle Keeper comment templates  
lda template --list                 # List available templates
lda template --scenario bug_discovery    # Generate bug discovery template
lda template --search "debugging"   # Search templates by keyword
lda template --categories           # Show template categories
```

### **Linting and Validation Tools**
```bash
# Validate TLDL entries
python src/SymbolicLinter/validate_docs.py --tldl-path TLDL/entries/

# Check symbol resolution
python src/SymbolicLinter/symbolic_linter.py --path src/

# Validate debug systems  
python src/DebugOverlayValidation/debug_overlay_validator.py --path src/DebugOverlayValidation/

# Run ECS system analysis
python src/SymbolicLinter/ecs_system_linter.py --path src/
```

---

## 🎨 Customization

### **For Python Projects**
- Add your Python source to `src/`
- Update linting paths in CI workflow
- Configure virtual environment in setup scripts

### **For Unity Projects**
- Add Unity-specific validation rules
- Configure ECS system linting for your components
- Integrate with Unity Cloud Build

### **For Web Projects**
- Add Node.js dependencies and scripts
- Configure web-specific linting tools
- Integrate with deployment pipelines

### **For Any Project Type**
- Modify the validation tools for your technology stack
- Update DevTimeTravel configuration for your workflow
- Customize Copilot behavior for your domain

---

## 🧪 Testing Your Setup

```bash
# Test template initialization
scripts/init_agent_context.sh --dry-run

# Validate all components
python src/SymbolicLinter/validate_docs.py --tldl-path TLDL/entries/
python src/SymbolicLinter/symbolic_linter.py --path src/

# Test CI pipeline locally (if using act)
act -P ubuntu-latest=ghcr.io/catthehacker/ubuntu:act-latest
```

---

## 📖 Documentation

- **[Sacred Manifesto](MANIFESTO.md)**: The complete Save The Butts! philosophy and game lore
- **[Game Design Document](docs/game-design-document.md)**: 2D ECS/DOTS RPG concept and mechanics
- **[Setup Guide](docs/Copilot-Setup.md)**: Detailed setup and configuration instructions
- **[Contributing Guide](CONTRIBUTING.md)**: Guidelines for contributing to template development
- **[TLDL Template](docs/tldl_template.yaml)**: Template for creating TLDL entries
- **[DevTimeTravel Config](docs/devtimetravel_snapshot.yaml)**: Configuration for context capture
- **[Sacred Initialization Script](scripts/initMyButt.sh)**: The holy ritual of workspace setup

---

## 🤝 Contributing

We welcome contributions to improve this template! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for:

- Development setup instructions
- Coding standards and guidelines  
- TLDL documentation requirements
- Pull request process
- Issue reporting guidelines

---

## 🌟 Features & Benefits

### ✅ **Proven Workflow**
- Battle-tested development patterns
- Comprehensive documentation approach
- Integrated tooling and automation
- AI-powered assistance integration

### ✅ **Developer Experience**
- One-click template setup
- Automated validation and linting
- Context-aware development tools
- Consistent project structure

### ✅ **Team Collaboration**
- Standardized documentation format
- Clear development decision tracking
- Integrated code review processes
- Knowledge preservation and sharing

### ✅ **Quality Assurance**
- Automated validation pipelines
- Comprehensive linting tools
- Documentation completeness checks
- Integration testing workflows

---

## 📄 License

This project is licensed under the [GNU General Public License v3.0](LICENSE). Feel free to use this template for any project, with appropriate attribution and compliance with GPLv3 terms.

---

## 🚀 Next Steps

1. **Create your repository** from this template
2. **Follow the setup guide** in `docs/Copilot-Setup.md`
3. **Start your first TLDL entry** documenting your project initialization
4. **Configure GitHub Copilot** for enhanced AI assistance
5. **Begin development** with the full Living Dev Agent workflow

---

## 💡 Advanced Workflow Insights

### Interactive AI Collaboration Patterns

This template incorporates breakthrough workflow discoveries from real-world AI collaboration:

#### **🔄 Ping-and-Fix Methodology**
Instead of traditional batch review cycles, use **real-time collaborative debugging**:

```bash
# Traditional: Code → Review → Change Request → Fix → Re-Review
# Optimized: Code → @copilot this failed, fix please → Immediate Fix → Continue
```

**Benefits:**
- **2x faster resolution** compared to formal review cycles
- **Immediate course correction** prevents compounding errors
- **Continuous progress** without workflow bottlenecks

#### **🎯 Slot Instructions During Tasks**
Provide contextual guidance **during task execution**, not just at the beginning:

- ✅ `@copilot the CI is failing on PyYAML import - add dependency installation`
- ✅ `@copilot neutrality validation has false positives - exclude .md files`
- ❌ Wait until task completion to provide all feedback at once

#### **🚀 Interactive Guidance vs Batch Processing**
Transform AI collaboration from **batch processing** to **real-time guided execution**:

| Traditional Batch Mode | Interactive Guidance Mode |
|------------------------|---------------------------|
| Complete instructions upfront | Initial direction + real-time guidance |
| Wait for full completion | Monitor progress continuously |
| Review entire result | Incremental corrections during execution |
| Request changes if needed | Achieve correct result in first cycle |

**Real-World Evidence**: See `TLDL/entries/TLDL-2024-12-19-WorkflowEvolutionInsights.md` for detailed analysis of this methodology's effectiveness during repository sanitization.

---

**Happy coding with your Living Dev Agent! 🤖✨**

> 💡 **Pro Tip**: Start every significant development session by reviewing recent TLDL entries and creating a new one for your current work. Use **ping-and-fix methodology** for AI collaboration to achieve optimal efficiency and quality.

> 🔥 **Workflow Achievement Unlock**: Use `@copilot` ping comments for immediate issue resolution instead of formal review cycles. This creates tighter feedback loops and prevents error compounding.