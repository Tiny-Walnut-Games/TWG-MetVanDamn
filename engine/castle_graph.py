from __future__ import annotations
from typing import List, Dict, Any
import time

class CastleGraph:
    """Castle Graph: infuses mist lines and retrieves top rooms."""
    def __init__(self):
        self.nodes = {}  # concept_id -> node_data
        self.edges = []  # list of edge dicts
        self.updated_epoch = 0

    def infuse(self, mist_lines: List[Dict[str, Any]]) -> Dict[str, Any]:
        """Infuse mist lines into castle nodes, updating heat."""
        start = time.time()
        infused_count = 0
        
        for mist in mist_lines:
            concept_id = self._extract_concept_id(mist)
            if concept_id:
                self._heat_node(concept_id, mist)
                infused_count += 1
        
        self.updated_epoch = int(time.time())
        return {
            "infused_count": infused_count,
            "elapsed_ms": (time.time() - start) * 1000,
            "total_nodes": len(self.nodes),
        }

    def get_top_rooms(self, limit: int = 5) -> List[Dict[str, Any]]:
        """Retrieve top castle rooms by heat."""
        # TODO: Sort by heat and recency
        sorted_nodes = sorted(
            self.nodes.items(),
            key=lambda x: x[1].get("heat", 0.0),
            reverse=True
        )
        
        return [
            {
                "concept_id": concept_id,
                "heat": node_data.get("heat", 0.0),
                "room_type": node_data.get("room_type", "chamber"),
                "last_visit": node_data.get("last_visit", 0),
            }
            for concept_id, node_data in sorted_nodes[:limit]
        ]

    def _extract_concept_id(self, mist: Dict[str, Any]) -> str:
        # TODO: Advanced concept extraction from proto_thought
        proto = mist.get("proto_thought", "")
        if not proto:
            return None
        
        # Simple extraction: first meaningful word
        words = proto.replace("[Proto]", "").strip().split()
        if words:
            return f"concept_{words[0].lower()}"
        return None

    def _heat_node(self, concept_id: str, mist: Dict[str, Any]):
        """Add heat to a castle node from mist infusion."""
        if concept_id not in self.nodes:
            self.nodes[concept_id] = {
                "heat": 0.0,
                "room_type": "chamber",
                "creation_epoch": int(time.time()),
                "visit_count": 0,
            }
        
        # TODO: Complex heat calculation based on mist properties
        heat_boost = mist.get("mythic_weight", 0.1) + 0.1
        self.nodes[concept_id]["heat"] += heat_boost
        self.nodes[concept_id]["visit_count"] += 1
        self.nodes[concept_id]["last_visit"] = int(time.time())