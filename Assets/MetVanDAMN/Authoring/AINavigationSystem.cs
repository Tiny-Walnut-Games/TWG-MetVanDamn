using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Authoring
{
    /// <summary>
    /// Runtime pathfinding component for AI agents
    /// Contains current pathfinding state and target information
    /// </summary>
    public struct AINavigationState : IComponentData
    {
        /// <summary>
        /// Current navigation target node ID
        /// </summary>
        public uint TargetNodeId;
        
        /// <summary>
        /// Current navigation source node ID
        /// </summary>
        public uint CurrentNodeId;
        
        /// <summary>
        /// Pathfinding state
        /// </summary>
        public PathfindingStatus Status;
        
        /// <summary>
        /// Number of nodes in current path
        /// </summary>
        public int PathLength;
        
        /// <summary>
        /// Current step in the path (0-based index)
        /// </summary>
        public int CurrentPathStep;
        
        /// <summary>
        /// Total cost of current path
        /// </summary>
        public float PathCost;
        
        /// <summary>
        /// Last time pathfinding was requested
        /// </summary>
        public double LastPathfindTime;

        public AINavigationState(uint currentNodeId, uint targetNodeId = 0)
        {
            CurrentNodeId = currentNodeId;
            TargetNodeId = targetNodeId;
            Status = PathfindingStatus.Idle;
            PathLength = 0;
            CurrentPathStep = 0;
            PathCost = 0.0f;
            LastPathfindTime = 0.0;
        }
    }

    /// <summary>
    /// Pathfinding status enumeration
    /// </summary>
    public enum PathfindingStatus : byte
    {
        Idle = 0,
        Searching = 1,
        PathFound = 2,
        NoPathFound = 3,
        TargetUnreachable = 4,
        InProgress = 5
    }

    /// <summary>
    /// Buffer element for storing pathfinding results
    /// Contains sequence of node IDs to follow
    /// </summary>
    public struct PathNodeBufferElement : IBufferElementData
    {
        public uint NodeId;
        public float TraversalCost;
        
        public static implicit operator PathNodeBufferElement(uint nodeId) => new() { NodeId = nodeId, TraversalCost = 1.0f };
    }

    /// <summary>
    /// System responsible for AI navigation and pathfinding with polarity constraints
    /// Handles both hard blocking and soft cost-based gate handling
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial struct AINavigationSystem : ISystem
    {
        private EntityQuery _navigationRequestQuery;
        private EntityQuery _navNodeQuery;

        public void OnCreate(ref SystemState state)
        {
            // Query for entities requesting pathfinding
            _navigationRequestQuery = SystemAPI.QueryBuilder()
                .WithAll<AINavigationState, AgentCapabilities>()
                .WithAny<PathNodeBufferElement>()
                .Build();

            // Query for navigation nodes
            _navNodeQuery = SystemAPI.QueryBuilder()
                .WithAll<NavNode, NavLinkBufferElement>()
                .Build();

            state.RequireForUpdate(_navigationRequestQuery);
            state.RequireForUpdate<NavigationGraph>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var navGraph = SystemAPI.GetSingleton<NavigationGraph>();
            if (!navGraph.IsReady)
                return;

            // Process navigation requests using manual EntityQuery
            if (_navigationRequestQuery.IsEmpty)
                return;
                
            var entities = _navigationRequestQuery.ToEntityArray(Allocator.Temp);
            var navStates = _navigationRequestQuery.ToComponentDataArray<AINavigationState>(Allocator.Temp);
            var capabilities = _navigationRequestQuery.ToComponentDataArray<AgentCapabilities>(Allocator.Temp);
            
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var nav = navStates[i];
                var caps = capabilities[i];
                
                // Skip if no target or already at target
                if (nav.TargetNodeId == 0 || nav.CurrentNodeId == nav.TargetNodeId)
                {
                    nav.Status = PathfindingStatus.Idle;
                    SystemAPI.SetComponent(entity, nav);
                    continue;
                }
                
                // Skip if currently pathfinding
                if (nav.Status == PathfindingStatus.Searching)
                    continue;

                // Start pathfinding
                nav.Status = PathfindingStatus.Searching;
                nav.LastPathfindTime = SystemAPI.Time.ElapsedTime;
                
                var pathfindingResult = FindPath(ref state, nav.CurrentNodeId, nav.TargetNodeId, caps);
                
                // Update navigation state based on result
                if (pathfindingResult.Success)
                {
                    nav.Status = PathfindingStatus.PathFound;
                    nav.PathLength = pathfindingResult.PathLength;
                    nav.PathCost = pathfindingResult.TotalCost;
                    nav.CurrentPathStep = 0;
                    
                    // Update path buffer if it exists
                    if (SystemAPI.HasBuffer<PathNodeBufferElement>(entity))
                    {
                        var pathBuffer = SystemAPI.GetBuffer<PathNodeBufferElement>(entity);
                        pathBuffer.Clear();
                        for (int j = 0; j < pathfindingResult.PathLength; j++)
                        {
                            pathBuffer.Add(new PathNodeBufferElement 
                            { 
                                NodeId = pathfindingResult.Path[j],
                                TraversalCost = j < pathfindingResult.PathLength - 1 ? 
                                               pathfindingResult.PathCosts[j] : 0.0f
                            });
                        }
                    }
                }
                else
                {
                    nav.Status = pathfindingResult.PathLength == 0 ? 
                                 PathfindingStatus.TargetUnreachable : 
                                 PathfindingStatus.NoPathFound;
                    nav.PathLength = 0;
                    nav.PathCost = float.MaxValue;
                    
                    if (SystemAPI.HasBuffer<PathNodeBufferElement>(entity))
                    {
                        var pathBuffer = SystemAPI.GetBuffer<PathNodeBufferElement>(entity);
                        pathBuffer.Clear();
                    }
                }
                
                // Update the component
                SystemAPI.SetComponent(entity, nav);

                // Dispose temporary path arrays
                if (pathfindingResult.Path.IsCreated)
                    pathfindingResult.Path.Dispose();
                if (pathfindingResult.PathCosts.IsCreated)
                    pathfindingResult.PathCosts.Dispose();
            }
            
            entities.Dispose();
            navStates.Dispose();
            capabilities.Dispose();
        }

        [BurstCompile]
        private PathfindingResult FindPath(ref SystemState state, uint startNodeId, uint targetNodeId, AgentCapabilities capabilities)
        {
            var maxNodes = 1000; // Reasonable limit for pathfinding
            
            // Initialize data structures for A* pathfinding
            var openSet = new NativeList<uint>(64, Allocator.Temp);
            var cameFrom = new NativeHashMap<uint, uint>(maxNodes, Allocator.Temp);
            var gScore = new NativeHashMap<uint, float>(maxNodes, Allocator.Temp);
            var fScore = new NativeHashMap<uint, float>(maxNodes, Allocator.Temp);
            var visited = new NativeHashSet<uint>(maxNodes, Allocator.Temp);

            var result = new PathfindingResult();

            try
            {
                // Initialize starting node
                openSet.Add(startNodeId);
                gScore[startNodeId] = 0.0f;
                fScore[startNodeId] = HeuristicCostEstimate(startNodeId, targetNodeId, ref state);

                while (openSet.Length > 0)
                {
                    // Find node with lowest fScore
                    var currentNodeId = GetLowestFScoreNode(openSet, fScore);
                    
                    if (currentNodeId == targetNodeId)
                    {
                        // Path found! Reconstruct it
                        result = ReconstructPath(cameFrom, gScore, currentNodeId, startNodeId);
                        break;
                    }

                    // Move current from open to closed set
                    openSet.RemoveAtSwapBack(openSet.IndexOf(currentNodeId));
                    visited.Add(currentNodeId);

                    // Process neighbors
                    var currentEntity = FindEntityByNodeId(ref state, currentNodeId);
                    if (currentEntity == Entity.Null || !SystemAPI.HasBuffer<NavLinkBufferElement>(currentEntity))
                        continue;

                    var linkBuffer = SystemAPI.GetBuffer<NavLinkBufferElement>(currentEntity);
                    for (int i = 0; i < linkBuffer.Length; i++)
                    {
                        var link = linkBuffer[i].Value;
                        var neighborId = link.GetDestination(currentNodeId);
                        
                        if (neighborId == 0 || visited.Contains(neighborId))
                            continue;

                        // Check if agent can traverse this link
                        if (!link.CanTraverseWith(capabilities, currentNodeId))
                            continue;

                        // Calculate arc-aware traversal cost using node positions
                        float traversalCost = CalculateArcAwareTraversalCost(link, capabilities, currentNodeId, neighborId, ref state);
                        var tentativeGScore = gScore[currentNodeId] + traversalCost;
                        
                        if (!openSet.Contains(neighborId))
                        {
                            openSet.Add(neighborId);
                        }
                        else if (gScore.ContainsKey(neighborId) && tentativeGScore >= gScore[neighborId])
                        {
                            continue; // Not a better path
                        }

                        // This path is the best so far
                        cameFrom[neighborId] = currentNodeId;
                        gScore[neighborId] = tentativeGScore;
                        fScore[neighborId] = tentativeGScore + HeuristicCostEstimate(neighborId, targetNodeId, ref state);
                    }
                }

                result.Success = result.PathLength > 0;
            }
            finally
            {
                // Cleanup
                openSet.Dispose();
                cameFrom.Dispose();
                gScore.Dispose();
                fScore.Dispose();
                visited.Dispose();
            }

            return result;
        }

        [BurstCompile]
        private uint GetLowestFScoreNode(NativeList<uint> openSet, NativeHashMap<uint, float> fScore)
        {
            var bestNodeId = openSet[0];
            var bestScore = fScore.ContainsKey(bestNodeId) ? fScore[bestNodeId] : float.MaxValue;
            
            for (int i = 1; i < openSet.Length; i++)
            {
                var nodeId = openSet[i];
                var score = fScore.ContainsKey(nodeId) ? fScore[nodeId] : float.MaxValue;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestNodeId = nodeId;
                }
            }
            
            return bestNodeId;
        }

        [BurstCompile]
        private float HeuristicCostEstimate(uint fromNodeId, uint toNodeId, ref SystemState state)
        {
            var fromEntity = FindEntityByNodeId(ref state, fromNodeId);
            var toEntity = FindEntityByNodeId(ref state, toNodeId);
            
            if (fromEntity == Entity.Null || toEntity == Entity.Null)
            {
                // Return maximum finite cost to indicate unreachable nodes
                // This allows A* to continue searching for alternative paths
                return float.MaxValue;
            }
                
            if (!SystemAPI.HasComponent<NavNode>(fromEntity) || !SystemAPI.HasComponent<NavNode>(toEntity))
            {
                // Invalid node components - treat as unreachable
                return float.MaxValue;
            }
                
            var fromNode = SystemAPI.GetComponent<NavNode>(fromEntity);
            var toNode = SystemAPI.GetComponent<NavNode>(toEntity);
            
            // Enhanced heuristic with arc trajectory and vertical movement considerations
            return CalculateMovementHeuristic(fromNode.WorldPosition, toNode.WorldPosition);
        }

        /// <summary>
        /// Calculate movement heuristic considering jump arcs and vertical traversal
        /// </summary>
        [BurstCompile]
        private float CalculateMovementHeuristic(float3 fromPos, float3 toPos)
        {
            float3 displacement = toPos - fromPos;
            float horizontalDistance = math.length(displacement.xz);
            float verticalDistance = displacement.y;
            
            // Base horizontal movement cost
            float baseCost = horizontalDistance;
            
            // Add vertical movement cost with arc trajectory considerations
            if (math.abs(verticalDistance) > 0.1f)
            {
                // Upward movement requires jump energy - use parabolic arc estimation
                if (verticalDistance > 0)
                {
                    // Arc trajectory cost: accounts for both horizontal and vertical components
                    // Uses physics-based parabolic arc calculation for jump estimation
                    float arcMultiplier = CalculateJumpArcMultiplier(horizontalDistance, verticalDistance);
                    baseCost *= arcMultiplier;
                }
                else
                {
                    // Downward movement is easier but still requires fall time
                    baseCost += math.abs(verticalDistance) * 0.3f;
                }
            }
            
            return baseCost;
        }

        /// <summary>
        /// Calculate jump arc multiplier based on horizontal and vertical distance
        /// Uses simplified parabolic trajectory physics for cost estimation
        /// </summary>
        [BurstCompile]
        private float CalculateJumpArcMultiplier(float horizontalDistance, float verticalDistance)
        {
            if (verticalDistance <= 0) return 1.0f;
            
            // Simplified jump physics constants (adjustable for game feel)
            const float gravity = 9.81f;
            const float baseJumpVelocity = 5.0f;
            const float maxJumpHeight = 3.0f;
            
            // Check if jump is physically possible with current abilities
            float requiredJumpHeight = verticalDistance;
            if (requiredJumpHeight > maxJumpHeight)
            {
                // Impossible jump - high cost penalty
                return 10.0f;
            }
            
            // Calculate time to reach peak and total arc time
            float timeToReachHeight = math.sqrt(2.0f * requiredJumpHeight / gravity);
            float requiredHorizontalVelocity = horizontalDistance / (timeToReachHeight * 2.0f);
            
            // Arc efficiency: ratio of required velocity to available velocity
            float velocityRatio = requiredHorizontalVelocity / baseJumpVelocity;
            
            // Cost increases quadratically with velocity requirement
            return 1.0f + (velocityRatio * velocityRatio * 2.0f);
        }

        /// <summary>
        /// Calculate arc-aware traversal cost using node positions for trajectory analysis
        /// </summary>
        [BurstCompile]
        private float CalculateArcAwareTraversalCost(NavLink link, AgentCapabilities capabilities, 
                                                    uint fromNodeId, uint toNodeId, ref SystemState state)
        {
            // Get base traversal cost
            float baseCost = link.CalculateTraversalCost(capabilities);
            
            // For jump-based movements, enhance with trajectory analysis
            if ((link.RequiredAbilities & (Ability.Jump | Ability.DoubleJump | Ability.WallJump | Ability.Dash |
                                          Ability.ArcJump | Ability.ChargedJump | Ability.TeleportArc | Ability.Grapple)) != 0)
            {
                var fromEntity = FindEntityByNodeId(ref state, fromNodeId);
                var toEntity = FindEntityByNodeId(ref state, toNodeId);
                
                if (fromEntity != Entity.Null && toEntity != Entity.Null &&
                    SystemAPI.HasComponent<NavNode>(fromEntity) && SystemAPI.HasComponent<NavNode>(toEntity))
                {
                    var fromNode = SystemAPI.GetComponent<NavNode>(fromEntity);
                    var toNode = SystemAPI.GetComponent<NavNode>(toEntity);
                    
                    // Use the enhanced NavLink arc calculation
                    return link.CalculateTraversalCost(capabilities, fromNode.WorldPosition, toNode.WorldPosition);
                }
            }
            
            return baseCost;
        }

        [BurstCompile]
        private Entity FindEntityByNodeId(ref SystemState state, uint nodeId)
        {
            // Use manual EntityQuery to avoid source generator compatibility issues
            var nodeQuery = SystemAPI.QueryBuilder()
                .WithAll<NodeId>()
                .Build();
            
            if (nodeQuery.IsEmpty)
                return Entity.Null;
                
            var entities = nodeQuery.ToEntityArray(Allocator.Temp);
            var nodeIds = nodeQuery.ToComponentDataArray<NodeId>(Allocator.Temp);
            
            for (int i = 0; i < entities.Length; i++)
            {
                if (nodeIds[i].Value == nodeId)
                {
                    var foundEntity = entities[i];
                    entities.Dispose();
                    nodeIds.Dispose();
                    return foundEntity;
                }
            }
            
            entities.Dispose();
            nodeIds.Dispose();
            return Entity.Null;
        }

        [BurstCompile]
        private PathfindingResult ReconstructPath(NativeHashMap<uint, uint> cameFrom, NativeHashMap<uint, float> gScore, uint currentNodeId, uint startNodeId)
        {
            var path = new NativeList<uint>(32, Allocator.Temp);
            var pathCosts = new NativeList<float>(32, Allocator.Temp);
            
            var nodeId = currentNodeId;
            while (nodeId != startNodeId && cameFrom.ContainsKey(nodeId))
            {
                path.Add(nodeId);
                pathCosts.Add(gScore.ContainsKey(nodeId) ? gScore[nodeId] : 1.0f);
                nodeId = cameFrom[nodeId];
            }
            path.Add(startNodeId); // Add start node
            pathCosts.Add(0.0f);
            
            // Reverse to get correct order (start to target)
            var finalPath = new NativeArray<uint>(path.Length, Allocator.Temp);
            var finalCosts = new NativeArray<float>(path.Length, Allocator.Temp);
            
            for (int i = 0; i < path.Length; i++)
            {
                finalPath[i] = path[path.Length - 1 - i];
                finalCosts[i] = pathCosts[path.Length - 1 - i];
            }
            
            var totalCost = gScore.ContainsKey(currentNodeId) ? gScore[currentNodeId] : 0.0f;
            
            path.Dispose();
            pathCosts.Dispose();
            
            return new PathfindingResult
            {
                Success = true,
                PathLength = finalPath.Length,
                TotalCost = totalCost,
                Path = finalPath,
                PathCosts = finalCosts
            };
        }

        private struct PathfindingResult
        {
            public bool Success;
            public int PathLength;
            public float TotalCost;
            public NativeArray<uint> Path;
            public NativeArray<float> PathCosts;
        }
    }
}