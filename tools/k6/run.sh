#!/usr/bin/env bash
set -e

# Default settings
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

PROFILE="${1:-load}"
TARGET_SCRIPT="${2:-scripts/dashboard/get-dashboard-bundle.js}"
BASE_URL="${BASE_URL:-http://localhost:8080}"

echo "=========================================================="
echo "          IID API — k6 Performance & Load Testing         "
echo "=========================================================="
echo " Target Script : $TARGET_SCRIPT"
echo " Profile       : $PROFILE"
echo " Base URL      : $BASE_URL"
echo "=========================================================="

if command -v k6 &> /dev/null; then
  echo "🚀 Running with local k6 installation..."
  cd "$SCRIPT_DIR"
  k6 run \
    -e PROFILE="$PROFILE" \
    -e BASE_URL="$BASE_URL" \
    "$TARGET_SCRIPT"
else
  echo "🐳 Local k6 not found. Running with Docker (grafana/k6)..."
  docker run --rm -i \
    --network="host" \
    -v "$SCRIPT_DIR:/scripts" \
    -e PROFILE="$PROFILE" \
    -e BASE_URL="$BASE_URL" \
    grafana/k6 run "/scripts/$TARGET_SCRIPT"
fi
