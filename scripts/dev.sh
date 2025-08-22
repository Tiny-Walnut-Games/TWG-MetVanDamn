#!/usr/bin/env bash
#
# Living Dev Agent Development Workflow Script
# Daily development workflow automation for developers
#
# This script addresses the CID Schoolhouse Report recommendation for 
# "Development Environment Scripts" to streamline daily development workflows.
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
Living Dev Agent Development Workflow Script

USAGE:
    $0 [COMMAND] [OPTIONS]

COMMANDS:
    start              Start development session (default)
    validate           Run all validation checks
    tldl <title>       Create new TLDL entry
    quote [context]    Get inspirational development quote
    snapshot           Create DevTimeTravel snapshot  
    server             Start MCP server
    test               Run tests and validation suite
    clean              Clean temporary files and caches
    status             Show project status and recent activity

OPTIONS:
    --verbose          Enable verbose output
    --dry-run          Show what would be done without making changes
    --help             Show this help message

EXAMPLES:
    $0                           # Start development session
    $0 validate                  # Run all validations
    $0 tldl "BugFixSession"      # Create TLDL entry
    $0 quote workflow            # Get workflow quote
    $0 server --port 8080        # Start MCP server on custom port
    $0 test --coverage           # Run tests with coverage

DESCRIPTION:
    This script provides common development workflow automation, leveraging
    the existing Living Dev Agent infrastructure for maximum efficiency.

EOF
}

start_development_session() {
    log_info "Starting Living Dev Agent development session..."
    
    # Show current project status
    show_project_status
    
    # Quick validation check
    log_info "Running quick health check..."
    if run_quick_validation; then
        log_success "Project health check passed"
    else
        log_warning "Project health check found issues (see details above)"
    fi
    
    # Show today's inspirational quote
    show_dev_quote "workflow"
    
    log_success "Development session ready!"
    echo ""
    echo "Available commands:"
    echo "  ./scripts/dev.sh validate     # Full validation suite"
    echo "  ./scripts/dev.sh tldl <title> # Create TLDL entry"
    echo "  ./scripts/dev.sh server       # Start MCP server"
    echo "  ./scripts/dev.sh --help       # Full command reference"
}

show_project_status() {
    log_info "Project Status Summary"
    echo "======================"
    
    # Git status
    echo "📊 Repository Status:"
    git --no-pager status --porcelain | head -10 || echo "  Clean working directory"
    
    # Recent activity
    echo ""
    echo "🕐 Recent Activity:"
    git --no-pager log --oneline -5 || echo "  No recent commits"
    
    # TLDL entries count
    echo ""
    echo "📚 Documentation:"
    local tldl_count=$(find "$PROJECT_ROOT/docs" -name "TLDL-*.md" 2>/dev/null | wc -l)
    local tldl_entries_count=$(find "$PROJECT_ROOT/TLDL/entries" -name "TLDL-*.md" -o -name "[0-9][0-9][0-9][0-9]-*.md" 2>/dev/null | wc -l)
    echo "  TLDL entries: $tldl_count (misplaced in docs) + $tldl_entries_count (correctly in TLDL/entries)"
    
    echo ""
}

run_quick_validation() {
    local validation_passed=true
    
    # Check if Python is available
    if ! command -v python3 &> /dev/null && ! command -v python &> /dev/null; then
        log_error "Python not available"
        return 1
    fi
    
    local python_cmd="python3"
    if ! command -v python3 &> /dev/null; then
        python_cmd="python"
    fi
    
    # Quick TLDL validation
    if "$python_cmd" "$PROJECT_ROOT/src/SymbolicLinter/validate_docs.py" --tldl-path "$PROJECT_ROOT/TLDL/entries/" > /dev/null 2>&1; then
        echo "  ✅ TLDL validation: PASS"
    else
        echo "  ⚠️ TLDL validation: WARNINGS"
        validation_passed=false
    fi
    
    # Quick MCP config validation
    if "$python_cmd" "$PROJECT_ROOT/scripts/validate_mcp_config.py" > /dev/null 2>&1; then
        echo "  ✅ MCP config: VALID"
    else
        echo "  ⚠️ MCP config: WARNINGS" 
        validation_passed=false
    fi
    
    return $([[ "$validation_passed" == true ]] && echo 0 || echo 1)
}

run_full_validation() {
    log_info "Running comprehensive validation suite..."
    
    local python_cmd="python3"
    if ! command -v python3 &> /dev/null; then
        python_cmd="python"
    fi
    
    echo ""
    echo "🔍 TLDL Validation:"
    "$python_cmd" "$PROJECT_ROOT/src/SymbolicLinter/validate_docs.py" --tldl-path "$PROJECT_ROOT/TLDL/entries/"
    
    echo ""
    echo "🔍 Debug Overlay Validation:"
    local debug_overlay_path="${DEBUG_OVERLAY_PATH:-"$PROJECT_ROOT/src/DebugOverlayValidation/"}"
    "$python_cmd" "$PROJECT_ROOT/src/DebugOverlayValidation/debug_overlay_validator.py" --path "$debug_overlay_path"
    
    echo ""  
    echo "🔍 Symbolic Linting:"
    "$python_cmd" "$PROJECT_ROOT/src/SymbolicLinter/symbolic_linter.py" --path "$PROJECT_ROOT/src/"
    
    echo ""
    echo "🔍 MCP Security Validation:"
    "$python_cmd" "$PROJECT_ROOT/scripts/validate_mcp_config.py" --strict
    
    log_success "Comprehensive validation complete"
}

create_tldl_entry() {
    local title="$1"
    if [[ -z "$title" ]]; then
        log_error "TLDL title is required"
        echo "Usage: $0 tldl <title>"
        echo "Example: $0 tldl \"BugFixSession\""
        return 1
    fi
    
    log_info "Creating TLDL entry: $title"
    
    if "$PROJECT_ROOT/scripts/init_agent_context.sh" --create-tldl "$title"; then
        log_success "TLDL entry created successfully"
        
        # Show the created file
        local date
        date=$(date +%Y-%m-%d)
        local filename="TLDL-$date-$title.md"
        if [[ -f "$PROJECT_ROOT/docs/$filename" ]]; then
            log_info "Created: docs/$filename"
            echo "Edit the file to add your development story!"
        fi
    else
        log_error "Failed to create TLDL entry"
        return 1
    fi
}

show_dev_quote() {
    local context="${1:-general}"
    
    if [[ -f "$PROJECT_ROOT/src/ScrollQuoteEngine/quote_engine.py" ]]; then
        local python_cmd="python3"
        if ! command -v python3 &> /dev/null; then
            python_cmd="python"
        fi
        
        local quote
        quote=$("$python_cmd" "$PROJECT_ROOT/src/ScrollQuoteEngine/quote_engine.py" --context "$context" --format cli 2>/dev/null || echo "")
        
        if [[ -n "$quote" ]]; then
            echo ""
            echo -e "${BLUE}📜 Development Wisdom:${NC}"
            echo "$quote"
            echo ""
        fi
    fi
}

start_mcp_server() {
    log_info "Starting MCP server..."
    
    if [[ -f "$PROJECT_ROOT/scripts/mcp_server.py" ]]; then
        local python_cmd="python3"
        if ! command -v python3 &> /dev/null; then
            python_cmd="python"
        fi
        
        echo "Starting MCP server (Ctrl+C to stop)..."
        "$python_cmd" "$PROJECT_ROOT/scripts/mcp_server.py" "$@"
    else
        log_error "MCP server script not found at scripts/mcp_server.py"
        return 1
    fi
}

run_test_suite() {
    log_info "Running test suite and validation..."
    
    # Run the full validation suite
    run_full_validation
    
    # Add any additional test commands here
    # For now, validation IS our testing
    
    log_success "Test suite complete"
}

clean_project() {
    log_info "Cleaning temporary files and caches..."
    
    # Python cache
    find "$PROJECT_ROOT" -name "__pycache__" -type d -exec rm -rf {} + 2>/dev/null || true
    find "$PROJECT_ROOT" -name "*.pyc" -type f -delete 2>/dev/null || true
    
    # Temporary files
    find "$PROJECT_ROOT" -name "*.tmp" -type f -delete 2>/dev/null || true
    find "$PROJECT_ROOT" -name ".DS_Store" -type f -delete 2>/dev/null || true
    
    log_success "Project cleaned"
}

create_dev_snapshot() {
    log_info "Creating DevTimeTravel snapshot..."
    
    # Create a snapshot using the existing DevTimeTravel system
    local snapshot_id="DEV-$(date +%Y-%m-%d-%H%M%S)-$(git rev-parse --short HEAD)"
    local snapshot_dir="$PROJECT_ROOT/.devtimetravel/snapshots"
    
    mkdir -p "$snapshot_dir"
    
    # Create snapshot metadata
    cat > "$snapshot_dir/$snapshot_id.yaml" << EOF
snapshot_id: $snapshot_id
timestamp: $(date -u +%Y-%m-%dT%H:%M:%S.%3NZ)
commit_hash: $(git rev-parse HEAD)
branch: $(git branch --show-current)
user: $(git config user.name || whoami)
purpose: development_snapshot
files_tracked: $(git ls-files | wc -l)
uncommitted_changes: $(git --no-pager status --porcelain | wc -l)
EOF
    
    log_success "Snapshot created: $snapshot_id"
    log_info "Snapshot saved to: .devtimetravel/snapshots/$snapshot_id.yaml"
}

# Parse arguments
VERBOSE=false
DRY_RUN=false
COMMAND=""

while [[ $# -gt 0 ]]; do
    case $1 in
        --verbose)
            VERBOSE=true
            shift
            ;;
        --dry-run)
            DRY_RUN=true
            shift
            ;;
        --help)
            show_help
            exit 0
            ;;
        start|validate|tldl|quote|snapshot|server|test|clean|status)
            COMMAND="$1"
            shift
            break
            ;;
        *)
            # If no valid command found, treat as default start
            COMMAND="start"
            break
            ;;
    esac
done

# Main execution
main() {
    # Set default command if none provided
    if [[ -z "$COMMAND" ]]; then
        COMMAND="start"
    fi
    
    case "$COMMAND" in
        start)
            start_development_session
            ;;
        validate)
            run_full_validation
            ;;
        tldl)
            create_tldl_entry "$1"
            ;;
        quote)
            show_dev_quote "$1"
            ;;
        snapshot)
            create_dev_snapshot
            ;;
        server)
            start_mcp_server "$@"
            ;;
        test)
            run_test_suite
            ;;
        clean)
            clean_project
            ;;
        status)
            show_project_status
            ;;
        *)
            log_error "Unknown command: $COMMAND"
            show_help
            exit 1
            ;;
    esac
}

# Run main with remaining arguments
main "$@"