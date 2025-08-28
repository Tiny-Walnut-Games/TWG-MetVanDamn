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
        /// </summary>
        [BurstCompile]
        public static int2 CalculateMinimumPlatformSpacing(in JumpArcPhysics physics)
        {
            // Calculate horizontal distance based on jump capabilities
            var horizontalSpacing = (int)math.ceil(physics.JumpDistance * 0.8f); // 80% of max distance for safety
            
            // Calculate vertical spacing based on jump height
            var verticalSpacing = (int)math.ceil(physics.JumpHeight * 0.7f); // 70% of max height for reachability
            
            return new int2(horizontalSpacing, verticalSpacing);
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
        /// </summary>
        [BurstCompile]
        public static JumpArcData CalculateJumpArc(in int2 from, in int2 to, in JumpArcPhysics physics)
        {
            var delta = (float2)(to - from);
            var distance = math.length(delta);
            var direction = math.normalize(delta);
            
            // Calculate initial velocity needed
            var gravity = physics.GravityScale * 9.81f;
            var timeToTarget = math.sqrt(2.0f * math.abs(delta.y) / gravity);
            
            if (timeToTarget <= 0.001f) // Nearly horizontal
            {
                
                timeToTarget = direction.x != 0 ? distance / physics.JumpDistance : 0.1f; // TODO: is this a valid use of distance? Should it be max horizontal speed?

            }
            
            var initialVelocityX = delta.x / timeToTarget;
            var initialVelocityY = (delta.y + 0.5f * gravity * timeToTarget * timeToTarget) / timeToTarget;
                       
            return new JumpArcData
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
        /// </summary>
        [BurstCompile]
        public static bool ValidateRoomReachability(in int2 entrance, NativeArray<int2> criticalAreas, 
                                                   Ability availableAbilities, in JumpArcPhysics physics, 
                                                   RectInt roomBounds, Allocator allocator)
        {
            // implement an array to use with allocator
            NativeArray<int2> tempArray = new(criticalAreas.Length, allocator);

            // Check if all critical areas are reachable from the entrance
            for (int i = 0; i < criticalAreas.Length; i++)
            {
                var criticalArea = criticalAreas[i];

                // Skip if critical area is the entrance
                if (criticalArea.Equals(entrance))
                {
                    continue;
                }

                // Check if critical area is within tempArray
                if (!tempArray.Contains(criticalArea))
                {
                    continue; // Skip areas not in tempArray
                }

                // Ensure critical area is within room bounds
                if (criticalArea.x < roomBounds.x || criticalArea.x >= roomBounds.x + roomBounds.width ||
                    criticalArea.y < roomBounds.y || criticalArea.y >= roomBounds.y + roomBounds.height)
                {
                    continue; // Skip out-of-bounds areas
                }
                
                // Check direct reachability
                if (!IsReachable(in entrance, in criticalArea, availableAbilities, in physics))
                {
                    // Try pathfinding through other critical areas
                    // how do we utilise allocator here? We could use it for temporary arrays if needed
                    
                    bool foundPath = false;
                    for (int j = 0; j < criticalAreas.Length; j++)
                    {
                        if (i == j) continue;
                        
                        var intermediate = criticalAreas[j];
                        if (IsReachable(in entrance, in intermediate, availableAbilities, in physics) &&
                            IsReachable(in intermediate, in criticalArea, availableAbilities, in physics))
                        {
                            foundPath = true;
                            break;
                        }
                    }
                    
                    if (!foundPath)
                    {
                        return false; // Critical area is unreachable
                    }
                }
            }
            
            return true; // All critical areas are reachable
        }
        
        /// <summary>
        /// Validate reachability using a single critical area (convenience overload)
        /// </summary>
        [BurstCompile]
        public static bool ValidateRoomReachability(in int2 entrance, in int2 criticalArea, 
                                                   Ability availableAbilities, in JumpArcPhysics physics, 
                                                   RectInt roomBounds, Allocator allocator)
        {
            var tempArray = new NativeArray<int2>(1, allocator);
            tempArray[0] = criticalArea;
            
            bool result = ValidateRoomReachability(in entrance, tempArray, availableAbilities, in physics, roomBounds, allocator);
            
            tempArray.Dispose();
            return result;
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
