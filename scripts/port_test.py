import requests

# Test specific ports that might be Docker Model Runner
test_ports = [38000, 50051, 50060, 9010, 8084]

for port in test_ports:
    try:
        print(f"Testing port {port}...")
        
        # Try a simple GET first
        response = requests.get(f"http://localhost:{port}", timeout=2)
        print(f"Port {port} GET: {response.status_code} - {response.text[:100]}")
        
    except Exception as e:
        print(f"Port {port}: {str(e)[:100]}")

print("Done.")
