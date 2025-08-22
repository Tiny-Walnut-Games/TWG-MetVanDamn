"""
ScrollQuoteEngine - Randomized Lore from the Secret Art of the Living Dev

This module provides ambient inspiration through randomized quotes from the
mystical scrolls of Cheekdom, ensuring buttsafe compliance and TLDL alignment.
"""

from .quote_engine import ScrollQuoteEngine, QuoteCategory
from .content_filter import ContentFilterSentinel

__version__ = "1.0.0"
__all__ = ["ScrollQuoteEngine", "QuoteCategory", "ContentFilterSentinel"]