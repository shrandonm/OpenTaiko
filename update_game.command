#!/bin/bash

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
STATUS=0

cd "$SCRIPT_DIR" || exit 1

echo "Updating repository..."
if ! git pull; then
    STATUS=$?
fi

if [ "$STATUS" -eq 0 ]; then
    echo
    echo "Building macOS release..."
    if ! /bin/bash "$SCRIPT_DIR/build-osx-arm64.sh"; then
        STATUS=$?
    fi
fi

echo
if [ "$STATUS" -eq 0 ]; then
    echo "Update completed!"
else
    echo "Update failed with exit code $STATUS."
fi

read -r -p "Press Enter to close..."
exit "$STATUS"