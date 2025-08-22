#!/usr/bin/env python3
"""
Capsule Scroll Generator - Archive Wall Continuity System
Creates conversation context anchors for preserving development wisdom.

Usage:
    python3 capsule_scroll_generator.py --arc-name "Feature Development Arc" --start-date "2025-08-18"
    python3 capsule_scroll_generator.py --from-daily-ledger docs/daily-ledger/2025-08-18.md
"""

import argparse
import os
import sys
import yaml
from datetime import datetime, timedelta
import re
import json

class CapsuleScrollGenerator:
    def __init__(self, project_root=None):
        if project_root is None:
            project_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
        self.project_root = project_root
        self.capsules_dir = os.path.join(project_root, 'capsules')
        self.templates_dir = os.path.join(self.capsules_dir, 'templates')
        self.active_dir = os.path.join(self.capsules_dir, 'active')
        self.archived_dir = os.path.join(self.capsules_dir, 'archived')
        
        # Ensure directories exist
        for directory in [self.capsules_dir, self.templates_dir, self.active_dir, self.archived_dir]:
            os.makedirs(directory, exist_ok=True)
    
    def create_capsule_id(self, arc_name, date_str=None):
        """Generate a unique capsule ID."""
        if date_str is None:
            date_str = datetime.now().strftime('%Y-%m-%d')
        slug = re.sub(r'[^a-zA-Z0-9]+', '-', arc_name.lower()).strip('-')[:30]
        return f"CAPS-{date_str}-{slug}"
    
    def load_template(self):
        """Load the capsule scroll template."""
        template_path = os.path.join(self.templates_dir, 'capsule-scroll-template.md')
        if not os.path.exists(template_path):
            raise FileNotFoundError(f"Template not found: {template_path}")
        
        with open(template_path, 'r', encoding='utf-8') as f:
            return f.read()
    
    def extract_from_daily_ledger(self, ledger_path):
        """Extract capsule scroll data from a daily ledger."""
        if not os.path.exists(ledger_path):
            raise FileNotFoundError(f"Daily ledger not found: {ledger_path}")
        
        with open(ledger_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Extract metadata from the daily ledger
        data = {
            'arc_name': 'Daily Development Arc',
            'start_date': '',
            'end_date': '',
            'status': 'ongoing',
            'core_decisions': [],
            'artifacts': [],
            'glyphs': [],
            'unresolved_threads': [],
            're_entry_spell': ''
        }
        
        # Parse the daily ledger sections
        if '**Arc:** ' in content:
            arc_match = re.search(r'\*\*Arc:\*\* (.+)', content)
            if arc_match:
                data['arc_name'] = arc_match.group(1).strip()
        
        # Extract date from filename or content
        date_match = re.search(r'(\d{4}-\d{2}-\d{2})', ledger_path)
        if date_match:
            data['start_date'] = date_match.group(1)
            data['end_date'] = date_match.group(1)
        
        # Extract core decisions from "Core prompts / decisions today"
        decisions_match = re.search(r'\*\*Core prompts / decisions today:\*\*\s*\n(.*?)\n\n', content, re.DOTALL)
        if decisions_match:
            decisions_text = decisions_match.group(1)
            for line in decisions_text.split('\n'):
                if line.strip().startswith('-'):
                    decision = line.strip()[1:].strip()
                    data['core_decisions'].append({
                        'title': decision[:50] + '...' if len(decision) > 50 else decision,
                        'context': 'From daily ledger',
                        'outcome': decision,
                        'rationale': 'Daily development decision',
                        'impact': 'To be determined'
                    })
        
        # Extract artifacts from "Key Artifacts & Commits"
        artifacts_match = re.search(r'\*\*Key Artifacts & Commits:\*\*\s*\n(.*?)\n\n', content, re.DOTALL)
        if artifacts_match:
            artifacts_text = artifacts_match.group(1)
            for line in artifacts_text.split('\n'):
                if line.strip().startswith('-') and '[' in line and '](' in line:
                    data['artifacts'].append(line.strip()[1:].strip())
        
        # Extract glyphs from "Glyphs / Running Jokes"
        glyphs_match = re.search(r'\*\*Glyphs / Running Jokes:\*\*\s*\n(.*?)\n\n', content, re.DOTALL)
        if glyphs_match:
            glyphs_text = glyphs_match.group(1)
            for line in glyphs_text.split('\n'):
                if line.strip().startswith('-'):
                    glyph = line.strip()[1:].strip()
                    data['glyphs'].append({
                        'term': glyph.replace('"', ''),
                        'definition': 'Cultural reference from daily development'
                    })
        
        # Extract unresolved threads
        unresolved_match = re.search(r'\*\*Unresolved Threads:\*\*\s*\n(.*?)\n\n', content, re.DOTALL)
        if unresolved_match:
            unresolved_text = unresolved_match.group(1)
            for line in unresolved_text.split('\n'):
                if line.strip().startswith('-'):
                    thread = line.strip()[1:].strip()
                    data['unresolved_threads'].append({
                        'type': 'Future Work',
                        'item': thread,
                        'description': 'Carried forward from daily ledger'
                    })
        
        # Extract re-entry spell
        spell_match = re.search(r'### \*\*4\. Re‑entry Spell\*\*\s*\n> \*(.*?)\*', content, re.DOTALL)
        if spell_match:
            data['re_entry_spell'] = spell_match.group(1).strip()
        
        return data
    
    def generate_capsule_scroll(self, data):
        """Generate a capsule scroll from the provided data."""
        template = self.load_template()
        timestamp = datetime.now().isoformat()
        capsule_id = self.create_capsule_id(data['arc_name'], data.get('start_date'))
        
        # Replace template variables
        replacements = {
            '{ARC_NAME}': data['arc_name'],
            '{YYYY-MM-DD}': data.get('start_date', datetime.now().strftime('%Y-%m-%d')),
            '{SLUG}': re.sub(r'[^a-zA-Z0-9]+', '-', data['arc_name'].lower()).strip('-')[:30],
            '{TIMESTAMP}': timestamp,
            '{STATUS}': data.get('status', 'active'),
            '{START_DATE}': data.get('start_date', 'TBD'),
            '{END_DATE}': data.get('end_date', 'ongoing'),
            '{DURATION}': self.calculate_duration(data.get('start_date'), data.get('end_date')),
            '{THREE_SENTENCE_CONTEXT_RESET}': data.get('re_entry_spell', 'Context to be determined based on development arc progression. Key decisions and artifacts will guide future conversations. The adventure continues with preserved wisdom.'),
            '{TONE_KEYWORD_1}': 'collaborative',
            '{TONE_KEYWORD_2}': 'technical',
            '{TONE_KEYWORD_3}': 'iterative',
            '{TRIGGER_TYPE}': data.get('trigger_type', 'manual'),
            '{ISSUE_NUMBERS}': str(data.get('issue_numbers', [])),
            '{PR_NUMBERS}': str(data.get('pr_numbers', [])),
            '{TLDL_IDS}': str(data.get('tldl_ids', [])),
            '{UNRESOLVED_COUNT}': str(len(data.get('unresolved_threads', []))),
            '{VERSION}': '1.0.0'
        }
        
        content = template
        for placeholder, value in replacements.items():
            content = content.replace(placeholder, value)
        
        # Replace decision sections
        if data.get('core_decisions'):
            decisions_content = ""
            for i, decision in enumerate(data['core_decisions'][:3], 1):  # Limit to 3 decisions
                decisions_content += f"""### Decision {i}: {decision.get('title', 'Unnamed Decision')}
- **Context**: {decision.get('context', 'No context provided')}
- **Choice Made**: {decision.get('outcome', 'No outcome recorded')}
- **Rationale**: {decision.get('rationale', 'No rationale provided')}
- **Impact**: {decision.get('impact', 'Impact to be determined')}

"""
            # Replace the template decisions
            decision_pattern = r'### Decision 1:.*?(?=### Decision 2:)|### Decision 2:.*?(?=\*Add additional)'
            content = re.sub(decision_pattern, '', content, flags=re.DOTALL)
            content = content.replace('## 🎯 Core Decisions\n\n', f'## 🎯 Core Decisions\n\n{decisions_content}')
        
        # Replace artifacts sections
        if data.get('artifacts'):
            artifacts_content = "### Key References\n"
            for artifact in data['artifacts'][:5]:  # Limit to 5 artifacts
                artifacts_content += f"- {artifact}\n"
            artifacts_content += "\n"
            
            # Replace template artifacts
            artifact_pattern = r'### Issues.*?### TLDL Entries.*?\n\n'
            content = re.sub(artifact_pattern, artifacts_content, content, flags=re.DOTALL)
        
        # Replace glyphs section
        if data.get('glyphs'):
            glyphs_content = ""
            for glyph in data['glyphs'][:5]:  # Limit to 5 glyphs
                glyphs_content += f"- **{glyph.get('term', 'Unknown')}**: {glyph.get('definition', 'No definition provided')}\n"
            
            glyph_pattern = r'### Terminology.*?### Running Jokes.*?\n\n'
            content = re.sub(glyph_pattern, f"### Key Terms & References\n{glyphs_content}\n", content, flags=re.DOTALL)
        
        # Replace unresolved threads
        if data.get('unresolved_threads'):
            threads_content = ""
            for thread in data['unresolved_threads']:
                threads_content += f"- [ ] {thread.get('item', 'Unknown item')} - {thread.get('description', 'No description')}\n"
            
            threads_pattern = r'### Technical Debt.*?### Future Work.*?\n\n'
            content = content.replace('{{CORE_DECISIONS}}', decisions_content)
        
        # Replace artifacts section using placeholder
        if data.get('artifacts'):
            artifacts_content = "### Key References\n"
            for artifact in data['artifacts'][:5]:  # Limit to 5 artifacts
                artifacts_content += f"- {artifact}\n"
            artifacts_content += "\n"
            content = content.replace('{{ARTIFACTS}}', artifacts_content)
        
        # Replace glyphs section using placeholder
        if data.get('glyphs'):
            glyphs_content = ""
            for glyph in data['glyphs'][:5]:  # Limit to 5 glyphs
                glyphs_content += f"- **{glyph.get('term', 'Unknown')}**: {glyph.get('definition', 'No definition provided')}\n"
            content = content.replace('{{GLYPHS}}', f"### Key Terms & References\n{glyphs_content}\n")
        
        # Replace unresolved threads section using placeholder
        if data.get('unresolved_threads'):
            threads_content = ""
            for thread in data['unresolved_threads']:
                threads_content += f"- [ ] {thread.get('item', 'Unknown item')} - {thread.get('description', 'No description')}\n"
            content = content.replace('{{UNRESOLVED_THREADS}}', f"### Carried Forward\n{threads_content}\n")
        
        return content, capsule_id
    
    def calculate_duration(self, start_date, end_date):
        """Calculate duration between dates."""
        if not start_date or not end_date or end_date == 'ongoing':
            return 'ongoing'
        try:
            start = datetime.strptime(start_date, '%Y-%m-%d')
            end = datetime.strptime(end_date, '%Y-%m-%d')
            delta = end - start
            return f"{delta.days} days"
        except:
            return 'unknown'
    
    def save_capsule_scroll(self, content, capsule_id, status='active'):
        """Save the capsule scroll to the appropriate directory."""
        directory = self.active_dir if status == 'active' else self.archived_dir
        filename = f"{capsule_id}.md"
        filepath = os.path.join(directory, filename)
        
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        
        return filepath
    
    def create_from_daily_ledger(self, ledger_path):
        """Create a capsule scroll from a daily ledger."""
        data = self.extract_from_daily_ledger(ledger_path)
        content, capsule_id = self.generate_capsule_scroll(data)
        filepath = self.save_capsule_scroll(content, capsule_id)
        return filepath, capsule_id
    
    def create_manual(self, arc_name, start_date=None, end_date=None, status='active'):
        """Create a manual capsule scroll with basic information."""
        data = {
            'arc_name': arc_name,
            'start_date': start_date or datetime.now().strftime('%Y-%m-%d'),
            'end_date': end_date or 'ongoing',
            'status': status,
            'core_decisions': [],
            'artifacts': [],
            'glyphs': [],
            'unresolved_threads': [],
            're_entry_spell': f'Beginning of {arc_name} development arc. Initial context and decisions will be captured as the work progresses. The adventure starts here.'
        }
        
        content, capsule_id = self.generate_capsule_scroll(data)
        filepath = self.save_capsule_scroll(content, capsule_id, status)
        return filepath, capsule_id

def main():
    parser = argparse.ArgumentParser(description='Generate Capsule Scrolls for Archive Wall Continuity')
    parser.add_argument('--arc-name', help='Name of the development arc')
    parser.add_argument('--start-date', help='Start date (YYYY-MM-DD)')
    parser.add_argument('--end-date', help='End date (YYYY-MM-DD)')
    parser.add_argument('--status', choices=['active', 'archived'], default='active', help='Capsule status')
    parser.add_argument('--from-daily-ledger', help='Path to daily ledger to extract from')
    parser.add_argument('--project-root', help='Project root directory')
    
    args = parser.parse_args()
    
    if not args.arc_name and not args.from_daily_ledger:
        parser.error("Either --arc-name or --from-daily-ledger must be provided")
    
    generator = CapsuleScrollGenerator(args.project_root)
    
    try:
        if args.from_daily_ledger:
            filepath, capsule_id = generator.create_from_daily_ledger(args.from_daily_ledger)
            print(f"📜 Capsule Scroll created from daily ledger:")
        else:
            filepath, capsule_id = generator.create_manual(args.arc_name, args.start_date, args.end_date, args.status)
            print(f"📜 Manual Capsule Scroll created:")
        
        print(f"   ID: {capsule_id}")
        print(f"   Path: {filepath}")
        print(f"   Status: {args.status}")
        print()
        print("🧠 Next steps:")
        print("   1. Edit the capsule scroll to add specific context")
        print("   2. Link from TLDL entries for discoverability")
        print("   3. Reference in daily ledger for continuity")
        
    except Exception as e:
        print(f"❌ Error creating capsule scroll: {e}")
        sys.exit(1)

if __name__ == '__main__':
    main()