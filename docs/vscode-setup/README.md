# VS Code Setup for MetVanDAMN Testing

## 🎯 Quick Setup

To enable VS Code testing integration, copy the configuration files from this folder to your workspace root:

```bash
# From the repository root
cp docs/vscode-setup/* .vscode/
```

Or manually create `.vscode/` folder and copy the files:
- `tasks.json` - Test execution tasks
- `launch.json` - Debug configurations  
- `settings.json` - Project settings
- `test.runsettings` - Test runner configuration
- `extensions.json` - Recommended extensions

## 📁 Configuration Files

### `tasks.json`
Provides VS Code tasks for:
- **Unity: Run All Tests (VS Code Optimized)** - Uses vscode-test-runner.ps1
- **Unity: Run All Tests (Headless)** - Uses enhanced run_tests.ps1
- **Unity: Run PlayMode Tests** - PlayMode-specific execution
- **Unity: Run EditMode Tests** - EditMode-specific execution
- **Unity: Run Specific Test** - Interactive test filtering
- **Unity: Open Test Results** - Opens debug folder
- **Unity: Clean Test Results** - Cleanup utilities

### `launch.json`
Debug configurations for:
- Unity PlayMode test debugging
- Unity EditMode test debugging
- Attach to Unity Editor

### `settings.json`
Optimized project settings including:
- OmniSharp configuration for better C# support
- Unity-specific file associations and exclusions
- Test runner integration with code lens support
- Proper indentation and formatting rules

### `test.runsettings`
Test runner configuration with:
- Code coverage settings
- TRX and HTML result generation
- Proper module filtering for Unity assemblies

### `extensions.json`
Recommended VS Code extensions:
- C# dev tools
- Unity debugger support
- Test explorer integration

## 🚀 Usage After Setup

1. **Install recommended extensions** (VS Code will prompt)
2. **Open Command Palette** (`Ctrl+Shift+P`)
3. **Select**: `Tasks: Run Task` → Choose desired test task
4. **For debugging**: Use `Run and Debug` panel with Unity configurations

## ⚠️ Note

These files are not included in the repository by default due to `.gitignore` settings. This allows developers to customize their VS Code setup without affecting the shared repository configuration.

For team standardization, consider creating a team-specific setup script or documentation that ensures consistent VS Code configuration across all developers.