#!/usr/bin/env python3
"""
Living Dev Agent XP System - Package Dependencies & Requirements Manager
Jerry's vision for automatic dependency tracking and environment management

Features:
- Automatic dependency detection
- Minimum required package versions
- Cross-platform compatibility checks
- Integration health monitoring
"""

import json
import os
import sys
import subprocess
import platform
from pathlib import Path
from typing import Dict, List, Any, Optional, Tuple
from dataclasses import dataclass
from enum import Enum
import datetime

class DependencyType(Enum):
    """Types of dependencies"""
    PYTHON_PACKAGE = "python_package"
    SYSTEM_BINARY = "system_binary"
    UNITY_PACKAGE = "unity_package"
    DOTNET_PACKAGE = "dotnet_package"
    NPM_PACKAGE = "npm_package"
    IDE_EXTENSION = "ide_extension"

class DependencyStatus(Enum):
    """Dependency installation status"""
    SATISFIED = "satisfied"
    MISSING = "missing"
    VERSION_MISMATCH = "version_mismatch"
    UNKNOWN = "unknown"

@dataclass
class Dependency:
    """A single dependency requirement"""
    name: str
    dependency_type: DependencyType
    min_version: str
    max_version: Optional[str] = None
    install_command: Optional[str] = None
    check_command: Optional[str] = None
    description: str = ""
    required: bool = True
    platform_specific: List[str] = None  # ["windows", "macos", "linux"]
    
    def __post_init__(self):
        if self.platform_specific is None:
            self.platform_specific = ["windows", "macos", "linux"]

@dataclass
class DependencyCheck:
    """Result of checking a dependency"""
    dependency: Dependency
    status: DependencyStatus
    installed_version: Optional[str] = None
    error_message: Optional[str] = None
    install_suggestion: Optional[str] = None

class PackageDependencyManager:
    """Manages all package dependencies for the Living Dev Agent system"""
    
    def __init__(self, workspace_path: str = "."):
        self.workspace_path = Path(workspace_path)
        self.platform = platform.system().lower()
        
        # Dependency manifests
        self.manifests_dir = self.workspace_path / "experience" / "dependencies"
        self.manifests_dir.mkdir(parents=True, exist_ok=True)
        
        self.core_manifest_file = self.manifests_dir / "core_dependencies.json"
        self.optional_manifest_file = self.manifests_dir / "optional_dependencies.json"
        self.environment_report_file = self.manifests_dir / "environment_report.json"
        
        # Load or create dependency manifests
        self.core_dependencies = self._create_core_dependencies()
        self.optional_dependencies = self._create_optional_dependencies()
        
        self.load_manifests()

    def _create_core_dependencies(self) -> List[Dependency]:
        """Define core dependencies required for XP system"""
        return [
            # Python core
            Dependency(
                name="python",
                dependency_type=DependencyType.SYSTEM_BINARY,
                min_version="3.8.0",
                check_command="python --version",
                description="Python interpreter for XP system",
                required=True
            ),
            
            # Python packages
            Dependency(
                name="PyYAML",
                dependency_type=DependencyType.PYTHON_PACKAGE,
                min_version="6.0",
                install_command="pip install PyYAML>=6.0",
                check_command="python -c \"import yaml; print(yaml.__version__)\"",
                description="YAML parsing for configuration files",
                required=True
            ),
            
            Dependency(
                name="argparse",
                dependency_type=DependencyType.PYTHON_PACKAGE,
                min_version="1.4.0",
                install_command="pip install argparse>=1.4.0",
                check_command="python -c \"import argparse; print('1.4.0')\"",  # Usually built-in
                description="Command-line argument parsing",
                required=True
            ),
            
            # Git for version control integration
            Dependency(
                name="git",
                dependency_type=DependencyType.SYSTEM_BINARY,
                min_version="2.20.0",
                check_command="git --version",
                description="Git version control for XP tracking",
                required=True,
                install_suggestion="Install Git from https://git-scm.com/"
            ),
            
            # Platform-specific Python commands
            Dependency(
                name="python3",
                dependency_type=DependencyType.SYSTEM_BINARY,
                min_version="3.8.0",
                check_command="python3 --version",
                description="Python 3 interpreter (Unix-style)",
                required=True,
                platform_specific=["macos", "linux"]
            )
        ]

    def _create_optional_dependencies(self) -> List[Dependency]:
        """Define optional dependencies for enhanced features"""
        return [
            # Unity Editor (for Unity integration)
            Dependency(
                name="Unity Editor",
                dependency_type=DependencyType.SYSTEM_BINARY,
                min_version="2022.3.0",
                check_command="",  # Complex Unity detection
                description="Unity Editor for game development XP tracking",
                required=False
            ),
            
            # .NET SDK (for .NET projects)
            Dependency(
                name="dotnet",
                dependency_type=DependencyType.SYSTEM_BINARY,
                min_version="6.0.0",
                check_command="dotnet --version",
                install_command="Download from https://dotnet.microsoft.com/",
                description=".NET SDK for C# project integration",
                required=False
            ),
            
            # VS Code extensions
            Dependency(
                name="ms-python.python",
                dependency_type=DependencyType.IDE_EXTENSION,
                min_version="2023.1.0",
                description="Python extension for VS Code",
                required=False,
                install_suggestion="Install from VS Code marketplace"
            ),
            
            Dependency(
                name="ms-dotnettools.csharp",
                dependency_type=DependencyType.IDE_EXTENSION,
                min_version="1.25.0",
                description="C# extension for VS Code",
                required=False,
                install_suggestion="Install from VS Code marketplace"
            ),
            
            # Enhanced Python packages for advanced features
            Dependency(
                name="cryptography",
                dependency_type=DependencyType.PYTHON_PACKAGE,
                min_version="3.4.0",
                install_command="pip install cryptography>=3.4.0",
                check_command="python -c \"import cryptography; print(cryptography.__version__)\"",
                description="Encryption for secret achievements",
                required=False
            ),
            
            Dependency(
                name="requests",
                dependency_type=DependencyType.PYTHON_PACKAGE,
                min_version="2.25.0",
                install_command="pip install requests>=2.25.0",
                check_command="python -c \"import requests; print(requests.__version__)\"",
                description="HTTP requests for team synchronization",
                required=False
            ),
            
            # Development tools
            Dependency(
                name="black",
                dependency_type=DependencyType.PYTHON_PACKAGE,
                min_version="22.0.0",
                install_command="pip install black>=22.0.0",
                check_command="black --version",
                description="Python code formatting",
                required=False
            ),
            
            Dependency(
                name="pytest",
                dependency_type=DependencyType.PYTHON_PACKAGE,
                min_version="6.0.0",
                install_command="pip install pytest>=6.0.0",
                check_command="python -c \"import pytest; print(pytest.__version__)\"",
                description="Python testing framework",
                required=False
            )
        ]

    def check_dependency(self, dependency: Dependency) -> DependencyCheck:
        """Check if a single dependency is satisfied"""
        
        # Skip platform-specific dependencies on wrong platform
        if self.platform not in dependency.platform_specific:
            return DependencyCheck(
                dependency=dependency,
                status=DependencyStatus.SATISFIED,
                installed_version="N/A (platform-specific)",
                install_suggestion="Not required on this platform"
            )
        
        if not dependency.check_command:
            return DependencyCheck(
                dependency=dependency,
                status=DependencyStatus.UNKNOWN,
                error_message="No check command defined"
            )
        
        try:
            # Execute check command
            result = subprocess.run(
                dependency.check_command.split(),
                capture_output=True,
                text=True,
                timeout=10
            )
            
            if result.returncode != 0:
                return DependencyCheck(
                    dependency=dependency,
                    status=DependencyStatus.MISSING,
                    error_message=result.stderr.strip(),
                    install_suggestion=dependency.install_command or dependency.install_suggestion
                )
            
            # Extract version from output
            output = result.stdout.strip()
            installed_version = self._extract_version(output)
            
            # Check version compatibility
            if self._is_version_compatible(installed_version, dependency.min_version, dependency.max_version):
                return DependencyCheck(
                    dependency=dependency,
                    status=DependencyStatus.SATISFIED,
                    installed_version=installed_version
                )
            else:
                return DependencyCheck(
                    dependency=dependency,
                    status=DependencyStatus.VERSION_MISMATCH,
                    installed_version=installed_version,
                    install_suggestion=dependency.install_command
                )
                
        except subprocess.TimeoutExpired:
            return DependencyCheck(
                dependency=dependency,
                status=DependencyStatus.UNKNOWN,
                error_message="Check command timed out"
            )
        except Exception as e:
            return DependencyCheck(
                dependency=dependency,
                status=DependencyStatus.MISSING,
                error_message=str(e),
                install_suggestion=dependency.install_command or dependency.install_suggestion
            )

    def _extract_version(self, output: str) -> str:
        """Extract version number from command output"""
        import re
        
        # Common version patterns
        patterns = [
            r'(\d+\.\d+\.\d+)',  # Major.Minor.Patch
            r'(\d+\.\d+)',       # Major.Minor
            r'version (\d+\.\d+\.\d+)',  # "version X.Y.Z"
            r'v(\d+\.\d+\.\d+)', # "vX.Y.Z"
        ]
        
        for pattern in patterns:
            match = re.search(pattern, output)
            if match:
                return match.group(1)
        
        return output.strip()  # Fallback to raw output

    def _is_version_compatible(self, installed: str, min_version: str, max_version: Optional[str] = None) -> bool:
        """Check if installed version meets requirements"""
        try:
            from packaging import version
            
            installed_ver = version.parse(installed)
            min_ver = version.parse(min_version)
            
            if installed_ver < min_ver:
                return False
            
            if max_version:
                max_ver = version.parse(max_version)
                if installed_ver > max_ver:
                    return False
            
            return True
            
        except ImportError:
            # Fallback to simple string comparison if packaging not available
            return installed >= min_version

    def check_all_dependencies(self) -> Tuple[List[DependencyCheck], List[DependencyCheck]]:
        """Check all core and optional dependencies"""
        
        core_results = []
        optional_results = []
        
        print("🔍 Checking core dependencies...")
        for dependency in self.core_dependencies:
            result = self.check_dependency(dependency)
            core_results.append(result)
            
            status_emoji = {
                DependencyStatus.SATISFIED: "✅",
                DependencyStatus.MISSING: "❌",
                DependencyStatus.VERSION_MISMATCH: "⚠️",
                DependencyStatus.UNKNOWN: "❓"
            }
            
            emoji = status_emoji.get(result.status, "❓")
            version_info = f" ({result.installed_version})" if result.installed_version else ""
            print(f"  {emoji} {dependency.name}{version_info}")
            
            if result.error_message:
                print(f"     Error: {result.error_message}")
            if result.install_suggestion:
                print(f"     Install: {result.install_suggestion}")
        
        print("\n🔍 Checking optional dependencies...")
        for dependency in self.optional_dependencies:
            result = self.check_dependency(dependency)
            optional_results.append(result)
            
            emoji = status_emoji.get(result.status, "❓")
            version_info = f" ({result.installed_version})" if result.installed_version else ""
            print(f"  {emoji} {dependency.name}{version_info}")
        
        return core_results, optional_results

    def generate_environment_report(self) -> Dict[str, Any]:
        """Generate comprehensive environment report"""
        
        core_results, optional_results = self.check_all_dependencies()
        
        # Calculate satisfaction percentages
        core_satisfied = len([r for r in core_results if r.status == DependencyStatus.SATISFIED])
        core_total = len([r for r in core_results if r.dependency.required])
        core_percentage = (core_satisfied / core_total * 100) if core_total > 0 else 0
        
        optional_satisfied = len([r for r in optional_results if r.status == DependencyStatus.SATISFIED])
        optional_total = len(optional_results)
        optional_percentage = (optional_satisfied / optional_total * 100) if optional_total > 0 else 0
        
        # System information
        system_info = {
            "platform": platform.system(),
            "platform_version": platform.version(),
            "architecture": platform.architecture()[0],
            "python_version": platform.python_version(),
            "python_implementation": platform.python_implementation()
        }
        
        report = {
            "generated_at": datetime.datetime.now().isoformat(),
            "system_info": system_info,
            "core_dependencies": {
                "satisfied": core_satisfied,
                "total": core_total,
                "percentage": round(core_percentage, 1),
                "results": [
                    {
                        "name": r.dependency.name,
                        "status": r.status.value,
                        "installed_version": r.installed_version,
                        "required_version": r.dependency.min_version,
                        "error": r.error_message,
                        "install_suggestion": r.install_suggestion
                    }
                    for r in core_results
                ]
            },
            "optional_dependencies": {
                "satisfied": optional_satisfied,
                "total": optional_total,
                "percentage": round(optional_percentage, 1),
                "results": [
                    {
                        "name": r.dependency.name,
                        "status": r.status.value,
                        "installed_version": r.installed_version,
                        "required_version": r.dependency.min_version,
                        "error": r.error_message,
                        "install_suggestion": r.install_suggestion
                    }
                    for r in optional_results
                ]
            },
            "recommendations": self._generate_recommendations(core_results, optional_results)
        }
        
        return report

    def _generate_recommendations(self, core_results: List[DependencyCheck], optional_results: List[DependencyCheck]) -> List[str]:
        """Generate installation and setup recommendations"""
        recommendations = []
        
        # Check for missing core dependencies
        missing_core = [r for r in core_results if r.status in [DependencyStatus.MISSING, DependencyStatus.VERSION_MISMATCH]]
        
        if missing_core:
            recommendations.append("🚨 CRITICAL: Install missing core dependencies before using XP system")
            for result in missing_core:
                if result.install_suggestion:
                    recommendations.append(f"   Run: {result.install_suggestion}")
        
        # Platform-specific recommendations
        if self.platform == "windows":
            recommendations.append("💡 Windows: Consider using Windows Subsystem for Linux (WSL) for better compatibility")
        elif self.platform == "darwin":
            recommendations.append("💡 macOS: Use Homebrew for easy package management: brew install python git")
        elif self.platform == "linux":
            recommendations.append("💡 Linux: Use your distribution's package manager for system dependencies")
        
        # Optional enhancements
        useful_optional = [r for r in optional_results if r.status == DependencyStatus.MISSING and r.dependency.name in ["cryptography", "requests", "dotnet"]]
        
        if useful_optional:
            recommendations.append("✨ OPTIONAL: Install these packages for enhanced features:")
            for result in useful_optional:
                if result.install_suggestion:
                    recommendations.append(f"   {result.install_suggestion}")
        
        # IDE recommendations
        recommendations.append("🛠️ IDE SETUP: Install XP system extensions for your preferred IDE")
        recommendations.append("   VS Code: Install Python and C# extensions")
        recommendations.append("   Unity: XP menu items will appear automatically after setup")
        
        return recommendations

    def install_missing_core_dependencies(self) -> bool:
        """Attempt to automatically install missing core dependencies"""
        
        core_results, _ = self.check_all_dependencies()
        missing_core = [r for r in core_results if r.status in [DependencyStatus.MISSING, DependencyStatus.VERSION_MISMATCH]]
        
        if not missing_core:
            print("✅ All core dependencies are satisfied!")
            return True
        
        print(f"🔧 Attempting to install {len(missing_core)} missing dependencies...")
        
        for result in missing_core:
            if not result.install_suggestion or not result.install_suggestion.startswith("pip"):
                print(f"⏭️ Skipping {result.dependency.name} (manual installation required)")
                continue
            
            try:
                print(f"📦 Installing {result.dependency.name}...")
                subprocess.run(result.install_suggestion.split(), check=True, timeout=60)
                print(f"✅ Successfully installed {result.dependency.name}")
                
            except subprocess.CalledProcessError as e:
                print(f"❌ Failed to install {result.dependency.name}: {e}")
                return False
            except subprocess.TimeoutExpired:
                print(f"⏰ Installation of {result.dependency.name} timed out")
                return False
        
        print("🎉 Core dependency installation complete!")
        return True

    def save_manifests(self) -> bool:
        """Save dependency manifests to files"""
        try:
            # Save core dependencies
            core_data = {
                "version": "1.0",
                "platform": self.platform,
                "last_updated": datetime.datetime.now().isoformat(),
                "dependencies": [
                    {
                        "name": dep.name,
                        "type": dep.dependency_type.value,
                        "min_version": dep.min_version,
                        "max_version": dep.max_version,
                        "install_command": dep.install_command,
                        "check_command": dep.check_command,
                        "description": dep.description,
                        "required": dep.required,
                        "platform_specific": dep.platform_specific
                    }
                    for dep in self.core_dependencies
                ]
            }
            
            with open(self.core_manifest_file, 'w', encoding='utf-8') as f:
                json.dump(core_data, f, indent=2)
            
            # Save optional dependencies
            optional_data = {
                "version": "1.0",
                "platform": self.platform,
                "last_updated": datetime.datetime.now().isoformat(),
                "dependencies": [
                    {
                        "name": dep.name,
                        "type": dep.dependency_type.value,
                        "min_version": dep.min_version,
                        "max_version": dep.max_version,
                        "install_command": dep.install_command,
                        "check_command": dep.check_command,
                        "description": dep.description,
                        "required": dep.required,
                        "platform_specific": dep.platform_specific
                    }
                    for dep in self.optional_dependencies
                ]
            }
            
            with open(self.optional_manifest_file, 'w', encoding='utf-8') as f:
                json.dump(optional_data, f, indent=2)
            
            return True
            
        except Exception as e:
            print(f"❌ Failed to save manifests: {e}")
            return False

    def load_manifests(self) -> bool:
        """Load dependency manifests from files"""
        try:
            # Create default manifests if they don't exist
            if not self.core_manifest_file.exists():
                self.save_manifests()
            
            return True
            
        except Exception as e:
            print(f"⚠️ Could not load manifests: {e}")
            return False


def main():
    """Package Dependency Manager CLI"""
    import argparse
    
    parser = argparse.ArgumentParser(description="📦 Package Dependency Manager")
    parser.add_argument('--workspace', default='.', help='Workspace directory path')
    parser.add_argument('--check', action='store_true', help='Check all dependencies')
    parser.add_argument('--install', action='store_true', help='Install missing core dependencies')
    parser.add_argument('--report', action='store_true', help='Generate environment report')
    parser.add_argument('--save-manifests', action='store_true', help='Save dependency manifests')
    
    args = parser.parse_args()
    
    try:
        manager = PackageDependencyManager(workspace_path=args.workspace)
        
        if args.check:
            core_results, optional_results = manager.check_all_dependencies()
            
            core_issues = len([r for r in core_results if r.status != DependencyStatus.SATISFIED])
            optional_issues = len([r for r in optional_results if r.status != DependencyStatus.SATISFIED])
            
            print(f"\n📊 Summary:")
            print(f"   Core dependencies: {len(core_results) - core_issues}/{len(core_results)} satisfied")
            print(f"   Optional dependencies: {len(optional_results) - optional_issues}/{len(optional_results)} satisfied")
            
            if core_issues > 0:
                print(f"\n⚠️ {core_issues} core dependencies need attention!")
                print("   Run --install to attempt automatic installation")
        
        elif args.install:
            success = manager.install_missing_core_dependencies()
            if success:
                print("\n🎉 Installation complete! Run --check to verify.")
            else:
                print("\n❌ Some installations failed. Check errors above.")
        
        elif args.report:
            report = manager.generate_environment_report()
            
            print("📋 ENVIRONMENT REPORT")
            print("=" * 50)
            print(f"Platform: {report['system_info']['platform']} {report['system_info']['platform_version']}")
            print(f"Python: {report['system_info']['python_version']} ({report['system_info']['python_implementation']})")
            print(f"Architecture: {report['system_info']['architecture']}")
            
            print(f"\n🎯 Core Dependencies: {report['core_dependencies']['percentage']}% satisfied")
            print(f"✨ Optional Dependencies: {report['optional_dependencies']['percentage']}% satisfied")
            
            if report['recommendations']:
                print(f"\n💡 Recommendations:")
                for rec in report['recommendations']:
                    print(f"   {rec}")
            
            # Save report
            with open(manager.environment_report_file, 'w', encoding='utf-8') as f:
                json.dump(report, f, indent=2)
            print(f"\n📄 Report saved to: {manager.environment_report_file}")
        
        elif args.save_manifests:
            if manager.save_manifests():
                print("✅ Dependency manifests saved successfully")
            else:
                print("❌ Failed to save dependency manifests")
        
        else:
            print("📦 Living Dev Agent Package Dependency Manager")
            print("Use --help to see available commands")
            print(f"Manifests directory: {manager.manifests_dir}")
            
    except KeyboardInterrupt:
        print("\n⚠️ Dependency check interrupted")
    except Exception as e:
        print(f"❌ Dependency manager error: {e}")


if __name__ == "__main__":
    main()
