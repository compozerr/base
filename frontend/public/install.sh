#!/bin/bash
# Check if jq is installed
if ! command -v jq &> /dev/null; then
    echo "jq not found, installing..."
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        sudo apt-get update && sudo apt-get install -y jq
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        brew install jq
    else
        echo "Unsupported OS for automatic jq installation"
        exit 1
    fi
fi

LATEST_VERSION=$(curl -s https://storage.googleapis.com/compozerr.firebasestorage.app/versions.json?cachebust=$(date +%s) | jq -r 'max_by(split(".") | map(tonumber))' | tr -d '"')

printf "Downloading compozerr v${LATEST_VERSION}...\n"

# Download directly to /usr/local/bin with sudo if needed
if [ -w "/usr/local/bin" ]; then
    curl -s https://storage.googleapis.com/compozerr.firebasestorage.app/${LATEST_VERSION} -o /usr/local/bin/compozerr
    chmod +x /usr/local/bin/compozerr
else
    sudo curl -s https://storage.googleapis.com/compozerr.firebasestorage.app/${LATEST_VERSION} -o /usr/local/bin/compozerr
    sudo chmod +x /usr/local/bin/compozerr
fi

echo "compozerr installed successfully"
