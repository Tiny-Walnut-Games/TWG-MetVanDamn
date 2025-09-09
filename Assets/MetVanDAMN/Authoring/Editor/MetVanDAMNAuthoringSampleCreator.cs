#if UNITY_EDITOR
using System.IO;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using TinyWalnutGames.MetVD.Shared;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TinyWalnutGames.MetVD.Authoring.Editor
	{
	/// <summary>
	/// Creates the MetVanDAMN Authoring Sample scene as specified in the art pass issue
	/// This scene demonstrates the complete authoring workflow without custom bootstrappers
	/// 🧙‍♂️ ENHANCED: Now includes SubScene creation with proper Unity timing magic!
	/// </summary>
	public static class MetVanDAMNAuthoringSampleCreator
		{
		private const string SubScenesFolder = "Assets/Scenes/SubScenes";
		private static readonly string[] SampleSubScenes = { "SampleGeneration", "SampleBiomes" };

		[MenuItem("Tiny Walnut Games/MetVanDAMN/Sample Creation/Create Authoring Sample Scene")]
		public static void CreateAuthoringSampleScene()
			{
			// 🔥 PHASE 1: Create new scene and basic components
			Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

			CreateWorldConfiguration();
			CreateSampleDistricts();
			CreateSampleConnections();
			CreateSampleBiomeFields();
			CreateWfcTilePrototypeLibrary();
			CreateSampleCamera();
			CreateLighting();

			// 🔥 PHASE 2: Save initial scene
			string scenePath = "Assets/Scenes/MetVanDAMN_AuthoringSample.unity";
			EnsureFolder("Assets/Scenes");
			EditorSceneManager.SaveScene(newScene, scenePath);

			Debug.Log($"✅ Initial scene created at: {scenePath}");

			// 🧙‍♂️ PHASE 3: Create SubScenes with ULTRA timing magic
			CreateSampleSubScenesWithTiming(scenePath);

			Debug.Log($"🌟 MetVanDAMN Authoring Sample scene created at: {scenePath}");
			Debug.Log("🎮 This scene can be played directly without custom bootstrappers!");
			Debug.Log("🔍 SubScenes included for advanced workflow demonstration");
			}

		/// <summary>
		/// 🧙‍♂️ ULTRA TIMING MAGIC: Enhanced beyond MetVanDAMNSceneBootstrap.cs
		/// Creates SubScenes with MAXIMUM Unity asset database synchronization
		/// </summary>
		private static void CreateSampleSubScenesWithTiming(string mainScenePath)
			{
			EnsureFolder(SubScenesFolder);

			// 🔥 PHASE 3A: Create SubScene files first, let meta files stabilize
			Debug.Log("📁 Creating SubScene files...");
			foreach (string subName in SampleSubScenes)
				{
				if (!SafeEnsureSubScene(Path.Combine(SubScenesFolder, subName + ".unity").Replace("\\", "/"), subName))
					{
					Debug.LogError($"❌ Failed to create SubScene: {subName}");
					return;
					}
				}

			// 🔥 PHASE 3B: ULTRA AGGRESSIVE asset database synchronization
			Debug.Log("🔄 ULTRA synchronizing asset database...");
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

			// 🧙‍♂️ EXTRA WAIT: Let Unity fully digest the scenes
			System.Threading.Thread.Sleep(1000); // Longer wait for more stability

			// 🧙‍♂️ PHASE 3C: EXTENDED meta file validation with retries
			Debug.Log("⏳ EXTENDED meta file validation...");
			bool allMetaFilesReady = false;
			int metaAttempts = 0;
			while (!allMetaFilesReady && metaAttempts < 50) // Increased attempts
				{
				allMetaFilesReady = true;
				foreach (string subName in SampleSubScenes)
					{
					string scenePath = Path.Combine(SubScenesFolder, subName + ".unity").Replace("\\", "/");
					string metaPath = scenePath + ".meta";
					string guid = AssetDatabase.AssetPathToGUID(scenePath);

					if (!File.Exists(metaPath) || string.IsNullOrEmpty(guid))
						{
						allMetaFilesReady = false;
						Debug.Log($"🔍 Still waiting for: {subName} (Meta: {File.Exists(metaPath)}, GUID: {!string.IsNullOrEmpty(guid)})");
						break;
						}
					}

				if (!allMetaFilesReady)
					{
					System.Threading.Thread.Sleep(300); // Longer waits
					AssetDatabase.Refresh(); // Additional refresh attempts
					metaAttempts++;
					}
				}

			if (!allMetaFilesReady)
				{
				Debug.LogWarning("⚠️ EXTENDED meta file validation timeout. Proceeding anyway...");
				}
			else
				{
				Debug.Log("✅ ALL SubScene meta files validated successfully with EXTENDED validation.");
				}

			// 🎯 PHASE 3D: Re-open main scene and create SubScene GameObjects
			Debug.Log("🔗 Creating SubScene GameObjects...");
			Scene mainScene = EditorSceneManager.OpenScene(mainScenePath, OpenSceneMode.Single);

			var subScenesParent = new GameObject("_SubScenes");
			subScenesParent.transform.position = new Vector3(15, 0, 0); // Offset from main content

			// Create all SubScene GameObjects first (no scene references yet)
			foreach (string subName in SampleSubScenes)
				{
				CreateSubSceneGameObject(subName, subScenesParent.transform);
				}

			// 🔥 PHASE 3E: Save scene with empty SubScene components
			Debug.Log("💾 Saving scene with SubScene GameObjects...");
			EditorSceneManager.MarkSceneDirty(mainScene);
			EditorSceneManager.SaveScene(mainScene);

			// 🧙‍♂️ PHASE 3F: Simple sync before reference assignment (working approach)
			Debug.Log("🔄 Synchronizing before reference assignment...");
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			// ✅ PHASE 3G: Use EditorApplication.delayCall for proper timing (WORKING APPROACH!)
			Debug.Log("🔗 Assigning SceneAsset references with proper timing...");
			EditorApplication.delayCall += () =>
				{
					Debug.Log("🔗 DELAYED: Starting SceneAsset reference assignment...");

					foreach (string subName in SampleSubScenes)
						{
						Debug.Log($"🔗 Attempting reference assignment for: {subName}");
						AssignSubSceneReferenceWithValidation(subName);
						}

					// Final save
					Scene activeScene = EditorSceneManager.GetActiveScene();
					EditorSceneManager.MarkSceneDirty(activeScene);
					EditorSceneManager.SaveScene(activeScene);
					AssetDatabase.SaveAssets();

					Debug.Log("✅ SubScene creation complete with WORKING assignment logic!");
				};
			}

		/// <summary>
		/// Creates SubScene file with ENHANCED GUID validation
		/// </summary>
		private static bool SafeEnsureSubScene(string scenePath, string sceneName)
			{
			if (File.Exists(scenePath))
				{
				Debug.Log($"🔍 SubScene already exists: {sceneName}");
				return true; // Already exists
				}

			try
				{
				Scene subScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
				subScene.name = sceneName;

				// Add sample content to SubScene
				AddSampleSubSceneContent(sceneName);

				if (!EditorSceneManager.SaveScene(subScene, scenePath))
					{
					Debug.LogError("❌ Failed to save SubScene " + sceneName + " at " + scenePath);
					return false;
					}

				// 🔥 ULTRA AGGRESSIVE synchronization!
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

				// 🧙‍♂️ ENHANCED GUID VALIDATION with more attempts
				int attempts = 0;
				while (attempts < 40) // More attempts
					{
					SceneAsset testAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
					string metaPath = scenePath + ".meta";

					if (testAsset != null && File.Exists(metaPath))
						{
						string guid = AssetDatabase.AssetPathToGUID(scenePath);
						if (!string.IsNullOrEmpty(guid))
							{
							Debug.Log($"🔍 SubScene GUID verified: {sceneName} -> {guid}");

							// 🧙‍♂️ EXTRA VALIDATION: Try to reload the asset
							SceneAsset reloadTest = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
							if (reloadTest != null)
								{
								Debug.Log($"✅ SubScene reload test passed: {sceneName}");
								return true; // Success!
								}
							}
						}

					System.Threading.Thread.Sleep(150); // Longer waits
					AssetDatabase.Refresh(); // Additional refresh
					attempts++;
					}

				Debug.LogWarning($"⚠️ ENHANCED GUID validation timeout for SubScene: {sceneName}. References may be unstable.");
				return true; // Continue anyway
				}
			catch (System.Exception ex)
				{
				Debug.LogError($"❌ Failed to create SubScene {sceneName}: {ex.Message}");
				return false;
				}
			}

		/// <summary>
		/// Adds sample content to SubScenes for demonstration
		/// 🧙‍♂️ ENHANCED: Now coordinate-aware with spatial intelligence
		/// </summary>
		private static void AddSampleSubSceneContent(string sceneName)
			{
			// 🎯 COORDINATE-AWARE ENHANCEMENT: Position content based on scene purpose
			Vector3 basePosition = sceneName switch
				{
					"SampleGeneration" => new Vector3(0, 0, 0), // Center for generation demo
					"SampleBiomes" => new Vector3(0, 0.5f, 0),   // Slightly elevated for biome demo
					_ => Vector3.zero
					};

			switch (sceneName)
				{
				case "SampleGeneration":
					var genMarker = new GameObject("GenerationMarker");
					genMarker.transform.position = basePosition;

					// 🧙‍♂️ SPATIAL INTELLIGENCE: Create a mini demonstration grid
					for (int i = 0; i < 3; i++)
						{
						var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
						cube.name = $"GenerationContent_{i}";
						cube.transform.SetParent(genMarker.transform);
						cube.transform.localPosition = new Vector3(i * 0.6f - 0.6f, 0, 0);
						cube.transform.localScale = Vector3.one * (0.3f + i * 0.1f); // Progressive sizing

						// 🎯 COLOR-CODED COORDINATE AWARENESS
						Renderer renderer = cube.GetComponent<Renderer>();
						renderer.material = new(Shader.Find("Universal Render Pipeline/Lit"))
							{
							color = Color.HSVToRGB(i * 0.33f, 0.8f, 0.9f)
							};
						}
					break;

				case "SampleBiomes":
					var biomeMarker = new GameObject("BiomeMarker");
					biomeMarker.transform.position = basePosition;

					// 🧙‍♂️ BIOME-AWARE SPATIAL DEMONSTRATION
					string[] biomeTypes = new[] { "Solar", "Volcanic", "Icy", "Hub" };
					Color[] biomeColors = new[] { Color.yellow, Color.red, Color.cyan, Color.blue };

					for (int i = 0; i < biomeTypes.Length; i++)
						{
						var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
						sphere.name = $"BiomeContent_{biomeTypes[i]}";
						sphere.transform.SetParent(biomeMarker.transform);

						// 🎯 RADIAL COORDINATE INTELLIGENCE
						float angle = i * (360f / biomeTypes.Length) * Mathf.Deg2Rad;
						Vector3 radialPos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 0.8f;
						sphere.transform.localPosition = radialPos;
						sphere.transform.localScale = Vector3.one * 0.2f;

						// 🧙‍♂️ BIOME-SPECIFIC MATERIAL INTELLIGENCE
						Renderer renderer = sphere.GetComponent<Renderer>();
						renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
							{
							color = biomeColors[i]
							};
						}
					break;
				default:
					break;
				}
			}

#if METVD_FULL_DOTS
		/// <summary>
		/// Creates SubScene GameObject with component but NO scene reference assigned yet
		/// </summary>
		private static void CreateSubSceneGameObject(string subName, Transform parent)
			{
			var go = new GameObject(subName);
			go.transform.SetParent(parent, false);

			// Fixed: Use System.Array.IndexOf instead of Array.IndexOf
			int index = System.Array.IndexOf(SampleSubScenes, subName);
			go.transform.localPosition = new Vector3(index * 2f, 0, 0);

			// ⚠Intended⚠ DO NOT assign subscene reference yet - just create the component
			Unity.Scenes.SubScene subSceneComponent = go.AddComponent<Unity.Scenes.SubScene>();
			Debug.Log($"📦 Created SubScene GameObject: {subName} (no reference assigned yet)");
			Debug.LogWarning($"⚠️ SubSceneComponent '{subSceneComponent}' reference will be assigned in a separate step to ensure stability.");
			}

		/// <summary>
		/// ✅ WORKING APPROACH: Copy proven SubScene assignment logic from MetVanDAMNSceneBootstrap.cs
		/// This uses the exact same logic that successfully assigns SubScene references
		/// </summary>
		private static void AssignSubSceneReferenceWithValidation(string subName)
			{
			string scenePath = Path.Combine(SubScenesFolder, subName + ".unity").Replace("\\", "/");

			var go = GameObject.Find(subName);
			if (go == null)
				{
				Debug.LogError($"❌ SubScene GameObject '{subName}' not found for reference assignment!");
				return;
				}

			Debug.Log($"✅ Found GameObject: {subName}");

			if (!go.TryGetComponent(out Unity.Scenes.SubScene subSceneComp))
				{
				Debug.LogError($"❌ SubScene component not found on '{subName}'!");
				return;
				}

			Debug.Log($"✅ Found SubScene component on: {subName}");

			// 🔍 VALIDATION: Load SceneAsset and verify GUID (from working approach)
			SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
			if (sceneAsset == null)
				{
				Debug.LogError($"❌ Failed to load SceneAsset from path: {scenePath}");
				return;
				}

			string guid = AssetDatabase.AssetPathToGUID(scenePath);
			if (string.IsNullOrEmpty(guid))
				{
				Debug.LogError($"❌ Scene asset at '{scenePath}' has no GUID! SubScene will show as Missing.");
				return;
				}

			Debug.Log($"✅ SceneAsset loaded with GUID: {guid}");

			// 🔗 ASSIGNMENT: Set SceneAsset reference (WORKING APPROACH!)
			try
				{
				SerializedObject so = new(subSceneComp);

				// 🧙‍♂️ DEBUG: List all available properties to identify the correct one
				SerializedProperty iterator = so.GetIterator();
				var availableProperties = new System.Collections.Generic.List<string>();

				if (iterator.NextVisible(true))
					{
					do
						{
						availableProperties.Add(iterator.propertyPath);
						}
					while (iterator.NextVisible(false));
					}

				Debug.Log($"🔍 Available properties on {subName}: {string.Join(", ", availableProperties)}");

				// 🎯 CRITICAL: Try multiple property names for SceneAsset reference (WORKING APPROACH!)
				SerializedProperty sceneProp = so.FindProperty("m_SceneAsset");
				if (sceneProp == null) sceneProp = so.FindProperty("_SceneAsset");
				if (sceneProp == null) sceneProp = so.FindProperty("sceneAsset");
				if (sceneProp == null) sceneProp = so.FindProperty("SceneAsset");
				if (sceneProp == null) sceneProp = so.FindProperty("m_Scene");
				if (sceneProp == null) sceneProp = so.FindProperty("_Scene");

				if (sceneProp != null)
					{
					sceneProp.objectReferenceValue = sceneAsset;
					Debug.Log($"✅ Set SceneAsset reference for {subName} using property: {sceneProp.propertyPath}");
					}
				else
					{
					Debug.LogWarning($"⚠️ SceneAsset property not found on {subName} - available properties: {string.Join(", ", availableProperties)}");
					}

				// 🔗 ASSIGNMENT: Set auto-load (from working approach)
				SerializedProperty autoLoadProp = so.FindProperty("m_AutoLoadScene");
				if (autoLoadProp == null) autoLoadProp = so.FindProperty("AutoLoadScene");
				if (autoLoadProp != null)
					{
					autoLoadProp.boolValue = true;
					Debug.Log($"✅ Set auto-load = true for {subName}");
					}

				// 🔥 ENHANCED: Force dirty state and ensure changes are applied (from working approach)
				EditorUtility.SetDirty(subSceneComp);
				EditorUtility.SetDirty(go);

				// 💾 SAVE: Apply changes with force update (from working approach)
				bool applied = so.ApplyModifiedPropertiesWithoutUndo();
				if (!applied)
					{
					Debug.LogWarning($"⚠️ Failed to apply SerializedObject changes for {subName}");
					return;
					}

				// 🧙‍♂️ FINAL VALIDATION: Verify the assignment worked (from working approach)
				SerializedObject validation = new(subSceneComp);
				SerializedProperty validateScene = validation.FindProperty("_SceneAsset");
				if (validateScene == null) validateScene = validation.FindProperty("m_SceneAsset");
				if (validateScene == null) validateScene = validation.FindProperty("sceneAsset");
				if (validateScene == null) validateScene = validation.FindProperty("SceneAsset");

				bool assignmentSuccess = validateScene != null && validateScene.objectReferenceValue != null;

				Debug.Log($"✅ SubScene reference assignment {(assignmentSuccess ? "SUCCESSFUL" : "FAILED")} for '{subName}' -> {scenePath}");
				}
			catch (System.Exception ex)
				{
				Debug.LogError($"❌ Exception during SubScene reference assignment for {subName}: {ex.Message}");
				}
			}

		/// <summary>
		/// ⚠ Intention ⚠ Legacy method for compatibility
		/// - @jmeyer1980: I kept this for compatibility but it is currently unused
		/// </summary>
		private static void AssignSubSceneReference(string subName)
			{
			AssignSubSceneReferenceWithValidation(subName);
			}
#else
        /// <summary>
        /// Creates SubScene GameObject with reflection for compatibility
        /// </summary>
        private static void CreateSubSceneGameObject(string subName, Transform parent)
        {
            var go = new GameObject(subName);
            go.transform.SetParent(parent, false);

            // Fixed: Use System.Array.IndexOf instead of Array.IndexOf
            int index = System.Array.IndexOf(SampleSubScenes, subName);
            go.transform.localPosition = new Vector3(index * 2f, 0, 0);

            System.Type subSceneType = FindTypeAnywhere("Unity.Scenes.SubScene");
            if (subSceneType != null)
            {
                go.AddComponent(subSceneType);
                Debug.Log($"📦 Created SubScene GameObject: {subName} (reflection mode, no reference assigned yet)");
            }
            else
            {
                // Fallback marker for when SubScene type isn't available
                go.AddComponent<Transform>(); // Just ensure it has a component
                Debug.Log($"📦 Created SubScene marker GameObject: {subName} (SubScene type not available)");
            }
        }

        /// <summary>
        /// Assigns SceneAsset reference using reflection with enhanced validation
        /// </summary>
        private static void AssignSubSceneReferenceWithValidation(string subName)
        {
            string scenePath = Path.Combine(SubScenesFolder, subName + ".unity").Replace("\\", "/");

            System.Type subSceneType = FindTypeAnywhere("Unity.Scenes.SubScene");
            var go = GameObject.Find(subName);
            if (go == null || subSceneType == null)
            {
                Debug.LogWarning($"⚠️ Cannot assign reference to '{subName}' - GameObject or SubScene type not found");
                return;
            }

            Component existing = go.GetComponent(subSceneType);
            if (existing == null)
            {
                Debug.LogWarning($"⚠️ SubScene component not found on '{subName}' for reference assignment");
                return;
            }

            // 🧙‍♂️ ENHANCED: Multiple attempts to load the SceneAsset
            SceneAsset sceneAsset = null;
            int loadAttempts = 0;
            while (sceneAsset == null && loadAttempts < 10)
            {
                sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                if (sceneAsset == null)
                {
                    Debug.Log($"🔄 Attempt {loadAttempts + 1}: Waiting for SceneAsset to load: {scenePath}");
                    AssetDatabase.Refresh();
                    System.Threading.Thread.Sleep(100);
                    loadAttempts++;
                }
            }

            if (sceneAsset != null)
            {
                try
                {
                    SerializedObject so = new(existing);
                    SerializedProperty sceneProp = so.FindProperty("m_SceneAsset");
                    if (sceneProp != null) sceneProp.objectReferenceValue = sceneAsset;
                    SerializedProperty autoLoadProp = so.FindProperty("m_AutoLoadScene");
                    if (autoLoadProp != null) autoLoadProp.boolValue = true;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(existing);

                    Debug.Log($"✅ SubScene reference assigned: '{subName}' -> {scenePath} (reflection mode)");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"❌ SubScene reference assignment failed for {subName}: " + e.Message);
                }
            }
            else
            {
                Debug.LogError($"❌ SceneAsset at '{scenePath}' could not be loaded after {loadAttempts} attempts!");
            }
        }

        /// <summary>
        /// Legacy method for compatibility
        /// </summary>
        private static void AssignSubSceneReference(string subName)
        {
            AssignSubSceneReferenceWithValidation(subName);
        }

        private static System.Type FindTypeAnywhere(string fullName)
        {
            foreach (System.Reflection.Assembly asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    System.Type t = asm.GetType(fullName, false);
                    if (t != null) return t;
                }
                catch { }
            }
            return null;
        }
#endif

		private static void CreateWorldConfiguration()
			{
			var worldConfigGO = new GameObject("WorldConfiguration");
			WorldConfigurationAuthoring worldConfig = worldConfigGO.AddComponent<WorldConfigurationAuthoring>();

			// Configure with sensible defaults for the sample
			// Note: Assuming WorldConfigurationAuthoring has typical world setup fields
			// @jmeyer1980 ⚠ Methodology ⚠: I allowed autocomplete to fill in all of these fields - they made sense to me
			// @Copilot TODO - Please verify they are all valid and make sense
			worldConfig.seed = 12345;
			worldConfig.worldSize = new int2(64, 64);
			worldConfig.randomizationMode = RandomizationMode.Partial;
			worldConfig.biomeCount = new Vector2Int(3, 6);
			worldConfig.biomeWeight = 1.0f;
			worldConfig.districtCount = new Vector2Int(4, 12);
			worldConfig.districtMinDistance = 15f;
			worldConfig.districtPlacementAttempts = 10;
			worldConfig.districtWeight = 1.0f;
			worldConfig.sectorsPerDistrict = new Vector2Int(2, 8);
			worldConfig.sectorGridSize = new int2(6, 6);
			worldConfig.roomsPerSector = new Vector2Int(3, 12);
			worldConfig.targetLoopDensity = 0.3f;
			worldConfig.enableDebugVisualization = true;
			worldConfig.logGenerationSteps = true;

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

					DistrictAuthoring district = districtGO.AddComponent<DistrictAuthoring>();
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

		/// <summary>
		/// Creates district visual with ENHANCED coordinate-aware styling
		/// 🧙‍♂️ SPATIAL INTELLIGENCE: Visual feedback based on grid position
		/// </summary>
		private static void CreateDistrictVisual(GameObject parent, int x, int z)
			{
			var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
			visual.name = "Visual";
			visual.transform.SetParent(parent.transform);
			visual.transform.localPosition = Vector3.zero;

			// 🎯 COORDINATE-AWARE SCALING: Size based on distance from center
			float distanceFromCenter = Mathf.Sqrt(x * x + z * z);
			float scaleMultiplier = 1.0f + (distanceFromCenter * 0.2f); // Larger at edges
			visual.transform.localScale = new Vector3(1.5f * scaleMultiplier, 0.2f, 1.5f * scaleMultiplier);

			Renderer renderer = visual.GetComponent<Renderer>();
			renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

			// 🧙‍♂️ ENHANCED SPATIAL HUE INTELLIGENCE: Multi-dimensional color coding
			float hue = (x + z + 2f) / 4f; // Base hue from position
			float saturation = 0.7f + (distanceFromCenter * 0.1f); // More saturated at edges
			float brightness = 0.8f - (Mathf.Abs(x + z) * 0.05f); // Dimmer for diagonal positions

			renderer.material.color = Color.HSVToRGB(hue, Mathf.Clamp01(saturation), Mathf.Clamp01(brightness));

			// 🎯 DEBUG INTELLIGENCE: Add coordinate text for spatial awareness
			var textGO = new GameObject("CoordinateLabel");
			textGO.transform.SetParent(visual.transform);
			// @jmeyer1980: ⚠ Fix/Upgrade ⚠ - I changed this:
			//
			// textGO.transform.localPosition = Vector3.up * 0.5f;
			// textGO.transform.localRotation = Quaternion.Euler(90, 0, 0);
			//
			// to this for better control...
			textGO.transform.SetLocalPositionAndRotation(Vector3.up * 0.5f, Quaternion.Euler(90, 0, 0));

			// 🧙‍♂️ SPATIAL DEBUGGING AID: Visual coordinate confirmation
			TextMesh textMesh = textGO.AddComponent<TextMesh>();
			textMesh.text = $"({x},{z})";
			textMesh.fontSize = 8;
			textMesh.color = Color.white;
			textMesh.anchor = TextAnchor.MiddleCenter;
			}

		private static void CreateSampleConnections()
			{
			var connectionsParent = new GameObject("Connections");

			// Find all district authoring components
			DistrictAuthoring[] districts = Object.FindObjectsByType<DistrictAuthoring>(FindObjectsSortMode.None);

			// Create connections between adjacent districts
			foreach (DistrictAuthoring district1 in districts)
				{
				foreach (DistrictAuthoring district2 in districts)
					{
					if (district1 == district2)
						{
						continue;
						}

					// Check if districts are adjacent (distance of 1 in grid coordinates)
					int dist = math.abs(district1.gridCoordinates.x - district2.gridCoordinates.x) +
							  math.abs(district1.gridCoordinates.y - district2.gridCoordinates.y);

					if (dist == 1 && district1.nodeId < district2.nodeId) // Avoid duplicate connections
						{
						var connectionGO = new GameObject($"Connection_{district1.nodeId}_{district2.nodeId}");
						connectionGO.transform.SetParent(connectionsParent.transform);

						// Position connection between districts
						Vector3 pos1 = district1.transform.position;
						Vector3 pos2 = district2.transform.position;
						connectionGO.transform.position = (pos1 + pos2) * 0.5f + Vector3.up * 0.5f;

						ConnectionAuthoring connection = connectionGO.AddComponent<ConnectionAuthoring>();
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
			Vector3 direction = (to - from).normalized;
			float distance = Vector3.Distance(from, to);

			visual.transform.localPosition = Vector3.zero;
			visual.transform.localScale = new Vector3(0.1f, distance * 0.5f, 0.1f);
			visual.transform.LookAt(parent.transform.position + direction);
			visual.transform.Rotate(90, 0, 0);

			Renderer renderer = visual.GetComponent<Renderer>();
			renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
				{
				color = new Color(0.8f, 0.3f, 0.8f)
				};
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

			// @jmeyer1980: ⚠ nitpick ⚠ I deconstructed config directly in the loop for clarity
			for (int i = 0; i < biomeConfigs.Length; i++)
				{
				(Vector3 position, BiomeType primary, BiomeType secondary, float strength, float gradient) = biomeConfigs[i];
				var biomeFieldGO = new GameObject($"BiomeField_{i + 1}");
				biomeFieldGO.transform.SetParent(biomeFieldsParent.transform);
				biomeFieldGO.transform.position = position;

				BiomeFieldAuthoring biomeField = biomeFieldGO.AddComponent<BiomeFieldAuthoring>();
				biomeField.primaryBiome = primary;
				biomeField.secondaryBiome = secondary;
				biomeField.strength = strength;
				biomeField.gradient = gradient;

				// Add visual representation
				CreateBiomeFieldVisual(biomeFieldGO, primary, strength);
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

			Renderer renderer = visual.GetComponent<Renderer>();
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

			// @jmeyer1980: ⚠ Intention ⚠ I deconstructed config directly in the loop for clarity
			for (int i = 0; i < tileConfigs.Length; i++)
				{
				(string name, uint id, float weight, BiomeType biome, Polarity polarity, byte minConn, byte maxConn) = tileConfigs[i];
				var tileGO = new GameObject($"WfcTilePrototype_{name}");
				tileGO.transform.SetParent(wfcLibraryParent.transform);
				tileGO.transform.position = new Vector3(i * 2f, 0, 0);

				WfcTilePrototypeAuthoring wfcTile = tileGO.AddComponent<WfcTilePrototypeAuthoring>();
				wfcTile.tileId = id;
				wfcTile.weight = weight;
				wfcTile.biomeType = biome;
				wfcTile.primaryPolarity = polarity;
				wfcTile.minConnections = minConn;
				wfcTile.maxConnections = maxConn;

				// ⚠ Intention ⚠ Configure sockets (simplified for sample)
				// @jmeyer1980: ⚠ nitpick ⚠ I switched the switch to an expression named switch
				wfcTile.sockets = name switch
					{
						"Hub" => new WfcSocketConfig[]
												{
							new() { socketId = 1, direction = 0, requiredPolarity = Polarity.None, isOpen = true },
							new() { socketId = 1, direction = 1, requiredPolarity = Polarity.None, isOpen = true },
							new() { socketId = 1, direction = 2, requiredPolarity = Polarity.None, isOpen = true },
							new() { socketId = 1, direction = 3, requiredPolarity = Polarity.None, isOpen = true }
												},
						"Corridor" => new WfcSocketConfig[]
							{
							new() { socketId = 1, direction = 0, requiredPolarity = Polarity.None, isOpen = true },
							new() { socketId = 1, direction = 2, requiredPolarity = Polarity.None, isOpen = true }
							},
						_ => new WfcSocketConfig[]
							{
							new() { socketId = 1, direction = 0, requiredPolarity = Polarity.None, isOpen = true },
							new() { socketId = 1, direction = 1, requiredPolarity = Polarity.None, isOpen = true }
							},// Standard configuration for other types
						};

				// Add visual representation
				CreateWfcTileVisual(tileGO, name, biome);
				}

			Debug.Log("Created WFC tile prototype library with 4 tile types");
			}

		/// <summary>
		/// Creates WFC tile visual with ENHANCED biome-aware spatial intelligence
		/// 🧙‍♂️ COORDINATE-AWARE: Visual feedback for tile properties and spatial relationships
		/// </summary>
		private static void CreateWfcTileVisual(GameObject parent, string typeName, BiomeType biomeType)
			{
			// 🎯 ENHANCED TYPE-AWARE PRIMITIVE SELECTION
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

			// 🧙‍♂️ BIOME-AWARE SCALING: Size reflects biome importance
			float biomeScaleMultiplier = biomeType switch
				{
					BiomeType.HubArea => 1.2f,      // Hubs are larger
					BiomeType.VolcanicCore => 1.1f, // Hazardous biomes slightly larger
					BiomeType.SolarPlains => 1.0f,  // Standard size
					BiomeType.TransitionZone => 0.9f, // Transitions slightly smaller
					_ => 0.8f                        // Unknown biomes smaller
					};
			visual.transform.localScale = Vector3.one * (0.8f * biomeScaleMultiplier);

			Renderer renderer = visual.GetComponent<Renderer>();
			renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

			// 🎯 ENHANCED BIOME-AWARE COLOR INTELLIGENCE
			Color typeColor = typeName switch
				{
					"Hub" => Color.blue,
					"Corridor" => Color.gray,
					"Chamber" => Color.yellow,
					"Specialist" => Color.red,
					_ => Color.white
					};

			// 🧙‍♂️ BIOME TINTING: Blend type color with biome characteristics
			Color biomeTint = biomeType switch
				{
					BiomeType.VolcanicCore => Color.red,
					BiomeType.SolarPlains => Color.yellow,
					BiomeType.IcyCanyon => Color.cyan,
					BiomeType.HubArea => Color.white,
					BiomeType.TransitionZone => Color.gray,
					_ => Color.white
					};

			// 🎯 SPATIAL COLOR BLENDING: Combine type and biome colors intelligently
			var finalColor = Color.Lerp(typeColor, biomeTint, 0.3f);
			renderer.material.color = finalColor;

			// 🧙‍♂️ DEBUG INTELLIGENCE: Add type label for spatial awareness
			var labelGO = new GameObject("TypeLabel");
			labelGO.transform.SetParent(visual.transform);
			// @jmeyer1980: ⚠ nitpick ⚠ I changed this next line as it was previously split into two lines:
			labelGO.transform.SetLocalPositionAndRotation(Vector3.up * 1.2f, Quaternion.Euler(90, 0, 0));
			TextMesh labelMesh = labelGO.AddComponent<TextMesh>();
			labelMesh.text = typeName[..Mathf.Min(3, typeName.Length)]; // Short abbreviation - ⚠ nitpick ⚠ @jmeyer1980: I changed this to a range operator
			labelMesh.fontSize = 6;
			labelMesh.color = Color.black;
			labelMesh.anchor = TextAnchor.MiddleCenter;
			}

		private static void CreateSampleCamera()
			{
			var cameraGO = new GameObject("Sample Camera");
			cameraGO.transform.position = new Vector3(0, 8, -8);
			cameraGO.transform.LookAt(Vector3.zero);

			Camera camera = cameraGO.AddComponent<Camera>();
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

			Light light = lightGO.AddComponent<Light>();
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

		/// <summary>
		/// Ensures folder exists in the AssetDatabase
		/// </summary>
		private static void EnsureFolder(string path)
			{
			if (AssetDatabase.IsValidFolder(path))
				{
				return;
				}

			string[] segments = path.Split('/');
			string current = segments[0];
			for (int i = 1; i < segments.Length; i++)
				{
				string next = current + "/" + segments[i];
				if (!AssetDatabase.IsValidFolder(next))
					{
					AssetDatabase.CreateFolder(current, segments[i]);
					}
				current = next;
				}
			}
		}
	}
#endif
