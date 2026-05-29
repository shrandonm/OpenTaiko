#!/bin/bash

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PUBLISH_DIR="$SCRIPT_DIR/OpenTaiko/bin/Release/net8.0/osx-arm64/publish"
EXECUTABLE="$PUBLISH_DIR/OpenTaiko"

cd "$SCRIPT_DIR" || exit 1

NEEDS_BUILD=false
if [ ! -f "$EXECUTABLE" ]; then
    NEEDS_BUILD=true
elif [ -n "$(find "$SCRIPT_DIR/OpenTaiko/src" "$SCRIPT_DIR/FDK/src" -name "*.cs" -newer "$EXECUTABLE" 2>/dev/null | head -1)" ]; then
    echo "Source changes detected. Rebuilding..."
    NEEDS_BUILD=true
fi

if [ "$NEEDS_BUILD" = true ]; then
    echo "Building release..."
    /bin/bash "$SCRIPT_DIR/build-osx-arm64.sh" || exit 1
    echo
fi

echo "Launching OpenTaiko..."
cd "$PUBLISH_DIR" && exec "$EXECUTABLE"
