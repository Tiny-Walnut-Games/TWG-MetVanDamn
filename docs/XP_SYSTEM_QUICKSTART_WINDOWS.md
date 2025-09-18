# Living Dev Agent XP System - Windows Quick Start

## 🎮 Platform: Windows
**Python Command**: `python`
**Shell**: powershell

## 🚀 Quick Setup
```bash
# Test XP system
python template/src/DeveloperExperience/dev_experience.py --profile Jerry

# Award yourself setup XP
python template/src/DeveloperExperience/dev_experience.py --record "Jerry" innovation legendary "Setup XP system on windows" --metrics "platform:windows,setup:1"
```

## 🛠️ IDE Integrations Available

### VS Code
- **Tasks**: Ctrl+P → "Tasks: Run Task" → "XP: ..."
- **Keybindings**: Ctrl+Shift+X, Ctrl+Shift+D for debug session
- **Files**: `.vscode/tasks.json`, `.vscode/keybindings.json`

### Unity Editor
- **Menu**: Tools > Developer Experience > ...
- **Auto-tracking**: Play mode automatically awards test coverage XP
- **File**: `Assets/Plugins/DeveloperExperience/Editor/UnityXPIntegration.cs`

### Visual Studio
- **Batch file**: `xp_vs.bat profile` to see your XP
- **External Tools**: Add to Tools menu for easy access
- **Commands**: `xp_vs.bat [profile|debug|leaderboard]`

### JetBrains Rider
- **External Tools**: Tools > External Tools > XP: ...
- **Configuration**: `.idea/externalTools.xml`
- **Usage**: Right-click → External Tools → XP tools

## 🎯 Platform-Specific Features

### Windows Features
- **PowerShell support**: Handles Unicode emojis properly
- **Batch file helpers**: Easy command-line access
- **Git Bash compatibility**: Works with Git for Windows
- **Path handling**: Automatic Windows path conversion

## 🧪 Test Your Setup

```bash
# 1. Test basic functionality
python template/src/DeveloperExperience/dev_experience.py --help

# 2. Create your first contribution
python template/src/DeveloperExperience/dev_experience.py --record "Jerry" code_contribution good "Testing XP system on windows" --metrics "platform_test:1"

# 3. Check your profile
python template/src/DeveloperExperience/dev_experience.py --profile "Jerry"

# 4. Test daily bonus
python template/src/DeveloperExperience/dev_experience.py --daily-bonus "Jerry"
```

## 🎮 Next Steps
1. **Make a Git commit** - Git hooks will automatically award XP
2. **Open Unity** - Use Tools > Developer Experience menu
3. **Try VS Code shortcuts** - Ctrl+Shift+X combinations
4. **Check your leaderboard** - See how you rank!

Your XP system is ready for windows! 🏆
