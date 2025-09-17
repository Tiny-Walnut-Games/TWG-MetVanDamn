using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using System.Text;
using UnityEngine; // for Application/Debug if needed
using System.Globalization;

namespace TinyWalnutGames.Tools.Editor
    {
    public static class HeadlessTestRunner
        {
        // Usage:
        //   -executeMethod TinyWalnutGames.Tools.Editor.HeadlessTestRunner.RunPlayMode
        //   -executeMethod TinyWalnutGames.Tools.Editor.HeadlessTestRunner.RunEditMode
        //   -executeMethod TinyWalnutGames.Tools.Editor.HeadlessTestRunner.RunAll
        // Optional env vars:
        //   UNITY_TEST_FILTER, UNITY_TEST_CATEGORY
        public static void RunPlayMode()
            {
            RunTestsInternal(TestMode.PlayMode);
            }

        public static void RunEditMode()
            {
            RunTestsInternal(TestMode.EditMode);
            }

        public static void RunAll()
            {
            Debug.Log("HeadlessTestRunner: Running all tests (EditMode + PlayMode)");
            RunTestsInternal(TestMode.EditMode | TestMode.PlayMode);
            }

        private static void RunTestsInternal(TestMode testMode)
            {
            // Allow explicit output via -testResults, test filtering via -testFilter and -testCategory
            ParseArgs(out string explicitResultsPath, out string[] cliTestNames, out string[] cliCategories);

            string projectPath = Directory.GetCurrentDirectory();
            string debugDir = Path.Combine(projectPath, "debug");
            Directory.CreateDirectory(debugDir);
            var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var modeString = testMode.ToString().Replace(" ", "_").Replace(",", "_");
            var resultsPath = string.IsNullOrEmpty(explicitResultsPath)
                ? Path.Combine(debugDir, $"TestResults_{modeString}_{ts}.xml")
                : explicitResultsPath;

            Debug.Log($"HeadlessTestRunner: Starting {testMode} tests");
            Debug.Log($"HeadlessTestRunner: Results will be saved to: {resultsPath}");

            var filter = new Filter
                {
                testMode = testMode,
                testNames = null,
                groupNames = null,
                categoryNames = cliCategories,
                assemblyNames = null
                };
            if (cliTestNames != null && cliTestNames.Length > 0)
                {
                filter.testNames = cliTestNames;
                Debug.Log($"HeadlessTestRunner: Filtering tests: {string.Join(", ", cliTestNames)}");
                }
            if (cliCategories != null && cliCategories.Length > 0)
                {
                Debug.Log($"HeadlessTestRunner: Filtering categories: {string.Join(", ", cliCategories)}");
                }

            var api = new TestRunnerApi();
            var settings = new ExecutionSettings(filter)
                {
                overloadTestRunSettings = null
                };

            api.RegisterCallbacks(new Callbacks(resultsPath, testMode));

            api.Execute(settings);
            }

        private class Callbacks : ICallbacks
            {
            private readonly string _resultsPath;
            private readonly TestMode _testMode;
            private readonly DateTime _startTime;

            public Callbacks(string resultsPath, TestMode testMode)
                {
                _resultsPath = resultsPath;
                _testMode = testMode;
                _startTime = DateTime.Now;
                }

            public void RunStarted(ITestAdaptor testsToRun) 
                { 
                Debug.Log($"HeadlessTestRunner: Test run started for {_testMode}");
                }

            public void RunFinished(ITestResultAdaptor result)
                {
                var endTime = DateTime.Now;
                var duration = (endTime - _startTime).TotalSeconds;
                
                Debug.Log($"HeadlessTestRunner: Test run finished for {_testMode} in {duration:F2} seconds");
                
                try
                    {
                    WriteEnhancedNUnitXml(result, _resultsPath, _startTime, endTime, _testMode);
                    Debug.Log($"HeadlessTestRunner: Test results written to {_resultsPath}");
                    }
                catch (Exception e)
                    {
                    Debug.LogError($"HeadlessTestRunner: Failed to write test results: {e.Message}");
                    try
                        {
                        var logPath = Path.ChangeExtension(_resultsPath, ".log");
                        File.WriteAllText(logPath, $"Error writing test results:\n{e}");
                        }
                    catch { /* ignore secondary failures */ }
                    }
                finally
                    {
                    try { AssetDatabase.SaveAssets(); } catch { }
                    
                    // Schedule exit on the next editor update to give Unity enough time to flush logs to disk.
                    // In most cases, this is sufficient for log flushing, but on slower machines or under heavy load,
                    // consider waiting for multiple frames or using a longer delay if logs are missing.
                    EditorApplication.delayCall += () => EditorApplication.Exit(0);
                    }
                }

            public void TestStarted(ITestAdaptor test) 
                { 
                Debug.Log($"HeadlessTestRunner: Starting test: {test.FullName}");
                }
                
            public void TestFinished(ITestResultAdaptor result) 
                { 
                var status = result.ResultState == "Passed" ? "✅" : "❌";
                Debug.Log($"HeadlessTestRunner: {status} {result.FullName} - {result.ResultState}");
                if (!string.IsNullOrEmpty(result.Message))
                    {
                    Debug.Log($"HeadlessTestRunner: Message: {result.Message}");
                    }
                }

            private static void WriteEnhancedNUnitXml(ITestResultAdaptor result, string path, DateTime startTime, DateTime endTime, TestMode testMode)
                {
                int total = CountTotalTestCases(result);
                int passed = 0, failed = 0, skipped = 0, inconclusive = 0;
                CountResults(result, ref passed, ref failed, ref skipped, ref inconclusive);
                
                var duration = (endTime - startTime).TotalSeconds;

                var sb = new StringBuilder();
                sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
                sb.AppendFormat("<test-run id=\"1\" testcasecount=\"{0}\" total=\"{0}\" passed=\"{1}\" failed=\"{2}\" inconclusive=\"{3}\" skipped=\"{4}\" result=\"{5}\" start-time=\"{6}\" end-time=\"{7}\" duration=\"{8:F6}\">\n",
                    total, passed, failed, inconclusive, skipped, 
                    failed == 0 ? "Passed" : "Failed",
                    startTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                    endTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                    duration);
                    
                sb.AppendFormat("  <command-line><![CDATA[Unity {0} Tests via HeadlessTestRunner]]></command-line>\n", testMode);
                sb.Append("  <test-suite type=\"Assembly\" id=\"0-1000\" name=\"Unity.TestRunner\" fullname=\"Unity.TestRunner\">\n");
                sb.AppendFormat("    <result>{0}</result>\n", failed == 0 ? "Passed" : "Failed");
                
                // Add detailed test results
                WriteTestSuiteDetails(result, sb, "    ");
                
                sb.Append("  </test-suite>\n");
                sb.Append("</test-run>\n");

                File.WriteAllText(path, sb.ToString());
                }

            private static void WriteTestSuiteDetails(ITestResultAdaptor node, StringBuilder sb, string indent)
                {
                if (node.HasChildren)
                    {
                    foreach (var child in node.Children)
                        {
                        sb.AppendFormat("{0}<test-suite type=\"TestFixture\" name=\"{1}\" fullname=\"{2}\">\n", 
                            indent, EscapeXml(child.Name), EscapeXml(child.FullName));
                        sb.AppendFormat("{0}  <result>{1}</result>\n", indent, child.FailCount == 0 ? "Passed" : "Failed");
                        WriteTestSuiteDetails(child, sb, indent + "  ");
                        sb.AppendFormat("{0}</test-suite>\n", indent);
                        }
                    }
                else
                    {
                    // Individual test case
                    sb.AppendFormat("{0}<test-case name=\"{1}\" fullname=\"{2}\" result=\"{3}\"", 
                        indent, EscapeXml(node.Name), EscapeXml(node.FullName), node.ResultState);
                    
                    if (node.Duration.HasValue)
                        {
                        sb.AppendFormat(" duration=\"{0:F6}\"", node.Duration.Value);
                        }
                    
                    if (!string.IsNullOrEmpty(node.Message) || !string.IsNullOrEmpty(node.StackTrace))
                        {
                        sb.Append(">\n");
                        if (!string.IsNullOrEmpty(node.Message))
                            {
                            sb.AppendFormat("{0}  <message><![CDATA[{1}]]></message>\n", indent, node.Message);
                            }
                        if (!string.IsNullOrEmpty(node.StackTrace))
                            {
                            sb.AppendFormat("{0}  <stack-trace><![CDATA[{1}]]></stack-trace>\n", indent, node.StackTrace);
                            }
                        sb.AppendFormat("{0}</test-case>\n", indent);
                        }
                    else
                        {
                        sb.Append(" />\n");
                        }
                    }
                }

            private static string EscapeXml(string text)
                {
                if (string.IsNullOrEmpty(text)) return text;
                return text.Replace("&", "&amp;")
                          .Replace("<", "&lt;")
                          .Replace(">", "&gt;")
                          .Replace("\"", "&quot;")
                          .Replace("'", "&apos;");
                }

            private static void CountResults(ITestResultAdaptor node, ref int passed, ref int failed, ref int skipped, ref int inconclusive)
                {
                if (node.HasChildren)
                    {
                    foreach (ITestResultAdaptor child in node.Children)
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
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++)
                    {
                    string a = args[i];
                    if (string.Equals(a, "-testResults", StringComparison.OrdinalIgnoreCase))
                        {
                        if (i + 1 < args.Length) resultsPath = args[++i];
                        continue;
                        }
                    if (string.Equals(a, "-testFilter", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "-filter", StringComparison.OrdinalIgnoreCase))
                        {
                        if (i + 1 < args.Length)
                            {
                            string raw = args[++i];
                            testNames = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            }
                        continue;
                        }
                    if (string.Equals(a, "-testCategory", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "-category", StringComparison.OrdinalIgnoreCase))
                        {
                        if (i + 1 < args.Length)
                            {
                            string raw = args[++i];
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
