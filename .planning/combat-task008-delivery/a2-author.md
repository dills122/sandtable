# Author Explanation — A2

## Intent and plan

Complete Task008 A2 creation-only Snapshot12 codec/readback after accepted A1c (`a8eed35`, PR124).
Exact requirements remain C2 envelope/schema/fixtures and D1 creation-prefix framing. Also pin how
publication evidence will satisfy original creation contract without selecting storage now.

## Implementation flow

`CampaignCreationSnapshotV12.Create` bounds and privately copies Created11 bytes, validates them
against separately trusted A1c request, then computes event SHA-256 and creation-prefix digest over
ASCII domain/NUL/BE64 byte length/exact event. Snapshot owns validated Created11 and immutable
receipt/prefix; it retains no caller arrays. `CampaignCreationSnapshotV12Codec` emits frozen field
order and reconstructs expected complete creation state before exact import comparison. Mandatory
null/empty future slots remain explicit. Noninitial states reject; no generic later-state fallback.

## Components and choices

- New model file owns typed creation snapshot, receipt and prefix derivation.
- New codec file owns canonical write/readback; reuses established Setup/World/RNG/position writers.
- New test file uses literal7903-byte fixture, fixed event/prefix digests, independent BE64 framing,
  frozen snapshot mutations, raw negatives, missing/foreign evidence, unsigned seeds and mutation
  isolation. Typed expected values are never parsed from untrusted snapshot fields.
- Plan/spec/roadmap/design/names/README reflect bounded creation recovery and publication ownership.

Exact reconstruction fits creation-only invariant; a universal Snapshot12 parser would prematurely
admit later state. Private byte copy prevents caller mutation between validation and digesting.
No new dependencies, public surface, host implementation or historical codec edits.

## Publication evidence decision

Existing envelope says Task008 owes actual persistence; plan Checkpoint D excludes hosting and
HOST-RSH-001 defers provider choice until020–021 plus verified023. Preserve obligation explicitly
open as HOST-PUB-001, jointly Core/Host-owned. Target expected-head per-campaign commit batch;
spec now lists provider failure matrix, provenance, full-byte parity and evidence retention.
Core D/H may satisfy their in-process dependencies; neither closes Task008 publication obligation.
Task025/hosting/MVP ledgers must retain it until actual provider tests pass. No behavioral requirement
waived, provider selected or production storage schema/API frozen. A2 only pins evidence boundary.

## Verification

See `a2-evidence.md` for exact up-to-date commands/results and limitations. Worker RED missing
implementation observed; GREEN34 pass. Envelope oracle passed after doc changes. Lead dev review
found no remaining finding; full integration is recorded separately when finished.

## Risks and challenge points

Only stateVersion1 creation is supported; full retained history, Cycle/Combat and inherited families
remain B–H/019A. Closed-admission proof uses pure Core cut, not a running registry/provider. Checksums
do not authenticate artifacts. Review P0 framing/field ordering and externally mutable storage,
trusted request substitution, empty/malformed evidence and exact noninitial rejection. Critically
verify publication evidence allocation retains original obligation and does not overclaim acceptance.
