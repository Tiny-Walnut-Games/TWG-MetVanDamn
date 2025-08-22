# The Scribe (TLDLScribeWindow)

> KeeperBook: Living Dev Agent authoring surface for TLDL (The Living Dev Log) and Issue templates.

## Purpose
The Scribe window streamlines creation, curation, and evolution of structured developer knowledge: discoveries, actions, proofs, lessons, and next steps. It binds a structured form to a raw markdown editor and a rendered preview, minimizing drift while allowing freeform editing.

## High-Level Flow
1. Fill structured form sections (Discoveries, Actions, Technical Details, etc.)
2. Generate or auto-sync into Raw Editor (idempotent snapshot builder)
3. Optionally refine raw markdown manually (auto-sync pauses while dirty)
4. Preview rendered content (images, checklists, headings)
5. Persist to disk in active documentation directory

## Key Features
- Form → Raw synchronization with dirty-guard
- GitBook-like navigator (left panel) with:
  - Stable alphabetical sorting
  - Directory activation context
  - Image thumbnail preview
  - File duplication & reveal actions
- Image pipeline: import -> images/ subfolder -> cursor-aware markdown insertion
- Template-driven issue creation via registry (YAML) with auto README bootstrap
- Metadata hydration when loading existing markdown (author/context/summary/tags)
- Light inline markdown rendering for preview (headings, lists, code, images, checklists)
- Deterministic markdown generation for reproducible commits / diffs

## Architecture Overview
```
+------------------+     +------------------+     +------------------+
|   Form Model     | --> | Markdown Builder | --> | Raw Editor Buffer |
+------------------+     +------------------+     +------------------+
         ^                         |                         |
         | (Parse metadata)        | (Regenerate)            v
         |                         v                 +----------------+
         |                 +------------------+      | Preview Renderer|
         +---------------- |  Auto-Sync Guard | <----+----------------+
                           +------------------+
```

### Auto-Sync Guard
Prevents overwriting manual edits. Tracks last generated snapshot string; if user modifies raw content (dirty flag set) future form changes do not auto-regenerate until user manually triggers an update.

### Image Cache (LRU)
In-memory dictionary + insertion order list with max size (128). Evicts oldest textures to avoid editor memory bloat during long documentation sessions.

## KeeperNotes (Design Rationale)
| Area | Rationale |
|------|-----------|
| Extension HashSets | Centralized policy for supported document and image types. |
| Navigator | Promotes archive thinking: browsing past logs encourages continuity & reuse. |
| Safe Duplication | Enables experimentation without risking original scrolls. |
| Cursor Insert | Preserves writing flow: images appear exactly where intent expressed. |
| Metadata Parse | Rehydrates form for iterative updates instead of starting blank. |
| Auto-Sync Skip When Dirty | Honors user intent; prevents silent loss of manual adjustments. |
| Deterministic Builder | Stable output = smaller diffs + easier review history. |

## Templates System
Templates live under `templates/comments/*.yaml` with a simple registry file listing keys, titles, and file references. The loader uses a lightweight scan for `template: |` blocks, avoiding a full YAML dependency inside the editor assembly.

### Example registry excerpt
```
templates:
  bug_discovery:
    title: "Bug Discovery"
    file: bug_discovery.yaml
  debugging_ritual:
    title: "Debugging Ritual"
    file: debugging_ritual.yaml
```

## Public-ish Entry Points
Although internal to the editor script, these are the primary conceptual operations:
- Generate From Form → Editor
- Update / Preview From Form
- Create TLDL File
- Create Issue From Template
- Insert Image…

## Markdown Generation Rules
- Sections included only when their toggle is active
- Ordering fixed to maintain consistency
- Bullet lists normalized (trim + skip blank)
- Checklists rendered as `- [ ]` items
- Code / config fenced with language hints (diff, yaml)

## Potential Extensions
- Backward parse of full sections (e.g., actions table) to rehydrate lists
- Tag autocompletion sourced from historical entries
- Export to GitBook-ready directory tree with index generation
- Embedded diff viewer for modified raw vs last generated snapshot

---
# ScribeUtils
Small static helper focused on pure string transformations to keep the main window lean.

## Functions
| Function | Responsibility |
|----------|----------------|
| SanitizeTitle | Safe filename fragment (alnum, underscore, dash) fallback to Entry |
| Bulletize | Convert newline list into `-` bullets (skips blanks) |
| Checklist | Same as Bulletize but with unchecked boxes |
| FormatTags | Transform comma-separated tags into space-separated #tags (dashed) |

## Design Notes
- Pure methods (no side effects) -> easy to unit test
- Encoding decisions centralized (e.g., dash substitution in tags)
- Avoid allocations beyond a single StringBuilder per call

## Example
Input:
```
Deps Added:
Newtonsoft.Json
 Serilog

```
Bulletize ->
```
- Newtonsoft.Json
- Serilog
```

---
# GitBook Mock Structure
Suggested layout if exporting to GitBook-compatible docs folder:
```
/Docs
  /scribe
    index.md               # Overview (this file distilled)
    usage.md               # Walkthrough & screenshots
    architecture.md        # Diagram + lifecycle
    templates.md           # Template system description
    images.md              # Image workflow & cache mechanics
    changelog.md           # KeeperNotes summarizing deltas
  /tldl
    README.md              # Purpose of TLDL entries
    examples/
      sample-action-log.md
```

### index.md (excerpt)
```
# The Scribe
A focused editor window for crafting durable, high-signal engineering log entries.
```

### usage.md (outline)
1. Opening the window
2. Selecting root directory
3. Authoring workflow (form → generate → refine → preview → persist)
4. Image insertion flow
5. Issue template creation

### architecture.md (outline)
- Component diagram
- Auto-sync guard logic
- Snapshot hashing strategy
- LRU cache behavior

---
## Changelog Strategy
KeeperNotes inside code double as source for curated changelog entries, enabling semi-automated extraction later (regex on `KeeperNote:` lines).

---
## Future Automation Hooks
- CLI parity: headless generation of TLDL from JSON form snapshot
- MCP tool integration for remote creation
- Validation pass to ensure all referenced images exist on disk

---
## License / Attribution
Portions authored with assistance from @copilot (Keeper assisted). All inserted KeeperNotes are documentation metadata and may be stripped for production builds.
