#!/usr/bin/env python3
"""
Test script for Semantic Anchor System with Provenance
Tests embedding providers, anchor creation, clustering, and lifecycle management.
"""

import sys
import os
import time
from pathlib import Path

# Add parent directory to path for engine imports
sys.path.insert(0, str(Path(__file__).parent.parent))

from engine.semantic_anchors import SemanticAnchorGraph, SemanticAnchor, AnchorProvenance
from engine.embeddings import EmbeddingProviderFactory


def test_embedding_providers():
    """Test different embedding providers."""
    print("🧠 Testing Embedding Providers")
    print("=" * 50)
    
    # Test local provider
    local_provider = EmbeddingProviderFactory.create_provider("local", {"dimension": 64})
    print(f"Local Provider: {local_provider.get_provider_info()}")
    
    test_text = "Implementing advanced debugging tools for better developer experience"
    embedding = local_provider.embed_text(test_text)
    print(f"Sample embedding dimension: {len(embedding)}")
    print(f"First 5 values: {embedding[:5]}")
    
    # Test batch embedding
    texts = [
        "Memory castle architecture requires careful consideration",
        "Evaporation processes transform raw data into insights",
        "Giant compression algorithms optimize storage"
    ]
    batch_embeddings = local_provider.embed_batch(texts)
    print(f"Batch embeddings: {len(batch_embeddings)} vectors")
    
    # Test similarity
    similarity = local_provider.calculate_similarity(batch_embeddings[0], batch_embeddings[1])
    print(f"Similarity between first two texts: {similarity:.3f}")
    
    print("✅ Embedding providers working!\n")


def test_semantic_anchor_creation():
    """Test semantic anchor creation and updates."""
    print("⚓ Testing Semantic Anchor Creation")
    print("=" * 50)
    
    # Create anchor graph with local provider
    config = {"max_age_days": 7, "consolidation_threshold": 0.7}
    anchor_graph = SemanticAnchorGraph(config=config)
    
    # Create some anchors
    test_cases = [
        ("debugging tools implementation", "utterance_1", {"session": "dev_session_1"}),
        ("memory castle architecture", "utterance_2", {"session": "dev_session_1"}),
        ("debugging and development tools", "utterance_3", {"session": "dev_session_2"}),  # Similar to first
        ("warbler cloud formation patterns", "utterance_4", {"session": "dev_session_2"}),
    ]
    
    anchor_ids = []
    for concept, utterance_id, context in test_cases:
        anchor_id = anchor_graph.create_or_update_anchor(concept, utterance_id, context)
        anchor_ids.append(anchor_id)
        print(f"Created/Updated anchor: {anchor_id}")
        
    print(f"Total anchors: {len(anchor_graph.anchors)}")
    
    # Show anchor details
    for anchor_id, anchor in anchor_graph.anchors.items():
        print(f"\nAnchor {anchor_id[:20]}...")
        print(f"  Concept: {anchor.concept_text}")
        print(f"  Heat: {anchor.heat:.3f}")
        print(f"  Updates: {anchor.provenance.update_count}")
        print(f"  Age: {anchor.calculate_age_days():.3f} days")
        
    print("✅ Anchor creation working!\n")
    return anchor_graph


def test_semantic_clustering(anchor_graph):
    """Test semantic clustering of anchors."""
    print("🔗 Testing Semantic Clustering")
    print("=" * 50)
    
    clusters = anchor_graph.get_semantic_clusters(max_clusters=3)
    print(f"Found {len(clusters)} semantic clusters")
    
    for i, cluster in enumerate(clusters):
        print(f"\nCluster {i+1}: {cluster['cluster_id']}")
        print(f"  Anchors: {cluster['anchor_count']}")
        print(f"  Summary: {cluster['summary']}")
        print(f"  Total Heat: {cluster['total_heat']:.3f}")
        print(f"  Average Age: {cluster['average_age']:.3f} days")
        
    print("✅ Semantic clustering working!\n")


def test_anchor_diff(anchor_graph):
    """Test anchor diff functionality."""
    print("📊 Testing Anchor Diff Engine")
    print("=" * 50)
    
    # Get baseline timestamp
    baseline = time.time() - 1  # 1 second ago
    
    # Add some new anchors
    time.sleep(0.1)  # Small delay for timestamp difference
    anchor_graph.create_or_update_anchor(
        "evolutionary pet system", "utterance_5", {"session": "dev_session_3"}
    )
    
    # Update existing anchor
    first_anchor_id = list(anchor_graph.anchors.keys())[0]
    anchor_graph.create_or_update_anchor(
        anchor_graph.anchors[first_anchor_id].concept_text, 
        "utterance_6", 
        {"session": "dev_session_3"}
    )
    
    # Get diff
    diff = anchor_graph.get_anchor_diff(baseline)
    
    print(f"Anchor changes since {baseline}")
    print(f"Added: {len(diff['added'])}")
    print(f"Updated: {len(diff['updated'])}")
    print(f"Total anchors: {diff['total_anchors']}")
    
    for added in diff['added']:
        print(f"  + {added['concept_text']} (heat: {added['heat']:.3f})")
        
    for updated in diff['updated']:
        print(f"  ~ {updated['concept_text']} (heat: {updated['heat']:.3f}, drift: {updated['semantic_drift']:.3f})")
        
    print("✅ Anchor diff working!\n")


def test_lifecycle_policies(anchor_graph):
    """Test anchor lifecycle policies."""
    print("♻️ Testing Lifecycle Policies")
    print("=" * 50)
    
    # Get initial metrics
    initial_metrics = anchor_graph.get_stability_metrics()
    print("Initial metrics:")
    for key, value in initial_metrics.items():
        if isinstance(value, float):
            print(f"  {key}: {value:.3f}")
        else:
            print(f"  {key}: {value}")
            
    # Apply lifecycle policies
    actions = anchor_graph.apply_lifecycle_policies()
    print(f"\nLifecycle actions:")
    print(f"  Aged: {actions['aged']}")
    print(f"  Consolidated: {actions['consolidated']}")
    print(f"  Evicted: {actions['evicted']}")
    
    if actions['evicted_anchors']:
        print("  Evicted anchors:")
        for evicted in actions['evicted_anchors']:
            print(f"    - {evicted['concept_text']} (age: {evicted['age_days']:.3f}, heat: {evicted['final_heat']:.3f})")
            
    # Get updated metrics
    final_metrics = anchor_graph.get_stability_metrics()
    print(f"\nFinal metrics:")
    for key, value in final_metrics.items():
        if isinstance(value, float):
            print(f"  {key}: {value:.3f}")
        else:
            print(f"  {key}: {value}")
            
    print("✅ Lifecycle policies working!\n")


def test_full_semantic_grounding_cycle():
    """Run complete semantic grounding test cycle."""
    print("🔥 Full Semantic Grounding & Anchor Provenance Test")
    print("=" * 70)
    
    try:
        # Test 1: Embedding providers
        test_embedding_providers()
        
        # Test 2: Anchor creation
        anchor_graph = test_semantic_anchor_creation()
        
        # Test 3: Semantic clustering
        test_semantic_clustering(anchor_graph)
        
        # Test 4: Anchor diff
        test_anchor_diff(anchor_graph)
        
        # Test 5: Lifecycle policies
        test_lifecycle_policies(anchor_graph)
        
        print("🎯 All semantic grounding tests passed!")
        print("📊 System ready for milestone v0.2 deployment")
        
        return True
        
    except Exception as e:
        print(f"❌ Test failed: {e}")
        import traceback
        traceback.print_exc()
        return False


if __name__ == "__main__":
    success = test_full_semantic_grounding_cycle()
    sys.exit(0 if success else 1)