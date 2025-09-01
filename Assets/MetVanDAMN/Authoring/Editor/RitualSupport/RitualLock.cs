#if UNITY_EDITOR && METVD_FULL_DOTS
// @Intent: Central serialized lock ensuring only one SubScene lifecycle ritual runs at a time.
// @CheekPreservation: Prevents ritual overlap that causes hierarchy ghost entries
using UnityEditor;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring.Editor.RitualSupport
	{
	[InitializeOnLoad]
	internal static class RitualLock
		{
		private static bool _locked;
		private static string _owner;

		public static bool TryAcquire(string owner)
			{
			if (_locked)
				{
				Debug.LogWarning($"🔒 Ritual lock held by '{_owner}', cannot acquire for '{owner}'");
				return false;
				}
			_locked = true;
			_owner = owner;
			Debug.Log($"🔓 Ritual lock acquired by: {owner}");
			return true;
			}

		public static void Release(string owner)
			{
			if (_locked && _owner == owner)
				{
				Debug.Log($"🔓 Ritual lock released by: {owner}");
				_locked = false;
				_owner = null;
				}
			else if (_locked)
				{
				Debug.LogWarning($"⚠️ Cannot release lock held by '{_owner}' (requested by '{owner}')");
				}
			}

		public static bool IsHeld => _locked;
		public static string Owner => _owner;
		}
	}
#endif
