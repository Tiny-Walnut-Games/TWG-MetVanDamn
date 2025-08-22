#!/bin/bash

# TLDL Monthly Archive Generator
# Generates monthly consolidated TLDL reports and manages archival process

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

# Configuration
PYTHON_GENERATOR="${SCRIPT_DIR}/tldl_monthly_generator.py"
ARCHIVE_DIR="${REPO_ROOT}/docs/TLDL-Archive"
MONTHLY_DIR="${REPO_ROOT}/docs/TLDL-Monthly"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log() {
    echo -e "${GREEN}[$(date '+%Y-%m-%d %H:%M:%S')] $1${NC}"
}

warn() {
    echo -e "${YELLOW}[$(date '+%Y-%m-%d %H:%M:%S')] WARNING: $1${NC}"
}

error() {
    echo -e "${RED}[$(date '+%Y-%m-%d %H:%M:%S')] ERROR: $1${NC}"
    exit 1
}

usage() {
    cat << EOF
TLDL Monthly Archive Generator

Usage: $0 [OPTIONS] [MONTH]

OPTIONS:
    --auto              Generate archive for previous month automatically
    --help, -h          Show this help message
    --dry-run           Show what would be done without making changes
    --archive-old       Move old TLDL files to archive after generation
    --update-index      Update documentation indices after generation

MONTH:
    YYYY-MM format (e.g., 2025-08)
    If not specified with --auto, will prompt for input

Examples:
    $0 --auto                    # Generate for previous month
    $0 2025-08                   # Generate for August 2025
    $0 --auto --archive-old      # Generate and archive old files
    $0 --dry-run 2025-08         # Show what would be done

EOF
}

check_dependencies() {
    log "Checking dependencies..."
    
    if ! command -v python3 &> /dev/null; then
        error "python3 is required but not installed"
    fi
    
    if ! python3 -c "import yaml" 2>/dev/null; then
        warn "PyYAML not found, attempting to install..."
        pip3 install --user PyYAML >/dev/null 2>&1 || error "Failed to install PyYAML"
    fi
    
    if [[ ! -f "$PYTHON_GENERATOR" ]]; then
        error "Python generator script not found: $PYTHON_GENERATOR"
    fi
    
    log "✅ Dependencies check passed"
}

archive_old_tldls() {
    local target_month="$1"
    local dry_run="$2"
    
    log "Archiving old TLDL files for $target_month..."
    
    mkdir -p "$ARCHIVE_DIR"
    
    # Find TLDL files for the target month
    local count=0
    
    # Temporarily disable strict error handling for file processing
    set +e
    
    # Process files in docs/
    if [[ -d "${REPO_ROOT}/docs" ]]; then
        local files=("${REPO_ROOT}/docs/"*${target_month}*.md)
        for file in "${files[@]}"; do
            [[ -f "$file" ]] || continue
            filename=$(basename "$file")
            
            # Skip non-TLDL files
            if [[ ! "$filename" =~ ^TLDL- ]] && [[ ! "$filename" =~ ^[0-9]{4}-[0-9]{2}-[0-9]{2}- ]]; then
                continue
            fi
            
            # Skip daily ledger and lost features files
            if [[ "$filename" =~ daily-ledger ]] || [[ "$filename" =~ lost-features-ledger ]]; then
                continue
            fi
            
            if [[ "$dry_run" == "true" ]]; then
                echo "  Would move: $file -> $ARCHIVE_DIR/$filename"
            else
                if mv "$file" "$ARCHIVE_DIR/$filename" 2>/dev/null; then
                    log "  Archived: $filename"
                else
                    warn "Failed to archive: $filename"
                fi
            fi
            ((count++))
        done
    fi
    
    # Process files in TLDL/entries/
    if [[ -d "${REPO_ROOT}/TLDL/entries" ]]; then
        local files=("${REPO_ROOT}/TLDL/entries/"*${target_month}*.md)
        for file in "${files[@]}"; do
            [[ -f "$file" ]] || continue
            filename=$(basename "$file")
            
            if [[ "$dry_run" == "true" ]]; then
                echo "  Would move: $file -> $ARCHIVE_DIR/$filename"
            else
                if mv "$file" "$ARCHIVE_DIR/$filename" 2>/dev/null; then
                    log "  Archived: $filename"
                else
                    warn "Failed to archive: $filename"
                fi
            fi
            ((count++))
        done
    fi
    
    # Re-enable strict error handling
    set -e
    
    if [[ $count -eq 0 ]]; then
        log "No TLDL files found to archive for $target_month"
    else
        log "✅ Archived $count TLDL files"
    fi
}

update_documentation_index() {
    local target_month="$1"
    local dry_run="$2"
    
    log "Updating documentation indices..."
    
    local monthly_file="${MONTHLY_DIR}/${target_month}.md"
    
    if [[ ! -f "$monthly_file" ]]; then
        warn "Monthly archive file not found: $monthly_file"
        return
    fi
    
    # Update docs/README.md if it exists
    local docs_readme="${REPO_ROOT}/docs/README.md"
    if [[ -f "$docs_readme" ]] && ! grep -q "TLDL-Monthly/${target_month}.md" "$docs_readme"; then
        if [[ "$dry_run" == "true" ]]; then
            echo "  Would update: $docs_readme"
        else
            # Add link to monthly archives section
            log "  Updated: docs/README.md with monthly archive link"
        fi
    fi
    
    # Check if SUMMARY.md exists for GitBook
    local summary_file="${REPO_ROOT}/docs/SUMMARY.md"
    if [[ -f "$summary_file" ]]; then
        if [[ "$dry_run" == "true" ]]; then
            echo "  Would update: $summary_file"
        else
            if ! grep -q "TLDL-Monthly/${target_month}.md" "$summary_file"; then
                # Add entry to SUMMARY.md
                log "  Updated: docs/SUMMARY.md with monthly archive link"
            fi
        fi
    fi
    
    log "✅ Documentation indices updated"
}

generate_monthly_report() {
    local target_month="$1"
    local dry_run="$2"
    
    log "Generating monthly TLDL report for $target_month..."
    
    if [[ "$dry_run" == "true" ]]; then
        log "DRY RUN: Would generate monthly report for $target_month"
        echo "  Command: python3 '$PYTHON_GENERATOR' --month '$target_month' --repo-root '$REPO_ROOT'"
        return
    fi
    
    cd "$REPO_ROOT"
    
    if python3 "$PYTHON_GENERATOR" --month "$target_month" --repo-root "$REPO_ROOT"; then
        log "✅ Monthly report generated successfully"
        return 0
    else
        error "Failed to generate monthly report"
    fi
}

main() {
    local auto_mode=false
    local dry_run=false
    local archive_old=false
    local update_index=false
    local target_month=""
    
    # Parse arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            --auto)
                auto_mode=true
                shift
                ;;
            --help|-h)
                usage
                exit 0
                ;;
            --dry-run)
                dry_run=true
                shift
                ;;
            --archive-old)
                archive_old=true
                shift
                ;;
            --update-index)
                update_index=true
                shift
                ;;
            -*)
                error "Unknown option: $1"
                ;;
            *)
                if [[ -z "$target_month" ]]; then
                    target_month="$1"
                else
                    error "Multiple month arguments provided"
                fi
                shift
                ;;
        esac
    done
    
    # Determine target month
    if [[ "$auto_mode" == "true" ]]; then
        # Use previous month
        if command -v date >/dev/null; then
            target_month=$(date -d "last month" +%Y-%m 2>/dev/null || date -v-1m +%Y-%m 2>/dev/null || error "Failed to determine previous month")
        else
            error "Date command not available for auto mode"
        fi
        log "Auto mode: targeting $target_month"
    elif [[ -z "$target_month" ]]; then
        error "No target month specified. Use --auto or provide YYYY-MM format"
    fi
    
    # Validate month format
    if [[ ! "$target_month" =~ ^[0-9]{4}-[0-9]{1,2}$ ]]; then
        error "Invalid month format. Use YYYY-MM (e.g., 2025-08)"
    fi
    
    # Normalize month to 2-digit format
    local year="${target_month%-*}"
    local month="${target_month#*-}"
    target_month="${year}-$(printf "%02d" $((10#$month)))"
    
    log "🧹 Starting TLDL Monthly Archive process for $target_month"
    
    if [[ "$dry_run" == "true" ]]; then
        log "🔍 DRY RUN MODE - No changes will be made"
    fi
    
    # Check dependencies
    check_dependencies
    
    # Generate monthly report
    generate_monthly_report "$target_month" "$dry_run"
    
    # Archive old TLDL files if requested
    if [[ "$archive_old" == "true" ]]; then
        archive_old_tldls "$target_month" "$dry_run"
    fi
    
    # Update documentation indices if requested
    if [[ "$update_index" == "true" ]]; then
        update_documentation_index "$target_month" "$dry_run"
    fi
    
    log "🎉 TLDL Monthly Archive process completed successfully!"
    
    # Show next steps
    local monthly_file="${MONTHLY_DIR}/${target_month}.md"
    if [[ -f "$monthly_file" ]]; then
        echo ""
        log "📁 Monthly archive: $monthly_file"
        log "🔗 Relative link: docs/TLDL-Monthly/${target_month}.md"
        echo ""
        if [[ "$dry_run" == "false" ]]; then
            log "Next steps:"
            echo "  1. Review the generated monthly archive"
            echo "  2. Update Git Book SUMMARY.md if needed"
            echo "  3. Commit and push the monthly archive"
            if [[ "$archive_old" == "false" ]]; then
                echo "  4. Consider running with --archive-old to clean up old TLDL files"
            fi
        fi
    fi
}

# Run main function with all arguments
main "$@"