#!/usr/bin/env bash
#
# MIT License
#
# Copyright (c) 2025 Jerry Meyer (Tiny Walnut Games)
#
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in all
# copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
# SOFTWARE.
#
# Living Dev Agent Story Initialization
# Unified onboarding system with Story Mode and Quick Mode options

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Source shared utilities
source "$SCRIPT_DIR/lib/lda_common.sh"

# Script configuration
SCRIPT_NAME="$(basename "$0")"
VERSION="1.0.0"

# Default options
STORY_MODE=false
QUICK_MODE=false
DRY_RUN=false
JSON_OUTPUT=""
LICENSE_PLAN=""
MODULES=()
ALL_MODULES=("ergonomics" "character" "context" "tldl" "xp" "unity" "ci" "comfort")

# Available modules with descriptions
declare -A MODULE_DESCRIPTIONS
MODULE_DESCRIPTIONS[ergonomics]="Set up ergonomic development practices and butt-saving protocols"
MODULE_DESCRIPTIONS[character]="Configure your developer character class and specialization"
MODULE_DESCRIPTIONS[context]="Initialize Living Dev Agent context and validation tools"
MODULE_DESCRIPTIONS[tldl]="Set up The Living Dev Log workflow and documentation system"
MODULE_DESCRIPTIONS[xp]="Configure experience tracking and progression systems"
MODULE_DESCRIPTIONS[unity]="Set up Unity integration and game development tools"
MODULE_DESCRIPTIONS[ci]="Configure continuous integration and automation workflows"
MODULE_DESCRIPTIONS[comfort]="Install comfort reminders and wellness protocols"

# JSON report data
declare -A REPORT_DATA

show_help() {
    cat << EOF
$(lda_cecho "$LDA_GOLD" "${LDA_EMOJI_MAGIC} Living Dev Agent Story Initialization ${LDA_EMOJI_MAGIC}")

Unified onboarding system providing Story Mode and Quick Mode experiences
for the Living Dev Agent development environment.

USAGE:
    $SCRIPT_NAME [OPTIONS]

MODES:
    --story                 Launch interactive Story Mode onboarding
    --quick                 Use Quick Mode for rapid setup
    
OPTIONS:
    --modules LIST          Comma-separated list of modules to install
                           Available: $(IFS=,; echo "${ALL_MODULES[*]}")
    --json-out FILE         Output onboarding report to JSON file
    --license-plan PLAN     License alignment plan (mit|apache|gpl)
    --dry-run              Preview actions without making changes
    --no-color             Disable colored output
    -h, --help             Show this help message

MODULES:
$(for module in "${ALL_MODULES[@]}"; do
    printf "    %-12s %s\n" "$module" "${MODULE_DESCRIPTIONS[$module]}"
done)

EXAMPLES:
    # Interactive story mode with full onboarding
    $SCRIPT_NAME --story
    
    # Quick setup with specific modules
    $SCRIPT_NAME --quick --modules ergonomics,character,tldl
    
    # Dry run with JSON output
    $SCRIPT_NAME --story --dry-run --json-out setup_plan.json

For more information, see docs/ONBOARDING_STORY.md
EOF
}

parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            --story)
                STORY_MODE=true
                shift
                ;;
            --quick)
                QUICK_MODE=true
                shift
                ;;
            --modules)
                if [[ -n "$2" ]]; then
                    IFS=',' read -ra MODULES <<< "$2"
                    shift 2
                else
                    lda_die "Error: --modules requires a comma-separated list"
                fi
                ;;
            --json-out)
                if [[ -n "$2" ]]; then
                    JSON_OUTPUT="$2"
                    shift 2
                else
                    lda_die "Error: --json-out requires a filename"
                fi
                ;;
            --license-plan)
                if [[ -n "$2" ]]; then
                    LICENSE_PLAN="$2"
                    shift 2
                else
                    lda_die "Error: --license-plan requires a plan (mit|apache|gpl)"
                fi
                ;;
            --dry-run)
                DRY_RUN=true
                shift
                ;;
            --no-color)
                export NO_COLOR=1
                source "$SCRIPT_DIR/lib/lda_common.sh"  # Reload with no color
                shift
                ;;
            -h|--help)
                show_help
                exit 0
                ;;
            *)
                lda_die "Unknown option: $1. Use --help for usage information."
                ;;
        esac
    done
}

validate_arguments() {
    if [[ "$STORY_MODE" == "true" && "$QUICK_MODE" == "true" ]]; then
        lda_die "Cannot specify both --story and --quick modes"
    fi
    
    if [[ "$STORY_MODE" == "false" && "$QUICK_MODE" == "false" ]]; then
        lda_log_warning "No mode specified, defaulting to Story Mode"
        STORY_MODE=true
    fi
    
    # Validate modules
    for module in "${MODULES[@]}"; do
        if [[ ! " ${ALL_MODULES[*]} " =~ " ${module} " ]]; then
            lda_die "Invalid module: $module. Available: $(IFS=,; echo "${ALL_MODULES[*]}")"
        fi
    done
    
    # Validate license plan
    if [[ -n "$LICENSE_PLAN" && ! "$LICENSE_PLAN" =~ ^(mit|apache|gpl)$ ]]; then
        lda_die "Invalid license plan: $LICENSE_PLAN. Must be one of: mit, apache, gpl"
    fi
}

initialize_report() {
    REPORT_DATA[timestamp]="$(lda_timestamp_iso)"
    REPORT_DATA[mode]=""
    REPORT_DATA[modules_requested]=""
    REPORT_DATA[modules_completed]=""
    REPORT_DATA[modules_failed]=""
    REPORT_DATA[license_plan]="$LICENSE_PLAN"
    REPORT_DATA[dry_run]="$DRY_RUN"
    REPORT_DATA[status]="in_progress"
    REPORT_DATA[errors]=""
    REPORT_DATA[warnings]=""
}

execute_module() {
    local module="$1"
    local status="success"
    local message=""
    
    lda_log_info "Executing module: $module"
    
    case "$module" in
        ergonomics)
            execute_ergonomics_module
            ;;
        character)
            execute_character_module
            ;;
        context)
            execute_context_module
            ;;
        tldl)
            execute_tldl_module
            ;;
        xp)
            execute_xp_module
            ;;
        unity)
            execute_unity_module
            ;;
        ci)
            execute_ci_module
            ;;
        comfort)
            execute_comfort_module
            ;;
        *)
            lda_log_error "Unknown module: $module"
            return 1
            ;;
    esac
}

execute_ergonomics_module() {
    lda_log_info "Setting up ergonomic development practices..."
    
    if [[ "$DRY_RUN" == "true" ]]; then
        lda_log_info "[DRY-RUN] Would execute ergonomics setup"
        return 0
    fi
    
    # Check if initMyButt.sh exists and is executable
    if [[ -x "$SCRIPT_DIR/initMyButt.sh" ]]; then
        lda_log_info "Running initMyButt.sh for ergonomics setup..."
        if "$SCRIPT_DIR/initMyButt.sh" --ergonomics-only --non-interactive; then
            lda_log_success "Ergonomics module completed successfully"
        else
            lda_log_warning "initMyButt.sh completed with warnings (often normal)"
        fi
    else
        lda_log_warning "initMyButt.sh not found or not executable, creating stub reminder"
        create_comfort_reminder
    fi
}

execute_character_module() {
    lda_log_info "Setting up developer character configuration..."
    
    if [[ "$DRY_RUN" == "true" ]]; then
        lda_log_info "[DRY-RUN] Would execute character setup"
        return 0
    fi
    
    if [[ -x "$SCRIPT_DIR/initMyButt.sh" ]]; then
        lda_log_info "Running character setup..."
        if lda_is_non_interactive; then
            "$SCRIPT_DIR/initMyButt.sh" --character-only --non-interactive
        else
            "$SCRIPT_DIR/initMyButt.sh" --character-only
        fi
        lda_log_success "Character module completed"
    else
        lda_log_warning "Character setup script not available, skipping"
    fi
}

execute_context_module() {
    lda_log_info "Initializing Living Dev Agent context..."
    
    if [[ "$DRY_RUN" == "true" ]]; then
        lda_log_info "[DRY-RUN] Would execute context initialization"
        return 0
    fi
    
    if [[ -x "$SCRIPT_DIR/init_agent_context.sh" ]]; then
        lda_log_info "Running agent context initialization..."
        "$SCRIPT_DIR/init_agent_context.sh"
        lda_log_success "Context module completed"
    else
        lda_log_warning "Agent context script not available, creating basic structure"
        mkdir -p "$PROJECT_ROOT/TLDL/entries"
        mkdir -p "$PROJECT_ROOT/.github/workflows"
    fi
}

execute_tldl_module() {
    lda_log_info "Setting up The Living Dev Log workflow..."
    
    if [[ "$DRY_RUN" == "true" ]]; then
        lda_log_info "[DRY-RUN] Would execute TLDL setup"
        return 0
    fi
    
    # Ensure TLDL directories exist
    mkdir -p "$PROJECT_ROOT/TLDL/entries"
    mkdir -p "$PROJECT_ROOT/docs"
    
    # Create TLDL index if it doesn't exist
    if [[ ! -f "$PROJECT_ROOT/TLDL/index.md" ]]; then
        cat > "$PROJECT_ROOT/TLDL/index.md" << 'EOF'
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
EOF
    fi
    
    lda_log_success "TLDL module completed"
}

execute_xp_module() {
    lda_log_info "Setting up experience tracking systems..."
    
    if [[ "$DRY_RUN" == "true" ]]; then
        lda_log_info "[DRY-RUN] Would execute XP setup"
        return 0
    fi
    
    # Create XP tracking directories if they don't exist
    mkdir -p "$PROJECT_ROOT/experience"
    
    lda_log_success "XP module completed"
}

execute_unity_module() {
    lda_log_info "Setting up Unity integration tools..."
    
    if [[ "$DRY_RUN" == "true" ]]; then
        lda_log_info "[DRY-RUN] Would execute Unity setup"
        return 0
    fi
    
    # Create Unity-specific directories if they don't exist
    mkdir -p "$PROJECT_ROOT/Assets"
    
    lda_log_success "Unity module completed"
}

execute_ci_module() {
    lda_log_info "Setting up continuous integration workflows..."
    
    if [[ "$DRY_RUN" == "true" ]]; then
        lda_log_info "[DRY-RUN] Would execute CI setup"
        return 0
    fi
    
    # Ensure GitHub workflows directory exists
    mkdir -p "$PROJECT_ROOT/.github/workflows"
    
    lda_log_success "CI module completed"
}

execute_comfort_module() {
    lda_log_info "Installing comfort reminders and wellness protocols..."
    
    if [[ "$DRY_RUN" == "true" ]]; then
        lda_log_info "[DRY-RUN] Would execute comfort setup"
        return 0
    fi
    
    create_comfort_reminder
    lda_log_success "Comfort module completed"
}

create_comfort_reminder() {
    cat > "$PROJECT_ROOT/.comfort_reminder" << 'EOF'
# Living Dev Agent Comfort Reminder

Remember to take breaks and maintain good ergonomic practices:

- Stand and stretch every 30 minutes
- Keep proper posture while coding
- Take your eyes off the screen regularly
- Stay hydrated
- Celebrate small victories

Your health is more important than any deadline!
EOF
    lda_log_success "Comfort reminder created at .comfort_reminder"
}

run_story_mode() {
    lda_show_banner "${LDA_EMOJI_MAGIC} STORY MODE ${LDA_EMOJI_MAGIC}" "Interactive Onboarding Experience"
    
    REPORT_DATA[mode]="story"
    
    lda_log_magic "Welcome to the Living Dev Agent onboarding adventure!"
    echo ""
    lda_log_info "Story Mode will guide you through setting up your development environment"
    lda_log_info "with interactive choices and explanations for each component."
    echo ""
    
    if lda_is_non_interactive; then
        lda_log_info "Non-interactive mode detected, using default modules"
        MODULES=("${ALL_MODULES[@]}")
    else
        select_modules_interactive
    fi
    
    run_modules
}

run_quick_mode() {
    lda_show_banner "${LDA_EMOJI_SPARKLE} QUICK MODE ${LDA_EMOJI_SPARKLE}" "Rapid Setup Experience"
    
    REPORT_DATA[mode]="quick"
    
    lda_log_info "Quick Mode: Setting up your development environment efficiently"
    echo ""
    
    # Use provided modules or default to essential ones
    if [[ ${#MODULES[@]} -eq 0 ]]; then
        MODULES=("ergonomics" "character" "context" "tldl" "comfort")
        lda_log_info "Using default module set: $(IFS=,; echo "${MODULES[*]}")"
    fi
    
    run_modules
}

select_modules_interactive() {
    lda_log_info "Please select the modules you'd like to install:"
    echo ""
    
    local selected=()
    for module in "${ALL_MODULES[@]}"; do
        if lda_confirm "Install $module module? (${MODULE_DESCRIPTIONS[$module]})" "y"; then
            selected+=("$module")
        fi
    done
    
    MODULES=("${selected[@]}")
    
    if [[ ${#MODULES[@]} -eq 0 ]]; then
        lda_log_warning "No modules selected, using minimal setup"
        MODULES=("context" "comfort")
    fi
    
    echo ""
    lda_log_info "Selected modules: $(IFS=,; echo "${MODULES[*]}")"
}

run_modules() {
    local completed=()
    local failed=()
    
    REPORT_DATA[modules_requested]="$(IFS=,; echo "${MODULES[*]}")"
    
    lda_log_info "Installing ${#MODULES[@]} modules..."
    echo ""
    
    for module in "${MODULES[@]}"; do
        lda_log_info "=== Module: $module ==="
        
        if execute_module "$module"; then
            completed+=("$module")
            lda_log_success "Module $module completed successfully"
        else
            failed+=("$module")
            lda_log_error "Module $module failed"
        fi
        echo ""
    done
    
    REPORT_DATA[modules_completed]="$(IFS=,; echo "${completed[*]}")"
    REPORT_DATA[modules_failed]="$(IFS=,; echo "${failed[*]}")"
    
    # Final summary
    lda_log_magic "Onboarding Summary:"
    lda_log_success "Completed modules: ${#completed[@]}"
    if [[ ${#failed[@]} -gt 0 ]]; then
        lda_log_warning "Failed modules: ${#failed[@]}"
    fi
    
    if [[ ${#failed[@]} -eq 0 ]]; then
        REPORT_DATA[status]="success"
        lda_log_success "All modules completed successfully!"
    else
        REPORT_DATA[status]="partial_success"
        lda_log_warning "Some modules failed, but core functionality should work"
    fi
}

write_json_report() {
    if [[ -z "$JSON_OUTPUT" ]]; then
        return 0
    fi
    
    lda_log_info "Writing onboarding report to $JSON_OUTPUT"
    
    # Create JSON report
    cat > "$JSON_OUTPUT" << EOF
{
  "timestamp": "$(lda_json_escape "${REPORT_DATA[timestamp]}")",
  "mode": "$(lda_json_escape "${REPORT_DATA[mode]}")",
  "modules_requested": "$(lda_json_escape "${REPORT_DATA[modules_requested]}")",
  "modules_completed": "$(lda_json_escape "${REPORT_DATA[modules_completed]}")",
  "modules_failed": "$(lda_json_escape "${REPORT_DATA[modules_failed]}")",
  "license_plan": "$(lda_json_escape "${REPORT_DATA[license_plan]}")",
  "dry_run": $(if [[ "$DRY_RUN" == "true" ]]; then echo "true"; else echo "false"; fi),
  "status": "$(lda_json_escape "${REPORT_DATA[status]}")",
  "version": "$VERSION"
}
EOF
    
    lda_log_success "Report written to $JSON_OUTPUT"
}

main() {
    parse_arguments "$@"
    validate_arguments
    initialize_report
    
    if [[ "$STORY_MODE" == "true" ]]; then
        run_story_mode
    elif [[ "$QUICK_MODE" == "true" ]]; then
        run_quick_mode
    fi
    
    write_json_report
    
    echo ""
    lda_log_magic "Living Dev Agent onboarding complete!"
    lda_log_info "Next steps:"
    lda_log_info "  - Review generated configurations"
    lda_log_info "  - Run validation: python3 src/SymbolicLinter/validate_docs.py"
    lda_log_info "  - Create your first TLDL entry"
    lda_log_info "  - Start developing with confidence!"
    echo ""
    lda_log_comfort "The Bootstrap Sentinel stands ready to assist!"
}

# Only run main if script is executed directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi