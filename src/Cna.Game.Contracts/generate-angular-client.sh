#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
output="$repo_root/src/Cna.Maproom/projects/maproom-alpha/src/app/api/generated"
spec="$repo_root/src/Cna.Game.Contracts/openapi.v1.json"
cd "$repo_root"
npx --yes @openapitools/openapi-generator-cli validate -i "$spec"
rm -rf "$output"
npx --yes @openapitools/openapi-generator-cli generate \
  -i "$spec" -g typescript-angular -o "$output" \
  --additional-properties=ngVersion=22.2.3,providedIn=root,withInterfaces=true \
  --global-property=apis,models,supportingFiles
find "$output" -type f ! -name '*.ts' -delete
"$repo_root/src/Cna.Maproom/node_modules/.bin/prettier" --write "$output" >/dev/null
