# Alchemist Faculty Linkback Automation - Edge Cases & Error Handling

## Overview

The linkback automation system handles various edge cases and error conditions gracefully to ensure reliable operation in production environments.

## Edge Cases Handled

### 1. Missing Evidence Links Section

**Scenario**: GitHub issue does not have an existing "## 🧪 Alchemist Evidence Links" section.

**Handling**: 
- Automatically appends the evidence section to the end of the issue body
- Preserves all existing content and formatting
- Adds proper spacing between existing content and new section

**Implementation**: `_update_issue_body()` method checks for existing section using regex pattern matching.

### 2. Existing Evidence Links Section

**Scenario**: Issue already contains evidence links from previous automation runs.

**Handling**:
- Replaces the entire existing evidence section with updated content
- Preserves content before and after the evidence section
- Uses regex pattern to identify section boundaries

**Pattern Used**: `## 🧪 Alchemist Evidence Links.*?(?=\n##|\n---|\Z)`

### 3. Empty Claims Directory

**Scenario**: Claims directory exists but contains no validation results.

**Handling**:
- Returns empty claims list
- Generates "compost" stage decision with appropriate rationale
- Creates evidence section indicating no claims were found
- Continues execution without errors

### 4. Missing Claims Directories

**Scenario**: Expected subdirectories (validated/, hypotheses/, regressions/) don't exist.

**Handling**:
- Skips non-existent directories during scanning
- Continues processing with available claims
- Logs warnings for missing directories in verbose mode

### 5. Corrupted or Invalid Claim Files

**Scenario**: JSON files in claims directories are malformed or missing required fields.

**Handling**:
- Catches `json.JSONDecodeError` and `KeyError` exceptions
- Logs warning with specific file and error details
- Continues processing other valid claim files
- Does not halt automation pipeline

**Error Logged**: `"Warning: Failed to parse claim file {filename}: {error}"`

### 6. Missing Integration Metadata

**Scenario**: `github_integration.json` file is missing or corrupted.

**Handling**:
- Returns `None` for integration data
- Uses default values for baseline delta (0.0)
- Continues with claim-based analysis only
- Logs warning about missing metadata

### 7. No GitHub Token (Dry-Run Mode)

**Scenario**: GitHub token not provided or invalid.

**Handling**:
- Automatically enables dry-run mode
- Uses mock issue data for testing
- Shows what actions would be performed
- Does not attempt actual GitHub API calls

**Output**: Displays complete evidence section and labels that would be applied.

## Error Handling Strategies

### 1. GitHub API Errors

**Authentication Failures**:
- Clear error message: "GitHub API request failed: 401 Unauthorized"
- Suggests checking token permissions
- Graceful fallback to dry-run mode

**Rate Limiting**:
- Handles HTTP 429 responses
- Uses exponential backoff for retries (not yet implemented)
- Respects GitHub's rate limit headers

**Network Failures**:
- Catches `requests.RequestException`
- Provides specific error messages
- Continues with local processing when possible

### 2. File System Errors

**Permission Issues**:
- Catches `IOError` and `PermissionError`
- Provides clear error messages about file access
- Suggests checking directory permissions

**Disk Space**:
- Minimal disk usage for claim processing
- Reads files sequentially to manage memory

### 3. Template Rendering Errors

**Missing Template Variables**:
- Provides default values for all required template variables
- Uses "unknown" or "pending" placeholders when data unavailable
- Logs warnings for missing data sources

**Template Format Issues**:
- Falls back to embedded template if external template malformed
- Validates template variables before rendering
- Provides basic template structure as last resort

## Configuration and Monitoring

### 1. Verbose Logging

Enable detailed logging with `--verbose` flag:

```bash
python3 linkback_automation.py --verbose --issue-number 123 --repo owner/repo
```

**Logged Information**:
- Claims scanning progress
- Template variable population
- GitHub API request details
- Error context and stack traces

### 2. Dry-Run Validation

Always test changes with `--dry-run` before live execution:

```bash
python3 linkback_automation.py --dry-run --issue-number 123 --repo owner/repo
```

**Dry-Run Output**:
- Complete evidence section preview
- Label list that would be applied
- Summary comment content
- Validation of template rendering

### 3. Health Checks

**Pre-Execution Validation**:
- Verifies claims directory exists
- Checks for readable claim files
- Validates GitHub token permissions (if provided)
- Confirms issue exists and is accessible

**Post-Execution Verification**:
- Confirms successful issue update
- Validates applied labels
- Checks comment posting success
- Reports any partial failures

## Recovery Procedures

### 1. Partial Failure Recovery

**Issue Update Succeeds, Labeling Fails**:
- Evidence section is preserved
- Manual label application possible
- Logs specific label errors for investigation

**Labeling Succeeds, Comment Posting Fails**:
- Issue and labels are updated correctly
- Comment can be posted manually or re-run
- No data loss or corruption

### 2. Retry Logic

**Automatic Retries** (not yet implemented but planned):
- Exponential backoff for rate limiting
- Maximum retry attempts: 3
- Retry conditions: Network timeouts, rate limits, temporary API failures

**Manual Recovery**:
- Re-run automation with same parameters
- System detects existing evidence section and updates in-place
- No duplicate content or broken formatting

### 3. Rollback Procedures

**Manual Rollback**:
- Evidence section can be manually removed from issue
- Labels can be manually removed
- Comments cannot be deleted but can be edited

**Version Control**:
- All changes tracked in Git commits
- Easy to revert automation script changes
- Issue edit history preserved in GitHub

## Best Practices

### 1. Monitoring

- Monitor GitHub API rate limits
- Check for automation failures in CI/CD
- Validate evidence section formatting regularly
- Review claim file quality and completeness

### 2. Maintenance

- Update template variables as schema evolves
- Adjust stage decision criteria based on experience
- Enhance error messages based on user feedback
- Add new claim types as validation system expands

### 3. Security

- Use minimal scope GitHub tokens
- Avoid logging sensitive information
- Validate all user inputs and file paths
- Implement proper access controls for automation

## Future Enhancements

### 1. Enhanced Error Recovery

- Implement exponential backoff retry logic
- Add webhook notifications for failures
- Create automated health monitoring
- Develop self-healing capabilities

### 2. Advanced Validation

- Schema validation for claim files
- Template syntax validation
- Issue format verification
- Cross-reference validation with manifests

### 3. Performance Optimization

- Batch GitHub API operations
- Cache template rendering
- Parallel claims processing
- Incremental updates for large claim sets

---

*Last Updated: 2025-09-06*  
*Version: 0.1.0*  
*Alchemist Faculty Team*