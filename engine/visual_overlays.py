"""
Visual Overlays - Multimodal Expressive Layer v0.5

Generates visual overlays for semantic anchor relevance and cognitive state visualization.
Provides heatmap generation and export functionality.
"""

from typing import Dict, List, Any, Optional, Tuple
from dataclasses import dataclass
import json
import time
import math


@dataclass
class AnchorVisualization:
    """Visual representation of a semantic anchor."""
    anchor_id: str
    x: float                    # Normalized coordinates (0.0 to 1.0)
    y: float
    heat: float                 # Heat value (0.0 to 1.0)
    radius: float               # Visual radius
    label: str
    color_intensity: float      # Color intensity (0.0 to 1.0)
    relevance_score: float      # Relevance to current context
    cluster_id: Optional[str] = None


@dataclass 
class HeatmapData:
    """Complete heatmap visualization data."""
    timestamp: float
    anchors: List[AnchorVisualization]
    dimensions: Tuple[int, int]  # Width, height
    global_max_heat: float
    conflict_zones: List[Dict[str, Any]]
    metadata: Dict[str, Any]


class VisualOverlayGenerator:
    """
    Generates visual overlays for semantic anchor relevance and cognitive states.
    
    Creates heatmaps showing anchor activity, cluster formations, and conflict zones.
    """
    
    def __init__(self, semantic_anchor_graph=None):
        self.semantic_anchor_graph = semantic_anchor_graph
        self.layout_cache = {}
        self.visualization_history = []
        self.max_history = 50
        
        # Default visualization parameters
        self.default_params = {
            "canvas_width": 800,
            "canvas_height": 600,
            "min_radius": 10,
            "max_radius": 40,
            "heat_threshold": 0.1,
            "cluster_opacity": 0.3,
            "conflict_highlight": True
        }
    
    def generate_anchor_heatmap(self, anchors: List = None, 
                               layout_mode: str = "force_directed",
                               **params) -> HeatmapData:
        """Generate a heatmap visualization of semantic anchors."""
        
        # Merge parameters
        viz_params = {**self.default_params, **params}
        
        # Get anchors from graph if not provided
        if anchors is None and self.semantic_anchor_graph:
            anchors = list(self.semantic_anchor_graph.anchors.values())
        elif anchors is None:
            anchors = []
        
        # Generate layout
        anchor_positions = self._generate_layout(anchors, layout_mode, viz_params)
        
        # Create visualizations
        anchor_visualizations = []
        global_max_heat = max((a.heat for a in anchors), default=1.0)
        
        for anchor in anchors:
            if anchor.anchor_id in anchor_positions:
                x, y = anchor_positions[anchor.anchor_id]
                
                # Calculate visual properties
                normalized_heat = anchor.heat / global_max_heat if global_max_heat > 0 else 0
                radius = self._calculate_radius(anchor.heat, viz_params)
                color_intensity = min(normalized_heat * 1.2, 1.0)
                
                anchor_viz = AnchorVisualization(
                    anchor_id=anchor.anchor_id,
                    x=x, y=y,
                    heat=anchor.heat,
                    radius=radius,
                    label=anchor.concept_text[:20] + "..." if len(anchor.concept_text) > 20 else anchor.concept_text,
                    color_intensity=color_intensity,
                    relevance_score=self._calculate_relevance(anchor),
                    cluster_id=getattr(anchor, 'cluster_id', None)
                )
                anchor_visualizations.append(anchor_viz)
        
        # Detect conflict zones
        conflict_zones = self._detect_conflict_zones(anchor_visualizations)
        
        # Create heatmap data
        heatmap = HeatmapData(
            timestamp=time.time(),
            anchors=anchor_visualizations,
            dimensions=(viz_params["canvas_width"], viz_params["canvas_height"]),
            global_max_heat=global_max_heat,
            conflict_zones=conflict_zones,
            metadata={
                "layout_mode": layout_mode,
                "total_anchors": len(anchor_visualizations),
                "active_anchors": len([a for a in anchor_visualizations if a.heat > viz_params["heat_threshold"]]),
                "cluster_count": len(set(a.cluster_id for a in anchor_visualizations if a.cluster_id))
            }
        )
        
        # Store in history
        self.visualization_history.append(heatmap)
        if len(self.visualization_history) > self.max_history:
            self.visualization_history.pop(0)
        
        return heatmap
    
    def _generate_layout(self, anchors: List, layout_mode: str, params: Dict) -> Dict[str, Tuple[float, float]]:
        """Generate layout positions for anchors."""
        if not anchors:
            return {}
        
        # Simple cache key
        cache_key = f"{layout_mode}_{len(anchors)}_{params['canvas_width']}x{params['canvas_height']}"
        
        if layout_mode == "grid":
            return self._grid_layout(anchors, params)
        elif layout_mode == "circular":
            return self._circular_layout(anchors, params)
        elif layout_mode == "cluster_based":
            return self._cluster_layout(anchors, params)
        else:  # Default to force_directed
            return self._force_directed_layout(anchors, params)
    
    def _grid_layout(self, anchors: List, params: Dict) -> Dict[str, Tuple[float, float]]:
        """Simple grid layout."""
        positions = {}
        cols = math.ceil(math.sqrt(len(anchors)))
        rows = math.ceil(len(anchors) / cols)
        
        for i, anchor in enumerate(anchors):
            col = i % cols
            row = i // cols
            
            x = (col + 0.5) / cols
            y = (row + 0.5) / rows
            positions[anchor.anchor_id] = (x, y)
        
        return positions
    
    def _circular_layout(self, anchors: List, params: Dict) -> Dict[str, Tuple[float, float]]:
        """Circular layout based on heat values."""
        positions = {}
        center_x, center_y = 0.5, 0.5
        
        # Sort by heat (hottest in center)
        sorted_anchors = sorted(anchors, key=lambda a: a.heat, reverse=True)
        
        for i, anchor in enumerate(sorted_anchors):
            if i == 0:
                # Center position for hottest anchor
                x, y = center_x, center_y
            else:
                # Circular arrangement
                angle = (2 * math.pi * i) / len(anchors)
                radius = 0.3 + (0.2 * (i / len(anchors)))  # Increasing radius
                
                x = center_x + radius * math.cos(angle)
                y = center_y + radius * math.sin(angle)
                
                # Clamp to bounds
                x = max(0.05, min(0.95, x))
                y = max(0.05, min(0.95, y))
            
            positions[anchor.anchor_id] = (x, y)
        
        return positions
    
    def _cluster_layout(self, anchors: List, params: Dict) -> Dict[str, Tuple[float, float]]:
        """Layout based on semantic clusters."""
        positions = {}
        
        # Group by cluster
        clusters = {}
        unclustered = []
        
        for anchor in anchors:
            cluster_id = getattr(anchor, 'cluster_id', None)
            if cluster_id:
                if cluster_id not in clusters:
                    clusters[cluster_id] = []
                clusters[cluster_id].append(anchor)
            else:
                unclustered.append(anchor)
        
        # Position clusters
        cluster_count = len(clusters) + (1 if unclustered else 0)
        cluster_positions = self._generate_cluster_centers(cluster_count)
        
        cluster_index = 0
        
        # Position clustered anchors
        for cluster_id, cluster_anchors in clusters.items():
            center_x, center_y = cluster_positions[cluster_index]
            cluster_index += 1
            
            for i, anchor in enumerate(cluster_anchors):
                if len(cluster_anchors) == 1:
                    x, y = center_x, center_y
                else:
                    # Arrange around cluster center
                    angle = (2 * math.pi * i) / len(cluster_anchors)
                    radius = 0.08  # Small radius around cluster center
                    
                    x = center_x + radius * math.cos(angle)
                    y = center_y + radius * math.sin(angle)
                
                positions[anchor.anchor_id] = (x, y)
        
        # Position unclustered anchors
        if unclustered:
            center_x, center_y = cluster_positions[cluster_index]
            for i, anchor in enumerate(unclustered):
                if len(unclustered) == 1:
                    x, y = center_x, center_y
                else:
                    angle = (2 * math.pi * i) / len(unclustered)
                    radius = 0.1
                    
                    x = center_x + radius * math.cos(angle)
                    y = center_y + radius * math.sin(angle)
                
                positions[anchor.anchor_id] = (x, y)
        
        return positions
    
    def _force_directed_layout(self, anchors: List, params: Dict) -> Dict[str, Tuple[float, float]]:
        """Simple force-directed layout simulation."""
        # Start with random positions
        positions = {}
        for i, anchor in enumerate(anchors):
            x = 0.2 + (0.6 * (i / len(anchors)))  # Spread across width
            y = 0.3 + (0.4 * ((i * 7) % len(anchors)) / len(anchors))  # Some vertical variation
            positions[anchor.anchor_id] = (x, y)
        
        return positions
    
    def _generate_cluster_centers(self, count: int) -> List[Tuple[float, float]]:
        """Generate center positions for clusters."""
        centers = []
        
        if count == 1:
            centers.append((0.5, 0.5))
        elif count == 2:
            centers.extend([(0.3, 0.5), (0.7, 0.5)])
        elif count <= 4:
            centers.extend([(0.3, 0.3), (0.7, 0.3), (0.3, 0.7), (0.7, 0.7)])
        else:
            # Circular arrangement for more clusters
            for i in range(count):
                angle = (2 * math.pi * i) / count
                x = 0.5 + 0.3 * math.cos(angle)
                y = 0.5 + 0.3 * math.sin(angle)
                centers.append((x, y))
        
        return centers[:count]
    
    def _calculate_radius(self, heat: float, params: Dict) -> float:
        """Calculate visual radius based on heat value."""
        min_radius = params["min_radius"]
        max_radius = params["max_radius"]
        
        # Scale radius with heat (with minimum)
        normalized_heat = min(heat, 1.0)
        radius = min_radius + (max_radius - min_radius) * normalized_heat
        
        return radius
    
    def _calculate_relevance(self, anchor) -> float:
        """Calculate relevance score for anchor."""
        # Simple relevance based on recent activity and heat
        base_relevance = min(anchor.heat, 1.0)
        
        # Age factor (newer anchors are more relevant)
        age_days = anchor.calculate_age_days() if hasattr(anchor, 'calculate_age_days') else 1.0
        age_factor = max(0.1, 1.0 - (age_days / 30.0))  # Decay over 30 days
        
        relevance = base_relevance * age_factor
        return min(relevance, 1.0)
    
    def _detect_conflict_zones(self, anchor_visualizations: List[AnchorVisualization]) -> List[Dict[str, Any]]:
        """Detect potential conflict zones in the visualization."""
        conflict_zones = []
        
        # Simple conflict detection: anchors that are close but have high heat difference
        for i, anchor1 in enumerate(anchor_visualizations):
            for anchor2 in anchor_visualizations[i+1:]:
                distance = math.sqrt((anchor1.x - anchor2.x)**2 + (anchor1.y - anchor2.y)**2)
                heat_diff = abs(anchor1.heat - anchor2.heat)
                
                # If anchors are close but have very different heat levels, mark as potential conflict
                if distance < 0.2 and heat_diff > 0.5:
                    conflict_zones.append({
                        "anchor1_id": anchor1.anchor_id,
                        "anchor2_id": anchor2.anchor_id,
                        "center_x": (anchor1.x + anchor2.x) / 2,
                        "center_y": (anchor1.y + anchor2.y) / 2,
                        "intensity": heat_diff,
                        "type": "heat_disparity"
                    })
        
        return conflict_zones
    
    def export_heatmap_json(self, heatmap: HeatmapData) -> str:
        """Export heatmap data as JSON."""
        def serialize_anchor(anchor: AnchorVisualization) -> Dict:
            return {
                "anchor_id": anchor.anchor_id,
                "x": anchor.x,
                "y": anchor.y, 
                "heat": anchor.heat,
                "radius": anchor.radius,
                "label": anchor.label,
                "color_intensity": anchor.color_intensity,
                "relevance_score": anchor.relevance_score,
                "cluster_id": anchor.cluster_id
            }
        
        data = {
            "timestamp": heatmap.timestamp,
            "anchors": [serialize_anchor(a) for a in heatmap.anchors],
            "dimensions": {"width": heatmap.dimensions[0], "height": heatmap.dimensions[1]},
            "global_max_heat": heatmap.global_max_heat,
            "conflict_zones": heatmap.conflict_zones,
            "metadata": heatmap.metadata
        }
        
        return json.dumps(data, indent=2)
    
    def export_svg_heatmap(self, heatmap: HeatmapData) -> str:
        """Export heatmap as SVG for visualization."""
        width, height = heatmap.dimensions
        
        svg_parts = [
            f'<svg width="{width}" height="{height}" xmlns="http://www.w3.org/2000/svg">',
            '<defs>',
            '<radialGradient id="heatGradient" cx="50%" cy="50%" r="50%">',
            '<stop offset="0%" style="stop-color:red;stop-opacity:1" />',
            '<stop offset="100%" style="stop-color:yellow;stop-opacity:0.3" />',
            '</radialGradient>',
            '</defs>'
        ]
        
        # Draw anchors
        for anchor in heatmap.anchors:
            x = anchor.x * width
            y = anchor.y * height
            
            # Color based on heat
            if anchor.heat > 0.7:
                color = "red"
            elif anchor.heat > 0.4:
                color = "orange"
            else:
                color = "yellow"
            
            opacity = max(0.3, anchor.color_intensity)
            
            svg_parts.append(
                f'<circle cx="{x:.1f}" cy="{y:.1f}" r="{anchor.radius}" '
                f'fill="{color}" opacity="{opacity:.2f}" stroke="black" stroke-width="1"/>'
            )
            
            # Add label if there's space
            if anchor.radius > 15:
                svg_parts.append(
                    f'<text x="{x:.1f}" y="{y+4:.1f}" text-anchor="middle" '
                    f'font-family="Arial" font-size="10" fill="black">{anchor.label}</text>'
                )
        
        # Draw conflict zones
        for zone in heatmap.conflict_zones:
            x = zone["center_x"] * width
            y = zone["center_y"] * height
            intensity = zone["intensity"]
            
            svg_parts.append(
                f'<circle cx="{x:.1f}" cy="{y:.1f}" r="25" '
                f'fill="none" stroke="red" stroke-width="3" opacity="{intensity:.2f}" '
                f'stroke-dasharray="5,5"/>'
            )
        
        svg_parts.append('</svg>')
        
        return '\n'.join(svg_parts)
    
    def get_visualization_history(self, limit: int = 10) -> List[HeatmapData]:
        """Get recent visualization history."""
        return self.visualization_history[-limit:]