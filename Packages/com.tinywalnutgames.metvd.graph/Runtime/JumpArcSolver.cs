using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// Physics-based jump arc calculations for Metroidvania movement
    /// Provides trajectory analysis for platforming and navigation systems
    /// </summary>
    [BurstCompile]
    public static class JumpArcSolver
    {
        /// <summary>
        /// Calculate minimum platform spacing based on jump physics
        /// Fixed: Use output parameter instead of return for Burst compatibility
        /// </summary>
        [BurstCompile]
        public static void CalculateMinimumPlatformSpacing(in JumpArcPhysics physics, out int2 result)
        {
            // Calculate horizontal distance based on jump capabilities
            var horizontalSpacing = (int)math.ceil(physics.JumpDistance * 0.8f); // 80% of max distance for safety
            
            // Calculate vertical spacing based on jump height
            var verticalSpacing = (int)math.ceil(physics.JumpHeight * 0.7f); // 70% of max height for reachability
            
            result = new int2(horizontalSpacing, verticalSpacing);
        }
        
        /// <summary>
        /// Check if a destination is reachable from a starting position
        /// </summary>
        [BurstCompile]
        public static bool IsReachable(in int2 from, in int2 to, Ability availableAbilities, in JumpArcPhysics physics)
        {
            var distance = math.distance((float2)from, (float2)to);
            var heightDiff = to.y - from.y;
            
            // Basic jump check
            if (distance <= physics.JumpDistance && heightDiff <= physics.JumpHeight)
            {
                return true;
            }
            
            // Double jump extends range
            if ((availableAbilities & Ability.DoubleJump) != 0)
            {
                var extendedHeight = physics.JumpHeight * physics.DoubleJumpBonus;
                var extendedDistance = physics.JumpDistance * 1.2f;
                
                if (distance <= extendedDistance && heightDiff <= extendedHeight)
                {
                    return true;
                }
            }
            
            // Wall jump for vertical movement
            if ((availableAbilities & Ability.WallJump) != 0 && math.abs(from.x - to.x) <= 2)
            {
                if (heightDiff <= physics.WallJumpHeight * 3) // Multiple wall jumps
                {
                    return true;
                }
            }
            
            // Dash extends horizontal range
            if ((availableAbilities & Ability.Dash) != 0)
            {
                var dashDistance = distance + physics.DashDistance;
                if (dashDistance <= physics.JumpDistance + physics.DashDistance && heightDiff <= physics.JumpHeight)
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Calculate jump arc trajectory data
        /// ✅ ACTUALLY FIXED: Use output parameter instead of return for Burst compatibility
        /// </summary>
        [BurstCompile]
        public static void CalculateJumpArc(in int2 from, in int2 to, in JumpArcPhysics physics, out JumpArcData result)
        {
            var delta = (float2)(to - from);
            var distance = math.length(delta);
            var direction = math.normalize(delta);
            
            // Calculate initial velocity needed
            var gravity = physics.GravityScale * 9.81f;
            var timeToTarget = math.sqrt(2.0f * math.abs(delta.y) / gravity);
            
            if (timeToTarget <= 0.001f) // Nearly horizontal
            {
                timeToTarget = direction.x != 0 ? distance / physics.JumpDistance : 0.1f;
            }
            
            var initialVelocityX = delta.x / timeToTarget;
            var initialVelocityY = (delta.y + 0.5f * gravity * timeToTarget * timeToTarget) / timeToTarget;
                       
            result = new JumpArcData
            {
                StartPosition = from,
                EndPosition = to,
                InitialVelocity = new float2(initialVelocityX, initialVelocityY),
                FlightTime = timeToTarget,
                PeakHeight = from.y + (initialVelocityY * initialVelocityY) / (2.0f * gravity),
                IsValid = distance <= physics.JumpDistance && math.abs(initialVelocityY) <= physics.JumpHeight * 2.0f
            };
        }
        
        /// <summary>
        /// Validate that a room's key areas are reachable using given movement abilities
        /// ✅ FIXED: Actually use the allocator for temporary pathfinding data structures
        /// </summary>
        [BurstCompile]
        public static bool ValidateRoomReachability(in int2 entrance, in NativeArray<int2>.ReadOnly criticalAreas, 
                                                   Ability availableAbilities, in JumpArcPhysics physics, 
                                                   int roomBoundsX, int roomBoundsY, int roomBoundsWidth, int roomBoundsHeight, 
                                                   Allocator allocator)
        {
            // Early exit if no critical areas to validate
            if (criticalAreas.Length == 0) return true;
            
            // 🔥 USE ALLOCATOR: Create temporary collections for pathfinding algorithm
            var visited = new NativeHashSet<int2>(criticalAreas.Length, allocator);
            var reachableFromEntrance = new NativeHashSet<int2>(criticalAreas.Length, allocator);
            var pathfindingQueue = new NativeQueue<int2>(allocator);
            
            try
            {
                // 🔥 USE ALLOCATOR: Track which areas we can reach directly from entrance
                pathfindingQueue.Enqueue(entrance);
                visited.Add(entrance);
                reachableFromEntrance.Add(entrance);
                
                // 🔥 FLOOD-FILL PATHFINDING: Use allocator-backed collections for BFS
                while (pathfindingQueue.Count > 0)
                {
                    var currentPos = pathfindingQueue.Dequeue();
                    
                    // Check reachability to all critical areas from current position
                    for (int i = 0; i < criticalAreas.Length; i++)
                    {
                        var criticalArea = criticalAreas[i];
                        
                        // Skip if already processed or out of bounds
                        if (visited.Contains(criticalArea) || !IsWithinRoomBounds(criticalArea, roomBoundsX, roomBoundsY, roomBoundsWidth, roomBoundsHeight))
                            continue;
                        
                        // Check if reachable from current position
                        if (IsReachable(in currentPos, in criticalArea, availableAbilities, in physics))
                        {
                            visited.Add(criticalArea);
                            reachableFromEntrance.Add(criticalArea);
                            pathfindingQueue.Enqueue(criticalArea); // Continue pathfinding from this area
                        }
                    }
                }
                
                // 🔥 VALIDATION: Check if all critical areas are reachable
                for (int i = 0; i < criticalAreas.Length; i++)
                {
                    var criticalArea = criticalAreas[i];
                    
                    // Skip entrance (always reachable from itself)
                    if (criticalArea.Equals(entrance)) continue;
                    
                    // Skip out-of-bounds areas
                    if (!IsWithinRoomBounds(criticalArea, roomBoundsX, roomBoundsY, roomBoundsWidth, roomBoundsHeight))
                        continue;
                    
                    // Check if this critical area is reachable
                    if (!reachableFromEntrance.Contains(criticalArea))
                    {
                        return false; // Found unreachable critical area
                    }
                }
                
                return true; // All critical areas are reachable
            }
            finally
            {
                // 🔥 CLEANUP: Always dispose allocator-backed collections
                if (visited.IsCreated) visited.Dispose();
                if (reachableFromEntrance.IsCreated) reachableFromEntrance.Dispose();
                if (pathfindingQueue.IsCreated) pathfindingQueue.Dispose();
            }
        }
        
        /// <summary>
        /// Helper method to check room bounds without struct parameters
        /// </summary>
        [BurstCompile]
        private static bool IsWithinRoomBounds(in int2 position, int roomBoundsX, int roomBoundsY, int roomBoundsWidth, int roomBoundsHeight)
        {
            return position.x >= roomBoundsX && position.x < roomBoundsX + roomBoundsWidth &&
                   position.y >= roomBoundsY && position.y < roomBoundsY + roomBoundsHeight;
        }
        
        /// <summary>
        /// Validate reachability using a single critical area (convenience overload)
        /// ✅ FIXED: Actually use allocator for temporary array
        /// </summary>
        [BurstCompile]
        public static bool ValidateRoomReachability(in int2 entrance, in int2 criticalArea, 
                                                   Ability availableAbilities, in JumpArcPhysics physics, 
                                                   int roomBoundsX, int roomBoundsY, int roomBoundsWidth, int roomBoundsHeight, 
                                                   Allocator allocator)
        {
            // 🔥 USE ALLOCATOR: Create temporary array for single critical area
            var tempArray = new NativeArray<int2>(1, allocator);
            
            try
            {
                tempArray[0] = criticalArea;
                
                return ValidateRoomReachability(in entrance, tempArray.AsReadOnly(), availableAbilities, in physics, 
                                              roomBoundsX, roomBoundsY, roomBoundsWidth, roomBoundsHeight, allocator);
            }
            finally
            {
                // 🔥 CLEANUP: Always dispose allocator-backed memory
                if (tempArray.IsCreated) tempArray.Dispose();
            }
        }
        
        /// <summary>
        /// Check if a position is reachable from another position (convenience alias for IsReachable)
        /// </summary>
        [BurstCompile]
        public static bool IsPositionReachable(in int2 from, in int2 to, Ability availableAbilities, in JumpArcPhysics physics)
        {
            return IsReachable(in from, in to, availableAbilities, in physics);
        }
    }
    
    /// <summary>
    /// Data structure for jump arc calculations
    /// </summary>
    public struct JumpArcData
    {
        public int2 StartPosition;
        public int2 EndPosition;
        public float2 InitialVelocity;
        public float FlightTime;
        public float PeakHeight;
        public bool IsValid;
    }
}
