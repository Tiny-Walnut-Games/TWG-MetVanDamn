#!/usr/bin/env python3
"""
Badge Pet System Integration Example
Demonstrates how the pet system integrates with existing TLDA systems

This script shows:
1. Pet adoption workflow
2. XP awarding from TLDL entries
3. Evolution tracking
4. Achievement unlocking
"""

import sys
import os
import json
from pathlib import Path

# Add src to path
sys.path.insert(0, str(Path(__file__).parent.parent / "src" / "DeveloperExperience"))

def demonstrate_pet_system():
    """Demonstrate complete pet system workflow"""
    print("🐾 Badge Pet System Integration Demonstration")
    print("=" * 60)
    
    try:
        from badge_pet_system import BadgePetSystem, PetArchetype, PetLevel
    except ImportError:
        print("⚠️ Pet system not available - showing mock workflow")
        demonstrate_mock_workflow()
        return
    
    # Initialize system
    print("\n1. 🚀 Initializing Pet System...")
    pet_system = BadgePetSystem()
    
    # Example developer
    developer = "integration_demo_dev"
    
    # Step 1: Adopt initial pet
    print(f"\n2. 🏠 {developer} adopts a Scrollhound...")
    success = pet_system.adopt_pet(developer, PetArchetype.SCROLLHOUND)
    if success:
        print("   ✅ Scrollhound adopted successfully!")
    
    # Step 2: Award XP for TLDL entry
    print(f"\n3. 📜 {developer} creates an excellent TLDL entry...")
    pet_system.award_pet_xp(developer, 100, "tldl_entry", "excellent", collaborative=False)
    
    # Step 3: Check pet status
    print(f"\n4. 📊 Checking {developer}'s pet status...")
    profile = pet_system.get_developer_profile(developer)
    if profile:
        pet = profile.active_pets[0]
        print(f"   🐕 Pet Type: {pet.archetype.value}")
        print(f"   📈 XP: {pet.xp}")
        print(f"   🏆 Level: {pet.current_level.value}")
        print(f"   ⚡ Abilities: {', '.join(pet.abilities[:3])}...")
    
    # Step 4: Adopt second pet for diversity bonus
    print(f"\n5. 🐱 {developer} adopts a ChronoCat for diversity...")
    pet_system.adopt_pet(developer, PetArchetype.CHRONOCAT)
    
    # Step 5: Award more XP with diversity bonus
    print(f"\n6. 🤝 {developer} does collaborative work (with diversity bonus)...")
    pet_system.award_pet_xp(developer, 150, "collaborative_session", "good", collaborative=True)
    
    # Step 6: Show final status
    print(f"\n7. 🎯 Final pet status for {developer}:")
    profile = pet_system.get_developer_profile(developer)
    if profile:
        print(f"   👥 Total Pets: {len(profile.active_pets)}")
        print(f"   💎 Total XP: {profile.total_pet_xp}")
        for i, pet in enumerate(profile.active_pets):
            print(f"   🐾 Pet {i+1}: {pet.archetype.value} (Level: {pet.current_level.value}, XP: {pet.xp})")
    
    print(f"\n8. 🔍 Checking evolution ledger...")
    ledger_count = len(pet_system.evolution_ledger)
    print(f"   📋 Total evolution events tracked: {ledger_count}")
    
    print("\n" + "=" * 60)
    print("🎉 Pet system integration demonstration complete!")

def demonstrate_mock_workflow():
    """Show mock workflow when imports aren't available"""
    print("\n📋 Mock Pet System Workflow:")
    print("1. Developer adopts Scrollhound pet")
    print("2. Creates excellent TLDL entry → Gains 150 XP (100 base × 1.5 quality)")
    print("3. Pet evolves from Puppy to Tracker at 500 XP")
    print("4. Adopts ChronoCat for diversity")
    print("5. Collaborative work → Gains 216 XP (150 × 1.2 collab × 1.2 diversity)")
    print("6. Now has 2 pets with total 366 XP combined")
    print("7. Evolution events tracked in cryptographically signed ledger")
    print("\n✅ Mock workflow demonstrates pet progression mechanics")

def show_registry_summary():
    """Show summary of pet registry"""
    print("\n📚 Pet Registry Summary:")
    print("=" * 40)
    
    registry_path = Path("pets/registry.json")
    if not registry_path.exists():
        print("❌ Registry not found")
        return
    
    with open(registry_path, 'r') as f:
        registry = json.load(f)
    
    archetypes = registry.get("archetypes", {})
    print(f"🐾 Available Archetypes: {len(archetypes)}")
    
    for archetype_id, data in archetypes.items():
        print(f"   {data['emoji']} {data['name']}")
        evolution_tree = data.get("evolutionTree", {})
        max_level = max([level_data.get("level", 1) for level_data in evolution_tree.values()])
        print(f"      📈 Max Level: {max_level}")
        specialties = data.get("specialties", [])
        print(f"      🎯 Specialties: {len(specialties)} areas")
        print()

def show_governance_summary():
    """Show governance document summary"""
    print("\n📜 Governance Summary:")
    print("=" * 30)
    
    charter_path = Path("docs/governance/PET-Charter.md")
    if charter_path.exists():
        print("✅ PET Charter: Defines privacy, fairness, and transparency")
        
    formulas_path = Path("docs/registry/evolution-formulas.md")
    if formulas_path.exists():
        print("✅ Evolution Formulas: Public XP calculations and thresholds")
    
    print("\n🔑 Key Principles:")
    print("   🔒 Privacy: Only GitHub handles tracked")
    print("   ⚖️ Fairness: No paid boosts, quality-based progression")
    print("   🔍 Transparency: Open source algorithms")
    print("   🚪 Opt-Out: Instant opt-out available")

if __name__ == "__main__":
    demonstrate_pet_system()
    show_registry_summary()
    show_governance_summary()