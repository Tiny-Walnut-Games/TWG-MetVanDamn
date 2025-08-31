using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Core
	{
	/// <summary>
	/// Navigation node representing a traversable location in the world
	/// Core component for AI pathfinding across districts, rooms, and sectors
	/// </summary>
	public struct NavNode : IComponentData
		{
		/// <summary>
		/// Unique identifier for this navigation node
		/// </summary>
		public uint NodeId;

		/// <summary>
		/// World position of this navigation node
		/// </summary>
		public float3 WorldPosition;

		/// <summary>
		/// Biome type this node belongs to
		/// </summary>
		public BiomeType BiomeType;

		/// <summary>
		/// Primary polarity field at this location
		/// </summary>
		public Polarity PrimaryPolarity;

		/// <summary>
		/// Whether this node is currently active for navigation
		/// </summary>
		public bool IsActive;

		/// <summary>
		/// Whether this node has been discovered by AI agents
		/// </summary>
		public bool IsDiscovered;

		/// <summary>
		/// Base navigation cost for pathfinding algorithms
		/// </summary>
		public float BaseTraversalCost;

		public NavNode (uint nodeId, float3 worldPosition, BiomeType biomeType = BiomeType.Unknown,
					  Polarity primaryPolarity = Polarity.None, float baseTraversalCost = 1.0f)
			{
			NodeId = nodeId;
			WorldPosition = worldPosition;
			BiomeType = biomeType;
			PrimaryPolarity = primaryPolarity;
			IsActive = true;
			IsDiscovered = false;
			BaseTraversalCost = math.max(0.1f, baseTraversalCost);
			}

		/// <summary>
		/// Check if this node is compatible with agent capabilities
		/// </summary>
		public readonly bool IsCompatibleWith (AgentCapabilities capabilities)
			{
			if (!IsActive)
				{
				return false;
				}

			// Check if agent can handle this node's polarity environment
			return PrimaryPolarity != Polarity.None && PrimaryPolarity != Polarity.Any
				? (capabilities.AvailablePolarity & PrimaryPolarity) != 0
				: true;
			}

		public override readonly string ToString ()
			{
			return $"NavNode({NodeId}, {BiomeType}, {PrimaryPolarity}, Active:{IsActive})";
			}
		}

	/// <summary>
	/// Navigation link between two nodes with traversal rules and gate conditions
	/// Represents connections, gates, and special traversal paths
	/// </summary>
	public struct NavLink : IComponentData
		{
		/// <summary>
		/// Source navigation node ID
		/// </summary>
		public uint FromNodeId;

		/// <summary>
		/// Destination navigation node ID
		/// </summary>
		public uint ToNodeId;

		/// <summary>
		/// Type of connection determining traversal rules
		/// </summary>
		public ConnectionType ConnectionType;

		/// <summary>
		/// Required polarity to traverse this link
		/// </summary>
		public Polarity RequiredPolarity;

		/// <summary>
		/// Required abilities to traverse this link
		/// </summary>
		public Ability RequiredAbilities;

		/// <summary>
		/// Base traversal cost for pathfinding
		/// </summary>
		public float BaseCost;

		/// <summary>
		/// Additional cost multiplier for polarity mismatch (soft gating)
		/// </summary>
		public float PolarityMismatchCostMultiplier;

		/// <summary>
		/// Whether this link is currently active/passable
		/// </summary>
		public bool IsActive;

		/// <summary>
		/// Whether this link has been discovered by AI agents
		/// </summary>
		public bool IsDiscovered;

		/// <summary>
		/// Gate softness for skill-based bypass possibility
		/// </summary>
		public GateSoftness GateSoftness;

		/// <summary>
		/// Description for debugging and UI
		/// </summary>
		public FixedString64Bytes Description;

		public NavLink (uint fromNodeId, uint toNodeId, ConnectionType connectionType = ConnectionType.Bidirectional,
					  Polarity requiredPolarity = Polarity.None, Ability requiredAbilities = Ability.None,
					  float baseCost = 1.0f, float polarityMismatchCostMultiplier = 5.0f,
					  GateSoftness gateSoftness = GateSoftness.Hard, FixedString64Bytes description = default)
			{
			FromNodeId = fromNodeId;
			ToNodeId = toNodeId;
			ConnectionType = connectionType;
			RequiredPolarity = requiredPolarity;
			RequiredAbilities = requiredAbilities;
			BaseCost = math.max(0.1f, baseCost);
			PolarityMismatchCostMultiplier = math.max(1.0f, polarityMismatchCostMultiplier);
			IsActive = true;
			IsDiscovered = false;
			GateSoftness = gateSoftness;
			Description = description;
			}

		/// <summary>
		/// Check if this link can be traversed by the agent with given capabilities
		/// </summary>
		public readonly bool CanTraverseWith (AgentCapabilities capabilities, uint fromNodeId)
			{
			if (!IsActive)
				{
				return false;
				}

			// Check if traversing from the correct node
			bool validDirection = (ConnectionType == ConnectionType.Bidirectional)
				? (fromNodeId == FromNodeId || fromNodeId == ToNodeId)
				: (fromNodeId == FromNodeId);

			if (!validDirection)
				{
				return false;
				}

			// For hard gates, strict requirement checking
			if (GateSoftness == GateSoftness.Hard)
				{
				// Check polarity requirements
				if (RequiredPolarity != Polarity.None && RequiredPolarity != Polarity.Any)
					{
					if ((capabilities.AvailablePolarity & RequiredPolarity) == 0)
						{
						return false;
						}
					}

				// Check ability requirements  
				if (RequiredAbilities != Ability.None)
					{
					if ((capabilities.AvailableAbilities & RequiredAbilities) != RequiredAbilities)
						{
						return false;
						}
					}
				}

			// For soft gates, always allow traversal but with cost penalty
			return true;
			}

		/// <summary>
		/// Calculate the effective traversal cost for this link given agent capabilities
		/// </summary>
		public readonly float CalculateTraversalCost (AgentCapabilities capabilities)
			{
			if (!IsActive)
				{
				return float.MaxValue;
				}

			float effectiveCost = BaseCost;

			// Apply polarity mismatch penalty for soft gates
			if (RequiredPolarity != Polarity.None && RequiredPolarity != Polarity.Any)
				{
				if ((capabilities.AvailablePolarity & RequiredPolarity) == 0)
					{
					effectiveCost *= PolarityMismatchCostMultiplier;
					}
				}

			// Apply ability mismatch penalty with jump-specific considerations
			if (RequiredAbilities != Ability.None)
				{
				if ((capabilities.AvailableAbilities & RequiredAbilities) != RequiredAbilities)
					{
					effectiveCost *= PolarityMismatchCostMultiplier;
					}
				else
					{
					// Apply movement efficiency bonuses for specific abilities
					effectiveCost *= CalculateMovementEfficiencyMultiplier(capabilities.AvailableAbilities);
					}
				}

			// Apply gate softness modifier
			float softnessMultiplier = GateSoftness switch
				{
					GateSoftness.Hard => 1.0f,
					GateSoftness.VeryDifficult => 1.2f,
					GateSoftness.Difficult => 1.5f,
					GateSoftness.Moderate => 2.0f,
					GateSoftness.Easy => 3.0f,
					GateSoftness.Trivial => 4.0f,
					_ => 1.0f
					};

			return effectiveCost * softnessMultiplier;
			}

		/// <summary>
		/// Calculate the effective traversal cost for this link with arc trajectory analysis
		/// </summary>
		public readonly float CalculateTraversalCost (AgentCapabilities capabilities, float3 fromPosition, float3 toPosition)
			{
			float baseCost = CalculateTraversalCost(capabilities);
			if (baseCost >= float.MaxValue)
				{
				return baseCost;
				}

			// Add arc trajectory cost for jump-based movements
			if (HasJumpMovementRequirement())
				{
				float arcCost = CalculateArcTraversalCost(fromPosition, toPosition, capabilities.AvailableAbilities);
				baseCost *= arcCost;
				}

			return baseCost;
			}

		/// <summary>
		/// Calculate movement efficiency multiplier based on available abilities
		/// </summary>
		private readonly float CalculateMovementEfficiencyMultiplier (Ability availableAbilities)
			{
			float multiplier = 1.0f;

			// Check for movement ability efficiency bonuses
			if ((RequiredAbilities & Ability.Jump) != 0 && (availableAbilities & Ability.DoubleJump) != 0)
				{
				multiplier *= 0.8f; // Double jump makes regular jumps more efficient
				}

			if ((RequiredAbilities & Ability.WallJump) != 0 && (availableAbilities & Ability.Climb) != 0)
				{
				multiplier *= 0.7f; // Climbing ability improves wall jump efficiency
				}

			if ((RequiredAbilities & Ability.Dash) != 0 && (availableAbilities & Ability.GlideSpeed) != 0)
				{
				multiplier *= 0.9f; // Glide extends dash effectiveness
				}

			// Arc-based ability synergies
			if ((RequiredAbilities & Ability.Jump) != 0 && (availableAbilities & Ability.ArcJump) != 0)
				{
				multiplier *= 0.75f; // Arc jump provides superior jump control
				}

			if ((RequiredAbilities & Ability.DoubleJump) != 0 && (availableAbilities & Ability.ChargedJump) != 0)
				{
				multiplier *= 0.85f; // Charged jump reduces double jump necessity
				}

			if ((RequiredAbilities & Ability.Grapple) != 0 && (availableAbilities & Ability.AllArcMovement) != 0)
				{
				multiplier *= 0.8f; // Arc movement abilities complement grappling
				}

			// Precision movement combinations
			if ((availableAbilities & Ability.ArcJump) != 0 && (availableAbilities & Ability.TeleportArc) != 0)
				{
				multiplier *= 0.9f; // Advanced arc abilities work together
				}

			return multiplier;
			}

		/// <summary>
		/// Check if this link requires jump-based movement
		/// </summary>
		private readonly bool HasJumpMovementRequirement ()
			{
			return (RequiredAbilities & (Ability.Jump | Ability.DoubleJump | Ability.WallJump | Ability.Dash |
										Ability.ArcJump | Ability.ChargedJump | Ability.TeleportArc | Ability.Grapple)) != 0;
			}

		/// <summary>
		/// Calculate arc traversal cost based on trajectory physics
		/// </summary>
		private readonly float CalculateArcTraversalCost (float3 fromPos, float3 toPos, Ability availableAbilities)
			{
			float3 displacement = toPos - fromPos;
			float horizontalDistance = math.length(displacement.xz);
			float verticalDistance = displacement.y;

			// Base arc difficulty
			float arcMultiplier = 1.0f;

			if (verticalDistance > 0.1f) // Upward movement
				{
				// Calculate jump arc requirements with enhanced ability support
				const float maxSingleJumpHeight = 2.0f;
				const float maxDoubleJumpHeight = 4.0f;
				const float maxWallJumpReach = 3.0f;
				const float maxArcJumpHeight = 6.0f;        // Enhanced arc control
				const float maxChargedJumpHeight = 8.0f;    // Variable jump power
				const float maxTeleportRange = 10.0f;       // Teleport range
				const float maxGrappleRange = 12.0f;        // Grappling hook range

				if ((availableAbilities & Ability.TeleportArc) != 0 &&
					math.length(displacement) <= maxTeleportRange)
					{
					// Teleportation - instantaneous but high energy cost
					arcMultiplier = 2.0f + (math.length(displacement) / maxTeleportRange) * 1.0f;
					}
				else if ((availableAbilities & Ability.Grapple) != 0 &&
						 math.length(displacement) <= maxGrappleRange)
					{
					// Grappling hook - smooth arc with moderate cost
					arcMultiplier = 1.3f + (math.length(displacement) / maxGrappleRange) * 0.7f;
					}
				else if (verticalDistance <= maxChargedJumpHeight && (availableAbilities & Ability.ChargedJump) != 0)
					{
					// Charged jump - variable power with precision control
					float chargeLevel = verticalDistance / maxChargedJumpHeight;
					arcMultiplier = 1.1f + chargeLevel * 0.6f;
					}
				else if (verticalDistance <= maxArcJumpHeight && (availableAbilities & Ability.ArcJump) != 0)
					{
					// Arc jump - precise parabolic control
					arcMultiplier = 1.0f + (verticalDistance / maxArcJumpHeight) * 0.4f;
					}
				else if (verticalDistance <= maxSingleJumpHeight && (availableAbilities & Ability.Jump) != 0)
					{
					// Regular jump trajectory
					arcMultiplier = 1.0f + (verticalDistance / maxSingleJumpHeight) * 0.5f;
					}
				else if (verticalDistance <= maxDoubleJumpHeight && (availableAbilities & Ability.DoubleJump) != 0)
					{
					// Double jump trajectory - more complex arc
					arcMultiplier = 1.2f + (verticalDistance / maxDoubleJumpHeight) * 0.8f;
					}
				else if (horizontalDistance <= maxWallJumpReach && (availableAbilities & Ability.WallJump) != 0)
					{
					// Wall jump - requires precise timing
					arcMultiplier = 1.5f + (verticalDistance / maxSingleJumpHeight) * 1.0f;
					}
				else
					{
					// Impossible trajectory
					return 10.0f;
					}

				// Add horizontal distance penalty for arc complexity
				if (horizontalDistance > 1.0f)
					{
					float horizontalComplexity = math.min(horizontalDistance / 5.0f, 2.0f);
					arcMultiplier *= (1.0f + horizontalComplexity * 0.3f);
					}

				// Apply precision bonuses for advanced movement abilities
				if ((availableAbilities & Ability.ArcJump) != 0)
					{
					arcMultiplier *= 0.9f; // Arc jump provides better control
					}

				if ((availableAbilities & Ability.GlideSpeed) != 0)
					{
					arcMultiplier *= 0.85f; // Gliding extends effective range
					}
				}
			else if (verticalDistance < -0.1f) // Downward movement
				{
				// Falling arc - easier but requires fall time consideration
				float fallMultiplier = 1.0f + math.min(math.abs(verticalDistance) / 10.0f, 0.5f);

				// Gliding abilities reduce fall cost
				if ((availableAbilities & Ability.GlideSpeed) != 0)
					{
					fallMultiplier *= 0.7f;
					}

				arcMultiplier = fallMultiplier;
				}

			return arcMultiplier;
			}

		/// <summary>
		/// Get the destination node when traversing from the given source
		/// </summary>
		public readonly uint GetDestination (uint sourceNodeId)
			{
			if (ConnectionType == ConnectionType.Bidirectional)
				{
				return sourceNodeId == FromNodeId ? ToNodeId : FromNodeId;
				}

			return sourceNodeId == FromNodeId ? ToNodeId : 0; // 0 indicates invalid traversal
			}

		public override readonly string ToString ()
			{
			string direction = ConnectionType == ConnectionType.Bidirectional ? "<->" : "->";
			return $"NavLink({FromNodeId} {direction} {ToNodeId}, {RequiredPolarity}, Cost:{BaseCost:F1})";
			}
		}

	/// <summary>
	/// Agent capabilities for navigation and gate traversal
	/// Defines what polarity and abilities an AI agent possesses
	/// </summary>
	public struct AgentCapabilities : IComponentData
		{
		/// <summary>
		/// Available polarity access for this agent
		/// </summary>
		public Polarity AvailablePolarity;

		/// <summary>
		/// Available abilities for this agent
		/// </summary>
		public Ability AvailableAbilities;

		/// <summary>
		/// Agent's skill level for soft gate bypass (0.0 to 1.0)
		/// </summary>
		public float SkillLevel;

		/// <summary>
		/// Agent type identifier for behavior differentiation
		/// </summary>
		public FixedString32Bytes AgentType;

		/// <summary>
		/// Whether this agent can use discovered paths of other agents
		/// </summary>
		public bool CanShareDiscoveredPaths;

		public AgentCapabilities (Polarity availablePolarity = Polarity.None,
							   Ability availableAbilities = Ability.None,
							   float skillLevel = 0.0f,
							   FixedString32Bytes agentType = default,
							   bool canShareDiscoveredPaths = true)
			{
			AvailablePolarity = availablePolarity;
			AvailableAbilities = availableAbilities;
			SkillLevel = math.clamp(skillLevel, 0.0f, 1.0f);
			AgentType = agentType;
			CanShareDiscoveredPaths = canShareDiscoveredPaths;
			}

		/// <summary>
		/// Check if this agent can pass a gate condition
		/// </summary>
		public readonly bool CanPassGate (GateCondition gate)
			{
			return gate.CanPass(AvailablePolarity, AvailableAbilities, SkillLevel);
			}

		public override readonly string ToString ()
			{
			return $"AgentCapabilities({AvailablePolarity}, {AvailableAbilities}, Skill:{SkillLevel:F2})";
			}
		}

	/// <summary>
	/// Buffer element for storing navigation links from a node
	/// Enables efficient graph traversal and pathfinding
	/// </summary>
	public struct NavLinkBufferElement : IBufferElementData
		{
		public NavLink Value;

		public static implicit operator NavLink (NavLinkBufferElement e) => e.Value;
		public static implicit operator NavLinkBufferElement (NavLink e) => new() { Value = e };
		}

	/// <summary>
	/// Runtime navigation graph state and pathfinding data
	/// Singleton component for managing navigation system
	/// </summary>
	public struct NavigationGraph : IComponentData
		{
		/// <summary>
		/// Total number of navigation nodes in the graph
		/// </summary>
		public int NodeCount;

		/// <summary>
		/// Total number of navigation links in the graph
		/// </summary>
		public int LinkCount;

		/// <summary>
		/// Whether the navigation graph has been built and is ready for use
		/// </summary>
		public bool IsReady;

		/// <summary>
		/// Last time the navigation graph was rebuilt
		/// </summary>
		public double LastRebuildTime;

		/// <summary>
		/// Number of unreachable areas detected in last validation
		/// </summary>
		public int UnreachableAreaCount;

		public NavigationGraph (int nodeCount = 0, int linkCount = 0)
			{
			NodeCount = nodeCount;
			LinkCount = linkCount;
			IsReady = false;
			LastRebuildTime = 0.0;
			UnreachableAreaCount = 0;
			}

		public override readonly string ToString ()
			{
			return $"NavigationGraph({NodeCount} nodes, {LinkCount} links, Ready:{IsReady})";
			}
		}
	}