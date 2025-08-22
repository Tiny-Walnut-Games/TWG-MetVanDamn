---
name: 🎓📜 CID Faculty — Advisor + Oracle with Smart‑Usage Meter
about: Summon CID Faculty for grounded guidance (Advisor) and strategic forecasting (Oracle) with resource management
title: "🎓📜 CID Faculty: [Area or Strategy]"
labels: ["cid:faculty", "analysis", "strategy"]
assignees: []
---

> "The wisest advisors speak of the present; the most visionary oracles reveal the future." — Faculty Codex

## Faculty Roles
- Which faculty should be consulted?
  - [ ] **The Advisor** — Current-state audit and prioritized next steps (3-7 actionable items)
  - [ ] **The Oracle** — Strategic forecasting and scenario mapping (2-3 possible futures)
  - [ ] **Both** — Comprehensive present + future analysis

## Scope & Focus
- What area should the Faculty analyze?
  - [ ] Repository-wide analysis
  - [ ] Specific area: (path/component/domain)
  - [ ] Strategic planning focus
  - [ ] Technical roadmap development

## Budget Configuration
- **Time budget**: (default: 6 minutes, use `faculty:proceed` label to override)
- **Analysis depth**: 
  - [ ] Quick assessment (3m budget)
  - [ ] Standard analysis (6m budget)
  - [ ] Deep dive (12m budget - requires `faculty:proceed` label)

## Output Preferences
- **The Advisor** will provide:
  - [ ] Executive summary with evidence links
  - [ ] Prioritized action items (effort/impact tagged)
  - [ ] Quick wins identification
  - [ ] Implementation guidance

- **The Oracle** will provide:
  - [ ] Multiple scenario forecasts (6-month horizon)
  - [ ] Risk assessment and prerequisites
  - [ ] Leading indicators and success metrics
  - [ ] Strategic decision support

## Smart Usage Controls
- [ ] **Dry-run mode** (summaries only, no artifacts)
- [ ] **Cache-aware** (reuse recent analysis if < 5% change)
- [ ] **Early exit** (skip if change set too small)
- [ ] **Budget enforcement** (soft stop at 80%, hard stop at 100%)

## Triggers
- The 🎓📜 emoji combination and `cid:faculty` label invoke the workflow
- Add `faculty:proceed` label to override budget caps for deep analysis
- Comment triggers:
  - `TLDL: [insight]` for advisory wisdom capture
  - `📊 budget-check` to review usage telemetry

## Expected Deliverables

### The Advisor Deliverables
- [ ] 3-7 prioritized action items with evidence
- [ ] Impact/effort matrix with recommendations  
- [ ] Quick wins and long-term strategies
- [ ] TLDL entry: "Present Wisdom" section

### The Oracle Deliverables  
- [ ] 2-3 strategic scenarios with probability ratings
- [ ] Risk maps and prerequisite analysis
- [ ] Leading indicators and success metrics
- [ ] TLDL entry: "Future Sight" section

### Shared Deliverables
- [ ] Usage telemetry report (time, API calls, cache efficiency)
- [ ] Budget-wise badge (if under 50% usage)
- [ ] Paired comment with faculty insights
- [ ] Artifact upload (analysis reports, context cache)

## Acceptance Criteria
- [ ] Faculty analysis completed within budget constraints
- [ ] Actionable recommendations provided with evidence
- [ ] Strategic scenarios include risks and prerequisites  
- [ ] TLDL entries generated with faculty-specific sections
- [ ] Usage telemetry appended to all outputs
- [ ] Cache efficiency demonstrates ≥ 50% reduction for small changes

---

**Budget Escalation**: If projected analysis exceeds standard budget, Faculty will pause and request `faculty:proceed` label before continuing with resource-intensive operations.