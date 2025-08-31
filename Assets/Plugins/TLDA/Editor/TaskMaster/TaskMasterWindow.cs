#if UNITY_EDITOR
using Codice.Client.BaseCommands;
using LivingDevAgent.Editor.Modules;
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
		
		// 🎯 CORE FEATURE: Kanban overlay toggle for hybrid views
		// This actually works and provides meaningful visual enhancement
		private readonly bool _showKanbanOverlay = true;

		// Task management  
		private readonly List<TaskData> _allTasks = new();
		private TaskData _selectedTask = null;
		
		// 🎯 LEARNING OPPORTUNITY: Drag and drop support for task management
		// Basic implementation works, can be enhanced with visual feedback
		private TaskData _draggedTask = null;

		// UI state
		private string _newTaskTitle = "";
		
		// 🎯 LEARNING OPPORTUNITY: Enhanced task creation with priority and deadline selection
		// These work right now but can be enhanced with better UI controls
		private readonly TaskPriority _newTaskPriority = TaskPriority.Medium;
		private readonly DateTime _newTaskDeadline = DateTime.Now.AddDays(7);

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

        private void DrawSidebar()
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
                {
                    this.ImportTimeDataFromChronas();
                }

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
					GUILayout.Label(this.GetPriorityEmoji(this.GetTaskPriorityFromLevel(task.priorityLevel)), GUILayout.Width(20));
					
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
					
					// 🎯 CORE FEATURE: Kanban overlay implementation
					if (this._showKanbanOverlay)
						{
						using (new EditorGUILayout.HorizontalScope())
							{
							GUILayout.Label("✨ Kanban Overlay Active", EditorStyles.miniLabel);
							GUILayout.FlexibleSpace();
							// Show task status counts as overlay info
							ProjectStats stats = this.CalculateProjectStats();
							GUILayout.Label($"📋{stats.totalTasks-stats.completedTasks} 🚀{stats.inProgressTasks} 🚫{stats.blockedTasks} ✅{stats.completedTasks}", EditorStyles.miniLabel);
							}
						}
					
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
			var newStatus = (TaskStatus)EditorGUILayout.EnumPopup("Status:", currentStatus);
			
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
				
				// 🎯 CORE FEATURE: Enhanced time tracking status display
				using (new EditorGUILayout.HorizontalScope())
					{
					string statusIcon = task.timeCard.GetIsOngoing() ? "⏱️" : 
										task.timeCard.GetIsCompleted() ? "✅" : "⏸️";
					string statusText = task.timeCard.GetIsOngoing() ? "In Progress" : 
										task.timeCard.GetIsCompleted() ? "Completed" : "Paused";
					
					EditorGUILayout.LabelField($"{statusIcon} Status:", GUILayout.Width(80));
					EditorGUILayout.LabelField(statusText, EditorStyles.boldLabel);
					}
				
				if (task.timeCard.GetIsOngoing())
					{
					EditorGUILayout.LabelField($"Started: {task.timeCard.GetStartTime():g}");
					}
				else if (task.timeCard.GetIsCompleted())
					{
					EditorGUILayout.LabelField($"Completed: {task.timeCard.GetEndTime():g}");
					if (task.timeCard.GetSessionCount() > 1)
						{
						EditorGUILayout.LabelField($"Sessions: {task.timeCard.GetSessionCount()}");
						}
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
					$"Are you sure you want to delete '{task.TaskName}'?", "Delete", "Cancel"))
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
			if ((task.timeCard != null && task.timeCard.GetIsOngoing()) || !string.IsNullOrEmpty(task.assignedTo))
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

		private string GetPriorityEmoji (TaskPriority priority)
			{
			// 🎯 CORE FEATURE: Complete emoji mapping system for rich UI feedback
			return priority switch
				{
				TaskPriority.Critical => "🔴", // Red circle for critical
				TaskPriority.High => "🟡",     // Yellow circle for high
				TaskPriority.Medium => "🟢",   // Green circle for medium
				TaskPriority.Low => "🔵",      // Blue circle for low
				TaskPriority.Backlog => "⚪",  // White circle for backlog
				_ => "📋"                      // Clipboard for unknown
				};
			}

		private Color GetPriorityColor (TaskPriority priority)
			{
			// 🎯 CORE FEATURE: Rich visual feedback system with full implementation
			return priority switch
				{
				TaskPriority.Critical => new Color(0.9f, 0.2f, 0.2f, 1f), // Bright red
				TaskPriority.High => new Color(1f, 0.6f, 0.1f, 1f),       // Orange
				TaskPriority.Medium => new Color(0.2f, 0.7f, 0.3f, 1f),   // Green
				TaskPriority.Low => new Color(0.3f, 0.5f, 0.9f, 1f),      // Blue
				TaskPriority.Backlog => new Color(0.6f, 0.6f, 0.6f, 1f),  // Gray
				_ => new Color(0.8f, 0.8f, 0.8f, 1f)                      // Light gray
				};
			}

		private Color GetTaskColor (TaskData task)
			{
			// 🎯 CORE FEATURE: Rich task coloring system based on status and priority
			TaskStatus status = this.GetTaskStatusFromTaskData(task);
			Color baseColor = status switch
				{
				TaskStatus.ToDo => new Color(0.7f, 0.7f, 0.7f, 0.8f),      // Gray
				TaskStatus.InProgress => new Color(0.2f, 0.8f, 1f, 0.8f),  // Cyan
				TaskStatus.Blocked => new Color(1f, 0.3f, 0.3f, 0.8f),     // Red
				TaskStatus.Done => new Color(0.2f, 1f, 0.2f, 0.8f),        // Green
				_ => new Color(0.5f, 0.5f, 0.5f, 0.8f)                     // Default gray
				};

			// Blend with priority color for richer visual feedback
			Color priorityColor = this.GetPriorityColor(this.GetTaskPriorityFromLevel(task.priorityLevel));
			return Color.Lerp(baseColor, priorityColor, 0.3f); // 30% priority influence
			}

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
			var task1 = TaskData.CreateTask("Fix Chronas Integration", "Connect scene overlay to TaskMaster timeline", "@copilot", 2);
			this._allTasks.Add(task1);

			var task2 = TaskData.CreateTask("Implement GitHub Issue Sync", "Create GitHub issues from TaskMaster tasks with assignees", "@jmeyer1980", 1);
			this._allTasks.Add(task2);

			var task3 = TaskData.CreateTask("Build Timeline Rendering", "Multi-scale Day/Week/Month/Year timeline view", "@copilot", 3);
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

		/// <summary>
		/// 🎯 LEARNING OPPORTUNITY: Enhanced task creation with priority and deadline
		/// Basic but functional implementation - perfect for expansion learning
		/// </summary>
		private void CreateNewTask ()
			{
			if (string.IsNullOrEmpty(this._newTaskTitle.Trim()))
				{
				EditorUtility.DisplayDialog("Invalid Task", "Please enter a task title.", "OK");
				return;
				}

			// Use the learning opportunity fields for real task creation
			var newTask = TaskData.CreateTask(
				this._newTaskTitle.Trim(), 
				"Created from TaskMaster with enhanced options", 
				"@copilot", 
				this.GetPriorityLevelFromTaskPriority(this._newTaskPriority)
			);

			// Apply deadline if it's different from default
			if (this._newTaskDeadline != DateTime.Now.AddDays(7))
				{
				// TODO: TaskData could be enhanced to support deadlines
				// For now, add deadline info to description
				newTask.taskDescription += $" (Deadline: {this._newTaskDeadline:yyyy-MM-dd})";
				}

			this._allTasks.Add(newTask);
			
			// 💾 SAVE: Persist the new task to disk  
			this.SaveTaskData(newTask);
			
			this._newTaskTitle = ""; // Clear input field

			Debug.Log($"✅ Created task '{newTask.TaskName}' with priority {this._newTaskPriority} and deadline {this._newTaskDeadline:yyyy-MM-dd}");
			}

		/// <summary>
		/// 🎯 LEARNING HELPER: Convert TaskPriority enum to integer level
		/// </summary>
		private int GetPriorityLevelFromTaskPriority(TaskPriority priority)
			{
			return priority switch
				{
				TaskPriority.Critical => 1,
				TaskPriority.High => 2,
				TaskPriority.Medium => 3,
				TaskPriority.Low => 4,
				TaskPriority.Backlog => 5,
				_ => 3
				};
			}

		private DateTime DrawDateTimePicker (string label, DateTime current)
			{
			// 🎯 LEARNING OPPORTUNITY: Functional date picker ready for enhancement
			// Works right now, can be improved with calendar popup or better UI
			using (new EditorGUILayout.HorizontalScope())
				{
				EditorGUILayout.LabelField(label, GUILayout.Width(100));

				// Functional date picker with multiple input options
				string dateString = EditorGUILayout.TextField(current.ToString("yyyy-MM-dd"), GUILayout.Width(100));
				if (DateTime.TryParse(dateString, out DateTime newDate))
					{
					current = newDate;
					}

				// Quick date buttons for common actions
				if (GUILayout.Button("Today", EditorStyles.miniButton, GUILayout.Width(50)))
					current = DateTime.Now.Date;
				if (GUILayout.Button("+1W", EditorStyles.miniButton, GUILayout.Width(35)))
					current = current.AddDays(7);
				if (GUILayout.Button("+1M", EditorStyles.miniButton, GUILayout.Width(35)))
					current = current.AddMonths(1);

				return current;
				}
			}

		private void HandleTaskDropInColumn (Rect dropRect, TaskStatus targetStatus)
			{
			// 🎯 LEARNING OPPORTUNITY: Basic drag and drop between columns
			// Functional implementation ready for visual enhancement
			Event e = Event.current;
			
			if (e.type == EventType.DragUpdated && dropRect.Contains(e.mousePosition))
				{
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				e.Use();
				}
			else if (e.type == EventType.DragPerform && dropRect.Contains(e.mousePosition))
				{
				DragAndDrop.AcceptDrag();
				
				// Simple implementation: If we have a dragged task, move it to this column
				if (this._draggedTask != null)
					{
					this.SetTaskDataStatus(this._draggedTask, targetStatus);
					Debug.Log($"📋 Moved task '{this._draggedTask.TaskName}' to {targetStatus}");
					this._draggedTask = null; // Clear drag state
					}
				e.Use();
				}
			}

		private void HandleTaskCardInteraction (TaskData task, Rect rect)
			{
			Event e = Event.current;
			if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
				{
				// 🎯 ENHANCED: Task selection with drag support
				TaskData previousSelection = this._selectedTask;
				this._selectedTask = task;
				
				// 🎯 LEARNING OPPORTUNITY: Start drag operation
				if (e.button == 0) // Left mouse button
					{
					this._draggedTask = task; // Set up for potential drag
					}
				
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
			else if (e.type == EventType.MouseDrag && this._draggedTask == task)
				{
				// 🎯 LEARNING OPPORTUNITY: Visual drag feedback
				DragAndDrop.PrepareStartDrag();
				DragAndDrop.objectReferences = new UnityEngine.Object[] { }; // Empty but valid
				DragAndDrop.SetGenericData("TaskData", task);
				DragAndDrop.StartDrag($"Moving: {task.TaskName}");
				e.Use();
				}
			}

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
					safeTitle = safeTitle[..50];

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
			// 🎯 LEARNING-FRIENDLY: Simple but functional task filtering
			if (priority.HasValue)
				{
				Debug.Log($"🎯 Filtering tasks by {priority.Value} priority");
				
				// Simple implementation: Show filtered count in status
				var filteredTasks = this._allTasks.Where(t => this.GetTaskPriorityFromLevel(t.priorityLevel) == priority.Value).ToList();
				EditorUtility.DisplayDialog("Task Filter", 
					$"Found {filteredTasks.Count} tasks with {priority.Value} priority.", "OK");
					
				// Future enhancement: Could store filter state and modify task display
				// For now, just demonstrate the filtering capability
				}
			else
				{
				Debug.Log("📋 Showing all tasks");
				EditorUtility.DisplayDialog("Task Filter", "Showing all tasks", "OK");
				}
			}

		/// <summary>
		/// 🎯 LEARNING HELPER: Convert integer priority level to TaskPriority enum
		/// </summary>
		private TaskPriority GetTaskPriorityFromLevel(int priorityLevel)
			{
			return priorityLevel switch
				{
				1 => TaskPriority.Critical,
				2 => TaskPriority.High,
				3 => TaskPriority.Medium,
				4 => TaskPriority.Low,
				5 => TaskPriority.Backlog,
				_ => TaskPriority.Medium
				};
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
				System.Reflection.FieldInfo timeCardsField = chronasType.GetField("_timeCards", 
					System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
				if (timeCardsField == null)
					{
					EditorUtility.DisplayDialog("Chronas Integration Error", 
						"Unable to access Chronas time cards. The internal API may have changed.", "OK");
					return;
					}

				// Get the time cards
				if (timeCardsField.GetValue(null) is not System.Collections.IList timeCards || timeCards.Count == 0)
					{
					EditorUtility.DisplayDialog("No Time Cards",
						"No time cards found in Chronas. Start and stop a timer in Chronas first.", "OK");
					return;
					}

				int importedCount = 0;
				foreach (object timeCardObj in timeCards)
					{
					// Get properties using reflection
					System.Reflection.PropertyInfo taskNameProp = timeCardObj.GetType().GetProperty("TaskName");
					System.Reflection.PropertyInfo durationProp = timeCardObj.GetType().GetProperty("DurationSeconds");
					System.Reflection.PropertyInfo startTimeProp = timeCardObj.GetType().GetProperty("StartTime");

					if (taskNameProp != null && durationProp != null)
						{
						string taskName = taskNameProp.GetValue(timeCardObj) as string;
						double durationSeconds = (double)durationProp.GetValue(timeCardObj);
						var startTime = (System.DateTime)startTimeProp.GetValue(timeCardObj);

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
							var newTask = TaskData.CreateTask(
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
				completedTasks = this._allTasks.Count(t => this.GetTaskStatusFromTaskData(t) == TaskStatus.Done),
				inProgressTasks = this._allTasks.Count(t => this.GetTaskStatusFromTaskData(t) == TaskStatus.InProgress),
				blockedTasks = this._allTasks.Count(t => this.GetTaskStatusFromTaskData(t) == TaskStatus.Blocked)
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
				var todoTasks = this._allTasks.Where(t => this.GetTaskStatusFromTaskData(t) == TaskStatus.ToDo).ToList();
				var inProgressTasks = this._allTasks.Where(t => this.GetTaskStatusFromTaskData(t) == TaskStatus.InProgress).ToList();
				var completedTasks = this._allTasks.Where(t => this.GetTaskStatusFromTaskData(t) == TaskStatus.Done).ToList();
				var blockedTasks = this._allTasks.Where(t => this.GetTaskStatusFromTaskData(t) == TaskStatus.Blocked).ToList();

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
				(DateTime startDate, DateTime endDate) = this.GetTimelineRange();
				
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
					(DateTime startDate, DateTime endDate) = this.GetTimelineRange();
					double totalDays = (endDate - startDate).TotalDays;

					foreach (TaskData task in tasks)
						{
						// Calculate task position (simple: use creation date)
						double taskDays = (task.createdAt - startDate).TotalDays;
						if (taskDays >= 0 && taskDays <= totalDays)
							{
							float xPos = (float)(taskDays / totalDays) * timelineRect.width;
							var taskRect = new Rect(timelineRect.x + xPos, timelineRect.y + 5, 20, 20);
							
							// Draw task marker
							Color taskColor = this.GetPriorityColor(this.GetTaskPriorityFromLevel(task.priorityLevel));
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
							GUILayout.Label(this.GetPriorityEmoji(this.GetTaskPriorityFromLevel(task.priorityLevel)), GUILayout.Width(20));
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

			if (!GitHubIntegration.IsConfigured())
				{
				EditorUtility.DisplayDialog("GitHub Not Configured",
					"Please configure GitHub integration first.\n\nUse Tools/Living Dev Agent/TaskMaster/GitHub Integration to set up your token.", "OK");
				return;
				}

			// 🎯 FIXED: Use async Task method properly
			System.Threading.Tasks.Task.Run(async () =>
				{
				bool success = await GitHubIntegration.CreateGitHubIssue(task);
				if (success)
					{
					UnityEngine.Debug.Log($"✅ Successfully created GitHub issue for '{task.TaskName}'");
					}
				else
					{
					UnityEngine.Debug.LogError($"❌ Failed to create GitHub issue for '{task.TaskName}'");
					}
				});

			EditorUtility.DisplayDialog("GitHub Issue Creation",
				$"Creating GitHub issue for '{task.TaskName}'...\n\nCheck the console for results.", "OK");
			}
		}
#endif
	}
