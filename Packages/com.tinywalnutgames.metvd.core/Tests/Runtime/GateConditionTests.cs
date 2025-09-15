using NUnit.Framework;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Tests
	{
	public class GateConditionTests
		{
		[Test]
		public void Polarity_AllSinglePairs_FormExpectedDualMask()
			{
			Polarity [ ] singles = new [ ] { Polarity.Sun, Polarity.Moon, Polarity.Heat, Polarity.Cold, Polarity.Earth, Polarity.Wind, Polarity.Life, Polarity.Tech };
			for (int i = 0; i < singles.Length; i++)
				{
				for (int j = i + 1; j < singles.Length; j++)
					{
					Polarity combo = singles [ i ] | singles [ j ];
					Assert.IsTrue((combo & singles [ i ]) != 0 && (combo & singles [ j ]) != 0);
					}
				}

			Assert.AreEqual(Polarity.SunMoon, Polarity.Sun | Polarity.Moon);
			Assert.AreEqual(Polarity.HeatCold, Polarity.Heat | Polarity.Cold);
			Assert.AreEqual(Polarity.EarthWind, Polarity.Earth | Polarity.Wind);
			Assert.AreEqual(Polarity.LifeTech, Polarity.Life | Polarity.Tech);
			}

		[Test]
		public void CanPass_ActiveHardGate_RequiresAll()
			{
			var gate = new GateCondition(Polarity.SunMoon, Ability.Jump | Ability.Dash, GateSoftness.Hard, 0.0f, "Hard SunMoon Gate");
			// Missing Moon bit => fail
			Assert.IsFalse(gate.CanPass(Polarity.Sun, Ability.Jump | Ability.Dash));
			// Missing Dash ability => fail even if polarity satisfied
			Assert.IsFalse(gate.CanPass(Polarity.SunMoon, Ability.Jump));
			// All present => pass
			Assert.IsTrue(gate.CanPass(Polarity.SunMoon, Ability.Jump | Ability.Dash));
			}

		[Test]
		public void CanPass_InactiveOrUnlocked_AlwaysTrue()
			{
			var gate = new GateCondition(Polarity.Heat, Ability.Bomb, GateSoftness.Hard, 0.0f, "Inactive") { IsActive = false };
			Assert.IsTrue(gate.CanPass(Polarity.None, Ability.None));
			gate.IsActive = true; gate.IsUnlocked = true;
			Assert.IsTrue(gate.CanPass(Polarity.None, Ability.None));
			}

		[Test]
		public void CanPass_SoftGate_BypassesWithSkill()
			{
			var gate = new GateCondition(Polarity.Cold, Ability.None, GateSoftness.VeryDifficult, 0.5f, "Skill Bypass");
			Assert.IsFalse(gate.CanPass(Polarity.None, Ability.None, 0.4f));
			Assert.IsFalse(gate.CanPass(Polarity.None, Ability.None, 0.5f));
			Assert.IsTrue(gate.CanPass(Polarity.None, Ability.None, 0.85f));
			}

		[Test]
		public void CanPass_TrivialGate_EasyBypass()
			{
			var gate = new GateCondition(Polarity.Heat, Ability.None, GateSoftness.Trivial, 0.2f, "Trivial");
			Assert.IsFalse(gate.CanPass(Polarity.None, Ability.None, 0.1f));
			Assert.IsTrue(gate.CanPass(Polarity.None, Ability.None, 0.2f));
			Assert.IsTrue(gate.CanPass(Polarity.None, Ability.None, 0.9f));
			}

		[Test]
		public void GetMissingRequirements_ReturnsExpectedMasks()
			{
			var gate = new GateCondition(Polarity.HeatCold, Ability.Jump | Ability.Dash, GateSoftness.Hard, 0f, "Req Mask");
			(Polarity missPol, Ability missAb) = gate.GetMissingRequirements(Polarity.Heat, Ability.Jump);
			Assert.AreEqual(Polarity.Cold, missPol);
			Assert.AreEqual(Ability.Dash, missAb);
			(missPol, missAb) = gate.GetMissingRequirements(Polarity.HeatCold, Ability.Jump | Ability.Dash);
			Assert.AreEqual(Polarity.None, missPol);
			Assert.AreEqual(Ability.None, missAb);
			}

		[Test]
		public void AnyPolarityRequirement_PassesWithAny()
			{
			var gate = new GateCondition(Polarity.Any, Ability.None, GateSoftness.Hard, 0f, "Any Gate");
			Assert.IsTrue(gate.CanPass(Polarity.Sun, Ability.None));
			Assert.IsTrue(gate.CanPass(Polarity.Tech, Ability.None));
			Assert.IsTrue(gate.CanPass(Polarity.SunMoon, Ability.None));
			}
		}
	}
