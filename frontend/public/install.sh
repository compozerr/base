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
# Download the latest version
curl -s https://storage.googleapis.com/compozerr.firebasestorage.app/${LATEST_VERSION} -o compozerr

# Make the binary executable
chmod +x compozerr

# Move the binary to the PATH
if [ -w "/usr/local/bin" ]; then
    mv compozerr /usr/local/bin/compozerr
else
    echo "Need sudo permissions to install to add to PATH"
    sudo mv compozerr /usr/local/bin/compozerr
fi

echo "compozerr installed successfully"