#!/bin/bash
# ⚡ Structure Preflight Check - Fast Fail for Repository Structure Issues
#
# This script performs sub-10s validation of critical repository structure
# to fail fast on common PR mistakes like misplaced files.
#
# Exit Codes:
#   0 - Structure is valid
#   1 - Structure violations found (expected, should be guarded)
#   2 - Critical failures that need immediate attention

set -euo pipefail

# Configuration
declare -a STRUCTURE_ERRORS=()
declare -a STRUCTURE_WARNINGS=()
START_TIME=$(date +%s)

# Helper functions
log_error() {
    STRUCTURE_ERRORS+=("$1")
    echo "❌ ERROR: $1"
}

log_warning() {
    STRUCTURE_WARNINGS+=("$1")
    echo "⚠️  WARNING: $1"
}

log_info() {
    echo "ℹ️  INFO: $1"
}

# Check 1: TLDL files should be in TLDL/ directory, not docs/
check_tldl_placement() {
    log_info "Checking TLDL file placement..."
    
    # Find TLDL files in docs/ (should be in TLDL/ instead)
    local misplaced_tldl=$(find docs/ -name "TLDL-*.md" 2>/dev/null | head -5)
    if [[ -n "$misplaced_tldl" ]]; then
        log_error "TLDL files found in docs/ directory - should be in TLDL/entries/:"
        while read -r file; do
            [[ -n "$file" ]] && echo "  → $file"
        done <<< "$misplaced_tldl"
    fi
    
    # Check if TLDL directory structure exists
    if [[ ! -d "TLDL" ]]; then
        log_warning "TLDL/ directory missing - may be intentional for template"
    elif [[ ! -d "TLDL/entries" ]]; then
        log_warning "TLDL/entries/ directory missing - TLDL files should go here"
    fi
}

# Check 2: Documentation files should not be in TLDL/ directory
check_docs_placement() {
    log_info "Checking documentation file placement..."
    
    # Find non-TLDL markdown files in TLDL directory
    # Accept both TLDL-YYYY-MM-DD-*.md (manual) and YYYY-MM-DD-*.md (Chronicle Keeper) patterns
    local misplaced_docs=$(find TLDL/ -name "*.md" ! -name "TLDL-*.md" ! -name "[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]-*.md" 2>/dev/null | head -5)
    if [[ -n "$misplaced_docs" ]]; then
        log_error "Non-TLDL documentation found in TLDL/ directory - should be in docs/:"
        while read -r file; do
            [[ -n "$file" ]] && echo "  → $file"
        done <<< "$misplaced_docs"
    fi
}

# Check 3: Required directories exist
check_required_directories() {
    log_info "Checking required directories..."
    
    local required_dirs=(".github/workflows" "scripts" "src")
    for dir in "${required_dirs[@]}"; do
        if [[ ! -d "$dir" ]]; then
            log_error "Required directory missing: $dir"
        fi
    done
}

# Check 4: Critical files are not accidentally deleted
check_critical_files() {
    log_info "Checking critical files..."
    
    local critical_files=("README.md" ".gitignore" "scripts/init_agent_context.sh")
    for file in "${critical_files[@]}"; do
        if [[ ! -f "$file" ]]; then
            log_warning "Critical file missing: $file"
        fi
    done
}

# Check 5: No obvious secrets or sensitive files committed
check_sensitive_files() {
    log_info "Checking for sensitive files..."
    
    local sensitive_patterns=("*.key" "*.pem" "*.env" "*password*" "*secret*")
    for pattern in "${sensitive_patterns[@]}"; do
        local found=$(find . -name "$pattern" -not -path "./.git/*" -not -path "./node_modules/*" 2>/dev/null | head -3)
        if [[ -n "$found" ]]; then
            log_warning "Potential sensitive file found (pattern: $pattern):"
            while read -r file; do
                [[ -n "$file" ]] && echo "  → $file"
            done <<< "$found"
        fi
    done
}

# Check 6: Workflow files are valid YAML (basic syntax check)
check_workflow_syntax() {
    log_info "Checking workflow file syntax..."
    
    if command -v python3 >/dev/null 2>&1; then
        for workflow in .github/workflows/*.yml .github/workflows/*.yaml; do
            if [[ -f "$workflow" ]]; then
                if ! python3 -c "import yaml; yaml.safe_load(open('$workflow'))" 2>/dev/null; then
                    log_error "Invalid YAML syntax in: $workflow"
                fi
            fi
        done 2>/dev/null || true
    else
        log_warning "Python3 not available - skipping YAML syntax validation"
    fi
}

# Main execution
main() {
    echo "⚡ Repository Structure Preflight Check"
    echo "======================================"
    
    # Run all checks
    check_tldl_placement
    check_docs_placement  
    check_required_directories
    check_critical_files
    check_sensitive_files
    check_workflow_syntax
    
    # Calculate execution time
    local end_time=$(date +%s)
    local duration=$((end_time - START_TIME))
    
    echo ""
    echo "📊 Preflight Summary"
    echo "==================="
    echo "Execution time: ${duration}s"
    echo "Errors: ${#STRUCTURE_ERRORS[@]}"
    echo "Warnings: ${#STRUCTURE_WARNINGS[@]}"
    
    # Exit based on results
    if [[ ${#STRUCTURE_ERRORS[@]} -gt 0 ]]; then
        echo ""
        echo "❌ Structure validation FAILED"
        echo "The following errors must be addressed:"
        for error in "${STRUCTURE_ERRORS[@]}"; do
            echo "  • $error"
        done
        
        # Exit code 1 for expected structure issues (will be caught by guarded-pass)
        exit 1
    elif [[ ${#STRUCTURE_WARNINGS[@]} -gt 0 ]]; then
        echo ""
        echo "⚠️  Structure validation completed with WARNINGS"
        echo "Consider addressing these warnings:"
        for warning in "${STRUCTURE_WARNINGS[@]}"; do
            echo "  • $warning"
        done
        
        # Exit code 0 for warnings - they're informational
        exit 0
    else
        echo ""
        echo "✅ Structure validation PASSED"
        echo "Repository structure looks good!"
        exit 0
    fi
}

# Handle command line arguments
if [[ "${1:-}" == "--help" ]] || [[ "${1:-}" == "-h" ]]; then
    echo "Usage: $0 [options]"
    echo ""
    echo "Options:"
    echo "  --help, -h    Show this help message"
    echo "  --verbose, -v Verbose output (not implemented yet)"
    echo ""
    echo "This script performs fast structure validation to catch common"
    echo "repository organization issues like misplaced TLDL files."
    echo ""
    echo "Exit codes:"
    echo "  0 - All checks passed (or warnings only)"
    echo "  1 - Structure errors found (expected, use with guarded-pass)"
    echo "  2 - Critical failures requiring immediate attention"
    exit 0
fi

# Execute main function
main "$@"