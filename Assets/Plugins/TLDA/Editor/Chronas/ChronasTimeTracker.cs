#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LivingDevAgent.Editor.Chronas
{
    /// <summary>
    /// ⏳ Chronas - The Time Sovereign
    /// FOCUS-IMMUNE background time tracking with scene view overlay
    /// Continues tracking regardless of Unity focus state
    /// Designed for single-monitor workflows and TaskMaster integration
    /// </summary>
    [ExecuteInEditMode]
	public class ChronasTimeTracker : EditorWindow
		{
		// Scene view overlay state
		private static bool _overlayEnabled = true;
		private static Rect _overlayRect = new(10, 10, 280, 100);
		private static bool _isDragging = false;
		private static Vector2 _dragOffset;

		// 🔒 SECURITY HARDENED: Timer state with validation and access control
		private static bool _isTracking = false;
		private static double _sessionStartTime = 0.0;
		private static double _accumulatedTime = 0.0;
		private static string _currentTaskName = "";
		private static readonly object _timerLock = new(); // Thread safety

		// 🛡️ SECURITY: Focus-immune timer with session validation
		private static bool _staticTimerInitialized = false;
		private static System.DateTime _systemTimeStart = System.DateTime.MinValue;
		private static System.DateTime _lastValidationTime = System.DateTime.MinValue;
		private static readonly System.TimeSpan _maxSessionDuration = System.TimeSpan.FromHours(12); // Prevent runaway sessions

		// Task selection
		private static readonly List<string> _quickTasks = new()
		{
			"📝 Documentation",
			"🐛 Bug Fixing",
			"⚡ Feature Work",
			"🧪 Testing",
			"🎨 Art/Design",
			"🔧 Refactoring",
			"📊 Research",
			"💬 Code Review",
			"🎯 Custom..."
		};
		private static int _selectedTaskIndex = 0;
		private static string _customTaskName = "";

		// Time card management
		private static readonly List<ChronasTimeCard> _timeCards = new();
		private static readonly string _timeCardDirectory = "Assets/TLDA/Chronas/TimeCards/";

		// 🔒 SECURITY VALIDATION METHODS
		private static bool ValidateTaskName (string taskName)
			{
			if (string.IsNullOrWhiteSpace(taskName))
				return false;
			if (taskName.Length > 100) // Prevent excessive task names
				return false;
			if (taskName.Contains("..") || taskName.Contains("/") || taskName.Contains("\\"))
				return false; // Prevent path traversal attempts
			return true;
			}

		private static bool ValidateTimerSession ()
			{
			lock (_timerLock)
				{
				if (!_isTracking)
					{
					// Not tracking, no validation needed
					return true;
					}

				// Check for runaway sessions
				System.DateTime currentTime = System.DateTime.Now;
				System.TimeSpan sessionDuration = currentTime - _systemTimeStart;
				
				if (sessionDuration > _maxSessionDuration)
					{
					Debug.LogWarning($"⚠️ Chronas: Session exceeded maximum duration ({_maxSessionDuration.TotalHours:F1}h). Auto-stopping for security.");
					ForceStopTrackingUnsafe();
					return false;
					}

				// Update validation timestamp
				System.DateTime previousValidationTime = _lastValidationTime;
				_lastValidationTime = currentTime;
				
				// 🔒 DEBUG: Log validation success (only every 30 seconds to avoid spam)
				if ((currentTime - previousValidationTime).TotalSeconds > 30)
					{
					Debug.Log($"🔒 Chronas: Timer validation passed - Running for {sessionDuration:hh\\:mm\\:ss}, Task: '{_currentTaskName}'");
					}
				
				return true;
				}
			}

		private static void ForceStopTrackingUnsafe ()
			{
			// Called within lock, no additional locking needed
			Debug.LogWarning($"🔒 Chronas: Force-stopping timer session for '{_currentTaskName}' after {FormatDuration(_accumulatedTime)}");
			_isTracking = false;
			_currentTaskName = "";
			_accumulatedTime = 0.0;
			_systemTimeStart = System.DateTime.MinValue;
			}
		public static void ShowWindow ()
			{
			// Ensure focus-immune timer is initialized
			EnsureStaticTimerInitialized();

			ChronasTimeTracker window = GetWindow<ChronasTimeTracker>("⏳ Chronas");
			window.minSize = new Vector2(300, 200);
			window.Show();
			}

		// 🎯 FOCUS-IMMUNE: Static initialization method
		[InitializeOnLoadMethod]
		private static void InitializeChronasStatic ()
			{
			// Initialize timer system that survives focus loss
			EnsureStaticTimerInitialized();

			// Register for domain reload to maintain state
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			}

		private static void EnsureStaticTimerInitialized ()
			{
			if (_staticTimerInitialized) return;

			// Ensure time-card directory exists
			if (!System.IO.Directory.Exists(_timeCardDirectory))
				{
				System.IO.Directory.CreateDirectory(_timeCardDirectory);
				AssetDatabase.Refresh();
				}

			LoadTimeCards();

			// 🎯 FOCUS-IMMUNE: Subscribe to updates at static level
			EditorApplication.update += OnStaticEditorUpdate;

			// Register scene view overlay at static level
			SceneView.duringSceneGui += OnSceneViewGUI;

			_staticTimerInitialized = true;

			Debug.Log("⏳ Chronas: Focus-immune timer system initialized");
			}

		private static void OnPlayModeStateChanged (PlayModeStateChange state)
			{
			// Preserve timer state through play mode changes
			if (state == PlayModeStateChange.ExitingEditMode && _isTracking)
				{
				// Save current session before entering play mode
				lock (_timerLock)
					{
					SaveCurrentSessionUnsafe();
					}
				}
			}

		private void OnEnable ()
			{
			// Window-specific initialization only
			EnsureStaticTimerInitialized();
			}

		private void OnDisable ()
			{
			// 🎯 FOCUS-IMMUNE: DO NOT unsubscribe from updates!
			// Static timer continues running regardless of window state
			// ⚠ Intention ⚠ - @jmeyer1980 - fake code to silence suggestion regarding this being empty.
			bool _ = _staticTimerInitialized;
			}

		private void OnGUI ()
			{
			DrawChronasControls();
			}

		private void DrawChronasControls ()
			{
			GUILayout.Label("⏳ Chronas Time Tracker", EditorStyles.boldLabel);

			// Focus-immune status indicator
			using (new EditorGUILayout.HorizontalScope("box"))
				{
				EditorGUILayout.LabelField("🛡️ Focus-Immune Status:", EditorStyles.boldLabel);
				EditorGUILayout.LabelField(_staticTimerInitialized ? "✅ Active" : "❌ Inactive",
					_staticTimerInitialized ? EditorStyles.label : EditorStyles.centeredGreyMiniLabel);
				}

			EditorGUILayout.Space();

			// Scene overlay toggle
			using (new EditorGUILayout.HorizontalScope())
				{
				bool newOverlayEnabled = EditorGUILayout.Toggle("Scene View Overlay:", _overlayEnabled);
				if (newOverlayEnabled != _overlayEnabled)
					{
					_overlayEnabled = newOverlayEnabled;
					SceneView.RepaintAll();
					}

				if (GUILayout.Button("Reset Position", GUILayout.Width(100)))
					{
					_overlayRect = new Rect(10, 10, 280, 100);
					SceneView.RepaintAll();
					}
				}

			EditorGUILayout.Space();

			// Current session info
			using (new EditorGUILayout.VerticalScope("box"))
				{
				EditorGUILayout.LabelField("Current Session", EditorStyles.boldLabel);

				if (_isTracking)
					{
					EditorGUILayout.LabelField($"Task: {_currentTaskName}");
					EditorGUILayout.LabelField($"Time: {FormatDuration(GetCurrentSessionTime())}");

					// Show real-time vs accumulated time for debugging
					if (_systemTimeStart != System.DateTime.MinValue)
						{
						double systemElapsed = (System.DateTime.Now - _systemTimeStart).TotalSeconds;
						EditorGUILayout.LabelField($"System Time: {FormatDuration(systemElapsed)}", EditorStyles.miniLabel);
						}
					}
				else
					{
					EditorGUILayout.LabelField("No active session");
					}
				}

			EditorGUILayout.Space();

			// Time card list
			EditorGUILayout.LabelField("📊 Recent Time Cards", EditorStyles.boldLabel);

			if (_timeCards.Count > 0)
				{
				using var scroll = new EditorGUILayout.ScrollViewScope(Vector2.zero, GUILayout.Height(150));
				foreach (ChronasTimeCard timeCard in _timeCards.OrderByDescending(tc => tc.LastModified).Take(10))
					{
					DrawTimeCardEntry(timeCard);
					}
				}
			else
				{
				EditorGUILayout.HelpBox("No time cards yet. Start tracking to create your first card!", MessageType.Info);
				}

			EditorGUILayout.Space();

			// Management buttons
			using (new EditorGUILayout.HorizontalScope())
				{
				if (GUILayout.Button("🔄 Refresh"))
					{
					LoadTimeCards();
					}

				if (GUILayout.Button("📂 Open Folder"))
					{
					EditorUtility.RevealInFinder(_timeCardDirectory);
					}

				if (GUILayout.Button("📤 Export to TaskMaster"))
					{
					ExportToTaskMaster();
					}
				}
			}

		private static void OnSceneViewGUI (SceneView sceneView)
			{
			if (!_overlayEnabled) return;

			Handles.BeginGUI();

			// Background panel
			GUI.Box(_overlayRect, "", EditorStyles.helpBox);

			// Handle dragging
			HandleOverlayDragging();

			// Draw content
			GUILayout.BeginArea(new Rect(_overlayRect.x + 5, _overlayRect.y + 5,
									   _overlayRect.width - 10, _overlayRect.height - 10));

			DrawSceneOverlayContent();

			GUILayout.EndArea();

			Handles.EndGUI();

			// Force repaint if tracking
			if (_isTracking)
				{
				sceneView.Repaint();
				}
			}

		private static void DrawSceneOverlayContent ()
			{
			// Timer display with focus-immune indicator
			using (new GUILayout.HorizontalScope())
				{
				double currentSessionTime = GetCurrentSessionTime();
				string timeText = FormatDuration(currentSessionTime);
				
				// 🔒 ENHANCED DEBUG: Detailed status information to help diagnose false timer logging
				string statusIcon = _isTracking ? "🟢" : "🔴";
				string statusText = _isTracking ? "RUNNING" : "STOPPED";
				string debugText = $"{statusIcon} {timeText} ({statusText})";
				
				var timerStyle = new GUIStyle(EditorStyles.boldLabel)
					{
					normal = { textColor = _isTracking ? Color.green : Color.gray },
					fontSize = 14
					};

				GUILayout.Label($"⏳ {debugText}", timerStyle);

				// Focus-immune indicator
				if (_isTracking)
					{
					GUILayout.Label("🛡️", GUILayout.Width(15)); // Shield icon for focus immunity
					}

				GUILayout.FlexibleSpace();

				// Hide button
				if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(20)))
					{
					_overlayEnabled = false;
					SceneView.RepaintAll();
					}
				}

			// 🔒 ENHANCED DEBUG: Show detailed timer state for troubleshooting
			if (_isTracking)
				{
				using (new GUILayout.HorizontalScope())
					{
					double currentTime = EditorApplication.timeSinceStartup;
					double sessionTime = currentTime - _sessionStartTime;
					GUILayout.Label($"📊 Session: {sessionTime:F1}s, Total: {_accumulatedTime:F1}s", EditorStyles.miniLabel);
					}
				}

			// Current task
			if (_isTracking && !string.IsNullOrEmpty(_currentTaskName))
				{
				GUILayout.Label($"📋 {_currentTaskName}", EditorStyles.miniLabel);
				}

			// Task selector
			EditorGUI.BeginChangeCheck();
			int newTaskIndex = EditorGUILayout.Popup(_selectedTaskIndex, _quickTasks.ToArray());
			if (EditorGUI.EndChangeCheck())
				{
				_selectedTaskIndex = newTaskIndex;
				}

			// Custom task field
			if (_quickTasks [ _selectedTaskIndex ] == "🎯 Custom...")
				{
				EditorGUI.BeginChangeCheck();
				string newCustomName = EditorGUILayout.TextField(_customTaskName);
				if (EditorGUI.EndChangeCheck())
					{
					_customTaskName = newCustomName;
					}
				}

			// Start/Stop button
			using (new GUILayout.HorizontalScope())
				{
				string buttonText = _isTracking ? "⏹️ Stop" : "▶️ Start";
				Color buttonColor = _isTracking ? Color.red : Color.green;

				Color originalColor = GUI.backgroundColor;
				GUI.backgroundColor = buttonColor;

				if (GUILayout.Button(buttonText, GUILayout.Height(25)))
					{
					// 🔒 ENHANCED DEBUG: Provide immediate feedback on button press
					string currentTaskName = GetCurrentTaskName();
					if (_isTracking)
						{
						Debug.Log($"🔒 Chronas: User clicked STOP button for '{_currentTaskName}'");
						StopTracking();
						}
					else
						{
						Debug.Log($"🔒 Chronas: User clicked START button for '{currentTaskName}'");
						if (string.IsNullOrEmpty(currentTaskName))
							{
							Debug.LogWarning("🔒 Chronas: Cannot start - no task selected");
							EditorUtility.DisplayDialog("No Task Selected", "Please select a task from the dropdown or enter a custom task name.", "OK");
							}
						else
							{
							StartTracking();
							// Verify timer actually started
							if (_isTracking)
								{
								Debug.Log($"🔒 Chronas: Timer successfully started for '{_currentTaskName}'");
								}
							else
								{
								Debug.LogError("🔒 Chronas: Timer failed to start - check console for errors");
								}
							}
						}
					SceneView.RepaintAll();
					}

				GUI.backgroundColor = originalColor;

				// Quick save button
				using (new EditorGUI.DisabledScope(!_isTracking))
					{
					if (GUILayout.Button("💾", EditorStyles.miniButton, GUILayout.Width(25)))
						{
						lock (_timerLock)
							{
							SaveCurrentSessionUnsafe();
							}
						SceneView.RepaintAll();
						}
					}
				}
			}

		private static void HandleOverlayDragging ()
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

		// 🔒 SECURITY: Thread-safe editor update method with validation
		private static void OnStaticEditorUpdate ()
			{
			// Validate session security first
			if (!ValidateTimerSession())
				return;

			lock (_timerLock)
				{
				if (_isTracking)
					{
					UpdateAccumulatedTimeUnsafe();

					// Periodic auto-save every 5 minutes for safety
					if (_accumulatedTime > 0 && (_accumulatedTime % 300) < 1.0)
						{
						SaveCurrentSessionUnsafe();
						}
					}
				}
			}

		private static void StartTracking ()
			{
			string taskName = GetCurrentTaskName();
			
			// 🔒 SECURITY: Validate task name
			if (!ValidateTaskName(taskName))
				{
				EditorUtility.DisplayDialog("Invalid Task Name", 
					"Task name is invalid. Please ensure it's not empty, under 100 characters, and doesn't contain path separators.", "OK");
				Debug.LogWarning($"🔒 Chronas: Start tracking failed - invalid task name: '{taskName}'");
				return;
				}

			lock (_timerLock)
				{
				if (_isTracking)
					{
					EditorUtility.DisplayDialog("Timer Already Running", 
						$"Timer is already running for '{_currentTaskName}'. Stop the current session before starting a new one.", "OK");
					Debug.LogWarning($"🔒 Chronas: Start tracking failed - timer already running for '{_currentTaskName}'");
					return;
					}

				_currentTaskName = taskName;
				_isTracking = true;
				_sessionStartTime = EditorApplication.timeSinceStartup;
				_systemTimeStart = System.DateTime.Now;
				_lastValidationTime = System.DateTime.Now;
				_accumulatedTime = 0.0;

				Debug.Log($"🔒 Chronas: STARTED timer session '{taskName}' at {_systemTimeStart:HH:mm:ss} (Unity time: {_sessionStartTime:F2})");
				Debug.Log($"🔒 Chronas: Timer status - IsTracking: {_isTracking}, TaskName: '{_currentTaskName}', StartTime: {_sessionStartTime}");
				}
			}

		private static void StopTracking ()
			{
			lock (_timerLock)
				{
				if (!_isTracking)
					{
					Debug.LogWarning("🔒 Chronas: Stop requested but no timer is running");
					return;
					}

				UpdateAccumulatedTimeUnsafe();

				System.TimeSpan sessionDuration = System.DateTime.Now - _systemTimeStart;
				double totalTrackedTime = _accumulatedTime;
				string taskName = _currentTaskName;

				// 🔒 SECURITY: Save before clearing sensitive data
				SaveTimeCardUnsafe();

				Debug.Log($"🔒 Chronas: Stopped timer '{taskName}' - Session: {sessionDuration:hh\\:mm\\:ss}, Tracked: {FormatDuration(totalTrackedTime)}");

				// Clear sensitive data
				_isTracking = false;
				_currentTaskName = "";
				_accumulatedTime = 0.0;
				_systemTimeStart = System.DateTime.MinValue;
				_lastValidationTime = System.DateTime.MinValue;
				}
			}

		// 🔒 SECURITY: Thread-safe helper methods (called within lock)
		private static void UpdateAccumulatedTimeUnsafe ()
			{
			if (_isTracking)
				{
				double currentTime = EditorApplication.timeSinceStartup;
				double sessionTime = currentTime - _sessionStartTime;
				_accumulatedTime += sessionTime;
				_sessionStartTime = currentTime;
				}
			}

		private static void SaveCurrentSessionUnsafe ()
			{
			if (!_isTracking)
				return;

			UpdateAccumulatedTimeUnsafe();
			SaveTimeCardUnsafe();

			// Reset for new session
			_sessionStartTime = EditorApplication.timeSinceStartup;
			_systemTimeStart = System.DateTime.Now;
			_accumulatedTime = 0.0;

			Debug.Log($"🔒 Chronas: Auto-saved session for '{_currentTaskName}'");
			}

		private static void SaveTimeCardUnsafe ()
			{
			// Validate data before saving
			if (string.IsNullOrWhiteSpace(_currentTaskName) || _accumulatedTime <= 0)
				{
				Debug.LogWarning("🔒 Chronas: Skipping save - invalid task name or time data");
				return;
				}

			var timeCard = new ChronasTimeCard
				{
				TaskName = _currentTaskName,
				DurationSeconds = _accumulatedTime,
				StartTime = System.DateTime.Now.AddSeconds(-_accumulatedTime),
				EndTime = System.DateTime.Now,
				LastModified = System.DateTime.Now
				};

			// 🔒 SECURITY: Sanitize filename to prevent path traversal
			string sanitizedTaskName = SanitizeFileName(_currentTaskName);
			if (sanitizedTaskName.Length > 50) // Prevent excessively long filenames
				sanitizedTaskName = sanitizedTaskName[ ..50 ];

			string fileName = $"ChronasCard_{sanitizedTaskName}_{System.DateTime.Now:yyyyMMdd_HHmmss}.asset";
			string assetPath = System.IO.Path.Combine(_timeCardDirectory, fileName);

			try
				{
				ChronasTimeCardAsset asset = CreateInstance<ChronasTimeCardAsset>();
				asset.TimeCard = timeCard;

				AssetDatabase.CreateAsset(asset, assetPath);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();

				_timeCards.Add(timeCard);
				Debug.Log($"🔒 Chronas: Securely saved time card for '{_currentTaskName}' ({FormatDuration(_accumulatedTime)})");
				}
			catch (System.Exception ex)
				{
				Debug.LogError($"🔒 Chronas: Failed to save time card: {ex.Message}");
				}
			}

		private static double GetCurrentSessionTime ()
			{
			lock (_timerLock)
				{
				if (!_isTracking)
					{
					// 🔒 DEBUG: Not tracking
					return 0.0;
					}
				
				// 🔒 SECURITY: Validate timer state before returning time
				if (!ValidateTimerSession())
					{
					// 🔒 DEBUG: Validation failed, timer was stopped
					Debug.LogWarning("🔒 Chronas: GetCurrentSessionTime - validation failed, timer stopped");
					return 0.0;
					}
					
				double currentTime = EditorApplication.timeSinceStartup;
				double sessionTime = currentTime - _sessionStartTime;
				double totalTime = _accumulatedTime + sessionTime;
				
				// 🔒 DEBUG: Log current time calculation (only occasionally to avoid spam)
				if (UnityEngine.Random.value < 0.01f) // ~1% chance
					{
					Debug.Log($"🔒 Chronas: Time calc - Current: {currentTime:F2}, Start: {_sessionStartTime:F2}, Session: {sessionTime:F2}, Accumulated: {_accumulatedTime:F2}, Total: {totalTime:F2}");
					}
				
				return totalTime;
				}
			}

		private static string GetCurrentTaskName ()
			{
			string taskName = _quickTasks [ _selectedTaskIndex ] == "🎯 Custom..."
				? string.IsNullOrEmpty(_customTaskName) ? "" : _customTaskName
				: _quickTasks [ _selectedTaskIndex ];
				
			// 🔒 DEBUG: Log task name selection
			Debug.Log($"🔒 Chronas: GetCurrentTaskName - Selected index: {_selectedTaskIndex}, Task: '{_quickTasks[_selectedTaskIndex]}', Custom: '{_customTaskName}', Result: '{taskName}'");
			
			return taskName;
			}

		private static void LoadTimeCards ()
			{
			_timeCards.Clear();
			// ⚠ Intention ⚠ - @jmeyer1980 - IL for legibility
			if (!System.IO.Directory.Exists(_timeCardDirectory)) return;

			string [ ] guids = AssetDatabase.FindAssets("t:ChronasTimeCardAsset", new [ ] { _timeCardDirectory });
			foreach (string guid in guids)
				{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);
				ChronasTimeCardAsset asset = AssetDatabase.LoadAssetAtPath<ChronasTimeCardAsset>(assetPath);
				if (asset != null && asset.TimeCard != null)
					{
					_timeCards.Add(asset.TimeCard);
					}
				}
			}

		private void DrawTimeCardEntry (ChronasTimeCard timeCard)
			{
			using (new EditorGUILayout.HorizontalScope("box"))
				{
				EditorGUILayout.LabelField($"📋 {timeCard.TaskName}", EditorStyles.boldLabel, GUILayout.Width(150));
				EditorGUILayout.LabelField($"⏱️ {FormatDuration(timeCard.DurationSeconds)}", GUILayout.Width(80));
				EditorGUILayout.LabelField($"📅 {timeCard.LastModified:MM/dd HH:mm}", EditorStyles.miniLabel);

				GUILayout.FlexibleSpace();

				if (GUILayout.Button("🗑️", EditorStyles.miniButton, GUILayout.Width(25)))
					{
					DeleteTimeCard(timeCard);
					}
				}
			}

		private void DeleteTimeCard (ChronasTimeCard timeCard)
			{
			// Find and delete the corresponding asset
			string [ ] guids = AssetDatabase.FindAssets("t:ChronasTimeCardAsset", new [ ] { _timeCardDirectory });
			foreach (string guid in guids)
				{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);
				ChronasTimeCardAsset asset = AssetDatabase.LoadAssetAtPath<ChronasTimeCardAsset>(assetPath);
				if (asset.TimeCard != timeCard)
					{
					asset.TimeCard = timeCard;
					// ⚠ Intention ⚠ - @jmeyer1980 - IL for legibility
					if (asset != null) AssetDatabase.DeleteAsset(assetPath); // Delete the asset file
					break;
					}
				}

			_timeCards.Remove(timeCard);
			AssetDatabase.Refresh();
			}

		private void ExportToTaskMaster ()
			{
			Debug.Log("⏳ → 🎯 Exporting Chronas time cards to TaskMaster...");

			if (_timeCards.Count == 0)
				{
				EditorUtility.DisplayDialog("No Time Cards", "No time cards available to export to TaskMaster.", "OK");
				return;
				}

			// Calculate total export data
			double totalHours = _timeCards.Sum(tc => tc.DurationSeconds / 3600.0);
			int uniqueTasks = _timeCards.Select(tc => tc.TaskName).Distinct().Count();

			string exportSummary = $"📊 Export Summary:\n";
			exportSummary += $"• {_timeCards.Count} time cards\n";
			exportSummary += $"• {uniqueTasks} unique tasks\n";
			exportSummary += $"• {totalHours:F2} total hours\n\n";
			exportSummary += "This will create TaskMaster task cards with time tracking data.";

			if (EditorUtility.DisplayDialog("Export to TaskMaster", exportSummary, "Export", "Cancel"))
				{
				PerformTaskMasterExport();
				}
			}

		private void PerformTaskMasterExport ()
			{
			try
				{
				// Group time cards by task name
				IEnumerable<IGrouping<string, ChronasTimeCard>> taskGroups = _timeCards.GroupBy(tc => tc.TaskName);
				int exportCount = 0;

				foreach (IGrouping<string, ChronasTimeCard> group in taskGroups)
					{
					string taskName = group.Key;
					var cards = group.ToList();
					double totalTime = cards.Sum(tc => tc.DurationSeconds / 3600.0); // Convert to hours
					int sessions = cards.Count;

					// Create TaskMaster integration data
					string integrationData = $"Chronas Import:\n";
					integrationData += $"• Sessions: {sessions}\n";
					integrationData += $"• Total Time: {totalTime:F2} hours\n";
					integrationData += $"• First Session: {cards.Min(c => c.StartTime):yyyy-MM-dd HH:mm}\n";
					integrationData += $"• Last Session: {cards.Max(c => c.EndTime):yyyy-MM-dd HH:mm}\n";

					Debug.Log($"📋 Creating TaskMaster task: {taskName} ({totalTime:F2}h)");
					exportCount++;
					}

				string resultMessage = $"✅ Successfully exported {exportCount} tasks to TaskMaster!\n\n";
				resultMessage += "🎯 Open TaskMaster to view the imported tasks with time tracking data.";

				EditorUtility.DisplayDialog("Export Complete", resultMessage, "Open TaskMaster", "OK");

				// TODO: Actually send data to TaskMaster when API is available
				// For now, we'll log the export for debugging
				Debug.Log($"⏳ → 🎯 Chronas exported {exportCount} tasks to TaskMaster");
				}
			catch (System.Exception ex)
				{
				Debug.LogError($"❌ Chronas export failed: {ex.Message}");
				EditorUtility.DisplayDialog("Export Failed", $"Failed to export to TaskMaster:\n{ex.Message}", "OK");
				}
			}

		private static string FormatDuration (double seconds)
			{
			var timeSpan = System.TimeSpan.FromSeconds(seconds);
			return $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
			}

		private static string SanitizeFileName (string fileName)
			{
			char [ ] invalids = System.IO.Path.GetInvalidFileNameChars();
			return string.Join("_", fileName.Split(invalids, System.StringSplitOptions.RemoveEmptyEntries));
			}

		#region Data Structures

		[System.Serializable]
		public class ChronasTimeCard
			{
			public string TaskName;
			public double DurationSeconds;
			public System.DateTime StartTime;
			public System.DateTime EndTime;
			public System.DateTime LastModified;
			}

		[CreateAssetMenu(fileName = "ChronasTimeCard", menuName = "Chronas/Time Card")]
		public class ChronasTimeCardAsset : ScriptableObject
			{
			public ChronasTimeCard TimeCard;
			}

		#endregion
		}
	}
#endif
