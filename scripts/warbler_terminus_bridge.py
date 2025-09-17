#!/usr/bin/env python3
"""
🧙‍♂️ Warbler Terminus Bridge
Enhanced AI service management with Terminus console integration
Provides progress feedback for Ollama and GitHub operations
"""

import sys
import subprocess
import json
import time
from pathlib import Path
from typing import Optional


class WarblerTerminusBridge:
    """Bridge between Warbler AI services and Terminus console"""
    
    def __init__(self):
        self.script_dir = Path(__file__).parent
        
    def start_ollama_with_feedback(self, model: Optional[str] = None) -> bool:
        """Start Ollama with progress feedback to Terminus"""
        print("🔥 Terminus-Enhanced Ollama Startup")
        print("=" * 50)
        
        try:
            # Import and use the enhanced ollama manager
            sys.path.insert(0, str(self.script_dir))
            from ollama_manager import OllamaManager
            
            manager = OllamaManager()
            
            # Check if Ollama binary exists
            if not manager.ollama_path:
                print("📥 PHASE 1: Downloading Ollama Binary")
                print("⏳ This may take a few minutes depending on your connection...")
                print("-" * 40)
                
                manager.ollama_path = manager._download_ollama()
                
                print("\n✅ PHASE 1 COMPLETE: Ollama binary ready")
                print("-" * 40)
            else:
                print("✅ PHASE 1 SKIP: Ollama binary already available")
                print("-" * 40)
            
            # Start Ollama service
            print("\n🚀 PHASE 2: Starting Ollama Service")
            print("⏳ Initializing AI service...")
            print("-" * 40)
            
            success = manager.start(model)
            
            if success:
                print("\n✅ PHASE 2 COMPLETE: Ollama service running")
                print("-" * 40)
                
                # Optionally download a model
                if model:
                    print(f"\n📥 PHASE 3: Ensuring Model '{model}' is Available")
                    print("⏳ This may take several minutes for large models...")
                    print("-" * 40)
                    
                    model_success = manager.ensure_model(model)
                    if model_success:
                        print(f"\n✅ PHASE 3 COMPLETE: Model '{model}' ready")
                        print("-" * 40)
                    else:
                        print(f"\n⚠️ PHASE 3 WARNING: Model '{model}' download failed")
                        print("The service is running but the specific model is not available")
                        print("-" * 40)
                
                print("\n🎉 OLLAMA STARTUP SUCCESSFUL!")
                print("🧙‍♂️ Warbler AI is now ready for magical code generation!")
                return True
            else:
                print("\n❌ PHASE 2 FAILED: Could not start Ollama service")
                print("Check the error messages above for troubleshooting")
                return False
                
        except Exception as e:
            print(f"\n❌ STARTUP FAILED: {e}")
            print("Please check your Python environment and try again")
            return False
    
    def authenticate_github_with_feedback(self) -> bool:
        """Authenticate GitHub with progress feedback to Terminus"""
        print("🔐 Terminus-Enhanced GitHub Authentication")
        print("=" * 50)
        
        try:
            # Import and use the enhanced GitHub auth
            sys.path.insert(0, str(self.script_dir))
            from github_copilot_auth import get_stored_token, validate_token, GitHubOAuthHandler, store_token
            
            print("🔍 PHASE 1: Checking Existing Authentication")
            print("-" * 40)
            
            stored_token = get_stored_token()
            if stored_token and validate_token(stored_token):
                print("✅ PHASE 1 COMPLETE: Valid GitHub token found")
                print("🔐 You are already authenticated with GitHub Copilot!")
                return True
            
            print("❌ PHASE 1 RESULT: No valid token found")
            print("🔄 Starting new authentication process...")
            print("-" * 40)
            
            print("\n🌐 PHASE 2: Browser Authentication")
            print("⏳ Opening browser for GitHub OAuth...")
            print("👆 Please complete authentication in your browser")
            print("-" * 40)
            
            oauth_handler = GitHubOAuthHandler()
            new_token = oauth_handler.authenticate(timeout=180)  # 3 minute timeout
            
            if new_token:
                print("\n✅ PHASE 2 COMPLETE: Authentication successful")
                print("-" * 40)
                
                print("\n💾 PHASE 3: Storing Authentication Token")
                print("🔒 Securely saving token for future use...")
                print("-" * 40)
                
                if store_token(new_token):
                    print("✅ PHASE 3 COMPLETE: Token stored securely")
                    print("-" * 40)
                else:
                    print("⚠️ PHASE 3 WARNING: Token storage failed")
                    print("Authentication successful but token not saved")
                    print("-" * 40)
                
                print("\n🎉 GITHUB AUTHENTICATION SUCCESSFUL!")
                print("🔐 Warbler can now access GitHub Copilot AI!")
                return True
            else:
                print("\n❌ PHASE 2 FAILED: Authentication unsuccessful")
                print("Please try again or check your GitHub connection")
                return False
                
        except Exception as e:
            print(f"\n❌ AUTHENTICATION FAILED: {e}")
            print("Please check your internet connection and try again")
            return False
    
    def run_connection_diagnostic(self) -> dict:
        """Run comprehensive connection diagnostic"""
        print("🛠️ Terminus-Enhanced Connection Diagnostic")
        print("=" * 50)
        
        results = {
            "ollama_available": False,
            "github_authenticated": False,
            "python_version": sys.version,
            "script_location": str(self.script_dir),
            "recommendations": []
        }
        
        # Test Ollama
        print("🦙 Testing Ollama Connection...")
        try:
            sys.path.insert(0, str(self.script_dir))
            from ollama_manager import OllamaManager
            
            manager = OllamaManager()
            if manager.is_running():
                results["ollama_available"] = True
                print("✅ Ollama is running and accessible")
            else:
                print("❌ Ollama is not running")
                results["recommendations"].append("Run 'python warbler_terminus_bridge.py --start-ollama' to start Ollama")
        except Exception as e:
            print(f"❌ Ollama test failed: {e}")
            results["recommendations"].append("Ollama manager not available - check installation")
        
        print("-" * 30)
        
        # Test GitHub
        print("🔐 Testing GitHub Authentication...")
        try:
            from github_copilot_auth import get_stored_token, validate_token
            
            token = get_stored_token()
            if token and validate_token(token):
                results["github_authenticated"] = True
                print("✅ GitHub authentication is valid")
            else:
                print("❌ GitHub authentication not found or invalid")
                results["recommendations"].append("Run 'python warbler_terminus_bridge.py --auth-github' to authenticate")
        except Exception as e:
            print(f"❌ GitHub test failed: {e}")
            results["recommendations"].append("GitHub auth module not available - check installation")
        
        print("-" * 30)
        
        # Overall status
        if results["ollama_available"] and results["github_authenticated"]:
            print("🎉 ALL SYSTEMS OPERATIONAL!")
            print("🧙‍♂️ Warbler is ready for AI-powered development!")
        elif results["ollama_available"] or results["github_authenticated"]:
            print("⚠️ PARTIAL FUNCTIONALITY AVAILABLE")
            print("Some AI features are ready, others need setup")
        else:
            print("❌ SETUP REQUIRED")
            print("Both Ollama and GitHub need configuration")
        
        if results["recommendations"]:
            print("\n💡 Recommendations:")
            for i, rec in enumerate(results["recommendations"], 1):
                print(f"  {i}. {rec}")
        
        return results


def main():
    """Command line interface for Terminus bridge"""
    bridge = WarblerTerminusBridge()
    
    if len(sys.argv) < 2:
        print("Warbler Terminus Bridge - Enhanced AI Service Management")
        print()
        print("Usage:")
        print("  python warbler_terminus_bridge.py --start-ollama [model]")
        print("  python warbler_terminus_bridge.py --auth-github")
        print("  python warbler_terminus_bridge.py --diagnostic")
        print("  python warbler_terminus_bridge.py --full-setup")
        sys.exit(1)
    
    command = sys.argv[1].lower()
    
    if command == "--start-ollama":
        model = sys.argv[2] if len(sys.argv) > 2 else None
        success = bridge.start_ollama_with_feedback(model)
        sys.exit(0 if success else 1)
    
    elif command == "--auth-github":
        success = bridge.authenticate_github_with_feedback()
        sys.exit(0 if success else 1)
    
    elif command == "--diagnostic":
        results = bridge.run_connection_diagnostic()
        print(f"\nDiagnostic Results: {json.dumps(results, indent=2)}")
        sys.exit(0)
    
    elif command == "--full-setup":
        print("🚀 Full Warbler Setup - All Services")
        print("=" * 50)
        
        # Step 1: Start Ollama
        print("Step 1/2: Setting up Ollama...")
        ollama_success = bridge.start_ollama_with_feedback("llama3.2:1b")  # Use small model for testing
        
        print("\n" + "=" * 50)
        
        # Step 2: Authenticate GitHub
        print("Step 2/2: Setting up GitHub...")
        github_success = bridge.authenticate_github_with_feedback()
        
        print("\n" + "=" * 50)
        
        # Final status
        if ollama_success and github_success:
            print("🎉 FULL SETUP COMPLETE!")
            print("🧙‍♂️ Warbler is fully operational with both Ollama and GitHub!")
        elif ollama_success:
            print("⚠️ PARTIAL SETUP COMPLETE")
            print("Ollama is ready, but GitHub authentication failed")
        elif github_success:
            print("⚠️ PARTIAL SETUP COMPLETE")
            print("GitHub is ready, but Ollama setup failed")
        else:
            print("❌ SETUP FAILED")
            print("Both Ollama and GitHub setup encountered issues")
        
        sys.exit(0 if (ollama_success and github_success) else 1)
    
    else:
        print(f"❌ Unknown command: {command}")
        sys.exit(1)


if __name__ == "__main__":
    main()