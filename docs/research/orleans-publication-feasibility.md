# HOST-RSH-001: campaign publication and recovery

Status: bounded research complete at input `6852088`; owner decides production contract/storage.
Three primary files: this packet, [project](probes/orleans-publication/HostProbe.csproj),
[probe](probes/orleans-publication/Program.cs). No shared authority registration or host changes.

## Experiment contract, fixed before execution

One current Rules9 synthetic Truck campaign, seed12345. Real isolated Orleans silo; injected
memory storage implements whole-record compare-and-swap and explicit failure points. Same-process
reactivation is not process durability. Compare complete canonical creation/event/receipt/snapshot
bytes and all three audiences' legal sets with direct Core. No model calls or wall-clock game input.

Private probe-only schema1 envelope: pinned creation request and its canonical event/snapshot,
ordered entries containing request ID, trusted actor, canonical submission/event/receipt/snapshot.
Campaign grain key must match creation identity. An entry is published with its receipt and checkpoint
in one conditional record write before acknowledgment. Exact actor/request-ID/command retries return
the stored receipt; changed reuse rejects. Domain-stale requests with fresh IDs remain rejected.

Creation/query/submission use public CampaignExercises APIs. Restore recreates the session and
re-adjudicates every retained command, comparing every stored canonical output; raw ReadCheckpoint
only reads metadata and cannot restore authority. Retained records are trusted storage, not an
attacker-authentication mechanism. Production event replay/import remains a separate contract gap.

Required cases: direct/host parity, duplicate and concurrent retry, changed payload/actor, stale
submission, failure before store, store success followed by storage error, application reply loss
after publication, fresh activation identity with identical restored evidence. Stop at first-side
Combat entry or report the bounded experiment inconclusive; no Combat adjudication claim.

## Decision proposal

**Recommendation (inference):** use a versioned per-campaign commit batch as the durable publication
unit. Atomically retain the accepted command identity, authoritative event(s), receipt and new journal
head under an expected prior head. A checkpoint is a verified, reconstructable cache of that history.
Start provider evaluation with this contract; do not select a database from this memory-store result.
The tested whole-record aggregate is a useful bounded baseline, not an unbounded campaign layout.

Owner acceptance is still required before production host/storage implementation. Current contract progression is
remaining003D2c.2b–d →003D2c.3–4 →004 → checkpoint B; see the [combined plan](../design/combat-cycle-implementation-plan.md). Production host timing remains the accepted020–021 public Core
activation plus one verified023 trace and accepted host contract/storage decision;024 remains required
for Combat closeout. This research does not activate Combat or complete campaign lifecycle/Maproom.

## Sources and existing seams

Sources consulted2026-09-07; library API checked by compilation against the repository's pinned
Microsoft.Orleans.Server10.3.1. Current online documentation is supporting rationale, not evidence
that an untested provider meets the proposed guarantees.

- **Documented fact:** persistent state loads at activation, writes are explicit, write failures
  surface as failed operations, and provider ETags can enforce conditional writes. Separate named
  state objects do not establish an application transaction. [Microsoft: persistence](https://learn.microsoft.com/en-us/dotnet/orleans/grains/grain-persistence/).
- **Documented fact:** retrying can deliver a request more than once; application-level durable
  deduplication is necessary. [Microsoft: delivery guarantees](https://learn.microsoft.com/en-us/dotnet/orleans/implementation/messaging-delivery-guarantees).
- **Documented fact:** default non-reentrant grains serialize requests; interleaving choices change
  that behavior. Keep campaign command methods non-reentrant. This does not replace storage conflict
  checks. [Microsoft: scheduling](https://learn.microsoft.com/en-us/dotnet/orleans/grains/request-scheduling).
- **Documented fact:** Orleans supports hosted grain tests. This probe builds a real local silo with
  HostBuilder and the existing Server package rather than adding a testing package or mocking Grain.
  [Microsoft: grain testing](https://learn.microsoft.com/en-us/dotnet/orleans/implementation/testing).

| Repository observation | Consequence |
| --- | --- |
| [OrleansHost](../../src/Cna.OrleansHost/Program.cs) configures a Development-only silo, basic endpoint and no campaign grain/storage. | Probe stays in its own executable, outside the solution and AppHost. Production scaffold remains unchanged. |
| [CampaignAuthority](../../src/Cna.Core/Campaigns/CampaignAuthority.cs) creates an opaque handle; [legal actions](../../src/Cna.Core/Actions/CampaignLegalActions.cs) query and submit it. Submission returns successor/receipt, not event/checkpoint bytes. | This seam alone cannot implement Chronicle publication. |
| [CampaignExercises](../../src/Cna.Core/Exercises/CampaignExercises.cs) exposes creation and accepted-step canonical bytes through the same creation/adjudication implementation. | Suitable trusted experiment seam, without reflection, friend assembly access or new authority registration. |
| [ReadCheckpoint](../../src/Cna.Core/Exercises/ExerciseCheckpoint.cs) returns metadata; Reconstruct requires an existing opaque ExerciseSession. | No public import of retained bytes into executable authority. Probe re-adjudicates creation + commands and compares every result. Production needs a trusted replay/restore adapter and provenance contract. |
| Setup catalog is internal; Runner consumes explicit creation fields. [Existing fixture input](../../tests/Cna.ExerciseRunner.Tests/Artifacts/ExerciseManifestCodecTests.cs) retains admitted identity values. | Pin known setup/content values and Rules9 hash; do not expose internals or discover configuration by reflection. |

## Observations and failure matrix

The executable uses one fixed campaign and follows ActFirst, no Reserve designation, complete Movement,
then empty Breakdown completion. This is a hosting trace through the existing Rules9 path, not a
positive movement/Breakdown outcome study or Combat simulation. Full canonical bytes are compared,
not only state-version counters. Every hosted successor is also checked after a new activation GUID.

| Case | Observed outcome |
| --- | --- |
| Creation, including exact retry and changed creation-byte retry | One initial write; identical initial event/snapshot; changed bytes rejected. |
| Direct Core versus hosted execution | 12 accepted commands, matching every event/receipt/snapshot and all39 audience legal-set queries through terminal. |
| Two concurrent identical submissions at each step | Same stored receipt; one accepted entry per step. Retry lookup precedes domain staleness. |
| Changed request-ID payload, mismatched actor, stale command with fresh ID | Rejected; no extra stored entry or change to canonical authority record. |
| Injected failure before first command write | Call failed; prior complete record retained; retry accepted once. |
| Store commits second command, then throws before returning acknowledgment | Call failed; explicit re-read recovered committed record; retry returned its existing receipt without another write. |
| Third command publishes, then throws TimeoutException before returning receipt | Call failed; retry returned published receipt; no extra event/cost/RNG advancement. This simulates application reply loss, not a dropped network packet. |
| Deactivate after every accepted command, then call again | 12 different activation GUIDs; all stored bytes and retry receipts unchanged. |
| Independent Core event reconstruction of final direct session | Verified; hosted bytes matched that direct history. |

The storage bridge wrapped the injected IOException in OrleansException. Initial failure assertions
expected an unwrapped exception; corrected assertions require the observed wrapper and inner cause.
The provider serializes copies to a dictionary, holds a lock for each compare-and-swap, and injects
Before/After failures at specified writes. Those semantics are part of the test double, not a
measurement of a database. There are13 successful record writes total: creation +12 commands.

## Proposed production contract

These requirements are a proposal, not a frozen schema or transport registration:

1. **Creation binding:** authenticated campaign ownership plus versioned creation request, ruleset,
   setup/content hashes, scenario, seed/RNG algorithm and applicable configuration hash. Current
   Rules9 input has no separate Combat configuration field; do not invent a placeholder value.
2. **Accepted batch:** campaign identity, prior/resulting authority version and journal head,
   original authenticated principal/side, request ID, canonical command digest, exact accepted
   event bytes and receipt, schema versions and optional verified checkpoint. Bind immutable
   source/config identity from creation; preserve it across replay and upgrades.
3. **Acknowledgment:** return success only after conditional durable commit. If commit outcome is
   unknown, re-read the authoritative head/receipt; if recovery cannot establish truth, remain
   unavailable/deactivate. Never reuse a possibly advanced in-memory successor as persisted truth.
4. **Retries:** same identity and exact bytes return the original receipt, even after later commands;
   changed identity reuse conflicts. Recheck authorization before receipt lookup. Define retention
   and tombstone policy before deleting deduplication evidence. No claim of exactly-once transport.
5. **Audience mapping:** authenticated ingress maps principal to campaign/side; only internal service
   identity may request System actions. Caller-supplied enum values are not authentication. Outward
   query/reply uses existing side-safe views; trusted Inspect/events/snapshots remain private. A
   receipt's raw authority revision is not automatically suitable for future sealed-round views.
6. **Recovery/publication:** Chronicle event history is authoritative. Validate sequence/head,
   creation/source identity and checkpoint binding, then replay the suffix; reject inconsistent
   evidence. Commit any required outbound delivery marker with the batch, then deliver off-turn
   with idempotent consumers. No model/remote inference inside an authoritative grain turn.

## Alternatives

| Approach | Strength | Failure/cost and next evidence |
| --- | --- | --- |
| Whole atomic campaign record containing history, receipts and checkpoint | Tested here with one CAS write; no split-save window, easy small-campaign recovery. | Rewrites accumulated history; bounded size and throughput need measurement. A checkpoint alone would lose authoritative history/receipt evidence. |
| Append-only journal with transactional batch/head/receipt, derived checkpoints | Recommended production target: retains Chronicle authority without rewriting all history. Checkpoint lag is recoverable. | Requires actual provider conditional append/transaction semantics, trusted event import and batch/head reconciliation tests; not implemented or benchmarked here. |
| Independently save event, receipt and snapshot | Superficially small writes. | Crash/ack gaps can publish incompatible records. Do not adopt without an explicit commit marker/reconciliation protocol and corresponding failure tests. |

A transaction can implement the recommended batch in relational storage, or a conditional record
can implement it in another provider; clustering storage is a separate choice. No provider wins
from this experiment. A strict bounded-campaign requirement plus measured record size/latency could
justify the whole-record approach; a failed journal atomicity test would require a different store
or revised protocol, not reliance on Orleans scheduling alone.

## Remaining bounded implementation gates

After owner accepts the direction, propose contract-first children:

- Specify one versioned commit-batch/receipt/checkpoint contract, audience mapping and failure/retry
  behavior, including retention bounds and upgrade policy.
- Add a trusted Core accepted-step export and event-history/checkpoint restore adapter; prove parity
  with creation/replay and reject mismatched source, tampered events and incorrect checkpoints.
- Evaluate one selected provider against that contract: failure before/after commit, actual process
  kill/restart, stale ETag/fencing conflict, read failure, checkpoint lag, duplicate receipt and
  outbound delivery recovery. Measure payload size and recovery work under explicit bounds.
- Only then implement the bounded campaign grain/ingress and re-run Core-versus-host evidence,
  including the accepted Combat trace. Pending model decisions remain a separate increment.

**Confidence:** high for the executed same-process, single-silo cases; moderate for the publication
recommendation; unresolved for durable provider behavior, multi-silo fencing, storage read failures,
corrupted stored records, principal authentication, positive Combat, latency and scaling. Probe's
Restore checks do not constitute an adversarial storage test. It retains trusted memory records and
uses no provider crash recovery. No production security or independent-review verdict is claimed.

## Reproduction and retained evidence

Executed2026-09-07 on macOS26.6.2 (25G83), arm64, .NET SDK10.0.400/runtime10.0.11,
Orleans10.3.1, Debug, source input `6852088` plus the two probe files identified below. SDK roll-forward
follows global.json. No solution, shared package, src, scenario or frozen Combat artifact changed.
No full .NET suite or fresh simulator study is claimed; the focused executable is the experiment.

From repository root (port19111 must be free; local gateway disabled):

```sh
dotnet restore docs/research/probes/orleans-publication/HostProbe.csproj '/bl:/tmp/host-probe-restore-{}.binlog'
dotnet build docs/research/probes/orleans-publication/HostProbe.csproj --no-restore '/bl:/tmp/host-probe-build-{}.binlog'
dotnet run --project docs/research/probes/orleans-publication/HostProbe.csproj --no-build
dotnet format docs/research/probes/orleans-publication/HostProbe.csproj --verify-no-changes --no-restore --include docs/research/probes/orleans-publication/Program.cs
```

The probe exits nonzero on a failed assertion and prints the complete deterministic input identity,
trace and outcome hashes on success. All APIs invoked through the grain proxy use real Orleans
activation and persistence plumbing. Its private transport and JSON record are experimental, not
registered production wire contracts or an untrusted-ingress codec.

Observed output:

```json
{
  "status": "PASS",
  "commands": 12,
  "writes": 13,
  "terminal": "land.position.operation-1.first-player.movement-and-combat.combat.position-determination",
  "StateVersion": 13,
  "rulesetHash": "17f3e6047f34b5bf6f5f809055863b664a4bd82a8481db83ee83e0ae5cad3a2a",
  "SetupId": "rules-lab.breakdown.truck.v1",
  "SetupHash": "sha256:e6631e81ad8f97e39fd9d7eec93bad7fe2b39db4d2d3059ed94a02dd4093e7a3",
  "ContentPackId": "rules-lab.content.breakdown-truck.v1",
  "ContentHash": "sha256:646e76e69ecceb82216b37d84e950928099acd8a3cb04b51526d0fe631e512ee",
  "ScenarioId": "breakdown-truck-lab",
  "finalSnapshotHash": "sha256:222b36760b250ab6ea8eb14a7913ffb56bab1b1786cb7a9d9b1f720360ca3c84",
  "recordHash": "sha256:12eb59912e12a86891e2c3a9ce4b9bc7b881f6f1f3f5858e3a2bee410a390a83",
  "recordBytes": 179260,
  "trace": [
    "resolve-initiative",
    "resolve-no-obligation-naval-convoy-schedule",
    "resolve-no-obligation-tactical-shipping",
    "act-first",
    "resolve-weather",
    "resolve-no-obligation-organization",
    "resolve-no-obligation-naval-convoy-arrival",
    "resolve-no-obligation-fleet-assignment",
    "resolve-no-obligation-fleet-repair",
    "complete-reserve-designation",
    "complete-movement-segment",
    "complete-breakdown-segment"
  ],
  "limits": "one silo; injected memory CAS; no durable process restart"
}
```

The179260-byte envelope includes hex-encoded canonical evidence and every intermediate snapshot;
it is a probe representation size, not a production storage estimate. No latency comparison was run.

Local execution evidence: `/tmp/host-probe-red.log` records the deliberate NotImplementedException;
`/tmp/host-probe-green.log` retains the initially unexpected storage wrapper;
`/tmp/host-probe-green2.log` records the first passing run;
`/tmp/host-probe-final.log` records final pinned-source/trace output.
Final build succeeded with zero warnings/errors; binlog
`/tmp/host-probe-build-20260907-172926--17878--Sy9Dv3.binlog` exists.
Sandbox-only restore and formatter IPC failures were retried with the required environment access;
these were tooling failures, not failed hosting invariants.

Probe source SHA-256:

- `docs/research/probes/orleans-publication/HostProbe.csproj`: `2c00ed853a215bd00728c2a021a28f12eaa56efaa66b9f184283fbc066245a6c`
- `docs/research/probes/orleans-publication/Program.cs`: `66286f8c28b320c7cdd747a91047c6d95f350d8361f40d5b88b1633adf0a39ad`

Author review checked conditional publication, failure recovery, duplicate-before-stale ordering,
Core authority boundaries, bounded history and explicit provider/authentication limits. Focused
format verification and `git diff --check` pass. Navigation check:520 local links,9 anchors;
25 stable task IDs/dependencies,72 unchanged design acceptance criteria and8 policy IDs. No new
independent review round was run. At this historical experiment checkpoint, next work was003C3c. Current progress and next gates
are maintained in the [combined plan](../design/combat-cycle-implementation-plan.md); production host
deployment remains gated.
