# Grouped campaign world, version 9

The grouped world retains the complete version 8 world and pending arrivals, and adds
formation members that are represented by a parent counter. Each member has a stable
ID, optional parent ID, nullable infantry TOE and component type/TOE, capability-point
allowance and expenditure, cohesion, and an explicit motor-point reservation. Unknown
allowances are null and cannot establish movement capacity. Null component fields
mean unknown; they do not assert that a headquarters has no components.
When a source unit has more than one component, the optional additional-component
array retains each distinct type and current TOE; it is omitted when empty so earlier
world bytes remain unchanged. Source-profile validation rejects components not present
in that profile's pinned content.
The group's source content identity, owner, location, adjudicated stack value and motor pool are
authoritative. Member IDs must be unique and disjoint from existing and pending units;
each parent chain must terminate at the group's root.

Stacking counts the grouped counter once, not its attached members. Its value remains
unknown until shell status is established; a printed full counter value alone is not
proof. Existing counter
values are supplied by the caller's validated content; if any value is unknown, the
result is unknown. This calculation does not certify a hex's terrain or stack limit.
Motor-point adjustment conserves the group pool and cannot exceed a member's known
infantry TOE. It reserves a transport duty from the highest parent's pool; it does not
transfer ownership or detach points to the member (Land sections 8.95–8.96). It records
infantry lift only; full mobility, global priority, cargo and
fuel remain separate obligations. Legal owner choices and source-specific priority
must be re-adjudicated by the owning authority before invoking this operation.

The source successor must retain and replay this world as its authoritative state,
bind the exact content and predecessor hashes in its event, and reject stale choices.
Version 8 serialization and previously recorded events remain unchanged. This schema
does not start a turn or authorize movement.

A confirmed formation's eligible, unreserved leaf can be detached only after the
owning source authority validates its hierarchy and stacking. The transition removes
the member from the formation and retains it as an independent occupant, with its
component and operational state. It supplies the resulting formation stack explicitly;
the generic world does not assume a formation remains non-shell. A later World8
promotion replaces that occupant only after checking its identity, location, component
TOE, expenditure and cohesion. Other additions at a grouped location fail closed.
The optional detached array is omitted when empty, preserving earlier canonical bytes.
Parent-owned motor reservations remain charged to the same pool when a member is
detached or promoted. An independently represented priority member's reservation
is retained separately from its World8 counter; source admission and replay must
bind it to the exact selected unit and the earlier reservation ledger. Empty external
reservation arrays are omitted to preserve earlier bytes.
