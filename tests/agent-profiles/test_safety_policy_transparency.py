#!/usr/bin/env python3
"""
Test Suite for v0.8 Safety & Policy Transparency Layer

Comprehensive tests for tiered safety events, metadata transparency,
redaction transforms, and audit logging functionality.

🧙‍♂️ "Trust but verify - every safety guardian must prove their vigilance 
    through rigorous testing of their protective spells." - Bootstrap Sentinel
"""

import sys
import time
import json
import tempfile
import shutil
from pathlib import Path
from typing import Dict, Any

# Add engine to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent / "engine"))

from intervention_metrics import InterventionMetrics, InterventionType, SafetyEventLevel, AcceptanceStatus
from redaction_transforms import RedactionTransforms, RedactionType
from safety_policy_transparency import SafetyPolicyTransparency, SafetyEvent, PolicyAction


class SafetySystemTestSuite:
    """Comprehensive test suite for v0.8 safety features"""
    
    def __init__(self):
        self.test_results = []
        self.temp_dir = None
    
    def setup_test_environment(self):
        """Set up temporary test environment"""
        self.temp_dir = Path(tempfile.mkdtemp())
        print(f"🔧 Test environment: {self.temp_dir}")
    
    def cleanup_test_environment(self):
        """Clean up test environment"""
        if self.temp_dir and self.temp_dir.exists():
            shutil.rmtree(self.temp_dir)
    
    def run_test(self, test_name: str, test_func) -> bool:
        """Run a single test and record results"""
        print(f"\n🧪 Testing: {test_name}")
        try:
            test_func()
            print(f"   ✅ PASS: {test_name}")
            self.test_results.append({"test": test_name, "status": "PASS", "error": None})
            return True
        except Exception as e:
            print(f"   ❌ FAIL: {test_name} - {str(e)}")
            self.test_results.append({"test": test_name, "status": "FAIL", "error": str(e)})
            return False
    
    def test_redaction_transforms(self):
        """Test PII redaction and content filtering"""
        redactor = RedactionTransforms()
        
        # Test PII redaction
        test_content = "Contact john.doe@example.com or call (555) 123-4567"
        redacted, metadata = redactor.apply_redaction(test_content)
        
        assert "[EMAIL_REDACTED]" in redacted, "Email should be redacted"
        assert "[PHONE_REDACTED]" in redacted, "Phone should be redacted"
        assert metadata["redaction_applied"], "Redaction should be applied"
        assert metadata["redaction_counts"]["email_count"] == 1, "Should count 1 email"
        assert metadata["redaction_counts"]["phone_count"] == 1, "Should count 1 phone"
        
        # Test transparency report
        report = redactor.create_transparency_report(test_content, redacted, metadata)
        assert "redaction_summary" in report, "Should include redaction summary"
        assert "transparency_notes" in report, "Should include transparency notes"
    
    def test_safety_event_creation(self):
        """Test tiered safety event system"""
        audit_path = self.temp_dir / "test_safety_audit.jsonl"
        safety_layer = SafetyPolicyTransparency(audit_log_path=str(audit_path))
        
        # Test each safety level
        levels_to_test = [
            (SafetyEventLevel.NOTICE, PolicyAction.AUDIT_ONLY),
            (SafetyEventLevel.WARN, PolicyAction.MODIFY),
            (SafetyEventLevel.BLOCK, PolicyAction.BLOCK),
            (SafetyEventLevel.ESCALATE, PolicyAction.ESCALATE)
        ]
        
        created_events = []
        for safety_level, expected_action in levels_to_test:
            event = safety_layer.create_safety_event(
                safety_level=safety_level,
                event_type=f"test_{safety_level.value}",
                context={"test": True},
                reasoning=f"Testing {safety_level.value} level",
                content="Test content with john.doe@example.com",
                user_id="test_user"
            )
            
            assert event.safety_level == safety_level, f"Safety level should be {safety_level.value}"
            assert event.policy_action == expected_action, f"Policy action should be {expected_action.value}"
            assert event.event_id.startswith("safety_"), "Event ID should have safety prefix"
            
            created_events.append(event)
        
        # Verify audit log was created
        assert audit_path.exists(), "Audit log should be created"
        
        # Read and verify audit entries
        with open(audit_path, 'r') as f:
            audit_lines = f.readlines()
        
        assert len(audit_lines) == 4, "Should have 4 audit entries"
        
        for line in audit_lines:
            entry = json.loads(line.strip())
            assert entry["event_type"] == "safety_event", "Should be safety event"
            assert "transparency_metadata" in entry, "Should include transparency metadata"
    
    def test_intervention_safety_integration(self):
        """Test integration between intervention system and safety layer"""
        metrics_path = self.temp_dir / "test_intervention_metrics.json"
        metrics = InterventionMetrics(str(metrics_path))
        
        # Test intervention with safety event
        intervention_id = metrics.record_intervention(
            intervention_type=InterventionType.SAFETY_INTERVENTION,
            context={"test": True, "risk_level": "high"},
            original_input="Test input with sensitive data: john.doe@example.com",
            suggested_output="Redacted version",
            reasoning="Safety intervention required",
            safety_event_level=SafetyEventLevel.WARN,
            enable_redaction=True,
            user_id="test_user"
        )
        
        assert intervention_id.startswith("int_"), "Should return intervention ID"
        
        # Verify intervention was recorded with safety metadata
        interventions = metrics.data["interventions"]
        assert len(interventions) > 0, "Should have recorded intervention"
        
        latest_intervention = interventions[-1]
        assert latest_intervention.intervention_id == intervention_id, "Should match intervention ID"
        assert latest_intervention.safety_event_level == SafetyEventLevel.WARN, "Should have safety level"
        assert latest_intervention.redaction_applied, "Should have applied redaction"
        assert latest_intervention.audit_trail is not None, "Should have audit trail"
    
    def test_policy_transparency_metadata(self):
        """Test policy transparency and metadata annotation"""
        audit_path = self.temp_dir / "test_transparency_audit.jsonl"
        safety_layer = SafetyPolicyTransparency(audit_log_path=str(audit_path))
        
        # Create event with rich metadata - use non-escalating event type
        event = safety_layer.create_safety_event(
            safety_level=SafetyEventLevel.BLOCK,
            event_type="content_filtering",  # Changed from policy_violation
            context={
                "user_action": "content_generation",
                "violation_type": "unsafe_content",
                "risk_score": 0.85
            },
            reasoning="Content violates safety guidelines",
            content="This is sensitive content that should be blocked",
            user_id="test_user"
        )
        
        # Verify metadata transparency
        assert event.policy_action == PolicyAction.BLOCK, "Should block the action"
        assert "risk_score" in event.context, "Should preserve context metadata"
        
        # Read audit entry and verify transparency metadata
        with open(audit_path, 'r') as f:
            audit_entry = json.loads(f.readline().strip())
        
        transparency_meta = audit_entry["transparency_metadata"]
        assert transparency_meta["policy_version"], "Should include policy version"
        assert transparency_meta["audit_trail_complete"], "Should confirm complete audit trail"
        assert transparency_meta["automated_action"] is True, "Should indicate automated action for BLOCK"
    
    def test_audit_log_format(self):
        """Test JSONL audit log format and structure"""
        audit_path = self.temp_dir / "test_audit_format.jsonl"
        safety_layer = SafetyPolicyTransparency(audit_log_path=str(audit_path))
        
        # Create multiple events
        events = []
        for i in range(3):
            event = safety_layer.create_safety_event(
                safety_level=SafetyEventLevel.NOTICE,
                event_type=f"test_event_{i}",
                context={"event_number": i},
                reasoning=f"Test event {i}",
                user_id=f"user_{i}"
            )
            events.append(event)
        
        # Verify JSONL format
        with open(audit_path, 'r') as f:
            lines = f.readlines()
        
        assert len(lines) == 3, "Should have 3 lines"
        
        for i, line in enumerate(lines):
            entry = json.loads(line.strip())
            
            # Verify required fields
            required_fields = ["timestamp", "event_type", "event_data", "transparency_metadata"]
            for field in required_fields:
                assert field in entry, f"Should include {field}"
            
            # Verify event data structure
            event_data = entry["event_data"]
            assert event_data["event_type"] == f"test_event_{i}", f"Should match event type {i}"
            assert event_data["user_id"] == f"user_{i}", f"Should match user {i}"
    
    def test_safety_analytics(self):
        """Test safety analytics and reporting"""
        audit_path = self.temp_dir / "test_analytics_audit.jsonl"
        safety_layer = SafetyPolicyTransparency(audit_log_path=str(audit_path))
        
        # Create diverse safety events
        test_events = [
            (SafetyEventLevel.NOTICE, "info_event"),
            (SafetyEventLevel.WARN, "warning_event"),
            (SafetyEventLevel.BLOCK, "block_event"),
            (SafetyEventLevel.ESCALATE, "escalation_event"),
            (SafetyEventLevel.WARN, "another_warning")
        ]
        
        for level, event_type in test_events:
            safety_layer.create_safety_event(
                safety_level=level,
                event_type=event_type,
                context={"test": True},
                reasoning=f"Test {event_type}",
                user_id="analytics_user"
            )
        
        # Test analytics
        analytics = safety_layer.get_safety_analytics(user_id="analytics_user")
        
        assert analytics["total_safety_events"] == 5, "Should count 5 events"
        assert analytics["events_by_safety_level"]["notice"] == 1, "Should count 1 notice"
        assert analytics["events_by_safety_level"]["warn"] == 2, "Should count 2 warnings"
        assert analytics["events_by_safety_level"]["block"] == 1, "Should count 1 block"
        assert analytics["events_by_safety_level"]["escalate"] == 1, "Should count 1 escalation"
        
        # Test transparency report
        report = safety_layer.create_transparency_report()
        assert report["report_metadata"]["total_audit_entries"] == 5, "Should count 5 audit entries"
        assert report["safety_summary"]["total_safety_events"] == 5, "Should count 5 safety events"
    
    def test_escalation_handling(self):
        """Test escalation process and human oversight"""
        audit_path = self.temp_dir / "test_escalation_audit.jsonl"
        safety_layer = SafetyPolicyTransparency(audit_log_path=str(audit_path))
        
        # Create escalation event
        event = safety_layer.create_safety_event(
            safety_level=SafetyEventLevel.ESCALATE,
            event_type="security_violation",
            context={"severity": "critical", "threat_level": "high"},
            reasoning="Critical security breach detected",
            content="Sensitive security incident data",
            user_id="security_user"
        )
        
        # Verify escalation handling
        assert event.policy_action == PolicyAction.ESCALATE, "Should escalate"
        assert event.escalation_path is not None, "Should have escalation path"
        assert event.escalation_path.startswith("esc_"), "Escalation path should have prefix"
        
        # Verify audit entry indicates human oversight required
        with open(audit_path, 'r') as f:
            audit_entry = json.loads(f.readline().strip())
        
        transparency_meta = audit_entry["transparency_metadata"]
        assert transparency_meta["human_oversight_required"], "Should require human oversight"
        assert not transparency_meta["automated_action"], "Should not be automated action"
    
    def run_all_tests(self):
        """Run complete test suite"""
        print("🧙‍♂️ v0.8 Safety & Policy Transparency Layer Test Suite")
        print("=" * 60)
        
        self.setup_test_environment()
        
        try:
            tests = [
                ("Redaction Transforms", self.test_redaction_transforms),
                ("Safety Event Creation", self.test_safety_event_creation),
                ("Intervention Safety Integration", self.test_intervention_safety_integration),
                ("Policy Transparency Metadata", self.test_policy_transparency_metadata),
                ("Audit Log Format", self.test_audit_log_format),
                ("Safety Analytics", self.test_safety_analytics),
                ("Escalation Handling", self.test_escalation_handling)
            ]
            
            passed = 0
            for test_name, test_func in tests:
                if self.run_test(test_name, test_func):
                    passed += 1
            
            print(f"\n📊 Test Results: {passed}/{len(tests)} tests passed")
            
            if passed == len(tests):
                print("🎉 All tests passed! Safety system is ready for deployment.")
            else:
                print("⚠️ Some tests failed. Review and fix issues before deployment.")
                
        finally:
            self.cleanup_test_environment()
        
        return passed == len(tests)


def main():
    """Run the safety system test suite"""
    test_suite = SafetySystemTestSuite()
    success = test_suite.run_all_tests()
    
    if success:
        print("\n✅ Safety & Policy Transparency Layer v0.8 - All tests passed!")
    else:
        print("\n❌ Some tests failed - please review and fix issues.")
        sys.exit(1)


if __name__ == "__main__":
    main()