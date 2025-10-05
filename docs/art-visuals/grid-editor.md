# 🎨 Grid Layer Editor
## *Advanced Tilemap and Layering Tools*

> **"Good level design is invisible - players should feel immersed, not aware of the grid. But you need to see the grid to create that magic."**

[![Grid Editor](https://img.shields.io/badge/Grid-Editor-purple.svg)](grid-editor.md)
[![Tilemap](https://img.shields.io/badge/Tilemap-Advanced-blue.svg)](grid-editor.md)

---

## 🎯 **What is the Grid Layer Editor?**

**Grid Layer Editor** is MetVanDAMN's advanced tilemap system that lets you create complex, multi-layered worlds. Unlike basic tilemaps, it supports:

- 🏗️ **Multiple Layers** - Background, gameplay, foreground
- 🔄 **Isometric & Hexagonal** - Different grid types
- 🎭 **Biome Integration** - Art that matches world themes
- 🎮 **Interactive Editing** - Paint, erase, select tools
- 📏 **Custom Grids** - Any size, shape, or orientation

**Perfect for creating rich, detailed environments that feel alive!**

---

## 🏗️ **Core Concepts**

### **Layer System**

```csharp
// Layer configuration
[System.Serializable]
public class GridLayer
{
    public string Name = "Background";
    public int SortingOrder = 0;
    public bool IsInteractive = false; // Can player interact?
    public Tilemap Tilemap;
    public TileBase[] AvailableTiles;
}

// Multi-layer grid setup
public class MultiLayerGrid : MonoBehaviour
{
    [SerializeField] private GridLayer[] layers;
    [SerializeField] private Grid grid;

    void Start()
    {
        InitializeLayers();
    }

    private void InitializeLayers()
    {
        foreach (var layer in layers)
        {
            if (layer.Tilemap == null)
            {
                // Create tilemap for this layer
                var tilemapObj = new GameObject(layer.Name);
                tilemapObj.transform.parent = transform;

                layer.Tilemap = tilemapObj.AddComponent<Tilemap>();
                var renderer = tilemapObj.AddComponent<TilemapRenderer>();
                renderer.sortingOrder = layer.SortingOrder;
            }
        }
    }
}
```

### **Grid Types**

```csharp
// Different grid configurations
public enum GridType
{
    Square,
    Isometric,
    Hexagonal
}

public class AdaptiveGrid : MonoBehaviour
{
    [SerializeField] private GridType gridType;
    [SerializeField] private float cellSize = 1f;

    private Grid grid;

    void Start()
    {
        SetupGrid();
    }

    private void SetupGrid()
    {
        grid = GetComponent<Grid>();

        switch (gridType)
        {
            case GridType.Square:
                // Standard square grid
                grid.cellLayout = GridLayout.CellLayout.Rectangle;
                grid.cellSize = new Vector3(cellSize, cellSize, 0);
                break;

            case GridType.Isometric:
                // Isometric diamond grid
                grid.cellLayout = GridLayout.CellLayout.Isometric;
                grid.cellSize = new Vector3(cellSize, cellSize * 0.5f, 0);
                break;

            case GridType.Hexagonal:
                // Hexagonal grid
                grid.cellLayout = GridLayout.CellLayout.Hexagon;
                grid.cellSize = new Vector3(cellSize, cellSize, 0);
                break;
        }
    }
}
```

---

## 🚀 **Quick Grid Setup (15 Minutes)**

### **Step 1: Create Grid Foundation**

```csharp
// Grid setup component
public class GridFoundation : MonoBehaviour
{
    [SerializeField] private Vector2Int gridSize = new Vector2Int(50, 50);
    [SerializeField] private float cellSize = 1f;

    void Start()
    {
        CreateGrid();
    }

    private void CreateGrid()
    {
        // Create grid GameObject
        var gridObj = new GameObject("WorldGrid");
        gridObj.transform.parent = transform;

        var grid = gridObj.AddComponent<Grid>();
        grid.cellSize = new Vector3(cellSize, cellSize, 0);

        // Create layers
        CreateLayer(grid.transform, "Background", -10);
        CreateLayer(grid.transform, "Terrain", -5);
        CreateLayer(grid.transform, "Interactive", 0);
        CreateLayer(grid.transform, "Foreground", 10);
    }

    private void CreateLayer(Transform parent, string name, int sortingOrder)
    {
        var layerObj = new GameObject(name);
        layerObj.transform.parent = parent;

        var tilemap = layerObj.AddComponent<Tilemap>();
        var renderer = layerObj.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;

        // Add collider if interactive
        if (sortingOrder >= 0)
        {
            layerObj.AddComponent<TilemapCollider2D>();
        }
    }
}
```

### **Step 2: Add Tile Painting**

```csharp
// Tile painting system
public class TilePainter : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private Tilemap targetTilemap;
    [SerializeField] private TileBase paintTile;

    void Update()
    {
        if (Input.GetMouseButton(0)) // Left click to paint
        {
            PaintTile();
        }
        else if (Input.GetMouseButton(1)) // Right click to erase
        {
            EraseTile();
        }
    }

    private void PaintTile()
    {
        Vector3 worldPoint = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = targetTilemap.WorldToCell(worldPoint);

        targetTilemap.SetTile(cellPosition, paintTile);
    }

    private void EraseTile()
    {
        Vector3 worldPoint = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = targetTilemap.WorldToCell(worldPoint);

        targetTilemap.SetTile(cellPosition, null);
    }
}
```

### **Step 3: Biome-Aware Painting**

```csharp
// Paint tiles based on biome
public class BiomeTilePainter : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private BiomeArtProfile[] biomeProfiles;

    public void PaintBiomeTiles(BiomeType biome, Bounds area)
    {
        var profile = GetBiomeProfile(biome);
        if (profile == null) return;

        // Convert bounds to cell coordinates
        Vector3Int minCell = tilemap.WorldToCell(area.min);
        Vector3Int maxCell = tilemap.WorldToCell(area.max);

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, 0);

                // Check if this position should have biome art
                if (ShouldPlaceBiomeTile(cellPos, profile))
                {
                    TileBase tile = SelectBiomeTile(profile);
                    tilemap.SetTile(cellPos, tile);
                }
            }
        }
    }

    private BiomeArtProfile GetBiomeProfile(BiomeType biome)
    {
        return biomeProfiles.FirstOrDefault(p => p.BiomeType == biome);
    }

    private bool ShouldPlaceBiomeTile(Vector3Int position, BiomeArtProfile profile)
    {
        // Use noise or rules to decide placement
        float noise = Mathf.PerlinNoise(position.x * 0.1f, position.y * 0.1f);
        return noise > profile.Density;
    }

    private TileBase SelectBiomeTile(BiomeArtProfile profile)
    {
        // Random selection with weights
        float totalWeight = profile.Tiles.Sum(t => t.Weight);
        float random = UnityEngine.Random.Range(0f, totalWeight);

        float currentWeight = 0f;
        foreach (var tileWeight in profile.Tiles)
        {
            currentWeight += tileWeight.Weight;
            if (random <= currentWeight)
            {
                return tileWeight.Tile;
            }
        }

        return profile.Tiles[0].Tile; // Fallback
    }
}
```

---

## 🎨 **Advanced Layer Features**

### **Layer Blending**

```csharp
// Blend between layers for smooth transitions
public class LayerBlender : MonoBehaviour
{
    [SerializeField] private Tilemap backgroundLayer;
    [SerializeField] private Tilemap foregroundLayer;
    [SerializeField] private float blendDistance = 5f;

    public void BlendLayers(Bounds blendArea)
    {
        Vector3Int minCell = backgroundLayer.WorldToCell(blendArea.min);
        Vector3Int maxCell = backgroundLayer.WorldToCell(blendArea.max);

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                Vector3 worldPos = backgroundLayer.CellToWorld(cellPos);

                // Calculate blend factor based on distance to blend area center
                float distance = Vector3.Distance(worldPos, blendArea.center);
                float blendFactor = Mathf.Clamp01(distance / blendDistance);

                // Blend tile alpha or use transition tiles
                BlendTilesAtPosition(cellPos, blendFactor);
            }
        }
    }

    private void BlendTilesAtPosition(Vector3Int position, float blendFactor)
    {
        var bgTile = backgroundLayer.GetTile(position);
        var fgTile = foregroundLayer.GetTile(position);

        if (bgTile != null && fgTile != null)
        {
            // Create blended tile or use alpha
            var blendedTile = CreateBlendedTile(bgTile, fgTile, blendFactor);
            foregroundLayer.SetTile(position, blendedTile);
        }
    }
}
```

### **Interactive Layers**

```csharp
// Layers that respond to player interaction
public class InteractiveLayer : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase brokenTile;
    [SerializeField] private float breakRadius = 1f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Break tiles around impact point
            Vector3 impactPoint = collision.GetContact(0).point;
            BreakTiles(impactPoint);
        }
    }

    private void BreakTiles(Vector3 center)
    {
        Vector3Int centerCell = tilemap.WorldToCell(center);
        int radiusInCells = Mathf.CeilToInt(breakRadius / tilemap.cellSize.x);

        for (int x = -radiusInCells; x <= radiusInCells; x++)
        {
            for (int y = -radiusInCells; y <= radiusInCells; y++)
            {
                Vector3Int cellPos = centerCell + new Vector3Int(x, y, 0);

                // Check if within circular radius
                Vector3 cellWorldPos = tilemap.CellToWorld(cellPos);
                if (Vector3.Distance(cellWorldPos, center) <= breakRadius)
                {
                    // Replace with broken tile or remove
                    if (brokenTile != null)
                    {
                        tilemap.SetTile(cellPos, brokenTile);
                    }
                    else
                    {
                        tilemap.SetTile(cellPos, null);
                    }
                }
            }
        }
    }
}
```

### **Procedural Layer Generation**

```csharp
// Generate layers algorithmically
public class ProceduralLayerGenerator : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase[] terrainTiles;
    [SerializeField] private int seed = 42;

    [ContextMenu("Generate Layer")]
    public void GenerateLayer()
    {
        var random = new System.Random(seed);

        BoundsInt bounds = tilemap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);

                // Generate height using noise
                float height = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);

                // Choose tile based on height
                TileBase tile = SelectTileByHeight(height, random);
                tilemap.SetTile(position, tile);
            }
        }
    }

    private TileBase SelectTileByHeight(float height, System.Random random)
    {
        if (height < 0.3f)
        {
            return terrainTiles[0]; // Water
        }
        else if (height < 0.6f)
        {
            return terrainTiles[1]; // Grass
        }
        else if (height < 0.8f)
        {
            return terrainTiles[2]; // Dirt
        }
        else
        {
            return terrainTiles[3]; // Stone
        }
    }
}
```

---

## 🎮 **Editor Tools**

### **Custom Editor Window**

```csharp
using UnityEditor;
using UnityEngine;

public class GridLayerEditorWindow : EditorWindow
{
    private MultiLayerGrid selectedGrid;
    private int selectedLayer = 0;
    private TileBase selectedTile;

    [MenuItem("MetVanDAMN/Grid Layer Editor")]
    static void ShowWindow()
    {
        GetWindow<GridLayerEditorWindow>("Grid Layer Editor");
    }

    void OnGUI()
    {
        selectedGrid = (MultiLayerGrid)EditorGUILayout.ObjectField(
            "Target Grid", selectedGrid, typeof(MultiLayerGrid), true);

        if (selectedGrid == null) return;

        // Layer selection
        string[] layerNames = selectedGrid.GetLayerNames();
        selectedLayer = EditorGUILayout.Popup("Layer", selectedLayer, layerNames);

        // Tile selection
        selectedTile = (TileBase)EditorGUILayout.ObjectField(
            "Paint Tile", selectedTile, typeof(TileBase), false);

        // Tools
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Paint Mode"))
        {
            EnablePaintMode();
        }

        if (GUILayout.Button("Fill Layer"))
        {
            FillLayer();
        }

        if (GUILayout.Button("Clear Layer"))
        {
            ClearLayer();
        }
    }

    private void EnablePaintMode()
    {
        // Switch to paint mode in scene view
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            // Paint tile at mouse position
            Vector3 mousePos = e.mousePosition;
            mousePos.y = sceneView.camera.pixelHeight - mousePos.y;
            mousePos = sceneView.camera.ScreenToWorldPoint(mousePos);

            var tilemap = selectedGrid.GetLayerTilemap(selectedLayer);
            Vector3Int cellPos = tilemap.WorldToCell(mousePos);
            tilemap.SetTile(cellPos, selectedTile);

            e.Use();
        }
    }

    private void FillLayer()
    {
        var tilemap = selectedGrid.GetLayerTilemap(selectedLayer);
        BoundsInt bounds = tilemap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), selectedTile);
            }
        }
    }

    private void ClearLayer()
    {
        var tilemap = selectedGrid.GetLayerTilemap(selectedLayer);
        tilemap.ClearAllTiles();
    }
}
```

### **Brush Tools**

```csharp
// Custom brush shapes
public abstract class GridBrush : ScriptableObject
{
    public abstract void Paint(Tilemap tilemap, Vector3Int position, TileBase tile);
    public abstract void Erase(Tilemap tilemap, Vector3Int position);
}

public class CircleBrush : GridBrush
{
    [SerializeField] private int radius = 3;

    public override void Paint(Tilemap tilemap, Vector3Int center, TileBase tile)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x * x + y * y <= radius * radius)
                {
                    Vector3Int pos = center + new Vector3Int(x, y, 0);
                    tilemap.SetTile(pos, tile);
                }
            }
        }
    }

    public override void Erase(Tilemap tilemap, Vector3Int center)
    {
        Paint(tilemap, center, null); // Erase with null tile
    }
}
```

---

## 🎯 **Performance Optimization**

### **Chunk-Based Rendering**

```csharp
// Render only visible chunks
public class ChunkedTilemapRenderer : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private int chunkSize = 16;
    [SerializeField] private Camera targetCamera;

    private Dictionary<Vector2Int, TilemapChunk> chunks = new Dictionary<Vector2Int, TilemapChunk>();

    void Update()
    {
        UpdateVisibleChunks();
    }

    private void UpdateVisibleChunks()
    {
        // Calculate visible area
        Bounds visibleBounds = CalculateVisibleBounds();

        // Convert to chunk coordinates
        Vector2Int minChunk = WorldToChunk(visibleBounds.min);
        Vector2Int maxChunk = WorldToChunk(visibleBounds.max);

        // Enable visible chunks, disable others
        var chunksToRemove = new List<Vector2Int>(chunks.Keys);

        for (int x = minChunk.x; x <= maxChunk.x; x++)
        {
            for (int y = minChunk.y; y <= maxChunk.y; y++)
            {
                Vector2Int chunkCoord = new Vector2Int(x, y);

                if (!chunks.ContainsKey(chunkCoord))
                {
                    chunks[chunkCoord] = CreateChunk(chunkCoord);
                }

                chunks[chunkCoord].SetVisible(true);
                chunksToRemove.Remove(chunkCoord);
            }
        }

        // Hide distant chunks
        foreach (var chunkCoord in chunksToRemove)
        {
            chunks[chunkCoord].SetVisible(false);
        }
    }

    private Bounds CalculateVisibleBounds()
    {
        if (targetCamera == null) return new Bounds();

        float height = 2f * targetCamera.orthographicSize;
        float width = height * targetCamera.aspect;

        return new Bounds(targetCamera.transform.position, new Vector3(width, height, 0));
    }

    private Vector2Int WorldToChunk(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / (chunkSize * tilemap.cellSize.x)),
            Mathf.FloorToInt(worldPos.y / (chunkSize * tilemap.cellSize.y))
        );
    }
}
```

### **LOD for Tilemaps**

```csharp
// Reduce detail for distant tiles
public class TilemapLOD : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float[] lodDistances = { 10f, 25f, 50f };

    private TileBase[] originalTiles;
    private BoundsInt tilemapBounds;

    void Start()
    {
        tilemapBounds = tilemap.cellBounds;
        CacheOriginalTiles();
    }

    void Update()
    {
        ApplyLOD();
    }

    private void CacheOriginalTiles()
    {
        originalTiles = new TileBase[tilemapBounds.size.x * tilemapBounds.size.y];

        int index = 0;
        for (int x = tilemapBounds.xMin; x < tilemapBounds.xMax; x++)
        {
            for (int y = tilemapBounds.yMin; y < tilemapBounds.yMax; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                originalTiles[index++] = tilemap.GetTile(pos);
            }
        }
    }

    private void ApplyLOD()
    {
        Vector3 cameraPos = targetCamera.transform.position;

        int index = 0;
        for (int x = tilemapBounds.xMin; x < tilemapBounds.xMax; x++)
        {
            for (int y = tilemapBounds.yMin; y < tilemapBounds.yMax; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                Vector3 worldPos = tilemap.CellToWorld(pos);
                float distance = Vector3.Distance(worldPos, cameraPos);

                // Determine LOD level
                int lodLevel = 0;
                for (int i = 0; i < lodDistances.Length; i++)
                {
                    if (distance > lodDistances[i])
                    {
                        lodLevel = i + 1;
                    }
                }

                // Apply LOD tile
                TileBase lodTile = GetLODTile(originalTiles[index], lodLevel);
                tilemap.SetTile(pos, lodTile);

                index++;
            }
        }
    }

    private TileBase GetLODTile(TileBase originalTile, int lodLevel)
    {
        // Return simplified version based on LOD level
        // Implementation depends on your tile assets
        return originalTile; // Placeholder
    }
}
```

---

## 🚀 **Next Steps**

**Ready to create amazing tilemaps?**
- **[Biome Art Integration](biome-art.md)** - Connect tiles to world themes
- **[Debug Visualization](../testing-debugging/debug-tools.md)** - See your grids in action
- **[Performance Optimization](../advanced/performance.md)** - Keep tilemaps running fast

**Need tilemap inspiration?**
- Check the [demo tilemaps](../../Assets/Scenes/) in the project
- Look at [tilemap tutorials](../../tutorials/) for examples
- Join [Unity Tilemap discussions](https://forum.unity.com/forums/2d.53/) for community help

---

*"Tilemaps are the invisible architecture of great games - they provide structure while staying out of the way of your creativity."*

**🍑 Happy Tilemapping! 🍑**
