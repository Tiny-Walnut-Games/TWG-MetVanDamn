using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace LivingDevAgent.Editor.Scribe
    {
    /// <summary>
    /// File I/O operations for The Scribe
    /// KeeperNote: This is the "archive" - handles all document persistence
    /// </summary>
    public class ScribeFileOperations
        {
        private readonly ScribeDataManager _data;
        private string _currentFilePath = "";

        public string CurrentFilePath => _currentFilePath;

        public ScribeFileOperations(ScribeDataManager data)
            {
            _data = data;
            }

        public bool LoadFile(string path)
            {
            if (!File.Exists(path))
                {
                Debug.LogError($"[ScribeFileOps] File not found: {path}");
                return false;
                }

            try
                {
                string content = File.ReadAllText(path, Encoding.UTF8);
                string fileExtension = Path.GetExtension(path).ToLowerInvariant();

                // 🔧 LEGENDARY ENHANCEMENT - Intelligent Sacred Script Processing
                // Current: Universal file loading with format-aware enhancement
                // Enhancement path: Syntax highlighting, validation, format conversion
                content = ProcessSacredScript(content, fileExtension, path);

                _data.RawMarkdown = content;
                _data.RawIsDirty = false;
                _currentFilePath = path;

                // Parse metadata to populate form (only for markdown files)
                if (IsMarkdownFile(fileExtension))
                    {
                    _data.ParseMarkdown(content);
                    }
                else
                    {
                    // For non-markdown files, populate basic metadata
                    _data.Title = Path.GetFileNameWithoutExtension(path);
                    _data.Context = $"Sacred Script: {fileExtension.TrimStart('.')} file";
                    _data.Summary = $"Loaded {fileExtension} file for inspection and documentation";
                    }

                Debug.Log($"[ScribeFileOps] Loaded sacred script: {path} ({fileExtension})");
                return true;
                }
            catch (System.Exception e)
                {
                Debug.LogError($"[ScribeFileOps] Failed to load sacred script: {e.Message}");
                return false;
                }
            }

        /// <summary>
        /// 🔧 LEGENDARY - Sacred Script Processing Engine
        /// Current: Format-aware content enhancement for different file types
        /// Enhancement path: Syntax validation, auto-formatting, structure analysis
        /// Sacred Symbol Preservation: Makes all file types readable and debuggable!
        /// </summary>
        string ProcessSacredScript(string content, string extension, string filePath)
            {
            string fileName = Path.GetFileName(filePath);
            var processedContent = new StringBuilder();

            // Add sacred header with file information
            processedContent.AppendLine($"# 📜 Sacred Script Analysis: {fileName}");
            processedContent.AppendLine($"**File Type**: {GetFileTypeDescription(extension)}");
            processedContent.AppendLine($"**Path**: `{filePath}`");
            processedContent.AppendLine($"**Loaded**: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            processedContent.AppendLine();
            processedContent.AppendLine("---");
            processedContent.AppendLine();

            // Format content based on file type
            switch (extension)
                {
                case ".yaml":
                case ".yml":
                    processedContent.AppendLine("## 🔧 YAML Configuration Analysis");
                    processedContent.AppendLine();
                    processedContent.AppendLine("```yaml");
                    processedContent.AppendLine(content);
                    processedContent.AppendLine("```");
                    break;

                case ".json":
                    processedContent.AppendLine("## 📊 JSON Data Structure");
                    processedContent.AppendLine();
                    processedContent.AppendLine("```json");
                    processedContent.AppendLine(content);
                    processedContent.AppendLine("```");
                    break;

                case ".cs":
                    processedContent.AppendLine("## 🧙‍♂️ C# Sacred Code");
                    processedContent.AppendLine();
                    processedContent.AppendLine("```csharp");
                    processedContent.AppendLine(content);
                    processedContent.AppendLine("```");
                    break;

                case ".js":
                case ".ts":
                    processedContent.AppendLine("## ⚡ JavaScript/TypeScript Magic");
                    processedContent.AppendLine();
                    processedContent.AppendLine($"```{(extension == ".ts" ? "typescript" : "javascript")}");
                    processedContent.AppendLine(content);
                    processedContent.AppendLine("```");
                    break;

                case ".py":
                    processedContent.AppendLine("## 🐍 Python Serpent Script");
                    processedContent.AppendLine();
                    processedContent.AppendLine("```python");
                    processedContent.AppendLine(content);
                    processedContent.AppendLine("```");
                    break;

                case ".sh":
                case ".bat":
                case ".cmd":
                    processedContent.AppendLine("## 🔨 Shell/Batch Incantations");
                    processedContent.AppendLine();
                    processedContent.AppendLine("```bash");
                    processedContent.AppendLine(content);
                    processedContent.AppendLine("```");
                    break;

                case ".xml":
                case ".html":
                    processedContent.AppendLine("## 🏗️ Markup Architecture");
                    processedContent.AppendLine();
                    processedContent.AppendLine("```xml");
                    processedContent.AppendLine(content);
                    processedContent.AppendLine("```");
                    break;

                case ".css":
                    processedContent.AppendLine("## 🎨 Style Enchantments");
                    processedContent.AppendLine();
                    processedContent.AppendLine("```css");
                    processedContent.AppendLine(content);
                    processedContent.AppendLine("```");
                    break;

                case ".md":
                case ".markdown":
                    // Return as-is for markdown files
                    return content;

                default:
                    processedContent.AppendLine("## 📄 Raw Sacred Text");
                    processedContent.AppendLine();
                    processedContent.AppendLine("```");
                    processedContent.AppendLine(content);
                    processedContent.AppendLine("```");
                    break;
                }

            // Add analysis footer
            processedContent.AppendLine();
            processedContent.AppendLine("---");
            processedContent.AppendLine();
            processedContent.AppendLine("## 🔍 Analysis Notes");
            processedContent.AppendLine("*Use this space to document discoveries, issues, or insights about this sacred script.*");

            return processedContent.ToString();
            }

        /// <summary>
        /// 🔧 File type classification for sacred script analysis
        /// </summary>
        string GetFileTypeDescription(string extension)
            {
            return extension switch
                {
                    ".yaml" or ".yml" => "YAML Configuration",
                    ".json" => "JSON Data Structure",
                    ".cs" => "C# Source Code",
                    ".js" => "JavaScript",
                    ".ts" => "TypeScript",
                    ".py" => "Python Script",
                    ".sh" => "Shell Script",
                    ".bat" or ".cmd" => "Batch Script",
                    ".xml" => "XML Document",
                    ".html" => "HTML Document",
                    ".css" => "CSS Stylesheet",
                    ".md" or ".markdown" => "Markdown Document",
                    ".txt" => "Plain Text",
                    ".log" => "Log File",
                    ".ini" or ".cfg" or ".conf" => "Configuration File",
                    ".gitignore" => "Git Ignore Rules",
                    "dockerfile" => "Docker Configuration",
                    _ => "Unknown Sacred Script"
                    };
            }

        /// <summary>
        /// 🔧 Determines if file should use markdown parsing
        /// </summary>
        bool IsMarkdownFile(string extension)
            {
            return extension == ".md" || extension == ".markdown";
            }

        public bool SaveFile(string path = null)
            {
            if (string.IsNullOrEmpty(path))
                path = _currentFilePath;

            if (string.IsNullOrEmpty(path))
                {
                Debug.LogError("[ScribeFileOps] No file path specified");
                return false;
                }

            try
                {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                    {
                    Directory.CreateDirectory(directory);
                    }

                // Write file without BOM
                File.WriteAllText(path, _data.RawMarkdown ?? "", new UTF8Encoding(false));

                _currentFilePath = path;
                _data.RawIsDirty = false;

                // Import if in Assets
                ImportIfInAssets(path);

                Debug.Log($"[ScribeFileOps] Saved: {path}");
                return true;
                }
            catch (System.Exception e)
                {
                Debug.LogError($"[ScribeFileOps] Failed to save file: {e.Message}");
                return false;
                }
            }

        public string SaveAsDialog()
            {
            string directory = string.IsNullOrEmpty(_currentFilePath)
                ? GetDefaultSaveDirectory()
                : Path.GetDirectoryName(_currentFilePath);

            string fileName = GenerateFileName();

            string path = EditorUtility.SaveFilePanel(
                "Save TLDL Document",
                directory,
                fileName,
                "md"
            );

            if (!string.IsNullOrEmpty(path) && SaveFile(path))
                {
                return path;
                }

            return null;
            }

        public string OpenFileDialog()
            {
            string directory = string.IsNullOrEmpty(_currentFilePath)
                ? GetDefaultSaveDirectory()
                : Path.GetDirectoryName(_currentFilePath);

            // 🔧 LEGENDARY ENHANCEMENT - Universal Sacred Script Support
            // The Scribe can now read ALL forms of documentation and configuration!
            string extensions = "md,markdown,txt,yaml,yml,json,cs,js,ts,py,sh,bat,cmd,xml,html,css,ini,cfg,conf,log,gitignore,dockerfile";

            string path = EditorUtility.OpenFilePanel(
                "Open Sacred Script or Document",
                directory,
                extensions
            );

            if (!string.IsNullOrEmpty(path) && LoadFile(path))
                {
                return path;
                }

            return null;
            }

        public void CreateNewDocument()
            {
            _data.RawMarkdown = "";
            _data.RawIsDirty = false;
            _currentFilePath = "";

            // Reset form data
            _data.Title = "";
            _data.Author = "@copilot";
            _data.Context = "";
            _data.Summary = "";
            _data.TagsCsv = "";

            _data.Discoveries.Clear();
            _data.Discoveries.Add(new Discovery { Category = "Discovery 1" });

            _data.Actions.Clear();
            _data.Actions.Add(new ActionItem { Name = "Action 1" });
            }

        string GenerateFileName()
            {
            string date = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
            string safeTitle = SanitizeTitle(_data.Title);

            if (string.IsNullOrEmpty(safeTitle))
                safeTitle = "Entry";

            return $"TLDL-{date}-{safeTitle}.md";
            }

        string SanitizeTitle(string title)
            {
            if (string.IsNullOrWhiteSpace(title))
                return "";

            char[] invalid = Path.GetInvalidFileNameChars();
            string result = title;

            foreach (char c in invalid)
                {
                result = result.Replace(c.ToString(), "");
                }

            return result.Replace(" ", "-");
            }

        string GetDefaultSaveDirectory()
            {
            string rootPath = EditorPrefs.GetString("LDA_SCRIBE_ROOT", "");

            if (string.IsNullOrEmpty(rootPath))
                {
                rootPath = Path.Combine(Application.dataPath, "Plugins/TLDA/docs");
                }

            if (!Directory.Exists(rootPath))
                {
                Directory.CreateDirectory(rootPath);
                }

            return rootPath;
            }

        void ImportIfInAssets(string path)
            {
            string dataPath = Application.dataPath.Replace('\\', '/');
            string normalizedPath = path.Replace('\\', '/');

            if (normalizedPath.StartsWith(dataPath))
                {
                string assetPath = "Assets" + normalizedPath[dataPath.Length..];
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                }
            }
        }
    }
