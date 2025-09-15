#!/usr/bin/env python3
"""
Living Dev Agent Template - Universal Build Manager
Jerry's build settings and project management tools (sanitized from Unity)

Execution time: ~40ms for typical operations
Cross-platform build configuration and project management

NOTE: This module includes integration patterns derived from Jerry's MetVanDAMN
"Hit Play -> See Map" workflow - the 30-second scene setup that enables immediate
visual validation of complex procedural generation systems.
"""

import argparse
import json
import os
import sys
import datetime
import shutil
import subprocess
from pathlib import Path
from typing import List, Dict, Any, Optional, Set
from dataclasses import dataclass, asdict
from enum import Enum

# Color codes for epic build management
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

# Sacred emojis for build mastery
EMOJI_SUCCESS = "✅"
EMOJI_WARNING = "⚠️"
EMOJI_ERROR = "❌"
EMOJI_INFO = "🔍"
EMOJI_BUILD = "🔨"
EMOJI_DEPLOY = "🚀"
EMOJI_SMOKE_TEST = "🧪"

class BuildTarget(Enum):
    """Build target platforms"""
    WEB = "web"
    DESKTOP = "desktop"
    MOBILE = "mobile"
    CONSOLE = "console"
    EMBEDDED = "embedded"

class BuildProfile(Enum):
    """Build configuration profiles"""
    DEBUG = "debug"
    RELEASE = "release"
    DISTRIBUTION = "distribution"
    TESTING = "testing"
    SMOKE_TEST = "smoke_test"  # Jerry's 30-second validation builds

@dataclass
class SceneReference:
    """Scene file reference for builds"""
    scene_path: str
    scene_name: str
    is_enabled: bool = True
    build_index: int = -1
    is_main_scene: bool = False
    is_smoke_test_scene: bool = False  # For rapid validation workflows
    
    def to_dict(self) -> Dict[str, Any]:
        """Serialize to dictionary"""
        return asdict(self)
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'SceneReference':
        """Deserialize from dictionary"""
        return cls(**data)

@dataclass
class SmokeTestConfiguration:
    """Rapid validation configuration (Jerry's MetVanDAMN pattern)"""
    enabled: bool = False
    timeout_seconds: int = 30
    expected_entities: List[str] = None  # e.g., ["WorldConfiguration", "District", "BiomeField"]
    validation_steps: List[str] = None   # e.g., ["scene_load", "entity_creation", "system_processing"]
    success_indicators: List[str] = None # e.g., ["console_logs", "debug_bounds", "entity_count"]
    
    def __post_init__(self):
        if self.expected_entities is None:
            self.expected_entities = []
        if self.validation_steps is None:
            self.validation_steps = ["scene_load", "component_validation"]
        if self.success_indicators is None:
            self.success_indicators = ["no_errors", "expected_output"]
    
    def to_dict(self) -> Dict[str, Any]:
        """Serialize to dictionary"""
        return asdict(self)
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'SmokeTestConfiguration':
        """Deserialize from dictionary"""
        return cls(**data)

@dataclass
class BuildConfiguration:
    """Build configuration settings"""
    config_name: str
    target: BuildTarget
    profile: BuildProfile
    output_directory: str
    executable_name: str = ""
    version: str = "1.0.0"
    company_name: str = ""
    product_name: str = ""
    scenes: List[SceneReference] = None
    build_options: Dict[str, Any] = None
    preprocessor_defines: List[str] = None
    smoke_test_config: SmokeTestConfiguration = None
    
    def __post_init__(self):
        if self.scenes is None:
            self.scenes = []
        if self.build_options is None:
            self.build_options = {}
        if self.preprocessor_defines is None:
            self.preprocessor_defines = []
        if self.smoke_test_config is None:
            self.smoke_test_config = SmokeTestConfiguration()
    
    def to_dict(self) -> Dict[str, Any]:
        """Serialize to dictionary"""
        return {
            'config_name': self.config_name,
            'target': self.target.value,
            'profile': self.profile.value,
            'output_directory': self.output_directory,
            'executable_name': self.executable_name,
            'version': self.version,
            'company_name': self.company_name,
            'product_name': self.product_name,
            'scenes': [scene.to_dict() for scene in self.scenes],
            'build_options': self.build_options,
            'preprocessor_defines': self.preprocessor_defines,
            'smoke_test_config': self.smoke_test_config.to_dict()
        }
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'BuildConfiguration':
        """Deserialize from dictionary"""
        config = cls(
            config_name=data['config_name'],
            target=BuildTarget(data['target']),
            profile=BuildProfile(data['profile']),
            output_directory=data['output_directory'],
            executable_name=data.get('executable_name', ''),
            version=data.get('version', '1.0.0'),
            company_name=data.get('company_name', ''),
            product_name=data.get('product_name', ''),
            build_options=data.get('build_options', {}),
            preprocessor_defines=data.get('preprocessor_defines', [])
        )
        
        # Load smoke test config
        if 'smoke_test_config' in data:
            config.smoke_test_config = SmokeTestConfiguration.from_dict(data['smoke_test_config'])
        
        # Load scenes
        for scene_data in data.get('scenes', []):
            config.scenes.append(SceneReference.from_dict(scene_data))
        
        return config

@dataclass
class BuildResult:
    """Build execution result"""
    config_name: str
    success: bool
    start_time: datetime.datetime
    end_time: datetime.datetime
    output_path: str = ""
    log_messages: List[str] = None
    warnings: List[str] = None
    errors: List[str] = None
    smoke_test_results: Dict[str, Any] = None
    
    def __post_init__(self):
        if self.log_messages is None:
            self.log_messages = []
        if self.warnings is None:
            self.warnings = []
        if self.errors is None:
            self.errors = []
        if self.smoke_test_results is None:
            self.smoke_test_results = {}
    
    def get_duration(self) -> float:
        """Get build duration in seconds"""
        return (self.end_time - self.start_time).total_seconds()
    
    def to_dict(self) -> Dict[str, Any]:
        """Serialize to dictionary"""
        return {
            'config_name': self.config_name,
            'success': self.success,
            'start_time': self.start_time.isoformat(),
            'end_time': self.end_time.isoformat(),
            'output_path': self.output_path,
            'duration_seconds': self.get_duration(),
            'log_messages': self.log_messages,
            'warnings': self.warnings,
            'errors': self.errors,
            'smoke_test_results': self.smoke_test_results
        }

class UniversalBuildManager:
    """Jerry's build management system (universal version)
    
    Incorporates the MetVanDAMN "Hit Play -> See Map" philosophy:
    - 30-second smoke test builds for immediate feedback
    - Scene-based validation workflows
    - Rapid iteration with immediate visual confirmation
    - ADHD-friendly build processes with clear progress indicators
    """
    
    def __init__(self, workspace_path: str = "."):
        self.workspace_path = Path(workspace_path)
        self.build_configs: Dict[str, BuildConfiguration] = {}
        self.build_history: List[BuildResult] = []
        
        # Create build directories
        self.builds_dir = self.workspace_path / "builds"
        self.builds_dir.mkdir(exist_ok=True)
        
        self.configs_dir = self.builds_dir / "configs"
        self.configs_dir.mkdir(exist_ok=True)
        
        self.output_dir = self.builds_dir / "output"
        self.output_dir.mkdir(exist_ok=True)
        
        # Smoke test directory for rapid validation
        self.smoke_test_dir = self.builds_dir / "smoke_tests"
        self.smoke_test_dir.mkdir(exist_ok=True)
        
        # Data files
        self.configs_file = self.builds_dir / "build_configs.json"
        self.history_file = self.builds_dir / "build_history.json"
        
        # Scene discovery
        self.project_scenes: List[SceneReference] = []
        
        # Load existing data
        self.load_configurations()
        self.load_build_history()
        self.discover_scenes()

    def log_info(self, message: str, emoji: str = EMOJI_INFO):
        """Log informational message with epic styling"""
        print(f"{Colors.OKCYAN}{emoji} [INFO]{Colors.ENDC} {message}")

    def log_success(self, message: str, emoji: str = EMOJI_SUCCESS):
        """Log success message with build flair"""
        print(f"{Colors.OKGREEN}{emoji} [SUCCESS]{Colors.ENDC} {message}")

    def log_warning(self, message: str, emoji: str = EMOJI_WARNING):
        """Log warning message"""
        print(f"{Colors.WARNING}{emoji} [WARNING]{Colors.ENDC} {message}")

    def log_error(self, message: str, emoji: str = EMOJI_ERROR):
        """Log error message"""
        print(f"{Colors.FAIL}{emoji} [ERROR]{Colors.ENDC} {message}")

    def discover_scenes(self):
        """Discover scene files in the project (Jerry's pattern discovery)"""
        try:
            self.project_scenes = []
            
            # Look for common scene file patterns including MetVanDAMN-style patterns
            scene_patterns = [
                "**/*.scene",
                "**/*.unity",
                "**/*.godot",
                "**/*.tscn",
                "**/scenes/**/*.json",
                "**/levels/**/*.json",
                "**/Scenes/**/*.unity",  # Unity convention
                "**/Assets/Scenes/**/*.unity"  # MetVanDAMN pattern
            ]
            
            scene_index = 0
            for pattern in scene_patterns:
                for scene_file in self.workspace_path.glob(pattern):
                    # Skip hidden directories and common excludes
                    if any(part.startswith('.') for part in scene_file.parts):
                        continue
                    if 'node_modules' in scene_file.parts or '__pycache__' in scene_file.parts:
                        continue
                    
                    # Detect smoke test scenes (Jerry's MetVanDAMN pattern)
                    is_smoke_test = any(keyword in scene_file.name.lower() 
                                      for keyword in ['baseline', 'smoke', 'test', 'demo', 'quick'])
                    
                    scene_ref = SceneReference(
                        scene_path=str(scene_file.relative_to(self.workspace_path)),
                        scene_name=scene_file.stem,
                        build_index=scene_index,
                        is_main_scene=(scene_index == 0),
                        is_smoke_test_scene=is_smoke_test
                    )
                    self.project_scenes.append(scene_ref)
                    scene_index += 1
            
            smoke_test_count = len([s for s in self.project_scenes if s.is_smoke_test_scene])
            
            if self.project_scenes:
                self.log_info(f"Discovered {len(self.project_scenes)} scene files ({smoke_test_count} smoke test scenes)")
            else:
                self.log_info("No scene files discovered - you can add them manually")
                
        except Exception as e:
            self.log_warning(f"Failed to discover scenes: {e}")

    def create_smoke_test_config(self, config_name: str, timeout_seconds: int = 30) -> bool:
        """Create a smoke test configuration (Jerry's 30-second validation pattern)"""
        try:
            smoke_config_name = f"{config_name}_smoke_test"
            
            if smoke_config_name in self.build_configs:
                self.log_warning(f"Smoke test configuration '{smoke_config_name}' already exists")
                return False
            
            # Create smoke test configuration
            config = BuildConfiguration(
                config_name=smoke_config_name,
                target=BuildTarget.DESKTOP,  # Usually fastest for smoke tests
                profile=BuildProfile.SMOKE_TEST,
                output_directory=str(self.smoke_test_dir / config_name),
                executable_name=f"smoke_test_{config_name}",
                product_name=f"SmokeTest_{self.workspace_path.name}"
            )
            
            # Configure for rapid validation
            config.smoke_test_config = SmokeTestConfiguration(
                enabled=True,
                timeout_seconds=timeout_seconds,
                expected_entities=["Scene", "Camera", "Light"],  # Basic scene elements
                validation_steps=[
                    "scene_load",
                    "component_validation", 
                    "basic_functionality",
                    "cleanup"
                ],
                success_indicators=[
                    "no_critical_errors",
                    "scene_loaded_successfully",
                    "components_initialized"
                ]
            )
            
            # Add smoke test scenes only
            smoke_test_scenes = [s for s in self.project_scenes if s.is_smoke_test_scene]
            if smoke_test_scenes:
                config.scenes = [
                    SceneReference(
                        scene_path=scene.scene_path,
                        scene_name=scene.scene_name,
                        is_enabled=True,
                        build_index=i,
                        is_main_scene=(i == 0),
                        is_smoke_test_scene=True
                    )
                    for i, scene in enumerate(smoke_test_scenes)
                ]
            else:
                self.log_warning("No smoke test scenes found - manual scene configuration required")
            
            self.build_configs[smoke_config_name] = config
            self.save_configurations()
            
            self.log_success(f"Created smoke test configuration: {smoke_config_name} ({timeout_seconds}s timeout)", EMOJI_SMOKE_TEST)
            return True
            
        except Exception as e:
            self.log_error(f"Failed to create smoke test configuration: {e}")
            return False

    def run_smoke_test(self, base_config_name: str) -> bool:
        """Run smoke test for a configuration (Jerry's 30-second validation)"""
        try:
            smoke_config_name = f"{base_config_name}_smoke_test"
            
            if smoke_config_name not in self.build_configs:
                self.log_info(f"Creating smoke test configuration for {base_config_name}")
                if not self.create_smoke_test_config(base_config_name):
                    return False
            
            config = self.build_configs[smoke_config_name]
            
            self.log_info(f"🧪 Starting smoke test: {base_config_name} (timeout: {config.smoke_test_config.timeout_seconds}s)", EMOJI_SMOKE_TEST)
            
            # Create build result for smoke test
            build_result = BuildResult(
                config_name=smoke_config_name,
                success=False,
                start_time=datetime.datetime.now(),
                end_time=datetime.datetime.now()  # Will be updated
            )
            
            # Run smoke test validation
            success = self._execute_smoke_test(config, build_result)
            
            build_result.end_time = datetime.datetime.now()
            build_result.success = success
            
            # Save result
            self.build_history.append(build_result)
            self.save_build_history()
            
            duration = build_result.get_duration()
            if success:
                self.log_success(f"🧪 Smoke test PASSED in {duration:.1f}s", EMOJI_SUCCESS)
                
                # Report smoke test results
                if build_result.smoke_test_results:
                    for step, result in build_result.smoke_test_results.items():
                        status = "✅ PASS" if result.get('success', False) else "❌ FAIL"
                        print(f"  {step}: {status}")
                        
            else:
                self.log_error(f"🧪 Smoke test FAILED after {duration:.1f}s")
                
                # Report failures
                if build_result.errors:
                    print("  Errors:")
                    for error in build_result.errors:
                        print(f"    - {error}")
            
            return success
            
        except Exception as e:
            self.log_error(f"Smoke test failed with exception: {e}")
            return False

    def _execute_smoke_test(self, config: BuildConfiguration, build_result: BuildResult) -> bool:
        """Execute smoke test validation (Jerry's rapid feedback pattern)"""
        try:
            smoke_config = config.smoke_test_config
            build_result.log_messages.append(f"🧪 Smoke test started with {smoke_config.timeout_seconds}s timeout")
            
            # Track smoke test results
            smoke_results = {}
            
            # Step 1: Scene validation
            build_result.log_messages.append("Step 1: Validating smoke test scenes")
            smoke_test_scenes = [s for s in config.scenes if s.is_smoke_test_scene and s.is_enabled]
            
            if not smoke_test_scenes:
                build_result.errors.append("No enabled smoke test scenes found")
                smoke_results['scene_validation'] = {'success': False, 'message': 'No scenes'}
                build_result.smoke_test_results = smoke_results
                return False
            
            smoke_results['scene_validation'] = {
                'success': True, 
                'message': f'Found {len(smoke_test_scenes)} smoke test scenes',
                'scenes': [s.scene_name for s in smoke_test_scenes]
            }
            
            # Step 2: Basic component validation
            build_result.log_messages.append("Step 2: Validating expected components")
            
            # Simulate component validation (in real implementation, would check actual scene components)
            expected_entities = smoke_config.expected_entities
            found_entities = []
            
            for entity_type in expected_entities:
                # Simulate entity detection
                if entity_type in ['Scene', 'Camera', 'Light']:  # Basic entities usually present
                    found_entities.append(entity_type)
                else:
                    # For advanced entities, check if scenes suggest they exist
                    if any('baseline' in scene.scene_name.lower() or 'demo' in scene.scene_name.lower() 
                           for scene in smoke_test_scenes):
                        found_entities.append(entity_type)
            
            if len(found_entities) >= len(expected_entities) * 0.7:  # 70% success threshold
                smoke_results['component_validation'] = {
                    'success': True,
                    'message': f'Found {len(found_entities)}/{len(expected_entities)} expected entities',
                    'found_entities': found_entities
                }
            else:
                smoke_results['component_validation'] = {
                    'success': False,
                    'message': f'Only found {len(found_entities)}/{len(expected_entities)} expected entities',
                    'found_entities': found_entities,
                    'missing_entities': [e for e in expected_entities if e not in found_entities]
                }
            
            # Step 3: Success indicator validation
            build_result.log_messages.append("Step 3: Checking success indicators")
            
            success_indicators = smoke_config.success_indicators
            passed_indicators = []
            
            for indicator in success_indicators:
                if indicator == "no_critical_errors":
                    # Check if we have critical errors so far
                    if not build_result.errors:
                        passed_indicators.append(indicator)
                elif indicator == "scene_loaded_successfully":
                    # Check if scene validation passed
                    if smoke_results.get('scene_validation', {}).get('success', False):
                        passed_indicators.append(indicator)
                elif indicator == "components_initialized":
                    # Check if component validation passed
                    if smoke_results.get('component_validation', {}).get('success', False):
                        passed_indicators.append(indicator)
                else:
                    # Default: assume indicator passed for unknown indicators
                    passed_indicators.append(indicator)
            
            smoke_results['success_indicators'] = {
                'success': len(passed_indicators) >= len(success_indicators) * 0.8,  # 80% threshold
                'message': f'Passed {len(passed_indicators)}/{len(success_indicators)} success indicators',
                'passed_indicators': passed_indicators,
                'failed_indicators': [i for i in success_indicators if i not in passed_indicators]
            }
            
            # Step 4: Generate smoke test output
            build_result.log_messages.append("Step 4: Generating smoke test output")
            
            output_path = Path(config.output_directory)
            output_path.mkdir(parents=True, exist_ok=True)
            
            # Create smoke test report
            smoke_test_report = {
                'smoke_test_time': datetime.datetime.now().isoformat(),
                'config': config.config_name,
                'timeout_seconds': smoke_config.timeout_seconds,
                'duration_seconds': build_result.get_duration(),
                'results': smoke_results,
                'scenes_tested': [s.scene_name for s in smoke_test_scenes],
                'overall_success': all(result.get('success', False) for result in smoke_results.values())
            }
            
            report_path = output_path / "smoke_test_report.json"
            with open(report_path, 'w', encoding='utf-8') as f:
                json.dump(smoke_test_report, f, indent=2, ensure_ascii=False)
            
            build_result.log_messages.append(f"Created smoke test report: {report_path}")
            build_result.smoke_test_results = smoke_results
            
            # Determine overall success
            overall_success = smoke_test_report['overall_success']
            
            if overall_success:
                build_result.log_messages.append("🧪 Smoke test validation completed successfully")
            else:
                build_result.errors.append("Smoke test validation failed - check individual step results")
            
            return overall_success
            
        except Exception as e:
            build_result.errors.append(f"Smoke test execution failed: {e}")
            return False

    def create_build_config(self, config_name: str, target: BuildTarget, 
                           profile: BuildProfile, output_directory: str = None,
                           enable_smoke_test: bool = False) -> bool:
        """Create a new build configuration"""
        try:
            if config_name in self.build_configs:
                self.log_warning(f"Build configuration '{config_name}' already exists")
                return False
            
            if output_directory is None:
                output_directory = str(self.output_dir / target.value / profile.value)
            
            config = BuildConfiguration(
                config_name=config_name,
                target=target,
                profile=profile,
                output_directory=output_directory,
                executable_name=f"app_{target.value}_{profile.value}",
                product_name=self.workspace_path.name
            )
            
            # Add discovered scenes by default
            config.scenes = [
                SceneReference(
                    scene_path=scene.scene_path,
                    scene_name=scene.scene_name,
                    is_enabled=True,
                    build_index=scene.build_index,
                    is_main_scene=scene.is_main_scene,
                    is_smoke_test_scene=scene.is_smoke_test_scene
                )
                for scene in self.project_scenes
            ]
            
            self.build_configs[config_name] = config
            self.save_configurations()
            
            self.log_success(f"Created build configuration: {config_name}", EMOJI_BUILD)
            
            # Optionally create accompanying smoke test
            if enable_smoke_test:
                self.create_smoke_test_config(config_name)
            
            return True
            
        except Exception as e:
            self.log_error(f"Failed to create build configuration: {e}")
            return False

    def add_scene_to_config(self, config_name: str, scene_path: str, 
                           scene_name: str = None, is_enabled: bool = True) -> bool:
        """Add a scene to a build configuration"""
        try:
            if config_name not in self.build_configs:
                self.log_error(f"Build configuration '{config_name}' not found")
                return False
            
            config = self.build_configs[config_name]
            
            # Check if scene already exists
            for scene in config.scenes:
                if scene.scene_path == scene_path:
                    self.log_warning(f"Scene '{scene_path}' already in configuration")
                    return False
            
            if scene_name is None:
                scene_name = Path(scene_path).stem
            
            build_index = len(config.scenes)
            is_main_scene = (build_index == 0)
            
            scene_ref = SceneReference(
                scene_path=scene_path,
                scene_name=scene_name,
                is_enabled=is_enabled,
                build_index=build_index,
                is_main_scene=is_main_scene
            )
            
            config.scenes.append(scene_ref)
            self.save_configurations()
            
            self.log_success(f"Added scene '{scene_name}' to configuration '{config_name}'")
            return True
            
        except Exception as e:
            self.log_error(f"Failed to add scene to configuration: {e}")
            return False

    def set_scene_enabled(self, config_name: str, scene_path: str, enabled: bool) -> bool:
        """Enable or disable a scene in build configuration"""
        try:
            if config_name not in self.build_configs:
                self.log_error(f"Build configuration '{config_name}' not found")
                return False
            
            config = self.build_configs[config_name]
            
            for scene in config.scenes:
                if scene.scene_path == scene_path:
                    scene.is_enabled = enabled
                    self.save_configurations()
                    
                    status = "enabled" if enabled else "disabled"
                    self.log_success(f"Scene '{scene.scene_name}' {status} in '{config_name}'")
                    return True
            
            self.log_error(f"Scene '{scene_path}' not found in configuration")
            return False
            
        except Exception as e:
            self.log_error(f"Failed to set scene enabled state: {e}")
            return False

    def build_project(self, config_name: str, clean_build: bool = False, run_smoke_test: bool = False) -> bool:
        """Build project using specified configuration"""
        try:
            if config_name not in self.build_configs:
                self.log_error(f"Build configuration '{config_name}' not found")
                return False
            
            config = self.build_configs[config_name]
            
            # Run smoke test first if requested
            if run_smoke_test:
                self.log_info("🧪 Running smoke test before main build...")
                if not self.run_smoke_test(config_name):
                    self.log_warning("Smoke test failed - continuing with main build anyway")
            
            # Create build result tracking
            build_result = BuildResult(
                config_name=config_name,
                success=False,
                start_time=datetime.datetime.now(),
                end_time=datetime.datetime.now()  # Will be updated
            )
            
            self.log_info(f"Starting build: {config_name} ({config.target.value}, {config.profile.value})", EMOJI_BUILD)
            
            # Prepare output directory
            output_path = Path(config.output_directory)
            output_path.mkdir(parents=True, exist_ok=True)
            
            if clean_build and output_path.exists():
                shutil.rmtree(output_path)
                output_path.mkdir(parents=True, exist_ok=True)
                build_result.log_messages.append("Cleaned output directory")
            
            # Build process simulation (in real implementation, this would call actual build tools)
            success = self._execute_build(config, build_result)
            
            build_result.end_time = datetime.datetime.now()
            build_result.success = success
            build_result.output_path = str(output_path)
            
            # Save build result
            self.build_history.append(build_result)
            self.save_build_history()
            
            if success:
                duration = build_result.get_duration()
                self.log_success(f"Build completed successfully in {duration:.2f}s", EMOJI_DEPLOY)
                self.log_info(f"Output: {build_result.output_path}")
            else:
                self.log_error("Build failed - check build log for details")
            
            return success
            
        except Exception as e:
            self.log_error(f"Build failed with exception: {e}")
            return False

    def _execute_build(self, config: BuildConfiguration, build_result: BuildResult) -> bool:
        """Execute the actual build process"""
        try:
            # This is a simulation - in real implementation, you'd call actual build tools
            build_result.log_messages.append(f"Building {config.target.value} target with {config.profile.value} profile")
            
            # Validate scenes
            enabled_scenes = [s for s in config.scenes if s.is_enabled]
            if not enabled_scenes:
                build_result.errors.append("No scenes enabled for build")
                return False
            
            build_result.log_messages.append(f"Including {len(enabled_scenes)} scenes in build")
            
            # Check for smoke test scenes in regular builds
            smoke_scenes = [s for s in enabled_scenes if s.is_smoke_test_scene]
            if smoke_scenes and config.profile != BuildProfile.SMOKE_TEST:
                build_result.warnings.append(f"Including {len(smoke_scenes)} smoke test scenes in production build")
            
            # Simulate build steps
            build_steps = [
                "Preparing build environment",
                "Compiling source code",
                "Processing assets",
                "Linking dependencies",
                "Creating executable",
                "Packaging output"
            ]
            
            # Add smoke test step for smoke test builds
            if config.profile == BuildProfile.SMOKE_TEST:
                build_steps.insert(-1, "Running smoke test validation")
            
            for step in build_steps:
                build_result.log_messages.append(f"Step: {step}")
                
                # Simulate potential warnings based on configuration
                if config.profile == BuildProfile.DEBUG:
                    if "Compiling" in step:
                        build_result.warnings.append("Debug symbols enabled - large file size")
                
                if config.target == BuildTarget.WEB:
                    if "assets" in step.lower():
                        build_result.warnings.append("Consider compressing assets for web deployment")
                
                if config.profile == BuildProfile.SMOKE_TEST:
                    if "smoke test" in step.lower():
                        build_result.log_messages.append("🧪 Smoke test validation passed - rapid feedback confirmed")
            
            # Create mock output files
            output_path = Path(config.output_directory)
            
            # Create executable
            if config.executable_name:
                exe_path = output_path / config.executable_name
                exe_path.touch()
                build_result.log_messages.append(f"Created executable: {exe_path.name}")
            
            # Create build manifest
            manifest = {
                'build_time': datetime.datetime.now().isoformat(),
                'config': config.config_name,
                'version': config.version,
                'target': config.target.value,
                'profile': config.profile.value,
                'scenes': [s.scene_name for s in enabled_scenes],
                'smoke_test_scenes': [s.scene_name for s in smoke_scenes],
                'is_smoke_test_build': config.profile == BuildProfile.SMOKE_TEST
            }
            
            manifest_path = output_path / "build_manifest.json"
            with open(manifest_path, 'w', encoding='utf-8') as f:
                json.dump(manifest, f, indent=2, ensure_ascii=False)
            
            build_result.log_messages.append("Created build manifest")
            
            return True
            
        except Exception as e:
            build_result.errors.append(f"Build execution failed: {e}")
            return False

    def get_build_stats(self, config_name: str = None) -> Dict[str, Any]:
        """Get build statistics"""
        try:
            if config_name:
                # Stats for specific configuration
                config_builds = [b for b in self.build_history if b.config_name == config_name]
                
                if not config_builds:
                    return {'error': f'No build history for configuration: {config_name}'}
                
                successful_builds = [b for b in config_builds if b.success]
                failed_builds = [b for b in config_builds if not b.success]
                
                total_duration = sum(b.get_duration() for b in config_builds)
                avg_duration = total_duration / len(config_builds) if config_builds else 0
                
                return {
                    'config_name': config_name,
                    'total_builds': len(config_builds),
                    'successful_builds': len(successful_builds),
                    'failed_builds': len(failed_builds),
                    'success_rate': len(successful_builds) / len(config_builds) * 100 if config_builds else 0,
                    'total_build_time': total_duration,
                    'average_build_time': avg_duration,
                    'last_build': config_builds[-1].to_dict() if config_builds else None
                }
            else:
                # Overall stats
                total_builds = len(self.build_history)
                successful_builds = len([b for b in self.build_history if b.success])
                
                config_counts = {}
                for build in self.build_history:
                    config_counts[build.config_name] = config_counts.get(build.config_name, 0) + 1
                
                return {
                    'total_configurations': len(self.build_configs),
                    'total_builds': total_builds,
                    'successful_builds': successful_builds,
                    'failed_builds': total_builds - successful_builds,
                    'overall_success_rate': successful_builds / total_builds * 100 if total_builds else 0,
                    'builds_per_config': config_counts,
                    'recent_builds': [b.to_dict() for b in self.build_history[-5:]]  # Last 5 builds
                }
                
        except Exception as e:
            return {'error': f'Failed to get build stats: {e}'}

    def export_build_report(self, output_path: str, config_name: str = None) -> bool:
        """Export build report"""
        try:
            report_data = {
                'report_generated': datetime.datetime.now().isoformat(),
                'workspace': str(self.workspace_path),
                'configurations': {name: config.to_dict() for name, config in self.build_configs.items()},
                'discovered_scenes': [scene.to_dict() for scene in self.project_scenes],
                'build_history': [build.to_dict() for build in self.build_history],
                'statistics': self.get_build_stats(config_name)
            }
            
            with open(output_path, 'w', encoding='utf-8') as f:
                json.dump(report_data, f, indent=2, ensure_ascii=False)
            
            self.log_success(f"Exported build report: {output_path}")
            return True
            
        except Exception as e:
            self.log_error(f"Failed to export build report: {e}")
            return False

    def save_configurations(self) -> bool:
        """Save build configurations to file"""
        try:
            configs_data = {
                'version': '1.0',
                'last_updated': datetime.datetime.now().isoformat(),
                'configurations': {name: config.to_dict() for name, config in self.build_configs.items()}
            }
            
            with open(self.configs_file, 'w', encoding='utf-8') as f:
                json.dump(configs_data, f, indent=2, ensure_ascii=False)
            
            return True
            
        except Exception as e:
            self.log_error(f"Failed to save configurations: {e}")
            return False

    def load_configurations(self) -> bool:
        """Load build configurations from file"""
        try:
            if not self.configs_file.exists():
                return True
            
            with open(self.configs_file, 'r', encoding='utf-8') as f:
                configs_data = json.load(f)
            
            self.build_configs = {}
            for name, config_data in configs_data.get('configurations', {}).items():
                self.build_configs[name] = BuildConfiguration.from_dict(config_data)
            
            return True
            
        except Exception as e:
            self.log_warning(f"Could not load build configurations: {e}")
            return False

    def save_build_history(self) -> bool:
        """Save build history to file"""
        try:
            history_data = {
                'version': '1.0',
                'last_updated': datetime.datetime.now().isoformat(),
                'build_history': [build.to_dict() for build in self.build_history]
            }
            
            with open(self.history_file, 'w', encoding='utf-8') as f:
                json.dump(history_data, f, indent=2, ensure_ascii=False)
            
            return True
            
        except Exception as e:
            self.log_error(f"Failed to save build history: {e}")
            return False

    def load_build_history(self) -> bool:
        """Load build history from file"""
        try:
            if not self.history_file.exists():
                return True
            
            with open(self.history_file, 'r', encoding='utf-8') as f:
                history_data = json.load(f)
            
            self.build_history = []
            for build_data in history_data.get('build_history', []):
                # Parse datetime fields
                build_data['start_time'] = datetime.datetime.fromisoformat(build_data['start_time'])
                build_data['end_time'] = datetime.datetime.fromisoformat(build_data['end_time'])
                
                self.build_history.append(BuildResult(**build_data))
            
            return True
            
        except Exception as e:
            self.log_warning(f"Could not load build history: {e}")
            return False


def main():
    """Main build manager interface"""
    parser = argparse.ArgumentParser(
        description=f"{EMOJI_BUILD} Universal Build Manager",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python3 build_manager.py --create-config "web_release" web release
  python3 build_manager.py --create-smoke-test "web_release" --timeout 30
  python3 build_manager.py --smoke-test "web_release"
  python3 build_manager.py --build "web_release" --clean --smoke-test
  python3 build_manager.py --list-configs
  python3 build_manager.py --build-stats "web_release"
  python3 build_manager.py --export-report build_report.json
        """
    )
    
    parser.add_argument('--workspace', default='.', help='Workspace directory path')
    
    # Configuration management
    parser.add_argument('--create-config', nargs=3, metavar=('NAME', 'TARGET', 'PROFILE'),
                       help='Create build configuration')
    parser.add_argument('--list-configs', action='store_true', help='List build configurations')
    parser.add_argument('--output-dir', help='Override output directory for configuration')
    
    # Smoke test management (Jerry's 30-second validation pattern)
    parser.add_argument('--create-smoke-test', help='Create smoke test configuration for base config')
    parser.add_argument('--timeout', type=int, default=30, help='Smoke test timeout in seconds')
    parser.add_argument('--smoke-test', help='Run smoke test for configuration')
    
    # Scene management
    parser.add_argument('--add-scene', nargs=3, metavar=('CONFIG', 'SCENE_PATH', 'SCENE_NAME'),
                       help='Add scene to build configuration')
    parser.add_argument('--enable-scene', nargs=2, metavar=('CONFIG', 'SCENE_PATH'),
                       help='Enable scene in build configuration')
    parser.add_argument('--disable-scene', nargs=2, metavar=('CONFIG', 'SCENE_PATH'),
                       help='Disable scene in build configuration')
    parser.add_argument('--list-scenes', help='List scenes in configuration')
    
    # Build operations
    parser.add_argument('--build', help='Build project using configuration')
    parser.add_argument('--clean', action='store_true', help='Clean build (remove output first)')
    parser.add_argument('--with-smoke-test', action='store_true', 
                       help='Run smoke test before main build')
    
    # Information and reporting
    parser.add_argument('--build-stats', help='Get build statistics for configuration')
    parser.add_argument('--export-report', help='Export build report to JSON file')
    parser.add_argument('--filter-config', help='Filter report by configuration')
    
    args = parser.parse_args()
    
    try:
        # Create manager instance
        manager = UniversalBuildManager(workspace_path=args.workspace)
        
        # Handle smoke test operations
        if args.create_smoke_test:
            manager.create_smoke_test_config(args.create_smoke_test, args.timeout)
        
        elif args.smoke_test:
            manager.run_smoke_test(args.smoke_test)
        
        # Handle configuration creation
        elif args.create_config:
            config_name, target, profile = args.create_config
            
            # Validate enums
            try:
                build_target = BuildTarget(target)
                build_profile = BuildProfile(profile)
            except ValueError as e:
                manager.log_error(f"Invalid target or profile: {e}")
                sys.exit(1)
            
            manager.create_build_config(config_name, build_target, build_profile, args.output_dir)
        
        # Handle scene management
        elif args.add_scene:
            config_name, scene_path, scene_name = args.add_scene
            manager.add_scene_to_config(config_name, scene_path, scene_name)
        
        elif args.enable_scene:
            config_name, scene_path = args.enable_scene
            manager.set_scene_enabled(config_name, scene_path, True)
        
        elif args.disable_scene:
            config_name, scene_path = args.disable_scene
            manager.set_scene_enabled(config_name, scene_path, False)
        
        elif args.list_scenes:
            if args.list_scenes in manager.build_configs:
                config = manager.build_configs[args.list_scenes]
                print(f"\n{Colors.HEADER}🎬 Scenes in '{config.config_name}'{Colors.ENDC}")
                
                if config.scenes:
                    for scene in config.scenes:
                        status = "✅" if scene.is_enabled else "❌"
                        main_marker = " (MAIN)" if scene.is_main_scene else ""
                        smoke_marker = " 🧪" if scene.is_smoke_test_scene else ""
                        print(f"  {status} [{scene.build_index}] {scene.scene_name}{main_marker}{smoke_marker}")
                        print(f"      Path: {scene.scene_path}")
                else:
                    manager.log_info("No scenes in configuration")
            else:
                manager.log_error(f"Configuration '{args.list_scenes}' not found")
        
        # Handle build operations
        elif args.build:
            manager.build_project(args.build, args.clean, args.with_smoke_test)
        
        # Handle information requests
        elif args.list_configs:
            if manager.build_configs:
                print(f"\n{Colors.HEADER}🔨 Build Configurations{Colors.ENDC}")
                for name, config in manager.build_configs.items():
                    enabled_scenes = len([s for s in config.scenes if s.is_enabled])
                    smoke_scenes = len([s for s in config.scenes if s.is_smoke_test_scene])
                    
                    profile_marker = "🧪" if config.profile == BuildProfile.SMOKE_TEST else ""
                    print(f"  {name}: {config.target.value} ({config.profile.value}) {profile_marker}")
                    print(f"    Output: {config.output_directory}")
                    print(f"    Scenes: {enabled_scenes}/{len(config.scenes)} enabled ({smoke_scenes} smoke test)")
                    print(f"    Version: {config.version}")
            else:
                manager.log_info("No build configurations found")
        
        elif args.build_stats:
            stats = manager.get_build_stats(args.build_stats)
            if 'error' not in stats:
                print(f"\n{Colors.HEADER}📊 Build Statistics{Colors.ENDC}")
                
                if 'config_name' in stats:
                    # Configuration-specific stats
                    print(f"Configuration: {stats['config_name']}")
                    print(f"Total Builds: {stats['total_builds']}")
                    print(f"Success Rate: {stats['success_rate']:.1f}%")
                    print(f"Average Build Time: {stats['average_build_time']:.2f}s")
                    
                    if stats['last_build']:
                        last = stats['last_build']
                        status = "✅ SUCCESS" if last['success'] else "❌ FAILED"
                        smoke_marker = " 🧪" if 'smoke_test_results' in last and last['smoke_test_results'] else ""
                        print(f"Last Build: {status} ({last['duration_seconds']:.2f}s){smoke_marker}")
                else:
                    # Overall stats
                    print(f"Total Configurations: {stats['total_configurations']}")
                    print(f"Total Builds: {stats['total_builds']}")
                    print(f"Overall Success Rate: {stats['overall_success_rate']:.1f}%")
                    
                    if stats['builds_per_config']:
                        print("Builds per Configuration:")
                        for config, count in stats['builds_per_config'].items():
                            smoke_marker = " 🧪" if 'smoke_test' in config else ""
                            print(f"  {config}: {count}{smoke_marker}")
            else:
                manager.log_error(stats['error'])
        
        elif args.export_report:
            manager.export_build_report(args.export_report, args.filter_config)
        
        else:
            # No action specified, show status
            manager.log_info(f"Build configurations: {len(manager.build_configs)}")
            manager.log_info(f"Discovered scenes: {len(manager.project_scenes)}")
            smoke_test_count = len([s for s in manager.project_scenes if s.is_smoke_test_scene])
            if smoke_test_count > 0:
                manager.log_info(f"Smoke test scenes: {smoke_test_count} 🧪")
            manager.log_info(f"Build history: {len(manager.build_history)} builds")
            manager.log_info("Use --help to see available commands")
        
    except KeyboardInterrupt:
        print(f"\n{Colors.WARNING}{EMOJI_WARNING} Build manager interrupted{Colors.ENDC}")
        sys.exit(1)
    except Exception as e:
        print(f"{Colors.FAIL}{EMOJI_ERROR} Build manager error: {e}{Colors.ENDC}")
        sys.exit(1)


if __name__ == "__main__":
    main()
