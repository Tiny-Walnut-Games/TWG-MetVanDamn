#!/usr/bin/env python3
"""
Idea Capture CLI - Quick Idea Intake for High-Velocity Ideation

Provides rapid command-line interface for capturing ideas into the
Idea Catalog during high-velocity development sessions.

Usage:
    python scripts/idea_capture.py "Your brilliant idea here"
    python scripts/idea_capture.py "Another idea" --tag "!"
    python scripts/idea_capture.py --sluice "Quick overflow thought"

🧙‍♂️ "The fastest pen captures the swiftest thoughts - make idea capture effortless." 
    - Bootstrap Sentinel's Velocity Wisdom
"""

import sys
import os
from pathlib import Path

# Add src to path so we can import selfcare modules
sys.path.insert(0, str(Path(__file__).parent.parent / "src"))

from selfcare.idea_catalog import IdeaCatalog, IdeaTag
from selfcare.sluice_manager import SluiceManager


def main():
    """CLI interface for rapid idea capture"""
    import argparse
    
    parser = argparse.ArgumentParser(
        description="Idea Capture - Rapid idea intake for high-velocity development",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Capture idea to catalog
  python scripts/idea_capture.py "Add dark mode support"
  
  # Capture with immediate classification  
  python scripts/idea_capture.py "Fix urgent login bug" --tag "!"
  
  # Quick overflow to sluice
  python scripts/idea_capture.py --sluice "Refactor user service"
  
  # Promote sluice line to catalog
  python scripts/idea_capture.py --promote-sluice 5
  
Valid tags: ! (urgent), ? (question), ⧗ (later), ♻ (recycle)
        """
    )
    
    # Main capture modes
    parser.add_argument('idea', nargs='?', help='Idea text to capture')
    parser.add_argument('--tag', choices=['!', '?', '⧗', '♻'], 
                       help='Immediate classification tag')
    parser.add_argument('--sluice', help='Add idea to overflow sluice instead')
    parser.add_argument('--promote-sluice', type=int, 
                       help='Promote sluice line number to idea catalog')
    
    # Quick actions
    parser.add_argument('--list-recent', type=int, default=5, 
                       help='List recent ideas (default: 5)')
    parser.add_argument('--stats', action='store_true', 
                       help='Show quick statistics')
    parser.add_argument('--sluice-lines', action='store_true',
                       help='Show today\'s sluice lines')
    
    # Configuration
    parser.add_argument('--catalog-path', default='data/idea_catalog.json',
                       help='Path to idea catalog')
    parser.add_argument('--sluice-dir', default='overflow_sluice',
                       help='Overflow sluice directory')
    
    args = parser.parse_args()
    
    # Initialize managers
    catalog = IdeaCatalog(args.catalog_path)
    sluice = SluiceManager(args.sluice_dir)
    
    # Handle different modes
    if args.idea:
        # Main idea capture to catalog
        idea_id = catalog.add_raw(args.idea)
        print(f"✨ Idea captured: {idea_id}")
        
        # Apply tag if specified
        if args.tag:
            if catalog.classify(idea_id, args.tag):
                tag_name = {
                    '!': 'urgent',
                    '?': 'question', 
                    '⧗': 'later',
                    '♻': 'recycle'
                }[args.tag]
                print(f"🏷️ Tagged as: {tag_name}")
            else:
                print("⚠️ Failed to apply tag")
        
        print(f"💡 Use 'python src/selfcare/idea_catalog.py --promote {idea_id}' to promote")
        
    elif args.sluice:
        # Quick capture to overflow sluice
        if sluice.append_line(args.sluice):
            print(f"🌊 Added to overflow sluice: {args.sluice}")
            print("💡 Use --sluice-lines to see all sluice entries")
        else:
            print("❌ Failed to add to sluice")
            
    elif args.promote_sluice is not None:
        # Promote sluice line to catalog
        idea_id = sluice.promote_sluice_line(args.promote_sluice)
        if idea_id:
            print(f"🚀 Sluice line {args.promote_sluice} promoted to catalog: {idea_id}")
        else:
            print(f"❌ Sluice line {args.promote_sluice} not found")
            
    elif args.sluice_lines:
        # Show today's sluice lines
        lines = sluice.get_lines()
        if lines:
            print("🌊 Today's Overflow Sluice:")
            print("=" * 40)
            for line in lines:
                promoted_mark = " 🚀" if "→ 💡" in line['full_line'] else ""
                print(f"{line['line_no']:2d}: [{line['timestamp']}] {line['text']}{promoted_mark}")
        else:
            print("🌊 No overflow sluice entries today")
            print("💡 Use --sluice 'your thought' to add quick captures")
            
    elif args.stats:
        # Quick statistics
        catalog_stats = catalog.get_stats()
        sluice_stats = sluice.get_stats()
        
        print("📊 Self-Care System Status:")
        print("=" * 30)
        print(f"💡 Ideas in catalog: {catalog_stats['total_ideas']}")
        print(f"🚀 Promoted ideas: {catalog_stats['promoted_ideas']}")
        print(f"🌊 Sluice files: {sluice_stats['total_files']}")
        print(f"🌊 Sluice lines: {sluice_stats['total_lines']}")
        
        if catalog_stats['by_tag']:
            print("\n🏷️ Ideas by tag:")
            for tag, count in catalog_stats['by_tag'].items():
                tag_name = tag if tag != 'untagged' else 'no tag'
                print(f"   {tag_name}: {count}")
                
    else:
        # Default: show recent ideas
        ideas = catalog.list(limit=args.list_recent)
        if ideas:
            print(f"💡 Recent Ideas ({len(ideas)} shown):")
            print("=" * 50)
            for idea in ideas:
                tag_display = f" [{idea.get('tag', '')}]" if idea.get('tag') else ""
                promoted_display = " 🚀" if idea.get('promoted') else ""
                timestamp = idea['timestamp'][:16].replace('T', ' ')
                print(f"{idea['id']}: {idea['text'][:60]}...{tag_display}{promoted_display}")
                print(f"    {timestamp}")
                print()
        else:
            print("💡 No ideas in catalog yet")
            
        print("💭 Quick commands:")
        print("  Add idea: python scripts/idea_capture.py 'your idea'")
        print("  Add to sluice: python scripts/idea_capture.py --sluice 'quick thought'")
        print("  Show help: python scripts/idea_capture.py --help")


if __name__ == "__main__":
    main()