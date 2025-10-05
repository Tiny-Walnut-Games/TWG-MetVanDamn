using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TinyWalnutGames.Tools.Editor
	{
	/// <summary>
	/// Editor utility to collect and copy Unity test results from automated save locations
	/// to the project's debug folder for easier access and version control.
	/// Updated: 2025-09-18
	/// </summary>
	public static class TestResultsCollector
		{
		private const string MENU_PATH = "Tiny Walnut Games/Test Results/Collect Latest Results";
		private const string DEBUG_FOLDER = "debug";

		[MenuItem(MENU_PATH, false, 1)]
		private static void CollectLatestTestResults()
			{
			string projectPath = Application.dataPath.Replace("/Assets", "");
			string debugPath = Path.Combine(projectPath, DEBUG_FOLDER);

			// Ensure debug folder exists
			if (!Directory.Exists(debugPath))
				{
				Directory.CreateDirectory(debugPath);
				Debug.Log($"Created debug folder: {debugPath}");
				}

			// Search locations for test results
			string[] searchPaths = GetTestResultSearchPaths(projectPath);

			FileInfo latestResult = null;
			string sourcePath = null;

			foreach (string searchPath in searchPaths)
				{
				if (!Directory.Exists(searchPath))
					continue;

				try
					{
					var xmlFiles = Directory.GetFiles(searchPath, "*.xml", SearchOption.AllDirectories)
						.Where(f => Path.GetFileName(f).Contains("TestResult") ||
						            Path.GetFileName(f).Contains("test-result") ||
						            Path.GetFileName(f).Contains("TestResults"))
						.Select(f => new FileInfo(f))
						.OrderByDescending(f => f.LastWriteTime)
						.ToArray();

					if (xmlFiles.Length > 0 &&
					    (latestResult == null || xmlFiles[0].LastWriteTime > latestResult.LastWriteTime))
						{
						latestResult = xmlFiles[0];
						sourcePath = searchPath;
						}

					// Also check for .trx files (Visual Studio test format)
					var trxFiles = Directory.GetFiles(searchPath, "*.trx", SearchOption.AllDirectories)
						.Select(f => new FileInfo(f))
						.OrderByDescending(f => f.LastWriteTime)
						.ToArray();

					if (trxFiles.Length > 0 &&
					    (latestResult == null || trxFiles[0].LastWriteTime > latestResult.LastWriteTime))
						{
						latestResult = trxFiles[0];
						sourcePath = searchPath;
						}
					}
				catch (Exception ex)
					{
					Debug.LogWarning($"Error searching {searchPath}: {ex.Message}");
					}
				}

			if (latestResult == null)
				{
				Debug.LogWarning("No test result files found in automated save locations. Try running tests first.");
				ShowNotification("No test results found", "Run tests first to generate results");
				return;
				}

			// Copy to debug folder with timestamp
			string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			string fileName = $"TestResults_{timestamp}{latestResult.Extension}";
			string destinationPath = Path.Combine(debugPath, fileName);

			try
				{
				File.Copy(latestResult.FullName, destinationPath, true);
				Debug.Log($"✅ Test results collected: {fileName}\n   From: {sourcePath}\n   To: {DEBUG_FOLDER}/");

				// Also copy any associated log files from the same directory
				CopyAssociatedLogFiles(latestResult.DirectoryName, debugPath, timestamp);

				// Also copy Unity's standard log files from LocalLow
				CopyUnityLogFiles(debugPath, timestamp);

				ShowNotification("Test Results Collected", $"Saved to {DEBUG_FOLDER}/{fileName}");
				}
			catch (Exception ex)
				{
				Debug.LogError($"Failed to copy test results: {ex.Message}");
				ShowNotification("Copy Failed", ex.Message);
				}
			}

		[MenuItem(MENU_PATH, true)]
		private static bool ValidateCollectLatestTestResults()
			{
			// Menu item is always enabled
			return true;
			}

		[MenuItem("Tiny Walnut Games/Test Results/Open Debug Folder", false, 20)]
		private static void OpenDebugFolder()
			{
			string projectPath = Application.dataPath.Replace("/Assets", "");
			string debugPath = Path.Combine(projectPath, DEBUG_FOLDER);

			if (!Directory.Exists(debugPath))
				{
				Directory.CreateDirectory(debugPath);
				}

			// Open folder in system explorer
			System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
				{
				FileName = debugPath,
				UseShellExecute = true,
				Verb = "open"
				});
			}

		[MenuItem("Tiny Walnut Games/Test Results/Clear Old Results", false, 21)]
		private static void ClearOldTestResults()
			{
			string projectPath = Application.dataPath.Replace("/Assets", "");
			string debugPath = Path.Combine(projectPath, DEBUG_FOLDER);

			if (!Directory.Exists(debugPath))
				{
				Debug.Log("Debug folder doesn't exist, nothing to clear.");
				return;
				}

			try
				{
				var testFiles = Directory.GetFiles(debugPath, "TestResults_*.xml")
					.Concat(Directory.GetFiles(debugPath, "TestResults_*.trx"))
					.Concat(Directory.GetFiles(debugPath, "unity_*test*.log"))
					.ToArray();

				if (testFiles.Length == 0)
					{
					Debug.Log("No test result files to clear.");
					return;
					}

				// Keep the 5 most recent files, delete the rest
				var filesToDelete = testFiles
					.Select(f => new FileInfo(f))
					.OrderByDescending(f => f.LastWriteTime)
					.Skip(5)
					.ToArray();

				foreach (var file in filesToDelete)
					{
					file.Delete();
					Debug.Log($"Deleted old test result: {file.Name}");
					}

				Debug.Log($"Cleaned up {filesToDelete.Length} old test result files. Kept 5 most recent.");
				ShowNotification("Cleanup Complete", $"Removed {filesToDelete.Length} old files");
				}
			catch (Exception ex)
				{
				Debug.LogError($"Failed to clear old results: {ex.Message}");
				}
			}

		/// <summary>
		/// Gets all test result file paths from automated Unity save locations.
		/// </summary>
		/// <returns>Array of file paths where test results might be found.</returns>
		public static string[] GetTestResultPaths()
			{
			string projectPath = Application.dataPath.Replace("/Assets", "");
			return GetTestResultSearchPaths(projectPath);
			}

		/// <summary>
		/// Gets all Unity log file paths from standard locations.
		/// </summary>
		/// <returns>Array of file paths where Unity logs might be found.</returns>
		public static string[] GetUnityLogPaths()
			{
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string localLowAppData = Path.Combine(localAppData, "..", "LocalLow");

			return new[]
				{
				Path.Combine(localAppData, "Unity", "Editor", "Editor.log"),
				Path.Combine(localLowAppData, "Tiny Walnut Games", "TWG-MetVanDamn", "Player.log"),
				Path.Combine(localLowAppData, "Unity", "Player.log"),
				Path.Combine(localLowAppData, "Unity", "Editor", "Player.log")
				};
			}

		/// <summary>
		/// Collects all Unity log files from standard locations.
		/// </summary>
		/// <returns>Array of file paths to found Unity log files.</returns>
		public static string[] CollectUnityLogs()
			{
			var logPaths = GetUnityLogPaths();
			return logPaths.Where(File.Exists).ToArray();
			}

		/// <summary>
		/// Cleans up old test result and log files, keeping only the 5 most recent.
		/// </summary>
		public static void CleanupOldTestResults()
			{
			string projectPath = Application.dataPath.Replace("/Assets", "");
			string debugPath = Path.Combine(projectPath, DEBUG_FOLDER);

			if (!Directory.Exists(debugPath))
				return;

			try
				{
				var testFiles = Directory.GetFiles(debugPath, "TestResults_*.xml")
					.Concat(Directory.GetFiles(debugPath, "TestResults_*.trx"))
					.Concat(Directory.GetFiles(debugPath, "unity_*test*.log"))
					.Concat(Directory.GetFiles(debugPath, "unity_log_*.log"))
					.ToArray();

				// Keep the 5 most recent files, delete the rest
				var filesToDelete = testFiles
					.Select(f => new FileInfo(f))
					.OrderByDescending(f => f.LastWriteTime)
					.Skip(5)
					.ToArray();

				foreach (var file in filesToDelete)
					{
					file.Delete();
					}
				}
			catch (Exception ex)
				{
				Debug.LogWarning($"Could not cleanup old results: {ex.Message}");
				}
			}

		private static string[] GetTestResultSearchPaths(string projectPath)
			{
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

			// Unity uses LocalLow for some logs (different from LocalApplicationData)
			string localLowAppData = Path.Combine(localAppData, "..", "LocalLow");

			return new[]
				{
				// Unity's Temp directory in project
				Path.Combine(projectPath, "Temp"),

				// Unity Editor appdata locations (Roaming)
				Path.Combine(appData, "Unity"),
				Path.Combine(appData, "Unity", "Editor"),
				Path.Combine(appData, "Unity", "Editor", "TestResults"),

				// Local appdata Unity locations
				Path.Combine(localAppData, "Unity"),
				Path.Combine(localAppData, "Unity", "Editor"),
				Path.Combine(localAppData, "Unity", "Editor", "TestResults"),

				// LocalLow appdata (where Unity stores Player.log and some editor logs)
				Path.Combine(localLowAppData, "Unity"),
				Path.Combine(localLowAppData, "Unity", "Editor"),
				Path.Combine(localLowAppData, "Unity", "Editor", "TestResults"),

				// Project-specific LocalLow location (where Player.log is stored)
				Path.Combine(localLowAppData, "Tiny Walnut Games", "TWG-MetVanDamn"),

				// Unity Hub and editor temp locations
				Path.Combine(localAppData, "Temp", "Unity"),
				Path.Combine(localAppData, "Temp", "UnityTestFramework"),

				// Windows temp directory
				Path.GetTempPath(),

				// Current debug folder (in case results are already there)
				Path.Combine(projectPath, DEBUG_FOLDER)
				};
			}

		private static void CopyAssociatedLogFiles(string sourceDir, string destDir, string timestamp)
			{
			try
				{
				// Look for log files in the same directory as the test results
				var logFiles = Directory.GetFiles(sourceDir, "*.log")
					.Where(f => Path.GetFileName(f).Contains("test") ||
					            Path.GetFileName(f).Contains("Test") ||
					            Path.GetFileName(f).Contains("unity"))
					.ToArray();

				foreach (string logFile in logFiles)
					{
					string fileName = $"unity_test_log_{timestamp}_{Path.GetFileName(logFile)}";
					string destPath = Path.Combine(destDir, fileName);
					File.Copy(logFile, destPath, true);
					Debug.Log($"   Also copied log: {fileName}");
					}
				}
			catch (Exception ex)
				{
				Debug.LogWarning($"Could not copy associated log files: {ex.Message}");
				}
			}

		private static void CopyUnityLogFiles(string destDir, string timestamp)
			{
			try
				{
				string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				string localLowAppData = Path.Combine(localAppData, "..", "LocalLow");

				// Standard Unity log file locations
				string[] logFilePaths = new[]
					{
					// Editor.log (in Unity's appdata)
					Path.Combine(localAppData, "Unity", "Editor", "Editor.log"),

					// Player.log (in project's LocalLow directory)
					Path.Combine(localLowAppData, "Tiny Walnut Games", "TWG-MetVanDamn", "Player.log"),

					// Alternative Player.log location
					Path.Combine(localLowAppData, "Unity", "Player.log"),

					// Editor Player.log (sometimes stored here)
					Path.Combine(localLowAppData, "Unity", "Editor", "Player.log")
					};

				foreach (string logPath in logFilePaths)
					{
					if (File.Exists(logPath))
						{
						string fileName = $"unity_log_{timestamp}_{Path.GetFileName(logPath)}";
						string destPath = Path.Combine(destDir, fileName);
						File.Copy(logPath, destPath, true);
						Debug.Log($"   Also copied Unity log: {fileName}");
						}
					}
				}
			catch (Exception ex)
				{
				Debug.LogWarning($"Could not copy Unity log files: {ex.Message}");
				}
			}

		private static void ShowNotification(string title, string message)
			{
			// Show notification in Unity Editor
			if (EditorWindow.focusedWindow != null)
				{
				EditorWindow.focusedWindow.ShowNotification(new GUIContent($"{title}: {message}"));
				}
			}

		/// <summary>
		/// Collects test result files from automated Unity save locations.
		/// Returns a list of file paths where test results were found.
		/// </summary>
		public static IEnumerable<string> CollectTestResults()
			{
			string projectPath = Application.dataPath.Replace("/Assets", "");
			var results = new List<string>();

			// Search locations for test results
			string[] searchPaths = GetTestResultSearchPaths(projectPath);

			foreach (string searchPath in searchPaths)
				{
				if (!Directory.Exists(searchPath))
					continue;

				try
					{
					var xmlFiles = Directory.GetFiles(searchPath, "*.xml", SearchOption.AllDirectories)
						.Where(f => Path.GetFileName(f).Contains("TestResult") ||
						            Path.GetFileName(f).Contains("test-result") ||
						            Path.GetFileName(f).Contains("TestResults"));

					var trxFiles = Directory.GetFiles(searchPath, "*.trx", SearchOption.AllDirectories)
						.Where(f => Path.GetFileName(f).Contains("TestResult") ||
						            Path.GetFileName(f).Contains("test-result") ||
						            Path.GetFileName(f).Contains("TestResults"));

					results.AddRange(xmlFiles);
					results.AddRange(trxFiles);
					}
				catch (Exception ex)
					{
					Debug.LogWarning($"Error searching {searchPath}: {ex.Message}");
					}
				}

			return results;
			}

		/// <summary>
		/// Simple test method to verify the class is accessible.
		/// </summary>
		public static string TestAccessibility()
			{
			return "TestResultsCollector is accessible";
			}
		}
	}
