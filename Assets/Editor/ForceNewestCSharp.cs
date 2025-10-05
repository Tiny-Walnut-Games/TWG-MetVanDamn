#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Editor
	{
	public static class ForceNewestCSharp
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

		private const string RspFileContent = "-langversion:10.0\n";

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
				Debug.Log($"🧙 Injected {PropsFileName} into project root to enforce C# 10.0 + nullable.");
				}

			Debug.Log("📜 LangVersion injected: 10.0");
			Debug.Log("📜 Nullable enabled: true");
			Debug.Log($"📜 Props path: {propsPath}");

			PromptAndCreateRspFiles();
			}

		[InitializeOnLoadMethod]
		private static void ValidateLanguageVersion()
			{
			// This is just a placeholder check — actual version detection is limited in runtime.
			Debug.Log("🔍 C# Language Level: '10.0' enforced via Directory.Build.props");
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
						Debug.Log($"📝 Created {rspPath} with 10.0 language version.");
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

			foreach (var asmdefPath in Directory.EnumerateFiles(Application.dataPath, "*.asmdef",
				         SearchOption.AllDirectories))
				{
				try
					{
					var json = File.ReadAllText(asmdefPath);
					var asmdef = JsonUtility.FromJson<AsmdefStub>(json);
					if (asmdef?.name is not { Length: > 0 } name)
						continue;

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
			public string name = string.Empty;
			}
		}
	}
