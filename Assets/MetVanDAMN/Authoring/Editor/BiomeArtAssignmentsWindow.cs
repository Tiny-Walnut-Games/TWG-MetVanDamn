using System.Collections.Generic;
using System.Linq;
using TinyWalnutGames.MetVD.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring.Editor
    {
    /// <summary>
    /// Shows live counts of BiomeArtProfile assignments per biome type during Play Mode.
    /// Non-intrusive debug window to verify auto-assignment behavior.
    /// </summary>
    public class BiomeArtAssignmentsWindow : EditorWindow
        {
        private Vector2 _scroll;
        private bool _autoRefresh = true;
        private float _refreshInterval = 1.0f;
        private double _lastRefresh;

        private readonly Dictionary<BiomeType, Dictionary<string, int>> _live = new();

        [MenuItem("Tiny Walnut Games/MetVanDAMN/Debug/Biome Art Assignments")]
        public static void ShowWindow()
            {
            var win = GetWindow<BiomeArtAssignmentsWindow>("Biome Art Assignments");
            win.minSize = new Vector2(360, 240);
            win.RefreshNow();
            }

        private void OnEnable()
            {
            _lastRefresh = EditorApplication.timeSinceStartup;
            EditorApplication.update += OnEditorUpdate;
            RefreshNow();
            }

        private void OnDisable()
            {
            EditorApplication.update -= OnEditorUpdate;
            }

        private void OnEditorUpdate()
            {
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefresh > _refreshInterval)
                {
                RefreshNow();
                _lastRefresh = EditorApplication.timeSinceStartup;
                Repaint();
                }
            }

        private void OnGUI()
            {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Width(70)))
                {
                RefreshNow();
                }
            GUILayout.Space(8);
            _autoRefresh = EditorGUILayout.Toggle("Auto Refresh", _autoRefresh, GUILayout.Width(140));
            if (_autoRefresh)
                {
                GUILayout.Label("Interval", GUILayout.Width(50));
                _refreshInterval = EditorGUILayout.Slider(_refreshInterval, 0.5f, 5f, GUILayout.Width(140));
                GUILayout.Label("s", GUILayout.Width(16));
                }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (Application.isPlaying && World.DefaultGameObjectInjectionWorld != null)
                {
                if (_live.Count == 0)
                    {
                    EditorGUILayout.HelpBox("No biome assignments found yet. Ensure the scene contains biome entities and the auto-assignment system has run.", MessageType.Info);
                    }
                else
                    {
                    foreach (var kv in _live.OrderBy(k => k.Key.ToString()))
                        {
                        EditorGUILayout.BeginVertical(GUI.skin.box);
                        EditorGUILayout.LabelField($"{kv.Key}", EditorStyles.boldLabel);
                        int total = kv.Value.Values.Sum();
                        EditorGUILayout.LabelField($"Total: {total}", EditorStyles.miniLabel);

                        foreach (var p in kv.Value.OrderByDescending(p => p.Value))
                            {
                            EditorGUILayout.LabelField($"- {p.Key}", GUILayout.Width(220));
                            EditorGUILayout.LabelField($"{p.Value}", GUILayout.Width(60));
                            }
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(4);
                        }
                    }
                }
            else
                {
                EditorGUILayout.HelpBox("Enter Play Mode to view live biome art assignments.", MessageType.None);
                DrawLibrarySummary();
                }

            EditorGUILayout.EndScrollView();
            }

        private void RefreshNow()
            {
            _live.Clear();

            if (!Application.isPlaying || World.DefaultGameObjectInjectionWorld == null)
                return;

            var world = World.DefaultGameObjectInjectionWorld;
            var em = world.EntityManager;

            try
                {
                using var query = em.CreateEntityQuery(
                    ComponentType.ReadOnly<Core.Biome>(),
                    ComponentType.ReadOnly<BiomeArtProfileReference>());

                using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
                using NativeArray<Core.Biome> biomes = query.ToComponentDataArray<Core.Biome>(Allocator.Temp);
                using NativeArray<BiomeArtProfileReference> refs = query.ToComponentDataArray<BiomeArtProfileReference>(Allocator.Temp);

                for (int i = 0; i < entities.Length; i++)
                    {
                    var b = biomes[i];
                    var r = refs[i];
                    string profileName = r.ProfileRef.IsValid() && r.ProfileRef.Value != null ? r.ProfileRef.Value.name : "<none>";

                    if (!_live.TryGetValue(b.Type, out var map))
                        {
                        map = new Dictionary<string, int>();
                        _live[b.Type] = map;
                        }

                    map.TryGetValue(profileName, out int count);
                    map[profileName] = count + 1;
                    }
                }
            catch (System.Exception ex)
                {
                Debug.LogWarning($"BiomeArtAssignmentsWindow: query failed - {ex.Message}");
                }
            }

        private void DrawLibrarySummary()
            {
            var libAuth = Object.FindFirstObjectByType<BiomeArtProfileLibraryAuthoring>(FindObjectsInactive.Include);
            if (libAuth == null || libAuth.library == null)
                {
                EditorGUILayout.HelpBox("No BiomeArtProfileLibrary found in the scene.", MessageType.Warning);
                return;
                }

            var lib = libAuth.library;

            EditorGUILayout.LabelField("Library Summary", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            if (lib.perTypeBuckets != null && lib.perTypeBuckets.Length > 0)
                {
                foreach (var bucket in lib.perTypeBuckets)
                    {
                    if (bucket == null) continue;
                    int count = bucket.profiles != null ? bucket.profiles.Count(p => p != null) : 0;
                    EditorGUILayout.LabelField($"{bucket.type}: {count} per-type profiles");
                    }
                }

            int globalCount = lib.profiles != null ? lib.profiles.Count(p => p != null) : 0;
            EditorGUILayout.LabelField($"Global profiles: {globalCount}");
            }
        }
    }
