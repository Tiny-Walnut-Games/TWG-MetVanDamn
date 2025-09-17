#!/usr/bin/env python3
"""
Simple connection test for Unity Warbler integration
"""

import sys
import requests
import subprocess
import time

def test_connection(endpoint):
    """Test connection to AI service endpoint"""
    try:
        response = requests.get(endpoint, timeout=3)
        if response.status_code == 200:
            return True, "HTTP API server is running"
        else:
            return False, f"HTTP {response.status_code}: {response.reason}"
    except requests.exceptions.ConnectionError:
        return False, "No HTTP API server running on this port"
    except requests.exceptions.Timeout:
        return False, "Connection timeout - server may be overloaded"
    except Exception as e:
        return False, f"Connection error: {str(e)}"

def start_gemma3():
    """Start Gemma3 AI service via Docker"""
    try:
        # Start the Docker model
        process = subprocess.Popen(
            ["docker", "model", "run", "ai/gemma3:latest"],
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE
        )
        
        # Give it a moment to start
        time.sleep(3)
        
        # Test if it's running
        connected, message = test_connection("http://localhost:8080")
        return connected
    except Exception as e:
        print(f"Failed to start Gemma3: {e}")
        return False

def main():
    if len(sys.argv) > 1:
        if sys.argv[1] == "--test-connection":
            endpoint = sys.argv[2] if len(sys.argv) > 2 else "http://localhost:8080"
            connected, message = test_connection(endpoint)
            if connected:
                print("CONNECTION SUCCESS")
                return 0
            else:
                print(f"CONNECTION FAILED: {message}")
                return 1
        elif sys.argv[1] == "--start-gemma3":
            return 0 if start_gemma3() else 1
    else:
        # Default: test connection
        connected, message = test_connection("http://localhost:8080")
        print(f"Connection test: {'SUCCESS' if connected else 'FAILED'}")
        print(f"Message: {message}")
        return 0 if connected else 1

if __name__ == "__main__":
    sys.exit(main())
