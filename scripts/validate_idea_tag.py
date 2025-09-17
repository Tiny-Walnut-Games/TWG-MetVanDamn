#!/usr/bin/env python3
"""
Living Dev Agent Template - IDEA Tag Validator
Jerry's legendary validation system for IDEA-tagged TLDL entries

Execution time: ~80ms for typical validation runs
Validates IDEA-tagged TLDL entries for charter completeness and structure
"""

import argparse
import os
import sys
import re
import datetime
from pathlib import Path
from typing import List, Dict, Any, Optional, Tuple

# Color codes for legendary terminal output
class Colors:
    HEADER = '\033[95m'
    OKBLUE = '\033[94m'
    OKCYAN = '\033[96m'
    OKGREEN = '\033[92m'
    WARNING = '\033[93m'
    FAIL = '\033[91m'
    ENDC = '\033[0m'
    BOLD = '\033[1m'
    UNDERLINE = '\033[4m'

# Sacred emojis for maximum scroll-worthiness
EMOJI_SUCCESS = "✅"
EMOJI_WARNING = "⚠️"
EMOJI_ERROR = "❌"
EMOJI_INFO = "🔍"
EMOJI_MAGIC = "🧙‍♂️"
EMOJI_SCROLL = "📜"
EMOJI_IDEA = "💡"

class IdeaTagValidator:
    """The Bootstrap Sentinel's sacred IDEA tag validation system"""
    
    def __init__(self, tldl_path: str, verbose: bool = False):
        self.tldl_path = Path(tldl_path)
        self.verbose = verbose
        self.errors: List[str] = []
        self.warnings: List[str] = []
        self.validated_files: List[str] = []
        self.validation_start_time = datetime.datetime.now()
        
        # Required charter fields for IDEA-tagged entries
        self.required_charter_fields = [
            "Problem",
            "Why Now", 
            "Risk of Ignoring",
            "1-Sentence Pitch",
            "Synergy",
            "Integrity Boost Mechanism",
            "Prototype Shape",
            "Kill Criteria"
        ]

    def log_message(self, level: str, message: str, emoji: str = ""):
        """Sacred scroll logging with maximum visibility"""
        timestamp = datetime.datetime.now().strftime("%H:%M:%S")
        color = {
            'SUCCESS': Colors.OKGREEN,
            'WARNING': Colors.WARNING,
            'ERROR': Colors.FAIL,
            'INFO': Colors.OKBLUE
        }.get(level, Colors.ENDC)
        
        formatted_message = f"{color}{emoji} [{timestamp}] {message}{Colors.ENDC}"
        print(formatted_message)
        
        if self.verbose:
            print(f"    Context: IDEA Tag Validation - {level}")

    def is_idea_tagged_entry(self, content: str, filename: str) -> bool:
        """Check if TLDL entry has IDEA tag in title or content"""
        # Check for IDEA in title
        title_pattern = r'^#.*IDEA'
        title_match = re.search(title_pattern, content, re.MULTILINE | re.IGNORECASE)
        
        # Check for IDEA emoji in title
        emoji_pattern = r'^#.*💡'
        emoji_match = re.search(emoji_pattern, content, re.MULTILINE)
        
        # Check for IDEA tag in metadata
        metadata_pattern = r'Tags.*#idea'
        metadata_match = re.search(metadata_pattern, content, re.IGNORECASE)
        
        return bool(title_match or emoji_match or metadata_match)

    def find_charter_reference(self, content: str) -> Optional[str]:
        """Find charter reference in TLDL content"""
        # Look for charter reference patterns
        patterns = [
            r'Charter Reference.*?:\s*\[.*?\]\s*\((.+?)\)',  # Markdown link format
            r'Charter Reference.*?:\s*(.+)',
            r'Idea Charter.*?:\s*(.+)',
            r'\[Charter\]\s*\((.+?)\)',
            r'Charter.*?:\s*\[.*?\]\s*\((.+?)\)'
        ]
        
        for pattern in patterns:
            match = re.search(pattern, content, re.IGNORECASE)
            if match:
                ref = match.group(1).strip()
                # Clean up markdown link format if present
                if ref.startswith('[') and '](' in ref:
                    ref = ref.split('](')[1].rstrip(')')
                return ref
        
        return None

    def validate_charter_content(self, charter_content: str, charter_path: str) -> List[str]:
        """Validate that charter contains all required fields"""
        missing_fields = []
        
        for field in self.required_charter_fields:
            # Check for section headers with various formats
            patterns = [
                f"## [🎯⏰🔥🚀🔗⚡🛠️💀]*\\s*{re.escape(field)}",
                f"### [🎯⏰🔥🚀🔗⚡🛠️💀]*\\s*{re.escape(field)}",
                f"\\*\\*{re.escape(field)}\\*\\*",
                f"{re.escape(field)}:"
            ]
            
            found = False
            for pattern in patterns:
                if re.search(pattern, charter_content, re.IGNORECASE):
                    found = True
                    break
            
            if not found:
                missing_fields.append(field)
        
        return missing_fields

    def validate_charter_file(self, charter_path: str) -> Tuple[bool, List[str]]:
        """Validate charter file exists and contains required content"""
        errors = []
        
        if not os.path.exists(charter_path):
            errors.append(f"Charter file not found: {charter_path}")
            return False, errors
        
        try:
            with open(charter_path, 'r', encoding='utf-8') as f:
                charter_content = f.read()
        except Exception as e:
            errors.append(f"Failed to read charter file {charter_path}: {str(e)}")
            return False, errors
        
        # Validate charter contains required fields
        missing_fields = self.validate_charter_content(charter_content, charter_path)
        if missing_fields:
            errors.append(f"Charter missing required fields: {', '.join(missing_fields)}")
        
        # Validate charter has proper ID format
        if not re.search(r'IDEA-\d{4}-\d{2}-\d{2}-', charter_content):
            errors.append("Charter missing proper ID format (IDEA-YYYY-MM-DD-Title)")
        
        return len(errors) == 0, errors

    def validate_idea_entry(self, file_path: Path) -> bool:
        """Validate a single IDEA-tagged TLDL entry"""
        entry_valid = True
        
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
        except Exception as e:
            self.errors.append(f"Failed to read {file_path}: {str(e)}")
            return False
        
        # Check if this is an IDEA-tagged entry
        if not self.is_idea_tagged_entry(content, str(file_path)):
            return True  # Not an IDEA entry, no validation needed
        
        self.log_message('INFO', f"Validating IDEA-tagged entry: {file_path.name}", EMOJI_IDEA)
        
        # Find charter reference
        charter_ref = self.find_charter_reference(content)
        if not charter_ref:
            error_msg = f"IDEA entry missing charter reference: {file_path.name}"
            self.errors.append(error_msg)
            self.log_message('ERROR', error_msg, EMOJI_ERROR)
            entry_valid = False
        else:
            # Resolve charter path (handle relative paths)
            if charter_ref.startswith('http'):
                self.log_message('WARNING', f"External charter reference (not validated): {charter_ref}", EMOJI_WARNING)
            else:
                # Handle relative path resolution
                if not charter_ref.startswith('/'):
                    charter_path = file_path.parent.parent / charter_ref
                else:
                    charter_path = Path(charter_ref)
                
                # Validate charter file
                charter_valid, charter_errors = self.validate_charter_file(str(charter_path))
                if not charter_valid:
                    for error in charter_errors:
                        self.errors.append(f"Charter validation failed for {file_path.name}: {error}")
                        self.log_message('ERROR', error, EMOJI_ERROR)
                    entry_valid = False
                else:
                    self.log_message('SUCCESS', f"Charter validation passed: {charter_path.name}", EMOJI_SUCCESS)
        
        # Additional IDEA entry structure validation
        required_sections = ['Objective', 'Discovery', 'Actions Taken']
        for section in required_sections:
            if not re.search(f"##[#]?\\s*[🎯🔍⚡]*\\s*{section}", content, re.IGNORECASE):
                warning_msg = f"IDEA entry {file_path.name} missing recommended section: {section}"
                self.warnings.append(warning_msg)
                self.log_message('WARNING', warning_msg, EMOJI_WARNING)
        
        return entry_valid

    def validate_all_entries(self) -> bool:
        """Validate all TLDL entries for IDEA tag compliance"""
        self.log_message('INFO', f"Starting IDEA tag validation in: {self.tldl_path}", EMOJI_MAGIC)
        
        if not self.tldl_path.exists():
            error_msg = f"TLDL path does not exist: {self.tldl_path}"
            self.errors.append(error_msg)
            self.log_message('ERROR', error_msg, EMOJI_ERROR)
            return False
        
        # Find all TLDL entry files
        entry_files = list(self.tldl_path.glob("**/*.md"))
        if not entry_files:
            self.log_message('WARNING', "No TLDL entries found to validate", EMOJI_WARNING)
            return True
        
        all_valid = True
        idea_entries_found = 0
        
        for entry_file in entry_files:
            self.validated_files.append(str(entry_file))
            if not self.validate_idea_entry(entry_file):
                all_valid = False
            
            # Count IDEA entries for statistics
            try:
                with open(entry_file, 'r', encoding='utf-8') as f:
                    content = f.read()
                if self.is_idea_tagged_entry(content, str(entry_file)):
                    idea_entries_found += 1
            except:
                pass
        
        # Validation summary
        elapsed_time = datetime.datetime.now() - self.validation_start_time
        self.log_message('INFO', f"Validation completed in {elapsed_time.total_seconds():.3f}s", EMOJI_SCROLL)
        self.log_message('INFO', f"Total files validated: {len(entry_files)}", EMOJI_INFO)
        self.log_message('INFO', f"IDEA-tagged entries found: {idea_entries_found}", EMOJI_IDEA)
        
        if all_valid:
            self.log_message('SUCCESS', "All IDEA-tagged entries passed validation!", EMOJI_SUCCESS)
        else:
            self.log_message('ERROR', f"Validation failed with {len(self.errors)} errors", EMOJI_ERROR)
        
        return all_valid

    def print_summary(self):
        """Print validation summary with scroll-worthy formatting"""
        print(f"\n{Colors.HEADER}{'='*60}{Colors.ENDC}")
        print(f"{Colors.HEADER}{EMOJI_MAGIC} IDEA Tag Validation Summary {EMOJI_SCROLL}{Colors.ENDC}")
        print(f"{Colors.HEADER}{'='*60}{Colors.ENDC}")
        
        print(f"\n{Colors.OKBLUE}Files Validated:{Colors.ENDC} {len(self.validated_files)}")
        print(f"{Colors.OKGREEN}Errors:{Colors.ENDC} {len(self.errors)}")
        print(f"{Colors.WARNING}Warnings:{Colors.ENDC} {len(self.warnings)}")
        
        if self.errors:
            print(f"\n{Colors.FAIL}{EMOJI_ERROR} Errors:{Colors.ENDC}")
            for error in self.errors:
                print(f"  • {error}")
        
        if self.warnings:
            print(f"\n{Colors.WARNING}{EMOJI_WARNING} Warnings:{Colors.ENDC}")
            for warning in self.warnings:
                print(f"  • {warning}")
        
        elapsed_time = datetime.datetime.now() - self.validation_start_time
        print(f"\n{Colors.OKCYAN}Validation completed in {elapsed_time.total_seconds():.3f} seconds{Colors.ENDC}")

def main():
    """Sacred entry point for IDEA tag validation"""
    parser = argparse.ArgumentParser(
        description="IDEA Tag Validator - Ensures IDEA-tagged TLDL entries have complete charters",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Validate TLDL entries directory
  python3 validate_idea_tag.py --tldl-path TLDL/entries/
  
  # Verbose validation output
  python3 validate_idea_tag.py --tldl-path TLDL/entries/ --verbose
        """
    )
    
    parser.add_argument(
        '--tldl-path',
        type=str,
        required=True,
        help='Path to TLDL entries directory'
    )
    
    parser.add_argument(
        '--verbose',
        action='store_true',
        help='Enable verbose output for debugging'
    )
    
    args = parser.parse_args()
    
    # Initialize the legendary validator
    validator = IdeaTagValidator(
        tldl_path=args.tldl_path,
        verbose=args.verbose
    )
    
    # Perform the sacred validation ritual
    try:
        validation_passed = validator.validate_all_entries()
        validator.print_summary()
        
        # Exit with appropriate code for CI integration
        sys.exit(0 if validation_passed else 1)
        
    except KeyboardInterrupt:
        print(f"\n{Colors.WARNING}⚠️  Validation interrupted by user{Colors.ENDC}")
        sys.exit(130)
    except Exception as e:
        print(f"\n{Colors.FAIL}❌ Validation failed with unexpected error: {str(e)}{Colors.ENDC}")
        sys.exit(1)

if __name__ == '__main__':
    main()