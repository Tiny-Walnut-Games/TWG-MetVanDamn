#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace LivingDevAgent.Editor.Modules
	{
	/// <summary>
	/// Forces Unity to refresh its menu cache by triggering a script recompilation
	/// This is a temporary script to overcome Unity's stubborn menu caching
	/// </summary>
	public static class MenuRefreshForcer
		{
		[MenuItem("Tiny Walnut Games/🔄 Force Menu Refresh")]
		public static void ForceMenuRefresh()
			{
			Debug.Log("🔄 Forcing Unity menu cache refresh...");

			// Force Unity to recompile scripts and refresh menus
			AssetDatabase.Refresh();
			// Force a UnityEditor UI refresh
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

			// @jmeyer1980 - 💡 Learning Opportunity 💡 - Turns out that this is deprecated:
			// 
			// `UnityEditor.EditorApplication.projectWindowChanged += () =>`
			// 
			// Instead, we use the following modernized approach:
			EditorApplication.projectChanged += OnProjectChanged;

			// Immediate menu refresh attempt
			EditorApplication.delayCall += () =>
			{
				RefreshAllEditorWindows();
				Debug.Log("✅ Immediate menu refresh completed!");
			};

			Debug.Log("✅ Menu refresh triggered! Check menus in a moment...");
			}

		[MenuItem("Tiny Walnut Games/🔄 Force Script Recompilation")]
		public static void ForceScriptRecompilation()
			{
			Debug.Log("🧙‍♂️ Forcing Unity script recompilation...");

			// First, refresh asset database
			AssetDatabase.Refresh();

			// Force script recompilation by touching a script file
			// This is the modern non-deprecated way to trigger recompilation
			ForceRecompilationByTouchingScript();

			// Also refresh UI after recompilation trigger
			EditorApplication.delayCall += () =>
			{
				RefreshAllEditorWindows();
				Debug.Log("⚡ Script recompilation triggered! Menus will refresh after compilation...");
			};

			Debug.Log("🔄 Recompilation request sent to Unity...");
			}

		[MenuItem("Tiny Walnut Games/🔄 Nuclear Menu + Script Refresh")]
		public static void NuclearMenuRefresh()
			{
			Debug.Log("💥 NUCLEAR REFRESH: Forcing both menu refresh AND script recompilation...");

			// Combine both approaches for maximum effectiveness
			AssetDatabase.Refresh();
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

			// Set up callbacks
			EditorApplication.projectChanged += OnProjectChanged;

			// Force recompilation
			ForceRecompilationByTouchingScript();

			// Immediate UI refresh
			EditorApplication.delayCall += () =>
			{
				RefreshAllEditorWindows();
				Debug.Log("✅ Nuclear refresh completed - menus should update after recompilation!");
			};

			Debug.Log("💥 Nuclear refresh engaged! Maximum menu cache destruction activated...");
			}

		private static void OnProjectChanged()
			{
			// This callback ensures that the menu is refreshed after the project window changes
			EditorApplication.delayCall += RefreshAllEditorWindows;

			// Clean up the callback to avoid memory leaks
			EditorApplication.projectChanged -= OnProjectChanged;

			Debug.Log("🔄 Project change detected - refreshing menus...");
			}

		private static void RefreshAllEditorWindows()
			{
			// Comprehensive editor refresh
			EditorApplication.RepaintHierarchyWindow();
			EditorApplication.RepaintProjectWindow();

			// Force menu system refresh without deprecated RequestScriptReload
			// The AssetDatabase.Refresh calls above should be sufficient for menu refresh
			// Menu cache refresh happens through the project change callback chain

			Debug.Log("🔄 All editor windows refreshed!");
			}

		private static void ForceRecompilationByTouchingScript()
			{
			// Modern approach: Create a temporary script file that Unity will compile
			// This forces Unity to recompile and refresh menus without using deprecated APIs
			string tempScriptPath = "Assets/__TempMenuRefreshTrigger.cs";
			string tempScriptContent =
				"// Temporary script to trigger Unity recompilation\n" +
				"// This file is automatically deleted\n" +
				"#if UNITY_EDITOR\n" +
				"using UnityEngine;\n" +
				"public class TempMenuRefreshTrigger\n" +
				"{\n" +
				$"    // Generated at {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
				"    // This triggers script recompilation and menu refresh\n" +
				"}\n" +
				"#endif";

			try
				{
				// Write temporary script
				System.IO.File.WriteAllText(tempScriptPath, tempScriptContent);
				AssetDatabase.ImportAsset(tempScriptPath);

				Debug.Log("🧙‍♂️ Temporary recompilation trigger created...");

				// Schedule deletion after compilation
				EditorApplication.delayCall += () =>
				{
					EditorApplication.delayCall += () =>
					{
						// Double delay to ensure compilation happens first
						if (System.IO.File.Exists(tempScriptPath))
							{
							AssetDatabase.DeleteAsset(tempScriptPath);
							Debug.Log("🧹 Temporary recompilation trigger cleaned up!");
							}
					};
				};
				}
			catch (System.Exception ex)
				{
				Debug.LogWarning($"⚠️ Could not create temporary recompilation trigger: {ex.Message}");
				}
			}
		}
	}
#endif
