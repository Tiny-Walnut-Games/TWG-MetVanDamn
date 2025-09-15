using UnityEngine;
using TinyWalnutGames.MetVD.Core;

public class DebugEnumTest : MonoBehaviour
{
    void Start()
    {
        // Test the AllArcMovement enum fix
        bool includesGrapple = (Ability.AllArcMovement & Ability.Grapple) != 0;
        Debug.Log($"AllArcMovement includes Grapple: {includesGrapple}");
        Debug.Log($"AllArcMovement value: {Ability.AllArcMovement}");
        Debug.Log($"Grapple value: {Ability.Grapple}");
    }
}
