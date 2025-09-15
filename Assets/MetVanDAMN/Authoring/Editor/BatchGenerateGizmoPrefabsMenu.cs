using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TinyWalnutGames.MetVanDAMN.Authoring.Editor
    {
    public static class BatchGenerateGizmoPrefabsMenu
        {
        [MenuItem("MetVanDAMN/Gizmos/Generate Gizmo Prefabs For All Registries In Scene")]
        public static void GenerateAll()
            {
            var registries = Object.FindObjectsByType<EcsPrefabRegistryAuthoring>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (registries == null || registries.Length == 0)
                {
                EditorUtility.DisplayDialog("ECS Prefab Registry", "No EcsPrefabRegistryAuthoring components found in the scene.", "OK");
                return;
                }

            int processed = 0;
            foreach (var reg in registries)
                {
                if (!reg.EnableGizmoPrefabGeneration) continue;
                // Reuse the inspector logic via a hidden method would be ideal, but here we just call the generator directly
                EcsPrefabRegistryGizmoGeneratorUtil.Generate(reg);
                processed++;
                }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("ECS Prefab Registry", $"Processed {processed} registries.", "OK");
            }
        }

    // Small helper so inspector and menu share generation without reflection
    internal static class EcsPrefabRegistryGizmoGeneratorUtil
        {
        public static void Generate(EcsPrefabRegistryAuthoring reg)
            {
            EcsPrefabRegistryGizmoGenerator.GenerateGizmoPrefabsStatic(reg);
            }
        }
    }
