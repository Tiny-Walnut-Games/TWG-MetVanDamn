"""
Embedding Provider Factory - Dynamic Provider Creation
"""

from typing import Dict, Any, Optional
from .base_provider import EmbeddingProvider
from .local_provider import LocalEmbeddingProvider
from .openai_provider import OpenAIEmbeddingProvider


class EmbeddingProviderFactory:
    """Factory for creating embedding providers."""
    
    PROVIDERS = {
        "local": LocalEmbeddingProvider,
        "openai": OpenAIEmbeddingProvider,
    }
    
    @classmethod
    def create_provider(cls, provider_type: str, config: Optional[Dict[str, Any]] = None) -> EmbeddingProvider:
        """Create an embedding provider of the specified type."""
        if provider_type not in cls.PROVIDERS:
            available = list(cls.PROVIDERS.keys())
            raise ValueError(f"Unknown provider type '{provider_type}'. Available: {available}")
            
        provider_class = cls.PROVIDERS[provider_type]
        return provider_class(config)
        
    @classmethod 
    def get_default_provider(cls, config: Optional[Dict[str, Any]] = None) -> EmbeddingProvider:
        """Get the default embedding provider (local fallback)."""
        return cls.create_provider("local", config)
        
    @classmethod
    def list_available_providers(cls) -> list[str]:
        """List all available provider types."""
        return list(cls.PROVIDERS.keys())
        
    @classmethod
    def create_from_config(cls, full_config: Dict[str, Any]) -> EmbeddingProvider:
        """Create provider from configuration dict."""
        provider_type = full_config.get("provider", "local")
        provider_config = full_config.get("config", {})
        
        return cls.create_provider(provider_type, provider_config)