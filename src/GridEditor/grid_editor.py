#!/usr/bin/env python3
"""
Living Dev Agent Template - Universal Grid Layer Editor
Jerry's 2D grid layout system (sanitized from Unity Grid Layer Editor)

Execution time: ~35ms for typical grid operations
Cross-platform grid-based layout and design tools
"""

import argparse
import json
import os
import sys
import datetime
from pathlib import Path
from typing import List, Dict, Any, Optional, Tuple, Union
from dataclasses import dataclass, asdict
from enum import Enum
import math

# Color codes for epic grid management
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

# Sacred emojis for grid mastery
EMOJI_SUCCESS = "✅"
EMOJI_WARNING = "⚠️"
EMOJI_ERROR = "❌"
EMOJI_INFO = "🔍"
EMOJI_GRID = "📐"
EMOJI_LAYER = "🗂️"

class GridProjection(Enum):
    """Grid projection types (based on Jerry's Unity implementations)"""
    PLATFORMER = "platformer"
    TOP_DOWN = "top_down"
    ISOMETRIC = "isometric"
    HEXAGONAL = "hexagonal"

class GridCellType(Enum):
    """Grid cell content types"""
    EMPTY = "empty"
    SOLID = "solid"
    PLATFORM = "platform"
    HAZARD = "hazard"
    COLLECTIBLE = "collectible"
    SPAWN = "spawn"
    CUSTOM = "custom"

@dataclass
class GridCell:
    """Universal grid cell definition"""
    x: int
    y: int
    cell_type: GridCellType = GridCellType.EMPTY
    properties: Dict[str, Any] = None
    layer_id: str = "default"
    
    def __post_init__(self):
        if self.properties is None:
            self.properties = {}
    
    def to_dict(self) -> Dict[str, Any]:
        """Serialize to dictionary"""
        return {
            'x': self.x,
            'y': self.y,
            'cell_type': self.cell_type.value,
            'properties': self.properties,
            'layer_id': self.layer_id
        }
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'GridCell':
        """Deserialize from dictionary"""
        return cls(
            x=data['x'],
            y=data['y'],
            cell_type=GridCellType(data.get('cell_type', GridCellType.EMPTY.value)),
            properties=data.get('properties', {}),
            layer_id=data.get('layer_id', 'default')
        )

@dataclass
class GridLayer:
    """Grid layer containing cells"""
    layer_id: str
    name: str
    cells: Dict[Tuple[int, int], GridCell] = None
    visible: bool = True
    opacity: float = 1.0
    properties: Dict[str, Any] = None
    
    def __post_init__(self):
        if self.cells is None:
            self.cells = {}
        if self.properties is None:
            self.properties = {}
    
    def set_cell(self, x: int, y: int, cell_type: GridCellType, properties: Dict[str, Any] = None):
        """Set a cell in this layer"""
        cell = GridCell(x, y, cell_type, properties or {}, self.layer_id)
        self.cells[(x, y)] = cell
    
    def get_cell(self, x: int, y: int) -> Optional[GridCell]:
        """Get a cell from this layer"""
        return self.cells.get((x, y))
    
    def remove_cell(self, x: int, y: int) -> bool:
        """Remove a cell from this layer"""
        if (x, y) in self.cells:
            del self.cells[(x, y)]
            return True
        return False
    
    def get_bounds(self) -> Tuple[int, int, int, int]:
        """Get layer bounds (min_x, min_y, max_x, max_y)"""
        if not self.cells:
            return (0, 0, 0, 0)
        
        positions = list(self.cells.keys())
        min_x = min(pos[0] for pos in positions)
        max_x = max(pos[0] for pos in positions)
        min_y = min(pos[1] for pos in positions)
        max_y = max(pos[1] for pos in positions)
        
        return (min_x, min_y, max_x, max_y)
    
    def to_dict(self) -> Dict[str, Any]:
        """Serialize to dictionary"""
        return {
            'layer_id': self.layer_id,
            'name': self.name,
            'cells': [cell.to_dict() for cell in self.cells.values()],
            'visible': self.visible,
            'opacity': self.opacity,
            'properties': self.properties
        }
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'GridLayer':
        """Deserialize from dictionary"""
        layer = cls(
            layer_id=data['layer_id'],
            name=data['name'],
            visible=data.get('visible', True),
            opacity=data.get('opacity', 1.0),
            properties=data.get('properties', {})
        )
        
        # Load cells
        for cell_data in data.get('cells', []):
            cell = GridCell.from_dict(cell_data)
            layer.cells[(cell.x, cell.y)] = cell
        
        return layer

@dataclass
class GridConfiguration:
    """Grid configuration settings"""
    width: int
    height: int
    cell_size: int = 32
    projection: GridProjection = GridProjection.PLATFORMER
    origin_x: int = 0
    origin_y: int = 0
    properties: Dict[str, Any] = None
    
    def __post_init__(self):
        if self.properties is None:
            self.properties = {}
    
    def to_dict(self) -> Dict[str, Any]:
        """Serialize to dictionary"""
        return {
            'width': self.width,
            'height': self.height,
            'cell_size': self.cell_size,
            'projection': self.projection.value,
            'origin_x': self.origin_x,
            'origin_y': self.origin_y,
            'properties': self.properties
        }
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'GridConfiguration':
        """Deserialize from dictionary"""
        return cls(
            width=data['width'],
            height=data['height'],
            cell_size=data.get('cell_size', 32),
            projection=GridProjection(data.get('projection', GridProjection.PLATFORMER.value)),
            origin_x=data.get('origin_x', 0),
            origin_y=data.get('origin_y', 0),
            properties=data.get('properties', {})
        )

class UniversalGridLayerEditor:
    """Jerry's grid layer editing system (universal version)"""
    
    def __init__(self, workspace_path: str = "."):
        self.workspace_path = Path(workspace_path)
        self.grid_config: Optional[GridConfiguration] = None
        self.layers: Dict[str, GridLayer] = {}
        self.active_layer_id: Optional[str] = None
        
        # Create grid directories
        self.grids_dir = self.workspace_path / "grids"
        self.grids_dir.mkdir(exist_ok=True)
        
        self.templates_dir = self.grids_dir / "templates"
        self.templates_dir.mkdir(exist_ok=True)
        
        # Grid data files
        self.config_file = self.grids_dir / "grid_config.json"
        self.layers_file = self.grids_dir / "grid_layers.json"

    def log_info(self, message: str, emoji: str = EMOJI_INFO):
        """Log informational message with epic styling"""
        print(f"{Colors.OKCYAN}{emoji} [INFO]{Colors.ENDC} {message}")

    def log_success(self, message: str, emoji: str = EMOJI_SUCCESS):
        """Log success message with grid flair"""
        print(f"{Colors.OKGREEN}{emoji} [SUCCESS]{Colors.ENDC} {message}")

    def log_warning(self, message: str, emoji: str = EMOJI_WARNING):
        """Log warning message"""
        print(f"{Colors.WARNING}{emoji} [WARNING]{Colors.ENDC} {message}")

    def log_error(self, message: str, emoji: str = EMOJI_ERROR):
        """Log error message"""
        print(f"{Colors.FAIL}{emoji} [ERROR]{Colors.ENDC} {message}")

    def create_grid(self, width: int, height: int, cell_size: int = 32, 
                   projection: GridProjection = GridProjection.PLATFORMER) -> bool:
        """Create a new grid configuration"""
        try:
            self.grid_config = GridConfiguration(
                width=width,
                height=height,
                cell_size=cell_size,
                projection=projection
            )
            
            # Create default layer
            default_layer = GridLayer(
                layer_id="default",
                name="Default Layer"
            )
            self.layers = {"default": default_layer}
            self.active_layer_id = "default"
            
            self.save_grid()
            self.log_success(f"Created {width}x{height} grid with {projection.value} projection", EMOJI_GRID)
            return True
            
        except Exception as e:
            self.log_error(f"Failed to create grid: {e}")
            return False

    def add_layer(self, layer_id: str, name: str) -> bool:
        """Add a new layer to the grid"""
        try:
            if layer_id in self.layers:
                self.log_warning(f"Layer '{layer_id}' already exists")
                return False
            
            layer = GridLayer(layer_id=layer_id, name=name)
            self.layers[layer_id] = layer
            
            self.save_grid()
            self.log_success(f"Added layer: {name} ({layer_id})", EMOJI_LAYER)
            return True
            
        except Exception as e:
            self.log_error(f"Failed to add layer: {e}")
            return False

    def set_active_layer(self, layer_id: str) -> bool:
        """Set the active layer for editing"""
        if layer_id not in self.layers:
            self.log_error(f"Layer '{layer_id}' not found")
            return False
        
        self.active_layer_id = layer_id
        self.log_info(f"Active layer: {self.layers[layer_id].name}")
        return True

    def paint_cell(self, x: int, y: int, cell_type: GridCellType, 
                  properties: Dict[str, Any] = None, layer_id: str = None) -> bool:
        """Paint a cell in the specified layer"""
        try:
            if not self.grid_config:
                self.log_error("No grid configuration. Create a grid first.")
                return False
            
            # Use specified layer or active layer
            target_layer_id = layer_id or self.active_layer_id
            if not target_layer_id or target_layer_id not in self.layers:
                self.log_error("No valid layer specified")
                return False
            
            # Check bounds
            if not (0 <= x < self.grid_config.width and 0 <= y < self.grid_config.height):
                self.log_warning(f"Position ({x}, {y}) is outside grid bounds")
                return False
            
            layer = self.layers[target_layer_id]
            layer.set_cell(x, y, cell_type, properties)
            
            return True
            
        except Exception as e:
            self.log_error(f"Failed to paint cell: {e}")
            return False

    def paint_rectangle(self, start_x: int, start_y: int, end_x: int, end_y: int,
                       cell_type: GridCellType, properties: Dict[str, Any] = None,
                       layer_id: str = None) -> int:
        """Paint a rectangle of cells"""
        try:
            painted_count = 0
            
            # Ensure proper bounds
            min_x, max_x = min(start_x, end_x), max(start_x, end_x)
            min_y, max_y = min(start_y, end_y), max(start_y, end_y)
            
            for y in range(min_y, max_y + 1):
                for x in range(min_x, max_x + 1):
                    if self.paint_cell(x, y, cell_type, properties, layer_id):
                        painted_count += 1
            
            self.log_success(f"Painted {painted_count} cells in rectangle", EMOJI_GRID)
            return painted_count
            
        except Exception as e:
            self.log_error(f"Failed to paint rectangle: {e}")
            return 0

    def flood_fill(self, start_x: int, start_y: int, cell_type: GridCellType,
                  properties: Dict[str, Any] = None, layer_id: str = None) -> int:
        """Flood fill starting from a position"""
        try:
            target_layer_id = layer_id or self.active_layer_id
            if not target_layer_id or target_layer_id not in self.layers:
                self.log_error("No valid layer specified")
                return 0
            
            layer = self.layers[target_layer_id]
            start_cell = layer.get_cell(start_x, start_y)
            target_type = start_cell.cell_type if start_cell else GridCellType.EMPTY
            
            if target_type == cell_type:
                return 0  # No change needed
            
            # Flood fill algorithm
            stack = [(start_x, start_y)]
            filled_count = 0
            visited = set()
            
            while stack:
                x, y = stack.pop()
                
                if (x, y) in visited:
                    continue
                
                if not (0 <= x < self.grid_config.width and 0 <= y < self.grid_config.height):
                    continue
                
                current_cell = layer.get_cell(x, y)
                current_type = current_cell.cell_type if current_cell else GridCellType.EMPTY
                
                if current_type != target_type:
                    continue
                
                visited.add((x, y))
                layer.set_cell(x, y, cell_type, properties)
                filled_count += 1
                
                # Add neighbors
                stack.extend([(x+1, y), (x-1, y), (x, y+1), (x, y-1)])
            
            self.log_success(f"Flood filled {filled_count} cells", EMOJI_GRID)
            return filled_count
            
        except Exception as e:
            self.log_error(f"Failed to flood fill: {e}")
            return 0

    def get_cell_info(self, x: int, y: int, layer_id: str = None) -> Optional[Dict[str, Any]]:
        """Get information about a cell"""
        try:
            target_layer_id = layer_id or self.active_layer_id
            if not target_layer_id or target_layer_id not in self.layers:
                return None
            
            layer = self.layers[target_layer_id]
            cell = layer.get_cell(x, y)
            
            if cell:
                return {
                    'position': (x, y),
                    'type': cell.cell_type.value,
                    'properties': cell.properties,
                    'layer': layer.name
                }
            else:
                return {
                    'position': (x, y),
                    'type': GridCellType.EMPTY.value,
                    'properties': {},
                    'layer': layer.name
                }
                
        except Exception as e:
            self.log_error(f"Failed to get cell info: {e}")
            return None

    def export_layer_as_csv(self, layer_id: str, output_path: str) -> bool:
        """Export layer as CSV format"""
        try:
            if layer_id not in self.layers:
                self.log_error(f"Layer '{layer_id}' not found")
                return False
            
            layer = self.layers[layer_id]
            min_x, min_y, max_x, max_y = layer.get_bounds()
            
            if not layer.cells:
                self.log_warning("Layer is empty")
                return False
            
            # Create CSV content
            csv_lines = []
            csv_lines.append("x,y,type,properties")
            
            for (x, y), cell in layer.cells.items():
                properties_str = json.dumps(cell.properties) if cell.properties else "{}"
                csv_lines.append(f"{x},{y},{cell.cell_type.value},\"{properties_str}\"")
            
            with open(output_path, 'w', encoding='utf-8') as f:
                f.write('\n'.join(csv_lines))
            
            self.log_success(f"Exported layer to CSV: {output_path}")
            return True
            
        except Exception as e:
            self.log_error(f"Failed to export layer as CSV: {e}")
            return False

    def import_layer_from_csv(self, csv_path: str, layer_id: str, layer_name: str) -> bool:
        """Import layer from CSV format"""
        try:
            if not Path(csv_path).exists():
                self.log_error(f"CSV file not found: {csv_path}")
                return False
            
            # Create or replace layer
            layer = GridLayer(layer_id=layer_id, name=layer_name)
            
            import csv as csv_module
            with open(csv_path, 'r', encoding='utf-8') as f:
                reader = csv_module.DictReader(f)
                
                for row in reader:
                    x = int(row['x'])
                    y = int(row['y'])
                    cell_type = GridCellType(row['type'])
                    
                    # Parse properties JSON
                    properties = {}
                    if row.get('properties') and row['properties'].strip():
                        try:
                            properties = json.loads(row['properties'])
                        except json.JSONDecodeError:
                            self.log_warning(f"Invalid properties JSON at ({x}, {y})")
                    
                    layer.set_cell(x, y, cell_type, properties)
            
            self.layers[layer_id] = layer
            self.log_success(f"Imported layer from CSV: {layer_name}")
            return True
            
        except Exception as e:
            self.log_error(f"Failed to import layer from CSV: {e}")
            return False

    def create_template(self, template_name: str, description: str = "") -> bool:
        """Create a template from current grid"""
        try:
            if not self.grid_config:
                self.log_error("No grid configuration to save as template")
                return False
            
            template_data = {
                'name': template_name,
                'description': description,
                'created_date': datetime.datetime.now().isoformat(),
                'config': self.grid_config.to_dict(),
                'layers': {layer_id: layer.to_dict() for layer_id, layer in self.layers.items()}
            }
            
            template_file = self.templates_dir / f"{template_name}.json"
            with open(template_file, 'w', encoding='utf-8') as f:
                json.dump(template_data, f, indent=2, ensure_ascii=False)
            
            self.log_success(f"Created template: {template_name}")
            return True
            
        except Exception as e:
            self.log_error(f"Failed to create template: {e}")
            return False

    def load_template(self, template_name: str) -> bool:
        """Load a template"""
        try:
            template_file = self.templates_dir / f"{template_name}.json"
            if not template_file.exists():
                self.log_error(f"Template not found: {template_name}")
                return False
            
            with open(template_file, 'r', encoding='utf-8') as f:
                template_data = json.load(f)
            
            # Load configuration
            self.grid_config = GridConfiguration.from_dict(template_data['config'])
            
            # Load layers
            self.layers = {}
            for layer_id, layer_data in template_data['layers'].items():
                self.layers[layer_id] = GridLayer.from_dict(layer_data)
            
            # Set first layer as active
            if self.layers:
                self.active_layer_id = list(self.layers.keys())[0]
            
            self.log_success(f"Loaded template: {template_name}")
            return True
            
        except Exception as e:
            self.log_error(f"Failed to load template: {e}")
            return False

    def list_templates(self) -> List[str]:
        """List available templates"""
        try:
            templates = []
            for template_file in self.templates_dir.glob("*.json"):
                templates.append(template_file.stem)
            return sorted(templates)
        except Exception as e:
            self.log_error(f"Failed to list templates: {e}")
            return []

    def get_grid_stats(self) -> Dict[str, Any]:
        """Get grid statistics"""
        try:
            if not self.grid_config:
                return {'error': 'No grid configured'}
            
            stats = {
                'config': {
                    'width': self.grid_config.width,
                    'height': self.grid_config.height,
                    'cell_size': self.grid_config.cell_size,
                    'projection': self.grid_config.projection.value,
                    'total_cells': self.grid_config.width * self.grid_config.height
                },
                'layers': {},
                'totals': {
                    'layer_count': len(self.layers),
                    'total_painted_cells': 0,
                    'cell_type_counts': {}
                }
            }
            
            # Layer statistics
            for layer_id, layer in self.layers.items():
                layer_stats = {
                    'name': layer.name,
                    'cell_count': len(layer.cells),
                    'visible': layer.visible,
                    'opacity': layer.opacity,
                    'bounds': layer.get_bounds(),
                    'cell_types': {}
                }
                
                # Count cell types in this layer
                for cell in layer.cells.values():
                    cell_type = cell.cell_type.value
                    layer_stats['cell_types'][cell_type] = layer_stats['cell_types'].get(cell_type, 0) + 1
                    stats['totals']['cell_type_counts'][cell_type] = stats['totals']['cell_type_counts'].get(cell_type, 0) + 1
                
                stats['layers'][layer_id] = layer_stats
                stats['totals']['total_painted_cells'] += len(layer.cells)
            
            return stats
            
        except Exception as e:
            self.log_error(f"Failed to get grid stats: {e}")
            return {'error': str(e)}

    def save_grid(self) -> bool:
        """Save grid configuration and layers"""
        try:
            # Save configuration
            if self.grid_config:
                config_data = {
                    'version': '1.0',
                    'last_updated': datetime.datetime.now().isoformat(),
                    'config': self.grid_config.to_dict()
                }
                
                with open(self.config_file, 'w', encoding='utf-8') as f:
                    json.dump(config_data, f, indent=2, ensure_ascii=False)
            
            # Save layers
            layers_data = {
                'version': '1.0',
                'last_updated': datetime.datetime.now().isoformat(),
                'active_layer_id': self.active_layer_id,
                'layers': {layer_id: layer.to_dict() for layer_id, layer in self.layers.items()}
            }
            
            with open(self.layers_file, 'w', encoding='utf-8') as f:
                json.dump(layers_data, f, indent=2, ensure_ascii=False)
            
            return True
            
        except Exception as e:
            self.log_error(f"Failed to save grid: {e}")
            return False

    def load_grid(self) -> bool:
        """Load grid configuration and layers"""
        try:
            # Load configuration
            if self.config_file.exists():
                with open(self.config_file, 'r', encoding='utf-8') as f:
                    config_data = json.load(f)
                self.grid_config = GridConfiguration.from_dict(config_data['config'])
            
            # Load layers
            if self.layers_file.exists():
                with open(self.layers_file, 'r', encoding='utf-8') as f:
                    layers_data = json.load(f)
                
                self.active_layer_id = layers_data.get('active_layer_id')
                self.layers = {}
                
                for layer_id, layer_data in layers_data.get('layers', {}).items():
                    self.layers[layer_id] = GridLayer.from_dict(layer_data)
            
            return True
            
        except Exception as e:
            self.log_warning(f"Could not load existing grid: {e}")
            return False


def main():
    """Main grid layer editor interface"""
    parser = argparse.ArgumentParser(
        description=f"{EMOJI_GRID} Universal Grid Layer Editor",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python3 grid_editor.py --create-grid 32 24 --projection platformer
  python3 grid_editor.py --add-layer "collision" "Collision Layer"
  python3 grid_editor.py --paint 10 5 --type solid --layer collision
  python3 grid_editor.py --paint-rect 0 0 31 2 --type platform
  python3 grid_editor.py --flood-fill 15 10 --type solid
  python3 grid_editor.py --export-csv collision collision_data.csv
  python3 grid_editor.py --create-template "my_level" "Level template"
        """
    )
    
    parser.add_argument('--workspace', default='.', help='Workspace directory path')
    
    # Grid creation
    parser.add_argument('--create-grid', nargs=2, type=int, metavar=('WIDTH', 'HEIGHT'),
                       help='Create new grid with specified dimensions')
    parser.add_argument('--cell-size', type=int, default=32, help='Grid cell size in pixels')
    parser.add_argument('--projection', choices=[p.value for p in GridProjection],
                       default=GridProjection.PLATFORMER.value, help='Grid projection type')
    
    # Layer management
    parser.add_argument('--add-layer', nargs=2, metavar=('ID', 'NAME'),
                       help='Add new layer with ID and name')
    parser.add_argument('--set-active', help='Set active layer by ID')
    parser.add_argument('--layer', help='Target layer for operations')
    
    # Cell painting
    parser.add_argument('--paint', nargs=2, type=int, metavar=('X', 'Y'),
                       help='Paint single cell at position')
    parser.add_argument('--paint-rect', nargs=4, type=int, metavar=('X1', 'Y1', 'X2', 'Y2'),
                       help='Paint rectangle of cells')
    parser.add_argument('--flood-fill', nargs=2, type=int, metavar=('X', 'Y'),
                       help='Flood fill starting from position')
    parser.add_argument('--type', choices=[t.value for t in GridCellType],
                       default=GridCellType.SOLID.value, help='Cell type to paint')
    parser.add_argument('--properties', help='Cell properties as JSON string')
    
    # Data operations
    parser.add_argument('--export-csv', nargs=2, metavar=('LAYER_ID', 'OUTPUT'),
                       help='Export layer to CSV file')
    parser.add_argument('--import-csv', nargs=3, metavar=('CSV_FILE', 'LAYER_ID', 'LAYER_NAME'),
                       help='Import layer from CSV file')
    
    # Templates
    parser.add_argument('--create-template', nargs=2, metavar=('NAME', 'DESCRIPTION'),
                       help='Create template from current grid')
    parser.add_argument('--load-template', help='Load template by name')
    parser.add_argument('--list-templates', action='store_true', help='List available templates')
    
    # Information
    parser.add_argument('--cell-info', nargs=2, type=int, metavar=('X', 'Y'),
                       help='Get information about cell')
    parser.add_argument('--grid-stats', action='store_true', help='Show grid statistics')
    
    args = parser.parse_args()
    
    try:
        # Create editor instance
        editor = UniversalGridLayerEditor(workspace_path=args.workspace)
        editor.load_grid()
        
        # Handle grid creation
        if args.create_grid:
            width, height = args.create_grid
            projection = GridProjection(args.projection)
            editor.create_grid(width, height, args.cell_size, projection)
        
        # Handle layer operations
        elif args.add_layer:
            layer_id, layer_name = args.add_layer
            editor.add_layer(layer_id, layer_name)
        
        elif args.set_active:
            editor.set_active_layer(args.set_active)
        
        # Handle painting operations
        elif args.paint:
            x, y = args.paint
            cell_type = GridCellType(args.type)
            properties = json.loads(args.properties) if args.properties else None
            if editor.paint_cell(x, y, cell_type, properties, args.layer):
                editor.save_grid()
        
        elif args.paint_rect:
            x1, y1, x2, y2 = args.paint_rect
            cell_type = GridCellType(args.type)
            properties = json.loads(args.properties) if args.properties else None
            count = editor.paint_rectangle(x1, y1, x2, y2, cell_type, properties, args.layer)
            if count > 0:
                editor.save_grid()
        
        elif args.flood_fill:
            x, y = args.flood_fill
            cell_type = GridCellType(args.type)
            properties = json.loads(args.properties) if args.properties else None
            count = editor.flood_fill(x, y, cell_type, properties, args.layer)
            if count > 0:
                editor.save_grid()
        
        # Handle data operations
        elif args.export_csv:
            layer_id, output_path = args.export_csv
            editor.export_layer_as_csv(layer_id, output_path)
        
        elif args.import_csv:
            csv_file, layer_id, layer_name = args.import_csv
            if editor.import_layer_from_csv(csv_file, layer_id, layer_name):
                editor.save_grid()
        
        # Handle templates
        elif args.create_template:
            name, description = args.create_template
            editor.create_template(name, description)
        
        elif args.load_template:
            if editor.load_template(args.load_template):
                editor.save_grid()
        
        elif args.list_templates:
            templates = editor.list_templates()
            if templates:
                print(f"\n{Colors.HEADER}📚 Available Templates{Colors.ENDC}")
                for template in templates:
                    print(f"  - {template}")
            else:
                editor.log_info("No templates found")
        
        # Handle information
        elif args.cell_info:
            x, y = args.cell_info
            info = editor.get_cell_info(x, y, args.layer)
            if info:
                print(f"\n{Colors.HEADER}📐 Cell Information{Colors.ENDC}")
                print(f"Position: {info['position']}")
                print(f"Type: {info['type']}")
                print(f"Layer: {info['layer']}")
                if info['properties']:
                    print(f"Properties: {json.dumps(info['properties'], indent=2)}")
        
        elif args.grid_stats:
            stats = editor.get_grid_stats()
            if 'error' not in stats:
                print(f"\n{Colors.HEADER}📊 Grid Statistics{Colors.ENDC}")
                config = stats['config']
                print(f"Grid Size: {config['width']}x{config['height']} ({config['total_cells']} cells)")
                print(f"Cell Size: {config['cell_size']}px")
                print(f"Projection: {config['projection']}")
                print(f"Layers: {stats['totals']['layer_count']}")
                print(f"Painted Cells: {stats['totals']['total_painted_cells']}")
                
                if stats['totals']['cell_type_counts']:
                    print("Cell Types:")
                    for cell_type, count in stats['totals']['cell_type_counts'].items():
                        print(f"  {cell_type}: {count}")
            else:
                editor.log_error(stats['error'])
        
        else:
            # No action specified, show status
            if editor.grid_config:
                editor.log_info(f"Grid loaded: {editor.grid_config.width}x{editor.grid_config.height}")
                editor.log_info(f"Layers: {len(editor.layers)}")
                if editor.active_layer_id:
                    editor.log_info(f"Active layer: {editor.layers[editor.active_layer_id].name}")
            else:
                editor.log_info("No grid loaded. Use --create-grid to start.")
            
            editor.log_info("Use --help to see available commands")
        
    except KeyboardInterrupt:
        print(f"\n{Colors.WARNING}{EMOJI_WARNING} Grid editor interrupted{Colors.ENDC}")
        sys.exit(1)
    except Exception as e:
        print(f"{Colors.FAIL}{EMOJI_ERROR} Grid editor error: {e}{Colors.ENDC}")
        sys.exit(1)


if __name__ == "__main__":
    main()
