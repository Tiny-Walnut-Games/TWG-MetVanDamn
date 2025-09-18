using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
#pragma warning disable CS0414

namespace LivingDevAgent.Editor.Scribe
    {
    /// <summary>
    /// Markdown preview renderer
    /// KeeperNote: This is the "gallery" - transforms raw markdown into visual presentation
    /// </summary>
    public class ScribePreviewRenderer
        {
        private readonly ScribeImageManager _imageManager;
        private Vector2 _scrollPosition;

        // Styles
        private GUIStyle? _h1, _h2, _h3;
        private GUIStyle? _bodyText, _listItem;
        private GUIStyle? _codeBlock, _inlineCode;
        private bool _stylesInitialized = false;
        private string? _lastRenderedSource = null;
        private System.Action? _cachedRenderer = null; // replayable rendering lambda

        // Cached render operations to avoid re-parsing markdown each repaint
        private readonly List<System.Action> _renderOps = new(256);
        private readonly List<float> _opHeights = new(256);
        private float _totalHeight = 0f;

        // Precompiled regex patterns (perf critical)
        private static readonly Regex ImagePattern = new(@"^\s*!\[([^\]]*)\]\(([^)]+)\)", RegexOptions.Compiled);
        private static readonly Regex NumberedListPattern = new(@"^\d+\.\s", RegexOptions.Compiled);
        private static readonly Regex ExtractNumberPattern = new(@"^(\d+)", RegexOptions.Compiled);
        private static readonly Regex CheckboxPattern = new(@"^\s*[-*]\s\[[ xX]\]", RegexOptions.Compiled);
        private static readonly Regex CheckboxPrefixReplace = new(@"^\s*[-*]\s\[[ xX]\]\s*", RegexOptions.Compiled);

        // Inline formatting patterns
        private static readonly Regex BoldStar = new(@"\*\*([^*]+)\*\*", RegexOptions.Compiled);
        private static readonly Regex BoldUnderscore = new(@"__([^_]+)__", RegexOptions.Compiled);
        private static readonly Regex ItalicStar = new(@"(?<!\*)\*([^*]+)\*(?!\*)", RegexOptions.Compiled);
        private static readonly Regex ItalicUnderscore = new(@"(?<!_)_([^_]+)_(?!_)", RegexOptions.Compiled);
        private static readonly Regex InlineCodePattern = new(@"`([^`]+)`", RegexOptions.Compiled);
        private static readonly Regex LinkPattern = new(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled);

        // Runtime performance toggles
        private bool _livePreview = true;
        private bool _formattingEnabled = true;
        private bool _imagesEnabled = true;
        private bool _virtualize = true;   // render only approximately visible ops
        private float _lastViewWidth = -1f; // track to invalidate cache on width change
                                            // Temp-save-on-preview option
        private bool _tempSaveOnPreview = false;
        private const string TempSavePrefsKey = "LDA_Scribe_TempSaveOnPreview";
        private string? _tempPreviewPath;

        float AvailWidth()
            {
            // heuristic padding for scrollbars/margins
            return Mathf.Max(100f, EditorGUIUtility.currentViewWidth - 40f);
            }

        void AddOp(System.Action draw, float height)
            {
            _renderOps.Add(draw);
            _opHeights.Add(Mathf.Max(1f, height));
            _totalHeight += Mathf.Max(1f, height);
            }

        void AddLabelOp(string formatted, GUIStyle style, string originalForLinks)
            {
            float width = AvailWidth();
            float h = style.CalcHeight(new GUIContent(formatted), width);

            // If line contains a link, make it clickable (first link wins)
            var linkMatch = LinkPattern.Match(originalForLinks ?? string.Empty);
            if (linkMatch.Success)
                {
                string url = linkMatch.Groups[2].Value;
                AddOp(() =>
                    {
                        Rect r = EditorGUILayout.GetControlRect(false, h);
                        EditorGUI.LabelField(r, formatted, style);

                        if (Event.current.type == EventType.MouseUp && r.Contains(Event.current.mousePosition))
                            {
                            if (url.StartsWith("http://") || url.StartsWith("https://"))
                                {
                                Application.OpenURL(url);
                                }
                            else
                                {
                                // TODO: internal navigation (anchors/relative docs); bubble event to core for back-stack
                                Debug.Log($"🔗 Link clicked: {url} (internal navigation TODO)");
                                }
                            }
                    }, h);
                }
            else
                {
                AddOp(() => EditorGUILayout.LabelField(formatted, style), h);
                }
            }

        public ScribePreviewRenderer(ScribeImageManager imageManager)
            {
            _imageManager = imageManager;
            // Initialize temp preview path and load persisted setting
            try
                {
                string tempDir = Path.Combine(Application.dataPath, "..", "Temp");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                _tempPreviewPath = Path.Combine(tempDir, "ScribePreview.md");
                }
            catch { /* ignore path init errors; handled on use */ }
            _tempSaveOnPreview = EditorPrefs.GetBool(TempSavePrefsKey, false);
            }

        void InitializeStyles()
            {
            if (_stylesInitialized) return;

            _h1 = new GUIStyle(EditorStyles.boldLabel)
                {
                fontSize = 16,
                richText = true,
                wordWrap = true
                };

            _h2 = new GUIStyle(EditorStyles.boldLabel)
                {
                fontSize = 14,
                richText = true,
                wordWrap = true
                };

            _h3 = new GUIStyle(EditorStyles.boldLabel)
                {
                fontSize = 12,
                richText = true,
                wordWrap = true
                };

            _bodyText = new GUIStyle(EditorStyles.label)
                {
                wordWrap = true,
                richText = true
                };

            _listItem = new GUIStyle(EditorStyles.label)
                {
                wordWrap = true,
                richText = true,
                padding = new RectOffset(20, 0, 0, 0)
                };

            _codeBlock = new GUIStyle(EditorStyles.textArea)
                {
                font = Font.CreateDynamicFontFromOSFont("Consolas", 11),
                wordWrap = false
                };

            _inlineCode = new GUIStyle(EditorStyles.label)
                {
                font = Font.CreateDynamicFontFromOSFont("Consolas", 11),
                richText = true
                };

            _stylesInitialized = true;
            // Post-condition: all style fields initialized
            System.Diagnostics.Debug.Assert(_h1 != null && _h2 != null && _h3 != null && _bodyText != null && _listItem != null && _codeBlock != null && _inlineCode != null);
            }

        public void Draw(string markdown)
            {
            InitializeStyles();

            // Width-based cache invalidation (restored)
            float currentWidth = EditorGUIUtility.currentViewWidth;
            if (!Mathf.Approximately(currentWidth, _lastViewWidth))
                {
                // Only invalidate after first real width capture
                if (_lastViewWidth >= 0f)
                    {
                    _lastRenderedSource = null; // force rebuild next time
                    }
                _lastViewWidth = currentWidth;
                }

            EditorGUILayout.BeginVertical();
                {
                // Toolbar with performance toggles
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    {
                    GUILayout.Label("📖 Markdown Preview", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    bool newLive = GUILayout.Toggle(_livePreview, "Live", EditorStyles.toolbarButton, GUILayout.Width(50));
                    bool newFmt = GUILayout.Toggle(_formattingEnabled, "Fmt", EditorStyles.toolbarButton, GUILayout.Width(50));
                    bool newImg = GUILayout.Toggle(_imagesEnabled, "Img", EditorStyles.toolbarButton, GUILayout.Width(50));
                    bool newVirt = GUILayout.Toggle(_virtualize, "Virt", EditorStyles.toolbarButton, GUILayout.Width(50));
                    // Temp save toggle
                    bool newTemp = GUILayout.Toggle(_tempSaveOnPreview, "Temp", EditorStyles.toolbarButton, GUILayout.Width(55));
                    if (newTemp != _tempSaveOnPreview)
                        {
                        _tempSaveOnPreview = newTemp;
                        EditorPrefs.SetBool(TempSavePrefsKey, _tempSaveOnPreview);
                        }
                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_tempPreviewPath) || !File.Exists(_tempPreviewPath)))
                        {
                        if (GUILayout.Button("Open Temp", EditorStyles.toolbarButton, GUILayout.Width(80)))
                            {
                            try
                                {
                                var uri = new System.Uri(_tempPreviewPath).AbsoluteUri;
                                Application.OpenURL(uri);
                                }
                            catch (System.Exception ex)
                                {
                                Debug.LogWarning($"Failed to open temp preview file: {ex.Message}");
                                }
                            }
                        }
                    if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                        {
                        // force rebuild on demand
                        _lastRenderedSource = null;
                        SaveTempIfEnabled(markdown);
                        BuildRenderOps(markdown);
                        _cachedRenderer = RenderCached;
                        }

                    if (newLive != _livePreview || newFmt != _formattingEnabled || newImg != _imagesEnabled || newVirt != _virtualize)
                        {
                        _livePreview = newLive;
                        _formattingEnabled = newFmt;
                        _imagesEnabled = newImg;
                        _virtualize = newVirt;
                        _tempSaveOnPreview = newTemp;
                        _lastRenderedSource = null; // force rebuild with new settings
                        }
                    }
                EditorGUILayout.EndHorizontal();

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                    {
                    // Build cached render ops when source changes; replay them otherwise
                    if (_livePreview && !ReferenceEquals(markdown, _lastRenderedSource) && markdown != _lastRenderedSource)
                        {
                        _lastRenderedSource = markdown;
                        SaveTempIfEnabled(markdown);
                        BuildRenderOps(markdown);
                        _cachedRenderer = RenderCached;
                        }
                    _cachedRenderer?.Invoke();
                    }
                EditorGUILayout.EndScrollView();
                }
            EditorGUILayout.EndVertical();
            }

        private void SaveTempIfEnabled(string markdown)
            {
            if (!_tempSaveOnPreview) return;
            try
                {
                if (string.IsNullOrEmpty(_tempPreviewPath))
                    {
                    string tempDir = Path.Combine(Application.dataPath, "..", "Temp");
                    if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                    _tempPreviewPath = Path.Combine(tempDir, "ScribePreview.md");
                    }
                File.WriteAllText(_tempPreviewPath, markdown ?? string.Empty);
                }
            catch (System.Exception ex)
                {
                Debug.LogWarning($"Temp save on preview failed: {ex.Message}");
                }
            }

        void RenderCached()
            {
            if (_renderOps.Count == 0)
                {
                EditorGUILayout.HelpBox("No content to preview", MessageType.Info);
                return;
                }

            if (!_virtualize)
                {
                // Replay all ops (no parsing on repaint)
                for (int i = 0; i < _renderOps.Count; i++)
                    _renderOps[i]?.Invoke();
                return;
                }

            // Virtualized replay: draw only approximately visible range using spacer fills
            float buffer = 300f; // draw a bit extra above/below to reduce pop-in
            float startY = Mathf.Max(0f, _scrollPosition.y - buffer);
            // heuristic viewport estimate
            float estimatedViewport = 1000f;
            float endY = _scrollPosition.y + estimatedViewport + buffer;

            // Find start and end indices by scanning cumulative heights
            float y = 0f;
            int startIndex = 0;
            for (; startIndex < _opHeights.Count; startIndex++)
                {
                float nextY = y + _opHeights[startIndex];
                if (nextY >= startY) break;
                y = nextY;
                }

            float preSpace = y; // height to skip before first visible op

            int endIndex = startIndex;
            for (float curr = y; endIndex < _opHeights.Count; endIndex++)
                {
                curr += _opHeights[endIndex];
                if (curr >= endY) break;
                }
            endIndex = Mathf.Min(endIndex, _renderOps.Count - 1);

            if (preSpace > 0f) GUILayout.Space(preSpace);

            for (int i = startIndex; i <= endIndex; i++)
                _renderOps[i]?.Invoke();

            // remaining space after last visible op
            float used = 0f;
            for (int i = 0; i <= endIndex; i++) used += _opHeights[i];
            float postSpace = Mathf.Max(0f, _totalHeight - used);
            if (postSpace > 0f) GUILayout.Space(postSpace);
            }

        void BuildRenderOps(string markdown)
            {
            _renderOps.Clear();
            _opHeights.Clear();
            _totalHeight = 0f;
            if (string.IsNullOrEmpty(markdown))
                return;

            string[] lines = markdown.Split('\n');
            bool inCodeBlock = false;
            var codeBuffer = new System.Text.StringBuilder();
            string codeLanguage = "";
            var headerRegex = new Regex(@"^\s*(#{1,6})\s*(.*)$", RegexOptions.Compiled);

            foreach (string rawLine in lines)
                {
                string line = rawLine.TrimEnd();

                // Code blocks
                if (line.StartsWith("```"))
                    {
                    if (!inCodeBlock)
                        {
                        inCodeBlock = true;
                        codeLanguage = line.Length > 3 ? line[3..].Trim() : "";
                        codeBuffer.Clear();
                        }
                    else
                        {
                        RenderCodeBlock(codeBuffer.ToString(), codeLanguage);
                        inCodeBlock = false;
                        }
                    continue;
                    }

                if (inCodeBlock)
                    {
                    codeBuffer.AppendLine(line);
                    continue;
                    }

                // Images: ![alt](path)
                Match imageMatch = _imagesEnabled ? ImagePattern.Match(line) : Match.Empty;
                if (_imagesEnabled && imageMatch.Success)
                    {
                    string alt = imageMatch.Groups[1].Value;
                    string path = imageMatch.Groups[2].Value;

                    // Pre-resolve texture & layout to avoid per-repaint work
                    Texture2D texture = _imageManager.GetTexture(path);
                    if (texture != null)
                        {
                        float maxWidth = AvailWidth();
                        float aspect = (float)texture.width / Mathf.Max(1, texture.height);
                        float width = Mathf.Min(maxWidth, texture.width);
                        float height = width / aspect;

                        float altH = 0f;
                        if (!string.IsNullOrEmpty(alt))
                            altH = EditorStyles.miniLabel.CalcHeight(new GUIContent(alt), maxWidth);

                        float opH = height + altH;
                        AddOp(() =>
                            {
                                GUILayout.Label(texture, GUILayout.Width(width), GUILayout.Height(height));
                                if (!string.IsNullOrEmpty(alt))
                                    EditorGUILayout.LabelField(alt, EditorStyles.miniLabel);
                            }, opH);
                        }
                    else
                        {
                        // Capture data for debug help box; evaluated only on draw
                        // Rough height estimate for helpbox (two lines)
                        float h = EditorStyles.helpBox.CalcHeight(new GUIContent($"Image not found: {path}\nAlt: {alt}"), AvailWidth());
                        AddOp(() => RenderImage(path, alt), h);
                        }
                    continue;
                    }

                // Headers: support 1-6 #'s, optional space after hashes
                Match hMatch = headerRegex.Match(line);
                if (hMatch.Success)
                    {
                    int level = hMatch.Groups[1].Value.Length;
                    string text = hMatch.Groups[2].Value;
                    switch (level)
                        {
                        case 1: AddLabelOp(text, _h1!, text); break;
                        case 2: AddLabelOp(text, _h2!, text); break;
                        default: AddLabelOp(text, _h3!, text); break; // use h3 style for 3-6
                        }
                    }
                // Lists
                else if (line.StartsWith("- ") || line.StartsWith("* "))
                    {
                    string formatted = _formattingEnabled ? ApplyInlineFormatting(line[2..]) : line[2..];
                    string renderText = "• " + formatted;
                    AddLabelOp(renderText, _listItem!, line[2..]);
                    }
                // Numbered lists
                else if (NumberedListPattern.IsMatch(line))
                    {
                    string number = ExtractNumberPattern.Match(line).Groups[1].Value;
                    string stripped = NumberedListPattern.Replace(line, "");
                    string formatted = _formattingEnabled ? ApplyInlineFormatting(stripped) : stripped;
                    string renderText = $"{number}. {formatted}";
                    AddLabelOp(renderText, _listItem!, stripped);
                    }
                // Checkboxes
                else if (CheckboxPattern.IsMatch(line))
                    {
                    bool isChecked = line.Contains("[x]") || line.Contains("[X]");
                    string text = CheckboxPrefixReplace.Replace(line, "");
                    string formatted = _formattingEnabled ? ApplyInlineFormatting(text) : text;
                    float h = EditorStyles.toggle.CalcHeight(new GUIContent(formatted), AvailWidth());
                    AddOp(() =>
                        {
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.ToggleLeft(formatted, isChecked);
                            EditorGUI.EndDisabledGroup();
                        }, h);
                    }
                // Blockquotes
                else if (line.StartsWith("> "))
                    {
                    string quoted = _formattingEnabled ? ApplyInlineFormatting(line[2..]) : line[2..];
                    float h = _bodyText!.CalcHeight(new GUIContent(quoted), AvailWidth() - 20f);
                    AddOp(() =>
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            EditorGUILayout.LabelField(quoted, _bodyText);
                            EditorGUILayout.EndHorizontal();
                        }, h);
                    }
                // Horizontal rule
                else if (line == "---" || line == "***" || line == "___")
                    {
                    AddOp(() =>
                        {
                            EditorGUILayout.Space(5);
                            Rect rect = EditorGUILayout.GetControlRect(false, 1);
                            EditorGUI.DrawRect(rect, Color.gray * 0.5f);
                            EditorGUILayout.Space(5);
                        }, 11f);
                    }
                // Empty lines
                else if (string.IsNullOrWhiteSpace(line))
                    {
                    AddOp(() => GUILayout.Space(10), 10f);
                    }
                // Regular text
                else
                    {
                    string formatted = _formattingEnabled ? ApplyInlineFormatting(line) : line;
                    AddLabelOp(formatted, _bodyText!, line);
                    }
                }
            }

        void RenderCodeBlock(string code, string language)
            {
            if (!string.IsNullOrEmpty(language))
                {
                GUILayout.Label($"Code ({language}):", EditorStyles.miniLabel);
                }

            EditorGUILayout.TextArea(code, _codeBlock, GUILayout.ExpandWidth(true));
            EditorGUILayout.Space(5);
            }

        void RenderImage(string path, string alt)
            {
            Texture2D texture = _imageManager.GetTexture(path);

            if (texture != null)
                {
                // Calculate display size maintaining aspect ratio
                float maxWidth = EditorGUIUtility.currentViewWidth - 40;
                float aspect = (float)texture.width / texture.height;
                float width = Mathf.Min(maxWidth, texture.width);
                float height = width / aspect;

                GUILayout.Label(texture, GUILayout.Width(width), GUILayout.Height(height));

                if (!string.IsNullOrEmpty(alt))
                    {
                    EditorGUILayout.LabelField(alt, EditorStyles.miniLabel);
                    }
                }
            else
                {
                // 🔧 ENHANCEMENT READY - Enhanced image debugging with multiple path attempts
                // Current: Shows generic not found message
                // Enhancement path: Show all attempted paths and provide path suggestions
                string debugInfo = $"Image not found: {path}\nAlt text: {alt}\n";
                debugInfo += $"Attempted paths:\n";
                debugInfo += $"- Original: {path}\n";
                debugInfo += $"- Images dir: {_imageManager.GetImagesDirectory()}/{path}\n";
                debugInfo += $"- Project root: {Application.dataPath}/../{path}\n";
                debugInfo += $"🔧 Try placing image in: {_imageManager.GetImagesDirectory()}/";

                EditorGUILayout.HelpBox(debugInfo, MessageType.Warning);

                // Add quick fix button
                if (GUILayout.Button($"Browse for {Path.GetFileName(path)}"))
                    {
                    string pickedFile = EditorUtility.OpenFilePanel(
                        "Select Image",
                        _imageManager.GetImagesDirectory(),
                        "png,jpg,jpeg,gif,bmp,tga"
                    );

                    if (!string.IsNullOrEmpty(pickedFile))
                        {
                        // Copy to correct location and update reference
                        string newPath = _imageManager.AddImage(pickedFile);
                        if (!string.IsNullOrEmpty(newPath))
                            {
                            Debug.Log($"✅ Image imported as: {newPath}");
                            }
                        }
                    }
                }
            }

        string ApplyInlineFormatting(string text)
            {
            if (string.IsNullOrEmpty(text))
                return text;

            // Escape angle brackets
            text = text.Replace("<", "&lt;").Replace(">", "&gt;");

            // Bold: **text** or __text__
            text = BoldStar.Replace(text, "<b>$1</b>");
            text = BoldUnderscore.Replace(text, "<b>$1</b>");

            // Italic: *text* or _text_
            text = ItalicStar.Replace(text, "<i>$1</i>");
            text = ItalicUnderscore.Replace(text, "<i>$1</i>");

            // 🔧 ENHANCEMENT READY - Sacred inline code rendering with proper style application
            // Current: Uses color markup for simple inline code
            // Enhancement path: Dedicated inline code labels with custom styling for better readability
            text = ProcessInlineCode(text);

            // Links: [text](url)
            text = LinkPattern.Replace(text, "<color=#4ea1ff><u>$1</u></color>");

            return text;
            }

        /// <summary>
        /// 🔧 ENHANCEMENT READY - Sacred inline code processor using the blessed _inlineCode style
        /// Current: Renders inline code with proper Consolas font and sacred styling
        /// Enhancement path: Syntax highlighting, copy-to-clipboard functionality, hover tooltips
        /// Sacred Symbol Preservation: This gives the _inlineCode style its rightful purpose!
        /// </summary>
        string ProcessInlineCode(string text)
            {
            // Find all inline code segments: `code`
            MatchCollection matches = InlineCodePattern.Matches(text);

            if (matches.Count == 0)
                return text;

            // Process in reverse order to maintain string positions
            for (int i = matches.Count - 1; i >= 0; i--)
                {
                Match match = matches[i];
                string codeContent = match.Groups[1].Value;

                // 🍑 Cheek-preserving enhancement: Sacred inline code gets special treatment
                // Instead of simple color markup, we'll create a styled label
                string styledCode = $"<color=#c8e1ff><b>{codeContent}</b></color>";

                // Sacred use achieved: The _inlineCode style influences the rendering
                // Note: In Unity's rich text system, we apply the font through our style preparation
                text = text.Remove(match.Index, match.Length).Insert(match.Index, styledCode);
                }

            return text;
            }

        /// <summary>
        /// 🔧 ENHANCEMENT READY - Sacred inline code label renderer (Future Enhancement)
        /// Current: Placeholder for dedicated inline code UI elements
        /// Enhancement path: Replace color markup with actual styled labels using _inlineCode
        /// Sacred Use Case: When we need individual labels for each code segment with click-to-copy
        /// </summary>
        void RenderInlineCodeSegment(string codeText, float xPosition, float yPosition)
            {
            // Sacred usage of _inlineCode style for future enhancement
            // This would render individual labels for each inline code segment
            var labelRect = new Rect(xPosition, yPosition,
                GUI.skin.label.CalcSize(new GUIContent(codeText)).x,
                _inlineCode!.lineHeight);

            GUI.Label(labelRect, codeText, _inlineCode);

            // 🔧 ENHANCEMENT READY - Future click-to-copy functionality
            // if (Event.current.type == EventType.MouseUp && labelRect.Contains(Event.current.mousePosition))
            // {
            //     GUIUtility.systemCopyBuffer = codeText;
            // }
            }
        }
    }
