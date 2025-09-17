using UnityEngine;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVanDAMN.Authoring
{
    /// <summary>
    /// Complete player setup component that integrates all upgrade systems.
    /// Automatically configures the player with progression, combat, movement, and UI systems.
    /// </summary>
    public class CompletePlayerSetup : MonoBehaviour
    {
        [Header("Player Configuration")]
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private bool enableLevelUpUI = true;
        [SerializeField] private bool enableDebugControls = true;

        [Header("Starting Configuration")]
        [SerializeField] private int startingLevel = 1;
        [SerializeField] private int startingXP = 0;
        [SerializeField] private Ability startingAbilities = Ability.Jump;

        [Header("Component References")]
        [SerializeField] private PlayerLevelProgression levelProgression;
        [SerializeField] private LevelUpChoiceSystem choiceSystem;
        [SerializeField] private UpgradeEffectApplicator effectApplicator;
        [SerializeField] private LevelUpChoiceUI choiceUI;
        [SerializeField] private DemoPlayerMovement playerMovement;
        [SerializeField] private DemoPlayerCombat playerCombat;
        [SerializeField] private DemoPlayerInventory playerInventory;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging = true;

        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupPlayer();
            }
        }

        /// <summary>
        /// Setup the complete player with all upgrade systems
        /// </summary>
        [ContextMenu("Setup Player")]
        public void SetupPlayer()
        {
            if (enableDebugLogging)
            {
                Debug.Log("🎮 Setting up complete player with upgrade systems...");
            }

            // Get or create core components
            EnsureCoreComponents();

            // Setup upgrade database
            EnsureUpgradeDatabase();

            // Configure starting state
            ConfigureStartingState();

            // Setup UI
            if (enableLevelUpUI)
            {
                SetupUI();
            }

            // Connect events
            ConnectEvents();

            if (enableDebugLogging)
            {
                Debug.Log("✅ Complete player setup finished!");
                LogPlayerStats();
            }
        }

        private void EnsureCoreComponents()
        {
            // PlayerLevelProgression
            if (levelProgression == null)
            {
                levelProgression = GetComponent<PlayerLevelProgression>();
                if (levelProgression == null)
                {
                    levelProgression = gameObject.AddComponent<PlayerLevelProgression>();
                }
            }

            // LevelUpChoiceSystem
            if (choiceSystem == null)
            {
                choiceSystem = GetComponent<LevelUpChoiceSystem>();
                if (choiceSystem == null)
                {
                    choiceSystem = gameObject.AddComponent<LevelUpChoiceSystem>();
                }
            }

            // UpgradeEffectApplicator
            if (effectApplicator == null)
            {
                effectApplicator = GetComponent<UpgradeEffectApplicator>();
                if (effectApplicator == null)
                {
                    effectApplicator = gameObject.AddComponent<UpgradeEffectApplicator>();
                }
            }

            // Player components
            if (playerMovement == null)
            {
                playerMovement = GetComponent<DemoPlayerMovement>();
                if (playerMovement == null)
                {
                    playerMovement = gameObject.AddComponent<DemoPlayerMovement>();
                }
            }

            if (playerCombat == null)
            {
                playerCombat = GetComponent<DemoPlayerCombat>();
                if (playerCombat == null)
                {
                    playerCombat = gameObject.AddComponent<DemoPlayerCombat>();
                }
            }

            if (playerInventory == null)
            {
                playerInventory = GetComponent<DemoPlayerInventory>();
                if (playerInventory == null)
                {
                    playerInventory = gameObject.AddComponent<DemoPlayerInventory>();
                }
            }

            // Ensure Rigidbody
            if (GetComponent<Rigidbody2D>() == null && GetComponent<Rigidbody>() == null)
            {
                gameObject.AddComponent<Rigidbody2D>();
            }

            // Ensure Collider
            if (GetComponent<Collider2D>() == null && GetComponent<Collider>() == null)
            {
                var collider = gameObject.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(0.8f, 1.8f);
            }
        }

        private void EnsureUpgradeDatabase()
        {
            // Find or create upgrade database manager
            var databaseManager = FindObjectOfType<UpgradeDatabaseManager>();
            if (databaseManager == null)
            {
                var dbObj = new GameObject("UpgradeDatabaseManager");
                databaseManager = dbObj.AddComponent<UpgradeDatabaseManager>();
                
                if (enableDebugLogging)
                {
                    Debug.Log("📚 Created UpgradeDatabaseManager");
                }
            }
        }

        private void ConfigureStartingState()
        {
            if (levelProgression != null)
            {
                // Note: This would need to be configured via inspector or through public properties
                // We can't modify serialized fields at runtime like this
                if (enableDebugLogging)
                {
                    Debug.Log($"🎯 Configure starting state manually: Level {startingLevel}, XP {startingXP}, Abilities {startingAbilities}");
                }
            }
        }

        private void SetupUI()
        {
            // Find or create choice UI
            if (choiceUI == null)
            {
                choiceUI = FindObjectOfType<LevelUpChoiceUI>();
                if (choiceUI == null)
                {
                    var uiObj = new GameObject("LevelUpChoiceUI");
                    choiceUI = uiObj.AddComponent<LevelUpChoiceUI>();
                    
                    if (enableDebugLogging)
                    {
                        Debug.Log("🎨 Created LevelUpChoiceUI");
                    }
                }
            }
        }

        private void ConnectEvents()
        {
            // Connect level progression to choice system
            if (levelProgression != null && choiceSystem != null)
            {
                levelProgression.OnLevelUp += (level) => choiceSystem.GenerateUpgradeChoices();
            }

            // Connect choice system to effect application
            if (choiceSystem != null && levelProgression != null)
            {
                choiceSystem.OnUpgradeChosen += (upgrade) => levelProgression.ApplyUpgrade(upgrade);
            }

            // Connect UI events
            if (choiceUI != null && choiceSystem != null)
            {
                choiceUI.OnUpgradeSelected += (upgrade) => choiceSystem.ChooseUpgrade(upgrade);
            }
        }

        private void LogPlayerStats()
        {
            if (levelProgression == null) return;

            Debug.Log("=== PLAYER SETUP COMPLETE ===");
            Debug.Log($"Level: {levelProgression.CurrentLevel}");
            Debug.Log($"XP: {levelProgression.CurrentXP}/{levelProgression.XPRequiredForNextLevel}");
            Debug.Log($"Abilities: {levelProgression.CurrentAbilities}");
            Debug.Log($"Upgrades Applied: {levelProgression.AppliedUpgrades.Length}");
        }

        #region Debug Controls

        private void Update()
        {
            if (!enableDebugControls) return;

            // Debug controls for testing
            if (Input.GetKeyDown(KeyCode.F1))
            {
                GainXP(50);
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                ForceLevelUp();
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                ForceShowChoices();
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                ResetProgression();
            }
        }

        [ContextMenu("Gain 50 XP")]
        private void GainXP(int amount = 50)
        {
            if (levelProgression != null)
            {
                levelProgression.GainXP(amount);
                Debug.Log($"🌟 Gained {amount} XP");
            }
        }

        [ContextMenu("Force Level Up")]
        private void ForceLevelUp()
        {
            if (levelProgression != null)
            {
                int xpNeeded = levelProgression.XPRequiredForNextLevel - levelProgression.CurrentXP;
                levelProgression.GainXP(xpNeeded);
                Debug.Log("⬆️ Forced level up");
            }
        }

        [ContextMenu("Force Show Choices")]
        private void ForceShowChoices()
        {
            if (choiceSystem != null)
            {
                choiceSystem.GenerateUpgradeChoices();
                Debug.Log("🎯 Forced upgrade choice generation");
            }
        }

        [ContextMenu("Reset Progression")]
        private void ResetProgression()
        {
            if (levelProgression != null)
            {
                // Reset to starting state
                var method = levelProgression.GetType().GetMethod("ResetProgression", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(levelProgression, null);
                
                Debug.Log("🔄 Reset player progression");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get the player's current progression data
        /// </summary>
        public (int level, int xp, Ability abilities, int upgradeCount) GetProgressionData()
        {
            if (levelProgression == null)
                return (1, 0, Ability.Jump, 0);

            return (
                levelProgression.CurrentLevel,
                levelProgression.CurrentXP,
                levelProgression.CurrentAbilities,
                levelProgression.AppliedUpgrades.Length
            );
        }

        /// <summary>
        /// Check if the player can level up
        /// </summary>
        public bool CanLevelUp()
        {
            if (levelProgression == null) return false;
            return levelProgression.CurrentXP >= levelProgression.XPRequiredForNextLevel;
        }

        /// <summary>
        /// Get upgrade choices without leveling up (for preview)
        /// </summary>
        public void PreviewUpgradeChoices()
        {
            if (choiceSystem != null)
            {
                choiceSystem.GenerateUpgradeChoices();
            }
        }

        /// <summary>
        /// Apply an upgrade directly (for testing)
        /// </summary>
        public void ApplyUpgradeDirectly(UpgradeDefinition upgrade)
        {
            if (levelProgression != null && upgrade != null)
            {
                levelProgression.ApplyUpgrade(upgrade);
            }
        }

        #endregion

        private void OnValidate()
        {
            // Ensure starting level is at least 1
            if (startingLevel < 1) startingLevel = 1;
            
            // Ensure starting XP is not negative
            if (startingXP < 0) startingXP = 0;
        }
    }
}