#!/bin/bash
# Sacred Workflow Visibility Enhancement for Living Dev Agent Template
# 🧙‍♂️ Auto-import Sacred Workflows into IDE workspace visibility

echo "🎯 Sacred Workflow Visibility Enhancement Ritual Initiated..."

# Ensure .vscode directory exists
mkdir -p .vscode

# Create Sacred Workspace Configuration
cat > .vscode/living-dev-agent.code-workspace << 'EOF'
{
  "folders": [
    {
      "name": "🏠 Project Root",
      "path": "."
    },
    {
      "name": "🧙‍♂️ Sacred Workflows",
      "path": ".github/workflows"
    },
    {
      "name": "📜 Sacred Scripts",
      "path": "scripts"
    },
    {
      "name": "📚 Sacred Documentation",
      "path": "docs"
    },
    {
      "name": "🔧 Sacred Configuration",
      "path": ".vscode"
    }
  ],
  "settings": {
    "files.associations": {
      "*.yml": "yaml",
      "*.yaml": "yaml",
      "*.agent-profile.yaml": "yaml",
      "*.tldl": "markdown",
      "copilot-instructions.md": "markdown"
    },
    "explorer.excludeGitIgnore": false,
    "search.exclude": {
      ".github/workflows": false,
      "scripts": false,
      "docs": false
    },
    "files.watcherExclude": {
      ".github/workflows": false,
      "scripts": false
    },
    "yaml.schemas": {
      "https://json.schemastore.org/github-workflow.json": [
        ".github/workflows/*.yml",
        ".github/workflows/*.yaml"
      ]
    },
    "workbench.colorCustomizations": {
      "tab.activeBorder": "#ff6b35",
      "statusBar.background": "#ff6b35"
    },
    "workbench.colorTheme": "Dark+ (default dark)",
    "editor.rulers": [80, 120],
    "editor.minimap.enabled": true,
    "explorer.sortOrder": "type",
    "files.autoSave": "onFocusChange"
  },
  "extensions": {
    "recommendations": [
      "ms-vscode.vscode-yaml",
      "GitHub.copilot",
      "GitHub.copilot-chat",
      "ms-python.python",
      "ms-dotnettools.csharp"
    ]
  }
}
EOF

echo "✅ Sacred Workspace Configuration Created: .vscode/living-dev-agent.code-workspace"

# Enhanced VS Code Settings for Sacred Workflow Visibility
cat > .vscode/settings.json << 'EOF'
{
  "files.associations": {
    "*.yml": "yaml",
    "*.yaml": "yaml", 
    "*.agent-profile.yaml": "yaml",
    "copilot-instructions.md": "markdown",
    "TLDL-*.md": "markdown",
    "*.tldl": "markdown"
  },
  "explorer.excludeGitIgnore": false,
  "search.exclude": {
    ".github/workflows": false,
    "scripts": false,
    "docs": false,
    "src": false
  },
  "files.watcherExclude": {
    ".github/workflows": false,
    "scripts": false,
    "docs": false
  },
  "yaml.schemas": {
    "https://json.schemastore.org/github-workflow.json": [
      ".github/workflows/*.yml",
      ".github/workflows/*.yaml"
    ]
  },
  "workbench.tree.indent": 12,
  "explorer.sortOrder": "type",
  "files.autoSave": "onFocusChange",
  "editor.formatOnSave": true,
  "python.defaultInterpreterPath": "python3",
  "terminal.integrated.defaultProfile.linux": "bash",
  "terminal.integrated.defaultProfile.windows": "PowerShell",
  "git.openRepositoryInParentFolders": "always",
  "git.autofetch": true,
  "markdown.preview.fontSize": 14,
  "markdown.preview.lineHeight": 1.6
}
EOF

echo "✅ Enhanced VS Code Settings Applied"

# Create Sacred File Auto-Opener Script
cat > scripts/open-sacred-arsenal.sh << 'EOF'
#!/bin/bash
# 🧙‍♂️ Sacred Workflow Arsenal Auto-Opener
# Opens all Sacred Workflows and Scripts for AI agent visibility

echo "🧙‍♂️ Opening Sacred Workflow Arsenal..."

# Function to open files based on available IDE
open_sacred_files() {
    local files_to_open=(
        ".github/workflows/*.yml"
        "scripts/*.sh"
        "scripts/*.py"
        "docs/copilot-instructions.md"
        "docs/TLDL-*.md"
        ".vscode/settings.json"
        ".agent-profile.yaml"
        "mcp-config.json"
    )
    
    # VS Code
    if command -v code &> /dev/null; then
        echo "📝 Opening files in VS Code..."
        for pattern in "${files_to_open[@]}"; do
            find . -path "./$pattern" -type f 2>/dev/null | while read -r file; do
                code "$file" 2>/dev/null &
            done
        done
        
        # Open the Sacred Workspace
        if [[ -f ".vscode/living-dev-agent.code-workspace" ]]; then
            code ".vscode/living-dev-agent.code-workspace" &
        fi
        echo "✅ Sacred Arsenal opened in VS Code"
        return 0
    fi
    
    # JetBrains Rider
    if command -v rider &> /dev/null; then
        echo "📝 Opening project in JetBrains Rider..."
        rider . &
        echo "✅ Sacred Arsenal opened in Rider"
        return 0
    fi
    
    # Visual Studio (Windows)
    if command -v devenv &> /dev/null; then
        echo "📝 Opening project in Visual Studio..."
        find . -name "*.sln" | head -1 | xargs devenv &
        echo "✅ Sacred Arsenal opened in Visual Studio"
        return 0
    fi
    
    echo "⚠️ No supported IDE found. Please manually open the Sacred Workflow files."
    echo "📁 Sacred Workflow locations:"
    echo "   - .github/workflows/"
    echo "   - scripts/"
    echo "   - docs/"
    echo "   - .vscode/living-dev-agent.code-workspace"
}

open_sacred_files
EOF

chmod +x scripts/open-sacred-arsenal.sh
echo "✅ Sacred Arsenal Auto-Opener Created: scripts/open-sacred-arsenal.sh"

# Create Sacred Workflow Index for Documentation
cat > docs/SACRED-WORKFLOW-ARSENAL.md << 'EOF'
# 🧙‍♂️ Sacred Workflow Arsenal Index

*The complete collection of Sacred Workflows and automation spells for Living Dev Agent mastery.*

## 🎯 Quick Access Commands

```bash
# Open all Sacred Workflows in your IDE
scripts/open-sacred-arsenal.sh

# Validate Sacred Arsenal integrity  
scripts/init_agent_context.sh

# Create new TLDL quest log
scripts/init_agent_context.sh --create-tldl "YourQuestName"
```

## 📜 Sacred Workflow Collection

### 🧙‍♂️ AI Collaboration & Documentation
- **[AI Collaboration Detector](.github/workflows/ai-collaboration-detector.yml)** - Auto-detects AI assistance and creates TLDL entries
- **[TLDL Monthly Archive](.github/workflows/tldl-monthly-archive.yml)** - Monthly consolidation of Living Dev Log entries
- **[Chronicle Keeper](.github/workflows/chronicle-keeper.yml)** - Maintains project history and narrative

### 🔒 Security & Quality Guardians
- **[Overlord Sentinel](.github/workflows/overlord-sentinel.yml)** - Advanced security monitoring
- **[Overlord Sentinel Security](.github/workflows/overlord-sentinel-security.yml)** - Security-focused validation
- **[CID Faculty](.github/workflows/cid-faculty.yml)** - Code quality education
- **[CID Schoolhouse](.github/workflows/cid-schoolhouse.yml)** - Learning-focused quality checks
- **[Circular Dependency Guard](.github/workflows/circular-dependency-guard.yml)** - Prevents circular dependencies

### 🚀 CI/CD & Build Management
- **[Main CI Pipeline](.github/workflows/ci.yml)** - Core build and validation pipeline
- **[Shield Demo](.github/workflows/shield-demo.yml)** - Demo environment management
- **[Security Scanner](.github/workflows/security.yml)** - Security vulnerability scanning

### 📚 Documentation & Architecture
- **[Docs Architecture Check](.github/workflows/docs-architecture-check.yml)** - Documentation structure validation

## 🔧 Sacred Scripts Collection

### 🎯 Core Sacred Scripts
- **[Init Agent Context](scripts/init_agent_context.sh)** - ~180ms - Initialize Living Dev Agent environment
- **[Clone and Clean](scripts/clone-and-clean.sh)** - ~53ms - Template creation and setup
- **[Open Sacred Arsenal](scripts/open-sacred-arsenal.sh)** - Auto-open all Sacred Workflows in IDE

### 🧙‍♂️ AI Integration Scripts
- **[AI Collaboration Detector](scripts/ai-collaboration-detector.py)** - Detect AI assistance significance
- **[Auto TLDL Generator](scripts/auto-tldl-generator.py)** - Generate TLDL entries from collaboration
- **[Oracle Predictor](scripts/oracle-predictor.py)** - Future impact predictions

## 🛡️ Sacred Validation Rituals

### ⚡ Performance Guarantees
- **TLDL Validation**: ~60ms execution
- **Debug Overlay Validation**: ~56ms execution  
- **Symbolic Linting**: ~68ms execution
- **Template Initialization**: ~180ms execution
- **Template Creation**: ~53ms execution

### 🧙‍♂️ Validation Commands
```bash
# Sacred Validation Trinity (run before any major changes)
python3 src/SymbolicLinter/validate_docs.py --tldl-path docs/        # 60ms
python3 src/DebugOverlayValidation/debug_overlay_validator.py --path src/  # 56ms  
python3 src/SymbolicLinter/symbolic_linter.py --path src/            # 68ms
```

## 🔮 Sacred Configuration Files

### 🧙‍♂️ Living Dev Agent Configuration
- **[Copilot Instructions](.github/copilot-instructions.md)** - AI agent personality and behavior
- **[Agent Profile](.agent-profile.yaml)** - Project-specific AI preferences
- **[MCP Config](mcp-config.json)** - Model Context Protocol settings

### 🎯 IDE Integration
- **[VS Code Workspace](.vscode/living-dev-agent.code-workspace)** - Sacred Workflow visibility
- **[VS Code Settings](.vscode/settings.json)** - Enhanced development environment
- **[Editor Config](.editorconfig)** - Cross-IDE consistency

## 🎭 Sacred Workflow Philosophy

### 🧙‍♂️ The Living Dev Agent Principles
1. **Sacred Symbol Preservation** - No knowledge shall be lost
2. **Cheek Preservation Protocol** - Defensive coding and humor prevent disasters
3. **Achievement Documentation** - Every victory deserves a TLDL entry
4. **Oracle Wisdom** - Future impact prediction guides decisions
5. **Collaborative Intelligence** - AI and human working as one

### 🍑 Cheek Preservation Strategies
- **Defensive Snapshots**: Capture state before risky operations
- **Validation Rituals**: Pre-battle preparations catch issues early
- **Sarcastic Intervention**: Well-timed humor prevents meltdowns
- **Backup Protocols**: Always have an escape route

## 🏆 Achievement Gallery

### 🎯 Sacred Accomplishments
- ✅ **Sub-200ms Validation** - All Sacred Rituals execute in milliseconds
- ✅ **Zero-Configuration Setup** - Template works out of the box
- ✅ **Multi-IDE Support** - VS Code, Rider, Visual Studio, OmniSharp
- ✅ **Automatic Documentation** - AI collaboration generates TLDL entries
- ✅ **Oracle Integration** - Future predictions guide development

*"A Sacred Workflow Arsenal is only as powerful as the adventurers who wield it!"* 🧙‍♂️⚔️✨

---

**Generated by Sacred Workflow Visibility Enhancement v1.0**  
*Part of the Living Dev Agent Template ecosystem*
EOF

echo "📚 Sacred Workflow Arsenal Index Created: docs/SACRED-WORKFLOW-ARSENAL.md"

# Create enhanced .gitignore entries for Sacred Visibility
if [[ -f ".gitignore" ]]; then
    echo "" >> .gitignore
    echo "# Sacred Workflow Visibility - Keep IDE workspace configurations" >> .gitignore
    echo "# Remove this section if you want to share Sacred IDE configurations" >> .gitignore
    echo ".vscode/launch.json" >> .gitignore
    echo ".vscode/tasks.json" >> .gitignore
    echo "*.code-workspace.backup" >> .gitignore
fi

echo "📋 Updated .gitignore for Sacred Workflow visibility"

# Final Sacred Ritual - Test the Enhancement
echo ""
echo "🎯 Testing Sacred Workflow Visibility Enhancement..."

if [[ -f ".vscode/living-dev-agent.code-workspace" ]]; then
    echo "✅ Sacred Workspace Configuration: PRESENT"
else
    echo "❌ Sacred Workspace Configuration: MISSING"
fi

if [[ -f "scripts/open-sacred-arsenal.sh" && -x "scripts/open-sacred-arsenal.sh" ]]; then
    echo "✅ Sacred Arsenal Auto-Opener: PRESENT & EXECUTABLE"
else
    echo "❌ Sacred Arsenal Auto-Opener: MISSING OR NOT EXECUTABLE"
fi

if [[ -f "docs/SACRED-WORKFLOW-ARSENAL.md" ]]; then
    echo "✅ Sacred Workflow Arsenal Index: PRESENT"
else
    echo "❌ Sacred Workflow Arsenal Index: MISSING"
fi

echo ""
echo "🧙‍♂️ Sacred Workflow Visibility Enhancement Complete!"
echo ""
echo "🎯 Next Steps for Ultimate Developer Enjoyment:"
echo "1. 📝 Run: scripts/open-sacred-arsenal.sh"
echo "2. 🧙‍♂️ Open: .vscode/living-dev-agent.code-workspace" 
echo "3. 📚 Reference: docs/SACRED-WORKFLOW-ARSENAL.md"
echo "4. ⚡ Validate: scripts/init_agent_context.sh"
echo ""
echo "🍑 Cheek Preservation Protocol: ACTIVE"
echo "🔮 Oracle Status: MONITORING"
echo "✨ Sacred Symbol Preservation: ENHANCED"
echo ""
echo "*\"Now every AI agent shall see the Sacred Workflow Arsenal in all its glory!\"* 🧙‍♂️⚔️✨"
