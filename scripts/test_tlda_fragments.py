#!/usr/bin/env python3
"""
Test TLDA Fragments with Warbler Cloud System

This script tests the generated TLDA fragments by running them through
the complete Cognitive Geo-Thermal Lore Engine cycle to validate that
they work correctly with Giant compression, evaporation, and selector synthesis.

Usage:
    python3 scripts/test_tlda_fragments.py
    python3 scripts/test_tlda_fragments.py --fragments data/tlda_fragments.json
    python3 scripts/test_tlda_fragments.py --count 20
"""

import sys
import os
import json
import time
import argparse
from pathlib import Path

# Add parent directory to path for engine imports
sys.path.insert(0, str(Path(__file__).parent.parent))

from engine import (
    GiantCompressor, MeltLayer, EvaporationEngine, CastleGraph, 
    Selector, Governance, CycleTelemetry
)
from engine.giant_compressor import SedimentStore
from engine.melt_layer import MagmaStore
from engine.evaporation import CloudStore
from engine.telemetry import aggregate_cycle_metrics, create_cycle_summary

def load_tlda_fragments(fragments_path: str, count: int = None) -> list:
    """Load TLDA fragments from JSON file."""
    try:
        with open(fragments_path, 'r', encoding='utf-8') as f:
            fragments = json.load(f)
        
        if count:
            fragments = fragments[:count]
        
        print(f"📂 Loaded {len(fragments)} TLDA fragments from {fragments_path}")
        return fragments
    except Exception as e:
        print(f"❌ Failed to load fragments: {e}")
        return []

def convert_tlda_to_raw_fragments(tlda_fragments: list) -> list:
    """Convert TLDA fragments to raw fragment format expected by Giant."""
    raw_fragments = []
    
    for tlda in tlda_fragments:
        raw_fragment = {
            "id": tlda["id"],
            "text": tlda["text"],
            "emotional_weight": tlda.get("emotional_weight", 0.5),
            "tags": tlda.get("tags", []),
            "timestamp": tlda.get("unix_millis", int(time.time() * 1000)),
            "source": tlda.get("source", "Unknown")
        }
        raw_fragments.append(raw_fragment)
    
    return raw_fragments

def test_warbler_cloud_with_tlda_fragments(tlda_fragments: list):
    """Test the complete Warbler Cloud system with TLDA fragments."""
    print("🔥 Testing TLDA Fragments with Cognitive Geo-Thermal Lore Engine")
    print("=" * 70)
    
    # Initialize telemetry
    cycle_id = f"tlda_test_cycle_{int(time.time())}"
    telemetry = CycleTelemetry(cycle_id=cycle_id)
    
    # Initialize stores
    sediment_store = SedimentStore()
    magma_store = MagmaStore()
    cloud_store = CloudStore()
    castle_graph = CastleGraph()
    governance = Governance()
    
    # Initialize engine components
    giant = GiantCompressor(sediment_store)
    melt_layer = MeltLayer(magma_store)
    evaporator = EvaporationEngine(magma_store, cloud_store)
    selector = Selector(castle_graph, cloud_store, governance)
    
    try:
        # Convert TLDA fragments to raw format
        raw_fragments = convert_tlda_to_raw_fragments(tlda_fragments)
        
        # 1. STOMP: Giant compression
        print(f"\n🦣 Phase 1: Giant Stomping ({len(raw_fragments)} TLDA fragments)")
        stomp_result = giant.stomp(raw_fragments)
        print(f"   Result: {stomp_result['clusters']} clusters, {stomp_result['elapsed_ms']:.1f}ms")
        
        # 2. RETIRE: Cluster melting into glyphs
        print("\n🌋 Phase 2: Melt Layer Retirement (TLDA Clusters → Molten Glyphs)")
        molten_glyphs = []
        for stratum in sediment_store.strata:
            for cluster in stratum["clusters"]:
                glyph = melt_layer.retire_cluster(cluster)
                molten_glyphs.append(glyph)
        
        print(f"   Result: {len(molten_glyphs)} molten glyphs created from TLDA data")
        
        # 3. EVAPORATE: Glyph → Mist Lines
        print("\n☁️ Phase 3: Evaporation (TLDA Glyphs → Mist Lines)")
        mist_lines = evaporator.evaporate(limit=10)  # Process more mist lines for testing
        mist_count = len(mist_lines)
        print(f"   Result: {mist_count} mist lines, humidity {cloud_store.humidity_index:.2f}")
        
        # 4. INFUSE: Mist → Castle Nodes
        print("\n🏰 Phase 4: Castle Infusion (TLDA Mist → Memory Nodes)")
        infusion_result = castle_graph.infuse(mist_lines)
        print(f"   Result: {infusion_result['infused_count']} mist lines infused, {infusion_result['total_nodes']} total nodes")
        
        # 5. SELECT: Assemble prompt
        print("\n🎭 Phase 5: Selector Assembly (TLDA Knowledge → Response)")
        prompt_scaffold = selector.assemble_prompt("Testing TLDA fragment integration with Warbler Cloud")
        response = selector.respond(prompt_scaffold)
        print(f"   Result: Response generated with {len(prompt_scaffold['voices'])} voices")
        print(f"   Sample: {response['response_text'][:100]}...")
        
        # 6. GOVERNANCE: Score cycle
        print("\n⚖️ Phase 6: Governance Scoring")
        cycle_outputs = {
            "molten_glyphs": molten_glyphs,
            "mist_count": mist_count,
            "top_rooms": castle_graph.get_top_rooms(10)
        }
        
        # Complete telemetry
        telemetry.finish_cycle()
        components = {
            "stomp_result": stomp_result,
            "molten_glyphs": molten_glyphs,
            "mist_count": mist_count,
            "infusion_result": infusion_result,
            "top_rooms": cycle_outputs["top_rooms"]
        }
        
        # Governance scoring
        cycle_summary = create_cycle_summary(telemetry, cycle_outputs)
        governance_result = governance.score_cycle(cycle_summary["cycle_report"])
        components["governance_result"] = governance_result
        
        print(f"   Result: Quality score {governance_result['score']:.2f}, {governance_result['assessment']}")
        
        # Analyze TLDA-specific results
        print("\n" + "=" * 70)
        print("🎯 TLDA Fragment Analysis:")
        print("=" * 70)
        
        # Tag analysis
        all_tags = []
        emotional_weights = []
        for fragment in tlda_fragments:
            all_tags.extend(fragment.get('tags', []))
            emotional_weights.append(fragment.get('emotional_weight', 0.5))
        
        tag_counts = {}
        for tag in all_tags:
            tag_counts[tag] = tag_counts.get(tag, 0) + 1
        
        print(f"📊 Fragment Statistics:")
        print(f"   • Total Fragments: {len(tlda_fragments)}")
        print(f"   • Average Emotional Weight: {sum(emotional_weights)/len(emotional_weights):.2f}")
        print(f"   • Unique Tags: {len(set(all_tags))}")
        print(f"   • Most Common Tags: {sorted(tag_counts.items(), key=lambda x: x[1], reverse=True)[:5]}")
        
        print(f"\n🌫️ Warbler Cloud Results:")
        print(f"   • Molten Glyphs: {len(molten_glyphs)}")
        print(f"   • Mist Lines: {mist_count}")
        print(f"   • Castle Nodes: {len(castle_graph.nodes)}")
        print(f"   • Top Concepts: {[room['concept_id'] for room in cycle_outputs['top_rooms'][:3]]}")
        
        # Final JSON summary
        output = {
            "test_summary": {
                "tlda_fragments_processed": len(tlda_fragments),
                "molten_glyphs_created": len(molten_glyphs),
                "mist_lines_generated": mist_count,
                "castle_nodes_total": len(castle_graph.nodes),
                "governance_score": governance_result['score']
            },
            "cycle_report": cycle_summary["cycle_report"],
            "top_concepts": [room['concept_id'] for room in cycle_outputs['top_rooms'][:5]],
            "emotional_analysis": {
                "average_weight": sum(emotional_weights)/len(emotional_weights),
                "weight_range": [min(emotional_weights), max(emotional_weights)],
                "most_common_tags": sorted(tag_counts.items(), key=lambda x: x[1], reverse=True)[:10]
            }
        }
        
        print("\n" + "=" * 70)
        print("📋 Final Test Results JSON:")
        print("=" * 70)
        print(json.dumps(output, indent=2))
        
        return True
        
    except Exception as e:
        print(f"\n❌ TLDA fragment testing failed: {e}")
        import traceback
        traceback.print_exc()
        return False

def main():
    """Main entry point for TLDA fragment testing."""
    parser = argparse.ArgumentParser(description="Test TLDA fragments with Warbler Cloud")
    parser.add_argument("--fragments", "-f", default="data/tlda_fragments.json",
                       help="Path to TLDA fragments JSON file")
    parser.add_argument("--count", "-c", type=int, 
                       help="Number of fragments to test (default: all)")
    
    args = parser.parse_args()
    
    print("🧙‍♂️ TLDA Fragment Warbler Cloud Integration Test")
    print("=" * 60)
    
    # Load fragments
    fragments = load_tlda_fragments(args.fragments, args.count)
    if not fragments:
        print("❌ No fragments to test!")
        return False
    
    # Test with Warbler Cloud
    success = test_warbler_cloud_with_tlda_fragments(fragments)
    
    if success:
        print("\n✅ TLDA fragment integration test successful!")
        print("🌫️ Fragments are ready for Warbler Cloud bootstrap!")
    else:
        print("\n❌ TLDA fragment integration test failed!")
    
    return success

if __name__ == "__main__":
    success = main()
    sys.exit(0 if success else 1)