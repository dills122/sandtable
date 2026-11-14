# Campaign scheduled-arrival content draft v1

**Status:** bounded, non-admitting analysis of real Core content definitions.
This is a contract for a first campaign integration prerequisite, not a new
content format, setup identity, campaign event, or playable scenario.

## Problem

`ContentPackValidator` requires every independent element to have an initial
placement in every scenario. A scheduled reinforcement must instead exist in
the selected scenario's content without already existing on the map. Changing
that legacy rule would reinterpret existing content and hashes. The successor
must eventually bind initial and deferred definitions, schedule, permitted
destinations, other obligations, rules and rulings to one versioned identity.

## Draft analysis contract

Analyze a `ContentPackDefinition`, one explicitly named scenario and an
explicit list of `ScheduledArrivalGroup` records. Return the unmodified legacy
validation result, unexplained legacy issues, draft-specific issues and exact legacy
`placement.missing-element` paths that would be explained by **individually
valid** deferred entries. This is diagnostic only: a clean draft report does
not create a `ContentPackArtifact`, change legacy validation, admit a setup or
authorize phase completion.

The selected scenario must occur exactly once. Each group must have unique ID,
an indexed source, a slot inside the scenario's inclusive boundaries, and one
or more independently placeable existing content elements of the matching side.
Each deferred element must not be initially placed and may be listed only once
in this draft; withdrawal/return requires a separately versioned lifecycle.
Duplicate IDs, unknown elements, wrong owners, attached-only elements,
out-of-range dates, unknown sources, initially placed elements or duplicate
deferred elements generate issues and cannot explain a legacy missing-placement
issue. Entries valid in one group must not mask a defect in another group.

Legacy validation remains intact and its exact issues remain available. An
explanation applies only to the selected scenario's exact element path, never to
another scenario, a different error code, an unknown or ambiguous element, or
an invalid group. Missing unlisted elements remain unexplained. The unexplained
view removes only those exact missing-placement issues; the original legacy
result remains unchanged. Source references
and sides are internal authority data, not player-facing views.

## Further gates

A future admitted format must validate source and rights, identify the full set
of eligible destinations and non-land obligations, define canonical bytes and
content/rules/ruling binding, and prove compatible creation, snapshot, event,
projection, durable receipts, observation privacy and replay. This draft does
not enumerate legal destinations, authenticate a schedule, model returns or
create units. An empty schedule is not proof an arrival phase is empty.
