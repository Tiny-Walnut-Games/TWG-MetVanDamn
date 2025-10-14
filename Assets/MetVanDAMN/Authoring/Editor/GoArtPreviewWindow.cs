#nullable enable
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TinyWalnutGames.MetVanDAMN.Authoring.Editor
{
    /// <summary>
    /// ?? INTENDED EXPANSION ZONE - GameObject Art Preview
    /// Provides a GameObject-oriented art preview and quick access to GO demo scene creators.
    /// This complements the ECS-first pipeline by letting artists and designers validate visuals rapidly.
    /// </summary>
    public sealed class GoArtPreviewWindow : EditorWindow
    {
        private const int GridCols = 8;
        private const int GridRows = 4;
        private const float CellSize = 1.1f;
        private const string RootName = "__GOArtPreview_Root__";

        [SerializeField] private uint previewSeed = 42;
        [SerializeField] private bool autoRefresh = true;
        [SerializeField] private Vector2 scroll;

        // Swatches representative of biome palette expectations
        private static readonly (string name, Color color)[] BiomeSwatches =
        {
            ("Sun", new Color(1.0f, 0.9f, 0.2f)),
            ("Moon", new Color(0.7f, 0.7f, 1f)),
            ("Heat", new Color(0.9f, 0.2f, 0.2f)),
            ("Cold", new Color(0.2f, 0.5f, 1.0f)),
            ("Earth", new Color(0.6f, 0.5f, 0.3f)),
            ("Wind", new Color(0.5f, 0.9f, 0.8f)),
            ("Life", new Color(0.3f, 0.9f, 0.4f)),
            ("Tech", new Color(0.6f, 0.9f, 1.0f))
        };

        [MenuItem("Tiny Walnut Games/MetVanDAMN!/GameObject Workflow/GO Art Preview", priority = 5)]
        public static void ShowWindow()
        {
            var win = GetWindow<GoArtPreviewWindow>("GO Art Preview");
            win.minSize = new Vector2(520, 380);
            win.Show();
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("🎨 GameObject Art Preview", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Validate visuals, colors, and prefab composition without entering Play mode.", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);
            uint newSeed = (uint)EditorGUILayout.IntField("Seed", (int)previewSeed);
            if (newSeed != previewSeed)
            {
                previewSeed = newSeed;
                if (autoRefresh) SpawnGridPreview();
            }

            autoRefresh = EditorGUILayout.Toggle("Auto Refresh", autoRefresh);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 Refresh")) SpawnGridPreview();
            if (GUILayout.Button("🧹 Clear")) ClearPreviewRoot();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            DrawBiomeSwatches();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene Shortcuts (GameObject-Oriented Demos)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🎮 Create 2D Platformer GO Demo"))
            {
                CompleteDemoSceneGenerator.CreateComplete2DPlatformerDemo();
                FocusOnScene();
            }
            if (GUILayout.Button("🧭 Create Top-Down GO Demo"))
            {
                CompleteDemoSceneGenerator.CreateCompleteTopDownDemo();
                FocusOnScene();
            }
            if (GUILayout.Button("🧱 Create 3D GO Demo"))
            {
                CompleteDemoSceneGenerator.CreateComplete3DDemo();
                FocusOnScene();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview Utilities", EditorStyles.boldLabel);
            if (GUILayout.Button("📦 Spawn Sample Prop Grid")) SpawnGridPreview();
            if (GUILayout.Button("♻ Rebuild Grid (Deterministic)")) SpawnGridPreview();

            EditorGUILayout.EndScrollView();
        }

        private static void FocusOnScene()
        {
            // Ensure the Scene view is visible after scene creation
            EditorApplication.delayCall += () =>
            {
                var sceneView = SceneView.lastActiveSceneView;
                if (sceneView != null)
                {
                    sceneView.Focus();
                    sceneView.Repaint();
                }
            };
        }

        private static void ClearPreviewRoot()
        {
            var root = GameObject.Find(RootName);
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        private void SpawnGridPreview()
        {
            ClearPreviewRoot();
            var root = new GameObject(RootName);

            // Use deterministic color sequence from seed
            var rand = new Unity.Mathematics.Random(previewSeed == 0 ? 1u : previewSeed);
            int count = GridCols * GridRows;
            for (int i = 0; i < count; i++)
            {
                // Alternate primitive types for variety
                PrimitiveType type = (i % 3) switch
                {
                    0 => PrimitiveType.Cube,
                    1 => PrimitiveType.Sphere,
                    _ => PrimitiveType.Capsule
                };

                GameObject go = GameObject.CreatePrimitive(type);
                go.name = $"Preview_{i:00}";
                go.transform.SetParent(root.transform);
                int col = i % GridCols;
                int row = i / GridCols;
                go.transform.position = new Vector3(col * CellSize, 0f, row * CellSize);

                var rend = go.GetComponent<Renderer>();
                if (rend == null)
                {
                    rend = go.AddComponent<MeshRenderer>();
                }

                // Pick biome-tinted color with slight deterministic variation
                var swatch = BiomeSwatches[i % BiomeSwatches.Length];
                float h, s, v;
                Color.RGBToHSV(swatch.color, out h, out s, out v);
                float dv = rand.NextFloat(-0.08f, 0.08f);
                float ds = rand.NextFloat(-0.05f, 0.05f);
                Color final = Color.HSVToRGB(Mathf.Repeat(h + ds * 0.15f, 1f), Mathf.Clamp01(s + ds), Mathf.Clamp01(v + dv));
                rend.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                rend.sharedMaterial.color = final;
            }

            // Frame the preview
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.FrameSelected();
            }
        }

        private static void DrawBiomeSwatches()
        {
            EditorGUILayout.LabelField("Biome Swatches", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (var (name, color) in BiomeSwatches)
            {
                DrawColorBox(name, color);
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawColorBox(string label, Color color)
        {
            var rect = GUILayoutUtility.GetRect(80, 40);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, rect.height - 16), color);
            var prev = GUI.color;
            GUI.color = Color.black;
            GUI.Label(new Rect(rect.x + 6, rect.y + rect.height - 16, rect.width - 12, 16), label, EditorStyles.miniLabel);
            GUI.color = prev;
        }
    }
}
