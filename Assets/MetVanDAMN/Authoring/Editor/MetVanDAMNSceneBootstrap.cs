#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_2021_3_OR_NEWER
#endif

#if METVD_FULL_DOTS
using TinyWalnutGames.MetVD.Samples; // SmokeTestSceneSetup
using Unity.Scenes;                  // SubScene
#endif

namespace TinyWalnutGames.MetVD.Authoring.Editor
	{
	public static class MetVanDAMNSceneBootstrap
		{
		private const string RootSceneName = "MetVanDAMN_Baseline";
		private static readonly string [ ] SubSceneNames = { "WorldGen_Terrain", "WorldGen_Dungeon", "NPC_Interactions", "UI_HUD" };
		private const string ScenesRootFolder = "Assets/Scenes";
		private const string SubScenesFolder = "Assets/Scenes/SubScenes";
		private static bool _fallbackTriggeredThisRun = false; // track if fallback unloaded baseline
		private static string _currentRootScenePath;           // track path for reopening after fallback

#if !METVD_FULL_DOTS
        [AddComponentMenu("")]
        private class SubSceneMarker : MonoBehaviour { }
#endif

		[MenuItem("Tiny Walnut Games/MetVanDAMN/Sample Creation/Create Baseline Scene %#m", priority = 10)]
		public static void CreateBaseline ()
			{
			CreateBaselineScene();
			}

		private static void CreateBaselineScene ()
			{
			_fallbackTriggeredThisRun = false;
			EnsureFolder(ScenesRootFolder);
			EnsureFolder(SubScenesFolder);

			string rootPath = Path.Combine(ScenesRootFolder, RootSceneName + ".unity").Replace("\\", "/");
			_currentRootScenePath = rootPath;
			if (File.Exists(rootPath))
				{
				bool overwrite = EditorUtility.DisplayDialog(
					"Overwrite Existing?",
					"A scene already exists at:\n" + rootPath + "\n\nOverwrite? (This will recreate sub‑scene links)",
					"Overwrite", "Cancel");
				if (!overwrite)
					{
					Debug.Log("⏭ Baseline scene creation aborted by user.");
					return;
					}
				}

#if UNITY_2021_3_OR_NEWER
			// Prevent prefab isolation issues
			if (PrefabStageUtility.GetCurrentPrefabStage() != null)
				{
				if (!EditorUtility.DisplayDialog("Exit Prefab Stage?", "You are editing a prefab which blocks additive scene creation. Exit and continue?", "Yes", "Cancel"))
					{
					return;
					}

				if (PrefabStageUtility.GetCurrentPrefabStage().scene.isDirty)
					{
					PrefabStageUtility.GetCurrentPrefabStage().ClearDirtiness();
					}

				AssetDatabase.SaveAssets();
				EditorSceneManager.CloseScene(SceneManager.GetActiveScene(), true);
				}
#endif

			// 🔥 PHASE 1: Create and save baseline root scene
			Scene rootScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
			rootScene.name = RootSceneName;
			CreateBaselineEnvironment();
			CreateBootstrapMono();
			if (!EditorSceneManager.SaveScene(rootScene, rootPath))
				{
				Debug.LogError("❌ Failed initial save of root scene at " + rootPath);
				return;
				}

			// 🧙‍♂️ PHASE 2: Create all SubScene files first, let meta files stabilize
			Debug.Log("📁 Phase 2: Creating SubScene files...");
			foreach (string subName in SubSceneNames)
				{
				ReopenRootIfNeeded();
				if (!SafeEnsureScene(Path.Combine(SubScenesFolder, subName + ".unity").Replace("\\", "/"), subName))
					{
					Debug.LogError($"❌ Failed to create scene: {subName}");
					return;
					}
				}

			// 🔥 PHASE 3: Force complete asset database synchronization
			Debug.Log("🔄 Phase 3: Synchronizing asset database...");
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

			// 🧙‍♂️ PHASE 4: Wait for ALL meta files to be stable
			Debug.Log("⏳ Phase 4: Validating meta file generation...");
			bool allMetaFilesReady = false;
			int metaAttempts = 0;
			while (!allMetaFilesReady && metaAttempts < 30)
				{
				allMetaFilesReady = true;
				foreach (string subName in SubSceneNames)
					{
					string scenePath = Path.Combine(SubScenesFolder, subName + ".unity").Replace("\\", "/");
					string metaPath = scenePath + ".meta";
					string guid = AssetDatabase.AssetPathToGUID(scenePath);

					if (!File.Exists(metaPath) || string.IsNullOrEmpty(guid))
						{
						allMetaFilesReady = false;
						break;
						}
					}

				if (!allMetaFilesReady)
					{
					System.Threading.Thread.Sleep(200);
					metaAttempts++;
					}
				}

			if (!allMetaFilesReady)
				{
				Debug.LogWarning("⚠️ Meta file validation timeout. Proceeding anyway...");
				}
			else
				{
				Debug.Log("✅ All meta files validated successfully.");
				}

			// 🎯 PHASE 5: Create SubScene GameObjects WITHOUT references first
			Debug.Log("🔗 Phase 5: Creating SubScene GameObjects...");
			ReopenRootIfNeeded();
			var parentGO = GameObject.Find("_SubScenes");
			if (!parentGO)
				{
				parentGO = new GameObject("_SubScenes");
				}

			// Create all SubScene GameObjects first (no scene references yet)
			foreach (string subName in SubSceneNames)
				{
				CreateSubSceneGameObject(subName, parentGO.transform);
				}

			// 🔥 PHASE 6: Save scene with empty SubScene components
			Debug.Log("💾 Phase 6: Saving scene with SubScene GameObjects...");
			ReopenRootIfNeeded();
			Scene rootForGameObjects = SceneManager.GetSceneByPath(rootPath);
			if (rootForGameObjects.IsValid())
				{
				EditorSceneManager.MarkSceneDirty(rootForGameObjects);
				EditorSceneManager.SaveScene(rootForGameObjects);
				}

			// 🧙‍♂️ PHASE 7: Force asset database sync and wait
			System.Threading.Thread.Sleep(500); // Let Unity digest the scene structure
			Debug.Log("🔄 Phase 7: Synchronizing before reference assignment...");
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			System.Threading.Thread.Sleep(500); // Let Unity digest the scene structure

			// 🎯 PHASE 8: Assign SceneAsset references to existing SubScene components
			Debug.Log("🔗 Phase 8: Assigning SceneAsset references...");

			//// 🔥 CRITICAL: Ensure root scene is open AND active before reference assignment
			//ReopenRootIfNeeded();
			//Scene activeRoot = SceneManager.GetSceneByPath(rootPath);
			//if (!activeRoot.IsValid() || !activeRoot.isLoaded)
			//{
			//    Debug.LogError("❌ Root scene not properly loaded for reference assignment!");
			//    return;
			//}

			// 🧙‍♂️ TIMING FIX: Ensure Unity has fully processed the scene structure
			EditorApplication.delayCall += () =>
			{
				Debug.Log("🔗 Phase 8 DELAYED: Starting SceneAsset reference assignment...");
				
				// Double-check that root scene is still active
				Scene rootForReferenceAssignment = SceneManager.GetSceneByPath(rootPath);
				if (!rootForReferenceAssignment.IsValid() || !rootForReferenceAssignment.isLoaded)
					{
					Debug.LogError("❌ Root scene not available for reference assignment in delayed call!");
					return;
					}

				SceneManager.SetActiveScene(rootForReferenceAssignment);
				Debug.Log($"🔗 Active scene confirmed: {SceneManager.GetActiveScene().name}");

				// Assign references with detailed logging
				int successCount = 0;
				foreach (string subName in SubSceneNames)
					{
					Debug.Log($"🔗 Attempting reference assignment for: {subName}");
					bool success = AssignSubSceneReferenceWithValidation(subName);
					if (success) successCount++;
					}

				// Final save and validation
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				
				Debug.Log($"✅ SubScene reference assignment completed: {successCount}/{SubSceneNames.Length} successful");
				string note = _fallbackTriggeredThisRun ? " (one or more sub‑scenes created via fallback)" : string.Empty;
				Debug.Log("✅ MetVanDAMN baseline scene + " + SubSceneNames.Length + " sub‑scenes created at " + rootPath + note);
				
				if (successCount < SubSceneNames.Length)
					{
					Debug.LogWarning($"⚠️ {SubSceneNames.Length - successCount} SubScene reference assignments failed. Check console for details.");
					}
				
				if (_fallbackTriggeredThisRun)
					{
					Debug.LogWarning("Some sub‑scenes were created using fallback (Single) mode because additive creation was unavailable. Re-run the bootstrap later if you need to refresh links.");
					}

				Debug.Log("   Next: Open the scene and press Play for immediate worldgen smoke validation.");
			};

			// 🔗 CRITICAL: Remove conflicting code - all validation moved to delayCall
			// The delayCall above handles all scene validation and reference assignment
			}

		private static void ReopenRootIfNeeded ()
			{
			if (string.IsNullOrEmpty(_currentRootScenePath))
				{
				return;
				}

			Scene root = SceneManager.GetSceneByPath(_currentRootScenePath);
			if (!root.IsValid() || !root.isLoaded)
				{
				if (File.Exists(_currentRootScenePath))
					{
					System.Threading.Thread.Sleep(500); // @jmeyer1980 ⚠ Intention ⚠TODO: Test if this wait is actually needed
					EditorSceneManager.OpenScene(_currentRootScenePath, OpenSceneMode.Additive);
					}
				}
			// Ensure the root scene remains active (helps SubScene authoring context)
			Scene ensuredRoot = SceneManager.GetSceneByPath(_currentRootScenePath);
			if (ensuredRoot.IsValid())
				{
				System.Threading.Thread.Sleep(500); // @jmeyer1980 ⚠ Intention ⚠ TODO: Test if this wait is actually needed
				SceneManager.SetActiveScene(ensuredRoot);
				}
			}

#if METVD_FULL_DOTS
		private static void CreateBootstrapMono ()
			{
			GameObject go = new("Bootstrap");
			SmokeTestSceneSetup comp = go.AddComponent<SmokeTestSceneSetup>();
			SerializedObject so = new(comp);
			SerializedProperty seedProp = so.FindProperty("worldSeed");
			if (seedProp != null)
				{
				seedProp.uintValue = 42u;
				}

			SerializedProperty worldSizeProp = so.FindProperty("worldSize");
			if (worldSizeProp != null)
				{
				SerializedProperty x = worldSizeProp.FindPropertyRelative("x");
				SerializedProperty y = worldSizeProp.FindPropertyRelative("y");
				if (x != null)
					{
					x.intValue = 50;
					}

				if (y != null)
					{
					y.intValue = 50;
					}
				}
			SerializedProperty sectorsProp = so.FindProperty("targetSectorCount");
			if (sectorsProp != null)
				{
				sectorsProp.intValue = 5;
				}

			SerializedProperty radiusProp = so.FindProperty("biomeTransitionRadius");
			if (radiusProp != null)
				{
				radiusProp.floatValue = 10f;
				}

			so.ApplyModifiedPropertiesWithoutUndo();
			}
#else
        private static void CreateBootstrapMono()
        {
            GameObject go = new("Bootstrap");
            
            // Try to add WorldBootstrapAuthoring if available, fallback to SmokeTestSceneSetup
            Type worldBootstrapType = FindTypeAnywhere("TinyWalnutGames.MetVD.Authoring.WorldBootstrapAuthoring");
            if (worldBootstrapType != null)
            {
                Component bootstrapComp = go.AddComponent(worldBootstrapType);
                try
                {
                    SerializedObject so = new(bootstrapComp);
                    SerializedProperty seedProp = so.FindProperty("seed");
                    if (seedProp != null) seedProp.intValue = 42;
                    SerializedProperty worldSizeProp = so.FindProperty("worldSize");
                    if (worldSizeProp != null)
                    {
                        SerializedProperty x = worldSizeProp.FindPropertyRelative("x");
                        SerializedProperty y = worldSizeProp.FindPropertyRelative("y");
                        if (x != null) x.intValue = 50;
                        if (y != null) y.intValue = 50;
                    }
                    SerializedProperty districtCountProp = so.FindProperty("districtCount");
                    if (districtCountProp != null)
                    {
                        SerializedProperty x = districtCountProp.FindPropertyRelative("x");
                        SerializedProperty y = districtCountProp.FindPropertyRelative("y");
                        if (x != null) x.intValue = 3;
                        if (y != null) y.intValue = 8;
                    }
                    SerializedProperty sectorsPerDistrictProp = so.FindProperty("sectorsPerDistrict");
                    if (sectorsPerDistrictProp != null)
                    {
                        SerializedProperty x = sectorsPerDistrictProp.FindPropertyRelative("x");
                        SerializedProperty y = sectorsPerDistrictProp.FindPropertyRelative("y");
                        if (x != null) x.intValue = 2;
                        if (y != null) y.intValue = 5;
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log("✅ Created WorldBootstrapAuthoring with procedural generation settings");
                }
                catch (Exception e)
                {
                    Debug.LogWarning("WorldBootstrapAuthoring property initialization failed: " + e.Message);
                }
                return;
            }

            Type bootstrapType = FindTypeAnywhere("TinyWalnutGames.MetVD.Samples.SmokeTestSceneSetup");
            if (bootstrapType == null)
            {
                Debug.LogWarning("⚠️ Neither WorldBootstrapAuthoring nor SmokeTestSceneSetup type found. Baseline scene created without runtime bootstrap. (Define METVD_FULL_DOTS for direct mode.)");
                return;
            }
            Component comp = go.AddComponent(bootstrapType);
            try
            {
                SerializedObject so = new(comp);
                SerializedProperty seedProp = so.FindProperty("worldSeed");
                if (seedProp != null) seedProp.uintValue = 42u;
                SerializedProperty worldSizeProp = so.FindProperty("worldSize");
                if (worldSizeProp != null)
                {
                    SerializedProperty x = worldSizeProp.FindPropertyRelative("x");
                    SerializedProperty y = worldSizeProp.FindPropertyRelative("y");
                    if (x != null) x.intValue = 50;
                    if (y != null) y.intValue = 50;
                }
                SerializedProperty sectorsProp = so.FindProperty("targetSectorCount");
                if (sectorsProp != null) sectorsProp.intValue = 5;
                SerializedProperty radiusProp = so.FindProperty("biomeTransitionRadius");
                if (radiusProp != null) radiusProp.floatValue = 10f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            catch (Exception e)
            {
                Debug.LogWarning("SmokeTestSceneSetup property initialization failed: " + e.Message);
            }
        }
#endif

		private static bool SafeEnsureScene (string scenePath, string sceneName)
			{
			Scene existingLoaded = SceneManager.GetSceneByPath(scenePath);
			if (existingLoaded.IsValid() && existingLoaded.isLoaded)
				{
				return true;
				}

			if (File.Exists(scenePath))
				{
				EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
				return true;
				}

			try
				{
				System.Threading.Thread.Sleep(500); // @jmeyer1980 ⚠ Intention ⚠ TODO: Test if this wait is actually needed
				Scene additive = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
				additive.name = sceneName;
				if (!EditorSceneManager.SaveScene(additive, scenePath))
					{
					Debug.LogError("❌ Failed to save sub‑scene " + sceneName + " at " + scenePath);
					return false;
					}

				// 🔥 TIMING FIX v2.0: More aggressive synchronization!
				AssetDatabase.SaveAssets();

				System.Threading.Thread.Sleep(500); // @jmeyer1980 ⚠ Intention ⚠ TODO: Test if this wait is actually needed
				AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

				// 🧙‍♂️ GUID VALIDATION: Wait for both scene AND meta file
				int attempts = 0;
				while (attempts < 20) // Increased attempts
					{
					SceneAsset testAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
					string metaPath = scenePath + ".meta";

					if (testAsset != null && File.Exists(metaPath))
						{
						// 🔥 GUID VERIFICATION: Make sure the asset has a valid GUID
						string guid = AssetDatabase.AssetPathToGUID(scenePath);
						if (!string.IsNullOrEmpty(guid))
							{
							Debug.Log($"🔍 Scene GUID verified: {sceneName} -> {guid}");
							break; // Asset is properly imported with valid GUID!
							}
						}

					System.Threading.Thread.Sleep(500); // Longer wait for GUID generation
					attempts++;
					}

				if (attempts >= 20)
					{
					Debug.LogWarning($"⚠️ GUID validation timeout for scene: {sceneName}. SubScene references may be unstable.");
					}

				return true;
				}
			catch (InvalidOperationException ex)
				{
				_fallbackTriggeredThisRun = true;
				Debug.LogWarning("Additive scene creation fallback engaged for '" + sceneName + "': " + ex.Message);
				Scene temp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
				temp.name = sceneName;
				if (!EditorSceneManager.SaveScene(temp, scenePath))
					{
					Debug.LogError("❌ Fallback save failed for sub‑scene " + sceneName + " at " + scenePath);
					return false;
					}

				// 🔥 TIMING FIX v2.0: Same aggressive synchronization for fallback!
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

				// 🧙‍♂️ GUID VALIDATION for fallback too
				int attempts = 0;
				while (attempts < 20)
					{
					string guid = AssetDatabase.AssetPathToGUID(scenePath);
					if (!string.IsNullOrEmpty(guid))
						{
						Debug.Log($"🔍 Fallback scene GUID verified: {sceneName} -> {guid}");
						break;
						}
					System.Threading.Thread.Sleep(500);
					attempts++;
					}

				// Re-open root baseline additively if it exists
				ReopenRootIfNeeded();
				return true;
				}
			}

#if !METVD_FULL_DOTS
        private static void CloseIfLoaded(string scenePath)
        {
            Scene sc = SceneManager.GetSceneByPath(scenePath);
            if (sc.IsValid() && sc.isLoaded)
            {
                // Do not close root scene
                if (!string.Equals(sc.path, _currentRootScenePath, StringComparison.OrdinalIgnoreCase))
                {
                    EditorSceneManager.CloseScene(sc, true);
                }
            }
        }

        /// <summary>
        /// Creates and links SubScene GameObject with SceneAsset reference.
        /// 🔥 REFLECTION MODE ONLY: Used when DOTS SubScene types need reflection-based access
        /// In METVD_FULL_DOTS mode, use CreateSubSceneGameObject() + AssignSubSceneReference() instead
        /// 🧙‍♂️ Sacred Symbol Preservation: Conditionally compiled but IDE warning suppressors are weak magic
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "Used in reflection mode (!METVD_FULL_DOTS) - conditional compilation confuses IDE analysis")]
        private static void TryCreateAndLinkSubScene(string subName, Transform parent)
        {
            string scenePath = Path.Combine(SubScenesFolder, subName + ".unity").Replace("\\", "/");
            if (!SafeEnsureScene(scenePath, subName)) return;
            if (parent == null || parent.gameObject == null)
            {
                var parentGO = GameObject.Find("_SubScenes");
                if (!parentGO)
                {
                    parentGO = new GameObject("_SubScenes");
                }
                parent = parentGO.transform;
            }
            Type subSceneType = FindTypeAnywhere("Unity.Scenes.SubScene");
            GameObject go = GameObject.Find(subName);
            if (!go)
            {
                go = new GameObject(subName);
            }
            go.transform.SetParent(parent, false);
            if (subSceneType != null)
            {
                Component existing = go.GetComponent(subSceneType);
                if (!existing) existing = go.AddComponent(subSceneType);
                SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                if (sceneAsset != null)
                {
                    try
                    {
                        SerializedObject so = new(existing);
                        SerializedProperty sceneProp = so.FindProperty("m_SceneAsset");
                        if (sceneProp != null) sceneProp.objectReferenceValue = sceneAsset;
                        SerializedProperty autoLoadProp = so.FindProperty("m_AutoLoadScene");
                        if (autoLoadProp != null) autoLoadProp.boolValue = true;
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(existing);
                        AssetDatabase.SaveAssets();
                        Debug.Log($"✅ SubScene '{subName}' successfully linked to {scenePath} (reflection mode)");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("SubScene serialization assignment failed for " + subName + ": " + e.Message);
                    }
                }
                else
                {
                    // 🔥 TIMING DIAGNOSTIC: Better error feedback for timing issues
                    Debug.LogError($"❌ SceneAsset at '{scenePath}' could not be loaded! File exists: {File.Exists(scenePath)}. This suggests an AssetDatabase timing issue.");
                }
            }
            else if (!go.GetComponent<SubSceneMarker>())
            {
                go.AddComponent<SubSceneMarker>();
                Debug.Log($"✅ SubSceneMarker added to '{subName}' (SubScene type not available)");
            }
            CloseIfLoaded(scenePath);
        }
#endif

		private static void CreateBaselineEnvironment ()
			{
			if (UnityEngine.Object.FindFirstObjectByType<Light>() == null)
				{
				GameObject lightGO = new("Directional Light");
				Light light = lightGO.AddComponent<Light>();
				light.type = LightType.Directional;
				light.intensity = 1.2f;
				light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
				}
			if (UnityEngine.Object.FindFirstObjectByType<Camera>() == null)
				{
				GameObject camGO = new("Main Camera") { tag = "MainCamera" };
				Camera cam = camGO.AddComponent<Camera>();
				cam.transform.SetPositionAndRotation(new Vector3(0f, 60f, -60f), Quaternion.Euler(45f, 0f, 0f));
				}
			}

#if !METVD_FULL_DOTS
        private static Type FindTypeAnywhere(string fullName)
        {
            foreach (System.Reflection.Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type t = asm.GetType(fullName, false);
                    if (t != null) return t;
                }
                catch { }
            }
            return null;
        }
#endif

		private static void EnsureFolder (string path)
			{
			if (AssetDatabase.IsValidFolder(path))
				{
				return;
				}

			string [ ] segments = path.Split('/');
			string current = segments [ 0 ];
			for (int i = 1; i < segments.Length; i++)
				{
				string next = current + "/" + segments [ i ];
				if (!AssetDatabase.IsValidFolder(next))
					{
					AssetDatabase.CreateFolder(current, segments [ i ]);
					}
				current = next;
				}
			}

		// 🔥 SPLIT-TASK HELPER METHODS

		/// <summary>
		/// Creates SubScene GameObject with component but NO scene reference assigned yet
		/// </summary>
		private static void CreateSubSceneGameObject (string subName, Transform parent)
			{
#if METVD_FULL_DOTS
			var go = GameObject.Find(subName);
			if (!go)
				{
				go = new GameObject(subName);
				}
			go.transform.SetParent(parent, false);

			SubScene subSceneComp = go.GetComponent<SubScene>();
			if (!subSceneComp)
				{
				subSceneComp = go.AddComponent<SubScene>();
				// DO NOT assign scene reference yet - just create the component
				Debug.Log($"📦 Created SubScene GameObject: {subName} (no reference assigned yet)");
				}
#else
            Type subSceneType = FindTypeAnywhere("Unity.Scenes.SubScene");
            var go = GameObject.Find(subName);
            if (!go)
            {
                go = new GameObject(subName);
            }
            go.transform.SetParent(parent, false);
            
            if (subSceneType != null)
            {
                Component existing = go.GetComponent(subSceneType);
                if (!existing)
                {
                    existing = go.AddComponent(subSceneType);
                    Debug.Log($"📦 Created SubScene GameObject: {subName} (reflection mode, no reference assigned yet)");
                }
            }
            else
            {
                if (!go.GetComponent<SubSceneMarker>())
                {
                    go.AddComponent<SubSceneMarker>();
                    Debug.Log($"📦 Created SubSceneMarker GameObject: {subName}");
                }
            }
#endif
			}

		/// <summary>
		/// Assigns SceneAsset reference to existing SubScene GameObject with comprehensive validation
		/// 🔥 ENHANCED: Returns success status and includes detailed error reporting
		/// </summary>
		private static bool AssignSubSceneReferenceWithValidation (string subName)
			{
			string scenePath = Path.Combine(SubScenesFolder, subName + ".unity").Replace("\\", "/");

			// 🔍 VALIDATION: Check if scene file exists
			if (!System.IO.File.Exists(scenePath))
				{
				Debug.LogError($"❌ Scene file does not exist: {scenePath}");
				return false;
				}

			// 🔍 VALIDATION: Check if GameObject exists
			var go = GameObject.Find(subName);
			if (go == null)
				{
				Debug.LogError($"❌ SubScene GameObject '{subName}' not found! Available GameObjects: {string.Join(", ", UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.InstanceID).Select(obj => obj.name))}");
				return false;
				}

			Debug.Log($"✅ Found GameObject: {subName}");

#if METVD_FULL_DOTS
			// 🔍 VALIDATION: Check if SubScene component exists
			if (!go.TryGetComponent(out SubScene subSceneComp))
				{
				Debug.LogError($"❌ SubScene component not found on '{subName}'! Components: {string.Join(", ", go.GetComponents<Component>().Select(c => c.GetType().Name))}");
				return false;
				}

			Debug.Log($"✅ Found SubScene component on: {subName}");

			// 🔍 VALIDATION: Load SceneAsset and verify GUID
			SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
			if (sceneAsset == null)
				{
				Debug.LogError($"❌ Failed to load SceneAsset from path: {scenePath}");
				return false;
				}

			string guid = AssetDatabase.AssetPathToGUID(scenePath);
			if (string.IsNullOrEmpty(guid))
				{
				Debug.LogError($"❌ Scene asset at '{scenePath}' has no GUID! SubScene will show as Missing.");
				return false;
				}

			Debug.Log($"✅ SceneAsset loaded with GUID: {guid}");

			// 🔗 ASSIGNMENT: Set SceneAsset reference and GUID
			try
				{
				SerializedObject so = new(subSceneComp);

				// 🎯 CRITICAL: Assign the SceneAsset reference
				SerializedProperty sceneProp = so.FindProperty("m_SceneAsset");
				if (sceneProp != null)
					{
					sceneProp.objectReferenceValue = sceneAsset;
					Debug.Log($"✅ Set m_SceneAsset reference for {subName}");
					}
				else
					{
					Debug.LogWarning($"⚠️ m_SceneAsset property not found on {subName}");
					}

				// 🔥 THE MISSING PIECE: Assign the _SceneGUID field!
				SerializedProperty sceneGuidProp = so.FindProperty("_SceneGUID");
				if (sceneGuidProp != null)
					{
					SerializedProperty guidValueProp = sceneGuidProp.FindPropertyRelative("Value");
					if (guidValueProp != null)
						{
						// 🧙‍♂️ GUID CONVERSION MAGIC: Convert string GUID to Unity's 4x uint32 format
						var unityGuid = new System.Guid(guid);
						byte [ ] guidBytes = unityGuid.ToByteArray();

						uint x = System.BitConverter.ToUInt32(guidBytes, 0);
						uint y = System.BitConverter.ToUInt32(guidBytes, 4);
						uint z = System.BitConverter.ToUInt32(guidBytes, 8);
						uint w = System.BitConverter.ToUInt32(guidBytes, 12);

						SerializedProperty xProp = guidValueProp.FindPropertyRelative("x");
						SerializedProperty yProp = guidValueProp.FindPropertyRelative("y");
						SerializedProperty zProp = guidValueProp.FindPropertyRelative("z");
						SerializedProperty wProp = guidValueProp.FindPropertyRelative("w");

						if (xProp != null) xProp.uintValue = x;
						if (yProp != null) yProp.uintValue = y;
						if (zProp != null) zProp.uintValue = z;
						if (wProp != null) wProp.uintValue = w;

						Debug.Log($"✅ Set _SceneGUID for {subName}: {guid} -> ({x:X8}, {y:X8}, {z:X8}, {w:X8})");
						}
					else
						{
						Debug.LogWarning($"⚠️ _SceneGUID.Value property not found on {subName}");
						}
					}
				else
					{
					Debug.LogWarning($"⚠️ _SceneGUID property not found on {subName}");
					}

				// 🔗 ASSIGNMENT: Set auto-load
				SerializedProperty autoLoadProp = so.FindProperty("m_AutoLoadScene");
				if (autoLoadProp != null)
					{
					autoLoadProp.boolValue = true;
					Debug.Log($"✅ Set m_AutoLoadScene = true for {subName}");
					}

				// 💾 SAVE: Apply changes
				bool applied = so.ApplyModifiedPropertiesWithoutUndo();
				if (!applied)
					{
					Debug.LogWarning($"⚠️ Failed to apply SerializedObject changes for {subName}");
					}

				EditorUtility.SetDirty(subSceneComp);
				EditorUtility.SetDirty(go);

				Debug.Log($"✅ SubScene reference assignment completed for '{subName}' -> {scenePath}");
				return true;
				}
			catch (System.Exception ex)
				{
				Debug.LogError($"❌ Exception during SubScene assignment for {subName}: {ex.Message}");
				return false;
				}
#else
			// 🔍 VALIDATION: Reflection mode assignment
			Type subSceneType = FindTypeAnywhere("Unity.Scenes.SubScene");
			if (subSceneType == null)
				{
				Debug.LogWarning($"⚠️ SubScene type not found via reflection - using marker instead for {subName}");
				return true; // Not a failure, just using fallback
				}

			Component existing = go.GetComponent(subSceneType);
			if (existing == null)
				{
				Debug.LogError($"❌ SubScene component not found on '{subName}' in reflection mode");
				return false;
				}

			// Similar assignment logic for reflection mode
			SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
			if (sceneAsset == null)
				{
				Debug.LogError($"❌ Failed to load SceneAsset from path: {scenePath} (reflection mode)");
				return false;
				}

			string guid = AssetDatabase.AssetPathToGUID(scenePath);
			if (string.IsNullOrEmpty(guid))
				{
				Debug.LogError($"❌ Scene asset at '{scenePath}' has no GUID! (reflection mode)");
				return false;
				}

			try
				{
				SerializedObject so = new(existing);
				
				SerializedProperty sceneProp = so.FindProperty("m_SceneAsset");
				if (sceneProp != null) sceneProp.objectReferenceValue = sceneAsset;
				
				// GUID assignment for reflection mode
				SerializedProperty sceneGuidProp = so.FindProperty("_SceneGUID");
				if (sceneGuidProp != null)
					{
					SerializedProperty guidValueProp = sceneGuidProp.FindPropertyRelative("Value");
					if (guidValueProp != null)
						{
						var unityGuid = new System.Guid(guid);
						byte[] guidBytes = unityGuid.ToByteArray();
						
						uint x = System.BitConverter.ToUInt32(guidBytes, 0);
						uint y = System.BitConverter.ToUInt32(guidBytes, 4);
						uint z = System.BitConverter.ToUInt32(guidBytes, 8);
						uint w = System.BitConverter.ToUInt32(guidBytes, 12);
						
						SerializedProperty xProp = guidValueProp.FindPropertyRelative("x");
						SerializedProperty yProp = guidValueProp.FindPropertyRelative("y");
						SerializedProperty zProp = guidValueProp.FindPropertyRelative("z");
						SerializedProperty wProp = guidValueProp.FindPropertyRelative("w");
						
						if (xProp != null) xProp.uintValue = x;
						if (yProp != null) yProp.uintValue = y;
						if (zProp != null) zProp.uintValue = z;
						if (wProp != null) wProp.uintValue = w;
						}
					}
				
				SerializedProperty autoLoadProp = so.FindProperty("m_AutoLoadScene");
				if (autoLoadProp != null) autoLoadProp.boolValue = true;
				
				so.ApplyModifiedProperties();
				EditorUtility.SetDirty(existing);
				
				Debug.Log($"✅ SubScene reference assignment completed (reflection mode): '{subName}' -> {scenePath}");
				return true;
				}
			catch (System.Exception ex)
				{
				Debug.LogError($"❌ Exception during SubScene assignment (reflection mode) for {subName}: {ex.Message}");
				return false;
				}
#endif
			}
		}
	}
#endif
