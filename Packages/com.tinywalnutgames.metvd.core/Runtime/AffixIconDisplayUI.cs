using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace TinyWalnutGames.MetVD.Core
	{
#nullable enable
	/// <summary>
	/// Component for UI representation of enemy affix icons
	/// Integrates with health bar and enemy display systems
	/// </summary>
	public struct AffixIconDisplay : IComponentData
		{
		/// <summary>
		/// Maximum number of icons to display simultaneously
		/// </summary>
		public byte MaxIcons;

		/// <summary>
		/// Spacing between icons in UI units
		/// </summary>
		public float IconSpacing;

		/// <summary>
		/// Size of each icon in UI units
		/// </summary>
		public float IconSize;

		/// <summary>
		/// Whether icons should be displayed above the entity (world space) or in UI panel
		/// </summary>
		public bool UseWorldSpace;

		public AffixIconDisplay(byte maxIcons = 4, float iconSpacing = 25.0f, float iconSize = 20.0f, bool useWorldSpace = true)
			{
			MaxIcons = maxIcons;
			IconSpacing = iconSpacing;
			IconSize = iconSize;
			UseWorldSpace = useWorldSpace;
			}
		}

	/// <summary>
	/// MonoBehaviour component for managing affix icon display in Unity UI
	/// This bridges ECS data with Unity's UI system
	/// </summary>
	public class AffixIconDisplayUI : MonoBehaviour
		{
		[Header("Icon Display Settings")]
		[SerializeField] private GameObject iconPrefab;
		[SerializeField] private Transform iconContainer;
		[SerializeField] private int maxIcons = 4;
		[SerializeField] private float iconSpacing = 25.0f;

		[Header("Icon Resources")]
		[SerializeField] private Sprite[] affixIcons;

		private Entity targetEntity;
		private EntityManager entityManager;
		private Image[] iconImages = System.Array.Empty<Image>(); // Initialized to satisfy nullability
		private bool isInitialized;

		/// <summary>
		/// Initialize the display for a specific enemy entity
		/// </summary>
		public void Initialize(Entity entity, EntityManager em)
			{
			targetEntity = entity;
			entityManager = em;

			CreateIconImages();
			isInitialized = true;
			}

		/// <summary>
		/// Update the display with current affix data
		/// </summary>
		public void UpdateDisplay()
			{
			if (!isInitialized || !entityManager.Exists(targetEntity))
				{
				return;
				}

			// Check if entity has naming component and should show icons
			if (!entityManager.HasComponent<EnemyNaming>(targetEntity))
				{
				HideAllIcons();
				return;
				}

			EnemyNaming naming = entityManager.GetComponentData<EnemyNaming>(targetEntity);
			if (!naming.ShowIcons || naming.DisplayMode == AffixDisplayMode.NamesOnly)
				{
				HideAllIcons();
				return;
				}

			// Get affix data
			if (!entityManager.HasBuffer<EnemyAffixBufferElement>(targetEntity))
				{
				HideAllIcons();
				return;
				}

			DynamicBuffer<EnemyAffixBufferElement> affixBuffer = entityManager.GetBuffer<EnemyAffixBufferElement>(targetEntity);
			DisplayAffixIcons(affixBuffer);
			}

		/// <summary>
		/// Create the UI icon images
		/// </summary>
		private void CreateIconImages()
			{
			if (iconContainer == null)
				{
				iconContainer = transform;
				}

			iconImages = new Image[maxIcons];

			for (int i = 0; i < maxIcons; i++)
				{
				GameObject iconGO = iconPrefab != null ? Instantiate(iconPrefab, iconContainer) : new GameObject($"Icon_{i}");
				iconGO.transform.SetParent(iconContainer);

				if (!iconGO.TryGetComponent<RectTransform>(out RectTransform rectTransform))
					{
					rectTransform = iconGO.AddComponent<RectTransform>();
					}

				// Position icon
				rectTransform.anchoredPosition = new Vector2(i * iconSpacing, 0);
				rectTransform.sizeDelta = new Vector2(20, 20);

				if (!iconGO.TryGetComponent<Image>(out Image image))
					{
					image = iconGO.AddComponent<Image>();
					}

				iconImages[i] = image;
				iconGO.SetActive(false);
				}
			}

		/// <summary>
		/// Display icons for the given affixes
		/// </summary>
		private void DisplayAffixIcons(DynamicBuffer<EnemyAffixBufferElement> affixes)
			{
			// Hide all icons first
			HideAllIcons();

			// Display up to maxIcons
			int iconsToShow = Mathf.Min(affixes.Length, maxIcons);

			for (int i = 0; i < iconsToShow; i++)
				{
				EnemyAffix affix = affixes[i].Value;
				Sprite? sprite = GetSpriteForAffix(affix.IconRef.ToString());

				if (sprite != null && i < iconImages.Length)
					{
					iconImages[i].sprite = sprite;
					iconImages[i].gameObject.SetActive(true);
					}
				}
			}

		/// <summary>
		/// Hide all icon displays
		/// </summary>
		private void HideAllIcons()
			{
			if (iconImages != null)
				{
				foreach (Image icon in iconImages)
					{
					if (icon != null)
						{
						icon.gameObject.SetActive(false);
						}
					}
				}
			}

		/// <summary>
		/// Get sprite for affix icon reference
		/// In a real implementation, this would load from Resources or an asset database
		/// </summary>
		private Sprite? GetSpriteForAffix(string iconRef)
			{
			// Simple mapping for demo purposes
			// In production, this would load from Resources, Addressables, or an asset database
			if (affixIcons != null)
				{
				foreach (Sprite sprite in affixIcons)
					{
					if (sprite != null && sprite.name.Contains(iconRef.Replace("icon_", "").Replace(".png", "")))
						{
						return sprite;
						}
					}
				}

			// Return a default icon or null
			return null; // explicit null allowed with nullable return type
			}

		/// <summary>
		/// Update display every frame if needed
		/// </summary>
		private void Update()
			{
			if (isInitialized)
				{
				UpdateDisplay();
				}
			}

		/// <summary>
		/// Clean up when destroyed
		/// </summary>
		private void OnDestroy()
			{
			if (iconImages != null)
				{
				foreach (Image icon in iconImages)
					{
					if (icon != null && icon.gameObject != null)
						{
						DestroyImmediate(icon.gameObject);
						}
					}
				}
			}
		}

	/// <summary>
	/// System that manages affix icon display updates
	/// Bridges ECS data with MonoBehaviour UI components
	/// </summary>
	[UpdateInGroup(typeof(PresentationSystemGroup))]
	public partial class AffixIconDisplaySystem : SystemBase
		{
		protected override void OnUpdate()
			{
			// Find all AffixIconDisplayUI components and update them
			foreach (AffixIconDisplayUI displayUI in Object.FindObjectsByType<AffixIconDisplayUI>(FindObjectsSortMode.None))
				{
				if (displayUI != null)
					{
					displayUI.UpdateDisplay();
					}
				}
			}
		}
	}
