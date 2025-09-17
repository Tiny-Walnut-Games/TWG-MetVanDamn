#!/usr/bin/env python3
"""
SpriteForge Test Script - Integration with TLDA Fragment System

This script tests the SpriteForge NFT generation system by creating mock TLDA
ritual events and validating the complete pipeline from ritual → sprite → NFT metadata.

Usage:
    python3 scripts/test_spriteforge.py
    python3 scripts/test_spriteforge.py --faculty-only
    python3 scripts/test_spriteforge.py --count 10 --output data/test_nfts.json
"""

import json
import hashlib
import time
import random
import argparse
import sys
from pathlib import Path
from typing import Dict, List, Any

# Add scripts directory to path for imports
sys.path.append(str(Path(__file__).parent))

class SpriteForgeTestHarness:
    """Test harness for SpriteForge NFT generation system"""
    
    def __init__(self):
        self.ritual_types = [
            "nitpick_hunt",
            "scroll_integrity", 
            "warbler_song",
            "giant_in_well",
            "castle_rite"
        ]
        
        self.faculty_roles = [
            "warbler",
            "creator", 
            "sentinel",
            "archivist",
            "wrangler",
            "scribe",
            "oracle",
            "keeper"
        ]
        
        self.genre_archetypes = {
            "scifi": ["sentinel", "automaton", "wisp"],
            "fantasy": ["familiar", "golem", "drifter"],
            "steampunk": ["automaton", "golem", "warder"],
            "cyberpunk": ["sentinel", "wisp", "homunculus"]
        }
        
        self.test_results = []
    
    def create_mock_ritual_event(self, ritual_type: str = None, faculty: str = None) -> Dict[str, Any]:
        """Create a mock TLDA ritual event for testing"""
        if ritual_type is None:
            ritual_type = random.choice(self.ritual_types)
        
        timestamp = int(time.time() * 1000)
        source = "SpriteForgeTestHarness"
        
        # Generate ritual-specific text
        text = self._generate_ritual_text(ritual_type, faculty)
        
        # Create provenance hash using TLDA methodology
        combined = f"{source}{text}{timestamp}"
        provenance_hash = hashlib.sha256(combined.encode('utf-8')).hexdigest()
        
        return {
            "id": f"TEST-RITUAL-{timestamp}-{random.randint(100, 999)}",
            "source": source,
            "text": text,
            "ritual_type": ritual_type,
            "emotional_weight": random.uniform(0.3, 0.9),
            "tags": self._generate_ritual_tags(ritual_type),
            "unix_millis": timestamp,
            "provenance_hash": provenance_hash
        }
    
    def _generate_ritual_text(self, ritual_type: str, faculty: str = None) -> str:
        """Generate ritual-specific text content"""
        ritual_texts = {
            "nitpick_hunt": [
                "Discovered inconsistency in documentation formatting - the hunt reveals truth",
                "Code review unveiled hidden complexity - another successful hunt",
                "Found edge case in validation logic - the nitpick bears fruit"
            ],
            "scroll_integrity": [
                "Documentation verification complete - integrity maintained",
                "Cross-reference validation successful - the scrolls remain pure",
                "Knowledge consistency check passed - wisdom preserved"
            ],
            "warbler_song": [
                "The Warbler's song echoes through the development halls",
                "Musical patterns emerge from the chaos of compilation",
                "Harmonic resonance detected in the code architecture"
            ],
            "giant_in_well": [
                "Deep architectural decisions stir in the foundation layers",
                "The Giant shifts beneath our abstractions - changes coming",
                "Fundamental forces move in the depths of the system"
            ],
            "castle_rite": [
                "Mind Castle gates opened for sacred code review ceremony",
                "Ritual of architectural contemplation begins in the fortress",
                "The castle's wisdom chambers echo with new understanding"
            ]
        }
        
        base_text = random.choice(ritual_texts.get(ritual_type, ["Generic ritual text"]))
        
        if faculty:
            base_text += f" - {faculty.capitalize()} faculty recognition ritual"
        
        return base_text
    
    def _generate_ritual_tags(self, ritual_type: str) -> List[str]:
        """Generate appropriate tags for ritual type"""
        base_tags = ["test", "spriteforge", "nft_generation"]
        
        ritual_specific_tags = {
            "nitpick_hunt": ["documentation", "quality", "review"],
            "scroll_integrity": ["validation", "consistency", "knowledge"],
            "warbler_song": ["music", "harmony", "faculty"],
            "giant_in_well": ["architecture", "foundation", "deep_changes"],
            "castle_rite": ["ceremony", "wisdom", "contemplation"]
        }
        
        return base_tags + ritual_specific_tags.get(ritual_type, [])
    
    def simulate_sprite_generation(self, ritual_event: Dict[str, Any]) -> Dict[str, Any]:
        """Simulate the sprite generation process"""
        # Generate deterministic seed from ritual
        seed = self._create_sprite_seed(ritual_event)
        
        # Determine token type and characteristics
        token_spec = self._determine_token_spec(ritual_event, seed)
        
        # Simulate sprite generation result
        sprite_result = self._simulate_sprite_result(token_spec, seed)
        
        # Generate NFT metadata
        nft_metadata = self._generate_nft_metadata(token_spec, ritual_event, sprite_result)
        
        return {
            "ritual_event": ritual_event,
            "sprite_seed": seed,
            "token_specification": token_spec,
            "sprite_result": sprite_result,
            "nft_metadata": nft_metadata
        }
    
    def _create_sprite_seed(self, ritual_event: Dict[str, Any]) -> str:
        """Create deterministic sprite seed from ritual event"""
        combined = f"{ritual_event['source']}{ritual_event['text']}{ritual_event['unix_millis']}{ritual_event['ritual_type']}"
        return hashlib.sha256(combined.encode('utf-8')).hexdigest()[:16]
    
    def _determine_token_spec(self, ritual_event: Dict[str, Any], seed: str) -> Dict[str, Any]:
        """Determine token specification from ritual and seed"""
        ritual_type = ritual_event["ritual_type"]
        random_gen = random.Random(seed)
        
        # Faculty ultra-rares
        if ritual_type == "warbler_song":
            return {
                "category": "faculty_ultra_rare",
                "faculty_role": "warbler",
                "archetype": "wisp",
                "genre": "mythic",
                "rarity": "oneofone",
                "size": [32, 32],
                "animation_frames": 12
            }
        
        # Regular genre creatures
        if ritual_type in ["nitpick_hunt", "scroll_integrity"]:
            genre = random_gen.choice(["fantasy", "scifi", "steampunk", "cyberpunk"])
            archetype = random_gen.choice(self.genre_archetypes[genre])
            rarity = random_gen.choice(["common", "uncommon", "rare", "epic"])
            
            return {
                "category": "genre_creature",
                "genre": genre,
                "archetype": archetype,
                "rarity": rarity,
                "size": [24, 24],
                "animation_frames": 4 if rarity in ["common", "uncommon"] else 6
            }
        
        # Locations
        if ritual_type in ["giant_in_well", "castle_rite"]:
            location = "the_well" if ritual_type == "giant_in_well" else "mind_castle_atrium"
            return {
                "category": "location",
                "location_name": location,
                "rarity": "legendary" if location == "the_well" else "epic",
                "size": [48, 32],
                "animation_frames": 8,
                "ambient_effects": True
            }
        
        # Default fallback
        return {
            "category": "genre_creature",
            "genre": "fantasy",
            "archetype": "familiar",
            "rarity": "common",
            "size": [24, 24],
            "animation_frames": 4
        }
    
    def _simulate_sprite_result(self, spec: Dict[str, Any], seed: str) -> Dict[str, Any]:
        """Simulate sprite generation result"""
        return {
            "sprite_sheet_size": [spec["size"][0] * spec["animation_frames"], spec["size"][1]],
            "frame_count": spec["animation_frames"],
            "frame_size": spec["size"],
            "generation_time_ms": random.randint(50, 200),
            "quality_score": random.uniform(0.85, 0.98),
            "color_palette": self._get_color_palette(spec),
            "special_effects": spec.get("ambient_effects", False) or spec.get("rarity") in ["epic", "legendary", "oneofone"]
        }
    
    def _get_color_palette(self, spec: Dict[str, Any]) -> List[str]:
        """Get color palette for token type"""
        if spec["category"] == "faculty_ultra_rare":
            if spec["faculty_role"] == "warbler":
                return ["#3366cc", "#1a99e6", "#66ccff", "#4db3ff"]
            return ["#ffcc00", "#e6b800", "#ffe64d", "#ffffcc"]
        
        genre_palettes = {
            "fantasy": ["#4a3728", "#8b5a3c", "#3a5a3a", "#b84df5"],
            "scifi": ["#1a1a2e", "#16213e", "#00d4ff", "#7fbfff"],
            "steampunk": ["#663b28", "#9b6633", "#cc9933", "#e6cc99"],
            "cyberpunk": ["#0a0a0a", "#1a1a2e", "#ff00ff", "#00ff80"]
        }
        
        return genre_palettes.get(spec.get("genre", "fantasy"), ["#666666", "#999999", "#cccccc", "#ffffff"])
    
    def _generate_nft_metadata(self, spec: Dict[str, Any], ritual_event: Dict[str, Any], sprite_result: Dict[str, Any]) -> Dict[str, Any]:
        """Generate NFT metadata following ERC-721/1155 standards"""
        token_name = self._generate_token_name(spec)
        
        metadata = {
            "name": token_name,
            "description": self._generate_token_description(spec, ritual_event),
            "image": f"ipfs://placeholder/{token_name.replace(' ', '_')}/sheet.png",
            "animation_url": f"ipfs://placeholder/{token_name.replace(' ', '_')}/sheet.mp4",
            "attributes": []
        }
        
        # Core attributes
        metadata["attributes"].extend([
            {"trait_type": "Category", "value": spec["category"].replace("_", " ").title()},
            {"trait_type": "Rarity", "value": spec["rarity"].title()},
            {"trait_type": "Ritual Type", "value": ritual_event["ritual_type"].replace("_", " ").title()},
            {"trait_type": "Emotional Weight", "value": ritual_event["emotional_weight"], "display_type": "number"},
            {"trait_type": "Generation Quality", "value": sprite_result["quality_score"], "display_type": "number"}
        ])
        
        # Type-specific attributes
        if spec["category"] == "faculty_ultra_rare":
            metadata["attributes"].extend([
                {"trait_type": "Faculty Role", "value": spec["faculty_role"].title()},
                {"trait_type": "Uniqueness", "value": "1/1"}
            ])
        elif spec["category"] == "genre_creature":
            metadata["attributes"].extend([
                {"trait_type": "Genre", "value": spec["genre"].title()},
                {"trait_type": "Archetype", "value": spec["archetype"].title()}
            ])
        elif spec["category"] == "location":
            metadata["attributes"].extend([
                {"trait_type": "Location", "value": spec["location_name"].replace("_", " ").title()},
                {"trait_type": "Ambient Effects", "value": "Yes" if spec.get("ambient_effects") else "No"}
            ])
        
        # Technical attributes
        metadata["attributes"].extend([
            {"trait_type": "Animation Frames", "value": spec["animation_frames"], "display_type": "number"},
            {"trait_type": "Sprite Size", "value": f"{spec['size'][0]}x{spec['size'][1]}"},
            {"trait_type": "Special Effects", "value": "Yes" if sprite_result["special_effects"] else "No"}
        ])
        
        return metadata
    
    def _generate_token_name(self, spec: Dict[str, Any]) -> str:
        """Generate token name"""
        if spec["category"] == "faculty_ultra_rare":
            return f"{spec['faculty_role'].title()} • {spec['archetype'].title()} of the Divine"
        elif spec["category"] == "genre_creature":
            genre_adjectives = {
                "fantasy": "Mystical",
                "scifi": "Stellar", 
                "steampunk": "Clockwork",
                "cyberpunk": "Neural"
            }
            adj = genre_adjectives.get(spec["genre"], "Unknown")
            return f"{adj} {spec['archetype'].title()}"
        elif spec["category"] == "location":
            return spec["location_name"].replace("_", " ").title()
        
        return "Unknown Token"
    
    def _generate_token_description(self, spec: Dict[str, Any], ritual_event: Dict[str, Any]) -> str:
        """Generate token description"""
        ritual_date = time.strftime("%Y-%m-%d", time.localtime(ritual_event["unix_millis"] / 1000))
        
        if spec["category"] == "faculty_ultra_rare":
            return f"Faculty ultra-rare 1/1. Born from the {ritual_event['ritual_type'].replace('_', ' ')} ritual on {ritual_date}. Forged from TLDA verdicts and faculty wisdom."
        elif spec["category"] == "genre_creature":
            return f"{spec['genre'].title()} creature forged from TLDA ritual validation. Emotional weight: {ritual_event['emotional_weight']:.2f}. A {spec['rarity']} {spec['archetype']} from the {spec['genre']} realm."
        elif spec["category"] == "location":
            return f"Sacred location from the TLDA mythos: {spec['location_name'].replace('_', ' ').title()}. Witnessed during {ritual_event['ritual_type'].replace('_', ' ')} ritual on {ritual_date}."
        
        return "Token generated from TLDA ritual system."
    
    def run_test_suite(self, count: int = 10, faculty_only: bool = False) -> List[Dict[str, Any]]:
        """Run complete test suite"""
        print(f"🎨 SpriteForge Test Suite - Generating {count} test tokens")
        print("=" * 60)
        
        for i in range(count):
            print(f"Test {i+1}/{count}: ", end="")
            
            if faculty_only:
                # Test only faculty ultra-rares
                faculty = random.choice(self.faculty_roles)
                ritual_event = self.create_mock_ritual_event("warbler_song", faculty)
            else:
                # Test random ritual types
                ritual_event = self.create_mock_ritual_event()
            
            print(f"Ritual: {ritual_event['ritual_type']}", end=" → ")
            
            try:
                result = self.simulate_sprite_generation(ritual_event)
                self.test_results.append(result)
                
                token_name = result["nft_metadata"]["name"]
                rarity = result["token_specification"]["rarity"]
                print(f"✅ {token_name} ({rarity})")
                
            except Exception as e:
                print(f"❌ Failed: {e}")
                continue
        
        print("\n" + "=" * 60)
        self._print_test_summary()
        
        return self.test_results
    
    def _print_test_summary(self):
        """Print test results summary"""
        if not self.test_results:
            print("❌ No successful tests")
            return
        
        print(f"✅ Generated {len(self.test_results)} tokens successfully")
        
        # Count by category
        categories = {}
        rarities = {}
        
        for result in self.test_results:
            spec = result["token_specification"]
            category = spec["category"]
            rarity = spec["rarity"]
            
            categories[category] = categories.get(category, 0) + 1
            rarities[rarity] = rarities.get(rarity, 0) + 1
        
        print("\nCategories:")
        for category, count in categories.items():
            print(f"  • {category.replace('_', ' ').title()}: {count}")
        
        print("\nRarities:")
        for rarity, count in rarities.items():
            print(f"  • {rarity.title()}: {count}")
        
        # Calculate average quality
        avg_quality = sum(r["sprite_result"]["quality_score"] for r in self.test_results) / len(self.test_results)
        print(f"\nAverage Generation Quality: {avg_quality:.3f}")
    
    def save_results(self, output_path: str):
        """Save test results to JSON file"""
        if not self.test_results:
            print("❌ No results to save")
            return
        
        output_data = {
            "test_metadata": {
                "generated_at": time.strftime("%Y-%m-%d %H:%M:%S UTC", time.gmtime()),
                "total_tokens": len(self.test_results),
                "test_version": "1.0.0"
            },
            "results": self.test_results
        }
        
        Path(output_path).parent.mkdir(parents=True, exist_ok=True)
        
        with open(output_path, 'w') as f:
            json.dump(output_data, f, indent=2, default=str)
        
        print(f"💾 Results saved to: {output_path}")

def main():
    parser = argparse.ArgumentParser(description="Test SpriteForge NFT generation system")
    parser.add_argument("--count", type=int, default=10, help="Number of test tokens to generate")
    parser.add_argument("--faculty-only", action="store_true", help="Test only Faculty ultra-rares")
    parser.add_argument("--output", type=str, help="Output file for test results")
    parser.add_argument("--verbose", action="store_true", help="Enable verbose output")
    
    args = parser.parse_args()
    
    # Initialize test harness
    test_harness = SpriteForgeTestHarness()
    
    # Run tests
    results = test_harness.run_test_suite(args.count, args.faculty_only)
    
    # Save results if requested
    if args.output:
        test_harness.save_results(args.output)
    
    # Show sample metadata
    if args.verbose and results:
        print("\n" + "=" * 60)
        print("Sample NFT Metadata:")
        print(json.dumps(results[0]["nft_metadata"], indent=2))

if __name__ == "__main__":
    main()