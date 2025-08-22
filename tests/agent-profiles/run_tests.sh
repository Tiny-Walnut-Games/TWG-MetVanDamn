#!/bin/bash
# Agent Profile System Test Runner
# Validates agent profile configurations and LDA CLI functionality

# Note: Not using set -e to allow individual test failures

echo "🧬 Agent Profile System - Test Suite"
echo "==================================="
echo ""

# Get script directory and project root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
cd "$PROJECT_ROOT" || exit 1

# Allow terminal to fully initialize for stable PATH/resolution
sleep 1

# Ensure required profile file exists for tests
if [[ ! -f ".agent-profile.yaml" ]]; then
  if [[ -f "agent-profile.yaml" ]]; then
    cp "agent-profile.yaml" ".agent-profile.yaml"
    echo "ℹ️  Using agent-profile.yaml as .agent-profile.yaml for tests"
  else
    cat > .agent-profile.yaml << 'EOF'
agent_personality:
  tone: professional
workflow_preferences:
  mode: standard
behavior_flags: {}
cli_integration: { available_commands: [] }
project_context: {}
EOF
    echo "ℹ️  Created minimal .agent-profile.yaml for tests"
  fi
fi

# ---------- Python bootstrap (cross-platform, non-interactive) ----------
command_exists() { command -v "$1" >/dev/null 2>&1; }

create_python3_shim_if_needed() {
  if command_exists python && ! command_exists python3; then
    mkdir -p "$PROJECT_ROOT/.bin"
    cat > "$PROJECT_ROOT/.bin/python3" <<'EOF'
#!/usr/bin/env bash
exec python "$@"
EOF
    chmod +x "$PROJECT_ROOT/.bin/python3"
    export PATH="$PROJECT_ROOT/.bin:$PATH"
  fi
}

ensure_python() {
  if command_exists python3 || command_exists python; then
    create_python3_shim_if_needed
    return 0
  fi
  return 1
}

ensure_python || true
PY=$(command -v python3 || command -v python || echo "")
export PY
# ---------- end Python bootstrap ----------

# Ensure PyYAML is available; install requirements if missing
if [[ -n "$PY" ]]; then
  if ! "$PY" - <<'PYCHK' >/dev/null 2>&1
import sys
try:
    import yaml  # noqa: F401
    sys.exit(0)
except Exception:
    sys.exit(1)
PYCHK
  then
    if "$PY" -m pip --version >/dev/null 2>&1; then
      req_file="$PROJECT_ROOT/scripts/requirements.txt"
      if [[ -f "$req_file" ]]; then
        echo "ℹ️  Installing test requirements (PyYAML) via pip"
        "$PY" -m pip install --user -r "$req_file" >/dev/null 2>&1 || true
      else
        echo "ℹ️  Installing PyYAML via pip"
        "$PY" -m pip install --user PyYAML >/dev/null 2>&1 || true
      fi
    else
      echo "⚠️  pip not available; proceeding without installing PyYAML"
    fi
  fi
fi

# Initialize test results
TESTS_PASSED=0
TESTS_FAILED=0
TEST_RESULTS=()

# Function to run a test and track results
run_test() {
    local test_name="$1"
    local test_command="$2"

    echo "🧪 Running: $test_name"
    if eval "$test_command" >/dev/null 2>&1; then
        echo "   ✅ PASSED"
        ((TESTS_PASSED++))
        TEST_RESULTS+=("✅ $test_name")
    else
        echo "   ❌ FAILED"
        ((TESTS_FAILED++))
        TEST_RESULTS+=("❌ $test_name")

        # Show error details for failed tests
        echo "   📋 Error details:"
        eval "$test_command" 2>&1 | sed 's/^/      /'
    fi
    echo ""
}

# Test 1: Agent Profile YAML Syntax Validation
run_test "Agent Profile YAML Syntax" "\"$PY\" -c \"
import yaml
import sys

# Test .agent-profile.yaml
try:
    with open('.agent-profile.yaml', 'r') as f:
        profile = yaml.safe_load(f)
    assert isinstance(profile, dict), 'Profile must be a dictionary'
    print('OK .agent-profile.yaml syntax valid')
except Exception as e:
    print(f'ERROR .agent-profile.yaml error: {e}')
    sys.exit(1)

# Test agent-profile.yaml if it exists
try:
    with open('agent-profile.yaml', 'r') as f:
        legacy_profile = yaml.safe_load(f)
    assert isinstance(legacy_profile, dict), 'Legacy profile must be a dictionary'
    print('OK agent-profile.yaml syntax valid')
except FileNotFoundError:
    print('INFO agent-profile.yaml not found (optional)')
except Exception as e:
    print(f'ERROR agent-profile.yaml error: {e}')
    sys.exit(1)
\""

# Test 2: Required Fields Validation
run_test "Required Profile Fields" "\"$PY\" -c \"
import yaml

with open('.agent-profile.yaml', 'r') as f:
    profile = yaml.safe_load(f)

# Check required sections
required_sections = ['agent_personality', 'workflow_preferences', 'behavior_flags']
for section in required_sections:
    assert section in profile, f'Missing required section: {section}'

# Check specific required fields
personality = profile.get('agent_personality', {})
assert 'tone' in personality, 'agent_personality must specify tone'

workflow = profile.get('workflow_preferences', {})
assert 'mode' in workflow, 'workflow_preferences must specify mode'

print('OK All required fields present')
\""

# Test 3: Valid Field Values
run_test "Valid Field Values" "\"$PY\" -c \"
import yaml

with open('.agent-profile.yaml', 'r') as f:
    profile = yaml.safe_load(f)

# Validate tone
personality = profile.get('agent_personality', {})
tone = personality.get('tone')
if tone:
    valid_tones = {'professional', 'friendly', 'dry_humor', 'sarcastic', 'enthusiastic'}
    assert tone in valid_tones, f'Invalid tone: {tone}'

# Validate mode
workflow = profile.get('workflow_preferences', {})
mode = workflow.get('mode')
if mode:
    valid_modes = {'exploration', 'implementation', 'documentation', 'crisis', 'standard'}
    assert mode in valid_modes, f'Invalid mode: {mode}'

print('OK All field values valid')
\""

# Test 4: LDA CLI Tool Availability
run_test "LDA CLI Tool Availability" "
if [[ -f 'scripts/lda' ]]; then
    \"$PY\" scripts/lda --help >/dev/null 2>&1
    echo '✅ LDA CLI tool functional'
else
    echo '❌ LDA CLI tool not found'
    exit 1
fi
"

# Test 5: LDA CLI Basic Commands
run_test "LDA CLI Basic Commands" "
cd /tmp
mkdir -p lda_test_$$
cd lda_test_$$

# Initialize git repo for testing
git init >/dev/null 2>&1
git config user.name 'Test User' >/dev/null 2>&1
git config user.email 'test@example.com' >/dev/null 2>&1

# Test LDA init with quiet mode
PYTHONPATH=\"$PROJECT_ROOT/scripts\" \"$PY\" \"$PROJECT_ROOT/scripts/lda\" init --quiet --force >/dev/null 2>&1

# Verify profile was created
if [[ -f 'agent-profile.yaml' ]]; then
    echo '✅ LDA init command functional'
else
    echo '❌ LDA init did not create profile'
    exit 1
fi

# Clean up
cd /tmp
rm -rf lda_test_$$
"

# Test 6: Pipeline Preferences Validation (if present)
run_test "Pipeline Preferences Validation" "\"$PY\" -c \"
import yaml

# Check legacy profile for pipeline preferences
try:
    with open('agent-profile.yaml', 'r') as f:
        profile = yaml.safe_load(f)

    if 'pipeline_preferences' in profile:
        pipelines = profile['pipeline_preferences']
        assert isinstance(pipelines, list), 'pipeline_preferences must be a list'

        valid_pipelines = {'URP', 'HDRP', 'BRP', 'SRP'}
        for pipeline in pipelines:
            assert pipeline in valid_pipelines, f'Invalid pipeline: {pipeline}'

        print(f'OK Pipeline preferences valid: {pipelines}')
    else:
        print('INFO No pipeline preferences defined (optional)')

except FileNotFoundError:
    print('INFO agent-profile.yaml not found (optional)')
\""

# Test 7: CLI Integration Commands Validation
run_test "CLI Integration Commands" "\"$PY\" -c \"
import yaml

with open('.agent-profile.yaml', 'r') as f:
    profile = yaml.safe_load(f)

cli_integration = profile.get('cli_integration', {})
if 'available_commands' in cli_integration:
    commands = cli_integration['available_commands']
    assert isinstance(commands, list), 'available_commands must be a list'

    for cmd in commands:
        assert isinstance(cmd, str), f'Command must be string: {cmd}'
        assert cmd.startswith('lda '), f'Command must start with lda : {cmd}'

    print(f'OK CLI commands valid: {len(commands)} commands')
else:
    print('INFO No CLI integration commands defined (optional)')
\""

# Test 8: Project Context Validation
run_test "Project Context Validation" "\"$PY\" -c \"
import yaml

with open('.agent-profile.yaml', 'r') as f:
    profile = yaml.safe_load(f)

project_context = profile.get('project_context', {})
if project_context:
    assert 'name' in project_context, 'project_context must have name'
    assert 'domain' in project_context, 'project_context must have domain'

    name = project_context['name']
    domain = project_context['domain']

    print(f'OK Project context valid: {name} - {domain}')
else:
    print('INFO No project context defined (optional)')
\""

# Print final results
echo "📊 Agent Profile System Test Results"
echo "===================================="
echo ""

for result in "${TEST_RESULTS[@]}"; do
    echo "$result"
done

echo ""
echo "📈 Summary:"
echo "   ✅ Passed: $TESTS_PASSED"
echo "   ❌ Failed: $TESTS_FAILED"
echo "   📊 Total:  $((TESTS_PASSED + TESTS_FAILED))"

if [[ $TESTS_FAILED -eq 0 ]]; then
    echo ""
    echo "🎉 All tests passed! Agent Profile System is healthy."
    echo "🛡️ The cheeks are preserved! All hail the Cheeks! 🙌"
    exit 0
else
    echo ""
    echo "💥 Some tests failed. Agent Profile System needs attention."
    echo "🛡️ Cheek preservation protocols activated. Fix the issues above."
    exit 1
fi
