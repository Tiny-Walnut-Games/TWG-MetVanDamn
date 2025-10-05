#!/usr/bin/env python3
"""
Tests for The Conservator - Warbler Auto-Repair Module

Tests the core functionality of The Conservator including:
- Module registration and manifest management
- Repair operations and triggers
- Snapshot creation and restoration
- Chronicle Keeper integration
- CLI interface
"""

import unittest
import tempfile
import shutil
import json
import os
from pathlib import Path
import sys

# Add project root to path
project_root = Path(__file__).parent.parent
sys.path.insert(0, str(project_root))

from engine.conservator import (
    TheConservator, ConservatorManifest, ModuleRegistration, RepairOperation,
    RepairTrigger, RepairAction, RepairStatus, create_conservator
)


class TestConservatorManifest(unittest.TestCase):
    """Test the ConservatorManifest class."""
    
    def setUp(self):
        """Set up test environment."""
        self.temp_dir = tempfile.mkdtemp()
        self.manifest_path = Path(self.temp_dir) / "test_manifest.json"
        self.manifest = ConservatorManifest(str(self.manifest_path))
        
        # Create a test module file
        self.test_module_path = Path(self.temp_dir) / "test_module.py"
        with open(self.test_module_path, 'w') as f:
            f.write("# Test module\nprint('Hello, Conservator!')\n")
    
    def tearDown(self):
        """Clean up test environment."""
        shutil.rmtree(self.temp_dir)
    
    def test_manifest_creation(self):
        """Test manifest creation and basic functionality."""
        self.assertTrue(self.manifest_path.parent.exists())
        self.assertEqual(len(self.manifest.registrations), 0)
    
    def test_module_registration(self):
        """Test module registration."""
        registration = ModuleRegistration(
            module_name="test_module",
            module_path=str(self.test_module_path),
            last_known_good_hash="",
            backup_strategy="file_copy",
            validation_command=f"python3 -c 'import py_compile; py_compile.compile(\"{self.test_module_path}\")'",
            repair_actions_enabled={RepairAction.RESTORE_FROM_SNAPSHOT}
        )
        
        success = self.manifest.register_module(registration)
        self.assertTrue(success)
        self.assertIn("test_module", self.manifest.registrations)
        
        # Check that hash was generated
        reg = self.manifest.registrations["test_module"]
        self.assertNotEqual(reg.last_known_good_hash, "")
    
    def test_manifest_persistence(self):
        """Test manifest save and load functionality."""
        # Register a module
        registration = ModuleRegistration(
            module_name="test_module",
            module_path=str(self.test_module_path),
            last_known_good_hash="test_hash",
            backup_strategy="file_copy",
            validation_command="echo 'test'",
            repair_actions_enabled={RepairAction.RESTORE_FROM_SNAPSHOT}
        )
        
        self.manifest.register_module(registration)
        
        # Create new manifest instance and verify data persisted
        new_manifest = ConservatorManifest(str(self.manifest_path))
        self.assertIn("test_module", new_manifest.registrations)
        
        reg = new_manifest.registrations["test_module"]
        self.assertEqual(reg.module_name, "test_module")
        self.assertEqual(reg.backup_strategy, "file_copy")
        self.assertIn(RepairAction.RESTORE_FROM_SNAPSHOT, reg.repair_actions_enabled)
    
    def test_module_unregistration(self):
        """Test module unregistration."""
        # Register a module first
        registration = ModuleRegistration(
            module_name="test_module",
            module_path=str(self.test_module_path),
            last_known_good_hash="",
            backup_strategy="file_copy",
            validation_command="echo 'test'",
            repair_actions_enabled={RepairAction.RESTORE_FROM_SNAPSHOT}
        )
        
        self.manifest.register_module(registration)
        self.assertIn("test_module", self.manifest.registrations)
        
        # Unregister
        success = self.manifest.unregister_module("test_module")
        self.assertTrue(success)
        self.assertNotIn("test_module", self.manifest.registrations)
        
        # Test unregistering non-existent module
        success = self.manifest.unregister_module("non_existent")
        self.assertFalse(success)


class TestTheConservator(unittest.TestCase):
    """Test The Conservator main class."""
    
    def setUp(self):
        """Set up test environment."""
        self.temp_dir = tempfile.mkdtemp()
        self.manifest_path = Path(self.temp_dir) / "test_manifest.json"
        self.backup_path = Path(self.temp_dir) / "backups"
        self.chronicle_path = Path(self.temp_dir) / "chronicles"
        
        self.conservator = TheConservator(
            manifest_path=str(self.manifest_path),
            backup_path=str(self.backup_path),
            chronicle_path=str(self.chronicle_path)
        )
        
        # Create a test module file
        self.test_module_path = Path(self.temp_dir) / "test_module.py"
        with open(self.test_module_path, 'w') as f:
            f.write("# Test module\nprint('Hello, Conservator!')\n")
        
        # Register the test module
        registration = ModuleRegistration(
            module_name="test_module",
            module_path=str(self.test_module_path),
            last_known_good_hash="",
            backup_strategy="file_copy",
            validation_command="python3 -m py_compile " + str(self.test_module_path),
            repair_actions_enabled={
                RepairAction.RESTORE_FROM_SNAPSHOT,
                RepairAction.VALIDATE_AND_ROLLBACK
            }
        )
        
        self.conservator.manifest.register_module(registration)
    
    def tearDown(self):
        """Clean up test environment."""
        shutil.rmtree(self.temp_dir)
    
    def test_conservator_initialization(self):
        """Test Conservator initialization."""
        self.assertTrue(self.backup_path.exists())
        self.assertTrue(self.chronicle_path.exists())
        self.assertIn("test_module", self.conservator.manifest.registrations)
    
    def test_snapshot_creation(self):
        """Test snapshot creation."""
        success = self.conservator.create_snapshot("test_module")
        self.assertTrue(success)
        
        # Check that backup was created
        module_backup_dir = self.backup_path / "test_module"
        self.assertTrue(module_backup_dir.exists())
        
        # Check that backup contains our file
        backup_dirs = [d for d in module_backup_dir.iterdir() if d.is_dir()]
        self.assertGreater(len(backup_dirs), 0)
        
        backup_file = backup_dirs[0] / self.test_module_path.name
        self.assertTrue(backup_file.exists())
    
    def test_module_validation(self):
        """Test module validation."""
        success, results = self.conservator.validate_module("test_module")
        
        # Should succeed because our test module is valid Python
        self.assertTrue(success)
        self.assertIn("return_code", results)
        self.assertEqual(results["return_code"], 0)
    
    def test_repair_operation(self):
        """Test repair operation."""
        # Create a snapshot first
        self.conservator.create_snapshot("test_module")
        
        # Perform repair operation
        repair_op = self.conservator.repair_module(
            "test_module",
            RepairTrigger.EXPLICIT_HUMAN_COMMAND,
            [RepairAction.VALIDATE_AND_ROLLBACK]
        )
        
        self.assertEqual(repair_op.module_name, "test_module")
        self.assertEqual(repair_op.trigger, RepairTrigger.EXPLICIT_HUMAN_COMMAND)
        self.assertIn(RepairAction.VALIDATE_AND_ROLLBACK, repair_op.actions_taken)
        self.assertEqual(repair_op.status, RepairStatus.SUCCESS)
    
    def test_module_status(self):
        """Test module status retrieval."""
        status = self.conservator.get_module_status("test_module")
        
        self.assertEqual(status["module_name"], "test_module")
        self.assertIn("integrity_check", status)
        self.assertIn("current_hash", status)
        self.assertEqual(status["repair_count"], 0)
    
    def test_system_health_check(self):
        """Test system health check."""
        health = self.conservator.repair_system_health_check()
        
        self.assertTrue(health["conservator_operational"])
        self.assertTrue(health["manifest_accessible"])
        self.assertTrue(health["backup_directory_accessible"])
        self.assertTrue(health["chronicle_directory_accessible"])
        self.assertEqual(health["registered_modules_count"], 1)
    
    def test_list_registered_modules(self):
        """Test listing registered modules."""
        modules = self.conservator.list_registered_modules()
        self.assertEqual(modules, ["test_module"])
    
    def test_chronicle_keeper_integration(self):
        """Test Chronicle Keeper integration."""
        # Perform a repair operation that should create a chronicle entry
        repair_op = self.conservator.repair_module(
            "test_module",
            RepairTrigger.EXPLICIT_HUMAN_COMMAND
        )
        
        # Check that chronicle entry was created
        self.assertIsNotNone(repair_op.chronicle_entry_id)
        
        chronicle_file = Path(repair_op.chronicle_entry_id)
        self.assertTrue(chronicle_file.exists())
        
        # Check content
        with open(chronicle_file, 'r') as f:
            content = f.read()
        
        self.assertIn("The Conservator", content)
        self.assertIn("test_module", content)
        self.assertIn("explicit_human_command", content)


class TestRepairScenarios(unittest.TestCase):
    """Test various repair scenarios."""
    
    def setUp(self):
        """Set up test environment."""
        self.temp_dir = tempfile.mkdtemp()
        self.manifest_path = Path(self.temp_dir) / "test_manifest.json"
        self.backup_path = Path(self.temp_dir) / "backups"
        self.chronicle_path = Path(self.temp_dir) / "chronicles"
        
        self.conservator = TheConservator(
            manifest_path=str(self.manifest_path),
            backup_path=str(self.backup_path),
            chronicle_path=str(self.chronicle_path)
        )
        
        # Create a test module file
        self.test_module_path = Path(self.temp_dir) / "test_module.py"
        with open(self.test_module_path, 'w') as f:
            f.write("# Original test module\nprint('Original version')\n")
        
        # Register the test module
        registration = ModuleRegistration(
            module_name="test_module",
            module_path=str(self.test_module_path),
            last_known_good_hash="",
            backup_strategy="file_copy",
            validation_command="python3 -m py_compile " + str(self.test_module_path),
            repair_actions_enabled={
                RepairAction.RESTORE_FROM_SNAPSHOT,
                RepairAction.VALIDATE_AND_ROLLBACK
            }
        )
        
        self.conservator.manifest.register_module(registration)
        
        # Create initial snapshot
        self.conservator.create_snapshot("test_module")
    
    def tearDown(self):
        """Clean up test environment."""
        shutil.rmtree(self.temp_dir)
    
    def test_corruption_and_restore(self):
        """Test module corruption detection and restoration."""
        # Corrupt the module
        with open(self.test_module_path, 'w') as f:
            f.write("# Corrupted module\nprint('Corrupted version'\nimport nonexistent_module\n")
        
        # Verify corruption is detected
        status = self.conservator.get_module_status("test_module")
        self.assertFalse(status["integrity_check"])
        
        # Perform restore repair
        repair_op = self.conservator.repair_module(
            "test_module",
            RepairTrigger.MODULE_CRASH,
            [RepairAction.RESTORE_FROM_SNAPSHOT]
        )
        
        self.assertEqual(repair_op.status, RepairStatus.SUCCESS)
        self.assertIn(RepairAction.RESTORE_FROM_SNAPSHOT, repair_op.actions_taken)
        
        # Verify restoration
        with open(self.test_module_path, 'r') as f:
            content = f.read()
        
        self.assertIn("Original version", content)
        self.assertNotIn("Corrupted version", content)
    
    def test_failed_validation_escalation(self):
        """Test escalation when validation fails."""
        # Register a module with a failing validation command
        bad_module_path = Path(self.temp_dir) / "bad_module.py"
        with open(bad_module_path, 'w') as f:
            f.write("# Module with syntax error\nprint('Hello'\n")  # Missing closing parenthesis
        
        registration = ModuleRegistration(
            module_name="bad_module",
            module_path=str(bad_module_path),
            last_known_good_hash="",
            backup_strategy="file_copy",
            validation_command="python3 -m py_compile " + str(bad_module_path),
            repair_actions_enabled={RepairAction.VALIDATE_AND_ROLLBACK}
        )
        
        self.conservator.manifest.register_module(registration)
        
        # Attempt repair
        repair_op = self.conservator.repair_module(
            "bad_module",
            RepairTrigger.FAILED_CORE_TEST
        )
        
        # Should fail and require human intervention
        self.assertEqual(repair_op.status, RepairStatus.FAILED)
        self.assertTrue(repair_op.human_intervention_required)
    
    def test_unregistered_module_repair(self):
        """Test repair attempt on unregistered module."""
        repair_op = self.conservator.repair_module(
            "nonexistent_module",
            RepairTrigger.EXPLICIT_HUMAN_COMMAND
        )
        
        self.assertEqual(repair_op.status, RepairStatus.FAILED)
        self.assertTrue(repair_op.human_intervention_required)
        self.assertIn("not registered", repair_op.error_message)


def run_conservator_tests():
    """Run all Conservator tests."""
    print("🧪 Running The Conservator Test Suite...")
    
    # Create test suite
    test_classes = [
        TestConservatorManifest,
        TestTheConservator,
        TestRepairScenarios
    ]
    
    suite = unittest.TestSuite()
    
    for test_class in test_classes:
        tests = unittest.TestLoader().loadTestsFromTestCase(test_class)
        suite.addTests(tests)
    
    # Run tests
    runner = unittest.TextTestRunner(verbosity=2)
    result = runner.run(suite)
    
    # Print summary
    print(f"\n📊 Test Results:")
    print(f"   Tests Run: {result.testsRun}")
    print(f"   Failures: {len(result.failures)}")
    print(f"   Errors: {len(result.errors)}")
    
    if result.failures:
        print(f"\n❌ Failures:")
        for test, traceback in result.failures:
            print(f"   {test}: {traceback}")
    
    if result.errors:
        print(f"\n💥 Errors:")
        for test, traceback in result.errors:
            print(f"   {test}: {traceback}")
    
    success = len(result.failures) == 0 and len(result.errors) == 0
    
    if success:
        print(f"\n✅ All tests passed! The Conservator is ready for duty.")
    else:
        print(f"\n❌ Some tests failed. Please review and fix issues before deploying.")
    
    return success


if __name__ == '__main__':
    import sys
    
    success = run_conservator_tests()
    sys.exit(0 if success else 1)