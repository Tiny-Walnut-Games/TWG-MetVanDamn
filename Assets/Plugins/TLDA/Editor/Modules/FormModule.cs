#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace LivingDevAgent.Editor.Modules
	{
	/// <summary>
	/// 📝 Form Module - The Structured Quest Log Builder!
	/// Handles the main form interface where users input TLDL data.
	/// 🎯 ENHANCED: Now includes all detailed section editors based on toggles
	/// </summary>
	public class FormModule : ScribeModuleBase
		{
		public bool ShouldSwitchToEditor { get; private set; } = false;
		public bool ShouldSwitchToPreview { get; private set; } = false;

		public FormModule (TLDLScribeData data) : base(data) { }

		public void ResetSwitchFlags ()
			{
			this.ShouldSwitchToEditor = false;
			this.ShouldSwitchToPreview = false;
			}

		public void DrawToolbar ()
			{
			// 🕐 Time Tracking Controls (Prime Position!)
			if (this._data.IsTimerActive)
				{
				System.TimeSpan elapsed = System.DateTime.Now - this._data.SessionStartTime;
				string timerText = $"⏱️ {elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";

				// Active timer display with clock out
				GUILayout.Label(timerText, EditorStyles.boldLabel, GUILayout.Width(100));

				if (GUILayout.Button("🕐 Clock Out", EditorStyles.toolbarButton, GUILayout.Width(80)))
					{
					this.ClockOut();
					}
				}
			else
				{
				if (GUILayout.Button("🕐 Clock In", EditorStyles.toolbarButton, GUILayout.Width(80)))
					{
					this.ClockIn();
					}

				if (this._data.TotalSessionMinutes > 0)
					{
					GUILayout.Label($"📊 {this._data.TotalSessionMinutes:F0}m", EditorStyles.miniLabel, GUILayout.Width(60));
					}
				}

			GUILayout.Space(10);

			// Form operations
			if (GUILayout.Button("🎯 Generate → Editor", EditorStyles.toolbarButton))
				{
				if (this.WarnOverwriteRawIfDirty())
					{
					string md = this.BuildMarkdown();
					this._data.RawContent = md;
					this._data.RawGeneratedSnapshot = md;
					this._data.RawDirty = false;
					this.ShouldSwitchToEditor = true;
					this.SetStatus("🎯 Generated content from form into Editor");
					}
				}

			if (GUILayout.Button("📄 Create TLDL", EditorStyles.toolbarButton))
				{
				this.TryCreateTLDL();
				}
			}

		public void DrawContent (Rect windowPosition)
			{
			float viewportHeight = Mathf.Max(140f, windowPosition.height - 220f);
			this._data.FormScroll = EditorGUILayout.BeginScrollView(this._data.FormScroll, GUILayout.Height(viewportHeight), GUILayout.ExpandHeight(true));

			// Header section
			this.DrawHeaderSection();

			// Sections toggles and content
			this.DrawSectionsToggles();
			this.DrawSectionEditors(); // 🎯 NEW: Detailed section editors

			// Auto-sync controls
			this.DrawAutoSyncControls();

			EditorGUILayout.EndScrollView();
			}

		private void DrawHeaderSection ()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("📋 Header", EditorStyles.boldLabel);

			this.DrawHelp("Title", "Short, descriptive. Used in the filename.");
			this._data.Title = EditorGUILayout.TextField("Title", this._data.Title);

			this._data.Author = EditorGUILayout.TextField("Author", string.IsNullOrWhiteSpace(this._data.Author) ? "@copilot" : this._data.Author);

			this.DrawPlaceholder("Context", "Issue #XX, Feature Name, or short description.");
			this._data.Context = EditorGUILayout.TextArea(this._data.Context, _textAreaWrap, GUILayout.MinHeight(40), GUILayout.ExpandWidth(true));

			this.DrawPlaceholder("Summary", "One line describing the result.");
			this._data.Summary = EditorGUILayout.TextArea(this._data.Summary, _textAreaWrap, GUILayout.MinHeight(40), GUILayout.ExpandWidth(true));

			this.DrawPlaceholder("Tags (comma-separated)", "e.g., Chronicle Keeper, LDA, Docs");
			this._data.TagsCsv = EditorGUILayout.TextField("Tags", this._data.TagsCsv);

			EditorGUILayout.EndVertical();
			}

		private void DrawSectionsToggles ()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("🗂️ Sections", EditorStyles.boldLabel);

			this._data.IncludeDiscoveries = EditorGUILayout.ToggleLeft("🔍 Include Discoveries", this._data.IncludeDiscoveries);
			this._data.IncludeActions = EditorGUILayout.ToggleLeft("⚡ Include Actions Taken", this._data.IncludeActions);
			this._data.IncludeTechnicalDetails = EditorGUILayout.ToggleLeft("🔧 Include Technical Details", this._data.IncludeTechnicalDetails);
			this._data.IncludeTerminalProof = EditorGUILayout.ToggleLeft("💻 Include Terminal Proof", this._data.IncludeTerminalProof);
			this._data.IncludeDependencies = EditorGUILayout.ToggleLeft("📦 Include Dependencies", this._data.IncludeDependencies);
			this._data.IncludeLessons = EditorGUILayout.ToggleLeft("🎓 Include Lessons Learned", this._data.IncludeLessons);
			this._data.IncludeNextSteps = EditorGUILayout.ToggleLeft("🚀 Include Next Steps", this._data.IncludeNextSteps);
			this._data.IncludeReferences = EditorGUILayout.ToggleLeft("🔗 Include References", this._data.IncludeReferences);
			this._data.IncludeDevTimeTravel = EditorGUILayout.ToggleLeft("⏰ Include DevTimeTravel", this._data.IncludeDevTimeTravel);
			this._data.IncludeMetadata = EditorGUILayout.ToggleLeft("📊 Include Metadata", this._data.IncludeMetadata);
			this._data.IncludeImages = EditorGUILayout.ToggleLeft("🖼️ Include Images", this._data.IncludeImages);

			EditorGUILayout.EndVertical();
			}

		// 🎯 NEW: Complete section editors for all toggles
		private void DrawSectionEditors ()
			{
			if (this._data.IncludeDiscoveries)
				{
				this.DrawDiscoveriesSection();
				}

			if (this._data.IncludeActions)
				{
				this.DrawActionsSection();
				}

			if (this._data.IncludeTechnicalDetails)
				{
				this.DrawTechnicalDetailsSection();
				}

			if (this._data.IncludeTerminalProof)
				{
				this.DrawTerminalProofSection();
				}

			if (this._data.IncludeDependencies)
				{
				this.DrawDependenciesSection();
				}

			if (this._data.IncludeLessons)
				{
				this.DrawLessonsSection();
				}

			if (this._data.IncludeNextSteps)
				{
				this.DrawNextStepsSection();
				}

			if (this._data.IncludeReferences)
				{
				this.DrawReferencesSection();
				}

			if (this._data.IncludeDevTimeTravel)
				{
				this.DrawDevTimeTravelSection();
				}

			if (this._data.IncludeMetadata)
				{
				this.DrawMetadataSection();
				}

			if (this._data.IncludeImages)
				{
				this.DrawImagesSection();
				}
			}

		private void DrawDiscoveriesSection ()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("🔍 Discoveries", EditorStyles.boldLabel);

			this.DrawPlaceholder("Key findings", "What did you discover? New patterns, unexpected behaviors, root causes...");
			this._data.DiscoveriesText = EditorGUILayout.TextArea(this._data.DiscoveriesText ?? "", _textAreaWrap, GUILayout.MinHeight(80), GUILayout.ExpandWidth(true));

			EditorGUILayout.EndVertical();
			}

		private void DrawActionsSection ()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("⚡ Actions Taken", EditorStyles.boldLabel);

			this.DrawPlaceholder("What did you do?", "Step-by-step actions, changes made, commands run...");
			this._data.ActionsTaken = EditorGUILayout.TextArea(this._data.ActionsTaken ?? "", _textAreaWrap, GUILayout.MinHeight(80), GUILayout.ExpandWidth(true));

			EditorGUILayout.EndVertical();
			}

		private void DrawTechnicalDetailsSection ()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("🔧 Technical Details", EditorStyles.boldLabel);

			this.DrawPlaceholder("Architecture insights", "Code patterns, system design, technical decisions...");
			this._data.TechnicalDetails = EditorGUILayout.TextArea(this._data.TechnicalDetails ?? "", _textAreaWrap, GUILayout.MinHeight(80), GUILayout.ExpandWidth(true));

			EditorGUILayout.EndVertical();
			}

		private void DrawTerminalProofSection ()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("💻 Terminal Proof", EditorStyles.boldLabel);

			this.DrawPlaceholder("Command output", "Terminal commands and their output as evidence...");
			this._data.TerminalProof = EditorGUILayout.TextArea(this._data.TerminalProof ?? "", EditorStyles.textArea, GUILayout.MinHeight(100), GUILayout.ExpandWidth(true));

			EditorGUILayout.EndVertical();
			}

		private void DrawDependenciesSection ()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("📦 Dependencies", EditorStyles.boldLabel);

			this.DrawPlaceholder("Dependencies used", "Libraries, packages, tools, systems this work depends on...");
			this._data.Dependencies = EditorGUILayout.TextArea(this._data.Dependencies ?? "", _textAreaWrap, GUILayout.MinHeight(60), GUILayout.ExpandWidth(true));

			EditorGUILayout.EndVertical();
			}

		private void DrawLessonsSection ()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("🎓 Lessons Learned", EditorStyles.boldLabel);

			this.DrawPlaceholder("What did you learn?", "Insights, gotchas, best practices discovered...");
			this._data.LessonsLearned = EditorGUILayout.TextArea(this._data.LessonsLearned ?? "", _textAreaWrap, GUILayout.MinHeight(80), GUILayout.ExpandWidth(true));

			EditorGUILayout.EndVertical();
			}

		private void DrawNextStepsSection ()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("🚀 Next Steps", EditorStyles.boldLabel);

			this.DrawPlaceholder("Future work", "What should be done next? Follow-up tasks, improvements...");
			this._data.NextSteps = EditorGUILayout.TextArea(this._data.NextSteps ?? "", _textAreaWrap, GUILayout.MinHeight(60), GUILayout.ExpandWidth(true));

			EditorGUILayout.EndVertical();
			}

		private void DrawReferencesSection ()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("🔗 References", EditorStyles.boldLabel);

			this.DrawPlaceholder("Links and references", "URLs, documentation, related issues, commit hashes...");
			this._data.References = EditorGUILayout.TextArea(this._data.References ?? "", _textAreaWrap, GUILayout.MinHeight(60), GUILayout.ExpandWidth(true));

			EditorGUILayout.EndVertical();
			}

		private void DrawDevTimeTravelSection ()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("⏰ DevTimeTravel", EditorStyles.boldLabel);

			this.DrawPlaceholder("Context snapshots", "Environment state, configuration snapshots, timeline context...");
			this._data.DevTimeTravel = EditorGUILayout.TextArea(this._data.DevTimeTravel ?? "", _textAreaWrap, GUILayout.MinHeight(60), GUILayout.ExpandWidth(true));

			EditorGUILayout.EndVertical();
			}

		private void DrawImagesSection ()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("🖼️ Images", EditorStyles.boldLabel);

			this.DrawPlaceholder("Image references", "Relative paths to images, screenshots, diagrams...");
			this._data.ImagePaths = EditorGUILayout.TextArea(this._data.ImagePaths ?? "", _textAreaWrap, GUILayout.MinHeight(60), GUILayout.ExpandWidth(true));

			EditorGUILayout.HelpBox("💡 Images will be embedded in generated markdown. Use paths relative to the TLDL file.", MessageType.Info);

			EditorGUILayout.EndVertical();
			}

		private void DrawMetadataSection ()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("📊 Metadata", EditorStyles.boldLabel);

			// 🕐 Time Tracking Section
			this.DrawTimeTrackingSection();

			EditorGUILayout.Space(5);
			EditorGUILayout.LabelField("Additional Properties", EditorStyles.boldLabel);

			this._data.Complexity = (ComplexityLevel)EditorGUILayout.EnumPopup("Complexity", this._data.Complexity);
			this._data.Impact = (ImpactLevel)EditorGUILayout.EnumPopup("Impact", this._data.Impact);
			this._data.Duration = EditorGUILayout.TextField("Duration", this._data.Duration ?? "");
			this._data.TeamMembers = EditorGUILayout.TextField("Team Members", this._data.TeamMembers ?? "");

			this.DrawPlaceholder("Additional metadata", "Custom metadata, metrics, measurements...");
			this._data.CustomMetadata = EditorGUILayout.TextArea(this._data.CustomMetadata ?? "", _textAreaWrap, GUILayout.MinHeight(40), GUILayout.ExpandWidth(true));

			EditorGUILayout.EndVertical();
			}

		// 🕐 NEW: Time Tracking UI Section
		private void DrawTimeTrackingSection ()
			{
			EditorGUILayout.BeginVertical("box");

			using (new EditorGUILayout.HorizontalScope())
				{
				GUILayout.Label("🕐 Time Tracking", EditorStyles.boldLabel);
				GUILayout.FlexibleSpace();

				if (this._data.IsTimerActive)
					{
					// Show active timer with live updates
					System.TimeSpan elapsed = System.DateTime.Now - this._data.SessionStartTime;
					GUILayout.Label($"⏱️ {elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}", EditorStyles.boldLabel);
					}
				else
					{
					GUILayout.Label($"📊 Total: {this._data.TotalSessionMinutes:F1}m", EditorStyles.miniLabel);
					}
				}

			// Task description for current session
			if (this._data.IsTimerActive)
				{
				this.DrawPlaceholder("Current task", "What are you working on right now?");
				this._data.ActiveTaskDescription = EditorGUILayout.TextField("Active Task", this._data.ActiveTaskDescription ?? "");
				}

			// Clock In/Out Controls
			using (new EditorGUILayout.HorizontalScope())
				{
				if (this._data.IsTimerActive)
					{
					if (GUILayout.Button("🕐 Clock Out", GUILayout.Height(30)))
						{
						this.ClockOut();
						}

					if (GUILayout.Button("⏸️ Pause & Note", GUILayout.Width(120), GUILayout.Height(30)))
						{
						this.PauseWithNote();
						}
					}
				else
					{
					if (GUILayout.Button("🕐 Clock In", GUILayout.Height(30)))
						{
						this.ClockIn();
						}

					using (new EditorGUI.DisabledScope(this._data.TimeSessions.Count == 0))
						{
						if (GUILayout.Button("📈 View Sessions", GUILayout.Width(120), GUILayout.Height(30)))
							{
							this.ShowSessionHistory();
							}
						}
					}
				}

			// Session summary
			if (this._data.TimeSessions.Count > 0)
				{
				EditorGUILayout.Space(3);
				TimeSession lastSession = this._data.TimeSessions [ ^1 ];
				EditorGUILayout.LabelField($"Last: {lastSession.StartTime:HH:mm}-{lastSession.EndTime:HH:mm} ({lastSession.DurationMinutes:F1}m)", EditorStyles.miniLabel);

				if (this._data.TimeSessions.Count > 1)
					{
					EditorGUILayout.LabelField($"Sessions today: {this._data.TimeSessions.Count} | Total: {this._data.TotalSessionMinutes:F1}m", EditorStyles.miniLabel);
					}
				}

			EditorGUILayout.EndVertical();
			}

		private void ClockIn ()
			{
			this._data.IsTimerActive = true;
			this._data.SessionStartTime = System.DateTime.Now;
			this._data.ActiveTaskDescription = this._data.Title ?? "Documentation Task";
			this.SetStatus($"🕐 Clocked in at {this._data.SessionStartTime:HH:mm} - Timer started!");

			// Force GUI repaint for live timer
			EditorApplication.update += this.UpdateTimer;
			}

		private void ClockOut ()
			{
			if (!this._data.IsTimerActive) return;

			var session = new TimeSession(this._data.SessionStartTime, this._data.ActiveTaskDescription);
			session.EndSession("Session completed");

			this._data.TimeSessions.Add(session);
			this._data.TotalSessionMinutes += session.DurationMinutes;
			this._data.IsTimerActive = false;

			// Update duration field with accumulated time
			this._data.Duration = $"{this._data.TotalSessionMinutes:F1} minutes";

			this.SetStatus($"🕐 Clocked out! Session: {session.DurationMinutes:F1}m | Total: {this._data.TotalSessionMinutes:F1}m");

			// Stop GUI updates
			EditorApplication.update -= this.UpdateTimer;
			}

		private void PauseWithNote ()
			{
			string note = EditorUtility.DisplayDialogComplex(
				"Pause Session",
				"Add a note for this work session:",
				"Save & Continue", "Clock Out", "Cancel") switch
				{
					0 => "Session paused - continuing work",
					1 => "Session completed early",
					_ => null
					};

			if (note == null) return;

			if (note.Contains("completed"))
				{
				this.ClockOut();
				}
			else
				{
				// Just add a note but keep timer running
				this.SetStatus($"📝 Session note added: {note}");
				}
			}

		private void ShowSessionHistory ()
			{
			var content = new System.Text.StringBuilder();
			content.AppendLine("📊 Time Tracking History");
			content.AppendLine($"Total Sessions: {this._data.TimeSessions.Count}");
			content.AppendLine($"Total Time: {this._data.TotalSessionMinutes:F1} minutes ({this._data.TotalSessionMinutes / 60:F1} hours)");
			content.AppendLine();

			foreach (TimeSession session in this._data.TimeSessions)
				{
				content.AppendLine($"• {session}");
				}

			EditorUtility.DisplayDialog("Time Tracking History", content.ToString(), "Close");
			}

		private void UpdateTimer ()
			{
			if (!this._data.IsTimerActive)
				{
				EditorApplication.update -= this.UpdateTimer;
				return;
				}

			// Force repaint to update live timer display
			if (EditorWindow.focusedWindow != null)
				{
				EditorWindow.focusedWindow.Repaint();
				}
			}

		private void DrawAutoSyncControls ()
			{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("🔄 Form → Editor Sync", EditorStyles.boldLabel);
			this._data.AutoSyncEditor = EditorGUILayout.ToggleLeft("Auto-sync changes to Editor (raw)", this._data.AutoSyncEditor);

			using (new EditorGUILayout.HorizontalScope())
				{
				if (GUILayout.Button("📝 Update Editor From Form", GUILayout.Width(200)))
					{
					if (this.WarnOverwriteRawIfDirty())
						{
						this._data.RawContent = this.BuildMarkdown();
						this._data.RawGeneratedSnapshot = this._data.RawContent;
						this._data.RawDirty = false;
						this.ShouldSwitchToEditor = true;
						this._data.LastFormSnapshot = this.BuildFormSnapshot();
						this.SetStatus("📝 Editor updated from form");
						}
					}
				if (GUILayout.Button("👀 Preview From Form", GUILayout.Width(160)))
					{
					if (this.WarnOverwriteRawIfDirty())
						{
						this._data.RawContent = this.BuildMarkdown();
						this._data.RawGeneratedSnapshot = this._data.RawContent;
						this._data.RawDirty = false;
						this._data.LastFormSnapshot = this.BuildFormSnapshot();
						this.ShouldSwitchToPreview = true;
						this.SetStatus("👀 Preview generated from form");
						}
					}
				}

			EditorGUILayout.HelpBox("Use 'Update Editor From Form' to push current form data into the raw markdown buffer. Enable auto-sync to regenerate whenever form content changes.", MessageType.Info);
			EditorGUILayout.EndVertical();

			// Auto-sync processing
			if (this._data.AutoSyncEditor)
				{
				string snap = this.BuildFormSnapshot();
				if (snap != this._data.LastFormSnapshot)
					{
					if (this._data.RawDirty)
						{
						this.SetStatus("🔄 Auto-sync skipped (raw editor has manual edits)");
						}
					else
						{
						this._data.RawContent = this.BuildMarkdown();
						this._data.RawGeneratedSnapshot = this._data.RawContent;
						this._data.RawDirty = false;
						this._data.LastFormSnapshot = snap;
						this.SetStatus("🔄 Editor auto-synced from form");
						}
					}
				}
			}

		private bool WarnOverwriteRawIfDirty ()
			{
			return !this._data.RawDirty || EditorUtility.DisplayDialog("Overwrite Raw Editor?", "You have unsaved manual edits in the raw editor. Overwrite with regenerated content?", "Overwrite", "Cancel");
			}

		private string BuildMarkdown ()
			{
			// 🎯 ENHANCED: Complete markdown builder with all sections
			var sb = new System.Text.StringBuilder();

			// Header
			sb.AppendLine("# 📜 TLDL Entry");
			sb.AppendLine($"**Entry ID:** TLDL-{System.DateTime.UtcNow:yyyy-MM-dd}-{ScribeUtils.SanitizeTitle(this._data.Title)}");
			sb.AppendLine($"**Author:** {(this._data.Author?.Trim().Length > 0 ? this._data.Author.Trim() : "@copilot")}");
			sb.AppendLine($"**Context:** {this._data.Context}");
			sb.AppendLine($"**Summary:** {this._data.Summary}");

			if (!string.IsNullOrWhiteSpace(this._data.TagsCsv))
				{
				sb.AppendLine($"**Tags:** {this._data.TagsCsv}");
				}

			sb.AppendLine();
			sb.AppendLine("---");
			sb.AppendLine();
			sb.AppendLine("> 📜 \"[Insert inspirational quote from Secret Art of the Living Dev using: `python3 src/ScrollQuoteEngine/quote_engine.py --context documentation --format markdown`]\"");
			sb.AppendLine();
			sb.AppendLine("---");
			sb.AppendLine();

			// Dynamic sections based on toggles
			if (this._data.IncludeDiscoveries && !string.IsNullOrWhiteSpace(this._data.DiscoveriesText))
				{
				sb.AppendLine("## 🔍 Discoveries");
				sb.AppendLine();
				sb.AppendLine(this._data.DiscoveriesText);
				sb.AppendLine();
				}

			if (this._data.IncludeActions && !string.IsNullOrWhiteSpace(this._data.ActionsTaken))
				{
				sb.AppendLine("## ⚡ Actions Taken");
				sb.AppendLine();
				sb.AppendLine(this._data.ActionsTaken);
				sb.AppendLine();
				}

			if (this._data.IncludeTechnicalDetails && !string.IsNullOrWhiteSpace(this._data.TechnicalDetails))
				{
				sb.AppendLine("## 🔧 Technical Details");
				sb.AppendLine();
				sb.AppendLine(this._data.TechnicalDetails);
				sb.AppendLine();
				}

			if (this._data.IncludeTerminalProof && !string.IsNullOrWhiteSpace(this._data.TerminalProof))
				{
				sb.AppendLine("## 💻 Terminal Proof");
				sb.AppendLine();
				sb.AppendLine("```");
				sb.AppendLine(this._data.TerminalProof);
				sb.AppendLine("```");
				sb.AppendLine();
				}

			if (this._data.IncludeDependencies && !string.IsNullOrWhiteSpace(this._data.Dependencies))
				{
				sb.AppendLine("## 📦 Dependencies");
				sb.AppendLine();
				sb.AppendLine(this._data.Dependencies);
				sb.AppendLine();
				}

			if (this._data.IncludeLessons && !string.IsNullOrWhiteSpace(this._data.LessonsLearned))
				{
				sb.AppendLine("## 🎓 Lessons Learned");
				sb.AppendLine();
				sb.AppendLine(this._data.LessonsLearned);
				sb.AppendLine();
				}

			if (this._data.IncludeNextSteps && !string.IsNullOrWhiteSpace(this._data.NextSteps))
				{
				sb.AppendLine("## 🚀 Next Steps");
				sb.AppendLine();
				sb.AppendLine(this._data.NextSteps);
				sb.AppendLine();
				}

			if (this._data.IncludeReferences && !string.IsNullOrWhiteSpace(this._data.References))
				{
				sb.AppendLine("## 🔗 References");
				sb.AppendLine();
				sb.AppendLine(this._data.References);
				sb.AppendLine();
				}

			if (this._data.IncludeDevTimeTravel && !string.IsNullOrWhiteSpace(this._data.DevTimeTravel))
				{
				sb.AppendLine("## ⏰ DevTimeTravel");
				sb.AppendLine();
				sb.AppendLine(this._data.DevTimeTravel);
				sb.AppendLine();
				}

			if (this._data.IncludeImages && !string.IsNullOrWhiteSpace(this._data.ImagePaths))
				{
				sb.AppendLine("## 🖼️ Images");
				sb.AppendLine();

				// Process image paths and embed them
				string [ ] imagePaths = this._data.ImagePaths.Split('\n');
				foreach (string imagePath in imagePaths)
					{
					string trimmedPath = imagePath.Trim();
					if (!string.IsNullOrEmpty(trimmedPath))
						{
						sb.AppendLine($"![Image]({trimmedPath})");
						sb.AppendLine();
						}
					}
				}

			if (this._data.IncludeMetadata)
				{
				sb.AppendLine("## 📊 Metadata");
				sb.AppendLine();
				sb.AppendLine($"**Complexity:** {this._data.Complexity}");
				sb.AppendLine($"**Impact:** {this._data.Impact}");

				if (!string.IsNullOrWhiteSpace(this._data.Duration))
					sb.AppendLine($"**Duration:** {this._data.Duration}");

				if (!string.IsNullOrWhiteSpace(this._data.TeamMembers))
					sb.AppendLine($"**Team Members:** {this._data.TeamMembers}");

				sb.AppendLine($"**Created:** {System.DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

				// Time Tracking Metadata
				if (this._data.TimeSessions.Count > 0)
					{
					sb.AppendLine();
					sb.AppendLine("### ⏱️ Time Tracking");
					sb.AppendLine($"**Total Sessions:** {this._data.TimeSessions.Count}");
					sb.AppendLine($"**Total Time:** {this._data.TotalSessionMinutes:F1} minutes ({this._data.TotalSessionMinutes / 60:F1} hours)");

					if (this._data.IsTimerActive)
						{
						double currentElapsed = (System.DateTime.Now - this._data.SessionStartTime).TotalMinutes;
						sb.AppendLine($"**Active Session:** {currentElapsed:F1}m (started {this._data.SessionStartTime:HH:mm})");
						}

					sb.AppendLine();
					sb.AppendLine("**Session History:**");
					foreach (TimeSession session in this._data.TimeSessions)
						{
						sb.AppendLine($"- {session.StartTime:MM/dd HH:mm}-{session.EndTime:HH:mm}: {session.DurationMinutes:F1}m - {session.TaskDescription}");
						}
					}

				if (!string.IsNullOrWhiteSpace(this._data.CustomMetadata))
					{
					sb.AppendLine();
					sb.AppendLine(this._data.CustomMetadata);
					}
				sb.AppendLine();
				}

			return sb.ToString();
			}

		private string BuildFormSnapshot ()
			{
			// Enhanced snapshot for auto-sync detection
			var sb = new System.Text.StringBuilder();
			sb.Append(this._data.Title).Append('|').Append(this._data.Author).Append('|').Append(this._data.Context).Append('|').Append(this._data.Summary).Append('|').Append(this._data.TagsCsv);
			sb.Append('|').Append(this._data.IncludeDiscoveries).Append(this._data.IncludeActions).Append(this._data.IncludeTechnicalDetails);
			sb.Append('|').Append(this._data.IncludeTerminalProof).Append(this._data.IncludeDependencies).Append(this._data.IncludeLessons);
			sb.Append('|').Append(this._data.IncludeNextSteps).Append(this._data.IncludeReferences).Append(this._data.IncludeDevTimeTravel);
			sb.Append('|').Append(this._data.IncludeMetadata).Append(this._data.IncludeImages);

			// Include content hashes for sections
			sb.Append('|').Append(this._data.DiscoveriesText?.GetHashCode() ?? 0);
			sb.Append('|').Append(this._data.ActionsTaken?.GetHashCode() ?? 0);
			sb.Append('|').Append(this._data.TechnicalDetails?.GetHashCode() ?? 0);
			sb.Append('|').Append(this._data.TerminalProof?.GetHashCode() ?? 0);
			sb.Append('|').Append(this._data.Dependencies?.GetHashCode() ?? 0);
			sb.Append('|').Append(this._data.LessonsLearned?.GetHashCode() ?? 0);
			sb.Append('|').Append(this._data.NextSteps?.GetHashCode() ?? 0);
			sb.Append('|').Append(this._data.References?.GetHashCode() ?? 0);
			sb.Append('|').Append(this._data.DevTimeTravel?.GetHashCode() ?? 0);
			sb.Append('|').Append(this._data.ImagePaths?.GetHashCode() ?? 0);
			sb.Append('|').Append(this._data.CustomMetadata?.GetHashCode() ?? 0);

			return sb.ToString();
			}

		private void TryCreateTLDL ()
			{
			try
				{
				string date = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
				string safeTitle = ScribeUtils.SanitizeTitle(this._data.Title);
				if (string.IsNullOrEmpty(safeTitle))
					safeTitle = "Entry";

				string fileName = $"TLDL-{date}-{safeTitle}.md";
				string targetFolder = this.ResolveActiveFolder(this.GetApplication());
				if (!System.IO.Directory.Exists(targetFolder))
					{
					System.IO.Directory.CreateDirectory(targetFolder);
					}

				string absPath = System.IO.Path.Combine(targetFolder, fileName);
				string md = this.BuildMarkdown();
				System.IO.File.WriteAllText(absPath, md, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

				this._data.CurrentFilePath = absPath;
				this._data.RawContent = md;
				this._data.RawGeneratedSnapshot = md;
				this._data.RawDirty = false;

				this.SetStatus($"📄 TLDL created: {this.MakeProjectRelative(absPath)}");
				}
			catch (System.Exception ex)
				{
				this.SetStatus($"❌ Error creating TLDL: {ex.Message}");
				}
			}

		private string GetApplication ()
			{
			// Fix: Return string instead of Application type - this appears to be expecting a path
			return Application.dataPath;
			}

		private string ResolveActiveFolder (string applicationPath)
			{
			return !string.IsNullOrEmpty(this._data.ActiveDirPath)
				? this._data.ActiveDirPath
				: !string.IsNullOrEmpty(this._data.RootPath)
				? this._data.RootPath
				: System.IO.Path.Combine(Application.dataPath, "Plugins/TLDA/docs");
			}

		private string MakeProjectRelative (string absPath)
			{
			string projectRoot = System.IO.Directory.GetParent(Application.dataPath)!.FullName.Replace('\\', '/');
			string norm = absPath.Replace('\\', '/');
			return norm.StartsWith(projectRoot) ? norm [ (projectRoot.Length + 1).. ] : absPath;
			}
		}
	}
#endif
