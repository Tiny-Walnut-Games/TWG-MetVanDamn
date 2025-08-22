#!/usr/bin/env python3
"""
TLDL Monthly Archive Generator

Consolidates all TLDL entries from a specified month into a single monthly archive
with actionables, lost features, key decisions, and cross-links.

Usage:
    python3 scripts/tldl_monthly_generator.py --month 2025-08
    python3 scripts/tldl_monthly_generator.py --auto  # Current month
"""

import os
import re
import argparse
import yaml
from datetime import datetime, timedelta
from pathlib import Path
import json

class TLDLMonthlyGenerator:
    def __init__(self, repo_root="."):
        self.repo_root = Path(repo_root)
        self.tldl_paths = [
            self.repo_root / "docs",
            self.repo_root / "TLDL" / "entries"
        ]
        self.output_dir = self.repo_root / "docs" / "TLDL-Monthly"
        self.archive_dir = self.repo_root / "docs" / "TLDL-Archive"
        
    def find_tldl_files(self, target_month):
        """Find all TLDL files for the specified month"""
        tldl_files = []
        
        # Parse target month
        year, month = target_month.split('-')
        month_prefix = f"TLDL-{year}-{month.zfill(2)}-"
        
        for tldl_path in self.tldl_paths:
            if tldl_path.exists():
                for file_path in tldl_path.glob("TLDL-*.md"):
                    filename = file_path.name
                    if filename.startswith(month_prefix):
                        tldl_files.append(file_path)
                        
                # Also check for files without TLDL- prefix but with date pattern
                date_pattern = f"{year}-{month.zfill(2)}-"
                for file_path in tldl_path.glob(f"*{date_pattern}*.md"):
                    filename = file_path.name
                    if date_pattern in filename and file_path not in tldl_files:
                        tldl_files.append(file_path)
        
        return sorted(tldl_files, key=lambda x: x.name)
    
    def parse_tldl_entry(self, file_path):
        """Parse a TLDL file and extract key information"""
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # Extract YAML frontmatter if present
            frontmatter = {}
            yaml_match = re.match(r'^---\s*\n(.*?)\n---\s*\n', content, re.DOTALL)
            if yaml_match:
                try:
                    frontmatter = yaml.safe_load(yaml_match.group(1)) or {}
                except yaml.YAMLError:
                    pass
            
            # Extract sections using regex
            entry_data = {
                'file_path': file_path,
                'filename': file_path.name,
                'frontmatter': frontmatter,
                'content': content,
                'title': self.extract_title(content),
                'actions_taken': self.extract_section(content, r'## Actions Taken'),
                'next_steps': self.extract_section(content, r'## Next Steps'),
                'discoveries': self.extract_section(content, r'## Discoveries'),
                'lessons_learned': self.extract_section(content, r'## Lessons Learned'),
                'technical_details': self.extract_section(content, r'## Technical Details'),
                'actionables': self.extract_actionables(content)
            }
            
            return entry_data
            
        except Exception as e:
            print(f"Warning: Failed to parse {file_path}: {e}")
            return None
    
    def extract_title(self, content):
        """Extract the title from the TLDL entry"""
        title_match = re.search(r'^# (.+)$', content, re.MULTILINE)
        if title_match:
            return title_match.group(1).strip()
        return "Untitled Entry"
    
    def extract_section(self, content, section_header):
        """Extract content from a specific section"""
        pattern = rf'{section_header}(.*?)(?=^## |\Z)'
        match = re.search(pattern, content, re.MULTILINE | re.DOTALL)
        if match:
            return match.group(1).strip()
        return None
    
    def extract_actionables(self, content):
        """Extract actionable items (TODO, checkboxes, etc.)"""
        actionables = []
        
        # Find checkbox items
        checkbox_pattern = r'- \[([ x])\] (.+)'
        for match in re.finditer(checkbox_pattern, content):
            checked = match.group(1) == 'x'
            text = match.group(2).strip()
            actionables.append({
                'text': text,
                'completed': checked,
                'type': 'checkbox'
            })
        
        # Find TODO items
        todo_pattern = r'(?:TODO|FIXME|XXX):?\s*(.+)'
        for match in re.finditer(todo_pattern, content, re.IGNORECASE):
            text = match.group(1).strip()
            actionables.append({
                'text': text,
                'completed': False,
                'type': 'todo'
            })
        
        return actionables
    
    def generate_monthly_report(self, target_month):
        """Generate the monthly consolidated report"""
        tldl_files = self.find_tldl_files(target_month)
        
        if not tldl_files:
            print(f"No TLDL files found for {target_month}")
            return None
        
        print(f"Found {len(tldl_files)} TLDL files for {target_month}")
        
        # Parse all TLDL entries
        entries = []
        for file_path in tldl_files:
            entry = self.parse_tldl_entry(file_path)
            if entry:
                entries.append(entry)
        
        if not entries:
            print("No valid TLDL entries found")
            return None
        
        # Generate the report
        year, month = target_month.split('-')
        month_name = datetime(int(year), int(month), 1).strftime('%B')
        report_content = self.build_report_content(entries, f"{month_name} {year}")
        
        # Write the report
        output_file = self.output_dir / f"{target_month}.md"
        self.output_dir.mkdir(parents=True, exist_ok=True)
        
        with open(output_file, 'w', encoding='utf-8') as f:
            f.write(report_content)
        
        print(f"Monthly report generated: {output_file}")
        return output_file
    
    def build_report_content(self, entries, month_year):
        """Build the content of the monthly report"""
        report = [
            f"# TLDL Monthly Archive - {month_year}",
            "",
            '> *"When the ledger grows thick, the archivist binds it into tomes."* — **The Chronicle Keeper\'s Codex**',
            "",
            "---",
            "",
            f"## 📊 Archive Summary",
            "",
            f"**Archival Date**: {datetime.now().strftime('%Y-%m-%d')}  ",
            f"**Total TLDL Entries**: {len(entries)}  ",
            f"**Chronicle Period**: {month_year}  ",
            f"**Archive Status**: Complete  ",
            "",
            "---",
            "",
            "## 📜 Chronological Entry Summary",
            ""
        ]
        
        # Chronological summary
        for entry in entries:
            report.extend([
                f"### {entry['title']}",
                f"**File**: `{entry['filename']}`  ",
                f"**Location**: `{entry['file_path'].parent.name}/{entry['filename']}`  ",
                ""
            ])
            
            if entry['frontmatter']:
                if 'status' in entry['frontmatter']:
                    report.append(f"**Status**: {entry['frontmatter']['status']}  ")
                if 'tags' in entry['frontmatter']:
                    tags = ', '.join(entry['frontmatter']['tags'])
                    report.append(f"**Tags**: {tags}  ")
            
            report.append("")
            
            # Add brief summary from actions taken or discoveries
            if entry['actions_taken']:
                summary = entry['actions_taken'][:200] + "..." if len(entry['actions_taken']) > 200 else entry['actions_taken']
                report.extend([
                    "**Summary**:",
                    summary.replace('\n', ' ').strip(),
                    ""
                ])
            elif entry['discoveries']:
                summary = entry['discoveries'][:200] + "..." if len(entry['discoveries']) > 200 else entry['discoveries']
                report.extend([
                    "**Summary**:",
                    summary.replace('\n', ' ').strip(),
                    ""
                ])
            
            report.append("---")
            report.append("")
        
        # Actionables Summary
        all_actionables = []
        for entry in entries:
            for actionable in entry['actionables']:
                actionable['source_entry'] = entry['filename']
                all_actionables.append(actionable)
        
        if all_actionables:
            report.extend([
                "## ✅ Consolidated Actionables",
                "",
                "All actionable items identified across TLDL entries this month:",
                ""
            ])
            
            completed = [a for a in all_actionables if a['completed']]
            pending = [a for a in all_actionables if not a['completed']]
            
            if completed:
                report.extend([
                    "### Completed Actions",
                    ""
                ])
                for actionable in completed:
                    report.append(f"- [x] {actionable['text']} *(from {actionable['source_entry']})*")
                report.append("")
            
            if pending:
                report.extend([
                    "### Pending Actions",
                    ""
                ])
                for actionable in pending:
                    report.append(f"- [ ] {actionable['text']} *(from {actionable['source_entry']})*")
                report.append("")
        
        # Key Decisions & Development Arcs
        report.extend([
            "## 🎯 Key Decisions & Development Arcs",
            "",
            "Major themes and decisions from this month's development activities:",
            ""
        ])
        
        # Analyze patterns in the entries
        themes = self.analyze_themes(entries)
        for theme in themes:
            report.extend([
                f"### {theme['name']}",
                f"**Frequency**: {theme['count']} entries  ",
                f"**Key Insights**: {theme['description']}  ",
                ""
            ])
        
        # Lost Features Integration
        report.extend([
            "## 👻 Lost Features Integration",
            "",
            "*This section would be populated from the Ghost Ledger system*",
            "",
            "- Review monthly Ghost Ledger entries for features discussed but not implemented",
            "- Cross-reference with development activities from this month",
            "- Update ghost status based on monthly progress",
            "",
            "---",
            "",
        ])
        
        # Cross-links
        report.extend([
            "## 🔗 Cross-Links & References",
            "",
            "### Related Documentation",
            f"- [TLDL Index](../TLDL/index.md)",
            f"- [Daily Ledger Archives](../daily-ledger/)",
            f"- [Capsule Scrolls](../../capsules/)",
            "",
            "### Source TLDL Entries",
            ""
        ])
        
        for entry in entries:
            relative_path = os.path.relpath(entry['file_path'], self.repo_root / "docs")
            report.append(f"- [{entry['title']}](../{relative_path})")
        
        report.extend([
            "",
            "---",
            "",
            f"**Archive Generated**: {datetime.now().strftime('%Y-%m-%d %H:%M:%S UTC')}  ",
            f"**Generated by**: TLDL Monthly Archive System  ",
            f"**Preservation Status**: 🍑 Buttsafe Certified  ",
            "",
            "*\"Every chronicle preserved is a future saved from context loss.\"* 📚✨"
        ])
        
        return '\n'.join(report)
    
    def analyze_themes(self, entries):
        """Analyze themes and patterns across entries"""
        themes = []
        
        # Simple theme analysis based on common keywords
        theme_keywords = {
            "Chronicle Keeper": ["chronicle", "keeper", "scribe", "tldl generation"],
            "Automation Systems": ["automation", "script", "workflow", "ci"],
            "Documentation": ["documentation", "docs", "guide", "manual"],
            "Validation & Quality": ["validation", "testing", "quality", "linting"],
            "Development Workflow": ["workflow", "development", "process", "methodology"]
        }
        
        for theme_name, keywords in theme_keywords.items():
            count = 0
            descriptions = []
            
            for entry in entries:
                content = entry['content'].lower()
                if any(keyword in content for keyword in keywords):
                    count += 1
                    # Extract a relevant snippet
                    for keyword in keywords:
                        if keyword in content:
                            descriptions.append(f"Enhanced {keyword} capabilities")
                            break
            
            if count > 0:
                themes.append({
                    'name': theme_name,
                    'count': count,
                    'description': f"Focus on {', '.join(descriptions[:2])}" if descriptions else "Development activities"
                })
        
        return sorted(themes, key=lambda x: x['count'], reverse=True)

def main():
    parser = argparse.ArgumentParser(description='Generate monthly TLDL archives')
    parser.add_argument('--month', help='Target month in YYYY-MM format (e.g., 2025-08)')
    parser.add_argument('--auto', action='store_true', help='Use previous month automatically')
    parser.add_argument('--repo-root', default='.', help='Repository root directory')
    
    args = parser.parse_args()
    
    if args.auto:
        # Use previous month
        now = datetime.now()
        if now.month == 1:
            target_month = f"{now.year - 1}-12"
        else:
            target_month = f"{now.year}-{now.month - 1:02d}"
    elif args.month:
        target_month = args.month
    else:
        parser.error("Either --month or --auto must be specified")
    
    # Validate month format
    if not re.match(r'^\d{4}-\d{1,2}$', target_month):
        parser.error("Month must be in YYYY-MM format")
    
    generator = TLDLMonthlyGenerator(args.repo_root)
    output_file = generator.generate_monthly_report(target_month)
    
    if output_file:
        print(f"\n✅ Monthly archive generated successfully!")
        print(f"📁 Location: {output_file}")
        print(f"📊 Archive covers: {target_month}")
    else:
        print("❌ Failed to generate monthly archive")

if __name__ == "__main__":
    main()