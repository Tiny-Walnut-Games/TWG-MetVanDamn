# 🧠 LDA Overlord/Sentinel - Internal Workflow Approval Authority

## Overview

The LDA Overlord/Sentinel is a security-first automation system that provides Guardian-grade approval authority for internal GitHub Actions workflows. It enables trusted internal actors to bypass manual workflow approval while maintaining comprehensive security validation and audit logging.

### 🔗 Role Chain Position
```
Guardian → Wizard → Advisor → Overlord/Sentinel
```

The Overlord/Sentinel inherits purity rules from the Guardian, analytical capabilities from the Wizard, and wisdom from the Advisor to make informed approval decisions.

## Architecture

### Core Components

1. **Configuration System** (`configs/overlord-sentinel.yml`)
   - Trusted actor definitions (users, service accounts, organizations)
   - Repository and branch trust patterns
   - Workflow scope control (auto-approvable vs sensitive)
   - Security settings and audit configuration

2. **Evaluation Engine** (`scripts/cid-faculty/overlord-sentinel.js`)
   - Actor identity verification
   - Repository/branch trust validation
   - Workflow scope assessment
   - User consent verification
   - Comprehensive audit logging

3. **CLI Interface** (enhanced `scripts/lda`)
   - Approval evaluation commands
   - Consent management
   - Status reporting
   - Integration with existing LDA toolchain

4. **GitHub Workflow** (`.github/workflows/overlord-sentinel.yml`)
   - Workflow dispatch triggers
   - Issue comment processing
   - Repository dispatch support
   - CID Faculty integration

## Security Model

### Guardian-Grade Validation

The Overlord/Sentinel applies multiple layers of security validation:

1. **Actor Trust Validation**
   - Allow-list of trusted users and service accounts
   - GitHub API identity verification (when enabled)
   - Organization membership validation

2. **Source Trust Validation**
   - Repository origin verification
   - Branch pattern matching (trusted vs untrusted)
   - Fork and external PR protection

3. **Workflow Scope Control**
   - Auto-approvable workflow patterns
   - Sensitive workflow detection (deploy, prod, secret)
   - Manual approval requirements for high-risk operations

4. **User Consent Management**
   - Explicit opt-in requirement
   - Version-tracked consent with timestamps
   - Revocation capabilities

5. **Audit and Compliance**
   - Comprehensive audit trail for all decisions
   - Security violation tracking
   - Rate limiting and abuse prevention
   - Emergency stop capabilities

### Trust Levels

#### Trusted Users
- Repository owners and maintainers
- Verified team members
- AI assistants (like @copilot) with explicit consent

#### Trusted Service Accounts
- `github-actions[bot]`
- `dependabot[bot]`
- Other verified automation accounts

#### Trusted Sources
- Main repository branches
- Internal feature branches
- Copilot and automated fix branches

#### Auto-Approvable Workflows
- Standard CI/CD pipelines
- Testing and validation workflows
- Documentation updates
- Non-sensitive automation

## Usage

### CLI Commands

#### Evaluate Approval Request
```bash
# Evaluate if a workflow should be auto-approved
lda overlord evaluate --actor=copilot --workflow="Living Dev Agent CI" --branch=copilot/fix-60

# Test with dry-run mode
lda overlord evaluate --actor=copilot --workflow="Living Dev Agent CI" --dry-run

# Emergency stop mode
lda overlord evaluate --actor=copilot --workflow="Living Dev Agent CI" --emergency-stop
```

#### Consent Management
```bash
# Grant user consent for auto-approval
lda overlord grant-consent --user=jmeyer1980

# Revoke user consent
lda overlord revoke-consent --user=copilot

# Check consent status in report
lda overlord report
```

#### Status Reporting
```bash
# Generate comprehensive status report
lda overlord report

# Example output includes:
# - Current configuration summary
# - Daily approval statistics
# - Recent activity log
# - Security posture overview
```

### GitHub Workflow Integration

#### Manual Dispatch
Trigger workflow approval evaluation directly:

```yaml
workflow_dispatch:
  inputs:
    actor: "copilot"
    repository: "jmeyer1980/living-dev-agent"
    workflow_name: "Living Dev Agent CI"
    branch: "copilot/fix-60"
```

#### Issue Comments
Use magic keywords in issue comments:

```
@overlord grant consent
@overlord revoke consent
@overlord report
@overlord approve workflow "Living Dev Agent CI"
```

#### Repository Dispatch
External systems can trigger approvals:

```bash
curl -X POST \
  -H "Authorization: token $GITHUB_TOKEN" \
  -H "Accept: application/vnd.github.v3+json" \
  https://api.github.com/repos/jmeyer1980/living-dev-agent/dispatches \
  -d '{
    "event_type": "overlord-approval-request",
    "client_payload": {
      "actor": "copilot",
      "workflow_name": "Living Dev Agent CI",
      "branch": "copilot/fix-60"
    }
  }'
```

## Configuration

### Basic Configuration (`configs/overlord-sentinel.yml`)

```yaml
overlord:
  identity:
    name: "LDA Overlord/Sentinel"
    authority_level: "overlord"
    version: "1.0.0"
    
  security:
    require_user_consent: true
    max_daily_approvals: 50
    validate_actor_identity: true
    
  trusted_actors:
    users:
      - "jmeyer1980"  # Repository owner
      - "copilot"     # AI assistant
    service_accounts:
      - "github-actions[bot]"
      - "dependabot[bot]"
      
  trusted_sources:
    repositories:
      - "jmeyer1980/living-dev-agent"
    trusted_branches:
      - "main"
      - "develop"
      - "copilot/*"
      - "fix/*"
      - "feature/*"
      
  workflow_scopes:
    auto_approvable:
      - "Living Dev Agent CI"
      - "Chronicle Keeper - TLDL Scribe System"
      - "CID Faculty - Wisdom Consultation"
    manual_approval_required:
      - "*deploy*"
      - "*release*"
      - "*secret*"
```

### User Consent Tracking

User consent is automatically tracked in the configuration:

```yaml
user_consent:
  jmeyer1980:
    granted: true
    timestamp: "2025-08-18T10:34:31.050Z"
    version: "1.0.0"
  copilot:
    granted: true
    timestamp: "2025-08-18T10:27:25.393Z"
    version: "1.0.0"
```

## Security Features

### Multi-Layer Validation

1. **Emergency Stop**: Immediate halt of all auto-approvals
2. **Rate Limiting**: Daily approval limits (default: 50)
3. **Scope Control**: Sensitive workflows always require manual approval
4. **Audit Trail**: Complete decision logging with timestamps
5. **Version Tracking**: Consent tied to configuration versions
6. **Pattern Matching**: Flexible branch and workflow pattern support

### Sensitive Workflow Protection

Automatically blocks auto-approval for:
- Deployment workflows (`*deploy*`, `*prod*`, `*release*`)
- Security-sensitive operations (`*secret*`, `*security*`)
- Publishing workflows (`*publish*`)
- Custom sensitive patterns (configurable)

### Audit Logging

Every overlord decision generates audit entries:

```json
{
  "timestamp": "2025-08-18T10:34:42.808Z",
  "event_type": "AUTO_APPROVED",
  "actor": "copilot",
  "repository": "jmeyer1980/living-dev-agent",
  "workflow_name": "Living Dev Agent CI",
  "branch": "copilot/fix-60",
  "approval_granted": true,
  "reason": "All security validations passed",
  "audit_trail": [
    "Actor: Actor 'copilot' found in trusted users list",
    "Source: Branch 'copilot/fix-60' matches trusted pattern 'copilot/*'",
    "Scope: Workflow 'Living Dev Agent CI' matches auto-approvable pattern",
    "Consent: User 'copilot' has granted consent (version 1.0.0)"
  ],
  "confidence": 0.95
}
```

## Integration

### CID Faculty Integration

The Overlord/Sentinel integrates with existing CID Faculty systems:

- **Advisor Consultation**: Authority usage analysis
- **Oracle Forecasting**: Future security posture predictions
- **Badge System**: Faculty-specific achievement tracking

### Chronicle Keeper Integration

Lore-worthy approval decisions are automatically preserved:

- Significant approval patterns
- Security violation incidents
- Configuration changes
- Authority evolution insights

## Example Scenarios

### ✅ Auto-Approved Scenario

**Context**: Copilot fixes a bug in a standard CI workflow
- **Actor**: copilot (trusted user with consent)
- **Repository**: jmeyer1980/living-dev-agent (trusted)
- **Branch**: copilot/fix-60 (matches copilot/* pattern)
- **Workflow**: Living Dev Agent CI (auto-approvable)
- **Result**: ✅ AUTO_APPROVED with 95% confidence

### 🚨 Manual Approval Required

**Context**: External contributor attempts deployment
- **Actor**: external-user (not in trusted users)
- **Repository**: jmeyer1980/living-dev-agent (trusted)
- **Branch**: external/feature (untrusted pattern)
- **Workflow**: Deploy to Production (sensitive pattern)
- **Result**: 🚨 MANUAL_APPROVAL_REQUIRED

### 🛑 Emergency Stop

**Context**: Security incident detected
- **Configuration**: emergency_stop: true
- **Any Request**: All approval requests
- **Result**: 🛑 EMERGENCY_STOP (no auto-approvals)

## Troubleshooting

### Common Issues

1. **"User consent required"**
   ```bash
   lda overlord grant-consent --user=username
   ```

2. **"Actor not in trusted list"**
   - Add user to `trusted_actors.users` in config
   - Or add to service accounts if applicable

3. **"Workflow requires manual approval"**
   - Check if workflow matches sensitive patterns
   - Add to `auto_approvable` list if appropriate

4. **"Branch not trusted"**
   - Verify branch matches trusted patterns
   - Add new pattern to `trusted_branches`

### Debug Commands

```bash
# Check configuration
lda overlord report

# Test evaluation with dry-run
lda overlord evaluate --actor=test --workflow=test --dry-run

# View recent audit log
lda overlord report | jq '.recent_activity'
```

## Best Practices

### Security

1. **Regular Review**: Audit trusted actor lists quarterly
2. **Principle of Least Privilege**: Only grant necessary access
3. **Monitor Activity**: Review audit logs regularly
4. **Emergency Preparedness**: Know how to activate emergency stop
5. **Consent Management**: Regularly verify user consent status

### Configuration

1. **Start Restrictive**: Begin with minimal trusted actors
2. **Gradual Expansion**: Add trust incrementally
3. **Pattern Precision**: Use specific patterns over wildcards
4. **Documentation**: Comment configuration changes
5. **Version Control**: Track configuration history

### Operational

1. **Monitor Metrics**: Track approval rates and violations
2. **Performance**: Set appropriate daily limits
3. **Integration**: Coordinate with existing workflows
4. **Communication**: Inform team of overlord capabilities
5. **Incident Response**: Have procedures for security violations

## Future Enhancements

### Planned Features

1. **Advanced Analytics**: ML-based risk assessment
2. **External Integrations**: Third-party security tools
3. **Workflow Templates**: Pre-approved workflow patterns
4. **Multi-Repository**: Cross-repository trust relationships
5. **Time-Based Rules**: Scheduled approval windows

### API Extensions

1. **REST API**: External system integration
2. **Webhooks**: Real-time approval notifications
3. **GraphQL**: Advanced querying capabilities
4. **SDK**: Language-specific client libraries

---

## 🛡️ Security Notice

The LDA Overlord/Sentinel has elevated authority within the LDA ecosystem. Proper configuration and monitoring are essential for maintaining security posture. Always follow the principle of least privilege and regularly audit trusted actor lists.

**Emergency Contact**: @jmeyer1980, @copilot
**Documentation Version**: 1.0.0
**Last Updated**: 2025-08-18