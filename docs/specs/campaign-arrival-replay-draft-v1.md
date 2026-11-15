# Campaign arrival replay draft v1

Internal synthetic prototype. It is not an admitted campaign event, snapshot,
Host command, durable campaign storage format, or proof of historical rules.

A journal begins with the selected candidate scenario's initial placements,
version zero, and a trusted draft identity supplied by its caller. Initial
world values use the same factory construction as existing admitted content,
but this path does not run or replace legacy world admission. Each version-1 command includes command ID,
expected version, group, side, turn, stage, and destination. A matching event
binds the entire command, the identity hash, the next version, and the exact
ordered element IDs. State transitions use the validated content and world
contribution. Group membership and world effects are recomputed, never trusted
from event data. Duplicate group delivery, wrong side/slot/destination, stale
versions, altered identity and altered event effects fail without changing state.
An arrival at a later slot is rejected while an earlier scheduled group remains
pending; groups in the same slot may resolve in either order. This ordering is
only a draft invariant and does not prove that intervening phase obligations
have been completed.

Receipt lookup precedes version checking: an exact command retry returns the
original event after replay, while reuse of its ID with different payload fails.
Replay reconstructs that initial world, checks every event through the same transition,
and rejects retries, gaps, reordering, forged effects, and foreign identities.
Candidate event encoding uses a versioned fixed-field UTF-8 JSON form. Decoding rejects noncanonical bytes and validates by replay before any state is accepted. Encoded event lists remain candidate data only; callers must authenticate the input journal.

This deliberately exercises deferred elements in isolation. It does not model
advancement, other obligations, real phase ordering,
Host storage/atomic commit, player authentication or privacy. None
of its records may be used as existing campaign persistence. An authoritative
successor must bind its trusted initial world and setup, authenticate event history,
use versioned codecs and atomic Host event/receipt writes, then add side-safe
projection and tests before activation.

## Host boundary checked

`TemporaryCampaignGrain.SubmitAsync` authenticates the capability and actor,
checks request reuse, delegates canonical submissions to `CampaignHostAdapter`,
and atomically updates its in-memory event/snapshot/receipt view within a grain
turn. Its own comment states that process restart loses this memory. Activation
reloads that process-local store. Therefore this adapter is useful as a behavioral
model, but does not provide the requested durable arrival journal across process
restart. Arrival integration needs an admitted Core action and replay codec plus
a durable storage transaction; adding a side channel to the temporary grain would
bypass its audience proofs and would not satisfy persistence.

The normal Host also has `CampaignDurableAuthorityJournal` (separate from the
temporary grain). Its existing SQLite immediate transaction commits canonical
accepted evidence and head together, and cold reads import and replay the
retained transcript. Arrival work should extend that journal and its existing
seat/opening/receipt path after a real Core action and versioned event are
admitted. It should not introduce an independent arrival store. The draft codec
above is not that event codec and cannot be inserted into `accepted_steps`.
See [durable authority journal](campaign-durable-authority-journal-v1.md).

## Owner admission dependency

Existing `SubmitOwnerActionCore` keys its authenticated opening and retry lookup
by `actingElementId`, seat epoch, opening revision and owner action reference.
An off-map arrival group has no current world element to use as that key. A
native arrival integration needs a separately typed arrival-opening subject,
bound to a currently eligible group and owner, with its own domain-separated
reference and cold-replay checks. Do not invent an on-map acting unit, reuse an
opponent-visible future group ID, or route this draft codec through a movement
opening. The existing accepted-step transaction can be reused only after its
Core action, side projection and typed opening semantics are implemented and
verified together.
