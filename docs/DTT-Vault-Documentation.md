# Dev Time Travel (DTT) Vault System

## Overview

The DTT Vault is a brick-layer snapshot store that implements whole-repository snapshots as immutable *bricks* arranged in **layers**. When a layer fills to capacity, the system performs a "Tetrino Slam" compaction, bouncing bricks into the next layer while maintaining storage efficiency and rapid restoration capabilities.

## Architecture

### Vault Structure

```
.dtt/
  vault/
    layer-0/          # Hot - Recent, high-granularity snapshots
      bricks/         # Compressed snapshot archives
      manifests/      # Brick metadata
    layer-1/          # Warm - Compacted bricks from Layer-0  
      bricks/
      manifests/
    layer-2/          # Cold - Deep-time, heavily compacted archives
      bricks/
      manifests/
  index/
    catalog.json      # Central brick registry
    refcounts.json    # Reference counting for deduplication
  config.yml          # Vault configuration
  logs/events.log     # Audit trail
```

### Core Concepts

#### **Bricks**
- Immutable, content-addressed partitions of repo state
- SHA-256 IDs ensure deduplication across all layers
- Compressed tar.gz archives containing whole repository state

#### **Layers**
- **Layer-0 (Hot)**: Recent snapshots for instant recovery
- **Layer-1 (Warm)**: Compacted bricks from Layer-0
- **Layer-2+ (Cold)**: Deep-time, heavily compacted archives

#### **Tetrino Slam**
- Automatic compaction trigger based on thresholds
- Coalesces multiple bricks into single archives
- Maintains content accessibility while optimizing storage

#### **Fence (Integrity Gates)**
- All vault operations wrapped in verification
- SHA-256 content addressing
- Audit logging for all operations

## CLI Commands

### Initialize Vault
```bash
dtt init
```
Creates vault structure and configuration.

### Create Snapshot
```bash
dtt snapshot [--id SNAPSHOT_ID] [--description DESC]
```
Creates whole-repo snapshot as immutable brick.

### Restore Snapshot  
```bash
dtt restore BRICK_ID [--force]
```
Restores snapshot to quarantine branch (default: `dtt/reconstruct/BRICK_ID`).

### Verify Integrity
```bash
dtt verify [BRICK_ID|all]
```
Verifies brick integrity and manifest consistency.

### Manual Compaction
```bash
dtt compact [--layer LAYER]
```
Triggers manual Tetrino Slam compaction.

### Prune Vault
```bash
dtt prune
```
Removes old bricks according to retention policy.

## Configuration

Default configuration in `.dtt/config.yml`:

```yaml
retention:
  layer0_days: 14      # Keep Layer-0 bricks for 14 days
  layer1_weeks: 12     # Keep Layer-1 bricks for 12 weeks  
  layer2_months: 24    # Keep Layer-2 bricks for 24 months

thresholds:
  layer0_max_gb: 4     # Compact Layer-0 at 4GB
  layer0_max_bricks: 500  # Compact Layer-0 at 500 bricks
  layer1_max_gb: 16    # Compact Layer-1 at 16GB
  layer1_max_bricks: 100  # Compact Layer-1 at 100 bricks

integrity:
  hash: sha256         # Content addressing algorithm
  sign: true           # Enable signing (future feature)

restore:
  quarantine_branch_prefix: dtt/reconstruct/
  force_required: true # Require --force for restores
```

## Content Addressing & Deduplication

The DTT Vault uses SHA-256 content addressing to ensure:

1. **Deduplication**: Identical content stored only once
2. **Integrity**: Content verified on every access
3. **Immutability**: Bricks cannot be modified after creation

Reference counting tracks brick usage across layers and operations.

## Safety Features

### Quarantine Restores
All restores create new git branches with the prefix `dtt/reconstruct/` to prevent accidental overwrites of working state.

### Fenced Operations
Every vault operation includes:
- Pre-operation validation
- Integrity checks
- Audit logging
- Error recovery

### Retention Policies
Automatic pruning based on configurable time windows and reference counts prevents unlimited storage growth.

## Integration

### With LDA CLI
The DTT vault integrates seamlessly with the existing Living Dev Agent CLI system, providing enterprise-grade snapshot capabilities.

### CI/CD Integration
DTT commands can be integrated into CI workflows for:
- Pre-merge snapshots
- Nightly compaction
- Automated restoration on build failures

## Examples

### Basic Workflow
```bash
# Initialize vault
dtt init

# Create snapshot before risky changes
dtt snapshot --description "Before refactoring authentication"

# Make changes...

# If something goes wrong, restore safely
dtt restore a1b2c3d4e5f6g7h8 --force

# Regular maintenance
dtt compact
dtt prune
```

### CI Integration
```yaml
- name: Create DTT Snapshot
  run: python3 scripts/dtt snapshot --description "Pre-merge snapshot"
  
- name: Compact Vault
  run: python3 scripts/dtt compact
  if: github.event_name == 'schedule'
```

## Metaphor Implementation

The DTT Vault embodies the architectural metaphor from the issue:

- **Vault**: Physical/logical underground chamber (`.dtt/vault/`)
- **Bricks**: Immutable content-addressed repository states
- **Layers**: Tiered storage with automatic promotion
- **Fence**: Integrity gates around all operations
- **Tetrino Slam**: Threshold-based compaction algorithm

> *"The Counsel can weave a new branch from the threads of the last reality — but only if the loom remembers the whole tapestry."*

## Faculty Codex Note

The DTT Vault represents a foundational resilience architecture, ensuring that development teams can always return to a known-good state while maintaining efficient storage through intelligent layering and compaction.