using System.IO;
using UnityEditor;
using UnityEngine;

namespace TinyWalnutGames.MetVanDAMN.Authoring.Editor
    {
    [CustomEditor(typeof(EcsPrefabRegistryAuthoring))]
    public class EcsPrefabRegistryGizmoGenerator : UnityEditor.Editor
        {
        public override void OnInspectorGUI()
            {
            base.OnInspectorGUI();

            var reg = (EcsPrefabRegistryAuthoring)target;
            if (!reg.EnableGizmoPrefabGeneration) return;

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Gizmo Prefab Generation", EditorStyles.boldLabel);
            if (GUILayout.Button("Generate Gizmo Prefabs For Missing/All Keys"))
                {
                GenerateGizmoPrefabsStatic(reg);
                }
            }

        internal static void GenerateGizmoPrefabsStatic(EcsPrefabRegistryAuthoring reg)
            {
            if (reg.Entries == null || reg.Entries.Count == 0)
                {
                EditorUtility.DisplayDialog("ECS Prefab Registry", "No entries to process.", "OK");
                return;
                }

            Directory.CreateDirectory(reg.GeneratedPrefabsFolder);
            for (int i = 0; i < reg.Entries.Count; i++)
                {
                var entry = reg.Entries[i];
                if (!reg.OverwriteExistingWithGizmos && entry.Prefab != null) continue;
                if (string.IsNullOrWhiteSpace(entry.Key)) continue;

                // Create a temporary GO with marker
                var go = new GameObject($"Gizmo_{Sanitize(entry.Key)}");
                var marker = go.AddComponent<GizmoPrefabMarker>();
                marker.Key = entry.Key;
                marker.Shape = reg.GeneratedGizmoShape;
                marker.Size = reg.GeneratedGizmoSize;
                marker.Color = reg.GeneratedGizmoColor;

                var path = Path.Combine(reg.GeneratedPrefabsFolder, $"Gizmo_{Sanitize(entry.Key)}.prefab");
                path = path.Replace('\\', '/');
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
                GameObject.DestroyImmediate(go);

                entry.Prefab = prefab;
                reg.Entries[i] = entry;
                }

            EditorUtility.SetDirty(reg);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            }

        private static string Sanitize(string key)
            {
            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
                key = key.Replace(c, '_');
            return key;
            }
        }
    }
