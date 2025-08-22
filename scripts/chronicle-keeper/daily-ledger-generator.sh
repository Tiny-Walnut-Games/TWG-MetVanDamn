#!/usr/bin/env bash
#
# Daily Ledger Generator
# Generates daily ledger entries from template with automatic date/arc detection
#
# 🧠 Daily Ledger Generator — Keeper‑plus Ritual
# Generates a new ledger from /daily-ledger/_TEMPLATE.md
# Purpose: Preserve daily arc context, decisions, and glyphs for continuity.
# Usage: Fill each section before day's end. Use Re‑entry Spell to reload tone/context in new thread.
# Links: Backward/forward to previous/next ledger to maintain unbroken chain.

set -e

# Colors for output
BLUE='\033[0;34m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
PURPLE='\033[0;35m'
NC='\033[0m'

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$(dirname "$SCRIPT_DIR")")"
LEDGER_DIR="$PROJECT_ROOT/docs/daily-ledger"
TEMPLATE_FILE="$LEDGER_DIR/_TEMPLATE.md"

# Ensure required directories exist
mkdir -p "$LEDGER_DIR"

show_help() {
    echo -e "${PURPLE}📜 Daily Ledger Generator — Keeper‑plus Ritual${NC}"
    echo ""
    echo "Generates new Daily Ledger entries with automatic date and arc detection."
    echo ""
    echo "Usage: $0 [command] [options]"
    echo ""
    echo "Commands:"
    echo "  generate [YYYY-MM-DD] [arc-name]  Generate ledger for specific date/arc"
    echo "  today [arc-name]                  Generate ledger for today"
    echo "  link-entries                      Update forward/backward links in all entries"
    echo "  list                              List all existing ledger entries"
    echo ""
    echo "Options:"
    echo "  --dry-run         Show what would be done without making changes"
    echo "  --verbose         Show detailed output"
    echo "  --force           Overwrite existing entries"
    echo "  --help, -h        Show this help"
    echo ""
    echo "Examples:"
    echo "  $0 today \"Feature Implementation\"     # Generate today's ledger"
    echo "  $0 generate 2025-08-19 \"Bug Hunt\"    # Generate specific date ledger"
    echo "  $0 link-entries --dry-run             # Preview linking operation"
    echo ""
    echo "🍑 Part of the Chronicle Keeper system - Two devs, one brain bank"
}

# Parse command line arguments
COMMAND=""
DRY_RUN=false
VERBOSE=false
FORCE=false
TARGET_DATE=""
ARC_NAME=""

while [[ $# -gt 0 ]]; do
    case $1 in
        generate|today|link-entries|list)
            COMMAND="$1"
            shift
            ;;
        --dry-run)
            DRY_RUN=true
            shift
            ;;
        --verbose)
            VERBOSE=true
            shift
            ;;
        --force)
            FORCE=true
            shift
            ;;
        --help|-h)
            show_help
            exit 0
            ;;
        -*)
            echo -e "${RED}❌ Unknown option: $1${NC}"
            show_help
            exit 1
            ;;
        *)
            if [[ -z "$TARGET_DATE" && "$COMMAND" == "generate" ]]; then
                TARGET_DATE="$1"
            elif [[ -z "$ARC_NAME" ]]; then
                ARC_NAME="$1"
            fi
            shift
            ;;
    esac
done

# Utility functions
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

log_verbose() {
    if [[ "$VERBOSE" == true ]]; then
        echo -e "${PURPLE}🔍 $1${NC}"
    fi
}

# Validate template exists
validate_template() {
    if [[ ! -f "$TEMPLATE_FILE" ]]; then
        log_error "Template file not found: $TEMPLATE_FILE"
        exit 1
    fi
    log_verbose "Template validated: $TEMPLATE_FILE"
}

# Get sorted list of existing ledger entries
get_existing_entries() {
    find "$LEDGER_DIR" -name "*.md" -not -name "_TEMPLATE.md" | \
        grep -E "[0-9]{4}-[0-9]{2}-[0-9]{2}\.md$" | \
        sort || true
}

# Get previous and next entries for a given date
get_navigation_entries() {
    local target_date="$1"
    local entries=($(get_existing_entries))
    local target_file="$LEDGER_DIR/$target_date.md"
    
    local prev_date=""
    local next_date=""
    
    for i in "${!entries[@]}"; do
        if [[ "${entries[i]}" == "$target_file" ]]; then
            if [[ $i -gt 0 ]]; then
                prev_date=$(basename "${entries[$((i-1))]}" .md)
            fi
            if [[ $((i+1)) -lt ${#entries[@]} ]]; then
                next_date=$(basename "${entries[$((i+1))]}" .md)
            fi
            break
        fi
    done
    
    echo "$prev_date|$next_date"
}

# Generate a new ledger entry
generate_ledger() {
    local date="$1"
    local arc="$2"
    
    # Use today's date if none provided
    if [[ -z "$date" ]]; then
        date=$(date +%Y-%m-%d)
    fi
    
    # Default arc name
    if [[ -z "$arc" ]]; then
        arc="Daily Development Arc"
    fi
    
    local output_file="$LEDGER_DIR/$date.md"
    
    log_info "Generating Daily Ledger for $date..."
    log_verbose "Arc: $arc"
    log_verbose "Output: $output_file"
    
    # Check if file exists and force not specified
    if [[ -f "$output_file" && "$FORCE" != true ]]; then
        log_error "Ledger entry already exists: $output_file"
        log_info "Use --force to overwrite existing entries"
        exit 1
    fi
    
    # Get navigation info
    local nav_info=$(get_navigation_entries "$date")
    local prev_date=$(echo "$nav_info" | cut -d'|' -f1)
    local next_date=$(echo "$nav_info" | cut -d'|' -f2)
    
    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would create ledger entry at $output_file"
        log_verbose "Arc: $arc"
        log_verbose "Previous: ${prev_date:-"None"}"
        log_verbose "Next: ${next_date:-"None"}"
        return
    fi
    
    # Generate the entry content
    local content=$(cat "$TEMPLATE_FILE")
    
    # Replace template variables
    content="${content//YYYY-MM-DD/$date}"
    content="${content//\{ARC_NAME\}/$arc}"
    content="${content//\{ARC_SUBTITLE\}/Chronicle Entry}"
    content="${content//\{MODE_TYPE\}/Keeper‑plus}"
    
    # Handle navigation links
    local nav_section=""
    if [[ -n "$prev_date" || -n "$next_date" ]]; then
        nav_section="### **5. Navigation**"$'\n'
        
        if [[ -n "$prev_date" ]]; then
            nav_section+="**Previous Ledger:** [$prev_date](./$prev_date.md)"
        else
            nav_section+="**Previous Ledger:** None (First Entry)"
        fi
        
        nav_section+=" | "
        
        if [[ -n "$next_date" ]]; then
            nav_section+="**Next Ledger:** [$next_date](./$next_date.md)"
        else
            nav_section+="**Next Ledger:** TBD"
        fi
        
        nav_section+=$'\n\n---'
    fi
    
    # Replace navigation section with a simpler approach
    content="${content//\{PREV_DATE\}/PREV_PLACEHOLDER}"
    content="${content//\{NEXT_DATE\}/NEXT_PLACEHOLDER}"
    
    # Write the content to temp file first, then fix navigation
    local temp_file=$(mktemp)
    echo "$content" > "$temp_file"
    
    # Now fix the navigation section with proper values
    while IFS= read -r line; do
        if [[ "$line" =~ PREV_PLACEHOLDER.*NEXT_PLACEHOLDER ]]; then
            # Replace the navigation line
            if [[ -n "$prev_date" ]]; then
                if [[ -n "$next_date" ]]; then
                    echo "**Previous Ledger:** [$prev_date](./$prev_date.md) | **Next Ledger:** [$next_date](./$next_date.md)"
                else
                    echo "**Previous Ledger:** [$prev_date](./$prev_date.md) | **Next Ledger:** TBD"
                fi
            else
                if [[ -n "$next_date" ]]; then
                    echo "**Previous Ledger:** None (First Entry) | **Next Ledger:** [$next_date](./$next_date.md)"
                else
                    echo "**Previous Ledger:** None (First Entry) | **Next Ledger:** TBD"
                fi
            fi
        else
            echo "$line"
        fi
    done < "$temp_file" > "$output_file"
    
    rm "$temp_file"
    
    log_success "Daily Ledger created: $output_file"
    
    # Update all navigation links after creating new entry
    if [[ "$COMMAND" != "link-entries" ]]; then
        log_info "Updating navigation links..."
        update_navigation_links
    fi
}

# Update forward/backward navigation links in all entries
update_navigation_links() {
    local entries=($(get_existing_entries))
    
    log_info "Updating navigation links for ${#entries[@]} entries..."
    
    for i in "${!entries[@]}"; do
        local current_file="${entries[i]}"
        local current_date=$(basename "$current_file" .md)
        
        # Get previous and next
        local prev_date=""
        local next_date=""
        
        if [[ $i -gt 0 ]]; then
            prev_date=$(basename "${entries[$((i-1))]}" .md)
        fi
        
        if [[ $((i+1)) -lt ${#entries[@]} ]]; then
            next_date=$(basename "${entries[$((i+1))]}" .md)
        fi
        
        log_verbose "Updating $current_date: prev=$prev_date, next=$next_date"
        
        if [[ "$DRY_RUN" == true ]]; then
            log_info "DRY RUN: Would update navigation in $current_file"
            continue
        fi
        
        # Read current content and update navigation section
        local temp_file=$(mktemp)
        local in_nav_section=false
        local found_nav_section=false
        
        while IFS= read -r line; do
            if [[ "$line" =~ ^###\ \*\*5\.\ Navigation\*\* ]]; then
                found_nav_section=true
                in_nav_section=true
                echo "$line"
                
                # Write new navigation content
                if [[ -n "$prev_date" ]]; then
                    if [[ -n "$next_date" ]]; then
                        echo "**Previous Ledger:** [$prev_date](./$prev_date.md) | **Next Ledger:** [$next_date](./$next_date.md)"
                    else
                        echo "**Previous Ledger:** [$prev_date](./$prev_date.md) | **Next Ledger:** TBD"
                    fi
                else
                    if [[ -n "$next_date" ]]; then
                        echo "**Previous Ledger:** None (First Entry) | **Next Ledger:** [$next_date](./$next_date.md)"
                    else
                        echo "**Previous Ledger:** None (First Entry) | **Next Ledger:** TBD"
                    fi
                fi
                
                continue
            elif [[ "$in_nav_section" == true && "$line" =~ ^--- ]]; then
                in_nav_section=false
                echo "$line"
                continue
            elif [[ "$in_nav_section" == true ]]; then
                # Skip existing navigation content
                continue
            fi
            
            echo "$line"
        done < "$current_file" > "$temp_file"
        
        # If no navigation section found, add one before the final ---
        if [[ "$found_nav_section" == false ]]; then
            # Insert navigation section before final ---
            sed -i '$i\
### **5. Navigation**\
**Previous Ledger:** '"${prev_date:+[$prev_date](./$prev_date.md)}"''"${prev_date:-None (First Entry)}"' | **Next Ledger:** '"${next_date:+[$next_date](./$next_date.md)}"''"${next_date:-TBD}"'\
            local temp_file2=$(mktemp)
            local nav_section="### **5. Navigation**
**Previous Ledger:** ${prev_date:+[$prev_date](./$prev_date.md)}${prev_date:-None (First Entry)} | **Next Ledger:** ${next_date:+[$next_date](./$next_date.md)}${next_date:-TBD}"
            local inserted=false
            while IFS= read -r line; do
                if [[ "$inserted" == false && "$line" =~ ^--- ]]; then
                    echo "$nav_section" >> "$temp_file2"
                    inserted=true
                fi
                echo "$line" >> "$temp_file2"
            done < "$temp_file"
            mv "$temp_file2" "$temp_file"
        fi
        
        mv "$temp_file" "$current_file"
    done
    
    log_success "Navigation links updated for all entries"
}

# List existing entries
list_entries() {
    local entries=($(get_existing_entries))
    
    if [[ ${#entries[@]} -eq 0 ]]; then
        log_warning "No Daily Ledger entries found"
        return
    fi
    
    log_info "Daily Ledger entries (${#entries[@]} total):"
    echo ""
    
    for entry in "${entries[@]}"; do
        local date=$(basename "$entry" .md)
        local arc_name="Unknown Arc"
        
        # Try to extract arc name from file
        if [[ -f "$entry" ]]; then
            arc_name=$(grep "^\*\*Arc:\*\*" "$entry" | head -1 | sed 's/.*Arc:\*\* *\([^—-]*\)[—-]*.*/\1/' | sed 's/ *$//' || echo "Unknown Arc")
        fi
        
        echo -e "  📅 ${GREEN}$date${NC} — $arc_name"
    done
    
    echo ""
}

# Main execution
main() {
    validate_template
    
    case "$COMMAND" in
        "generate")
            generate_ledger "$TARGET_DATE" "$ARC_NAME"
            ;;
        "today")
            generate_ledger "$(date +%Y-%m-%d)" "$ARC_NAME"
            ;;
        "link-entries")
            update_navigation_links
            ;;
        "list")
            list_entries
            ;;
        "")
            echo -e "${RED}❌ No command specified${NC}"
            show_help
            exit 1
            ;;
        *)
            echo -e "${RED}❌ Unknown command: $COMMAND${NC}"
            show_help
            exit 1
            ;;
    esac
}

# Only run main if script is executed directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi