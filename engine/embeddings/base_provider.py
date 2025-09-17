"""
Base Embedding Provider - Abstract Interface for Semantic Grounding
"""

from abc import ABC, abstractmethod
from typing import List, Dict, Any, Optional
import time


class EmbeddingProvider(ABC):
    """Abstract base class for embedding providers."""
    
    def __init__(self, config: Optional[Dict[str, Any]] = None):
        self.config = config or {}
        self.provider_id = self.__class__.__name__
        self.created_at = time.time()
        
    @abstractmethod
    def embed_text(self, text: str) -> List[float]:
        """Generate embedding vector for a single text."""
        pass
        
    @abstractmethod
    def embed_batch(self, texts: List[str]) -> List[List[float]]:
        """Generate embedding vectors for multiple texts."""
        pass
        
    @abstractmethod
    def get_dimension(self) -> int:
        """Get the dimension of embedding vectors."""
        pass
        
    def calculate_similarity(self, embedding1: List[float], embedding2: List[float]) -> float:
        """Calculate cosine similarity between two embeddings."""
        import math
        
        # Dot product
        dot_product = sum(a * b for a, b in zip(embedding1, embedding2))
        
        # Magnitudes
        magnitude1 = math.sqrt(sum(a * a for a in embedding1))
        magnitude2 = math.sqrt(sum(b * b for b in embedding2))
        
        # Avoid division by zero
        if magnitude1 == 0 or magnitude2 == 0:
            return 0.0
            
        return dot_product / (magnitude1 * magnitude2)
        
    def get_provider_info(self) -> Dict[str, Any]:
        """Get provider metadata."""
        return {
            "provider_id": self.provider_id,
            "dimension": self.get_dimension(),
            "created_at": self.created_at,
            "config_keys": list(self.config.keys()),
        }