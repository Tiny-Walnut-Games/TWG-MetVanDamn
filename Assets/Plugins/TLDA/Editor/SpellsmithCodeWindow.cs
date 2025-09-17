#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LivingDevAgent.Editor.Scribe
    {
    /// <summary>
    /// 🔍 LEGENDARY - Spellsmith Code Selection Popup Modal (Fixed to Stay Open!)
    /// Sacred Vision: "It should stay open so I can actually select lines!"
    /// This version stays persistently open for proper code selection workflow
    /// </summary>
    public class SpellsmithCodePopup : EditorWindow
        {
        // File content and selection
        private string _filePath = "";
        private string _fileContent = "";
        private string[] _lines = new string[0];
        private int _selectedLineNumber = -1;
        private int _snapshotRange = 10;
        private Vector2 _codeScrollPosition;
        private Vector2 _previewScrollPosition;

        // UI state
        private readonly Dictionary<string, int> _quickPresets = new()
        {
            { "Tight", 3 },
            { "Standard", 10 },
            { "Wide", 25 }
        };

        // Visual styles
        private GUIStyle _codeStyle;
        private GUIStyle _lineNumberStyle;
        private GUIStyle _selectedLineStyle;
        private bool _stylesInitialized = false;

        // Callback for when user creates snapshot
        private System.Action<string> _onSnapshotCreated;

        // Static reference to keep window alive
        private static SpellsmithCodePopup _instance;

        public static void ShowWindow(string filePath = "", System.Action<string> onSnapshotCreated = null)
            {
            // Use GetWindow instead of PopupWindow.Show to create persistent window
            _instance = GetWindow<SpellsmithCodePopup>("🔍 Spellsmith Code Selector");
            _instance.minSize = new Vector2(800, 500);
            _instance._onSnapshotCreated = onSnapshotCreated;

            if (!string.IsNullOrEmpty(filePath))
                {
                _instance.LoadFile(filePath);
                }

            _instance.Show();
            }

        void OnEnable()
            {
            // Restore window reference if lost
            if (_instance == null)
                _instance = this;
            }

        void OnGUI()
            {
            InitializeStyles();

            EditorGUILayout.BeginVertical();
                {
                DrawToolbar();
                DrawCodeAndPreview();
                DrawActionButtons();
                }
            EditorGUILayout.EndVertical();
            }

        void InitializeStyles()
            {
            if (_stylesInitialized) return;

            _codeStyle = new GUIStyle(EditorStyles.label)
                {
                font = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Monaco", "Lucida Console" }, 10),
                richText = false,
                wordWrap = false,
                padding = new RectOffset(5, 5, 1, 1)
                };

            _lineNumberStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                font = _codeStyle.font,
                alignment = TextAnchor.MiddleRight,
                padding = new RectOffset(2, 8, 1, 1),
                normal = { textColor = Color.gray }
                };

            _selectedLineStyle = new GUIStyle(_codeStyle)
                {
                normal = { textColor = Color.white }
                };

            _stylesInitialized = true;
            }

        void DrawToolbar()
            {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                {
                GUILayout.Label("🔍 Spellsmith Code Selector", EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("📂 Open File", EditorStyles.toolbarButton))
                    {
                    OpenFileDialog();
                    }

                // Range controls
                GUILayout.Label("Range:", EditorStyles.miniLabel);

                // Quick presets
                foreach (KeyValuePair<string, int> preset in _quickPresets)
                    {
                    if (GUILayout.Button($"{preset.Key}", EditorStyles.toolbarButton, GUILayout.Width(50)))
                        {
                        _snapshotRange = preset.Value;
                        }
                    }

                _snapshotRange = EditorGUILayout.IntSlider(_snapshotRange, 1, 50, GUILayout.Width(100));
                }
            EditorGUILayout.EndHorizontal();

            // File info
            if (!string.IsNullOrEmpty(_filePath))
                {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    {
                    GUILayout.Label($"📄 {Path.GetFileName(_filePath)}", EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    if (_selectedLineNumber >= 0)
                        {
                        GUILayout.Label($"📍 Line: {_selectedLineNumber + 1}/{_lines.Length}", EditorStyles.miniLabel);
                        GUILayout.Label($"📏 Range: ±{_snapshotRange}", EditorStyles.miniLabel);
                        }
                    else
                        {
                        GUILayout.Label($"📄 {_lines.Length} lines total - Click a line to select", EditorStyles.miniLabel);
                        }
                    }
                EditorGUILayout.EndHorizontal();
                }
            }

        void DrawCodeAndPreview()
            {
            if (_lines.Length == 0)
                {
                EditorGUILayout.HelpBox("🔍 Click 'Open File' to load a code file for line selection", MessageType.Info);
                return;
                }

            EditorGUILayout.BeginHorizontal();
                {
                // Left side - Code viewer
                EditorGUILayout.BeginVertical(GUILayout.Width(400));
                    {
                    GUILayout.Label("🔍 Click any line to select it:", EditorStyles.boldLabel);

                    _codeScrollPosition = EditorGUILayout.BeginScrollView(_codeScrollPosition, GUILayout.Height(300));
                        {
                        EditorGUILayout.BeginVertical();
                            {
                            for (int i = 0; i < _lines.Length; i++)
                                {
                                DrawCodeLine(i);
                                }
                            }
                        EditorGUILayout.EndVertical();
                        }
                    EditorGUILayout.EndScrollView();
                    }
                EditorGUILayout.EndVertical();

                // Right side - Preview
                EditorGUILayout.BeginVertical();
                    {
                    DrawSnapshotPreview();
                    }
                EditorGUILayout.EndVertical();
                }
            EditorGUILayout.EndHorizontal();
            }

        void DrawCodeLine(int lineIndex)
            {
            string line = _lines[lineIndex];
            bool isSelected = lineIndex == _selectedLineNumber;
            bool isInRange = _selectedLineNumber >= 0 &&
                           Mathf.Abs(lineIndex - _selectedLineNumber) <= _snapshotRange;

            EditorGUILayout.BeginHorizontal(GUILayout.Height(16));
                {
                // Line number
                GUILayout.Label((lineIndex + 1).ToString(), _lineNumberStyle, GUILayout.Width(40));

                // Background color
                Color originalBackgroundColor = GUI.backgroundColor;
                if (isSelected)
                    {
                    GUI.backgroundColor = new Color(0.3f, 0.6f, 1f, 0.8f);
                    }
                else if (isInRange)
                    {
                    GUI.backgroundColor = new Color(0.1f, 0.4f, 0.8f, 0.4f);
                    }

                // Code content - clickable
                GUIStyle buttonStyle = isSelected ? EditorStyles.helpBox : EditorStyles.label;
                if (GUILayout.Button(line, buttonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(14)))
                    {
                    _selectedLineNumber = lineIndex;
                    Debug.Log($"🔍 Selected line {lineIndex + 1}: {line[..Mathf.Min(50, line.Length)]}...");
                    Repaint();
                    }

                GUI.backgroundColor = originalBackgroundColor;
                }
            EditorGUILayout.EndHorizontal();
            }

        void DrawSnapshotPreview()
            {
            GUILayout.Label("📸 Snapshot Preview:", EditorStyles.boldLabel);

            if (_selectedLineNumber >= 0 && _lines.Length > 0)
                {
                int startLine = Mathf.Max(0, _selectedLineNumber - _snapshotRange);
                int endLine = Mathf.Min(_lines.Length - 1, _selectedLineNumber + _snapshotRange);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                    GUILayout.Label($"Lines {startLine + 1}-{endLine + 1} (Target: {_selectedLineNumber + 1})", EditorStyles.miniLabel);

                    _previewScrollPosition = EditorGUILayout.BeginScrollView(_previewScrollPosition, GUILayout.Height(250));
                        {
                        string preview = GenerateSnapshotText();
                        EditorGUILayout.SelectableLabel(preview, EditorStyles.wordWrappedLabel, GUILayout.ExpandHeight(true));
                        }
                    EditorGUILayout.EndScrollView();
                    }
                EditorGUILayout.EndVertical();
                }
            else
                {
                EditorGUILayout.HelpBox("Select a line to preview snapshot", MessageType.Info);
                }
            }

        void DrawActionButtons()
            {
            EditorGUILayout.BeginHorizontal();
                {
                GUI.enabled = _selectedLineNumber >= 0;
                if (GUILayout.Button("📋 Copy Snapshot", GUILayout.Height(30)))
                    {
                    string snapshot = GenerateSnapshotText();
                    GUIUtility.systemCopyBuffer = snapshot;
                    _onSnapshotCreated?.Invoke(snapshot);

                    // Show confirmation but DON'T close the window
                    Debug.Log($"📋 Snapshot copied! Lines {Mathf.Max(0, _selectedLineNumber - _snapshotRange) + 1}-{Mathf.Min(_lines.Length - 1, _selectedLineNumber + _snapshotRange) + 1}");
                    ShowNotification(new GUIContent("📋 Snapshot copied to clipboard!"));
                    }
                GUI.enabled = true;

                if (GUILayout.Button("🔄 Clear Selection", GUILayout.Height(30)))
                    {
                    _selectedLineNumber = -1;
                    Repaint();
                    }

                if (GUILayout.Button("❌ Close", GUILayout.Height(30)))
                    {
                    Close();
                    }

                if (!string.IsNullOrEmpty(_filePath) && GUILayout.Button("✏️ Edit in IDE", GUILayout.Width(100), GUILayout.Height(30)))
                    {
                    System.Diagnostics.Process.Start(_filePath);
                    }
                }
            EditorGUILayout.EndHorizontal();
            }

        void OpenFileDialog()
            {
            string path = EditorUtility.OpenFilePanel(
                "Select Code File",
                string.IsNullOrEmpty(_filePath) ? Application.dataPath : Path.GetDirectoryName(_filePath),
                "cs,py,js,mjs,sh,md,yaml,yml,json,txt"
            );

            if (!string.IsNullOrEmpty(path))
                {
                LoadFile(path);
                }
            }

        void LoadFile(string filePath)
            {
            try
                {
                _filePath = filePath;
                _fileContent = File.ReadAllText(filePath);
                _lines = _fileContent.Split('\n');
                _selectedLineNumber = -1;

                Debug.Log($"🔍 Loaded {_lines.Length} lines from {Path.GetFileName(filePath)}");
                Repaint();
                }
            catch (System.Exception e)
                {
                EditorUtility.DisplayDialog("File Load Error", $"Failed to load file:\n{e.Message}", "OK");
                }
            }

        string GenerateSnapshotText()
            {
            if (_selectedLineNumber < 0) return "";

            int startLine = Mathf.Max(0, _selectedLineNumber - _snapshotRange);
            int endLine = Mathf.Min(_lines.Length - 1, _selectedLineNumber + _snapshotRange);

            var snapshot = new System.Text.StringBuilder();
            string extension = Path.GetExtension(_filePath).ToLower();
            string language = extension switch
                {
                    ".cs" => "csharp",
                    ".py" => "python",
                    ".js" or ".mjs" => "javascript",
                    ".sh" => "bash",
                    _ => "text"
                    };

            snapshot.AppendLine($"```{language}");
            snapshot.AppendLine($"// 📸 Code Snapshot: Lines {startLine + 1}-{endLine + 1} (Range: ±{_snapshotRange})");
            snapshot.AppendLine($"// Target Line: {_selectedLineNumber + 1}");
            snapshot.AppendLine($"// File: {Path.GetFileName(_filePath)}");
            snapshot.AppendLine();

            for (int i = startLine; i <= endLine; i++)
                {
                string linePrefix = (i == _selectedLineNumber) ? ">>> " : "    ";
                snapshot.AppendLine($"{linePrefix}{_lines[i]}");
                }

            snapshot.AppendLine("```");
            return snapshot.ToString();
            }

        void OnDestroy()
            {
            // Clear static reference when window is destroyed
            if (_instance == this)
                _instance = null;
            }
        }
    }
#endif
