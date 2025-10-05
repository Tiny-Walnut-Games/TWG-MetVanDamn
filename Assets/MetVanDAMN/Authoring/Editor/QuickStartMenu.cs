#if UNITY_EDITOR
using UnityEditor;
using
	TinyWalnutGames.MetVD.Authoring.Editor; // MetVanDAMNSceneBootstrap, MetVDWorldDebugWindow, ProceduralLayoutPreview
using AuthoringSampleCreator = TinyWalnutGames.MetVD.Authoring.Editor.MetVanDAMNAuthoringSampleCreator;
using PrefabCreator = TinyWalnutGames.MetVD.Authoring.Editor.MetVanDAMNSamplePrefabCreator;

namespace TinyWalnutGames.MetVanDAMN.Authoring.Editor
	{
	/// <summary>
	/// Quick access menu for the most common MetVanDAMN actions.
	/// Uses minimal emoji markers for fast visual scanning.
	/// </summary>
	public static class QuickStartMenu
		{
		// Complete Demo Scenes - Now with full gameplay mechanics, player controls, combat, AI, inventory, and proper scene setup!

		// Complete Demo Scenes - Now with full gameplay mechanics!
		[MenuItem("Tiny Walnut Games/MetVanDAMN!/Quick Start/🎮 Complete 2D Platformer Demo", priority = 0)]
		public static void CreateReady2D() => ReadyDemoSceneMenu.Create2DPlatformerScene();

		[MenuItem("Tiny Walnut Games/MetVanDAMN!/Quick Start/🧭 Complete Top-Down Demo", priority = 1)]
		public static void CreateReadyTopDown() => ReadyDemoSceneMenu.CreateTopDownScene();

		[MenuItem("Tiny Walnut Games/MetVanDAMN!/Quick Start/🧱 Complete 3D Demo", priority = 2)]
		public static void CreateReady3D() => ReadyDemoSceneMenu.Create3DScene();

		// Sample scenes
		[MenuItem("Tiny Walnut Games/MetVanDAMN!/Quick Start/🧩 Authoring Sample Scene", priority = 3)]
		public static void CreateAuthoringSample() => AuthoringSampleCreator.CreateAuthoringSampleScene();

		[MenuItem("Tiny Walnut Games/MetVanDAMN!/Quick Start/📦 Legacy Sample Scene (ECS Registry)", priority = 4)]
		public static void CreateLegacySample() => CreateSampleSceneMenu.CreateSampleScene();

		[MenuItem("Tiny Walnut Games/MetVanDAMN!/Quick Start/🧪 Create All Sample Prefabs", priority = 5)]
		public static void CreateAllSamplePrefabs() => PrefabCreator.CreateAllSamplePrefabs();

		// Baseline + Tools
		[MenuItem("Tiny Walnut Games/MetVanDAMN!/Quick Start/🧰 Create Baseline Scene", priority = 6)]
		public static void CreateBaseline() => MetVanDAMNSceneBootstrap.CreateBaseline();
		}
	}
#endif
