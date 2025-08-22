#!/usr/bin/env python3
"""
Symbolic Linter for Living Dev Agent Template
Validates symbol resolution, dependency tracking, and code structure.

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
import ast
import sys
import json
import argparse
from pathlib import Path
from typing import List, Dict, Any, Set
from dataclasses import dataclass

@dataclass
class SymbolIssue:
    file_path: str
    line_number: int
    issue_type: str
    symbol: str
    description: str
    severity: str  # 'error', 'warning', 'info'

class SymbolicLinter:
    def __init__(self):
        self.issues: List[SymbolIssue] = []
        self.symbol_registry: Dict[str, Set[str]] = {}
        
    def analyze_python_file(self, file_path: Path) -> List[SymbolIssue]:
        """Analyze a Python file for symbol resolution issues"""
        issues = []
        
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
                
            # Parse AST
            tree = ast.parse(content, filename=str(file_path))
            
            # Track imports
            imports = set()
            for node in ast.walk(tree):
                if isinstance(node, ast.Import):
                    for alias in node.names:
                        imports.add(alias.name)
                elif isinstance(node, ast.ImportFrom):
                    if node.module:
                        for alias in node.names:
                            imports.add(f"{node.module}.{alias.name}")
            
            # Check for undefined symbols
            for node in ast.walk(tree):
                if isinstance(node, ast.Name) and isinstance(node.ctx, ast.Load):
                    symbol = node.id
                    if not self._is_symbol_defined(symbol, imports, content):
                        issues.append(SymbolIssue(
                            file_path=str(file_path),
                            line_number=node.lineno,
                            issue_type="undefined_symbol",
                            symbol=symbol,
                            description=f"Symbol '{symbol}' may not be defined",
                            severity="warning"
                        ))
                        
        except SyntaxError as e:
            issues.append(SymbolIssue(
                file_path=str(file_path),
                line_number=e.lineno or 0,
                issue_type="syntax_error",
                symbol="",
                description=f"Syntax error: {e.msg}",
                severity="error"
            ))
        except Exception as e:
            issues.append(SymbolIssue(
                file_path=str(file_path),
                line_number=0,
                issue_type="parse_error",
                symbol="",
                description=f"Failed to parse file: {str(e)}",
                severity="error"
            ))
            
        return issues
    
    def analyze_csharp_file(self, file_path: Path) -> List[SymbolIssue]:
        """Analyze a C# file for basic symbol resolution issues"""
        issues = []
        
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            lines = content.split('\n')
            
            # Check for common C# issues
            for i, line in enumerate(lines, 1):
                # Check for missing using statements
                if re.search(r'\b(List|Dictionary|IEnumerable)\b', line) and 'using System.Collections' not in content:
                    issues.append(SymbolIssue(
                        file_path=str(file_path),
                        line_number=i,
                        issue_type="missing_using",
                        symbol="System.Collections",
                        description="Possible missing 'using System.Collections.Generic;'",
                        severity="warning"
                    ))
                
                # Check for Unity-specific symbols without Unity using
                if re.search(r'\b(GameObject|Transform|MonoBehaviour)\b', line) and 'using UnityEngine' not in content:
                    issues.append(SymbolIssue(
                        file_path=str(file_path),
                        line_number=i,
                        issue_type="missing_using",
                        symbol="UnityEngine",
                        description="Possible missing 'using UnityEngine;'",
                        severity="warning"
                    ))
                
                # Check for ECS symbols without Unity.Entities
                if re.search(r'\b(Entity|IComponentData|ISystem)\b', line) and 'using Unity.Entities' not in content:
                    issues.append(SymbolIssue(
                        file_path=str(file_path),
                        line_number=i,
                        issue_type="missing_using",
                        symbol="Unity.Entities",
                        description="Possible missing 'using Unity.Entities;'",
                        severity="warning"
                    ))
                        
        except Exception as e:
            issues.append(SymbolIssue(
                file_path=str(file_path),
                line_number=0,
                issue_type="parse_error",
                symbol="",
                description=f"Failed to parse C# file: {str(e)}",
                severity="error"
            ))
            
        return issues
    
    def _is_symbol_defined(self, symbol: str, imports: Set[str], content: str) -> bool:
        """Check if a symbol is defined in the current scope"""
        # Built-in Python symbols
        builtin_names = set(dir(builtins))
        
        if symbol in builtin_names:
            return True
        
        # Check if symbol is imported
        if symbol in imports or any(imp.endswith(f".{symbol}") for imp in imports):
            return True
        
        # Check if symbol is defined in the file
        if re.search(rf'\b(def|class)\s+{symbol}\b', content):
            return True
        
        if re.search(rf'^{symbol}\s*=', content, re.MULTILINE):
            return True
        
        return False
    
    def analyze_directory(self, directory: Path) -> List[SymbolIssue]:
        """Analyze all source files in a directory"""
        all_issues = []
        
        # Python files
        for py_file in directory.rglob("*.py"):
            if py_file.name.startswith('.'):
                continue
            issues = self.analyze_python_file(py_file)
            all_issues.extend(issues)
        
        # C# files
        for cs_file in directory.rglob("*.cs"):
            if cs_file.name.startswith('.'):
                continue
            issues = self.analyze_csharp_file(cs_file)
            all_issues.extend(issues)
        
        return all_issues
    
    def generate_report(self, issues: List[SymbolIssue]) -> Dict[str, Any]:
        """Generate a comprehensive linting report"""
        errors = [issue for issue in issues if issue.severity == "error"]
        warnings = [issue for issue in issues if issue.severity == "warning"]
        infos = [issue for issue in issues if issue.severity == "info"]
        
        # Group by file
        by_file = {}
        for issue in issues:
            if issue.file_path not in by_file:
                by_file[issue.file_path] = []
            by_file[issue.file_path].append(issue)
        
        # Group by issue type
        by_type = {}
        for issue in issues:
            if issue.issue_type not in by_type:
                by_type[issue.issue_type] = []
            by_type[issue.issue_type].append(issue)
        
        report = {
            "summary": {
                "total_issues": len(issues),
                "errors": len(errors),
                "warnings": len(warnings),
                "infos": len(infos),
                "files_analyzed": len(by_file),
                "status": "FAIL" if errors else "PASS"
            },
            "issues_by_file": {
                file_path: [
                    {
                        "line": issue.line_number,
                        "type": issue.issue_type,
                        "symbol": issue.symbol,
                        "description": issue.description,
                        "severity": issue.severity
                    }
                    for issue in file_issues
                ]
                for file_path, file_issues in by_file.items()
            },
            "issues_by_type": {
                issue_type: len(type_issues)
                for issue_type, type_issues in by_type.items()
            },
            "all_issues": [
                {
                    "file": issue.file_path,
                    "line": issue.line_number,
                    "type": issue.issue_type,
                    "symbol": issue.symbol,
                    "description": issue.description,
                    "severity": issue.severity
                }
                for issue in issues
            ]
        }
        
        return report

def main():
    parser = argparse.ArgumentParser(description="Symbolic linter for code analysis")
    parser.add_argument("--path", required=True, help="Path to analyze (file or directory)")
    parser.add_argument("--output-format", choices=["text", "json"], default="text", help="Output format")
    parser.add_argument("--output-file", help="Output file (default: stdout)")
    parser.add_argument("--strict-mode", action="store_true", help="Treat warnings as errors")
    args = parser.parse_args()
    
    linter = SymbolicLinter()
    path = Path(args.path)
    
    if not path.exists():
        print(f"Error: Path {path} does not exist")
        sys.exit(1)
    
    # Analyze files
    if path.is_file():
        if path.suffix == ".py":
            issues = linter.analyze_python_file(path)
        elif path.suffix == ".cs":
            issues = linter.analyze_csharp_file(path)
        else:
            print(f"Warning: Unsupported file type {path.suffix}")
            issues = []
    else:
        issues = linter.analyze_directory(path)
    
    # Generate report
    report = linter.generate_report(issues)
    
    # Adjust status for strict mode
    if args.strict_mode and report["summary"]["warnings"] > 0:
        report["summary"]["status"] = "FAIL"
    
    # Output results
    if args.output_format == "json":
        output = json.dumps(report, indent=2)
    else:
        # Text format
        lines = []
        lines.append("=== Symbolic Linter Report ===")
        lines.append(f"Status: {report['summary']['status']}")
        lines.append(f"Total Issues: {report['summary']['total_issues']}")
        lines.append(f"Errors: {report['summary']['errors']}")
        lines.append(f"Warnings: {report['summary']['warnings']}")
        lines.append(f"Files Analyzed: {report['summary']['files_analyzed']}")
        lines.append("")
        
        # Issues by type
        if report['issues_by_type']:
            lines.append("Issues by Type:")
            for issue_type, count in report['issues_by_type'].items():
                lines.append(f"  {issue_type}: {count}")
            lines.append("")
        
        # Detailed issues
        for file_path, file_issues in report['issues_by_file'].items():
            lines.append(f"File: {file_path}")
            for issue in file_issues:
                severity_marker = "❌" if issue['severity'] == "error" else "⚠️" if issue['severity'] == "warning" else "ℹ️"
                lines.append(f"  {severity_marker} Line {issue['line']}: {issue['description']}")
                if issue['symbol']:
                    lines.append(f"     Symbol: {issue['symbol']}")
            lines.append("")
        
        output = "\n".join(lines)
    
    if args.output_file:
        with open(args.output_file, 'w') as f:
            f.write(output)
        print(f"Report written to {args.output_file}")
    else:
        print(output)
    
    # Exit with error code if analysis failed
    if report['summary']['status'] == "FAIL":
        sys.exit(1)

if __name__ == "__main__":
    main()
