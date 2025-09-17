#!/usr/bin/env python3
"""
TLDA/Warbler LLM Connection Test
Tests connectivity to local LLM endpoints (Gemma3, Ollama, etc.)
"""

import requests
import json
import time

def test_ollama_connection(host="localhost", port=11434):
    """Test connection to Ollama API (common Gemma3 host)"""
    try:
        url = f"http://{host}:{port}/api/version"
        response = requests.get(url, timeout=5)
        if response.status_code == 200:
            print(f"✅ Ollama detected at {host}:{port}")
            return True
    except Exception as e:
        print(f"❌ Ollama not found at {host}:{port}: {e}")
    return False

def test_gemma3_chat(host="localhost", port=11434, model="gemma3"):
    """Test Gemma3 chat functionality"""
    try:
        url = f"http://{host}:{port}/api/chat"
        payload = {
            "model": model,
            "messages": [
                {
                    "role": "user", 
                    "content": "Hello from TLDA/Warbler! Can you help with development tasks?"
                }
            ],
            "stream": False
        }
        
        response = requests.post(url, json=payload, timeout=30)
        if response.status_code == 200:
            result = response.json()
            print(f"✅ Gemma3 chat working!")
            print(f"Response: {result.get('message', {}).get('content', 'No content')}")
            return True
    except Exception as e:
        print(f"❌ Gemma3 chat failed: {e}")
    return False

def test_docker_model_runner(host="localhost", port=9998):
    """Test Docker Model Runner API"""
    try:
        # Try the Docker Model Runner completions endpoint
        url = f"http://{host}:{port}/v1/completions"
        payload = {
            "model": "ai/gemma3",
            "prompt": "Hello from TLDA/Warbler!",
            "max_tokens": 50
        }
        response = requests.post(url, json=payload, timeout=30)
        if response.status_code == 200:
            print(f"✅ Docker Model Runner detected at {host}:{port}")
            result = response.json()
            print(f"Response: {result}")
            return True
    except Exception as e:
        print(f"❌ Docker Model Runner not found at {host}:{port}: {e}")
    return False

def test_alternative_ports():
    """Test common LLM API ports"""
    ports = [9998, 11434, 8080, 5000, 3000, 8000]
    for port in ports:
        print(f"Testing port {port}...")
        if test_ollama_connection(port=port):
            return port
        if test_docker_model_runner(port=port):
            return port
    return None

if __name__ == "__main__":
    print("🧙‍♂️ TLDA/Warbler LLM Connection Test")
    print("=" * 50)
    
    # Test Docker Model Runner first
    print("Testing Docker Model Runner...")
    if test_docker_model_runner():
        print("🎉 Connected to Gemma3 via Docker Model Runner!")
    elif test_ollama_connection():
        test_gemma3_chat()
    else:
        # Try alternative ports
        working_port = test_alternative_ports()
        if working_port:
            print(f"🎉 Found LLM service on port {working_port}")
        else:
            print("❌ No LLM endpoints found. Is Gemma3/Ollama running?")
            print("💡 Try: docker model run ai/gemma3")
