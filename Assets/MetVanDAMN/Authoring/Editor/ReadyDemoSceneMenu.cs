using TinyWalnutGames.MetVanDAMN.Authoring;

namespace TinyWalnutGames.MetVanDAMN.Authoring.Editor
	{
	/// <summary>
	/// Customer-facing creators for fully ready demo scenes. Each scene includes:
	/// - Complete gameplay mechanics with player character, combat, AI, inventory
	/// - WorldAuthoring and BiomeArtProfileLibraryAuthoring with real demo content
	/// - ECS Prefab Registry with representative keys for quick hookup
	/// - Camera systems, lighting, and environment setup
	/// - Full MetVanDAMN demo experience instead of smoke test primitives
	/// </summary>
	public static class ReadyDemoSceneMenu
		{
		// Menu items moved under Quick Start; keep public methods for reuse
		public static void Create2DPlatformerScene() => CompleteDemoSceneGenerator.CreateComplete2DPlatformerDemo();

		public static void CreateTopDownScene() => CompleteDemoSceneGenerator.CreateCompleteTopDownDemo();

		public static void Create3DScene() => CompleteDemoSceneGenerator.CreateComplete3DDemo();

		// Legacy smoke test scene creation methods removed.
		// Now delegates to CompleteDemoSceneGenerator for real gameplay demo scenes
		// instead of primitive shape visualizations.
		}
	}
