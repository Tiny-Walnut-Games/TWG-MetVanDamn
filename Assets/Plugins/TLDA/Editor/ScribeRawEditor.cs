using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace LivingDevAgent.Editor.Scribe
    {
    /// <summary>
    /// High-performance raw markdown editor with optional Script mode.
    /// </summary>
    public class ScribeRawEditor
        {
        private readonly ScribeDataManager _data;
        private readonly ScribeImageManager _imageManager;

        private Vector2 _scrollPosition;
        private Vector2 _editScroll;
        private string _lastMarkdown = string.Empty;
        private bool _contentChanged = false;
        private double _nextNotifyTime = 0f;
        private const double ChangeNotifyDebounce = 0.12; // seconds
        private bool _notifyScheduled = false;

        private const string ScriptModePrefsKey = "LDA_Scribe_ScriptMode";
        private const int LargeDocCharThreshold = 200_000; // switch to virtualized viewer above this size
        private bool _scriptMode;
        private bool _forceEditLargeDoc = false; // allow slow editing override

        // Cursor tracking
        private int _cursorPosition = 0;
        private bool _focusTextArea = false;

        // Cached style
        private GUIStyle _textAreaStyle;
        private bool _styleInitialized = false;
        private GUIStyle _viewerStyle; // for virtualized read-only view

        // Cached stats for status bar and height calc
        private string _lastStatsSource = string.Empty;
        private int _cachedLineCount, _cachedWordCount, _cachedCharCount;

        public event System.Action<string> OnContentChanged;

        public ScribeRawEditor(ScribeDataManager data, ScribeImageManager imageManager)
            {
            _data = data;
            _imageManager = imageManager;
            _scriptMode = EditorPrefs.GetBool(ScriptModePrefsKey, false);
            }

        private void InitializeStyle()
            {
            if (_styleInitialized) return;

            _textAreaStyle = new GUIStyle(EditorStyles.textArea)
                {
                font = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Monaco", "Lucida Console" }, 12),
                wordWrap = !_scriptMode, // script mode disables wrapping
                richText = false
                };

            _viewerStyle = new GUIStyle(EditorStyles.label)
                {
                font = _textAreaStyle.font,
                wordWrap = false,
                richText = false
                };

            _styleInitialized = true;
            }

        public void Draw()
            {
            InitializeStyle();

            EditorGUILayout.BeginVertical();
                {
                DrawToolbar();
                // Reserve a content rect that flexes between toolbar and status bar
                Rect contentRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                // Draw editor inside the reserved area to avoid layout compression issues
                GUILayout.BeginArea(contentRect);
                DrawEditor(contentRect);
                GUILayout.EndArea();
                // Small spacer to guarantee status bar visibility even under tight layouts
                GUILayout.Space(2);
                // Status bar anchored at bottom
                DrawStatusBar();
                }
            EditorGUILayout.EndVertical();
            }

        private void DrawToolbar()
            {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                {
                GUILayout.Label("✏️ Raw Editor", EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                // Script mode toggle
                bool newScript = GUILayout.Toggle(_scriptMode, "Script", EditorStyles.toolbarButton, GUILayout.Width(60));
                if (newScript != _scriptMode)
                    {
                    _scriptMode = newScript;
                    EditorPrefs.SetBool(ScriptModePrefsKey, _scriptMode);
                    _styleInitialized = false; // re-init style for wrapping
                    }

                // Large doc override (enable slow editing)
                bool largeMode = IsLargeDoc(_data.RawMarkdown);
                using (new EditorGUI.DisabledScope(!largeMode))
                    {
                    bool newOverride = GUILayout.Toggle(_forceEditLargeDoc, "Edit Large (slow)", EditorStyles.toolbarButton, GUILayout.Width(130));
                    if (newOverride != _forceEditLargeDoc)
                        {
                        _forceEditLargeDoc = newOverride;
                        // Ensure edit view renders immediately
                        if (_forceEditLargeDoc)
                            {
                            _focusTextArea = true;
                            }
                        InternalEditorUtility.RepaintAllViews();
                        EditorApplication.QueuePlayerLoopUpdate();
                        }
                    }

                if (GUILayout.Button("Open External", EditorStyles.toolbarButton))
                    {
                    OpenInExternalEditor(_data.RawMarkdown ?? string.Empty);
                    }

                // Insert/Paste/Format (disabled in Script mode)
                EditorGUI.BeginDisabledGroup(_scriptMode);
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
                EditorGUI.EndDisabledGroup();
                }
            EditorGUILayout.EndHorizontal();
            }

        private void DrawEditor(Rect contentRect)
            {
            EditorGUILayout.LabelField(_scriptMode ? "Script Content:" : "Markdown Content:", EditorStyles.boldLabel);

            // Stats cache (cheap line count for height calc)
            string currentContent = _data.RawMarkdown ?? string.Empty;
            if (!ReferenceEquals(currentContent, _lastStatsSource) && currentContent != _lastStatsSource)
                {
                _lastStatsSource = currentContent;
                int lines = 1;
                for (int i = 0; i < currentContent.Length; i++) if (currentContent[i] == '\n') lines++;
                _cachedLineCount = lines;
                _cachedCharCount = currentContent.Length;
                _cachedWordCount = Regex.Matches(currentContent, @"\b\w+\b").Count;
                }

            // Large doc virtualized viewer (read-only by default)
            bool useVirtualViewer = IsLargeDoc(currentContent) && !_forceEditLargeDoc;
            if (useVirtualViewer)
                {
                DrawVirtualizedViewer(currentContent, Mathf.Max(200f, contentRect.height));
                return;
                }

            // Editable TextArea: rely on built-in internal scrolling instead of an outer ScrollView
            // Focus request
            if (_focusTextArea)
                {
                GUI.FocusControl("RawMarkdownEditor");
                _focusTextArea = false;
                }

            GUI.SetNextControlName("RawMarkdownEditor");
            // Horizontal scroller wrapper so long lines in Script mode don't expand the window
            EditorGUILayout.BeginVertical(GUILayout.Height(Mathf.Max(200f, contentRect.height)));
                {
                // Horizontal only, let TextArea manage vertical
                _editScroll = EditorGUILayout.BeginScrollView(
                    _editScroll,
                    true,  // show horizontal
                    false, // no vertical (TextArea handles it)
                    GUI.skin.horizontalScrollbar,
                    GUIStyle.none,
                    GUIStyle.none,
                    GUILayout.Height(Mathf.Max(200f, contentRect.height)),
                    GUILayout.MinHeight(200)
                );
                    {
                    float availWidth = Mathf.Max(200f, EditorGUIUtility.currentViewWidth - 40f);
                    float estimatedContentWidth = EstimateContentWidth(currentContent, availWidth, _textAreaStyle);
                    // Constrain to content width within the horizontal scroll view
                    Rect editorRect = GUILayoutUtility.GetRect(
                        estimatedContentWidth,
                        Mathf.Max(200f, contentRect.height - 4f),
                        _textAreaStyle,
                        GUILayout.Width(estimatedContentWidth),
                        GUILayout.ExpandWidth(false),
                        GUILayout.Height(Mathf.Max(200f, contentRect.height - 4f)),
                        GUILayout.MinHeight(200)
                    );
                    string newContent = EditorGUI.TextArea(editorRect, currentContent, _textAreaStyle);

                    // Cursor tracking only if focused
                    if (GUI.GetNameOfFocusedControl() == "RawMarkdownEditor")
                        {
                        var textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                        if (textEditor != null)
                            {
                            _cursorPosition = textEditor.cursorIndex;
                            }
                        }

                    // Debounced content change notification
                    if (newContent != _lastMarkdown)
                        {
                        _data.RawMarkdown = newContent;
                        _lastMarkdown = newContent;
                        _contentChanged = true;
                        _nextNotifyTime = EditorApplication.timeSinceStartup + GetEffectiveDebounce();
                        if (!_notifyScheduled)
                            {
                            EditorApplication.update += NotifyIfDue;
                            _notifyScheduled = true;
                            }
                        // Avoid forcing global repaints on every keystroke; IMGUI will repaint naturally
                        }
                    }
                EditorGUILayout.EndScrollView();
                }
            EditorGUILayout.EndVertical();

            // Note: Status is drawn by parent container to keep it anchored at bottom
            }

        // -------- Large Doc Virtualized Viewer --------
        private string _lastIndexedSource = string.Empty;
        private List<int> _lineOffsets = new(1024);
        private int _indexedLineCount = 0;

        private bool IsLargeDoc(string content)
            => !string.IsNullOrEmpty(content) && content.Length >= LargeDocCharThreshold;

        private void EnsureLineIndex(string content)
            {
            if (ReferenceEquals(content, _lastIndexedSource) || content == _lastIndexedSource)
                return;

            _lineOffsets.Clear();
            _lineOffsets.Add(0);
            for (int i = 0; i < content.Length; i++)
                {
                if (content[i] == '\n')
                    _lineOffsets.Add(i + 1);
                }
            _indexedLineCount = _lineOffsets.Count;
            _lastIndexedSource = content;
            }

        private void DrawVirtualizedViewer(string content, float viewportHeight)
            {
            EnsureLineIndex(content);

            float lineH = _viewerStyle.lineHeight > 0 ? _viewerStyle.lineHeight : 16f;
            // Use provided viewport height; fallback to a reasonable default
            float viewportH = Mathf.Max(300f, viewportHeight);
            float totalH = Mathf.Max(600f, _indexedLineCount * lineH + 20f);

            // Lightweight mode banner (doesn't steal much space)
            EditorGUILayout.LabelField("Large document mode: read-only virtualized view for performance. Toggle 'Edit Large (slow)' to edit inline or use 'Open External'.", EditorStyles.miniLabel);

            EditorGUILayout.BeginVertical(GUILayout.Height(viewportH))
                ;
                {
                _scrollPosition = EditorGUILayout.BeginScrollView(
                    _scrollPosition,
                    false, // horizontal always
                    true,  // vertical always
                    GUI.skin.horizontalScrollbar,
                    GUI.skin.verticalScrollbar,
                    GUIStyle.none,
                    GUILayout.Height(viewportH),
                    GUILayout.MinHeight(300)
                );
                    {
                    // Determine visible line window
                    float bufferPx = 400f;
                    float startY = Mathf.Max(0f, _scrollPosition.y - bufferPx);
                    int startLine = Mathf.Max(0, (int)(startY / lineH));
                    float drawH = viewportH + 2 * bufferPx;
                    int linesToDraw = Mathf.Min(_indexedLineCount - startLine, Mathf.CeilToInt(drawH / lineH) + 20);

                    // Spacer before
                    if (startLine > 0)
                        GUILayout.Space(startLine * lineH);

                    // Draw visible lines
                    for (int i = 0; i < linesToDraw; i++)
                        {
                        int lineIndex = startLine + i;
                        if (lineIndex >= _indexedLineCount) break;

                        int start = _lineOffsets[lineIndex];
                        int end = (lineIndex + 1 < _indexedLineCount) ? _lineOffsets[lineIndex + 1] - 1 : content.Length;
                        int len = Mathf.Max(0, end - start);
                        string slice = len > 0 ? content.Substring(start, len) : string.Empty;

                        EditorGUILayout.LabelField(slice, _viewerStyle, GUILayout.ExpandWidth(true));
                        }

                    // Spacer after
                    float usedH = (startLine + linesToDraw) * lineH;
                    if (totalH - usedH > 0f)
                        GUILayout.Space(totalH - usedH);
                    }
                EditorGUILayout.EndScrollView();
                }
            EditorGUILayout.EndVertical();

            // Keep UI responsive during scrollbar drags (especially when pointer leaves control)
            if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
                {
                InternalEditorUtility.RepaintAllViews();
                EditorApplication.QueuePlayerLoopUpdate();
                }
            }

        private void OpenInExternalEditor(string content)
            {
            try
                {
                string tempDir = Path.Combine(Application.dataPath, "..", "Temp");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                string path = Path.Combine(tempDir, "ScribeLargeDoc.md");
                File.WriteAllText(path, content ?? string.Empty);
                var uri = new System.Uri(path).AbsoluteUri; // file:/// URI
                Application.OpenURL(uri);
                }
            catch (System.Exception ex)
                {
                Debug.LogWarning($"Failed to open external editor: {ex.Message}");
                }
            }

        private void InsertImageAtCursor()
            {
            string imagePath = EditorUtility.OpenFilePanel(
                "Select Image",
                _imageManager.GetImagesDirectory(),
                "png,jpg,jpeg,gif,bmp,tga"
            );

            if (!string.IsNullOrEmpty(imagePath))
                {
                string relativePath = _imageManager.AddImage(imagePath);
                if (!string.IsNullOrEmpty(relativePath))
                    {
                    string imageMarkdown = $"![Image]({relativePath})\n";
                    string currentContent = _data.RawMarkdown ?? string.Empty;

                    GUI.FocusControl("RawMarkdownEditor");
                    var textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                    int cursorPos = textEditor?.cursorIndex ?? currentContent.Length;
                    cursorPos = Mathf.Clamp(cursorPos, 0, currentContent.Length);

                    string newContent = currentContent.Insert(cursorPos, imageMarkdown);
                    _data.RawMarkdown = newContent;

                    EditorApplication.delayCall += () =>
                    {
                        if (textEditor != null)
                            {
                            textEditor.cursorIndex = cursorPos + imageMarkdown.Length;
                            textEditor.selectIndex = textEditor.cursorIndex;
                            }
                    };

                    OnContentChanged?.Invoke(newContent);
                    }
                }
            }

        private void PasteContentAtCursor()
            {
            string clipboardContent = GUIUtility.systemCopyBuffer;
            if (!string.IsNullOrEmpty(clipboardContent))
                {
                string currentContent = _data.RawMarkdown ?? string.Empty;
                int insertPosition = Mathf.Clamp(_cursorPosition, 0, currentContent.Length);

                string newContent = currentContent.Insert(insertPosition, clipboardContent);
                _data.RawMarkdown = newContent;

                _cursorPosition = insertPosition + clipboardContent.Length;
                _focusTextArea = true;

                OnContentChanged?.Invoke(newContent);
                }
            }

        private void FormatMarkdown()
            {
            string content = _data.RawMarkdown ?? string.Empty;
            content = Regex.Replace(content, @"\n{3,}", "\n\n");
            content = Regex.Replace(content, @"^(#{1,6})\s*(.+)$", "$1 $2", RegexOptions.Multiline);

            _data.RawMarkdown = content;
            OnContentChanged?.Invoke(content);
            }

        private void DrawStatusBar()
            {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                {
                GUILayout.Label($"📊 Lines: {_cachedLineCount} | Words: {_cachedWordCount} | Characters: {_cachedCharCount}", EditorStyles.miniLabel);

                GUILayout.FlexibleSpace();

                GUILayout.Label($"🎯 Cursor: {_cursorPosition}", EditorStyles.miniLabel);
                if (_scriptMode)
                    GUILayout.Label("🔒 Script mode", EditorStyles.miniLabel);

                if (_contentChanged)
                    {
                    GUILayout.Label("💾 Saving...", EditorStyles.miniLabel);
                    }
                }
            EditorGUILayout.EndHorizontal();
            }

        private void NotifyIfDue()
            {
            if (!_contentChanged)
                {
                EditorApplication.update -= NotifyIfDue;
                _notifyScheduled = false;
                return;
                }

            if (EditorApplication.timeSinceStartup >= _nextNotifyTime)
                {
                _contentChanged = false;
                OnContentChanged?.Invoke(_data.RawMarkdown ?? string.Empty);
                EditorApplication.update -= NotifyIfDue;
                _notifyScheduled = false;
                }
            }

        private double GetEffectiveDebounce()
            {
            // Increase debounce for heavy scenarios to reduce churn
            if (IsLargeDoc(_data.RawMarkdown)) return 0.35;
            if (_scriptMode) return 0.25;
            return ChangeNotifyDebounce;
            }

        private float EstimateContentWidth(string content, float availWidth, GUIStyle style)
            {
            // Estimate based on max line length across first N lines and the current line
            int maxLines = 1500;
            int maxLen = 0;
            int scanned = 0;
            int len = content?.Length ?? 0;
            int lineLen = 0;
            for (int i = 0; i < len && scanned < maxLines; i++)
                {
                char c = content[i];
                if (c == '\n')
                    {
                    if (lineLen > maxLen) maxLen = lineLen;
                    lineLen = 0;
                    scanned++;
                    }
                else
                    {
                    lineLen++;
                    }
                }
            if (lineLen > maxLen) maxLen = lineLen;

            // Include the active line around cursor to avoid sudden width jumps
            try
                {
                var te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                if (te != null && content != null)
                    {
                    int start = content.LastIndexOf('\n', Mathf.Clamp(te.cursorIndex - 1, 0, content.Length - 1));
                    int end = content.IndexOf('\n', te.cursorIndex);
                    if (start < 0) start = 0; else start += 1;
                    if (end < 0) end = content.Length;
                    int currLen = Mathf.Max(0, end - start);
                    if (currLen > maxLen) maxLen = currLen;
                    }
                }
            catch { }

            // Estimate character width using 'W' in our monospace font
            float charW = style != null ? style.CalcSize(new GUIContent("W")).x : 8f;
            charW = Mathf.Max(6f, charW);
            float desired = 20f + maxLen * charW;
            // Never smaller than the view, cap to keep scroll smooth
            return Mathf.Clamp(desired, availWidth, 200000f);
            }
        }
    }
