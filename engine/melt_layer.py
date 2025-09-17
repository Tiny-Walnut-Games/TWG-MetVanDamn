from __future__ import annotations
from typing import Dict, Any, List
import time, hashlib

class MeltLayer:
    """Melt Layer: retires clusters into molten glyphs.
    Append-only; never mutate an existing glyph in-place.
    """
    def __init__(self, magma_store: "MagmaStore", embed_fn=None):
        self.magma_store = magma_store
        self.embed_fn = embed_fn or (lambda frags: [0.0])

    def retire_cluster(self, cluster: Dict[str, Any]) -> Dict[str, Any]:
        fragments: List[Dict[str, Any]] = cluster.get("fragments", [])
        summary = self._summarize(fragments)
        glyph_id = self._glyph_id(summary)
        glyph = {
            "id": glyph_id,
            "source_ids": [f.get("id") for f in fragments if f.get("id")],
            "compressed_summary": summary,
            "embedding": self.embed_fn([f.get("text", "") for f in fragments]),
            "affect": {"awe": 0.1, "humor": 0.05, "tension": 0.05},
            "resolution_state": "retired",
            "heat_seed": 0.55,
            "provenance_hash": self._prov_hash(fragments),
            "created_epoch": int(time.time()),
        }
        self.magma_store.add_glyph(glyph)
        return glyph

    def _glyph_id(self, summary: str) -> str:
        return "mglyph_" + hashlib.sha256(summary.encode()).hexdigest()[:12]

    def _summarize(self, fragments: List[Dict[str, Any]]) -> str:
        # TODO: more advanced summarization
        texts = [f.get("text", "") for f in fragments]
        base = " | ".join(t[:60] for t in texts)[:180]
        return base or "(empty cluster)"

    def _prov_hash(self, fragments: List[Dict[str, Any]]) -> str:
        concat = "".join(f.get("text", "") for f in fragments)
        return "sha256:" + hashlib.sha256(concat.encode()).hexdigest()

class MagmaStore:
    def __init__(self):
        self.glyphs = []

    def add_glyph(self, glyph: Dict[str, Any]):
        self.glyphs.append(glyph)

    def select_hot(self, limit: int) -> List[Dict[str, Any]]:
        # TODO: sort by heat / recency
        return self.glyphs[-limit:]