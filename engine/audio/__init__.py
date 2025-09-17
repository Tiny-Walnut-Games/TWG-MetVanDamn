"""
Audio Package - Multimodal Expressive Layer v0.5
"""

from .tts_provider import (
    TTSProvider, MockTTSProvider, SystemTTSProvider, TTSProviderFactory,
    VoiceConfig, VoiceCharacteristic, TTSRequest, TTSResult
)

__all__ = [
    "TTSProvider", "MockTTSProvider", "SystemTTSProvider", "TTSProviderFactory",
    "VoiceConfig", "VoiceCharacteristic", "TTSRequest", "TTSResult"
]