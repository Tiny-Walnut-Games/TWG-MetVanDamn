#!/usr/bin/env python3
"""
Enhanced Cycle Runner with Semantic Anchors
Demonstrates the complete Geo-Thermal Lore Engine with semantic grounding and provenance.
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
    Selector, Governance, CycleTelemetry, SemanticAnchorGraph,
    EmbeddingProviderFactory
)
from engine.giant_compressor import SedimentStore
from engine.melt_layer import MagmaStore
from engine.evaporation import CloudStore
from engine.telemetry import aggregate_cycle_metrics, create_cycle_summary


def create_demo_fragments():
    """Create demo raw fragments for the enhanced cycle."""
    return [
        {"id": "frag_1", "text": "Implementing advanced debugging tools for better developer experience"},
        {"id": "frag_2", "text": "The warbler cloud formation shows promising patterns in knowledge distillation"},
        {"id": "frag_3", "text": "Memory castle architecture requires careful consideration of heat distribution"},
        {"id": "frag_4", "text": "Evaporation processes transform raw data into actionable insights"},
        {"id": "frag_5", "text": "Giant compression algorithms optimize storage while preserving semantic meaning"},
        {"id": "frag_6", "text": "Semantic anchors enhance understanding through provenance tracking"},
        {"id": "frag_7", "text": "Embedding providers enable flexible semantic grounding approaches"},
        {"id": "frag_8", "text": "Anchor lifecycle policies maintain system health through aging and eviction"},
    ]


def run_enhanced_demonstration_cycle():
    """Run complete demonstration cycle with semantic grounding."""
    print("🔥 Enhanced Cognitive Geo-Thermal Lore Engine with Semantic Anchors")
    print("=" * 80)
    
    # Initialize telemetry
    cycle_id = f"enhanced_cycle_{int(time.time())}"
    telemetry = CycleTelemetry(cycle_id=cycle_id)
    
    # Initialize stores
    sediment_store = SedimentStore()
    magma_store = MagmaStore()
    cloud_store = CloudStore()
    castle_graph = CastleGraph()
    governance = Governance()
    
    # Initialize semantic anchor system
    anchor_config = {
        "max_age_days": 30,
        "consolidation_threshold": 0.7,
        "eviction_heat_threshold": 0.05
    }
    embedding_provider = EmbeddingProviderFactory.get_default_provider({"dimension": 64})
    semantic_anchors = SemanticAnchorGraph(embedding_provider, anchor_config)
    
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
        mist_lines = evaporator.evaporate(limit=5)
        mist_count = len(mist_lines)
        print(f"   Result: {mist_count} mist lines, humidity {cloud_store.humidity_index:.2f}")
        
        # 4. INFUSE: Mist → Castle Nodes (Legacy)
        print("\n🏰 Phase 4a: Castle Infusion (Legacy System)")
        infusion_result = castle_graph.infuse(mist_lines)
        print(f"   Result: {infusion_result['infused_count']} mist lines infused, {infusion_result['total_nodes']} total nodes")
        
        # 4b. SEMANTIC ANCHOR: Enhanced Semantic Processing
        print("\n⚓ Phase 4b: Semantic Anchor Processing (Enhanced System)")
        anchor_ids = []
        for i, mist in enumerate(mist_lines):
            proto_thought = mist.get("proto_thought", "")
            if proto_thought:
                utterance_id = f"mist_{i}_{int(time.time())}"
                context = {
                    "mist_id": mist.get("id", f"mist_{i}"),
                    "mythic_weight": mist.get("mythic_weight", 0.1),
                    "cycle_id": cycle_id
                }
                anchor_id = semantic_anchors.create_or_update_anchor(proto_thought, utterance_id, context)
                anchor_ids.append(anchor_id)
                
        print(f"   Result: {len(anchor_ids)} semantic anchors created/updated")
        
        # 5. SEMANTIC ANALYSIS: Clustering and Metrics
        print("\n🔗 Phase 5: Semantic Analysis (Clustering & Provenance)")
        clusters = semantic_anchors.get_semantic_clusters(max_clusters=3)
        stability_metrics = semantic_anchors.get_stability_metrics()
        
        print(f"   Semantic clusters: {len(clusters)}")
        print(f"   Total anchors: {stability_metrics['total_anchors']}")
        print(f"   Stability score: {stability_metrics['stability_score']:.3f}")
        
        # 6. LIFECYCLE: Apply policies
        print("\n♻️ Phase 6: Lifecycle Management")
        lifecycle_actions = semantic_anchors.apply_lifecycle_policies()
        print(f"   Aged: {lifecycle_actions['aged']}, Evicted: {lifecycle_actions['evicted']}")
        
        # 7. SELECT: Assemble prompt (Enhanced)
        print("\n🎭 Phase 7: Enhanced Selector Assembly")
        prompt_scaffold = selector.assemble_prompt("Enhanced demo context with semantic grounding")
        response = selector.respond(prompt_scaffold)
        print(f"   Result: Response generated with {len(prompt_scaffold['voices'])} voices")
        
        # 8. GOVERNANCE: Score cycle
        print("\n⚖️ Phase 8: Governance Scoring")
        cycle_outputs = {
            "molten_glyphs": molten_glyphs,
            "mist_count": mist_count,
            "top_rooms": castle_graph.get_top_rooms(5),
            "semantic_anchors": len(semantic_anchors.anchors),
            "semantic_clusters": len(clusters),
            "stability_metrics": stability_metrics
        }
        
        # Complete telemetry
        telemetry.finish_cycle()
        components = {
            "stomp_result": stomp_result,
            "molten_glyphs": molten_glyphs,
            "mist_count": mist_count,
            "infusion_result": infusion_result,
            "top_rooms": cycle_outputs["top_rooms"],
            "semantic_anchors": anchor_ids,
            "semantic_clusters": clusters,
            "lifecycle_actions": lifecycle_actions
        }
        
        # Governance scoring
        cycle_summary = create_cycle_summary(telemetry, cycle_outputs)
        governance_result = governance.score_cycle(cycle_summary["cycle_report"])
        components["governance_result"] = governance_result
        
        print(f"   Result: Quality score {governance_result['score']:.2f}, {governance_result['assessment']}")
        
        # Final aggregation with semantic enhancements
        final_metrics = aggregate_cycle_metrics(telemetry, components)
        final_summary = create_cycle_summary(telemetry, cycle_outputs)
        
        print("\n" + "=" * 80)
        print("🎯 Enhanced Cycle Complete! Semantic Grounding Summary:")
        print("=" * 80)
        
        # Output enhanced JSON summary
        output = {
            "cycle_report": final_summary["cycle_report"],
            "molten_glyphs": [{"id": g["id"], "summary": g["compressed_summary"][:50] + "..."} for g in molten_glyphs],
            "mist_count": mist_count,
            "top_rooms": cycle_outputs["top_rooms"],
            "semantic_enhancement": {
                "total_anchors": len(semantic_anchors.anchors),
                "semantic_clusters": len(clusters),
                "stability_score": stability_metrics["stability_score"],
                "provider_info": stability_metrics["provider_info"],
                "lifecycle_actions": lifecycle_actions
            },
            "anchor_provenance_sample": [
                {
                    "anchor_id": anchor_id[:20] + "...",
                    "concept": anchor.concept_text,
                    "heat": anchor.heat,
                    "updates": anchor.provenance.update_count,
                    "age_days": anchor.calculate_age_days()
                }
                for anchor_id, anchor in list(semantic_anchors.anchors.items())[:3]
            ]
        }
        
        print(json.dumps(output, indent=2))
        
        # Show anchor diff
        print("\n📊 Anchor Diff (since cycle start):")
        diff = semantic_anchors.get_anchor_diff(telemetry.start_time)
        print(f"   Added: {len(diff['added'])}, Updated: {len(diff['updated'])}")
        
        return True
        
    except Exception as e:
        print(f"\n❌ Enhanced cycle failed: {e}")
        import traceback
        traceback.print_exc()
        return False


if __name__ == "__main__":
    success = run_enhanced_demonstration_cycle()
    sys.exit(0 if success else 1)