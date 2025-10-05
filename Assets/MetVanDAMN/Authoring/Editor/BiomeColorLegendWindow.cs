using System.Collections.Generic;
using System.Linq;
using TinyWalnutGames.MetVD.Authoring;
using TinyWalnutGames.MetVD.Biome;
using TinyWalnutGames.MetVD.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TinyWalnutGames.MetVD.Authoring.Editor
	{
	/// <summary>
	/// Enhanced biome color legend panel for the World Debugger
	/// </summary>
	public class BiomeColorLegendWindow : EditorWindow
		{
		private readonly Dictionary<BiomeType, Color> biomeColors = new();

		private readonly List<BiomeInfo> biomeInfos = new();
		private bool autoRefresh = true;
		private double lastRefreshTime;
		private float refreshInterval = 1f;
		private Vector2 scrollPosition;

		private void OnEnable()
			{
			lastRefreshTime = EditorApplication.timeSinceStartup;
			EditorApplication.update += OnEditorUpdate;
			RefreshBiomeData();
			}

		private void OnDisable()
			{
			EditorApplication.update -= OnEditorUpdate;
			}

		private void OnGUI()
			{
			EditorGUILayout.Space(5);

			// Header controls
			DrawHeaderControls();

			EditorGUILayout.Space(10);

			// Biome legend content
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

			if (biomeInfos.Count == 0)
				{
				DrawEmptyState();
				}
			else
				{
				DrawBiomeLegend();
				}

			EditorGUILayout.EndScrollView();

			EditorGUILayout.Space(5);

			// Footer stats
			DrawFooterStats();
			}

		[MenuItem("Tiny Walnut Games/MetVanDAMN!/Debug/Gizmos/Biome Color Legend")]
		public static void ShowWindow()
			{
			BiomeColorLegendWindow window = GetWindow<BiomeColorLegendWindow>("Biome Legend");
			window.minSize = new Vector2(300, 200);
			window.RefreshBiomeData();
			}

		private void OnEditorUpdate()
			{
			if (autoRefresh && EditorApplication.timeSinceStartup - lastRefreshTime > refreshInterval)
				{
				RefreshBiomeData();
				CheckForRuntimeColorChanges();
				lastRefreshTime = EditorApplication.timeSinceStartup;
				Repaint();
				}
			}

		private void CheckForRuntimeColorChanges()
			{
			// Enhanced runtime synchronization with ECS biome systems and tilemap renderers
			if (Application.isPlaying)
				{
				SynchronizeWithECSBiomeSystems();
				}

			SynchronizeWithTilemapRenderers();
			}

		private void SynchronizeWithECSBiomeSystems()
			{
			// Runtime ECS biome system synchronization for play mode
			World world = World.DefaultGameObjectInjectionWorld;
			if (world == null)
				{
				return;
				}

			// Query ECS entities with biome components for runtime color data
			EntityManager entityManager = world.EntityManager;
			EntityQuery biomeQuery = entityManager.CreateEntityQuery(typeof(Core.Biome), typeof(NodeId));

			if (biomeQuery.CalculateEntityCount() > 0)
				{
				NativeArray<Entity> entities = biomeQuery.ToEntityArray(Allocator.Temp);
				NativeArray<Core.Biome> biomes = biomeQuery.ToComponentDataArray<Core.Biome>(Allocator.Temp);
				NativeArray<NodeId> nodeIds = biomeQuery.ToComponentDataArray<NodeId>(Allocator.Temp);

				for (int i = 0; i < entities.Length; i++)
					{
					NodeId nodeId = nodeIds[i];
					string biomeType = biomes[i].Type.ToString();

					// Update biome data if runtime color information is available
					BiomeInfo existingEntry = biomeInfos.Find(entry =>
						entry.nodeId == nodeId || entry.type.ToString() == biomeType);

					if (existingEntry != null)
						{
						existingEntry.isRuntimeActive = true;
						// Additional ECS data could be pulled here for enhanced runtime info
						}
					}

				entities.Dispose();
				biomes.Dispose();
				nodeIds.Dispose();
				}

			biomeQuery.Dispose();
			}

		private void SynchronizeWithTilemapRenderers()
			{
			// TilemapRenderer color detection and updates
			TilemapRenderer[] tilemapRenderers = FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);

			foreach (TilemapRenderer renderer in tilemapRenderers)
				{
				if (renderer.material != null && renderer.material.HasProperty("_Color"))
					{
					Color rendererColor = renderer.material.color;

					// Try to match tilemap renderer to biome by checking parent hierarchy
					BiomeFieldAuthoring biomeAuthoring = renderer.GetComponentInParent<BiomeFieldAuthoring>();
					if (biomeAuthoring != null)
						{
						BiomeInfo existingEntry = biomeInfos.Find(entry => entry.nodeId == biomeAuthoring.nodeId);
						if (existingEntry != null && existingEntry.currentColor != rendererColor)
							{
							existingEntry.currentColor = rendererColor;
							existingEntry.hasColorOverride = true;
							existingEntry.colorSource = "TilemapRenderer";
							}
						}
					else
						{
						// Try to match by name patterns
						string rendererName = renderer.gameObject.name.ToLowerInvariant();
						BiomeInfo matchingEntry = biomeInfos.Find(entry =>
							rendererName.Contains(entry.type.ToString().ToLowerInvariant()) ||
							rendererName.Contains(entry.nodeId.ToString()));

						if (matchingEntry != null && matchingEntry.currentColor != rendererColor)
							{
							matchingEntry.currentColor = rendererColor;
							matchingEntry.hasColorOverride = true;
							matchingEntry.colorSource = $"TilemapRenderer ({renderer.name})";
							}
						}
					}
				}
			}

		private void DrawHeaderControls()
			{
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("Refresh", GUILayout.Width(60)))
				{
				RefreshBiomeData();
				}

			GUILayout.Space(10);

			autoRefresh = EditorGUILayout.Toggle("Auto Refresh", autoRefresh, GUILayout.Width(100));

			if (autoRefresh)
				{
				GUILayout.Label("Interval:", GUILayout.Width(50));
				refreshInterval = EditorGUILayout.Slider(refreshInterval, 0.5f, 5f, GUILayout.Width(100));
				GUILayout.Label("s", GUILayout.Width(15));
				}

			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Focus All", GUILayout.Width(70)))
				{
				FocusAllBiomes();
				}

			EditorGUILayout.EndHorizontal();
			}

		private void DrawEmptyState()
			{
			EditorGUILayout.BeginVertical(GUI.skin.box);

			GUILayout.FlexibleSpace();

			GUIStyle centeredStyle = new(EditorStyles.label)
				{
				alignment = TextAnchor.MiddleCenter,
				fontSize = 14,
				fontStyle = FontStyle.Italic
				};

			GUILayout.Label("No biomes found in scene", centeredStyle);
			GUILayout.Space(5);

			GUIStyle smallCenteredStyle = new(EditorStyles.label)
				{
				alignment = TextAnchor.MiddleCenter,
				fontSize = 11
				};

			GUILayout.Label("Add BiomeFieldAuthoring components to see biome information", smallCenteredStyle);

			GUILayout.FlexibleSpace();

			EditorGUILayout.EndVertical();
			}

		private void DrawBiomeLegend()
			{
			EditorGUILayout.LabelField("Biome Color Legend", EditorStyles.boldLabel);
			EditorGUILayout.Space(5);

			foreach (BiomeInfo biomeInfo in biomeInfos.OrderBy(b => b.type.ToString()))
				{
				DrawBiomeEntry(biomeInfo);
				}
			}

		private void DrawBiomeEntry(BiomeInfo biomeInfo)
			{
			EditorGUILayout.BeginHorizontal(GUI.skin.box);

			// Color indicator
			Rect colorRect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20), GUILayout.Height(20));
			EditorGUI.DrawRect(colorRect, biomeInfo.color);
			EditorGUI.DrawRect(new Rect(colorRect.x, colorRect.y, colorRect.width, 1), Color.black);
			EditorGUI.DrawRect(new Rect(colorRect.x, colorRect.yMax - 1, colorRect.width, 1), Color.black);
			EditorGUI.DrawRect(new Rect(colorRect.x, colorRect.y, 1, colorRect.height), Color.black);
			EditorGUI.DrawRect(new Rect(colorRect.xMax - 1, colorRect.y, 1, colorRect.height), Color.black);

			GUILayout.Space(5);

			// Biome info
			EditorGUILayout.BeginVertical();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(biomeInfo.name, EditorStyles.boldLabel, GUILayout.Width(120));
			EditorGUILayout.LabelField($"({biomeInfo.instanceCount} instances)", GUILayout.Width(100));
			GUILayout.FlexibleSpace();

			// Visibility toggle
			bool newVisibility = EditorGUILayout.Toggle(biomeInfo.isVisible, GUILayout.Width(20));
			if (newVisibility != biomeInfo.isVisible)
				{
				biomeInfo.isVisible = newVisibility;
				UpdateBiomeVisibility(biomeInfo);
				}

			if (GUILayout.Button("Focus", GUILayout.Width(50)))
				{
				FocusBiome(biomeInfo);
				}

			EditorGUILayout.EndHorizontal();

			// Additional details
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField($"Type: {biomeInfo.type}", EditorStyles.miniLabel, GUILayout.Width(150));
			if (biomeInfo.artProfile != null)
				{
				EditorGUILayout.LabelField($"Profile: {biomeInfo.artProfile.name}", EditorStyles.miniLabel);
				}
			else
				{
				EditorGUILayout.LabelField("Profile: None", EditorStyles.miniLabel);
				}

			EditorGUILayout.EndHorizontal();

			if (biomeInfo.instanceCount > 1)
				{
				EditorGUILayout.LabelField($"Avg Position: {biomeInfo.averagePosition:F1}", EditorStyles.miniLabel);
				}

			EditorGUILayout.EndVertical();

			EditorGUILayout.EndHorizontal();

			GUILayout.Space(2);
			}

		private void DrawFooterStats()
			{
			EditorGUILayout.BeginHorizontal(GUI.skin.box);

			EditorGUILayout.LabelField($"Total Biomes: {biomeInfos.Count}", EditorStyles.miniLabel);
			EditorGUILayout.LabelField($"Total Instances: {biomeInfos.Sum(b => b.instanceCount)}",
				EditorStyles.miniLabel);
			EditorGUILayout.LabelField($"Unique Types: {biomeInfos.Select(b => b.type).Distinct().Count()}",
				EditorStyles.miniLabel);

			EditorGUILayout.EndHorizontal();
			}

		private void RefreshBiomeData()
			{
			biomeInfos.Clear();
			biomeColors.Clear();

			BiomeFieldAuthoring[] biomeAuthorings = FindObjectsByType<BiomeFieldAuthoring>(FindObjectsSortMode.None);
			var biomesByType = new Dictionary<BiomeType, List<BiomeFieldAuthoring>>();

			// Group biomes by type
			foreach (BiomeFieldAuthoring biomeAuthoring in biomeAuthorings)
				{
				if (biomeAuthoring == null)
					{
					continue;
					}

				BiomeType biomeType = biomeAuthoring.biomeType;

				if (!biomesByType.ContainsKey(biomeType))
					{
					biomesByType[biomeType] = new List<BiomeFieldAuthoring>();
					}

				biomesByType[biomeType].Add(biomeAuthoring);
				}

			// Sync with runtime ECS data if available
			SyncWithRuntimeBiomeData(biomesByType);

			// Create biome info entries
			foreach (KeyValuePair<BiomeType, List<BiomeFieldAuthoring>> kvp in biomesByType)
				{
				BiomeType type = kvp.Key;
				List<BiomeFieldAuthoring> instances = kvp.Value;

				Color biomeColor = GetBiomeColor(type, instances);
				Vector3 averagePosition = CalculateAveragePosition(instances);
				BiomeArtProfile? artProfile = GetMostCommonArtProfile(instances);

				var biomeInfo = new BiomeInfo
					{
					type = type,
					color = biomeColor,
					name = GetBiomeDisplayName(type, artProfile),
					instanceCount = instances.Count,
					averagePosition = averagePosition,
					artProfile = artProfile,
					isVisible = true
					};

				biomeInfos.Add(biomeInfo);
				biomeColors[type] = biomeColor;
				}
			}

		/// <summary>
		/// Syncs biome color data with runtime ECS systems when available
		/// </summary>
		private void SyncWithRuntimeBiomeData(Dictionary<BiomeType, List<BiomeFieldAuthoring>> biomesByType)
			{
			// Sync with runtime biome field system if available during play mode
			if (Application.isPlaying && World.DefaultGameObjectInjectionWorld != null)
				{
				World world = World.DefaultGameObjectInjectionWorld;
				SystemHandle biomeSystem = world.GetExistingSystem<BiomeFieldSystem>();

				if (biomeSystem == null)
					{
					return;
					}

				foreach (BiomeFieldAuthoring biome in biomesByType.SelectMany(kvp => kvp.Value))
					{
					if (biome == null)
						{
						continue;
						}

					BiomeType biomeType = biome.biomeType;
					if (!biomeColors.ContainsKey(biomeType))
						{
						biomeColors[biomeType] = GetBiomeColor(biomeType, biomesByType[biomeType]);
						}
					}

				// Access runtime biome data through ECS query
				SyncWithECSBiomeData(world, biomesByType);
				}

			// Also sync with any runtime tile renderers that may have modified colors
			SyncWithTileMapRenderers(biomesByType);
			}

		private void SyncWithECSBiomeData(World world, Dictionary<BiomeType, List<BiomeFieldAuthoring>> biomesByType)
			{
			try
				{
				EntityManager entityManager = world.EntityManager;
				var biomesByTypeList = biomesByType.Keys.ToList();

				// Query all biome entities with art profile references
				using EntityQuery query = entityManager.CreateEntityQuery(
					ComponentType.ReadOnly<Core.Biome>(),
					ComponentType.ReadOnly<BiomeArtProfileReference>()
				);

				using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
				using NativeArray<Core.Biome> biomeComponents = query.ToComponentDataArray<Core.Biome>(Allocator.Temp);
				using NativeArray<BiomeArtProfileReference> artProfileRefs =
					query.ToComponentDataArray<BiomeArtProfileReference>(Allocator.Temp);

				// for each biome type in biomesByTypeList, find matching ECS entities
				for (int j = 0; j < biomesByTypeList.Count; j++)
					{
					for (int i = 0; i < entities.Length; i++)
						{
						Core.Biome biomeComponent = biomeComponents[i];
						if (biomeComponent.Type == biomeComponents[i].Type)
							{
							BiomeArtProfileReference artProfileRef = artProfileRefs[i];
							if (artProfileRef.ProfileRef.IsValid())
								{
								BiomeArtProfile profile = artProfileRef.ProfileRef.Value;
								if (profile.debugColor.a > 0f)
									{
									// Runtime debug color takes precedence
									UpdateBiomeColorFromRuntime(biomeComponent.Type, profile.debugColor);
									}
								}
							}
						}

					BiomeType biomeType = biomesByTypeList[j];
					}

				// Additionally, update biome info entries with art profile data
				for (int i = 0; i < entities.Length; i++)
					{
					Core.Biome biomeComponent = biomeComponents[i];
					BiomeArtProfileReference artProfileRef = artProfileRefs[i];

					if (artProfileRef.ProfileRef.Value != null)
						{
						BiomeArtProfile profile = artProfileRef.ProfileRef.Value;

						// Update biome info entry if it exists
						BiomeInfo existingEntry = biomeInfos.Find(entry => entry.type == biomeComponent.Type);
						if (existingEntry != null)
							{
							existingEntry.isRuntimeActive = true;
							existingEntry.artProfile = profile;
							existingEntry.name = GetBiomeDisplayName(biomeComponent.Type, profile);

							// If runtime color differs from design color, mark as override
							if (existingEntry.color != profile.debugColor && profile.debugColor.a > 0f)
								{
								existingEntry.currentColor = profile.debugColor;
								existingEntry.hasColorOverride = true;
								existingEntry.colorSource = "ECS BiomeArtProfile";
								}
							}
						}
					}
				}
			catch (System.Exception ex)
				{
				// Handle potential ECS access issues gracefully
				Debug.LogWarning($"Could not sync with ECS biome data: {ex.Message}");
				}
			}

		private void SyncWithTileMapRenderers(Dictionary<BiomeType, List<BiomeFieldAuthoring>> biomesByType)
			{
			TilemapRenderer[] tilemapRenderers = FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);

			foreach (TilemapRenderer renderer in tilemapRenderers)
				{
				if (renderer.material != null && renderer.material.HasProperty("_Color"))
					{
					// Check if this tilemap is associated with any biome
					BiomeFieldAuthoring? associatedBiome = FindBiomeForTilemap(renderer, biomesByType);
					if (associatedBiome != null)
						{
						// Update color from runtime material
						Color runtimeColor = renderer.material.color;
						if (runtimeColor.a > 0f) // Valid color
							{
							UpdateBiomeColorFromRuntime(associatedBiome.biomeType, runtimeColor);
							}
						}
					}
				}
			}

		private BiomeFieldAuthoring? FindBiomeForTilemap(TilemapRenderer renderer,
			Dictionary<BiomeType, List<BiomeFieldAuthoring>> biomesByType)
			{
			// Find biome by proximity to tilemap or by name matching
			foreach (KeyValuePair<BiomeType, List<BiomeFieldAuthoring>> kvp in biomesByType)
				{
				foreach (BiomeFieldAuthoring biome in kvp.Value)
					{
					if (biome == null)
						{
						continue;
						}

					if (renderer == null)
						{
						continue;
						}

					renderer.name.ToLowerInvariant();
					// Try to match by name patterns first
					if (renderer.name.ToLowerInvariant().Contains(biome.biomeType.ToString().ToLowerInvariant()) ||
					    renderer.name.ToLowerInvariant().Contains(biome.nodeId.ToString().ToLowerInvariant()))
						{
						return biome;
						}

					// Or by proximity
					float distance = Vector3.Distance(biome.transform.position, renderer.transform.position);
					if (distance < biome.fieldRadius + 5f) // Within biome influence + buffer
						{
						return biome;
						}
					}
				}

			return null;
			}

		private void UpdateBiomeColorFromRuntime(BiomeType biomeType, Color runtimeColor)
			{
			biomeColors[biomeType] = runtimeColor;
			}

		private Color GetBiomeColor(BiomeType type, List<BiomeFieldAuthoring> instances)
			{
			// Try to derive from BiomeArtProfileLibrary (by biome name match)
			Color? libraryColor = TryGetLibraryColorForType(type);
			if (libraryColor.HasValue) return libraryColor.Value;

			// Fallback to default colors based on biome type
			return GetDefaultBiomeColor(type);
			}

		private Color? TryGetLibraryColorForType(BiomeType type)
			{
			// Look for a library in the scene
			BiomeArtProfileLibraryAuthoring libAuth =
				Object.FindFirstObjectByType<BiomeArtProfileLibraryAuthoring>(FindObjectsInactive.Include);
			if (libAuth == null || libAuth.library == null)
				return null;

			string typeName = type.ToString();

			// Prefer per-type bucket colors if available
			BiomeArtProfileLibrary lib = libAuth.library;
			if (lib.perTypeBuckets != null)
				{
				for (int i = 0; i < lib.perTypeBuckets.Length; i++)
					{
					BiomeArtProfileLibrary.BiomeTypeBucket bucket = lib.perTypeBuckets[i];
					if (bucket == null || bucket.type != type || bucket.profiles == null) continue;
					foreach (BiomeArtProfile p in bucket.profiles)
						{
						if (p != null && p.debugColor.a > 0f) return p.debugColor;
						}
					}
				}

			// Fallback: global profiles
			if (lib.profiles != null && lib.profiles.Length > 0)
				{
				// First try: exact or contains match by biomeName
				foreach (BiomeArtProfile p in lib.profiles)
					{
					if (p == null || string.IsNullOrEmpty(p.biomeName)) continue;
					if (p.biomeName.IndexOf(typeName, System.StringComparison.OrdinalIgnoreCase) >= 0 &&
					    p.debugColor.a > 0f)
						{
						return p.debugColor;
						}
					}

				// Fallback: use first with a valid debug color
				foreach (BiomeArtProfile p in lib.profiles)
					{
					if (p != null && p.debugColor.a > 0f) return p.debugColor;
					}
				}

			return null;
			}

		private Color GetDefaultBiomeColor(BiomeType type)
			{
			// 🔥 FIXED: Complete color mapping for all 27 biome types
			return type switch
				{
				// Light-aligned biomes
				BiomeType.SolarPlains => new Color(1f, 0.9f, 0.4f, 1f), // Bright golden
				BiomeType.CrystalCaverns => new Color(0.6f, 0.8f, 1f, 1f), // Crystal blue
				BiomeType.SkyGardens => new Color(0.4f, 0.7f, 0.9f, 1f), // Sky blue

				// Dark-aligned biomes
				BiomeType.ShadowRealms => new Color(0.2f, 0.1f, 0.3f, 1f), // Dark purple
				BiomeType.DeepUnderwater => new Color(0.1f, 0.3f, 0.6f, 1f), // Deep blue
				BiomeType.VoidChambers => new Color(0.1f, 0.1f, 0.1f, 1f), // Near black

				// Hazard/Energy biomes
				BiomeType.VolcanicCore => new Color(1f, 0.3f, 0.1f, 1f), // Lava red
				BiomeType.PowerPlant => new Color(0.2f, 0.8f, 0.3f, 1f), // Electric green
				BiomeType.PlasmaFields => new Color(0.9f, 0.2f, 0.9f, 1f), // Plasma purple

				// Ice/Crystal biomes
				BiomeType.FrozenWastes => new Color(0.9f, 0.95f, 1f, 1f), // Ice white
				BiomeType.IceCatacombs => new Color(0.7f, 0.85f, 0.95f, 1f), // Ice blue
				BiomeType.CryogenicLabs => new Color(0.6f, 0.9f, 1f, 1f), // Cryo blue
				BiomeType.IcyCanyon => new Color(0.8f, 0.9f, 0.95f, 1f), // Canyon ice
				BiomeType.Tundra => new Color(0.8f, 0.9f, 1f, 1f), // Tundra blue

				// Earth/Nature biomes
				BiomeType.Forest => new Color(0.2f, 0.6f, 0.2f, 1f), // Forest green
				BiomeType.Mountains => new Color(0.5f, 0.5f, 0.5f, 1f), // Mountain gray
				BiomeType.Desert => new Color(0.9f, 0.8f, 0.3f, 1f), // Desert tan

				// Water biomes
				BiomeType.Ocean => new Color(0.2f, 0.4f, 0.8f, 1f), // Ocean blue

				// Space biomes
				BiomeType.Cosmic => new Color(0.3f, 0.1f, 0.6f, 1f), // Cosmic purple

				// Crystal biomes
				BiomeType.Crystal => new Color(0.8f, 0.5f, 1f, 1f), // Crystal violet

				// Ruins/Ancient biomes
				BiomeType.Ruins => new Color(0.6f, 0.5f, 0.4f, 1f), // Ancient stone
				BiomeType.AncientRuins => new Color(0.7f, 0.6f, 0.3f, 1f), // Weathered gold

				// Volcanic/Fire biomes
				BiomeType.Volcanic => new Color(0.8f, 0.2f, 0.1f, 1f), // Volcanic red
				BiomeType.Hell => new Color(1f, 0.1f, 0f, 1f), // Hell fire red

				// Neutral/Mixed biomes
				BiomeType.HubArea => new Color(0.7f, 0.7f, 0.7f, 1f), // Neutral gray
				BiomeType.TransitionZone => new Color(0.6f, 0.6f, 0.8f, 1f), // Transition purple

				// Unknown/fallback
				BiomeType.Unknown => new Color(0.5f, 0.5f, 0.5f, 1f), // Unknown gray

				// Default hash-based fallback for any future additions
				_ => GenerateHashBasedColor(type)
				};
			}

		private static Color GenerateHashBasedColor(BiomeType type)
			{
			// Generate color based on hash of type name for future-proofing
			int hash = type.GetHashCode();
			float r = ((hash & 0xFF0000) >> 16) / 255f;
			float g = ((hash & 0x00FF00) >> 8) / 255f;
			float b = (hash & 0x0000FF) / 255f;
			return new Color(r * 0.7f + 0.3f, g * 0.7f + 0.3f, b * 0.7f + 0.3f, 1f);
			}

		private Vector3 CalculateAveragePosition(List<BiomeFieldAuthoring> instances)
			{
			if (instances.Count == 0)
				{
				return Vector3.zero;
				}

			Vector3 sum = Vector3.zero;
			foreach (BiomeFieldAuthoring instance in instances)
				{
				sum += instance.transform.position;
				}

			return sum / instances.Count;
			}

		private BiomeArtProfile? GetMostCommonArtProfile(List<BiomeFieldAuthoring> instances)
			{
			// Without direct art references on fields, infer from library by biome type prevalence.
			BiomeArtProfileLibraryAuthoring libAuth =
				Object.FindFirstObjectByType<BiomeArtProfileLibraryAuthoring>(FindObjectsInactive.Include);
			if (libAuth == null || libAuth.library == null)
				return null;

			// Count types
			BiomeType? topType = instances
				.GroupBy(i => i.biomeType)
				.OrderByDescending(g => g.Count())
				.FirstOrDefault()?.Key;

			if (topType == null) return null;

			BiomeArtProfileLibrary lib = libAuth.library;
			// Prefer per-type bucket profiles
			if (lib.perTypeBuckets != null)
				{
				for (int i = 0; i < lib.perTypeBuckets.Length; i++)
					{
					BiomeArtProfileLibrary.BiomeTypeBucket bucket = lib.perTypeBuckets[i];
					if (bucket == null || bucket.type != topType || bucket.profiles == null ||
					    bucket.profiles.Length == 0) continue;
					// choose first defined profile
					for (int j = 0; j < bucket.profiles.Length; j++)
						{
						if (bucket.profiles[j] != null) return bucket.profiles[j];
						}
					}
				}

			// Fallback: pick first global profile whose biomeName matches type
			string typeName = topType.ToString();
			if (lib.profiles != null)
				{
				foreach (BiomeArtProfile p in lib.profiles)
					{
					if (p == null || string.IsNullOrEmpty(p.biomeName)) continue;
					if (p.biomeName.IndexOf(typeName, System.StringComparison.OrdinalIgnoreCase) >= 0)
						return p;
					}
				}

			return null;
			}

		private string GetBiomeDisplayName(BiomeType type, BiomeArtProfile? artProfile)
			{
			return artProfile != null && !string.IsNullOrEmpty(artProfile.biomeName)
				? artProfile.biomeName
				: type.ToString();
			}

		private void FocusBiome(BiomeInfo biomeInfo)
			{
			BiomeFieldAuthoring[] instances = FindObjectsByType<BiomeFieldAuthoring>(FindObjectsSortMode.None)
				.Where(b => b.biomeType.Equals(biomeInfo.type))
				.ToArray();

			if (instances.Length > 0)
				{
				Selection.objects = instances.Cast<Object>().ToArray();

				if (instances.Length == 1)
					{
					SceneView.FrameLastActiveSceneView();
					}
				else
					{
					// Frame all instances
					var bounds = new Bounds(instances[0].transform.position, Vector3.zero);
					foreach (BiomeFieldAuthoring instance in instances)
						{
						bounds.Encapsulate(instance.transform.position);
						}

					SceneView.lastActiveSceneView.Frame(bounds, false);
					}
				}
			}

		private void FocusAllBiomes()
			{
			BiomeFieldAuthoring[] allBiomes = FindObjectsByType<BiomeFieldAuthoring>(FindObjectsSortMode.None);

			if (allBiomes.Length > 0)
				{
				Selection.objects = allBiomes.Cast<Object>().ToArray();

				var bounds = new Bounds(allBiomes[0].transform.position, Vector3.zero);
				foreach (BiomeFieldAuthoring biome in allBiomes)
					{
					bounds.Encapsulate(biome.transform.position);
					}

				SceneView.lastActiveSceneView.Frame(bounds, false);
				}
			}

		private void UpdateBiomeVisibility(BiomeInfo biomeInfo)
			{
			BiomeFieldAuthoring[] instances = FindObjectsByType<BiomeFieldAuthoring>(FindObjectsSortMode.None)
				.Where(b => b.biomeType.Equals(biomeInfo.type))
				.ToArray();

			foreach (BiomeFieldAuthoring instance in instances)
				{
				instance.gameObject.SetActive(biomeInfo.isVisible);
				}

			SceneView.RepaintAll();
			}

		/// <summary>
		/// Get the current biome color mapping for use by other systems
		/// </summary>
		public static Dictionary<BiomeType, Color> GetBiomeColorMapping()
			{
			// Use modern EditorWindow finding approach for Unity 6000.2.2f1
			BiomeColorLegendWindow? window =
				HasOpenInstances<BiomeColorLegendWindow>() ? GetWindow<BiomeColorLegendWindow>() : null;
			return window != null ? window.biomeColors : new Dictionary<BiomeType, Color>();
			}

		[System.Serializable]
		private class BiomeInfo
			{
			public BiomeType type;
			public Color color;
			public Color currentColor; // Runtime color (may differ from design-time)
			public string? name;
			public int instanceCount;
			public Vector3 averagePosition;
			public BiomeArtProfile? artProfile;
			public bool isVisible = true;
			public bool isRuntimeActive = false; // True if found in ECS systems during play mode
			public bool hasColorOverride = false; // True if runtime color differs from design color
			public string colorSource = ""; // Source of current color (e.g., "TilemapRenderer", "ECS", "Design")
			public uint nodeId = 0; // NodeId for better tracking
			}
		}
	}
