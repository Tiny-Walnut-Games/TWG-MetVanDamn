#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_2021_3_OR_NEWER
using UnityEditor.Experimental.SceneManagement;
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
        private static readonly string[] SubSceneNames = { "WorldGen_Terrain", "WorldGen_Dungeon", "NPC_Interactions", "UI_HUD" };
        private const string ScenesRootFolder = "Assets/Scenes";
        private const string SubScenesFolder = "Assets/Scenes/SubScenes";
        private static bool _fallbackTriggeredThisRun = false; // track if fallback unloaded baseline
        private static string _currentRootScenePath;           // track path for reopening after fallback

#if !METVD_FULL_DOTS
        [AddComponentMenu("")]
        private class SubSceneMarker : MonoBehaviour { }
#endif

        [MenuItem("MetVanDAMN/Create Baseline Scene %#m", priority = 10)]
        public static void CreateBaselineScene()
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
                EditorSceneManager.CloseScene(EditorSceneManager.GetActiveScene(), true);
            }
#endif

            // Create and SAVE baseline root first so we can reopen it after any fallback sub-scene creation that switches to Single mode.
            Scene rootScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            rootScene.name = RootSceneName;
            CreateBaselineEnvironment();
            CreateBootstrapMono();
            if (!EditorSceneManager.SaveScene(rootScene, rootPath))
            {
                Debug.LogError("❌ Failed initial save of root scene at " + rootPath);
                return;
            }

            // Now create sub‑scenes; root path can be reopened after each fallback
            foreach (string subName in SubSceneNames)
            {
                // Ensure root scene is loaded additively before each attempt (in case prior fallback replaced it)
                ReopenRootIfNeeded();
                var parentGO = GameObject.Find("_SubScenes");
                if (!parentGO)
                {
                    parentGO = new GameObject("_SubScenes");
                }

                TryCreateAndLinkSubScene(subName, parentGO.transform);
            }

            // Final ensure root open & saved
            ReopenRootIfNeeded();
            Scene finalRoot = SceneManager.GetSceneByPath(rootPath);
            if (finalRoot.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(finalRoot);
                EditorSceneManager.SaveScene(finalRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            string note = _fallbackTriggeredThisRun ? " (one or more sub‑scenes created via fallback)" : string.Empty;
            Debug.Log("✅ MetVanDAMN baseline scene + " + SubSceneNames.Length + " sub‑scenes created at " + rootPath + note);
            if (_fallbackTriggeredThisRun)
            {
                Debug.LogWarning("Some sub‑scenes were created using fallback (Single) mode because additive creation was unavailable. Re-run the bootstrap later if you need to refresh links.");
            }

            Debug.Log("   Next: Open the scene and press Play for immediate worldgen smoke validation.");
        }

        private static void ReopenRootIfNeeded()
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
                    EditorSceneManager.OpenScene(_currentRootScenePath, OpenSceneMode.Additive);
                }
            }
            // Ensure the root scene remains active (helps SubScene authoring context)
            Scene ensuredRoot = SceneManager.GetSceneByPath(_currentRootScenePath);
            if (ensuredRoot.IsValid())
            {
                SceneManager.SetActiveScene(ensuredRoot);
            }
        }

#if METVD_FULL_DOTS
        private static void CreateBootstrapMono()
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

        private static bool SafeEnsureScene(string scenePath, string sceneName)
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
                Scene additive = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                additive.name = sceneName;
                if (!EditorSceneManager.SaveScene(additive, scenePath))
                {
                    Debug.LogError("❌ Failed to save sub‑scene " + sceneName + " at " + scenePath);
                    return false;
                }
                
                // 🔥 TIMING FIX: Force immediate asset database synchronization!
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                
                // 🧙‍♂️ Sacred Symbol Preservation: Wait for the asset to be actually importable
                // This prevents the classic "SceneAsset is null even though file exists" timing issue
                int attempts = 0;
                while (attempts < 10)
                {
                    SceneAsset testAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                    if (testAsset != null)
                    {
                        break; // Asset is ready!
                    }
                    System.Threading.Thread.Sleep(50); // Give Unity a moment to process
                    attempts++;
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
                
                // 🔥 TIMING FIX: Same synchronization for fallback path!
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                
                // Re-open root baseline additively if it exists
                ReopenRootIfNeeded();
                return true;
            }
        }

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

#if METVD_FULL_DOTS
        private static void TryCreateAndLinkSubScene(string subName, Transform parent)
        {
            string scenePath = Path.Combine(SubScenesFolder, subName + ".unity").Replace("\\", "/");
            if (!SafeEnsureScene(scenePath, subName))
            {
                return;
            }

            if (parent == null || parent.gameObject == null)
            {
                var parentGO = GameObject.Find("_SubScenes");
                if (!parentGO)
                {
                    parentGO = new GameObject("_SubScenes");
                }
                parent = parentGO.transform;
            }
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
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset != null)
            {
                SerializedObject so = new(subSceneComp);
                SerializedProperty sceneProp = so.FindProperty("m_SceneAsset");
                if (sceneProp != null)
                {
                    sceneProp.objectReferenceValue = sceneAsset;
                }

                SerializedProperty autoLoadProp = so.FindProperty("m_AutoLoadScene");
                if (autoLoadProp != null)
                {
                    autoLoadProp.boolValue = true;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"✅ SubScene '{subName}' successfully linked to {scenePath}");
            }
            else
            {
                // 🔥 TIMING DIAGNOSTIC: Better error feedback for timing issues
                Debug.LogError($"❌ SceneAsset at '{scenePath}' could not be loaded! File exists: {File.Exists(scenePath)}. This suggests an AssetDatabase timing issue.");
            }
            
            // Close sub-scene if currently loaded so it becomes a proper SubScene reference only
            CloseIfLoaded(scenePath);
        }
#else
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

        private static void CreateBaselineEnvironment()
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

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string[] segments = path.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }
                current = next;
            }
        }
    }
}
#endif
