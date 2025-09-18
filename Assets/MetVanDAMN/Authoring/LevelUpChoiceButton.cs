using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVanDAMN.Authoring
    {
#nullable enable
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
        [SerializeField] private Color normalColor = new(0.2f, 0.2f, 0.2f, 0.9f);
        [SerializeField] private Color hoverColor = new(0.3f, 0.5f, 0.8f, 0.9f);
        [SerializeField] private Color selectedColor = new(0.5f, 0.8f, 0.3f, 0.9f);
        [SerializeField] private float animationDuration = 0.2f;

        // Runtime data
        private UpgradeDefinition? upgrade;
        private LevelUpChoiceUI? parentUI;
        private bool isHovered = false;
        private bool isSelected = false;

        // Category colors
        private static readonly Color[] CategoryColors =
        {
            new(0.3f, 0.8f, 0.3f, 1f), // Movement - Green
            new(0.8f, 0.3f, 0.3f, 1f), // Offense - Red
            new(0.3f, 0.3f, 0.8f, 1f), // Defense - Blue
            new(0.8f, 0.8f, 0.3f, 1f), // Utility - Yellow
            new(0.8f, 0.3f, 0.8f, 1f)  // Special - Purple
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
            upgrade = upgradeDefinition ?? throw new System.ArgumentNullException(nameof(upgradeDefinition));
            parentUI = parentChoiceUI ?? throw new System.ArgumentNullException(nameof(parentChoiceUI));

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
            descriptionText.textWrappingMode = TextWrappingModes.Normal;

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
            previewText.textWrappingMode = TextWrappingModes.Normal;

            var layoutElement = previewObj.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 20f;
            }

        private void PopulateData()
            {
            if (upgrade == null) return; // Safety guard

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
            var categoryIconSymbol = GetCategoryIcon();

            // Set category icon with proper Unicode symbol display
            if (categoryIcon != null)
                {
                // Since we're using an Image component but need to display Unicode symbols,
                // we'll create a proper sprite-based solution using Unity's built-in methods
                var iconTexture = CreateCategoryIconTexture(categoryIconSymbol, categoryColor);
                var iconSprite = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), new Vector2(0.5f, 0.5f));
                categoryIcon.sprite = iconSprite;
                categoryIcon.color = Color.white; // Use white since color is baked into texture
                }

            // Optionally tint the background slightly with category color
            if (backgroundImage != null)
                {
                var tintedNormal = Color.Lerp(normalColor, categoryColor, 0.1f);
                normalColor = tintedNormal;
                backgroundImage.color = normalColor;
                }
            }

        /// <summary>
        /// Creates a texture with the category icon symbol rendered as an image
        /// </summary>
        private Texture2D CreateCategoryIconTexture(string iconSymbol, Color iconColor)
            {
            // Create a small texture for the icon
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);

            // Fill with transparent background
            var pixels = new Color32[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
                {
                pixels[i] = Color.clear;
                }

            // For this implementation, we'll create simple geometric shapes for each category
            // This provides a complete solution without relying on external font rendering
            switch (iconSymbol)
                {
                case "🏃": // Movement - Arrow pointing right
                    DrawArrow(pixels, 32, iconColor);
                    break;
                case "⚔️": // Offense - Cross/sword shape
                    DrawCross(pixels, 32, iconColor);
                    break;
                case "🛡️": // Defense - Shield shape
                    DrawShield(pixels, 32, iconColor);
                    break;
                case "🔧": // Utility - Gear/cog shape
                    DrawGear(pixels, 32, iconColor);
                    break;
                case "✨": // Special - Star shape
                    DrawStar(pixels, 32, iconColor);
                    break;
                default:
                    DrawCircle(pixels, 32, iconColor); // Fallback
                    break;
                }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
            }

        private void DrawArrow(Color32[] pixels, int size, Color color)
            {
            Color32 c = color;
            int center = size / 2;

            // Draw arrow pointing right
            for (int y = 0; y < size; y++)
                {
                for (int x = 0; x < size; x++)
                    {
                    int index = y * size + x;

                    // Arrow shaft (horizontal line)
                    if (y >= center - 2 && y <= center + 2 && x >= 8 && x <= 20)
                        {
                        pixels[index] = c;
                        }
                    // Arrow head (triangle)
                    else if (x >= 18 && x <= 24)
                        {
                        int distFromCenter = Mathf.Abs(y - center);
                        int allowedDist = (24 - x) * 2;
                        if (distFromCenter <= allowedDist)
                            {
                            pixels[index] = c;
                            }
                        }
                    }
                }
            }

        private void DrawCross(Color32[] pixels, int size, Color color)
            {
            Color32 c = color;
            int center = size / 2;

            // Draw cross/sword
            for (int y = 0; y < size; y++)
                {
                for (int x = 0; x < size; x++)
                    {
                    int index = y * size + x;

                    // Vertical line
                    if (x >= center - 2 && x <= center + 2 && y >= 4 && y <= 28)
                        {
                        pixels[index] = c;
                        }
                    // Horizontal line (crossguard)
                    else if (y >= center - 2 && y <= center + 2 && x >= 8 && x <= 24)
                        {
                        pixels[index] = c;
                        }
                    }
                }
            }

        private void DrawShield(Color32[] pixels, int size, Color color)
            {
            Color32 c = color;
            int center = size / 2;

            // Draw shield shape
            for (int y = 0; y < size; y++)
                {
                for (int x = 0; x < size; x++)
                    {
                    int index = y * size + x;

                    float dx = x - center;
                    float dy = y - center;

                    // Shield outline (rounded rectangle with pointed bottom)
                    if (y < center + 8)
                        {
                        // Upper rounded part
                        if (Mathf.Abs(dx) <= 8 && Mathf.Abs(dy) <= 8)
                            {
                            float dist = Mathf.Sqrt(dx * dx + dy * dy);
                            if ((Mathf.Abs(dx) <= 6 || Mathf.Abs(dy) <= 6) && dist <= 10)
                                {
                                pixels[index] = c;
                                }
                            }
                        }
                    else
                        {
                        // Lower pointed part
                        float allowedWidth = 8 - (y - center - 8) * 0.8f;
                        if (Mathf.Abs(dx) <= allowedWidth)
                            {
                            pixels[index] = c;
                            }
                        }
                    }
                }
            }

        private void DrawGear(Color32[] pixels, int size, Color color)
            {
            Color32 c = color;
            int center = size / 2;

            // Draw gear/cog
            for (int y = 0; y < size; y++)
                {
                for (int x = 0; x < size; x++)
                    {
                    int index = y * size + x;

                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);

                    // Gear teeth (8 teeth)
                    float teethAngle = (angle + Mathf.PI) / (2 * Mathf.PI) * 8;
                    bool isTeeth = (teethAngle % 1) < 0.4f;

                    // Outer ring with teeth
                    if (dist >= 8 && dist <= (isTeeth ? 12 : 10))
                        {
                        pixels[index] = c;
                        }
                    // Inner circle
                    else if (dist <= 5)
                        {
                        pixels[index] = c;
                        }
                    }
                }
            }

        private void DrawStar(Color32[] pixels, int size, Color color)
            {
            Color32 c = color;
            int center = size / 2;

            // Draw 5-pointed star
            for (int y = 0; y < size; y++)
                {
                for (int x = 0; x < size; x++)
                    {
                    int index = y * size + x;

                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx) + Mathf.PI / 2; // Rotate so star points up

                    if (angle < 0) angle += 2 * Mathf.PI;

                    // 5-pointed star calculation
                    float starAngle = (angle % (2 * Mathf.PI / 5)) / (2 * Mathf.PI / 5);
                    float radius = 6 + 4 * Mathf.Cos(starAngle * 2 * Mathf.PI);

                    if (dist <= radius && dist >= 2)
                        {
                        pixels[index] = c;
                        }
                    }
                }
            }

        private void DrawCircle(Color32[] pixels, int size, Color color)
            {
            Color32 c = color;
            int center = size / 2;

            // Draw simple circle as fallback
            for (int y = 0; y < size; y++)
                {
                for (int x = 0; x < size; x++)
                    {
                    int index = y * size + x;

                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= 8 && dist >= 6)
                        {
                        pixels[index] = c;
                        }
                    }
                }
            }

        private Color GetCategoryColor()
            {
            if (upgrade == null) return Color.white;
            int categoryIndex = (int)upgrade.Category;
            if (categoryIndex >= 0 && categoryIndex < CategoryColors.Length)
                {
                return CategoryColors[categoryIndex];
                }
            return Color.white;
            }

        private string GetCategoryIcon()
            {
            if (upgrade == null) return "?";
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
            if (upgrade != null && upgrade.GrantsAbilities != Ability.None)
                {
                preview += $"Grants: {upgrade.GrantsAbilities}\n";
                }

            // Show stat modifications
            if (upgrade != null && !string.IsNullOrEmpty(upgrade.TargetStat))
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
            return upgrade!; // Caller contract expects non-null after successful Initialize
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
