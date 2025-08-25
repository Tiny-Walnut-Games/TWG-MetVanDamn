#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring.Editor
{
    public class MetVDWorldDebugWindow : EditorWindow
    {
        private MetVDGizmoSettings _settings;
        private Vector2 _scroll;

        [MenuItem("MetVanDAMN/World Debugger", priority = 50)]
        public static void Open() => GetWindow<MetVDWorldDebugWindow>("MetVD World Debug");

        private void OnEnable()
        {
            if (_settings == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:MetVDGizmoSettings");
                if (guids.Length > 0)
                    _settings = AssetDatabase.LoadAssetAtPath<MetVDGizmoSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("MetVanDAMN World Debug", EditorStyles.boldLabel);
            _settings = (MetVDGizmoSettings)EditorGUILayout.ObjectField("Gizmo Settings", _settings, typeof(MetVDGizmoSettings), false);
            if (_settings == null)
            {
                if (GUILayout.Button("Create Gizmo Settings Asset"))
                {
                    _settings = ScriptableObject.CreateInstance<MetVDGizmoSettings>();
                    AssetDatabase.CreateAsset(_settings, "Assets/MetVanDAMN/Authoring/Editor/Gizmos/MetVDGizmoSettings.asset");
                    AssetDatabase.SaveAssets();
                }
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            SerializedObject so = new(_settings);
            so.Update();
            SerializedProperty prop = so.GetIterator();
            bool enter = true;
            while (prop.NextVisible(enter))
            {
                if (prop.name == "m_Script") continue;
                EditorGUILayout.PropertyField(prop, true);
                enter = false;
            }
            so.ApplyModifiedProperties();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            if (GUILayout.Button("Frame All Districts"))
            {
                // With this line:
                DistrictAuthoring[] districts = Object.FindObjectsByType<DistrictAuthoring>(FindObjectsSortMode.None);
                if (districts.Length > 0)
                {
                    Bounds b = new(districts[0].transform.position, Vector3.one);
                    foreach (var d in districts)
                        b.Encapsulate(d.transform.position);
                    SceneView.lastActiveSceneView.Frame(b, false);
                }
            }
        }
    }
}
#endif
