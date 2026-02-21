#!/bin/bash
set -e

echo "Installing Portless.NET..."
echo ""

# Check if .NET SDK is installed
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK is not installed."
    echo "Please install .NET SDK from https://dotnet.microsoft.com/download"
    exit 1
fi

# Install from NuGet.org (or custom source)
echo "Installing Portless.NET.Tool from NuGet..."
dotnet tool install --global Portless.NET.Tool --version 1.0.0

# Add to PATH (if not already)
TOOL_PATH="$HOME/.dotnet/tools"
if [[ ":$PATH:" != *":$TOOL_PATH:"* ]]; then
    echo ""
    echo "Adding $TOOL_PATH to PATH..."

    # Detect shell and update appropriate config file
    if [ -n "$ZSH_VERSION" ]; then
        CONFIG_FILE="$HOME/.zshrc"
    elif [ -n "$BASH_VERSION" ]; then
        CONFIG_FILE="$HOME/.bashrc"
    else
        CONFIG_FILE="$HOME/.profile"
    fi

    echo "export PATH=\"\$PATH:\$HOME/.dotnet/tools\"" >> "$CONFIG_FILE"
    export PATH="$PATH:$HOME/.dotnet/tools"

    echo "Added to $CONFIG_FILE"
    echo "Please restart your terminal or run: source $CONFIG_FILE"
fi

echo ""
# Verify installation
if command -v portless &> /dev/null; then
    echo "Portless installed successfully!"
    echo ""
    portless --version 2>/dev/null || echo "Version: 1.0.0"
    echo ""
    echo "Getting started:"
    echo "  1. Start the proxy: portless proxy start"
    echo "  2. Run your app:    portless myapp dotnet run"
    echo "  3. Access via URL:  http://myapp.localhost"
else
    echo "Installation completed, but 'portless' command not found in PATH."
    echo "Please restart your terminal or run:"
    echo "  export PATH=\"\$PATH:\$HOME/.dotnet/tools\""
    exit 1
fi
