#!/usr/bin/env bash
# Runs the opt-in capacity benchmarks (tests/Cna.OrleansHost.Tests/CampaignCapacity*BenchmarkTests.cs)
# under the conditions their published numbers need: Release, Server GC (DOTNET_gcServer=1), an
# explicit JSONL output file, and a recorded environment header line. Without this wrapper the
# test host runs Workstation GC and reports ~2.5x lower throughput (docs/work/2026-11-25-capacity-benchmark-baseline.md).
#
# Needs Docker (Testcontainers Postgres) unless SANDTABLE_TEST_POSTGRES_URL points at a shared
# instance (scripts/shared-test-postgres.sh up).
#
# Usage:
#   scripts/capacity-benchmark.sh [--out FILE] [--only history|parallel|stream|storm]... [--no-build]
# Default: all four benchmarks, output artifacts/capacity-benchmark/<utc-stamp>.jsonl
# Line 1 of the output is {"kind":"env",...}; each benchmark appends its own {"kind":...} lines.

set -euo pipefail
cd "$(dirname "$0")/.."

out=""
only=()
build=1
while [[ $# -gt 0 ]]; do
  case "$1" in
    --out) out="$2"; shift 2 ;;
    --only) only+=("$2"); shift 2 ;;
    --no-build) build=0; shift ;;
    *) echo "Usage: $(basename "$0") [--out FILE] [--only history|parallel|stream|storm]... [--no-build]" >&2; exit 2 ;;
  esac
done
[[ ${#only[@]} -gt 0 ]] || only=(history parallel stream storm)

if [[ -z "$out" ]]; then
  mkdir -p artifacts/capacity-benchmark
  out="artifacts/capacity-benchmark/$(date -u +%Y%m%dT%H%M%SZ).jsonl"
fi
mkdir -p "$(dirname "$out")"
out="$(cd "$(dirname "$out")" && pwd)/$(basename "$out")"
: > "$out"

cores="$( (sysctl -n hw.ncpu 2>/dev/null || nproc 2>/dev/null) || echo unknown)"
sha="$(git rev-parse HEAD)"
dirty="false"; [[ -n "$(git status --porcelain)" ]] && dirty="true"
printf '{"kind":"env","gcServer":true,"cores":"%s","os":"%s","dotnet":"%s","sha":"%s","dirty":%s,"sharedPostgres":%s,"startedUtc":"%s"}\n' \
  "$cores" "$(uname -sr)" "$(dotnet --version)" "$sha" "$dirty" \
  "$([[ -n "${SANDTABLE_TEST_POSTGRES_URL:-}" ]] && echo true || echo false)" \
  "$(date -u +%Y-%m-%dT%H:%M:%SZ)" >> "$out"

export DOTNET_gcServer=1
export SANDTABLE_CAPACITY_BENCHMARK=1
export SANDTABLE_BENCHMARK_OUT="$out"

project=tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj
if [[ $build -eq 1 ]]; then
  dotnet build "$project" --configuration Release
fi

for name in "${only[@]}"; do
  case "$name" in
    history)  method='*CapacityBenchmarkHistoryGrowthExploration' ;;
    parallel) method='*CapacityBenchmarkUnitOfWorkParallelism' ;;
    stream)   method='*CapacityBenchmarkStreamLengthStorageCost' ;;
    storm)    method='*CapacityBenchmarkDeadlineStorm' ;;
    *) echo "unknown benchmark '$name'" >&2; exit 2 ;;
  esac
  echo "== $name ($method)" >&2
  dotnet test --project "$project" --configuration Release --no-build --filter-method "$method"
done
echo "benchmark output: $out" >&2
