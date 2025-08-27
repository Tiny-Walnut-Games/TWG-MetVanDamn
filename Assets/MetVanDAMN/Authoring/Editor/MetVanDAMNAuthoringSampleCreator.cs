#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;

namespace TinyWalnutGames.MetVD.Authoring.Editor
{
    /// <summary>
    /// Creates the MetVanDAMN Authoring Sample scene as specified in the art pass issue
    /// This scene demonstrates the complete authoring workflow without custom bootstrappers
    /// </summary>
    public static class MetVanDAMNAuthoringSampleCreator
    {
        [MenuItem("Tools/MetVanDAMN/Create Authoring Sample Scene")]
        public static void CreateAuthoringSampleScene()
        {
            // Create new scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            CreateWorldConfiguration();
            CreateSampleDistricts();
            CreateSampleConnections();
            CreateSampleBiomeFields();
            CreateWfcTilePrototypeLibrary();
            CreateSampleCamera();
            CreateLighting();
            
            // Save the scene
            string scenePath = "Assets/Scenes/MetVanDAMN_AuthoringSample.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);
            
            Debug.Log($"MetVanDAMN Authoring Sample scene created at: {scenePath}");
            Debug.Log("This scene can be played directly without custom bootstrappers!");
        }
        
        private static void CreateWorldConfiguration()
        {
            var worldConfigGO = new GameObject("WorldConfiguration");
            var worldConfig = worldConfigGO.AddComponent<WorldConfigurationAuthoring>();
            
            // Configure with sensible defaults for the sample
            // Note: Assuming WorldConfigurationAuthoring has typical world setup fields
            
            Debug.Log("Created WorldConfiguration");
        }
        
        private static void CreateSampleDistricts()
        {
            var districtsParent = new GameObject("Districts");
            
            // Create a 3x3 grid of districts for a comprehensive sample
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    var districtGO = new GameObject($"District_{x + 1}_{z + 1}");
                    districtGO.transform.SetParent(districtsParent.transform);
                    districtGO.transform.position = new Vector3(x * 5f, 0, z * 5f);
                    
                    var district = districtGO.AddComponent<DistrictAuthoring>();
                    district.nodeId = (uint)((x + 1) * 3 + (z + 1) + 1); // Unique IDs 1-9
                    district.level = 0;
                    district.parentId = 0;
                    district.gridCoordinates = new int2(x, z);
                    district.targetLoopDensity = 0.3f + (x + z) * 0.1f; // Vary density slightly
                    district.initialWfcState = WfcGenerationState.Initialized;
                    
                    // Add visual representation
                    CreateDistrictVisual(districtGO, x, z);
                }
            }
            
            Debug.Log("Created 9 sample districts in 3x3 grid");
        }
        
        private static void CreateDistrictVisual(GameObject parent, int x, int z)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(parent.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(1.5f, 0.2f, 1.5f);
            
            var renderer = visual.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            
            // Color code by position for easy identification
            float hue = (x + z + 2f) / 4f; // Normalize to 0-1 range
            renderer.material.color = Color.HSVToRGB(hue, 0.7f, 0.8f);
        }
        
        private static void CreateSampleConnections()
        {
            var connectionsParent = new GameObject("Connections");
            
            // Find all district authoring components
            var districts = UnityEngine.Object.FindObjectsOfType<DistrictAuthoring>();
            
            // Create connections between adjacent districts
            foreach (var district1 in districts)
            {
                foreach (var district2 in districts)
                {
                    if (district1 == district2) continue;
                    
                    // Check if districts are adjacent (distance of 1 in grid coordinates)
                    var dist = math.abs(district1.gridCoordinates.x - district2.gridCoordinates.x) +
                              math.abs(district1.gridCoordinates.y - district2.gridCoordinates.y);
                    
                    if (dist == 1 && district1.nodeId < district2.nodeId) // Avoid duplicate connections
                    {
                        var connectionGO = new GameObject($"Connection_{district1.nodeId}_{district2.nodeId}");
                        connectionGO.transform.SetParent(connectionsParent.transform);
                        
                        // Position connection between districts
                        var pos1 = district1.transform.position;
                        var pos2 = district2.transform.position;
                        connectionGO.transform.position = (pos1 + pos2) * 0.5f + Vector3.up * 0.5f;
                        
                        var connection = connectionGO.AddComponent<ConnectionAuthoring>();
                        connection.from = district1;
                        connection.to = district2;
                        connection.type = ConnectionType.Bidirectional;
                        connection.requiredPolarity = Polarity.None;
                        connection.traversalCost = 1.0f;
                        
                        // Add visual representation
                        CreateConnectionVisual(connectionGO, pos1, pos2);
                    }
                }
            }
            
            Debug.Log("Created connections between adjacent districts");
        }
        
        private static void CreateConnectionVisual(GameObject parent, Vector3 from, Vector3 to)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Visual";
            visual.transform.SetParent(parent.transform);
            
            // Orient cylinder to connect the two points
            var direction = (to - from).normalized;
            var distance = Vector3.Distance(from, to);
            
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.1f, distance * 0.5f, 0.1f);
            visual.transform.LookAt(parent.transform.position + direction);
            visual.transform.Rotate(90, 0, 0);
            
            var renderer = visual.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            renderer.material.color = new Color(0.8f, 0.3f, 0.8f);
        }
        
        private static void CreateSampleBiomeFields()
        {
            var biomeFieldsParent = new GameObject("BiomeFields");
            
            // Create a few biome fields with different configurations
            var biomeConfigs = new (Vector3 position, BiomeType primary, BiomeType secondary, float strength, float gradient)[]
            {
                (new Vector3(-3, 1, -3), BiomeType.SolarPlains, BiomeType.Unknown, 1.0f, 0.3f),
                (new Vector3(3, 1, 3), BiomeType.VolcanicCore, BiomeType.Unknown, 0.8f, 0.6f),
                (new Vector3(0, 1, 0), BiomeType.HubArea, BiomeType.TransitionZone, 0.6f, 0.5f),
                (new Vector3(-3, 1, 3), BiomeType.IcyCanyon, BiomeType.Unknown, 0.7f, 0.4f)
            };
            
            for (int i = 0; i < biomeConfigs.Length; i++)
            {
                var config = biomeConfigs[i];
                var biomeFieldGO = new GameObject($"BiomeField_{i + 1}");
                biomeFieldGO.transform.SetParent(biomeFieldsParent.transform);
                biomeFieldGO.transform.position = config.position;
                
                var biomeField = biomeFieldGO.AddComponent<BiomeFieldAuthoring>();
                biomeField.primaryBiome = config.primary;
                biomeField.secondaryBiome = config.secondary;
                biomeField.strength = config.strength;
                biomeField.gradient = config.gradient;
                
                // Add visual representation
                CreateBiomeFieldVisual(biomeFieldGO, config.primary, config.strength);
            }
            
            Debug.Log("Created 4 sample biome fields");
        }
        
        private static void CreateBiomeFieldVisual(GameObject parent, BiomeType biomeType, float strength)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.transform.SetParent(parent.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one * (2f + strength);
            
            var renderer = visual.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            
            // Color code by biome type
            Color biomeColor = biomeType switch
            {
                BiomeType.SolarPlains => Color.yellow,
                BiomeType.VolcanicCore => Color.red,
                BiomeType.HubArea => Color.blue,
                BiomeType.IcyCanyon => Color.cyan,
                BiomeType.TransitionZone => Color.gray,
                _ => Color.white
            };
            
            biomeColor.a = 0.3f; // Make translucent
            renderer.material.color = biomeColor;
            
            // Set up transparency
            renderer.material.SetFloat("_Mode", 2);
            renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            renderer.material.SetInt("_ZWrite", 0);
            renderer.material.DisableKeyword("_ALPHATEST_ON");
            renderer.material.EnableKeyword("_ALPHABLEND_ON");
            renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            renderer.material.renderQueue = 3000;
        }
        
        private static void CreateWfcTilePrototypeLibrary()
        {
            var wfcLibraryParent = new GameObject("WfcTilePrototypeLibrary");
            wfcLibraryParent.transform.position = new Vector3(10, 0, 0); // Offset from main scene
            
            // Create the tile prototypes as defined in the sample data
            var tileConfigs = new (string name, uint id, float weight, BiomeType biome, Polarity polarity, byte minConn, byte maxConn)[]
            {
                ("Hub", 1, 1.0f, BiomeType.HubArea, Polarity.None, 2, 4),
                ("Corridor", 2, 0.8f, BiomeType.TransitionZone, Polarity.None, 2, 2),
                ("Chamber", 3, 0.6f, BiomeType.SolarPlains, Polarity.Sun, 1, 3),
                ("Specialist", 4, 0.4f, BiomeType.VolcanicCore, Polarity.Heat, 1, 2)
            };
            
            for (int i = 0; i < tileConfigs.Length; i++)
            {
                var config = tileConfigs[i];
                var tileGO = new GameObject($"WfcTilePrototype_{config.name}");
                tileGO.transform.SetParent(wfcLibraryParent.transform);
                tileGO.transform.position = new Vector3(i * 2f, 0, 0);
                
                var wfcTile = tileGO.AddComponent<WfcTilePrototypeAuthoring>();
                wfcTile.tileId = config.id;
                wfcTile.weight = config.weight;
                wfcTile.biomeType = config.biome;
                wfcTile.primaryPolarity = config.polarity;
                wfcTile.minConnections = config.minConn;
                wfcTile.maxConnections = config.maxConn;
                
                // Configure sockets (simplified for sample)
                switch (config.name)
                {
                    case "Hub":
                        wfcTile.sockets = new WfcSocketConfig[]
                        {
                            new() { socketId = 1, direction = 0, requiredPolarity = Polarity.None, isOpen = true },
                            new() { socketId = 1, direction = 1, requiredPolarity = Polarity.None, isOpen = true },
                            new() { socketId = 1, direction = 2, requiredPolarity = Polarity.None, isOpen = true },
                            new() { socketId = 1, direction = 3, requiredPolarity = Polarity.None, isOpen = true }
                        };
                        break;
                    case "Corridor":
                        wfcTile.sockets = new WfcSocketConfig[]
                        {
                            new() { socketId = 1, direction = 0, requiredPolarity = Polarity.None, isOpen = true },
                            new() { socketId = 1, direction = 2, requiredPolarity = Polarity.None, isOpen = true }
                        };
                        break;
                    default:
                        // Standard configuration for other types
                        wfcTile.sockets = new WfcSocketConfig[]
                        {
                            new() { socketId = 1, direction = 0, requiredPolarity = Polarity.None, isOpen = true },
                            new() { socketId = 1, direction = 1, requiredPolarity = Polarity.None, isOpen = true }
                        };
                        break;
                }
                
                // Add visual representation
                CreateWfcTileVisual(tileGO, config.name, config.biome);
            }
            
            Debug.Log("Created WFC tile prototype library with 4 tile types");
        }
        
        private static void CreateWfcTileVisual(GameObject parent, string typeName, BiomeType biomeType)
        {
            GameObject visual = typeName switch
            {
                "Hub" => GameObject.CreatePrimitive(PrimitiveType.Sphere),
                "Corridor" => GameObject.CreatePrimitive(PrimitiveType.Capsule),
                "Chamber" => GameObject.CreatePrimitive(PrimitiveType.Cube),
                "Specialist" => GameObject.CreatePrimitive(PrimitiveType.Cylinder),
                _ => GameObject.CreatePrimitive(PrimitiveType.Cube)
            };
            
            visual.name = "Visual";
            visual.transform.SetParent(parent.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one * 0.8f;
            
            var renderer = visual.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            
            // Color by type
            Color typeColor = typeName switch
            {
                "Hub" => Color.blue,
                "Corridor" => Color.gray,
                "Chamber" => Color.yellow,
                "Specialist" => Color.red,
                _ => Color.white
            };
            
            renderer.material.color = typeColor;
        }
        
        private static void CreateSampleCamera()
        {
            var cameraGO = new GameObject("Sample Camera");
            cameraGO.transform.position = new Vector3(0, 8, -8);
            cameraGO.transform.LookAt(Vector3.zero);
            
            var camera = cameraGO.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000f;
            
            // Add audio listener
            cameraGO.AddComponent<AudioListener>();
            
            Debug.Log("Created sample camera");
        }
        
        private static void CreateLighting()
        {
            // Create directional light
            var lightGO = new GameObject("Directional Light");
            lightGO.transform.rotation = Quaternion.Euler(30f, 30f, 0f);
            
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1f;
            
            // Set up production lighting settings
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.212f, 0.227f, 0.259f);
            RenderSettings.ambientEquatorColor = new Color(0.114f, 0.125f, 0.133f);
            RenderSettings.ambientGroundColor = new Color(0.047f, 0.043f, 0.035f);
            
            Debug.Log("Created lighting setup");
        }
    }
}
#endif