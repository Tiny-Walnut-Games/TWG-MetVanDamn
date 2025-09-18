#nullable enable
#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace LivingDevAgent.Editor
    {
    [Overlay(typeof(SceneView), "Chronas Timer", true)]
    public class ChronasTimerOverlay : Overlay
        {
        private VisualElement _root;
        private Label _statusLabel;
        private Label _timeLabel;
        private readonly Button _startStopButton;
        private readonly Button _pauseResumeButton;
        private TextField _taskNameField;
        private TextField _projectField;

        private bool _timerActive = false;
        private string _currentTask = "";
        private string _currentProject = "";
        private DateTime _lastUpdate = DateTime.MinValue;

        private const float UPDATE_INTERVAL = 1.0f; // Update every second

        private static readonly Regex TimeRegex = new("(\\d{2}:\\d{2}:\\d{2})", RegexOptions.Compiled);

        // 🔧 ENHANCEMENT READY - Cheek-preserving dependency validation
        // Current: Assumes external chronas.py exists and python3 is available
        // Enhancement path: Fallback to Unity-native timer, better error messaging, setup guidance
        private bool _isUnityOnlyMode = false;
        private DateTime _unityTimerStart = DateTime.MinValue;
        private bool _unityTimerRunning = false;

        public ChronasTimerOverlay()
            {
            // Initialize button fields once; styles are applied in CreatePanelContent
            _startStopButton = new Button(StartStopTimer) { text = "Start" };
            _pauseResumeButton = new Button(PauseResumeTimer) { text = "Pause" };
            }

        public override VisualElement CreatePanelContent()
            {
            _root = new();
            _root.style.minWidth = 250;
            _root.style.minHeight = 120;
            _root.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            _root.style.borderTopColor = new Color(0.3f, 0.6f, 1.0f, 0.8f); // Reduced alpha for blue border
            //_root.style.borderTopWidth = 2;
            _root.style.paddingTop = 5;
            _root.style.paddingBottom = 5;
            _root.style.paddingLeft = 5;
            _root.style.paddingRight = 5;
            //_root.style.borderTopLeftRadius = 5;
            //_root.style.borderTopRightRadius = 5;
            //_root.style.borderBottomLeftRadius = 5;
            //_root.style.borderBottomRightRadius = 5;

            // Header
            Label header = new("🕰️ Chronas Timer");
            header.style.fontSize = 14;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = Color.white;
            header.style.marginBottom = 5;
            _root.Add(header);

            // Status display
            _statusLabel = new("Timer: Stopped");
            _statusLabel.style.fontSize = 11;
            _statusLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
            _root.Add(_statusLabel);

            _timeLabel = new("00:00:00");
            _timeLabel.style.fontSize = 12;
            _timeLabel.style.color = Color.white;
            _timeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _timeLabel.style.marginBottom = 5;
            _root.Add(_timeLabel);

            // Task input
            _taskNameField = new("Task Name");
            _taskNameField.style.fontSize = 10;
            _taskNameField.style.marginBottom = 2;
            _taskNameField.value = "Scene work session";
            _root.Add(_taskNameField);

            _projectField = new("Project");
            _projectField.style.fontSize = 10;
            _projectField.style.marginBottom = 5;
            _projectField.value = "Unity Development";
            _root.Add(_projectField);

            // Control buttons
            VisualElement buttonContainer = new();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.justifyContent = Justify.SpaceBetween;

            // Start/Stop
            _startStopButton.style.fontSize = 10;
            _startStopButton.style.width = 60;
            _startStopButton.style.backgroundColor = new Color(0.2f, 0.7f, 0.2f, 0.8f);

            // Pause/Resume
            _pauseResumeButton.style.fontSize = 10;
            _pauseResumeButton.style.width = 60;
            _pauseResumeButton.style.backgroundColor = new Color(0.7f, 0.5f, 0.2f, 0.8f);
            _pauseResumeButton.SetEnabled(false);

            // Refresh
            Button refreshButton = new(RefreshStatus) { text = "Refresh" };
            refreshButton.style.fontSize = 10;
            refreshButton.style.width = 60;
            refreshButton.style.backgroundColor = new Color(0.2f, 0.5f, 0.7f, 0.8f);

            buttonContainer.Add(_startStopButton);
            buttonContainer.Add(_pauseResumeButton);
            buttonContainer.Add(refreshButton);
            _root.Add(buttonContainer);

            // Register for updates
            EditorApplication.update += UpdateTimerDisplay;

            // Initial status check
            RefreshStatus();

            return _root;
            }

        public override void OnWillBeDestroyed()
            {
            EditorApplication.update -= UpdateTimerDisplay;
            base.OnWillBeDestroyed();
            }

        private void UpdateTimerDisplay()
            {
            // Only update every second to avoid performance issues
            if ((DateTime.Now - _lastUpdate).TotalSeconds < UPDATE_INTERVAL)
                return;

            _lastUpdate = DateTime.Now;

            // Update Unity timer display if running
            if (_unityTimerRunning && _unityTimerStart != DateTime.MinValue)
                {
                TimeSpan elapsed = DateTime.Now - _unityTimerStart;
                _timeLabel.text = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
                }
            // Auto-refresh status periodically when external timer is active
            else if (_timerActive && !_isUnityOnlyMode)
                {
                RefreshStatus();
                }
            }

        private void StartStopTimer()
            {
            try
                {
                // Try external chronas first, fall back to Unity-native
                if (!_isUnityOnlyMode && ValidateChronasEnvironment(out string _))
                    {
                    StartStopChronasTimer();
                    }
                else
                    {
                    StartStopUnityTimer();
                    }
                }
            catch (Exception ex)
                {
                UnityEngine.Debug.LogWarning($"[Chronas Overlay] Falling back to Unity timer: {ex.Message}");
                _isUnityOnlyMode = true;
                StartStopUnityTimer();
                }
            }

        private void StartStopChronasTimer()
            {
            if (_timerActive)
                {
                // Stop timer
                string notes = "Scene work completed via Unity overlay";
                RunChronasCommand($"--stop --notes \"{notes}\"");
                }
            else
                {
                // Start timer
                string taskName = string.IsNullOrWhiteSpace(_taskNameField.value) ? "Unity scene work" : _taskNameField.value;
                string project = string.IsNullOrWhiteSpace(_projectField.value) ? "" : _projectField.value;

                string projectArg = string.IsNullOrEmpty(project) ? "" : $"--project \"{project}\"";
                RunChronasCommand($"--start \"{taskName}\" {projectArg} --category \"unity-development\"");
                }

            // Refresh status after a short delay
            EditorApplication.delayCall += () =>
            {
                System.Threading.Thread.Sleep(100);
                RefreshStatus();
            };
            }

        // 🔧 ENHANCEMENT READY - Unity-native timer fallback system
        // Current: Basic Unity-only timer with EditorPrefs persistence
        // Enhancement path: Session tracking, time analytics, productivity insights
        private void StartStopUnityTimer()
            {
            if (_unityTimerRunning)
                {
                // Stop Unity timer
                _unityTimerRunning = false;
                TimeSpan sessionDuration = DateTime.Now - _unityTimerStart;
                _unityTimerStart = DateTime.MinValue;

                // Save session to EditorPrefs for persistence across Unity sessions
                string sessionKey = "ChronasUnityTimer.LastSession";
                string taskName = string.IsNullOrWhiteSpace(_taskNameField.value) ? "Unity scene work" : _taskNameField.value;
                string sessionData = $"{taskName}|{DateTime.Now:yyyy-MM-dd HH:mm:ss}|Session completed in {sessionDuration:hh\\:mm\\:ss}";
                EditorPrefs.SetString(sessionKey, sessionData);

                _statusLabel.text = "Timer: Stopped (Unity mode)";
                _timeLabel.text = "00:00:00";
                _timerActive = false;

                UnityEngine.Debug.Log($"[Chronas Overlay] Unity timer session completed: {taskName} ({sessionDuration:hh\\:mm\\:ss})");
                }
            else
                {
                // Start Unity timer
                _unityTimerRunning = true;
                _unityTimerStart = DateTime.Now;
                _timerActive = true;

                string taskName = string.IsNullOrWhiteSpace(_taskNameField.value) ? "Unity scene work" : _taskNameField.value;
                _statusLabel.text = $"Timer: Running - {taskName} (Unity mode)";

                UnityEngine.Debug.Log($"[Chronas Overlay] Unity timer started: {taskName}");
                }

            UpdateButtonStates();
            }

        private void RefreshStatus()
            {
            try
                {
                // 🍑 Cheek-preserving validation before attempting timer operations
                if (!ValidateChronasEnvironment(out string validationError))
                    {
                    _isUnityOnlyMode = true;

                    if (_unityTimerRunning)
                        {
                        string taskName = string.IsNullOrWhiteSpace(_taskNameField.value) ? "Unity scene work" : _taskNameField.value;
                        _statusLabel.text = $"Timer: Running - {taskName} (Unity mode)";
                        _timerActive = true;
                        }
                    else
                        {
                        _statusLabel.text = "Timer: Stopped (Unity mode)";
                        _timeLabel.text = "00:00:00";
                        _timerActive = false;
                        }

                    UpdateButtonStates();
                    return;
                    }

                _isUnityOnlyMode = false;
                string statusOutput = RunChronasCommand("--status");
                ParseStatusOutput(statusOutput);
                }
            catch (Exception ex)
                {
                // More helpful error messages for common issues
                string userFriendlyError = ex.Message.Contains("python3")
                    ? "Python not available - using Unity-only mode"
                    : ex.Message.Contains("chronas.py")
                    ? "Chronas timer script missing - using Unity-only mode"
                    : $"Timer error: {ex.Message}";

                UnityEngine.Debug.LogWarning($"[Chronas Overlay] {userFriendlyError}");
                _isUnityOnlyMode = true;
                _statusLabel.text = "Timer: Stopped (Unity mode)";
                _timeLabel.text = "00:00:00";
                _timerActive = false;
                UpdateButtonStates();
                }
            }

        private static string ExtractAfter(string line, string marker)
            {
            int idx = line.IndexOf(marker, StringComparison.Ordinal);
            return idx >= 0 ? line[(idx + marker.Length)..].Trim() : null;
            }

        private void ParseStatusOutput(string output)
            {
            if (string.IsNullOrEmpty(output))
                {
                _statusLabel.text = "Timer: Stopped";
                _timeLabel.text = "00:00:00";
                _timerActive = false;
                UpdateButtonStates();
                return;
                }

            _timerActive = output.Contains("Active Session") || output.Contains("Active Timer");

            if (_timerActive)
                {
                // Extract lines
                string[] lines = output.Split('\n');

                foreach (string line in lines)
                    {
                    if (line.Contains("Task:"))
                        {
                        _currentTask = ExtractAfter(line, "Task:") ?? _currentTask;
                        break;
                        }
                    if (line.Contains("Active Timer:"))
                        {
                        _currentTask = ExtractAfter(line, "Active Timer:") ?? _currentTask;
                        break;
                        }
                    }

                foreach (string line in lines)
                    {
                    if (line.Contains("Project:"))
                        {
                        _currentProject = ExtractAfter(line, "Project:") ?? "";
                        break;
                        }
                    }

                string elapsedTime = "00:00:00";
                foreach (string line in lines)
                    {
                    if (line.Contains("Elapsed:"))
                        {
                        string elapsedPart = ExtractAfter(line, "Elapsed:") ?? "";
                        Match timeMatch = TimeRegex.Match(elapsedPart);
                        if (timeMatch.Success)
                            {
                            elapsedTime = timeMatch.Groups[1].Value;
                            }
                        break;
                        }
                    }

                _statusLabel.text = $"Timer: Running - {_currentTask}";
                _timeLabel.text = elapsedTime;

                if (!string.IsNullOrEmpty(_currentProject))
                    {
                    _statusLabel.text += $" ({_currentProject})";
                    }
                }
            else
                {
                _statusLabel.text = "Timer: Stopped";
                _timeLabel.text = "00:00:00";
                _currentTask = "";
                _currentProject = "";
                }

            UpdateButtonStates();
            }

        private void UpdateButtonStates()
            {
            if (_timerActive)
                {
                _startStopButton.text = "Stop";
                _startStopButton.style.backgroundColor = new Color(0.7f, 0.2f, 0.2f, 0.8f);
                _pauseResumeButton.SetEnabled(true);
                _taskNameField.SetEnabled(false);
                _projectField.SetEnabled(false);
                }
            else
                {
                _startStopButton.text = "Start";
                _startStopButton.style.backgroundColor = new Color(0.2f, 0.7f, 0.2f, 0.8f);
                _pauseResumeButton.SetEnabled(false);
                _taskNameField.SetEnabled(true);
                _projectField.SetEnabled(true);
                }
            }

        private string RunChronasCommand(string args)
            {
            try
                {
                string chronasPath = GetChronasPath();
                if (!File.Exists(chronasPath))
                    {
                    throw new FileNotFoundException($"Chronas script not found at: {chronasPath}");
                    }

                return RunPythonCommand(chronasPath, args);
                }
            catch (Exception ex)
                {
                UnityEngine.Debug.LogError($"[Chronas Overlay] Failed to run Chronas command: {ex.Message}");
                throw;
                }
            }

        private string RunPythonCommand(string scriptPath, string args)
            {
            ProcessStartInfo startInfo = new()
                {
                FileName = "python3",
                Arguments = $"\"{scriptPath}\" {args}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = GetProjectRoot()
                };

            using Process process = Process.Start(startInfo);
            process.WaitForExit(5000); // 5 second timeout

            if (!process.HasExited)
                {
                try { process.Kill(); } catch { /* ignore */ }
                throw new TimeoutException("Python command timed out");
                }

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                {
                throw new Exception($"Python command failed: {error}");
                }

            return output;
            }

        private string GetChronasPath()
            {
            // 🍑 Cheek-saving fix: Look in root directory, not Assets/src
            string projectRoot = GetProjectRoot();
            return Path.Combine(projectRoot, "chronas.py");
            }

        private string GetTimerServicePath()
            {
            // 🍑 Cheek-saving fix: Look in root directory, not Assets/src
            string projectRoot = GetProjectRoot();
            return Path.Combine(projectRoot, "timer_service.py");
            }

        private string GetProjectRoot()
            {
            // Find project root by looking for Assets directory
            string currentDir = Directory.GetCurrentDirectory();
            string assetsDir = Path.Combine(currentDir, "Assets");

            if (Directory.Exists(assetsDir))
                {
                return currentDir;
                }

            // Fallback: look for parent directories
            DirectoryInfo parent = Directory.GetParent(currentDir);
            while (parent != null)
                {
                string testAssetsDir = Path.Combine(parent.FullName, "Assets");
                if (Directory.Exists(testAssetsDir))
                    {
                    return parent.FullName;
                    }
                parent = parent.Parent;
                }

            // Ultimate fallback
            return currentDir;
            }

        // 🔧 ENHANCEMENT READY - Cheek-preserving dependency validation
        // Current: Assumes external chronas.py exists and python3 is available
        // Enhancement path: Fallback to Unity-native timer, better error messaging, setup guidance
        private bool ValidateChronasEnvironment(out string errorMessage)
            {
            errorMessage = "";

            // Check if Python is available
            string[] pythonCommands = { "python3", "python", "py" };
            bool pythonFound = false;

            foreach (string pythonCmd in pythonCommands)
                {
                try
                    {
                    ProcessStartInfo testInfo = new()
                        {
                        FileName = pythonCmd,
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                        };

                    using Process testProcess = Process.Start(testInfo);
                    testProcess.WaitForExit(2000);

                    if (testProcess.ExitCode == 0)
                        {
                        pythonFound = true;
                        break;
                        }
                    }
                catch
                    {
                    // Continue trying other Python commands
                    }
                }

            if (!pythonFound)
                {
                errorMessage = "Python not found. Using Unity-only mode.";
                return false;
                }

            // Check if chronas.py exists in root directory
            string chronasPath = GetChronasPath();
            if (!File.Exists(chronasPath))
                {
                errorMessage = $"Chronas timer not found. Using Unity-only mode.";
                return false;
                }

            return true;
            }

        // 🔧 ENHANCEMENT READY - Pause/Resume functionality
        // Current: Basic pause/resume for both Chronas and Unity modes
        // Enhancement path: Advanced session management, break tracking, productivity analytics
        private void PauseResumeTimer()
            {
            try
                {
                if (_isUnityOnlyMode)
                    {
                    // Unity timer doesn't support pause - convert to stop/start
                    if (_unityTimerRunning)
                        {
                        StartStopUnityTimer(); // This will stop it
                        UnityEngine.Debug.Log("[Chronas Overlay] Unity timer paused (converted to stop)");
                        }
                    return;
                    }

                // For external chronas timer service
                string timerServicePath = GetTimerServicePath();
                if (File.Exists(timerServicePath))
                    {
                    if (_statusLabel.text.Contains("running", StringComparison.OrdinalIgnoreCase))
                        {
                        RunPythonCommand(timerServicePath, "--pause");
                        }
                    else if (_statusLabel.text.Contains("paused", StringComparison.OrdinalIgnoreCase))
                        {
                        RunPythonCommand(timerServicePath, "--resume");
                        }

                    EditorApplication.delayCall += () =>
                    {
                        System.Threading.Thread.Sleep(100);
                        RefreshStatus();
                    };
                    }
                }
            catch (Exception ex)
                {
                UnityEngine.Debug.LogError($"[Chronas Overlay] Error in pause/resume: {ex.Message}");
                }
            }

        }
    }
#endif
