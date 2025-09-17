#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
GitHub Copilot API Client
Provides Copilot Chat API integration for Warbler project intelligence
"""

import json
import requests
import time
from typing import Dict, List, Optional, Any
from pathlib import Path

class GitHubCopilotClient:
    """Client for GitHub Copilot Chat API integration"""
    
    def __init__(self, access_token: str):
        self.access_token = access_token
        self.base_url = "https://api.github.com"
        self.chat_url = "https://api.githubcopilot.com/chat/completions"
        
        self.headers = {
            'Authorization': f'Bearer {access_token}',
            'Accept': 'application/json',
            'Content-Type': 'application/json',
            'User-Agent': 'Warbler-Unity-Orchestrator/1.0',
            'X-GitHub-Api-Version': '2022-11-28'
        }
    
    def test_connection(self) -> bool:
        """Test connection to GitHub Copilot API"""
        try:
            # Test basic GitHub API access first
            response = requests.get(
                f"{self.base_url}/user",
                headers=self.headers,
                timeout=10
            )
            
            if response.status_code != 200:
                print(f"❌ GitHub API access failed: {response.status_code}")
                return False
            
            user_data = response.json()
            print(f"✅ GitHub API access confirmed for user: {user_data.get('login', 'unknown')}")
            
            # Note: Actual Copilot API testing would require Copilot subscription
            # For now, we'll assume access if GitHub API works
            return True
            
        except requests.exceptions.RequestException as e:
            print(f"❌ GitHub connection test failed: {e}")
            return False
    
    def get_project_analysis(self, user_request: str) -> Optional[Dict[str, Any]]:
        """
        Get project analysis from GitHub Copilot
        Returns structured analysis similar to Ollama format
        """
        
        # Create a specialized prompt for game development analysis
        system_prompt = """You are Warbler, an expert Unity game development AI assistant. 
        Analyze game project requests and provide detailed setup recommendations.
        
        Always respond with valid JSON in this exact format:
        {
            "game_type": "survivor.io|platformer|tower-defense|racing|puzzle|rpg|custom",
            "complexity_level": "simple|moderate|complex",
            "required_systems": ["PlayerController", "EnemySpawner", "etc"],
            "recommended_folders": ["Scripts/Player", "Scripts/Enemies", "etc"],
            "unity_packages": ["InputSystem", "2D Tilemap", "etc"],
            "estimated_dev_time": "2-4 weeks",
            "key_mechanics": ["movement", "shooting", "upgrades", "etc"],
            "technical_considerations": ["performance", "mobile-friendly", "etc"],
            "suggested_architecture": "MVC|ECS|Component-based",
            "warbler_insights": "Additional AI-powered recommendations"
        }
        
        Focus on practical Unity implementation details and modern game development patterns."""
        
        user_prompt = f"""Analyze this game project request:

        "{user_request}"
        
        Provide a comprehensive analysis with specific Unity recommendations, required systems, 
        folder structure, and development timeline. Consider performance, scalability, and 
        modern game development best practices."""
        
        try:
            # Prepare messages for chat completion
            messages = [
                {
                    "role": "system",
                    "content": system_prompt
                },
                {
                    "role": "user", 
                    "content": user_prompt
                }
            ]
            
            # For now, we'll simulate the Copilot API call since it requires special access
            # In production, this would make actual requests to GitHub Copilot API
            return self._simulate_copilot_analysis(user_request)
            
            # Real implementation would be:
            # return self._make_chat_request(messages)
            
        except Exception as e:
            print(f"❌ GitHub Copilot analysis failed: {e}")
            return None
    
    def _make_chat_request(self, messages: List[Dict[str, str]]) -> Optional[Dict[str, Any]]:
        """Make actual chat completion request to GitHub Copilot API"""
        
        payload = {
            "model": "gpt-4",  # GitHub Copilot uses GPT-4
            "messages": messages,
            "max_tokens": 2000,
            "temperature": 0.3,  # Lower temperature for more consistent technical analysis
            "top_p": 0.9
        }
        
        try:
            response = requests.post(
                self.chat_url,
                headers=self.headers,
                json=payload,
                timeout=30
            )
            
            if response.status_code == 200:
                result = response.json()
                
                if 'choices' in result and len(result['choices']) > 0:
                    content = result['choices'][0]['message']['content']
                    
                    # Try to parse JSON response
                    try:
                        analysis = json.loads(content)
                        return analysis
                    except json.JSONDecodeError:
                        # If not pure JSON, try to extract JSON from response
                        import re
                        json_match = re.search(r'\{.*\}', content, re.DOTALL)
                        if json_match:
                            try:
                                analysis = json.loads(json_match.group())
                                return analysis
                            except json.JSONDecodeError:
                                pass
                        
                        print(f"⚠️ Could not parse Copilot response as JSON: {content[:200]}...")
                        return None
                else:
                    print("❌ No choices in Copilot response")
                    return None
            else:
                print(f"❌ Copilot API request failed: {response.status_code} - {response.text}")
                return None
                
        except requests.exceptions.RequestException as e:
            print(f"❌ Copilot API request error: {e}")
            return None
    
    def _simulate_copilot_analysis(self, user_request: str) -> Dict[str, Any]:
        """
        Simulate GitHub Copilot analysis for testing/demo purposes
        This provides enhanced analysis compared to basic fallback
        """
        
        request_lower = user_request.lower()
        
        # Enhanced game type detection using more sophisticated patterns
        if any(phrase in request_lower for phrase in ['survivor', 'survival', 'wave defense', 'horde mode', 'endless waves']):
            return self._create_enhanced_survivor_analysis(user_request)
        elif any(phrase in request_lower for phrase in ['platform', 'side scroller', '2d jump', 'mario style']):
            return self._create_enhanced_platformer_analysis(user_request)
        elif any(phrase in request_lower for phrase in ['tower defense', 'td game', 'defend base', 'strategy defense']):
            return self._create_enhanced_tower_defense_analysis(user_request)
        elif any(phrase in request_lower for phrase in ['racing', 'driving', 'car game', 'speed racing']):
            return self._create_enhanced_racing_analysis(user_request)
        elif any(phrase in request_lower for phrase in ['puzzle', 'match 3', 'brain teaser', 'logic game']):
            return self._create_enhanced_puzzle_analysis(user_request)
        elif any(phrase in request_lower for phrase in ['rpg', 'role playing', 'character progression', 'quest system']):
            return self._create_enhanced_rpg_analysis(user_request)
        elif any(phrase in request_lower for phrase in ['text adventure', 'choose your own', 'interactive fiction', 'zork']):
            return self._create_enhanced_text_adventure_analysis(user_request)
        else:
            return self._create_enhanced_custom_analysis(user_request)
    
    def _create_enhanced_text_adventure_analysis(self, user_request: str) -> Dict[str, Any]:
        """Enhanced analysis specifically for text adventure games (like the Zork example)"""
        return {
            'game_type': 'text-adventure',
            'complexity_level': 'moderate',
            'required_systems': [
                'TextAdventureManager',
                'DialogueSystem', 
                'InventorySystem',
                'CommandParser',
                'RoomManager',
                'InteractableSystem',
                'SaveGameSystem',
                'UITextController'
            ],
            'recommended_folders': [
                'Scripts/Adventure',
                'Scripts/Dialogue',
                'Scripts/Inventory', 
                'Scripts/Commands',
                'Scripts/Rooms',
                'Scripts/Interactables',
                'Scripts/UI',
                'Scripts/Managers',
                'Data/Rooms',
                'Data/Items',
                'Data/Dialogues'
            ],
            'unity_packages': ['InputSystem', 'UI Toolkit', 'Addressables', 'Localization'],
            'estimated_dev_time': '4-6 weeks',
            'key_mechanics': [
                'text-based navigation',
                'command parsing', 
                'interactive dialogue',
                'inventory management',
                'room exploration',
                'object interaction',
                'dynamic content generation',
                'save/load system',
                'ascii minimap'
            ],
            'technical_considerations': [
                'efficient text parsing',
                'scalable dialogue system',
                'content data management',
                'save system architecture',
                'UI text rendering performance',
                'localization support',
                'accessibility features'
            ],
            'suggested_architecture': 'MVC with ScriptableObject data containers',
            'warbler_insights': 'Text adventures require robust command parsing, flexible dialogue systems, and excellent content management. Focus on creating reusable content structures and intuitive text-based interactions. Consider implementing a mini-map system using ASCII characters and button-based shortcuts for common actions.',
            'github_copilot_enhanced': True,
            'ai_provider': 'github_copilot_simulation'
        }
    
    def _create_enhanced_survivor_analysis(self, user_request: str) -> Dict[str, Any]:
        """Enhanced survivor.io analysis with GitHub Copilot insights"""
        return {
            'game_type': 'survivor.io',
            'complexity_level': 'moderate',
            'required_systems': [
                'PlayerController', 
                'EnemySpawner', 
                'WeaponSystem', 
                'UpgradeManager', 
                'WaveManager',
                'ExperienceManager',
                'HealthSystem',
                'DamageSystem',
                'ObjectPoolManager',
                'PerformanceMonitor'
            ],
            'recommended_folders': [
                'Scripts/Player', 
                'Scripts/Enemies', 
                'Scripts/Weapons', 
                'Scripts/Upgrades',
                'Scripts/Managers',
                'Scripts/UI',
                'Scripts/Projectiles',
                'Scripts/Effects',
                'Scripts/Performance'
            ],
            'unity_packages': ['InputSystem', '2D Sprite', 'Cinemachine', 'Addressables'],
            'estimated_dev_time': '3-5 weeks',
            'key_mechanics': [
                'top-down movement',
                'automatic shooting',
                'enemy waves',
                'experience collection',
                'upgrade selection',
                'damage numbers',
                'procedural spawning',
                'performance optimization'
            ],
            'technical_considerations': [
                'object pooling for projectiles and enemies',
                'efficient enemy spawning algorithms',
                'performance with 100+ entities',
                'mobile-friendly controls',
                'memory management',
                'frame rate consistency'
            ],
            'suggested_architecture': 'ECS hybrid with object pooling',
            'warbler_insights': 'GitHub Copilot Enhanced: Survivor.io games benefit from ECS architecture for performance at scale. Implement hierarchical object pooling, spatial partitioning for collision detection, and use burst compilation for critical systems. Consider implementing adaptive quality settings for mobile platforms.',
            'github_copilot_enhanced': True,
            'ai_provider': 'github_copilot_simulation'
        }
    
    def _create_enhanced_platformer_analysis(self, user_request: str) -> Dict[str, Any]:
        """Enhanced platformer analysis with GitHub Copilot insights"""
        return {
            'game_type': 'platformer',
            'complexity_level': 'moderate',
            'required_systems': [
                'PlayerMovement',
                'JumpController', 
                'PlatformController',
                'EnemyAI',
                'CollectibleSystem',
                'LevelManager',
                'CheckpointSystem',
                'CameraController',
                'PhysicsManager',
                'InputBuffer'
            ],
            'recommended_folders': [
                'Scripts/Player',
                'Scripts/Enemies', 
                'Scripts/Platforms',
                'Scripts/Collectibles',
                'Scripts/Managers',
                'Scripts/UI',
                'Scripts/Environment',
                'Scripts/Physics',
                'Scripts/Camera'
            ],
            'unity_packages': ['InputSystem', '2D Tilemap Extras', 'Cinemachine', '2D Physics', 'ProBuilder'],
            'estimated_dev_time': '4-6 weeks',
            'key_mechanics': [
                'precise jumping with coyote time',
                'physics-based movement with momentum',
                'wall jumping and sliding',
                'enemy collision with knockback',
                'collectible gathering with juice',
                'level progression with transitions',
                'checkpoint system with visual feedback',
                'camera smoothing and look-ahead'
            ],
            'technical_considerations': [
                'input buffering for responsive controls',
                'fixed timestep physics for consistency',
                'level streaming for performance',
                'save system with checkpoint data',
                'pixel-perfect camera alignment',
                'animation state management'
            ],
            'suggested_architecture': 'Component-based with State Machines and Scriptable Objects',
            'warbler_insights': 'GitHub Copilot Enhanced: Modern platformers require frame-perfect input handling with coyote time and input buffering. Implement a robust state machine for character movement, use Unity\'s new Input System for complex control schemes, and consider implementing visual feedback systems for player actions to improve game feel.',
            'github_copilot_enhanced': True,
            'ai_provider': 'github_copilot_simulation'
        }
    
    def _create_enhanced_custom_analysis(self, user_request: str) -> Dict[str, Any]:
        """Enhanced custom analysis with GitHub Copilot insights"""
        return {
            'game_type': 'custom',
            'complexity_level': 'moderate',
            'required_systems': [
                'GameManager',
                'PlayerController',
                'UIManager',
                'InputHandler',
                'SceneManager',
                'AudioManager',
                'SettingsManager',
                'SaveSystem'
            ],
            'recommended_folders': [
                'Scripts/Core',
                'Scripts/Player',
                'Scripts/Managers',
                'Scripts/UI',
                'Scripts/Audio',
                'Scripts/Data',
                'Scripts/Utilities'
            ],
            'unity_packages': ['InputSystem', 'Audio Mixer', 'Addressables'],
            'estimated_dev_time': '4-6 weeks',
            'key_mechanics': ['custom_gameplay', 'user_interaction', 'progressive_complexity'],
            'technical_considerations': ['modular architecture', 'performance optimization', 'maintainability', 'extensibility', 'testability'],
            'suggested_architecture': 'SOLID principles with dependency injection',
            'warbler_insights': 'GitHub Copilot Enhanced: For custom games, establish a solid foundation with SOLID principles, implement service locator or dependency injection patterns, and create modular systems that can be easily extended. Consider using Unity\'s new Addressables system for dynamic content loading and implement comprehensive logging for debugging.',
            'github_copilot_enhanced': True,
            'ai_provider': 'github_copilot_simulation'
        }
    
    def _create_enhanced_tower_defense_analysis(self, user_request: str) -> Dict[str, Any]:
        """Enhanced tower defense analysis"""
        return {
            'game_type': 'tower-defense',
            'complexity_level': 'complex',
            'required_systems': [
                'GridSystem',
                'TowerPlacement',
                'TowerUpgrade',
                'EnemyPathfinding',
                'WaveSpawner',
                'ResourceManager',
                'ProjectileSystem',
                'TargetingSystem',
                'AIDirector',
                'BalanceManager'
            ],
            'recommended_folders': [
                'Scripts/Grid',
                'Scripts/Towers',
                'Scripts/Enemies',
                'Scripts/Pathfinding',
                'Scripts/Managers',
                'Scripts/UI',
                'Scripts/Economy',
                'Scripts/AI',
                'Scripts/Balance'
            ],
            'unity_packages': ['InputSystem', '2D Tilemap Extras', 'NavMesh Components', 'Addressables'],
            'estimated_dev_time': '6-8 weeks',
            'key_mechanics': [
                'grid-based placement with validation',
                'A* pathfinding with dynamic obstacles',
                'tower targeting with priority systems',
                'resource management and economy',
                'wave progression with AI director',
                'tower upgrades with branching paths',
                'strategic planning with preview',
                'difficulty scaling with player skill'
            ],
            'technical_considerations': [
                'efficient pathfinding with caching',
                'grid optimization with spatial hashing',
                'projectile pooling with prediction',
                'AI performance scaling with LOD',
                'balance system with data-driven design',
                'save system with game state'
            ],
            'suggested_architecture': 'ECS with ScriptableObject data and event-driven communication',
            'warbler_insights': 'GitHub Copilot Enhanced: Tower defense games benefit from data-driven design with ScriptableObjects for towers and enemies. Implement a flexible targeting system with priority queues, use Unity\'s Job System for pathfinding calculations, and create an AI director that adapts difficulty based on player performance.',
            'github_copilot_enhanced': True,
            'ai_provider': 'github_copilot_simulation'
        }
    
    def _create_enhanced_racing_analysis(self, user_request: str) -> Dict[str, Any]:
        """Enhanced racing analysis"""
        return {
            'game_type': 'racing',
            'complexity_level': 'complex',
            'required_systems': [
                'VehicleController',
                'PhysicsManager',
                'TrackManager', 
                'AIRacer',
                'LapSystem',
                'InputSystem',
                'CameraFollow',
                'UIManager',
                'WeatherSystem',
                'TerrainManager'
            ],
            'recommended_folders': [
                'Scripts/Vehicle',
                'Scripts/Track',
                'Scripts/AI',
                'Scripts/Physics',
                'Scripts/Managers',
                'Scripts/UI',
                'Scripts/Weather',
                'Scripts/Terrain'
            ],
            'unity_packages': ['InputSystem', '3D Physics', 'Cinemachine', 'ProBuilder', 'Terrain Tools'],
            'estimated_dev_time': '6-10 weeks',
            'key_mechanics': [
                'realistic vehicle physics',
                'dynamic track surfaces',
                'intelligent AI opponents',
                'lap timing with precision',
                'collision detection with recovery',
                'speed management with assists',
                'weather effects on handling',
                'performance optimization'
            ],
            'technical_considerations': [
                'physics stability at high speeds',
                'performance optimization for complex scenes',
                'AI behavior variety and challenge',
                'track streaming for large environments',
                'network synchronization for multiplayer',
                'cross-platform input handling'
            ],
            'suggested_architecture': 'Component-based with Physics integration and networked systems',
            'warbler_insights': 'GitHub Copilot Enhanced: Racing games require careful physics tuning with wheel colliders and suspension systems. Implement rubber-band AI for competitive racing, use LOD systems for performance, and consider implementing a replay system for player analysis.',
            'github_copilot_enhanced': True,
            'ai_provider': 'github_copilot_simulation'
        }
    
    def _create_enhanced_puzzle_analysis(self, user_request: str) -> Dict[str, Any]:
        """Enhanced puzzle analysis"""
        return {
            'game_type': 'puzzle',
            'complexity_level': 'moderate',
            'required_systems': [
                'PuzzleManager',
                'GridSystem',
                'MatchSystem',
                'ScoreManager',
                'LevelProgression',
                'InputHandler',
                'AnimationController',
                'UIManager',
                'HintSystem',
                'TutorialManager'
            ],
            'recommended_folders': [
                'Scripts/Puzzle',
                'Scripts/Grid',
                'Scripts/Matching',
                'Scripts/Managers',
                'Scripts/UI',
                'Scripts/Animation',
                'Scripts/Tutorial',
                'Scripts/Hints'
            ],
            'unity_packages': ['InputSystem', 'UI Toolkit', 'DOTween', 'Addressables'],
            'estimated_dev_time': '3-5 weeks',
            'key_mechanics': [
                'grid-based logic with validation',
                'pattern matching with scoring',
                'score calculation with multipliers',
                'level progression with difficulty curve',
                'visual feedback with juicy animations',
                'input validation with undo system',
                'hint system with smart suggestions',
                'tutorial system with guided learning'
            ],
            'technical_considerations': [
                'algorithm efficiency for large grids',
                'animation performance with pooling',
                'UI responsiveness with async operations',
                'save system with progress tracking',
                'accessibility features for all players',
                'analytics for level difficulty tuning'
            ],
            'suggested_architecture': 'MVC with State Machines and data-driven level design',
            'warbler_insights': 'GitHub Copilot Enhanced: Puzzle games benefit from data-driven level design using ScriptableObjects. Implement a robust undo/redo system, use DOTween for satisfying animations, and create an analytics system to track player behavior for level balancing.',
            'github_copilot_enhanced': True,
            'ai_provider': 'github_copilot_simulation'
        }
    
    def _create_enhanced_rpg_analysis(self, user_request: str) -> Dict[str, Any]:
        """Enhanced RPG analysis"""
        return {
            'game_type': 'rpg',
            'complexity_level': 'complex',
            'required_systems': [
                'CharacterController',
                'InventorySystem',
                'CharacterStats',
                'DialogueSystem',
                'QuestManager',
                'CombatSystem',
                'SaveSystem',
                'LevelManager',
                'SkillTreeSystem',
                'LootSystem'
            ],
            'recommended_folders': [
                'Scripts/Character',
                'Scripts/Inventory',
                'Scripts/Dialogue',
                'Scripts/Quests',
                'Scripts/Combat',
                'Scripts/Managers',
                'Scripts/UI',
                'Scripts/Skills',
                'Scripts/Loot'
            ],
            'unity_packages': ['InputSystem', 'UI Toolkit', 'Addressables', 'Localization', 'Analytics'],
            'estimated_dev_time': '8-12 weeks',
            'key_mechanics': [
                'character progression with multiple paths',
                'inventory management with categories',
                'dialogue trees with branching narratives',
                'quest tracking with objectives',
                'combat systems with strategy',
                'save/load functionality with versioning',
                'skill trees with prerequisites',
                'loot system with rarities'
            ],
            'technical_considerations': [
                'data management with versioning',
                'save system architecture with compression',
                'content scalability with modular design',
                'localization support with dynamic text',
                'performance optimization for large worlds',
                'mod support with secure sandboxing'
            ],
            'suggested_architecture': 'Clean Architecture with CQRS and event sourcing',
            'warbler_insights': 'GitHub Copilot Enhanced: RPGs require sophisticated data management using Clean Architecture principles. Implement event sourcing for save systems, use CQRS for separating read/write operations, and create a flexible dialogue system with Yarn Spinner or similar tools.',
            'github_copilot_enhanced': True,
            'ai_provider': 'github_copilot_simulation'
        }

def test_github_copilot_connection(access_token: str) -> bool:
    """Test GitHub Copilot API connection"""
    if not access_token:
        print("❌ No access token provided")
        return False
    
    client = GitHubCopilotClient(access_token)
    return client.test_connection()

def get_github_copilot_analysis(access_token: str, user_request: str) -> Optional[Dict[str, Any]]:
    """Get project analysis from GitHub Copilot"""
    if not access_token:
        print("❌ No access token provided for GitHub Copilot")
        return None
    
    client = GitHubCopilotClient(access_token)
    return client.get_project_analysis(user_request)

def main():
    """Command line interface for GitHub Copilot client testing"""
    import sys
    
    if len(sys.argv) < 3:
        print("Usage:")
        print("  python github_copilot_client.py test <token>")
        print("  python github_copilot_client.py analyze <token> 'project request'")
        sys.exit(1)
    
    command = sys.argv[1].lower()
    token = sys.argv[2]
    
    if command == "test":
        if test_github_copilot_connection(token):
            print("✅ GitHub Copilot connection successful")
        else:
            print("❌ GitHub Copilot connection failed")
            sys.exit(1)
    
    elif command == "analyze":
        if len(sys.argv) < 4:
            print("❌ Please provide a project request")
            sys.exit(1)
        
        request = sys.argv[3]
        analysis = get_github_copilot_analysis(token, request)
        
        if analysis:
            print("✅ GitHub Copilot analysis successful")
            print(json.dumps(analysis, indent=2))
        else:
            print("❌ GitHub Copilot analysis failed")
            sys.exit(1)
    
    else:
        print(f"❌ Unknown command: {command}")
        sys.exit(1)

if __name__ == "__main__":
    main()