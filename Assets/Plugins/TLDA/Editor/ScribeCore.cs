#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LivingDevAgent.Editor.Scribe
    {
    /// <summary>
    /// 🔧 ENHANCEMENT READY - The Scribe Core orchestrator
    /// Current: Basic UI framework with module coordination
    /// Enhancement path: Full documentation workflow with template integration
    /// Sacred Symbol Preservation: Mind Castle architecture foundation - expand features while preserving module separation
    /// </summary>
    public class ScribeCore : EditorWindow
        {
        // Module references (rooms in the Mind Castle)
        private ScribeNavigator _navigator;
        private ScribeFormBuilder _formBuilder;
        private ScribeRawEditor _rawEditor;
        private ScribePreviewRenderer _previewRenderer;
        private ScribeDataManager _dataManager;
        private ScribeFileOperations _fileOps;
        private ScribeImageManager _imageManager;
        private ScribeTemplateManager _templateManager;
        private ScribeMarkdownParser _markdownParser;
        private ScribeMarkdownGenerator _markdownGenerator;

        // Window state
        private int _currentTab = 0; // 0=Form, 1=Editor, 2=Preview
        private string _statusMessage = "Ready";
        private Rect _position;

        // Performance: cached git status for status bar (updated on timer, never in OnGUI)
        private string _cachedGitInfo = string.Empty;
        private double _gitInfoNextUpdate = 0f;
        private const double GitInfoRefreshInterval = 5.0; // seconds
        private bool _gitInfoUpdateInFlight = false;
        private CancellationTokenSource _gitCts;

        // Performance: repaint debounce to avoid excessive redraws during rapid events
        private double _nextAllowedRepaint = 0f;
        private const double RepaintMinInterval = 0.15; // ~6-7 fps for status changes

        // 📸 REVOLUTIONARY: Adjustable Code Snapshot System - Jerry's Genius Innovation
        private int _snapshotRange = 10; // Default: Standard (10)
        private int _selectedLineNumber = -1;
        private string _currentFileContent = "";
        private readonly Dictionary<string, int> _quickPresets = new()
        {
            { "Tight", 3 },
            { "Standard", 10 },
            { "Wide", 25 }
        };

        [MenuItem("Tiny Walnut Games/Living Dev Agent/The Scribe")]
        public static void ShowWindow()
            {
            ScribeCore window = GetWindow<ScribeCore>("The Scribe");
            window.minSize = new Vector2(900, 600);
            window.Show();
            }

        void OnEnable()
            {
            _dataManager = new ScribeDataManager();
            _fileOps = new ScribeFileOperations(_dataManager);
            _navigator = new ScribeNavigator(_fileOps);
            _imageManager = new ScribeImageManager();
            _markdownParser = new ScribeMarkdownParser(_dataManager);
            _markdownGenerator = new ScribeMarkdownGenerator(_dataManager);
            _previewRenderer = new ScribePreviewRenderer(_imageManager);
            _rawEditor = new ScribeRawEditor(_dataManager, _imageManager);
            _formBuilder = new ScribeFormBuilder(_dataManager, _imageManager);

            // 🔧 ENHANCEMENT READY - Navigator selection state management
            // Current: Connects navigator events to proper file handling
            // Enhancement path: Multi-tab support, recent files, workspace session persistence
            _navigator.OnFileSelected += OnNavigatorFileSelected;
            _navigator.OnDirectorySelected += OnNavigatorDirectorySelected;

            _rawEditor.OnContentChanged += OnRawContentChanged;

            LoadInitialFile();

            // Init git cache
            _gitCts = new CancellationTokenSource();
            _gitInfoNextUpdate = EditorApplication.timeSinceStartup; // immediate first refresh
            }

        void InitializeModules()
            {
            // KeeperNote: Module initialization order matters - dependencies first
            _dataManager = new ScribeDataManager();
            _fileOps = new ScribeFileOperations(_dataManager);
            _imageManager = new ScribeImageManager();
            _templateManager = new ScribeTemplateManager();

            _navigator = new ScribeNavigator(_fileOps);
            _formBuilder = new ScribeFormBuilder(_dataManager, _imageManager);
            _rawEditor = new ScribeRawEditor(_dataManager, _imageManager);
            _previewRenderer = new ScribePreviewRenderer(_imageManager);

            // Wire up event handlers
            _navigator.OnFileSelected += HandleFileSelected;
            _formBuilder.OnContentChanged += HandleFormContentChanged;
            _rawEditor.OnContentChanged += HandleRawContentChanged;
            }

        void OnGUI()
            {
            _position = position;

            DrawToolbar();

            EditorGUILayout.BeginHorizontal();
                {
                // Left panel - Navigator
                _navigator.Draw(260);

                // Main content area
                EditorGUILayout.BeginVertical();
                    {
                    DrawTabs();
                    DrawCurrentTabContent();
                    }
                EditorGUILayout.EndVertical();
                }
            EditorGUILayout.EndHorizontal();

            DrawStatusBar();
            }

        void DrawToolbar()
            {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                {
                if (GUILayout.Button("Generate", EditorStyles.toolbarButton))
                    {
                    GenerateFromForm();
                    }

                if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                    {
                    SaveCurrentDocument();
                    }

                // 📸 Adjustable Code Snapshot Controls
                GUILayout.Space(10);
                GUILayout.Label("📸", EditorStyles.miniLabel);

                // Quick presets
                foreach (KeyValuePair<string, int> preset in _quickPresets)
                    {
                    if (GUILayout.Button($"{preset.Key} ({preset.Value})", EditorStyles.toolbarButton, GUILayout.Width(80)))
                        {
                        _snapshotRange = preset.Value;
                        UpdateStatus($"Snapshot range: {preset.Key} ({preset.Value} lines)");
                        }
                    }

                GUILayout.Space(5);
                GUILayout.Label("Range:", EditorStyles.miniLabel);
                _snapshotRange = EditorGUILayout.IntSlider(_snapshotRange, 1, 50, GUILayout.Width(120));

                if (GUILayout.Button("📋 Copy Snapshot", EditorStyles.toolbarButton))
                    {
                    OpenSpellsmithCodeViewer();
                    }

                if (GUILayout.Button("🔗 Add Reference", EditorStyles.toolbarButton))
                    {
                    AddCodeReference();
                    }

                GUILayout.FlexibleSpace();

                // 🐙 GitExtensions-style Git Integration
                if (GUILayout.Button("🐙 Git Status", EditorStyles.toolbarButton))
                    {
                    ShowGitStatus();
                    }

                if (GUILayout.Button("📝 Smart Commit", EditorStyles.toolbarButton))
                    {
                    ShowWarblerCommitDialog();
                    }

                if (GUILayout.Button("Settings", EditorStyles.toolbarButton))
                    {
                    ShowSettings();
                    }
                }
            EditorGUILayout.EndHorizontal();
            }

        // 🔧 ENHANCEMENT READY - Tab navigation system
        // Current: Basic tab switching for Form/Editor/Preview modes
        // Enhancement path: Add keyboard shortcuts, tab context menus, drag reordering
        void DrawTabs()
            {
            string[] tabNames = { "Form Builder", "Raw Editor", "Preview" };
            _currentTab = GUILayout.Toolbar(_currentTab, tabNames);
            }

        // 🔧 ENHANCEMENT READY - Content area rendering
        // Current: Basic module delegation based on active tab
        // Enhancement path: Split view modes, synchronized scrolling, real-time preview
        void DrawCurrentTabContent()
            {
            switch (_currentTab)
                {
                case 0:
                    _formBuilder?.Draw();
                    break;
                case 1:
                    _rawEditor?.Draw();
                    break;
                case 2:
                    _previewRenderer?.Draw(_dataManager?.RawMarkdown ?? "");
                    break;
                }
            }

        // 🔧 ENHANCEMENT READY - Status communication system
        // Current: Enhanced status display with git integration and operation confirmation
        // Enhancement path: Progress indicators, actionable notifications, toast messages, commit history
        void DrawStatusBar()
            {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                {
                GUILayout.Label(_statusMessage, EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();

                // Git status indicator (cached + throttled)
                TryScheduleGitInfoUpdate();
                if (!string.IsNullOrEmpty(_cachedGitInfo))
                    {
                    GUILayout.Label($"🐙 {_cachedGitInfo}", EditorStyles.miniLabel);
                    GUILayout.Space(10);
                    }

                // Show current file info
                if (!string.IsNullOrEmpty(_fileOps?.CurrentFilePath))
                    {
                    GUILayout.Label($"📄 {System.IO.Path.GetFileName(_fileOps.CurrentFilePath)}", EditorStyles.miniLabel);
                    }
                }
            EditorGUILayout.EndHorizontal();
            }

        // Background-cached git info (never blocks OnGUI)
        void TryScheduleGitInfoUpdate()
            {
            double now = EditorApplication.timeSinceStartup;
            if (_gitInfoUpdateInFlight || now < _gitInfoNextUpdate) return;
            _gitInfoUpdateInFlight = true;
            _gitInfoNextUpdate = now + GitInfoRefreshInterval;

            var token = _gitCts?.Token ?? CancellationToken.None;
            Task.Run(() =>
                {
                    string info = ComputeGitInfoSafe(token);
                    if (token.IsCancellationRequested) return;
                    EditorApplication.delayCall += () =>
                        {
                            _cachedGitInfo = info;
                            _gitInfoUpdateInFlight = false;
                            DebouncedRepaint();
                        };
                });
            }

        static string ComputeGitInfoSafe(CancellationToken token)
            {
            try
                {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                    FileName = "git",
                    Arguments = "status --porcelain -b", // includes branch + changes
                    WorkingDirectory = UnityEngine.Application.dataPath + "/..",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                    };

                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process == null) return string.Empty;

                // Wait with hard timeout to prevent zombie processes
                if (!process.WaitForExit(500))
                    {
                    try { process.Kill(); } catch { }
                    return string.Empty;
                    }

                string output = process.StandardOutput.ReadToEnd();
                if (string.IsNullOrEmpty(output)) return string.Empty;

                // First line example: '## main' or '## feature/foo'
                // Remaining lines are changes in porcelain format
                var lines = output.Split('\n');
                if (lines.Length == 0) return string.Empty;
                string branch = lines[0].Trim().TrimStart('#', ' ');
                int changedFiles = 0;
                for (int i = 1; i < lines.Length; i++)
                    if (!string.IsNullOrWhiteSpace(lines[i])) changedFiles++;

                return changedFiles > 0 ? $"{branch} ({changedFiles} changes)" : $"{branch} (clean)";
                }
            catch
                {
                return string.Empty;
                }
            }

        // 🔧 ENHANCEMENT READY - Document generation workflow
        // Current: Basic form-to-markdown conversion
        // Enhancement path: Template selection, batch generation, export formats
        void GenerateFromForm()
            {
            try
                {
                if (_dataManager != null)
                    {
                    _dataManager.SyncFormToRaw();
                    _currentTab = 2; // Switch to preview
                    UpdateStatus("Document generated successfully");
                    }
                }
            catch (System.Exception ex)
                {
                UpdateStatus($"Generation failed: {ex.Message}");
                Debug.LogError($"[Scribe] Generation error: {ex}");
                }
            }

        // 🔧 ENHANCEMENT READY - Document persistence system
        // Current: Basic save operation through file ops
        // Enhancement path: Auto-save, version control integration, backup management
        void SaveCurrentDocument()
            {
            try
                {
                if (_fileOps != null && _fileOps.SaveFile())
                    {
                    UpdateStatus("Document saved successfully");
                    }
                else
                    {
                    UpdateStatus("Save failed - check file permissions");
                    }
                }
            catch (System.Exception ex)
                {
                UpdateStatus($"Save failed: {ex.Message}");
                Debug.LogError($"[Scribe] Save error: {ex}");
                }
            }

        // 🔧 ENHANCEMENT READY - Settings management
        // Current: Placeholder for configuration UI
        // Enhancement path: Preferences window, workspace settings, plugin configuration
        void ShowSettings()
            {
            // TODO: Implement settings window
            UpdateStatus("Settings panel coming soon...");
            }

        // 🔧 ENHANCEMENT READY - Session state persistence
        // Current: Placeholder for window state restoration
        // Enhancement path: Tab positions, recent files, workspace layout memory
        void RestoreSessionState()
            {
            // TODO: Restore window state from EditorPrefs
            // _currentTab = EditorPrefs.GetInt("Scribe.CurrentTab", 0);
            // _dataManager.RootPath = EditorPrefs.GetString("Scribe.RootPath", "Assets");
            }

        /// <summary>
        /// 🔧 ENHANCEMENT READY - Initial file loading for startup
        /// Current: Basic default document creation
        /// Enhancement path: Remember last opened file, workspace restoration, template selection
        /// </summary>
        void LoadInitialFile()
            {
            try
                {
                // Create a new document by default
                _fileOps?.CreateNewDocument();
                UpdateStatus("Ready - Create new document or use Navigator to open file");
                }
            catch (System.Exception ex)
                {
                UpdateStatus($"Initialization warning: {ex.Message}");
                Debug.LogWarning($"[Scribe] LoadInitialFile: {ex.Message}");
                }
            }

        // Event handlers - Sacred extension points for workflow integration

        void HandleFileSelected(string filePath)
            {
            _fileOps.LoadFile(filePath);
            _currentTab = 1; // Switch to editor
            UpdateStatus($"Loaded: {System.IO.Path.GetFileName(filePath)}");
            }

        void HandleFormContentChanged()
            {
            if (_dataManager != null && _dataManager.AutoSyncEnabled)
                {
                _dataManager.SyncFormToRaw();
                // Note: Removed Refresh() call as it doesn't exist in ScribeRawEditor
                UpdateStatus("Form synced to raw editor");
                }
            }

        // 🔧 ENHANCEMENT READY - Raw editor change handling
        // Current: Basic notification of content changes
        // Enhancement path: Conflict resolution, real-time validation, auto-formatting
        void HandleRawContentChanged(string newContent)
            {
            if (_dataManager != null)
                {
                _dataManager.RawIsDirty = true;
                UpdateStatus("Raw content modified");
                }
            }

        /// <summary>
        /// 🔧 ENHANCEMENT READY - Raw content change handler for Navigator integration
        /// Current: Handles raw editor content changes
        /// Enhancement path: Auto-save, conflict detection, real-time validation
        /// </summary>
        void OnRawContentChanged(string newContent)
            {
            if (_dataManager != null)
                {
                _dataManager.RawMarkdown = newContent;
                _dataManager.RawIsDirty = true;
                UpdateStatus("Content updated");
                }
            }

        void UpdateStatus(string message)
            {
            _statusMessage = message;
            DebouncedRepaint();
            }

        // Prevent repaint storms from frequent status changes
        void DebouncedRepaint()
            {
            double now = EditorApplication.timeSinceStartup;
            if (now >= _nextAllowedRepaint)
                {
                _nextAllowedRepaint = now + RepaintMinInterval;
                Repaint();
                }
            }

        void OnDisable()
            {
            // Clean up event handlers
            if (_navigator != null)
                _navigator.OnFileSelected -= HandleFileSelected;
            if (_formBuilder != null)
                _formBuilder.OnContentChanged -= HandleFormContentChanged;
            if (_rawEditor != null)
                _rawEditor.OnContentChanged -= HandleRawContentChanged;

            // cancel background work
            try { _gitCts?.Cancel(); _gitCts?.Dispose(); } catch { }
            }

        /// <summary>
        /// 📸 REVOLUTIONARY - Dynamic Code Snapshot with Surgical Precision
        /// Current: Captures adjustable range around selected line with live preview
        /// Enhancement path: Syntax highlighting, smart context detection, auto-range suggestion
        /// Sacred Symbol Preservation: This is Jerry's genius innovation for perfect context capture!
        /// </summary>
        void CopyCodeSnapshot()
            {
            if (string.IsNullOrEmpty(_currentFileContent) || _selectedLineNumber < 0)
                {
                UpdateStatus("⚠️ Select a line in the editor first");
                return;
                }

            string[] lines = _currentFileContent.Split('\n');
            int targetLine = Mathf.Clamp(_selectedLineNumber, 0, lines.Length - 1);

            int startLine = Mathf.Max(0, targetLine - _snapshotRange);
            int endLine = Mathf.Min(lines.Length - 1, targetLine + _snapshotRange);

            var snapshot = new System.Text.StringBuilder();
            snapshot.AppendLine($"```csharp");
            snapshot.AppendLine($"// 📸 Code Snapshot: Lines {startLine + 1}-{endLine + 1} (Range: ±{_snapshotRange})");
            snapshot.AppendLine($"// Target Line: {targetLine + 1}");
            snapshot.AppendLine($"// File: {_fileOps?.CurrentFilePath ?? "Unknown"}");
            snapshot.AppendLine();

            for (int i = startLine; i <= endLine; i++)
                {
                string linePrefix = (i == targetLine) ? ">>> " : "    ";
                snapshot.AppendLine($"{linePrefix}{lines[i]}");
                }

            snapshot.AppendLine("```");

            GUIUtility.systemCopyBuffer = snapshot.ToString();
            UpdateStatus($"📸 Snapshot copied! Lines {startLine + 1}-{endLine + 1} (±{_snapshotRange} range)");
            }

        /// <summary>
        /// 🔗 ENHANCEMENT READY - Smart Code Reference Integration
        /// Current: Adds snapshot as embedded reference in current TLDL entry
        /// Enhancement path: Cross-document linking, version-aware references, diff integration
        /// </summary>
        void AddCodeReference()
            {
            if (_dataManager != null)
                {
                string snapshot = GenerateCodeSnapshot();

                // Add to current action or discovery
                if (_dataManager.Actions.Count > 0)
                    {
                    ActionItem lastAction = _dataManager.Actions[^1];
                    lastAction.FilesChanged += $"\n\n{snapshot}";
                    }
                else if (_dataManager.Discoveries.Count > 0)
                    {
                    Discovery lastDiscovery = _dataManager.Discoveries[^1];
                    lastDiscovery.Evidence += $"\n\n{snapshot}";
                    }

                _dataManager.SyncFormToRaw();
                UpdateStatus($"🔗 Code reference added with ±{_snapshotRange} line context");
                }
            }

        /// <summary>
        /// 🐙 LEGENDARY - GitExtensions-style Git Integration
        /// Current: Basic git status display in Unity Editor
        /// Enhancement path: Full GitExtensions UI, diff viewer, branch management, merge conflict resolution
        /// Sacred Vision: "It would make it feel a little like you" - AI-powered Git workflow!
        /// </summary>
        void ShowGitStatus()
            {
            ScribeGitWindow gitWindow = EditorWindow.GetWindow<ScribeGitWindow>("Git Status");
            gitWindow.Show();
            UpdateStatus("🐙 Git interface opening...");
            }

        /// <summary>
        /// 📝 REVOLUTIONARY - Warbler-Powered Smart Commit System
        /// Current: Integration point for warbler commit message generation
        /// Enhancement path: AI-powered commit messages, automatic staging, semantic versioning
        /// Sacred Vision: Warbler syncing for commit messages that feel like AI collaboration!
        /// </summary>
        void ShowWarblerCommitDialog()
            {
            ScribeCommitWindow commitWindow = EditorWindow.GetWindow<ScribeCommitWindow>("Smart Commit");
            commitWindow.Initialize(_dataManager, _fileOps);
            commitWindow.Show();
            UpdateStatus("📝 Warbler commit dialog opening...");
            }

        string GenerateCodeSnapshot()
            {
            if (string.IsNullOrEmpty(_currentFileContent) || _selectedLineNumber < 0)
                return "// No code selected for snapshot";

            string[] lines = _currentFileContent.Split('\n');
            int targetLine = Mathf.Clamp(_selectedLineNumber, 0, lines.Length - 1);
            int startLine = Mathf.Max(0, targetLine - _snapshotRange);
            int endLine = Mathf.Min(lines.Length - 1, targetLine + _snapshotRange);

            var snapshot = new System.Text.StringBuilder();
            snapshot.AppendLine($"### 📸 Code Reference (±{_snapshotRange} lines)");
            snapshot.AppendLine($"**File**: `{System.IO.Path.GetFileName(_fileOps?.CurrentFilePath ?? "Unknown")}`");
            snapshot.AppendLine($"**Lines**: {startLine + 1}-{endLine + 1} (Target: {targetLine + 1})");
            snapshot.AppendLine();
            snapshot.AppendLine("```csharp");

            for (int i = startLine; i <= endLine; i++)
                {
                string lineMarker = (i == targetLine) ? " // ← TARGET" : "";
                snapshot.AppendLine($"{lines[i]}{lineMarker}");
                }

            snapshot.AppendLine("```");
            return snapshot.ToString();
            }

        /// <summary>
        /// 🔧 ENHANCEMENT READY - Navigator file selection with proper state management
        /// Current: Handles file selection and maintains Navigator selection state
        /// Enhancement path: File change detection, auto-save prompts, multi-document tabs
        /// Sacred Vision: Files remain blue when selected, user always knows what's open!
        /// </summary>
        void OnNavigatorFileSelected(string filePath)
            {
            try
                {
                // Update Navigator selection state FIRST
                _navigator.SetSelectedFile(filePath);

                // Load the file
                _fileOps.LoadFile(filePath);

                // Update UI components - simplified approach
                if (_dataManager != null && _fileOps != null)
                    {
                    // For markdown files, parse into form
                    string extension = System.IO.Path.GetExtension(filePath).ToLower();
                    if (extension == ".md")
                        {
                        _dataManager.ParseMarkdown(_dataManager.RawMarkdown);
                        }
                    }

                // Update status
                UpdateStatus($"📖 Opened: {System.IO.Path.GetFileName(filePath)}");

                // Force UI refresh to show selection state
                Repaint();

                Debug.Log($"✅ File selected and loaded: {filePath}");
                }
            catch (System.Exception e)
                {
                UpdateStatus($"❌ Failed to open file: {e.Message}");
                Debug.LogError($"[ScribeCore] Failed to open file {filePath}: {e.Message}");
                }
            }

        void OnNavigatorDirectorySelected(string directoryPath)
            {
            UpdateStatus($"📁 Selected directory: {System.IO.Path.GetFileName(directoryPath)}");
            Debug.Log($"Directory selected: {directoryPath}");
            }

        /// <summary>
        /// 🔍 LEGENDARY - Open Spellsmith Code Selector as persistent window
        /// Current: Opens persistent window for proper code selection workflow
        /// Enhancement path: Direct integration with current file, auto-line selection
        /// Sacred Vision: "It should stay open so I can actually select lines!"
        /// </summary>
        void OpenSpellsmithCodeViewer()
            {
            string currentFile = _fileOps?.CurrentFilePath;
            if (!string.IsNullOrEmpty(currentFile) && File.Exists(currentFile))
                {
                // Open as persistent window that stays open for line selection
                SpellsmithCodePopup.ShowWindow(currentFile, (snapshot) =>
                {
                    // Callback when user creates snapshot - but window stays open!
                    if (!string.IsNullOrEmpty(snapshot))
                        {
                        GUIUtility.systemCopyBuffer = snapshot;
                        UpdateStatus("📸 Code snapshot copied to clipboard!");
                        }
                });
                }
            else
                {
                SpellsmithCodePopup.ShowWindow("", (snapshot) =>
                {
                    if (!string.IsNullOrEmpty(snapshot))
                        {
                        GUIUtility.systemCopyBuffer = snapshot;
                        UpdateStatus("📸 Code snapshot copied to clipboard!");
                        }
                });
                }
            UpdateStatus("🔍 Spellsmith Code Selector opened - stays open for line selection!");
            }
        }
    }
#endif
