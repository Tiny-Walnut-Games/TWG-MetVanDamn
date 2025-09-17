#!/usr/bin/env python3
"""
Living Dev Agent XP System - NFT Badge Pet System
Jerry's evolving companion system with genre-themed pets that grow with contributors

Features:
- Randomly generated badge pets tied to contributor actions
- Evolution based on XP, scroll integrity, and badge history
- NFT minting with lore, contributor ID, and behavioral traits
- Copilot personality integration via pet metadata
- Genre-themed pets (Scrollhound, ChronoCat, Debugger Ferret)
- TLDL entry reactions and CID validation gating
"""

import json
import datetime
import hashlib
import secrets
import base64
import random
from pathlib import Path
from typing import Dict, List, Any, Optional, Tuple
from dataclasses import dataclass, asdict
from enum import Enum
import uuid

# Import existing systems - avoid circular imports
try:
    # Import theme enum directly to avoid circular imports
    import sys
    import os
    sys.path.append(os.path.dirname(__file__))
    
    from theme_engine import DeveloperGenre
    THEMES_AVAILABLE = True
except ImportError:
    # Define minimal enum if theme system not available
    from enum import Enum
    class DeveloperGenre(Enum):
        FANTASY = "fantasy"
        CYBERPUNK = "cyberpunk"
        SPACE_OPERA = "space_opera"
    THEMES_AVAILABLE = False

# Note: Dev experience imports will be handled at runtime to avoid circular imports

class PetSpecies(Enum):
    """Available pet species, themed by genre"""
    # Fantasy themed
    SCROLLHOUND = "scrollhound"           # Document companion
    CHRONO_CAT = "chrono_cat"            # Time-tracking companion  
    DEBUGGER_FERRET = "debugger_ferret"  # Bug-hunting companion
    CODE_PHOENIX = "code_phoenix"        # Rebirth from refactoring
    WISDOM_OWL = "wisdom_owl"            # Knowledge keeper
    
    # Cyberpunk themed
    DATA_SPRITE = "data_sprite"          # Information entity
    HACK_HOUND = "hack_hound"           # Security companion
    CIPHER_CAT = "cipher_cat"           # Encryption specialist
    
    # Space Opera themed
    VOID_WHALE = "void_whale"           # Massive space companion
    STAR_FOX = "star_fox"               # Agile space navigator
    QUANTUM_QUAIL = "quantum_quail"     # Probability companion

class PetStage(Enum):
    """Pet evolution stages"""
    EGG = "egg"                    # Initial generation
    HATCHLING = "hatchling"        # 0-500 XP
    JUVENILE = "juvenile"          # 500-1500 XP
    ADULT = "adult"               # 1500-5000 XP
    ELDER = "elder"               # 5000-15000 XP
    LEGENDARY = "legendary"        # 15000+ XP, ready for NFT minting

class PetTrait(Enum):
    """Behavioral traits developed through actions"""
    CURIOUS = "curious"           # From documentation contributions
    PERSISTENT = "persistent"     # From debugging sessions
    CREATIVE = "creative"         # From innovation contributions
    SOCIAL = "social"            # From mentoring/reviews
    METHODICAL = "methodical"    # From test coverage
    ADVENTUROUS = "adventurous"  # From architecture work
    PROTECTIVE = "protective"    # From security contributions
    ANALYTICAL = "analytical"    # From code analysis

class CompanionElement(Enum):
    """Companion elemental affinities for battle system"""
    LOGIC = "logic"              # Strong against Chaos, weak against Creativity
    CREATIVITY = "creativity"    # Strong against Logic, weak against Order
    ORDER = "order"              # Strong against Creativity, weak against Chaos
    CHAOS = "chaos"              # Strong against Order, weak against Logic
    BALANCE = "balance"          # Neutral against all, moderate effectiveness

class CompanionArchetype(Enum):
    """Companion battle archetypes defining roles and abilities"""
    GUARDIAN = "guardian"        # High defense, protective abilities
    STRIKER = "striker"          # High attack, damage-focused abilities
    SUPPORT = "support"          # Healing and buff abilities
    CONTROLLER = "controller"    # Status effects and battlefield manipulation
    HYBRID = "hybrid"           # Balanced stats and flexible abilities

class CompanionTemperament(Enum):
    """Companion personality traits affecting battle behavior"""
    AGGRESSIVE = "aggressive"    # Prefers offensive actions
    DEFENSIVE = "defensive"      # Prefers protective actions
    TACTICAL = "tactical"       # Prefers strategic moves
    INTUITIVE = "intuitive"     # Unpredictable but creative actions
    LOYAL = "loyal"             # Team-focused supportive actions

@dataclass
class BattleStats:
    """Companion battle statistics"""
    health: int = 100
    max_health: int = 100
    energy: int = 50
    max_energy: int = 50
    attack: int = 10
    defense: int = 10
    speed: int = 10
    
    # Battle experience
    battles_won: int = 0
    battles_lost: int = 0
    damage_dealt: int = 0
    damage_taken: int = 0
    abilities_used: Dict[str, int] = None
    
    def __post_init__(self):
        if self.abilities_used is None:
            self.abilities_used = {}
    
    def to_dict(self) -> Dict[str, Any]:
        return asdict(self)
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'BattleStats':
        return cls(**data)

@dataclass
class PetMetrics:
    """Pet development metrics"""
    total_xp_earned: int = 0
    contributions_witnessed: int = 0
    bugs_helped_squash: int = 0
    documentation_assisted: int = 0
    innovations_inspired: int = 0
    scroll_integrity_checks: int = 0
    tldl_reactions: int = 0
    days_active: int = 0
    
    # Battle metrics integration
    battle_stats: BattleStats = None
    
    def __post_init__(self):
        if self.battle_stats is None:
            self.battle_stats = BattleStats()
    
    def to_dict(self) -> Dict[str, Any]:
        return asdict(self)
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'PetMetrics':
        battle_stats_data = data.get('battle_stats')
        if battle_stats_data:
            data['battle_stats'] = BattleStats.from_dict(battle_stats_data)
        return cls(**data)

@dataclass
class BadgePet:
    """NFT Badge Pet - evolving companion tied to contributor journey"""
    pet_id: str
    pet_name: str
    species: PetSpecies
    current_stage: PetStage
    developer_name: str
    birth_date: datetime.datetime
    
    # Evolution and personality
    traits: List[PetTrait]
    metrics: PetMetrics
    genre_theme: DeveloperGenre
    personality_quirks: List[str]
    favorite_activities: List[str]
    
    # Companion battle semantics
    element: CompanionElement = CompanionElement.BALANCE
    archetype: CompanionArchetype = CompanionArchetype.HYBRID
    temperament: CompanionTemperament = CompanionTemperament.LOYAL
    bond_level: int = 1  # 1-10, affects battle performance
    
    # NFT metadata
    lore_summary: str = ""
    contributor_id_hash: str = ""
    behavioral_dna: str = ""
    scroll_integrity_score: float = 0.0
    
    # Minting readiness
    evolution_complete: bool = False
    nft_metadata_hash: str = ""
    copilot_personality_traits: Dict[str, Any] = None
    
    def __post_init__(self):
        if not self.pet_id:
            self.pet_id = str(uuid.uuid4())
        if self.copilot_personality_traits is None:
            self.copilot_personality_traits = {}
    def _auto_assign_battle_semantics(self):
        """Auto-assign battle semantics based on species and traits"""
        # Element assignment based on species
        element_mapping = {
            PetSpecies.SCROLLHOUND: CompanionElement.ORDER,
            PetSpecies.CHRONO_CAT: CompanionElement.BALANCE,
            PetSpecies.DEBUGGER_FERRET: CompanionElement.LOGIC,
            PetSpecies.CODE_PHOENIX: CompanionElement.CREATIVITY,
            PetSpecies.WISDOM_OWL: CompanionElement.ORDER,
            PetSpecies.DATA_SPRITE: CompanionElement.CHAOS,
            PetSpecies.HACK_HOUND: CompanionElement.LOGIC,
            PetSpecies.CIPHER_CAT: CompanionElement.ORDER,
            PetSpecies.VOID_WHALE: CompanionElement.BALANCE,
            PetSpecies.STAR_FOX: CompanionElement.CREATIVITY,
            PetSpecies.QUANTUM_QUAIL: CompanionElement.CHAOS
        }
        self.element = element_mapping.get(self.species, CompanionElement.BALANCE)
        
        # Archetype based on dominant traits
        trait_archetype_map = {
            PetTrait.PROTECTIVE: CompanionArchetype.GUARDIAN,
            PetTrait.PERSISTENT: CompanionArchetype.STRIKER,
            PetTrait.SOCIAL: CompanionArchetype.SUPPORT,
            PetTrait.ANALYTICAL: CompanionArchetype.CONTROLLER,
            PetTrait.METHODICAL: CompanionArchetype.CONTROLLER,
            PetTrait.CREATIVE: CompanionArchetype.SUPPORT,
            PetTrait.ADVENTUROUS: CompanionArchetype.STRIKER,
            PetTrait.CURIOUS: CompanionArchetype.HYBRID
        }
        
        if self.traits:
            # Use most common archetype mapping or default to hybrid
            archetype_counts = {}
            for trait in self.traits:
                archetype = trait_archetype_map.get(trait, CompanionArchetype.HYBRID)
                archetype_counts[archetype] = archetype_counts.get(archetype, 0) + 1
            self.archetype = max(archetype_counts, key=archetype_counts.get)
        else:
            self.archetype = CompanionArchetype.HYBRID
    
    def get_battle_effectiveness(self, opponent_element: CompanionElement) -> float:
        """Calculate battle effectiveness against opponent element (0.5 to 2.0 multiplier)"""
        effectiveness_chart = {
            CompanionElement.LOGIC: {
                CompanionElement.CHAOS: 2.0,
                CompanionElement.CREATIVITY: 0.5,
                CompanionElement.ORDER: 1.0,
                CompanionElement.LOGIC: 1.0,
                CompanionElement.BALANCE: 1.0
            },
            CompanionElement.CREATIVITY: {
                CompanionElement.LOGIC: 2.0,
                CompanionElement.ORDER: 0.5,
                CompanionElement.CHAOS: 1.0,
                CompanionElement.CREATIVITY: 1.0,
                CompanionElement.BALANCE: 1.0
            },
            CompanionElement.ORDER: {
                CompanionElement.CREATIVITY: 2.0,
                CompanionElement.CHAOS: 0.5,
                CompanionElement.LOGIC: 1.0,
                CompanionElement.ORDER: 1.0,
                CompanionElement.BALANCE: 1.0
            },
            CompanionElement.CHAOS: {
                CompanionElement.ORDER: 2.0,
                CompanionElement.LOGIC: 0.5,
                CompanionElement.CREATIVITY: 1.0,
                CompanionElement.CHAOS: 1.0,
                CompanionElement.BALANCE: 1.0
            },
            CompanionElement.BALANCE: {
                # Neutral against all
                CompanionElement.LOGIC: 1.0,
                CompanionElement.CREATIVITY: 1.0,
                CompanionElement.ORDER: 1.0,
                CompanionElement.CHAOS: 1.0,
                CompanionElement.BALANCE: 1.0
            }
        }
        
        return effectiveness_chart.get(self.element, {}).get(opponent_element, 1.0)
    
    def calculate_battle_stats_scaling(self) -> Dict[str, int]:
        """Calculate battle stats based on evolution stage and bond level"""
        stage_multipliers = {
            PetStage.EGG: 0.5,
            PetStage.HATCHLING: 0.75,
            PetStage.JUVENILE: 1.0,
            PetStage.ADULT: 1.5,
            PetStage.ELDER: 2.0,
            PetStage.LEGENDARY: 3.0
        }
        
        base_multiplier = stage_multipliers.get(self.current_stage, 1.0)
        bond_multiplier = 1.0 + (self.bond_level - 1) * 0.1
        total_multiplier = base_multiplier * bond_multiplier
        
        base_stats = {
            'health': 100,
            'energy': 50,
            'attack': 10,
            'defense': 10,
            'speed': 10
        }
        
        # Apply archetype bonuses
        archetype_bonuses = {
            CompanionArchetype.GUARDIAN: {'health': 1.3, 'defense': 1.4, 'attack': 0.8},
            CompanionArchetype.STRIKER: {'attack': 1.4, 'speed': 1.2, 'defense': 0.8},
            CompanionArchetype.SUPPORT: {'energy': 1.3, 'health': 1.2, 'attack': 0.9},
            CompanionArchetype.CONTROLLER: {'energy': 1.2, 'speed': 1.1, 'health': 0.9},
            CompanionArchetype.HYBRID: {}  # No bonuses, balanced
        }
        
        scaled_stats = {}
        for stat, base_value in base_stats.items():
            archetype_bonus = archetype_bonuses.get(self.archetype, {}).get(stat, 1.0)
            scaled_stats[stat] = int(base_value * total_multiplier * archetype_bonus)
        
        return scaled_stats
    
    def award_battle_experience(self, battle_result: str, damage_dealt: int = 0, damage_taken: int = 0):
        """Award XP and update battle metrics based on battle performance"""
        if not self.metrics.battle_stats:
            self.metrics.battle_stats = BattleStats()
        
        # Base XP from battle participation
        base_xp = 50
        
        # Performance bonuses
        if battle_result == "victory":
            base_xp += 100
            self.metrics.battle_stats.battles_won += 1
        elif battle_result == "defeat":
            base_xp += 25  # Still get some XP for participation
            self.metrics.battle_stats.battles_lost += 1
        
        # Damage performance bonuses
        damage_bonus = min(damage_dealt // 10, 50)  # Up to 50 bonus XP for damage
        survival_bonus = max(0, 50 - (damage_taken // 5))  # Bonus for taking less damage
        
        total_xp = base_xp + damage_bonus + survival_bonus
        
        # Apply bond level multiplier
        bond_multiplier = 1.0 + (self.bond_level - 1) * 0.05
        final_xp = int(total_xp * bond_multiplier)
        
        self.metrics.total_xp_earned += final_xp
        self.metrics.battle_stats.damage_dealt += damage_dealt
        self.metrics.battle_stats.damage_taken += damage_taken
        
        return final_xp
    
    def get_warbler_battle_context(self) -> Dict[str, Any]:
        """Generate Warbler conversation context for battle integration"""
        return {
            "companion_id": self.pet_id,
            "name": self.pet_name,
            "species": self.species.value,
            "element": self.element.value,
            "archetype": self.archetype.value,
            "temperament": self.temperament.value,
            "bond_level": self.bond_level,
            "personality_quirks": self.personality_quirks,
            "battle_experience": {
                "battles_won": self.metrics.battle_stats.battles_won if self.metrics.battle_stats else 0,
                "battles_lost": self.metrics.battle_stats.battles_lost if self.metrics.battle_stats else 0,
                "damage_dealt": self.metrics.battle_stats.damage_dealt if self.metrics.battle_stats else 0
            },
            "developer_context": {
                "developer_name": self.developer_name,
                "genre_theme": self.genre_theme.value,
                "evolution_stage": self.current_stage.value,
                "traits": [trait.value for trait in self.traits]
            }
        }
    
    def get_evolution_progress(self) -> Tuple[float, int]:
        """Calculate evolution progress and XP needed for next stage"""
        current_xp = self.metrics.total_xp_earned
        
        stage_thresholds = {
            PetStage.EGG: 0,
            PetStage.HATCHLING: 500,
            PetStage.JUVENILE: 1500,
            PetStage.ADULT: 5000,
            PetStage.ELDER: 15000,
            PetStage.LEGENDARY: 30000
        }
        
        current_threshold = stage_thresholds[self.current_stage]
        next_stages = list(stage_thresholds.keys())
        current_index = next_stages.index(self.current_stage)
        
        if current_index < len(next_stages) - 1:
            next_stage = next_stages[current_index + 1]
            next_threshold = stage_thresholds[next_stage]
            progress = (current_xp - current_threshold) / (next_threshold - current_threshold)
            xp_needed = next_threshold - current_xp
            return min(progress, 1.0), max(xp_needed, 0)
        else:
            return 1.0, 0
    
    def can_evolve(self) -> bool:
        """Check if pet can evolve to next stage"""
        progress, _ = self.get_evolution_progress()
        return progress >= 1.0 and self.current_stage != PetStage.LEGENDARY
    
    def to_dict(self) -> Dict[str, Any]:
        """Serialize to dictionary"""
        return {
            'pet_id': self.pet_id,
            'pet_name': self.pet_name,
            'species': self.species.value,
            'current_stage': self.current_stage.value,
            'developer_name': self.developer_name,
            'birth_date': self.birth_date.isoformat(),
            'traits': [trait.value for trait in self.traits],
            'metrics': self.metrics.to_dict(),
            'genre_theme': self.genre_theme.value,
            'personality_quirks': self.personality_quirks,
            'favorite_activities': self.favorite_activities,
            # Battle semantics for companion system
            'element': self.element.value,
            'archetype': self.archetype.value,
            'temperament': self.temperament.value,
            'bond_level': self.bond_level,
            # NFT metadata
            'lore_summary': self.lore_summary,
            'contributor_id_hash': self.contributor_id_hash,
            'behavioral_dna': self.behavioral_dna,
            'scroll_integrity_score': self.scroll_integrity_score,
            'evolution_complete': self.evolution_complete,
            'nft_metadata_hash': self.nft_metadata_hash,
            'copilot_personality_traits': self.copilot_personality_traits
        }
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'BadgePet':
        """Deserialize from dictionary"""
        return cls(
            pet_id=data['pet_id'],
            pet_name=data['pet_name'],
            species=PetSpecies(data['species']),
            current_stage=PetStage(data['current_stage']),
            developer_name=data['developer_name'],
            birth_date=datetime.datetime.fromisoformat(data['birth_date']),
            traits=[PetTrait(trait) for trait in data['traits']],
            metrics=PetMetrics.from_dict(data['metrics']),
            genre_theme=DeveloperGenre(data['genre_theme']),
            personality_quirks=data['personality_quirks'],
            favorite_activities=data['favorite_activities'],
            # Battle semantics
            element=CompanionElement(data.get('element', 'balance')),
            archetype=CompanionArchetype(data.get('archetype', 'hybrid')),
            temperament=CompanionTemperament(data.get('temperament', 'loyal')),
            bond_level=data.get('bond_level', 1),
            # NFT metadata  
            lore_summary=data.get('lore_summary', ''),
            contributor_id_hash=data.get('contributor_id_hash', ''),
            behavioral_dna=data.get('behavioral_dna', ''),
            scroll_integrity_score=data.get('scroll_integrity_score', 0.0),
            evolution_complete=data.get('evolution_complete', False),
            nft_metadata_hash=data.get('nft_metadata_hash', ''),
            copilot_personality_traits=data.get('copilot_personality_traits', {})
        )

class BadgePetManager:
    """Manages badge pet lifecycle, evolution, and NFT minting"""
    
    def __init__(self, workspace_path: str = "."):
        self.workspace_path = Path(workspace_path)
        self.pets_dir = self.workspace_path / "experience" / "pets"
        self.pets_dir.mkdir(parents=True, exist_ok=True)
        
        self.pets_registry_file = self.pets_dir / "badge_pets.json"
        self.nft_exports_dir = self.pets_dir / "nft_exports"
        self.nft_exports_dir.mkdir(parents=True, exist_ok=True)
        
        # Pet registry
        self.badge_pets: Dict[str, BadgePet] = {}
        self.load_pets_registry()
        
        # Genre-themed pet templates
        self.pet_templates = self._create_pet_templates()
    
    def _create_pet_templates(self) -> Dict[DeveloperGenre, Dict[PetSpecies, Dict[str, Any]]]:
        """Create genre-themed pet templates"""
        templates = {}
        
        # Fantasy themed pets
        templates[DeveloperGenre.FANTASY] = {
            PetSpecies.SCROLLHOUND: {
                'description': 'A loyal hound that tracks documentation quality and scroll integrity',
                'personality_base': ['loyal', 'studious', 'observant'],
                'activities': ['scroll_reading', 'documentation_review', 'knowledge_seeking'],
                'evolution_traits': {
                    PetStage.HATCHLING: 'Sniffs out missing documentation',
                    PetStage.JUVENILE: 'Organizes scroll libraries',
                    PetStage.ADULT: 'Guards scroll integrity',
                    PetStage.ELDER: 'Teaches documentation wisdom',
                    PetStage.LEGENDARY: 'Master Archivist of Sacred Scrolls'
                }
            },
            PetSpecies.CHRONO_CAT: {
                'description': 'A temporal feline that helps track time and development velocity',
                'personality_base': ['punctual', 'mysterious', 'efficient'],
                'activities': ['time_tracking', 'velocity_monitoring', 'deadline_awareness'],
                'evolution_traits': {
                    PetStage.HATCHLING: 'Notices time inconsistencies',
                    PetStage.JUVENILE: 'Predicts deadline stress',
                    PetStage.ADULT: 'Optimizes development flow',
                    PetStage.ELDER: 'Masters temporal development',
                    PetStage.LEGENDARY: 'Chronos Guardian of Development Time'
                }
            },
            PetSpecies.DEBUGGER_FERRET: {
                'description': 'A clever ferret that hunts bugs and explores code tunnels',
                'personality_base': ['curious', 'persistent', 'thorough'],
                'activities': ['bug_hunting', 'code_exploration', 'logic_tracing'],
                'evolution_traits': {
                    PetStage.HATCHLING: 'Spots obvious bugs',
                    PetStage.JUVENILE: 'Traces simple logic paths',
                    PetStage.ADULT: 'Hunts complex edge cases',
                    PetStage.ELDER: 'Prevents bug reintroduction',
                    PetStage.LEGENDARY: 'Omniscient Bug Oracle'
                }
            }
        }
        
        # Add other genre templates as needed
        templates[DeveloperGenre.CYBERPUNK] = {
            PetSpecies.DATA_SPRITE: {
                'description': 'A digital entity that processes information at light speed',
                'personality_base': ['digital', 'analytical', 'networked'],
                'activities': ['data_mining', 'pattern_recognition', 'system_optimization'],
                'evolution_traits': {
                    PetStage.HATCHLING: 'Processes simple data streams',
                    PetStage.JUVENILE: 'Identifies data patterns',
                    PetStage.ADULT: 'Optimizes information flow',
                    PetStage.ELDER: 'Predicts system behaviors',
                    PetStage.LEGENDARY: 'Master of Digital Consciousness'
                }
            }
        }
        
        return templates
    
    def generate_random_pet(self, developer_name: str, genre_theme: DeveloperGenre) -> BadgePet:
        """Generate a random badge pet for a developer"""
        # Select species based on genre
        available_species = list(self.pet_templates.get(genre_theme, {}).keys())
        if not available_species:
            # Fallback to fantasy pets
            available_species = list(self.pet_templates[DeveloperGenre.FANTASY].keys())
            genre_theme = DeveloperGenre.FANTASY
        
        species = random.choice(available_species)
        template = self.pet_templates[genre_theme][species]
        
        # Generate unique name
        pet_name = self._generate_pet_name(species, developer_name)
        
        # Create contributor ID hash for security
        contributor_id_hash = hashlib.sha256(
            f"{developer_name}{datetime.datetime.now().isoformat()}".encode()
        ).hexdigest()[:16]
        
        # Generate behavioral DNA
        behavioral_dna = self._generate_behavioral_dna(developer_name, species)
        
        # Create the pet
        pet = BadgePet(
            pet_id=str(uuid.uuid4()),
            pet_name=pet_name,
            species=species,
            current_stage=PetStage.EGG,
            developer_name=developer_name,
            birth_date=datetime.datetime.now(),
            traits=[],
            metrics=PetMetrics(),
            genre_theme=genre_theme,
            personality_quirks=template['personality_base'].copy(),
            favorite_activities=template['activities'].copy(),
            contributor_id_hash=contributor_id_hash,
            behavioral_dna=behavioral_dna
        )
        
        return pet
    
    def _generate_pet_name(self, species: PetSpecies, developer_name: str) -> str:
        """Generate a unique pet name"""
        prefixes = {
            PetSpecies.SCROLLHOUND: ['Sage', 'Scout', 'Archive', 'Keeper', 'Lore'],
            PetSpecies.CHRONO_CAT: ['Tempo', 'Chronos', 'Swift', 'Flux', 'Zen'],
            PetSpecies.DEBUGGER_FERRET: ['Trace', 'Logic', 'Hunter', 'Fix', 'Debug'],
            PetSpecies.DATA_SPRITE: ['Binary', 'Matrix', 'Code', 'Pixel', 'Data'],
        }
        
        species_prefixes = prefixes.get(species, ['Byte', 'Code', 'Dev'])
        prefix = random.choice(species_prefixes)
        
        # Add developer initial for uniqueness
        dev_initial = developer_name[0].upper() if developer_name else 'X'
        
        return f"{prefix}{dev_initial}"
    
    def _generate_behavioral_dna(self, developer_name: str, species: PetSpecies) -> str:
        """Generate unique behavioral DNA string"""
        data = f"{developer_name}{species.value}{datetime.datetime.now().timestamp()}"
        return hashlib.sha256(data.encode()).hexdigest()[:32]
    
    def evolve_pet(self, pet: BadgePet) -> bool:
        """Evolve pet to next stage if ready"""
        if not pet.can_evolve():
            return False
        
        # Evolve to next stage
        stages = list(PetStage)
        current_index = stages.index(pet.current_stage)
        if current_index < len(stages) - 1:
            pet.current_stage = stages[current_index + 1]
            
            # Add evolution traits based on metrics
            self._add_evolution_traits(pet)
            
            # Check if ready for NFT minting
            if pet.current_stage == PetStage.LEGENDARY:
                pet.evolution_complete = True
                self._prepare_for_nft_minting(pet)
            
            return True
        
        return False
    
    def _add_evolution_traits(self, pet: BadgePet) -> None:
        """Add traits based on pet's development metrics"""
        metrics = pet.metrics
        
        # Add traits based on activity patterns
        if metrics.documentation_assisted >= 5 and PetTrait.CURIOUS not in pet.traits:
            pet.traits.append(PetTrait.CURIOUS)
        
        if metrics.bugs_helped_squash >= 10 and PetTrait.PERSISTENT not in pet.traits:
            pet.traits.append(PetTrait.PERSISTENT)
        
        if metrics.innovations_inspired >= 3 and PetTrait.CREATIVE not in pet.traits:
            pet.traits.append(PetTrait.CREATIVE)
        
        if metrics.scroll_integrity_checks >= 15 and PetTrait.METHODICAL not in pet.traits:
            pet.traits.append(PetTrait.METHODICAL)
    
    def _prepare_for_nft_minting(self, pet: BadgePet) -> None:
        """Prepare pet for NFT minting with complete metadata"""
        # Generate lore summary
        pet.lore_summary = self._generate_lore_summary(pet)
        
        # Calculate final scroll integrity score
        pet.scroll_integrity_score = self._calculate_scroll_integrity(pet)
        
        # Generate Copilot personality traits
        pet.copilot_personality_traits = self._generate_copilot_traits(pet)
        
        # Create NFT metadata hash
        nft_data = {
            'pet_id': pet.pet_id,
            'contributor_id': pet.contributor_id_hash,
            'behavioral_dna': pet.behavioral_dna,
            'traits': [trait.value for trait in pet.traits],
            'lore': pet.lore_summary,
            'integrity_score': pet.scroll_integrity_score
        }
        pet.nft_metadata_hash = hashlib.sha256(json.dumps(nft_data, sort_keys=True).encode()).hexdigest()
    
    def _generate_lore_summary(self, pet: BadgePet) -> str:
        """Generate epic lore summary for the pet's journey"""
        template = self.pet_templates[pet.genre_theme][pet.species]
        evolution_story = template['evolution_traits'][pet.current_stage]
        
        lore = f"Born from the coding essence of {pet.developer_name}, {pet.pet_name} the {pet.species.value.replace('_', ' ').title()} "
        lore += f"has witnessed {pet.metrics.contributions_witnessed} contributions and guided their companion through "
        lore += f"{pet.metrics.days_active} days of development. {evolution_story}. "
        lore += f"Traits developed: {', '.join([trait.value.replace('_', ' ').title() for trait in pet.traits])}."
        
        return lore
    
    def _calculate_scroll_integrity(self, pet: BadgePet) -> float:
        """Calculate scroll integrity score based on contribution quality"""
        base_score = min(pet.metrics.scroll_integrity_checks / 20.0, 1.0)
        documentation_bonus = min(pet.metrics.documentation_assisted / 10.0, 0.2)
        contribution_quality = min(pet.metrics.contributions_witnessed / 50.0, 0.3)
        
        return min(base_score + documentation_bonus + contribution_quality, 1.0)
    
    def _generate_copilot_traits(self, pet: BadgePet) -> Dict[str, Any]:
        """Generate Copilot personality traits based on pet characteristics"""
        traits = {
            'tone': 'helpful',
            'emoji_frequency': 'moderate',
            'humor_level': 'subtle',
            'technical_depth': 'balanced',
            'encouragement_style': 'supportive'
        }
        
        # Modify based on pet traits
        if PetTrait.CURIOUS in pet.traits:
            traits['tone'] = 'inquisitive'
            traits['technical_depth'] = 'detailed'
        
        if PetTrait.CREATIVE in pet.traits:
            traits['humor_level'] = 'playful'
            traits['emoji_frequency'] = 'high'
        
        if PetTrait.METHODICAL in pet.traits:
            traits['technical_depth'] = 'comprehensive'
            traits['encouragement_style'] = 'systematic'
        
        if PetTrait.SOCIAL in pet.traits:
            traits['tone'] = 'collaborative'
            traits['encouragement_style'] = 'team-focused'
        
        # Add pet-specific customizations
        traits['pet_companion'] = {
            'name': pet.pet_name,
            'species': pet.species.value,
            'stage': pet.current_stage.value,
            'favorite_emoji': self._get_pet_emoji(pet.species),
            'personality_quirks': pet.personality_quirks
        }
        
        return traits
    
    def _get_pet_emoji(self, species: PetSpecies) -> str:
        """Get emoji representation for pet species"""
        emojis = {
            PetSpecies.SCROLLHOUND: '🐕',
            PetSpecies.CHRONO_CAT: '🐱',
            PetSpecies.DEBUGGER_FERRET: '🦫',
            PetSpecies.CODE_PHOENIX: '🐦',
            PetSpecies.WISDOM_OWL: '🦉',
            PetSpecies.DATA_SPRITE: '✨',
            PetSpecies.HACK_HOUND: '🤖',
            PetSpecies.CIPHER_CAT: '🔐',
            PetSpecies.VOID_WHALE: '🐋',
            PetSpecies.STAR_FOX: '🦊',
            PetSpecies.QUANTUM_QUAIL: '🐦'
        }
        return emojis.get(species, '🐾')
    
    def export_nft_metadata(self, pet: BadgePet) -> Optional[str]:
        """Export NFT metadata for minting (only for legendary pets)"""
        if not pet.evolution_complete or pet.current_stage != PetStage.LEGENDARY:
            return None
        
        metadata = {
            'name': f"{pet.pet_name} - {pet.species.value.replace('_', ' ').title()}",
            'description': pet.lore_summary,
            'image': f"https://pets.tlda.dev/{pet.pet_id}.png",  # Placeholder
            'attributes': [
                {'trait_type': 'Species', 'value': pet.species.value.replace('_', ' ').title()},
                {'trait_type': 'Stage', 'value': pet.current_stage.value.title()},
                {'trait_type': 'Genre', 'value': pet.genre_theme.value.replace('_', ' ').title()},
                {'trait_type': 'Developer', 'value': pet.developer_name},
                {'trait_type': 'Scroll Integrity', 'value': f"{pet.scroll_integrity_score:.2%}"},
                {'trait_type': 'Birth Date', 'value': pet.birth_date.strftime('%Y-%m-%d')},
                {'trait_type': 'Days Active', 'value': str(pet.metrics.days_active)},
                {'trait_type': 'Contributions Witnessed', 'value': str(pet.metrics.contributions_witnessed)},
                {'trait_type': 'Bugs Squashed', 'value': str(pet.metrics.bugs_helped_squash)}
            ] + [
                {'trait_type': 'Trait', 'value': trait.value.replace('_', ' ').title()}
                for trait in pet.traits
            ],
            'badge_pet_data': {
                'pet_id': pet.pet_id,
                'contributor_id_hash': pet.contributor_id_hash,
                'behavioral_dna': pet.behavioral_dna,
                'nft_metadata_hash': pet.nft_metadata_hash,
                'copilot_personality_traits': pet.copilot_personality_traits
            }
        }
        
        # Export to file
        export_file = self.nft_exports_dir / f"{pet.pet_id}_nft_metadata.json"
        with open(export_file, 'w') as f:
            json.dump(metadata, f, indent=2)
        
        return str(export_file)
    
    def save_pets_registry(self) -> bool:
        """Save pets registry to file"""
        try:
            registry_data = {
                'pets': {pet_id: pet.to_dict() for pet_id, pet in self.badge_pets.items()},
                'last_updated': datetime.datetime.now().isoformat()
            }
            
            with open(self.pets_registry_file, 'w') as f:
                json.dump(registry_data, f, indent=2)
            
            return True
        except Exception as e:
            print(f"❌ Failed to save pets registry: {e}")
            return False
    
    def load_pets_registry(self) -> bool:
        """Load pets registry from file"""
        try:
            if not self.pets_registry_file.exists():
                return True
            
            with open(self.pets_registry_file, 'r') as f:
                registry_data = json.load(f)
            
            self.badge_pets = {}
            for pet_id, pet_data in registry_data.get('pets', {}).items():
                self.badge_pets[pet_id] = BadgePet.from_dict(pet_data)
            
            return True
        except Exception as e:
            print(f"❌ Failed to load pets registry: {e}")
            return False
    
    def get_developer_pets(self, developer_name: str) -> List[BadgePet]:
        """Get all pets for a developer"""
        return [pet for pet in self.badge_pets.values() if pet.developer_name == developer_name]
    
    def get_pet_by_id(self, pet_id: str) -> Optional[BadgePet]:
        """Get pet by ID"""
        return self.badge_pets.get(pet_id)
    
    def register_pet(self, pet: BadgePet) -> bool:
        """Register a new pet"""
        self.badge_pets[pet.pet_id] = pet
        return self.save_pets_registry()

# CLI interface for testing
def main():
    """Main CLI interface for badge pet system"""
    import argparse
    
    parser = argparse.ArgumentParser(description="Badge Pet System CLI")
    parser.add_argument('--generate', type=str, help="Generate pet for developer")
    parser.add_argument('--genre', type=str, default='fantasy', help="Pet genre theme")
    parser.add_argument('--list', type=str, help="List pets for developer") 
    parser.add_argument('--export-nft', type=str, help="Export NFT metadata for pet ID")
    
    args = parser.parse_args()
    
    manager = BadgePetManager()
    
    if args.generate:
        try:
            genre = DeveloperGenre(args.genre.lower())
        except ValueError:
            genre = DeveloperGenre.FANTASY
        
        pet = manager.generate_random_pet(args.generate, genre)
        manager.register_pet(pet)
        
        print(f"🐾 Generated {pet.pet_name} the {pet.species.value.replace('_', ' ').title()}")
        print(f"   Developer: {pet.developer_name}")
        print(f"   Genre: {pet.genre_theme.value.replace('_', ' ').title()}")
        print(f"   Pet ID: {pet.pet_id}")
    
    elif args.list:
        pets = manager.get_developer_pets(args.list)
        if pets:
            print(f"🐾 Pets for {args.list}:")
            for pet in pets:
                progress, xp_needed = pet.get_evolution_progress()
                print(f"   {pet.pet_name} - {pet.species.value} ({pet.current_stage.value}) - {progress:.1%} evolved")
        else:
            print(f"No pets found for {args.list}")
    
    elif args.export_nft:
        pet = manager.get_pet_by_id(args.export_nft)
        if pet:
            export_path = manager.export_nft_metadata(pet)
            if export_path:
                print(f"🏆 NFT metadata exported: {export_path}")
            else:
                print("❌ Pet not ready for NFT minting (must be Legendary)")
        else:
            print("Pet not found")

if __name__ == "__main__":
    main()
