#!/usr/bin/env python3
"""
Minimal MCP-like HTTP server for the Living Dev Agent template.

This is not a full MCP JSON-RPC implementation. It provides pragmatic HTTP endpoints
that mirror the tools declared in mcp-config.json so you can run and test locally.

Endpoints:
- GET  /health
- POST /tools/validate_tldl
- POST /tools/create_tldl_entry
- POST /tools/capture_devtimetravel_snapshot
- POST /tools/run_linters
- POST /tools/validate_debug_overlay
- GET  /tools/get_project_health

Start:
  python -m scripts.mcp_server

Requires: fastapi, uvicorn, pydantic, pyyaml
Install:  pip install -r scripts/requirements.txt
"""
from __future__ import annotations

import os
import re
import time
from datetime import datetime
from pathlib import Path
from typing import List, Optional

import sys
import traceback

# Dependency import guard (prints actionable message if deps missing)
try:
    from fastapi import FastAPI, HTTPException
    from pydantic import BaseModel
    import yaml
except Exception as _imp_err:
    print("[MCP] Missing or failed dependency import:", _imp_err)
    print("[MCP] Run: pip install -r scripts/requirements.txt (from the plugin root)")

# Resolve project root as the parent of this script's directory
SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parent
DOCS_DIR = PROJECT_ROOT / "docs"
DEVTIME_DIR = PROJECT_ROOT / ".devtimetravel" / "snapshots"
TLDL_TEMPLATE = DOCS_DIR / "tldl_template.yaml"

app = FastAPI(title="Living Dev Agent MCP Server", version="0.1.0")


class ValidateTLDLRequest(BaseModel):
    path: str
    strict: Optional[bool] = False


class CreateTLDLRequest(BaseModel):
    title: str
    context: Optional[str] = None
    author: Optional[str] = "@copilot"


class SnapshotRequest(BaseModel):
    message: str
    tag: Optional[str] = None
    include_uncommitted: Optional[bool] = True


class RunLintersRequest(BaseModel):
    path: Optional[str] = "src/"
    linter_type: Optional[str] = "all"
    output_format: Optional[str] = "text"


class ValidateDebugOverlayRequest(BaseModel):
    path: Optional[str] = "src/DebugOverlayValidation/"


@app.get("/health")
def health():
    return {"status": "ok", "time": time.time(), "project_root": str(PROJECT_ROOT)}


@app.post("/tools/validate_tldl")
def validate_tldl(req: ValidateTLDLRequest):
    target = Path(req.path)
    if not target.is_absolute():
        target = PROJECT_ROOT / target

    if not target.exists():
        raise HTTPException(status_code=400, detail=f"Path not found: {target}")

    files: List[Path] = []
    if target.is_dir():
        files = sorted([p for p in target.glob("TLDL-*.md") if p.is_file()])
    else:
        files = [target]

    issues = []
    validated = 0

    for f in files:
        text = f.read_text(encoding="utf-8", errors="ignore")
        # Very light checks: ID and minimal sections
        if not re.search(r"entry_id\s*:\s*\"?TLDL-\d{4}-\d{2}-\d{2}-", text):
            issues.append(f"{f.name}: missing or malformed entry_id")
        if "discoveries:" not in text:
            issues.append(f"{f.name}: missing discoveries section")
        if "actions_taken:" not in text:
            issues.append(f"{f.name}: missing actions_taken section")
        if "next_steps:" not in text:
            issues.append(f"{f.name}: missing next_steps section")
        validated += 1

    status = "PASS" if not issues else ("FAIL" if req.strict else "WARN")
    return {
        "status": status,
        "checked": validated,
        "issues": issues,
    }


@app.post("/tools/create_tldl_entry")
def create_tldl_entry(req: CreateTLDLRequest):
    DOCS_DIR.mkdir(parents=True, exist_ok=True)
    if not TLDL_TEMPLATE.exists():
        raise HTTPException(status_code=500, detail=f"Template not found: {TLDL_TEMPLATE}")

    today = datetime.now().strftime("%Y-%m-%d")
    safe_title = re.sub(r"[^A-Za-z0-9_-]", "", req.title.strip()).strip("-") or "Entry"
    filename = f"TLDL-{today}-{safe_title}.md"
    out_path = DOCS_DIR / filename

    if out_path.exists():
        return {"status": "EXISTS", "path": str(out_path)}

    content = TLDL_TEMPLATE.read_text(encoding="utf-8")
    content = content.replace("YYYY-MM-DD", today).replace("DescriptiveTitle", safe_title)

    # Best-effort enrichment
    if req.context:
        content += f"\ncontext: \"{req.context}\"\n"
    if req.author:
        content = content.replace('author: "Your Name or @copilot"', f'author: "{req.author}"')

    out_path.write_text(content, encoding="utf-8")
    return {"status": "CREATED", "path": str(out_path)}


@app.post("/tools/capture_devtimetravel_snapshot")
def capture_snapshot(req: SnapshotRequest):
    DEVTIME_DIR.mkdir(parents=True, exist_ok=True)
    ts = datetime.now().strftime("%Y%m%d-%H%M%S")
    snap = {
        "message": req.message,
        "tag": req.tag,
        "time": ts,
        "include_uncommitted": req.include_uncommitted,
    }
    out = DEVTIME_DIR / f"snapshot-{ts}.json"
    out.write_text(yaml.safe_dump(snap, sort_keys=False), encoding="utf-8")
    return {"status": "OK", "snapshot": str(out)}


@app.post("/tools/run_linters")
def run_linters(req: RunLintersRequest):
    # Placeholder: just check the requested path exists
    target = Path(req.path) if req.path else PROJECT_ROOT / "src"
    if not target.is_absolute():
        target = PROJECT_ROOT / target
    exists = target.exists()
    return {"status": "OK" if exists else "NOT_FOUND", "path": str(target)}


@app.post("/tools/validate_debug_overlay")
def validate_debug_overlay(req: ValidateDebugOverlayRequest):
    target = Path(req.path) if req.path else PROJECT_ROOT / "src" / "DebugOverlayValidation"
    if not target.is_absolute():
        target = PROJECT_ROOT / target
    return {"status": "OK", "path": str(target)}


@app.get("/tools/get_project_health")
def get_project_health(include_details: bool = False):
    tldl = list(DOCS_DIR.glob("TLDL-*.md")) if DOCS_DIR.exists() else []
    health = {
        "tldl_count": len(tldl),
        "docs_dir": str(DOCS_DIR),
        "status": "GOOD" if len(tldl) >= 0 else "UNKNOWN",
    }
    if include_details:
        health["tldl_files"] = [p.name for p in tldl[:50]]
    return health


def _run():
    import uvicorn
    port = int(os.environ.get("MCP_PORT", "8000"))
    host = os.environ.get("MCP_HOST", "127.0.0.1")
    print(f"[MCP] Starting server on {host}:{port} (cwd={os.getcwd()})")
    try:
        uvicorn.run(
            "scripts.mcp_server:app",
            host=host,
            port=port,
            reload=False,
            log_level="info",
        )
    except Exception:
        print("[MCP] FATAL: failed to start server:")
        traceback.print_exc()
        sys.exit(1)


if __name__ == "__main__":
    _run()
