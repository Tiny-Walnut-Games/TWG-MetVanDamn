//  This script is intended to be used in the Unity Editor only, stored in an Editor folder to ensure it is not included in builds.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites; // this script requires com.unity.2d.sprite package be imported to work.

namespace TinyWalnutGames.Tools.Editor
{
    /// <summary>
    /// Biome Region Extractor
    /// Unity Editor tool to automatically slice and organize biome-specific sprites from large spritesheets.
    /// Accepts color-coded masks, JSON rect lists, or tilemap exports to separate sprites by biome.
    /// Integrates with existing BatchSpriteSlicer infrastructure for consistent workflow.
    /// </summary>
    public class BiomeRegionExtractor : EditorWindow
    {
        // Color comparison tolerance for biome mask matching
        private const float ColorComparisonTolerance = 0.01f;
        
        // Spritesheet and mapping inputs
        private List<Texture2D> spritesheets = new();
        private UnityEngine.Object biomeMaskAsset = null; // Can be Texture2D, TextAsset (JSON), or other mapping file
        
        // Cell and slicing settings (reusing BatchSpriteSlicer patterns)
        private Vector2Int cellSize = new(64, 64);
        private SpriteAlignment pivotAlignment = SpriteAlignment.BottomCenter;
        private bool generatePhysicsShape = false;
        
        // Biome mapping data
        [System.Serializable]
        public class BiomeMapping
        {
            public string biomeName = "Unknown";
            public Color maskColor = Color.white;
            public bool includeInExport = true;
            public string exportFolderSuffix = "";
        }
        
        private List<BiomeMapping> detectedBiomes = new();
        private bool showPreviewOverlay = true;
        private Vector2 biomeListScrollPos;
        
        // Export settings
        private string outputFolderPath = "Assets/Sprites/Biomes";
        private string fileNamingPattern = "{biome}_{row}_{col}";
        private bool createAtlasPerBiome = false;
        private bool embedBiomeMetadata = true;
        
        // Validation and preview
        private List<string> validationWarnings = new();
        private Vector2 validationScrollPos;
        private Vector2 previewScrollPos;
        private float previewZoom = 1f;
        
        // Processing state
        private bool isProcessing = false;
        private float processProgress = 0f;
        private string currentProcessingBiome = "";

        /// <summary>
        /// Opens the Biome Region Extractor window in the Unity Editor.
        /// </summary>
        [MenuItem("Tools/Biome Region Extractor")]
        public static void OpenWindow()
        {
            GetWindow<BiomeRegionExtractor>("Biome Region Extractor");
        }

        /// <summary>
        /// Called when the window is drawn in the Unity Editor.
        /// </summary>
        private void OnGUI()
        {
            GUILayout.Label("Biome Region Extractor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Automatically slice and organize biome-specific sprites from large spritesheets. " +
                "Accepts color-coded masks, JSON mappings, or tilemap exports to separate sprites by biome. " +
                "Integrates with existing sprite slicing infrastructure.", MessageType.Info);

            DrawSpritesheetSection();
            DrawBiomeMappingSection();
            DrawSlicingSettings();
            DrawValidationPanel();
            DrawPreviewArea();
            DrawExportSettings();
            DrawProcessingControls();
        }
        
        private void DrawSpritesheetSection()
        {
            GUILayout.Space(10);
            GUILayout.Label("Spritesheet Selection", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox(
                "Select one or more spritesheets containing mixed biome tiles. " +
                "All selected sheets should have consistent cell sizes and layouts.",
                MessageType.None);
            
            // Multi-select spritesheets
            if (GUILayout.Button("Select Spritesheets from Project"))
            {
                SelectSpritesheetsFromProject();
            }
            
            // Display selected spritesheets
            if (spritesheets.Count > 0)
            {
                EditorGUILayout.LabelField($"Selected Spritesheets ({spritesheets.Count}):");
                EditorGUI.indentLevel++;
                foreach (var sheet in spritesheets)
                {
                    EditorGUILayout.ObjectField(sheet, typeof(Texture2D), false);
                }
                EditorGUI.indentLevel--;
                
                if (GUILayout.Button("Clear Selection"))
                {
                    spritesheets.Clear();
                    detectedBiomes.Clear();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No spritesheets selected.", MessageType.Warning);
            }
        }
        
        private void DrawBiomeMappingSection()
        {
            GUILayout.Space(10);
            GUILayout.Label("Biome Mapping", EditorStyles.boldLabel);
            
            biomeMaskAsset = EditorGUILayout.ObjectField(
                "Biome Mask/Mapping", 
                biomeMaskAsset, 
                typeof(UnityEngine.Object), 
                false);
            
            EditorGUILayout.HelpBox(
                "Assign a color-coded mask (Texture2D), JSON mapping file (TextAsset), or tilemap export. " +
                "Each pixel/cell color will be mapped to a biome ID for sprite grouping.",
                MessageType.None);
            
            if (biomeMaskAsset != null)
            {
                if (GUILayout.Button("Detect Biomes from Mask"))
                {
                    DetectBiomesFromMask();
                }
            }
            else
            {
                if (GUILayout.Button("Auto-Detect Contiguous Regions"))
                {
                    AutoDetectBiomeRegions();
                }
                EditorGUILayout.HelpBox(
                    "Without a mask, the tool will attempt to detect contiguous regions of similar colors.",
                    MessageType.Info);
            }
            
            DrawBiomeList();
        }
        
        private void DrawBiomeList()
        {
            if (detectedBiomes.Count > 0)
            {
                GUILayout.Label("Detected Biomes", EditorStyles.boldLabel);
                
                biomeListScrollPos = EditorGUILayout.BeginScrollView(biomeListScrollPos, GUILayout.Height(120));
                
                for (int i = 0; i < detectedBiomes.Count; i++)
                {
                    var biome = detectedBiomes[i];
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    // Color swatch
                    biome.maskColor = EditorGUILayout.ColorField(GUIContent.none, biome.maskColor, 
                        false, false, false, GUILayout.Width(30));
                    
                    // Biome name (editable)
                    biome.biomeName = EditorGUILayout.TextField(biome.biomeName);
                    
                    // Include toggle
                    biome.includeInExport = EditorGUILayout.Toggle(biome.includeInExport, GUILayout.Width(20));
                    
                    EditorGUILayout.EndHorizontal();
                    
                    detectedBiomes[i] = biome;
                }
                
                EditorGUILayout.EndScrollView();
                
                showPreviewOverlay = EditorGUILayout.Toggle("Show Preview Colors", showPreviewOverlay);
            }
        }
        
        private void DrawSlicingSettings()
        {
            GUILayout.Space(10);
            GUILayout.Label("Slicing Settings", EditorStyles.boldLabel);
            
            cellSize = EditorGUILayout.Vector2IntField("Cell Size", cellSize);
            EditorGUILayout.HelpBox(
                "Width and height in pixels for each sprite cell. Must match the grid used in your spritesheets.",
                MessageType.None);
            
            pivotAlignment = (SpriteAlignment)EditorGUILayout.EnumPopup("Pivot Alignment", pivotAlignment);
            generatePhysicsShape = EditorGUILayout.Toggle("Generate Physics Shape", generatePhysicsShape);
        }
        
        private void DrawValidationPanel()
        {
            GUILayout.Space(10);
            GUILayout.Label("Validation", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Run Pre-Flight Check"))
            {
                RunPreFlightValidation();
            }
            
            if (validationWarnings.Count > 0)
            {
                EditorGUILayout.HelpBox($"{validationWarnings.Count} validation issues found:", MessageType.Warning);
                
                validationScrollPos = EditorGUILayout.BeginScrollView(validationScrollPos, GUILayout.Height(80));
                foreach (var warning in validationWarnings)
                {
                    EditorGUILayout.LabelField("• " + warning, EditorStyles.wordWrappedLabel);
                }
                EditorGUILayout.EndScrollView();
            }
            else if (spritesheets.Count > 0)
            {
                EditorGUILayout.HelpBox("✓ No validation issues detected.", MessageType.Info);
            }
        }
        
        private void DrawPreviewArea()
        {
            GUILayout.Space(10);
            GUILayout.Label("Preview", EditorStyles.boldLabel);
            
            if (spritesheets.Count > 0)
            {
                previewZoom = EditorGUILayout.Slider("Preview Zoom", previewZoom, 0.1f, 2f);
                
                previewScrollPos = EditorGUILayout.BeginScrollView(previewScrollPos, GUILayout.Height(200));
                
                var firstSheet = spritesheets[0];
                if (firstSheet != null)
                {
                    var rect = GUILayoutUtility.GetRect(
                        firstSheet.width * previewZoom, 
                        firstSheet.height * previewZoom);
                    
                    EditorGUI.DrawPreviewTexture(rect, firstSheet);
                    
                    if (showPreviewOverlay && detectedBiomes.Count > 0)
                    {
                        DrawBiomeOverlay(rect, firstSheet);
                    }
                }
                
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("Select spritesheets to see preview.", MessageType.Info);
            }
        }
        
        private void DrawExportSettings()
        {
            GUILayout.Space(10);
            GUILayout.Label("Export Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Output Folder:", GUILayout.Width(100));
            outputFolderPath = EditorGUILayout.TextField(outputFolderPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                var path = EditorUtility.SaveFolderPanel("Select Output Folder", outputFolderPath, "");
                if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                {
                    outputFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            fileNamingPattern = EditorGUILayout.TextField("File Naming Pattern", fileNamingPattern);
            EditorGUILayout.HelpBox(
                "Use {biome}, {row}, {col} placeholders. Example: '{biome}_{row}_{col}' → 'Forest_0_1.png'",
                MessageType.None);
            
            createAtlasPerBiome = EditorGUILayout.Toggle("Create Atlas per Biome", createAtlasPerBiome);
            embedBiomeMetadata = EditorGUILayout.Toggle("Embed Biome Metadata", embedBiomeMetadata);
            
            if (embedBiomeMetadata)
            {
                EditorGUILayout.HelpBox(
                    "Stores biome ID in sprite asset metadata for ECS bootstrap integration.",
                    MessageType.Info);
            }
        }
        
        private void DrawProcessingControls()
        {
            GUILayout.Space(10);
            GUILayout.Label("Processing", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(isProcessing || spritesheets.Count == 0 || detectedBiomes.Count == 0);
            
            if (GUILayout.Button("Process Biome Extraction", GUILayout.Height(30)))
            {
                StartBiomeExtraction();
            }
            
            EditorGUI.EndDisabledGroup();
            
            if (isProcessing)
            {
                var progressRect = EditorGUILayout.GetControlRect();
                EditorGUI.ProgressBar(progressRect, processProgress, $"Processing {currentProcessingBiome}...");
                
                if (GUILayout.Button("Cancel"))
                {
                    isProcessing = false;
                }
            }
        }

        #region Implementation Methods
        
        private void DrawBiomeOverlay(Rect previewRect, Texture2D spritesheet)
        {
            if (biomeMaskAsset == null || detectedBiomes.Count == 0) return;
            
            // Draw grid overlay showing biome regions
            Texture2D maskTexture = biomeMaskAsset as Texture2D;
            if (maskTexture == null) return;
            
            string maskPath = AssetDatabase.GetAssetPath(maskTexture);
            TextureImporter maskImporter = AssetImporter.GetAtPath(maskPath) as TextureImporter;
            bool maskWasReadable = maskImporter.isReadable;
            
            if (!maskWasReadable)
            {
                maskImporter.isReadable = true;
                AssetDatabase.ImportAsset(maskPath);
            }
            
            try
            {
                // Calculate grid dimensions
                int columns = spritesheet.width / cellSize.x;
                int rows = spritesheet.height / cellSize.y;
                
                // Draw grid cells with biome colors
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < columns; col++)
                    {
                        // Calculate cell position in mask
                        int maskX = col * cellSize.x + cellSize.x / 2;
                        int maskY = row * cellSize.y + cellSize.y / 2;
                        
                        // Clamp to texture bounds
                        maskX = Mathf.Clamp(maskX, 0, maskTexture.width - 1);
                        maskY = Mathf.Clamp(maskY, 0, maskTexture.height - 1);
                        
                        Color maskPixel = maskTexture.GetPixel(maskX, maskY);
                        
                        // Find matching biome
                        var matchingBiome = detectedBiomes.FirstOrDefault(b => 
                            IsCellInBiome(maskTexture, maskX, maskY, b.maskColor) && b.includeInExport);
                        
                        if (matchingBiome != null)
                        {
                            // Calculate screen position for this cell
                            float cellScreenX = previewRect.x + (col * cellSize.x * previewZoom);
                            float cellScreenY = previewRect.y + (row * cellSize.y * previewZoom);
                            float cellScreenWidth = cellSize.x * previewZoom;
                            float cellScreenHeight = cellSize.y * previewZoom;
                            
                            Rect cellRect = new Rect(cellScreenX, cellScreenY, cellScreenWidth, cellScreenHeight);
                            
                            // Draw semi-transparent biome color overlay
                            Color overlayColor = matchingBiome.maskColor;
                            overlayColor.a = 0.3f;
                            
                            EditorGUI.DrawRect(cellRect, overlayColor);
                            
                            // Draw biome name label for larger cells
                            if (cellScreenWidth > 40 && cellScreenHeight > 20)
                            {
                                GUI.Label(cellRect, matchingBiome.biomeName, 
                                    new GUIStyle(EditorStyles.miniLabel) 
                                    { 
                                        alignment = TextAnchor.MiddleCenter,
                                        normal = { textColor = Color.white }
                                    });
                            }
                        }
                    }
                }
                
                // Draw grid lines
                DrawPreviewGrid(previewRect, spritesheet, columns, rows);
            }
            finally
            {
                if (!maskWasReadable)
                {
                    maskImporter.isReadable = false;
                    AssetDatabase.ImportAsset(maskPath);
                }
            }
        }
        
        private void DrawPreviewGrid(Rect previewRect, Texture2D spritesheet, int columns, int rows)
        {
            Color gridColor = Color.white;
            gridColor.a = 0.5f;
            
            // Draw vertical lines
            for (int col = 0; col <= columns; col++)
            {
                float x = previewRect.x + (col * cellSize.x * previewZoom);
                Rect lineRect = new Rect(x, previewRect.y, 1, previewRect.height);
                EditorGUI.DrawRect(lineRect, gridColor);
            }
            
            // Draw horizontal lines
            for (int row = 0; row <= rows; row++)
            {
                float y = previewRect.y + (row * cellSize.y * previewZoom);
                Rect lineRect = new Rect(previewRect.x, y, previewRect.width, 1);
                EditorGUI.DrawRect(lineRect, gridColor);
            }
        }
        
        private void SelectSpritesheetsFromProject()
        {
            var selected = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
            spritesheets.Clear();
            
            foreach (UnityEngine.Object obj in selected)
            {
                if (obj is Texture2D texture)
                {
                    spritesheets.Add(texture);
                }
            }
            
            if (spritesheets.Count == 0)
            {
                Debug.LogWarning("No Texture2D assets selected. Please select spritesheets in the Project window.");
            }
            else
            {
                Debug.Log($"Selected {spritesheets.Count} spritesheets for biome extraction.");
            }
        }
        
        private void DetectBiomesFromMask()
        {
            detectedBiomes.Clear();
            
            if (biomeMaskAsset is Texture2D maskTexture)
            {
                DetectBiomesFromColorMask(maskTexture);
            }
            else if (biomeMaskAsset is TextAsset jsonAsset)
            {
                DetectBiomesFromJSON(jsonAsset);
            }
            else
            {
                Debug.LogWarning("Unsupported biome mask format. Please use Texture2D or TextAsset (JSON).");
            }
        }
        
        private void DetectBiomesFromColorMask(Texture2D maskTexture)
        {
            // Make texture readable temporarily
            var path = AssetDatabase.GetAssetPath(maskTexture);
            var importer = TextureImporter.GetAtPath(path) as TextureImporter;
            bool wasReadable = importer.isReadable;
            
            if (!wasReadable)
            {
                importer.isReadable = true;
                AssetDatabase.ImportAsset(path);
            }
            
            try
            {
                // Sample colors from the mask to detect unique biome colors
                var colors = maskTexture.GetPixels();
                var uniqueColors = new HashSet<Color>();

                // Sample at regular intervals to improve performance on large textures
                const int sampleStride = 4; // Adjust as needed for accuracy/performance tradeoff
                int width = maskTexture.width;
                int height = maskTexture.height;
                for (int y = 0; y < height; y += sampleStride)
                {
                    for (int x = 0; x < width; x += sampleStride)
                    {
                        int idx = y * width + x;
                        if (idx < colors.Length)
                        {
                            var color = colors[idx];
                            // Skip transparent pixels
                            if (color.a > 0.1f)
                            {
                                // Round colors to avoid minor variations
                                var rounded = new Color(
                                    Mathf.Round(color.r * 255f) / 255f,
                                    Mathf.Round(color.g * 255f) / 255f,
                                    Mathf.Round(color.b * 255f) / 255f,
                                    1f);
                                uniqueColors.Add(rounded);
                            }
                        }
                    }
                }
                
                // Create biome mappings from unique colors
                int biomeIndex = 0;
                foreach (var color in uniqueColors)
                {
                    detectedBiomes.Add(new BiomeMapping
                    {
                        biomeName = GetBiomeNameFromColor(color, biomeIndex),
                        maskColor = color,
                        includeInExport = true,
                        exportFolderSuffix = GetBiomeNameFromColor(color, biomeIndex).ToLower()
                    });
                    biomeIndex++;
                }
                
                Debug.Log($"Detected {detectedBiomes.Count} biomes from color mask.");
            }
            finally
            {
                // Restore original readable state
                if (!wasReadable)
                {
                    importer.isReadable = false;
                    AssetDatabase.ImportAsset(path);
                }
            }
        }
        
        private void DetectBiomesFromJSON(TextAsset jsonAsset)
        {
            try
            {
                var jsonData = JsonUtility.FromJson<BiomeMapData>(jsonAsset.text);
                if (jsonData != null && jsonData.biomes != null)
                {
                    detectedBiomes.Clear();
                    foreach (var biomeDef in jsonData.biomes)
                    {
                        var biomeMapping = new BiomeMapping
                        {
                            biomeName = biomeDef.name,
                            maskColor = ParseColorFromHex(biomeDef.color),
                            includeInExport = true,
                            exportFolderSuffix = biomeDef.name.ToLower().Replace(" ", "_")
                        };
                        detectedBiomes.Add(biomeMapping);
                    }
                    Debug.Log($"Loaded {detectedBiomes.Count} biomes from JSON mapping.");
                }
                else
                {
                    Debug.LogWarning("Invalid JSON format. Expected format: {\"biomes\": [{\"name\": \"Forest\", \"color\": \"#00FF00\"}]}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse JSON biome mapping: {e.Message}");
            }
        }
        
        private Color ParseColorFromHex(string hexColor)
        {
            if (string.IsNullOrEmpty(hexColor)) return Color.white;
            
            if (hexColor.StartsWith("#"))
                hexColor = hexColor.Substring(1);
            
            if (hexColor.Length == 6)
            {
                if (int.TryParse(hexColor.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out int r) &&
                    int.TryParse(hexColor.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out int g) &&
                    int.TryParse(hexColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out int b))
                {
                    return new Color(r / 255f, g / 255f, b / 255f, 1f);
                }
            }
            
            Debug.LogWarning($"Invalid hex color format: {hexColor}. Using white as fallback.");
            return Color.white;
        }
        
        private void AutoDetectBiomeRegions()
        {
            if (spritesheets.Count == 0)
            {
                Debug.LogWarning("No spritesheets selected for auto-detection.");
                return;
            }
            
            var firstSpritesheet = spritesheets[0];
            string path = AssetDatabase.GetAssetPath(firstSpritesheet);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            
            bool wasReadable = importer.isReadable;
            if (!wasReadable)
            {
                importer.isReadable = true;
                AssetDatabase.ImportAsset(path);
            }
            
            try
            {
                detectedBiomes.Clear();
                var uniqueColors = new HashSet<Color>();
                
                // Sample colors from grid cells
                int cols = firstSpritesheet.width / cellSize.x;
                int rows = firstSpritesheet.height / cellSize.y;
                
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        int sampleX = col * cellSize.x + cellSize.x / 2;
                        int sampleY = row * cellSize.y + cellSize.y / 2;
                        
                        if (sampleX < firstSpritesheet.width && sampleY < firstSpritesheet.height)
                        {
                            Color cellColor = firstSpritesheet.GetPixel(sampleX, sampleY);
                            
                            // Skip transparent pixels
                            if (cellColor.a > 0.1f)
                            {
                                // Quantize colors to reduce variation
                                var quantized = QuantizeColor(cellColor);
                                uniqueColors.Add(quantized);
                            }
                        }
                    }
                }
                
                // Create biome mappings from detected colors
                int biomeIndex = 0;
                foreach (var color in uniqueColors)
                {
                    detectedBiomes.Add(new BiomeMapping
                    {
                        biomeName = GetBiomeNameFromColor(color, biomeIndex),
                        maskColor = color,
                        includeInExport = true,
                        exportFolderSuffix = GetBiomeNameFromColor(color, biomeIndex).ToLower()
                    });
                    biomeIndex++;
                }
                
                Debug.Log($"Auto-detected {detectedBiomes.Count} biome regions from spritesheet colors.");
            }
            finally
            {
                if (!wasReadable)
                {
                    importer.isReadable = false;
                    AssetDatabase.ImportAsset(path);
                }
            }
        }
        
        private Color QuantizeColor(Color color)
        {
            // Quantize to 16 levels per channel to group similar colors
            float quantum = 16f;
            return new Color(
                Mathf.Round(color.r * quantum) / quantum,
                Mathf.Round(color.g * quantum) / quantum,
                Mathf.Round(color.b * quantum) / quantum,
                1f
            );
        }
        
        private string GetBiomeNameFromColor(Color color, int index)
        {
            // Simple color-to-biome mapping
            if (color.r > 0.8f && color.g < 0.3f && color.b < 0.3f) return "Volcanic";
            if (color.g > 0.8f && color.r < 0.5f && color.b < 0.5f) return "Forest";
            if (color.b > 0.8f && color.r < 0.5f && color.g < 0.5f) return "Ocean";
            if (color.r > 0.8f && color.g > 0.8f && color.b < 0.3f) return "Desert";
            if (color.r > 0.6f && color.g > 0.4f && color.b > 0.8f) return "Crystal";
            
            return $"Biome_{index:D2}";
        }
        
        private void RunPreFlightValidation()
        {
            validationWarnings.Clear();
            
            // Check if spritesheets are selected
            if (spritesheets.Count == 0)
            {
                validationWarnings.Add("No spritesheets selected.");
                return;
            }
            
            // Check if biomes are detected
            if (detectedBiomes.Count == 0)
            {
                validationWarnings.Add("No biomes detected. Please assign a biome mask or run auto-detection.");
            }
            
            // Check if any biomes are selected for export
            if (detectedBiomes.Count > 0 && !detectedBiomes.Any(b => b.includeInExport))
            {
                validationWarnings.Add("No biomes selected for export. Enable at least one biome in the biome list.");
            }
            
            // Check cell size consistency
            if (cellSize.x <= 0 || cellSize.y <= 0)
            {
                validationWarnings.Add("Invalid cell size. Width and height must be greater than 0.");
            }
            
            // Check spritesheet dimensions against cell size
            foreach (var sheet in spritesheets)
            {
                if (sheet.width % cellSize.x != 0 || sheet.height % cellSize.y != 0)
                {
                    validationWarnings.Add($"Spritesheet '{sheet.name}' dimensions ({sheet.width}x{sheet.height}) " +
                                         $"are not evenly divisible by cell size ({cellSize.x}x{cellSize.y}).");
                }
                
                // Check for extremely small or large cell sizes
                if (cellSize.x > sheet.width || cellSize.y > sheet.height)
                {
                    validationWarnings.Add($"Cell size ({cellSize.x}x{cellSize.y}) is larger than spritesheet '{sheet.name}' ({sheet.width}x{sheet.height}).");
                }
            }
            
            // Check output folder
            if (string.IsNullOrEmpty(outputFolderPath))
            {
                validationWarnings.Add("Output folder path is empty.");
            }
            else if (!outputFolderPath.StartsWith("Assets/"))
            {
                validationWarnings.Add("Output folder must be within the Assets folder.");
            }
            
            // Check file naming pattern
            if (string.IsNullOrEmpty(fileNamingPattern))
            {
                validationWarnings.Add("File naming pattern is empty.");
            }
            else if (!fileNamingPattern.Contains("{biome}"))
            {
                validationWarnings.Add("File naming pattern should include {biome} placeholder for proper organization.");
            }
            
            // Check biome mask dimensions if provided
            if (biomeMaskAsset is Texture2D maskTexture && spritesheets.Count > 0)
            {
                var firstSheet = spritesheets[0];
                if (maskTexture.width != firstSheet.width || maskTexture.height != firstSheet.height)
                {
                    validationWarnings.Add($"Biome mask dimensions ({maskTexture.width}x{maskTexture.height}) " +
                                         $"don't match spritesheet dimensions ({firstSheet.width}x{firstSheet.height}).");
                }
            }
            
            // Check for duplicate biome names
            var biomeNames = detectedBiomes.Where(b => b.includeInExport).Select(b => b.biomeName).ToList();
            var duplicates = biomeNames.GroupBy(name => name).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var duplicate in duplicates)
            {
                validationWarnings.Add($"Duplicate biome name detected: '{duplicate}'. Each biome must have a unique name.");
            }
            
            if (validationWarnings.Count == 0)
            {
                Debug.Log("✓ Pre-flight validation passed.");
            }
            else
            {
                Debug.LogWarning($"Pre-flight validation found {validationWarnings.Count} issues.");
            }
        }
        
        private void StartBiomeExtraction()
        {
            if (validationWarnings.Count > 0)
            {
                if (!EditorUtility.DisplayDialog("Validation Warnings", 
                    $"Found {validationWarnings.Count} validation issues. Continue anyway?", 
                    "Continue", "Cancel"))
                {
                    return;
                }
            }
            
            isProcessing = true;
            processProgress = 0f;
            
            try
            {
                ProcessBiomeExtraction();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Biome extraction failed: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Biome extraction failed:\n{e.Message}", "OK");
            }
            finally
            {
                isProcessing = false;
                processProgress = 0f;
                currentProcessingBiome = "";
            }
        }
        
        private void ProcessBiomeExtraction()
        {
            
            var biomesToProcess = detectedBiomes.Where(b => b.includeInExport).ToList();
            float totalSteps = biomesToProcess.Count * spritesheets.Count;
            float currentStep = 0f;
            
            foreach (var biome in biomesToProcess)
            {
                currentProcessingBiome = biome.biomeName;
                
                foreach (var spritesheet in spritesheets)
                {
                    ProcessSpritesheetForBiome(spritesheet, biome);
                    
                    currentStep++;
                    processProgress = currentStep / totalSteps;
                    
                    // Allow UI updates
                    if (currentStep % 10 == 0)
                    {
                        EditorUtility.DisplayProgressBar("Biome Extraction", 
                            $"Processing {biome.biomeName}...", processProgress);
                    }
                }
            }
            
            EditorUtility.ClearProgressBar();
            Debug.Log($"✓ Biome extraction completed. Processed {biomesToProcess.Count} biomes across {spritesheets.Count} spritesheets.");
        }
        
        private void ProcessSpritesheetForBiome(Texture2D spritesheet, BiomeMapping biome)
        {
            string spritesheetPath = AssetDatabase.GetAssetPath(spritesheet);
            TextureImporter importer = AssetImporter.GetAtPath(spritesheetPath) as TextureImporter;
            
            if (importer == null)
            {
                Debug.LogWarning($"Cannot process '{spritesheet.name}' - texture importer not found.");
                return;
            }
            
            // Create biome-specific output folder
            string biomeFolder = Path.Combine(outputFolderPath, biome.exportFolderSuffix);
            if (!AssetDatabase.IsValidFolder(biomeFolder))
            {
                // Ensure parent folders exist
                string parentFolder = outputFolderPath;
                if (!AssetDatabase.IsValidFolder(parentFolder))
                {
                    // Create parent folder structure if it doesn't exist
                    string[] pathParts = parentFolder.Replace("Assets/", "").Split('/');
                    string currentPath = "Assets";
                    foreach (string part in pathParts)
                    {
                        string newPath = Path.Combine(currentPath, part);
                        if (!AssetDatabase.IsValidFolder(newPath))
                        {
                            AssetDatabase.CreateFolder(currentPath, part);
                        }
                        currentPath = newPath;
                    }
                }
                
                string folderName = Path.GetFileName(biomeFolder);
                AssetDatabase.CreateFolder(parentFolder, folderName);
                AssetDatabase.Refresh();
            }
            
            // Get or create mask texture for biome filtering
            Texture2D maskTexture = biomeMaskAsset as Texture2D;
            if (maskTexture == null)
            {
                Debug.LogWarning($"No biome mask available for filtering. Skipping {spritesheet.name}.");
                return;
            }
            
            // Slice sprites using biome mask filtering
            SliceSpritesheetWithBiomeFilter(spritesheet, maskTexture, biome, biomeFolder);
        }
        
        private void SliceSpritesheetWithBiomeFilter(Texture2D spritesheet, Texture2D maskTexture, BiomeMapping biome, string outputFolder)
        {
            string spritesheetPath = AssetDatabase.GetAssetPath(spritesheet);
            TextureImporter importer = AssetImporter.GetAtPath(spritesheetPath) as TextureImporter;
            
            // Ensure textures are readable
            bool spritesheetWasReadable = importer.isReadable;
            if (!spritesheetWasReadable)
            {
                importer.isReadable = true;
                AssetDatabase.ImportAsset(spritesheetPath);
            }
            
            string maskPath = AssetDatabase.GetAssetPath(maskTexture);
            TextureImporter maskImporter = AssetImporter.GetAtPath(maskPath) as TextureImporter;
            bool maskWasReadable = maskImporter.isReadable;
            if (!maskWasReadable)
            {
                maskImporter.isReadable = true;
                AssetDatabase.ImportAsset(maskPath);
            }
            
            try
            {
                // Calculate grid dimensions
                int columns = spritesheet.width / cellSize.x;
                int rows = spritesheet.height / cellSize.y;
                
                List<SpriteRect> biomeSprites = new();
                
                // Process each cell in the grid
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < columns; col++)
                    {
                        // Calculate cell position in mask
                        int maskX = col * cellSize.x + cellSize.x / 2; // Sample from center of cell
                        int maskY = row * cellSize.y + cellSize.y / 2;
                        
                        // Check if this cell belongs to the current biome
                        if (IsCellInBiome(maskTexture, maskX, maskY, biome.maskColor))
                        {
                            // Create sprite rect for this cell
                            int rectX = col * cellSize.x;
                            int rectY = spritesheet.height - ((row + 1) * cellSize.y); // Flip Y coordinate
                            
                            Rect cellRect = new Rect(rectX, rectY, cellSize.x, cellSize.y);
                            
                            // Generate sprite name using pattern
                            string spriteName = GenerateSpriteName(biome.biomeName, row, col);
                            
                            SpriteRect spriteRect = new SpriteRect
                            {
                                name = spriteName,
                                rect = cellRect,
                                alignment = pivotAlignment,
                                pivot = GetPivotForAlignment(pivotAlignment)
                            };
                            
                            biomeSprites.Add(spriteRect);
                        }
                    }
                }
                
                // Apply sprites to a duplicate texture for this biome
                if (biomeSprites.Count > 0)
                {
                    CreateBiomeSpecificTexture(spritesheet, biomeSprites, biome, outputFolder);
                }
                else
                {
                    Debug.LogWarning($"No sprites found for biome '{biome.biomeName}' in spritesheet '{spritesheet.name}'.");
                }
            }
            finally
            {
                // Restore original readable states
                if (!spritesheetWasReadable)
                {
                    importer.isReadable = false;
                    AssetDatabase.ImportAsset(spritesheetPath);
                }
                if (!maskWasReadable)
                {
                    maskImporter.isReadable = false;
                    AssetDatabase.ImportAsset(maskPath);
                }
            }
        }
        
        private bool IsCellInBiome(Texture2D maskTexture, int x, int y, Color biomeColor)
        {
            // Clamp coordinates to texture bounds
            x = Mathf.Clamp(x, 0, maskTexture.width - 1);
            y = Mathf.Clamp(y, 0, maskTexture.height - 1);
            
            Color maskPixel = maskTexture.GetPixel(x, y);
            
            // Compare colors with tolerance for slight variations
            return Mathf.Abs(maskPixel.r - biomeColor.r) < BiomeRegionExtractor.ColorComparisonTolerance &&
                   Mathf.Abs(maskPixel.g - biomeColor.g) < BiomeRegionExtractor.ColorComparisonTolerance &&
                   Mathf.Abs(maskPixel.b - biomeColor.b) < BiomeRegionExtractor.ColorComparisonTolerance;
        }
        
        private string GenerateSpriteName(string biomeName, int row, int col)
        {
            return fileNamingPattern
                .Replace("{biome}", biomeName)
                .Replace("{row}", row.ToString())
                .Replace("{col}", col.ToString());
        }
        
        private void CreateBiomeSpecificTexture(Texture2D sourceTexture, List<SpriteRect> sprites, BiomeMapping biome, string outputFolder)
        {
            // Create a copy of the source texture for this biome
            string sourceTexturePath = AssetDatabase.GetAssetPath(sourceTexture);
            string biomeName = biome.biomeName.Replace(" ", "");
            string newTextureName = $"{sourceTexture.name}_{biomeName}.png";
            string newTexturePath = Path.Combine(outputFolder, newTextureName);
            
            // Copy the original texture
            AssetDatabase.CopyAsset(sourceTexturePath, newTexturePath);
            AssetDatabase.Refresh();
            
            // Configure the new texture with biome-specific sprites
            TextureImporter newImporter = AssetImporter.GetAtPath(newTexturePath) as TextureImporter;
            if (newImporter != null)
            {
                // Set up sprite import settings
                newImporter.textureType = TextureImporterType.Sprite;
                newImporter.spriteImportMode = SpriteImportMode.Multiple;
                
                // Use sprite editor data provider to set sprite rects
                var factory = new SpriteDataProviderFactories();
                factory.Init();
                var dataProvider = factory.GetSpriteEditorDataProviderFromObject(newImporter);
                dataProvider.InitSpriteEditorDataProvider();
                
                // Set the sprite rectangles
                dataProvider.SetSpriteRects(sprites.ToArray());
                
                // Embed biome metadata if requested
                if (embedBiomeMetadata)
                {
                    EmbedBiomeMetadata(newImporter, biome);
                }
                
                dataProvider.Apply();
                AssetDatabase.ImportAsset(newTexturePath, ImportAssetOptions.ForceUpdate);
                
                Debug.Log($"Created biome texture '{newTextureName}' with {sprites.Count} sprites for biome '{biome.biomeName}'.");
            }
        }
        
        private void EmbedBiomeMetadata(TextureImporter importer, BiomeMapping biome)
        {
            // Store biome information in the texture's user data for ECS integration
            var biomeMetadata = new BiomeMetadata
            {
                biomeName = biome.biomeName,
                biomeColor = biome.maskColor
            };
            
            string jsonMetadata = JsonUtility.ToJson(biomeMetadata);
            importer.userData = jsonMetadata;
        }
        
        /// <summary>
        /// Gets the pivot vector for the specified sprite alignment.
        /// Reused from BatchSpriteSlicer for consistency.
        /// </summary>
        private Vector2 GetPivotForAlignment(SpriteAlignment alignment)
        {
            return alignment switch
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
        
        #endregion
        
        #region Data Structures
        
        /// <summary>
        /// Metadata structure for biome information storage in sprite assets.
        /// </summary>
        [System.Serializable]
        public class BiomeMetadata
        {
            public string biomeName;
            public Color biomeColor;
        }
        
        /// <summary>
        /// JSON data structure for biome mapping files.
        /// </summary>
        [System.Serializable]
        public class BiomeMapData
        {
            public BiomeDefinition[] biomes;
        }
        
        [System.Serializable]
        public class BiomeDefinition
        {
            public string name;
            public string color;
        }
        
        #endregion
    }
}
#endif