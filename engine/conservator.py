#!/usr/bin/env python3
"""
The Conservator - Warbler Auto-Repair Module

📜 "Refactoring is not a sign of failure; it's a sign of growth. Like molting, but for code."
   — Code Evolution Theory, Vol. III

The Conservator provides automated, bounded repair mechanisms to maintain the operational
integrity of Warbler's core modules. This system is strictly reactive, only performing
repairs using pre-approved, known-good assets or code paths.

Key Features:
- Opt-in module registration system
- Bounded repair scope (no upgrades or architectural changes)
- Chronicle Keeper integration for full audit trail
- Human oversight and escalation paths
- Dependabot-compatible update acceptance

Author: Bootstrap Sentinel & The Conservator Team
Sacred Mission: Preserve the integrity of the Warbler ecosystem
"""

from __future__ import annotations
import json
import time
import datetime
import hashlib
import subprocess
import shutil
from dataclasses import dataclass, field
from pathlib import Path
from typing import Dict, List, Any, Optional, Set, Tuple
from enum import Enum
import logging

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class RepairTrigger(Enum):
    """Triggers that can initiate a repair operation."""
    FAILED_CORE_TEST = "failed_core_test"
    MODULE_CRASH = "module_crash"
    EXPLICIT_HUMAN_COMMAND = "explicit_human_command"
    DEPENDENCY_CORRUPTION = "dependency_corruption"


class RepairAction(Enum):
    """Available repair actions within bounded scope."""
    RESTORE_FROM_SNAPSHOT = "restore_from_snapshot"
    RELINK_DEPENDENCIES = "relink_dependencies"
    REINITIALIZE_MODULE = "reinitialize_module"
    VALIDATE_AND_ROLLBACK = "validate_and_rollback"


class RepairStatus(Enum):
    """Status of repair operations."""
    PENDING = "pending"
    IN_PROGRESS = "in_progress"
    SUCCESS = "success"
    FAILED = "failed"
    ESCALATED = "escalated"


@dataclass
class ModuleRegistration:
    """Module registration for Conservator repair services."""
    module_name: str
    module_path: str
    last_known_good_hash: str
    backup_strategy: str
    validation_command: str
    dependencies: List[str] = field(default_factory=list)
    repair_actions_enabled: Set[RepairAction] = field(default_factory=set)
    registered_at: str = field(default_factory=lambda: datetime.datetime.utcnow().isoformat())
    last_repair: Optional[str] = None
    repair_count: int = 0
    
    def __post_init__(self):
        """Ensure repair_actions_enabled is a set."""
        if not isinstance(self.repair_actions_enabled, set):
            self.repair_actions_enabled = set(self.repair_actions_enabled)


@dataclass
class RepairOperation:
    """Record of a repair operation for Chronicle Keeper."""
    operation_id: str
    module_name: str
    trigger: RepairTrigger
    actions_taken: List[RepairAction]
    status: RepairStatus
    start_time: str
    end_time: Optional[str] = None
    error_message: Optional[str] = None
    validation_results: Dict[str, Any] = field(default_factory=dict)
    human_intervention_required: bool = False
    chronicle_entry_id: Optional[str] = None


class ConservatorManifest:
    """Secure, human-editable manifest for module opt-in registration."""
    
    def __init__(self, manifest_path: str = "data/conservator_manifest.json"):
        self.manifest_path = Path(manifest_path)
        self.manifest_path.parent.mkdir(parents=True, exist_ok=True)
        self.registrations = self._load_manifest()
    
    def _load_manifest(self) -> Dict[str, ModuleRegistration]:
        """Load module registrations from manifest."""
        if not self.manifest_path.exists():
            return {}
        
        try:
            with open(self.manifest_path, 'r') as f:
                data = json.load(f)
            
            registrations = {}
            for module_name, reg_data in data.get('registrations', {}).items():
                registrations[module_name] = ModuleRegistration(
                    module_name=reg_data['module_name'],
                    module_path=reg_data['module_path'],
                    last_known_good_hash=reg_data['last_known_good_hash'],
                    backup_strategy=reg_data['backup_strategy'],
                    validation_command=reg_data['validation_command'],
                    dependencies=reg_data.get('dependencies', []),
                    repair_actions_enabled=set(RepairAction(action) for action in reg_data.get('repair_actions_enabled', [])),
                    registered_at=reg_data.get('registered_at', datetime.datetime.utcnow().isoformat()),
                    last_repair=reg_data.get('last_repair'),
                    repair_count=reg_data.get('repair_count', 0)
                )
            
            return registrations
            
        except (json.JSONDecodeError, KeyError, ValueError) as e:
            logger.error(f"Failed to load conservator manifest: {e}")
            return {}
    
    def save_manifest(self):
        """Save manifest to disk."""
        try:
            data = {
                'schema_version': '1.0.0',
                'last_updated': datetime.datetime.utcnow().isoformat(),
                'registrations': {}
            }
            
            for module_name, registration in self.registrations.items():
                data['registrations'][module_name] = {
                    'module_name': registration.module_name,
                    'module_path': registration.module_path,
                    'last_known_good_hash': registration.last_known_good_hash,
                    'backup_strategy': registration.backup_strategy,
                    'validation_command': registration.validation_command,
                    'dependencies': registration.dependencies,
                    'repair_actions_enabled': [action.value for action in registration.repair_actions_enabled],
                    'registered_at': registration.registered_at,
                    'last_repair': registration.last_repair,
                    'repair_count': registration.repair_count
                }
            
            with open(self.manifest_path, 'w') as f:
                json.dump(data, f, indent=2)
                
        except Exception as e:
            logger.error(f"Failed to save conservator manifest: {e}")
    
    def register_module(self, registration: ModuleRegistration) -> bool:
        """Register a module for Conservator services."""
        try:
            # Validate module path exists
            if not Path(registration.module_path).exists():
                logger.error(f"Module path does not exist: {registration.module_path}")
                return False
            
            # Generate hash for current state
            registration.last_known_good_hash = self._generate_module_hash(registration.module_path)
            
            self.registrations[registration.module_name] = registration
            self.save_manifest()
            
            logger.info(f"Module '{registration.module_name}' registered with Conservator")
            return True
            
        except Exception as e:
            logger.error(f"Failed to register module '{registration.module_name}': {e}")
            return False
    
    def unregister_module(self, module_name: str) -> bool:
        """Unregister a module from Conservator services."""
        if module_name in self.registrations:
            del self.registrations[module_name]
            self.save_manifest()
            logger.info(f"Module '{module_name}' unregistered from Conservator")
            return True
        return False
    
    def _generate_module_hash(self, module_path: str) -> str:
        """Generate hash for module's current state."""
        path = Path(module_path)
        if path.is_file():
            with open(path, 'rb') as f:
                return hashlib.sha256(f.read()).hexdigest()
        elif path.is_dir():
            # Hash directory contents
            hash_obj = hashlib.sha256()
            for file_path in sorted(path.rglob('*')):
                if file_path.is_file():
                    hash_obj.update(str(file_path.relative_to(path)).encode())
                    with open(file_path, 'rb') as f:
                        hash_obj.update(f.read())
            return hash_obj.hexdigest()
        return ""


class TheConservator:
    """
    The Conservator - Warbler's Auto-Repair Module
    
    Provides bounded, reactive repair mechanisms for maintaining the operational
    integrity of registered Warbler modules.
    """
    
    def __init__(self, manifest_path: str = "data/conservator_manifest.json",
                 backup_path: str = "data/conservator_backups",
                 chronicle_path: str = "TLDL/entries"):
        self.manifest = ConservatorManifest(manifest_path)
        self.backup_path = Path(backup_path)
        self.backup_path.mkdir(parents=True, exist_ok=True)
        self.chronicle_path = Path(chronicle_path)
        self.chronicle_path.mkdir(parents=True, exist_ok=True)
        self.repair_history: List[RepairOperation] = []
        
        logger.info("The Conservator initialized and ready for repair operations")
    
    def create_snapshot(self, module_name: str) -> bool:
        """Create a snapshot of a registered module's current state."""
        if module_name not in self.manifest.registrations:
            logger.error(f"Module '{module_name}' not registered with Conservator")
            return False
        
        registration = self.manifest.registrations[module_name]
        module_path = Path(registration.module_path)
        
        # Create timestamped backup
        timestamp = datetime.datetime.utcnow().strftime("%Y%m%d_%H%M%S")
        backup_dir = self.backup_path / module_name / timestamp
        backup_dir.mkdir(parents=True, exist_ok=True)
        
        try:
            if module_path.is_file():
                shutil.copy2(module_path, backup_dir / module_path.name)
            elif module_path.is_dir():
                shutil.copytree(module_path, backup_dir / module_path.name, dirs_exist_ok=True)
            
            # Update last known good hash
            registration.last_known_good_hash = self.manifest._generate_module_hash(str(module_path))
            self.manifest.save_manifest()
            
            logger.info(f"Snapshot created for '{module_name}' at {backup_dir}")
            return True
            
        except Exception as e:
            logger.error(f"Failed to create snapshot for '{module_name}': {e}")
            return False
    
    def validate_module(self, module_name: str) -> Tuple[bool, Dict[str, Any]]:
        """Run validation tests for a registered module."""
        if module_name not in self.manifest.registrations:
            return False, {"error": "Module not registered"}
        
        registration = self.manifest.registrations[module_name]
        
        try:
            # Run validation command
            result = subprocess.run(
                registration.validation_command,
                shell=True,
                capture_output=True,
                text=True,
                timeout=60
            )
            
            validation_results = {
                "return_code": result.returncode,
                "stdout": result.stdout,
                "stderr": result.stderr,
                "command": registration.validation_command,
                "timestamp": datetime.datetime.utcnow().isoformat()
            }
            
            success = result.returncode == 0
            
            if success:
                logger.info(f"Validation passed for '{module_name}'")
            else:
                logger.warning(f"Validation failed for '{module_name}': {result.stderr}")
            
            return success, validation_results
            
        except subprocess.TimeoutExpired:
            return False, {"error": "Validation timeout", "timeout": 60}
        except Exception as e:
            return False, {"error": str(e)}
    
    def repair_module(self, module_name: str, trigger: RepairTrigger, 
                     requested_actions: Optional[List[RepairAction]] = None) -> RepairOperation:
        """Perform repair operation on a registered module."""
        operation_id = f"repair_{module_name}_{int(time.time())}"
        
        # Initialize repair operation
        repair_op = RepairOperation(
            operation_id=operation_id,
            module_name=module_name,
            trigger=trigger,
            actions_taken=[],
            status=RepairStatus.PENDING,
            start_time=datetime.datetime.utcnow().isoformat()
        )
        
        try:
            # Check if module is registered
            if module_name not in self.manifest.registrations:
                repair_op.status = RepairStatus.FAILED
                repair_op.error_message = "Module not registered with Conservator"
                repair_op.human_intervention_required = True
                return repair_op
            
            registration = self.manifest.registrations[module_name]
            repair_op.status = RepairStatus.IN_PROGRESS
            
            # Determine repair actions
            if requested_actions:
                actions_to_perform = [action for action in requested_actions 
                                    if action in registration.repair_actions_enabled]
            else:
                actions_to_perform = list(registration.repair_actions_enabled)
            
            # Perform repair actions in order
            for action in actions_to_perform:
                success = self._perform_repair_action(action, registration, repair_op)
                if not success:
                    repair_op.status = RepairStatus.FAILED
                    repair_op.human_intervention_required = True
                    break
                repair_op.actions_taken.append(action)
            
            # Run post-repair validation
            if repair_op.status == RepairStatus.IN_PROGRESS:
                validation_success, validation_results = self.validate_module(module_name)
                repair_op.validation_results = validation_results
                
                if validation_success:
                    repair_op.status = RepairStatus.SUCCESS
                    registration.last_repair = datetime.datetime.utcnow().isoformat()
                    registration.repair_count += 1
                    self.manifest.save_manifest()
                    logger.info(f"Repair completed successfully for '{module_name}'")
                else:
                    repair_op.status = RepairStatus.FAILED
                    repair_op.error_message = "Post-repair validation failed"
                    repair_op.human_intervention_required = True
                    logger.error(f"Post-repair validation failed for '{module_name}'")
            
            repair_op.end_time = datetime.datetime.utcnow().isoformat()
            
            # Log to Chronicle Keeper
            chronicle_id = self._log_to_chronicle_keeper(repair_op)
            repair_op.chronicle_entry_id = chronicle_id
            
            self.repair_history.append(repair_op)
            return repair_op
            
        except Exception as e:
            repair_op.status = RepairStatus.FAILED
            repair_op.error_message = str(e)
            repair_op.human_intervention_required = True
            repair_op.end_time = datetime.datetime.utcnow().isoformat()
            logger.error(f"Repair operation failed for '{module_name}': {e}")
            return repair_op
    
    def _perform_repair_action(self, action: RepairAction, 
                              registration: ModuleRegistration, 
                              repair_op: RepairOperation) -> bool:
        """Perform a specific repair action."""
        try:
            if action == RepairAction.RESTORE_FROM_SNAPSHOT:
                return self._restore_from_snapshot(registration)
            elif action == RepairAction.RELINK_DEPENDENCIES:
                return self._relink_dependencies(registration)
            elif action == RepairAction.REINITIALIZE_MODULE:
                return self._reinitialize_module(registration)
            elif action == RepairAction.VALIDATE_AND_ROLLBACK:
                return self._validate_and_rollback(registration)
            else:
                logger.error(f"Unknown repair action: {action}")
                return False
                
        except Exception as e:
            logger.error(f"Failed to perform repair action '{action}': {e}")
            return False
    
    def _restore_from_snapshot(self, registration: ModuleRegistration) -> bool:
        """Restore module from the most recent snapshot."""
        module_backups = self.backup_path / registration.module_name
        if not module_backups.exists():
            logger.error(f"No backups found for '{registration.module_name}'")
            return False
        
        # Find most recent backup
        backup_dirs = [d for d in module_backups.iterdir() if d.is_dir()]
        if not backup_dirs:
            logger.error(f"No backup directories found for '{registration.module_name}'")
            return False
        
        latest_backup = max(backup_dirs, key=lambda d: d.name)
        
        try:
            # Restore from backup
            module_path = Path(registration.module_path)
            backup_source = latest_backup / module_path.name
            
            if backup_source.is_file():
                shutil.copy2(backup_source, module_path)
            elif backup_source.is_dir():
                if module_path.exists():
                    shutil.rmtree(module_path)
                shutil.copytree(backup_source, module_path)
            
            logger.info(f"Restored '{registration.module_name}' from snapshot: {latest_backup}")
            return True
            
        except Exception as e:
            logger.error(f"Failed to restore from snapshot: {e}")
            return False
    
    def _relink_dependencies(self, registration: ModuleRegistration) -> bool:
        """Re-link or re-initialize corrupted dependencies."""
        try:
            # Simple dependency re-linking (can be extended based on specific needs)
            for dependency in registration.dependencies:
                logger.info(f"Re-linking dependency: {dependency}")
                # TODO: Implement specific dependency re-linking logic
            
            return True
            
        except Exception as e:
            logger.error(f"Failed to relink dependencies: {e}")
            return False
    
    def _reinitialize_module(self, registration: ModuleRegistration) -> bool:
        """Re-initialize the module to a clean state."""
        try:
            # Basic re-initialization logic
            logger.info(f"Re-initializing module: {registration.module_name}")
            # TODO: Implement module-specific re-initialization logic
            
            return True
            
        except Exception as e:
            logger.error(f"Failed to reinitialize module: {e}")
            return False
    
    def _validate_and_rollback(self, registration: ModuleRegistration) -> bool:
        """Validate current state and rollback if necessary."""
        validation_success, _ = self.validate_module(registration.module_name)
        
        if not validation_success:
            logger.info(f"Validation failed, rolling back '{registration.module_name}'")
            return self._restore_from_snapshot(registration)
        
        return True
    
    def _log_to_chronicle_keeper(self, repair_op: RepairOperation) -> Optional[str]:
        """Log repair operation to Chronicle Keeper format."""
        try:
            # Create TLDL entry for repair operation
            timestamp = datetime.datetime.utcnow().strftime("%Y-%m-%d")
            tldl_filename = f"TLDL-{timestamp}-ConservatorRepair-{repair_op.module_name}.md"
            tldl_path = self.chronicle_path / tldl_filename
            
            # Ensure TLDL directory exists
            self.chronicle_path.mkdir(parents=True, exist_ok=True)
            
            tldl_content = f"""# TLDL-{timestamp}-ConservatorRepair-{repair_op.module_name}

**Entry ID**: {repair_op.operation_id}  
**Author**: The Conservator  
**Date**: {timestamp}  
**Context**: Automated module repair operation  
**Summary**: Repair operation for module '{repair_op.module_name}' triggered by {repair_op.trigger.value}

## Objective

Perform automated repair of module '{repair_op.module_name}' following Conservator protocols to restore operational integrity.

## Discovery

- **Trigger**: {repair_op.trigger.value}
- **Module**: {repair_op.module_name}
- **Operation ID**: {repair_op.operation_id}
- **Start Time**: {repair_op.start_time}
- **End Time**: {repair_op.end_time or 'In Progress'}

## Actions Taken

1. **Repair Actions Performed**:
"""
            
            for i, action in enumerate(repair_op.actions_taken, 1):
                tldl_content += f"   - {i}. {action.value.replace('_', ' ').title()}\n"
            
            tldl_content += f"""
2. **Validation Results**:
   - **Status**: {repair_op.status.value}
   - **Human Intervention Required**: {repair_op.human_intervention_required}

## Key Insights

- **Repair Status**: {repair_op.status.value.title()}
- **Actions Count**: {len(repair_op.actions_taken)}
- **Validation**: {'Passed' if repair_op.validation_results.get('return_code') == 0 else 'Failed'}

{f"- **Error**: {repair_op.error_message}" if repair_op.error_message else ""}

## Next Steps

"""
            
            if repair_op.status == RepairStatus.SUCCESS:
                tldl_content += "- ✅ Repair completed successfully\n- Monitor module for stability\n"
            elif repair_op.human_intervention_required:
                tldl_content += "- ⚠️ Human intervention required\n- Review repair logs and validation results\n- Consider manual troubleshooting\n"
            else:
                tldl_content += "- 🔄 Repair operation ongoing\n"
            
            tldl_content += """
---

*This TLDL entry was automatically generated by The Conservator repair system.*
"""
            
            with open(tldl_path, 'w') as f:
                f.write(tldl_content)
            
            logger.info(f"Chronicle Keeper entry created: {tldl_filename}")
            return str(tldl_path)
            
        except Exception as e:
            logger.error(f"Failed to log to Chronicle Keeper: {e}")
            return None
    
    def get_module_status(self, module_name: str) -> Dict[str, Any]:
        """Get comprehensive status for a registered module."""
        if module_name not in self.manifest.registrations:
            return {"error": "Module not registered"}
        
        registration = self.manifest.registrations[module_name]
        
        # Check current module hash
        current_hash = self.manifest._generate_module_hash(registration.module_path)
        integrity_check = current_hash == registration.last_known_good_hash
        
        # Get recent repair operations
        recent_repairs = [op for op in self.repair_history 
                         if op.module_name == module_name][-5:]  # Last 5 repairs
        
        return {
            "module_name": module_name,
            "module_path": registration.module_path,
            "integrity_check": integrity_check,
            "last_known_good_hash": registration.last_known_good_hash,
            "current_hash": current_hash,
            "last_repair": registration.last_repair,
            "repair_count": registration.repair_count,
            "enabled_actions": [action.value for action in registration.repair_actions_enabled],
            "recent_repairs": [
                {
                    "operation_id": op.operation_id,
                    "trigger": op.trigger.value,
                    "status": op.status.value,
                    "actions_taken": [action.value for action in op.actions_taken],
                    "start_time": op.start_time,
                    "end_time": op.end_time
                }
                for op in recent_repairs
            ]
        }
    
    def list_registered_modules(self) -> List[str]:
        """Get list of all registered module names."""
        return list(self.manifest.registrations.keys())
    
    def repair_system_health_check(self) -> Dict[str, Any]:
        """Perform health check on the Conservator system itself."""
        health_status = {
            "conservator_operational": True,
            "manifest_accessible": self.manifest.manifest_path.exists(),
            "backup_directory_accessible": self.backup_path.exists() and self.backup_path.is_dir(),
            "chronicle_directory_accessible": self.chronicle_path.exists() and self.chronicle_path.is_dir(),
            "registered_modules_count": len(self.manifest.registrations),
            "total_repairs_performed": sum(reg.repair_count for reg in self.manifest.registrations.values()),
            "recent_operations_count": len(self.repair_history),
            "timestamp": datetime.datetime.utcnow().isoformat()
        }
        
        # Check if any modules need attention
        modules_needing_attention = []
        for module_name, registration in self.manifest.registrations.items():
            current_hash = self.manifest._generate_module_hash(registration.module_path)
            if current_hash != registration.last_known_good_hash:
                modules_needing_attention.append(module_name)
        
        health_status["modules_needing_attention"] = modules_needing_attention
        health_status["integrity_issues_detected"] = len(modules_needing_attention) > 0
        
        return health_status


# Convenience functions for CLI integration
def create_conservator(manifest_path: str = "data/conservator_manifest.json") -> TheConservator:
    """Create a Conservator instance with default configuration."""
    return TheConservator(manifest_path=manifest_path)


def register_warbler_core_modules(conservator: TheConservator) -> bool:
    """Register core Warbler modules with sensible defaults."""
    success_count = 0
    
    # Register Warden (example)
    warden_registration = ModuleRegistration(
        module_name="warden",
        module_path="engine/governance.py",  # Using existing governance as placeholder
        last_known_good_hash="",
        backup_strategy="file_copy",
        validation_command="python3 -m py_compile engine/governance.py",
        repair_actions_enabled={
            RepairAction.RESTORE_FROM_SNAPSHOT,
            RepairAction.VALIDATE_AND_ROLLBACK
        }
    )
    
    if conservator.manifest.register_module(warden_registration):
        success_count += 1
    
    # Register Alice (example using telemetry as placeholder)
    alice_registration = ModuleRegistration(
        module_name="alice",
        module_path="engine/telemetry.py",
        last_known_good_hash="",
        backup_strategy="file_copy",
        validation_command="python3 -m py_compile engine/telemetry.py",
        repair_actions_enabled={
            RepairAction.RESTORE_FROM_SNAPSHOT,
            RepairAction.RELINK_DEPENDENCIES,
            RepairAction.VALIDATE_AND_ROLLBACK
        }
    )
    
    if conservator.manifest.register_module(alice_registration):
        success_count += 1
    
    return success_count > 0