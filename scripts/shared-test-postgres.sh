#!/usr/bin/env bash
# Starts/stops ONE long-lived, named Postgres container that several concurrent worker processes
# (each a Codex/Claude worker in its own git worktree, spun up by the Orchestrator app) can share,
# instead of each worker's `Cna.OrleansHost.Tests` run starting its own Testcontainers container.
# That avoids N concurrent containers + N concurrent Release builds piling up on one machine. See
# the "Shared Postgres for concurrent workers" section of docs/runbooks/local-testing.md.
#
# This is purely opt-in tooling: it has no effect unless a worker also exports
# SANDTABLE_TEST_POSTGRES_URL (this script prints the exact line to run). Leaving it unset keeps
# today's default behavior -- each test process starts its own ephemeral container, as always.
#
# Usage:
#   scripts/shared-test-postgres.sh up      # idempotent: starts the container if needed, or just
#                                            # prints connection info if it's already running
#   scripts/shared-test-postgres.sh down    # stops and removes the container

set -euo pipefail

container_name="sandtable-shared-test-postgres"
image="postgres:16-alpine" # same image PostgresContainerFixture uses, so test behavior matches
port="5433"                # deliberately not 5432, so this doesn't collide with a local Postgres
user="postgres"
password="postgres"
database="postgres"
connection_string="Host=localhost;Port=${port};Username=${user};Password=${password};Database=${database}"

usage() {
  echo "Usage: $(basename "$0") {up|down}" >&2
  exit 1
}

print_export_line() {
  echo
  echo "Run this in each worker's shell (or set it in the Orchestrator's per-worker env):"
  echo
  echo "export SANDTABLE_TEST_POSTGRES_URL=\"$connection_string\""
}

cmd_up() {
  if docker container inspect "$container_name" >/dev/null 2>&1; then
    local running
    running="$(docker container inspect -f '{{.State.Running}}' "$container_name")"
    if [ "$running" != "true" ]; then
      echo "Container '$container_name' exists but is stopped; starting it."
      docker start "$container_name" >/dev/null
    else
      echo "Container '$container_name' is already running."
    fi
  else
    echo "Starting new container '$container_name' ($image) on port $port."
    docker run -d \
      --name "$container_name" \
      -e POSTGRES_USER="$user" \
      -e POSTGRES_PASSWORD="$password" \
      -e POSTGRES_DB="$database" \
      -p "${port}:5432" \
      "$image" >/dev/null
  fi

  print_export_line
}

cmd_down() {
  if docker container inspect "$container_name" >/dev/null 2>&1; then
    echo "Stopping and removing container '$container_name'."
    docker rm -f "$container_name" >/dev/null
  else
    echo "Container '$container_name' does not exist; nothing to do."
  fi
}

case "${1:-}" in
  up) cmd_up ;;
  down) cmd_down ;;
  *) usage ;;
esac
