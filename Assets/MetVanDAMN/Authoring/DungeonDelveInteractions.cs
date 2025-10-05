#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace TinyWalnutGames.MetVanDAMN.Authoring
	{
	/// <summary>
	/// Dungeon progression lock component - biome-themed locks that gate progress through floors.
	/// Each lock is tied to a specific floor and requires specific conditions to unlock.
	/// </summary>
	public class DungeonProgressionLock : MonoBehaviour
		{
		[Header("Lock Configuration")] [SerializeField]
		private int floorIndex;

		[SerializeField] private string lockName;
		[SerializeField] private bool isUnlocked = false;
		[SerializeField] private float interactionRange = 2f;

		[Header("Visual Effects")] [SerializeField]
		private GameObject unlockedEffect;

		[SerializeField] private AudioClip unlockSound;
		private bool _baselineReady;
		private GameObject _interactionPrompt = null!;
		private Text _interactionPromptText = null!;
		private bool _playerResolved;

		// Nullability Annihilation: all runtime refs are deterministically established.
		private DungeonDelveMode dungeonMode = null!; // Assigned via Initialize
		private Material lockedMaterial = null!;
		private Renderer lockRenderer = null!; // Optional; guarded usage
		private bool playerInRange = false;
		private Transform playerTransform = null!; // Placeholder until resolved
		private Material unlockedMaterial = null!;

		public bool IsUnlocked => isUnlocked;
		public string LockName => lockName;
		public int FloorIndex => floorIndex;

		private void Awake()
			{
			EnsureBaseline();
			}

		private void Update()
			{
			if (!_playerResolved) ResolvePlayer();
			if (isUnlocked || !_playerResolved) return;

			// Check if player is in range
			float distance = Vector3.Distance(transform.position, playerTransform.position);
			bool wasInRange = playerInRange;
			playerInRange = distance <= interactionRange;

			// Show interaction prompt when player enters range
			if (playerInRange && !wasInRange)
				{
				ShowInteractionPrompt();
				}
			else if (!playerInRange && wasInRange)
				{
				HideInteractionPrompt();
				}

			// Handle interaction input
			if (playerInRange && Input.GetKeyDown(KeyCode.E))
				{
				TryUnlock();
				}
			}

		private void OnDrawGizmosSelected()
			{
			// Draw interaction range
			Gizmos.color = isUnlocked ? Color.green : Color.red;
			Gizmos.DrawWireSphere(transform.position, interactionRange);
			}

		private void EnsureBaseline()
			{
			if (_baselineReady) return;
			// Placeholder player transform so field never null
			var placeholder = new GameObject("__ProgressionLock_PlaceholderPlayer__");
			placeholder.hideFlags = HideFlags.HideAndDontSave;
			playerTransform = placeholder.transform;

			// Pre-create prompt (hidden)
			Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
			if (canvas == null)
				{
				var cgo = new GameObject("DungeonUI_Canvas_Auto");
				canvas = cgo.AddComponent<Canvas>();
				canvas.renderMode = RenderMode.ScreenSpaceOverlay;
				cgo.AddComponent<CanvasScaler>();
				cgo.AddComponent<GraphicRaycaster>();
				}

			_interactionPrompt = new GameObject($"LockPrompt_{GetInstanceID()}");
			_interactionPrompt.transform.SetParent(canvas.transform, false);
			_interactionPromptText = _interactionPrompt.AddComponent<Text>();
			_interactionPromptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			_interactionPromptText.fontSize = 18;
			_interactionPromptText.color = Color.yellow;
			_interactionPromptText.alignment = TextAnchor.MiddleCenter;
			RectTransform rt = _interactionPromptText.GetComponent<RectTransform>();
			rt.anchorMin = new Vector2(0.5f, 0.8f);
			rt.anchorMax = new Vector2(0.5f, 0.8f);
			rt.anchoredPosition = Vector2.zero;
			rt.sizeDelta = new Vector2(300, 30);
			_interactionPrompt.SetActive(false);

			_baselineReady = true;

			Debug.Log($"🔒 Progression lock baseline initialized for '{GetInstanceID()}'");
			}

		public void Initialize(int floor, string name, DungeonDelveMode mode)
			{
			EnsureBaseline();
			floorIndex = floor;
			lockName = name;
			dungeonMode = mode;

			// Find player
			ResolvePlayer();

			// Setup materials
			lockRenderer = GetComponent<Renderer>();
			if (lockRenderer)
				{
				lockedMaterial = lockRenderer.material;
				unlockedMaterial = new Material(lockedMaterial);
				unlockedMaterial.color = Color.green;
				}

			Debug.Log($"🔒 Progression lock '{lockName}' configured for floor {floor + 1}");
			}

		private void ShowInteractionPrompt()
			{
			_interactionPromptText.text = $"Press E to unlock {lockName}";
			_interactionPrompt.SetActive(true);
			}

		private void HideInteractionPrompt() => _interactionPrompt.SetActive(false);

		private void ResolvePlayer()
			{
			if (_playerResolved) return;
			DemoPlayerMovement pm = FindFirstObjectByType<DemoPlayerMovement>();
			if (pm)
				{
				playerTransform = pm.transform;
				_playerResolved = true;
				}
			}

		private void TryUnlock()
			{
			if (isUnlocked) return;

			// Check if player has the required conditions to unlock
			// For demo purposes, we'll unlock based on floor progression
			bool canUnlock = CheckUnlockConditions();

			if (canUnlock)
				{
				UnlockLock();
				}
			else
				{
				Debug.Log($"🚫 Cannot unlock {lockName} - requirements not met");
				ShowUnlockRequirements();
				}
			}

		private bool CheckUnlockConditions()
			{
			// First lock can always be unlocked (entry to dungeon)
			if (floorIndex == 0) return true;

			if (dungeonMode == null) return false;
			bool floorProgressMet = dungeonMode.CurrentFloor >= floorIndex;
			bool hasRequiredProgression = ValidateSpecificUnlockRequirements();
			return floorProgressMet && hasRequiredProgression;
			}

		private bool ValidateSpecificUnlockRequirements()
			{
			switch (floorIndex)
				{
					case 0: // Crystal Key - always available at start
						return true;

					case 1: // Flame Essence - requires completing Crystal Caverns
						// Check if player has explored sufficient areas on previous floor
						if (dungeonMode != null)
							{
							int secretsFound = dungeonMode.TotalSecretsFound;
							return secretsFound >= 1; // Must find at least 1 secret to prove exploration
							}

						return false;

					case 2: // Void Core - requires completing Molten Depths
						// Check if player has demonstrated mastery (found multiple secrets, defeated bosses)
						if (dungeonMode != null)
							{
							int secretsFound = dungeonMode.TotalSecretsFound;
							bool sufficientExploration = secretsFound >= 3; // Must find secrets on multiple floors
							bool adequateProgress = dungeonMode.CurrentFloor >= 2;
							return sufficientExploration && adequateProgress;
							}

						return false;

					default:
						return false;
				}
			}

		private void ShowUnlockRequirements()
			{
			switch (floorIndex)
				{
					case 0:
						Debug.Log("💎 Requires: Prove your worth in the Crystal Caverns");
						break;
					case 1:
						Debug.Log("🔥 Requires: Flame essence from the Molten Depths");
						break;
					case 2:
						Debug.Log("🌌 Requires: Void energy from the dark realms");
						break;
				}
			}

		private void UnlockLock()
			{
			isUnlocked = true;

			// Visual feedback
			if (lockRenderer && unlockedMaterial)
				{
				lockRenderer.material = unlockedMaterial;
				}

			// Audio feedback
			if (unlockSound)
				{
				AudioSource.PlayClipAtPoint(unlockSound, transform.position);
				}

			// Particle effect
			if (unlockedEffect)
				{
				Instantiate(unlockedEffect, transform.position, transform.rotation);
				}

			// Notify dungeon mode
			if (dungeonMode != null) dungeonMode.OnProgressionLockUnlocked(floorIndex);

			Debug.Log($"🔓 {lockName} has been unlocked!");

			// Hide interaction prompt
			HideInteractionPrompt();
			}

		/// <summary>
		/// Force unlock this lock (for testing or special conditions)
		/// </summary>
		public void ForceUnlock()
			{
			UnlockLock();
			}
		}

	/// <summary>
	/// Dungeon secret component - hidden discoverable elements that provide rewards.
	/// Each secret is biome-themed and provides meaningful rewards for the current run.
	/// </summary>
	public class DungeonSecret : MonoBehaviour
		{
		[Header("Secret Configuration")] [SerializeField]
		private int floorIndex;

		[SerializeField] private int secretIndex;
		[SerializeField] private bool isDiscovered = false;
		[SerializeField] private float discoveryRange = 1.5f;

		[Header("Rewards")] [SerializeField] private int currencyReward = 50;

		[SerializeField] private bool grantsHealthBonus = true;
		[SerializeField] private bool grantsManaBonus = false;

		[Header("Visual Effects")] [SerializeField]
		private GameObject discoveryEffect;

		[SerializeField] private AudioClip discoverySound;
		private bool _playerResolved;
		private Material discoveredMaterial = null!;

		private DungeonDelveMode dungeonMode = null!;
		private Material hiddenMaterial = null!;
		private float originalAlpha;
		private Transform playerTransform = null!;
		private Renderer secretRenderer = null!;

		public bool IsDiscovered => isDiscovered;
		public int FloorIndex => floorIndex;
		public int SecretIndex => secretIndex;

		private void Awake()
			{
			// Placeholder player transform
			var placeholder = new GameObject("__Secret_PlaceholderPlayer__");
			placeholder.hideFlags = HideFlags.HideAndDontSave;
			playerTransform = placeholder.transform;
			}

		private void Update()
			{
			if (!_playerResolved) ResolvePlayer();
			if (isDiscovered || !_playerResolved) return;

			// Check if player is close enough to discover the secret
			float distance = Vector3.Distance(transform.position, playerTransform.position);

			if (distance <= discoveryRange)
				{
				DiscoverSecret();
				}
			}

		private void OnDrawGizmosSelected()
			{
			// Draw discovery range
			Gizmos.color = isDiscovered ? Color.yellow : new Color(1f, 1f, 0f, 0.5f);
			Gizmos.DrawWireSphere(transform.position, discoveryRange);
			}

		public void Initialize(int floor, int index, DungeonDelveMode mode)
			{
			floorIndex = floor;
			secretIndex = index;
			dungeonMode = mode;

			// Find player
			ResolvePlayer();

			// Setup materials for hidden state
			secretRenderer = GetComponent<Renderer>();
			if (secretRenderer)
				{
				hiddenMaterial = secretRenderer.material;
				originalAlpha = hiddenMaterial.color.a;

				// Make secrets semi-transparent initially
				Color hiddenColor = hiddenMaterial.color;
				hiddenColor.a = 0.3f;
				hiddenMaterial.color = hiddenColor;

				discoveredMaterial = new Material(hiddenMaterial);
				Color discoveredColor = discoveredMaterial.color;
				discoveredColor.a = 1f;
				discoveredColor = Color.yellow; // Bright color when discovered
				discoveredMaterial.color = discoveredColor;
				}

			Debug.Log($"🔍 Secret {index} initialized on floor {floor + 1}");
			}

		private void DiscoverSecret()
			{
			if (isDiscovered) return;

			isDiscovered = true;

			// Visual feedback
			if (secretRenderer && discoveredMaterial)
				{
				secretRenderer.material = discoveredMaterial;
				}

			// Audio feedback
			if (discoverySound)
				{
				AudioSource.PlayClipAtPoint(discoverySound, transform.position);
				}

			// Particle effect
			if (discoveryEffect)
				{
				Instantiate(discoveryEffect, transform.position, transform.rotation);
				}

			// Apply rewards
			ApplySecretRewards();

			// Notify dungeon mode
			if (dungeonMode != null) dungeonMode.OnSecretDiscovered(floorIndex, secretIndex);
			Debug.Log($"🌟 Secret discovered on floor {floorIndex + 1}! Rewards granted.");
			}

		private void ResolvePlayer()
			{
			if (_playerResolved) return;
			DemoPlayerMovement pm = FindFirstObjectByType<DemoPlayerMovement>();
			if (pm)
				{
				playerTransform = pm.transform;
				_playerResolved = true;
				}
			}

		private void ApplySecretRewards()
			{
			DemoPlayerInventory? playerInventory = FindFirstObjectByType<DemoPlayerInventory>();
			DemoPlayerCombat playerCombat = FindFirstObjectByType<DemoPlayerCombat>();

			// Currency reward - create currency item and add to inventory
			if (currencyReward > 0)
				{
				Debug.Log($"💰 Gained {currencyReward} currency from secret!");

				if (playerInventory != null)
					{
					var currencyItem = new DemoItem
						{
						id = "gold_coins",
						name = "Gold Coins",
						description = $"Valuable gold coins worth {currencyReward} value",
						type = ItemType.Material,
						rarity = ItemRarity.Common,
						stackSize = 99,
						currentStack = currencyReward,
						value = currencyReward
						};
					playerInventory.AddItem(currencyItem);
					}
				}

			// Health bonus - permanent max health increase
			if (grantsHealthBonus)
				{
				Debug.Log("❤️ Gained permanent health bonus from secret!");

				if (playerCombat != null)
					{
					// Increase max health permanently by 10
					int healthBonus = 10;
					playerCombat.maxHealth += healthBonus;
					playerCombat.Heal(healthBonus); // Also heal for the bonus amount
					Debug.Log($"❤️ Max health increased by {healthBonus}! New max: {playerCombat.maxHealth}");
					}
				}

			// Mana bonus - add mana restoration consumable (since there's no mana system, use energy potion)
			if (grantsManaBonus)
				{
				Debug.Log("💙 Gained permanent mana bonus from secret!");

				if (playerInventory != null)
					{
					var energyPotion = new DemoItem
						{
						id = "energy_potion",
						name = "Energy Potion",
						description = "Restores energy and provides a speed boost",
						type = ItemType.Consumable,
						rarity = ItemRarity.Uncommon,
						stackSize = 3,
						currentStack = 1,
						value = 75,
						consumableEffect = ConsumableEffect.Buff,
						effectValue = 30 // 30 second speed boost
						};
					playerInventory.AddItem(energyPotion, 2); // Give 2 energy potions
					Debug.Log($"💙 Added {energyPotion.name} x2 to inventory!");
					}
				}

			// Biome-specific rewards based on floor
			ApplyBiomeSpecificReward();
			}

		private void ApplyBiomeSpecificReward()
			{
			DemoPlayerInventory playerInventory = FindFirstObjectByType<DemoPlayerInventory>();
			DemoPlayerCombat playerCombat = FindFirstObjectByType<DemoPlayerCombat>();

			switch (floorIndex)
				{
					case 0: // Crystal Caverns
						Debug.Log("💎 Crystal power enhances your defenses!");
						if (playerCombat != null)
							{
							// Grant temporary defense bonus
							playerCombat.AddDefenseBonus(5f); // 5 points defense bonus
							Debug.Log("🛡️ Crystal defense bonus applied (+5 DEF)!");
							}

						// Also give a crystal shard as material
						if (playerInventory != null)
							{
							var crystalShard = new DemoItem
								{
								id = "crystal_shard",
								name = "Crystal Shard",
								description = "A sharp crystal fragment that gleams with inner light",
								type = ItemType.Material,
								rarity = ItemRarity.Rare,
								stackSize = 10,
								currentStack = 1,
								value = 100
								};
							playerInventory.AddItem(crystalShard);
							Debug.Log("💎 Added Crystal Shard to inventory!");
							}

						break;

					case 1: // Molten Depths
						Debug.Log("🔥 Flame essence increases your attack power!");
						if (playerCombat != null)
							{
							// Grant temporary damage bonus
							playerCombat.AddDamageBonus(10f); // 10 points damage bonus
							Debug.Log("⚔️ Flame damage bonus applied (+10 DMG)!");
							}

						// Also give molten essence as material
						if (playerInventory != null)
							{
							var moltenEssence = new DemoItem
								{
								id = "molten_essence",
								name = "Molten Essence",
								description = "Liquid fire contained in a crystalline vessel",
								type = ItemType.Material,
								rarity = ItemRarity.Epic,
								stackSize = 5,
								currentStack = 1,
								value = 200
								};
							playerInventory.AddItem(moltenEssence);
							Debug.Log("🔥 Added Molten Essence to inventory!");
							}

						break;

					case 2: // Void Sanctum
						Debug.Log("🌌 Void energy grants mystical abilities!");

						// Grant a powerful void trinket
						if (playerInventory != null)
							{
							var voidTrinket = new DemoItem
								{
								id = "void_trinket",
								name = "Void Heart",
								description = "A mysterious artifact that pulses with otherworldly energy",
								type = ItemType.Trinket,
								rarity = ItemRarity.Legendary,
								stackSize = 1,
								currentStack = 1,
								value = 500,
								trinketStats = new TrinketStats
									{
									healthBonus = (int)25f,
									damageBonus = (int)15f,
									defenseBonus = (int)10f,
									speedBonus = (int)1.2f
									}
								};
							playerInventory.AddItem(voidTrinket);

							// Auto-equip if no trinket equipped
							DemoItem? currentTrinket = playerInventory.GetEquippedItem(EquipmentSlot.Trinket);
							if (currentTrinket == null)
								{
								playerInventory.EquipItem(voidTrinket);
								Debug.Log("🌌 Void Heart equipped! All stats increased!");
								}
							else
								{
								Debug.Log("🌌 Added Void Heart to inventory!");
								}
							}

						break;
				}
			}
		}

	/// <summary>
	/// Dungeon pickup component - functional pickup items integrated with inventory systems.
	/// All pickups are biome-themed and provide meaningful benefits for the current run.
	/// </summary>
	public class DungeonPickup : MonoBehaviour
		{
		[Header("Pickup Configuration")] [SerializeField]
		private PickupType pickupType;

		[SerializeField] private int value = 1;
		[SerializeField] private bool isCollected = false;
		[SerializeField] private float collectionRange = 1f;

		[Header("Visual Effects")] [SerializeField]
		private GameObject collectionEffect;

		[SerializeField] private AudioClip collectionSound;
		[SerializeField] private float bobHeight = 0.5f;
		[SerializeField] private float bobSpeed = 2f;
		[SerializeField] private float rotationSpeed = 90f;
		private bool _playerResolved;
		private float bobOffset;

		private DungeonDelveMode dungeonMode = null!;
		private Vector3 originalPosition;
		private Transform playerTransform = null!;

		public PickupType Type => pickupType;
		public bool IsCollected => isCollected;
		public int Value => value;

		private void Awake()
			{
			var placeholder = new GameObject("__Pickup_PlaceholderPlayer__");
			placeholder.hideFlags = HideFlags.HideAndDontSave;
			playerTransform = placeholder.transform;
			}

		private void Update()
			{
			if (isCollected) return;

			// Animate the pickup (bobbing and rotation)
			AnimatePickup();

			// Check for collection
			if (_playerResolved)
				{
				float distance = Vector3.Distance(transform.position, playerTransform.position);
				if (distance <= collectionRange) CollectPickup();
				}
			else
				{
				ResolvePlayer();
				}
			}

		private void OnDrawGizmosSelected()
			{
			// Draw collection range
			Gizmos.color = isCollected ? Color.gray : Color.green;
			Gizmos.DrawWireSphere(transform.position, collectionRange);
			}

		public void Initialize(PickupType type, DungeonDelveMode mode)
			{
			pickupType = type;
			dungeonMode = mode;

			// Find player
			ResolvePlayer();

			// Setup animation
			originalPosition = transform.position;
			bobOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

			// Set value based on type
			SetPickupValue();

			Debug.Log($"💎 {pickupType} pickup initialized with value {value}");
			}

		private void SetPickupValue()
			{
			switch (pickupType)
				{
					case PickupType.Health:
						value = 25; // Health points to restore
						break;
					case PickupType.Mana:
						value = 20; // Mana points to restore
						break;
					case PickupType.Currency:
						value = UnityEngine.Random.Range(10, 30); // Random currency amount
						break;
					case PickupType.Equipment:
						value = 1; // Equipment piece
						break;
					case PickupType.Consumable:
						value = 1; // Consumable item
						break;
				}
			}

		private void AnimatePickup()
			{
			// Bobbing motion
			float bobY = Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobHeight;
			transform.position = originalPosition + Vector3.up * bobY;

			// Rotation
			transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
			}

		private void CollectPickup()
			{
			if (isCollected) return;

			isCollected = true;

			// Visual feedback
			if (collectionEffect)
				{
				Instantiate(collectionEffect, transform.position, transform.rotation);
				}

			// Audio feedback
			if (collectionSound)
				{
				AudioSource.PlayClipAtPoint(collectionSound, transform.position);
				}

			// Apply pickup effects
			ApplyPickupEffect();

			// Notify dungeon mode
			if (dungeonMode != null) dungeonMode.OnPickupCollected(pickupType);
			Debug.Log($"✨ Collected {pickupType} pickup (value: {value})");
			Destroy(gameObject);
			}

		private void ResolvePlayer()
			{
			if (_playerResolved) return;
			DemoPlayerMovement pm = FindFirstObjectByType<DemoPlayerMovement>();
			if (pm)
				{
				playerTransform = pm.transform;
				_playerResolved = true;
				}
			}

		private void ApplyPickupEffect()
			{
			DemoPlayerInventory playerInventory = FindFirstObjectByType<DemoPlayerInventory>();
			DemoPlayerCombat playerCombat = FindFirstObjectByType<DemoPlayerCombat>();

			switch (pickupType)
				{
					case PickupType.Health:
						Debug.Log($"❤️ Restored {value} health points!");
						if (playerCombat != null)
							{
							playerCombat.Heal(value);
							}

						break;

					case PickupType.Mana:
						Debug.Log($"💙 Restored {value} mana points!");
						// Since there's no mana system, give speed boost instead as "energy"
						if (playerCombat != null)
							{
							playerCombat.AddDamageBonus(value * 0.5f); // Convert mana to temporary damage bonus
							Debug.Log($"💙 Energy boost applied! +{value * 0.5f} damage for 30 seconds");
							}

						break;

					case PickupType.Currency:
						Debug.Log($"💰 Gained {value} currency!");
						if (playerInventory != null)
							{
							var goldCoins = new DemoItem
								{
								id = "gold_coins",
								name = "Gold Coins",
								description = $"Shiny gold coins worth {value} value",
								type = ItemType.Material,
								rarity = ItemRarity.Common,
								stackSize = 99,
								currentStack = value,
								value = value
								};
							playerInventory.AddItem(goldCoins);
							}

						break;

					case PickupType.Equipment:
						Debug.Log("⚔️ Found new equipment!");
						if (playerInventory != null)
							{
							DemoItem randomEquipment = GenerateRandomEquipment();
							playerInventory.AddItem(randomEquipment);
							Debug.Log($"⚔️ Added {randomEquipment.name} to inventory!");
							}

						break;

					case PickupType.Consumable:
						Debug.Log("🧪 Found consumable item!");
						if (playerInventory != null)
							{
							DemoItem randomConsumable = GenerateRandomConsumable();
							playerInventory.AddItem(randomConsumable);
							Debug.Log($"🧪 Added {randomConsumable.name} to inventory!");
							}

						break;
				}
			}

		private DemoItem GenerateRandomEquipment()
			{
			// Generate random equipment based on dungeon floor and pickup value
			ItemType[] equipmentTypes = new[] { ItemType.Weapon, ItemType.Armor, ItemType.Trinket };
			ItemType selectedType = equipmentTypes[Random.Range(0, equipmentTypes.Length)];

			ItemRarity[] rarities = new[] { ItemRarity.Common, ItemRarity.Uncommon, ItemRarity.Rare };
			ItemRarity selectedRarity = rarities[Random.Range(0, rarities.Length)];

			switch (selectedType)
				{
					case ItemType.Weapon:
						return new DemoItem
							{
							id = $"random_weapon_{Random.Range(1000, 9999)}",
							name = GetRandomWeaponName(selectedRarity),
							description = "A weapon found in the dungeon depths",
							type = ItemType.Weapon,
							rarity = selectedRarity,
							stackSize = 1,
							value = value * GetRarityMultiplier(selectedRarity),
							weaponStats = new WeaponStats
								{
								damage = value + Random.Range(5, 15) * GetRarityMultiplier(selectedRarity),
								range = Random.Range(1.5f, 4f),
								attackSpeed = Random.Range(0.8f, 1.4f)
								}
							};

					case ItemType.Armor:
						return new DemoItem
							{
							id = $"random_armor_{Random.Range(1000, 9999)}",
							name = GetRandomArmorName(selectedRarity),
							description = "Protective gear found in the dungeon",
							type = ItemType.Armor,
							rarity = selectedRarity,
							stackSize = 1,
							value = value * GetRarityMultiplier(selectedRarity),
							armorStats = new ArmorStats
								{
								defense = Random.Range(3, 8) * GetRarityMultiplier(selectedRarity),
								healthBonus = Random.Range(10, 25) * GetRarityMultiplier(selectedRarity)
								}
							};

					case ItemType.Trinket:
						return new DemoItem
							{
							id = $"random_trinket_{Random.Range(1000, 9999)}",
							name = GetRandomTrinketName(selectedRarity),
							description = "A mystical trinket with magical properties",
							type = ItemType.Trinket,
							rarity = selectedRarity,
							stackSize = 1,
							value = value * GetRarityMultiplier(selectedRarity),
							trinketStats = new TrinketStats
								{
								healthBonus = Random.Range(5, 15),
								damageBonus = Random.Range(2, 8),
								defenseBonus = Random.Range(1, 5),
								speedBonus = Random.Range(1.05f, 1.15f)
								}
							};

					default:
						return GenerateRandomEquipment(); // Fallback recursion
				}
			}

		private DemoItem GenerateRandomConsumable()
			{
			ConsumableEffect[] consumableTypes = new[]
				{
				ConsumableEffect.Heal,
				ConsumableEffect.DamageBuff,
				ConsumableEffect.DefenseBuff,
				ConsumableEffect.SpeedBuff
				};
			ConsumableEffect selectedEffect = consumableTypes[Random.Range(0, consumableTypes.Length)];

			return new DemoItem
				{
				id = $"random_consumable_{Random.Range(1000, 9999)}",
				name = GetConsumableName(selectedEffect),
				description = GetConsumableDescription(selectedEffect),
				type = ItemType.Consumable,
				rarity = ItemRarity.Common,
				stackSize = 5,
				value = Random.Range(20, 60),
				consumableEffect = selectedEffect,
				effectValue = GetConsumableEffectValue(selectedEffect)
				};
			}

		private string GetRandomWeaponName(ItemRarity rarity)
			{
			string[] commonNames = new[] { "Iron Sword", "Bronze Axe", "Wooden Staff", "Stone Mace" };
			string[] uncommonNames = new[] { "Steel Blade", "Silver Axe", "Mystical Staff", "Iron Hammer" };
			string[] rareNames = new[] { "Enchanted Sword", "Dwarven Axe", "Arcane Staff", "Blessed Mace" };

			return rarity switch
				{
				ItemRarity.Common => commonNames[Random.Range(0, commonNames.Length)],
				ItemRarity.Uncommon => uncommonNames[Random.Range(0, uncommonNames.Length)],
				ItemRarity.Rare => rareNames[Random.Range(0, rareNames.Length)],
				_ => "Unknown Weapon"
				};
			}

		private string GetRandomArmorName(ItemRarity rarity)
			{
			string[] commonNames = new[] { "Leather Vest", "Cloth Robes", "Chain Mail", "Hide Armor" };
			string[] uncommonNames = new[] { "Steel Plate", "Reinforced Leather", "Mage Robes", "Scaled Armor" };
			string[] rareNames = new[] { "Enchanted Plate", "Dragon Scale", "Archmage Robes", "Blessed Mail" };

			return rarity switch
				{
				ItemRarity.Common => commonNames[Random.Range(0, commonNames.Length)],
				ItemRarity.Uncommon => uncommonNames[Random.Range(0, uncommonNames.Length)],
				ItemRarity.Rare => rareNames[Random.Range(0, rareNames.Length)],
				_ => "Unknown Armor"
				};
			}

		private string GetRandomTrinketName(ItemRarity rarity)
			{
			string[] commonNames = new[] { "Simple Ring", "Leather Pouch", "Bone Charm", "Iron Pendant" };
			string[] uncommonNames = new[] { "Silver Ring", "Mystic Amulet", "Carved Idol", "Crystal Pendant" };
			string[] rareNames = new[] { "Ring of Power", "Ancient Amulet", "Sacred Idol", "Void Crystal" };

			return rarity switch
				{
				ItemRarity.Common => commonNames[Random.Range(0, commonNames.Length)],
				ItemRarity.Uncommon => uncommonNames[Random.Range(0, uncommonNames.Length)],
				ItemRarity.Rare => rareNames[Random.Range(0, rareNames.Length)],
				_ => "Unknown Trinket"
				};
			}

		private string GetConsumableName(ConsumableEffect effect)
			{
			return effect switch
				{
				ConsumableEffect.Heal => "Health Potion",
				ConsumableEffect.DamageBuff => "Strength Potion",
				ConsumableEffect.DefenseBuff => "Defense Potion",
				ConsumableEffect.SpeedBuff => "Speed Potion",
				_ => "Unknown Potion"
				};
			}

		private string GetConsumableDescription(ConsumableEffect effect)
			{
			return effect switch
				{
				ConsumableEffect.Heal => "Restores health when consumed",
				ConsumableEffect.DamageBuff => "Temporarily increases damage output",
				ConsumableEffect.DefenseBuff => "Temporarily increases defense",
				ConsumableEffect.SpeedBuff => "Temporarily increases movement speed",
				_ => "A mysterious potion with unknown effects"
				};
			}

		private int GetConsumableEffectValue(ConsumableEffect effect)
			{
			return effect switch
				{
				ConsumableEffect.Heal => Random.Range(30, 80),
				ConsumableEffect.DamageBuff => Random.Range(15, 30),
				ConsumableEffect.DefenseBuff => Random.Range(10, 20),
				ConsumableEffect.SpeedBuff => Random.Range(20, 40), // Duration in seconds
				_ => 25
				};
			}

		private int GetRarityMultiplier(ItemRarity rarity)
			{
			return rarity switch
				{
				ItemRarity.Common => 1,
				ItemRarity.Uncommon => 2,
				ItemRarity.Rare => 3,
				ItemRarity.Epic => 4,
				ItemRarity.Legendary => 5,
				_ => 1
				};
			}
		}
	}
