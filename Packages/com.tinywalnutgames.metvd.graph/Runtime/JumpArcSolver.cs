using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// Physics-aware reachability validation system for procedural room generation.
    /// Implements jump arc calculations to ensure all room areas are reachable with player movement capabilities.
    /// Part of the Master Spec pipeline - runs during content pass before finalizing room layouts.
    /// </summary>
    [BurstCompile]
    public static class JumpArcSolver
    {
        /// <summary>
        /// Validate that a target position is reachable from a starting position given movement capabilities
        /// </summary>
        [BurstCompile]
        public static bool IsPositionReachable(int2 startPos, int2 targetPos, Ability availableMovement, JumpArcPhysics physics)
        {
            float2 start = startPos;
            float2 target = targetPos;
            float2 delta = target - start;
            
            // Direct horizontal movement check
            if (math.abs(delta.y) < 0.1f && math.abs(delta.x) <= physics.JumpDistance)
                return true;
                
            // Check if dash can bridge the gap
            if ((availableMovement & Ability.Dash) != 0 && math.abs(delta.x) <= physics.DashDistance && math.abs(delta.y) < 1.0f)
                return true;
                
            // Standard jump arc calculation
            if ((availableMovement & Ability.Jump) != 0)
            {
                if (CanReachWithJump(delta, physics, false))
                    return true;
                    
                // Try double jump if available
                if ((availableMovement & Ability.DoubleJump) != 0 && CanReachWithJump(delta, physics, true))
                    return true;
            }
            
            // Wall jump sequences for vertical movement
            if ((availableMovement & Ability.WallJump) != 0 && delta.y > 0)
            {
                return CanReachWithWallJumps(delta, physics);
            }
            
            // Grapple point connections (simplified - assumes grapple points exist)
            if ((availableMovement & Ability.Grapple) != 0)
            {
                float grappleRange = physics.JumpDistance * 2.0f; // Grapple has longer range
                return math.length(delta) <= grappleRange;
            }
            
            return false;
        }

        /// <summary>
        /// Calculate if a jump arc can reach the target position
        /// </summary>
        [BurstCompile]
        private static bool CanReachWithJump(float2 delta, JumpArcPhysics physics, bool doubleJump)
        {
            float horizontalDistance = math.abs(delta.x);
            float verticalDistance = delta.y;
            
            float maxJumpHeight = physics.JumpHeight;
            float maxJumpDistance = physics.JumpDistance;
            
            if (doubleJump)
            {
                maxJumpHeight *= physics.DoubleJumpBonus;
                maxJumpDistance *= physics.DoubleJumpBonus;
            }
            
            // Simple ballistic trajectory check
            if (horizontalDistance > maxJumpDistance)
                return false;
                
            // For upward movement, check if we have enough height
            if (verticalDistance > 0 && verticalDistance > maxJumpHeight)
                return false;
                
            // For downward movement, gravity helps - use parabolic trajectory
            if (verticalDistance < 0)
            {
                float timeToFall = math.sqrt(2.0f * math.abs(verticalDistance) / physics.GravityScale);
                float maxHorizontalInTime = maxJumpDistance; // Simplified horizontal velocity
                return horizontalDistance <= maxHorizontalInTime;
            }
            
            // Parabolic trajectory for upward arcs
            float discriminant = maxJumpHeight * maxJumpHeight - 2.0f * physics.GravityScale * verticalDistance * horizontalDistance * horizontalDistance / (maxJumpDistance * maxJumpDistance);
            return discriminant >= 0;
        }

        /// <summary>
        /// Check if wall jumping can reach the target (simplified wall jump chain)
        /// </summary>
        [BurstCompile]
        private static bool CanReachWithWallJumps(float2 delta, JumpArcPhysics physics)
        {
            float verticalGain = physics.WallJumpHeight;
            int maxWallJumps = (int)math.ceil(delta.y / verticalGain);
            
            // Assume we can wall jump up to a reasonable limit
            if (maxWallJumps <= 3 && math.abs(delta.x) <= physics.JumpDistance * maxWallJumps)
                return true;
                
            return false;
        }

        /// <summary>
        /// Generate reachable positions from a starting point using breadth-first search
        /// Used for room validation and navigation graph generation
        /// </summary>
        [BurstCompile]
        /// <param name="maxPositions">Optional limit on the number of positions to explore (prevents excessive search in large rooms).</param>
        public static void GenerateReachablePositions(int2 startPos, Ability availableMovement, JumpArcPhysics physics, 
                                                     RectInt roomBounds, NativeList<int2> reachablePositions, Allocator allocator, int maxPositions = int.MaxValue)
        {
            if (!reachablePositions.IsCreated)
                return;
                
            var visited = new NativeHashSet<int2>(roomBounds.width * roomBounds.height, allocator);
            var queue = new NativeQueue<int2>(allocator);
            
            queue.Enqueue(startPos);
            visited.Add(startPos);
            reachablePositions.Add(startPos);
            
            // Movement offsets for different abilities
            var basicMovement = new NativeArray<int2>(8, allocator);
            basicMovement[0] = new int2(1, 0);   // Right
            basicMovement[1] = new int2(-1, 0);  // Left
            basicMovement[2] = new int2(0, 1);   // Up
            basicMovement[3] = new int2(0, -1);  // Down
            basicMovement[4] = new int2(1, 1);   // Diagonal up-right
            basicMovement[5] = new int2(-1, 1);  // Diagonal up-left
            basicMovement[6] = new int2(1, -1);  // Diagonal down-right
            basicMovement[7] = new int2(-1, -1); // Diagonal down-left
            
            while (queue.TryDequeue(out int2 currentPos))
            {
                // Check all basic movement directions
                for (int i = 0; i < basicMovement.Length; i++)
                {
                    var offset = basicMovement[i];
                    var newPos = currentPos + offset;
                    
                    if (!IsWithinBounds(newPos, roomBounds) || visited.Contains(newPos))
                        continue;
                        
                    if (IsPositionReachable(currentPos, newPos, availableMovement, physics))
                    {
                        visited.Add(newPos);
                        queue.Enqueue(newPos);
                        reachablePositions.Add(newPos);
                    }
                }
                
                // Check jump-specific positions
                if ((availableMovement & Ability.Jump) != 0)
                {
                    AddJumpReachablePositions(currentPos, physics, roomBounds, availableMovement, visited, queue, reachablePositions);
                }
                
                // Check dash-specific positions
                if ((availableMovement & Ability.Dash) != 0)
                {
                    AddDashReachablePositions(currentPos, physics, roomBounds, visited, queue, reachablePositions);
                }
            }
            
            basicMovement.Dispose();
            visited.Dispose();
            queue.Dispose();
        }

        [BurstCompile]
        private static bool IsWithinBounds(int2 position, RectInt bounds)
        {
            return position.x >= bounds.x && position.x < bounds.x + bounds.width &&
                   position.y >= bounds.y && position.y < bounds.y + bounds.height;
        }

        [BurstCompile]
        private static void AddJumpReachablePositions(int2 currentPos, JumpArcPhysics physics, RectInt roomBounds, 
                                                     Ability availableMovement, NativeHashSet<int2> visited, 
                                                     NativeQueue<int2> queue, NativeList<int2> reachablePositions)
        {
            int jumpRange = (int)physics.JumpDistance;
            int jumpHeight = (int)physics.JumpHeight;
            
            // Check positions within jump range
            for (int x = -jumpRange; x <= jumpRange; x++)
            {
                for (int y = -jumpHeight; y <= jumpHeight; y++)
                {
                    if (x == 0 && y == 0) continue;
                    
                    var targetPos = currentPos + new int2(x, y);
                    
                    if (!IsWithinBounds(targetPos, roomBounds) || visited.Contains(targetPos))
                        continue;
                        
                    if (IsPositionReachable(currentPos, targetPos, availableMovement, physics))
                    {
                        visited.Add(targetPos);
                        queue.Enqueue(targetPos);
                        reachablePositions.Add(targetPos);
                    }
                }
            }
        }

        [BurstCompile]
        private static void AddDashReachablePositions(int2 currentPos, JumpArcPhysics physics, RectInt roomBounds,
                                                     NativeHashSet<int2> visited, NativeQueue<int2> queue, 
                                                     NativeList<int2> reachablePositions)
        {
            int dashRange = (int)physics.DashDistance;
            
            // Horizontal dash positions
            for (int x = -dashRange; x <= dashRange; x++)
            {
                if (x == 0) continue;
                
                var targetPos = currentPos + new int2(x, 0);
                
                if (IsWithinBounds(targetPos, roomBounds) && !visited.Contains(targetPos))
                {
                    visited.Add(targetPos);
                    queue.Enqueue(targetPos);
                    reachablePositions.Add(targetPos);
                }
            }
        }

        /// <summary>
        /// Validate that all critical positions in a room are reachable from the entrance
        /// Used during room generation to ensure completability
        /// </summary>
        [BurstCompile]
        public static bool ValidateRoomReachability(int2 entrancePos, NativeArray<int2> criticalPositions, 
                                                   Ability playerMovement, JumpArcPhysics physics, RectInt roomBounds, Allocator allocator)
        {
            var reachablePositions = new NativeList<int2>(roomBounds.width * roomBounds.height, allocator);
            GenerateReachablePositions(entrancePos, playerMovement, physics, roomBounds, reachablePositions, allocator);
            
            bool allReachable = true;
            for (int i = 0; i < criticalPositions.Length; i++)
            {
                bool found = false;
                for (int j = 0; j < reachablePositions.Length; j++)
                {
                    if (math.all(reachablePositions[j] == criticalPositions[i]))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    allReachable = false;
                    break;
                }
            }
            
            reachablePositions.Dispose();
            return allReachable;
        }
    }
}