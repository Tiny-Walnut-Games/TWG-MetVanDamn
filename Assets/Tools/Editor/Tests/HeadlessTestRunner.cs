using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;

namespace TinyWalnutGames.Tools.Editor.Tests
    {
    public static class HeadlessTestRunner
        {
        // General entry point for -executeMethod
        // Supports optional CLI args:
        //   -testFilter  Fully qualified test name (can be semicolon-separated for multiple)
        //   -testResults Output XML path (defaults to .\debug\TestResults_Editor_<timestamp>.xml)
        public static void Run()
            {
            var api = new TestRunnerApi();

            var (testNames, resultsPath) = ParseArgs();
            EnsureDirectory(resultsPath);

            var callbacks = new CliCallbacks(resultsPath);
            api.RegisterCallbacks(callbacks);

            var filter = new Filter
                {
                testMode = TestMode.EditMode,
                testNames = testNames
                };

            var settings = new ExecutionSettings(filter);
            api.Execute(settings);
            }

        // Convenience entry point to run a known EditorWindow test without passing args
        public static void RunBioRegionExtractorSingle()
            {
            var api = new TestRunnerApi();

            var filter = new Filter
                {
                testNames = new[]
                {
                    "TinyWalnutGames.Tools.Editor.Tests.BiomeRegionExtractorTests.BiomeRegionExtractor_CanBeCreated"
                },
                testMode = TestMode.EditMode
                };

            var resultsPath = GetResultsPath();
            EnsureDirectory(resultsPath);

            var callbacks = new CliCallbacks(resultsPath);
            api.RegisterCallbacks(callbacks);

            var settings = new ExecutionSettings(filter);
            api.Execute(settings);
            }

        private static string GetResultsPath()
            {
            // Allow overriding via env var; default to repo debug folder
            var path = Environment.GetEnvironmentVariable("BIO_TEST_RESULTS");
            if (string.IsNullOrWhiteSpace(path))
                {
                var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                path = Path.Combine(Directory.GetCurrentDirectory(), "debug", $"TestResults_Editor_{ts}.xml");
                }
            return path;
            }

        private static (string[] testNames, string resultsPath) ParseArgs()
            {
            var args = Environment.GetCommandLineArgs();

            string testFilter = null;
            string resultsPath = null;

            for (int i = 0; i < args.Length; i++)
                {
                var arg = args[i];
                if (string.Equals(arg, "-testFilter", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(arg, "-filter", StringComparison.OrdinalIgnoreCase))
                    {
                    if (i + 1 < args.Length) testFilter = args[i + 1];
                    i++;
                    continue;
                    }
                if (string.Equals(arg, "-testResults", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(arg, "-results", StringComparison.OrdinalIgnoreCase))
                    {
                    if (i + 1 < args.Length) resultsPath = args[i + 1];
                    i++;
                    continue;
                    }
                }

            // Fallbacks
            if (string.IsNullOrWhiteSpace(resultsPath))
                {
                resultsPath = GetResultsPath();
                }

            string[] names = null;
            if (!string.IsNullOrWhiteSpace(testFilter))
                {
                names = testFilter.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                }

            return (names, resultsPath);
            }

        private static void EnsureDirectory(string filePath)
            {
            try
                {
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                    Directory.CreateDirectory(dir);
                    }
                }
            catch (Exception)
                {
                // ignore
                }
            }

        private class CliCallbacks : ICallbacks
            {
            private readonly string _resultsPath;

            public CliCallbacks(string resultsPath)
                {
                _resultsPath = resultsPath;
                }

            public void RunStarted(ITestAdaptor testsToRun) { }

            public void RunFinished(ITestResultAdaptor result)
                {
                try
                    {
                    WriteMinimalNUnitXml(result, _resultsPath);
                    }
                catch (Exception e)
                    {
                    try
                        {
                        var logPath = Path.ChangeExtension(_resultsPath, ".log");
                        File.WriteAllText(logPath, e.ToString());
                        }
                    catch { /* ignore secondary failures */ }
                    }
                finally
                    {
                    EditorApplication.Exit(0);
                    }
                }

            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            private static void WriteMinimalNUnitXml(ITestResultAdaptor result, string path)
                {
                // Build a minimal NUnit-like report
                int total = CountTotalTestCases(result);
                int passed = 0, failed = 0, skipped = 0, inconclusive = 0;

                CountResults(result, ref passed, ref failed, ref skipped, ref inconclusive);

                var sb = new StringBuilder();
                sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
                sb.AppendFormat("<test-run total=\"{0}\" passed=\"{1}\" failed=\"{2}\" inconclusive=\"{3}\" skipped=\"{4}\" result=\"{5}\">\n",
                    total, passed, failed, inconclusive, skipped, result.FailCount == 0 ? "Passed" : "Failed");
                sb.Append("  <test-suite type=\"Assembly\" name=\"TinyWalnutGames.Tools.Editor.Tests\">\n");
                sb.AppendFormat("    <result>{0}</result>\n", result.FailCount == 0 ? "Passed" : "Failed");
                sb.Append("  </test-suite>\n");
                sb.Append("</test-run>\n");

                File.WriteAllText(path, sb.ToString());
                }

            private static void CountResults(ITestResultAdaptor node, ref int passed, ref int failed, ref int skipped, ref int inconclusive)
                {
                if (node.HasChildren)
                    {
                    foreach (var child in node.Children)
                        CountResults(child, ref passed, ref failed, ref skipped, ref inconclusive);
                    }
                else
                    {
                    switch (node.ResultState)
                        {
                        case "Passed": passed++; break;
                        case "Failed": failed++; break;
                        case "Skipped": skipped++; break;
                        default: inconclusive++; break;
                        }
                    }
                }

            private static int CountTotalTestCases(ITestResultAdaptor node)
                {
                if (node.HasChildren)
                    {
                    int count = 0;
                    foreach (ITestResultAdaptor child in node.Children)
                        count += CountTotalTestCases(child);
                    return count;
                    }
                else
                    {
                    return 1;
                    }
                }
            }
        }
    }
