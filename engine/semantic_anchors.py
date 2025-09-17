"""
Enhanced Anchor System with Semantic Grounding and Provenance
"""

from typing import List, Dict, Any, Optional, Tuple
import time
import hashlib
import json
from dataclasses import dataclass, asdict
from .embeddings import EmbeddingProvider, EmbeddingProviderFactory
from .anchor_memory_pool import AnchorMemoryPool, get_global_anchor_pool
from .anchor_data_classes import SemanticAnchor, AnchorProvenance

# Privacy hooks for PII scrubbing before anchor injection
try:
    from .hooks.privacy_hooks import PrivacyHooks, get_default_privacy_hooks
    PRIVACY_HOOKS_AVAILABLE = True
except ImportError:
    PRIVACY_HOOKS_AVAILABLE = False


class SemanticAnchorGraph:
    """Enhanced CastleGraph with semantic grounding and provenance."""
    
    def __init__(self, embedding_provider: Optional[EmbeddingProvider] = None, 
                 config: Optional[Dict[str, Any]] = None,
                 memory_pool: Optional[AnchorMemoryPool] = None,
                 privacy_hooks: Optional[PrivacyHooks] = None):
        self.config = config or {}
        self.embedding_provider = embedding_provider or EmbeddingProviderFactory.get_default_provider()
        
        # Memory pool for performance optimization
        self.memory_pool = memory_pool or get_global_anchor_pool()
        
        # Privacy hooks for PII scrubbing (optional but recommended)
        if privacy_hooks:
            self.privacy_hooks = privacy_hooks
        elif PRIVACY_HOOKS_AVAILABLE and self.config.get("enable_privacy_hooks", True):
            self.privacy_hooks = get_default_privacy_hooks()
        else:
            self.privacy_hooks = None
        
        # Anchor storage
        self.anchors: Dict[str, SemanticAnchor] = {}
        self.clusters: Dict[str, List[str]] = {}  # cluster_id -> anchor_ids
        
        # Lifecycle configuration
        self.max_age_days = self.config.get("max_age_days", 30)
        self.consolidation_threshold = self.config.get("consolidation_threshold", 0.8)
        self.eviction_heat_threshold = self.config.get("eviction_heat_threshold", 0.1)
        
        # Performance configuration
        self.enable_memory_pooling = self.config.get("enable_memory_pooling", True)
        
        # Metrics
        self.metrics = {
            "total_anchors_created": 0,
            "total_updates": 0,
            "total_evictions": 0,
            "total_consolidations": 0,
        }
        
    def create_or_update_anchor(self, concept_text: str, utterance_id: str, context: Dict[str, Any]) -> str:
        """Create new anchor or update existing one with PII scrubbing applied."""
        
        # 🔐 PRIVACY HOOK: Apply PII scrubbing before anchor injection
        original_concept_text = concept_text
        privacy_metadata = {}
        
        if self.privacy_hooks:
            concept_text, privacy_metadata = self.privacy_hooks.scrub_content_for_anchor_injection(
                concept_text, context, utterance_id
            )
            
            # Add privacy metadata to context for provenance tracking
            context = context.copy()
            context["privacy_scrubbing_applied"] = privacy_metadata.get("privacy_hook_applied", False)
            context["original_content_length"] = len(original_concept_text)
            context["scrubbed_content_length"] = len(concept_text)
            
            # Validate privacy compliance if configured
            is_compliant, violations = self.privacy_hooks.validate_privacy_compliance(
                original_concept_text, context
            )
            if not is_compliant:
                context["privacy_violations"] = violations
                # Log the violation but continue with scrubbed content
                print(f"⚠️ Privacy violations detected for anchor injection: {violations}")
        
        # Generate embedding from scrubbed content
        embedding = self.embedding_provider.embed_text(concept_text)
        
        # Check for existing similar anchor
        existing_anchor_id = self._find_similar_anchor(embedding)
        
        if existing_anchor_id:
            # Update existing anchor
            anchor = self.anchors[existing_anchor_id]
            old_embedding = anchor.embedding.copy()
            
            # Update embedding (weighted average with recency bias)
            weight = 0.3  # Weight for new embedding
            anchor.embedding = [
                (1 - weight) * old + weight * new 
                for old, new in zip(anchor.embedding, embedding)
            ]
            
            # Calculate semantic drift
            anchor.semantic_drift = self._calculate_drift(old_embedding, anchor.embedding)
            
            # Update provenance
            anchor.provenance.add_update(utterance_id, context)
            
            # Increase heat
            anchor.heat += 0.1
            
            self.metrics["total_updates"] += 1
            
            return existing_anchor_id
            
        else:
            # Create new anchor using memory pool
            anchor_id = self._generate_anchor_id(concept_text)
            
            if self.enable_memory_pooling:
                # Use memory pool for better performance
                anchor = self.memory_pool.acquire_anchor(
                    anchor_id=anchor_id,
                    concept_text=concept_text,
                    embedding=embedding,
                    heat=0.2,  # Initial heat
                    creation_context=context
                )
                # Add initial utterance
                anchor.provenance.add_update(utterance_id, context)
            else:
                # Create anchor directly (fallback)
                provenance = AnchorProvenance(
                    first_seen=time.time(),
                    utterance_ids=[utterance_id],
                    update_count=1,
                    last_updated=time.time(),
                    creation_context=context,
                    update_history=[]
                )
                
                anchor = SemanticAnchor(
                    anchor_id=anchor_id,
                    concept_text=concept_text,
                    embedding=embedding,
                    heat=0.2,  # Initial heat
                    provenance=provenance
                )
            
            self.anchors[anchor_id] = anchor
            self.metrics["total_anchors_created"] += 1
            
            return anchor_id
            
    def get_semantic_clusters(self, max_clusters: int = 5) -> List[Dict[str, Any]]:
        """Get semantic clusters of anchors."""
        if len(self.anchors) < 2:
            return []
            
        # Simple clustering based on embedding similarity
        anchors_list = list(self.anchors.values())
        clusters = []
        used_anchors = set()
        
        for anchor in anchors_list:
            if anchor.anchor_id in used_anchors:
                continue
                
            cluster_anchors = [anchor]
            used_anchors.add(anchor.anchor_id)
            
            # Find similar anchors
            for other_anchor in anchors_list:
                if other_anchor.anchor_id in used_anchors:
                    continue
                    
                similarity = self.embedding_provider.calculate_similarity(
                    anchor.embedding, other_anchor.embedding
                )
                
                if similarity > self.consolidation_threshold:
                    cluster_anchors.append(other_anchor)
                    used_anchors.add(other_anchor.anchor_id)
                    
            if len(cluster_anchors) > 1:
                cluster_info = self._create_cluster_info(cluster_anchors)
                clusters.append(cluster_info)
                
                if len(clusters) >= max_clusters:
                    break
                    
        return clusters
        
    def get_anchor_diff(self, since_timestamp: float) -> Dict[str, Any]:
        """Get anchor changes since timestamp."""
        added = []
        updated = []
        decayed = []
        
        for anchor in self.anchors.values():
            if anchor.provenance.first_seen > since_timestamp:
                added.append({
                    "anchor_id": anchor.anchor_id,
                    "concept_text": anchor.concept_text,
                    "heat": anchor.heat,
                    "first_seen": anchor.provenance.first_seen
                })
            elif anchor.provenance.last_updated > since_timestamp:
                # Check if reinforced or decayed
                recent_updates = [
                    update for update in anchor.provenance.update_history 
                    if update["timestamp"] > since_timestamp
                ]
                
                if recent_updates:
                    updated.append({
                        "anchor_id": anchor.anchor_id,
                        "concept_text": anchor.concept_text,
                        "heat": anchor.heat,
                        "updates": len(recent_updates),
                        "semantic_drift": anchor.semantic_drift
                    })
                    
        return {
            "since_timestamp": since_timestamp,
            "added": added,
            "updated": updated,
            "decayed": decayed,
            "total_anchors": len(self.anchors)
        }
        
    def apply_lifecycle_policies(self) -> Dict[str, Any]:
        """Apply aging, consolidation, and eviction policies."""
        actions = {
            "aged": 0,
            "consolidated": 0,
            "evicted": 0,
            "evicted_anchors": []
        }
        
        current_time = time.time()
        anchors_to_evict = []
        
        # Apply aging
        for anchor in self.anchors.values():
            age_days = anchor.calculate_age_days()
            
            # Heat decay based on age
            decay_factor = max(0.95 ** age_days, 0.1)
            anchor.heat *= decay_factor
            actions["aged"] += 1
            
            # Mark for eviction if too old or too cold
            if (age_days > self.max_age_days or 
                anchor.heat < self.eviction_heat_threshold):
                anchors_to_evict.append(anchor.anchor_id)
                
        # Evict old/cold anchors and return to memory pool
        for anchor_id in anchors_to_evict:
            evicted_anchor = self.anchors.pop(anchor_id)
            
            # Return to memory pool if enabled
            if self.enable_memory_pooling:
                self.memory_pool.return_anchor(evicted_anchor)
                
            actions["evicted"] += 1
            actions["evicted_anchors"].append({
                "anchor_id": anchor_id,
                "concept_text": evicted_anchor.concept_text,
                "age_days": evicted_anchor.calculate_age_days(),
                "final_heat": evicted_anchor.heat
            })
            
        self.metrics["total_evictions"] += actions["evicted"]
        
        # Trigger memory pool cleanup periodically
        if self.enable_memory_pooling and actions["evicted"] > 0:
            self.memory_pool.cleanup_pool()
        
        return actions
        
    def get_stability_metrics(self) -> Dict[str, Any]:
        """Calculate anchor churn, drift, and stability metrics."""
        if not self.anchors:
            return {
                "total_anchors": 0,
                "average_age_days": 0,
                "average_heat": 0,
                "average_drift": 0,
                "churn_rate": 0,
                "stability_score": 1.0
            }
            
        anchors = list(self.anchors.values())
        
        # Basic metrics
        total_anchors = len(anchors)
        average_age = sum(a.calculate_age_days() for a in anchors) / total_anchors
        average_heat = sum(a.heat for a in anchors) / total_anchors
        average_drift = sum(a.semantic_drift for a in anchors) / total_anchors
        
        # Churn rate (evictions per day)
        days_active = max(average_age, 1)
        churn_rate = self.metrics["total_evictions"] / days_active
        
        # Overall stability score (lower drift = higher stability)
        stability_score = max(0, 1 - average_drift)
        
        # Get memory pool metrics if enabled
        memory_metrics = {}
        if self.enable_memory_pooling:
            memory_metrics = self.memory_pool.get_pool_metrics()
        
        return {
            "total_anchors": total_anchors,
            "average_age_days": average_age,
            "average_heat": average_heat,
            "average_drift": average_drift,
            "churn_rate": churn_rate,
            "stability_score": stability_score,
            "provider_info": self.embedding_provider.get_provider_info(),
            "memory_pool_metrics": memory_metrics
        }
        
    def _find_similar_anchor(self, embedding: List[float]) -> Optional[str]:
        """Find existing anchor with similar embedding."""
        best_similarity = 0
        best_anchor_id = None
        
        for anchor_id, anchor in self.anchors.items():
            similarity = self.embedding_provider.calculate_similarity(
                embedding, anchor.embedding
            )
            
            if similarity > best_similarity and similarity > self.consolidation_threshold:
                best_similarity = similarity
                best_anchor_id = anchor_id
                
        return best_anchor_id
        
    def _calculate_drift(self, old_embedding: List[float], new_embedding: List[float]) -> float:
        """Calculate semantic drift between embeddings."""
        similarity = self.embedding_provider.calculate_similarity(old_embedding, new_embedding)
        return 1.0 - similarity  # Drift is inverse of similarity
        
    def _generate_anchor_id(self, concept_text: str) -> str:
        """Generate unique anchor ID."""
        timestamp = str(int(time.time() * 1000))
        text_hash = hashlib.md5(concept_text.encode()).hexdigest()[:8]
        return f"anchor_{timestamp}_{text_hash}"
        
    def _create_cluster_info(self, cluster_anchors: List[SemanticAnchor]) -> Dict[str, Any]:
        """Create cluster information summary."""
        cluster_id = f"cluster_{int(time.time())}_{len(cluster_anchors)}"
        
        # Calculate centroid
        centroid = [0] * len(cluster_anchors[0].embedding)
        for anchor in cluster_anchors:
            for i, value in enumerate(anchor.embedding):
                centroid[i] += value
        centroid = [v / len(cluster_anchors) for v in centroid]
        
        # Summary text (most common concepts)
        concepts = [anchor.concept_text for anchor in cluster_anchors]
        summary = f"Cluster of {len(concepts)} related concepts"
        
        return {
            "cluster_id": cluster_id,
            "anchor_count": len(cluster_anchors),
            "anchor_ids": [a.anchor_id for a in cluster_anchors],
            "centroid": centroid,
            "summary": summary,
            "total_heat": sum(a.heat for a in cluster_anchors),
            "average_age": sum(a.calculate_age_days() for a in cluster_anchors) / len(cluster_anchors)
        }
    
    def get_privacy_metrics(self) -> Dict[str, Any]:
        """Get privacy enforcement metrics from privacy hooks"""
        if self.privacy_hooks:
            return self.privacy_hooks.get_privacy_metrics()
        else:
            return {
                "privacy_hooks_enabled": False,
                "privacy_note": "Privacy hooks not available or disabled"
            }