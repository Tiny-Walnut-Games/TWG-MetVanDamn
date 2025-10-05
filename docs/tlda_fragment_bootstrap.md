# TLDA Fragment Bootstrap Documentation

## Overview

This document describes the 100 TLDA (Tiny Living Dev Agent) fragments generated to seed the Warbler Cloud system for testing Giant compression, evaporation, and selector synthesis.

## Fragment Format

Each TLDA fragment follows the specified format:

```json
{
  "id": "TLDA-MOCK-###",
  "source": "Seeder",
  "text": "Narrative line here...",
  "emotional_weight": 0.3–0.9,
  "tags": ["tag1", "tag2", ...],
  "unix_millis": timestamp,
  "provenance_hash": "sha256 of source+text+timestamp"
}
```

## Content Categories

The fragments draw inspiration from various aspects of Tiny Walnut Games project lore:

### 1. Validator Rituals (10 fragments)
- Scroll integrity checks
- Section naming validation
- Debug overlay health scoring  
- Chronicle Keeper validation
- Expected behavior confirmations

### 2. Giant & Magma System (10 fragments)
- Giant stomping events
- Sediment compression
- Molten glyph processing
- Magma overflow scenarios
- Tetrino slam operations

### 3. Castle Memory Events (10 fragments)
- Memory node operations
- Corridor navigation
- Graph expansion
- Room rotation based on heat
- Architectural integrity

### 4. Faculty Onboarding (10 fragments)
- TLDA initiation rituals
- Contributor breakthroughs
- Badge verification processes
- Oracle ascension events
- Onboarding failures

### 5. Pets & Local Agents (10 fragments)
- Pet evolution events
- Agent telemetry
- Badge pet system
- Cognitive overflow detection
- Efficiency measurements

### 6. Chronicle Automation (10 fragments)
- Capsule scroll generation
- Archive continuity rituals
- Lore integration
- Temporal paradoxes
- Monthly generation

### 7. DevTimeTravel System (10 fragments)
- DTT Vault operations
- Snapshot restoration
- Content addressing
- Layer promotion
- Integrity verification

### 8. Warbler Cloud (10 fragments)
- Humidity management
- Mist line generation
- Style bias calibration
- Cloud formation anomalies
- Evaporation optimization

### 9. Unity Integration (10 fragments)
- Editor tool crashes
- Assembly validation
- Image workflow issues
- Version compatibility
- Metadata parsing

### 10. Licensing Doctrine (10 fragments)
- Compliance audits
- License validation
- Header injection
- Attribution scanning
- CLA verification

## Emotional Weight Distribution

- **Range**: 0.22 - 0.90
- **Average**: 0.565
- **Distribution**: Spread across the full range with emphasis on mid-range values (0.4-0.7)

## Tag Analysis

- **Total Unique Tags**: 105
- **Most Common Tags**:
  1. chronicle (14 occurrences)
  2. castle (13 occurrences)
  3. faculty (11 occurrences)
  4. licensing (11 occurrences)
  5. unity (11 occurrences)

## Provenance Security

Each fragment includes a SHA-256 provenance hash generated from:
`SHA-256(source + text + timestamp)`

This ensures fragment integrity and provides cryptographic verification of origin.

## Integration Test Results

✅ **Successfully processed**: 100 TLDA fragments
🌋 **Molten glyphs created**: 1 (clustering all fragments)
☁️ **Mist lines generated**: 1 
🏰 **Castle nodes created**: 1
⚖️ **Governance score**: 0.50 (baseline)

## File Locations

- **Fragment Data**: `data/tlda_fragments.json`
- **Generation Script**: `scripts/tlda_fragment_seeder.py`
- **Test Script**: `scripts/test_tlda_fragments.py`
- **Documentation**: `docs/tlda_fragment_bootstrap.md`

## Usage Examples

### Generate new fragments:
```bash
python3 scripts/tlda_fragment_seeder.py --count 100
```

### Test fragments with Warbler Cloud:
```bash
python3 scripts/test_tlda_fragments.py
```

### Generate smaller test set:
```bash
python3 scripts/tlda_fragment_seeder.py --count 25 --output data/test_fragments.json
```

## Next Steps

1. **Enhanced Clustering**: Implement semantic clustering for better Giant compression
2. **Embedding Integration**: Add vector embeddings for similarity-based processing
3. **Persistent Storage**: Integrate with production storage backends
4. **Quality Metrics**: Develop fragment quality assessment tools
5. **Multi-voice Synthesis**: Expand selector capabilities for varied response generation

## Notes

- Fragments are designed to simulate realistic development events and emotional states
- The current Giant compressor uses naive clustering (all fragments → 1 cluster)
- Future enhancements will implement semantic similarity clustering
- Fragments serve as bootstrap data for initial Warbler Cloud training

---

**Generated**: January 2025  
**Fragment Count**: 100  
**Ready for Warbler Cloud Bootstrap**: ✅