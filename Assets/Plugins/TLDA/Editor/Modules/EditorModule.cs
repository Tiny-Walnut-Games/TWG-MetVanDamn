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
				this.LoadFileDialog();
				}

			using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(this._data.RawContent)))
				{
				if (GUILayout.Button("💾 Save", EditorStyles.toolbarButton, GUILayout.Width(60)))
					{
					this.SaveRaw();
					}

				if (GUILayout.Button("💾 Save As…", EditorStyles.toolbarButton, GUILayout.Width(90)))
					{
					this.SaveRawAs();
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

					this._data.RawWrap = EditorGUILayout.ToggleLeft("📄 Wrap", this._data.RawWrap, GUILayout.Width(80));
					}

				// Editor area
				var rawStyle = new GUIStyle(_textAreaMonospace) { wordWrap = this._data.RawWrap };
				float viewportHeight = Mathf.Max(140f, windowPosition.height - 240f);

				this._data.RawScroll = EditorGUILayout.BeginScrollView(this._data.RawScroll, GUILayout.Height(viewportHeight), GUILayout.ExpandHeight(true));

				GUI.SetNextControlName(this._data.RawEditorControlName);
				string edited = EditorGUILayout.TextArea(this._data.RawContent ?? string.Empty, rawStyle, GUILayout.ExpandHeight(true));

				if (edited != this._data.RawContent)
					{
					if (this._data.RawGeneratedSnapshot != null && edited != this._data.RawGeneratedSnapshot)
						{
						this._data.RawDirty = true; // user diverged from generated content
						}
					this._data.RawContent = edited;
					}

				// Handle cursor tracking
				this.HandleCursorTracking();

				EditorGUILayout.EndScrollView();

				// Bottom controls
				this.DrawBottomControls();
				}
			}

		private void HandleCursorTracking ()
			{
			if (GUI.GetNameOfFocusedControl() == this._data.RawEditorControlName)
				{
				var te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
				if (te != null)
					{
					this._data.RawCursorIndex = te.cursorIndex;
					if (this._data.PendingScrollToCursor)
						{
						// Rough scroll heuristic: count newlines up to cursor and estimate line height
						int line = 0;
						if (!string.IsNullOrEmpty(this._data.RawContent) && this._data.RawCursorIndex > 0)
							{
							for (int i = 0; i < Math.Min(this._data.RawCursorIndex, this._data.RawContent.Length); i++)
								{
								if (this._data.RawContent [ i ] == '\n')
									line++;
								}
							}
						this._data.RawScroll.y = line * 18f; // approx line height
						this._data.PendingScrollToCursor = false;
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
					this.LoadFileDialog();
					}

				using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(this._data.RawContent)))
					{
					if (GUILayout.Button("💾 Save Raw"))
						{
						this.SaveRaw();
						}

					if (GUILayout.Button("💾 Save Raw As…"))
						{
						this.SaveRawAs();
						}
					}

				if (GUILayout.Button("🖼️ Insert Image…", GUILayout.Width(120)))
					{
					this.AddImageAndInsertAtCursor();
					}

				GUILayout.FlexibleSpace();

				// Status indicators
				if (this._data.RawDirty)
					{
					GUILayout.Label("✏️ Modified", EditorStyles.miniLabel);
					}

				if (!string.IsNullOrEmpty(this._data.CurrentFilePath))
					{
					GUILayout.Label($"📄 {Path.GetFileName(this._data.CurrentFilePath)}", EditorStyles.miniLabel);
					}
				}
			}

		private void LoadFileDialog ()
			{
			string dir = this.ResolveActiveFolder();
			string picked = EditorUtility.OpenFilePanelWithFilters(
				"Open Document",
				dir,
				new [ ] { "All", "*.*", "Markdown", "md,markdown", "Text", "txt,log", "XML", "xml" }
			);
			if (!string.IsNullOrEmpty(picked))
				{
				this.LoadFile(picked);
				}
			}

		private void LoadFile (string absPath)
			{
			try
				{
				string text = File.ReadAllText(absPath);
				this._data.CurrentFilePath = absPath;
				this._data.RawContent = text;
				this._data.RawGeneratedSnapshot = text;
				this._data.RawDirty = false;
				this.ParseBasicMetadata(text);
				this.SetStatus($"📂 Loaded: {this.MakeProjectRelative(absPath)}");
				}
			catch (Exception ex)
				{
				this.SetStatus($"❌ Failed to load file: {ex.Message}");
				}
			}

		private void SaveRaw ()
			{
			if (!string.IsNullOrEmpty(this._data.CurrentFilePath))
				{
				try
					{
					File.WriteAllText(this._data.CurrentFilePath, this._data.RawContent ?? string.Empty, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
					this._data.RawDirty = false;
					this.SetStatus($"💾 Saved: {this.MakeProjectRelative(this._data.CurrentFilePath)}");
					}
				catch (Exception ex)
					{
					this.SetStatus($"❌ Failed to save: {ex.Message}");
					}
				}
			else
				{
				this.SaveRawAs();
				}
			}

		private void SaveRawAs ()
			{
			string dir = this.ResolveActiveFolder();
			string suggested = $"TLDL-{DateTime.UtcNow:yyyy-MM-dd}-Entry.md";
			string picked = EditorUtility.SaveFilePanel("Save Document As", dir, suggested, "md");
			if (!string.IsNullOrEmpty(picked))
				{
				try
					{
					File.WriteAllText(picked, this._data.RawContent ?? string.Empty, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
					this._data.CurrentFilePath = picked;
					this._data.RawDirty = false;
					this.SetStatus($"💾 Saved: {this.MakeProjectRelative(this._data.CurrentFilePath)}");
					}
				catch (Exception ex)
					{
					this.SetStatus($"❌ Failed to save: {ex.Message}");
					}
				}
			}

		private void AddImageAndInsertAtCursor ()
			{
			string startDir = this.EnsureImagesDirectory();
			string picked = EditorUtility.OpenFilePanelWithFilters("Add Image", startDir, new [ ] { "Images", "png,jpg,jpeg,gif", "All", "*.*" });
			if (string.IsNullOrEmpty(picked)) return;

			try
				{
				string imagesDir = this.EnsureImagesDirectory();
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
					if (string.IsNullOrEmpty(this._data.ImagePaths))
						this._data.ImagePaths = mdRef;
					else
						this._data.ImagePaths += "\n" + mdRef;
					}

				this.InsertImageMarkdownAtCursor(mdRef);
				this.SetStatus($"🖼️ Image added: {mdRef}");
				}
			catch (Exception ex)
				{
				this.SetStatus($"❌ Failed to add image: {ex.Message}");
				}
			}

		private void InsertImageMarkdownAtCursor (string relPath)
			{
			string insert = $"![image]({relPath})";

			// Also add to tracked image paths if not already present
			if (!string.IsNullOrEmpty(relPath) && !this._data.ImagePaths.Contains(relPath))
				{
				if (string.IsNullOrEmpty(this._data.ImagePaths))
					this._data.ImagePaths = relPath;
				else
					this._data.ImagePaths += "\n" + relPath;
				}

			if (GUI.GetNameOfFocusedControl() == this._data.RawEditorControlName && this._data.RawCursorIndex >= 0 && this._data.RawCursorIndex <= (this._data.RawContent?.Length ?? 0))
				{
				this._data.RawContent ??= string.Empty;
				bool needLeadingNewline = this._data.RawCursorIndex > 0 && this._data.RawContent [ this._data.RawCursorIndex - 1 ] != '\n';
				bool needTrailingNewline = this._data.RawCursorIndex < this._data.RawContent.Length && this._data.RawContent [ this._data.RawCursorIndex ] != '\n';
				string prefix = this._data.RawContent [ ..this._data.RawCursorIndex ];
				string suffix = this._data.RawContent [ this._data.RawCursorIndex.. ];
				var builder = new StringBuilder(prefix.Length + insert.Length + suffix.Length + 4);
				builder.Append(prefix);
				if (needLeadingNewline) builder.Append('\n');
				builder.Append(insert);
				if (needTrailingNewline) builder.Append('\n');
				builder.Append(suffix);
				this._data.RawContent = builder.ToString();
				this._data.RawCursorIndex = prefix.Length + (needLeadingNewline ? 1 : 0) + insert.Length + (needTrailingNewline ? 1 : 0);
				this.SetStatus($"🖼️ Inserted image at cursor: {relPath}");
				}
			else
				{
				string md = this._data.RawContent ?? string.Empty;
				if (!md.EndsWith("\n") && md.Length > 0) md += "\n";
				this._data.RawCursorIndex = md.Length;
				this._data.RawContent = md + insert + "\n";
				this.SetStatus($"🖼️ Inserted image at end: {relPath}");
				}
			this._data.PendingScrollToCursor = true;

			// Focus raw editor next repaint
			EditorApplication.delayCall += () => GUI.FocusControl(this._data.RawEditorControlName);
			}

		private string EnsureImagesDirectory ()
			{
			string baseDir = string.IsNullOrEmpty(this._data.CurrentFilePath) ? this.ResolveActiveFolder() : Path.GetDirectoryName(this._data.CurrentFilePath);
			if (string.IsNullOrEmpty(baseDir))
				baseDir = this.ResolveActiveFolder();

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
						if (!string.IsNullOrEmpty(v)) this._data.Author = v;
						}
					else if (line.StartsWith("**Summary:"))
						{
						string v = line [ "**Summary:**".Length.. ].Trim();
						if (!string.IsNullOrEmpty(v)) this._data.Summary = v;
						}
					else if (line.StartsWith("**Context:"))
						{
						string v = line [ "**Context:**".Length.. ].Trim();
						if (!string.IsNullOrEmpty(v)) this._data.Context = v;
						}
					}
				}
			catch { }
			}

		private string ResolveActiveFolder ()
			{
			return !string.IsNullOrEmpty(this._data.ActiveDirPath)
				? this._data.ActiveDirPath
				: !string.IsNullOrEmpty(this._data.RootPath) ? this._data.RootPath : Path.Combine(Application.dataPath, "Plugins/TLDA/docs");
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
