using System.IO;
using UnityEditor;
using UnityEngine;

namespace TinyWalnutGames.GridLayerEditor
	{
	/// <summary>
	/// Editor utility for creating GridLayerConfig assets via the Unity menu.
	/// </summary>
	public static class GridLayerConfigEditor
		{
		/// <summary>
		/// Creates a new GridLayerConfig asset in the selected folder or in Assets.
		/// Now with intelligent duplicate prevention - checks for existing configs first.
		/// 🔥 UPDATED: Made menu path unique to prevent conflicts
		/// </summary>
		[MenuItem("Assets/Create/Tiny Walnut Games/Grid Layer Editor/Grid Layer Config", priority = 1)]
		public static void CreateGridLayerConfig()
			{
			// Get the path of the selected object in the Project window
			string path = AssetDatabase.GetAssetPath(Selection.activeObject);
			if (string.IsNullOrEmpty(path))
				{
				path = "Assets";
				}
			else if (!Directory.Exists(path))
				{
				path = Path.GetDirectoryName(path);
				}

			// 🔥 DUPLICATE PREVENTION MAGIC: Check for existing GridLayerConfig in target directory
			if (HasExistingGridLayerConfigInDirectory(path))
				{
				// Ask user if they want to create another one
				bool createAnyway = EditorUtility.DisplayDialog(
					"Grid Layer Config Already Exists",
					$"A GridLayerConfig asset already exists in '{path}'.\n\nDo you want to create another one?",
					"Create Another",
					"Cancel"
				);

				if (!createAnyway)
					{
					Debug.Log($"Grid Layer Config creation cancelled - existing config found in {path}");
					return;
					}
				}

			// Generate a unique asset path for the new config
			string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(path, "GridLayerConfig.asset"));

			// 🔥 ADDITIONAL SAFETY: Double-check that we're not about to overwrite an existing asset
			if (AssetDatabase.LoadAssetAtPath<GridLayerConfig>(assetPathAndName) != null)
				{
				Debug.LogWarning($"GridLayerConfig already exists at {assetPathAndName}. Using alternative name.");
				assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(path, "NewGridLayerConfig.asset"));
				}

			// Create and save the new GridLayerConfig asset
			GridLayerConfig asset = ScriptableObject.CreateInstance<GridLayerConfig>();
			AssetDatabase.CreateAsset(asset, assetPathAndName);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			// Focus the Project window and select the new asset
			EditorUtility.FocusProjectWindow();
			Selection.activeObject = asset;

			Debug.Log($"Created new GridLayerConfig at: {assetPathAndName}");
			}

		/// <summary>
		/// 🔥 DUPLICATE DETECTION SPELL: Check if a GridLayerConfig already exists in the specified directory
		/// </summary>
		private static bool HasExistingGridLayerConfigInDirectory(string directoryPath)
			{
			if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
				{
				return false;
				}

			// Search for any .asset files in the directory that are GridLayerConfig assets
			string [ ] assetFiles = Directory.GetFiles(directoryPath, "*.asset", SearchOption.TopDirectoryOnly);

			foreach (string assetFile in assetFiles)
				{
				// Convert to Unity asset path format
				string unityAssetPath = assetFile.Replace('\\', '/');
				if (unityAssetPath.StartsWith(Application.dataPath))
					{
					unityAssetPath = "Assets" + unityAssetPath [ Application.dataPath.Length.. ];
					}

				// Check if this asset is a GridLayerConfig
				GridLayerConfig asset = AssetDatabase.LoadAssetAtPath<GridLayerConfig>(unityAssetPath);
				if (asset != null)
					{
					Debug.Log($"Found existing GridLayerConfig: {unityAssetPath}");
					return true;
					}
				}

			return false;
			}

		/// <summary>
		/// 🔥 BONUS SPELL: Validate menu item availability (optional - for future enhancement)
		/// This method controls when the menu item should be enabled/disabled
		/// 🔥 UPDATED: Made validation path unique to match the menu item
		/// </summary>
		[MenuItem("Assets/Create/Tiny Walnut Games/Grid Layer Editor/Grid Layer Config", true, priority = 1)]
		public static bool ValidateCreateGridLayerConfig()
			{
			// Menu item is always available, but we could add conditions here:
			// - Check if GridLayerEditor package is properly installed
			// - Verify that the target directory is writable
			// - Ensure we're not in play mode

			return !Application.isPlaying; // Don't allow creation during play mode
			}
		}
	}
