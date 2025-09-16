using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Samples;
using Unity.Entities;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Debug
    {
    /// <summary>
    /// Simple component to display entity information in the scene
    /// Helps debug what entities are actually being created
    /// </summary>
    public class EntityDebugDisplay : MonoBehaviour
        {
        [Header("Debug Display Settings")]
        [SerializeField] private bool showEntityCounts = true;
        [SerializeField] private bool showEntityNames = true;
        [SerializeField] private float updateInterval = 1.0f;

        private float lastUpdateTime;

        private void Update()
            {
            if (Time.time - lastUpdateTime >= updateInterval)
                {
                DisplayEntityInfo();
                lastUpdateTime = Time.time;
                }
            }

        private void DisplayEntityInfo()
            {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                {
                UnityEngine.Debug.LogWarning("🚨 No default world found for entity debugging");
                return;
                }

            EntityManager em = world.EntityManager;

            if (showEntityCounts)
                {
                using var nodeQuery = em.CreateEntityQuery(typeof(NodeId));
                using var polarityQuery = em.CreateEntityQuery(typeof(PolarityFieldData));

                int nodeCount = nodeQuery.CalculateEntityCount();
                int polarityCount = polarityQuery.CalculateEntityCount();

                UnityEngine.Debug.Log($"🔍 Entity Debug: {nodeCount} districts, {polarityCount} polarity fields");
                }

            if (showEntityNames)
                {
                using var nodeQuery = em.CreateEntityQuery(typeof(NodeId));
                var entities = nodeQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

                foreach (Entity entity in entities)
                    {
                    string name = em.GetName(entity);
                    NodeId nodeId = em.GetComponentData<NodeId>(entity);
                    UnityEngine.Debug.Log($"📍 District: {name} at coordinates {nodeId.Coordinates}");
                    }

                entities.Dispose();
                }
            }

        private void OnGUI()
            {
            if (!showEntityCounts) return;

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            EntityManager em = world.EntityManager;
            using var nodeQuery = em.CreateEntityQuery(typeof(NodeId));
            using var polarityQuery = em.CreateEntityQuery(typeof(PolarityFieldData));

            int nodeCount = nodeQuery.CalculateEntityCount();
            int polarityCount = polarityQuery.CalculateEntityCount();

            GUI.Label(new Rect(10, 10, 300, 20), $"Districts: {nodeCount}, Polarity Fields: {polarityCount}");
            }
        }
    }
