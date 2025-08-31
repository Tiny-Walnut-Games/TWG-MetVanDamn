#if UNITY_EDITOR
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring.Editor
	{
	/// <summary>
	/// Utility for creating sample prefabs for MetVanDAMN authoring workflow
	/// This replaces manual prefab creation as mentioned in the art pass issue
	/// </summary>
	public static class MetVanDAMNSamplePrefabCreator
		{
		private const string PrefabPath = "Assets/MetVanDAMN/Prefabs/Samples/";

		[MenuItem("Tiny Walnut Games/MetVanDAMN/Sample Creation/Create Sample Prefabs")]
		public static void CreateAllSamplePrefabs ()
			{
			// Ensure directory exists
			if (!AssetDatabase.IsValidFolder("Assets/MetVanDAMN/Prefabs"))
				{
				AssetDatabase.CreateFolder("Assets/MetVanDAMN", "Prefabs");
				}

			if (!AssetDatabase.IsValidFolder("Assets/MetVanDAMN/Prefabs/Samples"))
				{
				AssetDatabase.CreateFolder("Assets/MetVanDAMN/Prefabs", "Samples");
				}

			CreateDistrictPrefab();
			CreateConnectionAnchorPrefab();
			CreateBiomeFieldPrefab();
			CreateWfcTilePrototypePrefabs();

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			Debug.Log("MetVanDAMN sample prefabs created successfully!");
			}

		private static void CreateDistrictPrefab ()
			{
			var go = new GameObject("District_Sample");

			// Add district authoring component
			DistrictAuthoring district = go.AddComponent<DistrictAuthoring>();
			district.nodeId = 1;
			district.level = 0;
			district.parentId = 0;
			district.gridCoordinates = new int2(0, 0);
			district.targetLoopDensity = 0.3f;
			district.initialWfcState = WfcGenerationState.Initialized;

			// Add visual representation
			var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
			cube.transform.SetParent(go.transform);
			cube.transform.localPosition = Vector3.zero;
			cube.transform.localScale = Vector3.one * 0.8f;

			// Color coding for districts
			Renderer renderer = cube.GetComponent<Renderer>();
			renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
				{
				color = new Color(0.2f, 0.8f, 0.2f, 0.8f) // Green
				};

			SavePrefab(go, "District_Sample.prefab");
			}

		private static void CreateConnectionAnchorPrefab ()
			{
			var go = new GameObject("ConnectionAnchor_Sample");

			// Add connection authoring component (will be configured in scene)
			ConnectionAuthoring connection = go.AddComponent<ConnectionAuthoring>();
			connection.type = ConnectionType.Bidirectional;
			connection.requiredPolarity = Polarity.None;
			connection.traversalCost = 1.0f;

			// Add visual representation - a cylinder for connection visualization
			var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			cylinder.transform.SetParent(go.transform);
			cylinder.transform.localPosition = Vector3.zero;
			cylinder.transform.localScale = new Vector3(0.1f, 0.5f, 0.1f);

			// Color coding for connections
			Renderer renderer = cylinder.GetComponent<Renderer>();
			renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
				{
				color = new Color(0.8f, 0.2f, 0.8f, 0.9f) // Magenta
				};

			SavePrefab(go, "ConnectionAnchor_Sample.prefab");
			}

		private static void CreateBiomeFieldPrefab ()
			{
			var go = new GameObject("BiomeField_Sample");

			// Add biome field authoring component
			BiomeFieldAuthoring biomeField = go.AddComponent<BiomeFieldAuthoring>();
			biomeField.primaryBiome = BiomeType.SolarPlains;
			biomeField.secondaryBiome = BiomeType.Unknown;
			biomeField.strength = 1.0f;
			biomeField.gradient = 0.5f;

			// Add visual representation - a sphere for field visualization
			var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			sphere.transform.SetParent(go.transform);
			sphere.transform.localPosition = Vector3.zero;
			sphere.transform.localScale = Vector3.one * 2.0f; // Larger to show field area

			// 📝 Learning Opportunity Enhanced 📝 - @copilot: @jmeyer1980 discovered that `renderer.material` 
			// leaks materials in editor scripts. HOWEVER, modifying `sharedMaterial` affects ALL objects!
			// For prefabs, we need to create a NEW material instance that won't leak.
			Renderer renderer = sphere.GetComponent<Renderer>();
			var transparentMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
				{
				color = new Color(0.8f, 0.8f, 0.2f, 0.3f) // Yellow, transparent
				};

			// Set up transparency properly on our NEW material
			transparentMaterial.SetFloat("_Mode", 2); // Transparent mode
			transparentMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			transparentMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			transparentMaterial.SetInt("_ZWrite", 0);
			transparentMaterial.DisableKeyword("_ALPHATEST_ON");
			transparentMaterial.EnableKeyword("_ALPHABLEND_ON");
			transparentMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			transparentMaterial.renderQueue = 3000;

			// Assign our custom material to the renderer
			renderer.material = transparentMaterial; // OK here because we created the material ourselves

			SavePrefab(go, "BiomeField_Sample.prefab");
			}

		private static void CreateWfcTilePrototypePrefabs ()
			{
			// Create different tile prototype samples
			CreateWfcTilePrototype("Hub", 1, 1.0f, BiomeType.HubArea, Polarity.None, 2, 4);
			CreateWfcTilePrototype("Corridor", 2, 0.8f, BiomeType.TransitionZone, Polarity.None, 2, 2);
			CreateWfcTilePrototype("Chamber", 3, 0.6f, BiomeType.SolarPlains, Polarity.Sun, 1, 3);
			CreateWfcTilePrototype("Specialist", 4, 0.4f, BiomeType.VolcanicCore, Polarity.Heat, 1, 2);
			}

		private static void CreateWfcTilePrototype (string name, uint tileId, float weight, BiomeType biomeType, Polarity polarity, byte minConn, byte maxConn)
			{
			var go = new GameObject($"WfcTilePrototype_{name}");

			// Add WFC tile prototype authoring component
			WfcTilePrototypeAuthoring wfcTile = go.AddComponent<WfcTilePrototypeAuthoring>();
			wfcTile.tileId = tileId;
			wfcTile.weight = weight;
			wfcTile.biomeType = biomeType;
			wfcTile.primaryPolarity = polarity;
			wfcTile.minConnections = minConn;
			wfcTile.maxConnections = maxConn;

			// Configure sockets based on tile type
			switch (name)
				{
				case "Hub":
					wfcTile.sockets = new WfcSocketConfig [ ]
					{
						new() { socketId = 1, direction = 0, requiredPolarity = Polarity.None, isOpen = true },
						new() { socketId = 1, direction = 1, requiredPolarity = Polarity.None, isOpen = true },
						new() { socketId = 1, direction = 2, requiredPolarity = Polarity.None, isOpen = true },
						new() { socketId = 1, direction = 3, requiredPolarity = Polarity.None, isOpen = true }
					};
					break;
				case "Corridor":
					wfcTile.sockets = new WfcSocketConfig [ ]
					{
						new() { socketId = 1, direction = 0, requiredPolarity = Polarity.None, isOpen = true },
						new() { socketId = 1, direction = 2, requiredPolarity = Polarity.None, isOpen = true }
					};
					break;
				case "Chamber":
					wfcTile.sockets = new WfcSocketConfig [ ]
					{
						new() { socketId = 2, direction = 0, requiredPolarity = Polarity.Sun, isOpen = true },
						new() { socketId = 1, direction = 1, requiredPolarity = Polarity.None, isOpen = true },
						new() { socketId = 1, direction = 2, requiredPolarity = Polarity.None, isOpen = true }
					};
					break;
				case "Specialist":
					wfcTile.sockets = new WfcSocketConfig [ ]
					{
						new() { socketId = 3, direction = 1, requiredPolarity = Polarity.Heat, isOpen = true },
						new() { socketId = 2, direction = 3, requiredPolarity = Polarity.Heat, isOpen = true }
					};
					break;
				default:
					break;
				}

			// 📝 Learning Opportunity: Fixed Material Leak Pattern 📝 
			// @copilot - Create visual with proper material management - no leaks!
			GameObject visual = null;
			Color visualColor = Color.white; // Define color first

			switch (name)
				{
				case "Hub":
					visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
					visualColor = Color.blue;
					break;
				case "Corridor":
					visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
					visualColor = Color.gray;
					break;
				case "Chamber":
					visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
					visualColor = Color.yellow;
					break;
				case "Specialist":
					visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
					visualColor = Color.red;
					break;
				default:
					break;
				}

			if (visual != null)
				{
				visual.transform.SetParent(go.transform);
				visual.transform.localPosition = Vector3.zero;
				visual.transform.localScale = Vector3.one * 0.6f;

				// Create NEW material with proper color - no leaks, no waste!
				Renderer renderer = visual.GetComponent<Renderer>();
				var newMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
					{
					color = visualColor // Use our predetermined color
					};
				renderer.material = newMaterial; // Assign our controlled material
				}

			SavePrefab(go, $"WfcTilePrototype_{name}.prefab");
			}

		private static void SavePrefab (GameObject go, string filename)
			{
			try
				{
				string path = PrefabPath + filename;
				GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
				if (prefab != null)
					{
					Debug.Log($"Created prefab: {path}");
					}
				else
					{
					Debug.LogError($"Failed to create prefab: {path}");
					}
				}
			catch (System.Exception e)
				{
				Debug.LogError($"Exception creating prefab {filename}: {e.Message}");
				}
			finally
				{
				Object.DestroyImmediate(go);
				}
			}
		}
	}
#endif
