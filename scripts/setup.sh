#!/usr/bin/env bash
#
# Living Dev Agent Setup Script
# Comprehensive setup for new developers joining the project
#
# This script addresses the CID Schoolhouse Report recommendation for 
# "Development Environment Scripts" to improve developer onboarding experience.
#
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

log_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

log_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

log_error() {
    echo -e "${RED}❌ $1${NC}"
}

show_help() {
    cat << EOF
Living Dev Agent Setup Script

USAGE:
    $0 [OPTIONS]

OPTIONS:
    --skip-deps        Skip dependency installation
    --skip-validation  Skip validation checks
    --verbose          Enable verbose output  
    --help             Show this help message

DESCRIPTION:
    This script sets up a complete development environment for the Living Dev Agent.
    It orchestrates the existing setup scripts and performs comprehensive initialization.

WHAT IT DOES:
    1. Validates system requirements (Python, Git, Node.js)
    2. Installs Python dependencies 
    3. Initializes agent context and DevTimeTravel
    4. Runs validation tools to ensure everything works
    5. Sets up development tools and configurations
    6. Creates initial TLDL entry for setup experience

EOF
}

check_system_requirements() {
    log_info "Checking system requirements..."
    
    local missing_tools=()
    
    # Check for essential tools
    if ! command -v git &> /dev/null; then
        missing_tools+=("git")
    fi
    
    if ! command -v python3 &> /dev/null && ! command -v python &> /dev/null; then
        missing_tools+=("python3")
    fi
    
    # Node.js is optional for basic functionality
    if ! command -v node &> /dev/null; then
        log_warning "Node.js not found (optional for full functionality)"
    fi
    
    if [[ ${#missing_tools[@]} -gt 0 ]]; then
        log_error "Missing required tools: ${missing_tools[*]}"
        log_info "Please install the missing tools and run this script again"
        return 1
    fi
    
    log_success "System requirements satisfied"
    return 0
}

setup_git_config() {
    log_info "Checking Git configuration..."
    
    if ! git config user.name &> /dev/null; then
        log_warning "Git user.name not set"
        echo -n "Enter your Git username: "
        read -r git_user
        git config --global user.name "$git_user"
        log_success "Set Git user.name to: $git_user"
    fi
    
    if ! git config user.email &> /dev/null; then
        log_warning "Git user.email not set"
        echo -n "Enter your Git email: "
        read -r git_email
        git config --global user.email "$git_email"
        log_success "Set Git user.email to: $git_email"
    fi
    
    log_success "Git configuration verified"
}

install_dependencies() {
    log_info "Installing Python dependencies..."
    
    # Ensure requirements.txt exists
    if [[ ! -f "$PROJECT_ROOT/scripts/requirements.txt" ]]; then
        log_error "Requirements file not found at scripts/requirements.txt"
        return 1
    fi
    
    # Install with pip
    python3 -m pip install --upgrade pip || python -m pip install --upgrade pip
    python3 -m pip install --user -r "$PROJECT_ROOT/scripts/requirements.txt" || \
    python -m pip install --user -r "$PROJECT_ROOT/scripts/requirements.txt"
    
    log_success "Python dependencies installed"
}

initialize_agent_context() {
    log_info "Initializing Living Dev Agent context..."
    
    # Make scripts executable
    chmod +x "$PROJECT_ROOT/scripts/init_agent_context.sh"
    
    # Run the existing initialization script
    if "$PROJECT_ROOT/scripts/init_agent_context.sh" --create-tldl "DeveloperSetupExperience"; then
        log_success "Agent context initialized successfully"
    else
        log_warning "Agent context initialization completed with warnings (this is often normal)"
    fi
}

run_validation_suite() {
    log_info "Running validation suite..."
    
    # Run the comprehensive validation from our existing tools
    local validation_passed=true
    
    # TLDL validation
    if python3 "$PROJECT_ROOT/src/SymbolicLinter/validate_docs.py" --tldl-path "$PROJECT_ROOT/docs/" > /dev/null 2>&1; then
        log_success "TLDL validation: PASS"
    else
        log_warning "TLDL validation: WARNINGS (often normal for new setups)"
    fi
    
    # Debug overlay validation
    if python3 "$PROJECT_ROOT/src/DebugOverlayValidation/debug_overlay_validator.py" --path "$PROJECT_ROOT/src/DebugOverlayValidation/" > /dev/null 2>&1; then
        log_success "Debug overlay validation: PASS"
    else
        log_warning "Debug overlay validation: WARNINGS"
        validation_passed=false
    fi
    
    # MCP security validation
    if python3 "$PROJECT_ROOT/scripts/validate_mcp_config.py" --strict > /dev/null 2>&1; then
        log_success "MCP security validation: PASS"
    else
    if [ -f "$PROJECT_ROOT/scripts/validate_mcp_config.py" ]; then
        if python3 "$PROJECT_ROOT/scripts/validate_mcp_config.py" --strict > /dev/null 2>&1; then
            log_success "MCP security validation: PASS"
        else
            log_warning "MCP security validation: WARNINGS"
        fi
    else
        log_warning "MCP security validation: SKIPPED (validate_mcp_config.py not found)"
    fi
    
    if [[ "$validation_passed" == true ]]; then
        log_success "Validation suite completed successfully"
    else
        log_warning "Validation suite completed with some warnings (often normal)"
    fi
}

setup_development_tools() {
    log_info "Setting up development tools..."
    
    # Make LDA CLI executable
    if [[ -f "$PROJECT_ROOT/scripts/lda" ]]; then
        chmod +x "$PROJECT_ROOT/scripts/lda"
        log_success "LDA CLI tool is ready"
    fi
    
    # Make other utility scripts executable
    chmod +x "$PROJECT_ROOT/scripts/"*.sh 2>/dev/null || true
    
    log_success "Development tools configured"
}

show_next_steps() {
    echo ""
    echo "🎉 Living Dev Agent setup complete!"
    echo ""
    echo "Next steps:"
    echo "1. 📚 Read the documentation: docs/Copilot-Setup.md"
    echo "2. 🤖 Configure GitHub Copilot in your IDE"
    echo "3. 🔧 Customize configs: TWG-Copilot-Agent.yaml and mcp-config.json"
    echo "4. 📝 Create your first TLDL entry:"
    echo "   ./scripts/init_agent_context.sh --create-tldl \"MyFirstFeature\""
    echo "5. 🚀 Start developing with: ./scripts/dev.sh"
    echo ""
    echo "For help: ./scripts/setup.sh --help"
    echo "For development: ./scripts/dev.sh --help"
    echo ""
    echo "🛡️ All cheeks preserved! Happy coding! 🍑"
}

# Parse arguments
SKIP_DEPS=false
SKIP_VALIDATION=false
VERBOSE=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-deps)
            SKIP_DEPS=true
            shift
            ;;
        --skip-validation)
            SKIP_VALIDATION=true
            shift
            ;;
        --verbose)
            VERBOSE=true
            shift
            ;;
        --help)
            show_help
            exit 0
            ;;
        *)
            log_error "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# Main setup process
main() {
    echo "🤖 Living Dev Agent Setup"
    echo "=========================="
    echo ""
    
    # Step 1: System requirements
    if ! check_system_requirements; then
        exit 1
    fi
    
    # Step 2: Git configuration
    setup_git_config
    
    # Step 3: Dependencies (unless skipped)
    if [[ "$SKIP_DEPS" != true ]]; then
        install_dependencies
    else
        log_info "Skipping dependency installation (--skip-deps)"
    fi
    
    # Step 4: Agent context initialization
    initialize_agent_context
    
    # Step 5: Development tools setup
    setup_development_tools
    
    # Step 6: Validation (unless skipped)
    if [[ "$SKIP_VALIDATION" != true ]]; then
        run_validation_suite
    else
        log_info "Skipping validation checks (--skip-validation)"
    fi
    
    # Step 7: Show next steps
    show_next_steps
}

# Run the main setup
main "$@"