#!/usr/bin/env python3
"""
🧙‍♂️ Warbler AI Service Helper for TLDA Terminus
==================================================
Enhanced AI service management with Terminus integration
"""

import subprocess
import requests
import time
import json
import sys
from datetime import datetime

class TerminusAIHelper:
    def __init__(self):
        self.endpoints = [
            ("Docker Model Runner", "http://localhost:8080"),
            ("Ollama Service", "http://localhost:11434"),
            ("Alternative AI", "http://localhost:9998")
        ]
        
    def print_banner(self):
        print("🧙‍♂️ Warbler AI Service Manager for TLDA Terminus")
        print("=" * 50)
        print(f"🕐 Session started: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        print()
        
    def test_connection(self, endpoint):
        """Test connection to a specific endpoint"""
        try:
            response = requests.get(endpoint, timeout=5)
            if response.status_code == 200:
                return True, f"✅ Connected - Status {response.status_code}"
            else:
                return False, f"⚠️ Service responded but not ready - Status {response.status_code}"
        except requests.exceptions.ConnectionError:
            return False, "🔴 Connection refused - Service not running"
        except requests.exceptions.Timeout:
            return False, "⏱️ Connection timeout - Service may be starting"
        except Exception as e:
            return False, f"❌ Error: {str(e)}"
    
    def check_all_connections(self):
        """Check all known AI service endpoints"""
        print("🔌 Testing AI Service Connections:")
        print()
        
        any_connected = False
        for name, endpoint in self.endpoints:
            print(f"🔍 Testing {name}: {endpoint}")
            connected, message = self.test_connection(endpoint)
            print(f"   {message}")
            if connected:
                any_connected = True
            print()
            
        return any_connected
    
    def start_gemma3(self):
        """Start Gemma3 using Docker Model Runner"""
        print("🚀 Starting Gemma3 AI Service...")
        try:
            result = subprocess.run(
                ["docker", "model", "run", "gemma3"], 
                capture_output=True, 
                text=True,
                timeout=30
            )
            
            if result.returncode == 0:
                print("✅ Gemma3 start command executed successfully")
                print("⏱️ Waiting for service to initialize...")
                time.sleep(5)
                
                # Test connection after starting
                connected, message = self.test_connection("http://localhost:8080")
                if connected:
                    print("🎉 Gemma3 is now running and ready!")
                    return True
                else:
                    print("⚠️ Gemma3 started but may still be initializing...")
                    return False
            else:
                print(f"❌ Failed to start Gemma3: {result.stderr}")
                return False
                
        except subprocess.TimeoutExpired:
            print("⏱️ Start command timed out - Gemma3 may still be starting")
            return False
        except Exception as e:
            print(f"❌ Error starting Gemma3: {e}")
            return False
    
    def check_docker_status(self):
        """Check Docker and model status"""
        print("🐳 Checking Docker Status:")
        try:
            # Check if Docker is running
            result = subprocess.run(["docker", "ps"], capture_output=True, text=True)
            if result.returncode == 0:
                print("✅ Docker is running")
                
                # Check Docker Model Runner status
                result = subprocess.run(["docker", "model", "ps"], capture_output=True, text=True)
                if result.returncode == 0:
                    print("🤖 Docker Model Runner status:")
                    print(result.stdout or "   No models currently running")
                else:
                    print("⚠️ Docker Model Runner not available")
            else:
                print("❌ Docker is not running or not accessible")
                
        except FileNotFoundError:
            print("❌ Docker command not found - Docker may not be installed")
        except Exception as e:
            print(f"❌ Error checking Docker: {e}")
        print()
    
    def run_full_diagnostic(self):
        """Run complete diagnostic for Terminus integration"""
        self.print_banner()
        self.check_docker_status()
        any_connected = self.check_all_connections()
        
        if not any_connected:
            print("💡 Terminus Integration Tips:")
            print("   1. Use 'docker model run gemma3' in Terminus to start AI service")
            print("   2. Check 'docker model ps' to see running models")
            print("   3. Verify Docker is running with 'docker ps'")
            print("   4. Use Terminus terminal for better AI service management")
            print()
            
            # Offer to start Gemma3
            if len(sys.argv) > 1 and "--auto-start" in sys.argv:
                print("🚀 Auto-starting Gemma3...")
                self.start_gemma3()
        else:
            print("🎉 AI services are running! Warbler is ready for action!")
        
        return any_connected

def main():
    helper = TerminusAIHelper()
    
    if len(sys.argv) > 1:
        if sys.argv[1] == "--quick-test":
            # Quick connection test for Unity integration
            endpoint = sys.argv[2] if len(sys.argv) > 2 else "http://localhost:8080"
            connected, message = helper.test_connection(endpoint)
            if connected:
                print("✅ CONNECTION SUCCESS")
                return 0
            else:
                print(f"❌ CONNECTION FAILED: {message}")
                return 1
        elif sys.argv[1] == "--test-connection":
            # Unity-style connection test
            endpoint = sys.argv[2] if len(sys.argv) > 2 else "http://localhost:8080"
            connected, message = helper.test_connection(endpoint)
            if connected:
                print("✅ CONNECTION SUCCESS")
                return 0
            else:
                print(f"❌ CONNECTION FAILED: {message}")
                return 1
        elif sys.argv[1] == "--start-gemma3":
            return 0 if helper.start_gemma3() else 1
    else:
        # Full diagnostic
        helper.run_full_diagnostic()
        return 0

if __name__ == "__main__":
    sys.exit(main())
