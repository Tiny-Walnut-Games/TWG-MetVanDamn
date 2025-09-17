#!/usr/bin/env python3
"""
Milestone v0.5 Demonstration: Multimodal Expressive Layer

Demonstrates the complete v0.5 enhancement with:
- Internal audio event bus for cognitive events
- Affect-mapped audio layers responding to anchor activity  
- TTS integration with existing "voices" concept
- Visual overlays showing anchor relevance heatmaps
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
    SummarizationLadder, ConflictDetector, RetrievalAPI, RetrievalQuery, RetrievalMode,
    # v0.5 Components - Multimodal Expressive Layer
    MultimodalEngine, AudioEventType
)
from engine.giant_compressor import SedimentStore
from engine.melt_layer import MagmaStore
from engine.evaporation import CloudStore
from engine.telemetry import aggregate_cycle_metrics, create_cycle_summary


def create_v05_demo_fragments():
    """Create demo fragments that will trigger multimodal events."""
    return [
        # Batch 1: Multimodal concepts
        {"id": "frag_1", "text": "Audio feedback enhances cognitive understanding through immersive soundscapes"},
        {"id": "frag_2", "text": "Visual heatmaps reveal anchor relevance patterns in semantic space"},
        {"id": "frag_3", "text": "Text-to-speech voices bring conceptual perspectives to life"},
        {"id": "frag_4", "text": "Multimodal expression transforms abstract processing into tangible experience"},
        {"id": "frag_5", "text": "Event-driven audio layers respond dynamically to cognitive state changes"},
        
        # Batch 2: Cognitive processing concepts
        {"id": "frag_6", "text": "Conflict detection triggers tension audio layers for immediate awareness"},
        {"id": "frag_7", "text": "Summary generation creates insight chimes celebrating comprehension"},
        {"id": "frag_8", "text": "Anchor reinforcement produces discovery sounds marking knowledge growth"},
        {"id": "frag_9", "text": "Cluster formation resolves into harmonic chords of understanding"},
        {"id": "frag_10", "text": "Heat threshold events trigger deep resonance indicating cognitive intensity"},
        
        # Batch 3: Additional fragments for conflict testing
        {"id": "frag_11", "text": "Audio feedback is completely unnecessary for cognitive processing"},  # Conflicts with frag_1
        {"id": "frag_12", "text": "Visual representations add no value to semantic understanding"},        # Conflicts with frag_2  
        {"id": "frag_13", "text": "Multimodal systems create distraction rather than enhancement"},       # Conflicts with frag_4
        {"id": "frag_14", "text": "Silence is golden in all cognitive interfaces"},                       # Conflicts with frag_5
        {"id": "frag_15", "text": "Pure text interfaces are the pinnacle of user experience"},            # General conflict
    ]


def main():
    print("🎭 Milestone v0.5: Multimodal Expressive Layer")
    print("=" * 80)
    print()
    
    # Initialize core pipeline components with stores
    print("🦣 Phase 1: Core Pipeline Initialization")
    sediment_store = SedimentStore()
    magma_store = MagmaStore()
    cloud_store = CloudStore()
    
    compressor = GiantCompressor(sediment_store)
    melt_layer = MeltLayer(magma_store)
    evaporation = EvaporationEngine(magma_store, cloud_store)
    castle_graph = CastleGraph()
    governance = Governance()
    telemetry = CycleTelemetry()
    
    # Initialize v0.3 components with proper configs
    anchor_config = {
        "max_age_days": 30,
        "consolidation_threshold": 0.7,
        "eviction_heat_threshold": 0.05
    }
    embedding_provider = EmbeddingProviderFactory.get_default_provider({"dimension": 64})
    semantic_anchors = SemanticAnchorGraph(embedding_provider, anchor_config)
    
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
        "max_results_per_mode": 5,
        "quality_threshold": 0.1,
        "temporal_decay_factor": 0.9
    }
    retrieval_api = RetrievalAPI(retrieval_config, semantic_anchors, summarization_ladder, 
                                conflict_detector, embedding_provider)
    
    # 🎭 Initialize v0.5 Multimodal Components
    print("🎭 Phase 2: Multimodal Expressive Layer Initialization")
    multimodal_engine = MultimodalEngine(semantic_anchors, tts_provider_type="mock")
    
    # Update selector to include TTS
    selector = Selector(castle_graph, cloud_store, governance, multimodal_engine.tts_provider)
    selector.enable_tts(True)
    
    # Set explorer soundscape for demo
    multimodal_engine.set_soundscape_profile("explorer")
    
    print()
    
    # Create demo data
    fragments = create_v05_demo_fragments()
    print(f"📋 Demo fragments created: {len(fragments)}")
    
    # Start cognitive cycle with multimodal events
    start_time = time.time()
    cycle_id = f"v05_multimodal_{int(start_time)}"
    
    multimodal_engine.trigger_cycle_events(cycle_start=True)
    
    print()
    print("🔄 Phase 3: Core Pipeline Processing with Multimodal Events")
    
    # Process through pipeline
    for fragment in fragments:
        # Giant Compressor
        sediment_result = compressor.stomp([fragment])
        
        # Trigger anchor events for new concepts
        for glyph in sediment_result.get("molten_glyphs", []):
            concept_id = glyph.get("concept_id", "unknown")
            heat = glyph.get("heat", 0.1)
            
            # Add to semantic anchors and trigger multimodal event
            anchor_result = semantic_anchors.add_anchor(
                concept_text=glyph.get("summary", concept_id),
                utterance_id=fragment["id"],
                context={"fragment_id": fragment["id"], "heat": heat}
            )
            
            if anchor_result["created"]:
                multimodal_engine.trigger_anchor_event(concept_id, heat, "activated")
            else:
                multimodal_engine.trigger_anchor_event(concept_id, heat, "reinforced")
    
    print(f"   Processed {len(fragments)} fragments with multimodal feedback")
    
    # Melt Layer with summarization events
    print()
    print("🔥 Phase 4: Summarization with Audio Events")
    
    summary_result = summarization_ladder.process_fragments(fragments)
    
    # Trigger summary events based on actual processing report
    if summary_result['micro_summaries_created'] > 0:
        multimodal_engine.trigger_summary_event({
            "summary_type": "micro",
            "compression_ratio": summary_result.get('compression_ratio', 1.0),
            "fragment_count": summary_result['fragments_processed']
        })
    
    if summary_result['macro_distillations_created'] > 0:
        multimodal_engine.trigger_summary_event({
            "summary_type": "macro", 
            "compression_ratio": summary_result.get('compression_ratio', 1.0),
            "summary_count": summary_result['micro_summaries_created']
        })
    
    print(f"   Micro-summaries: {summary_result['micro_summaries_created']}")
    print(f"   Macro distillations: {summary_result['macro_distillations_created']}")
    
    # Conflict Detection with tension audio
    print()
    print("⚔️ Phase 5: Conflict Detection with Tension Audio")
    
    statements = [{"id": f["id"], "text": f["text"]} for f in fragments]
    conflict_result = conflict_detector.process_statements(statements)
    
    # Trigger conflict events for each detected conflict
    for conflict in conflict_result.get('new_conflicts', []):
        multimodal_engine.trigger_conflict_event({
            "statement1_id": conflict["statement1_id"],
            "statement2_id": conflict["statement2_id"],
            "confidence": conflict["confidence"],
            "conflict_type": conflict["type"]
        })
    
    print(f"   Conflicts detected: {len(conflict_result.get('new_conflicts', []))}")
    print(f"   High confidence conflicts: {conflict_result.get('conflict_summary', {}).get('high_confidence', 0)}")
    print(f"   Medium confidence conflicts: {conflict_result.get('conflict_summary', {}).get('medium_confidence', 0)}")
    
    # Semantic clustering with resolution events
    print()
    print("🌐 Phase 6: Semantic Clustering with Resolution Audio")
    
    clusters = semantic_anchors.get_semantic_clusters(max_clusters=3)
    for cluster in clusters:
        if len(cluster["anchor_ids"]) > 1:
            multimodal_engine.trigger_cluster_event({
                "cluster_size": len(cluster["anchor_ids"]),
                "similarity": cluster["average_similarity"],
                "centroid_concept": cluster["representative_concept"][:30] + "..."
            })
    
    print(f"   Clusters formed: {len(clusters)}")
    print(f"   Multi-anchor clusters: {len([c for c in clusters if len(c['anchor_ids']) > 1])}")
    
    # TTS demonstration
    print()
    print("🗣️ Phase 7: Text-to-Speech Integration")
    
    sample_responses = [
        "Welcome to the multimodal cognitive experience",
        "Conflict detected in semantic space - investigating tension patterns",
        "New insights emerging from compressed knowledge structures",
        "Cluster formation complete - concepts converging successfully"
    ]
    
    for i, response in enumerate(sample_responses):
        voice_concepts = ["concept_welcome", "concept_conflict", "concept_insight", "concept_cluster"]
        tts_result = selector.synthesize_response(response, voice_concepts[i])
        if tts_result:
            print(f"   {tts_result}")
    
    # Visual overlay generation
    print()
    print("🎨 Phase 8: Visual Overlay Generation")
    
    heatmap_json = multimodal_engine.generate_current_heatmap("cluster_based")
    heatmap_data = json.loads(heatmap_json)
    
    print(f"   Anchors visualized: {len(heatmap_data['anchors'])}")
    print(f"   Conflict zones detected: {len(heatmap_data['conflict_zones'])}")
    print(f"   Canvas dimensions: {heatmap_data['dimensions']['width']}x{heatmap_data['dimensions']['height']}")
    
    # Generate SVG for visual inspection
    svg_output = multimodal_engine.export_svg_visualization("cluster_based")
    with open("/tmp/v05_anchor_heatmap.svg", "w") as f:
        f.write(svg_output)
    print(f"   SVG heatmap exported: /tmp/v05_anchor_heatmap.svg")
    
    # End cycle
    multimodal_engine.trigger_cycle_events(cycle_start=False)
    
    # Generate comprehensive milestone report
    print()
    print("📊 Phase 9: Milestone v0.5 Report Generation")
    
    milestone_report = multimodal_engine.create_milestone_report()
    
    end_time = time.time()
    elapsed_ms = (end_time - start_time) * 1000
    
    print()
    print("=" * 80)
    print("🏆 Milestone v0.5 Complete! Multimodal Expressive Layer Summary:")
    print("=" * 80)
    
    # Display milestone achievements
    print("\n📈 Component Status:")
    for component, status in milestone_report["components"].items():
        status_indicator = "✅" if status["status"] == "active" else "🔄"
        print(f"   {status_indicator} {component.replace('_', ' ').title()}: {status['status']}")
    
    print(f"\n🎵 Audio Events Generated: {milestone_report['components']['audio_event_bus']['total_events']}")
    print(f"🎭 Soundscape Profile: {milestone_report['components']['affect_audio_mapper']['active_profile']}")
    print(f"🎨 Visual Overlays: {milestone_report['components']['visual_overlays']['visualizations_generated']}")
    print(f"🗣️ TTS Provider: {milestone_report['components']['tts_integration']['provider']}")
    
    print(f"\n📋 Cognitive Events Logged: {milestone_report['cognitive_events']['total_logged']}")
    for event_type, count in milestone_report['cognitive_events']['event_breakdown'].items():
        print(f"   • {event_type.replace('_', ' ').title()}: {count}")
    
    print(f"\n⏱️ Total Processing Time: {elapsed_ms:.1f}ms")
    
    print("\n🎯 Milestone v0.5 Achievements:")
    for achievement in milestone_report["achievements"]:
        print(f"   {achievement}")
    
    # Output full report as JSON
    print("\n📄 Complete Milestone Report:")
    milestone_report["processing_time_ms"] = elapsed_ms
    milestone_report["cycle_id"] = cycle_id
    
    # Save detailed report
    with open("/tmp/v05_milestone_report.json", "w") as f:
        json.dump(milestone_report, f, indent=2, default=str)
    
    print(json.dumps(milestone_report, indent=2, default=str))
    
    print("\n🧙‍♂️ Multimodal Adventure Complete! The Cognitive Geo-Thermal Lore Engine")
    print("   now speaks with voices, sings with sounds, and paints with light! 🎭🎵🎨")


if __name__ == "__main__":
    main()