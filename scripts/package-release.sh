#!/usr/bin/env sh
set -eu

ROOT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
OUTPUT="${1:-$ROOT_DIR/tongji-retail-release.zip}"

cd "$ROOT_DIR"
rm -f "$OUTPUT"
zip -qr "$OUTPUT" . \
  -x '.git/*' \
  -x '.env' \
  -x '**/bin/*' \
  -x '**/obj/*' \
  -x '**/TestResults/*' \
  -x 'frontend/node_modules/*' \
  -x 'frontend/.next/*' \
  -x 'frontend/coverage/*' \
  -x '*.zip'
printf 'Created %s\n' "$OUTPUT"
