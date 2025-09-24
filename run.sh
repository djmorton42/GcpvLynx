#!/bin/bash

# GcpvLynx Development Script
# This script builds and runs the Avalonia application

set -e  # Exit on any error

echo "🏁 GcpvLynx Development"
echo "======================="

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ Error: .NET SDK is not installed or not in PATH"
    echo "Please install .NET 8.0 or later from https://dotnet.microsoft.com/download"
    exit 1
fi

# Check .NET version
DOTNET_VERSION=$(dotnet --version)
echo "✅ .NET version: $DOTNET_VERSION"

# Clean previous builds
echo "🧹 Cleaning previous builds..."
dotnet clean --configuration Debug

# Restore packages
echo "📦 Restoring NuGet packages..."
dotnet restore

# Build the project
echo "🔨 Building GcpvLynx..."
dotnet build --configuration Debug --verbosity minimal

# Check if build was successful
if [ $? -eq 0 ]; then
    echo "✅ Build successful!"
    echo ""
    echo "🚀 Starting GcpvLynx application..."
    echo "   Framework: .NET 9.0"
    echo "   UI Framework: Avalonia UI"
    echo "   Configuration: Debug"
    echo ""
    echo "Press Ctrl+C to stop the application"
    echo "======================="
    
    # Run the application
    dotnet run --configuration Debug
else
    echo "❌ Build failed! Please check the error messages above."
    exit 1
fi
