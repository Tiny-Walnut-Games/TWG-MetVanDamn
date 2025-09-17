#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Warbler Project Intelligence Bridge
Connects Unity Project Orchestrator with Gemma3 AI for intelligent project analysis
"""

import json
import os
import sys
import requests
import traceback
from typing import Optional, Dict, Any
from datetime import datetime
from pathlib import Path

# Import GitHub Copilot integration
try:
    from github_copilot_auth import get_valid_token as get_github_token, validate_token as validate_github_token
    from github_copilot_client import get_github_copilot_analysis, test_github_copilot_connection
    GITHUB_COPILOT_AVAILABLE = True
except ImportError as e:
    print(f"⚠️ GitHub Copilot integration not available: {e}")
    GITHUB_COPILOT_AVAILABLE = False
    
    # Provide fallback functions when GitHub Copilot is not available
    def get_github_token() -> Optional[str]:
        return None
    
    def validate_github_token(access_token: str) -> bool:
        return False
    
    def get_github_copilot_analysis(access_token: str, user_request: str) -> Optional[Dict[str, Any]]:
        return None
    
    def test_github_copilot_connection(access_token: str) -> bool:
        return False

# Ensure UTF-8 encoding for Windows console compatibility
if sys.stdout.encoding != 'utf-8':
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

def safe_print(message):
    """Print message with fallback for Unicode encoding issues"""
    try:
        print(message)
    except UnicodeEncodeError:
        # Remove emojis and non-ASCII characters if encoding fails
        safe_message = message.encode('ascii', 'ignore').decode('ascii')
        print(safe_message)

def log_error_to_file(error_message, exception=None):
    """Log errors to a persistent file that won't get cleared"""
    try:
        log_dir = Path(__file__).parent
        log_file = log_dir / "warbler_error_log.txt"
        
        with open(log_file, 'a', encoding='utf-8') as f:
            timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            f.write(f"\n[{timestamp}] WARBLER ERROR:\n")
            f.write(f"{error_message}\n")
            
            if exception:
                f.write(f"Exception Details:\n")
                f.write(f"{str(exception)}\n")
                f.write(f"Traceback:\n")
                f.write(traceback.format_exc())
            
            f.write("-" * 50 + "\n")
            
        # Also print to console
        safe_print(f"❌ ERROR LOGGED TO: {log_file}")
        safe_print(f"❌ {error_message}")
        
    except Exception as log_error:
        safe_print(f"❌ Failed to log error: {log_error}")
        safe_print(f"❌ Original error: {error_message}")
from datetime import datetime
from pathlib import Path

class WarblerProjectIntelligence:
    def __init__(self, gemma_endpoint="http://localhost:11434", prefer_github_copilot=True):
        self.gemma_endpoint = gemma_endpoint
        self.project_root = self._find_project_root()
        self.prefer_github_copilot = prefer_github_copilot and GITHUB_COPILOT_AVAILABLE
        self.github_token = None
        
        # Initialize GitHub Copilot if available and preferred
        if self.prefer_github_copilot:
            try:
                # Try to get existing valid token without triggering auth flow
                from github_copilot_auth import get_stored_token
                stored_token = get_stored_token()
                if stored_token and validate_github_token(stored_token):
                    self.github_token = stored_token
                    safe_print("✅ GitHub Copilot ready (existing token)")
                else:
                    safe_print("ℹ️ GitHub Copilot available but not authenticated")
            except Exception as e:
                safe_print(f"⚠️ GitHub Copilot initialization failed: {e}")
                self.prefer_github_copilot = False
        
    def _find_project_root(self):
        current_dir = Path(__file__).parent
        while current_dir.parent != current_dir:
            if (current_dir / "Assets").exists() and (current_dir / "TWG-TLDA.sln").exists():
                return current_dir
            current_dir = current_dir.parent
        return Path(".")
    
    def analyze_project_request(self, user_request):
        """
        Use multiple AI providers to intelligently analyze project setup requests
        Priority: GitHub Copilot → Ollama → Enhanced Fallback
        Returns detailed project plan with specific Unity recommendations
        """
        # Debug information for troubleshooting
        debug_info = f"""
DEBUG INFO:
- User Request: {user_request}
- Project Root: {self.project_root}
- Gemma Endpoint: {self.gemma_endpoint}
- GitHub Copilot Available: {GITHUB_COPILOT_AVAILABLE}
- GitHub Copilot Preferred: {self.prefer_github_copilot}
- GitHub Token Present: {bool(self.github_token)}
- Python Version: {sys.version}
- Working Directory: {os.getcwd()}
"""
        safe_print(debug_info)
        log_error_to_file(f"Starting analysis with debug info: {debug_info}")
        
        # Try AI providers in priority order
        providers_tried = []
        
        # 1. Try GitHub Copilot first (if available and preferred)
        if self.prefer_github_copilot and self.github_token:
            try:
                safe_print("[AI] Attempting GitHub Copilot analysis...")
                providers_tried.append("GitHub Copilot")
                
                analysis = get_github_copilot_analysis(self.github_token, user_request)
                if analysis:
                    safe_print("✅ GitHub Copilot analysis successful!")
                    enhanced = self._enhance_analysis(analysis, user_request)
                    enhanced['ai_provider_used'] = 'github_copilot'
                    enhanced['providers_tried'] = providers_tried
                    return enhanced
                else:
                    safe_print("⚠️ GitHub Copilot analysis failed, trying next provider...")
                    
            except Exception as e:
                log_error_to_file(f"GitHub Copilot analysis failed: {e}")
                safe_print(f"⚠️ GitHub Copilot error: {e}")
        
        # 2. Try Ollama (existing implementation)
        try:
            safe_print("[AI] Attempting Ollama analysis...")
            providers_tried.append("Ollama")
            
            analysis = self._get_ollama_analysis(user_request)
            if analysis:
                safe_print("✅ Ollama analysis successful!")
                enhanced = self._enhance_analysis(analysis, user_request)
                enhanced['ai_provider_used'] = 'ollama'
                enhanced['providers_tried'] = providers_tried
                return enhanced
            else:
                safe_print("⚠️ Ollama analysis failed, using enhanced fallback...")
                
        except Exception as e:
            log_error_to_file(f"Ollama analysis failed: {e}")
            safe_print(f"⚠️ Ollama error: {e}")
        
        # 3. Enhanced fallback analysis
        safe_print("[AI] Using enhanced fallback analysis...")
        providers_tried.append("Enhanced Fallback")
        analysis = self._fallback_analysis(user_request)
        enhanced = self._enhance_analysis(analysis, user_request)
        enhanced['ai_provider_used'] = 'enhanced_fallback'
        enhanced['providers_tried'] = providers_tried
        return enhanced
    
    def _get_ollama_analysis(self, user_request):
        """
        Original Ollama analysis implementation (extracted from analyze_project_request)
        """
        analysis_prompt = f"""
You are Warbler, an expert Unity game development AI. Analyze this project request and provide a detailed setup plan:

User Request: "{user_request}"

Please analyze and respond with JSON in this exact format:
{{
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
}}

Focus on practical Unity implementation details and modern game development patterns.
"""
        
        # Try Ollama native API first, then fallback to other endpoints
        endpoints_to_try = [
            f"{self.gemma_endpoint}/api/generate",  # Ollama native API
            f"{self.gemma_endpoint}/v1/chat/completions",  # OpenAI-compatible
        ]
        
        for endpoint in endpoints_to_try:
            try:
                safe_print(f"[AI] Testing endpoint: {endpoint}")
                
                # Test endpoint connectivity first
                try:
                    test_response = requests.get(endpoint.replace("/v1/chat/completions", "").replace("/api/chat", ""), 
                                               timeout=5)
                    safe_print(f"[AI] Endpoint test result: {test_response.status_code}")
                except Exception as test_error:
                    log_error_to_file(f"Endpoint test failed for {endpoint}: {test_error}")
                    continue
                
                safe_print(f"[AI] Making request to: {endpoint}")
                
                if "/api/generate" in endpoint:
                    # Ollama native format
                    response = requests.post(
                        endpoint,
                        json={
                            "model": "llama2",
                            "prompt": analysis_prompt,
                            "stream": False
                        },
                        timeout=30
                    )
                else:
                    # OpenAI-compatible format
                    response = requests.post(
                        endpoint,
                        json={
                            "model": "llama2",
                            "messages": [{"role": "user", "content": analysis_prompt}],
                            "stream": False
                        },
                        timeout=30
                    )
                
                if response.status_code == 200:
                    print(f"✅ Connected to AI at {endpoint}")
                    result = response.json()
                    
                    # Handle different response formats
                    message_content = None
                    if "/api/generate" in endpoint:
                        # Ollama native response format
                        message_content = result.get('response', '')
                    else:
                        # OpenAI-compatible response format
                        message_content = result.get('choices', [{}])[0].get('message', {}).get('content', '')
                    
                    if message_content:
                        try:
                            analysis = json.loads(message_content)
                            return analysis
                        except json.JSONDecodeError:
                            # If JSON parsing fails, try to extract JSON from response
                            import re
                            json_match = re.search(r'\{.*\}', message_content, re.DOTALL)
                            if json_match:
                                try:
                                    analysis = json.loads(json_match.group())
                                    return analysis
                                except json.JSONDecodeError:
                                    pass
                            
                            # Use fallback analysis if AI response can't be parsed
                            safe_print("⚠️ AI response couldn't be parsed as JSON, returning None for fallback")
                            return None
                    else:
                        safe_print("⚠️ No content in AI response")
                else:
                    safe_print(f"❌ AI endpoint returned {response.status_code}: {response.text[:200]}")
                    log_error_to_file(f"API Error {response.status_code}: {response.text}")
                    
            except requests.exceptions.Timeout:
                error_msg = f"Connection timeout to {endpoint} (30 seconds)"
                log_error_to_file(error_msg)
                continue
            except requests.exceptions.ConnectionError as e:
                error_msg = f"Connection failed to {endpoint} - {str(e)}"
                log_error_to_file(error_msg, e)
                continue
            except Exception as e:
                error_msg = f"Unexpected error with {endpoint}"
                log_error_to_file(error_msg, e)
                continue
        
        safe_print(f"[AI] No Ollama endpoints available")
        return None
    
    def authenticate_github_copilot(self):
        """Authenticate with GitHub Copilot interactively"""
        if not GITHUB_COPILOT_AVAILABLE:
            safe_print("❌ GitHub Copilot integration not available")
            return False
        
        try:
            self.github_token = get_github_token()
            if self.github_token:
                safe_print("✅ GitHub Copilot authentication successful!")
                return True
            else:
                safe_print("❌ GitHub Copilot authentication failed")
                return False
        except Exception as e:
            log_error_to_file(f"GitHub Copilot authentication error: {e}")
            safe_print(f"❌ Authentication error: {e}")
            return False
    
    def test_github_copilot_connection(self):
        """Test GitHub Copilot connection"""
        if not GITHUB_COPILOT_AVAILABLE:
            return False
        
        if not self.github_token:
            # Try to get existing token
            try:
                from github_copilot_auth import get_stored_token
                stored_token = get_stored_token()
                if stored_token and validate_github_token(stored_token):
                    self.github_token = stored_token
                else:
                    return False
            except Exception:
                return False
        
        try:
            return test_github_copilot_connection(self.github_token)
        except Exception as e:
            log_error_to_file(f"GitHub Copilot connection test failed: {e}")
            return False
    
    def test_ollama_connection(self):
        """Test Ollama connection"""
        try:
            import requests
            response = requests.get(self.gemma_endpoint, timeout=5)
            return response.status_code == 200
        except Exception:
            return False
    
    def get_provider_status(self):
        """Get status of all AI providers"""
        status = {
            'github_copilot': {
                'available': GITHUB_COPILOT_AVAILABLE,
                'authenticated': bool(self.github_token) if GITHUB_COPILOT_AVAILABLE else False,
                'connected': False
            },
            'ollama': {
                'available': True,
                'endpoint': self.gemma_endpoint,
                'connected': False
            }
        }
        
        # Test connections
        if GITHUB_COPILOT_AVAILABLE and self.github_token:
            status['github_copilot']['connected'] = self.test_github_copilot_connection()
        
        status['ollama']['connected'] = self.test_ollama_connection()
        
        return status
    
    def _enhance_analysis(self, analysis_result, user_request):
        """Enhance AI analysis with Warbler-specific insights"""
        
        # Add Warbler's expertise
        enhanced = analysis_result.copy()
        enhanced['warbler_enhancement'] = {
            'tlda_integration': True,
            'auto_documentation': True,
            'performance_monitoring': True,
            'suggested_tldl_tags': self._suggest_tldl_tags(analysis_result.get('game_type', 'custom')),
            'development_milestones': self._create_milestones(analysis_result),
            'testing_strategy': self._suggest_testing_approach(analysis_result)
        }
        
        return enhanced
    
    def _suggest_tldl_tags(self, game_type):
        """Suggest TLDL tags for project tracking"""
        base_tags = ['project-setup', 'warbler-generated', game_type]
        
        type_specific = {
            'survivor.io': ['wave-defense', 'upgrades', 'survival'],
            'platformer': ['physics', 'level-design', 'collectibles'],
            'tower-defense': ['strategy', 'grid-system', 'pathfinding'],
            'racing': ['physics', 'ai-opponents', 'track-design'],
            'puzzle': ['game-logic', 'ui-heavy', 'progression'],
            'rpg': ['character-system', 'inventory', 'dialogue']
        }
        
        return base_tags + type_specific.get(game_type, ['custom-gameplay'])
    
    def _create_milestones(self, analysis):
        """Create development milestones based on project complexity"""
        complexity = analysis.get('complexity_level', 'moderate')
        
        if complexity == 'simple':
            return [
                "Core mechanics prototype (Week 1)",
                "Basic UI implementation (Week 2)",
                "Polish and testing (Week 3)"
            ]
        elif complexity == 'moderate':
            return [
                "Foundation systems (Week 1-2)",
                "Core gameplay loop (Week 3-4)",
                "Content creation (Week 5-6)",
                "Polish and optimization (Week 7-8)"
            ]
        else:  # complex
            return [
                "Architecture setup (Week 1-2)",
                "Core systems development (Week 3-6)",
                "Feature implementation (Week 7-10)",
                "Integration and testing (Week 11-12)",
                "Polish and optimization (Week 13-16)"
            ]
    
    def _suggest_testing_approach(self, analysis):
        """Suggest testing strategy based on project type"""
        game_type = analysis.get('game_type', 'custom')
        
        strategies = {
            'survivor.io': ['Performance testing with 100+ enemies', 'Upgrade balance testing', 'Wave difficulty progression'],
            'platformer': ['Physics consistency testing', 'Level completion validation', 'Control responsiveness'],
            'tower-defense': ['Pathfinding stress testing', 'Tower placement validation', 'Economic balance testing'],
            'racing': ['Physics stability testing', 'AI behavior validation', 'Track collision testing'],
            'puzzle': ['Logic validation testing', 'UI interaction testing', 'Progression flow testing'],
            'rpg': ['Save system testing', 'Character progression validation', 'Dialogue system testing']
        }
        
        return strategies.get(game_type, ['General gameplay testing', 'Performance validation', 'User experience testing'])
    
    def _fallback_analysis(self, user_request):
        """Enhanced fallback analysis when Gemma3 is unavailable"""
        # Intelligent keyword-based analysis with game-specific templates
        request_lower = user_request.lower()
        
        if any(word in request_lower for word in ['survivor', 'survival', 'wave', 'horde']):
            return self._create_survivor_io_analysis(user_request)
        elif any(word in request_lower for word in ['platform', 'jump', 'side-scroll']):
            return self._create_platformer_analysis(user_request)
        elif any(word in request_lower for word in ['tower', 'defense', 'strategy']):
            return self._create_tower_defense_analysis(user_request)
        elif any(word in request_lower for word in ['race', 'racing', 'car', 'speed']):
            return self._create_racing_analysis(user_request)
        elif any(word in request_lower for word in ['puzzle', 'match', 'logic']):
            return self._create_puzzle_analysis(user_request)
        elif any(word in request_lower for word in ['rpg', 'character', 'level', 'inventory']):
            return self._create_rpg_analysis(user_request)
        else:
            return self._create_custom_analysis(user_request)
    
    def _create_survivor_io_analysis(self, user_request):
        """Create detailed analysis for survivor.io style games"""
        game_type = 'survivor.io'
        return {
            'game_type': game_type,
            'complexity_level': 'moderate',
            'required_systems': [
                'PlayerController', 
                'EnemySpawner', 
                'WeaponSystem', 
                'UpgradeManager', 
                'WaveManager',
                'ExperienceManager',
                'HealthSystem',
                'DamageSystem'
            ],
            'recommended_folders': [
                'Scripts/Player', 
                'Scripts/Enemies', 
                'Scripts/Weapons', 
                'Scripts/Upgrades',
                'Scripts/Managers',
                'Scripts/UI',
                'Scripts/Projectiles'
            ],
            'unity_packages': ['InputSystem', '2D Sprite', 'Cinemachine'],
            'estimated_dev_time': '3-5 weeks',
            'key_mechanics': [
                'top-down movement',
                'automatic shooting',
                'enemy waves',
                'experience collection',
                'upgrade selection',
                'damage numbers',
                'procedural spawning'
            ],
            'technical_considerations': [
                'object pooling for projectiles',
                'efficient enemy spawning',
                'performance with 100+ entities',
                'mobile-friendly controls'
            ],
            'suggested_architecture': 'Component-based with ScriptableObject configs',
            'warbler_insights': 'Survivor.io games require efficient spawning, object pooling, and scalable upgrade systems. Focus on performance optimization and clear visual feedback.',
            'warbler_enhancement': {
                'tlda_integration': True,
                'auto_documentation': True,
                'performance_monitoring': True,
                'suggested_tldl_tags': self._suggest_tldl_tags(game_type),
                'development_milestones': [
                    'Player movement and basic shooting (Week 1)',
                    'Enemy spawning and collision (Week 2)', 
                    'Upgrade system and UI (Week 3)',
                    'Wave progression and balance (Week 4)',
                    'Polish and optimization (Week 5)'
                ],
                'testing_strategy': [
                    'Performance testing with 100+ enemies',
                    'Upgrade balance testing',
                    'Wave difficulty progression',
                    'Mobile input responsiveness'
                ]
            }
        }
    
    def _create_platformer_analysis(self, user_request):
        """Create detailed analysis for platformer games"""
        game_type = 'platformer'
        return {
            'game_type': game_type,
            'complexity_level': 'moderate',
            'required_systems': [
                'PlayerMovement',
                'JumpController', 
                'PlatformController',
                'EnemyAI',
                'CollectibleSystem',
                'LevelManager',
                'CheckpointSystem',
                'CameraController'
            ],
            'recommended_folders': [
                'Scripts/Player',
                'Scripts/Enemies', 
                'Scripts/Platforms',
                'Scripts/Collectibles',
                'Scripts/Managers',
                'Scripts/UI',
                'Scripts/Environment'
            ],
            'unity_packages': ['InputSystem', '2D Tilemap Extras', 'Cinemachine', '2D Physics'],
            'estimated_dev_time': '4-6 weeks',
            'key_mechanics': [
                'precise jumping',
                'physics-based movement',
                'enemy collisions',
                'collectible gathering',
                'level progression',
                'checkpoint system'
            ],
            'technical_considerations': [
                'tight control responsiveness',
                'physics consistency',
                'level streaming',
                'save system integration'
            ],
            'suggested_architecture': 'Component-based with State Machines',
            'warbler_insights': 'Platformers require precise controls, consistent physics, and well-designed level progression. Focus on player feel and responsive input.',
            'warbler_enhancement': {
                'tlda_integration': True,
                'auto_documentation': True,
                'performance_monitoring': True,
                'suggested_tldl_tags': self._suggest_tldl_tags(game_type),
                'development_milestones': [
                    'Player movement and jumping (Week 1-2)',
                    'Basic level design and platforms (Week 3)',
                    'Enemies and collision systems (Week 4)',
                    'Collectibles and progression (Week 5)',
                    'Polish and level creation (Week 6)'
                ],
                'testing_strategy': [
                    'Physics consistency testing',
                    'Control responsiveness validation',
                    'Level completion playtesting',
                    'Edge case collision testing'
                ]
            }
        }
    
    def _create_tower_defense_analysis(self, user_request):
        """Create detailed analysis for tower defense games"""
        game_type = 'tower-defense'
        return {
            'game_type': game_type,
            'complexity_level': 'complex',
            'required_systems': [
                'GridSystem',
                'TowerPlacement',
                'TowerUpgrade',
                'EnemyPathfinding',
                'WaveSpawner',
                'ResourceManager',
                'ProjectileSystem',
                'TargetingSystem'
            ],
            'recommended_folders': [
                'Scripts/Grid',
                'Scripts/Towers',
                'Scripts/Enemies',
                'Scripts/Pathfinding',
                'Scripts/Managers',
                'Scripts/UI',
                'Scripts/Economy'
            ],
            'unity_packages': ['InputSystem', '2D Tilemap Extras', 'NavMesh Components'],
            'estimated_dev_time': '6-8 weeks',
            'key_mechanics': [
                'grid-based placement',
                'pathfinding AI',
                'tower targeting',
                'resource management',
                'wave progression',
                'tower upgrades',
                'strategic planning'
            ],
            'technical_considerations': [
                'efficient pathfinding algorithms',
                'grid optimization',
                'projectile pooling',
                'AI performance scaling'
            ],
            'suggested_architecture': 'ECS with ScriptableObject data',
            'warbler_insights': 'Tower Defense requires robust pathfinding, efficient grid systems, and balanced economic progression. Focus on modular tower design.',
            'warbler_enhancement': {
                'tlda_integration': True,
                'auto_documentation': True,
                'performance_monitoring': True,
                'suggested_tldl_tags': self._suggest_tldl_tags(game_type),
                'development_milestones': [
                    'Grid system and basic placement (Week 1-2)',
                    'Pathfinding and enemy movement (Week 3-4)',
                    'Tower systems and targeting (Week 5-6)',
                    'Economy and progression (Week 7)',
                    'Balance and polish (Week 8)'
                ],
                'testing_strategy': [
                    'Pathfinding stress testing',
                    'Tower placement validation',
                    'Economic balance testing',
                    'Performance with multiple units'
                ]
            }
        }
    
    def _create_racing_analysis(self, user_request):
        """Create detailed analysis for racing games"""
        game_type = 'racing'
        return {
            'game_type': game_type,
            'complexity_level': 'complex',
            'required_systems': [
                'VehicleController',
                'PhysicsManager',
                'TrackManager', 
                'AIRacer',
                'LapSystem',
                'InputSystem',
                'CameraFollow',
                'UIManager'
            ],
            'recommended_folders': [
                'Scripts/Vehicle',
                'Scripts/Track',
                'Scripts/AI',
                'Scripts/Physics',
                'Scripts/Managers',
                'Scripts/UI'
            ],
            'unity_packages': ['InputSystem', '3D Physics', 'Cinemachine', 'ProBuilder'],
            'estimated_dev_time': '6-10 weeks',
            'key_mechanics': [
                'vehicle physics',
                'track navigation',
                'AI opponents',
                'lap timing',
                'collision detection',
                'speed management'
            ],
            'technical_considerations': [
                'physics stability',
                'performance optimization',
                'AI behavior variety',
                'track streaming'
            ],
            'suggested_architecture': 'Component-based with Physics integration',
            'warbler_insights': 'Racing games require stable physics, responsive controls, and engaging AI opponents. Focus on feel and performance.',
            'warbler_enhancement': {
                'tlda_integration': True,
                'auto_documentation': True,
                'performance_monitoring': True,
                'suggested_tldl_tags': self._suggest_tldl_tags(game_type),
                'development_milestones': [
                    'Basic vehicle physics (Week 1-2)',
                    'Track design and navigation (Week 3-4)',
                    'AI opponents and racing logic (Week 5-6)',
                    'UI and timing systems (Week 7-8)',
                    'Polish and optimization (Week 9-10)'
                ],
                'testing_strategy': [
                    'Physics stability testing',
                    'AI behavior validation',
                    'Track collision testing',
                    'Performance optimization'
                ]
            }
        }
    
    def _create_puzzle_analysis(self, user_request):
        """Create detailed analysis for puzzle games"""
        game_type = 'puzzle'
        return {
            'game_type': game_type,
            'complexity_level': 'moderate',
            'required_systems': [
                'PuzzleManager',
                'GridSystem',
                'MatchSystem',
                'ScoreManager',
                'LevelProgression',
                'InputHandler',
                'AnimationController',
                'UIManager'
            ],
            'recommended_folders': [
                'Scripts/Puzzle',
                'Scripts/Grid',
                'Scripts/Matching',
                'Scripts/Managers',
                'Scripts/UI',
                'Scripts/Animation'
            ],
            'unity_packages': ['InputSystem', 'UI Toolkit', 'DOTween'],
            'estimated_dev_time': '3-5 weeks',
            'key_mechanics': [
                'grid-based logic',
                'pattern matching',
                'score calculation',
                'level progression',
                'visual feedback',
                'input validation'
            ],
            'technical_considerations': [
                'algorithm efficiency',
                'animation performance',
                'UI responsiveness',
                'save system'
            ],
            'suggested_architecture': 'MVC with State Machines',
            'warbler_insights': 'Puzzle games require clear logic systems, satisfying feedback, and progressive difficulty. Focus on player understanding.',
            'warbler_enhancement': {
                'tlda_integration': True,
                'auto_documentation': True,
                'performance_monitoring': True,
                'suggested_tldl_tags': self._suggest_tldl_tags(game_type),
                'development_milestones': [
                    'Core puzzle mechanics (Week 1-2)',
                    'Grid system and matching (Week 3)',
                    'UI and feedback systems (Week 4)',
                    'Level progression and polish (Week 5)'
                ],
                'testing_strategy': [
                    'Logic validation testing',
                    'UI interaction testing',
                    'Progression flow testing',
                    'Performance validation'
                ]
            }
        }
    
    def _create_rpg_analysis(self, user_request):
        """Create detailed analysis for RPG games"""
        game_type = 'rpg'
        return {
            'game_type': game_type,
            'complexity_level': 'complex',
            'required_systems': [
                'CharacterController',
                'InventorySystem',
                'CharacterStats',
                'DialogueSystem',
                'QuestManager',
                'CombatSystem',
                'SaveSystem',
                'LevelManager'
            ],
            'recommended_folders': [
                'Scripts/Character',
                'Scripts/Inventory',
                'Scripts/Dialogue',
                'Scripts/Quests',
                'Scripts/Combat',
                'Scripts/Managers',
                'Scripts/UI'
            ],
            'unity_packages': ['InputSystem', 'UI Toolkit', 'Addressables', 'Localization'],
            'estimated_dev_time': '8-12 weeks',
            'key_mechanics': [
                'character progression',
                'inventory management',
                'dialogue trees',
                'quest tracking',
                'combat systems',
                'save/load functionality'
            ],
            'technical_considerations': [
                'data management',
                'save system architecture',
                'content scalability',
                'localization support'
            ],
            'suggested_architecture': 'MVC with ScriptableObject data',
            'warbler_insights': 'RPGs require robust data management, flexible content systems, and deep player progression. Focus on modular design.',
            'warbler_enhancement': {
                'tlda_integration': True,
                'auto_documentation': True,
                'performance_monitoring': True,
                'suggested_tldl_tags': self._suggest_tldl_tags(game_type),
                'development_milestones': [
                    'Character system and movement (Week 1-2)',
                    'Inventory and stats systems (Week 3-4)',
                    'Dialogue and quest systems (Week 5-6)',
                    'Combat mechanics (Week 7-8)',
                    'Save system and content (Week 9-10)',
                    'Polish and balancing (Week 11-12)'
                ],
                'testing_strategy': [
                    'Save system testing',
                    'Character progression validation',
                    'Dialogue system testing',
                    'Performance with large datasets'
                ]
            }
        }
    
    def _create_custom_analysis(self, user_request):
        """Create analysis for custom/unknown game types"""
        game_type = 'custom'
        return {
            'game_type': game_type,
            'complexity_level': 'moderate',
            'required_systems': [
                'PlayerController',
                'GameManager',
                'UIManager',
                'InputHandler',
                'SceneManager',
                'AudioManager'
            ],
            'recommended_folders': [
                'Scripts/Player',
                'Scripts/Managers',
                'Scripts/UI',
                'Scripts/Audio',
                'Scripts/Core'
            ],
            'unity_packages': ['InputSystem', 'Audio Mixer'],
            'estimated_dev_time': '4-6 weeks',
            'key_mechanics': ['custom_gameplay', 'user_interaction'],
            'technical_considerations': ['performance', 'maintainability', 'extensibility'],
            'suggested_architecture': 'Component-based',
            'warbler_insights': 'Custom game detected - analysis based on common game development patterns. Consider refining the request for more specific recommendations.',
            'warbler_enhancement': {
                'tlda_integration': True,
                'auto_documentation': True,
                'performance_monitoring': True,
                'suggested_tldl_tags': self._suggest_tldl_tags(game_type),
                'development_milestones': [
                    'Core system setup (Week 1-2)',
                    'Basic gameplay implementation (Week 3-4)',
                    'UI and polish (Week 5-6)'
                ],
                'testing_strategy': [
                    'Basic functionality testing',
                    'Performance validation',
                    'User experience testing'
                ]
            }
        }
    
    def create_project_blueprint(self, analysis, user_request):
        """Create a comprehensive project blueprint file"""
        blueprint = {
            'project_info': {
                'name': f"Warbler-Generated-{analysis['game_type'].title()}",
                'created_date': datetime.now().isoformat(),
                'user_request': user_request,
                'warbler_analysis': analysis
            },
            'development_plan': {
                'milestones': analysis['warbler_enhancement']['development_milestones'],
                'testing_strategy': analysis['warbler_enhancement']['testing_strategy'],
                'estimated_timeline': analysis.get('estimated_dev_time', '4-6 weeks')
            },
            'technical_specs': {
                'unity_version': '2022.3 LTS or later',
                'target_platforms': ['PC', 'Mobile'],
                'required_packages': analysis.get('unity_packages', []),
                'architecture': analysis.get('suggested_architecture', 'Component-based')
            },
            'tlda_integration': {
                'auto_documentation': True,
                'performance_monitoring': True,
                'suggested_tags': analysis['warbler_enhancement']['suggested_tldl_tags'],
                'warbler_insights': analysis.get('warbler_insights', 'AI-powered development assistance')
            }
        }
        
        blueprint_path = self.project_root / "blueprints" / f"warbler-project-{datetime.now().strftime('%Y%m%d-%H%M%S')}.json"
        blueprint_path.parent.mkdir(exist_ok=True)
        
        with open(blueprint_path, 'w') as f:
            json.dump(blueprint, f, indent=2)
        
        print(f"🧙‍♂️ Warbler project blueprint created: {blueprint_path}")
        return blueprint_path
    
    def generate_unity_scripts(self, analysis, output_dir=None):
        """Generate actual Unity C# scripts based on analysis"""
        if output_dir is None:
            output_dir = self.project_root / "Assets" / "Scripts"
        else:
            output_dir = Path(output_dir)
        
        scripts_created = []
        
        for system in analysis.get('required_systems', []):
            script_content = self._generate_script_content(system, analysis)
            folder = self._determine_script_folder(system, analysis)
            
            script_dir = output_dir / folder
            script_dir.mkdir(parents=True, exist_ok=True)
            
            script_path = script_dir / f"{system}.cs"
            
            with open(script_path, 'w', encoding='utf-8') as f:
                f.write(script_content)
            
            scripts_created.append(str(script_path))
            print(f"✅ Created script: {script_path}")
        
        return scripts_created
    
    def _determine_script_folder(self, system_name, analysis):
        """Determine the best folder for a script based on AI analysis"""
        recommended_folders = analysis.get('recommended_folders', [])
        
        # Map system names to folders
        folder_mapping = {
            'player': 'Player',
            'enemy': 'Enemies', 
            'weapon': 'Weapons',
            'upgrade': 'Upgrades',
            'manager': 'Managers',
            'ui': 'UI',
            'camera': 'Managers'
        }
        
        system_lower = system_name.lower()
        for key, folder in folder_mapping.items():
            if key in system_lower:
                return folder
        
        return 'Managers'  # Default
    
    def _generate_script_content(self, system_name, analysis):
        """Generate actual Unity C# script content"""
        game_type = analysis.get('game_type', 'custom')
        architecture = analysis.get('suggested_architecture', 'Component-based')
        
        # Generate different scripts based on system type
        if 'player' in system_name.lower():
            return self._generate_player_script(system_name, game_type, analysis)
        elif 'enemy' in system_name.lower():
            return self._generate_enemy_script(system_name, game_type, analysis)
        elif 'weapon' in system_name.lower():
            return self._generate_weapon_script(system_name, game_type, analysis)
        elif 'upgrade' in system_name.lower():
            return self._generate_upgrade_script(system_name, game_type, analysis)
        elif 'wave' in system_name.lower():
            return self._generate_wave_script(system_name, game_type, analysis)
        else:
            return self._generate_generic_script(system_name, game_type, analysis)
    
    def _generate_player_script(self, system_name, game_type, analysis):
        """Generate player controller script"""
        return f'''using UnityEngine;

namespace TWG.TLDA.Generated
{{
    /// <summary>
    /// {system_name} - AI-Generated by Warbler for {game_type}
    /// Generated on {datetime.now().strftime("%Y-%m-%d %H:%M:%S")}
    /// 
    /// AI Analysis Results:
    /// - Game Type: {game_type}
    /// - Architecture: {analysis.get('suggested_architecture', 'Component-based')}
    /// - Key Mechanics: {', '.join(analysis.get('key_mechanics', []))}
    /// </summary>
    public class {system_name} : MonoBehaviour
    {{
        [Header("AI-Optimized Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private Rigidbody2D rb;
        
        [Header("Health System")]
        [SerializeField] private int maxHealth = 100;
        private int currentHealth;
        
        [Header("Input")]
        private Vector2 moveInput;
        
        void Start()
        {{
            InitializePlayer();
        }}
        
        void Update()
        {{
            HandleInput();
        }}
        
        void FixedUpdate()
        {{
            HandleMovement();
        }}
        
        void InitializePlayer()
        {{
            currentHealth = maxHealth;
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            
            Debug.Log("🤖 AI-Generated {system_name} initialized!");
        }}
        
        void HandleInput()
        {{
            moveInput.x = Input.GetAxis("Horizontal");
            moveInput.y = Input.GetAxis("Vertical");
        }}
        
        void HandleMovement()
        {{
            // AI-recommended smooth movement for {game_type}
            Vector2 targetVelocity = moveInput.normalized * moveSpeed;
            rb.velocity = Vector2.Lerp(rb.velocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        }}
        
        public void TakeDamage(int damage)
        {{
            currentHealth -= damage;
            Debug.Log($"Player took {{damage}} damage. Health: {{currentHealth}}/{{maxHealth}}");
            
            if (currentHealth <= 0)
            {{
                Die();
            }}
        }}
        
        void Die()
        {{
            Debug.Log("💀 Player died! Implement game over logic here.");
            // TODO: Trigger game over sequence
        }}
        
        // AI-suggested getter for other systems
        public Vector2 GetPosition() => transform.position;
        public bool IsAlive() => currentHealth > 0;
    }}
}}'''
    
    def _generate_enemy_script(self, system_name, game_type, analysis):
        """Generate enemy spawner script"""
        return f'''using UnityEngine;
using System.Collections;

namespace TWG.TLDA.Generated
{{
    /// <summary>
    /// {system_name} - AI-Generated by Warbler for {game_type}
    /// Generated on {datetime.now().strftime("%Y-%m-%d %H:%M:%S")}
    /// </summary>
    public class {system_name} : MonoBehaviour
    {{
        [Header("AI-Optimized Spawning")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private float spawnRate = 2f;
        [SerializeField] private float spawnDistance = 15f;
        [SerializeField] private int maxEnemies = 50;
        
        [Header("Performance Optimization")]
        [SerializeField] private Transform enemyParent;
        
        private Transform player;
        private int currentEnemyCount = 0;
        
        void Start()
        {{
            FindPlayer();
            SetupEnemyParent();
            StartCoroutine(SpawnEnemies());
        }}
        
        void FindPlayer()
        {{
            var playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {{
                player = playerController.transform;
            }}
            else
            {{
                Debug.LogWarning("⚠️ Player not found! Create a PlayerController script.");
            }}
        }}
        
        void SetupEnemyParent()
        {{
            if (enemyParent == null)
            {{
                GameObject parentObj = new GameObject("Enemies");
                enemyParent = parentObj.transform;
            }}
        }}
        
        IEnumerator SpawnEnemies()
        {{
            while (true)
            {{
                yield return new WaitForSeconds(spawnRate);
                
                if (currentEnemyCount < maxEnemies && player != null)
                {{
                    SpawnEnemy();
                }}
            }}
        }}
        
        void SpawnEnemy()
        {{
            if (enemyPrefab == null) return;
            
            Vector2 spawnPos = GetRandomSpawnPosition();
            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity, enemyParent);
            
            currentEnemyCount++;
            
            // AI-recommended: Subscribe to enemy death to track count
            var enemyComponent = enemy.GetComponent<EnemyController>();
            if (enemyComponent != null)
            {{
                enemyComponent.OnDeath += () => currentEnemyCount--;
            }}
        }}
        
        Vector2 GetRandomSpawnPosition()
        {{
            // AI-optimized spawning outside camera view
            Vector2 playerPos = player.position;
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            
            Vector2 spawnPos = playerPos + new Vector2(
                Mathf.Cos(angle) * spawnDistance,
                Mathf.Sin(angle) * spawnDistance
            );
            
            return spawnPos;
        }}
        
        // AI-suggested public methods for wave management
        public void SetSpawnRate(float newRate) => spawnRate = newRate;
        public void SetMaxEnemies(int newMax) => maxEnemies = newMax;
        public int GetCurrentEnemyCount() => currentEnemyCount;
    }}
}}'''
    
    def _generate_weapon_script(self, system_name, game_type, analysis):
        """Generate weapon system script"""
        return f'''using UnityEngine;

namespace TWG.TLDA.Generated
{{
    /// <summary>
    /// {system_name} - AI-Generated by Warbler for {game_type}
    /// Generated on {datetime.now().strftime("%Y-%m-%d %H:%M:%S")}
    /// </summary>
    public class {system_name} : MonoBehaviour
    {{
        [Header("AI-Optimized Weapon Config")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireRate = 2f;
        [SerializeField] private float projectileSpeed = 10f;
        [SerializeField] private float range = 20f;
        
        [Header("Auto-Targeting")]
        [SerializeField] private LayerMask enemyLayer = 1 << 8; // Enemies layer
        
        private float nextFireTime;
        private Transform nearestEnemy;
        
        void Update()
        {{
            FindNearestEnemy();
            TryShoot();
        }}
        
        void FindNearestEnemy()
        {{
            Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, range, enemyLayer);
            
            if (enemies.Length == 0)
            {{
                nearestEnemy = null;
                return;
            }}
            
            float nearestDistance = float.MaxValue;
            Transform nearest = null;
            
            foreach (var enemy in enemies)
            {{
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                if (distance < nearestDistance)
                {{
                    nearestDistance = distance;
                    nearest = enemy.transform;
                }}
            }}
            
            nearestEnemy = nearest;
        }}
        
        void TryShoot()
        {{
            if (nearestEnemy == null || Time.time < nextFireTime) return;
            
            nextFireTime = Time.time + 1f / fireRate;
            Shoot();
        }}
        
        void Shoot()
        {{
            if (projectilePrefab == null || firePoint == null || nearestEnemy == null) return;
            
            Vector2 direction = (nearestEnemy.position - firePoint.position).normalized;
            
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            
            // Rotate projectile to face direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            projectile.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            
            // Apply velocity
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {{
                rb.velocity = direction * projectileSpeed;
            }}
            
            Debug.Log($"🔫 Weapon fired at {{nearestEnemy.name}}");
        }}
        
        // AI-suggested upgrade methods
        public void UpgradeFireRate(float multiplier) => fireRate *= multiplier;
        public void UpgradeRange(float multiplier) => range *= multiplier;
        public void UpgradeProjectileSpeed(float multiplier) => projectileSpeed *= multiplier;
        
        void OnDrawGizmosSelected()
        {{
            // Visual debugging for range
            Gizmos.color = Color.red;
            Gizmos.DrawWireCircle(transform.position, range);
        }}
    }}
}}'''
    
    def _generate_upgrade_script(self, system_name, game_type, analysis):
        """Generate upgrade manager script"""
        return f'''using UnityEngine;
using System.Collections.Generic;

namespace TWG.TLDA.Generated
{{
    /// <summary>
    /// {system_name} - AI-Generated by Warbler for {game_type}
    /// Generated on {datetime.now().strftime("%Y-%m-%d %H:%M:%S")}
    /// </summary>
    public class {system_name} : MonoBehaviour
    {{
        [System.Serializable]
        public class Upgrade
        {{
            public string name;
            public string description;
            public UpgradeType type;
            public float value;
            public Sprite icon;
        }}
        
        public enum UpgradeType
        {{
            MovementSpeed,
            FireRate,
            Damage,
            Health,
            Range,
            ProjectileSpeed
        }}
        
        [Header("AI-Generated Upgrade System")]
        [SerializeField] private List<Upgrade> availableUpgrades;
        [SerializeField] private GameObject upgradeUIPanel;
        
        void Start()
        {{
            SetupDefaultUpgrades();
        }}
        
        void SetupDefaultUpgrades()
        {{
            if (availableUpgrades == null)
                availableUpgrades = new List<Upgrade>();
            
            // AI-recommended upgrades for {game_type}
            if (availableUpgrades.Count == 0)
            {{
                availableUpgrades.AddRange(new Upgrade[]
                {{
                    new Upgrade {{ name = "Speed Boost", description = "Increase movement speed by 20%", type = UpgradeType.MovementSpeed, value = 1.2f }},
                    new Upgrade {{ name = "Rapid Fire", description = "Increase fire rate by 50%", type = UpgradeType.FireRate, value = 1.5f }},
                    new Upgrade {{ name = "Extra Health", description = "Increase max health by 25", type = UpgradeType.Health, value = 25f }},
                    new Upgrade {{ name = "Long Range", description = "Increase weapon range by 30%", type = UpgradeType.Range, value = 1.3f }},
                    new Upgrade {{ name = "Power Shot", description = "Increase projectile speed by 40%", type = UpgradeType.ProjectileSpeed, value = 1.4f }}
                }});
            }}
        }}
        
        public void ShowUpgradeOptions()
        {{
            var randomUpgrades = GetRandomUpgrades(3);
            
            Debug.Log("🎯 Upgrade options available:");
            foreach (var upgrade in randomUpgrades)
            {{
                Debug.Log($"   • {{upgrade.name}}: {{upgrade.description}}");
            }}
            
            // TODO: Show upgrade UI
            if (upgradeUIPanel != null)
            {{
                upgradeUIPanel.SetActive(true);
            }}
        }}
        
        public void ApplyUpgrade(Upgrade upgrade)
        {{
            switch (upgrade.type)
            {{
                case UpgradeType.MovementSpeed:
                    ApplyMovementSpeedUpgrade(upgrade.value);
                    break;
                case UpgradeType.FireRate:
                    ApplyFireRateUpgrade(upgrade.value);
                    break;
                case UpgradeType.Health:
                    ApplyHealthUpgrade(upgrade.value);
                    break;
                case UpgradeType.Range:
                    ApplyRangeUpgrade(upgrade.value);
                    break;
                case UpgradeType.ProjectileSpeed:
                    ApplyProjectileSpeedUpgrade(upgrade.value);
                    break;
            }}
            
            Debug.Log($"✅ Applied upgrade: {{upgrade.name}}");
        }}
        
        void ApplyMovementSpeedUpgrade(float multiplier)
        {{
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
            {{
                // Reflection to modify private fields - AI-recommended approach
                var field = player.GetType().GetField("moveSpeed", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {{
                    float currentSpeed = (float)field.GetValue(player);
                    field.SetValue(player, currentSpeed * multiplier);
                }}
            }}
        }}
        
        void ApplyFireRateUpgrade(float multiplier)
        {{
            var weapon = FindObjectOfType<WeaponSystem>();
            if (weapon != null)
            {{
                weapon.UpgradeFireRate(multiplier);
            }}
        }}
        
        void ApplyHealthUpgrade(float amount)
        {{
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
            {{
                // Player would need public method to add max health
                Debug.Log($"Would add {{amount}} max health to player");
            }}
        }}
        
        void ApplyRangeUpgrade(float multiplier)
        {{
            var weapon = FindObjectOfType<WeaponSystem>();
            if (weapon != null)
            {{
                weapon.UpgradeRange(multiplier);
            }}
        }}
        
        void ApplyProjectileSpeedUpgrade(float multiplier)
        {{
            var weapon = FindObjectOfType<WeaponSystem>();
            if (weapon != null)
            {{
                weapon.UpgradeProjectileSpeed(multiplier);
            }}
        }}
        
        public List<Upgrade> GetRandomUpgrades(int count = 3)
        {{
            List<Upgrade> randomUpgrades = new List<Upgrade>();
            List<Upgrade> available = new List<Upgrade>(availableUpgrades);
            
            for (int i = 0; i < count && available.Count > 0; i++)
            {{
                int randomIndex = Random.Range(0, available.Count);
                randomUpgrades.Add(available[randomIndex]);
                available.RemoveAt(randomIndex);
            }}
            
            return randomUpgrades;
        }}
    }}
}}'''
    
    def _generate_wave_script(self, system_name, game_type, analysis):
        """Generate wave manager script"""
        return f'''using UnityEngine;
using System.Collections;

namespace TWG.TLDA.Generated
{{
    /// <summary>
    /// {system_name} - AI-Generated by Warbler for {game_type}
    /// Generated on {datetime.now().strftime("%Y-%m-%d %H:%M:%S")}
    /// </summary>
    public class {system_name} : MonoBehaviour
    {{
        [Header("AI-Optimized Wave Management")]
        [SerializeField] private float waveDuration = 30f;
        [SerializeField] private float waveBreakDuration = 10f;
        [SerializeField] private float difficultyIncrease = 1.2f;
        
        [Header("References")]
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private UpgradeManager upgradeManager;
        
        private int currentWave = 0;
        private bool waveActive = false;
        
        void Start()
        {{
            FindReferences();
            StartCoroutine(WaveLoop());
        }}
        
        void FindReferences()
        {{
            if (enemySpawner == null)
                enemySpawner = FindObjectOfType<EnemySpawner>();
            
            if (upgradeManager == null)
                upgradeManager = FindObjectOfType<UpgradeManager>();
        }}
        
        IEnumerator WaveLoop()
        {{
            while (true)
            {{
                yield return StartCoroutine(RunWave());
                yield return StartCoroutine(WaveBreak());
            }}
        }}
        
        IEnumerator RunWave()
        {{
            currentWave++;
            waveActive = true;
            
            Debug.Log($"🌊 Wave {{currentWave}} started! Duration: {{waveDuration}}s");
            
            // AI-recommended difficulty scaling
            if (enemySpawner != null)
            {{
                float newSpawnRate = 2f / Mathf.Pow(difficultyIncrease, currentWave - 1);
                int newMaxEnemies = Mathf.RoundToInt(20 * Mathf.Pow(difficultyIncrease, currentWave - 1));
                
                enemySpawner.SetSpawnRate(newSpawnRate);
                enemySpawner.SetMaxEnemies(newMaxEnemies);
                
                Debug.Log($"   📈 Spawn rate: {{newSpawnRate:F2}}s, Max enemies: {{newMaxEnemies}}");
            }}
            
            yield return new WaitForSeconds(waveDuration);
            
            waveActive = false;
            Debug.Log($"✅ Wave {{currentWave}} completed!");
        }}
        
        IEnumerator WaveBreak()
        {{
            Debug.Log($"⏸️ Wave break for {{waveBreakDuration}}s - Choose your upgrade!");
            
            // Show upgrade options
            if (upgradeManager != null)
            {{
                upgradeManager.ShowUpgradeOptions();
            }}
            
            yield return new WaitForSeconds(waveBreakDuration);
        }}
        
        // AI-suggested public methods for UI integration
        public int GetCurrentWave() => currentWave;
        public bool IsWaveActive() => waveActive;
        public float GetWaveProgress()
        {{
            if (!waveActive) return 0f;
            
            float elapsedTime = waveDuration; // Would need to track actual elapsed time
            return Mathf.Clamp01(elapsedTime / waveDuration);
        }}
    }}
}}'''
    
    def _generate_generic_script(self, system_name, game_type, analysis):
        """Generate a generic manager script"""
        return f'''using UnityEngine;

namespace TWG.TLDA.Generated
{{
    /// <summary>
    /// {system_name} - AI-Generated by Warbler for {game_type}
    /// Generated on {datetime.now().strftime("%Y-%m-%d %H:%M:%S")}
    /// 
    /// This is a generic manager script. Customize based on your specific needs.
    /// </summary>
    public class {system_name} : MonoBehaviour
    {{
        [Header("AI-Generated Manager")]
        [SerializeField] private bool debugMode = true;
        
        void Start()
        {{
            Initialize();
        }}
        
        void Initialize()
        {{
            if (debugMode)
                Debug.Log($"🤖 {{GetType().Name}} initialized by Warbler AI for {{"{game_type}"}}");
        }}
        
        void Update()
        {{
            // AI-recommended update pattern for performance
            if (Time.frameCount % 60 == 0) // Every 60 frames (1 second at 60 FPS)
            {{
                PeriodicUpdate();
            }}
        }}
        
        protected virtual void PeriodicUpdate()
        {{
            // Override in derived classes for specific functionality
        }}
    }}
}}'''

    def create_folder_structure(self, analysis):
        """Create the actual folder structure"""
        created_folders = []
        base_path = self.project_root / "Assets"
        
        for folder in analysis.get('recommended_folders', []):
            folder_path = base_path / folder
            folder_path.mkdir(parents=True, exist_ok=True)
            created_folders.append(str(folder_path))
            print(f"📁 Created: {folder}/")
        
        return created_folders

    def create_tldl_entry(self, analysis, user_request):
        """Create a real TLDL entry"""
        tldl_content = f"""# TLDL-{datetime.now().strftime('%Y-%m-%d')}-WarblerAI-{analysis['game_type'].replace('.', '')}

## Metadata
- Entry ID: TLDL-{datetime.now().strftime('%Y-%m-%d')}-WarblerAI-{analysis['game_type'].replace('.', '')}
- Author: Warbler AI Project Intelligence + Gemma3
- Context: AI-powered project generation for {analysis['game_type']}
- Summary: Complete project created from natural language request
- Tags: {', '.join(analysis['warbler_enhancement']['suggested_tldl_tags'])}

## Objective
**User Request:** "{user_request}"
**AI Interpretation:** {analysis['game_type']} ({analysis['complexity_level']} complexity)

## AI Analysis Results
### Game Type Detection
- **Type:** {analysis['game_type']}
- **Complexity:** {analysis['complexity_level']} 
- **Timeline:** {analysis['estimated_dev_time']}
- **Architecture:** {analysis['suggested_architecture']}

### Generated Systems
{chr(10).join(f"- **{system}.cs**: AI-generated {system.lower()} implementation" for system in analysis['required_systems'])}

### Key Mechanics Identified
{chr(10).join(f"- {mechanic}" for mechanic in analysis['key_mechanics'])}

### Folder Structure Created
{chr(10).join(f"- {folder}/" for folder in analysis['recommended_folders'])}

## Development Roadmap
{chr(10).join(f"{i+1}. {milestone}" for i, milestone in enumerate(analysis['warbler_enhancement']['development_milestones']))}

## Testing Strategy
{chr(10).join(f"- {test}" for test in analysis['warbler_enhancement']['testing_strategy'])}

## Warbler Insights
{analysis['warbler_insights']}

## Next Steps
1. **Review Generated Code**: Examine AI-generated scripts for customization opportunities
2. **Create Prefabs**: Build game objects based on generated system architecture
3. **Configure Scenes**: Set up game scenes according to requirements
4. **Test Integration**: Validate system interactions and performance
5. **Customize & Polish**: Adapt generated code to specific game vision

*Generated by Warbler AI Project Intelligence - transforming natural language into complete game projects!* 🧙‍♂️🤖⚡

---
**Achievement Unlocked:** 🏆 **AI Project Genesis** - Created complete game project from natural language in under 60 seconds!
"""
        
        tldl_path = self.project_root / "TLDL" / "entries" / f"TLDL-{datetime.now().strftime('%Y-%m-%d')}-WarblerAI-{analysis['game_type'].replace('.', '')}.md"
        tldl_path.parent.mkdir(parents=True, exist_ok=True)
        
        with open(tldl_path, 'w', encoding='utf-8') as f:
            f.write(tldl_content)
        
        return tldl_path

def main():
    if len(sys.argv) < 2:
        print("Usage:")
        print("  python warbler_project_intelligence.py 'project request' [--generate-files] [--prefer-ollama]")
        print("  python warbler_project_intelligence.py --auth-github")
        print("  python warbler_project_intelligence.py --test-connection")
        print("  python warbler_project_intelligence.py --provider-status")
        sys.exit(1)
    
    # Handle special commands
    if sys.argv[1] == '--auth-github':
        warbler = WarblerProjectIntelligence()
        if warbler.authenticate_github_copilot():
            print("✅ GitHub Copilot authentication successful!")
        else:
            print("❌ GitHub Copilot authentication failed")
            sys.exit(1)
        return
    
    if sys.argv[1] == '--test-connection':
        warbler = WarblerProjectIntelligence()
        status = warbler.get_provider_status()
        print("🔍 AI Provider Connection Status:")
        print(json.dumps(status, indent=2))
        return
    
    if sys.argv[1] == '--provider-status':
        warbler = WarblerProjectIntelligence()
        status = warbler.get_provider_status()
        print("📊 AI Provider Status:")
        for provider, info in status.items():
            status_icon = "✅" if info.get('connected') else "❌"
            print(f"  {provider.replace('_', ' ').title()}: {status_icon}")
            if provider == 'github_copilot':
                auth_status = "🔐 Authenticated" if info.get('authenticated') else "🔒 Not Authenticated"
                print(f"    Authentication: {auth_status}")
            elif provider == 'ollama':
                print(f"    Endpoint: {info.get('endpoint')}")
        return
    
    # Parse project request and options
    user_request = ' '.join(arg for arg in sys.argv[1:] if not arg.startswith('--'))
    generate_files = '--generate-files' in sys.argv
    prefer_ollama = '--prefer-ollama' in sys.argv
    
    if not user_request.strip():
        print("❌ Please provide a project request")
        sys.exit(1)
    
    print(f"[WARBLER] Analyzing request: '{user_request}'")
    
    # Initialize with provider preference
    warbler = WarblerProjectIntelligence(prefer_github_copilot=not prefer_ollama)
    analysis = warbler.analyze_project_request(user_request)
    
    safe_print(f"[ANALYSIS] Complete!")
    print(f"Game Type: {analysis['game_type']}")
    print(f"Complexity: {analysis['complexity_level']}")
    print(f"Estimated Timeline: {analysis.get('estimated_dev_time', 'TBD')}")
    print(f"AI Provider Used: {analysis.get('ai_provider_used', 'unknown')}")
    if 'providers_tried' in analysis:
        print(f"Providers Tried: {', '.join(analysis['providers_tried'])}")
    
    blueprint_path = warbler.create_project_blueprint(analysis, user_request)
    
    # Generate actual files if requested
    if generate_files:
        safe_print(f"\n[PROJECT] Generating real project files...")
        
        # Create folder structure
        created_folders = warbler.create_folder_structure(analysis)
        safe_print(f"[CREATED] {len(created_folders)} folders")
        
        # Generate Unity scripts
        created_scripts = warbler.generate_unity_scripts(analysis)
        safe_print(f"[CREATED] {len(created_scripts)} Unity scripts")
        
        # Create TLDL entry
        tldl_path = warbler.create_tldl_entry(analysis, user_request)
        print(f"📜 Created TLDL entry: {tldl_path}")
    
    # Output for Unity integration
    output = {
        'success': True,
        'analysis': analysis,
        'blueprint_path': str(blueprint_path),
        'files_generated': generate_files
    }
    
    print(f"\n🔮 Warbler Intelligence Output:")
    print(json.dumps(output, indent=2))

if __name__ == "__main__":
    main()
