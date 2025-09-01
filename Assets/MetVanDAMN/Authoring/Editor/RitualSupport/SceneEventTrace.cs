#if UNITY_EDITOR && METVD_FULL_DOTS
// @Intent: Event-based trace for sceneOpened / sceneClosed used during toggling ritual.
// @CheekPreservation: Precise tracking instead of polling to prevent state confusion
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TinyWalnutGames.MetVD.Authoring.Editor.RitualSupport
	{
	[InitializeOnLoad]
	internal static class SceneEventTrace
		{
		static SceneEventTrace()
			{
			EditorSceneManager.sceneOpened += OnOpened;
			EditorSceneManager.sceneClosed += OnClosed;
			Debug.Log("🎬 SceneEventTrace initialized - monitoring scene lifecycle events");
			}

		private static readonly List<string> _openedRecent = new();
		private static readonly List<string> _closedRecent = new();

		private static void OnOpened(Scene s, OpenSceneMode m)
			{
			if (!_openedRecent.Contains(s.path))
				{
				_openedRecent.Add(s.path);
				Debug.Log($"📂 Scene opened: {s.name} ({s.path}) - Mode: {m}");
				}
			}

		private static void OnClosed(Scene s)
			{
			if (!_closedRecent.Contains(s.path))
				{
				_closedRecent.Add(s.path);
				Debug.Log($"📁 Scene closed: {s.name} ({s.path})");
				}
			}

		public static void Reset()
			{
			Debug.Log("🔄 SceneEventTrace reset - clearing recent activity");
			_openedRecent.Clear();
			_closedRecent.Clear();
			}

		public static IReadOnlyList<string> RecentlyOpened => _openedRecent;
		public static IReadOnlyList<string> RecentlyClosed => _closedRecent;

		public static string GetSummary()
			{
			return $"Recent Activity - Opened: {_openedRecent.Count}, Closed: {_closedRecent.Count}";
			}
		}
	}
#endif
