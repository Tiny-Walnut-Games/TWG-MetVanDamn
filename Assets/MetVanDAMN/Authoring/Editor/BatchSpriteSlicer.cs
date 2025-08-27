//  This script is intended to be used in the Unity Editor only, stored in an Editor folder to ensure it is not included in builds.
#if UNITY_EDITOR

// Check if sprite editor assemblies are available
#if HAS_SPRITE_EDITOR || UNITY_2D_SPRITE_EDITOR_AVAILABLE
// This comment is a visual indicator that the sprite editor features are available
// If grey, no. If commenting color, yes.
#define SPRITE_EDITOR_FEATURES_AVAILABLE
#endif

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

#if SPRITE_EDITOR_FEATURES_AVAILABLE
using UnityEditor.U2D.Sprites; // this script requires com.unity.2d.sprite package be imported to work.
#endif

namespace TinyWalnutGames.MetVD.Utility.Editor
{
    /// <summary>
    /// Batch Sprite Slicer
    /// Use this tool to slice multiple sprites from selected textures in the project.
    /// I personally like to run a search for all textures in the project
    /// and then slice the ones I want to slice. I work in smallish batches of twenty or so as to not overwhelm the editor.
    /// I wanted to thread the tasks, but I am understanding that the Unity API is not thread-safe,
    /// so I had to use the ISpriteEditorDataProvider interface instead.
    /// see https://docs.unity3d.com/Packages/com.unity.2d.sprite@latest/manual/SpriteEditorDataProvider.html
    /// </summary>
    public class BatchSpriteSlicer : EditorWindow
    {
        private bool useCellSize = false;               // Choose between cell size or rows/columns
        private Vector2 cellSize = new(64, 64);          // Cell size when useCellSize is true
        private int columns = 12;                        // Grid columns when useCellSize is false
        private int rows = 8;                            // Grid rows when useCellSize is false
        private SpriteAlignment pivotAlignment = SpriteAlignment.BottomCenter; // Pivot alignment
        private bool ignoreEmptyRects = true;            // Skip fully transparent cells

#if SPRITE_EDITOR_FEATURES_AVAILABLE
        private static List<SpriteRect> copiedRects = null;                             // Copied rects
        private static readonly Dictionary<string, List<Vector2[]>> copiedOutlines = new(); // Copied outlines keyed by original rect GUID
#else
        private static bool spriteEditorNotAvailable = true; // Silence unused warnings when features unavailable
#endif
        private static int copiedTexWidth = 0;  // Source texture width for copy layout scaling
        private static int copiedTexHeight = 0; // Source texture height for copy layout scaling

        [MenuItem("Tools/Batch Sprite Slicer")]
        public static void OpenWindow() => GetWindow<BatchSpriteSlicer>("Batch Sprite Slicer");

        private void OnGUI()
        {
#if !SPRITE_EDITOR_FEATURES_AVAILABLE
            GUILayout.Label("Batch Sprite Slicer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Sprite Editor features are not available. This tool requires Unity's 2D Sprite Editor package " +
                "or sprite editor assemblies to be available. Please install com.unity.2d.sprite package to enable functionality.",
                MessageType.Warning);
            if (GUILayout.Button("Open Package Manager"))
            {
                UnityEditor.PackageManager.UI.Window.Open("com.unity.2d.sprite");
            }
            return;
#endif
            GUILayout.Label("Batch Sprite Slicer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Slice multiple sprites from selected textures. Copy/paste slice layouts, adjust pivots, or grid-slice textures in batches.",
                MessageType.Info);

            useCellSize = EditorGUILayout.Toggle("Use Cell Size", useCellSize);
            if (useCellSize)
            {
                cellSize = EditorGUILayout.Vector2Field("Cell Size", cellSize);
                EditorGUILayout.HelpBox("Specify width/height for each cell; grid size inferred from texture dimensions.", MessageType.None);
            }
            else
            {
                GUILayout.Label("Grid Settings", EditorStyles.boldLabel);
                columns = EditorGUILayout.IntField("Columns", columns);
                rows = EditorGUILayout.IntField("Rows", rows);
            }

            pivotAlignment = (SpriteAlignment)EditorGUILayout.EnumPopup("Pivot Alignment", pivotAlignment);
            ignoreEmptyRects = EditorGUILayout.Toggle("Ignore Empty Rects", ignoreEmptyRects);

            EditorGUILayout.Space();
            GUILayout.Label("Slice Layout Operations", EditorStyles.boldLabel);
            if (GUILayout.Button(new GUIContent("Copy Rect Layout", "Copy slice rectangles (and outlines) from first selected texture.")))
                CopySlicesFromSelected();
            using (new EditorGUI.DisabledScope(copiedRects == null))
            {
                if (GUILayout.Button(new GUIContent("Paste Rect Layout", "Paste copied slice layout (scaled) to all selected textures.")))
                {
                    if (Selection.objects.Length == 0)
                    {
                        Debug.LogWarning("No textures selected. Select textures to paste slices.");
                        return;
                    }
                    PasteSlicesToSelected();
                }
            }

            EditorGUILayout.Space();
            GUILayout.Label("Pivot Adjustment", EditorStyles.boldLabel);
            if (GUILayout.Button(new GUIContent("Adjust Pivot Of Selected Slices", "Overwrite pivots for all slices on selected textures.")))
            {
                if (Selection.objects.Length == 0)
                {
                    Debug.LogWarning("No textures selected. Select textures to adjust pivots.");
                }
                else
                {
                    AdjustPivotOfSelectedSlices();
                }
            }

            EditorGUILayout.Space();
            GUILayout.Label("Batch Slicing", EditorStyles.boldLabel);
            if (GUILayout.Button(new GUIContent("Slice Selected Sprites (Grid)", "Slice selected textures using current grid settings.")))
            {
                if (Selection.objects.Length == 0)
                {
                    Debug.LogWarning("No textures selected. Select textures to slice.");
                }
                else
                {
                    SliceSelectedSprites();
                }
            }
        }

#if SPRITE_EDITOR_FEATURES_AVAILABLE
        private void CopySlicesFromSelected()
        {
            Object[] selectedTextures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
            if (selectedTextures.Length == 0)
            {
                Debug.LogWarning("No texture selected to copy slices from.");
                return;
            }
            string path = AssetDatabase.GetAssetPath(selectedTextures[0]);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) { Debug.LogWarning("Texture importer not found."); return; }
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (!texture) { Debug.LogWarning("Could not load texture asset."); return; }

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();
            var rects = new List<SpriteRect>(dataProvider.GetSpriteRects());
            if (rects.Count == 0) { Debug.LogWarning("No custom slices found on selected texture."); return; }

            copiedOutlines.Clear();
            var outlineProvider = dataProvider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
            foreach (var rect in rects)
            {
                var outlines = outlineProvider.GetOutlines(rect.spriteID);
                copiedOutlines[rect.spriteID.ToString()] = outlines != null ? outlines.Select(o => o.ToArray()).ToList() : new List<Vector2[]>();
            }
            copiedRects = rects;
            copiedTexWidth = texture.width;
            copiedTexHeight = texture.height;
            Debug.Log($"Copied {rects.Count} sprite rects (and outlines) from '{texture.name}' ({copiedTexWidth}x{copiedTexHeight}).");
        }

        private void PasteSlicesToSelected()
        {
            if (copiedRects == null || copiedRects.Count == 0) { Debug.LogWarning("No copied slices to paste."); return; }
            Object[] selectedTextures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
            foreach (Object obj in selectedTextures)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) { Debug.LogWarning($"Skipping '{obj.name}', texture importer not found."); continue; }
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (!texture) { Debug.LogWarning($"Skipping '{obj.name}', could not load texture asset."); continue; }
                int texWidth = texture.width; int texHeight = texture.height;

                var factory = new SpriteDataProviderFactories(); factory.Init();
                var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer); dataProvider.InitSpriteEditorDataProvider();

                List<SpriteRect> newRects = new();
                float scaleX = (float)texWidth / copiedTexWidth;
                float scaleY = (float)texHeight / copiedTexHeight;
                Dictionary<string, SpriteRect> guidToNewRect = new();
                foreach (var srcRect in copiedRects)
                {
                    var r = srcRect.rect;
                    var scaledRect = new Rect(
                        Mathf.RoundToInt(r.x * scaleX),
                        Mathf.RoundToInt(r.y * scaleY),
                        Mathf.RoundToInt(r.width * scaleX),
                        Mathf.RoundToInt(r.height * scaleY));
                    if (scaledRect.width <= 0 || scaledRect.height <= 0) continue;
                    SpriteRect newRect = new() { name = srcRect.name, rect = scaledRect, alignment = srcRect.alignment, pivot = srcRect.pivot };
                    newRects.Add(newRect); guidToNewRect[srcRect.spriteID.ToString()] = newRect;
                }

                dataProvider.SetSpriteRects(System.Array.Empty<SpriteRect>()); dataProvider.Apply(); // clear
                dataProvider.SetSpriteRects(newRects.ToArray()); dataProvider.Apply();

                var outlineProvider = dataProvider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
                foreach (var srcRect in copiedRects)
                {
                    if (!guidToNewRect.TryGetValue(srcRect.spriteID.ToString(), out var newRect)) continue;
                    if (copiedOutlines.TryGetValue(srcRect.spriteID.ToString(), out var outlines) && outlines != null && outlines.Count > 0)
                    {
                        var srcR = srcRect.rect; var dstR = newRect.rect;
                        float outlineScaleX = dstR.width / srcR.width; float outlineScaleY = dstR.height / srcR.height;
                        List<Vector2[]> scaled = new();
                        foreach (var outline in outlines)
                        {
                            Vector2[] arr = new Vector2[outline.Length];
                            for (int i = 0; i < outline.Length; i++) arr[i] = new Vector2(outline[i].x * outlineScaleX, outline[i].y * outlineScaleY);
                            scaled.Add(arr);
                        }
                        outlineProvider.SetOutlines(newRect.spriteID, scaled);
                    }
                }
                dataProvider.Apply();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                Debug.Log($"Pasted {newRects.Count} slices (and outlines) to '{texture.name}' ({texWidth}x{texHeight}).");
            }
        }

        private void AdjustPivotOfSelectedSlices()
        {
            Object[] selectedTextures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
            foreach (Object obj in selectedTextures)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) { Debug.LogWarning($"Skipping '{obj.name}', texture importer not found."); continue; }
                var factory = new SpriteDataProviderFactories(); factory.Init();
                var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer); dataProvider.InitSpriteEditorDataProvider();
                var rects = new List<SpriteRect>(dataProvider.GetSpriteRects());
                if (rects.Count == 0) { Debug.LogWarning($"No slices found on '{obj.name}'."); continue; }
                for (int i = 0; i < rects.Count; i++) { rects[i].alignment = pivotAlignment; rects[i].pivot = GetPivotForAlignment(pivotAlignment); }
                dataProvider.SetSpriteRects(rects.ToArray()); dataProvider.Apply();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                Debug.Log($"Adjusted pivot for {rects.Count} slices on '{obj.name}'.");
            }
        }

        private void SliceSelectedSprites()
        {
            Object[] selectedTextures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
            foreach (Object obj in selectedTextures)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) { Debug.LogWarning($"Skipping '{obj.name}', texture importer not found."); continue; }
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (!texture) { Debug.LogWarning($"Skipping '{obj.name}', could not load texture asset."); continue; }
                int texWidth = texture.width; int texHeight = texture.height;
                int actualColumns, actualRows, spriteWidth, spriteHeight;
                if (useCellSize)
                {
                    spriteWidth = Mathf.Max(1, Mathf.RoundToInt(cellSize.x));
                    spriteHeight = Mathf.Max(1, Mathf.RoundToInt(cellSize.y));
                    actualColumns = Mathf.Max(1, texWidth / spriteWidth);
                    actualRows = Mathf.Max(1, texHeight / spriteHeight);
                }
                else
                {
                    actualColumns = Mathf.Max(1, columns);
                    actualRows = Mathf.Max(1, rows);
                    spriteWidth = texWidth / actualColumns;
                    spriteHeight = texHeight / actualRows;
                }
                var factory = new SpriteDataProviderFactories(); factory.Init();
                var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer); dataProvider.InitSpriteEditorDataProvider();

                List<SpriteRect> spriteRects = new();
                TextureImporter texImporter = importer; bool wasReadable = texImporter.isReadable;
                if (!wasReadable) { texImporter.isReadable = true; AssetDatabase.ImportAsset(path); }

                for (int y = 0; y < actualRows; y++)
                {
                    for (int x = 0; x < actualColumns; x++)
                    {
                        int rectX = x * spriteWidth; int rectY = y * spriteHeight; int rectW = spriteWidth; int rectH = spriteHeight;
                        if (x == actualColumns - 1) rectW = texWidth - rectX;
                        if (y == actualRows - 1) rectH = texHeight - rectY;
                        rectW = Mathf.Clamp(rectW, 0, texWidth - rectX); rectH = Mathf.Clamp(rectH, 0, texHeight - rectY);
                        if (rectW <= 0 || rectH <= 0) continue;
                        int flippedY = texHeight - (rectY + rectH);
                        Rect cellRect = new(rectX, flippedY, rectW, rectH);
                        bool isEmpty = false;
                        if (ignoreEmptyRects)
                        {
                            Color[] pixels = texture.GetPixels(
                                Mathf.RoundToInt(cellRect.x),
                                Mathf.RoundToInt(cellRect.y),
                                Mathf.RoundToInt(cellRect.width),
                                Mathf.RoundToInt(cellRect.height));
                            isEmpty = true;
                            foreach (var pixel in pixels) { if (pixel.a > 0f) { isEmpty = false; break; } }
                        }
                        if (ignoreEmptyRects && isEmpty) continue;
                        SpriteRect rect = new()
                        {
                            name = $"{obj.name}_{x}_{y}",
                            rect = cellRect,
                            alignment = pivotAlignment,
                            pivot = GetPivotForAlignment(pivotAlignment)
                        };
                        spriteRects.Add(rect);
                    }
                }
                dataProvider.SetSpriteRects(spriteRects.ToArray()); dataProvider.Apply();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            Debug.Log("Batch slicing completed using ISpriteEditorDataProvider!");
        }
#endif // SPRITE_EDITOR_FEATURES_AVAILABLE

        private Vector2 GetPivotForAlignment(SpriteAlignment alignment) => alignment switch
        {
            SpriteAlignment.BottomCenter => new Vector2(0.5f, 0f),
            SpriteAlignment.Center => new Vector2(0.5f, 0.5f),
            SpriteAlignment.TopLeft => new Vector2(0f, 1f),
            SpriteAlignment.TopCenter => new Vector2(0.5f, 1f),
            SpriteAlignment.TopRight => new Vector2(1f, 1f),
            SpriteAlignment.LeftCenter => new Vector2(0f, 0.5f),
            SpriteAlignment.RightCenter => new Vector2(1f, 0.5f),
            SpriteAlignment.BottomLeft => new Vector2(0f, 0f),
            SpriteAlignment.BottomRight => new Vector2(1f, 0f),
            SpriteAlignment.Custom => new Vector2(0.5f, 0f),
            _ => new Vector2(0.5f, 0.5f),
        };
    }
}
#endif
