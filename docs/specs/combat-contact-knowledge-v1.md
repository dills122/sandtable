# Campaign contact knowledge v1

This is the first Core implementation of the permanent contact-based fog rule
described in [the architecture decision](../research/combat-fog-of-war-architecture-spike.md).
It is the foundation of the successor to `CMB-CON-005`, not another campaign
profile. Historical V6/V7 observations and P01 records keep their original
meaning. New campaigns must not publish this partial contact view until Core
source detection, durable replay, Host opening, and all outward consumers use
the successor together.

## Authority and event shape

Core alone emits `CampaignContactTransition` at an adjudicated authority cut.
Version 1 carries campaign, side, authority version, unique source event ID,
visibility rule ID, transition kind, side-local handle, private bound element,
observer formation, apparent location, identification level and access scope.
The private binding and source IDs remain in authority storage. The handle is
allocated monotonically within one side (`c-00000001`, then `c-00000002`),
never derived from an opponent ID or private hash.

Transitions are `observed`, `updated`, `lost`, and `rediscovered`. `Observed`
creates a new contact; `updated` changes an existing current contact after new
visible evidence; `lost` preserves its last-known fields and marks it stale;
`rediscovered` makes that same stale binding current after new observation.
A private authority transition may update source provenance without changing
any player-visible field. Such a transition does not advance audience revision.
No side-visible transition may be inferred solely from hidden enemy movement.

Replay rejects wrong campaign or side, regressing authority version, duplicate
source/contact pair, invented or reused handle, and a transition against the
wrong private binding. Event replay, not a caller-supplied World projection,
is the source of side contact knowledge for the successor.

`CampaignContactJournal` now retains canonical contact records beside the
campaign's committed movement stream. Its cold readback re-derives each record
from independently replayed movement events and compares exact bytes; a
missing, altered, or invented contact record fails closed. The journal owns
both sides' knowledge and emits only the requested side's view. This is the
Core replay contract that durable campaign storage must append atomically
with its source event. It does not by itself issue an owner opening.
`CampaignExercises` now carries this journal with its accepted event history;
session reconstruction independently replays contact records from that
history and rejects a mismatch. The current V7 observation remains a
historical reader. New owner observations must use the successor contact
projection and action set at one cut.

The first live source is a committed native Movement v3 event with a frozen
Reaction opportunity. Its adjacency evidence identifies a specific reacting
formation and the apparent moved location. Core derives one contact event for
that formation from the committed move and its private trigger binding.
Derivation is pure against the retained ledger; the caller appends the events
durably and replays them before publishing an observation. A move without a
frozen opportunity emits no contact from this source. Other detection,
sharing and loss sources will use this same ledger and outward contract.

## Player view and action boundary

`CampaignContactObservationV1` projects only contacts accessible to the acting
formation and a side-audience revision. A current contact can become a
visibility-qualified target candidate. A stale contact remains a last-known
marker and cannot be a direct-assault target candidate. A local contact is
usable by its observer; a side-shared contact is usable by the side. Combat
resources, obligations and timing must further filter target candidates before
any legal action is offered. A side observation never contains a private
opponent ID, source ID, or authority version. The private target lookup stays
inside Core and rejects stale or inaccessible handles.

The next implementation stage attaches further authentic sight/combat/share
sources, persists these transitions with the campaign, and makes one Core
observation/action pair and Host owner opening consume them. Until that is
complete, this contract is internal and unissuable; it does not admit Combat.
