# Inherited Combat successors and first opening v1

`CMB-TASK-003D2c.1`, input `faa3282`. Contract checkpoint for CON-003/004;
parent003D2c/003 and checkpoint B remain open. [Schema inventory](combat-inherited-successors-v1.schema.json),
[retained fixture](fixtures/combat-inherited-successors-v1.json),
[oracle](verify-combat-inherited-successors-v1.py), [combined plan](../design/combat-cycle-implementation-plan.md).

This packet declares the exact inherited successor versions and freezes a private, isolated
Reserve-completion opening transition. It does **not** supply an actual creation-to-Reserve history,
a general World reader, Snapshot12 restoration, production registration or simulator evidence.
D2c.2 supplies accepted predecessor events; D2c.3 supplies Movement/Reaction/Breakdown provenance
and continuation; D2c.4 composes complete authority and capacity. Other declared event payloads
remain unfrozen until their respective child checkpoint. A version declaration alone is not a codec.

## Current dispatch and successor declarations

Current `CampaignCurrentEventSerializer` admits Created10, eleven preamble cases through
`CampaignV11PreambleCodec`, and eight cases through `CampaignBreakdownEventSerializer`.
The current context is Content6/Setup6/World6/Snapshot11/sequence4. The prospective context is
Content7/Setup7/World7/Snapshot12/sequence5. Current preamble explicitly translates only sequences
3 and4; it cannot be reused by changing a manifest hash or relabelling its bytes as sequence5.

The fixture pins fifteen source files, including the dispatchers, event version constructors and
Reserve source ruling. The oracle compares the exact preamble type set, inherited event names and
eight current post-Movement version pairs against those sources. This is a source-backed inventory,
not execution of the C# readers. Historical codecs, bytes and admission remain unchanged. Their
explicit old version/context checks reject these successors; production compatibility tests belong
to008. No implicit migration or defaulting of absent cycle/history data is permitted.

| Event type | Current → successor | Family | Contract status |
| --- | --- | --- | --- |
| `campaign-created` | 10 → 11 | creation | frozen-c2 |
| `initiative-determined` | 2 → 3 | preamble | declared |
| `no-obligation-naval-convoy-schedule-resolved` | 1 → 2 | preamble | declared |
| `no-obligation-tactical-shipping-resolved` | 1 → 2 | preamble | declared |
| `initiative-order-declared` | 1 → 2 | preamble | declared |
| `weather-determined` | 1 → 2 | weather | declared |
| `no-obligation-organization-resolved` | 1 → 2 | preamble | declared |
| `no-obligation-naval-convoy-arrival-resolved` | 1 → 2 | preamble | declared |
| `no-obligation-fleet-assignment-resolved` | 1 → 2 | preamble | declared |
| `no-obligation-fleet-repair-resolved` | 1 → 2 | preamble | declared |
| `reserve-element-designated` | 1 → 2 | reserve | declared |
| `reserve-designation-completed` | 1 → 2 | reserve | frozen-isolated-opening |
| `element-moved` | 3 → 4 | movement | declared |
| `reacting-element-moved` | 2 → 3 | reaction | declared |
| `reaction-participant-completed` | 2 → 3 | reaction | declared |
| `reaction-window-closed` | 2 → 3 | reaction | declared |
| `movement-segment-completed` | 2 → 3 | movement | declared |
| `element-movement-stopped` | 1 → 2 | breakdown | declared |
| `breakdown-stop-resolved` | 1 → 2 | breakdown | declared |
| `breakdown-segment-completed` | 1 → 2 | breakdown | declared |

Created11 is already frozen by [C2](combat-authority-envelope-v1.md). Reserve completion2 below
is frozen only for the named isolated first-opening profile. The other eighteen declarations
reserve exact future pairs; their family packets must freeze fields, replay and canonical bytes.
D2a's separate experimental `combat-cycle-element-moved`1 remains distinct from inherited
`element-moved`4; D2c.3 must reconcile its cost/proof semantics without conflating wire types.

## Opening authority and trust boundary

`OpeningBase`1 contains a C2 creation request, resolved first acting side, exact D1 Reserve position,
prior version/prefix, complete bounded World, retained RNG and own Reserve members. Profile is
exactly `isolated-first-opening`: game turn1, stage1, relative first slot, either resolved side.
The catalog position retains its relative actor and null active side; `cycle.actingSide` resolves
that role using `firstActingSide`. A second slot, later stage or fabricated actor position rejects.

The base is **independently trusted input**, never supplied or recovered from the event being read.
Its prefix and any designation receipt are synthetic fixture evidence. These fields bind a fork;
they do not prove its history. D2c.2 must derive them from accepted predecessors before this
transition can participate in a composed prospective trace. A self-consistent forged base is not
a valid production restore. There is no snapshot admission entry point in this packet.

The closed World profile is C2's two infantry initial World with only the acting unit's Reserve
status (`none` or `I`) and integral CP0–10 varied. CP3 probes preservation, not reachability at
Reserve designation. All other World bytes must equal the initial profile: ammo10, unchanged TOE,
locations and provenance, no relationships, settlements or future obligations. Nonempty future
obligations require D2c.4; this packet cannot establish their preservation in a reachable campaign.
RNG retains the certified algorithm and seed with a typed unsigned cursor; no draw occurs here.
Every own original unit appears exactly once with matching creation key, status and CP. I requires
a designation receipt, none forbids it, and all conversion/release/offensive/exception history is
empty at ordinal1. Missing members, duplicate/cross-side units or preexisting release state reject.

`OpeningCommand`2 binds kind `complete-reserve-designation`, canonical base hash, prior version
and expected Reserve position; trusted input also binds submitting actor. Only the resolved owner
can complete. No decision window, remote I/O, random draw or second cycle-open command exists.
The command applies to both empty designation and the retained I member.

## Atomic event, replay and retry

`reserve-designation-completed`2 atomically advances to the exact same-slot Movement position and
opens ordinal1. It retains campaign/rules/configuration identities, prior/result versions, prior
prefix, source Reserve position, accepted input, successor position and the cycle authority/ID.
The old completion's source references (`18.11`, `5.2.reserve-designation`) remain explicit.
Game/stage/owner are bound by the authority, not inferred from ambient current state. Exact field
order and types are in the schema; unknown, duplicate or reordered properties reject.

The cycle uses D1's identity framing, including the complete content/setup/rules/config identity,
relative slot and resolved actor. `openedAuthorityVersion = priorVersion + 1`; `openingPrefix`
is the prefix **before this event**. The event receipt is `rc.` plus the domain-separated digest
of the canonical event excluding `receiptId`. Event hash includes that receipt; the resulting
Chronicle prefix uses D1 `prefix_event(priorPrefix, canonicalEventBytes)`. This avoids self-hashing.
Authority version arithmetic is checked: prior10 through signed64 max−1 can encode the one event;
this local bound does not certify remaining campaign capacity. D2c.4 must enforce composition
capacity before admission, including mandatory successor events.

Replay takes a separately validated base and a suffix of zero or one event. Every event field is
recomputed from its accepted input and must match canonical bytes. The resulting private
`OpeningState`1 preserves the exact World, RNG and Reserve history and appends one C3-shaped
command receipt. It is an oracle projection, **not Snapshot12**. A second event, wrong fork,
wrong rules/configuration, changed owner, ordinal, prefix, position or receipt rejects atomically.
Caller-supplied cached state is not authority; restore recomputes it from base and suffix.

Exact retry after completion first authenticates the actor, then compares the entire accepted
input before stale-version handling. It returns the same receipt/event/state with `duplicate=true`
and publishes nothing. A changed command, even with a reused base, is a conflicting retry.
There is no system timeout fallback for this synchronous inherited command. The caller's base and
suffix remain unchanged on success, retry and rejection.

Canonical JSON uses the predecessor's compact ASCII encoding and schema order. Maximum is1MiB,
depth32, arrays512; the closed profile has one own member and one event. Unknown/null/wrong-type
fields, booleans or fractional values in integer fields, BOMs, extra whitespace and duplicate keys
reject. Codes:001 shape/bytes,003 unsupported profile/value,004 identity/precondition,
005 actor,006 suffix/conflict,007 version range,008 noncanonical bytes,009 source/declaration drift.

## Bounded implementation ownership

These are concrete proposed child IDs under existing008/019, subject to checkpoint B acceptance.
No new parent task or runtime authorization is introduced. Each child includes its focused tests
and is capped at five primary files; split further before edits if needed.

| Child | Responsibility | After |
| --- | --- | --- |
| 008A | Created11/root event codec | 007 |
| 008B | New-context preamble codecs and typed sequence binding | 008A |
| 008C | Weather successor codec and retained RNG evidence | 008B |
| 008D | Reserve designation/completion codecs and own-history binding | 008C |
| 019A | Atomic first-cycle opening projector | 008D |
| 008E | Inherited Movement codecs | 008D |
| 008F | Reaction codecs | 008E |
| 008G | Breakdown codecs | 008E |
| 008H | Shared dispatcher and restore composition | 008B/C/D/E/F/G and019A |

008 owns codecs/adapters and restore;007 owns World/Snapshot root shapes. 019A moves first opening
ahead of the first composed predecessor-to-Combat test. The rest of019 retains its018 dependency
for Movement witnesses and repeat/finish. 008E's codec dependency does not imply completed018
movement adjudication. Actual gameplay projection remains with its numbered domain owner, and
020/021 registration cannot absorb missing inherited adapters. The owner DAG is acyclic and
preserves the25 numbered tasks; broader runtime readiness still follows their parent gates.

## Verification and remaining work

`python3 docs/specs/verify-combat-inherited-successors-v1.py` checks20 source-bound declarations,
nine bounded owners, four literal opening traces (both actors, empty/I designation), eight replay
cuts,216 event-leaf mutations,32 raw rejections and88 boundary checks. Twenty retained base/input/
state/event goldens are compared only after literal ordinal/version/position/resource assertions.
Normal verification never regenerates fixtures. Author first-opening regression failed against
the stub, then passed with the atomic transition. Two source-audit regex errors were localized to
unrelated parser branches/property strings and narrowed before the inventory check passed.

Evidence covers the private boundary only. Full Weather/order/designation provenance, genuinely
reachable CP/RNG values, old/new C# codec execution, general snapshots, pending interrupts,
armed continuation, later stages/slots, simulator and privacy evidence remain D2c.2–4/004/B and
runtime tasks. Parent003 does not close on these isolated goldens.
