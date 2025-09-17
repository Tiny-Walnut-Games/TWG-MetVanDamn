#!/usr/bin/env python3
"""
Living Dev Agent Template - TLDL Documentation Validator
Jerry's legendary validation system for structured development documentation

Execution time: ~60ms for typical validation runs
Validates TLDL entries for completeness and adherence to template standards
"""

import argparse
import os
import sys
import yaml
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

class TLDLValidator:
    """The Bootstrap Sentinel's sacred TLDL validation system"""
    
    def __init__(self, tldl_path: str, config_path: str = None, verbose: bool = False):
        self.tldl_path = Path(tldl_path)
        self.config_path = Path(config_path) if config_path else None
        self.verbose = verbose
        self.errors: List[str] = []
        self.warnings: List[str] = []
        self.validated_files: List[str] = []
        self.validation_start_time = datetime.datetime.now()
        
        # TLDL entry requirements
        self.required_sections = [
            "Objective",
            "Discovery", 
            "Actions Taken",
            "Key Insights",
            "Next Steps"
        ]
        
        # Section alias mapping to support existing entry formats
        self.section_aliases = {
            "Objective": ["Objective", "Purpose", "Goals", "Faculty Consultation Results", "Executive Summary", "DevTimeTravel Context"],
            "Discovery": ["Discovery", "Discoveries"],
            "Key Insights": ["Key Insights", "Lessons Learned", "Implementation Insights", "Learning Outcomes"]
        }
        
        self.required_metadata = [
            "Entry ID",
            "Author",
            "Context", 
            "Summary"
        ]

    def log_info(self, message: str, emoji: str = EMOJI_INFO):
        """Log informational message with epic styling"""
        print(f"{Colors.OKCYAN}{emoji} [INFO]{Colors.ENDC} {message}")

    def log_success(self, message: str, emoji: str = EMOJI_SUCCESS):
        """Log success message with legendary flair"""
        print(f"{Colors.OKGREEN}{emoji} [SUCCESS]{Colors.ENDC} {message}")

    def log_warning(self, message: str, emoji: str = EMOJI_WARNING):
        """Log warning message"""
        print(f"{Colors.WARNING}{emoji} [WARNING]{Colors.ENDC} {message}")
        self.warnings.append(message)

    def log_error(self, message: str, emoji: str = EMOJI_ERROR):
        """Log error message"""
        print(f"{Colors.FAIL}{emoji} [ERROR]{Colors.ENDC} {message}")
        self.errors.append(message)

    def verbose_log(self, message: str):
        """Log verbose information when enabled"""
        if self.verbose:
            print(f"{Colors.OKBLUE}[VERBOSE]{Colors.ENDC} {message}")

    def validate_tldl_filename(self, filepath: Path) -> bool:
        """Validate TLDL filename follows the pattern TLDL-YYYY-MM-DD-Title.md"""
        filename = filepath.name
        pattern = r'^TLDL-\d{4}-\d{2}-\d{2}-.+\.md$'
        
        if not re.match(pattern, filename):
            self.log_warning(f"Filename doesn't follow TLDL pattern: {filename}")
            self.log_info("Expected pattern: TLDL-YYYY-MM-DD-Title.md")
            return False
        
        self.verbose_log(f"Filename validation passed: {filename}")
        return True

    def extract_metadata_from_content(self, content: str) -> Dict[str, str]:
        """Extract metadata from TLDL content"""
        metadata = {}
        
        # Look for metadata in the first few lines
        lines = content.split('\n')[:20]  # Check first 20 lines for metadata
        
        for line in lines:
            line = line.strip()
            # Handle both formats: **Key:** value and **Key**: value
            if line.startswith('**') and (':**' in line or '**:' in line):
                # Extract key-value pairs like **Entry ID:** TLDL-2025-01-15-Title
                # or **Entry ID**: TLDL-2025-01-15-Title
                if ':**' in line:
                    parts = line.split(':**', 1)
                elif '**:' in line:
                    parts = line.split('**:', 1)
                else:
                    continue
                    
                if len(parts) == 2:
                    key = parts[0].strip('*').strip()
                    value = parts[1].strip()
                    metadata[key] = value
        
        return metadata

    def validate_tldl_metadata(self, filepath: Path, content: str) -> bool:
        """Validate TLDL metadata completeness"""
        metadata = self.extract_metadata_from_content(content)
        all_valid = True
        
        for required_field in self.required_metadata:
            if required_field not in metadata:
                self.log_error(f"{filepath.name}: Missing required metadata field '{required_field}'")
                all_valid = False
            elif not metadata[required_field].strip():
                self.log_error(f"{filepath.name}: Empty metadata field '{required_field}'")
                all_valid = False
            else:
                self.verbose_log(f"Metadata '{required_field}': {metadata[required_field][:50]}...")
        
        # Validate Entry ID format
        if "Entry ID" in metadata:
            entry_id = metadata["Entry ID"]
            if not re.match(r'^TLDL-\d{4}-\d{2}-\d{2}-.+', entry_id):
                self.log_warning(f"{filepath.name}: Entry ID doesn't follow expected format")
        
        return all_valid

    def validate_tldl_sections(self, filepath: Path, content: str) -> bool:
        """Validate required TLDL sections are present"""
        all_valid = True
        
        for section in self.required_sections:
            # Get acceptable aliases for this section
            acceptable_variants = self.section_aliases.get(section, [section])
            
            found = False
            found_variant = None
            
            # Check for each acceptable variant
            for variant in acceptable_variants:
                # Look for section headers (various markdown formats)
                patterns = [
                    f"## {variant}",
                    f"### {variant}", 
                    f"#### {variant}",
                    f"## 🎯 {variant}",
                    f"## 🔍 {variant}",
                    f"## ⚡ {variant}",
                    f"## 🧠 {variant}",
                    f"## 📋 {variant}",
                    f"## 💡 {variant}"
                ]
                
                if any(pattern in content for pattern in patterns):
                    found = True
                    found_variant = variant
                    break
            
            if not found:
                self.log_error(f"{filepath.name}: Missing required section '{section}'")
                all_valid = False
            else:
                self.verbose_log(f"Found required section: {section} (as '{found_variant}')")
        
        return all_valid

    def validate_tldl_content_quality(self, filepath: Path, content: str) -> bool:
        """Validate TLDL content quality and completeness"""
        issues_found = False
        
        # Check for placeholder text that indicates incomplete entries
        placeholders = [
            "[Your Name]",
            "[Brief context description]", 
            "[One-line summary",
            "[What are you trying to accomplish",
            "[What did you learn",
            "[What specific steps did you take",
            "[Important insights",
            "[What obstacles did you face"
        ]
        
        for placeholder in placeholders:
            if placeholder in content:
                self.log_warning(f"{filepath.name}: Contains placeholder text: {placeholder}")
                issues_found = True
        
        # Check for minimum content length in key sections
        if len(content) < 500:
            self.log_warning(f"{filepath.name}: Entry seems quite short ({len(content)} characters)")
            issues_found = True
        
        # Check for proper markdown formatting
        if "```" in content:
            code_blocks = content.count("```")
            if code_blocks % 2 != 0:
                self.log_error(f"{filepath.name}: Unbalanced code blocks (``` count: {code_blocks})")
                issues_found = True
        
        return not issues_found

    def validate_single_tldl_file(self, filepath: Path) -> bool:
        """Validate a single TLDL file comprehensively"""
        self.verbose_log(f"Validating TLDL file: {filepath}")
        
        try:
            # Read file content
            with open(filepath, 'r', encoding='utf-8') as f:
                content = f.read()
        except Exception as e:
            self.log_error(f"Failed to read {filepath}: {e}")
            return False
        
        # Run all validations
        filename_valid = self.validate_tldl_filename(filepath)
        metadata_valid = self.validate_tldl_metadata(filepath, content)
        sections_valid = self.validate_tldl_sections(filepath, content)
        content_valid = self.validate_tldl_content_quality(filepath, content)
        
        all_valid = filename_valid and metadata_valid and sections_valid and content_valid
        
        if all_valid:
            self.log_success(f"TLDL validation passed: {filepath.name}")
        else:
            self.log_error(f"TLDL validation failed: {filepath.name}")
        
        self.validated_files.append(str(filepath))
        return all_valid

    def find_tldl_files(self) -> List[Path]:
        """Find all TLDL markdown files in the specified path"""
        tldl_files = []
        
        if not self.tldl_path.exists():
            self.log_error(f"TLDL path does not exist: {self.tldl_path}")
            return tldl_files
        
        if self.tldl_path.is_file():
            # Single file
            if self.tldl_path.suffix.lower() == '.md':
                tldl_files.append(self.tldl_path)
        else:
            # Directory - find all markdown files
            for pattern in ['*.md', '**/*.md']:
                found_files = list(self.tldl_path.glob(pattern))
                for f in found_files:
                    # Filter for TLDL files (either filename pattern or content check)
                    if (f.name.startswith('TLDL-') or 
                        'Entry ID' in f.read_text(encoding='utf-8', errors='ignore')[:1000]):
                        tldl_files.append(f)
        
        return sorted(tldl_files)

    def run_validation(self) -> bool:
        """Run complete TLDL validation suite"""
        self.log_info(f"Starting TLDL validation for: {self.tldl_path}", EMOJI_MAGIC)
        
        # Find TLDL files
        tldl_files = self.find_tldl_files()
        
        if not tldl_files:
            self.log_warning("No TLDL files found to validate")
            return True
        
        self.log_info(f"Found {len(tldl_files)} TLDL files to validate")
        
        # Validate each file
        all_valid = True
        for tldl_file in tldl_files:
            file_valid = self.validate_single_tldl_file(tldl_file)
            all_valid = all_valid and file_valid
        
        # Show summary
        execution_time = (datetime.datetime.now() - self.validation_start_time).total_seconds()
        
        print("\n" + "="*60)
        self.log_info(f"TLDL Validation Summary", EMOJI_SCROLL)
        print(f"Files validated: {len(self.validated_files)}")
        print(f"Warnings: {len(self.warnings)}")
        print(f"Errors: {len(self.errors)}")
        print(f"Execution time: {execution_time:.3f}s")
        
        if all_valid:
            self.log_success("All TLDL validations passed!", EMOJI_MAGIC)
        else:
            self.log_error("TLDL validation failures detected")
        
        return all_valid


def main():
    """Main execution function for TLDL validation"""
    parser = argparse.ArgumentParser(
        description=f"{EMOJI_MAGIC} Living Dev Agent TLDL Validator {EMOJI_SCROLL}",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python3 validate_docs.py --tldl-path TLDL/entries/
  python3 validate_docs.py --tldl-path docs/ --verbose
  python3 validate_docs.py --tldl-path single-file.md --config-path config/
        """
    )
    
    parser.add_argument(
        '--tldl-path',
        required=True,
        help='Path to TLDL files (directory or single file)'
    )
    
    parser.add_argument(
        '--config-path', 
        help='Path to configuration files (optional)'
    )
    
    parser.add_argument(
        '--verbose',
        action='store_true',
        help='Enable verbose output for debugging'
    )
    
    args = parser.parse_args()
    
    try:
        # Create validator and run validation
        validator = TLDLValidator(
            tldl_path=args.tldl_path,
            config_path=args.config_path,
            verbose=args.verbose
        )
        
        success = validator.run_validation()
        
        # Exit with appropriate code
        sys.exit(0 if success else 1)
        
    except KeyboardInterrupt:
        print(f"\n{Colors.WARNING}{EMOJI_WARNING} Validation interrupted by user{Colors.ENDC}")
        sys.exit(1)
    except Exception as e:
        print(f"{Colors.FAIL}{EMOJI_ERROR} Validation failed with exception: {e}{Colors.ENDC}")
        sys.exit(1)


if __name__ == "__main__":
    main()
