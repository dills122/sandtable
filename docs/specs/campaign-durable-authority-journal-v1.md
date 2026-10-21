# Campaign durable authority journal v1

Normal campaign Host storage retains the canonical Core creation evidence and every accepted command, event, receipt, and snapshot checkpoint. The store is an internal authority journal, not a browser contract or a second adjudicator. Core replay of the complete transcript is required on every cold read and before append.

## Schema and transition

- `campaigns` binds the versioned creation request fields, canonical creation event, initial snapshot, head sequence, and head hash. Campaign IDs are unique. A creation cannot overwrite an existing campaign.
- `accepted_steps` has one row per accepted Core transition: contiguous sequence, canonical command, canonical event, canonical receipt, checkpoint, prior hash, and record hash. The hash binds length-prefixed bytes; the initial hash binds the request and both creation artifacts.
- `operator_capability` holds only a domain-separated digest of a 32-byte operator secret. `seats` holds two possible sides per campaign, an increasing epoch, and at most one outstanding grant or credential digest per side. Grants live ten minutes and are single use. Redeemed credentials live two hours. A replacement grant increments epoch and invalidates the prior credential, including after process restart.
- `reference_key` holds a private 256-bit HMAC key. `owner_openings` binds the authenticated seat epoch, acting element, visible bytes and digest, audience revision, and current private authority version. `owner_actions` holds the reference-to-Core-action map separately. Query checks the seat and replays Core inside one immediate transaction, then replaces the private map at the current cut. Equal visible bytes under an unseen authority change retain the opening revision and outward references; a changed view or seat epoch advances the revision.
- Append takes a SQLite immediate transaction, replays the retained prefix through Core, checks the submitted command against that exact head, then commits evidence and head in the same transaction. Rejected commands write nothing. A concurrent or stale command cannot advance the head.
- Read uses one SQLite snapshot transaction. It rejects missing, reordered, truncated, or altered records and requires `CampaignHostAdapter.Import` to reproduce every retained byte. A failed read never returns a session.

This journal stores no plaintext seat secret. Owner-opening queries bind credentials to one Core cut and retain a private action map, but there is no public route. The normal Host still needs a contact-derived Core submit path, idempotent request/receipt storage, and outward API before exposing an action route. Historical temporary and P01 private stores remain separate.
