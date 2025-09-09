using TinyWalnutGames.MetVD.Core;
using Unity.Collections;
using Unity.Entities;
using NavLinkBufferElement = TinyWalnutGames.MetVD.Core.NavLinkBufferElement;

namespace TinyWalnutGames.MetVD.Authoring
	{
	/// <summary>
	/// System responsible for validating navigation graph connectivity and identifying unreachable areas
	/// Integrates with AuthoringValidator to provide comprehensive reachability analysis
	/// </summary>
	[UpdateInGroup(typeof(InitializationSystemGroup))]
	[UpdateAfter(typeof(NavigationGraphBuildSystem))]
	public partial class NavigationValidationSystem : SystemBase
		{
		private EntityQuery _navNodeQuery;
		private EntityQuery _navigationGraphQuery;

		protected override void OnCreate()
			{
			_navNodeQuery = GetEntityQuery(
				ComponentType.ReadOnly<NavNode>(),
				ComponentType.ReadOnly<NavLinkBufferElement>()
			);

			_navigationGraphQuery = GetEntityQuery(
				ComponentType.ReadOnly<NavigationGraph>()
			);

			RequireForUpdate(_navNodeQuery);
			RequireForUpdate(_navigationGraphQuery);
			}

		protected override void OnUpdate()
			{
			NavigationGraph navGraph = SystemAPI.GetSingleton<NavigationGraph>();
			if (!navGraph.IsReady)
				{
				return;
				}

			// Perform reachability analysis with different agent capability sets
			int unreachableCount = PerformReachabilityAnalysis();

			// Update navigation graph state
			SystemAPI.SetSingleton(navGraph);

			// Log validation results for debugging
			if (unreachableCount > 0)
				{
				UnityEngine.Debug.LogWarning($"Navigation validation found {unreachableCount} unreachable areas");
				}
			}

		private int PerformReachabilityAnalysis()
			{
			int unreachableCount = 0;

			// Test reachability with different agent capability profiles
			NativeArray<AgentCapabilities> testCapabilities = GetTestCapabilityProfiles();

			for (int profileIndex = 0; profileIndex < testCapabilities.Length; profileIndex++)
				{
				AgentCapabilities capabilities = testCapabilities[profileIndex];
				NativeArray<bool> reachabilityResults = AnalyzeReachability(capabilities);

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

		private NativeArray<AgentCapabilities> GetTestCapabilityProfiles()
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

		private NativeArray<bool> AnalyzeReachability(AgentCapabilities capabilities)
			{
			// Collect all navigation nodes
			var nodeIds = new NativeList<uint>(256, Allocator.Temp);

			Entities.WithAll<NavNode>().ForEach((in NavNode navNode) =>
			{
				if (navNode.IsActive)
					{
					nodeIds.Add(navNode.NodeId);
					}
			}).WithoutBurst().Run();

			var reachability = new NativeArray<bool>(nodeIds.Length, Allocator.Temp);

			if (nodeIds.Length == 0)
				{
				nodeIds.Dispose();
				return reachability;
				}

			// Start from first node and see what we can reach
			uint startNodeId = nodeIds[0];
			NativeHashSet<uint> reachableNodes = FloodFillReachability(startNodeId, capabilities);

			// Mark reachable nodes
			for (int i = 0; i < nodeIds.Length; i++)
				{
				reachability[i] = reachableNodes.Contains(nodeIds[i]);
				}

			nodeIds.Dispose();
			reachableNodes.Dispose();

			return reachability;
			}

		private NativeHashSet<uint> FloodFillReachability(uint startNodeId, AgentCapabilities capabilities)
			{
			var reachableNodes = new NativeHashSet<uint>(1000, Allocator.Temp);
			var queue = new NativeQueue<uint>(Allocator.Temp);

			queue.Enqueue(startNodeId);
			reachableNodes.Add(startNodeId);

			while (queue.Count > 0)
				{
				uint currentNodeId = queue.Dequeue();
				Entity currentEntity = FindEntityByNodeId(currentNodeId);

				if (currentEntity == Entity.Null || !SystemAPI.HasBuffer<NavLinkBufferElement>(currentEntity))
					{
					continue;
					}

				DynamicBuffer<NavLinkBufferElement> linkBuffer = SystemAPI.GetBuffer<NavLinkBufferElement>(currentEntity);

				for (int i = 0; i < linkBuffer.Length; i++)
					{
					NavLink link = linkBuffer[i].Value;
					uint neighborId = link.GetDestination(currentNodeId);

					if (neighborId == 0 || reachableNodes.Contains(neighborId))
						{
						continue;
						}

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

		private Entity FindEntityByNodeId(uint nodeId)
			{
			Entity foundEntity = Entity.Null;

			Entities.WithAll<NodeId>().ForEach((Entity entity, in NodeId id) =>
			{
				if (id._value == nodeId)
					{
					foundEntity = entity;
					}
			}).WithoutBurst().Run();

			return foundEntity;
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
				{
				UnreachableNodeIds.Dispose();
				}

			if (Issues.IsCreated)
				{
				Issues.Dispose();
				}
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
			EntityManager em = world.EntityManager;
			EntityQuery navGraphQuery = em.CreateEntityQuery(ComponentType.ReadOnly<NavigationGraph>());


			if (navGraphQuery.IsEmpty)
				{
				return new NavigationValidationReport(0, 0);
				}

			NavigationGraph navGraph = navGraphQuery.GetSingleton<NavigationGraph>();
			var report = new NavigationValidationReport(navGraph.NodeCount, navGraph.LinkCount);

			// Perform comprehensive validation using the system's analysis results
			EntityQuery nodeQuery = em.CreateEntityQuery(ComponentType.ReadOnly<NavNode>(), ComponentType.ReadOnly<NavLinkBufferElement>());

			// Collect all navigation nodes for analysis
			NativeArray<Entity> allNodes = nodeQuery.ToEntityArray(Allocator.Temp);
			report.TotalNodes = allNodes.Length;

			// Test reachability with multiple agent capability profiles
			NativeArray<AgentCapabilities> testCapabilities = GetTestCapabilityProfiles();
			var unreachableNodeIds = new NativeHashSet<uint>(allNodes.Length, Allocator.Temp);

			for (int profileIndex = 0; profileIndex < testCapabilities.Length; profileIndex++)
				{
				AgentCapabilities capabilities = testCapabilities[profileIndex];
				NativeHashSet<uint> reachableFromStart = PerformReachabilityAnalysis(world, capabilities, allNodes);

				// Mark unreachable nodes for this capability profile
				for (int nodeIndex = 0; nodeIndex < allNodes.Length; nodeIndex++)
					{
					Entity nodeEntity = allNodes[nodeIndex];
					if (em.HasComponent<NavNode>(nodeEntity))
						{
						NavNode navNode = em.GetComponentData<NavNode>(nodeEntity);
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
			foreach (uint nodeId in unreachableNodeIds)
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
			var reachableNodes = new NativeHashSet<uint>(allNodes.Length, Allocator.Temp);
			EntityManager entityManager = world.EntityManager;

			if (allNodes.Length == 0)
				{
				return reachableNodes;
				}

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
				{
				return reachableNodes;
				}

			// Flood fill algorithm to find all reachable nodes
			var queue = new NativeQueue<uint>(Allocator.Temp);
			queue.Enqueue(startNodeId);
			reachableNodes.Add(startNodeId);

			while (queue.Count > 0)
				{
				uint currentNodeId = queue.Dequeue();
				Entity currentEntity = FindEntityByNodeId(world, currentNodeId);

				if (currentEntity == Entity.Null || !entityManager.HasBuffer<NavLinkBufferElement>(currentEntity))
					{
					continue;
					}

				DynamicBuffer<NavLinkBufferElement> linkBuffer = entityManager.GetBuffer<NavLinkBufferElement>(currentEntity);

				for (int i = 0; i < linkBuffer.Length; i++)
					{
					NavLink link = linkBuffer[i].Value;
					uint neighborId = link.GetDestination(currentNodeId);

					if (neighborId == 0 || reachableNodes.Contains(neighborId))
						{
						continue;
						}

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
		/// Fixed to use more reliable entity lookup approach
		/// </summary>
		private static Entity FindEntityByNodeId(World world, uint nodeId)
			{
			EntityManager entityManager = world.EntityManager;
			EntityQuery nodeQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<NodeId>());

			// Use direct iteration instead of ToEntityArray to avoid potential issues
			Entity foundEntity = Entity.Null;

			using (var entities = nodeQuery.ToEntityArray(Allocator.Temp))
				{
				for (int i = 0; i < entities.Length; i++)
					{
					NodeId id = entityManager.GetComponentData<NodeId>(entities[i]);
					if (id._value == nodeId)
						{
						foundEntity = entities[i];
						break;
						}
					}
				}

			return foundEntity;
			}

		/// <summary>
		/// Get predefined test capability profiles for validation
		/// </summary>
		private static NativeArray<AgentCapabilities> GetTestCapabilityProfiles()
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

		/// <summary>
		/// Check if a specific path is possible with given capabilities
		/// Useful for editor validation and debugging
		/// </summary>
		public static bool IsPathPossible(World world, uint fromNodeId, uint toNodeId, AgentCapabilities capabilities)
			{
			if (world == null)
				{
				return false;
				}

			EntityManager entityManager = world.EntityManager;
			if (entityManager == null)
				{
				return false;
				}

			// Quick check: if source and destination are the same, path is always possible
			if (fromNodeId == toNodeId)
				{
				return true;
				}

			// Find source entity
			Entity sourceEntity = FindEntityByNodeId(world, fromNodeId);
			if (sourceEntity == Entity.Null)
				{
				return false;
				}

			// Find destination entity
			Entity destinationEntity = FindEntityByNodeId(world, toNodeId);
			if (destinationEntity == Entity.Null)
				{
				return false;
				}

			// Perform flood fill pathfinding from source to destination
			var reachableNodes = new NativeHashSet<uint>(1000, Allocator.Temp);
			var queue = new NativeQueue<uint>(Allocator.Temp);

			try
				{
				// Start flood fill from source node
				queue.Enqueue(fromNodeId);
				reachableNodes.Add(fromNodeId);

				while (queue.Count > 0)
					{
					uint currentNodeId = queue.Dequeue();

					// Check if we've reached the destination
					if (currentNodeId == toNodeId)
						{
						return true; // Path found!
						}

					// Find current entity and explore its neighbors
					Entity currentEntity = FindEntityByNodeId(world, currentNodeId);
					if (currentEntity == Entity.Null || !entityManager.HasBuffer<NavLinkBufferElement>(currentEntity))
						{
						continue;
						}

					DynamicBuffer<NavLinkBufferElement> linkBuffer = entityManager.GetBuffer<NavLinkBufferElement>(currentEntity);

					for (int i = 0; i < linkBuffer.Length; i++)
						{
						NavLink link = linkBuffer[i].Value;
						uint neighborId = link.GetDestination(currentNodeId);

						if (neighborId == 0 || reachableNodes.Contains(neighborId))
							{
							continue;
							}

						// 🔥 CRITICAL: Check if agent can traverse this link with their capabilities
						if (link.CanTraverseWith(capabilities, currentNodeId))
							{
							reachableNodes.Add(neighborId);
							queue.Enqueue(neighborId);
							}
						}
					}

				// If we exit the loop without finding the destination, no path exists
				return false;
				}
			finally
				{
				// Always clean up native collections
				if (reachableNodes.IsCreated)
					{
					reachableNodes.Dispose();
					}

				if (queue.IsCreated)
					{
					queue.Dispose();
					}
				}
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
				NavigationIssue issue = report.Issues[i];
				if (issue.Type == NavigationIssueType.UnreachableNode)
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
