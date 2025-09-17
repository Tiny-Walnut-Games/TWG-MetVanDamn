#!/usr/bin/env python3
"""
Living Dev Agent Template - Developer Experience & Achievement System
Jerry's gamified development progression with XP, badges, and CopilotCoins

Execution time: ~15ms for XP calculations
Persistent achievement tracking and developer progression
NOW WITH THEMATIC FLAVOR! 🎭
"""

import json
import datetime
import hashlib
from pathlib import Path
from typing import List, Dict, Any, Optional, Tuple
from dataclasses import dataclass, asdict
from enum import Enum
import uuid

# Import theme system
try:
    from theme_engine import GenreThemeManager, DeveloperGenre
    THEMES_AVAILABLE = True
except ImportError:
    THEMES_AVAILABLE = False

# Import badge pet system
try:
    from badge_pet_system import BadgePetManager, PetTrait, PetStage
    BADGE_PETS_AVAILABLE = True
except ImportError:
    BADGE_PETS_AVAILABLE = False

# Color codes for epic achievement notifications
class Colors:
    HEADER = '\033[95m'
    OKBLUE = '\033[94m'
    OKCYAN = '\033[96m'
    OKGREEN = '\033[92m'
    WARNING = '\033[93m'
    FAIL = '\033[91m'
    ENDC = '\033[0m'
    BOLD = '\033[1m'
    UNDERLINE = '\033[4m'
    GOLD = '\033[93m'
    PURPLE = '\033[95m'

# Sacred emojis for achievements
EMOJI_XP = "⭐"
EMOJI_LEVEL_UP = "🎉"
EMOJI_BADGE = "🏅"
EMOJI_COIN = "🪙"
EMOJI_ACHIEVEMENT = "🏆"

class ContributionType(Enum):
    """Types of developer contributions"""
    CODE_CONTRIBUTION = "code_contribution"
    DEBUGGING_SESSION = "debugging_session"
    DOCUMENTATION = "documentation"
    TEST_COVERAGE = "test_coverage"
    REFACTORING = "refactoring"
    ARCHITECTURE = "architecture"
    MENTORING = "mentoring"
    INNOVATION = "innovation"
    CODE_REVIEW = "code_review"
    ISSUE_RESOLUTION = "issue_resolution"

class QualityLevel(Enum):
    """Quality assessment levels"""
    LEGENDARY = "legendary"      # 2.5x multiplier
    EPIC = "epic"               # 2.0x multiplier
    EXCELLENT = "excellent"     # 1.5x multiplier
    GOOD = "good"              # 1.0x multiplier
    NEEDS_WORK = "needs_work"  # 0.5x multiplier

@dataclass
class Achievement:
    """Developer achievement definition"""
    achievement_id: str
    name: str
    description: str
    emoji: str
    badge_color: str
    date_earned: datetime.datetime
    contribution_id: str = ""
    faculty_signature: str = ""
    
    def to_dict(self) -> Dict[str, Any]:
        """Serialize to dictionary"""
        return {
            'achievement_id': self.achievement_id,
            'name': self.name,
            'description': self.description,
            'emoji': self.emoji,
            'badge_color': self.badge_color,
            'date_earned': self.date_earned.isoformat(),
            'contribution_id': self.contribution_id,
            'faculty_signature': self.faculty_signature
        }
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'Achievement':
        """Deserialize from dictionary"""
        return cls(
            achievement_id=data['achievement_id'],
            name=data['name'],
            description=data['description'],
            emoji=data['emoji'],
            badge_color=data['badge_color'],
            date_earned=datetime.datetime.fromisoformat(data['date_earned']),
            contribution_id=data.get('contribution_id', ''),
            faculty_signature=data.get('faculty_signature', '')
        )

@dataclass
class Contribution:
    """Developer contribution record"""
    contribution_id: str
    developer_name: str
    contribution_type: ContributionType
    quality_level: QualityLevel
    description: str
    files_affected: List[str]
    timestamp: datetime.datetime
    base_xp: int
    quality_multiplier: float
    total_xp: int
    coins_earned: int
    metrics: Dict[str, Any] = None
    
    def __post_init__(self):
        if self.metrics is None:
            self.metrics = {}
    
    def to_dict(self) -> Dict[str, Any]:
        """Serialize to dictionary"""
        return {
            'contribution_id': self.contribution_id,
            'developer_name': self.developer_name,
            'contribution_type': self.contribution_type.value,
            'quality_level': self.quality_level.value,
            'description': self.description,
            'files_affected': self.files_affected,
            'timestamp': self.timestamp.isoformat(),
            'base_xp': self.base_xp,
            'quality_multiplier': self.quality_multiplier,
            'total_xp': self.total_xp,
            'coins_earned': self.coins_earned,
            'metrics': self.metrics
        }
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'Contribution':
        """Deserialize from dictionary"""
        return cls(
            contribution_id=data['contribution_id'],
            developer_name=data['developer_name'],
            contribution_type=ContributionType(data['contribution_type']),
            quality_level=QualityLevel(data['quality_level']),
            description=data['description'],
            files_affected=data['files_affected'],
            timestamp=datetime.datetime.fromisoformat(data['timestamp']),
            base_xp=data['base_xp'],
            quality_multiplier=data['quality_multiplier'],
            total_xp=data['total_xp'],
            coins_earned=data['coins_earned'],
            metrics=data.get('metrics', {})
        )

@dataclass
class DeveloperProfile:
    """Developer profile with progression stats"""
    developer_name: str
    total_xp: int = 0
    level: int = 1
    title: str = "🌱 Seedling Coder"
    copilot_coins: int = 0
    contributions: List[Contribution] = None
    achievements: List[Achievement] = None
    faculty_badges: List[str] = None
    badge_pets: List[str] = None  # Pet IDs for the developer's badge pets
    created_date: datetime.datetime = None
    last_active: datetime.datetime = None
    
    def __post_init__(self):
        if self.contributions is None:
            self.contributions = []
        if self.achievements is None:
            self.achievements = []
        if self.faculty_badges is None:
            self.faculty_badges = []
        if self.badge_pets is None:
            self.badge_pets = []
        if self.created_date is None:
            self.created_date = datetime.datetime.now()
        if self.last_active is None:
            self.last_active = datetime.datetime.now()
    
    def calculate_level(self) -> Tuple[int, str]:
        """Calculate level and title based on XP"""
        if self.total_xp >= 30000:
            return 7, "🌟 Legendary Architect"
        elif self.total_xp >= 15000:
            return 6, "🧙‍♂️ Debugging Sage"
        elif self.total_xp >= 7500:
            return 5, "⚡ Code Wizard"
        elif self.total_xp >= 3500:
            return 4, "🏔️ Mountain Climber"
        elif self.total_xp >= 1500:
            return 3, "🌳 Seasoned Programmer"
        elif self.total_xp >= 500:
            return 2, "🌿 Growing Developer"
        else:
            return 1, "🌱 Seedling Coder"
    
    def to_dict(self) -> Dict[str, Any]:
        """Serialize to dictionary"""
        return {
            'developer_name': self.developer_name,
            'total_xp': self.total_xp,
            'level': self.level,
            'title': self.title,
            'copilot_coins': self.copilot_coins,
            'contributions': [c.to_dict() for c in self.contributions],
            'achievements': [a.to_dict() for a in self.achievements],
            'faculty_badges': self.faculty_badges,
            'badge_pets': self.badge_pets,
            'created_date': self.created_date.isoformat(),
            'last_active': self.last_active.isoformat()
        }
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'DeveloperProfile':
        """Deserialize from dictionary"""
        profile = cls(
            developer_name=data['developer_name'],
            total_xp=data.get('total_xp', 0),
            level=data.get('level', 1),
            title=data.get('title', "🌱 Seedling Coder"),
            copilot_coins=data.get('copilot_coins', 0),
            faculty_badges=data.get('faculty_badges', []),
            badge_pets=data.get('badge_pets', []),
            created_date=datetime.datetime.fromisoformat(data['created_date']),
            last_active=datetime.datetime.fromisoformat(data['last_active'])
        )
        
        # Load contributions
        for contrib_data in data.get('contributions', []):
            profile.contributions.append(Contribution.from_dict(contrib_data))
        
        # Load achievements
        for achievement_data in data.get('achievements', []):
            profile.achievements.append(Achievement.from_dict(achievement_data))
        
        return profile

class DeveloperExperienceManager:
    """Jerry's gamified developer progression system with thematic flair"""
    
    # Base XP values for different contribution types
    BASE_XP_VALUES = {
        ContributionType.CODE_CONTRIBUTION: 50,
        ContributionType.DEBUGGING_SESSION: 75,
        ContributionType.DOCUMENTATION: 40,
        ContributionType.TEST_COVERAGE: 60,
        ContributionType.REFACTORING: 45,
        ContributionType.ARCHITECTURE: 100,
        ContributionType.MENTORING: 80,
        ContributionType.INNOVATION: 150,
        ContributionType.CODE_REVIEW: 35,
        ContributionType.ISSUE_RESOLUTION: 55
    }
    
    # Quality multipliers
    QUALITY_MULTIPLIERS = {
        QualityLevel.LEGENDARY: 2.5,
        QualityLevel.EPIC: 2.0,
        QualityLevel.EXCELLENT: 1.5,
        QualityLevel.GOOD: 1.0,
        QualityLevel.NEEDS_WORK: 0.5
    }
    
    # CopilotCoin base awards
    BASE_COIN_VALUES = {
        ContributionType.CODE_CONTRIBUTION: 15,
        ContributionType.DEBUGGING_SESSION: 20,
        ContributionType.DOCUMENTATION: 12,
        ContributionType.TEST_COVERAGE: 18,
        ContributionType.REFACTORING: 14,
        ContributionType.ARCHITECTURE: 30,
        ContributionType.MENTORING: 25,
        ContributionType.INNOVATION: 40,
        ContributionType.CODE_REVIEW: 10,
        ContributionType.ISSUE_RESOLUTION: 16
    }
    
    def __init__(self, workspace_path: str = "."):
        self.workspace_path = Path(workspace_path)
        self.developer_profiles: Dict[str, DeveloperProfile] = {}
        
        # Create experience directories
        self.experience_dir = self.workspace_path / "experience"
        self.experience_dir.mkdir(exist_ok=True)
        
        self.profiles_dir = self.experience_dir / "profiles"
        self.profiles_dir.mkdir(exist_ok=True)
        
        self.achievements_dir = self.experience_dir / "achievements"
        self.achievements_dir.mkdir(exist_ok=True)
        
        # Data files
        self.profiles_file = self.experience_dir / "developer_profiles.json"
        self.global_stats_file = self.experience_dir / "global_stats.json"
        
        # Initialize theme system
        if THEMES_AVAILABLE:
            self.theme_manager = GenreThemeManager(workspace_path)
        else:
            self.theme_manager = None
            
        # Initialize badge pet system
        if BADGE_PETS_AVAILABLE:
            self.pet_manager = BadgePetManager(workspace_path)
        else:
            self.pet_manager = None
        
        # Load existing data
        self.load_profiles()
        self.load_build_history()
        self.discover_scenes()

    def load_build_history(self):
        """Placeholder for build history loading (not implemented in theme system)"""
        pass

    def discover_scenes(self):
        """Placeholder for scene discovery (not implemented in theme system)"""  
        pass

    def _get_themed_emoji(self, emoji_type: str, fallback: str) -> str:
        """Get themed emoji or fallback"""
        if self.theme_manager:
            return self.theme_manager.get_themed_emoji(emoji_type)
        return fallback

    def _get_themed_term(self, term: str) -> str:
        """Get themed terminology"""
        if self.theme_manager:
            return self.theme_manager.get_themed_term(term)
        return term.title()

    def _format_themed_message(self, message: str, message_type: str = 'info') -> str:
        """Format message with theme colors and emojis"""
        if self.theme_manager:
            return self.theme_manager.format_themed_message(message, message_type)
        
        # Fallback formatting
        fallback_emojis = {
            'success': '✅', 'warning': '⚠️', 'error': '❌', 'info': '🔍',
            'xp': '⭐', 'level_up': '🎉', 'achievement': '🏆'
        }
        emoji = fallback_emojis.get(message_type, '🔍')
        return f"{emoji} {message}"

    def log_achievement(self, message: str):
        """Log achievement notification with thematic styling"""
        emoji = self._get_themed_emoji('achievement', '🏆')
        formatted = self._format_themed_message(message, 'achievement')
        print(formatted)

    def log_xp_gain(self, message: str):
        """Log XP gain with thematic styling"""
        emoji = self._get_themed_emoji('xp', '⭐')
        formatted = self._format_themed_message(message, 'xp')
        print(formatted)

    def log_level_up(self, message: str):
        """Log level up with thematic celebration"""
        emoji = self._get_themed_emoji('level_up', '🎉')
        formatted = self._format_themed_message(message, 'level_up')
        print(formatted)

    def log_success(self, message: str):
        """Log success with thematic styling"""
        formatted = self._format_themed_message(message, 'success')
        print(formatted)

    def log_warning(self, message: str):
        """Log warning with thematic styling"""
        formatted = self._format_themed_message(message, 'warning')
        print(formatted)

    def log_error(self, message: str):
        """Log error with thematic styling"""
        formatted = self._format_themed_message(message, 'error')
        print(formatted)

    def record_contribution(self, developer_name: str, contribution_type: ContributionType,
                          quality_level: QualityLevel, description: str,
                          files_affected: List[str] = None, metrics: Dict[str, Any] = None) -> str:
        """Record a developer contribution and award XP/coins/achievements with thematic flair"""
        try:
            if files_affected is None:
                files_affected = []
            if metrics is None:
                metrics = {}
            
            # Generate contribution ID
            contribution_id = str(uuid.uuid4())[:8]
            
            # Calculate XP and coins
            base_xp = self.BASE_XP_VALUES[contribution_type]
            quality_multiplier = self.QUALITY_MULTIPLIERS[quality_level]
            total_xp = int(base_xp * quality_multiplier)
            
            base_coins = self.BASE_COIN_VALUES[contribution_type]
            coins_earned = int(base_coins * quality_multiplier)
            
            # Create contribution record
            contribution = Contribution(
                contribution_id=contribution_id,
                developer_name=developer_name,
                contribution_type=contribution_type,
                quality_level=quality_level,
                description=description,
                files_affected=files_affected,
                timestamp=datetime.datetime.now(),
                base_xp=base_xp,
                quality_multiplier=quality_multiplier,
                total_xp=total_xp,
                coins_earned=coins_earned,
                metrics=metrics
            )
            
            # Get or create developer profile
            if developer_name not in self.developer_profiles:
                self.developer_profiles[developer_name] = DeveloperProfile(developer_name=developer_name)
            
            profile = self.developer_profiles[developer_name]
            old_level = profile.level
            
            # Update profile
            profile.contributions.append(contribution)
            profile.total_xp += total_xp
            profile.copilot_coins += coins_earned
            profile.last_active = datetime.datetime.now()
            
            # Calculate new level with themed titles
            new_level, new_title = self._calculate_themed_level(profile.total_xp)
            profile.level = new_level
            profile.title = new_title
            
            # Themed terminology
            contrib_term = self._get_themed_term(contribution_type.value.replace('_', ' '))
            coin_name = self.theme_manager.get_current_theme().coin_name if self.theme_manager else "CopilotCoins"
            coin_symbol = self.theme_manager.get_current_theme().currency_symbol if self.theme_manager else "🪙"
            
            # Log XP gain with themed language
            self.log_xp_gain(f"{developer_name} earned {total_xp} XP ({quality_level.value} {contrib_term.lower()})")
            self.log_xp_gain(f"{developer_name} earned {coins_earned} {coin_name} {coin_symbol}")
            
            # Check for level up with themed title
            if new_level > old_level:
                self.log_level_up(f"{developer_name} leveled up! {old_level} → {new_level} ({new_title})")
            
            # Award achievements with themed descriptions
            new_achievements = self._check_themed_achievements(profile, contribution)
            for achievement in new_achievements:
                profile.achievements.append(achievement)
                self.log_achievement(f"{developer_name} earned: {achievement.emoji} {achievement.name}")
            
            # Award faculty badges
            new_badges = self._award_faculty_badges(contribution)
            for badge in new_badges:
                if badge not in profile.faculty_badges:
                    profile.faculty_badges.append(badge)
                    self.log_achievement(f"{developer_name} earned faculty badge: {badge}")
            
            # Handle badge pets
            self._update_badge_pets(profile, contribution)
            
            # Save profiles
            self.save_profiles()
            
            return contribution_id
            
        except Exception as e:
            self.log_error(f"Failed to record contribution: {e}")
            return ""

    def _calculate_themed_level(self, total_xp: int) -> Tuple[int, str]:
        """Calculate level with themed titles"""
        if self.theme_manager:
            # Use themed level titles
            if total_xp >= 30000:
                level = 7
            elif total_xp >= 15000:
                level = 6
            elif total_xp >= 7500:
                level = 5
            elif total_xp >= 3500:
                level = 4
            elif total_xp >= 1500:
                level = 3
            elif total_xp >= 500:
                level = 2
            else:
                level = 1
            
            title = self.theme_manager.get_level_title(level)
            return level, title
        else:
            # Fallback to default titles
            if total_xp >= 30000:
                return 7, "🌟 Legendary Architect"
            elif total_xp >= 15000:
                return 6, "🧙‍♂️ Debugging Sage"
            elif total_xp >= 7500:
                return 5, "⚡ Code Wizard"
            elif total_xp >= 3500:
                return 4, "🏔️ Mountain Climber"
            elif total_xp >= 1500:
                return 3, "🌳 Seasoned Programmer"
            elif total_xp >= 500:
                return 2, "🌿 Growing Developer"
            else:
                return 1, "🌱 Seedling Coder"

    def _check_themed_achievements(self, profile: DeveloperProfile, contribution: Contribution) -> List[Achievement]:
        """Check and award achievements with themed descriptions"""
        achievements = []
        
        # First contribution achievement
        if len(profile.contributions) == 1:
            theme_desc = self._get_themed_achievement('first_steps', 
                                                     "Made your first contribution to the codebase")
            achievements.append(Achievement(
                achievement_id="first_steps",
                name="First Steps",
                description=theme_desc,
                emoji=self._get_themed_emoji('achievement', '👶'),
                badge_color="green",
                date_earned=datetime.datetime.now(),
                contribution_id=contribution.contribution_id,
                faculty_signature=f"{self._get_themed_emoji('debug', '🧙‍♂️')} Bootstrap Sentinel"
            ))
        
        # Quality-based achievements
        if contribution.quality_level == QualityLevel.LEGENDARY:
            contrib_term = self._get_themed_term(contribution.contribution_type.value.replace('_', ' '))
            theme_desc = self._get_themed_achievement('legendary_innovation',
                                                     f"Delivered legendary quality {contrib_term.lower()}")
            achievements.append(Achievement(
                achievement_id=f"legendary_{contribution.contribution_type.value}",
                name=f"Legendary {contrib_term}",
                description=theme_desc,
                emoji="🌟",
                badge_color="gold",
                date_earned=datetime.datetime.now(),
                contribution_id=contribution.contribution_id,
                faculty_signature="⚡ Quality Oracle"
            ))
        
        # Debugging achievements
        if contribution.contribution_type == ContributionType.DEBUGGING_SESSION:
            debug_sessions = len([c for c in profile.contributions if c.contribution_type == ContributionType.DEBUGGING_SESSION])
            
            if debug_sessions == 5:
                debug_term = self._get_themed_term('debugging_session')
                achievements.append(Achievement(
                    achievement_id="debug_detective",
                    name=f"{debug_term} Detective",
                    description=f"Completed 5 {debug_term.lower()} sessions",
                    emoji=self._get_themed_emoji('debug', '🔍'),
                    badge_color="blue",
                    date_earned=datetime.datetime.now(),
                    contribution_id=contribution.contribution_id,
                    faculty_signature="📝 Console Commentary Master"
                ))
            
            if "fuck_moments_resolved" in contribution.metrics and contribution.metrics["fuck_moments_resolved"] >= 3:
                theme_desc = self._get_themed_achievement('triple_fuck_slayer',
                                                         "Resolved 3+ FUCK moments in a single session")
                achievements.append(Achievement(
                    achievement_id="triple_fuck_slayer",
                    name="Triple FUCK Moment Slayer",
                    description=theme_desc,
                    emoji=self._get_themed_emoji('innovation', '🔥'),
                    badge_color="red",
                    date_earned=datetime.datetime.now(),
                    contribution_id=contribution.contribution_id,
                    faculty_signature="🔍 FUCK Moment Resolver"
                ))
        
        # Documentation achievements
        if contribution.contribution_type == ContributionType.DOCUMENTATION:
            doc_contributions = len([c for c in profile.contributions if c.contribution_type == ContributionType.DOCUMENTATION])
            
            if doc_contributions == 10:
                achievements.append(Achievement(
                    achievement_id="documentation_champion",
                    name="Documentation Champion",
                    description="Created 10 documentation contributions",
                    emoji="📚",
                    badge_color="purple",
                    date_earned=datetime.datetime.now(),
                    contribution_id=contribution.contribution_id,
                    faculty_signature="📚 Knowledge Preservation Monk"
                ))
        
        # Architecture achievements
        if contribution.contribution_type == ContributionType.ARCHITECTURE:
            achievements.append(Achievement(
                achievement_id="system_architect",
                name="System Architect",
                description="Contributed to system architecture",
                emoji="🏗️",
                badge_color="silver",
                date_earned=datetime.datetime.now(),
                contribution_id=contribution.contribution_id,
                faculty_signature="🏗️ System Design Oracle"
            ))
        
        # Consistency achievements
        recent_contributions = [c for c in profile.contributions 
                             if (datetime.datetime.now() - c.timestamp).days <= 7]
        if len(recent_contributions) >= 5:
            achievements.append(Achievement(
                achievement_id="consistent_contributor",
                name="Consistent Contributor",
                description="Made 5+ contributions in one week",
                emoji="🎯",
                badge_color="orange",
                date_earned=datetime.datetime.now(),
                contribution_id=contribution.contribution_id,
                faculty_signature="⏰ Temporal Flow Master"
            ))
        
        return achievements

    def _get_themed_achievement(self, achievement_id: str, fallback: str) -> str:
        """Get themed achievement description"""
        if self.theme_manager:
            return self.theme_manager.get_themed_achievement(achievement_id, fallback)
        return fallback

    def _award_faculty_badges(self, contribution: Contribution) -> List[str]:
        """Award faculty-specific badges based on contribution"""
        badges = []
        
        # Console Commentary badges
        if contribution.contribution_type == ContributionType.DEBUGGING_SESSION:
            if contribution.quality_level in [QualityLevel.EPIC, QualityLevel.LEGENDARY]:
                badges.append("📝 Console Commentary Sage")
            
            if "solution_clarity_score" in contribution.metrics and contribution.metrics["solution_clarity_score"] > 0.85:
                badges.append("💡 Eureka Moment Documentarian")
        
        # Code Snapshot badges
        if "code_snapshots" in contribution.metrics:
            snapshot_count = contribution.metrics.get("code_snapshots", 0)
            if snapshot_count >= 5:
                badges.append("📸 Context Capture Virtuoso")
            
            if contribution.metrics.get("context_completeness", 0) > 0.9:
                badges.append("🎯 Perfect Context Capturer")
        
        # TaskMaster badges
        if contribution.contribution_type in [ContributionType.ARCHITECTURE, ContributionType.CODE_CONTRIBUTION]:
            if contribution.quality_level == QualityLevel.LEGENDARY:
                badges.append("🎯 Epic Quest Coordinator")
        
        # Chronas badges
        if "time_tracking_accuracy" in contribution.metrics:
            if contribution.metrics["time_tracking_accuracy"] > 0.9:
                badges.append("⏰ Temporal Flow Master")
        
        # Validation badges
        if contribution.contribution_type == ContributionType.TEST_COVERAGE:
            if contribution.quality_level in [QualityLevel.EXCELLENT, QualityLevel.EPIC, QualityLevel.LEGENDARY]:
                badges.append("🛡️ Quality Assurance Sentinel")
        
        return badges

    def _update_badge_pets(self, profile: DeveloperProfile, contribution: Contribution) -> None:
        """Update badge pets with new contribution data"""
        if not self.pet_manager:
            return
        
        # Create first pet if developer doesn't have any
        if not profile.badge_pets:
            # Determine genre theme
            current_genre = self.theme_manager.current_genre if self.theme_manager else DeveloperGenre.FANTASY
            
            # Generate first pet
            new_pet = self.pet_manager.generate_random_pet(profile.developer_name, current_genre)
            self.pet_manager.register_pet(new_pet)
            profile.badge_pets.append(new_pet.pet_id)
            
            self.log_achievement(f"🐾 {profile.developer_name} received their first badge pet: {new_pet.pet_name} the {new_pet.species.value.replace('_', ' ').title()}!")
        
        # Update all pets for this developer
        for pet_id in profile.badge_pets:
            pet = self.pet_manager.get_pet_by_id(pet_id)
            if not pet:
                continue
            
            # Update pet metrics based on contribution
            pet.metrics.total_xp_earned = profile.total_xp
            pet.metrics.contributions_witnessed += 1
            pet.metrics.days_active = (datetime.datetime.now() - pet.birth_date).days
            
            # Type-specific metric updates
            if contribution.contribution_type == ContributionType.DEBUGGING_SESSION:
                pet.metrics.bugs_helped_squash += 1
            elif contribution.contribution_type == ContributionType.DOCUMENTATION:
                pet.metrics.documentation_assisted += 1
            elif contribution.contribution_type == ContributionType.INNOVATION:
                pet.metrics.innovations_inspired += 1
            
            # TLDL entry reactions (if metrics contain TLDL data)
            if "tldl_entry" in contribution.metrics:
                pet.metrics.tldl_reactions += 1
            
            # Scroll integrity checks (if metrics contain integrity data)
            if "scroll_integrity_score" in contribution.metrics:
                pet.metrics.scroll_integrity_checks += 1
            
            # Check for evolution
            if pet.can_evolve():
                old_stage = pet.current_stage.value
                if self.pet_manager.evolve_pet(pet):
                    self.log_achievement(f"🌟 {pet.pet_name} evolved from {old_stage} to {pet.current_stage.value}!")
                    
                    # Special message for legendary evolution (NFT ready)
                    if pet.evolution_complete:
                        self.log_achievement(f"🏆 {pet.pet_name} has reached Legendary status and is ready for NFT minting!")
            
            # Save pet updates
            self.pet_manager.save_pets_registry()

    def get_developer_profile(self, developer_name: str) -> Optional[DeveloperProfile]:
        """Get developer profile by name"""
        return self.developer_profiles.get(developer_name)

    def get_leaderboard(self, limit: int = 10) -> List[DeveloperProfile]:
        """Get top developers by XP"""
        sorted_profiles = sorted(self.developer_profiles.values(), key=lambda p: p.total_xp, reverse=True)
        return sorted_profiles[:limit]

    def spend_copilot_coins(self, developer_name: str, amount: int, item_description: str) -> bool:
        """Spend CopilotCoins for premium features"""
        try:
            if developer_name not in self.developer_profiles:
                return False
            
            profile = self.developer_profiles[developer_name]
            
            if profile.copilot_coins < amount:
                print(f"{Colors.WARNING}⚠️ [SHOP]{Colors.ENDC} Insufficient CopilotCoins! Need {amount}, have {profile.copilot_coins}")
                return False
            
            profile.copilot_coins -= amount
            profile.last_active = datetime.datetime.now()
            
            self.save_profiles()
            
            print(f"{Colors.OKGREEN}🪙 [SHOP]{Colors.ENDC} {developer_name} purchased: {item_description} (-{amount} coins)")
            print(f"{Colors.OKCYAN}💰 [BALANCE]{Colors.ENDC} Remaining balance: {profile.copilot_coins} CopilotCoins")
            
            return True
            
        except Exception as e:
            print(f"{Colors.FAIL}❌ [ERROR]{Colors.ENDC} Failed to process coin transaction: {e}")
            return False

    def spend_xp_for_refactoring(self, developer_name: str, xp_cost: int, refactoring_description: str, urgency: str = "normal") -> Dict[str, Any]:
        """
        Spend XP for costly automatic document refactoring
        
        Args:
            developer_name: Developer requesting refactoring
            xp_cost: XP cost for the refactoring operation
            refactoring_description: Description of what will be refactored
            urgency: "normal", "high", or "emergency" (affects cost multiplier)
            
        Returns:
            Dict with success status and transaction details
        """
        try:
            if developer_name not in self.developer_profiles:
                return {
                    "success": False,
                    "error": "Developer profile not found - create profile first with a contribution",
                    "xp_cost": xp_cost,
                    "balance": 0
                }
            
            profile = self.developer_profiles[developer_name]
            
            # Apply urgency multiplier
            urgency_multipliers = {
                "normal": 1.0,
                "high": 1.5, 
                "emergency": 2.0
            }
            
            final_cost = int(xp_cost * urgency_multipliers.get(urgency, 1.0))
            
            if profile.total_xp < final_cost:
                return {
                    "success": False,
                    "error": f"Insufficient XP! Need {final_cost}, have {profile.total_xp}",
                    "xp_cost": final_cost,
                    "balance": profile.total_xp,
                    "strategic_advice": self._get_strategic_advice(profile.total_xp, final_cost)
                }
            
            # Deduct XP (this is the "cost" mechanism)
            profile.total_xp -= final_cost
            profile.last_active = datetime.datetime.now()
            
            # Award CopilotCoins as compensation for XP spending (conversion rate)
            copilot_coins_earned = max(1, final_cost // 10)  # 10:1 XP to CopilotCoin conversion
            profile.copilot_coins += copilot_coins_earned
            
            # Record the refactoring transaction
            refactoring_record = {
                "timestamp": datetime.datetime.now().isoformat(),
                "description": refactoring_description,
                "xp_cost": final_cost,
                "urgency": urgency,
                "copilot_coins_earned": copilot_coins_earned
            }
            
            # Add to contribution history
            contribution_id = f"refactor_{int(datetime.datetime.now().timestamp())}"
            contribution = Contribution(
                contribution_id=contribution_id,
                developer_name=developer_name,
                contribution_type=ContributionType.REFACTORING,
                quality_level=QualityLevel.GOOD,  # Automatic refactoring is good quality
                description=f"📜 Faculty Standards Refactoring: {refactoring_description}",
                files_affected=[],
                timestamp=datetime.datetime.now(),
                base_xp=0,  # No base XP earned, this is spending
                quality_multiplier=1.0,
                total_xp=0,  # No XP earned
                coins_earned=copilot_coins_earned,
                metrics={"refactoring_cost": final_cost, "urgency": urgency}
            )
            
            profile.contributions.append(contribution)
            self.save_profiles()
            
            print(f"{Colors.PURPLE}📜 [REFACTOR]{Colors.ENDC} {developer_name} summoned Chronicler for Faculty standards compliance")
            print(f"{Colors.WARNING}💸 [COST]{Colors.ENDC} XP spent: {final_cost} (urgency: {urgency})")
            print(f"{Colors.OKGREEN}🪙 [REWARD]{Colors.ENDC} CopilotCoins earned: {copilot_coins_earned}")
            print(f"{Colors.OKCYAN}⭐ [BALANCE]{Colors.ENDC} Remaining XP: {profile.total_xp}")
            
            # Check for achievements
            self._check_refactoring_achievements(profile, final_cost)
            
            return {
                "success": True,
                "xp_spent": final_cost,
                "copilot_coins_earned": copilot_coins_earned,
                "remaining_xp": profile.total_xp,
                "refactoring_id": contribution_id,
                "strategic_advice": f"Refactoring approved! You now have {profile.total_xp} XP remaining for future operations."
            }
            
        except Exception as e:
            return {
                "success": False,
                "error": f"Refactoring transaction failed: {e}",
                "xp_cost": final_cost if 'final_cost' in locals() else xp_cost,
                "balance": profile.total_xp if 'profile' in locals() else 0
            }

    def _get_strategic_advice(self, current_xp: int, needed_xp: int) -> str:
        """Provide strategic advice for XP spending decisions"""
        deficit = needed_xp - current_xp
        
        if deficit <= 50:
            return f"Close call! You need {deficit} more XP. Consider a quick documentation contribution first."
        elif deficit <= 150:
            return f"Moderate deficit of {deficit} XP. A good debugging session or code contribution would cover this."
        elif deficit <= 300:
            return f"Significant XP gap of {deficit}. Consider earning XP through collaborative work or innovative solutions."
        else:
            return f"Large XP deficit of {deficit}. Emergency situation - consider breaking refactoring into smaller, affordable pieces."

    def _check_refactoring_achievements(self, profile: DeveloperProfile, xp_spent: int):
        """Check and award refactoring-related achievements"""
        refactoring_contributions = [c for c in profile.contributions if c.contribution_type == ContributionType.REFACTORING]
        total_refactoring_xp = sum(c.metrics.get("refactoring_cost", 0) for c in refactoring_contributions)
        
        achievements_to_award = []
        
        # First refactoring achievement
        if len(refactoring_contributions) == 1:
            achievements_to_award.append(Achievement(
                achievement_id=f"first_refactor_{profile.developer_name}",
                name="🧙‍♂️ Chronicler Summoner",
                description="Summoned the Chronicler for first Faculty standards refactoring",
                emoji="🧙‍♂️",
                badge_color="purple",
                date_earned=datetime.datetime.now(),
                contribution_id=refactoring_contributions[-1].contribution_id,
                faculty_signature="Chronicler Faculty Seal of Approval"
            ))
        
        # High-cost refactoring achievement
        if xp_spent >= 200:
            achievements_to_award.append(Achievement(
                achievement_id=f"high_cost_refactor_{int(datetime.datetime.now().timestamp())}",
                name="💸 XP Gambler",
                description=f"Spent {xp_spent} XP on high-stakes document refactoring",
                emoji="💸",
                badge_color="gold",
                date_earned=datetime.datetime.now(),
                contribution_id=refactoring_contributions[-1].contribution_id,
                faculty_signature="Faculty Doctrine, Vol. VII Witness"
            ))
            
        # Total XP spending milestone achievements
        if total_refactoring_xp >= 500:
            achievements_to_award.append(Achievement(
                achievement_id=f"refactor_master_{profile.developer_name}",
                name="📜 Refactoring Master",
                description=f"Invested {total_refactoring_xp} total XP in Faculty standards compliance",
                emoji="📜",
                badge_color="platinum",
                date_earned=datetime.datetime.now(),
                contribution_id=refactoring_contributions[-1].contribution_id,
                faculty_signature="Chronicle Keeper Supreme Seal"
            ))
        
        # Award achievements
        for achievement in achievements_to_award:
            if achievement.achievement_id not in [a.achievement_id for a in profile.achievements]:
                profile.achievements.append(achievement)
                print(f"{Colors.GOLD}🏆 [ACHIEVEMENT]{Colors.ENDC} {achievement.name}: {achievement.description}")

    def get_refactoring_affordability(self, developer_name: str, xp_cost: int) -> Dict[str, Any]:
        """
        Check if developer can afford refactoring and provide strategic guidance
        
        Args:
            developer_name: Developer name
            xp_cost: Proposed XP cost
            
        Returns:
            Dict with affordability analysis and recommendations
        """
        if developer_name not in self.developer_profiles:
            return {
                "affordable": False,
                "current_xp": 0,
                "cost": xp_cost,
                "recommendation": "Create developer profile first by making a contribution"
            }
            
        profile = self.developer_profiles[developer_name]
        current_xp = profile.total_xp
        
        return {
            "affordable": current_xp >= xp_cost,
            "current_xp": current_xp,
            "cost": xp_cost,
            "remaining_after": max(0, current_xp - xp_cost),
            "deficit": max(0, xp_cost - current_xp),
            "percentage_of_wealth": min(100, (xp_cost / max(1, current_xp)) * 100),
            "recommendation": self._get_affordability_recommendation(current_xp, xp_cost),
            "strategic_timing": self._get_strategic_timing_advice(current_xp, xp_cost)
        }

    def _get_affordability_recommendation(self, current_xp: int, cost: int) -> str:
        """Get recommendation based on XP affordability"""
        if current_xp >= cost:
            percentage = (cost / current_xp) * 100
            if percentage <= 25:
                return "✅ Excellent value - low impact on XP reserves"
            elif percentage <= 50:
                return "✅ Affordable - reasonable XP investment"
            elif percentage <= 75:
                return "⚠️ Significant cost - consider priority and timing"
            else:
                return "⚠️ High cost - major XP investment, evaluate carefully"
        else:
            deficit = cost - current_xp
            return f"❌ Cannot afford - need {deficit} more XP"

    def _get_strategic_timing_advice(self, current_xp: int, cost: int) -> str:
        """Provide strategic timing advice for refactoring decisions"""
        if current_xp >= cost * 2:
            return "Perfect timing - you have comfortable XP reserves"
        elif current_xp >= cost * 1.5:
            return "Good timing - adequate XP buffer for future needs"
        elif current_xp >= cost:
            return "Caution advised - this will significantly impact your XP reserves"
        else:
            return "Poor timing - consider earning more XP first or reducing refactoring scope"

    def award_daily_bonus(self, developer_name: str) -> bool:
        """Award daily login bonus"""
        try:
            if developer_name not in self.developer_profiles:
                self.developer_profiles[developer_name] = DeveloperProfile(developer_name=developer_name)
            
            profile = self.developer_profiles[developer_name]
            
            # Check if already awarded today
            today = datetime.datetime.now().date()
            if profile.last_active.date() == today:
                return False  # Already got daily bonus
            
            # Award daily bonus
            daily_coins = 10
            profile.copilot_coins += daily_coins
            profile.last_active = datetime.datetime.now()
            
            self.save_profiles()
            
            print(f"{Colors.OKGREEN}🪙 [DAILY]{Colors.ENDC} {developer_name} earned {daily_coins} CopilotCoins for daily activity!")
            
            return True
            
        except Exception as e:
            print(f"{Colors.FAIL}❌ [ERROR]{Colors.ENDC} Failed to award daily bonus: {e}")
            return False

    def get_developer_pets(self, developer_name: str) -> List[Any]:
        """Get all badge pets for a developer"""
        if not self.pet_manager:
            return []
        
        profile = self.get_developer_profile(developer_name)
        if not profile:
            return []
        
        pets = []
        for pet_id in profile.badge_pets:
            pet = self.pet_manager.get_pet_by_id(pet_id)
            if pet:
                pets.append(pet)
        
        return pets
    
    def export_pet_nft(self, developer_name: str, pet_id: str) -> Optional[str]:
        """Export NFT metadata for a developer's pet"""
        if not self.pet_manager:
            return None
        
        profile = self.get_developer_profile(developer_name)
        if not profile or pet_id not in profile.badge_pets:
            return None
        
        pet = self.pet_manager.get_pet_by_id(pet_id)
        if not pet:
            return None
        
        return self.pet_manager.export_nft_metadata(pet)
    
    def generate_copilot_personality(self, developer_name: str) -> Optional[Dict[str, Any]]:
        """Generate Copilot personality profile based on developer's pets"""
        if not self.pet_manager:
            return None
        
        pets = self.get_developer_pets(developer_name)
        if not pets:
            return None
        
        # Find the most evolved pet for personality generation
        legendary_pets = [pet for pet in pets if pet.evolution_complete]
        if legendary_pets:
            pet = legendary_pets[0]  # Use first legendary pet
            return pet.copilot_personality_traits
        
        # Use most evolved pet if no legendary pets
        if pets:
            most_evolved = max(pets, key=lambda p: list(PetStage).index(p.current_stage))
            if most_evolved.copilot_personality_traits:
                return most_evolved.copilot_personality_traits
        
        return None

    def save_profiles(self) -> bool:
        """Save all developer profiles"""
        try:
            profiles_data = {
                'version': '1.0',
                'last_updated': datetime.datetime.now().isoformat(),
                'profiles': {name: profile.to_dict() for name, profile in self.developer_profiles.items()}
            }
            
            with open(self.profiles_file, 'w', encoding='utf-8') as f:
                json.dump(profiles_data, f, indent=2, ensure_ascii=False)
            
            return True
            
        except Exception as e:
            print(f"{Colors.FAIL}❌ [ERROR]{Colors.ENDC} Failed to save profiles: {e}")
            return False

    def load_profiles(self) -> bool:
        """Load all developer profiles"""
        try:
            if not self.profiles_file.exists():
                return True
            
            with open(self.profiles_file, 'r', encoding='utf-8') as f:
                profiles_data = json.load(f)
            
            self.developer_profiles = {}
            for name, profile_data in profiles_data.get('profiles', {}).items():
                self.developer_profiles[name] = DeveloperProfile.from_dict(profile_data)
            
            return True
            
        except Exception as e:
            print(f"{Colors.WARNING}⚠️ [WARNING]{Colors.ENDC} Could not load profiles: {e}")
            return False


def main():
    """Developer Experience Manager CLI"""
    import argparse
    
    parser = argparse.ArgumentParser(
        description="🏆 Developer Experience & Achievement System",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Record contributions
  python3 dev_experience.py --record "Alice" debugging_session excellent "Fixed race condition in WFC system" --files "WfcSystem.cs" --metrics "fuck_moments_resolved:3,solution_clarity_score:0.9"
  
  # Check profile
  python3 dev_experience.py --profile "Alice"
  
  # View leaderboard
  python3 dev_experience.py --leaderboard
  
  # Spend coins
  python3 dev_experience.py --spend "Alice" 75 "Extra debugging session with Copilot"
  
  # Award daily bonus
  python3 dev_experience.py --daily-bonus "Alice"
  
  # Document refactoring operations
  python3 dev_experience.py --refactor "Alice" 150 "Fix TLDL metadata and sections" normal
  python3 dev_experience.py --check-afford "Alice" 150
  python3 dev_experience.py --refactor-analyze "TLDL/entries/problematic-entry.md"
        """
    )
    
    parser.add_argument('--workspace', default='.', help='Workspace directory path')
    
    # Record contribution
    parser.add_argument('--record', nargs=4, metavar=('DEVELOPER', 'TYPE', 'QUALITY', 'DESCRIPTION'),
                       help='Record a contribution')
    parser.add_argument('--files', help='Comma-separated list of affected files')
    parser.add_argument('--metrics', help='Comma-separated key:value metrics (e.g., "score:0.85,count:3")')
    
    # Profile operations
    parser.add_argument('--profile', help='Show developer profile')
    parser.add_argument('--leaderboard', action='store_true', help='Show XP leaderboard')
    parser.add_argument('--limit', type=int, default=10, help='Limit for leaderboard')
    
    # Coin operations
    parser.add_argument('--spend', nargs=3, metavar=('DEVELOPER', 'AMOUNT', 'DESCRIPTION'),
                       help='Spend CopilotCoins')
    parser.add_argument('--daily-bonus', help='Award daily bonus to developer')
    
    # Document refactoring operations
    parser.add_argument('--refactor', nargs=4, metavar=('DEVELOPER', 'XP_COST', 'DESCRIPTION', 'URGENCY'),
                       help='Spend XP for Faculty standards document refactoring')
    parser.add_argument('--check-afford', nargs=2, metavar=('DEVELOPER', 'XP_COST'),
                       help='Check if developer can afford refactoring cost')
    parser.add_argument('--refactor-analyze', help='Analyze document for refactoring needs and costs')
    
    args = parser.parse_args()
    
    try:
        # Create experience manager
        experience_manager = DeveloperExperienceManager(workspace_path=args.workspace)
        
        # Record contribution
        if args.record:
            developer, contrib_type, quality, description = args.record
            
            try:
                contribution_type = ContributionType(contrib_type)
                quality_level = QualityLevel(quality)
            except ValueError as e:
                print(f"{Colors.FAIL}❌ [ERROR]{Colors.ENDC} Invalid contribution type or quality: {e}")
                return
            
            files_affected = args.files.split(',') if args.files else []
            
            # Parse metrics
            metrics = {}
            if args.metrics:
                for metric in args.metrics.split(','):
                    if ':' in metric:
                        key, value = metric.split(':', 1)
                        # Try to parse as number, fall back to string
                        try:
                            if '.' in value:
                                metrics[key] = float(value)
                            else:
                                metrics[key] = int(value)
                        except ValueError:
                            metrics[key] = value
            
            contribution_id = experience_manager.record_contribution(
                developer, contribution_type, quality_level, description,
                files_affected, metrics
            )
            
            if contribution_id:
                print(f"{Colors.OKGREEN}✅ [SUCCESS]{Colors.ENDC} Recorded contribution: {contribution_id}")
        
        # Show profile
        elif args.profile:
            profile = experience_manager.get_developer_profile(args.profile)
            if profile:
                print(f"\n{Colors.HEADER}👤 Developer Profile: {profile.developer_name}{Colors.ENDC}")
                print(f"Level: {profile.level} ({profile.title})")
                print(f"Total XP: {profile.total_xp} ⭐")
                print(f"CopilotCoins: {profile.copilot_coins} 🪙")
                print(f"Contributions: {len(profile.contributions)}")
                print(f"Achievements: {len(profile.achievements)} 🏆")
                print(f"Faculty Badges: {len(profile.faculty_badges)} 🏅")
                
                if profile.achievements:
                    print(f"\n{Colors.GOLD}🏆 Recent Achievements:{Colors.ENDC}")
                    for achievement in profile.achievements[-5:]:  # Show last 5
                        print(f"  {achievement.emoji} {achievement.name}")
                        print(f"    {achievement.description}")
                
                if profile.faculty_badges:
                    print(f"\n{Colors.PURPLE}🏅 Faculty Badges:{Colors.ENDC}")
                    for badge in profile.faculty_badges:
                        print(f"  {badge}")
            else:
                print(f"{Colors.WARNING}⚠️ [WARNING]{Colors.ENDC} Developer '{args.profile}' not found")
        
        # Show leaderboard
        elif args.leaderboard:
            leaderboard = experience_manager.get_leaderboard(args.limit)
            if leaderboard:
                print(f"\n{Colors.HEADER}🏆 XP Leaderboard (Top {len(leaderboard)}){Colors.ENDC}")
                print("=" * 60)
                
                for i, profile in enumerate(leaderboard, 1):
                    rank_emoji = "🥇" if i == 1 else "🥈" if i == 2 else "🥉" if i == 3 else f"{i}."
                    print(f"{rank_emoji} {profile.developer_name} - {profile.total_xp} XP ({profile.title})")
                    print(f"    💰 {profile.copilot_coins} coins | 🏆 {len(profile.achievements)} achievements")
            else:
                print(f"{Colors.WARNING}⚠️ [INFO]{Colors.ENDC} No developers found")
        
        # Spend coins
        elif args.spend:
            developer, amount_str, description = args.spend
            try:
                amount = int(amount_str)
                experience_manager.spend_copilot_coins(developer, amount, description)
            except ValueError:
                print(f"{Colors.FAIL}❌ [ERROR]{Colors.ENDC} Invalid amount: {amount_str}")
        
        # Award daily bonus
        elif args.daily_bonus:
            if experience_manager.award_daily_bonus(args.daily_bonus):
                print(f"{Colors.OKGREEN}✅ [SUCCESS]{Colors.ENDC} Daily bonus awarded!")
            else:
                print(f"{Colors.WARNING}⚠️ [INFO]{Colors.ENDC} Daily bonus already awarded today")
        
        # Document refactoring operations
        elif args.refactor:
            developer, xp_cost_str, description, urgency = args.refactor
            try:
                xp_cost = int(xp_cost_str)
                result = experience_manager.spend_xp_for_refactoring(developer, xp_cost, description, urgency)
                
                if result["success"]:
                    print(f"{Colors.OKGREEN}✅ [REFACTOR SUCCESS]{Colors.ENDC} Chronicler summoned!")
                    print(f"💡 Strategic Advice: {result['strategic_advice']}")
                else:
                    print(f"{Colors.FAIL}❌ [REFACTOR FAILED]{Colors.ENDC} {result['error']}")
                    if "strategic_advice" in result:
                        print(f"💡 Strategic Advice: {result['strategic_advice']}")
                        
            except ValueError:
                print(f"{Colors.FAIL}❌ [ERROR]{Colors.ENDC} Invalid XP cost: {xp_cost_str}")
        
        elif args.check_afford:
            developer, xp_cost_str = args.check_afford
            try:
                xp_cost = int(xp_cost_str)
                affordability = experience_manager.get_refactoring_affordability(developer, xp_cost)
                
                print(f"{Colors.HEADER}💰 Refactoring Affordability Analysis{Colors.ENDC}")
                print(f"Developer: {developer}")
                print(f"Current XP: {affordability['current_xp']} ⭐")
                print(f"Refactoring Cost: {affordability['cost']} XP")
                print(f"Affordable: {'✅ Yes' if affordability['affordable'] else '❌ No'}")
                
                if affordability['affordable']:
                    print(f"Remaining After: {affordability['remaining_after']} XP")
                    print(f"Cost Percentage: {affordability['percentage_of_wealth']:.1f}% of total XP")
                else:
                    print(f"XP Deficit: {affordability['deficit']} XP needed")
                    
                print(f"💡 Recommendation: {affordability['recommendation']}")
                print(f"📅 Strategic Timing: {affordability['strategic_timing']}")
                
            except ValueError:
                print(f"{Colors.FAIL}❌ [ERROR]{Colors.ENDC} Invalid XP cost: {xp_cost_str}")
                
        elif args.refactor_analyze:
            # Import Faculty Standards Validator
            import sys
            sys.path.append('src/SymbolicLinter')
            try:
                from faculty_standards_validator import FacultyStandardsValidator
                
                file_path = Path(args.refactor_analyze)
                if file_path.exists():
                    content = file_path.read_text(encoding='utf-8')
                    validator = FacultyStandardsValidator()
                    needs = validator.validate_document(str(file_path), content)
                    cost_analysis = validator.calculate_total_refactoring_cost(needs)
                    
                    print(f"{Colors.HEADER}📜 Faculty Standards Analysis{Colors.ENDC}")
                    print(f"File: {file_path.name}")
                    print(f"Issues Found: {cost_analysis['needs_count']}")
                    print(f"Total Refactoring Cost: {cost_analysis['total_cost']} XP")
                    print(f"Strategic Recommendation: {cost_analysis['strategic_recommendation']}")
                    
                    if cost_analysis['quick_wins']:
                        print(f"\n💨 Quick Wins ({len(cost_analysis['quick_wins'])} items):")
                        for need in cost_analysis['quick_wins'][:3]:  # Show first 3
                            print(f"  • {need.description} (Cost: {need.final_cost} XP)")
                            
                    if cost_analysis['high_priority_needs']:
                        print(f"\n🚨 High Priority ({len(cost_analysis['high_priority_needs'])} items):")
                        for need in cost_analysis['high_priority_needs'][:3]:  # Show first 3
                            print(f"  • {need.description} (Cost: {need.final_cost} XP)")
                            
                    print(f"\n💡 Use --refactor to proceed with fixes")
                else:
                    print(f"{Colors.FAIL}❌ [ERROR]{Colors.ENDC} File not found: {args.refactor_analyze}")
                    
            except ImportError:
                print(f"{Colors.FAIL}❌ [ERROR]{Colors.ENDC} Faculty Standards Validator not available")
            except Exception as e:
                print(f"{Colors.FAIL}❌ [ERROR]{Colors.ENDC} Analysis failed: {e}")
        
        else:
            # Show general stats
            total_developers = len(experience_manager.developer_profiles)
            total_contributions = sum(len(p.contributions) for p in experience_manager.developer_profiles.values())
            total_achievements = sum(len(p.achievements) for p in experience_manager.developer_profiles.values())
            
            print(f"{Colors.HEADER}🏆 Developer Experience System{Colors.ENDC}")
            print(f"Active Developers: {total_developers}")
            print(f"Total Contributions: {total_contributions}")
            print(f"Total Achievements: {total_achievements}")
            print("Use --help to see available commands")
    
    except KeyboardInterrupt:
        print(f"\n{Colors.WARNING}⚠️ [INTERRUPTED]{Colors.ENDC} Developer experience manager interrupted")
    except Exception as e:
        print(f"{Colors.FAIL}❌ [ERROR]{Colors.ENDC} Experience manager error: {e}")


if __name__ == "__main__":
    main()
