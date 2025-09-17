#!/usr/bin/env python3
"""
Performance Configuration Profiles - v0.6 Optimization System

Provides dev/perf/experiment configuration profiles for optimizing
different aspects of the Cognitive Geo-Thermal Lore Engine.

🧙‍♂️ "Different battles require different armor - choose your 
    configuration wisely for the quest ahead." - Bootstrap Sentinel
"""

from typing import Dict, Any, Optional
import json
from dataclasses import dataclass, asdict
from pathlib import Path
from enum import Enum


class ProfileType(Enum):
    """Available performance profile types."""
    DEVELOPMENT = "dev"
    BALANCED = "balanced"
    PERFORMANCE = "perf"
    EXPERIMENT = "experiment"
    CUSTOM = "custom"


@dataclass
class MemoryPoolConfig:
    """Memory pool configuration settings."""
    enabled: bool = True
    initial_size: int = 50
    max_size: int = 500
    cleanup_interval_sec: float = 300.0
    memory_pressure_threshold: int = 1000


@dataclass
class DiffEngineConfig:
    """Incremental diff engine configuration."""
    enabled: bool = True
    max_diff_depth: int = 10
    ignore_timestamp_fields: bool = True
    compression_enabled: bool = True
    cache_max_age_sec: float = 3600.0


@dataclass
class StreamingPipelineConfig:
    """Streaming ingestion pipeline configuration."""
    enabled: bool = True
    initial_batch_size: int = 10
    max_batch_size: int = 100
    max_queue_size: int = 1000
    backpressure_threshold: float = 0.8
    adaptive_sizing: bool = True
    processing_timeout_sec: float = 30.0


@dataclass
class AnchorSystemConfig:
    """Semantic anchor system configuration."""
    memory_pooling_enabled: bool = True
    max_age_days: int = 30
    consolidation_threshold: float = 0.8
    eviction_heat_threshold: float = 0.1
    clustering_enabled: bool = True
    lifecycle_cleanup_interval: float = 600.0


@dataclass
class RetrievalConfig:
    """Retrieval system configuration."""
    batch_evaluation_enabled: bool = True
    max_batch_size: int = 50
    context_cache_enabled: bool = True
    prefetch_enabled: bool = False
    parallel_retrieval: bool = False


@dataclass
class TelemetryConfig:
    """Performance telemetry configuration."""
    enabled: bool = True
    detailed_metrics: bool = False
    real_time_monitoring: bool = False
    metrics_retention_hours: int = 24
    export_enabled: bool = False


@dataclass
class PerformanceProfile:
    """Complete performance configuration profile."""
    profile_type: str
    profile_name: str
    description: str
    
    # Component configurations
    memory_pool: MemoryPoolConfig
    diff_engine: DiffEngineConfig
    streaming_pipeline: StreamingPipelineConfig
    anchor_system: AnchorSystemConfig
    retrieval: RetrievalConfig
    telemetry: TelemetryConfig
    
    # Global settings
    debug_mode: bool = False
    log_level: str = "INFO"
    performance_monitoring: bool = True
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert profile to dictionary."""
        return asdict(self)
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'PerformanceProfile':
        """Create profile from dictionary."""
        # Extract component configs
        memory_pool = MemoryPoolConfig(**data.get('memory_pool', {}))
        diff_engine = DiffEngineConfig(**data.get('diff_engine', {}))
        streaming_pipeline = StreamingPipelineConfig(**data.get('streaming_pipeline', {}))
        anchor_system = AnchorSystemConfig(**data.get('anchor_system', {}))
        retrieval = RetrievalConfig(**data.get('retrieval', {}))
        telemetry = TelemetryConfig(**data.get('telemetry', {}))
        
        return cls(
            profile_type=data.get('profile_type', 'custom'),
            profile_name=data.get('profile_name', 'Custom Profile'),
            description=data.get('description', 'Custom configuration'),
            memory_pool=memory_pool,
            diff_engine=diff_engine,
            streaming_pipeline=streaming_pipeline,
            anchor_system=anchor_system,
            retrieval=retrieval,
            telemetry=telemetry,
            debug_mode=data.get('debug_mode', False),
            log_level=data.get('log_level', 'INFO'),
            performance_monitoring=data.get('performance_monitoring', True)
        )


class PerformanceProfileManager:
    """
    Manager for performance configuration profiles.
    
    Provides predefined profiles and custom profile management.
    """
    
    def __init__(self, config_dir: str = "configs/performance"):
        self.config_dir = Path(config_dir)
        self.config_dir.mkdir(parents=True, exist_ok=True)
        
        # Current active profile
        self.active_profile: Optional[PerformanceProfile] = None
        
        # Profile registry
        self.profiles: Dict[str, PerformanceProfile] = {}
        
        # Load predefined profiles
        self._initialize_predefined_profiles()
        
        # Load custom profiles from disk
        self._load_custom_profiles()
        
    def _initialize_predefined_profiles(self):
        """Initialize predefined performance profiles."""
        
        # Development Profile - Fast iteration, detailed debugging
        dev_profile = PerformanceProfile(
            profile_type=ProfileType.DEVELOPMENT.value,
            profile_name="Development",
            description="Optimized for development with detailed debugging and fast iteration",
            memory_pool=MemoryPoolConfig(
                enabled=True,
                initial_size=20,
                max_size=100,
                cleanup_interval_sec=60.0
            ),
            diff_engine=DiffEngineConfig(
                enabled=True,
                max_diff_depth=5,
                compression_enabled=False,  # Disable for debugging
                cache_max_age_sec=300.0
            ),
            streaming_pipeline=StreamingPipelineConfig(
                enabled=True,
                initial_batch_size=5,
                max_batch_size=20,
                max_queue_size=100,
                backpressure_threshold=0.7,
                adaptive_sizing=True
            ),
            anchor_system=AnchorSystemConfig(
                memory_pooling_enabled=True,
                max_age_days=7,  # Shorter for dev
                consolidation_threshold=0.9,
                lifecycle_cleanup_interval=120.0
            ),
            retrieval=RetrievalConfig(
                batch_evaluation_enabled=False,  # Disable for simplicity
                max_batch_size=10,
                context_cache_enabled=True,
                prefetch_enabled=False
            ),
            telemetry=TelemetryConfig(
                enabled=True,
                detailed_metrics=True,
                real_time_monitoring=True,
                metrics_retention_hours=4
            ),
            debug_mode=True,
            log_level="DEBUG",
            performance_monitoring=True
        )
        
        # Performance Profile - Maximum throughput
        perf_profile = PerformanceProfile(
            profile_type=ProfileType.PERFORMANCE.value,
            profile_name="Performance",
            description="Optimized for maximum throughput and minimal latency",
            memory_pool=MemoryPoolConfig(
                enabled=True,
                initial_size=100,
                max_size=1000,
                cleanup_interval_sec=600.0,
                memory_pressure_threshold=2000
            ),
            diff_engine=DiffEngineConfig(
                enabled=True,
                max_diff_depth=15,
                compression_enabled=True,
                cache_max_age_sec=7200.0
            ),
            streaming_pipeline=StreamingPipelineConfig(
                enabled=True,
                initial_batch_size=50,
                max_batch_size=200,
                max_queue_size=2000,
                backpressure_threshold=0.95,
                adaptive_sizing=True,
                processing_timeout_sec=60.0
            ),
            anchor_system=AnchorSystemConfig(
                memory_pooling_enabled=True,
                max_age_days=60,
                consolidation_threshold=0.7,
                eviction_heat_threshold=0.05,
                lifecycle_cleanup_interval=900.0
            ),
            retrieval=RetrievalConfig(
                batch_evaluation_enabled=True,
                max_batch_size=100,
                context_cache_enabled=True,
                prefetch_enabled=True,
                parallel_retrieval=True
            ),
            telemetry=TelemetryConfig(
                enabled=True,
                detailed_metrics=False,  # Reduce overhead
                real_time_monitoring=False,
                metrics_retention_hours=48
            ),
            debug_mode=False,
            log_level="WARN",
            performance_monitoring=True
        )
        
        # Experiment Profile - Research and testing
        experiment_profile = PerformanceProfile(
            profile_type=ProfileType.EXPERIMENT.value,
            profile_name="Experiment",
            description="Configured for research experiments and A/B testing",
            memory_pool=MemoryPoolConfig(
                enabled=True,
                initial_size=200,
                max_size=2000,
                cleanup_interval_sec=1800.0
            ),
            diff_engine=DiffEngineConfig(
                enabled=True,
                max_diff_depth=20,
                compression_enabled=True,
                cache_max_age_sec=14400.0
            ),
            streaming_pipeline=StreamingPipelineConfig(
                enabled=True,
                initial_batch_size=100,
                max_batch_size=500,
                max_queue_size=5000,
                backpressure_threshold=0.98,
                adaptive_sizing=True,
                processing_timeout_sec=120.0
            ),
            anchor_system=AnchorSystemConfig(
                memory_pooling_enabled=True,
                max_age_days=90,
                consolidation_threshold=0.6,
                eviction_heat_threshold=0.02,
                clustering_enabled=True
            ),
            retrieval=RetrievalConfig(
                batch_evaluation_enabled=True,
                max_batch_size=200,
                context_cache_enabled=True,
                prefetch_enabled=True,
                parallel_retrieval=True
            ),
            telemetry=TelemetryConfig(
                enabled=True,
                detailed_metrics=True,
                real_time_monitoring=True,
                metrics_retention_hours=168,  # 1 week
                export_enabled=True
            ),
            debug_mode=False,
            log_level="INFO",
            performance_monitoring=True
        )
        
        # Balanced Profile - Good defaults
        balanced_profile = PerformanceProfile(
            profile_type=ProfileType.BALANCED.value,
            profile_name="Balanced",
            description="Balanced configuration for general use",
            memory_pool=MemoryPoolConfig(
                enabled=True,
                initial_size=50,
                max_size=500,
                cleanup_interval_sec=300.0
            ),
            diff_engine=DiffEngineConfig(
                enabled=True,
                max_diff_depth=10,
                compression_enabled=True,
                cache_max_age_sec=3600.0
            ),
            streaming_pipeline=StreamingPipelineConfig(
                enabled=True,
                initial_batch_size=10,
                max_batch_size=50,
                max_queue_size=500,
                backpressure_threshold=0.8,
                adaptive_sizing=True
            ),
            anchor_system=AnchorSystemConfig(
                memory_pooling_enabled=True,
                max_age_days=30,
                consolidation_threshold=0.8,
                eviction_heat_threshold=0.1
            ),
            retrieval=RetrievalConfig(
                batch_evaluation_enabled=True,
                max_batch_size=50,
                context_cache_enabled=True,
                prefetch_enabled=False
            ),
            telemetry=TelemetryConfig(
                enabled=True,
                detailed_metrics=False,
                real_time_monitoring=False,
                metrics_retention_hours=24
            ),
            debug_mode=False,
            log_level="INFO",
            performance_monitoring=True
        )
        
        # Register predefined profiles
        self.profiles[ProfileType.DEVELOPMENT.value] = dev_profile
        self.profiles[ProfileType.PERFORMANCE.value] = perf_profile
        self.profiles[ProfileType.EXPERIMENT.value] = experiment_profile
        self.profiles[ProfileType.BALANCED.value] = balanced_profile
        
    def _load_custom_profiles(self):
        """Load custom profiles from configuration files."""
        for config_file in self.config_dir.glob("*.json"):
            try:
                with open(config_file, 'r') as f:
                    profile_data = json.load(f)
                    profile = PerformanceProfile.from_dict(profile_data)
                    self.profiles[profile.profile_type] = profile
            except Exception as e:
                print(f"Failed to load profile {config_file}: {e}")
                
    def get_profile(self, profile_type: str) -> Optional[PerformanceProfile]:
        """Get profile by type."""
        return self.profiles.get(profile_type)
        
    def set_active_profile(self, profile_type: str) -> bool:
        """Set the active performance profile."""
        profile = self.get_profile(profile_type)
        if profile:
            self.active_profile = profile
            return True
        return False
        
    def get_active_profile(self) -> Optional[PerformanceProfile]:
        """Get the currently active profile."""
        return self.active_profile
        
    def create_custom_profile(self, profile_type: str, profile_name: str, 
                            base_profile: str = "balanced", 
                            overrides: Dict[str, Any] = None) -> PerformanceProfile:
        """
        Create a custom profile based on an existing profile.
        
        Args:
            profile_type: Unique identifier for the profile
            profile_name: Human-readable name
            base_profile: Profile to use as base
            overrides: Dictionary of overrides to apply
        """
        base = self.get_profile(base_profile)
        if not base:
            base = self.get_profile(ProfileType.BALANCED.value)
            
        # Convert to dict and apply overrides
        profile_data = base.to_dict()
        profile_data['profile_type'] = profile_type
        profile_data['profile_name'] = profile_name
        
        if overrides:
            self._apply_overrides(profile_data, overrides)
            
        # Create new profile
        custom_profile = PerformanceProfile.from_dict(profile_data)
        self.profiles[profile_type] = custom_profile
        
        return custom_profile
        
    def _apply_overrides(self, profile_data: Dict[str, Any], overrides: Dict[str, Any]):
        """Apply overrides to profile data recursively."""
        for key, value in overrides.items():
            if '.' in key:
                # Nested key like 'memory_pool.max_size'
                parts = key.split('.')
                current = profile_data
                for part in parts[:-1]:
                    if part not in current:
                        current[part] = {}
                    current = current[part]
                current[parts[-1]] = value
            else:
                profile_data[key] = value
                
    def save_custom_profile(self, profile_type: str) -> bool:
        """Save a custom profile to disk."""
        profile = self.get_profile(profile_type)
        if not profile:
            return False
            
        try:
            config_file = self.config_dir / f"{profile_type}.json"
            with open(config_file, 'w') as f:
                json.dump(profile.to_dict(), f, indent=2)
            return True
        except Exception as e:
            print(f"Failed to save profile {profile_type}: {e}")
            return False
            
    def list_profiles(self) -> Dict[str, str]:
        """List all available profiles."""
        return {
            profile_type: profile.profile_name 
            for profile_type, profile in self.profiles.items()
        }
        
    def get_performance_comparison(self) -> Dict[str, Dict[str, Any]]:
        """Get performance comparison across profiles."""
        comparison = {}
        
        for profile_type, profile in self.profiles.items():
            comparison[profile_type] = {
                "name": profile.profile_name,
                "memory_pool_max": profile.memory_pool.max_size,
                "batch_size_max": profile.streaming_pipeline.max_batch_size,
                "queue_size_max": profile.streaming_pipeline.max_queue_size,
                "anchor_max_age": profile.anchor_system.max_age_days,
                "retrieval_batch_max": profile.retrieval.max_batch_size,
                "debug_mode": profile.debug_mode,
                "performance_focus": self._assess_performance_focus(profile)
            }
            
        return comparison
        
    def _assess_performance_focus(self, profile: PerformanceProfile) -> str:
        """Assess the primary performance focus of a profile."""
        if profile.debug_mode and profile.telemetry.detailed_metrics:
            return "debugging"
        elif profile.streaming_pipeline.max_batch_size > 100:
            return "throughput"
        elif profile.memory_pool.max_size > 1000:
            return "memory_efficiency" 
        elif profile.telemetry.export_enabled:
            return "research"
        else:
            return "general"


# Global profile manager instance
_global_profile_manager: Optional[PerformanceProfileManager] = None


def get_global_profile_manager() -> PerformanceProfileManager:
    """Get or create global profile manager."""
    global _global_profile_manager
    if _global_profile_manager is None:
        _global_profile_manager = PerformanceProfileManager()
    return _global_profile_manager


def apply_performance_profile(profile_type: str) -> bool:
    """Apply a performance profile globally."""
    manager = get_global_profile_manager()
    return manager.set_active_profile(profile_type)


def get_current_config() -> Optional[PerformanceProfile]:
    """Get the current active performance configuration."""
    manager = get_global_profile_manager()
    return manager.get_active_profile()