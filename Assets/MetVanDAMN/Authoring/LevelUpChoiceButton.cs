using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVanDAMN.Authoring
{
    /// <summary>
    /// Individual upgrade choice button with hover effects and detailed display.
    /// Shows upgrade name, description, icon, category, and preview information.
    /// </summary>
    public class LevelUpChoiceButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("UI References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image categoryIcon;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI previewText;
        [SerializeField] private TextMeshProUGUI categoryText;

        [Header("Visual Settings")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        [SerializeField] private Color hoverColor = new Color(0.3f, 0.5f, 0.8f, 0.9f);
        [SerializeField] private Color selectedColor = new Color(0.5f, 0.8f, 0.3f, 0.9f);
        [SerializeField] private float animationDuration = 0.2f;

        // Runtime data
        private UpgradeDefinition upgrade;
        private LevelUpChoiceUI parentUI;
        private bool isHovered = false;
        private bool isSelected = false;

        // Category colors
        private static readonly Color[] CategoryColors = 
        {
            new Color(0.3f, 0.8f, 0.3f, 1f), // Movement - Green
            new Color(0.8f, 0.3f, 0.3f, 1f), // Offense - Red
            new Color(0.3f, 0.3f, 0.8f, 1f), // Defense - Blue
            new Color(0.8f, 0.8f, 0.3f, 1f), // Utility - Yellow
            new Color(0.8f, 0.3f, 0.8f, 1f)  // Special - Purple
        };

        // Category icons (Unicode symbols as fallback)
        private static readonly string[] CategoryIcons = 
        {
            "🏃", // Movement
            "⚔️", // Offense
            "🛡️", // Defense
            "🔧", // Utility
            "✨"  // Special
        };

        /// <summary>
        /// Initialize the button with upgrade data
        /// </summary>
        public void Initialize(UpgradeDefinition upgradeDefinition, LevelUpChoiceUI parentChoiceUI)
        {
            upgrade = upgradeDefinition;
            parentUI = parentChoiceUI;

            CreateUI();
            PopulateData();
        }

        private void CreateUI()
        {
            // Create background
            if (backgroundImage == null)
            {
                backgroundImage = gameObject.AddComponent<Image>();
            }
            backgroundImage.color = normalColor;

            // Create layout structure
            CreateLayoutStructure();
        }

        private void CreateLayoutStructure()
        {
            // Main vertical layout
            var verticalLayout = gameObject.AddComponent<VerticalLayoutGroup>();
            verticalLayout.padding = new RectOffset(10, 10, 10, 10);
            verticalLayout.spacing = 5f;
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = false;
            verticalLayout.childForceExpandWidth = true;

            // Header (icon + name + category)
            CreateHeader();

            // Description
            CreateDescription();

            // Preview
            CreatePreview();
        }

        private void CreateHeader()
        {
            var headerObj = new GameObject("Header");
            headerObj.transform.SetParent(transform, false);

            var headerRect = headerObj.AddComponent<RectTransform>();
            var headerLayout = headerObj.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 8f;
            headerLayout.childControlWidth = false;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandHeight = false;

            // Icon
            CreateIcon(headerObj.transform);

            // Name and category container
            CreateNameAndCategory(headerObj.transform);
        }

        private void CreateIcon(Transform parent)
        {
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(parent, false);

            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(40f, 40f);

            iconImage = iconObj.AddComponent<Image>();
            iconImage.color = Color.white;

            var layoutElement = iconObj.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 40f;
            layoutElement.preferredHeight = 40f;
        }

        private void CreateNameAndCategory(Transform parent)
        {
            var nameContainerObj = new GameObject("NameContainer");
            nameContainerObj.transform.SetParent(parent, false);

            var nameLayout = nameContainerObj.AddComponent<VerticalLayoutGroup>();
            nameLayout.spacing = 2f;
            nameLayout.childControlWidth = true;
            nameLayout.childControlHeight = false;

            // Name text
            var nameObj = new GameObject("Name");
            nameObj.transform.SetParent(nameContainerObj.transform, false);

            nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.fontSize = 16f;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = Color.white;

            // Category container
            var categoryContainerObj = new GameObject("CategoryContainer");
            categoryContainerObj.transform.SetParent(nameContainerObj.transform, false);

            var categoryLayout = categoryContainerObj.AddComponent<HorizontalLayoutGroup>();
            categoryLayout.spacing = 4f;
            categoryLayout.childControlWidth = false;
            categoryLayout.childControlHeight = true;

            // Category icon
            var categoryIconObj = new GameObject("CategoryIcon");
            categoryIconObj.transform.SetParent(categoryContainerObj.transform, false);

            var categoryIconRect = categoryIconObj.AddComponent<RectTransform>();
            categoryIconRect.sizeDelta = new Vector2(16f, 16f);

            categoryIcon = categoryIconObj.AddComponent<Image>();

            var categoryIconLayout = categoryIconObj.AddComponent<LayoutElement>();
            categoryIconLayout.preferredWidth = 16f;
            categoryIconLayout.preferredHeight = 16f;

            // Category text
            var categoryTextObj = new GameObject("CategoryText");
            categoryTextObj.transform.SetParent(categoryContainerObj.transform, false);

            categoryText = categoryTextObj.AddComponent<TextMeshProUGUI>();
            categoryText.fontSize = 12f;
            categoryText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        }

        private void CreateDescription()
        {
            var descObj = new GameObject("Description");
            descObj.transform.SetParent(transform, false);

            descriptionText = descObj.AddComponent<TextMeshProUGUI>();
            descriptionText.fontSize = 12f;
            descriptionText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            descriptionText.enableWordWrapping = true;

            var layoutElement = descObj.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 24f;
            layoutElement.flexibleHeight = 1f;
        }

        private void CreatePreview()
        {
            var previewObj = new GameObject("Preview");
            previewObj.transform.SetParent(transform, false);

            previewText = previewObj.AddComponent<TextMeshProUGUI>();
            previewText.fontSize = 11f;
            previewText.color = new Color(0.7f, 0.9f, 0.7f, 1f);
            previewText.fontStyle = FontStyles.Italic;
            previewText.enableWordWrapping = true;

            var layoutElement = previewObj.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 20f;
        }

        private void PopulateData()
        {
            if (upgrade == null) return;

            // Set name
            if (nameText != null)
            {
                nameText.text = upgrade.UpgradeName;
            }

            // Set description
            if (descriptionText != null)
            {
                descriptionText.text = upgrade.Description;
            }

            // Set preview
            if (previewText != null)
            {
                string preview = upgrade.GeneratePreview();
                if (string.IsNullOrEmpty(preview))
                {
                    preview = GenerateBasicPreview();
                }
                previewText.text = preview;
            }

            // Set category
            if (categoryText != null)
            {
                categoryText.text = upgrade.Category.ToString();
            }

            // Set category color and icon
            SetCategoryVisuals();

            // Set icon
            if (iconImage != null)
            {
                if (upgrade.Icon != null)
                {
                    iconImage.sprite = upgrade.Icon;
                }
                else
                {
                    // Create a simple colored square as fallback
                    iconImage.color = GetCategoryColor();
                }
            }
        }

        private void SetCategoryVisuals()
        {
            var categoryColor = GetCategoryColor();

            // Set category icon color
            if (categoryIcon != null)
            {
                categoryIcon.color = categoryColor;
                
                // For now, just use a colored square. In a full implementation,
                // you'd load actual category icon sprites
            }

            // Optionally tint the background slightly with category color
            if (backgroundImage != null)
            {
                var tintedNormal = Color.Lerp(normalColor, categoryColor, 0.1f);
                normalColor = tintedNormal;
                backgroundImage.color = normalColor;
            }
        }

        private Color GetCategoryColor()
        {
            int categoryIndex = (int)upgrade.Category;
            if (categoryIndex >= 0 && categoryIndex < CategoryColors.Length)
            {
                return CategoryColors[categoryIndex];
            }
            return Color.white;
        }

        private string GetCategoryIcon()
        {
            int categoryIndex = (int)upgrade.Category;
            if (categoryIndex >= 0 && categoryIndex < CategoryIcons.Length)
            {
                return CategoryIcons[categoryIndex];
            }
            return "?";
        }

        private string GenerateBasicPreview()
        {
            var preview = "";

            // Show granted abilities
            if (upgrade.GrantsAbilities != Ability.None)
            {
                preview += $"Grants: {upgrade.GrantsAbilities}\n";
            }

            // Show stat modifications
            if (!string.IsNullOrEmpty(upgrade.TargetStat))
            {
                string modifierText = upgrade.ModifierType switch
                {
                    ModifierType.Additive => $"+{upgrade.Value}",
                    ModifierType.Multiplicative => $"×{upgrade.Value:F1}",
                    ModifierType.NewAbility => "NEW",
                    ModifierType.Enhanced => "ENHANCED",
                    _ => upgrade.Value.ToString()
                };

                preview += $"{upgrade.TargetStat}: {modifierText}";
            }

            return preview;
        }

        #region Event Handlers

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            UpdateVisuals();
            
            // Play hover sound
            if (parentUI != null)
            {
                parentUI.PlayHoverSound();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            UpdateVisuals();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                SelectUpgrade();
            }
        }

        #endregion

        private void UpdateVisuals()
        {
            if (backgroundImage == null) return;

            Color targetColor;
            if (isSelected)
            {
                targetColor = selectedColor;
            }
            else if (isHovered)
            {
                targetColor = hoverColor;
            }
            else
            {
                targetColor = normalColor;
            }

            // Animate color change
            StopAllCoroutines();
            StartCoroutine(AnimateColorChange(targetColor));
        }

        private System.Collections.IEnumerator AnimateColorChange(Color targetColor)
        {
            Color startColor = backgroundImage.color;
            float elapsedTime = 0f;

            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.unscaledDeltaTime; // Use unscaled time since game might be paused
                float t = elapsedTime / animationDuration;
                
                backgroundImage.color = Color.Lerp(startColor, targetColor, t);
                
                yield return null;
            }

            backgroundImage.color = targetColor;
        }

        private void SelectUpgrade()
        {
            if (upgrade == null || parentUI == null) return;

            isSelected = true;
            UpdateVisuals();

            // Notify parent UI
            parentUI.SelectUpgrade(upgrade);
        }

        /// <summary>
        /// Get the upgrade definition for this button
        /// </summary>
        public UpgradeDefinition GetUpgrade()
        {
            return upgrade;
        }

        /// <summary>
        /// Set selection state (for external control)
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            UpdateVisuals();
        }
    }
}