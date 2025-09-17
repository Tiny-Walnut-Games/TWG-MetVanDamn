"""
Sentiment Lens Plugin

Analyzes emotional context and sentiment of cognitive events to provide
emotional intelligence overlays for the Cognitive Geo-Thermal Lore Engine.
"""

import re
from typing import Dict, Any, Optional, List
from collections import defaultdict

from ...base_plugin import CognitiveEventPlugin, PluginMetadata
from ....audio_event_bus import AudioEvent, AudioEventType


class SentimentLensPlugin(CognitiveEventPlugin):
    """
    Plugin that analyzes sentiment and emotional context of cognitive events.
    
    Provides emotional intelligence by analyzing text content, conflict patterns,
    and cognitive anchor activation patterns for emotional undertones.
    """
    
    def __init__(self, metadata: PluginMetadata):
        super().__init__(metadata)
        self.sentiment_history = []
        self.emotional_patterns = defaultdict(int)
        self.sentiment_lexicon = self._build_sentiment_lexicon()
        
        # Configuration
        self.sensitivity_threshold = 0.3
        self.analysis_depth = "standard"
        self.include_emotional_tone = True
    
    def initialize(self, context: Dict[str, Any]) -> bool:
        """Initialize sentiment analysis components."""
        try:
            # Load configuration if available
            config = context.get("configuration", {})
            self.sensitivity_threshold = config.get("sensitivity_threshold", 0.3)
            self.analysis_depth = config.get("analysis_depth", "standard")
            self.include_emotional_tone = config.get("include_emotional_tone", True)
            
            print(f"SentimentLens initialized with sensitivity={self.sensitivity_threshold}")
            return True
            
        except Exception as e:
            print(f"SentimentLens initialization error: {e}")
            return False
    
    def process_event(self, event: AudioEvent) -> Optional[Dict[str, Any]]:
        """Analyze sentiment and emotional context of cognitive events."""
        
        # Extract text content for analysis
        text_content = self._extract_text_content(event)
        if not text_content:
            return None
        
        # Perform sentiment analysis
        sentiment_score = self._analyze_sentiment(text_content)
        emotional_tone = self._analyze_emotional_tone(text_content) if self.include_emotional_tone else None
        
        # Context-aware analysis based on event type
        contextual_insights = self._analyze_event_context(event, sentiment_score)
        
        # Update cognitive state
        self._update_emotional_state(event.event_type, sentiment_score, emotional_tone)
        
        # Generate insights
        insights = self._generate_sentiment_insights(sentiment_score, emotional_tone, contextual_insights)
        
        # Track patterns
        self._track_emotional_patterns(event.event_type, sentiment_score, event.timestamp)
        
        return {
            "sentiment_analysis": {
                "score": sentiment_score,
                "emotional_tone": emotional_tone,
                "insights": insights,
                "contextual_analysis": contextual_insights,
                "confidence": self._calculate_confidence(text_content)
            },
            "publish_events": self._generate_emotional_events(sentiment_score, emotional_tone)
        }
    
    def _extract_text_content(self, event: AudioEvent) -> str:
        """Extract analyzable text content from event data."""
        text_parts = []
        
        # Extract text from various event data fields
        data = event.data
        
        if "text" in data:
            text_parts.append(str(data["text"]))
        
        if "content" in data:
            text_parts.append(str(data["content"]))
        
        if "summary" in data:
            text_parts.append(str(data["summary"]))
        
        if "anchor_text" in data:
            text_parts.append(str(data["anchor_text"]))
        
        if "conflict_description" in data:
            text_parts.append(str(data["conflict_description"]))
        
        return " ".join(text_parts).strip()
    
    def _analyze_sentiment(self, text: str) -> float:
        """
        Analyze sentiment polarity of text content.
        
        Returns:
            Sentiment score from -1.0 (negative) to +1.0 (positive)
        """
        if not text:
            return 0.0
        
        words = re.findall(r'\b\w+\b', text.lower())
        
        sentiment_sum = 0.0
        word_count = 0
        
        for word in words:
            if word in self.sentiment_lexicon:
                sentiment_sum += self.sentiment_lexicon[word]
                word_count += 1
        
        if word_count == 0:
            return 0.0
        
        # Average sentiment, normalized
        raw_sentiment = sentiment_sum / word_count
        
        # Apply sensitivity threshold
        if abs(raw_sentiment) < self.sensitivity_threshold:
            return 0.0
        
        # Normalize to [-1, 1] range
        return max(-1.0, min(1.0, raw_sentiment))
    
    def _analyze_emotional_tone(self, text: str) -> Optional[Dict[str, float]]:
        """Analyze emotional tone beyond simple positive/negative sentiment."""
        if not text:
            return None
        
        emotional_indicators = {
            "excitement": ["amazing", "awesome", "fantastic", "incredible", "brilliant"],
            "concern": ["worry", "concern", "issue", "problem", "trouble"],
            "curiosity": ["interesting", "wonder", "explore", "discover", "investigate"],
            "satisfaction": ["complete", "finished", "accomplished", "success", "achieved"],
            "frustration": ["difficult", "challenging", "stuck", "blocked", "confused"]
        }
        
        text_lower = text.lower()
        tone_scores = {}
        
        for emotion, indicators in emotional_indicators.items():
            score = sum(1 for indicator in indicators if indicator in text_lower)
            if score > 0:
                tone_scores[emotion] = min(1.0, score / len(indicators))
        
        return tone_scores if tone_scores else None
    
    def _analyze_event_context(self, event: AudioEvent, sentiment_score: float) -> Dict[str, Any]:
        """Analyze sentiment in context of specific event types."""
        contextual_insights = {
            "event_type_context": event.event_type.value,
            "intensity_correlation": event.intensity
        }
        
        if event.event_type == AudioEventType.CONFLICT_DETECTED:
            # Conflicts might have negative sentiment but high cognitive value
            contextual_insights["conflict_sentiment_analysis"] = {
                "is_constructive": sentiment_score > -0.5,
                "emotional_intensity": abs(sentiment_score),
                "resolution_potential": max(0, sentiment_score + 0.3)
            }
        
        elif event.event_type == AudioEventType.ANCHOR_ACTIVATED:
            # Anchor activation with positive sentiment indicates strong learning
            contextual_insights["learning_sentiment"] = {
                "positive_reinforcement": sentiment_score > 0.2,
                "engagement_level": (sentiment_score + 1.0) / 2.0
            }
        
        elif event.event_type == AudioEventType.SUMMARY_GENERATED:
            # Summary sentiment indicates comprehension satisfaction
            contextual_insights["comprehension_sentiment"] = {
                "satisfaction_level": sentiment_score,
                "clarity_indicator": sentiment_score > 0.0
            }
        
        return contextual_insights
    
    def _update_emotional_state(self, event_type: AudioEventType, sentiment: float, tone: Optional[Dict[str, float]]) -> None:
        """Update plugin's emotional state tracking."""
        self.cognitive_state.update({
            "current_sentiment": sentiment,
            "last_event_type": event_type.value,
            "emotional_tone": tone,
            "sentiment_trend": self._calculate_sentiment_trend()
        })
    
    def _calculate_sentiment_trend(self) -> str:
        """Calculate recent sentiment trend."""
        if len(self.sentiment_history) < 3:
            return "insufficient_data"
        
        recent_sentiments = [entry["sentiment"] for entry in self.sentiment_history[-5:]]
        avg_recent = sum(recent_sentiments) / len(recent_sentiments)
        
        if avg_recent > 0.2:
            return "positive_trend"
        elif avg_recent < -0.2:
            return "negative_trend"
        else:
            return "neutral_trend"
    
    def _generate_sentiment_insights(self, sentiment: float, tone: Optional[Dict[str, float]], context: Dict[str, Any]) -> List[str]:
        """Generate human-readable sentiment insights."""
        insights = []
        
        if abs(sentiment) > 0.5:
            polarity = "positive" if sentiment > 0 else "negative"
            intensity = "strong" if abs(sentiment) > 0.7 else "moderate"
            insights.append(f"Detected {intensity} {polarity} sentiment (score: {sentiment:.2f})")
        
        if tone:
            dominant_emotions = sorted(tone.items(), key=lambda x: x[1], reverse=True)[:2]
            for emotion, score in dominant_emotions:
                if score > 0.3:
                    insights.append(f"Emotional tone: {emotion} (strength: {score:.2f})")
        
        # Contextual insights
        if "conflict_sentiment_analysis" in context:
            conflict_analysis = context["conflict_sentiment_analysis"]
            if conflict_analysis["is_constructive"]:
                insights.append("Conflict shows constructive sentiment pattern")
        
        return insights
    
    def _track_emotional_patterns(self, event_type: AudioEventType, sentiment: float, event_timestamp: float = None) -> None:
        """Track emotional patterns over time."""
        # Store in history
        self.sentiment_history.append({
            "timestamp": event_timestamp or time.time(),
            "event_type": event_type.value,
            "sentiment": sentiment
        })
        
        # Limit history size
        if len(self.sentiment_history) > 100:
            self.sentiment_history = self.sentiment_history[-50:]
        
        # Update pattern tracking
        pattern_key = f"{event_type.value}_sentiment"
        self.emotional_patterns[pattern_key] += 1
    
    def _calculate_confidence(self, text: str) -> float:
        """Calculate confidence in sentiment analysis."""
        if not text:
            return 0.0
        
        word_count = len(re.findall(r'\b\w+\b', text))
        lexicon_matches = sum(1 for word in re.findall(r'\b\w+\b', text.lower()) 
                             if word in self.sentiment_lexicon)
        
        if word_count == 0:
            return 0.0
        
        coverage = lexicon_matches / word_count
        length_factor = min(1.0, word_count / 10.0)  # More confidence with more words
        
        return min(0.95, coverage * length_factor)
    
    def _generate_emotional_events(self, sentiment: float, tone: Optional[Dict[str, float]]) -> List[Dict[str, Any]]:
        """Generate new emotional events based on analysis."""
        events = []
        
        # High intensity emotional events
        if abs(sentiment) > 0.8:
            events.append({
                "event_type": "cognitive_cycle_start",  # Reuse existing event type
                "data": {
                    "emotional_intensity": abs(sentiment),
                    "emotional_context": "high_sentiment_detected",
                    "sentiment_score": sentiment
                },
                "intensity": min(1.0, abs(sentiment)),
                "affect_layer": "sentiment_lens"
            })
        
        return events
    
    def _build_sentiment_lexicon(self) -> Dict[str, float]:
        """Build a basic sentiment lexicon for analysis."""
        return {
            # Positive words
            "excellent": 0.8, "amazing": 0.9, "great": 0.6, "good": 0.4, "success": 0.7,
            "wonderful": 0.8, "fantastic": 0.9, "brilliant": 0.8, "perfect": 0.9,
            "love": 0.8, "happy": 0.7, "joy": 0.8, "excited": 0.7, "pleased": 0.6,
            "accomplished": 0.7, "achieved": 0.6, "completed": 0.5, "satisfied": 0.6,
            "effective": 0.5, "efficient": 0.5, "clear": 0.4, "smooth": 0.5,
            
            # Negative words  
            "terrible": -0.8, "awful": -0.8, "bad": -0.4, "poor": -0.5, "failed": -0.7,
            "horrible": -0.9, "disaster": -0.9, "hate": -0.8, "angry": -0.7, "frustrated": -0.6,
            "confused": -0.4, "difficult": -0.3, "problem": -0.4, "issue": -0.3, "trouble": -0.5,
            "error": -0.4, "broken": -0.5, "stuck": -0.4, "blocked": -0.5, "complicated": -0.3,
            
            # Neutral but contextually important
            "interesting": 0.2, "noted": 0.1, "observed": 0.1, "detected": 0.0,
            "analyzed": 0.1, "processed": 0.1, "considered": 0.1
        }
    
    def get_cognitive_insights(self) -> Dict[str, Any]:
        """Get comprehensive cognitive insights from sentiment analysis."""
        base_insights = super().get_cognitive_insights()
        
        sentiment_insights = {
            "sentiment_patterns": dict(self.emotional_patterns),
            "sentiment_history_summary": self._summarize_sentiment_history(),
            "emotional_stability": self._calculate_emotional_stability(),
            "dominant_emotional_themes": self._identify_dominant_themes()
        }
        
        base_insights["sentiment_analysis"] = sentiment_insights
        return base_insights
    
    def _summarize_sentiment_history(self) -> Dict[str, Any]:
        """Summarize sentiment history for insights."""
        if not self.sentiment_history:
            return {"status": "no_history"}
        
        sentiments = [entry["sentiment"] for entry in self.sentiment_history]
        return {
            "total_events": len(sentiments),
            "average_sentiment": sum(sentiments) / len(sentiments),
            "sentiment_range": (min(sentiments), max(sentiments)),
            "positive_events": sum(1 for s in sentiments if s > 0.2),
            "negative_events": sum(1 for s in sentiments if s < -0.2),
            "neutral_events": sum(1 for s in sentiments if -0.2 <= s <= 0.2)
        }
    
    def _calculate_emotional_stability(self) -> float:
        """Calculate emotional stability based on sentiment variance."""
        if len(self.sentiment_history) < 3:
            return 0.5  # Default neutral stability
        
        sentiments = [entry["sentiment"] for entry in self.sentiment_history[-20:]]
        variance = sum((s - sum(sentiments)/len(sentiments))**2 for s in sentiments) / len(sentiments)
        
        # Stability is inverse of variance, normalized
        stability = 1.0 / (1.0 + variance)
        return stability
    
    def _identify_dominant_themes(self) -> List[str]:
        """Identify dominant emotional themes from patterns."""
        themes = []
        
        if self.emotional_patterns.get("anchor_activated_sentiment", 0) > 5:
            themes.append("active_learning")
        
        if self.emotional_patterns.get("conflict_detected_sentiment", 0) > 3:
            themes.append("cognitive_tension")
        
        if self.emotional_patterns.get("summary_generated_sentiment", 0) > 4:
            themes.append("comprehension_focus")
        
        return themes