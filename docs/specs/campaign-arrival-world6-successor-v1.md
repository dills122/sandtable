# Synthetic scheduled World6 successor v1

This internal synthetic successor-world cut uses the existing certified
`rules-lab.content.breakdown-truck.v1` Content6 artifact and its
`breakdown-truck-lab` scenario. A distinct future setup explicitly treats the
scenario's `axis-infantry` placement as a deferred arrival template. The
unmodified Content6 artifact remains valid and retains its original meaning for
all existing setups. This override is permitted only under this new synthetic
profile, never inferred from a legacy `ExplicitNone` setup.
The exact certified artifact hash is
`sha256:646e76e69ecceb82216b37d84e950928099acd8a3cb04b51526d0fe631e512ee`;
the transition verifies that identity rather than accepting caller content.

At initial world construction, Core first constructs and validates the normal
World6 from the certified artifact, then withholds the exact `axis-infantry`
element and its bound representation. The other elements, component TOE states,
cohorts, representation IDs and initial locations are unchanged. At turn 1,
stage 1 at the exact sequence-v4 Naval Convoy Arrival position, only Axis may deliver that exact unit to its template location
`north-west`. Its complete component and operational state and original map
representation are restored atomically. The resulting world must equal the
normal certified initial World6 exactly; duplicate, early, wrong-owner, wrong
location or altered initial world reject. No RNG is drawn.

This is a bounded World6 transition, not yet a campaign setup, phase gate,
authoritative event or Host action. A full successor must bind the override,
scenario and artifact hashes to versioned creation/setup evidence, validate the
actual sequence cursor and all other obligations, retain event and receipt
atomically in the existing journal, and prove replay and fog boundaries. A
synthetic override does not resolve historical force or arrival rulings.

## Bounded phase and event

The successor phase binds a stable campaign ID, a canonical synthetic setup
hash, the caller's trusted positive base state version and the exact sequence-v4
arrival position. Its setup binds the certified artifact identity, scenario,
deferred element, destination, arrival slot and synthetic declaration of no
other arrival obligations. An owner command carries the campaign and setup
identity, expected state version, command ID, side and destination. Acceptance
constructs the full World6, advances to the sequence-v4 Fleet Assignment
successor, and emits one versioned event with the exact representation and element
identities. The event retains the command so an exact retry returns the same
receipt while conflicting reuse or a second arrival rejects.

Replay starts from the pinned setup, caller-supplied trusted campaign/base
version and pending World6, then recomputes the transition and rejects a foreign
campaign, changed setup, wrong version, forged effect, wrong successor or
repeated event. This cut does not itself authenticate its caller, admit a normal
campaign snapshot or write the Host journal.

This bounded setup is pinned to
`sha256:f1a99a29b552f2cdc87ea80ac6c35061a97663d7933bb54b97a19d4f3935f109`.
A change to its ruleset, artifact or declared fields fails closed; retaining
historical versions still requires an explicit versioned resolver. The setup ID
is part of those canonical bytes, and resolving this cut requires the exact bytes
and hash, not merely a self-consistent caller-provided pair.

Event bytes use a bounded versioned canonical JSON envelope; decoding alone is
not admission. The phase checkpoint stores trusted campaign ID, base version,
setup hash and zero or one canonical event. Readback reconstructs World6 by
replay, checks the claimed phase version and successor, and rejects foreign or
noncanonical bytes. These are internal successor-cut contracts until integrated
with current campaign event/snapshot serializers and the journal.

## Creation cut

A bounded Core creation request uses the existing `CampaignCreationRequest`
shape but requires the exact scheduled World6 setup ID/hash, current canonical
ruleset hash, certified Content6 ID/hash, and scenario ID. It validates a stable
campaign ID and creates a version-1 synthetic successor creation event with
the withheld World6, the normal seeded random stream at cursor zero, and the
first sequence-v4 position. Replaying that event recomputes all values from the
request and pinned inputs; mismatched event data rejects. This new creation cut
is not yet selected by `CampaignCreationExecution`, `CampaignExercises`, or the
Host journal, and it cannot advance through the preamble yet.

The bounded creation event codec records the creation request bindings, version,
first position and initial RNG algorithm/cursor in a fixed canonical envelope.
The World6 is reconstructed from the pinned content and setup, not accepted from
caller-supplied world bytes. Encoding verifies that the supplied event matches
recomputation; decoding requires an independently trusted expected request and
exactly the bytes Core recomputes. This is not yet the normal Chronicle creation
codec or an authenticated journal record.

The phase also binds the SHA-256 of the canonical creation evidence. Starting or
replaying the cut requires the trusted creation request and a creation event
that recomputes exactly from it; event commands and checkpoints carry that
creation hash. An event from another seed, setup or campaign cannot be replayed
as this campaign's arrival. The supplied base version and phase cursor still
require validation against the real preamble history before Host integration.

At the eligible cut, the synthetic offer exposes the deferred element and its
destination only to Axis. Commonwealth receives the same empty offer list before
and after resolution, without version or schedule fields. This is an internal
projection and does not authenticate a seat or provide a Host action reference.

## Direct preamble computation

The setup pins certified baseline setup
`sha256:e6631e81ad8f97e39fd9d7eec93bad7fe2b39db4d2d3059ed94a02dd4093e7a3`
and synthetic Axis-first initiative choice. The successor now directly computes
six existing typed preamble events from the trusted creation and rules, with
initiative, RNG and weather derived from the successor state. No baseline
campaign session or baseline world is constructed. Tests compare event digests,
RNG, order and weather against the certified campaign across several seeds.

The bounded phase and checkpoint readers consume a private preamble cut produced
by the direct computation, rather than accepting a caller-selected state
version or phase cursor. The cut retains only the validated successor creation,
its request, the exact arrival cursor, version and RNG state; it never exposes
an unrelated session or world. This removes arbitrary cursor/version inputs
within the prototype. The old bounded event envelope remains digest-based;
it is not the normal campaign event union or ordinary admission.

The bounded successor snapshot joins the replay-validated World6 phase with the
verified cut's RNG state, initiative holder, stage order and weather. Its
constructor accepts only a verified cut; callers cannot supply those fields.
The arrival must preserve all of them. Snapshot decoding reconstructs the cut
from trusted creation/preamble and the World6 phase from event evidence, rather
than trusting serialized world or RNG claims. This snapshot is an internal
successor cut and is not yet the normal CampaignSnapshot or Host checkpoint.

The deterministic trace retains the digest of each directly computed native
preamble event and its resulting version and position. Its existing wire format
is unchanged; a digest is not itself a native Chronicle event or receipt.

A bounded successor preamble event binds the campaign, setup and creation hash,
previous/result version, previous/successor position and digest of its
recomputed native preamble event. The six expected events are deterministic
for the trusted creation. Verification compares each supplied event to that
recomputed sequence; a valid prefix is explicitly incomplete and cannot yield
an arrival cut, while altered, repeated, foreign, reordered or extra events
reject. Only a complete sequence yields the verified cut. These internal events
are a specialized successor contract retained by the internal Host journal,
not yet the normal Chronicle event union.
Successor preamble events have a separate bounded canonical JSON codec;
readback must be followed by full sequence verification before it yields a cut.
New internal journal records use a v2 envelope that also stores the full native
event bytes; cold replay compares the entire envelope with a fresh computation.
Existing digest-only v1 records remain readable. Neither version is ordinary
campaign admission.

Each verified preamble prefix also has a recomputed successor checkpoint: its
current version, sequence position, RNG, initiative/order/weather and the
withheld-unit World6. Checkpoints are computed from trusted creation and rules,
not from caller-provided state. Empty and partial prefixes remain unable to
open arrivals; native event bytes are not yet stored in the v1 envelope.
A bounded canonical prefix-checkpoint envelope retains the exact successor
preamble event bytes and claimed cut coordinates. Decode requires the trusted
creation request and event, decodes and verifies every event, recomputes the
sanitized World6 checkpoint, and compares the entire canonical envelope.
The internal Host journal authenticates and commits these records; normal
campaign admission remains separate.

## Core exercise bridge

A narrowly scoped synthetic Core bridge may expose an opaque session to Host
integration code. Begin verifies the existing creation request against the pinned
setup and returns canonical creation evidence and an initial checkpoint. Each
preamble advance emits exactly one successor preamble event and its recomputed
checkpoint. Only a complete verified preamble exposes an Axis offer; other sides
receive no offers. Arrival submission emits one event and checkpoint, and exact
retry returns the original evidence without advancing. Import requires a trusted
creation request, canonical creation evidence, an ordered preamble event prefix,
an optional arrival event and the exact recomputed checkpoint; altered, missing,
foreign or extra evidence rejects. All returned byte arrays are defensive copies.

The bridge is synthetic and explicitly not the normal campaign creation or
Chronicle API. It performs no seat authentication or persistence. Host must
check the authenticated seat, preserve fog boundaries, validate replay and
commit event and receipt atomically before exposing any result. Until that
integration exists, this bridge must not be offered as a playable campaign.

For Host integration, every accepted synthetic step supplies separate canonical
command, event, receipt and checkpoint bytes. Preamble commands bind campaign,
creation, expected version and ordinal; arrival commands bind those fields plus
command ID, actor and destination. Receipts bind campaign, result version and
the SHA-256 of the exact event. A replay verifier must recompute and compare all
four byte strings for each ordered step, not trust a receipt, hash chain or
checkpoint alone. The Core bridge returns copies. Host authentication and the
SQLite transaction remain separate responsibilities.
