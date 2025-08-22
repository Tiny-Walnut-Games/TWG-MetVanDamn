#!/usr/bin/env python3
"""
Comment Template Engine for Chronicle Keeper
Generates structured comment templates for lore preservation
"""

import argparse
import os
import re
import sys
import threading
import yaml
from pathlib import Path
from typing import Dict, Any, List, Optional
from datetime import datetime

class CommentTemplateEngine:
    """Engine for managing and generating Chronicle Keeper comment templates"""
    
    def __init__(self):
        self._usage_stats_lock = threading.Lock()
        self._usage_stats_timer = None
        self.project_root = self.find_project_root()
        self.templates_dir = self.project_root / "templates" / "comments"
        self.registry_path = self.templates_dir / "registry.yaml"
        self._registry = None
        
    def find_project_root(self) -> Path:
        """
        Find the project root directory.
        Looks for a '.chroniclekeeper' marker file or a '.git' directory.
        """
        current = Path.cwd()
        while current != current.parent:
            if (current / ".chroniclekeeper").exists() or (current / ".git").exists():
                return current
            current = current.parent
        return Path.cwd()
    
    @property
    def registry(self) -> Dict[str, Any]:
        """Load and cache the template registry"""
        if self._registry is None:
            if self.registry_path.exists():
                with open(self.registry_path, 'r') as f:
                    self._registry = yaml.safe_load(f)
            else:
                self._registry = {"templates": {}, "categories": {}}
        return self._registry
    
    def list_templates(self) -> List[Dict[str, Any]]:
        """List all available templates with metadata"""
        templates = []
        for template_id, template_info in self.registry.get("templates", {}).items():
            template_info["id"] = template_id
            templates.append(template_info)
        return templates
    
    def list_categories(self) -> Dict[str, Any]:
        """Get all template categories"""
        return self.registry.get("categories", {})
    
    def get_template_by_id(self, template_id: str) -> Optional[Dict[str, Any]]:
        """Get a specific template by ID"""
        template_info = self.registry.get("templates", {}).get(template_id)
        if not template_info:
            return None
            
        template_path = self.templates_dir / template_info["file"]
        if not template_path.exists():
            return None
            
        with open(template_path, 'r') as f:
            template_data = yaml.safe_load(f)
        
        return {**template_info, **template_data}
    
    def generate_template(self, template_id: str, values: Dict[str, str] = None, 
                         interactive: bool = True) -> str:
        """Generate a filled template"""
        template_data = self.get_template_by_id(template_id)
        if not template_data:
            raise ValueError(f"Template '{template_id}' not found")
        
        template_content = template_data.get("template", "")
        placeholders = template_data.get("placeholders", {})
        
        # If interactive, prompt for values
        if interactive and not values:
            values = self._interactive_fill(placeholders)
        
        # Fill template with values
        filled_template = self._fill_template(template_content, placeholders, values or {})
        
        return filled_template
    
    def _interactive_fill(self, placeholders: Dict[str, Any]) -> Dict[str, str]:
        """Interactively collect placeholder values"""
        values = {}
        print("\n📝 Chronicle Keeper Template Generator")
        print("=====================================")
        print("Fill in the following details (press Enter for defaults):\n")
        
        for placeholder_id, placeholder_info in placeholders.items():
            if isinstance(placeholder_info, dict):
                description = placeholder_info.get("description", placeholder_id)
                default = placeholder_info.get("default", "")
            else:
                description = placeholder_id
                default = str(placeholder_info)
            
            prompt = f"{description}"
            if default:
                prompt += f" [{default}]"
            prompt += ": "
            
            user_input = input(prompt).strip()
            values[placeholder_id] = user_input if user_input else default
        
        return values
    
    def _fill_template(self, template: str, placeholders: Dict[str, Any], 
                      values: Dict[str, str]) -> str:
        """Fill template with provided values"""
        filled = template
        
        for placeholder_id, placeholder_info in placeholders.items():
            placeholder_pattern = "{{ " + placeholder_id + " }}"
            
            if placeholder_id in values:
                replacement = values[placeholder_id]
            elif isinstance(placeholder_info, dict):
                replacement = placeholder_info.get("default", f"[{placeholder_id}]")
            else:
                replacement = str(placeholder_info)
            
            filled = filled.replace(placeholder_pattern, replacement)
        
        return filled
    
    def search_templates(self, query: str) -> List[Dict[str, Any]]:
        """Search templates by name, description, or category"""
        query_lower = query.lower()
        results = []
        
        for template_id, template_info in self.registry.get("templates", {}).items():
            template_info["id"] = template_id
            
            # Search in title, description, and use cases
            searchable_text = " ".join([
                template_info.get("title", ""),
                template_info.get("short_description", ""),
                template_info.get("category", ""),
                " ".join(template_info.get("use_cases", []))
            ]).lower()
            
            if query_lower in searchable_text:
                results.append(template_info)
        
        return results
    
    def update_usage_stats(self, template_id: str):
        """Update usage statistics for a template (batched/delayed write)"""
        try:
            with self._usage_stats_lock:
                stats = self.registry.get("statistics", {})
                usage_count = stats.get("usage_count", {})
                usage_count[template_id] = usage_count.get(template_id, 0) + 1
                
                # Update most used template
                most_used = max(usage_count.items(), key=lambda x: x[1])[0]
                stats["most_used_template"] = most_used
                stats["usage_count"] = usage_count
                stats["last_updated"] = datetime.now().isoformat() + "Z"
                
                # Update in-memory registry
                self._registry["statistics"] = stats

                # Schedule delayed write
                if self._usage_stats_timer is not None:
                    self._usage_stats_timer.cancel()
                self._usage_stats_timer = threading.Timer(1.0, self._flush_registry_to_disk)
                self._usage_stats_timer.daemon = True
                self._usage_stats_timer.start()
        except FileNotFoundError as e:
            print(f"⚠️  Could not update usage statistics: Registry file not found: {e}", file=sys.stderr)
        except PermissionError as e:
            print(f"⚠️  Could not update usage statistics: Permission denied: {e}", file=sys.stderr)
        except yaml.YAMLError as e:
            print(f"⚠️  Could not update usage statistics: YAML error: {e}", file=sys.stderr)
        except Exception as e:
            # Don't fail template generation if stats update fails
            print(f"⚠️  Could not update usage statistics due to an unexpected error: {e}", file=sys.stderr)

    def _flush_registry_to_disk(self):
        """Flush the in-memory registry to disk (called by timer)"""
        try:
            with self._usage_stats_lock:
                with open(self.registry_path, 'w') as f:
                    yaml.dump(self._registry, f, default_flow_style=False, sort_keys=False)
        except Exception as e:
            print(f"⚠️  Could not flush usage statistics to disk: {e}", file=sys.stderr)
def create_template_parser():
    """Create argument parser for template command"""
    parser = argparse.ArgumentParser(
        prog='lda template',
        description='Generate Chronicle Keeper comment templates',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  lda template --list                           # List available templates
  lda template --type comment --scenario bug_discovery   # Generate bug discovery template
  lda template --search "debugging"            # Search templates
  lda template --categories                    # Show template categories
        """
    )
    
    parser.add_argument('--type', choices=['comment'], default='comment',
                       help='Type of template to generate')
    
    parser.add_argument('--scenario', '--template-id', dest='template_id',
                       help='Template scenario to generate')
    
    parser.add_argument('--list', action='store_true',
                       help='List available templates')
    
    parser.add_argument('--categories', action='store_true',
                       help='Show template categories')
    
    parser.add_argument('--search', metavar='QUERY',
                       help='Search templates by keyword')
    
    parser.add_argument('--non-interactive', action='store_true',
                       help='Generate template with default values only')
    
    parser.add_argument('--output', metavar='FILE',
                       help='Output template to file instead of stdout')
    
    parser.add_argument('--values', metavar='KEY=VALUE', nargs='*',
                       help='Provide placeholder values directly (format: key=value)')
    
    return parser


def parse_values_args(values_args: List[str]) -> Dict[str, str]:
    """Parse key=value arguments into dictionary"""
    values = {}
    if values_args:
        for arg in values_args:
            if '=' in arg:
                key, value = arg.split('=', 1)
                values[key.strip()] = value.strip()
    return values


def main():
    """Main entry point for template command"""
    parser = create_template_parser()
    args = parser.parse_args()
    
    engine = CommentTemplateEngine()
    
    try:
        # List templates
        if args.list:
            print("📜 Available Chronicle Keeper Comment Templates:")
            print("=" * 50)
            templates = engine.list_templates()
            
            if not templates:
                print("No templates found. Check templates/comments/ directory.")
                return 1
            
            for template in templates:
                category = engine.list_categories().get(template.get("category", ""), {})
                category_icon = category.get("icon", "📜")
                
                print(f"\n{category_icon} {template.get('title', template['id'])}")
                print(f"   ID: {template['id']}")
                print(f"   Description: {template.get('short_description', 'No description')}")
                print(f"   Category: {template.get('category', 'general')}")
                print(f"   Time estimate: {template.get('estimated_time', 'Unknown')}")
                
                use_cases = template.get('use_cases', [])
                if use_cases:
                    print("   Use cases:")
                    for use_case in use_cases:
                        print(f"     • {use_case}")
            
            return 0
        
        # Show categories
        if args.categories:
            print("📂 Template Categories:")
            print("=" * 30)
            categories = engine.list_categories()
            
            for category_id, category_info in categories.items():
                icon = category_info.get("icon", "📁")
                name = category_info.get("name", category_id)
                description = category_info.get("description", "No description")
                
                print(f"\n{icon} {name}")
                print(f"   {description}")
            
            return 0
        
        # Search templates
        if args.search:
            print(f"🔍 Searching templates for: '{args.search}'")
            print("=" * 40)
            results = engine.search_templates(args.search)
            
            if not results:
                print(f"No templates found matching '{args.search}'")
                return 1
            
            for template in results:
                category = engine.list_categories().get(template.get("category", ""), {})
                category_icon = category.get("icon", "📜")
                print(f"\n{category_icon} {template.get('title', template['id'])} ({template['id']})")
                print(f"   {template.get('short_description', 'No description')}")
            
            return 0
        
        # Generate template
        if args.template_id:
            values = parse_values_args(args.values or [])
            interactive = not args.non_interactive and not values
            
            try:
                generated = engine.generate_template(
                    args.template_id,
                    values=values,
                    interactive=interactive
                )
                
                if args.output:
                    with open(args.output, 'w') as f:
                        f.write(generated)
                    print(f"✅ Template generated and saved to {args.output}")
                else:
                    print("\n📜 Generated Chronicle Keeper Template:")
                    print("=" * 50)
                    print(generated)
                
                # Update usage statistics
                engine.update_usage_stats(args.template_id)
                
                return 0
                
            except ValueError as e:
                print(f"❌ Error: {e}")
                print("\nAvailable templates:")
                for template in engine.list_templates():
                    print(f"  • {template['id']}: {template.get('title', 'No title')}")
                return 1
        
        # No specific action provided
        parser.print_help()
        return 0
        
    except KeyboardInterrupt:
        print("\n❌ Operation cancelled")
        return 1
    except Exception as e:
        print(f"❌ Error: {e}")
        return 1


if __name__ == "__main__":
    sys.exit(main())
