#!/usr/bin/env python3
"""
Private Journal - The Sanctuary of Personal Reflection

Provides opt-in local journaling capabilities that remain private and
never committed to version control. A safe space for personal thoughts,
debugging notes, and cognitive processing.

🧙‍♂️ "The mind needs both public chronicles and private sanctuaries - 
    honor both with equal reverence." - Bootstrap Sentinel
"""

import datetime
from pathlib import Path
from typing import Optional, Dict, Any, List
import os


class PrivateJournal:
    """
    Private Journal Manager - Local-only, git-ignored personal space
    
    Creates and manages journal entries in the local_journal/ directory
    which is explicitly excluded from version control.
    """
    
    def __init__(self, journal_dir: str = "local_journal"):
        self.journal_dir = Path(journal_dir)
        self._ensure_privacy_setup()
    
    def _ensure_privacy_setup(self):
        """Ensure journal directory is set up with privacy protections"""
        # Create directory if it doesn't exist
        self.journal_dir.mkdir(exist_ok=True)
        
        # Create .gitignore in journal directory as extra protection
        gitignore_path = self.journal_dir / ".gitignore"
        if not gitignore_path.exists():
            gitignore_content = """# Private Journal - Never commit personal reflections
*
!.gitignore

# This directory contains private journal entries and should
# never be committed to version control. It's your personal
# sanctuary for thoughts, debugging notes, and cognitive processing.
"""
            gitignore_path.write_text(gitignore_content, encoding='utf-8')
        
        # Create README to explain the directory
        readme_path = self.journal_dir / "README.md"
        if not readme_path.exists():
            readme_content = """# Private Journal Directory

🔒 **Privacy Notice**: This directory is your personal sanctuary and is **never committed** to version control.

## Purpose
- Personal reflection and thoughts
- Debugging notes and problem-solving journeys  
- Cognitive processing during complex development
- Mood and energy tracking
- Private project notes

## Structure
- Journal entries use the format: `Journal-YYYY-MM-DD_HHMM.md`
- Entries include templates for mood, energy, and focus tracking
- Feel free to customize the structure for your needs

## Privacy Guarantee
- This directory is excluded from Git via `.gitignore`
- Your entries remain completely private and local
- No automated processing or analysis of content

---

*"The wise developer knows that clarity comes through both code and contemplation."* — Bootstrap Sentinel
"""
            readme_path.write_text(readme_content, encoding='utf-8')
    
    def create_entry(self, mood: Optional[str] = None, energy: Optional[str] = None, 
                    content: Optional[str] = None) -> Path:
        """
        Create a new journal entry with template header
        
        Args:
            mood: Optional mood indicator (e.g., "focused", "scattered", "energized")
            energy: Optional energy level (e.g., "high", "medium", "low")
            content: Optional initial content for the entry
            
        Returns:
            Path to the created journal file
        """
        # Generate filename with timestamp
        now = datetime.datetime.now()
        filename = f"Journal-{now.strftime('%Y-%m-%d_%H%M')}.md"
        entry_path = self.journal_dir / filename
        
        # Generate template header
        header = self._generate_entry_header(now, mood, energy)
        
        # Combine header with content
        full_content = header
        if content:
            full_content += f"\n## Initial Thoughts\n\n{content}\n"
        
        # Add space for additional sections
        full_content += """
## Development Notes

<!-- Technical thoughts, debugging insights, code discoveries -->

## Challenges & Solutions

<!-- Problems encountered and how they were (or might be) solved -->

## Reflections

<!-- Personal insights, lessons learned, future considerations -->

## Energy & Focus Tracking

<!-- How am I feeling throughout the day? Any patterns? -->

---

*Remember: This journal is your private space. Write freely and honestly.*
"""
        
        entry_path.write_text(full_content, encoding='utf-8')
        return entry_path
    
    def _generate_entry_header(self, timestamp: datetime.datetime, 
                             mood: Optional[str], energy: Optional[str]) -> str:
        """Generate the template header for a journal entry"""
        
        # Format the timestamp nicely
        date_str = timestamp.strftime("%A, %B %d, %Y")
        time_str = timestamp.strftime("%I:%M %p")
        
        # Mood and energy with defaults
        mood_display = mood or "balanced"
        energy_display = energy or "medium"
        
        header = f"""# Journal Entry - {date_str}

**Time**: {time_str}  
**Mood**: {mood_display}  
**Energy Level**: {energy_display}  
**Weather**: *(optional - how does the day feel?)*

---

## Quick Capture

<!-- What's on your mind right now? Fast thoughts, immediate concerns -->

"""
        return header
    
    def list_entries(self, limit: Optional[int] = None) -> List[Dict[str, Any]]:
        """
        List journal entries with metadata
        
        Args:
            limit: Optional limit on number of entries to return
            
        Returns:
            List of entry information dictionaries
        """
        entries = []
        
        # Find all journal files
        for entry_path in self.journal_dir.glob("Journal-*.md"):
            # Extract timestamp from filename
            filename = entry_path.name
            try:
                # Parse: Journal-YYYY-MM-DD_HHMM.md
                timestamp_str = filename.replace("Journal-", "").replace(".md", "")
                timestamp = datetime.datetime.strptime(timestamp_str, "%Y-%m-%d_%H%M")
                
                # Get file stats
                stat = entry_path.stat()
                
                entries.append({
                    "filename": filename,
                    "path": str(entry_path),
                    "timestamp": timestamp.isoformat(),
                    "date": timestamp.strftime("%Y-%m-%d"),
                    "time": timestamp.strftime("%H:%M"),
                    "size": stat.st_size,
                    "modified": datetime.datetime.fromtimestamp(stat.st_mtime).isoformat()
                })
                
            except ValueError:
                # Skip files that don't match the expected format
                continue
        
        # Sort by timestamp (newest first)
        entries.sort(key=lambda x: x["timestamp"], reverse=True)
        
        if limit:
            entries = entries[:limit]
        
        return entries
    
    def get_entry_content(self, filename: str) -> Optional[str]:
        """
        Get content of a specific journal entry
        
        Args:
            filename: Name of the journal file
            
        Returns:
            Content of the entry or None if not found
        """
        entry_path = self.journal_dir / filename
        
        if entry_path.exists() and entry_path.is_file():
            return entry_path.read_text(encoding='utf-8')
        
        return None
    
    def append_to_today(self, content: str, section: str = "Quick Capture") -> bool:
        """
        Append content to today's journal entry
        
        Args:
            content: Content to append
            section: Section to append to (default: "Quick Capture")
            
        Returns:
            True if successful, False otherwise
        """
        today = datetime.date.today()
        
        # Find today's entry (there might be multiple with different times)
        today_entries = [
            entry for entry in self.list_entries()
            if entry["date"] == today.isoformat()
        ]
        
        if not today_entries:
            # Create new entry if none exists for today
            self.create_entry(content=content)
            return True
        
        # Use the most recent entry from today
        latest_entry = today_entries[0]
        entry_path = Path(latest_entry["path"])
        
        try:
            # Read current content
            current_content = entry_path.read_text(encoding='utf-8')
            
            # Find the section to append to
            section_marker = f"## {section}"
            
            if section_marker in current_content:
                # Append to existing section
                lines = current_content.split('\n')
                
                # Find the section and insert content
                in_target_section = False
                insert_index = len(lines)
                
                for i, line in enumerate(lines):
                    if line.strip() == section_marker:
                        in_target_section = True
                    elif in_target_section and line.startswith('## '):
                        # Found next section, insert before it
                        insert_index = i
                        break
                
                # Insert the new content
                timestamp = datetime.datetime.now().strftime("%H:%M")
                new_lines = [
                    f"",
                    f"**{timestamp}**: {content}",
                    f""
                ]
                
                lines[insert_index:insert_index] = new_lines
                entry_path.write_text('\n'.join(lines), encoding='utf-8')
                
            else:
                # Section doesn't exist, append at end
                timestamp = datetime.datetime.now().strftime("%H:%M")
                append_content = f"\n\n## {section}\n\n**{timestamp}**: {content}\n"
                
                with open(entry_path, 'a', encoding='utf-8') as f:
                    f.write(append_content)
            
            return True
            
        except Exception:
            return False
    
    def search_entries(self, query: str, limit: int = 10) -> List[Dict[str, Any]]:
        """
        Search journal entries for specific text
        
        Args:
            query: Text to search for
            limit: Maximum number of results
            
        Returns:
            List of matching entries with context
        """
        results = []
        query_lower = query.lower()
        
        entries = self.list_entries()
        
        for entry in entries:
            content = self.get_entry_content(entry["filename"])
            if not content:
                continue
            
            if query_lower in content.lower():
                # Find context around the match
                lines = content.split('\n')
                matching_lines = []
                
                for i, line in enumerate(lines):
                    if query_lower in line.lower():
                        # Get context: 1 line before and after
                        start = max(0, i - 1)
                        end = min(len(lines), i + 2)
                        context = lines[start:end]
                        matching_lines.extend(context)
                
                results.append({
                    **entry,
                    "matching_context": matching_lines[:5],  # Limit context
                    "query": query
                })
                
                if len(results) >= limit:
                    break
        
        return results
    
    def get_stats(self) -> Dict[str, Any]:
        """Get journal statistics"""
        entries = self.list_entries()
        
        if not entries:
            return {
                "total_entries": 0,
                "date_range": None,
                "total_size": 0,
                "average_size": 0
            }
        
        total_size = sum(entry["size"] for entry in entries)
        
        return {
            "total_entries": len(entries),
            "oldest_entry": entries[-1]["date"] if entries else None,
            "newest_entry": entries[0]["date"] if entries else None,
            "total_size": total_size,
            "average_size": total_size // len(entries) if entries else 0,
            "journal_directory": str(self.journal_dir)
        }


def main():
    """CLI interface for private journal"""
    import argparse
    import json
    
    parser = argparse.ArgumentParser(description="Private Journal - Your Personal Development Sanctuary")
    parser.add_argument('--create', action='store_true', help='Create new journal entry')
    parser.add_argument('--mood', help='Initial mood for new entry')
    parser.add_argument('--energy', help='Initial energy level for new entry')
    parser.add_argument('--content', help='Initial content for new entry')
    parser.add_argument('--list', action='store_true', help='List journal entries')
    parser.add_argument('--limit', type=int, default=10, help='Limit number of entries to show')
    parser.add_argument('--read', help='Read specific journal entry by filename')
    parser.add_argument('--append', help='Append content to today\'s entry')
    parser.add_argument('--section', default='Quick Capture', help='Section to append to')
    parser.add_argument('--search', help='Search journal entries')
    parser.add_argument('--stats', action='store_true', help='Show journal statistics')
    parser.add_argument('--journal-dir', default='local_journal', help='Journal directory path')
    
    args = parser.parse_args()
    
    journal = PrivateJournal(args.journal_dir)
    
    if args.create:
        entry_path = journal.create_entry(args.mood, args.energy, args.content)
        print(f"📝 Created journal entry: {entry_path.name}")
        print(f"💡 Edit with: code {entry_path}")
        
    elif args.list:
        entries = journal.list_entries(args.limit)
        if entries:
            print(f"📖 Journal Entries ({len(entries)} shown):")
            print("=" * 50)
            for entry in entries:
                size_kb = entry["size"] / 1024
                print(f"{entry['filename']} - {size_kb:.1f}KB")
                print(f"  {entry['date']} at {entry['time']}")
                print()
        else:
            print("📖 No journal entries found")
            print("💡 Create your first entry with --create")
            
    elif args.read:
        content = journal.get_entry_content(args.read)
        if content:
            print(f"📖 Reading: {args.read}")
            print("=" * 60)
            print(content)
        else:
            print(f"❌ Entry not found: {args.read}")
            
    elif args.append:
        if journal.append_to_today(args.append, args.section):
            print(f"✏️ Added to today's journal in section '{args.section}'")
        else:
            print("❌ Failed to append to journal")
            
    elif args.search:
        results = journal.search_entries(args.search, args.limit)
        if results:
            print(f"🔍 Search results for '{args.search}' ({len(results)} found):")
            print("=" * 60)
            for result in results:
                print(f"📝 {result['filename']} - {result['date']}")
                print("Context:")
                for line in result['matching_context']:
                    if args.search.lower() in line.lower():
                        print(f"  → {line.strip()}")
                    else:
                        print(f"    {line.strip()}")
                print()
        else:
            print(f"🔍 No entries found matching '{args.search}'")
            
    elif args.stats:
        stats = journal.get_stats()
        print("📊 Journal Statistics:")
        print("=" * 25)
        print(f"Total Entries: {stats['total_entries']}")
        if stats['total_entries'] > 0:
            print(f"Date Range: {stats['oldest_entry']} to {stats['newest_entry']}")
            print(f"Total Size: {stats['total_size'] / 1024:.1f} KB")
            print(f"Average Size: {stats['average_size'] / 1024:.1f} KB")
        print(f"Directory: {stats['journal_directory']}")
        
    else:
        parser.print_help()


if __name__ == "__main__":
    main()