set dotenv-load := false

solution := "Sandtable.slnx"

default:
    @just --list

setup:
    dotnet --version
    dotnet restore {{ solution }}

restore:
    dotnet restore {{ solution }}

build: restore
    dotnet build {{ solution }} --no-restore

test: build
    dotnet test --solution {{ solution }} --no-build

boundary-check: build
    dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait "Boundary=UserSpace"

format:
    dotnet format {{ solution }}

format-check: restore
    dotnet format {{ solution }} --verify-no-changes --no-restore

check: format-check build boundary-check test

# Install the pinned Lychee release described in the CIH-IMP-004 baseline packet.
docs-links:
    #!/usr/bin/env bash
    set -euo pipefail
    if [[ "$(lychee --version)" != "lychee 0.24.2" ]]; then
        echo "docs-links requires lychee 0.24.2 on PATH." >&2
        exit 1
    fi
    inputs="$(mktemp)"
    trap 'rm -f "$inputs"' EXIT
    git -c core.quotepath=false ls-files -- '*.md' > "$inputs"
    test -s "$inputs"
    echo "Checking $(wc -l < "$inputs" | tr -d ' ') tracked Markdown files."
    lychee --config .lychee.toml --root-dir "$PWD" --files-from "$inputs"

run:
    dotnet run --project src/Cna.AppHost/Cna.AppHost.csproj
