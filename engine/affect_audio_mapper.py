"""
Affect Audio Mapper - Multimodal Expressive Layer v0.5

Maps cognitive events from the Geo-Thermal Lore Engine to affect-based audio layers.
Provides configurable soundscape profiles for different cognitive states.
"""

from typing import Dict, List, Any, Optional
from dataclasses import dataclass
from .audio_event_bus import AudioEventBus, AudioEventType, AudioEvent


@dataclass
class AudioLayer:
    """Represents an audio layer with affect characteristics."""
    layer_id: str
    name: str
    base_frequency: float = 440.0  # Hz
    intensity_range: tuple = (0.1, 1.0)
    affect_type: str = "neutral"  # neutral, positive, tension, resolution
    parameters: Dict[str, Any] = None
    
    def __post_init__(self):
        if self.parameters is None:
            self.parameters = {}


@dataclass
class SoundscapeProfile:
    """A collection of audio layers for a specific cognitive context."""
    profile_id: str
    name: str
    description: str
    layers: List[AudioLayer]
    
    def get_layer(self, layer_id: str) -> Optional[AudioLayer]:
        """Get layer by ID."""
        return next((layer for layer in self.layers if layer.layer_id == layer_id), None)


class AffectAudioMapper:
    """
    Maps cognitive events to affect-based audio layers.
    
    Transforms semantic anchor activities, conflicts, and summarization events
    into expressive audio configurations.
    """
    
    def __init__(self, audio_event_bus: AudioEventBus):
        self.audio_event_bus = audio_event_bus
        self.soundscape_profiles = {}
        self.active_profile = None
        self.current_state = {
            "anchor_heat_levels": {},
            "conflict_tension": 0.0,
            "summary_flow": 0.0,
            "cognitive_load": 0.0
        }
        
        # Initialize default soundscape profiles
        self._create_default_profiles()
        
        # Subscribe to cognitive events
        self._subscribe_to_events()
    
    def _create_default_profiles(self):
        """Create default soundscape profiles."""
        
        # Explorer Profile - Light, curious soundscape
        explorer_layers = [
            AudioLayer("anchor_pulse", "Anchor Pulse", 220.0, (0.1, 0.6), "positive"),
            AudioLayer("discovery_chime", "Discovery Chime", 880.0, (0.2, 0.8), "positive"),
            AudioLayer("ambient_flow", "Ambient Flow", 110.0, (0.05, 0.3), "neutral")
        ]
        self.soundscape_profiles["explorer"] = SoundscapeProfile(
            "explorer", "Explorer", "Light, curious cognitive exploration", explorer_layers
        )
        
        # Scholar Profile - Deep, contemplative soundscape  
        scholar_layers = [
            AudioLayer("deep_resonance", "Deep Resonance", 55.0, (0.2, 0.7), "neutral"),
            AudioLayer("insight_bell", "Insight Bell", 1760.0, (0.1, 0.9), "positive"),
            AudioLayer("tension_drone", "Tension Drone", 185.0, (0.0, 0.8), "tension"),
            AudioLayer("resolution_chord", "Resolution Chord", 330.0, (0.3, 1.0), "resolution")
        ]
        self.soundscape_profiles["scholar"] = SoundscapeProfile(
            "scholar", "Scholar", "Deep contemplative analysis", scholar_layers
        )
        
        # Default to explorer profile
        self.active_profile = "explorer"
    
    def _subscribe_to_events(self):
        """Subscribe to cognitive events from the audio event bus."""
        self.audio_event_bus.subscribe(AudioEventType.ANCHOR_ACTIVATED, self._handle_anchor_event)
        self.audio_event_bus.subscribe(AudioEventType.ANCHOR_REINFORCED, self._handle_anchor_event)
        self.audio_event_bus.subscribe(AudioEventType.CONFLICT_DETECTED, self._handle_conflict_event)
        self.audio_event_bus.subscribe(AudioEventType.SUMMARY_GENERATED, self._handle_summary_event)
        self.audio_event_bus.subscribe(AudioEventType.CLUSTER_FORMED, self._handle_cluster_event)
        self.audio_event_bus.subscribe(AudioEventType.HEAT_THRESHOLD, self._handle_heat_event)
    
    def _handle_anchor_event(self, event: AudioEvent):
        """Handle anchor activation/reinforcement events."""
        anchor_id = event.data.get("anchor_id", "unknown")
        heat = event.data.get("heat", 0.0)
        
        # Update state
        self.current_state["anchor_heat_levels"][anchor_id] = heat
        
        # Generate audio mapping
        if event.event_type == AudioEventType.ANCHOR_ACTIVATED:
            self._trigger_audio_layer("anchor_pulse", intensity=min(heat * 2, 1.0))
        elif event.event_type == AudioEventType.ANCHOR_REINFORCED:
            self._trigger_audio_layer("discovery_chime", intensity=event.intensity)
    
    def _handle_conflict_event(self, event: AudioEvent):
        """Handle conflict detection events."""
        confidence = event.data.get("confidence", 0.0)
        conflict_type = event.data.get("conflict_type", "unknown")
        
        # Update tension state
        self.current_state["conflict_tension"] = max(
            self.current_state["conflict_tension"], confidence
        )
        
        # Trigger tension audio
        self._trigger_audio_layer("tension_drone", intensity=confidence)
    
    def _handle_summary_event(self, event: AudioEvent):
        """Handle summary generation events."""
        compression_ratio = event.data.get("compression_ratio", 1.0)
        summary_type = event.data.get("summary_type", "micro")
        
        # Update flow state
        flow_intensity = min(compression_ratio / 10.0, 1.0)
        self.current_state["summary_flow"] = flow_intensity
        
        # Trigger insight audio
        if summary_type == "macro":
            self._trigger_audio_layer("insight_bell", intensity=event.intensity * 1.5)
        else:
            self._trigger_audio_layer("insight_bell", intensity=event.intensity)
    
    def _handle_cluster_event(self, event: AudioEvent):
        """Handle cluster formation events."""
        cluster_size = event.data.get("cluster_size", 1)
        similarity = event.data.get("similarity", 0.0)
        
        # Trigger resolution for successful clustering
        if cluster_size > 1:
            self._trigger_audio_layer("resolution_chord", intensity=similarity)
    
    def _handle_heat_event(self, event: AudioEvent):
        """Handle heat threshold events."""
        threshold_type = event.data.get("threshold_type", "normal")
        heat_value = event.data.get("heat_value", 0.0)
        
        if threshold_type == "high":
            self._trigger_audio_layer("deep_resonance", intensity=min(heat_value, 1.0))
    
    def _trigger_audio_layer(self, layer_id: str, intensity: float = 0.5):
        """Trigger an audio layer with given intensity."""
        if not self.active_profile:
            return
            
        profile = self.soundscape_profiles.get(self.active_profile)
        if not profile:
            return
            
        layer = profile.get_layer(layer_id)
        if not layer:
            return
        
        # Clamp intensity to layer range
        min_intensity, max_intensity = layer.intensity_range
        clamped_intensity = max(min_intensity, min(intensity, max_intensity))
        
        # For now, just log the audio trigger - actual audio implementation would go here
        print(f"🔊 Audio Layer Triggered: {layer.name} (intensity: {clamped_intensity:.2f}, freq: {layer.base_frequency}Hz)")
    
    def set_active_profile(self, profile_id: str):
        """Set the active soundscape profile."""
        if profile_id in self.soundscape_profiles:
            self.active_profile = profile_id
            print(f"🎵 Soundscape Profile: {self.soundscape_profiles[profile_id].name}")
        else:
            print(f"Warning: Unknown soundscape profile '{profile_id}'")
    
    def get_current_state(self) -> Dict[str, Any]:
        """Get current cognitive audio state."""
        return {
            "active_profile": self.active_profile,
            "cognitive_state": self.current_state.copy(),
            "available_profiles": list(self.soundscape_profiles.keys())
        }
    
    def add_custom_profile(self, profile: SoundscapeProfile):
        """Add a custom soundscape profile."""
        self.soundscape_profiles[profile.profile_id] = profile
    
    def create_heatmap_data(self) -> Dict[str, Any]:
        """Create data for visual heatmap of current audio state."""
        anchor_data = []
        for anchor_id, heat in self.current_state["anchor_heat_levels"].items():
            anchor_data.append({
                "anchor_id": anchor_id,
                "heat": heat,
                "visual_intensity": min(heat * 0.8, 1.0)  # Scale for visual display
            })
        
        return {
            "timestamp": time.time(),
            "anchors": anchor_data,
            "conflict_tension": self.current_state["conflict_tension"],
            "summary_flow": self.current_state["summary_flow"],
            "cognitive_load": self.current_state["cognitive_load"],
            "active_profile": self.active_profile
        }


import time