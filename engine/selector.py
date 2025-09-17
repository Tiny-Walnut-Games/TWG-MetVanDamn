from __future__ import annotations
from typing import List, Dict, Any, Optional
from .audio import TTSProvider, TTSProviderFactory, VoiceConfig, VoiceCharacteristic, TTSRequest

class Selector:
    """Selector: assembles prompt (multi-voice scaffold) with respond() stub."""
    def __init__(self, castle_graph, cloud_store, governance=None, tts_provider: Optional[TTSProvider] = None):
        self.castle_graph = castle_graph
        self.cloud_store = cloud_store
        self.governance = governance
        
        # v0.5 Multimodal: TTS integration
        self.tts_provider = tts_provider or TTSProviderFactory.create_best_available()
        self.voice_mapping = self._create_voice_mapping()
        self.tts_enabled = False  # Can be enabled for audio output

    def assemble_prompt(self, context: str = "", limit: int = 3) -> Dict[str, Any]:
        """Assemble multi-voice prompt from castle rooms and mist lines."""
        top_rooms = self.castle_graph.get_top_rooms(limit)
        active_mist = self.cloud_store.get_active_mist(limit)
        
        # TODO: Advanced prompt engineering with voice modulation
        voices = []
        for room in top_rooms:
            voice = {
                "voice_id": f"voice_{room['concept_id']}",
                "heat_level": room["heat"],
                "perspective": self._generate_perspective(room),
            }
            voices.append(voice)
        
        prompt_scaffold = {
            "context": context,
            "voices": voices,
            "mist_context": [m.get("proto_thought", "") for m in active_mist],
            "generation_mode": self.cloud_store.generation_mode,
            "humidity_index": self.cloud_store.humidity_index,
        }
        
        return prompt_scaffold

    def respond(self, prompt_scaffold: Dict[str, Any]) -> Dict[str, Any]:
        """Generate response using prompt scaffold (placeholder)."""
        # TODO: Replace with real LLM integration
        voices = prompt_scaffold.get("voices", [])
        context = prompt_scaffold.get("context", "")
        
        # Mock response generation
        response = {
            "response_text": f"[Generated from {len(voices)} voices] {context[:50]}...",
            "confidence": 0.7,
            "voice_contributions": [v["voice_id"] for v in voices[:2]],
            "generation_time_ms": 150,  # placeholder
        }
        
        # Apply governance if available
        if self.governance:
            response = self.governance.filter_response(response)
        
        return response

    def enable_tts(self, enabled: bool = True):
        """Enable or disable TTS output."""
        self.tts_enabled = enabled and self.tts_provider.is_available()
        if self.tts_enabled:
            print("🗣️ TTS Output: Enabled")
        else:
            print("🔇 TTS Output: Disabled")

    def _create_voice_mapping(self) -> Dict[str, VoiceConfig]:
        """Create mapping from cognitive concepts to TTS voices."""
        available_voices = self.tts_provider.get_available_voices()
        
        # Create smart mapping based on voice characteristics
        mapping = {}
        for voice in available_voices:
            if voice.characteristic == VoiceCharacteristic.NARRATOR:
                mapping["default"] = voice
                mapping["concept_implementing"] = voice
            elif voice.characteristic == VoiceCharacteristic.EXPLORER:
                mapping["concept_advanced"] = voice
                mapping["concept_debugging"] = voice
            elif voice.characteristic == VoiceCharacteristic.SCHOLAR:
                mapping["concept_memory"] = voice
                mapping["concept_semantic"] = voice
            elif voice.characteristic == VoiceCharacteristic.ADVISOR:
                mapping["concept_wisdom"] = voice
                mapping["concept_governance"] = voice
            elif voice.characteristic == VoiceCharacteristic.ALERT:
                mapping["concept_conflict"] = voice
                mapping["concept_urgent"] = voice
        
        # Ensure default voice exists
        if "default" not in mapping and available_voices:
            mapping["default"] = available_voices[0]
        
        return mapping

    def _get_voice_for_concept(self, concept_id: str) -> VoiceConfig:
        """Get appropriate TTS voice for a concept."""
        # Try exact match first
        if concept_id in self.voice_mapping:
            return self.voice_mapping[concept_id]
        
        # Try partial matches for concept patterns
        for mapped_concept, voice in self.voice_mapping.items():
            if mapped_concept in concept_id or concept_id in mapped_concept:
                return voice
        
        # Fall back to default
        return self.voice_mapping.get("default", self.tts_provider.get_available_voices()[0])

    def synthesize_response(self, response_text: str, primary_voice_id: str = None) -> Optional[str]:
        """Synthesize response text to speech using appropriate voice."""
        if not self.tts_enabled or not response_text.strip():
            return None
        
        # Determine voice
        if primary_voice_id:
            voice_config = self._get_voice_for_concept(primary_voice_id)
        else:
            voice_config = self.voice_mapping.get("default")
        
        if not voice_config:
            return None
        
        # Create TTS request
        tts_request = TTSRequest(
            text=response_text,
            voice_config=voice_config,
            context={"source": "cognitive_response", "voice_id": primary_voice_id}
        )
        
        # Synthesize
        result = self.tts_provider.synthesize(tts_request)
        
        if result.success:
            return f"TTS synthesized: {result.duration_ms}ms using {voice_config.name}"
        else:
            print(f"TTS failed: {result.error_message}")
            return None

    def _generate_perspective(self, room: Dict[str, Any]) -> str:
        """Generate voice perspective from castle room data."""
        # TODO: Rich perspective generation based on room history
        concept = room["concept_id"].replace("concept_", "")
        heat = room["heat"]
        
        if heat > 0.5:
            return f"Passionate advocate for {concept}"
        elif heat > 0.2:
            return f"Knowledgeable about {concept}"
        else:
            return f"Curious observer of {concept}"