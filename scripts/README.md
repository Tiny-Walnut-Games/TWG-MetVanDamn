# 🛠️ Living Dev Agent Scripts

Welcome to the command center of the Living Dev Agent! This directory houses the essential scripts that power template creation, context initialization, and project setup workflows.

## 🧙‍♂️ Script Lore

These scripts are the **automation spells** that transform a simple repository into a fully-functional Living Dev Agent environment. Each script has been carefully crafted and timed to ensure reliable, predictable execution while maintaining the adventure spirit of development.

## 🎯 Core Scripts

### 🚀 `init_agent_context.sh`
**The Context Initialization Spell** - Sets up the complete development environment

**Execution Profile:**
- **Timing**: ~180ms (lightning fast!)
- **Purpose**: Initialize template context and prepare development environment
- **Dependencies**: Python 3.11+, bash, git configuration
- **Expected Behavior**: Warnings about symbolic linting are normal and expected

**Usage Patterns:**
```bash
# Standard initialization (the most common quest starter)
scripts/init_agent_context.sh

# Create new TLDL entry with initialization
scripts/init_agent_context.sh --create-tldl "EpicFeatureDevelopment"

# Validate existing setup
scripts/init_agent_context.sh --validate-only
```

**🍑 Cheek Preservation Features:**
- Validates Python environment before proceeding
- Checks for required git configuration
- Creates backup snapshots of critical files
- Provides clear error messages for common issues

### 🏗️ `clone-and-clean.sh`
**The Template Creation Ritual** - Creates new projects from the Living Dev Agent template

**Execution Profile:**
- **Timing**: ~53ms (faster than you can say "new project"!)
- **Purpose**: Clone template and prepare clean project structure
- **Requirements**: Git with configured user.name and user.email
- **Auto-Magic**: Creates initial Git repository and commit

**Usage Patterns:**
```bash
# Create new project (the classic adventure begins)
scripts/clone-and-clean.sh /path/to/new/project

# Create project with custom branch name
scripts/clone-and-clean.sh /path/to/new/project --branch main

# Verbose mode for debugging template issues
scripts/clone-and-clean.sh /path/to/new/project --verbose
```

**🛡️ Safety Mechanisms:**
- Validates target directory doesn't exist
- Ensures git credentials are configured
- Creates backup of original if overwriting
- Provides rollback capabilities for failed operations

## 🧰 Dependencies and Requirements

### 📦 `requirements.txt`
**The Dependency Scroll** - Python packages required for full functionality

**Installation Profile:**
- **Timing**: 5-10 seconds (network dependent)
- **Tolerance**: Installation timeouts are acceptable - core deps usually pre-installed
- **Critical Dependencies**: PyYAML>=6.0, argparse>=1.4.0
- **Install Command**: `pip install -r scripts/requirements.txt`

**🔮 Magic Ingredients:**
```txt
PyYAML>=6.0       # YAML parsing for configuration files
argparse>=1.4.0   # Command-line argument processing
requests>=2.25.0  # HTTP operations (optional for extended features)
colorama>=0.4.0   # Terminal color output (adventure enhancement)
```

## 🎮 Usage Workflows

### 🌟 Template Bootstrap Sequence
The sacred ritual for setting up a new Living Dev Agent environment:

```bash
# Step 1: Ensure required directory exists
mkdir -p .github/workflows

# Step 2: Install dependencies (may timeout, but core deps are usually available)
pip install -r scripts/requirements.txt

# Step 3: Make scripts executable
chmod +x scripts/init_agent_context.sh scripts/clone-and-clean.sh

# Step 4: Initialize context (the moment of truth!)
scripts/init_agent_context.sh
```

### 🏆 New Project Creation Quest
For creating new projects from the template:

```bash
# Step 1: Configure git (absolutely critical!)
git config --global user.email "your.email@example.com"
git config --global user.name "Your Name"

# Step 2: Create project (the birth of a new adventure!)
scripts/clone-and-clean.sh /path/to/new/project

# Step 3: Navigate and initialize
cd /path/to/new/project
scripts/init_agent_context.sh
```

## 🧬 Performance Benchmarks

### Validated Execution Times
These timings have been measured and validated across multiple environments:

| Script                         | Average Time   | Range     | Status               |
|--------------------------------|----------------|-----------|----------------------|
| `init_agent_context.sh`        | 180ms          | 160-200ms | ✅ Stable             |
| `clone-and-clean.sh`           | 53ms           | 45-65ms   | ✅ Reliable           |
| `pip install requirements.txt` | 8s             | 5-10s     | ⚠️ Network dependent |

### 🎯 Performance Expectations
- **Sub-second execution** for core template operations
- **Network tolerance** for dependency installation
- **Graceful degradation** when optional dependencies fail
- **Clear feedback** during longer operations

## 🍑 Cheek Preservation Protocols

### Error Handling Wisdom
- **Timeout tolerance**: Scripts handle network timeouts gracefully
- **Prerequisite validation**: Check environment before execution
- **Rollback capabilities**: Undo operations when things go sideways
- **Descriptive failures**: Error messages guide toward solutions

### Common Rescue Scenarios

#### 🚨 "Template structure validation failed"
```bash
# The classic missing directory issue
mkdir -p .github/workflows
scripts/init_agent_context.sh
```

#### 🚨 "Git commit fails during clone-and-clean"
```bash
# Git credentials not configured
git config --global user.name "Your Name"
git config --global user.email "your.email@example.com"
scripts/clone-and-clean.sh /path/to/project
```

#### 🚨 "Python3 not available"
```bash
# Cross-platform Python checks (after running any script that bootstraps $PY)
"$PY" --version  # Verify Python
which python3 || which python  # Locate Python installation
```

## 🧾 Sacred Script Maintenance

### Modification Guidelines
- **Preserve timing characteristics** - maintain performance benchmarks
- **Document behavioral changes** - update README with new features
- **Test across environments** - validate on different systems
- **Maintain adventure narrative** - keep the engaging tone in output

### Extension Patterns
- **Follow established naming** - use descriptive, action-oriented names
- **Include timing information** - measure and document execution time
- **Provide usage examples** - show common and edge-case scenarios
- **Integrate with existing workflows** - respect established patterns

---

*"Great scripts are like trusted companions - reliable, helpful, and they make the journey more enjoyable."* 🛠️✨
