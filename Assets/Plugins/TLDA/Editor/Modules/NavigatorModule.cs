#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LivingDevAgent.Editor.Modules
	{
	/// <summary>
	/// 🗺️ Navigator Module - The GitBook-style Knowledge Tree Explorer!
	/// Provides left-panel navigation with file management, thumbnails, and folder operations.
	/// Enhanced with visual styling and modular dashboard approach.
	/// </summary>
	public class NavigatorModule : ScribeModuleBase
		{
		public NavigatorModule (TLDLScribeData data) : base(data) { }

		public override void Initialize ()
			{
			// Load persisted root if available; otherwise, default to TLDA docs
			_data.RootPath = EditorPrefs.GetString(TLDLScribeData.EditorPrefsRootKey, string.Empty);
			if (string.IsNullOrEmpty(_data.RootPath))
				{
				string defaultFolder = Path.Combine(Application.dataPath, "Plugins/TLDA/docs");
				if (Directory.Exists(defaultFolder))
					{
					_data.RootPath = defaultFolder;
					}
				}
			if (!string.IsNullOrEmpty(_data.RootPath) && Directory.Exists(_data.RootPath))
				{
				if (string.IsNullOrEmpty(_data.ActiveDirPath))
					{
					_data.ActiveDirPath = _data.RootPath;
					}
				}

			InitializeStyles();
			}

		public override void Dispose ()
			{
			foreach (KeyValuePair<string, Texture2D> kvp in
				from kvp in _data.ImageCache
				where kvp.Value != null
				select kvp)
				{
				UnityEngine.Object.DestroyImmediate(kvp.Value);
				}

			_data.ImageCache.Clear();
			_data.ImageCacheOrder.Clear();
			}

		public void DrawToolbar ()
			{
			// Root selection with emoji
			GUIContent rootIcon = new("📁 Root…", "Choose the root folder for your documentation tree");
			if (GUILayout.Button(rootIcon, EditorStyles.toolbarButton, GUILayout.Width(80)))
				{
				ChooseRootFolder();
				}

			using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_data.RootPath)))
				{
				if (GUILayout.Button("🔍 Open", EditorStyles.toolbarButton, GUILayout.Width(70)))
					{
					OpenInFileBrowser(_data.RootPath);
					}

				if (GUILayout.Button("🔄", EditorStyles.toolbarButton, GUILayout.Width(30)))
					{
					RefreshNavigator();
					}
				}
			}

		public void DrawPanel (float width) // @jmeyer1980 ⚠ Intention ⚠ - TODO: refactor to use width
			{
			// Header with proper emoji display and styling
			using (new EditorGUILayout.HorizontalScope())
				{
				GUIContent headerContent = new("🗺️ Sudo-GitBook");
				GUILayout.Label(headerContent, EditorStyles.boldLabel);

				GUILayout.FlexibleSpace();

				if (GUILayout.Button("⚙️", EditorStyles.miniButton, GUILayout.Width(25)))
					{
					ShowNavigatorSettings();
					}
				}

			// Root path display
			using (new EditorGUILayout.HorizontalScope())
				{
				EditorGUILayout.TextField("Root", string.IsNullOrEmpty(_data.RootPath) ? "(not set)" : _data.RootPath);
				}

			// File tree with enhanced styling
			_data.NavScroll = EditorGUILayout.BeginScrollView(_data.NavScroll, GUILayout.ExpandHeight(true));

			if (string.IsNullOrEmpty(_data.RootPath))
				{
				EditorGUILayout.HelpBox("🌟 Choose a root folder to begin your documentation journey!", MessageType.Info);
				}
			else if (!Directory.Exists(_data.RootPath))
				{
				EditorGUILayout.HelpBox("⚠️ Root folder not found. Please choose a new root.", MessageType.Warning);
				}
			else
				{
				DrawDirectoryNode(_data.RootPath, 0, GetIndent(0));
				}

			EditorGUILayout.EndScrollView();

			// Active directory controls
			EditorGUILayout.Space(4);
			using (new EditorGUI.DisabledScope(true))
				{
				EditorGUILayout.TextField("📂 Active", string.IsNullOrEmpty(_data.ActiveDirPath) ? "(none)" : _data.ActiveDirPath);
				}

			using (new EditorGUILayout.HorizontalScope())
				{
				using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_data.ActiveDirPath)))
					{
					if (GUILayout.Button("🔍 Open"))
						{
						OpenInFileBrowser(_data.ActiveDirPath);
						}

					if (GUILayout.Button("➕ Folder"))
						{
						CreateChildFolder();
						}
					}
				}
			}

		private void ShowNavigatorSettings ()
			{
			GenericMenu menu = new();
			menu.AddItem(new("📁 Choose New Root"), false, ChooseRootFolder);
			menu.AddItem(new("🔄 Refresh Navigator"), false, RefreshNavigator);
			menu.AddSeparator("");
			menu.AddItem(new("🗑️ Clear Image Cache"), false, ClearImageCache);
			menu.ShowAsContext();
			}

		private void ClearImageCache ()
			{
			foreach (KeyValuePair<string, Texture2D> kvp in _data.ImageCache)
				{
				if (kvp.Value != null)
					{
					UnityEngine.Object.DestroyImmediate(kvp.Value);
					}
				}
			_data.ImageCache.Clear();
			_data.ImageCacheOrder.Clear();
			SetStatus("🗑️ Image cache cleared");
			}

		private void DrawDirectoryNode (string path, int depth, string indent)
			{
			if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
				{
				return;
				}

			string folderName = Path.GetFileName(path);
			if (string.IsNullOrEmpty(folderName))
				{
				folderName = path;
				}

			if (!_data.FolderExpanded.ContainsKey(path))
				{
				_data.FolderExpanded [ path ] = depth <= 1; // expand root/top-level by default
				}

			string labelPrefix = indent ?? string.Empty;
			using (new EditorGUILayout.HorizontalScope())
				{
				string folderIcon = _data.FolderExpanded [ path ] ? "📂" : "📁";
				_data.FolderExpanded [ path ] = EditorGUILayout.Foldout(_data.FolderExpanded [ path ], $"{folderIcon} {labelPrefix}{folderName}", true);

				if (GUILayout.Button("✅", EditorStyles.miniButton, GUILayout.Width(25)))
					{
					_data.ActiveDirPath = path;
					SetStatus($"📂 Active directory: {path}");
					}
				}

			if (!_data.FolderExpanded [ path ]) return; // @jmeyer1980 ⚠ Intention ⚠ - IL for legibility

			try
				{
				// Sort subdirectories for stable nav
				string [ ] subDirs = Directory.GetDirectories(path);
				Array.Sort(subDirs, StringComparer.OrdinalIgnoreCase);
				foreach (string d in subDirs)
					{
					EditorGUI.indentLevel++;
					DrawDirectoryNode(d, depth + 1, GetIndent(depth + 1));
					EditorGUI.indentLevel--;
					}

				// Sort files for stable nav
				string [ ] files = Directory.GetFiles(path);
				Array.Sort(files, StringComparer.OrdinalIgnoreCase);
				foreach (string f in files)
					{
					string ext = Path.GetExtension(f);
					if (!TLDLScribeData.AllowedExts.Contains(ext)) continue;

					DrawFileNode(f, indent, depth);
					}
				}
			catch (Exception e)
				{
				EditorGUILayout.HelpBox($"❌ Error reading directory: {e.Message}", MessageType.Warning);
				}
			}

		private void DrawFileNode (string filePath, string indent, int depth)
			{
			using (new EditorGUILayout.HorizontalScope())
				{
				EditorGUI.indentLevel++;
				string fileName = Path.GetFileName(filePath);
				string fileLabel = (indent ?? string.Empty) + "  " + fileName;
				bool isImage = TLDLScribeData.ImageExts.Contains(Path.GetExtension(filePath));

				// File type emoji
				string fileIcon = GetFileIcon(Path.GetExtension(filePath));

				// Optional thumbnail for images
				if (isImage)
					{
					DrawImageThumbnail(filePath);
					}

				if (GUILayout.Button($"{fileIcon} {fileLabel}", EditorStyles.label))
					{
					if (isImage)
						{
						CopyImageMarkdownLink(filePath);
						}
					else
						{
						LoadFile(filePath);
						}
					}

				// Action buttons
				DrawFileActionButtons(filePath, isImage);

				EditorGUI.indentLevel--;
				}
			}

		private void DrawImageThumbnail (string filePath)
			{
			if (!_data.ImageCache.TryGetValue(filePath, out Texture2D tex) || tex == null)
				{
				try
					{
					byte [ ] bytes = File.ReadAllBytes(filePath);
					if (bytes != null)
						{
						tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
						if (tex.LoadImage(bytes))
							{
							AddTextureToCache(filePath, tex);
							}
						else
							{
							UnityEngine.Object.DestroyImmediate(tex);
							tex = null;
							}
						}
					}
				catch { /* ignore IO/format errors */ }
				}

			if (_data.ImageCache.TryGetValue(filePath, out Texture2D thumb) && thumb != null)
				{
				const float maxThumb = 36f;
				float aspect = (float)thumb.width / Mathf.Max(1, thumb.height);
				float w = Mathf.Min(maxThumb * aspect, maxThumb);
				float h = Mathf.Min(maxThumb, maxThumb / Mathf.Max(0.01f, aspect));
				GUILayout.Label(thumb, GUILayout.Width(w), GUILayout.Height(h));
				}
			else
				{
				GUILayout.Space(4);
				}
			}

		private void DrawFileActionButtons (string filePath, bool isImage)
			{
			if (!isImage)
				{
				if (GUILayout.Button("📖", EditorStyles.miniButton, GUILayout.Width(25)))
					{
					LoadFile(filePath);
					}
				}

			if (GUILayout.Button("🔍", EditorStyles.miniButton, GUILayout.Width(25)))
				{
				OpenContainingFolderOfFile(filePath);
				}

			if (isImage)
				{
				if (GUILayout.Button("➕", EditorStyles.miniButton, GUILayout.Width(25)))
					{
					InsertImageAtCursor(filePath);
					}
				}

			if (GUILayout.Button("📋", EditorStyles.miniButton, GUILayout.Width(25)))
				{
				try
					{
					string copy = DuplicateFile(filePath);
					SetStatus($"📋 Duplicated: {MakeProjectRelative(copy)}");
					RefreshNavigator();
					}
				catch (Exception ex)
					{
					SetStatus($"❌ Duplicate failed: {ex.Message}");
					}
				}
			}

		private string GetFileIcon (string extension)
			{
			return extension.ToLower() switch
				{
					".md" or ".markdown" => "📝",
					".txt" or ".log" => "📄",
					".xml" => "🔧",
					".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".tga" => "🖼️",
					_ => "📄"
					};
			}

		private void AddTextureToCache (string key, Texture2D tex)
			{
			if (tex == null) return; // @jmeyer1980 ⚠ Intention ⚠ - IL for legibility

			_data.ImageCache [ key ] = tex;
			_data.ImageCacheOrder.Add(key);
			if (_data.ImageCacheOrder.Count > TLDLScribeData.ImageCacheMax)
				{
				string oldest = _data.ImageCacheOrder [ 0 ];
				_data.ImageCacheOrder.RemoveAt(0);
				if (_data.ImageCache.TryGetValue(oldest, out Texture2D oldTex) && oldTex != null)
					{
					UnityEngine.Object.DestroyImmediate(oldTex);
					}
				_data.ImageCache.Remove(oldest);
				}
			}

		private string GetIndent (int depth)
			{
			return new string(' ', depth * 2);
			}

		// File operations
		private void ChooseRootFolder ()
			{
			string start = string.IsNullOrEmpty(_data.RootPath) ? Application.dataPath : _data.RootPath;
			string picked = EditorUtility.OpenFolderPanel("Choose Sudo-GitBook Root", start, "");
			if (!string.IsNullOrEmpty(picked))
				{
				_data.RootPath = picked;
				_data.ActiveDirPath = _data.RootPath;
				EditorPrefs.SetString(TLDLScribeData.EditorPrefsRootKey, _data.RootPath);
				SetStatus($"📁 Root set to: {_data.RootPath}");
				RefreshNavigator();
				}
			}

		// Replace the RefreshNavigator method to cast _window to EditorWindow before calling Repaint
		private void RefreshNavigator ()
			{
			if (_window is EditorWindow editorWindow)
				{
				editorWindow.Repaint();
				}
			}

		private void CreateChildFolder ()
			{
			string parent = _data.ActiveDirPath;
			if (string.IsNullOrEmpty(parent))
				{
				parent = ResolveActiveFolder();
				}

			string name = EditorUtility.SaveFilePanel("New Folder Name", parent, "NewFolder", "");
			if (!string.IsNullOrEmpty(name))
				{
				try
					{
					string dir = name;
					if (Path.HasExtension(dir))
						{
						dir = Path.GetDirectoryName(dir);
						}

					if (!Directory.Exists(dir))
						{
						Directory.CreateDirectory(dir);
						}

					_data.ActiveDirPath = dir;
					SetStatus($"📁 Folder ready: {dir}");
					RefreshNavigator();
					}
				catch (Exception ex)
					{
					SetStatus($"❌ Failed to create folder: {ex.Message}");
					}
				}
			}

		private string ResolveActiveFolder ()
			{
			// @jmeyer1980 ⚠ Intention ⚠ - IL for legibility
			if (!string.IsNullOrEmpty(_data.ActiveDirPath)) return _data.ActiveDirPath;

			if (!string.IsNullOrEmpty(_data.RootPath)) return _data.RootPath;

			string fallback = Path.Combine(Application.dataPath, "Plugins/TLDA/docs");
			return fallback;
			}

		private void OpenInFileBrowser (string path)
			{
			// @jmeyer1980 ⚠ Intention ⚠ - IL for legibility
			if (string.IsNullOrEmpty(path)) return;
			EditorUtility.RevealInFinder(path);
			}

		private void OpenContainingFolderOfFile (string absPath)
			{
			// @jmeyer1980 ⚠ Intention ⚠ - IL for legibility
			if (string.IsNullOrEmpty(absPath)) return;
			EditorUtility.RevealInFinder(absPath);
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
				SetStatus($"📖 Loaded: {MakeProjectRelative(absPath)}");
				}
			catch (Exception ex)
				{
				SetStatus($"❌ Failed to load file: {ex.Message}");
				}
			}

		private void CopyImageMarkdownLink (string filePath)
			{
			string baseDir = string.IsNullOrEmpty(_data.CurrentFilePath) ? ResolveActiveFolder() : Path.GetDirectoryName(_data.CurrentFilePath);
			if (string.IsNullOrEmpty(baseDir))
				{
				baseDir = ResolveActiveFolder();
				}

			string rel = GetRelativePath(baseDir, filePath).Replace('\\', '/');
			string alt = Path.GetFileNameWithoutExtension(filePath);
			string md = $"![{alt}]({rel})";
			EditorGUIUtility.systemCopyBuffer = md;
			SetStatus($"📋 Link copied: {rel}");
			}

		private void InsertImageAtCursor (string filePath)
			{
			string baseDir = string.IsNullOrEmpty(_data.CurrentFilePath) ? ResolveActiveFolder() : Path.GetDirectoryName(_data.CurrentFilePath);
			if (string.IsNullOrEmpty(baseDir))
				{
				baseDir = ResolveActiveFolder();
				}

			string rel = GetRelativePath(baseDir, filePath).Replace('\\', '/');
			// This would be handled by the editor module
			SetStatus($"🖼️ Ready to insert: {rel}");
			}

		private string DuplicateFile (string srcPath)
			{
			if (string.IsNullOrEmpty(srcPath) || !File.Exists(srcPath))
				{
				throw new FileNotFoundException("Source file not found", srcPath);
				}

			string target = GenerateUniqueCopyPath(srcPath);
			File.Copy(srcPath, target, overwrite: false);
			return target;
			}

		private string GenerateUniqueCopyPath (string srcPath)
			{
			string dir = Path.GetDirectoryName(srcPath) ?? "";
			string name = Path.GetFileNameWithoutExtension(srcPath);
			string ext = Path.GetExtension(srcPath);

			string candidate = Path.Combine(dir, $"{name} Copy{ext}");
			int i = 2;
			while (File.Exists(candidate))
				{
				candidate = Path.Combine(dir, $"{name} Copy {i}{ext}");
				i++;
				}
			return candidate;
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
					else if (line.StartsWith("**Tags"))
						{
						int idx = line.IndexOf(':');
						if (idx >= 0)
							{
							string v = line [ (idx + 1).. ].Trim();
							if (!string.IsNullOrEmpty(v))
								{
								_data.TagsCsv = v.Replace('#', ' ').Replace("  ", " ").Trim().Replace(' ', ',');
								}
							}
						}
					}
				}
			catch { }
			}

		private string MakeProjectRelative (string absPath)
			{
			string projectRoot = Directory.GetParent(Application.dataPath)!.FullName.Replace('\\', '/');
			string norm = absPath.Replace('\\', '/');
			return norm.StartsWith(projectRoot) ? norm [ (projectRoot.Length + 1).. ] : absPath;
			}

		private static string GetRelativePath (string baseDir, string fullPath)
			{
			// @jmeyer1980 ⚠ Intention ⚠ - Dual-L for legibility
			if (baseDir == null)
				throw new ArgumentNullException(nameof(baseDir));
			if (string.IsNullOrEmpty(baseDir) || string.IsNullOrEmpty(fullPath))
				return fullPath ?? string.Empty;

			try
				{
				var baseUri = new Uri(AppendDirectorySeparatorChar(baseDir));
				var pathUri = new Uri(fullPath);
				string rel = Uri.UnescapeDataString(baseUri.MakeRelativeUri(pathUri).ToString());
				return rel.Replace('/', Path.DirectorySeparatorChar);
				}
			catch { return fullPath; }
			}

		private static string AppendDirectorySeparatorChar (string path)
			{
			// @jmeyer1980 ⚠ Intention ⚠ - IL for legibility
			if (string.IsNullOrEmpty(path)) return path;

			char last = path [ ^1 ]; // range operator for last character
									 // @jmeyer1980 ⚠ Intention ⚠ - Dual-L for legibility
			return last == Path.DirectorySeparatorChar || last == Path.AltDirectorySeparatorChar ? path : path + Path.DirectorySeparatorChar;
			}

		private GUIStyle _navBackgroundStyle;

		private Texture2D CreateNavBackgroundTexture ()
			{
			var tex = new Texture2D(32, 32);
			Color bgColor = EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f, 1f) : new Color(0.85f, 0.85f, 0.85f, 1f);

			// 🎨 NO MORE BORDER: Just solid background color
			// The stubborn gradient dragon is hereby BANISHED!

			// Fill with solid background color only
			for (int y = 0; y < 32; y++)
				{
				for (int x = 0; x < 32; x++)
					{
					tex.SetPixel(x, y, bgColor); // Solid color, no border at all
					}
				}

			tex.Apply();
			return tex;
			}

		private void InitializeStyles ()
			{
			// 🔥 FORCE RECREATION: Always destroy cached textures when initializing
			if (_navBackgroundStyle?.normal?.background != null)
				{
				UnityEngine.Object.DestroyImmediate(_navBackgroundStyle.normal.background);
				_navBackgroundStyle = null; // Force complete recreation
				}

			// 🎨 Create completely new style with solid background
			_navBackgroundStyle = new GUIStyle("Box")
				{
				normal = {
					background = CreateNavBackgroundTexture(),
					textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black
				},
				border = new RectOffset(1, 1, 1, 1),
				padding = new RectOffset(10, 8, 8, 8)
				};
			}

		public void ForceRefresh ()
			{
			// 🔄 PUBLIC method to force complete style refresh
			// Call this when the window gains focus to override Unity's caching
			_navBackgroundStyle = null;
			InitializeStyles();

			// Force immediate repaint of window
			if (_window is EditorWindow editorWindow)
				{
				editorWindow.Repaint();
				}
			}
		}
	}
#endif
