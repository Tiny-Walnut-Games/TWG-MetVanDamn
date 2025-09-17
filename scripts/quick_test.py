import requests
import json

# Test Docker Model Runner on common ports
ports = [9998, 11434, 8080, 9999]

for port in ports:
    try:
        print(f"Testing port {port}...")
        
        # Try OpenAI-compatible completion endpoint
        url = f"http://localhost:{port}/v1/completions"
        payload = {
            "model": "ai/gemma3",
            "prompt": "Hello!",
            "max_tokens": 10
        }
        
        response = requests.post(url, json=payload, timeout=5)
        print(f"Port {port}: Status {response.status_code}")
        
        if response.status_code == 200:
            print(f"✅ SUCCESS! Gemma3 running on port {port}")
            print(f"Response: {response.text[:200]}")
            break
            
    except Exception as e:
        print(f"Port {port}: {e}")

print("\nDone testing ports.")
