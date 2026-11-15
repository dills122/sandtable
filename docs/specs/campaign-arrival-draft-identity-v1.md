# Campaign arrival draft identity v1

**Status:** internal synthetic draft contract; not campaign admission, a
released wire format, a trusted journal head, or historical content. It builds
on [content draft analysis](campaign-scheduled-arrival-content-draft-v1.md).

## Identity

Version 1 binds the complete canonical candidate content definition, selected
scenario ID, exact scheduled groups, each group's allowed destination IDs,
and supplied SHA-256 ruleset and ruling bindings. Its own `sha256:` identity
hashes a domain-tagged UTF-8 JSON envelope with fixed field order. Groups sort
by turn, stage, owner and group ID; unit IDs, source references and destination
IDs sort ordinally. The candidate content is written by the existing canonical
content writer without relaxing its normal public validator; the draft envelope
is a separate format and must never be accepted as a legacy content artifact.

Creation requires a unique selected scenario, no draft issues, no unexplained
legacy issues, and no CNA compatibility issues. Each group has exactly one
nonempty list of distinct destinations, all present in content. Source IDs and
locators in the schedule use the existing safe ASCII source-atom grammar, so
JSON encoding cannot collapse malformed UTF-16. Inputs are copied; returned
bytes must be copies. Any change to content, selected scenario, schedule,
sources, destinations or supplied bindings changes the bytes and identity.

The internal candidate reader accepts at most 4 MiB of canonical envelope bytes,
reconstructs the embedded content and schedule, reruns all candidate validation,
and requires the reconstructed bytes and caller-supplied expected identity hash
to match exactly. The caller must authenticate the expected hash separately.
Unknown, duplicate,
missing, reordered or malformed fields and noncanonical encodings reject. This
supports testing cold reconstruction only: rehydration does not authenticate the
supplied hashes, establish a trusted setup, or admit the candidate to a campaign.

The ruleset and ruling hashes are supplied references, not authenticated facts;
callers must later bind and validate their authority. A valid draft does not
prove source rights, actual destinations, complete obligations or identity of a
historical force. A group list cannot certify that a stage has no obligations.

## Deferred lifecycle boundary

The draft describes independently placed content elements that are absent from
initial placements and scheduled exactly once. It does not model withdrawal and
return. A successor authoritative lifecycle must introduce pending/arrived
states, create and validate real world elements and map representations, bind
the accepted content/rules/ruling identities to setup and every event, and
replay from trusted history. It must account for other arrival categories before
phase completion, authenticate actor choices, and commit event and receipt
atomically. This draft must not be attached to an existing `ExplicitNone` setup
or used to change any existing snapshot's state/version relationship.

Tests must cover deterministic ordering, defensive copies, changed-field hash
sensitivity, duplicate and missing references, wrong owner, invalid dates and
destinations, unknown source, malformed text and legacy artifact rejection.

## Candidate world contribution

A draft-only contribution may materialize one scheduled group as real Core element and
independent map-representation values. It must check the group's side, exact scheduled
turn and stage, permitted destination, and absence of all group elements and generated
representation IDs in the supplied world before producing any values. Representation IDs
are deterministic and scoped to the draft identity and element. Operational fields start
with the same zero ledger and cohort defaults as initial placements, at the arrival slot;
these are synthetic defaults pending a source-backed readiness ruling. The contribution
contains no event and does not mutate or validate an authoritative campaign snapshot.
It is not admission, persistence, replay, or an implementation of off-map lifecycle.
