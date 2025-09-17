"""
Base Plugin Classes and Interfaces

Defines the core plugin architecture for the Cognitive Geo-Thermal Lore Engine.
Plugins can register for cognitive events and provide extended functionality.
"""

from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from typing import Dict, List, Any, Optional, Set
from enum import Enum
import time

from ..audio_event_bus import AudioEvent, AudioEventType


class PluginCapability(Enum):
    """Available plugin capabilities that can be negotiated."""
    EVENT_LISTENER = "event_listener"  # Listen to cognitive events
    EVENT_PUBLISHER = "event_publisher"  # Publish new events  
    DATA_PROCESSOR = "data_processor"  # Process and transform data
    SENTIMENT_ANALYSIS = "sentiment_analysis"  # Emotional context analysis
    DISCOURSE_TRACKING = "discourse_tracking"  # Conversation pattern analysis
    VISUALIZATION = "visualization"  # Generate visual overlays
    ANALYTICS = "analytics"  # Provide metrics and insights


@dataclass
class PluginMetadata:
    """Plugin metadata and configuration."""
    name: str
    version: str
    author: str
    description: str
    capabilities: Set[PluginCapability]
    dependencies: List[str] = field(default_factory=list)
    min_engine_version: str = "0.9.0"
    max_memory_mb: int = 50  # Memory limit in MB
    max_execution_time_ms: int = 1000  # Execution timeout in milliseconds
    event_subscriptions: Set[AudioEventType] = field(default_factory=set)
    
    def __post_init__(self):
        """Ensure capabilities and event_subscriptions are sets."""
        if not isinstance(self.capabilities, set):
            self.capabilities = set(self.capabilities)
        if not isinstance(self.event_subscriptions, set):
            self.event_subscriptions = set(self.event_subscriptions)


class BasePlugin(ABC):
    """
    Base class for all cognitive plugins.
    
    Plugins extend the cognitive processing capabilities by registering for events
    and providing specialized analysis or transformations.
    """
    
    def __init__(self, metadata: PluginMetadata):
        self.metadata = metadata
        self.enabled = True
        self.stats = {
            "events_processed": 0,
            "execution_time_total_ms": 0,
            "errors": 0,
            "last_execution": None
        }
    
    @abstractmethod
    def initialize(self, context: Dict[str, Any]) -> bool:
        """
        Initialize the plugin with system context.
        
        Args:
            context: System context including configuration and resources
            
        Returns:
            True if initialization successful, False otherwise
        """
        pass
    
    @abstractmethod
    def process_event(self, event: AudioEvent) -> Optional[Dict[str, Any]]:
        """
        Process a cognitive event.
        
        Args:
            event: The cognitive event to process
            
        Returns:
            Optional processing results or None if no output
        """
        pass
    
    def shutdown(self) -> None:
        """Clean up plugin resources. Override if needed."""
        self.enabled = False
    
    def get_capabilities(self) -> Set[PluginCapability]:
        """Get plugin capabilities."""
        return self.metadata.capabilities
    
    def supports_event_type(self, event_type: AudioEventType) -> bool:
        """Check if plugin supports a specific event type."""
        return event_type in self.metadata.event_subscriptions
    
    def update_stats(self, execution_time_ms: float, error: bool = False) -> None:
        """Update plugin execution statistics."""
        self.stats["events_processed"] += 1
        self.stats["execution_time_total_ms"] += execution_time_ms
        self.stats["last_execution"] = time.time()
        if error:
            self.stats["errors"] += 1
    
    def get_stats(self) -> Dict[str, Any]:
        """Get plugin execution statistics."""
        avg_time = 0
        if self.stats["events_processed"] > 0:
            avg_time = self.stats["execution_time_total_ms"] / self.stats["events_processed"]
            
        return {
            **self.stats,
            "average_execution_time_ms": avg_time,
            "error_rate": self.stats["errors"] / max(1, self.stats["events_processed"]),
            "enabled": self.enabled
        }


class CognitiveEventPlugin(BasePlugin):
    """
    Specialized plugin for cognitive event processing.
    
    Provides additional methods for cognitive-specific functionality.
    """
    
    def __init__(self, metadata: PluginMetadata):
        super().__init__(metadata)
        self.cognitive_state = {}
    
    def update_cognitive_state(self, state_update: Dict[str, Any]) -> None:
        """Update plugin's cognitive state."""
        self.cognitive_state.update(state_update)
    
    def get_cognitive_insights(self) -> Dict[str, Any]:
        """Get cognitive insights from the plugin."""
        return {
            "state": self.cognitive_state,
            "insights": self._generate_insights()
        }
    
    def _generate_insights(self) -> Dict[str, Any]:
        """Generate cognitive insights. Override in subclasses."""
        return {}