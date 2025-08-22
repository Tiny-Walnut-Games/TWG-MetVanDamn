#!/usr/bin/env python3
"""
MCP Configuration Validator

Validates MCP configuration for security compliance and consistency.
Addresses security concerns identified in issue #50.
"""

import json
import os
import sys
import logging
from pathlib import Path
from typing import Dict, List, Any, Tuple

class MCPConfigValidator:
    def __init__(self, config_path: str = "mcp-config.json"):
        self.config_path = Path(config_path)
        self.config = {}
        self.issues = []
        self.warnings = []
        
    def load_config(self) -> bool:
        """Load MCP configuration file"""
        try:
            with open(self.config_path, 'r') as f:
                self.config = json.load(f)
            return True
        except FileNotFoundError:
            self.issues.append(f"Config file not found: {self.config_path}")
            return False
        except json.JSONDecodeError as e:
            self.issues.append(f"Invalid JSON in config file: {e}")
            return False
    
    def validate_security_config(self) -> None:
        """Validate security configuration settings"""
        security = self.config.get('security', {})
        
        # Check allowed origins
        allowed_origins = security.get('allowedOrigins', [])
        if not allowed_origins:
            self.issues.append("No allowed origins specified")
        
        # Check for localhost restrictions
        localhost_restrictions = security.get('localhostRestrictions', {})
        if not localhost_restrictions.get('enabled', False):
            self.warnings.append("Localhost restrictions not enabled")
        
        # Validate localhost configuration
        for origin in allowed_origins:
            if 'localhost' in origin or '127.0.0.1' in origin:
                if localhost_restrictions.get('enabled', False):
                    allowed_ports = localhost_restrictions.get('allowedPorts', [])
                    if not allowed_ports:
                        self.issues.append("Localhost origins found but no allowed ports specified")
                else:
                    self.issues.append("Localhost origins found but localhost restrictions not enabled")
        
        # Check token validation
        token_validation = security.get('tokenValidation', {})
        if not token_validation.get('enabled', False):
            self.warnings.append("Token validation not enabled")
            
        required_scopes = token_validation.get('requiredScopes', [])
        if not required_scopes:
            self.warnings.append("No required token scopes specified")
    
    def validate_server_config(self) -> None:
        """Validate server configuration for startup reliability"""
        server_config = self.config.get('serverConfig', {})
        
        # Check timeout settings
        timeout = server_config.get('timeout', 0)
        if timeout < 30000:
            self.warnings.append(f"Server timeout may be too low: {timeout}ms (recommended: ≥30000ms)")
        
        # Check retry logic
        max_retries = server_config.get('maxRetries', 0)
        hard_fail = server_config.get('hardFailOnExhaustion', False)
        
        if max_retries > 0 and not hard_fail:
            self.issues.append("Retry logic enabled but hardFailOnExhaustion not set - may cause hanging")
        
        # Check startup buffer
        startup_buffer = server_config.get('startupTimeoutBuffer', 0)
        if startup_buffer < 10000:
            self.warnings.append(f"Startup timeout buffer may be too low: {startup_buffer}ms (recommended: ≥10000ms)")
    
    def validate_dependency_config(self) -> None:
        """Validate dependency configuration for supply chain security"""
        dev_config = self.config.get('development', {})
        dep_validation = dev_config.get('dependencyValidation', {})
        
        if not dep_validation.get('enabled', False):
            self.warnings.append("Dependency validation not enabled")
        
        # Check for pinned versions
        pinned_versions = dep_validation.get('pinnedVersions', {})
        playwright_version = pinned_versions.get('@playwright/mcp')
        
        if not playwright_version:
            self.issues.append("@playwright/mcp version not pinned - supply chain risk")
        elif playwright_version == "latest" or "@latest" in str(playwright_version):
            self.issues.append("@playwright/mcp using @latest - should use specific version")
        
        # Check dynamic install policy  
        allow_dynamic = dep_validation.get('allowDynamicInstalls', True)
        if allow_dynamic:
            self.warnings.append("Dynamic dependency installs allowed - consider disabling for security")
    
    def validate_tool_allowlist(self) -> None:
        """Validate tool allowlist configuration"""
        tool_audit = self.config.get('toolAllowListAudit', {})
        
        if not tool_audit.get('enabled', False):
            self.warnings.append("Tool allowlist audit not enabled")
            return
        
        allowed_tools = tool_audit.get('allowedTools', [])
        blocked_tools = tool_audit.get('blockedTools', [])
        
        # Check for known problematic tools
        mcp_tools = self.config.get('mcpConfig', {}).get('capabilities', {}).get('tools', [])
        declared_tools = [tool['name'] for tool in mcp_tools if isinstance(tool, dict)]
        
        for tool_name in declared_tools:
            if tool_name not in allowed_tools and tool_name not in blocked_tools:
                self.warnings.append(f"Tool '{tool_name}' declared but not in allowlist audit configuration")
        
        # Check for specific problematic tools mentioned in issue
        problematic_tools = ['search_repository_with_agent']
        for tool in problematic_tools:
            if tool in allowed_tools:
                self.issues.append(f"Problematic tool '{tool}' is in allowed list")
            elif tool not in blocked_tools:
                self.warnings.append(f"Problematic tool '{tool}' should be explicitly blocked")
    
    def validate_logging_config(self) -> None:
        """Validate logging configuration for security monitoring"""
        server_config = self.config.get('serverConfig', {})
        logging_config = server_config.get('logging', {})
        
        log_output = logging_config.get('output', '')
        if not log_output:
            self.warnings.append("No log output file specified")
        
        # Check if audit log is configured for tool allowlist
        tool_audit = self.config.get('toolAllowListAudit', {})
        audit_log = tool_audit.get('auditLog', '')
        
        if tool_audit.get('enabled', False) and not audit_log:
            self.warnings.append("Tool allowlist audit enabled but no audit log specified")
    
    def run_validation(self) -> Tuple[bool, Dict[str, Any]]:
        """Run all validations and return results"""
        if not self.load_config():
            return False, {
                'status': 'FAIL',
                'issues': self.issues,
                'warnings': self.warnings
            }
        
        # Run all validation checks
        self.validate_security_config()
        self.validate_server_config()
        self.validate_dependency_config()
        self.validate_tool_allowlist()
        self.validate_logging_config()
        
        # Determine overall status
        status = 'PASS' if not self.issues else 'FAIL'
        if self.warnings and not self.issues:
            status = 'PASS_WITH_WARNINGS'
        
        return len(self.issues) == 0, {
            'status': status,
            'config_file': str(self.config_path),
            'issues': self.issues,
            'warnings': self.warnings,
            'summary': {
                'total_issues': len(self.issues),
                'total_warnings': len(self.warnings)
            }
        }
    
    def print_report(self, results: Dict[str, Any]) -> None:
        """Print validation report"""
        print("=== MCP Configuration Validation Report ===")
        print(f"Status: {results['status']}")
        print(f"Config File: {results['config_file']}")
        print(f"Issues: {results['summary']['total_issues']}")
        print(f"Warnings: {results['summary']['total_warnings']}")
        print()
        
        if results['issues']:
            print("🚨 CRITICAL ISSUES:")
            for issue in results['issues']:
                print(f"  ❌ {issue}")
            print()
        
        if results['warnings']:
            print("⚠️  WARNINGS:")
            for warning in results['warnings']:
                print(f"  ⚠️  {warning}")
            print()
        
        if results['status'] == 'PASS' and not results['warnings']:
            print("✅ All validations passed!")

def main():
    import argparse
    
    parser = argparse.ArgumentParser(description='Validate MCP configuration for security compliance')
    parser.add_argument('--config', default='mcp-config.json', help='Path to MCP config file')
    parser.add_argument('--json', action='store_true', help='Output results as JSON')
    parser.add_argument('--strict', action='store_true', help='Treat warnings as errors')
    
    args = parser.parse_args()
    
    validator = MCPConfigValidator(args.config)
    success, results = validator.run_validation()
    
    if args.json:
        print(json.dumps(results, indent=2))
    else:
        validator.print_report(results)
    
    # Exit with appropriate code
    if not success or (args.strict and results['warnings']):
        sys.exit(1)
    else:
        sys.exit(0)

if __name__ == '__main__':
    main()