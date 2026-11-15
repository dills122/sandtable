# Synthetic campaign arrivals: admission and durable Host work session

## Goal and realistic checkpoint

A single owner can create a synthetic campaign with one initially placed unit
and a later independently arriving unit, reach its eligible stage, choose an
allowed destination, and recover the same world and receipt from the existing
SQLite journal after restart. This is an engineering vertical slice, not a
historical scenario or release-candidate certification. Existing setups,
wire formats, `ExplicitNone` semantics and journal histories must retain their
old behavior.

This is larger than a short edit: setup and content admission, sequence policy,
world validation, event/snapshot versions, owner projection and durable replay
all cross existing authority boundaries. Budget a focused 3–5 hour session for
contract and the first native Core admission/replay cut, then 4–8 additional hours
for Host integration and failure-sensitive checks; these are planning estimates,
not a promise. If a contract or compatibility issue proves larger, preserve a
reviewable checkpoint and revise the estimate rather than claim completion.

## Work order and acceptance

1. **Contract and trusted identity** (45–75 min): trace current creation,
   content and setup version boundaries. Define one synthetic fixture, versioned
   schedule/content/ruling binding, initial versus deferred unit validation,
   and exact allowed non-land obligations. Require a replayable canonical input;
   candidate hashes supplied by a caller are not trusted authority. Freeze
   creation, command, event, snapshot and receipt shapes before implementing.
   Historical source rights and independent review remain separate.
2. **Native Core lifecycle** (90–150 min): admit only that profile through a
   new setup/content version; keep legacy validation unchanged. Generate initial
   world without deferred units. At the exact arrival phase expose owner-only
   choices, reject stale/wrong-side/early/duplicate choices, create unit and map
   representation atomically, and block stage completion until every declared
   obligation is resolved. Recompute effects on replay and validate checkpoint
   state and identity. Prove old setup/event bytes and replays unchanged.
3. **Durable Host** (2–4 h): add a typed group opening to existing authenticated
   seat/reference path; no fabricated on-map acting element. Route the accepted
   Core command and its event, receipt, checkpoint and contact changes through
   the existing SQLite immediate transaction. Cold import must revalidate the
   complete transcript. Cover restart, retry after lost response, conflicting
   reuse, concurrent contenders, rollback and tampered journal evidence.
4. **Privacy and release checkpoint** (1–2 h): verify opponent cannot infer
   future group, choice, destination or private authority changes from opening,
   references, errors, receipt, logs or outward snapshots. Run focused Core,
   Host and boundary suites, then release checkpoint and exact-head CI as
   applicable. Update architecture and RC tracker with actual evidence and
   remaining historical rulings, rights and independent source-review gates.

The prototype in `campaign-arrival-replay-draft-v1.md` is a test aid only. Its
codec and freely supplied rules/ruling hashes must not be stored as authenticated
campaign evidence. The Host's existing `accepted_steps` transaction and cold
Core import are the target, not a new side store. Test both normal admission
and all rejection paths before changing the user-visible route.

## Earlier phase checkpoint

The pinned synthetic profile and separate versioned setup now drive an isolated
Core phase at the actual Land-sequence arrival position. It creates its unit and
representation, advances to Fleet Assignment, exposes a private owner offer,
encodes its event and reconstructs checkpoint state and retry receipt by replay.
Campaign ID, setup identity, version and effects are checked; a different
campaign or forged history rejects. This is not yet the normal Core campaign
creation, event union or snapshot path. It has no authenticated seat or durable
Host append. The journal test proves the exact candidate setup is rejected
without reserving a campaign ID, rather than claiming that it is admitted.

A significant integration constraint is that `CampaignCreationExecution`
resolves Setup6 and Content6, while current Snapshot11 admission requires its
old pre-movement version/position sequence and the initial world until movement.
An inserted arrival and new world member therefore require a successor setup,
content, event and snapshot admission path, plus the current serializer/import
and exercise/Host routing. Relaxing Snapshot11 would reinterpret existing
history. The fixed synthetic phase is a reference for those successor contracts,
not a substitute for them.

## Current synthetic journal checkpoint

The separate, pinned World6 successor now creates one deferred Axis unit from
the certified Content6 template and reaches the arrival position through six
replay-checked preamble records. The internal durable Host path stores creation,
all six records and the arrival in the existing hash-chained SQLite journal;
authenticated owner references, atomic commit, retry and full Core replay are
implemented. Existing `ExplicitNone` and normal campaign creation are unchanged.

This is still a deliberately narrow synthetic integration: native preamble
bytes now have a v2 internal journal envelope (v1 digest-only history remains
readable); ordinary `CreateCampaign` rejects
the successor, and no HTTP route or normal campaign observation exposes it.
Native successor campaign events and snapshots, general admission, safe public
contracts and privacy verification across those surfaces remain necessary.
Historical scenario source rights, independent second readings and rulings
remain separate release gates. See
[durable Host contract](../../specs/campaign-scheduled-world6-durable-host-v1.md)
and [World6 contract](../../specs/campaign-arrival-world6-successor-v1.md).

## Next ordinary-admission cut

1. Define a successor creation/snapshot/event union that carries the deferred
   schedule, native preamble records and arrival without changing V10/V11 bytes
   or their `ExplicitNone` admission. Pin its setup and content identity, phase
   obligations, canonical bytes and replay behavior before implementation.
2. Extend `CampaignCreationExecution` and `CampaignExercises.Begin` with a
   version-discriminated session instead of casting the successor to V11.
   `CampaignSnapshotV11Admission` requires the old initial world and version to
   equal its old sequence index; weakening it would reinterpret saved games.
3. Extend the ordinary journal's `CreateCampaign`, import, accepted-step and
   snapshot dispatch for the successor. Preserve old create/retry behavior and
   require byte-for-byte replay of both versions. Cover duplicate creation,
   rollback, cold process recovery, tampered native events and mixed history.
4. Only after that state can advance beyond Fleet Assignment, add typed owner
   group openings and responses to the existing HTTP contract. Check both sides'
   responses, status codes, references, logs and timing across pending/arrived
   states, including stale credentials and retry. The present HTTP privacy test
   checks only that the old opening route does not reveal the synthetic cut.
