using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using TinyWalnutGames.Tools.Editor;

namespace TinyWalnutGames.Tools.Editor.Tests
	{
	public class TestResultsCollectorTests
		{
		private string? mockAppDataPath;
		private string? mockLocalLowPath;
		private string? mockTempPath;
		private string? tempTestDirectory;

		[SetUp]
		public void Setup()
			{
			// Create temporary directories for testing
			tempTestDirectory = Path.Combine(Path.GetTempPath(), "TestResultsCollectorTests");
			mockAppDataPath = Path.Combine(tempTestDirectory, "AppData");
			mockLocalLowPath = Path.Combine(tempTestDirectory, "LocalLow");
			mockTempPath = Path.Combine(tempTestDirectory, "Temp");

			// Clean up any existing test directories
			if (Directory.Exists(tempTestDirectory))
				{
				Directory.Delete(tempTestDirectory, true);
				}

			// Create mock directory structure
			Directory.CreateDirectory(mockAppDataPath);
			Directory.CreateDirectory(mockLocalLowPath);
			Directory.CreateDirectory(mockTempPath);
			}

		[TearDown]
		public void TearDown()
			{
			// Clean up test directories
			if (Directory.Exists(tempTestDirectory))
				{
				Directory.Delete(tempTestDirectory, true);
				}
			}

		[Test]
		public void TestAccessibility_SimpleTest()
			{
			// Act
			var result = TestResultsCollector.TestAccessibility();

			// Assert
			Assert.AreEqual("TestResultsCollector is accessible", result);
			}

		[Test]
		public void CollectTestResults_CollectsFromLocalLowPath()
			{
			// Arrange
			var testResultsPath = Path.Combine(mockLocalLowPath, "TestResults.xml");
			File.WriteAllText(testResultsPath, "<test-results></test-results>");

			// Act
			var results = TestResultsCollector.CollectTestResults();

			// Assert
			Assert.IsTrue(results.Any(r => r.Contains("TestResults.xml")),
				"Should collect test results from LocalLow path");
			}

		[Test]
		public void CollectTestResults_CollectsFromTempPath()
			{
			// Arrange
			var testResultsPath = Path.Combine(mockTempPath, "TestResults.xml");
			File.WriteAllText(testResultsPath, "<test-results></test-results>");

			// Act
			var results = TestResultsCollector.CollectTestResults();

			// Assert
			Assert.IsTrue(results.Any(r => r.Contains("TestResults.xml")),
				"Should collect test results from Temp path");
			}

		[Test]
		public void CollectUnityLogs_CollectsLogFiles()
			{
			// Arrange
			var logFilePath = Path.Combine(mockAppDataPath, "unity.log");
			File.WriteAllText(logFilePath, "Test log content");

			// Act
			var logs = TestResultsCollector.CollectUnityLogs();

			// Assert
			Assert.IsTrue(logs.Any(l => l.Contains("unity.log")), "Should collect Unity log files");
			}

		[Test]
		public void CollectUnityLogs_CollectsEditorLogFiles()
			{
			// Arrange
			var editorLogPath = Path.Combine(mockAppDataPath, "Editor.log");
			File.WriteAllText(editorLogPath, "Editor log content");

			// Act
			var logs = TestResultsCollector.CollectUnityLogs();

			// Assert
			Assert.IsTrue(logs.Any(l => l.Contains("Editor.log")), "Should collect Editor log files");
			}

		[UnityTest]
		public IEnumerator CleanupOldTestResults_RemovesOldFiles()
			{
			// Arrange
			var oldFilePath = Path.Combine(mockAppDataPath, "old_TestResults.xml");
			File.WriteAllText(oldFilePath, "<test-results></test-results>");
			var oldFileInfo = new FileInfo(oldFilePath);
			oldFileInfo.LastWriteTime = System.DateTime.Now.AddDays(-8); // Make it 8 days old

			var newFilePath = Path.Combine(mockAppDataPath, "new_TestResults.xml");
			File.WriteAllText(newFilePath, "<test-results></test-results>");

			// Act
			TestResultsCollector.CleanupOldTestResults();
			yield return null; // Wait for cleanup to complete

			// Assert
			Assert.IsFalse(File.Exists(oldFilePath), "Should remove old test result files");
			Assert.IsTrue(File.Exists(newFilePath), "Should keep recent test result files");
			}

		[Test]
		public void GetTestResultPaths_ReturnsExpectedPaths()
			{
			// Act
			var paths = TestResultsCollector.GetTestResultPaths();

			// Assert
			Assert.IsTrue(paths.Length > 0, "Should return at least one test result path");
			Assert.IsTrue(paths.Any(p => p.Contains("AppData")), "Should include AppData paths");
			Assert.IsTrue(paths.Any(p => p.Contains("LocalLow")), "Should include LocalLow paths");
			Assert.IsTrue(paths.Any(p => p.Contains("Temp")), "Should include Temp paths");
			}

		[Test]
		public void GetUnityLogPaths_ReturnsExpectedPaths()
			{
			// Act
			var paths = TestResultsCollector.GetUnityLogPaths();

			// Assert
			Assert.IsTrue(paths.Length > 0, "Should return at least one Unity log path");
			Assert.IsTrue(paths.Any(p => p.Contains("AppData")), "Should include AppData paths");
			Assert.IsTrue(paths.Any(p => p.Contains("LocalLow")), "Should include LocalLow paths");
			}

		[Test]
		public void CollectTestResults_HandlesEmptyDirectories()
			{
			// Act
			var results = TestResultsCollector.CollectTestResults();

			// Assert
			Assert.IsNotNull(results, "Should return non-null collection even when no files exist");
			// Note: In a real scenario, this would collect from actual Unity paths
			}

		[Test]
		public void CollectUnityLogs_HandlesEmptyDirectories()
			{
			// Act
			var logs = TestResultsCollector.CollectUnityLogs();

			// Assert
			Assert.IsNotNull(logs, "Should return non-null collection even when no log files exist");
			// Note: In a real scenario, this would collect from actual Unity paths
			}
		}
	}
