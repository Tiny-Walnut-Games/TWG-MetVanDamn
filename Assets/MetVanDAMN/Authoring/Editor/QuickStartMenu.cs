#if UNITY_EDITOR
using UnityEditor;
using TinyWalnutGames.MetVD.Authoring.Editor; // MetVanDAMNSceneBootstrap, MetVDWorldDebugWindow, ProceduralLayoutPreview
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
        // @Copilot these "Ready Demo Scenes" are 100% not ready for customer use. They are incomplete. They are cheep-skate placeholders that make me angry. I asked for fully functioning demo scenes and got this garbage!
        // The demo scenes I require would be 100% functional, fully set up with 100% configured subscenes with appropriate game objects and authoring required for that subscene. It requires player controls, fully functioning cinemachine room zones, masking that hide everything but the current room, all required Cinemachine cameras, lighting, post-processing, physics, and sample art assets to demonstrate MetVanDAMN capabilities in each style (2D platformer, top-down, 3D). They would be polished and ready for users to explore and modify as starting points for their own projects. 100% true "click menu, click play, profit." Unity's 2D scenes are all set with a specific scene view but everything in this kit creates a standard 3D scene view. This is lazy and unacceptable. Fix it. All 2D scenes should default to a 2D scene view with the 2D camera and standard 2D tilemap layout. I personally do not like 2D top-own games being created in a 3D scene.Nothing 2D-scene related should be on the default 3D plane.
        // All 3D scenes should default to a 3D scene view. This is basic Unity knowledge and should be standard practice.

        // Ready Demo Scenes? Lol!
        [MenuItem("Tiny Walnut Games/MetVanDAMN!/Quick Start/🎮 Ready 2D Platformer Scene", priority = 0)]
        public static void CreateReady2D() => ReadyDemoSceneMenu.Create2DPlatformerScene();

        [MenuItem("Tiny Walnut Games/MetVanDAMN!/Quick Start/🧭 Ready Top-Down Scene", priority = 1)]
        public static void CreateReadyTopDown() => ReadyDemoSceneMenu.CreateTopDownScene();

        [MenuItem("Tiny Walnut Games/MetVanDAMN!/Quick Start/🧱 Ready 3D Scene", priority = 2)]
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
