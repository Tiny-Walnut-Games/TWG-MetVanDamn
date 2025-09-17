#!/usr/bin/env python3
"""
The Conservator CLI - Command Line Interface for Warbler Auto-Repair

This script provides a command-line interface for managing The Conservator
repair system, including module registration, repair operations, and status monitoring.

Usage Examples:
    # Register a module
    python3 conservator_cli.py register --name "my_module" --path "src/my_module.py" --validation "python3 -m py_compile src/my_module.py"
    
    # Repair a module
    python3 conservator_cli.py repair --module "my_module" --trigger failed_core_test
    
    # Check system status
    python3 conservator_cli.py status
    
    # Create snapshot
    python3 conservator_cli.py snapshot --module "my_module"
"""

import argparse
import json
import sys
from pathlib import Path

# Add project root to path for imports
project_root = Path(__file__).parent.parent
sys.path.insert(0, str(project_root))

from engine.conservator import (
    TheConservator, ModuleRegistration, RepairTrigger, RepairAction,
    create_conservator, register_warbler_core_modules
)


def register_module(args):
    """Register a module with The Conservator."""
    conservator = create_conservator(args.manifest)
    
    # Parse repair actions
    repair_actions = set()
    if args.actions:
        for action in args.actions:
            try:
                repair_actions.add(RepairAction(action))
            except ValueError:
                print(f"❌ Invalid repair action: {action}")
                return False
    else:
        # Default actions
        repair_actions = {
            RepairAction.RESTORE_FROM_SNAPSHOT,
            RepairAction.VALIDATE_AND_ROLLBACK
        }
    
    # Create registration
    registration = ModuleRegistration(
        module_name=args.name,
        module_path=args.path,
        last_known_good_hash="",  # Will be generated during registration
        backup_strategy=args.backup_strategy,
        validation_command=args.validation,
        dependencies=args.dependencies or [],
        repair_actions_enabled=repair_actions
    )
    
    # Register module
    success = conservator.manifest.register_module(registration)
    
    if success:
        print(f"✅ Module '{args.name}' successfully registered with The Conservator")
        
        # Create initial snapshot
        if conservator.create_snapshot(args.name):
            print(f"📸 Initial snapshot created for '{args.name}'")
        else:
            print(f"⚠️ Failed to create initial snapshot for '{args.name}'")
        
        return True
    else:
        print(f"❌ Failed to register module '{args.name}'")
        return False


def unregister_module(args):
    """Unregister a module from The Conservator."""
    conservator = create_conservator(args.manifest)
    
    success = conservator.manifest.unregister_module(args.name)
    
    if success:
        print(f"✅ Module '{args.name}' successfully unregistered from The Conservator")
        return True
    else:
        print(f"❌ Module '{args.name}' was not registered or failed to unregister")
        return False


def repair_module(args):
    """Perform repair operation on a module."""
    conservator = create_conservator(args.manifest)
    
    # Parse trigger
    try:
        trigger = RepairTrigger(args.trigger)
    except ValueError:
        print(f"❌ Invalid trigger: {args.trigger}")
        return False
    
    # Parse requested actions if provided
    requested_actions = None
    if args.actions:
        requested_actions = []
        for action in args.actions:
            try:
                requested_actions.append(RepairAction(action))
            except ValueError:
                print(f"❌ Invalid repair action: {action}")
                return False
    
    print(f"🔧 Starting repair operation for module '{args.module}'...")
    print(f"   Trigger: {trigger.value}")
    
    # Perform repair
    repair_op = conservator.repair_module(args.module, trigger, requested_actions)
    
    # Display results
    print(f"\n📊 Repair Operation Results:")
    print(f"   Operation ID: {repair_op.operation_id}")
    print(f"   Status: {repair_op.status.value}")
    print(f"   Actions Taken: {[action.value for action in repair_op.actions_taken]}")
    
    if repair_op.error_message:
        print(f"   Error: {repair_op.error_message}")
    
    if repair_op.human_intervention_required:
        print(f"   ⚠️ Human intervention required")
    
    if repair_op.chronicle_entry_id:
        print(f"   📜 Chronicle entry: {repair_op.chronicle_entry_id}")
    
    return repair_op.status.value in ['success']


def create_snapshot(args):
    """Create a snapshot of a module."""
    conservator = create_conservator(args.manifest)
    
    success = conservator.create_snapshot(args.module)
    
    if success:
        print(f"📸 Snapshot created successfully for module '{args.module}'")
        return True
    else:
        print(f"❌ Failed to create snapshot for module '{args.module}'")
        return False


def validate_module(args):
    """Validate a module."""
    conservator = create_conservator(args.manifest)
    
    success, results = conservator.validate_module(args.module)
    
    print(f"🧪 Validation Results for '{args.module}':")
    print(f"   Status: {'✅ PASSED' if success else '❌ FAILED'}")
    
    if 'return_code' in results:
        print(f"   Return Code: {results['return_code']}")
    
    if 'stdout' in results and results['stdout']:
        print(f"   Output: {results['stdout']}")
    
    if 'stderr' in results and results['stderr']:
        print(f"   Error: {results['stderr']}")
    
    return success


def show_status(args):
    """Show system and module status."""
    conservator = create_conservator(args.manifest)
    
    if args.module:
        # Show specific module status
        status = conservator.get_module_status(args.module)
        
        if 'error' in status:
            print(f"❌ {status['error']}")
            return False
        
        print(f"📊 Status for Module '{args.module}':")
        print(f"   Path: {status['module_path']}")
        print(f"   Integrity Check: {'✅ PASS' if status['integrity_check'] else '❌ FAIL'}")
        print(f"   Last Repair: {status['last_repair'] or 'Never'}")
        print(f"   Repair Count: {status['repair_count']}")
        print(f"   Enabled Actions: {status['enabled_actions']}")
        
        if status['recent_repairs']:
            print(f"\n🕐 Recent Repairs:")
            for repair in status['recent_repairs']:
                print(f"   - {repair['start_time']}: {repair['status']} ({repair['trigger']})")
        
    else:
        # Show system status
        health = conservator.repair_system_health_check()
        
        print(f"🏥 The Conservator System Health:")
        print(f"   Operational: {'✅' if health['conservator_operational'] else '❌'}")
        print(f"   Manifest Accessible: {'✅' if health['manifest_accessible'] else '❌'}")
        print(f"   Backup Directory: {'✅' if health['backup_directory_accessible'] else '❌'}")
        print(f"   Chronicle Directory: {'✅' if health['chronicle_directory_accessible'] else '❌'}")
        print(f"   Registered Modules: {health['registered_modules_count']}")
        print(f"   Total Repairs: {health['total_repairs_performed']}")
        
        if health['integrity_issues_detected']:
            print(f"\n⚠️ Modules Needing Attention:")
            for module in health['modules_needing_attention']:
                print(f"   - {module}")
        
        # List all registered modules
        modules = conservator.list_registered_modules()
        if modules:
            print(f"\n📋 Registered Modules:")
            for module in modules:
                print(f"   - {module}")
    
    return True


def setup_warbler_core(args):
    """Setup core Warbler modules with The Conservator."""
    conservator = create_conservator(args.manifest)
    
    print("🛠️ Setting up core Warbler modules with The Conservator...")
    
    success = register_warbler_core_modules(conservator)
    
    if success:
        print("✅ Core Warbler modules registered successfully")
        
        # Create initial snapshots
        for module_name in ['warden', 'alice']:
            if conservator.create_snapshot(module_name):
                print(f"📸 Initial snapshot created for '{module_name}'")
            else:
                print(f"⚠️ Failed to create initial snapshot for '{module_name}'")
        
        return True
    else:
        print("❌ Failed to register core Warbler modules")
        return False


def main():
    """Main CLI entry point."""
    parser = argparse.ArgumentParser(
        description="The Conservator - Warbler Auto-Repair CLI",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Register a module
  %(prog)s register --name my_module --path src/my_module.py --validation "python3 -m py_compile src/my_module.py"
  
  # Repair a module
  %(prog)s repair --module my_module --trigger failed_core_test
  
  # Check status
  %(prog)s status
  %(prog)s status --module my_module
  
  # Create snapshot
  %(prog)s snapshot --module my_module
  
  # Setup core modules
  %(prog)s setup-core
        """
    )
    
    parser.add_argument('--manifest', default='data/conservator_manifest.json',
                       help='Path to conservator manifest file')
    
    subparsers = parser.add_subparsers(dest='command', help='Available commands')
    
    # Register command
    register_parser = subparsers.add_parser('register', help='Register a module')
    register_parser.add_argument('--name', required=True, help='Module name')
    register_parser.add_argument('--path', required=True, help='Module path')
    register_parser.add_argument('--validation', required=True, help='Validation command')
    register_parser.add_argument('--backup-strategy', default='file_copy', help='Backup strategy')
    register_parser.add_argument('--dependencies', nargs='*', help='Module dependencies')
    register_parser.add_argument('--actions', nargs='*', 
                               choices=[action.value for action in RepairAction],
                               help='Enabled repair actions')
    
    # Unregister command
    unregister_parser = subparsers.add_parser('unregister', help='Unregister a module')
    unregister_parser.add_argument('--name', required=True, help='Module name')
    
    # Repair command
    repair_parser = subparsers.add_parser('repair', help='Repair a module')
    repair_parser.add_argument('--module', required=True, help='Module name')
    repair_parser.add_argument('--trigger', required=True,
                              choices=[trigger.value for trigger in RepairTrigger],
                              help='Repair trigger')
    repair_parser.add_argument('--actions', nargs='*',
                              choices=[action.value for action in RepairAction],
                              help='Specific repair actions to perform')
    
    # Snapshot command
    snapshot_parser = subparsers.add_parser('snapshot', help='Create module snapshot')
    snapshot_parser.add_argument('--module', required=True, help='Module name')
    
    # Validate command
    validate_parser = subparsers.add_parser('validate', help='Validate a module')
    validate_parser.add_argument('--module', required=True, help='Module name')
    
    # Status command
    status_parser = subparsers.add_parser('status', help='Show status')
    status_parser.add_argument('--module', help='Specific module to check')
    
    # Setup core command
    setup_parser = subparsers.add_parser('setup-core', help='Setup core Warbler modules')
    
    args = parser.parse_args()
    
    if not args.command:
        parser.print_help()
        return 1
    
    # Execute command
    try:
        if args.command == 'register':
            success = register_module(args)
        elif args.command == 'unregister':
            success = unregister_module(args)
        elif args.command == 'repair':
            success = repair_module(args)
        elif args.command == 'snapshot':
            success = create_snapshot(args)
        elif args.command == 'validate':
            success = validate_module(args)
        elif args.command == 'status':
            success = show_status(args)
        elif args.command == 'setup-core':
            success = setup_warbler_core(args)
        else:
            print(f"❌ Unknown command: {args.command}")
            return 1
        
        return 0 if success else 1
        
    except Exception as e:
        print(f"❌ Error executing command '{args.command}': {e}")
        return 1


if __name__ == '__main__':
    sys.exit(main())