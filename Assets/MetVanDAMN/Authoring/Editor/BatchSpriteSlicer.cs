//  This script is intended to be used in the Unity Editor only, stored in an Editor folder to ensure it is not included in builds.
//  Written, signed, cleaned, and commented by Bellok Tiny Walnut Games
#if UNITY_EDITOR

#if HAS_SPRITE_EDITOR || UNITY_2D_SPRITE_EDITOR_AVAILABLE
#define HAS_SPRITE_EDITOR
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

#if HAS_SPRITE_EDITOR
using UnityEditor.U2D.Sprites;
#endif

namespace TinyWalnutGames.MetVD.Utility.Editor
{
    /// <summary>
    /// Batch sprite slicing and pivot adjustment tool for 2D textures.
    /// Supports grid slicing, copying/pasting slice layouts between textures, and batch pivot updates.
    /// Uses <see cref="ISpriteEditorDataProvider"/> exclusively (no obsolete APIs) for safe metadata access.
    /// </summary>
    /// <remarks>
    /// Features:
    /// - Grid based slicing (fixed cell size or rows/columns)
    /// - Ignore fully transparent (empty) cells
    /// - Copy & paste slice layouts with automatic scaling to target texture size
    /// - Batch pivot realignment based on <see cref="SpriteAlignment"/>
    /// - Optional removal of empty rects to reduce asset count
    /// - Outline copy / paste when outline provider available (Unity 2022.2+)
    /// </remarks>
    public class BatchSpriteSlicer : EditorWindow
    {
        // Configuration state
        private bool useCellSize = false;
        private Vector2 cellSize = new(64, 64);
        private int columns = 12;
        private int rows = 8;
        private SpriteAlignment pivotAlignment = SpriteAlignment.BottomCenter;
        private bool ignoreEmptyRects = true;

#if HAS_SPRITE_EDITOR
        private static List<SpriteRect> copiedRects = null;
        // Outlines per sprite name (List<Vector2[]> matches ISpriteOutlineDataProvider contract)
        private static readonly Dictionary<string, List<Vector2[]>> copiedOutlines = new();
#else
        private static bool spriteEditorNotAvailable = true;
#endif
        private static int copiedTexWidth = 0;
        private static int copiedTexHeight = 0;

        /// <summary>
        /// Opens the Batch Sprite Slicer editor window.
        /// </summary>
        [MenuItem("Tools/Batch Sprite Slicer")]
        public static void OpenWindow() => GetWindow<BatchSpriteSlicer>("Batch Sprite Slicer");

        /// <summary>
        /// Renders the UI for all batch operations (slicing, layout copy/paste, pivot adjustment).
        /// </summary>
        private void OnGUI()
        {
#if !HAS_SPRITE_EDITOR
            GUILayout.Label("Batch Sprite Slicer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Sprite Editor features are not available. Install com.unity.2d.sprite package.",
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
                EditorGUILayout.HelpBox("Grid inferred from texture dimensions.", MessageType.None);
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
#if HAS_SPRITE_EDITOR
            if (GUILayout.Button(new GUIContent("Copy Rect Layout", "Copy slice rectangles from first selected texture (outlines included if supported).")))
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
#endif

            EditorGUILayout.Space();
            GUILayout.Label("Pivot Adjustment", EditorStyles.boldLabel);
#if HAS_SPRITE_EDITOR
            if (GUILayout.Button(new GUIContent("Adjust Pivot Of Selected Slices", "Overwrite pivots for all slices on selected textures.")))
            {
                if (Selection.objects.Length == 0)
                    Debug.LogWarning("No textures selected.");
                else
                    AdjustPivotOfSelectedSlices();
            }
#endif

            EditorGUILayout.Space();
            GUILayout.Label("Batch Slicing", EditorStyles.boldLabel);
#if HAS_SPRITE_EDITOR
            if (GUILayout.Button(new GUIContent("Slice Selected Sprites (Grid)", "Slice selected textures using current grid settings.")))
            {
                if (Selection.objects.Length == 0)
                    Debug.LogWarning("No textures selected.");
                else
                    SliceSelectedSprites();
            }
#endif
        }

#if HAS_SPRITE_EDITOR
        private static bool TryGetSpriteProvider(TextureImporter importer, out ISpriteEditorDataProvider dataProvider, out SpriteDataProviderFactories factories)
        {
            factories = new SpriteDataProviderFactories();
            factories.Init();
            dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (dataProvider == null)
                return false;
            dataProvider.InitSpriteEditorDataProvider();
            return true;
        }

        private static bool TryGetOutlineProvider(ISpriteEditorDataProvider provider, out ISpriteOutlineDataProvider outlineProvider)
        {
            outlineProvider = provider.GetDataProvider<ISpriteOutlineDataProvider>();
            return outlineProvider != null;
        }

        private static List<Vector2[]> CloneOutlines(IReadOnlyList<Vector2[]> src)
        {
            if (src == null) return null;
            var cloned = new List<Vector2[]>(src.Count);
            for (int i = 0; i < src.Count; i++)
            {
                var ring = src[i];
                if (ring == null) { cloned.Add(null); continue; }
                var copy = new Vector2[ring.Length];
                Array.Copy(ring, copy, ring.Length);
                cloned.Add(copy);
            }
            return cloned;
        }

        private void CopySlicesFromSelected()
        {
            var selectedTextures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
            if (selectedTextures.Length == 0)
            {
                Debug.LogWarning("No texture selected to copy slices from.");
                return;
            }
            string path = AssetDatabase.GetAssetPath(selectedTextures[0]);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                Debug.LogWarning("Texture importer not found.");
                return;
            }
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (!texture)
            {
                Debug.LogWarning("Could not load texture asset.");
                return;
            }
            if (!TryGetSpriteProvider(importer, out var provider, out _))
            {
                Debug.LogWarning("Failed to create sprite data provider.");
                return;
            }

            var existingRects = provider.GetSpriteRects();
            copiedRects = new List<SpriteRect>(existingRects.Length);
            copiedOutlines.Clear();

#if UNITY_2022_2_OR_NEWER
            bool haveOutline = TryGetOutlineProvider(provider, out var outlineProvider);
#endif
            foreach (var r in existingRects)
            {
                var copy = new SpriteRect
                {
                    name = r.name,
                    rect = r.rect,
                    alignment = r.alignment,
                    pivot = r.pivot,
                    border = r.border,
#if UNITY_2022_2_OR_NEWER
                    spriteID = r.spriteID,
#endif
                };
                copiedRects.Add(copy);
#if UNITY_2022_2_OR_NEWER
                if (haveOutline && r.spriteID != default)
                {
                    var outlines = outlineProvider.GetOutlines(r.spriteID); // returns List<Vector2[]>
                    if (outlines != null)
                        copiedOutlines[copy.name] = CloneOutlines(outlines);
                }
#endif
            }
            copiedTexWidth = texture.width;
            copiedTexHeight = texture.height;
            Debug.Log($"Copied {copiedRects.Count} slice rectangles from {texture.name} (outlines: {copiedOutlines.Count}).");
        }

        private void PasteSlicesToSelected()
        {
            if (copiedRects == null || copiedRects.Count == 0)
            {
                Debug.LogWarning("No rects copied to paste.");
                return;
            }
            var selectedTextures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
            int applied = 0;
            foreach (var obj in selectedTextures)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    continue;
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (!texture) continue;
                if (!TryGetSpriteProvider(importer, out var provider, out _))
                {
                    Debug.LogWarning($"Skipping {texture.name}: provider unavailable.");
                    continue;
                }
                float scaleX = copiedTexWidth > 0 ? (float)texture.width / copiedTexWidth : 1f;
                float scaleY = copiedTexHeight > 0 ? (float)texture.height / copiedTexHeight : 1f;
                var newRects = new List<SpriteRect>(copiedRects.Count);
                foreach (var src in copiedRects)
                {
                    var nr = new SpriteRect
                    {
                        name = src.name,
                        rect = new Rect(src.rect.x * scaleX, src.rect.y * scaleY, src.rect.width * scaleX, src.rect.height * scaleY),
                        alignment = src.alignment,
                        pivot = src.pivot,
                        border = src.border,
#if UNITY_2022_2_OR_NEWER
                        spriteID = src.spriteID != default ? src.spriteID : GUID.Generate(),
#endif
                    };
                    newRects.Add(nr);
                }
                provider.SetSpriteRects(newRects.ToArray());
#if UNITY_2022_2_OR_NEWER
                if (copiedOutlines.Count > 0 && TryGetOutlineProvider(provider, out var outlineProvider))
                {
                    foreach (var rect in newRects)
                    {
                        if (!copiedOutlines.TryGetValue(rect.name, out var rings) || rings == null)
                            continue;
                        var scaled = new List<Vector2[]>(rings.Count);
                        for (int i = 0; i < rings.Count; i++)
                        {
                            var ring = rings[i];
                            if (ring == null) { scaled.Add(null); continue; }
                            var ringScaled = new Vector2[ring.Length];
                            for (int p = 0; p < ring.Length; p++)
                                ringScaled[p] = new Vector2(ring[p].x * scaleX, ring[p].y * scaleY);
                            scaled.Add(ringScaled);
                        }
                        outlineProvider.SetOutlines(rect.spriteID, scaled);
                    }
                }
#endif
                provider.Apply();
                importer.SaveAndReimport();
                applied++;
            }
            Debug.Log($"Pasted slice layout to {applied} textures (outlines applied where available).");
        }

        private void AdjustPivotOfSelectedSlices()
        {
            var selectedTextures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
            int adjusted = 0;
            foreach (var obj in selectedTextures)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                if (!TryGetSpriteProvider(importer, out var provider, out _)) { Debug.LogWarning($"Skipping {obj.name}: provider unavailable."); continue; }
                var rects = provider.GetSpriteRects();
                if (rects == null || rects.Length == 0) continue;
                for (int i = 0; i < rects.Length; i++) { rects[i].alignment = pivotAlignment; rects[i].pivot = GetPivotForAlignment(pivotAlignment); }
                provider.SetSpriteRects(rects);
                provider.Apply();
                importer.SaveAndReimport();
                adjusted++;
            }
            Debug.Log($"Adjusted pivots for {adjusted} textures");
        }

        private void SliceSelectedSprites()
        {
            var selectedTextures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
            int sliced = 0;
            foreach (var obj in selectedTextures)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (!texture) continue;
                if (!TryGetSpriteProvider(importer, out var provider, out _)) { Debug.LogWarning($"Skipping {obj.name}: provider unavailable."); continue; }
                var newRects = new List<SpriteRect>();
                if (useCellSize)
                {
                    int colCount = Mathf.FloorToInt(texture.width / cellSize.x);
                    int rowCount = Mathf.FloorToInt(texture.height / cellSize.y);
                    for (int row = 0; row < rowCount; row++)
                        for (int col = 0; col < colCount; col++)
                        {
                            var rect = new Rect(col * cellSize.x, (rowCount - row - 1) * cellSize.y, cellSize.x, cellSize.y);
                            if (ignoreEmptyRects && IsRectEmpty(texture, rect)) continue;
                            newRects.Add(CreateSpriteRect(texture.name, row, col, rect));
                        }
                }
                else
                {
                    float cellWidth = (float)texture.width / columns;
                    float cellHeight = (float)texture.height / rows;
                    for (int row = 0; row < rows; row++)
                        for (int col = 0; col < columns; col++)
                        {
                            var rect = new Rect(col * cellWidth, (rows - row - 1) * cellHeight, cellWidth, cellHeight);
                            if (ignoreEmptyRects && IsRectEmpty(texture, rect)) continue;
                            newRects.Add(CreateSpriteRect(texture.name, row, col, rect));
                        }
                }
                provider.SetSpriteRects(newRects.ToArray());
                provider.Apply();
                importer.SaveAndReimport();
                sliced++;
            }
            Debug.Log($"Sliced {sliced} textures");
        }

        private SpriteRect CreateSpriteRect(string baseName, int row, int col, Rect rect)
        {
            return new SpriteRect
            {
                name = $"{baseName}_{row}_{col}",
                rect = rect,
                alignment = pivotAlignment,
                pivot = GetPivotForAlignment(pivotAlignment),
                border = Vector4.zero,
#if UNITY_2022_2_OR_NEWER
                spriteID = GUID.Generate(),
#endif
            };
        }

        private bool IsRectEmpty(Texture2D texture, Rect rect)
        {
            int xMin = Mathf.RoundToInt(rect.x);
            int yMin = Mathf.RoundToInt(rect.y);
            int w = Mathf.RoundToInt(rect.width);
            int h = Mathf.RoundToInt(rect.height);
            xMin = Mathf.Clamp(xMin, 0, texture.width - 1);
            yMin = Mathf.Clamp(yMin, 0, texture.height - 1);
            w = Mathf.Clamp(w, 1, texture.width - xMin);
            h = Mathf.Clamp(h, 1, texture.height - yMin);
            var pixels = texture.GetPixels(xMin, yMin, w, h);
            for (int i = 0; i < pixels.Length; i++) if (pixels[i].a > 0.0001f) return false; return true;
        }
#endif // HAS_SPRITE_EDITOR

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
