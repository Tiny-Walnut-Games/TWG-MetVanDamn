#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LivingDevAgent.Editor.Modules
{
    /// <summary>
    /// 🎯 TaskMaster Module - The DAW-Style Quest & Mission Timeline!
    /// Unity Timeline-inspired task management with TODO parsing and temporal integration
    /// 🎮 Features: Draggable nodes, timeline scrubbing, temporal scribe integration
    /// </summary>
    public class TaskMasterModule : ScribeModuleBase
    {
        // Timeline display state
        private Vector2 _timelineScroll = Vector2.zero;
        private float _timelineZoom = 1.0f;
        private float _playheadPosition = 0f;
        private bool _isPlaying = false;
        private double _lastUpdateTime = 0;
        
        // Task management
        private readonly List<TaskNode> _taskNodes = new();
        private TaskNode _selectedNode = null;
        private TaskNode _draggedNode = null;
        private Vector2 _dragOffset = Vector2.zero;
        
        // Parsing engine
        private readonly TaskParser _parser = new();
        private List<string> _scannedFiles = new();
        private float _lastScanTime = 0f;
        private const float ScanInterval = 2f; // Auto-scan every 2 seconds
        
        public TaskMasterModule(TLDLScribeData data) : base(data) { }

        public override void Initialize()
        {
            base.Initialize();
            RefreshTasksFromCodebase();
        }

        public void DrawToolbar()
        {
            // Timeline controls - DAW style
            if (GUILayout.Button(_isPlaying ? "⏸️" : "▶️", EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                _isPlaying = !_isPlaying;
                if (_isPlaying)
                {
                    _lastUpdateTime = EditorApplication.timeSinceStartup;
                    EditorApplication.update += UpdatePlayhead;
                }
                else
                {
                    EditorApplication.update -= UpdatePlayhead;
                }
            }
            
            if (GUILayout.Button("⏹️", EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                _isPlaying = false;
                _playheadPosition = 0f;
                EditorApplication.update -= UpdatePlayhead;
            }
            
            GUILayout.Space(10);
            
            // Zoom controls
            GUILayout.Label("🔍", GUILayout.Width(20));
            _timelineZoom = GUILayout.HorizontalSlider(_timelineZoom, 0.1f, 3f, GUILayout.Width(80));
            
            GUILayout.Space(10);
            
            // Task management
            if (GUILayout.Button("📝 New Task", EditorStyles.toolbarButton))
            {
                CreateNewTask();
            }
            
            if (GUILayout.Button("🔄 Scan Code", EditorStyles.toolbarButton))
            {
                RefreshTasksFromCodebase();
            }
            
            if (GUILayout.Button("🕐 Sync Timer", EditorStyles.toolbarButton))
            {
                SyncWithTemporalScribe();
            }
            
            GUILayout.FlexibleSpace();
            
            // Stats
            GUILayout.Label($"📋 {_taskNodes.Count} tasks", EditorStyles.miniLabel);
        }

        public void DrawContent(Rect windowPosition)
        {
            // Auto-scan check
            if (Time.realtimeSinceStartup - _lastScanTime > ScanInterval)
            {
                RefreshTasksFromCodebase();
                _lastScanTime = Time.realtimeSinceStartup;
            }
            
            DrawTimelineHeader(windowPosition);
            DrawTimelineTracks(windowPosition);
            DrawTaskInspector();
        }

        void DrawTimelineHeader(Rect windowPosition)
        {
            float headerHeight = 40f;
            Rect headerRect = new(0, 0, windowPosition.width, headerHeight);
            
            // Time ruler - DAW style
            GUI.Box(headerRect, "", EditorStyles.toolbar);
            
            float timelineWidth = windowPosition.width * _timelineZoom;
            float secondsPerPixel = 0.1f / _timelineZoom;
            
            // Draw time markers
            for (float time = 0; time < timelineWidth * secondsPerPixel; time += 1f)
            {
                float x = time / secondsPerPixel;
                if (x > timelineWidth) break;
                
                EditorGUI.DrawRect(new Rect(x, headerHeight - 10, 1, 10), Color.gray);
                
                if (time % 5 == 0) // Major markers every 5 seconds
                {
                    GUI.Label(new Rect(x + 2, headerHeight - 25, 50, 20), $"{time:F0}s", EditorStyles.miniLabel);
                }
            }
            
            // Playhead
            float playheadX = _playheadPosition / secondsPerPixel;
            EditorGUI.DrawRect(new Rect(playheadX, 0, 2, headerHeight), Color.red);
        }

        void DrawTimelineTracks(Rect windowPosition)
        {
            float headerHeight = 40f;
            float trackHeight = 60f;
            float availableHeight = windowPosition.height - headerHeight - 100f; // Leave room for inspector
            
            _timelineScroll = EditorGUILayout.BeginScrollView(_timelineScroll, 
                GUILayout.Height(availableHeight), GUILayout.ExpandHeight(true));
            
            // Group tasks by priority/type for tracks
            var taskGroups = _taskNodes.GroupBy(t => GetTaskTrack(t)).OrderBy(g => g.Key);
            
            float currentY = 0;
            foreach (var group in taskGroups)
            {
                DrawTrackHeader(group.Key, trackHeight, currentY);
                DrawTrackTasks(group, trackHeight, currentY, windowPosition.width);
                currentY += trackHeight;
            }
            
            // Add some extra space for dropping new tasks
            GUILayout.Space(trackHeight * 2);
            
            EditorGUILayout.EndScrollView();
            
            HandleTimelineInteraction(windowPosition);
        }

        void DrawTrackHeader(string trackName, float trackHeight, float y)
        {
            Rect trackRect = EditorGUILayout.GetControlRect(false, trackHeight);
            
            // Track background
            Color trackColor = GetTrackColor(trackName);
            EditorGUI.DrawRect(new Rect(0, trackRect.y, 150, trackHeight), trackColor * 0.3f);
            
            // Track label
            GUI.Label(new Rect(10, trackRect.y + trackHeight * 0.5f - 10, 140, 20), trackName, EditorStyles.boldLabel);
        }

        void DrawTrackTasks(IGrouping<string, TaskNode> taskGroup, float trackHeight, float trackY, float windowWidth)
        {
            foreach (var task in taskGroup)
            {
                DrawTaskNode(task, trackHeight, trackY, windowWidth);
            }
        }

        void DrawTaskNode(TaskNode task, float trackHeight, float trackY, float windowWidth)
        {
            float nodeWidth = task.EstimatedDuration * 50f * _timelineZoom; // 50 pixels per hour at 1x zoom
            float nodeX = task.StartTime * 50f * _timelineZoom;
            
            Rect nodeRect = new Rect(nodeX + 150, trackY + 5, nodeWidth, trackHeight - 10);
            
            // Node background
            Color nodeColor = GetPriorityColor(task.Priority);
            if (_selectedNode == task)
                nodeColor = Color.yellow;
            
            EditorGUI.DrawRect(nodeRect, nodeColor);
            EditorGUI.DrawRect(nodeRect, Color.black * 0.3f); // Border
            
            // Node content
            GUIStyle nodeStyle = new(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
                normal = { textColor = Color.white }
            };
            
            string nodeText = $"{task.Title}\n⏱️ {task.EstimatedDuration:F1}h";
            if (task.IsLinkedToTimer)
                nodeText += " 🕐";
                
            GUI.Label(nodeRect, nodeText, nodeStyle);
            
            // Handle interaction
            if (Event.current.type == EventType.MouseDown && nodeRect.Contains(Event.current.mousePosition))
            {
                _selectedNode = task;
                if (Event.current.button == 0) // Left click
                {
                    _draggedNode = task;
                    _dragOffset = Event.current.mousePosition - new Vector2(nodeRect.x, nodeRect.y);
                }
                Event.current.Use();
            }
        }

        void DrawTaskInspector()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Height(80));
            GUILayout.Label("🎯 Task Inspector", EditorStyles.boldLabel);
            
            if (_selectedNode != null)
            {
                EditorGUILayout.BeginHorizontal();
                _selectedNode.Title = EditorGUILayout.TextField("Title", _selectedNode.Title);
                
                if (GUILayout.Button("🗑️", GUILayout.Width(30)))
                {
                    _taskNodes.Remove(_selectedNode);
                    _selectedNode = null;
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                _selectedNode.Priority = (TaskPriority)EditorGUILayout.EnumPopup("Priority", _selectedNode.Priority);
                _selectedNode.EstimatedDuration = EditorGUILayout.FloatField("Duration (h)", _selectedNode.EstimatedDuration);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("🕐 Link to Timer"))
                {
                    LinkTaskToTimer(_selectedNode);
                }
                
                if (GUILayout.Button("📝 Add to TLDL"))
                {
                    AddTaskToTLDL(_selectedNode);
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("Select a task node to edit properties", EditorStyles.centeredGreyMiniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }

        void HandleTimelineInteraction(Rect windowPosition)
        {
            Event e = Event.current;
            
            // Handle dragging
            if (_draggedNode != null && e.type == EventType.MouseDrag)
            {
                Vector2 newPos = e.mousePosition - _dragOffset;
                _draggedNode.StartTime = Mathf.Max(0, (newPos.x - 150) / (50f * _timelineZoom));
                e.Use();
            }
            
            if (e.type == EventType.MouseUp)
            {
                _draggedNode = null;
            }
            
            // Handle playhead scrubbing
            if (e.type == EventType.MouseDown && e.mousePosition.y < 40)
            {
                _playheadPosition = (e.mousePosition.x - 150) * 0.1f / _timelineZoom;
                _playheadPosition = Mathf.Max(0, _playheadPosition);
                e.Use();
            }
        }

        void UpdatePlayhead()
        {
            if (_isPlaying)
            {
                double currentTime = EditorApplication.timeSinceStartup;
                _playheadPosition += (float)(currentTime - _lastUpdateTime);
                _lastUpdateTime = currentTime;
                
                // Trigger events at playhead position
                CheckTaskTriggers();
                
                // Force repaint
                EditorWindow.focusedWindow?.Repaint();
            }
        }

        void CheckTaskTriggers()
        {
            foreach (var task in _taskNodes)
            {
                if (!task.HasTriggered && _playheadPosition >= task.StartTime)
                {
                    task.HasTriggered = true;
                    SetStatus($"🎯 Task triggered: {task.Title}");
                    
                    if (task.IsLinkedToTimer)
                    {
                        // Auto-start timer if linked
                        _data.IsTimerActive = true;
                        _data.SessionStartTime = System.DateTime.Now;
                        _data.ActiveTaskDescription = task.Title;
                    }
                }
            }
        }

        void RefreshTasksFromCodebase()
        {
            var foundTasks = _parser.ParseProjectTasks();
            
            // Merge with existing tasks, avoiding duplicates
            foreach (var parsedTask in foundTasks)
            {
                if (!_taskNodes.Any(t => t.Id == parsedTask.Id))
                {
                    _taskNodes.Add(parsedTask);
                }
            }
            
            SetStatus($"🔄 Scanned codebase: {foundTasks.Count} tasks found");
        }

        void CreateNewTask()
        {
            var newTask = new TaskNode
            {
                Id = System.Guid.NewGuid().ToString(),
                Title = "New Task",
                Priority = TaskPriority.Medium,
                EstimatedDuration = 1.0f,
                StartTime = _playheadPosition,
                Source = TaskSource.Manual
            };
            
            _taskNodes.Add(newTask);
            _selectedNode = newTask;
        }

        void LinkTaskToTimer(TaskNode task)
        {
            task.IsLinkedToTimer = !task.IsLinkedToTimer;
            
            if (task.IsLinkedToTimer && _data.IsTimerActive)
            {
                _data.ActiveTaskDescription = task.Title;
            }
            
            SetStatus($"🕐 Timer link {(task.IsLinkedToTimer ? "enabled" : "disabled")} for: {task.Title}");
        }

        void AddTaskToTLDL(TaskNode task)
        {
            if (string.IsNullOrEmpty(_data.NextSteps))
                _data.NextSteps = "";
            
            _data.NextSteps += $"\n- {task.Title} (Est: {task.EstimatedDuration:F1}h)";
            _data.IncludeNextSteps = true;
            
            SetStatus($"📝 Added task to TLDL: {task.Title}");
        }

        void SyncWithTemporalScribe()
        {
            // Sync active timer with selected task
            if (_data.IsTimerActive && _selectedNode != null)
            {
                _selectedNode.IsLinkedToTimer = true;
                _selectedNode.ActualDuration = (float)(System.DateTime.Now - _data.SessionStartTime).TotalHours;
            }
            
            SetStatus("🕐 Synced with Temporal Scribe");
        }

        string GetTaskTrack(TaskNode task)
        {
            return task.Priority switch
            {
                TaskPriority.Critical => "🔴 Critical",
                TaskPriority.High => "🟡 High Priority",
                TaskPriority.Medium => "🟢 Medium Priority",
                TaskPriority.Low => "🔵 Low Priority",
                _ => "⚪ Backlog"
            };
        }

        Color GetTrackColor(string trackName)
        {
            return trackName switch
            {
                "🔴 Critical" => Color.red,
                "🟡 High Priority" => Color.yellow,
                "🟢 Medium Priority" => Color.green,
                "🔵 Low Priority" => Color.blue,
                _ => Color.gray
            };
        }

        Color GetPriorityColor(TaskPriority priority)
        {
            return priority switch
            {
                TaskPriority.Critical => new Color(0.8f, 0.2f, 0.2f, 0.8f),
                TaskPriority.High => new Color(0.8f, 0.6f, 0.2f, 0.8f),
                TaskPriority.Medium => new Color(0.2f, 0.6f, 0.2f, 0.8f),
                TaskPriority.Low => new Color(0.2f, 0.4f, 0.8f, 0.8f),
                _ => new Color(0.5f, 0.5f, 0.5f, 0.8f)
            };
        }

        public override void Dispose()
        {
            EditorApplication.update -= UpdatePlayhead;
            base.Dispose();
        }
    }

    /// <summary>
    /// 🎯 Task parsing engine for TODO comments and planning annotations
    /// </summary>
    public class TaskParser
    {
        private static readonly Regex TodoPattern = new(@"(?://|/\*|\#)\s*(?:TODO|FIXME|HACK|NOTE|BUG|OPTIMIZE|REFACTOR|@\w+)\s*:?\s*(.+?)(?:\*/|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex EstimatePattern = new(@"\b(\d+(?:\.\d+)?)\s*(?:h|hr|hrs|hour|hours|min|mins|minutes?|d|day|days)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex PriorityPattern = new(@"\b(critical|urgent|high|medium|low|p[0-4])\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public List<TaskNode> ParseProjectTasks()
        {
            var tasks = new List<TaskNode>();
            string[] codeFiles = System.IO.Directory.GetFiles(UnityEngine.Application.dataPath, "*.*", System.IO.SearchOption.AllDirectories)
                .Where(f => IsCodeFile(f)).ToArray();

            foreach (string file in codeFiles)
            {
                try
                {
                    string content = System.IO.File.ReadAllText(file);
                    tasks.AddRange(ParseFileForTasks(file, content));
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"Failed to parse file {file}: {ex.Message}");
                }
            }

            return tasks;
        }

        List<TaskNode> ParseFileForTasks(string filePath, string content)
        {
            var tasks = new List<TaskNode>();
            string[] lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                var match = TodoPattern.Match(line);
                
                if (match.Success)
                {
                    var task = new TaskNode
                    {
                        Id = $"{filePath}:{i + 1}",
                        Title = match.Groups[1].Value.Trim(),
                        FilePath = filePath,
                        LineNumber = i + 1,
                        Source = TaskSource.CodeComment,
                        Priority = ExtractPriority(line),
                        EstimatedDuration = ExtractEstimate(line),
                        StartTime = tasks.Count * 0.5f // Stagger by 30 minutes
                    };
                    
                    tasks.Add(task);
                }
            }

            return tasks;
        }

        bool IsCodeFile(string filePath)
        {
            string ext = System.IO.Path.GetExtension(filePath).ToLower();
            return ext == ".cs" || ext == ".js" || ext == ".ts" || ext == ".cpp" || ext == ".h" || 
                   ext == ".py" || ext == ".md" || ext == ".yaml" || ext == ".yml";
        }

        TaskPriority ExtractPriority(string text)
        {
            var match = PriorityPattern.Match(text);
            if (!match.Success) return TaskPriority.Medium;

            string priority = match.Groups[1].Value.ToLower();
            return priority switch
            {
                "critical" or "urgent" or "p0" => TaskPriority.Critical,
                "high" or "p1" => TaskPriority.High,
                "medium" or "p2" => TaskPriority.Medium,
                "low" or "p3" or "p4" => TaskPriority.Low,
                _ => TaskPriority.Medium
            };
        }

        float ExtractEstimate(string text)
        {
            var match = EstimatePattern.Match(text);
            if (!match.Success) return 1.0f;

            if (float.TryParse(match.Groups[1].Value, out float value))
            {
                string unit = match.Value.ToLower();
                if (unit.Contains("min"))
                    return value / 60f; // Convert minutes to hours
                if (unit.Contains("d"))
                    return value * 8f; // Convert days to hours (8h workday)
                return value; // Already in hours
            }

            return 1.0f;
        }
    }

    /// <summary>
    /// 🎯 Task node data structure for timeline representation
    /// </summary>
    [System.Serializable]
    public class TaskNode
    {
        public string Id;
        public string Title;
        public string Description;
        public TaskPriority Priority = TaskPriority.Medium;
        public TaskSource Source = TaskSource.Manual;
        public float StartTime; // Timeline position in hours
        public float EstimatedDuration = 1.0f; // Hours
        public float ActualDuration = 0f; // Tracked time
        public bool IsCompleted = false;
        public bool IsLinkedToTimer = false;
        public bool HasTriggered = false; // For timeline playback
        
        // Source tracking
        public string FilePath;
        public int LineNumber;
        
        // Dependencies (for future Gantt chart features)
        public List<string> Dependencies = new();
        public List<string> Blockers = new();
    }

    public enum TaskPriority
    {
        Critical,
        High, 
        Medium,
        Low,
        Backlog
    }

    public enum TaskSource
    {
        Manual,
        CodeComment,
        TLDLEntry,
        GitIssue,
        Imported
    }
}
#endif
