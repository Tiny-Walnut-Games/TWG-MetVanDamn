#!/usr/bin/env python3
"""
TLDA AI Setup & Diagnosis Tool
One-click setup for GitHub Copilot and Ollama integration
"""

import os
import sys
import subprocess
import json
import requests
import time
from pathlib import Path
import shutil

class TLDAAISetup:
    def __init__(self):
        self.script_dir = Path(__file__).parent
        self.results = {
            "ollama": {"available": False, "running": False, "models": []},
            "github_copilot": {"configured": False, "token_valid": False},
            "system": {"python_ok": False, "internet_ok": False}
        }
    
    def run_full_diagnosis(self):
        """Run complete system diagnosis and setup"""
        print("🔧 TLDA AI Setup & Diagnosis Tool")
        print("=" * 50)
        
        # Step 1: System checks
        print("\n1️⃣ System Prerequisites")
        self.check_python()
        self.check_internet()
        
        # Step 2: Ollama setup
        print("\n2️⃣ Ollama Setup")
        self.setup_ollama()
        
        # Step 3: GitHub Copilot setup
        print("\n3️⃣ GitHub Copilot Setup")
        self.setup_github_copilot()
        
        # Step 4: Final validation
        print("\n4️⃣ Final Validation")
        self.validate_setup()
        
        # Step 5: Unity integration test
        print("\n5️⃣ Unity Integration Test")
        self.test_unity_integration()
        
        print("\n" + "=" * 50)
        self.print_summary()
    
    def check_python(self):
        """Check Python installation"""
        try:
            version = sys.version_info
            if version.major >= 3 and version.minor >= 7:
                print(f"✅ Python {version.major}.{version.minor}.{version.micro} - OK")
                self.results["system"]["python_ok"] = True
            else:
                print(f"❌ Python {version.major}.{version.minor} - Need 3.7+")
        except Exception as e:
            print(f"❌ Python check failed: {e}")
    
    def check_internet(self):
        """Check internet connectivity"""
        try:
            response = requests.get("https://httpbin.org/status/200", timeout=5)
            if response.status_code == 200:
                print("✅ Internet connectivity - OK")
                self.results["system"]["internet_ok"] = True
            else:
                print("❌ Internet connectivity - Failed")
        except Exception as e:
            print(f"❌ Internet check failed: {e}")
    
    def setup_ollama(self):
        """Download and setup Ollama if needed"""
        print("   🔍 Checking for Ollama...")
        
        # Check if Ollama is in PATH
        ollama_cmd = shutil.which("ollama")
        if ollama_cmd:
            print(f"   ✅ Ollama found at: {ollama_cmd}")
            self.results["ollama"]["available"] = True
        else:
            print("   📥 Ollama not found - downloading...")
            if not self.download_ollama():
                print("   ❌ Failed to download Ollama")
                return
        
        # Start Ollama server
        self.start_ollama()
        
        # Download a basic model
        self.ensure_model()
    
    def download_ollama(self):
        """Download Ollama for Windows"""
        try:
            import platform
            if platform.system() != "Windows":
                print("   ❌ Auto-download only supports Windows")
                print("   📖 Please visit: https://ollama.com/download")
                return False
            
            download_url = "https://ollama.com/download/windows"
            print(f"   📥 Downloading from: {download_url}")
            print("   ⚠️  This will open a browser to download Ollama")
            print("   💡 Please install it and restart this script")
            
            import webbrowser
            webbrowser.open(download_url)
            
            input("   ⏸️  Press Enter after installing Ollama...")
            
            # Re-check
            ollama_cmd = shutil.which("ollama")
            if ollama_cmd:
                print(f"   ✅ Ollama installed successfully!")
                self.results["ollama"]["available"] = True
                return True
            else:
                print("   ❌ Ollama still not found in PATH")
                return False
                
        except Exception as e:
            print(f"   ❌ Download failed: {e}")
            return False
    
    def start_ollama(self):
        """Start Ollama server"""
        try:
            print("   🚀 Starting Ollama server...")
            
            # Try to start Ollama in background
            if sys.platform == "win32":
                subprocess.Popen(["ollama", "serve"], 
                               creationflags=subprocess.CREATE_NEW_CONSOLE)
            else:
                subprocess.Popen(["ollama", "serve"])
            
            # Wait for startup
            time.sleep(3)
            
            # Test connection
            response = requests.get("http://localhost:11434/api/version", timeout=5)
            if response.status_code == 200:
                print("   ✅ Ollama server running")
                self.results["ollama"]["running"] = True
                return True
            else:
                print("   ⚠️  Ollama server may not be ready")
                
        except requests.exceptions.ConnectionError:
            print("   ⚠️  Ollama server not responding (may need manual start)")
        except FileNotFoundError:
            print("   ❌ 'ollama' command not found")
        except Exception as e:
            print(f"   ❌ Failed to start Ollama: {e}")
        
        return False
    
    def ensure_model(self):
        """Download a lightweight model for testing"""
        if not self.results["ollama"]["running"]:
            print("   ⚠️  Skipping model download - server not running")
            return
        
        try:
            print("   📥 Ensuring test model is available...")
            
            # Try to download a small model
            result = subprocess.run(
                ["ollama", "pull", "llama3.2:1b"], 
                capture_output=True, 
                text=True, 
                timeout=120
            )
            
            if result.returncode == 0:
                print("   ✅ Model llama3.2:1b ready")
                self.results["ollama"]["models"].append("llama3.2:1b")
            else:
                print(f"   ⚠️  Model download issue: {result.stderr}")
                
        except subprocess.TimeoutExpired:
            print("   ⚠️  Model download timeout (will continue in background)")
        except Exception as e:
            print(f"   ❌ Model download failed: {e}")
    
    def setup_github_copilot(self):
        """Setup GitHub Copilot authentication"""
        print("   🔍 Checking GitHub Copilot setup...")
        
        try:
            # Check if auth module exists
            auth_file = self.script_dir / "github_copilot_auth.py"
            if not auth_file.exists():
                print("   ❌ GitHub Copilot auth module not found")
                return
            
            # Try to import and test
            sys.path.insert(0, str(self.script_dir))
            from github_copilot_auth import get_valid_token, validate_token
            
            # Check for existing token
            token = get_valid_token()
            if token and validate_token(token):
                print("   ✅ GitHub Copilot token valid")
                self.results["github_copilot"]["configured"] = True
                self.results["github_copilot"]["token_valid"] = True
            else:
                print("   ⚠️  No valid GitHub Copilot token found")
                self.guide_copilot_setup()
                
        except ImportError as e:
            print(f"   ❌ GitHub Copilot module import failed: {e}")
        except Exception as e:
            print(f"   ❌ GitHub Copilot check failed: {e}")
    
    def guide_copilot_setup(self):
        """Guide user through Copilot setup"""
        print("   💡 GitHub Copilot Setup Guide:")
        print("   1. Ensure you have GitHub Copilot subscription")
        print("   2. Run: python github_copilot_auth.py")
        print("   3. Follow OAuth flow in browser")
        print("   4. Token will be stored securely")
        
        if input("   ❓ Run GitHub Copilot auth now? (y/n): ").lower() == 'y':
            try:
                subprocess.run([sys.executable, "github_copilot_auth.py"], 
                             cwd=self.script_dir)
                print("   ✅ GitHub Copilot auth completed")
            except Exception as e:
                print(f"   ❌ Auth script failed: {e}")
    
    def validate_setup(self):
        """Run final validation tests"""
        print("   🧪 Running validation tests...")
        
        # Test Ollama API
        if self.results["ollama"]["running"]:
            self.test_ollama_api()
        
        # Test GitHub Copilot
        if self.results["github_copilot"]["token_valid"]:
            self.test_github_copilot_api()
    
    def test_ollama_api(self):
        """Test Ollama API with simple request"""
        try:
            payload = {
                "model": "llama3.2:1b",
                "prompt": "Say hello",
                "stream": False
            }
            
            response = requests.post(
                "http://localhost:11434/api/generate",
                json=payload,
                timeout=30
            )
            
            if response.status_code == 200:
                data = response.json()
                if "response" in data:
                    print("   ✅ Ollama API test successful")
                    return True
            
            print("   ⚠️  Ollama API test failed")
            
        except Exception as e:
            print(f"   ❌ Ollama API test error: {e}")
        
        return False
    
    def test_github_copilot_api(self):
        """Test GitHub Copilot API"""
        try:
            sys.path.insert(0, str(self.script_dir))
            from github_copilot_client import test_github_copilot_connection
            from github_copilot_auth import get_valid_token
            
            token = get_valid_token()
            if token and test_github_copilot_connection(token):
                print("   ✅ GitHub Copilot API test successful")
                return True
            else:
                print("   ⚠️  GitHub Copilot API test failed")
                
        except Exception as e:
            print(f"   ❌ GitHub Copilot API test error: {e}")
        
        return False
    
    def test_unity_integration(self):
        """Test Unity integration by running bridge scripts"""
        print("   🎮 Testing Unity integration...")
        
        try:
            # Test the Warbler bridge
            bridge_file = self.script_dir / "warbler_gemma3_bridge.py"
            if bridge_file.exists():
                result = subprocess.run([
                    sys.executable, str(bridge_file)
                ], capture_output=True, text=True, timeout=30)
                
                if result.returncode == 0:
                    print("   ✅ Bridge script test successful")
                else:
                    print(f"   ⚠️  Bridge script issues: {result.stderr}")
            else:
                print("   ❌ Bridge script not found")
                
        except subprocess.TimeoutExpired:
            print("   ⚠️  Bridge test timeout (may be downloading models)")
        except Exception as e:
            print(f"   ❌ Unity integration test failed: {e}")
    
    def print_summary(self):
        """Print setup summary and next steps"""
        print("📋 SETUP SUMMARY:")
        
        # System status
        print(f"   Python: {'✅' if self.results['system']['python_ok'] else '❌'}")
        print(f"   Internet: {'✅' if self.results['system']['internet_ok'] else '❌'}")
        
        # Ollama status
        print(f"   Ollama Available: {'✅' if self.results['ollama']['available'] else '❌'}")
        print(f"   Ollama Running: {'✅' if self.results['ollama']['running'] else '❌'}")
        print(f"   Models: {len(self.results['ollama']['models'])} available")
        
        # GitHub Copilot status
        print(f"   GitHub Copilot: {'✅' if self.results['github_copilot']['token_valid'] else '❌'}")
        
        # Next steps
        print("\n🎯 NEXT STEPS:")
        
        if not self.results['ollama']['running']:
            print("   1. Manually start Ollama: 'ollama serve'")
            print("   2. Download model: 'ollama pull llama3.2:1b'")
        
        if not self.results['github_copilot']['token_valid']:
            print("   3. Setup GitHub Copilot: 'python github_copilot_auth.py'")
        
        if self.results['ollama']['running'] or self.results['github_copilot']['token_valid']:
            print("   4. ✅ At least one AI service is ready!")
            print("   5. Unity integration should now work")
        else:
            print("   ❌ No AI services ready - manual setup required")
        
        print("\n📖 For manual setup guide, see:")
        print("   - Ollama: https://ollama.com/download")
        print("   - GitHub Copilot: Requires subscription + OAuth setup")

if __name__ == "__main__":
    setup = TLDAAISetup()
    setup.run_full_diagnosis()
