#!/usr/bin/env python3
"""
Generic System Health Validator for Development Projects
Validates system architecture, component usage, and identifies potential issues.
Supports multiple rendering pipelines and development frameworks.

Copyright (C) 2025 Bellok

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
"""

import builtins
import os
import re
import json
import glob
import argparse
from typing import List, Dict, Any, Set
from pathlib import Path
from dataclasses import dataclass
from datetime import datetime

@dataclass
class SystemValidation:
    name: str
    file_path: str
    domain: str
    health_score: float
    issues: List[str]
    strengths: List[str]
    components: List[str]
    update_group: str
    has_burst: bool
    has_jobs: bool

class SystemValidator:
    def __init__(self, project_paths: List[str] = None):
        self.validation_errors = []
        self.system_registry = {}
        self.component_usage = {}
        self.dependency_graph = {}
        self.project_paths = project_paths or ["."]
        
    def validate_all_systems(self) -> Dict[str, Any]:
        """Comprehensive system validation for development projects"""
        print("🔍 Validating system architecture...")
        
        # Scan all configured project paths
        all_systems = []
        for project_path in self.project_paths:
            system_files = glob.glob(f"{project_path}/**/*System.cs", recursive=True)
            all_systems.extend(system_files)
        
        validated_systems = []
        
        for system_file in all_systems:
            validation = self.validate_system_file(system_file)
            if validation:
                validated_systems.append(validation)
                self.system_registry[validation.name] = validation
                
        # Build cross-system analysis
        self.analyze_component_dependencies()
        self.build_dependency_graph()
        
        report = self.generate_validation_report(validated_systems)
        print(f"✅ Validated {len(validated_systems)} system files")
        
        return report
        
    def validate_system_file(self, file_path: str) -> SystemValidation:
        """Validate individual system file"""
        try:
            with open(file_path, 'r', encoding='utf-8') as file:
                content = file.read()
                
            system_name = Path(file_path).stem
            domain = self.detect_domain(file_path, content)
            
            # Initialize validation tracking
            issues = []
            strengths = []
            health_score = 100.0
            
            # Core validations
            update_group = self.check_update_in_group(content, issues, strengths)
            self.check_component_access_patterns(content, system_name, issues, strengths)
            job_usage = self.check_job_system_usage(content, issues, strengths)
            burst_usage = self.check_burst_compilation(content, issues, strengths)
            self.check_entity_command_buffer_usage(content, issues, strengths)
            self.check_error_handling(content, issues, strengths)
            
            # Domain-specific validations
            self.validate_domain_patterns(content, system_name, domain, issues, strengths)
                
            # Extract component dependencies
            components = self.extract_component_dependencies(content)
            
            # Calculate health score
            health_score = max(0, health_score - (len(issues) * 10))
            health_score += len(strengths) * 5
            health_score = min(100, health_score)
            
            return SystemValidation(
                name=system_name,
                file_path=file_path,
                domain=domain,
                health_score=health_score,
                issues=issues,
                strengths=strengths,
                components=components,
                update_group=update_group,
                has_burst=burst_usage,
                has_jobs=job_usage
            )
            
        except Exception as e:
            print(f"⚠️ Error validating {file_path}: {e}")
            return None
    
    def detect_domain(self, file_path: str, content: str) -> str:
        """Detect the domain/framework being used"""
        if "Unity" in content and "Entities" in content:
            return "Unity-ECS"
        elif "namespace" in content:
            # Extract namespace for domain detection
            namespace_match = re.search(r'namespace\s+([^\s{]+)', content)
            if namespace_match:
                namespace = namespace_match.group(1)
                return namespace.split('.')[0]
        elif "UnityEngine" in content:
            return "Unity"
        elif "MonoBehaviour" in content:
            return "Unity-MonoBehaviour"
        else:
            return "Generic"
    
    def validate_domain_patterns(self, content: str, system_name: str, domain: str, issues: List[str], strengths: List[str]):
        """Validate domain-specific patterns"""
        if domain == "Unity-ECS":
            self.validate_unity_ecs_patterns(content, system_name, issues, strengths)
        elif domain.startswith("Unity"):
            self.validate_unity_patterns(content, system_name, issues, strengths)
        else:
            self.validate_generic_patterns(content, system_name, issues, strengths)
            
    def check_update_in_group(self, content: str, issues: List[str], strengths: List[str]) -> str:
        """Validate UpdateInGroup attribute"""
        group_match = re.search(r'\[UpdateInGroup\(typeof\(([^)]+)\)\)\]', content)
        
        if group_match:
            group = group_match.group(1)
            strengths.append(f"Proper UpdateInGroup: {group}")
            return group
        else:
            issues.append("Missing [UpdateInGroup] attribute")
            return "Unknown"
            
    def check_component_access_patterns(self, content: str, system_name: str, issues: List[str], strengths: List[str]):
        """Validate ComponentType usage patterns"""
        readonly_pattern = r'ComponentType\.ReadOnly<([^>]+)>'
        readwrite_pattern = r'ComponentType\.ReadWrite<([^>]+)>'
        
        readonly_components = set(re.findall(readonly_pattern, content))
        readwrite_components = set(re.findall(readwrite_pattern, content))
        
        # Check for proper readonly usage
        if readonly_components:
            strengths.append(f"Uses ReadOnly components: {len(readonly_components)} types")
            
        # Check for potential data races
        overlapping = readonly_components & readwrite_components
        if overlapping:
            issues.append(f"Data race potential: {overlapping} accessed as both ReadOnly and ReadWrite")
            
        # Check for missing readonly optimization
        if readwrite_components and not readonly_components:
            issues.append("Consider using ComponentType.ReadOnly for data you don't modify")
            
    def check_job_system_usage(self, content: str, issues: List[str], strengths: List[str]) -> bool:
        """Check for proper job system integration"""
        job_patterns = [
            'IJobEntity', 'IJob', 'IJobParallelFor', 'IJobChunk',
            'IJobParallelForDefer', 'IJobFor'
        ]
        
        has_jobs = any(pattern in content for pattern in job_patterns)
        
        if has_jobs:
            strengths.append("Uses Job System for performance")
            
            # Check for proper job scheduling
            if 'Dependency =' in content or '.Schedule(' in content:
                strengths.append("Proper job dependency management")
            else:
                issues.append("Job system used but dependency management unclear")
                
        return has_jobs
        
    def check_burst_compilation(self, content: str, issues: List[str], strengths: List[str]) -> bool:
        """Check for Burst compilation usage"""
        has_burst = '[BurstCompile]' in content
        
        if has_burst:
            strengths.append("Uses Burst compilation for performance")
            
            # Check for burst-compatible patterns
            if 'unsafe' in content:
                issues.append("Burst compilation with unsafe code - verify compatibility")
            
        return has_burst
        
    def check_entity_command_buffer_usage(self, content: str, issues: List[str], strengths: List[str]):
        """Validate EntityCommandBuffer usage patterns"""
        if 'EntityCommandBuffer' in content:
            strengths.append("Uses EntityCommandBuffer for structural changes")
            
            # Check for proper ECB patterns
            if 'EndSimulationEntityCommandBufferSystem' in content:
                strengths.append("Proper ECB system integration")
            elif 'BeginInitializationEntityCommandBufferSystem' in content:
                strengths.append("Proper initialization ECB usage")
            else:
                issues.append("EntityCommandBuffer usage without clear system integration")
                
    def check_error_handling(self, content: str, issues: List[str], strengths: List[str]):
        """Check for proper error handling"""
        if 'try' in content and 'catch' in content:
            strengths.append("Includes error handling")
            
            if 'ObjectDisposedException' in content:
                strengths.append("Handles ECS world disposal gracefully")
                
        elif 'throw' in content or 'Debug.LogError' in content:
            strengths.append("Includes error reporting")
            
    def validate_unity_ecs_patterns(self, content: str, system_name: str, issues: List[str], strengths: List[str]):
        """Unity ECS-specific validation patterns"""
        # Check for proper ECS patterns
        if 'ISystem' in content or 'SystemBase' in content:
            strengths.append("Uses modern Unity ECS patterns")
            
        # Check for component access patterns
        if 'ComponentDataFromEntity' in content:
            issues.append("Uses deprecated ComponentDataFromEntity - consider ComponentLookup")
        elif 'ComponentLookup' in content:
            strengths.append("Uses modern ComponentLookup pattern")
            
        # Check for job safety
        if 'JobHandle' in content:
            strengths.append("Implements job dependency management")
            
    def validate_unity_patterns(self, content: str, system_name: str, issues: List[str], strengths: List[str]):
        """General Unity validation patterns"""
        # Check for Unity best practices
        if 'Update()' in content and 'FixedUpdate()' in content:
            issues.append("Both Update and FixedUpdate - consider separating concerns")
            
        if 'null' in content and 'ReferenceEquals' not in content:
            strengths.append("Includes null checking")
            
        # Check for rendering pipeline compatibility
        pipeline_indicators = {
            'URP': ['UniversalRenderPipelineAsset', 'URP', 'Universal Render Pipeline'],
            'HDRP': ['HDRenderPipelineAsset', 'HDRP', 'High Definition'],
            'BRP': ['GraphicsSettings.renderPipelineAsset == null', 'Built-in'],
            'SRP': ['RenderPipelineAsset', 'SRP', 'Scriptable Render Pipeline']
        }
        
        detected_pipelines = []
        for pipeline, indicators in pipeline_indicators.items():
            if any(indicator in content for indicator in indicators):
                detected_pipelines.append(pipeline)
                
        if detected_pipelines:
            strengths.append(f"Rendering pipeline awareness: {', '.join(detected_pipelines)}")
        else:
            issues.append("No rendering pipeline detection - consider pipeline compatibility")
            
    def validate_generic_patterns(self, content: str, system_name: str, issues: List[str], strengths: List[str]):
        """Generic validation patterns for non-Unity systems"""
        # Check for general good practices
        if 'interface' in content:
            strengths.append("Uses interface abstraction")
            
        if 'abstract' in content:
            strengths.append("Uses abstract base classes")
            
        if 'static' in content and 'readonly' in content:
            strengths.append("Uses immutable static members")
            
        # Check for dependency injection patterns
        if 'DependencyInjection' in content or 'IoC' in content:
            strengths.append("Uses dependency injection")
            
    def extract_component_dependencies(self, content: str) -> List[str]:
        """Extract component dependencies from system"""
        component_patterns = [
            r'ComponentType\.ReadOnly<([^>]+)>',
            r'ComponentType\.ReadWrite<([^>]+)>',
            r'typeof\(([A-Z][A-Za-z]*(?:Component|Data))\)',
            r'HasComponent<([^>]+)>',
            r'GetComponent(?:Data)?<([^>]+)>'
        ]
        
        components = set()
        for pattern in component_patterns:
            matches = re.findall(pattern, content)
            components.update(matches)
            
        return list(components)
        
    def analyze_component_dependencies(self):
        """Analyze component usage across systems"""
        for system in self.system_registry.values():
            for component in system.components:
                if component not in self.component_usage:
                    self.component_usage[component] = []
                self.component_usage[component].append(system.name)
                
    def build_dependency_graph(self):
        """Build system dependency graph"""
        # This would analyze update order dependencies
        # For now, basic categorization by update groups
        for system in self.system_registry.values():
            group = system.update_group
            if group not in self.dependency_graph:
                self.dependency_graph[group] = []
            self.dependency_graph[group].append(system.name)
            
    def generate_validation_report(self, systems: List[SystemValidation]) -> Dict[str, Any]:
        """Generate comprehensive validation report"""
        domain_groups = {}
        for system in systems:
            domain = system.domain
            if domain not in domain_groups:
                domain_groups[domain] = []
            domain_groups[domain].append(system)
        
        total_issues = sum(len(s.issues) for s in systems)
        total_strengths = sum(len(s.strengths) for s in systems)
        avg_health_score = sum(s.health_score for s in systems) / len(systems) if systems else 0
        
        return {
            'timestamp': datetime.now().isoformat(),
            'summary': {
                'total_systems': len(systems),
                'domains': {domain: len(systems) for domain, systems in domain_groups.items()},
                'average_health_score': round(avg_health_score, 1),
                'total_issues': total_issues,
                'total_strengths': total_strengths
            },
            'systems': [
                {
                    'name': s.name,
                    'domain': s.domain,
                    'health_score': s.health_score,
                    'issues_count': len(s.issues),
                    'strengths_count': len(s.strengths),
                    'update_group': s.update_group,
                    'has_burst': s.has_burst,
                    'has_jobs': s.has_jobs
                }
                for s in systems
            ],
            'detailed_validations': [
                {
                    'name': s.name,
                    'file_path': s.file_path,
                    'domain': s.domain,
                    'health_score': s.health_score,
                    'issues': s.issues,
                    'strengths': s.strengths,
                    'components': s.components,
                    'update_group': s.update_group
                }
                for s in systems
            ],
            'component_usage': self.component_usage,
            'dependency_graph': self.dependency_graph
        }

def main():
    """Main entry point for system validation"""
    parser = argparse.ArgumentParser(description='Validate system architecture in development projects')
    parser.add_argument('--path', default='.', help='Path to project directory')
    parser.add_argument('--output', default='validation-report.json', help='Output file for validation report')
    parser.add_argument('--verbose', action='store_true', help='Verbose output')
    
    args = parser.parse_args()
    
    # Add missing import
    # from datetime import datetime  # Already imported at top
    
    validator = SystemValidator([args.path])
    report = validator.validate_all_systems()
    
    # Save report
    with open(args.output, 'w') as f:
        json.dump(report, f, indent=2)
        
    # Print summary
    summary = report['summary']
    print(f"\n📊 System Health Report:")
    print(f"   Total Systems: {summary['total_systems']}")
    for domain, count in summary['domains'].items():
        print(f"   {domain} Systems: {count}")
    print(f"   Average Health Score: {summary['average_health_score']}/100")
    print(f"   Issues Found: {summary['total_issues']}")
    print(f"   Strengths Identified: {summary['total_strengths']}")
    print(f"   Report saved to: {args.output}")
    
    return 0

if __name__ == "__main__":
    exit(main())
