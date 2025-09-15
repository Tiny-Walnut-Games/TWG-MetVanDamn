#if UNITY_EDITOR && METVD_FULL_DOTS
// @Intent: Simple isolated manual test runner for the faculty toggle.
using UnityEditor;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring.Editor.Tests
	{
	internal static class RunFacultyToggleOnce
		{
		[MenuItem("Tiny Walnut Games/MetVanDAMN/Diagnostics/Test Faculty Toggle Pass", priority = 195)]
		private static void Run()
			{
			Debug.Log("🧪 Invoking FacultySubSceneToggle (1 pass)...");
			TinyWalnutGames.MetVD.Authoring.Editor.FacultySubSceneToggle.TryRun(passCount: 1);
			}
		}
	}
#endif
