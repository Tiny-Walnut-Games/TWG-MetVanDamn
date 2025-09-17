#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Diagnostics;
using Degbu = UnityEngine.Debug;
using System.Text.RegularExpressions;

namespace LivingDevAgent.Editor.Scribe
    {
    /// <summary>
    /// 📝 REVOLUTIONARY - Warbler-Powered Smart Commit Window
    /// Current: Compact commit interface with AI message generation
    /// Enhancement path: Full warbler integration, semantic versioning, auto-staging
    /// Sacred Vision: Warbler syncing for commit messages that feel like AI collaboration!
    /// </summary>
    public class ScribeCommitWindow : EditorWindow
        {
        // Commit data
        private string _commitMessage = "";
        private string _commitDescription = "";
        private bool _includeAllChanges = false;
        private readonly List<string> _selectedFiles = new();

        // AI Integration
        private bool _useWarblerSuggestion = true;
        private string _suggestedMessage = "";
        private bool _generatingMessage = false;

        // Visual confirmation system
        private string _lastOperationStatus = "";
        private bool _lastOperationSuccess = false;
        private (string branch, string lastCommit) _gitInfo;

        // Reference to ScribeCore data
        private ScribeDataManager _dataManager;
        private ScribeFileOperations _fileOps;

        public static void ShowWindow()
            {
            ScribeCommitWindow window = GetWindow<ScribeCommitWindow>("📝 Smart Commit");
            window.minSize = new Vector2(400, 350);
            window.Show();
            }

        public void Initialize(ScribeDataManager dataManager, ScribeFileOperations fileOps)
            {
            _dataManager = dataManager;
            _fileOps = fileOps;

            // Initialize git status on startup
            RefreshGitStatus();

            if (_useWarblerSuggestion)
                {
                GenerateWarblerSuggestion();
                }
            }

        void OnGUI()
            {
            DrawHeader();
            DrawMessageSection();
            DrawFileSelection();
            DrawAIIntegration();
            DrawCommitButtons();
            }

        void DrawHeader()
            {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                {
                GUILayout.Label("📝 Smart Commit", EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("🐙 Git Status", EditorStyles.toolbarButton))
                    {
                    ScribeGitWindow.ShowWindow();
                    }
                }
            EditorGUILayout.EndHorizontal();
            }

        void DrawMessageSection()
            {
            GUILayout.Label("💬 Commit Message", EditorStyles.boldLabel);

            // AI suggestion display
            if (!string.IsNullOrEmpty(_suggestedMessage))
                {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                    EditorGUILayout.BeginHorizontal();
                        {
                        GUILayout.Label("🤖 Warbler Suggestion:", EditorStyles.miniLabel);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Use", GUILayout.Width(50)))
                            {
                            _commitMessage = _suggestedMessage;
                            }
                        }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.SelectableLabel(_suggestedMessage, EditorStyles.wordWrappedMiniLabel);
                    }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                }

            // Manual message input
            GUILayout.Label("Subject (50 chars max):");
            _commitMessage = EditorGUILayout.TextField(_commitMessage);

            // Character count indicator
            int charCount = _commitMessage.Length;
            GUIStyle style = charCount > 50 ? EditorStyles.boldLabel : EditorStyles.miniLabel;
            Color color = charCount > 50 ? Color.red : Color.gray;

            Color oldColor = GUI.color;
            GUI.color = color;
            GUILayout.Label($"{charCount}/50 characters", style);
            GUI.color = oldColor;

            EditorGUILayout.Space();

            // Description
            GUILayout.Label("Description (optional):");
            _commitDescription = EditorGUILayout.TextArea(_commitDescription, GUILayout.Height(60));
            }

        void DrawFileSelection()
            {
            EditorGUILayout.Space();
            GUILayout.Label("📁 Files to Commit", EditorStyles.boldLabel);

            _includeAllChanges = EditorGUILayout.Toggle("📦 Include all modified files", _includeAllChanges);

            if (!_includeAllChanges)
                {
                EditorGUILayout.HelpBox("🔧 Individual file selection coming soon!\nUse 'Include all' or Git Status window for now.", MessageType.Info);
                }
            }

        void DrawAIIntegration()
            {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                GUILayout.Label("🤖 AI Integration", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                    {
                    _useWarblerSuggestion = EditorGUILayout.Toggle("Use Warbler suggestions", _useWarblerSuggestion);

                    GUI.enabled = !_generatingMessage;
                    if (GUILayout.Button("🔄 Regenerate", GUILayout.Width(100)))
                        {
                        GenerateWarblerSuggestion();
                        }
                    GUI.enabled = true;
                    }
                EditorGUILayout.EndHorizontal();

                if (_generatingMessage)
                    {
                    EditorGUILayout.HelpBox("🧠 Warbler is analyzing your changes...", MessageType.Info);
                    }

                // Context from current TLDL entry
                if (_dataManager != null && !string.IsNullOrEmpty(_dataManager.Title))
                    {
                    EditorGUILayout.Space();
                    GUILayout.Label("📜 TLDL Context:", EditorStyles.miniLabel);
                    EditorGUILayout.SelectableLabel($"Title: {_dataManager.Title}", EditorStyles.wordWrappedMiniLabel);

                    if (!string.IsNullOrEmpty(_dataManager.Summary))
                        {
                        EditorGUILayout.SelectableLabel($"Summary: {_dataManager.Summary}", EditorStyles.wordWrappedMiniLabel);
                        }
                    }
                }
            EditorGUILayout.EndVertical();
            }

        void DrawCommitButtons()
            {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
                {
                // Commit button
                GUI.enabled = !string.IsNullOrEmpty(_commitMessage) && !_generatingMessage;
                if (GUILayout.Button("✅ Commit Changes", GUILayout.Height(30)))
                    {
                    PerformCommit();
                    }
                GUI.enabled = true;

                // Cancel button
                if (GUILayout.Button("❌ Cancel", GUILayout.Height(30), GUILayout.Width(80)))
                    {
                    Close();
                    }
                }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Quick action buttons
            EditorGUILayout.BeginHorizontal();
                {
                if (GUILayout.Button("📸 Commit + Snapshot"))
                    {
                    PerformCommitWithSnapshot();
                    }

                if (GUILayout.Button("📜 Commit + TLDL"))
                    {
                    PerformCommitWithTLDL();
                    }
                }
            EditorGUILayout.EndHorizontal();

            // 🔧 ENHANCEMENT READY - Visual Status Confirmation Bar
            // Current: Basic status display with commit verification
            // Enhancement path: Git log integration, commit hash display, push status
            DrawCommitStatusBar();
            }

        /// <summary>
        /// 📊 Visual confirmation system for git operations
        /// Shows last commit info, current branch, and operation status
        /// </summary>
        void DrawCommitStatusBar()
            {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                GUILayout.Label("📊 Git Status", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                    {
                    // Current branch info
                    try
                        {
                        (string branch, string lastCommit) = GetCurrentBranchInfo();
                        GUILayout.Label($"🌲 Branch: {branch}", EditorStyles.miniLabel);
                        GUILayout.FlexibleSpace();

                        if (!string.IsNullOrEmpty(lastCommit))
                            {
                            GUILayout.Label($"📝 Last: {lastCommit}", EditorStyles.miniLabel);
                            }
                        }
                    catch
                        {
                        GUILayout.Label("🌲 Git info unavailable", EditorStyles.miniLabel);
                        }
                    }
                EditorGUILayout.EndHorizontal();

                // Operation status
                if (!string.IsNullOrEmpty(_lastOperationStatus))
                    {
                    Color statusColor = _lastOperationSuccess ? Color.green : Color.red;
                    string icon = _lastOperationSuccess ? "✅" : "❌";

                    Color oldColor = GUI.color;
                    GUI.color = statusColor;
                    GUILayout.Label($"{icon} {_lastOperationStatus}", EditorStyles.miniLabel);
                    GUI.color = oldColor;
                    }

                // Verification buttons
                EditorGUILayout.BeginHorizontal();
                    {
                    if (GUILayout.Button("🔄 Refresh Status", GUILayout.Width(120)))
                        {
                        RefreshGitStatus();
                        }

                    if (GUILayout.Button("📋 Copy Last Commit Hash", GUILayout.Width(150)))
                        {
                        CopyLastCommitHash();
                        }

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("📊 View Log", GUILayout.Width(80)))
                        {
                        ShowGitLog();
                        }
                    }
                EditorGUILayout.EndHorizontal();
                }
            EditorGUILayout.EndVertical();
            }

        /// <summary>
        /// 🤖 Generate AI-powered commit message using Warbler integration
        /// Current: Simulated warbler behavior with context analysis
        /// Enhancement path: Real warbler API integration, semantic analysis
        /// </summary>
        void GenerateWarblerSuggestion()
            {
            if (!_useWarblerSuggestion) return;

            _generatingMessage = true;
            _suggestedMessage = "";

            // Simulate warbler analysis delay
            EditorApplication.delayCall += () =>
            {
                try
                    {
                    string suggestion = AnalyzeChangesForCommitMessage();
                    _suggestedMessage = suggestion;
                    }
                catch (System.Exception ex)
                    {
                    UnityEngine.Debug.LogWarning($"[Warbler] Failed to generate suggestion: {ex.Message}");
                    _suggestedMessage = "feat: update project files"; // Fallback
                    }
                finally
                    {
                    _generatingMessage = false;
                    Repaint();
                    }
            };
            }

        string AnalyzeChangesForCommitMessage()
            {
            // 🤖 Simulated Warbler Intelligence
            // Real implementation would integrate with warbler API

            string[] suggestions = new[]
            {
                "feat: implement adjustable code snapshot system",
                "fix: resolve git integration compilation errors",
                "docs: update TLDL entry with code references",
                "refactor: enhance scribe UI with git integration",
                "style: improve git window layout and icons",
                "feat: add warbler-powered commit messages"
            };

            // Smart selection based on context
            if (_dataManager != null)
                {
                if (_dataManager.Title.Contains("git", System.StringComparison.OrdinalIgnoreCase))
                    return "feat: integrate git workflow with scribe system";

                if (_dataManager.Title.Contains("snapshot", System.StringComparison.OrdinalIgnoreCase))
                    return "feat: implement adjustable code snapshot system";

                if (_dataManager.Actions.Count > 0)
                    return $"feat: {_dataManager.Actions[0].Name.ToLower()}";
                }

            // Random intelligent suggestion
            var random = new System.Random();
            return suggestions[random.Next(suggestions.Length)];
            }

        void PerformCommit()
            {
            _lastOperationStatus = "Committing changes...";
            _lastOperationSuccess = false;
            Repaint();

            try
                {
                string fullMessage = _commitMessage;
                if (!string.IsNullOrEmpty(_commitDescription))
                    {
                    fullMessage += $"\n\n{_commitDescription}";
                    }

                // Stage files if needed
                if (_includeAllChanges)
                    {
                    _lastOperationStatus = "Staging files...";
                    Repaint();
                    RunGitCommand("add .");
                    }

                // Commit with proper escaping
                _lastOperationStatus = "Creating commit...";
                Repaint();
                string escapedMessage = fullMessage.Replace("\"", "\\\"");
                string result = RunGitCommand($"commit -m \"{escapedMessage}\"");

                // Extract commit hash from result
                string commitHash = ExtractCommitHash(result);

                _lastOperationStatus = $"✅ Commit successful! Hash: {commitHash}";
                _lastOperationSuccess = true;

                UnityEngine.Debug.Log($"[Git] Commit successful: {_commitMessage} (Hash: {commitHash})");
                ShowNotification(new GUIContent($"✅ Commit created: {commitHash}"));

                // Refresh git info
                RefreshGitStatus();

                // Refresh git window if open
                ScribeGitWindow[] gitWindow = Resources.FindObjectsOfTypeAll<ScribeGitWindow>();
                foreach (ScribeGitWindow window in gitWindow)
                    {
                    window.Repaint();
                    }

                // Keep window open to show confirmation
                Repaint();
                }
            catch (System.Exception ex)
                {
                _lastOperationStatus = $"❌ Commit failed: {ex.Message}";
                _lastOperationSuccess = false;

                UnityEngine.Debug.LogError($"[Git] Commit failed: {ex.Message}");
                ShowNotification(new GUIContent($"❌ Commit failed: {ex.Message}"));
                Repaint();
                }
            }

        /// <summary>
        /// 📊 Git status information retrieval for visual confirmation
        /// </summary>
        (string branch, string lastCommit) GetCurrentBranchInfo()
            {
            try
                {
                string branch = RunGitCommand("branch --show-current").Trim();
                string lastCommit = RunGitCommand("log -1 --oneline").Trim();

                // Truncate long commit messages for display
                if (lastCommit.Length > 50)
                    {
                    lastCommit = lastCommit[..47] + "...";
                    }

                return (branch, lastCommit);
                }
            catch
                {
                return ("unknown", "");
                }
            }

        void RefreshGitStatus()
            {
            try
                {
                _gitInfo = GetCurrentBranchInfo();

                if (string.IsNullOrEmpty(_lastOperationStatus))
                    {
                    _lastOperationStatus = "Ready for commit";
                    _lastOperationSuccess = true;
                    }

                Repaint();
                }
            catch (System.Exception ex)
                {
                _lastOperationStatus = $"⚠️ Git status unavailable: {ex.Message}";
                _lastOperationSuccess = false;
                Repaint();
                }
            }

        void CopyLastCommitHash()
            {
            try
                {
                string hash = RunGitCommand("rev-parse HEAD").Trim();
                GUIUtility.systemCopyBuffer = hash;

                _lastOperationStatus = $"📋 Copied commit hash: {hash[..8]}...";
                _lastOperationSuccess = true;
                ShowNotification(new GUIContent($"📋 Copied: {hash[..8]}..."));
                Repaint();
                }
            catch (System.Exception ex)
                {
                _lastOperationStatus = $"❌ Failed to get commit hash: {ex.Message}";
                _lastOperationSuccess = false;
                Repaint();
                }
            }

        void ShowGitLog()
            {
            try
                {
                string log = RunGitCommand("log --oneline -10");
                UnityEngine.Debug.Log($"[Git] Recent commits:\n{log}");

                _lastOperationStatus = "📊 Git log displayed in console";
                _lastOperationSuccess = true;
                ShowNotification(new GUIContent("📊 Git log shown in console"));
                Repaint();
                }
            catch (System.Exception ex)
                {
                _lastOperationStatus = $"❌ Failed to get git log: {ex.Message}";
                _lastOperationSuccess = false;
                Repaint();
                }
            }

        string ExtractCommitHash(string commitOutput)
            {
            try
                {
                // Git commit output typically starts with [branch hash] message
                Match match = System.Text.RegularExpressions.Regex.Match(commitOutput, @"\[.+?\s+([a-f0-9]+)\]");
                if (match.Success && match.Groups.Count > 1)
                    {
                    return match.Groups[1].Value;
                    }

                // Fallback: get current HEAD hash
                return RunGitCommand("rev-parse --short HEAD").Trim();
                }
            catch
                {
                return "unknown";
                }
            }

        void PerformCommitWithSnapshot()
            {
            // TODO: Integrate with snapshot system
            PerformCommit();
            }

        void PerformCommitWithTLDL()
            {
            // TODO: Auto-generate TLDL entry with commit details
            PerformCommit();
            }

        string RunGitCommand(string arguments)
            {
            var startInfo = new ProcessStartInfo
                {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = Application.dataPath + "/..",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
                };

            using var process = Process.Start(startInfo);
            process.WaitForExit(10000);

            if (process.ExitCode == 0)
                {
                return process.StandardOutput.ReadToEnd();
                }

            string error = process.StandardError.ReadToEnd();
            throw new System.Exception($"Git command failed: {error}");
            }
        }
    }
#endif
