//  This script is intended to be used in the Unity Editor only, stored in an Editor folder to ensure it is not included in builds.
#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites; // 🔥 LIBERATION: No more conditional imports!
using UnityEngine;

namespace TinyWalnutGames.Tools.Editor
	{
	/// <summary>
	/// Batch Sprite Slicer
	/// Use this tool to slice multiple sprites from selected textures in the project.
	/// I personally like to run a search for all textures in the project
	/// and then slice the ones I want to slice. I work in smallish batches of twenty or so as to not overwhelm the editor.
	/// I wanted to thread the tasks, but I am understanding that the Unity API is not thread-safe,
	/// so I had to use the ISpriteEditorDataProvider interface instead.
	/// see https://docs.unity3d.com/Packages/com.unity.2d.sprite@latest/manual/SpriteEditorDataProvider.html
	/// 
	/// 🔥 NUCLEAR LIBERATION: Conditional compilation walls DESTROYED!
	/// This is an Editor-only tool in an Editor folder - no need for sprite editor feature detection!
	/// </summary>
	public class BatchSpriteSlicer : EditorWindow
		{
		/// <summary>
		/// Whether to use cell size for slicing or fixed columns/rows.
		/// * If true, cellSize will be used to determine the size of each sprite slice.
		/// * If false, columns and rows will be used instead.
		/// </summary>
		[SerializeField] private bool useCellSize = false;

		/// <summary>
		/// Size of each cell for slicing when useCellSize is true.
		/// </summary>
		[SerializeField] private Vector2 cellSize = new(64, 64);

		/// <summary>
		/// Number of columns for slicing when useCellSize is false.
		/// </summary>
		[SerializeField] private int columns = 12;

		/// <summary>
		/// Number of rows for slicing when useCellSize is false.
		/// </summary>
		[SerializeField] private int rows = 8;

		/// <summary>
		/// Alignment for the pivot of the sliced sprites.
		/// </summary>
		[SerializeField] private SpriteAlignment pivotAlignment = SpriteAlignment.BottomCenter;

		/// <summary>
		/// Whether to ignore empty rectangles when slicing.
		/// </summary>
		[SerializeField] private bool ignoreEmptyRects = true;

		/// <summary>
		/// List of copied sprite rectangles from the last copy operation.
		/// We're now including custom physics shapes (outlines) as well
		/// </summary>
		private static List<SpriteRect> copiedRects = null;
		private static readonly Dictionary<string, List<Vector2 [ ]>> copiedOutlines = new();

		/// <summary>
		/// Width and height of the texture from which the last copy operation was performed.
		/// </summary>
		private static int copiedTexWidth = 0;
		private static int copiedTexHeight = 0;

		/// <summary>
		/// Opens the Batch Sprite Slicer window in the Unity Editor.
		/// ⚠Intended use!⚠ Menu path made unique to avoid conflicts with existing tools
		/// Original path: "Tools/Batch Sprite Slicer" - Modified to prevent duplicate menu warnings
		/// </summary>
		[MenuItem("Tiny Walnut Games/Tools/Batch Sprite Slicer")] // Sacred Symbol Preservation: Unique path prevents menu conflicts
		public static void OpenWindow ()
			{
			GetWindow<BatchSpriteSlicer>("Batch Sprite Slicer"); // Opens the window with a title
			}

		/// <summary>
		/// Called when the window is drawn in the Unity Editor.
		/// 🔥 LIBERATED: No more conditional compilation madness!
		/// </summary>
		private void OnGUI ()
			{
			GUILayout.Label("Batch Sprite Slicer", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox(
				"Slice multiple sprites from selected textures in the project. " +
				"You can copy/paste slice layouts, adjust pivots, or grid-slice textures in batches. " +
				"Requires the 2D Sprite package (com.unity.2d.sprite).", MessageType.Info);

			GUILayout.Space(10);
			this.useCellSize = EditorGUILayout.Toggle("Use Cell Size", this.useCellSize);

			if (this.useCellSize)
				{
				this.cellSize = EditorGUILayout.Vector2Field("Cell Size", this.cellSize);
				EditorGUILayout.HelpBox(
					"Set the width and height (in pixels) for each sprite cell. " +
					"The slicer will automatically determine the number of columns and rows based on the texture size.",
					MessageType.None);
				}
			else
				{
				GUILayout.Label("Grid Settings", EditorStyles.boldLabel);
				EditorGUILayout.HelpBox(
					"Specify the number of columns and rows to divide the texture into. " +
					"Each cell will be sized to fit the grid.", MessageType.None);
				this.columns = EditorGUILayout.IntField("Columns", this.columns);
				this.rows = EditorGUILayout.IntField("Rows", this.rows);
				}

			EditorGUILayout.Space();

			this.pivotAlignment = (SpriteAlignment)EditorGUILayout.EnumPopup("Pivot Alignment", this.pivotAlignment);
			this.ignoreEmptyRects = EditorGUILayout.Toggle("Ignore Empty Rects", this.ignoreEmptyRects);

			EditorGUILayout.Space();

			GUILayout.Label("Slice Layout Operations", EditorStyles.boldLabel);

			if (GUILayout.Button(new GUIContent(
				"Copy Rect Layout",
				"Copy the current sprite slice rectangles from the first selected texture. " +
				"You can paste this layout onto other textures of similar proportions.")))
				{
				this.CopySlicesFromSelected();
				}
			EditorGUILayout.HelpBox(
				"Copy the slice layout from the first selected texture. " +
				"Useful for applying the same slicing to multiple textures.", MessageType.None);

			using (new EditorGUI.DisabledScope(copiedRects == null))
				{
				if (GUILayout.Button(new GUIContent(
					"Paste Rect Layout",
					"Paste the previously copied slice layout onto all selected textures. " +
					"The layout will be scaled to fit each texture's size.")))
					{
					if (Selection.objects.Length == 0)
						{
						Debug.LogWarning("No textures selected. Please select textures to paste slices.");
						return;
						}
					this.PasteSlicesToSelected();
					}
				EditorGUILayout.HelpBox(
					"Paste the copied slice layout onto all selected textures. " +
					"The layout is automatically scaled to fit each texture.", MessageType.None);
				}

			EditorGUILayout.Space();

			GUILayout.Label("Pivot Adjustment", EditorStyles.boldLabel);

			if (GUILayout.Button(new GUIContent(
				"Adjust Pivot Of Selected Slices",
				"Set the pivot alignment for all slices in the selected textures to the value chosen above. " +
				"Warning: This will overwrite any custom pivots.")))
				{
				if (Selection.objects.Length == 0)
					{
					Debug.LogWarning("No textures selected. Please select textures to adjust pivots.");
					return;
					}
				this.AdjustPivotOfSelectedSlices();
				}
			EditorGUILayout.HelpBox(
				"Change the pivot alignment for all slices in the selected textures. " +
				"This will overwrite any custom pivots.", MessageType.Warning);

			EditorGUILayout.Space();

			GUILayout.Label("Batch Slicing", EditorStyles.boldLabel);

			if (GUILayout.Button(new GUIContent(
				"Slice Selected Sprites (Grid)",
				"Slice all selected textures into a grid using the settings above. " +
				"Empty cells (fully transparent) can be ignored if enabled.")))
				{
				if (Selection.objects.Length == 0)
					{
					Debug.LogWarning("No textures selected. Please select textures to slice.");
					return;
					}

				this.SliceSelectedSprites();
				}
			EditorGUILayout.HelpBox(
				"Slice all selected textures into a grid based on the current settings. " +
				"If 'Ignore Empty Rects' is enabled, fully transparent cells will be skipped.",
				MessageType.None);

			// Debug information section - utilizing all fields for transparency
			EditorGUILayout.Space();
			GUILayout.Label("Debug Information", EditorStyles.boldLabel);

			using (new EditorGUI.DisabledScope(true))
				{
				EditorGUILayout.LabelField("Current Settings:", EditorStyles.miniLabel);
				EditorGUILayout.LabelField($"  Mode: {(this.useCellSize ? "Cell Size" : "Grid")}", EditorStyles.miniLabel);
				if (this.useCellSize)
					{
					EditorGUILayout.LabelField($"  Cell Size: {this.cellSize.x}x{this.cellSize.y}", EditorStyles.miniLabel);
					}
				else
					{
					EditorGUILayout.LabelField($"  Grid: {this.columns}x{this.rows}", EditorStyles.miniLabel);
					}
				EditorGUILayout.LabelField($"  Pivot: {this.pivotAlignment}", EditorStyles.miniLabel);
				EditorGUILayout.LabelField($"  Ignore Empty: {this.ignoreEmptyRects}", EditorStyles.miniLabel);

				if (copiedRects != null)
					{
					EditorGUILayout.LabelField($"  Copied Slices: {copiedRects.Count} from {copiedTexWidth}x{copiedTexHeight}", EditorStyles.miniLabel);
					}
				else
					{
					EditorGUILayout.LabelField("  No slices copied", EditorStyles.miniLabel);
					}
				}
			}

		/// <summary>
		/// Copies the sprite rectangles and custom physics outlines from the selected texture(s) in the project.
		/// 🔥 LIBERATED: No more creepy death skull guardians needed!
		/// </summary>
		private void CopySlicesFromSelected ()
			{
			Object [ ] selectedTextures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
			if (selectedTextures.Length == 0)
				{
				Debug.LogWarning("No texture selected to copy slices from.");
				return;
				}

			string path = AssetDatabase.GetAssetPath(selectedTextures [ 0 ]);
			var importer = AssetImporter.GetAtPath(path) as TextureImporter;
			if (importer == null)
				{
				Debug.LogWarning("Texture importer not found.");
				return;
				}

			Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
			if (texture == null)
				{
				Debug.LogWarning("Could not load texture asset.");
				return;
				}

			var factory = new SpriteDataProviderFactories();
			factory.Init();
			ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
			dataProvider.InitSpriteEditorDataProvider();

			var rects = new List<SpriteRect>(dataProvider.GetSpriteRects());
			if (rects.Count == 0)
				{
				Debug.LogWarning("No custom slices found on selected texture.");
				return;
				}

			// Store outlines using GUID as key
			copiedOutlines.Clear();
			ISpritePhysicsOutlineDataProvider outlineProvider = dataProvider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
			foreach (SpriteRect rect in rects)
				{
				List<Vector2 [ ]> outlines = outlineProvider.GetOutlines(rect.spriteID);
				// Deep copy the outlines to avoid reference issues
				copiedOutlines [ rect.spriteID.ToString() ] = outlines != null
					? outlines.Select(arr => arr.ToArray()).ToList()
					: new List<Vector2 [ ]>();
				}

			copiedRects = rects;
			copiedTexWidth = texture.width;
			copiedTexHeight = texture.height;
			Debug.Log($"Copied {rects.Count} sprite rects (and outlines) from '{texture.name}' ({copiedTexWidth}x{copiedTexHeight}).");
			}

		/// <summary>
		/// Pastes the copied sprite rectangles and custom physics outlines to the selected texture(s) in the project.
		/// 🔥 LIBERATED: Freedom from conditional compilation chains!
		/// </summary>
		private void PasteSlicesToSelected ()
			{
			if (copiedRects == null || copiedRects.Count == 0)
				{
				Debug.LogWarning("No copied slices to paste.");
				return;
				}

			Object [ ] selectedTextures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);

			foreach (Object obj in selectedTextures)
				{
				string path = AssetDatabase.GetAssetPath(obj);
				var importer = AssetImporter.GetAtPath(path) as TextureImporter;
				if (importer == null)
					{
					Debug.LogWarning($"Skipping '{obj.name}', texture importer not found.");
					continue;
					}

				Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
				if (texture == null)
					{
					Debug.LogWarning($"Skipping '{obj.name}', could not load texture asset.");
					continue;
					}

				int texWidth = texture.width;
				int texHeight = texture.height;

				var factory = new SpriteDataProviderFactories();
				factory.Init();
				ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
				dataProvider.InitSpriteEditorDataProvider();

				// Scale rects to fit new texture size
				List<SpriteRect> newRects = new();
				float scaleX = (float)texWidth / copiedTexWidth;
				float scaleY = (float)texHeight / copiedTexHeight;

				// Map from old rect GUID to new rect for outline assignment
				Dictionary<string, SpriteRect> guidToNewRect = new();

				foreach (SpriteRect srcRect in copiedRects)
					{
					Rect r = srcRect.rect;
					var scaledRect = new Rect(
						Mathf.RoundToInt(r.x * scaleX),
						Mathf.RoundToInt(r.y * scaleY),
						Mathf.RoundToInt(r.width * scaleX),
						Mathf.RoundToInt(r.height * scaleY)
					);

					if (scaledRect.width <= 0 || scaledRect.height <= 0)
						{
						continue;
						}

					SpriteRect newRect = new()
						{
						name = srcRect.name,
						rect = scaledRect,
						alignment = srcRect.alignment,
						pivot = srcRect.pivot
						};
					newRects.Add(newRect);
					guidToNewRect [ srcRect.spriteID.ToString() ] = newRect;
					}

				// Clear all previous rects before setting new ones
				dataProvider.SetSpriteRects(System.Array.Empty<SpriteRect>());
				dataProvider.Apply();

				dataProvider.SetSpriteRects(newRects.ToArray());
				dataProvider.Apply();

				// Set outlines (physics shapes)
				ISpritePhysicsOutlineDataProvider outlineProvider = dataProvider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
				foreach (SpriteRect srcRect in copiedRects)
					{
					if (!guidToNewRect.TryGetValue(srcRect.spriteID.ToString(), out SpriteRect newRect))
						{
						continue;
						}

					if (copiedOutlines.TryGetValue(srcRect.spriteID.ToString(), out List<Vector2 [ ]> outlines) && outlines != null && outlines.Count > 0)
						{
						Rect srcRectRect = srcRect.rect;
						Rect newRectRect = newRect.rect;
						float outlineScaleX = newRectRect.width / srcRectRect.width;
						float outlineScaleY = newRectRect.height / srcRectRect.height;

						List<Vector2 [ ]> scaledOutlines = new();
						foreach (Vector2 [ ] outline in outlines)
							{
							var scaled = new Vector2 [ outline.Length ];
							for (int i = 0; i < outline.Length; i++)
								{
								scaled [ i ] = new Vector2(
									outline [ i ].x * outlineScaleX,
									outline [ i ].y * outlineScaleY
								);
								}
							scaledOutlines.Add(scaled);
							}
						outlineProvider.SetOutlines(newRect.spriteID, scaledOutlines);
						}
					}

				dataProvider.Apply();
				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

				Debug.Log($"Pasted {newRects.Count} slices (and outlines) to '{texture.name}' ({texWidth}x{texHeight}).");
				}
			}

		/// <summary>
		/// Adjusts the pivot of all selected sprite slices to the specified pivot alignment.
		/// 🔥 LIBERATED: Clean code, no more conditional chaos!
		/// </summary>
		private void AdjustPivotOfSelectedSlices ()
			{
			Object [ ] selectedTextures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);

			foreach (Object obj in selectedTextures)
				{
				string path = AssetDatabase.GetAssetPath(obj);
				var importer = AssetImporter.GetAtPath(path) as TextureImporter;

				if (importer == null)
					{
					Debug.LogWarning($"Skipping '{obj.name}', texture importer not found.");
					continue;
					}

				var factory = new SpriteDataProviderFactories();
				factory.Init();
				ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
				dataProvider.InitSpriteEditorDataProvider();

				var rects = new List<SpriteRect>(dataProvider.GetSpriteRects());

				if (rects.Count == 0)
					{
					Debug.LogWarning($"No slices found on '{obj.name}'.");
					continue;
					}

				// Update alignment and pivot for each rect
				for (int i = 0; i < rects.Count; i++)
					{
					rects [ i ].alignment = this.pivotAlignment;
					rects [ i ].pivot = this.GetPivotForAlignment(this.pivotAlignment);
					}

				dataProvider.SetSpriteRects(rects.ToArray());
				dataProvider.Apply();

				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

				Debug.Log($"Adjusted pivot for {rects.Count} slices on '{obj.name}'.");
				}
			}

		/// <summary>
		/// Slices the selected textures into multiple sprites based on the specified cell size or grid settings.
		/// 🔥 LIBERATED: Pure, clean sprite slicing power!
		/// </summary>
		private void SliceSelectedSprites ()
			{
			Object [ ] selectedTextures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);

			foreach (Object obj in selectedTextures)
				{
				string path = AssetDatabase.GetAssetPath(obj);
				var importer = AssetImporter.GetAtPath(path) as TextureImporter;

				if (importer == null)
					{
					Debug.LogWarning($"Skipping '{obj.name}', texture importer not found.");
					continue;
					}

				Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

				if (texture == null)
					{
					Debug.LogWarning($"Skipping '{obj.name}', could not load texture asset.");
					continue;
					}

				// Validate texture dimensions
				int texWidth = texture.width;
				int texHeight = texture.height;

				int actualColumns, actualRows, spriteWidth, spriteHeight;

				if (this.useCellSize)
					{
					spriteWidth = Mathf.Max(1, Mathf.RoundToInt(this.cellSize.x));
					spriteHeight = Mathf.Max(1, Mathf.RoundToInt(this.cellSize.y));
					actualColumns = Mathf.Max(1, texWidth / spriteWidth);
					actualRows = Mathf.Max(1, texHeight / spriteHeight);
					}
				else
					{
					actualColumns = Mathf.Max(1, this.columns);
					actualRows = Mathf.Max(1, this.rows);
					spriteWidth = texWidth / actualColumns;
					spriteHeight = texHeight / actualRows;
					}

				var factory = new SpriteDataProviderFactories();
				factory.Init();
				ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
				dataProvider.InitSpriteEditorDataProvider();

				List<SpriteRect> spriteRects = new();

				string assetPath = AssetDatabase.GetAssetPath(texture);
				var texImporter = (TextureImporter)AssetImporter.GetAtPath(assetPath);
				bool wasReadable = texImporter.isReadable;
				if (!wasReadable)
					{
					texImporter.isReadable = true;
					AssetDatabase.ImportAsset(assetPath);
					}

				// Iterate through the grid to create sprite rectangles
				for (int y = 0; y < actualRows; y++)
					{
					for (int x = 0; x < actualColumns; x++)
						{
						int rectX = x * spriteWidth;
						int rectY = y * spriteHeight;
						int rectW = spriteWidth;
						int rectH = spriteHeight;

						// Last column/row may be smaller if texture size is not a perfect multiple
						if (x == actualColumns - 1)
							{
							rectW = texWidth - rectX;
							}

						if (y == actualRows - 1)
							{
							rectH = texHeight - rectY;
							}

						// Clamp the rectangle dimensions to ensure they don't exceed texture bounds
						rectW = Mathf.Clamp(rectW, 0, texWidth - rectX);
						rectH = Mathf.Clamp(rectH, 0, texHeight - rectY);

						// Skip empty rectangles
						if (rectW <= 0 || rectH <= 0)
							{
							continue;
							}

						// Flip Y so row 0 is at the top
						int flippedY = texHeight - (rectY + rectH);

						// Create the rectangle for the sprite slice
						Rect cellRect = new(rectX, flippedY, rectW, rectH);

						bool isEmpty = false;

						// Check if the rectangle is empty if ignoreEmptyRects is true
						if (this.ignoreEmptyRects)
							{
							Color [ ] pixels = texture.GetPixels(
								Mathf.RoundToInt(cellRect.x),
								Mathf.RoundToInt(cellRect.y),
								Mathf.RoundToInt(cellRect.width),
								Mathf.RoundToInt(cellRect.height)
							);

							isEmpty = true;

							foreach (Color pixel in pixels)
								{
								if (pixel.a > 0f)
									{
									isEmpty = false;
									break;
									}
								}
							}

						if (this.ignoreEmptyRects && isEmpty)
							{
							continue;
							}

						SpriteRect rect = new()
							{
							name = $"{obj.name}_{x}_{y}",
							rect = cellRect,
							alignment = this.pivotAlignment,
							pivot = this.GetPivotForAlignment(this.pivotAlignment)
							};

						spriteRects.Add(rect);
						}
					}

				dataProvider.SetSpriteRects(spriteRects.ToArray());
				dataProvider.Apply();

				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
				}

			Debug.Log("Batch slicing completed using ISpriteEditorDataProvider!");
			}

		/// <summary>
		/// Gets the pivot vector for the specified sprite alignment.
		/// 🔥 LIBERATED: Pure utility function, no protection needed!
		/// </summary>
		private Vector2 GetPivotForAlignment (SpriteAlignment alignment)
			{
			return alignment switch
				{
					SpriteAlignment.BottomCenter => new Vector2(0.5f, 0f),
					SpriteAlignment.Center => new Vector2(0.5f, 0.5f),
					SpriteAlignment.TopLeft => new Vector2(0f, 1f),
					SpriteAlignment.TopCenter => new Vector2(0.5f, 1f),
					SpriteAlignment.TopRight => new Vector2(1f, 1f),
					SpriteAlignment.LeftCenter => new Vector2(0f, 0.5f),
					SpriteAlignment.RightCenter => new Vector2(1f, 0.5f),
					SpriteAlignment.BottomLeft => new Vector2(0f, 0f),
					SpriteAlignment.BottomRight => new Vector2(1f, 0f),
					SpriteAlignment.Custom => new Vector2(0.5f, 0f),
					_ => new Vector2(0.5f, 0.5f),
					};
			}
		}
	}
#endif
