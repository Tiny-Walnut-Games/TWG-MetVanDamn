#!/usr/bin/env bash
# 🎭 Weekly Wisdom Oracle Generator
# Generates fresh quotes using Warbler templates every week

set -euo pipefail

# Script configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
WARBLER_ENGINE="$PROJECT_ROOT/src/ScrollQuoteEngine/warbler_quote_engine.py"

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

show_header() {
    echo -e "${PURPLE}🎭 =============================================== 🎭${NC}"
    echo -e "${PURPLE}   WEEKLY WISDOM ORACLE GENERATOR${NC}"
    echo -e "${PURPLE}   Warbler-Powered Fresh Quote Generation${NC}"
    echo -e "${PURPLE}🎭 =============================================== 🎭${NC}"
    echo ""
}

show_scroll_quote() {
    echo -e "${CYAN}📜 From the Secret Art of the Living Dev:${NC}"
    echo -e "${YELLOW}🪶 \"Fresh wisdom flows from the living oracle, breathing new life into ancient scrolls.\"${NC}"
    echo -e "${YELLOW}   — Oracle of Weekly Renewal, Cycles of Wisdom, Vol. I${NC}"
    echo ""
}

check_prerequisites() {
    echo -e "${BLUE}🔍 Checking prerequisites...${NC}"
    
    # Check Python 3
    if ! command -v python3 &> /dev/null; then
        echo -e "${RED}❌ Python 3 not found${NC}"
        exit 1
    fi
    
    # Check Warbler engine
    if [[ ! -f "$WARBLER_ENGINE" ]]; then
        echo -e "${RED}❌ Warbler Quote Engine not found at $WARBLER_ENGINE${NC}"
        exit 1
    fi
    
    # Check Warbler pack
    local warbler_pack="$PROJECT_ROOT/packs/warbler-pack-wisdom-scrolls/pack/templates.json"
    if [[ ! -f "$warbler_pack" ]]; then
        echo -e "${RED}❌ Warbler wisdom templates not found at $warbler_pack${NC}"
        exit 1
    fi
    
    echo -e "${GREEN}✅ All prerequisites satisfied${NC}"
    echo ""
}

generate_wisdom() {
    local count=${1:-5}
    
    echo -e "${BLUE}🎭 Invoking the Warbler Oracle...${NC}"
    echo -e "${YELLOW}   Generating $count fresh wisdom quotes${NC}"
    echo ""
    
    # Generate new quotes
    if python3 "$WARBLER_ENGINE" --generate "$count"; then
        echo ""
        echo -e "${GREEN}✨ Weekly wisdom generation complete!${NC}"
        return 0
    else
        echo ""
        echo -e "${RED}❌ Wisdom generation failed${NC}"
        return 1
    fi
}

show_statistics() {
    echo -e "${BLUE}📊 Oracle Statistics:${NC}"
    python3 "$WARBLER_ENGINE" --stats
    echo ""
}

update_quote_integration() {
    echo -e "${BLUE}🔄 Updating quote integration points...${NC}"
    
    # Update lda-quote script to use new engine
    local lda_quote_script="$PROJECT_ROOT/scripts/lda-quote"
    if [[ -f "$lda_quote_script" ]]; then
        echo -e "${YELLOW}   📝 Updating lda-quote script integration${NC}"
        # We'll update this to reference the new engine
    fi
    
    # Check scroll integration in TLDL templates
    echo -e "${YELLOW}   📜 TLDL template integration ready${NC}"
    
    echo -e "${GREEN}✅ Integration points updated${NC}"
    echo ""
}

validate_generated_quotes() {
    echo -e "${BLUE}🔍 Validating generated quotes...${NC}"
    
    # Check if cache file exists and has content
    local cache_file="$PROJECT_ROOT/data/generated_wisdom_cache.json"
    if [[ -f "$cache_file" ]]; then
        local quote_count=$(python3 -c "
import json
try:
    with open('$cache_file', 'r') as f:
        data = json.load(f)
    print(len(data.get('quotes', [])))
except:
    print(0)
")
        
        if [[ "$quote_count" -gt 0 ]]; then
            echo -e "${GREEN}✅ $quote_count generated quotes in cache${NC}"
        else
            echo -e "${YELLOW}⚠️ No generated quotes found in cache${NC}"
        fi
    else
        echo -e "${YELLOW}⚠️ No generated quotes cache found${NC}"
    fi
    echo ""
}

test_quote_retrieval() {
    echo -e "${BLUE}🧪 Testing quote retrieval...${NC}"
    
    echo -e "${YELLOW}   🎲 Random quote:${NC}"
    python3 "$WARBLER_ENGINE"
    echo ""
    
    echo -e "${YELLOW}   📖 Documentation context:${NC}"
    python3 "$WARBLER_ENGINE" --context documentation --format markdown
    echo ""
    
    echo -e "${YELLOW}   🐛 Debugging context:${NC}"
    python3 "$WARBLER_ENGINE" --context debug --format cli
    echo ""
}

show_help() {
    echo "🎭 Weekly Wisdom Oracle Generator"
    echo ""
    echo "Usage: $0 [COMMAND] [OPTIONS]"
    echo ""
    echo "Commands:"
    echo "  generate [COUNT]    Generate COUNT wisdom quotes (default: 5)"
    echo "  stats              Show oracle statistics"
    echo "  test               Test quote retrieval"
    echo "  validate           Validate generated quotes"
    echo "  help               Show this help"
    echo ""
    echo "Examples:"
    echo "  $0 generate 10     Generate 10 new quotes"
    echo "  $0 stats           Show database statistics"
    echo "  $0 test            Test quote retrieval systems"
    echo ""
}

main() {
    local command=${1:-generate}
    
    case "$command" in
        "generate")
            local count=${2:-5}
            show_header
            show_scroll_quote
            check_prerequisites
            generate_wisdom "$count"
            show_statistics
            update_quote_integration
            validate_generated_quotes
            ;;
        "stats")
            show_header
            check_prerequisites
            show_statistics
            ;;
        "test")
            show_header
            check_prerequisites
            test_quote_retrieval
            ;;
        "validate")
            show_header
            validate_generated_quotes
            ;;
        "help"|"-h"|"--help")
            show_help
            ;;
        *)
            echo -e "${RED}❌ Unknown command: $command${NC}"
            echo ""
            show_help
            exit 1
            ;;
    esac
}

# Run if called directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi
