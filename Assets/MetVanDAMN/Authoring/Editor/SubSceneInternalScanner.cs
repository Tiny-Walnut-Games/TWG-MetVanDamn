#if UNITY_EDITOR && METVD_FULL_DOTS
// @Intent: Enumerate internal SubScene-related static editor methods & cache signatures.
// Run this first if you want to inspect what Unity exposes in your Entities build.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring.Editor
	{
	internal static class SubSceneInternalScanner
		{
		private static bool _scanned;
		internal static readonly List<MethodInfo> AllCandidateMethods = new();

		[MenuItem("Tiny Walnut Games/MetVanDAMN/Diagnostics/Scan SubScene Internals", priority = 190)]
		private static void ScanMenu()
			{
			Scan();
			Report();
			}

		internal static void EnsureScanned()
			{
			if (!_scanned) Scan();
			}

		private static void Scan()
			{
			_scanned = true;
			AllCandidateMethods.Clear();

			IEnumerable<Assembly> asms = AppDomain.CurrentDomain.GetAssemblies()
				.Where(a => a.GetName().Name!.StartsWith("Unity.", StringComparison.Ordinal));

			foreach (Assembly asm in asms)
				{
				Type [ ] types;
				try { types = asm.GetTypes(); }
				catch { continue; }

				foreach (Type t in types)
					{
					if (!t.FullName!.Contains("SubScene", StringComparison.Ordinal)) continue;

					foreach (MethodInfo m in t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
						{
						if (m.IsGenericMethod) continue;
						ParameterInfo [ ] ps = m.GetParameters();
						if (ps.Length == 0) continue;

						// Need at least a SubScene or SubScene[] in parameter list to be interesting:
						if (!ps.Any(p => p.ParameterType == typeof(Unity.Scenes.SubScene) ||
										 p.ParameterType == typeof(Unity.Scenes.SubScene [ ])))
							continue;

						string lname = m.Name.ToLowerInvariant();
						if (!(lname.Contains("open") ||
							  lname.Contains("edit") ||
							  lname.Contains("close") ||
							  lname.Contains("scene")))
							continue;

						AllCandidateMethods.Add(m);
						}
					}
				}
			}

		private static void Report()
			{
			Debug.Log("🔍 SubScene Internal Methods (filtered):");
			foreach (MethodInfo mi in AllCandidateMethods)
				{
				string ps = string.Join(", ", mi.GetParameters().Select(p => p.ParameterType.Name));
				Debug.Log($"🧪 {mi.DeclaringType?.FullName}.{mi.Name}({ps})  -> {mi.ReturnType.Name}");
				}
			Debug.Log($"🔍 Total candidates: {AllCandidateMethods.Count}");
			}
		}
	}
#endif
