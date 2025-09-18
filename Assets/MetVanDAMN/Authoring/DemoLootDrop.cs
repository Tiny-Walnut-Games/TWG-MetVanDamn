using UnityEngine;

namespace TinyWalnutGames.MetVanDAMN.Authoring
{
    /// <summary>
    /// Minimal loot drop component that exposes a DemoItem for pickup by the inventory system.
    /// Attach this to world loot prefabs and configure the item in the inspector.
    /// </summary>
    public class DemoLootDrop : MonoBehaviour
    {
        [SerializeField]
        private DemoItem item; // Assign in inspector

        public DemoItem GetItem()
        {
            return item;
        }
    }
}
