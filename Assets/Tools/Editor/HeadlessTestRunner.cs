using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using System.Text;
using UnityEngine; // for Application/Debug if needed

namespace TinyWalnutGames.Tools.Editor
    {
    public static class HeadlessTestRunner
        {
        // Usage:
        //   -executeMethod TinyWalnutGames.Tools.Editor.HeadlessTestRunner.RunPlayMode
        // Optional env vars:
        //   UNITY_TEST_FILTER, UNITY_TEST_CATEGORY
        public static void RunPlayMode()
            {
            // Allow explicit output via -testResults, test filtering via -testFilter and -testCategory
            ParseArgs(out var explicitResultsPath, out var cliTestNames, out var cliCategories);

            var projectPath = Directory.GetCurrentDirectory();
            var debugDir = Path.Combine(projectPath, "debug");
            Directory.CreateDirectory(debugDir);
            var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var resultsPath = string.IsNullOrEmpty(explicitResultsPath)
                ? Path.Combine(debugDir, $"TestResults_{ts}.xml")
                : explicitResultsPath;

            var filter = new Filter
                {
                testMode = TestMode.PlayMode,
                testNames = null,
                groupNames = null,
                categoryNames = cliCategories,
                assemblyNames = null
                };
            if (cliTestNames != null && cliTestNames.Length > 0)
                {
                filter.testNames = cliTestNames;
                }

            var api = new TestRunnerApi();
            var settings = new ExecutionSettings(filter)
                {
                overloadTestRunSettings = null
                };

            api.RegisterCallbacks(new Callbacks(resultsPath));

            api.Execute(settings);
            }

        private class Callbacks : ICallbacks
            {
            private readonly string _resultsPath;

            public Callbacks(string resultsPath)
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
                    try { AssetDatabase.SaveAssets(); } catch { }
                    EditorApplication.Exit(0);
                    }
                }

            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            private static void WriteMinimalNUnitXml(ITestResultAdaptor result, string path)
                {
                int total = CountTotalTestCases(result);
                int passed = 0, failed = 0, skipped = 0, inconclusive = 0;
                CountResults(result, ref passed, ref failed, ref skipped, ref inconclusive);

                var sb = new StringBuilder();
                sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
                sb.AppendFormat("<test-run total=\"{0}\" passed=\"{1}\" failed=\"{2}\" inconclusive=\"{3}\" skipped=\"{4}\" result=\"{5}\">\n",
                    total, passed, failed, inconclusive, skipped, failed == 0 ? "Passed" : "Failed");
                sb.Append("  <test-suite type=\"Assembly\" name=\"PlayMode\">\n");
                sb.AppendFormat("    <result>{0}</result>\n", failed == 0 ? "Passed" : "Failed");
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


        private static void ParseArgs(out string resultsPath, out string[] testNames, out string[] categories)
            {
            resultsPath = null;
            testNames = null;
            categories = null;
            try
                {
                var args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++)
                    {
                    var a = args[i];
                    if (string.Equals(a, "-testResults", StringComparison.OrdinalIgnoreCase))
                        {
                        if (i + 1 < args.Length) resultsPath = args[++i];
                        continue;
                        }
                    if (string.Equals(a, "-testFilter", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "-filter", StringComparison.OrdinalIgnoreCase))
                        {
                        if (i + 1 < args.Length)
                            {
                            var raw = args[++i];
                            testNames = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            }
                        continue;
                        }
                    if (string.Equals(a, "-testCategory", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "-category", StringComparison.OrdinalIgnoreCase))
                        {
                        if (i + 1 < args.Length)
                            {
                            var raw = args[++i];
                            categories = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            }
                        continue;
                        }
                    }
                }
            catch { /* ignore parse issues; fall back to defaults */ }
            }
        }
    }
