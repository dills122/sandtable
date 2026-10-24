# Campaign durable authority journal v1

Normal campaign Host storage retains the canonical Core creation evidence and every accepted command, event, receipt, snapshot checkpoint, and contact transition. The store is an internal authority journal, not a browser contract or a second adjudicator. Core replay of the complete transcript is required on every cold read and before append.

## Schema and transition

- `campaigns` binds the versioned creation request fields, canonical creation event, initial snapshot, initial contact records, head sequence, and head hash. Campaign IDs are unique. Creation takes an immediate transaction before lookup or fresh Core admission. An exact retry compares every creation-request field and canonical timing bytes, fully verifies retained evidence through Core, and returns the current retained head without writes, even after accepted steps. A differing request or timing conflicts without overwriting authority; concurrent creators serialize under the same transaction. Null timing remains distinct from explicit timing.
- `accepted_steps` has one row per accepted Core transition: contiguous sequence, action kind, optional acting element and owner reference/seat epoch/opening revision, canonical command, canonical event, canonical receipt, checkpoint, contact records appended by that transition, prior hash, and record hash. The hash binds every field and length-prefixed evidence bytes; the initial hash binds the request and creation artifacts including contact records. Current schema v8 retains canonical `timing_configuration`, `force_rounds`, and `rba_rounds`. Legacy v5 (without timing), v6 (timing without Force rounds), and v7 (Force rounds without RBA deadlines) remain readable with their historical hash rules; all mutations require v8. No implicit timing or schema upgrade occurs.
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
of outward route enablement. Each sweep visits at most sixteen distinct collecting
campaigns using a rotating lexicographic cursor, preventing a persistent corrupt
or non-expired first page from starving later campaigns. Each campaign has its
own immediate transaction. Corruption/storage errors are logged and isolated,
never counted as successful cancellation; shutdown cancellation interrupts the
scan between campaigns. Legacy journals have no RBA sweep or mutation. This is
local durable lifecycle recovery, not public-host certification.
