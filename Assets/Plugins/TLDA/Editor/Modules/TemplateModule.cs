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
		public TemplateModule (TLDLScribeData data) : base(data) { }

		public override void Initialize ()
			{
			this.EnsureTemplatesLoaded();
			}

		public void DrawToolbar ()
			{
			// 🎭 Template emoji with proper rendering
			var templateIcon = new GUIContent("🎭 Templates", "Choose a quest archetype to begin your documentation adventure");

			if (GUILayout.Button(templateIcon, EditorStyles.toolbarButton, GUILayout.Width(100)))
				{
				// Quick template dropdown or open template selector
				this.ShowTemplateQuickMenu();
				}

			if (GUILayout.Button("📋 Create Issue", EditorStyles.toolbarButton, GUILayout.Width(100)))
				{
				this.CreateIssueFromSelectedTemplate();
				}
			}

		public void DrawContent ()
			{
			EditorGUILayout.BeginVertical("box");

			// Header with proper emoji display
			using (new EditorGUILayout.HorizontalScope())
				{
				var headerContent = new GUIContent("🎭 Issue Creator Templates");
				GUILayout.Label(headerContent, EditorStyles.boldLabel);

				if (GUILayout.Button("🔄", EditorStyles.miniButton, GUILayout.Width(30)))
					{
					this.RefreshTemplates();
					}
				}

			this.EnsureTemplatesLoaded();
			if (this._data.Templates == null || this._data.Templates.Count == 0)
				{
				EditorGUILayout.HelpBox("📚 No templates found. Ensure templates/comments/registry.yaml exists at project root.", MessageType.Warning);
				return;
				}

			string [ ] items = new string [ this._data.Templates.Count ];
			for (int i = 0; i < this._data.Templates.Count; i++)
				{
				items [ i ] = string.IsNullOrEmpty(this._data.Templates [ i ].Title) ? this._data.Templates [ i ].Key : this._data.Templates [ i ].Title;
				}
			this._data.SelectedTemplateIndex = EditorGUILayout.Popup("Template", this._data.SelectedTemplateIndex, items);

			using (new EditorGUILayout.HorizontalScope())
				{
				if (GUILayout.Button("📝 Load Template → Editor"))
					{
					this.LoadTemplateToEditor();
					}
				if (GUILayout.Button("🎯 Create Issue From Template"))
					{
					this.CreateIssueFromSelectedTemplate();
					}
				}

			using (new EditorGUI.DisabledScope(true))
				{
				EditorGUILayout.TextField("Issues Directory", this.GetIssuesDirectory());
				}

			EditorGUILayout.EndVertical();
			}

		private void ShowTemplateQuickMenu ()
			{
			if (this._data.Templates == null || this._data.Templates.Count == 0)
				{
				this.SetStatus("No templates available - check templates/comments/registry.yaml");
				return;
				}

			var menu = new GenericMenu();
			for (int i = 0; i < this._data.Templates.Count; i++)
				{
				int index = i; // Capture for closure
				string displayName = string.IsNullOrEmpty(this._data.Templates [ i ].Title) ? this._data.Templates [ i ].Key : this._data.Templates [ i ].Title;
				menu.AddItem(new GUIContent(displayName), this._data.SelectedTemplateIndex == i, () =>
				{
					this._data.SelectedTemplateIndex = index;
					this.LoadTemplateToEditor();
				});
				}
			menu.ShowAsContext();
			}

		private void LoadTemplateToEditor ()
			{
			if (this._data.Templates == null || this._data.SelectedTemplateIndex >= this._data.Templates.Count)
				{
				this.SetStatus("❌ No template selected");
				return;
				}

			string md = this.LoadTemplateMarkdown(this._data.Templates [ this._data.SelectedTemplateIndex ]);
			this._data.RawContent = md ?? string.Empty;
			this.SetStatus($"📖 Loaded template: {this._data.Templates [ this._data.SelectedTemplateIndex ].Key}");
			}

		private void CreateIssueFromSelectedTemplate ()
			{
			if (this._data.Templates == null || this._data.SelectedTemplateIndex >= this._data.Templates.Count)
				{
				this.SetStatus("❌ No template selected");
				return;
				}

			this.CreateIssueFromTemplate(this._data.Templates [ this._data.SelectedTemplateIndex ]);
			}

		private void RefreshTemplates ()
			{
			this._data.Templates = null;
			this.EnsureTemplatesLoaded();
			this.SetStatus($"🔄 Templates refreshed - found {this._data.Templates?.Count ?? 0} templates");
			}

		private void EnsureTemplatesLoaded ()
			{
			if (this._data.Templates != null) return;

			try
				{
				this._data.Templates = new System.Collections.Generic.List<TemplateInfo>();
				string root = this.GetProjectRoot();
				string registry = Path.Combine(root, "templates", "comments", "registry.yaml");
				if (!File.Exists(registry))
					{
					return;
					}

				string [ ] lines = File.ReadAllLines(registry);
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
							this._data.Templates.Add(new TemplateInfo
								{
								Key = currentKey,
								Title = currentTitle,
								File = currentFile,
								AbsPath = Path.Combine(root, "templates", "comments", currentFile)
								});
							}
						currentKey = keyMatch.Groups [ 1 ].Value;
						currentFile = null;
						currentTitle = null;
						continue;
						}

					Match fileMatch = Regex.Match(line, @"^\s{4}file:\s*(.+)$");
					if (fileMatch.Success)
						{
						currentFile = fileMatch.Groups [ 1 ].Value.Trim();
						continue;
						}

					Match titleMatch = Regex.Match(line, @"^\s{4}title:\s*""?(.*?)""?$");
					if (titleMatch.Success)
						{
						currentTitle = titleMatch.Groups [ 1 ].Value.Trim();
						continue;
						}
					}

				if (!string.IsNullOrEmpty(currentKey) && !string.IsNullOrEmpty(currentFile))
					{
					this._data.Templates.Add(new TemplateInfo
						{
						Key = currentKey,
						Title = currentTitle,
						File = currentFile,
						AbsPath = Path.Combine(root, "templates", "comments", currentFile)
						});
					}
				}
			catch (Exception ex)
				{
				this.SetStatus($"❌ Failed to load templates: {ex.Message}");
				}
			}

		private void CreateIssueFromTemplate (TemplateInfo info)
			{
			try
				{
				string issuesDir = this.GetIssuesDirectory();
				if (!Directory.Exists(issuesDir))
					{
					Directory.CreateDirectory(issuesDir);
					}

				this.EnsureIssuesReadme(issuesDir);

				string safeTitle = string.IsNullOrWhiteSpace(this._data.Title) ? "Issue" : ScribeUtils.SanitizeTitle(this._data.Title);
				string fileName = $"Issue-{DateTime.UtcNow:yyyy-MM-dd}-{safeTitle}.md";
				string absPath = Path.Combine(issuesDir, fileName);

				var header = new StringBuilder();
				header.AppendLine($"# 🎯 Issue: {(this._data.Title ?? "Untitled")}");
				header.AppendLine($"**Created:** {this.GetCreatedTs()}");
				if (!string.IsNullOrWhiteSpace(this._data.Context))
					{
					header.AppendLine($"**Context:** {this._data.Context}");
					}

				if (!string.IsNullOrWhiteSpace(this._data.Summary))
					{
					header.AppendLine($"**Summary:** {this._data.Summary}");
					}

				header.AppendLine();

				string body = this.LoadTemplateMarkdown(info) ?? string.Empty;
				File.WriteAllText(absPath, header + body, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

				this._data.CurrentFilePath = absPath;
				this._data.RawContent = header + body;
				this._data.RawGeneratedSnapshot = this._data.RawContent;
				this._data.RawDirty = false;

				this.SetStatus($"🎯 Issue created: {this.MakeProjectRelative(absPath)}");

				this.PostWriteImport(absPath);
				}
			catch (Exception ex)
				{
				this.SetStatus($"❌ Failed to create issue: {ex.Message}");
				}
			}

		private string LoadTemplateMarkdown (TemplateInfo info)
			{
			try
				{
				if (info == null || string.IsNullOrEmpty(info.AbsPath) || !File.Exists(info.AbsPath))
					{
					return null;
					}

				string [ ] yaml = File.ReadAllLines(info.AbsPath);
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
						if (raw.Length > 0 && !char.IsWhiteSpace(raw [ 0 ]))
							{
							break; // out of block
							}

						string line = raw;
						if (line.StartsWith("  "))
							{
							line = line [ 2.. ];
							}

						md.AppendLine(line);
						}
					}
				return md.ToString();
				}
			catch (Exception ex)
				{
				this.SetStatus($"❌ Failed to read template: {ex.Message}");
				return null;
				}
			}

		private string GetProjectRoot ()
			{
			return Directory.GetParent(Application.dataPath)!.FullName;
			}

		private string GetIssuesDirectory ()
			{
			return Path.Combine(this.GetProjectRoot(), "TLDL", "issues");
			}

		private void EnsureIssuesReadme (string issuesDir)
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
				this.PostWriteImport(readme);
				}
			catch (Exception ex)
				{
				Debug.LogWarning($"[The Scribe] Unable to create Issues Readme: {ex.Message}");
				}
			}

		private string GetCreatedTs ()
			{
			return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
			}

		private string MakeProjectRelative (string absPath)
			{
			string projectRoot = Directory.GetParent(Application.dataPath)!.FullName.Replace('\\', '/');
			string norm = absPath.Replace('\\', '/');
			return norm.StartsWith(projectRoot) ? norm [ (projectRoot.Length + 1).. ] : absPath;
			}

		private void PostWriteImport (string absPath)
			{
			if (string.IsNullOrEmpty(absPath)) return;

			string unityPath = this.MakeUnityPath(absPath);
			if (!string.IsNullOrEmpty(unityPath))
				{
				AssetDatabase.ImportAsset(unityPath, ImportAssetOptions.ForceSynchronousImport);
				}
			}

		private string MakeUnityPath (string absPath)
			{
			string norm = absPath.Replace('\\', '/');
			string dataPath = Application.dataPath.Replace('\\', '/');
			return norm.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase) ? "Assets" + norm [ dataPath.Length.. ] : null;
			}
		}
	}
#endif
