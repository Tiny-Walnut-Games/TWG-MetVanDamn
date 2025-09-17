#!/usr/bin/env python3
"""
Privacy Hooks for Cross-Cutting PII Scrubbing & Privacy Policies

Provides configurable privacy hooks that are applied before anchor injection
to ensure sensitive data is properly scrubbed and privacy policies are enforced.

🧙‍♂️ "Privacy is not secrecy, but the right to choose what remains hidden - 
    and the wisdom to protect it before it spreads." - Bootstrap Sentinel
"""

import sys
from pathlib import Path
from typing import Dict, Any, Optional, Tuple, List
from dataclasses import dataclass
from enum import Enum

# Add engine to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent))

from redaction_transforms import RedactionTransforms, RedactionType
from safety_policy_transparency import SafetyPolicyTransparency, SafetyEventLevel


class PrivacyPolicyLevel(Enum):
    """Privacy policy strictness levels"""
    PERMISSIVE = "permissive"  # Basic PII patterns only
    STANDARD = "standard"      # Standard corporate policies
    STRICT = "strict"          # High-security environments
    CUSTOM = "custom"          # Custom configuration


@dataclass
class PrivacyPolicy:
    """Configuration for privacy enforcement"""
    level: PrivacyPolicyLevel
    enable_pii_scrubbing: bool = True
    enable_audit_logging: bool = True
    enable_encrypted_audit: bool = False
    custom_patterns: Optional[List[str]] = None
    context_filters: Optional[Dict[str, Any]] = None
    

class PrivacyHooks:
    """
    Privacy enforcement hooks for anchor injection and content processing
    
    Provides configurable PII scrubbing and privacy policy enforcement
    that integrates with existing safety and transparency systems.
    """
    
    def __init__(self, policy: Optional[PrivacyPolicy] = None):
        self.policy = policy or PrivacyPolicy(level=PrivacyPolicyLevel.STANDARD)
        
        # Initialize redaction engine with privacy-specific configuration
        self.redaction_engine = RedactionTransforms()
        self.safety_system = SafetyPolicyTransparency()
        
        # Privacy-specific metrics
        self.metrics = {
            "pii_scrubs_applied": 0,
            "privacy_violations_detected": 0,
            "content_blocked_for_privacy": 0,
            "audit_events_encrypted": 0
        }
    
    def scrub_content_for_anchor_injection(
        self,
        concept_text: str,
        context: Dict[str, Any],
        utterance_id: Optional[str] = None
    ) -> Tuple[str, Dict[str, Any]]:
        """
        Primary privacy hook: Scrub content before anchor injection
        
        Returns:
            Tuple[str, Dict[str, Any]]: (scrubbed_content, privacy_metadata)
        """
        if not self.policy.enable_pii_scrubbing:
            return concept_text, {"privacy_scrubbing": "disabled"}
        
        # Apply redaction based on privacy policy level
        redaction_context = self._get_privacy_redaction_context(context)
        scrubbed_content, redaction_metadata = self.redaction_engine.apply_redaction(
            concept_text, redaction_context
        )
        
        # Create privacy metadata
        privacy_metadata = {
            "privacy_policy_level": self.policy.level.value,
            "pii_detected": redaction_metadata.get("redaction_applied", False),
            "redaction_count": sum(redaction_metadata.get(key, 0) 
                                 for key in redaction_metadata 
                                 if key.endswith("_count")),
            "original_length": len(concept_text),
            "scrubbed_length": len(scrubbed_content),
            "privacy_hook_applied": True
        }
        
        # Update metrics
        if redaction_metadata.get("redaction_applied", False):
            self.metrics["pii_scrubs_applied"] += 1
        
        # Log privacy event if content was modified
        if scrubbed_content != concept_text:
            self._log_privacy_event(
                event_type="pii_scrubbing",
                original_content=concept_text,
                scrubbed_content=scrubbed_content,
                context=context,
                metadata=privacy_metadata,
                utterance_id=utterance_id
            )
        
        return scrubbed_content, privacy_metadata
    
    def validate_privacy_compliance(
        self,
        content: str,
        context: Dict[str, Any]
    ) -> Tuple[bool, List[str]]:
        """
        Validate content against privacy policies
        
        Returns:
            Tuple[bool, List[str]]: (is_compliant, violation_reasons)
        """
        violations = []
        
        # Check for policy-specific violations
        if self.policy.level == PrivacyPolicyLevel.STRICT:
            # In strict mode, any PII detection is a violation
            _, redaction_metadata = self.redaction_engine.apply_redaction(content)
            if redaction_metadata.get("redaction_applied", False):
                violations.append("PII detected in strict privacy mode")
        
        # Check custom patterns if configured
        if self.policy.custom_patterns:
            for pattern in self.policy.custom_patterns:
                import re
                if re.search(pattern, content, re.IGNORECASE):
                    violations.append(f"Custom privacy pattern matched: {pattern}")
        
        # Update metrics
        if violations:
            self.metrics["privacy_violations_detected"] += 1
        
        return len(violations) == 0, violations
    
    def _get_privacy_redaction_context(self, context: Dict[str, Any]) -> Dict[str, Any]:
        """Get redaction context based on privacy policy"""
        base_context = context.copy()
        
        # Set redaction strictness based on privacy policy
        if self.policy.level == PrivacyPolicyLevel.PERMISSIVE:
            base_context["redaction_mode"] = "permissive"
        elif self.policy.level == PrivacyPolicyLevel.STANDARD:
            base_context["redaction_mode"] = "standard"
        elif self.policy.level == PrivacyPolicyLevel.STRICT:
            base_context["redaction_mode"] = "strict"
        else:  # CUSTOM
            base_context["redaction_mode"] = "custom"
            base_context["custom_patterns"] = self.policy.custom_patterns or []
        
        # Apply context filters if configured
        if self.policy.context_filters:
            base_context.update(self.policy.context_filters)
        
        return base_context
    
    def _log_privacy_event(
        self,
        event_type: str,
        original_content: str,
        scrubbed_content: str,
        context: Dict[str, Any],
        metadata: Dict[str, Any],
        utterance_id: Optional[str] = None
    ):
        """Log privacy event through safety system"""
        try:
            # Determine safety level based on privacy impact
            safety_level = SafetyEventLevel.NOTICE
            if metadata.get("redaction_count", 0) > 3:
                safety_level = SafetyEventLevel.WARN
            elif "strict" in metadata.get("privacy_policy_level", ""):
                safety_level = SafetyEventLevel.WARN
            
            # Create privacy-focused safety event
            privacy_context = context.copy()
            privacy_context.update({
                "privacy_event_type": event_type,
                "privacy_metadata": metadata,
                "utterance_id": utterance_id,
                "original_content_hash": hash(original_content),
                "scrubbed_content_hash": hash(scrubbed_content)
            })
            
            # Don't include actual content in audit for privacy
            if not self.policy.enable_encrypted_audit:
                privacy_context["content_note"] = "Content excluded for privacy"
            
            safety_event = self.safety_system.create_safety_event(
                safety_level=safety_level,
                event_type=f"privacy_{event_type}",
                context=privacy_context,
                reasoning="Privacy policy enforcement applied before anchor injection",
                content=scrubbed_content if self.policy.enable_encrypted_audit else None,
                user_id=context.get("user_id", "default")
            )
            
            # Handle encrypted audit if enabled
            if self.policy.enable_encrypted_audit:
                self._handle_encrypted_audit(safety_event, original_content, scrubbed_content)
            
        except Exception as e:
            # Don't fail the main flow if privacy logging fails
            print(f"⚠️ Privacy event logging failed: {e}")
    
    def _handle_encrypted_audit(self, safety_event, original_content: str, scrubbed_content: str):
        """Handle encrypted audit logging (placeholder for full implementation)"""
        # For now, just mark that encryption would be applied
        # In a full implementation, this would use proper encryption
        safety_event.context["encrypted_audit_available"] = True
        safety_event.context["original_content_encrypted"] = f"[ENCRYPTED:{hash(original_content)}]"
        self.metrics["audit_events_encrypted"] += 1
    
    def get_privacy_metrics(self) -> Dict[str, Any]:
        """Get privacy enforcement metrics"""
        return {
            **self.metrics,
            "privacy_policy_level": self.policy.level.value,
            "pii_scrubbing_enabled": self.policy.enable_pii_scrubbing,
            "encrypted_audit_enabled": self.policy.enable_encrypted_audit
        }


def get_default_privacy_hooks() -> PrivacyHooks:
    """Get default privacy hooks instance"""
    return PrivacyHooks(PrivacyPolicy(level=PrivacyPolicyLevel.STANDARD))


def create_privacy_hooks_from_config(config: Dict[str, Any]) -> PrivacyHooks:
    """Create privacy hooks from configuration dictionary"""
    policy_level = PrivacyPolicyLevel(config.get("level", "standard"))
    
    policy = PrivacyPolicy(
        level=policy_level,
        enable_pii_scrubbing=config.get("enable_pii_scrubbing", True),
        enable_audit_logging=config.get("enable_audit_logging", True),
        enable_encrypted_audit=config.get("enable_encrypted_audit", False),
        custom_patterns=config.get("custom_patterns"),
        context_filters=config.get("context_filters")
    )
    
    return PrivacyHooks(policy)


# CLI interface for testing privacy hooks
def main():
    """CLI interface for privacy hooks testing"""
    print("🔐 Privacy Hooks Test Suite")
    print("=" * 50)
    
    # Test different privacy levels
    test_content = "John Doe's email is john.doe@example.com and his SSN is 123-45-6789"
    test_context = {"user_id": "test_user", "source": "cli_test"}
    
    for level in PrivacyPolicyLevel:
        print(f"\n🧪 Testing {level.value} privacy policy:")
        
        policy = PrivacyPolicy(level=level)
        hooks = PrivacyHooks(policy)
        
        scrubbed, metadata = hooks.scrub_content_for_anchor_injection(
            test_content, test_context, "test_utterance"
        )
        
        print(f"   Original: {test_content}")
        print(f"   Scrubbed: {scrubbed}")
        print(f"   Metadata: {metadata}")
        
        compliance, violations = hooks.validate_privacy_compliance(test_content, test_context)
        print(f"   Compliant: {compliance}, Violations: {violations}")
    
    print("\n✅ Privacy hooks testing completed!")


if __name__ == "__main__":
    main()