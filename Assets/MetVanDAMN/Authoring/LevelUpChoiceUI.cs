using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace TinyWalnutGames.MetVanDAMN.Authoring
{
    /// <summary>
    /// UI system for displaying level-up upgrade choices to the player.
    /// Shows upgrade options with name, description, icon, category, and preview.
    /// </summary>
    public class LevelUpChoiceUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject choicePanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Transform choiceContainer;
        [SerializeField] private GameObject choiceButtonPrefab;
        [SerializeField] private Button cancelButton;

        [Header("Choice Button Settings")]
        [SerializeField] private float buttonSpacing = 20f;
        [SerializeField] private Vector2 buttonSize = new Vector2(300f, 120f);

        [Header("Audio")]
        [SerializeField] private AudioClip choiceConfirmSound;
        [SerializeField] private AudioClip choiceHoverSound;
        [SerializeField] private AudioClip panelOpenSound;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging = true;

        // Runtime data
        private UpgradeDefinition[] currentChoices;
        private LevelUpChoiceSystem choiceSystem;
        private AudioSource audioSource;
        private List<LevelUpChoiceButton> choiceButtons = new List<LevelUpChoiceButton>();

        // Events
        public System.Action<UpgradeDefinition> OnUpgradeSelected;
        public System.Action OnChoiceCancelled;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Find choice system
            choiceSystem = FindObjectOfType<LevelUpChoiceSystem>();

            // Setup UI
            SetupUI();
            
            // Hide panel initially
            if (choicePanel != null)
            {
                choicePanel.SetActive(false);
            }
        }

        private void Start()
        {
            // Subscribe to choice system events
            if (choiceSystem != null)
            {
                choiceSystem.OnChoicesGenerated += ShowChoices;
            }
        }

        private void OnDestroy()
        {
            if (choiceSystem != null)
            {
                choiceSystem.OnChoicesGenerated -= ShowChoices;
            }
        }

        private void SetupUI()
        {
            // Create UI if not already assigned
            if (choicePanel == null)
            {
                CreateUI();
            }

            // Setup cancel button
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(CancelChoice);
            }
        }

        private void CreateUI()
        {
            // Find or create canvas
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("LevelUpChoiceCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000; // High priority
                canvasObj.AddComponent<GraphicRaycaster>();

                // Add EventSystem if not present
                if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    var eventSystemObj = new GameObject("EventSystem");
                    eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }

            // Create main panel
            var panelObj = new GameObject("LevelUpChoicePanel");
            panelObj.transform.SetParent(canvas.transform, false);

            var rectTransform = panelObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            // Add background
            var bgImage = panelObj.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.8f); // Semi-transparent black

            choicePanel = panelObj;

            // Create title
            CreateTitleText();

            // Create choice container
            CreateChoiceContainer();

            // Create cancel button
            CreateCancelButton();
        }

        private void CreateTitleText()
        {
            var titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(choicePanel.transform, false);

            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.8f);
            titleRect.anchorMax = new Vector2(0.5f, 0.9f);
            titleRect.sizeDelta = new Vector2(600f, 60f);

            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "🎉 LEVEL UP! Choose Your Upgrade";
            titleText.fontSize = 32f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
        }

        private void CreateChoiceContainer()
        {
            var containerObj = new GameObject("ChoiceContainer");
            containerObj.transform.SetParent(choicePanel.transform, false);

            var containerRect = containerObj.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.1f, 0.2f);
            containerRect.anchorMax = new Vector2(0.9f, 0.7f);
            containerRect.sizeDelta = Vector2.zero;

            // Add horizontal layout group
            var layoutGroup = containerObj.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = buttonSpacing;
            layoutGroup.padding = new RectOffset(20, 20, 20, 20);
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;

            choiceContainer = containerObj.transform;
        }

        private void CreateCancelButton()
        {
            var cancelObj = new GameObject("CancelButton");
            cancelObj.transform.SetParent(choicePanel.transform, false);

            var cancelRect = cancelObj.AddComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(0.5f, 0.05f);
            cancelRect.anchorMax = new Vector2(0.5f, 0.15f);
            cancelRect.sizeDelta = new Vector2(200f, 40f);

            cancelButton = cancelObj.AddComponent<Button>();
            
            // Add button background
            var buttonImage = cancelObj.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

            // Add button text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(cancelObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = "Cancel (Keep Current Level)";
            buttonText.fontSize = 14f;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;

            cancelButton.onClick.AddListener(CancelChoice);
        }

        /// <summary>
        /// Show upgrade choices to the player
        /// </summary>
        public void ShowChoices(UpgradeDefinition[] choices)
        {
            if (choices == null || choices.Length == 0)
            {
                if (enableDebugLogging)
                {
                    Debug.LogWarning("No choices provided to ShowChoices");
                }
                return;
            }

            currentChoices = choices;

            // Clear existing choice buttons
            ClearChoiceButtons();

            // Create new choice buttons
            CreateChoiceButtons();

            // Show panel
            if (choicePanel != null)
            {
                choicePanel.SetActive(true);
                Time.timeScale = 0f; // Pause game during choice

                // Play open sound
                if (panelOpenSound && audioSource)
                {
                    audioSource.PlayOneShot(panelOpenSound);
                }

                if (enableDebugLogging)
                {
                    Debug.Log($"🎯 Showing {choices.Length} upgrade choices");
                }
            }
        }

        private void ClearChoiceButtons()
        {
            foreach (var button in choiceButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            choiceButtons.Clear();
        }

        private void CreateChoiceButtons()
        {
            for (int i = 0; i < currentChoices.Length; i++)
            {
                var choice = currentChoices[i];
                var buttonObj = CreateChoiceButton(choice, i);
                choiceButtons.Add(buttonObj);
            }
        }

        private LevelUpChoiceButton CreateChoiceButton(UpgradeDefinition upgrade, int index)
        {
            var buttonObj = new GameObject($"ChoiceButton_{index}");
            buttonObj.transform.SetParent(choiceContainer, false);

            // Add RectTransform
            var rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = buttonSize;

            // Add choice button component
            var choiceButton = buttonObj.AddComponent<LevelUpChoiceButton>();
            choiceButton.Initialize(upgrade, this);

            // Add to layout element for proper sizing
            var layoutElement = buttonObj.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = buttonSize.x;
            layoutElement.preferredHeight = buttonSize.y;

            return choiceButton;
        }

        /// <summary>
        /// Handle upgrade selection
        /// </summary>
        public void SelectUpgrade(UpgradeDefinition upgrade)
        {
            if (upgrade == null)
            {
                Debug.LogError("Cannot select null upgrade");
                return;
            }

            // Play confirmation sound
            if (choiceConfirmSound && audioSource)
            {
                audioSource.PlayOneShot(choiceConfirmSound);
            }

            if (enableDebugLogging)
            {
                Debug.Log($"✅ Player selected upgrade: {upgrade.UpgradeName}");
            }

            // Apply upgrade through choice system
            if (choiceSystem != null)
            {
                choiceSystem.ChooseUpgrade(upgrade);
            }

            // Trigger event
            OnUpgradeSelected?.Invoke(upgrade);

            // Hide panel
            HidePanel();
        }

        /// <summary>
        /// Handle choice cancellation
        /// </summary>
        public void CancelChoice()
        {
            if (enableDebugLogging)
            {
                Debug.Log("❌ Player cancelled upgrade choice");
            }

            OnChoiceCancelled?.Invoke();
            HidePanel();
        }

        /// <summary>
        /// Hide the choice panel
        /// </summary>
        private void HidePanel()
        {
            if (choicePanel != null)
            {
                choicePanel.SetActive(false);
                Time.timeScale = 1f; // Resume game
            }

            ClearChoiceButtons();
            currentChoices = null;
        }

        /// <summary>
        /// Play hover sound for choice buttons
        /// </summary>
        public void PlayHoverSound()
        {
            if (choiceHoverSound && audioSource)
            {
                audioSource.PlayOneShot(choiceHoverSound);
            }
        }

        /// <summary>
        /// Force hide panel (for debugging)
        /// </summary>
        [ContextMenu("Hide Panel")]
        public void ForceHidePanel()
        {
            HidePanel();
        }

        /// <summary>
        /// Test show panel with dummy choices (for debugging)
        /// </summary>
        [ContextMenu("Test Show Choices")]
        public void TestShowChoices()
        {
            // Create dummy choices for testing
            var dummyChoices = new UpgradeDefinition[3];
            // This would normally come from actual UpgradeDefinition assets
            // For testing we'd need actual ScriptableObject instances
            
            if (enableDebugLogging)
            {
                Debug.Log("TestShowChoices called - would need actual UpgradeDefinition assets to test");
            }
        }

        private void OnValidate()
        {
            // Ensure button size is reasonable
            if (buttonSize.x < 200f) buttonSize.x = 200f;
            if (buttonSize.y < 80f) buttonSize.y = 80f;
            
            // Ensure spacing is positive
            if (buttonSpacing < 0f) buttonSpacing = 0f;
        }
    }
}