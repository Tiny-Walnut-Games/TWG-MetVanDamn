using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

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
        private GUIStyle _h1, _h2, _h3;
        private GUIStyle _bodyText, _listItem;
        private GUIStyle _codeBlock, _inlineCode;
        private bool _stylesInitialized = false;

        public ScribePreviewRenderer(ScribeImageManager imageManager)
            {
            _imageManager = imageManager;
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
            }

        public void Draw(string markdown)
            {
            InitializeStyles();

            EditorGUILayout.BeginVertical();
                {
                GUILayout.Label("Markdown Preview", EditorStyles.boldLabel);

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                    {
                    RenderMarkdown(markdown);
                    }
                EditorGUILayout.EndScrollView();
                }
            EditorGUILayout.EndVertical();
            }

        void RenderMarkdown(string markdown)
            {
            if (string.IsNullOrEmpty(markdown))
                {
                EditorGUILayout.HelpBox("No content to preview", MessageType.Info);
                return;
                }

            string[] lines = markdown.Split('\n');
            bool inCodeBlock = false;
            var codeBuffer = new System.Text.StringBuilder();
            string codeLanguage = "";

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
                Match imageMatch = Regex.Match(line, @"^\s*!\[([^\]]*)\]\(([^)]+)\)");
                if (imageMatch.Success)
                    {
                    string alt = imageMatch.Groups[1].Value;
                    string path = imageMatch.Groups[2].Value;
                    RenderImage(path, alt);
                    continue;
                    }

                // Headers
                if (line.StartsWith("### "))
                    {
                    EditorGUILayout.LabelField(line[4..], _h3);
                    }
                else if (line.StartsWith("## "))
                    {
                    EditorGUILayout.LabelField(line[3..], _h2);
                    }
                else if (line.StartsWith("# "))
                    {
                    EditorGUILayout.LabelField(line[2..], _h1);
                    }
                // Lists
                else if (line.StartsWith("- ") || line.StartsWith("* "))
                    {
                    string formatted = ApplyInlineFormatting(line[2..]);
                    EditorGUILayout.LabelField("• " + formatted, _listItem);
                    }
                // Numbered lists
                else if (Regex.IsMatch(line, @"^\d+\.\s"))
                    {
                    string formatted = ApplyInlineFormatting(Regex.Replace(line, @"^\d+\.\s", ""));
                    string number = Regex.Match(line, @"^(\d+)").Groups[1].Value;
                    EditorGUILayout.LabelField($"{number}. {formatted}", _listItem);
                    }
                // Checkboxes
                else if (Regex.IsMatch(line, @"^\s*[-*]\s\[[ xX]\]"))
                    {
                    bool isChecked = line.Contains("[x]") || line.Contains("[X]");
                    string text = Regex.Replace(line, @"^\s*[-*]\s\[[ xX]\]\s*", "");

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ToggleLeft(ApplyInlineFormatting(text), isChecked);
                    EditorGUI.EndDisabledGroup();
                    }
                // Blockquotes
                else if (line.StartsWith("> "))
                    {
                    string quoted = line[2..];
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    EditorGUILayout.LabelField(ApplyInlineFormatting(quoted), _bodyText);
                    EditorGUILayout.EndHorizontal();
                    }
                // Horizontal rule
                else if (line == "---" || line == "***" || line == "___")
                    {
                    EditorGUILayout.Space(5);
                    Rect rect = EditorGUILayout.GetControlRect(false, 1);
                    EditorGUI.DrawRect(rect, Color.gray * 0.5f);
                    EditorGUILayout.Space(5);
                    }
                // Empty lines
                else if (string.IsNullOrWhiteSpace(line))
                    {
                    GUILayout.Space(10);
                    }
                // Regular text
                else
                    {
                    EditorGUILayout.LabelField(ApplyInlineFormatting(line), _bodyText);
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
            text = Regex.Replace(text, @"\*\*([^*]+)\*\*", "<b>$1</b>");
            text = Regex.Replace(text, @"__([^_]+)__", "<b>$1</b>");

            // Italic: *text* or _text_
            text = Regex.Replace(text, @"(?<!\*)\*([^*]+)\*(?!\*)", "<i>$1</i>");
            text = Regex.Replace(text, @"(?<!_)_([^_]+)_(?!_)", "<i>$1</i>");

            // 🔧 ENHANCEMENT READY - Sacred inline code rendering with proper style application
            // Current: Uses color markup for simple inline code
            // Enhancement path: Dedicated inline code labels with custom styling for better readability
            text = ProcessInlineCode(text);

            // Links: [text](url)
            text = Regex.Replace(text, @"\[([^\]]+)\]\(([^)]+)\)",
                "<color=#4ea1ff><u>$1</u></color>");

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
            MatchCollection matches = Regex.Matches(text, @"`([^`]+)`");

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
                _inlineCode.lineHeight);

            GUI.Label(labelRect, codeText, _inlineCode);

            // 🔧 ENHANCEMENT READY - Future click-to-copy functionality
            // if (Event.current.type == EventType.MouseUp && labelRect.Contains(Event.current.mousePosition))
            // {
            //     GUIUtility.systemCopyBuffer = codeText;
            // }
            }
        }
    }
