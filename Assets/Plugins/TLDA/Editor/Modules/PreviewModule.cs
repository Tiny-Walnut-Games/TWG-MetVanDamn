#if UNITY_EDITOR
using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace LivingDevAgent.Editor.Modules
	{
	/// <summary>
	/// 👁️ Preview Module - The Live Markdown Renderer!
	/// Displays rendered markdown with emoji support and formatted output.
	/// </summary>
	public class PreviewModule : ScribeModuleBase
		{
		public PreviewModule (TLDLScribeData data) : base(data) { }

		public void DrawContent (Rect windowPosition)
			{
			// Always render from raw if available, otherwise show placeholder
			string md = !string.IsNullOrEmpty(this._data.RawContent) ? this._data.RawContent : this.GetPlaceholderMarkdown();

			using (new EditorGUILayout.VerticalScope())
				{
				// Header
				using (new EditorGUILayout.HorizontalScope())
					{
					var headerContent = new GUIContent("👁️ Markdown Preview");
					GUILayout.Label(headerContent, EditorStyles.boldLabel);

					GUILayout.FlexibleSpace();

					if (GUILayout.Button("🔄", EditorStyles.miniButton, GUILayout.Width(30)))
						{
						this.SetStatus("🔄 Preview refreshed");
						}
					}

				// Preview area
				float viewportHeight = Mathf.Max(120f, windowPosition.height - 220f);
				this._data.PreviewScroll = EditorGUILayout.BeginScrollView(this._data.PreviewScroll, GUILayout.Height(viewportHeight), GUILayout.ExpandHeight(true));

				this.RenderMarkdown(md);

				EditorGUILayout.EndScrollView();

				// Bottom info
				this.DrawPreviewInfo();
				}
			}

		private void DrawPreviewInfo ()
			{
			using (new EditorGUILayout.HorizontalScope())
				{
				if (!string.IsNullOrEmpty(this._data.RawContent))
					{
					int lines = this._data.RawContent.Split('\n').Length;
					int chars = this._data.RawContent.Length;
					GUILayout.Label($"📊 {lines} lines, {chars} characters", EditorStyles.miniLabel);
					}
				else
					{
					GUILayout.Label("📄 No content to preview", EditorStyles.miniLabel);
					}

				GUILayout.FlexibleSpace();

				if (this._data.RawDirty)
					{
					GUILayout.Label("✏️ Unsaved changes", EditorStyles.miniLabel);
					}
				}
			}

		private void RenderMarkdown (string md)
			{
			if (string.IsNullOrEmpty(md))
				{
				EditorGUILayout.HelpBox("📝 No content to preview. Switch to Form or Editor tab to create content.", MessageType.Info);
				return;
				}

			string [ ] lines = md.Split(new [ ] { "\r\n", "\n" }, StringSplitOptions.None);
			bool inCode = false;
			var codeBuffer = new StringBuilder();

			foreach (string raw in lines)
				{
				string line = raw;

				// Code block handling
				if (line.StartsWith("```"))
					{
					if (!inCode)
						{
						inCode = true;
						codeBuffer.Length = 0;
						}
					else
						{
						EditorGUILayout.TextArea(codeBuffer.ToString(), _codeBlock, GUILayout.ExpandWidth(true));
						inCode = false;
						}
					continue;
					}

				if (inCode)
					{
					codeBuffer.AppendLine(line);
					continue;
					}

				// Markdown image: ![alt](path)
				Match imgMatch = Regex.Match(line, @"^\s*!\[([^\]]*)\]\(([^)]+)\)\s*$");
				if (imgMatch.Success)
					{
					string alt = imgMatch.Groups [ 1 ].Value;
					string path = imgMatch.Groups [ 2 ].Value;
					this.RenderImagePlaceholder(path, alt);
					continue;
					}

				// Checkbox list items: - [ ] and - [x]
				if (Regex.IsMatch(line, @"^\s*[-*]\s\[( |x|X)\]\s"))
					{
					bool isChecked = line.IndexOf("[x]", StringComparison.OrdinalIgnoreCase) >= 0;
					string text = Regex.Replace(line, @"^\s*[-*]\s\[( |x|X)\]\s", "");
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.ToggleLeft(this.ApplyInlineFormatting(text), isChecked);
					EditorGUI.EndDisabledGroup();
					continue;
					}

				// Headers
				if (line.StartsWith("### "))
					{
					EditorGUILayout.LabelField(this.ApplyInlineFormatting(line [ 4.. ]), _h3);
					}
				else if (line.StartsWith("## "))
					{
					EditorGUILayout.LabelField(this.ApplyInlineFormatting(line [ 3.. ]), _h2);
					}
				else if (line.StartsWith("# "))
					{
					EditorGUILayout.LabelField(this.ApplyInlineFormatting(line [ 2.. ]), _h1);
					}
				// List items
				else if (line.StartsWith("- ") || line.StartsWith("* "))
					{
					EditorGUILayout.LabelField("• " + this.ApplyInlineFormatting(line [ 2.. ]), _listItem);
					}
				// Empty lines
				else if (string.IsNullOrWhiteSpace(line))
					{
					GUILayout.Space(4);
					}
				// Regular text
				else
					{
					EditorGUILayout.LabelField(this.ApplyInlineFormatting(line), _bodyWrap);
					}
				}
			}

		private void RenderImagePlaceholder (string refPath, string alt)
			{
			// For now, just show a placeholder - full image rendering could be added later
			using (new EditorGUILayout.HorizontalScope("box"))
				{
				GUILayout.Label("🖼️", GUILayout.Width(30));
				EditorGUILayout.LabelField($"Image: {alt}", EditorStyles.wordWrappedMiniLabel);
				EditorGUILayout.LabelField($"Path: {refPath}", EditorStyles.miniLabel);
				}
			}

		private string ApplyInlineFormatting (string input)
			{
			if (string.IsNullOrEmpty(input))
				return string.Empty;

			string s = input;

			// Escape HTML
			s = s.Replace("<", "&lt;").Replace(">", "&gt;");

			// Code inline
			s = Regex.Replace(s, "`([^`]+)`", m => $"<color=#c8e1ff><b>{m.Groups [ 1 ].Value}</b></color>");

			// Bold
			s = Regex.Replace(s, @"\*\*([^*]+)\*\*", m => $"<b>{m.Groups [ 1 ].Value}</b>");

			// Italic
			s = Regex.Replace(s, @"(?<!\*)\*([^*]+)\*(?!\*)", m => $"<i>{m.Groups [ 1 ].Value}</i>");
			s = Regex.Replace(s, @"_([^_]+)_", m => $"<i>{m.Groups [ 1 ].Value}</i>");

			// Links
			s = Regex.Replace(s, @"\[([^\]]+)\]\(([^)]+)\)", m => $"<color=#4ea1ff><u>{m.Groups [ 1 ].Value}</u></color>");

			return s;
			}

		private string GetPlaceholderMarkdown ()
			{
			return @"# 🎭 Welcome to The Scribe!

## 🚀 Quick Start Guide

1. **🎯 Choose a Template** - Click the Templates button in the toolbar to start with a quest archetype
2. **📝 Fill the Form** - Use the Form tab to structure your documentation
3. **✏️ Edit Raw Markdown** - Switch to Editor for direct markdown editing
4. **👁️ Preview Results** - See your formatted documentation here

## 🧙‍♂️ Living Dev Agent Features

- **📚 Template System** - Registry-based quest archetypes for consistent docs
- **🗺️ GitBook Navigator** - Browse your documentation tree with thumbnails
- **🔄 Auto-sync** - Keep form and raw editor in harmony
- **🖼️ Image Support** - Drag, drop, and insert with cursor awareness
- **📊 Rich Metadata** - Track complexity, impact, and project context

---

> 📜 *""Documentation is the secret art of the living dev - each entry a scroll worthy of preservation.""*

Start your documentation journey by choosing a template or creating content in the Form tab!";
			}
		}
	}
#endif
