#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
GitHub Copilot OAuth Authentication Helper
Secure browser-based authentication for GitHub Copilot API access
"""

import json
import os
import sys
import time
import webbrowser
import hashlib
import secrets
import base64
from urllib.parse import urlencode, parse_qs, urlparse
import http.server
import socketserver
import threading
import requests
from pathlib import Path

# GitHub OAuth configuration
GITHUB_CLIENT_ID = "Iv1.b507a08c87ecfe98"  # Public GitHub App Client ID for Copilot
GITHUB_OAUTH_SCOPES = "read:user"  # Minimal scope for Copilot access
GITHUB_AUTH_URL = "https://github.com/login/oauth/authorize"
GITHUB_TOKEN_URL = "https://github.com/login/oauth/access_token"
REDIRECT_PORT = 8765
REDIRECT_URI = f"http://localhost:{REDIRECT_PORT}/callback"

class GitHubOAuthHandler:
    """Handles secure GitHub OAuth authentication flow"""
    
    def __init__(self):
        self.access_token = None
        self.auth_code = None
        self.state = None
        self.code_verifier = None
        self.code_challenge = None
        self.server = None
        self.auth_complete = False
        
    def _generate_pkce_params(self):
        """Generate PKCE parameters for secure OAuth flow"""
        # Generate code verifier (random string)
        self.code_verifier = base64.urlsafe_b64encode(secrets.token_bytes(32)).decode('utf-8').rstrip('=')
        
        # Generate code challenge (SHA256 hash of verifier)
        challenge_bytes = hashlib.sha256(self.code_verifier.encode('utf-8')).digest()
        self.code_challenge = base64.urlsafe_b64encode(challenge_bytes).decode('utf-8').rstrip('=')
        
        # Generate state for CSRF protection
        self.state = secrets.token_urlsafe(32)
        
    def _create_auth_url(self):
        """Create GitHub authorization URL"""
        self._generate_pkce_params()
        
        params = {
            'client_id': GITHUB_CLIENT_ID,
            'redirect_uri': REDIRECT_URI,
            'scope': GITHUB_OAUTH_SCOPES,
            'state': self.state,
            'code_challenge': self.code_challenge,
            'code_challenge_method': 'S256',
            'response_type': 'code'
        }
        
        return f"{GITHUB_AUTH_URL}?{urlencode(params)}"
    
    def _start_callback_server(self):
        """Start local HTTP server to handle OAuth callback"""
        
        class OAuthCallbackHandler(http.server.BaseHTTPRequestHandler):
            def __init__(self, oauth_handler):
                self.oauth_handler = oauth_handler
                super().__init__()
                
            def __call__(self, *args, **kwargs):
                super().__init__(*args, **kwargs)
                
            def do_GET(self):
                if self.path.startswith('/callback'):
                    parsed_url = urlparse(self.path)
                    query_params = parse_qs(parsed_url.query)
                    
                    # Verify state parameter (CSRF protection)
                    received_state = query_params.get('state', [None])[0]
                    if received_state != self.oauth_handler.state:
                        self._send_error_response("Invalid state parameter")
                        return
                    
                    # Extract authorization code
                    auth_code = query_params.get('code', [None])[0]
                    error = query_params.get('error', [None])[0]
                    
                    if error:
                        self._send_error_response(f"OAuth error: {error}")
                        return
                    
                    if auth_code:
                        self.oauth_handler.auth_code = auth_code
                        self._send_success_response()
                        self.oauth_handler.auth_complete = True
                    else:
                        self._send_error_response("No authorization code received")
                else:
                    self._send_error_response("Invalid callback path")
                    
            def _send_success_response(self):
                """Send success page to user"""
                response_html = """
                <!DOCTYPE html>
                <html>
                <head>
                    <title>GitHub Authentication Success</title>
                    <style>
                        body { font-family: Arial, sans-serif; text-align: center; padding: 50px; }
                        .success { color: #28a745; }
                        .container { max-width: 500px; margin: 0 auto; }
                    </style>
                </head>
                <body>
                    <div class="container">
                        <h1 class="success">🎉 Authentication Successful!</h1>
                        <p>You have successfully authenticated with GitHub Copilot.</p>
                        <p>You can now close this window and return to Unity.</p>
                        <hr>
                        <p><small>Warbler AI Project Orchestrator - Secure GitHub Integration</small></p>
                    </div>
                </body>
                </html>
                """
                
                self.send_response(200)
                self.send_header('Content-type', 'text/html')
                self.end_headers()
                self.wfile.write(response_html.encode('utf-8'))
                
            def _send_error_response(self, error_msg):
                """Send error page to user"""
                response_html = f"""
                <!DOCTYPE html>
                <html>
                <head>
                    <title>GitHub Authentication Error</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; text-align: center; padding: 50px; }}
                        .error {{ color: #dc3545; }}
                        .container {{ max-width: 500px; margin: 0 auto; }}
                    </style>
                </head>
                <body>
                    <div class="container">
                        <h1 class="error">❌ Authentication Failed</h1>
                        <p>{error_msg}</p>
                        <p>Please try again from Unity or check your GitHub connection.</p>
                        <hr>
                        <p><small>Warbler AI Project Orchestrator</small></p>
                    </div>
                </body>
                </html>
                """
                
                self.send_response(400)
                self.send_header('Content-type', 'text/html')
                self.end_headers()
                self.wfile.write(response_html.encode('utf-8'))
                
            def log_message(self, format, *args):
                """Suppress server log messages"""
                pass
        
        # Create handler with reference to OAuth handler
        handler_class = lambda *args, **kwargs: OAuthCallbackHandler(self)(*args, **kwargs)
        
        # Start server
        try:
            self.server = socketserver.TCPServer(("localhost", REDIRECT_PORT), handler_class)
            server_thread = threading.Thread(target=self.server.serve_forever)
            server_thread.daemon = True
            server_thread.start()
            return True
        except Exception as e:
            print(f"❌ Failed to start callback server: {e}")
            return False
    
    def _exchange_code_for_token(self):
        """Exchange authorization code for access token"""
        if not self.auth_code:
            return None
            
        # NOTE: For GitHub Apps without client secret, we use the public flow
        # This is secure because we use PKCE and the app is registered as public
        data = {
            'client_id': GITHUB_CLIENT_ID,
            'code': self.auth_code,
            'redirect_uri': REDIRECT_URI,
            'code_verifier': self.code_verifier  # PKCE verification
        }
        
        headers = {
            'Accept': 'application/json',
            'User-Agent': 'Warbler-Unity-Orchestrator/1.0'
        }
        
        try:
            response = requests.post(GITHUB_TOKEN_URL, data=data, headers=headers, timeout=10)
            response.raise_for_status()
            
            token_data = response.json()
            
            if 'access_token' in token_data:
                return token_data['access_token']
            else:
                print(f"❌ Token exchange failed: {token_data}")
                return None
                
        except requests.exceptions.RequestException as e:
            print(f"❌ Token exchange request failed: {e}")
            return None
    
    def authenticate(self, timeout=120):
        """
        Perform complete OAuth authentication flow
        Returns access token if successful, None otherwise
        """
        print("PROGRESS: Starting GitHub Copilot authentication...")
        
        # Start callback server
        if not self._start_callback_server():
            print("ERROR: Failed to start callback server")
            return None
        
        # Create authorization URL and open browser
        auth_url = self._create_auth_url()
        print("PROGRESS: Callback server started")
        print(f"PROGRESS: Opening browser for GitHub authentication...")
        print(f"AUTH_URL: {auth_url}")
        
        try:
            webbrowser.open(auth_url)
            print("PROGRESS: Browser opened - please complete authentication")
        except Exception as e:
            print(f"PROGRESS: Could not open browser automatically: {e}")
            print(f"PROGRESS: Please manually visit the authentication URL")
        
        # Wait for callback
        start_time = time.time()
        print(f"PROGRESS: Waiting for authentication (timeout: {timeout}s)...")
        
        while not self.auth_complete and (time.time() - start_time) < timeout:
            elapsed = int(time.time() - start_time)
            remaining = timeout - elapsed
            if elapsed % 10 == 0:  # Update every 10 seconds
                print(f"PROGRESS: Waiting for user authentication... ({remaining}s remaining)")
            time.sleep(0.5)
        
        # Clean up server
        if self.server:
            self.server.shutdown()
            self.server.server_close()
        
        if not self.auth_complete:
            print(f"ERROR: Authentication timed out after {timeout} seconds")
            return None
        
        # Exchange code for token
        print("PROGRESS: Authentication received, exchanging for access token...")
        access_token = self._exchange_code_for_token()
        
        if access_token:
            print("SUCCESS: GitHub authentication successful!")
            self.access_token = access_token
            return access_token
        else:
            print("ERROR: Failed to obtain access token")
            return None

def get_stored_token():
    """Get stored GitHub token from secure location"""
    # For now, use a simple file-based approach
    # In production, this should use OS credential storage
    token_file = Path.home() / ".warbler" / "github_token"
    
    if token_file.exists():
        try:
            with open(token_file, 'r') as f:
                token_data = json.load(f)
                return token_data.get('access_token')
        except Exception:
            return None
    
    return None

def store_token(access_token):
    """Store GitHub token securely"""
    token_dir = Path.home() / ".warbler"
    token_dir.mkdir(exist_ok=True)
    
    token_file = token_dir / "github_token"
    
    token_data = {
        'access_token': access_token,
        'created_at': time.time(),
        'source': 'github_oauth'
    }
    
    try:
        with open(token_file, 'w') as f:
            json.dump(token_data, f)
        
        # Set restrictive permissions (owner read/write only)
        os.chmod(token_file, 0o600)
        return True
    except Exception as e:
        print(f"❌ Failed to store token: {e}")
        return False

def validate_token(access_token):
    """Validate GitHub access token"""
    if not access_token:
        return False
    
    headers = {
        'Authorization': f'token {access_token}',
        'Accept': 'application/json',
        'User-Agent': 'Warbler-Unity-Orchestrator/1.0'
    }
    
    try:
        response = requests.get('https://api.github.com/user', headers=headers, timeout=10)
        return response.status_code == 200
    except Exception:
        return False

def get_valid_token():
    """Get a valid GitHub token, authenticating if necessary"""
    # Try existing token first
    stored_token = get_stored_token()
    if stored_token and validate_token(stored_token):
        print("✅ Using existing valid GitHub token")
        return stored_token
    
    # Need new authentication
    print("🔐 No valid token found, starting authentication...")
    oauth_handler = GitHubOAuthHandler()
    new_token = oauth_handler.authenticate()
    
    if new_token:
        if store_token(new_token):
            print("💾 Token stored securely")
        return new_token
    
    return None

def revoke_token():
    """Revoke stored GitHub token"""
    token_file = Path.home() / ".warbler" / "github_token"
    
    if token_file.exists():
        try:
            token_file.unlink()
            print("✅ GitHub token revoked")
            return True
        except Exception as e:
            print(f"❌ Failed to revoke token: {e}")
            return False
    
    print("ℹ️ No token to revoke")
    return True

def main():
    """Command line interface for GitHub authentication"""
    if len(sys.argv) < 2:
        print("Usage:")
        print("  python github_copilot_auth.py auth      - Authenticate with GitHub")
        print("  python github_copilot_auth.py validate  - Check if token is valid")
        print("  python github_copilot_auth.py revoke    - Revoke stored token")
        print("  python github_copilot_auth.py auth-progress - Authenticate with Unity progress feedback")
        sys.exit(1)
    
    command = sys.argv[1].lower()
    
    if command == "auth":
        token = get_valid_token()
        if token:
            print("🎉 GitHub Copilot authentication complete!")
            print("Token stored securely for Unity integration")
        else:
            print("❌ Authentication failed")
            sys.exit(1)
    
    elif command == "auth-progress":
        # Special mode for Unity integration with structured progress output
        print("PROGRESS: Checking for existing GitHub token...")
        stored_token = get_stored_token()
        if stored_token and validate_token(stored_token):
            print("SUCCESS: Using existing valid GitHub token")
            sys.exit(0)
        
        print("PROGRESS: No valid token found, starting authentication...")
        oauth_handler = GitHubOAuthHandler()
        new_token = oauth_handler.authenticate()
        
        if new_token:
            if store_token(new_token):
                print("SUCCESS: GitHub Copilot authentication complete and token stored")
            else:
                print("SUCCESS: GitHub Copilot authentication complete but token storage failed")
            sys.exit(0)
        else:
            print("ERROR: GitHub authentication failed")
            sys.exit(1)
    
    elif command == "validate":
        token = get_stored_token()
        if validate_token(token):
            print("✅ Stored GitHub token is valid")
        else:
            print("❌ No valid GitHub token found")
            sys.exit(1)
    
    elif command == "revoke":
        if revoke_token():
            print("✅ GitHub token revoked successfully")
        else:
            sys.exit(1)
    
    else:
        print(f"❌ Unknown command: {command}")
        sys.exit(1)

if __name__ == "__main__":
    main()