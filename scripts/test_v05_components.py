#!/usr/bin/env python3
"""
v0.5 Component Validation Tests

Tests the multimodal expressive layer components to ensure they integrate
correctly with the existing Cognitive Geo-Thermal Lore Engine.
"""

import sys
import os
from pathlib import Path

# Add parent directory to path for engine imports
sys.path.insert(0, str(Path(__file__).parent.parent))

from engine import (
    # v0.5 Multimodal Components
    AudioEventBus, AudioEventType, AffectAudioMapper, 
    VisualOverlayGenerator, MultimodalEngine,
    TTSProviderFactory, VoiceCharacteristic
)


def test_audio_event_bus():
    """Test audio event bus functionality."""
    print("🧪 Testing Audio Event Bus...")
    
    bus = AudioEventBus()
    events_received = []
    
    def test_callback(event):
        events_received.append(event)
    
    # Subscribe to events
    bus.subscribe(AudioEventType.ANCHOR_ACTIVATED, test_callback)
    
    # Publish test event
    bus.publish(AudioEventType.ANCHOR_ACTIVATED, 
                data={"anchor_id": "test_anchor", "heat": 0.7},
                intensity=0.8)
    
    # Check results
    assert len(events_received) == 1
    assert events_received[0].event_type == AudioEventType.ANCHOR_ACTIVATED
    assert events_received[0].data["anchor_id"] == "test_anchor"
    assert events_received[0].intensity == 0.8
    
    stats = bus.get_stats()
    assert stats["total_events"] == 1
    
    print("   ✅ Audio event bus working correctly")


def test_affect_audio_mapper():
    """Test affect audio mapping.""" 
    print("🧪 Testing Affect Audio Mapper...")
    
    bus = AudioEventBus()
    mapper = AffectAudioMapper(bus)
    
    # Test profile switching
    assert mapper.active_profile == "explorer"
    mapper.set_active_profile("scholar")
    assert mapper.active_profile == "scholar"
    
    # Test event handling
    bus.publish(AudioEventType.ANCHOR_ACTIVATED,
                data={"anchor_id": "test", "heat": 0.5})
    
    bus.publish(AudioEventType.CONFLICT_DETECTED,
                data={"confidence": 0.8, "conflict_type": "semantic"})
    
    # Check state
    state = mapper.get_current_state()
    assert "test" in state["cognitive_state"]["anchor_heat_levels"]
    assert state["cognitive_state"]["conflict_tension"] >= 0.8
    
    print("   ✅ Affect audio mapper working correctly")


def test_visual_overlays():
    """Test visual overlay generation."""
    print("🧪 Testing Visual Overlays...")
    
    generator = VisualOverlayGenerator()
    
    # Create mock anchors
    class MockAnchor:
        def __init__(self, anchor_id, concept_text, heat):
            self.anchor_id = anchor_id
            self.concept_text = concept_text
            self.heat = heat
            self.cluster_id = None
            
        def calculate_age_days(self):
            return 1.0
    
    mock_anchors = [
        MockAnchor("anchor1", "concept testing", 0.8),
        MockAnchor("anchor2", "visual overlays", 0.6),
        MockAnchor("anchor3", "multimodal system", 0.4)
    ]
    
    # Generate heatmap
    heatmap = generator.generate_anchor_heatmap(mock_anchors, "grid")
    
    assert len(heatmap.anchors) == 3
    assert heatmap.global_max_heat == 0.8
    assert heatmap.dimensions == (800, 600)
    
    # Test JSON export
    json_data = generator.export_heatmap_json(heatmap)
    assert '"anchor_id": "anchor1"' in json_data
    assert '"heat": 0.8' in json_data
    
    # Test SVG export
    svg_data = generator.export_svg_heatmap(heatmap)
    assert svg_data.startswith('<svg')
    assert 'anchor1' in svg_data or 'concept testing' in svg_data
    
    print("   ✅ Visual overlays working correctly")


def test_tts_provider():
    """Test TTS provider system."""
    print("🧪 Testing TTS Provider...")
    
    # Test factory
    providers = TTSProviderFactory.get_available_providers()
    assert "mock" in providers
    
    provider = TTSProviderFactory.create_provider("mock")
    assert provider.is_available()
    
    # Test voices
    voices = provider.get_available_voices()
    assert len(voices) > 0
    
    # Test synthesis
    from engine.audio import TTSRequest
    request = TTSRequest("Hello multimodal world", voices[0])
    result = provider.synthesize(request)
    
    assert result.success
    assert result.duration_ms > 0
    assert result.provider_info["provider"] == "MockTTSProvider"
    
    print("   ✅ TTS provider working correctly")


def test_multimodal_engine():
    """Test complete multimodal engine."""
    print("🧪 Testing Multimodal Engine...")
    
    engine = MultimodalEngine()
    
    # Test state
    state = engine.get_multimodal_state()
    assert state["multimodal_enabled"] == True
    assert "explorer" in state["audio_state"]["available_profiles"]
    
    # Test events
    engine.trigger_anchor_event("test_anchor", 0.7, "activated")
    engine.trigger_conflict_event({"confidence": 0.8})
    engine.trigger_summary_event({"compression_ratio": 5.0, "summary_type": "micro"})
    
    # Check events were logged
    events = engine.get_recent_cognitive_events()
    assert len(events) >= 3
    
    event_types = [e["type"] for e in events]
    assert "anchor_event" in event_types
    assert "conflict_event" in event_types
    assert "summary_event" in event_types
    
    # Test milestone report
    report = engine.create_milestone_report()
    assert report["milestone"] == "v0.5"
    assert len(report["achievements"]) == 5
    
    print("   ✅ Multimodal engine working correctly")


def main():
    print("🚀 v0.5 Component Validation Tests")
    print("=" * 50)
    
    try:
        test_audio_event_bus()
        test_affect_audio_mapper()
        test_visual_overlays()
        test_tts_provider()
        test_multimodal_engine()
        
        print("\n✅ All v0.5 components validated successfully!")
        print("🎭 Multimodal Expressive Layer is ready for legendary adventures!")
        
    except Exception as e:
        print(f"\n❌ Test failed: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)


if __name__ == "__main__":
    main()