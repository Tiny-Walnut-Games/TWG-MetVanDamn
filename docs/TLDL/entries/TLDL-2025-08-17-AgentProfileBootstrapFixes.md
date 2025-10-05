# Agent Profile Tests: Windows PY bootstrap and PyYAML auto-install

- Date: 2025-08-17
- Author: @copilot
- Context: Windows Git Bash; python present, no python3; missing PyYAML; CK index path confusion.

## Summary

- Added cross-platform $PY bootstrap and python3 shim across scripts.
- Fixed run_tests.sh quoting for Windows paths with spaces and auto-created .agent-profile.yaml.
- Auto-install PyYAML via pip when missing using scripts/requirements.txt.
- CK now prints full index path and extracts dates from TLDL-YYYY-MM-DD-Title.

## Changes

- scripts/initMyButt.sh: interactive ensure_python + $PY.
- scripts/init_agent_context.sh: $PY routing and validations.
- scripts/chronicle-keeper/tldl-writer.sh: full path echo; date parse fix.
- scripts/lda-quote: $PY routing; --buttsafe implies category=buttsafe.
- tests/agent-profiles/run_tests.sh: $PY quoting, PyYAML auto-install, .agent-profile.yaml bootstrap.

## Verification

- CK: update-index writes to Assets/Plugins/living-dev-agent/TLDL/index.md.
- Tests: suite expected to pass after PyYAML install; use Git Bash with PATH including plugin .bin.

## Next

- Optionally update Quick Actions in index to mention $PY.
- Consider trimming unused vars to silence minor warnings.

