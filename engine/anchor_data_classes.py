#!/usr/bin/env python3
"""
Anchor Data Classes - Shared data structures for semantic anchors

Contains the core data classes used by both semantic anchors and memory pool
to avoid circular import issues.
"""

from typing import List, Dict, Any, Optional
import time
from dataclasses import dataclass


@dataclass 
class AnchorProvenance:
    """Provenance tracking for anchors."""
    first_seen: float
    utterance_ids: List[str]
    update_count: int
    last_updated: float
    creation_context: Dict[str, Any]
    update_history: List[Dict[str, Any]]
    
    def add_update(self, utterance_id: str, context: Dict[str, Any]):
        """Record an anchor update."""
        self.utterance_ids.append(utterance_id)
        self.update_count += 1
        self.last_updated = time.time()
        self.update_history.append({
            "timestamp": self.last_updated,
            "utterance_id": utterance_id,
            "context": context
        })


@dataclass
class SemanticAnchor:
    """Enhanced anchor with semantic embedding and provenance."""
    anchor_id: str
    concept_text: str
    embedding: List[float]
    heat: float
    provenance: AnchorProvenance
    cluster_id: Optional[str] = None
    semantic_drift: float = 0.0
    stability_score: float = 1.0
    
    def calculate_age_days(self) -> float:
        """Calculate anchor age in days."""
        return (time.time() - self.provenance.first_seen) / (24 * 3600)
        
    def calculate_activity_rate(self) -> float:
        """Calculate activity rate (updates per day)."""
        age_days = self.calculate_age_days()
        if age_days == 0:
            return 0
        return self.provenance.update_count / age_days