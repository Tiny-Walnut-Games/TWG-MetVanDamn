# 🕐 Workflow Cooldown System

## Overview

The Workflow Cooldown System prevents infinite loops in GitHub Actions workflows by implementing intelligent rate limiting based on issue numbers and trigger patterns. This system protects against scenarios where workflow-generated comments contain the same trigger patterns that originally invoked the workflow.

## Problem Solved

Before implementing cooldowns, the following scenario could occur:
1. User posts comment with trigger pattern (e.g., `TLDL:`, `🔮`, `📊 budget-check`)
2. Workflow executes and posts response comment containing the same trigger pattern
3. Response comment triggers workflow again, creating infinite loop
4. Rapid-fire executions could "bankrupt everyone" due to resource consumption

## Architecture

### Core Components

1. **WorkflowCooldown Class** (`scripts/cid-faculty/shared/workflow-cooldown.js`)
   - Issue-based cooldown tracking
   - Configurable timeout periods
   - Loop risk detection
   - Execution count monitoring
   - Cache cleanup utilities

2. **CID Faculty Integration** (`scripts/cid-faculty/comment-handler.js`)
   - Pre-execution cooldown checks
   - Risk pattern detection
   - Intelligent cooldown setting

3. **Chronicle Keeper Integration** (`scripts/chronicle-keeper/cooldown-check.js`)
   - Lightweight cooldown checking
   - Event-type awareness
   - Comment-specific protection

### Workflow Integration

Both `cid-faculty.yml` and `chronicle-keeper.yml` include cooldown checks:
- **Pre-execution**: Check for active cooldowns before starting expensive operations
- **Conditional steps**: All workflow steps respect cooldown status
- **Exit codes**: Special exit code 42 indicates cooldown skip

## Configuration

### Default Cooldown Periods

| Workflow | Default | High Risk | Error Cases |
|----------|---------|-----------|-------------|
| CID Faculty | 5 minutes | 10 minutes | 10 minutes |
| Chronicle Keeper | 3 minutes | 5 minutes | N/A |
| Budget Check | 2 minutes | N/A | N/A |

### Trigger Patterns

**Loop-Risk Patterns:**
- `TLDL:` - TLDL generation requests
- `🔮` - Oracle consultation emoji
- `📊 budget-check` - Budget status queries
- `📜` - Chronicle/lore markers
- `🎓` - Faculty/academic markers
- `👨‍🏫` - Advisor markers

## Usage

### Checking Cooldown Status

```bash
# CID Faculty cooldown status
node scripts/cid-faculty/shared/workflow-cooldown.js status cid-faculty 123

# Chronicle Keeper cooldown check
node scripts/chronicle-keeper/cooldown-check.js --issue=123 --event=issue_comment --comment="TLDL: test"
```

### Statistics and Maintenance

```bash
# View cooldown statistics
node scripts/cid-faculty/shared/workflow-cooldown.js stats

# Clean old cooldown files
node scripts/cid-faculty/shared/workflow-cooldown.js cleanup
```

### Testing

```bash
# Run comprehensive cooldown tests
node tests/test-workflow-cooldown.js
```

## File Locations

### Cache Directories
- CID Faculty: `out/cid/cooldown/`
- Chronicle Keeper: `out/chronicle/cooldown/`

### Implementation Files
- Core system: `scripts/cid-faculty/shared/workflow-cooldown.js`
- CID Faculty handler: `scripts/cid-faculty/comment-handler.js`
- Chronicle Keeper checker: `scripts/chronicle-keeper/cooldown-check.js`
- Tests: `tests/test-workflow-cooldown.js`

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success, no cooldown active |
| 42 | Cooldown active, execution skipped |
| 1 | Error in cooldown system |

## Monitoring

### Execution Count Warnings

The system tracks execution counts per issue and warns when patterns suggest potential problems:
- **Count > 3**: High execution count warning
- **Risk = high**: Multiple trigger patterns detected
- **Logs**: Detailed cooldown status in workflow logs

### Statistics Tracking

- Active cooldowns per workflow
- Total executions across all issues
- File count and storage usage
- Cache hit/miss ratios

## Maintenance

### Automatic Cleanup

Cooldown files are automatically cleaned up:
- **Default**: Files older than 24 hours
- **Workflow runs**: Cleanup triggered during cache maintenance
- **Manual**: Can be triggered via CLI commands

### Storage Management

- Cooldown files are excluded from Git (`.gitignore`)
- Small JSON files with minimal storage impact
- Automatic expiration prevents unbounded growth

## Customization

### Custom Cooldown Periods

```javascript
// Set custom cooldown (in milliseconds)
cooldown.setCooldown('workflow-name', '123', triggerInfo, 10 * 60 * 1000); // 10 minutes
```

### Custom Trigger Patterns

```javascript
// Define custom patterns for loop detection
const customPatterns = ['custom-trigger', '🎯', 'special-keyword'];
const loopRisk = cooldown.wouldTriggerLoop('workflow', customPatterns, commentText);
```

### Risk Assessment

- **None**: No trigger patterns detected
- **Medium**: Single trigger pattern detected
- **High**: Multiple trigger patterns detected

## Best Practices

1. **Always check cooldowns** before executing expensive operations
2. **Set cooldowns early** in the workflow to prevent re-triggers
3. **Use appropriate timeouts** based on workflow complexity
4. **Monitor execution counts** for signs of problematic patterns
5. **Test cooldown behavior** when adding new trigger patterns
6. **Clean up regularly** to prevent storage bloat

## Troubleshooting

### Common Issues

**Legitimate Use Blocked**: If legitimate use cases are being blocked, consider:
- Reducing cooldown periods for specific trigger types
- Adding pattern-specific exceptions
- Using different issues for testing

**High Execution Counts**: Investigate triggers causing repeated execution:
- Check comment patterns
- Review workflow outputs
- Examine trigger logic

**Storage Growth**: Monitor and clean cooldown directories:
```bash
du -sh out/*/cooldown/
node scripts/cid-faculty/shared/workflow-cooldown.js cleanup
```

### Debug Information

Enable verbose logging by checking cooldown status:
```bash
node scripts/cid-faculty/shared/workflow-cooldown.js status workflow-name issue-number
```

## Implementation Notes

- **Thread-safe**: Uses file-based locking for concurrent access
- **Cross-workflow**: Different workflows maintain separate cooldown spaces
- **Issue-scoped**: Cooldowns are per-issue to avoid blocking legitimate use
- **Failure-safe**: Errors in cooldown system don't block workflow execution
- **Extensible**: Easy to add new workflows or customize behavior