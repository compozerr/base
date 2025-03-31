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

USER_BIN_DIR="$HOME/.local/compozerr-bin"
mkdir -p "$USER_BIN_DIR"

curl -s https://storage.googleapis.com/compozerr.firebasestorage.app/${LATEST_VERSION} -o "$USER_BIN_DIR/compozerr"
chmod +x "$USER_BIN_DIR/compozerr"

# The compozerr debug tool checkout https://github.com/compozerr/cursor-dotnet-debug
curl -s https://storage.googleapis.com/compozerr.firebasestorage.app/compozerr-dbg -o "$USER_BIN_DIR/compozerr-dbg"
chmod +x "$USER_BIN_DIR/compozerr-dbg"
curl -s https://storage.googleapis.com/compozerr.firebasestorage.app/libdbgshim.dylib -o "$USER_BIN_DIR/libdbgshim.dylib"
curl -s https://storage.googleapis.com/compozerr.firebasestorage.app/ManagedPart.dll -o "$USER_BIN_DIR/ManagedPart.dll"
curl -s https://storage.googleapis.com/compozerr.firebasestorage.app/Microsoft.CodeAnalysis.CSharp.dll -o "$USER_BIN_DIR/Microsoft.CodeAnalysis.CSharp.dll"
curl -s https://storage.googleapis.com/compozerr.firebasestorage.app/Microsoft.CodeAnalysis.CSharp.Scripting.dll -o "$USER_BIN_DIR/Microsoft.CodeAnalysis.CSharp.Scripting.dll"
curl -s https://storage.googleapis.com/compozerr.firebasestorage.app/Microsoft.CodeAnalysis.dll -o "$USER_BIN_DIR/Microsoft.CodeAnalysis.dll"
curl -s https://storage.googleapis.com/compozerr.firebasestorage.app/Microsoft.CodeAnalysis.Scripting.dll -o "$USER_BIN_DIR/Microsoft.CodeAnalysis.Scripting.dll"

# Add to PATH if not already there
if [[ ":$PATH:" != *":$USER_BIN_DIR:"* ]]; then
    echo ""
    echo "Adding $USER_BIN_DIR to your PATH."
    if [ -f "$HOME/.zshrc" ]; then
        echo 'export PATH="$HOME/.local/compozerr-bin:$PATH"' >> "$HOME/.zshrc"
        echo "Please run 'source $HOME/.zshrc' to update your current shell."
    elif [ -f "$HOME/.bashrc" ]; then
        echo 'export PATH="$HOME/.local/compozerr-bin:$PATH"' >> "$HOME/.bashrc"
        echo "Please run 'source $HOME/.bashrc' to update your current shell."
    else
        echo "Please add the following line to your shell profile:"
        echo 'export PATH="$HOME/.local/compozerr-bin:$PATH"'
    fi
fi

echo "compozerr installed successfully to $USER_BIN_DIR"
