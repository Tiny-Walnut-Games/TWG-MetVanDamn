//  This script is intended to be used in the Unity Editor only, stored in an Editor folder to ensure it is not included in builds.
#if UNITY_EDITOR

#if HAS_SPRITE_EDITOR || UNITY_2D_SPRITE_EDITOR_AVAILABLE
#define SPRITE_EDITOR_FEATURES_AVAILABLE
#endif

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

#if SPRITE_EDITOR_FEATURES_AVAILABLE
using UnityEditor.U2D.Sprites;
#endif

namespace TinyWalnutGames.MetVD.Utility.Editor
{
    public class BatchSpriteSlicer : EditorWindow
    {
        private bool useCellSize = false;
        private Vector2 cellSize = new(64, 64);
        private int columns = 12;
        private int rows = 8;
        private SpriteAlignment pivotAlignment = SpriteAlignment.BottomCenter;
        private bool ignoreEmptyRects = true;
#if SPRITE_EDITOR_FEATURES_AVAILABLE
        private static List<SpriteRect> copiedRects = null;
        private static readonly Dictionary<string, List<Vector2[]>> copiedOutlines = new();
#else
        private static bool spriteEditorNotAvailable = true;
#endif
        private static int copiedTexWidth = 0;
        private static int copiedTexHeight = 0;

        [MenuItem("Tools/Batch Sprite Slicer")]
        public static void OpenWindow() => GetWindow<BatchSpriteSlicer>("Batch Sprite Slicer");

        private void OnGUI()
        {
#if !SPRITE_EDITOR_FEATURES_AVAILABLE
            GUILayout.Label("Batch Sprite Slicer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Sprite Editor package not available (com.unity.2d.sprite). Install to enable slicing.", MessageType.Warning);
            if (GUILayout.Button("Open Package Manager")) UnityEditor.PackageManager.UI.Window.Open("com.unity.2d.sprite");
            return;
#else
            GUILayout.Label("Batch Sprite Slicer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Slice multiple sprites. Copy/paste layouts or grid slice.", MessageType.Info);
            useCellSize = EditorGUILayout.Toggle("Use Cell Size", useCellSize);
            if (useCellSize)
            {
                cellSize = EditorGUILayout.Vector2Field("Cell Size", cellSize);
            }
            else
            {
                columns = EditorGUILayout.IntField("Columns", columns);
                rows = EditorGUILayout.IntField("Rows", rows);
            }
            pivotAlignment = (SpriteAlignment)EditorGUILayout.EnumPopup("Pivot Alignment", pivotAlignment);
            ignoreEmptyRects = EditorGUILayout.Toggle("Ignore Empty Rects", ignoreEmptyRects);
            GUILayout.Space(4);
            if (GUILayout.Button("Copy Rect Layout")) CopySlicesFromSelected();
            using (new EditorGUI.DisabledScope(copiedRects == null))
            {
                if (GUILayout.Button("Paste Rect Layout"))
                {
                    if (Selection.objects.Length == 0) Debug.LogWarning("No textures selected."); else PasteSlicesToSelected();
                }
            }
            if (GUILayout.Button("Adjust Pivot Of Selected Slices"))
            {
                if (Selection.objects.Length == 0) Debug.LogWarning("No textures selected."); else AdjustPivotOfSelectedSlices();
            }
            if (GUILayout.Button("Slice Selected Sprites (Grid)"))
            {
                if (Selection.objects.Length == 0) Debug.LogWarning("No textures selected."); else SliceSelectedSprites();
            }
#endif
        }

#if SPRITE_EDITOR_FEATURES_AVAILABLE
        private void CopySlicesFromSelected()
        {
            Object[] selectedTextures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
            if (selectedTextures.Length == 0) { Debug.LogWarning("No texture selected."); return; }
            string path = AssetDatabase.GetAssetPath(selectedTextures[0]);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) { Debug.LogWarning("Importer missing"); return; }
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path); if (!texture) { Debug.LogWarning("Texture load failed"); return; }
            var factory = new SpriteDataProviderFactories(); factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer); dataProvider.InitSpriteEditorDataProvider();
            var rectList = new List<SpriteRect>(dataProvider.GetSpriteRects()); if (rectList.Count == 0) { Debug.LogWarning("No slices found"); return; }
            copiedOutlines.Clear();
            var outlineProvider = dataProvider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
            foreach (var r in rectList)
            {
                var outlines = outlineProvider.GetOutlines(r.spriteID);
                copiedOutlines[r.spriteID.ToString()] = outlines != null ? outlines.Select(o => o.ToArray()).ToList() : new List<Vector2[]>();
            }
            copiedRects = rectList; copiedTexWidth = texture.width; copiedTexHeight = texture.height;
            Debug.Log($"Copied {rectList.Count} rects from {texture.name}");
        }

        private void PasteSlicesToSelected()
        {
            if (copiedRects == null || copiedRects.Count == 0) { Debug.LogWarning("Nothing copied"); return; }
            foreach (var obj in Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets))
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) { Debug.LogWarning($"Skip {obj.name}"); continue; }
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path); if (!texture) { Debug.LogWarning($"Skip {obj.name}"); continue; }
                int texW = texture.width, texH = texture.height;
                var factory = new SpriteDataProviderFactories(); factory.Init();
                var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer); dataProvider.InitSpriteEditorDataProvider();
                float scaleX = (float)texW / copiedTexWidth, scaleY = (float)texH / copiedTexHeight;
                var newRects = new List<SpriteRect>(); var guidMap = new Dictionary<string, SpriteRect>();
                foreach (var src in copiedRects)
                {
                    var r = src.rect; var scaled = new Rect(Mathf.RoundToInt(r.x * scaleX), Mathf.RoundToInt(r.y * scaleY), Mathf.RoundToInt(r.width * scaleX), Mathf.RoundToInt(r.height * scaleY));
                    if (scaled.width <= 0 || scaled.height <= 0) continue;
                    var nr = new SpriteRect { name = src.name, rect = scaled, alignment = src.alignment, pivot = src.pivot }; newRects.Add(nr); guidMap[src.spriteID.ToString()] = nr;
                }
                dataProvider.SetSpriteRects(System.Array.Empty<SpriteRect>()); dataProvider.Apply();
                dataProvider.SetSpriteRects(newRects.ToArray()); dataProvider.Apply();
                var outlineProvider = dataProvider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
                foreach (var src in copiedRects)
                {
                    if (!guidMap.TryGetValue(src.spriteID.ToString(), out var nr)) continue;
                    if (copiedOutlines.TryGetValue(src.spriteID.ToString(), out var outlines) && outlines != null && outlines.Count > 0)
                    {
                        var srcR = src.rect; var dstR = nr.rect; float ox = dstR.width / srcR.width, oy = dstR.height / srcR.height; var scaledOut = new List<Vector2[]>();
                        foreach (var o in outlines) { var arr = new Vector2[o.Length]; for (int i = 0; i < o.Length; i++) arr[i] = new Vector2(o[i].x * ox, o[i].y * oy); scaledOut.Add(arr); }
                        outlineProvider.SetOutlines(nr.spriteID, scaledOut);
                    }
                }
                dataProvider.Apply(); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                Debug.Log($"Pasted {newRects.Count} slices to {texture.name}");
            }
        }

        private void AdjustPivotOfSelectedSlices()
        {
            foreach (var obj in Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets))
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) { Debug.LogWarning($"Skip {obj.name}"); continue; }
                var factory = new SpriteDataProviderFactories(); factory.Init();
                var dp = factory.GetSpriteEditorDataProviderFromObject(importer); dp.InitSpriteEditorDataProvider();
                var rects = new List<SpriteRect>(dp.GetSpriteRects()); if (rects.Count == 0) continue;
                for (int i = 0; i < rects.Count; i++) { rects[i].alignment = pivotAlignment; rects[i].pivot = GetPivotForAlignment(pivotAlignment); }
                dp.SetSpriteRects(rects.ToArray()); dp.Apply(); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        private void SliceSelectedSprites()
        {
            foreach (var obj in Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets))
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) { Debug.LogWarning($"Skip {obj.name}"); continue; }
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path); if (!texture) continue;
                int texW = texture.width, texH = texture.height; int actualCols, actualRows, spriteW, spriteH;
                if (useCellSize) { spriteW = Mathf.Max(1, Mathf.RoundToInt(cellSize.x)); spriteH = Mathf.Max(1, Mathf.RoundToInt(cellSize.y)); actualCols = Mathf.Max(1, texW / spriteW); actualRows = Mathf.Max(1, texH / spriteH); }
                else { actualCols = Mathf.Max(1, columns); actualRows = Mathf.Max(1, rows); spriteW = texW / actualCols; spriteH = texH / actualRows; }
                var factory = new SpriteDataProviderFactories(); factory.Init();
                var dp = factory.GetSpriteEditorDataProviderFromObject(importer); dp.InitSpriteEditorDataProvider();
                var rects = new List<SpriteRect>(); var texImp = importer; bool readable = texImp.isReadable; if (!readable) { texImp.isReadable = true; AssetDatabase.ImportAsset(path); }
                for (int y = 0; y < actualRows; y++)
                {
                    for (int x = 0; x < actualCols; x++)
                    {
                        int rx = x * spriteW, ry = y * spriteH, rw = spriteW, rh = spriteH; if (x == actualCols - 1) rw = texW - rx; if (y == actualRows - 1) rh = texH - ry;
                        rw = Mathf.Clamp(rw, 0, texW - rx); rh = Mathf.Clamp(rh, 0, texH - ry); if (rw <= 0 || rh <= 0) continue;
                        int flippedY = texH - (ry + rh); var cell = new Rect(rx, flippedY, rw, rh); bool empty = false;
                        if (ignoreEmptyRects)
                        {
                            var pixels = texture.GetPixels(Mathf.RoundToInt(cell.x), Mathf.RoundToInt(cell.y), Mathf.RoundToInt(cell.width), Mathf.RoundToInt(cell.height));
                            empty = true; foreach (var p in pixels) { if (p.a > 0f) { empty = false; break; } }
                        }
                        if (ignoreEmptyRects && empty) continue;
                        rects.Add(new SpriteRect { name = $"{obj.name}_{x}_{y}", rect = cell, alignment = pivotAlignment, pivot = GetPivotForAlignment(pivotAlignment) });
                    }
                }
                dp.SetSpriteRects(rects.ToArray()); dp.Apply(); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
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
            _ => new Vector2(0.5f, 0.5f)
        };
    }
}
#endif
