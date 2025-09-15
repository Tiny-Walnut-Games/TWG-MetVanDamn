// Quick test script to verify WorldBootstrapAuthoring inspector functionality
// This script can be temporarily added to a scene with WorldBootstrapAuthoring component

using UnityEngine;
using TinyWalnutGames.MetVD.Authoring;

public class TestAuthoringRegeneration : MonoBehaviour
{
    void Start()
    {
        Debug.Log("🧪 Testing WorldBootstrapAuthoring runtime regeneration functionality");

        var worldBootstrap = FindObjectOfType<WorldBootstrapAuthoring>();
        if (worldBootstrap != null)
        {
            Debug.Log($"✅ Found WorldBootstrapAuthoring component with seed: {worldBootstrap.seed}");
            Debug.Log($"   World Size: {worldBootstrap.worldSize}");
            Debug.Log($"   District Count: {worldBootstrap.districtCount}");
            Debug.Log($"   Sectors Per District: {worldBootstrap.sectorsPerDistrict}");
            Debug.Log($"   Rooms Per Sector: {worldBootstrap.roomsPerSector}");
            Debug.Log("🎮 Play mode runtime regeneration buttons should now be available in the inspector!");
        }
        else
        {
            Debug.LogWarning("❌ No WorldBootstrapAuthoring component found in scene");
            Debug.Log("💡 Hint: Create a scene with WorldBootstrapAuthoring component using:");
            Debug.Log("   Menu: Tiny Walnut Games > MetVanDAMN > Sample Creation > Create Baseline Scene");
        }
    }

    void Update()
    {
        // Optional: Test seed changes during runtime
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var worldBootstrap = FindObjectOfType<WorldBootstrapAuthoring>();
            if (worldBootstrap != null)
            {
                int oldSeed = worldBootstrap.seed;
                worldBootstrap.seed = Random.Range(1, 999999);
                Debug.Log($"🎯 Manual seed change: {oldSeed} -> {worldBootstrap.seed}");
                Debug.Log("   Use inspector runtime regeneration buttons for full world regeneration!");
            }
        }
    }
}
