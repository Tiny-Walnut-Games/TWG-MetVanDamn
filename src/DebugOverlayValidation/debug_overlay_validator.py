#!/usr/bin/env python3
"""
Debug Overlay Validation for Living Dev Agent Template
Validates debug overlay systems, console integrations, and runtime diagnostics.

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
import json
import argparse
from pathlib import Path
from typing import List, Dict, Any, Optional
from dataclasses import dataclass

@dataclass
class DebugValidationResult:
    component: str
    status: str  # 'pass', 'fail', 'warning'
    message: str
    details: Optional[Dict[str, Any]] = None

class DebugOverlayValidator:
    def __init__(self):
        self.results: List[DebugValidationResult] = []
        self.validation_patterns = {
            "csharp": {
                "unity_debug_patterns": [
                    (r'public class.*DebugOverlay.*System', "debug_system_class"),
                    (r'OnGUI\(\)', "unity_gui_integration"),
                    (r'GUI\.Label|GUILayout\.', "gui_rendering"),
                    (r'#if\s+UNITY_EDITOR', "editor_conditional"),
                    (r'Debug\.Log|Console\.WriteLine', "logging_integration")
                ]
            }
        }
        
    def validate_debug_overlay_structure(self, path: Path) -> List[DebugValidationResult]:
        """Validate debug overlay file structure and components"""
        results = []
        
        # Check for debug overlay files
        debug_files = list(path.rglob("*Debug*.cs")) + list(path.rglob("*debug*.py"))
        
        if not debug_files:
            results.append(DebugValidationResult(
                component="debug_files",
                status="warning",
                message="No debug overlay files found",
                details={"expected_patterns": ["*Debug*.cs", "*debug*.py"]}
            ))
            return results
        
        results.append(DebugValidationResult(
            component="debug_files",
            status="pass",
            message=f"Found {len(debug_files)} debug files",
            details={"files": [str(f) for f in debug_files]}
        ))
        
        # Validate each debug file
        for debug_file in debug_files:
            file_results = self._validate_debug_file(debug_file)
            results.extend(file_results)
        
        return results
    
    def _validate_debug_file(self, file_path: Path) -> List[DebugValidationResult]:
        """Validate individual debug file"""
        results = []
        
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            if file_path.suffix == '.cs':
                results.extend(self._validate_csharp_debug_file(file_path, content))
            elif file_path.suffix == '.py':
                results.extend(self._validate_python_debug_file(file_path, content))
                
        except Exception as e:
            results.append(DebugValidationResult(
                component=f"file_parse_{file_path.name}",
                status="fail",
                message=f"Failed to parse debug file: {str(e)}",
                details={"file": str(file_path)}
            ))
        
        return results
    
    def _validate_csharp_debug_file(self, file_path: Path, content: str) -> List[DebugValidationResult]:
        """Validate C# debug overlay file"""
        results = []
        
        # Use configured patterns if available, otherwise Unity defaults
        unity_debug_patterns = self.validation_patterns.get("csharp", {}).get("unity_debug_patterns", [
            (r'public class.*DebugOverlay.*System', "debug_system_class"),
            (r'OnGUI\(\)', "unity_gui_integration"),
            (r'GUI\.Label|GUILayout\.', "gui_rendering"),
            (r'#if\s+UNITY_EDITOR', "editor_conditional"),
            (r'Debug\.Log|Console\.WriteLine', "logging_integration")
        ])
        
        for pattern, component in unity_debug_patterns:
            if re.search(pattern, content, re.IGNORECASE):
                results.append(DebugValidationResult(
                    component=f"csharp_{component}",
                    status="pass",
                    message=f"Found {component.replace('_', ' ')} in {file_path.name}",
                    details={"pattern": pattern}
                ))
            else:
                results.append(DebugValidationResult(
                    component=f"csharp_{component}",
                    status="warning",
                    message=f"Missing {component.replace('_', ' ')} in {file_path.name}",
                    details={"pattern": pattern, "suggestion": "Consider adding this feature"}
                ))
        
        # Check for ECS-specific patterns
        ecs_patterns = [
            (r'IComponentData|ISystemData', "ecs_components"),
            (r'EntityManager|World', "ecs_world_access"),
            (r'SystemBase|ISystem', "ecs_system_base")
        ]
        
        ecs_found = False
        for pattern, component in ecs_patterns:
            if re.search(pattern, content):
                ecs_found = True
                results.append(DebugValidationResult(
                    component=f"ecs_{component}",
                    status="pass",
                    message=f"ECS integration detected: {component}",
                    details={"pattern": pattern}
                ))
        
        if ecs_found:
            results.append(DebugValidationResult(
                component="ecs_integration",
                status="pass",
                message="ECS debug integration detected",
                details={"file": str(file_path)}
            ))
        
        return results
    
    def _validate_python_debug_file(self, file_path: Path, content: str) -> List[DebugValidationResult]:
        """Validate Python debug file"""
        results = []
        
        # Check for common debug patterns
        debug_patterns = [
            (r'import logging|from logging', "logging_module"),
            (r'def.*debug|class.*Debug', "debug_functions"),
            (r'print\(|logger\.|log\.', "output_methods"),
            (r'if __name__ == "__main__"', "standalone_execution"),
            (r'argparse|click|typer', "cli_interface")
        ]
        
        for pattern, component in debug_patterns:
            if re.search(pattern, content):
                results.append(DebugValidationResult(
                    component=f"python_{component}",
                    status="pass",
                    message=f"Found {component.replace('_', ' ')} in {file_path.name}",
                    details={"pattern": pattern}
                ))
        
        # Check for validation-specific patterns
        validation_patterns = [
            (r'def validate|class.*Validator', "validation_logic"),
            (r'assert|assertEqual|assertTrue', "test_assertions"),
            (r'try:|except:|finally:', "error_handling")
        ]
        
        for pattern, component in validation_patterns:
            if re.search(pattern, content):
                results.append(DebugValidationResult(
                    component=f"validation_{component}",
                    status="pass",
                    message=f"Validation pattern found: {component}",
                    details={"pattern": pattern}
                ))
        
        return results
    
    def validate_console_integration(self, path: Path) -> List[DebugValidationResult]:
        """Validate console and CLI integration"""
        results = []
        
        # Look for console-related files
        console_files = (
            list(path.rglob("*console*.py")) + 
            list(path.rglob("*Console*.cs")) +
            list(path.rglob("*cli*.py")) +
            list(path.rglob("*CLI*.cs"))
        )
        
        if console_files:
            results.append(DebugValidationResult(
                component="console_files",
                status="pass",
                message=f"Found {len(console_files)} console-related files",
                details={"files": [str(f) for f in console_files]}
            ))
            
            # Validate console functionality
            for console_file in console_files:
                try:
                    with open(console_file, 'r', encoding='utf-8') as f:
                        content = f.read()
                    
                    console_features = [
                        (r'def.*command|class.*Command', "command_system"),
                        (r'input\(|readline|Console\.ReadLine', "user_input"),
                        (r'help|--help|-h', "help_system"),
                        (r'KeyCode|Input\.|key.*press', "key_bindings")
                    ]
                    
                    for pattern, feature in console_features:
                        if re.search(pattern, content, re.IGNORECASE):
                            results.append(DebugValidationResult(
                                component=f"console_{feature}",
                                status="pass",
                                message=f"Console feature detected: {feature}",
                                details={"file": str(console_file)}
                            ))
                            
                except Exception as e:
                    results.append(DebugValidationResult(
                        component=f"console_parse_{console_file.name}",
                        status="fail",
                        message=f"Failed to parse console file: {str(e)}",
                        details={"file": str(console_file)}
                    ))
        else:
            results.append(DebugValidationResult(
                component="console_files",
                status="warning",
                message="No console integration files found",
                details={"suggestion": "Consider adding console/CLI integration for better debugging"}
            ))
        
        return results
    
    def validate_runtime_diagnostics(self, path: Path) -> List[DebugValidationResult]:
        """Validate runtime diagnostic capabilities"""
        results = []
        
        # Look for diagnostic-related patterns in all files
        diagnostic_files = list(path.rglob("*.cs")) + list(path.rglob("*.py"))
        
        diagnostic_features = {
            "performance_monitoring": [
                r'Stopwatch|DateTime\.Now|Time\.time',
                r'fps|frame.*rate|performance',
                r'memory|Memory|GC\.'
            ],
            "system_health": [
                r'health.*check|status.*check',
                r'validate|verification',
                r'diagnostic|Diagnostic'
            ],
            "error_reporting": [
                r'try.*catch|except.*Exception',
                r'error.*log|Error.*Log',
                r'exception.*handler|Exception.*Handler'
            ]
        }
        
        feature_counts = {feature: 0 for feature in diagnostic_features}
        
        for diag_file in diagnostic_files:
            try:
                with open(diag_file, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                for feature, patterns in diagnostic_features.items():
                    for pattern in patterns:
                        if re.search(pattern, content, re.IGNORECASE):
                            feature_counts[feature] += 1
                            break  # Count each file only once per feature
                            
            except Exception:
                continue  # Skip files we can't read
        
        # Report diagnostic capabilities
        for feature, count in feature_counts.items():
            if count > 0:
                results.append(DebugValidationResult(
                    component=f"diagnostics_{feature}",
                    status="pass",
                    message=f"Found {feature.replace('_', ' ')} in {count} files",
                    details={"file_count": count}
                ))
            else:
                results.append(DebugValidationResult(
                    component=f"diagnostics_{feature}",
                    status="warning",
                    message=f"No {feature.replace('_', ' ')} detected",
                    details={"suggestion": f"Consider adding {feature.replace('_', ' ')} capabilities"}
                ))
        
        return results
    
    def generate_report(self, all_results: List[DebugValidationResult]) -> Dict[str, Any]:
        """Generate comprehensive debug validation report"""
        passes = [r for r in all_results if r.status == "pass"]
        failures = [r for r in all_results if r.status == "fail"]
        warnings = [r for r in all_results if r.status == "warning"]
        
        # Group by component category
        by_category = {}
        for result in all_results:
            category = result.component.split('_')[0]
            if category not in by_category:
                by_category[category] = []
            by_category[category].append(result)
        
        report = {
            "summary": {
                "total_checks": len(all_results),
                "passes": len(passes),
                "failures": len(failures),
                "warnings": len(warnings),
                "overall_status": "FAIL" if failures else "PASS",
                "health_score": (len(passes) / len(all_results)) * 100 if all_results else 0
            },
            "categories": {
                category: {
                    "total": len(results),
                    "passes": len([r for r in results if r.status == "pass"]),
                    "failures": len([r for r in results if r.status == "fail"]),
                    "warnings": len([r for r in results if r.status == "warning"])
                }
                for category, results in by_category.items()
            },
            "detailed_results": [
                {
                    "component": result.component,
                    "status": result.status,
                    "message": result.message,
                    "details": result.details
                }
                for result in all_results
            ],
            "recommendations": [
                result.details.get("suggestion", result.message)
                for result in warnings
                if result.details and "suggestion" in result.details
            ]
        }
        
        return report

def main():
    parser = argparse.ArgumentParser(description="Validate debug overlay systems")
    parser.add_argument("--path", required=True, help="Path to debug overlay validation directory")
    parser.add_argument("--output-format", choices=["text", "json"], default="text", help="Output format")
    parser.add_argument("--output-file", help="Output file (default: stdout)")
    args = parser.parse_args()
    
    validator = DebugOverlayValidator()
    path = Path(args.path)
    
    if not path.exists():
        print(f"Error: Path {path} does not exist")
        exit(1)
    
    # Run all validations
    all_results = []
    
    # Structure validation
    all_results.extend(validator.validate_debug_overlay_structure(path))
    
    # Console integration validation
    all_results.extend(validator.validate_console_integration(path))
    
    # Runtime diagnostics validation
    all_results.extend(validator.validate_runtime_diagnostics(path))
    
    # Generate report
    report = validator.generate_report(all_results)
    
    # Output results
    if args.output_format == "json":
        output = json.dumps(report, indent=2)
    else:
        # Text format
        lines = []
        lines.append("=== Debug Overlay Validation Report ===")
        lines.append(f"Overall Status: {report['summary']['overall_status']}")
        lines.append(f"Health Score: {report['summary']['health_score']:.1f}%")
        lines.append(f"Total Checks: {report['summary']['total_checks']}")
        lines.append(f"Passes: {report['summary']['passes']}")
        lines.append(f"Failures: {report['summary']['failures']}")
        lines.append(f"Warnings: {report['summary']['warnings']}")
        lines.append("")
        
        # Category breakdown
        lines.append("Category Breakdown:")
        for category, stats in report['categories'].items():
            lines.append(f"  {category.title()}:")
            lines.append(f"    Total: {stats['total']}, Passes: {stats['passes']}, Failures: {stats['failures']}, Warnings: {stats['warnings']}")
        lines.append("")
        
        # Detailed results
        lines.append("Detailed Results:")
        for result in report['detailed_results']:
            status_icon = "✅" if result['status'] == "pass" else "❌" if result['status'] == "fail" else "⚠️"
            lines.append(f"  {status_icon} {result['component']}: {result['message']}")
        
        # Recommendations
        if report['recommendations']:
            lines.append("")
            lines.append("Recommendations:")
            for rec in report['recommendations']:
                lines.append(f"  • {rec}")
        
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
