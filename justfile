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

# Focused local iteration (Release, build selected projects before testing).
test-suites:
    python3 .github/scripts/local_tests.py list

test-suite suite:
    python3 .github/scripts/local_tests.py {{quote(suite)}}

test-class suite pattern:
    python3 .github/scripts/local_tests.py {{quote(suite)}} --class {{quote(pattern)}}

test-method suite pattern:
    python3 .github/scripts/local_tests.py {{quote(suite)}} --method {{quote(pattern)}}

# Same row allocation as CI, one shard at a time on this machine.
test-shard shard:
    python3 .github/scripts/local_tests.py {{quote(shard)}}

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

# Angular workspace and Field Atlas (Node version pinned in src/Cna.Maproom/.nvmrc).
maproom-setup:
    npm --prefix src/Cna.Maproom ci

maproom:
    npm --prefix src/Cna.Maproom start

field-atlas:
    npm --prefix src/Cna.Maproom run storybook

maproom-check:
    npm --prefix src/Cna.Maproom run check

maproom-browser-check:
    npm --prefix src/Cna.Maproom run test:e2e

# Containerized Development-only proving ground. No campaign API is connected yet.
proving-ground-up:
    #!/usr/bin/env bash
    set -euo pipefail
    docker compose -f compose.proving-ground.yaml up --build -d
    for attempt in {1..60}; do
        if curl --silent --show-error --fail http://127.0.0.1:5180/health >/dev/null 2>&1 && \
           curl --silent --show-error --fail http://127.0.0.1:4200/healthz >/dev/null 2>&1; then
            echo "Proving Ground ready: Maproom http://127.0.0.1:4200; Orleans host http://127.0.0.1:5180"
            exit 0
        fi
        sleep 1
    done
    docker compose -f compose.proving-ground.yaml ps
    echo "Proving Ground did not become healthy within 60 seconds." >&2
    exit 1

proving-ground-smoke:
    curl --fail --silent --show-error http://127.0.0.1:5180/health
    curl --fail --silent --show-error http://127.0.0.1:4200/healthz

proving-ground-logs:
    docker compose -f compose.proving-ground.yaml logs --follow

proving-ground-down:
    docker compose -f compose.proving-ground.yaml down

# Trusted CLI evidence, separate from browser campaign seeding.
rules-lab-evidence:
    dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- maneuver run --manifest scenarios/maneuvers/rules-lab.reaction.serial.breakdown.v1.json --artifact-root artifacts/exercises
