#!/usr/bin/env bash
#
# Chronicle Keeper - TLDL Writer
# Writes entries to TLDL/entries/ and updates TLDL/index.md
#
# TLDL: This script maintains the sacred Chronicle of development adventures

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
TLDL_DIR="$PROJECT_ROOT/TLDL"
ENTRIES_DIR="$TLDL_DIR/entries"
INDEX_FILE="$TLDL_DIR/index.md"

# Ensure required directories exist
mkdir -p "$ENTRIES_DIR"

show_help() {
    echo -e "${PURPLE}📜 Chronicle Keeper - TLDL Writer${NC}"
    echo ""
    echo "Writes TLDL entries and maintains the sacred Chronicle index."
    echo ""
    echo "Usage: $0 [command] [options]"
    echo ""
    echo "Commands:"
    echo "  write <file>       Write a TLDL entry from JSON file"
    echo "  update-index       Update the TLDL index with all entries"
    echo "  validate           Validate TLDL entries and index"
    echo "  stats              Show TLDL statistics"
    echo "  bootstrap          Initialize TLDL from existing docs/"
    echo ""
    echo "Options:"
    echo "  --dry-run         Show what would be done without making changes"
    echo "  --verbose         Show detailed output"
    echo "  --force           Overwrite existing entries"
    echo "  --append-timestamp Add timestamp suffix to avoid filename conflicts"
    echo "  --help, -h        Show this help"
    echo ""
    echo "Examples:"
    echo "  $0 write entry-data.json          # Write TLDL entry from JSON"
    echo "  $0 write data.json --force        # Overwrite existing entries"
    echo "  $0 write data.json --append-timestamp  # Add timestamp to avoid conflicts"
    echo "  $0 update-index                   # Update TLDL index"
    echo "  $0 bootstrap --dry-run            # Preview bootstrap operation"
    echo ""
    echo "🍑 Part of the Chronicle Keeper system - Preserving lore since TLDL began"
}

# Parse command line arguments
COMMAND=""
DRY_RUN=false
VERBOSE=false
FORCE=false
APPEND_TIMESTAMP=false
INPUT_FILE=""

while [[ $# -gt 0 ]]; do
    case $1 in
        write|update-index|validate|stats|bootstrap)
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
        --append-timestamp)
            APPEND_TIMESTAMP=true
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
            INPUT_FILE="$1"
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
    if [[ "$VERBOSE" == "true" ]]; then
        echo -e "${PURPLE}🔍 $1${NC}"
    fi
}

# ---------- Python bootstrap (cross-platform) ----------
command_exists() { command -v "$1" >/dev/null 2>&1; }

create_python3_shim_if_needed() {
  if command_exists python && ! command_exists python3; then
    log_info "python3 not found, but python is available. Offering shim creation."
    # Non-interactive: create shim automatically in this utility to ease CI usage
    mkdir -p "$PROJECT_ROOT/.bin"
    cat > "$PROJECT_ROOT/.bin/python3" <<'EOF'
#!/usr/bin/env bash
exec python "$@"
EOF
    chmod +x "$PROJECT_ROOT/.bin/python3"
    export PATH="$PROJECT_ROOT/.bin:$PATH"
    log_success "Created python3 shim in .bin and updated PATH for this session"
  fi
}

ensure_python() {
  if command_exists python3 || command_exists python; then
    create_python3_shim_if_needed
    return 0
  fi
  log_warning "Python not found on PATH. Some features will be degraded."
  return 1
}
# ---------- end Python bootstrap ----------

# Initialize PY for this session
ensure_python || true
PY=$(command -v python3 || command -v python || echo "")
export PY

# Get contextual quote from ScrollQuoteEngine
get_chronicle_quote() {
    local context="${1:-tldl}"

    if [[ -f "$PROJECT_ROOT/src/ScrollQuoteEngine/quote_engine.py" ]] && [[ -n "$PY" ]]; then
        local quote
        quote=$(cd "$PROJECT_ROOT" && "$PY" src/ScrollQuoteEngine/quote_engine.py --context "$context" --format markdown 2>/dev/null) || true

        if [[ -n "$quote" ]]; then
            echo "$quote"
        else
            echo '> *"Every chronicle entry is a scroll worthy of preservation."* — **Chronicle Keeper'\''s Codex, Vol. I**'
        fi
    else
        echo '> *"When the quote engine slumbers, wisdom flows from the heart."* — **Emergency Scrolls, Vol. Zero**'
    fi
}

# Write TLDL entry from JSON data
write_tldl_entry() {
    local json_file="$1"

    if [[ ! -f "$json_file" ]]; then
        log_error "JSON file not found: $json_file"
        return 1
    fi

    log_info "Processing TLDL entry from $json_file"

    # Parse JSON using Python (more reliable than jq)
    local entry_data
    if ! entry_data=$("$PY" -c "
import json, sys
try:
    with open('$json_file', 'r', encoding='utf-8') as f:
        data = json.load(f)
        print(json.dumps(data, indent=2))
except Exception as e:
    print(f'JSON parsing error: {e}', file=sys.stderr)
    sys.exit(1)
" 2>/dev/null); then
        log_error "Failed to parse JSON file"
        return 1
    fi

    # Extract key fields using Python safely
    local filename content
    filename=$("$PY" -c "
import json
try:
    with open('$json_file', 'r', encoding='utf-8') as f:
        data = json.load(f)
        print(data.get('filename', 'unknown.md'))
except:
    print('unknown.md')
")

    content=$("$PY" -c "
import json
try:
    with open('$json_file', 'r', encoding='utf-8') as f:
        data = json.load(f)
        print(data.get('content', '# Error: No content'))
except:
    print('# Error: No content')
")

    # Apply timestamp suffix if requested
    if [[ "$APPEND_TIMESTAMP" == "true" ]]; then
        local timestamp
        timestamp=$(date +"%H%M%S")
        local name_part="${filename%.*}"
        local ext_part="${filename##*.}"
        filename="${name_part}-${timestamp}.${ext_part}"
        log_verbose "Applied timestamp suffix: $filename"
    fi

    local output_path="$ENTRIES_DIR/$filename"

    # Check if file exists and handle overwrites
    if [[ -f "$output_path" ]] && [[ "$FORCE" != "true" ]]; then
        log_warning "Entry already exists: $filename"
        
        # Handle non-interactive environments (CI/CD)
        if [[ ! -t 0 ]] || [[ -n "$CI" ]] || [[ -n "$GITHUB_ACTIONS" ]]; then
            log_error "File exists and --force not specified in non-interactive environment"
            log_info "Use --force to overwrite existing entries in CI/CD"
            return 1
        fi
        
        read -p "Overwrite? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            log_info "Skipping entry write"
            return 0
        fi
    fi

    if [[ "$DRY_RUN" == "true" ]]; then
        log_info "[DRY RUN] Would write entry to: $output_path"
        log_verbose "Content preview: $(echo "$content" | head -n 5)"
        return 0
    fi

    # Write the entry
    echo "$content" > "$output_path"
    log_success "TLDL entry written: $filename"

    # Update index automatically
    update_tldl_index

    log_verbose "Entry details: $entry_data"
}

# Update TLDL index with all entries
update_tldl_index() {
    log_info "Updating TLDL index"

    # Count entries
    local entry_count
    entry_count=$(find "$ENTRIES_DIR" -name "*.md" -type f | wc -l)

    # Get current timestamp
    local timestamp
    timestamp=$(date -u +"%Y-%m-%d %H:%M:%S UTC")

    # Get contextual quote
    local quote
    quote=$(get_chronicle_quote "tldl")

    if [[ "$DRY_RUN" == "true" ]]; then
        log_info "[DRY RUN] Would update index with $entry_count entries"
        return 0
    fi

    # Generate new index content
    cat > "$INDEX_FILE" << EOF
# 📜 True Living Development Log (TLDL) Index

$quote

Welcome to the **Chronicle Keeper's** domain, where every development journey is preserved for posterity. This index chronicles all TLDL entries, maintained by the self-perpetuating Scribe System.

## 🧠 About the Chronicle Keeper

The Chronicle Keeper is the self-perpetuating Scribe System that powers the True Living Development Log. It parses GitHub issues, comments, and CI events to generate scroll-worthy entries automatically, preserving the lore and ensuring chronological accuracy.

## 📊 Chronicle Statistics

- **Total Entries**: $entry_count
- **Last Updated**: $timestamp
- **Chronicle Keeper Status**: 🟢 Active

## 📜 Entry Index

EOF

    # Add entry listings
    if [[ $entry_count -gt 0 ]]; then
        echo "" >> "$INDEX_FILE"
        echo "### Recent Chronicles" >> "$INDEX_FILE"
        echo "" >> "$INDEX_FILE"

        # List entries sorted by date (newest first)
        find "$ENTRIES_DIR" -name "*.md" -type f | sort -r | while read -r entry_file; do
            local entry_basename
            entry_basename=$(basename "$entry_file" .md)

            # Extract title from first line of file
            local title
            title=$(head -n 1 "$entry_file" | sed 's/^# //' 2>/dev/null || echo "$entry_basename")

            # Extract date from filename
            local date_part
            # Case 1: Filename like TLDL-YYYY-MM-DD-Title
            date_part=$(echo "$entry_basename" | grep -oE 'TLDL-[0-9]{4}-[0-9]{2}-[0-9]{2}' | sed 's/^TLDL-//' )
            # Case 2: Leading YYYY-MM-DD-Title
            if [[ -z "$date_part" ]]; then
              date_part=$(echo "$entry_basename" | grep -oE '^[0-9]{4}-[0-9]{2}-[0-9]{2}')
            fi
            date_part=${date_part:-unknown}

            echo "- [$title](entries/$entry_basename.md) - *$date_part*" >> "$INDEX_FILE"
        done
    else
        echo "*Chronicle entries will appear here as they are created by the Scribe System*" >> "$INDEX_FILE"
    fi

    # Add footer
    cat >> "$INDEX_FILE" << EOF

---

## 🎯 Quick Actions

- **Create New Entry**: Use \`lda tldl create "Entry Title"\`
- **Validate TLDL**: Run \`"\$PY" src/SymbolicLinter/validate_docs.py --tldl-path TLDL/\`
- **Generate Quote**: Use \`scripts/lda-quote --context tldl --format markdown\`

---

**🍑 Buttsafe Certified**: This chronicle maintains the highest standards of development lore preservation.

*Last Chronicle Update: $timestamp - Automated by Chronicle Keeper*
EOF

    log_success "TLDL index updated with $entry_count entries"
    echo "Index written to: $INDEX_FILE"
}

# Validate TLDL entries and index
validate_tldl() {
    log_info "Validating TLDL entries and index"

    local errors=0

    # Check if TLDL directory structure exists
    if [[ ! -d "$TLDL_DIR" ]]; then
        log_error "TLDL directory not found: $TLDL_DIR"
        ((errors++))
    fi

    if [[ ! -d "$ENTRIES_DIR" ]]; then
        log_error "TLDL entries directory not found: $ENTRIES_DIR"
        ((errors++))
    fi

    if [[ ! -f "$INDEX_FILE" ]]; then
        log_error "TLDL index file not found: $INDEX_FILE"
        ((errors++))
    fi

    # Validate entry files
    local entry_count=0
    if [[ -d "$ENTRIES_DIR" ]]; then
        while IFS= read -r -d '' entry_file; do
            ((entry_count++))
            log_verbose "Validating: $(basename "$entry_file")"

            # Check if file has content
            if [[ ! -s "$entry_file" ]]; then
                log_warning "Empty entry file: $(basename "$entry_file")"
                ((errors++))
            fi

            # Check filename format (should be YYYY-MM-DD-*.md)
            local basename_entry
            basename_entry=$(basename "$entry_file" .md)
            if [[ ! "$basename_entry" =~ ^[0-9]{4}-[0-9]{2}-[0-9]{2}- ]]; then
                log_warning "Entry filename doesn't follow convention: $(basename "$entry_file")"
            fi

        done < <(find "$ENTRIES_DIR" -name "*.md" -type f -print0 2>/dev/null)
    fi

    # Use existing validation if available
    if [[ -f "$PROJECT_ROOT/src/SymbolicLinter/validate_docs.py" ]] && [[ -n "$PY" ]]; then
        log_verbose "Running SymbolicLinter validation"
        if cd "$PROJECT_ROOT" && "$PY" src/SymbolicLinter/validate_docs.py --tldl-path TLDL/ 2>/dev/null; then
            log_success "SymbolicLinter validation passed"
        else
            log_warning "SymbolicLinter validation issues detected"
        fi
    fi

    if [[ $errors -eq 0 ]]; then
        log_success "TLDL validation completed - $entry_count entries validated"
    else
        log_error "TLDL validation failed with $errors errors"
        return 1
    fi
}

# Show TLDL statistics
show_tldl_stats() {
    set +e  # Temporarily disable exit on error for stats function
    log_info "TLDL Chronicle Statistics"
    echo ""

    # Count entries
    local entry_count=0
    local total_size=0

    if [[ -d "$ENTRIES_DIR" ]]; then
        while IFS= read -r -d '' entry_file; do
            ((entry_count++))
            size=$(wc -c < "$entry_file" 2>/dev/null || echo 0)
            total_size=$((total_size + size))
        done < <(find "$ENTRIES_DIR" -name "*.md" -type f -print0 2>/dev/null)
    fi

    # Calculate average size
    local avg_size=0
    if [[ $entry_count -gt 0 ]]; then
        avg_size=$((total_size / entry_count))
    fi

    # Get date range (extract dates from TLDL-YYYY-MM-DD-Title format)
    local oldest newest
    if [[ $entry_count -gt 0 ]]; then
        oldest=$(find "$ENTRIES_DIR" -name "*.md" -type f -exec basename {} .md \; 2>/dev/null | grep -oE 'TLDL-[0-9]{4}-[0-9]{2}-[0-9]{2}' | sed 's/TLDL-//' | sort | head -n 1 || echo "")
        newest=$(find "$ENTRIES_DIR" -name "*.md" -type f -exec basename {} .md \; 2>/dev/null | grep -oE 'TLDL-[0-9]{4}-[0-9]{2}-[0-9]{2}' | sed 's/TLDL-//' | sort | tail -n 1 || echo "")
    fi

    echo "📊 Chronicle Keeper Statistics:"
    echo "  Total Entries: $entry_count"
    if [[ $total_size -gt 0 ]]; then
        echo "  Total Size: $(( total_size / 1024 )) KB"
    else
        echo "  Total Size: 0 KB"
    fi
    if [[ $avg_size -gt 0 ]]; then
        echo "  Average Entry Size: $(( avg_size / 1024 )) KB"
    else
        echo "  Average Entry Size: 0 KB"
    fi
    echo "  Date Range: ${oldest:-N/A} to ${newest:-N/A}"
    echo ""

    # Index status
    if [[ -f "$INDEX_FILE" ]]; then
        local index_size
        index_size=$(wc -c < "$INDEX_FILE" 2>/dev/null || echo 0)
        local index_updated
        index_updated=$(stat -c %y "$INDEX_FILE" 2>/dev/null || date -r "$INDEX_FILE" 2>/dev/null || echo "unknown")
        echo "📜 Index Status:"
        if [[ $index_size -gt 0 ]]; then
            echo "  Index Size: $(( index_size / 1024 )) KB"
        else
            echo "  Index Size: 0 KB"
        fi
        echo "  Last Updated: $index_updated"
    else
        echo "📜 Index Status: Missing"
    fi

    echo ""
    echo "🧠 Chronicle Keeper Status: ${entry_count:-0} scrolls preserved"
    set -e  # Re-enable exit on error
}

# Bootstrap TLDL from existing docs/
bootstrap_tldl() {
    log_info "Bootstrapping TLDL from existing documentation"

    local docs_dir="$PROJECT_ROOT/docs"
    local bootstrap_count=0

    if [[ ! -d "$docs_dir" ]]; then
        log_error "Docs directory not found: $docs_dir"
        return 1
    fi

    # Find existing TLDL files in docs/
    while IFS= read -r -d '' doc_file; do
        local basename_doc
        basename_doc=$(basename "$doc_file")

        # Skip if it's just the template
        if [[ "$basename_doc" == "tldl_template.yaml" ]] || [[ "$basename_doc" =~ TestEntry ]]; then
            log_verbose "Skipping template/test file: $basename_doc"
            continue
        fi

        log_verbose "Found potential TLDL entry: $basename_doc"

        # Check if it's actually a real entry (not just template copy)
        if grep -q "Your Name or @copilot" "$doc_file" 2>/dev/null; then
            log_verbose "Skipping template copy: $basename_doc"
            continue
        fi

        local target_file="$ENTRIES_DIR/$basename_doc"

        if [[ -f "$target_file" ]] && [[ "$FORCE" != "true" ]]; then
            log_verbose "Entry already exists in TLDL: $basename_doc"
            continue
        fi

        if [[ "$DRY_RUN" == "true" ]]; then
            log_info "[DRY RUN] Would bootstrap: $basename_doc"
            ((bootstrap_count++))
            continue
        fi

        # Copy the file to TLDL entries
        cp "$doc_file" "$target_file"
        log_success "Bootstrapped entry: $basename_doc"
        ((bootstrap_count++))

    done < <(find "$docs_dir" -name "TLDL-*.md" -type f -print0 2>/dev/null)

    if [[ $bootstrap_count -gt 0 ]]; then
        log_success "Bootstrap completed: $bootstrap_count entries processed"

        if [[ "$DRY_RUN" != "true" ]]; then
            update_tldl_index
        fi
    else
        log_info "No new entries found to bootstrap"
    fi
}

# Main command execution
case "$COMMAND" in
    write)
        if [[ -z "$INPUT_FILE" ]]; then
            log_error "JSON input file required for write command"
            show_help
            exit 1
        fi
        write_tldl_entry "$INPUT_FILE"
        ;;
    update-index)
        update_tldl_index
        ;;
    validate)
        validate_tldl
        ;;
    stats)
        show_tldl_stats
        ;;
    bootstrap)
        bootstrap_tldl
        ;;
    "")
        log_error "Command required"
        show_help
        exit 1
        ;;
    *)
        log_error "Unknown command: $COMMAND"
        show_help
        exit 1
        ;;
esac
