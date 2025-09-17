#!/bin/bash
# Living Dev Agent Template - Professional Initialization Script
# Jerry's "Actually Works" initialization that hooks into real development workflow
# 
# Replaces InitMyBut.sh*t with something that integrates with Unity, IDEs, and team workflows

set -e  # Exit on any error

# Color codes for epic output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Sacred emojis
ROCKET="🚀"
CHECKMARK="✅"
WARNING="⚠️"
ERROR="❌"
WIZARD="🧙‍♂️"
GAMEPAD="🎮"
TROPHY="🏆"

echo -e "${PURPLE}${WIZARD} Living Dev Agent Template Initialization${NC}"
echo -e "${CYAN}Jerry's Professional Development Environment Setup${NC}"
echo ""

# Check if we're in a git repository
if [ ! -d ".git" ]; then
    echo -e "${ERROR} Not a Git repository. Initializing..."
    git init
    
    # Check for git config
    if ! git config user.name > /dev/null 2>&1; then
        echo -e "${WARNING} Git user.name not set. Please configure:"
        echo -e "  ${CYAN}git config --global user.name \"Your Name\"${NC}"
    fi
    
    if ! git config user.email > /dev/null 2>&1; then
        echo -e "${WARNING} Git user.email not set. Please configure:"
        echo -e "  ${CYAN}git config --global user.email \"your.email@example.com\"${NC}"
    fi
fi

# Check Python availability
echo -e "${BLUE}${ROCKET} Checking Python environment...${NC}"
if command -v python3 &> /dev/null; then
    PYTHON_VERSION=$(python3 --version | cut -d' ' -f2)
    echo -e "${CHECKMARK} Python 3 found: ${PYTHON_VERSION}"
else
    echo -e "${ERROR} Python 3 not found. Please install Python 3.8+ and try again."
    exit 1
fi

# Install Python dependencies
echo -e "${BLUE}${ROCKET} Installing Python dependencies...${NC}"
if [ -f "scripts/requirements.txt" ]; then
    pip3 install -r scripts/requirements.txt --quiet
    echo -e "${CHECKMARK} Python dependencies installed"
else
    echo -e "${WARNING} No requirements.txt found - creating minimal one"
    mkdir -p scripts
    cat > scripts/requirements.txt << EOF
PyYAML>=6.0
argparse>=1.4.0
EOF
    pip3 install -r scripts/requirements.txt --quiet
fi

# Create necessary directories
echo -e "${BLUE}${ROCKET} Creating project structure...${NC}"
mkdir -p .github/workflows
mkdir -p src/DeveloperExperience
mkdir -p experience/hooks
mkdir -p docs
mkdir -p .vscode

# Copy template files if they don't exist
if [ ! -f "src/DeveloperExperience/dev_experience.py" ]; then
    echo -e "${WARNING} XP system not found - you may need to copy template files"
fi

# Setup development environment integrations
echo -e "${BLUE}${GAMEPAD} Setting up development environment integrations...${NC}"
if [ -f "src/DeveloperExperience/dev_integration.py" ]; then
    python3 src/DeveloperExperience/dev_integration.py --setup-all
    if [ $? -eq 0 ]; then
        echo -e "${CHECKMARK} Development environment integrations setup successfully"
    else
        echo -e "${WARNING} Some integrations may have failed - check output above"
    fi
else
    echo -e "${WARNING} Integration script not found - manual setup required"
fi

# Initialize XP system with first achievement
echo -e "${BLUE}${TROPHY} Initializing XP system...${NC}"
if [ -f "src/DeveloperExperience/dev_experience.py" ]; then
    # Get current user
    CURRENT_USER=${USER:-${USERNAME:-"Developer"}}
    
    # Award setup achievement
    python3 src/DeveloperExperience/dev_experience.py --record "$CURRENT_USER" innovation legendary "Initialized Living Dev Agent Template with full integrations" --metrics "template_setup:1,integrations:4"
    
    if [ $? -eq 0 ]; then
        echo -e "${CHECKMARK} XP system initialized - you earned your first achievement!"
        
        # Show profile
        echo -e "${BLUE}${TROPHY} Your developer profile:${NC}"
        python3 src/DeveloperExperience/dev_experience.py --profile "$CURRENT_USER"
    fi
fi

# Setup Unity integration (if Unity project detected)
if [ -f "Assets" ] || [ -f "ProjectSettings/ProjectVersion.txt" ]; then
    echo -e "${BLUE}${ROCKET} Unity project detected - setting up Unity integration...${NC}"
    
    # Create Unity integration directories
    mkdir -p Assets/Plugins/DeveloperExperience/Editor
    
    # Unity integration is already created by dev_integration.py
    echo -e "${CHECKMARK} Unity XP integration ready"
    echo -e "${CYAN}  Menu: Tools > Developer Experience > XP Tracker${NC}"
fi

# Setup GitHub Actions (if .github directory exists)
if [ -d ".github" ]; then
    echo -e "${BLUE}${ROCKET} Setting up CI/CD integration...${NC}"
    
    # Create basic CI workflow that includes XP tracking
    cat > .github/workflows/xp_system_ci.yml << 'EOF'
name: XP System CI

on: [push, pull_request]

jobs:
  validate_xp:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - name: Set up Python
      uses: actions/setup-python@v3
      with:
        python-version: '3.8'
    - name: Install dependencies
      run: |
        pip install PyYAML argparse
    - name: Validate XP System
      run: |
        if [ -f "src/DeveloperExperience/dev_experience.py" ]; then
          python3 src/DeveloperExperience/dev_experience.py --help
          echo "✅ XP system validation passed"
        fi
    - name: Show XP Leaderboard
      run: |
        if [ -f "src/DeveloperExperience/dev_experience.py" ]; then
          python3 src/DeveloperExperience/dev_experience.py --leaderboard || true
        fi
EOF
    
    echo -e "${CHECKMARK} CI/CD integration setup"
fi

# Create quick-start documentation
echo -e "${BLUE}${ROCKET} Creating documentation...${NC}"
cat > docs/XP_SYSTEM_QUICK_START.md << 'EOF'
# Living Dev Agent XP System - Quick Start

## 🎮 Your Development Experience is Now Gamified!

### Automatic XP Earning
- **Git commits** automatically award XP based on commit quality
- **Unity play mode testing** gives test coverage XP
- **Unity builds** award architecture XP
- **File changes** are analyzed for contribution type and quality

### Manual XP Tracking
- **VS Code**: Use `Ctrl+Shift+X` shortcuts for quick XP operations
- **Unity**: Use `Tools > Developer Experience` menu
- **Command Line**: Use `python3 src/DeveloperExperience/dev_experience.py`

### CopilotCoin Shop
Spend earned coins on premium Copilot features:
- Extra debugging sessions (75 coins)
- Architecture consultations (100 coins)
- Custom achievements (200 coins)
- Legacy code rescue (400 coins)

### Keyboard Shortcuts (VS Code)
- `Ctrl+Shift+X, Ctrl+Shift+D` - Record debugging session
- `Ctrl+Shift+X, Ctrl+Shift+P` - Show your profile
- `Ctrl+Shift+X, Ctrl+Shift+L` - Show leaderboard
- `Ctrl+Shift+X, Ctrl+Shift+S` - Spend CopilotCoins

### Unity Menu Items
- `Tools > Developer Experience > XP Tracker` - Main interface
- `Tools > Developer Experience > Record Debug Session` - Quick debug XP
- `Tools > Developer Experience > Show My Profile` - Your stats
- `Tools > Developer Experience > Show Leaderboard` - Team rankings

### Getting Started
1. Make your first commit - automatically earn XP!
2. Open Unity and use `Tools > Developer Experience > Show My Profile`
3. In VS Code, press `Ctrl+Shift+X, Ctrl+Shift+P` to see your profile
4. Start earning XP with every contribution!

### Achievement Categories
- 🔥 Triple FUCK Moment Slayer - Resolve 3+ issues in one session
- 📚 Documentation Champion - Create 10 documentation contributions
- 🎯 Consistent Contributor - 5+ contributions in one week
- 🏗️ System Architect - Architecture contributions
- 🌟 Legendary [Type] - Legendary quality work

### Faculty Badges
- 📝 Console Commentary Sage - Quality debugging documentation
- 📸 Context Capture Virtuoso - Perfect code snapshots
- 🎯 Epic Quest Coordinator - Project management excellence
- ⏰ Temporal Flow Master - Time tracking mastery
- 🛡️ Quality Assurance Sentinel - Testing excellence

Start contributing and watch your developer legend grow! 🧙‍♂️⚡
EOF

echo -e "${CHECKMARK} Quick start documentation created"

# Final status check
echo ""
echo -e "${PURPLE}${WIZARD} Living Dev Agent Template Initialization Complete!${NC}"
echo ""
echo -e "${GREEN}${CHECKMARK} Integration Status:${NC}"

# Check Git hooks
if [ -f ".git/hooks/post-commit" ]; then
    echo -e "  ${CHECKMARK} Git hooks: Installed"
else
    echo -e "  ${WARNING} Git hooks: Not installed"
fi

# Check VS Code integration
if [ -f ".vscode/tasks.json" ]; then
    echo -e "  ${CHECKMARK} VS Code: Integrated"
else
    echo -e "  ${WARNING} VS Code: Not integrated"
fi

# Check Unity integration
if [ -f "Assets/Plugins/DeveloperExperience/UnityXPIntegration.cs" ]; then
    echo -e "  ${CHECKMARK} Unity: Integrated"
else
    echo -e "  ${WARNING} Unity: Not integrated"
fi

# Check XP system
if [ -f "experience/developer_profiles.json" ]; then
    echo -e "  ${CHECKMARK} XP System: Active with profiles"
else
    echo -e "  ${CHECKMARK} XP System: Ready for first use"
fi

echo ""
echo -e "${CYAN}🎮 Your development environment is now GAMIFIED!${NC}"
echo -e "${CYAN}Start earning XP with every commit, debug session, and contribution!${NC}"
echo ""
echo -e "${BLUE}Next Steps:${NC}"
echo -e "  1. Make a commit to earn your first automatic XP"
echo -e "  2. Open Unity and check Tools > Developer Experience"
echo -e "  3. In VS Code, try Ctrl+Shift+X, Ctrl+Shift+P to see your profile"
echo -e "  4. Read docs/XP_SYSTEM_QUICK_START.md for full details"
echo ""
echo -e "${PURPLE}${WIZARD} Welcome to the Living Dev Agent experience! ${TROPHY}${NC}"
