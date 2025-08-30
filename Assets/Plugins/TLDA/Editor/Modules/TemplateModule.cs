#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace LivingDevAgent.Editor.Modules
{
    /// <summary>
    /// 🧙‍♂️ Template Module - The Portal to Infinite Quest Archetypes!
    /// Handles template loading and issue creation from registry-based templates.
    /// Moved to top of toolbar for easier access - templates are your starting spells!
    /// </summary>
    public class TemplateModule : ScribeModuleBase
    {
        public TemplateModule(TLDLScribeData data) : base(data) { }

        public override void Initialize()
        {
            EnsureTemplatesLoaded();
        }

        public void DrawToolbar()
        {
            // 🎭 Template emoji with proper rendering
            GUIContent templateIcon = new GUIContent("🎭 Templates", "Choose a quest archetype to begin your documentation adventure");
            
            if (GUILayout.Button(templateIcon, EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                // Quick template dropdown or open template selector
                ShowTemplateQuickMenu();
            }
            
            if (GUILayout.Button("📋 Create Issue", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                CreateIssueFromSelectedTemplate();
            }
        }

        public void DrawContent()
        {
            EditorGUILayout.BeginVertical("box");
            
            // Header with proper emoji display
            using (new EditorGUILayout.HorizontalScope())
            {
                GUIContent headerContent = new GUIContent("🎭 Issue Creator Templates");
                GUILayout.Label(headerContent, EditorStyles.boldLabel);
                
                if (GUILayout.Button("🔄", EditorStyles.miniButton, GUILayout.Width(30)))
                {
                    RefreshTemplates();
                }
            }

            EnsureTemplatesLoaded();
            if (_data.Templates == null || _data.Templates.Count == 0)
            {
                EditorGUILayout.HelpBox("📚 No templates found. Ensure templates/comments/registry.yaml exists at project root.", MessageType.Warning);
                return;
            }

            string[] items = new string[_data.Templates.Count];
            for (int i = 0; i < _data.Templates.Count; i++)
            {
                items[i] = string.IsNullOrEmpty(_data.Templates[i].Title) ? _data.Templates[i].Key : _data.Templates[i].Title;
            }
            _data.SelectedTemplateIndex = EditorGUILayout.Popup("Template", _data.SelectedTemplateIndex, items);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("📝 Load Template → Editor"))
                {
                    LoadTemplateToEditor();
                }
                if (GUILayout.Button("🎯 Create Issue From Template"))
                {
                    CreateIssueFromSelectedTemplate();
                }
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Issues Directory", GetIssuesDirectory());
            }
            
            EditorGUILayout.EndVertical();
        }

        void ShowTemplateQuickMenu()
        {
            if (_data.Templates == null || _data.Templates.Count == 0)
            {
                SetStatus("No templates available - check templates/comments/registry.yaml");
                return;
            }

            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < _data.Templates.Count; i++)
            {
                int index = i; // Capture for closure
                string displayName = string.IsNullOrEmpty(_data.Templates[i].Title) ? _data.Templates[i].Key : _data.Templates[i].Title;
                menu.AddItem(new GUIContent(displayName), _data.SelectedTemplateIndex == i, () => {
                    _data.SelectedTemplateIndex = index;
                    LoadTemplateToEditor();
                });
            }
            menu.ShowAsContext();
        }

        void LoadTemplateToEditor()
        {
            if (_data.Templates == null || _data.SelectedTemplateIndex >= _data.Templates.Count)
            {
                SetStatus("❌ No template selected");
                return;
            }

            string md = LoadTemplateMarkdown(_data.Templates[_data.SelectedTemplateIndex]);
            _data.RawContent = md ?? string.Empty;
            SetStatus($"📖 Loaded template: {_data.Templates[_data.SelectedTemplateIndex].Key}");
        }

        void CreateIssueFromSelectedTemplate()
        {
            if (_data.Templates == null || _data.SelectedTemplateIndex >= _data.Templates.Count)
            {
                SetStatus("❌ No template selected");
                return;
            }

            CreateIssueFromTemplate(_data.Templates[_data.SelectedTemplateIndex]);
        }

        void RefreshTemplates()
        {
            _data.Templates = null;
            EnsureTemplatesLoaded();
            SetStatus($"🔄 Templates refreshed - found {_data.Templates?.Count ?? 0} templates");
        }

        void EnsureTemplatesLoaded()
        {
            if (_data.Templates != null) return;

            try
            {
                _data.Templates = new System.Collections.Generic.List<TemplateInfo>();
                string root = GetProjectRoot();
                string registry = Path.Combine(root, "templates", "comments", "registry.yaml");
                if (!File.Exists(registry))
                {
                    return;
                }

                string[] lines = File.ReadAllLines(registry);
                bool inTemplates = false;
                string currentKey = null, currentFile = null, currentTitle = null;

                foreach (string raw in lines)
                {
                    string line = raw.TrimEnd();
                    if (line.Trim() == "templates:") { inTemplates = true; continue; }
                    if (!inTemplates) continue;

                    Match keyMatch = Regex.Match(line, @"^\s{2}([A-Za-z0-9_-]+):\s*$");
                    if (keyMatch.Success)
                    {
                        if (!string.IsNullOrEmpty(currentKey) && !string.IsNullOrEmpty(currentFile))
                        {
                            _data.Templates.Add(new TemplateInfo { 
                                Key = currentKey, 
                                Title = currentTitle, 
                                File = currentFile, 
                                AbsPath = Path.Combine(root, "templates", "comments", currentFile) 
                            });
                        }
                        currentKey = keyMatch.Groups[1].Value;
                        currentFile = null; 
                        currentTitle = null;
                        continue;
                    }

                    Match fileMatch = Regex.Match(line, @"^\s{4}file:\s*(.+)$");
                    if (fileMatch.Success)
                    {
                        currentFile = fileMatch.Groups[1].Value.Trim();
                        continue;
                    }

                    Match titleMatch = Regex.Match(line, @"^\s{4}title:\s*""?(.*?)""?$");
                    if (titleMatch.Success)
                    {
                        currentTitle = titleMatch.Groups[1].Value.Trim();
                        continue;
                    }
                }

                if (!string.IsNullOrEmpty(currentKey) && !string.IsNullOrEmpty(currentFile))
                {
                    _data.Templates.Add(new TemplateInfo { 
                        Key = currentKey, 
                        Title = currentTitle, 
                        File = currentFile, 
                        AbsPath = Path.Combine(root, "templates", "comments", currentFile) 
                    });
                }
            }
            catch (Exception ex)
            {
                SetStatus($"❌ Failed to load templates: {ex.Message}");
            }
        }

        void CreateIssueFromTemplate(TemplateInfo info)
        {
            try
            {
                string issuesDir = GetIssuesDirectory();
                if (!Directory.Exists(issuesDir))
                {
                    Directory.CreateDirectory(issuesDir);
                }

                EnsureIssuesReadme(issuesDir);

                string safeTitle = string.IsNullOrWhiteSpace(_data.Title) ? "Issue" : LivingDevAgent.Editor.ScribeUtils.SanitizeTitle(_data.Title);
                string fileName = $"Issue-{DateTime.UtcNow:yyyy-MM-dd}-{safeTitle}.md";
                string absPath = Path.Combine(issuesDir, fileName);

                var header = new StringBuilder();
                header.AppendLine($"# 🎯 Issue: {(_data.Title ?? "Untitled")}");
                header.AppendLine($"**Created:** {GetCreatedTs()}");
                if (!string.IsNullOrWhiteSpace(_data.Context))
                {
                    header.AppendLine($"**Context:** {_data.Context}");
                }

                if (!string.IsNullOrWhiteSpace(_data.Summary))
                {
                    header.AppendLine($"**Summary:** {_data.Summary}");
                }

                header.AppendLine();

                string body = LoadTemplateMarkdown(info) ?? string.Empty;
                File.WriteAllText(absPath, header + body, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                _data.CurrentFilePath = absPath;
                _data.RawContent = header + body;
                _data.RawGeneratedSnapshot = _data.RawContent;
                _data.RawDirty = false;
                
                SetStatus($"🎯 Issue created: {MakeProjectRelative(absPath)}");
                
                PostWriteImport(absPath);
            }
            catch (Exception ex)
            {
                SetStatus($"❌ Failed to create issue: {ex.Message}");
            }
        }

        string LoadTemplateMarkdown(TemplateInfo info)
        {
            try
            {
                if (info == null || string.IsNullOrEmpty(info.AbsPath) || !File.Exists(info.AbsPath))
                {
                    return null;
                }

                string[] yaml = File.ReadAllLines(info.AbsPath);
                var md = new StringBuilder();
                bool inBlock = false;
                
                foreach (string raw in yaml)
                {
                    if (!inBlock)
                    {
                        if (Regex.IsMatch(raw, @"^\s*template:\s*\|"))
                        {
                            inBlock = true; 
                            continue;
                        }
                    }
                    else
                    {
                        if (raw.Length > 0 && !char.IsWhiteSpace(raw[0]))
                        {
                            break; // out of block
                        }

                        string line = raw;
                        if (line.StartsWith("  "))
                        {
                            line = line[2..];
                        }

                        md.AppendLine(line);
                    }
                }
                return md.ToString();
            }
            catch (Exception ex)
            {
                SetStatus($"❌ Failed to read template: {ex.Message}");
                return null;
            }
        }

        string GetProjectRoot()
        {
            return Directory.GetParent(UnityEngine.Application.dataPath)!.FullName;
        }

        string GetIssuesDirectory()
        {
            return Path.Combine(GetProjectRoot(), "TLDL", "issues");
        }

        void EnsureIssuesReadme(string issuesDir)
        {
            try
            {
                string readme = Path.Combine(issuesDir, "Readme.md");
                if (File.Exists(readme)) return;

                var sb = new StringBuilder();
                sb.AppendLine("# 📁 TLDL Issues Directory");
                sb.AppendLine();
                sb.AppendLine("This directory `Root\\TLDL\\issues` is used for all issues created using the Scribe window.");
                sb.AppendLine("Do not rename arbitrarily — automation / CI flows may rely on this canonical path.");
                sb.AppendLine();
                sb.AppendLine("🎯 Issues created here follow template-driven archetypes for consistent documentation.");
                
                File.WriteAllText(readme, sb.ToString(), new UTF8Encoding(false));
                PostWriteImport(readme);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[The Scribe] Unable to create Issues Readme: {ex.Message}");
            }
        }

        string GetCreatedTs()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
        }

        string MakeProjectRelative(string absPath)
        {
            string projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)!.FullName.Replace('\\', '/');
            string norm = absPath.Replace('\\', '/');
            if (norm.StartsWith(projectRoot))
            {
                return norm[(projectRoot.Length + 1)..];
            }
            return absPath;
        }

        void PostWriteImport(string absPath)
        {
            if (string.IsNullOrEmpty(absPath)) return;

            string unityPath = MakeUnityPath(absPath);
            if (!string.IsNullOrEmpty(unityPath))
            {
                AssetDatabase.ImportAsset(unityPath, ImportAssetOptions.ForceSynchronousImport);
            }
        }

        string MakeUnityPath(string absPath)
        {
            string norm = absPath.Replace('\\', '/');
            string dataPath = UnityEngine.Application.dataPath.Replace('\\', '/');
            if (norm.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                return "Assets" + norm[dataPath.Length..];
            }
            return null;
        }
    }
}
#endif
