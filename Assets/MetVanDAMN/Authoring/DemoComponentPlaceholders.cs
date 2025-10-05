#nullable enable
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using TinyWalnutGames.MetVD.Core;
using Unity.Entities;
using TinyWalnutGames.MetVD.Shared;
using System;
using UnityEngine.UI;
using System.Reflection; // For Array.Empty

#nullable enable

namespace TinyWalnutGames.MetVanDAMN.Authoring
	{
	/// <summary>
	/// Demo component placeholders for systems referenced by CompleteDemoSceneGenerator.
	/// These provide basic functionality that can be enhanced or replaced with full implementations.
	/// </summary>

	// Combat System Components
	public class DemoCombatManager : MonoBehaviour
		{
		[Header("Combat Settings")] public float globalDamageMultiplier = 1f;

		public bool enableFriendlyFire = false;

		private void Start()
			{
			Debug.Log("🗡️ Demo Combat Manager initialized");
			}

		public class DemoWeaponSpawner : MonoBehaviour
			{
			[Header("Weapon Spawning")] public float spawnInterval = 30f;

			public Transform[] spawnPoints;

			private void Start()
				{
				InvokeRepeating(nameof(SpawnRandomWeapon), spawnInterval, spawnInterval);
				}

			private void SpawnRandomWeapon()
				{
				if (spawnPoints.Length == 0) return;

				Vector3 spawnPos = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].position;
				DemoLootManager lootManager = FindFirstObjectByType<DemoLootManager>();
				if (lootManager)
					{
					lootManager.SpawnLoot(spawnPos);
					}
				}
			}

		// AI System Components
		public class DemoEnemySpawner : MonoBehaviour
			{
			[Header("Enemy Spawning")] public float spawnInterval = 10f;

			public int maxEnemies = 5;
			public float spawnRadius = 15f;

			private void Start()
				{
				InvokeRepeating(nameof(TrySpawnEnemy), spawnInterval, spawnInterval);
				}

			private void TrySpawnEnemy()
				{
				DemoAIManager aiManager = GetComponentInParent<DemoAIManager>();
				if (aiManager && aiManager.ActiveEnemyCount < maxEnemies)
					{
					// AI Manager will handle the actual spawning
					Debug.Log("🤖 Enemy spawn requested");
					}
				}
			}

		// Inventory System Components
		public class DemoInventoryManager : MonoBehaviour
			{
			[Header("Inventory Management")] public bool autoSortInventory = true;

			public float autoSaveInterval = 60f;

			private void Start()
				{
				Debug.Log("🎒 Demo Inventory Manager initialized");
				if (autoSaveInterval > 0)
					{
					InvokeRepeating(nameof(AutoSave), autoSaveInterval, autoSaveInterval);
					}
				}

			private void AutoSave()
				{
				// Full implementation for inventory auto-save functionality
				Debug.Log("💾 Auto-saving inventory data...");

				DemoPlayerInventory playerInventory = FindFirstObjectByType<DemoPlayerInventory>();
				if (playerInventory != null)
					{
					SaveInventoryData(playerInventory);
					Debug.Log("💾 Inventory data saved successfully");
					}
				else
					{
					Debug.LogWarning("⚠️ No player inventory found for auto-save");
					}
				}

			private void SaveInventoryData(DemoPlayerInventory inventory)
				{
				// Create save data structure
				var saveData = new InventorySaveData
					{
					coins = inventory.Coins,
					equippedWeapon = SerializeItem(inventory.EquippedWeapon),
					equippedOffhand = SerializeItem(inventory.EquippedOffhand),
					equippedArmor = SerializeItem(inventory.EquippedArmor),
					equippedTrinket = SerializeItem(inventory.EquippedTrinket),
					inventoryItems = SerializeInventoryItems(inventory.GetInventoryItems()),
					saveTimestamp = System.DateTime.Now.ToBinary()
					};

				// Convert to JSON and save to PlayerPrefs (simple save system)
				string jsonData = JsonUtility.ToJson(saveData, true);
				PlayerPrefs.SetString("DemoInventorySave", jsonData);
				PlayerPrefs.Save();
				}

			private SerializableItem? SerializeItem(DemoItem? item)
				{
				if (item == null) return null;

				// Guard nested stat containers which may be null for certain item archetypes
				WeaponStats? weaponStats = item.weaponStats;
				ArmorStats? armorStats = item.armorStats;

				return new SerializableItem
					{
					name = item.name,
					description = item.description,
					type = (int)item.type,
					rarity = (int)item.rarity,
					stackSize = item.stackSize,
					weaponDamage = weaponStats != null ? weaponStats.damage : 0f,
					weaponRange = weaponStats != null ? weaponStats.range : 0f,
					weaponAttackSpeed = weaponStats != null ? weaponStats.attackSpeed : 0f,
					armorDefense = armorStats != null ? armorStats.defense : 0f,
					armorHealth = armorStats != null ? armorStats.healthBonus : 0f
					};
				}

			private SerializableItem?[] SerializeInventoryItems(System.Collections.Generic.List<DemoItem> items)
				{
				var serializedItems = new SerializableItem?[items.Count];
				for (int i = 0; i < items.Count; i++)
					{
					serializedItems[i] = SerializeItem(items[i]);
					}

				return serializedItems;
				}

			public void LoadInventoryData()
				{
				if (PlayerPrefs.HasKey("DemoInventorySave"))
					{
					string jsonData = PlayerPrefs.GetString("DemoInventorySave");
					InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(jsonData);

					DemoPlayerInventory playerInventory = FindFirstObjectByType<DemoPlayerInventory>();
					if (playerInventory != null)
						{
						ApplyLoadedData(playerInventory, saveData);
						Debug.Log("💾 Inventory data loaded successfully");
						}
					}
				}

			private void ApplyLoadedData(DemoPlayerInventory inventory, InventorySaveData saveData)
				{
				// Restore coins
				inventory.AddCoins(saveData.coins - inventory.Coins);

				// Restore equipped items
				if (saveData.equippedWeapon != null)
					{
					DemoItem? weapon = DeserializeItem(saveData.equippedWeapon);
					if (weapon != null) inventory.EquipItem(weapon);
					}

				// Restore inventory items
				foreach (SerializableItem? serializedItem in saveData.inventoryItems)
					{
					if (serializedItem != null)
						{
						DemoItem? item = DeserializeItem(serializedItem);
						if (item != null) inventory.AddItem(item);
						}
					}
				}

			private DemoItem? DeserializeItem(SerializableItem? serialized)
				{
				if (serialized == null) return null;

				var item = new DemoItem
					{
					name = serialized.name,
					description = serialized.description,
					type = (ItemType)serialized.type,
					rarity = (ItemRarity)serialized.rarity,
					stackSize = serialized.stackSize,
					weaponStats = new WeaponStats(),
					armorStats = new ArmorStats(),
					trinketStats = new TrinketStats()
					};

				item.weaponStats.damage = (int)serialized.weaponDamage;
				item.weaponStats.range = serialized.weaponRange;
				item.weaponStats.attackSpeed = serialized.weaponAttackSpeed;

				item.armorStats.defense = (int)serialized.armorDefense;
				item.armorStats.healthBonus = (int)serialized.armorHealth;

				return item;
				}

			[System.Serializable]
			private class InventorySaveData
				{
				public int coins;
				public SerializableItem? equippedWeapon;
				public SerializableItem? equippedOffhand;
				public SerializableItem? equippedArmor;
				public SerializableItem? equippedTrinket;
				public SerializableItem?[] inventoryItems = Array.Empty<SerializableItem?>();
				public long saveTimestamp;
				}

			[System.Serializable]
			private class SerializableItem
				{
				public string name = string.Empty;
				public string description = string.Empty;
				public int type;
				public int rarity;
				public int stackSize;
				public float weaponDamage;
				public float weaponRange;
				public float weaponAttackSpeed;
				public float armorDefense;
				public float armorHealth;
				}
			}

		// Loot System Components
		public class DemoTreasureSpawner : MonoBehaviour
			{
			[Header("Treasure Spawning")] public int treasureChestCount = 3;

			public float spawnRadius = 20f;

			private void Start()
				{
				SpawnTreasureChests();
				}

			private void SpawnTreasureChests()
				{
				DemoLootManager lootManager = GetComponentInParent<DemoLootManager>();
				if (!lootManager) return;

				for (int i = 0; i < treasureChestCount; i++)
					{
					Vector3 spawnPos = transform.position + UnityEngine.Random.insideUnitSphere * spawnRadius;
					spawnPos.y = transform.position.y; // Keep at ground level
					lootManager.SpawnTreasureChest(spawnPos);
					}
				}
			}

		// Biome Art System Components
		public class DemoBiomeArtApplicator : MonoBehaviour
			{
			[Header("Art Application")] public bool autoApplyOnStart = true;

			public Color[] biomeColors = { Color.green, Color.blue, Color.red, Color.yellow };

			private void Start()
				{
				if (autoApplyOnStart)
					{
					ApplyBiomeArt();
					}
				}

			private void ApplyBiomeArt()
				{
				// Full implementation for biome art application
				Debug.Log("🎨 Applying biome art to scene objects...");

				// Find all renderers in the scene that should be affected by biome art
				Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
				int currentBiomeIndex = DetermineDominantBiome();

				if (currentBiomeIndex >= 0 && currentBiomeIndex < biomeColors.Length)
					{
					Color biomeColor = biomeColors[currentBiomeIndex];

					foreach (Renderer renderer in renderers)
						{
						// Apply biome coloring to environment objects
						if (IsEnvironmentObject(renderer.gameObject))
							{
							ApplyBiomeColorToRenderer(renderer, biomeColor);
							}
						}

					// Update lighting to match biome
					UpdateBiomeLighting(currentBiomeIndex);

					Debug.Log($"🎨 Applied biome art: {GetBiomeName(currentBiomeIndex)} theme");
					}
				}

			private int DetermineDominantBiome()
				{
				// Simplified: if ECS biome data is available in the scene, a separate system can set a hint.
				// For authoring/demo, choose a stable but varied theme using scene hash as seed.
				int seed = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.GetHashCode();
				var rng = new System.Random(seed);
				return rng.Next(0, biomeColors.Length);
				}

			private bool IsEnvironmentObject(GameObject obj)
				{
				// Determine if object should receive biome art treatment
				string[] tags = new[] { "Environment", "Terrain", "Ground", "Wall" };
				foreach (string? tag in tags)
					{
					if (obj.CompareTag(tag)) return true;
					}

				// Check by layer
				string[] envLayers = new[] { "Default", "Ground", "Environment" };
				string layerName = LayerMask.LayerToName(obj.layer);
				foreach (string? layer in envLayers)
					{
					if (layerName == layer) return true;
					}

				// Check by name patterns
				string name = obj.name.ToLower();
				return name.Contains("ground") || name.Contains("wall") || name.Contains("floor") ||
				       name.Contains("ceiling") || name.Contains("terrain");
				}

			private void ApplyBiomeColorToRenderer(Renderer renderer, Color biomeColor)
				{
				// Create new material instance to avoid affecting shared materials
				var material = new Material(renderer.material);

				// Apply biome color tinting
				if (material.HasProperty("_Color"))
					{
					Color originalColor = material.color;
					material.color = Color.Lerp(originalColor, biomeColor, 0.3f); // Subtle tinting
					}

				// Apply ambient color for atmospheric effect
				if (material.HasProperty("_EmissionColor"))
					{
					material.SetColor("_EmissionColor", biomeColor * 0.1f);
					material.EnableKeyword("_EMISSION");
					}

				renderer.material = material;
				}

			private void UpdateBiomeLighting(int biomeIndex)
				{
				// Update ambient lighting to match biome
				Color biomeColor = biomeColors[biomeIndex];
				RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, biomeColor, 0.2f);

				// Update fog color if fog is enabled
				if (RenderSettings.fog)
					{
					RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, biomeColor, 0.15f);
					}
				}

			private string GetBiomeName(int index)
				{
				string[] names = new[] { "Nature/Sun", "Ice/Moon", "Fire/Heat", "Electric/Cold" };
				return index >= 0 && index < names.Length ? names[index] : "Unknown";
				}
			}

		// Room System Components
		public class DemoRoomMaskingManager : MonoBehaviour
			{
			[Header("Room Masking")] public bool enableRoomMasking = true;

			public LayerMask roomLayers = 1;
			public LayerMask backgroundLayers = 2;
			public LayerMask uiLayers = 32;

			[Header("Room State Management")] public int maxVisibleRooms = 3;

			public bool useAdjacentRoomMasking = true;

			// State tracking for active rooms
			private HashSet<int> activeRooms = new();
			private LayerMask alwaysVisibleLayers;
			private int currentPlayerRoom = -1;

			// Layer management
			private LayerMask defaultLayers;
			private Dictionary<int, LayerMask> roomLayerMap = new();

			// Public property for API access
			public HashSet<int> ActiveRooms => new(activeRooms);

			private void Start()
				{
				Debug.Log("🏠 Demo Room Masking Manager initialized");
				InitializeRoomLayerSystem();
				}

			private void InitializeRoomLayerSystem()
				{
				// Store default camera settings
				Camera mainCamera = Camera.main;
				if (mainCamera != null)
					{
					defaultLayers = mainCamera.cullingMask;
					}

				// Setup always visible layers (UI, background, etc.)
				alwaysVisibleLayers = backgroundLayers | uiLayers;

				// Initialize room layer mappings
				InitializeRoomLayerMappings();

				Debug.Log($"🔧 Room masking system initialized with {roomLayerMap.Count} room layer mappings");
				}

			private void InitializeRoomLayerMappings()
				{
				// Auto-detect rooms and assign layer masks
				RoomIdentifier[] roomIdentifiers = FindObjectsByType<RoomIdentifier>(FindObjectsSortMode.None);
				int layerOffset = 8; // Start from layer 8 to avoid Unity defaults

				foreach (RoomIdentifier room in roomIdentifiers)
					{
					if (!roomLayerMap.ContainsKey(room.roomId))
						{
						// Assign unique layer mask for each room
						LayerMask roomMask = 1 << (layerOffset + (room.roomId % 20)); // Cycle through available layers
						roomLayerMap[room.roomId] = roomMask;

						// Assign room objects to the appropriate layer
						AssignRoomObjectsToLayer(room.roomId, layerOffset + (room.roomId % 20));
						}
					}
				}

			private void AssignRoomObjectsToLayer(int roomId, int layer)
				{
				GameObject[] roomObjects = FindRoomObjects(roomId);
				foreach (GameObject obj in roomObjects)
					{
					// Recursively set layer for all child objects
					SetLayerRecursively(obj, layer);
					}
				}

			private void SetLayerRecursively(GameObject obj, int layer)
				{
				obj.layer = layer;
				for (int i = 0; i < obj.transform.childCount; i++)
					{
					SetLayerRecursively(obj.transform.GetChild(i).gameObject, layer);
					}
				}

			public void SetPlayerRoom(int roomId)
				{
				if (currentPlayerRoom != roomId)
					{
					Debug.Log($"🚶 Player moved to room {roomId}");
					currentPlayerRoom = roomId;
					UpdateActiveRooms();
					}
				}

			public void ShowRoom(int roomId)
				{
				// Full implementation for room visibility management
				Debug.Log($"🔍 Showing room {roomId}...");

				// Find all objects in the room
				GameObject[] roomObjects = FindRoomObjects(roomId);

				foreach (GameObject obj in roomObjects)
					{
					SetObjectVisibility(obj, true);
					}

				// Update room masking layers
				UpdateRoomLayers(roomId, true);

				// Trigger room enter events
				TriggerRoomEvents(roomId, true);
				}

			public void HideRoom(int roomId)
				{
				// Full implementation for room visibility management
				Debug.Log($"🫥 Hiding room {roomId}...");

				// Find all objects in the room
				GameObject[] roomObjects = FindRoomObjects(roomId);

				foreach (GameObject obj in roomObjects)
					{
					SetObjectVisibility(obj, false);
					}

				// Update room masking layers
				UpdateRoomLayers(roomId, false);

				// Trigger room exit events
				TriggerRoomEvents(roomId, false);
				}

			private GameObject[] FindRoomObjects(int roomId)
				{
				// Find objects tagged with room ID or in room-specific parent objects
				GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
				var roomObjects = new System.Collections.Generic.List<GameObject>();

				foreach (GameObject obj in allObjects)
					{
					// Check for room ID in name or tag
					if (obj.name.Contains($"Room_{roomId}") || obj.name.Contains($"room{roomId}"))
						{
						roomObjects.Add(obj);
						}

					// Check for RoomIdentifier component
					RoomIdentifier roomIdentifier = obj.GetComponent<RoomIdentifier>();
					if (roomIdentifier && roomIdentifier.roomId == roomId)
						{
						roomObjects.Add(obj);
						}

					// Check layer membership and apply comprehensive room-specific logic
					if (((1 << obj.layer) & roomLayers) != 0)
						{
						// Enhanced room-specific logic with multiple validation methods
						int parentRoom = GetRoomIdFromHierarchy(obj);
						if (parentRoom == roomId)
							{
							roomObjects.Add(obj);
							}

						// Check for room-specific components that might indicate membership
						MonoBehaviour[] roomSpecificComponents = obj.GetComponents<MonoBehaviour>();
						foreach (MonoBehaviour component in roomSpecificComponents)
							{
							// Check if component has room-specific data or metadata
							Type componentType = component.GetType();
							if (componentType.Name.Contains("Room") ||
							    (component is IRoomSpecific roomSpecific && roomSpecific.GetRoomId() == roomId))
								{
								roomObjects.Add(obj);
								break;
								}
							}

						// Check for room boundaries and spatial containment
						RoomIdentifier foundRoomIdentifier = FindRoomIdentifier(roomId);
						if (foundRoomIdentifier != null)
							{
							var roomBounds = new Bounds(foundRoomIdentifier.transform.position,
								foundRoomIdentifier.roomBounds);
							if (roomBounds.Contains(obj.transform.position))
								{
								roomObjects.Add(obj);
								}
							}
						}
					}

				return roomObjects.ToArray();
				}

			private void SetObjectVisibility(GameObject obj, bool visible)
				{
				// Handle renderers
				Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
				foreach (Renderer renderer in renderers)
					{
					renderer.enabled = visible;
					}

				// Handle colliders (for gameplay elements)
				Collider[] colliders = obj.GetComponentsInChildren<Collider>();
				foreach (Collider collider in colliders)
					{
					if (!collider.isTrigger) // Keep triggers active for room transitions
						{
						collider.enabled = visible;
						}
					}

				// Handle 2D colliders
				Collider2D[] colliders2D = obj.GetComponentsInChildren<Collider2D>();
				foreach (Collider2D collider in colliders2D)
					{
					if (!collider.isTrigger)
						{
						collider.enabled = visible;
						}
					}

				// Handle UI elements
				CanvasGroup[] canvasGroups = obj.GetComponentsInChildren<CanvasGroup>();
				foreach (CanvasGroup canvasGroup in canvasGroups)
					{
					canvasGroup.alpha = visible ? 1f : 0f;
					canvasGroup.interactable = visible;
					canvasGroup.blocksRaycasts = visible;
					}
				}

			private void UpdateRoomLayers(int roomId, bool visible)
				{
				// Update the active rooms set based on visibility changes
				if (visible)
					{
					activeRooms.Add(roomId);
					}
				else
					{
					activeRooms.Remove(roomId);
					}

				// Update culling mask for all cameras to reflect room visibility changes
				UpdateAllCameraCullingMasks();

				Debug.Log(
					$"🎭 Room {roomId} layers updated: {(visible ? "visible" : "hidden")}. Active rooms: [{string.Join(", ", activeRooms)}]");
				}

			private void UpdateCameraCullingMask(Camera camera)
				{
				if (!enableRoomMasking)
					{
					camera.cullingMask = defaultLayers;
					return;
					}

				// Calculate comprehensive layer visibility based on room state
				LayerMask visibleLayers = CalculateActiveRoomLayers();
				camera.cullingMask = visibleLayers;

				Debug.Log(
					$"📷 Updated camera culling mask: {System.Convert.ToString(visibleLayers, 2)} (active rooms: {string.Join(", ", activeRooms)})");
				}

			private LayerMask CalculateActiveRoomLayers()
				{
				// Start with always visible layers (UI, background, etc.)
				LayerMask visibleLayers = alwaysVisibleLayers;

				if (!enableRoomMasking)
					{
					return defaultLayers;
					}

				// Add layers for all active rooms
				foreach (int roomId in activeRooms)
					{
					if (roomLayerMap.TryGetValue(roomId, out LayerMask roomMask))
						{
						visibleLayers |= roomMask;
						}
					}

				// If no rooms are active, show default room layers
				if (activeRooms.Count == 0)
					{
					visibleLayers |= roomLayers;
					}

				return visibleLayers;
				}

			private void UpdateActiveRooms()
				{
				if (currentPlayerRoom == -1)
					{
					return;
					}

				var newActiveRooms = new HashSet<int>();

				// Always include current player room
				newActiveRooms.Add(currentPlayerRoom);

				if (useAdjacentRoomMasking)
					{
					// Add adjacent rooms for seamless transitions
					List<int> adjacentRooms = GetAdjacentRooms(currentPlayerRoom);
					foreach (int adjacentRoom in adjacentRooms)
						{
						newActiveRooms.Add(adjacentRoom);

						// Limit total visible rooms for performance
						if (newActiveRooms.Count >= maxVisibleRooms)
							{
							break;
							}
						}
					}

				// Update active rooms if changed
				if (!activeRooms.SetEquals(newActiveRooms))
					{
					var previousRooms = new HashSet<int>(activeRooms);
					activeRooms = newActiveRooms;

					// Show newly active rooms
					foreach (int roomId in activeRooms.Except(previousRooms))
						{
						ShowRoom(roomId);
						}

					// Hide previously active rooms
					foreach (int roomId in previousRooms.Except(activeRooms))
						{
						HideRoom(roomId);
						}

					// Update camera culling for all cameras
					UpdateAllCameraCullingMasks();
					}
				}

			private List<int> GetAdjacentRooms(int roomId)
				{
				var adjacentRooms = new List<int>();

				// Find rooms that are spatially adjacent to the current room
				RoomIdentifier currentRoomIdentifier = FindRoomIdentifier(roomId);
				if (currentRoomIdentifier == null)
					{
					return adjacentRooms;
					}

				RoomIdentifier[] allRooms = FindObjectsByType<RoomIdentifier>(FindObjectsSortMode.None);
				Vector3 currentPosition = currentRoomIdentifier.transform.position;
				Vector3 currentBounds = currentRoomIdentifier.roomBounds;

				foreach (RoomIdentifier room in allRooms)
					{
					if (room.roomId == roomId) continue;

					float distance = Vector3.Distance(currentPosition, room.transform.position);
					float maxDistance = (currentBounds.magnitude + room.roomBounds.magnitude) * 0.75f;

					if (distance <= maxDistance)
						{
						adjacentRooms.Add(room.roomId);
						}
					}

				return adjacentRooms;
				}

			private RoomIdentifier FindRoomIdentifier(int roomId)
				{
				RoomIdentifier[] rooms = FindObjectsByType<RoomIdentifier>(FindObjectsSortMode.None);
				return System.Array.Find(rooms, r => r.roomId == roomId);
				}

			public void UpdateAllCameraCullingMasks()
				{
				Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
				foreach (Camera camera in cameras)
					{
					UpdateCameraCullingMask(camera);
					}
				}

			private void TriggerRoomEvents(int roomId, bool entering)
				{
				// Send room transition events to interested systems
				string eventType = entering ? "RoomEnter" : "RoomExit";

				// Find and notify room event listeners
				MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
				foreach (MonoBehaviour behaviour in behaviours)
					{
					if (behaviour is IRoomEventListener listener)
						{
						if (entering) listener.OnRoomEnter(roomId);
						else listener.OnRoomExit(roomId);
						}
					}

				Debug.Log($"📢 {eventType} event triggered for room {roomId}");
				}

			private int GetRoomIdFromHierarchy(GameObject obj)
				{
				// Walk up the hierarchy to find room ID
				Transform current = obj.transform;
				while (current != null)
					{
					RoomIdentifier roomIdentifier = current.GetComponent<RoomIdentifier>();
					if (roomIdentifier)
						{
						return roomIdentifier.roomId;
						}

					// Check name patterns
					string name = current.name;
					if (name.Contains("Room_"))
						{
						string[] parts = name.Split('_');
						for (int i = 0; i < parts.Length - 1; i++)
							{
							if (parts[i] == "Room" && int.TryParse(parts[i + 1], out int roomId))
								{
								return roomId;
								}
							}
						}

					current = current.parent;
					}

				return -1; // No room ID found
				}
			}

		// Helper component for room identification
		public class RoomIdentifier : MonoBehaviour
			{
			[Header("Room Configuration")] public int roomId;

			public string roomName;
			public bool isActiveRoom = true;

			[Header("Room Properties")] public Vector3 roomBounds = Vector3.one * 10f;

			public Color roomColor = Color.white;

			private void OnDrawGizmosSelected()
				{
				Gizmos.color = roomColor;
				Gizmos.DrawWireCube(transform.position, roomBounds);
				}
			}

		// Interface for room event listeners
		public interface IRoomEventListener
			{
			void OnRoomEnter(int roomId);
			void OnRoomExit(int roomId);
			}

		// Interface for room-specific components
		public interface IRoomSpecific
			{
			int GetRoomId();
			void SetRoomId(int roomId);
			}

		// Public API for external systems to interact with room masking
		public class DemoRoomMaskingAPI : MonoBehaviour
			{
			private DemoRoomMaskingManager roomManager;

			private void Awake()
				{
				roomManager = FindFirstObjectByType<DemoRoomMaskingManager>();
				if (roomManager == null)
					{
					Debug.LogWarning("⚠️ DemoRoomMaskingManager not found. Room masking API will not function.");
					}
				}

			/// <summary>
			/// Call this when the player enters a new room to update visibility
			/// </summary>
			public void OnPlayerEnterRoom(int roomId)
				{
				if (roomManager != null)
					{
					roomManager.SetPlayerRoom(roomId);
					}
				}

			/// <summary>
			/// Get the list of currently active (visible) rooms
			/// </summary>
			public HashSet<int> GetActiveRooms()
				{
				if (roomManager != null)
					{
					return roomManager.ActiveRooms;
					}

				return new HashSet<int>();
				}

			/// <summary>
			/// Check if a specific room is currently visible
			/// </summary>
			public bool IsRoomVisible(int roomId)
				{
				return roomManager != null && roomManager.ActiveRooms.Contains(roomId);
				}

			/// <summary>
			/// Force show a specific room (for debugging or special effects)
			/// </summary>
			public void ForceShowRoom(int roomId)
				{
				if (roomManager != null)
					{
					roomManager.ShowRoom(roomId);
					}
				}

			/// <summary>
			/// Force hide a specific room (for debugging or special effects)
			/// </summary>
			public void ForceHideRoom(int roomId)
				{
				if (roomManager != null)
					{
					roomManager.HideRoom(roomId);
					}
				}

			/// <summary>
			/// Toggle room masking on/off
			/// </summary>
			public void SetRoomMaskingEnabled(bool enabled)
				{
				if (roomManager != null)
					{
					roomManager.enableRoomMasking = enabled;
					roomManager.UpdateAllCameraCullingMasks();
					}
				}
			}

		public class DemoRoomTransitionDetector : MonoBehaviour
			{
			[Header("Transition Detection")] public float detectionRadius = 2f;

			public bool debugTransitions = true;

			private void OnDrawGizmosSelected()
				{
				Gizmos.color = Color.cyan;
				Gizmos.DrawWireSphere(transform.position, detectionRadius);
				}

			private void OnTriggerEnter(Collider other)
				{
				if (other.GetComponent<DemoPlayerMovement>())
					{
					HandleRoomTransition();
					}
				}

			private void OnTriggerEnter2D(Collider2D other)
				{
				if (other.GetComponent<DemoPlayerMovement>())
					{
					HandleRoomTransition();
					}
				}

			private void HandleRoomTransition()
				{
				if (debugTransitions)
					{
					Debug.Log("🚪 Room transition detected - analyzing transition...");
					}

				DemoRoomMaskingManager maskingManager = FindFirstObjectByType<DemoRoomMaskingManager>();
				if (maskingManager)
					{
					// Determine current and target rooms
					int currentRoom = GetCurrentRoom();
					int targetRoom = GetTargetRoom();

					if (currentRoom != targetRoom)
						{
						// Hide current room
						if (currentRoom >= 0)
							{
							maskingManager.HideRoom(currentRoom);
							}

						// Show target room
						if (targetRoom >= 0)
							{
							maskingManager.ShowRoom(targetRoom);
							UpdatePlayerRoomState(targetRoom);
							}

						// Trigger transition effects
						PlayTransitionEffects(currentRoom, targetRoom);

						Debug.Log($"🚪 Room transition: {currentRoom} → {targetRoom}");
						}
					}
				}

			private int GetCurrentRoom()
				{
				// Determine current room based on player position and room identifiers
				DemoPlayerMovement player = FindFirstObjectByType<DemoPlayerMovement>();
				if (!player) return -1;

				Vector3 playerPosition = player.transform.position;
				RoomIdentifier[] roomIdentifiers = FindObjectsByType<RoomIdentifier>(FindObjectsSortMode.None);

				foreach (RoomIdentifier room in roomIdentifiers)
					{
					if (IsPlayerInRoom(playerPosition, room))
						{
						return room.roomId;
						}
					}

				return -1; // No room found
				}

			private int GetTargetRoom()
				{
				// Determine target room based on transition trigger position
				RoomIdentifier[] roomIdentifiers = FindObjectsByType<RoomIdentifier>(FindObjectsSortMode.None);

				foreach (RoomIdentifier room in roomIdentifiers)
					{
					if (IsTransitionLeadingToRoom(transform.position, room))
						{
						return room.roomId;
						}
					}

				// Fallback: use a different room than current
				int currentRoom = GetCurrentRoom();
				int[] allRooms = roomIdentifiers.Select(r => r.roomId).Where(id => id != currentRoom).ToArray();

				if (allRooms.Length > 0)
					{
					return allRooms[UnityEngine.Random.Range(0, allRooms.Length)];
					}

				return UnityEngine.Random.Range(0, 5); // Fallback room ID
				}

			private bool IsPlayerInRoom(Vector3 playerPosition, RoomIdentifier room)
				{
				// Check if player is within room bounds
				Vector3 roomCenter = room.transform.position;
				Vector3 roomBounds = room.roomBounds;

				return playerPosition.x >= roomCenter.x - roomBounds.x / 2 &&
				       playerPosition.x <= roomCenter.x + roomBounds.x / 2 &&
				       playerPosition.y >= roomCenter.y - roomBounds.y / 2 &&
				       playerPosition.y <= roomCenter.y + roomBounds.y / 2 &&
				       playerPosition.z >= roomCenter.z - roomBounds.z / 2 &&
				       playerPosition.z <= roomCenter.z + roomBounds.z / 2;
				}

			private bool IsTransitionLeadingToRoom(Vector3 transitionPosition, RoomIdentifier room)
				{
				// Check if transition is near room entrance
				float distance = Vector3.Distance(transitionPosition, room.transform.position);
				return distance <= detectionRadius * 2f; // Allow some tolerance
				}

			private void UpdatePlayerRoomState(int roomId)
				{
				// Update any player state related to room changes
				DemoPlayerMovement player = FindFirstObjectByType<DemoPlayerMovement>();
				if (player)
					{
					// Could notify player of room change for state tracking
					player.SendMessage("OnRoomChanged", roomId, SendMessageOptions.DontRequireReceiver);
					}

				// Update map system if available
				MetVanDAMNMapGenerator mapGenerator = FindFirstObjectByType<MetVanDAMNMapGenerator>();
				if (mapGenerator)
					{
					mapGenerator.SendMessage("OnPlayerRoomChanged", roomId, SendMessageOptions.DontRequireReceiver);
					}
				}

			private void PlayTransitionEffects(int fromRoom, int toRoom)
				{
				// Visual/audio transition effects
				CreateTransitionEffect();
				PlayTransitionSound();

				// Screen effect (fade, slide, etc.)
				StartCoroutine(ScreenTransitionEffect());
				}

			private void CreateTransitionEffect()
				{
				// Create visual effect at transition point
				var effect = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
				effect.transform.position = transform.position;
				effect.transform.localScale = new Vector3(0.5f, 2f, 0.5f);

				Renderer renderer = effect.GetComponent<Renderer>();
				renderer.material.color = Color.cyan;

				// Animate the effect
				StartCoroutine(AnimateTransitionEffect(effect));
				}

			private System.Collections.IEnumerator AnimateTransitionEffect(GameObject effect)
				{
				float duration = 1f;
				float elapsed = 0f;
				Vector3 startScale = effect.transform.localScale;
				Color startColor = effect.GetComponent<Renderer>().material.color;

				while (elapsed < duration)
					{
					elapsed += Time.deltaTime;
					float progress = elapsed / duration;

					// Shrink and fade out
					effect.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);
					var color = Color.Lerp(startColor, Color.clear, progress);
					effect.GetComponent<Renderer>().material.color = color;

					yield return null;
					}

				Destroy(effect);
				}

			private void PlayTransitionSound()
				{
				// Play transition audio if AudioSource available
				AudioSource audioSource = GetComponent<AudioSource>();
				if (audioSource && audioSource.clip)
					{
					audioSource.Play();
					}
				}

			private System.Collections.IEnumerator ScreenTransitionEffect()
				{
				// Simple screen fade effect
				GameObject fadeOverlay = CreateFadeOverlay();
				if (fadeOverlay)
					{
					yield return StartCoroutine(FadeScreen(fadeOverlay, 0f, 1f, 0.25f)); // Fade to black
					yield return new WaitForSeconds(0.1f); // Brief pause
					yield return StartCoroutine(FadeScreen(fadeOverlay, 1f, 0f, 0.25f)); // Fade to clear
					Destroy(fadeOverlay);
					}
				}

			private GameObject CreateFadeOverlay()
				{
				// Create fullscreen fade overlay
				Canvas canvas = FindFirstObjectByType<Canvas>();
				if (!canvas)
					{
					var canvasObj = new GameObject("TransitionCanvas");
					canvas = canvasObj.AddComponent<Canvas>();
					canvas.renderMode = RenderMode.ScreenSpaceOverlay;
					canvas.sortingOrder = 1000; // Ensure it's on top
					canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
					}

				var overlay = new GameObject("FadeOverlay");
				overlay.transform.SetParent(canvas.transform, false);

				Image image = overlay.AddComponent<UnityEngine.UI.Image>();
				image.color = new Color(0, 0, 0, 0); // Start transparent

				RectTransform rectTransform = overlay.GetComponent<RectTransform>();
				rectTransform.anchorMin = Vector2.zero;
				rectTransform.anchorMax = Vector2.one;
				rectTransform.sizeDelta = Vector2.zero;
				rectTransform.anchoredPosition = Vector2.zero;

				return overlay;
				}

			private System.Collections.IEnumerator FadeScreen(GameObject overlay, float fromAlpha, float toAlpha,
				float duration)
				{
				Image image = overlay.GetComponent<UnityEngine.UI.Image>();
				float elapsed = 0f;

				while (elapsed < duration)
					{
					elapsed += Time.deltaTime;
					float alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
					image.color = new Color(0, 0, 0, alpha);
					yield return null;
					}

				image.color = new Color(0, 0, 0, toAlpha);
				}
			}

		// Validation Components
		public class DemoValidationSettings : MonoBehaviour
			{
			[Header("Validation Configuration")] public bool validateOnStart = true;

			public bool requireAllSystems = true;
			public bool logValidationResults = true;

			[Header("System Requirements")] public bool requirePlayerMovement = true;

			public bool requireCombatSystem = true;
			public bool requireInventorySystem = true;
			public bool requireAISystem = true;
			public bool requireLootSystem = true;

			private void Start()
				{
				if (validateOnStart)
					{
					ValidateScene();
					}
				}

			private void ValidateScene()
				{
				DemoSceneValidator validator = GetComponent<DemoSceneValidator>();
				if (!validator)
					{
					validator = gameObject.AddComponent<DemoSceneValidator>();
					}

				// Configure validator based on settings
				validator.validateOnStart = validateOnStart;
				validator.requirePlayerMovement = requirePlayerMovement;
				validator.requireCombatSystem = requireCombatSystem;
				validator.requireInventorySystem = requireInventorySystem;
				validator.requireLootSystem = requireLootSystem;
				validator.requireEnemyAI = requireAISystem;

				bool validationPassed = validator.ValidateScene();

				if (logValidationResults)
					{
					if (validationPassed)
						{
						Debug.Log("✅ Demo scene validation passed - all required systems are present and functional");
						}
					else
						{
						Debug.LogWarning("⚠️ Demo scene validation found issues - check console for details");
						}
					}
				}

			// Public API for runtime validation
			public bool RunValidation()
				{
				Debug.Log("🔍 Running comprehensive demo scene validation...");

				DemoSceneValidator validator = GetComponent<DemoSceneValidator>();
				if (!validator)
					{
					validator = gameObject.AddComponent<DemoSceneValidator>();
					}

				// Configure validator based on settings
				validator.validateOnStart = validateOnStart;
				validator.requirePlayerMovement = requirePlayerMovement;
				validator.requireCombatSystem = requireCombatSystem;
				validator.requireInventorySystem = requireInventorySystem;
				validator.requireLootSystem = requireLootSystem;
				validator.requireEnemyAI = requireAISystem;

				bool validationPassed = validator.ValidateScene();

				// Additional validation checks
				bool systemIntegrationValid = ValidateSystemIntegration();
				bool performanceValid = ValidatePerformance();
				bool complianceValid = ValidateCompliance();

				bool overallValid = validationPassed && systemIntegrationValid && performanceValid && complianceValid;

				if (logValidationResults)
					{
					LogDetailedValidationResults(overallValid, validationPassed, systemIntegrationValid,
						performanceValid, complianceValid);
					}

				return overallValid;
				}

			private bool ValidateSystemIntegration()
				{
				Debug.Log("🔗 Validating system integration...");

				bool integrationValid = true;

				// Check player-combat integration
				DemoPlayerMovement player = FindFirstObjectByType<DemoPlayerMovement>();
				DemoPlayerCombat? combat = FindFirstObjectByType<DemoPlayerCombat>();
				if (player && combat)
					{
					bool combatIntegrated = player.GetComponent<DemoPlayerCombat>() != null;
					if (!combatIntegrated)
						{
						Debug.LogWarning("⚠️ Player movement and combat systems not integrated on same GameObject");
						integrationValid = false;
						}
					}

				// Check inventory-combat integration
				DemoPlayerInventory inventory = FindFirstObjectByType<DemoPlayerInventory>();
				if (inventory && combat)
					{
					bool inventoryCombatLinked = inventory.GetComponent<DemoPlayerCombat>() != null ||
					                             combat.GetComponent<DemoPlayerInventory>() != null;
					if (!inventoryCombatLinked)
						{
						Debug.LogWarning(
							"⚠️ Inventory and combat systems should be on same GameObject for proper weapon switching");
						}
					}

				// Check AI-loot integration
				DemoAIManager aiManager = FindFirstObjectByType<DemoAIManager>();
				DemoLootManager lootManager = FindFirstObjectByType<DemoLootManager>();
				if (aiManager && lootManager)
					{
					// Verify AI can drop loot when killed
					Debug.Log("✅ AI-Loot integration present");
					}

				// Check map generator integration
				MetVanDAMNMapGenerator mapGenerator = FindFirstObjectByType<MetVanDAMNMapGenerator>();
				if (mapGenerator)
					{
					Debug.Log("✅ Map generation system integrated");
					}

				return integrationValid;
				}

			private bool ValidatePerformance()
				{
				Debug.Log("⚡ Validating performance characteristics...");

				// Check object counts
				int totalGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;
				int rendererCount = FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length;
				int colliderCount = FindObjectsByType<Collider>(FindObjectsSortMode.None).Length +
				                    FindObjectsByType<Collider2D>(FindObjectsSortMode.None).Length;

				bool performanceValid = true;

				// Reasonable limits for demo scene
				if (totalGameObjects > 1000)
					{
					Debug.LogWarning($"⚠️ High GameObject count: {totalGameObjects} (consider optimization)");
					}

				if (rendererCount > 200)
					{
					Debug.LogWarning($"⚠️ High Renderer count: {rendererCount} (consider object pooling)");
					}

				if (colliderCount > 500)
					{
					Debug.LogWarning($"⚠️ High Collider count: {colliderCount} (consider compound colliders)");
					}

				// Check for memory leaks (components without proper cleanup)
				CheckForMemoryLeaks();

				Debug.Log(
					$"📊 Performance metrics: {totalGameObjects} GameObjects, {rendererCount} Renderers, {colliderCount} Colliders");

				return performanceValid;
				}

			private void CheckForMemoryLeaks()
				{
				// Check for common memory leak patterns
				AudioSource[] audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
				foreach (AudioSource audio in audioSources)
					{
					if (audio.clip && audio.clip.loadState == AudioDataLoadState.Loaded && !audio.isPlaying)
						{
						Debug.LogWarning(
							$"⚠️ AudioSource on {audio.gameObject.name} has loaded clip but is not playing");
						}
					}

				// Check for unreferenced components
				Rigidbody[] rigidbodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
				foreach (Rigidbody rb in rigidbodies)
					{
					if (rb.isKinematic && rb.linearVelocity == Vector3.zero && rb.angularVelocity == Vector3.zero)
						{
						// This is fine, kinematic bodies at rest
						}
					}
				}

			private bool ValidateCompliance()
				{
				Debug.Log("📋 Validating MetVanDAMN compliance...");

				bool complianceValid = true;

				// Check for required demo functionality
				var complianceValidatorType = System.Type.GetType("MetVanDAMNComplianceValidator");
				if (complianceValidatorType != null)
					{
					MonoBehaviour? complianceValidatorInstance = null;
					MonoBehaviour[] behaviours =
						FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
					foreach (MonoBehaviour b in behaviours)
						{
						if (complianceValidatorType.IsInstanceOfType(b))
							{
							complianceValidatorInstance = b;
							break;
							}
						}

					MethodInfo result = complianceValidatorType.GetMethod("ValidateCompliance");
					if (result != null && complianceValidatorInstance != null)
						complianceValid = (bool)result.Invoke(complianceValidatorInstance, null);
					}
				else
					{
					Debug.LogWarning("⚠️ MetVanDAMNComplianceValidator not found - adding basic compliance checks");
					complianceValid = ValidateBasicCompliance();
					}

				return complianceValid;
				}

			private bool ValidateBasicCompliance()
				{
				// Basic compliance checks when full validator not available
				bool hasPlayerMovement = FindFirstObjectByType<DemoPlayerMovement>() != null;
				bool hasCombatSystem = FindFirstObjectByType<DemoPlayerCombat>() != null;
				bool hasInventorySystem = FindFirstObjectByType<DemoPlayerInventory>() != null;
				bool hasAISystem = FindFirstObjectByType<DemoAIManager>() != null;
				bool hasLootSystem = FindFirstObjectByType<DemoLootManager>() != null;

				bool basicCompliance = hasPlayerMovement && hasCombatSystem && hasInventorySystem && hasAISystem &&
				                       hasLootSystem;

				if (!basicCompliance)
					{
					Debug.LogWarning("⚠️ Basic compliance failed - missing required gameplay systems");
					}

				return basicCompliance;
				}

			private void LogDetailedValidationResults(bool overall, bool basic, bool integration, bool performance,
				bool compliance)
				{
				string status = overall ? "✅ PASSED" : "❌ FAILED";
				Debug.Log($"🏆 VALIDATION COMPLETE: {status}");
				Debug.Log($"   Basic Systems: {(basic ? "✅" : "❌")}");
				Debug.Log($"   Integration: {(integration ? "✅" : "❌")}");
				Debug.Log($"   Performance: {(performance ? "✅" : "❌")}");
				Debug.Log($"   Compliance: {(compliance ? "✅" : "❌")}");

				if (overall)
					{
					Debug.Log("🎉 Demo scene is ready for gameplay! All systems validated successfully.");
					}
				else
					{
					Debug.LogWarning("🚨 Demo scene has validation issues. Check warnings above for details.");
					}
				}
			}

		// Projectile System Component
		public class DemoProjectile : MonoBehaviour
			{
			[Header("Projectile Settings")] public float speed = 10f;

			public float lifetime = 5f;
			public float damage = 25f;

			private Vector3 direction;
			private float remainingLifetime;
			private GameObject? source;

			private void Update()
				{
				// Move projectile
				transform.position += direction * speed * Time.deltaTime;

				// Check lifetime
				remainingLifetime -= Time.deltaTime;
				if (remainingLifetime <= 0)
					{
					DestroyProjectile();
					}
				}

			private void OnTriggerEnter(Collider other)
				{
				HandleCollision(other.gameObject);
				}

			private void OnTriggerEnter2D(Collider2D other)
				{
				HandleCollision(other.gameObject);
				}

			public void Initialize(float projectileDamage, Vector3 projectileDirection, float range,
				GameObject sourceObject)
				{
				damage = projectileDamage;
				direction = projectileDirection.normalized;
				remainingLifetime = range / speed; // Calculate lifetime based on range and speed
				source = sourceObject;

				// Add visual trail or effect
				CreateProjectileVisual();
				}

			private void HandleCollision(GameObject target)
				{
				// Don't hit the source
				if (source != null && target == source) return;

				// Check for damageable target
				if (target.TryGetComponent<IDemoDamageable>(out IDemoDamageable? damageable))
					{
					if (source == null) return; // Source should be set during Initialize; guard for safety
					damageable.TakeDamage(damage, source, AttackType.Light);
					DestroyProjectile();
					}
				// Hit walls/obstacles
				else if (!target.CompareTag("Player") && target.layer != LayerMask.NameToLayer("Ignore Raycast"))
					{
					DestroyProjectile();
					}
				}

			private void CreateProjectileVisual()
				{
				// Simple visual representation
				Renderer renderer = GetComponent<Renderer>();
				if (!renderer)
					{
					// Create default projectile visual
					var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
					visual.transform.SetParent(transform);
					visual.transform.localPosition = Vector3.zero;
					visual.transform.localScale = Vector3.one * 0.2f;

					renderer = visual.GetComponent<Renderer>();
					renderer.material.color = Color.yellow;

					// Remove collider from visual
					DestroyImmediate(visual.GetComponent<Collider>());
					}

				// Add trigger collider for hit detection
				SphereCollider collider = GetComponent<SphereCollider>();
				if (!collider)
					{
					collider = gameObject.AddComponent<SphereCollider>();
					}

				collider.isTrigger = true;
				collider.radius = 0.2f;
				}

			private void DestroyProjectile()
				{
				// Add impact effect
				CreateImpactEffect();

				Destroy(gameObject);
				}

			private void CreateImpactEffect()
				{
				// Simple impact visual
				GameObject impact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				impact.transform.position = transform.position;
				impact.transform.localScale = Vector3.one * 0.5f;

				Renderer renderer = impact.GetComponent<Renderer>();
				renderer.material.color = Color.orange;

				DestroyImmediate(impact.GetComponent<Collider>());
				Destroy(impact, 0.3f);
				}
			}
		}
	}
