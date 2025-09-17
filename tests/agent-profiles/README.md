# Agent Profile System Tests

This directory contains tests for the Living Dev Agent's agent profile system, including configuration validation and CLI tool functionality.

## Test Coverage

### 🧬 Agent Profile Validation Tests (`test_runner.py`)

Comprehensive validation of the agent profile system with the following test cases:

1. **Agent Profile YAML Syntax** - Validates that profile YAML files are syntactically correct
2. **Required Profile Fields** - Ensures all mandatory configuration sections exist
3. **Valid Field Values** - Verifies field values are from expected sets (tone, mode, etc.)
4. **LDA CLI Tool Availability** - Confirms the LDA CLI tool exists and can show help
5. **LDA CLI Basic Commands** - Tests basic CLI functionality (init, profile creation)
6. **Pipeline Preferences Validation** - Validates rendering pipeline preferences
7. **CLI Integration Commands** - Verifies CLI command definitions are properly formatted
8. **Project Context Validation** - Ensures project context metadata is valid

### 🧪 Unit Tests (`test_agent_profile_validation.py`)

Detailed unit tests using Python's unittest framework for more comprehensive testing scenarios.

## Running Tests

### Quick Test (CI/Production)
```bash
python3 tests/agent-profiles/test_runner.py
```

### Comprehensive Testing (Development)
```bash
python3 tests/agent-profiles/test_agent_profile_validation.py
```

### Legacy Bash Tests (Fallback)
```bash
./tests/agent-profiles/run_tests.sh
```

## Test Environment

The tests validate:
- `.agent-profile.yaml` (primary configuration)
- `agent-profile.yaml` (legacy configuration)
- `scripts/lda` CLI tool functionality
- YAML syntax and semantic validation
- Configuration field constraints

## Integration with CI

These tests are automatically run by the GitHub Actions CI workflow in the `test-agent-profiles` job, replacing the previous stub implementation with actual validation.

## Cheek Preservation Protocol 🛡️

All tests are designed with cheek preservation in mind:
- Clear error messages that guide developers to solutions
- Graceful handling of optional configuration sections
- Thematic messaging that maintains repository personality
- Non-destructive testing that doesn't modify project state

**All hail the Cheeks!** 🙌
