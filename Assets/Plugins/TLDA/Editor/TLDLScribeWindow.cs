#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace LivingDevAgent.Editor
{
    public class TLDLScribeWindow : EditorWindow
    {
        // Header fields
        string _title = "";
        // KeeperNote: Default author seeded with @copilot so new entries always have attribution even if user forgets to fill.
        string _author = "@copilot";
        string _context = "";
        string _summary = "";
        string _tagsCsv = "";

        // Section toggles
        bool _includeDiscoveries = true;
        bool _includeActions = true;
        bool _includeTechnicalDetails = true;
        bool _includeDependencies = true;
        bool _includeLessons = true;
        bool _includeNextSteps = true;
        bool _includeReferences = true;
        bool _includeDevTimeTravel = true;
        bool _includeMetadata = true;
        bool _includeTerminalProof = false;

        // Discoveries / Actions dynamic lists
        [Serializable]
        class Discovery
        {
            public string Category = "";
            public string KeyFinding = "";
            public string Impact = "";
            public string Evidence = "";
            public string RootCause = "";
            public string PatternRecognition = "";
            public bool Foldout = true;
        }

        [Serializable]
        class ActionItem
        {
            public string Name = "";
            public string What = "";
            public string Why = "";
            public string How = "";
            public string Result = "";
            public string FilesChanged = "";
            public string Validation = "";
            public bool Foldout = true;
        }

        private static readonly List<Discovery> discoveries = new() { new Discovery { Category = "Discovery Category 1" } };
        readonly List<Discovery> _discoveries = new(discoveries);
        readonly List<ActionItem> _actions = new() { new ActionItem { Name = "Action 1" } };

        // Technical details
        string _codeChanges = "";
        string _configUpdates = "";
        string _terminalProof = "";

        // Dependencies
        string _depsAdded = "";   // one per line
        string _depsRemoved = ""; // one per line
        string _depsUpdated = ""; // one per line

        // Lessons learned
        string _lessonsWorked = "";
        string _lessonsImprove = "";
        string _lessonsGaps = "";

        // Next steps
        string _nextImmediate = "";
        string _nextMedium = "";
        string _nextLong = "";

        // References
        string _internalLinks = "";
        string _externalResources = "";
        private static readonly List<string> list = new();

        // References picker state
        readonly List<string> _referencePaths = new(list);

        // DevTimeTravel
        string _snapshotId = "";
        string _branch = "";
        string _commitHash = "";
        string _environment = "development";

        // Metadata
        enum Complexity { Low, Medium, High }
        enum Impact { Low, Medium, High, Critical }
        enum Status { Draft, InProgress, Complete, Archived }
        Complexity _complexity = Complexity.Medium;
        Impact _impact = Impact.Critical;
        string _teamMembers = "";
        string _duration = "";
        Status _status = Status.Draft;

        // UI state
        Vector2 _scroll;
        Vector2 _previewScroll;
        Vector2 _navScroll;
        Vector2 _rawScroll;                    // new: dedicated raw editor scroll
        int _tab = 0; // 0=Form, 1=Raw, 2=Preview
        string _statusLine = "Ready";
        bool _autoSyncEditor = false;              // new: auto-sync toggle
        string _lastFormSnapshot = null;           // new: last snapshot for auto-sync
        readonly string _rawEditorControlName = "TLDL_RAW_EDITOR"; // new: control name for raw editor
        int _rawCursorIndex = -1;                  // new: cached cursor index
        bool _pendingScrollToCursor = false;       // new: scroll flag after insertion

        // Left navigation (GitBook-like)
        string _rootPath;              // absolute path to gitbook root
        string _activeDirPath;         // absolute path of selected directory to save into
        string _currentFilePath;       // absolute path of currently opened file
        readonly Dictionary<string, bool> _folderExpanded = new();

        // Raw editor buffer
        string _rawContent = string.Empty;
        bool _rawWrap = true;
        bool _rawDirty = false;                 // new: tracks manual edits not reflected in form
        string _rawGeneratedSnapshot = null;    // new: snapshot of last form-generated content

        // Images
        bool _includeImages = false;
        readonly List<string> _imagePaths = new(); // project-relative or folder-relative like "images/file.png"
        readonly Dictionary<string, Texture2D> _imageCache = new();
        readonly List<string> _imageCacheOrder = new(); // new: order for LRU eviction
        const int ImageCacheMax = 128;                 // new: max cached images

        // Issue creator (templates)
        class TemplateInfo { public string Key; public string Title; public string File; public string AbsPath; }
        List<TemplateInfo> _templates = null;
        int _selectedTemplateIndex = 0;

        GUIStyle _labelWrap;
        GUIStyle _textAreaMonospace;
        GUIStyle _textAreaWrap;
        GUIStyle _h1, _h2, _h3, _bodyWrap, _listItem, _codeBlock;

        const string EditorPrefsRootKey = "LDA_TLDL_ROOT";

        [MenuItem("Tools/Living Dev Agent/The Scribe")]
        public static void OpenScribe()
        {
            OpenWindow("The Scribe");
        }

        [MenuItem("Tools/Living Dev Agent/TLDL Wizard (Deprecated)")]
        public static void OpenDeprecated()
        {
            OpenWindow("The Scribe (Deprecated)");
        }

        // Back-compat: keep old entry point name but route to Scribe
        public static void ShowWindow()
        {
            OpenWindow("The Scribe");
        }

        static void OpenWindow(string title)
        {
            var wnd = GetWindow<TLDLScribeWindow>(true, title, true);
            wnd.minSize = new Vector2(900, 600);
            wnd.Show();
        }

        void OnEnable()
        {
            static Font SafeFont(string family, int size)
            {
                try { return Font.CreateDynamicFontFromOSFont(family, size); } catch { return EditorStyles.textArea.font; }
            }
            _labelWrap = new GUIStyle(EditorStyles.label) { wordWrap = true };
            _textAreaMonospace = new GUIStyle(EditorStyles.textArea)
            {
                font = SafeFont("Consolas", 12),
                wordWrap = false
            };
            _textAreaWrap = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true
            };
            _bodyWrap = new GUIStyle(EditorStyles.label) { wordWrap = true, richText = true };
            _listItem = new GUIStyle(EditorStyles.label) { wordWrap = true, richText = true };
            _h1 = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, richText = true, wordWrap = true };
            _h2 = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, richText = true, wordWrap = true };
            _h3 = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12, richText = true, wordWrap = true };
            _codeBlock = new GUIStyle(EditorStyles.textArea) { font = SafeFont("Consolas", 12), wordWrap = false };

            // Load persisted root if available; otherwise, default to TLDA docs
            _rootPath = EditorPrefs.GetString(EditorPrefsRootKey, string.Empty);
            if (string.IsNullOrEmpty(_rootPath))
            {
                var defaultFolder = Path.Combine(Application.dataPath, "Plugins/TLDA/docs");
                if (Directory.Exists(defaultFolder))
                    _rootPath = defaultFolder;
            }
            if (!string.IsNullOrEmpty(_rootPath) && Directory.Exists(_rootPath))
            {
                if (string.IsNullOrEmpty(_activeDirPath)) _activeDirPath = _rootPath;
            }
        }

        void OnGUI()
        {
            DrawTopToolbar();

            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawNavigatorPanel(260);

                using (new EditorGUILayout.VerticalScope())
                {
                    var tabs = new[] { "Form", "Editor", "Preview" };
                    _tab = GUILayout.Toolbar(_tab, tabs);
                    EditorGUILayout.Space(6);

                    switch (_tab)
                    {
                        case 0: DrawForm(); break;
                        case 1: DrawRawEditor(); break;
                        case 2: DrawPreview(); break;
                    }
                }
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(_statusLine, MessageType.None);
        }

        void DrawTopToolbar()
        {
            // KeeperNote: Toolbar orchestrates high‑risk ops (generation / file writing). Each action flows through safety guards (dirty checks or dialog).
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // Root selection
                if (GUILayout.Button("Choose Root…", EditorStyles.toolbarButton, GUILayout.Width(110))) ChooseRootFolder();
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_rootPath)))
                {
                    if (GUILayout.Button("Open Root", EditorStyles.toolbarButton, GUILayout.Width(90))) OpenInFileBrowser(_rootPath);
                    if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70))) RefreshNavigator();
                }
                GUILayout.Space(8);

                // File ops
                if (GUILayout.Button("Load File…", EditorStyles.toolbarButton, GUILayout.Width(90))) LoadFileDialog();
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_rawContent)))
                {
                    if (GUILayout.Button("Save Raw", EditorStyles.toolbarButton, GUILayout.Width(80))) SaveRaw();
                    if (GUILayout.Button("Save Raw As…", EditorStyles.toolbarButton, GUILayout.Width(100))) SaveRawAs();
                }

                GUILayout.FlexibleSpace();

                // Form ops
                if (GUILayout.Button("Generate From Form → Editor", EditorStyles.toolbarButton))
                {
                    if (WarnOverwriteRawIfDirty())
                    {
                        var md = BuildMarkdown(GetCreatedTs());
                        _rawContent = md;
                        _rawGeneratedSnapshot = md;
                        _rawDirty = false;
                        _tab = 1; // jump to Editor
                        _statusLine = "Generated content from form into Editor";
                    }
                }
                if (GUILayout.Button("Create TLDL File", EditorStyles.toolbarButton)) TryCreate();
            }
        }

        void DrawNavigatorPanel(float width)
        {
            // KeeperNote: Left navigator turns scattered markdown into a browsable knowledge tree (GitBook style) with active directory context.
            using (new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(width), GUILayout.MinWidth(width)))
            {
                GUILayout.Label("Sudo-GitBook", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.TextField("Root", string.IsNullOrEmpty(_rootPath) ? "(not set)" : _rootPath);
                }

                _navScroll = EditorGUILayout.BeginScrollView(_navScroll, GUILayout.ExpandHeight(true));
                if (string.IsNullOrEmpty(_rootPath))
                {
                    EditorGUILayout.HelpBox("Choose a root folder to begin.", MessageType.Info);
                }
                else if (!Directory.Exists(_rootPath))
                {
                    EditorGUILayout.HelpBox("Root folder not found. Please choose a new root.", MessageType.Warning);
                }
                else
                {
                    DrawDirectoryNode(_rootPath, 0, GetIndent(0));
                }
                EditorGUILayout.EndScrollView();

                // Active dir indicator
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("Active Directory", string.IsNullOrEmpty(_activeDirPath) ? "(none)" : _activeDirPath);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_activeDirPath)))
                    {
                        if (GUILayout.Button("Open Folder")) OpenInFileBrowser(_activeDirPath);
                        if (GUILayout.Button("New Folder…")) CreateChildFolder();
                    }
                }
            }
        }

        private string GetIndent(int depth)
        {
            return new string(' ', depth * 2);
        }

        // Utility helpers (early placement so available to toolbar & form methods)
        // KeeperNote: Guards regeneration so the user does not accidentally lose bespoke raw edits when toggling form actions.
        bool WarnOverwriteRawIfDirty()
        {
            if (!_rawDirty) return true;
            return EditorUtility.DisplayDialog("Overwrite Raw Editor?", "You have unsaved manual edits in the raw editor. Overwrite with regenerated content?", "Overwrite", "Cancel");
        }
        // KeeperNote: Central image cache with LRU eviction prevents unbounded Texture2D accumulation during long authoring sessions.
        void AddTextureToCache(string key, Texture2D tex)
        {
            if (tex == null) return;
            _imageCache[key] = tex;
            _imageCacheOrder.Add(key);
            if (_imageCacheOrder.Count > ImageCacheMax)
            {
                var oldest = _imageCacheOrder[0];
                _imageCacheOrder.RemoveAt(0);
                if (_imageCache.TryGetValue(oldest, out var oldTex) && oldTex != null)
                {
                    DestroyImmediate(oldTex);
                }
                _imageCache.Remove(oldest);
            }
        }
        // KeeperNote: Lightweight header parser so reopening an existing markdown rehydrates core form inputs (author/context/summary/tags) for iterative refinement.
        void ParseBasicMetadata(string md)
        {
            if (string.IsNullOrEmpty(md)) return;
            try
            {
                using var reader = new StringReader(md);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("**Author:")) { var v = line["**Author:**".Length..].Trim(); if (!string.IsNullOrEmpty(v)) _author = v; }
                    else if (line.StartsWith("**Summary:")) { var v = line["**Summary:**".Length..].Trim(); if (!string.IsNullOrEmpty(v)) _summary = v; }
                    else if (line.StartsWith("**Context:")) { var v = line["**Context:**".Length..].Trim(); if (!string.IsNullOrEmpty(v)) _context = v; }
                    else if (line.StartsWith("**Tags"))
                    {
                        var idx = line.IndexOf(':');
                        if (idx >= 0)
                        {
                            var v = line[(idx + 1)..].Trim();
                            if (!string.IsNullOrEmpty(v)) _tagsCsv = v.Replace('#', ' ').Replace("  ", " ").Trim().Replace(' ', ',');
                        }
                    }
                }
            }
            catch { }
        }

        // PSEUDOCODE (non-destructive improvements):
        // - Introduce a single source of truth for allowed file extensions (HashSet) to avoid hardcoding scattered filters.
        // - Improve navigator usability non-destructively:
        //   - Sort folders and files alphabetically.
        //   - Add "Reveal" (open containing folder) and "Duplicate" (safe copy) actions for files.
        // - Fix PostWriteImport to robustly compute Unity-relative asset path and reimport reliably.
        // - Keep all existing behavior intact; only additive improvements and bug fix.
        //
        // CHANGES:
        // 1) Add field: s_AllowedExts
        // 2) Replace DrawDirectoryNode(...) method to use sorting, centralized ext filter, and new buttons
        // 3) Replace PostWriteImport(...) with a correct implementation
        // 4) Add helpers: MakeUnityPath, DuplicateFile, GenerateUniqueCopyPath, OpenContainingFolderOfFile

        // 1) Add near other fields in TLDLWizardWindow (e.g., after _folderExpanded):
        static readonly HashSet<string> s_AllowedExts = new(StringComparer.OrdinalIgnoreCase)
        {
            ".md", ".markdown", ".txt", ".log", ".xml",
            // Image types (for navigator visibility)
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tga"
        };
        static readonly HashSet<string> s_ImageExts = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tga"
        };
        // KeeperNote: Above two hash sets centralize file-type policy making it trivial to widen support (e.g., add .csv) without touching traversal logic.
        // 2) Replace the entire DrawDirectoryNode method with this version
        void DrawDirectoryNode(string path, int depth, string indent)
        {
            // KeeperNote: Non-destructive upgrade — adds thumbnails, duplication, reveal-in-finder & stable sorting while preserving legacy traversal semantics.
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return;

            string folderName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(folderName)) folderName = path;
            if (!_folderExpanded.ContainsKey(path)) _folderExpanded[path] = depth <= 1; // expand root/top-level by default

            var labelPrefix = indent ?? string.Empty;
            using (new EditorGUILayout.HorizontalScope())
            {
                _folderExpanded[path] = EditorGUILayout.Foldout(_folderExpanded[path], labelPrefix + folderName, true);
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    _activeDirPath = path;
                    _statusLine = $"Active directory set: {path}";
                }
            }

            if (!_folderExpanded[path]) return;

            try
            {
                // Sort subdirectories for stable nav
                var subDirs = Directory.GetDirectories(path);
                Array.Sort(subDirs, StringComparer.OrdinalIgnoreCase);
                foreach (var d in subDirs)
                {
                    EditorGUI.indentLevel++;
                    DrawDirectoryNode(d, depth + 1, GetIndent(depth + 1));
                    EditorGUI.indentLevel--;
                }

                // Sort files for stable nav
                var files = Directory.GetFiles(path);
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                foreach (var f in files)
                {
                    var ext = Path.GetExtension(f);
                    if (!s_AllowedExts.Contains(ext)) continue;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.indentLevel++;
                        var fileName = Path.GetFileName(f);
                        var fileLabel = (indent ?? string.Empty) + "  " + fileName;
                        bool isImage = s_ImageExts.Contains(ext);

                        // Optional thumbnail for images
                        if (isImage)
                        {
                            if (!_imageCache.TryGetValue(f, out var tex) || tex == null)
                            {
                                try
                                {
                                    var bytes = File.ReadAllBytes(f);
                                    if (bytes != null)
                                    {
                                        tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                                        if (tex.LoadImage(bytes))
                                        {
                                            AddTextureToCache(f, tex);
                                        }
                                        else
                                        {
                                            DestroyImmediate(tex);
                                            tex = null;
                                        }
                                    }
                                }
                                catch { /* ignore IO/format errors */ }
                            }
                            if (_imageCache.TryGetValue(f, out var thumb) && thumb != null)
                            {
                                const float maxThumb = 36f;
                                var aspect = (float)thumb.width / Mathf.Max(1, thumb.height);
                                var w = Mathf.Min(maxThumb * aspect, maxThumb);
                                var h = Mathf.Min(maxThumb, maxThumb / Mathf.Max(0.01f, aspect));
                                GUILayout.Label(thumb, GUILayout.Width(w), GUILayout.Height(h));
                            }
                            else
                            {
                                GUILayout.Space(4);
                            }
                        }

                        if (GUILayout.Button(fileLabel, EditorStyles.label))
                        {
                            if (isImage)
                            {
                                // Copy markdown image link instead of inserting directly
                                var baseDir = string.IsNullOrEmpty(_currentFilePath) ? ResolveActiveFolder() : Path.GetDirectoryName(_currentFilePath);
                                if (string.IsNullOrEmpty(baseDir)) baseDir = ResolveActiveFolder();
                                var rel = GetRelativePath(baseDir, f).Replace('\\', '/');
                                CopyImageMarkdownLink(f, rel);
                            }
                            else
                            {
                                LoadFile(f);
                            }
                        }
                        if (!isImage)
                        {
                            if (GUILayout.Button("Open", GUILayout.Width(50)))
                            {
                                LoadFile(f);
                            }
                        }
                        if (GUILayout.Button("Reveal", GUILayout.Width(60)))
                        {
                            OpenContainingFolderOfFile(f);
                        }
                        if (isImage)
                        {
                            if (GUILayout.Button("Insert", GUILayout.Width(60)))
                            {
                                var baseDir = string.IsNullOrEmpty(_currentFilePath) ? ResolveActiveFolder() : Path.GetDirectoryName(_currentFilePath);
                                if (string.IsNullOrEmpty(baseDir)) baseDir = ResolveActiveFolder();
                                var rel = GetRelativePath(baseDir, f).Replace('\\', '/');
                                InsertImageMarkdownAtCursor(rel);
                            }
                        }
                        if (GUILayout.Button("Duplicate", GUILayout.Width(80)))
                        {
                            try
                            {
                                var copy = DuplicateFile(f);
                                _statusLine = $"Duplicated: {MakeProjectRelative(copy)}";
                                RefreshNavigator();
                            }
                            catch (Exception ex)
                            {
                                _statusLine = $"Duplicate failed: {ex.Message}";
                                Debug.LogError($"[The Scribe] Duplicate failed: {ex}");
                            }
                        }
                        EditorGUI.indentLevel--;
                    }
                }
            }
            catch (Exception e)
            {
                EditorGUILayout.HelpBox($"Error reading directory: {e.Message}", MessageType.Warning);
            }
        }

        // 3) Replace PostWriteImport with a correct, minimal implementation
        void PostWriteImport(string absPath)
        {
            if (string.IsNullOrEmpty(absPath)) return;
            var unityPath = MakeUnityPath(absPath);
            if (!string.IsNullOrEmpty(unityPath))
            {
                AssetDatabase.ImportAsset(unityPath, ImportAssetOptions.ForceSynchronousImport);
            }
        }

        // 4) Add helpers
        string MakeUnityPath(string absPath)
        {
            var norm = absPath.Replace('\\', '/');
            var dataPath = Application.dataPath.Replace('\\', '/');
            if (norm.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                // Convert absolute path under Assets to "Assets/..." path
                return "Assets" + norm[dataPath.Length..];
            }
            return null;
        }

        string DuplicateFile(string srcPath)
        {
            // KeeperNote: Safe copy pattern: never overwrite originals, enabling exploratory edits on documentation snapshots.
            if (string.IsNullOrEmpty(srcPath) || !File.Exists(srcPath))
                throw new FileNotFoundException("Source file not found", srcPath);

            var target = GenerateUniqueCopyPath(srcPath);
            File.Copy(srcPath, target, overwrite: false);
            PostWriteImport(target);
            return target;
        }

        string GenerateUniqueCopyPath(string srcPath)
        {
            // KeeperNote: Incremental naming strategy ("Name Copy", "Name Copy 2", ...) mirrors common OS UX for predictability.
            var dir = Path.GetDirectoryName(srcPath) ?? "";
            var name = Path.GetFileNameWithoutExtension(srcPath);
            var ext = Path.GetExtension(srcPath);

            // Try "Name Copy.ext", then "Name Copy 2.ext", etc.
            var candidate = Path.Combine(dir, $"{name} Copy{ext}");
            int i = 2;
            while (File.Exists(candidate))
            {
                candidate = Path.Combine(dir, $"{name} Copy {i}{ext}");
                i++;
            }
            return candidate;
        }

        void OpenContainingFolderOfFile(string absPath)
        {
            if (string.IsNullOrEmpty(absPath)) return;
            EditorUtility.RevealInFinder(absPath);
        }

        void DrawForm()
        {
            var viewportHeight = Mathf.Max(140f, position.height - 220f);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(viewportHeight), GUILayout.ExpandHeight(true));

            // Header
            GUILayout.Label("Header", EditorStyles.boldLabel);
            DrawHelp("Title", "Short, descriptive. Used in the filename.");
            _title = EditorGUILayout.TextField("Title", _title);
            _author = EditorGUILayout.TextField("Author", string.IsNullOrWhiteSpace(_author) ? "@copilot" : _author);
            DrawPlaceholder("Context", "Issue #XX, Feature Name, or short description.");
            _context = EditorGUILayout.TextArea(_context, _textAreaWrap, GUILayout.MinHeight(40), GUILayout.ExpandWidth(true));
            DrawPlaceholder("Summary", "One line describing the result.");
            _summary = EditorGUILayout.TextArea(_summary, _textAreaWrap, GUILayout.MinHeight(40), GUILayout.ExpandWidth(true));
            DrawPlaceholder("Tags (comma-separated)", "e.g., Chronicle Keeper, LDA, Docs");
            _tagsCsv = EditorGUILayout.TextField("Tags", _tagsCsv);

            EditorGUILayout.Space(8);
            GUILayout.Label("Sections", EditorStyles.boldLabel);
            _includeDiscoveries = EditorGUILayout.ToggleLeft("Include Discoveries", _includeDiscoveries);
            if (_includeDiscoveries) DrawDiscoveries();

            _includeActions = EditorGUILayout.ToggleLeft("Include Actions Taken", _includeActions);
            if (_includeActions) DrawActions();

            _includeTechnicalDetails = EditorGUILayout.ToggleLeft("Include Technical Details (Code/Config)", _includeTechnicalDetails);
            if (_includeTechnicalDetails) DrawTechnicalDetails();

            _includeTerminalProof = EditorGUILayout.ToggleLeft("Include Terminal Proof", _includeTerminalProof);
            if (_includeTerminalProof) DrawTerminalProof();

            _includeDependencies = EditorGUILayout.ToggleLeft("Include Dependencies", _includeDependencies);
            if (_includeDependencies) DrawDependencies();

            _includeLessons = EditorGUILayout.ToggleLeft("Include Lessons Learned", _includeLessons);
            if (_includeLessons) DrawLessons();

            _includeNextSteps = EditorGUILayout.ToggleLeft("Include Next Steps", _includeNextSteps);
            if (_includeNextSteps) DrawNextSteps();

            _includeReferences = EditorGUILayout.ToggleLeft("Include References", _includeReferences);
            if (_includeReferences) DrawReferences();

            _includeDevTimeTravel = EditorGUILayout.ToggleLeft("Include DevTimeTravel Context", _includeDevTimeTravel);
            if (_includeDevTimeTravel) DrawDevTimeTravel();

            _includeMetadata = EditorGUILayout.ToggleLeft("Include Metadata", _includeMetadata);
            if (_includeMetadata) DrawMetadata();

            // Images
            _includeImages = EditorGUILayout.ToggleLeft("Include Images", _includeImages);
            if (_includeImages) DrawImagesSection();

            EditorGUILayout.Space(12);
            DrawIssueCreator();

            EditorGUILayout.EndScrollView();

            // --- Form → Editor controls (added for clarity) ---
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Form → Editor Sync", EditorStyles.boldLabel);
            _autoSyncEditor = EditorGUILayout.ToggleLeft("Auto-sync changes to Editor (raw)", _autoSyncEditor);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Update Editor From Form", GUILayout.Width(200)))
                {
                    if (WarnOverwriteRawIfDirty())
                    {
                        _rawContent = BuildMarkdown(GetCreatedTs());
                        _rawGeneratedSnapshot = _rawContent;
                        _rawDirty = false;
                        _tab = 1; // jump to Editor
                        _lastFormSnapshot = BuildFormSnapshot();
                        _statusLine = "Editor updated from form";
                    }
                }
                if (GUILayout.Button("Preview From Form", GUILayout.Width(160)))
                {
                    if (WarnOverwriteRawIfDirty())
                    {
                        _rawContent = BuildMarkdown(GetCreatedTs());
                        _rawGeneratedSnapshot = _rawContent;
                        _rawDirty = false;
                        _lastFormSnapshot = BuildFormSnapshot();
                        _tab = 2; // preview
                        _statusLine = "Preview generated from form";
                    }
                }
            }
            EditorGUILayout.HelpBox("Use 'Update Editor From Form' to push current form data into the raw markdown buffer. Enable auto-sync to regenerate whenever form content changes.", MessageType.Info);
            EditorGUILayout.EndVertical();

            // Auto-sync processing after UI so we capture latest edits
            if (_autoSyncEditor)
            {
                // KeeperNote: Auto-sync eliminates drift while respecting manual raw edits (skips when dirty) — acts like a guarded one-way binding.
                var snap = BuildFormSnapshot();
                if (snap != _lastFormSnapshot)
                {
                    if (_rawDirty)
                    {
                        _statusLine = "Auto-sync skipped (raw editor has manual edits)";
                    }
                    else
                    {
                        _rawContent = BuildMarkdown(GetCreatedTs());
                        _rawGeneratedSnapshot = _rawContent;
                        _rawDirty = false;
                        _lastFormSnapshot = snap;
                        _statusLine = "Editor auto-synced from form";
                    }
                }
            }
        }

        void DrawImagesSection()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Images", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Images will be copied into an 'images' subfolder under the chosen documentation folder. Use the buttons to insert Markdown image links at the cursor in the editor.", MessageType.Info);

            if (_imagePaths.Count > 0)
            {
                for (int i = 0; i < _imagePaths.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(_imagePaths[i], GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("Insert Link", GUILayout.Width(90)))
                    {
                        InsertImageMarkdownAtCursor(_imagePaths[i]);
                    }
                    if (GUILayout.Button("Remove", GUILayout.Width(70)))
                    {
                        _imagePaths.RemoveAt(i); i--; continue;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Image…", GUILayout.Width(120))) AddImageFile();
                if (GUILayout.Button("Open Images Folder", GUILayout.Width(160)))
                {
                    var imagesDir = EnsureImagesDirectory();
                    if (!string.IsNullOrEmpty(imagesDir)) OpenInFileBrowser(imagesDir);
                }
            }

            EditorGUILayout.EndVertical();
        }

        void DrawIssueCreator()
        {
            GUILayout.Label("Issue Creator", EditorStyles.boldLabel);
            EnsureTemplatesLoaded();
            if (_templates == null || _templates.Count == 0)
            {
                EditorGUILayout.HelpBox("No templates found. Ensure templates/comments/registry.yaml exists at project root.", MessageType.Warning);
                return;
            }

            var items = new string[_templates.Count];
            for (int i = 0; i < _templates.Count; i++)
            {
                items[i] = string.IsNullOrEmpty(_templates[i].Title) ? _templates[i].Key : _templates[i].Title;
            }
            _selectedTemplateIndex = EditorGUILayout.Popup("Template", _selectedTemplateIndex, items);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Load Template → Editor"))
                {
                    var md = LoadTemplateMarkdown(_templates[_selectedTemplateIndex]);
                    _rawContent = md ?? string.Empty;
                    _tab = 1;
                    _statusLine = "Loaded template into Editor";
                }
                if (GUILayout.Button("Create Issue From Template"))
                {
                    CreateIssueFromTemplate(_templates[_selectedTemplateIndex]);
                }
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Issues Directory", GetIssuesDirectory());
            }
        }

        void DrawDiscoveries()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Discoveries", EditorStyles.boldLabel);
            DrawHelp("Guidance", "Capture key findings, impacts, evidence, and root causes. Add as many as needed.");

            for (int i = 0; i < _discoveries.Count; i++)
            {
                var d = _discoveries[i];
                EditorGUILayout.BeginVertical("frameBox");
                d.Foldout = EditorGUILayout.Foldout(d.Foldout, string.IsNullOrEmpty(d.Category) ? $"Discovery {i + 1}" : d.Category, true);
                if (d.Foldout)
                {
                    d.Category = EditorGUILayout.TextField("Category/Title", d.Category);
                    d.KeyFinding = EditorGUILayout.TextArea(d.KeyFinding, _textAreaWrap, GUILayout.MinHeight(40), GUILayout.ExpandWidth(true));
                    LabelSmall("Key Finding");
                    d.Impact = EditorGUILayout.TextArea(d.Impact, _textAreaWrap, GUILayout.MinHeight(40), GUILayout.ExpandWidth(true));
                    LabelSmall("Impact");
                    d.Evidence = EditorGUILayout.TextArea(d.Evidence, _textAreaWrap, GUILayout.MinHeight(40), GUILayout.ExpandWidth(true));
                    LabelSmall("Evidence");
                    d.RootCause = EditorGUILayout.TextArea(d.RootCause, _textAreaWrap, GUILayout.MinHeight(40), GUILayout.ExpandWidth(true));
                    LabelSmall("Root Cause (optional)");
                    d.PatternRecognition = EditorGUILayout.TextArea(d.PatternRecognition, _textAreaWrap, GUILayout.MinHeight(40), GUILayout.ExpandWidth(true));
                    LabelSmall("Pattern Recognition (optional)");

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Remove")) { _discoveries.RemoveAt(i); i--; continue; }
                        if (GUILayout.Button("Duplicate")) { _discoveries.Insert(i + 1, Clone(d)); }
                    }
                }
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ Add Discovery")) _discoveries.Add(new Discovery { Category = $"Discovery {_discoveries.Count + 1}" });
            EditorGUILayout.EndVertical();
        }

        void DrawActions()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Actions Taken", EditorStyles.boldLabel);
            DrawHelp("Guidance", "Record what you did, why, how, result, and validation.");

            for (int i = 0; i < _actions.Count; i++)
            {
                var a = _actions[i];
                EditorGUILayout.BeginVertical("frameBox");
                a.Foldout = EditorGUILayout.Foldout(a.Foldout, string.IsNullOrEmpty(a.Name) ? $"Action {i + 1}" : a.Name, true);
                if (a.Foldout)
                {
                    a.Name = EditorGUILayout.TextField("Action Name", a.Name);
                    a.What = EditorGUILayout.TextArea(a.What, _textAreaWrap, GUILayout.MinHeight(40), GUILayout.ExpandWidth(true)); LabelSmall("What");
                    a.Why = EditorGUILayout.TextArea(a.Why, _textAreaWrap, GUILayout.MinHeight(40), GUILayout.ExpandWidth(true)); LabelSmall("Why");
                    a.How = EditorGUILayout.TextArea(a.How, _textAreaWrap, GUILayout.MinHeight(40), GUILayout.ExpandWidth(true)); LabelSmall("How");
                    a.Result = EditorGUILayout.TextArea(a.Result, _textAreaWrap, GUILayout.MinHeight(40), GUILayout.ExpandWidth(true)); LabelSmall("Result");
                    a.FilesChanged = EditorGUILayout.TextArea(a.FilesChanged, _textAreaWrap, GUILayout.MinHeight(40), GUILayout.ExpandWidth(true)); LabelSmall("Files Changed (optional)");
                    a.Validation = EditorGUILayout.TextArea(a.Validation, _textAreaWrap, GUILayout.MinHeight(40), GUILayout.ExpandWidth(true)); LabelSmall("Validation (optional)");

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Remove")) { _actions.RemoveAt(i); i--; continue; }
                        if (GUILayout.Button("Duplicate")) { _actions.Insert(i + 1, Clone(a)); }
                    }
                }
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ Add Action")) _actions.Add(new ActionItem { Name = $"Action {_actions.Count + 1}" });
            EditorGUILayout.EndVertical();
        }

        void DrawTechnicalDetails()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Technical Details", EditorStyles.boldLabel);

            GUILayout.Label("Code Changes (diff or notes)", _labelWrap);
            _codeChanges = EditorGUILayout.TextArea(_codeChanges, _textAreaMonospace, GUILayout.MinHeight(80), GUILayout.ExpandWidth(true));

            GUILayout.Label("Configuration Updates (YAML/JSON)", _labelWrap);
            _configUpdates = EditorGUILayout.TextArea(_configUpdates, _textAreaMonospace, GUILayout.MinHeight(80), GUILayout.ExpandWidth(true));

            EditorGUILayout.EndVertical();
        }

        void DrawTerminalProof()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Terminal Proof of Work", EditorStyles.boldLabel);
            DrawHelp("Hint", "Paste commands and outputs that prove the work.");
            _terminalProof = EditorGUILayout.TextArea(_terminalProof, _textAreaMonospace, GUILayout.MinHeight(80), GUILayout.ExpandWidth(true));
            EditorGUILayout.EndVertical();
        }

        void DrawDependencies()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Dependencies", EditorStyles.boldLabel);
            _depsAdded = LabeledLines("Added (one per line)", _depsAdded);
            _depsRemoved = LabeledLines("Removed (one per line)", _depsRemoved);
            _depsUpdated = LabeledLines("Updated (name version)", _depsUpdated);
            EditorGUILayout.EndVertical();
        }

        void DrawLessons()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Lessons Learned", EditorStyles.boldLabel);
            _lessonsWorked = LabeledMultiline("What Worked Well", _lessonsWorked);
            _lessonsImprove = LabeledMultiline("What Could Be Improved", _lessonsImprove);
            _lessonsGaps = LabeledMultiline("Knowledge Gaps Identified", _lessonsGaps);
            EditorGUILayout.EndVertical();
        }

        void DrawNextSteps()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Next Steps", EditorStyles.boldLabel);
            _nextImmediate = LabeledChecklist("Immediate Actions (High)", _nextImmediate);
            _nextMedium = LabeledChecklist("Medium-term Actions", _nextMedium);
            _nextLong = LabeledChecklist("Long-term Considerations", _nextLong);
            EditorGUILayout.EndVertical();
        }

        void DrawReferences()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("References", EditorStyles.boldLabel);

            // Drag-and-drop box
            var dropRect = GUILayoutUtility.GetRect(0, 60, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "Drag & Drop assets/files here", EditorStyles.helpBox);
            HandleReferenceDragAndDrop(dropRect);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Selected Assets")) AddSelectedAssetsAsReferences();
                if (GUILayout.Button("Add Files…")) AddFilesAsReferences();
            }

            // List current references with remove buttons
            if (_referencePaths.Count > 0)
            {
                GUILayout.Label("Linked Files", EditorStyles.miniBoldLabel);
                for (var i = 0; i < _referencePaths.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(_referencePaths[i], GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("Remove", GUILayout.Width(70))) { _referencePaths.RemoveAt(i); i--; }
                    EditorGUILayout.EndHorizontal();
                }
            }

            // Free-form text areas for additional links
            _internalLinks = LabeledLines("Internal Links (Markdown list)", _internalLinks);
            _externalResources = LabeledLines("External Resources (Markdown list)", _externalResources);

            EditorGUILayout.EndVertical();
        }

        void DrawRawEditor()
        {
            GUILayout.Label("Editor", EditorStyles.boldLabel);
            _rawWrap = EditorGUILayout.ToggleLeft("Wrap lines", _rawWrap);

            var rawStyle = new GUIStyle(_textAreaMonospace) { wordWrap = _rawWrap };
            var viewportHeight = Mathf.Max(140f, position.height - 240f);
            _rawScroll = EditorGUILayout.BeginScrollView(_rawScroll, GUILayout.Height(viewportHeight), GUILayout.ExpandHeight(true));
            GUI.SetNextControlName(_rawEditorControlName);
            // Use current buffer directly; do not pre-calc height which caused clipping / invisible appended lines
            var edited = EditorGUILayout.TextArea(_rawContent ?? string.Empty, rawStyle, GUILayout.ExpandHeight(true));
            if (edited != _rawContent)
            {
                if (_rawGeneratedSnapshot != null && edited != _rawGeneratedSnapshot)
                    _rawDirty = true; // user diverged from generated content
                _rawContent = edited;
            }

            // Capture cursor index if focused
            if (GUI.GetNameOfFocusedControl() == _rawEditorControlName)
            {
                var te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                if (te != null)
                {
                    _rawCursorIndex = te.cursorIndex;
                    if (_pendingScrollToCursor)
                    {
                        // Rough scroll heuristic: count newlines up to cursor and estimate line height
                        int line = 0;
                        if (!string.IsNullOrEmpty(_rawContent) && _rawCursorIndex > 0)
                        {
                            for (int i = 0; i < Math.Min(_rawCursorIndex, _rawContent.Length); i++)
                                if (_rawContent[i] == '\n') line++;
                        }
                        _rawScroll.y = line * 18f; // approx line height
                        _pendingScrollToCursor = false;
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Load File…", GUILayout.Width(110))) LoadFileDialog();
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_rawContent)))
                {
                    if (GUILayout.Button("Save Raw")) SaveRaw();
                    if (GUILayout.Button("Save Raw As…")) SaveRawAs();
                }
                if (GUILayout.Button("Insert Image…", GUILayout.Width(120))) AddImageAndInsertAtCursor();
            }
        }

        void HandleReferenceDragAndDrop(Rect area)
        {
            var evt = Event.current;
            if (!area.Contains(evt.mousePosition)) return;

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        var path = AssetDatabase.GetAssetPath(obj);
                        if (!string.IsNullOrEmpty(path)) TryAddReferencePath(path);
                    }
                    if (DragAndDrop.paths != null)
                    {
                        foreach (var raw in DragAndDrop.paths)
                        {
                            if (!string.IsNullOrEmpty(raw)) TryAddReferencePath(raw);
                        }
                    }
                }
                evt.Use();
            }
        }

        void AddSelectedAssetsAsReferences()
        {
            var objs = Selection.objects;
            foreach (var obj in objs)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path)) TryAddReferencePath(path);
            }
        }

        void AddFilesAsReferences()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            var dir = Application.dataPath;
            // Allow common docs
            var filters = new[] { "All", "*.*", "Markdown", "md,markdown", "Text", "txt,log", "YAML", "yaml,yml", "JSON", "json", "XML", "xml" };
            var picked = EditorUtility.OpenFilePanelWithFilters("Add Reference File", dir, filters);
            if (!string.IsNullOrEmpty(picked))
            {
                var rel = ToProjectRelativePath(picked, projectRoot) ?? picked;
                TryAddReferencePath(rel);
            }
        }

        void TryAddReferencePath(string path)
        {
            var norm = path.Replace('\\', '/');
            if (!_referencePaths.Contains(norm)) _referencePaths.Add(norm);
        }

        static string ToProjectRelativePath(string absPath, string projectRoot)
        {
            absPath = absPath.Replace('\\', '/');
            projectRoot = projectRoot.Replace('\\', '/');
            if (absPath.StartsWith(projectRoot))
            {
                var rel = absPath[projectRoot.Length..].TrimStart('/');
                return rel;
            }
            return null;
        }

        string MakeProjectRelative(string absPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName.Replace('\\', '/');
            var norm = absPath.Replace('\\', '/');
            if (norm.StartsWith(projectRoot))
            {
                return norm[(projectRoot.Length + 1)..];
            }
            return absPath;
        }

        private string GetCreatedTs()
        {
            return DateTime.UtcNow.ToString(format: "yyyy-MM-dd HH:mm:ss 'UTC'");
        }

        private string GetCreatedNormalizedTs()
        {
            return DateTime.UtcNow.ToString(format: "yyyy-MM-dd-HHmmss-utc");
        }

        string BuildMarkdown(string createdTs, string dateOverride = null, string safeTitleOverride = null)
        {
            // KeeperNote: Deterministic section builder; toggles act as feature flags so raw regen remains idempotent given same form snapshot.
            var date = string.IsNullOrEmpty(dateOverride) ? DateTime.UtcNow.ToString("yyyy-MM-dd") : dateOverride;
            var safeTitle = string.IsNullOrEmpty(safeTitleOverride) ? ScribeUtils.SanitizeTitle(_title) : safeTitleOverride;

            var sb = new StringBuilder();
            sb.AppendLine("# TLDL Entry Template");
            sb.AppendLine($"**Entry ID:** TLDL-{date}-{GetCreatedNormalizedTs()}-{safeTitle}");
            sb.AppendLine($"**Author:** {(_author?.Trim().Length > 0 ? _author.Trim() : "@copilot")} ");
            sb.AppendLine($"**Context:** {_context}");
            sb.AppendLine($"**Summary:** {_summary}");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine("");
            sb.AppendLine("> 📜 \"[Insert inspirational quote from Secret Art of the Living Dev using: `python3 src/ScrollQuoteEngine/quote_engine.py --context documentation --format markdown`]\"");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            if (_includeDiscoveries)
            {
                sb.AppendLine("## Discoveries");
                sb.AppendLine();
                foreach (var d in _discoveries)
                {
                    var heading = string.IsNullOrWhiteSpace(d.Category) ? "[Discovery]" : $"[{d.Category}]";
                    sb.AppendLine($"### {heading}");
                    if (!string.IsNullOrWhiteSpace(d.KeyFinding)) sb.AppendLine($"- **Key Finding**: {d.KeyFinding}");
                    if (!string.IsNullOrWhiteSpace(d.Impact)) sb.AppendLine($"- **Impact**: {d.Impact}");
                    if (!string.IsNullOrWhiteSpace(d.Evidence)) sb.AppendLine($"- **Evidence**: {d.Evidence}");
                    if (!string.IsNullOrWhiteSpace(d.RootCause)) sb.AppendLine($"- **Root Cause**: {d.RootCause}");
                    if (!string.IsNullOrWhiteSpace(d.PatternRecognition)) sb.AppendLine($"- **Pattern Recognition**: {d.PatternRecognition}");
                    sb.AppendLine();
                }
            }

            if (_includeActions)
            {
                sb.AppendLine("## Actions Taken");
                sb.AppendLine();
                for (int i = 0; i < _actions.Count; i++)
                {
                    var a = _actions[i];
                    var idx = i + 1;
                    sb.AppendLine($"{idx}. **[{(string.IsNullOrWhiteSpace(a.Name) ? $"Action {idx}" : a.Name)}]**");
                    if (!string.IsNullOrWhiteSpace(a.What)) sb.AppendLine($"   - **What**: {a.What}");
                    if (!string.IsNullOrWhiteSpace(a.Why)) sb.AppendLine($"   - **Why**: {a.Why}");
                    if (!string.IsNullOrWhiteSpace(a.How)) sb.AppendLine($"   - **How**: {a.How}");
                    if (!string.IsNullOrWhiteSpace(a.Result)) sb.AppendLine($"   - **Result**: {a.Result}");
                    if (!string.IsNullOrWhiteSpace(a.FilesChanged)) sb.AppendLine($"   - **Files Changed**: {a.FilesChanged}");
                    if (!string.IsNullOrWhiteSpace(a.Validation)) sb.AppendLine($"   - **Validation**: {a.Validation}");
                    sb.AppendLine();
                }
            }

            if (_includeTechnicalDetails)
            {
                sb.AppendLine("## Technical Details");
                sb.AppendLine();
                if (!string.IsNullOrWhiteSpace(_codeChanges))
                {
                    sb.AppendLine("### Code Changes");
                    sb.AppendLine("```diff");
                    sb.AppendLine(_codeChanges);
                    sb.AppendLine("```");
                    sb.AppendLine();
                }
                if (!string.IsNullOrWhiteSpace(_configUpdates))
                {
                    sb.AppendLine("### Configuration Updates");
                    sb.AppendLine("```yaml");
                    sb.AppendLine(_configUpdates);
                    sb.AppendLine("```");
                    sb.AppendLine();
                }
            }

            if (_includeTerminalProof && !string.IsNullOrWhiteSpace(_terminalProof))
            {
                sb.AppendLine("### Terminal Proof of Work");
                sb.AppendLine("```");
                sb.AppendLine(_terminalProof);
                sb.AppendLine("```");
                sb.AppendLine();
            }

            if (_includeDependencies)
            {
                sb.AppendLine("### Dependencies");
                if (!string.IsNullOrWhiteSpace(_depsAdded)) sb.AppendLine($"- **Added**:\n{ScribeUtils.Bulletize(_depsAdded)}");
                if (!string.IsNullOrWhiteSpace(_depsRemoved)) sb.AppendLine($"- **Removed**:\n{ScribeUtils.Bulletize(_depsRemoved)}");
                if (!string.IsNullOrWhiteSpace(_depsUpdated)) sb.AppendLine($"- **Updated**:\n{ScribeUtils.Bulletize(_depsUpdated)}");
                sb.AppendLine();
            }

            // NEW: Images section so auto-sync preserves inserted image references
            if (_includeImages && _imagePaths.Count > 0)
            {
                sb.AppendLine("## Images");
                sb.AppendLine();
                foreach (var img in _imagePaths)
                {
                    if (string.IsNullOrWhiteSpace(img)) continue;
                    var alt = Path.GetFileNameWithoutExtension(img);
                    sb.AppendLine($"![{alt}]({img})");
                }
                sb.AppendLine();
            }

            if (_includeLessons)
            {
                sb.AppendLine("## Lessons Learned");
                if (!string.IsNullOrWhiteSpace(_lessonsWorked)) sb.AppendLine($"\n### What Worked Well\n{ScribeUtils.Bulletize(_lessonsWorked)}");
                if (!string.IsNullOrWhiteSpace(_lessonsImprove)) sb.AppendLine($"\n### What Could Be Improved\n{ScribeUtils.Bulletize(_lessonsImprove)}");
                if (!string.IsNullOrWhiteSpace(_lessonsGaps)) sb.AppendLine($"\n### Knowledge Gaps Identified\n{ScribeUtils.Bulletize(_lessonsGaps)}");
                sb.AppendLine();
            }

            if (_includeNextSteps)
            {
                sb.AppendLine("## Next Steps");
                if (!string.IsNullOrWhiteSpace(_nextImmediate)) sb.AppendLine($"\n### Immediate Actions (High Priority)\n{ScribeUtils.Checklist(_nextImmediate)}");
                if (!string.IsNullOrWhiteSpace(_nextMedium)) sb.AppendLine($"\n### Medium-term Actions (Medium Priority)\n{ScribeUtils.Checklist(_nextMedium)}");
                if (!string.IsNullOrWhiteSpace(_nextLong)) sb.AppendLine($"\n### Long-term Considerations (Low Priority)\n{ScribeUtils.Checklist(_nextLong)}");
                sb.AppendLine();
            }

            if (_includeReferences)
            {
                sb.AppendLine("## References");
                sb.AppendLine();

                if (_referencePaths.Count > 0)
                {
                    sb.AppendLine("### Internal Links");
                    foreach (var p in _referencePaths)
                    {
                        var fileName = Path.GetFileName(p);
                        var linkPath = p.Replace('\\', '/');
                        sb.AppendLine($"- [{fileName}]({linkPath})");
                    }
                    sb.AppendLine();
                }

                if (!string.IsNullOrWhiteSpace(_internalLinks))
                {
                    sb.AppendLine("### Internal Links");
                    sb.AppendLine(_internalLinks);
                    sb.AppendLine();
                }

                if (!string.IsNullOrWhiteSpace(_externalResources))
                {
                    sb.AppendLine("### External Resources");
                    sb.AppendLine(_externalResources);
                    sb.AppendLine();
                }
            }

            if (_includeDevTimeTravel)
            {
                sb.AppendLine("## DevTimeTravel Context");
                sb.AppendLine();
                sb.AppendLine("### Snapshot Information");
                if (!string.IsNullOrWhiteSpace(_snapshotId)) sb.AppendLine($"- **Snapshot ID**: {_snapshotId}");
                if (!string.IsNullOrWhiteSpace(_branch)) sb.AppendLine($"- **Branch**: {_branch}");
                if (!string.IsNullOrWhiteSpace(_commitHash)) sb.AppendLine($"- **Commit Hash**: {_commitHash}");
                if (!string.IsNullOrWhiteSpace(_environment)) sb.AppendLine($"- **Environment**: {_environment}");
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();

            if (_includeMetadata)
            {
                sb.AppendLine("## TLDL Metadata");
                var tagsLine = ScribeUtils.FormatTags(_tagsCsv);
                if (!string.IsNullOrWhiteSpace(tagsLine)) sb.AppendLine($"\n**Tags**: {tagsLine}");
                sb.AppendLine($"**Complexity**: {_complexity}");
                sb.AppendLine($"**Impact**: {_impact}");
                if (!string.IsNullOrWhiteSpace(_teamMembers)) sb.AppendLine($"**Team Members**: {_teamMembers}");
                if (!string.IsNullOrWhiteSpace(_duration)) sb.AppendLine($"**Duration**: {_duration}");
                sb.AppendLine($"**Created**: {createdTs}");
                sb.AppendLine($"**Last Updated**: {createdTs}");
                sb.AppendLine($"**Status**: {FormatStatus(_status)}");
            }

            return sb.ToString();
        }

        void DrawHelp(string title, string body)
        {
            EditorGUILayout.HelpBox($"{title}: {body}", MessageType.None);
        }

        void DrawPlaceholder(string label, string hint)
        {
            GUILayout.Label(label, EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox(hint, MessageType.Info);
        }

        void LabelSmall(string text)
        {
            GUILayout.Label(text, EditorStyles.miniLabel);
        }

        static T Clone<T>(T src) where T : new()
        {
            var t = new T();
            foreach (var f in typeof(T).GetFields())
            {
                f.SetValue(t, f.GetValue(src));
            }
            return t;
        }

        static string FormatStatus(Status s)
        {
            return s switch
            {
                Status.Draft => "Draft",
                Status.InProgress => "In Progress",
                Status.Complete => "Complete",
                Status.Archived => "Archived",
                _ => s.ToString(),
            };
        }

        void DrawPreview()
        {
            // Always render from raw if available, otherwise from form
            var md = !string.IsNullOrEmpty(_rawContent) ? _rawContent : BuildMarkdown(GetCreatedTs());

            // Remove mode switch; preview is always rendered view
            var viewportHeight = Mathf.Max(120f, position.height - 220f);
            _previewScroll = EditorGUILayout.BeginScrollView(_previewScroll, GUILayout.Height(viewportHeight), GUILayout.ExpandHeight(true));
            GUILayout.Label("Markdown Preview (rendered)", EditorStyles.boldLabel);
            RenderMarkdown(md);
            EditorGUILayout.EndScrollView();
        }

        void RenderMarkdown(string md)
        {
            if (string.IsNullOrEmpty(md)) return;
            var lines = md.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var inCode = false;
            var codeBuffer = new StringBuilder();
            foreach (var raw in lines)
            {
                var line = raw;
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
                var imgMatch = Regex.Match(line, @"^\s*!\[([^\]]*)\]\(([^)]+)\)\s*$");
                if (imgMatch.Success)
                {
                    var alt = imgMatch.Groups[1].Value;
                    var path = imgMatch.Groups[2].Value;
                    RenderImage(path, alt);
                    continue;
                }

                // Checkbox list items: - [ ] and - [x]
                if (Regex.IsMatch(line, @"^\s*[-*]\s\[( |x|X)\]\s"))
                {
                    var isChecked = line.IndexOf("[x]", StringComparison.OrdinalIgnoreCase) >= 0;
                    var text = Regex.Replace(line, @"^\s*[-*]\s\[( |x|X)\]\s", "");
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ToggleLeft(ApplyInlineFormatting(text), isChecked);
                    EditorGUI.EndDisabledGroup();
                    continue;
                }

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
                else if (line.StartsWith("- ") || line.StartsWith("* "))
                {
                    EditorGUILayout.LabelField("• " + ApplyInlineFormatting(line[2..]), _listItem);
                }
                else if (string.IsNullOrWhiteSpace(line))
                {
                    GUILayout.Space(4);
                }
                else
                {
                    EditorGUILayout.LabelField(ApplyInlineFormatting(line), _bodyWrap);
                }
            }
        }

        void RenderImage(string refPath, string alt)
        {
            var resolved = ResolveImageAbsolutePath(refPath);
            if (string.IsNullOrEmpty(resolved))
            {
                EditorGUILayout.HelpBox($"Image not found: {refPath}", MessageType.None);
                return;
            }
            if (!_imageCache.TryGetValue(resolved, out var tex) || tex == null)
            {
                var bytes = File.Exists(resolved) ? File.ReadAllBytes(resolved) : null;
                if (bytes != null)
                {
                    tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!tex.LoadImage(bytes)) tex = null;
                    else _imageCache[resolved] = tex;
                }
            }
            if (tex == null)
            {
                EditorGUILayout.HelpBox($"(missing image) {alt} - {refPath}", MessageType.Warning);
                return;
            }
            var maxWidth = Mathf.Max(100f, position.width - 360f);
            var aspect = (float)tex.width / Mathf.Max(1, tex.height);
            var width = Mathf.Min(maxWidth, tex.width);
            var height = width / Mathf.Max(0.01f, aspect);
            GUILayout.Label(new GUIContent(tex, alt), GUILayout.Width(width), GUILayout.Height(height));
            if (!string.IsNullOrEmpty(alt)) EditorGUILayout.LabelField(alt, EditorStyles.miniLabel);
        }

        private string GetProjectRoot()
        {
            return Directory.GetParent(Application.dataPath)!.FullName;
        }

        string ResolveImageAbsolutePath(string refPath)
        {
            if (string.IsNullOrEmpty(refPath)) return null;
            var norm = refPath.Replace('\\', '/');
            if (Path.IsPathRooted(refPath)) return refPath;

            // If current file exists, resolve relative to its directory
            var baseDir = string.IsNullOrEmpty(_currentFilePath) ? ResolveActiveFolder() : Path.GetDirectoryName(_currentFilePath);
            if (string.IsNullOrEmpty(baseDir)) baseDir = ResolveActiveFolder();
            var abs = Path.GetFullPath(Path.Combine(baseDir, norm));
            if (File.Exists(abs)) return abs;

            // Try images subfolder under active folder
            var imgDir = EnsureImagesDirectory();
            var tryAbs = Path.Combine(imgDir ?? baseDir, Path.GetFileName(norm));
            if (File.Exists(tryAbs)) return tryAbs;
            return abs;
        }

        string EnsureImagesDirectory()
        {
            var baseDir = string.IsNullOrEmpty(_currentFilePath) ? ResolveActiveFolder() : Path.GetDirectoryName(_currentFilePath);
            if (string.IsNullOrEmpty(baseDir)) baseDir = ResolveActiveFolder();
            var imagesDir = Path.Combine(baseDir, "images");
            if (!Directory.Exists(imagesDir)) Directory.CreateDirectory(imagesDir);
            return imagesDir;
        }

        void AddImageFile()
        {
            var startDir = EnsureImagesDirectory();
            var picked = EditorUtility.OpenFilePanelWithFilters("Add Image", startDir, new[] { "Images", "png,jpg,jpeg,gif", "All", "*.*" });
            if (string.IsNullOrEmpty(picked)) return;
            try
            {
                var imagesDir = EnsureImagesDirectory();
                var fileName = Path.GetFileName(picked);
                var dest = Path.Combine(imagesDir, fileName);
                var uniqueDest = dest;
                int i = 1;
                while (File.Exists(uniqueDest))
                {
                    uniqueDest = Path.Combine(imagesDir, $"{Path.GetFileNameWithoutExtension(fileName)}_{i}{Path.GetExtension(fileName)}");
                    i++;
                }
                File.Copy(picked, uniqueDest, overwrite: false);
                PostWriteImport(uniqueDest);

                // Add relative path entry
                var rel = Path.GetFileName(uniqueDest);
                var mdRef = $"images/{rel}".Replace('\\', '/');
                if (!_imagePaths.Contains(mdRef)) _imagePaths.Add(mdRef);
                _statusLine = $"Image added: {mdRef}";
            }
            catch (Exception ex)
            {
                _statusLine = $"Failed to add image: {ex.Message}";
            }
        }

        void AddImageAndInsertAtCursor()
        {
            AddImageFile();
            if (_imagePaths.Count == 0) return;
            string last = _imagePaths[^1];
            InsertImageMarkdownAtCursor(last);
        }

        void InsertImageMarkdownAtCursor(string relPath)
        {
            // KeeperNote: Cursor-aware insertion preserves writing flow; fallback appends to end for resilience if focus state is lost.
            var insert = $"![image]({relPath})";
            if (!string.IsNullOrEmpty(relPath) && !_imagePaths.Contains(relPath))
            {
                _imagePaths.Add(relPath);
            }

            if (_tab == 1 && GUI.GetNameOfFocusedControl() == _rawEditorControlName && _rawCursorIndex >= 0 && _rawCursorIndex <= (_rawContent?.Length ?? 0))
            {
                _rawContent ??= string.Empty;
                bool needLeadingNewline = _rawCursorIndex > 0 && _rawContent[_rawCursorIndex - 1] != '\n';
                bool needTrailingNewline = _rawCursorIndex < _rawContent.Length && _rawContent[_rawCursorIndex] != '\n';
                var prefix = _rawContent[.._rawCursorIndex];
                var suffix = _rawContent[_rawCursorIndex..];
                var builder = new StringBuilder(prefix.Length + insert.Length + suffix.Length + 4);
                builder.Append(prefix);
                if (needLeadingNewline) builder.Append('\n');
                builder.Append(insert);
                if (needTrailingNewline) builder.Append('\n');
                builder.Append(suffix);
                _rawContent = builder.ToString();
                _rawCursorIndex = prefix.Length + (needLeadingNewline ? 1 : 0) + insert.Length + (needTrailingNewline ? 1 : 0);
                _statusLine = $"Inserted image at cursor: {relPath}";
            }
            else
            {
                var md = _rawContent ?? string.Empty;
                if (!md.EndsWith("\n") && md.Length > 0) md += "\n";
                _rawCursorIndex = md.Length; // set cursor to end prior to append
                _rawContent = md + insert + "\n";
                _statusLine = $"Inserted image at end: {relPath}";
            }
            _pendingScrollToCursor = true;
            _tab = 1;
            // Focus raw editor next repaint so we can capture cursor + apply scroll
            EditorApplication.delayCall += () => GUI.FocusControl(_rawEditorControlName);
            Repaint();
        }

        void CopyImageMarkdownLink(string absPath, string relPath)
        {
            if (string.IsNullOrEmpty(relPath)) return;
            var alt = Path.GetFileNameWithoutExtension(absPath);
            var md = $"![{alt}]({relPath})";
            EditorGUIUtility.systemCopyBuffer = md;
            _statusLine = $"Link copied: {relPath}";
            ShowNotification(new GUIContent("Link Copied"));
        }

        // Restored helpers and sections that other features depend on
        void ChooseRootFolder()
        {
            var start = string.IsNullOrEmpty(_rootPath) ? Application.dataPath : _rootPath;
            var picked = EditorUtility.OpenFolderPanel("Choose Sudo-GitBook Root", start, "");
            if (!string.IsNullOrEmpty(picked))
            {
                _rootPath = picked;
                _activeDirPath = _rootPath;
                EditorPrefs.SetString(EditorPrefsRootKey, _rootPath);
                _statusLine = $"Root set to: {_rootPath}";
                RefreshNavigator();
            }
        }

        void OpenInFileBrowser(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            EditorUtility.RevealInFinder(path);
        }

        void RefreshNavigator()
        {
            Repaint();
        }

        void CreateChildFolder()
        {
            var parent = _activeDirPath;
            if (string.IsNullOrEmpty(parent)) parent = ResolveActiveFolder();
            var name = EditorUtility.SaveFilePanel("New Folder Name", parent, "NewFolder", "");
            if (!string.IsNullOrEmpty(name))
            {
                try
                {
                    // SaveFilePanel returns a file path; we want folder path. Create directory if not exists.
                    var dir = name;
                    if (Path.HasExtension(dir)) dir = Path.GetDirectoryName(dir);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    _activeDirPath = dir;
                    _statusLine = $"Folder ready: {dir}";
                    RefreshNavigator();
                }
                catch (Exception ex)
                {
                    _statusLine = $"Failed to create folder: {ex.Message}";
                }
            }
        }

        string ResolveActiveFolder()
        {
            if (!string.IsNullOrEmpty(_activeDirPath)) return _activeDirPath;
            if (!string.IsNullOrEmpty(_rootPath)) return _rootPath;
            var fallback = Path.Combine(Application.dataPath, "Plugins/TLDA/docs");
            return fallback;
        }

        void LoadFileDialog()
        {
            var dir = ResolveActiveFolder();
            var picked = EditorUtility.OpenFilePanelWithFilters(
                "Open Document",
                dir,
                new[] { "All", "*.*", "Markdown", "md,markdown", "Text", "txt,log", "XML", "xml" }
            );
            if (!string.IsNullOrEmpty(picked))
            {
                LoadFile(picked);
            }
        }

        void LoadFile(string absPath)
        {
            try
            {
                var text = File.ReadAllText(absPath);
                _currentFilePath = absPath;
                _rawContent = text;
                _rawGeneratedSnapshot = text;
                _rawDirty = false;
                ParseBasicMetadata(text); // hydrate basic header fields
                _statusLine = $"Loaded: {MakeProjectRelative(absPath)}";
                _tab = 1; // Editor
            }
            catch (Exception ex)
            {
                _statusLine = $"Failed to load file: {ex.Message}";
            }
        }

        void SaveRaw()
        {
            if (!string.IsNullOrEmpty(_currentFilePath))
            {
                try
                {
                    File.WriteAllText(_currentFilePath, _rawContent ?? string.Empty, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                    PostWriteImport(_currentFilePath);
                    _statusLine = $"Saved: {MakeProjectRelative(_currentFilePath)}";
                    RefreshNavigator();
                }
                catch (Exception ex)
                {
                    _statusLine = $"Failed to save: {ex.Message}";
                }
            }
            else
            {
                SaveRawAs();
            }
        }

        void SaveRawAs()
        {
            var dir = ResolveActiveFolder();
            var suggested = $"TLDL-{DateTime.UtcNow:yyyy-MM-dd}-Entry.md";
            var picked = EditorUtility.SaveFilePanel("Save Document As", dir, suggested, "md");
            if (!string.IsNullOrEmpty(picked))
            {
                try
                {
                    File.WriteAllText(picked, _rawContent ?? string.Empty, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                    _currentFilePath = picked;
                    PostWriteImport(_currentFilePath);
                    _statusLine = $"Saved: {MakeProjectRelative(_currentFilePath)}";
                    RefreshNavigator();
                }
                catch (Exception ex)
                {
                    _statusLine = $"Failed to save: {ex.Message}";
                }
            }
        }

        void EnsureTemplatesLoaded()
        {
            if (_templates != null) return;
            try
            {
                _templates = new List<TemplateInfo>();
                var root = GetProjectRoot();
                var registry = Path.Combine(root, "templates", "comments", "registry.yaml");
                if (!File.Exists(registry)) return;

                var lines = File.ReadAllLines(registry);
                bool inTemplates = false;
                string currentKey = null, currentFile = null, currentTitle = null;
                foreach (var raw in lines)
                {
                    var line = raw.TrimEnd();
                    if (line.Trim() == "templates:") { inTemplates = true; continue; }
                    if (!inTemplates) continue;

                    var keyMatch = Regex.Match(line, @"^\s{2}([A-Za-z0-9_-]+):\s*$");
                    if (keyMatch.Success)
                    {
                        if (!string.IsNullOrEmpty(currentKey) && !string.IsNullOrEmpty(currentFile))
                        {
                            _templates.Add(new TemplateInfo { Key = currentKey, Title = currentTitle, File = currentFile, AbsPath = Path.Combine(root, "templates", "comments", currentFile) });
                        }
                        currentKey = keyMatch.Groups[1].Value;
                        currentFile = null; currentTitle = null;
                        continue;
                    }

                    var fileMatch = Regex.Match(line, @"^\s{4}file:\s*(.+)$");
                    if (fileMatch.Success)
                    {
                        currentFile = fileMatch.Groups[1].Value.Trim();
                        continue;
                    }

                    var titleMatch = Regex.Match(line, @"^\s{4}title:\s*""?(.*?)""?$");
                    if (titleMatch.Success)
                    {
                        currentTitle = titleMatch.Groups[1].Value.Trim();
                        continue;
                    }
                }
                if (!string.IsNullOrEmpty(currentKey) && !string.IsNullOrEmpty(currentFile))
                {
                    _templates.Add(new TemplateInfo { Key = currentKey, Title = currentTitle, File = currentFile, AbsPath = Path.Combine(root, "templates", "comments", currentFile) });
                }
            }
            catch (Exception ex)
            {
                _statusLine = $"Failed to load templates: {ex.Message}";
            }
        }

        void CreateIssueFromTemplate(TemplateInfo info)
        {
            // KeeperNote: Issue creation uses a template registry so extending new archetypes is declarative (edit YAML, no code change).
            try
            {
                var issuesDir = GetIssuesDirectory();
                if (!Directory.Exists(issuesDir)) Directory.CreateDirectory(issuesDir);
                EnsureIssuesReadme(issuesDir);

                var safeTitle = string.IsNullOrWhiteSpace(_title) ? "Issue" : ScribeUtils.SanitizeTitle(_title);
                var fileName = $"Issue-{DateTime.UtcNow:yyyy-MM-dd}-{safeTitle}.md";
                var absPath = Path.Combine(issuesDir, fileName);

                var header = new StringBuilder();
                header.AppendLine($"# Issue: {(_title ?? "Untitled")} ");
                header.AppendLine($"**Created:** {GetCreatedTs()}");
                if (!string.IsNullOrWhiteSpace(_context)) header.AppendLine($"**Context:** {_context}");
                if (!string.IsNullOrWhiteSpace(_summary)) header.AppendLine($"**Summary:** {_summary}");
                header.AppendLine();

                var body = LoadTemplateMarkdown(info) ?? string.Empty;
                File.WriteAllText(absPath, header + body, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                _currentFilePath = absPath;
                _rawContent = header + body;
                _rawGeneratedSnapshot = _rawContent;
                _rawDirty = false;
                _tab = 2; // preview
                _statusLine = $"Issue created: {MakeProjectRelative(absPath)}";
                RefreshNavigator();
                PostWriteImport(absPath);
            }
            catch (Exception ex)
            {
                _statusLine = $"Failed to create issue: {ex.Message}";
            }
        }

        void TryCreate()
        {
            // KeeperNote: Core TLDL artifact writer — uses sanitized title for filename safety and reproducible IDs.
            try
            {
                var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
                var safeTitle = ScribeUtils.SanitizeTitle(_title);
                if (string.IsNullOrEmpty(safeTitle)) safeTitle = "Entry";
                var fileName = $"TLDL-{date}-{safeTitle}.md";
                var targetFolder = ResolveActiveFolder();
                if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);
                var absPath = Path.Combine(targetFolder, fileName);
                var md = BuildMarkdown(GetCreatedTs(), date, safeTitle);
                File.WriteAllText(absPath, md, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                PostWriteImport(absPath);
                _currentFilePath = absPath;
                _rawContent = md;
                _rawGeneratedSnapshot = md;
                _rawDirty = false;
                _statusLine = $"Saved: {MakeProjectRelative(absPath)}";
                RefreshNavigator();
            }
            catch (Exception ex)
            {
                _statusLine = $"Error creating file: {ex.Message}";
                Debug.LogError($"[The Scribe] Create failed: {ex}");
            }
        }

        void DrawDevTimeTravel()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("DevTimeTravel Context", EditorStyles.boldLabel);
            _snapshotId = EditorGUILayout.TextField("Snapshot ID", _snapshotId);
            _branch = EditorGUILayout.TextField("Branch", _branch);
            _commitHash = EditorGUILayout.TextField("Commit Hash", _commitHash);
            _environment = EditorGUILayout.TextField("Environment", _environment);
            EditorGUILayout.EndVertical();
        }

        void DrawMetadata()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("TLDL Metadata", EditorStyles.boldLabel);
            _tagsCsv = EditorGUILayout.TextField("Tags (csv)", _tagsCsv);
            _complexity = (Complexity)EditorGUILayout.EnumPopup("Complexity", _complexity);
            _impact = (Impact)EditorGUILayout.EnumPopup("Impact", _impact);
            _teamMembers = EditorGUILayout.TextField("Team Members", _teamMembers);
            _duration = EditorGUILayout.TextField("Duration", _duration);
            _status = (Status)EditorGUILayout.EnumPopup("Status", _status);
            EditorGUILayout.EndVertical();
        }

        // --- Restored helper methods (previously removed during refactor) ---
        static string GetRelativePath(string baseDir, string fullPath)
        {
            if (string.IsNullOrEmpty(baseDir) || string.IsNullOrEmpty(fullPath)) return fullPath ?? string.Empty;
            try
            {
                var baseUri = new Uri(AppendDirectorySeparatorChar(baseDir));
                var pathUri = new Uri(fullPath);
                var rel = Uri.UnescapeDataString(baseUri.MakeRelativeUri(pathUri).ToString());
                return rel.Replace('/', Path.DirectorySeparatorChar);
            }
            catch { return fullPath; }
        }
        static string AppendDirectorySeparatorChar(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            char last = path[^1];
            if (last == Path.DirectorySeparatorChar || last == Path.AltDirectorySeparatorChar) return path;
            return path + Path.DirectorySeparatorChar;
        }
        string BuildFormSnapshot()
        {
            var sb = new StringBuilder();
            sb.Append(_title).Append('|').Append(_author).Append('|').Append(_context).Append('|').Append(_summary).Append('|').Append(_tagsCsv);
            sb.Append('|').Append(_includeDiscoveries).Append(_includeActions).Append(_includeTechnicalDetails).Append(_includeDependencies)
              .Append(_includeLessons).Append(_includeNextSteps).Append(_includeReferences).Append(_includeDevTimeTravel).Append(_includeMetadata)
              .Append(_includeTerminalProof).Append(_includeImages);
            foreach (var d in _discoveries)
            {
                sb.Append("|D:").Append(d.Category).Append('|').Append(d.KeyFinding).Append('|').Append(d.Impact).Append('|').Append(d.Evidence)
                  .Append('|').Append(d.RootCause).Append('|').Append(d.PatternRecognition);
            }
            foreach (var a in _actions)
            {
                sb.Append("|A:").Append(a.Name).Append('|').Append(a.What).Append('|').Append(a.Why).Append('|').Append(a.How)
                  .Append('|').Append(a.Result).Append('|').Append(a.FilesChanged).Append('|').Append(a.Validation);
            }
            sb.Append('|').Append(_codeChanges).Append('|').Append(_configUpdates).Append('|').Append(_terminalProof);
            sb.Append('|').Append(_depsAdded).Append('|').Append(_depsRemoved).Append('|').Append(_depsUpdated);
            sb.Append('|').Append(_lessonsWorked).Append('|').Append(_lessonsImprove).Append('|').Append(_lessonsGaps);
            sb.Append('|').Append(_nextImmediate).Append('|').Append(_nextMedium).Append('|').Append(_nextLong);
            sb.Append('|').Append(_internalLinks).Append('|').Append(_externalResources);
            sb.Append('|').Append(_snapshotId).Append('|').Append(_branch).Append('|').Append(_commitHash).Append('|').Append(_environment);
            sb.Append('|').Append(_complexity).Append('|').Append(_impact).Append('|').Append(_teamMembers).Append('|').Append(_duration).Append('|').Append(_status);
            return sb.ToString();
        }
        static string LabeledLines(string label, string value)
        {
            GUILayout.Label(label, EditorStyles.miniBoldLabel);
            return EditorGUILayout.TextArea(value, new GUIStyle(EditorStyles.textArea) { wordWrap = true }, GUILayout.MinHeight(56), GUILayout.ExpandWidth(true));
        }
        static string LabeledMultiline(string label, string value)
        {
            GUILayout.Label(label, EditorStyles.miniBoldLabel);
            return EditorGUILayout.TextArea(value, new GUIStyle(EditorStyles.textArea) { wordWrap = true }, GUILayout.MinHeight(80), GUILayout.ExpandWidth(true));
        }
        static string LabeledChecklist(string label, string value)
        {
            GUILayout.Label(label + " (one per line)", EditorStyles.miniBoldLabel);
            return EditorGUILayout.TextArea(value, new GUIStyle(EditorStyles.textArea) { wordWrap = true }, GUILayout.MinHeight(80), GUILayout.ExpandWidth(true));
        }
        string ApplyInlineFormatting(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var s = input;
            s = s.Replace("<", "&lt;").Replace(">", "&gt;");
            s = Regex.Replace(s, "`([^`]+)`", m => $"<color=#c8e1ff><b>{m.Groups[1].Value}</b></color>");
            s = Regex.Replace(s, @"\*\*([^*]+)\*\*", m => $"<b>{m.Groups[1].Value}</b>");
            s = Regex.Replace(s, @"(?<!\*)\*([^*]+)\*(?!\*)", m => $"<i>{m.Groups[1].Value}</i>");
            s = Regex.Replace(s, @"_([^_]+)_", m => $"<i>{m.Groups[1].Value}</i>");
            s = Regex.Replace(s, @"\[([^\]]+)\]\(([^)]+)\)", m => $"<color=#4ea1ff><u>{m.Groups[1].Value}</u></color>");
            return s;
        }
        string LoadTemplateMarkdown(TemplateInfo info)
        {
            // KeeperNote: Reads YAML-embedded template blocks (| literal) and extracts markdown payload without relying on full YAML parser.
            try
            {
                if (info == null || string.IsNullOrEmpty(info.AbsPath) || !File.Exists(info.AbsPath)) return null;
                var yaml = File.ReadAllLines(info.AbsPath);
                var md = new StringBuilder();
                bool inBlock = false;
                foreach (var raw in yaml)
                {
                    if (!inBlock)
                    {
                        if (Regex.IsMatch(raw, @"^\s*template:\s*|"))
                        {
                            inBlock = true; continue;
                        }
                    }
                    else
                    {
                        if (raw.Length > 0 && !char.IsWhiteSpace(raw[0])) break; // out of block
                        var line = raw;
                        if (line.StartsWith("  ")) line = line[2..];
                        md.AppendLine(line);
                    }
                }
                return md.ToString();
            }
            catch (Exception ex)
            {
                _statusLine = $"Failed to read template: {ex.Message}";
                return null;
            }
        }
        string GetIssuesDirectory()
        {
            // KeeperNote: Centralized so future relocation (e.g. configurable root) only requires single change.
            return Path.Combine(GetProjectRoot(), "TLDL", "issues");
        }
        void EnsureIssuesReadme(string issuesDir)
        {
            // KeeperNote: Bootstraps contextual README to explain automation contract for new collaborators.
            try
            {
                var readme = Path.Combine(issuesDir, "Readme.md");
                if (File.Exists(readme)) return;
                var sb = new StringBuilder();
                sb.AppendLine("This directory `Root\\TLDL\\issues` (`Root/` is the current root of the project that contains the Assets and Packages directories) is used for all issues created using the Scribe window.");
                sb.AppendLine("Do not rename arbitrarily — automation / CI flows may rely on this canonical path.");
                File.WriteAllText(readme, sb.ToString(), new UTF8Encoding(false));
                PostWriteImport(readme);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[The Scribe] Unable to create Issues Readme: {ex.Message}");
            }
        }
        // --- End restored helpers ---
    }
}
#endif
