#!/usr/bin/env bash
#
# Living Dev Agent Context Initialization Script
# Initializes agent context, validates setup, and prepares development environment
#
# Copyright (C) 2025 Bellok
#
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program.  If not, see <https://www.gnu.org/licenses/>.
#
set -e

# Make failures visible instead of silently exiting
trap 'echo -e "${RED}❌ Init aborted at line $LINENO${NC}"' ERR

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Configuration
DRY_RUN=false
CREATE_TLDL=""
VERBOSE=false
# Non-interactive/assume-yes mode for Windows/Rider flows
ASSUME_YES=false
if [[ "${LDA_ASSUME_YES:-}" == "1" || "${LDA_ASSUME_YES:-}" == "true" || "${LDA_NONINTERACTIVE:-}" == "1" || "${LDA_NONINTERACTIVE:-}" == "true" ]]; then
  ASSUME_YES=true
fi

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Functions
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

# ---------- Python bootstrap (cross-platform) ----------
command_exists() { command -v "$1" >/dev/null 2>&1; }

create_python3_shim_if_needed() {
  if command_exists python && ! command_exists python3; then
    if [[ "$ASSUME_YES" == true ]]; then
      mkdir -p "$PROJECT_ROOT/.bin"
      cat > "$PROJECT_ROOT/.bin/python3" <<'EOF'
#!/usr/bin/env bash
exec python "$@"
EOF
      chmod +x "$PROJECT_ROOT/.bin/python3"
      export PATH="$PROJECT_ROOT/.bin:$PATH"
      log_success "Created python3 shim in .bin and updated PATH for this session (non-interactive)"
      return 0
    fi
    echo -n "Create a 'python3' shim that calls 'python'? [Y/n] "
    read -r ans
    if [[ -z "$ans" || "$ans" =~ ^[Yy]$ ]]; then
      mkdir -p "$PROJECT_ROOT/.bin"
      cat > "$PROJECT_ROOT/.bin/python3" <<'EOF'
#!/usr/bin/env bash
exec python "$@"
EOF
      chmod +x "$PROJECT_ROOT/.bin/python3"
      export PATH="$PROJECT_ROOT/.bin:$PATH"
      log_success "Created python3 shim in .bin and updated PATH for this session"
    else
      log_info "Skipping shim creation"
    fi
  fi
}

install_python_windows() {
  if command_exists winget; then
    echo -n "Use winget to install Python 3? [y/N] "
    read -r ans
    if [[ "$ans" =~ ^[Yy]$ ]]; then
      log_info "Invoking winget (may require confirmations/admin)"
      winget install -e --id Python.Python.3.13 --accept-package-agreements --accept-source-agreements || return 1
      return 0
    fi
  fi
  echo -n "Download and run the official Python 3 installer silently? [y/N] "
  read -r ans
  if [[ "$ans" =~ ^[Yy]$ ]]; then
    local ps=$(
      ErrorActionPreference="Stop";
      ver="3.13.0";
      uri="https://www.python.org/ftp/python/$ver/python-$ver-amd64.exe";
      tmp=Join-Path env:TEMP "python-inst.exe";
      Invoke-WebRequest -Uri $uri -OutFile tmp;
      Start-Process -FilePath tmp -ArgumentList "/quiet InstallAllUsers=1 PrependPath=1 Include_launcher=1 Include_pip=1 Shortcuts=0" -Wait;
      Remove-Item tmp -Force
    )
    powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$ps" || return 1
    return 0
  fi
  return 1
}

install_python_macos() {
  if command_exists brew; then
    echo -n "Install Python 3 via Homebrew? [y/N] "
    read -r ans
    if [[ "$ans" =~ ^[Yy]$ ]]; then
      brew install python || return 1
      return 0
    fi
  fi
  echo -n "Open python.org download page in your browser? [y/N] "
  read -r ans
  if [[ "$ans" =~ ^[Yy]$ ]]; then
    open "https://www.python.org/downloads/" || true
  fi
  return 1
}

install_python_linux() {
  if command_exists apt; then
    echo -n "Install Python 3 via apt (may prompt for sudo)? [y/N] "
    read -r ans
    if [[ "$ans" =~ ^[Yy]$ ]]; then
      sudo apt update && sudo apt install -y python3 python3-pip || return 1
      return 0
    fi
  elif command_exists dnf; then
    echo -n "Install Python 3 via dnf? [y/N] "
    read -r ans
    if [[ "$ans" =~ ^[Yy]$ ]]; then
      sudo dnf install -y python3 python3-pip || return 1
      return 0
    fi
  elif command_exists pacman; then
    echo -n "Install Python 3 via pacman? [y/N] "
    read -r ans
    if [[ "$ans" =~ ^[Yy]$ ]]; then
      sudo pacman -Sy --noconfirm python python-pip || return 1
      return 0
    fi
  fi
  echo -n "Open python.org download page in your browser? [y/N] "
  read -r ans
  if [[ "$ans" =~ ^[Yy]$ ]]; then
    xdg-open "https://www.python.org/downloads/" || true
  fi
  return 1
}

ensure_python() {
  if command_exists python3 || command_exists python; then
    create_python3_shim_if_needed
    return 0
  fi
  log_warning "Python not found on PATH."
  echo -n "Get latest Python now? [y/N] "
  read -r go
  if [[ ! "$go" =~ ^[Yy]$ ]]; then
    log_info "Skipping Python install (some validation steps will be disabled)"
    return 1
  fi
  local uname_s
  uname_s="$(uname -s 2>/dev/null || echo unknown)"
  case "$uname_s" in
    MINGW*|MSYS*|CYGWIN*) log_info "Windows detected"; install_python_windows || return 1 ;;
    Darwin*) log_info "macOS detected"; install_python_macos || return 1 ;;
    Linux*) log_info "Linux detected"; install_python_linux || return 1 ;;
    *) log_error "Unrecognized platform ($uname_s). Please install Python manually."; return 1 ;;
  esac
  hash -r
  if command_exists python3 || command_exists python; then
    log_success "Python detected after install."
    create_python3_shim_if_needed
    if ! command_exists pip && command_exists python3; then
      python3 -m ensurepip --upgrade >/dev/null 2>&1 || true
    elif ! command_exists pip && command_exists python; then
      python -m ensurepip --upgrade >/dev/null 2>&1 || true
    fi
  else
    log_error "Python still not found after attempted install."
    return 1
  fi
}
# ---------- end Python bootstrap ----------

show_scroll_quote() {
    local context="$1"
    if [[ -f "$PROJECT_ROOT/src/ScrollQuoteEngine/quote_engine.py" ]] && [[ -n "$PY" ]] && command -v "$PY" &> /dev/null; then
        local quote
        if [[ -n "$context" ]]; then
            quote=$("$PY" "$PROJECT_ROOT/src/ScrollQuoteEngine/quote_engine.py" --context "$context" --format cli 2>/dev/null)
        else
            quote=$("$PY" "$PROJECT_ROOT/src/ScrollQuoteEngine/quote_engine.py" --format cli 2>/dev/null)
        fi

        if [[ -n "$quote" ]]; then
            echo -e "${BLUE}📜 From the Secret Art of the Living Dev:${NC}"
            echo "$quote"
            echo ""
        fi
    fi
}

show_help() {
    cat << EOF
Living Dev Agent Context Initialization

USAGE:
    $0 [OPTIONS]

OPTIONS:
    --dry-run              Show what would be done without making changes
    --create-tldl TITLE    Create a new TLDL entry with the given title
    --verbose              Enable verbose output
    --help                 Show this help message

EXAMPLES:
    $0                                    # Standard initialization
    $0 --dry-run                         # Preview initialization steps
    $0 --create-tldl "ProjectSetup"      # Create TLDL entry for project setup
    $0 --verbose --create-tldl "BugFix"  # Verbose mode with TLDL creation

DESCRIPTION:
    This script initializes the Living Dev Agent context by:
    - Validating the template structure
    - Setting up DevTimeTravel configuration
    - Initializing agent context files
    - Running basic validation checks
    - Creating TLDL entries if requested

EOF
}

validate_template_structure() {
    log_info "Validating template structure..."

    local required_dirs=("docs" "src" "scripts" ".github")
    local required_files=("docs/Copilot-Setup.md" "docs/devtimetravel_snapshot.yaml" "docs/tldl_template.yaml")

    local missing_items=()

    # Check directories
    for dir in "${required_dirs[@]}"; do
        if [[ ! -d "$PROJECT_ROOT/$dir" ]]; then
            missing_items+=("Directory: $dir")
        elif [[ "$VERBOSE" == true ]]; then
            log_success "Found directory: $dir"
        fi
    done

    # Check files
    for file in "${required_files[@]}"; do
        if [[ ! -f "$PROJECT_ROOT/$file" ]]; then
            missing_items+=("File: $file")
        elif [[ "$VERBOSE" == true ]]; then
            log_success "Found file: $file"
        fi
    done

    if [[ ${#missing_items[@]} -gt 0 ]]; then
        log_error "Template structure validation failed. Missing items:"
        for item in "${missing_items[@]}"; do
            echo "  - $item"
        done
        return 1
    else
        log_success "Template structure validation passed"
        return 0
    fi
}

initialize_devtimetravel() {
    log_info "Initializing DevTimeTravel context..."

    local config_file="$PROJECT_ROOT/docs/devtimetravel_snapshot.yaml"
    local snapshot_dir="$PROJECT_ROOT/.devtimetravel"

    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would create DevTimeTravel snapshot directory"
        log_info "DRY RUN: Would validate DevTimeTravel configuration"
        return 0
    fi

    # Create snapshot directory
    if [[ ! -d "$snapshot_dir" ]]; then
        mkdir -p "$snapshot_dir/snapshots"
        echo "# DevTimeTravel snapshots directory" > "$snapshot_dir/README.md"
        log_success "Created DevTimeTravel snapshot directory"
    fi

    # Validate configuration
    if [[ -f "$config_file" ]]; then
        if [[ -n "$PY" ]] && command -v "$PY" &> /dev/null; then
            if "$PY" - <<PYCODE 2>/dev/null
import yaml, sys
import io
p = r'''$config_file'''
try:
    with open(p, 'r', encoding='utf-8') as f:
        yaml.safe_load(f)
    sys.exit(0)
except Exception as e:
    sys.exit(1)
PYCODE
            then
                log_success "DevTimeTravel configuration is valid"
            else
                log_warning "DevTimeTravel configuration may have YAML syntax issues"
            fi
        else
            log_warning "Python not available, skipping YAML validation"
        fi
    else
        log_error "DevTimeTravel configuration file not found"
        return 1
    fi
}

create_tldl_entry() {
    local title="$1"
    local date
    date=$(date +%Y-%m-%d)
    local filename="TLDL-$date-$title.md"
    local filepath="$PROJECT_ROOT/docs/$filename"

    log_info "Creating TLDL entry: $filename"

    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would create TLDL entry at $filepath"
        return 0
    fi

    if [[ -f "$filepath" ]]; then
        log_warning "TLDL entry already exists: $filename"
        return 1
    fi

    # Copy template and customize
    if [[ -f "$PROJECT_ROOT/docs/tldl_template.yaml" ]]; then
        cp "$PROJECT_ROOT/docs/tldl_template.yaml" "$filepath"

        # Replace template placeholders
        # Portable sed -i handling (BSD/macOS vs GNU/Linux)
        if sed --version >/dev/null 2>&1; then
            # GNU sed
            sed -i "s/YYYY-MM-DD/$date/g" "$filepath"
            sed -i "s/DescriptiveTitle/$title/g" "$filepath"
        else
            # BSD/macOS sed
            sed -i.bak "s/YYYY-MM-DD/$date/g" "$filepath"
            sed -i.bak "s/DescriptiveTitle/$title/g" "$filepath"
            rm "$filepath.bak" 2>/dev/null || true
        fi

        log_success "Created TLDL entry: $filename"
        log_info "Edit the file to add your content: $filepath"
    else
        log_error "TLDL template not found"
        return 1
    fi
}

run_validation_checks() {
    log_info "Running validation checks..."

    local validation_scripts=(
        "src/SymbolicLinter/validate_docs.py"
        "src/SymbolicLinter/symbolic_linter.py"
        "src/DebugOverlayValidation/debug_overlay_validator.py"
    )

    if [[ "$DRY_RUN" == true ]]; then
        log_info "DRY RUN: Would run validation scripts:"
        for script in "${validation_scripts[@]}"; do
            echo "  - $script"
        done
        return 0
    fi

    # Check if Python is available
    if [[ -z "$PY" ]] || ! command -v "$PY" &> /dev/null; then
        log_warning "Python not available, skipping validation checks"
        return 0
    fi

    local failed_checks=0

    # Run TLDL validation
    if [[ -f "$PROJECT_ROOT/src/SymbolicLinter/validate_docs.py" ]]; then
        if [[ "$VERBOSE" == true ]]; then
            log_info "Running TLDL validation..."
        fi
        if "$PY" "$PROJECT_ROOT/src/SymbolicLinter/validate_docs.py" --tldl-path "$PROJECT_ROOT/docs" > /dev/null 2>&1; then
            log_success "TLDL validation passed"
        else
            log_warning "TLDL validation found issues"
            ((failed_checks++))
        fi
    fi

    # Run symbolic linting on source
    if [[ -f "$PROJECT_ROOT/src/SymbolicLinter/symbolic_linter.py" ]]; then
        if [[ "$VERBOSE" == true ]]; then
            log_info "Running symbolic linting..."
        fi
        if "$PY" "$PROJECT_ROOT/src/SymbolicLinter/symbolic_linter.py" --path "$PROJECT_ROOT/src" > /dev/null 2>&1; then
            log_success "Symbolic linting passed"
        else
            log_warning "Symbolic linting found issues"
            ((failed_checks++))
        fi
    fi

    if [[ $failed_checks -eq 0 ]]; then
        log_success "All validation checks passed"
    else
        log_warning "$failed_checks validation check(s) found issues (non-blocking)"
    fi
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --dry-run)
            DRY_RUN=true
            shift
            ;;
        --create-tldl)
            CREATE_TLDL="$2"
            shift 2
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

# Main execution
main() {
    echo "🤖 Living Dev Agent Context Initialization"
    echo "=========================================="

    # Preflight: ensure Python and set PY
    ensure_python || true
    PY=$(command -v python3 || command -v python || echo "")
    export PY

    # Trace startup context for visibility
    echo -e "${BLUE}Args:${NC} $*"
    echo -e "${BLUE}Project root:${NC} $PROJECT_ROOT"

    # Show opening scroll quote
    show_scroll_quote "workflow"

    if [[ "$DRY_RUN" == true ]]; then
        log_info "Running in DRY RUN mode - no changes will be made"
        echo ""
    fi

    # Step 1: Validate template structure
    log_info "Starting template validation..."
    if ! validate_template_structure; then
        log_error "Template structure validation failed. Aborting."
        exit 1
    fi

    # Step 2: Initialize DevTimeTravel
    log_info "Initializing DevTimeTravel..."
    if ! initialize_devtimetravel; then
        log_error "DevTimeTravel initialization failed. Aborting."
        exit 1
    fi

    # Step 3: Create TLDL entry if requested
    if [[ -n "$CREATE_TLDL" ]]; then
        log_info "TLDL creation requested: $CREATE_TLDL"
        if ! create_tldl_entry "$CREATE_TLDL"; then
            log_error "TLDL entry creation failed."
            exit 1
        fi
    fi

    # Step 4: Run validation checks
    run_validation_checks

    echo ""
    log_success "Living Dev Agent context initialization complete!"

    # Show completion quote
    show_scroll_quote "general"

    if [[ "$DRY_RUN" == false ]]; then
        echo ""
        echo "Next steps:"
        echo "1. Review and customize docs/devtimetravel_snapshot.yaml"
        echo "2. Set up GitHub Copilot in your IDE"
        echo "3. Configure TWG-Copilot-Agent.yaml and mcp-config.json"
        echo "4. Create your first TLDL entry:"
        echo "   $0 --create-tldl \"ProjectInitialization\""
        echo ""
        echo "📚 See docs/Copilot-Setup.md for detailed setup instructions"
    fi
}

# Run main function
main "$@"
