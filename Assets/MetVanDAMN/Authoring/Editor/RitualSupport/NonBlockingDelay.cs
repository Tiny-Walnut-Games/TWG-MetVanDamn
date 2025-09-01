#if UNITY_EDITOR
// @Intent: Utility for phased non-blocking waits instead of Thread.Sleep.
// @CheekPreservation: Eliminates main thread blocking that causes asset import races
using System;
using UnityEditor;

namespace TinyWalnutGames.MetVD.Authoring.Editor.RitualSupport
	{
	internal static class NonBlockingDelay
		{
		public static void AfterFrames(int frameCount, Action action)
			{
			if (frameCount <= 0)
				{
				action();
				return;
				}

			int remaining = frameCount;
			void Tick()
				{
				if (--remaining <= 0)
					{
					EditorApplication.update -= Tick;
					action();
					}
				}
			EditorApplication.update += Tick;
			}

		public static void AfterSeconds(double seconds, Action action)
			{
			if (seconds <= 0)
				{
				action();
				return;
				}

			double start = EditorApplication.timeSinceStartup;
			void Tick()
				{
				if (EditorApplication.timeSinceStartup - start >= seconds)
					{
					EditorApplication.update -= Tick;
					action();
					}
				}
			EditorApplication.update += Tick;
			}

		/// <summary>
		/// Faculty-grade replacement for Thread.Sleep in asset operations
		/// </summary>
		public static void AfterAssetRefresh(Action action, double maxWaitSeconds = 5.0)
			{
			// Wait for AssetDatabase to be idle, up to maxWaitSeconds
			double start = EditorApplication.timeSinceStartup;
			void Tick()
				{
				bool timeout = EditorApplication.timeSinceStartup - start >= maxWaitSeconds;
				bool ready = !EditorApplication.isUpdating && !EditorApplication.isCompiling;

				if (ready || timeout)
					{
					EditorApplication.update -= Tick;
					if (timeout)
						{
						UnityEngine.Debug.LogWarning($"⚠️ Asset refresh timeout after {maxWaitSeconds}s - proceeding anyway");
						}
					action();
					}
				}
			EditorApplication.update += Tick;
			}
		}
	}
#endif
