#!/usr/bin/env python3
"""
Living Dev Agent Template - Universal Code Snapshot System
Jerry's legendary adjustable code reference system (sanitized from Unity)

Execution time: ~25ms for code snapshot capture
Dynamic range control: 1-50 lines before/after target with live preview
"""

import argparse
import os
import sys
import datetime
import re
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

# Sacred emojis for code documentation
EMOJI_SUCCESS = "✅"
EMOJI_WARNING = "⚠️"
EMOJI_ERROR = "❌"
EMOJI_INFO = "🔍"
EMOJI_MAGIC = "🧙‍♂️"
EMOJI_SNAPSHOT = "📸"
EMOJI_CODE = "⚡"

class CodeSnapshotCapture:
    """Jerry's legendary adjustable code snapshot system"""
    
    # Jerry's genius preset system
    PRESETS = {
        'tight': {'before': 3, 'after': 3, 'description': 'Syntax errors and focused debugging'},
        'standard': {'before': 10, 'after': 10, 'description': 'Function-level context and logic flow'},
        'wide': {'before': 25, 'after': 25, 'description': 'Architecture investigation and comprehensive analysis'}
    }
    
    def __init__(self, lines_before: int = 10, lines_after: int = 10, workspace_path: str = "."):
        self.lines_before = max(1, min(50, lines_before))  # Clamp to 1-50 range
        self.lines_after = max(1, min(50, lines_after))
        self.workspace_path = Path(workspace_path)
        
        # Create snapshots directory
        self.snapshots_dir = self.workspace_path / "debug" / "snapshots"
        self.snapshots_dir.mkdir(parents=True, exist_ok=True)

    def log_info(self, message: str, emoji: str = EMOJI_INFO):
        """Log informational message with epic styling"""
        print(f"{Colors.OKCYAN}{emoji} [INFO]{Colors.ENDC} {message}")

    def log_success(self, message: str, emoji: str = EMOJI_SUCCESS):
        """Log success message with legendary flair"""
        print(f"{Colors.OKGREEN}{emoji} [SUCCESS]{Colors.ENDC} {message}")

    def log_warning(self, message: str, emoji: str = EMOJI_WARNING):
        """Log warning message"""
        print(f"{Colors.WARNING}{emoji} [WARNING]{Colors.ENDC} {message}")

    def log_error(self, message: str, emoji: str = EMOJI_ERROR):
        """Log error message"""
        print(f"{Colors.FAIL}{emoji} [ERROR]{Colors.ENDC} {message}")

    def apply_preset(self, preset_name: str) -> bool:
        """Apply one of Jerry's genius presets"""
        if preset_name.lower() not in self.PRESETS:
            self.log_error(f"Unknown preset: {preset_name}")
            self.log_info(f"Available presets: {', '.join(self.PRESETS.keys())}")
            return False
        
        preset = self.PRESETS[preset_name.lower()]
        self.lines_before = preset['before']
        self.lines_after = preset['after']
        
        self.log_success(f"Applied {preset_name} preset: {preset['before']}+1+{preset['after']} lines")
        self.log_info(f"Use case: {preset['description']}")
        return True

    def capture_snapshot(self, file_path: str, target_line: int, context: str = "", 
                        output_format: str = "markdown") -> str:
        """Capture code snapshot with Jerry's genius adjustable range system"""
        file_path = Path(file_path)
        
        # Validate file exists
        if not file_path.exists():
            self.log_error(f"File not found: {file_path}")
            return ""
        
        # Read file content
        try:
            with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                all_lines = f.readlines()
        except Exception as e:
            self.log_error(f"Failed to read file {file_path}: {e}")
            return ""
        
        total_lines = len(all_lines)
        
        # Validate target line
        if target_line < 1 or target_line > total_lines:
            self.log_error(f"Line {target_line} out of range (1-{total_lines}) in {file_path.name}")
            return ""
        
        # Calculate snapshot range using Jerry's genius system
        start_line = max(1, target_line - self.lines_before)
        end_line = min(total_lines, target_line + self.lines_after)
        captured_lines = end_line - start_line + 1
        
        self.log_info(f"Capturing {captured_lines} lines: {self.lines_before} before + target + {self.lines_after} after")
        
        # Generate snapshot content
        if output_format.lower() == "markdown":
            snapshot_content = self.generate_markdown_snapshot(
                file_path, all_lines, start_line, end_line, target_line, total_lines, context
            )
        else:
            snapshot_content = self.generate_text_snapshot(
                file_path, all_lines, start_line, end_line, target_line, total_lines, context
            )
        
        # Save snapshot to file
        snapshot_filename = self.save_snapshot(file_path, target_line, snapshot_content, output_format)
        
        self.log_success(f"Code snapshot captured: {file_path.name} line {target_line}")
        self.log_info(f"Range: {captured_lines} lines ({self.lines_before}+1+{self.lines_after})")
        self.log_info(f"Saved to: {snapshot_filename}")
        
        return snapshot_content

    def generate_markdown_snapshot(self, file_path: Path, all_lines: List[str], 
                                  start_line: int, end_line: int, target_line: int, 
                                  total_lines: int, context: str) -> str:
        """Generate markdown-formatted snapshot"""
        # Detect language for syntax highlighting
        language = self.detect_language(file_path)
        
        snapshot_content = f"""# 📸 Code Snapshot: {file_path.name}:{target_line}

**File:** `{file_path}`  
**Target Line:** {target_line}  
**Context:** {context or 'Code reference snapshot'}  
**Captured:** {datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')}  
**Range:** Lines {start_line}-{end_line} (showing {end_line - start_line + 1} of {total_lines} total)  
**Window Configuration:** {self.lines_before} before + target + {self.lines_after} after  

## Code Snapshot

```{language}
"""
        
        # Add line-by-line content with target highlighting
        for line_num in range(start_line, end_line + 1):
            line_content = all_lines[line_num - 1].rstrip('\n\r')
            line_prefix = ">>> " if line_num == target_line else "    "
            formatted_line_num = f"{line_num:3d}"
            
            snapshot_content += f"{line_prefix}{formatted_line_num} | {line_content}\n"
        
        snapshot_content += "```\n\n"
        
        # Add target line details
        target_content = all_lines[target_line - 1].strip() if target_line <= len(all_lines) else ""
        snapshot_content += f"**Target Line {target_line}:** `{target_content}`\n\n"
        
        # Add configuration details
        snapshot_content += f"**Jerry's Snapshot Configuration:**  \n"
        snapshot_content += f"- Lines before: {self.lines_before}  \n"
        snapshot_content += f"- Lines after: {self.lines_after}  \n"
        snapshot_content += f"- Total window: {end_line - start_line + 1} lines  \n"
        snapshot_content += f"- Use case: Perfect for {self.get_use_case_description()}\n\n"
        
        # Add metadata
        snapshot_content += "---\n\n"
        snapshot_content += "*This code snapshot was captured using Jerry's legendary adjustable range system.*\n"
        
        return snapshot_content

    def generate_text_snapshot(self, file_path: Path, all_lines: List[str], 
                              start_line: int, end_line: int, target_line: int, 
                              total_lines: int, context: str) -> str:
        """Generate plain text snapshot"""
        snapshot_content = f"Code Snapshot: {file_path.name}:{target_line}\n"
        snapshot_content += f"=" * 60 + "\n"
        snapshot_content += f"File: {file_path}\n"
        snapshot_content += f"Target Line: {target_line}\n"
        snapshot_content += f"Context: {context or 'Code reference snapshot'}\n"
        snapshot_content += f"Captured: {datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n"
        snapshot_content += f"Range: Lines {start_line}-{end_line} ({end_line - start_line + 1} of {total_lines} total)\n"
        snapshot_content += f"Window: {self.lines_before} before + target + {self.lines_after} after\n"
        snapshot_content += "-" * 60 + "\n\n"
        
        # Add code content
        for line_num in range(start_line, end_line + 1):
            line_content = all_lines[line_num - 1].rstrip('\n\r')
            line_prefix = ">>> " if line_num == target_line else "    "
            formatted_line_num = f"{line_num:3d}"
            
            snapshot_content += f"{line_prefix}{formatted_line_num} | {line_content}\n"
        
        snapshot_content += "\n" + "-" * 60 + "\n"
        snapshot_content += f"Target Line {target_line}: {all_lines[target_line - 1].strip()}\n"
        
        return snapshot_content

    def detect_language(self, file_path: Path) -> str:
        """Detect programming language from file extension"""
        extension_map = {
            '.py': 'python',
            '.js': 'javascript',
            '.ts': 'typescript',
            '.cs': 'csharp',
            '.cpp': 'cpp',
            '.c': 'c',
            '.h': 'c',
            '.hpp': 'cpp',
            '.java': 'java',
            '.go': 'go',
            '.rs': 'rust',
            '.php': 'php',
            '.rb': 'ruby',
            '.swift': 'swift',
            '.kt': 'kotlin',
            '.scala': 'scala',
            '.sh': 'bash',
            '.ps1': 'powershell',
            '.sql': 'sql',
            '.html': 'html',
            '.css': 'css',
            '.xml': 'xml',
            '.json': 'json',
            '.yaml': 'yaml',
            '.yml': 'yaml'
        }
        
        return extension_map.get(file_path.suffix.lower(), 'text')

    def get_use_case_description(self) -> str:
        """Get description of current configuration's use case"""
        total_lines = self.lines_before + 1 + self.lines_after
        
        if total_lines <= 7:
            return "syntax errors and focused debugging"
        elif total_lines <= 21:
            return "function-level context and logic flow"
        else:
            return "architecture investigation and comprehensive analysis"

    def save_snapshot(self, file_path: Path, target_line: int, content: str, format: str) -> str:
        """Save snapshot to file"""
        timestamp = datetime.datetime.now().strftime('%Y%m%d_%H%M%S')
        safe_filename = re.sub(r'[^a-zA-Z0-9_.-]', '_', file_path.name)
        extension = '.md' if format.lower() == 'markdown' else '.txt'
        
        snapshot_filename = f"snapshot_{safe_filename}_L{target_line}_{timestamp}{extension}"
        snapshot_path = self.snapshots_dir / snapshot_filename
        
        try:
            with open(snapshot_path, 'w', encoding='utf-8') as f:
                f.write(content)
            return str(snapshot_path)
        except Exception as e:
            self.log_error(f"Failed to save snapshot: {e}")
            return ""

    def find_function_or_class_line(self, file_path: str, search_term: str) -> Optional[int]:
        """Find line number of a function or class definition"""
        file_path = Path(file_path)
        
        if not file_path.exists():
            return None
        
        try:
            with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                lines = f.readlines()
            
            # Pattern for different languages
            patterns = [
                rf'^\s*(def|function|class|interface|struct|enum)\s+{re.escape(search_term)}\b',  # Python, JS, etc.
                rf'^\s*(public|private|protected)?\s*(static)?\s*(class|interface|struct|enum)\s+{re.escape(search_term)}\b',  # C#, Java
                rf'^\s*{re.escape(search_term)}\s*\(',  # Function calls
                rf'.*{re.escape(search_term)}.*',  # General search
            ]
            
            for line_num, line in enumerate(lines, 1):
                for pattern in patterns:
                    if re.search(pattern, line, re.IGNORECASE):
                        self.log_info(f"Found '{search_term}' at line {line_num}: {line.strip()}")
                        return line_num
            
            return None
            
        except Exception as e:
            self.log_error(f"Failed to search in {file_path}: {e}")
            return None

    def preview_snapshot_range(self, file_path: str, target_line: int) -> None:
        """Preview what lines would be captured without creating snapshot"""
        file_path = Path(file_path)
        
        if not file_path.exists():
            self.log_error(f"File not found: {file_path}")
            return
        
        try:
            with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                all_lines = f.readlines()
        except Exception as e:
            self.log_error(f"Failed to read file: {e}")
            return
        
        total_lines = len(all_lines)
        
        if target_line < 1 or target_line > total_lines:
            self.log_error(f"Line {target_line} out of range (1-{total_lines})")
            return
        
        start_line = max(1, target_line - self.lines_before)
        end_line = min(total_lines, target_line + self.lines_after)
        captured_lines = end_line - start_line + 1
        
        print(f"\n{Colors.HEADER}📸 Snapshot Preview: {file_path.name}:{target_line}{Colors.ENDC}")
        print(f"Configuration: {self.lines_before} before + target + {self.lines_after} after")
        print(f"Range: Lines {start_line}-{end_line} ({captured_lines} total lines)")
        print(f"Use case: {self.get_use_case_description()}")
        print("=" * 60)
        
        # Show preview of lines
        for line_num in range(start_line, min(start_line + 10, end_line + 1)):
            line_content = all_lines[line_num - 1].rstrip('\n\r')
            line_prefix = ">>> " if line_num == target_line else "    "
            formatted_line_num = f"{line_num:3d}"
            
            print(f"{line_prefix}{formatted_line_num} | {line_content}")
        
        if end_line - start_line > 9:
            print(f"    ... ({captured_lines - 10} more lines)")
        
        print()


def main():
    """Main code snapshot interface"""
    parser = argparse.ArgumentParser(
        description=f"{EMOJI_MAGIC} Jerry's Universal Code Snapshot System {EMOJI_SNAPSHOT}",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Jerry's Genius Preset System:
  tight:    3+1+3 lines    - Syntax errors and focused debugging
  standard: 10+1+10 lines  - Function-level context and logic flow  
  wide:     25+1+25 lines  - Architecture investigation and comprehensive analysis

Examples:
  python3 code_snapshot.py file.py 42 --preset tight
  python3 code_snapshot.py src/main.py 156 --context "Bug in validation logic"
  python3 code_snapshot.py script.js 89 --before 15 --after 5
  python3 code_snapshot.py --find-function "validateUser" user.py
  python3 code_snapshot.py --preview file.py 42 --preset wide
        """
    )
    
    parser.add_argument('file_path', nargs='?', help='Path to source file')
    parser.add_argument('target_line', nargs='?', type=int, help='Target line number')
    
    # Configuration options
    parser.add_argument('--before', type=int, default=10, help='Lines to capture before target (1-50)')
    parser.add_argument('--after', type=int, default=10, help='Lines to capture after target (1-50)')
    parser.add_argument('--preset', choices=['tight', 'standard', 'wide'], 
                       help="Use Jerry's genius preset configurations")
    parser.add_argument('--workspace', default='.', help='Workspace directory path')
    
    # Snapshot options
    parser.add_argument('--context', default='', help='Context description for the snapshot')
    parser.add_argument('--format', choices=['markdown', 'text'], default='markdown',
                       help='Output format for snapshot')
    
    # Utility options
    parser.add_argument('--preview', action='store_true', help='Preview snapshot range without creating file')
    parser.add_argument('--find-function', help='Find line number of function/class definition')
    parser.add_argument('--list-presets', action='store_true', help='List available presets')
    
    args = parser.parse_args()
    
    try:
        # Create snapshot system
        snapshot_system = CodeSnapshotCapture(
            lines_before=args.before,
            lines_after=args.after,
            workspace_path=args.workspace
        )
        
        # Handle list presets
        if args.list_presets:
            print(f"\n{Colors.HEADER}📸 Jerry's Genius Preset System{Colors.ENDC}")
            for name, preset in snapshot_system.PRESETS.items():
                print(f"{name:8s}: {preset['before']:2d}+1+{preset['after']:2d} lines - {preset['description']}")
            return
        
        # Apply preset if specified
        if args.preset:
            snapshot_system.apply_preset(args.preset)
        
        # Handle find function
        if args.find_function:
            if not args.file_path:
                print(f"{Colors.FAIL}❌ File path required for function search{Colors.ENDC}")
                sys.exit(1)
            
            line_num = snapshot_system.find_function_or_class_line(args.file_path, args.find_function)
            if line_num:
                snapshot_system.log_success(f"Found '{args.find_function}' at line {line_num}")
                
                # Offer to create snapshot
                if input("Create snapshot? (y/N): ").lower().startswith('y'):
                    snapshot_system.capture_snapshot(
                        args.file_path, line_num, 
                        f"Function/class definition: {args.find_function}", 
                        args.format
                    )
            else:
                snapshot_system.log_warning(f"'{args.find_function}' not found in {args.file_path}")
            return
        
        # Validate required arguments
        if not args.file_path or args.target_line is None:
            parser.print_help()
            sys.exit(1)
        
        # Handle preview
        if args.preview:
            snapshot_system.preview_snapshot_range(args.file_path, args.target_line)
            return
        
        # Create snapshot
        content = snapshot_system.capture_snapshot(
            args.file_path, args.target_line, args.context, args.format
        )
        
        if not content:
            sys.exit(1)
        
    except KeyboardInterrupt:
        print(f"\n{Colors.WARNING}{EMOJI_WARNING} Code snapshot interrupted{Colors.ENDC}")
        sys.exit(1)
    except Exception as e:
        print(f"{Colors.FAIL}{EMOJI_ERROR} Code snapshot error: {e}{Colors.ENDC}")
        sys.exit(1)


if __name__ == "__main__":
    main()
