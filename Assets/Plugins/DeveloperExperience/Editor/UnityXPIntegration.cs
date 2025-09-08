using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;

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

        [MenuItem("Tools/Developer Experience/XP Tracker")]
        public static void ShowWindow()
            {
            GetWindow<UnityXPIntegrationEditor>("XP Tracker");
            }

        [MenuItem("Tools/Developer Experience/Record Debug Session")]
        public static void RecordDebugSession()
            {
            string workspace = Directory.GetCurrentDirectory();
            string pythonScript = Path.Combine(workspace, "template", "src", "DeveloperExperience", "dev_experience.py");

            if (File.Exists(pythonScript))
                {
                string developerName = "Jerry";
                string args = $"--record \"{developerName}\" debugging_session excellent \"Unity debugging session\" --metrics \"unity_session:1,platform:windows\"";
                RunPythonScript(pythonScript, args);
                }
            else
                {
                UnityEngine.Debug.LogError("XP System not found. Make sure the Living Dev Agent template is properly installed.");
                }
            }

        [MenuItem("Tools/Developer Experience/Show My Profile")]
        public static void ShowProfile()
            {
            string workspace = Directory.GetCurrentDirectory();
            string pythonScript = Path.Combine(workspace, "template", "src", "DeveloperExperience", "dev_experience.py");

            if (File.Exists(pythonScript))
                {
                string developerName = "Jerry";
                string args = $"--profile \"{developerName}\"";
                RunPythonScript(pythonScript, args);
                }
            }

        [MenuItem("Tools/Developer Experience/Show Leaderboard")]
        public static void ShowLeaderboard()
            {
            string workspace = Directory.GetCurrentDirectory();
            string pythonScript = Path.Combine(workspace, "template", "src", "DeveloperExperience", "dev_experience.py");

            if (File.Exists(pythonScript))
                {
                RunPythonScript(pythonScript, "--leaderboard");
                }
            }

        [MenuItem("Tools/Developer Experience/Daily Bonus")]
        public static void DailyBonus()
            {
            string workspace = Directory.GetCurrentDirectory();
            string pythonScript = Path.Combine(workspace, "template", "src", "DeveloperExperience", "dev_experience.py");

            if (File.Exists(pythonScript))
                {
                string developerName = "Jerry";
                string args = $"--daily-bonus \"{developerName}\"";
                RunPythonScript(pythonScript, args);
                }
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

            string workspace = Directory.GetCurrentDirectory();
            string pythonScript = Path.Combine(workspace, "template", "src", "DeveloperExperience", "dev_experience.py");

            if (File.Exists(pythonScript))
                {
                string args = $"--record \"{developerName}\" {contributionType} {qualityLevel} \"{description}\" --metrics \"unity_manual:1,platform:windows\"";
                RunPythonScript(pythonScript, args);
                }
            else
                {
                EditorUtility.DisplayDialog("Error", "XP System not found.", "OK");
                }
            }

        private static void RunPythonScript(string scriptPath, string arguments)
            {
            try
                {
                var startInfo = new ProcessStartInfo
                    {
                    FileName = "python",
                    Arguments = $"\"{scriptPath}\" {arguments}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                    };

                var process = Process.Start(startInfo);
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                    {
                    UnityEngine.Debug.Log($"XP System: {output}");
                    }

                if (!string.IsNullOrEmpty(error))
                    {
                    UnityEngine.Debug.LogError($"XP System Error: {error}");
                    }
                }
            catch (System.Exception e)
                {
                UnityEngine.Debug.LogError($"Failed to run XP system: {e.Message}");
                }
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
            }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
            {
            if (state == PlayModeStateChange.EnteredPlayMode)
                {
                string workspace = Directory.GetCurrentDirectory();
                string pythonScript = Path.Combine(workspace, "template", "src", "DeveloperExperience", "dev_experience.py");

                if (File.Exists(pythonScript))
                    {
                    string developerName = "Jerry";
                    string args = $"--record \"{developerName}\" test_coverage good \"Unity play mode testing\" --metrics \"play_mode_test:1,platform:windows\"";
                    RunPythonScript(pythonScript, args);
                    }
                }
            }

        private static void RunPythonScript(string scriptPath, string arguments)
            {
            try
                {
                var startInfo = new ProcessStartInfo
                    {
                    FileName = "python",
                    Arguments = $"\"{scriptPath}\" {arguments}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                    };

                Process.Start(startInfo);
                }
            catch (System.Exception e)
                {
                UnityEngine.Debug.LogError($"Failed to track XP: {e.Message}");
                }
            }
        }
    }
