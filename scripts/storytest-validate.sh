#!/bin/sh
# Story Test philosophy validator
# Fails the analysis if violations are detected.

REPORT_DIR=".qodana"
REPORT_FILE="$REPORT_DIR/storytest-report.txt"

mkdir -p "$REPORT_DIR"
: > "$REPORT_FILE" # truncate report

echo "== MetVanDAMN Story Test Philosophy Validation ==" | tee -a "$REPORT_FILE"
echo "Timestamp: $(date -u +"%Y-%m-%dT%H:%M:%SZ")" | tee -a "$REPORT_FILE"
echo "" | tee -a "$REPORT_FILE"

VIOLATIONS=0

# Helper to run a grep rule and record violations
run_rule() {
  RULE_NAME="$1"
  REGEX="$2"
  DESCRIPTION="$3"
  HINT="$4"

  echo "Rule: $RULE_NAME" | tee -a "$REPORT_FILE"
  echo " - $DESCRIPTION" | tee -a "$REPORT_FILE"

  # Search across C# sources, excluding common build/generated folders
  MATCHES=$(grep -RInE --include='*.cs' \
    --exclude-dir=.git \
    --exclude-dir=.idea \
    --exclude-dir=.qodana \
    --exclude-dir=Library \
    --exclude-dir=Logs \
    --exclude-dir=Temp \
    --exclude-dir=UserSettings \
    --exclude-dir=Build \
    --exclude-dir=bin \
    --exclude-dir=obj \
    "$REGEX" . || true)

  if [ -n "$MATCHES" ]; then
    COUNT=$(printf "%s\n" "$MATCHES" | wc -l | tr -d ' ')
    VIOLATIONS=$((VIOLATIONS + COUNT))
    echo " - Violations ($COUNT):" | tee -a "$REPORT_FILE"
    printf "%s\n" "$MATCHES" | tee -a "$REPORT_FILE"
    echo " - Hint: $HINT" | tee -a "$REPORT_FILE"
  else
    echo " - OK" | tee -a "$REPORT_FILE"
  fi
  echo "" | tee -a "$REPORT_FILE"
}

# 1) No TODO/FIXME/HACK/TEMP markers in code
run_rule "No Dev Artifact Markers" \
  '\b(TODO|FIXME|HACK|TEMP)\b' \
  "Reject dev-only markers; production code must be complete and documented without TODOs." \
  "Remove markers and finish the implementation or file an issue; leave code complete."

# 2) No NotImplementedException placeholders
run_rule "No NotImplementedException" \
  'System\.NotImplementedException|NotImplementedException' \
  "Placeholders are not allowed in production-ready systems." \
  "Replace with a complete implementation or remove the unused code path."

# 3) No single-line empty method bodies: `void Foo() { }`
#    This is a heuristic and may flag trivial intentional empties; prefer documenting and removing unused code.
run_rule "No Empty Method Bodies" \
  '\)\s*\{\s*\}' \
  "Methods must not be empty placeholders." \
  "Provide a complete implementation or remove the method."

# Summary and exit code
echo "Total violations: $VIOLATIONS" | tee -a "$REPORT_FILE"

if [ "$VIOLATIONS" -gt 0 ]; then
  echo ""
  echo "Story Test validation failed. See $REPORT_FILE for details."
  exit 1
else
  echo "Story Test validation passed."
  exit 0
fi
