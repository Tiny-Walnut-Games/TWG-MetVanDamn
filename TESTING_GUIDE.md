# MetVanDAMN Testing Guide

## 🧙‍♂️ Overview

This guide covers both **headless testing** (automated CI/CD) and **VS Code testing** (developer workflow) for the MetVanDAMN project.

## 🚀 Quick Start

### For VS Code Users
1. **Install recommended extensions** (VS Code will prompt you)
2. **Open Command Palette** (`Ctrl+Shift+P`)
3. **Run Task**: `Unity: Run All Tests`

### For Command Line
```powershell
# Run all tests
pwsh ./run_tests.ps1

# Run specific test mode
$env:UNITY_TEST_PLATFORM = "PlayMode"
pwsh ./run_tests.ps1

# Run with filter
$env:UNITY_TEST_FILTER = "SmokeTestSceneSetup"
pwsh ./run_tests.ps1
```

## 🎯 Testing Options

### 1. **Headless Testing** (Primary Solution)
**Fixed Issues:**
- ✅ **Enhanced XML Generation**: Comprehensive NUnit-compatible XML with detailed test results
- ✅ **Multi-Mode Support**: EditMode, PlayMode, and All tests 
- ✅ **Better Error Handling**: Multiple fallback strategies with detailed logging
- ✅ **Improved PowerShell Script**: Enhanced detection and method selection

**Usage:**
```powershell
# Basic usage - auto-detects Unity and runs tests
pwsh ./run_tests.ps1

# Specify test platform
$env:UNITY_TEST_PLATFORM = "EditMode"    # or "PlayMode" 
pwsh ./run_tests.ps1

# Filter specific tests
$env:UNITY_TEST_FILTER = "MyTestClass.MyTestMethod"
pwsh ./run_tests.ps1

# Filter by category
$env:UNITY_TEST_CATEGORY = "Integration;Unit"
pwsh ./run_tests.ps1
```

### 2. **VS Code Integration** (Alternative Solution)
**Features:**
- 🎮 **Task-based execution** via Command Palette
- 🔍 **Interactive test filtering** with prompts
- 📊 **Results viewing** in integrated terminal
- 🛠️ **Debugging support** with Unity debugger
- 🧹 **Cleanup utilities** for test artifacts

**Available Tasks:**
- `Unity: Run All Tests` - Runs both EditMode and PlayMode
- `Unity: Run PlayMode Tests` - PlayMode tests only
- `Unity: Run EditMode Tests` - EditMode tests only  
- `Unity: Run Specific Test` - Prompts for test filter
- `Unity: Open Test Results` - Opens debug folder
- `Unity: Clean Test Results` - Cleans up old results

## 🧰 Components

### Enhanced HeadlessTestRunner.cs
**New Features:**
- **Multi-mode support**: `RunEditMode()`, `RunPlayMode()`, `RunAll()`
- **Enhanced XML output**: Detailed NUnit-compatible format with timing, messages, stack traces
- **Better logging**: Console output shows test progress and results
- **Improved error handling**: Graceful failure handling with detailed error logs

**Methods:**
```csharp
// Run specific test modes
TinyWalnutGames.Tools.Editor.HeadlessTestRunner.RunEditMode
TinyWalnutGames.Tools.Editor.HeadlessTestRunner.RunPlayMode
TinyWalnutGames.Tools.Editor.HeadlessTestRunner.RunAll
```

### Enhanced run_tests.ps1
**Improvements:**
- **Triple fallback strategy**: Unity CLI → Original HeadlessTestRunner → Enhanced methods
- **Better Unity detection**: Improved version detection and path resolution
- **Enhanced result parsing**: Supports multiple XML formats
- **Comprehensive logging**: Shows which method succeeded and why others failed

### VS Code Configuration
**Files Created:**
- `.vscode/tasks.json` - Test execution tasks
- `.vscode/launch.json` - Debug configurations
- `.vscode/settings.json` - Project-specific settings
- `.vscode/test.runsettings` - Test runner configuration
- `.vscode/extensions.json` - Recommended extensions

### Standalone Test Runner
**vscode-test-runner.ps1**:
- **Unity-aware**: Uses Unity if available, falls back to dotnet test
- **VS Code optimized**: Designed for IDE integration
- **Flexible execution**: Supports dry-run, filtering, verbose output

## 🔧 Configuration

### Environment Variables
```powershell
# Unity executable path (optional - auto-detected if not set)
$env:UNITY_EXE = "C:\Program Files\Unity\Hub\Editor\6000.2.0f1\Editor\Unity.exe"

# Test platform selection
$env:UNITY_TEST_PLATFORM = "PlayMode"  # EditMode, PlayMode, or All

# Test filtering
$env:UNITY_TEST_FILTER = "TestClass.TestMethod;AnotherTest"

# Category filtering  
$env:UNITY_TEST_CATEGORY = "Integration;Unit;Smoke"

# Additional Unity arguments
$env:UNITY_EXTRA_ARGS = "-stackTraceLogType Full"
```

### VS Code Settings
The `.vscode/settings.json` includes:
- **OmniSharp configuration** for better C# support
- **Unity-specific file associations**
- **Test runner integration**
- **Code lens for tests**
- **Proper exclusions** for Unity-generated files

## 📊 Test Results

### XML Output Format
Enhanced NUnit-compatible XML includes:
- **Test counts**: Total, passed, failed, skipped, inconclusive
- **Timing information**: Start time, end time, duration
- **Detailed results**: Individual test results with messages and stack traces
- **Proper structure**: Compatible with CI/CD systems and test result viewers

### Result Locations
- **Primary**: `debug/TestResults_<mode>_<timestamp>.xml`
- **Logs**: `debug/unity_powershell_test_<timestamp>.log`
- **VS Code**: Integrated terminal output

## 🛡️ Troubleshooting

### Common Issues

**"Unity not found"**
```powershell
# Set Unity path explicitly
$env:UNITY_EXE = "path\to\Unity.exe"
```

**"No XML produced"**
- Check Unity logs in `debug/` folder
- Verify test assemblies are configured correctly
- Use enhanced fallback methods (automatic in new script)

**"Tests not discovered"**
- Verify `.asmdef` files include proper test references
- Check `UNITY_INCLUDE_TESTS` define constraint
- Ensure test classes are public and have `[Test]` or `[UnityTest]` attributes

**VS Code Integration Issues**
- Install recommended extensions
- Verify PowerShell is available in PATH
- Check `.vscode/settings.json` for proper configuration

### Debug Mode
```powershell
# Run with detailed logging
$env:UNITY_EXTRA_ARGS = "-stackTraceLogType Full -logFile debug/detailed.log"
pwsh ./run_tests.ps1

# VS Code dry run
pwsh ./vscode-test-runner.ps1 -DryRun -Verbose
```

## 🎯 Best Practices

### Test Organization
- **Use descriptive test names** that explain what's being tested
- **Group related tests** in the same test class
- **Use categories** for different test types (Unit, Integration, Smoke)
- **Keep tests independent** - don't rely on test execution order

### CI/CD Integration
- **Use the enhanced run_tests.ps1** for reliable XML generation
- **Set appropriate timeouts** for Unity test execution
- **Archive test results** as build artifacts
- **Parse XML results** for build status determination

### Development Workflow
- **Use VS Code tasks** for quick test execution during development
- **Filter tests** when working on specific features
- **Clean up results** regularly to avoid clutter
- **Debug failing tests** using VS Code Unity debugger

## 🧬 Sacred Symbol Preservation

Following the SACRED-SYMBOL-PRESERVATION-MANIFESTO:
- ✅ **No existing code deleted** - only enhanced and extended
- ✅ **Backward compatibility maintained** - original HeadlessTestRunner still works
- ✅ **Additive improvements** - new functionality added alongside existing
- ✅ **Graceful fallbacks** - multiple strategies ensure something always works

## 📚 Reference

### Test Assembly Structure
```
Assets/MetVanDAMN/Authoring/Tests/
├── TinyWalnutGames.MetVD.Authoring.Tests.asmdef
├── SmokeTestSceneSetupTests.cs
├── WfcDeterminismTests.cs
└── [other test files]

Packages/com.tinywalnutgames.metvd.*/Tests/Runtime/
├── TinyWalnutGames.MetVD.*.Tests.asmdef  
└── [package-specific tests]
```

### Command Reference
```powershell
# Basic test execution
pwsh ./run_tests.ps1

# VS Code integrated execution
pwsh ./vscode-test-runner.ps1

# Direct Unity execution
Unity.exe -batchmode -runTests -testPlatform PlayMode -testResults results.xml -quit

# Enhanced Unity execution  
Unity.exe -batchmode -executeMethod TinyWalnutGames.Tools.Editor.HeadlessTestRunner.RunAll -quit
```

This comprehensive testing solution provides both robust headless execution for CI/CD and seamless VS Code integration for development workflows! 🚀