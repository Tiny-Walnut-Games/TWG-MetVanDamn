#!/usr/bin/env python3
"""
Anchor Memory Pool - Performance Optimization for v0.6

Implements object pooling for semantic anchors to reduce GC churn
and improve memory management during high-throughput ingestion.

🧙‍♂️ "Memory is the castle's foundation - manage it wisely, 
    lest the whole structure crumble under its own weight." - Bootstrap Sentinel
"""

from typing import List, Dict, Any, Optional, Deque
import time
from collections import deque
from dataclasses import dataclass
from threading import Lock
from .anchor_data_classes import SemanticAnchor, AnchorProvenance


@dataclass
class PoolMetrics:
    """Memory pool performance metrics."""
    total_created: int = 0
    total_reused: int = 0
    total_returned: int = 0
    current_pool_size: int = 0
    peak_pool_size: int = 0
    gc_collections_avoided: int = 0
    memory_pressure_events: int = 0
    last_cleanup_timestamp: float = 0.0
    
    def get_reuse_rate(self) -> float:
        """Calculate object reuse rate percentage."""
        total_requested = self.total_created + self.total_reused
        return (self.total_reused / total_requested * 100) if total_requested > 0 else 0.0


class AnchorMemoryPool:
    """
    Memory pool for semantic anchors with adaptive sizing and cleanup.
    
    Features:
    - Object reuse to reduce GC pressure
    - Adaptive pool sizing based on usage patterns
    - Memory pressure detection and cleanup
    - Thread-safe operations for concurrent access
    """
    
    def __init__(self, 
                 initial_size: int = 50,
                 max_size: int = 500,
                 cleanup_interval: float = 300.0,  # 5 minutes
                 memory_pressure_threshold: int = 1000):
        self.initial_size = initial_size
        self.max_size = max_size
        self.cleanup_interval = cleanup_interval
        self.memory_pressure_threshold = memory_pressure_threshold
        
        # Pool storage
        self.available_anchors: Deque[SemanticAnchor] = deque()
        self.available_provenances: Deque[AnchorProvenance] = deque()
        
        # Thread safety
        self._lock = Lock()
        
        # Metrics
        self.metrics = PoolMetrics()
        
        # Initialize pool with clean objects
        self._preallocate_pool()
        
    def _preallocate_pool(self):
        """Pre-allocate pool with initial objects."""
        for _ in range(self.initial_size):
            # Create clean anchor object
            anchor = self._create_clean_anchor()
            self.available_anchors.append(anchor)
            
            # Create clean provenance object
            provenance = self._create_clean_provenance()
            self.available_provenances.append(provenance)
            
        self.metrics.current_pool_size = self.initial_size
        self.metrics.peak_pool_size = self.initial_size
        
    def _create_clean_anchor(self) -> SemanticAnchor:
        """Create a clean anchor object for pooling."""
        return SemanticAnchor(
            anchor_id="",
            concept_text="",
            embedding=[],
            heat=0.0,
            provenance=None,  # Will be set when acquired
            cluster_id=None,
            semantic_drift=0.0,
            stability_score=1.0
        )
        
    def _create_clean_provenance(self) -> AnchorProvenance:
        """Create a clean provenance object for pooling."""
        return AnchorProvenance(
            first_seen=0.0,
            utterance_ids=[],
            update_count=0,
            last_updated=0.0,
            creation_context={},
            update_history=[]
        )
        
    def acquire_anchor(self, anchor_id: str, concept_text: str, 
                      embedding: List[float], heat: float,
                      creation_context: Dict[str, Any]) -> SemanticAnchor:
        """
        Acquire an anchor from the pool or create new one.
        
        Returns a configured anchor ready for use.
        """
        with self._lock:
            # Try to reuse from pool
            if self.available_anchors and self.available_provenances:
                anchor = self.available_anchors.popleft()
                provenance = self.available_provenances.popleft()
                self.metrics.total_reused += 1
                self.metrics.gc_collections_avoided += 1
            else:
                # Create new objects if pool is empty
                anchor = self._create_clean_anchor()
                provenance = self._create_clean_provenance()
                self.metrics.total_created += 1
                
            # Configure the anchor
            anchor.anchor_id = anchor_id
            anchor.concept_text = concept_text
            anchor.embedding = embedding.copy()  # Defensive copy
            anchor.heat = heat
            anchor.cluster_id = None
            anchor.semantic_drift = 0.0
            anchor.stability_score = 1.0
            
            # Configure provenance
            current_time = time.time()
            provenance.first_seen = current_time
            provenance.utterance_ids = []
            provenance.update_count = 0
            provenance.last_updated = current_time
            provenance.creation_context = creation_context.copy()
            provenance.update_history = []
            
            anchor.provenance = provenance
            
            self.metrics.current_pool_size = len(self.available_anchors)
            
            return anchor
            
    def return_anchor(self, anchor: SemanticAnchor):
        """
        Return an anchor to the pool for reuse.
        
        Cleans the anchor state before returning to pool.
        """
        if anchor is None:
            return
            
        with self._lock:
            # Check if we're at capacity
            if len(self.available_anchors) >= self.max_size:
                # Pool is full, let object be garbage collected
                self.metrics.memory_pressure_events += 1
                return
                
            # Clean the anchor for reuse
            self._clean_anchor_for_reuse(anchor)
            
            # Return to pool
            self.available_anchors.append(anchor)
            if anchor.provenance:
                self._clean_provenance_for_reuse(anchor.provenance)
                self.available_provenances.append(anchor.provenance)
                
            self.metrics.total_returned += 1
            self.metrics.current_pool_size = len(self.available_anchors)
            self.metrics.peak_pool_size = max(
                self.metrics.peak_pool_size, 
                self.metrics.current_pool_size
            )
            
    def _clean_anchor_for_reuse(self, anchor: SemanticAnchor):
        """Clean anchor state for reuse."""
        anchor.anchor_id = ""
        anchor.concept_text = ""
        anchor.embedding.clear()
        anchor.heat = 0.0
        anchor.cluster_id = None
        anchor.semantic_drift = 0.0
        anchor.stability_score = 1.0
        
    def _clean_provenance_for_reuse(self, provenance: AnchorProvenance):
        """Clean provenance state for reuse."""
        provenance.first_seen = 0.0
        provenance.utterance_ids.clear()
        provenance.update_count = 0
        provenance.last_updated = 0.0
        provenance.creation_context.clear()
        provenance.update_history.clear()
        
    def cleanup_pool(self, force: bool = False):
        """
        Clean up pool based on usage patterns and memory pressure.
        
        Args:
            force: Force cleanup regardless of interval
        """
        current_time = time.time()
        
        if not force and (current_time - self.metrics.last_cleanup_timestamp) < self.cleanup_interval:
            return
            
        with self._lock:
            # Adaptive pool sizing based on usage patterns
            target_size = self._calculate_optimal_pool_size()
            current_size = len(self.available_anchors)
            
            if current_size > target_size:
                # Reduce pool size
                excess = current_size - target_size
                for _ in range(excess):
                    if self.available_anchors:
                        self.available_anchors.popleft()
                    if self.available_provenances:
                        self.available_provenances.popleft()
                        
            elif current_size < target_size and current_size < self.max_size:
                # Grow pool if needed
                needed = min(target_size - current_size, self.max_size - current_size)
                for _ in range(needed):
                    anchor = self._create_clean_anchor()
                    provenance = self._create_clean_provenance()
                    self.available_anchors.append(anchor)
                    self.available_provenances.append(provenance)
                    
            self.metrics.last_cleanup_timestamp = current_time
            self.metrics.current_pool_size = len(self.available_anchors)
            
    def _calculate_optimal_pool_size(self) -> int:
        """Calculate optimal pool size based on usage patterns."""
        reuse_rate = self.metrics.get_reuse_rate()
        
        # High reuse rate = keep larger pool
        if reuse_rate > 80:
            return min(self.max_size, int(self.initial_size * 2))
        elif reuse_rate > 50:
            return int(self.initial_size * 1.5)
        else:
            return self.initial_size
            
    def get_pool_metrics(self) -> Dict[str, Any]:
        """Get comprehensive pool performance metrics."""
        with self._lock:
            return {
                "pool_status": {
                    "current_size": self.metrics.current_pool_size,
                    "peak_size": self.metrics.peak_pool_size,
                    "max_size": self.max_size,
                    "utilization_pct": (self.metrics.current_pool_size / self.max_size) * 100
                },
                "performance_metrics": {
                    "total_created": self.metrics.total_created,
                    "total_reused": self.metrics.total_reused,
                    "total_returned": self.metrics.total_returned,
                    "reuse_rate_pct": self.metrics.get_reuse_rate(),
                    "gc_collections_avoided": self.metrics.gc_collections_avoided
                },
                "memory_management": {
                    "memory_pressure_events": self.metrics.memory_pressure_events,
                    "last_cleanup": self.metrics.last_cleanup_timestamp,
                    "cleanup_interval": self.cleanup_interval
                }
            }
            
    def get_memory_savings_estimate(self) -> Dict[str, Any]:
        """Estimate memory savings from pooling."""
        # Rough estimates based on object sizes
        anchor_size_bytes = 1024  # Approximate size of SemanticAnchor
        provenance_size_bytes = 512  # Approximate size of AnchorProvenance
        
        total_objects_avoided = self.metrics.gc_collections_avoided * 2  # anchor + provenance
        memory_saved_bytes = total_objects_avoided * (anchor_size_bytes + provenance_size_bytes)
        
        return {
            "objects_reused": self.metrics.total_reused,
            "gc_collections_avoided": self.metrics.gc_collections_avoided,
            "estimated_memory_saved_bytes": memory_saved_bytes,
            "estimated_memory_saved_mb": memory_saved_bytes / (1024 * 1024),
            "efficiency_score": self.metrics.get_reuse_rate() / 100.0
        }


# Global pool instance for shared use
_global_anchor_pool: Optional[AnchorMemoryPool] = None


def get_global_anchor_pool() -> AnchorMemoryPool:
    """Get or create global anchor pool instance."""
    global _global_anchor_pool
    if _global_anchor_pool is None:
        _global_anchor_pool = AnchorMemoryPool()
    return _global_anchor_pool


def configure_global_pool(initial_size: int = 50, max_size: int = 500) -> AnchorMemoryPool:
    """Configure global anchor pool with custom settings."""
    global _global_anchor_pool
    _global_anchor_pool = AnchorMemoryPool(initial_size=initial_size, max_size=max_size)
    return _global_anchor_pool