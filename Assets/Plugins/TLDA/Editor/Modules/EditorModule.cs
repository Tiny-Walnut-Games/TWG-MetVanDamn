#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LivingDevAgent.Editor.Modules
	{
	/// <summary>
	/// ✏️ Editor Module - The Raw Markdown Command Center!
	/// Handles the raw text editor with cursor management and file operations.
	/// </summary>
	public class EditorModule : ScribeModuleBase
		{
		public EditorModule (TLDLScribeData data) : base(data) { }

		public void DrawToolbar ()
			{
			// File operations
			if (GUILayout.Button("📂 Load…", EditorStyles.toolbarButton, GUILayout.Width(70)))
				{
				LoadFileDialog();
				}

			using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_data.RawContent)))
				{
				if (GUILayout.Button("💾 Save", EditorStyles.toolbarButton, GUILayout.Width(60)))
					{
					SaveRaw();
					}

				if (GUILayout.Button("💾 Save As…", EditorStyles.toolbarButton, GUILayout.Width(90)))
					{
					SaveRawAs();
					}
				}
			}

		public void DrawContent (Rect windowPosition)
			{
			using (new EditorGUILayout.VerticalScope())
				{
				// Header with controls
				using (new EditorGUILayout.HorizontalScope())
					{
					var headerContent = new GUIContent("✏️ Raw Markdown Editor");
					GUILayout.Label(headerContent, EditorStyles.boldLabel);

					GUILayout.FlexibleSpace();

					_data.RawWrap = EditorGUILayout.ToggleLeft("📄 Wrap", _data.RawWrap, GUILayout.Width(80));
					}

				// Editor area
				var rawStyle = new GUIStyle(_textAreaMonospace) { wordWrap = _data.RawWrap };
				float viewportHeight = Mathf.Max(140f, windowPosition.height - 240f);

				_data.RawScroll = EditorGUILayout.BeginScrollView(_data.RawScroll, GUILayout.Height(viewportHeight), GUILayout.ExpandHeight(true));

				GUI.SetNextControlName(_data.RawEditorControlName);
				string edited = EditorGUILayout.TextArea(_data.RawContent ?? string.Empty, rawStyle, GUILayout.ExpandHeight(true));

				if (edited != _data.RawContent)
					{
					if (_data.RawGeneratedSnapshot != null && edited != _data.RawGeneratedSnapshot)
						{
						_data.RawDirty = true; // user diverged from generated content
						}
					_data.RawContent = edited;
					}

				// Handle cursor tracking
				HandleCursorTracking();

				EditorGUILayout.EndScrollView();

				// Bottom controls
				DrawBottomControls();
				}
			}

		private void HandleCursorTracking ()
			{
			if (GUI.GetNameOfFocusedControl() == _data.RawEditorControlName)
				{
				var te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
				if (te != null)
					{
					_data.RawCursorIndex = te.cursorIndex;
					if (_data.PendingScrollToCursor)
						{
						// Rough scroll heuristic: count newlines up to cursor and estimate line height
						int line = 0;
						if (!string.IsNullOrEmpty(_data.RawContent) && _data.RawCursorIndex > 0)
							{
							for (int i = 0; i < Math.Min(_data.RawCursorIndex, _data.RawContent.Length); i++)
								{
								if (_data.RawContent [ i ] == '\n')
									line++;
								}
							}
						_data.RawScroll.y = line * 18f; // approx line height
						_data.PendingScrollToCursor = false;
						}
					}
				}
			}

		private void DrawBottomControls ()
			{
			using (new EditorGUILayout.HorizontalScope())
				{
				if (GUILayout.Button("📂 Load File…", GUILayout.Width(110)))
					{
					LoadFileDialog();
					}

				using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_data.RawContent)))
					{
					if (GUILayout.Button("💾 Save Raw"))
						{
						SaveRaw();
						}

					if (GUILayout.Button("💾 Save Raw As…"))
						{
						SaveRawAs();
						}
					}

				if (GUILayout.Button("🖼️ Insert Image…", GUILayout.Width(120)))
					{
					AddImageAndInsertAtCursor();
					}

				GUILayout.FlexibleSpace();

				// Status indicators
				if (_data.RawDirty)
					{
					GUILayout.Label("✏️ Modified", EditorStyles.miniLabel);
					}

				if (!string.IsNullOrEmpty(_data.CurrentFilePath))
					{
					GUILayout.Label($"📄 {Path.GetFileName(_data.CurrentFilePath)}", EditorStyles.miniLabel);
					}
				}
			}

		private void LoadFileDialog ()
			{
			string dir = ResolveActiveFolder();
			string picked = EditorUtility.OpenFilePanelWithFilters(
				"Open Document",
				dir,
				new [ ] { "All", "*.*", "Markdown", "md,markdown", "Text", "txt,log", "XML", "xml" }
			);
			if (!string.IsNullOrEmpty(picked))
				{
				LoadFile(picked);
				}
			}

		private void LoadFile (string absPath)
			{
			try
				{
				string text = File.ReadAllText(absPath);
				_data.CurrentFilePath = absPath;
				_data.RawContent = text;
				_data.RawGeneratedSnapshot = text;
				_data.RawDirty = false;
				ParseBasicMetadata(text);
				SetStatus($"📂 Loaded: {MakeProjectRelative(absPath)}");
				}
			catch (Exception ex)
				{
				SetStatus($"❌ Failed to load file: {ex.Message}");
				}
			}

		private void SaveRaw ()
			{
			if (!string.IsNullOrEmpty(_data.CurrentFilePath))
				{
				try
					{
					File.WriteAllText(_data.CurrentFilePath, _data.RawContent ?? string.Empty, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
					_data.RawDirty = false;
					SetStatus($"💾 Saved: {MakeProjectRelative(_data.CurrentFilePath)}");
					}
				catch (Exception ex)
					{
					SetStatus($"❌ Failed to save: {ex.Message}");
					}
				}
			else
				{
				SaveRawAs();
				}
			}

		private void SaveRawAs ()
			{
			string dir = ResolveActiveFolder();
			string suggested = $"TLDL-{DateTime.UtcNow:yyyy-MM-dd}-Entry.md";
			string picked = EditorUtility.SaveFilePanel("Save Document As", dir, suggested, "md");
			if (!string.IsNullOrEmpty(picked))
				{
				try
					{
					File.WriteAllText(picked, _data.RawContent ?? string.Empty, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
					_data.CurrentFilePath = picked;
					_data.RawDirty = false;
					SetStatus($"💾 Saved: {MakeProjectRelative(_data.CurrentFilePath)}");
					}
				catch (Exception ex)
					{
					SetStatus($"❌ Failed to save: {ex.Message}");
					}
				}
			}

		private void AddImageAndInsertAtCursor ()
			{
			string startDir = EnsureImagesDirectory();
			string picked = EditorUtility.OpenFilePanelWithFilters("Add Image", startDir, new [ ] { "Images", "png,jpg,jpeg,gif", "All", "*.*" });
			if (string.IsNullOrEmpty(picked)) return;

			try
				{
				string imagesDir = EnsureImagesDirectory();
				string fileName = Path.GetFileName(picked);
				string dest = Path.Combine(imagesDir, fileName);
				string uniqueDest = dest;
				int i = 1;
				while (File.Exists(uniqueDest))
					{
					uniqueDest = Path.Combine(imagesDir, $"{Path.GetFileNameWithoutExtension(fileName)}_{i}{Path.GetExtension(fileName)}");
					i++;
					}
				File.Copy(picked, uniqueDest, overwrite: false);

				// Add relative path entry
				string rel = Path.GetFileName(uniqueDest);
				string mdRef = $"images/{rel}".Replace('\\', '/');
				if (!string.IsNullOrEmpty(mdRef))
					{
					// Add to image paths for form sync
					if (string.IsNullOrEmpty(_data.ImagePaths))
						_data.ImagePaths = mdRef;
					else
						_data.ImagePaths += "\n" + mdRef;
					}

				InsertImageMarkdownAtCursor(mdRef);
				SetStatus($"🖼️ Image added: {mdRef}");
				}
			catch (Exception ex)
				{
				SetStatus($"❌ Failed to add image: {ex.Message}");
				}
			}

		private void InsertImageMarkdownAtCursor (string relPath)
			{
			string insert = $"![image]({relPath})";

			// Also add to tracked image paths if not already present
			if (!string.IsNullOrEmpty(relPath) && !_data.ImagePaths.Contains(relPath))
				{
				if (string.IsNullOrEmpty(_data.ImagePaths))
					_data.ImagePaths = relPath;
				else
					_data.ImagePaths += "\n" + relPath;
				}

			if (GUI.GetNameOfFocusedControl() == _data.RawEditorControlName && _data.RawCursorIndex >= 0 && _data.RawCursorIndex <= (_data.RawContent?.Length ?? 0))
				{
				_data.RawContent ??= string.Empty;
				bool needLeadingNewline = _data.RawCursorIndex > 0 && _data.RawContent [ _data.RawCursorIndex - 1 ] != '\n';
				bool needTrailingNewline = _data.RawCursorIndex < _data.RawContent.Length && _data.RawContent [ _data.RawCursorIndex ] != '\n';
				string prefix = _data.RawContent [ .._data.RawCursorIndex ];
				string suffix = _data.RawContent [ _data.RawCursorIndex.. ];
				var builder = new StringBuilder(prefix.Length + insert.Length + suffix.Length + 4);
				builder.Append(prefix);
				if (needLeadingNewline) builder.Append('\n');
				builder.Append(insert);
				if (needTrailingNewline) builder.Append('\n');
				builder.Append(suffix);
				_data.RawContent = builder.ToString();
				_data.RawCursorIndex = prefix.Length + (needLeadingNewline ? 1 : 0) + insert.Length + (needTrailingNewline ? 1 : 0);
				SetStatus($"🖼️ Inserted image at cursor: {relPath}");
				}
			else
				{
				string md = _data.RawContent ?? string.Empty;
				if (!md.EndsWith("\n") && md.Length > 0) md += "\n";
				_data.RawCursorIndex = md.Length;
				_data.RawContent = md + insert + "\n";
				SetStatus($"🖼️ Inserted image at end: {relPath}");
				}
			_data.PendingScrollToCursor = true;

			// Focus raw editor next repaint
			EditorApplication.delayCall += () => GUI.FocusControl(_data.RawEditorControlName);
			}

		private string EnsureImagesDirectory ()
			{
			string baseDir = string.IsNullOrEmpty(_data.CurrentFilePath) ? ResolveActiveFolder() : Path.GetDirectoryName(_data.CurrentFilePath);
			if (string.IsNullOrEmpty(baseDir))
				baseDir = ResolveActiveFolder();

			string imagesDir = Path.Combine(baseDir, "images");
			if (!Directory.Exists(imagesDir))
				Directory.CreateDirectory(imagesDir);

			return imagesDir;
			}

		private void ParseBasicMetadata (string md)
			{
			if (string.IsNullOrEmpty(md)) return;

			try
				{
				using var reader = new StringReader(md);
				string line;
				while ((line = reader.ReadLine()) != null)
					{
					if (line.StartsWith("**Author:"))
						{
						string v = line [ "**Author:**".Length.. ].Trim();
						if (!string.IsNullOrEmpty(v)) _data.Author = v;
						}
					else if (line.StartsWith("**Summary:"))
						{
						string v = line [ "**Summary:**".Length.. ].Trim();
						if (!string.IsNullOrEmpty(v)) _data.Summary = v;
						}
					else if (line.StartsWith("**Context:"))
						{
						string v = line [ "**Context:**".Length.. ].Trim();
						if (!string.IsNullOrEmpty(v)) _data.Context = v;
						}
					}
				}
			catch { }
			}

		private string ResolveActiveFolder ()
			{
			return !string.IsNullOrEmpty(_data.ActiveDirPath)
				? _data.ActiveDirPath
				: !string.IsNullOrEmpty(_data.RootPath) ? _data.RootPath : Path.Combine(Application.dataPath, "Plugins/TLDA/docs");
			}

		private string MakeProjectRelative (string absPath)
			{
			string projectRoot = Directory.GetParent(Application.dataPath)!.FullName.Replace('\\', '/');
			string norm = absPath.Replace('\\', '/');
			return norm.StartsWith(projectRoot) ? norm [ (projectRoot.Length + 1).. ] : absPath;
			}
		}
	}
#endif
