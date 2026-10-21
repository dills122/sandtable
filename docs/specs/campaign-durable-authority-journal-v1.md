# Campaign durable authority journal v1

Normal campaign Host storage retains the canonical Core creation evidence and every accepted command, event, receipt, and snapshot checkpoint. The store is an internal authority journal, not a browser contract or a second adjudicator. Core replay of the complete transcript is required on every cold read and before append.

## Schema and transition

- `campaigns` binds the versioned creation request fields, canonical creation event, initial snapshot, head sequence, and head hash. Campaign IDs are unique. A creation cannot overwrite an existing campaign.
- `accepted_steps` has one row per accepted Core transition: contiguous sequence, canonical command, canonical event, canonical receipt, checkpoint, prior hash, and record hash. The hash binds length-prefixed bytes; the initial hash binds the request and both creation artifacts.
- Append takes a SQLite immediate transaction, replays the retained prefix through Core, checks the submitted command against that exact head, then commits evidence and head in the same transaction. Rejected commands write nothing. A concurrent or stale command cannot advance the head.
- Read uses one SQLite snapshot transaction. It rejects missing, reordered, truncated, or altered records and requires `CampaignHostAdapter.Import` to reproduce every retained byte. A failed read never returns a session.

This journal stores no seat secret or owner opening. The normal Host must add authenticated seats, a visible opening and private action map, request idempotency, and receipt lookup before exposing any action route. Historical temporary and P01 private stores remain separate.
