using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using System;

namespace DeveloperExperience
    {
    /// <summary>
    /// Cross-platform Unity Editor integration for XP system
    /// Platform: windows
    /// </summary>
    public class UnityXPIntegrationEditor : EditorWindow
        {
        private string developerName = "Jerry";
        private string description = "";
        private string contributionType = "code_contribution";
        private string qualityLevel = "good";

        private readonly string[] contributionTypes = {
            "code_contribution", "debugging_session", "documentation",
            "test_coverage", "refactoring", "architecture"
        };

        private readonly string[] qualityLevels = {
            "legendary", "epic", "excellent", "good", "needs_work"
        };

        [MenuItem("Tiny Walnut Games/Living Dev Agent/Developer Experience/XP Tracker")]
        public static void ShowWindow()
            {
            GetWindow<UnityXPIntegrationEditor>("XP Tracker");
            }

        [MenuItem("Tiny Walnut Games/Living Dev Agent/Developer Experience/Record Debug Session")]
        public static void RecordDebugSession()
            {
            RecordQuick("debugging_session", "excellent", "Unity debugging session", "unity_session:1");
            }

        [MenuItem("Tiny Walnut Games/Living Dev Agent/Developer Experience/Show My Profile")]
        public static void ShowProfile()
            {
            var script = ResolveScript();
            if (script == null) { WarnMissing(); return; }
            RunPythonScript(script, $"--profile \"{ResolveDeveloper()}\"");
            }

        [MenuItem("Tiny Walnut Games/Living Dev Agent/Developer Experience/Show Leaderboard")]
        public static void ShowLeaderboard()
            {
            var script = ResolveScript();
            if (script == null) { WarnMissing(); return; }
            RunPythonScript(script, "--leaderboard");
            }

        [MenuItem("Tiny Walnut Games/Living Dev Agent/Developer Experience/Daily Bonus")]
        public static void DailyBonus()
            {
            var script = ResolveScript();
            if (script == null) { WarnMissing(); return; }
            RunPythonScript(script, $"--daily-bonus \"{ResolveDeveloper()}\"");
            }

        private void OnGUI()
            {
            GUILayout.Label("Developer Experience Tracker (windows)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            developerName = EditorGUILayout.TextField("Developer Name", developerName);

            int contributionIndex = System.Array.IndexOf(contributionTypes, contributionType);
            contributionIndex = EditorGUILayout.Popup("Contribution Type", contributionIndex, contributionTypes);
            contributionType = contributionTypes[contributionIndex];

            int qualityIndex = System.Array.IndexOf(qualityLevels, qualityLevel);
            qualityIndex = EditorGUILayout.Popup("Quality Level", qualityIndex, qualityLevels);
            qualityLevel = qualityLevels[qualityIndex];

            description = EditorGUILayout.TextField("Description", description);

            EditorGUILayout.Space();

            if (GUILayout.Button("Record Contribution"))
                {
                RecordContribution();
                }

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Show Profile"))
                {
                ShowProfile();
                }
            if (GUILayout.Button("Daily Bonus"))
                {
                DailyBonus();
                }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Show Leaderboard"))
                {
                ShowLeaderboard();
                }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox($"Running on {System.Environment.OSVersion.Platform} with Python: python", MessageType.Info);
            }

        private void RecordContribution()
            {
            if (string.IsNullOrEmpty(developerName) || string.IsNullOrEmpty(description))
                {
                EditorUtility.DisplayDialog("Error", "Please fill in all fields", "OK");
                return;
                }

            var script = ResolveScript();
            if (script == null)
                {
                EditorUtility.DisplayDialog("Error", "XP System not found.", "OK");
                return;
                }
            string args = $"--record \"{developerName}\" {contributionType} {qualityLevel} \"{description}\" --metrics \"unity_manual:1,platform:{Application.platform.ToString().ToLower()}\"";
            RunPythonScript(script, args);
            }

        // ---------------- Helper / Consolidated Utilities ----------------
        public static string ResolveDeveloper()
            {
            // Prefer explicit editor preference in future; for now use environment fallback.
            return Environment.UserName ?? "UnknownDev";
            }

        public static string? ResolveScript()
            {
            string root = Directory.GetCurrentDirectory();
            string[] candidates = new[]
                {
                Path.Combine(root, "src", "DeveloperExperience", "dev_experience.py"),
                Path.Combine(root, "template", "src", "DeveloperExperience", "dev_experience.py")
                };
            foreach (var c in candidates)
                if (File.Exists(c)) return c;
            return null;
            }

        private static string? ResolvePython()
            {
            string[] candidates = { Environment.GetEnvironmentVariable("PYTHON") ?? string.Empty, "python3", "python" };
            foreach (var c in candidates)
                {
                if (string.IsNullOrWhiteSpace(c)) continue;
                try
                    {
                    var p = Process.Start(new ProcessStartInfo
                        {
                        FileName = c,
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                        });
                    if (p != null)
                        {
                        p.WaitForExit(1000);
                        if (p.ExitCode == 0) return c;
                        }
                    }
                catch { }
                }
            return null;
            }

        private static void WarnMissing()
            {
            UnityEngine.Debug.LogWarning("XP Tracker: dev_experience.py not found (looked in src/ and template/src/). Skipping.");
            }

        public static void RunPythonScript(string scriptPath, string arguments)
            {
            try
                {
                var py = ResolvePython();
                if (py == null)
                    {
                    UnityEngine.Debug.LogWarning("XP Tracker: No python interpreter found (python/python3). Skipping.");
                    return;
                    }
                if (!File.Exists(scriptPath))
                    {
                    WarnMissing();
                    return;
                    }
                var startInfo = new ProcessStartInfo
                    {
                    FileName = py,
                    Arguments = $"\"{scriptPath}\" {arguments}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                    };
                using var process = Process.Start(startInfo);
                if (process == null)
                    {
                    UnityEngine.Debug.LogWarning("XP Tracker: Failed to start python process.");
                    return;
                    }
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (!string.IsNullOrWhiteSpace(output)) UnityEngine.Debug.Log($"XP System: {output.Trim()}");
                if (!string.IsNullOrWhiteSpace(error)) UnityEngine.Debug.LogError($"XP System Error: {error.Trim()}");
                }
            catch (Exception e)
                {
                UnityEngine.Debug.LogError($"Failed to run XP system: {e.Message}");
                }
            }

        private static void RecordQuick(string type, string quality, string desc, string metrics)
            {
            var script = ResolveScript();
            if (script == null) { WarnMissing(); return; }
            RunPythonScript(script, $"--record \"{ResolveDeveloper()}\" {type} {quality} \"{desc}\" --metrics \"{metrics}\"");
            }
        }

    /// <summary>
    /// Automatic XP tracking for Unity Editor events
    /// </summary>
    [InitializeOnLoad]
    public class UnityXPAutoTracker
        {
        static UnityXPAutoTracker()
            {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuildPlayer);
            }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
            {
            if (state == PlayModeStateChange.EnteredPlayMode)
                {
                var script = UnityXPIntegrationEditor.ResolveScript();
                if (script == null) return;
                UnityXPIntegrationEditor.RunPythonScript(script, $"--record \"{Environment.UserName}\" test_coverage good \"Unity play mode testing\" --metrics \"play_mode_test:1\"");
                }
            }

        private static void OnBuildPlayer(BuildPlayerOptions options)
            {
            var script = UnityXPIntegrationEditor.ResolveScript();
            if (script != null)
                {
                UnityXPIntegrationEditor.RunPythonScript(script, $"--record \"{Environment.UserName}\" code_contribution excellent \"Unity build completion\" --metrics \"unity_build:1,target:{options.target}\"");
                }
            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
            }

        // AutoTracker no longer needs its own RunPythonScript; uses editor's shared utilities.
        }
    }
