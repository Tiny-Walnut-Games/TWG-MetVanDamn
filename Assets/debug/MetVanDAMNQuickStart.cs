using UnityEngine;
using TinyWalnutGames.MetVD.Samples;

/// <summary>
/// Demo scene for "Hit Play -> See Map" workflow validation
/// Drop this on a GameObject in your scene and hit Play
/// </summary>
public class MetVanDAMNQuickStart : MonoBehaviour
{
    [Header("Quick Start Demo")]
    [SerializeField] private bool createSmokeTestSetup = true;
    [SerializeField] private uint demoSeed = 12345;
    [SerializeField] private int demoSectorCount = 8;
    
    void Start()
    {
        if (createSmokeTestSetup)
        {
            Debug.Log("🚀 MetVanDAMN Quick Start: Creating SmokeTestSceneSetup...");
            
            var setupGO = new GameObject("MetVanDAMN_SmokeTestSetup");
            var setup = setupGO.AddComponent<SmokeTestSceneSetup>();
            
            // The component will automatically call Awake() and Start() and begin world generation
            Debug.Log($"✅ MetVanDAMN Quick Start: Setup created with seed {demoSeed}");
            Debug.Log("📊 Check Console for generation logs and Entity Debugger for created entities");
            Debug.Log("🎯 Expected: Hub + district entities, polarity fields, world configuration");
        }
    }
}
