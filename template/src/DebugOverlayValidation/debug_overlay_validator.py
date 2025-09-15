#!/usr/bin/env python3
"""
Living Dev Agent Template - Debug Overlay Validator
Jerry's legendary debug system validation for development tools

Execution time: ~56ms for typical validation runs
Validates debug overlay systems and development tool integration
"""

import argparse
import os
import sys
import re
import datetime
import json
from pathlib import Path
from typing import List, Dict, Any, Optional, Set

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
EMOJI_DEBUG = "🐛"
EMOJI_SHIELD = "🛡️"

class DebugOverlayValidator:
    """The Bootstrap Sentinel's legendary debug system validation"""
    
    def __init__(self, path: str, verbose: bool = False):
        self.path = Path(path)
        self.verbose = verbose
        self.errors: List[str] = []
        self.warnings: List[str] = []
        self.validated_components: List[str] = []
        self.validation_start_time = datetime.datetime.now()
        self.health_score = 100.0
        
        # Debug system patterns to validate
        self.debug_patterns = {
            'logging': [
                r'Debug\.Log',
                r'Console\.WriteLine',
                r'print\(',
                r'log_info',
                r'log_warning',
                r'log_error'
            ],
            'validation': [
                r'assert\s*\(',
                r'Assert\.',
                r'validate_',
                r'check_',
                r'verify_'
            ],
            'debug_ui': [
                r'EditorGUI',
                r'GUILayout',
                r'ImGui',
                r'debug.*window',
                r'overlay.*display'
            ]
        }

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
        self.health_score -= 2.5  # Small health penalty for warnings

    def log_error(self, message: str, emoji: str = EMOJI_ERROR):
        """Log error message"""
        print(f"{Colors.FAIL}{emoji} [ERROR]{Colors.ENDC} {message}")
        self.errors.append(message)
        self.health_score -= 10.0  # Larger health penalty for errors

    def verbose_log(self, message: str):
        """Log verbose information when enabled"""
        if self.verbose:
            print(f"{Colors.OKBLUE}[VERBOSE]{Colors.ENDC} {message}")

    def analyze_file_content(self, filepath: Path) -> Dict[str, Any]:
        """Analyze file content for debug overlay patterns"""
        self.verbose_log(f"Analyzing file: {filepath}")
        
        try:
            with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
                content = f.read()
        except Exception as e:
            self.log_error(f"Failed to read {filepath}: {e}")
            return {}
        
        analysis = {
            'filepath': str(filepath),
            'size': len(content),
            'lines': len(content.split('\n')),
            'patterns_found': {},
            'debug_quality': 0.0
        }
        
        # Check for debug patterns
        total_patterns = 0
        found_patterns = 0
        
        for category, patterns in self.debug_patterns.items():
            analysis['patterns_found'][category] = []
            
            for pattern in patterns:
                matches = re.findall(pattern, content, re.IGNORECASE)
                total_patterns += 1
                
                if matches:
                    found_patterns += 1
                    analysis['patterns_found'][category].append({
                        'pattern': pattern,
                        'count': len(matches)
                    })
                    self.verbose_log(f"Found {len(matches)} instances of {pattern}")
        
        # Calculate debug quality score
        if total_patterns > 0:
            analysis['debug_quality'] = (found_patterns / total_patterns) * 100.0
        
        return analysis

    def validate_debug_coverage(self, filepath: Path, analysis: Dict[str, Any]) -> bool:
        """Validate debug coverage and quality"""
        filename = filepath.name
        patterns_found = analysis.get('patterns_found', {})
        debug_quality = analysis.get('debug_quality', 0.0)
        
        # Check for minimum debug coverage
        if debug_quality < 20.0:
            self.log_warning(f"{filename}: Low debug coverage ({debug_quality:.1f}%)")
            return False
        
        # Check for specific debug categories
        has_logging = bool(patterns_found.get('logging'))
        has_validation = bool(patterns_found.get('validation'))
        
        if not has_logging and filepath.suffix in {'.py', '.cs', '.js'}:
            self.log_warning(f"{filename}: No logging patterns found in code file")
        
        if filepath.name.lower().find('test') != -1 and not has_validation:
            self.log_warning(f"{filename}: Test file lacks validation patterns")
        
        self.verbose_log(f"{filename}: Debug quality score: {debug_quality:.1f}%")
        return True

    def validate_debug_configuration(self, filepath: Path) -> bool:
        """Validate debug configuration files"""
        if filepath.suffix.lower() in {'.json', '.yaml', '.yml'}:
            try:
                with open(filepath, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                # Basic JSON/YAML validation
                if filepath.suffix.lower() == '.json':
                    import json
                    config = json.loads(content)
                    self.verbose_log(f"JSON configuration valid: {filepath.name}")
                    
                    # Check for debug-related configuration
                    if any(key.lower().find('debug') != -1 for key in str(config).lower()):
                        self.verbose_log(f"Debug configuration found in {filepath.name}")
                
                elif filepath.suffix.lower() in {'.yaml', '.yml'}:
                    try:
                        import yaml
                        config = yaml.safe_load(content)
                        self.verbose_log(f"YAML configuration valid: {filepath.name}")
                    except ImportError:
                        self.verbose_log("PyYAML not available for YAML validation")
                        
            except Exception as e:
                self.log_warning(f"Configuration validation failed for {filepath.name}: {e}")
                return False
        
        return True

    def find_files_to_validate(self) -> List[Path]:
        """Find all files to validate in the debug overlay path"""
        files_to_validate = []
        
        if not self.path.exists():
            self.log_error(f"Debug overlay path does not exist: {self.path}")
            return files_to_validate
        
        # File extensions relevant to debug systems
        relevant_extensions = {
            '.py', '.cs', '.js', '.ts',  # Code files
            '.json', '.yaml', '.yml',    # Configuration files
            '.md', '.txt'                # Documentation files
        }
        
        if self.path.is_file():
            if self.path.suffix.lower() in relevant_extensions:
                files_to_validate.append(self.path)
        else:
            # Recursively find relevant files
            for ext in relevant_extensions:
                pattern = f"**/*{ext}"
                found_files = list(self.path.glob(pattern))
                files_to_validate.extend(found_files)
        
        # Filter out irrelevant directories
        skip_patterns = {
            '__pycache__', '.git', '.vscode', '.idea', 
            'node_modules', '.pytest_cache', 'bin', 'obj'
        }
        
        filtered_files = []
        for f in files_to_validate:
            if not any(skip_pattern in str(f) for skip_pattern in skip_patterns):
                filtered_files.append(f)
        
        return sorted(filtered_files)

    def run_validation(self) -> bool:
        """Run complete debug overlay validation suite"""
        self.log_info(f"Starting debug overlay validation for: {self.path}", EMOJI_MAGIC)
        
        # Find files to validate
        files_to_validate = self.find_files_to_validate()
        
        if not files_to_validate:
            self.log_warning("No debug overlay files found to validate")
            return True
        
        self.log_info(f"Found {len(files_to_validate)} files to validate")
        
        # Validate each file
        all_valid = True
        total_debug_quality = 0.0
        
        for file_path in files_to_validate:
            self.validated_components.append(str(file_path))
            
            # Analyze file content
            analysis = self.analyze_file_content(file_path)
            
            # Validate debug coverage
            coverage_valid = self.validate_debug_coverage(file_path, analysis)
            
            # Validate configuration if applicable
            config_valid = self.validate_debug_configuration(file_path)
            
            file_valid = coverage_valid and config_valid
            all_valid = all_valid and file_valid
            
            # Accumulate debug quality scores
            total_debug_quality += analysis.get('debug_quality', 0.0)
        
        # Calculate overall debug quality
        average_debug_quality = total_debug_quality / len(files_to_validate) if files_to_validate else 0.0
        
        # Adjust health score based on overall quality
        if average_debug_quality < 50.0:
            self.health_score -= 20.0
        elif average_debug_quality < 70.0:
            self.health_score -= 10.0
        
        # Ensure health score doesn't go below 0
        self.health_score = max(0.0, self.health_score)
        
        # Show summary
        execution_time = (datetime.datetime.now() - self.validation_start_time).total_seconds()
        
        print("\n" + "="*60)
        self.log_info(f"Debug Overlay Validation Summary", EMOJI_DEBUG)
        print(f"Components validated: {len(self.validated_components)}")
        print(f"Average debug quality: {average_debug_quality:.1f}%")
        print(f"Health score: {self.health_score:.1f}%")
        print(f"Warnings: {len(self.warnings)}")
        print(f"Errors: {len(self.errors)}")
        print(f"Execution time: {execution_time:.3f}s")
        
        if self.health_score >= 85.0:
            self.log_success(f"Debug overlay system healthy! ({self.health_score:.1f}%)", EMOJI_SHIELD)
        elif self.health_score >= 70.0:
            self.log_warning(f"Debug overlay system functional with issues ({self.health_score:.1f}%)")
        else:
            self.log_error(f"Debug overlay system needs attention ({self.health_score:.1f}%)")
        
        return all_valid and self.health_score >= 70.0


def main():
    """Main execution function for debug overlay validation"""
    parser = argparse.ArgumentParser(
        description=f"{EMOJI_MAGIC} Living Dev Agent Debug Overlay Validator {EMOJI_DEBUG}",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python3 debug_overlay_validator.py --path src/DebugOverlayValidation/
  python3 debug_overlay_validator.py --path debug_tools/ --verbose
  python3 debug_overlay_validator.py --path single_debug_file.py
        """
    )
    
    parser.add_argument(
        '--path',
        required=True,
        help='Path to debug overlay files (directory or single file)'
    )
    
    parser.add_argument(
        '--verbose',
        action='store_true',
        help='Enable verbose output for detailed analysis'
    )
    
    args = parser.parse_args()
    
    try:
        # Create validator and run validation
        validator = DebugOverlayValidator(
            path=args.path,
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
