#nullable enable
using UnityEngine;

namespace TinyWalnutGames.MetVanDAMN.Authoring
	{
	public class DemoSceneValidator : MonoBehaviour
		{
		[Header("Validation Config")] public bool validateOnStart = true;

		public bool requirePlayerMovement = true;
		public bool requireCombatSystem = true;
		public bool requireInventorySystem = true;
		public bool requireLootSystem = true;
		public bool requireEnemyAI = true;

		private void Start()
			{
			if (validateOnStart)
				{
				ValidateScene();
				}
			}

		public bool ValidateScene()
			{
			bool ok = true;

			if (requirePlayerMovement && !FindFirstObjectByType<DemoPlayerMovement>())
				{
				Debug.LogWarning("Validation: Missing DemoPlayerMovement");
				ok = false;
				}

			if (requireCombatSystem && !FindFirstObjectByType<DemoPlayerCombat>())
				{
				Debug.LogWarning("Validation: Missing DemoPlayerCombat");
				ok = false;
				}

			if (requireInventorySystem && !FindFirstObjectByType<DemoPlayerInventory>())
				{
				Debug.LogWarning("Validation: Missing DemoPlayerInventory");
				ok = false;
				}

			if (requireLootSystem && !FindFirstObjectByType<DemoLootManager>())
				{
				Debug.LogWarning("Validation: Missing DemoLootManager");
				ok = false;
				}

			if (requireEnemyAI && !FindFirstObjectByType<DemoAIManager>())
				{
				Debug.LogWarning("Validation: Missing DemoAIManager");
				ok = false;
				}

			return ok;
			}
		}
	}
