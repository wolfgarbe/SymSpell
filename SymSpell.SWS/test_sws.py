import json
import ssl
import sys
import urllib.request

def main():
    if len(sys.argv) < 2:
        print("Certificate path not provided.", file=sys.stderr)
        sys.exit(1)

    cert_path = sys.argv[1]
    context = ssl.create_default_context(cafile=cert_path)

    try:
        # The service redirects http to https, so let's use https directly
        # We request Verbosity.All to get all suggestions within maxEditDistance
        url = "https://localhost:5001/lookup?word=watevr&verbosity=All"
        with urllib.request.urlopen(url, context=context) as response:
            if response.status != 200:
                print(f"Error: Received status code {response.status}", file=sys.stderr)
                sys.exit(1)

            data = json.loads(response.read().decode())
            
            if not data:
                print("Error: Received empty response.", file=sys.stderr)
                sys.exit(1)

            # Check if 'whatever' is in the list of suggestions
            found = any(s.get('term') == 'whatever' for s in data if isinstance(s, dict))

            if found:
                print("Test passed!")
            else:
                print(f"Test failed: Expected a suggestion of 'whatever' in the results, but got: {json.dumps(data)}", file=sys.stderr)
                sys.exit(1)

    except Exception as e:
        print(f"An error occurred: {e}", file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    main()
