#!/usr/bin/env python3
"""
Test script for v0.3 components to validate functionality.
"""

import sys
import time
from pathlib import Path

# Add parent directory to path for engine imports
sys.path.insert(0, str(Path(__file__).parent.parent))

from engine import (
    SummarizationLadder, ConflictDetector, RetrievalAPI, 
    RetrievalQuery, RetrievalMode, EmbeddingProviderFactory
)


def test_summarization_ladder():
    """Test the summarization ladder component."""
    print("🧪 Testing Summarization Ladder...")
    
    config = {"micro_window_size": 3, "macro_trigger_count": 2}
    embedding_provider = EmbeddingProviderFactory.get_default_provider()
    ladder = SummarizationLadder(config, embedding_provider)
    
    fragments = [
        {"id": "test_1", "text": "First test fragment for summarization"},
        {"id": "test_2", "text": "Second fragment about development"},
        {"id": "test_3", "text": "Third fragment on coding practices"},
        {"id": "test_4", "text": "Fourth fragment about debugging"},
        {"id": "test_5", "text": "Fifth fragment on optimization"},
        {"id": "test_6", "text": "Sixth fragment about testing"},
    ]
    
    report = ladder.process_fragments(fragments)
    
    print(f"   ✅ Processed {report['fragments_processed']} fragments")
    print(f"   ✅ Created {report['micro_summaries_created']} micro-summaries")
    print(f"   ✅ Created {report['macro_distillations_created']} macro distillations")
    
    metrics = ladder.get_compression_metrics()
    print(f"   ✅ Compression ratio: {metrics['summarization_ladder_metrics']['compression_ratio']:.2f}")
    
    return True


def test_conflict_detector():
    """Test the conflict detector component."""
    print("🧪 Testing Conflict Detector...")
    
    config = {"min_confidence_score": 0.5}
    embedding_provider = EmbeddingProviderFactory.get_default_provider()
    detector = ConflictDetector(config, embedding_provider)
    
    statements = [
        {"id": "stmt_1", "text": "Debugging tools are very helpful for developers"},
        {"id": "stmt_2", "text": "Debugging tools are not helpful for developers"},
        {"id": "stmt_3", "text": "Memory optimization improves performance significantly"},
        {"id": "stmt_4", "text": "Memory optimization never improves performance"},
    ]
    
    report = detector.process_statements(statements)
    
    print(f"   ✅ Processed {report['statements_processed']} statements")
    print(f"   ✅ Detected {len(report['new_conflicts'])} conflicts")
    
    summary = detector.get_global_conflict_summary()
    print(f"   ✅ System health score: {summary['system_health_score']:.2f}")
    print(f"   ✅ Status: {summary['status']}")
    
    return True


def test_retrieval_api():
    """Test the retrieval API component."""
    print("🧪 Testing Retrieval API...")
    
    embedding_provider = EmbeddingProviderFactory.get_default_provider()
    retrieval_api = RetrievalAPI(embedding_provider=embedding_provider)
    
    # Test semantic query
    query = RetrievalQuery(
        query_id="test_semantic",
        mode=RetrievalMode.SEMANTIC_SIMILARITY,
        semantic_query="development tools and debugging",
        max_results=5
    )
    
    context = retrieval_api.retrieve_context(query)
    
    print(f"   ✅ Retrieved {len(context.results)} results")
    print(f"   ✅ Assembly quality: {context.assembly_quality:.2f}")
    
    metrics = retrieval_api.get_retrieval_metrics()
    components = metrics['system_health']['components_available']
    print(f"   ✅ Components available: {sum(components.values())}/{len(components)}")
    
    return True


def main():
    """Run all component tests."""
    print("🚀 v0.3 Component Validation Tests")
    print("=" * 50)
    
    try:
        # Test each component
        test_summarization_ladder()
        test_conflict_detector()
        test_retrieval_api()
        
        print("\n✅ All v0.3 components validated successfully!")
        return True
        
    except Exception as e:
        print(f"\n❌ Component validation failed: {e}")
        import traceback
        traceback.print_exc()
        return False


if __name__ == "__main__":
    success = main()
    sys.exit(0 if success else 1)