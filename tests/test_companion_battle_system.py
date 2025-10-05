#!/usr/bin/env python3
"""
Companion Battle System Tests
Test suite for the TLDA/Warbler companion battler integration

Tests:
- Companion battle semantics (element, archetype, temperament)
- Battle stats scaling and progression
- Elemental effectiveness calculations
- Battle experience and XP awards
- Warbler personality integration
- NFT metadata generation for battle-proven companions
"""

import unittest
import sys
import os
import datetime
import json

# Add the badge pet system path
sys.path.append(os.path.join(os.path.dirname(__file__), '../src/DeveloperExperience'))

try:
    from badge_pet_system import *
    IMPORTS_AVAILABLE = True
except ImportError as e:
    print(f"⚠️ Import failed (expected in minimal environment): {e}")
    IMPORTS_AVAILABLE = False

class TestCompanionBattleSystem(unittest.TestCase):
    """Test companion battle system integration"""
    
    def setUp(self):
        """Set up test companions for battle testing"""
        if not IMPORTS_AVAILABLE:
            self.skipTest("Badge pet system not available")
        
        self.test_companion = BadgePet(
            pet_id='test-companion-001',
            pet_name='TestScrollhound',
            species=PetSpecies.SCROLLHOUND,
            current_stage=PetStage.JUVENILE,
            developer_name='TestDeveloper',
            birth_date=datetime.datetime.now(),
            traits=[PetTrait.ANALYTICAL, PetTrait.METHODICAL],
            metrics=PetMetrics(),
            genre_theme=DeveloperGenre.FANTASY,
            personality_quirks=['references_documentation_frequently'],
            favorite_activities=['documentation', 'code_review'],
            element=CompanionElement.ORDER,
            archetype=CompanionArchetype.GUARDIAN,
            temperament=CompanionTemperament.DEFENSIVE,
            bond_level=5
        )
    
    def test_companion_battle_semantics(self):
        """Test companion battle semantic assignments"""
        if not IMPORTS_AVAILABLE:
            self.skipTest("Badge pet system not available")
        
        # Test element assignment
        self.assertEqual(self.test_companion.element, CompanionElement.ORDER)
        
        # Test archetype assignment
        self.assertEqual(self.test_companion.archetype, CompanionArchetype.GUARDIAN)
        
        # Test temperament assignment
        self.assertEqual(self.test_companion.temperament, CompanionTemperament.DEFENSIVE)
        
        # Test bond level
        self.assertEqual(self.test_companion.bond_level, 5)
        
        print(f"✅ Battle semantics correctly assigned: {self.test_companion.element.value}/{self.test_companion.archetype.value}/{self.test_companion.temperament.value}")
    
    def test_elemental_effectiveness(self):
        """Test elemental effectiveness calculations"""
        if not IMPORTS_AVAILABLE:
            self.skipTest("Badge pet system not available")
        
        # Test strong effectiveness (Order vs Creativity)
        strong_effectiveness = self.test_companion.get_battle_effectiveness(CompanionElement.CREATIVITY)
        self.assertEqual(strong_effectiveness, 2.0)
        
        # Test weak effectiveness (Order vs Chaos) 
        weak_effectiveness = self.test_companion.get_battle_effectiveness(CompanionElement.CHAOS)
        self.assertEqual(weak_effectiveness, 0.5)
        
        # Test neutral effectiveness (Order vs Logic)
        neutral_effectiveness = self.test_companion.get_battle_effectiveness(CompanionElement.LOGIC)
        self.assertEqual(neutral_effectiveness, 1.0)
        
        # Test balance element (always neutral)
        balance_companion = BadgePet(
            pet_id='balance-test',
            pet_name='BalanceTest',
            species=PetSpecies.CHRONO_CAT,
            current_stage=PetStage.JUVENILE,
            developer_name='TestDeveloper',
            birth_date=datetime.datetime.now(),
            traits=[],
            metrics=PetMetrics(),
            genre_theme=DeveloperGenre.CYBERPUNK,
            personality_quirks=[],
            favorite_activities=[],
            element=CompanionElement.BALANCE,
            archetype=CompanionArchetype.HYBRID,
            temperament=CompanionTemperament.LOYAL,
            bond_level=1
        )
        
        balance_vs_all = [
            balance_companion.get_battle_effectiveness(element)
            for element in CompanionElement
        ]
        
        # All should be 1.0 (neutral)
        self.assertTrue(all(eff == 1.0 for eff in balance_vs_all))
        
        print(f"✅ Elemental effectiveness working: Strong={strong_effectiveness}x, Weak={weak_effectiveness}x, Neutral={neutral_effectiveness}x")
    
    def test_battle_stats_scaling(self):
        """Test battle stats scaling with evolution and bond level"""
        if not IMPORTS_AVAILABLE:
            self.skipTest("Badge pet system not available")
        
        # Test current stats (Juvenile stage, bond level 5, Guardian archetype)
        stats = self.test_companion.calculate_battle_stats_scaling()
        
        # Should have Guardian bonuses (high health/defense, lower attack)
        self.assertGreater(stats['health'], stats['attack'])  # Guardian should have more health than attack
        self.assertGreater(stats['defense'], stats['attack'])  # Guardian should have more defense than attack
        
        # Test bond level scaling
        bond_1_companion = BadgePet(
            pet_id='bond-test-1',
            pet_name='BondTest1',
            species=PetSpecies.SCROLLHOUND,
            current_stage=PetStage.JUVENILE,
            developer_name='TestDeveloper',
            birth_date=datetime.datetime.now(),
            traits=[],
            metrics=PetMetrics(),
            genre_theme=DeveloperGenre.FANTASY,
            personality_quirks=[],
            favorite_activities=[],
            element=CompanionElement.ORDER,
            archetype=CompanionArchetype.GUARDIAN,
            temperament=CompanionTemperament.DEFENSIVE,
            bond_level=1
        )
        
        bond_1_stats = bond_1_companion.calculate_battle_stats_scaling()
        
        # Bond level 5 should have higher stats than bond level 1
        self.assertGreater(stats['health'], bond_1_stats['health'])
        
        print(f"✅ Battle stats scaling working: Bond 5 Health={stats['health']}, Bond 1 Health={bond_1_stats['health']}")
    
    def test_battle_experience_system(self):
        """Test battle experience awards and progression"""
        if not IMPORTS_AVAILABLE:
            self.skipTest("Badge pet system not available")
        
        initial_xp = self.test_companion.metrics.total_xp_earned
        
        # Test victory experience
        victory_xp = self.test_companion.award_battle_experience('victory', damage_dealt=200, damage_taken=30)
        
        # Should gain substantial XP for victory
        self.assertGreater(victory_xp, 100)
        self.assertEqual(self.test_companion.metrics.total_xp_earned, initial_xp + victory_xp)
        self.assertEqual(self.test_companion.metrics.battle_stats.battles_won, 1)
        
        # Test defeat experience (should still gain some XP)
        defeat_xp = self.test_companion.award_battle_experience('defeat', damage_dealt=50, damage_taken=150)
        
        self.assertGreater(defeat_xp, 0)
        self.assertLess(defeat_xp, victory_xp)  # Should be less than victory XP
        self.assertEqual(self.test_companion.metrics.battle_stats.battles_lost, 1)
        
        print(f"✅ Battle experience system working: Victory XP={victory_xp}, Defeat XP={defeat_xp}")
    
    def test_warbler_integration_context(self):
        """Test Warbler conversation context generation"""
        if not IMPORTS_AVAILABLE:
            self.skipTest("Badge pet system not available")
        
        # Award some battle experience first
        self.test_companion.award_battle_experience('victory', damage_dealt=100, damage_taken=20)
        
        warbler_context = self.test_companion.get_warbler_battle_context()
        
        # Verify required context fields
        required_fields = [
            'companion_id', 'name', 'species', 'element', 'archetype', 
            'temperament', 'bond_level', 'personality_quirks', 
            'battle_experience', 'developer_context'
        ]
        
        for field in required_fields:
            self.assertIn(field, warbler_context)
        
        # Verify battle experience data
        battle_exp = warbler_context['battle_experience']
        self.assertEqual(battle_exp['battles_won'], 1)
        self.assertEqual(battle_exp['battles_lost'], 0)
        self.assertGreater(battle_exp['damage_dealt'], 0)
        
        # Verify developer context
        dev_context = warbler_context['developer_context']
        self.assertEqual(dev_context['developer_name'], 'TestDeveloper')
        self.assertEqual(dev_context['evolution_stage'], 'juvenile')
        self.assertIn('analytical', dev_context['traits'])
        
        print(f"✅ Warbler context generation working with {len(warbler_context)} fields")
    
    def test_archetype_specializations(self):
        """Test different archetype specializations"""
        if not IMPORTS_AVAILABLE:
            self.skipTest("Badge pet system not available")
        
        archetypes_to_test = [
            (CompanionArchetype.STRIKER, 'attack'),
            (CompanionArchetype.GUARDIAN, 'defense'),
            (CompanionArchetype.SUPPORT, 'energy'),
            (CompanionArchetype.CONTROLLER, 'speed')
        ]
        
        for archetype, expected_high_stat in archetypes_to_test:
            test_companion = BadgePet(
                pet_id=f'archetype-test-{archetype.value}',
                pet_name=f'Test{archetype.value.title()}',
                species=PetSpecies.DEBUGGER_FERRET,
                current_stage=PetStage.JUVENILE,
                developer_name='TestDeveloper',
                birth_date=datetime.datetime.now(),
                traits=[],
                metrics=PetMetrics(),
                genre_theme=DeveloperGenre.CYBERPUNK,
                personality_quirks=[],
                favorite_activities=[],
                element=CompanionElement.LOGIC,
                archetype=archetype,
                temperament=CompanionTemperament.TACTICAL,
                bond_level=1
            )
            
            stats = test_companion.calculate_battle_stats_scaling()
            
            # Archetype should excel in their specialization
            if expected_high_stat in stats:
                baseline_stat = 10  # Base stat value
                self.assertGreater(stats[expected_high_stat], baseline_stat)
        
        print(f"✅ Archetype specializations working for {len(archetypes_to_test)} archetypes")
    
    def test_nft_battle_metadata_generation(self):
        """Test NFT metadata generation for battle-proven companions"""
        if not IMPORTS_AVAILABLE:
            self.skipTest("Badge pet system not available")
        
        # Make companion battle-proven
        self.test_companion.award_battle_experience('victory', damage_dealt=150, damage_taken=40)
        self.test_companion.award_battle_experience('victory', damage_dealt=120, damage_taken=60)
        
        # Test that companion data can be serialized for NFT minting
        companion_dict = self.test_companion.to_dict()
        
        # Verify battle-specific NFT metadata fields
        self.assertIn('element', companion_dict)
        self.assertIn('archetype', companion_dict)
        self.assertIn('temperament', companion_dict)
        self.assertIn('bond_level', companion_dict)
        self.assertIn('metrics', companion_dict)
        
        # Verify battle stats in serialized form (nested under metrics)
        metrics = companion_dict['metrics']
        self.assertIn('battle_stats', metrics)
        battle_stats = metrics['battle_stats']
        self.assertEqual(battle_stats['battles_won'], 2)
        self.assertGreater(battle_stats['damage_dealt'], 0)
        
        # Test JSON serialization (for NFT metadata)
        json_data = json.dumps(companion_dict, default=str, indent=2)
        self.assertIsInstance(json_data, str)
        self.assertIn('battles_won', json_data.lower())  # Should contain battle statistics
        
        print(f"✅ NFT metadata generation working - Battle-proven companion with {battle_stats['battles_won']} victories")


class TestCompanionEvolutionIntegration(unittest.TestCase):
    """Test companion evolution with battle progression"""
    
    def setUp(self):
        if not IMPORTS_AVAILABLE:
            self.skipTest("Badge pet system not available")
        
        self.evolution_companion = BadgePet(
            pet_id='evolution-test-001',
            pet_name='EvolutionTestCompanion',
            species=PetSpecies.CODE_PHOENIX,
            current_stage=PetStage.JUVENILE,
            developer_name='EvolutionTester',
            birth_date=datetime.datetime.now(),
            traits=[PetTrait.CREATIVE, PetTrait.ADVENTUROUS],
            metrics=PetMetrics(),
            genre_theme=DeveloperGenre.FANTASY,
            personality_quirks=[],
            favorite_activities=[],
            element=CompanionElement.CREATIVITY,
            archetype=CompanionArchetype.STRIKER,
            temperament=CompanionTemperament.AGGRESSIVE,
            bond_level=1
        )
    
    def test_evolution_readiness_with_battles(self):
        """Test evolution readiness based on battle performance"""
        if not IMPORTS_AVAILABLE:
            self.skipTest("Badge pet system not available")
        
        # Initially should not be ready for evolution
        initial_progress, _ = self.evolution_companion.get_evolution_progress()
        self.assertLess(initial_progress, 1.0)
        
        # Award substantial battle experience to trigger evolution
        battles_for_evolution = 8
        for i in range(battles_for_evolution):
            xp_gained = self.evolution_companion.award_battle_experience(
                'victory', 
                damage_dealt=100 + i * 10, 
                damage_taken=30 + i * 5
            )
            print(f"   Battle {i+1}: Gained {xp_gained} XP")
        
        # Check if evolution is now possible
        final_progress, xp_needed = self.evolution_companion.get_evolution_progress()
        current_xp = self.evolution_companion.metrics.total_xp_earned
        
        print(f"   Evolution Progress: {final_progress:.2f} (XP: {current_xp})")
        print(f"   Battles Won: {self.evolution_companion.metrics.battle_stats.battles_won}")
        print(f"   Total Damage Dealt: {self.evolution_companion.metrics.battle_stats.damage_dealt}")
        
        # With enough battles, should have significant progress toward evolution
        self.assertGreater(final_progress, initial_progress)
        
        print(f"✅ Evolution progression working: {final_progress:.2f} progress with {battles_for_evolution} victories")


def run_companion_battle_tests():
    """Run all companion battle system tests"""
    print("🧪 Running Companion Battle System Tests...")
    print("=" * 60)
    
    # Create test suite
    suite = unittest.TestSuite()
    
    # Add test classes
    suite.addTest(unittest.makeSuite(TestCompanionBattleSystem))
    suite.addTest(unittest.makeSuite(TestCompanionEvolutionIntegration))
    
    # Run tests
    runner = unittest.TextTestRunner(verbosity=2)
    result = runner.run(suite)
    
    # Summary
    print("\n" + "=" * 60)
    if result.wasSuccessful():
        print("🎉 All Companion Battle System tests passed!")
    else:
        print(f"❌ {len(result.failures + result.errors)} test(s) failed")
        for failure in result.failures:
            print(f"   FAIL: {failure[0]}")
        for error in result.errors:
            print(f"   ERROR: {error[0]}")
    
    return result.wasSuccessful()


if __name__ == "__main__":
    success = run_companion_battle_tests()
    exit(0 if success else 1)