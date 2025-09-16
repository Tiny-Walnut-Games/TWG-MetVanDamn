#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring.Editor
	{
	[InitializeOnLoad]
	public static class MetVDGizmoDrawer
		{
		private static readonly MetVDGizmoSettings _settings;
		private const string SettingsAssetName = "MetVDGizmoSettings";

		static MetVDGizmoDrawer()
			{
			// Auto-load settings (first asset found)
			string [ ] guids = AssetDatabase.FindAssets("t:MetVDGizmoSettings");
			if (guids.Length > 0)
				{
				_settings = AssetDatabase.LoadAssetAtPath<MetVDGizmoSettings>(AssetDatabase.GUIDToAssetPath(guids [ 0 ]));
				}
			// SceneView.duringSceneGui += OnSceneGUI;
			}

		public static void SnapAllDistrictsToGrid()
			{
			DistrictAuthoring [ ] districts = Object.FindObjectsByType<DistrictAuthoring>(FindObjectsSortMode.None);
			Undo.RecordObjects(districts, "Snap Districts To Grid");
			foreach (DistrictAuthoring d in districts)
				{
				if (!_settings.useGridCoordinatesForGizmos)
					{
					continue; // only meaningful when grid-based
					}

				Vector3 target = GridPositionFromAuthoring(d);
				d.transform.position = target;
				if (_settings.adaptDistrictSizeToCell)
					{
					// scale visual size in settings only (global) -> keep consistent
					_settings.districtSize = new Vector2(_settings.gridCellSize, _settings.gridCellSize);
					}
				}
			EditorUtility.SetDirty(_settings);
			}

		private static Vector3 GridPositionFromAuthoring(DistrictAuthoring district)
			{
			if (_settings == null || !_settings.useGridCoordinatesForGizmos)
				{
				return district.transform.position;
				}

			Unity.Mathematics.int2 coord = district.gridCoordinates; // int2 authoring field
			return new Vector3(coord.x * _settings.gridCellSize, district.transform.position.y, coord.y * _settings.gridCellSize) + _settings.gridOriginOffset;
			}

		[DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
		private static void DrawDistrictGizmo(DistrictAuthoring district, GizmoType type)
			{
			if (_settings == null)
				{
				return;
				}

			if (!ShouldDraw())
				{
				return;
				}

			Vector3 pos = GridPositionFromAuthoring(district);

			// 🔧 DEBUG: Log what we're about to draw
			Debug.Log($"[MetVDGizmoDrawer] Drawing district {district.nodeId} at pos={pos}, magnitude={pos.magnitude}");

			// 🔧 FIX: Prevent massive rectangles from invalid coordinates
			if (pos.magnitude > 10000f || float.IsNaN(pos.x) || float.IsNaN(pos.y) || float.IsNaN(pos.z))
				{
				Debug.LogWarning($"[MetVDGizmoDrawer] SKIPPING district {district.nodeId} - extreme position: {pos}");
				return; // Don't draw - likely invalid coordinates
				}

			Vector2 size = _settings.useGridCoordinatesForGizmos && _settings.adaptDistrictSizeToCell
				? new Vector2(_settings.gridCellSize, _settings.gridCellSize)
				: _settings.districtSize;

			// 🔧 FIX: Clamp size to reasonable bounds and validate
			size.x = Mathf.Clamp(size.x, 0.1f, 100f);
			size.y = Mathf.Clamp(size.y, 0.1f, 100f);

			if (float.IsNaN(size.x) || float.IsNaN(size.y))
				{
				return; // Don't draw if size is invalid
				}

			Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
			Color prev = Handles.color;
			Handles.color = _settings.districtColor;

			// 🔧 FIX: Calculate rectangle corners safely
			Vector3 halfX = new Vector3(size.x * 0.5f, 0, 0);
			Vector3 halfZ = new Vector3(0, 0, size.y * 0.5f);
			Vector3 [ ] corners = {
				pos - halfX - halfZ,
				pos - halfX + halfZ,
				pos + halfX + halfZ,
				pos + halfX - halfZ
			};

			// 🔧 FIX: Validate all corners before drawing
			foreach (Vector3 corner in corners)
				{
				if (corner.magnitude > 10000f || float.IsNaN(corner.x) || float.IsNaN(corner.y) || float.IsNaN(corner.z))
					{
					Handles.color = prev; // Restore color
					return; // Don't draw if any corner is extreme
					}
				}

			Handles.DrawSolidRectangleWithOutline(corners, _settings.districtColor, _settings.districtOutline);

			// Label
			var style = new GUIStyle(EditorStyles.boldLabel)
				{
				normal = { textColor = _settings.labelColor },
				fontSize = _settings.labelFontSize,
				alignment = TextAnchor.UpperCenter
				};
			Handles.Label(pos + Vector3.up * 0.1f, $"#{district.nodeId}\nL{district.level}", style);

			Handles.color = prev;
			}

		[DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
		private static void DrawConnectionGizmo(ConnectionAuthoring connection, GizmoType type)
			{
			if (_settings == null)
				{
				return;
				}

			if (!ShouldDraw())
				{
				return;
				}

			if (connection.from == null || connection.to == null)
				{
				return;
				}

			if (connection.from == connection.to)
				{
				return;
				}

			Vector3 a = GridPositionFromAuthoring(connection.from);
			Vector3 b = GridPositionFromAuthoring(connection.to);
			Color lineColor = connection.type == Core.ConnectionType.Bidirectional ? _settings.connectionColor : _settings.oneWayColor;
			Handles.color = lineColor;
			Handles.DrawAAPolyLine(_settings.connectionWidth, a, b);

			// Arrow (direction a->b or both)
			DrawArrow(a, b, lineColor);
			if (connection.type == Core.ConnectionType.Bidirectional)
				{
				DrawArrow(b, a, lineColor);
				}
			}

		[DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
		private static void DrawBiomeFieldGizmo(BiomeFieldAuthoring field, GizmoType type)
			{
			if (_settings == null)
				{
				return;
				}

			if (!ShouldDraw())
				{
				return;
				}

			Handles.color = _settings.biomePrimary;
			Handles.DrawSolidDisc(field.transform.position, Vector3.up, _settings.biomeRadius * field.strength);
			if (field.secondaryBiome != Core.BiomeType.Unknown)
				{
				Handles.color = _settings.biomeSecondary;
				Handles.DrawWireDisc(field.transform.position, Vector3.up, _settings.biomeRadius * (field.gradient + 0.1f));
				}
			}

		private static void DrawArrow(Vector3 from, Vector3 to, Color c)
			{
			Vector3 dir = (to - from);
			float len = dir.magnitude;
			if (len < 0.01f)
				{
				return;
				}

			dir /= len;
			var basePos = Vector3.Lerp(from, to, 0.5f);
			Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
			float size = _settings.connectionArrowSize;
			Handles.color = c;
			Handles.DrawAAConvexPolygon(
				basePos,
				basePos - dir * size + right * (size * 0.5f),
				basePos - dir * size - right * (size * 0.5f)
			);
			}

		private static bool ShouldDraw()
			{
			bool playing = Application.isPlaying;
			return (playing && _settings.drawInPlayMode) || (!playing && _settings.drawInEditMode);
			}
		}
	}
#endif
