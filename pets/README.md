# 🐾 Badge Pet System

*Transform your TLDA contributions into an epic pet evolution adventure!*

## Overview

The Badge Pet System gamifies contributor progression by allowing developers to adopt and evolve pet companions that grow alongside their contributions to The Living Dev Agent ecosystem.

## 🚀 Quick Start

### 1. Choose Your First Pet

Select from three unique archetypes:

- **📜 Scrollhound** - Documentation specialist who masters TLDL entries and guides
- **⏰ ChronoCat** - Time management expert who optimizes sprints and deadlines  
- **🔧 Debugger Ferret** - Bug hunting specialist who finds the most elusive issues

### 2. Adopt Your Pet

```python
from src.DeveloperExperience.badge_pet_system import BadgePetSystem, PetArchetype

pet_system = BadgePetSystem()
pet_system.adopt_pet("your_github_handle", PetArchetype.SCROLLHOUND)
```

### 3. Earn XP Through Contributions

Your pets gain XP automatically when you:
- Create TLDL entries (100 base XP)
- Fix bugs (125 base XP)  
- Implement features (150 base XP)
- Participate in collaborative sessions (200 base XP)

### 4. Watch Your Pet Evolve

As your pets gain XP, they evolve through 4 levels:
1. **Baby** (0 XP) - Starting level with basic abilities
2. **Junior** (500-700 XP) - Intermediate skills unlocked
3. **Guardian** (1500-2000 XP) - Advanced capabilities
4. **Master** (4000-6000 XP) - Legendary abilities

## 📊 XP Calculation

Your final XP depends on multiple factors:

```
Final XP = Base XP × Quality × Collaboration × Diversity
```

### Quality Multipliers
- **Excellent**: 1.5x (comprehensive docs, tests, innovation)
- **Good**: 1.2x (above average, well documented)
- **Average**: 1.0x (meets standards)
- **Poor**: 0.5x (below standards)

### Collaboration Bonuses
- **Solo**: 1.0x
- **Pair**: 1.2x (pair programming)
- **Team**: 1.4x (3-5 contributors)
- **Cross-team**: 1.6x (multiple teams)

### Diversity Rewards
- **Single Pet**: 1.0x
- **Dual Pets**: 1.1x
- **Triple Pets**: 1.3x (maximum diversity bonus)

## 🎯 Pet Archetypes

### 📜 Scrollhound Progression
```
🐶 Scroll Puppy → 🔍 Documentation Tracker → 🛡️ Scroll Guardian → 📚 Scrollmaster Hound
```
**Specialties**: TLDL mastery, cross-referencing, quality assessment, template generation

### ⏰ ChronoCat Progression  
```
🐱 Time Kitten → ⏲️ Deadline Watcher → 🔮 Temporal Oracle → ⚡ ChronoMaster Cat
```
**Specialties**: Sprint optimization, velocity prediction, timeline visualization, risk assessment

### 🔧 Debugger Ferret Progression
```
🐾 Debug Kit → 🔍 Bug Hunter → 🕵️ Code Investigator → 🧙 Debugmaster Ferret
```
**Specialties**: Performance profiling, memory leak detection, security scanning, root cause analysis

## 🛡️ Privacy & Governance

### What We Track
✅ **Public Contributions**: TLDL entries, commits with docs, collaborative work  
✅ **Quality Metrics**: Contribution quality assessments  
✅ **Pet Evolution**: XP, levels, abilities unlocked  
✅ **Achievement Data**: Milestones and accomplishments  

### What We DON'T Track
❌ **Private Repositories**: Personal or company private work  
❌ **Personal Data**: No PII beyond GitHub handles  
❌ **Browsing Habits**: No tracking of documentation reading  
❌ **Time Surveillance**: No monitoring of development sessions  

### Your Rights
- **🔍 Full Transparency**: All formulas are public and auditable
- **⚖️ Appeal Process**: Request XP recalculation via TLDL entry or GitHub issue
- **🚪 Instant Opt-Out**: Disable all pet features immediately with no penalties
- **🔄 Reversible**: Re-enable pet features at any time

## 🧪 Testing Your Setup

Run the test suite to validate your installation:

```bash
python3 tests/test_badge_pet_system.py
```

Try the interactive demo:

```bash
python3 examples/pet_system_demo.py
```

## 📚 Documentation

### Core Documents
- **[PET Charter](docs/governance/PET-Charter.md)** - Complete governance framework
- **[Evolution Formulas](docs/registry/evolution-formulas.md)** - Public XP calculations
- **[Registry Schema](pets/registry.json)** - Pet definitions and abilities

### Integration Guides
- **Badge System Integration**: How pets work with existing sponsor badges
- **TLDL Integration**: Automatic XP from validated entries
- **Development Workflow**: Best practices for pet progression

## 🔧 Advanced Features

### Anti-Gaming Protection
- **Diminishing Returns**: Prevents XP farming with rate limiting
- **Quality Gates**: Ensures meaningful contributions only
- **Collaborative Diversity**: Encourages working with different people
- **Random Audits**: Maintains system integrity

### Security Features
- **Cryptographic Signatures**: All evolution events are HMAC-SHA256 signed
- **Tamper-Proof Ledger**: Append-only event history with integrity hashing
- **Environment Variables**: Production keys stored securely
- **Backup & Recovery**: Complete state reconstruction from ledger

## 🎮 Achievement System

Unlock special achievements:
- **🥇 First Companion** - Adopt your first pet (50 XP)
- **🏆 Trinity Master** - Master all three archetypes (1000 XP)
- **📚 Documentation Sage** - Create 50 TLDL entries (500 XP)
- **🐛 Bug Whisperer** - Fix 100 confirmed bugs (750 XP)

## 🚀 Getting Started Commands

```bash
# Initialize the pet system
python3 -c "
from src.DeveloperExperience.badge_pet_system import BadgePetSystem, PetArchetype
pet_system = BadgePetSystem()
pet_system.adopt_pet('your_handle', PetArchetype.SCROLLHOUND)
print('🎉 Welcome to the pet system!')
"

# Check your pet status
python3 -c "
from src.DeveloperExperience.badge_pet_system import BadgePetSystem
pet_system = BadgePetSystem()
profile = pet_system.get_developer_profile('your_handle')
if profile:
    for pet in profile.active_pets:
        print(f'🐾 {pet.archetype.value}: Level {pet.current_level.value}, {pet.xp} XP')
"
```

## 💬 Community & Support

- **Questions**: Create issue with `pet-question` label
- **Bug Reports**: Use `pet-bug` label  
- **Feature Requests**: Use `pet-enhancement` label
- **Appeals**: Use `pet-appeal` label for XP recalculation requests

## 🔮 Future Roadmap

Coming soon:
- **Pet Interactions**: Cross-developer collaboration mechanics
- **Seasonal Events**: Limited-time evolution paths
- **Guild System**: Team-based challenges
- **Mentor Pets**: Senior pets guide newcomers
- **Integration Hooks**: GitHub PR status, commit messages

---

**Ready to start your pet adventure?** Choose your archetype and begin contributing to watch your companion grow!

*The pet system grows stronger with every contribution. Your journey starts now.* 🐾✨