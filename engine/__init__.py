"""
Engine Module - Optional Integration Layer

Provides optional integration points for the self-care system with
existing or future development workflows.

This module is designed to be imported only when needed and
does not break existing functionality if not used.
"""

__version__ = "1.0.0"
__author__ = "Tiny Walnut Games Living Dev Agent"

# Optional imports - only available if modules exist
try:
    from .telemetry import DevelopmentTelemetry
    __all__ = ['DevelopmentTelemetry']
except ImportError:
    __all__ = []
    
"""Exports for the Cognitive Geo-Thermal Lore Engine scaffold."""
from .giant_compressor import GiantCompressor
from .melt_layer import MeltLayer
from .evaporation import EvaporationEngine
from .castle_graph import CastleGraph
from .selector import Selector
from .governance import Governance
from .telemetry import CycleTelemetry
from .semantic_anchors import SemanticAnchorGraph, SemanticAnchor, AnchorProvenance
from .embeddings import EmbeddingProvider, EmbeddingProviderFactory

# v0.3 Milestone Components
from .summarization_ladder import SummarizationLadder, MicroSummary, MacroDistillation
from .conflict_detector import ConflictDetector, ConflictEvidence, ConflictType
from .retrieval_api import RetrievalAPI, RetrievalQuery, RetrievalResult, ContextAssembly, RetrievalMode

# v0.5 Milestone Components - Multimodal Expressive Layer
from .audio_event_bus import AudioEventBus, AudioEvent, AudioEventType
from .affect_audio_mapper import AffectAudioMapper, AudioLayer, SoundscapeProfile
from .visual_overlays import VisualOverlayGenerator, AnchorVisualization, HeatmapData
from .audio import TTSProvider, TTSProviderFactory, VoiceConfig, VoiceCharacteristic, TTSRequest, TTSResult
from .multimodal_engine import MultimodalEngine

# The Conservator - Auto-Repair Module
from .conservator import (
    TheConservator, ConservatorManifest, ModuleRegistration, RepairOperation,
    RepairTrigger, RepairAction, RepairStatus, create_conservator, register_warbler_core_modules
)

__all__ = [
    "GiantCompressor",
    "MeltLayer",
    "EvaporationEngine",
    "CastleGraph",
    "Selector",
    "Governance",
    "CycleTelemetry",
    "SemanticAnchorGraph",
    "SemanticAnchor", 
    "AnchorProvenance",
    "EmbeddingProvider",
    "EmbeddingProviderFactory",
    # v0.3 Components
    "SummarizationLadder", "MicroSummary", "MacroDistillation",
    "ConflictDetector", "ConflictEvidence", "ConflictType",
    "RetrievalAPI", "RetrievalQuery", "RetrievalResult", "ContextAssembly", "RetrievalMode",
    # v0.5 Components - Multimodal Expressive Layer
    "AudioEventBus", "AudioEvent", "AudioEventType",
    "AffectAudioMapper", "AudioLayer", "SoundscapeProfile", 
    "VisualOverlayGenerator", "AnchorVisualization", "HeatmapData",
    "TTSProvider", "TTSProviderFactory", "VoiceConfig", "VoiceCharacteristic", "TTSRequest", "TTSResult",
    "MultimodalEngine",
    # The Conservator - Auto-Repair Module
    "TheConservator", "ConservatorManifest", "ModuleRegistration", "RepairOperation",
    "RepairTrigger", "RepairAction", "RepairStatus", "create_conservator", "register_warbler_core_modules"
]
