using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TinyWalnutGames.MetVanDAMN.Authoring.Editor
    {
    public static class CreateSampleSceneMenu
        {
        [MenuItem("MetVanDAMN/Create Sample Scene With ECS Registry")]
        public static void CreateSampleScene()
            {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects);
            var go = new GameObject("ECS Prefab Registry");
            var reg = go.AddComponent<EcsPrefabRegistryAuthoring>();

            // Pre-populate with representative keys; Prefab references left null for manual assignment.
            reg.Entries = new List<EcsPrefabRegistryAuthoring.Entry>
            {
                new() { Key = "spawn_boss", Prefab = null },
                new() { Key = "spawn_enemy_melee", Prefab = null },
                new() { Key = "spawn_enemy_ranged", Prefab = null },
                new() { Key = "pickup_health", Prefab = null },
                new() { Key = "pickup_coin", Prefab = null },
                new() { Key = "pickup_weapon", Prefab = null },
                new() { Key = "spawn_door_locked", Prefab = null },
                new() { Key = "spawn_door_timed", Prefab = null },
                new() { Key = "spawn_portal_biome", Prefab = null },
                new() { Key = "setpiece_crashed_ship", Prefab = null },
            };

            var path = "Assets/Scenes/MetVanDAMN_Baseline.unity";
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("MetVanDAMN", "Sample scene created at Assets/Scenes/MetVanDAMN_Baseline.unity. Assign prefabs to the ECS Prefab Registry entries.", "OK");
            }
        }
    }
