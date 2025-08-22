#!/usr/bin/env python3
"""
Documentation and TLDL Validator for Living Dev Agent Template
Validates TLDL entries, documentation structure, consistency, and scroll quotes.

Copyright (C) 2025 Bellok

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
"""

import builtins
import os
import re
import sys
import yaml
try:
    import argparse
except ImportError:
    print("Error: The 'argparse' module is required but not available in this Python environment.")
    exit(1)
from pathlib import Path
from typing import List, Dict, Any
from datetime import datetime

# Add ScrollQuoteEngine to path if available
try:
    sys.path.insert(0, str(Path(__file__).parent.parent))
    from ScrollQuoteEngine import ScrollQuoteEngine
    QUOTE_ENGINE_AVAILABLE = True
except ImportError:
    QUOTE_ENGINE_AVAILABLE = False

class TLDLValidator:
    def __init__(self, tldl_path: str = "docs/"):
        self.tldl_path = Path(tldl_path)
        self.errors = []
        self.warnings = []
        
    def validate_tldl_entry(self, file_path: Path) -> Dict[str, Any]:
        """Validate a single TLDL entry file"""
        result = {
            "file": str(file_path),
            "valid": True,
            "errors": [],
            "warnings": []
        }
        
        try:
            content = file_path.read_text(encoding='utf-8')
            
            # Check filename format
            if not re.match(r'TLDL-\d{4}-\d{2}-\d{2}-.+\.md$', file_path.name):
                result["warnings"].append("Filename doesn't follow TLDL-YYYY-MM-DD-Title.md format")
            
            # Check required sections
            required_sections = [
                "Entry ID:", "Author:", "Context:", "Summary:",
                "## Discoveries", "## Actions Taken", "## Next Steps"
            ]
            
            for section in required_sections:
                if section not in content:
                    result["errors"].append(f"Missing required section: {section}")
                    result["valid"] = False
            
            # Check entry ID format
            entry_id_match = re.search(r'\*\*Entry ID:\*\* (TLDL-\d{4}-\d{2}-\d{2}-.+)', content)
            if entry_id_match:
                entry_id = entry_id_match.group(1)
                if entry_id not in file_path.name:
                    result["warnings"].append("Entry ID doesn't match filename")
            else:
                result["errors"].append("Entry ID not found or malformed")
                result["valid"] = False
            
            # Check for TODO items in Next Steps
            next_steps_match = re.search(r'## Next Steps.*?(?=##|$)', content, re.DOTALL)
            if next_steps_match:
                next_steps_content = next_steps_match.group(0)
                todo_count = len(re.findall(r'- \[ \]', next_steps_content))
                if todo_count == 0:
                    result["warnings"].append("No actionable TODO items found in Next Steps")
            
        except Exception as e:
            result["errors"].append(f"Failed to parse file: {str(e)}")
            result["valid"] = False
            
        return result
    
    def validate_all_tldl_entries(self) -> List[Dict[str, Any]]:
        """Validate all TLDL entries in the docs directory"""
        results = []
        
        if not self.tldl_path.exists():
            print(f"Warning: TLDL path {self.tldl_path} does not exist")
            return results
        
        # Find all TLDL files
        tldl_files = list(self.tldl_path.glob("TLDL-*.md"))
        
        if not tldl_files:
            print(f"Warning: No TLDL files found in {self.tldl_path}")
            return results
        
        for tldl_file in tldl_files:
            result = self.validate_tldl_entry(tldl_file)
            results.append(result)
            
        return results
    
    def validate_devtimetravel_config(self) -> Dict[str, Any]:
        """Validate DevTimeTravel configuration file"""
        config_path = self.tldl_path / "devtimetravel_snapshot.yaml"
        result = {
            "file": str(config_path),
            "valid": True,
            "errors": [],
            "warnings": []
        }
        
        if not config_path.exists():
            result["errors"].append("DevTimeTravel config file not found")
            result["valid"] = False
            return result
        
        try:
            with open(config_path, 'r') as f:
                config = yaml.safe_load(f)
            
            # Check required top-level sections
            required_sections = ["project_info", "snapshot_config", "context_capture"]
            for section in required_sections:
                if section not in config:
                    result["errors"].append(f"Missing required section: {section}")
                    result["valid"] = False
            
            # Validate project info
            if "project_info" in config:
                project_info = config["project_info"]
                required_fields = ["name", "version", "description"]
                for field in required_fields:
                    if field not in project_info:
                        result["warnings"].append(f"Missing project_info.{field}")
            
            # Validate snapshot config
            if "snapshot_config" in config:
                snapshot_config = config["snapshot_config"]
                if "frequency" in snapshot_config:
                    valid_frequencies = ["daily", "on_commit", "manual", "ci_only"]
                    if snapshot_config["frequency"] not in valid_frequencies:
                        result["warnings"].append(f"Invalid frequency. Valid options: {', '.join(valid_frequencies)}")
                        
        except yaml.YAMLError as e:
            result["errors"].append(f"Invalid YAML: {str(e)}")
            result["valid"] = False
        except Exception as e:
            result["errors"].append(f"Failed to validate config: {str(e)}")
            result["valid"] = False
            
        return result
    
    def validate_scroll_quotes(self) -> Dict[str, Any]:
        """Validate the scroll quote database if available"""
        result = {
            "valid": True,
            "errors": [],
            "warnings": [],
            "stats": {}
        }
        
        if not QUOTE_ENGINE_AVAILABLE:
            result["warnings"].append("ScrollQuoteEngine not available, skipping quote validation")
            return result
        
        try:
            engine = ScrollQuoteEngine()
            validation_report = engine.validate_quotes_database()
            
            result["valid"] = validation_report["valid"]
            result["errors"] = validation_report["issues"]
            result["warnings"] = validation_report["recommendations"]
            result["stats"] = {
                "total_quotes": validation_report["total_quotes"],
                "buttsafe_certified": validation_report["buttsafe_certified"],
                "categories": validation_report["categories"]
            }
            
        except Exception as e:
            result["valid"] = False
            result["errors"].append(f"Quote validation failed: {str(e)}")
        
        return result
    
    def validate_capsule_scrolls(self) -> Dict[str, Any]:
        """Validate Capsule Scrolls if they exist"""
        capsules_path = Path("capsules")
        
        result = {
            "valid": True,
            "errors": [],
            "warnings": [],
            "total_capsules": 0,
            "valid_capsules": 0,
            "capsule_details": {}
        }
        
        if not capsules_path.exists():
            result["warnings"].append("Capsules directory does not exist")
            return result
            
        # Check directory structure
        expected_dirs = ["active", "archived", "templates"]
        for dirname in expected_dirs:
            dir_path = capsules_path / dirname
            if not dir_path.exists():
                result["warnings"].append(f"Missing capsules subdirectory: {dirname}")
        
        # Validate active capsules
        active_dir = capsules_path / "active"
        if active_dir.exists():
            for capsule_file in active_dir.glob("*.md"):
                result["total_capsules"] += 1
                capsule_validation = self._validate_single_capsule(capsule_file)
                result["capsule_details"][str(capsule_file)] = capsule_validation
                
                if capsule_validation["valid"]:
                    result["valid_capsules"] += 1
                else:
                    result["errors"].extend(capsule_validation["errors"])
                    result["valid"] = False
                
                result["warnings"].extend(capsule_validation["warnings"])
        
        # Validate archived capsules  
        archived_dir = capsules_path / "archived"
        if archived_dir.exists():
            for capsule_file in archived_dir.glob("*.md"):
                result["total_capsules"] += 1
                capsule_validation = self._validate_single_capsule(capsule_file)
                result["capsule_details"][str(capsule_file)] = capsule_validation
                
                if capsule_validation["valid"]:
                    result["valid_capsules"] += 1
                else:
                    result["errors"].extend(capsule_validation["errors"])
                    result["valid"] = False
                
                result["warnings"].extend(capsule_validation["warnings"])
        
        return result
        
    def _validate_single_capsule(self, file_path: Path) -> Dict[str, Any]:
        """Validate a single Capsule Scroll file"""
        result = {
            "valid": True,
            "errors": [],
            "warnings": []
        }
        
        try:
            content = file_path.read_text()
            
            # Check for required sections
            required_sections = [
                "Daily Ghost Sweep", "Badge Verdict Pass",
                "Compact Transcript", "Re‑entry Spell", "Links to Related Content"
            ]
            
            for section in required_sections:
                if section not in content:
                    result["errors"].append(f"Missing required section: {section}")
                    result["valid"] = False
            
            # Check for re-entry spell (should be between 2-5 sentences)
            reentry_match = re.search(r'### \*\*\d+\. Re‑entry Spell\*\*.*?\n> \*(.*?)\*', content, re.DOTALL)
            if reentry_match:
                spell_text = reentry_match.group(1)
                sentence_count = len(re.findall(r'[.!?]+', spell_text))
                if sentence_count < 2:
                    result["warnings"].append("Re-entry spell should be 2-5 sentences for optimal context restoration")
                elif sentence_count > 5:
                    result["warnings"].append("Re-entry spell is longer than recommended 5 sentences")
            else:
                result["errors"].append("Re-entry spell format is incorrect or missing")
                result["valid"] = False
            
            # Check for emoji patterns
            if "🧠📜" not in content and "🧠" not in content:
                result["warnings"].append("Capsule Scroll should contain brain emoji (🧠) or brain+scroll combo (🧠📜)")
            
            # Check for Buttsafe elements
            if "🍑" not in content and "Save the Butts" not in content and "butts" not in content.lower():
                result["warnings"].append("Capsule Scroll should reference Buttsafe elements for cultural consistency")
            
            # Check for Chronicle Keeper attribution
            if "Chronicle Keeper" not in content:
                result["warnings"].append("Missing Chronicle Keeper attribution")
                
        except Exception as e:
            result["errors"].append(f"Failed to read or parse file: {str(e)}")
            result["valid"] = False
        
        return result
    
    def generate_report(self, results: List[Dict[str, Any]], devtimetravel_result: Dict[str, Any], quote_result: Dict[str, Any] = None, capsule_result: Dict[str, Any] = None) -> Dict[str, Any]:
        """Generate a comprehensive validation report"""
        total_files = len(results)
        valid_files = sum(1 for r in results if r["valid"])
        total_errors = sum(len(r["errors"]) for r in results) + len(devtimetravel_result["errors"])
        total_warnings = sum(len(r["warnings"]) for r in results) + len(devtimetravel_result["warnings"])
        
        # Include quote validation in totals if available
        if quote_result:
            total_errors += len(quote_result["errors"])
            total_warnings += len(quote_result["warnings"])
        
        # Include capsule validation in totals if available  
        if capsule_result:
            total_errors += len(capsule_result["errors"])
            total_warnings += len(capsule_result["warnings"])
        
        report = {
            "summary": {
                "total_tldl_files": total_files,
                "valid_tldl_files": valid_files,
                "devtimetravel_config_valid": devtimetravel_result["valid"],
                "scroll_quotes_valid": quote_result["valid"] if quote_result else None,
                "capsule_scrolls_valid": capsule_result["valid"] if capsule_result else None,
                "total_capsules": capsule_result["total_capsules"] if capsule_result else 0,
                "valid_capsules": capsule_result["valid_capsules"] if capsule_result else 0,
                "total_errors": total_errors,
                "total_warnings": total_warnings,
                "overall_status": "PASS" if total_errors == 0 else "FAIL"
            },
            "tldl_results": results,
            "devtimetravel_result": devtimetravel_result,
            "quote_result": quote_result,
            "capsule_result": capsule_result,
            "generated_at": datetime.now().isoformat()
        }
        
        return report

def main():
    parser = argparse.ArgumentParser(description="Validate TLDL entries and documentation")
    parser.add_argument("--tldl-path", default="docs/", help="Path to TLDL documentation")
    parser.add_argument("--output-format", choices=["text", "json"], default="text", help="Output format")
    parser.add_argument("--output-file", help="Output file (default: stdout)")
    parser.add_argument("--skip-quotes", action="store_true", help="Skip scroll quote validation")
    args = parser.parse_args()
    
    validator = TLDLValidator(args.tldl_path)
    
    # Validate TLDL entries
    tldl_results = validator.validate_all_tldl_entries()
    
    # Validate DevTimeTravel config
    devtimetravel_result = validator.validate_devtimetravel_config()
    
    # Validate scroll quotes
    quote_result = None
    if not args.skip_quotes:
        quote_result = validator.validate_scroll_quotes()
    
    # Validate capsule scrolls
    capsule_result = validator.validate_capsule_scrolls()
    
    # Generate report
    report = validator.generate_report(tldl_results, devtimetravel_result, quote_result, capsule_result)
    
    # Output results
    if args.output_format == "json":
        import json
        output = json.dumps(report, indent=2)
    else:
        # Text format
        lines = []
        lines.append("=== TLDL and Documentation Validation Report ===")
        lines.append(f"Generated at: {report['generated_at']}")
        lines.append(f"Overall Status: {report['summary']['overall_status']}")
        lines.append(f"Total TLDL Files: {report['summary']['total_tldl_files']}")
        lines.append(f"Valid TLDL Files: {report['summary']['valid_tldl_files']}")
        lines.append(f"DevTimeTravel Config Valid: {report['summary']['devtimetravel_config_valid']}")
        if report['summary']['scroll_quotes_valid'] is not None:
            lines.append(f"Scroll Quotes Valid: {report['summary']['scroll_quotes_valid']}")
        if report['summary']['capsule_scrolls_valid'] is not None:
            lines.append(f"Capsule Scrolls Valid: {report['summary']['capsule_scrolls_valid']}")
            lines.append(f"Total Capsules: {report['summary']['total_capsules']}")
            lines.append(f"Valid Capsules: {report['summary']['valid_capsules']}")
        lines.append(f"Total Errors: {report['summary']['total_errors']}")
        lines.append(f"Total Warnings: {report['summary']['total_warnings']}")
        lines.append("")
        
        # TLDL file details
        for result in tldl_results:
            lines.append(f"File: {result['file']}")
            lines.append(f"  Valid: {result['valid']}")
            if result['errors']:
                lines.append("  Errors:")
                for error in result['errors']:
                    lines.append(f"    - {error}")
            if result['warnings']:
                lines.append("  Warnings:")
                for warning in result['warnings']:
                    lines.append(f"    - {warning}")
            lines.append("")
        
        # DevTimeTravel config details
        lines.append(f"DevTimeTravel Config: {devtimetravel_result['file']}")
        lines.append(f"  Valid: {devtimetravel_result['valid']}")
        if devtimetravel_result['errors']:
            lines.append("  Errors:")
            for error in devtimetravel_result['errors']:
                lines.append(f"    - {error}")
        if devtimetravel_result['warnings']:
            lines.append("  Warnings:")
            for warning in devtimetravel_result['warnings']:
                lines.append(f"    - {warning}")
        
        # Scroll quote validation details
        if quote_result:
            lines.append("")
            lines.append("Scroll Quotes Database:")
            lines.append(f"  Valid: {quote_result['valid']}")
            if quote_result['stats']:
                stats = quote_result['stats']
                lines.append(f"  Total Quotes: {stats.get('total_quotes', 0)}")
                lines.append(f"  Buttsafe Certified: {stats.get('buttsafe_certified', 0)}")
                lines.append(f"  Categories: {stats.get('categories', 0)}")
            if quote_result['errors']:
                lines.append("  Errors:")
                for error in quote_result['errors']:
                    lines.append(f"    - {error}")
            if quote_result['warnings']:
                lines.append("  Warnings:")
                for warning in quote_result['warnings']:
                    lines.append(f"    - {warning}")
        
        output = "\n".join(lines)
    
    if args.output_file:
        with open(args.output_file, 'w') as f:
            f.write(output)
        print(f"Report written to {args.output_file}")
    else:
        print(output)
    
    # Exit with error code if validation failed
    if report['summary']['overall_status'] == "FAIL":
        exit(1)

if __name__ == "__main__":
    main()
