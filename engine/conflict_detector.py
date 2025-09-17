"""
Conflict Detector - Semantic Statement Clash Detection

Detects conflicting or contradictory statements using semantic similarity and
logical opposition analysis for the Cognitive Geo-Thermal Lore Engine v0.3.
"""

from typing import List, Dict, Any, Optional, Tuple, Set
import time
import hashlib
from dataclasses import dataclass, asdict
from enum import Enum


class ConflictType(Enum):
    """Types of conflicts that can be detected."""
    SEMANTIC_OPPOSITION = "semantic_opposition"  # Directly opposing statements
    LOGICAL_CONTRADICTION = "logical_contradiction"  # Logically incompatible
    FACTUAL_INCONSISTENCY = "factual_inconsistency"  # Inconsistent facts
    TEMPORAL_CONFLICT = "temporal_conflict"  # Time-based conflicts
    SCOPE_MISMATCH = "scope_mismatch"  # Different scope/context but conflicting


@dataclass
class ConflictEvidence:
    """Evidence for a detected conflict."""
    statement_a_id: str
    statement_b_id: str
    conflict_type: ConflictType
    confidence_score: float  # 0.0 to 1.0
    semantic_distance: float
    opposition_indicators: List[str]
    context_overlap: float
    detection_timestamp: float
    
    def get_age_seconds(self) -> float:
        """Get conflict age in seconds."""
        return time.time() - self.detection_timestamp


@dataclass
class StatementFingerprint:
    """Semantic and structural fingerprint of a statement."""
    statement_id: str
    content: str
    embedding: List[float]
    negation_indicators: List[str]
    assertion_strength: float  # How definitive the statement is
    temporal_markers: List[str]
    domain_tags: Set[str]
    creation_timestamp: float


class ConflictDetector:
    """
    Semantic conflict detection system for identifying clashing statements.
    
    Features:
    - Semantic opposition detection using embeddings
    - Negation and assertion analysis
    - Temporal conflict identification
    - Confidence scoring and evidence collection
    """
    
    def __init__(self, config: Optional[Dict[str, Any]] = None, embedding_provider=None):
        self.config = config or {}
        self.embedding_provider = embedding_provider
        
        # Configuration parameters
        self.opposition_threshold = self.config.get("opposition_threshold", 0.7)
        self.semantic_similarity_threshold = self.config.get("semantic_similarity_threshold", 0.8)
        self.min_confidence_score = self.config.get("min_confidence_score", 0.6)
        self.max_statement_age_hours = self.config.get("max_statement_age_hours", 24)
        
        # Storage
        self.statement_fingerprints: Dict[str, StatementFingerprint] = {}
        self.detected_conflicts: List[ConflictEvidence] = []
        self.conflict_history: List[ConflictEvidence] = []
        
        # Conflict detection patterns
        self.negation_patterns = [
            "not", "no", "never", "none", "nothing", "nowhere",
            "isn't", "aren't", "won't", "can't", "don't", "doesn't",
            "unable", "impossible", "incorrect", "false", "wrong"
        ]
        
        self.assertion_patterns = [
            "always", "definitely", "certainly", "absolutely", "must",
            "will", "shall", "guaranteed", "proven", "fact", "truth"
        ]
        
        self.temporal_patterns = [
            "before", "after", "during", "when", "while", "since",
            "until", "by", "at", "on", "in", "yesterday", "today",
            "tomorrow", "now", "then", "later", "earlier"
        ]
        
        # Metrics
        self.metrics = {
            "statements_processed": 0,
            "conflicts_detected": 0,
            "false_positives_resolved": 0,
            "processing_time_ms": 0.0,
            "average_confidence": 0.0
        }
    
    def process_statements(self, statements: List[Dict[str, Any]]) -> Dict[str, Any]:
        """
        Process new statements and detect conflicts with existing statements.
        
        Args:
            statements: List of statement dicts with 'id', 'text', and optional metadata
            
        Returns:
            Processing report with new conflicts detected
        """
        start_time = time.time()
        processing_report = {
            "statements_processed": len(statements),
            "new_conflicts": [],
            "fingerprints_created": 0,
            "total_active_statements": 0,
            "conflict_summary": {
                "high_confidence": 0,
                "medium_confidence": 0,
                "low_confidence": 0
            }
        }
        
        # Process each statement
        for statement in statements:
            statement_id = statement.get("id", f"stmt_{int(time.time())}")
            content = statement.get("text", "")
            
            if not content.strip():
                continue
                
            # Create fingerprint for new statement
            fingerprint = self._create_statement_fingerprint(statement_id, content, statement)
            self.statement_fingerprints[statement_id] = fingerprint
            processing_report["fingerprints_created"] += 1
            
            # Detect conflicts with existing statements
            conflicts = self._detect_conflicts_for_statement(fingerprint)
            
            for conflict in conflicts:
                if conflict.confidence_score >= self.min_confidence_score:
                    self.detected_conflicts.append(conflict)
                    processing_report["new_conflicts"].append({
                        "conflict_id": self._generate_conflict_id(conflict),
                        "statement_a": conflict.statement_a_id,
                        "statement_b": conflict.statement_b_id,
                        "conflict_type": conflict.conflict_type.value,
                        "confidence_score": conflict.confidence_score,
                        "opposition_indicators": conflict.opposition_indicators
                    })
                    
                    # Categorize by confidence
                    if conflict.confidence_score >= 0.8:
                        processing_report["conflict_summary"]["high_confidence"] += 1
                    elif conflict.confidence_score >= 0.6:
                        processing_report["conflict_summary"]["medium_confidence"] += 1
                    else:
                        processing_report["conflict_summary"]["low_confidence"] += 1
        
        # Cleanup old statements
        self._cleanup_old_statements()
        
        # Update metrics
        elapsed_ms = (time.time() - start_time) * 1000
        self.metrics["statements_processed"] += len(statements)
        self.metrics["conflicts_detected"] += len(processing_report["new_conflicts"])
        self.metrics["processing_time_ms"] += elapsed_ms
        
        if self.detected_conflicts:
            self.metrics["average_confidence"] = sum(
                c.confidence_score for c in self.detected_conflicts
            ) / len(self.detected_conflicts)
        
        processing_report["elapsed_ms"] = elapsed_ms
        processing_report["total_active_statements"] = len(self.statement_fingerprints)
        processing_report["total_conflicts_detected"] = len(self.detected_conflicts)
        
        return processing_report
    
    def get_conflict_analysis(self, statement_id: str) -> Dict[str, Any]:
        """
        Get detailed conflict analysis for a specific statement.
        
        Returns conflicts involving the statement and recommendations.
        """
        conflicts_involving_statement = [
            conflict for conflict in self.detected_conflicts
            if conflict.statement_a_id == statement_id or conflict.statement_b_id == statement_id
        ]
        
        if not conflicts_involving_statement:
            return {
                "statement_id": statement_id,
                "conflicts_found": 0,
                "status": "no_conflicts",
                "recommendation": "Statement appears consistent with existing knowledge"
            }
        
        # Analyze conflict patterns
        conflict_types = {}
        max_confidence = 0
        opposing_statements = set()
        
        for conflict in conflicts_involving_statement:
            conflict_type = conflict.conflict_type.value
            conflict_types[conflict_type] = conflict_types.get(conflict_type, 0) + 1
            max_confidence = max(max_confidence, conflict.confidence_score)
            
            # Add opposing statement
            if conflict.statement_a_id == statement_id:
                opposing_statements.add(conflict.statement_b_id)
            else:
                opposing_statements.add(conflict.statement_a_id)
        
        # Generate recommendation
        recommendation = self._generate_conflict_recommendation(
            len(conflicts_involving_statement), max_confidence, conflict_types
        )
        
        return {
            "statement_id": statement_id,
            "conflicts_found": len(conflicts_involving_statement),
            "max_confidence": max_confidence,
            "conflict_types": conflict_types,
            "opposing_statements": list(opposing_statements),
            "status": "conflicts_detected" if conflicts_involving_statement else "no_conflicts",
            "recommendation": recommendation,
            "detailed_conflicts": [
                {
                    "opposing_statement": (conflict.statement_b_id if conflict.statement_a_id == statement_id 
                                         else conflict.statement_a_id),
                    "conflict_type": conflict.conflict_type.value,
                    "confidence": conflict.confidence_score,
                    "evidence": conflict.opposition_indicators,
                    "age_seconds": conflict.get_age_seconds()
                }
                for conflict in conflicts_involving_statement
            ]
        }
    
    def get_global_conflict_summary(self) -> Dict[str, Any]:
        """Get summary of all conflicts in the system."""
        if not self.detected_conflicts:
            return {
                "total_conflicts": 0,
                "conflict_types": {},
                "confidence_distribution": {"high": 0, "medium": 0, "low": 0},
                "recent_conflicts_1h": 0,
                "status": "healthy",
                "system_health_score": 1.0,
                "recommendations": ["Continue monitoring for new conflicts"],
                "metrics": self.metrics.copy()
            }
        
        # Analyze conflict distribution
        conflict_types = {}
        confidence_distribution = {"high": 0, "medium": 0, "low": 0}
        recent_conflicts = 0
        
        for conflict in self.detected_conflicts:
            # Count by type
            conflict_type = conflict.conflict_type.value
            conflict_types[conflict_type] = conflict_types.get(conflict_type, 0) + 1
            
            # Count by confidence
            if conflict.confidence_score >= 0.8:
                confidence_distribution["high"] += 1
            elif conflict.confidence_score >= 0.6:
                confidence_distribution["medium"] += 1
            else:
                confidence_distribution["low"] += 1
                
            # Count recent conflicts (last hour)
            if conflict.get_age_seconds() < 3600:
                recent_conflicts += 1
        
        # Determine system health
        high_confidence_conflicts = confidence_distribution["high"]
        if high_confidence_conflicts > 5:
            status = "critical"
        elif high_confidence_conflicts > 2:
            status = "warning"
        elif confidence_distribution["medium"] + confidence_distribution["low"] > 10:
            status = "monitoring"
        else:
            status = "healthy"
        
        health_score = self._calculate_health_score()
        recommendations = self._generate_system_recommendations(status, conflict_types)
        
        return {
            "total_conflicts": len(self.detected_conflicts),
            "conflict_types": conflict_types,
            "confidence_distribution": confidence_distribution,
            "recent_conflicts_1h": recent_conflicts,
            "status": status,
            "system_health_score": health_score,
            "recommendations": recommendations,
            "metrics": self.metrics.copy()
        }
    
    def resolve_conflict(self, conflict_id: str, resolution: str) -> bool:
        """
        Mark a conflict as resolved with explanation.
        
        Args:
            conflict_id: ID of conflict to resolve
            resolution: Explanation of how conflict was resolved
            
        Returns:
            True if conflict was found and resolved
        """
        for i, conflict in enumerate(self.detected_conflicts):
            if self._generate_conflict_id(conflict) == conflict_id:
                # Move to history
                resolved_conflict = conflict
                self.conflict_history.append(resolved_conflict)
                self.detected_conflicts.pop(i)
                self.metrics["false_positives_resolved"] += 1
                return True
        return False
    
    def _create_statement_fingerprint(self, statement_id: str, content: str, metadata: Dict[str, Any]) -> StatementFingerprint:
        """Create semantic and structural fingerprint for a statement."""
        # Generate embedding if provider available
        embedding = []
        if self.embedding_provider:
            try:
                embedding = self.embedding_provider.embed_text(content)
            except Exception:
                # Fallback to empty embedding
                pass
        
        # Detect negation indicators
        content_lower = content.lower()
        negation_indicators = [
            pattern for pattern in self.negation_patterns
            if pattern in content_lower
        ]
        
        # Calculate assertion strength
        assertion_indicators = [
            pattern for pattern in self.assertion_patterns
            if pattern in content_lower
        ]
        assertion_strength = min(len(assertion_indicators) * 0.2, 1.0)
        
        # Extract temporal markers
        temporal_markers = [
            pattern for pattern in self.temporal_patterns
            if pattern in content_lower
        ]
        
        # Extract domain tags (simple keyword-based)
        domain_tags = set()
        if "debug" in content_lower or "development" in content_lower:
            domain_tags.add("development")
        if "memory" in content_lower or "storage" in content_lower:
            domain_tags.add("memory")
        if "process" in content_lower or "algorithm" in content_lower:
            domain_tags.add("processing")
        if "semantic" in content_lower or "meaning" in content_lower:
            domain_tags.add("semantics")
        
        return StatementFingerprint(
            statement_id=statement_id,
            content=content,
            embedding=embedding,
            negation_indicators=negation_indicators,
            assertion_strength=assertion_strength,
            temporal_markers=temporal_markers,
            domain_tags=domain_tags,
            creation_timestamp=time.time()
        )
    
    def _detect_conflicts_for_statement(self, new_fingerprint: StatementFingerprint) -> List[ConflictEvidence]:
        """Detect conflicts between new statement and existing statements."""
        conflicts = []
        
        for existing_id, existing_fingerprint in self.statement_fingerprints.items():
            if existing_id == new_fingerprint.statement_id:
                continue  # Don't compare with self
                
            # Check for semantic opposition
            if self.embedding_provider and new_fingerprint.embedding and existing_fingerprint.embedding:
                similarity = self.embedding_provider.calculate_similarity(
                    new_fingerprint.embedding, existing_fingerprint.embedding
                )
                
                # High semantic similarity with negation indicators suggests opposition
                if similarity > self.semantic_similarity_threshold:
                    opposition_score = self._calculate_opposition_score(new_fingerprint, existing_fingerprint)
                    
                    if opposition_score > self.opposition_threshold:
                        # Calculate context overlap
                        context_overlap = len(new_fingerprint.domain_tags & existing_fingerprint.domain_tags) / \
                                        max(len(new_fingerprint.domain_tags | existing_fingerprint.domain_tags), 1)
                        
                        # Collect opposition evidence
                        opposition_indicators = []
                        if new_fingerprint.negation_indicators and not existing_fingerprint.negation_indicators:
                            opposition_indicators.extend(new_fingerprint.negation_indicators)
                        elif existing_fingerprint.negation_indicators and not new_fingerprint.negation_indicators:
                            opposition_indicators.extend(existing_fingerprint.negation_indicators)
                        
                        # Determine conflict type
                        conflict_type = self._determine_conflict_type(new_fingerprint, existing_fingerprint)
                        
                        # Calculate confidence score
                        confidence = self._calculate_confidence_score(
                            similarity, opposition_score, context_overlap, opposition_indicators
                        )
                        
                        if confidence >= self.min_confidence_score:
                            conflict = ConflictEvidence(
                                statement_a_id=new_fingerprint.statement_id,
                                statement_b_id=existing_fingerprint.statement_id,
                                conflict_type=conflict_type,
                                confidence_score=confidence,
                                semantic_distance=1.0 - similarity,
                                opposition_indicators=opposition_indicators,
                                context_overlap=context_overlap,
                                detection_timestamp=time.time()
                            )
                            conflicts.append(conflict)
        
        return conflicts
    
    def _calculate_opposition_score(self, fp1: StatementFingerprint, fp2: StatementFingerprint) -> float:
        """Calculate how much two statements oppose each other."""
        score = 0.0
        
        # Negation opposition (one has negation, other doesn't)
        if (fp1.negation_indicators and not fp2.negation_indicators) or \
           (fp2.negation_indicators and not fp1.negation_indicators):
            score += 0.4
        
        # Strong assertion differences
        assertion_diff = abs(fp1.assertion_strength - fp2.assertion_strength)
        if assertion_diff > 0.5:
            score += 0.3
        
        # Temporal conflicts
        if fp1.temporal_markers and fp2.temporal_markers:
            # Simple temporal conflict detection
            if any(marker in ["before", "earlier"] for marker in fp1.temporal_markers) and \
               any(marker in ["after", "later"] for marker in fp2.temporal_markers):
                score += 0.3
        
        return min(score, 1.0)
    
    def _determine_conflict_type(self, fp1: StatementFingerprint, fp2: StatementFingerprint) -> ConflictType:
        """Determine the type of conflict between two statements."""
        # Check for semantic opposition
        if (fp1.negation_indicators and not fp2.negation_indicators) or \
           (fp2.negation_indicators and not fp1.negation_indicators):
            return ConflictType.SEMANTIC_OPPOSITION
        
        # Check for temporal conflicts
        if fp1.temporal_markers and fp2.temporal_markers:
            return ConflictType.TEMPORAL_CONFLICT
        
        # Check for logical contradiction (high assertion strength difference)
        if abs(fp1.assertion_strength - fp2.assertion_strength) > 0.6:
            return ConflictType.LOGICAL_CONTRADICTION
        
        # Default to factual inconsistency
        return ConflictType.FACTUAL_INCONSISTENCY
    
    def _calculate_confidence_score(self, similarity: float, opposition_score: float, 
                                  context_overlap: float, indicators: List[str]) -> float:
        """Calculate confidence score for a conflict detection."""
        base_score = (similarity * 0.4) + (opposition_score * 0.4) + (context_overlap * 0.2)
        
        # Boost confidence if we have clear opposition indicators
        indicator_boost = min(len(indicators) * 0.1, 0.2)
        
        return min(base_score + indicator_boost, 1.0)
    
    def _cleanup_old_statements(self):
        """Remove old statements that exceed the maximum age."""
        current_time = time.time()
        max_age_seconds = self.max_statement_age_hours * 3600
        
        old_statement_ids = [
            stmt_id for stmt_id, fingerprint in self.statement_fingerprints.items()
            if current_time - fingerprint.creation_timestamp > max_age_seconds
        ]
        
        for stmt_id in old_statement_ids:
            del self.statement_fingerprints[stmt_id]
        
        # Also cleanup old conflicts
        self.detected_conflicts = [
            conflict for conflict in self.detected_conflicts
            if current_time - conflict.detection_timestamp < max_age_seconds
        ]
    
    def _generate_conflict_id(self, conflict: ConflictEvidence) -> str:
        """Generate unique ID for a conflict."""
        content = f"{conflict.statement_a_id}_{conflict.statement_b_id}_{conflict.conflict_type.value}"
        return hashlib.md5(content.encode()).hexdigest()[:12]
    
    def _generate_conflict_recommendation(self, conflict_count: int, max_confidence: float, 
                                        conflict_types: Dict[str, int]) -> str:
        """Generate recommendation for resolving conflicts."""
        if conflict_count == 0:
            return "No conflicts detected - statement appears consistent"
        
        if max_confidence > 0.9:
            return "High confidence conflict detected - manual review recommended"
        elif max_confidence > 0.7:
            return "Moderate conflict detected - verify statement accuracy"
        else:
            return "Low confidence conflicts - monitor for patterns"
    
    def _generate_system_recommendations(self, status: str, conflict_types: Dict[str, int]) -> List[str]:
        """Generate system-level recommendations."""
        recommendations = []
        
        if status == "critical":
            recommendations.append("Immediate review required - multiple high-confidence conflicts")
            recommendations.append("Consider statement validation workflow")
        elif status == "warning":
            recommendations.append("Monitor conflicts closely - elevated conflict level")
            recommendations.append("Review recent statements for accuracy")
        
        # Type-specific recommendations
        if conflict_types.get("semantic_opposition", 0) > 3:
            recommendations.append("Multiple semantic oppositions detected - check for negation errors")
        
        if conflict_types.get("temporal_conflict", 0) > 2:
            recommendations.append("Temporal conflicts detected - verify timeline consistency")
        
        if not recommendations:
            recommendations.append("System operating normally - continue monitoring")
        
        return recommendations
    
    def _calculate_health_score(self) -> float:
        """Calculate overall system health score (0.0 to 1.0)."""
        if not self.detected_conflicts:
            return 1.0
        
        high_confidence_conflicts = sum(
            1 for conflict in self.detected_conflicts 
            if conflict.confidence_score > 0.8
        )
        
        total_statements = len(self.statement_fingerprints)
        if total_statements == 0:
            return 1.0
        
        # Health score decreases with conflict ratio
        conflict_ratio = len(self.detected_conflicts) / total_statements
        high_confidence_penalty = high_confidence_conflicts * 0.1
        
        health_score = 1.0 - min(conflict_ratio + high_confidence_penalty, 0.9)
        return max(health_score, 0.1)  # Minimum 0.1 health score