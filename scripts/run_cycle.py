#!/usr/bin/env python3
"""
Cognitive Geo-Thermal Lore Engine - Demonstration Cycle Runner

This script demonstrates the complete Giant → Magma → Cloud → Castle stack.
Runs: stomp → retire → evaporate → infuse → (optional respond)
"""

import sys
import os
import json
import time
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

def create_demo_fragments():
    """Create demo raw fragments for the cycle."""
    return [
        {"id": "frag_1", "text": "Implementing advanced debugging tools for better developer experience"},
        {"id": "frag_2", "text": "The warbler cloud formation shows promising patterns in knowledge distillation"},
        {"id": "frag_3", "text": "Memory castle architecture requires careful consideration of heat distribution"},
        {"id": "frag_4", "text": "Evaporation processes transform raw data into actionable insights"},
        {"id": "frag_5", "text": "Giant compression algorithms optimize storage while preserving semantic meaning"},
    ]

def run_demonstration_cycle():
    """Run complete demonstration cycle of the Geo-Thermal Lore Engine."""
    print("🔥 Starting Cognitive Geo-Thermal Lore Engine Demonstration Cycle")
    print("=" * 70)
    
    # Initialize telemetry
    cycle_id = f"demo_cycle_{int(time.time())}"
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
        # 1. STOMP: Giant compression
        print("\n🦣 Phase 1: Giant Stomping (Raw Fragment Compression)")
        raw_fragments = create_demo_fragments()
        print(f"   Input: {len(raw_fragments)} raw fragments")
        
        stomp_result = giant.stomp(raw_fragments)
        print(f"   Result: {stomp_result['clusters']} clusters, {stomp_result['elapsed_ms']:.1f}ms")
        
        # 2. RETIRE: Cluster melting into glyphs
        print("\n🌋 Phase 2: Melt Layer Retirement (Cluster → Molten Glyphs)")
        molten_glyphs = []
        for stratum in sediment_store.strata:
            for cluster in stratum["clusters"]:
                glyph = melt_layer.retire_cluster(cluster)
                molten_glyphs.append(glyph)
        
        print(f"   Result: {len(molten_glyphs)} molten glyphs created")
        
        # 3. EVAPORATE: Glyph → Mist Lines
        print("\n☁️ Phase 3: Evaporation (Molten Glyphs → Mist Lines)")
        mist_lines = evaporator.evaporate(limit=3)
        mist_count = len(mist_lines)
        print(f"   Result: {mist_count} mist lines, humidity {cloud_store.humidity_index:.2f}")
        
        # 4. INFUSE: Mist → Castle Nodes
        print("\n🏰 Phase 4: Castle Infusion (Mist Lines → Memory Nodes)")
        infusion_result = castle_graph.infuse(mist_lines)
        print(f"   Result: {infusion_result['infused_count']} mist lines infused, {infusion_result['total_nodes']} total nodes")
        
        # 5. SELECT: Assemble prompt (optional)
        print("\n🎭 Phase 5: Selector Assembly (Optional Response Generation)")
        prompt_scaffold = selector.assemble_prompt("Demo context for testing")
        response = selector.respond(prompt_scaffold)
        print(f"   Result: Response generated with {len(prompt_scaffold['voices'])} voices")
        
        # 6. GOVERNANCE: Score cycle
        print("\n⚖️ Phase 6: Governance Scoring")
        cycle_outputs = {
            "molten_glyphs": molten_glyphs,
            "mist_count": mist_count,
            "top_rooms": castle_graph.get_top_rooms(5)
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
        
        # Final aggregation
        final_metrics = aggregate_cycle_metrics(telemetry, components)
        final_summary = create_cycle_summary(telemetry, cycle_outputs)
        
        print("\n" + "=" * 70)
        print("🎯 Cycle Complete! Final JSON Summary:")
        print("=" * 70)
        
        # Output required JSON summary
        output = {
            "cycle_report": final_summary["cycle_report"],
            "molten_glyphs": [{"id": g["id"], "summary": g["compressed_summary"][:50] + "..."} for g in molten_glyphs],
            "mist_count": mist_count,
            "top_rooms": cycle_outputs["top_rooms"]
        }
        
        print(json.dumps(output, indent=2))
        return True
        
    except Exception as e:
        print(f"\n❌ Cycle failed: {e}")
        return False

if __name__ == "__main__":
    success = run_demonstration_cycle()
    sys.exit(0 if success else 1)