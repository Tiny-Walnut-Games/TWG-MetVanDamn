from __future__ import annotations
from typing import List, Dict, Any
import time

class EvaporationEngine:
    """Evaporation: converts molten glyphs into mist lines (proto-thoughts)."""
    def __init__(self, magma_store, cloud_store):
        self.magma_store = magma_store
        self.cloud_store = cloud_store

    def evaporate(self, limit: int = 5) -> List[Dict[str, Any]]:
        molten = self.magma_store.select_hot(limit * 2)
        mist_lines = []
        for glyph in molten[:limit]:
            mist = self._distill_mist(glyph)
            mist_lines.append(mist)
        
        self.cloud_store.add_mist_lines(mist_lines)
        humidity = self._calculate_humidity(mist_lines)
        self.cloud_store.update_humidity(humidity)
        
        return mist_lines

    def _distill_mist(self, glyph: Dict[str, Any]) -> Dict[str, Any]:
        # TODO: Advanced mist distillation with style bias
        summary = glyph.get("compressed_summary", "")
        affect = glyph.get("affect", {})
        
        mist_line = {
            "id": f"mist_{glyph['id'][7:]}",  # Remove mglyph_ prefix
            "source_glyph": glyph["id"],
            "proto_thought": self._generate_proto_thought(summary, affect),
            "evaporation_temp": 0.7,  # placeholder
            "mythic_weight": affect.get("awe", 0.0),
            "technical_clarity": 0.6,  # placeholder
            "created_epoch": int(time.time()),
        }
        return mist_line

    def _generate_proto_thought(self, summary: str, affect: Dict[str, Any]) -> str:
        # TODO: Replace with real language generation
        fragments = summary.split(" | ")[:2]
        base = " → ".join(fragments) if len(fragments) > 1 else summary[:50]
        return f"[Proto] {base}..."

    def _calculate_humidity(self, mist_lines: List[Dict[str, Any]]) -> float:
        # TODO: Complex humidity calculation based on mist density and affect
        if not mist_lines:
            return 0.0
        
        total_mythic = sum(m.get("mythic_weight", 0.0) for m in mist_lines)
        avg_mythic = total_mythic / len(mist_lines)
        return min(0.95, 0.3 + (avg_mythic * 0.5))  # Bounded humidity

class CloudStore:
    def __init__(self):
        self.mist_lines = []
        self.humidity_index = 0.0
        self.generation_mode = "balanced"

    def add_mist_lines(self, mist_lines: List[Dict[str, Any]]):
        self.mist_lines.extend(mist_lines)

    def update_humidity(self, humidity: float):
        self.humidity_index = humidity

    def get_active_mist(self, limit: int = 10) -> List[Dict[str, Any]]:
        # TODO: Filter by recency and activity
        return self.mist_lines[-limit:]