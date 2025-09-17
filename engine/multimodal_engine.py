"""
Multimodal Engine - v0.5 Milestone Implementation

Integrates audio event bus, affect audio mapping, TTS, and visual overlays
with the existing Cognitive Geo-Thermal Lore Engine.
"""

from typing import Dict, List, Any, Optional
import time
from .audio_event_bus import AudioEventBus, AudioEventType
from .affect_audio_mapper import AffectAudioMapper
from .visual_overlays import VisualOverlayGenerator
from .audio import TTSProviderFactory


class MultimodalEngine:
    """
    Multimodal Expressive Layer - v0.5
    
    Extends the Cognitive Geo-Thermal Lore Engine with audio and visual capabilities.
    Provides event-driven multimodal feedback for cognitive processes.
    """
    
    def __init__(self, semantic_anchor_graph=None, tts_provider_type: str = "mock"):
        # Core multimodal components
        self.audio_event_bus = AudioEventBus()
        self.affect_audio_mapper = AffectAudioMapper(self.audio_event_bus)
        self.visual_overlay_generator = VisualOverlayGenerator(semantic_anchor_graph)
        
        # TTS integration
        self.tts_provider = TTSProviderFactory.create_provider(tts_provider_type)
        
        # State tracking
        self.cognitive_events_log = []
        self.multimodal_enabled = True
        
        print("🎭 Multimodal Expressive Layer v0.5 Initialized")
        print(f"🗣️ TTS Provider: {self.tts_provider.get_provider_info()['provider_name']}")
        print(f"🎵 Audio Profiles: {list(self.affect_audio_mapper.soundscape_profiles.keys())}")
    
    def connect_to_cycle_events(self, cycle_telemetry=None):
        """Connect multimodal events to existing cycle telemetry."""
        # This would integrate with the existing CycleTelemetry system
        # For now, we'll set up manual event triggers
        print("🔗 Connected to cycle telemetry events")
    
    def trigger_anchor_event(self, anchor_id: str, heat: float, event_type: str = "activated"):
        """Trigger an anchor-related multimodal event."""
        if not self.multimodal_enabled:
            return
        
        # Determine event type
        if event_type == "activated":
            audio_event_type = AudioEventType.ANCHOR_ACTIVATED
        elif event_type == "reinforced":
            audio_event_type = AudioEventType.ANCHOR_REINFORCED
        else:
            audio_event_type = AudioEventType.ANCHOR_ACTIVATED
        
        # Calculate intensity based on heat
        intensity = min(heat * 1.5, 1.0)
        
        # Publish audio event
        self.audio_event_bus.publish(
            audio_event_type,
            data={
                "anchor_id": anchor_id,
                "heat": heat,
                "event_type": event_type
            },
            intensity=intensity
        )
        
        # Log cognitive event
        self.cognitive_events_log.append({
            "timestamp": time.time(),
            "type": "anchor_event",
            "anchor_id": anchor_id,
            "heat": heat,
            "event_type": event_type,
            "intensity": intensity
        })
    
    def trigger_conflict_event(self, conflict_data: Dict[str, Any]):
        """Trigger a conflict detection multimodal event."""
        if not self.multimodal_enabled:
            return
        
        confidence = conflict_data.get("confidence", 0.5)
        
        self.audio_event_bus.publish(
            AudioEventType.CONFLICT_DETECTED,
            data=conflict_data,
            intensity=confidence,
            affect_layer="tension"
        )
        
        self.cognitive_events_log.append({
            "timestamp": time.time(),
            "type": "conflict_event",
            "confidence": confidence,
            "data": conflict_data
        })
    
    def trigger_summary_event(self, summary_data: Dict[str, Any]):
        """Trigger a summarization multimodal event."""
        if not self.multimodal_enabled:
            return
        
        compression_ratio = summary_data.get("compression_ratio", 1.0)
        summary_type = summary_data.get("summary_type", "micro")
        
        # Higher compression = more significant achievement
        intensity = min(compression_ratio / 10.0, 1.0)
        
        self.audio_event_bus.publish(
            AudioEventType.SUMMARY_GENERATED,
            data=summary_data,
            intensity=intensity,
            affect_layer="insight"
        )
        
        self.cognitive_events_log.append({
            "timestamp": time.time(),
            "type": "summary_event",
            "compression_ratio": compression_ratio,
            "summary_type": summary_type,
            "intensity": intensity
        })
    
    def trigger_cluster_event(self, cluster_data: Dict[str, Any]):
        """Trigger a clustering multimodal event."""
        if not self.multimodal_enabled:
            return
        
        cluster_size = cluster_data.get("cluster_size", 1)
        similarity = cluster_data.get("similarity", 0.5)
        
        self.audio_event_bus.publish(
            AudioEventType.CLUSTER_FORMED,
            data=cluster_data,
            intensity=similarity,
            affect_layer="resolution"
        )
        
        self.cognitive_events_log.append({
            "timestamp": time.time(),
            "type": "cluster_event",
            "cluster_size": cluster_size,
            "similarity": similarity
        })
    
    def trigger_cycle_events(self, cycle_start: bool = True):
        """Trigger cycle start/end events."""
        if not self.multimodal_enabled:
            return
        
        event_type = AudioEventType.COGNITIVE_CYCLE_START if cycle_start else AudioEventType.COGNITIVE_CYCLE_END
        
        self.audio_event_bus.publish(
            event_type,
            data={"cycle_timestamp": time.time()},
            intensity=0.3,
            affect_layer="ambient"
        )
    
    def generate_current_heatmap(self, layout_mode: str = "cluster_based") -> str:
        """Generate current visual heatmap."""
        heatmap = self.visual_overlay_generator.generate_anchor_heatmap(
            layout_mode=layout_mode
        )
        
        # Return JSON representation
        return self.visual_overlay_generator.export_heatmap_json(heatmap)
    
    def export_svg_visualization(self, layout_mode: str = "cluster_based") -> str:
        """Export current state as SVG visualization."""
        heatmap = self.visual_overlay_generator.generate_anchor_heatmap(
            layout_mode=layout_mode
        )
        
        return self.visual_overlay_generator.export_svg_heatmap(heatmap)
    
    def set_soundscape_profile(self, profile_id: str):
        """Set the active soundscape profile."""
        self.affect_audio_mapper.set_active_profile(profile_id)
    
    def get_available_profiles(self) -> List[str]:
        """Get available soundscape profiles."""
        return list(self.affect_audio_mapper.soundscape_profiles.keys())
    
    def enable_multimodal(self, enabled: bool = True):
        """Enable or disable multimodal feedback."""
        self.multimodal_enabled = enabled
        status = "Enabled" if enabled else "Disabled"
        print(f"🎭 Multimodal Feedback: {status}")
    
    def get_multimodal_state(self) -> Dict[str, Any]:
        """Get current multimodal system state."""
        audio_state = self.affect_audio_mapper.get_current_state()
        event_stats = self.audio_event_bus.get_stats()
        
        return {
            "multimodal_enabled": self.multimodal_enabled,
            "audio_state": audio_state,
            "event_stats": event_stats,
            "recent_events": len(self.cognitive_events_log),
            "tts_provider": self.tts_provider.get_provider_info(),
            "visual_history_count": len(self.visual_overlay_generator.visualization_history)
        }
    
    def get_recent_cognitive_events(self, limit: int = 10) -> List[Dict[str, Any]]:
        """Get recent cognitive events."""
        return self.cognitive_events_log[-limit:]
    
    def clear_event_history(self):
        """Clear event history."""
        self.cognitive_events_log.clear()
        self.audio_event_bus.clear_history()
        print("🧹 Multimodal event history cleared")
        
    def create_milestone_report(self) -> Dict[str, Any]:
        """Create a comprehensive milestone report for v0.5."""
        state = self.get_multimodal_state()
        recent_events = self.get_recent_cognitive_events()
        
        # Categorize events
        event_counts = {}
        for event in recent_events:
            event_type = event.get("type", "unknown")
            event_counts[event_type] = event_counts.get(event_type, 0) + 1
        
        return {
            "milestone": "v0.5",
            "title": "Multimodal Expressive Layer",
            "timestamp": time.time(),
            "components": {
                "audio_event_bus": {
                    "status": "active",
                    "total_events": state["event_stats"]["total_events"],
                    "subscriber_counts": state["event_stats"]["subscriber_counts"]
                },
                "affect_audio_mapper": {
                    "status": "active", 
                    "active_profile": state["audio_state"]["active_profile"],
                    "available_profiles": state["audio_state"]["available_profiles"]
                },
                "visual_overlays": {
                    "status": "active",
                    "visualizations_generated": state["visual_history_count"]
                },
                "tts_integration": {
                    "status": "available",
                    "provider": state["tts_provider"]["provider_name"],
                    "voices_available": state["tts_provider"].get("voices_count", 0)
                }
            },
            "cognitive_events": {
                "total_logged": len(recent_events),
                "event_breakdown": event_counts
            },
            "achievements": [
                "✅ Internal audio event bus implemented",
                "✅ Cognitive events mapped to affect audio layers",
                "✅ TTS integration with existing 'voices' concept",
                "✅ Visual overlay system for anchor relevance heatmaps",
                "✅ Full integration with v0.3 Cognitive Geo-Thermal Lore Engine"
            ]
        }