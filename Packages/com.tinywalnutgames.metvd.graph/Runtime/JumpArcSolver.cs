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
            if (math.abs(delta.y) < 0.1f && math.abs(delta.x) <= physics.JumpDistance) return true;
            if ((availableMovement & Ability.Dash) != 0 && math.abs(delta.x) <= physics.DashDistance && math.abs(delta.y) < 1.0f) return true;
            if ((availableMovement & Ability.Jump) != 0)
            {
                if (CanReachWithJump(delta, physics, false)) return true;
                if ((availableMovement & Ability.DoubleJump) != 0 && CanReachWithJump(delta, physics, true)) return true;
            }
            if ((availableMovement & Ability.WallJump) != 0 && delta.y > 0)
                return CanReachWithWallJumps(delta, physics);
            if ((availableMovement & Ability.Grapple) != 0)
            {
                float grappleRange = physics.JumpDistance * 2.0f;
                return math.length(delta) <= grappleRange;
            }
            return false;
        }
        [BurstCompile]
        private static bool CanReachWithJump(float2 delta, JumpArcPhysics physics, bool doubleJump)
        {
            float horizontalDistance = math.abs(delta.x); float verticalDistance = delta.y;
            float maxJumpHeight = physics.JumpHeight; float maxJumpDistance = physics.JumpDistance;
            if (doubleJump) { maxJumpHeight *= physics.DoubleJumpBonus; maxJumpDistance *= physics.DoubleJumpBonus; }
            if (horizontalDistance > maxJumpDistance) return false;
            if (verticalDistance > 0 && verticalDistance > maxJumpHeight) return false;
            if (verticalDistance < 0)
            {
                float timeToFall = math.sqrt(2.0f * math.abs(verticalDistance) / physics.GravityScale);
                float maxHorizontalInTime = maxJumpDistance;
                return horizontalDistance <= maxHorizontalInTime;
            }
            float discriminant = maxJumpHeight * maxJumpHeight - 2.0f * physics.GravityScale * verticalDistance * horizontalDistance * horizontalDistance / (maxJumpDistance * maxJumpDistance);
            return discriminant >= 0;
        }
        [BurstCompile]
        private static bool CanReachWithWallJumps(float2 delta, JumpArcPhysics physics)
        {
            float verticalGain = physics.WallJumpHeight; int maxWallJumps = (int)math.ceil(delta.y / verticalGain);
            return maxWallJumps <= 3 && math.abs(delta.x) <= physics.JumpDistance * maxWallJumps;
        }
        [BurstCompile]
        public static void GenerateReachablePositions(int2 startPos, Ability availableMovement, JumpArcPhysics physics,
                                                       RectInt roomBounds, NativeList<int2> reachablePositions, Allocator allocator, int maxPositions = int.MaxValue)
        {
            if (!reachablePositions.IsCreated) return;
            var visited = new NativeHashSet<int2>(roomBounds.width * roomBounds.height, allocator);
            var queue = new NativeQueue<int2>(allocator);
            queue.Enqueue(startPos); visited.Add(startPos); reachablePositions.Add(startPos);
            var basicMovement = new NativeArray<int2>(8, allocator);
            basicMovement[0] = new int2(1, 0); basicMovement[1] = new int2(-1, 0); basicMovement[2] = new int2(0, 1); basicMovement[3] = new int2(0, -1);
            basicMovement[4] = new int2(1, 1); basicMovement[5] = new int2(-1, 1); basicMovement[6] = new int2(1, -1); basicMovement[7] = new int2(-1, -1);
            while (queue.TryDequeue(out int2 currentPos))
            {
                for (int i = 0; i < basicMovement.Length; i++)
                {
                    var newPos = currentPos + basicMovement[i];
                    if (!IsWithinBounds(newPos, roomBounds) || visited.Contains(newPos)) continue;
                    if (IsPositionReachable(currentPos, newPos, availableMovement, physics)) { visited.Add(newPos); queue.Enqueue(newPos); reachablePositions.Add(newPos); }
                }
                if ((availableMovement & Ability.Jump) != 0)
                    AddJumpReachablePositions(currentPos, physics, roomBounds, availableMovement, visited, queue, reachablePositions);
                if ((availableMovement & Ability.Dash) != 0)
                    AddDashReachablePositions(currentPos, physics, roomBounds, visited, queue, reachablePositions);
                if (reachablePositions.Length >= maxPositions) break;
            }
            basicMovement.Dispose(); visited.Dispose(); queue.Dispose();
        }
        [BurstCompile] private static bool IsWithinBounds(int2 p, RectInt b) => p.x >= b.x && p.x < b.x + b.width && p.y >= b.y && p.y < b.y + b.height;
        [BurstCompile]
        private static void AddJumpReachablePositions(int2 currentPos, JumpArcPhysics physics, RectInt roomBounds,
                                                       Ability availableMovement, NativeHashSet<int2> visited,
                                                       NativeQueue<int2> queue, NativeList<int2> reachablePositions)
        {
            int jumpRange = (int)physics.JumpDistance; int jumpHeight = (int)physics.JumpHeight;
            for (int x = -jumpRange; x <= jumpRange; x++)
                for (int y = -jumpHeight; y <= jumpHeight; y++)
                {
                    if (x == 0 && y == 0) continue; var targetPos = currentPos + new int2(x, y);
                    if (!IsWithinBounds(targetPos, roomBounds) || visited.Contains(targetPos)) continue;
                    if (IsPositionReachable(currentPos, targetPos, availableMovement, physics)) { visited.Add(targetPos); queue.Enqueue(targetPos); reachablePositions.Add(targetPos); }
                }
        }
        [BurstCompile]
        private static void AddDashReachablePositions(int2 currentPos, JumpArcPhysics physics, RectInt roomBounds,
                                                       NativeHashSet<int2> visited, NativeQueue<int2> queue, NativeList<int2> reachablePositions)
        {
            int dashRange = (int)physics.DashDistance;
            for (int x = -dashRange; x <= dashRange; x++)
            {
                if (x == 0) continue; var targetPos = currentPos + new int2(x, 0);
                if (IsWithinBounds(targetPos, roomBounds) && !visited.Contains(targetPos)) { visited.Add(targetPos); queue.Enqueue(targetPos); reachablePositions.Add(targetPos); }
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
                    if (math.all(reachablePositions[j] == criticalPositions[i])) { found = true; break; }
                }
                if (!found) { allReachable = false; break; }
            }
            reachablePositions.Dispose();
            return allReachable;
        }
        /// <summary>
        /// Convenience overload supplying a default JumpArcPhysics when physics values are not provided.
        /// </summary>
        public static bool IsPositionReachable(int2 startPos, int2 targetPos, Ability availableMovement) => IsPositionReachable(startPos, targetPos, availableMovement, new JumpArcPhysics());
        /// <summary>
        /// Overload for tests that previously called ValidateRoomReachability without physics parameter.
        /// Uses default JumpArcPhysics configuration.
        /// </summary>
        public static bool ValidateRoomReachability(int2 entrancePos, NativeArray<int2> criticalPositions,
                                                     Ability playerMovement, RectInt roomBounds, Allocator allocator) =>
            ValidateRoomReachability(entrancePos, criticalPositions, playerMovement, new JumpArcPhysics(), roomBounds, allocator);
        /// <summary>
        /// Legacy 3-argument overload used by early pipeline code (derives bounds & uses Temp allocator).
        /// </summary>
        public static bool ValidateRoomReachability(int2 entrancePos, NativeArray<int2> criticalPositions, Ability playerMovement)
        {
            int minX = entrancePos.x, maxX = entrancePos.x, minY = entrancePos.y, maxY = entrancePos.y;
            for (int i = 0; i < criticalPositions.Length; i++)
            { var p = criticalPositions[i]; minX = math.min(minX, p.x); maxX = math.max(maxX, p.x); minY = math.min(minY, p.y); maxY = math.max(maxY, p.y); }
            var bounds = new RectInt(minX, minY, math.max(1, (maxX - minX) + 1), math.max(1, (maxY - minY) + 1));
            return ValidateRoomReachability(entrancePos, criticalPositions, playerMovement, new JumpArcPhysics(), bounds, Allocator.Temp);
        }
        /// <summary>Minimum safe platform spacing heuristic for generation.</summary>
        [BurstCompile]
        public static float2 CalculateMinimumPlatformSpacing(JumpArcPhysics physics)
        {
            float horizontalSpacing = physics.JumpDistance * 0.8f;
            float verticalSpacing = physics.JumpHeight * 0.7f;
            return new float2(horizontalSpacing, verticalSpacing);
        }
        /// <summary>Alias retained for backward compatibility.</summary>
        [BurstCompile]
        public static bool IsReachable(int2 fromPos, int2 toPos, Ability playerMovement, JumpArcPhysics physics) => IsPositionReachable(fromPos, toPos, playerMovement, physics);
        /// <summary>Compute jump arc data between two grid positions.</summary>
        [BurstCompile]
        public static JumpArcData CalculateJumpArc(int2 startPos, int2 endPos, JumpArcPhysics physics)
        {
            float2 start = startPos; float2 end = endPos; float2 delta = end - start;
            float horizontalDistance = math.abs(delta.x); float verticalDistance = delta.y;
            float timeToReach = math.max(0.0001f, horizontalDistance / math.max(0.0001f, physics.JumpDistance));
            float initialVerticalVelocity = (verticalDistance + 0.5f * physics.GravityScale * timeToReach * timeToReach) / timeToReach;
            return new JumpArcData
            {
                StartPosition = start,
                EndPosition = end,
                InitialVelocity = new float2(physics.JumpDistance / timeToReach, initialVerticalVelocity),
                FlightTime = timeToReach,
                ApexHeight = start.y + (initialVerticalVelocity * initialVerticalVelocity) / (2.0f * physics.GravityScale)
            };
        }
        /// <summary>
        /// Validate room reachability from platform and obstacle positions using legacy JumpPhysicsData.
        /// </summary>
        public static bool ValidateRoomReachability(NativeArray<float2> platformPositions, NativeArray<int2> obstaclePositions, JumpPhysicsData jumpPhysics)
        {
            if (platformPositions.Length == 0) return true;
            var entrance = new int2((int)platformPositions[0].x, (int)platformPositions[0].y);
            var critical = new NativeArray<int2>(platformPositions.Length, Allocator.Temp);
            for (int i = 0; i < platformPositions.Length; i++)
            {
                var p = platformPositions[i];
                critical[i] = new int2((int)p.x, (int)p.y);
            }
            var bounds = CalculateBounds(critical);
            bool ok = ValidateRoomReachability(entrance, critical, Ability.Jump | Ability.DoubleJump, new JumpArcPhysics
            {
                JumpHeight = jumpPhysics.JumpHeight,
                JumpDistance = jumpPhysics.JumpDistance,
                DashDistance = jumpPhysics.DashDistance,
                WallJumpHeight = jumpPhysics.WallJumpHeight
            }, bounds, Allocator.Temp);
            critical.Dispose();
            return ok;
        }
        private static RectInt CalculateBounds(NativeArray<int2> points)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            for (int i = 0; i < points.Length; i++) { var p = points[i]; minX = math.min(minX, p.x); maxX = math.max(maxX, p.x); minY = math.min(minY, p.y); maxY = math.max(maxY, p.y); }
            return new RectInt(minX, minY, math.max(1, (maxX - minX)+1), math.max(1, (maxY - minY)+1));
        }
        
        /// <summary>
        /// Calculate comprehensive arc data for movement validation and energy analysis
        /// </summary>
        [BurstCompile]
        public static JumpArcAnalysisData CalculateArcData(int2 startPos, int2 targetPos, Ability movement, JumpArcPhysics physics)
        {
            var basicArc = CalculateJumpArc(startPos, targetPos, physics);
            var requiresAdvanced = !IsPositionReachable(startPos, targetPos, Ability.Jump, physics);
            var energyCost = CalculateEnergyRequirement(startPos, targetPos, movement, physics);
            
            return new JumpArcAnalysisData
            {
                ArcData = basicArc,
                RequiresAdvancedMovement = requiresAdvanced,
                EnergyRequired = energyCost,
                MovementType = DetermineOptimalMovement(startPos, targetPos, movement, physics)
            };
        }
        
        [BurstCompile]
        private static float CalculateEnergyRequirement(int2 startPos, int2 targetPos, Ability movement, JumpArcPhysics physics)
        {
            float2 delta = (float2)targetPos - (float2)startPos;
            float distance = math.length(delta);
            float heightDiff = math.abs(delta.y);
            
            // Base energy cost scales with distance and height
            float energyCost = distance * 0.1f + heightDiff * 0.2f;
            
            // Advanced movement abilities cost more energy
            if ((movement & Ability.DoubleJump) != 0) energyCost *= 1.5f;
            if ((movement & Ability.Dash) != 0) energyCost *= 1.3f;
            if ((movement & Ability.WallJump) != 0) energyCost *= 1.4f;
            
            return energyCost;
        }
        
        [BurstCompile]
        private static Ability DetermineOptimalMovement(int2 startPos, int2 targetPos, Ability availableMovement, JumpArcPhysics physics)
        {
            // Try movements in order of efficiency
            if ((availableMovement & Ability.Jump) != 0 && IsPositionReachable(startPos, targetPos, Ability.Jump, physics))
                return Ability.Jump;
            if ((availableMovement & Ability.Dash) != 0 && IsPositionReachable(startPos, targetPos, Ability.Dash, physics))
                return Ability.Dash;
            if ((availableMovement & Ability.DoubleJump) != 0 && IsPositionReachable(startPos, targetPos, Ability.DoubleJump, physics))
                return Ability.DoubleJump;
            if ((availableMovement & Ability.WallJump) != 0 && IsPositionReachable(startPos, targetPos, Ability.WallJump, physics))
                return Ability.WallJump;
                
            return Ability.None; // No viable movement found
        }
    }
    
    /// <summary>
    /// Extended arc data for movement analysis and energy management
    /// </summary>
    public struct JumpArcAnalysisData
    {
        public JumpArcData ArcData;
        public bool RequiresAdvancedMovement;
        public float EnergyRequired;
        public Ability MovementType;
    }
    
    public struct JumpArcData { public float2 StartPosition; public float2 EndPosition; public float2 InitialVelocity; public float FlightTime; public float ApexHeight; }
}
