using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// Component that tracks player skill progression and abilities
    /// </summary>
    public struct PlayerSkillState : IComponentData
    {
        /// <summary>
        /// Currently unlocked abilities
        /// </summary>
        public Ability UnlockedAbilities;
        
        /// <summary>
        /// Abilities that are temporarily available (from powerups, etc.)
        /// </summary>
        public Ability TemporaryAbilities;
        
        /// <summary>
        /// Player level or progression marker
        /// </summary>
        public int ProgressionLevel;
        
        /// <summary>
        /// Experience points for skill advancement
        /// </summary>
        public float ExperiencePoints;
        
        /// <summary>
        /// Health/energy for abilities that require it
        /// </summary>
        public float AbilityEnergy;
        
        /// <summary>
        /// Maximum ability energy
        /// </summary>
        public float MaxAbilityEnergy;
        
        /// <summary>
        /// Timer for temporary abilities (countdown in seconds)
        /// </summary>
        public float TemporaryAbilityTimer;
        
        /// <summary>
        /// Rate of energy regeneration per second
        /// </summary>
        public float EnergyRegenRate;

        public PlayerSkillState(Ability initialAbilities = Ability.Jump | Ability.DoubleJump, int level = 1)
        {
            UnlockedAbilities = initialAbilities;
            TemporaryAbilities = Ability.None;
            ProgressionLevel = math.max(1, level);
            ExperiencePoints = 0f;
            AbilityEnergy = 100f;
            MaxAbilityEnergy = 100f;
            TemporaryAbilityTimer = 0f;
            EnergyRegenRate = 10f; // 10 energy per second default
        }

        /// <summary>
        /// Get all currently available abilities (unlocked + temporary)
        /// </summary>
        public readonly Ability GetAvailableAbilities()
        {
            return UnlockedAbilities | TemporaryAbilities;
        }

        /// <summary>
        /// Check if a specific ability is available
        /// </summary>
        public readonly bool HasAbility(Ability ability)
        {
            return (GetAvailableAbilities() & ability) != 0;
        }

        /// <summary>
        /// Unlock a new ability permanently
        /// </summary>
        public void UnlockAbility(Ability ability)
        {
            UnlockedAbilities |= ability;
        }

        /// <summary>
        /// Grant a temporary ability (from powerup, etc.)
        /// </summary>
        public void GrantTemporaryAbility(Ability ability)
        {
            TemporaryAbilities |= ability;
        }

        /// <summary>
        /// Remove a temporary ability
        /// </summary>
        public void RemoveTemporaryAbility(Ability ability)
        {
            TemporaryAbilities &= ~ability;
        }
    }

    /// <summary>
    /// Component for ability upgrade/unlock events
    /// </summary>
    public struct AbilityUnlockEvent : IComponentData
    {
        /// <summary>
        /// The ability being unlocked
        /// </summary>
        public Ability NewAbility;
        
        /// <summary>
        /// Whether this is a permanent unlock or temporary grant
        /// </summary>
        public bool IsPermanent;
        
        /// <summary>
        /// Duration for temporary abilities (in seconds, -1 for permanent)
        /// </summary>
        public float Duration;

        public AbilityUnlockEvent(Ability ability, bool isPermanent = true, float duration = -1f)
        {
            NewAbility = ability;
            IsPermanent = isPermanent;
            Duration = isPermanent ? -1f : math.max(0f, duration);
        }
    }

    /// <summary>
    /// Singleton component for global player state
    /// </summary>
    public struct GlobalPlayerState : IComponentData
    {
        /// <summary>
        /// Reference to the main player entity
        /// </summary>
        public Entity PlayerEntity;
        
        /// <summary>
        /// Current save game progression flags
        /// </summary>
        public int SaveProgressionFlags;
        
        /// <summary>
        /// Game completion percentage
        /// </summary>
        public float CompletionPercentage;

        public GlobalPlayerState(Entity playerEntity)
        {
            PlayerEntity = playerEntity;
            SaveProgressionFlags = 0;
            CompletionPercentage = 0f;
        }
    }

    /// <summary>
    /// System that manages player skill progression and state
    /// Provides centralized access to player abilities for room generation
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct PlayerStateSystem : ISystem
    {
        private ComponentLookup<PlayerSkillState> _skillStateLookup;
        private ComponentLookup<GlobalPlayerState> _globalStateLookup;
        private EntityQuery _playerQuery;
        private EntityQuery _unlockEventQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _skillStateLookup = state.GetComponentLookup<PlayerSkillState>();
            _globalStateLookup = state.GetComponentLookup<GlobalPlayerState>();
            
            _playerQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PlayerSkillState>()
                .Build(ref state);
                
            _unlockEventQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<AbilityUnlockEvent>()
                .Build(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _skillStateLookup.Update(ref state);
            _globalStateLookup.Update(ref state);

            // Process ability unlock events
            if (!_unlockEventQuery.IsEmpty)
            {
                var processUnlockJob = new ProcessAbilityUnlockJob
                {
                    SkillStateLookup = _skillStateLookup,
                    DeltaTime = state.WorldUnmanaged.Time.DeltaTime
                };

                state.Dependency = processUnlockJob.ScheduleParallel(_unlockEventQuery, state.Dependency);
                
                // Clean up processed events
                state.EntityManager.DestroyEntity(_unlockEventQuery);
            }

            // Update temporary ability timers (would be implemented for timed powerups)
            var updateTimersJob = new UpdateTemporaryAbilityTimersJob
            {
                DeltaTime = state.WorldUnmanaged.Time.DeltaTime
            };

            state.Dependency = updateTimersJob.ScheduleParallel(_playerQuery, state.Dependency);
        }
    }

    /// <summary>
    /// Job that processes ability unlock events
    /// </summary>
    [BurstCompile]
    public partial struct ProcessAbilityUnlockJob : IJobEntity
    {
        public ComponentLookup<PlayerSkillState> SkillStateLookup;
        public float DeltaTime;

        public void Execute(Entity entity, in AbilityUnlockEvent unlockEvent, in GlobalPlayerState globalState)
        {
            if (SkillStateLookup.HasComponent(globalState.PlayerEntity))
            {
                var skillState = SkillStateLookup[globalState.PlayerEntity];
                
                if (unlockEvent.IsPermanent)
                {
                    skillState.UnlockAbility(unlockEvent.NewAbility);
                }
                else
                {
                    skillState.GrantTemporaryAbility(unlockEvent.NewAbility);
                }
                
                SkillStateLookup[globalState.PlayerEntity] = skillState;
            }
        }
    }

    /// <summary>
    /// Job that updates temporary ability timers
    /// </summary>
    [BurstCompile]
    public partial struct UpdateTemporaryAbilityTimersJob : IJobEntity
    {
        public float DeltaTime;

        public readonly void Execute(ref PlayerSkillState skillState)
        {
            // Update temporary ability timers and remove expired ones
            skillState.TemporaryAbilityTimer -= DeltaTime;
            
            if (skillState.TemporaryAbilityTimer <= 0f)
            {
                // Remove temporary abilities that have expired
                skillState.TemporaryAbilities = Ability.None;
                skillState.TemporaryAbilityTimer = 0f;
            }
            
            // Update energy regeneration
            if (skillState.AbilityEnergy < skillState.MaxAbilityEnergy)
            {
                skillState.AbilityEnergy = math.min(
                    skillState.MaxAbilityEnergy,
                    skillState.AbilityEnergy + skillState.EnergyRegenRate * DeltaTime
                );
            }
        }
    }

    /// <summary>
    /// Static utility class for querying player state from other systems
    /// </summary>
    public static class PlayerStateUtility
    {
        /// <summary>
        /// Get currently available player abilities from any system
        /// </summary>
        public static Ability GetCurrentPlayerAbilities(EntityManager entityManager)
        {
            // Find the global player state
            using var globalStateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<GlobalPlayerState>()
                .Build(entityManager);

            if (!globalStateQuery.IsEmpty)
            {
                var globalState = globalStateQuery.GetSingleton<GlobalPlayerState>();
                
                if (entityManager.HasComponent<PlayerSkillState>(globalState.PlayerEntity))
                {
                    var skillState = entityManager.GetComponentData<PlayerSkillState>(globalState.PlayerEntity);
                    return skillState.GetAvailableAbilities();
                }
            }

            // Fallback to basic abilities if no player state found
            return Ability.Jump | Ability.DoubleJump;
        }

        /// <summary>
        /// Check if player has a specific ability
        /// </summary>
        public static bool PlayerHasAbility(EntityManager entityManager, Ability ability)
        {
            var availableAbilities = GetCurrentPlayerAbilities(entityManager);
            return (availableAbilities & ability) != 0;
        }

        /// <summary>
        /// Get player progression level
        /// </summary>
        public static int GetPlayerLevel(EntityManager entityManager)
        {
            using var globalStateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<GlobalPlayerState>()
                .Build(entityManager);

            if (!globalStateQuery.IsEmpty)
            {
                var globalState = globalStateQuery.GetSingleton<GlobalPlayerState>();
                
                if (entityManager.HasComponent<PlayerSkillState>(globalState.PlayerEntity))
                {
                    var skillState = entityManager.GetComponentData<PlayerSkillState>(globalState.PlayerEntity);
                    return skillState.ProgressionLevel;
                }
            }

            return 1; // Default starting level
        }
    }
}