#nullable enable
using UnityEngine;

namespace StoryTest
    {
    /// <summary>
    /// Base class for Story Tests - comprehensive validation suites that ensure
    /// all components work together harmoniously to deliver a complete experience.
    ///
    /// Story Tests are like stage plays where every actor knows their lines,
    /// hits their marks, and delivers a seamless performance. They validate that
    /// the mental model matches the actual implementation.
    ///
    /// Inherit from this class and override the test phases to create your own
    /// comprehensive validation suite.
    /// </summary>
    public abstract class StoryTest : MonoBehaviour
        {
        [Header("Story Test Configuration")]
        [SerializeField] private bool autoRunStoryTest = true;
        [SerializeField] private float testDuration = 30f;
        [SerializeField] private bool enableDebugLogging = true;

        // Story test state
        private float storyTime;
        private int currentTestPhase;
        private bool storyTestComplete;
        private bool storyTestPassed = true;

        /// <summary>
        /// Override this to define how many test phases your story has.
        /// </summary>
        protected abstract int TotalTestPhases { get; }

        /// <summary>
        /// Override this to provide a descriptive name for each test phase.
        /// </summary>
        /// <param name="phaseIndex">The phase index (0-based).</param>
        /// <returns>A descriptive name for the phase.</returns>
        protected abstract string GetPhaseName(int phaseIndex);

        /// <summary>
        /// Override this to implement the logic for each test phase.
        /// </summary>
        /// <param name="phaseIndex">The current phase index (0-based).</param>
        protected abstract void ExecuteTestPhase(int phaseIndex);

        /// <summary>
        /// Override this to perform final validation after all phases complete.
        /// </summary>
        /// <returns>True if validation passes, false otherwise.</returns>
        protected abstract bool PerformFinalValidation();

        /// <summary>
        /// Called when the story test begins. Override to perform setup.
        /// </summary>
        protected virtual void OnStoryTestBegin() { }

        /// <summary>
        /// Called when the story test completes. Override for cleanup.
        /// </summary>
        protected virtual void OnStoryTestComplete() { }

        void Start()
            {
            if (autoRunStoryTest)
                {
                StartStoryTest();
                }
            }

        void Update()
            {
            if (!storyTestComplete)
                {
                UpdateStoryTest();
                }
            }

        /// <summary>
        /// Begins the story test sequence.
        /// </summary>
        public void StartStoryTest()
            {
            Log("🎭 STORY TEST BEGINS");
            Log($"📖 Testing {TotalTestPhases} phases over {testDuration} seconds");

            OnStoryTestBegin();

            storyTime = 0f;
            currentTestPhase = 0;
            storyTestComplete = false;
            storyTestPassed = true;

            Log("✅ Setup Complete: Story test initialized and ready to begin");
            }

        /// <summary>
        /// Manually run the story test (useful for debugging).
        /// </summary>
        public void RunStoryTestManually()
            {
            StartStoryTest();
            }

        /// <summary>
        /// Updates the story test, advancing through each phase.
        /// </summary>
        private void UpdateStoryTest()
            {
            storyTime += Time.deltaTime;

            // Calculate which phase we should be in based on time
            int targetPhase = Mathf.FloorToInt((storyTime / testDuration) * TotalTestPhases);
            targetPhase = Mathf.Clamp(targetPhase, 0, TotalTestPhases - 1);

            // Advance phases as needed
            while (currentTestPhase < targetPhase && currentTestPhase < TotalTestPhases)
                {
                AdvanceToNextPhase();
                }

            // Execute current phase
            if (currentTestPhase < TotalTestPhases)
                {
                try
                    {
                    ExecuteTestPhase(currentTestPhase);
                    }
                catch (System.Exception ex)
                    {
                    LogError($"Phase {currentTestPhase} failed: {ex.Message}");
                    storyTestPassed = false;
                    }
                }

            // Check if test should complete
            if (storyTime >= testDuration || currentTestPhase >= TotalTestPhases)
                {
                CompleteStoryTest();
                }
            }

        /// <summary>
        /// Advances to the next test phase.
        /// </summary>
        private void AdvanceToNextPhase()
            {
            if (currentTestPhase < TotalTestPhases)
                {
                string phaseName = GetPhaseName(currentTestPhase);
                Log($"🎬 Phase {currentTestPhase + 1}/{TotalTestPhases}: {phaseName}");
                currentTestPhase++;
                }
            }

        /// <summary>
        /// Completes the story test and performs final validation.
        /// </summary>
        private void CompleteStoryTest()
            {
            Log("🏆 FINAL ACT: Validation");

            bool validationPassed = false;
            try
                {
                validationPassed = PerformFinalValidation();
                }
            catch (System.Exception ex)
                {
                LogError($"Final validation failed: {ex.Message}");
                validationPassed = false;
                }

            storyTestPassed &= validationPassed;

            OnStoryTestComplete();

            if (storyTestPassed)
                {
                Log("✅ STORY TEST PASSED: All phases completed successfully!");
                Log("🎉 The play runs perfectly - every actor delivered their lines!");
                }
            else
                {
                LogError("❌ STORY TEST FAILED: Some phases or validation failed");
                LogError("💔 The audience exits disappointed - plot holes were found");
                }

            storyTestComplete = true;
            }

        /// <summary>
        /// Logs a message if debug logging is enabled.
        /// </summary>
        /// <param name="message">The message to log.</param>
        protected void Log(string message)
            {
            if (enableDebugLogging)
                {
                Debug.Log($"[StoryTest] {message}");
                }
            }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        protected void LogError(string message)
            {
            Debug.LogError($"[StoryTest] {message}");
            }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        protected void LogWarning(string message)
            {
            Debug.LogWarning($"[StoryTest] {message}");
            }

        /// <summary>
        /// Gets the current story time (how long the test has been running).
        /// </summary>
        protected float StoryTime => storyTime;

        /// <summary>
        /// Gets the current test phase (0-based).
        /// </summary>
        protected int CurrentTestPhase => currentTestPhase;

        /// <summary>
        /// Gets whether the story test is complete.
        /// </summary>
        public bool IsStoryTestComplete => storyTestComplete;

        /// <summary>
        /// Gets whether the story test passed.
        /// </summary>
        public bool DidStoryTestPass => storyTestPassed;
        }
    }
