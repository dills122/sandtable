# Proving Ground local runtime

Status: Development-only infrastructure slice, 2026-10-16. This runbook launches the current
Angular Maproom shell and Orleans host scaffold. It does **not** create a campaign, seed a browser
session, submit a legal order, or prove game persistence. Those capabilities arrive through
[MAP-04–17](../design/local-game-server-maproom-plan.md).

## One-command Docker launch

Requirements: Docker Desktop with Compose v2, enough memory for a .NET 10 and Angular build, and
`just` plus `curl` on the host. From the repository root:

```sh
just proving-ground-up
just proving-ground-smoke
```

Open <http://127.0.0.1:4200> for the current synthetic Maproom shell. The host scaffold is at
<http://127.0.0.1:5180>; `/health` and `/alive` are Development-only health endpoints. `just
proving-ground-up` builds local images, starts both containers, and waits for both HTTP
probes. First build downloads base images, NuGet packages and npm packages. Later builds use Docker
cache. Ports bind to host loopback, not the LAN. No host source directories or secrets are mounted.

```sh
just proving-ground-logs
just proving-ground-down
```

`down` removes the containers and network. There is no campaign save volume in this profile.
Rebuilding or restarting cannot preserve a future in-memory campaign. Docker's local image cache
remains until cleaned with normal Docker commands.
Base images are pinned by digest; Angular dependencies use the committed npm lockfile and .NET
packages use the repository's central version declarations. For retained smoke evidence, record
the Git commit and `docker compose -f compose.proving-ground.yaml images` output alongside probe
results. This preview has no image publication or byte-for-byte build attestation gate.

For live Angular editing, use `just maproom-setup` once and `just maproom` in a terminal. For
Aspire diagnostics or the separate optional intelligence scaffolds, use `just run` in another
terminal. These are development alternatives to the packaged Docker preview; do not run both
profiles simultaneously on overlapping ports. The Docker preview serves a built Angular app and
does not hot reload.

## Process and service matrix

| Component | Docker-now role | Local port | State and next boundary |
| --- | --- | --- | --- |
| Maproom (nginx + built Angular) | Static synthetic operations shell | `127.0.0.1:4200` | No server calls or player data yet. MAP-03A/B/C finish fixture workflows; MAP-10 connects reads. |
| Orleans host (.NET 10) | One Development silo and health endpoints | `127.0.0.1:5180` | No campaign grain/API. MAP-04–09 add contracts, trusted adapter, temporary grain and ingress. |
| `Cna.Core` | Linked into Orleans host image | None | Only authority; current public supported path stops before Combat adjudication. |
| Exercise Runner | Host CLI outside Compose | None | Checked synthetic manifests produce deterministic evidence; not a campaign seed API. |
| Intelligence Gateway + Decision Worker | Optional Aspire `just run` scaffolds | Aspire assigned | Not needed for the intended human rules-lab path; no configured model provider or live dispatch. |
| PostgreSQL / Archives | Absent | None | MAP-14 contract and MAP-15 provider proof precede MAP-16 durable host. No database container implies no save/resume claim. |
| Chronicle / War Diary | Future host modules | None | Chronicle authoritative events; War Diary derived, audience-safe readback. Not separate pods. |

Compose runs **one** Orleans replica in `Development` because current host uses
`UseLocalhostClustering()` and deliberately rejects other environments. This profile is not a
production topology. Maproom and host images can later become separate Kubernetes workloads, but
the deployment gate requires production clustering, durable publication/restore, secret handling,
ingress/auth, probes, and restart tests. Do not translate the Development flag into a Kubernetes
manifest as a shortcut. The intended host shape remains one ASP.NET Core process with ingress,
Orleans and Archives/Chronicle modules; no extra microservices for these domains.

## Seed and prove current rules-lab path

The present seed mechanism is a versioned synthetic manifest for `Cna.ExerciseRunner`. It creates
a **fresh local exercise**, not a running browser campaign. Run from repository root:

```sh
just rules-lab-evidence
```

The runner prints a deterministic fingerprint and validated evidence paths. Use the
[Exercise Runner runbook](exercise-runner.md) to inspect or replay bundles. Generated evidence
stays under ignored `artifacts/`. Do not import a bundle into Orleans or expose its raw state to
Maproom: no trusted browser session/bootstrap contract exists yet.

After MAP-04–09, add an explicit Development-only fixture catalog and seed endpoint. It must bind
immutable rules/setup/content/configuration identities and a declared RNG seed at creation, return
only a server-issued campaign/session identity, and grant a side through the trusted host. MAP-06
must prove Core parity; MAP-07 must declare in-memory loss on restart. MAP-08–13 then establish
authorized reads, retry-safe commands, reconnect and seat isolation. Do not use the existing
Exercise bundle as an HTTP DTO. MAP-17 records the first end-to-end browser playthrough, including
an explicit unsupported Combat stop.

## Checks and failure isolation

```sh
docker compose -f compose.proving-ground.yaml config --quiet
just proving-ground-smoke
just maproom-check
just maproom-browser-check
dotnet build Sandtable.slnx --no-restore
```

`proving-ground-smoke` checks process availability, not gameplay, fog safety or persistence.
`maproom-browser-check` currently tests the synthetic shell and Storybook. When `/health` fails,
inspect `docker compose -f compose.proving-ground.yaml logs orleans-host`; when the browser shell
fails, inspect `... logs maproom` and rebuild with `just proving-ground-up`. A port conflict on
4200 or 5180 means stop the other local process or edit the host-side mapping in the Compose file.
Never change container-side ports without updating the corresponding image configuration.

## Slice dependencies before a real playtest

1. MAP-03A/B/C complete synthetic map, order lifecycle, ledger and diary behavior; MAP-04 freezes
   outward browser contracts in parallel.
2. MAP-05–07 establish trusted Core adapter, temporary campaign grain and bounded System steps.
3. MAP-08–09 expose loopback, seat-scoped reads and retry-safe command ingress; add dev proxy only
   after routes exist.
4. MAP-10–13 connect Maproom, live refresh and private hot-seat handoff.
5. MAP-14–16 prove atomic durable storage and process restart before save/resume is offered.
6. MAP-17 captures exact local playtest commands, accepted receipts and replay fingerprints.

Retain this Compose profile as a convenient smoke environment. Add a real browser-facing
end-to-end gate only when these APIs and workflows exist; passing current probes is not that gate.
