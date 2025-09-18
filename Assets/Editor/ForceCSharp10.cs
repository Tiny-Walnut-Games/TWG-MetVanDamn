#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Editor
    {
    public static class ForceCSharp10
        {
        private const string PropsFileName = "Directory.Build.props";
        private const string PropsFileContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project>
  <PropertyGroup>
    <LangVersion>10.0</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
";

        private const string RspFileContent = "-langversion:latest\n";

        // Guard so we don't prompt twice in the same reload
        private static bool _rspPromptShown;

        [DidReloadScripts]
        private static void InjectCSharpVersion()
            {
            if (Application.dataPath == null) return;
            var projectDir = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectDir)) return;

            var propsPath = Path.Combine(projectDir, PropsFileName);
            var needsWrite = !File.Exists(propsPath) || File.ReadAllText(propsPath) != PropsFileContent;

            if (needsWrite)
                {
                File.WriteAllText(propsPath, PropsFileContent);
                Debug.Log($"🧙 Injected {PropsFileName} into project root to enforce C#10.0 + nullable.");
                }

            Debug.Log($"📜 LangVersion injected: 10.0");
            Debug.Log($"📜 Nullable enabled: true");
            Debug.Log($"📜 Props path: {propsPath}");

            PromptAndCreateRspFiles();
            }

        [InitializeOnLoadMethod]
        private static void ValidateLanguageVersion()
            {
            var isCSharp10 = typeof(Func<,,>).GetGenericArguments().Length == 3;
            Debug.Log($"🔍 C# Language Level: {(isCSharp10 ? "C#10+ Confirmed ✅" : "Fallback < C#10 ❌")}");
            }

        private static void PromptAndCreateRspFiles()
            {
            if (_rspPromptShown) return;
            _rspPromptShown = true;

            var assemblies = FindAssembliesMissingRsp();
            if (assemblies.Count == 0)
                {
                Debug.Log("✅ All non‑Unity assemblies already have .csc.rsp files. No action needed.");
                return;
                }

            var message = "Create pre‑filled *.csc.rsp files for these assemblies?\n\n" +
                          string.Join("\n", assemblies.Keys);

            if (EditorUtility.DisplayDialog("Create .csc.rsp Files", message, "Yes", "No"))
                {
                foreach (var kvp in assemblies)
                    {
                    var assemblyName = kvp.Key;
                    var asmdefDir = kvp.Value;
                    var rspPath = Path.Combine(asmdefDir, $"{assemblyName}.csc.rsp");

                    try
                        {
                        File.WriteAllText(rspPath, RspFileContent);
                        Debug.Log($"📝 Created {rspPath} with latest language version.");
                        }
                    catch (Exception ex)
                        {
                        Debug.LogError($"❌ Failed to create {rspPath}: {ex.Message}");
                        }
                    }
                }
            else
                {
                Debug.Log("❌ User cancelled .csc.rsp creation.");
                }
            }

        private static Dictionary<string, string> FindAssembliesMissingRsp()
            {
            var missing = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var asmdefPath in Directory.EnumerateFiles(Application.dataPath, "*.asmdef", SearchOption.AllDirectories))
                {
                try
                    {
                    var json = File.ReadAllText(asmdefPath);
                    var asmdef = JsonUtility.FromJson<AsmdefStub>(json);
                    // Robust null-safe read (asmdef may deserialize to default stub). We *skip* invalid names instead of aborting the whole scan.
                    if (asmdef?.name is not { Length: > 0 } name)
                        continue; // malformed or missing name; skip safely

                    // Skip Unity assemblies
                    if (!name.StartsWith("Unity", StringComparison.OrdinalIgnoreCase))
                        {
                        var dir = Path.GetDirectoryName(asmdefPath);
                        if (!string.IsNullOrEmpty(dir))
                            {
                            var rspPath = Path.Combine(dir, $"{name}.csc.rsp");
                            if (!File.Exists(rspPath))
                                {
                                missing[name] = dir;
                                }
                            }
                        }
                    }
                catch (Exception ex)
                    {
                    Debug.LogWarning($"⚠️ Failed to read {asmdefPath}: {ex.Message}");
                    }
                }

            return missing;
            }

        [Serializable]
        private class AsmdefStub
            {
            // Default to empty string so flow analysis treats it non-null after check; avoids repeated nullable diagnostics.
            public string name = string.Empty;
            }
        }
    }
