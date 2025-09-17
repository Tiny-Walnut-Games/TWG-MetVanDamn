#!/usr/bin/env python3
"""
Living Dev Agent Template - Development Environment Integration
Jerry's XP system hooks for Unity, IDEs, and team workflows

Real-time contribution tracking from actual development activities
"""

import os
import sys
import json
import subprocess
import datetime
from pathlib import Path
from typing import List, Dict, Any, Optional
import re
import hashlib

# Import our XP system
sys.path.append(str(Path(__file__).parent))
from dev_experience import DeveloperExperienceManager, ContributionType, QualityLevel

class DevelopmentIntegrationHooks:
    """Integration hooks for real development environments"""
    
    def __init__(self, workspace_path: str = "."):
        self.workspace_path = Path(workspace_path)
        self.xp_manager = DeveloperExperienceManager(workspace_path)
        
        # Integration directories
        self.hooks_dir = self.workspace_path / "experience" / "hooks"
        self.hooks_dir.mkdir(parents=True, exist_ok=True)
        
        # IDE integration files
        self.vscode_dir = self.workspace_path / ".vscode"
        self.vscode_dir.mkdir(exist_ok=True)
        
        # Unity integration
        self.unity_editor_dir = self.workspace_path / "Assets" / "Plugins" / "DeveloperExperience"
        self.unity_editor_dir.mkdir(parents=True, exist_ok=True)

    def setup_git_hooks(self) -> bool:
        """Setup Git hooks for automatic XP tracking"""
        try:
            git_hooks_dir = self.workspace_path / ".git" / "hooks"
            if not git_hooks_dir.exists():
                print("❌ No .git directory found - not a Git repository")
                return False
            
            # Post-commit hook for XP awards
            post_commit_hook = git_hooks_dir / "post-commit"
            post_commit_content = f'''#!/bin/bash
# Living Dev Agent XP System - Post-commit hook
# Awards XP for commits automatically

python3 "{self.workspace_path}/experience/hooks/git_commit_tracker.py" "$@"
'''
            
            with open(post_commit_hook, 'w') as f:
                f.write(post_commit_content)
            
            # Make executable
            os.chmod(post_commit_hook, 0o755)
            
            # Pre-push hook for team XP sync
            pre_push_hook = git_hooks_dir / "pre-push"
            pre_push_content = f'''#!/bin/bash
# Living Dev Agent XP System - Pre-push hook
# Syncs XP data before pushing

python3 "{self.workspace_path}/experience/hooks/team_xp_sync.py" "$@"
'''
            
            with open(pre_push_hook, 'w') as f:
                f.write(pre_push_content)
            
            os.chmod(pre_push_hook, 0o755)
            
            print("✅ Git hooks installed for automatic XP tracking")
            return True
            
        except Exception as e:
            print(f"❌ Failed to setup Git hooks: {e}")
            return False

    def create_git_commit_tracker(self) -> bool:
        """Create the Git commit tracking script"""
        try:
            tracker_script = self.hooks_dir / "git_commit_tracker.py"
            
            content = '''#!/usr/bin/env python3
"""
Git commit tracker for XP system
Analyzes commits and awards XP automatically
"""

import sys
import subprocess
import json
import re
from pathlib import Path

# Add parent directory to path for imports
sys.path.append(str(Path(__file__).parent.parent))
from dev_experience import DeveloperExperienceManager, ContributionType, QualityLevel

def get_commit_info():
    """Get information about the latest commit"""
    try:
        # Get commit hash
        commit_hash = subprocess.check_output(['git', 'rev-parse', 'HEAD']).decode().strip()
        
        # Get commit message
        commit_msg = subprocess.check_output(['git', 'log', '-1', '--pretty=%B']).decode().strip()
        
        # Get author
        author = subprocess.check_output(['git', 'log', '-1', '--pretty=%an']).decode().strip()
        
        # Get changed files
        changed_files = subprocess.check_output([
            'git', 'diff-tree', '--no-commit-id', '--name-only', '-r', commit_hash
        ]).decode().strip().split('\\n')
        
        # Get diff stats
        diff_stats = subprocess.check_output([
            'git', 'diff-tree', '--no-commit-id', '--numstat', '-r', commit_hash
        ]).decode().strip()
        
        return {
            'hash': commit_hash,
            'message': commit_msg,
            'author': author,
            'files': [f for f in changed_files if f],
            'diff_stats': diff_stats
        }
        
    except Exception as e:
        print(f"Error getting commit info: {e}")
        return None

def analyze_commit_quality(commit_info):
    """Analyze commit quality and determine XP award"""
    if not commit_info:
        return ContributionType.CODE_CONTRIBUTION, QualityLevel.GOOD, {}
    
    message = commit_info['message'].lower()
    files = commit_info['files']
    
    # Analyze contribution type
    contrib_type = ContributionType.CODE_CONTRIBUTION
    
    if any(keyword in message for keyword in ['fix', 'bug', 'debug', 'fuck']):
        contrib_type = ContributionType.DEBUGGING_SESSION
    elif any(keyword in message for keyword in ['doc', 'readme', 'comment']):
        contrib_type = ContributionType.DOCUMENTATION
    elif any(keyword in message for keyword in ['test', 'spec', 'coverage']):
        contrib_type = ContributionType.TEST_COVERAGE
    elif any(keyword in message for keyword in ['refactor', 'clean', 'optimize']):
        contrib_type = ContributionType.REFACTORING
    elif any(keyword in message for keyword in ['architecture', 'design', 'system']):
        contrib_type = ContributionType.ARCHITECTURE
    elif any(keyword in message for keyword in ['review', 'feedback']):
        contrib_type = ContributionType.CODE_REVIEW
    
    # Analyze quality level
    quality = QualityLevel.GOOD  # Default
    
    # Check for quality indicators
    if len(message) > 100:  # Detailed commit message
        quality = QualityLevel.EXCELLENT
    
    if any(keyword in message for keyword in ['legendary', 'epic', 'amazing', 'breakthrough']):
        quality = QualityLevel.LEGENDARY
    elif any(keyword in message for keyword in ['major', 'significant', 'important']):
        quality = QualityLevel.EPIC
    elif any(keyword in message for keyword in ['minor', 'small', 'quick']):
        quality = QualityLevel.GOOD
    elif any(keyword in message for keyword in ['wip', 'temp', 'hack', 'broken']):
        quality = QualityLevel.NEEDS_WORK
    
    # File-based quality assessment
    if len(files) > 10:  # Large commit
        if quality == QualityLevel.GOOD:
            quality = QualityLevel.EXCELLENT
    
    # Calculate metrics
    metrics = {
        'files_changed': len(files),
        'commit_hash': commit_info['hash'][:8],
        'message_length': len(commit_info['message'])
    }
    
    # Parse diff stats for line counts
    if commit_info['diff_stats']:
        total_additions = 0
        total_deletions = 0
        for line in commit_info['diff_stats'].split('\\n'):
            if '\\t' in line:
                parts = line.split('\\t')
                if len(parts) >= 2:
                    try:
                        additions = int(parts[0]) if parts[0] != '-' else 0
                        deletions = int(parts[1]) if parts[1] != '-' else 0
                        total_additions += additions
                        total_deletions += deletions
                    except ValueError:
                        pass
        
        metrics['lines_added'] = total_additions
        metrics['lines_deleted'] = total_deletions
        metrics['net_lines'] = total_additions - total_deletions
    
    return contrib_type, quality, metrics

def main():
    """Main commit tracking function"""
    try:
        # Get workspace path
        workspace_path = Path.cwd()
        
        # Get commit information
        commit_info = get_commit_info()
        if not commit_info:
            print("❌ Could not get commit information")
            return
        
        # Analyze commit
        contrib_type, quality, metrics = analyze_commit_quality(commit_info)
        
        # Create XP manager and record contribution
        xp_manager = DeveloperExperienceManager(str(workspace_path))
        
        contribution_id = xp_manager.record_contribution(
            developer_name=commit_info['author'],
            contribution_type=contrib_type,
            quality_level=quality,
            description=f"Git commit: {commit_info['message'][:100]}...",
            files_affected=commit_info['files'],
            metrics=metrics
        )
        
        if contribution_id:
            print(f"🎉 XP awarded for commit {commit_info['hash'][:8]} by {commit_info['author']}")
        
    except Exception as e:
        print(f"❌ Commit tracking error: {e}")

if __name__ == "__main__":
    main()
'''
            
            with open(tracker_script, 'w') as f:
                f.write(content)
            
            os.chmod(tracker_script, 0o755)
            print("✅ Git commit tracker created")
            return True
            
        except Exception as e:
            print(f"❌ Failed to create Git commit tracker: {e}")
            return False

    def create_vscode_integration(self) -> bool:
        """Create VS Code integration for XP tracking"""
        try:
            # Tasks for XP operations
            tasks_json = {
                "version": "2.0.0",
                "tasks": [
                    {
                        "label": "XP: Record Debugging Session",
                        "type": "shell",
                        "command": "python3",
                        "args": [
                            "${workspaceFolder}/src/DeveloperExperience/dev_experience.py",
                            "--record", "${input:developer_name}", "debugging_session", "${input:quality_level}",
                            "${input:description}", "--files", "${input:files}"
                        ],
                        "group": "build",
                        "presentation": {
                            "echo": True,
                            "reveal": "always",
                            "focus": false,
                            "panel": "shared"
                        }
                    },
                    {
                        "label": "XP: Show My Profile",
                        "type": "shell",
                        "command": "python3",
                        "args": [
                            "${workspaceFolder}/src/DeveloperExperience/dev_experience.py",
                            "--profile", "${input:developer_name}"
                        ],
                        "group": "build"
                    },
                    {
                        "label": "XP: Show Leaderboard",
                        "type": "shell",
                        "command": "python3",
                        "args": [
                            "${workspaceFolder}/src/DeveloperExperience/dev_experience.py",
                            "--leaderboard"
                        ],
                        "group": "build"
                    },
                    {
                        "label": "XP: Spend CopilotCoins",
                        "type": "shell",
                        "command": "python3",
                        "args": [
                            "${workspaceFolder}/src/DeveloperExperience/dev_experience.py",
                            "--spend", "${input:developer_name}", "${input:coin_amount}", "${input:purchase_description}"
                        ],
                        "group": "build"
                    }
                ],
                "inputs": [
                    {
                        "id": "developer_name",
                        "description": "Developer name",
                        "default": "YourName",
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
            
            tasks_file = self.vscode_dir / "tasks.json"
            with open(tasks_file, 'w') as f:
                json.dump(tasks_json, f, indent=2)
            
            # Keybindings for quick XP operations
            keybindings_json = [
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
                    "key": "ctrl+shift+x ctrl+shift+l",
                    "command": "workbench.action.tasks.runTask",
                    "args": "XP: Show Leaderboard"
                },
                {
                    "key": "ctrl+shift+x ctrl+shift+s",
                    "command": "workbench.action.tasks.runTask",
                    "args": "XP: Spend CopilotCoins"
                }
            ]
            
            keybindings_file = self.vscode_dir / "keybindings.json"
            with open(keybindings_file, 'w') as f:
                json.dump(keybindings_json, f, indent=2)
            
            print("✅ VS Code integration created")
            print("🎮 Keyboard shortcuts:")
            print("  Ctrl+Shift+X, Ctrl+Shift+D - Record debugging session")
            print("  Ctrl+Shift+X, Ctrl+Shift+P - Show profile")
            print("  Ctrl+Shift+X, Ctrl+Shift+L - Show leaderboard")
            print("  Ctrl+Shift+X, Ctrl+Shift+S - Spend CopilotCoins")
            
            return True
            
        except Exception as e:
            print(f"❌ Failed to create VS Code integration: {e}")
            return False

    def create_unity_integration(self) -> bool:
        """Create Unity Editor integration for XP tracking"""
        try:
            # Unity Editor script for XP integration
            unity_script_content = '''using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;

namespace DeveloperExperience
{
    /// <summary>
    /// Unity Editor integration for Jerry's XP system
    /// Awards XP for Unity-specific development activities
    /// </summary>
    public class UnityXPIntegration : EditorWindow
    {
        private string developerName = "YourName";
        private string description = "";
        private string contributionType = "code_contribution";
        private string qualityLevel = "good";
        
        private readonly string[] contributionTypes = {
            "code_contribution", "debugging_session", "documentation", 
            "test_coverage", "refactoring", "architecture"
        };
        
        private readonly string[] qualityLevels = {
            "legendary", "epic", "excellent", "good", "needs_work"
        };
        
        [MenuItem("Tools/Developer Experience/XP Tracker")]
        public static void ShowWindow()
        {
            GetWindow<UnityXPIntegration>("XP Tracker");
        }
        
        [MenuItem("Tools/Developer Experience/Record Debug Session")]
        public static void RecordDebugSession()
        {
            string workspace = Directory.GetCurrentDirectory();
            string pythonScript = Path.Combine(workspace, "src", "DeveloperExperience", "dev_experience.py");
            
            if (File.Exists(pythonScript))
            {
                string args = $"--record \\"{System.Environment.UserName}\\" debugging_session excellent \\"Unity debugging session\\" --metrics \\"unity_session:1\\"";
                RunPythonScript(pythonScript, args);
            }
            else
            {
                UnityEngine.Debug.LogError("XP System not found. Make sure the Living Dev Agent template is properly installed.");
            }
        }
        
        [MenuItem("Tools/Developer Experience/Show My Profile")]
        public static void ShowProfile()
        {
            string workspace = Directory.GetCurrentDirectory();
            string pythonScript = Path.Combine(workspace, "src", "DeveloperExperience", "dev_experience.py");
            
            if (File.Exists(pythonScript))
            {
                string args = $"--profile \\"{System.Environment.UserName}\\"";
                RunPythonScript(pythonScript, args);
            }
        }
        
        [MenuItem("Tools/Developer Experience/Show Leaderboard")]
        public static void ShowLeaderboard()
        {
            string workspace = Directory.GetCurrentDirectory();
            string pythonScript = Path.Combine(workspace, "src", "DeveloperExperience", "dev_experience.py");
            
            if (File.Exists(pythonScript))
            {
                RunPythonScript(pythonScript, "--leaderboard");
            }
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Developer Experience Tracker", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            developerName = EditorGUILayout.TextField("Developer Name", developerName);
            
            // Contribution type dropdown
            int contributionIndex = System.Array.IndexOf(contributionTypes, contributionType);
            contributionIndex = EditorGUILayout.Popup("Contribution Type", contributionIndex, contributionTypes);
            contributionType = contributionTypes[contributionIndex];
            
            // Quality level dropdown
            int qualityIndex = System.Array.IndexOf(qualityLevels, qualityLevel);
            qualityIndex = EditorGUILayout.Popup("Quality Level", qualityIndex, qualityLevels);
            qualityLevel = qualityLevels[qualityIndex];
            
            description = EditorGUILayout.TextField("Description", description);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Record Contribution"))
            {
                RecordContribution();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Show My Profile"))
            {
                ShowProfile();
            }
            
            if (GUILayout.Button("Show Leaderboard"))
            {
                ShowLeaderboard();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("XP is automatically awarded for commits via Git hooks. Use this window for manual tracking of Unity-specific contributions.", MessageType.Info);
        }
        
        private void RecordContribution()
        {
            if (string.IsNullOrEmpty(developerName) || string.IsNullOrEmpty(description))
            {
                EditorUtility.DisplayDialog("Error", "Please fill in all fields", "OK");
                return;
            }
            
            string workspace = Directory.GetCurrentDirectory();
            string pythonScript = Path.Combine(workspace, "src", "DeveloperExperience", "dev_experience.py");
            
            if (File.Exists(pythonScript))
            {
                string args = $"--record \\"{developerName}\\" {contributionType} {qualityLevel} \\"{description}\\" --metrics \\"unity_manual:1\\"";
                RunPythonScript(pythonScript, args);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "XP System not found. Make sure the Living Dev Agent template is properly installed.", "OK");
            }
        }
        
        private static void RunPythonScript(string scriptPath, string arguments)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = $"\\"{scriptPath}\\" {arguments}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                Process process = Process.Start(startInfo);
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                
                process.WaitForExit();
                
                if (!string.IsNullOrEmpty(output))
                {
                    UnityEngine.Debug.Log($"XP System: {output}");
                }
                
                if (!string.IsNullOrEmpty(error))
                {
                    UnityEngine.Debug.LogError($"XP System Error: {error}");
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to run XP system: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Automatic XP tracking for Unity Editor events
    /// </summary>
    [InitializeOnLoad]
    public class UnityXPAutoTracker
    {
        static UnityXPAutoTracker()
        {
            // Track play mode sessions
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            // Track build completion
            BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuildPlayer);
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                // Award XP for testing/validation when entering play mode
                string workspace = Directory.GetCurrentDirectory();
                string pythonScript = Path.Combine(workspace, "src", "DeveloperExperience", "dev_experience.py");
                
                if (File.Exists(pythonScript))
                {
                    string args = $"--record \\"{System.Environment.UserName}\\" test_coverage good \\"Unity play mode testing\\" --metrics \\"play_mode_test:1\\"";
                    RunPythonScript(pythonScript, args);
                }
            }
        }
        
        private static void OnBuildPlayer(BuildPlayerOptions options)
        {
            // Award XP for successful builds
            string workspace = Directory.GetCurrentDirectory();
            string pythonScript = Path.Combine(workspace, "src", "DeveloperExperience", "dev_experience.py");
            
            if (File.Exists(pythonScript))
            {
                string args = $"--record \\"{System.Environment.UserName}\\" code_contribution excellent \\"Unity build completion\\" --metrics \\"unity_build:1,target:{options.target}\\"";
                RunPythonScript(pythonScript, args);
            }
        }
        
        private static void RunPythonScript(string scriptPath, string arguments)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = $"\\"{scriptPath}\\" {arguments}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                Process.Start(startInfo);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to track XP: {e.Message}");
            }
        }
    }
}
'''
            
            unity_script_file = self.unity_editor_dir / "UnityXPIntegration.cs"
            with open(unity_script_file, 'w') as f:
                f.write(unity_script_content)
            
            print("✅ Unity Editor integration created")
            print("🎮 Unity menu items:")
            print("  Tools > Developer Experience > XP Tracker")
            print("  Tools > Developer Experience > Record Debug Session")
            print("  Tools > Developer Experience > Show My Profile")
            print("  Tools > Developer Experience > Show Leaderboard")
            
            return True
            
        except Exception as e:
            print(f"❌ Failed to create Unity integration: {e}")
            return False

    def create_team_sync_script(self) -> bool:
        """Create team XP synchronization script"""
        try:
            sync_script = self.hooks_dir / "team_xp_sync.py"
            
            content = '''#!/usr/bin/env python3
"""
Team XP synchronization for shared repositories
Merges and syncs XP data across team members
"""

import sys
import json
import subprocess
from pathlib import Path
from datetime import datetime

# Add parent directory to path for imports
sys.path.append(str(Path(__file__).parent.parent))
from dev_experience import DeveloperExperienceManager

def sync_team_xp():
    """Sync XP data before pushing to shared repository"""
    try:
        workspace_path = Path.cwd()
        xp_manager = DeveloperExperienceManager(str(workspace_path))
        
        # Check if there are XP files to sync
        experience_dir = workspace_path / "experience"
        if not experience_dir.exists():
            return True
        
        profiles_file = experience_dir / "developer_profiles.json"
        if not profiles_file.exists():
            return True
        
        # Add XP files to git if they exist
        try:
            subprocess.run(['git', 'add', str(experience_dir)], check=True, capture_output=True)
            print("✅ XP data staged for commit")
        except subprocess.CalledProcessError:
            print("⚠️ Could not stage XP data - continuing anyway")
        
        # Create summary of local XP state
        total_developers = len(xp_manager.developer_profiles)
        total_contributions = sum(len(p.contributions) for p in xp_manager.developer_profiles.values())
        
        if total_developers > 0:
            print(f"🎮 Syncing XP data: {total_developers} developers, {total_contributions} contributions")
        
        return True
        
    except Exception as e:
        print(f"❌ Team XP sync error: {e}")
        return True  # Don't block pushes on XP sync failures

def main():
    """Main team sync function"""
    sync_team_xp()

if __name__ == "__main__":
    main()
'''
            
            with open(sync_script, 'w') as f:
                f.write(content)
            
            os.chmod(sync_script, 0o755)
            print("✅ Team XP sync script created")
            return True
            
        except Exception as e:
            print(f"❌ Failed to create team sync script: {e}")
            return False

    def setup_all_integrations(self) -> bool:
        """Setup all development environment integrations"""
        try:
            print("🚀 Setting up Living Dev Agent XP System integrations...")
            
            success = True
            
            # Git hooks
            if not self.setup_git_hooks():
                success = False
            
            # Git commit tracker
            if not self.create_git_commit_tracker():
                success = False
            
            # Team sync script
            if not self.create_team_sync_script():
                success = False
            
            # VS Code integration
            if not self.create_vscode_integration():
                success = False
            
            # Unity integration
            if not self.create_unity_integration():
                success = False
            
            if success:
                print("\n🎉 ALL INTEGRATIONS SETUP SUCCESSFULLY!")
                print("\n🎮 Your XP system is now integrated with:")
                print("  ✅ Git (automatic commit tracking)")
                print("  ✅ VS Code (keyboard shortcuts)")
                print("  ✅ Unity Editor (menu items)")
                print("  ✅ Team sync (pre-push hooks)")
                print("\n🏆 Start earning XP with every commit, debug session, and contribution!")
                print("🪙 Spend CopilotCoins on premium Copilot features!")
                
                # Award setup achievement
                self.xp_manager.record_contribution(
                    developer_name=os.environ.get('USER', 'Developer'),
                    contribution_type=ContributionType.INNOVATION,
                    quality_level=QualityLevel.LEGENDARY,
                    description="Setup Living Dev Agent XP System integrations",
                    files_affected=[],
                    metrics={'integration_setup': True, 'systems_count': 4}
                )
                
            else:
                print("\n⚠️ Some integrations failed - check errors above")
            
            return success
            
        except Exception as e:
            print(f"❌ Integration setup failed: {e}")
            return False


def main():
    """Setup development environment integrations"""
    import argparse
    
    parser = argparse.ArgumentParser(description="🎮 Development Environment Integration Setup")
    parser.add_argument('--workspace', default='.', help='Workspace directory path')
    parser.add_argument('--setup-all', action='store_true', help='Setup all integrations')
    parser.add_argument('--git-hooks', action='store_true', help='Setup Git hooks only')
    parser.add_argument('--vscode', action='store_true', help='Setup VS Code integration only')
    parser.add_argument('--unity', action='store_true', help='Setup Unity integration only')
    
    args = parser.parse_args()
    
    try:
        integrator = DevelopmentIntegrationHooks(workspace_path=args.workspace)
        
        if args.setup_all:
            integrator.setup_all_integrations()
        elif args.git_hooks:
            integrator.setup_git_hooks()
            integrator.create_git_commit_tracker()
            integrator.create_team_sync_script()
        elif args.vscode:
            integrator.create_vscode_integration()
        elif args.unity:
            integrator.create_unity_integration()
        else:
            print("🎮 Living Dev Agent XP System Integration")
            print("Use --setup-all to setup all integrations")
            print("Or use specific flags: --git-hooks, --vscode, --unity")
            
    except KeyboardInterrupt:
        print("\n⚠️ Integration setup interrupted")
    except Exception as e:
        print(f"❌ Integration error: {e}")


if __name__ == "__main__":
    main()
