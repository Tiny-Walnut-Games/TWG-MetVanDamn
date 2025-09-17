#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using LivingDevAgent.Editor.Scribe;
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
		private static bool _sceneViewOverlayEnabled = false; // Scene overlay disabled by default for cleaner demo experience
		private static Rect _overlayRect = new(10, 10, 300, 120);
		private static bool _isDragging = false;
		private static Vector2 _dragOffset;

		// 🎯 NEW: Error tracking and validation
		private static bool _initializationError = false;
		private static string _lastError = "";

		public TaskMasterModule(TLDLScribeData data) : base(data) { }

		public override void Initialize()
			{
			try
				{
				// Ensure time-card directory exists
				if (!System.IO.Directory.Exists(_timeCardDirectory))
					{
					System.IO.Directory.CreateDirectory(_timeCardDirectory);
					AssetDatabase.Refresh();
					}

				// Load existing time-cards
				LoadTimeCards();

				// Restore active timer state if exists
				RestoreActiveTimerState();

				// Subscribe to editor updates for background tracking
				EditorApplication.update += OnEditorUpdate;

				// 🎯 ALWAYS register scene view overlay (independent of module state)
				SceneView.duringSceneGui += OnSceneGUI;

				_initializationError = false;
				SetStatus("⏰ TaskMaster initialized successfully");
				}
			catch (System.Exception ex)
				{
				_initializationError = true;
				_lastError = ex.Message;
				SetStatus($"❌ TaskMaster initialization failed: {ex.Message}");
				Debug.LogError($"TaskMaster initialization error: {ex}");
				}
			}

		public override void Dispose()
			{
			try
				{
				// Save any active session before disposing
				if (_isTimerRunning && _activeTimeCard != null)
					{
					SaveCurrentSession();
					}

				// Unsubscribe from updates
				EditorApplication.update -= OnEditorUpdate;

				// 🎯 ALWAYS unregister scene view overlay
				SceneView.duringSceneGui -= OnSceneGUI;

				SetStatus("⏰ TaskMaster disposed cleanly");
				}
			catch (System.Exception ex)
				{
				Debug.LogError($"TaskMaster disposal error: {ex}");
				}
			}

		// 🎯 FIXED: Scene View Overlay Rendering with proper event handling
		private void OnSceneGUI(SceneView sceneView)
			{
			if (!_sceneViewOverlayEnabled) return;

			// Begin GUI overlay
			Handles.BeginGUI();

			// Background panel
			GUI.Box(_overlayRect, "", EditorStyles.helpBox);

			// Make panel draggable
			HandlePanelDragging();

			// 🎯 CRITICAL FIX: Ensure proper GUI event handling
			Event currentEvent = Event.current;

			// Draw timer content inside the panel with proper event scope
			GUILayout.BeginArea(new Rect(_overlayRect.x + 5, _overlayRect.y + 5,
									   _overlayRect.width - 10, _overlayRect.height - 10));

			try
				{
				DrawSceneViewTimerContent();
				}
			catch (System.Exception ex)
				{
				// Prevent scene view from breaking on GUI errors
				Debug.LogError($"Scene view timer error: {ex.Message}");
				}

			GUILayout.EndArea();

			Handles.EndGUI();

			// Force repaint if timer is running for live updates
			if (_isTimerRunning)
				{
				sceneView.Repaint();
				}
			}

		private void DrawSceneViewTimerContent()
			{
			// 🎯 CRITICAL: Force immediate layout to prevent GUI ID conflicts
			if (Event.current.type == EventType.Layout)
				{
				GUIUtility.GetControlID(FocusType.Passive);
				}

			// Header with timer display
			using (new GUILayout.HorizontalScope())
				{
				string timerText = FormatCurrentTime();
				GUIStyle timerStyle = new(EditorStyles.boldLabel)
					{
					normal = { textColor = _isTimerRunning ? Color.green : Color.gray },
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
			if (_isTimerRunning && _activeTimeCard != null)
				{
				TaskData task = _activeTimeCard.GetTask();
				string taskDisplay = task != null ? task.TaskName : "Unnamed Task";
				GUILayout.Label($"📋 {taskDisplay}", EditorStyles.miniLabel);
				}

			// Task selection dropdown with proper event handling
			GUILayout.Space(2);
			EditorGUI.BeginChangeCheck();
			int newTaskIndex = EditorGUILayout.Popup(_selectedTaskIndex, _availableTasks.ToArray(), EditorStyles.popup);
			if (EditorGUI.EndChangeCheck())
				{
				_selectedTaskIndex = newTaskIndex;
				_showCustomTask = (_availableTasks[_selectedTaskIndex] == "🎯 Custom Task...");
				Event.current.Use(); // Consume the event
				SceneView.RepaintAll();
				}

			// Custom task field with proper event handling
			if (_showCustomTask)
				{
				EditorGUI.BeginChangeCheck();
				string newCustomTaskName = EditorGUILayout.TextField(_customTaskName, EditorStyles.textField);
				if (EditorGUI.EndChangeCheck())
					{
					_customTaskName = newCustomTaskName;
					Event.current.Use(); // Consume the event
					}
				}

			GUILayout.Space(3);

			// Clock In/Out controls with proper event handling
			using (new GUILayout.HorizontalScope())
				{
				string buttonText = _isTimerRunning ? "⏸️ Clock Out" : "▶️ Clock In";
				Color buttonColor = _isTimerRunning ? Color.red : Color.green;

				Color originalColor = GUI.backgroundColor;
				GUI.backgroundColor = buttonColor;

				if (GUILayout.Button(buttonText, GUILayout.Height(25)))
					{
					try
						{
						if (_isTimerRunning)
							{
							ClockOut();
							}
						else
							{
							ClockIn();
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
				using (new EditorGUI.DisabledScope(!_isTimerRunning))
					{
					if (GUILayout.Button("💾", EditorStyles.miniButton, GUILayout.Width(25)))
						{
						try
							{
							SaveCurrentSession();
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
						ScribeCore.ShowWindow();
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

		private void HandlePanelDragging()
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
		public void DrawPanel()
			{
			// If module is disabled, take ZERO space in the GUI
			if (!_moduleEnabled)
				{
				return; // Component is "off" - no GUI space consumed
				}

			DrawPanel(new Rect(0, 0, 300, 400)); // Default size
			}

		public void DrawPanel(Rect rect)
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
						Initialize(); // Attempt to reinitialize
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
				DrawPanelContent();
				}
			}

		private void DrawPanelContent()
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
						SetStatus("⏰ TaskMaster module disabled (taking zero GUI space)");
						}
					else
						{
						SetStatus("⏰ TaskMaster module enabled");
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

				if (_isTimerRunning && _activeTimeCard != null)
					{
					TaskData task = _activeTimeCard.GetTask();
					string taskName = task != null ? task.TaskName : "Unnamed Task";
					EditorGUILayout.LabelField($"Task: {taskName}");
					EditorGUILayout.LabelField($"Time: {FormatCurrentTime()}");
					EditorGUILayout.LabelField($"Started: {_activeTimeCard.GetStartTime():HH:mm:ss}");
					}
				else
					{
					EditorGUILayout.LabelField("No active session");
					}
				}

			EditorGUILayout.Space();

			// Time-card management
			DrawTimeCardManagement();
			}

		private void DrawTimeCardManagement()
			{
			EditorGUILayout.LabelField("📊 Time-Card Management", EditorStyles.boldLabel);

			// Time-card list
			if (_allTimeCards.Count > 0)
				{
				using var scroll = new EditorGUILayout.ScrollViewScope(Vector2.zero, GUILayout.Height(150));
				foreach (TimeCardData timeCard in _allTimeCards.OrderByDescending(tc => tc.GetLastModified()))
					{
					DrawTimeCardEntry(timeCard);
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
					LoadTimeCards();
					}

				if (GUILayout.Button("📂 Open Time-Card Folder"))
					{
					EditorUtility.RevealInFinder(_timeCardDirectory);
					}

				if (GUILayout.Button("📋 Import to TLDL"))
					{
					ShowTimeCardImportDialog();
					}
				}
			}

		private void ClockIn()
			{
			string taskName = GetCurrentTaskName();
			if (string.IsNullOrEmpty(taskName))
				{
				EditorUtility.DisplayDialog("No Task Selected", "Please select or enter a task name before clocking in.", "OK");
				return;
				}

			// Create or resume time-card
			_activeTimeCard = GetOrCreateTimeCard(taskName);
			_isTimerRunning = true;
			_sessionStartTime = EditorApplication.timeSinceStartup;
			_accumulatedTime = 0.0;

			// Create TaskData for this session
			var taskData = TaskData.CreateTask(taskName, $"Work session on {taskName}", "@copilot", 1);

			// Start the time-card with proper API
			_activeTimeCard.StartTimeCard(taskData);
			EditorUtility.SetDirty(_activeTimeCard);

			SetStatus($"⏰ Clocked in: {taskName}");
			SceneView.RepaintAll(); // Update scene view overlay
			}

		private void ClockOut()
			{
			if (_activeTimeCard == null) return;

			// Calculate final session time
			UpdateAccumulatedTime();

			// End the time-card session properly
			_activeTimeCard.EndTimeCard();
			EditorUtility.SetDirty(_activeTimeCard);
			AssetDatabase.SaveAssets();

			TaskData task = _activeTimeCard.GetTask();
			string taskName = task != null ? task.TaskName : "Unknown Task";

			SetStatus($"⏰ Clocked out: {FormatDuration(_accumulatedTime)} on {taskName}");

			_isTimerRunning = false;
			_activeTimeCard = null;
			SceneView.RepaintAll(); // Update scene view overlay
			}

		private void SaveCurrentSession()
			{
			if (_activeTimeCard == null || !_isTimerRunning) return;

			UpdateAccumulatedTime();

			// For now, we'll end the current session and start a new one
			// This preserves the session as a complete record
			_activeTimeCard.EndTimeCard();

			// Create a new session for continued work
			TaskData task = _activeTimeCard.GetTask();
			if (task != null)
				{
				var newTaskData = TaskData.CreateTask(task.TaskName, $"Continued work on {task.TaskName}", "@copilot", 1);
				_activeTimeCard.StartTimeCard(newTaskData);
				}

			EditorUtility.SetDirty(_activeTimeCard);
			AssetDatabase.SaveAssets();

			// Reset for new session
			_sessionStartTime = EditorApplication.timeSinceStartup;
			_accumulatedTime = 0.0;

			string taskName = task != null ? task.TaskName : "Unknown Task";
			SetStatus($"💾 Session saved: {taskName}");
			}

		private void OnEditorUpdate()
			{
			// Background timer update - runs regardless of window focus
			if (_isTimerRunning && _activeTimeCard != null)
				{
				UpdateAccumulatedTime();

				// Periodic save every 5 minutes for safety
				if (_accumulatedTime > 0 && (_accumulatedTime % 300) < 1.0) // Every 5 minutes
					{
					EditorUtility.SetDirty(_activeTimeCard);
					}

				// Repaint scene views to update timer display
				SceneView.RepaintAll();
				}
			}

		private void UpdateAccumulatedTime()
			{
			if (_isTimerRunning)
				{
				double currentTime = EditorApplication.timeSinceStartup;
				double sessionTime = currentTime - _sessionStartTime;
				_accumulatedTime += sessionTime;
				_sessionStartTime = currentTime;
				}
			}

		private string GetCurrentTaskName()
			{
			return _showCustomTask
				? string.IsNullOrEmpty(_customTaskName) ? "" : _customTaskName
				: _availableTasks[_selectedTaskIndex];
			}

		private TimeCardData GetOrCreateTimeCard(string taskName)
			{
			// Look for existing time-card with matching task name
			TimeCardData existing = _allTimeCards.FirstOrDefault(tc =>
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

			string fileName = $"TimeCard_{SanitizeFileName(taskName)}_{System.DateTime.Now:yyyyMMdd_HHmmss}.asset";
			string assetPath = System.IO.Path.Combine(_timeCardDirectory, fileName);

			AssetDatabase.CreateAsset(timeCard, assetPath);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			_allTimeCards.Add(timeCard);
			return timeCard;
			}

		private void LoadTimeCards()
			{
			_allTimeCards.Clear();

			if (!System.IO.Directory.Exists(_timeCardDirectory)) return;

			string[] guids = AssetDatabase.FindAssets("t:TimeCardData", new[] { _timeCardDirectory });
			foreach (string guid in guids)
				{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);
				TimeCardData timeCard = AssetDatabase.LoadAssetAtPath<TimeCardData>(assetPath);
				if (timeCard != null)
					{
					_allTimeCards.Add(timeCard);
					}
				}
			}

		private void RestoreActiveTimerState()
			{
			// Look for active time-card using proper getter
			TimeCardData activeCard = _allTimeCards.FirstOrDefault(tc => tc.GetIsOngoing());
			if (activeCard != null)
				{
				_activeTimeCard = activeCard;
				_isTimerRunning = true;
				_sessionStartTime = EditorApplication.timeSinceStartup; // Reset to current time
				_accumulatedTime = 0.0; // Start fresh session

				TaskData task = activeCard.GetTask();
				string taskName = task != null ? task.TaskName : "Unknown Task";
				SetStatus($"⏰ Restored active timer: {taskName}");
				}
			}

		private void DrawTimeCardEntry(TimeCardData timeCard)
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
					ImportTimeCardToTLDL(timeCard);
					}

				if (GUILayout.Button("🗑️", EditorStyles.miniButton, GUILayout.Width(25)))
					{
					if (EditorUtility.DisplayDialog("Delete Time-Card",
						$"Delete time-card for '{taskName}'?", "Delete", "Cancel"))
						{
						DeleteTimeCard(timeCard);
						}
					}
				}
			}

		private void ImportTimeCardToTLDL(TimeCardData timeCard)
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

			_data.TechnicalDetails += timeSummary;
			_data.IncludeTechnicalDetails = true;

			SetStatus($"📝 Imported time-card to TLDL: {taskName}");
			}

		private void ShowTimeCardImportDialog()
			{
			if (_allTimeCards.Count == 0)
				{
				EditorUtility.DisplayDialog("No Time-Cards", "No time-cards available to import.", "OK");
				return;
				}

			var menu = new GenericMenu();
			foreach (TimeCardData timeCard in _allTimeCards)
				{
				TaskData task = timeCard.GetTask();
				string taskName = task != null ? task.TaskName : "Unknown Task";
				string duration = $"{timeCard.GetDurationInHours():F2}h";

				menu.AddItem(new GUIContent($"{taskName} ({duration})"),
						   false, () => ImportTimeCardToTLDL(timeCard));
				}
			menu.ShowAsContext();
			}

		private void DeleteTimeCard(TimeCardData timeCard)
			{
			_allTimeCards.Remove(timeCard);
			string assetPath = AssetDatabase.GetAssetPath(timeCard);
			AssetDatabase.DeleteAsset(assetPath);
			AssetDatabase.Refresh();

			TaskData task = timeCard.GetTask();
			string taskName = task != null ? task.TaskName : "Unknown Task";
			SetStatus($"🗑️ Deleted time-card: {taskName}");
			}

		private string FormatCurrentTime()
			{
			if (_isTimerRunning)
				{
				UpdateAccumulatedTime();
				return FormatDuration(_accumulatedTime);
				}
			else
				{
				return "00:00:00";
				}
			}

		private string FormatDuration(double seconds)
			{
			var timeSpan = System.TimeSpan.FromSeconds(seconds);
			return $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
			}

		private string SanitizeFileName(string fileName)
			{
			char[] invalids = System.IO.Path.GetInvalidFileNameChars();
			return string.Join("_", fileName.Split(invalids, System.StringSplitOptions.RemoveEmptyEntries));
			}
		}
#endif
	}
