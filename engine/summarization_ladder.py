"""
Summarization Ladder - Hierarchical Memory Compression with Micro and Macro Distillation

Implements rolling N-window micro-summaries and pipeline macro distillation
for the Cognitive Geo-Thermal Lore Engine v0.3.
"""

from typing import List, Dict, Any, Optional, Tuple
import time
import hashlib
from dataclasses import dataclass, asdict
from collections import deque


@dataclass
class MicroSummary:
    """Rolling window micro-summary with provenance."""
    summary_id: str
    window_fragments: List[str]  # Original fragment IDs in this window
    compressed_text: str
    window_size: int
    creation_timestamp: float
    heat_aggregate: float
    semantic_centroid: Optional[List[float]] = None
    
    def get_age_seconds(self) -> float:
        """Get summary age in seconds."""
        return time.time() - self.creation_timestamp


@dataclass
class MacroDistillation:
    """Macro distillation from N micro-summaries."""
    distillation_id: str
    source_micro_summaries: List[str]  # Micro-summary IDs
    distilled_essence: str
    consolidation_ratio: float  # Original fragments / distilled size
    provenance_chain: List[Dict[str, Any]]
    creation_timestamp: float
    anchor_reinforcements: List[str]  # Anchor IDs that were reinforced
    

class SummarizationLadder:
    """
    Hierarchical summarization system with micro-summaries and macro distillation.
    
    Architecture:
    - Micro-summaries: Rolling N-window summaries of recent fragments
    - Macro distillation: Pipeline processing after N micro-summaries accumulated
    - Recovery distillation: Anchor reinforcement during distillation process
    """
    
    def __init__(self, config: Optional[Dict[str, Any]] = None, embedding_provider=None):
        self.config = config or {}
        self.embedding_provider = embedding_provider
        
        # Configuration parameters
        self.micro_window_size = self.config.get("micro_window_size", 5)
        self.macro_trigger_count = self.config.get("macro_trigger_count", 3)
        self.max_micro_summaries = self.config.get("max_micro_summaries", 20)
        
        # Storage
        self.micro_summaries: deque = deque(maxlen=self.max_micro_summaries)
        self.macro_distillations: List[MacroDistillation] = []
        self.fragment_buffer: deque = deque(maxlen=self.micro_window_size)
        
        # State tracking
        self.total_fragments_processed = 0
        self.micro_summaries_created = 0
        self.macro_distillations_created = 0
        
        # Metrics
        self.metrics = {
            "total_fragments": 0,
            "micro_summaries_created": 0,
            "macro_distillations_created": 0,
            "compression_ratio": 0.0,
            "processing_time_ms": 0.0
        }
    
    def process_fragments(self, fragments: List[Dict[str, Any]]) -> Dict[str, Any]:
        """
        Process incoming fragments through the summarization ladder.
        
        Returns processing report with micro-summaries and any macro distillations.
        """
        start_time = time.time()
        processing_report = {
            "fragments_processed": len(fragments),
            "micro_summaries_created": 0,
            "macro_distillations_created": 0,
            "new_micro_summaries": [],
            "new_macro_distillations": []
        }
        
        for fragment in fragments:
            self.fragment_buffer.append(fragment)
            self.total_fragments_processed += 1
            
            # Check if we should create a micro-summary
            if len(self.fragment_buffer) >= self.micro_window_size:
                micro_summary = self._create_micro_summary()
                if micro_summary:
                    self.micro_summaries.append(micro_summary)
                    processing_report["micro_summaries_created"] += 1
                    processing_report["new_micro_summaries"].append({
                        "summary_id": micro_summary.summary_id,
                        "compressed_text": micro_summary.compressed_text[:100] + "...",
                        "window_size": micro_summary.window_size,
                        "heat_aggregate": micro_summary.heat_aggregate
                    })
                    
                    # Check if we should trigger macro distillation
                    if len(self.micro_summaries) >= self.macro_trigger_count:
                        macro_distillation = self._create_macro_distillation()
                        if macro_distillation:
                            self.macro_distillations.append(macro_distillation)
                            processing_report["macro_distillations_created"] += 1
                            processing_report["new_macro_distillations"].append({
                                "distillation_id": macro_distillation.distillation_id,
                                "distilled_essence": macro_distillation.distilled_essence[:100] + "...",
                                "consolidation_ratio": macro_distillation.consolidation_ratio,
                                "source_count": len(macro_distillation.source_micro_summaries)
                            })
        
        # Update metrics
        elapsed_ms = (time.time() - start_time) * 1000
        self.metrics["total_fragments"] = self.total_fragments_processed
        self.metrics["micro_summaries_created"] = self.micro_summaries_created
        self.metrics["macro_distillations_created"] = self.macro_distillations_created
        self.metrics["processing_time_ms"] += elapsed_ms
        
        # Calculate compression ratio
        if self.total_fragments_processed > 0:
            compressed_units = len(self.micro_summaries) + len(self.macro_distillations)
            self.metrics["compression_ratio"] = self.total_fragments_processed / max(compressed_units, 1)
        
        processing_report["elapsed_ms"] = elapsed_ms
        processing_report["total_micro_summaries"] = len(self.micro_summaries)
        processing_report["total_macro_distillations"] = len(self.macro_distillations)
        
        return processing_report
    
    def get_recovery_context(self, anchor_id: str, context_size: int = 3) -> Dict[str, Any]:
        """
        Get recovery distillation context for anchor reinforcement.
        
        Returns relevant micro-summaries and macro distillations that relate to the anchor.
        """
        recovery_context = {
            "anchor_id": anchor_id,
            "related_micro_summaries": [],
            "related_macro_distillations": [],
            "temporal_sequence": [],
            "consolidation_path": []
        }
        
        # Find micro-summaries that might relate to the anchor
        # (In a full implementation, this would use semantic similarity)
        recent_micros = list(self.micro_summaries)[-context_size:]
        for micro in recent_micros:
            recovery_context["related_micro_summaries"].append({
                "summary_id": micro.summary_id,
                "compressed_text": micro.compressed_text,
                "heat_aggregate": micro.heat_aggregate,
                "age_seconds": micro.get_age_seconds()
            })
            
        # Find relevant macro distillations
        recent_macros = self.macro_distillations[-context_size:] if self.macro_distillations else []
        for macro in recent_macros:
            if anchor_id in macro.anchor_reinforcements:
                recovery_context["related_macro_distillations"].append({
                    "distillation_id": macro.distillation_id,
                    "distilled_essence": macro.distilled_essence,
                    "consolidation_ratio": macro.consolidation_ratio,
                    "anchor_reinforcements": macro.anchor_reinforcements
                })
        
        # Build temporal sequence showing information flow
        all_items = []
        for micro in recent_micros:
            all_items.append(("micro", micro.creation_timestamp, micro.summary_id))
        for macro in recent_macros:
            all_items.append(("macro", macro.creation_timestamp, macro.distillation_id))
            
        all_items.sort(key=lambda x: x[1])
        recovery_context["temporal_sequence"] = [
            {"type": item[0], "timestamp": item[1], "id": item[2]} 
            for item in all_items
        ]
        
        return recovery_context
    
    def get_compression_metrics(self) -> Dict[str, Any]:
        """Get comprehensive compression and performance metrics."""
        return {
            "summarization_ladder_metrics": self.metrics.copy(),
            "current_state": {
                "micro_summaries_active": len(self.micro_summaries),
                "macro_distillations_total": len(self.macro_distillations),
                "fragment_buffer_size": len(self.fragment_buffer),
                "compression_ratio": self.metrics["compression_ratio"]
            },
            "ladder_health": {
                "processing_efficiency": self._calculate_processing_efficiency(),
                "compression_effectiveness": self._calculate_compression_effectiveness(),
                "temporal_coverage_hours": self._calculate_temporal_coverage()
            }
        }
    
    def _create_micro_summary(self) -> Optional[MicroSummary]:
        """Create a micro-summary from the current fragment buffer."""
        if len(self.fragment_buffer) < self.micro_window_size:
            return None
            
        fragments = list(self.fragment_buffer)
        
        # Extract fragment IDs and text
        fragment_ids = [f.get("id", f"frag_{i}") for i, f in enumerate(fragments)]
        fragment_texts = [f.get("text", "") for f in fragments]
        
        # Simple summarization (in production, would use more sophisticated methods)
        compressed_text = self._compress_fragment_texts(fragment_texts)
        
        # Calculate aggregate heat
        heat_aggregate = sum(f.get("heat", 0.1) for f in fragments) / len(fragments)
        
        # Generate semantic centroid if embedding provider available
        semantic_centroid = None
        if self.embedding_provider and fragment_texts:
            try:
                embeddings = [self.embedding_provider.embed_text(text) for text in fragment_texts]
                if embeddings:
                    # Calculate centroid
                    dim = len(embeddings[0])
                    semantic_centroid = [
                        sum(emb[i] for emb in embeddings) / len(embeddings)
                        for i in range(dim)
                    ]
            except Exception:
                # Fallback to None if embedding fails
                pass
        
        # Create micro-summary
        summary_id = self._generate_summary_id(compressed_text)
        micro_summary = MicroSummary(
            summary_id=summary_id,
            window_fragments=fragment_ids,
            compressed_text=compressed_text,
            window_size=len(fragments),
            creation_timestamp=time.time(),
            heat_aggregate=heat_aggregate,
            semantic_centroid=semantic_centroid
        )
        
        self.micro_summaries_created += 1
        
        # Clear part of buffer to allow for overlap
        overlap_size = max(1, self.micro_window_size // 3)
        for _ in range(len(self.fragment_buffer) - overlap_size):
            self.fragment_buffer.popleft()
            
        return micro_summary
    
    def _create_macro_distillation(self) -> Optional[MacroDistillation]:
        """Create a macro distillation from recent micro-summaries."""
        if len(self.micro_summaries) < self.macro_trigger_count:
            return None
            
        # Take the oldest micro-summaries for distillation
        source_summaries = []
        source_summary_ids = []
        
        for _ in range(self.macro_trigger_count):
            if self.micro_summaries:
                micro = self.micro_summaries.popleft()
                source_summaries.append(micro)
                source_summary_ids.append(micro.summary_id)
        
        if not source_summaries:
            return None
            
        # Distill the essence from micro-summaries
        distilled_essence = self._distill_macro_essence(source_summaries)
        
        # Calculate consolidation ratio
        total_original_fragments = sum(len(micro.window_fragments) for micro in source_summaries)
        consolidation_ratio = total_original_fragments / 1.0  # 1 distillation from N fragments
        
        # Build provenance chain
        provenance_chain = [
            {
                "micro_summary_id": micro.summary_id,
                "original_fragments": len(micro.window_fragments),
                "heat_contribution": micro.heat_aggregate,
                "creation_timestamp": micro.creation_timestamp
            }
            for micro in source_summaries
        ]
        
        # Mock anchor reinforcements (in production, would integrate with SemanticAnchorGraph)
        anchor_reinforcements = [f"anchor_reinforce_{i}" for i in range(len(source_summaries))]
        
        # Create macro distillation
        distillation_id = self._generate_distillation_id(distilled_essence)
        macro_distillation = MacroDistillation(
            distillation_id=distillation_id,
            source_micro_summaries=source_summary_ids,
            distilled_essence=distilled_essence,
            consolidation_ratio=consolidation_ratio,
            provenance_chain=provenance_chain,
            creation_timestamp=time.time(),
            anchor_reinforcements=anchor_reinforcements
        )
        
        self.macro_distillations_created += 1
        return macro_distillation
    
    def _compress_fragment_texts(self, texts: List[str]) -> str:
        """Compress multiple fragment texts into a micro-summary."""
        if not texts:
            return "(empty window)"
            
        # Simple compression: take key phrases from each text
        key_phrases = []
        for text in texts:
            # Extract first meaningful phrase (up to 30 chars)
            clean_text = text.strip()
            if clean_text:
                phrase = clean_text[:30]
                if len(clean_text) > 30:
                    phrase += "..."
                key_phrases.append(phrase)
        
        # Combine into micro-summary
        if len(key_phrases) == 1:
            return f"[Micro] {key_phrases[0]}"
        else:
            return f"[Micro] {' • '.join(key_phrases[:3])}"  # Limit to 3 phrases
    
    def _distill_macro_essence(self, micro_summaries: List[MicroSummary]) -> str:
        """Distill macro essence from multiple micro-summaries."""
        if not micro_summaries:
            return "(empty distillation)"
            
        # Extract key themes from micro-summaries
        themes = []
        for micro in micro_summaries:
            # Extract meaningful content from micro-summary
            content = micro.compressed_text.replace("[Micro]", "").strip()
            if content:
                themes.append(content)
        
        # Create macro distillation
        if len(themes) == 1:
            return f"[Macro] {themes[0]}"
        else:
            # Combine themes into higher-level abstraction
            combined = " ⟶ ".join(themes[:2])  # Show progression
            return f"[Macro] {combined}"
    
    def _generate_summary_id(self, content: str) -> str:
        """Generate unique ID for micro-summary."""
        timestamp = str(int(time.time() * 1000))
        content_hash = hashlib.md5(content.encode()).hexdigest()[:8]
        return f"micro_{timestamp}_{content_hash}"
    
    def _generate_distillation_id(self, essence: str) -> str:
        """Generate unique ID for macro distillation."""
        timestamp = str(int(time.time() * 1000))
        essence_hash = hashlib.md5(essence.encode()).hexdigest()[:8]
        return f"macro_{timestamp}_{essence_hash}"
    
    def _calculate_processing_efficiency(self) -> float:
        """Calculate processing efficiency metric."""
        if self.metrics["total_fragments"] == 0:
            return 1.0
        total_time_seconds = self.metrics["processing_time_ms"] / 1000.0
        if total_time_seconds == 0:
            return 1.0
        return self.metrics["total_fragments"] / total_time_seconds
    
    def _calculate_compression_effectiveness(self) -> float:
        """Calculate how effectively we're compressing information."""
        return min(self.metrics["compression_ratio"] / 10.0, 1.0)  # Normalize to 0-1
    
    def _calculate_temporal_coverage(self) -> float:
        """Calculate temporal coverage in hours."""
        if not self.micro_summaries and not self.macro_distillations:
            return 0.0
            
        oldest_time = time.time()
        newest_time = 0
        
        for micro in self.micro_summaries:
            oldest_time = min(oldest_time, micro.creation_timestamp)
            newest_time = max(newest_time, micro.creation_timestamp)
            
        for macro in self.macro_distillations:
            oldest_time = min(oldest_time, macro.creation_timestamp)
            newest_time = max(newest_time, macro.creation_timestamp)
        
        if newest_time > oldest_time:
            return (newest_time - oldest_time) / 3600.0  # Convert to hours
        return 0.0