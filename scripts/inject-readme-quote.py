#!/usr/bin/env python3
"""
README Quote Injector - Add scroll quotes to README headers

This script demonstrates how to integrate scroll quotes into README generation
for ambient inspiration throughout documentation.
"""

import sys
import re
from pathlib import Path
from typing import Optional

# Add the ScrollQuoteEngine to Python path
sys.path.insert(0, str(Path(__file__).parent.parent / "src"))

try:
    from ScrollQuoteEngine import ScrollQuoteEngine
except ImportError:
    print("Warning: ScrollQuoteEngine not available")
    ScrollQuoteEngine = None


def inject_readme_quotes(readme_path: Path, backup: bool = True) -> bool:
    """
    Inject quotes into README.md file
    
    Args:
        readme_path: Path to README.md file
        backup: Whether to create backup
        
    Returns:
        True if successful, False otherwise
    """
    if not readme_path.exists():
        print(f"README file not found: {readme_path}")
        return False
    
    # Create backup if requested
    if backup:
        backup_path = readme_path.with_suffix('.md.backup')
        backup_path.write_text(readme_path.read_text(encoding='utf-8'))
        print(f"Created backup: {backup_path}")
    
    # Read current content
    content = readme_path.read_text(encoding='utf-8')
    
    # Initialize quote engine
    if ScrollQuoteEngine:
        engine = ScrollQuoteEngine()
        
        # Get a quote for README context
        quote = engine.get_quote_for_context('readme')
        quote_text = quote.format_markdown()
        
        # Pattern to match the existing quote area or add after the title
        quote_pattern = r'(# .+?)\n(?:>.*?\n)*\n'
        replacement = f'\\1\n\n{quote_text}\n\n'
        
        # Try to replace existing quote
        new_content = re.sub(quote_pattern, replacement, content, count=1)
        
        # If no replacement was made, add after first header
        if new_content == content:
            lines = content.split('\n')
            for i, line in enumerate(lines):
                if line.startswith('# '):
                    lines.insert(i + 1, '')
                    lines.insert(i + 2, quote_text)
                    lines.insert(i + 3, '')
                    new_content = '\n'.join(lines)
                    break
        
        # Write updated content
        readme_path.write_text(new_content, encoding='utf-8')
        print(f"Updated README with quote: {quote.text[:50]}...")
        return True
    else:
        print("Quote engine not available, skipping quote injection")
        return False


def main():
    import argparse
    
    parser = argparse.ArgumentParser(description="Inject scroll quotes into README")
    parser.add_argument('--readme', type=Path, help='Path to README.md file')
    parser.add_argument('--no-backup', action='store_true', help='Skip creating backup')
    
    args = parser.parse_args()
    
    # Default to current directory README
    readme_path = args.readme or Path('README.md')
    
    # Inject quotes
    if inject_readme_quotes(readme_path, backup=not args.no_backup):
        print("✅ README quote injection successful!")
    else:
        print("❌ README quote injection failed!")
        sys.exit(1)


if __name__ == '__main__':
    main()