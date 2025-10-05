using UnityEngine;
using UnityEditor;

namespace TinyWalnutGames.MetVanDAMN.Authoring.Editor
	{
	/// <summary>
	/// Editor menu for MetVanDAMN map generation tools.
	/// Provides easy access to map generation and visualization features.
	/// </summary>
	public static class MapGenerationMenu
		{
		[MenuItem("Tiny Walnut Games/MetVanDAMN!/Map Generation/🗺️ Generate World Map", priority = 50)]
		public static void GenerateWorldMap()
			{
			var mapGenerator = Object.FindFirstObjectByType<MetVanDAMNMapGenerator>();
			if (mapGenerator)
				{
				mapGenerator.GenerateWorldMap();
				Debug.Log("✅ World map generation complete!");
				}
			else
				{
				Debug.LogWarning(
					"⚠️ No MetVanDAMNMapGenerator found in scene. Please create a demo scene first using 'Tiny Walnut Games/MetVanDAMN/Create Base DEMO Scene'");
				}
			}

		[MenuItem("Tiny Walnut Games/MetVanDAMN!/Map Generation/🧭 Toggle Minimap", priority = 51)]
		public static void ToggleMinimap()
			{
			var mapGenerator = Object.FindFirstObjectByType<MetVanDAMNMapGenerator>();
			if (mapGenerator)
				{
				mapGenerator.ToggleMinimap();
				}
			else
				{
				Debug.LogWarning("⚠️ No MetVanDAMNMapGenerator found in scene.");
				}
			}

		[MenuItem("Tiny Walnut Games/MetVanDAMN!/Map Generation/🖼️ Toggle Detailed Map", priority = 52)]
		public static void ToggleDetailedMap()
			{
			var mapGenerator = Object.FindFirstObjectByType<MetVanDAMNMapGenerator>();
			if (mapGenerator)
				{
				mapGenerator.ToggleDetailedMap();
				}
			else
				{
				Debug.LogWarning("⚠️ No MetVanDAMNMapGenerator found in scene.");
				}
			}

		[MenuItem("Tiny Walnut Games/MetVanDAMN!/Map Generation/📁 Export Map as Image", priority = 53)]
		public static void ExportMapAsImage()
			{
			var mapGenerator = Object.FindFirstObjectByType<MetVanDAMNMapGenerator>();
			if (mapGenerator)
				{
				// Temporarily enable export and regenerate
				mapGenerator.exportMapAsImage = true;
				mapGenerator.GenerateWorldMap();
				mapGenerator.exportMapAsImage = false;
				}
			else
				{
				Debug.LogWarning("⚠️ No MetVanDAMNMapGenerator found in scene.");
				}
			}

		[MenuItem("Tiny Walnut Games/MetVanDAMN!/Map Generation/🔄 Regenerate All Maps", priority = 54)]
		public static void RegenerateAllMaps()
			{
			var mapGenerator = Object.FindFirstObjectByType<MetVanDAMNMapGenerator>();
			if (mapGenerator)
				{
				mapGenerator.RegenerateMap();
				Debug.Log("🔄 All maps regenerated successfully!");
				}
			else
				{
				Debug.LogWarning("⚠️ No MetVanDAMNMapGenerator found in scene.");
				}
			}

		// Validation menu items
		[MenuItem("Tiny Walnut Games/MetVanDAMN!/Map Generation/🗺️ Generate World Map", true)]
		public static bool ValidateGenerateWorldMap()
			{
			return Application.isPlaying && Object.FindFirstObjectByType<MetVanDAMNMapGenerator>() != null;
			}

		[MenuItem("Tiny Walnut Games/MetVanDAMN!/Map Generation/🧭 Toggle Minimap", true)]
		public static bool ValidateToggleMinimap()
			{
			return Application.isPlaying && Object.FindFirstObjectByType<MetVanDAMNMapGenerator>() != null;
			}

		[MenuItem("Tiny Walnut Games/MetVanDAMN!/Map Generation/🖼️ Toggle Detailed Map", true)]
		public static bool ValidateToggleDetailedMap()
			{
			return Application.isPlaying && Object.FindFirstObjectByType<MetVanDAMNMapGenerator>() != null;
			}

		[MenuItem("Tiny Walnut Games/MetVanDAMN!/Map Generation/📁 Export Map as Image", true)]
		public static bool ValidateExportMapAsImage()
			{
			return Application.isPlaying && Object.FindFirstObjectByType<MetVanDAMNMapGenerator>() != null;
			}

		[MenuItem("Tiny Walnut Games/MetVanDAMN!/Map Generation/🔄 Regenerate All Maps", true)]
		public static bool ValidateRegenerateAllMaps()
			{
			return Application.isPlaying && Object.FindFirstObjectByType<MetVanDAMNMapGenerator>() != null;
			}
		}
	}
