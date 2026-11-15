# Campaign world v8 initial state

World v8 binds its initial state to the exact scheduled combat content identity and
scenario. Explicit initial placements and members of initial groups are placed at
their recorded locations, in element-ID order. A group records co-location only;
it does not establish command attachment or assign printed trucks to elements.
Each placed element uses its scenario-specific component TOE seeds. Scheduled
members have no map representation or operational state at creation; their
source schedules remain pending in the world.
Placed members use the same element and representation initializer as ordinary
campaign creation, with the initial operational policy supplied explicitly.

This initial-state contract is not campaign admission. It checks content structure,
but does not assert that combat rules support every recorded weapon or rating. Its operational
defaults are supplied explicitly by the caller, since the source content does not
yet establish all initial operational conditions. Attachment-only members and
vehicle breakdown cohorts are rejected until their initialization is specified.
Non-empty initial reserve designations likewise need a side-specific policy.
The [source-initialization contract](campaign-source-creation-v1.md) now retains
and replays this initial state in the normal Host journal. Rules for arrival
execution, turn advancement, gameplay snapshots and playable admission still
require their own contracts before this world can be played.
