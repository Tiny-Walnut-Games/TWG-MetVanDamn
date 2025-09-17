#!/usr/bin/env python3
"""
Milestone v0.3 Demonstration: Summarization Ladder & Memory Compression

Demonstrates the complete v0.3 enhancement with:
- Micro-summary worker with rolling N-window summaries
- Pipeline macro distillation after N segments
- Recovery distillation and anchor reinforcement
- Conflict detector for clashing statements
- Retrieval API with anchor-grounded recall context
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
    EmbeddingProviderFactory,
    # v0.3 Components
    SummarizationLadder, ConflictDetector, RetrievalAPI, RetrievalQuery, RetrievalMode
)
from engine.giant_compressor import SedimentStore
from engine.melt_layer import MagmaStore
from engine.evaporation import CloudStore
from engine.telemetry import aggregate_cycle_metrics, create_cycle_summary


def create_v03_demo_fragments():
    """Create demo fragments that will trigger summarization and conflict detection."""
    return [
        # Batch 1: Development process fragments
        {"id": "frag_1", "text": "Implementing advanced debugging tools improves developer productivity significantly"},
        {"id": "frag_2", "text": "The warbler cloud formation shows excellent patterns in knowledge distillation processes"},
        {"id": "frag_3", "text": "Memory castle architecture requires careful heat distribution for optimal performance"},
        {"id": "frag_4", "text": "Evaporation processes transform raw data into meaningful actionable insights"},
        {"id": "frag_5", "text": "Giant compression algorithms optimize storage while preserving semantic meaning perfectly"},
        
        # Batch 2: More fragments to trigger micro-summaries
        {"id": "frag_6", "text": "Semantic anchors enhance understanding through comprehensive provenance tracking systems"},
        {"id": "frag_7", "text": "Embedding providers enable flexible semantic grounding with multiple algorithm approaches"},
        {"id": "frag_8", "text": "Anchor lifecycle policies maintain system health through intelligent aging and eviction"},
        {"id": "frag_9", "text": "Conflict detection identifies opposing statements using semantic similarity analysis"},
        {"id": "frag_10", "text": "Retrieval API provides anchor-grounded context assembly for enhanced recall capabilities"},
        
        # Batch 3: Conflicting statements for conflict detection
        {"id": "frag_11", "text": "Debugging tools are not helpful for developer productivity and should be avoided"},
        {"id": "frag_12", "text": "Memory compression is impossible without losing critical semantic information"},
        {"id": "frag_13", "text": "Semantic grounding never works reliably in production environments"},
        {"id": "frag_14", "text": "Anchor-based retrieval always provides perfect context without any limitations"},
        {"id": "frag_15", "text": "Conflict detection cannot identify any meaningful opposing statements accurately"},
    ]


def run_v03_milestone_demonstration():
    """Run complete v0.3 milestone demonstration with all new features."""
    print("🚀 Milestone v0.3: Summarization Ladder & Memory Compression")
    print("=" * 80)
    
    # Initialize telemetry
    cycle_id = f"v03_milestone_{int(time.time())}"
    telemetry = CycleTelemetry(cycle_id=cycle_id)
    
    # Initialize core stores
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
    
    # Initialize v0.3 components
    summarization_config = {
        "micro_window_size": 5,
        "macro_trigger_count": 3,
        "max_micro_summaries": 20
    }
    summarization_ladder = SummarizationLadder(summarization_config, embedding_provider)
    
    conflict_config = {
        "opposition_threshold": 0.7,
        "semantic_similarity_threshold": 0.8,
        "min_confidence_score": 0.6
    }
    conflict_detector = ConflictDetector(conflict_config, embedding_provider)
    
    retrieval_config = {
        "default_max_results": 10,
        "relevance_threshold": 0.5,
        "temporal_decay_hours": 24
    }
    retrieval_api = RetrievalAPI(retrieval_config, semantic_anchors, summarization_ladder, 
                                conflict_detector, embedding_provider)
    
    # Initialize core engine components
    giant = GiantCompressor(sediment_store)
    melt_layer = MeltLayer(magma_store)
    evaporator = EvaporationEngine(magma_store, cloud_store)
    selector = Selector(castle_graph, cloud_store, governance)
    
    try:
        # Phase 1: Process fragments through core pipeline
        print("\n🦣 Phase 1: Core Pipeline Processing (Giant → Melt → Evaporation)")
        raw_fragments = create_v03_demo_fragments()
        print(f"   Input: {len(raw_fragments)} raw fragments")
        
        # Giant compression
        stomp_result = giant.stomp(raw_fragments)
        print(f"   Giant: {stomp_result['clusters']} clusters, {stomp_result['elapsed_ms']:.1f}ms")
        
        # Melt layer processing
        molten_glyphs = []
        for stratum in sediment_store.strata:
            for cluster in stratum["clusters"]:
                glyph = melt_layer.retire_cluster(cluster)
                molten_glyphs.append(glyph)
        print(f"   Melt: {len(molten_glyphs)} molten glyphs created")
        
        # Evaporation
        mist_lines = evaporator.evaporate(limit=5)
        mist_count = len(mist_lines)
        print(f"   Evaporation: {mist_count} mist lines, humidity {cloud_store.humidity_index:.2f}")
        
        # Phase 2: NEW - Summarization Ladder Processing
        print("\n📋 Phase 2: Summarization Ladder (Micro-summaries & Macro Distillation)")
        summarization_report = summarization_ladder.process_fragments(raw_fragments)
        
        print(f"   Fragments processed: {summarization_report['fragments_processed']}")
        print(f"   Micro-summaries created: {summarization_report['micro_summaries_created']}")
        print(f"   Macro distillations created: {summarization_report['macro_distillations_created']}")
        print(f"   Processing time: {summarization_report['elapsed_ms']:.1f}ms")
        
        # Show compression metrics
        compression_metrics = summarization_ladder.get_compression_metrics()
        print(f"   Compression ratio: {compression_metrics['summarization_ladder_metrics']['compression_ratio']:.2f}")
        
        # Phase 3: NEW - Conflict Detection
        print("\n⚔️ Phase 3: Conflict Detection (Statement Clash Analysis)")
        statements = [{"id": f["id"], "text": f["text"]} for f in raw_fragments]
        conflict_report = conflict_detector.process_statements(statements)
        
        print(f"   Statements processed: {conflict_report['statements_processed']}")
        print(f"   Conflicts detected: {len(conflict_report['new_conflicts'])}")
        print(f"   High confidence conflicts: {conflict_report['conflict_summary']['high_confidence']}")
        print(f"   Medium confidence conflicts: {conflict_report['conflict_summary']['medium_confidence']}")
        
        # Show specific conflicts
        if conflict_report['new_conflicts']:
            print("   🚨 Sample conflicts detected:")
            for conflict in conflict_report['new_conflicts'][:3]:  # Show first 3
                print(f"      - {conflict['conflict_type']}: {conflict['confidence_score']:.2f} confidence")
        
        # Phase 4: Semantic Anchor Processing (Enhanced)
        print("\n⚓ Phase 4: Enhanced Semantic Anchor Processing")
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
                
        print(f"   Semantic anchors created/updated: {len(anchor_ids)}")
        
        # Apply lifecycle policies
        lifecycle_actions = semantic_anchors.apply_lifecycle_policies()
        print(f"   Lifecycle actions - Aged: {lifecycle_actions['aged']}, Evicted: {lifecycle_actions['evicted']}")
        
        # Phase 5: NEW - Retrieval API Demonstration
        print("\n🔍 Phase 5: Retrieval API (Anchor-Grounded Context Assembly)")
        
        # Semantic similarity retrieval
        semantic_query = RetrievalQuery(
            query_id="demo_semantic",
            mode=RetrievalMode.SEMANTIC_SIMILARITY,
            semantic_query="debugging tools developer productivity",
            max_results=5
        )
        semantic_context = retrieval_api.retrieve_context(semantic_query)
        print(f"   Semantic retrieval: {len(semantic_context.results)} results, quality {semantic_context.assembly_quality:.2f}")
        
        # Anchor neighborhood retrieval
        if anchor_ids:
            neighborhood_context = retrieval_api.get_anchor_context(anchor_ids[0], context_radius=3)
            print(f"   Anchor neighborhood: {len(neighborhood_context.results)} results, quality {neighborhood_context.assembly_quality:.2f}")
        
        # Temporal sequence retrieval
        current_time = time.time()
        temporal_query = RetrievalQuery(
            query_id="demo_temporal",
            mode=RetrievalMode.TEMPORAL_SEQUENCE,
            temporal_range=(current_time - 3600, current_time),  # Last hour
            max_results=5
        )
        temporal_context = retrieval_api.retrieve_context(temporal_query)
        print(f"   Temporal retrieval: {len(temporal_context.results)} results, quality {temporal_context.assembly_quality:.2f}")
        
        # Phase 6: Recovery Distillation
        print("\n🔄 Phase 6: Recovery Distillation & Anchor Reinforcement")
        if anchor_ids:
            recovery_context = summarization_ladder.get_recovery_context(anchor_ids[0])
            print(f"   Recovery context for anchor: {len(recovery_context['related_micro_summaries'])} micro-summaries")
            print(f"   Temporal sequence items: {len(recovery_context['temporal_sequence'])}")
        
        # Phase 7: Global System Analysis
        print("\n📊 Phase 7: Global System Health & Metrics")
        
        # Conflict system health
        conflict_summary = conflict_detector.get_global_conflict_summary()
        print(f"   Conflict system status: {conflict_summary['status']}")
        print(f"   System health score: {conflict_summary['system_health_score']:.2f}")
        
        # Retrieval system metrics
        retrieval_metrics = retrieval_api.get_retrieval_metrics()
        print(f"   Retrieval cache hit rate: {retrieval_metrics['cache_performance']['hit_rate']:.2f}")
        print(f"   Average retrieval quality: {retrieval_metrics['system_health']['average_quality']:.2f}")
        
        # Traditional pipeline completion
        print("\n🏰 Phase 8: Traditional Pipeline Completion")
        infusion_result = castle_graph.infuse(mist_lines)
        print(f"   Castle infusion: {infusion_result['infused_count']} mist lines, {infusion_result['total_nodes']} nodes")
        
        prompt_scaffold = selector.assemble_prompt("v0.3 milestone demonstration context")
        response = selector.respond(prompt_scaffold)
        print(f"   Selector: Response generated with {len(prompt_scaffold['voices'])} voices")
        
        # Phase 9: Comprehensive Governance Scoring
        print("\n⚖️ Phase 9: Enhanced Governance Scoring")
        cycle_outputs = {
            "molten_glyphs": molten_glyphs,
            "mist_count": mist_count,
            "top_rooms": castle_graph.get_top_rooms(5),
            "semantic_anchors": len(semantic_anchors.anchors),
            "micro_summaries": len(summarization_ladder.micro_summaries),
            "macro_distillations": len(summarization_ladder.macro_distillations),
            "conflicts_detected": len(conflict_report['new_conflicts']),
            "retrieval_quality": semantic_context.assembly_quality
        }
        
        # Complete telemetry
        telemetry.finish_cycle()
        cycle_summary = create_cycle_summary(telemetry, cycle_outputs)
        governance_result = governance.score_cycle(cycle_summary["cycle_report"])
        
        print(f"   Governance score: {governance_result['score']:.2f}, {governance_result['assessment']}")
        
        # Final comprehensive output
        print("\n" + "=" * 80)
        print("🎯 Milestone v0.3 Complete! Enhanced System Summary:")
        print("=" * 80)
        
        # Comprehensive JSON output
        output = {
            "milestone": "v0.3",
            "cycle_report": cycle_summary["cycle_report"],
            "core_pipeline": {
                "molten_glyphs": [{"id": g["id"], "summary": g["compressed_summary"][:50] + "..."} for g in molten_glyphs],
                "mist_count": mist_count,
                "top_rooms": cycle_outputs["top_rooms"]
            },
            "v03_enhancements": {
                "summarization_ladder": {
                    "micro_summaries_created": summarization_report['micro_summaries_created'],
                    "macro_distillations_created": summarization_report['macro_distillations_created'],
                    "compression_ratio": compression_metrics['summarization_ladder_metrics']['compression_ratio'],
                    "processing_efficiency": compression_metrics['ladder_health']['processing_efficiency']
                },
                "conflict_detection": {
                    "total_conflicts": len(conflict_report['new_conflicts']),
                    "high_confidence_conflicts": conflict_report['conflict_summary']['high_confidence'],
                    "system_health_score": conflict_summary['system_health_score'],
                    "status": conflict_summary['status']
                },
                "retrieval_api": {
                    "semantic_quality": semantic_context.assembly_quality,
                    "temporal_quality": temporal_context.assembly_quality,
                    "cache_hit_rate": retrieval_metrics['cache_performance']['hit_rate'],
                    "average_results": retrieval_metrics['retrieval_metrics']['average_results_per_query']
                },
                "anchor_reinforcement": {
                    "total_anchors": len(semantic_anchors.anchors),
                    "lifecycle_evictions": lifecycle_actions['evicted'],
                    "stability_score": semantic_anchors.get_stability_metrics()['stability_score']
                }
            },
            "sample_conflicts": conflict_report['new_conflicts'][:3],
            "sample_retrieval_results": [
                {
                    "content_type": result.content_type,
                    "relevance_score": result.relevance_score,
                    "content_preview": result.content[:50] + "..."
                }
                for result in semantic_context.results[:3]
            ],
            "system_integration": {
                "components_integrated": 4,  # Summarization, Conflict, Retrieval, Anchors
                "backward_compatible": True,
                "governance_enhancement": True,
                "performance_impact": "minimal"
            }
        }
        
        print(json.dumps(output, indent=2))
        
        # Show specific v0.3 achievements
        print("\n🏆 Milestone v0.3 Achievements:")
        print("   ✅ Micro-summary worker with rolling N-window summaries")
        print("   ✅ Pipeline macro distillation after N segments")
        print("   ✅ Recovery distillation and anchor reinforcement")
        print("   ✅ Conflict detector flagging clashing statements")
        print("   ✅ Retrieval API with anchor-grounded recall context")
        print("   ✅ Full integration with existing Geo-Thermal Lore Engine")
        
        return True
        
    except Exception as e:
        print(f"\n❌ v0.3 milestone demonstration failed: {e}")
        import traceback
        traceback.print_exc()
        return False


if __name__ == "__main__":
    success = run_v03_milestone_demonstration()
    sys.exit(0 if success else 1)