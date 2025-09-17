using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

namespace LivingDevAgent.Editor.Scribe
    {
    /// <summary>
    /// 🔧 ENHANCEMENT READY - High-performance raw markdown editor with cursor positioning
    /// Current: Basic text area with markdown integration
    /// Enhancement path: Syntax highlighting, smart indentation, cursor position management for image insertion
    /// Sacred Vision: Transform typing lag into smooth, responsive editing experience!
    /// </summary>
    public class ScribeRawEditor
        {
        private readonly ScribeDataManager _data;
        private readonly ScribeImageManager _imageManager;
        private Vector2 _scrollPosition;
        private string _lastMarkdown = "";
        private bool _contentChanged = false;

        // 🔧 ENHANCEMENT READY - Cursor position tracking for precise image insertion
        private int _cursorPosition = 0;
        private bool _focusTextArea = false;

        // 🔧 Performance optimization - cached style
        private GUIStyle _textAreaStyle;
        private bool _styleInitialized = false;

        public event System.Action<string> OnContentChanged;

        public ScribeRawEditor(ScribeDataManager data, ScribeImageManager imageManager)
            {
            _data = data;
            _imageManager = imageManager;
            }

        void InitializeStyle()
            {
            if (_styleInitialized) return;

            _textAreaStyle = new GUIStyle(EditorStyles.textArea)
                {
                font = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Monaco", "Lucida Console" }, 12),
                wordWrap = true,
                richText = false // 🔧 Performance: Disable rich text for large documents
                };

            _styleInitialized = true;
            }

        public void Draw()
            {
            InitializeStyle();

            EditorGUILayout.BeginVertical();
                {
                DrawToolbar();
                DrawEditor();
                }
            EditorGUILayout.EndVertical();
            }

        void DrawToolbar()
            {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                {
                GUILayout.Label("✏️ Raw Editor", EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                // 🔧 ENHANCEMENT READY - Image insertion at cursor position
                if (GUILayout.Button("🖼️ Insert Image", EditorStyles.toolbarButton))
                    {
                    InsertImageAtCursor();
                    }

                if (GUILayout.Button("📋 Paste", EditorStyles.toolbarButton))
                    {
                    PasteContentAtCursor();
                    }

                if (GUILayout.Button("🔄 Format", EditorStyles.toolbarButton))
                    {
                    FormatMarkdown();
                    }
                }
            EditorGUILayout.EndHorizontal();
            }

        void DrawEditor()
            {
            EditorGUILayout.LabelField("Markdown Content:", EditorStyles.boldLabel);

            // 🔧 ENHANCEMENT READY - Performance optimization with change detection
            // Current: Only update when content actually changes
            // Enhancement path: Incremental parsing, syntax highlighting, auto-save

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));
                {
                // Set focus to text area if requested
                if (_focusTextArea)
                    {
                    GUI.FocusControl("RawMarkdownEditor");
                    _focusTextArea = false;
                    }

                GUI.SetNextControlName("RawMarkdownEditor");

                // 🔧 Performance enhancement: Only trigger change events when content actually changes
                string currentContent = _data.RawMarkdown ?? "";
                string newContent = EditorGUILayout.TextArea(
                    currentContent,
                    _textAreaStyle,
                    GUILayout.ExpandHeight(true),
                    GUILayout.MinHeight(400)
                );

                // Track cursor position for image insertion
                var textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                if (textEditor != null)
                    {
                    _cursorPosition = textEditor.cursorIndex;
                    }

                // 🔧 REAL-TIME UPDATE: Update immediately when content changes
                if (newContent != _lastMarkdown)
                    {
                    _data.RawMarkdown = newContent;
                    _lastMarkdown = newContent;
                    _contentChanged = true;

                    // Immediate notification for real-time feedback
                    OnContentChanged?.Invoke(newContent);

                    // Also trigger delayed update for any other systems
                    EditorApplication.delayCall += () =>
                    {
                        if (_contentChanged)
                            {
                            _contentChanged = false;
                            }
                    };
                    }
                }
            EditorGUILayout.EndScrollView();

            // 🔧 Status bar with helpful information
            DrawStatusBar();
            }

        /// <summary>
        /// 🔧 ENHANCEMENT READY - Precise cursor-based image insertion
        /// Current: Inserts image link at current cursor position with real-time feedback
        /// Enhancement path: Image preview, drag-and-drop support, auto-path resolution
        /// Sacred Mission: Images should appear WHERE the user wants them, not at the beginning!
        /// </summary>
        void InsertImageAtCursor()
            {
            string imagePath = EditorUtility.OpenFilePanel(
                "Select Image",
                _imageManager.GetImagesDirectory(),
                "png,jpg,jpeg,gif,bmp,tga"
            );

            if (!string.IsNullOrEmpty(imagePath))
                {
                // Import image to project
                string relativePath = _imageManager.AddImage(imagePath);
                if (!string.IsNullOrEmpty(relativePath))
                    {
                    string imageMarkdown = $"![Image]({relativePath})\n";

                    // 🔧 SACRED ENHANCEMENT: Get current cursor position from the actual text editor
                    string currentContent = _data.RawMarkdown ?? "";

                    // Get the TextEditor for cursor position
                    string controlName = "RawMarkdownEditor";
                    GUI.FocusControl(controlName);

                    var textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                    int cursorPos = textEditor?.cursorIndex ?? currentContent.Length;

                    // Ensure cursor position is valid
                    cursorPos = Mathf.Clamp(cursorPos, 0, currentContent.Length);

                    // Insert at cursor position
                    string newContent = currentContent.Insert(cursorPos, imageMarkdown);
                    _data.RawMarkdown = newContent;

                    // Move cursor after inserted content for next operation
                    EditorApplication.delayCall += () =>
                    {
                        if (textEditor != null)
                            {
                            textEditor.cursorIndex = cursorPos + imageMarkdown.Length;
                            textEditor.selectIndex = textEditor.cursorIndex;
                            }
                    };

                    OnContentChanged?.Invoke(newContent);

                    Debug.Log($"✅ Image inserted at cursor position {cursorPos}: {relativePath}");
                    }
                }
            }

        void PasteContentAtCursor()
            {
            string clipboardContent = GUIUtility.systemCopyBuffer;
            if (!string.IsNullOrEmpty(clipboardContent))
                {
                string currentContent = _data.RawMarkdown ?? "";
                int insertPosition = Mathf.Clamp(_cursorPosition, 0, currentContent.Length);

                string newContent = currentContent.Insert(insertPosition, clipboardContent);
                _data.RawMarkdown = newContent;

                _cursorPosition = insertPosition + clipboardContent.Length;
                _focusTextArea = true;

                OnContentChanged?.Invoke(newContent);
                }
            }

        void FormatMarkdown()
            {
            // 🔧 ENHANCEMENT READY - Smart markdown formatting
            // Current: Basic formatting helpers
            // Enhancement path: Auto-indentation, table formatting, link cleanup

            string content = _data.RawMarkdown ?? "";

            // Clean up multiple empty lines
            content = Regex.Replace(content, @"\n{3,}", "\n\n");

            // Ensure headers have proper spacing
            content = Regex.Replace(content, @"^(#{1,6})\s*(.+)$", "$1 $2", RegexOptions.Multiline);

            _data.RawMarkdown = content;
            OnContentChanged?.Invoke(content);

            Debug.Log("✨ Markdown formatting applied");
            }

        void DrawStatusBar()
            {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                {
                string content = _data.RawMarkdown ?? "";
                int lineCount = content.Split('\n').Length;
                int charCount = content.Length;
                int wordCount = Regex.Matches(content, @"\b\w+\b").Count;

                GUILayout.Label($"📊 Lines: {lineCount} | Words: {wordCount} | Characters: {charCount}", EditorStyles.miniLabel);

                GUILayout.FlexibleSpace();

                GUILayout.Label($"🎯 Cursor: {_cursorPosition}", EditorStyles.miniLabel);

                // 🔧 Performance indicator
                if (_contentChanged)
                    {
                    GUILayout.Label("💾 Saving...", EditorStyles.miniLabel);
                    }
                }
            EditorGUILayout.EndHorizontal();
            }
        }
    }
