"""
Audio Provider System - Multimodal Expressive Layer v0.5

Abstract interface and implementations for TTS (Text-to-Speech) providers.
Integrates with the existing "voices" concept in the Selector.
"""

from abc import ABC, abstractmethod
from typing import Dict, List, Any, Optional
from dataclasses import dataclass
from enum import Enum


class VoiceCharacteristic(Enum):
    """Voice characteristics for cognitive contexts."""
    NARRATOR = "narrator"        # Clear, neutral narration
    EXPLORER = "explorer"        # Curious, enthusiastic
    SCHOLAR = "scholar"          # Deep, contemplative
    ADVISOR = "advisor"          # Wise, measured
    ALERT = "alert"             # Urgent, attention-grabbing


@dataclass
class VoiceConfig:
    """Configuration for a TTS voice."""
    voice_id: str
    name: str
    characteristic: VoiceCharacteristic
    language: str = "en"
    speed: float = 1.0          # 0.5 to 2.0
    pitch: float = 1.0          # 0.5 to 2.0
    volume: float = 0.8         # 0.0 to 1.0
    parameters: Dict[str, Any] = None
    
    def __post_init__(self):
        if self.parameters is None:
            self.parameters = {}


@dataclass
class TTSRequest:
    """Request for text-to-speech synthesis."""
    text: str
    voice_config: VoiceConfig
    priority: int = 0           # Higher values = higher priority
    context: Dict[str, Any] = None
    
    def __post_init__(self):
        if self.context is None:
            self.context = {}


@dataclass
class TTSResult:
    """Result from TTS synthesis."""
    success: bool
    audio_data: Optional[bytes] = None
    audio_format: str = "wav"
    duration_ms: int = 0
    error_message: str = ""
    provider_info: Dict[str, Any] = None
    
    def __post_init__(self):
        if self.provider_info is None:
            self.provider_info = {}


class TTSProvider(ABC):
    """Abstract base class for TTS providers."""
    
    @abstractmethod
    def get_available_voices(self) -> List[VoiceConfig]:
        """Get list of available voices."""
        pass
    
    @abstractmethod
    def synthesize(self, request: TTSRequest) -> TTSResult:
        """Synthesize text to speech."""
        pass
    
    @abstractmethod
    def is_available(self) -> bool:
        """Check if provider is available."""
        pass
    
    @abstractmethod
    def get_provider_info(self) -> Dict[str, Any]:
        """Get provider information."""
        pass


class MockTTSProvider(TTSProvider):
    """Mock TTS provider for testing and development."""
    
    def __init__(self):
        self.voices = [
            VoiceConfig("mock_narrator", "Mock Narrator", VoiceCharacteristic.NARRATOR),
            VoiceConfig("mock_explorer", "Mock Explorer", VoiceCharacteristic.EXPLORER, speed=1.2),
            VoiceConfig("mock_scholar", "Mock Scholar", VoiceCharacteristic.SCHOLAR, speed=0.9, pitch=0.8),
            VoiceConfig("mock_advisor", "Mock Advisor", VoiceCharacteristic.ADVISOR, speed=0.8),
            VoiceConfig("mock_alert", "Mock Alert", VoiceCharacteristic.ALERT, speed=1.4, pitch=1.2)
        ]
    
    def get_available_voices(self) -> List[VoiceConfig]:
        """Get list of available mock voices."""
        return self.voices.copy()
    
    def synthesize(self, request: TTSRequest) -> TTSResult:
        """Mock synthesize - just logs the request."""
        text = request.text[:50] + "..." if len(request.text) > 50 else request.text
        voice_name = request.voice_config.name
        
        # Simulate processing time based on text length
        estimated_duration = len(request.text) * 50  # ~50ms per character
        
        print(f"🗣️ TTS [{voice_name}]: {text}")
        
        return TTSResult(
            success=True,
            audio_data=None,  # Mock - no actual audio
            duration_ms=estimated_duration,
            provider_info={
                "provider": "MockTTSProvider",
                "voice_used": voice_name,
                "text_length": len(request.text)
            }
        )
    
    def is_available(self) -> bool:
        """Mock provider is always available."""
        return True
    
    def get_provider_info(self) -> Dict[str, Any]:
        """Get mock provider information."""
        return {
            "provider_name": "Mock TTS Provider",
            "version": "1.0.0",
            "capabilities": ["all_languages", "voice_characteristics"],
            "latency": "instant",
            "voices_count": len(self.voices)
        }


class SystemTTSProvider(TTSProvider):
    """System TTS provider using platform-specific APIs."""
    
    def __init__(self):
        self.voices = []
        self._initialize_system_voices()
    
    def _initialize_system_voices(self):
        """Initialize system voices - platform-specific implementation would go here."""
        # This would probe system TTS capabilities
        # For now, create a basic voice set
        self.voices = [
            VoiceConfig("system_default", "System Default", VoiceCharacteristic.NARRATOR),
        ]
    
    def get_available_voices(self) -> List[VoiceConfig]:
        """Get system voices."""
        return self.voices.copy()
    
    def synthesize(self, request: TTSRequest) -> TTSResult:
        """Synthesize using system TTS - would use platform APIs."""
        # Platform-specific TTS implementation would go here
        print(f"🔊 System TTS: {request.text[:50]}...")
        
        return TTSResult(
            success=False,
            error_message="System TTS not implemented - use MockTTSProvider for testing",
            provider_info={"provider": "SystemTTSProvider", "status": "not_implemented"}
        )
    
    def is_available(self) -> bool:
        """Check if system TTS is available."""
        # Would check platform TTS availability
        return False  # Not implemented yet
    
    def get_provider_info(self) -> Dict[str, Any]:
        """Get system provider info."""
        return {
            "provider_name": "System TTS Provider", 
            "version": "1.0.0",
            "status": "not_implemented",
            "note": "Would use platform-specific TTS APIs"
        }


class TTSProviderFactory:
    """Factory for creating TTS providers."""
    
    @staticmethod
    def create_provider(provider_type: str = "mock") -> TTSProvider:
        """Create a TTS provider."""
        if provider_type == "mock":
            return MockTTSProvider()
        elif provider_type == "system":
            return SystemTTSProvider()
        else:
            raise ValueError(f"Unknown TTS provider type: {provider_type}")
    
    @staticmethod
    def get_available_providers() -> List[str]:
        """Get list of available provider types."""
        return ["mock", "system"]
    
    @staticmethod
    def create_best_available() -> TTSProvider:
        """Create the best available TTS provider."""
        # Try system first, fall back to mock
        system_provider = SystemTTSProvider()
        if system_provider.is_available():
            return system_provider
        return MockTTSProvider()