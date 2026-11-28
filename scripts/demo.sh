#!/usr/bin/env bash
# Disposable, loopback-only Development demo. Ctrl+C cleans up this run's processes and database.
set -euo pipefail
cd "$(dirname "$0")/.."
if [[ "${1:-}" == "--help" ]]; then
  echo "Usage: scripts/demo.sh (requires .NET 10, Node 22, npm 10, Docker, curl, Python 3)"
  exit 0
fi
if [[ "$#" != 0 ]]; then echo "Unexpected arguments (see --help)." >&2; exit 2; fi
for tool in docker dotnet node npm curl python3; do
  command -v "$tool" >/dev/null || { echo "Missing prerequisite: $tool" >&2; exit 1; }
done
docker info >/dev/null 2>&1 || { echo "Docker daemon unavailable. Start Docker Desktop (or Docker Engine), then retry." >&2; exit 1; }
python3 - <<'PY'
import socket
for port in (5087, 4300):
    with socket.socket() as listener:
        try:
            listener.bind(("127.0.0.1", port))
        except OSError:
            raise SystemExit(f"Port {port} is busy; stop its existing service before starting demo.")
PY
dotnet build src/Cna.OrleansHost/Cna.OrleansHost.csproj -c Release
dotnet build tools/Cna.LoadHarness/Cna.LoadHarness.csproj -c Release
(cd src/Cna.Maproom && npm ci)
db_id=""
host_pid=""
ui_pid=""
cleanup() {
  if [[ -n "$ui_pid" ]]; then kill "$ui_pid" 2>/dev/null || true; fi
  if [[ -n "$host_pid" ]]; then kill "$host_pid" 2>/dev/null || true; fi
  if [[ -n "$db_id" ]]; then docker rm -f "$db_id" >/dev/null 2>&1 || true; fi
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
operator_secret=$(python3 -c 'import secrets; print(secrets.token_hex(32))')
db_password=$(python3 -c 'import secrets; print(secrets.token_hex(24))')
labels=()
if [[ -n "${AO_SESSION_ID:-}" ]]; then labels+=(--label "ao.session=$AO_SESSION_ID"); fi
db_id=$(docker run --rm -d ${labels[@]+"${labels[@]}"} -e POSTGRES_PASSWORD="$db_password" \
  -p 127.0.0.1::5432 postgres:16-alpine)
db_port=$(docker port "$db_id" 5432/tcp | cut -d: -f2)
for attempt in $(seq 1 60); do
  if docker exec "$db_id" pg_isready -U postgres -h 127.0.0.1 >/dev/null 2>&1; then break; fi
  if [[ "$attempt" == 60 ]]; then echo "Postgres did not become ready." >&2; exit 1; fi
  sleep 1
done
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://127.0.0.1:5087 \
  CampaignJournal__EnableOutwardApi=true CampaignJournal__OperatorSecret="$operator_secret" \
  SANDTABLE_POSTGRES_HOT_CONNECTION_STRING="Host=127.0.0.1;Port=$db_port;Username=postgres;Password=$db_password;Database=postgres" \
  dotnet artifacts/bin/Cna.OrleansHost/release/Cna.OrleansHost.dll &
host_pid=$!
for attempt in $(seq 1 90); do
  kill -0 "$host_pid" 2>/dev/null || { echo "Host exited during startup." >&2; exit 1; }
  if curl -fsS -H 'Sec-Fetch-Site: same-origin' http://127.0.0.1:5087/api/v2/campaigns/opening-kinds >/dev/null 2>&1; then break; fi
  if [[ "$attempt" == 90 ]]; then echo "Development API did not become ready." >&2; exit 1; fi
  sleep 1
done
# Read the two JSON lines privately in the terminal and paste into hot-seat setup.
printf '\nFresh campaign seat credentials (keep private; do not record this terminal):\n'
SANDTABLE_LOAD_OPERATOR_SECRET="$operator_secret" \
  dotnet artifacts/bin/Cna.LoadHarness/release/Cna.LoadHarness.dll --prepare-demo
printf '\nOpen http://127.0.0.1:4300/hot-seat after Maproom finishes compiling. Ctrl+C stops the demo.\n'
(
  cd src/Cna.Maproom
  exec node node_modules/@angular/cli/bin/ng.js serve maproom-alpha --host 127.0.0.1 --port 4300 --proxy-config proxy.connected.json
) &
ui_pid=$!
while kill -0 "$host_pid" 2>/dev/null && kill -0 "$ui_pid" 2>/dev/null; do sleep 1; done
echo "A demo process stopped; cleaning up." >&2
exit 1
