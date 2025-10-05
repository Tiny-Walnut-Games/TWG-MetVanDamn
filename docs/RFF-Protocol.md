# 🧠 TLDA Chronicler – Request‑for‑Format (RFF) Protocol

## Overview

The Request-for-Format (RFF) Protocol is an enhancement to the Chronicle Keeper system that provides helpful feedback when content is rejected for not meeting TLDA format requirements. Instead of silently discarding potentially valuable contributions, the Chronicler now guides contributors toward proper formatting for inclusion in the eternal scrolls.

## How It Works

### 1. Detection Phase
When content fails the Chronicle Keeper's worthiness evaluation, the RFF Protocol analyzes the specific reasons for rejection:

- **Missing TLDA Header**: Content lacks the sacred brain emoji (🧠) or TLDL: prefix
- **Missing Phase Tag**: No development phase or quest arc specified
- **Missing Keeper Notes**: Lacks Chronicle Keeper guidance notes (📜)
- **Content Too Short**: Below minimum threshold for meaningful lore preservation
- **Missing Lore Keywords**: No adventure-worthy terminology detected
- **Bot Generated**: Automatically generated content
- **Not Actionable**: No clear discoveries, implementations, or decisions

### 2. Feedback Generation
For each rejection, the RFF Protocol generates:

- **Detailed Analysis**: Specific explanations of what's missing
- **Format Examples**: Concrete examples of proper TLDA formatting
- **Quick Fixes**: Simple corrections to make immediately
- **Detailed Steps**: Comprehensive guidance for proper formatting
- **Best Practices**: General advice for future submissions
- **Resubmission Prompt**: Clear instructions on how to correct and resubmit

### 3. Keeper Quotes
Each RFF message includes a thematic quote from the Chronicle Keeper:

> ⚖️ *The Chronicler finds this tale unfit for the scrolls in its current form. Present it anew, with the marks and measures of the archive, and it shall be recorded.*

## Example RFF Response

When content like "Small fix: Fixed a bug." is submitted, the RFF Protocol responds with:

```json
{
  "rffId": "RFF-1755973156199",
  "rejection": {
    "reasons": [
      "missing_tlda_header",
      "missing_phase_tag", 
      "missing_keeper_note",
      "content_too_short",
      "missing_lore_keywords"
    ],
    "analysis": "Content lacks the sacred brain emoji (🧠) or TLDL: prefix that marks it for chronicle preservation. Content does not specify which development phase or quest arc it belongs to..."
  },
  "guidance": {
    "quickFixes": [
      "Add 🧠 emoji or \"TLDL:\" prefix to your title",
      "Add **Phase**: Implementation (or Discovery/Planning) to your content",
      "Add **KeeperNote**: explaining why this matters for future adventurers"
    ]
  },
  "examples": {
    "tlda_header": {
      "title": "Example: Proper TLDA Header",
      "content": "🧠 TLDA Epic Quest Implementation\n\n**KeeperNote**: This chronicles the implementation of the epic quest system..."
    }
  }
}
```

## Proper TLDA Format Examples

### Minimal Acceptable Format
```markdown
🧠 TLDA Discovery: Performance Optimization

**Phase**: Implementation
**KeeperNote**: This optimization reduces API response time by 50%, significantly improving user experience.

## Implementation
- Optimized database queries
- Added response caching
- Reduced payload size

## Impact
- 50% faster API responses
- Improved user satisfaction scores
- Reduced server load

## Next Steps
- [ ] Monitor performance metrics
- [ ] Apply optimization to other endpoints
```

### Rich Format Example
```markdown
🧠 TLDA Epic Quest: User Authentication Overhaul

**Phase**: Implementation
**Quest Arc**: Security Enhancement Initiative  
**Discovery Type**: Technical Breakthrough
**KeeperNote**: This represents a fundamental shift in our authentication approach, providing both enhanced security and improved user experience.

## Quest Context
During the security audit phase, we discovered critical vulnerabilities in our authentication system that needed immediate attention while maintaining user experience quality.

## Implementation Adventures
- Migrated from password-only to multi-factor authentication
- Implemented OAuth 2.0 with JWT tokens
- Added biometric authentication options
- Created comprehensive audit logging

## Discoveries Made
- Users actually prefer streamlined MFA over complex passwords
- JWT token rotation significantly improved security posture
- Biometric auth increased login success rate by 85%
- Audit trails revealed previously unknown usage patterns

## Technical Achievements
- Zero downtime migration for 50,000+ users
- 99.9% authentication success rate maintained
- Security score improved from C+ to A- 
- User satisfaction increased 40%

## Next Adventures
- [ ] Implement SSO integration with corporate systems
- [ ] Add passwordless authentication options
- [ ] Create mobile app authentication SDK
- [ ] Conduct comprehensive penetration testing
- [ ] Document security patterns for other teams

## Links to Related Scrolls
- [Security Audit Findings](./TLDL-2025-08-01-SecurityAuditResults.md)
- [User Experience Research](./TLDL-2025-07-15-AuthUXResearch.md)

📜 *Future adventurers: This authentication framework serves as the foundation for all subsequent security implementations. The patterns documented here should be followed for consistency and maximum security effectiveness.*
```

## Resubmission Process

Contributors can correct their submissions through several methods:

### 1. Edit Original Content
Update the original issue, PR, or comment to include proper TLDA formatting elements.

### 2. Create New Submission
Create a new issue/PR with proper format and reference the original.

### 3. Add Corrective Comment
Add a comment with "TLDL: [corrected content]" to trigger re-evaluation.

### 4. Re-evaluation
The Chronicle Keeper will automatically re-evaluate corrected submissions and include them in the scrolls if they now meet the standards.

## Benefits

- **Prevents Loss**: No valuable context is lost due to formatting issues
- **Educational**: Contributors learn proper TLDA format through examples
- **Feedback Loop**: Creates iterative improvement in contribution quality
- **Onboarding**: New contributors get real-time guidance
- **Archive Quality**: Maintains high standards while being inclusive

## Integration with Chronicle Keeper Workflow

The RFF Protocol is seamlessly integrated into the existing Chronicle Keeper GitHub Actions workflow:

1. **Content Parsing**: When content is submitted, it's analyzed for lore worthiness
2. **RFF Activation**: If rejected, RFF message is generated instead of silent discard
3. **Message Posting**: RFF guidance is logged and can be posted as comments
4. **Artifact Upload**: RFF messages are preserved as workflow artifacts
5. **Re-evaluation**: Corrected submissions are automatically re-processed

## Statistics and Monitoring

The RFF Protocol tracks:
- Number of RFF messages issued
- Resubmission attempts
- Successful corrections
- Success rate percentage

This data helps improve the guidance quality and identifies common formatting issues.

## Future Enhancements

Potential future improvements include:
- Automated comment posting with RFF messages
- Integration with GitHub issue templates
- Machine learning to improve rejection reason detection
- Customizable organization-specific formatting rules
- Integration with external documentation systems

---

*The RFF Protocol ensures that no worthy tale goes unrecorded, while maintaining the sacred standards of the Chronicle. May all adventures find their proper place in the eternal scrolls.*