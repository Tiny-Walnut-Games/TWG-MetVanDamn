#!/bin/bash
# Package Installation Validator with Fail-Fast Logic
# Addresses issue #50 package fetch retry concerns

set -euo pipefail

# Configuration
MAX_RETRIES=3
RETRY_DELAY=2
TIMEOUT=30

log_info() {
    echo "ℹ️  $1"
}

log_error() {
    echo "❌ $1" >&2
}

log_success() {
    echo "✅ $1"
}

# Validate package with retry and hard fail
validate_package_install() {
    local package_name="$1"
    local install_cmd="$2"
    local attempt=1
    
    log_info "Validating package: $package_name"
    
    while [ $attempt -le $MAX_RETRIES ]; do
        log_info "Attempt $attempt/$MAX_RETRIES for $package_name"
        
        if timeout $TIMEOUT bash -c "$install_cmd"; then
            log_success "$package_name installed successfully"
            return 0
        else
            log_error "Failed to install $package_name (attempt $attempt/$MAX_RETRIES)"
            
            if [ $attempt -eq $MAX_RETRIES ]; then
                log_error "HARD FAIL: All retry attempts exhausted for $package_name"
                log_error "This addresses issue #50 - explicit abort after retry limit"
                return 1
            fi
            
            log_info "Retrying in ${RETRY_DELAY}s..."
            sleep $RETRY_DELAY
            ((attempt++))
        fi
    done
}

# Validate pinned dependencies (addresses issue #50 dynamic dependency concern)
validate_pinned_dependencies() {
    log_info "Validating pinned dependencies..."
    
    if [ -f "package.json" ]; then
        log_info "Checking package.json for dynamic dependencies..."
        
        # Check for @latest usage
        if grep -q "@latest" package.json; then
            log_error "@latest found in package.json - violates pinning policy"
            return 1
        fi
        
        # Check for @playwright/mcp pinning
        if ! grep -q '"@playwright/mcp".*"[0-9]' package.json; then
            log_error "@playwright/mcp version not properly pinned"
            return 1
        fi
        
        log_success "Package dependencies properly pinned"
    else
        log_info "No package.json found, skipping Node.js dependency validation"
    fi
    
    # Validate Python dependencies
    if [ -f "scripts/requirements.txt" ]; then
        log_info "Checking Python requirements for version pinning..."
        
        # Check for unpinned packages (basic check)
        if grep -E "^[a-zA-Z0-9_-]+$" scripts/requirements.txt; then
            log_error "Unpinned Python packages found in requirements.txt"
            return 1
        fi
        
        log_success "Python requirements properly versioned"
    fi
    
    return 0
}

# Main validation function
main() {
    local exit_code=0
    
    log_info "🔒 Package Installation Security Validator"
    log_info "Addresses security concerns from issue #50"
    echo ""
    
    # Validate pinned dependencies first
    if ! validate_pinned_dependencies; then
        exit_code=1
    fi
    
    # Test package installation with fail-fast logic
    log_info "Testing package installation with fail-fast logic..."
    
    # Test Python package installation
    if command -v pip3 &> /dev/null; then
        if validate_package_install "PyYAML" "pip3 install PyYAML>=6.0 --dry-run"; then
            log_success "Python package validation passed"
        else
            log_error "Python package validation failed"
            exit_code=1
        fi
    fi
    
    # Test Node.js package installation if npm is available and network accessible
    if command -v npm &> /dev/null; then
        # Quick network connectivity test for npm
        if timeout 5 npm ping &> /dev/null; then
            if validate_package_install "js-yaml" "npm install js-yaml@^4.1.0 --dry-run"; then
                log_success "Node.js package validation passed"
            else
                log_error "Node.js package validation failed"  
                exit_code=1
            fi
        else
            log_info "npm registry not accessible, skipping Node.js package validation"
        fi
    else
        log_info "npm not available, skipping Node.js package validation"
    fi
    
    echo ""
    if [ $exit_code -eq 0 ]; then
        log_success "All package validations passed"
        log_info "🛡️ Cheeks preserved from package installation issues!"
    else
        log_error "Package validation failed - explicit abort implemented"
        log_info "This prevents hanging installations as identified in issue #50"
    fi
    
    return $exit_code
}

# Run if called directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi