#!/bin/bash

# Build and package GcpvLynx for Windows distribution
# This script creates a zip file that can be extracted and run on Windows

set -e  # Exit on any error

echo "Building GcpvLynx for Windows distribution..."

# Configuration
PROJECT_NAME="GcpvLynx"
BUILD_CONFIG="Release"
RUNTIME="win-x64"
OUTPUT_DIR="dist"
ZIP_NAME=GcpvLynx-win-x64.zip

# Clean previous builds
echo "Cleaning previous builds..."
if [ -d "bin/${BUILD_CONFIG}/net9.0/${RUNTIME}" ]; then
    rm -rf "bin/${BUILD_CONFIG}/net9.0/${RUNTIME}"
fi
if [ -d "${OUTPUT_DIR}" ]; then
    rm -rf "${OUTPUT_DIR}"
fi

# Create output directory
mkdir -p "${OUTPUT_DIR}"

# Build the application for Windows
echo "Building application for Windows (${RUNTIME})..."
dotnet publish -c ${BUILD_CONFIG} -r ${RUNTIME} --self-contained true -p:PublishSingleFile=true -o "${OUTPUT_DIR}"

# Copy additional files needed for distribution
echo "Copying additional files..."

# Copy appsettings.json
if [ -f "appsettings.json" ]; then
    cp "appsettings.json" "${OUTPUT_DIR}/"
    echo "  ✓ appsettings.json"
else
    echo "  ⚠ Warning: appsettings.json not found"
fi

# Copy README.md if it exists
if [ -f "README.md" ]; then
    cp "README.md" "${OUTPUT_DIR}/"
    echo "  ✓ README.md"
fi


# Create the zip file
echo "Creating distribution package..."
cd "${OUTPUT_DIR}"
zip -r "../${ZIP_NAME}" . -x "*.pdb" "*.deps.json" "*.runtimeconfig.json"
cd ..

# Clean up the temporary output directory
rm -rf "${OUTPUT_DIR}"

# Display results
echo ""
echo "✅ Build completed successfully!"
echo ""
echo "Distribution package created: ${ZIP_NAME}"
echo "Size: $(du -h "${ZIP_NAME}" | cut -f1)"
echo ""
echo "To distribute:"
echo "1. Copy ${ZIP_NAME} to your Windows computer"
echo "2. Extract the zip file"
echo "3. Double-click GcpvLynx.exe to start the application"
echo ""
echo "The package includes:"
echo "  - Self-contained executable (no .NET installation required)"
echo "  - Configuration files"
echo "  - Usage instructions"
