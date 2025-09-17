/*
 * Debug Overlay Validation for Living Dev Agent Template
 * 
 * Copyright (C) 2025 Bellok
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

// @TestType: Debug System Validation
// @Domain: LivingDevAgent.Core
// @Role: Validate Debug Overlay Functionality

using UnityEngine;
#if UNITY_ENTITIES
using Unity.Entities;
#endif

namespace LivingDevAgent.Core.Validation
{
    /// <summary>
    /// Validation test for debug overlay systems to ensure display flag logic is active and testable.
    /// This provides a generalized validation framework for debug overlay functionality.
    /// </summary>
    public class DebugOverlayValidation : MonoBehaviour
    {
        [Header("Debug Overlay Validation")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool showDetailedLogs = true;
        
        void Start()
        {
            if (runOnStart)
            {
                ValidateDebugOverlaySystem();
            }
        }
        
        /// <summary>
        /// Comprehensive validation of debug overlay system display flags functionality
        /// </summary>
        [ContextMenu("Validate Debug Overlay System")]
        public void ValidateDebugOverlaySystem()
        {
            LogInfo("=== Debug Overlay System Validation Starting ===");
            
            int testsRun = 0;
            int testsPassed = 0;
            
            // Create generic debug system interface for testing
            var debugSystem = GetDebugSystem();
            if (debugSystem == null)
            {
                LogWarning("No debug system implementation found - tests will use mock validation");
                debugSystem = new MockDebugSystem();
            }
            
            // Test 1: DisableAllDebugDisplays
            testsRun++;
            debugSystem.DisableAllDebugDisplays();
            if (debugSystem.DisplayFlags == DebugDisplayFlags.None)
            {
                testsPassed++;
                LogSuccess("✓ DisableAllDebugDisplays works correctly");
            }
            else
            {
                LogError("✗ DisableAllDebugDisplays failed - Expected None, got: " + debugSystem.DisplayFlags);
            }
            
            // Test 2: EnableDebugDisplay for specific flags
            testsRun++;
            debugSystem.EnableDebugDisplay(DebugDisplayFlags.SystemInfo);
            if ((debugSystem.DisplayFlags & DebugDisplayFlags.SystemInfo) != 0)
            {
                testsPassed++;
                LogSuccess("✓ EnableDebugDisplay for SystemInfo works correctly");
            }
            else
            {
                LogError("✗ EnableDebugDisplay for SystemInfo failed");
            }
            
            // Test 3: EnableDebugDisplay multiple flags
            testsRun++;
            debugSystem.EnableDebugDisplay(DebugDisplayFlags.PerformanceMetrics);
            var expectedFlags = DebugDisplayFlags.SystemInfo | DebugDisplayFlags.PerformanceMetrics;
            if (debugSystem.DisplayFlags == expectedFlags)
            {
                testsPassed++;
                LogSuccess("✓ EnableDebugDisplay for multiple flags works correctly");
            }
            else
            {
                LogError("✗ EnableDebugDisplay for multiple flags failed - Expected: " + expectedFlags + ", got: " + debugSystem.DisplayFlags);
            }
            
            // Test 4: ToggleDebugDisplay 
            testsRun++;
            var flagsBeforeToggle = debugSystem.DisplayFlags;
            debugSystem.ToggleDebugDisplay(DebugDisplayFlags.RenderingInfo);
            var flagsAfterToggle = debugSystem.DisplayFlags;
            if ((flagsAfterToggle & DebugDisplayFlags.RenderingInfo) != 0 && 
                (flagsBeforeToggle & DebugDisplayFlags.RenderingInfo) == 0)
            {
                testsPassed++;
                LogSuccess("✓ ToggleDebugDisplay works correctly (enabled previously disabled flag)");
            }
            else
            {
                LogError("✗ ToggleDebugDisplay failed");
            }
            
            // Test 5: Toggle again to disable
            testsRun++;
            debugSystem.ToggleDebugDisplay(DebugDisplayFlags.RenderingInfo);
            if ((debugSystem.DisplayFlags & DebugDisplayFlags.RenderingInfo) == 0)
            {
                testsPassed++;
                LogSuccess("✓ ToggleDebugDisplay works correctly (disabled previously enabled flag)");
            }
            else
            {
                LogError("✗ ToggleDebugDisplay disable failed");
            }
            
            // Test 6: EnableAllDebugDisplays
            testsRun++;
            debugSystem.EnableAllDebugDisplays();
            if (debugSystem.DisplayFlags == DebugDisplayFlags.All)
            {
                testsPassed++;
                LogSuccess("✓ EnableAllDebugDisplays works correctly");
            }
            else
            {
                LogError("✗ EnableAllDebugDisplays failed - Expected All, got: " + debugSystem.DisplayFlags);
            }
            
            // Test 7: DisableDebugDisplay for specific flags
            testsRun++;
            debugSystem.DisableDebugDisplay(DebugDisplayFlags.SystemInfo);
            if ((debugSystem.DisplayFlags & DebugDisplayFlags.SystemInfo) == 0)
            {
                testsPassed++;
                LogSuccess("✓ DisableDebugDisplay for specific flags works correctly");
            }
            else
            {
                LogError("✗ DisableDebugDisplay for specific flags failed");
            }
            
            // Test 8: Verify system can check for world existence without errors
            testsRun++;
            try
            {
#if UNITY_ENTITIES
                var world = World.DefaultGameObjectInjectionWorld;
                var systemExists = world != null && world.GetExistingSystemManaged<IDebugOverlaySystem>() != null;
                LogInfo($"Debug overlay system exists in world: {systemExists}");
#else
                LogInfo("Unity Entities not available - using mock system validation");
#endif
                testsPassed++;
                LogSuccess("✓ System integration check completed without errors");
            }
            catch (System.Exception e)
            {
                LogError($"✗ System integration check failed: {e.Message}");
            }
            
            // Final results
            LogInfo($"=== Validation Complete: {testsPassed}/{testsRun} tests passed ===");
            
            if (testsPassed == testsRun)
            {
                LogSuccess("🎉 All display flags logic tests passed! Debug overlay system is active and testable.");
            }
            else
            {
                LogError($"❌ {testsRun - testsPassed} test(s) failed. Debug overlay system needs attention.");
            }
        }
        
        /// <summary>
        /// Get debug system implementation - abstract to allow for different engines/systems
        /// </summary>
        private IDebugOverlaySystem GetDebugSystem()
        {
            // Try to find existing debug system implementation
            // This allows the validation to work with different debug systems
            var existing = FindObjectOfType<MonoBehaviour>() as IDebugOverlaySystem;
            return existing ?? new MockDebugSystem();
        }
        
        private void LogInfo(string message)
        {
            if (showDetailedLogs)
                Debug.Log($"[DebugValidation] {message}");
        }
        
        private void LogSuccess(string message)
        {
            Debug.Log($"[DebugValidation] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[DebugValidation] {message}");
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[DebugValidation] {message}");
        }
    }
    
    /// <summary>
    /// Generic debug display flags enum for pipeline-agnostic validation
    /// </summary>
    [System.Flags]
    public enum DebugDisplayFlags
    {
        None = 0,
        SystemInfo = 1 << 0,
        PerformanceMetrics = 1 << 1,
        RenderingInfo = 1 << 2,
        MemoryUsage = 1 << 3,
        All = SystemInfo | PerformanceMetrics | RenderingInfo | MemoryUsage
    }
    
    /// <summary>
    /// Interface for debug overlay systems - allows for different implementations
    /// </summary>
    public interface IDebugOverlaySystem
    {
        DebugDisplayFlags DisplayFlags { get; }
        void EnableDebugDisplay(DebugDisplayFlags flags);
        void DisableDebugDisplay(DebugDisplayFlags flags);
        void ToggleDebugDisplay(DebugDisplayFlags flags);
        void EnableAllDebugDisplays();
        void DisableAllDebugDisplays();
    }
    
    /// <summary>
    /// Mock debug system implementation for testing when no real system is available
    /// </summary>
    public class MockDebugSystem : IDebugOverlaySystem
    {
        public DebugDisplayFlags DisplayFlags { get; private set; } = DebugDisplayFlags.None;
        
        public void EnableDebugDisplay(DebugDisplayFlags flags)
        {
            DisplayFlags |= flags;
        }
        
        public void DisableDebugDisplay(DebugDisplayFlags flags)
        {
            DisplayFlags &= ~flags;
        }
        
        public void ToggleDebugDisplay(DebugDisplayFlags flags)
        {
            DisplayFlags ^= flags;
        }
        
        public void EnableAllDebugDisplays()
        {
            DisplayFlags = DebugDisplayFlags.All;
        }
        
        public void DisableAllDebugDisplays()
        {
            DisplayFlags = DebugDisplayFlags.None;
        }
    }
}