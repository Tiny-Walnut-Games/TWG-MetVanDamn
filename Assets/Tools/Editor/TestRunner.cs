using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace TinyWalnutGames.Tools.Editor
	{
	public static class TestRunner
		{
		// Usage:
		//   -executeMethod TinyWalnutGames.Tools.Editor.TestRunner.RunTests
		// Optional env vars:
		//   UNITY_TEST_PLATFORM, UNITY_TEST_FILTER, UNITY_TEST_CATEGORY
		public static void RunTests()
			{
			// Get environment variables for test configuration
			string? testPlatform = Environment.GetEnvironmentVariable("UNITY_TEST_PLATFORM");
			string? testFilter = Environment.GetEnvironmentVariable("UNITY_TEST_FILTER");
			string? testCategory = Environment.GetEnvironmentVariable("UNITY_TEST_CATEGORY");

			// Default to EditMode if not specified
			TestMode testMode = TestMode.EditMode;
			if (!string.IsNullOrEmpty(testPlatform))
				{
				if (testPlatform.ToLower() == "playmode")
					testMode = TestMode.PlayMode;
				else if (testPlatform.ToLower() == "all")
					testMode = TestMode.EditMode | TestMode.PlayMode;
				}

			// Set up results path
			string projectPath = Directory.GetCurrentDirectory();
			string debugDir = Path.Combine(projectPath, "debug");
			Directory.CreateDirectory(debugDir);
			var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			var modeString = testMode.ToString().Replace(" ", "_").Replace(",", "_");
			var resultsPath = Path.Combine(debugDir, $"TestResults_{modeString}_{ts}.xml");

			Debug.Log($"TestRunner: Starting {testMode} tests");
			Debug.Log($"TestRunner: Results will be saved to: {resultsPath}");

			// Set up filter
			var filter = new Filter
				{
				testMode = testMode,
				testNames = null,
				groupNames = null,
				categoryNames = string.IsNullOrEmpty(testCategory) ? null : new[] { testCategory },
				assemblyNames = null
				};

			if (!string.IsNullOrEmpty(testFilter))
				{
				filter.testNames = new[] { testFilter };
				Debug.Log($"TestRunner: Filtering tests: {testFilter}");
				}

			if (!string.IsNullOrEmpty(testCategory))
				{
				Debug.Log($"TestRunner: Filtering categories: {testCategory}");
				}

			var api = ScriptableObject.CreateInstance<TestRunnerApi>();
			var settings = new ExecutionSettings(filter)
				{
				overloadTestRunSettings = null
				};

			api.RegisterCallbacks(new TestCallbacks(resultsPath, testMode));
			api.Execute(settings);
			}

		private class TestCallbacks : ICallbacks
			{
			private readonly string _resultsPath;
			private readonly DateTime _startTime;
			private readonly TestMode _testMode;

			public TestCallbacks(string resultsPath, TestMode testMode)
				{
				_resultsPath = resultsPath;
				_testMode = testMode;
				_startTime = DateTime.Now;
				}

			public void RunStarted(ITestAdaptor testsToRun)
				{
				Debug.Log($"TestRunner: Test run started for {_testMode} - Found {testsToRun.TestCaseCount} tests");
				if (testsToRun.TestCaseCount == 0)
					{
					Debug.Log("TestRunner: WARNING - No tests found! Check test discovery and filters.");
					}
				}

			public void RunFinished(ITestResultAdaptor result)
				{
				var duration = (DateTime.Now - _startTime).TotalSeconds;
				Debug.Log($"TestRunner: Test run finished for {_testMode} in {duration:F2} seconds");

				try
					{
					// Write results to XML file
					var xmlContent = GenerateXmlResult(result);
					File.WriteAllText(_resultsPath, xmlContent);
					Debug.Log($"TestRunner: Test results written to {_resultsPath}");
					}
				catch (Exception e)
					{
					Debug.LogError($"TestRunner: Failed to write test results: {e.Message}");
					}
				}

			public void TestStarted(ITestAdaptor test)
				{
				Debug.Log($"TestRunner: Starting test: {test.FullName}");
				}

			public void TestFinished(ITestResultAdaptor result)
				{
				string status = result.ResultState.ToString();
				Debug.Log($"TestRunner: {status} {result.FullName} - {result.ResultState}");

				if (!string.IsNullOrEmpty(result.Message))
					{
					Debug.Log($"TestRunner: Message: {result.Message}");
					}
				}

			private string GenerateXmlResult(ITestResultAdaptor result)
				{
				var sb = new System.Text.StringBuilder();
				sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n");
				sb.Append("<test-results>\n");
				sb.Append("  <test-suite type=\"Assembly\" name=\"Unity.TestRunner\" fullname=\"Unity.TestRunner\">\n");
				sb.Append($"    <results>\n");
				AppendTestResult(sb, result, 6);
				sb.Append($"    </results>\n");
				sb.Append("  </test-suite>\n");
				sb.Append("</test-results>\n");
				return sb.ToString();
				}

			private void AppendTestResult(System.Text.StringBuilder sb, ITestResultAdaptor result, int indent)
				{
				string indentStr = new string(' ', indent);
				sb.Append(
					$"{indentStr}<test-case name=\"{result.Name}\" fullname=\"{result.FullName}\" result=\"{result.ResultState}\"");
				if (result.Duration > 0)
					sb.Append($" time=\"{result.Duration:F4}\"");
				sb.Append(">\n");

				if (!string.IsNullOrEmpty(result.Message))
					{
					sb.Append($"{indentStr}  <failure>\n");
					sb.Append($"{indentStr}    <message><![CDATA[{result.Message}]]></message>\n");
					sb.Append($"{indentStr}  </failure>\n");
					}

				sb.Append($"{indentStr}</test-case>\n");

				// Handle child results if this is a test suite
				if (result.HasChildren)
					{
					foreach (var child in result.Children)
						{
						AppendTestResult(sb, child, indent);
						}
					}
				}
			}
		}
	}
