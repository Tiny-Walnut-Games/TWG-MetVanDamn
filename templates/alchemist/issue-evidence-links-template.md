# Issue Evidence Links Template

This template is used by the Alchemist Faculty to update Gu Pot issues with experimental evidence and validation results.

## Template Variables

- `{issue_number}` - GitHub issue number
- `{experiment_id}` - Unique experiment identifier
- `{run_timestamp}` - ISO timestamp of experiment run
- `{stage_decision}` - Final stage decision (serum/antitoxin/compost)
- `{confidence_score}` - Validation confidence score (0-1)
- `{baseline_delta}` - Improvement over baseline (percentage)
- `{claim_files}` - List of generated claim files
- `{artifact_inventory}` - Complete artifact inventory
- `{validation_summary}` - Summary of validation results
- `{promotion_rationale}` - Reasoning for promotion decision

## Evidence Links Section Template

```markdown
## 🧪 Alchemist Evidence Links

> **Experimental Status**: {stage_decision} | **Confidence**: {confidence_score} | **Baseline Δ**: {baseline_delta}%
> **Experiment ID**: `{experiment_id}` | **Run**: `{run_timestamp}`

### 📊 Validation Summary
{validation_summary}

### 🎯 Promotion Decision: **{stage_decision}**
{promotion_rationale}

### 📁 Evidence Artifacts

#### Generated Claims
{claim_files}

#### Experimental Data
{artifact_inventory}

### 🔗 Trace Links
- **Manifest**: [`gu_pot/issue-{issue_number}/manifest_v1.json`](gu_pot/issue-{issue_number}/manifest_v1.json)
- **Run Directory**: [`gu_pot/issue-{issue_number}/runs/{run_timestamp}/`](gu_pot/issue-{issue_number}/runs/{run_timestamp}/)
- **Claims Directory**: [`gu_pot/issue-{issue_number}/claims/`](gu_pot/issue-{issue_number}/claims/)

### ⚗️ Origin Binding Verified
- **Logline Hash**: `{logline_hash}`
- **Tension Hash**: `{tension_hash}` 
- **Extraction Time**: `{extracted_on}`
- **Alchemist Version**: `{alchemist_version}`

---
*Updated by Alchemist Faculty v{alchemist_version} on {update_timestamp}*
```

## Stage-Specific Templates

### Serum Promotion Template
```markdown
## 🏆 SERUM VALIDATED - Narrative Outcome Confirmed

**This Gu Pot issue has been promoted to SERUM status based on empirical validation.**

### Validation Criteria Met
- ✅ **Deterministic reproducibility** - Experiment reproduced with seed `{global_seed}`
- ✅ **Baseline improvement** - {baseline_delta}% improvement over baseline
- ✅ **Statistical significance** - Confidence score: {confidence_score}
- ✅ **Complete artifact chain** - All evidence files preserved
- ✅ **Origin binding verified** - Traceable to this Gu Pot issue

### Validated Claims
{validated_claims_list}

### Implementation Recommendation
Based on experimental evidence, this narrative improvement should be:
1. **Prioritized for implementation** - High confidence in positive impact
2. **Integrated with existing systems** - Claims show compatibility
3. **Monitored post-deployment** - Baseline metrics tracked for regression

### Evidence Package
Complete evidence package available at: [`gu_pot/issue-{issue_number}/`](gu_pot/issue-{issue_number}/)
```

### Antitoxin Candidate Template
```markdown
## 🛡️ ANTITOXIN CANDIDATE - Defensive Value Validated

**This Gu Pot issue shows validated defensive/neutral value for system stability.**

### Validation Criteria Met
- ✅ **No negative impact** - Baseline metrics maintained or improved
- ✅ **Risk mitigation value** - {defensive_value_description}
- ✅ **Stability under stress** - Edge case testing passed
- ✅ **Rollback safety** - Reversion path validated

### Defensive Claims
{defensive_claims_list}

### Deployment Recommendation
This change provides defensive value and should be considered for:
1. **Risk mitigation scenarios** - Prevents identified failure modes
2. **Stability improvements** - Enhances system robustness
3. **Future-proofing** - Prepares for anticipated challenges

### Safety Assessment
- **Error Rate**: {error_rate}% (within acceptable bounds)
- **Rollback Tested**: ✅ Verified reversion path
- **Side Effects**: {side_effects_assessment}
```

### Compost Classification Template
```markdown
## 🗑️ COMPOSTED - Learning Extracted, Path Closed

**This Gu Pot issue did not validate but provided valuable learning.**

### Failure Analysis
- **Root Cause**: {failure_root_cause}
- **Learning Extracted**: {learning_points}
- **Resource Investment**: {resource_summary}

### What Worked
{successful_aspects}

### What Failed
{failure_aspects}

### Alternative Approaches
{suggested_alternatives}

### Residue Pointer
Knowledge preserved for future reference: [`gu_pot/issue-{issue_number}/compost_summary.md`](gu_pot/issue-{issue_number}/compost_summary.md)

**Note**: This issue will be closed but all experimental data is preserved for learning purposes.
```

## Automated Update Process

### GitHub API Integration
```python
# Example usage in update_issue_evidence.py
def update_issue_with_evidence(issue_number, evidence_data):
    template = load_evidence_template(evidence_data['stage_decision'])
    evidence_section = template.format(**evidence_data)
    
    # Find existing evidence section or append
    issue_body = get_issue_body(issue_number)
    if "## 🧪 Alchemist Evidence Links" in issue_body:
        # Replace existing evidence section
        updated_body = replace_evidence_section(issue_body, evidence_section)
    else:
        # Append new evidence section
        updated_body = issue_body + "\n\n" + evidence_section
    
    # Update GitHub issue
    update_github_issue(issue_number, updated_body)
    
    # Apply appropriate labels
    apply_stage_labels(issue_number, evidence_data['stage_decision'])
```

### Label Management
Automatically applies labels based on stage decision:

**Serum**: `alchemist:serum`, `validation:passed`, `ready-for-implementation`
**Antitoxin**: `alchemist:antitoxin`, `defensive-value`, `stability-improvement`  
**Compost**: `alchemist:compost`, `learning-extracted`, `closed-with-value`

### Comment Automation
```markdown
🧪 **Alchemist Faculty Update**

Experimental validation complete for this Gu Pot issue:
- **Stage Decision**: {stage_decision}
- **Confidence Score**: {confidence_score}
- **Evidence Package**: Available in repository

See updated issue description for complete evidence links and validation details.

*Automated by Alchemist Faculty v{alchemist_version}*
```

## Quality Assurance

### Template Validation
- All template variables must be populated
- Evidence links must point to existing files
- Stage decision must match validation criteria
- Confidence scores must be within valid range (0-1)

### Update Verification
- Issue update successful
- Labels applied correctly
- Evidence links accessible
- Formatting preserved

### Error Handling
- Network failures: Retry with exponential backoff
- Authentication errors: Clear error message with resolution steps
- Template errors: Validate before attempting update
- Permission errors: Check repository access rights

---
*Template Version: 0.1.0*  
*Compatible with: Alchemist Faculty v0.1.0*  
*Last Updated: 2025-09-06*