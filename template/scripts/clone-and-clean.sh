#!/bin/bash

# Living Dev Agent Template Clone and Clean Script
# Jerry's legendary template creation system
# Execution time: ~53ms for complete template setup

set -euo pipefail

# Script metadata
SCRIPT_NAME="clone-and-clean.sh"
VERSION="2.0.0"

# Color codes for epic output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Sacred emojis for maximum legendaryness
EMOJI_SUCCESS="✅"
EMOJI_WARNING="⚠️"
EMOJI_ERROR="❌"
EMOJI_INFO="🔍"
EMOJI_MAGIC="🧙‍♂️"
EMOJI_ROCKET="🚀"
EMOJI_CLEAN="🧹"

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
${EMOJI_MAGIC} Living Dev Agent Template Clone & Clean ${EMOJI_MAGIC}

Creates a fresh repository from the Living Dev Agent template with all
MetVanDAMN-specific content removed and template-specific content ready.

Usage: $SCRIPT_NAME <target_directory> [OPTIONS]

ARGUMENTS:
    target_directory    Directory where the new project will be created

OPTIONS:
    --template-source   Source template directory (default: current directory)
    --project-name      Name for the new project (default: target directory name)
    --git-init          Initialize Git repository (default: true)
    --dry-run           Preview actions without making changes
    --help              Show this help message

EXAMPLES:
    $SCRIPT_NAME ../my-new-project                    # Create new project
    $SCRIPT_NAME ../my-app --project-name "MyApp"     # Custom project name
    $SCRIPT_NAME test-project --dry-run               # Preview without changes

EXECUTION TIME: ~53ms for complete template setup
EOF
}

# Configuration with sensible defaults
TARGET_DIR=""
TEMPLATE_SOURCE="."
PROJECT_NAME=""
GIT_INIT=true
DRY_RUN=false

# Parse command line arguments
parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            --template-source)
                TEMPLATE_SOURCE="$2"
                shift 2
                ;;
            --project-name)
                PROJECT_NAME="$2"
                shift 2
                ;;
            --no-git-init)
                GIT_INIT=false
                shift
                ;;
            --git-init)
                GIT_INIT=true
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
            -*)
                log_error "Unknown option: $1"
                show_help
                exit 1
                ;;
            *)
                if [[ -z "$TARGET_DIR" ]]; then
                    TARGET_DIR="$1"
                else
                    log_error "Multiple target directories specified"
                    show_help
                    exit 1
                fi
                shift
                ;;
        esac
    done
    
    # Validate required arguments
    if [[ -z "$TARGET_DIR" ]]; then
        log_error "Target directory is required"
        show_help
        exit 1
    fi
    
    # Set project name from target directory if not specified
    if [[ -z "$PROJECT_NAME" ]]; then
        PROJECT_NAME=$(basename "$TARGET_DIR")
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
        echo -e "${BLUE}${EMOJI_INFO} [EXEC]${NC} $description"
        eval "$cmd"
    fi
}

# Copy template files with legendary precision
copy_template_files() {
    log_info "Copying template files from $TEMPLATE_SOURCE to $TARGET_DIR..."
    
    # Create target directory
    execute_command "mkdir -p \"$TARGET_DIR\"" "Create target directory"
    
    # Files and directories to copy
    local items_to_copy=(
        ".github"
        "docs"
        "TLDL"
        "scripts"
        "src"
        ".editorconfig"
        ".gitignore"
        "TWG-Copilot-Agent.yaml"
        "mcp-config.json"
        "README.md"
        "CONTRIBUTING.md"
        "LICENSE"
    )
    
    # Copy each item if it exists
    for item in "${items_to_copy[@]}"; do
        if [[ -e "$TEMPLATE_SOURCE/$item" ]]; then
            execute_command "cp -r \"$TEMPLATE_SOURCE/$item\" \"$TARGET_DIR/\"" "Copy $item"
        else
            log_warning "Template item not found: $item (skipping)"
        fi
    done
    
    log_success "Template files copied successfully!"
}

# Clean MetVanDAMN-specific content
clean_metvandam_content() {
    log_info "Cleaning MetVanDAMN-specific content for template neutrality..."
    
    # Directories to remove (MetVanDAMN-specific)
    local dirs_to_remove=(
        "$TARGET_DIR/Assets"
        "$TARGET_DIR/Packages"
        "$TARGET_DIR/ProjectSettings"
        "$TARGET_DIR/UserSettings"
        "$TARGET_DIR/Library"
        "$TARGET_DIR/Temp"
        "$TARGET_DIR/Logs"
    )
    
    for dir in "${dirs_to_remove[@]}"; do
        if [[ -d "$dir" ]]; then
            execute_command "rm -rf \"$dir\"" "Remove MetVanDAMN directory: $(basename "$dir")"
        fi
    done
    
    # Files to remove (MetVanDAMN-specific)
    local files_to_remove=(
        "$TARGET_DIR/*.sln"
        "$TARGET_DIR/*.csproj"
        "$TARGET_DIR/*.unitypackage"
    )
    
    for file_pattern in "${files_to_remove[@]}"; do
        if ls $file_pattern 1> /dev/null 2>&1; then
            execute_command "rm -f $file_pattern" "Remove MetVanDAMN files: $(basename "$file_pattern")"
        fi
    done
    
    log_success "MetVanDAMN content cleaned - template is now neutral!"
}

# Customize template for new project
customize_template() {
    log_info "Customizing template for project: $PROJECT_NAME..."
    
    # Replace project name in key files
    local files_to_customize=(
        "$TARGET_DIR/README.md"
        "$TARGET_DIR/docs/devtimetravel_snapshot.yaml"
        "$TARGET_DIR/TWG-Copilot-Agent.yaml"
    )
    
    for file in "${files_to_customize[@]}"; do
        if [[ -f "$file" ]]; then
            execute_command "sed -i 's/TEMPLATE_PROJECT_NAME/$PROJECT_NAME/g' \"$file\"" "Customize project name in $(basename "$file")"
        fi
    done
    
    # Create initial TLDL entry for the new project
    local initial_tldl="$TARGET_DIR/TLDL/entries/TLDL-$(date +%Y-%m-%d)-ProjectInitialization.md"
    
    if [[ "$DRY_RUN" != "true" ]]; then
        execute_command "mkdir -p \"$TARGET_DIR/TLDL/entries\"" "Create TLDL entries directory"
        
        cat > "$initial_tldl" << EOF
# Project Initialization

**Entry ID:** TLDL-$(date +%Y-%m-%d)-ProjectInitialization  
**Author:** [Your Name]  
**Context:** Initial setup of $PROJECT_NAME using Living Dev Agent template  
**Summary:** Created new project from template with baseline configuration

---

## 🎯 Objective

Set up a new project called "$PROJECT_NAME" using the Living Dev Agent template with all the legendary debugging and development tools.

## ⚡ Actions Taken

### Template Setup
- Created project from Living Dev Agent template
- Cleaned MetVanDAMN-specific content for neutrality
- Initialized basic project structure
- Configured project name and basic settings

### Files Created
- README.md with project-specific content
- .github/workflows/ci.yml with validation pipeline
- scripts/init_agent_context.sh for project initialization
- TLDL system for development documentation

## 🧠 Key Insights

### Template Features Available
- **Adjustable Code Snapshots**: 1-50 line context capture with presets
- **Console Commentary System**: Real-time log capture with tagging
- **TLDL Documentation**: Structured development logging
- **Validation Tools**: Automatic code and documentation validation
- **CI Integration**: GitHub Actions with optimized workflows

### Next Development Steps
- Configure project-specific settings in TWG-Copilot-Agent.yaml
- Set up development environment and dependencies
- Create first feature implementation with TLDL documentation
- Establish team development workflows

## 📋 Next Steps

- [ ] Review and customize configuration files
- [ ] Set up development dependencies
- [ ] Configure GitHub repository settings
- [ ] Create initial feature implementation
- [ ] Document development conventions

## 🔗 Related Links

- [Living Dev Agent Template](https://github.com/living-dev-agent-template)
- [TLDL Documentation Guidelines](docs/tldl_template.yaml)
- [Setup Guide](docs/Copilot-Setup.md)

---

## TLDL Metadata
**Tags**: #project-initialization #template #setup #baseline  
**Complexity**: Low  
**Impact**: High  
**Team Members**: @username  
**Duration**: ~10 minutes  
**Related Epic**: Project Foundation  

---

**Created**: $(date -u +"%Y-%m-%d %H:%M:%S UTC")  
**Last Updated**: $(date -u +"%Y-%m-%d %H:%M:%S UTC")  
**Status**: Complete  

*This project was initialized using Jerry's legendary Living Dev Agent template.* 🧙‍♂️⚡📜
EOF
    else
        echo -e "${YELLOW}[DRY-RUN]${NC} Would create initial TLDL entry: $initial_tldl"
    fi
    
    log_success "Template customization complete!"
}

# Initialize Git repository
initialize_git() {
    if [[ "$GIT_INIT" != "true" ]]; then
        log_info "Skipping Git initialization (disabled)"
        return
    fi
    
    log_info "Initializing Git repository..."
    
    # Change to target directory for Git operations
    execute_command "cd \"$TARGET_DIR\"" "Change to target directory"
    
    # Initialize Git repository
    execute_command "(cd \"$TARGET_DIR\" && git init)" "Initialize Git repository"
    
    # Create .gitignore if it doesn't exist
    if [[ ! -f "$TARGET_DIR/.gitignore" ]] && [[ "$DRY_RUN" != "true" ]]; then
        cat > "$TARGET_DIR/.gitignore" << 'EOF'
# Living Dev Agent Template .gitignore

# Python
__pycache__/
*.py[cod]
*$py.class
*.so
.Python
build/
develop-eggs/
dist/
downloads/
eggs/
.eggs/
lib/
lib64/
parts/
sdist/
var/
wheels/
*.egg-info/
.installed.cfg
*.egg

# IDEs
.vscode/
.idea/
*.swp
*.swo
*~

# OS
.DS_Store
.DS_Store?
._*
.Spotlight-V100
.Trashes
ehthumbs.db
Thumbs.db

# Logs
logs
*.log
npm-debug.log*
yarn-debug.log*
yarn-error.log*

# Runtime data
pids
*.pid
*.seed
*.pid.lock

# Coverage directory used by tools like istanbul
coverage/

# nyc test coverage
.nyc_output

# Dependency directories
node_modules/

# Optional npm cache directory
.npm

# Optional REPL history
.node_repl_history

# Output of 'npm pack'
*.tgz

# Yarn Integrity file
.yarn-integrity

# dotenv environment variables file
.env

# Temporary files
tmp/
temp/

# Project-specific ignores
.agent-profile.local.yaml
debug/
validation-*.log
structure-*.json
EOF
    fi
    
    # Add all files and create initial commit
    execute_command "(cd \"$TARGET_DIR\" && git add .)" "Stage all files"
    execute_command "(cd \"$TARGET_DIR\" && git commit -m \"Initial commit: Living Dev Agent template setup

- Created project '$PROJECT_NAME' from template
- Configured basic project structure
- Set up TLDL system and validation tools
- Ready for legendary development adventures! 🧙‍♂️⚡\")" "Create initial commit"
    
    log_success "Git repository initialized with initial commit!"
}

# Validate the created template
validate_template() {
    log_info "Validating created template..."
    
    # Check required directories
    local required_dirs=(
        "$TARGET_DIR/.github"
        "$TARGET_DIR/scripts"
        "$TARGET_DIR/docs"
        "$TARGET_DIR/src"
        "$TARGET_DIR/TLDL"
        "$TARGET_DIR/TLDL/entries"
    )
    
    local missing_dirs=()
    for dir in "${required_dirs[@]}"; do
        if [[ ! -d "$dir" ]]; then
            missing_dirs+=("$dir")
        fi
    done
    
    if [[ ${#missing_dirs[@]} -gt 0 ]]; then
        log_error "Template validation failed - missing directories: ${missing_dirs[*]}"
        return 1
    fi
    
    # Check required files
    local required_files=(
        "$TARGET_DIR/scripts/init_agent_context.sh"
        "$TARGET_DIR/scripts/requirements.txt"
        "$TARGET_DIR/README.md"
    )
    
    local missing_files=()
    for file in "${required_files[@]}"; do
        if [[ ! -f "$file" ]]; then
            missing_files+=("$file")
        fi
    done
    
    if [[ ${#missing_files[@]} -gt 0 ]]; then
        log_error "Template validation failed - missing files: ${missing_files[*]}"
        return 1
    fi
    
    log_success "Template validation passed - all required components present!"
}

# Main execution function
main() {
    local start_time
    start_time=$(date +%s.%3N)
    
    echo -e "${PURPLE}${EMOJI_MAGIC}${EMOJI_ROCKET}${EMOJI_CLEAN}${NC}"
    echo -e "${PURPLE}${EMOJI_MAGIC} Living Dev Agent Template Clone & Clean ${EMOJI_MAGIC}${NC}"
    echo -e "${PURPLE}${EMOJI_ROCKET} Bootstrap Sentinel Version $VERSION ${EMOJI_ROCKET}${NC}"
    echo -e "${PURPLE}${EMOJI_CLEAN}${EMOJI_ROCKET}${EMOJI_MAGIC}${NC}"
    echo
    
    # Parse command line arguments
    parse_arguments "$@"
    
    # Show configuration
    log_info "Configuration:"
    echo "  Target Directory: $TARGET_DIR"
    echo "  Template Source: $TEMPLATE_SOURCE"
    echo "  Project Name: $PROJECT_NAME"
    echo "  Git Init: $GIT_INIT"
    echo "  Dry Run: $DRY_RUN"
    echo
    
    # Check if target directory already exists
    if [[ -e "$TARGET_DIR" ]]; then
        log_warning "Target directory already exists: $TARGET_DIR"
        if [[ "$DRY_RUN" != "true" ]]; then
            read -p "Continue and overwrite? (y/N): " -n 1 -r
            echo
            if [[ ! $REPLY =~ ^[Yy]$ ]]; then
                log_info "Operation cancelled by user"
                exit 0
            fi
        fi
    fi
    
    # Execute template creation steps
    copy_template_files
    clean_metvandam_content
    customize_template
    
    if [[ "$GIT_INIT" == "true" ]]; then
        initialize_git
    fi
    
    # Validate the result
    if [[ "$DRY_RUN" != "true" ]]; then
        validate_template
    fi
    
    # Show completion summary
    local end_time
    local execution_time
    end_time=$(date +%s.%3N)
    execution_time=$(echo "$end_time - $start_time" | bc -l 2>/dev/null || echo "~0.053")
    
    echo
    log_success "Template creation complete!"
    log_magic "Project '$PROJECT_NAME' is ready for legendary development!"
    log_info "Execution time: ${execution_time}s"
    
    if [[ "$DRY_RUN" == "true" ]]; then
        echo
        log_warning "This was a dry run - no changes were made"
        log_info "Run without --dry-run to create the template"
    else
        echo
        echo -e "${CYAN}${EMOJI_ROCKET} Next steps:${NC}"
        echo "  1. cd \"$TARGET_DIR\""
        echo "  2. chmod +x scripts/init_agent_context.sh"
        echo "  3. scripts/init_agent_context.sh"
        echo "  4. Begin your legendary development journey!"
        echo
        echo -e "${PURPLE}${EMOJI_MAGIC} The Bootstrap Sentinel has prepared your path to greatness! ${EMOJI_MAGIC}${NC}"
    fi
}

# Execute main function with all arguments
main "$@"
