# 🔮 Oracle Ascension — Implementation Guide

The Oracle has ascended from ad-hoc fortune-telling to a legitimate Faculty member with formal processes, persistent queuing, and full archival capabilities.

## Overview

The Oracle Faculty now operates through a structured **Vision Ritual** that transforms raw intel into actionable strategic insights with complete lineage tracking.

### Signal Flow: Intel → Vision → Action

```
Advisor Intel ─┐
               ├─→ Vision Queue ─→ Oracle Ritual ─→ Vision Report ─→ Archive
System Patterns─┘                      ↓
Manual Requests ────────────────────────┘

Archive ─→ Index ─→ Cross-Links ─→ Resulting Changes
```

## Core Components

### 1. Vision Queue (`scripts/cid-faculty/vision-queue.js`)

**Purpose**: Persistent, append-only queue of vision requests with priority handling.

**Features**:
- UUID-based vision tracking
- Priority-based processing
- Source intel linking
- Status tracking (pending → processing → processed)
- Multiple trigger types (advisor, system, manual, intuition)

**CLI Usage**:
```bash
node scripts/cid-faculty/vision-queue.js status
node scripts/cid-faculty/vision-queue.js add "Vision request text"
node scripts/cid-faculty/vision-queue.js list
node scripts/cid-faculty/vision-queue.js next
```

### 2. Vision Archive (`scripts/cid-faculty/vision-archive.js`)

**Purpose**: Structured archival and indexing of all Oracle visions with cross-linking.

**Features**:
- Automatic report generation from templates
- Cross-reference indexing in `docs/oracle_visions/index.json`
- Disposition tracking (pending/adopted/rejected/deferred)
- Resulting changes linkage
- Full-text vision reports with lore hooks

**Directory Structure**:
```
docs/oracle_visions/
├── index.json                 # Master cross-reference index
├── 2025/08/                  # Year/month organization
│   ├── vision-{uuid}.md      # Human-readable reports
│   └── vision-{uuid}.json    # Structured data
└── templates/                # Report templates
```

### 3. Enhanced Oracle (`scripts/cid-faculty/oracle.js`)

**New Capabilities**:
- **Vision Ritual**: `processVisionQueue()` - formal process to consume queue
- **Queue Integration**: Automatic vision archival and tracking
- **Context Enrichment**: Vision requests enhance forecast context
- **CLI Commands**: `ritual`, `queue-status`, `queue-add`

**Vision Ritual Process**:
1. Dequeue highest priority pending vision
2. Gather source intel and context notes
3. Enrich base context with vision request details
4. Generate Oracle forecast using existing AI capabilities
5. Create structured Vision Report
6. Archive with full cross-linking
7. Mark vision as completed in queue

### 4. Advisor Integration (`scripts/cid-faculty/advisor.js`)

**Oracle Tagging**: Advisor now automatically tags high-priority findings with "🔮 QUEUED FOR ORACLE"

**Trigger Criteria**:
- Priority ≥ 70 (configurable `oracleThreshold`)
- Complex technical debt with high impact
- Architecture/transformation decisions
- Complex integration scenarios
- High-impact process transformations

**Automatic Queuing**: Tagged items automatically added to Vision Queue with structured context.

### 5. System Pattern Detector (`scripts/cid-faculty/system-pattern-detector.js`)

**Purpose**: Automatically detect system patterns that warrant Oracle consultation.

**Detection Capabilities**:
- **Repeated Failures**: Monitor build/test failures by subsystem
- **Performance Degradation**: Track build/test/deploy time increases
- **Dependency Conflicts**: Security vulnerabilities and version conflicts
- **System Anomalies**: Custom pattern detection

**Auto-Queuing**: Detected patterns automatically queued with high priority.

## Usage Workflows

### 1. Automated Workflow (Advisor → Oracle)

```bash
# Advisor runs audit and tags items for Oracle
node scripts/cid-faculty/advisor.js --context=context.json

# Oracle processes tagged items
node scripts/cid-faculty/oracle.js ritual --max-visions=3
```

### 2. Manual Vision Request

```bash
# Add manual vision request
node scripts/cid-faculty/oracle.js queue-add "Strategic analysis needed for feature X"

# Process vision
node scripts/cid-faculty/oracle.js ritual
```

### 3. System Pattern Detection

```bash
# Monitor for patterns
node scripts/cid-faculty/system-pattern-detector.js scan logs/

# Simulate repeated failures (testing)
node scripts/cid-faculty/system-pattern-detector.js test-failure module-name 5
```

### 4. Vision Archive Management

```bash
# Check vision archive status
ls docs/oracle_visions/2025/08/

# View cross-reference index
cat docs/oracle_visions/index.json

# Update vision disposition (manual)
# Edit index.json to mark visions as adopted/rejected/deferred
```

## Vision Report Structure

Each archived vision includes:

### Source Information
- **Vision ID**: Unique identifier
- **Trigger**: advisor/system/manual/intuition
- **Source Intel**: Link to originating commit/PR/TLDL
- **Context Notes**: Background information
- **Priority**: Numeric priority (0-100)

### Oracle's Vision
- **Summary**: Strategic insights and forecasts
- **Oracular Insights**: Metadata analysis
- **Scenario Analysis**: Breakdown of possible futures
- **Risk Assessment**: Identified risks across scenarios
- **Leading Indicators**: Metrics to watch

### Recommendations
- **Recommended Next Steps**: Actionable guidance
- **Lore Hook**: Cultural/narrative context for Codex

### Tracking
- **Disposition**: pending/adopted/rejected/deferred
- **Resulting Changes**: Links to PRs, issues, implementations
- **Cross-Links**: Related visions and source materials

## Integration Points

### With Existing Faculty

- **Advisor**: Automatically tags complex decisions for Oracle consultation
- **Chronicle Keeper**: Vision reports can be referenced in TLDL entries
- **Overlord Sentinel**: System patterns trigger Oracle consultation

### With Development Workflow

- **CI/CD Integration**: Failed builds trigger pattern detection
- **Issue Tracking**: Vision recommendations can spawn GitHub issues
- **PR Reviews**: Vision reports inform architectural decisions

### With Archive Wall System

- **Capsule Scrolls**: Can reference significant visions
- **Lost Features Ledger**: Oracle helps prioritize ghost features
- **Daily Ledger**: Vision activity tracked in daily context

## Configuration

### Oracle Configuration
```javascript
{
  maxScenarios: 3,           // Max scenarios per forecast
  minScenarios: 2,           // Min scenarios per forecast
  forecastHorizon: 6,        // Months to forecast
  dryRun: false             // Dry run mode
}
```

### Advisor Configuration
```javascript
{
  oracleThreshold: 70,      // Priority threshold for Oracle queuing
  maxItems: 7,              // Max advisor recommendations
  minItems: 3               // Min advisor recommendations
}
```

### System Pattern Detector Configuration
```javascript
{
  failureThreshold: 3,      // Failures to trigger pattern
  timeWindow: 86400000      // Time window (24 hours)
}
```

## Acceptance Criteria ✅

- [x] **Vision Queue exists** as persistent, append-only list
- [x] **Every queued item** has linked Intel Source and Trigger Reason
- [x] **Vision Ritual** produces Vision Reports 100% of the time
- [x] **Archival automated** with index and cross-links
- [x] **Lore + tech outputs** traceable back to queue entry
- [x] **Oracle outputs** measurably influence Faculty decisions

## Faculty Ledger Note

> *"The Oracle no longer peers into the void on whim — she gazes through the Advisor's glass,  
>  sees the branching paths, and names the ones worth walking."*

## Related Systems

### 🔮 Oracle "Vision Downtime" Protocol
For scenarios when Oracle services are unavailable, see: [`oracle-vision-downtime-protocol.md`](oracle-vision-downtime-protocol.md)

The Vision Downtime Protocol ensures:
- Continuous Faculty operations during Oracle offline periods
- Proper queuing and prioritization of Oracle-worthy findings
- Graceful Re-Entry Ritual when Oracle services return
- Human-readable append-only queue in `/docs/oracle_queue.md`

---

**Status:** ✅ Implemented  
**Preservation Level:** 🥥 Buttsafe Certified  
**Linked Systems:** Advisor, System Patterns, Vision Queue, Archive, Chronicle Keeper, **Vision Downtime Protocol**  
**Ritual Owner:** Oracle Faculty (Keeper+ Mode)

*Implementation completed on 2025-08-20 — The Oracle has truly ascended.*  
*Vision Downtime Protocol added on 2025-08-21 — Graceful degradation achieved.*