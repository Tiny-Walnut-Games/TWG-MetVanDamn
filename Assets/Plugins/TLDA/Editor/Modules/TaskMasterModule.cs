#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LivingDevAgent.Editor.Modules
	{
	/// <summary>
	/// ⏰ TaskMaster Module - Scene View Time Tracking Overlay
	/// Provides focused time tracking with scene view integration and ScriptableObject time-cards
	/// Timer runs in background using EditorApplication.timeSinceStartup for accuracy
	/// Time-cards can be imported to TLDL entries when tasks complete
	/// </summary>
	public class TaskMasterModule : ScribeModuleBase
		{
		// Timer state - persisted in ScriptableObject
		private TimeCardData _activeTimeCard;
		private bool _isTimerRunning = false;
		private double _sessionStartTime = 0.0; // EditorApplication.timeSinceStartup
		private double _accumulatedTime = 0.0; // Total session time in seconds

		// Task management
		private readonly List<string> _availableTasks = new()
		{
			"📝 Documentation Writing",
			"🐛 Bug Investigation",
			"⚡ Feature Development",
			"🧪 Testing & Validation",
			"🎨 UI/UX Design",
			"🔧 Code Refactoring",
			"📊 Research & Analysis",
			"🏗️ Architecture Planning",
			"💬 Code Review",
			"🎯 Custom Task..."
		};

		private int _selectedTaskIndex = 0;
		private string _customTaskName = "";
		private bool _showCustomTask = false;

		// Time-card management
		private readonly List<TimeCardData> _allTimeCards = new();
		private readonly string _timeCardDirectory = "Assets/TLDA/TimeCards/";

		// Scene view overlay state
		private static bool _moduleEnabled = true; // Module can be completely disabled
		private static bool _sceneViewOverlayEnabled = true; // Scene overlay works independently
		private static Rect _overlayRect = new(10, 10, 300, 120);
		private static bool _isDragging = false;
		private static Vector2 _dragOffset;

		// 🎯 NEW: Error tracking and validation
		private static bool _initializationError = false;
		private static string _lastError = "";

		public TaskMasterModule (TLDLScribeData data) : base(data) { }

		public override void Initialize ()
			{
			try
				{
				// Ensure time-card directory exists
				if (!System.IO.Directory.Exists(this._timeCardDirectory))
					{
					System.IO.Directory.CreateDirectory(this._timeCardDirectory);
					AssetDatabase.Refresh();
					}

				// Load existing time-cards
				this.LoadTimeCards();

				// Restore active timer state if exists
				this.RestoreActiveTimerState();

				// Subscribe to editor updates for background tracking
				EditorApplication.update += this.OnEditorUpdate;

				// 🎯 ALWAYS register scene view overlay (independent of module state)
				SceneView.duringSceneGui += this.OnSceneGUI;

				_initializationError = false;
				this.SetStatus("⏰ TaskMaster initialized successfully");
				}
			catch (System.Exception ex)
				{
				_initializationError = true;
				_lastError = ex.Message;
				this.SetStatus($"❌ TaskMaster initialization failed: {ex.Message}");
				Debug.LogError($"TaskMaster initialization error: {ex}");
				}
			}

		public override void Dispose ()
			{
			try
				{
				// Save any active session before disposing
				if (this._isTimerRunning && this._activeTimeCard != null)
					{
					this.SaveCurrentSession();
					}

				// Unsubscribe from updates
				EditorApplication.update -= this.OnEditorUpdate;

				// 🎯 ALWAYS unregister scene view overlay
				SceneView.duringSceneGui -= this.OnSceneGUI;

				this.SetStatus("⏰ TaskMaster disposed cleanly");
				}
			catch (System.Exception ex)
				{
				Debug.LogError($"TaskMaster disposal error: {ex}");
				}
			}

		// 🎯 FIXED: Scene View Overlay Rendering with proper event handling
		private void OnSceneGUI (SceneView sceneView)
			{
			if (!_sceneViewOverlayEnabled) return;

			// Begin GUI overlay
			Handles.BeginGUI();

			// Background panel
			GUI.Box(_overlayRect, "", EditorStyles.helpBox);

			// Make panel draggable
			this.HandlePanelDragging();

			// 🎯 CRITICAL FIX: Ensure proper GUI event handling
			Event currentEvent = Event.current;

			// Draw timer content inside the panel with proper event scope
			GUILayout.BeginArea(new Rect(_overlayRect.x + 5, _overlayRect.y + 5,
									   _overlayRect.width - 10, _overlayRect.height - 10));

			try
				{
				this.DrawSceneViewTimerContent();
				}
			catch (System.Exception ex)
				{
				// Prevent scene view from breaking on GUI errors
				Debug.LogError($"Scene view timer error: {ex.Message}");
				}

			GUILayout.EndArea();

			Handles.EndGUI();

			// Force repaint if timer is running for live updates
			if (this._isTimerRunning)
				{
				sceneView.Repaint();
				}
			}

		private void DrawSceneViewTimerContent ()
			{
			// 🎯 CRITICAL: Force immediate layout to prevent GUI ID conflicts
			if (Event.current.type == EventType.Layout)
				{
				GUIUtility.GetControlID(FocusType.Passive);
				}

			// Header with timer display
			using (new GUILayout.HorizontalScope())
				{
				string timerText = this.FormatCurrentTime();
				GUIStyle timerStyle = new(EditorStyles.boldLabel)
					{
					normal = { textColor = this._isTimerRunning ? Color.green : Color.gray },
					fontSize = 14
					};

				GUILayout.Label($"⏰ {timerText}", timerStyle);

				GUILayout.FlexibleSpace();

				// Toggle overlay visibility with proper event handling
				if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(20)))
					{
					_sceneViewOverlayEnabled = false;
					Event.current.Use(); // Consume the event
					SceneView.RepaintAll();
					}
				}

			// Current task display
			if (this._isTimerRunning && this._activeTimeCard != null)
				{
				TaskData task = this._activeTimeCard.GetTask();
				string taskDisplay = task != null ? task.TaskName : "Unnamed Task";
				GUILayout.Label($"📋 {taskDisplay}", EditorStyles.miniLabel);
				}

			// Task selection dropdown with proper event handling
			GUILayout.Space(2);
			EditorGUI.BeginChangeCheck();
			int newTaskIndex = EditorGUILayout.Popup(this._selectedTaskIndex, this._availableTasks.ToArray(), EditorStyles.popup);
			if (EditorGUI.EndChangeCheck())
				{
				this._selectedTaskIndex = newTaskIndex;
				this._showCustomTask = (this._availableTasks [ this._selectedTaskIndex ] == "🎯 Custom Task...");
				Event.current.Use(); // Consume the event
				SceneView.RepaintAll();
				}

			// Custom task field with proper event handling
			if (this._showCustomTask)
				{
				EditorGUI.BeginChangeCheck();
				string newCustomTaskName = EditorGUILayout.TextField(this._customTaskName, EditorStyles.textField);
				if (EditorGUI.EndChangeCheck())
					{
					this._customTaskName = newCustomTaskName;
					Event.current.Use(); // Consume the event
					}
				}

			GUILayout.Space(3);

			// Clock In/Out controls with proper event handling
			using (new GUILayout.HorizontalScope())
				{
				string buttonText = this._isTimerRunning ? "⏸️ Clock Out" : "▶️ Clock In";
				Color buttonColor = this._isTimerRunning ? Color.red : Color.green;

				Color originalColor = GUI.backgroundColor;
				GUI.backgroundColor = buttonColor;

				if (GUILayout.Button(buttonText, GUILayout.Height(25)))
					{
					try
						{
						if (this._isTimerRunning)
							{
							this.ClockOut();
							}
						else
							{
							this.ClockIn();
							}
						Event.current.Use(); // Consume the event
						SceneView.RepaintAll();
						}
					catch (System.Exception ex)
						{
						Debug.LogError($"Timer button error: {ex.Message}");
						}
					}

				GUI.backgroundColor = originalColor;

				// Quick save button with proper event handling
				using (new EditorGUI.DisabledScope(!this._isTimerRunning))
					{
					if (GUILayout.Button("💾", EditorStyles.miniButton, GUILayout.Width(25)))
						{
						try
							{
							this.SaveCurrentSession();
							Event.current.Use(); // Consume the event
							SceneView.RepaintAll();
							}
						catch (System.Exception ex)
							{
							Debug.LogError($"Save session error: {ex.Message}");
							}
						}
					}

				// Open TaskMaster panel button with proper event handling
				if (GUILayout.Button("📊", EditorStyles.miniButton, GUILayout.Width(25)))
					{
					try
						{
						TLDLScribeWindow.ShowWindow();
						// TODO: Switch to TaskMaster tab programmatically
						Event.current.Use(); // Consume the event
						}
					catch (System.Exception ex)
						{
						Debug.LogError($"Open window error: {ex.Message}");
						}
					}
				}
			}

		private void HandlePanelDragging ()
			{
			Event e = Event.current;

			if (e.type == EventType.MouseDown && _overlayRect.Contains(e.mousePosition))
				{
				_isDragging = true;
				_dragOffset = e.mousePosition - new Vector2(_overlayRect.x, _overlayRect.y);
				e.Use();
				}
			else if (e.type == EventType.MouseDrag && _isDragging)
				{
				_overlayRect.position = e.mousePosition - _dragOffset;
				e.Use();
				}
			else if (e.type == EventType.MouseUp)
				{
				_isDragging = false;
				}
			}

		// 🎯 SCRIBE WINDOW: Time-card management panel only
		public void DrawPanel ()
			{
			// If module is disabled, take ZERO space in the GUI
			if (!_moduleEnabled)
				{
				return; // Component is "off" - no GUI space consumed
				}

			this.DrawPanel(new Rect(0, 0, 300, 400)); // Default size
			}

		public void DrawPanel (Rect rect)
			{
			// If module is disabled, take ZERO space in the GUI
			if (!_moduleEnabled)
				{
				return; // Component is "off" - no GUI space consumed
				}

			// Show error state if initialization failed
			if (_initializationError)
				{
				using (new GUI.GroupScope(rect))
					{
					EditorGUILayout.HelpBox($"⚠️ TaskMaster Error: {_lastError}", MessageType.Error);
					if (GUILayout.Button("🔄 Retry Initialization"))
						{
						this.Initialize(); // Attempt to reinitialize
						}
					}
				return;
				}

			// 🎯 FIXED: Use proper scroll view within allocated rect to prevent squishing
			using (new EditorGUILayout.VerticalScope())
				{
				// Allocate the full rect height for content
				Rect contentRect = EditorGUILayout.GetControlRect(false, rect.height, GUILayout.ExpandHeight(true));

				// Create a scroll view within the allocated space
				using var scrollScope = new EditorGUILayout.ScrollViewScope(Vector2.zero, GUILayout.Height(rect.height));
				this.DrawPanelContent();
				}
			}

		private void DrawPanelContent ()
			{
			// Module enable/disable toggle at the top
			using (new EditorGUILayout.HorizontalScope())
				{
				EditorGUILayout.LabelField("⏰ TaskMaster", EditorStyles.boldLabel);

				GUILayout.FlexibleSpace();

				bool newModuleEnabled = EditorGUILayout.Toggle("Module Enabled:", _moduleEnabled);
				if (newModuleEnabled != _moduleEnabled)
					{
					_moduleEnabled = newModuleEnabled;
					if (!_moduleEnabled)
						{
						this.SetStatus("⏰ TaskMaster module disabled (taking zero GUI space)");
						}
					else
						{
						this.SetStatus("⏰ TaskMaster module enabled");
						}
					}
				}

			// Only show full content if module is enabled
			if (!_moduleEnabled)
				{
				EditorGUILayout.HelpBox("TaskMaster module is disabled. Scene view timer still works independently.", MessageType.Info);
				return;
				}

			EditorGUILayout.LabelField("Time-Card Management", EditorStyles.boldLabel);

			// Scene view overlay toggle (independent of module state)
			using (new EditorGUILayout.HorizontalScope())
				{
				bool newOverlayEnabled = EditorGUILayout.Toggle("Scene View Timer:", _sceneViewOverlayEnabled);
				if (newOverlayEnabled != _sceneViewOverlayEnabled)
					{
					_sceneViewOverlayEnabled = newOverlayEnabled;
					SceneView.RepaintAll();
					}

				if (GUILayout.Button("Reset Position", GUILayout.Width(100)))
					{
					_overlayRect = new Rect(10, 10, 300, 120);
					SceneView.RepaintAll();
					}
				}

			EditorGUILayout.Space();

			// Current session info (read-only display)
			using (new EditorGUILayout.VerticalScope("box"))
				{
				EditorGUILayout.LabelField("Current Session", EditorStyles.boldLabel);

				if (this._isTimerRunning && this._activeTimeCard != null)
					{
					TaskData task = this._activeTimeCard.GetTask();
					string taskName = task != null ? task.TaskName : "Unnamed Task";
					EditorGUILayout.LabelField($"Task: {taskName}");
					EditorGUILayout.LabelField($"Time: {this.FormatCurrentTime()}");
					EditorGUILayout.LabelField($"Started: {this._activeTimeCard.GetStartTime():HH:mm:ss}");
					}
				else
					{
					EditorGUILayout.LabelField("No active session");
					}
				}

			EditorGUILayout.Space();

			// Time-card management
			this.DrawTimeCardManagement();
			}

		private void DrawTimeCardManagement ()
			{
			EditorGUILayout.LabelField("📊 Time-Card Management", EditorStyles.boldLabel);

			// Time-card list
			if (this._allTimeCards.Count > 0)
				{
				using var scroll = new EditorGUILayout.ScrollViewScope(Vector2.zero, GUILayout.Height(150));
				foreach (TimeCardData timeCard in this._allTimeCards.OrderByDescending(tc => tc.GetLastModified()))
					{
					this.DrawTimeCardEntry(timeCard);
					}
				}
			else
				{
				EditorGUILayout.HelpBox("No time-cards found. Use the scene view timer to create your first time-card!", MessageType.Info);
				}

			EditorGUILayout.Space();

			// Management buttons
			using (new EditorGUILayout.HorizontalScope())
				{
				if (GUILayout.Button("🔄 Refresh Time-Cards"))
					{
					this.LoadTimeCards();
					}

				if (GUILayout.Button("📂 Open Time-Card Folder"))
					{
					EditorUtility.RevealInFinder(this._timeCardDirectory);
					}

				if (GUILayout.Button("📋 Import to TLDL"))
					{
					this.ShowTimeCardImportDialog();
					}
				}
			}

		private void ClockIn ()
			{
			string taskName = this.GetCurrentTaskName();
			if (string.IsNullOrEmpty(taskName))
				{
				EditorUtility.DisplayDialog("No Task Selected", "Please select or enter a task name before clocking in.", "OK");
				return;
				}

			// Create or resume time-card
			this._activeTimeCard = this.GetOrCreateTimeCard(taskName);
			this._isTimerRunning = true;
			this._sessionStartTime = EditorApplication.timeSinceStartup;
			this._accumulatedTime = 0.0;

			// Create TaskData for this session
			var taskData = TaskData.CreateTask(taskName, $"Work session on {taskName}", "@copilot", 1);

			// Start the time-card with proper API
			this._activeTimeCard.StartTimeCard(taskData);
			EditorUtility.SetDirty(this._activeTimeCard);

			this.SetStatus($"⏰ Clocked in: {taskName}");
			SceneView.RepaintAll(); // Update scene view overlay
			}

		private void ClockOut ()
			{
			if (this._activeTimeCard == null) return;

			// Calculate final session time
			this.UpdateAccumulatedTime();

			// End the time-card session properly
			this._activeTimeCard.EndTimeCard();
			EditorUtility.SetDirty(this._activeTimeCard);
			AssetDatabase.SaveAssets();

			TaskData task = this._activeTimeCard.GetTask();
			string taskName = task != null ? task.TaskName : "Unknown Task";

			this.SetStatus($"⏰ Clocked out: {this.FormatDuration(this._accumulatedTime)} on {taskName}");

			this._isTimerRunning = false;
			this._activeTimeCard = null;
			SceneView.RepaintAll(); // Update scene view overlay
			}

		private void SaveCurrentSession ()
			{
			if (this._activeTimeCard == null || !this._isTimerRunning) return;

			this.UpdateAccumulatedTime();

			// For now, we'll end the current session and start a new one
			// This preserves the session as a complete record
			this._activeTimeCard.EndTimeCard();

			// Create a new session for continued work
			TaskData task = this._activeTimeCard.GetTask();
			if (task != null)
				{
				var newTaskData = TaskData.CreateTask(task.TaskName, $"Continued work on {task.TaskName}", "@copilot", 1);
				this._activeTimeCard.StartTimeCard(newTaskData);
				}

			EditorUtility.SetDirty(this._activeTimeCard);
			AssetDatabase.SaveAssets();

			// Reset for new session
			this._sessionStartTime = EditorApplication.timeSinceStartup;
			this._accumulatedTime = 0.0;

			string taskName = task != null ? task.TaskName : "Unknown Task";
			this.SetStatus($"💾 Session saved: {taskName}");
			}

		private void OnEditorUpdate ()
			{
			// Background timer update - runs regardless of window focus
			if (this._isTimerRunning && this._activeTimeCard != null)
				{
				this.UpdateAccumulatedTime();

				// Periodic save every 5 minutes for safety
				if (this._accumulatedTime > 0 && (this._accumulatedTime % 300) < 1.0) // Every 5 minutes
					{
					EditorUtility.SetDirty(this._activeTimeCard);
					}

				// Repaint scene views to update timer display
				SceneView.RepaintAll();
				}
			}

		private void UpdateAccumulatedTime ()
			{
			if (this._isTimerRunning)
				{
				double currentTime = EditorApplication.timeSinceStartup;
				double sessionTime = currentTime - this._sessionStartTime;
				this._accumulatedTime += sessionTime;
				this._sessionStartTime = currentTime;
				}
			}

		private string GetCurrentTaskName ()
			{
			return this._showCustomTask
				? string.IsNullOrEmpty(this._customTaskName) ? "" : this._customTaskName
				: this._availableTasks [ this._selectedTaskIndex ];
			}

		private TimeCardData GetOrCreateTimeCard (string taskName)
			{
			// Look for existing time-card with matching task name
			TimeCardData existing = this._allTimeCards.FirstOrDefault(tc =>
			{
				TaskData task = tc.GetTask();
				return task != null && task.TaskName == taskName;
			});

			if (existing != null)
				{
				return existing;
				}

			// Create new time-card ScriptableObject
			TimeCardData timeCard = ScriptableObject.CreateInstance<TimeCardData>();

			string fileName = $"TimeCard_{this.SanitizeFileName(taskName)}_{System.DateTime.Now:yyyyMMdd_HHmmss}.asset";
			string assetPath = System.IO.Path.Combine(this._timeCardDirectory, fileName);

			AssetDatabase.CreateAsset(timeCard, assetPath);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			this._allTimeCards.Add(timeCard);
			return timeCard;
			}

		private void LoadTimeCards ()
			{
			this._allTimeCards.Clear();

			if (!System.IO.Directory.Exists(this._timeCardDirectory)) return;

			string [ ] guids = AssetDatabase.FindAssets("t:TimeCardData", new [ ] { this._timeCardDirectory });
			foreach (string guid in guids)
				{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);
				TimeCardData timeCard = AssetDatabase.LoadAssetAtPath<TimeCardData>(assetPath);
				if (timeCard != null)
					{
					this._allTimeCards.Add(timeCard);
					}
				}
			}

		private void RestoreActiveTimerState ()
			{
			// Look for active time-card using proper getter
			TimeCardData activeCard = this._allTimeCards.FirstOrDefault(tc => tc.GetIsOngoing());
			if (activeCard != null)
				{
				this._activeTimeCard = activeCard;
				this._isTimerRunning = true;
				this._sessionStartTime = EditorApplication.timeSinceStartup; // Reset to current time
				this._accumulatedTime = 0.0; // Start fresh session

				TaskData task = activeCard.GetTask();
				string taskName = task != null ? task.TaskName : "Unknown Task";
				this.SetStatus($"⏰ Restored active timer: {taskName}");
				}
			}

		private void DrawTimeCardEntry (TimeCardData timeCard)
			{
			using (new EditorGUILayout.HorizontalScope("box"))
				{
				// Time-card info using proper getters
				TaskData task = timeCard.GetTask();
				string taskName = task != null ? task.TaskName : "Unknown Task";

				EditorGUILayout.LabelField($"📋 {taskName}", EditorStyles.boldLabel, GUILayout.Width(150));
				EditorGUILayout.LabelField($"⏱️ {timeCard.GetDurationInHours():F2}h", GUILayout.Width(80));
				EditorGUILayout.LabelField($"📅 {timeCard.GetLastModified():MM/dd HH:mm}", EditorStyles.miniLabel, GUILayout.Width(80));

				GUILayout.FlexibleSpace();

				// Actions
				if (GUILayout.Button("👁️", EditorStyles.miniButton, GUILayout.Width(25)))
					{
					Selection.activeObject = timeCard;
					EditorGUIUtility.PingObject(timeCard);
					}

				if (GUILayout.Button("📝", EditorStyles.miniButton, GUILayout.Width(25)))
					{
					this.ImportTimeCardToTLDL(timeCard);
					}

				if (GUILayout.Button("🗑️", EditorStyles.miniButton, GUILayout.Width(25)))
					{
					if (EditorUtility.DisplayDialog("Delete Time-Card",
						$"Delete time-card for '{taskName}'?", "Delete", "Cancel"))
						{
						this.DeleteTimeCard(timeCard);
						}
					}
				}
			}

		private void ImportTimeCardToTLDL (TimeCardData timeCard)
			{
			TaskData task = timeCard.GetTask();
			string taskName = task != null ? task.TaskName : "Unknown Task";

			// Add time summary to TLDL using report data
			string timeSummary = $"\n**Time Tracking - {taskName}:**\n";
			timeSummary += $"- Total Duration: {timeCard.GetDurationInHours():F2} hours\n";
			timeSummary += $"- Sessions: {timeCard.GetSessionCount()}\n";

			// Add detailed report if available
			string reportData = timeCard.GetReportData();
			if (!string.IsNullOrEmpty(reportData))
				{
				timeSummary += $"- Details:\n{reportData}\n";
				}

			this._data.TechnicalDetails += timeSummary;
			this._data.IncludeTechnicalDetails = true;

			this.SetStatus($"📝 Imported time-card to TLDL: {taskName}");
			}

		private void ShowTimeCardImportDialog ()
			{
			if (this._allTimeCards.Count == 0)
				{
				EditorUtility.DisplayDialog("No Time-Cards", "No time-cards available to import.", "OK");
				return;
				}

			var menu = new GenericMenu();
			foreach (TimeCardData timeCard in this._allTimeCards)
				{
				TaskData task = timeCard.GetTask();
				string taskName = task != null ? task.TaskName : "Unknown Task";
				string duration = $"{timeCard.GetDurationInHours():F2}h";

				menu.AddItem(new GUIContent($"{taskName} ({duration})"),
						   false, () => this.ImportTimeCardToTLDL(timeCard));
				}
			menu.ShowAsContext();
			}

		private void DeleteTimeCard (TimeCardData timeCard)
			{
			this._allTimeCards.Remove(timeCard);
			string assetPath = AssetDatabase.GetAssetPath(timeCard);
			AssetDatabase.DeleteAsset(assetPath);
			AssetDatabase.Refresh();

			TaskData task = timeCard.GetTask();
			string taskName = task != null ? task.TaskName : "Unknown Task";
			this.SetStatus($"🗑️ Deleted time-card: {taskName}");
			}

		private string FormatCurrentTime ()
			{
			if (this._isTimerRunning)
				{
				this.UpdateAccumulatedTime();
				return this.FormatDuration(this._accumulatedTime);
				}
			else
				{
				return "00:00:00";
				}
			}

		private string FormatDuration (double seconds)
			{
			var timeSpan = System.TimeSpan.FromSeconds(seconds);
			return $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
			}

		private string SanitizeFileName (string fileName)
			{
			char [ ] invalids = System.IO.Path.GetInvalidFileNameChars();
			return string.Join("_", fileName.Split(invalids, System.StringSplitOptions.RemoveEmptyEntries));
			}
		}
#endif
	}
