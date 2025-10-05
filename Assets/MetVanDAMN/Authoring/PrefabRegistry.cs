#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring
	{
	[CreateAssetMenu(menuName = "MetVanDAMN/Prefab Registry", fileName = "PrefabRegistry")]
	public class PrefabRegistry : ScriptableObject
		{
		[Tooltip("Mappings from action keys (e.g., 'spawn_boss') to prefabs.")]
		public List<Entry> Entries = new();

		private readonly Dictionary<string, GameObject> _cache = new(StringComparer.Ordinal);

		private void OnEnable()
			{
			RebuildCache();
			}

		public void RebuildCache()
			{
			_cache.Clear();
			if (Entries == null) return;
			foreach (Entry e in Entries)
				{
				if (string.IsNullOrWhiteSpace(e.Key) || e.Prefab == null) continue;
				if (!_cache.ContainsKey(e.Key))
					_cache.Add(e.Key, e.Prefab);
				}
			}

		public bool TryGet(string key, out GameObject? prefab)
			{
			if (string.IsNullOrEmpty(key))
				{
				prefab = null;
				return false;
				}

			// In Editor, cache can be stale when entries change without domain reload
			if (!_cache.TryGetValue(key, out prefab))
				{
				// Try slow path scan
				if (Entries != null)
					{
					foreach (Entry e in Entries)
						{
						if (e.Prefab != null && string.Equals(e.Key, key, StringComparison.Ordinal))
							{
							prefab = e.Prefab;
							_cache[key] = prefab;
							return true;
							}
						}
					}

				return false;
				}

			return prefab != null;
			}

		[Serializable]
		public struct Entry
			{
			public string Key;
			public GameObject? Prefab; // allow null in authoring lists
			}
		}
	}
