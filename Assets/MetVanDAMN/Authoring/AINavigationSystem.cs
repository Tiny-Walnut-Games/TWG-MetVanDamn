using TinyWalnutGames.MetVD.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

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
	/// Pathfinding result structure
	/// </summary>
	public struct PathfindingResult
		{
		public bool Success;
		public int PathLength;
		public float TotalCost;
		public NativeArray<uint> Path;
		public NativeArray<float> PathCosts;
		}

	/// <summary>
	/// Buffer element for navigation links
	/// </summary>
	public struct NavLinkBufferElement : IBufferElementData
		{
		public NavLink Value;

		public static implicit operator NavLink(NavLinkBufferElement e) => e.Value;
		public static implicit operator NavLinkBufferElement(NavLink e) => new() { Value = e };
		}

	/// <summary>
	/// System responsible for AI navigation and pathfinding with polarity constraints
	/// Handles both hard blocking and soft cost-based gate handling
	/// </summary>
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[RequireMatchingQueriesForUpdate]
	public partial class AINavigationSystem : SystemBase
		{
		private EntityQuery _navigationRequestQuery;

		protected override void OnCreate()
			{
			// Query for entities requesting pathfinding
			_navigationRequestQuery = GetEntityQuery(
				ComponentType.ReadWrite<AINavigationState>(),
				ComponentType.ReadOnly<AgentCapabilities>(),
				ComponentType.ReadWrite<PathNodeBufferElement>()
			);

			RequireForUpdate(_navigationRequestQuery);
			RequireForUpdate<NavigationGraph>();
			}

		protected override void OnUpdate()
			{
			NavigationGraph navGraph = SystemAPI.GetSingleton<NavigationGraph>();
			if (!navGraph.IsReady)
				{
				return;
				}

			double currentTime = SystemAPI.Time.ElapsedTime;

			Entities
				.WithoutBurst() // Required for SystemAPI calls and complex pathfinding
				.ForEach((Entity entity, ref AINavigationState navState, in AgentCapabilities capabilities) =>
				{
					DynamicBuffer<PathNodeBufferElement> pathBuffer = SystemAPI.GetBuffer<PathNodeBufferElement>(entity);

					// Skip if no target or already at target
					if (navState.TargetNodeId == 0 || navState.CurrentNodeId == navState.TargetNodeId)
						{
						navState.Status = PathfindingStatus.Idle;
						return;
						}

					// Skip if currently pathfinding
					if (navState.Status == PathfindingStatus.Searching)
						{
						return;
						}

					// Start pathfinding
					navState.Status = PathfindingStatus.Searching;
					navState.LastPathfindTime = currentTime;

					// Perform pathfinding with proper method call
					PathfindingResult pathfindingResult = PerformPathfinding(navState.CurrentNodeId, navState.TargetNodeId, capabilities);

					// Update navigation state based on result
					if (pathfindingResult.Success)
						{
						navState.Status = PathfindingStatus.PathFound;
						navState.PathLength = pathfindingResult.PathLength;
						navState.PathCost = pathfindingResult.TotalCost;
						navState.CurrentPathStep = 0;

						// Clear existing path and add new one
						pathBuffer.Clear();
						for (int i = 0; i < pathfindingResult.PathLength; i++)
							{
							pathBuffer.Add(new PathNodeBufferElement
								{
								NodeId = pathfindingResult.Path [ i ],
								TraversalCost = i < pathfindingResult.PathLength - 1 ?
											   pathfindingResult.PathCosts [ i ] : 0.0f
								});
							}
						}
					else
						{
						navState.Status = pathfindingResult.PathLength == 0 ?
										 PathfindingStatus.TargetUnreachable :
										 PathfindingStatus.NoPathFound;
						navState.PathLength = 0;
						navState.PathCost = float.MaxValue;
						pathBuffer.Clear();
						}

					// Dispose temporary path arrays
					if (pathfindingResult.Path.IsCreated)
						{
						pathfindingResult.Path.Dispose();
						}

					if (pathfindingResult.PathCosts.IsCreated)
						{
						pathfindingResult.PathCosts.Dispose();
						}
				}).Run(); // ✅ CRITICAL: Added .Run() to execute the ForEach
			}

		/// <summary>
		/// Performs pathfinding using A* algorithm with arc-aware cost calculation
		/// </summary>
		private PathfindingResult PerformPathfinding(uint startNodeId, uint targetNodeId, AgentCapabilities capabilities)
			{
			int maxNodes = 1000;

			// Initialize A* data structures
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
				gScore [ startNodeId ] = 0.0f;
				fScore [ startNodeId ] = CalculateHeuristicCostEstimate(startNodeId, targetNodeId);

				while (openSet.Length > 0)
					{
					uint currentNodeId = GetLowestFScoreNodeValue(openSet, fScore);

					if (currentNodeId == targetNodeId)
						{
						result = ReconstructPathResult(cameFrom, gScore, currentNodeId, startNodeId);
						break;
						}

					openSet.RemoveAtSwapBack(openSet.IndexOf(currentNodeId));
					visited.Add(currentNodeId);

					// Process neighbors
					Entity currentEntity = FindEntityByNodeIdValue(currentNodeId);
					if (currentEntity == Entity.Null || !SystemAPI.HasBuffer<NavLinkBufferElement>(currentEntity))
						{
						continue;
						}

					DynamicBuffer<NavLinkBufferElement> linkBuffer = SystemAPI.GetBuffer<NavLinkBufferElement>(currentEntity);
					for (int i = 0; i < linkBuffer.Length; i++)
						{
						NavLink link = linkBuffer [ i ].Value;
						uint neighborId = link.GetDestination(currentNodeId);

						if (neighborId == 0 || visited.Contains(neighborId))
							{
							continue;
							}

						if (!link.CanTraverseWith(capabilities, currentNodeId))
							{
							continue;
							}

						float traversalCost = CalculateArcAwareTraversalCostValue(link, capabilities, currentNodeId, neighborId);
						float tentativeGScore = gScore [ currentNodeId ] + traversalCost;

						if (!openSet.Contains(neighborId))
							{
							openSet.Add(neighborId);
							}
						else if (gScore.ContainsKey(neighborId) && tentativeGScore >= gScore [ neighborId ])
							{
							continue;
							}

						cameFrom [ neighborId ] = currentNodeId;
						gScore [ neighborId ] = tentativeGScore;
						fScore [ neighborId ] = tentativeGScore + CalculateHeuristicCostEstimate(neighborId, targetNodeId);
						}
					}

				result.Success = result.PathLength > 0;
				}
			finally
				{
				openSet.Dispose();
				cameFrom.Dispose();
				gScore.Dispose();
				fScore.Dispose();
				visited.Dispose();
				}

			return result;
			}

		private uint GetLowestFScoreNodeValue(NativeList<uint> openSet, NativeHashMap<uint, float> fScore)
			{
			uint bestNodeId = openSet [ 0 ];
			float bestScore = fScore.ContainsKey(bestNodeId) ? fScore [ bestNodeId ] : float.MaxValue;

			for (int i = 1; i < openSet.Length; i++)
				{
				uint nodeId = openSet [ i ];
				float score = fScore.ContainsKey(nodeId) ? fScore [ nodeId ] : float.MaxValue;
				if (score < bestScore)
					{
					bestScore = score;
					bestNodeId = nodeId;
					}
				}

			return bestNodeId;
			}

		private float CalculateHeuristicCostEstimate(uint fromNodeId, uint toNodeId)
			{
			Entity fromEntity = FindEntityByNodeIdValue(fromNodeId);
			Entity toEntity = FindEntityByNodeIdValue(toNodeId);

			if (fromEntity == Entity.Null || toEntity == Entity.Null)
				{
				return float.MaxValue;
				}

			if (!SystemAPI.HasComponent<NavNode>(fromEntity) || !SystemAPI.HasComponent<NavNode>(toEntity))
				{
				return float.MaxValue;
				}

			NavNode fromNode = SystemAPI.GetComponent<NavNode>(fromEntity);
			NavNode toNode = SystemAPI.GetComponent<NavNode>(toEntity);

			// ✅ FIXED: Added the missing CalculateMovementHeuristic method
			return CalculateMovementHeuristic(fromNode.WorldPosition, toNode.WorldPosition);
			}

		/// <summary>
		/// ✅ ADDED: Calculate movement heuristic considering jump arcs and vertical traversal
		/// </summary>
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
		/// ✅ ADDED: Calculate jump arc multiplier based on horizontal and vertical distance
		/// Uses simplified parabolic trajectory physics for cost estimation
		/// </summary>
		private float CalculateJumpArcMultiplier(float horizontalDistance, float verticalDistance)
			{
			if (verticalDistance <= 0)
				{
				return 1.0f;
				}

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

		private Entity FindEntityByNodeIdValue(uint nodeId)
			{
			Entity foundEntity = Entity.Null;

			Entities.ForEach((Entity entity, in NodeId id) =>
			{
				if (id._value == nodeId)
					{
					foundEntity = entity;
					}
			}).WithoutBurst().Run();

			return foundEntity;
			}

		private float CalculateArcAwareTraversalCostValue(NavLink link, AgentCapabilities capabilities,
														 uint fromNodeId, uint toNodeId)
			{
			// Get base traversal cost
			float baseCost = link.CalculateTraversalCost(capabilities);

			// For jump-based movements, enhance with trajectory analysis
			if ((link.RequiredAbilities & (Ability.Jump | Ability.DoubleJump | Ability.WallJump | Ability.Dash |
										  Ability.ArcJump | Ability.ChargedJump | Ability.TeleportArc | Ability.Grapple)) != 0)
				{
				Entity fromEntity = FindEntityByNodeIdValue(fromNodeId);
				Entity toEntity = FindEntityByNodeIdValue(toNodeId);

				if (fromEntity != Entity.Null && toEntity != Entity.Null &&
					SystemAPI.HasComponent<NavNode>(fromEntity) && SystemAPI.HasComponent<NavNode>(toEntity))
					{
					NavNode fromNode = SystemAPI.GetComponent<NavNode>(fromEntity);
					NavNode toNode = SystemAPI.GetComponent<NavNode>(toEntity);

					// Use the enhanced NavLink arc calculation
					return link.CalculateTraversalCost(capabilities, fromNode.WorldPosition, toNode.WorldPosition);
					}
				}

			return baseCost;
			}

		private PathfindingResult ReconstructPathResult(NativeHashMap<uint, uint> cameFrom, NativeHashMap<uint, float> gScore, uint currentNodeId, uint startNodeId)
			{
			var path = new NativeList<uint>(32, Allocator.Temp);
			var pathCosts = new NativeList<float>(32, Allocator.Temp);

			uint nodeId = currentNodeId;
			while (nodeId != startNodeId && cameFrom.ContainsKey(nodeId))
				{
				path.Add(nodeId);
				pathCosts.Add(gScore.ContainsKey(nodeId) ? gScore [ nodeId ] : 1.0f);
				nodeId = cameFrom [ nodeId ];
				}
			path.Add(startNodeId); // Add start node
			pathCosts.Add(0.0f);

			// Reverse to get correct order (start to target)
			var finalPath = new NativeArray<uint>(path.Length, Allocator.Temp);
			var finalCosts = new NativeArray<float>(path.Length, Allocator.Temp);

			for (int i = 0; i < path.Length; i++)
				{
				finalPath [ i ] = path [ path.Length - 1 - i ];
				finalCosts [ i ] = pathCosts [ path.Length - 1 - i ];
				}

			float totalCost = gScore.ContainsKey(currentNodeId) ? gScore [ currentNodeId ] : 0.0f;

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
		}
	}
