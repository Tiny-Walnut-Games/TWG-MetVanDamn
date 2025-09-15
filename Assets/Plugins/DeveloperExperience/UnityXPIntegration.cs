#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;

namespace DeveloperExperience
{
    /// <summary>
    /// Unity Editor integration for Jerry's XP system
    /// Awards XP for Unity-specific development activities
    /// </summary>
    public class UnityXPIntegration : EditorWindow
    {
        private string developerName = "YourName";
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
            GetWindow<UnityXPIntegration>("XP Tracker");
        }
        
        [MenuItem("Tools/Developer Experience/Record Debug Session")]
        public static void RecordDebugSession()
        {
            string workspace = Directory.GetCurrentDirectory();
            string pythonScript = Path.Combine(workspace, "src", "DeveloperExperience", "dev_experience.py");
            
            if (File.Exists(pythonScript))
            {
                string args = $"--record \"{System.Environment.UserName}\" debugging_session excellent \"Unity debugging session\" --metrics \"unity_session:1\"";
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
            string pythonScript = Path.Combine(workspace, "src", "DeveloperExperience", "dev_experience.py");
            
            if (File.Exists(pythonScript))
            {
                string args = $"--profile \"{System.Environment.UserName}\"";
                RunPythonScript(pythonScript, args);
            }
        }
        
        [MenuItem("Tools/Developer Experience/Show Leaderboard")]
        public static void ShowLeaderboard()
        {
            string workspace = Directory.GetCurrentDirectory();
            string pythonScript = Path.Combine(workspace, "src", "DeveloperExperience", "dev_experience.py");
            
            if (File.Exists(pythonScript))
            {
                RunPythonScript(pythonScript, "--leaderboard");
            }
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Developer Experience Tracker", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            developerName = EditorGUILayout.TextField("Developer Name", developerName);
            
            // Contribution type dropdown
            int contributionIndex = System.Array.IndexOf(contributionTypes, contributionType);
            contributionIndex = EditorGUILayout.Popup("Contribution Type", contributionIndex, contributionTypes);
            contributionType = contributionTypes[contributionIndex];
            
            // Quality level dropdown
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
            
            if (GUILayout.Button("Show My Profile"))
            {
                ShowProfile();
            }
            
            if (GUILayout.Button("Show Leaderboard"))
            {
                ShowLeaderboard();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("XP is automatically awarded for commits via Git hooks. Use this window for manual tracking of Unity-specific contributions.", MessageType.Info);
        }
        
        private void RecordContribution()
        {
            if (string.IsNullOrEmpty(developerName) || string.IsNullOrEmpty(description))
            {
                EditorUtility.DisplayDialog("Error", "Please fill in all fields", "OK");
                return;
            }
            
            string workspace = Directory.GetCurrentDirectory();
            string pythonScript = Path.Combine(workspace, "src", "DeveloperExperience", "dev_experience.py");
            
            if (File.Exists(pythonScript))
            {
                string args = $"--record \"{developerName}\" {contributionType} {qualityLevel} \"{description}\" --metrics \"unity_manual:1\"";
                RunPythonScript(pythonScript, args);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "XP System not found. Make sure the Living Dev Agent template is properly installed.", "OK");
            }
        }
        
        private static void RunPythonScript(string scriptPath, string arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "python3",
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
            // Track play mode sessions
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            // Track build completion
            BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuildPlayer);
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                // Award XP for testing/validation when entering play mode
                string workspace = Directory.GetCurrentDirectory();
                string pythonScript = Path.Combine(workspace, "src", "DeveloperExperience", "dev_experience.py");
                
                if (File.Exists(pythonScript))
                {
                    string args = $"--record \"{System.Environment.UserName}\" test_coverage good \"Unity play mode testing\" --metrics \"play_mode_test:1\"";
                    RunPythonScript(pythonScript, args);
                }
            }
        }
        
        private static void OnBuildPlayer(BuildPlayerOptions options)
        {
            // Award XP for successful builds
            string workspace = Directory.GetCurrentDirectory();
            string pythonScript = Path.Combine(workspace, "src", "DeveloperExperience", "dev_experience.py");
            
            if (File.Exists(pythonScript))
            {
                string args = $"--record \"{System.Environment.UserName}\" code_contribution excellent \"Unity build completion\" --metrics \"unity_build:1,target:{options.target}\"";
                RunPythonScript(pythonScript, args);
            }
        }
        
        private static void RunPythonScript(string scriptPath, string arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "python3",
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
#endif
