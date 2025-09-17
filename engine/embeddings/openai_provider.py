"""
OpenAI Embedding Provider - Cloud-based Semantic Grounding
"""

from typing import List, Dict, Any, Optional
import time
from .base_provider import EmbeddingProvider


class OpenAIEmbeddingProvider(EmbeddingProvider):
    """OpenAI API-based embedding provider."""
    
    def __init__(self, config: Optional[Dict[str, Any]] = None):
        super().__init__(config)
        self.api_key = config.get("api_key") if config else None
        self.model = config.get("model", "text-embedding-ada-002") if config else "text-embedding-ada-002"
        self.dimension = config.get("dimension", 1536) if config else 1536  # ada-002 default
        self._client = None
        
    def _get_client(self):
        """Lazy initialization of OpenAI client."""
        if self._client is None:
            try:
                import openai
                if self.api_key:
                    openai.api_key = self.api_key
                self._client = openai
            except ImportError:
                raise ImportError("OpenAI package not installed. Run: pip install openai")
        return self._client
        
    def embed_text(self, text: str) -> List[float]:
        """Generate OpenAI embedding for text."""
        try:
            client = self._get_client()
            response = client.Embedding.create(
                model=self.model,
                input=text
            )
            return response['data'][0]['embedding']
        except Exception as e:
            # Fallback to mock embedding for development
            print(f"Warning: OpenAI API failed ({e}), using mock embedding")
            return self._create_mock_embedding(text)
            
    def embed_batch(self, texts: List[str]) -> List[List[float]]:
        """Generate OpenAI embeddings for multiple texts."""
        try:
            client = self._get_client()
            response = client.Embedding.create(
                model=self.model,
                input=texts
            )
            return [item['embedding'] for item in response['data']]
        except Exception as e:
            # Fallback to mock embeddings for development
            print(f"Warning: OpenAI API failed ({e}), using mock embeddings")
            return [self._create_mock_embedding(text) for text in texts]
            
    def get_dimension(self) -> int:
        """Get embedding dimension."""
        return self.dimension
        
    def _create_mock_embedding(self, text: str) -> List[float]:
        """Create a mock embedding for development/testing."""
        import hashlib
        import struct
        
        # Use text hash to create deterministic mock embedding
        hash_obj = hashlib.md5(text.encode())
        hash_bytes = hash_obj.digest()
        
        # Convert hash bytes to normalized vector
        vector = []
        for i in range(0, min(len(hash_bytes), self.dimension // 4 * 4), 4):
            # Convert 4 bytes to float
            value = struct.unpack('f', hash_bytes[i:i+4])[0]
            vector.append(value)
            
        # Pad with normalized random-like values based on text
        while len(vector) < self.dimension:
            seed = len(vector) + hash(text)
            normalized_val = (seed % 1000) / 1000.0 - 0.5  # -0.5 to 0.5
            vector.append(normalized_val)
            
        # L2 normalization
        import math
        magnitude = math.sqrt(sum(x * x for x in vector))
        if magnitude > 0:
            vector = [x / magnitude for x in vector]
            
        return vector