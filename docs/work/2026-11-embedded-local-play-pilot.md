# T5 — Embedded local-play feasibility pilot (2026-11-28)

## Decision

**Embedded viable as a Development-only, API-only engineering pilot; partial as a player product.**
No Postgres or Docker is needed for the single-silo path. Keep this as the candidate local runtime
if the owner accepts a wall-clock deadline policy or funds an explicit pause design. Do not call it
Beta 0-ready. Compose/Postgres is the fallback for a technically skilled tester, not a
non-developer distribution or a solution to seat recovery and human pause UX.

## Design and launch

`LocalPlayPilot` is a narrowly scoped configuration of the existing Host. It reuses the
file-per-campaign SQLite store, existing `EmbeddedDeadlineRearm` lifecycle participant,
`UseTemporaryCampaignDevelopmentSilo` (localhost clustering, disabled Orleans client gateway,
in-memory reminders), and the normal v2 route map. No Core, event, journal, or v2 gate changes.
The default Host still rejects `CampaignJournal:Path` and non-Development still requires AdoNet
Postgres. The flag is intentionally not a new hosting environment.

On a Mac with a .NET 10 SDK and this checkout, build locally:

```sh
dotnet build src/Cna.OrleansHost/Cna.OrleansHost.csproj -c Release
mkdir -m 700 "$HOME/sandtable-local-save"
```

Launch from a local terminal (set `CampaignJournal__OperatorSecret` to a strong random value
retained securely by the operator, for example using a password manager; do not put it in a URL,
command history, or player-facing config):

```sh
export SANDTABLE_LOCAL_PLAY=1
export SANDTABLE_LOCAL_PLAY_DATA="$HOME/sandtable-local-save"
export DOTNET_ENVIRONMENT=Development
export ASPNETCORE_ENVIRONMENT=Development
# Export CampaignJournal__OperatorSecret from your private credential storage before running.
dotnet artifacts/bin/Cna.OrleansHost/release/Cna.OrleansHost.dll
```

Use the printed `http://127.0.0.1:<port>` address. HTTP binds directly to IPv4 loopback with port
0 (OS-selected). Orleans chooses an available loopback port before binding; if it is taken in
between, restart the process. Gateway is disabled. Configured Kestrel endpoints, Postgres hot or Orleans connection strings, and the retired path
setting are mutually exclusive with this mode. Kestrel endpoint configuration reload is disabled
for the pilot.
The directory must exist and be private; the pilot never silently creates a new directory when a
resume path is misspelled. A held `.local-play.lock` exclusive file handle fences cooperative Host
processes until they exit, including hard kill. Do not delete the lock file while a process is
running. Retain the entire directory, including `_host-identity.db`, SQLite sidecars and all
campaign files. Do not copy a live save without a consistent backup protocol.

API-only: no Maproom static build, packaged executable, campaign list, in-browser resume, or
credential persistence product in this spike. This is an internal checkout build that still has
pre-existing embedded content; no package, map, or source-derived content is added or distributed
by this pilot. The probe uses only existing synthetic rules-lab fixtures.

## Reproduce

```sh
dotnet build tools/Cna.LoadHarness/Cna.LoadHarness.csproj -c Release
dotnet artifacts/bin/Cna.LoadHarness/release/Cna.LoadHarness.dll \
  --local-play-probe artifacts/bin/Cna.OrleansHost/release/Cna.OrleansHost.dll
just test-method host '*Embedded*'
```

The probe generates an ephemeral private directory and operator secret, starts the actual built
Host on fresh dynamic ports, issues v2 creation/grant/redeem/open/submit requests, and hard-kills
only its child processes. SQLite is read-only evidence for collecting deadlines and timeout counts;
no save state or event bytes are edited to induce a timeout. The synthetic selection budget is
45 seconds, other budgets one hour; the process stays down until after the recorded deadline.
It compares the operator verification response before and after restart, preserving client-held
credentials in memory across Host lifetimes. It then tests credential reissue, waits offline,
checks timeout count before sending any campaign request, and verifies replay. Failure tests use
only the disposable test directory. It does not log credential or action-reference values.

## Results (macOS arm64, .NET SDK 10.0.400)

Release build: zero warnings and errors. Process probe: exit 0. Existing focused embedded
tests: 4 passed, 0 failed; no Docker. Changed-file formatting check passed. Full solution gate
not run, per spike scope; exact-head CI remains required.

| Item | Result | Evidence / limits |
| --- | --- | --- |
| Single process, no Postgres/Docker | Works | Actual Program.cs Host, explicit Development flag, embedded SQLite, dynamic loopback HTTP, local silo. |
| Both seats, 20+ accepted submissions | Works | 22 accepted owner submissions (11 per seat) on existing synthetic fixture, then a collecting selection round. |
| Crash + restart before deadline | Works | Hard kill, new process; verified fingerprint, event count, and deadline unchanged. |
| Seat credential retention | Works at API level | Each retained credential authenticates after restart without re-redeeming a grant; new grant/redeem increments seat epoch and revokes old credential. |
| Deadline re-arm | Works for tested round | Restart after offline deadline appends exactly one selection timeout before any campaign request; reconstruction verifies. Existing embedded re-arm tests also cover future-deadline arming. |
| Missing and corrupt save | Partial | Missing campaign verification returns 404; missing directory, missing host identity with campaign files, and an unrecognized journal fail startup. Valid SQLite with deeper corruption needs full recovery design. |
| Duplicate process | Works | Second launch while first holds the lock refuses startup. The first process hard kill releases lock automatically. |
| v2 boundary | Works in pilot scope | Development-only, loopback-only and positive same-origin requirement; absent, cross-site and mismatched Origin requests rejected; no existing gate code changed. |
| Non-developer launch | Partial | SDK build plus shell configuration is not a player launcher. Maproom, credential store and signing remain. |

The first probe reached the restart and timeout checks, but its final corruption check timed out
when reusing the original SQLite filename after moving only the main file. A separate fresh-file
corruption repro failed cleanly (SQLite error 26 plus restore guidance). The revised probe passed and moves
the whole test save file family and uses a new corrupt filename so a surviving WAL cannot mask
corruption. An individual missing campaign is not globally discoverable without a save inventory;
if all files are lost in an otherwise empty existing directory, startup cannot distinguish that
from a fresh directory. Do not infer recovery from startup success alone.

## Seat recovery and deadline policy

A v2 client that retains an unexpired bearer credential can reuse it against the new port without
a new grant; its epoch stays unchanged. The journal stores digests and expiry, not recoverable
bearer secrets. A redeemed grant is consumed. Existing lifetimes: grants 10 minutes, credentials
2 hours. If credentials are lost or expired, the operator with the original secret can issue and
redeem a fresh grant for each side; issuing it increments the epoch, clears the previous credential,
and invalidates old seat requests. Rediscover and reopen owner choices after reissue rather than
assuming a cached opening remains valid. The temporary `LocalSessionStore` is not persistent and
not used by this v2 probe. Browser refresh or origin/port change needs a dedicated recovery workflow.
The probe does not establish durable client-side storage, outstanding HTTP acknowledgement recovery,
all-round parity, or browser/Maproom resume.

Today the retained deadline is absolute wall-clock time. Re-arm enumerates seven collecting lanes
and arms timeout grains with the original timestamp. If past due, the existing fallback executes at
boot, even if no player asks for an opening. This is not a paused game; per-lane re-arm failures can
be logged and skipped, so successful startup does not prove every campaign recovered.

Owner policy options (no timing policy changed in this pilot):

- **Wall-clock expiry:** keep current semantics, explicitly disclose before quit/restart, show
  resolved deadlines on resume. Lowest engineering cost; unsuitable for surprise overnight auto-play.
- **Pause elapsed game time:** persist a versioned pause/resume marker and reconstruct deadline
  remaining time under the authoritative timing policy, including hard-crash and clock-change
  semantics. Cannot just mutate deadline rows; those records are bound to authoritative history.
- **Adjudicated resume:** show the operator expired pending windows before committing any fallback;
  an explicit authoritative decision resumes or expires them. Requires a new validated contract and
  timer lifecycle coordination. Current auto-expiry would have to be disabled under that policy.

## Security and productization (macOS first)

No gate relaxation ships. Today `MapDurableCampaignApi` refuses every non-Development environment;
its per-request gate also requires a loopback remote IP and positive same-origin evidence. Pilot
sets an explicit loopback listener and leaves these checks intact. A future release gate proposal
for separate security review: allow the route registration condition `IsDevelopment() ||
explicitlyValidatedLocalPlay`, where `explicitlyValidatedLocalPlay` is true only after configuring
and validating the dedicated loopback listener, disabled remote Orleans gateway, private directory,
and exclusive lock. Keep the request loopback and same-origin checks unchanged; review host-header
validation, origin/port changes, development-only endpoints and local operator bootstrap separately.
Do not simply enable v2 for Production or trust an environment flag alone.

Estimated additional engineering (worker-hours; inference, not measured, excludes content-pack
separation and broader rules/UI work):

| Slice | Hours |
| --- | ---: |
| macOS launcher, fixed save identity/version pin, private operator secret storage, port discovery | 5–9 |
| Same-origin Maproom integration, seat resume/reissue and durable save list | 8–16 |
| Wall-clock disclosure and recovery checklist, user-facing failure handling | 4–8 |
| Backup/restore, deep corruption, crash during commit/ack, duplicate-path and incompatible build checks | 6–12 |
| Security review, clean-machine packaging and signed/notarized distribution checks | 6–12 |
| **Total with current expiry semantics** | **29–57** |

Pause/adjudicated timing policy adds an estimated 12–24 hours plus authoritative contract/replay work;
changes there must wait for owner policy and the hot-file coordination. Release content separation
is independently required before shipping a binary outside owner machines. Choose one runtime for
Beta 0, not two; if the owner cannot commit effort for this additional productization, document and
validate a Compose/Postgres path for technical testers instead.
