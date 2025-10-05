using UnityEditor;
using UnityEngine;

namespace TinyWalnutGames.MetVanDAMN.Authoring.Editor
	{
	/// <summary>
	/// Rich custom inspector for MetVanDAMNMapGenerator delivering an engaging editor experience:
	///  - Live status badges (detailed / minimap readiness)
	///  - Generation controls (generate, regenerate, export)
	///  - Exploration progress bars (districts & rooms)
	///  - Inline texture previews (scaled) without selecting texture assets
	///  - Debug / developer utilities cluster
	/// </summary>
	[CustomEditor(typeof(MetVanDAMNMapGenerator))]
	public class MetVanDAMNMapGeneratorEditor : UnityEditor.Editor
		{
		private GUIStyle _badgeStyle;
		private Texture2D _darkBg;
		private MetVanDAMNMapGenerator _generator;
		private Vector2 _previewScroll;

		private void OnEnable()
			{
			_generator = (MetVanDAMNMapGenerator)target;
			_badgeStyle = new GUIStyle(EditorStyles.boldLabel)
				{
				alignment = TextAnchor.MiddleCenter,
				fontSize = 11
				};
			_darkBg = MakeTex(4, 4, new Color(0.12f, 0.12f, 0.14f, 1f));
			}

		public override void OnInspectorGUI()
			{
			serializedObject.Update();

			DrawHeader();
			EditorGUILayout.Space(4f);
			DrawGenerationControls();
			EditorGUILayout.Space(6f);
			DrawStatusSection();
			EditorGUILayout.Space(6f);
			DrawExplorationSection();
			EditorGUILayout.Space(6f);
			DrawPreviewSection();
			EditorGUILayout.Space(8f);
			DrawDebugUtilities();

			EditorGUILayout.Space();
			EditorGUILayout.HelpBox("MetVanDAMN Map Generator – Deterministic. Null-free. Battle-tested.",
				MessageType.Info);

			serializedObject.ApplyModifiedProperties();
			}

		private new void DrawHeader()
			{
			using (new EditorGUILayout.VerticalScope(GUI.skin.box))
				{
				GUILayout.Label("🗺️ MetVanDAMN Map Generator", EditorStyles.largeLabel);
				EditorGUILayout.LabelField("World visualization + exploration telemetry", EditorStyles.miniLabel);
				}
			}

		private void DrawGenerationControls()
			{
			using (new EditorGUILayout.VerticalScope(GUI.skin.box))
				{
				GUILayout.Label("Generation Controls", EditorStyles.boldLabel);
				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button(_generator.DetailedMapReady ? "Regenerate World Map" : "Generate World Map",
					    GUILayout.Height(28)))
					{
					_generator.RegenerateMap();
					}

				using (new EditorGUI.DisabledScope(!_generator.DetailedMapReady))
					{
					if (GUILayout.Button("Export PNG", GUILayout.Width(100), GUILayout.Height(28)))
						{
						_generator.EditorExportWorldMapPNG();
						}
					}

				EditorGUILayout.EndHorizontal();
				}
			}

		private void DrawStatusSection()
			{
			using (new EditorGUILayout.VerticalScope(GUI.skin.box))
				{
				GUILayout.Label("Status", EditorStyles.boldLabel);
				EditorGUILayout.BeginHorizontal();
				DrawBadge(_generator.DetailedMapReady ? "Detailed Map Ready" : "Detailed Map Pending",
					_generator.DetailedMapReady ? new Color(0.2f, 0.6f, 0.2f) : new Color(0.5f, 0.5f, 0.2f));
				DrawBadge(_generator.MinimapReady ? "Minimap Ready" : "Minimap Pending",
					_generator.MinimapReady ? new Color(0.2f, 0.6f, 0.2f) : new Color(0.5f, 0.5f, 0.2f));
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space(4f);
				EditorGUILayout.LabelField("Seed", _generator.Seed.ToString());
				EditorGUILayout.LabelField("Districts", _generator.DistrictCount.ToString());
				EditorGUILayout.LabelField("Rooms", _generator.RoomCount.ToString());
				}
			}

		private void DrawExplorationSection()
			{
			using (new EditorGUILayout.VerticalScope(GUI.skin.box))
				{
				GUILayout.Label("Exploration", EditorStyles.boldLabel);
				float districtPct = _generator.GetExplorationPercent();
				float roomPct = _generator.GetRoomExplorationPercent();
				DrawProgressBar(districtPct / 100f, $"Districts: {districtPct:0.0}%");
				DrawProgressBar(roomPct / 100f, $"Rooms: {roomPct:0.0}%");
				}
			}

		private void DrawPreviewSection()
			{
			using (new EditorGUILayout.VerticalScope(GUI.skin.box))
				{
				GUILayout.Label("Previews", EditorStyles.boldLabel);
				_previewScroll = EditorGUILayout.BeginScrollView(_previewScroll, GUILayout.Height(260));
				EditorGUILayout.BeginHorizontal();
				DrawTexturePreview(_generator.WorldMapTexture, "Detailed Map");
				DrawTexturePreview(_generator.MinimapTexture, "Minimap");
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndScrollView();
				}
			}

		private void DrawDebugUtilities()
			{
			using (new EditorGUILayout.VerticalScope(GUI.skin.box))
				{
				GUILayout.Label("Utilities", EditorStyles.boldLabel);
				if (GUILayout.Button("Toggle Detailed Map (Play Mode)"))
					{
					_generator.ToggleDetailedMap();
					}

				if (GUILayout.Button("Toggle Minimap (Play Mode)"))
					{
					_generator.ToggleMinimap();
					}
				}
			}

		private void DrawBadge(string text, Color color)
			{
			Color prev = GUI.color;
			GUI.color = color;
			using (new EditorGUILayout.VerticalScope(GUI.skin.box))
				{
				GUILayout.Label(text, _badgeStyle);
				}

			GUI.color = prev;
			}

		private void DrawProgressBar(float value, string label)
			{
			Rect rect = GUILayoutUtility.GetRect(18, 20, "TextField");
			EditorGUI.ProgressBar(rect, value, label);
			}

		private void DrawTexturePreview(Texture2D tex, string label)
			{
			using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.Width(240)))
				{
				GUILayout.Label(label, EditorStyles.miniBoldLabel);
				if (tex != null)
					{
					float maxSide = 200f;
					float aspect = tex.width / (float)tex.height;
					float w = aspect >= 1f ? maxSide : maxSide * aspect;
					float h = aspect >= 1f ? maxSide / aspect : maxSide;
					Rect r = GUILayoutUtility.GetRect(w, h, GUILayout.ExpandWidth(false));
					if (Event.current.type == EventType.Repaint)
						{
						GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, false);
						}
					}
				else
					{
					GUILayout.Label("(no texture)", EditorStyles.centeredGreyMiniLabel, GUILayout.Height(40));
					}
				}
			}

		private Texture2D MakeTex(int width, int height, Color col)
			{
			var pix = new Color[width * height];
			for (int i = 0; i < pix.Length; i++) pix[i] = col;
			var result = new Texture2D(width, height);
			result.SetPixels(pix);
			result.Apply();
			return result;
			}
		}
	}
