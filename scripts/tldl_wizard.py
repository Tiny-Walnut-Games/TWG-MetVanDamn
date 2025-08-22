#!/usr/bin/env python3
"""
TLDL Wizard (interactive)
- Guides you through creating a TLDL entry.
- Writes a Markdown file into docs/ with the correct filename pattern.

Usage:
  python scripts/tldl_wizard.py             # interactive
  python scripts/tldl_wizard.py --title "MyTopic" --author "@copilot" --context "Bug #123" --summary "Fixed X" --tags "feature,docs"

Notes:
- Works cross-platform.
- Keeps formatting aligned with docs/tldl_template.yaml.
"""
from __future__ import annotations

import argparse
import datetime as dt
import os
import re
import sys
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parent
DOCS_DIR = PROJECT_ROOT / "docs"
TEMPLATE_PATH = DOCS_DIR / "tldl_template.yaml"


def sanitize_title(title: str) -> str:
    s = title.strip()
    s = re.sub(r"[^A-Za-z0-9_-]+", "", s)
    return s or "Entry"


def now_ts() -> str:
    return dt.datetime.now(dt.timezone.utc).strftime("%Y-%m-%d %H:%M:%S UTC")


def build_markdown(date: str, title: str, author: str, context: str, summary: str, tags: list[str]) -> str:
    # Base header
    header = [
        "# TLDL Entry Template",
        f"**Entry ID:** TLDL-{date}-{title}",
        f"**Author:** {author}",
        f"**Context:** {context}",
        f"**Summary:** {summary}",
        "",
        "---",
        "",
        "> 📜 \"[Insert inspirational quote from Secret Art of the Living Dev using: `python3 src/ScrollQuoteEngine/quote_engine.py --context documentation --format markdown`]\"",
        "",
        "---",
        "",
    ]

    body = [
        "## Discoveries",
        "",
        "### [Discovery Category 1]",
        "- **Key Finding**: Describe what you learned or discovered",
        "- **Impact**: Why this discovery matters for the project",
        "- **Evidence**: Links to code, documentation, discussions, or external resources",
        "- **Root Cause**: If applicable, what caused the issue or led to this discovery",
        "",
        "### [Discovery Category 2]",
        "- **Key Finding**: Another significant discovery",
        "- **Impact**: Business or technical impact",
        "- **Evidence**: Supporting information or references",
        "- **Pattern Recognition**: If this relates to recurring themes or patterns",
        "",
        "## Actions Taken",
        "",
        "1. **[Action Name]**",
        "   - **What**: Specific action performed",
        "   - **Why**: Rationale behind the decision",
        "   - **How**: Implementation approach or method used",
        "   - **Result**: Outcome or current status",
        "   - **Files Changed**: List of modified files (if applicable)",
        "",
        "2. **[Another Action]**",
        "   - **What**: Description of the action",
        "   - **Why**: Reasoning and context",
        "   - **How**: Approach taken",
        "   - **Result**: What happened as a result",
        "   - **Validation**: How the result was verified",
        "",
        "## Technical Details",
        "",
        "### Code Changes",
        "```diff",
        "// Example of code changes made",
        "- Old implementation",
        "+ New implementation",
        "```",
        "",
        "### Configuration Updates",
        "```yaml",
        "# Example configuration changes",
        "old_setting: false",
        "new_setting: true",
        "```",
        "",
        "### Dependencies",
        "- **Added**: List any new dependencies",
        "- **Removed**: List any removed dependencies",
        "- **Updated**: List any updated dependencies with versions",
        "",
        "## Lessons Learned",
        "",
        "### What Worked Well",
        "- Effective approaches or techniques used",
        "- Tools or methods that proved valuable",
        "- Successful collaboration patterns",
        "",
        "### What Could Be Improved",
        "- Areas for optimization in future similar work",
        "- Process improvements identified",
        "- Tools or techniques to explore",
        "",
        "### Knowledge Gaps Identified",
        "- Areas where more research is needed",
        "- Missing documentation or resources",
        "- Training or skill development opportunities",
        "",
        "## Next Steps",
        "",
        "### Immediate Actions (High Priority)",
        "- [ ] Specific next step with clear owner assignment",
        "- [ ] Another immediate action item",
        "- [ ] Validation or testing to be completed",
        "",
        "### Medium-term Actions (Medium Priority)",
        "- [ ] Follow-up work to be scheduled",
        "- [ ] Documentation updates needed",
        "- [ ] Process improvements to implement",
        "",
        "### Long-term Considerations (Low Priority)",
        "- [ ] Strategic improvements or optimizations",
        "- [ ] Research and exploration items",
        "- [ ] Community or ecosystem contributions",
        "",
        "## References",
        "",
        "### Internal Links",
        f"- Related TLDL entries: [TLDL-{date}-RelatedTopic](./TLDL-{date}-RelatedTopic.md)",
        "- Project documentation: [Link to relevant docs]",
        "- Related issues or PRs: #XX, #YY",
        "",
        "### External Resources",
        "- Documentation: [Link to external docs]",
        "- Research papers or articles: [Academic or industry resources]",
        "- Community discussions: [Forum posts, Stack Overflow, etc.]",
        "- Tools and utilities: [Links to useful tools discovered]",
        "",
        "## DevTimeTravel Context",
        "",
        "### Snapshot Information",
        f"- **Snapshot ID**: DT-{date}-HHMMSS-ShortDesc",
        "- **Branch**: feature/branch-name or main",
        "- **Commit Hash**: abc123def (if applicable)",
        "- **Environment**: development, staging, production",
        "",
        "### File State",
        "- **Modified Files**: List of files changed during this work",
        "- **New Files**: List of files created",
        "- **Deleted Files**: List of files removed (if any)",
        "",
        "### Dependencies Snapshot",
        "```json",
        "{",
        "  \"python\": \"3.11.x\",",
        "  \"node\": \"18.x\",",
        "  \"frameworks\": [\"list\", \"of\", \"key\", \"dependencies\"]",
        "}",
        "```",
        "",
        "---",
        "",
        "## TLDL Metadata",
        "",
        f"**Tags**: {' '.join('#'+t.strip() for t in tags if t.strip()) if tags else '#documentation'}",
        "**Complexity**: Low | Medium | High",
        "**Impact**: Low | Medium | High | Critical",
        "**Team Members**: @username1, @username2",
        "**Duration**: X hours/days",
        "**Related Epics**: Epic Name or ID",
        "",
        f"**Created**: {now_ts()}",
        f"**Last Updated**: {now_ts()}",
        "**Status**: Draft | In Progress | Complete | Archived",
    ]

    return "\n".join(header + body) + "\n"


def write_file(date: str, title: str, content: str) -> Path:
    DOCS_DIR.mkdir(parents=True, exist_ok=True)
    fname = f"TLDL-{date}-{title}.md"
    out = DOCS_DIR / fname
    out.write_text(content, encoding="utf-8")
    return out


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description="Interactive TLDL wizard")
    parser.add_argument("--title")
    parser.add_argument("--author", default="@copilot")
    parser.add_argument("--context", default="")
    parser.add_argument("--summary", default="")
    parser.add_argument("--tags", default="")
    args = parser.parse_args(argv)

    today = dt.datetime.now(dt.timezone.utc).strftime("%Y-%m-%d")

    title = args.title or input("Title (e.g., FeatureXFix): ").strip()
    while not title:
        title = input("Title (required): ").strip()
    safe_title = sanitize_title(title)

    author = args.author or input("Author (default @copilot): ").strip() or "@copilot"
    context = args.context or input("Context (issue/feature): ").strip()
    summary = args.summary or input("Summary (one line): ").strip()
    tags = [t.strip() for t in (args.tags or input("Tags (comma-separated): ").strip()).split(",") if t.strip()]

    content = build_markdown(today, safe_title, author, context, summary, tags)
    out = write_file(today, safe_title, content)
    print(f"Created: {out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
