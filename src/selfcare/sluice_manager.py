#!/usr/bin/env python3
"""
Sluice Manager - The Overflow Sluice Controller

Manages the overflow_sluice/ directory with daily file creation and retention.
Provides promotion mechanisms to channel overflow into the Idea Catalog.

🧙‍♂️ "When the mind overflows, the sluice channels the torrent into manageable streams." 
    - Bootstrap Sentinel's Guide to Cognitive Hydraulics
"""

import datetime
import os
from pathlib import Path
from typing import List, Optional, Dict, Any
import re


class SluiceManager:
    """
    The Overflow Sluice Controller - manages cognitive overflow files
    
    Handles daily markdown file creation, retention policies, and promotion
    workflows to channel excess ideation into structured storage.
    """
    
    def __init__(self, sluice_dir: str = "overflow_sluice", retention_days: int = 7):
        self.sluice_dir = Path(sluice_dir)
        self.retention_days = retention_days
        self.sluice_dir.mkdir(exist_ok=True)
        
        # Create .gitkeep to ensure directory is tracked
        gitkeep_path = self.sluice_dir / ".gitkeep"
        if not gitkeep_path.exists():
            gitkeep_path.touch()
    
    def get_today_file(self) -> Path:
        """Get today's sluice file path"""
        today = datetime.date.today().strftime("%Y-%m-%d")
        return self.sluice_dir / f"{today}.md"
    
    def create_daily_file(self) -> Path:
        """
        Create or get today's overflow sluice file
        
        Returns:
            Path to today's sluice file
        """
        today_file = self.get_today_file()
        
        if not today_file.exists():
            # Create new daily file with header
            header = self._generate_daily_header()
            today_file.write_text(header, encoding='utf-8')
        
        return today_file
    
    def _generate_daily_header(self) -> str:
        """Generate header for daily sluice file"""
        today = datetime.date.today()
        return f"""# Overflow Sluice - {today.strftime("%B %d, %Y")}

> 🌊 *"The mind's river runs deep and swift - let the sluice catch what overflows."* 
> — Bootstrap Sentinel's Hydraulic Wisdom

**Purpose**: Capture cognitive overflow and high-velocity thoughts that need temporary storage before structured processing.

**Today's Focus**: {today.strftime("%A")} - Channel the creative current

---

## Quick Captures

<!-- Add rapid-fire ideas, thoughts, and fragments below -->
<!-- Use promotion tools to move items to structured Idea Catalog -->

"""
    
    def append_line(self, line: str) -> bool:
        """
        Append a line to today's sluice file
        
        Args:
            line: Text to append
            
        Returns:
            True if successful
        """
        try:
            today_file = self.create_daily_file()
            
            # Format the line with timestamp
            timestamp = datetime.datetime.now().strftime("%H:%M")
            formatted_line = f"- `{timestamp}` {line.strip()}\n"
            
            # Append to file
            with open(today_file, 'a', encoding='utf-8') as f:
                f.write(formatted_line)
            
            return True
            
        except Exception:
            return False
    
    def get_lines(self, date: Optional[str] = None) -> List[Dict[str, str]]:
        """
        Get lines from sluice file
        
        Args:
            date: Date in YYYY-MM-DD format, defaults to today
            
        Returns:
            List of line dictionaries with line_no, timestamp, text
        """
        if date:
            sluice_file = self.sluice_dir / f"{date}.md"
        else:
            sluice_file = self.get_today_file()
        
        if not sluice_file.exists():
            return []
        
        lines = []
        content = sluice_file.read_text(encoding='utf-8')
        
        # Parse lines with timestamps
        line_pattern = r'^- `(\d{2}:\d{2})` (.+)$'
        
        for line_no, line in enumerate(content.split('\n'), 1):
            match = re.match(line_pattern, line)
            if match:
                timestamp, text = match.groups()
                lines.append({
                    'line_no': line_no,
                    'timestamp': timestamp,
                    'text': text,
                    'full_line': line
                })
        
        return lines
    
    def promote_sluice_line(self, line_no: int, date: Optional[str] = None) -> Optional[str]:
        """
        Promote a sluice line to the Idea Catalog
        
        Args:
            line_no: Line number to promote
            date: Date of sluice file, defaults to today
            
        Returns:
            Idea ID if promotion successful, None otherwise
        """
        lines = self.get_lines(date)
        
        # Find the target line
        target_line = None
        for line in lines:
            if line['line_no'] == line_no:
                target_line = line
                break
        
        if not target_line:
            return None
        
        # Import here to avoid circular imports
        from .idea_catalog import IdeaCatalog
        
        # Add to idea catalog
        catalog = IdeaCatalog()
        idea_id = catalog.add_raw(target_line['text'])
        
        # Mark line as promoted in sluice file
        self._mark_line_promoted(line_no, idea_id, date)
        
        return idea_id
    
    def _mark_line_promoted(self, line_no: int, idea_id: str, date: Optional[str] = None):
        """Mark a line as promoted in the sluice file"""
        if date:
            sluice_file = self.sluice_dir / f"{date}.md"
        else:
            sluice_file = self.get_today_file()
        
        if not sluice_file.exists():
            return
        
        # Read content and mark the line
        content = sluice_file.read_text(encoding='utf-8')
        lines = content.split('\n')
        
        # Find and modify the target line
        line_pattern = r'^- `(\d{2}:\d{2})` (.+)$'
        
        for i, line in enumerate(lines):
            if i + 1 == line_no:  # line_no is 1-based
                match = re.match(line_pattern, line)
                if match:
                    timestamp, text = match.groups()
                    lines[i] = f"- `{timestamp}` ~~{text}~~ → 💡`{idea_id}`"
                break
        
        # Write back to file
        sluice_file.write_text('\n'.join(lines), encoding='utf-8')
    
    def prune(self) -> Dict[str, Any]:
        """
        Remove sluice files older than retention period
        
        Returns:
            Summary of pruning operation
        """
        cutoff_date = datetime.date.today() - datetime.timedelta(days=self.retention_days)
        
        pruned_files = []
        kept_files = []
        
        # Scan sluice directory
        for file_path in self.sluice_dir.glob("*.md"):
            # Extract date from filename
            match = re.match(r'(\d{4}-\d{2}-\d{2})\.md', file_path.name)
            if match:
                file_date_str = match.group(1)
                try:
                    file_date = datetime.datetime.strptime(file_date_str, "%Y-%m-%d").date()
                    
                    if file_date < cutoff_date:
                        file_path.unlink()  # Delete file
                        pruned_files.append(file_date_str)
                    else:
                        kept_files.append(file_date_str)
                        
                except ValueError:
                    # Skip files with invalid date format
                    pass
        
        return {
            "pruned_files": sorted(pruned_files),
            "kept_files": sorted(kept_files),
            "cutoff_date": cutoff_date.isoformat(),
            "retention_days": self.retention_days
        }
    
    def list_files(self) -> List[Dict[str, Any]]:
        """
        List all sluice files with metadata
        
        Returns:
            List of file information dictionaries
        """
        files = []
        
        for file_path in sorted(self.sluice_dir.glob("*.md")):
            if file_path.name == ".gitkeep":
                continue
                
            # Extract date from filename
            match = re.match(r'(\d{4}-\d{2}-\d{2})\.md', file_path.name)
            if match:
                file_date = match.group(1)
                lines = self.get_lines(file_date)
                
                # Count promoted lines
                promoted_count = len([
                    line for line in lines 
                    if '→ 💡' in line.get('full_line', '')
                ])
                
                files.append({
                    "date": file_date,
                    "filename": file_path.name,
                    "total_lines": len(lines),
                    "promoted_lines": promoted_count,
                    "file_size": file_path.stat().st_size,
                    "modified": datetime.datetime.fromtimestamp(
                        file_path.stat().st_mtime
                    ).isoformat()
                })
        
        return files
    
    def get_stats(self) -> Dict[str, Any]:
        """Get sluice statistics"""
        files = self.list_files()
        
        total_lines = sum(f["total_lines"] for f in files)
        total_promoted = sum(f["promoted_lines"] for f in files)
        
        return {
            "total_files": len(files),
            "total_lines": total_lines,
            "promoted_lines": total_promoted,
            "retention_days": self.retention_days,
            "oldest_file": files[0]["date"] if files else None,
            "newest_file": files[-1]["date"] if files else None
        }


def main():
    """CLI interface for sluice manager"""
    import argparse
    import json
    
    parser = argparse.ArgumentParser(description="Sluice Manager - Overflow Sluice Controller")
    parser.add_argument('--append', help='Append line to today\'s sluice')
    parser.add_argument('--promote', type=int, help='Promote line number to idea catalog')
    parser.add_argument('--date', help='Date for operations (YYYY-MM-DD)')
    parser.add_argument('--list-lines', action='store_true', help='List lines from sluice file')
    parser.add_argument('--list-files', action='store_true', help='List all sluice files')
    parser.add_argument('--prune', action='store_true', help='Prune old sluice files')
    parser.add_argument('--stats', action='store_true', help='Show sluice statistics')
    parser.add_argument('--create-today', action='store_true', help='Create today\'s sluice file')
    parser.add_argument('--sluice-dir', default='overflow_sluice', help='Sluice directory path')
    parser.add_argument('--retention-days', type=int, default=7, help='File retention days')
    
    args = parser.parse_args()
    
    manager = SluiceManager(args.sluice_dir, args.retention_days)
    
    if args.append:
        if manager.append_line(args.append):
            print(f"🌊 Added to sluice: {args.append}")
        else:
            print("❌ Failed to add to sluice")
            
    elif args.promote is not None:
        idea_id = manager.promote_sluice_line(args.promote, args.date)
        if idea_id:
            print(f"🚀 Line {args.promote} promoted to idea catalog as {idea_id}")
        else:
            print(f"❌ Line {args.promote} not found")
            
    elif args.list_lines:
        lines = manager.get_lines(args.date)
        date_display = args.date or "today"
        print(f"🌊 Sluice lines for {date_display}:")
        print("=" * 50)
        for line in lines:
            print(f"{line['line_no']:3d}: [{line['timestamp']}] {line['text']}")
            
    elif args.list_files:
        files = manager.list_files()
        print("🌊 Sluice Files:")
        print("=" * 60)
        for file_info in files:
            print(f"{file_info['date']}: {file_info['total_lines']:3d} lines "
                  f"({file_info['promoted_lines']} promoted)")
                  
    elif args.prune:
        result = manager.prune()
        print(f"🧹 Pruned {len(result['pruned_files'])} files:")
        for date in result['pruned_files']:
            print(f"  Removed: {date}.md")
        print(f"📁 Kept {len(result['kept_files'])} recent files")
        
    elif args.stats:
        stats = manager.get_stats()
        print("📊 Sluice Statistics:")
        print("=" * 25)
        print(f"Total Files: {stats['total_files']}")
        print(f"Total Lines: {stats['total_lines']}")
        print(f"Promoted Lines: {stats['promoted_lines']}")
        print(f"Retention: {stats['retention_days']} days")
        
    elif args.create_today:
        today_file = manager.create_daily_file()
        print(f"📄 Created today's sluice: {today_file}")
        
    else:
        parser.print_help()


if __name__ == "__main__":
    main()