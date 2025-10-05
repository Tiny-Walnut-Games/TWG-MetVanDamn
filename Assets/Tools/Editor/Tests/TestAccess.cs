using System;
using TinyWalnutGames.Tools.Editor;

namespace TinyWalnutGames.Tools.Editor.Tests
	{
	public static class TestAccess
		{
		public static void TestCollectorAccess()
			{
			var methods = typeof(TestResultsCollector).GetMethods();
			UnityEngine.Debug.Log($"Found {methods.Length} methods in TestResultsCollector");
			}
		}
	}
