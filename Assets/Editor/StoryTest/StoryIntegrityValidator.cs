// StoryIntegrityValidator.cs
// Stage 1 + Rule 1 (Phantom Props / write-only serialized fields)
// Additional rules will extend StoryUsage model.
#nullable enable
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TinyWalnutGames.StoryTest
    {
    internal sealed class StoryReport
        {
        public List<string> PhantomProps = new();
        public List<string> ColdPublicMethods = new();
        public List<string> HollowEnums = new();
        public List<string> PrematureCelebrations = new();
        // Suppressed (heuristic / coverage) lists for transparency
        public List<string> SuppressedPhantomProps = new();
        public List<string> SuppressedColdPublicMethods = new();
        public List<string> SuppressedHollowEnums = new();
        public List<string> SuppressedPrematureCelebrations = new();
        public DateTime GeneratedAt = DateTime.UtcNow;
        public string ToolVersion = "0.6.0"; // v0.6.0 adds severity, coverage overlay stub, reflection heuristics, global call graph
        public Severity HighestSeverity = Severity.Info;
        }

    internal enum Severity { Info = 0, Warning = 1, Error = 2 }

    internal struct FieldUsage
        {
        public bool Written;
        public bool Read;
        }

    public static class StoryIntegrityValidator
        {
        private const BindingFlags FieldScanFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private static readonly string RootNamespacePrefix = "TinyWalnutGames";
        private static readonly HashSet<string> UnityMagicMethods = new()
        {
            "Awake","Start","OnEnable","OnDisable","OnDestroy","Update","LateUpdate","FixedUpdate",
            "OnGUI","Reset" // extend as needed
        };

        [MenuItem("Tools/Story Test/Run Integrity Pass")]
        public static void Run()
            {
            var report = new StoryReport();
            try
                {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a.GetName().Name != null && a.GetName().Name.StartsWith(RootNamespacePrefix, StringComparison.Ordinal))
                    .ToArray();

                // Collect enum candidates & usage tracking structures
                var enumCandidates = new HashSet<Type>();
                var enumReferenced = new HashSet<Type>();

                // Coverage overlay (optional)
                var coverage = LoadCoverageOverlay();

                // Global collection for call graph & string literals (reflection heuristic)
                var globalInvokedTokens = new HashSet<int>();
                var globalStringLiterals = new HashSet<string>(StringComparer.Ordinal);
                var typeMethodILCache = new Dictionary<Type, List<(MethodInfo method, byte[] il)>>();

                foreach (var asm in assemblies)
                    {
                    AnalyzeAssembly(asm, report, enumCandidates, enumReferenced, globalInvokedTokens, globalStringLiterals, typeMethodILCache, coverage);
                    }

                // Determine hollow enums (declared but never referenced by type signature usage)
                foreach (var enumType in enumCandidates)
                    {
                    if (!enumReferenced.Contains(enumType))
                        {
                        // Ignore enums explicitly marked to ignore
                        if (enumType.IsDefined(typeof(StoryIgnoreAttribute), inherit: true)) continue;
                        string fullName = enumType.FullName ?? enumType.Name;
                        if (coverage != null && coverage.enumsReferenced.Contains(fullName))
                            {
                            report.SuppressedHollowEnums.Add(fullName + " (coverage)");
                            }
                        else
                            {
                            report.HollowEnums.Add(fullName);
                            }
                        }
                    }

                // Cold public methods global pass (replace per-type logic)
                IdentifyColdPublicMethods(report, assemblies, globalInvokedTokens, coverage);

                // Update highest severity after all classification
                ComputeHighestSeverity(report);

                SaveReport(report);
                TryWriteDiff(report);
                LogSummary(report);
                }
            catch (Exception ex)
                {
                Debug.LogError($"[StoryTest] Exception during run: {ex}\n{ex.StackTrace}");
                }
            }

        // Batchmode-friendly entry point: can be triggered with -executeMethod TinyWalnutGames.StoryTest.StoryIntegrityValidator.RunFromCI
        public static void RunFromCI()
            {
            Debug.Log("[StoryTest] CI invocation started");
            Run();
            // Optionally enforce failure on violations via env var toggle
            string failEnv = Environment.GetEnvironmentVariable("STORYTEST_FAIL_ON_VIOLATION") ?? string.Empty;
            string severityThreshold = Environment.GetEnvironmentVariable("STORYTEST_FAIL_ON_SEVERITY") ?? string.Empty; // INFO/WARN/ERROR
            if (failEnv.Equals("1") || failEnv.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                if (CachedLastReportHasViolations(out var r))
                    {
                    Debug.LogError("[StoryTest] Violations detected - exiting with code 3 due to STORYTEST_FAIL_ON_VIOLATION.");
                    EditorApplication.Exit(3);
                    }
                }
            if (!string.IsNullOrWhiteSpace(severityThreshold) && CachedLastReportHasViolations(out var report))
                {
                if (Enum.TryParse<Severity>(severityThreshold, true, out var threshold))
                    {
                    if (report != null && report.HighestSeverity >= threshold)
                        {
                        Debug.LogError($"[StoryTest] Highest severity {report.HighestSeverity} >= threshold {threshold} - exiting with code 4.");
                        EditorApplication.Exit(4);
                        }
                    }
                }
            }

        private static StoryReport? _lastReportCache;
        private static bool CachedLastReportHasViolations(out StoryReport? report)
            {
            report = _lastReportCache;
            if (report == null)
                {
                try
                    {
                    string path = Path.Combine(Directory.GetCurrentDirectory(), "story-report.json");
                    if (File.Exists(path))
                        {
                        var json = File.ReadAllText(path);
                        var wrapper = JsonUtility.FromJson<Wrapper>(json);
                        // Rehydrate minimal report for decision (only need lists)
                        var r = new StoryReport();
                        r.PhantomProps = wrapper.phantomProps ?? new();
                        r.ColdPublicMethods = wrapper.coldPublicMethods ?? new();
                        r.HollowEnums = wrapper.hollowEnums ?? new();
                        r.PrematureCelebrations = wrapper.prematureCelebrations ?? new();
                        _lastReportCache = r;
                        report = r;
                        }
                    }
                catch { /* ignore */ }
                }
            if (report == null) return false;
            return report.PhantomProps.Count + report.ColdPublicMethods.Count + report.HollowEnums.Count + report.PrematureCelebrations.Count > 0;
            }

        private static void AnalyzeAssembly(Assembly asm, StoryReport report, HashSet<Type> enumCandidates, HashSet<Type> enumReferenced,
            HashSet<int> globalInvokedTokens, HashSet<string> globalStringLiterals,
            Dictionary<Type, List<(MethodInfo method, byte[] il)>> typeMethodILCache, CoverageOverlay? coverage)
            {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException rtle)
                {
                types = rtle.Types.Where(t => t != null).ToArray()!;
                Debug.LogWarning($"[StoryTest] Partial load for assembly {asm.GetName().Name}: {rtle.LoaderExceptions?.Length} loader exceptions.");
                }

            foreach (var type in types)
                {
                if (type == null) continue;
                if (type.IsDefined(typeof(StoryIgnoreAttribute), inherit: true)) continue;
                if (!type.FullName!.StartsWith(RootNamespacePrefix, StringComparison.Ordinal)) continue;
                if (type.IsEnum)
                    {
                    // Track enum candidate
                    enumCandidates.Add(type);
                    continue; // enum body itself not analyzed for methods/fields usage
                    }

                // While scanning regular types, record enum usage in signatures
                try { RecordEnumUsagesInType(type, enumReferenced); } catch { /* best effort */ }

                AnalyzeType(type, report, globalInvokedTokens, globalStringLiterals, typeMethodILCache, coverage);
                }
            }

        private static void RecordEnumUsagesInType(Type type, HashSet<Type> enumReferenced)
            {
            // Fields
            foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                RegisterEnumTypeUsage(f.FieldType, enumReferenced);
                }

            // Properties
            foreach (var p in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                RegisterEnumTypeUsage(p.PropertyType, enumReferenced);
                }

            // Methods (return + parameters)
            foreach (var m in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                RegisterEnumTypeUsage(m.ReturnType, enumReferenced);
                foreach (var param in m.GetParameters())
                    {
                    RegisterEnumTypeUsage(param.ParameterType, enumReferenced);
                    }
                }
            }

        private static void RegisterEnumTypeUsage(Type t, HashSet<Type> enumReferenced)
            {
            if (t == null) return;
            // Unwrap arrays & nullable
            if (t.IsArray) t = t.GetElementType()!;
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                t = Nullable.GetUnderlyingType(t)!;
                }
            if (t.IsEnum)
                {
                enumReferenced.Add(t);
                }
            }

        private static void AnalyzeType(Type type, StoryReport report, HashSet<int> globalInvokedTokens, HashSet<string> globalStringLiterals,
            Dictionary<Type, List<(MethodInfo method, byte[] il)>> typeMethodILCache, CoverageOverlay? coverage)
            {
            // Basic write/read analysis of serialized instance fields.
            var fields = type.GetFields(FieldScanFlags);
            if (fields.Length == 0)
                {
                // Still may have methods for premature celebration detection (readiness flags might be on base) so continue building method IL anyway.
                }

            // Build method bodies for primitive scan.
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => !m.IsAbstract && m.GetMethodBody() != null);

            // Pre-collect IL bytes for scanning.
            var methodIL = new List<(MethodInfo method, byte[] il)>();
            foreach (var m in methods)
                {
                try
                    {
                    var body = m.GetMethodBody();
                    if (body != null)
                        {
                        var il = body.GetILAsByteArray();
                        methodIL.Add((m, il));
                        ExtractGlobalCallGraph(il, m, globalInvokedTokens, globalStringLiterals);
                        }
                    }
                catch { /* ignore transient reflection issues */ }
                }
            typeMethodILCache[type] = methodIL;

            foreach (var field in fields)
                {
                if (!IsSerializedField(field)) continue;
                if (field.IsDefined(typeof(StoryIgnoreAttribute), inherit: true)) continue;

                var usage = new FieldUsage();
                AnalyzeFieldUsage(field, methodIL, ref usage);

                if (usage.Written && !usage.Read)
                    {
                    string full = $"{type.FullName}.{field.Name}";
                    bool covered = coverage != null && coverage.fieldsRead.Contains(full);
                    bool heuristic = AppearsReflected(full, field.Name, globalStringLiterals);
                    if (covered)
                        report.SuppressedPhantomProps.Add(full + " (coverage)");
                    else if (heuristic)
                        report.SuppressedPhantomProps.Add(full + " (heuristic-reflection)");
                    else
                        report.PhantomProps.Add(full);
                    }
                }

            // Cold public methods moved to global pass

            // Premature Celebration Rule: celebratory logs emitted before readiness flag writes in same method.
            try
                {
                // Gather readiness field tokens (bool fields with name pattern *Ready / *IsReady / *Initialized / *Completed)
                var readinessTokens = new HashSet<int>();
                foreach (var f in fields)
                    {
                    if (f.FieldType == typeof(bool) && ReadinessNamePattern(f.Name))
                        {
                        readinessTokens.Add(f.MetadataToken);
                        }
                    }

                if (readinessTokens.Count > 0)
                    {
                    foreach (var (method, il) in methodIL)
                        {
                        if (il == null || il.Length == 0) continue;
                        bool readinessWritten = false;
                        string? pendingString = null;
                        Module module = method.Module;
                        for (int i = 0; i < il.Length; i++)
                            {
                            byte op = il[i];
                            switch (op)
                                {
                                case 0x7D: // stfld
                                    if (i + 4 < il.Length)
                                        {
                                        int token = BitConverter.ToInt32(il, i + 1);
                                        if (readinessTokens.Contains(token)) readinessWritten = true;
                                        i += 4;
                                        }
                                    break;
                                case 0x72: // ldstr
                                    if (i + 4 < il.Length)
                                        {
                                        int token = BitConverter.ToInt32(il, i + 1);
                                        try { pendingString = module.ResolveString(token); } catch { pendingString = null; }
                                        i += 4;
                                        }
                                    break;
                                case 0x28: // call
                                case 0x6F: // callvirt
                                    if (i + 4 < il.Length)
                                        {
                                        int token = BitConverter.ToInt32(il, i + 1);
                                        MethodBase? invoked = null;
                                        try { invoked = module.ResolveMethod(token); } catch { /* ignore */ }
                                        if (invoked != null && invoked.DeclaringType != null && invoked.DeclaringType.FullName == "UnityEngine.Debug")
                                            {
                                            if (pendingString != null && IsCelebratory(pendingString) && !readinessWritten)
                                                {
                                                string truncated = pendingString.Length > 60 ? pendingString[..57] + "..." : pendingString;
                                                string entry = $"{type.FullName}.{method.Name} -> '{truncated}' before readiness flag";
                                                if (coverage != null && coverage.methodsInvoked.Contains(type.FullName + "." + method.Name + "()"))
                                                    report.SuppressedPrematureCelebrations.Add(entry + " (coverage)");
                                                else
                                                    report.PrematureCelebrations.Add(entry);
                                                }
                                            pendingString = null; // consume
                                            }
                                        i += 4;
                                        }
                                    break;
                                default:
                                    // Any other instruction resets dangling string if too far away? Keep simple; allow distance.
                                    break;
                                }
                            }
                        }
                    }
                }
            catch (Exception ex)
                {
                Debug.LogWarning($"[StoryTest] Premature celebration scan failed for {type.FullName}: {ex.Message}");
                }
            }

        private static bool IsSerializedField(FieldInfo field)
            {
            // Public instance field OR has SerializeField attribute and is not static
            if (field.IsStatic) return false;
            if (field.IsPublic) return true;
            if (field.GetCustomAttribute(typeof(SerializeField)) != null) return true;
            return false;
            }

        private static void AnalyzeFieldUsage(FieldInfo field, List<(MethodInfo method, byte[] il)> methodIL, ref FieldUsage usage)
            {
            // Cheap IL scan: look for Ldfld/Ldflda vs Stfld referencing metadata token of field
            int metadataToken = field.MetadataToken;
            foreach (var (method, il) in methodIL)
                {
                if (UnityMagicMethods.Contains(method.Name) || method.IsConstructor)
                    {
                    // still valid sources of reads/writes - do not skip
                    }

                for (int i = 0; i < il.Length; i++)
                    {
                    OpCodePattern(il, ref i, metadataToken, ref usage);
                    if (usage.Read && usage.Written) return; // early exit
                    }
                }
            }

        private static void OpCodePattern(byte[] il, ref int index, int metadataToken, ref FieldUsage usage)
            {
            // Very small decoder for field ops: ldfld (0x7B), ldflda (0x7C), stfld (0x7D)
            byte op = il[index];
            if (op == 0x7B || op == 0x7C || op == 0x7D)
                {
                // Next 4 bytes (little endian) are metadata token in this simple context (typical for reflection emit reading)
                if (index + 4 >= il.Length) return;
                int token = BitConverter.ToInt32(il, index + 1);
                if (token == metadataToken)
                    {
                    if (op == 0x7D) usage.Written = true; else usage.Read = true;
                    }
                index += 4; // advance past token bytes
                }
            }

        private static void SaveReport(StoryReport report)
            {
            try
                {
                string path = Path.Combine(Directory.GetCurrentDirectory(), "story-report.json");
                File.WriteAllText(path, JsonUtility.ToJson(new Wrapper(report), prettyPrint: true));
                Debug.Log($"[StoryTest] Report written: {path}");
                _lastReportCache = report;
                }
            catch (Exception ex)
                {
                Debug.LogError($"[StoryTest] Failed to write report: {ex.Message}");
                }
            }

        private static void TryWriteDiff(StoryReport current)
            {
            try
                {
                string prevPath = Path.Combine(Directory.GetCurrentDirectory(), "story-report-prev.json");
                string currentPath = Path.Combine(Directory.GetCurrentDirectory(), "story-report.json");
                if (!File.Exists(prevPath))
                    {
                    // First run or no baseline; copy current as prev baseline for next run
                    File.Copy(currentPath, prevPath, overwrite: true);
                    Debug.Log("[StoryTest] No previous report – baseline established.");
                    return;
                    }
                var prevJson = File.ReadAllText(prevPath);
                var prevWrapper = JsonUtility.FromJson<Wrapper>(prevJson);
                var diff = ComputeDiff(prevWrapper, new Wrapper(current));
                string diffPath = Path.Combine(Directory.GetCurrentDirectory(), "story-report-diff.json");
                File.WriteAllText(diffPath, JsonUtility.ToJson(diff, prettyPrint: true));
                Debug.Log($"[StoryTest] Diff written: {diffPath}");
                // Rotate current to prev for next invocation
                File.Copy(currentPath, prevPath, overwrite: true);
                }
            catch (Exception ex)
                {
                Debug.LogWarning($"[StoryTest] Failed to write diff: {ex.Message}");
                }
            }

        [Serializable]
        [StoryIgnore("DTO for diff JSON serialization; fields only read via reflection (JsonUtility).")]
        private class DiffWrapper
            {
            public List<string> newPhantomProps = new();
            public List<string> resolvedPhantomProps = new();
            public List<string> newColdPublicMethods = new();
            public List<string> resolvedColdPublicMethods = new();
            public List<string> newHollowEnums = new();
            public List<string> resolvedHollowEnums = new();
            public List<string> newPrematureCelebrations = new();
            public List<string> resolvedPrematureCelebrations = new();
            public string previousVersion = string.Empty;
            public string currentVersion = string.Empty;
            public string generatedUtc = string.Empty;
            }

        private static DiffWrapper ComputeDiff(Wrapper prev, Wrapper current)
            {
            var diff = new DiffWrapper();
            diff.previousVersion = prev.version;
            diff.currentVersion = current.version;
            diff.generatedUtc = DateTime.UtcNow.ToString("o");
            DiffLists(prev.phantomProps, current.phantomProps, diff.newPhantomProps, diff.resolvedPhantomProps);
            DiffLists(prev.coldPublicMethods, current.coldPublicMethods, diff.newColdPublicMethods, diff.resolvedColdPublicMethods);
            DiffLists(prev.hollowEnums, current.hollowEnums, diff.newHollowEnums, diff.resolvedHollowEnums);
            DiffLists(prev.prematureCelebrations, current.prematureCelebrations, diff.newPrematureCelebrations, diff.resolvedPrematureCelebrations);
            return diff;
            }

        private static void DiffLists(List<string> prev, List<string> current, List<string> added, List<string> removed)
            {
            var prevSet = new HashSet<string>(prev ?? new());
            var currSet = new HashSet<string>(current ?? new());
            foreach (var item in currSet) if (!prevSet.Contains(item)) added.Add(item);
            foreach (var item in prevSet) if (!currSet.Contains(item)) removed.Add(item);
            }

        [Serializable]
        [StoryIgnore("DTO for main JSON serialization; public fields are reflection-read only.")]
        private class Wrapper
            {
            public List<string> phantomProps;
            public List<string> coldPublicMethods;
            public List<string> hollowEnums;
            public List<string> prematureCelebrations;
            public List<string> suppressedPhantomProps;
            public List<string> suppressedColdPublicMethods;
            public List<string> suppressedHollowEnums;
            public List<string> suppressedPrematureCelebrations;
            public string generatedUtc;
            public string version;
            public string highestSeverity;
            public Wrapper(StoryReport r)
                {
                phantomProps = r.PhantomProps;
                coldPublicMethods = r.ColdPublicMethods;
                hollowEnums = r.HollowEnums;
                prematureCelebrations = r.PrematureCelebrations;
                suppressedPhantomProps = r.SuppressedPhantomProps;
                suppressedColdPublicMethods = r.SuppressedColdPublicMethods;
                suppressedHollowEnums = r.SuppressedHollowEnums;
                suppressedPrematureCelebrations = r.SuppressedPrematureCelebrations;
                generatedUtc = r.GeneratedAt.ToString("o");
                version = r.ToolVersion;
                highestSeverity = r.HighestSeverity.ToString();
                }
            // Parameterless ctor for JsonUtility (if needed)
            public Wrapper()
                {
                phantomProps = new List<string>();
                coldPublicMethods = new List<string>();
                hollowEnums = new List<string>();
                prematureCelebrations = new List<string>();
                suppressedPhantomProps = new List<string>();
                suppressedColdPublicMethods = new List<string>();
                suppressedHollowEnums = new List<string>();
                suppressedPrematureCelebrations = new List<string>();
                generatedUtc = string.Empty;
                version = string.Empty;
                highestSeverity = Severity.Info.ToString();
                }
            }

        private static void LogSummary(StoryReport report)
            {
            if (report.PhantomProps.Count == 0 && report.ColdPublicMethods.Count == 0 && report.HollowEnums.Count == 0 && report.PrematureCelebrations.Count == 0)
                {
                Debug.Log($"[StoryTest] ✅ No narrative violations (rules scanned: PhantomProps, ColdPublicMethods, HollowEnums, PrematureCelebrations). Highest Severity: {report.HighestSeverity}");
                return;
                }

            if (report.PhantomProps.Count > 0)
                {
                Debug.LogWarning($"[StoryTest] Phantom Props: {report.PhantomProps.Count}");
                foreach (var p in report.PhantomProps)
                    {
                    Debug.LogWarning($"[StoryTest][PhantomProp] {p} -> Field written but never read.");
                    }
                }

            if (report.ColdPublicMethods.Count > 0)
                {
                Debug.LogWarning($"[StoryTest] Cold Public Methods: {report.ColdPublicMethods.Count}");
                foreach (var m in report.ColdPublicMethods)
                    {
                    Debug.LogWarning($"[StoryTest][ColdMethod] {m} -> Public method never invoked within assembly (may need event, call site, or StoryIgnore).");
                    }
                }

            if (report.HollowEnums.Count > 0)
                {
                Debug.LogWarning($"[StoryTest] Hollow Enums: {report.HollowEnums.Count}");
                foreach (var e in report.HollowEnums)
                    {
                    Debug.LogWarning($"[StoryTest][HollowEnum] {e} -> Enum type declared but never referenced in any field, parameter, return, or property signature.");
                    }
                }

            if (report.PrematureCelebrations.Count > 0)
                {
                Debug.LogWarning($"[StoryTest] Premature Celebrations: {report.PrematureCelebrations.Count}");
                foreach (var c in report.PrematureCelebrations)
                    {
                    Debug.LogWarning($"[StoryTest][PrematureCelebration] {c}");
                    }
                }

            if (report.SuppressedPhantomProps.Count + report.SuppressedColdPublicMethods.Count + report.SuppressedHollowEnums.Count + report.SuppressedPrematureCelebrations.Count > 0)
                {
                Debug.Log($"[StoryTest] ℹ️ Suppressed (coverage/heuristic) items: PhantomProps={report.SuppressedPhantomProps.Count}, ColdMethods={report.SuppressedColdPublicMethods.Count}, HollowEnums={report.SuppressedHollowEnums.Count}, PrematureCelebrations={report.SuppressedPrematureCelebrations.Count}");
                }

            Debug.Log($"[StoryTest] Highest Severity: {report.HighestSeverity}");
            }

        private static bool ReadinessNamePattern(string name)
            {
            name = name.Trim('_');
            return name.EndsWith("Ready", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith("IsReady", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith("Initialized", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith("IsInitialized", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith("Complete", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith("Completed", StringComparison.OrdinalIgnoreCase);
            }

        private static bool IsCelebratory(string msg)
            {
            string m = msg.ToLowerInvariant();
            if (m.Contains("✅")) return true;
            return m.Contains("complete") || m.Contains("completed") || m.Contains("success") || m.Contains("ready") ||
                   m.Contains("generated") || m.Contains("done") || m.Contains("finished") || m.Contains("initialized");
            }

        #region Global Call Graph & Heuristics
        private static void ExtractGlobalCallGraph(byte[] il, MethodInfo method, HashSet<int> globalInvokedTokens, HashSet<string> globalStringLiterals)
            {
            Module module = method.Module;
            for (int i = 0; i < il.Length; i++)
                {
                byte op = il[i];
                switch (op)
                    {
                    case 0x28: // call
                    case 0x6F: // callvirt
                        if (i + 4 < il.Length)
                            {
                            int token = BitConverter.ToInt32(il, i + 1);
                            globalInvokedTokens.Add(token);
                            i += 4;
                            }
                        break;
                    case 0x72: // ldstr
                        if (i + 4 < il.Length)
                            {
                            int token = BitConverter.ToInt32(il, i + 1);
                            try
                                {
                                string s = module.ResolveString(token);
                                if (!string.IsNullOrEmpty(s)) globalStringLiterals.Add(s);
                                }
                            catch { }
                            i += 4;
                            }
                        break;
                    default:
                        break;
                    }
                }
            }

        private static bool AppearsReflected(string fullFieldName, string fieldName, HashSet<string> literals)
            {
            // Simple heuristic: field name or full name appears alongside reflection verbs
            foreach (var lit in literals)
                {
                if (lit.IndexOf(fieldName, StringComparison.Ordinal) >= 0)
                    {
                    if (lit.Contains("GetField") || lit.Contains("GetProperty") || lit.Contains("Find") || lit.Contains("nameof") || lit.Contains(fieldName + "))"))
                        return true;
                    }
                }
            return false;
            }

        private static void IdentifyColdPublicMethods(StoryReport report, Assembly[] assemblies, HashSet<int> globalInvokedTokens, CoverageOverlay? coverage)
            {
            foreach (var asm in assemblies)
                {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException rtle) { types = rtle.Types.Where(t => t != null).ToArray()!; }
                foreach (var type in types)
                    {
                    if (type == null) continue;
                    if (!type.FullName!.StartsWith(RootNamespacePrefix, StringComparison.Ordinal)) continue;
                    if (type.IsDefined(typeof(StoryIgnoreAttribute), true)) continue;
                    var publicCandidates = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                        .Where(m => !m.IsConstructor && !UnityMagicMethods.Contains(m.Name) && !m.IsSpecialName && !m.IsAbstract && !m.IsDefined(typeof(StoryIgnoreAttribute), true))
                        .ToArray();
                    foreach (var candidate in publicCandidates)
                        {
                        int token = candidate.MetadataToken;
                        string full = type.FullName + "." + candidate.Name + "()";
                        bool covered = coverage != null && coverage.methodsInvoked.Contains(full);
                        if (!globalInvokedTokens.Contains(token))
                            {
                            if (covered)
                                report.SuppressedColdPublicMethods.Add(full + " (coverage)");
                            else
                                report.ColdPublicMethods.Add(full);
                            }
                        }
                    }
                }
            }

        private static void ComputeHighestSeverity(StoryReport report)
            {
            // Map rules to severities
            // Phantom Props -> Warning, Cold Methods -> Info, Hollow Enums -> Warning, Premature Celebrations -> Error
            Severity highest = Severity.Info;
            if (report.ColdPublicMethods.Count > 0) highest = Max(highest, Severity.Info);
            if (report.PhantomProps.Count > 0) highest = Max(highest, Severity.Warning);
            if (report.HollowEnums.Count > 0) highest = Max(highest, Severity.Warning);
            if (report.PrematureCelebrations.Count > 0) highest = Max(highest, Severity.Error);
            report.HighestSeverity = highest;
            }

        private static Severity Max(Severity a, Severity b) => a > b ? a : b;
        #endregion

        #region Coverage Overlay
        [Serializable]
        private class CoverageOverlay
            {
            public List<string> fieldsRead = new();
            public List<string> methodsInvoked = new();
            public List<string> enumsReferenced = new();
            }

        private static CoverageOverlay? LoadCoverageOverlay()
            {
            try
                {
                string path = Path.Combine(Directory.GetCurrentDirectory(), "story-coverage.json");
                if (!File.Exists(path)) return null;
                var json = File.ReadAllText(path);
                var overlay = JsonUtility.FromJson<CoverageOverlay>(json);
                return overlay;
                }
            catch (Exception ex)
                {
                Debug.LogWarning($"[StoryTest] Failed to load coverage overlay: {ex.Message}");
                return null;
                }
            }
        #endregion
        }
    }
#endif
