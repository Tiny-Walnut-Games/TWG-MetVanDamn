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
# 🍑 initMyButt.sh - The Sacred Butt Initialization Ritual
# Part of the Living Dev Agent Workflow - Save The Butts! Edition
#
# This script initializes your development environment with proper
# butt-saving protocols, ergonomic awareness, and documentation habits.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Source shared utilities
source "$SCRIPT_DIR/lib/lda_common.sh"

# Configuration
COMFORT_MODE=false
VERBOSE=false
CHARACTER_CLASS=""
SKIP_ERGONOMIC_CHECK=false
ERGONOMICS_ONLY=false
CHARACTER_ONLY=false
COMFORT_ONLY=false
NON_INTERACTIVE=false
QUIET=false
JSON_OUTPUT=""

# JSON report data
declare -A BUTT_REPORT_DATA

# Additional emojis specific to this script
BUTT_EMOJI="${LDA_EMOJI_BUTT}"
SHIELD_EMOJI="${LDA_EMOJI_SHIELD}"
MAGIC_EMOJI="${LDA_EMOJI_SPARKLE}"
SCROLL_EMOJI="${LDA_EMOJI_SCROLL}"
CROWN_EMOJI="${LDA_EMOJI_CROWN}"

# Legacy color aliases for compatibility
SACRED_BLUE="$LDA_BLUE"
BUTT_GOLD="$LDA_GOLD"
COMFORT_GREEN="$LDA_GREEN"
DANGER_RED="$LDA_RED"
WISDOM_PURPLE="$LDA_PURPLE"
NC="$LDA_NC"

# Legacy logging function aliases for backward compatibility
log_comfort() {
    lda_log_comfort "$1"
}

log_wisdom() {
    lda_log_wisdom "$1"
}

log_sacred() {
    lda_log_sacred "$1"
}

log_danger() {
    lda_log_danger "$1"
}

show_help() {
    cat << EOF
$(lda_cecho "$LDA_GOLD" "${BUTT_EMOJI} Sacred Butt Initialization Ritual ${BUTT_EMOJI}")

Initialize your development environment with proper butt-saving protocols,
ergonomic awareness, and documentation habits.

USAGE:
    $0 [OPTIONS]

OPTIONS:
    --ergonomics-only      Run only ergonomic setup checks
    --character-only       Run only character class configuration  
    --comfort-only         Run only comfort reminder setup
    --character-class NAME Set specific character class (codereaper|oracle|branchdancer)
    --non-interactive      Run without interactive prompts
    --quiet               Minimize output messages
    --no-color            Disable colored output
    --json-out FILE       Output summary report to JSON file
    -h, --help            Show this help message

EXAMPLES:
    # Full interactive setup
    $0
    
    # Ergonomics check only
    $0 --ergonomics-only
    
    # Set character class non-interactively
    $0 --character-class oracle --non-interactive
    
    # Generate JSON report
    $0 --json-out butt_report.json

The Sacred Butt Initialization Ritual ensures your development environment
is properly configured for comfortable, productive, and butt-safe coding!
EOF
}

parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            --ergonomics-only)
                ERGONOMICS_ONLY=true
                shift
                ;;
            --character-only)
                CHARACTER_ONLY=true
                shift
                ;;
            --comfort-only)
                COMFORT_ONLY=true
                shift
                ;;
            --character-class)
                if [[ -n "$2" ]]; then
                    CHARACTER_CLASS="$2"
                    shift 2
                else
                    lda_die "Error: --character-class requires a class name"
                fi
                ;;
            --non-interactive)
                NON_INTERACTIVE=true
                export LDA_NON_INTERACTIVE=true
                shift
                ;;
            --quiet)
                QUIET=true
                shift
                ;;
            --no-color)
                export NO_COLOR=1
                source "$SCRIPT_DIR/lib/lda_common.sh"  # Reload with no color
                shift
                ;;
            --json-out)
                if [[ -n "$2" ]]; then
                    JSON_OUTPUT="$2"
                    shift 2
                else
                    lda_die "Error: --json-out requires a filename"
                fi
                ;;
            -h|--help)
                show_help
                exit 0
                ;;
            # Legacy options for backward compatibility
            --comfort)
                COMFORT_MODE=true
                shift
                ;;
            --verbose)
                VERBOSE=true
                shift
                ;;
            *)
                lda_die "Unknown option: $1. Use --help for usage information."
                ;;
        esac
    done
}

validate_arguments() {
    # Validate character class if provided
    if [[ -n "$CHARACTER_CLASS" && ! "$CHARACTER_CLASS" =~ ^(codereaper|oracle|branchdancer)$ ]]; then
        lda_die "Invalid character class: $CHARACTER_CLASS. Must be one of: codereaper, oracle, branchdancer"
    fi
    
    # Count exclusive modes
    local mode_count=0
    [[ "$ERGONOMICS_ONLY" == "true" ]] && ((mode_count++))
    [[ "$CHARACTER_ONLY" == "true" ]] && ((mode_count++))
    [[ "$COMFORT_ONLY" == "true" ]] && ((mode_count++))
    
    if [[ $mode_count -gt 1 ]]; then
        lda_die "Cannot specify multiple exclusive modes (--ergonomics-only, --character-only, --comfort-only)"
    fi
}

initialize_report() {
    BUTT_REPORT_DATA[timestamp]="$(lda_timestamp_iso)"
    BUTT_REPORT_DATA[mode]="full"
    BUTT_REPORT_DATA[ergonomics_only]="$ERGONOMICS_ONLY"
    BUTT_REPORT_DATA[character_only]="$CHARACTER_ONLY"
    BUTT_REPORT_DATA[comfort_only]="$COMFORT_ONLY"
    BUTT_REPORT_DATA[character_class]="$CHARACTER_CLASS"
    BUTT_REPORT_DATA[non_interactive]="$NON_INTERACTIVE"
    BUTT_REPORT_DATA[status]="in_progress"
    BUTT_REPORT_DATA[ergonomics_completed]="false"
    BUTT_REPORT_DATA[character_completed]="false"
    BUTT_REPORT_DATA[comfort_completed]="false"
    BUTT_REPORT_DATA[errors]=""
    BUTT_REPORT_DATA[warnings]=""
}

write_json_report() {
    if [[ -z "$JSON_OUTPUT" ]]; then
        return 0
    fi
    
    [[ "$QUIET" != "true" ]] && lda_log_info "Writing butt initialization report to $JSON_OUTPUT"
    
    # Create JSON report
    cat > "$JSON_OUTPUT" << EOF
{
  "timestamp": "$(lda_json_escape "${BUTT_REPORT_DATA[timestamp]}")",
  "mode": "$(lda_json_escape "${BUTT_REPORT_DATA[mode]}")",
  "ergonomics_only": $(if [[ "$ERGONOMICS_ONLY" == "true" ]]; then echo "true"; else echo "false"; fi),
  "character_only": $(if [[ "$CHARACTER_ONLY" == "true" ]]; then echo "true"; else echo "false"; fi),
  "comfort_only": $(if [[ "$COMFORT_ONLY" == "true" ]]; then echo "true"; else echo "false"; fi),
  "character_class": "$(lda_json_escape "${BUTT_REPORT_DATA[character_class]}")",
  "non_interactive": $(if [[ "$NON_INTERACTIVE" == "true" ]]; then echo "true"; else echo "false"; fi),
  "status": "$(lda_json_escape "${BUTT_REPORT_DATA[status]}")",
  "ergonomics_completed": $(if [[ "${BUTT_REPORT_DATA[ergonomics_completed]}" == "true" ]]; then echo "true"; else echo "false"; fi),
  "character_completed": $(if [[ "${BUTT_REPORT_DATA[character_completed]}" == "true" ]]; then echo "true"; else echo "false"; fi),
  "comfort_completed": $(if [[ "${BUTT_REPORT_DATA[comfort_completed]}" == "true" ]]; then echo "true"; else echo "false"; fi),
  "errors": "$(lda_json_escape "${BUTT_REPORT_DATA[errors]}")",
  "warnings": "$(lda_json_escape "${BUTT_REPORT_DATA[warnings]}")"
}
EOF
    
    [[ "$QUIET" != "true" ]] && lda_log_success "Report written to $JSON_OUTPUT"
}

# ---------- Python bootstrap (cross-platform) ----------
command_exists() { command -v "$1" >/dev/null 2>&1; }

create_python3_shim_if_needed() {
  if command_exists python && ! command_exists python3; then
    if [[ "$NON_INTERACTIVE" == "true" ]]; then
      [[ "$QUIET" != "true" ]] && log_wisdom "Non-interactive mode: creating python3 shim automatically"
      mkdir -p "$PROJECT_ROOT/.bin"
      cat > "$PROJECT_ROOT/.bin/python3" <<'EOF'
#!/usr/bin/env bash
exec python "$@"
EOF
      chmod +x "$PROJECT_ROOT/.bin/python3"
      export PATH="$PROJECT_ROOT/.bin:$PATH"
      [[ "$QUIET" != "true" ]] && log_comfort "Created python3 shim in .bin and updated PATH for this session"
    else
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
        log_comfort "Created python3 shim in .bin and updated PATH for this session"
      else
        log_wisdom "Skipping shim creation"
      fi
    fi
  fi
}

install_python_windows() {
  if command_exists winget; then
    echo -n "Use winget to install Python 3? [y/N] "
    read -r ans
    if [[ "$ans" =~ ^[Yy]$ ]]; then
      log_wisdom "Invoking winget (may require confirmations/admin)"
      winget install -e --id Python.Python.3.13 --accept-package-agreements --accept-source-agreements || return 1
      return 0
    fi
  fi
  echo -n "Download and run the official Python 3 installer silently? [y/N] "
  read -r ans
  if [[ "$ans" =~ ^[Yy]$ ]]; then
    local ps='
      $ErrorActionPreference="Stop";
      $ver = "3.13.0";
      $uri = "https://www.python.org/ftp/python/$ver/python-$ver-amd64.exe";
      $tmp = Join-Path $env:TEMP "python-inst.exe";
      Invoke-WebRequest -Uri $uri -OutFile $tmp;
      Start-Process -FilePath $tmp -ArgumentList "/quiet InstallAllUsers=1 PrependPath=1 Include_launcher=1 Include_pip=1 Shortcuts=0" -Wait;
      Remove-Item $tmp -Force
    '
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
  
  if [[ "$NON_INTERACTIVE" == "true" ]]; then
    [[ "$QUIET" != "true" ]] && log_warning "Python not found on PATH - skipping installation in non-interactive mode"
    [[ "$QUIET" != "true" ]] && log_wisdom "Some validation steps will be disabled"
    return 1
  fi
  
  log_danger "Python not found on PATH."
  echo -n "Get latest Python now? [y/N] "
  read -r go
  if [[ ! "$go" =~ ^[Yy]$ ]]; then
    log_wisdom "Skipping Python install (some validation steps will be disabled)"
    return 1
  fi
  local uname_s
  uname_s="$(uname -s 2>/dev/null || echo unknown)"
  case "$uname_s" in
    MINGW*|MSYS*|CYGWIN*) log_wisdom "Windows detected"; install_python_windows || return 1 ;;
    Darwin*) log_wisdom "macOS detected"; install_python_macos || return 1 ;;
    Linux*) log_wisdom "Linux detected"; install_python_linux || return 1 ;;
    *) log_danger "Unrecognized platform ($uname_s). Please install Python manually."; return 1 ;;
  esac
  hash -r
  if command_exists python3 || command_exists python; then
    log_comfort "Python detected after install."
    create_python3_shim_if_needed
    if ! command_exists pip && command_exists python3; then
      python3 -m ensurepip --upgrade >/dev/null 2>&1 || true
    elif ! command_exists pip && command_exists python; then
      python -m ensurepip --upgrade >/dev/null 2>&1 || true
    fi
  else
    log_danger "Python still not found after attempted install."
    return 1
  fi
}
# ---------- end Python bootstrap ----------

show_sacred_banner() {
    echo ""
    echo -e "${BUTT_GOLD}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${NC}"
    echo -e "${BUTT_GOLD}${MAGIC_EMOJI}                                  ${MAGIC_EMOJI}${NC}"
    echo -e "${BUTT_GOLD}${MAGIC_EMOJI}      ${BUTT_EMOJI} SAVE THE BUTTS! ${BUTT_EMOJI}       ${MAGIC_EMOJI}${NC}"
    echo -e "${BUTT_GOLD}${MAGIC_EMOJI}   Sacred Initialization Ritual   ${MAGIC_EMOJI}${NC}"
    echo -e "${BUTT_GOLD}${MAGIC_EMOJI}                                  ${MAGIC_EMOJI}${NC}"
    echo -e "${BUTT_GOLD}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${MAGIC_EMOJI}${NC}"
    echo ""
    echo -e "${WISDOM_PURPLE}\"In the beginning was the Code, and the Code was with Butt,${NC}"
    echo -e "${WISDOM_PURPLE} and the Code was Butt. And lo, the Developer did sit,${NC}"
    echo -e "${WISDOM_PURPLE} and their sitting was good.\"${NC}"
    echo -e "${WISDOM_PURPLE}                    - Sacred Scrolls of Cheekdom${NC}"
    echo ""
}

log_comfort() {
    echo -e "${COMFORT_GREEN}${SHIELD_EMOJI} $1${NC}"
}

log_wisdom() {
    echo -e "${WISDOM_PURPLE}${SCROLL_EMOJI} $1${NC}"
}

log_sacred() {
    echo -e "${BUTT_GOLD}${CROWN_EMOJI} $1${NC}"
}

log_danger() {
    echo -e "${DANGER_RED}⚠️  $1${NC}"
}

perform_ergonomic_assessment() {
    if [[ "$SKIP_ERGONOMIC_CHECK" == true ]]; then
        [[ "$QUIET" != "true" ]] && log_wisdom "Skipping ergonomic assessment (your posterior's risk)"
        return 0
    fi

    if [[ "$QUIET" != "true" ]]; then
        echo ""
        log_sacred "Performing Sacred Ergonomic Assessment..."
        echo ""
    fi

    # Handle non-interactive mode
    if [[ "$NON_INTERACTIVE" == "true" ]]; then
        [[ "$QUIET" != "true" ]] && log_wisdom "Non-interactive mode: assuming optimal ergonomic setup"
        [[ "$QUIET" != "true" ]] && log_comfort "Chair comfort assumed acceptable. Your posterior approves."
        [[ "$QUIET" != "true" ]] && log_comfort "Posture check assumed passed. Spine alignment maintained."
        [[ "$QUIET" != "true" ]] && log_comfort "Break habits assumed approved. Circulation maintained."
        [[ "$QUIET" != "true" ]] && log_sacred "Ergonomic Assessment Complete!"
        return 0
    fi

    echo -e "${SACRED_BLUE}Please answer these sacred questions about your setup:${NC}"
    echo ""

    # Chair assessment
    echo -e "${BUTT_GOLD}1. Rate your current chair comfort (1-10):${NC}"
    read -p "   Comfort level: " chair_comfort

    if [[ $chair_comfort -lt 6 ]]; then
        log_danger "Chair comfort dangerously low! Your butt is at risk!"
        echo "   Consider upgrading to a Buttsafe Certified™ ergonomic chair"
    else
        log_comfort "Chair comfort acceptable. Your posterior approves."
    fi

    # Desk height assessment
    echo ""
    echo -e "${BUTT_GOLD}2. Can you maintain proper posture at your desk? (y/n):${NC}"
    read -p "   Proper posture: " posture_check

    if [[ "$posture_check" != "y" && "$posture_check" != "Y" ]]; then
        log_danger "Posture issues detected! Spinal alignment compromised!"
        echo "   Adjust desk height or consider a standing desk converter"
    else
        log_comfort "Posture check passed. Spine alignment maintained."
    fi

    # Break habits assessment
    echo ""
    echo -e "${BUTT_GOLD}3. Do you take breaks every 30-60 minutes? (y/n):${NC}"
    read -p "   Regular breaks: " break_habits

    if [[ "$break_habits" != "y" && "$break_habits" != "Y" ]]; then
        log_danger "Insufficient break frequency! Blood flow compromised!"
        echo "   Set up break reminders to preserve circulation and sanity"
    else
        log_comfort "Break habits approved. Circulation maintained."
    fi

    echo ""
    log_sacred "Ergonomic Assessment Complete!"
}

choose_character_class() {
    if [[ -n "$CHARACTER_CLASS" ]]; then
        [[ "$QUIET" != "true" ]] && log_wisdom "Character class pre-selected: $CHARACTER_CLASS"
        configure_character_class "$CHARACTER_CLASS"
        return 0
    fi

    # Handle non-interactive mode
    if [[ "$NON_INTERACTIVE" == "true" ]]; then
        [[ "$QUIET" != "true" ]] && log_wisdom "Non-interactive mode: defaulting to oracle class"
        configure_character_class "oracle"
        return 0
    fi

    echo ""
    log_sacred "Choose Your Sacred Path in the Cheekdom..."
    echo ""

    echo -e "${BUTT_GOLD}Available Classes:${NC}"
    echo ""
    echo -e "${SACRED_BLUE}1. ${SHIELD_EMOJI} Buttwarden${NC} - Guardian of posterior safety and ergonomic enlightenment"
    echo -e "${SACRED_BLUE}2. 🧙‍♂️ Lintmage${NC} - Wielder of code-cleaning magic and style standardization"
    echo -e "${SACRED_BLUE}3. ${SCROLL_EMOJI} Changelog Oracle${NC} - Prophet of documentation and version history"
    echo -e "${SACRED_BLUE}4. 💃 Branchdancer${NC} - Master of git flow and merge conflict choreography"
    echo ""

    while true; do
        read -p "Select your class (1-4): " class_choice
        case $class_choice in
            1) configure_character_class "buttwarden"; break;;
            2) configure_character_class "lintmage"; break;;
            3) configure_character_class "oracle"; break;;
            4) configure_character_class "branchdancer"; break;;
            *) echo "Invalid choice. The Cheekdom demands a valid selection.";;
        esac
    done
}

configure_character_class() {
    local class="$1"

    echo ""
    log_sacred "Configuring character class: $class"

    # Create character config file
    local config_file="$PROJECT_ROOT/.character_config"

    case "$class" in
        "buttwarden")
            log_comfort "You have chosen the path of the Buttwarden!"
            echo "class=buttwarden" > "$config_file"
            echo "primary_skill=ergonomic_mastery" >> "$config_file"
            echo "focus=comfort_optimization" >> "$config_file"
            echo "quest_type=comfort_enhancement" >> "$config_file"

            echo ""
            echo -e "${WISDOM_PURPLE}As a Buttwarden, you shall:${NC}"
            echo "• Champion ergonomic best practices"
            echo "• Protect developers from discomfort"
            echo "• Optimize workspace layouts for maximum comfort"
            echo "• Lead by example in self-care practices"
            ;;

        "lintmage")
            log_comfort "You have embraced the mystical arts of the Lintmage!"
            echo "class=lintmage" > "$config_file"
            echo "primary_skill=code_purification" >> "$config_file"
            echo "focus=technical_cleanliness" >> "$config_file"
            echo "quest_type=code_quality_improvement" >> "$config_file"

            echo ""
            echo -e "${WISDOM_PURPLE}As a Lintmage, you shall:${NC}"
            echo "• Cast spells of code standardization"
            echo "• Banish technical debt demons"
            echo "• Enforce the sacred formatting laws"
            echo "• Transmute chaos into readable beauty"
            ;;

        "oracle")
            log_comfort "The wisdom of the Changelog Oracle flows through you!"
            echo "class=oracle" > "$config_file"
            echo "primary_skill=documentation_prophecy" >> "$config_file"
            echo "focus=knowledge_preservation" >> "$config_file"
            echo "quest_type=documentation_enhancement" >> "$config_file"

            echo ""
            echo -e "${WISDOM_PURPLE}As a Changelog Oracle, you shall:${NC}"
            echo "• Document the present for future generations"
            echo "• Predict the consequences of code changes"
            echo "• Maintain the sacred TLDL traditions"
            echo "• Share wisdom through comprehensive guides"
            ;;

        "branchdancer")
            log_comfort "You move with the graceful flow of the Branchdancer!"
            echo "class=branchdancer" > "$config_file"
            echo "primary_skill=version_control_mastery" >> "$config_file"
            echo "focus=workflow_optimization" >> "$config_file"
            echo "quest_type=collaboration_enhancement" >> "$config_file"

            echo ""
            echo -e "${WISDOM_PURPLE}As a Branchdancer, you shall:${NC}"
            echo "• Choreograph elegant merge strategies"
            echo "• Resolve conflicts with artful grace"
            echo "• Facilitate smooth team collaboration"
            echo "• Maintain healthy repository ecosystems"
            ;;
    esac

    echo ""
    log_sacred "Character configuration complete!"
}

initialize_living_dev_log() {
    echo ""
    log_sacred "Initializing Living Dev Log workflow..."

    # Run the existing initialization script
    if [[ -x "$SCRIPT_DIR/init_agent_context.sh" ]]; then
        log_wisdom "Running agent context initialization..."
        if [[ "$VERBOSE" == true ]]; then
            "$SCRIPT_DIR/init_agent_context.sh" --verbose
        else
            "$SCRIPT_DIR/init_agent_context.sh"
        fi
        log_comfort "Living Dev Log workflow activated!"
    else
        log_danger "Agent context script not found or not executable"
        return 1
    fi

    # Create initial TLDL entry for butt initialization
    # Ensure docs directory exists
    if [[ ! -d "$PROJECT_ROOT/docs" ]]; then
        mkdir -p "$PROJECT_ROOT/docs"
    fi
    local today
    today=$(date +%Y-%m-%d)
    local init_tldl="$PROJECT_ROOT/docs/TLDL-$today-ButtInitializationRitual.md"

    if [[ ! -f "$init_tldl" ]]; then
        log_wisdom "Creating your first sacred TLDL entry..."
        # Check if --create-tldl is supported by init_agent_context.sh
        if "$SCRIPT_DIR/init_agent_context.sh" --help 2>&1 | grep -q -- '--create-tldl'; then
            "$SCRIPT_DIR/init_agent_context.sh" --create-tldl "ButtInitializationRitual"
            log_comfort "Sacred documentation ritual complete!"
        else
            log_danger "init_agent_context.sh does not support --create-tldl. Please create the TLDL entry manually."
        fi
    else
        log_wisdom "Initialization TLDL entry already exists"
    fi
}

create_comfort_reminders() {
    if [[ "$COMFORT_MODE" != true ]]; then
        return 0
    fi

    echo ""
    log_sacred "Setting up comfort reminder system..."

    # Create a simple comfort reminder script
    local reminder_script="$PROJECT_ROOT/scripts/comfort_reminder.sh"

    cat > "$reminder_script" << 'EOF'
#!/usr/bin/env bash

# Comfort Reminder - Part of the Save The Butts! Initiative

show_reminder() {
    echo ""
    echo "🍑 ===== COMFORT REMINDER ===== 🍑"
    echo ""
    echo "Time for a sacred break! Remember to:"
    echo "• Stand up and stretch for 2-3 minutes"
    echo "• Check your posture and adjust as needed"
    echo "• Take 5 deep breaths for mental clarity"
    echo "• Hydrate your body (and your soul)"
    echo "• Appreciate the progress you've made today"
    echo ""
    echo "Your butt (and your code) will thank you!"
    echo "==============================="
    echo ""
}

show_reminder
EOF

    chmod +x "$reminder_script"
    log_comfort "Comfort reminder system installed!"

    echo ""
    echo -e "${WISDOM_PURPLE}To set up automatic reminders, add this to your .bashrc or .zshrc:${NC}"
    echo -e "${SACRED_BLUE}alias comfort-break='\"$reminder_script\"'${NC}"
    echo ""
    echo -e "${WISDOM_PURPLE}Or set up a cron job for regular reminders:${NC}"
    echo -e "${SACRED_BLUE}*/30 * * * * $reminder_script${NC}"
}

validate_buttsafe_standards() {
    echo ""
    log_sacred "Validating Buttsafe Certification standards..."

    local standards_met=0
    local total_standards=6

    # Check for MANIFESTO.md
    if [[ -f "$PROJECT_ROOT/MANIFESTO.md" ]]; then
        log_comfort "✅ Sacred Manifesto present"
        ((standards_met++))
    else
        log_danger "❌ Missing sacred MANIFESTO.md"
    fi

    # Check for README.md
    if [[ -f "$PROJECT_ROOT/README.md" ]]; then
        log_comfort "✅ Repository documentation present"
        ((standards_met++))
    else
        log_danger "❌ Missing README.md documentation"
    fi

    # Check for TLDL template
    if [[ -f "$PROJECT_ROOT/docs/tldl_template.yaml" ]]; then
        log_comfort "✅ TLDL workflow template available"
        ((standards_met++))
    else
        log_danger "❌ Missing TLDL template"
    fi

    # Check for character configuration
    if [[ -f "$PROJECT_ROOT/.character_config" ]]; then
        log_comfort "✅ Character class configured"
        ((standards_met++))
    else
        log_danger "❌ Character class not selected"
    fi

    # Check for .github directory
    if [[ -d "$PROJECT_ROOT/.github" ]]; then
        log_comfort "✅ GitHub workflow configuration present"
        ((standards_met++))
    else
        log_danger "❌ Missing .github directory"
    fi

    # Check for init script
    if [[ -f "$PROJECT_ROOT/scripts/initMyButt.sh" ]]; then
        log_comfort "✅ Sacred initialization script present"
        ((standards_met++))
    else
        log_danger "❌ Sacred initialization script missing"
    fi

    echo ""
    local certification_level=$((standards_met * 100 / total_standards))

    if [[ $certification_level -ge 80 ]]; then
        log_sacred "🏆 BUTTSAFE CERTIFICATION ACHIEVED! ($certification_level%)"
        echo "   Your repository meets the sacred standards of butt safety!"
    elif [[ $certification_level -ge 60 ]]; then
        log_wisdom "🥉 Bronze Butt Status achieved ($certification_level%)"
        echo "   You're on the path to full certification!"
    else
        log_danger "⚠️  Certification incomplete ($certification_level%)"
        echo "   More work needed to achieve Buttsafe standards"
    fi
}

# Main ritual execution
main() {
    # Parse and validate arguments
    parse_arguments "$@"
    validate_arguments
    initialize_report
    
    # Preflight: ensure Python available and set $PY for called scripts
    ensure_python || true
    PY=$(command -v python3 || command -v python || echo "python3")
    export PY

    # Show banner unless in quiet mode
    if [[ "$QUIET" != "true" ]]; then
        show_sacred_banner
        echo -e "${SACRED_BLUE}Preparing to initialize your development sanctuary...${NC}"
        echo ""
    fi

    # Execute based on mode
    if [[ "$ERGONOMICS_ONLY" == "true" ]]; then
        BUTT_REPORT_DATA[mode]="ergonomics_only"
        [[ "$QUIET" != "true" ]] && lda_log_info "Running ergonomics-only mode"
        perform_ergonomic_assessment
        BUTT_REPORT_DATA[ergonomics_completed]="true"
    elif [[ "$CHARACTER_ONLY" == "true" ]]; then
        BUTT_REPORT_DATA[mode]="character_only"
        [[ "$QUIET" != "true" ]] && lda_log_info "Running character-only mode"
        choose_character_class
        BUTT_REPORT_DATA[character_completed]="true"
    elif [[ "$COMFORT_ONLY" == "true" ]]; then
        BUTT_REPORT_DATA[mode]="comfort_only"
        [[ "$QUIET" != "true" ]] && lda_log_info "Running comfort-only mode"
        create_comfort_reminders
        BUTT_REPORT_DATA[comfort_completed]="true"
    else
        # Full ritual mode
        BUTT_REPORT_DATA[mode]="full"
        
        # Step 1: Ergonomic Assessment
        perform_ergonomic_assessment
        BUTT_REPORT_DATA[ergonomics_completed]="true"

        # Step 2: Character Class Selection
        choose_character_class
        BUTT_REPORT_DATA[character_completed]="true"

        # Step 3: Living Dev Log Initialization
        initialize_living_dev_log

        # Step 4: Comfort System Setup
        create_comfort_reminders
        BUTT_REPORT_DATA[comfort_completed]="true"

        # Step 5: Buttsafe Validation
        validate_buttsafe_standards
    fi

    # Mark as successful
    BUTT_REPORT_DATA[status]="success"
    
    # Show completion message unless in quiet mode
    if [[ "$QUIET" != "true" ]]; then
        echo ""
        echo -e "${BUTT_GOLD}${MAGIC_EMOJI} SACRED RITUAL COMPLETE! ${MAGIC_EMOJI}${NC}"
        echo ""
        
        if [[ "$ERGONOMICS_ONLY" != "true" && "$CHARACTER_ONLY" != "true" && "$COMFORT_ONLY" != "true" ]]; then
            echo -e "${WISDOM_PURPLE}Your development environment has been blessed with:${NC}"
            echo "• Ergonomic awareness and comfort protocols"
            echo "• Character class configuration and quests"
            echo "• Living Dev Log workflow activation"
            echo "• Sacred documentation practices"
            echo "• Buttsafe Certification standards"
            echo ""
            echo -e "${BUTT_GOLD}May your builds always pass, your chairs always comfort,${NC}"
            echo -e "${BUTT_GOLD}and your butts always be safe.${NC}"
            echo ""
            echo -e "${SACRED_BLUE}Next steps:${NC}"
            echo "1. Review your character configuration in .character_config"
            echo "2. Create your first TLDL entry documenting today's ritual"
            echo "3. Set up comfort reminders for regular breaks"
            echo "4. Begin your first quest in the realm of Cheekdom!"
            echo ""
        fi
        
        echo -e "${COMFORT_GREEN}${BUTT_EMOJI} Welcome to the Buttsafe development lifestyle! ${BUTT_EMOJI}${NC}"
    fi
    
    # Write JSON report if requested
    write_json_report
}

# Execute the sacred ritual only if script is run directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi
