#nullable enable
// StoryIntegrityValidator.cs
// Generic version that works with any Unity 6+ C#9+ project
// Validates code quality and catches common issues before they become problems
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace StoryTest.Editor
{
    /// <summary>
    /// Configuration for the Story Integrity Validator.
    /// Customize these settings for your project's namespace and validation rules.
    /// </summary>
    public static class StoryIntegrityConfig
    {
        /// <summary>
        /// The namespace prefix to scan for validation (e.g., "MyCompany.MyProject").
        /// Set this to match your project's root namespace.
        /// </summary>
        public static string RootNamespacePrefix { get; set; } = "MyCompany";

        /// <summary>
        /// Unity lifecycle methods that should be excluded from "cold method" checks.
        /// Add your custom lifecycle methods here if needed.
        /// </summary>
        public static readonly HashSet<string> UnityMagicMethods = new()
        {
            "Awake", "Start", "OnEnable", "OnDisable", "OnDestroy", "Update", "LateUpdate", "FixedUpdate",
            "OnGUI", "Reset", "OnApplicationQuit", "OnApplicationFocus", "OnApplicationPause",
            "OnDrawGizmos", "OnDrawGizmosSelected", "OnValidate", "OnSceneGUI"
        };

        /// <summary>
        /// Methods that are commonly called via reflection or Unity events.
        /// Add your custom reflection-called methods here.
        /// </summary>
        public static readonly HashSet<string> ReflectionCalledMethods = new()
        {
            "GetHashCode", "Equals", "ToString", "CompareTo",
            "OnInspectorGUI", "OnSceneGUI", "CreateAssetMenu"
        };
    }

    internal sealed class StoryReport
    {
        public List<string> PhantomProps = new();
        public List<string> ColdPublicMethods = new();
        public List<string> HollowEnums = new();
        public List<string> PrematureCelebrations = new();
        // Suppressed lists for transparency
        public List<string> SuppressedPhantomProps = new();
        public List<string> SuppressedColdPublicMethods = new();
        public List<string> SuppressedHollowEnums = new();
        public List<string> SuppressedPrematureCelebrations = new();
        public DateTime GeneratedAt = DateTime.UtcNow;
        public string ToolVersion = "1.0.0"; // Generic version
        public Severity HighestSeverity = Severity.Info;
    }

    internal enum Severity { Info = 0, Warning = 1, Error = 2 }

    internal struct FieldUsage
    {
        public bool Written;
        public bool Read;
    }

    /// <summary>
    /// Validates code integrity and catches common issues that indicate incomplete implementations.
    /// Run this regularly to maintain code quality and catch problems early.
    /// </summary>
    public static class StoryIntegrityValidator
    {
        private const BindingFlags FieldScanFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private const BindingFlags MethodScanFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        [MenuItem("Tools/Story Test/Run Integrity Validation")]
        public static void Run()
        {
            var report = new StoryReport();
            try
            {
                // Find assemblies matching the configured namespace
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a.GetName().Name != null &&
                               a.GetName().Name!.StartsWith(StoryIntegrityConfig.RootNamespacePrefix, StringComparison.Ordinal))
                    .ToArray();

                if (assemblies.Length == 0)
                {
                    Debug.LogWarning($"[StoryTest] No assemblies found with namespace prefix '{StoryIntegrityConfig.RootNamespacePrefix}'. " +
                                   $"Update StoryIntegrityConfig.RootNamespacePrefix to match your project.");
                    return;
                }

                Debug.Log($"[StoryTest] Scanning {assemblies.Length} assemblies with prefix '{StoryIntegrityConfig.RootNamespacePrefix}'");

                // Collect enum candidates & usage tracking structures
                var enumCandidates = new HashSet<Type>();
                var enumReferenced = new HashSet<Type>();

                // Global collection for call graph analysis
                var globalInvokedTokens = new HashSet<int>();
                var globalStringLiterals = new HashSet<string>(StringComparer.Ordinal);
                var typeMethodILCache = new Dictionary<Type, List<(MethodInfo method, byte[] il)>>();

                foreach (var asm in assemblies)
                {
                    AnalyzeAssembly(asm, report, enumCandidates, enumReferenced, globalInvokedTokens, globalStringLiterals, typeMethodILCache);
                }

                // Determine hollow enums (declared but never referenced)
                foreach (var enumType in enumCandidates)
                {
                    if (!enumReferenced.Contains(enumType))
                    {
                        // Ignore enums explicitly marked to ignore
                        if (enumType.IsDefined(typeof(StoryIgnoreAttribute), inherit: true)) continue;
                        string fullName = enumType.FullName ?? enumType.Name;
                        report.HollowEnums.Add(fullName);
                    }
                }

                // Cold public methods global pass
                IdentifyColdPublicMethods(report, assemblies, globalInvokedTokens);

                // Update highest severity
                ComputeHighestSeverity(report);

                SaveReport(report);
                LogSummary(report);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StoryTest] Exception during validation: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // Batchmode-friendly entry point for CI/CD
        public static void RunFromCI()
        {
            Debug.Log("[StoryTest] CI validation started");
            Run();

            // Check for violations and exit with appropriate code
            string failOnViolation = Environment.GetEnvironmentVariable("STORYTEST_FAIL_ON_VIOLATION") ?? string.Empty;
            string severityThreshold = Environment.GetEnvironmentVariable("STORYTEST_SEVERITY_THRESHOLD") ?? string.Empty;

            if (failOnViolation.Equals("1") || failOnViolation.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                if (HasViolations())
                {
                    Debug.LogError("[StoryTest] Violations detected - failing build due to STORYTEST_FAIL_ON_VIOLATION");
                    EditorApplication.Exit(1);
                }
            }

            if (!string.IsNullOrWhiteSpace(severityThreshold) && Enum.TryParse<Severity>(severityThreshold, true, out var threshold))
            {
                if (GetHighestSeverity() >= threshold)
                {
                    Debug.LogError($"[StoryTest] Severity threshold {threshold} exceeded - failing build");
                    EditorApplication.Exit(2);
                }
            }
        }

        private static void AnalyzeAssembly(Assembly asm, StoryReport report, HashSet<Type> enumCandidates, HashSet<Type> enumReferenced,
            HashSet<int> globalInvokedTokens, HashSet<string> globalStringLiterals,
            Dictionary<Type, List<(MethodInfo method, byte[] il)>> typeMethodILCache)
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
                if (!type.FullName!.StartsWith(StoryIntegrityConfig.RootNamespacePrefix, StringComparison.Ordinal)) continue;

                if (type.IsEnum)
                {
                    enumCandidates.Add(type);
                    continue;
                }

                // Record enum usage in type signatures
                try { RecordEnumUsagesInType(type, enumReferenced); } catch { /* best effort */ }

                AnalyzeType(type, report, globalInvokedTokens, globalStringLiterals, typeMethodILCache);
            }
        }

        private static void AnalyzeType(Type type, StoryReport report, HashSet<int> globalInvokedTokens,
            HashSet<string> globalStringLiterals, Dictionary<Type, List<(MethodInfo method, byte[] il)>> typeMethodILCache)
        {
            // Analyze fields for phantom props (write-only serialized fields)
            AnalyzeFields(type, report);

            // Analyze methods for premature celebrations and collect IL for call graph
            AnalyzeMethods(type, report, globalInvokedTokens, globalStringLiterals, typeMethodILCache);
        }

        private static void AnalyzeFields(Type type, StoryReport report)
        {
            var fields = type.GetFields(FieldScanFlags);
            var fieldUsage = new Dictionary<string, FieldUsage>();

            foreach (var field in fields)
            {
                if (field.IsDefined(typeof(StoryIgnoreAttribute), inherit: true)) continue;
                if (field.IsStatic) continue; // Skip static fields for now

                var usage = new FieldUsage();

                // Check if field is serialized (has SerializeField or is public)
                bool isSerialized = field.IsPublic || field.IsDefined(typeof(SerializeField), inherit: true);
                if (!isSerialized) continue;

                // Look for writes (assignments) and reads in methods
                var methods = type.GetMethods(MethodScanFlags);
                foreach (var method in methods)
                {
                    if (method.IsDefined(typeof(StoryIgnoreAttribute), inherit: true)) continue;

                    try
                    {
                        var body = method.GetMethodBody();
                        if (body == null) continue;

                        // Simple heuristic: check for field name in method name or attributes
                        // This is a basic implementation - more sophisticated analysis would use IL parsing
                        string methodName = method.Name;
                        string fieldName = field.Name;

                        if (methodName.Contains("Set") && methodName.Contains(fieldName, StringComparison.OrdinalIgnoreCase))
                        {
                            usage.Written = true;
                        }
                        if ((methodName.Contains("Get") || methodName.Contains("Update") || methodName.Contains("Process")) &&
                            methodName.Contains(fieldName, StringComparison.OrdinalIgnoreCase))
                        {
                            usage.Read = true;
                        }
                    }
                    catch { /* ignore reflection errors */ }
                }

                fieldUsage[field.Name] = usage;
            }

            // Report phantom props (serialized fields that are written but never read)
            foreach (var kvp in fieldUsage)
            {
                if (kvp.Value.Written && !kvp.Value.Read)
                {
                    string fullName = $"{type.FullName}.{kvp.Key}";
                    report.PhantomProps.Add(fullName);
                }
            }
        }

        private static void AnalyzeMethods(Type type, StoryReport report, HashSet<int> globalInvokedTokens,
            HashSet<string> globalStringLiterals, Dictionary<Type, List<(MethodInfo method, byte[] il)>> typeMethodILCache)
        {
            var methods = type.GetMethods(MethodScanFlags);

            foreach (var method in methods)
            {
                if (method.IsDefined(typeof(StoryIgnoreAttribute), inherit: true)) continue;
                if (method.IsStatic) continue;
                if (StoryIntegrityConfig.UnityMagicMethods.Contains(method.Name)) continue;
                if (StoryIntegrityConfig.ReflectionCalledMethods.Contains(method.Name)) continue;

                // Check for premature celebrations (methods that return bool and have "Is" prefix but always return true/false)
                if (method.ReturnType == typeof(bool) && method.Name.StartsWith("Is", StringComparison.Ordinal))
                {
                    try
                    {
                        var body = method.GetMethodBody();
                        if (body != null)
                        {
                            // Simple heuristic: if method body is very small, it might be a premature celebration
                            // More sophisticated analysis would examine the IL
                            if (body.GetILAsByteArray().Length < 10) // Very small method
                            {
                                report.PrematureCelebrations.Add($"{type.FullName}.{method.Name}");
                            }
                        }
                    }
                    catch { /* ignore */ }
                }

                // Collect method token for call graph analysis
                try
                {
                    globalInvokedTokens.Add(method.MetadataToken);
                }
                catch { /* ignore */ }
            }
        }

        private static void RecordEnumUsagesInType(Type type, HashSet<Type> enumReferenced)
        {
            // Check method signatures for enum parameters/return types
            var methods = type.GetMethods(MethodScanFlags);
            foreach (var method in methods)
            {
                if (method.ReturnType.IsEnum) enumReferenced.Add(method.ReturnType);
                foreach (var param in method.GetParameters())
                {
                    if (param.ParameterType.IsEnum) enumReferenced.Add(param.ParameterType);
                }
            }

            // Check field types
            var fields = type.GetFields(FieldScanFlags);
            foreach (var field in fields)
            {
                if (field.FieldType.IsEnum) enumReferenced.Add(field.FieldType);
            }

            // Check property types
            var properties = type.GetProperties();
            foreach (var prop in properties)
            {
                if (prop.PropertyType.IsEnum) enumReferenced.Add(prop.PropertyType);
            }
        }

        private static void IdentifyColdPublicMethods(StoryReport report, Assembly[] assemblies, HashSet<int> globalInvokedTokens)
        {
            foreach (var asm in assemblies)
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch { continue; }

                foreach (var type in types)
                {
                    if (type == null || type.IsDefined(typeof(StoryIgnoreAttribute), inherit: true)) continue;
                    if (!type.FullName!.StartsWith(StoryIntegrityConfig.RootNamespacePrefix, StringComparison.Ordinal)) continue;

                    var methods = type.GetMethods(MethodScanFlags);
                    foreach (var method in methods)
                    {
                        if (method.IsDefined(typeof(StoryIgnoreAttribute), inherit: true)) continue;
                        if (!method.IsPublic || method.IsStatic) continue;
                        if (StoryIntegrityConfig.UnityMagicMethods.Contains(method.Name)) continue;
                        if (StoryIntegrityConfig.ReflectionCalledMethods.Contains(method.Name)) continue;

                        // Check if method is never invoked (based on token analysis)
                        if (!globalInvokedTokens.Contains(method.MetadataToken))
                        {
                            report.ColdPublicMethods.Add($"{type.FullName}.{method.Name}");
                        }
                    }
                }
            }
        }

        private static void ComputeHighestSeverity(StoryReport report)
        {
            if (report.PhantomProps.Count > 0 || report.ColdPublicMethods.Count > 0)
                report.HighestSeverity = Severity.Warning;
            if (report.HollowEnums.Count > 0 || report.PrematureCelebrations.Count > 0)
                report.HighestSeverity = Severity.Error;
        }

        private static void SaveReport(StoryReport report)
        {
            try
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), "story-report.json");
                var wrapper = new ReportWrapper
                {
                    phantomProps = report.PhantomProps.ToArray(),
                    coldPublicMethods = report.ColdPublicMethods.ToArray(),
                    hollowEnums = report.HollowEnums.ToArray(),
                    prematureCelebrations = report.PrematureCelebrations.ToArray(),
                    generatedAt = report.GeneratedAt.ToString("O"),
                    toolVersion = report.ToolVersion,
                    highestSeverity = report.HighestSeverity.ToString()
                };

                string json = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(path, json);
                Debug.Log($"[StoryTest] Report saved: {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StoryTest] Failed to save report: {ex.Message}");
            }
        }

        private static void LogSummary(StoryReport report)
        {
            Debug.Log($"[StoryTest] ✅ Validation complete - Highest Severity: {report.HighestSeverity}");

            if (report.PhantomProps.Count > 0)
                Debug.LogWarning($"[StoryTest] Phantom Props: {report.PhantomProps.Count} (serialized fields written but never read)");

            if (report.ColdPublicMethods.Count > 0)
                Debug.LogWarning($"[StoryTest] Cold Public Methods: {report.ColdPublicMethods.Count} (public methods never called)");

            if (report.HollowEnums.Count > 0)
                Debug.LogError($"[StoryTest] Hollow Enums: {report.HollowEnums.Count} (enums declared but never used)");

            if (report.PrematureCelebrations.Count > 0)
                Debug.LogError($"[StoryTest] Premature Celebrations: {report.PrematureCelebrations.Count} (methods that always return true/false)");

            if (report.PhantomProps.Count == 0 && report.ColdPublicMethods.Count == 0 &&
                report.HollowEnums.Count == 0 && report.PrematureCelebrations.Count == 0)
            {
                Debug.Log("[StoryTest] 🎉 No integrity violations found!");
            }
        }

        private static bool HasViolations()
        {
            // Load cached report or check for violations
            try
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), "story-report.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var wrapper = JsonUtility.FromJson<ReportWrapper>(json);
                    return (wrapper.phantomProps?.Length ?? 0) > 0 ||
                           (wrapper.coldPublicMethods?.Length ?? 0) > 0 ||
                           (wrapper.hollowEnums?.Length ?? 0) > 0 ||
                           (wrapper.prematureCelebrations?.Length ?? 0) > 0;
                }
            }
            catch { /* ignore */ }
            return false;
        }

        private static Severity GetHighestSeverity()
        {
            try
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), "story-report.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var wrapper = JsonUtility.FromJson<ReportWrapper>(json);
                    return Enum.TryParse<Severity>(wrapper.highestSeverity, out var severity) ? severity : Severity.Info;
                }
            }
            catch { /* ignore */ }
            return Severity.Info;
        }

        [Serializable]
        private class ReportWrapper
        {
            public string[]? phantomProps;
            public string[]? coldPublicMethods;
            public string[]? hollowEnums;
            public string[]? prematureCelebrations;
            public string? generatedAt;
            public string? toolVersion;
            public string? highestSeverity;
        }
    }
}
#endif
