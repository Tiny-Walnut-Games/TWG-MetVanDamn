#!/usr/bin/env bash
set -Eeuo pipefail

# Warbler Log Condenser
# - Archives files older than a cutoff date into monthly folders and packages them
# - Default cutoff: 2025-09-17 (pre-MetVanDAMN logs)
# - Operates on: ./debug and ./Logs if present
# - Bash-only, idempotent, safe on re-run

CUTOFF_DATE=${1:-"2025-09-17"}
ROOT_DIR=${2:-"."}

INFO_PREFIX="[Warbler]"

say() { echo "${INFO_PREFIX} $*"; }
err() { echo "${INFO_PREFIX} ERROR: $*" >&2; }

# Ensure path exists
ensure_dir() {
  local d="$1"
  [ -d "$d" ] || mkdir -p "$d"
}

# Ensure a unique destination path by appending (n) before extension if needed
ensure_unique_path() {
  local dest="$1"
  if [ ! -e "$dest" ]; then
    echo "$dest"
    return 0
  fi
  local dir base ext n
  dir=$(dirname -- "$dest")
  base=$(basename -- "$dest")
  ext=""
  if [[ "$base" == *.* ]]; then
    ext=".${base##*.}"
    base="${base%.*}"
  fi
  n=1
  local candidate
  while true; do
    candidate="${dir}/${base}(${n})${ext}"
    if [ ! -e "$candidate" ]; then
      echo "$candidate"
      return 0
    fi
    n=$((n+1))
  done
}

# Move all files older than CUTOFF_DATE from SRC_DIR into monthly archive under SRC_DIR/archives/YYYY-MM
archive_dir_old_files() {
  local SRC_DIR="$1"
  [ -d "$SRC_DIR" ] || return 0

  local ARCHIVES_ROOT="$SRC_DIR/archives"
  ensure_dir "$ARCHIVES_ROOT"

  # Find files older than cutoff; exclude metadata files
  # -not -newermt "$CUTOFF_DATE" means mtime < cutoff
  mapfile -d '' FILES < <(find "$SRC_DIR" -maxdepth 1 -type f \
    -not -name "*.meta" -not -name "*.DS_Store" \
    -not -name "*.tar.gz" -not -name "*.tgz" \
    -not -newermt "$CUTOFF_DATE" -print0)

  local moved=0
  for f in "${FILES[@]:-}"; do
    [ -n "${f}" ] || continue
    # Determine month folder by file's mtime
    local epoch month_dir dest base_name
    epoch=$(stat -c %Y "$f" 2>/dev/null || stat -f %m "$f")
    month_dir=$(date -u -d "@$epoch" +%Y-%m 2>/dev/null || date -ur "$epoch" +%Y-%m)
    ensure_dir "$ARCHIVES_ROOT/$month_dir"
    base_name=$(basename -- "$f")
    dest="$ARCHIVES_ROOT/$month_dir/$base_name"
    dest=$(ensure_unique_path "$dest")
    mv -f -- "$f" "$dest"
    moved=$((moved+1))
    say "Archived $(basename -- "$f") -> archives/$month_dir/"
  done

  echo "$moved"
}

# Package monthly folders into gz archives and optionally remove originals
package_months() {
  local SRC_DIR="$1"
  [ -d "$SRC_DIR/archives" ] || return 0
  local PACK_DIR="$SRC_DIR/archives/packages"
  ensure_dir "$PACK_DIR"

  local packaged=0
  shopt -s nullglob
  for month_dir in "$SRC_DIR/archives"/*/; do
    [ -d "$month_dir" ] || continue
    # Skip the packages dir itself
    if [[ "$month_dir" == "$PACK_DIR"/* || "$month_dir" == "$PACK_DIR"/ ]]; then
      continue
    fi
    local month_name
    month_name=$(basename -- "$month_dir")
    local tar_path="$PACK_DIR/logs-${month_name}.tar.gz"
    if [ -f "$tar_path" ]; then
      say "Package exists for $month_name, skipping"
      continue
    fi
    # Create package
    (cd "$SRC_DIR/archives" && tar -czf "$tar_path" "$month_name" )
    packaged=$((packaged+1))
    say "Packaged $month_name -> $(realpath "$tar_path" 2>/dev/null || echo "$tar_path")"
    # Remove original month folder after successful package
    rm -rf -- "$month_dir"
  done
  shopt -u nullglob
  echo "$packaged"
}

main() {
  say "Starting Warbler Log Condenser"
  say "Cutoff date: $CUTOFF_DATE"

  # If provided root dir is invalid, fall back to current directory
  if [ ! -d "$ROOT_DIR" ]; then
    say "Provided root dir '$ROOT_DIR' not found; falling back to '.'"
    ROOT_DIR="."
  fi

  local targets=("$ROOT_DIR/debug" "$ROOT_DIR/Logs")
  say "Scanning targets: ${targets[*]}"
  local total_moved=0
  local total_packaged=0

  for td in "${targets[@]}"; do
    if [ -d "$td" ]; then
      say "Processing directory: $td"
      local moved
      moved=$(archive_dir_old_files "$td")
      total_moved=$((total_moved + moved))
      local packaged
      packaged=$(package_months "$td")
      total_packaged=$((total_packaged + packaged))
    else
      say "Skipping missing directory: $td"
    fi
  done

  # Write a small JSON report for CI summary
  cat > warbler_condense_report.json <<EOF
{
  "cutoff": "$CUTOFF_DATE",
  "moved": $total_moved,
  "packaged": $total_packaged,
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
}
EOF

  say "Completed. Moved: $total_moved, Packaged: $total_packaged"
}

main "$@"
