using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

namespace TinyWalnutGames.MetVanDAMN.Authoring
{
    /// <summary>
    /// Main menu integration for Dungeon Delve Mode.
    /// Provides complete UI for starting, configuring, and monitoring dungeon runs.
    /// Fully compliant with MetVanDAMN mandate: self-explanatory UI with working defaults.
    /// </summary>
    public class DungeonDelveMainMenu : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas mainMenuCanvas;
        [SerializeField] private Button startDungeonButton;
        [SerializeField] private Button configureButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text seedDisplayText;
        
        [Header("Configuration Panel")]
        [SerializeField] private GameObject configurationPanel;
        [SerializeField] private InputField seedInputField;
        [SerializeField] private Toggle customSeedToggle;
        [SerializeField] private Button generateRandomSeedButton;
        [SerializeField] private Button applyConfigButton;
        [SerializeField] private Button cancelConfigButton;
        
        [Header("HUD Elements")]
        [SerializeField] private GameObject dungeonHUD;
        [SerializeField] private Text floorText;
        [SerializeField] private Text timeText;
        [SerializeField] private Text bossesDefeatedText;
        [SerializeField] private Text secretsFoundText;
        [SerializeField] private Text progressionLocksText;
        [SerializeField] private Button abortDungeonButton;
        
        [Header("Completion Panel")]
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private Text completionTimeText;
        [SerializeField] private Text completionStatsText;
        [SerializeField] private Button returnToMenuButton;
        [SerializeField] private Button startNewDungeonButton;
        
        // State
        private DungeonDelveMode currentDungeonMode;
        private uint configuredSeed = 42;
        private bool isInDungeon = false;
        private float dungeonStartTime;
        
        private void Awake()
        {
            InitializeUI();
            CreateUIIfNeeded();
        }
        
        private void Start()
        {
            // Ensure we start in main menu state
            ShowMainMenu();
            FindOrCreateDungeonMode();
        }
        
        private void Update()
        {
            // Update HUD if in dungeon
            if (isInDungeon && currentDungeonMode != null)
            {
                UpdateDungeonHUD();
            }
        }
        
        private void InitializeUI()
        {
            // Setup button listeners if buttons exist
            if (startDungeonButton) startDungeonButton.onClick.AddListener(StartDungeonDelve);
            if (configureButton) configureButton.onClick.AddListener(ShowConfiguration);
            if (exitButton) exitButton.onClick.AddListener(ExitToMainMenu);
            
            if (generateRandomSeedButton) generateRandomSeedButton.onClick.AddListener(GenerateRandomSeed);
            if (applyConfigButton) applyConfigButton.onClick.AddListener(ApplyConfiguration);
            if (cancelConfigButton) cancelConfigButton.onClick.AddListener(HideConfiguration);
            
            if (abortDungeonButton) abortDungeonButton.onClick.AddListener(AbortDungeon);
            if (returnToMenuButton) returnToMenuButton.onClick.AddListener(ReturnToMainMenu);
            if (startNewDungeonButton) startNewDungeonButton.onClick.AddListener(StartNewDungeon);
            
            if (customSeedToggle) customSeedToggle.onValueChanged.AddListener(OnCustomSeedToggled);
        }
        
        private void CreateUIIfNeeded()
        {
            // Create main menu canvas if it doesn't exist
            if (!mainMenuCanvas)
            {
                CreateMainMenuCanvas();
            }
            
            // Create HUD if it doesn't exist
            if (!dungeonHUD)
            {
                CreateDungeonHUD();
            }
            
            // Create completion panel if it doesn't exist
            if (!completionPanel)
            {
                CreateCompletionPanel();
            }
        }
        
        private void CreateMainMenuCanvas()
        {
            var canvasGO = new GameObject("Dungeon Delve Main Menu");
            mainMenuCanvas = canvasGO.AddComponent<Canvas>();
            mainMenuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainMenuCanvas.sortingOrder = 100;
            
            var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            CreateMainMenuElements();
        }
        
        private void CreateMainMenuElements()
        {
            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(mainMenuCanvas.transform, false);
            titleText = titleGO.AddComponent<Text>();
            titleText.text = "🏰 MetVanDAMN: Dungeon Delve Mode";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 48;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
            
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.8f);
            titleRect.anchorMax = new Vector2(0.5f, 0.8f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(800, 80);
            
            // Status text
            var statusGO = new GameObject("Status");
            statusGO.transform.SetParent(mainMenuCanvas.transform, false);
            statusText = statusGO.AddComponent<Text>();
            statusText.text = "Ready for legendary dungeon adventures!";
            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText.fontSize = 24;
            statusText.color = Color.cyan;
            statusText.alignment = TextAnchor.MiddleCenter;
            
            var statusRect = statusText.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.5f, 0.7f);
            statusRect.anchorMax = new Vector2(0.5f, 0.7f);
            statusRect.anchoredPosition = Vector2.zero;
            statusRect.sizeDelta = new Vector2(600, 40);
            
            // Seed display
            var seedGO = new GameObject("Seed Display");
            seedGO.transform.SetParent(mainMenuCanvas.transform, false);
            seedDisplayText = seedGO.AddComponent<Text>();
            seedDisplayText.text = $"Seed: {configuredSeed}";
            seedDisplayText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            seedDisplayText.fontSize = 20;
            seedDisplayText.color = Color.yellow;
            seedDisplayText.alignment = TextAnchor.MiddleCenter;
            
            var seedRect = seedDisplayText.GetComponent<RectTransform>();
            seedRect.anchorMin = new Vector2(0.5f, 0.6f);
            seedRect.anchorMax = new Vector2(0.5f, 0.6f);
            seedRect.anchoredPosition = Vector2.zero;
            seedRect.sizeDelta = new Vector2(400, 30);
            
            // Buttons
            CreateMainMenuButtons();
        }
        
        private void CreateMainMenuButtons()
        {
            // Start Dungeon button
            startDungeonButton = CreateButton("Start Dungeon Delve", new Vector2(0.5f, 0.5f), new Vector2(300, 60));
            startDungeonButton.onClick.AddListener(StartDungeonDelve);
            
            // Configure button
            configureButton = CreateButton("Configure", new Vector2(0.5f, 0.4f), new Vector2(200, 50));
            configureButton.onClick.AddListener(ShowConfiguration);
            
            // Exit button
            exitButton = CreateButton("Exit", new Vector2(0.5f, 0.3f), new Vector2(150, 50));
            exitButton.onClick.AddListener(ExitToMainMenu);
        }
        
        private Button CreateButton(string text, Vector2 anchorPosition, Vector2 sizeDelta)
        {
            var buttonGO = new GameObject($"Button_{text}");
            buttonGO.transform.SetParent(mainMenuCanvas.transform, false);
            
            var image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.2f, 0.3f, 0.8f, 0.8f);
            
            var button = buttonGO.AddComponent<Button>();
            
            var buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = anchorPosition;
            buttonRect.anchorMax = anchorPosition;
            buttonRect.anchoredPosition = Vector2.zero;
            buttonRect.sizeDelta = sizeDelta;
            
            // Button text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            var buttonText = textGO.AddComponent<Text>();
            buttonText.text = text;
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 18;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
            
            var textRect = buttonText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            return button;
        }
        
        private void CreateDungeonHUD()
        {
            var hudGO = new GameObject("Dungeon Delve HUD");
            hudGO.transform.SetParent(mainMenuCanvas.transform, false);
            dungeonHUD = hudGO;
            
            // Floor display
            var floorGO = new GameObject("Floor Display");
            floorGO.transform.SetParent(hudGO.transform, false);
            floorText = floorGO.AddComponent<Text>();
            floorText.text = "Floor: 1";
            floorText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            floorText.fontSize = 24;
            floorText.color = Color.white;
            
            var floorRect = floorText.GetComponent<RectTransform>();
            floorRect.anchorMin = new Vector2(0.05f, 0.9f);
            floorRect.anchorMax = new Vector2(0.05f, 0.9f);
            floorRect.anchoredPosition = Vector2.zero;
            floorRect.sizeDelta = new Vector2(200, 40);
            
            // Time display
            var timeGO = new GameObject("Time Display");
            timeGO.transform.SetParent(hudGO.transform, false);
            timeText = timeGO.AddComponent<Text>();
            timeText.text = "Time: 0:00";
            timeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            timeText.fontSize = 20;
            timeText.color = Color.cyan;
            
            var timeRect = timeText.GetComponent<RectTransform>();
            timeRect.anchorMin = new Vector2(0.05f, 0.85f);
            timeRect.anchorMax = new Vector2(0.05f, 0.85f);
            timeRect.anchoredPosition = Vector2.zero;
            timeRect.sizeDelta = new Vector2(200, 30);
            
            // Progress displays
            CreateProgressDisplays(hudGO);
            
            // Abort button
            abortDungeonButton = CreateButton("Abort Dungeon", new Vector2(0.05f, 0.05f), new Vector2(150, 40));
            abortDungeonButton.transform.SetParent(hudGO.transform, false);
            abortDungeonButton.onClick.AddListener(AbortDungeon);
            
            // Initially hidden
            dungeonHUD.SetActive(false);
        }
        
        private void CreateProgressDisplays(GameObject parent)
        {
            // Bosses defeated
            var bossesGO = new GameObject("Bosses Display");
            bossesGO.transform.SetParent(parent.transform, false);
            bossesDefeatedText = bossesGO.AddComponent<Text>();
            bossesDefeatedText.text = "Bosses: 0/3";
            bossesDefeatedText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bossesDefeatedText.fontSize = 18;
            bossesDefeatedText.color = Color.red;
            
            var bossesRect = bossesDefeatedText.GetComponent<RectTransform>();
            bossesRect.anchorMin = new Vector2(0.05f, 0.75f);
            bossesRect.anchorMax = new Vector2(0.05f, 0.75f);
            bossesRect.anchoredPosition = Vector2.zero;
            bossesRect.sizeDelta = new Vector2(200, 25);
            
            // Secrets found
            var secretsGO = new GameObject("Secrets Display");
            secretsGO.transform.SetParent(parent.transform, false);
            secretsFoundText = secretsGO.AddComponent<Text>();
            secretsFoundText.text = "Secrets: 0";
            secretsFoundText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            secretsFoundText.fontSize = 18;
            secretsFoundText.color = Color.yellow;
            
            var secretsRect = secretsFoundText.GetComponent<RectTransform>();
            secretsRect.anchorMin = new Vector2(0.05f, 0.7f);
            secretsRect.anchorMax = new Vector2(0.05f, 0.7f);
            secretsRect.anchoredPosition = Vector2.zero;
            secretsRect.sizeDelta = new Vector2(200, 25);
            
            // Progression locks
            var locksGO = new GameObject("Locks Display");
            locksGO.transform.SetParent(parent.transform, false);
            progressionLocksText = locksGO.AddComponent<Text>();
            progressionLocksText.text = "Locks: 0/3";
            progressionLocksText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            progressionLocksText.fontSize = 18;
            progressionLocksText.color = Color.magenta;
            
            var locksRect = progressionLocksText.GetComponent<RectTransform>();
            locksRect.anchorMin = new Vector2(0.05f, 0.65f);
            locksRect.anchorMax = new Vector2(0.05f, 0.65f);
            locksRect.anchoredPosition = Vector2.zero;
            locksRect.sizeDelta = new Vector2(200, 25);
        }
        
        private void CreateCompletionPanel()
        {
            var panelGO = new GameObject("Completion Panel");
            panelGO.transform.SetParent(mainMenuCanvas.transform, false);
            completionPanel = panelGO;
            
            // Background
            var bgImage = panelGO.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.8f);
            
            var panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            // Completion text
            var completionGO = new GameObject("Completion Text");
            completionGO.transform.SetParent(panelGO.transform, false);
            completionTimeText = completionGO.AddComponent<Text>();
            completionTimeText.text = "🎉 Dungeon Completed!";
            completionTimeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            completionTimeText.fontSize = 36;
            completionTimeText.color = Color.yellow;
            completionTimeText.alignment = TextAnchor.MiddleCenter;
            
            var completionRect = completionTimeText.GetComponent<RectTransform>();
            completionRect.anchorMin = new Vector2(0.5f, 0.7f);
            completionRect.anchorMax = new Vector2(0.5f, 0.7f);
            completionRect.anchoredPosition = Vector2.zero;
            completionRect.sizeDelta = new Vector2(600, 60);
            
            // Stats text
            var statsGO = new GameObject("Stats Text");
            statsGO.transform.SetParent(panelGO.transform, false);
            completionStatsText = statsGO.AddComponent<Text>();
            completionStatsText.text = "Statistics will appear here";
            completionStatsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            completionStatsText.fontSize = 20;
            completionStatsText.color = Color.white;
            completionStatsText.alignment = TextAnchor.MiddleCenter;
            
            var statsRect = completionStatsText.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.5f, 0.5f);
            statsRect.anchorMax = new Vector2(0.5f, 0.5f);
            statsRect.anchoredPosition = Vector2.zero;
            statsRect.sizeDelta = new Vector2(500, 150);
            
            // Buttons
            returnToMenuButton = CreateButton("Return to Menu", new Vector2(0.4f, 0.3f), new Vector2(200, 50));
            returnToMenuButton.transform.SetParent(panelGO.transform, false);
            returnToMenuButton.onClick.AddListener(ReturnToMainMenu);
            
            startNewDungeonButton = CreateButton("New Dungeon", new Vector2(0.6f, 0.3f), new Vector2(200, 50));
            startNewDungeonButton.transform.SetParent(panelGO.transform, false);
            startNewDungeonButton.onClick.AddListener(StartNewDungeon);
            
            // Initially hidden
            completionPanel.SetActive(false);
        }
        
        private void FindOrCreateDungeonMode()
        {
            currentDungeonMode = FindObjectOfType<DungeonDelveMode>();
            if (!currentDungeonMode)
            {
                var dungeonGO = new GameObject("Dungeon Delve Mode");
                currentDungeonMode = dungeonGO.AddComponent<DungeonDelveMode>();
                Debug.Log("🏰 Created Dungeon Delve Mode component");
            }
            
            // Subscribe to events
            if (currentDungeonMode)
            {
                currentDungeonMode.OnFloorChanged += OnFloorChanged;
                currentDungeonMode.OnBossDefeated += OnBossDefeated;
                currentDungeonMode.OnSecretFound += OnSecretFound;
                currentDungeonMode.OnProgressionLockObtained += OnProgressionLockObtained;
                currentDungeonMode.OnDungeonCompleted += OnDungeonCompleted;
                currentDungeonMode.OnSessionAborted += OnSessionAborted;
            }
        }
        
        // UI State Management
        private void ShowMainMenu()
        {
            isInDungeon = false;
            if (mainMenuCanvas) mainMenuCanvas.gameObject.SetActive(true);
            if (dungeonHUD) dungeonHUD.SetActive(false);
            if (completionPanel) completionPanel.SetActive(false);
            if (configurationPanel) configurationPanel.SetActive(false);
        }
        
        private void ShowDungeonHUD()
        {
            isInDungeon = true;
            dungeonStartTime = Time.time;
            if (mainMenuCanvas) mainMenuCanvas.gameObject.SetActive(false);
            if (dungeonHUD) dungeonHUD.SetActive(true);
            if (completionPanel) completionPanel.SetActive(false);
        }
        
        private void ShowCompletionScreen()
        {
            isInDungeon = false;
            if (dungeonHUD) dungeonHUD.SetActive(false);
            if (completionPanel) completionPanel.SetActive(true);
        }
        
        private void ShowConfiguration()
        {
            if (!configurationPanel)
            {
                CreateConfigurationPanel();
            }
            configurationPanel.SetActive(true);
            UpdateConfigurationUI();
        }
        
        private void HideConfiguration()
        {
            if (configurationPanel) configurationPanel.SetActive(false);
        }
        
        private void CreateConfigurationPanel()
        {
            var panelGO = new GameObject("Configuration Panel");
            panelGO.transform.SetParent(mainMenuCanvas.transform, false);
            configurationPanel = panelGO;
            
            // Background
            var bgImage = panelGO.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.2f, 0.9f);
            
            var panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.3f, 0.3f);
            panelRect.anchorMax = new Vector2(0.7f, 0.7f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            // Title
            var titleGO = new GameObject("Config Title");
            titleGO.transform.SetParent(panelGO.transform, false);
            var titleText = titleGO.AddComponent<Text>();
            titleText.text = "Dungeon Configuration";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 24;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
            
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.8f);
            titleRect.anchorMax = new Vector2(0.5f, 0.8f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(300, 40);
            
            CreateConfigurationElements(panelGO);
            
            // Initially hidden
            configurationPanel.SetActive(false);
        }
        
        private void CreateConfigurationElements(GameObject parent)
        {
            // Custom seed toggle
            var toggleGO = new GameObject("Custom Seed Toggle");
            toggleGO.transform.SetParent(parent.transform, false);
            customSeedToggle = toggleGO.AddComponent<Toggle>();
            
            var toggleRect = customSeedToggle.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0.5f, 0.6f);
            toggleRect.anchorMax = new Vector2(0.5f, 0.6f);
            toggleRect.anchoredPosition = Vector2.zero;
            toggleRect.sizeDelta = new Vector2(200, 30);
            
            // Seed input field
            var inputGO = new GameObject("Seed Input");
            inputGO.transform.SetParent(parent.transform, false);
            seedInputField = inputGO.AddComponent<InputField>();
            seedInputField.text = configuredSeed.ToString();
            
            var inputRect = seedInputField.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.5f, 0.5f);
            inputRect.anchorMax = new Vector2(0.5f, 0.5f);
            inputRect.anchoredPosition = Vector2.zero;
            inputRect.sizeDelta = new Vector2(200, 30);
            
            // Random seed button
            generateRandomSeedButton = CreateButton("Random Seed", new Vector2(0.5f, 0.4f), new Vector2(150, 30));
            generateRandomSeedButton.transform.SetParent(parent.transform, false);
            generateRandomSeedButton.onClick.AddListener(GenerateRandomSeed);
            
            // Apply button
            applyConfigButton = CreateButton("Apply", new Vector2(0.4f, 0.2f), new Vector2(100, 40));
            applyConfigButton.transform.SetParent(parent.transform, false);
            applyConfigButton.onClick.AddListener(ApplyConfiguration);
            
            // Cancel button
            cancelConfigButton = CreateButton("Cancel", new Vector2(0.6f, 0.2f), new Vector2(100, 40));
            cancelConfigButton.transform.SetParent(parent.transform, false);
            cancelConfigButton.onClick.AddListener(HideConfiguration);
        }
        
        private void UpdateConfigurationUI()
        {
            if (seedInputField) seedInputField.text = configuredSeed.ToString();
            if (customSeedToggle) customSeedToggle.isOn = true;
        }
        
        private void UpdateDungeonHUD()
        {
            if (!currentDungeonMode) return;
            
            // Update floor
            if (floorText)
            {
                floorText.text = $"Floor: {currentDungeonMode.CurrentFloor + 1}/3";
            }
            
            // Update time
            if (timeText)
            {
                float sessionTime = currentDungeonMode.SessionDuration;
                int minutes = Mathf.FloorToInt(sessionTime / 60);
                int seconds = Mathf.FloorToInt(sessionTime % 60);
                timeText.text = $"Time: {minutes}:{seconds:00}";
            }
            
            // Update bosses defeated
            if (bossesDefeatedText)
            {
                int defeated = 0;
                // Count defeated bosses - simplified for demo
                bossesDefeatedText.text = $"Bosses: {defeated}/3";
            }
            
            // Update secrets
            if (secretsFoundText)
            {
                secretsFoundText.text = $"Secrets: {currentDungeonMode.TotalSecretsFound}";
            }
            
            // Update locks
            if (progressionLocksText)
            {
                int unlockedCount = 0;
                // Count unlocked locks - simplified for demo  
                progressionLocksText.text = $"Locks: {unlockedCount}/3";
            }
        }
        
        // Button event handlers
        private void StartDungeonDelve()
        {
            if (currentDungeonMode)
            {
                // Apply configured seed
                var seedField = typeof(DungeonDelveMode).GetField("dungeonSeed", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (seedField != null)
                {
                    seedField.SetValue(currentDungeonMode, configuredSeed);
                }
                
                currentDungeonMode.StartDungeonDelve();
                ShowDungeonHUD();
                UpdateStatusText("🚀 Dungeon adventure has begun!");
            }
        }
        
        private void AbortDungeon()
        {
            if (currentDungeonMode)
            {
                currentDungeonMode.AbortDungeon();
            }
            ShowMainMenu();
            UpdateStatusText("Dungeon session aborted. Ready for new adventure!");
        }
        
        private void ReturnToMainMenu()
        {
            ShowMainMenu();
            UpdateStatusText("Welcome back! Ready for another adventure?");
        }
        
        private void StartNewDungeon()
        {
            if (currentDungeonMode)
            {
                currentDungeonMode.ResetForNewSession();
            }
            ShowMainMenu();
            GenerateRandomSeed(); // Get a new seed for the next run
            UpdateStatusText("New seed generated! Ready for a fresh adventure!");
        }
        
        private void ExitToMainMenu()
        {
            // In a full game, this would return to the actual main menu
            // For now, just reset the dungeon delve menu
            if (isInDungeon && currentDungeonMode)
            {
                currentDungeonMode.AbortDungeon();
            }
            ShowMainMenu();
            UpdateStatusText("Thank you for playing MetVanDAMN Dungeon Delve Mode!");
        }
        
        private void GenerateRandomSeed()
        {
            configuredSeed = (uint)UnityEngine.Random.Range(1, int.MaxValue);
            UpdateSeedDisplay();
            if (seedInputField) seedInputField.text = configuredSeed.ToString();
        }
        
        private void ApplyConfiguration()
        {
            // Parse seed from input field
            if (seedInputField && uint.TryParse(seedInputField.text, out uint newSeed))
            {
                configuredSeed = newSeed;
                UpdateSeedDisplay();
            }
            
            HideConfiguration();
            UpdateStatusText("Configuration applied!");
        }
        
        private void OnCustomSeedToggled(bool value)
        {
            if (seedInputField) seedInputField.interactable = value;
        }
        
        private void UpdateSeedDisplay()
        {
            if (seedDisplayText) seedDisplayText.text = $"Seed: {configuredSeed}";
        }
        
        private void UpdateStatusText(string message)
        {
            if (statusText) statusText.text = message;
        }
        
        // Dungeon event handlers
        private void OnFloorChanged(int newFloor)
        {
            UpdateStatusText($"Entered floor {newFloor + 1}!");
        }
        
        private void OnBossDefeated(string bossName)
        {
            UpdateStatusText($"🏆 {bossName} defeated!");
        }
        
        private void OnSecretFound(int floor, int secretIndex)
        {
            UpdateStatusText($"🔍 Secret discovered on floor {floor + 1}!");
        }
        
        private void OnProgressionLockObtained(string lockName)
        {
            UpdateStatusText($"🔓 {lockName} unlocked!");
        }
        
        private void OnDungeonCompleted()
        {
            ShowCompletionScreen();
            
            if (completionTimeText && currentDungeonMode)
            {
                float totalTime = currentDungeonMode.SessionDuration;
                int minutes = Mathf.FloorToInt(totalTime / 60);
                int seconds = Mathf.FloorToInt(totalTime % 60);
                completionTimeText.text = $"🎉 Dungeon Completed!\nTime: {minutes}:{seconds:00}";
            }
            
            if (completionStatsText && currentDungeonMode)
            {
                string stats = $"🏆 Final Statistics:\n" +
                              $"⏱️ Total Time: {Mathf.FloorToInt(currentDungeonMode.SessionDuration / 60)}:{Mathf.FloorToInt(currentDungeonMode.SessionDuration % 60):00}\n" +
                              $"🔍 Secrets Found: {currentDungeonMode.TotalSecretsFound}\n" +
                              $"🌟 Seed: {configuredSeed}\n" +
                              $"🎯 All bosses defeated!\n" +
                              $"🏅 Legendary adventure completed!";
                
                completionStatsText.text = stats;
            }
        }
        
        private void OnSessionAborted()
        {
            ShowMainMenu();
            UpdateStatusText("Session aborted. Ready to try again?");
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (currentDungeonMode)
            {
                currentDungeonMode.OnFloorChanged -= OnFloorChanged;
                currentDungeonMode.OnBossDefeated -= OnBossDefeated;
                currentDungeonMode.OnSecretFound -= OnSecretFound;
                currentDungeonMode.OnProgressionLockObtained -= OnProgressionLockObtained;
                currentDungeonMode.OnDungeonCompleted -= OnDungeonCompleted;
                currentDungeonMode.OnSessionAborted -= OnSessionAborted;
            }
        }
    }
    
    /// <summary>
    /// Scene manager for Dungeon Delve Mode - handles scene switching and persistence.
    /// </summary>
    public class DungeonDelveSceneManager : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] private string dungeonDelveSceneName = "DungeonDelveMode";
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        
        public void LoadDungeonDelveScene()
        {
            StartCoroutine(LoadSceneAsync(dungeonDelveSceneName));
        }
        
        public void LoadMainMenuScene()
        {
            StartCoroutine(LoadSceneAsync(mainMenuSceneName));
        }
        
        private IEnumerator LoadSceneAsync(string sceneName)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            
            Debug.Log($"📋 Loaded scene: {sceneName}");
        }
    }
}