# Sandtable

> Command the campaign. Let the Umpire handle the paperwork.

## What is Sandtable?

Sandtable is an in-development digital adaptation of SPI's 1979 board wargame *The Campaign for
North Africa: The Desert War, 1940-43*. The original game models the desert war at an extraordinary
level of detail. Players command Axis or Commonwealth forces, maneuver formations across North
Africa, manage scarce supplies and transport, fight battles, and pursue the victory conditions of
the chosen scenario.

Sandtable aims to preserve those decisions and the character of the original game while asking the
computer to handle the rules, calculations, record-keeping, and hidden information. It is not a
simplified game merely wearing the same theme: the rules target is the original 1979 SPI edition,
corrected by the September 1979 errata, with any necessary interpretations recorded explicitly.

The project uses original software and presentation assets. Scans, rules prose, maps, and counter
art from the published game are not distributed in this repository.

## How will it play?

A campaign is overseen by a digital **Umpire**. Players issue orders; the Umpire checks what is
legal, resolves movement and combat, applies uncertainty, reveals only what each side is allowed to
know, and records what happened.

In the simulated campaign calendar, each turn represents one week and contains three Operation
Stages. That does not mean a turn takes a real-world week to play: local sessions advance as the
players make decisions. Initiative shapes which side acts first or last in each stage, and play
moves through repeated movement and combat segments rather than one simple move-then-fight pass.
Over the course of a scenario, players must balance position, combat power, cohesion, supply,
transport, reinforcements, and the need to meet their own victory conditions.

The first playable release is planned as:

- the six-turn, Land-only **Graziani's Offensive** scenario;
- two-player local hot-seat play through the **Maproom** interface;
- an original schematic map with selectable formations and legal-action guidance;
- save and resume support, strict fog of war, and a complete campaign history; and
- deterministic replay from the same starting seed and accepted orders.

Later releases can add the detailed Air and Logistics Games, longer scenarios, the full 111-turn
campaign, remote multiplayer, and optional AI commanders and narrative. AI is intended to advise or
play a side; it will never decide the rules or secretly change the campaign state.

## Project at a glance

> [!IMPORTANT]
> Sandtable is a tested pre-alpha simulation engine, not yet a playable adaptation of the published
> game. Current scenarios are synthetic rules-laboratory fixtures, not released campaign content.

| Area | Current state | Meaning |
| --- | --- | --- |
| Deterministic Umpire | Working | Versioned rules, seeded randomness, canonical commands/events, replay, checkpoints, and side-safe action boundaries are implemented. |
| Playable rule path | Working through Combat entry | Runner can execute Initiative, stage preamble, Reserve Designation, Movement, bounded ZOC/Reaction, and Breakdown, then stops before Combat adjudication. |
| Combat and continual cycle | Permanent contact fog and normal owner movement merged; native Combat incomplete | Core and normal durable Host now retain contact-safe owner movement, selected assault, defender RBA, Force seals and a shared deadline with cancellation. Later Combat stages, public route and admission remain open. Original authenticated P01 has 44 provisional candidate shapes, 256 absent shapes and zero admitted; its classification does not prove supported public transitions. |
| Exercise and Maneuver tools | Working | Deterministic single runs, multi-run matrices, paired comparisons, strict readback, and evidence bundles are available from CLI. |
| User interface | Angular foundation and bounded connected read mode | `src/Cna.Maproom` contains `maproom-alpha`, Field Atlas and Storybook. The main Maproom route offers a synthetic fixture study and URL-backed `?mode=campaign` local read mode using the same map presentation; `/connected` remains a development deep link. Campaign mode can create one temporary local Rules9 fixture, enter a granted seat and display its initial audience-safe operations view. Reload can resume a valid seat cookie; public orders, later cuts and lost-cookie recovery remain pending. `site/` is project website only. |
| Published scenario | Not started | First target is six-turn, Land-only *Graziani's Offensive* after working Combat loop. |
| Durable save/resume | Not started | Replay/checkpoint contracts exist; user-facing campaign persistence comes later. |
| Model-backed commanders | Scaffold only | Gateway and worker exist, but no provider is configured and AI never owns authority. |

Current boundary in plain language: you can build engine, run full test suite, launch Aspire service
stack, and simulate checked rules-lab Movement/Reaction/Breakdown paths. You cannot yet play a
campaign or resolve Combat through public Runner actions. Combat internals are exercised through
dormant Core tests; they are not yet exposed as playable actions.

The permanent fog implementation has started in Core with
[versioned contact knowledge](docs/specs/combat-contact-knowledge-v1.md): replayed
side contacts, a side-safe view, visibility-qualified target candidates and a
default adjacency rule at creation and accepted ordinary and Reaction movement cuts. Core's campaign
exercise session retains contact records and checks them against authority-event
replay. Core now fences contact knowledge to each accepted authority cut and derives an
acting element's contact view and movement offers from that same cut, without consulting
hidden opposing positions for offer membership. Core now supplies a canonical owner-visible
draft to the trusted Host adapter while retaining its authority fence and action IDs privately.
The normal Host now has an internal [durable authority journal](docs/specs/campaign-durable-authority-journal-v1.md)
that atomically retains accepted Core evidence and verifies it by full Core replay on cold read.
It retains and rechecks canonical contact transitions with each source event.
It now retains one-time side grants and restart-safe credential digests with epoch revocation.
An internal authenticated owner-opening query retains a side-safe visible cut and private
reference-to-Core-action map. It keeps outward bytes and refs stable across unseen authority
changes. Internal contact-derived owner movement now commits once with seat, opening and
reference fences, while an unchanged visible cut remains usable across private authority advances;
cold replay verifies the same path and an exact retry returns the retained
receipt. A first internal native Combat decision now consumes the same current-contact
authority cut. Selected assault retains a private defender binding; voluntary no-attack
retains none, and both record the exact Barrage successor and round-trip through Exercise and
Host import replay. An accepted voluntary finish now records an ordered V13 System no-attack
completion through the five remaining Combat positions to same-slot Reserve Release entry;
Core replay checks every successor and leaves World, contact knowledge and resources unchanged.
The normal Host retains both steps atomically with an internal authenticated Combat opening,
accepted submission and restart-safe receipt. A selected infantry assault now also records a
no-effect V14 Barrage successor to Retreat Before Assault in that same transaction. Core cold
replay recomputes the route from the owner decision; a forged successor is rejected. Defender
Retreat Before Assault candidates now derive from the retained defender contact cut without
using hidden occupancy to suppress offers; a trusted Host draft separates visible bytes
from private action IDs. An explicit defender decline and the first actual retreat move
now pass through the normal seat-bound Host opening, accepted event, retained receipt
and cold replay. Each move updates Core World and both contact audiences, then returns to
the defender decision; repeat moves retain a canonical event and an explicit stop advances
to Force Assignment. Core now derives the next owner's full-current-TOE infantry
allocation candidate from that owner's facts. Core now accepts two
owner-only full-TOE seals at that cut and records V17 events. After both,
a separate V18 System completion advances to Anti Armor. Normal Host binds
each seal to its own seat, opaque reference, atomic evidence and restart-safe
receipt, then retains System completion in the same transaction.
Normal campaign creation can now pin a canonical seven-window timing configuration to
the durable journal head; campaigns without one cannot issue Force Assignment seals.
The configuration supplies no implicit budget. SQLite schema 7 retains one shared
Force Assignment opening and trusted high-water/deadline for both seats. A late or
unavailable-clock callback records Core V19 cancellation with zero or one accepted seal;
verified cold replay rejects changed round evidence. A permanent adjacent-infantry Rules Lab
scenario exercises that normal owner path through restart. Core V20 now records the
infantry-only empty Anti Armor step after either Force completion or cancellation;
normal Host appends it atomically and cold replays it. On a cancelled Force round,
Core V21 closes Close Assault without a draw and reaches same-slot Reserve Release.
Normal Content 6 now resolves a versioned Combat source supplement for formation
morale, offensive ratings, and initial ammunition, bound to the exact pack hash; positive adjudication
must pin that source identity before drawing or settling.
The registered adjacent-infantry assault scenario starts both sides at the
procedure's required 10 TOE; its setup uses normal campaign creation and replay.
Core derives the initial ammunition ledger from that exact source; accepted Combat
settlements will debit it in replay order, so a spent balance cannot be reused.
The normal positive-assault input cut now binds both accepted Force seals to the
content facts, capability budget, and current ammunition before a result draw.
Core now derives a V22 result from that cut and the retained random cursor, recording
each accepted/rejected byte, selected chart facts, and before/after ammunition hashes;
exact replay rejects altered result bytes. The same V22 transition derives both
roles' paid capability points in a retained normal World projection and binds
the prior/paid canonical World hashes for subsequent loss settlement.
The normal post-result loss assessor reuses selected Rules rounding and capture
arithmetic, with defender refusal affecting only its selected loss; retained
retreat choice now feeds a V24 normal World settlement.
The normal 10-TOE pack now carries a separately versioned, exact-pack retreat
direction source. Core derives post-result one-hex defender offers toward its
source anchor from the published map and current side contacts, with explicit
refusal. Core V23 now accepts an offered route or refusal, or a System
not-required result, and cold replay checks the retained choice. Normal Host
now retains the defender opening, accepted choice, receipt, and restart replay.
Core V24 applies source-selected TOE and cohesion losses, charges a completed
retreat, moves the defender's representation, and retains pending captive lots.
Normal Host appends this System event in the same transaction after V23 and
verifies its World and contact changes on cold replay.
Core now derives captor custody route/guard candidates from that accepted World,
an exact current captor contact, and published terrain. Core V25 accepts an
offered disposition into a versioned event, World update, and typed custody
assets; independent replay verifies all three. Normal Host atomically retains
the System no-captive disposition. Captor owner opening, replacement maturation,
and the next normal campaign position remain before outward issuance.
The normal Host appends the source-backed V22 result after both owner Force seals
and Anti Armor, and a fresh journal read replays the exact result and ammunition
spend. Earlier 5-TOE infantry campaigns remain at their prior unissued boundary.
Custody choice after pending capture, non-infantry disposition, and public route
remain unfinished; historical
observation readers remain readable while the complete successor path is built.

The private [actual Reaction-to-Combat entry contract](docs/specs/combat-actual-reaction-combat-entry-v1.md)
and native reader authenticate ordinary outward/return Movement and all three direct Reaction closure
reasons through stop, System resolution, Movement completion and Breakdown into positive Combat entry.
They preserve CP4 and original RNG with separate trusted ledgers and were merged in PR #187.
The private [moved-assault successor](docs/specs/combat-actual-moved-assault-v1.md) carries that actual
entry through Selection, a sealed Round, cumulative paid costs and settlement into same-cycle Reserve
Release entry. It authenticates six independent ledgers and merged in PR #188.
The private [moved continuation](docs/specs/combat-actual-moved-continuation-v1.md)
completes empty Reserve Release for paid and armed parents and derives immutable prospective assessment.
It merged in PR #189 and opens no cycle-control window.
The additive private [moved cycle control](docs/specs/combat-actual-moved-cycle-control-v1.md)
adds eight-ledger control for both modes: real same-slot phase finish or a distinctly typed pending
repeat intent with execution unavailable. Repeat intent retains the current occurrence and history.
N3 merged in PR #190. Its pending intent remains non-executable by itself.
The private [moved repeat Movement successor](docs/specs/combat-actual-moved-repeat-movement-v1.md)
consumes authentic paid and armed pending intents through eight ancestor ledgers and a ninth Movement
ledger. One System publication opens ordinal2; ordinary owner moves retain cumulative CP, apply
breakoff and incremental DP once, and preserve original history and RNG. The original MovementEnd
remains ordinal1 evidence. PR #192 merged at `3a7b626` after independent review and all 3,391 hosted test rows passed.
Post-merge [main CI run 38783352632](https://github.com/dills122/sandtable/actions/runs/38783352632)
verified all 3,391 rows at `3a7b626`, attempt 1. This R01 family remains move-only.

The separate private [repeat Movement completion successor](docs/specs/combat-actual-moved-repeat-movement-completion-v1.md)
(R02) authenticates all 10 independent ledgers. True-zero Movement completes with one owner event;
positive Movement requires owner stop, empty System resolution and owner completion, three events.
It retains both the historical ordinal1 proof and the receipt-bound ordinal2 proof, then enters
Breakdown Determination without executing Breakdown. Local validation passed all 325 rows across 20
fresh class processes; independent implementation review is complete at `07fac9f`.
[PR #194](https://github.com/dills122/sandtable/pull/194) merged at `121baa5`.
[Main CI run 38830848442](https://github.com/dills122/sandtable/actions/runs/38830848442), attempt 1,
verified all 3,716 rows across 26 execution processes. Public Combat,
Runner adoption, acceptance of all 72 criteria and the first release candidate remain open.

Next delivery sequence:

1. Build on CS01's private native [actual post-assault continuation](docs/specs/combat-actual-post-assault-continuation-v1.md)
   and the private [actual repeated Movement reference](docs/specs/combat-actual-repeated-movement-v1.md)
   and [actual completion contract](docs/specs/combat-actual-repeated-movement-completion-v1.md)
   and the [actual Breakdown completion contract](docs/specs/combat-actual-repeated-breakdown-completion-v1.md)
   and the [authentic moved-repeat Breakdown reference](docs/specs/combat-actual-moved-repeat-breakdown-completion-v1.md)
   to continue from C04's private native completion through the private reference Combat-entry boundary into the remaining selected-branch lineage. CS01's full
   paid history and four independent ledgers reach empty Release and guarded repeat/finish;
   native continuation stops at ordinal2 Movement opening; NRM01 consumes the Python contract for up to four safe
   native ordinary moves with cumulative CP/DP and original provenance; C04 completes them with an actual receipt-bound current proof. Complete later-II/consumed Reserve and production
   ingress authentication before activation. Existing synthetic settled contexts do not prove
   reachability from actual opening history.
2. Activate certified, side-safe public Combat actions (Tasks020–021), then prove Exercise/Runner
   replay and repeatability (Tasks022–024).
3. Reconcile all 72 acceptance criteria and demonstrate the authentic continual cycle (Task025).
   Actual publication evidence remains a separate obligation under HOST-PUB-001.
4. Measure and implement the exact six-turn scenario content and remaining Land/victory rules,
   then add durable save/resume and minimal hot-seat Maproom. MVP exit requires two complete,
   reproducible six-turn games.

See the [first-release audit](docs/research/2026-10-05-first-release-audit.md) for the measured release gaps,
and [current roadmap](docs/roadmap/pre-alpha-roadmap.md#current-checkpoint-and-next-gates) for
authoritative status and [Combat plan](docs/design/combat-cycle-implementation-plan.md) for detailed
task graph.

The [local-server and Angular Maproom plan](docs/design/local-game-server-maproom-plan.md) brings
forward a bounded developer playtest lane: Angular/Material/CDK, Tailwind, typed Reactive Forms
and Formly, SignalStore, HTTP and SignalR, with an initial SVG map. The
[Field Atlas design system](docs/design/maproom-design-system-v0.md) and
[operations workspace preview](docs/design/maproom-style-study/index.html) establish warm paper,
ink and restrained unit symbols through a compact map, order workflow and linked formation ledger.
MAP-02 supplies the theme and initial components; MAP-03A supplies the operational map fixture.
MAP-03B/C complete the order and records workflows before expanding the library.
Temporary rules-lab sessions stop at supported
public boundaries; durable hosting and complete scenario play retain their existing gates.

The [Angular workspace](src/Cna.Maproom/README.md) now implements MAP-01, the MAP-02 foundation
and the MAP-03A operational map, MAP-03B order workflow and MAP-03C records fixtures. The
synthetic map supports bounded drag at the selected zoom and a full-screen inspection panel;
its hex overlay keeps a consistent shape across panel sizes;
the larger order inspector and header controls keep order preparation prominent. These remain
local presentation interactions, not connected campaign actions. The
[MAP-04 browser contract](src/Cna.Game.Contracts/README.md) has an OpenAPI schema, audience-safe
fixture and compiling generated client. Local bootstrap, fixture creation and initial-view
handlers exist; public action submission and receipt lookup remain pending.
The [MAP-05 trusted host protocol](docs/specs/maproom-trusted-host-protocol-v1.md) specifies
private accepted-step evidence, pending publication, recovery and bounded System progression;
it does not implement campaign hosting or durable save.
The MAP-06 [Core host adapter](src/Cna.Core/Hosting/CampaignHostAdapter.cs) now offers admitted
creation, coherent side cuts, System legal queries, accepted-step evidence and verified private
transcript replay for the rules lab. Host authorization, journal fencing, outward mapping and
durable publication remain separate work.
MAP-07 adds a [temporary Orleans campaign grain](src/Cna.OrleansHost/TemporaryCampaignGrain.cs)
for the single admitted Rules9 lab fixture. Its trusted in-silo API retains request/receipt
identity and serialized Core transitions in memory, reports bounded System stops, and loses all
campaigns on process restart. Grain deactivation retains live state; a process-only capability
and disabled Orleans client gateway protect its private API. Synchronous memory publication has
no observable pending phase; provider-backed publication still needs fencing and pending recovery.
MAP-08 adds local browser routes and seat authorization around this temporary grain.
MAP-09 binds requests to a seat and retains exact retries privately; public action submission and
receipt lookup, Core terminal classification, public Combat and durable save remain open.
MAP-08A adds development-only, loopback-guarded operator bootstrap and process-local seat-session
plumbing to `Cna.OrleansHost`. A bounded MAP-08B route now creates the one admitted Rules9
fixture through the trusted grain and returns one retained grant pair for exact operator/request
retries. It reserves inactive grant capacity before creation, activates grants only after the
committed reply, and reconciles an uncertain bounded grain call through the same request ID.
The original grant IDs remain bound to that response; an exact retry never renews expired grants.
It omits `configurationHash` because this fixture has no separate configuration pin. The
temporary session store caps admission at 256 live sessions, expires unredeemed operator sessions
after 15 minutes and seat sessions after two hours, and returns `503` when full. Cross-site
navigation cannot mint a replacement operator cookie; bootstrap requires matching `Origin` or
positive same-origin Fetch Metadata. Trusted-host grants are capped at 512 live entries and swept
after expiry. The
`/api/v1/campaigns/{campaignId}/view` now projects an authorized, pinned initial rules-lab cut
after trusted in-process creation. It uses an admitted public layout and opaque formation,
contact, and action handles; private MAP-07 legal-action bytes never cross HTTP. A later cut,
missing presentation fact, or unsupported action still returns `503 unavailable`. Approved private
War Diary copy and classification do not establish a coherent public successor view. Action
submission and complete post-step reads remain absent.
The [MAP-08B1 side projection prerequisite](docs/specs/maproom-side-projection-prerequisites-v1.md)
defines typed permitted fields, pinned presentation data, audience-safe diary and opaque action
mapping that gate the browser view.
MAP-08B2 adds a typed, host-private side cut and process-local visibility tracker. Both sides
advance on creation and every accepted or System step; an unread disappearance retires its
contact handle. Handles are scoped to seat-session generation. The browser route serves only the
initial pinned fixture cut; later cuts await a coherent public successor projection and
validated diary mapping.
The private [`CampaignAudienceOutcomeSource`](src/Cna.Core/Hosting/CampaignAudienceOutcomeSource.cs)
now verifies one accepted step against canonical command/event/receipt and both committed side
cuts without publishing a turn. The temporary host now binds its paired private decisions and
both side cuts to the same exact committed head before installing a successor; missing or
mismatched proof makes private audience reads unavailable. The owner-approved nine-entry en-CA
copy catalog and private tracker retain per-audience monotonic cursors and the latest 50 approved
entries, with outward entry IDs scoped to seat-session generation. The public initial view
still has an empty diary and cursor zero; later cuts remain unavailable. See the
[private outcome head binding](docs/specs/maproom-private-outcome-head-binding-v1.md).
Run `just maproom-setup`, then `just maproom` (port 4200) and `just field-atlas` (port 6006) in separate
terminals. Storybook owns the live component/pattern reference; both surfaces consume
`@sandtable/field-atlas`. MAP-03B stages local delayed results and receipt readback only;
The searchable ledger and War Diary in the fixture study are synthetic own-side examples.
For the development-only initial campaign read in main Maproom, select **Local campaign read mode**
at <http://127.0.0.1:4300/> or open `/?mode=campaign` directly. Follow the
[connected Maproom local recipe](docs/design/maproom-connected-local-read.md); it uses ports 4300
and 5087 without changing the 4200 preview. `/connected` remains a development deep link.
MAP-10 extends the bounded read-only operations
workspace but does not submit orders or read later cuts. MAP-09 retains private, process-lifetime
request binding and exact retries without a public submit or receipt route. A process holds only
one fixture campaign: exact creation retries need the original operator/request within 15 minutes.
A reload with the same valid seat cookie resumes its authorized view; a lost cookie or different request conflicts until
that development host restarts. Command submission and later views remain open.

## Start, run, and develop

### Prerequisites

- [.NET SDK 10.0.302 or later .NET 10 feature band](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Just](https://just.systems/) for short commands (optional)
- Git
- Docker only for future container-backed Aspire resources; current stack does not require it

`global.json` selects .NET 10 and Microsoft.Testing.Platform. Check installed SDK with
`dotnet --version`. Use `dotnet test --solution` for the solution and `--project` for a test project;
do not pass a bare positional test path.

### First checkout

```sh
git clone https://github.com/dills122/sandtable.git
cd sandtable
just setup
just check
```

Without Just:

```sh
dotnet restore Sandtable.slnx
dotnet build Sandtable.slnx --no-restore
dotnet test --solution Sandtable.slnx --no-build
```

### Optional Ruflo MCP for Codex

Ruflo runs as a project-local stdio MCP server. After installing Ruflo, initialize its runtime
in each checkout and add a project-scoped `.codex/config.toml` with a host-local command:

```toml
[mcp_servers.ruflo]
command = "npx"
args = ["-y", "ruflo@latest", "mcp", "start"]
startup_timeout_sec = 30
```

Run `npx ruflo@latest init --minimal --skip-claude --no-mods --no-codex-detect --no-skills-sh --no-global --no-signup` once in that checkout, then restart Codex. Use `codex mcp get ruflo` to check registration. Local `.claude-flow/`, `.swarm/`, `ruvector.db*`, and `.codex/config.toml` stay out of Git because they can contain checkout-specific state and paths.

### Run service stack

```sh
just run
```

Equivalent command:

```sh
dotnet run --project src/Cna.AppHost/Cna.AppHost.csproj
```

Open Aspire dashboard URL printed in terminal. It shows Orleans host, Decision Worker, and
Intelligence Gateway. This launches development services—not a playable Maproom. Stop with
<kbd>Ctrl</kbd>+<kbd>C</kbd>.

### Launch containerized Proving Ground preview

With Docker Desktop running, `just proving-ground-up` builds and starts the current Angular
Maproom shell at <http://127.0.0.1:4200> and Development Orleans host at
<http://127.0.0.1:5180>. `just proving-ground-smoke` checks both HTTP probes;
`just proving-ground-down` stops them. The 4200 shell uses synthetic fixtures. The host maps
bounded local campaign routes, but Docker forwarding to port 5180 fails their loopback-caller guard;
use the host-native [connected read mode](docs/design/maproom-connected-local-read.md)
for an authorized initial view. Neither lane submits orders or provides durable save.
Use the [Proving Ground runbook](docs/runbooks/proving-ground.md) for service boundaries, current
rules-lab seed/evidence commands, and the MAP slice path to a real playtest.

### Run deterministic simulation

Run current bounded Reaction Maneuver and write validated artifacts under `artifacts/exercises`:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.reaction.serial.breakdown.v1.json \
  --artifact-root artifacts/exercises
```

Runner prints child bundle paths, aggregate report path, and deterministic fingerprint. More
checked manifests and diagnostic modes are listed in [Exercise Runner runbook](docs/runbooks/exercise-runner.md).

### Preview project website

```sh
python3 -m http.server 4173
```

Open `http://localhost:4173/site/`. Website is dependency-free project documentation, not game UI.

### Daily development loop

```sh
git switch -c codex/my-change
just check
```

Use feature branch; never commit directly to `main`. Read [contributor guide](CONTRIBUTING.md),
[architecture](tech-design.md), and [vocabulary](naming-overview.md) before changing boundaries.
Update contracts before consumers, add focused deterministic test before behavior, keep remote/model
I/O outside authoritative turns, then run `just check` before PR.

The [optional OCR scrutiny runbook](docs/runbooks/optional-code-review.md) describes a paid,
label-triggered PR-review pilot for high-risk changes. Configure its Sandtable Actions secret
before applying the label; ordinary CI and independent review remain the merge gates.

| Command | Purpose |
| --- | --- |
| `just --list` | Show repository recipes |
| `just setup` | Check SDK and restore dependencies |
| `just build` | Restore and build solution |
| `just test` | Build and run all tests |
| `just boundary-check` | Run user-space disclosure boundary suite |
| `just format-check` | Verify formatting without edits |
| `just check` | Full local gate: format, build, boundary tests, all tests |
| `just run` | Launch Aspire development stack |
| `just proving-ground-up` | Build and launch Docker Maproom shell plus Development Orleans host |
| `just proving-ground-smoke` | Check both local HTTP probes |
| `just proving-ground-down` | Stop the Docker preview |
| `just rules-lab-evidence` | Run checked synthetic Reaction Maneuver through trusted CLI |
| `just docs-links` | Check tracked Markdown links; requires Lychee 0.24.2 |

`just check` runs the .NET gates; it does not run the retained Python Combat contract oracles.
Some of those oracles currently fail: Breakdown and cycle-sequence source pins have drifted,
recursive Snapshot admission rejects that drift, and outward composition has a separate Content
pin failure. Pin maintenance is deferred; other bounded oracle timeouts remain unverified. See the
[verification inventory](docs/research/combat-verification-pin-maintenance.md) before interpreting
historical passing checks as current evidence. Record failures separately from any supplemental
semantic checks.

Build artifacts live under `artifacts/`. Do not commit generated `artifacts/bin` or `artifacts/obj`
content. See [security policy](SECURITY.md) for vulnerability reports.

CI separates expensive tests into eight required long-running suites, alongside four ordinary Core
suites, the three R02 suites, Exercise and Contracts. The matrix permits all 17 suites to run in
parallel, subject to shared GitHub runner availability. Formatting is a separate required job that
runs alongside preparation; aggregate verification requires both formatting and every test suite.
All test jobs retain a 15-minute deadline. The delivery target is a complete required pipeline under
ten minutes, with five to six minutes as a stretch; hosted performance validation is still pending.

The prepared inventory assigns every actual test row exactly once. Fifty-two groups split costly
legacy classes across fresh processes, while ordinary Core classes remain whole in four processes.
Together with 20 R02 processes and Exercise/Contracts, the baseline has 78 execution processes and
3,716 test rows. Long suites permit at most four concurrent child processes. R02 retains its existing
resource limits and the focused support class runs alone after job1's parallel children drain.
Source revision, attempt, SDK, binary and fixture fingerprints, actual selectors, process exits and
XML identities remain part of the required proof. Missing, duplicate or unexpected rows fail the gate.

The historical [Core-B investigation](docs/research/2026-10-13-rmc-core-b-investigation.md) and
[earlier capacity split](docs/research/2026-10-14-ci-core-capacity-proposal.md) explain the preceding
layout; their timings do not establish this layout's performance. The
[implementation checkpoint](docs/work/2026-10-16-ci-long-suite-isolation.md) records the new design
and focused local measurements; [manifest maintenance](.github/scripts/long-suite-weights.md)
explains how test additions update the exhaustive allocation.

## Architecture and documentation

The [P01 ordinal-two Combat Position contract design](docs/specs/combat-actual-moved-repeat-position-v1.md)
records a provisional 44-candidate/256-empty R03 partition and its acceptance gates. The private
reference and bounded proof slices are merged, but all 300 assessments remain unsupported with
zero admitted actions. The [selected-assault successor](docs/specs/combat-p01-selected-assault-v1.md)
classifies all 300 authentic roots but exercises bounded transitions on two representative positive
roots, one per acting side. The [Force Assignment successor](docs/specs/combat-p01-force-assignment-v1.md)
continues those bounded histories to Prepared or cancellation; neither slice certifies all 44
transitions. The [private paid-commit reference](docs/specs/combat-p01-paid-commit-v1.md) separately
executes its preflight and one atomic cost event on all 44 positive roots, with 256 candidate-absent
and zero admitted or public actions. It does not roll a result, settle a World or provide a native
consumer. A [private owner projection draft](docs/specs/combat-p01-owner-projection-v0.md) models a
separate visible alias and Umpire mapping. The private owner decision codec's
visible fields are approved, and an internal pure Core codec matches its six synthetic
vectors; authenticated owner openings and equal-visible hidden-variant privacy proof
remain open. P02 selection, P03 native consumption
and public activation remain gated.
The [private local host foundation](docs/specs/p01-private-host-foundation-v1.md) adds
an unregistered SQLite grant/activation-fence/probe store for Choice L tests. A separate
[private A1 physical import](docs/specs/p01-a1-physical-host-import-v1.md) binds one
validated R03 cut to an unissuable campaign row; it has no owner opening,
game-event journal, host route, receipt API or admission.
The [private no-attack v0 boundary](docs/specs/combat-p01-no-attack-v0.md) fixes a proposed
twelfth-ledger input shape but issues no event: no authentic supported-empty certificate exists.
The separate [private explicit-RBA successor](docs/specs/combat-p01-explicit-rba-v1.md) proves
zero **assault** candidates for authenticated nonadjacent infantry while retaining the defender's
RBA choice. Its decline/cancellation and six structural steps have no native or public consumer;
the full P01 support certificate remains open.
Accepted `CMB-POL-009` excludes Land §15.88 voluntary surrender from the initial RC
bounded choice surface; this remains an explicit source-legal coverage limit.
The [private automatic-surrender Core source reader](docs/specs/combat-auto-surrender-core-reader-v1.md)
accepts only frozen Content8/Rules/Setup8 bytes. Creation and eleven authenticated R03 predecessor
ledgers for that asymmetric source still need a separate authority path; no surrender is issued.
The [private pre-issue source seal](docs/specs/combat-auto-surrender-preissue-source-v1.md)
retains those exact bytes in a canonical typed envelope; it creates no campaign or ledger.

> Command decides. Staff plans. Dispatch carries. Umpire adjudicates. Chronicle remembers.
> Maproom shows.

The pure, in-process `Cna.Core` owns authoritative simulation. ExerciseRunner uses its legal-action
and replay path for local developer instrumentation. AppHost launches Orleans host, Decision
Worker and Intelligence Gateway scaffolds; campaign hosting and live decision dispatch are future
integration work. Gateway decision/narrative RPCs currently return `Unavailable`.

- [Documentation index](docs/README.md): capability specifications, designs, research and reviews
- [Technical design](tech-design.md): architecture, authority boundaries and integration rationale
- [Naming and vocabulary](naming-overview.md): product names and module responsibilities
- [Contributor guide](CONTRIBUTING.md): branch, verification and PR workflow
- [Historical implementation ledger](docs/research/2026-10-05-project-documentation-snapshot.md): retained checkpoint evidence

## License

No license has been selected yet. All rights are reserved until a license file is added.
