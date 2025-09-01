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

		public FormModule(TLDLScribeData data) : base(data) { }

		public void ResetSwitchFlags()
			{
			ShouldSwitchToEditor = false;
			ShouldSwitchToPreview = false;
			}

		public void DrawToolbar()
			{
			// 🕐 Time Tracking Controls (Prime Position!)
			if (_data.IsTimerActive)
				{
				System.TimeSpan elapsed = System.DateTime.Now - _data.SessionStartTime;
				string timerText = $"⏱️ {elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";

				// Active timer display with clock out
				GUILayout.Label(timerText, EditorStyles.boldLabel, GUILayout.Width(100));

				if (GUILayout.Button("🕐 Clock Out", EditorStyles.toolbarButton, GUILayout.Width(80)))
					{
					ClockOut();
					}
				}
			else
				{
				if (GUILayout.Button("🕐 Clock In", EditorStyles.toolbarButton, GUILayout.Width(80)))
					{
					ClockIn();
					}

				if (_data.TotalSessionMinutes > 0)
					{
					GUILayout.Label($"📊 {_data.TotalSessionMinutes:F0}m", EditorStyles.miniLabel, GUILayout.Width(60));
					}
				}

			GUILayout.Space(10);

			// Form operations
			if (GUILayout.Button("🎯 Generate → Editor", EditorStyles.toolbarButton))
				{
				if (WarnOverwriteRawIfDirty())
					{
					string md = BuildMarkdown();
					_data.RawContent = md;
					_data.RawGeneratedSnapshot = md;
					_data.RawDirty = false;
					ShouldSwitchToEditor = true;
					SetStatus("🎯 Generated content from form into Editor");
					}
				}

			if (GUILayout.Button("📄 Create TLDL", EditorStyles.toolbarButton))
				{
				TryCreateTLDL();
				}
			}

		public void DrawContent(Rect windowPosition)
			{
			float viewportHeight = Mathf.Max(140f, windowPosition.height - 220f);
			_data.FormScroll = EditorGUILayout.BeginScrollView(_data.FormScroll, GUILayout.Height(viewportHeight), GUILayout.ExpandHeight(true));

			// Header section
			DrawHeaderSection();

			// Sections toggles and content
			DrawSectionsToggles();
			DrawSectionEditors(); // 🎯 NEW: Detailed section editors

			// Auto-sync controls
			DrawAutoSyncControls();

			EditorGUILayout.EndScrollView();
			}

		private void DrawHeaderSection()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("📋 Header", EditorStyles.boldLabel);

			DrawHelp("Title", "Short, descriptive. Used in the filename.");
			_data.Title = EditorGUILayout.TextField("Title", _data.Title);

			_data.Author = EditorGUILayout.TextField("Author", string.IsNullOrWhiteSpace(_data.Author) ? "@copilot" : _data.Author);

			DrawPlaceholder("Context", "Issue #XX, Feature Name, or short description.");
			_data.Context = EditorGUILayout.TextArea(_data.Context, _textAreaWrap, GUILayout.MinHeight(40), GUILayout.ExpandWidth(true));

			DrawPlaceholder("Summary", "One line describing the result.");
			_data.Summary = EditorGUILayout.TextArea(_data.Summary, _textAreaWrap, GUILayout.MinHeight(40), GUILayout.ExpandWidth(true));

			DrawPlaceholder("Tags (comma-separated)", "e.g., Chronicle Keeper, LDA, Docs");
			_data.TagsCsv = EditorGUILayout.TextField("Tags", _data.TagsCsv);

			EditorGUILayout.EndVertical();
			}

		private void DrawSectionsToggles()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("🗂️ Sections", EditorStyles.boldLabel);

			_data.IncludeDiscoveries = EditorGUILayout.ToggleLeft("🔍 Include Discoveries", _data.IncludeDiscoveries);
			_data.IncludeActions = EditorGUILayout.ToggleLeft("⚡ Include Actions Taken", _data.IncludeActions);
			_data.IncludeTechnicalDetails = EditorGUILayout.ToggleLeft("🔧 Include Technical Details", _data.IncludeTechnicalDetails);
			_data.IncludeTerminalProof = EditorGUILayout.ToggleLeft("💻 Include Terminal Proof", _data.IncludeTerminalProof);
			_data.IncludeDependencies = EditorGUILayout.ToggleLeft("📦 Include Dependencies", _data.IncludeDependencies);
			_data.IncludeLessons = EditorGUILayout.ToggleLeft("🎓 Include Lessons Learned", _data.IncludeLessons);
			_data.IncludeNextSteps = EditorGUILayout.ToggleLeft("🚀 Include Next Steps", _data.IncludeNextSteps);
			_data.IncludeReferences = EditorGUILayout.ToggleLeft("🔗 Include References", _data.IncludeReferences);
			_data.IncludeDevTimeTravel = EditorGUILayout.ToggleLeft("⏰ Include DevTimeTravel", _data.IncludeDevTimeTravel);
			_data.IncludeMetadata = EditorGUILayout.ToggleLeft("📊 Include Metadata", _data.IncludeMetadata);
			_data.IncludeImages = EditorGUILayout.ToggleLeft("🖼️ Include Images", _data.IncludeImages);

			EditorGUILayout.EndVertical();
			}

		// 🎯 NEW: Complete section editors for all toggles
		private void DrawSectionEditors()
			{
			if (_data.IncludeDiscoveries)
				{
				DrawDiscoveriesSection();
				}

			if (_data.IncludeActions)
				{
				DrawActionsSection();
				}

			if (_data.IncludeTechnicalDetails)
				{
				DrawTechnicalDetailsSection();
				}

			if (_data.IncludeTerminalProof)
				{
				DrawTerminalProofSection();
				}

			if (_data.IncludeDependencies)
				{
				DrawDependenciesSection();
				}

			if (_data.IncludeLessons)
				{
				DrawLessonsSection();
				}

			if (_data.IncludeNextSteps)
				{
				DrawNextStepsSection();
				}

			if (_data.IncludeReferences)
				{
				DrawReferencesSection();
				}

			if (_data.IncludeDevTimeTravel)
				{
				DrawDevTimeTravelSection();
				}

			if (_data.IncludeMetadata)
				{
				DrawMetadataSection();
				}

			if (_data.IncludeImages)
				{
				DrawImagesSection();
				}
			}

		private void DrawDiscoveriesSection()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("🔍 Discoveries", EditorStyles.boldLabel);

			DrawPlaceholder("Key findings", "What did you discover? New patterns, unexpected behaviors, root causes...");
			_data.DiscoveriesText = EditorGUILayout.TextArea(_data.DiscoveriesText ?? "", _textAreaWrap, GUILayout.MinHeight(80), GUILayout.ExpandWidth(true));

			EditorGUILayout.EndVertical();
			}

		private void DrawActionsSection()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("⚡ Actions Taken", EditorStyles.boldLabel);

			DrawPlaceholder("What did you do?", "Step-by-step actions, changes made, commands run...");
			_data.ActionsTaken = EditorGUILayout.TextArea(_data.ActionsTaken ?? "", _textAreaWrap, GUILayout.MinHeight(80), GUILayout.ExpandWidth(true));

			EditorGUILayout.EndVertical();
			}

		private void DrawTechnicalDetailsSection()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("🔧 Technical Details", EditorStyles.boldLabel);

			DrawPlaceholder("Architecture insights", "Code patterns, system design, technical decisions...");
			_data.TechnicalDetails = EditorGUILayout.TextArea(_data.TechnicalDetails ?? "", _textAreaWrap, GUILayout.MinHeight(80), GUILayout.ExpandWidth(true));

			EditorGUILayout.EndVertical();
			}

		private void DrawTerminalProofSection()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("💻 Terminal Proof", EditorStyles.boldLabel);

			DrawPlaceholder("Command output", "Terminal commands and their output as evidence...");
			_data.TerminalProof = EditorGUILayout.TextArea(_data.TerminalProof ?? "", EditorStyles.textArea, GUILayout.MinHeight(100), GUILayout.ExpandWidth(true));

			EditorGUILayout.EndVertical();
			}

		private void DrawDependenciesSection()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("📦 Dependencies", EditorStyles.boldLabel);

			DrawPlaceholder("Dependencies used", "Libraries, packages, tools, systems this work depends on...");
			_data.Dependencies = EditorGUILayout.TextArea(_data.Dependencies ?? "", _textAreaWrap, GUILayout.MinHeight(60), GUILayout.ExpandWidth(true));

			EditorGUILayout.EndVertical();
			}

		private void DrawLessonsSection()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("🎓 Lessons Learned", EditorStyles.boldLabel);

			DrawPlaceholder("What did you learn?", "Insights, gotchas, best practices discovered...");
			_data.LessonsLearned = EditorGUILayout.TextArea(_data.LessonsLearned ?? "", _textAreaWrap, GUILayout.MinHeight(80), GUILayout.ExpandWidth(true));

			EditorGUILayout.EndVertical();
			}

		private void DrawNextStepsSection()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("🚀 Next Steps", EditorStyles.boldLabel);

			DrawPlaceholder("Future work", "What should be done next? Follow-up tasks, improvements...");
			_data.NextSteps = EditorGUILayout.TextArea(_data.NextSteps ?? "", _textAreaWrap, GUILayout.MinHeight(60), GUILayout.ExpandWidth(true));

			EditorGUILayout.EndVertical();
			}

		private void DrawReferencesSection()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("🔗 References", EditorStyles.boldLabel);

			DrawPlaceholder("Links and references", "URLs, documentation, related issues, commit hashes...");
			_data.References = EditorGUILayout.TextArea(_data.References ?? "", _textAreaWrap, GUILayout.MinHeight(60), GUILayout.ExpandWidth(true));

			EditorGUILayout.EndVertical();
			}

		private void DrawDevTimeTravelSection()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("⏰ DevTimeTravel", EditorStyles.boldLabel);

			DrawPlaceholder("Context snapshots", "Environment state, configuration snapshots, timeline context...");
			_data.DevTimeTravel = EditorGUILayout.TextArea(_data.DevTimeTravel ?? "", _textAreaWrap, GUILayout.MinHeight(60), GUILayout.ExpandWidth(true));

			EditorGUILayout.EndVertical();
			}

		private void DrawImagesSection()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("🖼️ Images", EditorStyles.boldLabel);

			DrawPlaceholder("Image references", "Relative paths to images, screenshots, diagrams...");
			_data.ImagePaths = EditorGUILayout.TextArea(_data.ImagePaths ?? "", _textAreaWrap, GUILayout.MinHeight(60), GUILayout.ExpandWidth(true));

			EditorGUILayout.HelpBox("💡 Images will be embedded in generated markdown. Use paths relative to the TLDL file.", MessageType.Info);

			EditorGUILayout.EndVertical();
			}

		private void DrawMetadataSection()
			{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("📊 Metadata", EditorStyles.boldLabel);

			// 🕐 Time Tracking Section
			DrawTimeTrackingSection();

			EditorGUILayout.Space(5);
			EditorGUILayout.LabelField("Additional Properties", EditorStyles.boldLabel);

			_data.Complexity = (ComplexityLevel)EditorGUILayout.EnumPopup("Complexity", _data.Complexity);
			_data.Impact = (ImpactLevel)EditorGUILayout.EnumPopup("Impact", _data.Impact);
			_data.Duration = EditorGUILayout.TextField("Duration", _data.Duration ?? "");
			_data.TeamMembers = EditorGUILayout.TextField("Team Members", _data.TeamMembers ?? "");

			DrawPlaceholder("Additional metadata", "Custom metadata, metrics, measurements...");
			_data.CustomMetadata = EditorGUILayout.TextArea(_data.CustomMetadata ?? "", _textAreaWrap, GUILayout.MinHeight(40), GUILayout.ExpandWidth(true));

			EditorGUILayout.EndVertical();
			}

		// 🕐 NEW: Time Tracking UI Section
		private void DrawTimeTrackingSection()
			{
			EditorGUILayout.BeginVertical("box");

			using (new EditorGUILayout.HorizontalScope())
				{
				GUILayout.Label("🕐 Time Tracking", EditorStyles.boldLabel);
				GUILayout.FlexibleSpace();

				if (_data.IsTimerActive)
					{
					// Show active timer with live updates
					System.TimeSpan elapsed = System.DateTime.Now - _data.SessionStartTime;
					GUILayout.Label($"⏱️ {elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}", EditorStyles.boldLabel);
					}
				else
					{
					GUILayout.Label($"📊 Total: {_data.TotalSessionMinutes:F1}m", EditorStyles.miniLabel);
					}
				}

			// Task description for current session
			if (_data.IsTimerActive)
				{
				DrawPlaceholder("Current task", "What are you working on right now?");
				_data.ActiveTaskDescription = EditorGUILayout.TextField("Active Task", _data.ActiveTaskDescription ?? "");
				}

			// Clock In/Out Controls
			using (new EditorGUILayout.HorizontalScope())
				{
				if (_data.IsTimerActive)
					{
					if (GUILayout.Button("🕐 Clock Out", GUILayout.Height(30)))
						{
						ClockOut();
						}

					if (GUILayout.Button("⏸️ Pause & Note", GUILayout.Width(120), GUILayout.Height(30)))
						{
						PauseWithNote();
						}
					}
				else
					{
					if (GUILayout.Button("🕐 Clock In", GUILayout.Height(30)))
						{
						ClockIn();
						}

					using (new EditorGUI.DisabledScope(_data.TimeSessions.Count == 0))
						{
						if (GUILayout.Button("📈 View Sessions", GUILayout.Width(120), GUILayout.Height(30)))
							{
							ShowSessionHistory();
							}
						}
					}
				}

			// Session summary
			if (_data.TimeSessions.Count > 0)
				{
				EditorGUILayout.Space(3);
				TimeSession lastSession = _data.TimeSessions [ ^1 ];
				EditorGUILayout.LabelField($"Last: {lastSession.StartTime:HH:mm}-{lastSession.EndTime:HH:mm} ({lastSession.DurationMinutes:F1}m)", EditorStyles.miniLabel);

				if (_data.TimeSessions.Count > 1)
					{
					EditorGUILayout.LabelField($"Sessions today: {_data.TimeSessions.Count} | Total: {_data.TotalSessionMinutes:F1}m", EditorStyles.miniLabel);
					}
				}

			EditorGUILayout.EndVertical();
			}

		private void ClockIn()
			{
			_data.IsTimerActive = true;
			_data.SessionStartTime = System.DateTime.Now;
			_data.ActiveTaskDescription = _data.Title ?? "Documentation Task";
			SetStatus($"🕐 Clocked in at {_data.SessionStartTime:HH:mm} - Timer started!");

			// Force GUI repaint for live timer
			EditorApplication.update += UpdateTimer;
			}

		private void ClockOut()
			{
			if (!_data.IsTimerActive) return;

			var session = new TimeSession(_data.SessionStartTime, _data.ActiveTaskDescription);
			session.EndSession("Session completed");

			_data.TimeSessions.Add(session);
			_data.TotalSessionMinutes += session.DurationMinutes;
			_data.IsTimerActive = false;

			// Update duration field with accumulated time
			_data.Duration = $"{_data.TotalSessionMinutes:F1} minutes";

			SetStatus($"🕐 Clocked out! Session: {session.DurationMinutes:F1}m | Total: {_data.TotalSessionMinutes:F1}m");

			// Stop GUI updates
			EditorApplication.update -= UpdateTimer;
			}

		private void PauseWithNote()
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
				ClockOut();
				}
			else
				{
				// Just add a note but keep timer running
				SetStatus($"📝 Session note added: {note}");
				}
			}

		private void ShowSessionHistory()
			{
			var content = new System.Text.StringBuilder();
			content.AppendLine("📊 Time Tracking History");
			content.AppendLine($"Total Sessions: {_data.TimeSessions.Count}");
			content.AppendLine($"Total Time: {_data.TotalSessionMinutes:F1} minutes ({_data.TotalSessionMinutes / 60:F1} hours)");
			content.AppendLine();

			foreach (TimeSession session in _data.TimeSessions)
				{
				content.AppendLine($"• {session}");
				}

			EditorUtility.DisplayDialog("Time Tracking History", content.ToString(), "Close");
			}

		private void UpdateTimer()
			{
			if (!_data.IsTimerActive)
				{
				EditorApplication.update -= UpdateTimer;
				return;
				}

			// Force repaint to update live timer display
			if (EditorWindow.focusedWindow != null)
				{
				EditorWindow.focusedWindow.Repaint();
				}
			}

		private void DrawAutoSyncControls()
			{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("🔄 Form → Editor Sync", EditorStyles.boldLabel);
			_data.AutoSyncEditor = EditorGUILayout.ToggleLeft("Auto-sync changes to Editor (raw)", _data.AutoSyncEditor);

			using (new EditorGUILayout.HorizontalScope())
				{
				if (GUILayout.Button("📝 Update Editor From Form", GUILayout.Width(200)))
					{
					if (WarnOverwriteRawIfDirty())
						{
						_data.RawContent = BuildMarkdown();
						_data.RawGeneratedSnapshot = _data.RawContent;
						_data.RawDirty = false;
						ShouldSwitchToEditor = true;
						_data.LastFormSnapshot = BuildFormSnapshot();
						SetStatus("📝 Editor updated from form");
						}
					}
				if (GUILayout.Button("👀 Preview From Form", GUILayout.Width(160)))
					{
					if (WarnOverwriteRawIfDirty())
						{
						_data.RawContent = BuildMarkdown();
						_data.RawGeneratedSnapshot = _data.RawContent;
						_data.RawDirty = false;
						_data.LastFormSnapshot = BuildFormSnapshot();
						ShouldSwitchToPreview = true;
						SetStatus("👀 Preview generated from form");
						}
					}
				}

			EditorGUILayout.HelpBox("Use 'Update Editor From Form' to push current form data into the raw markdown buffer. Enable auto-sync to regenerate whenever form content changes.", MessageType.Info);
			EditorGUILayout.EndVertical();

			// Auto-sync processing
			if (_data.AutoSyncEditor)
				{
				string snap = BuildFormSnapshot();
				if (snap != _data.LastFormSnapshot)
					{
					if (_data.RawDirty)
						{
						SetStatus("🔄 Auto-sync skipped (raw editor has manual edits)");
						}
					else
						{
						_data.RawContent = BuildMarkdown();
						_data.RawGeneratedSnapshot = _data.RawContent;
						_data.RawDirty = false;
						_data.LastFormSnapshot = snap;
						SetStatus("🔄 Editor auto-synced from form");
						}
					}
				}
			}

		private bool WarnOverwriteRawIfDirty()
			{
			return !_data.RawDirty || EditorUtility.DisplayDialog("Overwrite Raw Editor?", "You have unsaved manual edits in the raw editor. Overwrite with regenerated content?", "Overwrite", "Cancel");
			}

		private string BuildMarkdown()
			{
			// 🎯 ENHANCED: Complete markdown builder with all sections
			var sb = new System.Text.StringBuilder();

			// Header
			sb.AppendLine("# 📜 TLDL Entry");
			sb.AppendLine($"**Entry ID:** TLDL-{System.DateTime.UtcNow:yyyy-MM-dd}-{ScribeUtils.SanitizeTitle(_data.Title)}");
			sb.AppendLine($"**Author:** {(_data.Author?.Trim().Length > 0 ? _data.Author.Trim() : "@copilot")}");
			sb.AppendLine($"**Context:** {_data.Context}");
			sb.AppendLine($"**Summary:** {_data.Summary}");

			if (!string.IsNullOrWhiteSpace(_data.TagsCsv))
				{
				sb.AppendLine($"**Tags:** {_data.TagsCsv}");
				}

			sb.AppendLine();
			sb.AppendLine("---");
			sb.AppendLine();
			sb.AppendLine("> 📜 \"[Insert inspirational quote from Secret Art of the Living Dev using: `python3 src/ScrollQuoteEngine/quote_engine.py --context documentation --format markdown`]\"");
			sb.AppendLine();
			sb.AppendLine("---");
			sb.AppendLine();

			// Dynamic sections based on toggles
			if (_data.IncludeDiscoveries && !string.IsNullOrWhiteSpace(_data.DiscoveriesText))
				{
				sb.AppendLine("## 🔍 Discoveries");
				sb.AppendLine();
				sb.AppendLine(_data.DiscoveriesText);
				sb.AppendLine();
				}

			if (_data.IncludeActions && !string.IsNullOrWhiteSpace(_data.ActionsTaken))
				{
				sb.AppendLine("## ⚡ Actions Taken");
				sb.AppendLine();
				sb.AppendLine(_data.ActionsTaken);
				sb.AppendLine();
				}

			if (_data.IncludeTechnicalDetails && !string.IsNullOrWhiteSpace(_data.TechnicalDetails))
				{
				sb.AppendLine("## 🔧 Technical Details");
				sb.AppendLine();
				sb.AppendLine(_data.TechnicalDetails);
				sb.AppendLine();
				}

			if (_data.IncludeTerminalProof && !string.IsNullOrWhiteSpace(_data.TerminalProof))
				{
				sb.AppendLine("## 💻 Terminal Proof");
				sb.AppendLine();
				sb.AppendLine("```");
				sb.AppendLine(_data.TerminalProof);
				sb.AppendLine("```");
				sb.AppendLine();
				}

			if (_data.IncludeDependencies && !string.IsNullOrWhiteSpace(_data.Dependencies))
				{
				sb.AppendLine("## 📦 Dependencies");
				sb.AppendLine();
				sb.AppendLine(_data.Dependencies);
				sb.AppendLine();
				}

			if (_data.IncludeLessons && !string.IsNullOrWhiteSpace(_data.LessonsLearned))
				{
				sb.AppendLine("## 🎓 Lessons Learned");
				sb.AppendLine();
				sb.AppendLine(_data.LessonsLearned);
				sb.AppendLine();
				}

			if (_data.IncludeNextSteps && !string.IsNullOrWhiteSpace(_data.NextSteps))
				{
				sb.AppendLine("## 🚀 Next Steps");
				sb.AppendLine();
				sb.AppendLine(_data.NextSteps);
				sb.AppendLine();
				}

			if (_data.IncludeReferences && !string.IsNullOrWhiteSpace(_data.References))
				{
				sb.AppendLine("## 🔗 References");
				sb.AppendLine();
				sb.AppendLine(_data.References);
				sb.AppendLine();
				}

			if (_data.IncludeDevTimeTravel && !string.IsNullOrWhiteSpace(_data.DevTimeTravel))
				{
				sb.AppendLine("## ⏰ DevTimeTravel");
				sb.AppendLine();
				sb.AppendLine(_data.DevTimeTravel);
				sb.AppendLine();
				}

			if (_data.IncludeImages && !string.IsNullOrWhiteSpace(_data.ImagePaths))
				{
				sb.AppendLine("## 🖼️ Images");
				sb.AppendLine();

				// Process image paths and embed them
				string [ ] imagePaths = _data.ImagePaths.Split('\n');
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

			if (_data.IncludeMetadata)
				{
				sb.AppendLine("## 📊 Metadata");
				sb.AppendLine();
				sb.AppendLine($"**Complexity:** {_data.Complexity}");
				sb.AppendLine($"**Impact:** {_data.Impact}");

				if (!string.IsNullOrWhiteSpace(_data.Duration))
					sb.AppendLine($"**Duration:** {_data.Duration}");

				if (!string.IsNullOrWhiteSpace(_data.TeamMembers))
					sb.AppendLine($"**Team Members:** {_data.TeamMembers}");

				sb.AppendLine($"**Created:** {System.DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

				// Time Tracking Metadata
				if (_data.TimeSessions.Count > 0)
					{
					sb.AppendLine();
					sb.AppendLine("### ⏱️ Time Tracking");
					sb.AppendLine($"**Total Sessions:** {_data.TimeSessions.Count}");
					sb.AppendLine($"**Total Time:** {_data.TotalSessionMinutes:F1} minutes ({_data.TotalSessionMinutes / 60:F1} hours)");

					if (_data.IsTimerActive)
						{
						double currentElapsed = (System.DateTime.Now - _data.SessionStartTime).TotalMinutes;
						sb.AppendLine($"**Active Session:** {currentElapsed:F1}m (started {_data.SessionStartTime:HH:mm})");
						}

					sb.AppendLine();
					sb.AppendLine("**Session History:**");
					foreach (TimeSession session in _data.TimeSessions)
						{
						sb.AppendLine($"- {session.StartTime:MM/dd HH:mm}-{session.EndTime:HH:mm}: {session.DurationMinutes:F1}m - {session.TaskDescription}");
						}
					}

				if (!string.IsNullOrWhiteSpace(_data.CustomMetadata))
					{
					sb.AppendLine();
					sb.AppendLine(_data.CustomMetadata);
					}
				sb.AppendLine();
				}

			return sb.ToString();
			}

		private string BuildFormSnapshot()
			{
			// Enhanced snapshot for auto-sync detection
			var sb = new System.Text.StringBuilder();
			sb.Append(_data.Title).Append('|').Append(_data.Author).Append('|').Append(_data.Context).Append('|').Append(_data.Summary).Append('|').Append(_data.TagsCsv);
			sb.Append('|').Append(_data.IncludeDiscoveries).Append(_data.IncludeActions).Append(_data.IncludeTechnicalDetails);
			sb.Append('|').Append(_data.IncludeTerminalProof).Append(_data.IncludeDependencies).Append(_data.IncludeLessons);
			sb.Append('|').Append(_data.IncludeNextSteps).Append(_data.IncludeReferences).Append(_data.IncludeDevTimeTravel);
			sb.Append('|').Append(_data.IncludeMetadata).Append(_data.IncludeImages);

			// Include content hashes for sections
			sb.Append('|').Append(_data.DiscoveriesText?.GetHashCode() ?? 0);
			sb.Append('|').Append(_data.ActionsTaken?.GetHashCode() ?? 0);
			sb.Append('|').Append(_data.TechnicalDetails?.GetHashCode() ?? 0);
			sb.Append('|').Append(_data.TerminalProof?.GetHashCode() ?? 0);
			sb.Append('|').Append(_data.Dependencies?.GetHashCode() ?? 0);
			sb.Append('|').Append(_data.LessonsLearned?.GetHashCode() ?? 0);
			sb.Append('|').Append(_data.NextSteps?.GetHashCode() ?? 0);
			sb.Append('|').Append(_data.References?.GetHashCode() ?? 0);
			sb.Append('|').Append(_data.DevTimeTravel?.GetHashCode() ?? 0);
			sb.Append('|').Append(_data.ImagePaths?.GetHashCode() ?? 0);
			sb.Append('|').Append(_data.CustomMetadata?.GetHashCode() ?? 0);

			return sb.ToString();
			}

		private void TryCreateTLDL()
			{
			try
				{
				string date = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
				string safeTitle = ScribeUtils.SanitizeTitle(_data.Title);
				if (string.IsNullOrEmpty(safeTitle))
					safeTitle = "Entry";

				string fileName = $"TLDL-{date}-{safeTitle}.md";
				string targetFolder = ResolveActiveFolder(GetApplication());
				if (!System.IO.Directory.Exists(targetFolder))
					{
					System.IO.Directory.CreateDirectory(targetFolder);
					}

				string absPath = System.IO.Path.Combine(targetFolder, fileName);
				string md = BuildMarkdown();
				System.IO.File.WriteAllText(absPath, md, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

				_data.CurrentFilePath = absPath;
				_data.RawContent = md;
				_data.RawGeneratedSnapshot = md;
				_data.RawDirty = false;

				SetStatus($"📄 TLDL created: {MakeProjectRelative(absPath)}");
				}
			catch (System.Exception ex)
				{
				SetStatus($"❌ Error creating TLDL: {ex.Message}");
				}
			}

		private string GetApplication()
			{
			// Fix: Return string instead of Application type - this appears to be expecting a path
			return Application.dataPath;
			}

		private string ResolveActiveFolder(string applicationPath)
			{
			return !string.IsNullOrEmpty(_data.ActiveDirPath)
				? _data.ActiveDirPath
				: !string.IsNullOrEmpty(_data.RootPath)
				? _data.RootPath
				: System.IO.Path.Combine(Application.dataPath, "Plugins/TLDA/docs");
			}

		private string MakeProjectRelative(string absPath)
			{
			string projectRoot = System.IO.Directory.GetParent(Application.dataPath)!.FullName.Replace('\\', '/');
			string norm = absPath.Replace('\\', '/');
			return norm.StartsWith(projectRoot) ? norm [ (projectRoot.Length + 1).. ] : absPath;
			}
		}
	}
#endif
