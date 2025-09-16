#if UNITY_EDITOR && METVD_FULL_DOTS
#nullable enable
// @Intent: Legacy Faculty SubScene toggle now delegates to canonical ritual system
// @CheekPreservation: Maintains backward compatibility while using unified orchestrator
// @Ritual: SUBSCENE_TOGGLE_FACULTY_V2_CANONICAL

using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.Scenes;
using TinyWalnutGames.MetVD.Authoring.Editor.RitualSupport;

namespace TinyWalnutGames.MetVD.Authoring.Editor
	{
	internal static class FacultySubSceneToggle
		{
		private const string RitualId = "FACULTY_TOGGLE_V2";

		[MenuItem("Tiny Walnut Games/MetVanDAMN!/Tools/SubScenes/Close && Reopen ALL SubScenes (Faculty V2)", priority = 31)]
		private static void Menu()
			{
			ExecuteCanonicalRitual(passCount: 1);
			}

		[MenuItem("Tiny Walnut Games/MetVanDAMN!/Tools/SubScenes/Close && Reopen ALL SubScenes (Faculty V2 x2 passes)", priority = 32)]
		private static void MenuDouble()
			{
			ExecuteCanonicalRitual(passCount: 2);
			}

		/// <summary>
		/// Public API for other systems to trigger Faculty-grade SubScene toggling
		/// </summary>
		public static void TryRun(int passCount)
			{
			ExecuteCanonicalRitual(passCount);
			}

		private static void ExecuteCanonicalRitual(int passCount)
			{
			Debug.Log($"📣 Faculty V2 delegating to canonical SubScene ritual system ({passCount} pass{(passCount > 1 ? "es" : "")})");

			if (!RitualLock.TryAcquire(RitualId))
				{
				Debug.LogWarning($"⚠️ SubScene ritual busy (held by {RitualLock.Owner}) - Faculty V2 operation skipped");
				return;
				}

			SubScene[] subScenes = UnityEngine.Object.FindObjectsByType<SubScene>(FindObjectsSortMode.None);
			if (subScenes.Length == 0)
				{
				Debug.Log("🔍 No SubScenes found to process");
				RitualLock.Release(RitualId);
				return;
				}

			Debug.Log($"🔄 Faculty V2 processing {subScenes.Length} SubScene(s) with {passCount} pass(es)");

			// Reset event trace to capture this ritual's activity
			SceneEventTrace.Reset();

			// Capture pre-state
			EditingState preState = CaptureEditingState(subScenes);
			DumpState("PRE-FACULTY", preState);

			// Execute ritual with pass loop
			ExecuteRitualPasses(subScenes, passCount, preState);
			}

		private static void ExecuteRitualPasses(SubScene[] subScenes, int passCount, EditingState preState)
			{
			int currentPass = 1;

			void RunPass()
				{
				Debug.Log($"🔄 FACULTY PASS {currentPass}/{passCount}: Processing {subScenes.Length} SubScenes...");

				// Close all SubScenes
				CloseAllSubScenes(subScenes);

				// Reopen after delay
				NonBlockingDelay.AfterFrames(3, () =>
				{
					Debug.Log($"🔄 FACULTY PASS {currentPass}/{passCount}: Reopening {subScenes.Length} SubScenes...");
					OpenAllSubScenes(subScenes);

					if (currentPass < passCount)
						{
						currentPass++;
						// Another delay before next pass
						NonBlockingDelay.AfterFrames(3, RunPass);
						}
					else
						{
						// Finalize ritual
						NonBlockingDelay.AfterFrames(3, () =>
						{
							FinalizeRitual(subScenes, preState);
						});
						}
				});
				}

			RunPass();
			}

		private static void CloseAllSubScenes(SubScene[] subScenes)
			{
			// Use the established internal method discovery from original code
			var subSceneInspectorUtilityType = System.Type.GetType("Unity.Scenes.Editor.SubSceneInspectorUtility, Unity.Scenes.Editor");

			if (subSceneInspectorUtilityType != null)
				{
				System.Reflection.MethodInfo closeMethod = subSceneInspectorUtilityType.GetMethod("CloseSceneWithoutSaving",
					System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

				if (closeMethod != null)
					{
					System.Reflection.ParameterInfo[] parameters = closeMethod.GetParameters();
					if (parameters.Length > 0 && parameters[0].ParameterType == typeof(SubScene[]))
						{
						// Bulk close with array parameter
						closeMethod.Invoke(null, new object[] { subScenes });
						Debug.Log($"📁 Faculty V2 bulk closed {subScenes.Length} SubScene(s) via CloseSceneWithoutSaving");
						return;
						}
					}
				}

			// Fallback to individual closes
			Debug.LogWarning("⚠️ Faculty V2 falling back to individual SubScene close operations");
			foreach (SubScene subScene in subScenes)
				{
				CloseSubSceneFallback(subScene);
				}
			}

		private static void OpenAllSubScenes(SubScene[] subScenes)
			{
			// Use the established internal method discovery
			var subSceneInspectorUtilityType = System.Type.GetType("Unity.Scenes.Editor.SubSceneInspectorUtility, Unity.Scenes.Editor");

			if (subSceneInspectorUtilityType != null)
				{
				System.Reflection.MethodInfo openMethod = subSceneInspectorUtilityType.GetMethod("EditScene",
					System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

				if (openMethod != null)
					{
					// EditScene typically takes single SubScene, so open individually
					foreach (SubScene subScene in subScenes)
						{
						System.Reflection.ParameterInfo[] parameters = openMethod.GetParameters();
						if (parameters.Length > 0 && parameters[0].ParameterType == typeof(SubScene))
							{
							openMethod.Invoke(null, new object[] { subScene });
							}
						}
					Debug.Log($"📂 Faculty V2 reopened {subScenes.Length} SubScene(s) via EditScene");
					return;
					}
				}

			// Fallback to manual scene management
			Debug.LogWarning("⚠️ Faculty V2 falling back to manual SubScene reopen operations");
			foreach (SubScene subScene in subScenes)
				{
				OpenSubSceneFallback(subScene);
				}
			}

		private static void CloseSubSceneFallback(SubScene subScene)
			{
			try
				{
				if (subScene.EditingScene.IsValid() && subScene.EditingScene.isLoaded)
					{
					UnityEditor.SceneManagement.EditorSceneManager.CloseScene(subScene.EditingScene, true);
					}
				}
			catch (System.Exception ex)
				{
				Debug.LogWarning($"⚠️ Faculty V2 fallback close failed for {subScene.name}: {ex.Message}");
				}
			}

		private static void OpenSubSceneFallback(SubScene subScene)
			{
			try
				{
				SceneAsset? sceneAsset = GetSceneAsset(subScene);
				if (sceneAsset != null)
					{
					string path = AssetDatabase.GetAssetPath(sceneAsset);
					if (!string.IsNullOrEmpty(path))
						{
						UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
						}
					}
				}
			catch (System.Exception ex)
				{
				Debug.LogWarning($"⚠️ Faculty V2 fallback open failed for {subScene.name}: {ex.Message}");
				}
			}

		private static void FinalizeRitual(SubScene[] subScenes, EditingState preState)
			{
			EditingState postState = CaptureEditingState(subScenes);
			DumpState("POST-FACULTY", postState);
			ReportDiff(preState, postState);

			Debug.Log($"📊 {SceneEventTrace.GetSummary()}");

			EditorApplication.RepaintHierarchyWindow();
			AssetDatabase.SaveAssets();

			Debug.Log($"✅ Faculty V2 ritual complete - hierarchy should now be clean");

			RitualLock.Release(RitualId);
			}

		private static UnityEditor.SceneAsset? GetSceneAsset(SubScene sub)
			{
			System.Reflection.FieldInfo f = typeof(SubScene).GetField("SceneAsset", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
			return f?.GetValue(sub) as UnityEditor.SceneAsset;
			}

		// State diagnostics (maintained for compatibility)
		private struct EditingState
			{
			public System.Collections.Generic.List<string> LoadedEditingSceneNames;
			}

		private static EditingState CaptureEditingState(SubScene[] subs)
			{
			var names = new System.Collections.Generic.List<string>();
			foreach (SubScene s in subs)
				{
				if (s != null && s.EditingScene.IsValid() && s.EditingScene.isLoaded)
					{
					names.Add(s.name);
					}
				}
			return new EditingState { LoadedEditingSceneNames = names };
			}

		private static void DumpState(string label, EditingState st)
			{
			Debug.Log($"🧾 {label} Editing SubScenes: {(st.LoadedEditingSceneNames.Count == 0 ? "(none)" : string.Join(", ", st.LoadedEditingSceneNames))}");
			}

		private static void ReportDiff(EditingState before, EditingState after)
			{
			var beforeSet = new System.Collections.Generic.HashSet<string>(before.LoadedEditingSceneNames);
			var afterSet = new System.Collections.Generic.HashSet<string>(after.LoadedEditingSceneNames);

			var closed = beforeSet.Except(afterSet).ToList();
			var remained = beforeSet.Intersect(afterSet).ToList();
			var newly = afterSet.Except(beforeSet).ToList();

			Debug.Log($"📊 Faculty V2 Diff -> ClosedOnly:[{string.Join(", ", closed)}] RemainedOpen:[{string.Join(", ", remained)}] NewlyOpened:[{string.Join(", ", newly)}]");
			}
		}
	}
#nullable restore
#endif
