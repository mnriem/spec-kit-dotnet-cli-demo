#!/bin/bash
# Build script for multi-platform publishing
# Usage: ./scripts/build-all.sh [--release]

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
PROJECT="$ROOT_DIR/src/TimezoneUtility/TimezoneUtility.csproj"
OUTPUT_DIR="$ROOT_DIR/publish"

# Default to Release configuration
CONFIGURATION="Release"

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --debug)
            CONFIGURATION="Debug"
            shift
            ;;
        --release)
            CONFIGURATION="Release"
            shift
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

echo "Building tzutil ($CONFIGURATION)..."
echo "Output: $OUTPUT_DIR"
echo ""

# Clean output directory
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# Target platforms
PLATFORMS=(
    "win-x64"
    "win-arm64"
    "linux-x64"
    "linux-arm64"
    "osx-x64"
    "osx-arm64"
)

# Build for each platform
for RID in "${PLATFORMS[@]}"; do
    echo "Building for $RID..."
    
    dotnet publish "$PROJECT" \
        --configuration "$CONFIGURATION" \
        --runtime "$RID" \
        --output "$OUTPUT_DIR/$RID" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:PublishReadyToRun=true \
        -p:IncludeNativeLibrariesForSelfExtract=true
    
    # Create archive
    ARCHIVE_NAME="tzutil-$RID"
    pushd "$OUTPUT_DIR/$RID" > /dev/null
    
    if [[ "$RID" == win-* ]]; then
        # Windows: create zip
        zip -q "$OUTPUT_DIR/$ARCHIVE_NAME.zip" tzutil.exe
    else
        # Unix: create tar.gz
        tar -czf "$OUTPUT_DIR/$ARCHIVE_NAME.tar.gz" tzutil
    fi
    
    popd > /dev/null
    
    echo "  Created: $ARCHIVE_NAME"
done

echo ""
echo "Build complete! Archives in: $OUTPUT_DIR"
ls -la "$OUTPUT_DIR"/*.{zip,tar.gz} 2>/dev/null || true
