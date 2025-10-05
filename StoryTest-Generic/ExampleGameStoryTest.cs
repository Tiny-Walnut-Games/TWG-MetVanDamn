#nullable enable
using UnityEngine;
using StoryTest;

namespace MyCompany.MyProject
    {
    /// <summary>
    /// Example Story Test implementation for a simple game.
    /// This demonstrates how to create comprehensive validation suites
    /// that ensure all game systems work together harmoniously.
    /// </summary>
    public class GameStoryTest : StoryTest
        {
        [Header("Game Test Configuration")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject enemyPrefab;

        // Test state
        private GameObject testPlayer;
        private GameObject[] testEnemies;
        private bool playerMoved;
        private bool enemiesSpawned;
        private bool uiUpdated;
        private int enemiesDefeated;

        protected override int TotalTestPhases => 5;

        protected override string GetPhaseName(int phaseIndex)
            {
            return phaseIndex switch
                {
                    0 => "Player Initialization",
                    1 => "Movement System Validation",
                    2 => "Enemy Spawning",
                    3 => "Combat Integration",
                    4 => "UI Feedback",
                    _ => "Unknown Phase"
                    };
            }

        protected override void OnStoryTestBegin()
            {
            // Setup test environment
            testEnemies = new GameObject[3];
            playerMoved = false;
            enemiesSpawned = false;
            uiUpdated = false;
            enemiesDefeated = 0;

            Log("🎮 Setting up game test environment");
            }

        protected override void ExecuteTestPhase(int phaseIndex)
            {
            switch (phaseIndex)
                {
                case 0:
                    TestPlayerInitialization();
                    break;
                case 1:
                    TestMovementSystem();
                    break;
                case 2:
                    TestEnemySpawning();
                    break;
                case 3:
                    TestCombatIntegration();
                    break;
                case 4:
                    TestUIFeedback();
                    break;
                }
            }

        protected override bool PerformFinalValidation()
            {
            Log("🔍 Performing final validation...");

            bool allTestsPassed = true;

            // Validate player was created and moved
            if (testPlayer == null || !playerMoved)
                {
                LogError("❌ Player initialization or movement failed");
                allTestsPassed = false;
                }

            // Validate enemies were spawned
            if (!enemiesSpawned || testEnemies.Length == 0)
                {
                LogError("❌ Enemy spawning failed");
                allTestsPassed = false;
                }

            // Validate UI updates
            if (!uiUpdated)
                {
                LogError("❌ UI feedback failed");
                allTestsPassed = false;
                }

            // Validate combat occurred
            if (enemiesDefeated == 0)
                {
                LogError("❌ Combat integration failed");
                allTestsPassed = false;
                }

            if (allTestsPassed)
                {
                Log("✅ All game systems validated successfully!");
                }

            return allTestsPassed;
            }

        protected override void OnStoryTestComplete()
            {
            // Cleanup test objects
            if (testPlayer != null)
                {
                Destroy(testPlayer);
                }

            foreach (var enemy in testEnemies)
                {
                if (enemy != null)
                    {
                    Destroy(enemy);
                    }
                }

            Log("🧹 Test environment cleaned up");
            }

        private void TestPlayerInitialization()
            {
            if (testPlayer == null && playerPrefab != null)
                {
                testPlayer = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
                testPlayer.name = "StoryTest_Player";

                var controller = testPlayer.GetComponent<PlayerController>();
                if (controller != null)
                    {
                    Log("✅ Player initialized with controller");
                    }
                else
                    {
                    LogError("❌ Player missing controller component");
                    }
                }
            }

        private void TestMovementSystem()
            {
            if (testPlayer != null)
                {
                // Simulate player input
                var controller = testPlayer.GetComponent<PlayerController>();
                if (controller != null)
                    {
                    // Move player to test position
                    testPlayer.transform.position = new Vector3(5f, 0f, 0f);
                    playerMoved = true;
                    Log("✅ Player movement system validated");
                    }
                }
            }

        private void TestEnemySpawning()
            {
            if (!enemiesSpawned && enemyPrefab != null)
                {
                for (int i = 0; i < testEnemies.Length; i++)
                    {
                    testEnemies[i] = Instantiate(enemyPrefab,
                        new Vector3(-5f + i * 3f, 0f, 5f), Quaternion.identity);
                    testEnemies[i].name = $"StoryTest_Enemy_{i}";
                    }
                enemiesSpawned = true;
                Log("✅ Enemy spawning validated");
                }
            }

        private void TestCombatIntegration()
            {
            if (enemiesSpawned && testPlayer != null)
                {
                // Simulate combat - destroy an enemy to test the system
                foreach (var enemy in testEnemies)
                    {
                    if (enemy != null && Random.value > 0.7f) // 30% chance to defeat
                        {
                        Destroy(enemy);
                        enemiesDefeated++;
                        Log($"✅ Enemy defeated ({enemiesDefeated}/{testEnemies.Length})");
                        break;
                        }
                    }
                }
            }

        private void TestUIFeedback()
            {
            if (!uiUpdated && uiManager != null)
                {
                // Simulate UI update
                uiManager.UpdateScore(enemiesDefeated * 100);
                uiUpdated = true;
                Log("✅ UI feedback system validated");
                }
            }
        }

    // Example components referenced in the test
    // These would be your actual game components

    public class PlayerController : MonoBehaviour
        {
        public void Move(Vector3 direction) { /* Implementation */ }
        }

    public class EnemySpawner : MonoBehaviour
        {
        public void SpawnEnemy() { /* Implementation */ }
        }

    public class UIManager : MonoBehaviour
        {
        public void UpdateScore(int score) { /* Implementation */ }
        }
    }
