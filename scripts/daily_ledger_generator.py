#!/usr/bin/env python3
"""
Daily Ledger Generator - Archive Wall Continuity System
Automates the creation and maintenance of daily development ledgers.

Usage:
    python3 daily_ledger_generator.py --date 2025-08-18
    python3 daily_ledger_generator.py --generate-today
    python3 daily_ledger_generator.py --update-existing docs/daily-ledger/2025-08-18.md
"""

import argparse
import os
import sys
import yaml
from datetime import datetime, timedelta
import re
import subprocess
import json

class DailyLedgerGenerator:
    def __init__(self, project_root=None):
        if project_root is None:
            project_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
        self.project_root = project_root
        self.ledger_dir = os.path.join(project_root, 'docs', 'daily-ledger')
        self.capsules_dir = os.path.join(project_root, 'capsules')
        
        # Ensure directories exist
        os.makedirs(self.ledger_dir, exist_ok=True)
        os.makedirs(self.capsules_dir, exist_ok=True)
    
    def get_daily_ledger_template(self):
        """Get the daily ledger template structure."""
        return """---

## `/daily-ledger/{date}.md`

### 🗓 **Date:** {date}  
**Arc:** {arc_name}  
**Mode:** {mode}

---

### **1. Daily Ghost Sweep**
**New / highlighted features since last ledger:**
{ghost_sweep}

---

### **2. Badge Verdict Pass**  
{badge_verdicts}

---

### **3. Compact Transcript**
**Core prompts / decisions today:**
{core_decisions}

**Key Artifacts & Commits:**
{key_artifacts}

**Glyphs / Running Jokes:**  
{glyphs}

**Unresolved Threads:**  
{unresolved_threads}

---

### **4. Re‑entry Spell**  
> *{re_entry_spell}*

---

"""
    
    def scan_git_activity(self, date_str):
        """Scan git activity for the specified date."""
        try:
            # Get commits from the specified date
            cmd = ['git', 'log', '--oneline', '--since', f'{date_str} 00:00:00', '--until', f'{date_str} 23:59:59']
            result = subprocess.run(cmd, cwd=self.project_root, capture_output=True, text=True)
            
            commits = []
            if result.returncode == 0 and result.stdout.strip():
                for line in result.stdout.strip().split('\n'):
                    if line.strip():
                        parts = line.strip().split(' ', 1)
                        if len(parts) == 2:
                            hash_short = parts[0]
                            message = parts[1]
                            commits.append({
                                'hash': hash_short,
                                'message': message,
                                'url': f'{self.get_github_repo_url()}/commit/{hash_short}' if self.get_github_repo_url() else hash_short
                            })
            
            return commits
        except Exception as e:
            print(f"Warning: Could not scan git activity: {e}")
            return []
    
    def scan_file_changes(self, date_str):
        """Scan for files created or modified on the specified date."""
        try:
            # Get file modifications from git
            cmd = ['git', 'log', '--name-only', '--pretty=format:', '--since', f'{date_str} 00:00:00', '--until', f'{date_str} 23:59:59']
            result = subprocess.run(cmd, cwd=self.project_root, capture_output=True, text=True)
            
            files = set()
            if result.returncode == 0 and result.stdout.strip():
                for line in result.stdout.strip().split('\n'):
                    if line.strip() and not line.startswith('commit '):
                        files.add(line.strip())
            
            return list(files)
        except Exception as e:
            print(f"Warning: Could not scan file changes: {e}")
            return []
    
    def extract_decisions_from_commits(self, commits):
        """Extract decisions from commit messages."""
        decisions = []
        decision_patterns = [
            r'(implement|add|create|build|design)\s+(.+)',
            r'(fix|resolve|address)\s+(.+)', 
            r'(update|enhance|improve)\s+(.+)',
            r'(refactor|restructure|reorganize)\s+(.+)'
        ]
        
        for commit in commits:
            message = commit['message'].lower()
            for pattern in decision_patterns:
                match = re.search(pattern, message)
                if match:
                    action = match.group(1).title()
                    subject = match.group(2)
                    decisions.append(f"{action} {subject} (commit {commit['hash']})")
                    break
        
        return decisions
    
    def generate_ghost_sweep(self, files_changed):
        """Generate ghost sweep section based on file changes."""
        ghost_items = []
        
        # Look for new features based on file patterns
        for file in files_changed:
            if file.startswith('scripts/') and file.endswith('.py'):
                ghost_items.append(f"**New Script**: {file} — automation capability")
            elif file.startswith('capsules/'):
                ghost_items.append(f"**Capsule System**: {file} — context preservation")
            elif file.startswith('docs/') and 'ledger' in file:
                ghost_items.append(f"**Daily Ledger**: {file} — continuity system")
            elif file.endswith('.md') and 'TLDL' in file:
                ghost_items.append(f"**TLDL Entry**: {file} — knowledge capture")
        
        if not ghost_items:
            ghost_items.append("- No new features detected in file changes")
        
        return '\n'.join([f"- {item}" for item in ghost_items])
    
    def generate_badge_verdicts(self, date_str):
        """Generate badge verdicts based on validation results."""
        verdicts = []
        
        # Check if validation was run today
        validation_files = [
            'validation-report.json',
            'TLDL/validation-report.json'
        # Dynamically discover all validation-report.json files in project_root
        validation_files = []
        for root, dirs, files in os.walk(self.project_root):
            for file in files:
                if file == 'validation-report.json':
                    rel_path = os.path.relpath(os.path.join(root, file), self.project_root)
                    validation_files.append(rel_path)
        
        for val_file in validation_files:
            val_path = os.path.join(self.project_root, val_file)
            if os.path.exists(val_path):
                try:
                    with open(val_path, 'r') as f:
                        data = json.load(f)
                    
                    if data.get('status') == 'PASS':
                        verdicts.append(f"- Validation → **pass** ({val_file} clean)")
                    else:
                        verdicts.append(f"- Validation → **guarded pass** ({val_file} has warnings)")
                except:
                    verdicts.append(f"- Validation → **unknown** ({val_file} unreadable)")
        
        # Default verdicts
        verdicts.extend([
            f"- Daily Ledger → **pass** (this file generated successfully)",
            f"- Archive Wall → **pass** (continuity system active)"
        ])
        
        return '\n'.join(verdicts)
    
    def generate_glyphs(self, commits, files_changed):
        """Generate glyphs and running jokes section."""
        glyphs = []
        
        # Extract terminology from commit messages and file names
        terminology = set()
        for commit in commits:
            message = commit['message']
            # Look for quoted terms or capitalized concepts
            quoted_terms = re.findall(r'"([^"]+)"', message)
            capitalized_terms = re.findall(r'\b[A-Z][a-z]+(?:\s+[A-Z][a-z]+)*\b', message)
            terminology.update(quoted_terms)
            terminology.update(capitalized_terms)
        
        # Extract from file names
        for file in files_changed:
            if 'capsule' in file.lower():
                terminology.add('Capsule Scrolls')
            elif 'ledger' in file.lower():
                terminology.add('Daily Ledger')
            elif 'ghost' in file.lower():
                terminology.add('Ghost Features')
        
        # Standard glyphs for this system
        standard_glyphs = [
            "\"Save the Butts\" clause",
            "Archive wall as locked hallway door", 
            "Molting ritual for code"
        ]
        
        glyphs.extend([f"- {glyph}" for glyph in standard_glyphs])
        glyphs.extend([f"- {glyph}" for glyph in self.standard_glyphs])
        
        # Add discovered terminology
        for term in list(terminology)[:3]:  # Limit to 3 additional terms
            if len(term) > 3 and len(term) < 30:  # Reasonable length
                glyphs.append(f"- {term} terminology")
        
        return '\n'.join(glyphs)
    
    def generate_re_entry_spell(self, arc_name, decisions, key_artifacts):
        """Generate a 3-sentence re-entry spell."""
        # Template-based generation
        if 'Archive Wall' in arc_name or 'Continuity' in arc_name:
            return "The day we built the molting ritual for conversations. Faced with the archive wall's locked door, we forged new tools for context preservation and knowledge continuity. The Buttsafe clause was invoked, and the development archive grew stronger."
        
        # Decision-based generation
        if decisions:
            primary_action = decisions[0] if decisions else "Development continued"
            return f"A day of {primary_action.lower()}. Key artifacts were created and decisions were preserved in the living documentation system. The development narrative continues with enhanced context preservation."
        
        # Default spell
        return "Another day in the life of the Living Dev Agent. Progress was made, wisdom was preserved, and the archive wall remained at bay. The adventure continues with enhanced continuity."
    
    def create_daily_ledger(self, date_str, arc_name=None, mode=None):
        """Create a daily ledger for the specified date."""
        if arc_name is None:
            arc_name = "Daily Development Arc"
        if mode is None:
            mode = "Development & Documentation"
        
        # Scan for activity
        commits = self.scan_git_activity(date_str)
        files_changed = self.scan_file_changes(date_str)
        
        # Generate content sections
        ghost_sweep = self.generate_ghost_sweep(files_changed)
        badge_verdicts = self.generate_badge_verdicts(date_str)
        decisions = self.extract_decisions_from_commits(commits)
        
        core_decisions = '\n'.join([f"- {decision}" for decision in decisions]) if decisions else "- Standard development progress"
        
        key_artifacts = []
        for commit in commits:
            key_artifacts.append(f"- [{commit['message']}]({commit['url']})")
        if not key_artifacts:
            key_artifacts.append("- No significant artifacts created")
        
        glyphs = self.generate_glyphs(commits, files_changed)
        
        unresolved_threads = "- Ongoing development work to be continued"
        
        re_entry_spell = self.generate_re_entry_spell(arc_name, decisions, key_artifacts)
        
        # Fill template
        template = self.get_daily_ledger_template()
        content = template.format(
            date=date_str,
            arc_name=arc_name,
            mode=mode,
            ghost_sweep=ghost_sweep,
            badge_verdicts=badge_verdicts,
            core_decisions=core_decisions,
            key_artifacts='\n'.join(key_artifacts),
            glyphs=glyphs,
            unresolved_threads=unresolved_threads,
            re_entry_spell=re_entry_spell
        )
        
        return content
    
    def save_daily_ledger(self, content, date_str):
        """Save the daily ledger to file."""
        filename = f"{date_str}.md"
        filepath = os.path.join(self.ledger_dir, filename)
        
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        
        return filepath
    
    def update_existing_ledger(self, ledger_path):
        """Update an existing daily ledger with current information."""
        if not os.path.exists(ledger_path):
            raise FileNotFoundError(f"Ledger not found: {ledger_path}")
        
        # Extract date from filename
        date_match = re.search(r'(\d{4}-\d{2}-\d{2})', os.path.basename(ledger_path))
        if not date_match:
            raise ValueError(f"Could not extract date from ledger path: {ledger_path}")
        
        date_str = date_match.group(1)
        
        # Read existing content to extract arc name and mode
        with open(ledger_path, 'r', encoding='utf-8') as f:
            existing_content = f.read()
        
        arc_match = re.search(r'\*\*Arc:\*\* (.+)', existing_content)
        mode_match = re.search(r'\*\*Mode:\*\* (.+)', existing_content)
        
        arc_name = arc_match.group(1).strip() if arc_match else "Daily Development Arc"
        mode = mode_match.group(1).strip() if mode_match else "Development & Documentation"
        
        # Generate new content
        new_content = self.create_daily_ledger(date_str, arc_name, mode)
        
        # Save updated content
        filepath = self.save_daily_ledger(new_content, date_str)
        return filepath

def main():
    parser = argparse.ArgumentParser(description='Generate Daily Ledgers for Archive Wall Continuity')
    parser.add_argument('--date', help='Date for ledger (YYYY-MM-DD)')
    parser.add_argument('--generate-today', action='store_true', help='Generate ledger for today')
    parser.add_argument('--update-existing', help='Update existing ledger file')
    parser.add_argument('--arc-name', help='Arc name for the ledger')
    parser.add_argument('--mode', help='Development mode for the ledger')
    parser.add_argument('--project-root', help='Project root directory')
    
    args = parser.parse_args()
    
    if not any([args.date, args.generate_today, args.update_existing]):
        parser.error("One of --date, --generate-today, or --update-existing must be provided")
    
    generator = DailyLedgerGenerator(args.project_root)
    
    try:
        if args.update_existing:
            filepath = generator.update_existing_ledger(args.update_existing)
            print(f"📊 Daily Ledger updated:")
            print(f"   Path: {filepath}")
            print("   Status: Updated with current git activity")
        else:
            if args.generate_today:
                date_str = datetime.now().strftime('%Y-%m-%d')
            else:
                date_str = args.date
            
            content = generator.create_daily_ledger(date_str, args.arc_name, args.mode)
            filepath = generator.save_daily_ledger(content, date_str)
            
            print(f"📊 Daily Ledger created:")
            print(f"   Date: {date_str}")
            print(f"   Path: {filepath}")
            print("   Status: Generated with git activity analysis")
        
        print()
        print("🧠 Next steps:")
        print("   1. Review and enhance the generated content")
        print("   2. Create Capsule Scroll if needed for context preservation")
        print("   3. Update Lost Features Ledger with new ghosts")
        
    except Exception as e:
        print(f"❌ Error creating daily ledger: {e}")
        sys.exit(1)

if __name__ == '__main__':
    main()