# TLDL: MetVanDAMN — Integration & CI Chronicle Keeper Wiring

**Author:** @Bellok — Tiny Walnut Games  
**Date:** 2025‑08‑21  
**Security Classification:** Internal Development Log  
**Last AI Session:** 2025‑08‑21 10:12 EDT

---

## 🎯 Purpose

Wire the engine’s tests, logs, and narrative outputs into CI so every push/PR auto‑produces readable artifacts:
- **Tests run**, **badges update**, and a **Chronicle Keeper** entry is minted with the “what/why/how/when.”
- PR and issue text is safely parsed (multiline‑safe) and used to seed TLDLs and Capsule Scrolls.

---

## 🗓 Timeline & Context

- **When:** Immediately after unit tests + smoke test scene were stabilized.  
- **Why:** Ensure reproducibility and create a ritual where every run narrates itself.  
- **How:** Add/patch CI workflows, introduce safe JSON extraction for PR/issue bodies, and post scroll summaries on runs.

---

## ✅ Completed Integration

- **CI: Test & Validate Workflow**
  - **Trigger:** `push` to default branches and `pull_request`.
  - **Steps:** checkout → Unity cache restore (optional) → run Edit‑Mode tests → pack coverage → upload artifacts.
  - **Artifacts:** `test-results.xml`, `coverage/` archive, `seed_snapshots/` (when present).
  - **Badges:** Reachability, Loop Density, Tests Pass — exposed for README/Docs.

- **CI: Chronicle Keeper Workflow**
  - **Trigger:** `pull_request`, `issues`, `issue_comment` (selected events).
  - **Parsing:** Multiline‑safe extraction of event bodies via temp‑file pattern:
    ```bash
    echo '${{ toJson(github.event.pull_request.body) }}' > /tmp/pr-body.json
    PR_BODY_JSON=$(cat /tmp/pr-body.json)
    ```
  - **Synthesis:** Builds a TLDL summary (what changed, why, validation verdicts).
  - **Posting:** Adds a comment to PR/Issue with badges + distilled TLDL; stores a Markdown scroll artifact.

- **Safety Patch: PR Body Handling**
  - Replaced direct string assignment with temp‑file write/read to prevent shell mangling of multiline/special‑character bodies.
  - Applied to PR, Issue, and Comment handlers for consistency.

- **Docs Hooks**
  - README shields (Tests/Reachability/Loop Density/Butt‑Safety).
  - Links from shields → the latest Chronicle entry artifact for that branch/PR.

---

## 🔬 What the Workflows Produce (Per Run)

- **Test Summary:** Total, passed, failed, duration, and top failing suites (if any).  
- **Coverage Glance:** Core vs. Generation namespaces coverage percentages; link to full LCOV/HTML if generated.  
- **Engine Verdicts:**  
  - Reachability: Pass/Fail (with first failing gate or orphaned node, if any).  
  - Loop Density: Pass/Guarded Fail (observed vs. target).  
  - Polarity Audit: OK/Warnings (seam mismatches, out‑of‑range strengths).  
- **Seed Snapshot:** Optional JSON snapshot or image/ASCII map for canonical seeds.  
- **Scroll:** Quad‑fenced Markdown “run recap” suitable for copy/paste into the living archive.

---

## 🧭 Reproduction (Gifted‑Children Tutorial)

1. **Create/Update PR** with a descriptive title and body.  
   - Include checkable details (“What changed”, “Why”, “Risks”, “How to test”).
2. **Let CI run** (both workflows will trigger automatically).  
3. **Open the PR conversation tab:**
   - Look for the Chronicle Keeper comment with badges and the TLDL recap.
   - Download artifacts if you want the full test report or seed snapshots.
4. **If you need a Capsule Scroll:**  
   - Add a comment starting with `TLDL:` or include 🧠 in the title.  
   - CI will mint/pin a standalone scroll artifact summarizing that insight.
5. **Confirm README Badges:**  
   - On default branch merges, shields update to reflect the latest state.

---

## 🛠 Implementation Notes

- **Multiline JSON Safety:** All event bodies are written to `/tmp/*.json` and read back before `jq` parsing.  
- **Idempotency:** Re‑runs replace prior comments (using a hidden marker) to avoid clutter.  
- **Artifacts Retention:** Kept for 30–90 days (configurable); long‑term chronicles live in `docs/chronicles/`.

---

## 📌 Known Issues / Next Pass

- **Unity cache layer:** Optional; enabling reduces CI time but requires runner permissions.  
- **ASCII/Visual Maps:** Not yet generated in CI; add a lightweight renderer for seed diffs.  
- **Progression Simulator:** Replace simple “PathValidation” with full reachability/backtrack scoring to power the Reachability badge.  
- **Per‑Event Granularity:** Issue comment edits don’t retro‑update older scrolls yet (append‑only).

---

## 🎯 Next Steps

1. Add a **Seed Diff Renderer** (ASCII or tiny PNGs) to embed visual before/after in scrolls.  
2. Wire **ProgressionSimulatorSystem** into CI to produce authoritative Reachability/Backtrack metrics.  
3. Expose a **“Download Scroll”** artifact per run with deterministic naming (`chronicle-<run-id>.md`).  
4. Add **PR Template** prompts for “Butt Safety” and “Chronicle triggers” to improve auto‑summaries.  
5. Publish a **Docs/CI** page that explains badges and links to example scrolls.

---

## 📜 Lessons Learned

- Treat CI as a narrative device: if a run can’t explain itself in one screen, it won’t teach a future contributor.  
- Multiline safety for event bodies prevents elusive parsing bugs that derail auto‑docs.  
- Badges are only useful when they deep‑link to artifacts that prove their verdicts.

---

**Milestone Goal:** CI that tests, scores, and narrates the engine’s state on every change.  
**Success Criteria:** On any PR, you get a Chronicle comment with badges, a TLDL recap, and artifacts; on merge, README shields reflect the latest truth.
