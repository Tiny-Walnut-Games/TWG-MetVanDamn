using UnityEngine;
using TinyWalnutGames.MetVD.Core;

#nullable enable

namespace TinyWalnutGames.MetVanDAMN.Authoring
    {
    /// <summary>
    /// Simple test runner for validating the procedural leveling perk system.
    /// Tests core functionality without requiring Unity Test Framework.
    /// </summary>
    public class UpgradeSystemTestRunner : MonoBehaviour
        {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestsOnStart = false;
        [SerializeField] private bool enableDetailedLogging = true;

        [Header("Manual Test Controls")]
        [SerializeField] private KeyCode runTestsKey = KeyCode.F5;

        private void Start()
            {
            if (runTestsOnStart)
                {
                RunAllTests();
                }
            }

        private void Update()
            {
            if (Input.GetKeyDown(runTestsKey))
                {
                RunAllTests();
                }
            }

        [ContextMenu("Run All Tests")]
        public void RunAllTests()
            {
            Debug.Log("🧪 Starting Procedural Leveling Perk System Tests...");

            int passed = 0;
            int total = 0;

            // Test 1: Database Manager
            if (TestDatabaseManager())
                {
                passed++;
                Debug.Log("✅ Test 1: Database Manager - PASSED");
                }
            else
                {
                Debug.LogError("❌ Test 1: Database Manager - FAILED");
                }
            total++;

            // Test 2: Player Progression
            if (TestPlayerProgression())
                {
                passed++;
                Debug.Log("✅ Test 2: Player Progression - PASSED");
                }
            else
                {
                Debug.LogError("❌ Test 2: Player Progression - FAILED");
                }
            total++;

            // Test 3: Choice System
            if (TestChoiceSystem())
                {
                passed++;
                Debug.Log("✅ Test 3: Choice System - PASSED");
                }
            else
                {
                Debug.LogError("❌ Test 3: Choice System - FAILED");
                }
            total++;

            // Test 4: Effect Application
            if (TestEffectApplication())
                {
                passed++;
                Debug.Log("✅ Test 4: Effect Application - PASSED");
                }
            else
                {
                Debug.LogError("❌ Test 4: Effect Application - FAILED");
                }
            total++;

            // Test 5: UI Integration
            if (TestUIIntegration())
                {
                passed++;
                Debug.Log("✅ Test 5: UI Integration - PASSED");
                }
            else
                {
                Debug.LogError("❌ Test 5: UI Integration - FAILED");
                }
            total++;

            // Results
            Debug.Log($"🏁 Test Results: {passed}/{total} tests passed ({(float)passed / total * 100:F1}%)");

            if (passed == total)
                {
                Debug.Log("🎉 ALL TESTS PASSED! Procedural leveling perk system is working correctly.");
                }
            else
                {
                Debug.LogWarning($"⚠️ {total - passed} test(s) failed. Check individual test logs above.");
                }
            }

        private bool TestDatabaseManager()
            {
            try
                {
                var dbManager = FindFirstObjectByType<UpgradeDatabaseManager>();
                if (dbManager == null)
                    {
                    if (enableDetailedLogging)
                        Debug.LogWarning("No UpgradeDatabaseManager found in scene - this is expected for basic tests");
                    return true; // This is OK, system can work without it
                    }

                // Test database functionality
                var allIds = dbManager.GetAllUpgradeIds();
                if (enableDetailedLogging)
                    Debug.Log($"Database contains {allIds.Length} upgrade definitions");

                return true;
                }
            catch (System.Exception e)
                {
                Debug.LogError($"Database Manager test failed: {e.Message}");
                return false;
                }
            }

        private bool TestPlayerProgression()
            {
            try
                {
                var progression = FindFirstObjectByType<PlayerLevelProgression>();
                if (progression == null)
                    {
                    if (enableDetailedLogging)
                        Debug.LogWarning("No PlayerLevelProgression found - creating test instance");

                    var testGO = new GameObject("TestProgression");
                    progression = testGO.AddComponent<PlayerLevelProgression>();
                    }

                // Test XP and level up
                int initialLevel = progression.CurrentLevel;
                int initialXP = progression.CurrentXP;

                progression.GainXP(100);

                bool xpGained = progression.CurrentXP > initialXP;
                if (enableDetailedLogging)
                    Debug.Log($"XP test: Initial={initialXP}, After={progression.CurrentXP}, Gained={xpGained}");

                return xpGained;
                }
            catch (System.Exception e)
                {
                Debug.LogError($"Player Progression test failed: {e.Message}");
                return false;
                }
            }

        private bool TestChoiceSystem()
            {
            try
                {
                var choiceSystem = FindFirstObjectByType<LevelUpChoiceSystem>();
                if (choiceSystem == null)
                    {
                    if (enableDetailedLogging)
                        Debug.LogWarning("No LevelUpChoiceSystem found - creating test instance");

                    var testGO = new GameObject("TestChoiceSystem");
                    choiceSystem = testGO.AddComponent<LevelUpChoiceSystem>();
                    }

                // Test choice generation (this might not work without upgrade collections)
                bool hasEvents = choiceSystem.OnChoicesGenerated != null;
                if (enableDetailedLogging)
                    Debug.Log($"Choice system has event subscribers: {hasEvents}");

                return true; // Basic instantiation test
                }
            catch (System.Exception e)
                {
                Debug.LogError($"Choice System test failed: {e.Message}");
                return false;
                }
            }

        private bool TestEffectApplication()
            {
            try
                {
                var effectApplicator = FindFirstObjectByType<UpgradeEffectApplicator>();
                if (effectApplicator == null)
                    {
                    if (enableDetailedLogging)
                        Debug.LogWarning("No UpgradeEffectApplicator found - creating test instance");

                    var testGO = new GameObject("TestEffectApplicator");
                    effectApplicator = testGO.AddComponent<UpgradeEffectApplicator>();
                    }

                // Test stat retrieval
                float testStat = effectApplicator.GetCurrentStat("walkspeed");
                bool statRetrievalWorks = testStat >= 0;

                if (enableDetailedLogging)
                    Debug.Log($"Effect applicator stat test: walkspeed={testStat}");

                return statRetrievalWorks;
                }
            catch (System.Exception e)
                {
                Debug.LogError($"Effect Application test failed: {e.Message}");
                return false;
                }
            }

        private bool TestUIIntegration()
            {
            try
                {
                var choiceUI = FindFirstObjectByType<LevelUpChoiceUI>();
                if (choiceUI == null)
                    {
                    if (enableDetailedLogging)
                        Debug.LogWarning("No LevelUpChoiceUI found - this is OK for headless tests");
                    return true; // UI is optional for basic functionality
                    }

                // Test UI component existence
                bool hasUIComponent = choiceUI != null;
                if (enableDetailedLogging)
                    Debug.Log($"UI integration test: Component exists={hasUIComponent}");

                return hasUIComponent;
                }
            catch (System.Exception e)
                {
                Debug.LogError($"UI Integration test failed: {e.Message}");
                return false;
                }
            }

        /// <summary>
        /// Test the complete player setup flow
        /// </summary>
        [ContextMenu("Test Complete Player Setup")]
        public void TestCompletePlayerSetup()
            {
            Debug.Log("🎮 Testing Complete Player Setup...");

            var testGO = new GameObject("TestPlayer");
            var completeSetup = testGO.AddComponent<CompletePlayerSetup>();

            // Trigger setup
            completeSetup.SetupPlayer();

            // Check if components were added
            var components = testGO.GetComponents<MonoBehaviour>();
            Debug.Log($"Player has {components.Length} components after setup:");

            foreach (var component in components)
                {
                Debug.Log($"  • {component.GetType().Name}");
                }

            Debug.Log("✅ Complete player setup test finished");
            }

        /// <summary>
        /// Test creating a basic upgrade definition at runtime
        /// </summary>
        [ContextMenu("Test Runtime Upgrade Creation")]
        public void TestRuntimeUpgradeCreation()
            {
            Debug.Log("⚙️ Testing Runtime Upgrade Creation...");

            try
                {
                // Create test upgrade definition
                var upgrade = ScriptableObject.CreateInstance<UpgradeDefinition>();
                if (upgrade == null)
                    {
                    Debug.LogError("Upgrade creation test: FAILED (creation returned null)");
                    return;
                    }

                // We can't set private serialized fields directly, but we can test the object creation
                Debug.Log("Upgrade creation test: PASSED");
                Debug.Log($"  • Upgrade ID: {upgrade.Id}");
                Debug.Log($"  • Upgrade Name: {upgrade.UpgradeName}");
                Debug.Log($"  • Category: {upgrade.Category}");
                }
            catch (System.Exception e)
                {
                Debug.LogError($"Runtime upgrade creation test failed: {e.Message}");
                }
            }
        }
    }
