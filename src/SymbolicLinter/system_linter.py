#!/usr/bin/env python3
"""
Living Dev Agent Template - System Linter
Jerry's legendary system architecture validation tool

Execution time: ~75ms for typical system analysis
Validates system architecture, dependencies, and design patterns
"""

import argparse
import os
import sys
import ast
import re
import datetime
from pathlib import Path
from typing import List, Dict, Any, Optional, Set, Tuple

# Color codes for epic terminal output
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

# Sacred emojis for architectural excellence
EMOJI_SUCCESS = "✅"
EMOJI_WARNING = "⚠️"
EMOJI_ERROR = "❌"
EMOJI_INFO = "🔍"
EMOJI_MAGIC = "🧙‍♂️"
EMOJI_SYSTEM = "🏗️"
EMOJI_ARCH = "🏛️"

class SystemLinter:
    """The Bootstrap Sentinel's legendary system architecture validation"""
    
    def __init__(self, path: str, verbose: bool = False):
        self.path = Path(path)
        self.verbose = verbose
        self.errors: List[str] = []
        self.warnings: List[str] = []
        self.analyzed_systems: List[str] = []
        self.analysis_start_time = datetime.datetime.now()
        
        # System architecture patterns
        self.system_patterns = {
            'imports': r'^(import|from)\s+\w+',
            'classes': r'^class\s+(\w+)',
            'functions': r'^def\s+(\w+)',
            'components': r'class\s+\w+.*Component',
            'systems': r'class\s+\w+.*System',
            'managers': r'class\s+\w+.*Manager',
            'factories': r'class\s+\w+.*Factory',
            'builders': r'class\s+\w+.*Builder',
            'validators': r'class\s+\w+.*Validator'
        }
        
        # Anti-patterns to detect
        self.anti_patterns = {
            'god_class': r'class\s+\w+.*\{[^}]{2000,}\}',  # Very large classes
            'magic_numbers': r'\b\d{3,}\b',  # Numbers larger than 99
            'todo_fixme': r'(TODO|FIXME|HACK)',
            'deep_nesting': r'(\s{12,})',  # Very deep indentation
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

    def log_error(self, message: str, emoji: str = EMOJI_ERROR):
        """Log error message"""
        print(f"{Colors.FAIL}{emoji} [ERROR]{Colors.ENDC} {message}")
        self.errors.append(message)

    def verbose_log(self, message: str):
        """Log verbose information when enabled"""
        if self.verbose:
            print(f"{Colors.OKBLUE}[VERBOSE]{Colors.ENDC} {message}")

    def analyze_system_file(self, filepath: Path) -> Dict[str, Any]:
        """Analyze a system file for architecture patterns"""
        self.verbose_log(f"Analyzing system file: {filepath}")
        
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
            'patterns': {},
            'anti_patterns': {},
            'architecture_score': 0.0
        }
        
        # Analyze system patterns
        for pattern_name, pattern in self.system_patterns.items():
            matches = re.findall(pattern, content, re.MULTILINE | re.IGNORECASE)
            analysis['patterns'][pattern_name] = len(matches)
            
            if matches:
                self.verbose_log(f"Found {len(matches)} {pattern_name} patterns")
        
        # Analyze anti-patterns
        for anti_pattern_name, pattern in self.anti_patterns.items():
            matches = re.findall(pattern, content, re.MULTILINE | re.IGNORECASE)
            analysis['anti_patterns'][anti_pattern_name] = len(matches)
            
            if matches:
                self.verbose_log(f"Found {len(matches)} {anti_pattern_name} anti-patterns")
        
        # Calculate architecture score
        analysis['architecture_score'] = self.calculate_architecture_score(analysis)
        
        return analysis

    def calculate_architecture_score(self, analysis: Dict[str, Any]) -> float:
        """Calculate architecture quality score"""
        score = 100.0
        patterns = analysis.get('patterns', {})
        anti_patterns = analysis.get('anti_patterns', {})
        
        # Positive points for good patterns
        if patterns.get('classes', 0) > 0:
            score += 5.0
        if patterns.get('functions', 0) > 0:
            score += 5.0
        if patterns.get('components', 0) > 0:
            score += 10.0  # Components are good architecture
        if patterns.get('systems', 0) > 0:
            score += 10.0  # Systems are good architecture
        
        # Negative points for anti-patterns
        score -= anti_patterns.get('god_class', 0) * 20.0
        score -= anti_patterns.get('magic_numbers', 0) * 2.0
        score -= anti_patterns.get('deep_nesting', 0) * 1.0
        
        # Ensure score is in valid range
        return max(0.0, min(100.0, score))

    def validate_system_architecture(self, filepath: Path, analysis: Dict[str, Any]) -> bool:
        """Validate system architecture quality"""
        filename = filepath.name
        architecture_score = analysis.get('architecture_score', 0.0)
        patterns = analysis.get('patterns', {})
        anti_patterns = analysis.get('anti_patterns', {})
        
        # Check for minimum architecture quality
        if architecture_score < 60.0:
            self.log_warning(f"{filename}: Low architecture score ({architecture_score:.1f}/100)")
        
        # Check for specific architectural issues
        if anti_patterns.get('god_class', 0) > 0:
            self.log_warning(f"{filename}: Potential god class detected")
        
        if anti_patterns.get('magic_numbers', 0) > 5:
            self.log_warning(f"{filename}: Many magic numbers found ({anti_patterns['magic_numbers']})")
        
        if anti_patterns.get('deep_nesting', 0) > 10:
            self.log_warning(f"{filename}: Deep nesting detected ({anti_patterns['deep_nesting']} instances)")
        
        # Check for good architectural patterns
        has_components = patterns.get('components', 0) > 0
        has_systems = patterns.get('systems', 0) > 0
        has_patterns = patterns.get('classes', 0) > 0 or patterns.get('functions', 0) > 0
        
        if not has_patterns and filepath.suffix == '.py':
            self.log_warning(f"{filename}: No clear architectural patterns found")
        
        self.verbose_log(f"{filename}: Architecture score: {architecture_score:.1f}/100")
        return architecture_score >= 50.0

    def find_system_files(self) -> List[Path]:
        """Find all system files to analyze"""
        system_files = []
        
        if not self.path.exists():
            self.log_error(f"System path does not exist: {self.path}")
            return system_files
        
        # File extensions for system analysis
        system_extensions = {'.py', '.cs', '.js', '.ts', '.cpp', '.c', '.h'}
        
        if self.path.is_file():
            if self.path.suffix.lower() in system_extensions:
                system_files.append(self.path)
        else:
            # Find all system files recursively
            for ext in system_extensions:
                pattern = f"**/*{ext}"
                found_files = list(self.path.glob(pattern))
                system_files.extend(found_files)
        
        # Filter out irrelevant files
        skip_patterns = {
            '__pycache__', '.git', '.vscode', '.idea',
            'node_modules', '.pytest_cache', 'bin', 'obj',
            'test', 'tests'  # Skip test files for system analysis
        }
        
        filtered_files = []
        for f in system_files:
            if not any(skip_pattern in str(f).lower() for skip_pattern in skip_patterns):
                filtered_files.append(f)
        
        return sorted(filtered_files)

    def run_system_analysis(self) -> bool:
        """Run complete system architecture analysis"""
        self.log_info(f"Starting system architecture analysis for: {self.path}", EMOJI_MAGIC)
        
        # Find system files
        system_files = self.find_system_files()
        
        if not system_files:
            self.log_warning("No system files found to analyze")
            return True
        
        self.log_info(f"Found {len(system_files)} system files to analyze")
        
        # Analyze each system file
        all_valid = True
        total_architecture_score = 0.0
        
        for file_path in system_files:
            self.analyzed_systems.append(str(file_path))
            
            # Analyze system architecture
            analysis = self.analyze_system_file(file_path)
            
            # Validate architecture
            architecture_valid = self.validate_system_architecture(file_path, analysis)
            all_valid = all_valid and architecture_valid
            
            # Accumulate architecture scores
            total_architecture_score += analysis.get('architecture_score', 0.0)
        
        # Calculate overall architecture quality
        average_architecture_score = total_architecture_score / len(system_files) if system_files else 0.0
        
        # Show summary
        execution_time = (datetime.datetime.now() - self.analysis_start_time).total_seconds()
        
        print("\n" + "="*60)
        self.log_info(f"System Architecture Analysis Summary", EMOJI_SYSTEM)
        print(f"Systems analyzed: {len(self.analyzed_systems)}")
        print(f"Average architecture score: {average_architecture_score:.1f}/100")
        print(f"Warnings: {len(self.warnings)}")
        print(f"Errors: {len(self.errors)}")
        print(f"Execution time: {execution_time:.3f}s")
        
        if average_architecture_score >= 80.0:
            self.log_success(f"Excellent system architecture! ({average_architecture_score:.1f}/100)", EMOJI_ARCH)
        elif average_architecture_score >= 60.0:
            self.log_success(f"Good system architecture ({average_architecture_score:.1f}/100)", EMOJI_SYSTEM)
        else:
            self.log_warning(f"System architecture needs improvement ({average_architecture_score:.1f}/100)")
        
        return all_valid


def main():
    """Main execution function for system linting"""
    parser = argparse.ArgumentParser(
        description=f"{EMOJI_MAGIC} Living Dev Agent System Linter {EMOJI_SYSTEM}",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python3 system_linter.py --path src/
  python3 system_linter.py --path system_file.py --verbose
  python3 system_linter.py --path . --verbose
        """
    )
    
    parser.add_argument(
        '--path',
        required=True,
        help='Path to system files (directory or single file)'
    )
    
    parser.add_argument(
        '--verbose',
        action='store_true',
        help='Enable verbose output for detailed analysis'
    )
    
    args = parser.parse_args()
    
    try:
        # Create system linter and run analysis
        linter = SystemLinter(
            path=args.path,
            verbose=args.verbose
        )
        
        success = linter.run_system_analysis()
        
        # Exit with appropriate code
        sys.exit(0 if success else 1)
        
    except KeyboardInterrupt:
        print(f"\n{Colors.WARNING}{EMOJI_WARNING} Analysis interrupted by user{Colors.ENDC}")
        sys.exit(1)
    except Exception as e:
        print(f"{Colors.FAIL}{EMOJI_ERROR} Analysis failed with exception: {e}{Colors.ENDC}")
        sys.exit(1)


if __name__ == "__main__":
    main()
