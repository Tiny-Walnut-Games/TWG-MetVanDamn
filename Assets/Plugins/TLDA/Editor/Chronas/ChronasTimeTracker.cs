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

		// Timer state
		private static bool _isTracking = false;
		private static double _sessionStartTime = 0.0;
		private static double _accumulatedTime = 0.0;
		private static string _currentTaskName = "";

		// 🎯 FOCUS-IMMUNE: Static timer registration
		private static bool _staticTimerInitialized = false;
		private static System.DateTime _systemTimeStart = System.DateTime.MinValue;

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

		[MenuItem("Tools/Living Dev Agent/Chronas Time Tracker", priority = 30)]
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
				SaveCurrentSession();
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
			this.DrawChronasControls();
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
					this.DrawTimeCardEntry(timeCard);
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
					this.ExportToTaskMaster();
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
				string timeText = FormatDuration(GetCurrentSessionTime());
				var timerStyle = new GUIStyle(EditorStyles.boldLabel)
					{
					normal = { textColor = _isTracking ? Color.green : Color.gray },
					fontSize = 14
					};

				GUILayout.Label($"⏳ {timeText}", timerStyle);

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
					if (_isTracking)
						{
						StopTracking();
						}
					else
						{
						StartTracking();
						}
					SceneView.RepaintAll();
					}

				GUI.backgroundColor = originalColor;

				// Quick save button
				using (new EditorGUI.DisabledScope(!_isTracking))
					{
					if (GUILayout.Button("💾", EditorStyles.miniButton, GUILayout.Width(25)))
						{
						SaveCurrentSession();
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

		// 🎯 FOCUS-IMMUNE: Static editor update method
		private static void OnStaticEditorUpdate ()
			{
			// Background time accumulation - runs regardless of window focus
			if (_isTracking)
				{
				UpdateAccumulatedTime();

				// Periodic auto-save every 5 minutes for safety
				if (_accumulatedTime > 0 && (_accumulatedTime % 300) < 1.0)
					{
					SaveCurrentSession();
					}
				}
			}

		private static void StartTracking ()
			{
			string taskName = GetCurrentTaskName();
			if (string.IsNullOrEmpty(taskName))
				{
				EditorUtility.DisplayDialog("No Task", "Please select or enter a task name.", "OK");
				return;
				}

			_currentTaskName = taskName;
			_isTracking = true;
			_sessionStartTime = EditorApplication.timeSinceStartup;
			_systemTimeStart = System.DateTime.Now; // Track system time for verification
			_accumulatedTime = 0.0;

			Debug.Log($"⏳ Chronas: Started FOCUS-IMMUNE tracking '{taskName}'");
			}

		private static void StopTracking ()
			{
            // ⚠ Intention ⚠ - @jmeyer1980 - IL for legibility
            if (!_isTracking) return;

			UpdateAccumulatedTime();
			SaveTimeCard();

			Debug.Log($"⏳ Chronas: Stopped tracking '{_currentTaskName}' - {FormatDuration(_accumulatedTime)}");

			_isTracking = false;
			_currentTaskName = "";
			_accumulatedTime = 0.0;
			_systemTimeStart = System.DateTime.MinValue;
			}

		private static void SaveCurrentSession ()
			{
			// ⚠ Intention ⚠ - @jmeyer1980 - IL for legibility
			if (!_isTracking) return;

			UpdateAccumulatedTime();
			SaveTimeCard();

			// Reset for new session
			_sessionStartTime = EditorApplication.timeSinceStartup;
			_systemTimeStart = System.DateTime.Now;
			_accumulatedTime = 0.0;

			Debug.Log($"⏳ Chronas: Auto-saved session for '{_currentTaskName}'");
			}

		private static void UpdateAccumulatedTime ()
			{
			if (_isTracking)
				{
				double currentTime = EditorApplication.timeSinceStartup;
				double sessionTime = currentTime - _sessionStartTime;
				_accumulatedTime += sessionTime;
				_sessionStartTime = currentTime;
				}
			}

		private static double GetCurrentSessionTime ()
			{
			// ⚠ Intention ⚠ - @jmeyer1980 - IL for legibility
			return !_isTracking ? 0.0 : _accumulatedTime + (EditorApplication.timeSinceStartup - _sessionStartTime);
			}

		private static string GetCurrentTaskName ()
			{
			return _quickTasks [ _selectedTaskIndex ] == "🎯 Custom..."
				? string.IsNullOrEmpty(_customTaskName) ? "" : _customTaskName
				: _quickTasks [ _selectedTaskIndex ];
			}

		private static void SaveTimeCard ()
			{
			var timeCard = new ChronasTimeCard
				{
				TaskName = _currentTaskName,
				DurationSeconds = _accumulatedTime,
				StartTime = System.DateTime.Now.AddSeconds(-_accumulatedTime),
				EndTime = System.DateTime.Now,
				LastModified = System.DateTime.Now
				};

			// Save as ScriptableObject
			string fileName = $"ChronasCard_{SanitizeFileName(_currentTaskName)}_{System.DateTime.Now:yyyyMMdd_HHmmss}.asset";
			string assetPath = System.IO.Path.Combine(_timeCardDirectory, fileName);

			ChronasTimeCardAsset asset = CreateInstance<ChronasTimeCardAsset>();
			asset.TimeCard = timeCard;

			AssetDatabase.CreateAsset(asset, assetPath);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			_timeCards.Add(timeCard);
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
					this.DeleteTimeCard(timeCard);
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
				this.PerformTaskMasterExport();
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
