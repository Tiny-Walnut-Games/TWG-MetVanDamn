#!/usr/bin/env python3
"""
Living Dev Agent XP System - Cross-Platform Integration
Ensures XP system works flawlessly on Windows, macOS, and Linux
"""

import os
import sys
import platform
import subprocess
from pathlib import Path
from typing import Dict, List, Tuple, Optional

class CrossPlatformIntegrator:
    """Handles platform-specific XP system integration"""
    
    def __init__(self, workspace_path: str = "."):
        self.workspace_path = Path(workspace_path)
        self.platform = platform.system().lower()
        self.is_windows = self.platform == "windows"
        self.is_macos = self.platform == "darwin"
        self.is_linux = self.platform == "linux"
        
        # Platform-specific settings
        self.python_cmd = self._get_python_command()
        self.shell_type = self._get_shell_type()
        self.encoding = self._get_encoding()

    def _get_python_command(self) -> str:
        """Get the correct Python command for this platform"""
        if self.is_windows:
            # Try python3 first, then python
            for cmd in ["python3", "python"]:
                try:
                    result = subprocess.run([cmd, "--version"], capture_output=True, text=True)
                    if result.returncode == 0 and "3." in result.stdout:
                        return cmd
                except FileNotFoundError:
                    continue
            return "python"  # Fallback
        else:
            return "python3"

    def _get_shell_type(self) -> str:
        """Detect shell type for hook generation"""
        if self.is_windows:
            return "powershell"
        else:
            shell = os.environ.get('SHELL', '/bin/bash')
            if 'zsh' in shell:
                return "zsh"
            elif 'fish' in shell:
                return "fish"
            else:
                return "bash"

    def _get_encoding(self) -> str:
        """Get proper encoding for this platform"""
        if self.is_windows:
            return "utf-8"  # Force UTF-8 on Windows
        else:
            return "utf-8"

    def create_platform_specific_hooks(self) -> bool:
        """Create Git hooks that work on this platform"""
        try:
            git_hooks_dir = self.workspace_path / ".git" / "hooks"
            if not git_hooks_dir.exists():
                print("❌ No .git directory found")
                return False

            # Post-commit hook
            post_commit_content = self._generate_post_commit_hook()
            post_commit_hook = git_hooks_dir / "post-commit"
            
            with open(post_commit_hook, 'w', encoding=self.encoding, newline='\n') as f:
                f.write(post_commit_content)
            
            # Make executable (Unix-like systems)
            if not self.is_windows:
                os.chmod(post_commit_hook, 0o755)
            
            # Pre-push hook
            pre_push_content = self._generate_pre_push_hook()
            pre_push_hook = git_hooks_dir / "pre-push"
            
            with open(pre_push_hook, 'w', encoding=self.encoding, newline='\n') as f:
                f.write(pre_push_content)
            
            if not self.is_windows:
                os.chmod(pre_push_hook, 0o755)

            print(f"✅ Platform-specific Git hooks created for {self.platform}")
            return True

        except Exception as e:
            print(f"❌ Failed to create platform-specific hooks: {e}")
            return False

    def _generate_post_commit_hook(self) -> str:
        """Generate post-commit hook for this platform"""
        if self.is_windows:
            return f'''@echo off
REM Living Dev Agent XP System - Post-commit hook (Windows)
REM Awards XP for commits automatically

{self.python_cmd} "{self.workspace_path}\\template\\src\\DeveloperExperience\\hooks\\git_commit_tracker.py"
'''
        else:
            shebang = "#!/bin/bash" if self.shell_type == "bash" else f"#!/usr/bin/env {self.shell_type}"
            return f'''{shebang}
# Living Dev Agent XP System - Post-commit hook ({self.platform})
# Awards XP for commits automatically

{self.python_cmd} "{self.workspace_path}/template/src/DeveloperExperience/hooks/git_commit_tracker.py"
'''

    def _generate_pre_push_hook(self) -> str:
        """Generate pre-push hook for this platform"""
        if self.is_windows:
            return f'''@echo off
REM Living Dev Agent XP System - Pre-push hook (Windows)
REM Syncs XP data before pushing

{self.python_cmd} "{self.workspace_path}\\template\\src\\DeveloperExperience\\hooks\\team_xp_sync.py"
'''
        else:
            shebang = "#!/bin/bash" if self.shell_type == "bash" else f"#!/usr/bin/env {self.shell_type}"
            return f'''{shebang}
# Living Dev Agent XP System - Pre-push hook ({self.platform})
# Syncs XP data before pushing

{self.python_cmd} "{self.workspace_path}/template/src/DeveloperExperience/hooks/team_xp_sync.py"
'''

    def create_ide_integrations(self) -> Dict[str, bool]:
        """Create integrations for all major IDEs"""
        results = {}
        
        # VS Code (cross-platform)
        results['vscode'] = self._create_vscode_integration()
        
        # Visual Studio (Windows)
        if self.is_windows:
            results['visual_studio'] = self._create_visual_studio_integration()
        
        # JetBrains Rider (cross-platform)
        results['rider'] = self._create_rider_integration()
        
        # Unity Editor (cross-platform)
        results['unity'] = self._create_unity_integration()
        
        # macOS specific
        if self.is_macos:
            results['xcode'] = self._create_xcode_integration()
        
        return results

    def _create_vscode_integration(self) -> bool:
        """Create VS Code integration"""
        try:
            vscode_dir = self.workspace_path / ".vscode"
            vscode_dir.mkdir(exist_ok=True)

            # Tasks with platform-specific commands
            tasks = {
                "version": "2.0.0",
                "tasks": [
                    {
                        "label": "XP: Record Debugging Session",
                        "type": "shell",
                        "command": self.python_cmd,
                        "args": [
                            str(self.workspace_path / "template" / "src" / "DeveloperExperience" / "dev_experience.py"),
                            "--record", "${input:developer_name}", "debugging_session", "${input:quality_level}",
                            "${input:description}", "--files", "${input:files}"
                        ],
                        "group": "build",
                        "presentation": {
                            "echo": True,
                            "reveal": "always",
                            "focus": False,
                            "panel": "shared"
                        }
                    },
                    {
                        "label": "XP: Show My Profile",
                        "type": "shell",
                        "command": self.python_cmd,
                        "args": [
                            str(self.workspace_path / "template" / "src" / "DeveloperExperience" / "dev_experience.py"),
                            "--profile", "${input:developer_name}"
                        ],
                        "group": "build"
                    },
                    {
                        "label": "XP: Daily Bonus",
                        "type": "shell",
                        "command": self.python_cmd,
                        "args": [
                            str(self.workspace_path / "template" / "src" / "DeveloperExperience" / "dev_experience.py"),
                            "--daily-bonus", "${input:developer_name}"
                        ],
                        "group": "build"
                    },
                    {
                        "label": "XP: Spend CopilotCoins",
                        "type": "shell",
                        "command": self.python_cmd,
                        "args": [
                            str(self.workspace_path / "template" / "src" / "DeveloperExperience" / "dev_experience.py"),
                            "--spend", "${input:developer_name}", "${input:coin_amount}", "${input:purchase_description}"
                        ],
                        "group": "build"
                    }
                ],
                "inputs": [
                    {
                        "id": "developer_name",
                        "description": "Developer name",
                        "default": os.environ.get('USER', os.environ.get('USERNAME', 'Developer')),
                        "type": "promptString"
                    },
                    {
                        "id": "quality_level",
                        "description": "Quality level",
                        "type": "pickString",
                        "options": ["legendary", "epic", "excellent", "good", "needs_work"]
                    },
                    {
                        "id": "description",
                        "description": "Description of contribution",
                        "type": "promptString"
                    },
                    {
                        "id": "files",
                        "description": "Files affected (comma-separated)",
                        "type": "promptString"
                    },
                    {
                        "id": "coin_amount",
                        "description": "CopilotCoins to spend",
                        "type": "promptString"
                    },
                    {
                        "id": "purchase_description",
                        "description": "What are you buying?",
                        "type": "promptString"
                    }
                ]
            }

            with open(vscode_dir / "tasks.json", 'w', encoding='utf-8') as f:
                import json
                json.dump(tasks, f, indent=2)

            # Keybindings
            keybindings = [
                {
                    "key": "ctrl+shift+x ctrl+shift+d",
                    "command": "workbench.action.tasks.runTask",
                    "args": "XP: Record Debugging Session"
                },
                {
                    "key": "ctrl+shift+x ctrl+shift+p",
                    "command": "workbench.action.tasks.runTask", 
                    "args": "XP: Show My Profile"
                },
                {
                    "key": "ctrl+shift+x ctrl+shift+b",
                    "command": "workbench.action.tasks.runTask",
                    "args": "XP: Daily Bonus"
                },
                {
                    "key": "ctrl+shift+x ctrl+shift+s",
                    "command": "workbench.action.tasks.runTask",
                    "args": "XP: Spend CopilotCoins"
                }
            ]

            with open(vscode_dir / "keybindings.json", 'w', encoding='utf-8') as f:
                json.dump(keybindings, f, indent=2)

            print("✅ VS Code integration created")
            return True

        except Exception as e:
            print(f"❌ VS Code integration failed: {e}")
            return False

    def _create_rider_integration(self) -> bool:
        """Create JetBrains Rider integration"""
        try:
            # Create external tools configuration for Rider
            idea_dir = self.workspace_path / ".idea"
            idea_dir.mkdir(exist_ok=True)

            external_tools = f'''<?xml version="1.0" encoding="UTF-8"?>
<project version="4">
  <component name="ToolsProvider">
    <tool name="XP: Record Debug Session" description="Record debugging session for XP" showInMainMenu="true" showInEditor="true" showInProject="true" showInSearchPopup="true" disabled="false" useConsole="true" showConsoleOnStdOut="false" showConsoleOnStdErr="false" synchronizeAfterRun="true">
      <exec>
        <option name="COMMAND" value="{self.python_cmd}" />
        <option name="PARAMETERS" value="{self.workspace_path}/template/src/DeveloperExperience/dev_experience.py --record $USER$ debugging_session excellent &quot;Rider debugging session&quot; --metrics &quot;ide:rider&quot;" />
        <option name="WORKING_DIRECTORY" value="$ProjectFileDir$" />
      </exec>
    </tool>
    <tool name="XP: Show Profile" description="Show XP profile" showInMainMenu="true" showInEditor="false" showInProject="false" showInSearchPopup="true" disabled="false" useConsole="true" showConsoleOnStdOut="true" showConsoleOnStdErr="false" synchronizeAfterRun="true">
      <exec>
        <option name="COMMAND" value="{self.python_cmd}" />
        <option name="PARAMETERS" value="{self.workspace_path}/template/src/DeveloperExperience/dev_experience.py --profile $USER$" />
        <option name="WORKING_DIRECTORY" value="$ProjectFileDir$" />
      </exec>
    </tool>
    <tool name="XP: Leaderboard" description="Show team leaderboard" showInMainMenu="true" showInEditor="false" showInProject="false" showInSearchPopup="true" disabled="false" useConsole="true" showConsoleOnStdOut="true" showConsoleOnStdErr="false" synchronizeAfterRun="true">
      <exec>
        <option name="COMMAND" value="{self.python_cmd}" />
        <option name="PARAMETERS" value="{self.workspace_path}/template/src/DeveloperExperience/dev_experience.py --leaderboard" />
        <option name="WORKING_DIRECTORY" value="$ProjectFileDir$" />
      </exec>
    </tool>
  </component>
</project>'''

            with open(idea_dir / "externalTools.xml", 'w', encoding='utf-8') as f:
                f.write(external_tools)

            print("✅ JetBrains Rider integration created")
            return True

        except Exception as e:
            print(f"❌ Rider integration failed: {e}")
            return False

    def _create_visual_studio_integration(self) -> bool:
        """Create Visual Studio integration (Windows only)"""
        try:
            # Create Visual Studio external tools configuration
            vs_dir = self.workspace_path / ".vs"
            vs_dir.mkdir(exist_ok=True)

            # Create a simple batch file for VS integration
            batch_content = f'''@echo off
REM Living Dev Agent XP System - Visual Studio Integration
set PYTHON_CMD={self.python_cmd}
set XP_SCRIPT={self.workspace_path}\\template\\src\\DeveloperExperience\\dev_experience.py

if "%1"=="profile" (
    %PYTHON_CMD% "%XP_SCRIPT%" --profile %USERNAME%
) else if "%1"=="debug" (
    %PYTHON_CMD% "%XP_SCRIPT%" --record %USERNAME% debugging_session excellent "Visual Studio debugging session" --metrics "ide:visual_studio"
) else if "%1"=="leaderboard" (
    %PYTHON_CMD% "%XP_SCRIPT%" --leaderboard
) else (
    echo Usage: xp_vs.bat [profile^|debug^|leaderboard]
)
'''

            with open(self.workspace_path / "xp_vs.bat", 'w', encoding='utf-8') as f:
                f.write(batch_content)

            print("✅ Visual Studio integration created (xp_vs.bat)")
            print("📝 Add to VS: Tools > External Tools > Add:")
            print(f"   Title: XP Profile")
            print(f"   Command: {self.workspace_path}\\xp_vs.bat")
            print(f"   Arguments: profile")
            return True

        except Exception as e:
            print(f"❌ Visual Studio integration failed: {e}")
            return False

    def _create_unity_integration(self) -> bool:
        """Create Unity Editor integration"""
        try:
            unity_dir = self.workspace_path / "Assets" / "Plugins" / "DeveloperExperience" / "Editor"
            unity_dir.mkdir(parents=True, exist_ok=True)

            # Platform-specific Unity script
            unity_script = f'''using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;

namespace DeveloperExperience
{{
    /// <summary>
    /// Cross-platform Unity Editor integration for XP system
    /// Platform: {self.platform}
    /// </summary>
    public class UnityXPIntegration : EditorWindow
    {{
        private string developerName = "{os.environ.get('USER', os.environ.get('USERNAME', 'Developer'))}";
        private string description = "";
        private string contributionType = "code_contribution";
        private string qualityLevel = "good";
        
        private readonly string[] contributionTypes = {{
            "code_contribution", "debugging_session", "documentation", 
            "test_coverage", "refactoring", "architecture"
        }};
        
        private readonly string[] qualityLevels = {{
            "legendary", "epic", "excellent", "good", "needs_work"
        }};
        
        [MenuItem("Tools/Developer Experience/XP Tracker")]
        public static void ShowWindow()
        {{
            GetWindow<UnityXPIntegration>("XP Tracker");
        }}
        
        [MenuItem("Tools/Developer Experience/Record Debug Session")]
        public static void RecordDebugSession()
        {{
            string workspace = Directory.GetCurrentDirectory();
            string pythonScript = Path.Combine(workspace, "template", "src", "DeveloperExperience", "dev_experience.py");
            
            if (File.Exists(pythonScript))
            {{
                string developerName = "{os.environ.get('USER', os.environ.get('USERNAME', 'Developer'))}";
                string args = $"--record \\"{{developerName}}\\" debugging_session excellent \\"Unity debugging session\\" --metrics \\"unity_session:1,platform:{self.platform}\\"";
                RunPythonScript(pythonScript, args);
            }}
            else
            {{
                UnityEngine.Debug.LogError("XP System not found. Make sure the Living Dev Agent template is properly installed.");
            }}
        }}
        
        [MenuItem("Tools/Developer Experience/Show My Profile")]
        public static void ShowProfile()
        {{
            string workspace = Directory.GetCurrentDirectory();
            string pythonScript = Path.Combine(workspace, "template", "src", "DeveloperExperience", "dev_experience.py");
            
            if (File.Exists(pythonScript))
            {{
                string developerName = "{os.environ.get('USER', os.environ.get('USERNAME', 'Developer'))}";
                string args = $"--profile \\"{{developerName}}\\"";
                RunPythonScript(pythonScript, args);
            }}
        }}
        
        [MenuItem("Tools/Developer Experience/Show Leaderboard")]
        public static void ShowLeaderboard()
        {{
            string workspace = Directory.GetCurrentDirectory();
            string pythonScript = Path.Combine(workspace, "template", "src", "DeveloperExperience", "dev_experience.py");
            
            if (File.Exists(pythonScript))
            {{
                RunPythonScript(pythonScript, "--leaderboard");
            }}
        }}
        
        [MenuItem("Tools/Developer Experience/Daily Bonus")]
        public static void DailyBonus()
        {{
            string workspace = Directory.GetCurrentDirectory();
            string pythonScript = Path.Combine(workspace, "template", "src", "DeveloperExperience", "dev_experience.py");
            
            if (File.Exists(pythonScript))
            {{
                string developerName = "{os.environ.get('USER', os.environ.get('USERNAME', 'Developer'))}";
                string args = $"--daily-bonus \\"{{developerName}}\\"";
                RunPythonScript(pythonScript, args);
            }}
        }}
        
        private void OnGUI()
        {{
            GUILayout.Label("Developer Experience Tracker ({self.platform})", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            developerName = EditorGUILayout.TextField("Developer Name", developerName);
            
            int contributionIndex = System.Array.IndexOf(contributionTypes, contributionType);
            contributionIndex = EditorGUILayout.Popup("Contribution Type", contributionIndex, contributionTypes);
            contributionType = contributionTypes[contributionIndex];
            
            int qualityIndex = System.Array.IndexOf(qualityLevels, qualityLevel);
            qualityIndex = EditorGUILayout.Popup("Quality Level", qualityIndex, qualityLevels);
            qualityLevel = qualityLevels[qualityIndex];
            
            description = EditorGUILayout.TextField("Description", description);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Record Contribution"))
            {{
                RecordContribution();
            }}
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Show Profile"))
            {{
                ShowProfile();
            }}
            if (GUILayout.Button("Daily Bonus"))
            {{
                DailyBonus();
            }}
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("Show Leaderboard"))
            {{
                ShowLeaderboard();
            }}
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox($"Running on {{System.Environment.OSVersion.Platform}} with Python: {self.python_cmd}", MessageType.Info);
        }}
        
        private void RecordContribution()
        {{
            if (string.IsNullOrEmpty(developerName) || string.IsNullOrEmpty(description))
            {{
                EditorUtility.DisplayDialog("Error", "Please fill in all fields", "OK");
                return;
            }}
            
            string workspace = Directory.GetCurrentDirectory();
            string pythonScript = Path.Combine(workspace, "template", "src", "DeveloperExperience", "dev_experience.py");
            
            if (File.Exists(pythonScript))
            {{
                string args = $"--record \\"{{developerName}}\\" {{contributionType}} {{qualityLevel}} \\"{{description}}\\" --metrics \\"unity_manual:1,platform:{self.platform}\\"";
                RunPythonScript(pythonScript, args);
            }}
            else
            {{
                EditorUtility.DisplayDialog("Error", "XP System not found.", "OK");
            }}
        }}
        
        private static void RunPythonScript(string scriptPath, string arguments)
        {{
            try
            {{
                ProcessStartInfo startInfo = new ProcessStartInfo
                {{
                    FileName = "{self.python_cmd}",
                    Arguments = $"\\"{{scriptPath}}\\" {{arguments}}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }};
                
                Process process = Process.Start(startInfo);
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                
                process.WaitForExit();
                
                if (!string.IsNullOrEmpty(output))
                {{
                    UnityEngine.Debug.Log($"XP System: {{output}}");
                }}
                
                if (!string.IsNullOrEmpty(error))
                {{
                    UnityEngine.Debug.LogError($"XP System Error: {{error}}");
                }}
            }}
            catch (System.Exception e)
            {{
                UnityEngine.Debug.LogError($"Failed to run XP system: {{e.Message}}");
            }}
        }}
    }}
    
    /// <summary>
    /// Automatic XP tracking for Unity Editor events
    /// </summary>
    [InitializeOnLoad]
    public class UnityXPAutoTracker
    {{
        static UnityXPAutoTracker()
        {{
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }}
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {{
            if (state == PlayModeStateChange.EnteredPlayMode)
            {{
                string workspace = Directory.GetCurrentDirectory();
                string pythonScript = Path.Combine(workspace, "template", "src", "DeveloperExperience", "dev_experience.py");
                
                if (File.Exists(pythonScript))
                {{
                    string developerName = "{os.environ.get('USER', os.environ.get('USERNAME', 'Developer'))}";
                    string args = $"--record \\"{{developerName}}\\" test_coverage good \\"Unity play mode testing\\" --metrics \\"play_mode_test:1,platform:{self.platform}\\"";
                    RunPythonScript(pythonScript, args);
                }}
            }}
        }}
        
        private static void RunPythonScript(string scriptPath, string arguments)
        {{
            try
            {{
                ProcessStartInfo startInfo = new ProcessStartInfo
                {{
                    FileName = "{self.python_cmd}",
                    Arguments = $"\\"{{scriptPath}}\\" {{arguments}}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }};
                
                Process.Start(startInfo);
            }}
            catch (System.Exception e)
            {{
                UnityEngine.Debug.LogError($"Failed to track XP: {{e.Message}}");
            }}
        }}
    }}
}}'''

            with open(unity_dir / "UnityXPIntegration.cs", 'w', encoding='utf-8') as f:
                f.write(unity_script)

            print("✅ Unity Editor integration created")
            return True

        except Exception as e:
            print(f"❌ Unity integration failed: {e}")
            return False

    def _create_xcode_integration(self) -> bool:
        """Create Xcode integration (macOS only)"""
        try:
            # Create Xcode User Scripts directory
            scripts_dir = Path.home() / "Library" / "Developer" / "Xcode" / "UserData" / "CodeSnippets"
            scripts_dir.mkdir(parents=True, exist_ok=True)

            # Create XP tracking script for Xcode
            xcode_script = f'''#!/bin/bash
# Living Dev Agent XP System - Xcode Integration
# Usage: Run this script from Xcode to award XP

WORKSPACE_PATH="{self.workspace_path}"
PYTHON_CMD="{self.python_cmd}"
XP_SCRIPT="$WORKSPACE_PATH/template/src/DeveloperExperience/dev_experience.py"

if [ "$1" = "debug" ]; then
    $PYTHON_CMD "$XP_SCRIPT" --record "$USER" debugging_session excellent "Xcode debugging session" --metrics "ide:xcode"
elif [ "$1" = "profile" ]; then
    $PYTHON_CMD "$XP_SCRIPT" --profile "$USER"
elif [ "$1" = "build" ]; then
    $PYTHON_CMD "$XP_SCRIPT" --record "$USER" code_contribution good "Xcode build completion" --metrics "ide:xcode,build:1"
else
    echo "Usage: $0 [debug|profile|build]"
fi
'''

            script_path = self.workspace_path / "xp_xcode.sh"
            with open(script_path, 'w', encoding='utf-8') as f:
                f.write(xcode_script)
            
            os.chmod(script_path, 0o755)

            print("✅ Xcode integration created (xp_xcode.sh)")
            print("📝 Add to Xcode: Product > Perform Action > Run Custom Script...")
            return True

        except Exception as e:
            print(f"❌ Xcode integration failed: {e}")
            return False

    def setup_cross_platform_integration(self) -> bool:
        """Setup complete cross-platform integration"""
        try:
            print(f"🚀 Setting up XP system for {self.platform}...")
            print(f"   Python command: {self.python_cmd}")
            print(f"   Shell type: {self.shell_type}")
            print(f"   Encoding: {self.encoding}")
            
            # Create platform-specific hooks
            if not self.create_platform_specific_hooks():
                return False
            
            # Create IDE integrations
            ide_results = self.create_ide_integrations()
            
            print("\n🎮 IDE Integration Results:")
            for ide, success in ide_results.items():
                status = "✅" if success else "❌"
                print(f"   {status} {ide.title()}")
            
            # Create platform-specific quick start guide
            self.create_platform_quick_start()
            
            successful_integrations = sum(1 for success in ide_results.values() if success)
            total_integrations = len(ide_results)
            
            print(f"\n🏆 Integration Summary:")
            print(f"   Platform: {self.platform}")
            print(f"   IDE Integrations: {successful_integrations}/{total_integrations}")
            print(f"   Git Hooks: ✅")
            
            return successful_integrations > 0

        except Exception as e:
            print(f"❌ Cross-platform setup failed: {e}")
            return False

    def create_platform_quick_start(self) -> bool:
        """Create platform-specific quick start guide"""
        try:
            platform_guide = f"""# Living Dev Agent XP System - {self.platform.title()} Quick Start

## 🎮 Platform: {self.platform.title()}
**Python Command**: `{self.python_cmd}`
**Shell**: {self.shell_type}

## 🚀 Quick Setup
```bash
# Test XP system
{self.python_cmd} template/src/DeveloperExperience/dev_experience.py --profile {os.environ.get('USER', os.environ.get('USERNAME', 'YourName'))}

# Award yourself setup XP
{self.python_cmd} template/src/DeveloperExperience/dev_experience.py --record "{os.environ.get('USER', os.environ.get('USERNAME', 'YourName'))}" innovation legendary "Setup XP system on {self.platform}" --metrics "platform:{self.platform},setup:1"
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

"""

            if self.is_windows:
                platform_guide += """### Visual Studio
- **Batch file**: `xp_vs.bat profile` to see your XP
- **External Tools**: Add to Tools menu for easy access
- **Commands**: `xp_vs.bat [profile|debug|leaderboard]`

"""

            if self.is_macos:
                platform_guide += """### Xcode
- **Script**: `./xp_xcode.sh debug` for debugging XP
- **Integration**: Product > Perform Action > Run Custom Script
- **Commands**: `./xp_xcode.sh [debug|profile|build]`

"""

            platform_guide += """### JetBrains Rider
- **External Tools**: Tools > External Tools > XP: ...
- **Configuration**: `.idea/externalTools.xml`
- **Usage**: Right-click → External Tools → XP tools

## 🎯 Platform-Specific Features

"""

            if self.is_windows:
                platform_guide += """### Windows Features
- **PowerShell support**: Handles Unicode emojis properly
- **Batch file helpers**: Easy command-line access
- **Git Bash compatibility**: Works with Git for Windows
- **Path handling**: Automatic Windows path conversion

"""

            if self.is_macos:
                platform_guide += """### macOS Features  
- **Zsh/Bash support**: Works with default macOS shells
- **Xcode integration**: Custom script integration
- **Homebrew Python**: Compatible with brew-installed Python
- **Terminal.app**: Full emoji and color support

"""

            if self.is_linux:
                platform_guide += """### Linux Features
- **Distribution agnostic**: Works on Ubuntu, Fedora, Arch, etc.
- **Shell detection**: Auto-detects bash/zsh/fish
- **Package manager neutral**: Works with system or custom Python
- **Desktop integration**: Compatible with GNOME/KDE/etc.

"""

            platform_guide += f"""
## 🧪 Test Your Setup

```bash
# 1. Test basic functionality
{self.python_cmd} template/src/DeveloperExperience/dev_experience.py --help

# 2. Create your first contribution
{self.python_cmd} template/src/DeveloperExperience/dev_experience.py --record "{os.environ.get('USER', os.environ.get('USERNAME', 'YourName'))}" code_contribution good "Testing XP system on {self.platform}" --metrics "platform_test:1"

# 3. Check your profile
{self.python_cmd} template/src/DeveloperExperience/dev_experience.py --profile "{os.environ.get('USER', os.environ.get('USERNAME', 'YourName'))}"

# 4. Test daily bonus
{self.python_cmd} template/src/DeveloperExperience/dev_experience.py --daily-bonus "{os.environ.get('USER', os.environ.get('USERNAME', 'YourName'))}"
```

## 🎮 Next Steps
1. **Make a Git commit** - Git hooks will automatically award XP
2. **Open Unity** - Use Tools > Developer Experience menu
3. **Try VS Code shortcuts** - Ctrl+Shift+X combinations
4. **Check your leaderboard** - See how you rank!

Your XP system is ready for {self.platform}! 🏆
"""

            guide_path = self.workspace_path / f"docs/XP_SYSTEM_QUICKSTART_{self.platform.upper()}.md"
            with open(guide_path, 'w', encoding='utf-8') as f:
                f.write(platform_guide)

            print(f"✅ Platform quick start guide created: {guide_path}")
            return True

        except Exception as e:
            print(f"❌ Failed to create platform guide: {e}")
            return False


def main():
    """Main cross-platform integration setup"""
    import argparse
    
    parser = argparse.ArgumentParser(description="🎮 Cross-Platform XP System Integration")
    parser.add_argument('--workspace', default='.', help='Workspace directory path')
    parser.add_argument('--setup-all', action='store_true', help='Setup all platform integrations')
    parser.add_argument('--test', action='store_true', help='Test platform detection')
    
    args = parser.parse_args()
    
    try:
        integrator = CrossPlatformIntegrator(workspace_path=args.workspace)
        
        if args.test:
            print(f"🖥️ Platform Detection Test:")
            print(f"   System: {integrator.platform}")
            print(f"   Python: {integrator.python_cmd}")
            print(f"   Shell: {integrator.shell_type}")
            print(f"   Encoding: {integrator.encoding}")
            print(f"   Windows: {integrator.is_windows}")
            print(f"   macOS: {integrator.is_macos}")
            print(f"   Linux: {integrator.is_linux}")
        
        elif args.setup_all:
            integrator.setup_cross_platform_integration()
        
        else:
            print("🎮 Living Dev Agent XP System - Cross-Platform Integration")
            print("Use --setup-all to setup all platform integrations")
            print("Use --test to test platform detection")
            
    except KeyboardInterrupt:
        print("\n⚠️ Integration setup interrupted")
    except Exception as e:
        print(f"❌ Integration error: {e}")


if __name__ == "__main__":
    main()
