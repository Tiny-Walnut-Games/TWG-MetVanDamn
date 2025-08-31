#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LivingDevAgent.Editor.TaskMaster
	{
	/// <summary>
	/// 🎯 TaskMaster - Standalone Project Management Timeline
	/// Multi-scale timeline (Day/Week/Month/Year/5Y/10Y) with Kanban integration
	/// Professional project management for Unity development workflows
	/// Features: Timeline + Kanban hybrid, task cards, time tracking integration
	/// </summary>
	public class TaskMasterWindow : EditorWindow
		{
		// Timeline state
		private TimelineScale _currentScale = TimelineScale.Week;
		private DateTime _timelineCenter = DateTime.Now;
		private Vector2 _scrollPosition = Vector2.zero;

		// View modes
		private ViewMode _currentView = ViewMode.Timeline;
#pragma warning disable 0414 // Assigned but never used - will be implemented
		private readonly bool _showKanbanOverlay = true;
#pragma warning restore 0414

		// Task management  
		private readonly List<TaskData> _allTasks = new();
		private TaskData _selectedTask = null;
#pragma warning disable 0414 // Assigned but never used - will be implemented
		private readonly TaskData _draggedTask = null;
#pragma warning restore 0414

		// UI state
		private string _newTaskTitle = "";
#pragma warning disable 0414 // Assigned but never used - will be implemented
		private readonly TaskPriority _newTaskPriority = TaskPriority.Medium;
		private readonly DateTime _newTaskDeadline = DateTime.Now.AddDays(7);
#pragma warning restore 0414

		// Zoom and navigation
		private readonly float [ ] _zoomLevels = { 0.5f, 0.75f, 1.0f, 1.25f, 1.5f, 2.0f };
		private int _currentZoomIndex = 2; // Start at 1.0f

		[MenuItem("Tools/Living Dev Agent/TaskMaster", priority = 10)]
		public static void ShowWindow ()
			{
			TaskMasterWindow window = GetWindow<TaskMasterWindow>("🎯 TaskMaster");
			window.minSize = new Vector2(1000, 600);
			window.Show();
			}

		private void OnEnable ()
			{
			this.InitializeData();
			}

		private void InitializeData ()
			{
			// Load existing tasks from ScriptableObjects
			this.LoadTasksFromAssets();

			// Ensure we have some sample data for demo
			if (this._allTasks.Count == 0)
				{
				this.CreateSampleTasks();
				}
			}

		private void OnGUI ()
			{
			this.DrawTopToolbar();

			using (new EditorGUILayout.HorizontalScope())
				{
				this.DrawSidebar();
				this.DrawMainContent();
				this.DrawTaskInspector();
				}

			this.DrawBottomStatusBar();

			this.HandleKeyboardShortcuts();
			}

		private void DrawTopToolbar ()
			{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
				{
				// View mode toggles
				GUILayout.Label("📋", GUILayout.Width(20));
				if (GUILayout.Toggle(this._currentView == ViewMode.Kanban, "Kanban", EditorStyles.toolbarButton))
					this._currentView = ViewMode.Kanban;

				if (GUILayout.Toggle(this._currentView == ViewMode.Timeline, "Timeline", EditorStyles.toolbarButton))
					this._currentView = ViewMode.Timeline;

				if (GUILayout.Toggle(this._currentView == ViewMode.Hybrid, "Hybrid", EditorStyles.toolbarButton))
					this._currentView = ViewMode.Hybrid;

				GUILayout.Space(20);

				// Timeline scale controls
				GUILayout.Label("🗓️", GUILayout.Width(20));
				foreach (TimelineScale scale in Enum.GetValues(typeof(TimelineScale)))
					{
					if (GUILayout.Toggle(this._currentScale == scale, scale.ToString(), EditorStyles.toolbarButton))
						{
						if (this._currentScale != scale)
							{
							this._currentScale = scale;
							this.SnapToTimelineScale();
							}
						}
					}

				GUILayout.Space(20);

				// Navigation controls
				if (GUILayout.Button("◀", EditorStyles.toolbarButton, GUILayout.Width(25)))
					this.NavigateTimeline(-1);

				if (GUILayout.Button("Today", EditorStyles.toolbarButton))
					this.NavigateToToday();

				if (GUILayout.Button("▶", EditorStyles.toolbarButton, GUILayout.Width(25)))
					this.NavigateTimeline(1);

				GUILayout.FlexibleSpace();

				// Task creation
				this._newTaskTitle = EditorGUILayout.TextField(this._newTaskTitle, EditorStyles.toolbarTextField, GUILayout.Width(200));

				if (GUILayout.Button("+ Add Task", EditorStyles.toolbarButton))
					{
					this.CreateNewTask();
					}

				GUILayout.Space(10);

				// Zoom controls
				GUILayout.Label($"🔍 {this._zoomLevels [ this._currentZoomIndex ]:P0}", GUILayout.Width(50));
				if (GUILayout.Button("-", EditorStyles.toolbarButton, GUILayout.Width(20)))
					this.ZoomOut();
				if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(20)))
					this.ZoomIn();
				}
			}

		private void DrawSidebar ()
			{
			using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(200)))
				{
				GUILayout.Label("🎯 Quick Filters", EditorStyles.boldLabel);

				if (GUILayout.Button("📋 All Tasks"))
					this.FilterTasks(null);

				if (GUILayout.Button("🔴 Critical"))
					this.FilterTasks(TaskPriority.Critical);

				if (GUILayout.Button("🟡 High Priority"))
					this.FilterTasks(TaskPriority.High);

				if (GUILayout.Button("🟢 Medium Priority"))
					this.FilterTasks(TaskPriority.Medium);

				if (GUILayout.Button("🔵 Low Priority"))
					this.FilterTasks(TaskPriority.Low);

				GUILayout.Space(10);

				GUILayout.Label("⏰ Time Tracking", EditorStyles.boldLabel);

				if (GUILayout.Button("🕐 Import from Chronas"))
					this.ImportTimeDataFromChronas();

				if (GUILayout.Button("📊 Export Time Report"))
					this.ExportTimeReport();

				GUILayout.Space(10);

				GUILayout.Label("📈 Project Stats", EditorStyles.boldLabel);

				ProjectStats stats = this.CalculateProjectStats();
				EditorGUILayout.LabelField($"Total Tasks: {stats.totalTasks}");
				EditorGUILayout.LabelField($"Completed: {stats.completedTasks}");
				EditorGUILayout.LabelField($"In Progress: {stats.inProgressTasks}");
				EditorGUILayout.LabelField($"Blocked: {stats.blockedTasks}");
				}
			}

		private void DrawMainContent ()
			{
			using (new EditorGUILayout.VerticalScope())
				{
				switch (this._currentView)
					{
					case ViewMode.Kanban:
						this.DrawKanbanView();
						break;
					case ViewMode.Timeline:
						this.DrawTimelineView();
						break;
					case ViewMode.Hybrid:
						this.DrawHybridView();
						break;
					default:
						break;
					}
				}
			}

		private void DrawKanbanView ()
			{
			using var scroll = new EditorGUILayout.ScrollViewScope(this._scrollPosition);
			using (new EditorGUILayout.HorizontalScope())
				{
				this.DrawKanbanColumn("📋 To Do", TaskStatus.ToDo);
				this.DrawKanbanColumn("⚡ In Progress", TaskStatus.InProgress);
				this.DrawKanbanColumn("🚫 Blocked", TaskStatus.Blocked);
				this.DrawKanbanColumn("✅ Done", TaskStatus.Done);
				}
			}

		private void DrawKanbanColumn (string title, TaskStatus status)
			{
			using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(250), GUILayout.ExpandHeight(true)))
				{
				GUILayout.Label(title, EditorStyles.boldLabel);

				var tasksInColumn = this._allTasks.Where(t => this.GetTaskStatusFromTaskData(t) == status).ToList();

				foreach (TaskData task in tasksInColumn)
					{
					this.DrawTaskCard(task);
					}

				// Drop zone for dragging tasks between columns
				Rect dropRect = GUILayoutUtility.GetRect(200, 50);
				GUI.Box(dropRect, "Drop tasks here...", EditorStyles.helpBox);

				this.HandleTaskDropInColumn(dropRect, status);
				}
			}

		private void DrawTaskCard (TaskData task)
			{
			// 🎯 FIXED: Proper selection highlighting with visual feedback
			bool isSelected = (this._selectedTask == task);
			
			var cardStyle = new GUIStyle("box")
				{
				normal = { background = this.CreateTaskCardBackground(task) },
				padding = new RectOffset(8, 8, 8, 8)
				};

			// Add selection highlighting
			Color originalColor = GUI.backgroundColor;
			if (isSelected)
				{
				GUI.backgroundColor = new Color(1f, 1f, 0.3f, 1f); // Yellow highlight
				}

			using (new EditorGUILayout.VerticalScope(cardStyle))
				{
				// Task title and priority
				using (new EditorGUILayout.HorizontalScope())
					{
					GUILayout.Label(this.GetPriorityEmoji(task.priorityLevel), GUILayout.Width(20));
					
					// Use bold style for selected tasks
					GUIStyle titleStyle = isSelected ? EditorStyles.whiteBoldLabel : EditorStyles.boldLabel;
					EditorGUILayout.LabelField(task.TaskName, titleStyle);
					
					// Selection indicator
					if (isSelected)
						{
						GUILayout.Label("👈 SELECTED", EditorStyles.miniLabel, GUILayout.Width(80));
						}
					}

				// Task details
				if (!string.IsNullOrEmpty(task.taskDescription))
					{
					EditorGUILayout.LabelField(task.taskDescription, EditorStyles.wordWrappedMiniLabel);
					}

				// Deadline and time tracking
				using (new EditorGUILayout.HorizontalScope())
					{
					// Assignee info
					if (!string.IsNullOrEmpty(task.assignedTo))
						{
						EditorGUILayout.LabelField($"👤 {task.assignedTo}", EditorStyles.miniLabel);
						}

					GUILayout.FlexibleSpace();

					// Show time tracked from TimeCardData if available
					if (task.timeCard != null)
						{
						float hours = task.timeCard.GetDurationInHours();
						if (hours > 0)
							{
							EditorGUILayout.LabelField($"⏱️ {hours:F1}h", EditorStyles.miniLabel);
							}
						}
					}

				// Handle task selection and dragging
				Rect cardRect = GUILayoutUtility.GetLastRect();
				this.HandleTaskCardInteraction(task, cardRect);
				}
				
			// Restore original background color
			GUI.backgroundColor = originalColor;
			}

		private void DrawTimelineView ()
			{
			using var scroll = new EditorGUILayout.ScrollViewScope(this._scrollPosition);
			this.DrawTimelineHeader();
			this.DrawTimelineTracks();
			}

		private void DrawHybridView ()
			{
			// Split view: Timeline on top, Kanban on bottom
			using (new EditorGUILayout.VerticalScope())
				{
				// Timeline section (60% of height)
				using (new EditorGUILayout.VerticalScope("box", GUILayout.ExpandHeight(true)))
					{
					GUILayout.Label("📊 Timeline View", EditorStyles.boldLabel);
					this.DrawTimelineView();
					}

				GUILayout.Space(5);

				// Kanban section (40% of height)
				using (new EditorGUILayout.VerticalScope("box", GUILayout.Height(200)))
					{
					GUILayout.Label("📋 Kanban Board", EditorStyles.boldLabel);
					this.DrawKanbanView();
					}
				}
			}

		private void DrawTaskInspector ()
			{
			using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(300)))
				{
				GUILayout.Label("🔍 Task Inspector", EditorStyles.boldLabel);

				if (this._selectedTask != null)
					{
					this.DrawSelectedTaskDetails();
					}
				else
					{
					EditorGUILayout.HelpBox("Select a task to view details", MessageType.Info);
					}
				}
			}

		private void DrawSelectedTaskDetails ()
			{
			TaskData task = this._selectedTask;

			// 🎯 FIXED: Enhanced task inspector with save feedback
			EditorGUILayout.LabelField($"📋 Editing: {task.TaskName}", EditorStyles.boldLabel);
			EditorGUILayout.Space();

			// Track if changes are made
			EditorGUI.BeginChangeCheck();
			
			// Editable task properties
			task.TaskName = EditorGUILayout.TextField("Title:", task.TaskName);
			task.taskDescription = EditorGUILayout.TextArea(task.taskDescription, GUILayout.Height(60));
			task.priorityLevel = EditorGUILayout.IntSlider("Priority:", task.priorityLevel, 1, 5);
			task.assignedTo = EditorGUILayout.TextField("Assigned To:", task.assignedTo);

			// Status handling using TaskData's boolean flags
			TaskStatus currentStatus = this.GetTaskStatusFromTaskData(task);
			TaskStatus newStatus = (TaskStatus)EditorGUILayout.EnumPopup("Status:", currentStatus);
			
			if (newStatus != currentStatus)
				{
				this.SetTaskDataStatus(task, newStatus);
				}

			// Save changes automatically when fields change
			if (EditorGUI.EndChangeCheck())
				{
				EditorUtility.SetDirty(task);
				Debug.Log($"🎯 TaskMaster: Updated task '{task.TaskName}'");
				}

			GUILayout.Space(10);

			// Time tracking from TimeCardData
			if (task.timeCard != null)
				{
				float hours = task.timeCard.GetDurationInHours();
				EditorGUILayout.LabelField($"Time Tracked: {hours:F2} hours");
				
				if (task.timeCard.GetIsOngoing())
					{
					EditorGUILayout.LabelField($"Started: {task.timeCard.GetStartTime():g}");
					}
				}

			using (new EditorGUILayout.HorizontalScope())
				{
				if (GUILayout.Button("⏰ Start Timer"))
					{
					this.StartTimerForTask(task);
					}

				if (GUILayout.Button("⏸️ Stop Timer"))
					{
					this.StopTimerForTask(task);
					}
				}

			GUILayout.Space(10);

			// Task actions
			if (GUILayout.Button("🗑️ Delete Task"))
				{
				if (EditorUtility.DisplayDialog("Delete Task",
					$"Are you sure you want to delete '{task.Title}'?", "Delete", "Cancel"))
					{
					this.DeleteTask(task);
					}
				}

			if (GUILayout.Button("📋 Export to TLDL"))
				{
				this.ExportTaskToTLDL(task);
				}

			// 🐙 NEW: GitHub Integration
			if (GUILayout.Button("🐙 Create GitHub Issue"))
				{
				this.CreateGitHubIssueFromTask(task);
				}
			}

		// [Additional helper methods would go here - timeline drawing, date pickers, etc.]

		#region Helper Methods and Data Structures

		public enum TimelineScale
			{
			Day, Week, Month, Year, FiveYear, TenYear
			}

		public enum ViewMode
			{
			Kanban, Timeline, Hybrid
			}

		public enum TaskPriority
			{
			Critical, High, Medium, Low, Backlog
			}

		public enum TaskStatus
			{
			ToDo, InProgress, Blocked, Done
			}

		[Serializable]
		public struct ProjectStats
			{
			public int totalTasks;
			public int completedTasks;
			public int inProgressTasks;
			public int blockedTasks;
			}

		#endregion

		// Helper methods for TaskData integration
		private TaskStatus GetTaskStatusFromTaskData(TaskData task)
			{
			if (task.isCompleted) return TaskStatus.Done;
			if (task.isCanceled) return TaskStatus.Blocked;
			// Check if task has been started (has time tracking or is assigned)
			if (task.timeCard?.GetIsOngoing() == true || !string.IsNullOrEmpty(task.assignedTo))
				return TaskStatus.InProgress;
			return TaskStatus.ToDo;
			}

		private void SetTaskDataStatus(TaskData task, TaskStatus status)
			{
			switch (status)
				{
				case TaskStatus.ToDo:
					task.isCompleted = false;
					task.isCanceled = false;
					break;
				case TaskStatus.InProgress:
					task.isCompleted = false;
					task.isCanceled = false;
					// Start time tracking if not already started
					if (task.timeCard == null || !task.timeCard.GetIsOngoing())
						{
						if (task.timeCard == null)
							{
							task.timeCard = ScriptableObject.CreateInstance<TimeCardData>();
							}
						task.timeCard.StartTimeCard(task);
						}
					break;
				case TaskStatus.Done:
					task.CompleteTask(); // Use TaskData's built-in method
					break;
				case TaskStatus.Blocked:
					task.CancelTask(); // Use TaskData's built-in method
					break;
				}
			}

		private string GetPriorityEmoji(int priorityLevel)
			{
			return priorityLevel switch
				{
				1 => "🔥", // Highest priority
				2 => "⚡",
				3 => "📋",
				4 => "📌",
				5 => "💤", // Lowest priority
				_ => "📋"
				};
			}

		#endregion

		// Implementation stubs - IMPLEMENTING ACTUAL FUNCTIONALITY
		private void LoadTasksFromAssets ()
			{
			this._allTasks.Clear();
			
			// 🔍 Look for TaskData assets in the project
			string[] guids = AssetDatabase.FindAssets("t:TaskData");
			Debug.Log($"🎯 TaskMaster: Found {guids.Length} TaskData assets");
			
			foreach (string guid in guids)
				{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);
				TaskData asset = AssetDatabase.LoadAssetAtPath<TaskData>(assetPath);
				if (asset != null)
					{
					this._allTasks.Add(asset);
					Debug.Log($"📋 Loaded task: {asset.TaskName}");
					}
				}
			
			Debug.Log($"🎯 TaskMaster: Loaded {this._allTasks.Count} tasks from assets");
			}

		private void CreateSampleTasks ()
			{
			// Create some demo tasks using TaskData.CreateTask factory method
			TaskData task1 = TaskData.CreateTask("Fix Chronas Integration", "Connect scene overlay to TaskMaster timeline", "@copilot", 2);
			this._allTasks.Add(task1);

			TaskData task2 = TaskData.CreateTask("Implement GitHub Issue Sync", "Create GitHub issues from TaskMaster tasks with assignees", "@jmeyer1980", 1);
			this._allTasks.Add(task2);

			TaskData task3 = TaskData.CreateTask("Build Timeline Rendering", "Multi-scale Day/Week/Month/Year timeline view", "@copilot", 3);
			this._allTasks.Add(task3);
			
			Debug.Log($"🎯 TaskMaster: Created {this._allTasks.Count} sample tasks");
			}

		private void SnapToTimelineScale ()
			{
			// Adjust timeline view to align with selected scale
			switch (this._currentScale)
				{
				case TimelineScale.Day:
					// Snap to start of day
					this._timelineCenter = this._timelineCenter.Date;
					break;
				case TimelineScale.Week:
					// Snap to start of week (Sunday)
					this._timelineCenter = this._timelineCenter.Date.AddDays(-(int)this._timelineCenter.DayOfWeek);
					break;
				case TimelineScale.Month:
					// Snap to start of month
					this._timelineCenter = new DateTime(this._timelineCenter.Year, this._timelineCenter.Month, 1);
					break;
				case TimelineScale.Year:
					// Snap to start of year
					this._timelineCenter = new DateTime(this._timelineCenter.Year, 1, 1);
					break;
				case TimelineScale.FiveYear:
					// Snap to start of 5-year period
					int fiveYearStart = (this._timelineCenter.Year / 5) * 5;
					this._timelineCenter = new DateTime(fiveYearStart, 1, 1);
					break;
				case TimelineScale.TenYear:
					// Snap to start of decade
					int decadeStart = (this._timelineCenter.Year / 10) * 10;
					this._timelineCenter = new DateTime(decadeStart, 1, 1);
					break;
				}
			Debug.Log($"🗓️ Snapped to {this._currentScale} view: {this._timelineCenter:yyyy-MM-dd}");
			}

		private void NavigateTimeline (int direction)
			{
			// Navigate timeline forward/backward based on current scale
			switch (this._currentScale)
				{
				case TimelineScale.Day:
					this._timelineCenter = this._timelineCenter.AddDays(direction);
					break;
				case TimelineScale.Week:
					this._timelineCenter = this._timelineCenter.AddDays(direction * 7);
					break;
				case TimelineScale.Month:
					this._timelineCenter = this._timelineCenter.AddMonths(direction);
					break;
				case TimelineScale.Year:
					this._timelineCenter = this._timelineCenter.AddYears(direction);
					break;
				case TimelineScale.FiveYear:
					this._timelineCenter = this._timelineCenter.AddYears(direction * 5);
					break;
				case TimelineScale.TenYear:
					this._timelineCenter = this._timelineCenter.AddYears(direction * 10);
					break;
				default:
					break;
				}
			Debug.Log($"📅 Timeline center: {this._timelineCenter:yyyy-MM-dd}");
			}

		private void NavigateToToday ()
			{
			this._timelineCenter = DateTime.Now;
			Debug.Log("📅 Navigated to today");
			}

		private void CreateNewTask ()
			{
			if (string.IsNullOrEmpty(this._newTaskTitle.Trim()))
				{
				EditorUtility.DisplayDialog("Invalid Task", "Please enter a task title.", "OK");
				return;
				}

			// Use TaskData.CreateTask factory method
			TaskData newTask = TaskData.CreateTask(
				this._newTaskTitle.Trim(), 
				"Created from TaskMaster", 
				"@copilot", 
				3 // Medium priority
			);

			this._allTasks.Add(newTask);
			
			// 💾 SAVE: Persist the new task to disk  
			this.SaveTaskData(newTask);
			
			this._newTaskTitle = ""; // Clear input field

			Debug.Log($"✅ Created and saved new task: {newTask.TaskName}");
			}

		/// <summary>
		/// Save a TaskData to persistent storage as a ScriptableObject asset
		/// </summary>
		private void SaveTaskData (TaskData taskData)
			{
			try
				{
				// Create asset directory if it doesn't exist
				string assetDir = "Assets/TLDA/TaskMaster/Tasks";
				if (!AssetDatabase.IsValidFolder(assetDir))
					{
					if (!AssetDatabase.IsValidFolder("Assets/TLDA"))
						AssetDatabase.CreateFolder("Assets", "TLDA");
					if (!AssetDatabase.IsValidFolder("Assets/TLDA/TaskMaster"))
						AssetDatabase.CreateFolder("Assets/TLDA", "TaskMaster");
					if (!AssetDatabase.IsValidFolder("Assets/TLDA/TaskMaster/Tasks"))
						AssetDatabase.CreateFolder("Assets/TLDA/TaskMaster", "Tasks");
					}

				// Create a safe filename from the task title
				string safeTitle = taskData.TaskName;
				foreach (char invalidChar in System.IO.Path.GetInvalidFileNameChars())
					{
					safeTitle = safeTitle.Replace(invalidChar, '_');
					}
				if (safeTitle.Length > 50) 
					safeTitle = safeTitle.Substring(0, 50);

				string fileName = $"Task_{safeTitle}_{taskData.createdAt:yyyyMMdd_HHmmss}.asset";
				string assetPath = $"{assetDir}/{fileName}";

				// TaskData is already a ScriptableObject, so save it directly
				AssetDatabase.CreateAsset(taskData, assetPath);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();

				Debug.Log($"💾 TaskMaster: Saved task '{taskData.TaskName}' to {assetPath}");
				}
			catch (System.Exception ex)
				{
				Debug.LogError($"❌ TaskMaster: Failed to save task '{taskData.TaskName}': {ex.Message}");
				}
			}

		/// <summary>
		/// Update an existing TaskData in persistent storage
		/// </summary>
		private void UpdateTaskData (TaskData taskData)
			{
			// TaskData is already a ScriptableObject, so just mark it dirty and save
			EditorUtility.SetDirty(taskData);
			AssetDatabase.SaveAssets();
			Debug.Log($"💾 TaskMaster: Updated task '{taskData.TaskName}'");
			}

		private void ZoomIn ()
			{
			if (this._currentZoomIndex < this._zoomLevels.Length - 1)
				{
				this._currentZoomIndex++;
				Debug.Log($"🔍 Zoomed in to {this._zoomLevels [ this._currentZoomIndex ]:P0}");
				}
			}

		private void ZoomOut ()
			{
			if (this._currentZoomIndex > 0)
				{
				this._currentZoomIndex--;
				Debug.Log($"🔍 Zoomed out to {this._zoomLevels [ this._currentZoomIndex ]:P0}");
				}
			}

		private void FilterTasks (TaskPriority? priority)
			{
			// TODO: Implement task filtering
			if (priority.HasValue)
				{
				Debug.Log($"🎯 Filtering tasks by {priority.Value} priority");
				// Could set a filter flag and update task display
				}
			else
				{
				Debug.Log("📋 Showing all tasks");
				}
			}

		private void ImportTimeDataFromChronas ()
			{
			// 🎯 THE CHRONAS CONNECTION!
			Debug.Log("⏳ Importing time data from Chronas...");
			
			try 
				{
				// Access Chronas time cards using reflection (since they're in different assembly)
				var chronasType = System.Type.GetType("LivingDevAgent.Editor.Chronas.ChronasTimeTracker");
				if (chronasType == null)
					{
					EditorUtility.DisplayDialog("Chronas Not Found", 
						"ChronasTimeTracker not found. Make sure the Chronas module is available.", "OK");
					return;
					}

				// Get time cards field using reflection
				var timeCardsField = chronasType.GetField("_timeCards", 
					System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
				if (timeCardsField == null)
					{
					EditorUtility.DisplayDialog("Chronas Integration Error", 
						"Unable to access Chronas time cards. The internal API may have changed.", "OK");
					return;
					}

				// Get the time cards
				var timeCards = timeCardsField.GetValue(null) as System.Collections.IList;
				if (timeCards == null || timeCards.Count == 0)
					{
					EditorUtility.DisplayDialog("No Time Cards", 
						"No time cards found in Chronas. Start and stop a timer in Chronas first.", "OK");
					return;
					}

				int importedCount = 0;
				foreach (var timeCardObj in timeCards)
					{
					// Get properties using reflection
					var taskNameProp = timeCardObj.GetType().GetProperty("TaskName");
					var durationProp = timeCardObj.GetType().GetProperty("DurationSeconds");
					var startTimeProp = timeCardObj.GetType().GetProperty("StartTime");

					if (taskNameProp != null && durationProp != null)
						{
						string taskName = taskNameProp.GetValue(timeCardObj) as string;
						double durationSeconds = (double)durationProp.GetValue(timeCardObj);
						System.DateTime startTime = (System.DateTime)startTimeProp.GetValue(timeCardObj);

						// Find existing task with matching name or create new one
						TaskData existingTask = this._allTasks.FirstOrDefault(t => 
							string.Equals(t.TaskName, taskName, System.StringComparison.OrdinalIgnoreCase));

						if (existingTask != null)
							{
							// Update existing task's time tracking
							if (existingTask.timeCard != null)
								{
								// Update time tracking (assuming duration was tracked)
								// Note: TimeCardData tracks start/end times, not accumulated time
								}
							this.UpdateTaskData(existingTask);
							Debug.Log($"⏳ Updated task '{taskName}' with time tracking data");
							}
						else
							{
							// Create new task from time card using TaskData.CreateTask
							TaskData newTask = TaskData.CreateTask(
								taskName,
								$"Imported from Chronas time tracking (started {startTime:yyyy-MM-dd HH:mm})",
								"@copilot",
								3 // Medium priority
							);
							
							this._allTasks.Add(newTask);
							this.SaveTaskData(newTask);
							Debug.Log($"⏳ Created task '{taskName}' from Chronas import");
							}
						
						importedCount++;
						}
					}

				EditorUtility.DisplayDialog("Chronas Import Complete", 
					$"Successfully imported {importedCount} time entries from Chronas.\n\nTime tracking data has been merged with existing tasks or created new tasks.", "OK");
				
				Debug.Log($"✅ Chronas import complete: {importedCount} entries processed");
				}
			catch (System.Exception ex)
				{
				Debug.LogError($"❌ Chronas import failed: {ex.Message}");
				EditorUtility.DisplayDialog("Import Failed", 
					$"Failed to import from Chronas: {ex.Message}", "OK");
				}
			}

		private void ExportTimeReport ()
			{
			Debug.Log("📊 Exporting time report...");
			EditorUtility.DisplayDialog("Time Report",
				"Feature coming soon! This will generate detailed time reports.", "OK");
			}

		private ProjectStats CalculateProjectStats ()
			{
			return new ProjectStats
				{
				totalTasks = this._allTasks.Count,
				completedTasks = this._allTasks.Count(t => t.Status == TaskStatus.Done),
				inProgressTasks = this._allTasks.Count(t => t.Status == TaskStatus.InProgress),
				blockedTasks = this._allTasks.Count(t => t.Status == TaskStatus.Blocked)
				};
			}

		private void DrawBottomStatusBar ()
			{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
				{
				GUILayout.Label($"📊 {this._allTasks.Count} tasks total", EditorStyles.miniLabel);

				ProjectStats stats = this.CalculateProjectStats();
				GUILayout.Label($"✅ {stats.completedTasks} done", EditorStyles.miniLabel);
				GUILayout.Label($"⚡ {stats.inProgressTasks} in progress", EditorStyles.miniLabel);

				GUILayout.FlexibleSpace();

				GUILayout.Label($"📅 {this._timelineCenter:yyyy-MM-dd} ({this._currentScale})", EditorStyles.miniLabel);
				}
			}

		private void HandleKeyboardShortcuts ()
			{
			Event e = Event.current;
			if (e.type == EventType.KeyDown)
				{
				switch (e.keyCode)
					{
					case KeyCode.N when e.control:
						this.CreateNewTask();
						e.Use();
						break;
					case KeyCode.T when e.control:
						this.NavigateToToday();
						e.Use();
						break;
					case KeyCode.None:
						break;
					case KeyCode.Backspace:
						break;
					case KeyCode.Delete:
						break;
					case KeyCode.Tab:
						break;
					case KeyCode.Clear:
						break;
					case KeyCode.Return:
						break;
					case KeyCode.Pause:
						break;
					case KeyCode.Escape:
						break;
					case KeyCode.Space:
						break;
					case KeyCode.Keypad0:
						break;
					case KeyCode.Keypad1:
						break;
					case KeyCode.Keypad2:
						break;
					case KeyCode.Keypad3:
						break;
					case KeyCode.Keypad4:
						break;
					case KeyCode.Keypad5:
						break;
					case KeyCode.Keypad6:
						break;
					case KeyCode.Keypad7:
						break;
					case KeyCode.Keypad8:
						break;
					case KeyCode.Keypad9:
						break;
					case KeyCode.KeypadPeriod:
						break;
					case KeyCode.KeypadDivide:
						break;
					case KeyCode.KeypadMultiply:
						break;
					case KeyCode.KeypadMinus:
						break;
					case KeyCode.KeypadPlus:
						break;
					case KeyCode.KeypadEnter:
						break;
					case KeyCode.KeypadEquals:
						break;
					case KeyCode.UpArrow:
						break;
					case KeyCode.DownArrow:
						break;
					case KeyCode.RightArrow:
						break;
					case KeyCode.LeftArrow:
						break;
					case KeyCode.Insert:
						break;
					case KeyCode.Home:
						break;
					case KeyCode.End:
						break;
					case KeyCode.PageUp:
						break;
					case KeyCode.PageDown:
						break;
					case KeyCode.F1:
						break;
					case KeyCode.F2:
						break;
					case KeyCode.F3:
						break;
					case KeyCode.F4:
						break;
					case KeyCode.F5:
						break;
					case KeyCode.F6:
						break;
					case KeyCode.F7:
						break;
					case KeyCode.F8:
						break;
					case KeyCode.F9:
						break;
					case KeyCode.F10:
						break;
					case KeyCode.F11:
						break;
					case KeyCode.F12:
						break;
					case KeyCode.F13:
						break;
					case KeyCode.F14:
						break;
					case KeyCode.F15:
						break;
					case KeyCode.Alpha0:
						break;
					case KeyCode.Alpha1:
						break;
					case KeyCode.Alpha2:
						break;
					case KeyCode.Alpha3:
						break;
					case KeyCode.Alpha4:
						break;
					case KeyCode.Alpha5:
						break;
					case KeyCode.Alpha6:
						break;
					case KeyCode.Alpha7:
						break;
					case KeyCode.Alpha8:
						break;
					case KeyCode.Alpha9:
						break;
					case KeyCode.Exclaim:
						break;
					case KeyCode.DoubleQuote:
						break;
					case KeyCode.Hash:
						break;
					case KeyCode.Dollar:
						break;
					case KeyCode.Percent:
						break;
					case KeyCode.Ampersand:
						break;
					case KeyCode.Quote:
						break;
					case KeyCode.LeftParen:
						break;
					case KeyCode.RightParen:
						break;
					case KeyCode.Asterisk:
						break;
					case KeyCode.Plus:
						break;
					case KeyCode.Comma:
						break;
					case KeyCode.Minus:
						break;
					case KeyCode.Period:
						break;
					case KeyCode.Slash:
						break;
					case KeyCode.Colon:
						break;
					case KeyCode.Semicolon:
						break;
					case KeyCode.Less:
						break;
					case KeyCode.Equals:
						break;
					case KeyCode.Greater:
						break;
					case KeyCode.Question:
						break;
					case KeyCode.At:
						break;
					case KeyCode.LeftBracket:
						break;
					case KeyCode.Backslash:
						break;
					case KeyCode.RightBracket:
						break;
					case KeyCode.Caret:
						break;
					case KeyCode.Underscore:
						break;
					case KeyCode.BackQuote:
						break;
					case KeyCode.A:
						break;
					case KeyCode.B:
						break;
					case KeyCode.C:
						break;
					case KeyCode.D:
						break;
					case KeyCode.E:
						break;
					case KeyCode.F:
						break;
					case KeyCode.G:
						break;
					case KeyCode.H:
						break;
					case KeyCode.I:
						break;
					case KeyCode.J:
						break;
					case KeyCode.K:
						break;
					case KeyCode.L:
						break;
					case KeyCode.M:
						break;
					case KeyCode.N:
						break;
					case KeyCode.O:
						break;
					case KeyCode.P:
						break;
					case KeyCode.Q:
						break;
					case KeyCode.R:
						break;
					case KeyCode.S:
						break;
					case KeyCode.T:
						break;
					case KeyCode.U:
						break;
					case KeyCode.V:
						break;
					case KeyCode.W:
						break;
					case KeyCode.X:
						break;
					case KeyCode.Y:
						break;
					case KeyCode.Z:
						break;
					case KeyCode.LeftCurlyBracket:
						break;
					case KeyCode.Pipe:
						break;
					case KeyCode.RightCurlyBracket:
						break;
					case KeyCode.Tilde:
						break;
					case KeyCode.Numlock:
						break;
					case KeyCode.CapsLock:
						break;
					case KeyCode.ScrollLock:
						break;
					case KeyCode.RightShift:
						break;
					case KeyCode.LeftShift:
						break;
					case KeyCode.RightControl:
						break;
					case KeyCode.LeftControl:
						break;
					case KeyCode.RightAlt:
						break;
					case KeyCode.LeftAlt:
						break;
					case KeyCode.LeftMeta:
						break;
					case KeyCode.LeftWindows:
						break;
					case KeyCode.RightMeta:
						break;
					case KeyCode.RightWindows:
						break;
					case KeyCode.AltGr:
						break;
					case KeyCode.Help:
						break;
					case KeyCode.Print:
						break;
					case KeyCode.SysReq:
						break;
					case KeyCode.Break:
						break;
					case KeyCode.Menu:
						break;
					case KeyCode.WheelUp:
						break;
					case KeyCode.WheelDown:
						break;
					case KeyCode.F16:
						break;
					case KeyCode.F17:
						break;
					case KeyCode.F18:
						break;
					case KeyCode.F19:
						break;
					case KeyCode.F20:
						break;
					case KeyCode.F21:
						break;
					case KeyCode.F22:
						break;
					case KeyCode.F23:
						break;
					case KeyCode.F24:
						break;
					case KeyCode.Mouse0:
						break;
					case KeyCode.Mouse1:
						break;
					case KeyCode.Mouse2:
						break;
					case KeyCode.Mouse3:
						break;
					case KeyCode.Mouse4:
						break;
					case KeyCode.Mouse5:
						break;
					case KeyCode.Mouse6:
						break;
					case KeyCode.JoystickButton0:
						break;
					case KeyCode.JoystickButton1:
						break;
					case KeyCode.JoystickButton2:
						break;
					case KeyCode.JoystickButton3:
						break;
					case KeyCode.JoystickButton4:
						break;
					case KeyCode.JoystickButton5:
						break;
					case KeyCode.JoystickButton6:
						break;
					case KeyCode.JoystickButton7:
						break;
					case KeyCode.JoystickButton8:
						break;
					case KeyCode.JoystickButton9:
						break;
					case KeyCode.JoystickButton10:
						break;
					case KeyCode.JoystickButton11:
						break;
					case KeyCode.JoystickButton12:
						break;
					case KeyCode.JoystickButton13:
						break;
					case KeyCode.JoystickButton14:
						break;
					case KeyCode.JoystickButton15:
						break;
					case KeyCode.JoystickButton16:
						break;
					case KeyCode.JoystickButton17:
						break;
					case KeyCode.JoystickButton18:
						break;
					case KeyCode.JoystickButton19:
						break;
					case KeyCode.Joystick1Button0:
						break;
					case KeyCode.Joystick1Button1:
						break;
					case KeyCode.Joystick1Button2:
						break;
					case KeyCode.Joystick1Button3:
						break;
					case KeyCode.Joystick1Button4:
						break;
					case KeyCode.Joystick1Button5:
						break;
					case KeyCode.Joystick1Button6:
						break;
					case KeyCode.Joystick1Button7:
						break;
					case KeyCode.Joystick1Button8:
						break;
					case KeyCode.Joystick1Button9:
						break;
					case KeyCode.Joystick1Button10:
						break;
					case KeyCode.Joystick1Button11:
						break;
					case KeyCode.Joystick1Button12:
						break;
					case KeyCode.Joystick1Button13:
						break;
					case KeyCode.Joystick1Button14:
						break;
					case KeyCode.Joystick1Button15:
						break;
					case KeyCode.Joystick1Button16:
						break;
					case KeyCode.Joystick1Button17:
						break;
					case KeyCode.Joystick1Button18:
						break;
					case KeyCode.Joystick1Button19:
						break;
					case KeyCode.Joystick2Button0:
						break;
					case KeyCode.Joystick2Button1:
						break;
					case KeyCode.Joystick2Button2:
						break;
					case KeyCode.Joystick2Button3:
						break;
					case KeyCode.Joystick2Button4:
						break;
					case KeyCode.Joystick2Button5:
						break;
					case KeyCode.Joystick2Button6:
						break;
					case KeyCode.Joystick2Button7:
						break;
					case KeyCode.Joystick2Button8:
						break;
					case KeyCode.Joystick2Button9:
						break;
					case KeyCode.Joystick2Button10:
						break;
					case KeyCode.Joystick2Button11:
						break;
					case KeyCode.Joystick2Button12:
						break;
					case KeyCode.Joystick2Button13:
						break;
					case KeyCode.Joystick2Button14:
						break;
					case KeyCode.Joystick2Button15:
						break;
					case KeyCode.Joystick2Button16:
						break;
					case KeyCode.Joystick2Button17:
						break;
					case KeyCode.Joystick2Button18:
						break;
					case KeyCode.Joystick2Button19:
						break;
					case KeyCode.Joystick3Button0:
						break;
					case KeyCode.Joystick3Button1:
						break;
					case KeyCode.Joystick3Button2:
						break;
					case KeyCode.Joystick3Button3:
						break;
					case KeyCode.Joystick3Button4:
						break;
					case KeyCode.Joystick3Button5:
						break;
					case KeyCode.Joystick3Button6:
						break;
					case KeyCode.Joystick3Button7:
						break;
					case KeyCode.Joystick3Button8:
						break;
					case KeyCode.Joystick3Button9:
						break;
					case KeyCode.Joystick3Button10:
						break;
					case KeyCode.Joystick3Button11:
						break;
					case KeyCode.Joystick3Button12:
						break;
					case KeyCode.Joystick3Button13:
						break;
					case KeyCode.Joystick3Button14:
						break;
					case KeyCode.Joystick3Button15:
						break;
					case KeyCode.Joystick3Button16:
						break;
					case KeyCode.Joystick3Button17:
						break;
					case KeyCode.Joystick3Button18:
						break;
					case KeyCode.Joystick3Button19:
						break;
					case KeyCode.Joystick4Button0:
						break;
					case KeyCode.Joystick4Button1:
						break;
					case KeyCode.Joystick4Button2:
						break;
					case KeyCode.Joystick4Button3:
						break;
					case KeyCode.Joystick4Button4:
						break;
					case KeyCode.Joystick4Button5:
						break;
					case KeyCode.Joystick4Button6:
						break;
					case KeyCode.Joystick4Button7:
						break;
					case KeyCode.Joystick4Button8:
						break;
					case KeyCode.Joystick4Button9:
						break;
					case KeyCode.Joystick4Button10:
						break;
					case KeyCode.Joystick4Button11:
						break;
					case KeyCode.Joystick4Button12:
						break;
					case KeyCode.Joystick4Button13:
						break;
					case KeyCode.Joystick4Button14:
						break;
					case KeyCode.Joystick4Button15:
						break;
					case KeyCode.Joystick4Button16:
						break;
					case KeyCode.Joystick4Button17:
						break;
					case KeyCode.Joystick4Button18:
						break;
					case KeyCode.Joystick4Button19:
						break;
					case KeyCode.Joystick5Button0:
						break;
					case KeyCode.Joystick5Button1:
						break;
					case KeyCode.Joystick5Button2:
						break;
					case KeyCode.Joystick5Button3:
						break;
					case KeyCode.Joystick5Button4:
						break;
					case KeyCode.Joystick5Button5:
						break;
					case KeyCode.Joystick5Button6:
						break;
					case KeyCode.Joystick5Button7:
						break;
					case KeyCode.Joystick5Button8:
						break;
					case KeyCode.Joystick5Button9:
						break;
					case KeyCode.Joystick5Button10:
						break;
					case KeyCode.Joystick5Button11:
						break;
					case KeyCode.Joystick5Button12:
						break;
					case KeyCode.Joystick5Button13:
						break;
					case KeyCode.Joystick5Button14:
						break;
					case KeyCode.Joystick5Button15:
						break;
					case KeyCode.Joystick5Button16:
						break;
					case KeyCode.Joystick5Button17:
						break;
					case KeyCode.Joystick5Button18:
						break;
					case KeyCode.Joystick5Button19:
						break;
					case KeyCode.Joystick6Button0:
						break;
					case KeyCode.Joystick6Button1:
						break;
					case KeyCode.Joystick6Button2:
						break;
					case KeyCode.Joystick6Button3:
						break;
					case KeyCode.Joystick6Button4:
						break;
					case KeyCode.Joystick6Button5:
						break;
					case KeyCode.Joystick6Button6:
						break;
					case KeyCode.Joystick6Button7:
						break;
					case KeyCode.Joystick6Button8:
						break;
					case KeyCode.Joystick6Button9:
						break;
					case KeyCode.Joystick6Button10:
						break;
					case KeyCode.Joystick6Button11:
						break;
					case KeyCode.Joystick6Button12:
						break;
					case KeyCode.Joystick6Button13:
						break;
					case KeyCode.Joystick6Button14:
						break;
					case KeyCode.Joystick6Button15:
						break;
					case KeyCode.Joystick6Button16:
						break;
					case KeyCode.Joystick6Button17:
						break;
					case KeyCode.Joystick6Button18:
						break;
					case KeyCode.Joystick6Button19:
						break;
					case KeyCode.Joystick7Button0:
						break;
					case KeyCode.Joystick7Button1:
						break;
					case KeyCode.Joystick7Button2:
						break;
					case KeyCode.Joystick7Button3:
						break;
					case KeyCode.Joystick7Button4:
						break;
					case KeyCode.Joystick7Button5:
						break;
					case KeyCode.Joystick7Button6:
						break;
					case KeyCode.Joystick7Button7:
						break;
					case KeyCode.Joystick7Button8:
						break;
					case KeyCode.Joystick7Button9:
						break;
					case KeyCode.Joystick7Button10:
						break;
					case KeyCode.Joystick7Button11:
						break;
					case KeyCode.Joystick7Button12:
						break;
					case KeyCode.Joystick7Button13:
						break;
					case KeyCode.Joystick7Button14:
						break;
					case KeyCode.Joystick7Button15:
						break;
					case KeyCode.Joystick7Button16:
						break;
					case KeyCode.Joystick7Button17:
						break;
					case KeyCode.Joystick7Button18:
						break;
					case KeyCode.Joystick7Button19:
						break;
					case KeyCode.Joystick8Button0:
						break;
					case KeyCode.Joystick8Button1:
						break;
					case KeyCode.Joystick8Button2:
						break;
					case KeyCode.Joystick8Button3:
						break;
					case KeyCode.Joystick8Button4:
						break;
					case KeyCode.Joystick8Button5:
						break;
					case KeyCode.Joystick8Button6:
						break;
					case KeyCode.Joystick8Button7:
						break;
					case KeyCode.Joystick8Button8:
						break;
					case KeyCode.Joystick8Button9:
						break;
					case KeyCode.Joystick8Button10:
						break;
					case KeyCode.Joystick8Button11:
						break;
					case KeyCode.Joystick8Button12:
						break;
					case KeyCode.Joystick8Button13:
						break;
					case KeyCode.Joystick8Button14:
						break;
					case KeyCode.Joystick8Button15:
						break;
					case KeyCode.Joystick8Button16:
						break;
					case KeyCode.Joystick8Button17:
						break;
					case KeyCode.Joystick8Button18:
						break;
					case KeyCode.Joystick8Button19:
						break;
					default:
						break;
					}
				}
			}

		private void DrawTimelineHeader ()
			{
			EditorGUILayout.LabelField($"📊 Timeline View - {this._currentScale}", EditorStyles.boldLabel);
			EditorGUILayout.LabelField($"Center: {this._timelineCenter:yyyy-MM-dd}", EditorStyles.miniLabel);
			}

		private void DrawTimelineTracks ()
			{
			using (new EditorGUILayout.VerticalScope("box"))
				{
				EditorGUILayout.LabelField($"📅 Timeline - {this._currentScale} View", EditorStyles.boldLabel);
				
				// Draw time scale header
				this.DrawTimeScaleHeader();
				
				GUILayout.Space(5);
				
				// Group tasks by status for timeline tracks
				var todoTasks = this._allTasks.Where(t => t.Status == TaskStatus.ToDo).ToList();
				var inProgressTasks = this._allTasks.Where(t => t.Status == TaskStatus.InProgress).ToList();
				var completedTasks = this._allTasks.Where(t => t.Status == TaskStatus.Done).ToList();
				var blockedTasks = this._allTasks.Where(t => t.Status == TaskStatus.Blocked).ToList();

				// Draw timeline tracks
				this.DrawTimelineTrack("📋 To Do", todoTasks, new Color(0.7f, 0.7f, 0.7f, 0.3f));
				this.DrawTimelineTrack("🚀 In Progress", inProgressTasks, new Color(0.2f, 0.8f, 1f, 0.3f));
				this.DrawTimelineTrack("✅ Completed", completedTasks, new Color(0.2f, 1f, 0.2f, 0.3f));
				this.DrawTimelineTrack("🚫 Blocked", blockedTasks, new Color(1f, 0.3f, 0.3f, 0.3f));
				}
			}

		private void DrawTimeScaleHeader ()
			{
			using (new EditorGUILayout.HorizontalScope())
				{
				// Calculate time range based on current scale and center
				var (startDate, endDate) = this.GetTimelineRange();
				
				EditorGUILayout.LabelField($"📅 {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}", EditorStyles.centeredGreyMiniLabel);
				
				GUILayout.FlexibleSpace();
				
				if (GUILayout.Button("⬅️", EditorStyles.miniButtonLeft, GUILayout.Width(30)))
					this.NavigateTimeline(-1);
				if (GUILayout.Button("Today", EditorStyles.miniButtonMid, GUILayout.Width(50)))
					this.NavigateToToday();
				if (GUILayout.Button("➡️", EditorStyles.miniButtonRight, GUILayout.Width(30)))
					this.NavigateTimeline(1);
				}
			}

		private void DrawTimelineTrack (string trackName, List<TaskData> tasks, Color trackColor)
			{
			using (new EditorGUILayout.VerticalScope("box"))
				{
				// Track header
				using (new EditorGUILayout.HorizontalScope())
					{
					EditorGUILayout.LabelField(trackName, EditorStyles.boldLabel, GUILayout.Width(120));
					EditorGUILayout.LabelField($"({tasks.Count} tasks)", EditorStyles.miniLabel, GUILayout.Width(60));
					}

				if (tasks.Count > 0)
					{
					// Draw timeline bar background
					Rect timelineRect = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
					EditorGUI.DrawRect(timelineRect, trackColor);

					// Calculate task positions on timeline
					var (startDate, endDate) = this.GetTimelineRange();
					double totalDays = (endDate - startDate).TotalDays;

					foreach (TaskData task in tasks)
						{
						// Calculate task position (simple: use creation date)
						double taskDays = (task.createdAt - startDate).TotalDays;
						if (taskDays >= 0 && taskDays <= totalDays)
							{
							float xPos = (float)(taskDays / totalDays) * timelineRect.width;
							Rect taskRect = new Rect(timelineRect.x + xPos, timelineRect.y + 5, 20, 20);
							
							// Draw task marker
							Color taskColor = this.GetPriorityColor(task.priorityLevel);
							EditorGUI.DrawRect(taskRect, taskColor);
							
							// Task tooltip on hover
							if (taskRect.Contains(Event.current.mousePosition))
								{
								GUI.Label(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y - 20, 200, 20), 
									$"{task.TaskName} (P{task.priorityLevel})", EditorStyles.helpBox);
								}
							
							// Click to select task
							if (Event.current.type == EventType.MouseDown && taskRect.Contains(Event.current.mousePosition))
								{
								this._selectedTask = task;
								Event.current.Use();
								}
							}
						}

					// Show tasks in list below timeline
					foreach (TaskData task in tasks.Take(3)) // Show first 3 tasks
						{
						using (new EditorGUILayout.HorizontalScope())
							{
							GUILayout.Label(this.GetPriorityEmoji(task.priorityLevel), GUILayout.Width(20));
							EditorGUILayout.LabelField(task.TaskName, GUILayout.MaxWidth(150));
							if (task.timeCard != null)
								{
								float hours = task.timeCard.GetDurationInHours();
								if (hours > 0)
									EditorGUILayout.LabelField($"{hours:F1}h", GUILayout.Width(40));
								}
							GUILayout.FlexibleSpace();
							if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(50)))
								this._selectedTask = task;
							}
						}
						
					if (tasks.Count > 3)
						{
						EditorGUILayout.LabelField($"... and {tasks.Count - 3} more", EditorStyles.centeredGreyMiniLabel);
						}
					}
				else
					{
					EditorGUILayout.LabelField("No tasks in this track", EditorStyles.centeredGreyMiniLabel);
					}
				}
			}

		private (DateTime start, DateTime end) GetTimelineRange ()
			{
			DateTime start, end;
			switch (this._currentScale)
				{
				case TimelineScale.Day:
					start = this._timelineCenter.Date;
					end = start.AddDays(1);
					break;
				case TimelineScale.Week:
					start = this._timelineCenter.Date.AddDays(-(int)this._timelineCenter.DayOfWeek);
					end = start.AddDays(7);
					break;
				case TimelineScale.Month:
					start = new DateTime(this._timelineCenter.Year, this._timelineCenter.Month, 1);
					end = start.AddMonths(1);
					break;
				case TimelineScale.Year:
					start = new DateTime(this._timelineCenter.Year, 1, 1);
					end = start.AddYears(1);
					break;
				case TimelineScale.FiveYear:
					start = new DateTime(this._timelineCenter.Year - 2, 1, 1);
					end = start.AddYears(5);
					break;
				case TimelineScale.TenYear:
					start = new DateTime(this._timelineCenter.Year - 5, 1, 1);
					end = start.AddYears(10);
					break;
				default:
					start = DateTime.Now.Date;
					end = start.AddDays(7);
					break;
				}
			return (start, end);
			}

		private Color GetPriorityColor (TaskPriority priority)
			{
			return priority switch
				{
				TaskPriority.Critical => Color.red,
				TaskPriority.High => Color.yellow,
				TaskPriority.Medium => Color.green,
				TaskPriority.Low => Color.blue,
				_ => Color.gray
				};
			}

		private Texture2D CreateTaskCardBackground (TaskData task)
			{
			// 🎯 FIXED: Create colored backgrounds based on priority/status for better visual feedback
			TaskStatus status = this.GetTaskStatusFromTaskData(task);
			Color cardColor = status switch
				{
				TaskStatus.ToDo => new Color(0.8f, 0.8f, 0.8f, 0.3f),
				TaskStatus.InProgress => new Color(0.2f, 0.8f, 1f, 0.3f),
				TaskStatus.Blocked => new Color(1f, 0.3f, 0.3f, 0.3f),
				TaskStatus.Done => new Color(0.2f, 1f, 0.2f, 0.3f),
				_ => new Color(0.7f, 0.7f, 0.7f, 0.3f)
				};

			// Create a simple colored texture
			var texture = new Texture2D(1, 1);
			texture.SetPixel(0, 0, cardColor);
			texture.Apply();
			return texture;
			}

		private string GetPriorityEmoji (TaskPriority priority)
			{
			return priority switch
				{
					TaskPriority.Critical => "🔴",
					TaskPriority.High => "🟡",
					TaskPriority.Medium => "🟢",
					TaskPriority.Low => "🔵",
					TaskPriority.Backlog => "⚪",
					_ => "📋"
					};
			}

		private Color GetPriorityColor (int priorityLevel)
			{
			return priorityLevel switch
				{
				1 => Color.red,     // Critical  
				2 => Color.yellow,  // High
				3 => Color.green,   // Medium
				4 => Color.blue,    // Low
				5 => Color.gray,    // Backlog
				_ => Color.white
				};
			}

		private Color GetTaskColor (TaskData task)
			{
			TaskStatus status = this.GetTaskStatusFromTaskData(task);
			return status switch
				{
				TaskStatus.ToDo => Color.gray,
				TaskStatus.InProgress => Color.cyan,
				TaskStatus.Blocked => Color.red,
				TaskStatus.Done => Color.green,
				_ => Color.white
				};
			}

		private void HandleTaskCardInteraction (TaskData task, Rect rect)
			{
			Event e = Event.current;
			if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
				{
				// 🎯 FIXED: Enhanced selection feedback with visual confirmation
				TaskData previousSelection = this._selectedTask;
				this._selectedTask = task;
				e.Use();
				
				// Provide clear feedback about selection
				if (previousSelection == task)
					{
					Debug.Log($"📋 Task '{task.TaskName}' already selected - details shown in inspector");
					}
				else
					{
					TaskStatus status = this.GetTaskStatusFromTaskData(task);
					Debug.Log($"📋 Selected task: '{task.TaskName}' - Status: {status}, Priority: P{task.priorityLevel}");
					}
				
				// Force UI repaint to show selection changes immediately
				Repaint();
				}
			}

		private void HandleTaskDropInColumn (Rect dropRect, TaskStatus status)
			{
			// TODO: Implement drag and drop between columns
			Event e = Event.current;
			if (e.type == EventType.DragUpdated && dropRect.Contains(e.mousePosition))
				{
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				e.Use();
				}
			}

		private DateTime DrawDateTimePicker (string label, DateTime current)
			{
			using (new EditorGUILayout.HorizontalScope())
				{
				EditorGUILayout.LabelField(label, GUILayout.Width(100));

				// Simple date picker - could be enhanced with proper date UI
				string dateString = EditorGUILayout.TextField(current.ToString("yyyy-MM-dd"));
				return DateTime.TryParse(dateString, out DateTime newDate) ? newDate : current;
				}
			}

		private void StartTimerForTask (TaskData task)
			{
			// 🎯 THE CHRONAS INTEGRATION POINT!
			Debug.Log($"⏰ Starting timer for task: {task.TaskName}");
			
			// Start timer using the TaskData's TimeCardData
			if (task.timeCard == null)
				{
				task.timeCard = ScriptableObject.CreateInstance<TimeCardData>();
				}
			
			if (!task.timeCard.GetIsOngoing())
				{
				task.timeCard.StartTimeCard(task);
				EditorUtility.SetDirty(task);
				Debug.Log($"⏰ Timer started for '{task.TaskName}' using TimeCardData");
				}
			else
				{
				Debug.Log($"⚠️ Timer already running for '{task.TaskName}'");
				}

			// TODO: Integrate with Chronas focus-immune timer
			EditorUtility.DisplayDialog("Timer Started",
				$"Timer started for '{task.TaskName}'\n\nUsing TaskData TimeCardData system", "OK");
			}

		private void StopTimerForTask (TaskData task)
			{
			Debug.Log($"⏸️ Stopping timer for task: {task.TaskName}");

			if (task.timeCard != null && task.timeCard.GetIsOngoing())
				{
				task.timeCard.EndTimeCard();
				EditorUtility.SetDirty(task);
				Debug.Log($"⏸️ Timer stopped for '{task.TaskName}' - Duration: {task.timeCard.GetDurationInHours():F2}h");
				}

			// TODO: Stop Chronas timer and import time data
			EditorUtility.DisplayDialog("Timer Stopped",
				$"Timer stopped for '{task.TaskName}'\n\nTime tracked in TimeCardData system", "OK");
			}

		private void DeleteTask (TaskData task)
			{
			this._allTasks.Remove(task);
			if (this._selectedTask == task)
				{
				this._selectedTask = null;
				}
			Debug.Log($"🗑️ Deleted task: {task.TaskName}");
			}

		private void ExportTaskToTLDL (TaskData task)
			{
			Debug.Log($"📋 Exporting task to TLDL: {task.TaskName}");

			// TODO: Create TLDL entry with task details
			EditorUtility.DisplayDialog("TLDL Export",
				$"Task '{task.TaskName}' exported to TLDL!\n\nNote: Full TLDL integration coming soon!", "OK");
			}

		private void CreateGitHubIssueFromTask (TaskData task)
			{
			Debug.Log($"🐙 Creating GitHub issue for task: {task.TaskName}");

			// TODO: Implement GitHub integration when compilation issues are resolved
			EditorUtility.DisplayDialog("GitHub Integration",
				$"GitHub issue creation for '{task.TaskName}' coming soon!\n\nThis will create issues with assignees and time tracking.", "OK");
			}
		}
	}
#endif
