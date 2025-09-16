using System.IO;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Samples;
using TinyWalnutGames.MetVD.Authoring;
using TinyWalnutGames.MetVanDAMN.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TinyWalnutGames.MetVanDAMN.Authoring.Editor
    {
    /// <summary>
    /// Customer-facing creators for fully ready demo scenes. Each scene includes:
    /// - WorldAuthoring and BiomeArtProfileLibraryAuthoring placeholders (if available)
    /// - ECS Prefab Registry with representative keys for quick hookup
    /// - SmokeTestSceneSetup to guarantee Hit Play -> See Map experience
    /// - Variant-specific camera and grid helpers
    /// </summary>
    public static class ReadyDemoSceneMenu
        {
        private const string ScenesFolder = "Assets/Scenes";

        // Menu items moved under Quick Start; keep public methods for reuse
        public static void Create2DPlatformerScene() => CreateReadyScene("MetVanDAMN_2DPlatformer", SceneProjection.Platformer2D);

        public static void CreateTopDownScene() => CreateReadyScene("MetVanDAMN_TopDown", SceneProjection.TopDown2D);

        public static void Create3DScene() => CreateReadyScene("MetVanDAMN_3D", SceneProjection.ThreeD);

        private static void CreateReadyScene(string sceneName, SceneProjection projection)
            {
            EnsureFolder(ScenesFolder);

            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            newScene.name = sceneName;

            // 1) ECS Prefab Registry with useful keys
            var registryGO = new GameObject("ECS Prefab Registry");
            var reg = registryGO.AddComponent<EcsPrefabRegistryAuthoring>();
            reg.Entries = new System.Collections.Generic.List<EcsPrefabRegistryAuthoring.Entry>
            {
                new() { Key = "spawn_boss" },
                new() { Key = "spawn_enemy_melee" },
                new() { Key = "spawn_enemy_ranged" },
                new() { Key = "pickup_health" },
                new() { Key = "pickup_coin" },
                new() { Key = "pickup_weapon" },
                new() { Key = "spawn_door_locked" },
                new() { Key = "spawn_door_timed" },
                new() { Key = "spawn_portal_biome" },
                new() { Key = "setpiece_crashed_ship" },
            };

            // Sensible defaults for gizmo generation without touching real prefabs
            reg.EnableGizmoPrefabGeneration = true;
            reg.GeneratedGizmoShape = EcsPrefabRegistryAuthoring.GizmoShape.Cube;
            reg.GeneratedGizmoSize = 1.0f;
            reg.GeneratedGizmoColor = new Color(0.2f, 0.9f, 0.3f, 0.9f);
            reg.GeneratedPrefabsFolder = "Assets/MetVanDAMN/Debug/GizmoPrefabs";
            reg.OverwriteExistingWithGizmos = false;

            // 2) World + Biome art authoring placeholders if available
            // World defaults via authoring component
            var worldGO = new GameObject("WorldAuthoring");
            var world = worldGO.AddComponent<WorldAuthoring>();
            world.worldSeed = 42;
            world.worldSize = new Vector3(50f, 50f, 0f);
            world.targetSectorCount = 5;
            world.biomeTransitionRadius = 10f;
            world.enableDebugVisualization = true;
            world.logGenerationSteps = true;
            TryAddComponent<BiomeArtProfileLibraryAuthoring>(new GameObject("BiomeArtProfileLibrary"));

            // 3) Smoke test bootstrap to guarantee playable world
            var bootstrapGO = new GameObject("SmokeTestSceneSetup");
            var setup = bootstrapGO.AddComponent<SmokeTestSceneSetup>();
            // Configure minimal overrides via SerializedObject (private [SerializeField] fields)
            var so = new SerializedObject(setup);
            var seedProp = so.FindProperty("worldSeed");
            if (seedProp != null) seedProp.uintValue = 42;
            var sizeProp = so.FindProperty("worldSize");
            if (sizeProp != null) sizeProp.FindPropertyRelative("x").intValue = 50;
            if (sizeProp != null) sizeProp.FindPropertyRelative("y").intValue = 50;
            var sectorProp = so.FindProperty("targetSectorCount");
            if (sectorProp != null) sectorProp.intValue = 5;
            var radiusProp = so.FindProperty("biomeTransitionRadius");
            if (radiusProp != null) radiusProp.floatValue = 10f;
            var debugProp = so.FindProperty("enableDebugVisualization");
            if (debugProp != null) debugProp.boolValue = true;
            var logProp = so.FindProperty("logGenerationSteps");
            if (logProp != null) logProp.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 4) Projection-specific camera helpers
            SetupCameraForProjection(projection);

            // Save
            string path = Path.Combine(ScenesFolder, sceneName + ".unity").Replace('\\', '/');
            EditorSceneManager.SaveScene(newScene, path);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("MetVanDAMN!", $"Ready scene created at {path}. Assign your prefabs in 'ECS Prefab Registry' and Press Play.", "OK");
            }

        private static void SetupCameraForProjection(SceneProjection projection)
            {
            var cam = Object.FindFirstObjectByType<Camera>();
            if (!cam)
                {
                var camGO = new GameObject("Main Camera");
                cam = camGO.AddComponent<Camera>();
                cam.tag = "MainCamera";
                }

            switch (projection)
                {
                case SceneProjection.Platformer2D:
                case SceneProjection.TopDown2D:
                    cam.orthographic = true;
                    cam.orthographicSize = projection == SceneProjection.Platformer2D ? 8 : 12;
                    cam.transform.position = projection == SceneProjection.TopDown2D
                        ? new Vector3(0, 20, -10)
                        : new Vector3(0, 8, -10);
                    cam.transform.rotation = projection == SceneProjection.TopDown2D
                        ? Quaternion.Euler(60, 0, 0)
                        : Quaternion.identity;
                    break;
                case SceneProjection.ThreeD:
                    cam.orthographic = false;
                    cam.transform.position = new Vector3(0, 12, -18);
                    cam.transform.LookAt(Vector3.zero);
                    break;
                }
            }

        private static void EnsureFolder(string path)
            {
            if (!AssetDatabase.IsValidFolder(path))
                {
                var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
                var leaf = System.IO.Path.GetFileName(path);
                if (!AssetDatabase.IsValidFolder(parent))
                    {
                    AssetDatabase.CreateFolder("Assets", "Scenes");
                    }
                AssetDatabase.CreateFolder(parent, leaf);
                }
            }

        private static void TryAddComponent<T>(GameObject go) where T : Component
            {
            if (go == null) return;
            go.AddComponent<T>();
            }

        private enum SceneProjection
            {
            Platformer2D,
            TopDown2D,
            ThreeD
            }
        }
    }
