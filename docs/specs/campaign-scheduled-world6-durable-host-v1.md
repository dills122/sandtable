# Synthetic scheduled World6 durable Host cut v1

Internal synthetic journal integration; no HTTP route or general campaign
admission. The normal Host's existing SQLite `campaigns`, `accepted_steps`,
seat credential, reference key, head hash and immediate transaction mechanisms
are reused. Existing setup and journal replay paths retain their semantics.

Creation accepts only the exact pinned scheduled World6 request through the
Core bridge. In one immediate transaction, Host stores Core's creation event and
initial checkpoint, then each of six recomputed preamble command/event/receipt/
checkpoint records in contiguous `accepted_steps`, updating the head each time.
The synthetic profile has no contact records; it does not expose general world
observations. A retry of the exact request verifies and returns the retained
session without rewriting; a conflicting request rejects.

Cold read checks the existing journal hash chain and exact scheduled record kind
and metadata, then invokes Core bridge `Restore` over all creation, command,
event, receipt and checkpoint bytes. It never accepts a checkpoint or hash chain
without Core replay. Preamble rows have no seat/owner reference. A single arrival
row binds the authenticated Axis seat epoch, a domain-specific opaque reference
and its fixed eligible offer. The reference is HMAC-derived from the existing
journal key, campaign, side, seat epoch, profile, visible offer and opening
revision; no deferred ID or destination appears in a Commonwealth opening.
The arrival command ID in Core evidence must equal that retained opaque reference.

Owner query authenticates the seat and replays the journal in one transaction.
Only Axis sees an offer and its state version; Commonwealth and completed-phase
queries return an empty offer collection without a version. Submission verifies
the seat, current eligible offer and reference before calling Core. Event,
command, receipt, checkpoint, contacts and journal head commit together in one
immediate transaction. An exact accepted retry by the same active seat returns
the retained receipt after full replay; conflicting, stale, foreign, wrong-side
or changed-epoch references reject without mutation. A rotated credential cannot
recover a prior seat's receipt. No public endpoint or general campaign observation
is enabled by this cut.

This path is not `CampaignExercises`/Snapshot11 admission and does not provide a
playable campaign. It uses the separately versioned Core bridge and directly
computes preamble events. Future integration must add native events to the
ordinary campaign union, add all required phase and observation behavior and extend
normal routes with equivalent boundary tests.

## Versioned native preamble envelope

New synthetic journal creations use `scheduled-world6-preamble-v2` records.
Each canonical envelope binds the unchanged v1 preamble event and its full
canonical native preamble event bytes. The native bytes must equal the event
recomputed from trusted creation and the preceding prefix; their digest must
match the retained v1 event. Receipt hashes the complete v2 envelope. Cold
replay selects v1 or v2 from the stored action kind and verifies every command,
event, receipt and checkpoint; mixed versions within one history reject.
Previously retained v1 records and their bytes remain valid. This versioned
envelope is internal to the synthetic successor, not ordinary campaign admission.
