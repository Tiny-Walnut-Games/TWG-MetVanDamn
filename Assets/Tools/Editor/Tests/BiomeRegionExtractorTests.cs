using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using TinyWalnutGames.Tools.Editor;

#nullable enable

namespace TinyWalnutGames.Tools.Editor.Tests
	{
	/// <summary>
	/// Tests for BiomeRegionExtractor editor tool functionality
	/// </summary>
	public class BiomeRegionExtractorTests
		{
		private BiomeRegionExtractor? extractor;
		private Texture2D? testBiomeMask;
		private Texture2D? testSpritesheet;

		[SetUp]
		public void SetUp()
			{
			// Create test textures for validation
			testSpritesheet = new Texture2D(128, 128, TextureFormat.RGBA32, false);
			testBiomeMask = new Texture2D(128, 128, TextureFormat.RGBA32, false);

			// Fill test spritesheet with test pattern
			for (int x = 0; x < 128; x++)
				{
				for (int y = 0; y < 128; y++)
					{
					testSpritesheet.SetPixel(x, y, Color.white);
					}
				}

			// Fill biome mask with different colored regions
			for (int x = 0; x < 128; x++)
				{
				for (int y = 0; y < 128; y++)
					{
					if (x < 64 && y < 64) testBiomeMask.SetPixel(x, y, Color.red); // Volcanic
					else if (x >= 64 && y < 64) testBiomeMask.SetPixel(x, y, Color.green); // Forest
					else if (x < 64 && y >= 64) testBiomeMask.SetPixel(x, y, Color.blue); // Ocean
					else testBiomeMask.SetPixel(x, y, Color.yellow); // Desert
					}
				}

			testSpritesheet.Apply();
			testBiomeMask.Apply();
			}

		[TearDown]
		public void TearDown()
			{
			if (testSpritesheet != null)
				Object.DestroyImmediate(testSpritesheet);
			if (testBiomeMask != null)
				Object.DestroyImmediate(testBiomeMask);
			}

		[Test]
		public void BiomeRegionExtractor_CanBeCreated()
			{
			// This test validates the basic editor window can be instantiated
			// We can't easily test the full UI without Unity Editor context
			Assert.DoesNotThrow(() =>
				{
				// Use GetWindow so Unity initializes internal state correctly for show/close lifecycle
				BiomeRegionExtractor window =
					EditorWindow.GetWindow<BiomeRegionExtractor>(utility: true, title: "Biome Region Extractor",
						focus: false);
				Assert.IsNotNull(window);
				// Ensure the window is shown before closing to avoid Close() NREs in some Unity versions
				window.Show();
				if (window) // Unity null check
					{
					window.Close();
					}
				});
			}

		[Test]
		public void BiomeMapping_DataStructure_WorksCorrectly()
			{
			var mapping = new BiomeRegionExtractor.BiomeMapping
				{
				biomeName = "TestBiome",
				maskColor = Color.red,
				includeInExport = true,
				exportFolderSuffix = "test_biome"
				};

			Assert.AreEqual("TestBiome", mapping.biomeName);
			Assert.AreEqual(Color.red, mapping.maskColor);
			Assert.IsTrue(mapping.includeInExport);
			Assert.AreEqual("test_biome", mapping.exportFolderSuffix);
			}

		[Test]
		public void BiomeMetadata_Serialization_WorksCorrectly()
			{
			var metadata = new BiomeRegionExtractor.BiomeMetadata
				{
				biomeName = "TestBiome",
				biomeColor = Color.green
				};

			string json = JsonUtility.ToJson(metadata);
			Assert.IsNotEmpty(json);
			Assert.IsTrue(json.Contains("TestBiome"));

			BiomeRegionExtractor.BiomeMetadata deserialized =
				JsonUtility.FromJson<BiomeRegionExtractor.BiomeMetadata>(json);
			Assert.AreEqual("TestBiome", deserialized.biomeName);
			Assert.AreEqual(Color.green, deserialized.biomeColor);
			}

		[Test]
		public void ColorComparison_WithTolerance_WorksCorrectly()
			{
			// Test the color matching logic used in biome detection
			Color color1 = new Color(1f, 0f, 0f, 1f); // Pure red
			Color color2 = new Color(0.95f, 0.05f, 0.05f, 1f); // Nearly red
			Color color3 = new Color(0f, 1f, 0f, 1f); // Pure green

			float tolerance = 0.1f;

			// Colors should match within tolerance
			Assert.IsTrue(IsColorMatch(color1, color2, tolerance));

			// Colors should not match beyond tolerance
			Assert.IsFalse(IsColorMatch(color1, color3, tolerance));
			}

		private bool IsColorMatch(Color color1, Color color2, float tolerance)
			{
			return Mathf.Abs(color1.r - color2.r) < tolerance &&
			       Mathf.Abs(color1.g - color2.g) < tolerance &&
			       Mathf.Abs(color1.b - color2.b) < tolerance;
			}

		[Test]
		public void FileNamingPattern_Replacement_WorksCorrectly()
			{
			string pattern = "{biome}_{row}_{col}";
			string result = pattern
				.Replace("{biome}", "Forest")
				.Replace("{row}", "2")
				.Replace("{col}", "5");

			Assert.AreEqual("Forest_2_5", result);
			}

		[Test]
		public void CellSizeValidation_DetectsInvalidValues()
			{
			// Test validation logic for cell sizes
			Vector2Int validCellSize = new Vector2Int(64, 64);
			Vector2Int invalidCellSize1 = new Vector2Int(0, 64);
			Vector2Int invalidCellSize2 = new Vector2Int(64, -1);

			Assert.IsTrue(IsCellSizeValid(validCellSize));
			Assert.IsFalse(IsCellSizeValid(invalidCellSize1));
			Assert.IsFalse(IsCellSizeValid(invalidCellSize2));
			}

		private bool IsCellSizeValid(Vector2Int cellSize)
			{
			return cellSize.x > 0 && cellSize.y > 0;
			}

		[Test]
		public void GridDimensions_CalculatedCorrectly()
			{
			int textureWidth = 128;
			int textureHeight = 128;
			Vector2Int cellSize = new Vector2Int(32, 32);

			int expectedColumns = textureWidth / cellSize.x; // 4
			int expectedRows = textureHeight / cellSize.y; // 4

			Assert.AreEqual(4, expectedColumns);
			Assert.AreEqual(4, expectedRows);

			// Test with non-divisible dimensions
			Vector2Int oddCellSize = new Vector2Int(30, 30);
			int oddColumns = textureWidth / oddCellSize.x; // 4 (truncated)
			int oddRows = textureHeight / oddCellSize.y; // 4 (truncated)

			Assert.AreEqual(4, oddColumns);
			Assert.AreEqual(4, oddRows);
			}
		}
	}
