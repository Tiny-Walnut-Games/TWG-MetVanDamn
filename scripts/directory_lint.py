#!/usr/bin/env python3
"""
Directory lint: Enforce TLDL vs Docs architecture boundaries
Ensures time-stamped TLDL entries stay in TLDL/entries/ and evergreen docs stay in docs/
"""

import os
import sys
import re
from pathlib import Path

def lint_directory_structure():
    """Lint the repository directory structure for architecture compliance"""
    
    project_root = Path(__file__).parent.parent
    errors = []
    warnings = []
    
    # Check for TLDL files in docs/ (should be empty now, except for TLDL-Archive/)
    docs_dir = project_root / "docs"
    if docs_dir.exists():
        tldl_pattern = re.compile(r'^TLDL-\d{4}-\d{2}-\d{2}-.+\.md$')
        for file in docs_dir.rglob("*.md"):
            # Skip files in TLDL-Archive directory (these are expected)
            if "TLDL-Archive" in str(file):
                continue
            if tldl_pattern.match(file.name):
                errors.append(f"❌ TLDL entry found in docs/: {file.relative_to(project_root)}")
                errors.append(f"   → Should be in TLDL/entries/ for time-bound narratives")
    
    # Check for non-TLDL files in TLDL/entries/
    tldl_entries_dir = project_root / "TLDL" / "entries"
    if tldl_entries_dir.exists():
        for file in tldl_entries_dir.rglob("*.md"):
            # Allow TLDL-YYYY-MM-DD-*.md and comment/lore entries like 2025-08-18-comment-*.md
            if not (re.match(r'^TLDL-\d{4}-\d{2}-\d{2}-.+\.md$', file.name) or 
                   re.match(r'^\d{4}-\d{2}-\d{2}-.+\.md$', file.name)):
                warnings.append(f"⚠️  Non-TLDL file in entries/: {file.relative_to(project_root)}")
                warnings.append(f"   → Consider if this belongs in docs/ as evergreen content")
    
    # Check for templates in wrong location
    templates_pattern = re.compile(r'.*(template|example|test).*', re.IGNORECASE)
    for file in tldl_entries_dir.rglob("*.md") if tldl_entries_dir.exists() else []:
        if templates_pattern.search(file.name):
            warnings.append(f"⚠️  Template/test file in entries/: {file.relative_to(project_root)}")
            warnings.append(f"   → Consider moving to templates/ directory")
    
    # Report results
    print("=== Directory Structure Lint Report ===")
    print(f"Checked: docs/ and TLDL/entries/ directories")
    
    if errors:
        print(f"\n❌ ERRORS ({len(errors)//2}):")
        for error in errors:
            print(f"  {error}")
        
    if warnings:
        print(f"\n⚠️  WARNINGS ({len(warnings)//2}):")
        for warning in warnings:
            print(f"  {warning}")
    
    if not errors and not warnings:
        print("\n✅ Directory structure is compliant!")
        print("  • TLDL entries properly located in TLDL/entries/")
        print("  • Evergreen docs properly located in docs/")
    
    # Return exit code
    return 1 if errors else 0

if __name__ == "__main__":
    exit_code = lint_directory_structure()
    sys.exit(exit_code)