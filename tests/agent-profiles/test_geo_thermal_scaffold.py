#!/usr/bin/env python3
"""
Test suite for Cognitive Geo-Thermal Lore Engine Scaffold
Minimal smoke tests to validate basic functionality and structure mutations.

Usage:
    python3 tests/test_geo_thermal_scaffold.py
    
Note: This follows the direct execution pattern like other tests in the repository.
No pytest dependency required - uses stdlib only.
"""

import sys
import os
import tempfile
import json
from pathlib import Path

# Add parent directory to path for engine imports
sys.path.insert(0, str(Path(__file__).parent.parent))

def test_engine_imports():
    """Test that all engine modules import without error."""
    print("🧪 Testing engine module imports...")
    
    try:
        from engine import (
            GiantCompressor, MeltLayer, EvaporationEngine, CastleGraph,
            Selector, Governance, CycleTelemetry
        )
        from engine.giant_compressor import SedimentStore
        from engine.melt_layer import MagmaStore
        from engine.evaporation import CloudStore
        
        print("✅ All engine modules imported successfully")
        return True
    except ImportError as e:
        print(f"❌ Import failed: {e}")
        return False

def test_json_schemas():
    """Test that JSON schema stubs are valid JSON."""
    print("🧪 Testing JSON schema validity...")
    
    schema_files = [
        "schema/magma.json",
        "schema/sediment_layer.json", 
        "schema/cloud_state.json",
        "schema/memory_castle_graph.json"
    ]
    
    try:
        for schema_file in schema_files:
            with open(schema_file, 'r') as f:
                data = json.load(f)
                assert "version" in data, f"Missing version in {schema_file}"
                assert "_comment" in data, f"Missing comment in {schema_file}"
            print(f"   ✅ {schema_file}")
        
        print("✅ All JSON schemas are valid")
        return True
    except (json.JSONDecodeError, FileNotFoundError, AssertionError) as e:
        print(f"❌ Schema validation failed: {e}")
        return False

def test_cycle_execution():
    """Test that one complete cycle runs with dummy fragments."""
    print("🧪 Testing complete cycle execution...")
    
    try:
        from engine import (
            GiantCompressor, MeltLayer, EvaporationEngine, CastleGraph,
            Selector, Governance, CycleTelemetry
        )
        from engine.giant_compressor import SedimentStore
        from engine.melt_layer import MagmaStore
        from engine.evaporation import CloudStore
        
        # Initialize stores
        sediment_store = SedimentStore()
        magma_store = MagmaStore()
        cloud_store = CloudStore()
        castle_graph = CastleGraph()
        
        # Initialize engines
        giant = GiantCompressor(sediment_store)
        melt_layer = MeltLayer(magma_store)
        evaporator = EvaporationEngine(magma_store, cloud_store)
        selector = Selector(castle_graph, cloud_store)
        governance = Governance()
        
        # Create dummy fragments
        dummy_fragments = [
            {"id": "test_1", "text": "Test fragment for cycle validation"},
            {"id": "test_2", "text": "Another test fragment for processing"},
        ]
        
        # Execute cycle phases
        # 1. Stomp
        stomp_result = giant.stomp(dummy_fragments)
        assert stomp_result["clusters"] > 0, "No clusters created"
        assert len(sediment_store.strata) > 0, "No strata created"
        
        # 2. Retire
        initial_glyph_count = len(magma_store.glyphs)
        for stratum in sediment_store.strata:
            for cluster in stratum["clusters"]:
                melt_layer.retire_cluster(cluster)
        assert len(magma_store.glyphs) > initial_glyph_count, "No glyphs created"
        
        # 3. Evaporate  
        initial_mist_count = len(cloud_store.mist_lines)
        mist_lines = evaporator.evaporate(limit=2)
        assert len(mist_lines) > 0, "No mist lines created"
        assert len(cloud_store.mist_lines) > initial_mist_count, "Mist lines not stored"
        
        # 4. Infuse
        initial_node_count = len(castle_graph.nodes)
        infusion_result = castle_graph.infuse(mist_lines)
        assert infusion_result["infused_count"] > 0, "No mist lines infused"
        assert len(castle_graph.nodes) >= initial_node_count, "No nodes created/updated"
        
        # 5. Select
        prompt_scaffold = selector.assemble_prompt("test context")
        assert "voices" in prompt_scaffold, "No voices in prompt scaffold"
        assert "mist_context" in prompt_scaffold, "No mist context in scaffold"
        
        response = selector.respond(prompt_scaffold)
        assert "response_text" in response, "No response text generated"
        
        # 6. Governance
        telemetry = CycleTelemetry(cycle_id="test_cycle")
        cycle_data = {
            "cycle_report": telemetry.to_dict(),
            "molten_glyphs": magma_store.glyphs,
            "mist_count": len(mist_lines),
            "top_rooms": castle_graph.get_top_rooms(3)
        }
        
        governance_result = governance.score_cycle(cycle_data["cycle_report"])
        assert "score" in governance_result, "No governance score"
        assert 0.0 <= governance_result["score"] <= 1.0, "Invalid score range"
        
        print("✅ Complete cycle executed successfully")
        print(f"   📊 Created: {len(magma_store.glyphs)} glyphs, {len(mist_lines)} mist lines, {len(castle_graph.nodes)} nodes")
        return True
        
    except Exception as e:
        print(f"❌ Cycle execution failed: {e}")
        return False

def test_data_structure_mutations():
    """Test that structures mutate as expected during processing."""
    print("🧪 Testing data structure mutations...")
    
    try:
        from engine.giant_compressor import SedimentStore
        from engine.melt_layer import MagmaStore
        from engine.evaporation import CloudStore
        from engine.castle_graph import CastleGraph
        
        # Test SedimentStore mutations
        sediment = SedimentStore()
        initial_strata = len(sediment.strata)
        cluster = {"id": "test_cluster", "fragments": [], "size": 0}
        sediment.append_cluster(cluster)
        assert len(sediment.strata) == initial_strata + 1, "Strata not updated"
        
        # Test MagmaStore mutations
        magma = MagmaStore()
        initial_glyphs = len(magma.glyphs)
        test_glyph = {"id": "test_glyph", "data": "test"}
        magma.add_glyph(test_glyph)
        assert len(magma.glyphs) == initial_glyphs + 1, "Glyphs not updated"
        
        # Test CloudStore mutations
        cloud = CloudStore()
        initial_mist = len(cloud.mist_lines)
        test_mist = [{"id": "test_mist", "data": "test"}]
        cloud.add_mist_lines(test_mist)
        assert len(cloud.mist_lines) == initial_mist + 1, "Mist lines not updated"
        
        # Test CastleGraph mutations
        castle = CastleGraph()
        initial_nodes = len(castle.nodes)
        castle._heat_node("test_concept", {"mythic_weight": 0.5})
        assert len(castle.nodes) == initial_nodes + 1, "Castle nodes not updated"
        assert castle.nodes["test_concept"]["heat"] > 0, "Node heat not increased"
        
        print("✅ All data structure mutations working correctly")
        return True
        
    except Exception as e:
        print(f"❌ Data structure mutation test failed: {e}")
        return False

def test_run_cycle_script():
    """Test that run_cycle.py produces expected JSON output."""
    print("🧪 Testing run_cycle.py script execution...")
    
    try:
        import subprocess
        import json
        
        # Run the cycle script and capture output
        result = subprocess.run([
            "python3", "scripts/run_cycle.py"
        ], capture_output=True, text=True, timeout=30)
        
        if result.returncode != 0:
            print(f"❌ Script execution failed: {result.stderr}")
            return False
        
        # Find the JSON output in stdout
        lines = result.stdout.split('\n')
        json_start = None
        for i, line in enumerate(lines):
            if line.strip() == '{':
                json_start = i
                break
        
        if json_start is None:
            print("❌ No JSON output found in script")
            return False
        
        # Extract and parse JSON
        json_lines = lines[json_start:]
        json_text = '\n'.join(json_lines).strip()
        
        # Parse the JSON
        output = json.loads(json_text)
        
        # Validate required keys
        required_keys = ["cycle_report", "molten_glyphs", "mist_count", "top_rooms"]
        for key in required_keys:
            assert key in output, f"Missing required key: {key}"
        
        # Validate data types
        assert isinstance(output["cycle_report"], dict), "cycle_report not a dict"
        assert isinstance(output["molten_glyphs"], list), "molten_glyphs not a list"
        assert isinstance(output["mist_count"], int), "mist_count not an int"
        assert isinstance(output["top_rooms"], list), "top_rooms not a list"
        
        print("✅ run_cycle.py produces valid JSON with required keys")
        print(f"   📊 Output: {output['mist_count']} mist lines, {len(output['molten_glyphs'])} glyphs, {len(output['top_rooms'])} rooms")
        return True
        
    except Exception as e:
        print(f"❌ Script execution test failed: {e}")
        return False

def run_all_tests():
    """Run all geo-thermal scaffold tests."""
    print("🔥 Running Cognitive Geo-Thermal Lore Engine Scaffold Tests")
    print("=" * 65)
    
    tests = [
        test_engine_imports,
        test_json_schemas,
        test_cycle_execution,
        test_data_structure_mutations,
        test_run_cycle_script,
    ]
    
    passed = 0
    failed = 0
    
    for test_func in tests:
        try:
            if test_func():
                passed += 1
            else:
                failed += 1
        except Exception as e:
            print(f"❌ Test {test_func.__name__} failed with exception: {e}")
            failed += 1
        print()
    
    print("=" * 65)
    print(f"📊 Test Results: {passed} passed, {failed} failed")
    
    if failed == 0:
        print("✅ All tests passed! Geo-Thermal Lore Engine scaffold is ready.")
        return True
    else:
        print("❌ Some tests failed. Review output above.")
        return False

if __name__ == "__main__":
    success = run_all_tests()
    sys.exit(0 if success else 1)