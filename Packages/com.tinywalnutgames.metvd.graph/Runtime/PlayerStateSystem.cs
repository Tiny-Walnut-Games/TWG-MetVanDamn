using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// Component that tracks player skill progression and unlocked abilities
    /// </summary>
    public struct PlayerSkillState : IComponentData
    {
        public Ability UnlockedAbilities;          // Currently unlocked abilities
        public Ability TemporaryPowerups;          // Temporary powerups (expire over time)
        public float ExperiencePoints;             // Total XP accumulated
        public float EnergyLevel;                  // Current energy for abilities (0-1)
        public float EnergyRegenRate;              // Energy regeneration per second
        public int SkillPointsAvailable;           // Skill points to spend
        public bool HasRecentlyUnlockedAbility;    // Flag for UI notifications
        
        public PlayerSkillState(Ability startingAbilities = Ability.Jump)
        {
            UnlockedAbilities = startingAbilities;
            TemporaryPowerups = Ability.None;
            ExperiencePoints = 0f;
            EnergyLevel = 1f;
            EnergyRegenRate = 0.2f; // 20% per second
            SkillPointsAvailable = 0;
            HasRecentlyUnlockedAbility = false;
        }
    }

    /// <summary>
    /// Component for tracking ability unlock requirements
    /// </summary>
    public struct AbilityUnlockRequirement : IComponentData
    {
        public Ability RequiredPrerequisites;     // Abilities needed before unlocking
        public float ExperienceRequired;          // XP needed to unlock
        public int SkillPointsCost;              // Skill points needed
        public bool IsUnlocked;                  // Whether this has been unlocked
        
        public AbilityUnlockRequirement(Ability prerequisites, float xpRequired, int skillCost = 1)
        {
            RequiredPrerequisites = prerequisites;
            ExperienceRequired = xpRequired;
            SkillPointsCost = skillCost;
            IsUnlocked = false;
        }
    }

    /// <summary>
    /// Singleton component for managing global player state
    /// </summary>
    public struct PlayerStateManager : IComponentData
    {
        public Entity PlayerEntity;
        public float GlobalDifficultyModifier;     // Adjusts challenge based on player progress
        public bool EnableProgressiveSkillGating;  // Whether rooms gate based on skills
        public float LastSkillUpdateTime;         // For tracking progression rate
        
        public PlayerStateManager(Entity player, float difficulty = 1.0f)
        {
            PlayerEntity = player;
            GlobalDifficultyModifier = difficulty;
            EnableProgressiveSkillGating = true;
            LastSkillUpdateTime = 0f;
        }
    }

    /// <summary>
    /// System that tracks skill progression and manages ability unlocks
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct PlayerStateSystem : ISystem
    {
        private ComponentLookup<PlayerSkillState> _skillStateLookup;
        private ComponentLookup<AbilityUnlockRequirement> _unlockRequirementLookup;
        private EntityQuery _playerQuery;
        private EntityQuery _unlockableAbilityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _skillStateLookup = state.GetComponentLookup<PlayerSkillState>();
            _unlockRequirementLookup = state.GetComponentLookup<AbilityUnlockRequirement>();
            
            _playerQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PlayerSkillState>()
                .Build(ref state);

            _unlockableAbilityQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<AbilityUnlockRequirement>()
                .WithNone<PlayerSkillState>() // Abilities are separate entities
                .Build(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _skillStateLookup.Update(ref state);
            _unlockRequirementLookup.Update(ref state);

            var deltaTime = state.WorldUnmanaged.Time.DeltaTime;
            var time = (float)state.WorldUnmanaged.Time.ElapsedTime;

            // Update player skill progression
            var progressionJob = new SkillProgressionJob
            {
                DeltaTime = deltaTime,
                CurrentTime = time,
                UnlockRequirementLookup = _unlockRequirementLookup,
                EntityCommandBuffer = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged)
            };

            state.Dependency = progressionJob.ScheduleParallel(_playerQuery, state.Dependency);

            // Process ability unlocks
            var unlockJob = new AbilityUnlockJob
            {
                SkillStateLookup = _skillStateLookup,
                EntityCommandBuffer = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged)
            };

            state.Dependency = unlockJob.ScheduleParallel(_unlockableAbilityQuery, state.Dependency);
        }
    }

    /// <summary>
    /// Job that updates player skill progression and energy regeneration
    /// </summary>
    [BurstCompile]
    public partial struct SkillProgressionJob : IJobEntity
    {
        public float DeltaTime;
        public float CurrentTime;
        [ReadOnly] public ComponentLookup<AbilityUnlockRequirement> UnlockRequirementLookup;
        public EntityCommandBuffer EntityCommandBuffer;

        public void Execute(Entity entity, ref PlayerSkillState skillState, ref PlayerStateManager manager)
        {
            // Regenerate energy
            if (skillState.EnergyLevel < 1f)
            {
                skillState.EnergyLevel = math.min(1f, skillState.EnergyLevel + skillState.EnergyRegenRate * DeltaTime);
            }

            // Award XP for continued play (passive progression)
            skillState.ExperiencePoints += 1.0f * DeltaTime; // 1 XP per second base rate

            // Check for skill point awards based on XP thresholds
            var skillPointThreshold = (skillState.SkillPointsAvailable + 1) * 100f; // 100, 200, 300 XP...
            if (skillState.ExperiencePoints >= skillPointThreshold)
            {
                skillState.SkillPointsAvailable++;
            }

            // Update manager timestamp
            manager.LastSkillUpdateTime = CurrentTime;

            // Adjust global difficulty based on player progression
            var progressionRatio = skillState.ExperiencePoints / 1000f; // Normalize to 1000 XP
            manager.GlobalDifficultyModifier = 1f + math.min(progressionRatio * 0.5f, 2f); // Cap at 3x difficulty
        }
    }

    /// <summary>
    /// Job that processes ability unlock requirements
    /// </summary>
    [BurstCompile]
    public partial struct AbilityUnlockJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<PlayerSkillState> SkillStateLookup;
        public EntityCommandBuffer EntityCommandBuffer;

        public void Execute(Entity abilityEntity, ref AbilityUnlockRequirement requirement)
        {
            if (requirement.IsUnlocked)
                return;

            // Find player entity (in practice, this would be cached or passed in)
            var playerEntity = FindPlayerEntity();
            if (playerEntity == Entity.Null || !SkillStateLookup.HasComponent(playerEntity))
                return;

            var playerSkills = SkillStateLookup[playerEntity];

            // Check if unlock requirements are met
            bool prerequisitesMet = (playerSkills.UnlockedAbilities & requirement.RequiredPrerequisites) == requirement.RequiredPrerequisites;
            bool hasXP = playerSkills.ExperiencePoints >= requirement.ExperienceRequired;
            bool hasSkillPoints = playerSkills.SkillPointsAvailable >= requirement.SkillPointsCost;

            if (prerequisitesMet && hasXP && hasSkillPoints)
            {
                // Unlock the ability (would need to know which ability this requirement represents)
                requirement.IsUnlocked = true;
                
                // This would typically trigger an event or update the player's skill state
                // For now, mark that an unlock happened
                EntityCommandBuffer.SetComponent(playerEntity, new PlayerSkillState
                {
                    UnlockedAbilities = playerSkills.UnlockedAbilities, // Would add new ability here
                    TemporaryPowerups = playerSkills.TemporaryPowerups,
                    ExperiencePoints = playerSkills.ExperiencePoints,
                    EnergyLevel = playerSkills.EnergyLevel,
                    EnergyRegenRate = playerSkills.EnergyRegenRate,
                    SkillPointsAvailable = playerSkills.SkillPointsAvailable - requirement.SkillPointsCost,
                    HasRecentlyUnlockedAbility = true
                });
            }
        }

        private Entity FindPlayerEntity()
        {
            // In a full implementation, this would use a singleton or cached reference
            // For now, return Entity.Null as placeholder
            return Entity.Null;
        }
    }

    /// <summary>
    /// Utility class for querying player skills from other systems
    /// </summary>
    public static class PlayerStateUtility
    {
        /// <summary>
        /// Get currently available abilities for room generation systems
        /// </summary>
        public static Ability GetAvailableAbilities(ref SystemState state, Entity playerEntity)
        {
            var skillLookup = state.GetComponentLookup<PlayerSkillState>(true);
            if (skillLookup.HasComponent(playerEntity))
            {
                var skills = skillLookup[playerEntity];
                return skills.UnlockedAbilities | skills.TemporaryPowerups;
            }
            
            // Default starting abilities if no player state found
            return Ability.Jump | Ability.DoubleJump;
        }

        /// <summary>
        /// Get current difficulty modifier based on player progression
        /// </summary>
        public static float GetDifficultyModifier(ref SystemState state)
        {
            var managerLookup = state.GetComponentLookup<PlayerStateManager>(true);
            
            // Find player state manager (would typically be a singleton)
            // For now, return default difficulty
            return 1.0f;
        }

        /// <summary>
        /// Check if player has sufficient energy for an ability
        /// </summary>
        public static bool HasSufficientEnergy(ref SystemState state, Entity playerEntity, float energyCost)
        {
            var skillLookup = state.GetComponentLookup<PlayerSkillState>(true);
            if (skillLookup.HasComponent(playerEntity))
            {
                var skills = skillLookup[playerEntity];
                return skills.EnergyLevel >= energyCost;
            }
            
            return true; // Assume sufficient energy if no player state
        }
    }
}