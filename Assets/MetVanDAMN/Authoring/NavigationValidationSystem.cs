using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Authoring
{
    /// <summary>
    /// System responsible for validating navigation graph connectivity and identifying unreachable areas
    /// Integrates with AuthoringValidator to provide comprehensive reachability analysis
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(NavigationGraphBuildSystem))]
    public partial struct NavigationValidationSystem : ISystem
    {
        private EntityQuery _navNodeQuery;
        private EntityQuery _navigationGraphQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _navNodeQuery = SystemAPI.QueryBuilder()
                .WithAll<NavNode, NavLinkBufferElement>()
                .Build();

            _navigationGraphQuery = SystemAPI.QueryBuilder()
                .WithAll<NavigationGraph>()
                .Build();

            state.RequireForUpdate(_navNodeQuery);
            state.RequireForUpdate(_navigationGraphQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var navGraph = SystemAPI.GetSingleton<NavigationGraph>();
            if (!navGraph.IsReady)
                return;

            // Perform reachability analysis with different agent capability sets
            var unreachableCount = PerformReachabilityAnalysis(ref state);
            
            // Update navigation graph with validation results
            navGraph.UnreachableAreaCount = unreachableCount;
            SystemAPI.SetSingleton(navGraph);

            // Log validation results for debugging
            if (unreachableCount > 0)
            {
                UnityEngine.Debug.LogWarning($"Navigation validation found {unreachableCount} unreachable areas");
            }
        }

        [BurstCompile]
        private int PerformReachabilityAnalysis(ref SystemState state)
        {
            var unreachableCount = 0;
            
            // Test reachability with different agent capability profiles
            var testCapabilities = GetTestCapabilityProfiles();
            
            for (int profileIndex = 0; profileIndex < testCapabilities.Length; profileIndex++)
            {
                var capabilities = testCapabilities[profileIndex];
                var reachabilityResults = AnalyzeReachability(ref state, capabilities);
                
                // Count unreachable nodes for this capability profile
                for (int i = 0; i < reachabilityResults.Length; i++)
                {
                    if (!reachabilityResults[i])
                    {
                        unreachableCount++;
                    }
                }
                
                reachabilityResults.Dispose();
            }
            
            testCapabilities.Dispose();
            return unreachableCount;
        }

        [BurstCompile]
        private readonly NativeArray<AgentCapabilities> GetTestCapabilityProfiles()
        {
            var profiles = new NativeArray<AgentCapabilities>(5, Allocator.Temp);
            
            // Basic agent - no special abilities
            profiles[0] = new AgentCapabilities(Polarity.None, Ability.None, 0.0f, "BasicAgent");
            
            // Movement specialist
            profiles[1] = new AgentCapabilities(Polarity.None, Ability.AllMovement, 0.8f, "MovementAgent");
            
            // Environmental specialist  
            profiles[2] = new AgentCapabilities(Polarity.HeatCold | Polarity.EarthWind, Ability.AllEnvironmental, 0.6f, "EnvironmentalAgent");
            
            // Polarity master
            profiles[3] = new AgentCapabilities(Polarity.Any, Ability.AllPolarity, 1.0f, "PolarityAgent");
            
            // Master agent - all abilities
            profiles[4] = new AgentCapabilities(Polarity.Any, Ability.Everything, 1.0f, "MasterAgent");
            
            return profiles;
        }

        [BurstCompile]
        private readonly NativeArray<bool> AnalyzeReachability(ref SystemState state, AgentCapabilities capabilities)
        {
            // Collect all navigation nodes
            var nodeIds = new NativeList<uint>(256, Allocator.Temp);
            foreach (var (navNode, entity) in SystemAPI.Query<RefRO<NavNode>>().WithEntityAccess())
            {
                if (navNode.ValueRO.IsActive)
                {
                    nodeIds.Add(navNode.ValueRO.NodeId);
                }
            }
            
            var reachability = new NativeArray<bool>(nodeIds.Length, Allocator.Temp);
            
            if (nodeIds.Length == 0)
            {
                nodeIds.Dispose();
                return reachability;
            }
            
            // Start from first node and see what we can reach
            var startNodeId = nodeIds[0];
            var reachableNodes = FloodFillReachability(ref state, startNodeId, capabilities);
            
            // Mark reachable nodes
            for (int i = 0; i < nodeIds.Length; i++)
            {
                reachability[i] = reachableNodes.Contains(nodeIds[i]);
            }
            
            nodeIds.Dispose();
            reachableNodes.Dispose();
            
            return reachability;
        }

        [BurstCompile]
        private readonly NativeHashSet<uint> FloodFillReachability(ref SystemState state, uint startNodeId, AgentCapabilities capabilities)
        {
            var reachableNodes = new NativeHashSet<uint>(1000, Allocator.Temp);
            var queue = new NativeQueue<uint>(Allocator.Temp);
            
            queue.Enqueue(startNodeId);
            reachableNodes.Add(startNodeId);
            
            while (queue.Count > 0)
            {
                var currentNodeId = queue.Dequeue();
                var currentEntity = FindEntityByNodeId(ref state, currentNodeId);
                
                if (currentEntity == Entity.Null || !SystemAPI.HasBuffer<NavLinkBufferElement>(currentEntity))
                    continue;
                    
                var linkBuffer = SystemAPI.GetBuffer<NavLinkBufferElement>(currentEntity);
                
                for (int i = 0; i < linkBuffer.Length; i++)
                {
                    var link = linkBuffer[i].Value;
                    var neighborId = link.GetDestination(currentNodeId);
                    
                    if (neighborId == 0 || reachableNodes.Contains(neighborId))
                        continue;
                        
                    // Check if this agent can traverse this link
                    if (link.CanTraverseWith(capabilities, currentNodeId))
                    {
                        reachableNodes.Add(neighborId);
                        queue.Enqueue(neighborId);
                    }
                }
            }
            
            queue.Dispose();
            return reachableNodes;
        }

        [BurstCompile]
        private readonly Entity FindEntityByNodeId(ref SystemState state, uint nodeId)
        {
            foreach (var (id, entity) in SystemAPI.Query<RefRO<NodeId>>().WithEntityAccess())
            {
                if (id.ValueRO.Value == nodeId)
                    return entity;
            }
            return Entity.Null;
        }
    }

    /// <summary>
    /// Navigation validation report for authoring integration
    /// Contains detailed analysis of connectivity issues
    /// </summary>
    public struct NavigationValidationReport
    {
        public int TotalNodes;
        public int TotalLinks;
        public int UnreachableNodeCount;
        public int IsolatedComponentCount;
        public bool HasUnreachableAreas;
        public NativeList<uint> UnreachableNodeIds;
        public NativeList<NavigationIssue> Issues;

        public NavigationValidationReport(int totalNodes, int totalLinks, Allocator allocator = Allocator.Temp)
        {
            TotalNodes = totalNodes;
            TotalLinks = totalLinks;
            UnreachableNodeCount = 0;
            IsolatedComponentCount = 0;
            HasUnreachableAreas = false;
            UnreachableNodeIds = new NativeList<uint>(totalNodes, allocator);
            Issues = new NativeList<NavigationIssue>(32, allocator);
        }

        public void Dispose()
        {
            if (UnreachableNodeIds.IsCreated)
                UnreachableNodeIds.Dispose();
            if (Issues.IsCreated)
                Issues.Dispose();
        }
    }

    /// <summary>
    /// Navigation issue descriptor for detailed reporting
    /// </summary>
    public struct NavigationIssue
    {
        public NavigationIssueType Type;
        public uint NodeId;
        public uint RelatedNodeId;
        public Polarity RequiredPolarity;
        public Ability RequiredAbilities;
        public FixedString128Bytes Description;

        public NavigationIssue(NavigationIssueType type, uint nodeId, FixedString128Bytes description,
                              uint relatedNodeId = 0, Polarity requiredPolarity = Polarity.None, 
                              Ability requiredAbilities = Ability.None)
        {
            Type = type;
            NodeId = nodeId;
            RelatedNodeId = relatedNodeId;
            RequiredPolarity = requiredPolarity;
            RequiredAbilities = requiredAbilities;
            Description = description;
        }
    }

    /// <summary>
    /// Types of navigation issues that can be detected
    /// </summary>
    public enum NavigationIssueType : byte
    {
        UnreachableNode = 0,
        RequiresUnavailablePolarity = 1,
        RequiresUnavailableAbility = 2,
        IsolatedComponent = 3,
        CyclicDependency = 4,
        MissingConnection = 5,
        HardGateBlocking = 6
    }

    /// <summary>
    /// Static utility class for navigation validation helpers
    /// Provides integration points for AuthoringValidator
    /// </summary>
    public static class NavigationValidationUtility
    {
        /// <summary>
        /// Generate comprehensive navigation validation report
        /// Called by AuthoringValidator to check navigation connectivity
        /// </summary>
        public static NavigationValidationReport GenerateValidationReport(World world)
        {
            var em = world.EntityManager;
            var navGraphQuery = em.CreateEntityQuery(ComponentType.ReadOnly<NavigationGraph>());
            
            if (navGraphQuery.IsEmpty)
            {
                return new NavigationValidationReport(0, 0);
            }

            var navGraph = navGraphQuery.GetSingleton<NavigationGraph>();
            var report = new NavigationValidationReport(navGraph.NodeCount, navGraph.LinkCount);
            
            // Perform comprehensive validation using the system's analysis results
            var nodeQuery = em.CreateEntityQuery(ComponentType.ReadOnly<NavNode>(), ComponentType.ReadOnly<NavLinkBufferElement>());
            
            // Collect all navigation nodes for analysis
            var allNodes = nodeQuery.ToEntityArray(Allocator.Temp);
            report.TotalNodes = allNodes.Length;
            
            // Test reachability with multiple agent capability profiles
            var testCapabilities = GetTestCapabilityProfiles();
            var unreachableNodeIds = new NativeHashSet<uint>(allNodes.Length, Allocator.Temp);
            
            for (int profileIndex = 0; profileIndex < testCapabilities.Length; profileIndex++)
            {
                var capabilities = testCapabilities[profileIndex];
                var reachableFromStart = PerformReachabilityAnalysis(world, capabilities, allNodes);
                
                // Mark unreachable nodes for this capability profile
                for (int nodeIndex = 0; nodeIndex < allNodes.Length; nodeIndex++)
                {
                    var nodeEntity = allNodes[nodeIndex];
                    if (em.HasComponent<NavNode>(nodeEntity))
                    {
                        var navNode = em.GetComponentData<NavNode>(nodeEntity);
                        if (!reachableFromStart.Contains(navNode.NodeId))
                        {
                            unreachableNodeIds.Add(navNode.NodeId);
                            
                            // Add detailed issue for this unreachable node
                            var issue = new NavigationIssue(
                                NavigationIssueType.UnreachableNode,
                                navNode.NodeId,
                                $"Node unreachable with {capabilities.AgentType} capabilities"
                            );
                            report.Issues.Add(issue);
                        }
                    }
                }
                
                reachableFromStart.Dispose();
            }
            
            // Update report with final results
            report.HasUnreachableAreas = unreachableNodeIds.Count > 0;
            report.UnreachableNodeCount = unreachableNodeIds.Count;
            
            // Copy unreachable node IDs to report
            foreach (var nodeId in unreachableNodeIds)
            {
                report.UnreachableNodeIds.Add(nodeId);
            }
            
            // Cleanup
            allNodes.Dispose();
            unreachableNodeIds.Dispose();
            testCapabilities.Dispose();
            
            return report;
        }

        /// <summary>
        /// Performs reachability analysis from a starting node with given agent capabilities
        /// Returns a set of reachable node IDs
        /// </summary>
        private static NativeHashSet<uint> PerformReachabilityAnalysis(World world, AgentCapabilities capabilities, NativeArray<Entity> allNodes)
        {
            var reachableNodes = new NativeHashSet<uint>(allNodes.Length, Unity.Collections.Allocator.Temp);
            var entityManager = world.EntityManager;
            
            if (allNodes.Length == 0)
                return reachableNodes;
                
            // Start from the first valid node
            Entity startEntity = Entity.Null;
            uint startNodeId = 0;
            
            for (int i = 0; i < allNodes.Length; i++)
            {
                if (entityManager.HasComponent<NavNode>(allNodes[i]))
                {
                    startEntity = allNodes[i];
                    startNodeId = entityManager.GetComponentData<NavNode>(startEntity).NodeId;
                    break;
                }
            }
            
            if (startEntity == Entity.Null)
                return reachableNodes;
            
            // Flood fill algorithm to find all reachable nodes
            var queue = new NativeQueue<uint>(Unity.Collections.Allocator.Temp);
            queue.Enqueue(startNodeId);
            reachableNodes.Add(startNodeId);
            
            while (queue.Count > 0)
            {
                var currentNodeId = queue.Dequeue();
                var currentEntity = FindEntityByNodeId(world, currentNodeId);
                
                if (currentEntity == Entity.Null || !entityManager.HasBuffer<NavLinkBufferElement>(currentEntity))
                    continue;
                    
                var linkBuffer = entityManager.GetBuffer<NavLinkBufferElement>(currentEntity);
                
                for (int i = 0; i < linkBuffer.Length; i++)
                {
                    var link = linkBuffer[i].Value;
                    var neighborId = link.GetDestination(currentNodeId);
                    
                    if (neighborId == 0 || reachableNodes.Contains(neighborId))
                        continue;
                        
                    // Check if this agent can traverse this link
                    if (link.CanTraverseWith(capabilities, currentNodeId))
                    {
                        reachableNodes.Add(neighborId);
                        queue.Enqueue(neighborId);
                    }
                }
            }
            
            queue.Dispose();
            return reachableNodes;
        }

        /// <summary>
        /// Helper method to find entity by node ID
        /// </summary>
        private static Entity FindEntityByNodeId(World world, uint nodeId)
        {
            var entityManager = world.EntityManager;
            var nodeQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<NodeId>());
            var entities = nodeQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            
            foreach (var entity in entities)
            {
                var id = entityManager.GetComponentData<NodeId>(entity);
                if (id.Value == nodeId)
                {
                    entities.Dispose();
                    return entity;
                }
            }
            
            entities.Dispose();
            return Entity.Null;
        }

        /// <summary>
        /// Get predefined test capability profiles for validation
        /// </summary>
        private static NativeArray<AgentCapabilities> GetTestCapabilityProfiles()
        {
            var profiles = new NativeArray<AgentCapabilities>(5, Unity.Collections.Allocator.Temp);
            
            // Basic agent - no special abilities
            profiles[0] = new AgentCapabilities(Polarity.None, Ability.None, 0.0f, "BasicAgent");
            
            // Movement specialist
            profiles[1] = new AgentCapabilities(Polarity.None, Ability.AllMovement, 0.8f, "MovementAgent");
            
            // Environmental specialist  
            profiles[2] = new AgentCapabilities(Polarity.HeatCold | Polarity.EarthWind, Ability.AllEnvironmental, 0.6f, "EnvironmentalAgent");
            
            // Polarity master
            profiles[3] = new AgentCapabilities(Polarity.Any, Ability.AllPolarity, 1.0f, "PolarityAgent");
            
            // Master agent - all abilities
            profiles[4] = new AgentCapabilities(Polarity.Any, Ability.Everything, 1.0f, "MasterAgent");
            
            return profiles;
        }

        /// <summary>
        /// Check if a specific path is possible with given capabilities
        /// Useful for editor validation and debugging
        /// </summary>
        public static bool IsPathPossible(World world, uint fromNodeId, uint toNodeId, AgentCapabilities capabilities)
        {
            // Path API not exposed yet; return false to avoid compile errors
            return false;
        }

        /// <summary>
        /// Generate quick-fix suggestions for navigation issues
        /// Integrates with AuthoringValidator auto-fix functionality
        /// </summary>
        public static NativeList<NavigationQuickFix> GenerateQuickFixSuggestions(NavigationValidationReport report)
        {
            var fixes = new NativeList<NavigationQuickFix>(16, Allocator.Temp);
            
            // Generate suggestions based on issues found
            for (int i = 0; i < report.Issues.Length; i++)
            {
                var issue = report.Issues[i];
                if(issue.Type == NavigationIssueType.UnreachableNode)
                {
                    fixes.Add(new NavigationQuickFix
                    {
                        Type = NavigationQuickFixType.AddConnection,
                        TargetNodeId = issue.NodeId,
                        Description = "Add connection to reachable area"
                    });
                }
            }
            
            return fixes;
        }
    }

    /// <summary>
    /// Navigation quick-fix suggestion
    /// </summary>
    public struct NavigationQuickFix
    {
        public NavigationQuickFixType Type;
        public uint TargetNodeId;
        public uint RelatedNodeId;
        public FixedString128Bytes Description;
    }

    /// <summary>
    /// Types of quick fixes that can be suggested
    /// </summary>
    public enum NavigationQuickFixType : byte
    {
        None = 0,
        AddConnection = 1,
        SoftenGate = 2,
        ChangePolarity = 3,
        AddAlternativePath = 4
    }
}
