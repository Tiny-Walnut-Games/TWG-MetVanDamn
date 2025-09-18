using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace TinyWalnutGames.MetVD.QuadRig
{
    /// <summary>
    /// System that handles Y-axis billboard rotation for quad-rig humanoid characters.
    /// Ensures characters always face the camera without distorting their animations.
    /// Implements smooth rotation to prevent jarring movement.
    /// </summary>
    [BurstCompile]
    public partial struct BillboardSystem : ISystem
    {
        /// <summary>
        /// Camera position for billboard calculations
        /// </summary>
        private float3 cameraPosition;

        /// <summary>
        /// Whether the camera position has been initialized
        /// </summary>
        private bool cameraInitialized;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<QuadRigHumanoid>();
            state.RequireForUpdate<BillboardData>();
            cameraInitialized = false;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Update camera position (in real implementation this would come from camera system)
            UpdateCameraPosition(ref state);

            if (!cameraInitialized)
                return;

            float deltaTime = SystemAPI.Time.DeltaTime;

            // Process billboard rotation for all quad-rig characters
            var billboardJob = new BillboardRotationJob
            {
                CameraPosition = cameraPosition,
                DeltaTime = deltaTime
            };

            billboardJob.ScheduleParallel();
        }

        /// <summary>
        /// Updates camera position for billboard calculations.
        /// In a real implementation, this would query the active camera entity.
        /// For now, we'll use a mock camera position that can be set externally.
        /// </summary>
        private void UpdateCameraPosition(ref SystemState state)
        {
            // TODO: In real implementation, query camera entity
            // For now, use a default position that demonstrates billboard functionality
            if (!cameraInitialized)
            {
                cameraPosition = new float3(0, 2, 5); // Mock camera position
                cameraInitialized = true;
            }
        }

        /// <summary>
        /// Public method to set camera position for testing purposes
        /// </summary>
        public void SetCameraPosition(float3 position)
        {
            cameraPosition = position;
            cameraInitialized = true;
        }
    }

    /// <summary>
    /// Burst-compiled job for processing billboard rotations in parallel.
    /// Calculates Y-axis rotation to face camera while preserving animation.
    /// </summary>
    [BurstCompile]
    public partial struct BillboardRotationJob : IJobEntity
    {
        [ReadOnly] public float3 CameraPosition;
        [ReadOnly] public float DeltaTime;

        [BurstCompile]
        public void Execute(
            ref LocalTransform transform,
            in QuadRigHumanoid humanoid,
            in BillboardData billboard)
        {
            // Skip if billboard is disabled for this character
            if (!humanoid.EnableBillboard || !billboard.IsActive)
                return;

            // Calculate direction to camera (Y-axis constrained)
            float3 characterPosition = transform.Position;
            float3 directionToCamera = CameraPosition - characterPosition;
            
            // Constrain to Y-axis rotation only (flatten to XZ plane)
            directionToCamera.y = 0;
            
            // Skip if too close to camera (avoid singularity)
            float distanceSquared = math.lengthsq(directionToCamera);
            if (distanceSquared < 0.01f)
                return;

            // Calculate target rotation
            float3 forward = math.normalize(directionToCamera);
            quaternion targetRotation = quaternion.LookRotationSafe(forward, math.up());

            // Apply smooth rotation
            quaternion currentRotation = transform.Rotation;
            quaternion newRotation = math.slerp(currentRotation, targetRotation, billboard.RotationSpeed * DeltaTime);
            
            // Update transform
            transform.Rotation = newRotation;
        }
    }

    /// <summary>
    /// Utility class for billboard system configuration and testing.
    /// Provides methods to control billboard behavior and camera setup.
    /// </summary>
    public static class BillboardUtility
    {
        /// <summary>
        /// Creates a standard billboard configuration for humanoid characters
        /// </summary>
        public static BillboardData CreateHumanoidBillboard(bool isActive = true, float rotationSpeed = 5.0f)
        {
            return new BillboardData(isActive, rotationSpeed);
        }

        /// <summary>
        /// Calculates the Y-axis rotation needed to face a target position
        /// </summary>
        public static quaternion CalculateYAxisLookRotation(float3 from, float3 to)
        {
            float3 direction = to - from;
            direction.y = 0; // Constrain to Y-axis
            
            if (math.lengthsq(direction) < 0.001f)
                return quaternion.identity;
                
            float3 forward = math.normalize(direction);
            return quaternion.LookRotationSafe(forward, math.up());
        }

        /// <summary>
        /// Validates billboard configuration for proper Y-axis constraint
        /// </summary>
        public static bool ValidateBillboardConfig(in BillboardData billboard)
        {
            // Check that constraint axis is normalized Y-axis
            float3 expectedAxis = math.up();
            float dotProduct = math.dot(billboard.ConstraintAxis, expectedAxis);
            return math.abs(dotProduct - 1.0f) < 0.01f;
        }
    }
}