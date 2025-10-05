# RitualBot: The Living Dev Agent's Update Sentinel

> *"In the ancient art of dependency maintenance, only the wise automate what can be safely automated, while preserving the sacred rituals of human oversight."* — The Chronicle Keeper

## 🤖 The RitualBot Prophecy

RitualBot is the TWG Living Dev Agent's automated update system, designed to handle the endless scroll summons of dependency updates while maintaining the safety and wisdom that only comes from proper ritual oversight.

### The Three Sacred Phases

#### Phase 0: External Dependency Streams (Current Implementation)
**The Foundation Ritual** - Automated management of external dependencies through Dependabot integration.

**Components:**
- **Dependabot Configuration**: Weekly scheduled updates for Python (pip), JavaScript (npm), and GitHub Actions
- **Security Sentinel**: Daily security-only updates with auto-approval for critical patches
- **Auto-Merge Guardian**: Optional automated merging with safety controls and manual overrides
- **Label-Based Control System**: Granular control over merge behavior through sacred labels

**Status**: ✅ **IMPLEMENTED** - Active and protecting the realm

#### Phase 1: Internal Template Drift Detection (Future Vision)
**The Synchronization Spell** - Detection and automated PR generation for template evolution drift.

**Planned Components:**
- Template diff detection engine
- Automated update PR generation for template changes
- TLDA fragment emission for multi-repository synchronization
- Conflict resolution strategies for divergent customizations

**Status**: 📋 **PLANNED** - Awaiting Phase 0 stabilization

#### Phase 2: Multi-Repo Propagation (The Great Synchronization)
**The Network Effect Ritual** - Coordinated updates across the entire Living Dev Agent ecosystem.

**Planned Components:**
- Cross-repository dependency analysis
- Coordinated rollout strategies
- Impact assessment and rollback capabilities
- Community notification and approval workflows

**Status**: 🔮 **VISIONARY** - The distant future holds great promise

## 🏷️ Sacred Label System

The RitualBot responds to specific labels that control its behavior, much like ancient incantations:

### Primary Control Labels

| Label | Color | Effect | Use Case |
|-------|-------|--------|----------|
| `ritual-auto` | 🟢 Green (`28a745`) | **Grants auto-merge blessing** | Apply to trusted Dependabot PRs for automated merging |
| `no-automerge` | 🔴 Red (`d73a49`) | **Explicit protection barrier** | Block auto-merge regardless of other settings |
| `ritual-batch` | 🟡 Yellow (`ffd33d`) | **Future grouping semantics** | Reserved for Phase 1+ batch update coordination |

### Ecosystem Labels (Auto-Applied by Dependabot)

| Label | Color | Description |
|-------|-------|-------------|
| `dependencies` | 🔵 Blue (`0366d6`) | General dependency updates |
| `security` | 🔴 Red (`d73a49`) | Security-related updates (auto-tagged for daily runs) |
| `python` | 🟢 Green (`2ea043`) | Python/pip dependency updates |
| `javascript` | 🟡 Yellow (`ffd33d`) | Node.js/npm dependency updates |
| `github-actions` | 🔵 Blue (`2188ff`) | GitHub Actions workflow updates |

## 🛡️ Safety Mechanisms & Guardian Protocols

### The Three Pillars of Trust

1. **Actor Verification**: Only `dependabot[bot]` can trigger auto-merge workflows
2. **Explicit Consent**: Default behavior requires `ritual-auto` label (configurable)
3. **Escape Hatches**: `no-automerge` label provides absolute veto power

### Auto-Merge Prerequisites

Before RitualBot will bless a PR with auto-merge:

- ✅ **Actor Check**: Must be created by `dependabot[bot]`
- ✅ **Label Check**: Must have `ritual-auto` label (or `DEFAULT_AUTOMERGE=true`)
- ✅ **Blocking Check**: Must NOT have `no-automerge` label
- ✅ **CI Status**: All checks must pass (no failures, no pending)
- ✅ **Merge State**: No conflicts, not in draft mode
- ✅ **Review Status**: Follows repository protection rules

### Merge Strategy

- **Default Method**: Squash merge for clean history
- **Commit Messages**: Preserved from Dependabot with emoji prefixes
- **Branch Cleanup**: Automatic deletion after merge

## ⚙️ Configuration & Customization

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `DEFAULT_AUTOMERGE` | `false` | Enable auto-merge without `ritual-auto` label |

### Repository Configuration

RitualBot respects your repository's branch protection rules and never bypasses security measures. It works within the existing permission structure.

### Opting Out of Auto-Merge

**Per PR**: Add `no-automerge` label to any Dependabot PR  
**Permanently**: Set `DEFAULT_AUTOMERGE=false` and never add `ritual-auto` labels  
**Temporarily**: Remove RitualBot's permissions or disable the workflow

## 📊 Operational Metrics

### Update Cadence

- **Weekly Updates**: Every Monday at 09:00 UTC for routine dependency maintenance
- **Daily Security Scans**: Immediate security patches with `ritual-auto` pre-approval
- **On-Demand**: Manual workflow dispatch available for emergency updates

### Impact Assessment

Track RitualBot's effectiveness through:
- Reduced manual PR review overhead
- Faster security patch deployment
- Maintained test suite stability
- Developer satisfaction with automation levels

## 🔮 Future Phase Preparation

### Phase 1 Ready Configuration

A skeleton configuration file exists at `.github/ritualbot-config.json` to prepare for template synchronization capabilities:

```json
{
  "version": "0.1.0",
  "enable_template_sync": false,
  "template_source": "",
  "reviewers": [],
  "phase1_ready": false
}
```

### Migration Path

When Phase 1 becomes available:
1. Update `ritualbot-config.json` with your template source
2. Enable template sync monitoring
3. Configure review workflows for template changes
4. Test with dry-run mode before full automation

## 🎭 The Living Philosophy

RitualBot embodies the Living Dev Agent's core principle: **Automate the predictable, preserve human judgment for the exceptional.**

- **Routine Updates**: Let the bot handle the daily grind of dependency maintenance
- **Security Patches**: Trust but verify with expedited automated processing
- **Major Changes**: Human wisdom still guides significant architectural decisions
- **Emergency Responses**: Rapid deployment capabilities when the realm is threatened

## 📚 References & Lore

- [Dependabot Configuration Documentation](https://docs.github.com/en/code-security/dependabot)
- [GitHub Actions Auto-merge Best Practices](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/incorporating-changes-from-a-pull-request/automatically-merging-a-pull-request)
- [TWG Living Dev Agent Manifesto](../MANIFESTO.md)
- [Security Runbooks](../Security-Runbooks.md)

---

*"The greatest magic is not in the power to control, but in the wisdom to know when to step back and let the ritual flow."* — Ancient Developer Proverb

**Scroll Keepers**: This document lives at `docs/lore/ritual_updates.md` and should be updated as RitualBot evolves through its phases. Each phase implementation should add its own chapter to this living chronicle.