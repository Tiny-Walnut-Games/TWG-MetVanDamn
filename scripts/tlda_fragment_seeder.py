#!/usr/bin/env python3
"""
TLDA Fragment Seeder - Generate 100 TLDA Fragments from Project Lore

This script generates 100 TLDA fragments that simulate real development events,
rituals, and emotional states from various Tiny Walnut Games projects. These 
entries will be used to seed the initial Warbler Cloud and test Giant compression,
evaporation, and selector synthesis.

Format:
{
  "id": "TLDA-MOCK-###",
  "source": "Seeder", 
  "text": "Narrative line here...",
  "emotional_weight": 0.3–0.9,
  "tags": ["tag1", "tag2", ...],
  "unix_millis": timestamp,
  "provenance_hash": "sha256 of source+text+timestamp"
}

Usage:
    python3 scripts/tlda_fragment_seeder.py
    python3 scripts/tlda_fragment_seeder.py --output data/tlda_fragments.json
    python3 scripts/tlda_fragment_seeder.py --count 50
"""

import json
import hashlib
import time
import random
import argparse
from pathlib import Path
from typing import List, Dict, Any

class TLDAFragmentSeeder:
    """Generate TLDA fragments based on Tiny Walnut Games project lore."""
    
    def __init__(self):
        self.base_timestamp = int(time.time() * 1000)
        self.fragment_templates = self._initialize_templates()
        
    def _initialize_templates(self) -> Dict[str, List[Dict[str, Any]]]:
        """Initialize fragment templates based on project lore."""
        return {
            "validator_rituals": [
                {
                    "text": "Validator failed due to 'Discoveries' vs 'Discovery'. Contributor annotated the breach.",
                    "emotional_weight_range": (0.7, 0.8),
                    "tags": ["validator", "scroll-integrity", "ritual"]
                },
                {
                    "text": "TLDL symbolic linter caught parse errors in Python files. Expected behavior confirmed.",
                    "emotional_weight_range": (0.4, 0.6),
                    "tags": ["validator", "symbolic-linter", "expected"]
                },
                {
                    "text": "Debug overlay validation returned 85.7% health score. C# parsing issues acknowledged.",
                    "emotional_weight_range": (0.5, 0.7),
                    "tags": ["validator", "debug-overlay", "health-score"]
                },
                {
                    "text": "Section naming validator triggered on 'Discoveries' plural form. Manual correction required.",
                    "emotional_weight_range": (0.6, 0.8),
                    "tags": ["validator", "naming", "manual-fix"]
                },
                {
                    "text": "Chronicle Keeper validation passed with warnings about entry ID format standards.",
                    "emotional_weight_range": (0.3, 0.5),
                    "tags": ["validator", "chronicle-keeper", "warnings"]
                }
            ],
            "giant_magma": [
                {
                    "text": "The Giant stomped a broken refactor. Magma pressure rose. Castle corridor sealed.",
                    "emotional_weight_range": (0.8, 0.9),
                    "tags": ["giant", "magma", "castle"]
                },
                {
                    "text": "Sediment compression created new strata layer. Molten glyphs await evaporation cycle.",
                    "emotional_weight_range": (0.4, 0.6),
                    "tags": ["giant", "sediment", "compression"]
                },
                {
                    "text": "Giant compressor processed 47 fragments into 3 clusters. Tetrino slam successful.",
                    "emotional_weight_range": (0.5, 0.7),
                    "tags": ["giant", "compression", "tetrino-slam"]
                },
                {
                    "text": "Magma store overflow triggered emergency melt budget protocols. Heat dissipation required.",
                    "emotional_weight_range": (0.7, 0.9),
                    "tags": ["magma", "overflow", "emergency"]
                },
                {
                    "text": "Molten glyph retirement ceremony completed. Ancient knowledge preserved in liquid form.",
                    "emotional_weight_range": (0.3, 0.5),
                    "tags": ["magma", "retirement", "preservation"]
                }
            ],
            "castle_memory": [
                {
                    "text": "Castle memory nodes reached thermal equilibrium. Room heat distribution stabilized.",
                    "emotional_weight_range": (0.3, 0.5),
                    "tags": ["castle", "memory", "thermal"]
                },
                {
                    "text": "Corridor navigation failed during castle infusion. Mist lines redirected to backup chambers.",
                    "emotional_weight_range": (0.6, 0.8),
                    "tags": ["castle", "navigation", "backup"]
                },
                {
                    "text": "Memory castle graph expanded with 12 new concept nodes. Relationship mapping updated.",
                    "emotional_weight_range": (0.4, 0.6),
                    "tags": ["castle", "graph", "expansion"]
                },
                {
                    "text": "Castle's top rooms rotated based on heat metrics. Knowledge access patterns shifted.",
                    "emotional_weight_range": (0.5, 0.7),
                    "tags": ["castle", "rotation", "heat-metrics"]
                },
                {
                    "text": "Architectural integrity breach detected in east wing. Emergency sealing protocols engaged.",
                    "emotional_weight_range": (0.8, 0.9),
                    "tags": ["castle", "integrity", "emergency"]
                }
            ],
            "faculty_onboarding": [
                {
                    "text": "New faculty member completed TLDA initiation ritual. Scroll access permissions granted.",
                    "emotional_weight_range": (0.3, 0.5),
                    "tags": ["faculty", "onboarding", "initiation"]
                },
                {
                    "text": "Contributor breakthrough: First successful TLDL entry submission. Chronicle Keeper notified.",
                    "emotional_weight_range": (0.4, 0.6),
                    "tags": ["faculty", "breakthrough", "first-submission"]
                },
                {
                    "text": "Faculty orientation failed at badge verification stage. Cryptographic signature mismatch.",
                    "emotional_weight_range": (0.7, 0.8),
                    "tags": ["faculty", "orientation", "verification-failed"]
                },
                {
                    "text": "Senior developer ascended to Oracle status. Wisdom transmission protocols activated.",
                    "emotional_weight_range": (0.2, 0.4),
                    "tags": ["faculty", "ascension", "oracle"]
                },
                {
                    "text": "Onboarding wizard crashed during Unity package installation. Manual intervention required.",
                    "emotional_weight_range": (0.6, 0.8),
                    "tags": ["faculty", "wizard", "unity-crash"]
                }
            ],
            "pets_agents": [
                {
                    "text": "Pet evolution triggered by significant IDEA charter milestone. New abilities unlocked.",
                    "emotional_weight_range": (0.3, 0.5),
                    "tags": ["pets", "evolution", "idea-charter"]
                },
                {
                    "text": "Local agent detected cognitive overflow event. Sluice activation recommended.",
                    "emotional_weight_range": (0.5, 0.7),
                    "tags": ["agents", "cognitive", "overflow"]
                },
                {
                    "text": "Badge pet system registered new sponsor. Cryptographic verification completed successfully.",
                    "emotional_weight_range": (0.3, 0.5),
                    "tags": ["pets", "badge", "sponsor"]
                },
                {
                    "text": "Pet naming ceremony interrupted by validator breach. Ritual postponed indefinitely.",
                    "emotional_weight_range": (0.7, 0.9),
                    "tags": ["pets", "naming", "validator-breach"]
                },
                {
                    "text": "Agent telemetry showed 127% efficiency gain after Chronicle Keeper integration.",
                    "emotional_weight_range": (0.2, 0.4),
                    "tags": ["agents", "telemetry", "efficiency"]
                }
            ],
            "chronicle_automation": [
                {
                    "text": "Chronicle Keeper automation triggered capsule scroll generation. Archive continuity maintained.",
                    "emotional_weight_range": (0.3, 0.5),
                    "tags": ["chronicle", "automation", "capsule"]
                },
                {
                    "text": "Monthly TLDL archive generation failed. Decision index population incomplete.",
                    "emotional_weight_range": (0.7, 0.8),
                    "tags": ["chronicle", "archive", "generation-failed"]
                },
                {
                    "text": "Lore integration detected semantic drift in knowledge artifacts. Recalibration scheduled.",
                    "emotional_weight_range": (0.5, 0.7),
                    "tags": ["chronicle", "lore", "semantic-drift"]
                },
                {
                    "text": "Archive wall continuity ritual completed successfully. Historical integrity preserved.",
                    "emotional_weight_range": (0.2, 0.4),
                    "tags": ["chronicle", "archive-wall", "continuity"]
                },
                {
                    "text": "Chronicle Keeper experienced temporal paradox during timestamp reconciliation.",
                    "emotional_weight_range": (0.8, 0.9),
                    "tags": ["chronicle", "temporal", "paradox"]
                }
            ],
            "devtimetravel": [
                {
                    "text": "DTT Vault brick layer compaction initiated. Tetrino slam algorithm engaged.",
                    "emotional_weight_range": (0.4, 0.6),
                    "tags": ["dtt", "vault", "compaction"]
                },
                {
                    "text": "DevTimeTravel snapshot restored to quarantine branch. Safety protocols observed.",
                    "emotional_weight_range": (0.5, 0.7),
                    "tags": ["dtt", "snapshot", "quarantine"]
                },
                {
                    "text": "Content addressing collision detected in brick storage. Reference counting updated.",
                    "emotional_weight_range": (0.6, 0.8),
                    "tags": ["dtt", "addressing", "collision"]
                },
                {
                    "text": "Layer promotion ceremony elevated 23 snapshots to cold storage. Archive depth increased.",
                    "emotional_weight_range": (0.3, 0.5),
                    "tags": ["dtt", "promotion", "cold-storage"]
                },
                {
                    "text": "Vault integrity verification failed on Layer-2 manifests. Emergency audit triggered.",
                    "emotional_weight_range": (0.8, 0.9),
                    "tags": ["dtt", "integrity", "audit"]
                }
            ],
            "warbler_cloud": [
                {
                    "text": "Warbler cloud humidity reached critical threshold. Evaporation cycle acceleration required.",
                    "emotional_weight_range": (0.6, 0.8),
                    "tags": ["warbler", "humidity", "critical"]
                },
                {
                    "text": "Mist line generation produced 47 ephemeral knowledge fragments. Castle infusion pending.",
                    "emotional_weight_range": (0.4, 0.6),
                    "tags": ["warbler", "mist-lines", "knowledge"]
                },
                {
                    "text": "Style bias calibration completed. Mythic weight: 0.8, Technical precision: 0.6.",
                    "emotional_weight_range": (0.3, 0.5),
                    "tags": ["warbler", "style-bias", "calibration"]
                },
                {
                    "text": "Cloud formation anomaly detected during selector synthesis. Pattern recognition failed.",
                    "emotional_weight_range": (0.7, 0.9),
                    "tags": ["warbler", "anomaly", "selector"]
                },
                {
                    "text": "Evaporation engine achieved 94% efficiency rating. Thermal optimization successful.",
                    "emotional_weight_range": (0.2, 0.4),
                    "tags": ["warbler", "evaporation", "optimization"]
                }
            ],
            "unity_integration": [
                {
                    "text": "Unity Editor TLDL Scribe Window crashed during markdown rendering. Memory leak suspected.",
                    "emotional_weight_range": (0.7, 0.8),
                    "tags": ["unity", "editor", "crash"]
                },
                {
                    "text": "Assembly definition validation passed for LivingDevAgent.Runtime. Package integrity confirmed.",
                    "emotional_weight_range": (0.3, 0.5),
                    "tags": ["unity", "assembly", "validation"]
                },
                {
                    "text": "Image insertion workflow failed during markdown cursor positioning. Manual correction applied.",
                    "emotional_weight_range": (0.6, 0.8),
                    "tags": ["unity", "image", "workflow"]
                },
                {
                    "text": "Unity 2022.3 compatibility verified across all TLDA components. Upgrade path secured.",
                    "emotional_weight_range": (0.2, 0.4),
                    "tags": ["unity", "compatibility", "upgrade"]
                },
                {
                    "text": "ScribeUtils metadata parsing encountered unexpected YAML structure. Fallback engaged.",
                    "emotional_weight_range": (0.5, 0.7),
                    "tags": ["unity", "scribe", "yaml"]
                }
            ],
            "licensing_doctrine": [
                {
                    "text": "Licensing compliance audit revealed 3 dependency violations. Legal review initiated.",
                    "emotional_weight_range": (0.7, 0.9),
                    "tags": ["licensing", "compliance", "violations"]
                },
                {
                    "text": "Open source doctrine validation passed. MIT license terms properly preserved.",
                    "emotional_weight_range": (0.2, 0.4),
                    "tags": ["licensing", "doctrine", "mit"]
                },
                {
                    "text": "Contributor license agreement verification failed. Signature authenticity questioned.",
                    "emotional_weight_range": (0.6, 0.8),
                    "tags": ["licensing", "cla", "verification"]
                },
                {
                    "text": "License header injection completed across 247 source files. Copyright notices updated.",
                    "emotional_weight_range": (0.3, 0.5),
                    "tags": ["licensing", "headers", "copyright"]
                },
                {
                    "text": "Third-party attribution scanning detected unlicensed code fragment. Removal scheduled.",
                    "emotional_weight_range": (0.8, 0.9),
                    "tags": ["licensing", "attribution", "unlicensed"]
                }
            ]
        }
    
    def _generate_provenance_hash(self, source: str, text: str, timestamp: int) -> str:
        """Generate SHA-256 hash of source+text+timestamp."""
        combined = f"{source}{text}{timestamp}"
        return hashlib.sha256(combined.encode('utf-8')).hexdigest()
    
    def _select_random_template(self) -> Dict[str, Any]:
        """Select a random template from all categories."""
        category = random.choice(list(self.fragment_templates.keys()))
        template = random.choice(self.fragment_templates[category])
        return template
    
    def _generate_fragment(self, fragment_id: int) -> Dict[str, Any]:
        """Generate a single TLDA fragment."""
        template = self._select_random_template()
        
        # Generate timestamp with some variation
        timestamp = self.base_timestamp + (fragment_id * 1000) + random.randint(-500, 500)
        
        # Generate emotional weight within template range
        min_weight, max_weight = template["emotional_weight_range"]
        emotional_weight = round(random.uniform(min_weight, max_weight), 2)
        
        # Add some variation to tags
        base_tags = template["tags"].copy()
        if random.random() < 0.3:  # 30% chance to add extra tag
            extra_tags = ["urgent", "routine", "complex", "simple", "milestone", "bugfix", "feature", "refactor"]
            base_tags.append(random.choice(extra_tags))
        
        fragment = {
            "id": f"TLDA-MOCK-{fragment_id:03d}",
            "source": "Seeder",
            "text": template["text"],
            "emotional_weight": emotional_weight,
            "tags": base_tags,
            "unix_millis": timestamp,
            "provenance_hash": self._generate_provenance_hash("Seeder", template["text"], timestamp)
        }
        
        return fragment
    
    def generate_fragments(self, count: int = 100) -> List[Dict[str, Any]]:
        """Generate the specified number of TLDA fragments."""
        fragments = []
        
        print(f"🧠 Generating {count} TLDA fragments from project lore...")
        
        for i in range(1, count + 1):
            fragment = self._generate_fragment(i)
            fragments.append(fragment)
            
            if i % 10 == 0:
                print(f"   Generated {i}/{count} fragments...")
        
        print(f"✅ Successfully generated {len(fragments)} TLDA fragments")
        return fragments
    
    def save_fragments(self, fragments: List[Dict[str, Any]], output_path: str):
        """Save fragments to JSON file."""
        output_file = Path(output_path)
        output_file.parent.mkdir(parents=True, exist_ok=True)
        
        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(fragments, f, indent=2, ensure_ascii=False)
        
        print(f"💾 Saved {len(fragments)} fragments to {output_path}")
    
    def print_sample_fragments(self, fragments: List[Dict[str, Any]], count: int = 5):
        """Print sample fragments for verification."""
        print(f"\n📋 Sample TLDA Fragments (showing {count} of {len(fragments)}):")
        print("=" * 70)
        
        for i, fragment in enumerate(fragments[:count]):
            print(f"\n{i+1}. {fragment['id']}")
            print(f"   Text: {fragment['text']}")
            print(f"   Emotional Weight: {fragment['emotional_weight']}")
            print(f"   Tags: {', '.join(fragment['tags'])}")
            print(f"   Timestamp: {fragment['unix_millis']}")
            print(f"   Hash: {fragment['provenance_hash'][:16]}...")

def main():
    """Main entry point for TLDA fragment generation."""
    parser = argparse.ArgumentParser(description="Generate TLDA fragments from project lore")
    parser.add_argument("--output", "-o", default="data/tlda_fragments.json", 
                       help="Output file path (default: data/tlda_fragments.json)")
    parser.add_argument("--count", "-c", type=int, default=100,
                       help="Number of fragments to generate (default: 100)")
    parser.add_argument("--sample", "-s", type=int, default=5,
                       help="Number of sample fragments to display (default: 5)")
    
    args = parser.parse_args()
    
    print("🧙‍♂️ TLDA Fragment Seeder - Warbler Cloud Bootstrap")
    print("=" * 60)
    
    # Generate fragments
    seeder = TLDAFragmentSeeder()
    fragments = seeder.generate_fragments(args.count)
    
    # Save to file
    seeder.save_fragments(fragments, args.output)
    
    # Display samples
    seeder.print_sample_fragments(fragments, args.sample)
    
    print(f"\n🌫️ Fragments ready for Warbler Cloud seeding!")
    print(f"📍 Location: {args.output}")
    print(f"📊 Count: {len(fragments)} fragments")
    print(f"🎯 Ready for Giant compression, evaporation, and selector synthesis testing")

if __name__ == "__main__":
    main()