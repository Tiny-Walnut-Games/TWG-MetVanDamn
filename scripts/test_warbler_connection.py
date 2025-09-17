#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Warbler Connection Test
Quick diagnostic tool to test AI endpoints and troubleshoot connection issues
"""

import requests
import sys
from datetime import datetime

def test_endpoint(url, description):
    """Test a single endpoint and report results"""
    print(f"\n🔍 Testing {description}: {url}")
    
    try:
        response = requests.get(url, timeout=10)
        print(f"✅ Status: {response.status_code}")
        print(f"📄 Response length: {len(response.text)} chars")
        if response.headers.get('content-type'):
            print(f"📋 Content-Type: {response.headers['content-type']}")
        return True
    except requests.exceptions.Timeout:
        print(f"⏰ TIMEOUT: {url} took longer than 10 seconds")
        return False
    except requests.exceptions.ConnectionError as e:
        print(f"🔌 CONNECTION ERROR: {e}")
        return False
    except Exception as e:
        print(f"❌ ERROR: {e}")
        return False

def test_ai_request(url, description):
    """Test an actual AI request"""
    print(f"\n🧠 Testing AI request to {description}: {url}")
    
    # Test data for different endpoint formats
    test_requests = [
        # OpenAI-style format
        {
            "model": "gemma3",
            "messages": [{"role": "user", "content": "Hello, respond with just 'Test successful'"}],
            "max_tokens": 50
        },
        # Ollama format
        {
            "model": "gemma3", 
            "prompt": "Hello, respond with just 'Test successful'",
            "stream": False
        }
    ]
    
    for i, data in enumerate(test_requests):
        try:
            print(f"  Trying format {i+1}...")
            response = requests.post(url, json=data, timeout=30)
            print(f"  ✅ Status: {response.status_code}")
            if response.status_code == 200:
                print(f"  📄 Response: {response.text[:200]}...")
                return True
            else:
                print(f"  ❌ Error response: {response.text[:200]}...")
        except Exception as e:
            print(f"  ❌ Request failed: {e}")
    
    return False

def main():
    print("🧙‍♂️ Warbler AI Connection Diagnostic Tool")
    print("=" * 50)
    print(f"🕐 Test started: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    
    # Basic connectivity tests
    endpoints_to_test = [
        ("http://localhost:8080", "Docker Model Runner (Default)"),
        ("http://localhost:11434", "Ollama (Local)"),  
        ("http://localhost:9998", "Alternative AI Service"),
        ("http://127.0.0.1:8080", "Docker Model Runner (127.0.0.1)"),
    ]
    
    print("\n🔌 Testing Basic Connectivity:")
    working_endpoints = []
    
    for url, desc in endpoints_to_test:
        if test_endpoint(url, desc):
            working_endpoints.append((url, desc))
    
    if not working_endpoints:
        print("\n❌ No endpoints are responding!")
        print("\n🛠️  Troubleshooting steps:")
        print("1. Check if Docker is running: docker ps")
        print("2. Check if any AI model is running: docker model ps") 
        print("3. Try starting Gemma3: docker model run gemma3")
        print("4. Check if ports are in use: netstat -an | findstr :8080")
        return
    
    # Test AI requests on working endpoints
    print(f"\n🧠 Testing AI Requests on {len(working_endpoints)} working endpoints:")
    
    ai_endpoints = [
        (f"{url}/v1/chat/completions", f"{desc} (OpenAI format)")
        for url, desc in working_endpoints
    ] + [
        (f"{url}/api/chat", f"{desc} (Ollama format)")  
        for url, desc in working_endpoints
    ]
    
    successful_ai = []
    for url, desc in ai_endpoints:
        if test_ai_request(url, desc):
            successful_ai.append((url, desc))
    
    print(f"\n📊 Test Results Summary:")
    print(f"✅ Working endpoints: {len(working_endpoints)}")
    print(f"🧠 Working AI endpoints: {len(successful_ai)}")
    
    if successful_ai:
        print(f"\n🎉 SUCCESS! Working AI endpoints:")
        for url, desc in successful_ai:
            print(f"   - {desc}: {url}")
        print(f"\n💡 Use these endpoints in your Warbler configuration!")
    else:
        print(f"\n❌ No AI endpoints are working properly")
        print(f"🛠️  Try: docker model run gemma3")

if __name__ == "__main__":
    main()
