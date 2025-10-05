// Test script to check if TestResultsCollector is accessible

using UnityEditor;
using TinyWalnutGames.Tools.Editor;

public class TestAccess
	{
	[MenuItem("Tiny Walnut Games/Test/Test Collector Access")]
	private static void TestCollectorAccess()
		{
		var methods = typeof(TestResultsCollector).GetMethods();
		UnityEngine.Debug.Log($"TestResultsCollector has {methods.Length} methods");
		}
	}
