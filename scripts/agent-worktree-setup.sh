#!/usr/bin/env bash
# Prepares a fresh git worktree (e.g. one the Orchestrator app spins up for an
# agent session) so it's ready for focused test runs without redoing
# restore/build work on every iteration.
#
# Mirrors the real baseline the justfile's `check-fast` and `boundary-check`
# recipes rely on (see justfile at repo root):
#   - `format-check` depends on `restore`.
#   - `check-fast` depends on `build`/`boundary-check` and then runs
#     .github/scripts/local_parallel_check.py, which builds both
#     Sandtable.slnx and .github/scripts/ReleaseTestRows/ReleaseTestRows.csproj
#     in the Release configuration before sharding tests.
#   - `boundary-check` depends on `build` and then runs the Cna.Core.Tests
#     boundary suite with `--no-build`, so the solution must already be built.
#
# This script does not itself run format-check, build, or tests as a gate —
# it only restores and builds, so later commands (`just check-fast`,
# `just test-suite <suite>`, `just test-class ...`, `just boundary-check`) can
# run with `--no-restore`/`--no-build` and skip redundant work.

set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

solution="Sandtable.slnx"
release_test_rows_project=".github/scripts/ReleaseTestRows/ReleaseTestRows.csproj"

echo "== dotnet SDK =="
dotnet --version

echo "== restore (justfile: restore) =="
dotnet restore "$solution"

echo "== build, Release config (justfile: check-fast's local_parallel_check.py baseline) =="
dotnet build "$solution" --configuration Release --no-restore

if [ -f "$release_test_rows_project" ]; then
  echo "== build ReleaseTestRows helper project (needed by check-fast's sharding) =="
  dotnet build "$release_test_rows_project" --configuration Release
fi

# Maproom (Angular) deps are not needed for most backend/agent tasks and take
# a while to install. Uncomment if the task touches src/Cna.Maproom:
# npm --prefix src/Cna.Maproom ci

# codebase-memory-mcp is registered globally (not per-worktree-path), so no
# re-index step is needed here today. If it ever becomes worktree-path
# sensitive, a `mcp__codebase-memory-mcp__index_repository` call for this
# worktree's path would go here.

# .mcp.json is intentionally NOT copied into the worktree by
# .worktreeinclude (it holds absolute paths and is meant to be regenerated
# per-worktree). If a future change makes .mcp.json's contents
# worktree-path-sensitive, add a regeneration step here rather than adding it
# to .worktreeinclude.

echo "== worktree setup complete =="
