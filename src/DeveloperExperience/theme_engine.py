#!/usr/bin/env python3
"""
Living Dev Agent XP System - Theme & Genre Engine
Jerry's personality-driven developer experience with thematic flair

Theme changes the FLAVOR of the joy - same XP system, different narratives!
"""

import json
import os
import datetime
from pathlib import Path
from typing import Dict, List, Any, Optional
from enum import Enum
from dataclasses import dataclass, asdict

class DeveloperGenre(Enum):
    """Available developer experience genres"""
    CYBERPUNK = "cyberpunk"           # Matrix/GitLab hacker aesthetic
    MYTHICAL = "mythical"             # Gods and legends of code
    FANTASY = "fantasy"               # Wizards and magic spells
    APOCALYPTIC = "apocalyptic"       # Surviving the code wasteland
    UTOPIAN = "utopian"               # Perfect harmony development
    STEAMPUNK = "steampunk"           # Victorian tech mastery
    SPACE_OPERA = "space_opera"       # Galactic empire building
    DETECTIVE_NOIR = "detective_noir" # Debugging mysteries
    PIRATE = "pirate"                 # Code buccaneer adventures
    SUPERHERO = "superhero"           # Save the codebase!

@dataclass
class ThemeColors:
    """Theme-specific color schemes"""
    primary: str
    secondary: str
    accent: str
    success: str
    warning: str
    error: str
    xp_gain: str
    level_up: str

@dataclass
class GenreTheme:
    """Complete genre theme definition"""
    name: str
    description: str
    colors: ThemeColors
    emojis: Dict[str, str]
    terminology: Dict[str, str]
    achievement_templates: Dict[str, str]
    level_titles: List[str]
    coin_name: str
    currency_symbol: str
    narrator_voice: str

class GenreThemeManager:
    """Manages theme switching and genre-specific content"""
    
    def __init__(self, workspace_path: str = "."):
        self.workspace_path = Path(workspace_path)
        self.themes_dir = self.workspace_path / "experience" / "themes"
        self.themes_dir.mkdir(parents=True, exist_ok=True)
        
        self.current_theme_file = self.themes_dir / "current_theme.json"
        self.current_genre = DeveloperGenre.FANTASY  # Default Jerry theme
        
        # Initialize built-in themes
        self.built_in_themes = self._create_built_in_themes()
        self.load_current_theme()

    def _create_built_in_themes(self) -> Dict[DeveloperGenre, GenreTheme]:
        """Create all built-in theme definitions"""
        themes = {}
        
        # 🧙‍♂️ FANTASY (Jerry's Default)
        themes[DeveloperGenre.FANTASY] = GenreTheme(
            name="Fantasy Wizard",
            description="Jerry's magical coding adventure with spells and wizardry",
            colors=ThemeColors(
                primary='\033[95m',      # Purple
                secondary='\033[94m',    # Blue  
                accent='\033[96m',       # Cyan
                success='\033[92m',      # Green
                warning='\033[93m',      # Yellow
                error='\033[91m',        # Red
                xp_gain='\033[92m',      # Green
                level_up='\033[95m'      # Purple
            ),
            emojis={
                'xp': '⭐', 'level_up': '🎉', 'achievement': '🏆', 'coin': '🪙',
                'success': '✅', 'warning': '⚠️', 'error': '❌', 'info': '🔍',
                'build': '🔨', 'deploy': '🚀', 'debug': '🧙‍♂️', 'innovation': '💡'
            },
            terminology={
                'developer': 'Wizard', 'contribution': 'Spell', 'commit': 'Incantation',
                'bug': 'Curse', 'feature': 'Enchantment', 'build': 'Ritual',
                'deploy': 'Summoning', 'review': 'Scrying', 'documentation': 'Scroll',
                'test': 'Divination', 'refactor': 'Transmutation'
            },
            achievement_templates={
                'first_steps': '🧙‍♂️ Apprentice Awakening - Cast your first spell in the codebase',
                'debug_detective': '🔮 Curse Breaker - Shattered 5 curses with divine insight',
                'legendary_innovation': '⚡ Archmage Ascension - Achieved legendary magical prowess',
                'triple_fuck_slayer': '🔥 Demon Slayer - Banished 3+ demons in a single ritual'
            },
            level_titles=[
                '🌱 Seedling Apprentice', '🌿 Growing Adept', '🌳 Seasoned Mage',
                '🏔️ Mountain Sage', '⚡ Code Wizard', '🧙‍♂️ Master Enchanter', '🌟 Legendary Archmage'
            ],
            coin_name="ManaCoins",
            currency_symbol="🔮",
            narrator_voice="An ancient wizard chronicling your magical journey through the realm of code"
        )
        
        # 🤖 CYBERPUNK
        themes[DeveloperGenre.CYBERPUNK] = GenreTheme(
            name="Cyberpunk Hacker",
            description="Matrix-style digital rebellion with neon aesthetics",
            colors=ThemeColors(
                primary='\033[96m',      # Cyan (matrix green)
                secondary='\033[95m',    # Magenta
                accent='\033[93m',       # Yellow (neon)
                success='\033[92m',      # Green
                warning='\033[91m',      # Red
                error='\033[41m',        # Red background
                xp_gain='\033[96m',      # Cyan
                level_up='\033[95m'      # Magenta
            ),
            emojis={
                'xp': '💎', 'level_up': '⚡', 'achievement': '🏅', 'coin': '₿',
                'success': '✓', 'warning': '⚠', 'error': '✗', 'info': '🔍',
                'build': '⚙️', 'deploy': '📡', 'debug': '🤖', 'innovation': '💡'
            },
            terminology={
                'developer': 'Hacker', 'contribution': 'Exploit', 'commit': 'Upload',
                'bug': 'Virus', 'feature': 'Module', 'build': 'Compile',
                'deploy': 'Inject', 'review': 'Scan', 'documentation': 'Database',
                'test': 'Trace', 'refactor': 'Optimize'
            },
            achievement_templates={
                'first_steps': '🤖 System Access - First successful intrusion into the codebase',
                'debug_detective': '🔍 Virus Hunter - Eliminated 5 system infections',
                'legendary_innovation': '⚡ Matrix Master - Achieved legendary cyber-prowess',
                'triple_fuck_slayer': '💀 Bug Terminator - Exterminated 3+ critical system errors'
            },
            level_titles=[
                '🔰 Script Kiddie', '💻 Code Runner', '🔧 System Admin',
                '🛡️ Security Expert', '🤖 Cyber Warrior', '👑 Net Samurai', '⚡ Digital God'
            ],
            coin_name="CyberCredits",
            currency_symbol="₿",
            narrator_voice="An AI chronicling your digital rebellion against buggy legacy systems"
        )
        
        # ⚔️ MYTHICAL
        themes[DeveloperGenre.MYTHICAL] = GenreTheme(
            name="Mythical Hero",
            description="Epic legends and godlike programming feats",
            colors=ThemeColors(
                primary='\033[93m',      # Gold
                secondary='\033[94m',    # Blue
                accent='\033[91m',       # Red
                success='\033[92m',      # Green
                warning='\033[93m',      # Yellow
                error='\033[91m',        # Red
                xp_gain='\033[93m',      # Gold
                level_up='\033[91m'      # Red
            ),
            emojis={
                'xp': '⚔️', 'level_up': '👑', 'achievement': '🏛️', 'coin': '🥇',
                'success': '⚡', 'warning': '🌩️', 'error': '💀', 'info': '📜',
                'build': '🔨', 'deploy': '🚀', 'debug': '⚔️', 'innovation': '🔱'
            },
            terminology={
                'developer': 'Hero', 'contribution': 'Quest', 'commit': 'Victory',
                'bug': 'Monster', 'feature': 'Artifact', 'build': 'Forge',
                'deploy': 'Conquest', 'review': 'Prophecy', 'documentation': 'Chronicle',
                'test': 'Trial', 'refactor': 'Reforging'
            },
            achievement_templates={
                'first_steps': '⚔️ Hero\'s Journey - Embarked on your first epic quest',
                'debug_detective': '🏛️ Monster Slayer - Defeated 5 code beasts in glorious combat',
                'legendary_innovation': '👑 Godlike Ascension - Achieved mythical status among mortals',
                'triple_fuck_slayer': '⚡ Titan Crusher - Conquered 3+ legendary monsters in one battle'
            },
            level_titles=[
                '🥉 Mortal Apprentice', '🥈 Blessed Warrior', '🥇 Epic Hero',
                '⚔️ Legendary Champion', '👑 Mythical Demigod', '⚡ Divine Avatar', '🌟 Immortal Legend'
            ],
            coin_name="GloryPoints",
            currency_symbol="🏆",
            narrator_voice="The Oracle of Code, weaving your deeds into the eternal myths of development"
        )
        
        # 💀 APOCALYPTIC
        themes[DeveloperGenre.APOCALYPTIC] = GenreTheme(
            name="Post-Apocalyptic Survivor",
            description="Surviving the wasteland of legacy code and technical debt",
            colors=ThemeColors(
                primary='\033[91m',      # Red
                secondary='\033[90m',    # Dark gray
                accent='\033[93m',       # Yellow (radiation)
                success='\033[92m',      # Green
                warning='\033[93m',      # Yellow
                error='\033[101m',       # Red background
                xp_gain='\033[91m',      # Red
                level_up='\033[93m'      # Yellow
            ),
            emojis={
                'xp': '☢️', 'level_up': '💀', 'achievement': '🏴‍☠️', 'coin': '🧟',
                'success': '🔥', 'warning': '☣️', 'error': '💥', 'info': '📻',
                'build': '🔧', 'deploy': '📡', 'debug': '💀', 'innovation': '⚗️'
            },
            terminology={
                'developer': 'Survivor', 'contribution': 'Salvage', 'commit': 'Cache',
                'bug': 'Mutation', 'feature': 'Upgrade', 'build': 'Craft',
                'deploy': 'Broadcast', 'review': 'Scout', 'documentation': 'Log',
                'test': 'Experiment', 'refactor': 'Rebuild'
            },
            achievement_templates={
                'first_steps': '💀 Wasteland Walker - Survived your first day in the code wasteland',
                'debug_detective': '☢️ Mutation Hunter - Eliminated 5 radioactive code anomalies',
                'legendary_innovation': '🏴‍☠️ Apocalypse Lord - Mastered survival in the digital wasteland',
                'triple_fuck_slayer': '💥 Nuke Specialist - Obliterated 3+ catastrophic system failures'
            },
            level_titles=[
                '🧟 Code Zombie', '💀 Wasteland Scavenger', '☢️ Radiation Survivor',
                '🔧 Tech Salvager', '🏴‍☠️ Chaos Lord', '💥 Apocalypse Master', '🌟 Digital Overlord'
            ],
            coin_name="WastelandCaps",
            currency_symbol="☢️",
            narrator_voice="A grizzled survivor's journal, documenting the struggle against the code apocalypse"
        )
        
        # 🌈 UTOPIAN
        themes[DeveloperGenre.UTOPIAN] = GenreTheme(
            name="Utopian Builder",
            description="Creating perfect harmony in code and collaboration",
            colors=ThemeColors(
                primary='\033[96m',      # Cyan
                secondary='\033[94m',    # Blue
                accent='\033[95m',       # Magenta
                success='\033[92m',      # Green
                warning='\033[93m',      # Yellow
                error='\033[91m',        # Red
                xp_gain='\033[96m',      # Cyan
                level_up='\033[95m'      # Magenta
            ),
            emojis={
                'xp': '🌟', 'level_up': '🌈', 'achievement': '🏆', 'coin': '💎',
                'success': '✨', 'warning': '💫', 'error': '🌪️', 'info': '💭',
                'build': '🏗️', 'deploy': '🚀', 'debug': '🔬', 'innovation': '💡'
            },
            terminology={
                'developer': 'Architect', 'contribution': 'Creation', 'commit': 'Gift',
                'bug': 'Disharmony', 'feature': 'Vision', 'build': 'Construct',
                'deploy': 'Manifest', 'review': 'Collaborate', 'documentation': 'Wisdom',
                'test': 'Validate', 'refactor': 'Perfect'
            },
            achievement_templates={
                'first_steps': '🌟 Harmony Begun - Contributed your first creation to the perfect world',
                'debug_detective': '✨ Discord Resolver - Restored harmony by resolving 5 disruptions',
                'legendary_innovation': '🌈 Utopia Architect - Achieved perfect mastery of creation',
                'triple_fuck_slayer': '💫 Chaos Harmonizer - Transformed 3+ major disruptions into beauty'
            },
            level_titles=[
                '🌱 Harmony Seeker', '🌿 Peace Builder', '🌳 Unity Architect',
                '🌈 Perfection Crafter', '✨ Harmony Master', '💎 Utopia Designer', '🌟 Perfect Creator'
            ],
            coin_name="HarmonyPoints",
            currency_symbol="✨",
            narrator_voice="The collective consciousness celebrating your contributions to the perfect codebase"
        )
        
        # ⚙️ STEAMPUNK
        themes[DeveloperGenre.STEAMPUNK] = GenreTheme(
            name="Victorian Engineer",
            description="Brass gears, steam power, and mechanical code mastery",
            colors=ThemeColors(
                primary='\033[93m',      # Yellow (brass)
                secondary='\033[91m',    # Red (copper)
                accent='\033[90m',       # Dark gray (iron)
                success='\033[92m',      # Green
                warning='\033[93m',      # Yellow
                error='\033[91m',        # Red
                xp_gain='\033[93m',      # Yellow
                level_up='\033[91m'      # Red
            ),
            emojis={
                'xp': '⚙️', 'level_up': '🎩', 'achievement': '🏅', 'coin': '🔩',
                'success': '✅', 'warning': '⚠️', 'error': '💥', 'info': '🔍',
                'build': '🔨', 'deploy': '🚂', 'debug': '🔧', 'innovation': '💡'
            },
            terminology={
                'developer': 'Engineer', 'contribution': 'Invention', 'commit': 'Blueprint',
                'bug': 'Malfunction', 'feature': 'Mechanism', 'build': 'Assembly',
                'deploy': 'Launch', 'review': 'Inspection', 'documentation': 'Manual',
                'test': 'Calibration', 'refactor': 'Reengineering'
            },
            achievement_templates={
                'first_steps': '⚙️ Apprentice Tinker - Built your first mechanical contribution',
                'debug_detective': '🔧 Master Mechanic - Fixed 5 complex mechanical failures',
                'legendary_innovation': '🎩 Grand Inventor - Achieved mastery of mechanical engineering',
                'triple_fuck_slayer': '💥 Catastrophe Engineer - Prevented 3+ mechanical disasters'
            },
            level_titles=[
                '🔩 Apprentice Tinker', '⚙️ Junior Engineer', '🔧 Master Mechanic',
                '🎩 Chief Inventor', '🚂 Steam Master', '💎 Gear Virtuoso', '🌟 Legendary Artificer'
            ],
            coin_name="CogCoins",
            currency_symbol="⚙️",
            narrator_voice="A Victorian gentleman's journal documenting mechanical marvels of code"
        )
        
        return themes

    def get_current_theme(self) -> GenreTheme:
        """Get the currently active theme"""
        return self.built_in_themes[self.current_genre]

    def set_theme(self, genre: DeveloperGenre) -> bool:
        """Switch to a different theme"""
        try:
            if genre not in self.built_in_themes:
                return False
            
            self.current_genre = genre
            self.save_current_theme()
            
            print(f"{self.get_themed_emoji('success')} Theme switched to: {self.get_current_theme().name}")
            print(f"🎭 {self.get_current_theme().description}")
            
            return True
            
        except Exception as e:
            print(f"❌ Failed to switch theme: {e}")
            return False

    def get_themed_emoji(self, emoji_type: str) -> str:
        """Get themed emoji for current genre"""
        theme = self.get_current_theme()
        return theme.emojis.get(emoji_type, '❓')

    def get_themed_term(self, term: str) -> str:
        """Get themed terminology for current genre"""
        theme = self.get_current_theme()
        return theme.terminology.get(term, term.title())

    def get_themed_achievement(self, achievement_id: str, fallback: str) -> str:
        """Get themed achievement description"""
        theme = self.get_current_theme()
        return theme.achievement_templates.get(achievement_id, fallback)

    def get_level_title(self, level: int) -> str:
        """Get themed level title"""
        theme = self.get_current_theme()
        if 1 <= level <= len(theme.level_titles):
            return theme.level_titles[level - 1]
        return f"Level {level} {self.get_themed_term('developer')}"

    def get_themed_color(self, color_type: str) -> str:
        """Get themed color code"""
        theme = self.get_current_theme()
        return getattr(theme.colors, color_type, '\033[0m')

    def format_themed_message(self, message: str, message_type: str = 'info') -> str:
        """Format message with current theme colors and emojis"""
        color = self.get_themed_color(message_type)
        emoji = self.get_themed_emoji(message_type)
        reset = '\033[0m'
        
        return f"{color}{emoji} {message}{reset}"

    def get_narrator_voice(self) -> str:
        """Get the narrator voice description for current theme"""
        return self.get_current_theme().narrator_voice

    def list_available_themes(self) -> Dict[str, str]:
        """List all available themes with descriptions"""
        return {genre.value: theme.description for genre, theme in self.built_in_themes.items()}

    def save_current_theme(self) -> bool:
        """Save current theme selection"""
        try:
            theme_data = {
                'current_genre': self.current_genre.value,
                'last_updated': str(datetime.datetime.now().isoformat())
            }
            
            with open(self.current_theme_file, 'w', encoding='utf-8') as f:
                json.dump(theme_data, f, indent=2)
            
            return True
            
        except Exception as e:
            print(f"❌ Failed to save theme: {e}")
            return False

    def load_current_theme(self) -> bool:
        """Load current theme selection"""
        try:
            if not self.current_theme_file.exists():
                return True  # Use default
            
            with open(self.current_theme_file, 'r', encoding='utf-8') as f:
                theme_data = json.load(f)
            
            genre_value = theme_data.get('current_genre', 'fantasy')
            self.current_genre = DeveloperGenre(genre_value)
            
            return True
            
        except Exception as e:
            print(f"⚠️ Could not load theme, using default: {e}")
            self.current_genre = DeveloperGenre.FANTASY
            return False


def main():
    """Theme selection CLI"""
    import argparse
    
    parser = argparse.ArgumentParser(description="🎭 Developer Experience Theme Selection")
    parser.add_argument('--workspace', default='.', help='Workspace directory path')
    parser.add_argument('--set-theme', help='Set theme genre')
    parser.add_argument('--list-themes', action='store_true', help='List available themes')
    parser.add_argument('--current-theme', action='store_true', help='Show current theme')
    parser.add_argument('--preview', help='Preview theme without switching')
    
    args = parser.parse_args()
    
    try:
        theme_manager = GenreThemeManager(workspace_path=args.workspace)
        
        if args.list_themes:
            print("🎭 Available Developer Experience Themes:")
            print("=" * 50)
            
            themes = theme_manager.list_available_themes()
            for genre, description in themes.items():
                current = " (CURRENT)" if genre == theme_manager.current_genre.value else ""
                print(f"🎮 {genre}{current}")
                print(f"   {description}")
                print()
        
        elif args.current_theme:
            theme = theme_manager.get_current_theme()
            print(f"🎭 Current Theme: {theme.name}")
            print(f"📖 {theme.description}")
            print(f"🗣️ Narrator: {theme.narrator_voice}")
            print(f"💰 Currency: {theme.coin_name} {theme.currency_symbol}")
            
            print(f"\n🏆 Level Progression:")
            for i, title in enumerate(theme.level_titles, 1):
                print(f"  {i}. {title}")
        
        elif args.set_theme:
            try:
                genre = DeveloperGenre(args.set_theme)
                if theme_manager.set_theme(genre):
                    theme = theme_manager.get_current_theme()
                    print(f"🎭 Successfully switched to: {theme.name}")
                    print(f"🗣️ {theme.narrator_voice}")
                else:
                    print("❌ Failed to switch theme")
            except ValueError:
                print(f"❌ Invalid theme: {args.set_theme}")
                print("Use --list-themes to see available options")
        
        elif args.preview:
            try:
                genre = DeveloperGenre(args.preview)
                theme = theme_manager.built_in_themes[genre]
                
                print(f"🎭 Preview: {theme.name}")
                print(f"📖 {theme.description}")
                print(f"🗣️ {theme.narrator_voice}")
                print(f"💰 Currency: {theme.coin_name} {theme.currency_symbol}")
                
                # Show sample messages
                print(f"\n🎨 Sample Experience:")
                print(f"  XP Gain: {theme.emojis['xp']} You earned 150 XP for legendary {theme.terminology['contribution'].lower()}!")
                print(f"  Level Up: {theme.emojis['level_up']} Congratulations! You are now a {theme.level_titles[2]}!")
                print(f"  Achievement: {theme.emojis['achievement']} {theme.achievement_templates.get('first_steps', 'Achievement unlocked!')}")
                
            except ValueError:
                print(f"❌ Invalid theme: {args.preview}")
        
        else:
            theme = theme_manager.get_current_theme()
            print(f"🎭 Current Theme: {theme.name}")
            print("Use --list-themes to see all available themes")
            print("Use --set-theme <genre> to switch themes")
            print("Use --preview <genre> to preview a theme")
            
    except KeyboardInterrupt:
        print("\n⚠️ Theme selection interrupted")
    except Exception as e:
        print(f"❌ Theme system error: {e}")


if __name__ == "__main__":
    main()
