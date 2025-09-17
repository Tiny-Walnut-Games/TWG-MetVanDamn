"""
Audio Event Bus - Multimodal Expressive Layer v0.5

Internal event system for mapping cognitive events to audio and visual expressions.
Provides a lightweight pub/sub mechanism for the Cognitive Geo-Thermal Lore Engine.
"""

from typing import Dict, List, Any, Callable, Optional
from dataclasses import dataclass
from enum import Enum
import time
import json
from threading import Lock


class AudioEventType(Enum):
    """Types of cognitive audio events."""
    ANCHOR_ACTIVATED = "anchor_activated"
    ANCHOR_REINFORCED = "anchor_reinforced" 
    CONFLICT_DETECTED = "conflict_detected"
    SUMMARY_GENERATED = "summary_generated"
    CLUSTER_FORMED = "cluster_formed"
    HEAT_THRESHOLD = "heat_threshold"
    COGNITIVE_CYCLE_START = "cognitive_cycle_start"
    COGNITIVE_CYCLE_END = "cognitive_cycle_end"


@dataclass
class AudioEvent:
    """Audio event with cognitive context."""
    event_type: AudioEventType
    timestamp: float
    data: Dict[str, Any]
    intensity: float = 0.5  # 0.0 to 1.0
    affect_layer: str = "default"
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary for serialization."""
        return {
            "event_type": self.event_type.value,
            "timestamp": self.timestamp,
            "data": self.data,
            "intensity": self.intensity,
            "affect_layer": self.affect_layer
        }


class AudioEventBus:
    """
    Lightweight event bus for cognitive audio events.
    
    Provides pub/sub functionality to connect cognitive events with audio/visual
    expressions in the multimodal layer.
    """
    
    def __init__(self):
        self._subscribers: Dict[AudioEventType, List[Callable]] = {}
        self._event_history: List[AudioEvent] = []
        self._lock = Lock()
        self._max_history = 100
        
    def subscribe(self, event_type: AudioEventType, callback: Callable[[AudioEvent], None]):
        """Subscribe to audio events."""
        with self._lock:
            if event_type not in self._subscribers:
                self._subscribers[event_type] = []
            self._subscribers[event_type].append(callback)
    
    def unsubscribe(self, event_type: AudioEventType, callback: Callable[[AudioEvent], None]):
        """Unsubscribe from audio events."""
        with self._lock:
            if event_type in self._subscribers:
                self._subscribers[event_type] = [
                    cb for cb in self._subscribers[event_type] if cb != callback
                ]
    
    def publish(self, event_type: AudioEventType, data: Dict[str, Any], 
                intensity: float = 0.5, affect_layer: str = "default"):
        """Publish an audio event."""
        event = AudioEvent(
            event_type=event_type,
            timestamp=time.time(),
            data=data,
            intensity=intensity,
            affect_layer=affect_layer
        )
        
        # Store in history
        with self._lock:
            self._event_history.append(event)
            if len(self._event_history) > self._max_history:
                self._event_history.pop(0)
        
        # Notify subscribers
        subscribers = self._subscribers.get(event_type, [])
        for callback in subscribers:
            try:
                callback(event)
            except Exception as e:
                # Log error but don't break other subscribers
                print(f"AudioEventBus: Error in subscriber callback: {e}")
    
    def get_recent_events(self, limit: int = 10) -> List[AudioEvent]:
        """Get recent events from history."""
        with self._lock:
            return self._event_history[-limit:]
    
    def get_events_by_type(self, event_type: AudioEventType, limit: int = 10) -> List[AudioEvent]:
        """Get recent events of specific type."""
        with self._lock:
            filtered = [e for e in self._event_history if e.event_type == event_type]
            return filtered[-limit:]
    
    def clear_history(self):
        """Clear event history."""
        with self._lock:
            self._event_history.clear()
    
    def get_stats(self) -> Dict[str, Any]:
        """Get event bus statistics."""
        with self._lock:
            event_counts = {}
            for event in self._event_history:
                event_type = event.event_type.value
                event_counts[event_type] = event_counts.get(event_type, 0) + 1
            
            return {
                "total_events": len(self._event_history),
                "event_counts": event_counts,
                "subscriber_counts": {
                    event_type.value: len(callbacks) 
                    for event_type, callbacks in self._subscribers.items()
                },
                "history_size": len(self._event_history),
                "max_history": self._max_history
            }