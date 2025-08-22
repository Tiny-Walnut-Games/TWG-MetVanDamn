#!/usr/bin/env python3
"""
Quote Engine - Core functionality for the Scroll Quote Engine

Provides randomized quote selection from the Secret Art of the Living Dev,
with support for different contexts and content filtering.
"""

import random
import yaml
import os
import sys
from enum import Enum
from pathlib import Path
from typing import Dict, List, Optional, Union, Any
from dataclasses import dataclass


class QuoteCategory(Enum):
    """Categories of quotes for different contexts"""
    GENERAL = "general"
    DEVELOPMENT = "development" 
    DEBUGGING = "debugging"
    DOCUMENTATION = "documentation"
    COMMITS = "commits"
    CI_CD = "ci_cd"
    BUTTSAFE = "buttsafe"
    WORKFLOW = "workflow"
    LORE = "lore"


@dataclass
class Quote:
    """Represents a single quote from the scrolls"""
    text: str
    author: str
    source: str
    category: QuoteCategory
    volume: str
    tags: List[str]
    buttsafe_certified: bool = True
    
    def __str__(self) -> str:
        return f'"{self.text}" — {self.source}, {self.volume}'
    
    def format_markdown(self) -> str:
        """Format quote for markdown display"""
        return f'> *"{self.text}"* — **{self.source}, {self.volume}**'
    
    def format_cli(self) -> str:
        """Format quote for CLI display"""
        return f'🪶 "{self.text}"\n   — {self.source}, {self.volume}'


class ScrollQuoteEngine:
    """Main quote engine for accessing the Secret Art of the Living Dev"""
    
    def __init__(self, quotes_path: Optional[Path] = None):
        """
        Initialize the quote engine
        
        Args:
            quotes_path: Path to quotes database, defaults to bundled quotes
        """
        self.quotes_path = quotes_path or self._get_default_quotes_path()
        self.quotes: Dict[QuoteCategory, List[Quote]] = {}
        self._load_quotes()
    
    def _get_default_quotes_path(self) -> Path:
        """Get path to default quotes database"""
        current_dir = Path(__file__).parent
        project_root = current_dir.parent.parent
        return project_root / "data" / "secret_art_quotes.yaml"
    
    def _load_quotes(self) -> None:
        """Load quotes from the database"""
        try:
            if not self.quotes_path.exists():
                # Create default quotes if file doesn't exist
                self._create_default_quotes()
            
            with open(self.quotes_path, 'r', encoding='utf-8') as f:
                quotes_data = yaml.safe_load(f)
            
            self.quotes = {}
            for category_name, quote_list in quotes_data.get('quotes', {}).items():
                try:
                    category = QuoteCategory(category_name)
                    self.quotes[category] = []
                    
                    for quote_data in quote_list:
                        quote = Quote(
                            text=quote_data['text'],
                            author=quote_data.get('author', 'Anonymous'),
                            source=quote_data.get('source', 'Secret Art of the Living Dev'),
                            category=category,
                            volume=quote_data.get('volume', 'Vol. I'),
                            tags=quote_data.get('tags', []),
                            buttsafe_certified=quote_data.get('buttsafe_certified', True)
                        )
                        self.quotes[category].append(quote)
                except ValueError:
                    print(f"Warning: Unknown quote category '{category_name}', skipping")
                    
        except Exception as e:
            print(f"Error loading quotes: {e}")
            self._create_emergency_quotes()
    
    def _create_default_quotes(self) -> None:
        """Create default quotes database if none exists"""
        # We'll populate this with a rich database in the next step
        pass
    
    def _create_emergency_quotes(self) -> None:
        """Create minimal emergency quotes if loading fails"""
        emergency_quote = Quote(
            text="When the quote engine fails, improvisation becomes the highest art.",
            author="Emergency Protocols",
            source="Disaster Recovery Scrolls",
            category=QuoteCategory.GENERAL,
            volume="Vol. 404",
            tags=["emergency", "resilience"],
            buttsafe_certified=True
        )
        self.quotes = {QuoteCategory.GENERAL: [emergency_quote]}
    
    def get_random_quote(self, category: Optional[QuoteCategory] = None, 
                        tags: Optional[List[str]] = None,
                        buttsafe_only: bool = True) -> Quote:
        """
        Get a random quote, optionally filtered by category and tags
        
        Args:
            category: Specific category to select from, or None for any
            tags: List of tags to filter by
            buttsafe_only: Only return buttsafe certified quotes
            
        Returns:
            A randomly selected quote
        """
        # Get candidate quotes
        candidates = []
        
        if category:
            candidates = self.quotes.get(category, [])
        else:
            # Collect all quotes from all categories
            for quotes_list in self.quotes.values():
                candidates.extend(quotes_list)
        
        # Filter by tags if specified
        if tags:
            candidates = [q for q in candidates if any(tag in q.tags for tag in tags)]
        
        # Filter by buttsafe certification
        if buttsafe_only:
            candidates = [q for q in candidates if q.buttsafe_certified]
        
        # Return random quote or emergency fallback
        if candidates:
            return random.choice(candidates)
        else:
            # Fallback if no matches
            return self._get_fallback_quote()
    
    def _get_fallback_quote(self) -> Quote:
        """Get fallback quote when no matches found"""
        return Quote(
            text="Sometimes the best wisdom comes from silence, but this isn't one of those times.",
            author="Fallback Systems",
            source="Emergency Wisdom Cache",
            category=QuoteCategory.GENERAL,
            volume="Vol. Backup",
            tags=["fallback", "humor"],
            buttsafe_certified=True
        )
    
    def get_quote_for_context(self, context: str) -> Quote:
        """
        Get a contextually appropriate quote
        
        Args:
            context: Context hint (readme, tldl, ci, commit, etc.)
            
        Returns:
            Appropriate quote for the context
        """
        context_map = {
            'readme': QuoteCategory.GENERAL,
            'tldl': QuoteCategory.DOCUMENTATION,
            'ci': QuoteCategory.CI_CD,
            'commit': QuoteCategory.COMMITS,
            'debug': QuoteCategory.DEBUGGING,
            'development': QuoteCategory.DEVELOPMENT,
            'workflow': QuoteCategory.WORKFLOW,
            'lore': QuoteCategory.LORE,
            'buttsafe': QuoteCategory.BUTTSAFE
        }
        
        category = context_map.get(context.lower(), QuoteCategory.GENERAL)
        return self.get_random_quote(category=category)
    
    def list_categories(self) -> List[QuoteCategory]:
        """Get list of available quote categories"""
        return list(self.quotes.keys())
    
    def get_category_stats(self) -> Dict[QuoteCategory, int]:
        """Get count of quotes by category"""
        return {category: len(quotes) for category, quotes in self.quotes.items()}
    
    def validate_quotes_database(self) -> Dict[str, Any]:
        """
        Validate the quotes database for completeness and compliance
        
        Returns:
            Validation report dictionary
        """
        report = {
            'valid': True,
            'total_quotes': 0,
            'buttsafe_certified': 0,
            'categories': len(self.quotes),
            'issues': [],
            'recommendations': []
        }
        
        for category, quotes in self.quotes.items():
            report['total_quotes'] += len(quotes)
            
            for quote in quotes:
                if quote.buttsafe_certified:
                    report['buttsafe_certified'] += 1
                
                # Check for basic quote quality
                if len(quote.text) < 10:
                    report['issues'].append(f"Very short quote in {category.value}: '{quote.text[:20]}...'")
                
                if not quote.source or not quote.volume:
                    report['issues'].append(f"Missing source/volume for quote in {category.value}")
        
        # Add recommendations
        if report['total_quotes'] < 50:
            report['recommendations'].append("Consider adding more quotes for better variety")
        
        if report['buttsafe_certified'] / max(report['total_quotes'], 1) < 0.9:
            report['recommendations'].append("Ensure higher percentage of buttsafe certified quotes")
        
        report['valid'] = len(report['issues']) == 0
        
        return report


def main():
    """CLI interface for the quote engine"""
    import argparse
    
    parser = argparse.ArgumentParser(description="Access the Secret Art of the Living Dev")
    parser.add_argument('--category', choices=[c.value for c in QuoteCategory], 
                       help='Quote category to select from')
    parser.add_argument('--context', help='Context hint for appropriate quote selection')
    parser.add_argument('--tags', nargs='+', help='Filter by tags')
    parser.add_argument('--format', choices=['cli', 'markdown', 'plain'], default='cli',
                       help='Output format')
    parser.add_argument('--validate', action='store_true', 
                       help='Validate quotes database')
    parser.add_argument('--stats', action='store_true',
                       help='Show database statistics')
    parser.add_argument('--quotes-path', type=Path, help='Path to quotes database')
    
    args = parser.parse_args()
    
    # Initialize engine
    engine = ScrollQuoteEngine(quotes_path=args.quotes_path)
    
    if args.validate:
        report = engine.validate_quotes_database()
        print("📜 Quote Database Validation Report")
        print("=" * 40)
        print(f"Status: {'✅ VALID' if report['valid'] else '❌ ISSUES FOUND'}")
        print(f"Total Quotes: {report['total_quotes']}")
        print(f"Buttsafe Certified: {report['buttsafe_certified']} ({report['buttsafe_certified']/max(report['total_quotes'],1)*100:.1f}%)")
        print(f"Categories: {report['categories']}")
        
        if report['issues']:
            print("\nIssues:")
            for issue in report['issues']:
                print(f"  ⚠️  {issue}")
        
        if report['recommendations']:
            print("\nRecommendations:")
            for rec in report['recommendations']:
                print(f"  💡 {rec}")
        
        return
    
    if args.stats:
        stats = engine.get_category_stats()
        print("📊 Quote Database Statistics")
        print("=" * 30)
        for category, count in stats.items():
            print(f"{category.value:15}: {count:3} quotes")
        return
    
    # Get quote
    if args.context:
        quote = engine.get_quote_for_context(args.context)
    elif args.category:
        category = QuoteCategory(args.category)
        quote = engine.get_random_quote(category=category, tags=args.tags)
    else:
        quote = engine.get_random_quote(tags=args.tags)
    
    # Format output
    if args.format == 'markdown':
        print(quote.format_markdown())
    elif args.format == 'plain':
        print(quote.text)
    else:
        print(quote.format_cli())


if __name__ == '__main__':
    main()