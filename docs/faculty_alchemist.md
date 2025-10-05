# Alchemist Faculty (Narrative → Evidence Distillation)

Purpose  
Transforms narrative incubation artifacts (Gu Pot issues) into empirically validated, reproducible claims and synthesized reports. It is the “distillery” that decides when story energy becomes measurable system advancement.

Core Concept  
Gu Pot = Narrative selective survival.  
Evidence Pipeline = Deterministic experiment & claim engine.  
Alchemist = Gatekeeper + Transformer linking them.

Pipeline Overview

1. Narrative Incubation (Gu Pot Issue)  
   Stage: larva / fermenting / distilled  
   Artifact: Gu Pot issue (logline, tension, stakes, irreversible shift, residue list)
2. Distillation Trigger  
   Condition: Issue reaches distilled; irreversible shift + measurable residue fields populated.  
   Output: Experiment Manifest Skeleton (seeded with hypothesis & expected metrics)
3. Experiment Execution (Runner – Stage 3)  
   Artifacts: manifest.json, run_metadata.json, metrics.json, logs.txt  
   Guarantee: Deterministic (seeded + reproducibility hash)
4. Validation / Promotion (Stage 4)  
   Process: Baseline comparison → hypothesis claims → validated/regression/anomaly classification  
   Artifacts: claims/hypotheses/*.json → claims/validated/*.json or claims/regressions/*.json
5. Report Synthesis (Stage 5)  
   Input: validated claim set  
   Output: report.md (actionable intel) + provenance block
6. Feedback to Narrative  
   Update Gu Pot issue: Evidence Links section + stage decision (serum / antitoxin / compost)

Role Boundaries

Gu Pot Maintainer:
- Curates narrative, clarifies stakes, defines irreversible shift

Alchemist (automation + human):
- Detects when narrative is “distilled”
- Generates manifest scaffold
- Tracks experimental lineage & claim provenance
- Enforces promotion gates

Key Gating Principle  
No claim is “serum” (fully validated narrative outcome) without: deterministic run lineage, baseline comparison, explicit evidence file references, recorded confidence method, origin binding back to Gu Pot issue.

Origin Binding Schema (attached to each claim)
```
"origin": {
  "type": "gu_pot",
  "issue_number": 123,
  "issue_url": "https://github.com/OWNER/REPO/issues/123",
  "stage_at_evaluation": "distilled",
  "logline_hash": "sha256:<64hex>",
  "tension_hash": "sha256:<64hex>",
  "irreversible_shift_declared": true,
  "extracted_on": "2025-09-06T02:55:00Z",
  "alchemist_version": "0.1.0"
}
```

Hashing Guidance
- Normalize text: trim, lowercase, collapse internal whitespace to single spaces before hashing.
- Use SHA-256; prefix with `sha256:`.

Directory & Trace Layout (Proposed)
```
gu_pot/
  issue-123/
    manifest_v1.json
    runs/
      2025-09-06T02-40-10Z/
        metrics.json
        run_metadata.json
    claims/
      hypotheses/
      validated/
      regressions/
    report/
      report_v1.md
```

State Machine Summary  
larva → fermenting → distilled → (experiments produce claim set)  
 distilled + validated improvements → serum  
 distilled + validated neutral defensive value → antitoxin candidate  
 any stage → compost (document cause + residue pointer)

Rationale for “Alchemist” Name  
Alchemy: transforms volatile narrative substrates into stable, analyzable compounds (claims). Fuses qualitative intuition with quantitative evaluation.

Future Hooks
- CI labeler: if Gu Pot moves to distilled, open scaffold PR
- Auto comment in issue when first validated claim arrives
- Dashboard: narrative progression vs empirical adoption ratio

Versioning  
Add `alchemist_version` to claim origin for auditability.

---
Version: 0.1.0