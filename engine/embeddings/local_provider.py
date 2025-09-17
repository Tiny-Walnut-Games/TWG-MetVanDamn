"""
Local Embedding Provider - Fallback Semantic Grounding
Simple TF-IDF based embeddings for offline operation
"""

from typing import List, Dict, Any, Optional
import re
import math
from collections import Counter
from .base_provider import EmbeddingProvider


class LocalEmbeddingProvider(EmbeddingProvider):
    """Local TF-IDF based embedding provider for fallback scenarios."""
    
    def __init__(self, config: Optional[Dict[str, Any]] = None):
        super().__init__(config)
        self.vocabulary = set()
        self.document_frequency = Counter()
        self.total_documents = 0
        self.vector_dimension = config.get("dimension", 128) if config else 128
        
    def embed_text(self, text: str) -> List[float]:
        """Generate TF-IDF based embedding for text."""
        # Tokenize and clean text
        tokens = self._tokenize(text)
        
        # Update vocabulary and document frequency
        self._update_vocabulary(tokens)
        
        # Calculate TF-IDF vector
        tf_vector = self._calculate_tf(tokens)
        tfidf_vector = self._calculate_tfidf(tf_vector)
        
        # Pad or truncate to fixed dimension
        return self._normalize_vector(tfidf_vector)
        
    def embed_batch(self, texts: List[str]) -> List[List[float]]:
        """Generate embeddings for multiple texts."""
        # First pass: update vocabulary with all texts
        all_tokens = []
        for text in texts:
            tokens = self._tokenize(text)
            all_tokens.append(tokens)
            self._update_vocabulary(tokens, update_df=False)
            
        # Update document frequencies
        self.total_documents += len(texts)
        for tokens in all_tokens:
            unique_tokens = set(tokens)
            for token in unique_tokens:
                self.document_frequency[token] += 1
                
        # Second pass: generate embeddings
        embeddings = []
        for tokens in all_tokens:
            tf_vector = self._calculate_tf(tokens)
            tfidf_vector = self._calculate_tfidf(tf_vector)
            embeddings.append(self._normalize_vector(tfidf_vector))
            
        return embeddings
        
    def get_dimension(self) -> int:
        """Get embedding dimension."""
        return self.vector_dimension
        
    def _tokenize(self, text: str) -> List[str]:
        """Simple tokenization."""
        # Convert to lowercase and extract words
        text = text.lower()
        tokens = re.findall(r'\b\w+\b', text)
        
        # Filter out common stop words
        stop_words = {'the', 'a', 'an', 'and', 'or', 'but', 'in', 'on', 'at', 'to', 'for', 'of', 'with', 'by', 'is', 'are', 'was', 'were'}
        tokens = [token for token in tokens if token not in stop_words and len(token) > 2]
        
        return tokens
        
    def _update_vocabulary(self, tokens: List[str], update_df: bool = True):
        """Update vocabulary and document frequency."""
        self.vocabulary.update(tokens)
        
        if update_df:
            self.total_documents += 1
            unique_tokens = set(tokens)
            for token in unique_tokens:
                self.document_frequency[token] += 1
                
    def _calculate_tf(self, tokens: List[str]) -> Dict[str, float]:
        """Calculate term frequency."""
        token_count = len(tokens)
        if token_count == 0:
            return {}
            
        tf_dict = Counter(tokens)
        return {token: count / token_count for token, count in tf_dict.items()}
        
    def _calculate_tfidf(self, tf_vector: Dict[str, float]) -> Dict[str, float]:
        """Calculate TF-IDF vector."""
        tfidf_vector = {}
        
        for token, tf in tf_vector.items():
            # IDF calculation with smoothing
            df = self.document_frequency.get(token, 1)
            idf = math.log((self.total_documents + 1) / (df + 1)) + 1
            tfidf_vector[token] = tf * idf
            
        return tfidf_vector
        
    def _normalize_vector(self, tfidf_vector: Dict[str, float]) -> List[float]:
        """Convert to fixed-dimension normalized vector."""
        # Create vocab list sorted for consistency
        vocab_list = sorted(list(self.vocabulary))
        
        # Limit vocabulary size to dimension
        if len(vocab_list) > self.vector_dimension:
            vocab_list = vocab_list[:self.vector_dimension]
            
        # Create vector
        vector = []
        for i, token in enumerate(vocab_list):
            if i >= self.vector_dimension:
                break
            vector.append(tfidf_vector.get(token, 0.0))
            
        # Pad with zeros if needed
        while len(vector) < self.vector_dimension:
            vector.append(0.0)
            
        # L2 normalization
        magnitude = math.sqrt(sum(x * x for x in vector))
        if magnitude > 0:
            vector = [x / magnitude for x in vector]
            
        return vector