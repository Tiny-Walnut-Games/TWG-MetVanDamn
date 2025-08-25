using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// Physics-aware reachability validation for room generation
    /// Implements Jump Arc Solver requirement from the issue
    /// </summary>
    [BurstCompile]
    public static class JumpArcSolver
    {
        /// <summary>
        /// Check if a target position is reachable from a start position using jump physics
        /// </summary>
        public static bool IsReachable(float2 startPos, float2 targetPos, in JumpPhysicsData physics)
        {
            float deltaX = targetPos.x - startPos.x;
            float deltaY = targetPos.y - startPos.y;
            
            // Basic distance check
            if (math.abs(deltaX) > physics.MaxJumpDistance)
                return false;
                
            // Basic height check
            if (deltaY > physics.MaxJumpHeight)
                return false;
                
            // Parabolic trajectory calculation
            return CanReachWithParabolicArc(deltaX, deltaY, physics);
        }
        
        /// <summary>
        /// Check if a path between two points is clear of obstacles
        /// </summary>
        public static bool IsPathClear(float2 startPos, float2 targetPos, in NativeArray<int2> obstacles)
        {
            if (obstacles.Length == 0) return true;
            
            // Simple line-of-sight check using Bresenham's algorithm
            int x0 = (int)startPos.x;
            int y0 = (int)startPos.y;
            int x1 = (int)targetPos.x;
            int y1 = (int)targetPos.y;
            
            int dx = math.abs(x1 - x0);
            int dy = math.abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            
            int x = x0, y = y0;
            
            while (true)
            {
                // Check if current position is an obstacle
                int2 currentPos = new int2(x, y);
                for (int i = 0; i < obstacles.Length; i++)
                {
                    if (obstacles[i].x == currentPos.x && obstacles[i].y == currentPos.y)
                        return false;
                }
                
                if (x == x1 && y == y1) break;
                
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Calculate jump vectors between platforms for navigation
        /// </summary>
        public static NativeArray<float2> CalculateJumpVectors(NativeArray<float2> platforms, 
                                                              in JumpPhysicsData physics, 
                                                              Allocator allocator)
        {
            var jumpVectors = new NativeArray<float2>(platforms.Length * platforms.Length, allocator);
            int vectorIndex = 0;
            
            for (int i = 0; i < platforms.Length; i++)
            {
                for (int j = 0; j < platforms.Length; j++)
                {
                    if (i == j) continue;
                    
                    float2 from = platforms[i];
                    float2 to = platforms[j];
                    
                    if (IsReachable(from, to, physics))
                    {
                        jumpVectors[vectorIndex] = to - from;
                        vectorIndex++;
                    }
                }
            }
            
            // Resize to actual number of valid vectors
            var validVectors = new NativeArray<float2>(vectorIndex, allocator);
            for (int i = 0; i < vectorIndex; i++)
            {
                validVectors[i] = jumpVectors[i];
            }
            
            jumpVectors.Dispose();
            return validVectors;
        }
        
        /// <summary>
        /// Validate that a room layout has reachable connections between critical points
        /// </summary>
        public static bool ValidateRoomReachability(NativeArray<float2> criticalPoints,
                                                   NativeArray<int2> obstacles,
                                                   in JumpPhysicsData physics)
        {
            if (criticalPoints.Length < 2) return true;
            
            // Check that each critical point can reach at least one other critical point
            for (int i = 0; i < criticalPoints.Length; i++)
            {
                bool canReachAny = false;
                
                for (int j = 0; j < criticalPoints.Length; j++)
                {
                    if (i == j) continue;
                    
                    if (IsReachable(criticalPoints[i], criticalPoints[j], physics) &&
                        IsPathClear(criticalPoints[i], criticalPoints[j], obstacles))
                    {
                        canReachAny = true;
                        break;
                    }
                }
                
                if (!canReachAny) return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Calculate minimum platform spacing for a given jump difficulty
        /// </summary>
        public static float2 CalculateMinimumPlatformSpacing(in JumpPhysicsData physics, float difficulty = 0.5f)
        {
            // Scale spacing based on difficulty (0.0 = easy, 1.0 = maximum challenge)
            float horizontalSpacing = math.lerp(1.0f, physics.MaxJumpDistance * 0.9f, difficulty);
            float verticalSpacing = math.lerp(0.5f, physics.MaxJumpHeight * 0.9f, difficulty);
            
            return new float2(horizontalSpacing, verticalSpacing);
        }
        
        /// <summary>
        /// Calculate optimal jump arc for reaching a target
        /// </summary>
        public static bool CalculateJumpArc(float2 startPos, float2 targetPos, in JumpPhysicsData physics,
                                          out float launchAngle, out float launchVelocity)
        {
            launchAngle = 0;
            launchVelocity = 0;
            
            float deltaX = targetPos.x - startPos.x;
            float deltaY = targetPos.y - startPos.y;
            
            // Check if target is within range
            if (math.abs(deltaX) > physics.MaxJumpDistance || deltaY > physics.MaxJumpHeight)
                return false;
            
            // Calculate launch parameters for parabolic trajectory
            float g = physics.Gravity;
            float v0_squared = g * (deltaX * deltaX) / (deltaX * math.sin(2 * math.PI/4) - 2 * deltaY * math.cos(math.PI/4) * math.cos(math.PI/4));
            
            if (v0_squared <= 0) return false;
            
            launchVelocity = math.sqrt(v0_squared);
            launchAngle = math.PI / 4; // 45 degrees for optimal distance
            
            return true;
        }
        
        [BurstCompile]
        private static bool CanReachWithParabolicArc(float deltaX, float deltaY, in JumpPhysicsData physics)
        {
            float g = physics.Gravity;
            float v0 = math.sqrt(2 * g * physics.MaxJumpHeight); // Initial velocity for max height
            
            // Check if trajectory can reach the target
            float discriminant = (v0 * v0) * (v0 * v0) - g * (g * deltaX * deltaX + 2 * deltaY * v0 * v0);
            
            return discriminant >= 0;
        }
    }
    
    /// <summary>
    /// Component to store jump arc validation results
    /// </summary>
    public struct JumpArcValidation : IComponentData
    {
        public bool IsValidated;
        public bool IsReachable;
        public float ValidationTime;
        public int TestedConnections;
        public int ValidConnections;
        
        public JumpArcValidation(bool isReachable, int testedConnections, int validConnections)
        {
            IsValidated = true;
            IsReachable = isReachable;
            ValidationTime = UnityEngine.Time.time;
            TestedConnections = testedConnections;
            ValidConnections = validConnections;
        }
    }
    
    /// <summary>
    /// Buffer element for storing valid jump connections
    /// </summary>
    public struct JumpConnectionElement : IBufferElementData
    {
        public float2 FromPosition;
        public float2 ToPosition;
        public float LaunchAngle;
        public float LaunchVelocity;
        public Ability RequiredSkill;
        
        public JumpConnectionElement(float2 from, float2 to, float angle, float velocity, 
                                   Ability skill = Ability.None)
        {
            FromPosition = from;
            ToPosition = to;
            LaunchAngle = angle;
            LaunchVelocity = velocity;
            RequiredSkill = skill;
        }
    }
}