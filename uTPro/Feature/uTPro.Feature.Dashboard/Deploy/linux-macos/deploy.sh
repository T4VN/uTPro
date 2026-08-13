#!/usr/bin/env bash
# ====================================================================
#  uTPro Deploy Script (Linux / macOS)
#  ------------------------------------
#  Called by the uTPro Dashboard "Deploy" button via the management API.
#  Downloads the latest GitHub release publish asset, stops all configured
#  systemd services, removes old DLLs, overlays the new release (preserving
#  data folders), and restarts the services.
#
#  Exit codes:
#    0 = success (DEPLOY_SUCCESS or NO_CHANGES in stdout)
#    1 = error
#
#  Usage:
#    bash deploy.sh [--config /path/to/deploy.config.json]
# ====================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CONFIG_FILE="${1:-$SCRIPT_DIR/../deploy.config.json}"

if [ ! -f "$CONFIG_FILE" ]; then
    echo "ERROR: Config file not found: $CONFIG_FILE" >&2
    exit 1
fi

# --- Parse config (requires python3 or jq) ---
if command -v python3 >/dev/null 2>&1; then
    REPO=$(python3 -c "import json,sys;c=json.load(open('$CONFIG_FILE'));print(c['github']['repo'])")
    ASSET_PATTERN=$(python3 -c "import json,sys;c=json.load(open('$CONFIG_FILE'));print(c['github']['assetPattern'])")
    SITES_JSON=$(python3 -c "import json,sys;c=json.load(open('$CONFIG_FILE'));print(json.dumps(c['sites']))")
    PRESERVE_JSON=$(python3 -c "import json,sys;c=json.load(open('$CONFIG_FILE'));print(json.dumps(c['preserve']))")
elif command -v jq >/dev/null 2>&1; then
    REPO=$(jq -r '.github.repo' "$CONFIG_FILE")
    ASSET_PATTERN=$(jq -r '.github.assetPattern' "$CONFIG_FILE")
    SITES_JSON=$(jq -c '.sites' "$CONFIG_FILE")
    PRESERVE_JSON=$(jq -c '.preserve' "$CONFIG_FILE")
else
    echo "ERROR: python3 or jq is required to parse config." >&2
    exit 1
fi

TEMP_DIR=$(mktemp -d -t utpro-deploy.XXXXXX)

cleanup() {
    rm -rf "$TEMP_DIR"
}
trap cleanup EXIT

# --- 1. Query the latest release from GitHub ---
echo "[1/5] Querying latest release from GitHub ($REPO)..."
RELEASE_JSON=$(curl -fsSL -H 'User-Agent: uTPro-Deploy' "https://api.github.com/repos/$REPO/releases/latest")
TAG=$(echo "$RELEASE_JSON" | python3 -c "import json,sys;print(json.load(sys.stdin)['tag_name'])" 2>/dev/null \
    || echo "$RELEASE_JSON" | jq -r '.tag_name')
echo "       Latest release: $TAG"

# --- 2. Check if update is needed ---
FIRST_SITE_PATH=$(echo "$SITES_JSON" | python3 -c "import json,sys;print(json.load(sys.stdin)[0]['path'])" 2>/dev/null \
    || echo "$SITES_JSON" | jq -r '.[0].path')
MARKER_FILE="$FIRST_SITE_PATH/.utpro-release"

if [ -f "$MARKER_FILE" ]; then
    INSTALLED=$(tr -d '[:space:]' < "$MARKER_FILE")
    if [ "$INSTALLED" = "$TAG" ]; then
        echo "NO_CHANGES"
        echo "       All sites are already on $TAG."
        exit 0
    fi
    echo "       Installed: $INSTALLED -> Updating to: $TAG"
fi

# --- 3. Download and extract the release asset ---
echo "[2/5] Downloading release asset..."
# Find the asset URL matching the pattern
ASSET_URL=$(echo "$RELEASE_JSON" | python3 -c "
import json,sys,fnmatch
data=json.load(sys.stdin)
for a in data.get('assets',[]):
    if fnmatch.fnmatch(a['name'], '$ASSET_PATTERN'):
        print(a['browser_download_url']); break
" 2>/dev/null || echo "$RELEASE_JSON" | jq -r ".assets[] | select(.name | test(\"publish_output.*\\\\.zip\")) | .browser_download_url" | head -1)

if [ -z "$ASSET_URL" ]; then
    echo "ERROR: No asset matching '$ASSET_PATTERN' found on release $TAG." >&2
    exit 1
fi

ZIP_PATH="$TEMP_DIR/release.zip"
EXTRACT_PATH="$TEMP_DIR/extracted"
mkdir -p "$EXTRACT_PATH"

curl -fsSL -H 'User-Agent: uTPro-Deploy' "$ASSET_URL" -o "$ZIP_PATH"
echo "       Downloaded."

echo "[3/5] Extracting..."
if command -v unzip >/dev/null 2>&1; then
    unzip -q "$ZIP_PATH" -d "$EXTRACT_PATH"
else
    tar -xf "$ZIP_PATH" -C "$EXTRACT_PATH"
fi

# Find the release root (single subfolder or flat)
RELEASE_ROOT=$(find "$EXTRACT_PATH" -mindepth 1 -maxdepth 1 -type d | head -n1)
if [ -z "$RELEASE_ROOT" ]; then
    RELEASE_ROOT="$EXTRACT_PATH"
fi

# --- 4. Stop all services ---
echo "[4/5] Stopping services..."
SITE_COUNT=$(echo "$SITES_JSON" | python3 -c "import json,sys;print(len(json.load(sys.stdin)))" 2>/dev/null \
    || echo "$SITES_JSON" | jq 'length')

for i in $(seq 0 $((SITE_COUNT - 1))); do
    SERVICE_NAME=$(echo "$SITES_JSON" | python3 -c "import json,sys;print(json.load(sys.stdin)[$i].get('serviceName',''))" 2>/dev/null \
        || echo "$SITES_JSON" | jq -r ".[$i].serviceName // empty")
    SITE_NAME=$(echo "$SITES_JSON" | python3 -c "import json,sys;print(json.load(sys.stdin)[$i]['name'])" 2>/dev/null \
        || echo "$SITES_JSON" | jq -r ".[$i].name")

    if [ -n "$SERVICE_NAME" ]; then
        sudo systemctl stop "$SERVICE_NAME" 2>/dev/null && echo "       Stopped: $SERVICE_NAME" \
            || echo "       Warning: could not stop $SERVICE_NAME"
    fi
done

sleep 3

# --- 5. Update each site ---
echo "[5/5] Deploying to $SITE_COUNT site(s)..."

# Build preserve list
PRESERVE_LIST=$(echo "$PRESERVE_JSON" | python3 -c "import json,sys;[print(p) for p in json.load(sys.stdin)]" 2>/dev/null \
    || echo "$PRESERVE_JSON" | jq -r '.[]')

for i in $(seq 0 $((SITE_COUNT - 1))); do
    SITE_PATH=$(echo "$SITES_JSON" | python3 -c "import json,sys;print(json.load(sys.stdin)[$i]['path'])" 2>/dev/null \
        || echo "$SITES_JSON" | jq -r ".[$i].path")
    SITE_NAME=$(echo "$SITES_JSON" | python3 -c "import json,sys;print(json.load(sys.stdin)[$i]['name'])" 2>/dev/null \
        || echo "$SITES_JSON" | jq -r ".[$i].name")

    echo "       -> $SITE_NAME ($SITE_PATH)"
    mkdir -p "$SITE_PATH"

    # 5a. Remove old DLL/PDB files from site root (not recursive into data dirs)
    REMOVED=$(find "$SITE_PATH" -maxdepth 1 \( -name '*.dll' -o -name '*.pdb' \) -type f -delete -print | wc -l)
    echo "          Removed $REMOVED old .dll/.pdb files"

    # 5b. Copy new files, skipping preserved paths
    COPIED=0
    while IFS= read -r -d '' src_file; do
        rel="${src_file#$RELEASE_ROOT/}"
        skip=false
        while IFS= read -r preserve_path; do
            if [ "$rel" = "$preserve_path" ] || [[ "$rel" == "$preserve_path/"* ]]; then
                skip=true
                break
            fi
        done <<< "$PRESERVE_LIST"

        if [ "$skip" = false ]; then
            dest="$SITE_PATH/$rel"
            dest_dir=$(dirname "$dest")
            mkdir -p "$dest_dir"
            cp -f "$src_file" "$dest"
            COPIED=$((COPIED + 1))
        fi
    done < <(find "$RELEASE_ROOT" -type f -print0)
    echo "          Copied $COPIED files"

    # 5c. Stamp the version marker
    printf '%s' "$TAG" > "$SITE_PATH/.utpro-release"
done

# --- 6. Start all services ---
for i in $(seq 0 $((SITE_COUNT - 1))); do
    SERVICE_NAME=$(echo "$SITES_JSON" | python3 -c "import json,sys;print(json.load(sys.stdin)[$i].get('serviceName',''))" 2>/dev/null \
        || echo "$SITES_JSON" | jq -r ".[$i].serviceName // empty")
    if [ -n "$SERVICE_NAME" ]; then
        sudo systemctl start "$SERVICE_NAME" 2>/dev/null && echo "       Started: $SERVICE_NAME" \
            || echo "       Warning: could not start $SERVICE_NAME"
    fi
done

echo "DEPLOY_SUCCESS"
echo "       Deployed $TAG to $SITE_COUNT site(s)."
