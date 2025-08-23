#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

#if METVD_FULL_DOTS
// Direct references (enable symbol METVD_FULL_DOTS in asmdef or Player Settings to use strongly-typed path)
using TinyWalnutGames.MetVD.Samples; // SmokeTestSceneSetup
using Unity.Scenes;                  // SubScene
#endif

namespace TinyWalnutGames.MetVD.Authoring.Editor
{
    /// <summary>
    /// One‑click baseline scene + sub‑scene generator for MetVanDAMN.
    /// Produces a reproducible starting environment that exercises core worldgen / biome / gate logic.
    /// Non‑destructive: will prompt before overwriting existing scenes.
    /// Two modes:
    ///   - Direct Mode (define METVD_FULL_DOTS): strong references to SubScene + SmokeTestSceneSetup (compile-time safety)
    ///   - Fallback Mode (no define): reflection lookups so the tool still works without optional packages
    /// </summary>
    public static class MetVanDAMNSceneBootstrap
    {
        private const string RootSceneName = "MetVanDAMN_Baseline";
        private static readonly string[] SubSceneNames =
        {
            "WorldGen_Terrain",
            "WorldGen_Dungeon",
            "NPC_Interactions",
            "UI_HUD"
        };

        private const string ScenesRootFolder = "Assets/Scenes";
        private const string SubScenesFolder = "Assets/Scenes/SubScenes";

#if !METVD_FULL_DOTS
        // Fallback marker when SubScene component not available (hidden from Add Component menu)
        [AddComponentMenu("")]
        private class SubSceneMarker : MonoBehaviour { }
#endif

        [MenuItem("MetVanDAMN/Create Baseline Scene %#m", priority = 10)]
        public static void CreateBaselineScene()
        {
            EnsureFolder(ScenesRootFolder);
            EnsureFolder(SubScenesFolder);

            string rootPath = Path.Combine(ScenesRootFolder, RootSceneName + ".unity").Replace("\\", "/");
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

            Scene rootScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            rootScene.name = RootSceneName;

            CreateBaselineEnvironment();
            CreateBootstrapMono();

            GameObject subScenesRoot = new("_SubScenes");
            foreach (string subName in SubSceneNames)
            {
                TryCreateAndLinkSubScene(subName, subScenesRoot.transform);
            }

            if (!EditorSceneManager.SaveScene(rootScene, rootPath))
            {
                Debug.LogError("❌ Failed to save root scene at " + rootPath);
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("✅ MetVanDAMN baseline scene + " + SubSceneNames.Length + " sub‑scenes created at " + rootPath);
            Debug.Log("   Next: Open the scene and press Play for immediate worldgen smoke validation.");
        }

        // --- Bootstrap (direct vs fallback) ------------------------------------------------------
#if METVD_FULL_DOTS
        private static void CreateBootstrapMono()
        {
            GameObject go = new("Bootstrap");
            SmokeTestSceneSetup comp = go.AddComponent<SmokeTestSceneSetup>();
            // Optional: expose tweakable defaults if serialized fields changed names
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
#else
        private static void CreateBootstrapMono()
        {
            Type bootstrapType = FindTypeAnywhere("TinyWalnutGames.MetVD.Samples.SmokeTestSceneSetup");
            GameObject go = new("Bootstrap");
            if (bootstrapType == null)
            {
                Debug.LogWarning("⚠️ SmokeTestSceneSetup type not found. Baseline scene created without runtime bootstrap. (Define METVD_FULL_DOTS for direct mode.)");
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

        // --- SubScene link (direct vs fallback) ---------------------------------------------------
#if METVD_FULL_DOTS
        private static void TryCreateAndLinkSubScene(string subName, Transform parent)
        {
            string scenePath = Path.Combine(SubScenesFolder, subName + ".unity").Replace("\\", "/");
            Scene existingLoaded = SceneManager.GetSceneByPath(scenePath);
            if (!(existingLoaded.IsValid() && existingLoaded.isLoaded))
            {
                if (!File.Exists(scenePath))
                {
                    Scene additiveScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                    additiveScene.name = subName;
                    if (!EditorSceneManager.SaveScene(additiveScene, scenePath))
                    {
                        Debug.LogError("❌ Failed to save sub‑scene " + subName + " at " + scenePath);
                        return;
                    }
                }
                else
                {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                }
            }

            GameObject go = GameObject.Find(subName);
            if (!go) go = new GameObject(subName);
            go.transform.SetParent(parent, false);

            SubScene subSceneComp = go.GetComponent<SubScene>();
            if (!subSceneComp) subSceneComp = go.AddComponent<SubScene>();
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset != null)
            {
                SerializedObject so = new(subSceneComp);
                SerializedProperty sceneProp = so.FindProperty("m_SceneAsset");
                if (sceneProp != null) sceneProp.objectReferenceValue = sceneAsset;
                SerializedProperty autoLoadProp = so.FindProperty("m_AutoLoadScene");
                if (autoLoadProp != null) autoLoadProp.boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
#else
        private static void TryCreateAndLinkSubScene(string subName, Transform parent)
        {
            string scenePath = Path.Combine(SubScenesFolder, subName + ".unity").Replace("\\", "/");
            Scene existingLoaded = SceneManager.GetSceneByPath(scenePath);
            if (!(existingLoaded.IsValid() && existingLoaded.isLoaded))
            {
                if (!File.Exists(scenePath))
                {
                    Scene additiveScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                    additiveScene.name = subName;
                    if (!EditorSceneManager.SaveScene(additiveScene, scenePath))
                    {
                        Debug.LogError("❌ Failed to save sub‑scene " + subName + " at " + scenePath);
                        return;
                    }
                }
                else
                {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                }
            }

            Type subSceneType = FindTypeAnywhere("Unity.Scenes.SubScene");
            GameObject go = GameObject.Find(subName);
            if (!go) go = new GameObject(subName);
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
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("SubScene serialization assignment failed for " + subName + ": " + e.Message);
                    }
                }
            }
            else if (!go.GetComponent<SubSceneMarker>())
            {
                go.AddComponent<SubSceneMarker>();
            }
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
            if (Camera.main == null)
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
            if (AssetDatabase.IsValidFolder(path)) return;
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
