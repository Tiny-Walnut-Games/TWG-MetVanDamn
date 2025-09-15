using UnityEngine;
using TinyWalnutGames.MetVD.Samples;

/// <summary>
/// Quick lifecycle debug test for SmokeTestSceneSetup
/// </summary>
public class TestLifecycleDebug : MonoBehaviour
{
    void Start()
    {
        Debug.Log("🔍 TestLifecycleDebug: Creating SmokeTestSceneSetup component...");
        
        var go = new GameObject("SmokeTestSceneSetup");
        var component = go.AddComponent<SmokeTestSceneSetup>();
        
        Debug.Log("🔍 TestLifecycleDebug: Component created. Waiting for its Awake/Start...");
        
        // Component should automatically call Awake() and Start() since it's added to an active GameObject
    }
}
