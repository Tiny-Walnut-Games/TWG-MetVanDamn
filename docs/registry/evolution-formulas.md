# 🧮 Pet Evolution Formulas Registry

*Public documentation of all scoring algorithms as mandated by the PET Charter*

## Base XP Values

### Contribution Types
```python
BASE_XP_VALUES = {
    "tldl_entry": 100,           # TLDL documentation entry
    "code_contribution": 75,      # Code commits with documentation
    "documentation_improvement": 50,  # README, guides, comments
    "bug_fix": 125,              # Confirmed bug resolution
    "feature_implementation": 150,   # New feature with tests
    "collaborative_session": 200,    # Pair programming, code review
    "test_creation": 90,         # Unit tests, integration tests
    "performance_optimization": 175, # Measurable performance improvement
    "security_improvement": 200,     # Security vulnerability fix
    "tutorial_creation": 120     # Educational content creation
}
```

## Quality Multipliers

### Quality Assessment
```python
QUALITY_MULTIPLIERS = {
    "excellent": 1.5,    # Exceptional quality, comprehensive documentation
    "good": 1.2,         # Above average, well documented
    "average": 1.0,      # Meets standards, adequate documentation
    "poor": 0.5,         # Below standards, minimal documentation
    "incomplete": 0.2    # Unfinished or inadequate contribution
}
```

### Quality Criteria
- **Excellent**: Complete documentation, tests, follows all conventions, innovative approach
- **Good**: Good documentation, tests present, follows most conventions
- **Average**: Basic documentation, some tests, follows basic conventions
- **Poor**: Minimal documentation, few/no tests, convention violations
- **Incomplete**: Missing essential components or documentation

## Collaboration Bonuses

### Team Work Multipliers
```python
COLLABORATION_BONUSES = {
    "solo": 1.0,         # Individual contribution
    "pair": 1.2,         # Pair programming or co-authored
    "team": 1.4,         # Team effort (3-5 contributors)
    "cross_team": 1.6,   # Cross-team collaboration
    "community": 1.8     # Open community contribution
}
```

## Archetype Diversity Rewards

### Multi-Archetype Benefits
```python
DIVERSITY_REWARDS = {
    "single_archetype": 1.0,     # One pet type
    "dual_archetype": 1.1,       # Two pet types
    "triple_archetype": 1.3      # All three pet types (max diversity)
}
```

## Final XP Calculation Formula

```python
def calculate_final_xp(base_contribution, quality_level, collaboration_type, num_archetypes):
    """
    Calculate final XP with all multipliers applied
    
    Args:
        base_contribution: Contribution type from BASE_XP_VALUES
        quality_level: Quality assessment from QUALITY_MULTIPLIERS
        collaboration_type: Collaboration level from COLLABORATION_BONUSES
        num_archetypes: Number of different pet archetypes owned
    
    Returns:
        Final XP amount (integer)
    """
    base_xp = BASE_XP_VALUES[base_contribution]
    quality_mult = QUALITY_MULTIPLIERS[quality_level]
    collab_mult = COLLABORATION_BONUSES[collaboration_type]
    
    # Determine diversity reward
    if num_archetypes >= 3:
        diversity_mult = DIVERSITY_REWARDS["triple_archetype"]
    elif num_archetypes == 2:
        diversity_mult = DIVERSITY_REWARDS["dual_archetype"]
    else:
        diversity_mult = DIVERSITY_REWARDS["single_archetype"]
    
    final_xp = int(base_xp * quality_mult * collab_mult * diversity_mult)
    
    return final_xp
```

## Evolution Thresholds

### Pet Level Requirements
```python
EVOLUTION_THRESHOLDS = {
    "scrollhound": {
        "puppy": 0,           # Starting level
        "tracker": 500,       # Documentation Tracker
        "guardian": 1500,     # Scroll Guardian
        "master": 4000        # Scrollmaster Hound
    },
    "chronocat": {
        "kitten": 0,          # Starting level
        "watcher": 600,       # Deadline Watcher
        "oracle": 1800,       # Temporal Oracle
        "master": 5000        # ChronoMaster Cat
    },
    "debugger_ferret": {
        "kit": 0,            # Starting level
        "hunter": 700,       # Bug Hunter
        "investigator": 2000, # Code Investigator
        "master": 6000       # Debugmaster Ferret
    }
}
```

## Chronicler Refactoring Costs

### XP Cost for Automatic Document Refactoring
*"Every refactor is a gamble: clarity gained, XP drained."* — **Faculty Doctrine, Vol. VII**

```python
REFACTORING_COSTS = {
    "minor_format_fix": 25,       # Missing punctuation, basic formatting
    "metadata_completion": 50,    # Missing Entry ID, Author, Context fields
    "section_restructure": 75,    # Missing/wrong sections (Objective, Discovery, etc.)
    "content_enhancement": 100,   # Placeholder text replacement, detailed improvements
    "comprehensive_rewrite": 150, # Complete document restructuring with Faculty standards
    "emergency_faculty_summon": 200  # Manual Copilot invocation for complex issues
}
```

### Refactoring Quality Multipliers
Different complexity levels require different XP investment:

```python
REFACTORING_QUALITY_COSTS = {
    "quick_fix": 1.0,            # Simple automated fixes
    "standard_refactor": 1.5,    # Normal Faculty compliance fixes
    "deep_refactor": 2.0,        # Complex structural improvements
    "copilot_assisted": 2.5      # Manual Copilot summoning with guidance
}
```

### Strategic XP Spending Calculation
```python
def calculate_refactoring_cost(base_cost, complexity, urgency_modifier=1.0):
    """
    Calculate final XP cost for document refactoring
    
    Args:
        base_cost: Base cost from REFACTORING_COSTS
        complexity: Quality multiplier from REFACTORING_QUALITY_COSTS
        urgency_modifier: Emergency multiplier (1.0-3.0)
    
    Returns:
        Final XP cost for refactoring operation
    """
    return int(base_cost * complexity * urgency_modifier)
```

## Anti-Gaming Measures

### Diminishing Returns System
```python
def apply_diminishing_returns(xp_amount, recent_activity):
    """
    Apply diminishing returns to prevent XP farming
    
    Args:
        xp_amount: Base XP to award
        recent_activity: Dict with activity counts
    
    Returns:
        Adjusted XP amount
    """
    daily_contributions = recent_activity.get('daily_count', 0)
    weekly_contributions = recent_activity.get('weekly_count', 0)
    same_type_today = recent_activity.get('same_type_today', 0)
    
    # Daily cap multiplier (reduces after 5 contributions per day)
    if daily_contributions > 5:
        daily_mult = max(0.3, 1.0 - (daily_contributions - 5) * 0.1)
    else:
        daily_mult = 1.0
    
    # Weekly cap multiplier (reduces after 25 contributions per week)
    if weekly_contributions > 25:
        weekly_mult = max(0.5, 1.0 - (weekly_contributions - 25) * 0.05)
    else:
        weekly_mult = 1.0
    
    # Same activity type limit (reduces after 3 of same type per day)
    if same_type_today > 3:
        type_mult = max(0.2, 1.0 - (same_type_today - 3) * 0.2)
    else:
        type_mult = 1.0
    
    # Apply all multipliers
    final_xp = int(xp_amount * daily_mult * weekly_mult * type_mult)
    
    return final_xp
```

### Activity Rate Limiting
- **Daily Limit**: Maximum 10 contributions counted per day
- **Type Diversity**: Only first 3 contributions of same type get full XP per day
- **Quality Gate**: All contributions must meet minimum quality threshold
- **Cooldown Period**: 15-minute cooldown between XP awards

## Ability Unlock System

### Archetype-Specific Abilities
```python
ABILITY_UNLOCK_LEVELS = {
    "scrollhound": {
        1: ["basic_documentation", "tldl_reading"],
        2: ["cross_reference", "link_validation", "basic_editing"],
        3: ["quality_assessment", "template_mastery", "collaborative_editing"],
        4: ["documentation_generation", "style_enforcement", "mentorship"]
    },
    "chronocat": {
        1: ["basic_scheduling", "deadline_awareness"],
        2: ["milestone_tracking", "velocity_monitoring", "burndown_analysis"],
        3: ["predictive_analysis", "risk_assessment", "timeline_optimization"],
        4: ["time_manipulation", "parallel_timeline_management", "temporal_mentorship"]
    },
    "debugger_ferret": {
        1: ["basic_logging", "simple_breakpoints"],
        2: ["advanced_debugging", "performance_profiling", "test_creation"],
        3: ["root_cause_analysis", "security_scanning", "optimization_suggestions"],
        4: ["quantum_debugging", "predictive_error_detection", "debugging_mentorship"]
    }
}
```

## Achievement Bonuses

### Special Achievement XP Rewards
```python
ACHIEVEMENT_BONUSES = {
    "first_pet": 50,                  # First pet adoption
    "tri_archetype_master": 1000,     # Master all three archetypes
    "documentation_sage": 500,        # 50 validated TLDL entries
    "bug_whisperer": 750,            # 100 confirmed bug fixes
    "collaboration_champion": 600,    # 25 collaborative sessions
    "mentor_badge": 800,             # Help 10 other developers
    "innovation_award": 1200,        # Create groundbreaking feature
    "security_guardian": 900         # Find and fix security issues
}
```

## Audit Functions

### Verification Methods
```python
def verify_xp_calculation(contribution_data):
    """Verify XP calculation matches public formula"""
    expected_xp = calculate_final_xp(
        contribution_data['type'],
        contribution_data['quality'],
        contribution_data['collaboration'],
        contribution_data['num_archetypes']
    )
    
    if contribution_data['recent_activity']:
        expected_xp = apply_diminishing_returns(
            expected_xp, 
            contribution_data['recent_activity']
        )
    
    return expected_xp == contribution_data['awarded_xp']

def audit_pet_evolution(pet_data):
    """Audit pet evolution against thresholds"""
    archetype = pet_data['archetype']
    current_xp = pet_data['xp']
    current_level = pet_data['level']
    
    thresholds = EVOLUTION_THRESHOLDS[archetype]
    
    # Check if pet should be at higher level
    for level, required_xp in thresholds.items():
        if current_xp >= required_xp:
            expected_level = level
    
    return expected_level == current_level
```

---

## Change Log

### Version 1.0.0 (2025-09-02)
- Initial formula definitions
- Base XP values established
- Quality and collaboration multipliers defined
- Anti-gaming measures implemented
- Evolution thresholds set

---

## Formula Updates

All formula changes require:
1. **RFC Process**: Public request for comments
2. **Community Review**: 14-day review period
3. **Migration Plan**: Automatic recalculation for affected pets
4. **Backward Compatibility**: Existing pets grandfathered when possible

---

*These formulas are open source and auditable. All calculations are deterministic and reproducible. For questions or proposed changes, create an issue with the `pet-formula` label.*