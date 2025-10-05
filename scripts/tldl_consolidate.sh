#!/usr/bin/env bash
set -Eeuo pipefail

# TLDL Consolidation Guard
# Enforces canonical location for TLDL entries and optionally migrates legacy files.
# - Canonical: docs/TLDL/entries
# - Legacy:    TLDL/entries (repo root)
# - Archive:   docs/TLDL-Archive
# Usage:
#   bash scripts/tldl_consolidate.sh             # report only
#   bash scripts/tldl_consolidate.sh --fix       # perform moves

CANONICAL_DIR="docs/TLDL/entries"
LEGACY_DIR="TLDL/entries"
ARCHIVE_DIR="docs/TLDL-Archive"

MODE="report"
if [[ "${1:-}" == "--fix" ]]; then MODE="fix"; fi

say() { echo "[TLDL] $*"; }
err() { echo "[TLDL] ERROR: $*" >&2; }

ensure_dir() { [ -d "$1" ] || mkdir -p "$1"; }

ensure_dir "$CANONICAL_DIR"
ensure_dir "$ARCHIVE_DIR"

report_json() {
  local moved_canon="$1" moved_archive="$2" dups="$3" legacy_total="$4" nonconform="$5" stray_total="$6"
  cat > tldl_consolidate_report.json <<EOF
{
  "mode": "$MODE",
  "legacy_total": $legacy_total,
  "stray_total": $stray_total,
  "moved_to_canonical": $moved_canon,
  "moved_to_archive": $moved_archive,
  "duplicates": $dups,
  "nonconforming_names": $nonconform,
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
}
EOF
}

is_external_topic() {
  # Heuristic: archive entries that reference external/legacy MetVanDAMN import to avoid polluting active stream
  grep -qiE '\bMetVanDAMN\b|\bDungeonDelveMode\b' "$1" && return 0 || return 1
}

is_name_conforming() {
  # Accept both patterns: TLDL-YYYY-MM-DD-... and YYYY-MM-DD--...
  local base; base=$(basename -- "$1")
  [[ "$base" =~ ^TLDL-[0-9]{4}-[0-9]{2}-[0-9]{2}- ]] || [[ "$base" =~ ^[0-9]{4}-[0-9]{2}-[0-9]{2}-- ]]
}

main() {
  local legacy_count=0 moved_canon=0 moved_archive=0 dups=0 nonconform=0 stray_total=0

  shopt -s nullglob

  # Collect legacy entries (root-level TLDL/entries)
  local entries=()
  if [ -d "$LEGACY_DIR" ]; then
    entries=("$LEGACY_DIR"/*.md)
    legacy_count=${#entries[@]}
    if [ $legacy_count -gt 0 ]; then
      say "Found $legacy_count legacy TLDL entries in $LEGACY_DIR"
    else
      say "Legacy directory exists but is empty."
    fi
  else
    say "No legacy directory found ($LEGACY_DIR)."
  fi

  # Collect stray entries anywhere else matching TLDL-*.md excluding canonical, archive, legacy and templates
  local stray_candidates=()
  while IFS= read -r -d $'\0' file; do
    stray_candidates+=("$file")
  done < <(find . -type f -name 'TLDL-*.md' \
      -not -path "./$CANONICAL_DIR/*" \
      -not -path "./$ARCHIVE_DIR/*" \
      -not -path "./$LEGACY_DIR/*" \
      -not -path "./templates/*" \
      -print0)

  stray_total=${#stray_candidates[@]}
  if [ $stray_total -gt 0 ]; then
    say "Found $stray_total stray TLDL entries outside canonical/archive:"
    for s in "${stray_candidates[@]}"; do echo " - $s"; done
  fi

  # Process legacy entries
  for src in "${entries[@]}"; do
    local base; base=$(basename -- "$src")

    # Nonconforming names are allowed but flagged
    if ! is_name_conforming "$src"; then
      nonconform=$((nonconform+1))
    fi

    local dest_canon="$CANONICAL_DIR/$base"
    local dest_archive="$ARCHIVE_DIR/$base"

    if [ -f "$dest_canon" ]; then
      say "Duplicate in canonical exists: $base (skipping move)"
      dups=$((dups+1))
      continue
    fi

    if is_external_topic "$src"; then
      say "Classifying as external/legacy: $base -> $dest_archive"
      if [ "$MODE" = "fix" ]; then
        mv -f -- "$src" "$dest_archive"
      fi
      moved_archive=$((moved_archive+1))
    else
      say "Moving to canonical: $base -> $dest_canon"
      if [ "$MODE" = "fix" ]; then
        mv -f -- "$src" "$dest_canon"
      fi
      moved_canon=$((moved_canon+1))
    fi
  done

  # Process stray entries
  for src in "${stray_candidates[@]}"; do
    local base; base=$(basename -- "$src")

    # Nonconforming names are allowed but flagged
    if ! is_name_conforming "$src"; then
      nonconform=$((nonconform+1))
    fi

    local dest_canon="$CANONICAL_DIR/$base"
    local dest_archive="$ARCHIVE_DIR/$base"

    if [ -f "$dest_canon" ]; then
      say "Duplicate in canonical exists for stray: $base (skipping move)"
      dups=$((dups+1))
      continue
    fi

    if is_external_topic "$src"; then
      say "Classifying stray as external/legacy: $base -> $dest_archive"
      if [ "$MODE" = "fix" ]; then
        mv -f -- "$src" "$dest_archive"
      fi
      moved_archive=$((moved_archive+1))
    else
      say "Relocating stray to canonical: $base -> $dest_canon"
      if [ "$MODE" = "fix" ]; then
        mv -f -- "$src" "$dest_canon"
      fi
      moved_canon=$((moved_canon+1))
    fi
  done

  shopt -u nullglob

  report_json "$moved_canon" "$moved_archive" "$dups" "$legacy_count" "$nonconform" "$stray_total"

  say "Consolidation $MODE complete. Moved to canonical: $moved_canon, to archive: $moved_archive, duplicates: $dups, nonconforming: $nonconform, stray_total: $stray_total"

  # If fix mode and legacy dir is now empty, remove it to keep tree clean
  if [ "$MODE" = "fix" ] && [ -d "$LEGACY_DIR" ]; then
    if [ -z "$(ls -A "$LEGACY_DIR" 2>/dev/null)" ]; then
      rmdir "$LEGACY_DIR" || true
      # Remove parent TLDL if empty and not the canonical docs path
      if [ -d "TLDL" ] && [ -z "$(ls -A "TLDL" 2>/dev/null)" ]; then
        rmdir "TLDL" || true
      fi
    fi
  fi
}

main "$@"
