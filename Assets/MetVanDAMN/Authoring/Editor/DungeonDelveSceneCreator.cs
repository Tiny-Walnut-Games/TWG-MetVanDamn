using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Samples;
using TinyWalnutGames.MetVD.Authoring;
using TinyWalnutGames.MetVanDAMN.Authoring;

namespace TinyWalnutGames.MetVanDAMN.Authoring.Editor
{
    /// <summary>
    /// Editor menu for creating complete Dungeon Delve Mode demo scene.
    /// Creates a fully functional scene with all required components for immediate testing.
    /// Compliant with MetVanDAMN mandate: complete, working defaults with no setup required.
    /// </summary>
    public static class DungeonDelveSceneCreator
    {
        private const string SceneName = "DungeonDelveMode_Demo";
        private const string ScenesFolder = "Assets/Scenes";
        
        [MenuItem("MetVanDAMN/Create Dungeon Delve Demo Scene")]
        public static void CreateDungeonDelveScene()
        {
            Debug.Log("🏰 Creating Dungeon Delve Mode demo scene...");
            
            // Create new scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            newScene.name = SceneName;
            
            // Create all required components
            CreateDungeonDelveSystem();
            CreatePlayerSystem();
            CreateCameraSystem();
            CreateUISystem();
            CreateValidationSystem();
            CreateWorldGenerationSystem();
            
            // Save scene
            EnsureFolder(ScenesFolder);
            string scenePath = $"{ScenesFolder}/{SceneName}.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);
            
            Debug.Log($"✅ Dungeon Delve Mode demo scene created at: {scenePath}");
            Debug.Log("🎮 Press Play to experience the complete dungeon delve adventure!");
        }
        
        private static void CreateDungeonDelveSystem()
        {
            // Main Dungeon Delve Mode component
            var dungeonGO = new GameObject("Dungeon Delve Mode");
            var dungeonMode = dungeonGO.AddComponent<DungeonDelveMode>();
            
            // Set initial configuration
            SetDungeonModeDefaults(dungeonMode);
            
            // Main Menu system
            var menuGO = new GameObject("Dungeon Delve Main Menu");
            var mainMenu = menuGO.AddComponent<DungeonDelveMainMenu>();
            
            // Validator system
            var validatorGO = new GameObject("Dungeon Delve Validator");
            var validator = validatorGO.AddComponent<DungeonDelveValidator>();
            
            Debug.Log("🏰 Dungeon Delve core systems created");
        }
        
        private static void SetDungeonModeDefaults(DungeonDelveMode dungeonMode)
        {
            // Use reflection to set private fields with good defaults
            var seedField = typeof(DungeonDelveMode).GetField("dungeonSeed", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (seedField != null)
            {
                seedField.SetValue(dungeonMode, (uint)42); // Classic seed
            }
            
            var floorsField = typeof(DungeonDelveMode).GetField("floorsCount", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (floorsField != null)
            {
                floorsField.SetValue(dungeonMode, 3); // Standard 3 floors
            }
        }
        
        private static void CreatePlayerSystem()
        {
            // Player GameObject
            var playerGO = new GameObject("Player");
            playerGO.transform.position = Vector3.zero;
            
            // Add player components
            var playerMovement = playerGO.AddComponent<DemoPlayerMovement>();
            var playerCombat = playerGO.AddComponent<DemoPlayerCombat>();
            var playerInventory = playerGO.AddComponent<DemoPlayerInventory>();
            
            // Add visual representation
            var renderer = playerGO.AddComponent<MeshRenderer>();
            var meshFilter = playerGO.AddComponent<MeshFilter>();
            meshFilter.mesh = CreatePlayerMesh();
            renderer.material = CreatePlayerMaterial();
            
            // Add collider for interaction
            var collider = playerGO.AddComponent<CapsuleCollider>();
            collider.height = 2f;
            collider.radius = 0.5f;
            
            // Add rigidbody for physics
            var rigidbody = playerGO.AddComponent<Rigidbody>();
            rigidbody.freezeRotation = true;
            
            Debug.Log("👤 Player system created");
        }
        
        private static void CreateCameraSystem()
        {
            // Find main camera or create one
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraGO = new GameObject("Main Camera");
                cameraGO.tag = "MainCamera";
                mainCamera = cameraGO.AddComponent<Camera>();
                cameraGO.AddComponent<AudioListener>();
            }
            
            // Position camera for good view of dungeon
            mainCamera.transform.position = new Vector3(0, 10, -10);
            mainCamera.transform.rotation = Quaternion.Euler(30, 0, 0);
            
            // Add camera controller for dungeon exploration
            var cameraController = mainCamera.gameObject.AddComponent<DungeonCameraController>();
            
            Debug.Log("📷 Camera system created");
        }
        
        private static void CreateUISystem()
        {
            // Create UI Canvas if it doesn't exist
            var existingCanvas = GameObject.FindObjectOfType<Canvas>();
            if (existingCanvas == null)
            {
                var canvasGO = new GameObject("UI Canvas");
                var canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                
                var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(1920, 1080);
                
                canvasGO.AddComponent<GraphicRaycaster>();
            }
            
            // UI will be created automatically by DungeonDelveMainMenu component
            Debug.Log("🖥️ UI system prepared");
        }
        
        private static void CreateValidationSystem()
        {
            // Validation components are already created with the dungeon system
            // Add additional validation helpers if needed
            
            var validationGO = new GameObject("Validation Helpers");
            var validationHelper = validationGO.AddComponent<DungeonValidationHelper>();
            
            Debug.Log("✅ Validation system created");
        }
        
        private static void CreateWorldGenerationSystem()
        {
            // SmokeTestSceneSetup for immediate world generation
            var worldGenGO = new GameObject("World Generation");
            var smokeTest = worldGenGO.AddComponent<SmokeTestSceneSetup>();
            
            // Configure for dungeon delve mode
            ConfigureSmokeTestForDungeon(smokeTest);
            
            // Add ECS Prefab Registry
            var registryGO = new GameObject("ECS Prefab Registry");
            var registry = registryGO.AddComponent<EcsPrefabRegistryAuthoring>();
            ConfigurePrefabRegistry(registry);
            
            // Add AI Manager
            var aiGO = new GameObject("AI Manager");
            var aiManager = aiGO.AddComponent<DemoAIManager>();
            
            // Add Loot Manager
            var lootGO = new GameObject("Loot Manager");
            var lootManager = lootGO.AddComponent<DemoLootManager>();
            
            // Add Map Generator
            var mapGO = new GameObject("Map Generator");
            var mapGenerator = mapGO.AddComponent<MetVanDAMNMapGenerator>();
            
            Debug.Log("🗺️ World generation system created");
        }
        
        private static void ConfigureSmokeTestForDungeon(SmokeTestSceneSetup smokeTest)
        {
            // Use reflection to configure smoke test for dungeon mode
            var seedField = typeof(SmokeTestSceneSetup).GetField("worldSeed", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (seedField != null)
            {
                seedField.SetValue(smokeTest, (uint)42);
            }
            
            var sizeField = typeof(SmokeTestSceneSetup).GetField("worldSize", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (sizeField != null)
            {
                sizeField.SetValue(smokeTest, new Unity.Mathematics.int2(50, 50));
            }
            
            var sectorsField = typeof(SmokeTestSceneSetup).GetField("targetSectorCount", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (sectorsField != null)
            {
                sectorsField.SetValue(smokeTest, 5);
            }
            
            var debugField = typeof(SmokeTestSceneSetup).GetField("enableDebugVisualization", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (debugField != null)
            {
                debugField.SetValue(smokeTest, true);
            }
        }
        
        private static void ConfigurePrefabRegistry(EcsPrefabRegistryAuthoring registry)
        {
            registry.Entries = new System.Collections.Generic.List<EcsPrefabRegistryAuthoring.Entry>
            {
                new() { Key = "spawn_boss" },
                new() { Key = "spawn_enemy_melee" },
                new() { Key = "spawn_enemy_ranged" },
                new() { Key = "pickup_health" },
                new() { Key = "pickup_mana" },
                new() { Key = "pickup_coin" },
                new() { Key = "pickup_weapon" },
                new() { Key = "pickup_consumable" },
                new() { Key = "spawn_door_locked" },
                new() { Key = "spawn_door_timed" },
                new() { Key = "secret_crystal" },
                new() { Key = "secret_flame" },
                new() { Key = "secret_void" }
            };
        }
        
        private static Mesh CreatePlayerMesh()
        {
            // Create simple capsule mesh for player
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            // Simple capsule approximation
            int segments = 8;
            float radius = 0.5f;
            float height = 2f;
            
            // Bottom circle
            vertices.Add(new Vector3(0, 0, 0)); // Center
            for (int i = 0; i < segments; i++)
            {
                float angle = i * 2 * Mathf.PI / segments;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius));
            }
            
            // Top circle
            vertices.Add(new Vector3(0, height, 0)); // Center
            for (int i = 0; i < segments; i++)
            {
                float angle = i * 2 * Mathf.PI / segments;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius));
            }
            
            // Bottom triangles
            for (int i = 0; i < segments; i++)
            {
                triangles.Add(0);
                triangles.Add(1 + i);
                triangles.Add(1 + (i + 1) % segments);
            }
            
            // Top triangles
            int topCenter = segments + 1;
            for (int i = 0; i < segments; i++)
            {
                triangles.Add(topCenter);
                triangles.Add(topCenter + 1 + (i + 1) % segments);
                triangles.Add(topCenter + 1 + i);
            }
            
            // Side triangles
            for (int i = 0; i < segments; i++)
            {
                int bottom1 = 1 + i;
                int bottom2 = 1 + (i + 1) % segments;
                int top1 = topCenter + 1 + i;
                int top2 = topCenter + 1 + (i + 1) % segments;
                
                // Triangle 1
                triangles.Add(bottom1);
                triangles.Add(top1);
                triangles.Add(bottom2);
                
                // Triangle 2
                triangles.Add(bottom2);
                triangles.Add(top1);
                triangles.Add(top2);
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.name = "PlayerMesh";
            
            return mesh;
        }
        
        private static Material CreatePlayerMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = Color.blue;
            material.name = "PlayerMaterial";
            return material;
        }
        
        private static void EnsureFolder(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parentFolder = System.IO.Path.GetDirectoryName(folderPath);
                string folderName = System.IO.Path.GetFileName(folderPath);
                
                if (!AssetDatabase.IsValidFolder(parentFolder))
                {
                    EnsureFolder(parentFolder);
                }
                
                AssetDatabase.CreateFolder(parentFolder, folderName);
                AssetDatabase.Refresh();
            }
        }
    }
    
    /// <summary>
    /// Simple camera controller for dungeon exploration
    /// </summary>
    public class DungeonCameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private float rotationSpeed = 2f;
        [SerializeField] private Vector3 offset = new Vector3(0, 10, -10);
        
        private Transform playerTransform;
        private Camera cameraComponent;
        
        private void Start()
        {
            cameraComponent = GetComponent<Camera>();
            
            // Find player
            var playerMovement = FindObjectOfType<DemoPlayerMovement>();
            if (playerMovement)
            {
                playerTransform = playerMovement.transform;
            }
        }
        
        private void LateUpdate()
        {
            if (playerTransform == null) return;
            
            // Follow player with offset
            Vector3 targetPosition = playerTransform.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
            
            // Look at player
            Vector3 lookDirection = playerTransform.position - transform.position;
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
    
    /// <summary>
    /// Helper component for validation and debugging
    /// </summary>
    public class DungeonValidationHelper : MonoBehaviour
    {
        [Header("Validation Controls")]
        [SerializeField] private bool runValidationOnStart = true;
        [SerializeField] private bool showValidationUI = true;
        
        private DungeonDelveValidator validator;
        private ValidationReport lastReport;
        
        private void Start()
        {
            validator = FindObjectOfType<DungeonDelveValidator>();
            
            if (runValidationOnStart && validator)
            {
                lastReport = validator.ValidateAll();
            }
        }
        
        private void OnGUI()
        {
            if (!showValidationUI || lastReport == null) return;
            
            // Simple validation status display
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("🔍 Validation Status", EditorStyles.boldLabel);
            
            string statusText = lastReport.overallPassed ? "✅ PASSED" : "❌ FAILED";
            GUILayout.Label($"Overall: {statusText}");
            GUILayout.Label($"Passed: {lastReport.passedChecks.Count}");
            GUILayout.Label($"Failed: {lastReport.failedChecks.Count}");
            GUILayout.Label($"Time: {lastReport.totalValidationTime:F2}s");
            
            if (GUILayout.Button("Run Validation"))
            {
                if (validator)
                {
                    lastReport = validator.ValidateAll();
                }
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}