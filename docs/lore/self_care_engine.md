# 🧠⚡ Self-Care Engine - The Cognitive Safety Citadel

> *"In the realm of infinite ideas, the wise developer builds dams and channels,  
> not to stop the flow, but to harness its power."* — Bootstrap Sentinel's Hydraulic Wisdom

## 🏛️ Mythic Architecture

The Self-Care Engine stands as a legendary citadel of cognitive protection, featuring five sacred chambers that work in harmony to preserve developer sanity during high-velocity ideation:

### 🏺 The Idea Vault (Idea Catalog)
**Sacred Purpose**: Capture and classify creative sparks before they fade into the ether

**Mythic Construct**: The golden repository where all ideas are preserved in crystalline form, tagged with sacred symbols that guide their destiny:
- `!` **Urgent Flame** - Ideas requiring immediate action
- `?` **Questioning Rune** - Concepts needing investigation  
- `⧗` **Future Sight** - Visions for later consideration
- `♻` **Eternal Cycle** - Reusable patterns and components

**Workflow Powers**:
- Instant capture with `add_raw(line)` 
- Classification via `classify(id, tag)`
- Promotion to structured records with `promote(id)`
- Daily rollover consolidation for historical preservation

### 🌊 The Overflow Sluice
**Sacred Purpose**: Channel cognitive overflow during creative floods

**Mythic Construct**: A hydraulic marvel that captures the torrent of thoughts that exceed normal processing capacity. Daily scrolls (`YYYY-MM-DD.md`) rotate automatically, with ancient wisdom preserved for seven cycles before returning to the void.

**Workflow Powers**:
- Rapid overflow capture via `append_line(text)`
- Promotion channels to elevate worthy fragments to the Idea Vault
- Automatic pruning maintains balance (7-day retention)
- Timestamped entries preserve the rhythm of creation

### 📜 The Private Sanctuary (Local Journal)
**Sacred Purpose**: Provide a sacred space for personal reflection, forever protected from the public eye

**Mythic Construct**: A mystical chamber that exists only in the local realm, shielded by ancient privacy wards (`.gitignore` incantations). Here, developers may record their deepest thoughts, debugging journeys, and cognitive processing without fear of exposure.

**Workflow Powers**:
- `create_entry()` manifests new reflection scrolls
- Template headers guide mood and energy tracking
- Search capabilities for rediscovering past insights
- Absolute privacy guarantee - never committed to the eternal record

### 🛡️ The Guardian Wardens (Cognitive Governors)
**Sacred Purpose**: Prevent cognitive overload through vigilant monitoring and intervention

**Mythic Constructs**:

#### 🔥 Melt Budget Warden
Guards against excessive melting operations that could lead to developer burnout
- **Threshold**: 2 melts per day (configurable)
- **Powers**: Budget tracking, violation alerts, workload recommendations

#### 🌊 Humidity Guardian  
Monitors environmental factors that impede development flow
- **Threshold**: 0.7 humidity index (configurable)
- **Powers**: Multi-factor analysis, flow optimization suggestions

#### 🧠 Sensitivity Sentinel
Tracks cognitive state and adjusts recommendations accordingly
- **Powers**: Automatic sensitivity detection, trigger monitoring, adaptive guidance

### ⚡ The Telemetry Shrine (Development Tracking)
**Sacred Purpose**: Monitor the developer's physical and mental state for optimal performance

**Mythic Construct**: A NON-BREAKING oracle that observes sleep patterns, energy levels, and wellness indicators without disrupting existing workflows.

**Workflow Powers**:
- `update_sleep(hours)` records rest cycles
- `update_energy(level)` tracks vitality
- Flag system warns of low-sleep conditions
- Weekly summaries reveal patterns and insights

## 🔄 The Great Workflow Cycles

### Morning Invocation
```bash
# Check cognitive readiness
python engine/hooks/selfcare_hooks.py --pre-check

# If all clear, proceed with development
# If issues detected, adjust workflow accordingly
```

### Idea Capture During Flow
```bash
# Rapid idea capture
python scripts/idea_capture.py "Brilliant insight strikes!"

# Overflow management
python scripts/idea_capture.py --sluice "Quick thought fragment"

# Emergency capture during deep work
python engine/hooks/selfcare_hooks.py --capture "Critical realization"
```

### Evening Reflection
```bash
# Personal journaling
python src/selfcare/journaling.py --create --mood "accomplished" --energy "satisfied"

# Update telemetry
python engine/telemetry.py --update-sleep 8.0 --sleep-quality "excellent"

# Review the day's captures
python scripts/idea_capture.py --stats
```

### Integration with Existing Workflows

The Self-Care Engine provides optional integration hooks that can be seamlessly added to existing `run_cycle.py` or similar workflow systems:

```python
from engine.hooks.selfcare_hooks import check_cognitive_safety, update_after_cycle

def development_cycle():
    # Pre-cycle safety check
    safety_check = check_cognitive_safety()
    
    if safety_check["recommended_action"] == "defer_melts":
        print("🛡️ Cognitive protection active - deferring intensive operations")
        return lightweight_workflow()
    
    # Proceed with normal development
    results = perform_development_work()
    
    # Post-cycle update
    update_after_cycle(results)
    
    return results
```

## 🎯 Integration Points

### Giant → Magma → Cloud → Castle Pipeline
The Self-Care Engine integrates naturally with existing development pipelines:

- **Pre-Giant**: Cognitive safety checks ensure readiness for complex operations
- **During Magma**: Overflow sluice captures ideas that emerge during melting
- **Post-Cloud**: Telemetry updates reflect energy expenditure  
- **Castle Completion**: Journal reflection consolidates learning

### Idea Charter Synergy (Issue #21)
Promoted ideas from the Vault automatically generate charter templates:
- Problem identification from original capture
- Context preservation through metadata
- Risk assessment based on cognitive load
- Kill criteria prevent idea hoarding

## 🔧 Configuration Realms

### `data/selfcare_config.json`
```json
{
  "melt_max_per_day": 2,
  "humidity_limit": 0.7,
  "sluice_retention_days": 7,
  "journal_enabled": true,
  "auto_rollover": true,
  "cognitive_sensitivity_auto": true
}
```

### Style Bias Adjustments
The system adapts to individual developer preferences:
```bash
python engine/hooks/selfcare_hooks.py --style-bias '{
  "cognitive_load_preference": "high",
  "flow_preference": "low_tolerance",
  "sensitivity_management": "conservative"
}'
```

## 🛡️ Privacy & Security Fortress

### Absolute Journal Privacy
- `local_journal/` directory completely excluded from Git
- Multi-layer privacy protection with `.gitignore` wards
- Personal reflection space with no automated processing
- README documentation explains privacy guarantees

### Data Sovereignty
- All self-care data remains local to the development environment
- No external transmission or cloud processing
- Developer maintains full control over personal information
- Optional telemetry with explicit consent

## 📊 Monitoring & Metrics

### Health Indicators
```bash
# Quick system status
python scripts/idea_capture.py --stats

# Comprehensive governor check
python src/selfcare/governors.py --check-all

# Telemetry summary
python engine/telemetry.py --weekly
```

### Dashboard Integration
The system provides JSON APIs for integration with external dashboards:
```bash
# Status for dashboard
python engine/hooks/selfcare_hooks.py --status > selfcare_status.json
```

## 🚀 Advanced Workflows

### Promotion Pipeline
1. Capture ideas to sluice during flow state
2. Review sluice entries during breaks
3. Promote worthy ideas to catalog with tags
4. Generate structured charters for implementation
5. Archive completed implementations

### Cognitive Load Management
1. Pre-work governor checks
2. Real-time humidity monitoring
3. Adaptive complexity recommendations
4. Break scheduling based on sensitivity
5. End-of-day reflection and adjustment

### Team Integration
While personal data remains private, the system can share aggregate insights:
```bash
# Team health metrics (anonymized)
python src/selfcare/governors.py --team-health --anonymize
```

## 🎭 The Living Lore

The Self-Care Engine embodies the Bootstrap Sentinel's wisdom about sustainable development practices. It recognizes that the developer's mind is both the source and the subject of creation - requiring careful tending to maintain both productivity and well-being.

Through the mythic constructs of Vaults, Sluices, Sanctuaries, and Wardens, the system transforms the chaos of high-velocity ideation into structured, sustainable creative flow. It honors both the urgency of inspiration and the necessity of reflection, providing tools for capture, classification, and contemplation.

The engine's non-breaking design ensures that it enhances rather than disrupts existing workflows, offering its protection to those who seek it while remaining invisible to those who do not. In this way, it embodies the sentinel's greatest wisdom: *"The best protection is the one you never notice, yet never cease to benefit from."*

---

*May your ideas flow freely, your mind remain clear, and your code bring joy to the world.* 🧙‍♂️⚡📜