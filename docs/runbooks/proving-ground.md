# Proving Ground local runtime

Status: Development-only local runtime. This runbook launches the synthetic Angular Maproom shell
and an Orleans host with bounded operator bootstrap, single-fixture creation, seat and initial-view
routes. The separate connected Maproom development lane can read that one initial cut. Neither
surface submits a legal order, reads a successor cut, or proves game persistence. Those capabilities
remain in [MAP-09–17](../design/local-game-server-maproom-plan.md).

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
| Maproom (nginx + built Angular) | Static synthetic operations shell | `127.0.0.1:4200` | Fixture workflows only; the separate `/connected` development lane reads one initial authorized cut. MAP-10 adds full workspace reads. |
| Orleans host (.NET 10) | One Development silo, temporary grain and bounded local API | `127.0.0.1:5180` | Operator bootstrap, fixture creation, seat and initial-view routes exist. MAP-09 private action ingress exists; public submit/receipt and successor views remain open. |
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

For CLI evidence, a versioned synthetic manifest feeds `Cna.ExerciseRunner`. It creates a
**fresh local exercise**, not a running browser campaign. Run from repository root:

```sh
just rules-lab-evidence
```

The runner prints a deterministic fingerprint and validated evidence paths. Use the
[Exercise Runner runbook](exercise-runner.md) to inspect or replay bundles. Generated evidence
stays under ignored `artifacts/`. Do not import a bundle into Orleans or expose its raw state to
Maproom: this CLI evidence is not a trusted browser seed; use the bounded Development fixture
creation route and seat grant for the separate connected initial read.

The Development-only creation route now admits one pinned Rules9 fixture and returns a retained
grant pair for an exact operator/request retry. MAP-06 proves the bounded Core adapter; MAP-07
declares in-memory loss on restart. MAP-08 adds the initial authorized read; MAP-09 private
request binding does not expose public commands. MAP-10–13 still need connected successor reads,
orders, reconnect and hot-seat isolation. Do not use the existing
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
3. MAP-08 exposes local creation, seat-scoped initial reads and a separate loopback dev proxy;
   MAP-09 has private retry-safe command ingress but no public submit or receipt route yet.
4. MAP-10–13 connect Maproom, live refresh and private hot-seat handoff.
5. MAP-14–16 prove atomic durable storage and process restart before save/resume is offered.
6. MAP-17 captures exact local playtest commands, accepted receipts and replay fingerprints.

Retain this Compose profile as a convenient smoke environment. Add a real browser-facing
end-to-end gate only when these APIs and workflows exist; passing current probes is not that gate.
