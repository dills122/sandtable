# Campaign durable authority journal v1

Normal campaign Host storage retains the canonical Core creation evidence and every accepted command, event, receipt, snapshot checkpoint, and contact transition. The store is an internal authority journal, not a browser contract or a second adjudicator. Core replay of the complete transcript is required on every cold read and before append.

## Schema and transition

- `campaigns` binds the versioned creation request fields, canonical creation event, initial snapshot, initial contact records, head sequence, and head hash. Campaign IDs are unique. Creation takes an immediate transaction before lookup or fresh Core admission. An exact retry compares every creation-request field and canonical timing bytes, fully verifies retained evidence through Core, and returns the current retained head without writes, even after accepted steps. A differing request or timing conflicts without overwriting authority; concurrent creators serialize under the same transaction. Null timing remains distinct from explicit timing.
- `accepted_steps` has one row per accepted Core transition: contiguous sequence, action kind, optional acting element and owner reference/seat epoch/opening revision, canonical command, canonical event, canonical receipt, checkpoint, contact records appended by that transition, prior hash, and record hash. The hash binds every field and length-prefixed evidence bytes; the initial hash binds the request and creation artifacts including contact records. Current schema v10 retains canonical `timing_configuration`, `force_rounds`, `rba_rounds`, and `cycle_rounds`. Legacy v5 (without timing), v6 (timing without Force rounds), v7 (Force rounds without RBA deadlines), v8 (lazy Force opening), and v9 (no cycle-control deadline) remain readable with their historical hash rules; all mutations require v10. No implicit timing or schema upgrade occurs.
- `operator_capability` holds only a domain-separated digest of a 32-byte operator secret. `seats` holds two possible sides per campaign, an increasing epoch, and at most one outstanding grant or credential digest per side. Grants live ten minutes and are single use. Redeemed credentials live two hours. A replacement grant increments epoch and invalidates the prior credential, including after process restart.
- `reference_key` holds a private 256-bit HMAC key. `owner_openings` binds the authenticated seat epoch, acting element, visible bytes and digest, audience revision, and current private authority version. `owner_actions` holds the reference-to-Core-action map separately. Query checks the seat and replays Core inside one immediate transaction, then replaces the private map at the current cut. Equal visible bytes under an unseen authority change retain the opening revision and outward references; a changed view or seat epoch advances the revision.
- Append takes a SQLite immediate transaction, replays the retained prefix through Core, checks the submitted command against that exact head, then commits evidence and head in the same transaction. Rejected commands write nothing. A concurrent or stale command cannot advance the head.
- Contact-derived owner movement submission authenticates the seat, checks the opening revision, compares the retained visible cut and action mapping with the freshly derived cut, verifies the HMAC reference, and asks Core to adjudicate that offer against the current private authority in the same immediate transaction. An unseen accepted step may advance that private version while visible bytes and offers remain equal; it does not invalidate the old reference. A changed visible cut or offer does. The committed step is marked `contact-movement`; cold replay uses the contact path rather than historical V7 action lookup. A retry of the same accepted reference and opening revision returns the retained receipt after full cold replay. Unknown, stale, or forged references return one generic rejection.
- Read uses one SQLite snapshot transaction. It rejects missing, reordered, truncated, or altered records and requires `CampaignHostAdapter.Import` to reproduce every retained command, event, receipt, checkpoint, and creation/step contact record from independently replayed authority. A failed read never returns a session.

This journal stores no plaintext seat secret. Owner opening and movement submission include bounded native Combat stages through the explicitly configured, loopback-only Development v2 API. Creation retries retain the existing 201 acknowledgement; identity conflicts use generic 409, with operator authentication before campaign lookup. This does not activate public hosting or close original HOST-PUB certification obligations. Historical temporary and P01 private stores remain separate.

## Persisted RBA lifecycle (schema v8)

`rba_rounds` is keyed by campaign ID and the authentic Barrage event hash. It binds
the Core decision action ID, campaign timing configuration hash, opened/deadline
milliseconds, monotonic high-water milliseconds, collecting/completed/cancelled
status and a domain-separated record hash. Its deadline is opened time plus the
pinned `rba` budget. The round is created in the same immediate transaction as
automatic Barrage, before any owner can query the opening. Accepted retreat moves
retain this original deadline and binding; reopening or cold restart never renews
it. Accepted decline completes the round before continuing Force Assignment.

At deadline equality, trusted-clock loss or regression, Core emits a distinct
V33 RBA cancellation that completes Combat without attack and advances into
same-slot Reserve Release. Empty Reserve Release may then append ordinary V27 completion. No defender decline, seal, result roll, World debit or surrender is
fabricated. Earlier accepted RBA retreat movement remains authoritative. If timing
or trusted time is unavailable at initial entry, Core receives an explicit
unavailable cancellation with absent clock evidence; Host invents no timestamp.

Owner submissions authenticate and validate retained revision, current visible
offer, private action mapping and HMAC before clock gating. Malformed, stale or
foreign proposals cannot manufacture cancellation. Exact accepted retries return
their retained receipt without clock mutation. Round closure, cancellation,
completion and any automatic Reserve Release append commit atomically. Cold read
checks each round against its authentic Barrage and terminal decline/cancellation;
missing, extra, tampered or incompatible evidence rejects.

A Host background service uses the same injected `TimeProvider` as the journal,
ticks every second when a journal is configured, and remains active independently
of outward route enablement. Each sweep visits at most sixteen collecting RBA
campaigns, sixteen collecting Force campaigns, and sixteen collecting cycle-control
campaigns, with independent rotating lexicographic cursors. A persistent corrupt or non-expired first page cannot starve
later campaigns or the other decision window. Already-persisted Force rounds use
existing V19 cancellation and V20/V21 continuation; no seal or result is fabricated.
Schema v9 creates the Force round atomically with an accepted RBA decline, using
the trusted time already validated by the RBA clock gate and the pinned Force
budget. Queries only read this round; no owner request starts or renews its clock. Each campaign has its
own immediate transaction. Corruption/storage errors are logged and isolated,
never counted as successful cancellation; shutdown cancellation interrupts the
scan between campaigns. Legacy journals have no RBA sweep or mutation. This is
local durable lifecycle recovery, not public-host certification.


## Force decision entry (schema v9)

An accepted RBA decline, its RBA completed status, and the initial collecting
Force round commit in one immediate transaction. The opening timestamp is the
same trusted reading accepted by the preceding RBA gate; the Force budget starts
there, even when neither owner requests a Force opening. No second clock read
can move or restart entry time. Existing pre-opening `force-unavailable` evidence
remains the only no-round exception for an unavailable initial Force decision.

Replay maps each decline to its preceding authentic Barrage in the accepted
transcript. The Force opening must equal that completed RBA round's retained
high-water timestamp; even a consistently rehashed shifted Force clock rejects.

Every v9 decline must have exactly its authentic decline-bound round or explicit
no-round cancellation. Deleted/missing rounds reject cold reads and owner queries;
queries never repair evidence. Schema 8 keeps its original lazy-round verifier and
RBA integrity checks, but all mutations are rejected. No migration invents a
historical opening timestamp. Schema 5/6/7 also remain read-only.


## Cycle-control deadline (schema v10)

Every native completed Reserve Release event atomically creates a cycle round,
keyed by campaign and the canonical completed release hash. It retains entry
state version, timing configuration hash, opened/deadline/high-water times, status
and record hash. Its immutable deadline uses the pinned `cycle-control` budget.
Queries cannot create or renew it. When timing or trusted time is unavailable at
entry, explicit native V34 `cycle-control-unavailable` finishes without invented
timestamps or a collecting row.

Valid owner Finish/Repeat completes the row in the same transaction. Invalid,
stale or foreign submissions are rejected before clock mutation; exact accepted
retries recover their old receipt. At deadline equality, clock loss or regression,
Core V34 records System `finish-phase` and enters same-slot Truck Convoy Movement,
without fabricating an owner Finish, changing World/RNG or repeating movement.
Cold replay checks every release has its row or explicit unavailable event,
checks every ending against its release and verifies exact clock evidence.

Background combat ticking adds an independent sixteen-campaign cycle cursor, so
RBA/Force queues and corrupt campaigns cannot starve it. Schema 5–9 remains
read-only, preserving schema 9 Force entry checks and schema 8 RBA checks. No
migration invents deadlines for historical completed releases.
