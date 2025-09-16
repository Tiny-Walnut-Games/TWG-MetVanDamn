using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;

namespace MetaTests
    {
    [TestFixture]
    public class UnityFixtureRunner
        {
        private static string FindProjectRoot(string startDir)
            {
            string dir = startDir;
            while (!string.IsNullOrEmpty(dir))
                {
                var projectVersion = Path.Combine(dir, "ProjectSettings", "ProjectVersion.txt");
                if (File.Exists(projectVersion)) return dir;
                var parent = Directory.GetParent(dir);
                dir = parent?.FullName;
                }
            return null;
            }

        private static string ReadUnityVersion(string projectPath)
            {
            var pvPath = Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt");
            if (!File.Exists(pvPath)) return null;
            foreach (var line in File.ReadAllLines(pvPath))
                {
                // Lines look like: m_EditorVersion: 6000.2.0f1
                // or m_EditorVersionWithRevision: 6000.2.0f1 (deadbeef)
                var idx = line.IndexOf(":", StringComparison.Ordinal);
                if (idx > 0)
                    {
                    var key = line.Substring(0, idx).Trim();
                    if (key == "m_EditorVersion" || key == "m_EditorVersionWithRevision")
                        {
                        var value = line.Substring(idx + 1).Trim();
                        // Strip trailing revision hash if present
                        var spaceIdx = value.IndexOf(' ');
                        if (spaceIdx > 0) value = value.Substring(0, spaceIdx);
                        return value;
                        }
                    }
                }
            return null;
            }

        private static string ResolveUnityEditorPath(string version)
            {
            // Allow env override first
            var envExe = Environment.GetEnvironmentVariable("UNITY_EXE");
            if (!string.IsNullOrWhiteSpace(envExe) && File.Exists(envExe)) return envExe;

            // Optional UNITY_HOME pointing at the root install (expects Editor/Unity.exe)
            var unityHome = Environment.GetEnvironmentVariable("UNITY_HOME");
            if (!string.IsNullOrWhiteSpace(unityHome))
                {
                var candidate = Path.Combine(unityHome, "Editor", "Unity.exe");
                if (File.Exists(candidate)) return candidate;
                }

            if (string.IsNullOrWhiteSpace(version)) return null;

            // Windows common install paths (Unity Hub)
            var candidates = new[]
                {
                $@"C:\\Program Files\\Unity\\Hub\\Editor\\{version}\\Editor\\Unity.exe",
                $@"C:\\Program Files (x86)\\Unity\\Hub\\Editor\\{version}\\Editor\\Unity.exe"
                };

            foreach (var path in candidates)
                {
                if (File.Exists(path)) return path;
                }

            // As a fallback, try any installed version under Hub that matches major.minor
            var hubRoot1 = @"C:\\Program Files\\Unity\\Hub\\Editor";
            var hubRoot2 = @"C:\\Program Files (x86)\\Unity\\Hub\\Editor";
            foreach (var hubRoot in new[] { hubRoot1, hubRoot2 })
                {
                if (Directory.Exists(hubRoot))
                    {
                    try
                        {
                        var subdirs = Directory.GetDirectories(hubRoot);
                        var found = subdirs
                            .Select(d => Path.Combine(d, "Editor", "Unity.exe"))
                            .FirstOrDefault(File.Exists);
                        if (found != null) return found;
                        }
                    catch { /* ignore */ }
                    }
                }
            return null;
            }

        [Test]
        public void Unity_PlayMode_Tests_Should_Pass()
            {
            // --- CONFIG ---
            // Resolve project path: ENV override -> find ProjectSettings from current dir
            var projectPath = Environment.GetEnvironmentVariable("UNITY_PROJECT_PATH");
            if (string.IsNullOrWhiteSpace(projectPath))
                {
                var start = Directory.GetCurrentDirectory();
                projectPath = FindProjectRoot(start) ?? FindProjectRoot(Path.GetDirectoryName(typeof(UnityFixtureRunner).Assembly.Location));
                }
            Assert.IsFalse(string.IsNullOrWhiteSpace(projectPath), "Unable to locate Unity project root (missing ProjectSettings/ProjectVersion.txt). Set UNITY_PROJECT_PATH env var.");

            // Version and editor path
            var unityVersion = ReadUnityVersion(projectPath);
            var unityExe = ResolveUnityEditorPath(unityVersion);
            Assert.IsFalse(string.IsNullOrWhiteSpace(unityExe) || !File.Exists(unityExe), $"Unable to resolve Unity editor for version '{unityVersion}'. Set UNITY_EXE env var.");

            // Test options via ENV
            var testPlatform = Environment.GetEnvironmentVariable("UNITY_TEST_PLATFORM");
            if (string.IsNullOrWhiteSpace(testPlatform)) testPlatform = "PlayMode";
            var testFilter = Environment.GetEnvironmentVariable("UNITY_TEST_FILTER"); // optional
            var resultsDir = Environment.GetEnvironmentVariable("UNITY_RESULTS_DIR");
            if (string.IsNullOrWhiteSpace(resultsDir)) resultsDir = Path.Combine(projectPath, "debug");
            var extraArgs = Environment.GetEnvironmentVariable("UNITY_EXTRA_ARGS"); // optional
            var timeoutSecondsEnv = Environment.GetEnvironmentVariable("UNITY_TIMEOUT_SECONDS"); // optional
            var resultsGraceSecondsEnv = Environment.GetEnvironmentVariable("UNITY_RESULTS_GRACE_SECONDS"); // optional

            string debugDir = resultsDir;
            Directory.CreateDirectory(debugDir);

            var runStart = DateTime.Now;
            string ts = runStart.ToString("yyyyMMdd_HHmmss");
            string resultsPath = Path.Combine(debugDir, $"TestResults_{ts}.xml");
            string logPath = Path.Combine(debugDir, $"unity_powershell_test_{ts}.log");

            // --- RUN UNITY ---
            var psi = new ProcessStartInfo
                {
                FileName = unityExe,
                Arguments = $"-batchmode -nographics -projectPath \"{projectPath}\" -runTests -testPlatform {testPlatform} "
                            + (string.IsNullOrWhiteSpace(testFilter) ? string.Empty : $"-testFilter {testFilter} ")
                            + $"-testResults \"{resultsPath}\" -logFile \"{logPath}\" "
                            + (string.IsNullOrWhiteSpace(extraArgs) ? string.Empty : extraArgs + " ")
                            + "-quit",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
                };

            using (var proc = Process.Start(psi))
                {
                proc.OutputDataReceived += (s, e) => { if (e.Data != null) TestContext.Progress.WriteLine(e.Data); };
                proc.ErrorDataReceived += (s, e) => { if (e.Data != null) TestContext.Progress.WriteLine(e.Data); };
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                // While Unity is running, patiently poll for the results file appearing (some setups write before full exit)
                var preExitWatch = Stopwatch.StartNew();
                var preExitMax = TimeSpan.FromMinutes(10);
                while (!proc.HasExited && preExitWatch.Elapsed < preExitMax && !File.Exists(resultsPath))
                    {
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(2));
                    }
                // Primary wait for Unity to exit
                var overallTimeout = TimeSpan.FromMinutes(15);
                if (int.TryParse(timeoutSecondsEnv, out var toSec) && toSec > 0)
                    overallTimeout = TimeSpan.FromSeconds(toSec);
                if (!proc.WaitForExit((int)overallTimeout.TotalMilliseconds))
                    {
                    try { proc.Kill(); } catch { /* ignore */ }
                    Assert.Fail($"Unity did not exit within timeout. Check log: {logPath}");
                    }

                // After exit, allow a grace period for the results file to appear/flush to disk
                var resultsGrace = TimeSpan.FromSeconds(120);
                if (int.TryParse(resultsGraceSecondsEnv, out var rgSec) && rgSec > 0)
                    resultsGrace = TimeSpan.FromSeconds(rgSec);
                var pollInterval = TimeSpan.FromSeconds(2);
                var sw = Stopwatch.StartNew();
                while (!File.Exists(resultsPath) && sw.Elapsed < resultsGrace)
                    {
                    System.Threading.Thread.Sleep(pollInterval);
                    }

                // Fallback: parse log for an alternate results path Unity may have used
                if (!File.Exists(resultsPath) && File.Exists(logPath))
                    {
                    try
                        {
                        var logLines = File.ReadAllLines(logPath);
                        string[] prefixes = new[] { "Saving results to:", "Results saved to:", "Test results written to:", "Generated report (full):" };
                        string altLine = logLines.FirstOrDefault(l => prefixes.Any(p => l.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0));
                        if (altLine != null)
                            {
                            foreach (var p in prefixes)
                                {
                                var idx = altLine.IndexOf(p, StringComparison.OrdinalIgnoreCase);
                                if (idx >= 0)
                                    {
                                    var candidate = altLine.Substring(idx + p.Length).Trim().Trim('"');
                                    if (File.Exists(candidate))
                                        {
                                        try { File.Copy(candidate, resultsPath, overwrite: true); } catch { /* ignore copy errors */ }
                                        }
                                    break;
                                    }
                                }
                            }
                        }
                    catch { /* ignore parse errors */ }
                    }

                // Fallback 2: scan common directories for any TestResults_*.xml created after runStart
                if (!File.Exists(resultsPath))
                    {
                    try
                        {
                        string[] probeDirs = new[]
                            {
                            debugDir,
                            Path.Combine(projectPath, "Assets", "debug"),
                            Path.Combine(projectPath, "debug"),
                            projectPath
                            };
                        var found = probeDirs
                            .Where(Directory.Exists)
                            .SelectMany(d => Directory.GetFiles(d, "TestResults_*.xml", SearchOption.TopDirectoryOnly))
                            .Select(p => new FileInfo(p))
                            .Where(fi => fi.LastWriteTime >= runStart.AddMinutes(-10))
                            .OrderByDescending(fi => fi.LastWriteTime)
                            .FirstOrDefault();
                        if (found != null)
                            {
                            try { File.Copy(found.FullName, resultsPath, overwrite: true); } catch { /* ignore */ }
                            }
                        }
                    catch { /* ignore */ }
                    }
                }

            // --- VERIFY RESULTS FILE ---
            if (!File.Exists(resultsPath))
                {
                string existing = string.Empty;
                try
                    {
                    if (Directory.Exists(Path.GetDirectoryName(resultsPath)))
                        existing = string.Join(", ", Directory.GetFiles(Path.GetDirectoryName(resultsPath), "TestResults_*.xml").Select(Path.GetFileName));
                    }
                catch { }

                string logTail = string.Empty;
                try
                    {
                    if (File.Exists(logPath))
                        {
                        var lines = File.ReadAllLines(logPath);
                        logTail = string.Join(Environment.NewLine, lines.Skip(Math.Max(0, lines.Length - 80)));
                        }
                    }
                catch { }

                Assert.Fail($"No results file found at {resultsPath}. Existing results: [{existing}]. Check Unity log: {logPath}. Log tail:\n{logTail}");
                }

            // --- PARSE XML ---
            var xml = new XmlDocument();
            xml.Load(resultsPath);
            var testRunNode = xml.SelectSingleNode("/test-run");
            int total = int.Parse(testRunNode.Attributes["total"].Value);
            int passed = int.Parse(testRunNode.Attributes["passed"].Value);
            int failed = int.Parse(testRunNode.Attributes["failed"].Value);
            int inconclusive = int.Parse(testRunNode.Attributes["inconclusive"].Value);

            TestContext.WriteLine($"Unity Fixture Results: total={total}, passed={passed}, failed={failed}, inconclusive={inconclusive}");

            // --- ASSERT ---
            Assert.That(failed, Is.EqualTo(0), $"Unity PlayMode tests failed. See {resultsPath} and {logPath}");
            }
        }
    }
