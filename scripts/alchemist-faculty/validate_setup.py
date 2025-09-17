#!/usr/bin/env python3
"""
Setup validation script for Alchemist Faculty tools

Validates that all dependencies and requirements are met for using
the Alchemist Faculty bundle.
"""

import sys
import subprocess
import importlib
import json
import os
from pathlib import Path

def check_python_version():
    """Check if Python version is 3.11 or higher"""
    version = sys.version_info
    if version.major == 3 and version.minor >= 11:
        print(f"✅ Python {version.major}.{version.minor}.{version.micro} - Compatible")
        return True
    else:
        print(f"❌ Python {version.major}.{version.minor}.{version.micro} - Requires Python 3.11+")
        return False

def check_required_packages():
    """Check if required Python packages are installed"""
    required_packages = [
        'yaml',
        'requests',
        'argparse'
    ]
    
    missing = []
    for package in required_packages:
        try:
            importlib.import_module(package)
            print(f"✅ {package} - Available")
        except ImportError:
            print(f"❌ {package} - Missing")
            missing.append(package)
    
    return len(missing) == 0, missing

def check_project_structure():
    """Check if project has expected structure"""
    required_dirs = [
        'gu_pot',
        'schemas/alchemist',
        'scripts/alchemist-faculty',
        'templates/alchemist',
        'docs/faculty/alchemist'
    ]
    
    missing = []
    for dir_path in required_dirs:
        if Path(dir_path).exists():
            print(f"✅ {dir_path} - Exists")
        else:
            print(f"❌ {dir_path} - Missing")
            missing.append(dir_path)
    
    return len(missing) == 0, missing

def check_schema_validity():
    """Check if Alchemist schemas are valid JSON"""
    schema_files = [
        'schemas/alchemist/claim-origin-extension.json'
    ]
    
    invalid = []
    for schema_file in schema_files:
        try:
            with open(schema_file, 'r') as f:
                json.load(f)
            print(f"✅ {schema_file} - Valid JSON")
        except (FileNotFoundError, json.JSONDecodeError) as e:
            print(f"❌ {schema_file} - Invalid: {e}")
            invalid.append(schema_file)
    
    return len(invalid) == 0, invalid

def check_script_permissions():
    """Check if scripts have proper execution permissions"""
    scripts = [
        'scripts/alchemist-faculty/generate_manifest.py'
    ]
    
    missing_perms = []
    for script in scripts:
        if Path(script).exists():
            if os.access(script, os.X_OK):
                print(f"✅ {script} - Executable")
            else:
                print(f"⚠️ {script} - Not executable (fixable)")
                missing_perms.append(script)
        else:
            print(f"❌ {script} - Missing")
            missing_perms.append(script)
    
    return len(missing_perms) == 0, missing_perms

def fix_permissions(scripts):
    """Fix script permissions"""
    for script in scripts:
        if Path(script).exists():
            os.chmod(script, 0o755)
            print(f"🔧 Fixed permissions for {script}")

def create_missing_dirs(dirs):
    """Create missing directories"""
    for dir_path in dirs:
        Path(dir_path).mkdir(parents=True, exist_ok=True)
        print(f"🔧 Created directory {dir_path}")

def main():
    print("🧪 Alchemist Faculty Setup Validation")
    print("=" * 50)
    
    all_good = True
    
    # Check Python version
    print("\n📋 Checking Python Version...")
    if not check_python_version():
        all_good = False
    
    # Check required packages
    print("\n📦 Checking Required Packages...")
    packages_ok, missing_packages = check_required_packages()
    if not packages_ok:
        all_good = False
        print(f"\nTo install missing packages:")
        print(f"pip install {' '.join(missing_packages)}")
    
    # Check project structure
    print("\n📁 Checking Project Structure...")
    structure_ok, missing_dirs = check_project_structure()
    if not structure_ok:
        print("\n🔧 Creating missing directories...")
        create_missing_dirs(missing_dirs)
    
    # Check schema validity
    print("\n📋 Checking Schema Files...")
    schemas_ok, invalid_schemas = check_schema_validity()
    if not schemas_ok:
        all_good = False
    
    # Check script permissions
    print("\n🔑 Checking Script Permissions...")
    perms_ok, missing_perms = check_script_permissions()
    if not perms_ok:
        print("\n🔧 Fixing script permissions...")
        fix_permissions(missing_perms)
    
    # Final status
    print("\n" + "=" * 50)
    if all_good:
        print("✅ All checks passed! Alchemist Faculty is ready to use.")
        print("\nNext steps:")
        print("1. Review docs/faculty/alchemist/quickstart-guide.md")
        print("2. Try: python scripts/alchemist-faculty/generate_manifest.py --help")
        print("3. Test Unity tools: Tools > Alchemist Faculty > Generate Manifest")
    else:
        print("⚠️ Some issues found. Please address them before using Alchemist Faculty.")
        print("\nRerun this script after fixing issues.")
    
    return 0 if all_good else 1

if __name__ == '__main__':
    sys.exit(main())