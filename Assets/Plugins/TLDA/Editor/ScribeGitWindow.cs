#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

namespace LivingDevAgent.Editor.Scribe
    {
    /// <summary>
    /// 🐙 LEGENDARY - GitExtensions-style Git Integration Window
    /// Current: Tab-based Git interface with real estate optimization
    /// Enhancement path: Full GitExtensions UI, diff viewer, branch management
    /// Sacred Vision: "It would make it feel a little like you" - AI-powered Git workflow!
    /// </summary>
    public class ScribeGitWindow : EditorWindow
        {
        // Tab system for real estate optimization
        private int _currentTab = 0;
        private readonly string[] _tabNames = { "📊 Status", "📝 Changes", "🌲 Branches", "📈 History" };

        // Git status data
        private readonly List<GitFileStatus> _modifiedFiles = new();
        private readonly List<GitFileStatus> _stagedFiles = new();
        private string _currentBranch = "main";
        private Vector2 _scrollPosition;
        private bool _refreshing = false;

        // Git operation status tracking
        private string _lastSyncOperation = "";
        private bool _lastSyncSuccess = false;

        // Git command result structure
        private struct GitResult
            {
            public int exitCode;
            public string output;
            public string error;
            }

        public static void ShowWindow()
            {
            ScribeGitWindow window = GetWindow<ScribeGitWindow>("🐙 Git Status");
            window.minSize = new Vector2(400, 300);
            window.Show();
            }

        void OnEnable()
            {
            RefreshGitStatus();
            }

        void OnGUI()
            {
            DrawHeader();
            DrawTabs();
            DrawTabContent();
            DrawFooter();
            }

        void DrawHeader()
            {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                {
                GUILayout.Label($"🌲 {_currentBranch}", EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("🔄 Refresh", EditorStyles.toolbarButton))
                    {
                    RefreshGitStatus();
                    }

                if (GUILayout.Button("📝 Commit", EditorStyles.toolbarButton))
                    {
                    ScribeCommitWindow.ShowWindow();
                    }
                }
            EditorGUILayout.EndHorizontal();
            }

        void DrawTabs()
            {
            _currentTab = GUILayout.Toolbar(_currentTab, _tabNames);
            }

        void DrawTabContent()
            {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                {
                switch (_currentTab)
                    {
                    case 0: DrawStatusTab(); break;
                    case 1: DrawChangesTab(); break;
                    case 2: DrawBranchesTab(); break;
                    case 3: DrawHistoryTab(); break;
                    }
                }
            EditorGUILayout.EndScrollView();
            }

        /// <summary>
        /// 📊 Git Status Overview Tab
        /// Shows modified, staged, and untracked files in compact format
        /// </summary>
        void DrawStatusTab()
            {
            if (_refreshing)
                {
                EditorGUILayout.HelpBox("🔄 Refreshing git status...", MessageType.Info);
                return;
                }

            // Staged files
            if (_stagedFiles.Count > 0)
                {
                GUILayout.Label("📦 Staged Changes", EditorStyles.boldLabel);
                foreach (GitFileStatus file in _stagedFiles)
                    {
                    DrawFileStatus(file, true);
                    }
                EditorGUILayout.Space();
                }

            // Modified files
            if (_modifiedFiles.Count > 0)
                {
                GUILayout.Label("📝 Modified Files", EditorStyles.boldLabel);
                foreach (GitFileStatus file in _modifiedFiles)
                    {
                    DrawFileStatus(file, false);
                    }
                }

            if (_stagedFiles.Count == 0 && _modifiedFiles.Count == 0)
                {
                EditorGUILayout.HelpBox("✨ Working tree clean", MessageType.Info);
                }

            DrawGitActions();
            }

        void DrawFileStatus(GitFileStatus file, bool isStaged)
            {
            EditorGUILayout.BeginHorizontal();
                {
                // Status icon
                GUILayout.Label(GetStatusIcon(file.Status), GUILayout.Width(20));

                // File path (clickable)
                if (GUILayout.Button(file.Path, EditorStyles.linkLabel))
                    {
                    // Open file in editor
                    Object asset = AssetDatabase.LoadAssetAtPath<Object>(file.Path);
                    if (asset != null)
                        {
                        AssetDatabase.OpenAsset(asset);
                        }
                    }

                GUILayout.FlexibleSpace();

                // Stage/Unstage button
                if (isStaged)
                    {
                    if (GUILayout.Button("⬇️ Unstage", GUILayout.Width(70)))
                        {
                        UnstageFile(file.Path);
                        }
                    }
                else
                    {
                    if (GUILayout.Button("⬆️ Stage", GUILayout.Width(70)))
                        {
                        StageFile(file.Path);
                        }
                    }
                }
            EditorGUILayout.EndHorizontal();
            }

        /// <summary>
        /// 📝 Changes Tab - Detailed diff view
        /// Real estate efficient diff display
        /// </summary>
        void DrawChangesTab()
            {
            EditorGUILayout.HelpBox("🔧 Diff viewer coming soon!\nClick files in Status tab to open them.", MessageType.Info);
            }

        /// <summary>
        /// 🌲 Branches Tab - Branch management
        /// Compact branch switching and creation
        /// </summary>
        void DrawBranchesTab()
            {
            EditorGUILayout.HelpBox("🔧 Branch management coming soon!\nUse terminal or GitExtensions for now.", MessageType.Info);
            }

        /// <summary>
        /// 📈 History Tab - Commit history
        /// Compact commit log with essential info
        /// </summary>
        void DrawHistoryTab()
            {
            EditorGUILayout.HelpBox("🔧 Commit history coming soon!\nUse git log or GitExtensions for now.", MessageType.Info);
            }

        void DrawFooter()
            {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                {
                GUILayout.Label($"📁 {_modifiedFiles.Count} modified, 📦 {_stagedFiles.Count} staged", EditorStyles.miniLabel);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("📝 Smart Commit", EditorStyles.toolbarButton))
                    {
                    ScribeCommitWindow.ShowWindow();
                    }
                }
            EditorGUILayout.EndHorizontal();
            }

        void RefreshGitStatus()
            {
            _refreshing = true;

            try
                {
                // Get current branch
                GitResult branchResult = RunGitCommand("branch --show-current");
                if (!string.IsNullOrEmpty(branchResult.output))
                    {
                    _currentBranch = branchResult.output.Trim();
                    }

                // Get status
                GitResult statusResult = RunGitCommand("status --porcelain");
                ParseGitStatus(statusResult.output);
                }
            catch (System.Exception ex)
                {
                Debug.LogWarning($"[Git] Failed to refresh status: {ex.Message}");
                }
            finally
                {
                _refreshing = false;
                Repaint();
                }
            }

        void ParseGitStatus(string statusOutput)
            {
            _modifiedFiles.Clear();
            _stagedFiles.Clear();

            if (string.IsNullOrEmpty(statusOutput))
                return;

            string[] lines = statusOutput.Split('\n');
            foreach (string line in lines)
                {
                if (line.Length < 3) continue;

                char indexStatus = line[0];
                char workTreeStatus = line[1];
                string filePath = line[3..];

                if (indexStatus != ' ' && indexStatus != '?')
                    {
                    _stagedFiles.Add(new GitFileStatus { Path = filePath, Status = indexStatus });
                    }

                if (workTreeStatus != ' ')
                    {
                    _modifiedFiles.Add(new GitFileStatus { Path = filePath, Status = workTreeStatus });
                    }
                }
            }

        string GetStatusIcon(char status)
            {
            return status switch
                {
                    'M' => "📝", // Modified
                    'A' => "➕", // Added
                    'D' => "🗑️", // Deleted
                    'R' => "🔄", // Renamed
                    'C' => "📋", // Copied
                    '?' => "❓", // Untracked
                    _ => "📄"
                    };
            }

        void StageFile(string filePath)
            {
            RunGitCommand($"add \"{filePath}\"");
            RefreshGitStatus();
            }

        void UnstageFile(string filePath)
            {
            RunGitCommand($"reset HEAD \"{filePath}\"");
            RefreshGitStatus();
            }

        /// <summary>
        /// 🔧 ENHANCEMENT READY - Complete Git workflow integration
        /// Current: Basic status display and refresh
        /// Enhancement path: Push, pull, sync, branch management, conflict resolution
        /// Sacred Vision: Full GitExtensions-style interface for complete workflow!
        /// </summary>
        void DrawGitActions()
            {
            EditorGUILayout.Space();
            GUILayout.Label("🐙 Git Operations", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
                {
                // Refresh status
                if (GUILayout.Button("🔄 Refresh"))
                    {
                    RefreshGitStatus();
                    }

                // 🔧 ENHANCEMENT READY - Push functionality
                GUI.enabled = HasCommitsToSync();
                if (GUILayout.Button("⬆️ Push"))
                    {
                    PerformGitPush();
                    }
                GUI.enabled = true;

                // 🔧 ENHANCEMENT READY - Pull functionality
                if (GUILayout.Button("⬇️ Pull"))
                    {
                    PerformGitPull();
                    }

                // 🔧 ENHANCEMENT READY - Sync (pull + push)
                if (GUILayout.Button("🔄 Sync"))
                    {
                    PerformGitSync();
                    }
                }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
                {
                // 🔧 Branch management
                if (GUILayout.Button("🌿 Branches"))
                    {
                    ShowBranchManagement();
                    }

                // 🔧 View history
                if (GUILayout.Button("📜 History"))
                    {
                    ShowGitHistory();
                    }

                // 🔧 Open in external tool
                if (GUILayout.Button("🔧 External"))
                    {
                    OpenInExternalGitTool();
                    }
                }
            EditorGUILayout.EndHorizontal();

            // Show sync status
            if (!string.IsNullOrEmpty(_lastSyncOperation))
                {
                MessageType messageType = _lastSyncSuccess ? MessageType.Info : MessageType.Error;
                EditorGUILayout.HelpBox(_lastSyncOperation, messageType);
                }
            }

        /// <summary>
        /// 🔧 ENHANCEMENT READY - Git push with proper error handling and progress feedback
        /// Current: Complete push implementation with status tracking
        /// Enhancement path: Upstream tracking, force push options, credential management
        /// </summary>
        void PerformGitPush()
            {
            _lastSyncOperation = "Pushing to remote...";
            _lastSyncSuccess = false;

            try
                {
                GitResult result = RunGitCommand("push origin HEAD");

                if (result.exitCode == 0)
                    {
                    _lastSyncOperation = "✅ Push successful";
                    _lastSyncSuccess = true;
                    Debug.Log($"[Git] Push successful: {result.output}");
                    }
                else
                    {
                    _lastSyncOperation = $"❌ Push failed: {result.error}";
                    _lastSyncSuccess = false;
                    Debug.LogError($"[Git] Push failed: {result.error}");
                    }

                RefreshGitStatus();
                }
            catch (System.Exception e)
                {
                _lastSyncOperation = $"❌ Push error: {e.Message}";
                _lastSyncSuccess = false;
                Debug.LogError($"[Git] Push exception: {e.Message}");
                }
            }

        /// <summary>
        /// 🔧 ENHANCEMENT READY - Git pull with merge conflict detection
        /// Current: Basic pull implementation
        /// Enhancement path: Merge conflict resolution, rebase options, stash management
        /// </summary>
        void PerformGitPull()
            {
            _lastSyncOperation = "Pulling from remote...";
            _lastSyncSuccess = false;

            try
                {
                GitResult result = RunGitCommand("pull origin HEAD");

                if (result.exitCode == 0)
                    {
                    _lastSyncOperation = "✅ Pull successful";
                    _lastSyncSuccess = true;
                    Debug.Log($"[Git] Pull successful: {result.output}");
                    }
                else
                    {
                    _lastSyncOperation = $"❌ Pull failed: {result.error}";
                    _lastSyncSuccess = false;
                    Debug.LogError($"[Git] Pull failed: {result.error}");

                    // Check for merge conflicts
                    if (result.error.Contains("CONFLICT") || result.error.Contains("conflict"))
                        {
                        _lastSyncOperation += "\n🔀 Merge conflicts detected - resolve manually";
                        }
                    }

                RefreshGitStatus();
                }
            catch (System.Exception e)
                {
                _lastSyncOperation = $"❌ Pull error: {e.Message}";
                _lastSyncSuccess = false;
                Debug.LogError($"[Git] Pull exception: {e.Message}");
                }
            }

        /// <summary>
        /// 🔧 ENHANCEMENT READY - Intelligent sync operation (pull then push)
        /// Current: Sequential pull + push with proper error handling
        /// Enhancement path: Conflict resolution workflow, automatic stashing
        /// </summary>
        void PerformGitSync()
            {
            _lastSyncOperation = "Syncing with remote...";
            _lastSyncSuccess = false;

            try
                {
                // First, pull changes
                GitResult pullResult = RunGitCommand("pull origin HEAD");

                if (pullResult.exitCode != 0)
                    {
                    _lastSyncOperation = $"❌ Sync failed during pull: {pullResult.error}";
                    _lastSyncSuccess = false;
                    return;
                    }

                // Then, push our changes
                GitResult pushResult = RunGitCommand("push origin HEAD");

                if (pushResult.exitCode == 0)
                    {
                    _lastSyncOperation = "✅ Sync successful";
                    _lastSyncSuccess = true;
                    Debug.Log("[Git] Sync completed successfully");
                    }
                else
                    {
                    _lastSyncOperation = $"❌ Sync failed during push: {pushResult.error}";
                    _lastSyncSuccess = false;
                    Debug.LogError($"[Git] Sync push failed: {pushResult.error}");
                    }

                RefreshGitStatus();
                }
            catch (System.Exception e)
                {
                _lastSyncOperation = $"❌ Sync error: {e.Message}";
                _lastSyncSuccess = false;
                Debug.LogError($"[Git] Sync exception: {e.Message}");
                }
            }

        bool HasCommitsToSync()
            {
            try
                {
                GitResult result = RunGitCommand("rev-list --count @{u}..HEAD");
                if (result.exitCode == 0 && int.TryParse(result.output.Trim(), out int count))
                    {
                    return count > 0;
                    }
                }
            catch
                {
                // If we can't determine, assume there might be commits to push
                return true;
                }

            return false;
            }

        void ShowBranchManagement()
            {
            // 🔧 ENHANCEMENT READY - Branch management window
            Debug.Log("🔧 Future: Branch management window");
            }

        void ShowGitHistory()
            {
            try
                {
                GitResult result = RunGitCommand("log --oneline -10");
                if (result.exitCode == 0)
                    {
                    Debug.Log($"[Git] Recent history:\n{result.output}");
                    _lastSyncOperation = "📜 History displayed in console";
                    _lastSyncSuccess = true;
                    }
                }
            catch (System.Exception e)
                {
                _lastSyncOperation = $"❌ Failed to get history: {e.Message}";
                _lastSyncSuccess = false;
                }
            }

        void OpenInExternalGitTool()
            {
            try
                {
                string projectPath = System.IO.Path.GetDirectoryName(Application.dataPath);

                // Try common Git tools
                string[] gitTools = new[]
                {
                    "github", // GitHub Desktop
                    "sourcetree", // SourceTree
                    "gitextensions", // Git Extensions
                };

                foreach (string tool in gitTools)
                    {
                    try
                        {
                        var startInfo = new System.Diagnostics.ProcessStartInfo
                            {
                            FileName = tool,
                            Arguments = $"\"{projectPath}\"",
                            UseShellExecute = true
                            };
                        System.Diagnostics.Process.Start(startInfo);
                        _lastSyncOperation = $"✅ Opened in {tool}";
                        _lastSyncSuccess = true;
                        return;
                        }
                    catch
                        {
                        continue;
                        }
                    }

                // Fallback: open folder in explorer/finder
                EditorUtility.RevealInFinder(projectPath);
                _lastSyncOperation = "📂 Opened project folder";
                _lastSyncSuccess = true;
                }
            catch (System.Exception e)
                {
                _lastSyncOperation = $"❌ Failed to open external tool: {e.Message}";
                _lastSyncSuccess = false;
                }
            }

        /// <summary>
        /// 🔧 ENHANCEMENT READY - Enhanced git command execution with detailed result tracking
        /// Current: Returns structured result with exit code, output, and error details
        /// Enhancement path: Timeout handling, credential management, progress tracking
        /// </summary>
        GitResult RunGitCommand(string arguments)
            {
            var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = System.IO.Path.GetDirectoryName(Application.dataPath),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
                };

            try
                {
                using var process = System.Diagnostics.Process.Start(startInfo);

                // Wait for completion with timeout
                if (!process.WaitForExit(30000)) // 30 second timeout
                    {
                    process.Kill();
                    return new GitResult
                        {
                        exitCode = -1,
                        output = "",
                        error = "Git command timed out after 30 seconds"
                        };
                    }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                return new GitResult
                    {
                    exitCode = process.ExitCode,
                    output = output,
                    error = error
                    };
                }
            catch (System.Exception e)
                {
                return new GitResult
                    {
                    exitCode = -1,
                    output = "",
                    error = $"Failed to execute git command: {e.Message}"
                    };
                }
            }
        }

    [System.Serializable]
    public class GitFileStatus
        {
        public string Path;
        public char Status;
        }
    }
#endif
