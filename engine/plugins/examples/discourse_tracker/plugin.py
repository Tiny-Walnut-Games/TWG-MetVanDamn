"""
Discourse Tracker Plugin

Analyzes discourse markers and conversation patterns in cognitive events
to understand information flow and cognitive coherence patterns.
"""

import re
from typing import Dict, Any, Optional, List, Set, Tuple
from collections import defaultdict, deque
from dataclasses import dataclass

from ...base_plugin import CognitiveEventPlugin, PluginMetadata
from ....audio_event_bus import AudioEvent, AudioEventType


@dataclass
class DiscourseMarker:
    """Represents a discourse marker found in text."""
    marker: str
    category: str
    position: int
    context: str
    strength: float


class DiscourseTrackerPlugin(CognitiveEventPlugin):
    """
    Plugin that tracks discourse markers and conversation patterns.
    
    Analyzes linguistic patterns, topic shifts, coherence markers, and
    cognitive flow indicators in the stream of cognitive events.
    """
    
    def __init__(self, metadata: PluginMetadata):
        super().__init__(metadata)
        self.discourse_history = deque(maxlen=50)
        self.topic_coherence_tracker = {}
        self.transition_patterns = defaultdict(int)
        self.discourse_markers = self._build_discourse_marker_lexicon()
        
        # Configuration
        self.track_transitions = True
        self.analyze_coherence = True
        self.detect_topic_shifts = True
        
        # Analysis state
        self.current_topic_indicators = set()
        self.last_cognitive_phase = None
        self.coherence_score = 0.5
    
    def initialize(self, context: Dict[str, Any]) -> bool:
        """Initialize discourse tracking components."""
        try:
            # Load configuration
            config = context.get("configuration", {})
            self.track_transitions = config.get("track_transitions", True)
            self.analyze_coherence = config.get("analyze_coherence", True)
            self.detect_topic_shifts = config.get("detect_topic_shifts", True)
            
            print(f"DiscourseTracker initialized with coherence analysis: {self.analyze_coherence}")
            return True
            
        except Exception as e:
            print(f"DiscourseTracker initialization error: {e}")
            return False
    
    def process_event(self, event: AudioEvent) -> Optional[Dict[str, Any]]:
        """Analyze discourse patterns in cognitive events."""
        
        # Extract text content
        text_content = self._extract_text_content(event)
        
        # Analyze discourse markers
        markers = self._identify_discourse_markers(text_content)
        
        # Track cognitive phase transitions
        phase_transition = self._track_cognitive_phase_transition(event)
        
        # Analyze topic coherence
        coherence_analysis = self._analyze_topic_coherence(text_content, event) if self.analyze_coherence else None
        
        # Detect topic shifts
        topic_shift = self._detect_topic_shift(text_content, event) if self.detect_topic_shifts else None
        
        # Update discourse state
        self._update_discourse_state(event, markers, phase_transition, coherence_analysis)
        
        # Generate discourse insights
        insights = self._generate_discourse_insights(markers, phase_transition, coherence_analysis, topic_shift)
        
        return {
            "discourse_analysis": {
                "markers": [self._marker_to_dict(m) for m in markers],
                "phase_transition": phase_transition,
                "coherence_analysis": coherence_analysis,
                "topic_shift": topic_shift,
                "insights": insights,
                "flow_quality": self._assess_flow_quality()
            },
            "publish_events": self._generate_discourse_events(markers, phase_transition)
        }
    
    def _extract_text_content(self, event: AudioEvent) -> str:
        """Extract text content from event for discourse analysis."""
        text_parts = []
        data = event.data
        
        # Extract text from various fields
        for field in ["text", "content", "summary", "anchor_text", "description"]:
            if field in data and data[field]:
                text_parts.append(str(data[field]))
        
        return " ".join(text_parts).strip()
    
    def _identify_discourse_markers(self, text: str) -> List[DiscourseMarker]:
        """Identify discourse markers in text content."""
        if not text:
            return []
        
        markers = []
        text_lower = text.lower()
        
        for category, marker_list in self.discourse_markers.items():
            for marker_info in marker_list:
                marker_text = marker_info["text"]
                pattern = marker_info["pattern"]
                strength = marker_info["strength"]
                
                # Find all occurrences
                for match in re.finditer(pattern, text_lower):
                    start_pos = match.start()
                    context = self._extract_context(text, start_pos, 20)
                    
                    markers.append(DiscourseMarker(
                        marker=marker_text,
                        category=category,
                        position=start_pos,
                        context=context,
                        strength=strength
                    ))
        
        return sorted(markers, key=lambda m: m.position)
    
    def _extract_context(self, text: str, position: int, context_length: int = 20) -> str:
        """Extract context around a discourse marker."""
        start = max(0, position - context_length)
        end = min(len(text), position + context_length)
        return text[start:end].strip()
    
    def _track_cognitive_phase_transition(self, event: AudioEvent) -> Optional[Dict[str, Any]]:
        """Track transitions between cognitive phases."""
        if not self.track_transitions:
            return None
        
        current_phase = self._identify_cognitive_phase(event)
        
        transition_info = None
        if self.last_cognitive_phase and current_phase != self.last_cognitive_phase:
            transition_key = f"{self.last_cognitive_phase}_to_{current_phase}"
            self.transition_patterns[transition_key] += 1
            
            transition_info = {
                "from_phase": self.last_cognitive_phase,
                "to_phase": current_phase,
                "transition_type": self._classify_transition_type(self.last_cognitive_phase, current_phase),
                "frequency": self.transition_patterns[transition_key]
            }
        
        self.last_cognitive_phase = current_phase
        return transition_info
    
    def _identify_cognitive_phase(self, event: AudioEvent) -> str:
        """Identify the cognitive phase from event type and content."""
        phase_mapping = {
            AudioEventType.COGNITIVE_CYCLE_START: "initiation",
            AudioEventType.ANCHOR_ACTIVATED: "processing",
            AudioEventType.ANCHOR_REINFORCED: "reinforcement", 
            AudioEventType.CONFLICT_DETECTED: "conflict_resolution",
            AudioEventType.SUMMARY_GENERATED: "synthesis",
            AudioEventType.CLUSTER_FORMED: "organization",
            AudioEventType.COGNITIVE_CYCLE_END: "completion"
        }
        
        return phase_mapping.get(event.event_type, "unknown")
    
    def _classify_transition_type(self, from_phase: str, to_phase: str) -> str:
        """Classify the type of cognitive phase transition."""
        # Define transition categories
        progressive_transitions = {
            ("initiation", "processing"): "natural_progression",
            ("processing", "synthesis"): "natural_progression",
            ("synthesis", "completion"): "natural_progression"
        }
        
        iterative_transitions = {
            ("processing", "processing"): "iterative_deepening",
            ("conflict_resolution", "processing"): "conflict_resolution_cycle"
        }
        
        key = (from_phase, to_phase)
        
        if key in progressive_transitions:
            return progressive_transitions[key]
        elif key in iterative_transitions:
            return iterative_transitions[key]
        elif to_phase == "conflict_resolution":
            return "conflict_emergence"
        else:
            return "irregular_transition"
    
    def _analyze_topic_coherence(self, text: str, event: AudioEvent) -> Dict[str, Any]:
        """Analyze topic coherence and thematic consistency."""
        if not text:
            return {"coherence_score": 0.5, "analysis": "no_content"}
        
        # Extract key terms and topics
        current_topics = self._extract_topic_indicators(text)
        
        # Calculate coherence with previous content
        coherence_score = self._calculate_coherence_score(current_topics)
        
        # Update topic tracking
        self.current_topic_indicators.update(current_topics)
        
        # Trim topic indicators to prevent infinite growth
        if len(self.current_topic_indicators) > 100:
            # Keep only the most recent/relevant topics
            self.current_topic_indicators = set(list(self.current_topic_indicators)[-50:])
        
        return {
            "coherence_score": coherence_score,
            "current_topics": list(current_topics),
            "topic_continuity": len(current_topics.intersection(self.current_topic_indicators)) / max(1, len(current_topics)),
            "analysis": self._interpret_coherence_score(coherence_score)
        }
    
    def _extract_topic_indicators(self, text: str) -> Set[str]:
        """Extract topic indicators from text content."""
        # Simple approach: extract meaningful words/terms
        words = re.findall(r'\b[a-zA-Z]{3,}\b', text.lower())
        
        # Filter out common words
        stop_words = {"the", "and", "but", "for", "are", "with", "this", "that", "they", "have", "was", "been", "from"}
        meaningful_words = [w for w in words if w not in stop_words and len(w) > 3]
        
        return set(meaningful_words[:20])  # Limit to top 20 terms
    
    def _calculate_coherence_score(self, current_topics: Set[str]) -> float:
        """Calculate coherence score based on topic overlap."""
        if not self.current_topic_indicators or not current_topics:
            return 0.5
        
        overlap = len(current_topics.intersection(self.current_topic_indicators))
        union_size = len(current_topics.union(self.current_topic_indicators))
        
        # Jaccard similarity with smoothing
        coherence = overlap / max(1, union_size) if union_size > 0 else 0.5
        
        # Update running coherence score with exponential smoothing
        alpha = 0.3
        self.coherence_score = alpha * coherence + (1 - alpha) * self.coherence_score
        
        return self.coherence_score
    
    def _interpret_coherence_score(self, score: float) -> str:
        """Interpret coherence score for human understanding."""
        if score > 0.8:
            return "high_coherence"
        elif score > 0.6:
            return "moderate_coherence"
        elif score > 0.4:
            return "low_coherence"
        else:
            return "fragmented_discourse"
    
    def _detect_topic_shift(self, text: str, event: AudioEvent) -> Optional[Dict[str, Any]]:
        """Detect significant topic shifts in the discourse."""
        if not text or len(self.discourse_history) < 3:
            return None
        
        current_topics = self._extract_topic_indicators(text)
        
        # Compare with recent history
        recent_topics = set()
        for entry in list(self.discourse_history)[-3:]:
            recent_topics.update(entry.get("topics", []))
        
        # Calculate topic shift magnitude
        overlap = len(current_topics.intersection(recent_topics))
        shift_magnitude = 1.0 - (overlap / max(1, len(current_topics.union(recent_topics))))
        
        # Detect significant shifts
        if shift_magnitude > 0.7:
            return {
                "shift_detected": True,
                "shift_magnitude": shift_magnitude,
                "shift_type": "major_topic_change",
                "new_topics": list(current_topics - recent_topics),
                "abandoned_topics": list(recent_topics - current_topics)
            }
        elif shift_magnitude > 0.4:
            return {
                "shift_detected": True,
                "shift_magnitude": shift_magnitude,
                "shift_type": "topic_evolution",
                "topic_drift": list(current_topics - recent_topics)
            }
        
        return {
            "shift_detected": False,
            "shift_magnitude": shift_magnitude,
            "shift_type": "topic_continuity"
        }
    
    def _update_discourse_state(self, event: AudioEvent, markers: List[DiscourseMarker], 
                               phase_transition: Optional[Dict[str, Any]], 
                               coherence_analysis: Optional[Dict[str, Any]]) -> None:
        """Update plugin's discourse tracking state."""
        
        # Add to discourse history
        entry = {
            "timestamp": getattr(event, 'timestamp', 0),
            "event_type": event.event_type.value,
            "markers": [m.category for m in markers],
            "topics": list(self._extract_topic_indicators(self._extract_text_content(event))),
            "phase": self._identify_cognitive_phase(event)
        }
        
        if coherence_analysis:
            entry["coherence_score"] = coherence_analysis["coherence_score"]
        
        self.discourse_history.append(entry)
        
        # Update cognitive state
        self.cognitive_state.update({
            "current_coherence": self.coherence_score,
            "active_markers": len(markers),
            "current_phase": self.last_cognitive_phase,
            "discourse_quality": self._assess_discourse_quality()
        })
    
    def _assess_discourse_quality(self) -> str:
        """Assess overall discourse quality."""
        if self.coherence_score > 0.8:
            return "excellent"
        elif self.coherence_score > 0.6:
            return "good"
        elif self.coherence_score > 0.4:
            return "fair"
        else:
            return "poor"
    
    def _assess_flow_quality(self) -> Dict[str, Any]:
        """Assess the quality of cognitive flow."""
        if len(self.discourse_history) < 3:
            return {"quality": "insufficient_data"}
        
        # Analyze transition smoothness
        smooth_transitions = 0
        total_transitions = 0
        
        for transition_key, count in self.transition_patterns.items():
            total_transitions += count
            if "natural_progression" in transition_key:
                smooth_transitions += count
        
        transition_smoothness = smooth_transitions / max(1, total_transitions)
        
        return {
            "quality": "smooth" if transition_smoothness > 0.6 else "choppy",
            "transition_smoothness": transition_smoothness,
            "coherence_stability": self.coherence_score,
            "overall_score": (transition_smoothness + self.coherence_score) / 2
        }
    
    def _generate_discourse_insights(self, markers: List[DiscourseMarker], 
                                   phase_transition: Optional[Dict[str, Any]],
                                   coherence_analysis: Optional[Dict[str, Any]], 
                                   topic_shift: Optional[Dict[str, Any]]) -> List[str]:
        """Generate human-readable discourse insights."""
        insights = []
        
        # Marker insights
        if markers:
            marker_categories = defaultdict(int)
            for marker in markers:
                marker_categories[marker.category] += 1
            
            dominant_category = max(marker_categories.items(), key=lambda x: x[1])
            insights.append(f"Dominant discourse pattern: {dominant_category[0]} ({dominant_category[1]} markers)")
        
        # Phase transition insights
        if phase_transition:
            insights.append(f"Cognitive transition: {phase_transition['from_phase']} → {phase_transition['to_phase']} ({phase_transition['transition_type']})")
        
        # Coherence insights
        if coherence_analysis:
            coherence_desc = coherence_analysis["analysis"]
            score = coherence_analysis["coherence_score"]
            insights.append(f"Topic coherence: {coherence_desc} (score: {score:.2f})")
        
        # Topic shift insights
        if topic_shift and topic_shift["shift_detected"]:
            shift_type = topic_shift["shift_type"]
            magnitude = topic_shift["shift_magnitude"]
            insights.append(f"Topic shift detected: {shift_type} (magnitude: {magnitude:.2f})")
        
        return insights
    
    def _generate_discourse_events(self, markers: List[DiscourseMarker], 
                                  phase_transition: Optional[Dict[str, Any]]) -> List[Dict[str, Any]]:
        """Generate new events based on discourse analysis."""
        events = []
        
        # Significant discourse marker clusters
        if len(markers) > 3:
            events.append({
                "event_type": "cognitive_cycle_start",
                "data": {
                    "discourse_intensity": len(markers),
                    "discourse_context": "rich_linguistic_markers",
                    "marker_categories": list(set(m.category for m in markers))
                },
                "intensity": min(1.0, len(markers) / 5.0),
                "affect_layer": "discourse_tracker"
            })
        
        return events
    
    def _marker_to_dict(self, marker: DiscourseMarker) -> Dict[str, Any]:
        """Convert DiscourseMarker to dictionary for serialization."""
        return {
            "marker": marker.marker,
            "category": marker.category,
            "position": marker.position,
            "context": marker.context,
            "strength": marker.strength
        }
    
    def _build_discourse_marker_lexicon(self) -> Dict[str, List[Dict[str, Any]]]:
        """Build lexicon of discourse markers for analysis."""
        return {
            "transition": [
                {"text": "however", "pattern": r"\bhowever\b", "strength": 0.8},
                {"text": "therefore", "pattern": r"\btherefore\b", "strength": 0.8},
                {"text": "meanwhile", "pattern": r"\bmeanwhile\b", "strength": 0.7},
                {"text": "furthermore", "pattern": r"\bfurthermore\b", "strength": 0.7},
                {"text": "consequently", "pattern": r"\bconsequently\b", "strength": 0.8}
            ],
            "elaboration": [
                {"text": "specifically", "pattern": r"\bspecifically\b", "strength": 0.6},
                {"text": "for example", "pattern": r"\bfor example\b", "strength": 0.7},
                {"text": "in particular", "pattern": r"\bin particular\b", "strength": 0.6},
                {"text": "namely", "pattern": r"\bnamely\b", "strength": 0.7},
                {"text": "that is", "pattern": r"\bthat is\b", "strength": 0.6}
            ],
            "contrast": [
                {"text": "on the other hand", "pattern": r"\bon the other hand\b", "strength": 0.9},
                {"text": "in contrast", "pattern": r"\bin contrast\b", "strength": 0.8},
                {"text": "alternatively", "pattern": r"\balternatively\b", "strength": 0.7},
                {"text": "whereas", "pattern": r"\bwhereas\b", "strength": 0.7},
                {"text": "unlike", "pattern": r"\bunlike\b", "strength": 0.6}
            ],
            "summary": [
                {"text": "in conclusion", "pattern": r"\bin conclusion\b", "strength": 0.9},
                {"text": "to summarize", "pattern": r"\bto summarize\b", "strength": 0.8},
                {"text": "overall", "pattern": r"\boverall\b", "strength": 0.6},
                {"text": "in summary", "pattern": r"\bin summary\b", "strength": 0.8},
                {"text": "finally", "pattern": r"\bfinally\b", "strength": 0.7}
            ],
            "emphasis": [
                {"text": "importantly", "pattern": r"\bimportantly\b", "strength": 0.7},
                {"text": "notably", "pattern": r"\bnotably\b", "strength": 0.6},
                {"text": "significantly", "pattern": r"\bsignificantly\b", "strength": 0.7},
                {"text": "crucially", "pattern": r"\bcrucially\b", "strength": 0.8},
                {"text": "especially", "pattern": r"\bespecially\b", "strength": 0.6}
            ]
        }
    
    def get_cognitive_insights(self) -> Dict[str, Any]:
        """Get comprehensive cognitive insights from discourse analysis."""
        base_insights = super().get_cognitive_insights()
        
        discourse_insights = {
            "transition_patterns": dict(self.transition_patterns),
            "discourse_history_summary": self._summarize_discourse_history(),
            "coherence_trends": self._analyze_coherence_trends(),
            "communication_quality": self._assess_communication_quality()
        }
        
        base_insights["discourse_analysis"] = discourse_insights
        return base_insights
    
    def _summarize_discourse_history(self) -> Dict[str, Any]:
        """Summarize discourse history for insights."""
        if not self.discourse_history:
            return {"status": "no_history"}
        
        total_events = len(self.discourse_history)
        phases = [entry["phase"] for entry in self.discourse_history]
        marker_counts = defaultdict(int)
        
        for entry in self.discourse_history:
            for marker in entry.get("markers", []):
                marker_counts[marker] += 1
        
        return {
            "total_events": total_events,
            "phase_distribution": {phase: phases.count(phase) for phase in set(phases)},
            "marker_usage": dict(marker_counts),
            "average_coherence": sum(entry.get("coherence_score", 0.5) for entry in self.discourse_history) / total_events
        }
    
    def _analyze_coherence_trends(self) -> Dict[str, Any]:
        """Analyze trends in topic coherence over time."""
        coherence_scores = [entry.get("coherence_score", 0.5) for entry in self.discourse_history if "coherence_score" in entry]
        
        if len(coherence_scores) < 3:
            return {"trend": "insufficient_data"}
        
        recent_avg = sum(coherence_scores[-5:]) / min(5, len(coherence_scores))
        overall_avg = sum(coherence_scores) / len(coherence_scores)
        
        trend = "improving" if recent_avg > overall_avg + 0.1 else "declining" if recent_avg < overall_avg - 0.1 else "stable"
        
        return {
            "trend": trend,
            "recent_average": recent_avg,
            "overall_average": overall_avg,
            "volatility": max(coherence_scores) - min(coherence_scores)
        }
    
    def _assess_communication_quality(self) -> Dict[str, Any]:
        """Assess overall communication quality."""
        return {
            "discourse_richness": len(set(marker for entry in self.discourse_history for marker in entry.get("markers", []))),
            "phase_diversity": len(set(entry["phase"] for entry in self.discourse_history)),
            "coherence_quality": self._interpret_coherence_score(self.coherence_score),
            "flow_assessment": self._assess_flow_quality()
        }