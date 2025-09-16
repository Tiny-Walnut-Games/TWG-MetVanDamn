using UnityEditor;
using UnityEngine;
using TinyWalnutGames.MetVD.Samples;
using TinyWalnutGames.MetVD.Shared;
using TinyWalnutGames.MetVD.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Authoring.Editor
	{
	/// <summary>
	/// Custom inspector for SmokeTestSceneSetup with runtime regeneration capabilities
	/// Provides "regenerate world" functionality both in editor and play mode
	/// </summary>
	[CustomEditor(typeof(SmokeTestSceneSetup))]
	public class SmokeTestSceneSetupInspector : UnityEditor.Editor
		{
		private bool _showAdvancedOptions = false;

		// Cache for reducing constant redraws
		private int _cachedWorldSeeds = -1;
		private int _cachedDistricts = -1;
		private int _cachedPolarityFields = -1;
		private uint _cachedCurrentSeed = 0;
		private float _lastUpdateTime = 0f;
		private const float UPDATE_INTERVAL = 0.5f; // Update every 500ms instead of every frame

		public override void OnInspectorGUI()
			{
			var smokeTest = (SmokeTestSceneSetup)target;

			EditorGUILayout.Space();
			EditorGUILayout.HelpBox(
				"Smoke Test Scene Setup provides immediate 'hit Play -> see map' experience. " +
				"Use this for rapid prototyping and validation of MetVanDAMN world generation.",
				MessageType.Info);

			EditorGUILayout.Space();

			// Draw default inspector
			DrawDefaultInspector();

			EditorGUILayout.Space();

			// Regeneration controls
			DrawRegenerationControls(smokeTest);

			// Advanced debugging options
			EditorGUILayout.Space();
			_showAdvancedOptions = EditorGUILayout.Foldout(_showAdvancedOptions, "Advanced Debug Options", false);
			if (_showAdvancedOptions)
				{
				DrawAdvancedOptions(smokeTest);
				}

			// Runtime entity information
			if (Application.isPlaying)
				{
				DrawRuntimeInformation(smokeTest);
				}

			if (GUI.changed)
				{
				EditorUtility.SetDirty(smokeTest);
				}
			}

		private void DrawRegenerationControls(SmokeTestSceneSetup smokeTest)
			{
			EditorGUILayout.LabelField("Regeneration Controls", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();

			// Full randomization button
			GUI.backgroundColor = new Color(1.0f, 0.8f, 0.8f); // Light red
			if (GUILayout.Button("🎲 FULL Random", GUILayout.Height(35)))
				{
				PerformFullRandomRegeneration(smokeTest);
				}

			// Partial randomization button
			GUI.backgroundColor = new Color(0.8f, 1.0f, 0.8f); // Light green
			if (GUILayout.Button("🔄 Partial Random", GUILayout.Height(35)))
				{
				PerformPartialRandomRegeneration(smokeTest);
				}

			GUI.backgroundColor = Color.white;
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space(5);

			// Seed regeneration with current settings
			EditorGUILayout.BeginHorizontal();
			GUI.backgroundColor = new Color(0.9f, 0.9f, 1.0f); // Light blue
			if (GUILayout.Button("🔨 Regenerate Current Seed", GUILayout.Height(30)))
				{
				RegenerateWithCurrentSeed(smokeTest);
				}

			GUI.backgroundColor = new Color(1.0f, 1.0f, 0.8f); // Light yellow
			if (GUILayout.Button("🎯 New Random Seed", GUILayout.Height(30)))
				{
				GenerateNewRandomSeed(smokeTest);
				}

			GUI.backgroundColor = Color.white;
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space(5);
			EditorGUILayout.HelpBox(
				"FULL Random: New seed + randomized parameters for completely different results\n" +
				"Partial Random: Keep familiar layout but randomize details\n" +
				"Current Seed: Regenerate exact same world (useful for testing)\n" +
				"New Seed: Generate new seed but keep all other parameters",
				MessageType.Info);
			}

		private void DrawAdvancedOptions(SmokeTestSceneSetup smokeTest)
			{
			EditorGUILayout.LabelField("Debug Information", EditorStyles.miniBoldLabel);

			if (Application.isPlaying && smokeTest.DefaultWorld != null)
				{
				EditorGUILayout.LabelField($"Active World: {smokeTest.DefaultWorld.Name}");
				EditorGUILayout.LabelField($"World Valid: {smokeTest.DefaultWorld.IsCreated}");
				}
			else if (Application.isPlaying)
				{
				EditorGUILayout.LabelField("Active World: None (check console for errors)");
				}
			else
				{
				EditorGUILayout.LabelField("Active World: Not in play mode");
				}

			EditorGUILayout.Space(5);

			// Manual world cleanup
			if (Application.isPlaying)
				{
				EditorGUILayout.BeginHorizontal();

				GUI.backgroundColor = new Color(1.0f, 0.7f, 0.7f);
				if (GUILayout.Button("⚠️ Clear All Entities"))
					{
					if (EditorUtility.DisplayDialog("Clear Entities",
						"This will remove ALL entities from the world for a clean slate. Continue?",
						"Clear", "Cancel"))
						{
						ClearAllGeneratedEntities(smokeTest);
						}
					}

				GUI.backgroundColor = new Color(0.7f, 0.7f, 1.0f);
				if (GUILayout.Button("🔄 Refresh Display"))
					{
					// Force immediate cache refresh
					_cachedWorldSeeds = -1;
					_cachedDistricts = -1;
					_cachedPolarityFields = -1;
					_cachedCurrentSeed = 0;
					_lastUpdateTime = 0f;
					Repaint();
					}

				GUI.backgroundColor = Color.white;
				EditorGUILayout.EndHorizontal();
				}
			}

		private void DrawRuntimeInformation(SmokeTestSceneSetup smokeTest)
			{
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Runtime Entity Information", EditorStyles.boldLabel);

			if (smokeTest.DefaultWorld != null && smokeTest.DefaultWorld.IsCreated)
				{
				// Only update entity counts periodically to reduce constant redraws
				float currentTime = (float)EditorApplication.timeSinceStartup;
				bool shouldUpdate = currentTime - _lastUpdateTime > UPDATE_INTERVAL || _cachedWorldSeeds == -1;

				if (shouldUpdate)
					{
					_lastUpdateTime = currentTime;
					// Count entities by type - cache results to avoid constant EntityQuery operations
					using EntityQuery worldSeedQuery = smokeTest.EntityManager.CreateEntityQuery(typeof(WorldSeed));
					using EntityQuery districtQuery = smokeTest.EntityManager.CreateEntityQuery(typeof(NodeId));
					using EntityQuery polarityQuery = smokeTest.EntityManager.CreateEntityQuery(typeof(PolarityFieldData));

					_cachedWorldSeeds = worldSeedQuery.CalculateEntityCount();
					_cachedDistricts = districtQuery.CalculateEntityCount();
					_cachedPolarityFields = polarityQuery.CalculateEntityCount();

					// Get current world seed if available
					if (_cachedWorldSeeds > 0)
						{
						Entity seedEntity = worldSeedQuery.GetSingletonEntity();
						WorldSeed worldSeed = smokeTest.EntityManager.GetComponentData<WorldSeed>(seedEntity);
						_cachedCurrentSeed = worldSeed.Value;
						}
					else
						{
						_cachedCurrentSeed = 0;
						}
					}

				// Always display cached values to avoid constant updates
				EditorGUILayout.BeginVertical("box");
				EditorGUILayout.LabelField($"🌱 World Seeds: {_cachedWorldSeeds}");
				EditorGUILayout.LabelField($"🏰 Districts: {_cachedDistricts}");
				EditorGUILayout.LabelField($"🌊 Polarity Fields: {_cachedPolarityFields}");

				if (_cachedCurrentSeed > 0)
					{
					EditorGUILayout.LabelField($"🎲 Current Seed: {_cachedCurrentSeed}");
					}

				EditorGUILayout.LabelField($"📊 Next update in: {Mathf.Ceil(UPDATE_INTERVAL - (currentTime - _lastUpdateTime))}s", EditorStyles.miniLabel);
				EditorGUILayout.EndVertical();
				}
			else
				{
				EditorGUILayout.LabelField("EntityManager not available");
				// Reset cache when not available
				_cachedWorldSeeds = -1;
				_cachedDistricts = -1;
				_cachedPolarityFields = -1;
				_cachedCurrentSeed = 0;
				}
			}

		private void PerformFullRandomRegeneration(SmokeTestSceneSetup smokeTest)
			{
			Debug.Log("🎲 FULL Random Regeneration: Randomizing all parameters for completely different world");

			// Randomize all key parameters
			var random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);

			// Randomize world size (within reasonable bounds)
			int newWorldX = random.NextInt(30, 80);
			int newWorldY = random.NextInt(30, 80);
			SetWorldSize(smokeTest, newWorldX, newWorldY);

			// Randomize sector count
			int newSectorCount = random.NextInt(3, 10);
			SetTargetSectorCount(smokeTest, newSectorCount);

			// Randomize biome transition radius
			float newBiomeRadius = random.NextFloat(5.0f, 20.0f);
			SetBiomeTransitionRadius(smokeTest, newBiomeRadius);

			// Generate completely new seed
			uint newSeed = random.NextUInt(1, uint.MaxValue);
			SetWorldSeed(smokeTest, newSeed);

			// Trigger regeneration
			TriggerRegeneration(smokeTest);

			Debug.Log($"🌟 Full randomization complete: Seed={newSeed}, World=({newWorldX},{newWorldY}), Sectors={newSectorCount}, BiomeRadius={newBiomeRadius:F1}");
			}

		private void PerformPartialRandomRegeneration(SmokeTestSceneSetup smokeTest)
			{
			Debug.Log("🔄 Partial Random Regeneration: Familiar layout with detail randomization");

			// Keep world size and major parameters, but randomize seed and minor details
			var random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);

			// Small adjustments to sector count (±1)
			int currentSectors = GetTargetSectorCount(smokeTest);
			int newSectorCount = math.max(3, currentSectors + random.NextInt(-1, 2));
			SetTargetSectorCount(smokeTest, newSectorCount);

			// Small adjustment to biome radius (±20%)
			float currentRadius = GetBiomeTransitionRadius(smokeTest);
			float radiusVariation = random.NextFloat(0.8f, 1.2f);
			float newRadius = currentRadius * radiusVariation;
			SetBiomeTransitionRadius(smokeTest, newRadius);

			// Generate new seed (this is the main randomization)
			uint newSeed = random.NextUInt(1, uint.MaxValue);
			SetWorldSeed(smokeTest, newSeed);

			// Trigger regeneration
			TriggerRegeneration(smokeTest);

			Debug.Log($"🔄 Partial randomization complete: Seed={newSeed}, Sectors={newSectorCount}, BiomeRadius={newRadius:F1}");
			}

		private void RegenerateWithCurrentSeed(SmokeTestSceneSetup smokeTest)
			{
			uint currentSeed = GetWorldSeed(smokeTest);
			Debug.Log($"🔨 Regenerating world with current seed: {currentSeed}");

			TriggerRegeneration(smokeTest);
			Debug.Log("🔨 Regeneration complete - should be identical to previous generation");
			}

		private void GenerateNewRandomSeed(SmokeTestSceneSetup smokeTest)
			{
			var random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
			uint newSeed = random.NextUInt(1, uint.MaxValue);

			Debug.Log($"🎯 Generating new random seed: {newSeed}");
			SetWorldSeed(smokeTest, newSeed);
			TriggerRegeneration(smokeTest);
			Debug.Log("🎯 New seed regeneration complete");
			}

		private void TriggerRegeneration(SmokeTestSceneSetup smokeTest)
			{
			if (Application.isPlaying)
				{
				// Runtime regeneration
				ClearAllGeneratedEntities(smokeTest);

				// Use reflection to call the private SetupSmokeTestWorld method
				System.Reflection.MethodInfo setupMethod = typeof(SmokeTestSceneSetup).GetMethod("SetupSmokeTestWorld",
					System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

				if (setupMethod != null)
					{
					setupMethod.Invoke(smokeTest, null);

					// Reset cache after regeneration to force immediate update
					_cachedWorldSeeds = -1;
					_cachedDistricts = -1;
					_cachedPolarityFields = -1;
					_cachedCurrentSeed = 0;
					_lastUpdateTime = 0f;

					Debug.Log("✅ World regeneration complete!");
					Repaint(); // Force inspector refresh
					}
				else
					{
					Debug.LogError("❌ Could not find SetupSmokeTestWorld method for regeneration");
					}
				}
			else
				{
				// Editor-time feedback
				Debug.Log("⏰ Regeneration settings applied - will take effect when you press Play");
				EditorUtility.SetDirty(smokeTest);
				}
			}

		private void ClearAllGeneratedEntities(SmokeTestSceneSetup smokeTest)
			{
			if (!Application.isPlaying || smokeTest.DefaultWorld == null || !smokeTest.DefaultWorld.IsCreated)
				return;

			Debug.Log("🧹 Clearing generated MetVanDAMN entities...");

			// More selective cleanup - only destroy MetVanDAMN-specific entities, not system entities
			EntityManager entityManager = smokeTest.EntityManager;
			int destroyedCount = 0;

			// Clear World Seed entities
			using (EntityQuery worldSeedQuery = entityManager.CreateEntityQuery(typeof(WorldSeed)))
				{
				if (worldSeedQuery.CalculateEntityCount() > 0)
					{
					entityManager.DestroyEntity(worldSeedQuery);
					destroyedCount += worldSeedQuery.CalculateEntityCount();
					}
				}

			// Clear District entities (NodeId components)
			using (EntityQuery districtQuery = entityManager.CreateEntityQuery(typeof(NodeId)))
				{
				if (districtQuery.CalculateEntityCount() > 0)
					{
					entityManager.DestroyEntity(districtQuery);
					destroyedCount += districtQuery.CalculateEntityCount();
					}
				}

			// Clear Polarity Field entities
			using (EntityQuery polarityQuery = entityManager.CreateEntityQuery(typeof(PolarityFieldData)))
				{
				if (polarityQuery.CalculateEntityCount() > 0)
					{
					entityManager.DestroyEntity(polarityQuery);
					destroyedCount += polarityQuery.CalculateEntityCount();
					}
				}

			// Clear World Config entities
			using (EntityQuery worldConfigQuery = entityManager.CreateEntityQuery(typeof(WorldSeedData)))
				{
				if (worldConfigQuery.CalculateEntityCount() > 0)
					{
					entityManager.DestroyEntity(worldConfigQuery);
					destroyedCount += worldConfigQuery.CalculateEntityCount();
					}
				}

			// Reset cache since we cleared everything
			_cachedWorldSeeds = -1;
			_cachedDistricts = -1;
			_cachedPolarityFields = -1;
			_cachedCurrentSeed = 0;
			_lastUpdateTime = 0f;

			Debug.Log($"🧹 Selective entity cleanup finished - destroyed {destroyedCount} MetVanDAMN entities");
			}

		// Helper methods to get/set private fields using reflection
		private uint GetWorldSeed(SmokeTestSceneSetup smokeTest)
			{
			System.Reflection.FieldInfo field = typeof(SmokeTestSceneSetup).GetField("worldSeed",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			return field != null ? (uint)field.GetValue(smokeTest) : 42u;
			}

		private void SetWorldSeed(SmokeTestSceneSetup smokeTest, uint seed)
			{
			System.Reflection.FieldInfo field = typeof(SmokeTestSceneSetup).GetField("worldSeed",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			field?.SetValue(smokeTest, seed);
			}

		private int GetTargetSectorCount(SmokeTestSceneSetup smokeTest)
			{
			System.Reflection.FieldInfo field = typeof(SmokeTestSceneSetup).GetField("targetSectorCount",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			return field != null ? (int)field.GetValue(smokeTest) : 5;
			}

		private void SetTargetSectorCount(SmokeTestSceneSetup smokeTest, int count)
			{
			System.Reflection.FieldInfo field = typeof(SmokeTestSceneSetup).GetField("targetSectorCount",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			field?.SetValue(smokeTest, count);
			}

		private float GetBiomeTransitionRadius(SmokeTestSceneSetup smokeTest)
			{
			System.Reflection.FieldInfo field = typeof(SmokeTestSceneSetup).GetField("biomeTransitionRadius",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			return field != null ? (float)field.GetValue(smokeTest) : 10.0f;
			}

		private void SetBiomeTransitionRadius(SmokeTestSceneSetup smokeTest, float radius)
			{
			System.Reflection.FieldInfo field = typeof(SmokeTestSceneSetup).GetField("biomeTransitionRadius",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			field?.SetValue(smokeTest, radius);
			}

		private void SetWorldSize(SmokeTestSceneSetup smokeTest, int x, int y)
			{
			System.Reflection.FieldInfo field = typeof(SmokeTestSceneSetup).GetField("worldSize",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			field?.SetValue(smokeTest, new int2(x, y));
			}
		}
	}
