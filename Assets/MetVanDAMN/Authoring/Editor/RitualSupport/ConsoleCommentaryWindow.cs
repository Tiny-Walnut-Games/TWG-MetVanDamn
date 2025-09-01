#if UNITY_EDITOR
// @Intent: Interactive console commentary system for developer annotations
// @CheekPreservation: Turn "FUCK!!!" moments into documented learning opportunities
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring.Editor.RitualSupport
	{
	public class ConsoleCommentaryWindow : EditorWindow
		{
		[MenuItem("Tiny Walnut Games/MetVanDAMN/Diagnostics/Console Commentary Window", priority = 200)]
		private static void ShowWindow()
			{
			ConsoleCommentaryWindow window = GetWindow<ConsoleCommentaryWindow>("Console Commentary");
			window.titleContent = new GUIContent("🗨️ Console Commentary", "Add developer annotations to Unity console logs");
			window.Show();
			}

		private Vector2 _scrollPosition;
		private Vector2 _logsScrollPosition;
		private string _newComment = "";
		private string _selectedLogEntry = "";
		private LogType _selectedLogType = LogType.Log;
		private List<CommentaryEntry> _commentaries = new();
		private string _commentaryFilePath;
		private List<LogEntry> _capturedLogs = new();
		private bool _isCapturing = true;
		private int _selectedLogIndex = -1;
		private string _sessionName = "";
		private bool _autoScrollLogs = true;
		
		// Jerry's BRILLIANT adjustable range system
		private int _snapshotLinesBefore = 10;
		private int _snapshotLinesAfter = 10;

		[Serializable]
		private class LogEntry
			{
			public string message;
			public LogType type;
			public string stackTrace;
			public string timestamp;
			public bool isCommentaryLog; // Flag to prevent infinite recursion

			public LogEntry(string msg, LogType logType, string stack)
				{
				message = msg;
				type = logType;
				stackTrace = stack;
				timestamp = DateTime.Now.ToString("HH:mm:ss");
				isCommentaryLog = msg.StartsWith("💬 Commentary added") ||
								msg.StartsWith("📜 Console commentary") ||
								msg.StartsWith("🧹 Console commentary") ||
								msg.StartsWith("📂 Raw console logs") ||
								msg.StartsWith("📡 Console Commentary:");
				}
			}

		[Serializable]
		private class CommentaryEntry
			{
			public string timestamp;
			public string logMessage;
			public LogType logType;
			public string developerComment;
			public string context;
			public string tags;
			public string stackTrace;
			public int logIndex; // Reference to which log this commentary is about

			public CommentaryEntry(string log, LogType type, string comment, string ctx = "", string tagList = "", string stack = "", int logIdx = -1)
				{
				timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				logMessage = log;
				logType = type;
				developerComment = comment;
				context = ctx;
				tags = tagList;
				stackTrace = stack;
				logIndex = logIdx;
				}
			}

		private void OnEnable()
			{
			_commentaryFilePath = Path.Combine(Application.dataPath, "debug", "console_commentary.json");
			LoadCommentaries();

			// Initialize session name
			if (string.IsNullOrEmpty(_sessionName))
				{
				_sessionName = $"Session-{DateTime.Now:HHmmss}";
				}

			// Hook into Unity's log callback to capture new entries
			Application.logMessageReceived += OnLogMessageReceived;

			// Try to get Unity console history using reflection
			LoadExistingConsoleLogs();
			}

		private void OnDisable()
			{
			Application.logMessageReceived -= OnLogMessageReceived;
			SaveCommentaries();
			}

		private void LoadExistingConsoleLogs()
			{
			try
				{
				// Use reflection to access Unity's Console window and its log entries
				Type consoleWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
				if (consoleWindowType != null)
					{
					// Try to get existing console entries - this is complex reflection
					// For now, we'll just start fresh but this is where we'd grab Unity's internal log buffer
					Debug.Log("🗨️ Console Commentary: Monitoring started - capturing future logs");
					}
				}
			catch
				{
				// Silently fail - reflection access to console internals is fragile
				}
			}

		private void OnLogMessageReceived(string logString, string stackTrace, LogType type)
			{
			if (!_isCapturing) return;

			var logEntry = new LogEntry(logString, type, stackTrace);

			// Prevent infinite recursion by filtering our own commentary logs
			if (!logEntry.isCommentaryLog)
				{
				_capturedLogs.Add(logEntry);

				// Keep only last 200 logs to prevent memory bloat
				if (_capturedLogs.Count > 200)
					{
					_capturedLogs.RemoveAt(0);
					// Adjust selected index if it was pointing to removed log
					if (_selectedLogIndex >= 0) _selectedLogIndex--;
					}

				// Auto-select the most recent non-commentary log
				_selectedLogIndex = _capturedLogs.Count - 1;
				UpdateSelectedLogFromIndex();

				if (_autoScrollLogs)
					{
					_logsScrollPosition.y = float.MaxValue; // Scroll to bottom
					}

				Repaint();
				}
			}

		private void UpdateSelectedLogFromIndex()
			{
			if (_selectedLogIndex >= 0 && _selectedLogIndex < _capturedLogs.Count)
				{
				LogEntry logEntry = _capturedLogs [ _selectedLogIndex ];
				_selectedLogEntry = logEntry.message;
				_selectedLogType = logEntry.type;
				}
			}

		private void OnGUI()
			{
			GUILayout.Label("🗨️ Console Commentary System", EditorStyles.boldLabel);

			// Session info and controls
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Session:", GUILayout.Width(60));
			_sessionName = EditorGUILayout.TextField(_sessionName, GUILayout.Width(150));

			GUILayout.FlexibleSpace();

			bool newCapturing = EditorGUILayout.Toggle("📡 Capture", _isCapturing, GUILayout.Width(80));
			if (newCapturing != _isCapturing)
				{
				_isCapturing = newCapturing;
				}

			_autoScrollLogs = EditorGUILayout.Toggle("📜 Auto-scroll", _autoScrollLogs, GUILayout.Width(90));

			// Manual refresh button - only show when auto-capture is disabled
			if (!_isCapturing)
				{
				if (GUILayout.Button("🔄 Refresh", GUILayout.Width(70)))
					{
					RefreshConsoleCapture();
					}
				}

			if (GUILayout.Button("🧹 Clear", GUILayout.Width(60)))
				{
				_capturedLogs.Clear();
				_selectedLogIndex = -1;
				_selectedLogEntry = "";
				}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5);

			// Recent logs section with scrollview
			EditorGUILayout.LabelField($"📝 Console Logs ({_capturedLogs.Count}):", EditorStyles.boldLabel);

			// Logs scrollview
			_logsScrollPosition = EditorGUILayout.BeginScrollView(_logsScrollPosition, GUILayout.Height(200));

			if (_capturedLogs.Count > 0)
				{
				for (int i = _capturedLogs.Count - 1; i >= 0; i--) // Show newest first
					{
					LogEntry logEntry = _capturedLogs [ i ];

					EditorGUILayout.BeginHorizontal();

					// Selection button
					bool isSelected = _selectedLogIndex == i;
					if (GUILayout.Button(isSelected ? "✅" : "⭕", GUILayout.Width(25)))
						{
						_selectedLogIndex = i;
						UpdateSelectedLogFromIndex();
						}

					// Log type icon
					string typeIcon = logEntry.type switch
						{
							LogType.Error => "❌",
							LogType.Warning => "⚠️",
							LogType.Log => "📝",
							LogType.Exception => "💥",
							_ => "🔸"
							};

					GUILayout.Label(typeIcon, GUILayout.Width(20));
					GUILayout.Label($"[{logEntry.timestamp}]", EditorStyles.miniLabel, GUILayout.Width(60));

					// Check if this log has commentary
					bool hasCommentary = _commentaries.Any(c => c.logIndex == i);
					if (hasCommentary)
						{
						GUILayout.Label("💬", GUILayout.Width(20));
						}
					else
						{
						GUILayout.Space(20);
						}

					// Truncated message
					string truncated = logEntry.message.Length > 60
						? logEntry.message [ ..60 ] + "..."
						: logEntry.message;

					GUIStyle style = hasCommentary ? EditorStyles.boldLabel : EditorStyles.miniLabel;
					GUILayout.Label(truncated, style);
					EditorGUILayout.EndHorizontal();
					}
				}
			else
				{
				EditorGUILayout.HelpBox("No logs captured yet. Enable capture and trigger some Unity console output.", MessageType.Info);
				}

			EditorGUILayout.EndScrollView();

			GUILayout.Space(10);

			// Commentary section
			EditorGUILayout.LabelField("💬 Add Commentary:", EditorStyles.boldLabel);

			EditorGUILayout.LabelField("Selected Log Entry:", EditorStyles.label);
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			_selectedLogEntry = EditorGUILayout.TextArea(_selectedLogEntry, GUILayout.Height(60));
			EditorGUILayout.EndVertical();

			_selectedLogType = (LogType)EditorGUILayout.EnumPopup("Log Type:", _selectedLogType);

			EditorGUILayout.LabelField("Your Commentary:", EditorStyles.label);
			_newComment = EditorGUILayout.TextArea(_newComment, GUILayout.Height(80));

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("💬 Add Commentary", GUILayout.Height(30)))
				{
				AddCommentary();
				}

			if (GUILayout.Button("🏷️ Tag as 'FUCK Moment'", GUILayout.Height(30)))
				{
				AddCommentary("FUCK-Moment,Learning-Opportunity");
				}

			if (GUILayout.Button("✅ Tag as 'Achievement'", GUILayout.Height(30)))
				{
				AddCommentary("Achievement,Success");
				}

			EditorGUILayout.EndHorizontal();

			GUILayout.Space(15);

			// Export section
			EditorGUILayout.LabelField("📊 Export Session:", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("📂 Export Raw Logs", GUILayout.Height(25)))
				{
				ExportRawLogs();
				}
			if (GUILayout.Button("📜 Complete TLDL Session", GUILayout.Height(25)))
				{
				CompleteTLDLSession();
				}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5);

			// Code snapshot section - Jerry's BRILLIANT idea!
			EditorGUILayout.LabelField("📸 Code Snapshot Tools:", EditorStyles.boldLabel);
			
			// Jerry's GENIUS adjustable range controls
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Snapshot Range:", GUILayout.Width(100));
			_snapshotLinesBefore = EditorGUILayout.IntSlider("Before", _snapshotLinesBefore, 1, 50, GUILayout.Width(120));
			_snapshotLinesAfter = EditorGUILayout.IntSlider("After", _snapshotLinesAfter, 1, 50, GUILayout.Width(120));
			
			// Quick presets for common scenarios
			if (GUILayout.Button("📏 Tight (3)", GUILayout.Width(80)))
			{
				_snapshotLinesBefore = _snapshotLinesAfter = 3;
			}
			if (GUILayout.Button("📐 Standard (10)", GUILayout.Width(90)))
			{
				_snapshotLinesBefore = _snapshotLinesAfter = 10;
			}
			if (GUILayout.Button("📊 Wide (25)", GUILayout.Width(80)))
			{
				_snapshotLinesBefore = _snapshotLinesAfter = 25;
			}
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField($"Total capture: {_snapshotLinesBefore + 1 + _snapshotLinesAfter} lines", EditorStyles.miniLabel);
			GUILayout.FlexibleSpace();
			
			if (GUILayout.Button("🎯 Capture Current Editor Line", GUILayout.Height(25)))
			{
				CaptureCurrentEditorLine();
			}
			if (GUILayout.Button("📸 Snapshot Script + Line", GUILayout.Height(25)))
			{
				ShowScriptLineSnapshotDialog();
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10);

			// Commentary history section (condensed)
			EditorGUILayout.LabelField($"📚 Session Commentary ({_commentaries.Count}):", EditorStyles.boldLabel);

			if (GUILayout.Button("🧹 Clear Session Commentary"))
				{
				ClearCommentaries();
				}

			GUILayout.Space(5);

			// Commentary list (condensed view)
			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(150));

			for (int i = _commentaries.Count - 1; i >= 0; i--) // Show newest first
				{
				CommentaryEntry entry = _commentaries [ i ];

				EditorGUILayout.BeginVertical(EditorStyles.helpBox);

				// Header with timestamp and type
				EditorGUILayout.BeginHorizontal();
				GUILayout.Label($"🕒 {entry.timestamp [ 11.. ]}", EditorStyles.miniLabel, GUILayout.Width(60)); // Show only time

				string typeIcon = entry.logType switch
					{
						LogType.Error => "❌",
						LogType.Warning => "⚠️",
						LogType.Log => "📝",
						LogType.Exception => "💥",
						_ => "🔸"
						};

				GUILayout.Label($"{typeIcon}", GUILayout.Width(20));

				// Tags
				if (!string.IsNullOrEmpty(entry.tags))
					{
					GUILayout.Label($"🏷️{entry.tags}", EditorStyles.miniLabel, GUILayout.Width(100));
					}

				GUILayout.FlexibleSpace();

				if (GUILayout.Button("🗑️", GUILayout.Width(25)))
					{
					_commentaries.RemoveAt(i);
					SaveCommentaries();
					continue;
					}
				EditorGUILayout.EndHorizontal();

				// Commentary (single line)
				string truncatedComment = entry.developerComment.Length > 80
					? entry.developerComment [ ..80 ] + "..."
					: entry.developerComment;
				EditorGUILayout.LabelField(truncatedComment, EditorStyles.wordWrappedMiniLabel);

				EditorGUILayout.EndVertical();
				GUILayout.Space(2);
				}

			EditorGUILayout.EndScrollView();
			}

		private void AddCommentary(string tags = "")
			{
			if (string.IsNullOrWhiteSpace(_selectedLogEntry) || string.IsNullOrWhiteSpace(_newComment))
				{
				EditorUtility.DisplayDialog("Invalid Input", "Please select a log entry and add your commentary.", "OK");
				return;
				}

			// Get stack trace if available
			string stackTrace = "";
			if (_selectedLogIndex >= 0 && _selectedLogIndex < _capturedLogs.Count)
				{
				stackTrace = _capturedLogs [ _selectedLogIndex ].stackTrace;
				}

			var entry = new CommentaryEntry(_selectedLogEntry, _selectedLogType, _newComment, "", tags, stackTrace, _selectedLogIndex);
			_commentaries.Add(entry);

			SaveCommentaries();

			// Clear for next entry
			_newComment = "";

			// DO NOT add to Unity console - just repaint to show the commentary icon
			Repaint();
			}

		private void ExportRawLogs()
			{
			try
				{
				string logPath = Path.Combine(Application.dataPath, "debug", $"unity-console-logs-{_sessionName}-{DateTime.Now:yyyy-MM-dd-HHmmss}.txt");
				Directory.CreateDirectory(Path.GetDirectoryName(logPath));

				string content = $"Unity Console Log Export - {_sessionName}\n";
				content += $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
				content += $"Total Entries: {_capturedLogs.Count}\n";
				content += new string('=', 50) + "\n\n";

				foreach (LogEntry log in _capturedLogs)
					{
					content += $"[{log.timestamp}] {log.type}: {log.message}\n";
					if (!string.IsNullOrEmpty(log.stackTrace))
						{
						content += $"Stack Trace:\n{log.stackTrace}\n";
						}
					content += new string('-', 30) + "\n";
					}

				File.WriteAllText(logPath, content);

				EditorUtility.DisplayDialog("Export Complete", $"Console logs exported to:\n{logPath}", "OK");
				}
			catch (Exception ex)
				{
				Debug.LogError($"Failed to export raw logs: {ex.Message}");
				}
			}

		private void CompleteTLDLSession()
			{
			try
				{
				// Create TLDL directory if it doesn't exist
				string tldlDir = Path.Combine(Application.dataPath, "..", "docs", "tldl-sessions");
				Directory.CreateDirectory(tldlDir);

				string tldlPath = Path.Combine(tldlDir, $"TLDL-{DateTime.Now:yyyy-MM-dd}-{_sessionName}-ConsoleCommentary.md");

				string content = GenerateCompleteTLDLContent();

				File.WriteAllText(tldlPath, content);

				// Also export raw logs as supporting artifact
				string logPath = Path.Combine(Path.GetDirectoryName(tldlPath), $"console-logs-{_sessionName}-{DateTime.Now:yyyy-MM-dd-HHmmss}.txt");
				ExportRawLogsToPath(logPath);

				EditorUtility.DisplayDialog("TLDL Session Complete",
					$"Session documentation exported to:\n{tldlPath}\n\nRaw logs saved to:\n{logPath}", "OK");

				// Clear session after export
				if (EditorUtility.DisplayDialog("Clear Session", "Session exported successfully. Clear current session to start fresh?", "Clear", "Keep"))
					{
					_capturedLogs.Clear();
					_commentaries.Clear();
					_selectedLogIndex = -1;
					_selectedLogEntry = "";
					_sessionName = $"Session-{DateTime.Now:HHmmss}";
					SaveCommentaries();
					}
				}
			catch (Exception ex)
				{
				Debug.LogError($"Failed to complete TLDL session: {ex.Message}");
				}
			}

		private void ExportRawLogsToPath(string logPath)
			{
			string content = $"Unity Console Log Export - {_sessionName}\n";
			content += $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
			content += $"Total Entries: {_capturedLogs.Count}\n";
			content += new string('=', 50) + "\n\n";

			foreach (LogEntry log in _capturedLogs)
				{
				content += $"[{log.timestamp}] {log.type}: {log.message}\n";
				if (!string.IsNullOrEmpty(log.stackTrace))
					{
					content += $"Stack Trace:\n{log.stackTrace}\n";
					}
				content += new string('-', 30) + "\n";
				}

			File.WriteAllText(logPath, content);
			}

		private void LoadCommentaries()
			{
			try
				{
				if (File.Exists(_commentaryFilePath))
					{
					string json = File.ReadAllText(_commentaryFilePath);
					CommentaryWrapper wrapper = JsonUtility.FromJson<CommentaryWrapper>(json);
					_commentaries = wrapper?.commentaries ?? new List<CommentaryEntry>();
					}
				}
			catch (Exception ex)
				{
				Debug.LogWarning($"Failed to load commentary history: {ex.Message}");
				_commentaries = new List<CommentaryEntry>();
				}
			}

		private void SaveCommentaries()
			{
			try
				{
				Directory.CreateDirectory(Path.GetDirectoryName(_commentaryFilePath));
				var wrapper = new CommentaryWrapper { commentaries = _commentaries };
				string json = JsonUtility.ToJson(wrapper, true);
				File.WriteAllText(_commentaryFilePath, json);
				}
			catch (Exception ex)
				{
				Debug.LogError($"Failed to save commentary history: {ex.Message}");
				}
			}

		private string GenerateCompleteTLDLContent()
			{
			string content = $"# Console Commentary Session: {_sessionName}\n\n";
			content += $"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}  \n";
			content += $"**Author:** Jerry Meyer (Console Commentary System)  \n";
			content += $"**Session ID:** {_sessionName}  \n";
			content += $"**Context:** MetVanDAMN Development Session  \n";
			content += $"**Summary:** Debug session with {_commentaries.Count} annotated console events from {_capturedLogs.Count} total logs\n\n";

			// Generate summary statistics
			int errorCount = _commentaries.Count(c => c.logType == LogType.Error);
			int warningCount = _commentaries.Count(c => c.logType == LogType.Warning);
			int fuckMoments = _commentaries.Count(c => c.tags.Contains("FUCK-Moment"));
			int achievements = _commentaries.Count(c => c.tags.Contains("Achievement"));

			content += "## 📊 Session Statistics\n\n";
			content += $"- **Total Console Logs Captured:** {_capturedLogs.Count}\n";
			content += $"- **Logs with Commentary:** {_commentaries.Count}\n";
			content += $"- **Errors Analyzed:** {errorCount}\n";
			content += $"- **Warnings Analyzed:** {warningCount}\n";
			content += $"- **FUCK Moments:** {fuckMoments}\n";
			content += $"- **Achievements:** {achievements}\n";
			content += $"- **Session Duration:** Started when window opened, completed at {DateTime.Now:HH:mm:ss}\n\n";

			// Log overview section
			content += "## 📝 Console Log Overview\n\n";
			content += "### Recent Activity Summary\n";

			var logTypeCounts = _capturedLogs.GroupBy(l => l.type).ToDictionary(g => g.Key, g => g.Count());
			foreach (KeyValuePair<LogType, int> kvp in logTypeCounts)
				{
				string icon = kvp.Key switch
					{
						LogType.Error => "❌",
						LogType.Warning => "⚠️",
						LogType.Log => "📝",
						LogType.Exception => "💥",
						_ => "🔸"
						};
				content += $"- {icon} **{kvp.Key}:** {kvp.Value} entries\n";
				}
			content += "\n";

			if (_commentaries.Count > 0)
				{
				content += "## 🗨️ Developer Commentary Log\n\n";

				foreach (CommentaryEntry entry in _commentaries.OrderBy(c => c.timestamp))
					{
					content += $"### {entry.timestamp} - {entry.logType}\n\n";

					if (!string.IsNullOrEmpty(entry.tags))
						{
						content += $"**Tags:** `{entry.tags}`\n\n";
						}

					content += $"**Original Log:**\n```\n{entry.logMessage}\n```\n\n";
					content += $"**Developer Commentary:**\n{entry.developerComment}\n\n";

					if (!string.IsNullOrEmpty(entry.stackTrace))
						{
						content += $"**Stack Trace:**\n```\n{entry.stackTrace}\n```\n\n";
						}

					content += "---\n\n";
					}
				}

			// Related artifacts section
			content += "## 📂 Related Artifacts\n\n";
			content += $"- **Raw Console Logs:** `console-logs-{_sessionName}-{DateTime.Now:yyyy-MM-dd-HHmmss}.txt`\n";
			content += $"- **Session Commentary:** Saved in commentary system for reference\n";
			content += $"- **Capture Duration:** Full session from window open to export\n\n";

			// Lessons learned section
			if (_commentaries.Count > 0)
				{
				content += "## 🧠 Key Insights\n\n";

				if (fuckMoments > 0)
					{
					content += $"**{fuckMoments} FUCK Moment(s) Documented:** These represent learning opportunities and debugging challenges that provide valuable context for future development.\n\n";
					}

				if (achievements > 0)
					{
					content += $"**{achievements} Achievement(s) Celebrated:** Successful implementations and breakthroughs worth preserving as reference material.\n\n";
					}

				content += "**Next Steps:** Review commentary for patterns, document solutions, and preserve insights for future debugging sessions.\n\n";
				}

			return content;
			}

		private void ClearCommentaries()
			{
			if (EditorUtility.DisplayDialog("Clear Session Commentary", "Are you sure you want to clear all commentary entries for this session?", "Clear", "Cancel"))
				{
				_commentaries.Clear();
				SaveCommentaries();
				}
			}

		private void CaptureCurrentEditorLine()
		{
			try
			{
				// Get the current active script editor
				var editorWindow = EditorWindow.focusedWindow;
				if (editorWindow != null && editorWindow.GetType().Name == "ScriptEditorWindow")
				{
					// Use reflection to get current script and line
					var scriptField = editorWindow.GetType().GetField("m_CurrentScript", BindingFlags.NonPublic | BindingFlags.Instance);
					var lineField = editorWindow.GetType().GetField("m_CurrentLine", BindingFlags.NonPublic | BindingFlags.Instance);
					
					if (scriptField != null && lineField != null)
					{
						var script = scriptField.GetValue(editorWindow);
						var line = lineField.GetValue(editorWindow);
						
						if (script != null && line != null)
						{
							var scriptPath = AssetDatabase.GetAssetPath((UnityEngine.Object)script);
							var lineNumber = (int)line;
							
							CaptureCodeSnapshot(scriptPath, lineNumber, "Current editor cursor position");
							return;
						}
					}
				}
				
				// Fallback: Show manual input dialog
				ShowScriptLineSnapshotDialog();
				
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"Could not capture current editor line automatically: {ex.Message}");
				ShowScriptLineSnapshotDialog();
			}
		}

		private void ShowScriptLineSnapshotDialog()
		{
			// Simple input dialog for script path and line number
			string scriptPath = EditorUtility.OpenFilePanel("Select Script File", Application.dataPath, "cs");
			if (!string.IsNullOrEmpty(scriptPath))
			{
				// Convert absolute path to relative if within project
				if (scriptPath.StartsWith(Application.dataPath))
				{
					scriptPath = "Assets" + scriptPath[Application.dataPath.Length..];
				}
				
				// Get line number from user - simplified approach for now
				int lineNumber = 1;
				
				// For now, just use line 1 - this could be enhanced with a proper input field later
				CaptureCodeSnapshot(scriptPath, lineNumber, "Manual script selection - line 1 (enhance this dialog for custom line input)");
			}
		}

		private void CaptureCodeSnapshot(string scriptPath, int targetLine, string context)
		{
			try
			{
				if (!File.Exists(scriptPath))
				{
					Debug.LogError($"📸 Code Snapshot: File not found - {scriptPath}");
					return;
				}
				
				string[] allLines = File.ReadAllLines(scriptPath);
				
				if (targetLine < 1 || targetLine > allLines.Length)
				{
					Debug.LogError($"📸 Code Snapshot: Line {targetLine} out of range (1-{allLines.Length}) in {Path.GetFileName(scriptPath)}");
					return;
				}
				
				// Jerry's GENIUS: Use adjustable range instead of fixed 21-line window
				int startLine = Math.Max(1, targetLine - _snapshotLinesBefore);
				int endLine = Math.Min(allLines.Length, targetLine + _snapshotLinesAfter);
				int totalLines = endLine - startLine + 1;
				
				// Build the snapshot
				var snapshot = new System.Text.StringBuilder();
				snapshot.AppendLine($"📸 **Code Snapshot: {Path.GetFileName(scriptPath)}:{targetLine}**");
				snapshot.AppendLine($"**Context:** {context}");
				snapshot.AppendLine($"**Captured:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
				snapshot.AppendLine($"**Range:** Lines {startLine}-{endLine} (showing {totalLines} of {allLines.Length} total)");
				snapshot.AppendLine($"**Window:** {_snapshotLinesBefore} before + target + {_snapshotLinesAfter} after");
				snapshot.AppendLine();
				snapshot.AppendLine("```csharp");
				
				for (int i = startLine; i <= endLine; i++)
				{
					string linePrefix = i == targetLine ? ">>> " : "    ";
					string lineNumber = i.ToString().PadLeft(3);
					string lineContent = i <= allLines.Length ? allLines[i - 1] : "";
					
					snapshot.AppendLine($"{linePrefix}{lineNumber} | {lineContent}");
				}
				
				snapshot.AppendLine("```");
				snapshot.AppendLine();
				snapshot.AppendLine($"**Target Line {targetLine}:** `{(targetLine <= allLines.Length ? allLines[targetLine - 1].Trim() : "")}`");
				
				// Add configuration details for future reference
				snapshot.AppendLine();
				snapshot.AppendLine($"**Capture Settings:** {_snapshotLinesBefore} lines before, {_snapshotLinesAfter} lines after (total window: {totalLines} lines)");
				
				// Add to commentary as a special code snapshot entry
				var snapshotEntry = new CommentaryEntry(
					$"Code Snapshot: {Path.GetFileName(scriptPath)}:{targetLine}", 
					LogType.Log, 
					snapshot.ToString(), 
					scriptPath, 
					$"Code-Snapshot,TLDL-Reference,Range-{_snapshotLinesBefore}+{_snapshotLinesAfter}", 
					"", 
					-1
				);
				
				_commentaries.Add(snapshotEntry);
				SaveCommentaries();
				
				Debug.Log($"📸 Code Snapshot captured: {Path.GetFileName(scriptPath)} line {targetLine} ({totalLines} lines: {_snapshotLinesBefore}+1+{_snapshotLinesAfter})");
				Repaint();
				
			}
			catch (Exception ex)
			{
				Debug.LogError($"📸 Code Snapshot failed: {ex.Message}");
			}
		}

        private void RefreshConsoleCapture()
        {
            try
            {
                // Attempt to access Unity's internal console log entries via reflection
                Type consoleWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
                if (consoleWindowType != null)
                {
                    // Try to get the static console entries
                    var logEntriesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
                    if (logEntriesType != null)
                    {
                        // Try to get count and entries
                        var getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
                        var getEntryMethod = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.NonPublic);
                        
                        if (getCountMethod != null && getEntryMethod != null)
                        {
                            int logCount = (int)getCountMethod.Invoke(null, null);
                            int startIndex = Math.Max(0, logCount - 50); // Get last 50 entries
                            
                            var logEntryType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntry");
                            if (logEntryType != null)
                            {
                                object logEntry = Activator.CreateInstance(logEntryType);
                                var messageField = logEntryType.GetField("message");
                                var modeField = logEntryType.GetField("mode");
                                
                                for (int i = startIndex; i < logCount; i++)
                                {
                                    if (getEntryMethod.Invoke(null, new object[] { i, logEntry }) is bool success && success)
                                    {
                                        if (messageField?.GetValue(logEntry) is string message &&
                                            modeField?.GetValue(logEntry) is int mode)
                                        {
                                            LogType logType = mode switch
                                            {
                                                0 => LogType.Error,
                                                1 => LogType.Assert,
                                                2 => LogType.Warning,
                                                3 => LogType.Log,
                                                4 => LogType.Exception,
                                                _ => LogType.Log
                                            };
                                            
                                            var newLogEntry = new LogEntry(message, logType, "");
                                            
                                            // Only add if not already captured and not our own commentary
                                            if (!newLogEntry.isCommentaryLog && !_capturedLogs.Any(l => l.message == message && l.type == logType))
                                            {
                                                _capturedLogs.Add(newLogEntry);
                                            }
                                        }
                                    }
                                }
                                
                                // Trim to our limit
                                while (_capturedLogs.Count > 200)
                                {
                                    _capturedLogs.RemoveAt(0);
                                    if (_selectedLogIndex >= 0) _selectedLogIndex--;
                                }
                                
                                // Auto-select most recent if none selected
                                if (_selectedLogIndex < 0 && _capturedLogs.Count > 0)
                                {
                                    _selectedLogIndex = _capturedLogs.Count - 1;
                                    UpdateSelectedLogFromIndex();
                                }
                                
                                Debug.Log($"🔄 Console Commentary: Refreshed {_capturedLogs.Count} log entries from Unity console");
                                Repaint();
                                return;
                            }
                        }
                    }
                }
                
                // Fallback: Show friendly message that manual refresh attempted
                Debug.Log("🔄 Console Commentary: Manual refresh attempted - Unity console API access limited. Enable auto-capture for real-time monitoring.");
                
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"🔄 Console Commentary: Manual refresh failed - {ex.Message}. Unity console reflection API may have changed.");
            }
        }

        [Serializable]
        private class CommentaryWrapper
        {
            public List<CommentaryEntry> commentaries;
        }
    }
}
#endif
