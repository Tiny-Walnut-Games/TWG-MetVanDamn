#!/usr/bin/env python3
"""
Redaction Transforms for Safety & Policy Transparency Layer

Provides PII masking, content filtering, and safety redaction capabilities
for the v0.8 Safety & Policy Transparency Layer milestone.

🧙‍♂️ "Even in transparency, wisdom knows when to shield and when to reveal - 
    for protection serves truth as much as disclosure." - Bootstrap Sentinel
"""

from __future__ import annotations
import re
import hashlib
from typing import Dict, Any, List, Tuple, Optional
from enum import Enum
from dataclasses import dataclass


class RedactionType(Enum):
    """Types of redaction transforms available"""
    PII_MASKING = "pii_masking"
    CONTENT_FILTERING = "content_filtering"
    PLACEHOLDER_REPLACEMENT = "placeholder_replacement"
    HASH_SUBSTITUTION = "hash_substitution"
    PARTIAL_REVEAL = "partial_reveal"


@dataclass
class RedactionRule:
    """Configuration for redaction operations"""
    pattern: str
    redaction_type: RedactionType
    replacement: str
    preserve_structure: bool = True
    metadata_key: Optional[str] = None


class RedactionTransforms:
    """
    Safety-focused content redaction and PII masking system
    
    Provides configurable redaction rules with metadata tracking
    for safety and policy transparency requirements.
    """
    
    def __init__(self, config_path: Optional[str] = None, privacy_config: Optional[Dict[str, Any]] = None):
        self.redaction_rules = self._load_default_rules()
        if config_path:
            self._load_custom_rules(config_path)
        
        # Privacy-specific configuration support
        self.privacy_config = privacy_config or {}
        self._configure_privacy_rules()
    
    def _load_default_rules(self) -> List[RedactionRule]:
        """Load default PII and safety redaction rules"""
        return [
            # Email addresses
            RedactionRule(
                pattern=r'\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b',
                redaction_type=RedactionType.PII_MASKING,
                replacement="[EMAIL_REDACTED]",
                metadata_key="email_count"
            ),
            # Phone numbers (various formats)
            RedactionRule(
                pattern=r'(\+?\d{1,3}[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}',
                redaction_type=RedactionType.PII_MASKING,
                replacement="[PHONE_REDACTED]",
                metadata_key="phone_count"
            ),
            # Social Security Numbers
            RedactionRule(
                pattern=r'\b\d{3}-\d{2}-\d{4}\b',
                redaction_type=RedactionType.PII_MASKING,
                replacement="[SSN_REDACTED]",
                metadata_key="ssn_count"
            ),
            # Credit card numbers (basic pattern)
            RedactionRule(
                pattern=r'\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b',
                redaction_type=RedactionType.PII_MASKING,
                replacement="[CARD_REDACTED]",
                metadata_key="card_count"
            ),
            # API keys and tokens (common patterns)
            RedactionRule(
                pattern=r'\b[A-Za-z0-9]{20,}\b',
                redaction_type=RedactionType.HASH_SUBSTITUTION,
                replacement="[TOKEN_{hash}]",
                metadata_key="token_count"
            ),
            # IP addresses
            RedactionRule(
                pattern=r'\b(?:[0-9]{1,3}\.){3}[0-9]{1,3}\b',
                redaction_type=RedactionType.PII_MASKING,
                replacement="[IP_REDACTED]",
                metadata_key="ip_count"
            )
        ]
    
    def _load_custom_rules(self, config_path: str):
        """Load custom redaction rules from configuration file"""
        # Implementation for loading custom rules from file
        pass
    
    def _configure_privacy_rules(self):
        """Configure privacy-specific redaction rules based on privacy_config"""
        privacy_level = self.privacy_config.get("level", "standard")
        
        if privacy_level == "strict":
            # In strict mode, add more aggressive patterns
            strict_rules = [
                # Catch potential usernames/IDs
                RedactionRule(
                    pattern=r'\b[a-zA-Z0-9._-]+(?=\s*[@#:]|\s*(?:id|user|name))',
                    redaction_type=RedactionType.PII_MASKING,
                    replacement="[USER_ID_REDACTED]",
                    metadata_key="user_id_count"
                ),
                # Catch potential organization names
                RedactionRule(
                    pattern=r'\b[A-Z][a-zA-Z0-9\s&.-]{2,}\s*(?:Inc|LLC|Corp|Ltd|Company)\b',
                    redaction_type=RedactionType.PII_MASKING,
                    replacement="[ORG_REDACTED]",
                    metadata_key="org_count"
                )
            ]
            self.redaction_rules.extend(strict_rules)
        
        # Add custom patterns from privacy config
        custom_patterns = self.privacy_config.get("custom_patterns", [])
        for pattern in custom_patterns:
            custom_rule = RedactionRule(
                pattern=pattern,
                redaction_type=RedactionType.PII_MASKING,
                replacement="[CUSTOM_REDACTED]",
                metadata_key="custom_count"
            )
            self.redaction_rules.append(custom_rule)
    
    def apply_redaction(
        self,
        content: str,
        context: Optional[Dict[str, Any]] = None
    ) -> Tuple[str, Dict[str, Any]]:
        """
        Apply redaction transforms to content
        
        Args:
            content: Input content to redact
            context: Optional context for conditional redaction
            
        Returns:
            Tuple of (redacted_content, redaction_metadata)
        """
        redacted_content = content
        metadata = {
            "redaction_applied": False,
            "redaction_types": [],
            "redaction_counts": {},
            "safety_level": "none"
        }
        
        for rule in self.redaction_rules:
            if self._should_apply_rule(rule, context):
                redacted_content, rule_metadata = self._apply_rule(
                    redacted_content, rule
                )
                
                if rule_metadata["applied"]:
                    metadata["redaction_applied"] = True
                    metadata["redaction_types"].append(rule.redaction_type.value)
                    
                    if rule.metadata_key:
                        metadata["redaction_counts"][rule.metadata_key] = rule_metadata["count"]
        
        # Determine safety level based on redactions applied
        metadata["safety_level"] = self._calculate_safety_level(metadata)
        
        return redacted_content, metadata
    
    def _should_apply_rule(
        self,
        rule: RedactionRule,
        context: Optional[Dict[str, Any]]
    ) -> bool:
        """Determine if redaction rule should be applied based on context"""
        if not context:
            return True
            
        # Context-aware redaction logic
        safety_mode = context.get("safety_mode", "standard")
        if safety_mode == "strict":
            return True
        elif safety_mode == "permissive":
            return rule.redaction_type in [RedactionType.PII_MASKING]
        
        return True
    
    def _apply_rule(
        self,
        content: str,
        rule: RedactionRule
    ) -> Tuple[str, Dict[str, Any]]:
        """Apply a single redaction rule to content"""
        pattern = re.compile(rule.pattern, re.IGNORECASE)
        matches = pattern.findall(content)
        
        rule_metadata = {
            "applied": len(matches) > 0,
            "count": len(matches)
        }
        
        if not matches:
            return content, rule_metadata
        
        if rule.redaction_type == RedactionType.HASH_SUBSTITUTION:
            # Replace with hash-based placeholders
            def hash_replacement(match):
                hash_obj = hashlib.sha256(match.group().encode())
                return rule.replacement.format(hash=hash_obj.hexdigest()[:8])
            
            redacted_content = pattern.sub(hash_replacement, content)
            
        elif rule.redaction_type == RedactionType.PARTIAL_REVEAL:
            # Keep partial structure visible
            def partial_replacement(match):
                text = match.group()
                if len(text) <= 4:
                    return "[REDACTED]"
                return text[:2] + "*" * (len(text) - 4) + text[-2:]
            
            redacted_content = pattern.sub(partial_replacement, content)
            
        else:
            # Standard replacement
            redacted_content = pattern.sub(rule.replacement, content)
        
        return redacted_content, rule_metadata
    
    def _calculate_safety_level(self, metadata: Dict[str, Any]) -> str:
        """Calculate overall safety level based on redactions applied"""
        if not metadata["redaction_applied"]:
            return "none"
        
        total_redactions = sum(metadata["redaction_counts"].values())
        
        if total_redactions >= 5:
            return "high"
        elif total_redactions >= 2:
            return "medium"
        else:
            return "low"
    
    def create_transparency_report(
        self,
        original_content: str,
        redacted_content: str,
        metadata: Dict[str, Any]
    ) -> Dict[str, Any]:
        """
        Create transparency report for redaction operations
        
        Provides audit trail information for policy transparency
        """
        return {
            "redaction_summary": {
                "total_length_original": len(original_content),
                "total_length_redacted": len(redacted_content),
                "redaction_percentage": (
                    (len(original_content) - len(redacted_content)) / len(original_content) * 100
                    if len(original_content) > 0 else 0
                ),
                "safety_level": metadata["safety_level"]
            },
            "redaction_details": metadata,
            "transparency_notes": self._generate_transparency_notes(metadata)
        }
    
    def _generate_transparency_notes(self, metadata: Dict[str, Any]) -> List[str]:
        """Generate human-readable transparency notes"""
        notes = []
        
        if metadata["redaction_applied"]:
            notes.append("Content redaction applied for safety and privacy protection")
            
            for redaction_type in set(metadata["redaction_types"]):
                if redaction_type == "pii_masking":
                    notes.append("Personal identifying information (PII) has been masked")
                elif redaction_type == "hash_substitution":
                    notes.append("Sensitive tokens replaced with cryptographic hashes")
                elif redaction_type == "content_filtering":
                    notes.append("Content filtered for policy compliance")
        else:
            notes.append("No redaction required - content cleared for transparency")
        
        return notes


def main():
    """CLI interface for redaction transforms testing"""
    redactor = RedactionTransforms()
    
    # Test examples
    test_content = """
    Contact me at john.doe@example.com or call (555) 123-4567.
    My SSN is 123-45-6789 and my card number is 1234 5678 9012 3456.
    API token: abc123xyz789token456def
    Server IP: 192.168.1.100
    """
    
    print("🛡️ Redaction Transforms Demo")
    print("=" * 40)
    print("Original content:")
    print(test_content)
    
    redacted, metadata = redactor.apply_redaction(test_content)
    
    print("\nRedacted content:")
    print(redacted)
    
    print("\nRedaction metadata:")
    import json
    print(json.dumps(metadata, indent=2))
    
    transparency_report = redactor.create_transparency_report(
        test_content, redacted, metadata
    )
    
    print("\nTransparency report:")
    print(json.dumps(transparency_report, indent=2))


if __name__ == "__main__":
    main()