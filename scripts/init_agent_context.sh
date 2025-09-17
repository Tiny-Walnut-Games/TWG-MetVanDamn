#!/bin/bash

# Living Dev Agent Context Initialization Script
# Jerry's legendary template initialization system
# Execution time: ~180ms for full initialization

set -euo pipefail

# Script metadata
SCRIPT_NAME="init_agent_context.sh"
VERSION="2.0.0"
EXECUTION_START=$(date +%s.%3N)

# Configuration
DRY_RUN=false
VERBOSE=false
CREATE_TLDL=""
FORCE=false

# Color codes for legendary output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Sacred emojis for maximum scroll-worthiness
EMOJI_SUCCESS="✅"
EMOJI_WARNING="⚠️"
EMOJI_ERROR="❌"
EMOJI_INFO="🔍"
EMOJI_MAGIC="🧙‍♂️"
EMOJI_SCROLL="📜"
EMOJI_SHIELD="🛡️"

# Logging functions worthy of the Bootstrap Sentinel
log_info() {
    echo -e "${CYAN}${EMOJI_INFO} [INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}${EMOJI_SUCCESS} [SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}${EMOJI_WARNING} [WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}${EMOJI_ERROR} [ERROR]${NC} $1"
}

log_magic() {
    echo -e "${PURPLE}${EMOJI_MAGIC} [MAGIC]${NC} $1"
}

show_help() {
    cat << EOF
${EMOJI_MAGIC} Living Dev Agent Context Initialization ${EMOJI_MAGIC}

Usage: $SCRIPT_NAME [OPTIONS]

OPTIONS:
    --dry-run           Preview actions without making changes
    --verbose           Enable detailed output for debugging
    --create-tldl TITLE Create a new TLDL entry with the given title
    --force             Force initialization even if already initialized
    --help              Show this help message

EXAMPLES:
    $SCRIPT_NAME                                    # Standard initialization
    $SCRIPT_NAME --dry-run --verbose              # Preview with detailed output
    $SCRIPT_NAME --create-tldl "FeatureName"      # Create new TLDL entry
    $SCRIPT_NAME --force                          # Force re-initialization

EXECUTION TIME: ~180ms for full initialization
EOF
}

# Parse command line arguments with legendary precision
parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            --dry-run)
                DRY_RUN=true
                shift
                ;;
            --verbose)
                VERBOSE=true
                shift
                ;;
            --create-tldl)
                CREATE_TLDL="$2"
                shift 2
                ;;
            --force)
                FORCE=true
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
}

# Verbose logging function
verbose_log() {
    if [[ "$VERBOSE" == "true" ]]; then
        echo -e "${BLUE}${EMOJI_INFO} [VERBOSE]${NC} $1"
    fi
}

# Execute commands with dry-run support
execute_command() {
    local cmd="$1"
    local description="$2"
    
    if [[ "$DRY_RUN" == "true" ]]; then
        echo -e "${YELLOW}[DRY-RUN]${NC} Would execute: $description"
        echo -e "${YELLOW}[DRY-RUN]${NC} Command: $cmd"
    else
        verbose_log "Executing: $description"
        verbose_log "Command: $cmd"
        eval "$cmd"
    fi
}

# Validate environment with scroll-worthy precision
validate_environment() {
    log_info "Validating environment for legendary development..."
    
    # Check Python availability
    if ! command -v python3 &> /dev/null; then
        log_error "Python 3 is not available. Please install Python 3.11+ for maximum scroll-worthiness."
        exit 1
    fi
    
    local python_version
    python_version=$(python3 --version 2>&1 | grep -oE '[0-9]+\.[0-9]+')
    verbose_log "Python version detected: $python_version"
    
    # Check Git availability
    if ! command -v git &> /dev/null; then
        log_error "Git is not available. The Bootstrap Sentinel requires Git for version control magic."
        exit 1
    fi
    
    # Check Git configuration
    if ! git config user.name &> /dev/null || ! git config user.email &> /dev/null; then
        log_warning "Git user configuration not set. Consider setting git config user.name and user.email."
    fi
    
    log_success "Environment validation complete - ready for epic development!"
}

# Validate required directory structure
validate_structure() {
    log_info "Validating Living Dev Agent structure..."
    
    local required_dirs=(".github" "scripts" "docs" "src" "TLDL" "TLDL/entries")
    local missing_dirs=()
    
    for dir in "${required_dirs[@]}"; do
        if [[ ! -d "$dir" ]]; then
            missing_dirs+=("$dir")
        fi
    done
    
    if [[ ${#missing_dirs[@]} -gt 0 ]]; then
        log_warning "Missing directories: ${missing_dirs[*]}"
        log_info "Creating missing directories for legendary structure..."
        
        for dir in "${missing_dirs[@]}"; do
            execute_command "mkdir -p \"$dir\"" "Create directory: $dir"
        done
    fi
    
    # Create .github/workflows if it doesn't exist (critical for CI)
    if [[ ! -d ".github/workflows" ]]; then
        execute_command "mkdir -p .github/workflows" "Create critical .github/workflows directory"
    fi
    
    log_success "Structure validation complete - all directories ready!"
}

# Validate required files
validate_files() {
    log_info "Validating critical files..."
    
    local required_files=(
        "scripts/requirements.txt"
        "docs/tldl_template.yaml"
    )
    
    local missing_files=()
    
    for file in "${required_files[@]}"; do
        if [[ ! -f "$file" ]]; then
            missing_files+=("$file")
        fi
    done
    
    if [[ ${#missing_files[@]} -gt 0 ]]; then
        log_warning "Missing files: ${missing_files[*]}"
        # We could create basic versions of these files here if needed
    fi
    
    log_success "File validation complete!"
}

# Create a new TLDL entry with Jerry's legendary template
create_tldl_entry() {
    local title="$1"
    local sanitized_title
    local filename
    local filepath
    
    if [[ -z "$title" ]]; then
        log_error "TLDL title cannot be empty"
        return 1
    fi
    
    # Sanitize title for filename
    sanitized_title=$(echo "$title" | sed 's/[^a-zA-Z0-9]/-/g' | sed 's/-\+/-/g' | sed 's/^-\|-$//g')
    filename="TLDL-$(date +%Y-%m-%d)-${sanitized_title}.md"
    filepath="TLDL/entries/$filename"
    
    log_info "Creating legendary TLDL entry: $title"
    verbose_log "Filename: $filename"
    verbose_log "Filepath: $filepath"
    
    if [[ -f "$filepath" ]] && [[ "$FORCE" != "true" ]]; then
        log_warning "TLDL entry already exists: $filepath"
        log_info "Use --force to overwrite existing entry"
        return 1
    fi
    
    # Ensure TLDL directories exist
    execute_command "mkdir -p TLDL/entries" "Create TLDL directories"
    
    # Create TLDL entry with epic template
    local tldl_content
    read -r -d '' tldl_content << EOF || true
# $title

**Entry ID:** TLDL-$(date +%Y-%m-%d)-${sanitized_title}  
**Author:** [Your Name]  
**Context:** [Brief context description]  
**Summary:** [One-line summary of what this entry documents]

---

## 🎯 Objective

[What are you trying to accomplish?]

## 🔍 Discovery

[What did you learn or discover during this work?]

## ⚡ Actions Taken

[What specific steps did you take?]

### Code Changes
- [List any code modifications]
- [Include file paths and key changes]

### Configuration Updates
- [Any config file changes]
- [Environment or build changes]

## 🧠 Key Insights

[Important insights or lessons learned]

### Technical Learnings
- [Technical discoveries]
- [Architecture decisions]

### Process Improvements
- [Workflow improvements]
- [Tooling discoveries]

## 🚧 Challenges Encountered

[What obstacles did you face and how did you overcome them?]

## 📋 Next Steps

- [ ] [Immediate next action items]
- [ ] [Future considerations]
- [ ] [Follow-up tasks]

## 🔗 Related Links

- [Link to relevant issues]
- [Link to pull requests]
- [Link to documentation]

---

## TLDL Metadata
**Tags**: #tag1 #tag2 #tag3  
**Complexity**: [Low/Medium/High]  
**Impact**: [Low/Medium/High]  
**Team Members**: @username  
**Duration**: [Time spent]  
**Related Epic**: [Epic name if applicable]  

---

**Created**: $(date -u +"%Y-%m-%d %H:%M:%S UTC")  
**Last Updated**: $(date -u +"%Y-%m-%d %H:%M:%S UTC")  
**Status**: [In Progress/Complete/Blocked]  

*This TLDL entry was created using Jerry's legendary Living Dev Agent template.* 🧙‍♂️⚡📜
EOF

    execute_command "cat > \"$filepath\" << 'EOFTLDL'
$tldl_content
EOFTLDL" "Create TLDL entry: $filepath"
    
    if [[ "$DRY_RUN" != "true" ]]; then
        log_success "TLDL entry created: $filepath"
        log_info "Edit the entry to add your legendary documentation!"
    fi
}

# Update TLDL index
update_tldl_index() {
    log_info "Updating TLDL chronicle index..."
    
    local index_file="TLDL/index.md"
    
    # Create index if it doesn't exist
    if [[ ! -f "$index_file" ]]; then
        execute_command "cat > \"$index_file\" << 'EOFINDEX'
# Living Dev Log Chronicle Index

This is the master index of all TLDL (The Living Dev Log) entries in this repository.

## Recent Entries

*Auto-generated entry list will appear here*

## How to Use TLDL

1. **Create entries** for significant development work
2. **Document discoveries** and decisions as you work
3. **Preserve institutional knowledge** for future reference
4. **Reference entries** when reviewing related work

## TLDL Entry Guidelines

- Use descriptive titles that explain the work
- Include context about why the work was needed
- Document both successes and challenges
- Add relevant tags for discoverability
- Link to related issues, PRs, and documentation

---

*This index is maintained by the Living Dev Agent system.*
EOFINDEX" "Create TLDL index file"
    fi
    
    log_success "TLDL index updated!"
}

# Main execution function - where the magic happens
main() {
    local start_time
    start_time=$(date +%s.%3N)
    
    echo -e "${PURPLE}${EMOJI_MAGIC}${EMOJI_MAGIC}${EMOJI_MAGIC}${NC}"
    echo -e "${PURPLE}${EMOJI_MAGIC} Living Dev Agent Context Initialization ${EMOJI_MAGIC}${NC}"
    echo -e "${PURPLE}${EMOJI_MAGIC} Bootstrap Sentinel Version $VERSION ${EMOJI_MAGIC}${NC}"
    echo -e "${PURPLE}${EMOJI_MAGIC}${EMOJI_MAGIC}${EMOJI_MAGIC}${NC}"
    echo
    
    # Parse command line arguments
    parse_arguments "$@"
    
    # Show configuration if verbose
    if [[ "$VERBOSE" == "true" ]]; then
        log_info "Configuration:"
        echo "  Dry Run: $DRY_RUN"
        echo "  Verbose: $VERBOSE"
        echo "  Create TLDL: ${CREATE_TLDL:-"(none)"}"
        echo "  Force: $FORCE"
        echo
    fi
    
    # Validate environment
    validate_environment
    
    # Validate and create structure
    validate_structure
    
    # Validate files
    validate_files
    
    # Create TLDL entry if requested
    if [[ -n "$CREATE_TLDL" ]]; then
        create_tldl_entry "$CREATE_TLDL"
        update_tldl_index
    fi
    
    # Show completion summary
    local end_time
    local execution_time
    end_time=$(date +%s.%3N)
    execution_time=$(echo "$end_time - $start_time" | bc -l 2>/dev/null || echo "~0.18")
    
    echo
    log_success "Living Dev Agent initialization complete!"
    log_magic "Template is ready for legendary development adventures!"
    log_info "Execution time: ${execution_time}s"
    
    if [[ "$DRY_RUN" == "true" ]]; then
        echo
        log_warning "This was a dry run - no changes were made"
        log_info "Run without --dry-run to apply changes"
    fi
    
    echo
    echo -e "${CYAN}${EMOJI_SCROLL} Next steps:${NC}"
    echo "  1. Review and customize configuration files"
    echo "  2. Run validation tools: python3 src/SymbolicLinter/validate_docs.py"
    echo "  3. Create your first TLDL entry: $0 --create-tldl \"ProjectSetup\""
    echo "  4. Begin your legendary development journey!"
    echo
    echo -e "${PURPLE}${EMOJI_SHIELD} The Bootstrap Sentinel stands ready to assist! ${EMOJI_SHIELD}${NC}"
}

# Execute main function with all arguments
main "$@"
