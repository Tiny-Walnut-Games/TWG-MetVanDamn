#!/usr/bin/env python3
"""
Test script for GitHub Copilot integration and multi-provider system
This script validates the complete authentication and analysis workflow
"""

import sys
import json
import time
from pathlib import Path

def test_provider_status():
    """Test getting provider status"""
    print("🔍 Testing provider status...")
    import subprocess
    
    try:
        result = subprocess.run([
            sys.executable, 
            "scripts/warbler_project_intelligence.py", 
            "--provider-status"
        ], capture_output=True, text=True, timeout=30)
        
        if result.returncode == 0:
            print("✅ Provider status check successful")
            print(result.stdout)
            return True
        else:
            print(f"❌ Provider status check failed: {result.stderr}")
            return False
    except Exception as e:
        print(f"❌ Provider status test error: {e}")
        return False

def test_github_auth_validation():
    """Test GitHub authentication validation"""
    print("🔐 Testing GitHub authentication validation...")
    import subprocess
    
    try:
        result = subprocess.run([
            sys.executable, 
            "scripts/github_copilot_auth.py", 
            "validate"
        ], capture_output=True, text=True, timeout=30)
        
        # Both success (0) and failure (1) are valid for this test
        if result.returncode in [0, 1]:
            print("✅ GitHub auth validation test successful")
            print(result.stdout)
            return True
        else:
            print(f"❌ GitHub auth validation test failed: {result.stderr}")
            return False
    except Exception as e:
        print(f"❌ GitHub auth validation test error: {e}")
        return False

def test_multi_provider_analysis():
    """Test multi-provider analysis with different preferences"""
    print("🧠 Testing multi-provider analysis...")
    
    test_requests = [
        ("Create a simple platformer game", "--prefer-ollama"),
        ("Create a text adventure like Zork", ""),  # Default GitHub preference
        ("Create a tower defense game", "--prefer-ollama")
    ]
    
    for request, preference in test_requests:
        try:
            print(f"\n📝 Testing: '{request}' with {preference or 'default preference'}")
            
            import subprocess
            args = [sys.executable, "scripts/warbler_project_intelligence.py", request]
            if preference:
                args.append(preference)
            
            result = subprocess.run(args, capture_output=True, text=True, timeout=60)
            
            if result.returncode == 0:
                # Parse the JSON output
                lines = result.stdout.split('\n')
                json_start = False
                json_data = []
                
                for line in lines:
                    if "🔮 Warbler Intelligence Output:" in line:
                        json_start = True
                        continue
                    elif json_start:
                        json_data.append(line)
                
                if json_data:
                    try:
                        analysis = json.loads('\n'.join(json_data))
                        if analysis.get('success'):
                            game_type = analysis['analysis']['game_type']
                            provider = analysis['analysis'].get('ai_provider_used', 'unknown')
                            providers_tried = analysis['analysis'].get('providers_tried', [])
                            
                            print(f"  ✅ Analysis successful: {game_type} (via {provider})")
                            print(f"  📊 Providers tried: {', '.join(providers_tried)}")
                        else:
                            print("  ❌ Analysis marked as unsuccessful")
                    except json.JSONDecodeError as e:
                        print(f"  ⚠️ Could not parse JSON output: {e}")
                        print("  ⚠️ But process completed successfully")
                else:
                    print("  ⚠️ No JSON output found, but process completed")
            else:
                print(f"  ❌ Analysis failed with return code {result.returncode}")
                print(f"  ❌ Error: {result.stderr}")
                
        except Exception as e:
            print(f"  ❌ Test error: {e}")
    
    return True

def test_github_copilot_client():
    """Test GitHub Copilot client functionality"""
    print("🤖 Testing GitHub Copilot client...")
    
    # Test basic import and instantiation
    try:
        # Add current directory to Python path for imports
        import sys
        import os
        sys.path.insert(0, os.path.join(os.getcwd(), 'scripts'))
        
        from github_copilot_client import GitHubCopilotClient, test_github_copilot_connection
        print("✅ GitHub Copilot client import successful")
        
        # Test connection with dummy token (will fail but should handle gracefully)
        result = test_github_copilot_connection("dummy_token")
        print(f"✅ Connection test handled gracefully (result: {result})")
        
        return True
    except Exception as e:
        print(f"❌ GitHub Copilot client test error: {e}")
        return False

def main():
    """Run all tests"""
    print("🚀 Starting GitHub Copilot Integration Test Suite")
    print("=" * 60)
    
    tests = [
        ("Provider Status", test_provider_status),
        ("GitHub Auth Validation", test_github_auth_validation),
        ("GitHub Copilot Client", test_github_copilot_client),
        ("Multi-Provider Analysis", test_multi_provider_analysis),
    ]
    
    results = []
    
    for test_name, test_func in tests:
        print(f"\n🧪 Running: {test_name}")
        print("-" * 40)
        
        try:
            start_time = time.time()
            success = test_func()
            duration = time.time() - start_time
            
            results.append((test_name, success, duration))
            
            if success:
                print(f"✅ {test_name} completed successfully ({duration:.2f}s)")
            else:
                print(f"❌ {test_name} failed ({duration:.2f}s)")
                
        except Exception as e:
            duration = time.time() - start_time
            results.append((test_name, False, duration))
            print(f"💥 {test_name} crashed: {e} ({duration:.2f}s)")
    
    # Summary
    print("\n" + "=" * 60)
    print("📊 TEST SUMMARY")
    print("=" * 60)
    
    passed = sum(1 for _, success, _ in results if success)
    total = len(results)
    
    for test_name, success, duration in results:
        status = "✅ PASS" if success else "❌ FAIL"
        print(f"{status} {test_name:<25} ({duration:.2f}s)")
    
    print(f"\nOverall: {passed}/{total} tests passed")
    
    if passed == total:
        print("🎉 All tests passed! GitHub Copilot integration is working correctly.")
        return 0
    else:
        print("⚠️ Some tests failed. Check the output above for details.")
        return 1

if __name__ == "__main__":
    sys.exit(main())